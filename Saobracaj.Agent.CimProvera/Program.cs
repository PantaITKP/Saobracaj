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
      --datum yyyy-MM-dd           datum prijema (za ""datum u budućnosti""); podrazumevano: najnoviji fajl
      --en                         poruka na engleskom
      --izlaz <folder>             upisuje rezultat (txt + json) po slučaju
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
            Izvestaj.Potpis = PotpisIzvestaja.Iz(cfg);
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

        // ------------------------------------------------------------------------------------------- validate

        private static int Validate(AgentConfig cfg, Log log, List<string> a)
        {
            string Opcija(string ime) { var i = a.IndexOf(ime); if (i < 0 || i + 1 >= a.Count) return null; var v = a[i + 1]; a.RemoveAt(i + 1); a.RemoveAt(i); return v; }
            bool Zastavica(string ime) { var i = a.IndexOf(ime); if (i < 0) return false; a.RemoveAt(i); return true; }

            var rezim = Opcija("--rezim") ?? "auto";
            var datumArg = Opcija("--datum");
            var izlaz = Opcija("--izlaz");
            var posiljalac = Opcija("--posiljalac");
            var en = Zastavica("--en");
            var podfolderi = Zastavica("--podfolderi");
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

            var pravila = PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, log);
            using var citac = new LokalniCitac(cfg, log, pravila.Kp);
            var obrada = new Obrada(pravila, citac, log, cfg.PragPouzdanosti);
            int ukupnoGresaka = 0;

            foreach (var (ime, fajlovi) in slucajevi)
            {
                var koren = fajlovi.Count == 1 ? Path.GetDirectoryName(fajlovi[0]) : ZajednickiKoren(fajlovi);
                var prilozi = fajlovi.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .Select(f => (ime: RelativnaPutanja(koren, f), sadrzaj: File.ReadAllBytes(f))).ToList();
                var datum = datumArg != null ? DateTime.ParseExact(datumArg, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : fajlovi.Select(File.GetLastWriteTime).DefaultIfEmpty(DateTime.Today).Max();

                log.Info("=== " + ime + " (" + prilozi.Count + " fajlova, datum prijema " + datum.ToString("dd.MM.yyyy") + ", režim " + rezim + ")");
                var ishod = obrada.Proveri(prilozi, posiljalac, posiljalac != null && cfg.JeInterni(posiljalac), datum, en, rezim);
                var r = ishod.Provera;

                var sb = new StringBuilder();
                sb.AppendLine("################ " + ime);
                sb.AppendLine("Dokumenti:");
                sb.AppendLine(Izvestaj.Dokumenti(r.Dokumenti));
                foreach (var x in ishod.Preskoceno) sb.AppendLine("  (preskočeno) " + x);
                foreach (var x in ishod.NisuProcitani) sb.AppendLine("  (NIJE PROČITANO) " + x);
                sb.AppendLine();
                if (!r.ImaTovarniList && ishod.NisuProcitani.Count > 0)
                    sb.AppendLine("Tovarni list nije pronađen među pročitanim prilozima, ali " + ishod.NisuProcitani.Count + " priloga nije pročitano – rezultat nije konačan (ručna provera).");
                else if (!r.ImaTovarniList) sb.AppendLine(Izvestaj.NemaTovarnogLista(pravila, en));
                else
                {
                    sb.AppendLine("Režim KP: " + string.Join(", ", r.Listovi.Select(l => l.KpRezim ? "da" : "ne").Distinct()));
                    sb.AppendLine("Predmet: " + Izvestaj.Naslov(r, pravila, en));
                    sb.AppendLine();
                    sb.AppendLine(Izvestaj.Telo(r, pravila, en, null, ishod.NisuProcitani));
                }
                if (r.GreskePravila.Count > 0) sb.AppendLine("Pravila sa greškom u izvršavanju: " + string.Join("; ", r.GreskePravila));
                Console.WriteLine(sb.ToString());
                ukupnoGresaka += r.BrojGresaka;

                if (izlaz != null)
                {
                    Directory.CreateDirectory(izlaz);
                    File.WriteAllText(Path.Combine(izlaz, ime + ".txt"), sb.ToString(), Encoding.UTF8);
                    if (r.ImaTovarniList)
                    {
                        File.WriteAllText(Path.Combine(izlaz, ime + ".html"), "<!doctype html><meta charset=\"utf-8\"><title>" + System.Net.WebUtility.HtmlEncode(Izvestaj.Naslov(r, pravila, en))
                            + "</title>" + Izvestaj.Html(r, pravila, en, null, ishod.NisuProcitani), Encoding.UTF8);
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
                            }
                            Console.WriteLine("PDF primer: " + (primer.Napravljen ? primer.Ime + " (" + primer.Pdf.Length / 1024 + " KB, strana " + primer.Strane.Count + "; "
                                + string.Join("; ", primer.PoListu.Select(x => x.list + ": ispravki " + x.ispravki + ", napomena " + x.napomena + ", samo u legendi " + x.samoULegendi)) + ")"
                                : "nije napravljen – " + primer.Razlog + (primer.Greska != null ? " (" + primer.Greska + ")" : "")));
                        }
                        // isti mail kao što ga agent šalje (multipart/alternative, UTF-8; sa PDF primerom kao prilogom) – otvara se u Outlook-u (.eml)
                        var pasus = Izvestaj.PasusPrimera(primer, en);
                        var mail = Slanje.Nova(cfg, new[] { cfg.IzvestajAdresa }, null, Izvestaj.Naslov(r, pravila, en),
                            Izvestaj.Telo(r, pravila, en, null, ishod.NisuProcitani, pasus), html: Izvestaj.Html(r, pravila, en, null, ishod.NisuProcitani, pasus));
                        if (primer?.Napravljen == true) Slanje.DodajPrilog(mail, primer.Ime, primer.Pdf);
                        mail.WriteTo(Path.Combine(izlaz, ime + ".eml"));
                        File.WriteAllText(Path.Combine(izlaz, ime + ".html"), "<!doctype html><meta charset=\"utf-8\"><title>" + System.Net.WebUtility.HtmlEncode(Izvestaj.Naslov(r, pravila, en))
                            + "</title>" + Izvestaj.Html(r, pravila, en, null, ishod.NisuProcitani, pasus), Encoding.UTF8);
                    }
                    File.WriteAllText(Path.Combine(izlaz, ime + ".json"), Json.U(new
                    {
                        slucaj = ime, datum, rezim, dokumenti = r.Dokumenti,
                        nalazi = r.Listovi.Select(l => new { list = l.Oznaka, kp = l.KpRezim, rucno = l.ZaRucnuProveru, nalazi = l.Nalazi }),
                        preskoceno = ishod.Preskoceno, nisu_procitani = ishod.NisuProcitani,
                    }), Encoding.UTF8);
                }
            }
            var ni = Motor.NijeImplementirano(pravila);
            Console.WriteLine("Pravila iz JSON-a koja nisu implementirana (ne izvršavaju se): " + (ni.Count == 0 ? "nema" : string.Join(", ", ni)));
            Console.WriteLine("OCR strana: " + citac.BrojOcrStrana + ".");
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
            return greske == 0 ? 0 : 1;
        }
    }
}
