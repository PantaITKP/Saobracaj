using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Saobracaj.Agenti_jobs;

namespace Saobracaj.Planiranje.Podaci
{
    internal sealed class IzmenaUloga
    {
        internal int DeSifra;
        internal string Naziv;
        internal bool Masinovodja, PregledacKola, Vozovodja, Manevrista;
        internal bool StaroMasinovodja, StaroPregledacKola, StaroVozovodja, StaroManevrista;
    }

    // Ime i aktivnost dolaze iz dbo.Delavci kroz pogled. Uloge se jedine upisuju u Delavci (izuzetak od pravila 3.4).
    internal static class OsobljeRepo
    {
        internal static DataTable Svi(bool samoUPlaniranju)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT o.*, s.Naziv AS MaticnaStanica, CAST(ISNULL(p.Auto, 0) AS bit) AS Auto, CAST(ISNULL(p.SamoDomicilna, 0) AS bit) AS SamoDomicilna FROM planiranje.OP_vOsoblje o " +
                    "LEFT JOIN planiranje.OP_Stanice s ON s.ID = o.MaticnaStanicaID " +
                    "LEFT JOIN planiranje.OP_OsobljePlan p ON p.DeSifra = o.DeSifra " +
                    "WHERE (@sve = 1 OR o.UPlaniranju = 1) ORDER BY o.Naziv",
                    "@sve", samoUPlaniranju ? 0 : 1);
        }

        // Dopuna za planiranje; red ne postoji dok se osoba prvi put ne sačuva.
        internal static byte[] PlanRowVer(int deSifra)
        {
            using (var c = Db.Open())
            {
                DataTable t = Db.Table(c, null, "SELECT RowVer FROM planiranje.OP_OsobljePlan WHERE DeSifra=@d", "@d", deSifra);
                return t.Rows.Count == 0 ? null : (byte[])t.Rows[0]["RowVer"];
            }
        }

        internal static void SacuvajPlan(int deSifra, byte[] rowVer, int? maticnaStanicaId, bool uPlaniranju, bool auto, bool samoDomicilna, string napomena)
        {
            using (var c = Db.Open())
                UpisiPlan(c, null, deSifra, rowVer, maticnaStanicaId, uPlaniranju, auto, samoDomicilna, napomena);
        }

        private static void UpisiPlan(SqlConnection c, SqlTransaction tx, int deSifra, byte[] rowVer, int? maticnaStanicaId, bool uPlaniranju, bool auto, bool samoDomicilna, string napomena)
        {
            if (rowVer != null)
            {
                PlaniranjeDb.IzvrsiSaRowVer(c, tx,
                    "UPDATE planiranje.OP_OsobljePlan SET MaticnaStanicaID=@m, UPlaniranju=@u, Auto=@a, SamoDomicilna=@sd, Napomena=@n, Izmenio=@k " +
                    "WHERE DeSifra=@d AND RowVer=@rv",
                    "@m", (object)maticnaStanicaId, "@u", uPlaniranju, "@a", auto, "@sd", samoDomicilna, "@n", (object)napomena, "@k", PlaniranjeDb.Korisnik, "@d", deSifra, "@rv", rowVer);
                return;
            }
            using (var cmd = Db.Command(c, tx,
                "INSERT planiranje.OP_OsobljePlan (DeSifra, MaticnaStanicaID, UPlaniranju, Auto, SamoDomicilna, Napomena, Izmenio) VALUES (@d, @m, @u, @a, @sd, @n, @k)",
                "@d", deSifra, "@m", (object)maticnaStanicaId, "@u", uPlaniranju, "@a", auto, "@sd", samoDomicilna, "@n", (object)napomena, "@k", PlaniranjeDb.Korisnik))
                cmd.ExecuteNonQuery();
        }

        // Dugme Sačuvaj: izmenjene uloge i dopuna izabrane osobe u istoj transakciji.
        internal static void SacuvajOsobe(IList<IzmenaUloga> uloge, int? deSifra, byte[] rowVer, int? maticnaStanicaId, bool uPlaniranju, bool auto, bool samoDomicilna, string napomena)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    foreach (IzmenaUloga u in uloge) UpisiUloge(c, tx, u);
                    if (deSifra.HasValue) UpisiPlan(c, tx, deSifra.Value, rowVer, maticnaStanicaId, uPlaniranju, auto, samoDomicilna, napomena);
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        // Delavci nema RowVer, pa stare vrednosti u uslovu otkrivaju izmenu drugog korisnika.
        private static void UpisiUloge(SqlConnection c, SqlTransaction tx, IzmenaUloga u)
        {
            PlaniranjeDb.IzvrsiSaRowVer(c, tx,
                "UPDATE dbo.Delavci SET Masinovodja=@m, PregledacKola=@p, Vozovodja=@v, Manevrista=@a WHERE DeSifra=@d " +
                "AND CASE WHEN ISNULL(Masinovodja,0) > 0 THEN 1 ELSE 0 END = @sm " +
                "AND CASE WHEN ISNULL(PregledacKola,0) > 0 OR ISNULL(Pregledac,0) > 0 THEN 1 ELSE 0 END = @sp " +
                "AND CASE WHEN ISNULL(Vozovodja,0) > 0 THEN 1 ELSE 0 END = @sv " +
                "AND CASE WHEN ISNULL(Manevrista,0) > 0 THEN 1 ELSE 0 END = @sa",
                "@m", u.Masinovodja ? 1 : 0, "@p", u.PregledacKola ? 1 : 0, "@v", u.Vozovodja ? 1 : 0, "@a", u.Manevrista ? 1 : 0, "@d", u.DeSifra,
                "@sm", u.StaroMasinovodja ? 1 : 0, "@sp", u.StaroPregledacKola ? 1 : 0, "@sv", u.StaroVozovodja ? 1 : 0, "@sa", u.StaroManevrista ? 1 : 0);
        }

        // Odsustva i smene imaju strani ključ na OP_OsobljePlan, a većina radnika tu još nema red.
        internal static void ObezbediPlan(int deSifra)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Command(c, null,
                "IF NOT EXISTS (SELECT 1 FROM planiranje.OP_OsobljePlan WHERE DeSifra=@d) " +
                "INSERT planiranje.OP_OsobljePlan (DeSifra, UPlaniranju, Izmenio) VALUES (@d, 0, @k)",
                "@d", deSifra, "@k", PlaniranjeDb.Korisnik))
                cmd.ExecuteNonQuery();
        }

        internal static DataTable Kvalifikacije(int deSifra)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT N'Pruga' AS Vrsta, p.Naziv, k.VaziDo FROM planiranje.OP_OsobljePruga k JOIN planiranje.OP_Pruga p ON p.ID = k.PrugaID WHERE k.DeSifra = @d " +
                    "UNION ALL SELECT N'Lokomotiva', l.Broj, k.VaziDo FROM planiranje.OP_OsobljeLokomotiva k JOIN planiranje.OP_Lokomotive l ON l.ID = k.LokomotivaID WHERE k.DeSifra = @d " +
                    "ORDER BY 1, 2",
                    "@d", deSifra);
        }
    }
}
