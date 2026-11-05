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
        // Otkud-kuda su išle najave od datuma, po merodavnim stanicama.
        internal static List<ParUpotrebe> ParoviOd(DateTime od)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT n.PlanOtpravnaID, n.PlanUputnaID, COUNT(*) AS Broj FROM planiranje.OP_vNajava n " +
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
                    "SELECT n.* FROM planiranje.OP_vNajava n " +
                    "WHERE n.PredvidjenoPrimanje >= @od AND n.PredvidjenoPrimanje < @do AND ISNULL(n.StatusNajave, 0) <> 7 AND n.StvarnaPredaja IS NULL " +
                    "AND NOT EXISTS (SELECT 1 FROM planiranje.OP_Posao p WHERE p.NajavaID = n.NajavaID AND p.Status <> N'OTKAZAN') " +
                    "ORDER BY n.PredvidjenoPrimanje",
                    "@od", od, "@do", doDatuma);
        }
    }
}
