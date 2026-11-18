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
        private ObradaPoruke obradaPoruke;

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
            // v3.0: tok obrade poruke je u jezgru (ObradaPoruke) – isti za agenta, CimRadionica i ponovnu obradu
            obradaPoruke = new ObradaPoruke(cfg, log, obrada, pravila, stanje, dryRun) { UpozoriSpolja = (k, t, h) => Upozori(stanje, k, t, h) };
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


        private int ObradiFolder(IMailFolder f, bool odlazni, Obrada obrada, PravilaSkup pravila, Stanje stanje, Sanduce sanduce, IndeksPrvih indeks)
        {
            f.Open(FolderAccess.ReadOnly);
            // stanje po sanduču (v2.7.1): „agenti@…|INBOX“ – zapisi drugog sanduča (npr. ranije stefan.obradovic@) se ne koriste
            var fs = stanje.FolderSanduceta(cfg.Nalog, f.FullName, DateTimeOffset.Now);
            if (fs.UidValidity != f.UidValidity)
            {
                if (fs.UidValidity != 0)
                {
                    log.Upozorenje("UIDVALIDITY foldera " + f.FullName + " se promenio – čita se od ImportOd, duplikati se prepoznaju po Message-ID.");
                    // v3.0 (2.9.3 tačka 4.3): INTERNO – folder je obrisan i napravljen ponovo (ili ispražnjen na serveru)
                    Upozori(stanje, "uidvalidity-" + f.FullName, "UIDVALIDITY foldera " + f.FullName + " se promenio (" + fs.UidValidity + " → " + f.UidValidity + "). Agent ne obrađuje ponovo već obrađene poruke "
                        + "(prepoznaju se po Message-ID i hešu priloga) i ne preskače nove; čita od nulte tačke / ImportOd. Arhiva i dosije ostaju ispravni.", 24);
                }
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
            return q.Obradi(red, (s, ps) => ObradiPoruku(f, s, ps, odlazni, pravila, sanduce, indeks));
        }

        /// <summary>v3.0: poruka iz IMAP foldera (samo čitanje) → tok obrade u jezgru; poređenje sa prvim izveštajem (Test) ostaje u agentu.</summary>
        private Korak ObradiPoruku(IMailFolder f, RedPoruka.Stavka s, Stanje.PorukaStanje ps, bool odlazni, PravilaSkup pravila, Sanduce sanduce, IndeksPrvih indeks)
        {
            var uid = new UniqueId(f.UidValidity, (uint)s.Uid);
            if (!f.IsOpen) f.Open(FolderAccess.ReadOnly);
            var m = f.GetMessage(uid);
            // v3.0 (tačka 5): svaka nova poruka ide i u arhivu (samo dopisivanje; greška arhive ne zaustavlja obradu)
            if (!dryRun && !string.IsNullOrEmpty(cfg.ArhivaFolder))
                try { new Arhiva(cfg.ArhivaFolder).Sacuvaj(f.FullName, f.UidValidity, uid.Id, m, t => log.Upozorenje(t)); }
                catch (Exception ex) { log.Upozorenje("  UID " + uid.Id + ": poruka nije upisana u arhivu: " + ex.Message); }
            obradaPoruke.Poredi = (fajlovi, r, en, opis) => Poredi(indeks, sanduce, f, fajlovi, r, pravila, en, opis, uid.Id);
            return obradaPoruke.Obradi(new UlaznaPoruka { Poruka = m, Uid = uid.Id, Folder = f.FullName, Kljuc = s.Kljuc, Odlazni = odlazni, Izvor = "imap" }, ps).Korak;
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
            var upis = obradaPoruke?.Upis;
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
