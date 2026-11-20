using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Mail;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera
{
    internal static class Program
    {
        private const string Pomoc = @"Saobracaj.Agent.CimProvera – provera CIM/CUV tovarnih listova u mailu

  (bez argumenata) | --once        jedan prolaz kroz sanduče (Task Scheduler)
  --petlja [minuta]                stalni rad: prolaz na svakih N minuta (podrazumevano 2)
  --dry-run                        uz --once/--petlja: ne šalje mail i ne pamti stanje, izveštaj ispisuje na ekran
  validate <fajl|folder>…          provera fajlova bez maila (folder = jedan mail)
      --podfolderi                 svaki podfolder je poseban mail (npr. testovi\fixtures)
      --rezim auto|kp|spoljni      režim ""KP popunjava"" (podrazumevano auto: po r62 = 3212)
      --datum yyyy-MM-dd[ HH:mm]   datum (i vreme) prijema (za ""datum u budućnosti""); podrazumevano: najnoviji fajl
      --en                         poruka na engleskom
      --izlaz <folder>             upisuje rezultat (txt + json) po slučaju
      --stanje <folder>            ponavljanja kroz niz poziva (isti prilog / isti tovarni list – bez novog izveštaja)
  rubrike <fajl>… [--ocr]          dijagnostika čitanja: vrsta strane, OCR, poravnanje i tekst svake rubrike
                                   (--ocr: PDF strane se iscrtavaju i čitaju OCR-om i kada imaju tekst)
  slike <fajl|folder>… [--izlaz f] JSON pročitanih rubrika + slika strane sa okvirima rubrika za svaki tovarni list
                                   (podrazumevano RezultatiFolder\yyyy-MM-dd)
  sablon-nacrt <fajl> --id ID [--strana N] [--naziv ""…""] [--ocr] [--izlaz folder]
                                   nacrt šablona novog obrasca iz uzorka: linije okvira, brojevi rubrika, rubrike,
                                   kvadratići i sidra (JSON + slika); posle provere kopirati u ObrasciFolder
  preuzmi <uid> [--odlazni] [--izlaz f]  čuva priloge jedne poruke (IMAP samo za čitanje)
  reset-stanje [--zadatak ""ime""] [--bez-zadatka]
                                   zaustavlja zadatak, pravi rezervnu kopiju stanja (stanje\arhiva\datum_vreme),
                                   briše spisak obrađenih mailova i keš (OCR, rezultati), postavlja nultu tačku i
                                   ponovo uključuje zadatak; pravila, šabloni, config, logovi i testovi ostaju
  nulta-tacka-arhiva [--suvo] [--max-min 60]
                                   v3.0.5: nulta tačka = poslednji UID pokriven arhivom (arhiva\izvestaj.txt i indeks.csv, bez IMAP-a);
                                   samo dok je zadatak obrade isključen; rezervna kopija stanja, red u nulta_tacka.txt
  --test-veze                      proverava pravila, šablon, OCR (tessdata), IMAP i SMTP, bez obrade
  --zastiti <vrednost>             DPAPI šifrovanje za config (pokrenuti pod servisnim nalogom)
  Word prilozi (.docx/.doc/.rtf) se pretvaraju u PDF preko Syncfusion DocIO; ključ je u Syncfusion.dpapi (Instaliraj.ps1)";

        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            AgentConfig cfg;
            try { cfg = AgentConfig.Ucitaj(); }
            catch (Exception ex) { Console.Error.WriteLine("Greška u config-u: " + ex); return 1; }
            var log = new Log(cfg.LogFolder, cfg.LogDana);
            Slanje.Dnevnik = log;   // v2.8.4: zaštita režima Test upisuje uklonjene primaoce u log
            // v3.0: SMTP postavlja samo agent (jezgro samo sastavlja; CimRadionica ne šalje). v3.0.3 (3.2): komanda „arhiviraj“ samo čita
            // sanduče – SMTP se ne inicijalizuje (transport ostaje „slanje nije podešeno“), pa ni greška ne može da pošalje mail
            if (!BezSlanja(args)) Slanje.Transport = SmtpSlanje.Posalji;
            Izvestaj.Potpis = PotpisIzvestaja.Iz(cfg);
            Baza.Ponude.Konekcija = cfg.Konekcija;   // v2.9.2: platilac preko broja ponude (samo čitanje dbo.Ponudba)
            // Syncfusion ključ (Word → PDF) iz Syncfusion.dpapi; greška se upisuje tek kada Word prilog treba pretvoriti
            try { Prilozi.WordUPdf.UcitajKljuc(); } catch { }

            try
            {
                var a = args.ToList();
                if (a.Contains("-h") || a.Contains("--help") || a.Contains("/?")) { Console.WriteLine(Pomoc); return 0; }
                if (a.Count > 0 && a[0] == "--zastiti")
                {
                    if (a.Count < 2) { Console.Error.WriteLine("Upotreba: --zastiti <vrednost>"); return 1; }
                    Console.WriteLine(AgentConfig.Zastiti(a[1]));
                    return 0;
                }
                if (a.Count > 0 && a[0] == "validate") return Validate(cfg, log, a.Skip(1).ToList());
                if (a.Count > 2 && a[0] == "iz-kesa")
                {
                    // iz-kesa <..._strane_....json> <ime fajla> [<json> <ime>…] [--interni] [--datum yyyy-MM-dd] [--izvestaj]:
                    // ponavlja prepoznavanje i proveru iz keša teksta strana; više parova = prilozi jednog maila
                    var pr = PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null);
                    var di = a.IndexOf("--datum");
                    var datum = di > 0 ? DateTime.ParseExact(a[di + 1], "yyyy-MM-dd", CultureInfo.InvariantCulture) : DateTime.Today;
                    if (di > 0) a.RemoveRange(di, 2);
                    var interni = a.Remove("--interni"); var ceo = a.Remove("--izvestaj");
                    var dokumenti = new List<Dokument>();
                    using (var c = new LokalniCitac(cfg, log, pr.Kp))
                        for (int i = 1; i + 1 < a.Count; i += 2)
                        {
                            var strane = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Citanje.Strana>>(File.ReadAllText(a[i], Encoding.UTF8));
                            if (!ceo) Console.WriteLine(c.Dijagnostika(strane, a[i + 1]));
                            var rp = c.Klasifikuj(new Prilozi.Prilog { Naziv = a[i + 1] }, strane);
                            Console.WriteLine("  " + a[i + 1] + ": " + string.Join(" | ", rp.StraneOpis));
                            foreach (var d in rp.Dokumenti) { d.Fajl = a[i + 1]; dokumenti.Add(d); }
                        }
                    var u = new UlazProvere { Dokumenti = dokumenti, InterniPosiljalac = interni, ReferentniDatum = datum, Prag = cfg.PragPouzdanosti };
                    var r = new Motor(pr, log).Proveri(u);
                    Console.WriteLine(Izvestaj.Dokumenti(r.Dokumenti));
                    if (ceo) Console.WriteLine(Izvestaj.Telo(r, pr, false));
                    else
                        foreach (var l in r.Listovi)
                            foreach (var n in l.Nalazi)
                                Console.WriteLine("  " + l.Oznaka + " | " + n.Tezina + " | " + n.Pravilo + " | r" + n.Rubrika + " | " + n.Vrednost + " | " + n.Detalj + " | takođe: " + string.Join(",", n.Takodje.Select(t => t.Pravilo)));
                    return 0;
                }
                if (a.Count > 1 && a[0] == "tabela")
                {
                    // tabela <fajl.xlsx|.xls|.docx>: dijagnostika tovarnog lista iz tabele – prepoznavanje, natpisi, rubrike
                    var pr = PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null);
                    var b = File.ReadAllBytes(a[1]);
                    var word = a[1].EndsWith(".docx", StringComparison.OrdinalIgnoreCase);
                    var listovi = word ? Prilozi.TabelarniCim.IzWorda(b) : Prilozi.TabelarniCim.IzExcela(b);
                    using (var c = new LokalniCitac(cfg, log, pr.Kp))
                        foreach (var cel in listovi.Where(x => x.Count > 0))
                        {
                            Console.WriteLine("=== list: " + cel.Count + " ćelija; tovarni list: " + Prilozi.TabelarniCim.JeTovarniList(cel, out var razlog) + " (" + razlog + ")");
                            if (a.Contains("--celije")) { foreach (var red in cel.GroupBy(x => x.Red).OrderBy(g => g.Key).Take(60)) Console.WriteLine("  " + (red.Key + 1) + ": " + string.Join(" | ", red.OrderBy(x => x.Kol).Select(x => x.Adresa + "=" + Regex.Replace(x.Tekst, @"\s+", " ")))); continue; }
                            var cim = Prilozi.TabelarniCim.Procitaj(cel, c.JeNatpisCelije, c.Pragovi, out var po);
                            foreach (var kv in po.OrderBy(k => Regex.Match(k.Key, @"\d+").Success ? int.Parse(Regex.Match(k.Key, @"\d+").Value) : 0))
                                Console.WriteLine("  r" + kv.Key.PadRight(6) + string.Join(" | ", kv.Value.Select(x => x.Adresa + "=" + Regex.Replace(x.Tekst, @"\s+", " "))));
                            Console.WriteLine("  natpisi: " + string.Join(" | ", cel.Where(x => c.JeNatpisCelije(x.Tekst)).Select(x => x.Adresa + "=" + Regex.Replace(x.Tekst, @"\s+", " "))));
                            Console.WriteLine("  → obrazac " + cim.Obrazac + ", r62 " + cim.R62.Zemlja + "/" + cim.R62.Stanica + "/" + cim.R62.Preduzece + "/" + cim.R62.Broj + ", r16 " + cim.R16.Mesto + " " + cim.R16.Datum
                                + ", r22 " + cim.R22.Tekst + ", r24 " + cim.R24.Tekst + ", r25 " + cim.R25.Tekst + ", r29 " + cim.R29.Mesto + " " + cim.R29.Datum + ", r30 CIM=" + cim.R30.Cim + " CUV=" + cim.R30.Cuv);
                        }
                    return 0;
                }
                if (a.Count > 1 && a[0] == "pdf-polja")
                {
                    // pdf-polja <fajl>: dijagnostika polja PDF obrasca (AcroForm) i anotacija po stranama
                    using (var doc = UglyToad.PdfPig.PdfDocument.Open(File.ReadAllBytes(a[1])))
                    {
                        Console.WriteLine("AcroForm: " + (doc.TryGetForm(out var form) && form != null ? form.Fields.Count + " polja" : "nema"));
                        if (form != null)
                            foreach (var f in form.Fields.Take(40))
                                Console.WriteLine("  " + f.GetType().Name + " | " + f.Information?.PartialName + " | str " + f.PageNumber + " | " + f.Bounds
                                    + (f is UglyToad.PdfPig.AcroForms.Fields.AcroTextField t ? " | „" + t.Value + "“" : "")
                                    + (f is UglyToad.PdfPig.AcroForms.Fields.AcroNonTerminalField nt ? " | deca " + nt.Children.Count : ""));
                        foreach (var p in doc.GetPages().Take(2))
                            foreach (var an in p.ExperimentalAccess.GetAnnotations().Take(10))
                                Console.WriteLine("  str " + p.Number + " anotacija " + an.Type + " " + an.Rectangle + " „" + an.Content + "“ ap=" + (an.HasNormalAppearance));
                    }
                    return 0;
                }
                if (a.Count > 0 && a[0] == "ponovna-obrada") return PonovnaObrada(cfg, log, a);
                if (a.Count > 0 && a[0] == "arhiviraj") return Arhiviraj(cfg, log, a);
                if (a.Count > 0 && a[0] == "nulta-tacka-arhiva") return NultaTackaArhive.Pokreni(cfg, log, a);
                if (a.Count > 1 && a[0] == "preuzmi")
                {
                    // preuzmi <uid> [--odlazni] [--izlaz folder]: čuva priloge jedne poruke (folder se otvara samo za čitanje; ništa se ne menja)
                    var ii = a.IndexOf("--izlaz"); var izlaz = ii > 0 ? a[ii + 1] : ".";
                    var odl = a.Contains("--odlazni");
                    using (var s = new Sanduce(cfg))
                    {
                        s.Povezi();
                        var f = odl ? s.Odlazni() : s.Dolazni();
                        f.Open(MailKit.FolderAccess.ReadOnly);
                        var m = f.GetMessage(new MailKit.UniqueId(uint.Parse(a[1])));
                        Directory.CreateDirectory(izlaz);
                        // v2.8.4: --eml – cela poruka sa zaglavljima (From/To/Cc, In-Reply-To) za validate i primaoce u Live režimu
                        if (a.Contains("--eml")) { var e = Path.Combine(izlaz, a[1] + ".eml"); m.WriteTo(e); Console.WriteLine(e); }
                        foreach (var (ime, sadrzaj) in Prilozi.Raspakivanje.PriloziMaila(m))
                        {
                            var put = Path.Combine(izlaz, string.Concat(ime.Split(Path.GetInvalidFileNameChars())));
                            File.WriteAllBytes(put, sadrzaj);
                            Console.WriteLine(put + " (" + sadrzaj.Length + " B)");
                        }
                    }
                    return 0;
                }
                if (a.Count > 1 && a[0] == "rubrike")
                {
                    var pr = PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null);
                    var uvekOcr = a.Remove("--ocr");
                    using (var c = new LokalniCitac(cfg, log, pr.Kp))
                        foreach (var f in a.Skip(1))
                            foreach (var pp in Prilozi.Raspakivanje.Raspakuj(Path.GetFileName(f), File.ReadAllBytes(f), new List<string>()))
                                Console.WriteLine("##### " + pp.Naziv + Environment.NewLine + c.Dijagnostika(pp, uvekOcr));
                    return 0;
                }
                if (a.Count > 1 && a[0] == "sablon-nacrt")
                {
                    // sablon-nacrt <fajl> --id <ID> [--strana N] [--naziv "..."] [--ocr] [--izlaz <folder>]
                    string Op(string ime, string d) { var i = a.IndexOf(ime); if (i < 0 || i + 1 >= a.Count) return d; var v = a[i + 1]; a.RemoveRange(i, 2); return v; }
                    var id = Op("--id", "NOVI"); var naziv = Op("--naziv", null); var strana = int.Parse(Op("--strana", "1")); var izlaz = Op("--izlaz", ".");
                    var mapaF = Op("--mapa", null);
                    var mapa = mapaF == null ? null : Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(mapaF, Encoding.UTF8));
                    var uvekOcr = a.Remove("--ocr");
                    var pr = PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null);
                    using (var c = new LokalniCitac(cfg, log, pr.Kp))
                    {
                        var f = a[1];
                        var pp = Prilozi.Raspakivanje.Raspakuj(Path.GetFileName(f), File.ReadAllBytes(f), new List<string>()).First();
                        var (json, png, nap) = c.NacrtSablona(pp, strana, id, naziv, uvekOcr, mapa);
                        Directory.CreateDirectory(izlaz);
                        var ime = Path.Combine(izlaz, Regex.Replace(((string)json["id"]).ToLowerInvariant(), @"[^\w\-]", "_") + (mapa == null ? ".nacrt" : ""));
                        File.WriteAllText(ime + ".json", json.ToString(Newtonsoft.Json.Formatting.Indented), new UTF8Encoding(false));
                        File.WriteAllBytes(ime + ".png", png);
                        foreach (var n in nap) Console.WriteLine(n);
                        Console.WriteLine("Nacrt: " + ime + ".json (slika " + ime + ".png). Proveriti, dopuniti i kopirati u " + cfg.ObrasciFolder);
                    }
                    return 0;
                }
                if (a.Count > 1 && a[0] == "slike")
                {
                    // slike <fajl|folder>… [--izlaz <folder>] [--ocr]: JSON pročitanih rubrika i slika okvira za svaki tovarni list
                    var ii = a.IndexOf("--izlaz");
                    var izlaz = ii > 0 ? a[ii + 1] : Path.Combine(cfg.RezultatiFolder, DateTime.Today.ToString("yyyy-MM-dd"));
                    if (ii > 0) a.RemoveRange(ii, 2);
                    var uvekOcr = a.Remove("--ocr");
                    var pr = PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null);
                    using (var c = new LokalniCitac(cfg, log, pr.Kp))
                        foreach (var arg in a.Skip(1))
                            foreach (var f in Directory.Exists(arg) ? Directory.GetFiles(arg).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray() : new[] { arg })
                                foreach (var pp in Prilozi.Raspakivanje.Raspakuj(Path.GetFileName(f), File.ReadAllBytes(f), new List<string>()).Where(x => x.Vrsta != Prilozi.VrstaPriloga.Excel))
                                    foreach (var poruka in c.SnimiSlike(pp, izlaz, uvekOcr)) Console.WriteLine(poruka);
                    Console.WriteLine("Rezultati: " + izlaz);
                    return 0;
                }
                if (a.Count > 0 && a[0] == "reset-potpun")
                {
                    // v2.8.2: potpun reset (stanje, keš, rezultati, logovi) + nulta tačka po UID-u; --suvo = samo spisak
                    var zi = a.IndexOf("--zadatak");
                    return ResetStanja.Potpun(cfg, log, zi > 0 && zi + 1 < a.Count ? a[zi + 1] : ResetStanja.PodrazumevaniZadatak, a.Contains("--bez-zadatka"), a.Contains("--suvo"));
                }
                if (a.Count > 0 && a[0] == "reset-stanje")
                {
                    var zi = a.IndexOf("--zadatak");
                    return ResetStanja.Pokreni(cfg, log, zi > 0 && zi + 1 < a.Count ? a[zi + 1] : ResetStanja.PodrazumevaniZadatak, a.Contains("--bez-zadatka"));
                }
                if (a.Contains("--test-veze")) return TestVeze(cfg, log);

                var dry = a.Contains("--dry-run");
                if (dry) log.Info("DRY-RUN: mail se ne šalje i stanje se ne pamti.");
                using (var mutex = new Mutex(false, @"Global\Saobracaj.Agent.CimProvera"))
                {
                    if (!mutex.WaitOne(0)) { log.Info("Druga instanca agenta već radi – izlaz."); return 0; }
                    try
                    {
                        // v2.9.1: jednokratni zahtev za ponovnu obradu (samo upis) – fajl „ponovna_obrada.zahtev“ („--od-uid 191“) u folderu
                        // agenta; izvršava se pod nalogom zadatka (DPAPI konekcija i lozinka sandučeta), pa se preimenuje u „.gotovo“
                        // v3.0 (tačka 5): jednokratno arhiviranje sandučeta – fajl „arhiviranje.zahtev“ („--od-uid 191“); izvršava se pod nalogom
                        // zadatka (DPAPI lozinka sandučeta), posle toga agent izlazi BEZ obrade pošte (agent je zaustavljen dok korisnik ne odluči)
                        var zahtevArhive = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "arhiviranje.zahtev");
                        if (!dry && File.Exists(zahtevArhive))
                        {
                            var uToku = Path.ChangeExtension(zahtevArhive, ".u_toku");
                            File.Move(zahtevArhive, uToku);
                            var arg = new List<string> { "arhiviraj" };
                            arg.AddRange(File.ReadAllText(uToku).Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries));
                            int rez;
                            try { rez = Arhiviraj(cfg, log, arg); }
                            catch (Exception ex) { log.Greska("Arhiviranje nije uspelo", ex); rez = 1; }
                            File.AppendAllText(uToku, Environment.NewLine + "# " + DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " – završeno, kod " + rez + " (izveštaj: arhiva\\izvestaj.txt, detalji u logu)" + Environment.NewLine);
                            var gotovo = Path.ChangeExtension(zahtevArhive, ".gotovo");
                            if (File.Exists(gotovo)) File.Delete(gotovo);
                            File.Move(uToku, gotovo);
                            return rez;
                        }
                        var zahtev = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ponovna_obrada.zahtev");
                        if (!dry && File.Exists(zahtev))
                        {
                            var uToku = Path.ChangeExtension(zahtev, ".u_toku");
                            File.Move(zahtev, uToku);
                            var arg = new List<string> { "ponovna-obrada" };
                            arg.AddRange(File.ReadAllText(uToku).Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries));
                            int rez;
                            try { rez = PonovnaObrada(cfg, log, arg); }
                            catch (Exception ex) { log.Greska("Ponovna obrada nije uspela", ex); rez = 1; }
                            if (rez == 2) { File.Move(uToku, zahtev); return new AgentRad(cfg, log, dry).Pokreni(); }   // čeka prava (OP_913) – ponovo u sledećem prolazu
                            File.AppendAllText(uToku, Environment.NewLine + "# " + DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " – završeno, kod " + rez + " (detalji u logu)" + Environment.NewLine);
                            var gotovo = Path.ChangeExtension(zahtev, ".gotovo");
                            if (File.Exists(gotovo)) File.Delete(gotovo);
                            File.Move(uToku, gotovo);
                        }
                        var i = a.IndexOf("--petlja");
                        if (i < 0) return new AgentRad(cfg, log, dry).Pokreni();
                        var min = i + 1 < a.Count && int.TryParse(a[i + 1], out var m) ? m : 2;
                        log.Info("Stalni rad: prolaz na svakih " + min + " min (" + cfg.Nalog + ", režim " + cfg.Rezim + ").");
                        while (true)
                        {
                            try { new AgentRad(AgentConfig.Ucitaj(), log, dry).Pokreni(); }
                            catch (Exception ex) { log.Greska("Prolaz nije uspeo", ex); }
                            Thread.Sleep(TimeSpan.FromMinutes(min));
                        }
                    }
                    finally { mutex.ReleaseMutex(); }
                }
            }
            catch (Exception ex)
            {
                log.Greska("Neočekivana greška", ex);
                return 1;
            }
        }

        // ------------------------------------------------------------------------------------------- arhiva (v3.0, tačka 5)

        /// <summary>v3.0.3 (3.2): komande bez slanja pošte – SMTP transport se ne postavlja (arhiviranje: samo IMAP čitanje i upis u arhivu).</summary>
        internal static bool BezSlanja(IList<string> a) => a != null && a.Count > 0 && (a[0] == "arhiviraj" || a[0] == "nulta-tacka-arhiva");

        /// <summary>
        /// v3.0.3 (3.4): popis foldera na serveru (samo čitanje: UID, veličina, BODYSTRUCTURE) – broj i veličina svih poruka, prilozi PRIMER,
        /// poslednji UID i rupe (UID-ovi na serveru od početne tačke kojih nema u arhivi).
        /// </summary>
        private static string PopisFoldera(MailKit.IMailFolder f, Arhiva ar, Func<MailKit.IMessageSummary, bool> zaArhivu)
        {
            var sve = f.Count == 0 ? new List<MailKit.IMessageSummary>() : f.Fetch(0, -1, new MailKit.FetchRequest(MailKit.MessageSummaryItems.UniqueId | MailKit.MessageSummaryItems.Size
                | MailKit.MessageSummaryItems.BodyStructure | MailKit.MessageSummaryItems.InternalDate)).ToList();
            string Mb(double b) => (b / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
            long primer = 0; int saPrimerom = 0;
            foreach (var s in sve)
            {
                var pr = (s.BodyParts ?? Enumerable.Empty<MailKit.BodyPartBasic>()).Where(b => (b.FileName ?? "").StartsWith("PRIMER", StringComparison.OrdinalIgnoreCase)).Sum(b => (long)b.Octets);
                if (pr > 0) { primer += pr; saPrimerom++; }
            }
            var arh = sve.Where(zaArhivu).ToList();
            var rupe = arh.Where(s => !File.Exists(ar.Putanja(f.FullName, f.UidValidity, s.UniqueId.Id))).Select(s => s.UniqueId.Id).ToList();
            return f.FullName + " (UIDVALIDITY " + f.UidValidity + "): na serveru " + sve.Count + " poruka, " + Mb(sve.Sum(s => (double)(s.Size ?? 0)))
                + (sve.Count > 0 ? ", UID " + sve.Min(s => s.UniqueId.Id) + "–" + sve.Max(s => s.UniqueId.Id) : "")
                + "; prilozi PRIMER: " + Mb(primer) + " u " + saPrimerom + " poruka"
                + "; za arhivu (od početne tačke): " + arh.Count + ", poslednji UID " + (arh.Count > 0 ? arh.Max(s => s.UniqueId.Id).ToString(CultureInfo.InvariantCulture) : "–")
                + "; rupe (nema u arhivi): " + (rupe.Count == 0 ? "nema" : rupe.Count + " – UID " + string.Join(", ", rupe.Take(30)) + (rupe.Count > 30 ? " …" : ""));
        }

        internal static string ArhivaKoren(AgentConfig cfg) => string.IsNullOrEmpty(cfg.ArhivaFolder) ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "arhiva") : cfg.ArhivaFolder;

        /// <summary>
        /// arhiviraj [--od-uid N]: sve poruke INBOX-a od UID-a N (podrazumevano nulta tačka) i foldera poslatih od istog vremena → arhiva\&lt;folder&gt;\&lt;UID&gt;.eml
        /// + indeks.csv. IMAP samo za čitanje; ništa se ne briše. Postojeće poruke u arhivi se preskaču. Izveštaj: arhiva\izvestaj.txt.
        /// </summary>
        private static int Arhiviraj(AgentConfig cfg, Log log, List<string> a)
        {
            string Op(string ime) { var i = a.IndexOf(ime); return i > 0 && i + 1 < a.Count ? a[i + 1] : null; }
            var stanje = Stanje.Ucitaj(cfg.StanjeFolder);
            var od = long.TryParse(Op("--od-uid") ?? "", out var o) ? o : Math.Max(1, stanje.NultaTackaUid);
            var ar = new Arhiva(ArhivaKoren(cfg));
            int novih = 0, postoji = 0;
            var popis = new List<string>();
            DateTimeOffset? odVreme = stanje.NultaTacka;
            using (var s = new Sanduce(cfg))
            {
                s.Povezi();
                var f = s.Dolazni();
                f.Open(MailKit.FolderAccess.ReadOnly);
                var uids = f.Search(MailKit.Search.SearchQuery.All).Where(u => u.Id >= od).OrderBy(u => u.Id).ToList();
                if (uids.Count > 0)
                {
                    var prvi = f.Fetch(new[] { uids[0] }, new MailKit.FetchRequest(MailKit.MessageSummaryItems.InternalDate)).FirstOrDefault()?.InternalDate;
                    if (prvi.HasValue && (!odVreme.HasValue || prvi.Value < odVreme.Value)) odVreme = prvi;
                }
                log.Info("Arhiviranje: " + f.FullName + " UID " + od + "+ (" + uids.Count + " poruka, UIDVALIDITY " + f.UidValidity + ") → " + ar.Koren);
                foreach (var u in uids)
                {
                    if (File.Exists(ar.Putanja(f.FullName, f.UidValidity, u.Id))) { postoji++; continue; }
                    if (ar.Sacuvaj(f.FullName, f.UidValidity, u.Id, f.GetMessage(u), t => log.Upozorenje(t))) novih++;
                }
                popis.Add(PopisFoldera(f, ar, s => s.UniqueId.Id >= od));
                if (cfg.CitajOdlazne)
                    try
                    {
                        var p = s.Odlazni();
                        p.Open(MailKit.FolderAccess.ReadOnly);
                        var sve = p.Search(MailKit.Search.SearchQuery.All).OrderBy(u => u.Id).ToList();
                        var datumi = sve.Count == 0 ? new Dictionary<MailKit.UniqueId, DateTimeOffset?>() : p.Fetch(sve, new MailKit.FetchRequest(MailKit.MessageSummaryItems.InternalDate)).ToDictionary(x => x.UniqueId, x => x.InternalDate);
                        var poslate = sve.Where(u => !odVreme.HasValue || (datumi.TryGetValue(u, out var d) && d.HasValue && d.Value >= odVreme.Value)).ToList();
                        log.Info("Arhiviranje: " + p.FullName + " (" + poslate.Count + " poruka od " + (odVreme?.ToString("dd.MM.yyyy HH:mm") ?? "početka") + ")");
                        foreach (var u in poslate)
                        {
                            if (File.Exists(ar.Putanja(p.FullName, p.UidValidity, u.Id))) { postoji++; continue; }
                            if (ar.Sacuvaj(p.FullName, p.UidValidity, u.Id, p.GetMessage(u), t => log.Upozorenje(t))) novih++;
                        }
                        var skup = new HashSet<uint>(poslate.Select(x => x.Id));
                        popis.Add(PopisFoldera(p, ar, s => skup.Contains(s.UniqueId.Id)));
                    }
                    catch (Exception ex) { log.Upozorenje("Arhiviranje: folder poslatih nije dostupan: " + ex.Message); }
            }
            var izv = "Arhiva " + ar.Koren + " – " + DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm zzz") + Environment.NewLine + "Novih: " + novih + ", već u arhivi: " + postoji + Environment.NewLine + ar.Izvestaj()
                      + "Sanduče (samo čitanje, bez slanja):" + Environment.NewLine + string.Join(Environment.NewLine, popis.Select(x => "  " + x)) + Environment.NewLine;
            File.WriteAllText(Path.Combine(ar.Koren, "izvestaj.txt"), izv, new UTF8Encoding(false));
            log.Info(izv.Replace(Environment.NewLine, " | "));
            Console.WriteLine(izv);
            return 0;
        }

        // ------------------------------------------------------------------------------------------- ponovna obrada (v2.9.1)

        /// <summary>
        /// v2.9.1 (tačka 7.2): ponovna-obrada --od-uid N [--do-uid M] [--folder F]: mailovi iz sandučeta (samo čitanje) od UID-a N redom
        /// prijema se preuzimaju kao .eml (bez sopstvenih izveštaja agenta i mailova bez priloga) i obrađuju kao validate --upis, sa
        /// privremenim folderom stanja (dosije i ponavljanja od nule). Ne šalje se nijedan mail; stanje agenta (stanje.json,
        /// posiljke.json, ponavljanja) se ne menja; keš OCR-a se koristi kao i inače.
        /// </summary>
        private static int PonovnaObrada(AgentConfig cfg, Log log, List<string> a)
        {
            string Op(string ime) { var i = a.IndexOf(ime); return i > 0 && i + 1 < a.Count ? a[i + 1] : null; }
            var od = long.Parse(Op("--od-uid") ?? "0");
            var doUid = long.TryParse(Op("--do-uid") ?? "", out var du) ? du : long.MaxValue;
            if (od <= 0) { Console.Error.WriteLine("Upotreba: ponovna-obrada --od-uid N [--do-uid M]"); return 1; }
            if (!cfg.UpisUBazu || string.IsNullOrEmpty(cfg.Konekcija)) { Console.Error.WriteLine("ponovna-obrada: upis u bazu nije podešen (UpisUBazu / Konekcija.dpapi)."); return 1; }
            // v2.9.1: bez prava na planiranje.tmpNHM (OP_913) NHM bi ostao NULL – obrada čeka (kod 2, zahtev ostaje za sledeći prolaz)
            using (var k = Baza.UpisUBazu.Otvori(cfg.Konekcija))
            {
                try { using (var cmd = new System.Data.SqlClient.SqlCommand("SELECT TOP 1 ID FROM planiranje.tmpNHM", k)) cmd.ExecuteScalar(); }
                catch (System.Data.SqlClient.SqlException ex) { log.Upozorenje("Ponovna obrada čeka: nema pristupa planiranje.tmpNHM (pokrenuti OP_913_tmp_Prava.sql) – " + ex.Message); return 2; }
                // v2.9.2: bez prava na dbo.Ponudba (OP_913) platilac preko broja ponude bi ostao NULL – obrada čeka
                try { using (var cmd = new System.Data.SqlClient.SqlCommand("SELECT TOP 0 PonStPon, PonPart FROM dbo.Ponudba", k)) cmd.ExecuteScalar(); }
                catch (System.Data.SqlClient.SqlException ex) { log.Upozorenje("Ponovna obrada čeka: nema pristupa dbo.Ponudba (pokrenuti OP_913_tmp_Prava.sql) – " + ex.Message); return 2; }
                // --obrisi-podatke: samo PODACI iz tmp tabela (kao OP_915_tmp_BrisanjePodataka.sql); tmpSinonimi i tmpNHM ostaju
                // v3.0.3 (3.5): posle OP_919 samo redovi agenta (Izvor = 'Agent') – podaci alata CimRadionica ostaju
                bool saIzvorom;
                using (var chk = new System.Data.SqlClient.SqlCommand("SELECT COL_LENGTH('planiranje.tmpJobs', 'Izvor')", k)) { var d0 = chk.ExecuteScalar(); saIzvorom = d0 != null && !(d0 is DBNull); }
                if (a.Contains("--obrisi-podatke") && saIzvorom)
                {
                    using (var tx = k.BeginTransaction())
                    using (var cmd = new System.Data.SqlClient.SqlCommand(Saobracaj.Agent.CimProvera.Radionica.RadniProstor.ObrisiSql, k, tx) { CommandTimeout = 300 })
                    {
                        cmd.Parameters.AddWithValue("@izvor", "Agent");
                        cmd.ExecuteNonQuery();
                        tx.Commit();
                    }
                    log.Info("Ponovna obrada: podaci agenta iz tmp tabela obrisani (DELETE, Izvor = 'Agent'; podaci alata, tmpSinonimi i tmpNHM ostaju).");
                }
                else if (a.Contains("--obrisi-podatke"))
                {
                    using (var tx = k.BeginTransaction())
                    using (var cmd = new System.Data.SqlClient.SqlCommand(@"IF OBJECT_ID(N'planiranje.tmpJobsDogadjaj', N'U') IS NOT NULL DELETE FROM planiranje.tmpJobsDogadjaj;
DELETE FROM planiranje.tmpJobsIstorija; DELETE FROM planiranje.tmpJobsPolje; DELETE FROM planiranje.tmpTeretnicaStavke;
UPDATE planiranje.tmpDokument SET JobID = NULL, TeretnicaID = NULL; DELETE FROM planiranje.tmpTeretnica; DELETE FROM planiranje.tmpJobs; DELETE FROM planiranje.tmpDokument;", k, tx) { CommandTimeout = 300 })
                    {
                        cmd.ExecuteNonQuery();
                        tx.Commit();
                    }
                    log.Info("Ponovna obrada: podaci iz tmp tabela obrisani (DELETE; tmpSinonimi i tmpNHM ostaju).");
                }
            }
            var radni = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ponovna_obrada", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            var eml = Path.Combine(radni, "eml"); var stanje = Path.Combine(radni, "stanje");
            Directory.CreateDirectory(eml); Directory.CreateDirectory(stanje);
            log.Info("Ponovna obrada (samo upis): UID " + od + (doUid < long.MaxValue ? "–" + doUid : "+") + ", radni folder " + radni + "; mail se ne šalje, stanje agenta se ne menja.");
            var poruke = new List<(DateTimeOffset vreme, string fajl)>();
            // nulta tačka (vreme reseta) iz stanja agenta – samo čitanje; za folder poslatih (odlazni) poruke posle tog vremena
            var nulta = Stanje.Ucitaj(cfg.StanjeFolder).NultaTacka;
            // v3.0 (tačka 5): poruka iz sandučeta ili iz arhive – isti filter i isti zapis
            void Dodaj(string folder, bool odlazni, long uid, MimeKit.MimeMessage m)
            {
                var razlog = Slanje.RazlogPreskakanja(m, cfg, !odlazni);
                if (razlog != null) { log.Info("  " + folder + " UID " + uid + ": preskočeno – " + razlog); return; }
                if (!Prilozi.Raspakivanje.PriloziMaila(m).Any()) { log.Info("  " + folder + " UID " + uid + ": bez priloga – preskočeno"); return; }
                var put = Path.Combine(eml, string.Concat(folder.Replace('/', '_').Split(Path.GetInvalidFileNameChars())) + "__" + uid + ".eml");
                m.WriteTo(put);
                poruke.Add((m.Date, put));
            }
            if (string.Equals(Op("--izvor"), "arhiva", StringComparison.OrdinalIgnoreCase))
            {
                var ar = new Arhiva(ArhivaKoren(cfg));
                var zapisi = ar.Procitaj();
                if (zapisi.Count == 0) { Console.Error.WriteLine("ponovna-obrada --izvor arhiva: arhiva je prazna (" + ar.Koren + ")."); return 1; }
                log.Info("Ponovna obrada iz arhive " + ar.Koren + " (" + zapisi.Count + " poruka u indeksu).");
                foreach (var z in ar.Poruke(cfg.DolazniFolder, od, doUid)) Dodaj(z.Folder, false, z.Uid, MimeKit.MimeMessage.Load(z.Fajl));
                if (cfg.CitajOdlazne && nulta.HasValue)
                    foreach (var fold in zapisi.Select(z => z.Folder).Distinct(StringComparer.OrdinalIgnoreCase).Where(x => !string.Equals(x, cfg.DolazniFolder, StringComparison.OrdinalIgnoreCase)))
                        foreach (var z in ar.Poruke(fold, 0).Where(z => DateTimeOffset.TryParse(z.Datum, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) && d >= nulta.Value))
                            Dodaj(z.Folder, true, z.Uid, MimeKit.MimeMessage.Load(z.Fajl));
            }
            else
            using (var s = new Sanduce(cfg))
            {
                s.Povezi();
                var folderi = new List<(MailKit.IMailFolder f, bool odlazni)> { (s.Dolazni(), false) };
                if (cfg.CitajOdlazne && nulta.HasValue) try { folderi.Add((s.Odlazni(), true)); } catch (Exception ex) { log.Upozorenje("Ponovna obrada: folder poslatih nije dostupan: " + ex.Message); }
                foreach (var (f, odlazni) in folderi)
                {
                    f.Open(MailKit.FolderAccess.ReadOnly);
                    var uids = f.Search(MailKit.Search.SearchQuery.All).Where(u => odlazni || (u.Id >= od && u.Id <= doUid)).OrderBy(u => u.Id).ToList();
                    if (odlazni)
                    {
                        var datumi = f.Fetch(uids, new MailKit.FetchRequest(MailKit.MessageSummaryItems.InternalDate)).ToDictionary(x => x.UniqueId, x => x.InternalDate);
                        uids = uids.Where(u => datumi.TryGetValue(u, out var d) && d.HasValue && d.Value >= nulta.Value).ToList();
                    }
                    foreach (var u in uids) Dodaj(f.FullName, odlazni, u.Id, f.GetMessage(u));
                }
            }
            log.Info("Ponovna obrada: " + poruke.Count + " mailova sa prilozima.");
            if (poruke.Count == 0) return 0;
            // v2.9.1: obrada u delovima od po 20 mailova, svaki deo u posebnom procesu (32-bitni proces sa OCR-om ostaje bez memorije posle
            // ~70 mailova – „Out of memory“, proces se gasi); stanje (dosije, ponavljanja) je zajedničko za sve delove, redosled prijema ostaje
            var redom = poruke.OrderBy(x => x.vreme).Select(x => x.fajl).ToList();
            var exe = System.Reflection.Assembly.GetExecutingAssembly().Location;
            const int Deo = 20;
            int greske = 0;
            for (int i = 0; i < redom.Count; i += Deo)
            {
                var deo = redom.Skip(i).Take(Deo).ToList();
                var arg = new List<string> { "validate" };
                arg.AddRange(deo);
                arg.AddRange(new[] { "--upis", "--stanje", stanje, "--izlaz", Path.Combine(radni, "izvestaji") });
                var psi = new System.Diagnostics.ProcessStartInfo(exe, string.Join(" ", arg.Select(x => "\"" + x.Replace("\"", "\\\"") + "\"")))
                {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                };
                log.Info("Ponovna obrada: deo " + (i / Deo + 1) + "/" + ((redom.Count + Deo - 1) / Deo) + " – mailovi " + (i + 1) + "–" + (i + deo.Count) + " od " + redom.Count);
                using (var pr = System.Diagnostics.Process.Start(psi))
                {
                    // izlaz se čita (ne čuva) da proces ne zastane na punom baferu; log piše sam proces
                    pr.OutputDataReceived += (s, e) => { }; pr.ErrorDataReceived += (s, e) => { };
                    pr.BeginOutputReadLine(); pr.BeginErrorReadLine();
                    pr.WaitForExit();
                    if (pr.ExitCode != 0) { greske++; log.Upozorenje("Ponovna obrada: deo " + (i / Deo + 1) + " završen sa kodom " + pr.ExitCode); }
                }
                File.AppendAllText(Path.Combine(radni, "obradjeno.txt"), string.Join(Environment.NewLine, deo.Select(Path.GetFileName)) + Environment.NewLine);
            }
            log.Info("Ponovna obrada: završeno " + redom.Count + " mailova" + (greske > 0 ? ", delova sa greškom " + greske : "") + ".");
            return greske == 0 ? 0 : 1;
        }

        // ------------------------------------------------------------------------------------------- validate

        private static int Validate(AgentConfig cfg, Log log, List<string> a)
        {
            string Opcija(string ime) { var i = a.IndexOf(ime); if (i < 0 || i + 1 >= a.Count) return null; var v = a[i + 1]; a.RemoveAt(i + 1); a.RemoveAt(i); return v; }
            bool Zastavica(string ime) { var i = a.IndexOf(ime); if (i < 0) return false; a.RemoveAt(i); return true; }

            var rezim = Opcija("--rezim") ?? "auto";
            var datumArg = Opcija("--datum");
            var izlaz = Opcija("--izlaz");
            var posiljalac = Opcija("--posiljalac");
            // v2.8.1: naslov i telo maila (za jezik kod opštih domena: gmail.com…)
            var naslovMaila = Opcija("--naslov");
            var teloMaila = Opcija("--telo");
            var istorija = Opcija("--istorija");
            // v2.8.4 (6): stanje ponavljanja (isti prilog / isti tovarni list) kroz niz validate poziva – isto kao u radu nad sandučetom
            var stanjePon = Opcija("--stanje");
            var ponavljanja = stanjePon != null ? Ponavljanja.Ucitaj(stanjePon) : null;
            var en = Zastavica("--en");
            var sr = Zastavica("--sr");
            var podfolderi = Zastavica("--podfolderi");
            // v2.9.0: --upis – upis u planiranje.tmp* (konekcija iz config-a / CIMPROVERA_KONEKCIJA); --meta <json> – podaci o mailu po slučaju
            var upisArg = Zastavica("--upis");
            var metaF = Opcija("--meta");
            // v2.9.1: --prefiks planiranje.tmpProba – lokalna proba upisa u kopije tmp tabela (prave tmp tabele se ne diraju)
            var prefiks = Opcija("--prefiks") ?? Baza.UpisUBazu.PodrazumevaniPrefiks;
            if (!prefiks.StartsWith(Baza.UpisUBazu.PodrazumevaniPrefiks, StringComparison.Ordinal)) { Console.Error.WriteLine("--prefiks mora da počinje sa planiranje.tmp"); return 1; }
            var meta = metaF == null ? new Dictionary<string, Newtonsoft.Json.Linq.JObject>(StringComparer.OrdinalIgnoreCase)
                : Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(metaF, Encoding.UTF8)).Properties().ToDictionary(x => x.Name, x => (Newtonsoft.Json.Linq.JObject)x.Value, StringComparer.OrdinalIgnoreCase);
            Baza.UpisUBazu upis = null;
            if (upisArg && string.IsNullOrEmpty(cfg.Konekcija)) { Console.Error.WriteLine("--upis: konekcija nije podešena (Konekcija / Konekcija.dpapi / CIMPROVERA_KONEKCIJA)."); return 1; }
            // v2.8.1: test podaci „<ime>.strane.json“ samo uz izričitu zastavicu (u radu nad sandučetom nikad)
            Prilozi.Raspakivanje.DozvoliProcitaneStrane = Zastavica("--test-strane");
            if (a.Count == 0) { Console.Error.WriteLine("Upotreba: validate <fajl|folder>… (vidi --help)"); return 1; }

            var slucajevi = new List<(string ime, List<string> fajlovi)>();
            foreach (var p in a)
            {
                if (File.Exists(p)) slucajevi.Add((Path.GetFileName(p), new List<string> { p }));
                else if (Directory.Exists(p))
                {
                    if (podfolderi)
                        foreach (var d in Directory.GetDirectories(p).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                            slucajevi.Add((Path.GetFileName(d), Directory.GetFiles(d, "*", SearchOption.AllDirectories).ToList()));
                    else slucajevi.Add((Path.GetFileName(p.TrimEnd('\\', '/')), Directory.GetFiles(p, "*", SearchOption.AllDirectories).ToList()));
                }
                else { Console.Error.WriteLine("Ne postoji: " + p); return 1; }
            }

            // jezik: --en / --sr, inače po pošiljaocu (--posiljalac) kao u radu nad sandučetom (v2.8)
            if (!en && !sr && posiljalac != null) en = Obrada.EngleskiZaPosiljaoca(posiljalac, cfg.JeInterni, naslovMaila, teloMaila);
            var enZadat = en; var posiljalac0 = posiljalac; var naslov0 = naslovMaila;
            var pravila = PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, log);
            using var citac = new LokalniCitac(cfg, log, pravila.Kp);
            var obrada = new Obrada(pravila, citac, log, cfg.PragPouzdanosti);
            // v2.8.1: CIT 7 bez lista u mailu – ranije obrađeni listovi iz zadatog foldera stanja (listovi.json)
            if (istorija != null) obrada.RanijiListovi = () => IstorijaListova.Ucitaj(istorija);
            int ukupnoGresaka = 0;
            void KrajUpisa()
            {
                if (upis == null) return;
                foreach (var x in upis.ZaSifarnik) { Console.WriteLine("ZA ŠIFARNIK: " + x); log.Info("Za šifarnik: " + x); }
                foreach (var kv in upis.KodoviBezSifre) Console.WriteLine("SRB KOD BEZ ŠIFRE: " + kv.Key + ";" + kv.Value);
                if (izlaz != null) try { Baza.UpisUBazu.SacuvajPredlogKodova(upis.KodoviBezSifre, Path.Combine(izlaz, "predlog_kodova_stanica.csv")); } catch { }
            }

            foreach (var (ime, fajlovi) in slucajevi)
            {
                var koren = fajlovi.Count == 1 ? Path.GetDirectoryName(fajlovi[0]) : ZajednickiKoren(fajlovi);
                // v2.8.4: ceo mail (.eml): prilozi, pošiljalac, naslov, datum i zaglavlja (primaoci u Live režimu, nit za dosije)
                MimeKit.MimeMessage ulazniMail = fajlovi.Count == 1 && fajlovi[0].EndsWith(".eml", StringComparison.OrdinalIgnoreCase) ? MimeKit.MimeMessage.Load(fajlovi[0]) : null;
                var prilozi = ulazniMail != null ? Prilozi.Raspakivanje.PriloziMaila(ulazniMail).ToList()
                    : fajlovi.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).Select(f => (ime: RelativnaPutanja(koren, f), sadrzaj: File.ReadAllBytes(f))).ToList();
                meta.TryGetValue(ime, out var mt);
                // v2.9.1: ponovna obrada – „<folder>__<uid>.eml“ → UID i sanduče kao u radu nad sandučetom
                var uidIme = System.Text.RegularExpressions.Regex.Match(ime, @"^(?<f>.+?)__(?<u>\d+)\.eml$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                long uidMaila = uidIme.Success ? long.Parse(uidIme.Groups["u"].Value) : mt?["uid"] != null ? (long)mt["uid"] : 0;
                var sanduceMaila = uidIme.Success ? cfg.Nalog + "|" + uidIme.Groups["f"].Value.Replace('_', '/') : "validate";
                posiljalac = posiljalac0 ?? ulazniMail?.From.Mailboxes.FirstOrDefault()?.Address ?? (string)mt?["od"];
                naslovMaila = naslov0 ?? ulazniMail?.Subject ?? (string)mt?["naslov"];
                en = enZadat || (!sr && ulazniMail != null && posiljalac != null && Obrada.EngleskiZaPosiljaoca(posiljalac, cfg.JeInterni, naslovMaila, ulazniMail.TextBody));
                var datum = datumArg != null ? DateTime.ParseExact(datumArg, new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm" }, CultureInfo.InvariantCulture, DateTimeStyles.None)
                    : ulazniMail != null ? ulazniMail.Date.LocalDateTime
                    : mt?["vreme"] != null ? DateTime.ParseExact((string)mt["vreme"], "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                    : fajlovi.Select(File.GetLastWriteTime).DefaultIfEmpty(DateTime.Today).Max();
                // v2.9.0: upis posle obrade slučaja (kao u radu nad sandučetom: posle provere i izveštaja)
                void Upisi(RezultatProvere rr, bool ponovljeni = false)
                {
                    if (!upisArg || (rr == null && !ponovljeni)) return;
                    try
                    {
                        upis = upis ?? new Baza.UpisUBazu(cfg.Konekcija, log, pravila.Kp) { Prefiks = prefiks };
                        var teloU = ulazniMail != null ? ObradaPoruke.TeloMaila(ulazniMail) : teloMaila ?? (string)mt?["telo"] ?? "";
                        var domen = (posiljalac ?? "").Contains("@") ? posiljalac.Substring(posiljalac.LastIndexOf('@') + 1) : "";
                        var iu = upis.Upisi(new Baza.MailZaUpis
                        {
                            MessageId = ulazniMail?.MessageId ?? (string)mt?["messageId"] ?? "validate:" + ime, Uid = uidMaila, Sanduce = sanduceMaila, Posiljalac = posiljalac ?? "",
                            Naslov = naslovMaila ?? ime, Primljeno = datum, Telo = teloU,
                            Prilozi = prilozi, Provera = rr, Dosije = obrada.PoslednjaPosiljka,
                            // v3.0 (2.10): događaji iz teksta maila – isto kao u agentu
                            Dogadjaji = Radionica.IzvlacenjeDogadjaja.Izvuci(naslovMaila ?? ime, teloU, new DateTimeOffset(datum), domen),
                        });
                        log.Info("  " + ime + ": upis u bazu – " + iu);
                        Console.WriteLine("Upis u bazu: " + iu);
                    }
                    catch (Exception ex) { log.Greska("  " + ime + ": upis u bazu nije uspeo (rollback)", ex); Console.WriteLine("Upis u bazu NIJE USPEO: " + ex.Message); }
                }
                var primaociLive = ulazniMail != null ? PrimaociLive.Iz(ulazniMail, cfg) : PrimaociLive.Iz(posiljalac, null, null, cfg.Nalog, cfg.ProsledjivanjeNaAgenti);

                log.Info("=== " + ime + " (" + prilozi.Count + " fajlova, datum prijema " + datum.ToString("dd.MM.yyyy") + ", režim " + rezim + ")");
                var oznakaPon = uidMaila > 0 ? "UID " + uidMaila : ime + " (" + datum.ToString("dd.MM. HH:mm") + ")";
                if (ponavljanja?.IstiPrilozi(prilozi) is Ponavljanja.Zapis istiP)
                {
                    var t = "################ " + ime + Environment.NewLine + "BEZ ODGOVORA – isti prilog kao " + istiP.Oznaka + " (" + istiP.Ishod + ") – već obrađeno; ništa se ne šalje.";
                    Console.WriteLine(t);
                    Upisi(null, true);   // v2.9.1 (5.7): prijem i Napomena posla
                    if (izlaz != null) { Directory.CreateDirectory(izlaz); File.WriteAllText(Path.Combine(izlaz, ime + ".txt"), t, Encoding.UTF8); }
                    continue;
                }
                obrada.NaslovMaila = naslovMaila ?? ime;
                // v2.8.4 (8): dosije pošiljke kroz niz validate poziva (isti folder stanja kao --stanje)
                if (stanjePon != null)
                {
                    obrada.Dosije = Dosije.Ucitaj(stanjePon);
                    obrada.MailDosijea = new MailZaDosije
                    {
                        Oznaka = oznakaPon, MessageId = ulazniMail?.MessageId ?? "", Naslov = naslovMaila ?? ime, Posiljalac = posiljalac ?? "", Vreme = datum,
                        Prethodni = ulazniMail == null ? new List<string>() : ulazniMail.References.Concat(string.IsNullOrEmpty(ulazniMail.InReplyTo) ? new string[0] : new[] { ulazniMail.InReplyTo }).Distinct().ToList(),
                    };
                }
                var ishod = obrada.Proveri(prilozi, posiljalac, posiljalac != null && cfg.JeInterni(posiljalac), datum, en, rezim);
                var rPuni = ishod.Provera;
                if (Environment.GetEnvironmentVariable("CIMPROVERA_DBG") == "1")
                    foreach (var l in rPuni.Listovi.Where(x => x.Cim != null))
                        Console.WriteLine("[dbg] ključ " + Ponavljanja.KljucLista(l.Cim) + " otisak " + Ponavljanja.Otisak(l.Cim) + "\n" + Ponavljanja.SadrzajOtiska(l.Cim));
                if (ponavljanja != null && rPuni.ImaTovarniList && ponavljanja.IstiListovi(rPuni) is Ponavljanja.ZapisLista istiL)
                {
                    var t = "################ " + ime + Environment.NewLine + "Dokumenti:" + Environment.NewLine + Izvestaj.Dokumenti(rPuni.Dokumenti) + Environment.NewLine
                            + "BEZ NOVOG IZVEŠTAJA – isti tovarni list kao " + istiL.Oznaka + ", sadržaj rubrika nepromenjen – već prijavljeno.";
                    Console.WriteLine(t);
                    if (izlaz != null) { Directory.CreateDirectory(izlaz); File.WriteAllText(Path.Combine(izlaz, ime + ".txt"), t, Encoding.UTF8); }
                    ponavljanja.ZapamtiPriloge(prilozi, oznakaPon, "isti tovarni list kao " + istiL.Oznaka, datum); ponavljanja.Sacuvaj();
                    obrada.ZapamtiUDosije(rPuni);
                    Upisi(rPuni);
                    continue;
                }
                if (ponavljanja != null)
                {
                    var bez = !rPuni.ImaTovarniList;
                    ponavljanja.ZapamtiPriloge(prilozi, oznakaPon, bez ? (ishod.NisuProcitani.Count + ishod.Neprepoznati.Count + ishod.NisaPouzdanost.Count > 0 ? "prilozi nisu pročitani – interna ručna provera" : "nema CIM/CUV – bez odgovora") : "izveštaj poslat", datum);
                    if (!bez) ponavljanja.ZapamtiListove(rPuni, oznakaPon, datum);
                    obrada.ZapamtiUDosije(rPuni);
                    ponavljanja.Sacuvaj();
                }
                // v2.8: isto kao u radu nad sandučetom – interni izveštaj (KP tovarni list / ručna provera) ili izveštaj za pošiljaoca bez nalaza „samo interno“
                var razlogInt = Izvestaj.RazlogInternog(rPuni, pravila);
                var r = razlogInt == null ? Izvestaj.ZaPosiljaoca(rPuni, cfg.Rezim, posiljalac != null && cfg.JeInterni(posiljalac)) : rPuni;
                var uvodV = razlogInt != null ? Izvestaj.UvodInternogKratak(razlogInt, rPuni, posiljalac, naslovMaila ?? ime, datum) : null;
                var en0 = en; en = razlogInt != null ? false : en;

                var sb = new StringBuilder();
                sb.AppendLine("################ " + ime);
                // v2.8.4 (10): primaoci u Live režimu (samo priprema; Test šalje samo na IzvestajAdresa)
                if (obrada.PoslednjaPosiljka != null) sb.AppendLine("Dosije pošiljke: " + string.Join(", ", obrada.PoslednjaPosiljka.Mailovi) + " (" + obrada.RazlogVeze + ")");
                sb.AppendLine("Primaoci u Live režimu: " + (razlogInt != null ? "samo interno (" + string.Join(", ", cfg.KopijaNa.DefaultIfEmpty(cfg.UpozorenjaNa)) + ")" : primaociLive.Opis()) + (razlogInt == null && naslovMaila != null ? "; naslov: " + PrimaociLive.Naslov(naslovMaila, en, r.BrojGresaka > 0) : ""));
                sb.AppendLine("Dokumenti:");
                sb.AppendLine(Izvestaj.Dokumenti(r.Dokumenti));
                foreach (var x in ishod.Preskoceno) sb.AppendLine("  (preskočeno) " + x);
                foreach (var x in ishod.NisuProcitani) sb.AppendLine("  (NIJE PROČITANO) " + x);
                sb.AppendLine();
                var teloV = ulazniMail != null ? ObradaPoruke.TeloMaila(ulazniMail) : teloMaila ?? (string)mt?["telo"] ?? "";
                if (!r.ImaTovarniList && ishod.NisuProcitani.Count + ishod.Neprepoznati.Count + ishod.NisaPouzdanost.Count > 0 && !Obrada.MozdaCim(ishod, naslovMaila, teloV))
                    sb.AppendLine("BEZ ODGOVORA (v2.9.2, 3.2) – prilozi nisu pročitani (" + string.Join("; ", ishod.NisuProcitani.Concat(ishod.NisaPouzdanost)) + "), a ni strana ne liči na CIM/CUV ni mail ne pominje CIM/CUV/tovarni list – samo log i tmpDokument.");
                else if (!r.ImaTovarniList && ishod.NisuProcitani.Count + ishod.Neprepoznati.Count + ishod.NisaPouzdanost.Count > 0)
                    sb.AppendLine("Tovarni list nije pronađen među pročitanim prilozima, ali prilozi nisu pouzdano pročitani (" + string.Join("; ", ishod.NisuProcitani.Concat(ishod.NisaPouzdanost)) + ") – rezultat nije konačan (interna ručna provera, ništa pošiljaocu).");
                // v2.8.4 (1): mail bez tovarnog lista – nikakav odgovor (ni interni), samo red u logu
                else if (!r.ImaTovarniList) sb.AppendLine("BEZ ODGOVORA – " + Izvestaj.LogBezTovarnogLista(r.Dokumenti));
                else
                {
                    sb.AppendLine("Režim KP: " + string.Join(", ", r.Listovi.Select(l => l.KpRezim ? "da" : "ne").Distinct()));
                    sb.AppendLine("Predmet: " + Izvestaj.Naslov(r, pravila, en));
                    sb.AppendLine();
                    sb.AppendLine(Izvestaj.Telo(r, pravila, en, uvodV, ishod.NisuProcitani, null, razlogInt != null));
                }
                if (r.GreskePravila.Count > 0) sb.AppendLine("Pravila sa greškom u izvršavanju: " + string.Join("; ", r.GreskePravila));
                // v2.8.1: CIT 7 – poseban interni izveštaj
                string cit7Tekst = null;
                if (rPuni.Cit7.Count > 0)
                {
                    cit7Tekst = Cit7Provera.Tekst(rPuni.Cit7, pravila, "[TEST] Interni izveštaj – ne ide pošiljaocu" + (posiljalac != null ? " (" + posiljalac + ")" : "") + ".");
                    sb.AppendLine().AppendLine("================ " + Cit7Provera.Naslov(rPuni.Cit7, naslovMaila ?? ime)).AppendLine(cit7Tekst);
                }
                Console.WriteLine(sb.ToString());
                Upisi(rPuni);
                ukupnoGresaka += r.BrojGresaka;

                if (izlaz != null)
                {
                    Directory.CreateDirectory(izlaz);
                    File.WriteAllText(Path.Combine(izlaz, ime + ".txt"), sb.ToString(), Encoding.UTF8);
                    if (r.ImaTovarniList)
                    {
                        File.WriteAllText(Path.Combine(izlaz, ime + ".html"), "<!doctype html><meta charset=\"utf-8\"><title>" + System.Net.WebUtility.HtmlEncode(Izvestaj.Naslov(r, pravila, en))
                            + "</title>" + Izvestaj.Html(r, pravila, en, uvodV, ishod.NisuProcitani, null, razlogInt != null), Encoding.UTF8);
                        // PDF primer (B2) + slike strana (PNG, 150 dpi) u izlaz\primeri
                        var primer = r.BrojGresaka + r.BrojUpozorenja > 0 ? PrimerPdf.Napravi(r, pravila, en, prilozi) : null;
                        if (primer != null)
                        {
                            var pd = Path.Combine(izlaz, "primeri"); Directory.CreateDirectory(pd);
                            if (primer.Napravljen)
                            {
                                File.WriteAllBytes(Path.Combine(pd, primer.Ime), primer.Pdf);
                                for (int si = 0; si < primer.Strane.Count; si++)
                                    File.WriteAllBytes(Path.Combine(pd, Path.GetFileNameWithoutExtension(primer.Ime) + "_str" + (si + 1) + ".png"), primer.Strane[si]);
                                // v2.8.1: šta je upisano u koju rubriku (provera izgleda)
                                File.WriteAllLines(Path.Combine(pd, Path.GetFileNameWithoutExtension(primer.Ime) + "_upisi.txt"), primer.Upisi.Select(x => x.ToString()), Encoding.UTF8);
                            }
                            Console.WriteLine("PDF primer: " + (primer.Napravljen ? primer.Ime + " (" + primer.Pdf.Length / 1024 + " KB, strana " + primer.Strane.Count + "; "
                                + string.Join("; ", primer.PoListu.Select(x => x.list + ": ispravki " + x.ispravki + ", napomena " + x.napomena + ", bez rubrike na strani " + x.bezRubrike)) + ")"
                                : "nije napravljen – " + primer.Razlog + (primer.Greska != null ? " (" + primer.Greska + ")" : "")));
                        }
                        // isti mail kao što ga agent šalje (multipart/alternative, UTF-8; sa PDF primerom kao prilogom) – otvara se u Outlook-u (.eml)
                        var pasus = Izvestaj.PasusPrimera(primer, en, razlogInt != null);
                        var mail = Slanje.Nova(cfg, new[] { cfg.IzvestajAdresa }, null, Izvestaj.Naslov(r, pravila, en),
                            Izvestaj.Telo(r, pravila, en, uvodV, ishod.NisuProcitani, pasus, razlogInt != null), html: Izvestaj.Html(r, pravila, en, uvodV, ishod.NisuProcitani, pasus, razlogInt != null));
                        if (primer?.Napravljen == true) Slanje.DodajPrilog(mail, primer.Ime, primer.Pdf);
                        mail.WriteTo(Path.Combine(izlaz, ime + ".eml"));
                        File.WriteAllText(Path.Combine(izlaz, ime + ".html"), "<!doctype html><meta charset=\"utf-8\"><title>" + System.Net.WebUtility.HtmlEncode(Izvestaj.Naslov(r, pravila, en))
                            + "</title>" + Izvestaj.Html(r, pravila, en, uvodV, ishod.NisuProcitani, pasus, razlogInt != null), Encoding.UTF8);
                    }
                    if (cit7Tekst != null)
                    {
                        var uvodC = "[TEST] Interni izveštaj – ne ide pošiljaocu" + (posiljalac != null ? " (" + posiljalac + ")" : "") + ".";
                        File.WriteAllText(Path.Combine(izlaz, ime + ".cit7.txt"), cit7Tekst, Encoding.UTF8);
                        var html7 = Cit7Provera.Html(rPuni.Cit7, pravila, uvodC);
                        File.WriteAllText(Path.Combine(izlaz, ime + ".cit7.html"), "<!doctype html><meta charset=\"utf-8\"><title>CIT 7</title>" + html7, Encoding.UTF8);
                        Slanje.Nova(cfg, new[] { cfg.IzvestajAdresa }, null, Cit7Provera.Naslov(rPuni.Cit7, naslovMaila ?? ime), Cit7Provera.Tekst(rPuni.Cit7, pravila, uvodC), html: html7)
                            .WriteTo(Path.Combine(izlaz, ime + ".cit7.eml"));
                    }
                    File.WriteAllText(Path.Combine(izlaz, ime + ".json"), Json.U(new
                    {
                        slucaj = ime, datum, rezim, dokumenti = r.Dokumenti,
                        interno = razlogInt, primaoci_live = razlogInt != null ? "samo interno" : primaociLive.Opis(), nalazi = rPuni.Listovi.Select(l => new { list = l.Oznaka, kp = l.KpRezim, rucno = l.ZaRucnuProveru, nalazi = l.Nalazi }),
                        preskoceno = ishod.Preskoceno, nisu_procitani = ishod.NisuProcitani,
                    }), Encoding.UTF8);
                }
                en = en0;
            }
            var ni = Motor.NijeImplementirano(pravila);
            Console.WriteLine("Pravila iz JSON-a koja nisu implementirana (ne izvršavaju se): " + (ni.Count == 0 ? "nema" : string.Join(", ", ni)));
            Console.WriteLine("OCR strana: " + citac.BrojOcrStrana + ".");
            KrajUpisa();
            return 0;
        }

        private static string ZajednickiKoren(List<string> fajlovi)
        {
            var delovi = fajlovi.Select(f => Path.GetDirectoryName(Path.GetFullPath(f)).Split(Path.DirectorySeparatorChar)).ToList();
            var n = delovi.Min(d => d.Length);
            int i = 0;
            while (i < n && delovi.All(d => string.Equals(d[i], delovi[0][i], StringComparison.OrdinalIgnoreCase))) i++;
            return string.Join(Path.DirectorySeparatorChar.ToString(), delovi[0].Take(i));
        }

        private static string RelativnaPutanja(string koren, string f)
        {
            var pun = Path.GetFullPath(f);
            return !string.IsNullOrEmpty(koren) && pun.StartsWith(koren, StringComparison.OrdinalIgnoreCase)
                ? pun.Substring(koren.Length).TrimStart('\\', '/') : Path.GetFileName(f);
        }

        // ------------------------------------------------------------------------------------------- test veze

        private static int TestVeze(AgentConfig cfg, Log log)
        {
            int greske = 0;
            void Korak(string ime, Action a)
            {
                try { a(); Console.WriteLine("OK    " + ime); }
                catch (Exception ex) { greske++; Console.WriteLine("GREŠKA " + ime + ": " + ex.Message); }
            }
            Console.WriteLine("Nalog: " + cfg.Nalog + ", režim " + cfg.Rezim + ", izveštaji (Test) na " + cfg.IzvestajAdresa);
            Korak("pravila " + Path.Combine(cfg.PravilaFolder, cfg.PravilaFajl), () => PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null));
            Korak("šablon " + cfg.SablonFajl, () => CimSablon.Ucitaj(cfg.SablonFajl));
            Korak("OCR (Tesseract, " + cfg.OcrJezici + ", " + cfg.TessdataFolder + ")", () =>
            {
                if (!Ocr.Dostupan(cfg.TessdataFolder, cfg.OcrJezici, out var razlog)) throw new InvalidOperationException(razlog);
                using (var o = new Ocr(cfg.TessdataFolder, cfg.OcrJezici))
                {
                    // Probna slika sa tekstom: proverava da se Tesseract i Leptonica (native DLL) učitavaju i da OCR čita.
                    byte[] png;
                    using (var bmp = new System.Drawing.Bitmap(900, 160))
                    using (var g = System.Drawing.Graphics.FromImage(bmp))
                    using (var font = new System.Drawing.Font("Arial", 40))
                    using (var ms = new MemoryStream())
                    {
                        g.Clear(System.Drawing.Color.White);
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                        g.DrawString("FRACHTBRIEF CIM 3212", font, System.Drawing.Brushes.Black, 20, 40);
                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        png = ms.ToArray();
                    }
                    var procitano = string.Join(" ", o.Prepoznaj(png, out _).Select(r => r.T));
                    Console.WriteLine("      OCR probne slike: \"" + procitano + "\"");
                    if (procitano.IndexOf("3212", StringComparison.Ordinal) < 0)
                        throw new InvalidOperationException("OCR radi, ali nije pročitao probni tekst (\"" + procitano + "\").");
                }
            });
            // nije greška testa: bez ključa agent radi, samo Word prilozi idu na ručnu proveru
            Console.WriteLine((Prilozi.WordUPdf.KljucUcitan ? "OK    " : "PAŽNJA ") + "Syncfusion ključ (Word → PDF): " + Prilozi.WordUPdf.StanjeKljuca);
            Korak("IMAP " + cfg.ImapHost + ":" + cfg.ImapPort, () =>
            {
                using (var s = new Sanduce(cfg))
                {
                    s.Povezi();
                    var d = s.Dolazni(); d.Open(MailKit.FolderAccess.ReadOnly);
                    Console.WriteLine("      dolazni: " + d.FullName + " (" + d.Count + " poruka)");
                    if (cfg.CitajOdlazne) { var o = s.Odlazni(); o.Open(MailKit.FolderAccess.ReadOnly); Console.WriteLine("      odlazni: " + o.FullName + " (" + o.Count + " poruka)"); }
                }
            });
            Korak("SMTP " + cfg.SmtpHost + ":" + cfg.SmtpPort, () =>
            {
                using (var smtp = new MailKit.Net.Smtp.SmtpClient())
                {
                    smtp.Connect(cfg.SmtpHost, cfg.SmtpPort, cfg.SmtpSsl ? MailKit.Security.SecureSocketOptions.SslOnConnect : MailKit.Security.SecureSocketOptions.StartTls);
                    smtp.Authenticate(cfg.Nalog, cfg.Lozinka);
                    smtp.Disconnect(true);
                }
            });
            // v2.9.0: upis u bazu – nije greška testa (provera i izveštaji rade i bez njega); konekcija se ne ispisuje
            if (cfg.UpisUBazu)
            {
                try
                {
                    if (string.IsNullOrEmpty(cfg.Konekcija)) throw new InvalidOperationException("konekcija nije podešena" + (AgentConfig.OtvorenaKonekcija ? " (ključ Konekcija u otvorenom tekstu se ne koristi – samo Konekcija.dpapi)" : ""));
                    using (var c = Baza.UpisUBazu.Otvori(cfg.Konekcija))
                    using (var cmd = new System.Data.SqlClient.SqlCommand(@"SELECT SUSER_SNAME(), DB_NAME(),
  HAS_PERMS_BY_NAME('planiranje.tmpJobs','OBJECT','INSERT') + HAS_PERMS_BY_NAME('planiranje.tmpTeretnicaStavke','OBJECT','DELETE') + HAS_PERMS_BY_NAME('dbo.stanice','OBJECT','SELECT') + HAS_PERMS_BY_NAME('dbo.Partnerji','OBJECT','SELECT') + HAS_PERMS_BY_NAME('dbo.NHM','OBJECT','SELECT')", c))
                    using (var r = cmd.ExecuteReader())
                    {
                        r.Read();
                        var prava = r.IsDBNull(2) ? 0 : r.GetInt32(2);
                        Console.WriteLine((prava == 5 ? "OK    " : "PAŽNJA ") + "upis u bazu: nalog " + r.GetString(0) + ", baza " + r.GetString(1) + (prava == 5 ? ", prava na tmp tabele i šifarnike postoje" : ", nedostaju prava (pokrenuti OP_913_tmp_Prava.sql)"));
                    }
                }
                catch (Exception ex) { Console.WriteLine("PAŽNJA upis u bazu: " + ex.Message); }
            }
            else Console.WriteLine("      upis u bazu: isključen (UpisUBazu = false)");
            return greske == 0 ? 0 : 1;
        }
    }
}
