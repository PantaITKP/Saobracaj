using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Saobracaj.Agenti_jobs
{
    internal static partial class RaspustanjeVoza
    {
        // Brise izabranu deonicu i sve njene nastavke u istoj transakciji.
        internal static HashSet<long> ObrisiDeonicu(SqlConnection c, SqlTransaction tx, long id, bool resetParent)
        {
            var rows = Db.Table(c, tx, @";WITH lanac AS (
 SELECT ID,ParentTrainID,PokretanjeID,CompletionRecorded FROM dbo.OperativniPlanVozovi WITH(UPDLOCK,HOLDLOCK) WHERE ID=@id
 UNION ALL SELECT v.ID,v.ParentTrainID,v.PokretanjeID,v.CompletionRecorded FROM dbo.OperativniPlanVozovi v WITH(UPDLOCK,HOLDLOCK) JOIN lanac l ON v.ParentTrainID=l.ID)
 SELECT * FROM lanac OPTION(MAXRECURSION 32767)", "@id", id);
            var ids = new HashSet<long>(rows.Rows.Cast<DataRow>().Select(r => Convert.ToInt64(r["ID"])));
            if (ids.Count == 0) return ids;
            if (rows.Rows.Cast<DataRow>().Any(r => Convert.ToBoolean(r["CompletionRecorded"])))
                throw new InvalidOperationException("Evidentirana voznja se ne brise sa aktivnog spiska.");
            object parent = rows.Rows.Cast<DataRow>().First(r => Convert.ToInt64(r["ID"]) == id)["ParentTrainID"];
            // Jedan DELETE obuhvata ceo podlanac radi samoreferentnih FK veza.
            string list = string.Join(",", ids.Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            using (var cmd = Db.Command(c, tx, "DELETE p FROM dbo.OperativniPlanPokretanja p JOIN dbo.OperativniPlanVozovi v ON v.PokretanjeID=p.ID WHERE v.ID IN (" + list + "); DELETE dbo.OperativniPlanVozovi WHERE ID IN (" + list + ")")) cmd.ExecuteNonQuery();
            if (resetParent && parent != DBNull.Value && !ids.Contains(Convert.ToInt64(parent)))
                using (var cmd = Db.Command(c, tx, "UPDATE dbo.OperativniPlanVozovi SET Dissolved=0,DissolveStation=NULL,DissolvedAtLocal=NULL,IzmenjenoUtc=GETUTCDATE() WHERE ID=@id", "@id", parent)) cmd.ExecuteNonQuery();
            return ids;
        }

        internal static void PosleCuvanja(SqlConnection c, SqlTransaction tx, DataRow r)
        {
            long id = Convert.ToInt64(r["ID"]);
            if (Podred(r) && Convert.ToBoolean(r["Started"]))
            {
                var parent = Db.Table(c, tx, "SELECT DissolvedAtLocal FROM dbo.OperativniPlanVozovi WITH(HOLDLOCK) WHERE ID=@id", "@id", r["ParentTrainID"]);
                if (parent.Rows.Count != 1 || (!parent.Rows[0].IsNull("DissolvedAtLocal") && Convert.ToDateTime(r["StartedAtLocal"]) < Convert.ToDateTime(parent.Rows[0]["DissolvedAtLocal"])))
                    throw new InvalidOperationException("Polazak podreda mora biti nakon prethodnog raspustanja.");
            }
            var children = Db.Table(c, tx, "SELECT * FROM dbo.OperativniPlanVozovi WITH(UPDLOCK,HOLDLOCK) WHERE ParentTrainID=@id", "@id", id);
            if (!Convert.ToBoolean(r["Dissolved"]))
            {
                if (children.Rows.Count == 0) return;
                ObrisiDeonicu(c, tx, Convert.ToInt64(children.Rows[0]["ID"]), false);
                return;
            }
            if (children.Rows.Count > 1) throw new InvalidOperationException("Postoji vise nastavaka iste deonice.");
            if (children.Rows.Count == 1)
            {
                var child = children.Rows[0];
                bool stationChanged = !Equals(r["DissolveStation"], r["DissolveStation", DataRowVersion.Original]);
                if (Convert.ToBoolean(child["Started"]))
                {
                    if (stationChanged) throw new InvalidOperationException("Stanica prethodnog raspustanja se ne menja nakon pokretanja podreda.");
                    if (!r.IsNull("DissolvedAtLocal") && (child.IsNull("StartedAtLocal") || Convert.ToDateTime(child["StartedAtLocal"]) < Convert.ToDateTime(r["DissolvedAtLocal"])))
                        throw new InvalidOperationException("Podred ne moze krenuti pre prethodnog raspustanja.");
                }
                else
                {
                    bool destinationChanged = !Equals(r["To"], r["To", DataRowVersion.Original]) &&
                        Db.Text(child, "To") == Convert.ToString(r["To", DataRowVersion.Original]);
                    if (stationChanged || destinationChanged)
                        using (var cmd = Db.Command(c, tx, "UPDATE dbo.OperativniPlanVozovi SET [From]=@od,[To]=@do,IzmenjenoUtc=GETUTCDATE() WHERE ID=@id",
                            "@od", stationChanged ? Db.Text(r, "DissolveStation").Trim() : Db.Text(child, "From"),
                            "@do", destinationChanged ? Db.Text(r, "To") : Db.Text(child, "To"), "@id", child["ID"])) cmd.ExecuteNonQuery();
                }
                return;
            }
            using (var cmd = Db.Command(c, tx, @"INSERT dbo.OperativniPlanVozovi
(Train,OriginalTrain,RouteID,[From],[To],Weight,MaxWeight,Price,LocomotiveNumber,Note,
AnnouncementNumber,AnnouncementFullId,AnnouncementPayer,AnnouncementWagons,IsAdHoc,Rezi,CreatedBy,
CenovnikDokumentID,CenovnikVaziOd,CenovnikVaziDo,CenovnikGodina,RootTrainID,ParentTrainID,SegmentNo)
SELECT Train,OriginalTrain,RouteID,@od,[To],Weight,MaxWeight,Price,NULL,Note,
AnnouncementNumber,AnnouncementFullId,AnnouncementPayer,AnnouncementWagons,IsAdHoc,Rezi,@user,
CenovnikDokumentID,CenovnikVaziOd,CenovnikVaziDo,CenovnikGodina,@root,ID,SegmentNo+1
FROM dbo.OperativniPlanVozovi WHERE ID=@id AND CompletionRecorded=0",
                "@od", Db.Text(r, "DissolveStation").Trim(), "@user", OperationalSession.UserName, "@root", Glavni(r), "@id", id))
                if (cmd.ExecuteNonQuery() != 1) throw new DBConcurrencyException("Voz vise nije aktivan.");
            using (var reset = Db.Command(c, tx, "UPDATE dbo.OperativniPlanVozovi SET Completed=0,CompletedAtLocal=NULL,IzmenjenoUtc=GETUTCDATE() WHERE ID=@root AND Completed=1 AND CompletionRecorded=0", "@root", Glavni(r))) reset.ExecuteNonQuery();
        }

    }
}
