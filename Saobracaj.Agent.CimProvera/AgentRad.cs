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

        public AgentRad(AgentConfig cfg, Log log, bool dryRun) { this.cfg = cfg; this.log = log; this.dryRun = dryRun; Izvestaj.Potpis = PotpisIzvestaja.Iz(cfg); }

        public int Pokreni()
        {
            var stanje = Stanje.Ucitaj(cfg.StanjeFolder);
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
                Upozori(stanje, "imap", "Agent ne može da pročita sanduče " + cfg.Nalog + ": " + ex.Message);
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
            int n = 0;
            foreach (var uid in uids)
            {
                var m = f.GetMessage(uid);
                var kljuc = Stanje.KljucPoruke(cfg.Nalog, !string.IsNullOrEmpty(m.MessageId) ? m.MessageId : f.FullName + "/" + f.UidValidity + "/" + uid.Id);
                stanje.Poruke.TryGetValue(kljuc, out var ps);
                if (ps != null && ps.Status != "PrivremenaGreska")
                {
                    log.Info("  UID " + uid.Id + ": već obrađeno (" + ps.Status + ", " + ps.Folder + ")");
                    fs.PoslednjiUid = uid.Id; continue;
                }
                ps = ps ?? new Stanje.PorukaStanje();
                ps.Folder = f.FullName; ps.Uid = uid.Id; ps.Vreme = DateTime.Now;

                var razlog = Slanje.RazlogPreskakanja(m, cfg, !odlazni);
                if (razlog != null)
                {
                    log.Info("  UID " + uid.Id + ": " + (razlog.StartsWith("sopstveni") ? razlog + " – preskočeno" : "preskočeno – " + razlog));
                    ps.Status = "Preskoceno"; Zavrsi(stanje, fs, kljuc, ps, uid); continue;
                }
                var prilozi = Raspakivanje.PriloziMaila(m).ToList();
                trenutniPrilozi = prilozi;
                if (prilozi.Count == 0)
                {
                    ps.Status = "BezPriloga"; Zavrsi(stanje, fs, kljuc, ps, uid); continue;
                }
                ps.HesPriloga = Prilog.Sha256(prilozi.SelectMany(p => p.sadrzaj).ToArray()).Substring(0, 16);

                var od = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
                var jeOdlazni = odlazni || string.Equals(od, cfg.Nalog, StringComparison.OrdinalIgnoreCase);
                var en = Obrada.JeEngleskiIliNemacki((m.Subject ?? "") + " " + (m.TextBody ?? m.HtmlBody ?? ""));
                var datum = m.Date == DateTimeOffset.MinValue ? DateTime.Now : m.Date.LocalDateTime;
                log.Info("  UID " + uid.Id + " " + kljuc + ": " + prilozi.Count + " priloga, od " + od + (jeOdlazni ? " (odlazni)" : ""));

                Ishod ishod;
                try { ishod = obrada.Proveri(prilozi, od, cfg.JeInterni(od), datum, en); }
                catch (Exception ex)
                {
                    log.Greska("  UID " + uid.Id + ": obrada nije uspela", ex);
                    ishod = new Ishod { PrivremenaGreska = true, NisuProcitani = { ex.Message } };
                }
                foreach (var x in ishod.Preskoceno) log.Info("    preskočen prilog: " + x);
                // Režim Test: poređenje sa prvim izveštajem za iste fajlove (prilozi i fajlovi iz ZIP-a).
                string Por(RezultatProvere rr, string opis) => Poredi(indeks, sanduce, f,
                    prilozi.Select(p => p.ime).Concat(rr?.Dokumenti.Select(d => d.Fajl) ?? Enumerable.Empty<string>()), rr, pravila, en, opis, uid.Id);

                if (ishod.PrivremenaGreska)
                {
                    ps.Pokusaja++;
                    if (ps.Pokusaja < cfg.MaxPokusaja)
                    {
                        ps.Status = "PrivremenaGreska";
                        stanje.Poruke[kljuc] = ps; if (!dryRun) stanje.Sacuvaj();
                        log.Upozorenje("  UID " + uid.Id + ": privremena greška (pokušaj " + ps.Pokusaja + "/" + cfg.MaxPokusaja + "), ponovo u sledećem pokretanju.");
                        break; // UID se ne pomera: sledeće pokretanje kreće od ove poruke
                    }
                    ps.Status = "Greska";
                    Upozori(stanje, "poruka-" + kljuc, "Mail „" + m.Subject + "“ (" + kljuc + ") nije proveren posle " + ps.Pokusaja + " pokušaja: "
                        + string.Join("; ", ishod.NisuProcitani) + ". Potrebna je ručna provera.", 1000);
                    PosaljiPoredjenje(m, Por(null, "obrada nije uspela"), "obrada nije uspela");
                    Zavrsi(stanje, fs, kljuc, ps, uid); continue;
                }

                var r = ishod.Provera;
                if (!r.ImaTovarniList && ishod.NisuProcitani.Count > 0)
                {
                    // Ne sme se zaključiti "nema CIM-a" ako neki prilog nije ni pročitan.
                    log.Upozorenje("  UID " + uid.Id + ": prilozi nisu pročitani, ne može da se utvrdi da li mail sadrži CIM – ručna provera.");
                    ps.Status = "NeprocitanoRucnaProvera";
                    Upozori(stanje, "neprocitano-" + kljuc, "Mail „" + m.Subject + "“ od " + od + " (" + kljuc + "): prilozi nisu pročitani – "
                        + string.Join("; ", ishod.NisuProcitani) + ". Ne može da se utvrdi da li sadrži tovarni list; potrebna je ručna provera.", 1000);
                    PosaljiPoredjenje(m, Por(r, "prilozi nisu pročitani"), "prilozi nisu pročitani");
                    Zavrsi(stanje, fs, kljuc, ps, uid); continue;
                }
                if (!r.ImaTovarniList)
                {
                    var spiskovi = r.Dokumenti.Where(d => d.Tip == TipDokumenta.SpisakKola).ToList();
                    if (spiskovi.Count > 0)
                    {
                        // Dostavljen je spisak kola (CIT 23) bez tovarnog lista: obaveštenje kao i za greške.
                        log.Info("  UID " + uid.Id + ": samo spisak kola, bez CIM/CUV (" + string.Join(", ", spiskovi.Select(d => d.Fajl).Distinct()) + ") – šalje se obaveštenje.");
                        ps.Status = "SamoSpisak";
                        try { PosaljiSamoSpisak(m, spiskovi, pravila, en, jeOdlazni); }
                        catch (Exception ex)
                        {
                            log.Greska("  UID " + uid.Id + ": obaveštenje nije poslato", ex);
                            ps.Pokusaja++;
                            if (ps.Pokusaja < cfg.MaxPokusaja) { ps.Status = "PrivremenaGreska"; stanje.Poruke[kljuc] = ps; if (!dryRun) stanje.Sacuvaj(); break; }
                        }
                        PosaljiPoredjenje(m, Por(r, "samo spisak kola, bez tovarnog lista"), "samo spisak kola, bez tovarnog lista");
                        Zavrsi(stanje, fs, kljuc, ps, uid); continue;
                    }
                    log.Info("  UID " + uid.Id + ": nema CIM/CUV – mail se preskače. Dokumenti: " + string.Join(", ", r.Dokumenti.Select(d => d.Tip).Distinct()));
                    PosaljiPoredjenje(m, Por(r, "tovarni list nije pronađen"), "tovarni list nije pronađen");
                    ps.Status = "NemaCim"; Zavrsi(stanje, fs, kljuc, ps, uid); continue;
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
                    ps.Pokusaja++;
                    if (ps.Pokusaja < cfg.MaxPokusaja) { ps.Status = "PrivremenaGreska"; stanje.Poruke[kljuc] = ps; if (!dryRun) stanje.Sacuvaj(); break; }
                    Upozori(stanje, "smtp", "Izveštaj za „" + m.Subject + "“ nije poslat: " + ex.Message);
                }
                if (dodatak != null && !poslatDodatak) PosaljiPoredjenje(m, dodatak, "izveštaj nije poslat (" + Izvestaj.OpisListova(r, false) + ", grešaka " + r.BrojGresaka + ", upozorenja " + r.BrojUpozorenja + ")");
                dodatak = null;
                Zavrsi(stanje, fs, kljuc, ps, uid);
                n++;
            }
            return n;
        }

        private void Zavrsi(Stanje stanje, Stanje.FolderStanje fs, string kljuc, Stanje.PorukaStanje ps, UniqueId uid)
        {
            stanje.Poruke[kljuc] = ps;
            fs.PoslednjiUid = Math.Max(fs.PoslednjiUid, uid.Id);
            if (!dryRun) stanje.Sacuvaj();
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

        /// <summary>Obaveštenje: u mailu je spisak kola (CIT 23), a tovarni list CIM/CUV nije pronađen.</summary>
        private void PosaljiSamoSpisak(MimeMessage m, List<Dokument> spiskovi, PravilaSkup pravila, bool en, bool odlazni)
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
            var telo = uvod + Environment.NewLine + Environment.NewLine + poruka + Environment.NewLine + (en ? "Wagon list: " : "Spisak kola: ") + fajlovi
                       + Environment.NewLine + Environment.NewLine + "--" + Environment.NewLine + pravila.Sablon("footer", en, "") + " " + (en ? "Rules version " : "Verzija pravila ") + pravila.Verzija + ".";
            var html = "<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:13px\"><p>" + System.Net.WebUtility.HtmlEncode(uvod).Replace("\n", "<br>") + "</p>"
                       + "<p style=\"color:#b00020\"><b>" + System.Net.WebUtility.HtmlEncode(poruka) + "</b></p><p>" + (en ? "Wagon list: " : "Spisak kola: ") + System.Net.WebUtility.HtmlEncode(fajlovi) + "</p></div>";
            var mail = Slanje.Nova(cfg, primaoci, kopija, naslov, telo, odgovorNa: m, html: html);
            if (dryRun) { log.Info("    [dry-run] obaveštenje za " + string.Join(", ", primaoci) + ": " + naslov); return; }
            Slanje.Posalji(cfg, mail);
            log.Info("    obaveštenje poslato na " + string.Join(", ", primaoci.Concat(kopija)) + ": " + naslov);
        }

        private void Upozori(Stanje stanje, string kljuc, string tekst, int sati = 6)
        {
            if (dryRun || string.IsNullOrEmpty(cfg.UpozorenjaNa) || !stanje.SmeUpozorenje(kljuc, sati)) return;
            try
            {
                Slanje.Posalji(cfg, Slanje.Nova(cfg, new[] { cfg.UpozorenjaNa }, null, "[CimProvera] Upozorenje: " + kljuc, tekst
                    + Environment.NewLine + Environment.NewLine + "Server: " + Environment.MachineName + ", " + DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm zzz")));
            }
            catch (Exception ex) { log.Greska("Upozorenje nije poslato mailom: " + ex.Message); }
        }
    }
}
