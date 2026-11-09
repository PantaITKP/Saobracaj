using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Saobracaj.Agenti_jobs
{
    public sealed class RouteCatalog
    {
        public long DocumentID { get; set; }
        public string FileName { get; set; } = "";
        public List<RouteInfo> Routes { get; set; } = new List<RouteInfo>();
    }

    public static class OperationsRepository
    {
        public static readonly Dictionary<string, string[]> EditableTables = new Dictionary<string, string[]>
        {
            {
                "OperativniPlanMreza",
                new[]
                {
                    "Stanica",
                    "UputnaStanica",
                    "Vagoni",
                    "IDVoza",
                    "Planiranje",
                    "Kolosek",
                    "BrojVoza",
                    "Klijent",
                    "Napomena",
                    "Carina",
                    "TPU"
                }
            },
            {
                "OperativniPlanNajave",
                new[]
                {
                    "GranicniPrelaz",
                    "UputnaStanica",
                    "Vagoni",
                    "IDVoza",
                    "OcekivanoVreme",
                    "BrojVoza",
                    "PRP",
                    "Operater",
                    "StatusNajave",
                    "Klijent",
                    "Napomena"
                }
            },
            {
                "OperativniPlanLokomotive",
                new[]
                {
                    "Lokomotiva",
                    "Sastav",
                    "BrojVoza",
                    "Napomena",
                    "Klijent",
                    "TPU"
                }
            },
            {
                "OperativniPlanVozovi",
                new[]
                {
                    "From",
                    "To",
                    "LocomotiveNumber",
                    "Note",
                    "Rezi",
                    "Started",
                    "StartedAtUtc",
                    "StartedAtLocal",
                    "Completed",
                    "CompletedAtLocal",
                    "Stopped",
                    "StopStation"
                }
            }
        };
        public static DataTable LoadGrid(string table, bool synchronizeStarts = true)
        {
            if (!EditableTables.ContainsKey(table))
                throw new ArgumentException("Nepoznata tabela.");
            using (var c = Db.Open())
            {
                if (table == "OperativniPlanVozovi" && synchronizeStarts)
                    SynchronizeStartHistory(c);
                // Mreža i najave: vreme unosa i poslednje izmene po lokalnom vremenu, samo za čitanje (čuvaju se samo kolone za unos).
                bool saVremenima = table == "OperativniPlanMreza" || table == "OperativniPlanNajave";
                var data = Db.Table(c, null, "SELECT *" +
                    (saVremenima ? ", DATEADD(minute, DATEDIFF(minute, GETUTCDATE(), GETDATE()), CreatedUtc) AS Uneto, DATEADD(minute, DATEDIFF(minute, GETUTCDATE(), GETDATE()), IzmenjenoUtc) AS PoslednjaIzmena" : "") +
                    " FROM dbo.[" + table + "]" +
                    (table == "OperativniPlanVozovi" ? " WHERE CompletionRecorded=0" : "") + " ORDER BY ID");
                if (saVremenima)
                {
                    data.Columns["Uneto"].ReadOnly = true;
                    data.Columns["PoslednjaIzmena"].ReadOnly = true;
                }
                return data;
            }
        }

        public static DataTable LoadNajava()
        {
            var years = TrainText.AnnouncementYears(DateTime.Now);
            using (var c = Db.Open())
                return Db.Table(c, null, @"
Select CONVERT(nvarchar(100),Najava.ID) as FullId,
CONVERT(nvarchar(100),Najava.ID) as BrojNajave,
RTrim(s1.Opis) as Otpravna,RTrim(s2.Opis) as Uputna,RTrim(p1.PaNaziv) as Platilac,
tezina as Tezina,duzina as Duzina,
LTRIM(RTRIM(COALESCE(CAST(Najava.BrojKola as nvarchar(5)),N'')+N' '+COALESCE(Najava.SerijaVagona,N''))) as Kola
from najava
inner join stanice s1 on s1.ID=najava.Otpravna
inner join stanice s2 on s2.ID=najava.Uputna
inner join Partnerji p1 on p1.PaSifra=Najava.Platilac
where LEFT(CONVERT(nvarchar(100),Najava.ID),2) IN (@previous,@current)
order by Najava.ID desc", "@previous", years[0], "@current", years[1]);
        }

        public static bool SaveGrid(string table, DataTable data)
        {
            if (!EditableTables.ContainsKey(table))
                throw new ArgumentException("Nepoznata tabela.");
            var changes = data.GetChanges();
            if (changes == null)
                return false;
            var columns = EditableTables[table];
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    if (table == "OperativniPlanVozovi")
                        Db.Lock(c, tx, "OperativniPlanTrainNumbers");
                    foreach (DataRow r in changes.Rows)
                    {
                        if (r.RowState == DataRowState.Added)
                        {
                            if (table == "OperativniPlanVozovi")
                                throw new InvalidOperationException("Voz dodajte preko predloga ili Ad hoc opcije.");
                            var names = string.Join(",", columns.Select(x => "[" + x + "]"));
                            var pars = string.Join(",", columns.Select((x, i) => "@p" + i));
                            using (var cmd = Db.Command(c, tx, "INSERT dbo.[" + table + "] (" + names + ") VALUES(" + pars + ")"))
                            {
                                for (int i = 0; i < columns.Length; i++)
                                    AddValue(cmd, "@p" + i, r[columns[i]]);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            var id = r["ID", DataRowVersion.Original];
                            var rv = r["VerzijaReda", DataRowVersion.Original];
                            string sql;
                            if (r.RowState == DataRowState.Deleted)
                                sql = "DELETE FROM dbo.[" + table + "] WHERE ID=@id AND VerzijaReda=@rv";
                            else
                            {
                                if (table == "OperativniPlanVozovi")
                                {
                                    if (Convert.ToBoolean(r["Completed"])) RadniNalogDopuna.ProveriNajavuZaZavrsetak(r);
                                    bool after = Convert.ToBoolean(r["Started"]);
                                    if (!after)
                                    {
                                        r["StartedAtUtc"] = DBNull.Value;
                                        r["StartedAtLocal"] = DBNull.Value;
                                    }
                                    else
                                    {
                                        if (r.IsNull("StartedAtUtc")) r["StartedAtUtc"] = DateTime.UtcNow;
                                        if (r.IsNull("StartedAtLocal")) r["StartedAtLocal"] = DateTime.Now;
                                    }
                                    if (Convert.ToBoolean(r["Completed"]) && r.IsNull("CompletedAtLocal"))
                                        r["CompletedAtLocal"] = DateTime.Now;
                                    if (!Convert.ToBoolean(r["Completed"]))
                                        r["CompletedAtLocal"] = DBNull.Value;
                                }

                                sql = "UPDATE dbo.[" + table + "] SET " + string.Join(",", columns.Select((x, i) => "[" + x + "]=@p" + i)) + ",IzmenjenoUtc=GETUTCDATE() WHERE ID=@id AND VerzijaReda=@rv";
                            }

                            using (var cmd = Db.Command(c, tx, sql, "@id", id, "@rv", rv))
                            {
                                if (r.RowState != DataRowState.Deleted)
                                    for (int i = 0; i < columns.Length; i++)
                                        AddValue(cmd, "@p" + i, r[columns[i]]);
                                if (cmd.ExecuteNonQuery() != 1)
                                    throw new DBConcurrencyException("Red je promenjen na drugom računaru. Ponovo učitajte tabelu; lokalna izmena nije prepisala tuđu.");
                            }
                            if (table == "OperativniPlanVozovi" && r.RowState != DataRowState.Deleted)
                            {
                                bool before = Convert.ToBoolean(r["Started", DataRowVersion.Original]);
                                bool after = Convert.ToBoolean(r["Started"]);
                                bool restarted = before && after && !Equals(r["StartedAtUtc"], r["StartedAtUtc", DataRowVersion.Original]);
                                if (!after || restarted)
                                    DeleteStart(c, tx, Convert.ToInt64(id));
                                if (after)
                                    InsertStart(c, tx, Convert.ToInt64(id), Db.Text(r, "Train"),
                                        Convert.ToDateTime(r["StartedAtLocal"]), r.IsNull("Price") ? (decimal?)null : Convert.ToDecimal(r["Price"]), Convert.ToBoolean(r["IsAdHoc"]));
                            }
                        }
                    }

                    tx.Commit();
                    return true;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        private static void AddValue(SqlCommand cmd, string name, object value)
        {
            var p = cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
            if (value is string)
            {
                p.SqlDbType = SqlDbType.NVarChar;
                p.Size = -1;
            }
        }

        public static long ActiveCatalogID()
        {
            using (var c = Db.Open())
            using (var cmd = Db.Command(c, null, "SELECT ISNULL(AktivniDokumentID,0) FROM dbo.OperativniPlanPodesavanje WHERE ID=1"))
                return Convert.ToInt64(cmd.ExecuteScalar());
        }

        public static RouteCatalog LoadCatalog()
        {
            using (var c = Db.Open())
            {
                var info = Db.Table(c, null, "SELECT d.ID,d.OriginalniNaziv FROM dbo.OperativniPlanPodesavanje p JOIN dbo.OperativniPlanTraseDokument d ON d.ID=p.AktivniDokumentID WHERE p.ID=1");
                if (info.Rows.Count == 0)
                    return new RouteCatalog();
                var id = Convert.ToInt64(info.Rows[0]["ID"]);
                var result = new RouteCatalog
                {
                    DocumentID = id,
                    FileName = Convert.ToString(info.Rows[0]["OriginalniNaziv"])
                };
                var rows = Db.Table(c, null, "SELECT * FROM dbo.OperativniPlanTrasa WHERE DocumentID=@id ORDER BY ID", "@id", id);
                foreach (DataRow r in rows.Rows)
                    result.Routes.Add(new RouteInfo { ID = Convert.ToInt64(r["ID"]), DocumentID = id, From = Db.Text(r, "From"), To = Db.Text(r, "To"), Train = Db.Text(r, "Train"), MaxWeight = r.IsNull("MaxWeight") ? (decimal?)null : Convert.ToDecimal(r["MaxWeight"]), Price = r.IsNull("Price") ? (decimal?)null : Convert.ToDecimal(r["Price"]), Departure = Db.Text(r, "Departure"), Arrival = Db.Text(r, "Arrival"), Days = Db.Text(r, "Days"), Locomotive = Db.Text(r, "Locomotive"), Length = Db.Text(r, "Length"), Note = Db.Text(r, "Note") });
                return result;
            }
        }

        // This method is called only by the planner's import menu.
        internal static int ImportCatalog(string path, DateTime validFrom, DateTime validTo)
        {
            CenovnikPravila.ProveriPeriod(validFrom, validTo);
            var bytes = File.ReadAllBytes(path);
            List<RouteInfo> routes;
            using (var stream = new MemoryStream(bytes))
                routes = ExcelRoutes.Read(stream);
            string hash;
            using (var sha = SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OperativniPlanRouteCatalog");
                    long id;
                    using (var cmd = Db.Command(c, tx, "INSERT dbo.OperativniPlanTraseDokument(OriginalniNaziv,Sha256,Sadrzaj,Uvezao,VaziOd,VaziDo,Godina) VALUES(@name,@hash,@data,@user,@od,@do,@year);SELECT CAST(SCOPE_IDENTITY() AS bigint)", "@name", Path.GetFileName(path), "@hash", hash, "@data", bytes, "@user", OperationalSession.UserName,
                        "@od", validFrom.Date, "@do", validTo.Date, "@year", validTo.Year))
                        id = Db.ScalarId(cmd);
                    foreach (var r in routes)
                        using (var cmd = Db.Command(c, tx, "INSERT dbo.OperativniPlanTrasa(DocumentID,[From],[To],Train,MaxWeight,Price,Departure,Arrival,Days,Locomotive,Length,Note,LengthValue,BrakePercent,Relation) VALUES(@doc,@from,@to,@train,@weight,@price,@departure,@arrival,@days,@loco,@length,@note,@lengthValue,@percent,@relation)", "@doc", id, "@from", r.From, "@to", r.To, "@train", r.Train, "@weight", r.MaxWeight, "@price", r.Price, "@departure", r.Departure, "@arrival", r.Arrival, "@days", r.Days, "@loco", r.Locomotive, "@length", r.Length, "@note", r.Note,
                            "@lengthValue", r.LengthValue, "@percent", r.BrakePercent, "@relation", r.Relation))
                            cmd.ExecuteNonQuery();
                    using (var cmd = Db.Command(c, tx, "UPDATE dbo.OperativniPlanPodesavanje SET AktivniDokumentID=@id,IzmenjenoUtc=GETUTCDATE() WHERE ID=1", "@id", id))
                        cmd.ExecuteNonQuery();
                    tx.Commit();
                    return routes.Count;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public static List<PlannedTrain> Plans(DataTable data)
        {
            return data.Rows.Cast<DataRow>().Where(r => r.RowState != DataRowState.Deleted).Select(r => new PlannedTrain { ID = Convert.ToInt64(r["ID"]), Train = Db.Text(r, "Train"), From = Db.Text(r, "From"), To = Db.Text(r, "To"), Weight = Convert.ToDecimal(r["Weight"]), MaxWeight = r.IsNull("MaxWeight") ? (decimal?)null : Convert.ToDecimal(r["MaxWeight"]), Price = r.IsNull("Price") ? (decimal?)null : Convert.ToDecimal(r["Price"]), Note = Db.Text(r, "Note"), LocomotiveNumber = Db.Text(r, "LocomotiveNumber"), AnnouncementNumber = Db.Text(r, "AnnouncementNumber"), Rezi = Convert.ToBoolean(r["Rezi"]), IsAdHoc = Convert.ToBoolean(r["IsAdHoc"]), Started = Convert.ToBoolean(r["Started"]), StartedAtUtc = r.IsNull("StartedAtUtc") ? (DateTime?)null : Convert.ToDateTime(r["StartedAtUtc"]), StartedAtLocal = r.IsNull("StartedAtLocal") ? (DateTime?)null : Convert.ToDateTime(r["StartedAtLocal"]), Completed = Convert.ToBoolean(r["Completed"]), CompletedAtLocal = r.IsNull("CompletedAtLocal") ? (DateTime?)null : Convert.ToDateTime(r["CompletedAtLocal"]), Stopped = Convert.ToBoolean(r["Stopped"]), StopStation = Db.Text(r, "StopStation") }).ToList();
        }

        public static long AddTrain(RouteInfo route, string from, string to, string number, decimal weight, bool adhoc)
        {
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to) || weight <= 0)
                throw new InvalidOperationException("Unesite obe stanice i težinu veću od nule.");
            if (adhoc && !System.Text.RegularExpressions.Regex.IsMatch(number ?? "", @"^[0-9]+$"))
                throw new InvalidOperationException("Broj trase mora sadržati samo cifre.");
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OperativniPlanTrainNumbers");
                    if (!adhoc)
                    {
                        Db.Lock(c, tx, "OperativniPlanRouteCatalog");
                        using (var cmd = Db.Command(c, tx, "SELECT COUNT(*) FROM dbo.OperativniPlanTrasa r JOIN dbo.OperativniPlanPodesavanje p ON p.AktivniDokumentID=r.DocumentID WHERE r.ID=@id AND p.ID=1", "@id", route.ID))
                            if (Convert.ToInt32(cmd.ExecuteScalar()) != 1)
                                throw new InvalidOperationException("Red vožnje je promenjen. Ponovo predložite trasu.");
                        if (!route.MaxWeight.HasValue || route.MaxWeight < weight)
                            throw new InvalidOperationException("Težina prelazi limit trase.");
                    }

                    // Istorija pokretanja nije rezervacija za narednu vožnju.
                    var reservations = Plans(Db.Table(c, tx, "SELECT * FROM dbo.OperativniPlanVozovi WHERE CompletionRecorded=0 AND Started=0"));
                    number = RouteRules.Allocate(number.Trim(), reservations, DateTime.UtcNow);
                    if (adhoc) Db.Lock(c, tx, "OperativniPlanRouteCatalog");
                    var document = Db.Table(c, tx, @"SELECT d.ID,d.VaziOd,d.VaziDo,d.Godina
FROM dbo.OperativniPlanPodesavanje p JOIN dbo.OperativniPlanTraseDokument d ON d.ID=p.AktivniDokumentID WHERE p.ID=1");
                    DataRow doc = document.Rows.Count == 0 ? null : document.Rows[0];
                    long id;
                    using (var cmd = Db.Command(c, tx, "INSERT dbo.OperativniPlanVozovi(Train,OriginalTrain,RouteID,[From],[To],Weight,MaxWeight,Price,IsAdHoc,CreatedBy,CenovnikDokumentID,CenovnikVaziOd,CenovnikVaziDo,CenovnikGodina) VALUES(@train,@original,@route,@from,@to,@weight,@max,@price,@adhoc,@user,@document,@validFrom,@validTo,@year);SELECT CAST(SCOPE_IDENTITY() AS bigint)", "@train", number, "@original", adhoc ? number : route.Train, "@route", adhoc ? (object)DBNull.Value : route.ID, "@from", from.Trim(), "@to", to.Trim(), "@weight", weight, "@max", adhoc ? null : route.MaxWeight, "@price", adhoc ? null : route.Price, "@adhoc", adhoc, "@user", OperationalSession.UserName,
                        "@document", doc == null ? null : doc["ID"], "@validFrom", doc == null ? null : doc["VaziOd"],
                        "@validTo", doc == null ? null : doc["VaziDo"], "@year", doc == null ? null : doc["Godina"]))
                        id = Db.ScalarId(cmd);
                    tx.Commit();
                    return id;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public static void LinkNajava(long id, byte[] rowVersion, string fullId, string payer, string wagons = null)
        {
            if (fullId != null && fullId.Trim().Length <= 2)
                throw new InvalidOperationException("ID najave je prekratak.");
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    var rows = Db.Table(c, tx, "SELECT Note,AnnouncementWagons FROM dbo.OperativniPlanVozovi WHERE ID=@id AND VerzijaReda=@rv AND CompletionRecorded=0",
                        "@id", id, "@rv", rowVersion);
                    if (rows.Rows.Count != 1)
                        throw new DBConcurrencyException("Voz je u međuvremenu promenjen. Ponovo učitajte spisak.");
                    var old = rows.Rows[0];
                    string composition = fullId == null ? null : wagons;
                    string note = TrainText.WithWagons(Db.Text(old, "Note"), Db.Text(old, "AnnouncementWagons"), composition);
                    using (var cmd = Db.Command(c, tx, @"UPDATE dbo.OperativniPlanVozovi SET
AnnouncementFullId=@full,AnnouncementNumber=@short,AnnouncementPayer=@payer,AnnouncementWagons=@wagons,Note=@note,IzmenjenoUtc=GETUTCDATE()
WHERE ID=@id AND VerzijaReda=@rv AND CompletionRecorded=0", "@id", id, "@rv", rowVersion, "@full", fullId,
                        "@short", fullId == null ? null : fullId.Trim().Substring(2), "@payer", payer,
                        "@wagons", composition, "@note", note))
                        if (cmd.ExecuteNonQuery() != 1)
                            throw new DBConcurrencyException("Voz je u međuvremenu promenjen. Ponovo učitajte spisak.");
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        private static void InsertStart(SqlConnection c, SqlTransaction tx, long id, string train, DateTime local, decimal? price, bool adhoc)
        {
            // The relationship is on the train, keeping the history table at the requested six columns.
            using (var existing = Db.Command(c, tx, @"SELECT p.ID
FROM dbo.OperativniPlanVozovi v JOIN dbo.OperativniPlanPokretanja p ON p.ID=v.PokretanjeID
WHERE v.ID=@id", "@id", id))
            {
                object value = existing.ExecuteScalar();
                if (value != null && value != DBNull.Value) return;
            }

            long history;
            using (var cmd = Db.Command(c, tx, @"INSERT dbo.OperativniPlanPokretanja
(BrojTrase,VremePokretanja,Cena,KreiranoUtc,adHoc) VALUES(@train,@local,@price,@created,@adhoc);
SELECT CAST(SCOPE_IDENTITY() AS bigint)", "@train", train, "@local", local,
                "@price", price, "@created", DateTime.Now, "@adhoc", adhoc))
                history = Db.ScalarId(cmd);

            using (var cmd = Db.Command(c, tx,
                "UPDATE dbo.OperativniPlanVozovi SET PokretanjeID=@history WHERE ID=@id",
                "@history", history, "@id", id))
                if (cmd.ExecuteNonQuery() != 1)
                    throw new DBConcurrencyException("Voz više ne postoji; pokretanje nije upisano.");
        }

        private static void DeleteStart(SqlConnection c, SqlTransaction tx, long id)
        {
            using (var cmd = Db.Command(c, tx, @"DELETE p
FROM dbo.OperativniPlanPokretanja p
JOIN dbo.OperativniPlanVozovi v ON v.PokretanjeID=p.ID WHERE v.ID=@id;
UPDATE dbo.OperativniPlanVozovi SET PokretanjeID=NULL WHERE ID=@id;", "@id", id))
                cmd.ExecuteNonQuery();
        }

        private static void SynchronizeStartHistory(SqlConnection c)
        {
            using (var check = Db.Command(c, null, @"SELECT TOP 1 v.ID
FROM dbo.OperativniPlanVozovi v WHERE v.Started=1 AND v.StartedAtUtc IS NOT NULL AND
(v.StartedAtLocal IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.OperativniPlanPokretanja p WHERE p.ID=v.PokretanjeID))"))
                if (check.ExecuteScalar() == null) return;
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OperativniPlanTrainNumbers");
                    var rows = Db.Table(c, tx, @"SELECT v.ID,v.Train,v.Price,v.IsAdHoc,v.StartedAtUtc,v.StartedAtLocal
FROM dbo.OperativniPlanVozovi v WHERE v.Started=1 AND v.StartedAtUtc IS NOT NULL AND
(v.StartedAtLocal IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.OperativniPlanPokretanja p WHERE p.ID=v.PokretanjeID))");
                    foreach (DataRow row in rows.Rows)
                    {
                        DateTime utc = Convert.ToDateTime(row["StartedAtUtc"]);
                        DateTime local = row.IsNull("StartedAtLocal") ? RouteRules.Belgrade(utc) : Convert.ToDateTime(row["StartedAtLocal"]);
                        if (row.IsNull("StartedAtLocal"))
                            using (var cmd = Db.Command(c, tx,
                                "UPDATE dbo.OperativniPlanVozovi SET StartedAtLocal=@local WHERE ID=@id",
                                "@local", local, "@id", row["ID"])) cmd.ExecuteNonQuery();
                        InsertStart(c, tx, Convert.ToInt64(row["ID"]), Db.Text(row, "Train"), local,
                            row.IsNull("Price") ? (decimal?)null : Convert.ToDecimal(row["Price"]), Convert.ToBoolean(row["IsAdHoc"]));
                    }
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        public static int RecordCompleted(List<SifarnikPrevoz> plans)
        {
            return SifarnikTrasaServis.Evidentiraj(plans);
        }
    }
}
