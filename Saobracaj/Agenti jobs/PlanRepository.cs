using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Saobracaj.Agenti_jobs
{
    public sealed class PlanMark
    {
        public int ID, Start, Length;
        public string Text = "", User = "";
        public DateTime CreatedUtc;
    }

    public sealed class PlanSnapshot
    {
        public int PlanID, ItemID, VersionID, VersionNumber;
        public string Text = "";
        public string Name = "";
        public List<PlanMark> Marks = new List<PlanMark>();
    }

    public static class PlanRepository
    {
        public static string Normalize(string s)
        {
            return (s ?? "").Replace("\r\n", "\n").Replace("\r", "\n");
        }

        public static PlanSnapshot Load(int planId = -1)
        {
            using (var c = Db.Open())
                return Load(c, null, planId);
        }

        private static PlanSnapshot Load(SqlConnection c, SqlTransaction tx, int planId)
        {
            var result = new PlanSnapshot();
            if (planId == 0)
                return result;
            var head = Db.Table(c, tx, planId < 0 ? "SELECT TOP 1 * FROM dbo.OperativniPlan ORDER BY OperativniPlanID DESC" : "SELECT * FROM dbo.OperativniPlan WHERE OperativniPlanID=@id", "@id", planId);
            if (head.Rows.Count == 0)
                return result;
            result.PlanID = Convert.ToInt32(head.Rows[0]["OperativniPlanID"]);
            result.Name = Db.Text(head.Rows[0], "Naziv");
            var version = Db.Table(c, tx, @"SELECT TOP 1 v.* FROM dbo.OperativniPlanStavka s JOIN dbo.OperativniPlanStavkaVerzija v ON v.StavkaID=s.StavkaID WHERE s.OperativniPlanID=@id AND s.JeObrisana=0 ORDER BY s.Redosled,v.BrojVerzije DESC", "@id", result.PlanID);
            if (version.Rows.Count == 0)
                return result;
            var r = version.Rows[0];
            result.ItemID = Convert.ToInt32(r["StavkaID"]);
            result.VersionID = Convert.ToInt32(r["StavkaVerzijaID"]);
            result.VersionNumber = Convert.ToInt32(r["BrojVerzije"]);
            result.Text = Db.Text(r, "Tekst");
            foreach (DataRow mark in Db.Table(c, tx, "SELECT * FROM dbo.OperativniPlanOznaka WHERE StavkaVerzijaID=@id AND JePonistena=0 ORDER BY OznakaID", "@id", result.VersionID).Rows)
                result.Marks.Add(new PlanMark { ID = Convert.ToInt32(mark["OznakaID"]), Start = Convert.ToInt32(mark["PocetnaPozicija"]), Length = Convert.ToInt32(mark["Duzina"]), Text = Db.Text(mark, "OznaceniTekst"), User = Db.Text(mark, "Oznacio"), CreatedUtc = Convert.ToDateTime(mark["OznacenoUtc"]) });
            return result;
        }

        public static PlanSnapshot Save(int planId, int expectedVersion, string text)
        {
            text = Normalize(text);
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OperativniPlanDocument");
                    var old = Load(c, tx, planId);
                    if (old.VersionID != expectedVersion)
                        throw new DBConcurrencyException("Plan je u međuvremenu promenjen. Sačuvajte svoj tekst van aplikacije, ponovo učitajte plan i uskladite izmene.");
                    int pid = old.PlanID, item = old.ItemID;
                    if (pid == 0)
                        using (var cmd = Db.Command(c, tx, "INSERT dbo.OperativniPlan(Naziv,DatumPlana,Kreirao) VALUES(@name,@date,@user);SELECT CAST(SCOPE_IDENTITY() AS int)", "@name", "Operativni plan " + RouteRules.Belgrade(DateTime.UtcNow).ToString("dd.MM.yyyy."), "@date", RouteRules.Belgrade(DateTime.UtcNow).Date, "@user", OperationalSession.UserName))
                            pid = Convert.ToInt32(cmd.ExecuteScalar());
                    if (item == 0)
                        using (var cmd = Db.Command(c, tx, "INSERT dbo.OperativniPlanStavka(OperativniPlanID,Redosled) VALUES(@id,0);SELECT CAST(SCOPE_IDENTITY() AS int)", "@id", pid))
                            item = Convert.ToInt32(cmd.ExecuteScalar());
                    if (old.VersionID != 0 && old.Text == text)
                    {
                        tx.Commit();
                        return Load(pid);
                    }

                    int version;
                    using (var cmd = Db.Command(c, tx, "INSERT dbo.OperativniPlanStavkaVerzija(StavkaID,BrojVerzije,Tekst,Izmenio) VALUES(@item,@number,@text,@user);SELECT CAST(SCOPE_IDENTITY() AS int)", "@item", item, "@number", old.VersionNumber + 1, "@text", text, "@user", OperationalSession.UserName))
                        version = Convert.ToInt32(cmd.ExecuteScalar());
                    var mapper = new TextRangeMapper(old.Text, text);
                    foreach (var mark in old.Marks)
                    {
                        int start;
                        if (!mapper.TryMap(mark.Start, mark.Length, out start))
                            continue;
                        using (var cmd = Db.Command(c, tx, @"INSERT dbo.OperativniPlanOznaka(StavkaVerzijaID,PocetnaPozicija,Duzina,OznaceniTekst,Oznacio,OznacenoUtc,PrenetoSaOznakeID) VALUES(@v,@start,@length,@text,@user,@utc,@old)", "@v", version, "@start", start, "@length", mark.Length, "@text", mark.Text, "@user", mark.User, "@utc", mark.CreatedUtc, "@old", mark.ID))
                            cmd.ExecuteNonQuery();
                    }

                    using (var cmd = Db.Command(c, tx, "UPDATE dbo.OperativniPlan SET Izmenio=@user,IzmenjenoUtc=GETUTCDATE() WHERE OperativniPlanID=@id", "@user", OperationalSession.UserName, "@id", pid))
                        cmd.ExecuteNonQuery();
                    tx.Commit();
                    return Load(pid);
                }
                catch
                {
                    if (tx.Connection != null)
                        tx.Rollback();
                    throw;
                }
            }
        }

        public static void Mark(int planId, int versionId, int start, int length, string selected, bool remove)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OperativniPlanDocument");
                    var current = Load(c, tx, planId);
                    if (current.VersionID != versionId)
                        throw new DBConcurrencyException("Plan je ažuriran. Sačekajte osvežavanje i ponovo označite tekst.");
                    if (start < 0 || length <= 0 || start > current.Text.Length - length || current.Text.Substring(start, length) != selected)
                        throw new InvalidOperationException("Selekcija ne odgovara učitanoj verziji plana.");
                    if (remove)
                    {
                        using (var cmd = Db.Command(c, tx, "UPDATE dbo.OperativniPlanOznaka SET JePonistena=1,Ponistio=@user,PonistenoUtc=GETUTCDATE() WHERE StavkaVerzijaID=@v AND JePonistena=0 AND PocetnaPozicija<@end AND PocetnaPozicija+Duzina>@start", "@user", OperationalSession.UserName, "@v", versionId, "@end", start + length, "@start", start))
                            cmd.ExecuteNonQuery();
                    }
                    else if (!current.Marks.Any(m => m.Start == start && m.Length == length))
                        using (var cmd = Db.Command(c, tx, "INSERT dbo.OperativniPlanOznaka(StavkaVerzijaID,PocetnaPozicija,Duzina,OznaceniTekst,Oznacio) VALUES(@v,@start,@length,@text,@user)", "@v", versionId, "@start", start, "@length", length, "@text", selected, "@user", OperationalSession.UserName))
                            cmd.ExecuteNonQuery();
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }
    }
}
