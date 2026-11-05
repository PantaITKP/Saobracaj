using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Saobracaj.Agenti_jobs;

namespace Saobracaj.Planiranje.Pravila
{
    internal sealed class RedTrase
    {
        internal string BrojVoza = "";
        internal string PocetnaNaziv = "";
        internal string KrajnjaNaziv = "";
        internal int? PocetnaStanicaID;
        internal int? KrajnjaStanicaID;
        internal string Relacija;
        internal string Rang;
        internal TimeSpan? VremePolaska;
        internal TimeSpan? VremeDolaska;
        internal int DanPlus;
        internal string DaniSaobracaja;
        internal decimal? MaxMasaT;
        internal decimal? MaxDuzinaM;
        internal decimal? ProcenatKocenja;
        internal decimal? Cena;
        internal string SerijaLokomotive;
        internal string Napomena;
        internal string Stanje;
        internal string OpisPromene;
        internal string Greska;
    }

    // Pravilo 8.11: ključ je normalizovan broj voza + početna + krajnja stanica.
    internal static class RazlikeTrasa
    {
        internal static string Kljuc(RedTrase r)
        {
            return (r.BrojVoza ?? "").Replace(" ", "").ToUpperInvariant() + "|"
                + NaziviStanica.Normalizuj(r.PocetnaNaziv) + "|" + NaziviStanica.Normalizuj(r.KrajnjaNaziv);
        }

        // Excel čuva vreme kao "22:30", "22.30" ili kao deo dana (0,9375).
        internal static TimeSpan? ProcitajVreme(string tekst)
        {
            string s = (tekst ?? "").Trim();
            if (s.Length == 0) return null;
            TimeSpan t;
            if (s.Contains(":") && TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out t) && t >= TimeSpan.Zero && t < TimeSpan.FromDays(1))
                return new TimeSpan(t.Hours, t.Minutes, 0);
            decimal d;
            if (!decimal.TryParse(s.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out d) || d < 0) return null;
            if (d < 1)
                return s.Contains(".") || s.Contains(",") ? TimeSpan.FromMinutes(Math.Round((double)d * 1440)) : (TimeSpan?)null;
            int sat = (int)Math.Truncate(d), minut = (int)Math.Round((d - sat) * 100);
            return sat < 24 && minut < 60 ? new TimeSpan(sat, minut, 0) : (TimeSpan?)null;
        }

        internal static RedTrase IzRute(RouteInfo r)
        {
            TimeSpan? polazak = ProcitajVreme(r.Departure), dolazak = ProcitajVreme(r.Arrival);
            return new RedTrase
            {
                BrojVoza = (r.Train ?? "").Trim(),
                PocetnaNaziv = (r.From ?? "").Trim(),
                KrajnjaNaziv = (r.To ?? "").Trim(),
                Relacija = Prazno(r.Relation),
                VremePolaska = polazak,
                VremeDolaska = dolazak,
                DanPlus = polazak.HasValue && dolazak.HasValue && dolazak < polazak ? 1 : 0,
                DaniSaobracaja = Prazno(r.Days),
                MaxMasaT = r.MaxWeight,
                MaxDuzinaM = r.LengthValue,
                ProcenatKocenja = r.BrakePercent,
                Cena = r.Price,
                SerijaLokomotive = Prazno(r.Locomotive),
                Napomena = Prazno(r.Note)
            };
        }

        private static string Prazno(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }

        internal static bool ImaVremena(IEnumerable<RedTrase> redovi)
        {
            return redovi.Any(r => r.VremePolaska.HasValue || r.VremeDolaska.HasValue);
        }

