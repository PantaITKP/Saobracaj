using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
namespace Saobracaj.Agenti_jobs
{
    public sealed class StanicaCenovnika
    {
        public int ID;
        public string Opis;
        public double Ocena;
    }
    public static class StaniceCenovnika
    {
        public static string NormalizujStanicu(string text)
        {
            string name = StationNames.Normalize(StationNames.OperationalName(text));
            name = Regex.Replace(name, @"\b(ranzirna|ranz|ranzirni)\b", "ran");
            name = Regex.Replace(name, @"\bglavna\b", "gl");
            name = Regex.Replace(name, @"\bteretna\b", "ter");
            return name;
        }
        public static List<StanicaCenovnika> Stanice(DataTable stations, string query, int maksimum = 25)
        {
            string key = NormalizujStanicu(query);
            if (key.Length == 0) return new List<StanicaCenovnika>();
            return stations.Rows.Cast<DataRow>().Where(r => !string.IsNullOrWhiteSpace(Convert.ToString(r["Opis"]))).Select(r =>
            {
                string name = Convert.ToString(r["Opis"]).Trim();
                string normalized = NormalizujStanicu(name);
                double score;
                if (normalized == key) score = 1;
                else if (normalized.Length == 0) score = 0;
                else
                {
                    score = 1.0 - (double)Udaljenost(key, normalized) / Math.Max(key.Length, normalized.Length);
                    if (normalized.Contains(key) || key.Contains(normalized)) score = Math.Max(score, .82);
                    var tokens = key.Split(' ');
                    double common = (double)tokens.Count(t => normalized.Split(' ').Contains(t)) / tokens.Length;
                    score = Math.Max(score, common * .8);
                }
                return new StanicaCenovnika { ID = Convert.ToInt32(r["ID"]), Opis = name, Ocena = score };
            }).OrderByDescending(c => c.Ocena).ThenBy(c => c.Opis).Take(maksimum).ToList();
        }
        private static int Udaljenost(string a, string b)
        {
            var previous = Enumerable.Range(0, b.Length + 1).ToArray();
            for (int i = 1; i <= a.Length; i++)
            {
                var next = new int[b.Length + 1]; next[0] = i;
                for (int j = 1; j <= b.Length; j++)
                    next[j] = Math.Min(Math.Min(next[j - 1] + 1, previous[j] + 1), previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
                previous = next;
            }
            return previous[b.Length];
        }
    }
}
