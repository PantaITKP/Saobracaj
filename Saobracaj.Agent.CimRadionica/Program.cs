using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using Saobracaj.Agent.CimProvera;
using Saobracaj.Agent.CimProvera.Mail;
using Saobracaj.Agent.CimProvera.Radionica;

namespace Saobracaj.Agent.CimRadionica
{
    /// <summary>Podešavanja alata (C:\CimRadionica\podesavanja.json) – bez tajni.</summary>
    internal sealed class Podesavanja
    {
        public string Koren = @"C:\CimRadionica";
        public string RadniProstor = "Osnovni";
        public bool Online;
        public string Verzija = RezultatJson.VerzijaPrograma;
        public string TestiranjeFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Testiranje");
        public int VelicinaDela = 20;
        /// <summary>
        /// v3.0 (odluka 18.11., laptop se ugasio pod punim opterećenjem – Kernel-Power 41): najviše toliko radnika radi istovremeno
        /// (pred-čitanje/OCR); hronološka obrada posle ide jednim radnikom iz keša. Podrazumevano 2.
        /// </summary>
        public int BrojRadnika = 2;
        /// <summary>Prioritet procesa radnika (Idle, BelowNormal, Normal); podrazumevano BelowNormal.</summary>
        public string PrioritetRadnika = "BelowNormal";

        public string Radni => Path.Combine(Koren, "Radni");
        public string Kes => Path.Combine(Koren, "kes");
        public string KonekcijaFajl => Path.Combine(Koren, "Konekcija.dpapi");
        private static string Fajl(string koren) => Path.Combine(koren, "podesavanja.json");

        public static Podesavanja Ucitaj()
        {
            var koren = Environment.GetEnvironmentVariable("CIMRADIONICA_KOREN");
            if (string.IsNullOrEmpty(koren)) koren = @"C:\CimRadionica";
            try
            {
                if (File.Exists(Fajl(koren)))
                {
                    var p = JsonConvert.DeserializeObject<Podesavanja>(File.ReadAllText(Fajl(koren), Encoding.UTF8)) ?? new Podesavanja();
                    p.Koren = koren;
                    return p;
                }
            }
            catch { }
            return new Podesavanja { Koren = koren };
        }

        public void Sacuvaj()
        {
            Directory.CreateDirectory(Koren);
            File.WriteAllText(Fajl(Koren), JsonConvert.SerializeObject(this, Formatting.Indented), new UTF8Encoding(false));
        }

        /// <summary>Konekcija iz DPAPI fajla (trenutni korisnik) ili CIMPROVERA_KONEKCIJA; null ako nije podešena. Nikad se ne upisuje u log ni rezultat.</summary>
        public string Konekcija()
        {
            var env = Environment.GetEnvironmentVariable("CIMPROVERA_KONEKCIJA");
            if (!string.IsNullOrWhiteSpace(env)) return env;
            try { return File.Exists(KonekcijaFajl) ? AgentConfig.Desifruj(File.ReadAllText(KonekcijaFajl).Trim()) : null; } catch { return null; }
        }

        public void SacuvajKonekciju(string konekcija)
        {
            Directory.CreateDirectory(Koren);
            File.WriteAllText(KonekcijaFajl, AgentConfig.Zastiti(konekcija));
        }

        public PodesavanjaAlata ZaObradu(string verzija = null) => new PodesavanjaAlata
        {
            Online = Online, Konekcija = Online ? Konekcija() : null, Verzija = verzija ?? Verzija, KesFolder = Kes,
        };
    }

