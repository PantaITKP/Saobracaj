using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Saobracaj.Agenti_jobs;
using Saobracaj.Planiranje.Modeli;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Podaci
{
    // Taskovi (OP_022): čitanje i sve promene u jednoj transakciji, sa događajem na poslu najave (pravilo 3.9).
    internal static class TaskRepo
    {
        internal static TaskOP Jedan(int taskId)
        {
            using (var c = Db.Open())
                return Ucitaj(c, null, "SELECT @id", true, "@id", taskId).FirstOrDefault();
        }

        // Taskovi za izbor u Ponudi osoblja: otvoreni i lansirani, od pre 7 dana pa nadalje.
        internal static DataTable ZaIzborOsoblja()
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT t.ID, N'Task ' + CAST(t.ID AS nvarchar(10)) + N' · ' + ISNULL(STUFF((SELECT N', ' + ISNULL(tn.BrojNajave, CAST(tn.NajavaID AS nvarchar(20))) " +
                    "FROM planiranje.OP_TaskNajava tn WHERE tn.TaskID = t.ID FOR XML PATH(''), TYPE).value('.', 'nvarchar(400)'), 1, 2, N''), N'') + " +
                    "N' · ' + a.Naziv + N' – ' + b.Naziv + ISNULL(N' · ' + CONVERT(nvarchar(5), t.ETD, 104) + N' ' + CONVERT(nvarchar(5), t.ETD, 108), N'') + " +
                    "N' · osoblje: ' + CAST((SELECT COUNT(*) FROM planiranje.OP_TaskOsoblje o WHERE o.TaskID = t.ID) AS nvarchar(10)) AS Naziv " +
                    "FROM planiranje.OP_Task t JOIN planiranje.OP_Stanice a ON a.ID = t.StanicaOdID JOIN planiranje.OP_Stanice b ON b.ID = t.StanicaDoID " +
                    "WHERE t.Status IN (N'OTVOREN', N'LANSIRAN') AND (t.ETD IS NULL OR t.ETD >= DATEADD(day, -7, CAST(GETDATE() AS date))) ORDER BY t.ETD, t.ID");
        }

        // Radno mesto taska (ko, gde, kada). Menja se dok mu zadatak nije poslat.
        internal static void SacuvajOsobu(TaskOP t, TaskOsoba o)
        {
            if (o.Do <= o.Od) throw new InvalidOperationException("Kraj radnog mesta mora biti posle početka.");
            if (t.Status == StatusTaska.Storniran || t.Status == StatusTaska.Zavrsen)
                throw new InvalidOperationException("Task " + t.ID + " je " + StatusTaska.Naziv(t.Status).ToLowerInvariant() + ".");
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OP_Task_" + t.ID);
                    if (o.ID == 0)
                        using (var cmd = Db.Command(c, tx,
                            "INSERT planiranje.OP_TaskOsoblje (TaskID, Uloga, StanicaID, Od, Do, Napomena, Kreirao) VALUES (@t, @u, @s, @od, @do, @n, @k)",
                            "@t", t.ID, "@u", o.Uloga, "@s", o.StanicaID, "@od", o.Od, "@do", o.Do, "@n", (object)o.Napomena, "@k", PlaniranjeDb.Korisnik))
                            cmd.ExecuteNonQuery();
                    else
                    {
                        ProveriBezZadatka(c, tx, o);
                        PlaniranjeDb.IzvrsiSaRowVer(c, tx,
                            "UPDATE planiranje.OP_TaskOsoblje SET StanicaID=@s, Od=@od, Do=@do, Napomena=@n WHERE ID=@id AND RowVer=@rv",
                            "@s", o.StanicaID, "@od", o.Od, "@do", o.Do, "@n", (object)o.Napomena, "@id", o.ID, "@rv", o.RowVer);
                    }
                    Dogadjaj(c, tx, new[] { t }, "TASK_OSOBLJE", "Task " + t.ID + ": " + TaskOsoba.NazivUloge(o.Uloga) + " " + o.Od.ToString("dd.MM. HH:mm") + " – " + o.Do.ToString("dd.MM. HH:mm") + (o.ID == 0 ? " dodat." : " izmenjen."));
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        internal static void ObrisiOsobu(TaskOP t, TaskOsoba o)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OP_Task_" + t.ID);
                    ProveriBezZadatka(c, tx, o);
                    // Zadaci bez odgovora ili odbijeni ostaju u evidenciji, bez veze sa radnim mestom.
                    using (var cmd = Db.Command(c, tx, "UPDATE planiranje.OP_Zadatak SET TaskOsobljeID = NULL WHERE TaskOsobljeID = @id", "@id", o.ID))
                        cmd.ExecuteNonQuery();
                    PlaniranjeDb.IzvrsiSaRowVer(c, tx, "DELETE planiranje.OP_TaskOsoblje WHERE ID=@id AND RowVer=@rv", "@id", o.ID, "@rv", o.RowVer);
                    Dogadjaj(c, tx, new[] { t }, "TASK_OSOBLJE", "Task " + t.ID + ": " + TaskOsoba.NazivUloge(o.Uloga) + " uklonjen.");
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        private static void ProveriBezZadatka(SqlConnection c, SqlTransaction tx, TaskOsoba o)
        {
            using (var cmd = Db.Command(c, tx, "SELECT COUNT(*) FROM planiranje.OP_Zadatak WHERE TaskOsobljeID = @id AND Status IN (N'POSLAT', N'PRIHVACEN')", "@id", o.ID))
                if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                    throw new InvalidOperationException(TaskOsoba.NazivUloge(o.Uloga) + ": zadatak je već poslat (" + o.Osoba + "). Prvo ga treba odbiti u Ponudi osoblja.");
        }

        internal static List<TaskOP> ZaNajavu(int najavaId, bool saStorniranim)
        {
            using (var c = Db.Open())
                return Ucitaj(c, null, "SELECT TaskID FROM planiranje.OP_TaskNajava WHERE NajavaID = @n", saStorniranim, "@n", najavaId);
        }

        // Taskovi najava iz perioda i taskovi koji polaze u periodu.
        internal static List<TaskOP> ZaPeriod(DateTime od, DateTime doDatuma, bool saStorniranim)
        {
            using (var c = Db.Open())
                return Ucitaj(c, null,
                    "SELECT tn.TaskID FROM planiranje.OP_TaskNajava tn JOIN planiranje.OP_vPosao p ON p.ID = tn.PosaoID WHERE " + PlanRepo.UPeriodu +
                    "UNION SELECT t.ID FROM planiranje.OP_Task t WHERE t.ETD >= @od AND t.ETD < @do",
                    saStorniranim, "@od", od, "@do", doDatuma);
        }

        // idUpit vraća kolonu ID-jeva taskova; isti argumenti važe za sva četiri upita.
        private static List<TaskOP> Ucitaj(SqlConnection c, SqlTransaction tx, string idUpit, bool saStorniranim, params object[] args)
        {
            var poId = new Dictionary<int, TaskOP>();
            foreach (DataRow r in Db.Table(c, tx,
                "SELECT t.ID, t.Km, t.BrutoT, t.BrojKola, t.DuzinaM, t.TipKola, t.ETD, t.ATD, t.ETA, t.ATA, t.Status, t.Nastanak, t.OpisRute, t.Napomena, t.TrajanjeMin, t.RowVer " +
                "FROM planiranje.OP_Task t WHERE t.ID IN (" + idUpit + ")" + (saStorniranim ? "" : " AND t.Status <> N'STORNIRAN'"), args).Rows)
                poId[Convert.ToInt32(r["ID"])] = new TaskOP
                {
                    ID = Convert.ToInt32(r["ID"]),
                    Km = PlanRepo.Dec(r, "Km") ?? 0,
                    BrutoT = PlanRepo.Dec(r, "BrutoT") ?? 0,
                    BrojKola = Convert.ToInt32(r["BrojKola"]),
                    DuzinaM = PlanRepo.Dec(r, "DuzinaM"),
                    TipKola = r.IsNull("TipKola") ? null : Convert.ToString(r["TipKola"]),
                    ETD = PlanRepo.Dat(r, "ETD"),
                    ATD = PlanRepo.Dat(r, "ATD"),
                    ETA = PlanRepo.Dat(r, "ETA"),
                    ATA = PlanRepo.Dat(r, "ATA"),
                    Status = Convert.ToString(r["Status"]),
                    Nastanak = Convert.ToString(r["Nastanak"]),
                    OpisRute = r.IsNull("OpisRute") ? null : Convert.ToString(r["OpisRute"]),
                    Napomena = r.IsNull("Napomena") ? null : Convert.ToString(r["Napomena"]),
                    RowVer = (byte[])r["RowVer"],
                    TrajanjeMin = PlanRepo.Ceo(r, "TrajanjeMin")
                };
            if (poId.Count == 0) return new List<TaskOP>();

            TaskOP t;
            foreach (DataRow r in Db.Table(c, tx,
                "SELECT s.TaskID, s.StanicaID, s.Km, s.PrugaID, s.TrasaID FROM planiranje.OP_TaskStanica s WHERE s.TaskID IN (" + idUpit + ") ORDER BY s.TaskID, s.RB", args).Rows)
                if (poId.TryGetValue(Convert.ToInt32(r["TaskID"]), out t))
                    t.Stanice.Add(new TaskStanica { StanicaID = Convert.ToInt32(r["StanicaID"]), Km = PlanRepo.Dec(r, "Km") ?? 0, PrugaID = PlanRepo.Ceo(r, "PrugaID"), TrasaID = PlanRepo.Ceo(r, "TrasaID") });
            foreach (DataRow r in Db.Table(c, tx,
                "SELECT v.TaskID, v.NajavaID, v.PosaoID, v.BrojNajave, v.KmOd, v.KmDo, v.RBDela, v.NajavaBrutoT, v.NajavaBrojKola, v.DeoRute, v.DelovaBruto " +
                "FROM planiranje.OP_vTaskNajava v WHERE v.TaskID IN (" + idUpit + ") ORDER BY v.TaskID, v.NajavaID", args).Rows)
                if (poId.TryGetValue(Convert.ToInt32(r["TaskID"]), out t))
                    t.Najave.Add(new TaskNajavaDeo
                    {
                        NajavaID = Convert.ToInt32(r["NajavaID"]),
                        PosaoID = PlanRepo.Ceo(r, "PosaoID"),
                        BrojNajave = r.IsNull("BrojNajave") ? Convert.ToString(r["NajavaID"]) : Convert.ToString(r["BrojNajave"]).Trim(),
                        KmOd = PlanRepo.Dec(r, "KmOd") ?? 0,
                        KmDo = PlanRepo.Dec(r, "KmDo") ?? 0,
                        RBDela = Convert.ToInt32(r["RBDela"]),
                        BrutoT = PlanRepo.Dec(r, "NajavaBrutoT") ?? 0,
                        BrojKola = Convert.ToInt32(r["NajavaBrojKola"]),
                        DeoRute = PlanRepo.Ceo(r, "DeoRute"),
                        DelovaBruto = PlanRepo.Ceo(r, "DelovaBruto")
                    });
            foreach (DataRow r in Db.Table(c, tx,
                "SELECT l.TaskID, l.LokomotivaID, l.Uloga, o.Broj FROM planiranje.OP_TaskLokomotiva l JOIN planiranje.OP_Lokomotive o ON o.ID = l.LokomotivaID " +
                "WHERE l.TaskID IN (" + idUpit + ") ORDER BY l.TaskID, CASE l.Uloga WHEN N'VUCNA' THEN 0 ELSE 1 END, l.RB", args).Rows)
                if (poId.TryGetValue(Convert.ToInt32(r["TaskID"]), out t))
                    t.Lokomotive.Add(new TaskLokomotiva { LokomotivaID = Convert.ToInt32(r["LokomotivaID"]), Uloga = Convert.ToString(r["Uloga"]), Broj = Convert.ToString(r["Broj"]).Trim() });
            // Radna mesta, sa osobom iz aktivnog zadatka (inače poslednjeg).
            foreach (DataRow r in Db.Table(c, tx,
                "SELECT o.ID, o.TaskID, o.Uloga, o.StanicaID, o.Od, o.Do, o.Napomena, o.RowVer, z.Status AS StatusZadatka, os.Naziv AS Osoba " +
                "FROM planiranje.OP_TaskOsoblje o " +
                "OUTER APPLY (SELECT TOP 1 x.Status, x.DeSifra FROM planiranje.OP_Zadatak x WHERE x.TaskOsobljeID = o.ID " +
                "ORDER BY CASE WHEN x.Status IN (N'POSLAT', N'PRIHVACEN') THEN 0 ELSE 1 END, x.ID DESC) z " +
                "LEFT JOIN planiranje.OP_vOsoblje os ON os.DeSifra = z.DeSifra " +
                "WHERE o.TaskID IN (" + idUpit + ") ORDER BY o.TaskID, o.Od, o.ID", args).Rows)
                if (poId.TryGetValue(Convert.ToInt32(r["TaskID"]), out t))
                    t.Osoblje.Add(new TaskOsoba
                    {
                        ID = Convert.ToInt32(r["ID"]),
                        TaskID = t.ID,
                        Uloga = Convert.ToString(r["Uloga"]),
                        StanicaID = Convert.ToInt32(r["StanicaID"]),
                        Od = Convert.ToDateTime(r["Od"]),
                        Do = Convert.ToDateTime(r["Do"]),
                        Napomena = r.IsNull("Napomena") ? null : Convert.ToString(r["Napomena"]),
                        RowVer = (byte[])r["RowVer"],
                        StatusZadatka = r.IsNull("StatusZadatka") ? null : Convert.ToString(r["StatusZadatka"]),
                        Osoba = r.IsNull("Osoba") ? null : Convert.ToString(r["Osoba"]).Trim()
                    });

            // Redosled po najavi i ruti: deo rute (KmOd), pa redni broj bruto dela; broj taska ne određuje redosled.
            return poId.Values.Where(x => x.Stanice.Count >= 2)
                .OrderBy(x => x.Najave.Select(n => n.NajavaID).DefaultIfEmpty().Min())
                .ThenBy(x => x.Najave.Select(n => n.KmOd).DefaultIfEmpty().Min())
                .ThenBy(x => x.Najave.Select(n => n.RBDela).DefaultIfEmpty().Min())
                .ThenBy(x => x.Status == StatusTaska.Storniran ? 1 : 0)
                .ThenBy(x => x.ID).ToList();
        }

        // Aktivni taskovi najave, da korisnik pre pravljenja novog zna šta se stornira.
        internal static List<TaskOP> AktivniZaNajavu(int najavaId)
        {
            return ZaNajavu(najavaId, false);
        }

        // Task iz izabrane rute. Postojeći otvoreni taskovi najave se storniraju samo uz potvrdu korisnika.
        internal static int Kreiraj(TaskOP novi, bool stornirajPostojece)
        {
            int najava = novi.Najave[0].NajavaID;
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OP_TaskNajava_" + najava);
                    // Postojeći taskovi istog pravca (posla); drugi pravac iste JOBS ima svoje taskove.
                    int posao = novi.Najave[0].PosaoID ?? 0;
                    List<TaskOP> postojeci = Ucitaj(c, tx, "SELECT TaskID FROM planiranje.OP_TaskNajava WHERE NajavaID = @n AND ISNULL(PosaoID, 0) = @p", false, "@n", najava, "@p", posao);
                    if (postojeci.Count > 0 && !stornirajPostojece)
                        throw new InvalidOperationException("Pravac već ima taskove. Osvežite prikaz.");
                    foreach (TaskOP p in postojeci)
                    {
                        TaskPravila.ProveriOtvoren(p);
                        Db.Lock(c, tx, "OP_Task_" + p.ID);
                        Storniraj(c, tx, p, "RUCNO");
                    }
                    int id = Upisi(c, tx, novi);
                    Prenumerisi(c, tx, najava);
                    Dogadjaj(c, tx, new[] { novi }, "TASK_NOV", "Task " + id + ": " + novi.OpisRute
                        + (postojeci.Count > 0 ? " Stornirani taskovi: " + string.Join(", ", postojeci.Select(p => p.ID)) + "." : ""));
                    tx.Commit();
                    return id;
                }
                catch { tx.Rollback(); throw; }
            }
        }

        // Split rute, split bruta i spajanje: izvorni taskovi se storniraju, a novi dobijaju status Otvoren.
        internal static List<int> Zameni(IList<TaskOP> izvorni, IList<TaskOP> novi, string vrsta)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    foreach (TaskOP t in izvorni.OrderBy(x => x.ID))
                    {
                        Db.Lock(c, tx, "OP_Task_" + t.ID);
                        ProveriVerziju(c, tx, t);
                        TaskPravila.ProveriOtvoren(t);
                    }
                    foreach (TaskOP t in izvorni) Storniraj(c, tx, t, vrsta);
                    var ids = new List<int>();
                    foreach (TaskOP n in novi)
                    {
                        n.Nastanak = vrsta;
                        int id = Upisi(c, tx, n);
                        ids.Add(id);
                        foreach (TaskOP t in izvorni)
                            using (var cmd = Db.Command(c, tx, "INSERT planiranje.OP_TaskVeza (TaskID, IzvorniTaskID, Vrsta) VALUES (@t, @i, @v)", "@t", id, "@i", t.ID, "@v", vrsta))
                                cmd.ExecuteNonQuery();
                    }
                    foreach (TaskOP n in novi) ProveriLokomotive(c, tx, n);
                    foreach (int najava in izvorni.Concat(novi).SelectMany(x => x.Najave).Select(x => x.NajavaID).Distinct())
                        Prenumerisi(c, tx, najava);
                    Dogadjaj(c, tx, izvorni.Concat(novi), "TASK_" + vrsta, NastanakTaska.Naziv(vrsta) + ": task " + string.Join(", ", izvorni.Select(x => x.ID))
                        + " → " + string.Join(", ", ids) + ".");
                    tx.Commit();
                    return ids;
                }
                catch { tx.Rollback(); throw; }
            }
        }

        // Za probni rad: briše sve taskove najave (i stornirane) i sve taskove spojene sa njima, pa najava kreće od početka.
        // Vraća broj obrisanih taskova; na posao se upisuje događaj TASK_BRISANJE.
        internal static int ObrisiZaNajavu(int najavaId)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OP_TaskNajava_" + najavaId);
                    const string taskovi = "SELECT TaskID FROM planiranje.OP_TaskNajava WHERE NajavaID = @n";
                    DataTable posloviNajava = Db.Table(c, tx,
                        "SELECT DISTINCT PosaoID FROM planiranje.OP_TaskNajava WHERE PosaoID IS NOT NULL AND TaskID IN (" + taskovi + ")", "@n", najavaId);
                    int broj;
                    using (var cmd = Db.Command(c, tx, "SELECT COUNT(*) FROM planiranje.OP_Task WHERE ID IN (" + taskovi + ")", "@n", najavaId))
                        broj = Convert.ToInt32(cmd.ExecuteScalar());
                    // OP_TaskVeza nema kaskadno brisanje (dve veze na OP_Task); ostale tabele taskova se brišu kaskadno.
                    using (var cmd = Db.Command(c, tx,
                        "DELETE planiranje.OP_Zadatak WHERE TaskID IN (" + taskovi + "); " +
                        "DELETE planiranje.OP_TaskVeza WHERE TaskID IN (" + taskovi + ") OR IzvorniTaskID IN (" + taskovi + "); " +
                        "DELETE planiranje.OP_Task WHERE ID IN (" + taskovi + ");", "@n", najavaId))
                        cmd.ExecuteNonQuery();
                    foreach (DataRow r in posloviNajava.Rows)
                        DogadjajRepo.Upisi(c, tx, Convert.ToInt32(r["PosaoID"]), "TASK_BRISANJE", "Obrisani svi taskovi najave " + najavaId + " (" + broj + ").");
                    tx.Commit();
                    return broj;
                }
                catch { tx.Rollback(); throw; }
            }
        }

        // Jedna vučna, ostale potisak; preklapanje sa drugim taskom ili kalendarom lokomotive se ne može potvrditi.
        internal static void SacuvajLokomotive(TaskOP t, List<TaskLokomotiva> lokomotive)
        {
            if (lokomotive.Count > 0 && lokomotive.Count(l => l.Uloga == TaskLokomotiva.Vucna) != 1)
                throw new InvalidOperationException("Task ima tačno jednu vučnu lokomotivu, a ostale su potisak.");
            if (lokomotive.Select(l => l.LokomotivaID).Distinct().Count() != lokomotive.Count)
                throw new InvalidOperationException("Ista lokomotiva je dodata dva puta.");
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OP_Task_" + t.ID);
                    ProveriVerziju(c, tx, t);
                    TaskPravila.ProveriOtvoren(t);
                    using (var cmd = Db.Command(c, tx, "DELETE planiranje.OP_TaskLokomotiva WHERE TaskID = @t", "@t", t.ID))
                        cmd.ExecuteNonQuery();
                    UpisiLokomotive(c, tx, t.ID, lokomotive);
                    var provera = new TaskOP { ID = t.ID, ETD = t.ETD, ATD = t.ATD, ETA = t.ETA, ATA = t.ATA, TrajanjeMin = t.TrajanjeMin, Lokomotive = lokomotive };
                    ProveriLokomotive(c, tx, provera);
                    PlaniranjeDb.IzvrsiSaRowVer(c, tx, "UPDATE planiranje.OP_Task SET Izmenio=@k, Izmenjeno=SYSDATETIME() WHERE ID=@id AND RowVer=@rv",
                        "@k", PlaniranjeDb.Korisnik, "@id", t.ID, "@rv", t.RowVer);
                    Dogadjaj(c, tx, new[] { t }, "TASK_LOKOMOTIVE", "Task " + t.ID + ": " + (lokomotive.Count == 0 ? "bez lokomotiva" : OpisLokomotiva(lokomotive)) + ".");
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        internal static string OpisLokomotiva(IEnumerable<TaskLokomotiva> lokomotive)
        {
            return string.Join(" + ", lokomotive.OrderBy(l => l.Uloga == TaskLokomotiva.Vucna ? 0 : 1).Select(l => l.Broj + (l.Uloga == TaskLokomotiva.Potisak ? " (potisak)" : "")));
        }

        internal static void SacuvajVremena(TaskOP t, DateTime? etd, DateTime? atd, DateTime? eta, DateTime? ata)
        {
            if (etd.HasValue && eta.HasValue && eta.Value <= etd.Value) throw new InvalidOperationException("ETA mora biti posle ETD.");
            if (atd.HasValue && ata.HasValue && ata.Value <= atd.Value) throw new InvalidOperationException("ATA mora biti posle ATD.");
            if (t.Status == StatusTaska.Storniran) throw new InvalidOperationException("Storniran task se ne menja.");
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OP_Task_" + t.ID);
                    PlaniranjeDb.IzvrsiSaRowVer(c, tx,
                        "UPDATE planiranje.OP_Task SET ETD=@etd, ATD=@atd, ETA=@eta, ATA=@ata, Izmenio=@k, Izmenjeno=SYSDATETIME() WHERE ID=@id AND RowVer=@rv AND Status <> N'STORNIRAN'",
                        "@etd", (object)etd, "@atd", (object)atd, "@eta", (object)eta, "@ata", (object)ata, "@k", PlaniranjeDb.Korisnik, "@id", t.ID, "@rv", t.RowVer);
                    if (t.Status != StatusTaska.Zavrsen)
                        ProveriLokomotive(c, tx, new TaskOP { ID = t.ID, ETD = etd, ATD = atd, ETA = eta, ATA = ata, TrajanjeMin = t.TrajanjeMin, Lokomotive = t.Lokomotive });
                    Dogadjaj(c, tx, new[] { t }, "TASK_VREMENA", "Task " + t.ID + ": ETD " + Vreme(etd) + ", ATD " + Vreme(atd) + ", ETA " + Vreme(eta) + ", ATA " + Vreme(ata) + ".");
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        // Otvoren → Lansiran → Završen; Lansiran se može vratiti u Otvoren, a Otvoren i Lansiran stornirati.
        internal static void PromeniStatus(TaskOP t, string novi, DateTime? atd = null, DateTime? ata = null)
        {
            string greska = null;
            if (novi == StatusTaska.Lansiran)
            {
                if (t.Status != StatusTaska.Otvoren) greska = "Lansira se samo otvoren task.";
                else if (!t.Lokomotive.Any(l => l.Uloga == TaskLokomotiva.Vucna)) greska = "Task nema vučnu lokomotivu.";
                else if (!t.ETD.HasValue || !t.ETA.HasValue) greska = "Task nema ETD i ETA.";
            }
            else if (novi == StatusTaska.Otvoren && t.Status != StatusTaska.Lansiran) greska = "U otvoren se vraća samo lansiran task.";
            else if (novi == StatusTaska.Zavrsen)
            {
                if (t.Status != StatusTaska.Lansiran) greska = "Završava se samo lansiran task.";
                else if (!(ata ?? t.ATA).HasValue) greska = "Za završen task upišite ATA.";
                else if ((atd ?? t.ATD).HasValue && (ata ?? t.ATA).Value <= (atd ?? t.ATD).Value) greska = "ATA mora biti posle ATD.";
            }
            else if (novi == StatusTaska.Storniran && t.Status != StatusTaska.Otvoren && t.Status != StatusTaska.Lansiran) greska = "Stornira se otvoren ili lansiran task.";
            if (greska != null) throw new InvalidOperationException("Task " + t.ID + ": " + greska);

            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OP_Task_" + t.ID);
                    PlaniranjeDb.IzvrsiSaRowVer(c, tx,
                        "UPDATE planiranje.OP_Task SET Status=@s, ATD=COALESCE(@atd, ATD), ATA=COALESCE(@ata, ATA), RazlogStorna=@r, Izmenio=@k, Izmenjeno=SYSDATETIME() WHERE ID=@id AND RowVer=@rv",
                        "@s", novi, "@atd", (object)atd, "@ata", (object)ata, "@r", novi == StatusTaska.Storniran ? "RUCNO" : null, "@k", PlaniranjeDb.Korisnik, "@id", t.ID, "@rv", t.RowVer);
                    if (novi == StatusTaska.Storniran)
                        foreach (int najava in t.Najave.Select(n => n.NajavaID).Distinct()) Prenumerisi(c, tx, najava);
                    Dogadjaj(c, tx, new[] { t }, "TASK_STATUS", "Task " + t.ID + ": " + StatusTaska.Naziv(t.Status) + " → " + StatusTaska.Naziv(novi) + ".");
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        private static string Vreme(DateTime? v)
        {
            return v.HasValue ? v.Value.ToString("dd.MM. HH:mm") : "–";
        }

        private static void ProveriVerziju(SqlConnection c, SqlTransaction tx, TaskOP t)
        {
            using (var cmd = Db.Command(c, tx, "SELECT RowVer FROM planiranje.OP_Task WHERE ID = @id", "@id", t.ID))
            {
                var rv = cmd.ExecuteScalar() as byte[];
                if (rv == null || t.RowVer == null || !rv.SequenceEqual(t.RowVer))
                    throw new DBConcurrencyException(PlaniranjeDb.PorukaKonkurentnost);
            }
        }

        private static void Storniraj(SqlConnection c, SqlTransaction tx, TaskOP t, string razlog)
        {
            PlaniranjeDb.IzvrsiSaRowVer(c, tx,
                "UPDATE planiranje.OP_Task SET Status=N'STORNIRAN', RazlogStorna=@r, Izmenio=@k, Izmenjeno=SYSDATETIME() WHERE ID=@id AND RowVer=@rv",
                "@r", razlog, "@k", PlaniranjeDb.Korisnik, "@id", t.ID, "@rv", t.RowVer);
        }

        // Kraj zauzetosti taska u SQL-u (alias t), isto kao TaskOP.Do: ATA, inače kasnije od ETA i polaska + trajanje.
        private const string KrajTaska =
            "COALESCE(t.ATA, CASE WHEN t.TrajanjeMin IS NOT NULL AND COALESCE(t.ATD, t.ETD) IS NOT NULL " +
            "AND (t.ETA IS NULL OR DATEADD(minute, t.TrajanjeMin, COALESCE(t.ATD, t.ETD)) > t.ETA) " +
            "THEN DATEADD(minute, t.TrajanjeMin, COALESCE(t.ATD, t.ETD)) ELSE t.ETA END)";

        // Trajanje se računa pri svakom upisu, jer split i spajanje menjaju km taska.
        internal static int Trajanje(TaskOP t)
        {
            DateTime datum = (t.ETD ?? DateTime.Today).Date;
            decimal obrada = ParametriRepo.Vrednost("TASK_OBRADA_MIN", datum, t.OdID, t.DoID, null, TaskPravila.PodrazumevanaObradaMin);
            decimal brzina = ParametriRepo.Vrednost("BRZINA_VOZA_KMH", datum, t.OdID, t.DoID, null, TaskPravila.PodrazumevanaBrzinaKmh);
            return TaskPravila.TrajanjeMin(t.Km, obrada, brzina);
        }

        private static int Upisi(SqlConnection c, SqlTransaction tx, TaskOP t)
        {
            t.TrajanjeMin = Trajanje(t);
            int id;
            using (var cmd = Db.Command(c, tx,
                "INSERT planiranje.OP_Task (StanicaOdID, StanicaDoID, Km, BrutoT, BrojKola, DuzinaM, TipKola, ETD, ATD, ETA, ATA, TrajanjeMin, Status, Nastanak, OpisRute, Napomena, Kreirao) " +
                "VALUES (@od, @do, @km, @b, @k, @d, @tip, @etd, @atd, @eta, @ata, @traj, N'OTVOREN', @n, @opis, @nap, @kor); SELECT CAST(SCOPE_IDENTITY() AS int);",
                "@od", t.OdID, "@do", t.DoID, "@km", t.Km, "@b", t.BrutoT, "@k", t.BrojKola, "@d", (object)t.DuzinaM, "@tip", (object)t.TipKola,
                "@etd", (object)t.ETD, "@atd", (object)t.ATD, "@eta", (object)t.ETA, "@ata", (object)t.ATA, "@n", t.Nastanak, "@opis", (object)t.OpisRute,
                "@nap", (object)t.Napomena, "@traj", t.TrajanjeMin.Value, "@kor", PlaniranjeDb.Korisnik))
                id = Convert.ToInt32(cmd.ExecuteScalar());
            for (int i = 0; i < t.Stanice.Count; i++)
            {
                TaskStanica s = t.Stanice[i];
                using (var cmd = Db.Command(c, tx, "INSERT planiranje.OP_TaskStanica (TaskID, RB, StanicaID, Km, PrugaID, TrasaID) VALUES (@t, @rb, @s, @km, @p, @tr)",
                    "@t", id, "@rb", i + 1, "@s", s.StanicaID, "@km", s.Km, "@p", (object)s.PrugaID, "@tr", (object)s.TrasaID))
                    cmd.ExecuteNonQuery();
            }
            foreach (TaskNajavaDeo n in t.Najave)
                using (var cmd = Db.Command(c, tx,
                    "INSERT planiranje.OP_TaskNajava (TaskID, NajavaID, PosaoID, BrojNajave, KmOd, KmDo, RBDela, BrutoT, BrojKola) VALUES (@t, @n, @p, @b, @od, @do, @rb, @br, @k)",
                    "@t", id, "@n", n.NajavaID, "@p", (object)n.PosaoID, "@b", (object)n.BrojNajave, "@od", n.KmOd, "@do", n.KmDo, "@rb", n.RBDela, "@br", n.BrutoT, "@k", n.BrojKola))
                    cmd.ExecuteNonQuery();
            UpisiLokomotive(c, tx, id, t.Lokomotive);
            t.ID = id;
            return id;
        }

        private static void UpisiLokomotive(SqlConnection c, SqlTransaction tx, int taskId, IEnumerable<TaskLokomotiva> lokomotive)
        {
            byte rb = 1;
            foreach (TaskLokomotiva l in lokomotive.OrderBy(x => x.Uloga == TaskLokomotiva.Vucna ? 0 : 1))
                using (var cmd = Db.Command(c, tx, "INSERT planiranje.OP_TaskLokomotiva (TaskID, LokomotivaID, Uloga, RB, Dodelio) VALUES (@t, @l, @u, @rb, @k)",
                    "@t", taskId, "@l", l.LokomotivaID, "@u", l.Uloga, "@rb", rb++, "@k", PlaniranjeDb.Korisnik))
                    cmd.ExecuteNonQuery();
        }

        // Unutar transakcije: lokomotiva ne sme da ima drugi task ni zapis u kalendaru (posao druge najave, servis, namenska) u vremenu taska.
        private static void ProveriLokomotive(SqlConnection c, SqlTransaction tx, TaskOP t)
        {
            if (t.Lokomotive.Count == 0) return;
            if (!t.Od.HasValue || !t.Do.HasValue)
                throw new InvalidOperationException("Task " + (t.ID > 0 ? t.ID.ToString() : "") + " nema ETD i ETA, a lokomotive se dodeljuju tasku sa vremenima.");
            if (t.Do.Value <= t.Od.Value) throw new InvalidOperationException("Dolazak taska mora biti posle polaska.");
            foreach (TaskLokomotiva l in t.Lokomotive.OrderBy(x => x.LokomotivaID))
            {
                Db.Lock(c, tx, "OP_Lok_" + l.LokomotivaID);
                DataTable drugi = Db.Table(c, tx,
                    "SELECT TOP 1 t.ID, COALESCE(t.ATD, t.ETD) AS Od, " + KrajTaska + " AS Do FROM planiranje.OP_TaskLokomotiva l JOIN planiranje.OP_Task t ON t.ID = l.TaskID " +
                    "WHERE l.LokomotivaID = @l AND t.ID <> @id AND t.Status <> N'STORNIRAN' AND COALESCE(t.ATD, t.ETD) < @do AND " + KrajTaska + " > @od ORDER BY 2",
                    "@l", l.LokomotivaID, "@id", t.ID, "@od", t.Od.Value, "@do", t.Do.Value);
                if (drugi.Rows.Count > 0)
                    throw new InvalidOperationException("Lokomotiva " + l.Broj + " je na tasku " + drugi.Rows[0]["ID"] + " od " + Vreme(PlanRepo.Dat(drugi.Rows[0], "Od")) + " do " + Vreme(PlanRepo.Dat(drugi.Rows[0], "Do")) + ". Preklapanje se ne može potvrditi.");
                DataTable kalendar = Db.Table(c, tx,
                    "SELECT TOP 1 k.Vrsta, k.PosaoID, k.Od, k.Do, k.Projekat, p.BrojNajave FROM planiranje.OP_vLokomotivaKalendar k LEFT JOIN planiranje.OP_vPosao p ON p.ID = k.PosaoID " +
                    "WHERE k.LokomotivaID = @l AND k.Od < @do AND k.Do > @od " +
                    "AND (k.PosaoID IS NULL OR k.PosaoID NOT IN (SELECT tn.PosaoID FROM planiranje.OP_TaskNajava tn WHERE tn.TaskID = @id AND tn.PosaoID IS NOT NULL)) ORDER BY k.Od",
                    "@l", l.LokomotivaID, "@id", t.ID, "@od", t.Od.Value, "@do", t.Do.Value);
                if (kalendar.Rows.Count > 0)
                {
                    DataRow k = kalendar.Rows[0];
                    string sta = k.IsNull("PosaoID") ? Convert.ToString(k["Vrsta"]).ToLowerInvariant() + (k.IsNull("Projekat") ? "" : " (" + k["Projekat"] + ")") : "posao najave " + Convert.ToString(k["BrojNajave"]).Trim();
                    throw new InvalidOperationException("Lokomotiva " + l.Broj + " ima " + sta + " od " + Vreme(PlanRepo.Dat(k, "Od")) + " do " + Vreme(PlanRepo.Dat(k, "Do")) + ". Preklapanje se ne može potvrditi.");
                }
            }
        }

        // Bruto delovi istog mesta na ruti najave dobijaju redne brojeve 1..n, po dosadašnjem redosledu.
        private static void Prenumerisi(SqlConnection c, SqlTransaction tx, int najavaId)
        {
            using (var cmd = Db.Command(c, tx,
                ";WITH x AS (SELECT tn.RBDela, ROW_NUMBER() OVER (PARTITION BY ISNULL(tn.PosaoID, 0), tn.KmOd ORDER BY tn.RBDela, tn.TaskID) AS N " +
                "FROM planiranje.OP_TaskNajava tn JOIN planiranje.OP_Task t ON t.ID = tn.TaskID WHERE tn.NajavaID = @n AND t.Status <> N'STORNIRAN') " +
                "UPDATE x SET RBDela = N WHERE RBDela <> N",
                "@n", najavaId))
                cmd.ExecuteNonQuery();
        }

        // Događaj ide na posao svake najave taska (Tok posla ga prikazuje).
        private static void Dogadjaj(SqlConnection c, SqlTransaction tx, IEnumerable<TaskOP> taskovi, string tip, string opis)
        {
            foreach (int posao in taskovi.SelectMany(t => t.Najave).Where(n => n.PosaoID.HasValue).Select(n => n.PosaoID.Value).Distinct())
                DogadjajRepo.Upisi(c, tx, posao, tip.Length > 30 ? tip.Substring(0, 30) : tip, opis.Length > 500 ? opis.Substring(0, 499) + "…" : opis);
        }
    }
}
