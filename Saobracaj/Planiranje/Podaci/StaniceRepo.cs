using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Saobracaj.Agenti_jobs;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Podaci
{
    internal static class StaniceRepo
    {
        internal static DataTable Sve(bool samoAktivne)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT ID, Naziv, NazivCir, SifraIZS, UIC, Kod, Drzava, VrstaMesta, Pruge, MaxDuzinaVozaAB, MaxDuzinaVozaBA, Granicna, Latitude, Longitude, KoordIzvor, Aktivna, Izvor, IzvorniID, " +
                    "VrstaKod, Posednutost, OtvorenaZa, Vaga, Rampa, KiM, DuplikatOd, NazivStari, NazivRegistar, RowVer " +
                    "FROM planiranje.OP_Stanice WHERE (@sve = 1 OR Aktivna = 1) ORDER BY Naziv",
                    "@sve", samoAktivne ? 0 : 1);
        }

        internal static DataTable ZaIzbor()
        {
            using (var c = Db.Open())
                return Db.Table(c, null, "SELECT ID, Naziv FROM planiranje.OP_Stanice WHERE Aktivna = 1 ORDER BY Naziv");
        }

        internal static DataTable ZaIzborSrb()
        {
            using (var c = Db.Open())
                return Db.Table(c, null, "SELECT ID, Naziv FROM planiranje.OP_Stanice WHERE Aktivna = 1 AND Drzava = N'SRB' ORDER BY Naziv");
        }

        // Za izbor rute: stanica van mreže nema rutu, pa se ne nudi.
        internal static DataTable ZaRutu()
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT s.ID, s.Naziv FROM planiranje.OP_Stanice s WHERE s.Aktivna = 1 AND s.Drzava = N'SRB' " +
                    "AND EXISTS (SELECT 1 FROM planiranje.OP_MrezaVeza v WHERE v.Aktivna = 1 AND (v.StanicaAID = s.ID OR v.StanicaBID = s.ID)) ORDER BY s.Naziv");
        }

        // Elektrifikacija je sa deonice, bez zamene podatkom sa pruge: prazno znači da se ne zna.
        internal static List<DeonicaPruge> DeonicePruga()
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT v.ID, v.StanicaAID, v.StanicaBID, v.PrugaID, p.Sifra, p.Naziv, p.Kategorija, a.Naziv AS OdNaziv, b.Naziv AS DoNaziv, v.DuzinaKm, v.Elektrificirana, v.Klasa, v.Vmax, v.KiM, " +
                    "(SELECT MIN(m.RB) FROM planiranje.OP_PrugaMesto m WHERE m.PrugaID = v.PrugaID AND m.StanicaID IN (v.StanicaAID, v.StanicaBID)) AS RB " +
                    "FROM planiranje.OP_MrezaVeza v " +
                    "JOIN planiranje.OP_Stanice a ON a.ID = v.StanicaAID " +
                    "JOIN planiranje.OP_Stanice b ON b.ID = v.StanicaBID " +
                    "LEFT JOIN planiranje.OP_Pruga p ON p.ID = v.PrugaID " +
                    "WHERE v.Aktivna = 1 ORDER BY v.PrugaID, RB, v.ID")
                    .Rows.Cast<DataRow>().Select(r => new DeonicaPruge
                    {
                        VezaID = Convert.ToInt32(r["ID"]),
                        A = Convert.ToInt32(r["StanicaAID"]),
                        B = Convert.ToInt32(r["StanicaBID"]),
                        PrugaID = r.IsNull("PrugaID") ? (int?)null : Convert.ToInt32(r["PrugaID"]),
                        Sifra = Db.Text(r, "Sifra"),
                        Naziv = Db.Text(r, "Naziv"),
                        Kategorija = Db.Text(r, "Kategorija"),
                        OdNaziv = Db.Text(r, "OdNaziv"),
                        DoNaziv = Db.Text(r, "DoNaziv"),
                        Km = Convert.ToDecimal(r["DuzinaKm"]),
                        Elektrificirana = r.IsNull("Elektrificirana") ? (bool?)null : Convert.ToBoolean(r["Elektrificirana"]),
                        Klasa = Db.Text(r, "Klasa"),
                        Vmax = r.IsNull("Vmax") ? (int?)null : Convert.ToInt32(r["Vmax"]),
                        KiM = Convert.ToBoolean(r["KiM"])
                    }).ToList();
        }

        internal static DataTable Veze()
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT v.ID, v.DuzinaKm, COALESCE(v.Elektrificirana, p.Elektrificirana) AS Elektrificirana, v.KiM, v.GeomWKT, p.Sifra AS Pruga, " +
                    "a.Latitude AS LatA, a.Longitude AS LonA, b.Latitude AS LatB, b.Longitude AS LonB, a.Naziv AS NazivA, b.Naziv AS NazivB " +
                    "FROM planiranje.OP_MrezaVeza v " +
                    "JOIN planiranje.OP_Stanice a ON a.ID = v.StanicaAID " +
                    "JOIN planiranje.OP_Stanice b ON b.ID = v.StanicaBID " +
                    "LEFT JOIN planiranje.OP_Pruga p ON p.ID = v.PrugaID " +
                    "WHERE v.Aktivna = 1 AND ((a.Latitude IS NOT NULL AND b.Latitude IS NOT NULL) OR v.GeomWKT IS NOT NULL)");
        }

        // Kod i UIC se računaju iz šifre; bez šifre UIC ostaje kako je unet.
        internal static void Dodaj(string naziv, int? sifra, string uic, string drzava, string vrsta, double? lat, double? lon, bool granicna, bool aktivna)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Command(c, null,
                "INSERT planiranje.OP_Stanice (Naziv, SifraIZS, Kod, UIC, Drzava, VrstaMesta, Latitude, Longitude, KoordIzvor, Granicna, Aktivna, Izvor, Kreirao, DatumAzuriranja) " +
                "VALUES (@n, @s, @kod, @u, @dr, @v, @lat, @lon, CASE WHEN @lat IS NULL THEN NULL ELSE 'RUCNO' END, @g, @a, N'RUCNO', @k, SYSDATETIME())",
                "@n", naziv, "@s", (object)sifra, "@kod", (object)(sifra.HasValue ? NaziviRegistra.Kod(sifra.Value) : null), "@u", (object)(sifra.HasValue ? NaziviRegistra.UIC(sifra.Value) : uic),
                "@dr", (object)drzava, "@v", (object)vrsta, "@lat", (object)lat, "@lon", (object)lon, "@g", granicna, "@a", aktivna, "@k", PlaniranjeDb.Korisnik))
                cmd.ExecuteNonQuery();
        }

        // Naziv i šifra mesta iz Registra se menjaju samo uvozom novog Registra.
        internal static void Sacuvaj(int id, byte[] rowVer, string naziv, int? sifra, string uic, string drzava, string vrsta, double? lat, double? lon, bool granicna, bool aktivna)
        {
            using (var c = Db.Open())
                PlaniranjeDb.IzvrsiSaRowVer(c, null,
                    "UPDATE planiranje.OP_Stanice SET " +
                    "Naziv = CASE WHEN Izvor = N'REGISTAR' THEN Naziv ELSE @n END, " +
                    "SifraIZS = CASE WHEN Izvor = N'REGISTAR' THEN SifraIZS ELSE @s END, " +
                    "Kod = CASE WHEN Izvor = N'REGISTAR' OR @s IS NULL THEN Kod ELSE @kod END, " +
                    "UIC = CASE WHEN Izvor = N'REGISTAR' THEN UIC ELSE @u END, " +
                    "Drzava=@dr, VrstaMesta=@v, Granicna=@g, Aktivna=@a, " +
                    "KoordIzvor = CASE WHEN ISNULL(Latitude, -1) <> ISNULL(@lat, -1) OR ISNULL(Longitude, -1) <> ISNULL(@lon, -1) THEN 'RUCNO' ELSE KoordIzvor END, " +
                    "Latitude=@lat, Longitude=@lon, DatumAzuriranja=SYSDATETIME() " +
                    "WHERE ID=@id AND RowVer=@rv",
                    "@n", naziv, "@s", (object)sifra, "@kod", (object)(sifra.HasValue ? NaziviRegistra.Kod(sifra.Value) : null), "@u", (object)(sifra.HasValue ? NaziviRegistra.UIC(sifra.Value) : uic),
                    "@dr", (object)drzava, "@v", (object)vrsta, "@lat", (object)lat, "@lon", (object)lon, "@g", granicna, "@a", aktivna, "@id", id, "@rv", rowVer);
        }

        // Prevlačenje tačke na mapi; vraća novu verziju reda da sledeće prevlačenje ne traži osvežavanje.
        internal static byte[] PomeriNaMapi(int id, byte[] rowVer, double lat, double lon)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Command(c, null,
                "UPDATE planiranje.OP_Stanice SET Latitude=@lat, Longitude=@lon, KoordIzvor='RUCNO', DatumAzuriranja=SYSDATETIME() " +
                "OUTPUT INSERTED.RowVer WHERE ID=@id AND RowVer=@rv",
                "@lat", lat, "@lon", lon, "@id", id, "@rv", rowVer))
            {
                var nova = cmd.ExecuteScalar() as byte[];
                if (nova == null) throw new DBConcurrencyException(PlaniranjeDb.PorukaKonkurentnost);
                return nova;
            }
        }

        // Reference na drugu stanicu prelaze na prvu, a druga postaje neaktivna.
        internal static void Spoji(int prvaId, int drugaId)
        {
            string[] jednostavne =
            {
                "UPDATE planiranje.OP_Lokomotive SET MaticnaStanicaID=@p WHERE MaticnaStanicaID=@d",
                "UPDATE planiranje.OP_OsobljePlan SET MaticnaStanicaID=@p WHERE MaticnaStanicaID=@d",
                "UPDATE planiranje.OP_PrugaMesto SET StanicaID=@p WHERE StanicaID=@d",
                "UPDATE planiranje.OP_Trase SET PocetnaStanicaID=@p WHERE PocetnaStanicaID=@d",
                "UPDATE planiranje.OP_Trase SET KrajnjaStanicaID=@p WHERE KrajnjaStanicaID=@d",
                "UPDATE planiranje.OP_TrasaStanice SET StanicaID=@p WHERE StanicaID=@d",
                "UPDATE planiranje.OP_TablicaMasa SET StanicaOdID=@p WHERE StanicaOdID=@d",
                "UPDATE planiranje.OP_TablicaMasa SET StanicaDoID=@p WHERE StanicaDoID=@d",
                "UPDATE planiranje.OP_Posao SET PrijemStanicaID=@p WHERE PrijemStanicaID=@d",
                "UPDATE planiranje.OP_Posao SET PredajaStanicaID=@p WHERE PredajaStanicaID=@d",
                "UPDATE planiranje.OP_PosaoRuta SET StanicaOdID=@p WHERE StanicaOdID=@d",
                "UPDATE planiranje.OP_PosaoRuta SET StanicaDoID=@p WHERE StanicaDoID=@d",
                "UPDATE planiranje.OP_Smena SET LokacijaPosleStanicaID=@p WHERE LokacijaPosleStanicaID=@d",
                "UPDATE planiranje.OP_Zadatak SET StanicaID=@p WHERE StanicaID=@d",
                // Parametar prve stanice ima prednost, pa se isti parametar druge stanice briše.
                "DELETE d FROM planiranje.OP_Parametri d JOIN planiranje.OP_Parametri p ON p.Kljuc=d.Kljuc AND p.VaziOd=d.VaziOd " +
                "AND p.StanicaID=@p AND d.StanicaID=@d AND ISNULL(p.StanicaDoID,0)=ISNULL(d.StanicaDoID,0) AND ISNULL(p.SerijaID,0)=ISNULL(d.SerijaID,0)",
                "UPDATE planiranje.OP_Parametri SET StanicaID=@p WHERE StanicaID=@d",
                "UPDATE planiranje.OP_Parametri SET StanicaDoID=@p WHERE StanicaDoID=@d",
                // Veza koja bi postala petlja ili duplikat se briše, ostale prelaze na prvu stanicu.
                "DELETE FROM planiranje.OP_MrezaVeza WHERE (StanicaAID=@p AND StanicaBID=@d) OR (StanicaAID=@d AND StanicaBID=@p)",
                "DELETE v FROM planiranje.OP_MrezaVeza v WHERE v.StanicaAID=@d AND EXISTS (SELECT 1 FROM planiranje.OP_MrezaVeza x WHERE (x.StanicaAID=@p AND x.StanicaBID=v.StanicaBID) OR (x.StanicaBID=@p AND x.StanicaAID=v.StanicaBID))",
                "DELETE v FROM planiranje.OP_MrezaVeza v WHERE v.StanicaBID=@d AND EXISTS (SELECT 1 FROM planiranje.OP_MrezaVeza x WHERE (x.StanicaAID=@p AND x.StanicaBID=v.StanicaAID) OR (x.StanicaBID=@p AND x.StanicaAID=v.StanicaAID))",
                "UPDATE planiranje.OP_MrezaVeza SET StanicaAID=@p WHERE StanicaAID=@d",
                "UPDATE planiranje.OP_MrezaVeza SET StanicaBID=@p WHERE StanicaBID=@d",
                // Red ostaje jer najave preko IzvorniID i dalje vode na njega; DuplikatOd ih šalje na prvu stanicu.
                "UPDATE planiranje.OP_Stanice SET DuplikatOd=@p WHERE DuplikatOd=@d",
                "UPDATE planiranje.OP_Stanice SET Aktivna=0, DuplikatOd=@p, DatumAzuriranja=SYSDATETIME() WHERE ID=@d"
            };
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    foreach (string sql in jednostavne)
                        using (var cmd = Db.Command(c, tx, sql, "@p", prvaId, "@d", drugaId))
                            cmd.ExecuteNonQuery();
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        // Stari šifarnik se samo čita, a dodaju se samo stanice kojih još nema.
        internal static int DopuniIzStarog()
        {
            using (var c = Db.Open())
            using (var cmd = Db.Command(c, null,
                "INSERT planiranje.OP_Stanice (Naziv, Kod, Drzava, Granicna, Latitude, Longitude, Izvor, IzvorniID, Kreirao) \n" +
                "SELECT RTRIM(s.Opis), NULLIF(RTRIM(s.Kod), N''), NULLIF(RTRIM(s.Drzava), N''), CASE WHEN s.Granicna = 1 THEN 1 ELSE 0 END, s.Latitude, s.Longitude, N'STANICE', s.ID, @k \n" +
                "FROM dbo.stanice s \n" +
                "WHERE NULLIF(RTRIM(s.Opis), N'') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM planiranje.OP_Stanice o WHERE o.IzvorniID = s.ID)",
                "@k", PlaniranjeDb.Korisnik))
                return cmd.ExecuteNonQuery();
        }
    }
}
