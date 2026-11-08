using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Saobracaj.Agenti_jobs;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Podaci
{
    // Najave se samo čitaju, kroz pogled nad dbo.Najava.
    internal static class NajaveRepo
    {
        // Status 4 (Nije pokrenuto - U Srbiji): voz čeka u Srbiji, pa ulazi u plan dok ne krene, i kad je primljen pre perioda.
        internal const int NijePokrenuto = 4;

        // Najava perioda, uz @od i @do, sa aliasom n nad OP_vNajava.
        internal static readonly string UslovPerioda =
            "ISNULL(n.StatusNajave, 0) <> 7 AND n.StvarnaPredaja IS NULL " +
            "AND ((n.PredvidjenoPrimanje >= @od AND n.PredvidjenoPrimanje < @do) " +
            "OR (n.StatusNajave = " + NijePokrenuto + " AND COALESCE(n.PredvidjenoPrimanje, n.StvarnoPrimanje) < @do)) ";

        // Otkud-kuda su išle najave od datuma, po merodavnim stanicama.
        internal static List<ParUpotrebe> ParoviOd(DateTime od)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT n.PlanOtpravnaID, n.PlanUputnaID, COUNT(*) AS Broj FROM planiranje.OP_vJobPravac n " +
                    "WHERE n.PredvidjenoPrimanje >= @od GROUP BY n.PlanOtpravnaID, n.PlanUputnaID", "@od", od)
                    .Rows.Cast<DataRow>().Select(r => new ParUpotrebe
                    {
                        OdID = r.IsNull("PlanOtpravnaID") ? (int?)null : Convert.ToInt32(r["PlanOtpravnaID"]),
                        DoID = r.IsNull("PlanUputnaID") ? (int?)null : Convert.ToInt32(r["PlanUputnaID"]),
                        Broj = Convert.ToInt32(r["Broj"])
                    }).ToList();
        }

        internal static DataTable BezPosla(DateTime od, DateTime doDatuma)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    // Od OP_030 izvor su pravci JOBS-a: svaki pravac je jedan posao (RBDeonice).
                    "SELECT n.* FROM planiranje.OP_vJobPravac n " +
                    "WHERE " + UslovPerioda +
                    "AND NOT EXISTS (SELECT 1 FROM planiranje.OP_Posao p WHERE p.NajavaID = n.NajavaID AND p.RBDeonice = n.Pravac AND p.Status <> N'OTKAZAN') " +
                    "ORDER BY n.PredvidjenoPrimanje",
                    "@od", od, "@do", doDatuma);
        }
    }
}
