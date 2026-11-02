using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Saobracaj.Agenti_jobs
{
    public sealed class GeographicPlace
    {
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string GeoNamesId { get; set; }
        public string Country { get; set; }
        public string Kind { get; set; }
    }

    public static class StationGeography
    {
        private static readonly Lazy<Dictionary<string, GeographicPlace>> Places = new Lazy<Dictionary<string, GeographicPlace>>(() =>
        {
            var assembly = typeof(StationGeography).Assembly;
            Stream stream = assembly.GetManifestResourceStream("Saobracaj.Agenti_jobs.Geography.json");
            if (stream == null)
            {
                // Podržava i ime koje Visual Studio daje resursu u Data podfolderu.
                var resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith(".Geography.json", StringComparison.OrdinalIgnoreCase));
                if (resourceName != null)
                    stream = assembly.GetManifestResourceStream(resourceName);
            }
            if (stream == null)
            {
                var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Geography.json");
                if (!File.Exists(file))
                    file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Agenti jobs", "Data", "Geography.json");
                if (!File.Exists(file))
                    throw new FileNotFoundException("Nedostaje Geography.json. Uključite Data/Geography.json kao Embedded Resource ili ga kopirajte pored aplikacije u Data folder.");
                stream = File.OpenRead(file);
            }

            using (stream)
            using (var reader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(reader))
            {
                var serializer = new JsonSerializer();
                var places = serializer.Deserialize<Dictionary<string, GeographicPlace>>(jsonReader);
                if (places == null || places.Count == 0)
                    throw new InvalidDataException("Geography.json ne sadrži podatke o stanicama.");
                return places;
            }
        });
        public static GeographicPlace Find(string station)
        {
            var name = StationNames.Normalize(station);
            if (name == "")
                return null;
            GeographicPlace exact;
            if (Places.Value.TryGetValue(name, out exact))
                return exact;
            var operational = StationNames.Normalize(StationNames.OperationalName(station));
            if (Places.Value.TryGetValue(operational, out exact))
                return exact;
            var suffix = Regex.Replace(operational, @"\s+(?:teretna|ranzirna|ranz|ran|glavna|gl|fabrika|fab|putnicka|ter|luka)(?:\s+.*)?$", "");
            if (Places.Value.TryGetValue(suffix, out exact))
                return exact;
            var words = operational.Split(' ');
            var matches = new List<KeyValuePair<int, GeographicPlace>>();
            for (int start = 0; start < words.Length; start++)
                for (int len = Math.Min(5, words.Length - start); len > 0; len--)
                {
                    if (Places.Value.TryGetValue(string.Join(" ", words.Skip(start).Take(len)), out exact))
                    {
                        matches.Add(new KeyValuePair<int, GeographicPlace>(len, exact));
                        break;
                    }
                }

            return matches.OrderByDescending(x => x.Key).ThenByDescending(x => x.Value.Latitude).Select(x => x.Value).FirstOrDefault();
        }
    }

    public sealed class EmailResult
    {
        public string Text;
        public List<string> Unknown;
    }

    public static class DispatchEmail
    {
        public static EmailResult Generate(IEnumerable<PlannedTrain> trains)
        {
            var unknown = new List<string>();
            var sorted = trains.Select((train, index) =>
            {
                var from = StationGeography.Find(train.From);
                var to = StationGeography.Find(train.To);
                if (from == null)
                    unknown.Add(train.From);
                if (to == null)
                    unknown.Add(train.To);
                return new
                {
                    Train = train,
                    Index = index,
                    Unknown = from == null || to == null,
                    North = Math.Max(from == null ? double.MinValue : from.Latitude, to == null ? double.MinValue : to.Latitude),
                    South = Math.Min(from == null ? double.MinValue : from.Latitude, to == null ? double.MinValue : to.Latitude)
                };
            }).OrderBy(x => x.Unknown).ThenByDescending(x => x.North).ThenByDescending(x => x.South).ThenBy(x => x.Index).ToList();
            var text = new System.Text.StringBuilder("Saobraća:\r\n\r\n");
            Action<PlannedTrain> block = r =>
            {
                text.Append(r.Train).Append(' ').Append(r.From).Append(" – ").Append(r.To);
                if (r.Started && !r.Stopped && !r.Completed && !r.Dissolved)
                    text.Append(" (u putu)");
                if (r.Dissolved) text.Append(" (raspusten)");
                text.AppendLine();
                string note = r.Rezi
                    ? TrainText.ReziNapomena(r.Note, r.AnnouncementWagons, r.AnnouncementNumber, r.AnnouncementFullId)
                    : TrainText.MailNote(r.Note, r.AnnouncementNumber);
                if (!string.IsNullOrWhiteSpace(note))
                    text.AppendLine(note.Replace("\n", Environment.NewLine));
                text.Append(r.Rezi ? "Reži lok. " : "lok. ").AppendLine(string.IsNullOrWhiteSpace(r.LocomotiveNumber) ? "[unesite broj lokomotive]" : r.LocomotiveNumber.Trim());
                if (r.IsAdHoc)
                    text.AppendLine("AD HOC");
                if (!string.IsNullOrWhiteSpace(r.StopStation))
                    text.Append("Voz je potrebno privremeno zaustaviti u stanici ").Append(r.StopStation.Trim().TrimEnd('.')).AppendLine(".");
                text.AppendLine();
            };
            foreach (var r in sorted.Where(x => !x.Train.Stopped))
                block(r.Train);
            if (sorted.Any(x => x.Train.Stopped))
            {
                text.AppendLine("Izostaje:");
                text.AppendLine();
                foreach (var r in sorted.Where(x => x.Train.Stopped))
                    block(r.Train);
            }

            return new EmailResult
            {
                Text = text.ToString(),
                Unknown = unknown.Distinct().ToList()
            };
        }
    }
}
