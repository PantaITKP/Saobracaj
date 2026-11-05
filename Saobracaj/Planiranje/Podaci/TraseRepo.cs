using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using Saobracaj.Agenti_jobs;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Podaci
{
    internal static class TraseRepo
    {
        private const string Kolone =
            "t.ID, t.VerzijaID, t.BrojVoza, t.PocetnaStanicaID, t.KrajnjaStanicaID, t.PocetnaNaziv, t.KrajnjaNaziv, t.Relacija, t.Rang, " +
            "t.VremePolaska, t.VremeDolaska, t.DanPlus, t.DaniSaobracaja, t.MaxMasaT, t.MaxDuzinaM, t.ProcenatKocenja, t.Cena, t.SerijaLokomotive, " +
            "t.Napomena, t.AdHoc, t.AdHocNajavaID, t.AdHocVaziOd, t.AdHocVaziDo, t.AdHocStatus, t.StanjeRazlike, t.OpisPromene, t.Greska, " +
            "t.RastojanjeKm, t.KmMagistralno, t.KmRegionalno, t.KmLokalno, t.KmManipulativno, t.KmDizel, t.KmElektro, t.NajmanjiVmax, t.NajveciUspon, t.RutaIzracunata, t.RowVer";

        internal static DataTable Verzije()
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT ID, Naziv, Godina, VaziOd, VaziDo, Status, Izvor, PrethodnaVerzijaID, OriginalniNaziv, Uvezao, Uvezeno, Objavio, Objavljeno " +
                    "FROM planiranje.OP_TraseVerzija ORDER BY Godina DESC, ID DESC");
        }

        internal static DataTable Trase(int verzijaId)
        {
            using (var c = Db.Open())
                return Db.Table(c, null, "SELECT " + Kolone + " FROM planiranje.OP_Trase t WHERE t.VerzijaID = @v ORDER BY t.BrojVoza", "@v", verzijaId);
        }

        internal static DataTable ZahteviAdHoc()
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT " + Kolone + " FROM planiranje.OP_Trase t WHERE t.AdHoc = 1 AND t.AdHocStatus = N'ZAHTEV' ORDER BY t.Kreirano");
        }

        // Važeća verzija za godinu, kao poređenje pri uvozu.
        internal static List<RedTrase> VazecaZaGodinu(int godina, out int? verzijaId)
        {
            verzijaId = null;
            var redovi = new List<RedTrase>();
            using (var c = Db.Open())
            {
                DataTable v = Db.Table(c, null, "SELECT ID FROM planiranje.OP_TraseVerzija WHERE Godina=@g AND Status=N'VAZECA'", "@g", godina);
                if (v.Rows.Count == 0) return redovi;
                verzijaId = Convert.ToInt32(v.Rows[0]["ID"]);
                DataTable t = Db.Table(c, null, "SELECT " + Kolone + " FROM planiranje.OP_Trase t WHERE t.VerzijaID=@v", "@v", verzijaId.Value);
                foreach (DataRow r in t.Rows)
                    redovi.Add(new RedTrase
                    {
                        BrojVoza = Db.Text(r, "BrojVoza"),
                        PocetnaNaziv = Db.Text(r, "PocetnaNaziv"),
                        KrajnjaNaziv = Db.Text(r, "KrajnjaNaziv"),
                        PocetnaStanicaID = r.IsNull("PocetnaStanicaID") ? (int?)null : Convert.ToInt32(r["PocetnaStanicaID"]),
                        KrajnjaStanicaID = r.IsNull("KrajnjaStanicaID") ? (int?)null : Convert.ToInt32(r["KrajnjaStanicaID"]),
                        Rang = r.IsNull("Rang") ? null : Db.Text(r, "Rang"),
                        VremePolaska = r.IsNull("VremePolaska") ? (TimeSpan?)null : (TimeSpan)r["VremePolaska"],
                        VremeDolaska = r.IsNull("VremeDolaska") ? (TimeSpan?)null : (TimeSpan)r["VremeDolaska"],
                        DaniSaobracaja = r.IsNull("DaniSaobracaja") ? null : Db.Text(r, "DaniSaobracaja"),
                        MaxMasaT = r.IsNull("MaxMasaT") ? (decimal?)null : Convert.ToDecimal(r["MaxMasaT"]),
                        MaxDuzinaM = r.IsNull("MaxDuzinaM") ? (decimal?)null : Convert.ToDecimal(r["MaxDuzinaM"])
                    });
            }
            return redovi;
        }

        internal static List<KandidatStanice> StaniceZaPoredjenje()
        {
            var lista = new List<KandidatStanice>();
            foreach (DataRow r in StaniceRepo.Sve(true).Rows)
                lista.Add(new KandidatStanice
                {
                    ID = Convert.ToInt32(r["ID"]),
                    Naziv = Db.Text(r, "Naziv"),
                    Latitude = r.IsNull("Latitude") ? (double?)null : Convert.ToDouble(r["Latitude"]),
                    Longitude = r.IsNull("Longitude") ? (double?)null : Convert.ToDouble(r["Longitude"])
                });
            // Trase su pisane i starim nazivima, pa stari naziv i naziv ugašenog duplikata vode na važeću stanicu.
            using (var c = Db.Open())
                foreach (DataRow r in Db.Table(c, null,
                    "SELECT s.ID, s.NazivStari AS Naziv, s.Latitude, s.Longitude FROM planiranje.OP_Stanice s WHERE s.Aktivna = 1 AND s.NazivStari IS NOT NULL " +
                    "UNION ALL SELECT g.ID, d.Naziv, g.Latitude, g.Longitude FROM planiranje.OP_Stanice d JOIN planiranje.OP_Stanice g ON g.ID = d.DuplikatOd WHERE g.Aktivna = 1").Rows)
                    lista.Add(new KandidatStanice
                    {
                        ID = Convert.ToInt32(r["ID"]),
                        Naziv = Db.Text(r, "Naziv"),
                        Latitude = r.IsNull("Latitude") ? (double?)null : Convert.ToDouble(r["Latitude"]),
                        Longitude = r.IsNull("Longitude") ? (double?)null : Convert.ToDouble(r["Longitude"])
                    });
            return lista;
        }

        // Objava: prethodna važeća postaje arhiva, a nova verzija važeća; sve u jednoj transakciji.
        internal static int Objavi(string naziv, int godina, DateTime vaziOd, DateTime vaziDo, string izvor, string originalniNaziv, byte[] sadrzaj, IEnumerable<RedTrase> redovi)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OP_Trase_Godina_" + godina);
                    DataTable pret = Db.Table(c, tx, "SELECT ID FROM planiranje.OP_TraseVerzija WHERE Godina=@g AND Status=N'VAZECA'", "@g", godina);
                    object prethodna = pret.Rows.Count == 0 ? null : pret.Rows[0]["ID"];
                    if (prethodna != null)
                        using (var cmd = Db.Command(c, tx, "UPDATE planiranje.OP_TraseVerzija SET Status=N'ARHIVA' WHERE ID=@id", "@id", prethodna))
                            cmd.ExecuteNonQuery();
                    int verzija;
                    using (var cmd = Db.Command(c, tx,
                        "INSERT planiranje.OP_TraseVerzija (Naziv, Godina, VaziOd, VaziDo, Status, Izvor, PrethodnaVerzijaID, OriginalniNaziv, Sha256, Sadrzaj, Uvezao, Objavio, Objavljeno) " +
                        "VALUES (@n, @g, @od, @do, N'VAZECA', @i, @p, @o, @h, @s, @k, @k, SYSDATETIME()); SELECT CAST(SCOPE_IDENTITY() AS int);",
                        "@n", naziv, "@g", godina, "@od", vaziOd, "@do", vaziDo, "@i", izvor, "@p", prethodna, "@o", (object)originalniNaziv, "@h", (object)Hes(sadrzaj), "@k", PlaniranjeDb.Korisnik))
                    {
                        var par = cmd.Parameters.Add("@s", SqlDbType.VarBinary, -1);
                        par.Value = (object)sadrzaj ?? DBNull.Value;
                        verzija = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    foreach (RedTrase r in redovi)
                    {
                        // Ukinuta trasa ostaje samo u prethodnoj verziji.
                        if (r.Stanje == "UKINUTA") continue;
                        using (var cmd = Db.Command(c, tx,
                            "INSERT planiranje.OP_Trase (VerzijaID, BrojVoza, PocetnaStanicaID, KrajnjaStanicaID, PocetnaNaziv, KrajnjaNaziv, Relacija, Rang, VremePolaska, VremeDolaska, DanPlus, DaniSaobracaja, MaxMasaT, MaxDuzinaM, ProcenatKocenja, Cena, SerijaLokomotive, Napomena, StanjeRazlike, OpisPromene, Greska, Kreirao) " +
                            "VALUES (@v, @b, @ps, @ks, @pn, @kn, @rel, @ra, @vp, @vd, @dp, @da, @mm, @md, @pk, @ce, @sl, @na, @st, @op, @gr, @k)",
                            "@v", verzija, "@b", Skrati(r.BrojVoza, 20), "@ps", (object)r.PocetnaStanicaID, "@ks", (object)r.KrajnjaStanicaID, "@pn", Skrati(r.PocetnaNaziv, 100), "@kn", Skrati(r.KrajnjaNaziv, 100),
                            "@rel", (object)Skrati(r.Relacija, 300), "@ra", (object)Skrati(r.Rang, 10), "@vp", (object)r.VremePolaska, "@vd", (object)r.VremeDolaska, "@dp", r.DanPlus, "@da", (object)Skrati(r.DaniSaobracaja, 50),
                            "@mm", (object)r.MaxMasaT, "@md", (object)r.MaxDuzinaM, "@pk", (object)r.ProcenatKocenja, "@ce", (object)r.Cena, "@sl", (object)Skrati(r.SerijaLokomotive, 50), "@na", (object)Skrati(r.Napomena, 1000),
                            "@st", r.Stanje, "@op", (object)Skrati(r.OpisPromene, 400), "@gr", (object)Skrati(r.Greska, 400), "@k", PlaniranjeDb.Korisnik))
                            cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                    return verzija;
                }
                catch { tx.Rollback(); throw; }
            }
        }

        // Ručna ispravka stanice; greška se briše kad su stanice i vremena u redu.
        internal static void IspraviStanice(int id, byte[] rowVer, int? pocetna, int? krajnja)
        {
            using (var c = Db.Open())
                PlaniranjeDb.IzvrsiSaRowVer(c, null,
                    "UPDATE planiranje.OP_Trase SET PocetnaStanicaID=@p, KrajnjaStanicaID=@k, " +
                    "Greska = CASE WHEN @p IS NOT NULL AND @k IS NOT NULL AND VremePolaska IS NOT NULL AND VremeDolaska IS NOT NULL THEN NULL ELSE Greska END " +
                    "WHERE ID=@id AND RowVer=@rv",
                    "@p", (object)pocetna, "@k", (object)krajnja, "@id", id, "@rv", rowVer);
        }

        // Trase koje važe na dan: važeća verzija i odobrene ad hoc. Veza sa stanicama se dopunjuje samo u memoriji.
        internal static List<TrasaZaRutu> ZaKombinacije(DateTime datum)
        {
            var trase = new List<TrasaZaRutu>();
            var stanice = new List<StanicaZaTrasu>();
            using (var c = Db.Open())
            {
                foreach (DataRow r in Db.Table(c, null,
                    "SELECT t.ID, t.BrojVoza, t.PocetnaStanicaID, t.KrajnjaStanicaID, t.PocetnaNaziv, t.KrajnjaNaziv, t.MaxMasaT, t.MaxDuzinaM " +
                    "FROM planiranje.OP_Trase t LEFT JOIN planiranje.OP_TraseVerzija v ON v.ID = t.VerzijaID " +
                    "WHERE (t.AdHoc = 0 AND v.Status = N'VAZECA' AND v.VaziOd <= @d AND v.VaziDo >= @d) " +
                    "OR (t.AdHoc = 1 AND t.AdHocStatus = N'ODOBRENA' AND (t.AdHocVaziOd IS NULL OR t.AdHocVaziOd <= @d) AND (t.AdHocVaziDo IS NULL OR t.AdHocVaziDo >= @d))",
                    "@d", datum.Date).Rows)
                    trase.Add(new TrasaZaRutu
                    {
                        ID = Convert.ToInt32(r["ID"]),
                        Broj = Db.Text(r, "BrojVoza"),
                        OdID = r.IsNull("PocetnaStanicaID") ? (int?)null : Convert.ToInt32(r["PocetnaStanicaID"]),
                        DoID = r.IsNull("KrajnjaStanicaID") ? (int?)null : Convert.ToInt32(r["KrajnjaStanicaID"]),
                        OdNaziv = Db.Text(r, "PocetnaNaziv"),
                        DoNaziv = Db.Text(r, "KrajnjaNaziv"),
                        MaxMasaT = r.IsNull("MaxMasaT") ? (decimal?)null : Convert.ToDecimal(r["MaxMasaT"]),
                        MaxDuzinaM = r.IsNull("MaxDuzinaM") ? (decimal?)null : Convert.ToDecimal(r["MaxDuzinaM"])
                    });
                foreach (DataRow r in Db.Table(c, null, "SELECT ID, Naziv, NazivStari, Drzava, GranicniPrelaz, DuplikatOd FROM planiranje.OP_Stanice").Rows)
                    stanice.Add(new StanicaZaTrasu
                    {
                        ID = Convert.ToInt32(r["ID"]),
                        Naziv = Db.Text(r, "Naziv"),
                        NazivStari = r.IsNull("NazivStari") ? null : Db.Text(r, "NazivStari"),
                        Drzava = r.IsNull("Drzava") ? null : Db.Text(r, "Drzava"),
                        GranicniPrelaz = !r.IsNull("GranicniPrelaz") && Convert.ToBoolean(r["GranicniPrelaz"]),
                        DuplikatOd = r.IsNull("DuplikatOd") ? (int?)null : Convert.ToInt32(r["DuplikatOd"])
                    });
            }
            KombinacijeTrasa.PoveziStanice(trase, stanice);
            return trase;
        }

        // Rezultat rutiranja: polja trase i spisak službenih mesta, u jednoj transakciji.
        internal static void SacuvajRutu(int id, byte[] rowVer, RezultatRute r)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    PlaniranjeDb.IzvrsiSaRowVer(c, tx,
                        "UPDATE planiranje.OP_Trase SET RastojanjeKm=@km, KmMagistralno=@m, KmRegionalno=@re, KmLokalno=@l, KmManipulativno=@mp, KmDizel=@d, KmElektro=@e, " +
                        "NajmanjiVmax=@v, NajveciUspon=@u, RutaIzracunata=SYSDATETIME() WHERE ID=@id AND RowVer=@rv",
                        "@km", r.Km, "@m", r.KmMagistralno, "@re", r.KmRegionalno, "@l", r.KmLokalno, "@mp", r.KmManipulativno, "@d", r.KmDizel, "@e", r.KmElektro,
                        "@v", (object)r.NajmanjiVmax, "@u", (object)r.NajveciUspon, "@id", id, "@rv", rowVer);
                    using (var cmd = Db.Command(c, tx, "DELETE planiranje.OP_TrasaStanice WHERE TrasaID=@t", "@t", id))
                        cmd.ExecuteNonQuery();
                    for (int i = 0; i < r.Mesta.Count; i++)
                    {
                        object pruga = r.Deonice.Count == 0 ? null : (object)r.Deonice[Math.Min(i, r.Deonice.Count - 1)].PrugaID;
                        using (var cmd = Db.Command(c, tx, "INSERT planiranje.OP_TrasaStanice (TrasaID, RB, StanicaID, PrugaID) VALUES (@t, @rb, @s, @p)",
                            "@t", id, "@rb", i + 1, "@s", r.Mesta[i], "@p", pruga))
                            cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        internal static void ZatraziAdHoc(SqlConnection c, SqlTransaction tx, int najavaId, string pocetnaNaziv, string krajnjaNaziv, int? pocetna, int? krajnja, string napomena)
        {
            using (var cmd = Db.Command(c, tx,
                "INSERT planiranje.OP_Trase (BrojVoza, PocetnaStanicaID, KrajnjaStanicaID, PocetnaNaziv, KrajnjaNaziv, AdHoc, AdHocNajavaID, AdHocStatus, Napomena, Kreirao) " +
                "VALUES (N'ADHOC', @ps, @ks, @pn, @kn, 1, @n, N'ZAHTEV', @na, @k)",
                "@ps", (object)pocetna, "@ks", (object)krajnja, "@pn", Skrati(pocetnaNaziv, 100), "@kn", Skrati(krajnjaNaziv, 100), "@n", najavaId, "@na", (object)napomena, "@k", PlaniranjeDb.Korisnik))
                cmd.ExecuteNonQuery();
        }

        internal static void OdobriAdHoc(int id, byte[] rowVer, string broj, int? pocetna, int? krajnja, TimeSpan? polazak, TimeSpan? dolazak, decimal? maxMasa, DateTime vaziOd, DateTime vaziDo, string napomena)
        {
            int danPlus = polazak.HasValue && dolazak.HasValue && dolazak < polazak ? 1 : 0;
            using (var c = Db.Open())
                PlaniranjeDb.IzvrsiSaRowVer(c, null,
                    "UPDATE planiranje.OP_Trase SET BrojVoza=@b, PocetnaStanicaID=@p, KrajnjaStanicaID=@k, VremePolaska=@vp, VremeDolaska=@vd, DanPlus=@dp, MaxMasaT=@m, AdHocVaziOd=@od, AdHocVaziDo=@do, Napomena=@n, AdHocStatus=N'ODOBRENA' " +
                    "WHERE ID=@id AND RowVer=@rv",
                    "@b", Skrati(broj, 20), "@p", (object)pocetna, "@k", (object)krajnja, "@vp", (object)polazak, "@vd", (object)dolazak, "@dp", danPlus, "@m", (object)maxMasa, "@od", vaziOd, "@do", vaziDo, "@n", (object)napomena, "@id", id, "@rv", rowVer);
        }

        internal static void OdbijAdHoc(int id, byte[] rowVer, string napomena)
        {
            using (var c = Db.Open())
                PlaniranjeDb.IzvrsiSaRowVer(c, null,
                    "UPDATE planiranje.OP_Trase SET AdHocStatus=N'ODBIJENA', Napomena=@n WHERE ID=@id AND RowVer=@rv",
                    "@n", (object)napomena, "@id", id, "@rv", rowVer);
        }

        private static string Skrati(string s, int n)
        {
            return s != null && s.Length > n ? s.Substring(0, n) : s;
        }

        private static string Hes(byte[] sadrzaj)
        {
            if (sadrzaj == null) return null;
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(sadrzaj).Select(b => b.ToString("x2")));
        }
    }
}
