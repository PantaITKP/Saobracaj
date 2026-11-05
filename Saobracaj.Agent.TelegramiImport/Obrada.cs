using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Saobracaj.Agent.TelegramiImport.Data;
using Saobracaj.Agent.TelegramiImport.Mail;
using Saobracaj.Agent.TelegramiImport.Parsing;
using Saobracaj.Agent.TelegramiImport.Pdf;

namespace Saobracaj.Agent.TelegramiImport
{
    internal sealed class RezultatObrade
    {
        public string Status;
        public int Upisano;
        public int Ugaseno;
        public bool Preskoceno;
    }

    /// <summary>Obrada jedne poruke u jednoj transakciji: ili su upisani svi redovi poruke ili nijedan.</summary>
    internal sealed class Obrada
    {
        private readonly AgentConfig cfg;
        private readonly Log log;
        private readonly Sifarnik sifarnik;

        public Obrada(AgentConfig cfg, Log log, Sifarnik sifarnik)
        {
            this.cfg = cfg;
            this.log = log;
            this.sifarnik = sifarnik;
        }

        public RezultatObrade Obradi(SqlConnection c, Poruka poruka)
        {
            var rez = new RezultatObrade();
            using (var tx = c.BeginTransaction())
            {
                var store = new TelegramStore(c, tx);
                if (store.VecObradjena(poruka))
                {
                    tx.Rollback();
                    rez.Preskoceno = true;
                    return rez;
                }

                var mailLogId = store.UpisiMailLog(poruka, cfg.DryRun);
                var poruke = new List<string>();
                ParsiranTelegram glavni = null;
                string status;

                var pdfovi = poruka.Prilozi.Where(a => a.JePdf).ToList();
                if (pdfovi.Count == 0)
                {
                    // Excel najave (HŽ Cargo) i slično: samo arhiva. Ostalo bez PDF-a: na pregled.
                    var samoExcel = poruka.Prilozi.Count > 0 && poruka.Prilozi.All(a => (a.Naziv ?? "").EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || (a.Naziv ?? "").EndsWith(".xls", StringComparison.OrdinalIgnoreCase));
                    status = samoExcel ? "Arhivirano" : "NeedsReview";
                    poruke.Add(samoExcel ? "Poruka sa Excel prilogom (najava); arhivirana." : "Poruka bez PDF priloga.");
                }
                else
                {
                    var statusi = new List<string>();
                    var noviIds = new List<int>();
                    foreach (var pdf in pdfovi)
                    {
                        string tekst;
                        try { tekst = PdfTekst.Izvuci(pdf.Sadrzaj); }
                        catch (Exception ex) { tekst = ""; poruke.Add(pdf.Naziv + ": PDF ne može da se pročita (" + ex.Message + ")."); }

                        var t = TelegramParser.Parsiraj(tekst, poruka.Naslov, poruka.Primljeno);
                        if (glavni == null || (glavni.Broj == null && t.Broj != null)) glavni = t;
                        poruke.AddRange(t.Upozorenja.Select(u => (t.Oznaka ?? pdf.Naziv) + ": " + u));
                        statusi.Add(ObradiTelegram(store, mailLogId, t, poruke, rez, noviIds));
                    }

                    if (noviIds.Count > 0 && !cfg.DryRun)
                    {
                        var putanja = TelegramStore.SnimiPdf(cfg.PdfFolder, noviIds.Min(), pdfovi, poruke);
                        if (putanja != null) store.PostaviPdf(noviIds, putanja);
                    }

                    status = new[] { "Greska", "NeedsReview", "Otkazano", "Upisano", "Arhivirano" }.FirstOrDefault(statusi.Contains) ?? "Arhivirano";
                }

                if (cfg.DryRun) poruke.Insert(0, "DryRun: ništa nije upisano.");
                store.ZavrsiMailLog(mailLogId, cfg.DryRun ? "DryRun" : status, glavni, rez.Upisano + rez.Ugaseno, poruke);
                rez.Status = status;

                log.Info(string.Format("Poruka UID {0} \"{1}\" → {2} {3} [{4}], upisano {5}, ugašeno {6}",
                    poruka.Uid, poruka.Naslov, glavni?.Oznaka, glavni?.Datum?.ToString("dd.MM.yyyy"), glavni?.VrsteTekst, rez.Upisano, rez.Ugaseno) +
                    " → " + status + (cfg.DryRun ? " (DryRun)" : ""));
                foreach (var p in poruke) log.Info("   " + p);

                if (cfg.DryRun) tx.Rollback(); else tx.Commit();
            }
            return rez;
        }

