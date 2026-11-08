using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Saobracaj.Agenti_jobs;
using Saobracaj.Planiranje.Modeli;

namespace Saobracaj.Planiranje.Podaci
{
    internal sealed class SifarniciJobs
    {
        internal DataTable Partneri;    // platilac, organizator, predaje operateru
        internal DataTable Prevoznici;  // preuzima od
        internal DataTable Stanice;
        internal DataTable TipoviPrevoza;
        internal DataTable Statusi;
        internal DataTable NHM;
        internal DataTable SerijeKola;  // dbo.VagoniSerije
    }

    // JOBS (OP_030). Šifarnici iz dbo se samo čitaju; upis je samo u planiranje.OP_Jobs, OP_JobsLog i OP_Posao.
    // Svaki pravac posla je jedan OP_Posao (RBDeonice = pravac), pa plan, Taskovi i Ponuda osoblja rade sa JOBS-om.
    internal static class JobsRepo
    {
        internal const int StatusPredato = 7;

        // Isti izvori kao na formi Najava.
        internal static SifarniciJobs Sifarnici()
        {
            using (var c = Db.Open())
                return new SifarniciJobs
                {
                    Partneri = Db.Table(c, null, "SELECT PaSifra AS ID, RTRIM(PaNaziv) AS Naziv FROM dbo.Partnerji WHERE Primalac = 1 ORDER BY PaNaziv"),
                    Prevoznici = Db.Table(c, null, "SELECT PaSifra AS ID, RTRIM(PaNaziv) AS Naziv FROM dbo.Partnerji WHERE Prevoznik = 1 ORDER BY PaNaziv"),
                    Stanice = Db.Table(c, null, "SELECT ID, RTRIM(Opis) AS Naziv FROM dbo.stanice ORDER BY Opis"),
                    TipoviPrevoza = Db.Table(c, null, "SELECT DISTINCT ID, RTRIM(Naziv) AS Naziv FROM dbo.TipSaobPrevoza ORDER BY 2"),
                    Statusi = Db.Table(c, null, "SELECT DISTINCT ID, RTRIM(Opis) AS Naziv FROM dbo.StatusVoza ORDER BY ID"),
                    NHM = Db.Table(c, null, "SELECT ID, RTRIM(Broj) AS Naziv FROM dbo.NHM ORDER BY Broj"),
                    // Kao combo serije vagona na formi Najava: oznaka serije je i šifra i naziv.
                    SerijeKola = Db.Table(c, null, "IF OBJECT_ID(N'dbo.VagoniSerije', N'U') IS NOT NULL SELECT DISTINCT RTRIM(Serija) AS ID, RTRIM(Serija) AS Naziv FROM dbo.VagoniSerije WHERE Serija IS NOT NULL ORDER BY 1 " +
                        "ELSE SELECT CAST(NULL AS nvarchar(20)) AS ID, CAST(NULL AS nvarchar(20)) AS Naziv WHERE 1 = 0")
                };
        }

