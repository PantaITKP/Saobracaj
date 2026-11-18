using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MimeKit;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Mail;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;
using Saobracaj.Common.Text;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>Ishod obrade jedne poruke.</summary>
    internal enum Korak
    {
        /// <summary>Poruka je završena bez izveštaja (preskočena, bez priloga, nema CIM-a, obaveštenje…).</summary>
        Gotova,
        /// <summary>Poruka je proverena i izveštaj je poslat.</summary>
        Obradjena,
        /// <summary>Privremena greška: poruka se ponovo pokušava u sledećem pokretanju (najviše MaxPokusaja puta).</summary>
        PrivremenaGreska,
    }

    /// <summary>
    /// v3.0: ulaz jedne poruke, nezavisan od izvora – IMAP (agent), .eml/.msg/folder/.zip (CimRadionica), arhiva (ponovna obrada).
    /// </summary>
    internal sealed class UlaznaPoruka
    {
        public MimeMessage Poruka;
        public long Uid;
        public string Folder = "INBOX";
        /// <summary>Ključ poruke u stanju (sanduče + Message-ID).</summary>
        public string Kljuc = "";
        public bool Odlazni;
        /// <summary>„imap“, „arhiva“, „eml“, „msg“, „folder“, „zip“.</summary>
        public string Izvor = "imap";
    }

    /// <summary>Poruka koju je obrada pripremila (agent je šalje; CimRadionica je samo čuva – nikad ne šalje).</summary>
    internal sealed class PripremljenaPoruka
    {
        /// <summary>izvestaj, interni, kopija, cit7, interna-rucna, azurirano, poredjenje, upozorenje.</summary>
        public string Vrsta = "";
        public MimeMessage Poruka;
        /// <summary>Režim Test: primaoci u Live režimu („Odgovori svima“) iz zaglavlja X-CimProvera-Live-Primaoci.</summary>
        public string LivePrimaoci;
        public string Naslov => Poruka?.Subject ?? "";
        public List<string> Za => PrimaociLive.Adrese(Poruka?.To);
        public List<string> Cc => PrimaociLive.Adrese(Poruka?.Cc);
        public string Telo => Poruka?.TextBody ?? "";
        public string Html => Poruka?.HtmlBody ?? "";
        public List<string> Prilozi => Poruka?.Attachments.Select(a => a.ContentDisposition?.FileName ?? a.ContentType?.Name ?? "").ToList() ?? new List<string>();
    }

    /// <summary>v3.0: rezultat obrade jedne poruke (za log, rezultat.json, Excel i ANALIZA.md).</summary>
    internal sealed class RezultatObrade
    {
        public Korak Korak;
        public string Status = "";
        /// <summary>Odluka o odgovoru u jednoj rečenici (log agenta).</summary>
        public string Odluka = "";
        public Ishod Ishod;
        public RezultatProvere Provera => Ishod?.Provera;
        public bool Engleski;
        public List<(string ime, byte[] sadrzaj)> Prilozi = new List<(string, byte[])>();
        public List<PripremljenaPoruka> Poruke = new List<PripremljenaPoruka>();
        public PrimerRezultat Primer;
        public Baza.IshodUpisa Upis;
        /// <summary>v3.0 (2.10): događaji iz novog dela poruke (isti spisak ide u upis i u rezultat.json).</summary>
        public List<Radionica.Dogadjaj> Dogadjaji = new List<Radionica.Dogadjaj>();
        /// <summary>Trajanje faza (s): citanje (provera sa čitanjem), upis, ukupno.</summary>
        public Dictionary<string, double> Trajanje = new Dictionary<string, double>();
        public string Greska;
    }

    /// <summary>
    /// v3.0: tok obrade jedne poruke – isti za agenta, CimRadionica i ponovnu obradu. Odluke (ponavljanja, dosije, provera, odgovor,
    /// PRIMER, upis) su u jezgru; ljuska daje ulaz (UlaznaPoruka), transport za slanje (Slanje.Transport – agent SMTP, alat nikad),
    /// upozorenja i poređenje sa prvim izveštajima (samo agent, režim Test).
    /// </summary>
    internal sealed class ObradaPoruke
    {
        private readonly AgentConfig cfg;
        private readonly Log log;
        private readonly bool dryRun;
        private readonly Obrada obrada;
        private readonly PravilaSkup pravila;
        private readonly Stanje stanje;
        /// <summary>Režim Test: odeljak poređenja koji se dodaje izveštaju tekuće poruke.</summary>
        private string dodatak;
        /// <summary>Prilozi poruke koja se obrađuje (podloga za PDF primer).</summary>
        private List<(string ime, byte[] sadrzaj)> trenutniPrilozi = new List<(string, byte[])>();
        private bool poslatDodatak;
        private RezultatObrade rez = new RezultatObrade();

        /// <summary>Interno upozorenje (ključ, tekst, najviše jednom na N sati) – agent ga šalje mailom; alat ga samo beleži.</summary>
        public Func<string, string, int, bool> UpozoriSpolja;
        /// <summary>Režim Test (agent): poređenje sa prvim izveštajem za iste fajlove; null kada ga nema.</summary>
        public Func<IEnumerable<string>, RezultatProvere, bool, string, string> Poredi;
        /// <summary>v2.9.0: jedan upis po prolazu (šifarnici se učitavaju jednom).</summary>
        public Baza.UpisUBazu Upis;
        /// <summary>v3.0 (2.10): mesto događaja → ID stanice (opciono; šifarnik).</summary>
        public Func<string, int?> StanicaPoNazivu;

        public ObradaPoruke(AgentConfig cfg, Log log, Obrada obrada, PravilaSkup pravila, Stanje stanje, bool dryRun)
        {
            this.cfg = cfg; this.log = log; this.obrada = obrada; this.pravila = pravila; this.stanje = stanje; this.dryRun = dryRun;
        }

        /// <summary>Obrada jedne poruke; status i broj pokušaja se vode u <paramref name="ps"/> (stanje poruke).</summary>
        public RezultatObrade Obradi(UlaznaPoruka u, Stanje.PorukaStanje ps)
        {
            rez = new RezultatObrade();
            var sw = Stopwatch.StartNew();
            try { rez.Korak = ObradiPoruku(u, ps); }
            finally
            {
                rez.Status = ps.Status ?? "";
                rez.Trajanje["ukupno"] = sw.Elapsed.TotalSeconds;
            }
            return rez;
        }

        private bool Upozori(string kljuc, string tekst, int sati = 6) => UpozoriSpolja?.Invoke(kljuc, tekst, sati) ?? false;

        /// <summary>Beleži pripremljenu poruku (za alat, rezultat.json i ANALIZA.md).</summary>
        private void Zabelezi(string vrsta, MimeMessage poruka, PrimerRezultat primer = null, string live = null)
        {
            rez.Poruke.Add(new PripremljenaPoruka { Vrsta = vrsta, Poruka = poruka, LivePrimaoci = live ?? (poruka.Headers[PrimaociLive.ZaglavljeLive]) });
        }

        /// <summary>Slanje kroz zaštitu režima Test i transport ljuske (agent: SMTP; CimRadionica: samo čuvanje).</summary>
        private void Posalji(string vrsta, MimeMessage poruka)
        {
            if (vrsta != null) Zabelezi(vrsta, poruka);
            Slanje.Posalji(cfg, poruka);
        }

        private static string Regex(string s) => System.Text.RegularExpressions.Regex.Replace(s ?? "", @"\s+", " ").Trim();

        /// <summary>Test: lista primalaca u Live režimu za zaglavlje test maila (X-CimProvera-Live-Primaoci).</summary>
        private string testLive;

        /// <summary>Telo maila kao običan tekst (TextBody, inače HtmlBody bez oznaka; citat u HTML-u – blockquote – se izbacuje).</summary>
        internal static string TeloMaila(MimeMessage m)
        {
            if (!string.IsNullOrWhiteSpace(m.TextBody)) return TekstUtil.Ocisti(m.TextBody);
            var h = m.HtmlBody ?? "";
            h = System.Text.RegularExpressions.Regex.Replace(h, @"<(blockquote|style|script|head)\b.*?</\1>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            h = System.Text.RegularExpressions.Regex.Replace(h, @"<br\s*/?>|</p>|</div>|</tr>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            h = System.Text.RegularExpressions.Regex.Replace(h, @"<[^>]+>", "");
            return TekstUtil.Ocisti(System.Net.WebUtility.HtmlDecode(h));
        }

        /// <summary>Znakovi van osnovne ravni (emoji…) u tekstu, za log: „U+1F642, U+1F44D“ (bez ostalog sadržaja).</summary>
        internal static string ZnakoviVanBmp(string s, int max = 5)
        {
            var l = new List<string>();
            s = s ?? "";
            for (int i = 0; i < s.Length && l.Count < max; i++)
                if (char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
                {
                    var k = "U+" + char.ConvertToUtf32(s[i], s[i + 1]).ToString("X");
                    if (!l.Contains(k)) l.Add(k);
                    i++;
                }
            return string.Join(", ", l);
        }

        private Korak ObradiPoruku(UlaznaPoruka u, Stanje.PorukaStanje ps)
        {
            var uid = (Id: u.Uid, Folder: u.Folder);
            var kljuc = u.Kljuc;
            var odlazni = u.Odlazni;
            var m = u.Poruka;
            try
            {
                var od1 = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
                rez.Dogadjaji = Radionica.IzvlacenjeDogadjaja.Izvuci(m.Subject, TeloMaila(m), m.Date, od1.Contains("@") ? od1.Substring(od1.LastIndexOf('@') + 1) : "", StanicaPoNazivu);
            }
            catch (Exception ex) { log?.Upozorenje("  događaji iz teksta maila nisu izdvojeni: " + ex.Message); }
            {
                var razlog = Slanje.RazlogPreskakanja(m, cfg, !odlazni);
                if (razlog != null)
                {
                    log.Info("  UID " + uid.Id + ": " + (razlog.StartsWith("sopstveni") ? razlog + " – preskočeno" : "preskočeno – " + razlog));
                    ps.Status = "Preskoceno"; return Korak.Gotova;
                }
                var prilozi = Raspakivanje.PriloziMaila(m).ToList();
                trenutniPrilozi = prilozi; rez.Prilozi = prilozi;
                if (prilozi.Count == 0)
                {
                    log.Info("  UID " + uid.Id + ": mail nema priloga – preskočeno (BezPriloga).");
                    // v3.0.1 (5): događaji iz teksta maila bez dokumenata se upisuju i vezuju za posao (nit, ID najave, voz, relacija)
                    if (rez.Dogadjaji.Count > 0) UpisiUBazu(m, uid.Id, u.Folder, null, prilozi, null);
                    ps.Status = "BezPriloga"; return Korak.Gotova;
                }
                // v3.0.1 (4.1): mail sa samo slikama iz potpisa (logo, baner) nema dokumenata – bez odgovora; slike ne učestvuju u ponavljanju
                if (prilozi.All(p => Raspakivanje.Ukras(p.ime, p.sadrzaj) != null))
                {
                    log.Info("  UID " + uid.Id + ": samo slike iz potpisa (" + prilozi.Count + ") – nema dokumenata, bez odgovora.");
                    if (rez.Dogadjaji.Count > 0) UpisiUBazu(m, uid.Id, u.Folder, null, new List<(string, byte[])>(), null);
                    ps.Status = "SamoPotpis"; return Korak.Gotova;
                }
                ps.HesPriloga = Prilog.Sha256(prilozi.SelectMany(p => p.sadrzaj).ToArray()).Substring(0, 16);
                // v2.8.4 (6.1): isti prilog (isti heš) koji je već proveren se ne proverava ponovo – novi mail dobija istu odluku;
                // ako je prvi rezultat već poslat, ništa se ne šalje ponovo
                var ponavljanja = Ponavljanja.Ucitaj(cfg.StanjeFolder);
                var oznaka = "UID " + uid.Id;
                void Zapamti(string ishodTxt, RezultatProvere rr, bool prijavljeno)
                {
                    if (dryRun) return;
                    try
                    {
                        ponavljanja.ZapamtiPriloge(prilozi, oznaka, ishodTxt, DateTime.Now);
                        if (prijavljeno && rr != null) ponavljanja.ZapamtiListove(rr, oznaka, DateTime.Now);
                        ponavljanja.Sacuvaj();
                        obrada.ZapamtiUDosije(rr);
                    }
                    catch (Exception ex) { log.Upozorenje("  UID " + uid.Id + ": " + Ponavljanja.Ime + " nije upisan: " + ex.Message); }
                    // v2.9.0: upis u privremene tabele – posle provere i izveštaja; greška ne menja ni jedno ni drugo
                    var iu = UpisiUBazu(m, uid.Id, u.Folder, rr, prilozi, obrada.PoslednjaPosiljka);
                    // v2.9.2 (3.3): mail bez novog izveštaja (isti tovarni list, nova verzija spiska) koji je ažurirao posao – u režimu Test
                    // kratka interna poruka šta je ažurirano (pošiljaocu ništa)
                    // v3.0 (0a.1): INTERNO napomena upisa („spisak (28 kola) ≠ tovarni list (30 kola) – proveriti“) ide i uz poslat izveštaj
                    if (iu != null && ((!prijavljeno && iu.Promene.Count > 0) || iu.Interno.Count > 0)) PosaljiInternoAzurirano(m, iu);
                }
                var istiPrilog = ponavljanja.IstiPrilozi(prilozi);
                if (istiPrilog != null)
                {
                    log.Info("  UID " + uid.Id + ": isti prilog kao " + istiPrilog.Oznaka + " (" + istiPrilog.Vreme.ToString("dd.MM. HH:mm") + ", " + istiPrilog.Ishod + ") – već obrađeno; ništa se ne šalje.");
                    // v2.9.1 (5.7): prijem se ipak beleži u tmpDokument, a tekst maila ide u Napomenu posla
                    var iu0 = UpisiUBazu(m, uid.Id, u.Folder, null, prilozi, null);
                    if (iu0 != null && iu0.Promene.Count > 0) PosaljiInternoAzurirano(m, iu0);
                    ps.Status = "IstiPrilog"; return Korak.Gotova;
                }

                var od = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
                var jeOdlazni = odlazni || string.Equals(od, cfg.Nalog, StringComparison.OrdinalIgnoreCase);
                // naslov i telo mogu imati neispravan Unicode (usamljeni surogat) – čiste se pre bilo kakve normalizacije
                var tekstMaila = (m.Subject ?? "") + " " + (m.TextBody ?? m.HtmlBody ?? "");
                var neispravnih = TekstUtil.BrojNeispravnih(tekstMaila);
                if (neispravnih > 0) log.Upozorenje("  UID " + uid.Id + ": naslov/telo maila ima " + neispravnih + " neispravnih Unicode znakova (usamljeni surogat) – zamenjeni znakom U+FFFD.");
                var vanBmp = ZnakoviVanBmp(tekstMaila);
                if (vanBmp.Length > 0) log.Info("  UID " + uid.Id + ": naslov/telo sadrži znakove van osnovne ravni (emoji i sl.): " + vanBmp);
                // v2.8: jezik po pošiljaocu (interni i .rs/.ba/.me/.hr/.si → srpski), ne po citiranom tekstu maila;
                // v2.8.1: opšti domeni (gmail.com…) – po tekstu koji je pošiljalac sam napisao (bez citata i potpisa)
                var en = Obrada.EngleskiZaPosiljaoca(od, cfg.JeInterni, m.Subject, TeloMaila(m));
                var datum = m.Date == DateTimeOffset.MinValue ? DateTime.Now : m.Date.LocalDateTime;
                log.Info("  UID " + uid.Id + " " + kljuc + ": " + prilozi.Count + " priloga, od " + od + (jeOdlazni ? " (odlazni)" : ""));

                Ishod ishod;
                obrada.NaslovMaila = m.Subject;
                // v2.8.4 (8): dosije pošiljke (stanje\posiljke.json): nit, naslov, ključ lista, r8, kola
                obrada.Dosije = Dosije.Ucitaj(cfg.StanjeFolder);
                obrada.MailDosijea = new MailZaDosije
                {
                    Oznaka = oznaka, MessageId = m.MessageId ?? "", Naslov = m.Subject ?? "", Posiljalac = od, Vreme = datum,
                    Prethodni = m.References.Concat(string.IsNullOrEmpty(m.InReplyTo) ? new string[0] : new[] { m.InReplyTo }).Distinct().ToList(),
                };
                var pocetakProvere = DateTime.Now;
                try { ishod = obrada.Proveri(prilozi, od, cfg.JeInterni(od), datum, en); }
                catch (Exception ex)
                {
                    log.Greska("  UID " + uid.Id + ": obrada nije uspela", ex);
                    ishod = new Ishod { PrivremenaGreska = true, NisuProcitani = { ex.Message } };
                }
                rez.Ishod = ishod; rez.Engleski = en;
                rez.Trajanje["provera"] = (DateTime.Now - pocetakProvere).TotalSeconds;
                if (WordUPdf.NedostajaoKljuc && !WordUPdf.KljucUcitan)
                {
                    // jedno interno obaveštenje (dok se ključ ne podesi); Word prilog je već na ručnoj proveri
                    Upozori("syncfusion-kljuc", "Word prilog u mailu „" + m.Subject + "“ od " + od + " nije pretvoren u PDF: Syncfusion licencni ključ nije učitan ("
                        + WordUPdf.StanjeKljuca + "). Prilog ide na ručnu proveru. Pokrenite Instaliraj.ps1 i unesite Syncfusion ključ.", 24 * 365);
                    WordUPdf.NedostajaoKljuc = false;
                }
                foreach (var x in ishod.Preskoceno) log.Info("    preskočen prilog: " + x);
                // Režim Test: poređenje sa prvim izveštajem za iste fajlove (prilozi i fajlovi iz ZIP-a).
                string Por(RezultatProvere rr, string opis) => Poredi?.Invoke(prilozi.Select(p => p.ime).Concat(rr?.Dokumenti.Select(d => d.Fajl) ?? Enumerable.Empty<string>()), rr, en, opis);

                if (ishod.PrivremenaGreska)
                {
                    // ponovni pokušaj / ručna provera posle MaxPokusaja: RedPoruka
                    ps.PoslednjaGreska = string.Join("; ", ishod.NisuProcitani);
                    return Korak.PrivremenaGreska;
                }

                var r = ishod.Provera;
                // v2.8.1: CIT 7 – interni izveštaj (veza sa listom, izmene, upozorenja); pošiljaocu ništa
                if (r.Cit7.Count > 0 && !ps.Cit7Poslat)
                    try { PosaljiCit7(m, r); if (!dryRun) ps.Cit7Poslat = true; }
                    catch (Exception ex) { log.Greska("  UID " + uid.Id + ": interni izveštaj o CIT 7 nije poslat: " + ex.Message); }
                // v2.8.1: istorija obrađenih listova (za CIT 7 koji stigne kasnije, bez lista u mailu)
                if (r.ImaTovarniList && !dryRun)
                    try { IstorijaListova.Dodaj(cfg.StanjeFolder, r.Listovi.Where(l => l.Cim != null && string.IsNullOrEmpty(l.Cim.NeprepoznatObrazac)).Select(l => IstorijaListova.Iz(l, r.Dokumenti, datum, Regex(m.Subject)))); }
                    catch (Exception ex) { log.Upozorenje("  UID " + uid.Id + ": istorija listova nije upisana: " + ex.Message); }
                if (!r.ImaTovarniList && (ishod.NisuProcitani.Count > 0 || ishod.Neprepoznati.Count > 0 || ishod.NisaPouzdanost.Count > 0)
                    && !Obrada.MozdaCim(ishod, m.Subject, TeloMaila(m)))
                {
                    // v2.9.2 (3.2): nepročitan prilog, a ni strana ne liči na CIM/CUV ni mail ne pominje CIM, CUV, tovarni list, teretnicu
                    // ili Frachtbrief („Spisak za dan 17.11.“ – rukom pisana lista, „za NT-ove“, „Sporna dokumentacija“) – samo log i tmpDokument
                    log.Info("  UID " + uid.Id + ": prilozi nisu pročitani (" + string.Join("; ", ishod.NisuProcitani.Concat(ishod.Neprepoznati).Concat(ishod.NisaPouzdanost)) + "), a mail ne pominje CIM/CUV/tovarni list – bez interne poruke.");
                    ps.Status = "NeprocitanoBezCim";
                    Zapamti("prilozi nisu pročitani – mail ne pominje CIM, bez poruke", r, false);
                    return Korak.Gotova;
                }
                if (!r.ImaTovarniList && (ishod.NisuProcitani.Count > 0 || ishod.Neprepoznati.Count > 0 || ishod.NisaPouzdanost.Count > 0))
                {
                    // Ne sme se zaključiti "nema CIM-a" ako neki prilog nije ni pročitan ni prepoznat: samo interna poruka, ne pošiljaocu.
                    log.Upozorenje("  UID " + uid.Id + ": prilozi nisu pročitani/prepoznati, ne može da se utvrdi da li mail sadrži CIM – ručna provera.");
                    ps.Status = "NeprocitanoRucnaProvera";
                    // v2.8.4 (6.3): jedna interna poruka po mailu – staro upozorenje „Mail … prilozi nisu pročitani – . Ne može da se utvrdi…“ uklonjeno
                    try { PosaljiInternoRucnaProvera(m, ishod, en); }
                    catch (Exception ex) { log.Greska("  UID " + uid.Id + ": interna poruka nije poslata: " + ex.Message); }
                    PosaljiPoredjenje(m, Por(r, "prilozi nisu pročitani"), "prilozi nisu pročitani");
                    Zapamti("prilozi nisu pročitani – interna ručna provera", r, false);
                    return Korak.Gotova;
                }
                if (!r.ImaTovarniList)
                {
                    // v2.8.4 (1): mail bez tovarnog lista (samo spisak kola, carinski dokumenti, najava, sastav voza) ne dobija nikakav
                    // odgovor, ni interni – samo red u logu. (Ranije: obaveštenje „samo spisak kola“.)
                    log.Info("  UID " + uid.Id + ": " + Izvestaj.LogBezTovarnogLista(r.Dokumenti));
                    ps.Status = r.Dokumenti.Any(d => d.Tip == TipDokumenta.SpisakKola) ? "SamoSpisak" : "NemaCim";
                    Zapamti("nema CIM/CUV – bez odgovora", r, false);
                    return Korak.Gotova;
                }
                // v2.8.4 (6.2): isti tovarni list (ista r62, ili pošiljalac + r8 + relacija) sa nepromenjenim rubrikama je već prijavljen
                var istiList = ponavljanja.IstiListovi(r);
                if (istiList != null)
                {
                    log.Info("  UID " + uid.Id + ": " + Izvestaj.OpisListova(r, false) + " – isti tovarni list kao " + istiList.Oznaka + " (" + istiList.Vreme.ToString("dd.MM. HH:mm")
                        + "), sadržaj rubrika nepromenjen – već prijavljeno, bez novog izveštaja.");
                    ps.Status = "IstiList";
                    Zapamti("isti tovarni list kao " + istiList.Oznaka + " – bez novog izveštaja", r, false);
                    return Korak.Gotova;
                }

                // v2.9.2 (1): broj ponude nije proveren (baza nedostupna) – ništa se ne šalje, ponavlja se u sledećem prolazu; posle
                // poslednjeg pokušaja izveštaj ide samo INTERNO (list je na ručnoj proveri)
                if (r.Listovi.SelectMany(l => l.Nalazi).Any(x => x.Kljuc == "ponuda-baza") && ps.Pokusaja < cfg.MaxPokusaja - 1)
                {
                    log.Upozorenje("  UID " + uid.Id + ": ponuda nije proverena – baza nedostupna; ponovo u sledećem prolazu.");
                    Upozori("ponuda-baza", "Broj ponude iz tovarnog lista nije proveren u dbo.Ponudba (baza nedostupna ili nalog svc_cimprovera nema pravo čitanja – pokrenuti OP_913_tmp_Prava.sql). Mailovi sa brojem ponude se ponavljaju u sledećem prolazu.", 24);
                    ps.PoslednjaGreska = "ponuda nije proverena – baza nedostupna";
                    return Korak.PrivremenaGreska;
                }
                ps.Greske = r.BrojGresaka; ps.Upozorenja = r.BrojUpozorenja;
                ps.Status = r.BrojGresaka > 0 ? "Greske" : "Ispravan";
                log.Info("  UID " + uid.Id + ": " + Izvestaj.OpisListova(r, false) + ", grešaka " + r.BrojGresaka + ", upozorenja " + r.BrojUpozorenja
                    + (r.ZaRucnuProveru ? ", RUČNA PROVERA" : "") + ". Nalazi: " + string.Join(" ", r.Listovi.SelectMany(l => l.Nalazi).Select(x => x.Pravilo).Distinct()));
                if (r.ZaRucnuProveru) ps.Status += "+RucnaProvera";

                dodatak = Por(r, null);
                poslatDodatak = false;
                try { PosaljiIzvestaj(m, r, en, jeOdlazni, ishod.NisuProcitani); }
                catch (Exception ex)
                {
                    log.Greska("  UID " + uid.Id + ": izveštaj nije poslat", ex);
                    ps.PoslednjaGreska = "izveštaj nije poslat: " + ex.Message;
                    dodatak = null;
                    return Korak.PrivremenaGreska;
                }
                if (dodatak != null && !poslatDodatak) PosaljiPoredjenje(m, dodatak, "izveštaj nije poslat (" + Izvestaj.OpisListova(r, false) + ", grešaka " + r.BrojGresaka + ", upozorenja " + r.BrojUpozorenja + ")");
                dodatak = null;
                Zapamti((Izvestaj.RazlogInternog(r, pravila) != null ? "interni izveštaj" : "izveštaj") + " poslat", r, true);
                return Korak.Obradjena;
            }
        }

        private Baza.IshodUpisa UpisiUBazu(MimeMessage m, long uid, string folder, RezultatProvere r, List<(string ime, byte[] sadrzaj)> prilozi, Dosije.Posiljka dosije)
        {
            if (!cfg.UpisUBazu || dryRun) return null;   // r == null: ponovljeni mail (isti prilozi) – samo prijem i Napomena
            if (string.IsNullOrEmpty(cfg.Konekcija))
            {
                log.Upozorenje("  UID " + uid + ": upis u bazu je uključen, ali konekcija nije podešena" + (AgentConfig.OtvorenaKonekcija ? " (ključ Konekcija u otvorenom tekstu se ne koristi – samo Konekcija.dpapi)" : "") + " – preskočeno.");
                Upozori("upis-baza", "Upis u bazu (UpisUBazu = true) nije podešen: nema konekcije (Konekcija / Konekcija.dpapi / CIMPROVERA_KONEKCIJA). Provera i izveštaji rade normalno.", 24);
                return null;
            }
            try
            {
                Upis = Upis ?? new Baza.UpisUBazu(cfg.Konekcija, log, pravila.Kp);
                var ishod = Upis.Upisi(new Baza.MailZaUpis
                {
                    MessageId = m.MessageId ?? "", Uid = uid, Sanduce = cfg.Nalog + "|" + folder, Posiljalac = m.From.Mailboxes.FirstOrDefault()?.Address ?? "",
                    Prethodni = new[] { m.InReplyTo }.Concat(m.References ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
                    Naslov = m.Subject ?? "", Primljeno = m.Date == DateTimeOffset.MinValue ? DateTime.Now : m.Date.LocalDateTime, Telo = TeloMaila(m),
                    Prilozi = prilozi, Provera = r, Dosije = dosije, Dogadjaji = rez.Dogadjaji,
                });
                log.Info("  UID " + uid + ": upis u bazu – " + ishod);
                rez.Upis = ishod;
                return ishod;
            }
            catch (Exception ex)
            {
                log.Greska("  UID " + uid + ": upis u bazu nije uspeo (rollback) – provera i izveštaj nisu promenjeni", ex);
                Upozori("upis-baza", "Upis u privremene tabele (planiranje.tmp*) nije uspeo za mail UID " + uid + " „" + m.Subject + "“: " + ex.Message
                    + Environment.NewLine + "Transakcija je vraćena (rollback). Provera i izveštaj su poslati normalno. Detalji su u logu.", 6);
                return null;
            }
        }

        /// <summary>
        /// v2.9.0: upis obrađenog maila u planiranje.tmp* (UpisUBazu = true). Jedan mail = jedna transakcija; greška → rollback, log i
        /// jedno interno obaveštenje (najviše jednom na 6 h). Provera i izveštaj su već završeni i ne menjaju se.
        /// </summary>
        /// <summary>v2.9.2 (3.3): kratka interna poruka (samo režim Test) – mail bez novog izveštaja je ažurirao posao (nova verzija spiska kola).</summary>
        private void PosaljiInternoAzurirano(MimeMessage m, Baza.IshodUpisa iu)
        {
            if (dryRun || cfg.Rezim != Rezim.Test) return;
            try
            {
                var od = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
                var telo = "INTERNO – " + (iu.Promene.Count > 0 ? "ažuriran posao" : "upis posla") + " (pošiljaocu ništa). Mail „" + m.Subject + "“ (" + od + ", " + m.Date.LocalDateTime.ToString("dd.MM. HH:mm") + ")"
                           + (iu.Promene.Count > 0 ? ": nova verzija priloga je ažurirala posao:" + Environment.NewLine + string.Join(Environment.NewLine, iu.Promene.Take(30).Select(x => "  - " + x)) : ".")
                           + (iu.Interno.Count > 0 ? Environment.NewLine + Environment.NewLine + "INTERNO: " + string.Join("; ", iu.Interno.Distinct()) : "")
                           + (iu.Napomene.Count > 0 ? Environment.NewLine + Environment.NewLine + "Napomene: " + string.Join("; ", iu.Napomene.Distinct().Take(10)) : "");
                Posalji("azurirano", Slanje.Nova(cfg, new[] { cfg.IzvestajAdresa }, null, "[INTERNO] Ažuriran posao – " + m.Subject, telo));
                log.Info("  interna poruka o ažuriranom poslu poslata (" + iu.Promene.Count + " izmena).");
            }
            catch (Exception ex) { log.Greska("  interna poruka o ažuriranom poslu nije poslata: " + ex.Message); }
        }

        private void PosaljiIzvestaj(MimeMessage m, RezultatProvere r, bool en, bool odlazni, List<string> nisuProcitani)
        {
            // Nalazi "samo interno" (npr. r49 koju popunjava KP kao prevoznik) ne idu spoljnom pošiljaocu: on dobija izveštaj
            // bez njih, a puni izveštaj ide kao interna kopija (KopijaNa). U režimu Test sve ide na IzvestajAdresa.
            var od0 = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
            // v2.8: list koji je izdao KP (r62 = 3212) ili list na ručnoj proveri → samo interni izveštaj, nikad pošiljaocu
            var razlog = Izvestaj.RazlogInternog(r, pravila);
            if (razlog != null) { PosaljiInterniIzvestaj(m, r, nisuProcitani, razlog); return; }
            // v2.8.3: Live – nalazi „samo interno“ (npr. R49, r8, kontakt) nikad u mailu ni u PDF primeru za pošiljaoca (puni izveštaj samo KopijaNa);
            // Test – mail ide na test adresu sa internim odeljkom na dnu („INTERNO (ne ide pošiljaocu)“). PDF primer ih nikad nema.
            if (!odlazni && r.ImaInternih && Izvestaj.ZaPosiljaoca(r, cfg.Rezim, cfg.JeInterni(od0)) != r)
            {
                PosaljiIzvestaj(m, r.BezInternih(), en, false, nisuProcitani, bezKopije: true);
                if (cfg.Rezim == Rezim.Live && cfg.KopijaNa.Count > 0) PosaljiInterno(m, r, en, nisuProcitani);
                return;
            }
            PosaljiIzvestaj(m, r, en, odlazni, nisuProcitani, false);
        }

        /// <summary>Interni izveštaj (KP tovarni list ili ručna provera): Test – IzvestajAdresa; Live – KopijaNa i UpozorenjaNa. Uvek na srpskom.</summary>
        private void PosaljiInterniIzvestaj(MimeMessage m, RezultatProvere r, List<string> nisuProcitani, string razlog)
        {
            var od = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
            var primaoci = cfg.Rezim == Rezim.Test ? new List<string> { cfg.IzvestajAdresa }
                : cfg.KopijaNa.Concat(new[] { cfg.UpozorenjaNa }).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (primaoci.Count == 0) { log.Upozorenje("    interni izveštaj nema primaoca (KopijaNa/UpozorenjaNa)."); return; }
            var naslov = Izvestaj.Naslov(r, pravila, false);
            // v2.8.3: kratak interni uvod – nije pismo pošiljaocu (bez „Poštovani“, zahvalnice i potpisa)
            var uvod = (cfg.Rezim == Rezim.Test ? "[TEST] " : "") + Izvestaj.UvodInternogKratak(razlog, r, od, m.Subject, m.Date.LocalDateTime);
            var primer = r.BrojGresaka + r.BrojUpozorenja > 0 ? NapraviPrimer(r, false) : null;
            var telo = Izvestaj.Telo(r, pravila, false, uvod, nisuProcitani, Izvestaj.PasusPrimera(primer, false, true), interni: true);
            var html = Izvestaj.Html(r, pravila, false, uvod, nisuProcitani, Izvestaj.PasusPrimera(primer, false, true), interni: true);
            var poruka = Slanje.Nova(cfg, primaoci, null, naslov, telo, odgovorNa: m, html: html);
            // v2.8.4 (10.3): interni izveštaj nikad ne ide spoljnim adresama – u Live samo KopijaNa / UpozorenjaNa
            if (cfg.Rezim == Rezim.Test) poruka.Headers.Add(PrimaociLive.ZaglavljeLive, "samo interno (" + string.Join(", ", cfg.KopijaNa.Concat(new[] { cfg.UpozorenjaNa }).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)) + ")");
            if (primer?.Napravljen == true) Slanje.DodajPrilog(poruka, primer.Ime, primer.Pdf);
            Zabelezi("interni", poruka, primer);
            if (dryRun) { log.Info("    [dry-run] interni izveštaj za " + string.Join(", ", primaoci) + ": " + naslov); Console.WriteLine(telo); return; }
            Posalji(null, poruka);
            log.Info("    interni izveštaj (" + (razlog == Izvestaj.RazlogKp ? "KP tovarni list, r62 = 3212" : "ručna provera") + ") poslat na " + string.Join(", ", primaoci) + ": " + naslov);
        }

        /// <summary>v2.8.1: interni izveštaj o CIT 7 (Test – IzvestajAdresa; Live – KopijaNa i UpozorenjaNa). Uvek na srpskom, nikad pošiljaocu.</summary>
        private void PosaljiCit7(MimeMessage m, RezultatProvere r)
        {
            var od = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
            var primaoci = cfg.Rezim == Rezim.Test ? new List<string> { cfg.IzvestajAdresa }
                : cfg.KopijaNa.Concat(new[] { cfg.UpozorenjaNa }).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (primaoci.Count == 0) { log.Upozorenje("    interni izveštaj o CIT 7 nema primaoca (KopijaNa/UpozorenjaNa)."); return; }
            var naslov = Cit7Provera.Naslov(r.Cit7, m.Subject);
            var uvod = (cfg.Rezim == Rezim.Test ? "[TEST] " : "") + "Interni izveštaj – ne ide pošiljaocu (" + od + "). Mail „" + m.Subject + "“, " + m.Date.LocalDateTime.ToString("dd.MM.yyyy HH:mm") + ".";
            var poruka = Slanje.Nova(cfg, primaoci, null, naslov, Cit7Provera.Tekst(r.Cit7, pravila, uvod), odgovorNa: m, html: Cit7Provera.Html(r.Cit7, pravila, uvod));
            Zabelezi("cit7", poruka);
            if (dryRun) { log.Info("    [dry-run] interni izveštaj o CIT 7 za " + string.Join(", ", primaoci) + ": " + naslov); return; }
            Posalji(null, poruka);
            log.Info("    interni izveštaj o CIT 7 poslat na " + string.Join(", ", primaoci) + ": " + naslov + " (" + string.Join("; ", r.Cit7.Select(c => (c.Veza != null ? "veza " + System.IO.Path.GetFileName(c.Veza.Fajl) : "list nije pronađen") + ", upozorenja " + c.Upozorenja.Count)) + ")");
        }

        /// <summary>Puni izveštaj (sa nalazima "samo interno") za KopijaNa, bez odgovora pošiljaocu.</summary>
        private void PosaljiInterno(MimeMessage m, RezultatProvere r, bool en, List<string> nisuProcitani)
        {
            var naslov = "[INTERNO] " + Izvestaj.Naslov(r, pravila, en);
            var uvod = "Interna kopija za KP: izveštaj za mail „" + m.Subject + "“ od " + (m.From.Mailboxes.FirstOrDefault()?.Address ?? "") + ". Nalazi u odeljku „INTERNO (ne ide pošiljaocu)“ nisu poslati pošiljaocu.";
            var poruka = Slanje.Nova(cfg, cfg.KopijaNa, null, naslov, Izvestaj.Telo(r, pravila, en, uvod, nisuProcitani), odgovorNa: m, html: Izvestaj.Html(r, pravila, en, uvod, nisuProcitani));
            Zabelezi("kopija", poruka);
            if (dryRun) { log.Info("    [dry-run] interna kopija za " + string.Join(", ", cfg.KopijaNa) + ": " + naslov); return; }
            Posalji(null, poruka);
            log.Info("    interna kopija poslata na " + string.Join(", ", cfg.KopijaNa) + ": " + naslov);
        }

        /// <summary>
        /// PDF primer ispravnog lista (B2). Greška pri pravljenju ne zaustavlja slanje: mail ide bez priloga, greška u log.
        /// Režim Test: kopija u RezultatiFolder\yyyy-MM-dd pored JSON-a i slika okvira.
        /// </summary>
        private PrimerRezultat NapraviPrimer(RezultatProvere r, bool en)
        {
            PrimerRezultat pr;
            try { pr = PrimerPdf.Napravi(r, pravila, en, trenutniPrilozi); }
            catch (Exception ex) { pr = new PrimerRezultat { Razlog = "greska", Greska = ex.Message }; }
            if (pr.Napravljen)
            {
                log.Info("    PDF primer " + pr.Ime + " (" + pr.Pdf.Length / 1024 + " KB, " + string.Join("; ", pr.PoListu.Select(x => x.ispravki + " ispravki, " + x.napomena + " napomena")) + ")");
                if (cfg.Rezim == Rezim.Test && !string.IsNullOrEmpty(cfg.RezultatiFolder))
                    try
                    {
                        var dir = System.IO.Path.Combine(cfg.RezultatiFolder, DateTime.Today.ToString("yyyy-MM-dd"));
                        System.IO.Directory.CreateDirectory(dir);
                        System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, pr.Ime), pr.Pdf);
                    }
                    catch (Exception ex) { log.Upozorenje("    kopija PDF primera nije sačuvana: " + ex.Message); }
            }
            else if (pr.Razlog == "greska" || pr.Razlog == "bez_podloge") log.Greska("    PDF primer nije napravljen (" + pr.Razlog + "): " + pr.Greska + " – mail ide bez priloga.");
            else log.Info("    PDF primer se ne prilaže: " + pr.Razlog);
            rez.Primer = pr;
            return pr;
        }

        private void PosaljiIzvestaj(MimeMessage m, RezultatProvere r, bool en, bool odlazni, List<string> nisuProcitani, bool bezKopije)
        {
            var imaNalaza = r.BrojGresaka + r.BrojUpozorenja > 0;
            if (odlazni && !imaNalaza && !cfg.ObavestiIspravanOdlazni) { log.Info("    odlazni CIM je ispravan – obaveštenje se ne šalje."); return; }
            if (!odlazni && !imaNalaza && !cfg.OdgovoriNaIspravanDolazni) return;

            var od = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
            var za = string.Join(", ", m.To.Mailboxes.Select(x => x.Address));
            var datum = m.Date.LocalDateTime.ToString("dd.MM.yyyy HH:mm");
            var naslov = Izvestaj.Naslov(r, pravila, en);
            string uvod = null;
            List<string> primaoci = new List<string>(), kopija = new List<string>();
            if (odlazni)
            {
                uvod = (en ? "The mail „" : "Mail „") + m.Subject + (en ? "“ that you sent on " : "“ koji ste poslali ") + datum + (en ? " to " : " na ") + za
                       + (r.BrojGresaka > 0 ? (en ? " contains a consignment note with errors:" : " sadrži tovarni list sa greškama:") : ":");
                primaoci = new List<string> { cfg.Nalog };
            }
            PrimaociLive live = null;
            if (!odlazni)
            {
                uvod = null; // pismo pošiljaocu počinje zahvalnicom (Izvestaj.Telo)
                // v2.8.4 (10): Live – „Odgovori svima“ (From + To + Cc, bez agenti@ i prosleđivanja), naslov „RE: … – CIM provera: …“, ista nit
                live = PrimaociLive.Iz(m, cfg);
                primaoci = live.To;
                kopija = live.Cc.Concat(bezKopije ? new List<string>() : cfg.KopijaNa).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                naslov = PrimaociLive.Naslov(m.Subject, en, r.BrojGresaka > 0);
                if (live.SamoInterno && cfg.Rezim == Rezim.Live)
                {
                    log.Info("    " + live.Napomena + " – izveštaj samo interno.");
                    // bezKopije: pozivalac (izveštaj sa internim nalazima) sam šalje punu internu kopiju – bez dvostruke kopije
                    if (cfg.KopijaNa.Count > 0 && !bezKopije) PosaljiInterno(m, r, en, nisuProcitani);
                    return;
                }
            }
            if (cfg.Rezim == Rezim.Test)
            {
                var liveOpis = odlazni ? string.Join(", ", primaoci.Concat(kopija)) : live.Opis() + (cfg.KopijaNa.Count > 0 && !bezKopije ? " (uz KopijaNa)" : "");
                uvod = "[TEST] U Live režimu išlo bi na: " + liveOpis + (odlazni ? "" : "; naslov: „" + naslov + "“") + "." + Environment.NewLine
                       + (odlazni ? "Odlazni" : "Dolazni") + " mail od " + od + " za " + za + ", " + datum + ". Naslov primljenog maila: „" + m.Subject + "“."
                       + (uvod != null ? Environment.NewLine + Environment.NewLine + uvod : "");
                testLive = liveOpis;
                primaoci = new List<string> { cfg.IzvestajAdresa };
                kopija = new List<string>();
                naslov = Izvestaj.Naslov(r, pravila, en);
            }
            var primer = imaNalaza ? NapraviPrimer(r, en) : null;
            var telo = Izvestaj.Telo(r, pravila, en, uvod, nisuProcitani, Izvestaj.PasusPrimera(primer, en));
            var html = Izvestaj.Html(r, pravila, en, uvod, nisuProcitani, Izvestaj.PasusPrimera(primer, en));
            // Opcija: ispravan dolazni CIM → samo kratka potvrda (podrazumevano isključeno).
            if (!odlazni && !imaNalaza && cfg.PotvrdaIspravnogDolaznog)
            {
                var ident = Izvestaj.Identifikacija(r, en);
                telo = (ident.Length > 0 ? ident + Environment.NewLine : "") + uvod + Environment.NewLine + Environment.NewLine + (en ? "The consignment note is correct." : "Tovarni list je ispravan.");
                html = "<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:13px\">" + System.Net.WebUtility.HtmlEncode(telo).Replace("\n", "<br>") + "</div>";
            }
            if (dodatak != null && cfg.Rezim == Rezim.Test)
            {
                var i = telo.LastIndexOf(Environment.NewLine + "--" + Environment.NewLine, StringComparison.Ordinal);
                telo = i >= 0 ? telo.Substring(0, i) + Environment.NewLine + dodatak + telo.Substring(i) : telo + Environment.NewLine + dodatak;
                var j = html.LastIndexOf("</div>", StringComparison.Ordinal);
                var blok = "<pre style=\"font-family:Consolas,monospace;font-size:12px;background:#f6f6f6;border:1px solid #ddd;padding:8px;white-space:pre-wrap\">"
                           + System.Net.WebUtility.HtmlEncode(dodatak) + "</pre>";
                html = j >= 0 ? html.Substring(0, j) + blok + html.Substring(j) : html + blok;
                poslatDodatak = true;
            }
            var poruka = Slanje.Nova(cfg, primaoci, kopija, naslov, telo, odgovorNa: m, html: html);
            if (testLive != null) poruka.Headers.Add(PrimaociLive.ZaglavljeLive, testLive);
            var testLive0 = testLive;
            testLive = null;
            if (primer?.Napravljen == true) Slanje.DodajPrilog(poruka, primer.Ime, primer.Pdf);
            Zabelezi("izvestaj", poruka, primer, testLive0);
            if (dryRun)
            {
                log.Info("    [dry-run] izveštaj za " + string.Join(", ", primaoci) + ": " + naslov);
                Console.WriteLine(Environment.NewLine + "=== " + naslov + " ===" + Environment.NewLine + telo);
                return;
            }
            Posalji(null, poruka);
            log.Info("    izveštaj poslat na " + string.Join(", ", primaoci.Concat(kopija)) + ": " + naslov);
        }

        /// <summary>
        /// Interna poruka (2.3): tovarni list nije nađen, a neki prilog nije pročitan ili prepoznat – „proverite ručno“.
        /// Ide samo internom primaocu (Test: TestPrimalac; Live: KopijaNa i UpozorenjaNa), nikad pošiljaocu.
        /// </summary>
        internal static (string naslov, string telo, List<string> primaoci) InternaRucnaProvera(AgentConfig cfg, MimeMessage m, Ishod ishod)
        {
            var od = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
            var neprepoznati = ishod.Neprepoznati.Concat(ishod.NisuProcitani.Concat(ishod.NisaPouzdanost).Select(x => x.Split(':')[0])).Select(x => System.IO.Path.GetFileName(x.Split('/').Last())).Distinct().ToList();
            var primaoci = cfg.Rezim == Rezim.Test ? new List<string> { cfg.IzvestajAdresa }
                : cfg.KopijaNa.Concat(new[] { cfg.UpozorenjaNa }).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var naslov = "[INTERNO] Ručna provera – " + System.Text.RegularExpressions.Regex.Replace(m.Subject ?? "", @"\s+", " ").Trim();
            var sb = new System.Text.StringBuilder();
            if (cfg.Rezim == Rezim.Test) sb.AppendLine("[TEST] Interna poruka – ne ide pošiljaocu (" + od + ").").AppendLine();
            sb.AppendLine("Mail „" + m.Subject + "“ od " + od + ", " + m.Date.LocalDateTime.ToString("dd.MM.yyyy HH:mm") + ".").AppendLine();
            sb.AppendLine("Tovarni list nije automatski prepoznat. " + (neprepoznati.Count == 1 ? "Prilog " + neprepoznati[0] + " nije mogao da se pročita" : "Prilozi " + string.Join(", ", neprepoznati) + " nisu mogli da se pročitaju") + " – proverite ručno.");
            if (ishod.NisuProcitani.Count + ishod.NisaPouzdanost.Count > 0) sb.AppendLine("Razlog: " + string.Join("; ", ishod.NisuProcitani.Concat(ishod.NisaPouzdanost)) + ".");
            if (ishod.OpisPriloga.Count > 0) sb.AppendLine().AppendLine(Izvestaj.PriloziTekst(new RezultatProvere { OpisPriloga = ishod.OpisPriloga }, false));
            return (naslov, sb.ToString(), primaoci);
        }

        private void PosaljiInternoRucnaProvera(MimeMessage m, Ishod ishod, bool en)
        {
            var (naslov, telo, primaoci) = InternaRucnaProvera(cfg, m, ishod);
            if (primaoci.Count == 0) { log.Upozorenje("    interna poruka „proverite ručno“ nema primaoca (KopijaNa/UpozorenjaNa)."); return; }
            var poruka = Slanje.Nova(cfg, primaoci, null, naslov, telo, odgovorNa: m);
            Zabelezi("interna-rucna", poruka);
            if (dryRun) { log.Info("    [dry-run] interna poruka za " + string.Join(", ", primaoci) + ": " + naslov); Console.WriteLine(telo); return; }
            Posalji(null, poruka);
            log.Info("    interna poruka poslata na " + string.Join(", ", primaoci) + ": " + naslov);
        }

        /// <summary>Režim Test: poređenje kao poseban mail kada za novi mail nema izveštaja (npr. tovarni list sada nije pronađen).</summary>
        private void PosaljiPoredjenje(MimeMessage m, string tekst, string opis)
        {
            if (tekst == null || cfg.Rezim != Rezim.Test) return;
            var od = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
            var naslov = "[TEST] Poređenje – " + Regex(m.Subject) + " – " + opis;
            var telo = "[TEST] Mail od " + od + ", " + m.Date.LocalDateTime.ToString("dd.MM.yyyy HH:mm") + ": " + opis + "." + Environment.NewLine + Environment.NewLine + tekst;
            var poruka = Slanje.Nova(cfg, new[] { cfg.IzvestajAdresa }, null, naslov, telo, odgovorNa: m);
            poruka.Headers.Replace(Slanje.ZaglavljeVrste, "poredjenje");
            Zabelezi("poredjenje", poruka);
            if (dryRun) { log.Info("    [dry-run] poređenje: " + naslov); Console.WriteLine(telo); return; }
            try { Posalji(null, poruka); log.Info("    poređenje poslato na " + cfg.IzvestajAdresa + ": " + naslov); }
            catch (Exception ex) { log.Greska("    poređenje nije poslato: " + ex.Message); }
        }
    }
}
