using System;
using System.Data;
using Saobracaj.Agenti_jobs;

namespace Saobracaj.Planiranje.Podaci
{
    // Pravilo 8.10: Dispečer i najava se samo čitaju, a promena faze se upisuje u OP_Posao sa događajem.
    internal static class TokPoslaRepo
    {
        internal static DataTable Aktivni()
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT ID, CAST(BrojNajave AS nvarchar(50)) + N' · ' + ISNULL(PlanOtpravna, N'') + N' – ' + ISNULL(PlanUputna, N'') AS Naziv FROM planiranje.OP_vPosao " +
                    "WHERE Status = N'AKTIVAN' ORDER BY PlaniraniPrijem DESC");
        }

        internal static int UskladiSve()
        {
            int promena = 0;
            DataTable poslovi;
            using (var c = Db.Open())
                poslovi = Db.Table(c, null, "SELECT ID FROM planiranje.OP_Posao WHERE Status = N'AKTIVAN'");
            foreach (DataRow r in poslovi.Rows)
                if (Uskladi(Convert.ToInt32(r["ID"]))) promena++;
            return promena;
        }

        // Faza se samo povećava; vraća true ako se posao promenio.
        internal static bool Uskladi(int posaoId)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    DataTable pt = Db.Table(c, tx,
                        "SELECT p.Faza, p.NajavaID, p.OperativniVozID, p.StvarniPolazak, p.StvarniDolazak, n.StvarnoPrimanje, n.StvarnaPredaja FROM planiranje.OP_Posao p " +
                        "LEFT JOIN planiranje.OP_vNajava n ON n.NajavaID = p.NajavaID WHERE p.ID = @id AND p.Status = N'AKTIVAN'", "@id", posaoId);
                    if (pt.Rows.Count == 0) { tx.Rollback(); return false; }
                    DataRow p = pt.Rows[0];
                    int faza = Convert.ToInt32(p["Faza"]), nova = faza;

                    DataTable voz = Db.Table(c, tx,
                        "SELECT TOP 1 ID, Started, StartedAtLocal, Completed, CompletedAtLocal FROM dbo.OperativniPlanVozovi WHERE AnnouncementFullId = @n ORDER BY ID DESC",
                        "@n", Convert.ToString(p["NajavaID"]));
                    object vozId = DBNull.Value, polazak = DBNull.Value, dolazak = DBNull.Value;
                    string izvor;
                    if (voz.Rows.Count > 0)
                    {
                        DataRow v = voz.Rows[0];
                        vozId = v["ID"];
                        izvor = "Dispečer";
                        if (!v.IsNull("Started") && Convert.ToBoolean(v["Started"])) { nova = Math.Max(nova, 5); polazak = v["StartedAtLocal"]; }
                        if (!v.IsNull("Completed") && Convert.ToBoolean(v["Completed"])) { nova = 7; dolazak = v["CompletedAtLocal"]; }
                    }
                    else
                    {
                        izvor = "najava";
                        if (!p.IsNull("StvarnoPrimanje")) { nova = Math.Max(nova, 5); polazak = p["StvarnoPrimanje"]; }
                        if (!p.IsNull("StvarnaPredaja")) { nova = 7; dolazak = p["StvarnaPredaja"]; }
                    }

                    bool noviVoz = vozId != DBNull.Value && (p.IsNull("OperativniVozID") || Convert.ToInt64(p["OperativniVozID"]) != Convert.ToInt64(vozId));
                    if (nova == faza && !noviVoz) { tx.Rollback(); return false; }
                    using (var cmd = Db.Command(c, tx,
                        "UPDATE planiranje.OP_Posao SET Faza=@f, Status=CASE WHEN @f = 7 THEN N'ZATVOREN' ELSE Status END, OperativniVozID=ISNULL(@v, OperativniVozID), " +
                        "StvarniPolazak=ISNULL(StvarniPolazak, @po), StvarniDolazak=ISNULL(StvarniDolazak, @do), Izmenio=@k, Izmenjeno=SYSDATETIME() WHERE ID=@id",
                        "@f", nova, "@v", vozId, "@po", polazak, "@do", dolazak, "@k", PlaniranjeDb.Korisnik, "@id", posaoId))
                        cmd.ExecuteNonQuery();
                    if (nova != faza)
                        DogadjajRepo.Upisi(c, tx, posaoId, "FAZA_" + nova, nova == 7 ? "Voz je stigao i posao je zatvoren (" + izvor + ")." : "Voz je krenuo (" + izvor + ").");
                    tx.Commit();
                    return true;
                }
                catch { tx.Rollback(); throw; }
            }
        }

        // Ručni koraci toka: 4 → 5 i 5 → 6.
        internal static void PromeniFazu(int posaoId, byte[] rowVer, int ocekivana, int nova, string tip, string opis)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    PlaniranjeDb.IzvrsiSaRowVer(c, tx,
                        "UPDATE planiranje.OP_Posao SET Faza=@n, Izmenio=@k, Izmenjeno=SYSDATETIME() WHERE ID=@id AND Faza=@o AND RowVer=@rv",
                        "@n", nova, "@k", PlaniranjeDb.Korisnik, "@id", posaoId, "@o", ocekivana, "@rv", rowVer);
                    DogadjajRepo.Upisi(c, tx, posaoId, tip, opis);
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        internal static bool VozPokrenut(int najavaId)
        {
            using (var c = Db.Open())
            {
                DataTable t = Db.Table(c, null, "SELECT TOP 1 Started FROM dbo.OperativniPlanVozovi WHERE AnnouncementFullId = @n ORDER BY ID DESC", "@n", Convert.ToString(najavaId));
                return t.Rows.Count > 0 && !t.Rows[0].IsNull("Started") && Convert.ToBoolean(t.Rows[0]["Started"]);
            }
        }
    }
}
