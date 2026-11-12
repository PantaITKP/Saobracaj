using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Saobracaj.Agenti_jobs;
using Saobracaj.Planiranje.Forme;
using Saobracaj.Planiranje.Modeli;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Podaci
{
    // Brojevi za kartice ekrana Transport. NULL znači da deo nije učitan (kartica tada prikazuje „–“).
    internal sealed class StanjeKartica
    {
        internal DateTime Sada;
        internal int? JobsNovi, JobsSaTaskovima;
        internal DateTime? NajstarijiNovi;
        internal int? TaskoviDanas, TaskoviOtvoreni, TaskoviKasni;
        internal int? ZatvoriDanas;
        internal int? OsobljeRasporedjeno, OsobljeSlobodno, OsobljeOdsutno;
    }

    internal sealed class PravaKartica
    {
        internal bool Jobs, Taskovi, Mapa, Osoblje;
    }

    internal static class KarticeRepo
    {
        // Svaki deo se čita posebno: kad jednog nema (npr. skripta nije puštena), ostale kartice i dalje rade.
        internal static StanjeKartica Ucitaj()
        {
            var s = new StanjeKartica { Sada = DateTime.Now };
            using (SqlConnection c = Db.Open())
            {
                // „Danas“ je po datumu servera.
                s.Sada = Convert.ToDateTime(Db.Table(c, null, "SELECT CAST(SYSDATETIME() AS datetime) AS Sada").Rows[0]["Sada"]);
                Pokusaj(() => Jobs(c, s));
                Pokusaj(() => Taskovi(c, s));
                Pokusaj(() => Zatvori(c, s));
            }
            Pokusaj(() => Osoblje(s));
            return s;
        }

        private static void Pokusaj(Action a)
        {
            try { a(); }
            catch (SqlException) { }
            catch (InvalidOperationException) { }
        }

        // Aktivan posao je posao koji nije predat (isto kao podrazumevana lista JOBS); stornirani taskovi se ne broje.
        private static void Jobs(SqlConnection c, StanjeKartica s)
        {
            DataRow r = Db.Table(c, null,
                "SELECT ISNULL(SUM(CASE WHEN x.Ima = 0 THEN 1 ELSE 0 END), 0) AS Novi, ISNULL(SUM(x.Ima), 0) AS SaTaskovima, " +
                "MIN(CASE WHEN x.Ima = 0 THEN x.Kreirano END) AS Najstariji " +
                "FROM (SELECT j.Kreirano, CASE WHEN " + ImaTask + " THEN 1 ELSE 0 END AS Ima " +
                "FROM planiranje.OP_Jobs j WHERE j.Status <> @predato) x",
                "@predato", JobsRepo.StatusPredato).Rows[0];
            s.JobsNovi = Convert.ToInt32(r["Novi"]);
            s.JobsSaTaskovima = Convert.ToInt32(r["SaTaskovima"]);
            s.NajstarijiNovi = PlanRepo.Dat(r, "Najstariji");
        }

        // Posao j ima bar jedan task koji nije storniran.
        internal const string ImaTask =
            "EXISTS (SELECT 1 FROM planiranje.OP_TaskNajava tn JOIN planiranje.OP_Task t ON t.ID = tn.TaskID WHERE tn.NajavaID = j.ID AND t.Status <> N'STORNIRAN')";

        // Otvoren task sa rutom; forma Taskovi ne prikazuje task sa manje od dve stanice rute (TaskRepo.Ucitaj).
        internal const string OtvorenTask =
            "t.Status IN (N'OTVOREN', N'LANSIRAN') AND (SELECT COUNT(*) FROM planiranje.OP_TaskStanica ts WHERE ts.TaskID = t.ID) >= 2";

        // Isto kao KarticeTransporta.Danas i Kasni: kraj taska je ETA, a bez nje ETD.
        private static void Taskovi(SqlConnection c, StanjeKartica s)
        {
            DataRow r = Db.Table(c, null,
                "SELECT COUNT(*) AS Otvoreni, " +
                "ISNULL(SUM(CASE WHEN t.ETD IS NOT NULL AND CAST(t.ETD AS date) <= @danas AND CAST(COALESCE(t.ETA, t.ETD) AS date) >= @danas THEN 1 ELSE 0 END), 0) AS Danas, " +
                "ISNULL(SUM(CASE WHEN CAST(COALESCE(t.ETA, t.ETD) AS date) < @danas THEN 1 ELSE 0 END), 0) AS Kasni " +
                "FROM planiranje.OP_Task t WHERE " + OtvorenTask,
                "@danas", s.Sada.Date).Rows[0];
            s.TaskoviOtvoreni = Convert.ToInt32(r["Otvoreni"]);
            s.TaskoviDanas = Convert.ToInt32(r["Danas"]);
            s.TaskoviKasni = Convert.ToInt32(r["Kasni"]);
        }

        // Dnevni prozor telegrama (TrajeOd, TrajeDo) se računa isto kao na mapi i u rutiranju.
        private static void Zatvori(SqlConnection c, StanjeKartica s)
        {
            int broj = 0;
            foreach (DataRow r in Db.Table(c, null,
                "SELECT t.Vrsta, t.Kolosek, t.VaziOd, t.VaziDo, t.TrajeOd, t.TrajeDo FROM planiranje.OP_vTelegram t " +
                "WHERE t.Aktivan = 1 AND t.VaziOd < @sutra AND (t.VaziDo IS NULL OR t.VaziDo >= @sada)",
                "@sutra", s.Sada.Date.AddDays(1), "@sada", s.Sada).Rows)
            {
                var t = new TelegramOP
                {
                    Vrsta = r.IsNull("Vrsta") ? null : Convert.ToString(r["Vrsta"]),
                    Kolosek = r.IsNull("Kolosek") ? null : Convert.ToString(r["Kolosek"]),
                    VaziOd = Convert.ToDateTime(r["VaziOd"]),
                    VaziDo = PlanRepo.Dat(r, "VaziDo"),
                    TrajeOd = r.IsNull("TrajeOd") ? (TimeSpan?)null : (TimeSpan)r["TrajeOd"],
                    TrajeDo = r.IsNull("TrajeDo") ? (TimeSpan?)null : (TimeSpan)r["TrajeDo"]
                };
                if (KarticeTransporta.ZatvorDanas(t, s.Sada)) broj++;
            }
            s.ZatvoriDanas = broj;
        }

        // Isti podaci kao na formi Dostupnost osoblja, da bi brojevi i filter forme bili isti.
        private static void Osoblje(StanjeKartica s)
        {
            DateTime danas = s.Sada.Date;
            OsobljeKontekst k = ZadatakRepo.Kontekst(new PlanKontekst(), danas, danas.AddDays(1));
            int ods = 0, ras = 0, slo = 0;
            foreach (OsobaPlan o in k.Osobe.Where(KarticeTransporta.Broji))
            {
                StatusOsobe st = KarticeTransporta.Status(k, o, s.Sada);
                if (st == StatusOsobe.Odsutan) ods++; else if (st == StatusOsobe.Rasporedjen) ras++; else slo++;
            }
            s.OsobljeOdsutno = ods;
            s.OsobljeRasporedjeno = ras;
            s.OsobljeSlobodno = slo;
        }

        // Bez poruke: kartica bez prava se samo zatamni.
        internal static PravaKartica Prava()
        {
            return new PravaKartica
            {
                Jobs = PravaRepo.Ucitaj(frmOPJobs.code).Pristup,
                Taskovi = PravaRepo.Ucitaj(frmOPTaskovi.code).Pristup,
                Mapa = PravaRepo.Ucitaj(frmOPStanice.code).Pristup,
                Osoblje = PravaRepo.Ucitaj(frmOPDostupnost.code).Pristup
            };
        }
    }
}
