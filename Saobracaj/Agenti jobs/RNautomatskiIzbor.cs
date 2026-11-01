using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
namespace Saobracaj.Agenti_jobs
{
    public static class RNAutomatskiIzbor
    {
        public static int? AutomatskaKolicina(int? ukupnoNajave, IEnumerable<int?> veze, bool rezi)
        {
            if (rezi) return 0;
            var counts = veze.Distinct().ToList();
            return counts.Count == 1 && counts[0].HasValue && counts[0].Value > 0 &&
                ukupnoNajave.HasValue && counts[0].Value == ukupnoNajave.Value ? counts[0] : null;
        }
        public static string OsnovnaStanica(string name)
        {
            string text = (name ?? "").Trim();
            // Najpre se izvan ove metode proverava celo ime u sifarniku.
            return Regex.Split(text, @"\s+[-–—]\s+|\s+(?i:via|preko)\s+", RegexOptions.CultureInvariant)[0].Trim();
        }
        public static List<StanicaCenovnika> Predlozi(DataTable stations, string name)
        {
            string key = StaniceCenovnika.NormalizujStanicu(name);
            return StaniceCenovnika.Stanice(stations, name, int.MaxValue).Where(c =>
            {
                string n = StaniceCenovnika.NormalizujStanicu(c.Opis);
                return n == key || n.StartsWith(key + " ") || key.StartsWith(n + " ") ||
                    (c.Ocena >= .75 && n.Split(' ')[0] == key.Split(' ')[0]);
            }).ToList();
        }
        public static List<string> BrojeviLokomotiva(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            var result = Regex.Split(text.Trim(), @"[,;/+\r\n]+").Select(s => Regex.Replace(s, @"[^0-9]", "")).ToList();
            if (result.Any(s => s.Length < 6 || s.Length > 8))
                throw new InvalidOperationException("Proverite broj lokomotive. Vise lokomotiva razdvojite zarezom.");
            return result.Distinct().ToList();
        }
        public static List<string> Lokomotive(DataTable mesta, string number)
        {
            var exact = mesta.Rows.Cast<DataRow>().Where(r => Regex.Replace(Convert.ToString(r["SmSifra"]), @"[^0-9]", "") == number)
                .Select(r => Convert.ToString(r["SmSifra"]).Trim()).ToList();
            if (exact.Count > 0) return exact;
            if (number.Length != 6) return new List<string>();
            return mesta.Rows.Cast<DataRow>().Where(r => {
                string n = Regex.Replace(Convert.ToString(r["SmSifra"]), @"[^0-9]", "");
                return n.Length == 7 && n.StartsWith(number, StringComparison.Ordinal);
            }).Select(r => Convert.ToString(r["SmSifra"]).Trim()).ToList();
        }
    }
}
