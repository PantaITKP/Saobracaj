using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Saobracaj.Agenti_jobs;
using Saobracaj.Planiranje.Kontrole;

namespace Saobracaj.Planiranje.Podaci
{
    internal sealed class PodaciMatrice
    {
        internal List<RadnikMatrice> Radnici;
        internal List<RedMatrice> Redovi;
        internal List<StavkaMatrice> Stavke;
    }

    // Matrice poznavanja pruga i lokomotiva. Red postoji = čekirano, VaziDo NULL = trajno.
    internal static class KvalifikacijeRepo
    {
        private static List<RadnikMatrice> Radnici(SqlConnection c, string uslov)
        {
            return Db.Table(c, null, "SELECT DeSifra, Naziv, Masinovodja, Manevrista FROM planiranje.OP_vOsoblje WHERE Aktivan = 1 AND (" + uslov + ") ORDER BY Naziv")
                .Rows.Cast<DataRow>().Select(r => new RadnikMatrice
                {
                    DeSifra = Convert.ToInt32(r["DeSifra"]),
                    Naziv = Db.Text(r, "Naziv"),
                    Masinovodja = Convert.ToBoolean(r["Masinovodja"]),
                    Manevrista = Convert.ToBoolean(r["Manevrista"])
                }).ToList();
        }

        private static List<RedMatrice> Redovi(SqlConnection c, string sql)
        {
            return Db.Table(c, null, sql).Rows.Cast<DataRow>().Select(r => new RedMatrice { ID = Convert.ToInt32(r["ID"]), Naziv = Db.Text(r, "Naziv") }).ToList();
        }

        private static List<StavkaMatrice> Stavke(SqlConnection c, string sql)
        {
            return Db.Table(c, null, sql).Rows.Cast<DataRow>().Select(r => new StavkaMatrice
            {
                DeSifra = Convert.ToInt32(r["DeSifra"]),
                RedID = Convert.ToInt32(r["RedID"]),
                VaziDo = r.IsNull("VaziDo") ? (DateTime?)null : Convert.ToDateTime(r["VaziDo"]),
                Napomena = r.IsNull("Napomena") ? null : Convert.ToString(r["Napomena"])
            }).ToList();
        }

        internal static PodaciMatrice Pruge()
        {
            using (var c = Db.Open())
                return new PodaciMatrice
                {
                    Radnici = Radnici(c, "Masinovodja = 1"),
                    Redovi = Redovi(c,
                        "SELECT p.ID, LEFT(p.Kategorija, 3) + N' ' + CAST(p.RedniBroj AS nvarchar(10)) + N'. ' + p.Naziv AS Naziv FROM planiranje.OP_Pruga p " +
                        "JOIN planiranje.OP_RegistarVerzija v ON v.ID = p.RegistarVerzijaID " +
                        "WHERE v.Status = N'VAZECA' AND p.Aktivna = 1 ORDER BY CASE p.Kategorija WHEN N'MAGISTRALNA' THEN 1 WHEN N'REGIONALNA' THEN 2 WHEN N'LOKALNA' THEN 3 ELSE 4 END, p.RedniBroj, p.Naziv"),
                    Stavke = Stavke(c, "SELECT DeSifra, PrugaID AS RedID, VaziDo, Napomena FROM planiranje.OP_OsobljePruga")
                };
        }

        // Aktivne lokomotive su one iz dbo.Mesta sa Lokomotiva = 1, kroz njihov par u OP_Lokomotive.
        internal static PodaciMatrice Lokomotive()
        {
            using (var c = Db.Open())
                return new PodaciMatrice
                {
                    Radnici = Radnici(c, "Masinovodja = 1 OR Manevrista = 1"),
                    Redovi = Redovi(c,
                        "SELECT l.ID, l.Broj + ISNULL(N' · ' + s.Oznaka, N'') AS Naziv FROM planiranje.OP_Lokomotive l " +
                        "LEFT JOIN planiranje.OP_SerijeLokomotiva s ON s.ID = l.SerijaID " +
                        "WHERE l.Aktivna = 1 AND EXISTS (SELECT 1 FROM dbo.Mesta m WHERE m.Lokomotiva = 1 AND RTRIM(m.SmSifra) = RTRIM(l.IzvorniSmSifra) COLLATE DATABASE_DEFAULT) " +
                        "ORDER BY l.Broj"),
                    Stavke = Stavke(c, "SELECT DeSifra, LokomotivaID AS RedID, VaziDo, Napomena FROM planiranje.OP_OsobljeLokomotiva")
                };
        }

        internal static void SacuvajPruge(IzmeneMatrice iz)
        {
            Sacuvaj(iz, "planiranje.OP_OsobljePruga", "PrugaID");
        }

        internal static void SacuvajLokomotive(IzmeneMatrice iz)
        {
            Sacuvaj(iz, "planiranje.OP_OsobljeLokomotiva", "LokomotivaID");
        }

        // Tabela i kolona su fiksne vrednosti iz dve metode iznad, a podaci idu kao parametri.
        private static void Sacuvaj(IzmeneMatrice iz, string tabela, string kolona)
        {
            if (!iz.Ima) return;
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    foreach (StavkaMatrice s in iz.Brisi)
                        using (var cmd = Db.Command(c, tx, "DELETE " + tabela + " WHERE DeSifra=@d AND " + kolona + "=@r", "@d", s.DeSifra, "@r", s.RedID))
                            cmd.ExecuteNonQuery();
                    foreach (int d in iz.Upisi.Select(s => s.DeSifra).Distinct())
                        using (var cmd = Db.Command(c, tx,
                            "IF NOT EXISTS (SELECT 1 FROM planiranje.OP_OsobljePlan WHERE DeSifra=@d) INSERT planiranje.OP_OsobljePlan (DeSifra, UPlaniranju, Izmenio) VALUES (@d, 0, @k)",
                            "@d", d, "@k", PlaniranjeDb.Korisnik))
                            cmd.ExecuteNonQuery();
                    foreach (StavkaMatrice s in iz.Upisi)
                        using (var cmd = Db.Command(c, tx,
                            "IF EXISTS (SELECT 1 FROM " + tabela + " WHERE DeSifra=@d AND " + kolona + "=@r) " +
                            "UPDATE " + tabela + " SET VaziDo=@v WHERE DeSifra=@d AND " + kolona + "=@r " +
                            "ELSE INSERT " + tabela + " (DeSifra, " + kolona + ", VaziDo, Napomena) VALUES (@d, @r, @v, @n)",
                            "@d", s.DeSifra, "@r", s.RedID, "@v", (object)s.VaziDo, "@n", (object)s.Napomena))
                            cmd.ExecuteNonQuery();
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }
    }
}
