using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
namespace Saobracaj.Agenti_jobs
{
    internal static partial class RaspustanjeVoza
    {
        internal static bool Podred(DataRow r) { return r.Table.Columns.Contains("ParentTrainID") && !r.IsNull("ParentTrainID"); }
        internal static long Glavni(DataRow r) { return r.Table.Columns.Contains("RootTrainID") && !r.IsNull("RootTrainID") ? Convert.ToInt64(r["RootTrainID"]) : Convert.ToInt64(r["ID"]); }
        internal static void ProveriRed(DataRow r)
        {
            if (Podred(r) && Convert.ToBoolean(r["Completed"]))
                throw new InvalidOperationException("Celokupnu voznju zavrsite i evidentirajte preko glavnog reda.");
            if (!Convert.ToBoolean(r["Dissolved"])) { r["DissolvedAtLocal"] = DBNull.Value; return; }
            if (string.IsNullOrWhiteSpace(Text(r, "DissolveStation")))
                throw new InvalidOperationException("Najpre unesite stanicu raspustanja pa cekirajte Raspusten.");
            // Najavljeno raspustanje nema stvarno vreme pre pokretanja.
            if (Convert.ToBoolean(r["Started"]) && !r.IsNull("StartedAtLocal"))
            {
                if (r.IsNull("DissolvedAtLocal")) r["DissolvedAtLocal"] = DateTime.Now;
                RadniNalogPravila.Trajanje(Convert.ToDateTime(r["StartedAtLocal"]), Convert.ToDateTime(r["DissolvedAtLocal"]));
            }
        }
        internal static DataTable Pripremi(DataTable source)
        {
            var result = source.Clone();
            foreach (var root in source.Rows.Cast<DataRow>().Where(r => !Podred(r) && Convert.ToBoolean(r["Completed"])))
            {
                long id = Convert.ToInt64(root["ID"]);
                var chain = source.Rows.Cast<DataRow>().Where(r => Glavni(r) == id).OrderBy(r => Convert.ToInt32(r["SegmentNo"])).ToList();
                ProveriLanac(chain);
                for (int i = 0; i < chain.Count; i++)
                {
                    result.ImportRow(chain[i]); var row = result.Rows[result.Rows.Count - 1];
                    row["Completed"] = true;
                    row["CompletedAtLocal"] = Convert.ToBoolean(row["Dissolved"]) ? row["DissolvedAtLocal"] : root["CompletedAtLocal"];
                    if (Convert.ToBoolean(row["Dissolved"]))
                    {
                        row["To"] = row["DissolveStation"];
                        string added = "Raspusten u stanici " + Text(row, "DissolveStation").Trim() + " " + Convert.ToDateTime(row["DissolvedAtLocal"]).ToString("dd.MM.yyyy HH:mm:ss") + ".";
                        row["Note"] = Text(row, "Note").Trim() + Environment.NewLine + added;
                    }
                }
            }
            return result;
        }
        internal static void ProveriLanac(IList<DataRow> rows)
        {
            if (rows.Count == 0) throw new InvalidOperationException("Nedostaje glavna voznja.");
            var root = rows[0];
            if (Podred(root)) throw new InvalidOperationException("Nedostaje glavni red.");
            DateTime? previous = null;
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (Convert.ToInt32(r["SegmentNo"]) != i || (i > 0 && Convert.ToInt64(r["ParentTrainID"]) != Convert.ToInt64(rows[i - 1]["ID"])))
                    throw new InvalidOperationException("Redosled podredova nije ispravan.");
                int announcement;
                if (!int.TryParse(Text(r, "AnnouncementFullId"), out announcement) || announcement <= 0)
                    throw new InvalidOperationException("Svaki deo mora biti povezan sa najavom.");
                if (!Convert.ToBoolean(r["Started"]) || r.IsNull("StartedAtLocal"))
                    throw new InvalidOperationException("Najpre pokrenite poslednji podred i unesite lokomotivu/vremena; nijedan podred ne sme ostati nepokrenut.");
                DateTime start = Convert.ToDateTime(r["StartedAtLocal"]);
                if (previous.HasValue && start < previous.Value) throw new InvalidOperationException("Pokretanje podreda je pre prethodnog raspustanja.");
                bool dissolved = Convert.ToBoolean(r["Dissolved"]);
                if ((i < rows.Count - 1) != dissolved) throw new InvalidOperationException("Svako raspustanje mora imati jedan nastavak; poslednji deo ne sme ostati raspusten.");
                if (dissolved && (r.IsNull("DissolvedAtLocal") || string.IsNullOrWhiteSpace(Text(r, "DissolveStation"))))
                    throw new InvalidOperationException("Nedostaju stanica ili vreme raspustanja.");
                if (!dissolved && root.IsNull("CompletedAtLocal")) throw new InvalidOperationException("Nedostaje zavrsetak glavne voznje.");
                DateTime end = Convert.ToDateTime(dissolved ? r["DissolvedAtLocal"] : root["CompletedAtLocal"]);
                RadniNalogPravila.Trajanje(start, end); previous = end;
            }
        }
        private static string Text(DataRow r, string c) { return r.IsNull(c) ? "" : Convert.ToString(r[c]); }
    }
}