        private string ObradiTelegram(TelegramStore store, int mailLogId, ParsiranTelegram t, List<string> poruke, RezultatObrade rez, List<int> noviIds)
        {
            if (t.BezTeksta) return "NeedsReview";

            // Otkazivanja / van snage
            // Otkazivanje se tiče redova u Telegrami samo ako je reč o zatvoru / skretnici / NP;
            // "van snage" telegrama o posedanju ili laganoj vožnji nema šta da ugasi u Telegrami.
            var telo = Saobracaj.Common.Text.TekstUtil.BezDijakritika(t.Telo ?? "").ToLowerInvariant();
            var zatvorsko = System.Text.RegularExpressions.Regex.IsMatch(telo, @"zatvor|skretnic|zabran\w* saobracaj|\bnp\b|narocit\w* posiljk");

            bool otkazano = false, nepoznatCilj = false;
            foreach (var cilj in t.Ciljevi)
            {
                var r = store.PrimeniOtkazivanje(t, cilj, zatvorsko);
                poruke.Add(r.Opis);
                rez.Ugaseno += r.Ugaseno;
                if (r.Ugaseno > 0) { otkazano = true; log.Info("   " + r.Opis); }
                if (!r.CiljPostoji) nepoznatCilj = true;
            }
            if ((t.Vrste.Contains(Vrsta.VanSnage) || t.Vrste.Contains(Vrsta.Otkazivanje)) && t.Ciljevi.Count == 0 && !t.ZaUpis)
                nepoznatCilj = true;

            if (!t.ZaUpis)
            {
                if (nepoznatCilj && zatvorsko) return "NeedsReview";
                if (nepoznatCilj) poruke.Add("Otkazivanje/van snage se ne odnosi na zatvor; ništa nije menjano u Telegrami.");
                return otkazano ? "Otkazano" : "Arhivirano";
            }
            if (t.Broj == null || t.Datum == null)
            {
                poruke.Add("Broj ili datum telegrama nisu pronađeni.");
                return "NeedsReview";
            }

            // Razrešavanje pruge i stanica; ako bilo šta nedostaje, cela poruka čeka pregled
            var razresene = new List<RazresenaStavka>();
            foreach (var s in t.Stavke)
            {
                var r = new RazresenaStavka();
                r.PrugaID = sifarnik.PrugaID(s.PrugaTekst, new[] { s.OdTekst, s.DoTekst }, r.Upozorenja);
                r.OdStanice = sifarnik.StanicaID(s.OdTekst, r.PrugaID, r.Upozorenja);
                r.DoStanice = sifarnik.StanicaID(s.DoTekst, r.PrugaID, r.Upozorenja);
                if (r.PrugaID == null) r.Upozorenja.Add("Nepoznata pruga: \"" + s.PrugaTekst + "\".");
                if (r.OdStanice == null) r.Upozorenja.Add("Nepoznata stanica: \"" + s.OdTekst + "\".");
                if (r.DoStanice == null) r.Upozorenja.Add("Nepoznata stanica: \"" + s.DoTekst + "\".");
                razresene.Add(r);
            }
            if (t.Stavke.Count == 0)
            {
                poruke.Add("Telegram je vrste " + t.GlavnaVrsta + ", ali nije pronađen nijedan red.");
                return "NeedsReview";
            }

            var potpuna = razresene.All(r => r.Potpuna);
            for (int i = 0; i < t.Stavke.Count; i++)
            {
                var s = t.Stavke[i];
                var r = razresene[i];
                poruke.AddRange(r.Upozorenja.Select(u => (s.Tacka != null ? "t." + s.Tacka + ": " : "") + u));
                if (!potpuna)
                {
                    store.UpisiStavku(mailLogId, t, s, r, "NeedsReview", null);
                    continue;
                }
                var dup = store.Duplikat(t, r, s);
                if (dup != null)
                {
                    store.UpisiStavku(mailLogId, t, s, r, "Duplikat", dup);
                    poruke.Add("Red već postoji u Telegrami (ID " + dup + "): " + s);
                    continue;
                }
                var id = store.UpisiTelegram(t, s, r, mailLogId, null);
                noviIds.Add(id);
                store.UpisiStavku(mailLogId, t, s, r, "Upisano", id);
                rez.Upisano++;
                log.Info("   + Telegrami ID " + id + ": " + t.Oznaka + " " + sifarnik.NazivStanice(r.OdStanice.Value) + " - " + sifarnik.NazivStanice(r.DoStanice.Value) +
                         " " + s.VaziOd.ToString("dd.MM.yyyy HH:mm") + " → " + s.VaziDo.ToString("dd.MM.yyyy HH:mm") + " [" + s.Kolosek + "]");
            }
            if (!potpuna || nepoznatCilj) return "NeedsReview";
            return otkazano ? "Otkazano" : "Upisano";
        }
    }
}