    /// <summary>Posao radnika (fajl u %TEMP%; bez konekcije – ona ide kroz promenljivu okruženja procesa).</summary>
    internal sealed class PosaoRadnika
    {
        public string Koren = "";
        public List<string> Kljucevi = new List<string>();
        public bool Online;
        public string Verzija = "";
        public string Kes = "";
        public string Analiza;   // folder za _Analiza (praćenje foldera Testiranje); null = bez analize
        /// <summary>v3.0: samo pred-čitanje (OCR u keš), bez obrade – više takvih radnika radi istovremeno.</summary>
        public bool SamoCitanje;
    }

    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            // nikad SMTP: jezgro bez transporta ne šalje; alat postavlja prazan transport (pripremljene poruke se samo čuvaju)
            Slanje.Transport = (c, m) => { };
            var a = args.ToList();
            if (a.Count > 0 && a[0] != "--radnik" && a[0].StartsWith("--") && a[0] != "--snimci")
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true });
            // v3.0.1 (4.3, „Syncfusion licencni ključ nije učitan (nije učitavan)“ za 10 Word priloga u „Osnovni“): alat ključ nije ni učitavao –
            // radnik (i pred-čitanje) ga učitava pre čitanja priloga, kao agent; bez ključa Word prilog ide na ručnu proveru
            if (a.Count >= 2 && a[0] == "--radnik") { try { Saobracaj.Agent.CimProvera.Prilozi.WordUPdf.UcitajKljuc(); } catch { } return Radnik(a[1]); }
            if (a.Count >= 3 && a[0] == "--uvezi") return Uvezi(a[1], a.Skip(2).ToList());
            if (a.Count >= 2 && a[0] == "--obradi") return ObradiBezUi(a);
            // v3.0 (odluka 18.11.): online režim na laptopu – konekcija iz CIMPROVERA_KONEKCIJA (nikad iz argumenata) se čuva DPAPI-jem
            // za trenutnog korisnika; folder Testiranje na Desktop-u
            // v3.0.1 (4.3): Syncfusion licencni ključ (Word → PDF) za nalog korisnika na laptopu – iz promenljive CIMPROVERA_SYNCFUSION (postavlja je
            // Instaliraj-CimRadionica.ps1 posle unosa) u %LOCALAPPDATA%\Saobracaj\CimProvera\Syncfusion.dpapi (DPAPI, trenutni korisnik). Ključ se ne ispisuje.
            if (a.Count >= 1 && a[0] == "--podesi-syncfusion")
            {
                var kljuc = Environment.GetEnvironmentVariable("CIMPROVERA_SYNCFUSION");
                if (string.IsNullOrWhiteSpace(kljuc)) { Console.Error.WriteLine("CIMPROVERA_SYNCFUSION nije postavljena."); return 1; }
                var put = Saobracaj.Agent.CimProvera.Prilozi.WordUPdf.PutanjeKljuca().Last();
                Directory.CreateDirectory(Path.GetDirectoryName(put));
                File.WriteAllText(put, AgentConfig.Zastiti(kljuc.Trim()), new System.Text.UTF8Encoding(false));
                kljuc = null;
                var ok = Saobracaj.Agent.CimProvera.Prilozi.WordUPdf.UcitajKljuc();
                Console.WriteLine("Syncfusion ključ sačuvan (DPAPI, trenutni korisnik) u " + put + "; provera: " + Saobracaj.Agent.CimProvera.Prilozi.WordUPdf.StanjeKljuca);
                return ok ? 0 : 2;
            }
            // v3.0.1: poređenje dve verzije iz komandne linije – --poredi <prostor> <bila> <sada> (Poredjenje_<bila>_<sada>.xlsx u radnom prostoru)
            if (a.Count >= 4 && a[0] == "--poredi")
            {
                var pp = Podesavanja.Ucitaj();
                var rp0 = RadniProstor.Otvori(Path.Combine(pp.Radni, a[1]));
                Console.WriteLine("Poređenje: " + Zbirno.SacuvajPoredjenje(rp0, a[2], a[3]));
                return 0;
            }
            if (a.Count >= 1 && a[0] == "--podesi-online")
            {
                var pp = Podesavanja.Ucitaj();
                var kon = Environment.GetEnvironmentVariable("CIMPROVERA_KONEKCIJA");
                if (string.IsNullOrWhiteSpace(kon)) { Console.Error.WriteLine("CIMPROVERA_KONEKCIJA nije postavljena."); return 1; }
                pp.SacuvajKonekciju(kon);
                pp.Online = true;
                pp.TestiranjeFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Testiranje");
                pp.Sacuvaj();
                Console.WriteLine("Online režim podešen; konekcija sačuvana (DPAPI, trenutni korisnik) u " + pp.KonekcijaFajl + "; Testiranje: " + pp.TestiranjeFolder);
                return 0;
            }
            // v3.0 (odluka 18.11.): paket za Claude – --paket-claude <prostor> <verzija> <izlazni folder> [dodatni fajlovi…] (delovi ≤ 100 MB)
            if (a.Count >= 4 && a[0] == "--paket-claude")
            {
                var pp = Podesavanja.Ucitaj();
                var rp0 = RadniProstor.Otvori(Path.Combine(pp.Radni, a[1]));
                var delovi = Zbirno.PaketZaClaude(rp0, a[2], a[3], a.Skip(4), out var izost);
                foreach (var d in delovi) Console.WriteLine("Deo: " + d + " (" + (new FileInfo(d).Length / 1048576.0).ToString("0.0") + " MB)");
                Console.WriteLine("Izostavljeno (tajne): " + izost.Count);
                return 0;
            }
            // v3.0: paket za analizu iz komandne linije – --paket <prostor> <verzija> <izlaz.zip> [--samo-ne]
            if (a.Count >= 4 && a[0] == "--paket")
            {
                var pp = Podesavanja.Ucitaj();
                var rp0 = RadniProstor.Otvori(Path.Combine(pp.Radni, a[1]));
                var zip = Zbirno.Paket(rp0, a[2], a[3], a.Contains("--samo-ne"), out var izost);
                Console.WriteLine("Paket: " + zip + " (" + new FileInfo(zip).Length / 1048576.0 + " MB); izostavljeno (tajne): " + izost.Count);
                return 0;
            }
            // osvežavanje lokalne kopije šifarnika (samo čitanje) – konekcija iz DPAPI fajla ili CIMPROVERA_KONEKCIJA, nikad iz argumenata
            if (a.Count >= 1 && a[0] == "--sifarnici")
            {
                var pp = Podesavanja.Ucitaj();
                var kon = pp.Konekcija();
                if (string.IsNullOrEmpty(kon)) { Console.Error.WriteLine("Konekcija nije podešena."); return 1; }
                var folder = Path.Combine(pp.Koren, "sifarnici");
                Console.WriteLine("Kopija šifarnika: " + SifarniciKopija.Napravi(kon, folder).ToString("dd.MM.yyyy HH:mm") + " → " + folder);
                return 0;
            }
            if (a.Count >= 3 && a[0] == "--recnik"){ var pp = Podesavanja.Ucitaj(); GlavnaForma.Recnik(RadniProstor.Otvori(Path.Combine(pp.Radni, a[1])), a.Count > 3 ? a[3] : pp.Verzija, a[2]); return 0; }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var p = Podesavanja.Ucitaj();
            if (a.Count >= 2 && a[0] == "--snimci")
            {
                using (var f = new GlavnaForma(p)) return f.Snimci(a[1]);
            }
            Application.Run(new GlavnaForma(p));
            return 0;
        }

        /// <summary>--uvezi &lt;radni prostor&gt; &lt;putanja&gt;… : uvoz bez UI (fajlovi, folderi, .zip).</summary>
        private static int Uvezi(string prostor, List<string> putanje)
        {
            var p = Podesavanja.Ucitaj();
            var rp = RadniProstor.Otvori(Path.Combine(p.Radni, prostor));
            var greske = new List<string>();
            var nove = rp.Uvezi(putanje, greske);
            Console.WriteLine("Uvezeno " + nove.Count + " novih poruka u „" + prostor + "“ (ukupno " + rp.Indeks.Stavke.Count + ")" + (greske.Count > 0 ? "; greške:\n  " + string.Join("\n  ", greske) : ""));
            return 0;
        }

        /// <summary>
        /// --obradi &lt;radni prostor&gt; [--verzija V] [--analiza] [--online] [--ispocetka] [--samo k1,k2]: obrada bez UI, hronološki, u delovima od po
        /// VelicinaDela poruka (svaki deo u posebnom procesu); posle obrade Zbirni Excel (i INDEKS/SAŽETAK analize).
        /// </summary>
        private static int ObradiBezUi(List<string> a)
        {
            var p = Podesavanja.Ucitaj();
            string Op(string k) { var i = a.IndexOf(k); return i > 0 && i + 1 < a.Count ? a[i + 1] : null; }
            var rp = RadniProstor.Otvori(Path.Combine(p.Radni, a[1]));
            var verzija = Op("--verzija") ?? p.Verzija;
            var online = a.Contains("--online") || (p.Online && !a.Contains("--offline"));
            var konekcija = online ? p.Konekcija() : null;
            if (online && string.IsNullOrEmpty(konekcija)) { Console.Error.WriteLine("Online: konekcija nije podešena."); return 1; }
            if (a.Contains("--ispocetka")) rp.KreniIspocetka();
            var samo = Op("--samo")?.Split(',').ToList();
            // --od NNN: nastavak posle prekida (redni broj u hronološkom redu, bez --ispocetka – dosije ranijih poruka ostaje)
            var od = int.TryParse(Op("--od"), out var odRb) ? odRb : 1;
            // --neuspele: samo poruke čiji rezultat ove verzije je „nije obrađen“ (greška okruženja / privremena greška)
            var neuspeleRanije = a.Contains("--neuspele") ? new HashSet<string>(rp.Neuspeli(verzija).Select(x => x.Kljuc)) : null;
            var stavke = rp.Hronoloski().Where(s => (samo == null || samo.Contains(s.Kljuc) || samo.Contains(rp.Rb(s).ToString("000"))) && rp.Rb(s) >= od
                                                   && (neuspeleRanije == null || neuspeleRanije.Contains(s.Kljuc))).ToList();
            // v3.0.1 (4.3): bez Syncfusion ključa – jasno upozorenje NA POČETKU obrade (ne tek po prilogu)
            if (!Saobracaj.Agent.CimProvera.Prilozi.WordUPdf.UcitajKljuc())
                Console.Error.WriteLine("UPOZORENJE: " + GlavnaForma.PorukaBezSyncfusion());
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int n = 0;
            var analiza = a.Contains("--analiza");
            GlavnaForma.PostaviRadnike(p);
            // v3.0: pred-čitanje (OCR u keš) sa najviše BrojRadnika radnika istovremeno, pa hronološka obrada jednim radnikom iz keša
            if (p.BrojRadnika > 1 && stavke.Count > 1)
            {
                var citano = 0;
                GlavnaForma.PredCitaj(rp.Koren, stavke.Select(x => x.Kljuc).ToList(), p, online ? konekcija : null, l =>
                {
                    if (l.StartsWith("CITANO ") || l.StartsWith("CITANJE-GRESKA ")) System.Threading.Interlocked.Increment(ref citano);
                    if (l.StartsWith("CITANJE-GRESKA ")) Console.Error.WriteLine(l);
                });
                Console.WriteLine("Pred-čitanje: " + citano + "/" + stavke.Count + " poruka, " + p.BrojRadnika + " radnika (" + sw.Elapsed.ToString(@"hh\:mm\:ss") + ")");
            }
            for (int i = 0; i < stavke.Count; i += p.VelicinaDela)
            {
                var deo = stavke.Skip(i).Take(p.VelicinaDela).ToList();
                var posao = new PosaoRadnika { Koren = rp.Koren, Kljucevi = deo.Select(x => x.Kljuc).ToList(), Online = online, Verzija = verzija, Kes = p.Kes, Analiza = analiza ? rp.Koren : null };
                var javljeni = new HashSet<string>();
                var neuspeh = GlavnaForma.PokreniRadnika(posao, konekcija, l =>
                {
                    n++; Console.WriteLine(n + "/" + stavke.Count + " [" + sw.Elapsed.ToString(@"hh\:mm\:ss") + "] " + l);
                    var m = System.Text.RegularExpressions.Regex.Match(l, @"^(?:OK|GRESKA) (.+?) \| ");
                    if (m.Success) javljeni.Add(m.Groups[1].Value);
                });
                // v3.0: pad radnika se nikad ne prećutkuje – poruke dela bez ishoda dobijaju „nije obrađen – greška okruženja …“; obrada ide dalje
                var neobradjene = RadniProstor.ZabeleziNeobradjene(rp.Koren, posao.Kljucevi, javljeni, verzija, neuspeh, online, analiza);
                foreach (var s in neobradjene) { n++; Console.WriteLine(n + "/" + stavke.Count + " [" + sw.Elapsed.ToString(@"hh\:mm\:ss") + "] NIJE OBRAĐEN " + s.Kljuc + " | " + s.Odluka); }
                if (neuspeh != null && neobradjene.Count == 0) Console.Error.WriteLine("Radnik: " + neuspeh + " (sve poruke dela imaju ishod).");
            }
            rp = RadniProstor.Otvori(rp.Koren);
            if (analiza) Analiza.ZbirnoIzFoldera(rp.Koren, verzija);
            Console.WriteLine("Zbirni Excel: " + Zbirno.Sacuvaj(rp, verzija) + " (" + sw.Elapsed.ToString(@"hh\:mm\:ss") + ")");
            var neuspeli = rp.Neuspeli(verzija);
            if (neuspeli.Count == 0) return 0;
            Console.WriteLine();
            Console.WriteLine("NISU OBRAĐENI (" + neuspeli.Count + "):");
            foreach (var s in neuspeli) Console.WriteLine("  " + rp.Rb(s).ToString("000") + "  " + s.Datum.ToString("dd.MM.yyyy HH:mm") + "  " + s.Naslov + "  – " + s.Odluka);
            Console.WriteLine("Ponovna obrada neuspelih: CimRadionica.exe --obradi " + a[1] + " --verzija " + verzija + " --neuspele");
            return 2;
        }

        /// <summary>
        /// Radnik: obrađuje zadate poruke radnog prostora hronološki u ovom procesu (jedan čitač, jedno jezgro) i piše napredak na izlaz
        /// („OK ključ | odluka“ / „GRESKA ključ | poruka“). Indeks radnog prostora čuva posle svake poruke.
        /// </summary>
        private static int Radnik(string posaoFajl)
        {
            var posao = JsonConvert.DeserializeObject<PosaoRadnika>(File.ReadAllText(posaoFajl, Encoding.UTF8));
            // WinExe nema konzolu: UTF-8 direktno na standardni izlaz (čita ga glavni proces)
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true });
            var rp = RadniProstor.Otvori(posao.Koren);
            var pa = new PodesavanjaAlata { Online = posao.Online, Konekcija = posao.Online ? Environment.GetEnvironmentVariable("CIMPROVERA_KONEKCIJA") : null, Verzija = posao.Verzija, KesFolder = posao.Kes };
            var analize = new List<(int, RezultatAlata, string)>();
            if (posao.SamoCitanje)
            {
                using (var rad = new RadnikObrade(rp, pa))
                    foreach (var s in rp.Hronoloski().Where(x => posao.Kljucevi.Contains(x.Kljuc)))
                    {
                        try { rad.Procitaj(s); Console.WriteLine("CITANO " + s.Kljuc + " | "); }
                        catch (Exception ex) { Console.WriteLine("CITANJE-GRESKA " + s.Kljuc + " | " + ex.Message.Replace('\n', ' ')); }
                        Console.Out.Flush();
                    }
                return 0;
            }
            using (var rad = new RadnikObrade(rp, pa))
                foreach (var s in rp.Hronoloski().Where(x => posao.Kljucevi.Contains(x.Kljuc)))
                {
                    try
                    {
                        var r = rad.Obradi(s);
                        rp.Sacuvaj();
                        if (posao.Analiza != null)
                        {
                            var nit = rp.Niti().FirstOrDefault(n => n.Contains(s));
                            var i = nit?.IndexOf(s) ?? -1;
                            analize.Add((rp.Rb(s), r, Analiza.Napisi(posao.Analiza, posao.Verzija, rp.Rb(s), r,
                                i > 0 ? nit[i - 1].Naslov : null, nit != null && i >= 0 && i < nit.Count - 1 ? nit[i + 1].Naslov : null)));
                        }
                        Console.WriteLine("OK " + s.Kljuc + " | " + s.Odluka);
                    }
                    catch (Exception ex)
                    {
                        // v3.0: rezultat nije zapisan – zapis „nije obrađen“ (stari rezultat iste verzije se ne zadržava)
                        try { var f = rp.ZabeleziNeuspeh(s, posao.Verzija, Stanje.GreskaOkruzenja(ex) ?? ex.GetType().Name + ": " + ex.Message, posao.Online); rp.Sacuvaj(); if (posao.Analiza != null) Analiza.NapisiNeuspeh(posao.Analiza, posao.Verzija, rp.Rb(s), s, f); }
                        catch { }
                        Console.WriteLine("GRESKA " + s.Kljuc + " | " + ex.Message.Replace('\n', ' '));
                    }
                    Console.Out.Flush();
                }
            return 0;
        }
    }
}
