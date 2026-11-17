using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MailKit;
using MimeKit;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Mail;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;
using Saobracaj.Common.Text;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>
    /// Jedan prolaz kroz sanduče: INBOX (dolazni) i Sent (odlazni). Svaki novi mail se obrađuje tačno jednom:
    /// prilozi se otvaraju jedan po jedan; ako nijedan nije CIM/CUV, mail se preskače. Ako jeste, šalje se izveštaj.
    /// </summary>
    internal sealed class AgentRad
    {
        private readonly AgentConfig cfg;
        private readonly Log log;
        private readonly bool dryRun;
        /// <summary>Režim Test: odeljak poređenja koji se dodaje izveštaju tekuće poruke.</summary>
        private string dodatak;
        /// <summary>Prilozi poruke koja se obrađuje (podloga za PDF primer).</summary>
        private List<(string ime, byte[] sadrzaj)> trenutniPrilozi = new List<(string, byte[])>();
        private bool poslatDodatak;

        public AgentRad(AgentConfig cfg, Log log, bool dryRun)
        {
            this.cfg = cfg; this.log = log; this.dryRun = dryRun; Izvestaj.Potpis = PotpisIzvestaja.Iz(cfg);
            Saobracaj.Common.Text.TekstUtil.UpozorenjeNormalizacije = t => log?.Upozorenje("    " + t);
        }

        public int Pokreni()
        {
            var stanje = Stanje.Ucitaj(cfg.StanjeFolder);
            // ključ je ponovo ispravan: sledeći kvar ključa se opet javlja
            if (WordUPdf.KljucUcitan) stanje.Upozorenja.Remove("syncfusion-kljuc");
            File.WriteAllText(Path.Combine(cfg.StanjeFolder, "heartbeat.txt"), DateTimeOffset.Now.ToString("o"));
            PravilaSkup pravila;
            try { pravila = PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, log); }
            catch (Exception ex) { log.Greska("Pravila nisu učitana", ex); Upozori(stanje, "pravila", "Pravila nisu učitana: " + ex.Message); return 1; }

            using var citac = new LokalniCitac(cfg, log, pravila.Kp);
            var obrada = new Obrada(pravila, citac, log, cfg.PragPouzdanosti) { RanijiListovi = () => IstorijaListova.Ucitaj(cfg.StanjeFolder) };
            int obradjeno = 0;
            try
            {
                log.Info("Program " + VerzijaPrograma + ", pravila " + pravila.Verzija + ".");
                if (stanje.NultaTackaNalog != null)
                    log.Info("Nulta tačka: vreme reseta " + (stanje.NultaTacka.HasValue ? stanje.NultaTacka.Value.ToString("dd.MM.yyyy HH:mm:ss zzz") : "–") + ", UID " + (stanje.NultaTackaUid > 0 ? stanje.NultaTackaUid + " (" + stanje.NultaTackaNalog + ", UIDVALIDITY " + stanje.NultaTackaUidValidity + ")" : "još nije zapisan (zapisuje se u ovom krugu)") + " – obrađuju se samo poruke primljene posle reseta.");
                log.Info("Sanduče: " + cfg.Nalog + " (From " + cfg.PosiljalacIzvestaja + ", Reply-To " + cfg.ReplyTo + (cfg.Rezim == Rezim.Test ? ", test primalac " + cfg.IzvestajAdresa : "") + ", režim " + cfg.Rezim + ")");
                using (var s = new Sanduce(cfg))
                {
                    s.Povezi();
                    var folderi = new List<(IMailFolder f, bool odlazni)> { (s.Dolazni(), false) };
                    if (cfg.CitajOdlazne) folderi.Add((s.Odlazni(), true));
                    var indeks = IndeksPrvihIzvestaja(s, stanje);
                    foreach (var (f, odlazni) in folderi)
                        obradjeno += ObradiFolder(f, odlazni, obrada, pravila, stanje, s, indeks);
                }
            }
            catch (Exception ex)
            {
                log.Greska("Rad sa sandučetom " + cfg.Nalog + " nije uspeo", ex);
                // greška celog kruga (prijava, mreža…): najviše jednom na sat, ne na svaka 2 minuta
                Upozori(stanje, "imap", "Agent ne može da pročita sanduče " + cfg.Nalog + ": " + ex.Message, 1);
                if (!dryRun) stanje.Sacuvaj();
                return 1;
            }
            DnevnaListaSifarnika(stanje);
            if (!dryRun) stanje.Sacuvaj();
            log.Info("Kraj: obrađeno " + obradjeno + " poruka; OCR strana " + citac.BrojOcrStrana + ".");
            return 0;
        }

        /// <summary>
        /// Režim Test: indeks prvih izveštaja (stanje\prvi_izvestaji.json). Posle reseta se sastavlja jednom, iz rezervne
        /// kopije stanja, logova i izveštaja agenta u sandučetu (samo čitanje). U režimu Live poređenja nema.
        /// </summary>
        private IndeksPrvih IndeksPrvihIzvestaja(Sanduce s, Stanje stanje)
        {
            if (cfg.Rezim != Rezim.Test) return null;
            if (stanje.PrviIzvestajiIzgraditi && !dryRun)
            {
                try
                {
                    var fold = new List<IMailFolder> { s.Dolazni() };
                    try { fold.Add(s.Odlazni()); } catch (Exception ex) { log.Upozorenje("Prvi izveštaji: folder poslatih nije dostupan: " + ex.Message); }
                    var ind = Poredjenje.Izgradi(cfg, stanje, fold, log);
                    ind.Sacuvaj(cfg.StanjeFolder);
                    stanje.PrviIzvestajiIzgraditi = false;
                    stanje.Sacuvaj();
                }
                catch (Exception ex) { log.Greska("Prvi izveštaji nisu sastavljeni (pokušava se u sledećem prolazu)", ex); }
            }
            return IndeksPrvih.Ucitaj(cfg.StanjeFolder);
        }

        /// <summary>Odeljak poređenja sa prvim izveštajem za iste fajlove (null kada nema prvog izveštaja).</summary>
        private string Poredi(IndeksPrvih indeks, Sanduce s, IMailFolder f, IEnumerable<string> fajlovi, RezultatProvere r, PravilaSkup pravila, bool en,
            string sadaOpis, long uid)
        {
            if (indeks == null) return null;
            try
            {
                var prvi = indeks.Za(fajlovi);
                if (prvi.Count == 0) return null;
                var lista = new List<Poredjenje.PrviZaFajl>();
                foreach (var (p, fl) in prvi)
                {
                    List<Poredjenje.Stavka> st = null;
                    try { st = Poredjenje.StavkePrvog(p, ime => s.Folder(ime)); }
                    catch (Exception ex) { log.Upozorenje("    prvi izveštaj (UID " + p.IzvestajUid + ") nije pročitan: " + ex.Message); }
                    lista.Add(new Poredjenje.PrviZaFajl { Prvi = p, Fajlovi = fl, StavkePre = st });
                }
                if (!f.IsOpen) f.Open(FolderAccess.ReadOnly);
                var imaR = r != null && r.ImaTovarniList;
                var tekst = Poredjenje.Tekst(lista, sadaOpis, imaR ? r.BrojGresaka : (int?)null, imaR ? r.BrojUpozorenja : (int?)null,
                    r?.Listovi.SelectMany(l => l.Nalazi).Select(n => n.Pravilo).Distinct(), imaR ? Izvestaj.TekstZaPoredjenje(r, en) : null, out var zaLog);
                log.Info("  UID " + uid + ": poređenje sa prvim izveštajem – " + zaLog);
                return tekst;
            }
            catch (Exception ex)
            {
                if (!f.IsOpen) f.Open(FolderAccess.ReadOnly);
                log.Upozorenje("  UID " + uid + ": poređenje sa prvim izveštajem nije uspelo: " + ex.Message);
                return null;
            }
        }

        /// <summary>Režim Test: poređenje kao poseban mail kada za novi mail nema izveštaja (npr. tovarni list sada nije pronađen).</summary>
        private void PosaljiPoredjenje(MimeMessage m, string tekst, string opis)
        {
            if (tekst == null || cfg.Rezim != Rezim.Test) return;
            var od = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
            var naslov = "[TEST] Poređenje – " + Regex(m.Subject) + " – " + opis;
            var telo = "[TEST] Mail od " + od + ", " + m.Date.LocalDateTime.ToString("dd.MM.yyyy HH:mm") + ": " + opis + "." + Environment.NewLine + Environment.NewLine + tekst;
            var poruka = Slanje.Nova(cfg, new[] { cfg.IzvestajAdresa }, null, naslov, telo, odgovorNa: m);
            poruka.Headers.Replace(Poredjenje.ZaglavljeVrste, "poredjenje");
            if (dryRun) { log.Info("    [dry-run] poređenje: " + naslov); Console.WriteLine(telo); return; }
            try { Slanje.Posalji(cfg, poruka); log.Info("    poređenje poslato na " + cfg.IzvestajAdresa + ": " + naslov); }
            catch (Exception ex) { log.Greska("    poređenje nije poslato: " + ex.Message); }
        }

        private static string Regex(string s) => System.Text.RegularExpressions.Regex.Replace(s ?? "", @"\s+", " ").Trim();

        /// <summary>Verzija programa (csproj &lt;Version&gt;), npr. „2.8.1“.</summary>
        internal static string VerzijaPrograma
        {
            get
            {
                var v = typeof(AgentRad).Assembly.GetName().Version;
                return v == null ? "?" : v.Major + "." + v.Minor + "." + v.Build;
            }
        }

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

        private int ObradiFolder(IMailFolder f, bool odlazni, Obrada obrada, PravilaSkup pravila, Stanje stanje, Sanduce sanduce, IndeksPrvih indeks)
        {
            f.Open(FolderAccess.ReadOnly);
            // stanje po sanduču (v2.7.1): „agenti@…|INBOX“ – zapisi drugog sanduča (npr. ranije stefan.obradovic@) se ne koriste
            var fs = stanje.FolderSanduceta(cfg.Nalog, f.FullName, DateTimeOffset.Now);
            if (fs.UidValidity != f.UidValidity)
            {
                if (fs.UidValidity != 0) log.Upozorenje("UIDVALIDITY foldera " + f.FullName + " se promenio – čita se od ImportOd, duplikati se prepoznaju po Message-ID.");
                fs.UidValidity = f.UidValidity;
                fs.PoslednjiUid = 0;
            }
            // v2.8.2 (posle reset-potpun): nulta tačka po UID-u – ako je reset nije mogao da zapiše (lozinka samo za nalog zadatka),
            // zapisuje se sada: najveći UID poruka koje su stigle PRE vremena reseta; poruke sa tim i manjim UID-om se ne obrađuju
            if (!odlazni && stanje.NultaTacka.HasValue && stanje.NultaTackaUid == 0 && fs.PoslednjiUid == 0
                && stanje.NultaTackaNalog != null && string.Equals(stanje.NultaTackaNalog, cfg.Nalog, StringComparison.OrdinalIgnoreCase))
            {
                var svi = f.Search(MailKit.Search.SearchQuery.All).OrderBy(u => u.Id).ToList();
                var poslednji = svi.Skip(Math.Max(0, svi.Count - 300)).ToList();
                var noviji = new HashSet<uint>();
                if (poslednji.Count > 0)
                    foreach (var x in f.Fetch(poslednji, new FetchRequest(MessageSummaryItems.InternalDate)))
                        if (x.InternalDate.HasValue && x.InternalDate.Value >= stanje.NultaTacka.Value) noviji.Add(x.UniqueId.Id);
                var maxStari = svi.Where(u => !noviji.Contains(u.Id)).Select(u => (long)u.Id).DefaultIfEmpty(0).Max();
                fs.UidValidity = f.UidValidity; fs.PoslednjiUid = maxStari;
                stanje.NultaTackaUid = maxStari; stanje.NultaTackaUidValidity = f.UidValidity; stanje.NultaTackaNalog = cfg.Nalog;
                if (!dryRun) stanje.Sacuvaj();
                log.Info("Nulta tačka zapisana: vreme reseta " + stanje.NultaTacka.Value.ToString("dd.MM.yyyy HH:mm:ss zzz") + ", UID " + maxStari + " (" + cfg.Nalog + " / " + f.FullName + ", UIDVALIDITY " + f.UidValidity
                         + ") = najveći UID poruka primljenih pre reseta (INTERNALDATE); te poruke se ne obrađuju, a svaki mail primljen posle reseta se obrađuje (" + noviji.Count + " već čeka).");
            }
            var nulta = new[] { stanje.NultaTacka, fs.NultaTacka }.Where(x => x.HasValue).Select(x => x.Value).DefaultIfEmpty().Max() is var nt && nt != default(DateTimeOffset) ? nt : (DateTimeOffset?)null;
            var od0 = nulta.HasValue && nulta.Value.LocalDateTime > cfg.ImportOd ? nulta.Value.LocalDateTime : cfg.ImportOd;
            var uids = Sanduce.Nove(f, fs.PoslednjiUid, od0);
            if (nulta.HasValue && fs.PoslednjiUid == 0 && uids.Count > 0)
            {
                // Posle reseta: poruke koje su stigle pre nulte tačke se ne obrađuju (ni ne preuzimaju).
                var datumi = f.Fetch(uids, new FetchRequest(MessageSummaryItems.InternalDate | MessageSummaryItems.Envelope))
                    .ToDictionary(x => x.UniqueId, x => x.InternalDate ?? x.Envelope?.Date);
                var stare = new HashSet<UniqueId>(uids.Where(u => !datumi.TryGetValue(u, out var d) || d == null || d.Value < nulta.Value));
                foreach (var u in uids) { if (!stare.Contains(u)) break; fs.PoslednjiUid = u.Id; }
                if (stare.Count > 0) log.Info(f.FullName + ": " + stare.Count + " poruka pre nulte tačke (" + nulta.Value.ToString("dd.MM.yyyy HH:mm:ss") + ") se ne obrađuje.");
                uids = uids.Where(u => !stare.Contains(u)).ToList();
            }
            log.Info(f.FullName + " (" + (odlazni ? "odlazni" : "dolazni") + "): novih poruka " + uids.Count);
            if (uids.Count == 0) return 0;

            // sažeci (zaglavlje, imena priloga) bez preuzimanja celih poruka: za log pre obrade i za već obrađene poruke
            var sazeci = new Dictionary<UniqueId, IMessageSummary>();
            try
            {
                foreach (var x in f.Fetch(uids, new FetchRequest(MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.BodyStructure)))
                    sazeci[x.UniqueId] = x;
            }
            catch (Exception ex) { log.Upozorenje(f.FullName + ": zaglavlja poruka nisu preuzeta (" + ex.Message + ") – Message-ID se čita iz poruke."); }
            var red = new List<RedPoruka.Stavka>();
            foreach (var u in uids)
            {
                sazeci.TryGetValue(u, out var sz);
                var mid = TekstUtil.Ocisti(sz?.Envelope?.MessageId ?? "").Trim().Trim('<', '>');
                if (mid.Length == 0 && sz == null)
                    try { mid = f.GetMessage(u).MessageId ?? ""; } catch { }
                List<string> imena;
                try { imena = sz?.Attachments?.Select(a => TekstUtil.Ocisti(a.FileName ?? a.ContentType?.Name ?? "(bez imena)")).ToList() ?? new List<string>(); }
                catch { imena = new List<string>(); }
                // v2.8.3: procena trajanja (PDF/slike i veličina) – teški mail ide na kraj kruga
                int pdfSlika = 0; long bajtova = 0;
                try
                {
                    foreach (var a in sz?.Attachments ?? Enumerable.Empty<BodyPartBasic>())
                    {
                        var ime = (a.FileName ?? a.ContentType?.Name ?? "").ToLowerInvariant();
                        var vrsta = (a.ContentType?.MimeType ?? "").ToLowerInvariant();
                        if (vrsta.Contains("pdf") || vrsta.StartsWith("image/") || System.Text.RegularExpressions.Regex.IsMatch(ime, @"\.(pdf|jpe?g|png|tiff?)$"))
                        { pdfSlika++; bajtova += (long)a.Octets; }
                    }
                }
                catch { }
                red.Add(new RedPoruka.Stavka
                {
                    Uid = u.Id, Folder = f.FullName, MessageId = mid,
                    Kljuc = Stanje.KljucPoruke(cfg.Nalog, mid.Length > 0 ? mid : f.FullName + "/" + f.UidValidity + "/" + u.Id),
                    Od = sz?.Envelope?.From?.Mailboxes.FirstOrDefault()?.Address ?? "",
                    Naslov = Regex(TekstUtil.Ocisti(sz?.Envelope?.Subject)),
                    Prilozi = imena, PdfSlika = pdfSlika, Bajtova = bajtova,
                });
            }
            var q = new RedPoruka(stanje, fs, cfg.MaxPokusaja, log, dryRun)
            {
                // jednom po poruci, interno (u test režimu agenti@), nikad pošiljaocu
                PosleZadnjegPokusaja = (s, ps) => Upozori(stanje, "poruka-" + s.Kljuc,
                    "Mail UID " + s.Uid + " (" + s.Folder + ") od " + s.Od + ", naslov „" + s.Naslov + "“, Message-ID " + s.MessageId + Environment.NewLine
                    + "nije obrađen posle " + ps.Pokusaja + " pokušaja. Status: greška – ručna provera; agent ga više ne pokušava. Pošiljaocu nije ništa poslato." + Environment.NewLine
                    + "Prilozi: " + (s.Prilozi.Count == 0 ? "nema" : string.Join(", ", s.Prilozi)) + Environment.NewLine
                    + "Greška: " + ps.PoslednjaGreska, 24 * 365),
            };
            return q.Obradi(red, (s, ps) => ObradiPoruku(f, s, ps, odlazni, obrada, pravila, stanje, sanduce, indeks));
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

        private Korak ObradiPoruku(IMailFolder f, RedPoruka.Stavka s, Stanje.PorukaStanje ps, bool odlazni, Obrada obrada, PravilaSkup pravila, Stanje stanje, Sanduce sanduce, IndeksPrvih indeks)
        {
            var uid = new UniqueId(f.UidValidity, (uint)s.Uid);
            var kljuc = s.Kljuc;
            if (!f.IsOpen) f.Open(FolderAccess.ReadOnly);
            var m = f.GetMessage(uid);
            {
                var razlog = Slanje.RazlogPreskakanja(m, cfg, !odlazni);
                if (razlog != null)
                {
                    log.Info("  UID " + uid.Id + ": " + (razlog.StartsWith("sopstveni") ? razlog + " – preskočeno" : "preskočeno – " + razlog));
                    ps.Status = "Preskoceno"; return Korak.Gotova;
                }
                var prilozi = Raspakivanje.PriloziMaila(m).ToList();
                trenutniPrilozi = prilozi;
                if (prilozi.Count == 0)
                {
                    log.Info("  UID " + uid.Id + ": mail nema priloga – preskočeno (BezPriloga).");
                    ps.Status = "BezPriloga"; return Korak.Gotova;
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
                    var iu = UpisiUBazu(stanje, pravila, m, uid.Id, f.FullName, rr, prilozi, obrada.PoslednjaPosiljka);
                    // v2.9.2 (3.3): mail bez novog izveštaja (isti tovarni list, nova verzija spiska) koji je ažurirao posao – u režimu Test
                    // kratka interna poruka šta je ažurirano (pošiljaocu ništa)
                    if (!prijavljeno && iu != null && iu.Promene.Count > 0) PosaljiInternoAzurirano(m, iu);
                }
                var istiPrilog = ponavljanja.IstiPrilozi(prilozi);
                if (istiPrilog != null)
                {
                    log.Info("  UID " + uid.Id + ": isti prilog kao " + istiPrilog.Oznaka + " (" + istiPrilog.Vreme.ToString("dd.MM. HH:mm") + ", " + istiPrilog.Ishod + ") – već obrađeno; ništa se ne šalje.");
                    // v2.9.1 (5.7): prijem se ipak beleži u tmpDokument, a tekst maila ide u Napomenu posla
                    var iu0 = UpisiUBazu(stanje, pravila, m, uid.Id, f.FullName, null, prilozi, null);
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
                try { ishod = obrada.Proveri(prilozi, od, cfg.JeInterni(od), datum, en); }
                catch (Exception ex)
                {
                    log.Greska("  UID " + uid.Id + ": obrada nije uspela", ex);
                    ishod = new Ishod { PrivremenaGreska = true, NisuProcitani = { ex.Message } };
                }
                if (WordUPdf.NedostajaoKljuc && !WordUPdf.KljucUcitan)
                {
                    // jedno interno obaveštenje (dok se ključ ne podesi); Word prilog je već na ručnoj proveri
                    Upozori(stanje, "syncfusion-kljuc", "Word prilog u mailu „" + m.Subject + "“ od " + od + " nije pretvoren u PDF: Syncfusion licencni ključ nije učitan ("
                        + WordUPdf.StanjeKljuca + "). Prilog ide na ručnu proveru. Pokrenite Instaliraj.ps1 i unesite Syncfusion ključ.", 24 * 365);
                    WordUPdf.NedostajaoKljuc = false;
                }
                foreach (var x in ishod.Preskoceno) log.Info("    preskočen prilog: " + x);
                // Režim Test: poređenje sa prvim izveštajem za iste fajlove (prilozi i fajlovi iz ZIP-a).
                string Por(RezultatProvere rr, string opis) => Poredi(indeks, sanduce, f,
                    prilozi.Select(p => p.ime).Concat(rr?.Dokumenti.Select(d => d.Fajl) ?? Enumerable.Empty<string>()), rr, pravila, en, opis, uid.Id);

                if (ishod.PrivremenaGreska)
                {
                    // ponovni pokušaj / ručna provera posle MaxPokusaja: RedPoruka
                    ps.PoslednjaGreska = string.Join("; ", ishod.NisuProcitani);
                    return Korak.PrivremenaGreska;
                }

                var r = ishod.Provera;
                // v2.8.1: CIT 7 – interni izveštaj (veza sa listom, izmene, upozorenja); pošiljaocu ništa
                if (r.Cit7.Count > 0 && !ps.Cit7Poslat)
                    try { PosaljiCit7(m, r, pravila); if (!dryRun) ps.Cit7Poslat = true; }
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
                    Upozori(stanje, "ponuda-baza", "Broj ponude iz tovarnog lista nije proveren u dbo.Ponudba (baza nedostupna ili nalog svc_cimprovera nema pravo čitanja – pokrenuti OP_913_tmp_Prava.sql). Mailovi sa brojem ponude se ponavljaju u sledećem prolazu.", 24);
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
                try { PosaljiIzvestaj(m, r, pravila, en, jeOdlazni, ishod.NisuProcitani); }
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

        /// <summary>v2.9.0: jedan upis po prolazu (šifarnici se učitavaju jednom).</summary>
        private Baza.UpisUBazu upis;

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
                var telo = "INTERNO – ažuriran posao (pošiljaocu ništa). Mail „" + m.Subject + "“ (" + od + ", " + m.Date.LocalDateTime.ToString("dd.MM. HH:mm") + "): tovarni list je isti kao ranije, nova verzija priloga je ažurirala posao:"
                           + Environment.NewLine + string.Join(Environment.NewLine, iu.Promene.Take(30).Select(x => "  - " + x))
                           + (iu.Napomene.Count > 0 ? Environment.NewLine + Environment.NewLine + "Napomene: " + string.Join("; ", iu.Napomene.Distinct().Take(10)) : "");
                Slanje.Posalji(cfg, Slanje.Nova(cfg, new[] { cfg.IzvestajAdresa }, null, "[INTERNO] Ažuriran posao – " + m.Subject, telo));
                log.Info("  interna poruka o ažuriranom poslu poslata (" + iu.Promene.Count + " izmena).");
            }
            catch (Exception ex) { log.Greska("  interna poruka o ažuriranom poslu nije poslata: " + ex.Message); }
        }

        private Baza.IshodUpisa UpisiUBazu(Stanje stanje, PravilaSkup pravila, MimeMessage m, long uid, string folder, RezultatProvere r, List<(string ime, byte[] sadrzaj)> prilozi, Dosije.Posiljka dosije)
        {
            if (!cfg.UpisUBazu || dryRun) return null;   // r == null: ponovljeni mail (isti prilozi) – samo prijem i Napomena
            if (string.IsNullOrEmpty(cfg.Konekcija))
            {
                log.Upozorenje("  UID " + uid + ": upis u bazu je uključen, ali konekcija nije podešena" + (AgentConfig.OtvorenaKonekcija ? " (ključ Konekcija u otvorenom tekstu se ne koristi – samo Konekcija.dpapi)" : "") + " – preskočeno.");
                Upozori(stanje, "upis-baza", "Upis u bazu (UpisUBazu = true) nije podešen: nema konekcije (Konekcija / Konekcija.dpapi / CIMPROVERA_KONEKCIJA). Provera i izveštaji rade normalno.", 24);
                return null;
            }
            try
            {
                upis = upis ?? new Baza.UpisUBazu(cfg.Konekcija, log, pravila.Kp);
                var ishod = upis.Upisi(new Baza.MailZaUpis
                {
                    MessageId = m.MessageId ?? "", Uid = uid, Sanduce = cfg.Nalog + "|" + folder, Posiljalac = m.From.Mailboxes.FirstOrDefault()?.Address ?? "",
                    Naslov = m.Subject ?? "", Primljeno = m.Date == DateTimeOffset.MinValue ? DateTime.Now : m.Date.LocalDateTime, Telo = TeloMaila(m),
                    Prilozi = prilozi, Provera = r, Dosije = dosije,
                });
                log.Info("  UID " + uid + ": upis u bazu – " + ishod);
                return ishod;
            }
            catch (Exception ex)
            {
                log.Greska("  UID " + uid + ": upis u bazu nije uspeo (rollback) – provera i izveštaj nisu promenjeni", ex);
                Upozori(stanje, "upis-baza", "Upis u privremene tabele (planiranje.tmp*) nije uspeo za mail UID " + uid + " „" + m.Subject + "“: " + ex.Message
                    + Environment.NewLine + "Transakcija je vraćena (rollback). Provera i izveštaj su poslati normalno. Detalji su u logu.", 6);
                return null;
            }
        }

        private void PosaljiIzvestaj(MimeMessage m, RezultatProvere r, PravilaSkup pravila, bool en, bool odlazni, List<string> nisuProcitani)
        {
            // Nalazi "samo interno" (npr. r49 koju popunjava KP kao prevoznik) ne idu spoljnom pošiljaocu: on dobija izveštaj
            // bez njih, a puni izveštaj ide kao interna kopija (KopijaNa). U režimu Test sve ide na IzvestajAdresa.
            var od0 = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
            // v2.8: list koji je izdao KP (r62 = 3212) ili list na ručnoj proveri → samo interni izveštaj, nikad pošiljaocu
            var razlog = Izvestaj.RazlogInternog(r, pravila);
            if (razlog != null) { PosaljiInterniIzvestaj(m, r, pravila, nisuProcitani, razlog); return; }
            // v2.8.3: Live – nalazi „samo interno“ (npr. R49, r8, kontakt) nikad u mailu ni u PDF primeru za pošiljaoca (puni izveštaj samo KopijaNa);
            // Test – mail ide na test adresu sa internim odeljkom na dnu („INTERNO (ne ide pošiljaocu)“). PDF primer ih nikad nema.
            if (!odlazni && r.ImaInternih && Izvestaj.ZaPosiljaoca(r, cfg.Rezim, cfg.JeInterni(od0)) != r)
            {
                PosaljiIzvestaj(m, r.BezInternih(), pravila, en, false, nisuProcitani, bezKopije: true);
                if (cfg.Rezim == Rezim.Live && cfg.KopijaNa.Count > 0) PosaljiInterno(m, r, pravila, en, nisuProcitani);
                return;
            }
            PosaljiIzvestaj(m, r, pravila, en, odlazni, nisuProcitani, false);
        }

        /// <summary>Interni izveštaj (KP tovarni list ili ručna provera): Test – IzvestajAdresa; Live – KopijaNa i UpozorenjaNa. Uvek na srpskom.</summary>
        private void PosaljiInterniIzvestaj(MimeMessage m, RezultatProvere r, PravilaSkup pravila, List<string> nisuProcitani, string razlog)
        {
            var od = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
            var primaoci = cfg.Rezim == Rezim.Test ? new List<string> { cfg.IzvestajAdresa }
                : cfg.KopijaNa.Concat(new[] { cfg.UpozorenjaNa }).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (primaoci.Count == 0) { log.Upozorenje("    interni izveštaj nema primaoca (KopijaNa/UpozorenjaNa)."); return; }
            var naslov = Izvestaj.Naslov(r, pravila, false);
            // v2.8.3: kratak interni uvod – nije pismo pošiljaocu (bez „Poštovani“, zahvalnice i potpisa)
            var uvod = (cfg.Rezim == Rezim.Test ? "[TEST] " : "") + Izvestaj.UvodInternogKratak(razlog, r, od, m.Subject, m.Date.LocalDateTime);
            var primer = r.BrojGresaka + r.BrojUpozorenja > 0 ? NapraviPrimer(r, pravila, false) : null;
            var telo = Izvestaj.Telo(r, pravila, false, uvod, nisuProcitani, Izvestaj.PasusPrimera(primer, false, true), interni: true);
            var html = Izvestaj.Html(r, pravila, false, uvod, nisuProcitani, Izvestaj.PasusPrimera(primer, false, true), interni: true);
            var poruka = Slanje.Nova(cfg, primaoci, null, naslov, telo, odgovorNa: m, html: html);
            // v2.8.4 (10.3): interni izveštaj nikad ne ide spoljnim adresama – u Live samo KopijaNa / UpozorenjaNa
            if (cfg.Rezim == Rezim.Test) poruka.Headers.Add(PrimaociLive.ZaglavljeLive, "samo interno (" + string.Join(", ", cfg.KopijaNa.Concat(new[] { cfg.UpozorenjaNa }).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)) + ")");
            if (primer?.Napravljen == true) Slanje.DodajPrilog(poruka, primer.Ime, primer.Pdf);
            if (dryRun) { log.Info("    [dry-run] interni izveštaj za " + string.Join(", ", primaoci) + ": " + naslov); Console.WriteLine(telo); return; }
            Slanje.Posalji(cfg, poruka);
            log.Info("    interni izveštaj (" + (razlog == Izvestaj.RazlogKp ? "KP tovarni list, r62 = 3212" : "ručna provera") + ") poslat na " + string.Join(", ", primaoci) + ": " + naslov);
        }

        /// <summary>v2.8.1: interni izveštaj o CIT 7 (Test – IzvestajAdresa; Live – KopijaNa i UpozorenjaNa). Uvek na srpskom, nikad pošiljaocu.</summary>
        private void PosaljiCit7(MimeMessage m, RezultatProvere r, PravilaSkup pravila)
        {
            var od = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
            var primaoci = cfg.Rezim == Rezim.Test ? new List<string> { cfg.IzvestajAdresa }
                : cfg.KopijaNa.Concat(new[] { cfg.UpozorenjaNa }).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (primaoci.Count == 0) { log.Upozorenje("    interni izveštaj o CIT 7 nema primaoca (KopijaNa/UpozorenjaNa)."); return; }
            var naslov = Cit7Provera.Naslov(r.Cit7, m.Subject);
            var uvod = (cfg.Rezim == Rezim.Test ? "[TEST] " : "") + "Interni izveštaj – ne ide pošiljaocu (" + od + "). Mail „" + m.Subject + "“, " + m.Date.LocalDateTime.ToString("dd.MM.yyyy HH:mm") + ".";
            var poruka = Slanje.Nova(cfg, primaoci, null, naslov, Cit7Provera.Tekst(r.Cit7, pravila, uvod), odgovorNa: m, html: Cit7Provera.Html(r.Cit7, pravila, uvod));
            if (dryRun) { log.Info("    [dry-run] interni izveštaj o CIT 7 za " + string.Join(", ", primaoci) + ": " + naslov); return; }
            Slanje.Posalji(cfg, poruka);
            log.Info("    interni izveštaj o CIT 7 poslat na " + string.Join(", ", primaoci) + ": " + naslov + " (" + string.Join("; ", r.Cit7.Select(c => (c.Veza != null ? "veza " + System.IO.Path.GetFileName(c.Veza.Fajl) : "list nije pronađen") + ", upozorenja " + c.Upozorenja.Count)) + ")");
        }

        /// <summary>Puni izveštaj (sa nalazima "samo interno") za KopijaNa, bez odgovora pošiljaocu.</summary>
        private void PosaljiInterno(MimeMessage m, RezultatProvere r, PravilaSkup pravila, bool en, List<string> nisuProcitani)
        {
            var naslov = "[INTERNO] " + Izvestaj.Naslov(r, pravila, en);
            var uvod = "Interna kopija za KP: izveštaj za mail „" + m.Subject + "“ od " + (m.From.Mailboxes.FirstOrDefault()?.Address ?? "") + ". Nalazi u odeljku „INTERNO (ne ide pošiljaocu)“ nisu poslati pošiljaocu.";
            var poruka = Slanje.Nova(cfg, cfg.KopijaNa, null, naslov, Izvestaj.Telo(r, pravila, en, uvod, nisuProcitani), odgovorNa: m, html: Izvestaj.Html(r, pravila, en, uvod, nisuProcitani));
            if (dryRun) { log.Info("    [dry-run] interna kopija za " + string.Join(", ", cfg.KopijaNa) + ": " + naslov); return; }
            Slanje.Posalji(cfg, poruka);
            log.Info("    interna kopija poslata na " + string.Join(", ", cfg.KopijaNa) + ": " + naslov);
        }

        /// <summary>
        /// PDF primer ispravnog lista (B2). Greška pri pravljenju ne zaustavlja slanje: mail ide bez priloga, greška u log.
        /// Režim Test: kopija u RezultatiFolder\yyyy-MM-dd pored JSON-a i slika okvira.
        /// </summary>
        private PrimerRezultat NapraviPrimer(RezultatProvere r, PravilaSkup pravila, bool en)
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
            return pr;
        }

        /// <summary>Test: lista primalaca u Live režimu za zaglavlje test maila (X-CimProvera-Live-Primaoci).</summary>
        private string testLive;

        private void PosaljiIzvestaj(MimeMessage m, RezultatProvere r, PravilaSkup pravila, bool en, bool odlazni, List<string> nisuProcitani, bool bezKopije)
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
                    if (cfg.KopijaNa.Count > 0) PosaljiInterno(m, r, pravila, en, nisuProcitani);
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
            var primer = imaNalaza ? NapraviPrimer(r, pravila, en) : null;
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
            testLive = null;
            if (primer?.Napravljen == true) Slanje.DodajPrilog(poruka, primer.Ime, primer.Pdf);
            if (dryRun)
            {
                log.Info("    [dry-run] izveštaj za " + string.Join(", ", primaoci) + ": " + naslov);
                Console.WriteLine(Environment.NewLine + "=== " + naslov + " ===" + Environment.NewLine + telo);
                return;
            }
            Slanje.Posalji(cfg, poruka);
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
            if (dryRun) { log.Info("    [dry-run] interna poruka za " + string.Join(", ", primaoci) + ": " + naslov); Console.WriteLine(telo); return; }
            Slanje.Posalji(cfg, poruka);
            log.Info("    interna poruka poslata na " + string.Join(", ", primaoci) + ": " + naslov);
        }

        /// <summary>Obaveštenje: u mailu je spisak kola (CIT 23), a tovarni list CIM/CUV nije pronađen.</summary>
        private void PosaljiSamoSpisak(MimeMessage m, List<Dokument> spiskovi, PravilaSkup pravila, bool en, bool odlazni, List<string> opisPriloga = null)
        {
            var od = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
            var za = string.Join(", ", m.To.Mailboxes.Select(x => x.Address));
            var datum = m.Date.LocalDateTime.ToString("dd.MM.yyyy HH:mm");
            var poruka = pravila.Sistemska("samo_spisak", en, "U mailu je dostavljen spisak kola (CIT 23), ali nije pronađen tovarni list CIM/CUV.");
            var fajlovi = string.Join(", ", spiskovi.Select(d => d.Fajl).Distinct());
            var naslov = Izvestaj.NaslovMaila(m.Subject, spiskovi.Select(d => d.Fajl), en);
            var uvod = (en ? "Mail „" : "Mail „") + m.Subject + "“ (" + (en ? "from " : "od ") + od + ", " + datum + "):";
            var primaoci = odlazni ? new List<string> { cfg.Nalog } : new List<string> { od };
            var kopija = odlazni ? new List<string>() : cfg.KopijaNa;
            if (cfg.Rezim == Rezim.Test)
            {
                uvod = "[TEST] " + (odlazni ? "Odlazni" : "Dolazni") + " mail od " + od + " za " + za + ", " + datum + ". U režimu Live obaveštenje bi išlo na: "
                       + string.Join(", ", primaoci.Concat(kopija)) + "." + Environment.NewLine + Environment.NewLine + uvod;
                primaoci = new List<string> { cfg.IzvestajAdresa };
                kopija = new List<string>();
            }
            var prilozi = opisPriloga != null && opisPriloga.Count > 0 ? Izvestaj.PriloziTekst(new RezultatProvere { OpisPriloga = opisPriloga }, en) : (en ? "Wagon list: " : "Spisak kola: ") + fajlovi;
            var telo = uvod + Environment.NewLine + Environment.NewLine + poruka + Environment.NewLine + prilozi
                       + Environment.NewLine + Environment.NewLine + "--" + Environment.NewLine + pravila.Sablon("footer", en, "") + " " + (en ? "Rules version " : "Verzija pravila ") + pravila.Verzija + ".";
            var html = "<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:13px\"><p>" + System.Net.WebUtility.HtmlEncode(uvod).Replace("\n", "<br>") + "</p>"
                       + "<p style=\"color:#b00020\"><b>" + System.Net.WebUtility.HtmlEncode(poruka) + "</b></p><p>" + System.Net.WebUtility.HtmlEncode(prilozi) + "</p></div>";
            var mail = Slanje.Nova(cfg, primaoci, kopija, naslov, telo, odgovorNa: m, html: html);
            if (dryRun) { log.Info("    [dry-run] obaveštenje za " + string.Join(", ", primaoci) + ": " + naslov); return; }
            Slanje.Posalji(cfg, mail);
            log.Info("    obaveštenje poslato na " + string.Join(", ", primaoci.Concat(kopija)) + ": " + naslov);
        }

        /// <summary>
        /// Interno upozorenje (UpozorenjaNa; prazno = IzvestajAdresa, u test režimu agenti@), istim nalogom i lozinkom kao
        /// izveštaji (Slanje.Posalji). Isto upozorenje najviše jednom na <paramref name="sati"/> sati; neuspelo slanje se
        /// ponavlja posle 10 minuta i svaki ishod se vidi u logu.
        /// </summary>
        /// <summary>
        /// v2.9.1 (5.3): partneri koje treba dopuniti u šifarniku (nije nađen u Partnerji / nema UIC / nema oznaku Prevoznik) – skupljaju se
        /// u stanje\za_sifarnik.txt i šalju interno najviše jednom dnevno; SRB kodovi stanica bez šifre → sifarnici\predlog_kodova_stanica.csv.
        /// </summary>
        private void DnevnaListaSifarnika(Stanje stanje)
        {
            if (dryRun || upis == null) return;
            try
            {
                Baza.UpisUBazu.SacuvajPredlogKodova(upis.KodoviBezSifre, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sifarnici", "predlog_kodova_stanica.csv"));
                var put = Path.Combine(cfg.StanjeFolder, "za_sifarnik.txt");
                var sve = new SortedSet<string>(File.Exists(put) ? File.ReadAllLines(put, System.Text.Encoding.UTF8) : new string[0], StringComparer.Ordinal);
                sve.UnionWith(upis.ZaSifarnik);
                if (sve.Count == 0) return;
                File.WriteAllLines(put, sve, System.Text.Encoding.UTF8);
                if (!stanje.SmeUpozorenje("sifarnik-dnevno", 24)) return;
                if (Upozori(stanje, "sifarnik-dnevno", "Šifarnik partnera (dbo.Partnerji) – predlog za dopunu (interno, jednom dnevno):" + Environment.NewLine
                        + string.Join(Environment.NewLine, sve.Select(x => "- " + x)) + Environment.NewLine + Environment.NewLine + "Agent šifarnik ne menja.", 24))
                    File.Delete(put);
            }
            catch (Exception ex) { log.Upozorenje("Lista za šifarnik nije sačuvana: " + ex.Message); }
        }

        private bool Upozori(Stanje stanje, string kljuc, string tekst, int sati = 6)
        {
            if (dryRun) return false;
            if (string.IsNullOrEmpty(cfg.UpozorenjaNa)) { log.Upozorenje("Upozorenje „" + kljuc + "“ nema primaoca (UpozorenjaNa/IzvestajAdresa)."); return false; }
            if (!stanje.SmeUpozorenje(kljuc, sati))
            {
                log.Info("Upozorenje „" + kljuc + "“ se ne šalje ponovo – poslednji pokušaj " + stanje.Upozorenja[kljuc].ToString("dd.MM. HH:mm") + " (najviše jednom na " + sati + " h).");
                return false;
            }
            try
            {
                Slanje.Posalji(cfg, Slanje.Nova(cfg, new[] { cfg.UpozorenjaNa }, null, "[CimProvera] Upozorenje: " + kljuc, tekst
                    + Environment.NewLine + Environment.NewLine + "Server: " + Environment.MachineName + ", " + DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm zzz")));
                stanje.ZapisiUpozorenje(kljuc, true, sati);
                log.Info("Upozorenje „" + kljuc + "“ poslato na " + cfg.UpozorenjaNa + ".");
                return true;
            }
            catch (Exception ex)
            {
                stanje.ZapisiUpozorenje(kljuc, false, sati);
                log.Greska("Upozorenje „" + kljuc + "“ nije poslato mailom (ponovni pokušaj za 10 min): " + ex.Message);
                return false;
            }
        }
    }
}
