using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Saobracaj.Planiranje.Pravila
{
    internal sealed class KandidatStanice
    {
        internal int ID;
        internal string Naziv;
        internal double? Latitude;
        internal double? Longitude;
    }

    internal sealed class PogodakStanice
    {
        internal KandidatStanice Stanica;
        internal double Ocena;
    }

    // Povezivanje naziva stanica, pravilo 8.13 specifikacije.
    internal static class NaziviStanica
    {
        private const double GranicaDaljineKm = 5;

        internal static string Normalizuj(string naziv)
        {
            if (string.IsNullOrWhiteSpace(naziv)) return "";
            string t = Latinica.IzCirilice(naziv).ToLowerInvariant();
            var sb = new StringBuilder(t.Length);
            foreach (char z in t)
            {
                switch (z)
                {
                    case 'š': sb.Append('s'); break;
                    case 'đ': sb.Append('d'); break;
                    case 'č':
                    case 'ć': sb.Append('c'); break;
                    case 'ž': sb.Append('z'); break;
                    case '-':
                    case '–':
                    case '—': sb.Append(' '); break;
                    default: sb.Append(z); break;
                }
            }
            t = Regex.Replace(sb.ToString(), @"(?<![\p{L}\p{N}])(st|gr)\.?(?![\p{L}\p{N}])", " ");
            return Regex.Replace(t, @"\s+", " ").Trim();
        }

        internal static int Levenshtein(string a, string b)
        {
            var prethodni = new int[b.Length + 1];
            var tekuci = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) prethodni[j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                tekuci[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cena = a[i - 1] == b[j - 1] ? 0 : 1;
                    tekuci[j] = Math.Min(Math.Min(tekuci[j - 1] + 1, prethodni[j] + 1), prethodni[j - 1] + cena);
                }
                var zamena = prethodni; prethodni = tekuci; tekuci = zamena;
            }
            return prethodni[b.Length];
        }

        // Nula znači da kandidat nije sličan po nazivu.
        internal static double Oceni(string naziv, KandidatStanice kandidat, double? lat = null, double? lon = null)
        {
            string a = Normalizuj(naziv), b = Normalizuj(kandidat.Naziv);
            if (a.Length == 0 || b.Length == 0) return 0;

            double ocena;
            if (a == b) ocena = 1.0;
            else if (a.Contains(b) || b.Contains(a)) ocena = 0.8;
            else if (Levenshtein(a, b) <= 2) ocena = 0.6;
            else return 0;

            if (lat.HasValue && lon.HasValue && kandidat.Latitude.HasValue && kandidat.Longitude.HasValue
                && DaljinaKm(lat.Value, lon.Value, kandidat.Latitude.Value, kandidat.Longitude.Value) > GranicaDaljineKm)
                ocena -= 0.3;
            return ocena;
        }

        // Kandidati od najbolje ocene; kod iste ocene prvi je najbliži, ako su koordinate poznate.
        internal static List<PogodakStanice> Pronadji(string naziv, IEnumerable<KandidatStanice> kandidati, double? lat = null, double? lon = null, int koliko = 5)
        {
            return kandidati
                .Select(k => new PogodakStanice { Stanica = k, Ocena = Oceni(naziv, k, lat, lon) })
                .Where(p => p.Ocena > 0)
                .OrderByDescending(p => p.Ocena)
                .ThenBy(p => lat.HasValue && lon.HasValue && p.Stanica.Latitude.HasValue && p.Stanica.Longitude.HasValue
                    ? DaljinaKm(lat.Value, lon.Value, p.Stanica.Latitude.Value, p.Stanica.Longitude.Value)
                    : double.MaxValue)
                .Take(koliko)
                .ToList();
        }

        internal static double DaljinaKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double radijusKm = 6371;
            double dLat = (lat2 - lat1) * Math.PI / 180, dLon = (lon2 - lon1) * Math.PI / 180;
            double h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return 2 * radijusKm * Math.Asin(Math.Min(1, Math.Sqrt(h)));
        }
    }
}
