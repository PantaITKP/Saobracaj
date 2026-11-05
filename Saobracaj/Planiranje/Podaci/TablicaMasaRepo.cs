using System;
using System.Collections.Generic;
using System.Data;
using Saobracaj.Agenti_jobs;

namespace Saobracaj.Planiranje.Podaci
{
    internal sealed class RedTabliceMasa
    {
        internal int SerijaID;
        internal int? PrugaID;
        internal int? StanicaOdID;
        internal int? StanicaDoID;
        internal string Smer;
        internal int BrojLokomotiva;
        internal string Sloj;
        internal decimal MaxMasaT;
        internal DateTime? VaziOd;
        internal DateTime? VaziDo;
        internal string Napomena;
    }

    internal static class TablicaMasaRepo
    {
        internal static DataTable Sve()
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT t.ID, t.SerijaID, s.Oznaka AS Serija, t.PrugaID, p.Naziv AS Pruga, t.StanicaOdID, o.Naziv AS StanicaOd, t.StanicaDoID, d.Naziv AS StanicaDo, " +
                    "t.Smer, t.BrojLokomotiva, t.Sloj, t.MaxMasaT, t.VaziOd, t.VaziDo, t.Napomena, t.RowVer " +
                    "FROM planiranje.OP_TablicaMasa t " +
                    "JOIN planiranje.OP_SerijeLokomotiva s ON s.ID = t.SerijaID " +
                    "LEFT JOIN planiranje.OP_Pruga p ON p.ID = t.PrugaID " +
                    "LEFT JOIN planiranje.OP_Stanice o ON o.ID = t.StanicaOdID " +
                    "LEFT JOIN planiranje.OP_Stanice d ON d.ID = t.StanicaDoID " +
                    "ORDER BY s.Oznaka, t.Sloj, t.BrojLokomotiva");
        }

        private static object[] Parametri(RedTabliceMasa r)
        {
            return new object[]
            {
                "@s", r.SerijaID, "@p", (object)r.PrugaID, "@o", (object)r.StanicaOdID, "@d", (object)r.StanicaDoID, "@sm", (object)r.Smer,
                "@b", r.BrojLokomotiva, "@sl", r.Sloj, "@m", r.MaxMasaT, "@vo", (object)r.VaziOd, "@vd", (object)r.VaziDo, "@n", (object)r.Napomena
            };
        }

        private const string Insert =
            "INSERT planiranje.OP_TablicaMasa (SerijaID, PrugaID, StanicaOdID, StanicaDoID, Smer, BrojLokomotiva, Sloj, MaxMasaT, VaziOd, VaziDo, Napomena) " +
            "VALUES (@s, @p, @o, @d, @sm, @b, @sl, @m, @vo, @vd, @n)";

        internal static void Dodaj(RedTabliceMasa r)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Command(c, null, Insert, Parametri(r)))
                cmd.ExecuteNonQuery();
        }

        internal static void Sacuvaj(int id, byte[] rowVer, RedTabliceMasa r)
        {
            var args = new List<object>(Parametri(r)) { "@id", id, "@rv", rowVer };
            using (var c = Db.Open())
                PlaniranjeDb.IzvrsiSaRowVer(c, null,
                    "UPDATE planiranje.OP_TablicaMasa SET SerijaID=@s, PrugaID=@p, StanicaOdID=@o, StanicaDoID=@d, Smer=@sm, BrojLokomotiva=@b, Sloj=@sl, MaxMasaT=@m, VaziOd=@vo, VaziDo=@vd, Napomena=@n " +
                    "WHERE ID=@id AND RowVer=@rv", args.ToArray());
        }

        internal static void Obrisi(int id, byte[] rowVer)
        {
            using (var c = Db.Open())
                PlaniranjeDb.IzvrsiSaRowVer(c, null, "DELETE FROM planiranje.OP_TablicaMasa WHERE ID=@id AND RowVer=@rv", "@id", id, "@rv", rowVer);
        }

        internal static void UveziSve(IEnumerable<RedTabliceMasa> redovi)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    foreach (RedTabliceMasa r in redovi)
                        using (var cmd = Db.Command(c, tx, Insert, Parametri(r)))
                            cmd.ExecuteNonQuery();
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }
    }
}