        // Lista: bez predatih poslova, osim kad je izabrano „Svi JOBS“; po želji samo jedan platilac.
        internal static DataTable Lista(bool svi, int? platilac)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT j.ID AS Sifra, RTRIM(pl.PaNaziv) AS Platilac, RTRIM(s1.Opis) AS [Stanica 1], RTRIM(s2.Opis) AS [Stanica 2], RTRIM(s3.Opis) AS [Stanica 3], " +
                    "j.ETD, (SELECT COUNT(DISTINCT tn.TaskID) FROM planiranje.OP_TaskNajava tn JOIN planiranje.OP_Task t ON t.ID = tn.TaskID " +
                    "WHERE tn.NajavaID = j.ID AND t.Status IN (N'OTVOREN', N'LANSIRAN')) AS [Otvorenih taskova], " +
                    "RTRIM(st.Opis) AS Status, j.IzvornaNajavaID AS [Iz najave] " +
                    "FROM planiranje.OP_Jobs j " +
                    "LEFT JOIN dbo.Partnerji pl ON pl.PaSifra = j.PlatilacID " +
                    "LEFT JOIN dbo.stanice s1 ON s1.ID = j.Stanica1ID LEFT JOIN dbo.stanice s2 ON s2.ID = j.Stanica2ID LEFT JOIN dbo.stanice s3 ON s3.ID = j.Stanica3ID " +
                    "LEFT JOIN dbo.StatusVoza st ON st.ID = j.Status " +
                    "WHERE (@svi = 1 OR j.Status <> @predato) AND (@pl IS NULL OR j.PlatilacID = @pl) ORDER BY j.ID DESC",
                    "@svi", svi ? 1 : 0, "@predato", StatusPredato, "@pl", (object)platilac);
        }

        internal static DataRow Jedan(int id)
        {
            using (var c = Db.Open())
            {
                DataTable t = Db.Table(c, null, "SELECT * FROM planiranje.OP_Jobs WHERE ID = @id", "@id", id);
                return t.Rows.Count == 0 ? null : t.Rows[0];
            }
        }

        private static JobOP IzReda(DataRow r)
        {
            Func<string, int?> ceo = k => r.IsNull(k) ? (int?)null : Convert.ToInt32(r[k]);
            Func<string, decimal?> dec = k => r.IsNull(k) ? (decimal?)null : Convert.ToDecimal(r[k]);
            Func<string, string> txt = k => r.IsNull(k) ? null : Convert.ToString(r[k]);
            Func<int, RobaPravca> roba = i => new RobaPravca
            {
                NHM = ceo("NHM" + i), Bruto = dec("Bruto" + i), BrojKola = ceo("BrojKola" + i), SerijaKola = txt("SerijaKola" + i), Duzina = dec("Duzina" + i),
                Neto = dec("Neto" + i), RIDBroj = txt("RIDBroj" + i), OMBroj = txt("OMBroj" + i)
            };
            return new JobOP
            {
                ID = Convert.ToInt32(r["ID"]), PlatilacID = ceo("PlatilacID"), OrganizatorID = ceo("OrganizatorID"), TipPrevozaID = ceo("TipPrevozaID"),
                OtpravnaID = ceo("OtpravnaID"), UputnaID = ceo("UputnaID"), Status = Convert.ToInt32(r["Status"]), Napomena = txt("Napomena"),
                PreuzimaOdID = ceo("PreuzimaOdID"), PredajeOperateruID = ceo("PredajeOperateruID"),
                Stanica1ID = Convert.ToInt32(r["Stanica1ID"]), Stanica2ID = ceo("Stanica2ID"), Stanica3ID = Convert.ToInt32(r["Stanica3ID"]),
                ETD = r.IsNull("ETD") ? (DateTime?)null : Convert.ToDateTime(r["ETD"]),
                Roba1 = roba(1), Roba2 = roba(2), RowVer = (byte[])r["RowVer"]
            };
        }

        private static object[] Parametri(JobOP j)
        {
            Func<object, object> v = x => x ?? DBNull.Value;
            RobaPravca r2 = j.Stanica2ID.HasValue ? j.Roba2 : new RobaPravca();
            return new object[]
            {
                "@pl", v(j.PlatilacID), "@or", v(j.OrganizatorID), "@tp", v(j.TipPrevozaID), "@ot", v(j.OtpravnaID), "@up", v(j.UputnaID),
                "@st", j.Status, "@na", v(j.Napomena), "@po", v(j.PreuzimaOdID), "@pr", v(j.PredajeOperateruID),
                "@s1", j.Stanica1ID, "@s2", v(j.Stanica2ID), "@s3", j.Stanica3ID, "@etd", v(j.ETD),
                "@n1", v(j.Roba1.NHM), "@b1", v(j.Roba1.Bruto), "@k1", v(j.Roba1.BrojKola), "@d1", v(j.Roba1.Duzina), "@e1", v(j.Roba1.Neto), "@r1", v(j.Roba1.RIDBroj), "@o1", v(j.Roba1.OMBroj), "@sk1", v(j.Roba1.SerijaKola),
                "@n2", v(r2.NHM), "@b2", v(r2.Bruto), "@k2", v(r2.BrojKola), "@d2", v(r2.Duzina), "@e2", v(r2.Neto), "@r2", v(r2.RIDBroj), "@o2", v(r2.OMBroj), "@sk2", v(r2.SerijaKola),
                "@kor", PlaniranjeDb.Korisnik
            };
        }

        private const string Kolone =
            "PlatilacID, OrganizatorID, TipPrevozaID, OtpravnaID, UputnaID, Status, Napomena, PreuzimaOdID, PredajeOperateruID, Stanica1ID, Stanica2ID, Stanica3ID, ETD, " +
            "NHM1, Bruto1, BrojKola1, Duzina1, Neto1, RIDBroj1, OMBroj1, NHM2, Bruto2, BrojKola2, Duzina2, Neto2, RIDBroj2, OMBroj2, SerijaKola1, SerijaKola2";
        private const string Vrednosti =
            "@pl, @or, @tp, @ot, @up, @st, @na, @po, @pr, @s1, @s2, @s3, @etd, @n1, @b1, @k1, @d1, @e1, @r1, @o1, @n2, @b2, @k2, @d2, @e2, @r2, @o2, @sk1, @sk2";

        // Nov ili izmenjen posao, LOG i poslovi po pravcu u istoj transakciji. Vraća šifru i napomene o pravcima koji se nisu mogli uskladiti.
        internal static int Sacuvaj(JobOP j, List<string> napomene)
        {
            if (j.Stanica1ID <= 0 || j.Stanica3ID <= 0) throw new InvalidOperationException("Unesite stanicu preuzimanja (1) i stanicu predaje (3).");
            if (!j.Stanica2ID.HasValue && j.Stanica1ID == j.Stanica3ID) throw new InvalidOperationException("Bez stanice istovara (2) stanica preuzimanja i stanica predaje moraju biti različite.");
            if (j.Stanica2ID.HasValue && (j.Stanica2ID == j.Stanica1ID || j.Stanica2ID == j.Stanica3ID))
                throw new InvalidOperationException("Stanica istovara (2) mora biti različita od stanice preuzimanja i stanice predaje.");
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    int id;
                    bool relacija = true;
                    if (j.ID == 0)
                    {
                        using (var cmd = Db.Command(c, tx, "INSERT planiranje.OP_Jobs (" + Kolone + ", Kreirao) VALUES (" + Vrednosti + ", @kor); SELECT CAST(SCOPE_IDENTITY() AS int);", Parametri(j)))
                            id = Convert.ToInt32(cmd.ExecuteScalar());
                        Log(c, tx, id, "NOV", "Posao je napravljen: " + j.BrojPravaca + (j.BrojPravaca == 1 ? " pravac." : " pravca."));
                    }
                    else
                    {
                        id = j.ID;
                        DataTable staro = Db.Table(c, tx, "SELECT * FROM planiranje.OP_Jobs WHERE ID = @id", "@id", id);
                        if (staro.Rows.Count == 0) throw new InvalidOperationException("Posao " + id + " više ne postoji.");
                        Dictionary<string, string> pre = IzReda(staro.Rows[0]).ZaLog(), posle = j.ZaLog();
                        string skup = "UPDATE planiranje.OP_Jobs SET " + string.Join(", ", Kolone.Split(',').Select((k, i) => k.Trim() + " = " + Vrednosti.Split(',')[i].Trim())) +
                            ", Izmenio = @kor, Izmenjeno = SYSDATETIME() WHERE ID = @id AND RowVer = @rv";
                        PlaniranjeDb.IzvrsiSaRowVer(c, tx, skup, Parametri(j).Concat(new object[] { "@id", id, "@rv", j.RowVer }).ToArray());
                        List<string> promene = pre.Where(p => posle[p.Key] != p.Value).Select(p => p.Key + ": " + p.Value + " → " + posle[p.Key]).ToList();
                        relacija = new[] { "Stanica 1", "Stanica 2", "Stanica 3", "ETD" }.Any(k => pre[k] != posle[k]);
                        if (promene.Count > 0) Log(c, tx, id, "IZMENA", string.Join("; ", promene));
                    }
                    j.ID = id;
                    UskladiPoslove(c, tx, j, napomene, relacija);
                    tx.Commit();
                    return id;
                }
                catch { tx.Rollback(); throw; }
            }
        }

        private static void Log(SqlConnection c, SqlTransaction tx, int id, string akcija, string opis)
        {
            using (var cmd = Db.Command(c, tx, "INSERT planiranje.OP_JobsLog (JobID, Korisnik, Akcija, Opis) VALUES (@j, @k, @a, @o)",
                "@j", id, "@k", PlaniranjeDb.Korisnik, "@a", akcija, "@o", (object)opis))
                cmd.ExecuteNonQuery();
        }

        // Svaki pravac je jedan posao (RBDeonice = pravac). Posao bez aktivnog taska i lokomotiva dobija nove stanice i ETD;
        // posao koji je već u radu se ne menja, a o tome se vraća napomena.
        private static void UskladiPoslove(SqlConnection c, SqlTransaction tx, JobOP j, List<string> napomene, bool relacijaPromenjena = true)
        {
            Db.Lock(c, tx, "OP_Job_" + j.ID);
            for (int pravac = 1; pravac <= 2; pravac++)
            {
                DataTable postoji = Db.Table(c, tx,
                    "SELECT p.ID, p.RowVer, " +
                    "(SELECT COUNT(*) FROM planiranje.OP_TaskNajava tn JOIN planiranje.OP_Task t ON t.ID = tn.TaskID WHERE tn.PosaoID = p.ID AND t.Status <> N'STORNIRAN') AS Taskova, " +
                    "(SELECT COUNT(*) FROM planiranje.OP_PosaoLokomotiva l WHERE l.PosaoID = p.ID) AS Lokomotiva " +
                    "FROM planiranje.OP_Posao p WHERE p.NajavaID = @j AND p.RBDeonice = @r AND p.Status <> N'OTKAZAN'",
                    "@j", j.ID, "@r", pravac);
                bool trebaPravac = pravac <= j.BrojPravaca;
                bool uRadu = postoji.Rows.Count > 0 && (Convert.ToInt32(postoji.Rows[0]["Taskova"]) > 0 || Convert.ToInt32(postoji.Rows[0]["Lokomotiva"]) > 0);
                if (!trebaPravac)
                {
                    if (postoji.Rows.Count == 0) continue;
                    if (uRadu) { napomene.Add("Pravac 2 više ne postoji, ali njegov posao ima taskove ili lokomotive, pa nije otkazan."); continue; }
                    int stari = Convert.ToInt32(postoji.Rows[0]["ID"]);
                    using (var cmd = Db.Command(c, tx, "UPDATE planiranje.OP_Posao SET Status = N'OTKAZAN', Izmenio = @k, Izmenjeno = SYSDATETIME() WHERE ID = @id", "@k", PlaniranjeDb.Korisnik, "@id", stari))
                        cmd.ExecuteNonQuery();
                    DogadjajRepo.Upisi(c, tx, stari, "JOB_PRAVAC", "Pravac 2 je uklonjen sa posla " + j.ID + ", posao je otkazan.");
                    continue;
                }
                const string stanica = "(SELECT TOP 1 COALESCE(DuplikatOd, ID) FROM planiranje.OP_Stanice WHERE IzvorniID = @s ORDER BY ID)";
                if (postoji.Rows.Count == 0)
                {
                    int novi;
                    using (var cmd = Db.Command(c, tx,
                        "INSERT planiranje.OP_Posao (NajavaID, RBDeonice, PrijemStanicaID, PredajaStanicaID, PlaniraniPrijem, Faza, Kreirao) " +
                        "SELECT @j, @r, " + stanica + ", " + stanica.Replace("@s", "@d") + ", @etd, 1, @k; SELECT CAST(SCOPE_IDENTITY() AS int);",
                        "@j", j.ID, "@r", pravac, "@s", j.OdPravca(pravac), "@d", j.DoPravca(pravac), "@etd", (object)j.ETD ?? DBNull.Value, "@k", PlaniranjeDb.Korisnik))
                        novi = Convert.ToInt32(cmd.ExecuteScalar());
                    DogadjajRepo.Upisi(c, tx, novi, "NAJAVA", "Posao " + j.ID + ", pravac " + pravac + " je ušao u plan.");
                    continue;
                }
                if (uRadu) { if (relacijaPromenjena) napomene.Add("Pravac " + pravac + " ima taskove ili lokomotive, pa stanice i ETD njegovog posla nisu menjani."); continue; }
                using (var cmd = Db.Command(c, tx,
                    "UPDATE planiranje.OP_Posao SET PrijemStanicaID = " + stanica + ", PredajaStanicaID = " + stanica.Replace("@s", "@d") + ", PlaniraniPrijem = @etd, " +
                    "Izmenio = @k, Izmenjeno = SYSDATETIME() WHERE ID = @id",
                    "@s", j.OdPravca(pravac), "@d", j.DoPravca(pravac), "@etd", (object)j.ETD ?? DBNull.Value, "@k", PlaniranjeDb.Korisnik, "@id", Convert.ToInt32(postoji.Rows[0]["ID"])))
                    cmd.ExecuteNonQuery();
            }
        }

        // Pravac 2 kreće kad pravac 1 završi: posao drugog pravca dobija prijem posle taska prvog pravca.
        internal static void PomeriPrijem(int posaoId, DateTime prijem)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Command(c, null, "UPDATE planiranje.OP_Posao SET PlaniraniPrijem = @p, Izmenio = @k, Izmenjeno = SYSDATETIME() WHERE ID = @id",
                "@p", prijem, "@k", PlaniranjeDb.Korisnik, "@id", posaoId))
                cmd.ExecuteNonQuery();
        }

        // Poslovi (pravci) JOBS-a, da bi se za svaki napravio task.
        internal static DataTable Pravci(int jobId)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT p.ID, p.RBDeonice, p.PlaniraniPrijem, " +
                    "(SELECT COUNT(*) FROM planiranje.OP_TaskNajava tn JOIN planiranje.OP_Task t ON t.ID = tn.TaskID WHERE tn.PosaoID = p.ID AND t.Status <> N'STORNIRAN') AS Taskova " +
                    "FROM planiranje.OP_Posao p WHERE p.NajavaID = @j AND p.Status <> N'OTKAZAN' ORDER BY p.RBDeonice",
                    "@j", jobId);
        }

        // Brisanje samo dok posao nema taskova i lokomotiva; poslovi pravaca se otkazuju.
        internal static void Obrisi(int id, byte[] rowVer)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OP_Job_" + id);
                    using (var cmd = Db.Command(c, tx,
                        "SELECT (SELECT COUNT(*) FROM planiranje.OP_TaskNajava tn JOIN planiranje.OP_Task t ON t.ID = tn.TaskID WHERE tn.NajavaID = @j AND t.Status <> N'STORNIRAN') + " +
                        "(SELECT COUNT(*) FROM planiranje.OP_PosaoLokomotiva l JOIN planiranje.OP_Posao p ON p.ID = l.PosaoID WHERE p.NajavaID = @j AND p.Status <> N'OTKAZAN')", "@j", id))
                        if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                            throw new InvalidOperationException("Posao " + id + " ima taskove ili lokomotive. Prvo stornirajte taskove.");
                    DataTable poslovi = Db.Table(c, tx, "SELECT ID FROM planiranje.OP_Posao WHERE NajavaID = @j AND Status <> N'OTKAZAN'", "@j", id);
                    foreach (DataRow p in poslovi.Rows)
                    {
                        using (var cmd = Db.Command(c, tx, "UPDATE planiranje.OP_Posao SET Status = N'OTKAZAN', Izmenio = @k, Izmenjeno = SYSDATETIME() WHERE ID = @id", "@k", PlaniranjeDb.Korisnik, "@id", p["ID"]))
                            cmd.ExecuteNonQuery();
                        DogadjajRepo.Upisi(c, tx, Convert.ToInt32(p["ID"]), "JOB_BRISANJE", "Posao " + id + " je obrisan.");
                    }
                    PlaniranjeDb.IzvrsiSaRowVer(c, tx, "DELETE planiranje.OP_Jobs WHERE ID = @id AND RowVer = @rv", "@id", id, "@rv", rowVer);
                    Log(c, tx, id, "BRISANJE", "Posao je obrisan.");
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        // Nazivi stanica planiranja (stanice taskova) i redni broj pravca svakog posla JOBS-a.
        internal static Dictionary<int, string> NaziviStanica()
        {
            using (var c = Db.Open())
                return Db.Table(c, null, "SELECT ID, Naziv FROM planiranje.OP_Stanice").Rows.Cast<DataRow>()
                    .ToDictionary(r => Convert.ToInt32(r["ID"]), r => Convert.ToString(r["Naziv"]));
        }

        internal static DataTable LogZa(int id)
        {
            using (var c = Db.Open())
                return Db.Table(c, null, "SELECT Vreme, Korisnik, Akcija, Opis FROM planiranje.OP_JobsLog WHERE JobID = @j ORDER BY Vreme DESC, ID DESC", "@j", id);
        }

        // Privremeno: najave unete ili predviđene od datuma prelaze u JOBS sa istim brojem (jedan pravac: merodavna otpravna → uputna,
        // kao u ranijem pogledu najave). Prenosi se samo najava koja još nije preneta i ima dve različite stanice. Vraća broj prenetih i preskočenih.
        internal static int[] PrenesiNajave(DateTime od)
        {
            const string merodavne =
                "SELECT n.ID, " +
                "CASE WHEN m1.Drzava = N'SRB' OR s1.Drzava IN (N'SRB', N'RS') OR NULLIF(n.OtpravnaGr, 0) IS NULL OR RTRIM(g1.Opis) = N'Nema' THEN n.Otpravna ELSE n.OtpravnaGr END AS S1, " +
                "CASE WHEN m2.Drzava = N'SRB' OR s2.Drzava IN (N'SRB', N'RS') OR NULLIF(n.Uputnagr, 0) IS NULL OR RTRIM(g2.Opis) = N'Nema' THEN n.Uputna ELSE n.Uputnagr END AS S3 " +
                "FROM dbo.Najava n " +
                "LEFT JOIN dbo.stanice s1 ON s1.ID = n.Otpravna LEFT JOIN dbo.stanice s2 ON s2.ID = n.Uputna " +
                "LEFT JOIN dbo.stanice g1 ON g1.ID = n.OtpravnaGr LEFT JOIN dbo.stanice g2 ON g2.ID = n.Uputnagr " +
                "LEFT JOIN planiranje.OP_Stanice o1 ON o1.IzvorniID = n.Otpravna LEFT JOIN planiranje.OP_Stanice m1 ON m1.ID = COALESCE(o1.DuplikatOd, o1.ID) " +
                "LEFT JOIN planiranje.OP_Stanice o2 ON o2.IzvorniID = n.Uputna LEFT JOIN planiranje.OP_Stanice m2 ON m2.ID = COALESCE(o2.DuplikatOd, o2.ID) " +
                "WHERE (n.PredvidjenoPrimanje >= @od OR n.DatumUnosa >= @od) " +
                "AND NOT EXISTS (SELECT 1 FROM planiranje.OP_Jobs j WHERE j.ID = n.ID OR j.IzvornaNajavaID = n.ID)";
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OP_JobsPrenos");
                    DataTable kandidati = Db.Table(c, tx, merodavne, "@od", od);
                    List<int> preneti = kandidati.Rows.Cast<DataRow>()
                        .Where(r => !r.IsNull("S1") && !r.IsNull("S3") && Convert.ToInt32(r["S1"]) > 0 && Convert.ToInt32(r["S3"]) > 0 && Convert.ToInt32(r["S1"]) != Convert.ToInt32(r["S3"]))
                        .Select(r => Convert.ToInt32(r["ID"])).ToList();
                    int preskoceno = kandidati.Rows.Count - preneti.Count;
                    foreach (int najava in preneti)
                    {
                        DataRow s = kandidati.Rows.Cast<DataRow>().First(r => Convert.ToInt32(r["ID"]) == najava);
                        using (var cmd = Db.Command(c, tx,
                            "SET IDENTITY_INSERT planiranje.OP_Jobs ON; " +
                            "INSERT planiranje.OP_Jobs (ID, PlatilacID, OrganizatorID, TipPrevozaID, OtpravnaID, UputnaID, Status, Napomena, PreuzimaOdID, PredajeOperateruID, " +
                            "Stanica1ID, Stanica2ID, Stanica3ID, ETD, NHM1, Bruto1, BrojKola1, SerijaKola1, Duzina1, Neto1, RIDBroj1, OMBroj1, IzvornaNajavaID, Kreirao) " +
                            "SELECT n.ID, NULLIF(n.Platilac, 0), NULL, NULLIF(n.TipPrevoza, 0), NULLIF(n.Otpravna, 0), NULLIF(n.Uputna, 0), ISNULL(n.Status, 1), " +
                            "NULLIF(RTRIM(CAST(n.NapomenaPriprema AS nvarchar(1000))), N''), NULLIF(n.Prevoznik, 0), NULLIF(n.PrevoznikZa, 0), " +
                            "@s1, NULL, @s3, CASE WHEN n.PredvidjenoPrimanje < '19500101' THEN NULL ELSE n.PredvidjenoPrimanje END, " +
                            "NULLIF(n.RobaNHM, 0), n.Tezina, n.BrojKola, NULLIF(RTRIM(n.SerijaVagona), N''), n.Duzina, n.NetoTezinaM, NULLIF(RTRIM(n.RidBroj), N''), NULLIF(RTRIM(n.OnBroj), N''), n.ID, @k " +
                            "FROM dbo.Najava n WHERE n.ID = @n; " +
                            "SET IDENTITY_INSERT planiranje.OP_Jobs OFF;",
                            "@n", najava, "@s1", s["S1"], "@s3", s["S3"], "@k", PlaniranjeDb.Korisnik))
                            cmd.ExecuteNonQuery();
                        Log(c, tx, najava, "PRENOS", "Preneto iz najave " + najava + ".");
                        // Postojeći posao najave (pravac 1) ostaje; nov se pravi samo ako ga nema.
                        var j = new JobOP { ID = najava, Stanica1ID = Convert.ToInt32(s["S1"]), Stanica3ID = Convert.ToInt32(s["S3"]) };
                        DataTable etd = Db.Table(c, tx, "SELECT ETD FROM planiranje.OP_Jobs WHERE ID = @id", "@id", najava);
                        j.ETD = etd.Rows[0].IsNull("ETD") ? (DateTime?)null : Convert.ToDateTime(etd.Rows[0]["ETD"]);
                        if (Db.Table(c, tx, "SELECT 1 FROM planiranje.OP_Posao WHERE NajavaID = @j AND Status <> N'OTKAZAN'", "@j", najava).Rows.Count == 0)
                            UskladiPoslove(c, tx, j, new List<string>());
                    }
                    tx.Commit();
                    return new[] { preneti.Count, preskoceno };
                }
                catch { tx.Rollback(); throw; }
            }
        }
    }
}
