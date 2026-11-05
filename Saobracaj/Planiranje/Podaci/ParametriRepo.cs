using System;
using System.Collections.Generic;
using System.Data;
using Saobracaj.Agenti_jobs;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Podaci
{
    internal static class ParametriRepo
    {
        internal static DataTable Svi()
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT ID, Kljuc, StanicaID, StanicaDoID, SerijaID, Vrednost, Jedinica, Opis, Izvor, VaziOd, VaziDo, Izmenio, Izmenjeno, RowVer " +
                    "FROM planiranje.OP_Parametri ORDER BY Kljuc, VaziOd DESC");
        }

        internal static DataTable SviSaNazivima()
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT p.ID, p.Kljuc, p.StanicaID, s.Naziv AS [Stanica od], p.StanicaDoID, d.Naziv AS [Stanica do], p.SerijaID, r.Oznaka AS Serija, " +
                    "p.Vrednost, p.Jedinica, p.VaziOd, p.VaziDo, p.Izvor, p.Opis, p.Izmenio, p.Izmenjeno, p.RowVer " +
                    "FROM planiranje.OP_Parametri p " +
                    "LEFT JOIN planiranje.OP_Stanice s ON s.ID = p.StanicaID " +
                    "LEFT JOIN planiranje.OP_Stanice d ON d.ID = p.StanicaDoID " +
                    "LEFT JOIN planiranje.OP_SerijeLokomotiva r ON r.ID = p.SerijaID " +
                    "ORDER BY p.Kljuc, p.VaziOd DESC");
        }

        internal static void Dodaj(string kljuc, int? stanica, int? stanicaDo, int? serija, decimal vrednost, string jedinica, string opis, string izvor, DateTime vaziOd, DateTime? vaziDo)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Command(c, null,
                "INSERT planiranje.OP_Parametri (Kljuc, StanicaID, StanicaDoID, SerijaID, Vrednost, Jedinica, Opis, Izvor, VaziOd, VaziDo, Izmenio) " +
                "VALUES (@k, @s, @d, @r, @v, @j, @o, @i, @od, @do, @u)",
                "@k", kljuc, "@s", (object)stanica, "@d", (object)stanicaDo, "@r", (object)serija, "@v", vrednost, "@j", (object)jedinica, "@o", (object)opis, "@i", (object)izvor, "@od", vaziOd, "@do", (object)vaziDo, "@u", PlaniranjeDb.Korisnik))
                cmd.ExecuteNonQuery();
        }

        internal static void Sacuvaj(int id, byte[] rowVer, string kljuc, int? stanica, int? stanicaDo, int? serija, decimal vrednost, string jedinica, string opis, string izvor, DateTime vaziOd, DateTime? vaziDo)
        {
            using (var c = Db.Open())
                PlaniranjeDb.IzvrsiSaRowVer(c, null,
                    "UPDATE planiranje.OP_Parametri SET Kljuc=@k, StanicaID=@s, StanicaDoID=@d, SerijaID=@r, Vrednost=@v, Jedinica=@j, Opis=@o, Izvor=@i, VaziOd=@od, VaziDo=@do, Izmenio=@u, Izmenjeno=SYSDATETIME() " +
                    "WHERE ID=@id AND RowVer=@rv",
                    "@k", kljuc, "@s", (object)stanica, "@d", (object)stanicaDo, "@r", (object)serija, "@v", vrednost, "@j", (object)jedinica, "@o", (object)opis, "@i", (object)izvor, "@od", vaziOd, "@do", (object)vaziDo, "@u", PlaniranjeDb.Korisnik, "@id", id, "@rv", rowVer);
        }

        internal static decimal Vrednost(string kljuc, DateTime datum, int? stanicaId, int? stanicaDoId, int? serijaId, decimal podrazumevano)
        {
            return ParametriPravila.Nadji(ZaKljuc(kljuc), datum, stanicaId, stanicaDoId, serijaId) ?? podrazumevano;
        }

        internal static List<ParametarRed> ZaKljuc(string kljuc)
        {
            var redovi = new List<ParametarRed>();
            using (var c = Db.Open())
            {
                DataTable t = Db.Table(c, null,
                    "SELECT StanicaID, StanicaDoID, SerijaID, Vrednost, VaziOd, VaziDo FROM planiranje.OP_Parametri WHERE Kljuc = @k",
                    "@k", kljuc);
                foreach (DataRow r in t.Rows)
                    redovi.Add(new ParametarRed
                    {
                        StanicaID = r.IsNull("StanicaID") ? (int?)null : Convert.ToInt32(r["StanicaID"]),
                        StanicaDoID = r.IsNull("StanicaDoID") ? (int?)null : Convert.ToInt32(r["StanicaDoID"]),
                        SerijaID = r.IsNull("SerijaID") ? (int?)null : Convert.ToInt32(r["SerijaID"]),
                        Vrednost = Convert.ToDecimal(r["Vrednost"]),
                        VaziOd = Convert.ToDateTime(r["VaziOd"]),
                        VaziDo = r.IsNull("VaziDo") ? (DateTime?)null : Convert.ToDateTime(r["VaziDo"])
                    });
            }
            return redovi;
        }
    }
}
