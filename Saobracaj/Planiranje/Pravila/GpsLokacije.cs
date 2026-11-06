using System;
using System.Collections.Generic;
using System.Linq;
using Saobracaj.Planiranje.Modeli;

namespace Saobracaj.Planiranje.Pravila
{
    // GPS pozicija lokomotive kao lokacija za predlog: uparivanje sa šifarnikom i najbliža stanica.
    internal static class GpsLokacije
    {
        private const double PoluprecnikZemljeKm = 6371.0;

        private static string Cifre(string s)
        {
            return new string((s ?? "").Where(char.IsDigit).ToArray());
        }

        // GPS oznaka 647-102 je broj 647102. Sedmocifreni broj u šifarniku ima i kontrolnu cifru (646-001 = 6460010).
        // Oznaka koja odgovara većem broju lokomotiva ostaje neuparena.
        internal static LokomotivaOP Upari(IEnumerable<LokomotivaOP> lokomotive, string oznaka)
        {
            string g = Cifre(oznaka);
            if (g.Length < 6) return null;
            List<LokomotivaOP> nadjene = lokomotive.Where(l =>
            {
                string d = Cifre(l.Broj);
                return d == g || (d.Length == g.Length + 1 && d.StartsWith(g, StringComparison.Ordinal));
            }).Take(2).ToList();
            return nadjene.Count == 1 ? nadjene[0] : null;
        }

        internal static double KmVazdusno(double lat1, double lon1, double lat2, double lon2)
        {
            double r = Math.PI / 180;
            double dLat = (lat2 - lat1) * r, dLon = (lon2 - lon1) * r;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * r) * Math.Cos(lat2 * r) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return 2 * PoluprecnikZemljeKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        }

        // Tačka se vezuje za najbližu stanicu iz „dostupne“ (one koje imaju prugu do prijema), da bi se prazna vožnja mogla izračunati.
        internal static Dictionary<int, GpsLokacija> Povezi(IList<LokomotivaOP> lokomotive, IEnumerable<GpsLokacija> tacke, IDictionary<int, double[]> koordinate, ICollection<int> dostupne)
        {
            var stanice = koordinate.Where(s => dostupne.Contains(s.Key)).ToList();
            var rezultat = new Dictionary<int, GpsLokacija>();
            foreach (GpsLokacija t in tacke)
            {
                LokomotivaOP l = Upari(lokomotive, t.Oznaka);
                if (l == null || rezultat.ContainsKey(l.ID)) continue;
                t.LokomotivaID = l.ID;
                double najmanje = double.MaxValue;
                foreach (KeyValuePair<int, double[]> s in stanice)
                {
                    double km = KmVazdusno(t.Lat, t.Lon, s.Value[0], s.Value[1]);
                    if (km < najmanje) { najmanje = km; t.StanicaID = s.Key; }
                }
                t.KmDoStanice = t.StanicaID.HasValue ? najmanje : (double?)null;
                rezultat[l.ID] = t;
            }
            return rezultat;
        }
    }
}
