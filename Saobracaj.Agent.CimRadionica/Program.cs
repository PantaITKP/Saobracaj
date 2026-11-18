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
            if (a.Count >= 2 && a[0] == "--radnik") return Radnik(a[1]);
            if (a.Count >= 3 && a[0] == "--uvezi") return Uvezi(a[1], a.Skip(2).ToList());
            if (a.Count >= 2 && a[0] == "--obradi") return ObradiBezUi(a);
            if (a.Count >= 3 && a[0] == "--recnik") { var pp = Podesavanja.Ucitaj(); GlavnaForma.Recnik(RadniProstor.Otvori(Path.Combine(pp.Radni, a[1])), a.Count > 3 ? a[3] : pp.Verzija, a[2]); return 0; }
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
            var online = a.Contains("--online");
            var konekcija = online ? p.Konekcija() : null;
            if (online && string.IsNullOrEmpty(konekcija)) { Console.Error.WriteLine("Online: konekcija nije podešena."); return 1; }
            if (a.Contains("--ispocetka")) rp.KreniIspocetka();
            var samo = Op("--samo")?.Split(',').ToList();
            var stavke = rp.Hronoloski().Where(s => samo == null || samo.Contains(s.Kljuc) || samo.Contains(rp.Rb(s).ToString("000"))).ToList();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int n = 0;
            for (int i = 0; i < stavke.Count; i += p.VelicinaDela)
            {
                var deo = stavke.Skip(i).Take(p.VelicinaDela).ToList();
                var posao = new PosaoRadnika { Koren = rp.Koren, Kljucevi = deo.Select(x => x.Kljuc).ToList(), Online = online, Verzija = verzija, Kes = p.Kes, Analiza = a.Contains("--analiza") ? rp.Koren : null };
                var fajl = Path.Combine(Path.GetTempPath(), "CimRadionica-posao-" + Guid.NewGuid().ToString("N") + ".json");
                File.WriteAllText(fajl, JsonConvert.SerializeObject(posao), new UTF8Encoding(false));
                var psi = new System.Diagnostics.ProcessStartInfo(Application.ExecutablePath, "--radnik \"" + fajl + "\"") { UseShellExecute = false, RedirectStandardOutput = true, StandardOutputEncoding = Encoding.UTF8 };
                if (!string.IsNullOrEmpty(konekcija)) psi.EnvironmentVariables["CIMPROVERA_KONEKCIJA"] = konekcija; else psi.EnvironmentVariables.Remove("CIMPROVERA_KONEKCIJA");
                using (var pr = System.Diagnostics.Process.Start(psi))
                {
                    string l;
                    while ((l = pr.StandardOutput.ReadLine()) != null) { n++; Console.WriteLine(n + "/" + stavke.Count + " [" + sw.Elapsed.ToString(@"hh\:mm\:ss") + "] " + l); }
                    pr.WaitForExit();
                }
                try { File.Delete(fajl); } catch { }
            }
            rp = RadniProstor.Otvori(rp.Koren);
            if (a.Contains("--analiza")) Analiza.ZbirnoIzFoldera(rp.Koren, verzija);
            Console.WriteLine("Zbirni Excel: " + Zbirno.Sacuvaj(rp, verzija) + " (" + sw.Elapsed.ToString(@"hh\:mm\:ss") + ")");
            return 0;
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
                    catch (Exception ex) { Console.WriteLine("GRESKA " + s.Kljuc + " | " + ex.Message.Replace('\n', ' ')); }
                    Console.Out.Flush();
                }
            return 0;
        }
    }
}
