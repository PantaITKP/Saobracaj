using System;
using System.Collections.Generic;

namespace Saobracaj.Agenti_jobs
{
    public static class PretragaStanica
    {
        // Menja programer. Skraćenica važi samo kada predstavlja ceo unos.
        public static readonly Dictionary<string, string> Skracenice =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "bg", "beograd" },
                { "ns", "novi sad" },
                { "ru", "ruma" }
            };

        // Grupa obuhvata ove početke punih naziva, na granici reči.
        // Tako Beograd ne uključuje npr. Mali Beograd, a Ruma ne uključuje Rumaniju.
        public static readonly Dictionary<string, string[]> Grupe =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "beograd", new[] { "beograd", "novi beograd" } },
                { "novi sad", new[] { "novi sad" } },
                { "ruma", new[] { "ruma" } }
            };

        public static string Prosiri(string unos)
        {
            string key = StationNames.Normalize(StationNames.OperationalName(unos));
            string value;
            return Skracenice.TryGetValue(key, out value)
                ? StationNames.Normalize(StationNames.OperationalName(value)) : key;
        }

        public static bool Poklapa(string nazivStanice, string unos)
        {
            string query = Prosiri(unos);
            if (query.Length == 0) return false;
            string name = StationNames.Normalize(StationNames.OperationalName(nazivStanice));
            string[] prefixes;
            if (Grupe.TryGetValue(query, out prefixes))
            {
                foreach (string prefix in prefixes)
                {
                    string normalized = StationNames.Normalize(StationNames.OperationalName(prefix));
                    if (normalized.Length > 0 &&
                        (name == normalized || name.StartsWith(normalized + " ", StringComparison.Ordinal)))
                        return true;
                }
                return false;
            }
            // Deo konkretnog naziva, bez automatskog odabira stanice ili promene unosa.
            return name.Contains(query);
        }
    }
}
