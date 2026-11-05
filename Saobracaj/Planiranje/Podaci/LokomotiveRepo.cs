using System;
using System.Data;
using Saobracaj.Agenti_jobs;

namespace Saobracaj.Planiranje.Podaci
{
    internal static class LokomotiveRepo
    {
        internal static DataTable Lokomotive(bool samoAktivne)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT l.ID, l.Broj, l.Naziv, l.SerijaID, s.Oznaka AS Serija, s.VrstaVuce, l.Vlasnistvo, l.Zakupodavac, " +
                    "l.ImaGPS, l.TechnodisID, l.MaticnaStanicaID, m.Naziv AS MaticnaStanica, l.MasaT, l.Aktivna, l.IzvorniSmSifra, l.RowVer " +
                    "FROM planiranje.OP_Lokomotive l " +
                    "LEFT JOIN planiranje.OP_SerijeLokomotiva s ON s.ID = l.SerijaID " +
                    "LEFT JOIN planiranje.OP_Stanice m ON m.ID = l.MaticnaStanicaID " +
                    "WHERE (@sve = 1 OR l.Aktivna = 1) ORDER BY l.Broj",
                    "@sve", samoAktivne ? 0 : 1);
        }

        internal static DataTable Serije()
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT ID, Oznaka, Opis, VrstaVuce, MasaT, SnagaKW, VmaxKmh, DuzinaM, Aktivna, IzvorniID, RowVer " +
                    "FROM planiranje.OP_SerijeLokomotiva ORDER BY Oznaka");
        }

        internal static void DodajLokomotivu(string broj, string naziv, int? serija, string vlasnistvo, string zakupodavac, bool gps, string technodis, int? maticna, decimal? masa, bool aktivna)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Command(c, null,
                "INSERT planiranje.OP_Lokomotive (Broj, Naziv, SerijaID, Vlasnistvo, Zakupodavac, ImaGPS, TechnodisID, MaticnaStanicaID, MasaT, Aktivna) " +
                "VALUES (@b, @n, @s, @v, @z, @g, @t, @m, @ma, @a)",
                "@b", broj, "@n", (object)naziv, "@s", (object)serija, "@v", vlasnistvo, "@z", (object)zakupodavac, "@g", gps, "@t", (object)technodis, "@m", (object)maticna, "@ma", (object)masa, "@a", aktivna))
                cmd.ExecuteNonQuery();
        }

        internal static void SacuvajLokomotivu(int id, byte[] rowVer, string broj, string naziv, int? serija, string vlasnistvo, string zakupodavac, bool gps, string technodis, int? maticna, decimal? masa, bool aktivna)
        {
            using (var c = Db.Open())
                PlaniranjeDb.IzvrsiSaRowVer(c, null,
                    "UPDATE planiranje.OP_Lokomotive SET Broj=@b, Naziv=@n, SerijaID=@s, Vlasnistvo=@v, Zakupodavac=@z, ImaGPS=@g, TechnodisID=@t, MaticnaStanicaID=@m, MasaT=@ma, Aktivna=@a " +
                    "WHERE ID=@id AND RowVer=@rv",
                    "@b", broj, "@n", (object)naziv, "@s", (object)serija, "@v", vlasnistvo, "@z", (object)zakupodavac, "@g", gps, "@t", (object)technodis, "@m", (object)maticna, "@ma", (object)masa, "@a", aktivna, "@id", id, "@rv", rowVer);
        }

        internal static void DodajSeriju(string oznaka, string opis, string vuca, decimal? masa, decimal? snaga, int? vmax, decimal? duzina, bool aktivna)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Command(c, null,
                "INSERT planiranje.OP_SerijeLokomotiva (Oznaka, Opis, VrstaVuce, MasaT, SnagaKW, VmaxKmh, DuzinaM, Aktivna) VALUES (@o, @op, @v, @m, @s, @x, @d, @a)",
                "@o", oznaka, "@op", (object)opis, "@v", (object)vuca, "@m", (object)masa, "@s", (object)snaga, "@x", (object)vmax, "@d", (object)duzina, "@a", aktivna))
                cmd.ExecuteNonQuery();
        }

        internal static void SacuvajSeriju(int id, byte[] rowVer, string oznaka, string opis, string vuca, decimal? masa, decimal? snaga, int? vmax, decimal? duzina, bool aktivna)
        {
            using (var c = Db.Open())
                PlaniranjeDb.IzvrsiSaRowVer(c, null,
                    "UPDATE planiranje.OP_SerijeLokomotiva SET Oznaka=@o, Opis=@op, VrstaVuce=@v, MasaT=@m, SnagaKW=@s, VmaxKmh=@x, DuzinaM=@d, Aktivna=@a WHERE ID=@id AND RowVer=@rv",
                    "@o", oznaka, "@op", (object)opis, "@v", (object)vuca, "@m", (object)masa, "@s", (object)snaga, "@x", (object)vmax, "@d", (object)duzina, "@a", aktivna, "@id", id, "@rv", rowVer);
        }

        internal static DataTable Zauzetosti()
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT z.ID, z.LokomotivaID, l.Broj AS Lokomotiva, z.Vrsta, z.[Od], z.[Do], z.Projekat, z.Napomena, z.Kreirao, z.RowVer " +
                    "FROM planiranje.OP_LokomotivaZauzetost z JOIN planiranje.OP_Lokomotive l ON l.ID = z.LokomotivaID ORDER BY z.[Od] DESC");
        }

        // Zauzetost ne sme da se preklopi sa već dodeljenim poslom; provera i upis su u istoj transakciji.
        internal static void SacuvajZauzetost(int? id, byte[] rowVer, int lokomotiva, string vrsta, DateTime od, DateTime doVreme, string projekat, string napomena)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OP_Lok_" + lokomotiva);
                    DataTable preklop = Db.Table(c, tx,
                        "SELECT COUNT(*) AS n FROM planiranje.OP_PosaoLokomotiva pl JOIN planiranje.OP_Posao p ON p.ID = pl.PosaoID " +
                        "WHERE pl.LokomotivaID=@l AND p.Status <> N'OTKAZAN' AND pl.[Od] < @do AND pl.[Do] > @od",
                        "@l", lokomotiva, "@od", od, "@do", doVreme);
                    if (Convert.ToInt32(preklop.Rows[0]["n"]) > 0)
                        throw new InvalidOperationException("Lokomotiva u tom periodu već ima dodeljen posao.");
                    if (id.HasValue)
                        PlaniranjeDb.IzvrsiSaRowVer(c, tx,
                            "UPDATE planiranje.OP_LokomotivaZauzetost SET LokomotivaID=@l, Vrsta=@v, [Od]=@od, [Do]=@do, Projekat=@p, Napomena=@n WHERE ID=@id AND RowVer=@rv",
                            "@l", lokomotiva, "@v", vrsta, "@od", od, "@do", doVreme, "@p", (object)projekat, "@n", (object)napomena, "@id", id.Value, "@rv", rowVer);
                    else
                        using (var cmd = Db.Command(c, tx,
                            "INSERT planiranje.OP_LokomotivaZauzetost (LokomotivaID, Vrsta, [Od], [Do], Projekat, Napomena, Kreirao) VALUES (@l, @v, @od, @do, @p, @n, @k)",
                            "@l", lokomotiva, "@v", vrsta, "@od", od, "@do", doVreme, "@p", (object)projekat, "@n", (object)napomena, "@k", PlaniranjeDb.Korisnik))
                            cmd.ExecuteNonQuery();
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        internal static void ObrisiZauzetost(int id, byte[] rowVer)
        {
            using (var c = Db.Open())
                PlaniranjeDb.IzvrsiSaRowVer(c, null, "DELETE FROM planiranje.OP_LokomotivaZauzetost WHERE ID=@id AND RowVer=@rv", "@id", id, "@rv", rowVer);
        }

        // Stari šifarnik se samo čita, a dodaju se samo serije i lokomotive kojih još nema.
        internal static int DopuniIzStarog()
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    int ukupno;
                    using (var cmd = Db.Command(c, tx,
                        "INSERT planiranje.OP_SerijeLokomotiva (Oznaka, Opis, IzvorniID) \n" +
                        "SELECT LEFT(RTRIM(ls.Oznaka), 20), NULLIF(RTRIM(ls.Opis), N''), ls.ID \n" +
                        "FROM dbo.LokomotivaSerija ls \n" +
                        "WHERE NULLIF(RTRIM(ls.Oznaka), N'') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM planiranje.OP_SerijeLokomotiva o WHERE o.Oznaka = LEFT(RTRIM(ls.Oznaka), 20))"))
                        ukupno = cmd.ExecuteNonQuery();
                    using (var cmd = Db.Command(c, tx,
                        "INSERT planiranje.OP_Lokomotive (Broj, Naziv, MasaT, IzvorniSmSifra) \n" +
                        "SELECT LEFT(RTRIM(m.SmSifra), 20), NULLIF(RTRIM(m.SmNaziv), N''), CAST(m.SmPogDela AS decimal(10,2)), LEFT(RTRIM(m.SmSifra), 20) \n" +
                        "FROM dbo.Mesta m \n" +
                        "WHERE m.Lokomotiva = 1 AND NULLIF(RTRIM(m.SmSifra), N'') IS NOT NULL \n" +
                        "AND NOT EXISTS (SELECT 1 FROM planiranje.OP_Lokomotive o WHERE o.IzvorniSmSifra = LEFT(RTRIM(m.SmSifra), 20) OR o.Broj = LEFT(RTRIM(m.SmSifra), 20))"))
                        ukupno += cmd.ExecuteNonQuery();
                    tx.Commit();
                    return ukupno;
                }
                catch { tx.Rollback(); throw; }
            }
        }
    }
}
