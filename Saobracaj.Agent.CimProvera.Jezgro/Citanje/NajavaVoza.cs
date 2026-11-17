using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Saobracaj.Agent.CimProvera.Citanje
{
    /// <summary>
    /// v2.8.3: najava (ili otkaz) voza – formular „Najava voza / Train announcement / Zuganmeldung“. Nije tovarni list i ne proverava se;
    /// za log se čitaju samo broj voza, otpravna i uputna stanica, broj kola, težina, dužina i ETA.
    /// </summary>
    internal static class NajavaVoza
    {
        internal static readonly string[] Natpisi =
        {
            "najava voza", "train announcement", "zuganmeldung", "otkaz voza", "train cancellation", "zugkundigung", "zugkündigung",
            "наява на влак", "најава воза", "najava vlaka", "avizare tren",
        };

        private static readonly (string polje, string[] natpisi)[] Polja =
        {
            ("voz", new[] { "zugnummer", "train number", "broj voza" }),
            ("od", new[] { "versandbahnhof", "forwarding station", "otpravna stanica" }),
            ("do", new[] { "bestimmungsbahnhof", "station of destination", "uputna stanica" }),
            ("kola", new[] { "anzahl der wagen", "number of wagon", "number of wagons", "broj kola" }),
            ("tezina", new[] { "zug bruto gewicht", "train brutoweight", "train gross weight", "težina voza", "tezina voza" }),
            ("duzina", new[] { "zuglänge", "zuglange", "trainlength", "train length", "dužina voza", "duzina voza" }),
        };

        /// <summary>Vrednost posle poslednjeg natpisa polja u redu („BROJ VOZA   TRAIN NUMBER   ZUGNUMMER   48080“ → „48080“).</summary>
        internal static Dictionary<string, string> Procitaj(string tekst)
        {
            var rez = new Dictionary<string, string>();
            var redovi = (tekst ?? "").Replace("\r", "").Split('\n');
            for (int i = 0; i < redovi.Length; i++)
            {
                var red = redovi[i];
                foreach (var (polje, natpisi) in Polja)
                {
                    if (rez.ContainsKey(polje)) continue;
                    int kraj = -1;
                    foreach (var n in natpisi)
                    {
                        var j = LastIndex(red, n);
                        if (j >= 0) kraj = System.Math.Max(kraj, j + n.Length);
                    }
                    if (kraj < 0) continue;
                    var v = Regex.Replace(red.Substring(kraj), @"\s+", " ").Trim(' ', ':', '-', '–');
                    if (v.Length > 0 && v.Length <= 80) rez[polje] = v;
                }
                if (!rez.ContainsKey("eta") && Regex.IsMatch(red, @"\bETA\b", RegexOptions.IgnoreCase))
                {
                    var okolina = red + " " + (i + 1 < redovi.Length ? redovi[i + 1] : "");
                    var m = Regex.Match(okolina, @"\b\d{1,2}\.\d{1,2}\.\d{4}\.?(?:\s+\d{1,2}:\d{2})?");
                    if (m.Success) rez["eta"] = m.Value.Trim();
                }
            }
            return rez;
        }

        private static int LastIndex(string s, string n) => s.ToLowerInvariant().LastIndexOf(n.ToLowerInvariant(), System.StringComparison.Ordinal);

        /// <summary>„najava voza 48080: Lüleburgaz - 3510445 → Crveni Krst …, 5 kola, 156t, 105 m, ETA 15.11.2026 02:40 (ne proverava se)“.</summary>
        internal static string Opis(string tekst)
        {
            var p = Procitaj(tekst);
            string V(string k) => p.TryGetValue(k, out var v) ? v : null;
            var delovi = new List<string>();
            if (V("od") != null || V("do") != null) delovi.Add((V("od") ?? "?") + " → " + (V("do") ?? "?"));
            if (V("kola") != null) delovi.Add(V("kola") + " kola");
            if (V("tezina") != null) delovi.Add("težina " + V("tezina"));
            if (V("duzina") != null) delovi.Add("dužina " + V("duzina"));
            if (V("eta") != null) delovi.Add("ETA " + V("eta"));
            return "najava voza" + (V("voz") != null ? " " + V("voz") : "") + (delovi.Count > 0 ? ": " + string.Join(", ", delovi) : "") + " (ne proverava se)";
        }
    }
}
