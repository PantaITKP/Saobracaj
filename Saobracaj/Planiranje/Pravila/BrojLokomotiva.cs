using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Saobracaj.Planiranje.Modeli;

namespace Saobracaj.Planiranje.Pravila
{
    // Pravilo 8.5: najveća masa po sloju i opsegu, a za rutu važi najmanja vrednost.
    internal static class BrojLokomotiva
    {
        private static readonly string[] Slojevi = { "RUCNA", "FIZIKA", "NAUCENA" };

        internal static decimal? MaxMasa(PlanKontekst k, int serijaId, List<DeonicaRute> ruta, int brojLokomotiva, DateTime datum)
        {
            List<RedMase> redovi = k.Tablica
                .Where(r => r.SerijaID == serijaId && r.BrojLokomotiva == brojLokomotiva
                    && (!r.VaziOd.HasValue || r.VaziOd.Value.Date <= datum.Date)
                    && (!r.VaziDo.HasValue || datum.Date < r.VaziDo.Value.Date))
                .ToList();
            if (redovi.Count == 0) return null;
            decimal? najmanja = null;
            for (int i = 0; i < ruta.Count; i++)
            {
                decimal? v = ZaDeonicu(k, redovi, ruta, i);
                if (v.HasValue && (!najmanja.HasValue || v.Value < najmanja.Value)) najmanja = v;
            }
            return najmanja;
        }

        private static decimal? ZaDeonicu(PlanKontekst k, List<RedMase> redovi, List<DeonicaRute> ruta, int i)
        {
            DeonicaRute d = ruta[i];
            string smer = k.Smer(d);
            foreach (string sloj in Slojevi)
            {
                List<RedMase> uSloju = redovi.Where(r => r.Sloj == sloj).ToList();
                if (uSloju.Count == 0) continue;
                List<RedMase> relacija = uSloju.Where(r => r.StanicaOdID.HasValue && r.StanicaDoID.HasValue && RelacijaPokriva(r, ruta, i)).ToList();
                if (relacija.Count > 0) return relacija.Min(r => r.MaxMasaT);
                if (!d.PrugaID.HasValue) continue;
                List<RedMase> naPruzi = uSloju.Where(r => r.PrugaID == d.PrugaID && !r.StanicaOdID.HasValue).ToList();
                List<RedMase> saSmerom = naPruzi.Where(r => r.Smer != null && r.Smer == smer).ToList();
                if (saSmerom.Count > 0) return saSmerom.Min(r => r.MaxMasaT);
                List<RedMase> bezSmera = naPruzi.Where(r => r.Smer == null).ToList();
                if (bezSmera.Count > 0) return bezSmera.Min(r => r.MaxMasaT);
            }
            return null;
        }

        // Relacija sa smerom BA važi kad se vozi od njene krajnje ka početnoj stanici.
        private static bool RelacijaPokriva(RedMase r, List<DeonicaRute> ruta, int i)
        {
            int a = MestoNaRuti(ruta, r.StanicaOdID.Value), b = MestoNaRuti(ruta, r.StanicaDoID.Value);
            if (a < 0 || b < 0 || a == b) return false;
            if (r.Smer == "BA") return b <= i && i < a;
            if (r.Smer == "AB") return a <= i && i < b;
            return Math.Min(a, b) <= i && i < Math.Max(a, b);
        }

        private static int MestoNaRuti(List<DeonicaRute> ruta, int stanica)
        {
            for (int i = 0; i < ruta.Count; i++)
                if (ruta[i].OdID == stanica) return i;
            return ruta.Count > 0 && ruta[ruta.Count - 1].DoID == stanica ? ruta.Count : -1;
        }

        internal static MasaZaSeriju Odredi(PlanKontekst k, decimal bruto, LokomotivaOP uzorak, List<DeonicaRute> ruta, DateTime datum)
        {
            int serija = uzorak.SerijaID ?? 0;
            decimal masaLok = k.Parametar("MASA_LOK_U_VUCI", datum, null, null, serija, 0) == 1 ? uzorak.MasaT ?? 0 : 0;
            var m = new MasaZaSeriju { SerijaID = uzorak.SerijaID, Bruto = bruto };
            m.MaxJedna = MaxMasa(k, serija, ruta, 1, datum);
            m.MaxDve = MaxMasa(k, serija, ruta, 2, datum);
            if (!m.MaxJedna.HasValue)
            {
                m.Broj = 1;
                m.BezTablice = true;
                m.Tekst = "Za seriju " + uzorak.Serija + " nema tablice masa na ovoj ruti.";
                return m;
            }
            if (bruto + masaLok <= m.MaxJedna.Value)
            {
                m.Broj = 1;
                m.Tekst = "Jedna lokomotiva je dovoljna: " + T(bruto + masaLok) + " t od najviše " + T(m.MaxJedna.Value) + " t.";
                return m;
            }
            decimal granica = m.MaxDve ?? 2 * m.MaxJedna.Value;
            m.DveBezTablice = !m.MaxDve.HasValue;
            if (bruto + 2 * masaLok <= granica)
            {
                m.Broj = 2;
                m.Tekst = "Jedna lokomotiva nije dovoljna: " + T(bruto + masaLok) + " t, a tablica za seriju " + uzorak.Serija + " dozvoljava " + T(m.MaxJedna.Value)
                    + " t. Predlog je dvostruka vuča (najviše " + T(granica) + " t)."
                    + (m.DveBezTablice ? " Nema tablice za dvostruku vuču." : "");
                return m;
            }
            m.Broj = 0;
            m.Tekst = "Ni dve lokomotive serije " + uzorak.Serija + " nisu dovoljne: " + T(bruto + 2 * masaLok) + " t, najviše " + T(granica) + " t.";
            return m;
        }

        private static string T(decimal v)
        {
            return v.ToString("#,##0.##", CultureInfo.GetCultureInfo("sr-Latn-RS"));
        }
    }
}
