using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Saobracaj.Agenti_jobs
{
    public sealed class RouteInfo
    {
        public long ID { get; set; }
        public long DocumentID { get; set; }
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public string Train { get; set; } = "";
        public decimal? LengthValue { get; set; }
        public decimal? BrakePercent { get; set; }
        public string Relation { get; set; } = "";
        public decimal? MaxWeight { get; set; }
        public decimal? Price { get; set; }
        public string Departure { get; set; } = "";
        public string Arrival { get; set; } = "";
        public string Days { get; set; } = "";
        public string Locomotive { get; set; } = "";
        public string Length { get; set; } = "";
        public string Note { get; set; } = "";
    }

    public sealed class PlannedTrain
    {
        public long ID { get; set; }
        public string Train { get; set; } = "";
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public decimal Weight { get; set; }
        public decimal? MaxWeight { get; set; }
        public decimal? Price { get; set; }
        public string Note { get; set; } = "";
        public string LocomotiveNumber { get; set; } = "";
        public string AnnouncementNumber { get; set; } = "";
        public bool Rezi { get; set; }
        public bool IsAdHoc { get; set; }
        public bool Started { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? StartedAtLocal { get; set; }
        public bool Completed { get; set; }
        public DateTime? CompletedAtLocal { get; set; }
        public bool Stopped { get; set; }
        public string StopStation { get; set; } = "";
    }

    public static class StationNames
    {
        public static string Normalize(string value)
        {
            var s = (value ?? "").ToLowerInvariant().Replace("đ", "dj");
            var b = new StringBuilder();
            foreach (var ch in s.Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    b.Append(ch);
            return Regex.Replace(b.ToString(), @"[^a-z0-9]+", " ").Trim();
        }

        public static string OperationalName(string s)
        {
            return Regex.Replace(s ?? "", @"^\([^)]*\)\s*[-–]?\s*", "").Trim();
        }

        public static bool Match(string a, string b)
        {
            return Normalize(a) == Normalize(b) || Normalize(OperationalName(a)) == Normalize(OperationalName(b));
        }
    }

    public static class RouteRules
    {
        public static List<RouteInfo> Suggest(IEnumerable<RouteInfo> routes, string from, string to, decimal weight)
        {
            if (weight <= 0)
                throw new InvalidOperationException("Unesite težinu veću od nule.");
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
                throw new InvalidOperationException("Unesite otpravnu i uputnu stanicu.");
            return routes.Where(r => PretragaStanica.Poklapa(r.From, from) &&
                PretragaStanica.Poklapa(r.To, to) && r.MaxWeight.HasValue &&
                r.MaxWeight.Value >= weight).OrderBy(r => r.Price ?? decimal.MaxValue).ToList();
        }

        public static DateTime Belgrade(DateTime utc)
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time"));
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById("Europe/Belgrade"));
            }
        }

        public static string Allocate(string train, IEnumerable<PlannedTrain> rows, DateTime utc)
        {
            // Pokrenuta vožnja ostaje u evidenciji, ali više ne rezerviše broj.
            var used = new HashSet<string>(rows.Where(r => !r.Started).Select(r => r.Train));
            if (!used.Contains(train))
                return train;
            if (train.Length > 0 && (train[0] == '5' || train[0] == '7'))
            {
                var second = (train[0] == '5' ? "6" : "8") + train.Substring(1);
                if (!used.Contains(second))
                    return second;
            }

            throw new InvalidOperationException("Broj trase je zauzet (uključujući moguću zamenu 5→6 ili 7→8). Broj se oslobađa čim se voz označi kao pokrenut.");
        }

        public static string[] Origins(IEnumerable<RouteInfo> routes, string destination)
        {
            return Names(Filter(routes, destination, r => r.To).Select(r => r.From));
        }

        public static string[] Destinations(IEnumerable<RouteInfo> routes, string origin)
        {
            return Names(Filter(routes, origin, r => r.From).Select(r => r.To));
        }


        public static string[] OriginSuggestions(IEnumerable<RouteInfo> routes, string destination, string search)
        {
            return Origins(routes, destination)
                .Where(name => string.IsNullOrWhiteSpace(search) || PretragaStanica.Poklapa(name, search)).ToArray();
        }

        public static string[] DestinationSuggestions(IEnumerable<RouteInfo> routes, string origin, string search)
        {
            return Destinations(routes, origin)
                .Where(name => string.IsNullOrWhiteSpace(search) || PretragaStanica.Poklapa(name, search)).ToArray();
        }

        private static IEnumerable<RouteInfo> Filter(IEnumerable<RouteInfo> routes, string query, Func<RouteInfo, string> get)
        {
            if (string.IsNullOrWhiteSpace(query))
                return routes;
            return routes.Where(r => PretragaStanica.Poklapa(get(r), query));
        }

        private static string[] Names(IEnumerable<string> names)
        {
            return names.Select(StationNames.OperationalName).GroupBy(StationNames.Normalize).Select(g => g.First()).OrderBy(x => x).ToArray();
        }
    }

    public static class TrainText
    {
        // Removes only the previously inserted prefix; manually edited notes remain intact.
        public static string WithWagons(string note, string previous, string wagons)
        {
            string text = (note ?? "").Trim();
            string old = (previous ?? "").Trim();
            if (old.Length > 0 && (text.Equals(old, StringComparison.OrdinalIgnoreCase) ||
                (text.StartsWith(old, StringComparison.OrdinalIgnoreCase) &&
                 text.Length > old.Length && char.IsWhiteSpace(text[old.Length]))))
                text = text.Substring(old.Length).TrimStart();
            string prefix = (wagons ?? "").Trim();
            if (prefix.Length == 0) return text;
            if (text.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                 text.Length > prefix.Length && char.IsWhiteSpace(text[prefix.Length])))
                return text;
            return prefix + (text.Length == 0 ? "" : " " + text);
        }

        public static string MailNote(string note, string announcement)
        {
            string text = (note ?? "").Trim().Replace("\r\n", "\n").Replace("\r", "\n");
            string id = (announcement ?? "").Trim();
            if (id.Length == 0) return text;
            int end = text.IndexOf('\n');
            string first = end < 0 ? text : text.Substring(0, end);
            if (!Regex.IsMatch(first, @"\bID\s+" + Regex.Escape(id) + @"\b", RegexOptions.IgnoreCase))
                first = (first.Length == 0 ? "" : first + " ") + "ID " + id;
            return first + (end < 0 ? "" : text.Substring(end));
        }

        public static DateTime? ParseCompletionTime(string value, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            DateTime time;
            if (DateTime.TryParseExact(value.Trim(), new[] { "H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss", "HHmm" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
                return date.Date.Add(time.TimeOfDay);
            return ParseLocalDate(value);
        }

        public static DateTime? ParseLocalDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            DateTime date;
            if (DateTime.TryParseExact(value, new[] { "dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy HH:mm" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out date) ||
                DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
                return date;
            throw new FormatException("Neispravan datum i vreme. Koristite dd.MM.yyyy HH:mm.");
        }

        public static string[] AnnouncementYears(DateTime now)
        {
            return new[] { now.AddYears(-1).ToString("yy", CultureInfo.InvariantCulture),
                now.ToString("yy", CultureInfo.InvariantCulture) };
        }
    }
}
