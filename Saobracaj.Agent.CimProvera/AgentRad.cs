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
            var obrada = new Obrada(pravila, citac, log, cfg.PragPouzdanosti);
            int obradjeno = 0;
            try
            {
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
                red.Add(new RedPoruka.Stavka
                {
                    Uid = u.Id, Folder = f.FullName, MessageId = mid,
                    Kljuc = Stanje.KljucPoruke(cfg.Nalog, mid.Length > 0 ? mid : f.FullName + "/" + f.UidValidity + "/" + u.Id),
                    Od = sz?.Envelope?.From?.Mailboxes.FirstOrDefault()?.Address ?? "",
                    Naslov = Regex(TekstUtil.Ocisti(sz?.Envelope?.Subject)),
                    Prilozi = imena,
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

                var od = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
                var jeOdlazni = odlazni || string.Equals(od, cfg.Nalog, StringComparison.OrdinalIgnoreCase);
                // naslov i telo mogu imati neispravan Unicode (usamljeni surogat) – čiste se pre bilo kakve normalizacije
                var tekstMaila = (m.Subject ?? "") + " " + (m.TextBody ?? m.HtmlBody ?? "");
                var neispravnih = TekstUtil.BrojNeispravnih(tekstMaila);
                if (neispravnih > 0) log.Upozorenje("  UID " + uid.Id + ": naslov/telo maila ima " + neispravnih + " neispravnih Unicode znakova (usamljeni surogat) – zamenjeni znakom U+FFFD.");
                var vanBmp = ZnakoviVanBmp(tekstMaila);
                if (vanBmp.Length > 0) log.Info("  UID " + uid.Id + ": naslov/telo sadrži znakove van osnovne ravni (emoji i sl.): " + vanBmp);
                var en = Obrada.JeEngleskiIliNemacki(TekstUtil.Ocisti(tekstMaila));
                var datum = m.Date == DateTimeOffset.MinValue ? DateTime.Now : m.Date.LocalDateTime;
                log.Info("  UID " + uid.Id + " " + kljuc + ": " + prilozi.Count + " priloga, od " + od + (jeOdlazni ? " (odlazni)" : ""));

                Ishod ishod;
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
                if (!r.ImaTovarniList && (ishod.NisuProcitani.Count > 0 || ishod.Neprepoznati.Count > 0))
                {
                    // Ne sme se zaključiti "nema CIM-a" ako neki prilog nije ni pročitan ni prepoznat: samo interna poruka, ne pošiljaocu.
                    log.Upozorenje("  UID " + uid.Id + ": prilozi nisu pročitani/prepoznati, ne može da se utvrdi da li mail sadrži CIM – ručna provera.");
                    ps.Status = "NeprocitanoRucnaProvera";
                    try { PosaljiInternoRucnaProvera(m, ishod, en); }
                    catch (Exception ex) { log.Greska("  UID " + uid.Id + ": interna poruka nije poslata: " + ex.Message); }
                    Upozori(stanje, "neprocitano-" + kljuc, "Mail „" + m.Subject + "“ od " + od + " (" + kljuc + "): prilozi nisu pročitani – "
                        + string.Join("; ", ishod.NisuProcitani) + ". Ne može da se utvrdi da li sadrži tovarni list; potrebna je ručna provera.", 1000);
                    PosaljiPoredjenje(m, Por(r, "prilozi nisu pročitani"), "prilozi nisu pročitani");
                    return Korak.Gotova;
                }
                if (!r.ImaTovarniList)
                {
                    var spiskovi = r.Dokumenti.Where(d => d.Tip == TipDokumenta.SpisakKola).ToList();
                    if (spiskovi.Count > 0)
                    {
                        // Dostavljen je spisak kola (CIT 23) bez tovarnog lista: obaveštenje kao i za greške.
                        log.Info("  UID " + uid.Id + ": samo spisak kola, bez CIM/CUV (" + string.Join(", ", spiskovi.Select(d => d.Fajl).Distinct()) + ") – šalje se obaveštenje.");
                        ps.Status = "SamoSpisak";
                        try { PosaljiSamoSpisak(m, spiskovi, pravila, en, jeOdlazni, ishod.OpisPriloga); }
                        catch (Exception ex)
                        {
                            log.Greska("  UID " + uid.Id + ": obaveštenje nije poslato", ex);
                            ps.PoslednjaGreska = "obaveštenje nije poslato: " + ex.Message;
                            return Korak.PrivremenaGreska;
                        }
                        PosaljiPoredjenje(m, Por(r, "samo spisak kola, bez tovarnog lista"), "samo spisak kola, bez tovarnog lista");
                        return Korak.Gotova;
                    }
                    log.Info("  UID " + uid.Id + ": nema CIM/CUV – mail se preskače. Dokumenti: " + string.Join(", ", r.Dokumenti.Select(d => d.Tip).Distinct()));
                    PosaljiPoredjenje(m, Por(r, "tovarni list nije pronađen"), "tovarni list nije pronađen");
                    ps.Status = "NemaCim"; return Korak.Gotova;
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
                return Korak.Obradjena;
            }
        }

        private void PosaljiIzvestaj(MimeMessage m, RezultatProvere r, PravilaSkup pravila, bool en, bool odlazni, List<string> nisuProcitani)
        {
            // Nalazi "samo interno" (npr. r49 koju popunjava KP kao prevoznik) ne idu spoljnom pošiljaocu: on dobija izveštaj
            // bez njih, a puni izveštaj ide kao interna kopija (KopijaNa). U režimu Test sve ide na IzvestajAdresa.
            var od0 = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
            if (!odlazni && cfg.Rezim == Rezim.Live && !cfg.JeInterni(od0) && r.ImaInternih)
            {
                PosaljiIzvestaj(m, r.BezInternih(), pravila, en, false, nisuProcitani, bezKopije: true);
                if (cfg.KopijaNa.Count > 0) PosaljiInterno(m, r, pravila, en, nisuProcitani);
                return;
            }
            PosaljiIzvestaj(m, r, pravila, en, odlazni, nisuProcitani, false);
        }

        /// <summary>Puni izveštaj (sa nalazima "samo interno") za KopijaNa, bez odgovora pošiljaocu.</summary>
        private void PosaljiInterno(MimeMessage m, RezultatProvere r, PravilaSkup pravila, bool en, List<string> nisuProcitani)
        {
            var naslov = "[INTERNO] " + Izvestaj.Naslov(r, pravila, en);
            var uvod = "Interna kopija za KP: izveštaj za mail „" + m.Subject + "“ od " + (m.From.Mailboxes.FirstOrDefault()?.Address ?? "") + ". Nalazi označeni „(interno)“ nisu poslati pošiljaocu.";
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

        private void PosaljiIzvestaj(MimeMessage m, RezultatProvere r, PravilaSkup pravila, bool en, bool odlazni, List<string> nisuProcitani, bool bezKopije)
        {
            var imaNalaza = r.BrojGresaka + r.BrojUpozorenja > 0;
            if (odlazni && !imaNalaza && !cfg.ObavestiIspravanOdlazni) { log.Info("    odlazni CIM je ispravan – obaveštenje se ne šalje."); return; }
            if (!odlazni && !imaNalaza && !cfg.OdgovoriNaIspravanDolazni) return;

            var od = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
            var za = string.Join(", ", m.To.Mailboxes.Select(x => x.Address));
            var datum = m.Date.LocalDateTime.ToString("dd.MM.yyyy HH:mm");
            var naslov = Izvestaj.Naslov(r, pravila, en);
            string uvod;
            List<string> primaoci, kopija = new List<string>();
            if (odlazni)
            {
                uvod = (en ? "The mail „" : "Mail „") + m.Subject + (en ? "“ that you sent on " : "“ koji ste poslali ") + datum + (en ? " to " : " na ") + za
                       + (r.BrojGresaka > 0 ? (en ? " contains a consignment note with errors:" : " sadrži tovarni list sa greškama:") : ":");
                primaoci = new List<string> { cfg.Nalog };
            }
            else
            {
                uvod = null; // pismo pošiljaocu počinje zahvalnicom (Izvestaj.Telo)
                primaoci = new List<string> { od };
                kopija = bezKopije ? new List<string>() : cfg.KopijaNa;
            }
            if (cfg.Rezim == Rezim.Test)
            {
                uvod = "[TEST] " + (odlazni ? "Odlazni" : "Dolazni") + " mail od " + od + " za " + za + ", " + datum + ". "
                       + "U režimu Live izveštaj bi išao na: " + string.Join(", ", primaoci.Concat(kopija)) + ". Naslov primljenog maila: „" + m.Subject + "“."
                       + (uvod != null ? Environment.NewLine + Environment.NewLine + uvod : "");
                primaoci = new List<string> { cfg.IzvestajAdresa };
                kopija = new List<string>();
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
            var neprepoznati = ishod.Neprepoznati.Concat(ishod.NisuProcitani.Select(x => x.Split(':')[0])).Select(x => System.IO.Path.GetFileName(x.Split('/').Last())).Distinct().ToList();
            var primaoci = cfg.Rezim == Rezim.Test ? new List<string> { cfg.IzvestajAdresa }
                : cfg.KopijaNa.Concat(new[] { cfg.UpozorenjaNa }).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var naslov = "[INTERNO] Ručna provera – " + System.Text.RegularExpressions.Regex.Replace(m.Subject ?? "", @"\s+", " ").Trim();
            var sb = new System.Text.StringBuilder();
            if (cfg.Rezim == Rezim.Test) sb.AppendLine("[TEST] Interna poruka – ne ide pošiljaocu (" + od + ").").AppendLine();
            sb.AppendLine("Mail „" + m.Subject + "“ od " + od + ", " + m.Date.LocalDateTime.ToString("dd.MM.yyyy HH:mm") + ".").AppendLine();
            sb.AppendLine("Tovarni list nije automatski prepoznat. " + (neprepoznati.Count == 1 ? "Prilog " + neprepoznati[0] + " nije mogao da se pročita" : "Prilozi " + string.Join(", ", neprepoznati) + " nisu mogli da se pročitaju") + " – proverite ručno.");
            if (ishod.NisuProcitani.Count > 0) sb.AppendLine("Razlog: " + string.Join("; ", ishod.NisuProcitani) + ".");
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
        private void Upozori(Stanje stanje, string kljuc, string tekst, int sati = 6)
        {
            if (dryRun) return;
            if (string.IsNullOrEmpty(cfg.UpozorenjaNa)) { log.Upozorenje("Upozorenje „" + kljuc + "“ nema primaoca (UpozorenjaNa/IzvestajAdresa)."); return; }
            if (!stanje.SmeUpozorenje(kljuc, sati))
            {
                log.Info("Upozorenje „" + kljuc + "“ se ne šalje ponovo – poslednji pokušaj " + stanje.Upozorenja[kljuc].ToString("dd.MM. HH:mm") + " (najviše jednom na " + sati + " h).");
                return;
            }
            try
            {
                Slanje.Posalji(cfg, Slanje.Nova(cfg, new[] { cfg.UpozorenjaNa }, null, "[CimProvera] Upozorenje: " + kljuc, tekst
                    + Environment.NewLine + Environment.NewLine + "Server: " + Environment.MachineName + ", " + DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm zzz")));
                stanje.ZapisiUpozorenje(kljuc, true, sati);
                log.Info("Upozorenje „" + kljuc + "“ poslato na " + cfg.UpozorenjaNa + ".");
            }
            catch (Exception ex)
            {
                stanje.ZapisiUpozorenje(kljuc, false, sati);
                log.Greska("Upozorenje „" + kljuc + "“ nije poslato mailom (ponovni pokušaj za 10 min): " + ex.Message);
            }
        }
    }
}
