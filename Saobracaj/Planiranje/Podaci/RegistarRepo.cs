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
    internal static class RegistarRepo
    {
        internal static DataTable Verzije()
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT ID, Naziv, OriginalniNaziv, Status, BrojRedova, Izvor, Uvezao, Uvezeno, Potvrdio, Potvrdjeno, Napomena " +
                    "FROM planiranje.OP_RegistarVerzija ORDER BY ID DESC");
        }

        internal static DataTable Pruge(int verzijaId)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT ID, RedniBroj, IdentBroj, Naziv, NazivCir, Kategorija, Elektrificirana, Aktivna, IzvorniID " +
                    "FROM planiranje.OP_Pruga WHERE RegistarVerzijaID = @v ORDER BY RedniBroj, Naziv",
                    "@v", verzijaId);
        }

        internal static DataTable VazecePruge()
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT p.ID, p.RedniBroj, p.IdentBroj, p.Naziv, p.Kategorija, p.Elektrificirana, p.IzvorniID " +
                    "FROM planiranje.OP_Pruga p JOIN planiranje.OP_RegistarVerzija v ON v.ID = p.RegistarVerzijaID " +
                    "WHERE v.Status = N'VAZECA' ORDER BY p.RedniBroj, p.Naziv");
        }

        internal static DataTable PrugeZaIzbor()
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT p.ID, p.Naziv FROM planiranje.OP_Pruga p " +
                    "JOIN planiranje.OP_RegistarVerzija v ON v.ID = p.RegistarVerzijaID " +
                    "WHERE v.Status = N'VAZECA' AND p.Aktivna = 1 ORDER BY p.Naziv");
        }

        internal static DataTable Mesta(int prugaId)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT m.ID, m.RB, m.Naziv, m.NazivOriginal, m.StanicaID, s.Naziv AS Stanica, m.UIC, m.VrstaMesta, m.StacionazaM, m.RastojanjeDoSledecegM, m.Kolosecnost, " +
                    "m.Klasa, m.Kategorija, m.VmaxDesni, m.VmaxLevi, m.MaxDuzinaVozaAB, m.MaxDuzinaVozaBA, m.NacinRegulisanja, m.NacinOsiguranja, " +
                    "m.NagibStanice, m.MerodavniUspon, m.MerodavniPad, m.TovarniProfil, m.NadmorskaVisina, m.PovezanoPo, m.Promena, m.OpisPromene, m.Aktivno " +
                    "FROM planiranje.OP_PrugaMesto m LEFT JOIN planiranje.OP_Stanice s ON s.ID = m.StanicaID " +
                    "WHERE m.PrugaID = @p ORDER BY m.RB",
                    "@p", prugaId);
        }

        internal static List<PovezivanjeRegistra.StanicaZaVezu> StaniceZaVezu()
        {
            var lista = new List<PovezivanjeRegistra.StanicaZaVezu>();
            using (var c = Db.Open())
                foreach (DataRow r in Db.Table(c, null,
                    "SELECT ID, Naziv, UIC, SifraIZS, RegKljuc, NazivRegistar, AltNazivi FROM planiranje.OP_Stanice WHERE (Aktivna = 1 OR RegKljuc IS NOT NULL) AND DuplikatOd IS NULL ORDER BY ID").Rows)
                    lista.Add(new PovezivanjeRegistra.StanicaZaVezu
                    {
                        ID = Convert.ToInt32(r["ID"]),
                        Naziv = Db.Text(r, "Naziv"),
                        UIC = r.IsNull("UIC") ? null : Db.Text(r, "UIC").Trim(),
                        SifraIZS = r.IsNull("SifraIZS") ? (int?)null : Convert.ToInt32(r["SifraIZS"]),
                        RegKljuc = r.IsNull("RegKljuc") ? null : Db.Text(r, "RegKljuc"),
                        NazivRegistar = r.IsNull("NazivRegistar") ? null : Db.Text(r, "NazivRegistar"),
                        AltNazivi = r.IsNull("AltNazivi") ? null : Db.Text(r, "AltNazivi")
                    });
            return lista;
        }

        // Stari brojevi pruga dolaze iz migracije dbo.Pruga, jer telegrami i kvalifikacije vode na njih.
        internal static List<PovezivanjeRegistra.PrugaZaVezu> StarePruge()
        {
            var lista = new List<PovezivanjeRegistra.PrugaZaVezu>();
            using (var c = Db.Open())
                foreach (DataRow r in Db.Table(c, null,
                    "SELECT p.IzvorniID, p.Naziv, p.IdentBroj, p.Elektrificirana FROM planiranje.OP_Pruga p " +
                    "JOIN planiranje.OP_RegistarVerzija v ON v.ID = p.RegistarVerzijaID " +
                    "WHERE p.IzvorniID IS NOT NULL AND v.Status = N'VAZECA'").Rows)
                    lista.Add(new PovezivanjeRegistra.PrugaZaVezu
                    {
                        IzvorniID = Convert.ToInt32(r["IzvorniID"]),
                        Naziv = Db.Text(r, "Naziv"),
                        IdentBroj = r.IsNull("IdentBroj") ? null : Db.Text(r, "IdentBroj"),
                        Elektrificirana = r.IsNull("Elektrificirana") ? (bool?)null : Convert.ToBoolean(r["Elektrificirana"])
                    });
            return lista;
        }

        internal static List<MestoZaPoredjenje> VazecaMesta()
        {
            var lista = new List<MestoZaPoredjenje>();
            using (var c = Db.Open())
                foreach (DataRow r in Db.Table(c, null,
                    "SELECT p.Kategorija, p.RedniBroj, m.Naziv, m.Aktivno, m.Klasa, m.VmaxDesni, m.VmaxLevi, m.MaxDuzinaVozaAB, m.MaxDuzinaVozaBA " +
                    "FROM planiranje.OP_PrugaMesto m JOIN planiranje.OP_Pruga p ON p.ID = m.PrugaID " +
                    "JOIN planiranje.OP_RegistarVerzija v ON v.ID = p.RegistarVerzijaID " +
                    "WHERE v.Status = N'VAZECA' AND p.RedniBroj IS NOT NULL ORDER BY p.ID, m.RB").Rows)
                    lista.Add(new MestoZaPoredjenje
                    {
                        Pruga = Db.Text(r, "Kategorija") + " " + r["RedniBroj"],
                        Naziv = Db.Text(r, "Naziv"),
                        Aktivno = Convert.ToBoolean(r["Aktivno"]),
                        Klasa = r.IsNull("Klasa") ? null : Db.Text(r, "Klasa"),
                        VmaxDesni = r.IsNull("VmaxDesni") ? (int?)null : Convert.ToInt32(r["VmaxDesni"]),
                        VmaxLevi = r.IsNull("VmaxLevi") ? (int?)null : Convert.ToInt32(r["VmaxLevi"]),
                        MaxDuzinaVozaAB = r.IsNull("MaxDuzinaVozaAB") ? (int?)null : Convert.ToInt32(r["MaxDuzinaVozaAB"]),
                        MaxDuzinaVozaBA = r.IsNull("MaxDuzinaVozaBA") ? (int?)null : Convert.ToInt32(r["MaxDuzinaVozaBA"])
                    });
            return lista;
        }

        // Potvrda uvoza: nova verzija postaje važeća, mesta bez stanice dobijaju novu stanicu,
        // a graf, kvalifikacije i tablica masa prelaze na pruge nove verzije.
        internal static int Potvrdi(string naziv, string originalniNaziv, byte[] sadrzaj, SadrzajRegistra registar)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OP_Registar");
                    DataTable pret = Db.Table(c, tx, "SELECT ID FROM planiranje.OP_RegistarVerzija WHERE Status=N'VAZECA'");
                    object prethodna = pret.Rows.Count == 0 ? null : pret.Rows[0]["ID"];
                    if (prethodna != null)
                        using (var cmd = Db.Command(c, tx, "UPDATE planiranje.OP_RegistarVerzija SET Status=N'ARHIVA' WHERE ID=@id", "@id", prethodna))
                            cmd.ExecuteNonQuery();

                    int verzija;
                    using (var cmd = Db.Command(c, tx,
                        "INSERT planiranje.OP_RegistarVerzija (Naziv, OriginalniNaziv, Sha256, Status, BrojRedova, Izvor, Uvezao, Potvrdio, Potvrdjeno) " +
                        "VALUES (@n, @o, @h, N'VAZECA', @b, N'EXCEL', @k, @k, SYSDATETIME()); SELECT CAST(SCOPE_IDENTITY() AS int);",
                        "@n", naziv, "@o", (object)originalniNaziv, "@h", (object)Hes(sadrzaj), "@b", registar.BrojRedova, "@k", PlaniranjeDb.Korisnik))
                        verzija = Convert.ToInt32(cmd.ExecuteScalar());
                    using (var cmd = Db.Command(c, tx, "UPDATE planiranje.OP_RegistarVerzija SET Sadrzaj=@s WHERE ID=@id", "@id", verzija))
                    {
                        var par = cmd.Parameters.Add("@s", SqlDbType.VarBinary, -1);
                        par.Value = (object)sadrzaj ?? DBNull.Value;
                        cmd.ExecuteNonQuery();
                    }

                    var noveStanice = new List<NovaStanicaUvoza>();
                    var obradjeniParovi = new HashSet<string>();
                    foreach (PrugaRegistra p in registar.Pruge)
                    {
                        int prugaId;
                        using (var cmd = Db.Command(c, tx,
                            "INSERT planiranje.OP_Pruga (RegistarVerzijaID, RedniBroj, IdentBroj, Naziv, NazivCir, Kategorija, Elektrificirana, IzvorniID, Sifra, KiM) " +
                            "VALUES (@v, @r, @i, @n, @c, @k, @e, @iz, @si, @ki); SELECT CAST(SCOPE_IDENTITY() AS int);",
                            "@si", (object)p.Sifra, "@ki", p.KiM, "@v", verzija, "@r", p.RedniBroj, "@i", (object)p.IdentBroj, "@n", Skrati(p.Naziv, 300), "@c", (object)Skrati(p.NazivCir, 300), "@k", (object)p.Kategorija, "@e", (object)p.Elektrificirana, "@iz", (object)p.IzvorniID))
                            prugaId = Convert.ToInt32(cmd.ExecuteScalar());

                        foreach (MestoRegistra m in p.Mesta)
                        {
                            if (!m.StanicaID.HasValue && !m.JeOznaka && !m.Inostranstvo)
                                m.StanicaID = NovaStanica(c, tx, m, noveStanice);
                            UpisiMesto(c, tx, prugaId, m);
                        }
                        UpisiVeze(c, tx, prugaId, p, obradjeniParovi);
                    }
                    UskladiStanice(c, tx, registar);

                    if (prethodna != null)
                    {
                        // Veza ide preko dbo.Pruga, jedinog broja zajedničkog za obe verzije.
                        foreach (string sql in new[]
                        {
                            // Veze koje uvoz ne pravi (dodate iz naziva pruge) prate prugu po šifri.
                            "UPDATE v SET PrugaID = n.ID FROM planiranje.OP_MrezaVeza v JOIN planiranje.OP_Pruga s ON s.ID = v.PrugaID AND s.RegistarVerzijaID = @pre JOIN planiranje.OP_Pruga n ON n.Sifra = s.Sifra AND n.RegistarVerzijaID = @nova",
                            "UPDATE k SET PrugaID = n.ID FROM planiranje.OP_OsobljePruga k JOIN planiranje.OP_Pruga s ON s.ID = k.PrugaID AND s.RegistarVerzijaID = @pre JOIN planiranje.OP_Pruga n ON n.IzvorniID = s.IzvorniID AND n.RegistarVerzijaID = @nova " +
                            "WHERE NOT EXISTS (SELECT 1 FROM planiranje.OP_OsobljePruga x WHERE x.DeSifra = k.DeSifra AND x.PrugaID = n.ID)",
                            "UPDATE t SET PrugaID = n.ID FROM planiranje.OP_TablicaMasa t JOIN planiranje.OP_Pruga s ON s.ID = t.PrugaID AND s.RegistarVerzijaID = @pre JOIN planiranje.OP_Pruga n ON n.IzvorniID = s.IzvorniID AND n.RegistarVerzijaID = @nova"
                        })
                            using (var cmd = Db.Command(c, tx, sql, "@pre", prethodna, "@nova", verzija))
                                cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                    return verzija;
                }
                catch { tx.Rollback(); throw; }
            }
        }

        private sealed class NovaStanicaUvoza
        {
            internal string Kljuc;
            internal string UIC;
            internal int ID;
        }

        // Isti naziv sa različitim UIC-om su dva mesta (npr. Vitkovac na dve pruge).
        private static int NovaStanica(SqlConnection c, SqlTransaction tx, MestoRegistra m, List<NovaStanicaUvoza> nove)
        {
            string kljuc = m.Kljuc ?? NaziviStanica.Normalizuj(m.Naziv);
            NovaStanicaUvoza ista = nove.FirstOrDefault(n => n.Kljuc == kljuc);
            if (ista != null) { m.PovezanoPo = "NAZIV"; return ista.ID; }
            int id;
            // Koordinate novo mesto dobija ručno: filter „Za proveru“ na ekranu Mreža i stanice.
            using (var cmd = Db.Command(c, tx,
                "INSERT planiranje.OP_Stanice (Naziv, NazivCir, NazivRegistar, RegKljuc, SifraIZS, Kod, UIC, Drzava, VrstaMesta, VrstaKod, Izvor, Kreirao, DatumAzuriranja) " +
                "VALUES (@n, @c, @r, @kl, @s, @kod, @u, N'SRB', @v, @vk, N'REGISTAR', @k, SYSDATETIME()); SELECT CAST(SCOPE_IDENTITY() AS int);",
                "@n", Skrati(NaziviRegistra.LatinicaLepo(m.NazivOriginal), 100), "@c", (object)Skrati(NaziviRegistra.CirilicaLepo(m.NazivOriginal), 100), "@r", (object)Skrati(m.NazivOriginal, 120), "@kl", (object)Skrati(m.Kljuc, 80),
                "@s", (object)m.SifraIZS, "@kod", (object)(m.SifraIZS.HasValue ? NaziviRegistra.Kod(m.SifraIZS.Value) : null), "@u", (object)(m.SifraIZS.HasValue ? NaziviRegistra.UIC(m.SifraIZS.Value) : null),
                "@v", (object)Skrati(m.VrstaMesta, 40), "@vk", (object)m.VrstaKod, "@k", PlaniranjeDb.Korisnik))
                id = Convert.ToInt32(cmd.ExecuteScalar());
            nove.Add(new NovaStanicaUvoza { Kljuc = kljuc, UIC = m.UIC, ID = id });
            m.PovezanoPo = "NOVO";
            return id;
        }

        private static void UpisiMesto(SqlConnection c, SqlTransaction tx, int prugaId, MestoRegistra m)
        {
            using (var cmd = Db.Command(c, tx,
                "INSERT planiranje.OP_PrugaMesto (PrugaID, RB, StanicaID, NazivOriginal, Naziv, UIC, VrstaMesta, StacionazaKM, StacionazaM, RastojanjeDoSledecegM, Kolosecnost, Klasa, Kategorija, VmaxDesni, VmaxLevi, " +
                "MaxDuzinaVozaAB, KoloseciAB, MaxDuzinaVozaBA, KoloseciBA, NacinRegulisanja, NacinOsiguranja, Vaga, Rampa, Posednutost, OtvorenoZa, MinPoluprecnikM, NagibStanice, MerodavniUspon, MerodavniPad, " +
                "MerodavniOtporUspon, MerodavniOtporPad, TovarniProfil, NadmorskaVisina, Aktivno, Promena, OpisPromene, PovezanoPo, RegKljuc, VrstaKod) VALUES " +
                "(@p, @rb, @s, @no, @n, @u, @v, @km, @m, @ra, @ko, @kl, @ka, @vd, @vl, @ab, @kab, @ba, @kba, @nr, @no2, @va, @ram, @po, @ot, @mp, @ns, @mu, @mpa, @ou, @op, @tp, @nv, @ak, @pr, @opi, @pp, @rk, @vk)",
                "@rk", (object)Skrati(m.Kljuc, 80), "@vk", (object)m.VrstaKod, "@p", prugaId, "@rb", m.RB, "@s", (object)m.StanicaID, "@no", (object)m.NazivOriginal, "@n", m.Naziv, "@u", (object)m.UIC, "@v", (object)Skrati(m.VrstaMesta, 40),
                "@km", m.StacionazaM.HasValue ? (object)(m.StacionazaM.Value / 1000m) : null, "@m", (object)m.StacionazaM, "@ra", (object)m.RastojanjeDoSledecegM, "@ko", (object)m.Kolosecnost, "@kl", (object)m.Klasa, "@ka", (object)m.Kategorija,
                "@vd", (object)m.VmaxDesni, "@vl", (object)m.VmaxLevi, "@ab", (object)m.MaxDuzinaVozaAB, "@kab", (object)m.KoloseciAB, "@ba", (object)m.MaxDuzinaVozaBA, "@kba", (object)m.KoloseciBA,
                "@nr", (object)m.NacinRegulisanja, "@no2", (object)m.NacinOsiguranja, "@va", (object)m.Vaga, "@ram", (object)m.Rampa, "@po", (object)m.Posednutost, "@ot", (object)m.OtvorenoZa,
                "@mp", (object)m.MinPoluprecnikM, "@ns", (object)m.NagibStanice, "@mu", (object)m.MerodavniUspon, "@mpa", (object)m.MerodavniPad, "@ou", (object)m.MerodavniOtporUspon, "@op", (object)m.MerodavniOtporPad,
                "@tp", (object)m.TovarniProfil, "@nv", (object)m.NadmorskaVisina, "@ak", m.Aktivno, "@pr", (object)m.Promena, "@opi", (object)m.OpisPromene, "@pp", (object)Skrati(m.PovezanoPo, 10)))
                cmd.ExecuteNonQuery();
        }

        // Susedna mesta sa stanicom čine vezu grafa; dužina je iz stacionaže. Zatvoren deo prekida prugu.
        // Postojećoj vezi ostaju trasa i elektrifikacija iz OSM-a, a dužina izračunata iz trase se ne menja.
        private static void UpisiVeze(SqlConnection c, SqlTransaction tx, int prugaId, PrugaRegistra p, HashSet<string> obradjeni)
        {
            MestoRegistra prethodno = null;
            int? metara = null;
            string klasa = null;
            int? vmax = null;
            byte? kolosecnost = null;
            foreach (MestoRegistra m in p.Mesta)
            {
                if (!m.Aktivno || m.Inostranstvo) { prethodno = null; metara = null; continue; }
                if (!m.StanicaID.HasValue) { metara += m.RastojanjeDoSledecegM; continue; }
                if (prethodno != null && prethodno.StanicaID != m.StanicaID)
                {
                    decimal km = metara.HasValue ? metara.Value / 1000m : 0;
                    int a = prethodno.StanicaID.Value, b = m.StanicaID.Value;
                    // Deonica koja je na dve pruge ostaje na prvoj, a to je pruga višeg ranga.
                    if (obradjeni.Add(Math.Min(a, b) + "|" + Math.Max(a, b)))
                        using (var cmd = Db.Command(c, tx,
                            "IF EXISTS (SELECT 1 FROM planiranje.OP_MrezaVeza WHERE (StanicaAID=@a AND StanicaBID=@b) OR (StanicaAID=@b AND StanicaBID=@a)) " +
                            "UPDATE planiranje.OP_MrezaVeza SET PrugaID=@p, Klasa=@kl, OsovinskoT=@os, Vmax=@vm, Dvokolosecna=@dv, MerodavniUspon=@mu, MerodavniPad=@mp, KiM=@ki, " +
                            "DuzinaKm = CASE WHEN @km > 0 AND ISNULL(KmIzvor, N'') <> N'GEOMETRIJA' THEN @km ELSE DuzinaKm END, " +
                            "KmIzvor = CASE WHEN @km > 0 AND KmIzvor = N'NEPOZNATO' THEN N'REGISTAR' ELSE KmIzvor END " +
                            "WHERE (StanicaAID=@a AND StanicaBID=@b) OR (StanicaAID=@b AND StanicaBID=@a) " +
                            "ELSE IF @km > 0 INSERT planiranje.OP_MrezaVeza (StanicaAID, StanicaBID, DuzinaKm, PrugaID, Elektrificirana, Izvor, KmIzvor, Klasa, OsovinskoT, Vmax, Dvokolosecna, MerodavniUspon, MerodavniPad, KiM) " +
                            "VALUES (@a, @b, @km, @p, @e, N'REGISTAR', N'REGISTAR', @kl, @os, @vm, @dv, @mu, @mp, @ki)",
                            "@a", a, "@b", b, "@p", prugaId, "@e", (object)p.Elektrificirana, "@km", km, "@kl", (object)klasa, "@os", (object)Rutiranje.OsovinskoIzKlase(klasa), "@vm", (object)vmax,
                            "@dv", kolosecnost.HasValue ? (object)(kolosecnost.Value == 2) : null, "@mu", (object)prethodno.MerodavniUspon, "@mp", (object)prethodno.MerodavniPad, "@ki", prethodno.KiM || m.KiM))
                            cmd.ExecuteNonQuery();
                }
                // U Registru su klasa, brzina i broj koloseka upisani samo kad se promene.
                klasa = m.Klasa ?? klasa;
                vmax = m.VmaxDesni ?? vmax;
                kolosecnost = m.Kolosecnost ?? kolosecnost;
                prethodno = m;
                metara = m.RastojanjeDoSledecegM;
            }
        }

        // Podaci iz Registra na stanici: prva pojava mesta daje vrednosti, a pruge se sabiraju. Koordinate se ne menjaju.
        private static void UskladiStanice(SqlConnection c, SqlTransaction tx, SadrzajRegistra registar)
        {
            var mesta = registar.Pruge.SelectMany(p => p.Mesta.Where(m => m.StanicaID.HasValue && !m.Inostranstvo).Select(m => new { Pruga = p.Sifra, Mesto = m }))
                .GroupBy(x => x.Mesto.StanicaID.Value);
            foreach (var g in mesta)
            {
                // Stanica ili ukrsnica ima prednost: tu su dužine voza i koloseci.
                MestoRegistra m = g.Select(x => x.Mesto).OrderBy(x => x.VrstaKod == 1 || x.VrstaKod == 2 ? 0 : 1).First();
                string pruge = string.Join(", ", g.Select(x => x.Pruga).Where(s => s != null).Distinct());
                string nazivRegistar = Skrati(m.NazivOriginal, 120);
                using (var cmd = Db.Command(c, tx,
                    "UPDATE planiranje.OP_Stanice SET " +
                    "NazivStari = CASE WHEN NazivRegistar IS NOT NULL AND NazivRegistar <> @r AND ISNULL(AltNazivi, N'') NOT LIKE N'%' + @r + N'%' THEN Naziv ELSE NazivStari END, " +
                    "Naziv = CASE WHEN NazivRegistar IS NOT NULL AND NazivRegistar <> @r AND ISNULL(AltNazivi, N'') NOT LIKE N'%' + @r + N'%' THEN @n ELSE Naziv END, " +
                    "NazivCir = CASE WHEN NazivRegistar IS NOT NULL AND NazivRegistar <> @r AND ISNULL(AltNazivi, N'') NOT LIKE N'%' + @r + N'%' THEN @c ELSE NazivCir END, " +
                    "NazivRegistar = CASE WHEN ISNULL(AltNazivi, N'') LIKE N'%' + @r + N'%' THEN NazivRegistar ELSE @r END, " +
                    "RegKljuc = ISNULL(RegKljuc, @kl), SifraIZS = ISNULL(@s, SifraIZS), Kod = ISNULL(@kod, Kod), UIC = ISNULL(@u, UIC), Drzava = N'SRB', " +
                    "VrstaMesta = @v, VrstaKod = @vk, Granicna = @g, GranicniPrelaz = @gp, AdministrativnaLinija = @al, KiM = @ki, Pruge = @pr, Posednutost = @po, OtvorenaZa = @ot, Vaga = @va, Rampa = @ra, " +
                    "MaxDuzinaVozaAB = @ab, KolosekAB = @kab, MaxDuzinaVozaBA = @ba, KolosekBA = @kba, Regulisanje = @re, Osiguranje = @os, NagibStanice = @ns, MinRadijus = @mr, NadmorskaVisina = @nv, " +
                    "Izvor = N'REGISTAR', DatumAzuriranja = SYSDATETIME() WHERE ID = @id",
                    "@id", g.Key, "@r", (object)nazivRegistar, "@n", Skrati(NaziviRegistra.LatinicaLepo(m.NazivOriginal), 100), "@c", (object)Skrati(NaziviRegistra.CirilicaLepo(m.NazivOriginal), 100),
                    "@kl", (object)Skrati(m.Kljuc, 80), "@s", (object)m.SifraIZS, "@kod", (object)(m.SifraIZS.HasValue ? NaziviRegistra.Kod(m.SifraIZS.Value) : null), "@u", (object)(m.SifraIZS.HasValue ? NaziviRegistra.UIC(m.SifraIZS.Value) : null),
                    "@v", (object)Skrati(m.VrstaMesta, 40), "@vk", (object)m.VrstaKod, "@g", g.Any(x => x.Mesto.Granicna), "@gp", m.VrstaKod == 15, "@al", m.NazivOriginal.Contains("АДМИНИСТРАТИВНА"), "@ki", g.Any(x => x.Mesto.KiM),
                    "@pr", (object)Skrati(pruge, 200), "@po", (object)Skrati(m.Posednutost, 10), "@ot", (object)Skrati(m.OtvorenoZa, 10), "@va", (object)Skrati(m.Vaga, 10), "@ra", (object)Skrati(m.Rampa, 10),
                    "@ab", (object)m.MaxDuzinaVozaAB, "@kab", (object)m.KoloseciAB, "@ba", (object)m.MaxDuzinaVozaBA, "@kba", (object)m.KoloseciBA, "@re", (object)Skrati(m.NacinRegulisanja, 40), "@os", (object)Skrati(m.NacinOsiguranja, 10),
                    "@ns", (object)m.NagibStanice, "@mr", (object)m.MinPoluprecnikM, "@nv", (object)m.NadmorskaVisina))
                    cmd.ExecuteNonQuery();
            }
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