        // Rezultat sadrži nove redove sa stanjem i, na kraju, ukinute redove iz prethodne verzije.
        internal static List<RedTrase> Uporedi(IList<RedTrase> novi, IList<RedTrase> prethodni, IList<KandidatStanice> stanice)
        {
            var poNazivu = stanice.GroupBy(s => NaziviStanica.Normalizuj(s.Naziv)).ToDictionary(g => g.Key, g => g.First());
            var stari = new Dictionary<string, RedTrase>();
            foreach (RedTrase p in prethodni)
                stari[Kljuc(p)] = p;

            // Stvarni red vožnje nema popunjena vremena; greška je samo red kome vreme nedostaje u datoteci koja ih ima.
            bool imaVremena = ImaVremena(novi);
            var vidjeni = new HashSet<string>();
            var rezultat = new List<RedTrase>();
            foreach (RedTrase r in novi)
            {
                string kljuc = Kljuc(r);
                var greske = new List<string>();
                r.PocetnaStanicaID = NadjiStanicu(r.PocetnaNaziv, "Početna", poNazivu, stanice, greske);
                r.KrajnjaStanicaID = NadjiStanicu(r.KrajnjaNaziv, "Krajnja", poNazivu, stanice, greske);
                if (!r.VremePolaska.HasValue || !r.VremeDolaska.HasValue)
                {
                    if (imaVremena) greske.Add("Nedostaje vreme.");
                }
                else if (r.VremePolaska == TimeSpan.Zero && r.VremeDolaska == TimeSpan.Zero)
                    greske.Add("Vreme je 00:00/00:00.");
                if (!vidjeni.Add(kljuc))
                    greske.Add("Duplikat u datoteci.");

                RedTrase star;
                bool imaStaru = stari.TryGetValue(kljuc, out star);
                if (greske.Count > 0)
                {
                    r.Stanje = "GRESKA";
                    r.Greska = string.Join(" ", greske);
                }
                else if (!imaStaru)
                {
                    r.Stanje = "NOVA";
                }
                else
                {
                    string promena = Promene(star, r);
                    r.Stanje = promena.Length > 0 ? "IZMENJENA" : "ISTA";
                    r.OpisPromene = promena.Length > 0 ? promena : null;
                }
                rezultat.Add(r);
            }
            foreach (KeyValuePair<string, RedTrase> s in stari)
            {
                if (vidjeni.Contains(s.Key)) continue;
                s.Value.Stanje = "UKINUTA";
                s.Value.OpisPromene = "Nema je u novoj datoteci.";
                rezultat.Add(s.Value);
            }
            return rezultat;
        }

        private static int? NadjiStanicu(string naziv, string uloga, Dictionary<string, KandidatStanice> poNazivu, IList<KandidatStanice> stanice, List<string> greske)
        {
            KandidatStanice s;
            if (poNazivu.TryGetValue(NaziviStanica.Normalizuj(naziv), out s)) return s.ID;
            PogodakStanice predlog = NaziviStanica.Pronadji(naziv, stanice, koliko: 1).FirstOrDefault();
            greske.Add(uloga + " stanica „" + naziv + "“ nije pronađena." + (predlog != null ? " Predlog: " + predlog.Stanica.Naziv + "." : ""));
            return null;
        }

        private static string Promene(RedTrase s, RedTrase n)
        {
            var d = new List<string>();
            Dodaj(d, "Polazak", Vreme(s.VremePolaska), Vreme(n.VremePolaska));
            Dodaj(d, "Dolazak", Vreme(s.VremeDolaska), Vreme(n.VremeDolaska));
            Dodaj(d, "Max masa", Broj(s.MaxMasaT), Broj(n.MaxMasaT));
            Dodaj(d, "Dužina", Broj(s.MaxDuzinaM), Broj(n.MaxDuzinaM));
            Dodaj(d, "Rang", s.Rang, n.Rang);
            Dodaj(d, "Dani", s.DaniSaobracaja, n.DaniSaobracaja);
            return string.Join("; ", d);
        }

        private static void Dodaj(List<string> lista, string polje, string staro, string novo)
        {
            staro = staro ?? ""; novo = novo ?? "";
            if (staro != novo) lista.Add(polje + " " + (staro.Length == 0 ? "–" : staro) + " → " + (novo.Length == 0 ? "–" : novo));
        }

        private static string Vreme(TimeSpan? t) { return t.HasValue ? t.Value.ToString(@"hh\:mm") : ""; }

        private static string Broj(decimal? v) { return v.HasValue ? v.Value.ToString("0.##", CultureInfo.InvariantCulture) : ""; }
    }
}
