using System;
using System.Collections.Generic;
using System.Data;
using Saobracaj.Agenti_jobs;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Podaci
{
    internal static class TurnusiRepo
    {
        internal static DataTable Svi()
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT ID, Naziv, CONVERT(varchar(5), Pocetak, 108) AS Pocetak, TrajanjeMin, DanaRada, DanaOdmora, Nocna, Aktivan FROM planiranje.OP_Turnus ORDER BY Naziv");
        }

        internal static void Sacuvaj(int? id, string naziv, TimeSpan pocetak, int trajanjeMin, int danaRada, int danaOdmora, bool nocna, bool aktivan)
        {
            using (var c = Db.Open())
            using (var cmd = id.HasValue
                ? Db.Command(c, null,
                    "UPDATE planiranje.OP_Turnus SET Naziv=@n, Pocetak=@p, TrajanjeMin=@t, DanaRada=@r, DanaOdmora=@o, Nocna=@no, Aktivan=@a WHERE ID=@id",
                    "@n", naziv, "@p", pocetak, "@t", trajanjeMin, "@r", danaRada, "@o", danaOdmora, "@no", nocna, "@a", aktivan, "@id", id.Value)
                : Db.Command(c, null,
                    "INSERT planiranje.OP_Turnus (Naziv, Pocetak, TrajanjeMin, DanaRada, DanaOdmora, Nocna, Aktivan) VALUES (@n, @p, @t, @r, @o, @no, @a)",
                    "@n", naziv, "@p", pocetak, "@t", trajanjeMin, "@r", danaRada, "@o", danaOdmora, "@no", nocna, "@a", aktivan))
                cmd.ExecuteNonQuery();
        }

        internal static DataTable DodeleOsobe(int deSifra)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT ot.ID, ot.DeSifra, ot.TurnusID, t.Naziv AS Turnus, ot.[Od], ot.[Do] FROM planiranje.OP_OsobljeTurnus ot " +
                    "JOIN planiranje.OP_Turnus t ON t.ID = ot.TurnusID WHERE ot.DeSifra=@d ORDER BY ot.[Od] DESC",
                    "@d", deSifra);
        }

        // Nova dodela zatvara prethodnu otvorenu, da osoba nema dva turnusa istog dana.
        internal static void Dodeli(int deSifra, int turnusId, DateTime od, DateTime? doDatuma)
        {
            OsobljeRepo.ObezbediPlan(deSifra);
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    using (var cmd = Db.Command(c, tx,
                        "UPDATE planiranje.OP_OsobljeTurnus SET [Do]=@pre WHERE DeSifra=@d AND [Do] IS NULL AND [Od] < @od",
                        "@pre", od.AddDays(-1), "@d", deSifra, "@od", od))
                        cmd.ExecuteNonQuery();
                    using (var cmd = Db.Command(c, tx,
                        "INSERT planiranje.OP_OsobljeTurnus (DeSifra, TurnusID, [Od], [Do]) VALUES (@d, @t, @od, @do)",
                        "@d", deSifra, "@t", turnusId, "@od", od, "@do", (object)doDatuma))
                        cmd.ExecuteNonQuery();
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        // Pravilo 8.8: smene iz turnusa; ručno unete (Izvor = RUCNO) se ne prepisuju.
        internal static int GenerisiSmene(DateTime od, DateTime doDatuma, IList<int> osobe)
        {
            int upisano = 0;
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    DataTable dodele = Db.Table(c, tx,
                        "SELECT ot.DeSifra, ot.[Od] AS DodelaOd, ot.[Do] AS DodelaDo, t.Pocetak, t.TrajanjeMin, t.DanaRada, t.DanaOdmora, t.Nocna " +
                        "FROM planiranje.OP_OsobljeTurnus ot JOIN planiranje.OP_Turnus t ON t.ID = ot.TurnusID " +
                        "WHERE ot.[Od] <= @do AND (ot.[Do] IS NULL OR ot.[Do] >= @od)",
                        "@od", od, "@do", doDatuma);
                    var izabrani = osobe == null ? null : new HashSet<int>(osobe);
                    foreach (DataRow r in dodele.Rows)
                    {
                        int osoba = Convert.ToInt32(r["DeSifra"]);
                        if (izabrani != null && !izabrani.Contains(osoba)) continue;
                        var turnus = new TurnusPodaci
                        {
                            Pocetak = (TimeSpan)r["Pocetak"],
                            TrajanjeMin = Convert.ToInt32(r["TrajanjeMin"]),
                            DanaRada = Convert.ToInt32(r["DanaRada"]),
                            DanaOdmora = Convert.ToInt32(r["DanaOdmora"]),
                            Nocna = Convert.ToBoolean(r["Nocna"])
                        };
                        DateTime dodelaOd = Convert.ToDateTime(r["DodelaOd"]);
                        DateTime? dodelaDo = r.IsNull("DodelaDo") ? (DateTime?)null : Convert.ToDateTime(r["DodelaDo"]);
                        foreach (SmenaDana s in Turnusi.Smene(turnus, dodelaOd, dodelaDo, od, doDatuma))
                        {
                            using (var cmd = Db.Command(c, tx,
                                "IF EXISTS (SELECT 1 FROM planiranje.OP_Smena WHERE DeSifra=@d AND Datum=@dat) " +
                                "UPDATE planiranje.OP_Smena SET Vrsta=@v, [Od]=@od, [Do]=@do, Izmenio=@k WHERE DeSifra=@d AND Datum=@dat AND Izvor <> N'RUCNO' " +
                                "ELSE INSERT planiranje.OP_Smena (DeSifra, Datum, Vrsta, [Od], [Do], Izvor, Izmenio) VALUES (@d, @dat, @v, @od, @do, N'TURNUS', @k)",
                                "@d", osoba, "@dat", s.Datum, "@v", s.Vrsta, "@od", (object)s.Od, "@do", (object)s.Do, "@k", PlaniranjeDb.Korisnik))
                                upisano += cmd.ExecuteNonQuery();
                        }
                    }
                    tx.Commit();
                    return upisano;
                }
                catch { tx.Rollback(); throw; }
            }
        }
    }
}
