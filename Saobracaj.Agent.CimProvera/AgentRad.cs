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

        public AgentRad(AgentConfig cfg, Log log, bool dryRun) { this.cfg = cfg; this.log = log; this.dryRun = dryRun; }

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
                using (var s = new Sanduce(cfg))
                {
                    s.Povezi();
                    var folderi = new List<(IMailFolder f, bool odlazni)> { (s.Dolazni(), false) };
                    if (cfg.CitajOdlazne) folderi.Add((s.Odlazni(), true));
                    foreach (var (f, odlazni) in folderi)
                        obradjeno += ObradiFolder(f, odlazni, obrada, pravila, stanje);
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

        private int ObradiFolder(IMailFolder f, bool odlazni, Obrada obrada, PravilaSkup pravila, Stanje stanje)
        {
            f.Open(FolderAccess.ReadOnly);
            var fs = stanje.Folder(f.FullName);
            if (fs.UidValidity != f.UidValidity)
            {
                if (fs.UidValidity != 0) log.Upozorenje("UIDVALIDITY foldera " + f.FullName + " se promenio – čita se od ImportOd, duplikati se prepoznaju po Message-ID.");
                fs.UidValidity = f.UidValidity;
                fs.PoslednjiUid = 0;
            }
            var uids = Sanduce.Nove(f, fs.PoslednjiUid, cfg.ImportOd);
            log.Info(f.FullName + " (" + (odlazni ? "odlazni" : "dolazni") + "): novih poruka " + uids.Count);
            int n = 0;
            foreach (var uid in uids)
            {
                var m = f.GetMessage(uid);
                var kljuc = !string.IsNullOrEmpty(m.MessageId) ? m.MessageId : f.FullName + "/" + f.UidValidity + "/" + uid.Id;
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
                    log.Info("  UID " + uid.Id + ": preskočeno – " + razlog);
                    ps.Status = "Preskoceno"; Zavrsi(stanje, fs, kljuc, ps, uid); continue;
                }
                var prilozi = Raspakivanje.PriloziMaila(m).ToList();
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
                        Zavrsi(stanje, fs, kljuc, ps, uid); continue;
                    }
                    log.Info("  UID " + uid.Id + ": nema CIM/CUV – mail se preskače. Dokumenti: " + string.Join(", ", r.Dokumenti.Select(d => d.Tip).Distinct()));
                    ps.Status = "NemaCim"; Zavrsi(stanje, fs, kljuc, ps, uid); continue;
                }

                ps.Greske = r.BrojGresaka; ps.Upozorenja = r.BrojUpozorenja;
                ps.Status = r.BrojGresaka > 0 ? "Greske" : "Ispravan";
                log.Info("  UID " + uid.Id + ": " + Izvestaj.OpisListova(r, false) + ", grešaka " + r.BrojGresaka + ", upozorenja " + r.BrojUpozorenja
                    + (r.ZaRucnuProveru ? ", RUČNA PROVERA" : "") + ". Nalazi: " + string.Join(" ", r.Listovi.SelectMany(l => l.Nalazi).Select(x => x.Pravilo).Distinct()));
                if (r.ZaRucnuProveru) ps.Status += "+RucnaProvera";

                try { PosaljiIzvestaj(m, r, pravila, en, jeOdlazni, ishod.NisuProcitani); }
                catch (Exception ex)
                {
                    log.Greska("  UID " + uid.Id + ": izveštaj nije poslat", ex);
                    ps.Pokusaja++;
                    if (ps.Pokusaja < cfg.MaxPokusaja) { ps.Status = "PrivremenaGreska"; stanje.Poruke[kljuc] = ps; if (!dryRun) stanje.Sacuvaj(); break; }
                    Upozori(stanje, "smtp", "Izveštaj za „" + m.Subject + "“ nije poslat: " + ex.Message);
                }
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
            var naslov = "[INTERNO] " + Izvestaj.NaslovMaila(m.Subject, Izvestaj.FajloviListova(r), en, r);
            var uvod = "Interna kopija za KP: izveštaj za mail „" + m.Subject + "“ od " + (m.From.Mailboxes.FirstOrDefault()?.Address ?? "") + ". Nalazi označeni „(interno)“ nisu poslati pošiljaocu.";
            var poruka = Slanje.Nova(cfg, cfg.KopijaNa, null, naslov, Izvestaj.Telo(r, pravila, en, uvod, nisuProcitani), odgovorNa: m, html: Izvestaj.Html(r, pravila, en, uvod, nisuProcitani));
            if (dryRun) { log.Info("    [dry-run] interna kopija za " + string.Join(", ", cfg.KopijaNa) + ": " + naslov); return; }
            Slanje.Posalji(cfg, poruka);
            log.Info("    interna kopija poslata na " + string.Join(", ", cfg.KopijaNa) + ": " + naslov);
        }

        private void PosaljiIzvestaj(MimeMessage m, RezultatProvere r, PravilaSkup pravila, bool en, bool odlazni, List<string> nisuProcitani, bool bezKopije)
        {
            var imaNalaza = r.BrojGresaka + r.BrojUpozorenja > 0;
            if (odlazni && !imaNalaza && !cfg.ObavestiIspravanOdlazni) { log.Info("    odlazni CIM je ispravan – obaveštenje se ne šalje."); return; }
            if (!odlazni && !imaNalaza && !cfg.OdgovoriNaIspravanDolazni) return;

            var od = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
            var za = string.Join(", ", m.To.Mailboxes.Select(x => x.Address));
            var datum = m.Date.LocalDateTime.ToString("dd.MM.yyyy HH:mm");
            var naslov = Izvestaj.NaslovMaila(m.Subject, Izvestaj.FajloviListova(r), en, r);
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
                uvod = (en ? "Check of the consignment note from your mail „" : "Provera tovarnog lista iz vašeg maila „") + m.Subject + "“ (" + datum + "):";
                primaoci = new List<string> { od };
                kopija = bezKopije ? new List<string>() : cfg.KopijaNa;
            }
            if (cfg.Rezim == Rezim.Test)
            {
                uvod = "[TEST] " + (odlazni ? "Odlazni" : "Dolazni") + " mail od " + od + " za " + za + ", " + datum + ". "
                       + "U režimu Live izveštaj bi išao na: " + string.Join(", ", primaoci.Concat(kopija)) + "." + Environment.NewLine + Environment.NewLine + uvod;
                primaoci = new List<string> { cfg.IzvestajAdresa };
                kopija = new List<string>();
            }
            var telo = Izvestaj.Telo(r, pravila, en, uvod, nisuProcitani);
            var html = Izvestaj.Html(r, pravila, en, uvod, nisuProcitani);
            // Opcija: ispravan dolazni CIM → samo kratka potvrda (podrazumevano isključeno).
            if (!odlazni && !imaNalaza && cfg.PotvrdaIspravnogDolaznog)
            {
                var ident = Izvestaj.Identifikacija(r, en);
                telo = (ident.Length > 0 ? ident + Environment.NewLine : "") + uvod + Environment.NewLine + Environment.NewLine + (en ? "The consignment note is correct." : "Tovarni list je ispravan.");
                html = "<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:13px\">" + System.Net.WebUtility.HtmlEncode(telo).Replace("\n", "<br>") + "</div>";
            }
            var poruka = Slanje.Nova(cfg, primaoci, kopija, naslov, telo, odgovorNa: m, html: html);
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
