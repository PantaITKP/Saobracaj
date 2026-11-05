using System;
using System.Collections.Generic;
using System.Linq;
using Saobracaj.Planiranje.Modeli;

namespace Saobracaj.Planiranje.Pravila
{
    internal sealed class DeonicaPruge
    {
        internal int VezaID;
        internal int A;
        internal int B;
        internal int? PrugaID;
        internal string Sifra;
        internal string Naziv;
        internal string Kategorija;
        internal string OdNaziv;
        internal string DoNaziv;
        internal decimal Km;
        internal bool? Elektrificirana;
        internal string Klasa;
        internal int? Vmax;
        internal bool KiM;
    }

    internal sealed class ParUpotrebe
    {
        internal int? OdID;
        internal int? DoID;
        internal int Broj;
    }

    internal sealed class RedPruge
    {
        internal int PrugaID;
        internal string Sifra;
        internal string Naziv;
        internal string Kategorija;
        internal decimal Km;
        internal decimal KmElektro;
        internal decimal KmBezMreze;
        internal decimal KmNepoznato;
        internal int Trase;
        internal int Najave;

        internal bool Koristi { get { return Trase > 0 || Najave > 0; } }
        internal string Elektrifikacija { get { return PregledPruga.Elektrifikacija(KmElektro, KmBezMreze, KmNepoznato); } }
    }

    internal sealed class PregledPrugaRezultat
    {
        internal List<RedPruge> Redovi = new List<RedPruge>();
        internal int TraseBezRute;
        internal int NajaveBezRute;
    }

    // Koje pruge koriste trase i najave. Ni trase ni najave nemaju upisan put, pa se uzima najkraća ruta po mreži.
    // Elektrifikacija se čita po deonici, jer pruga može biti elektrificirana samo delom (OP_019).
    internal static class PregledPruga
    {
        internal static string Elektrifikacija(decimal elektro, decimal bezMreze, decimal nepoznato)
        {
            string deo = nepoznato > 0 ? ", deo nepoznat" : "";
            if (elektro > 0 && bezMreze > 0) return "Delimično" + deo;
            if (elektro > 0) return "Da" + deo;
            if (bezMreze > 0) return "Ne" + deo;
            return "Nepoznato";
        }

        internal static PregledPrugaRezultat Izracunaj(IList<DeonicaPruge> deonice, IEnumerable<ParUpotrebe> trase, IEnumerable<ParUpotrebe> najave)
        {
            var rezultat = new PregledPrugaRezultat();
            var redovi = new Dictionary<int, RedPruge>();
            foreach (DeonicaPruge d in deonice)
            {
                if (!d.PrugaID.HasValue) continue;
                RedPruge r;
                if (!redovi.TryGetValue(d.PrugaID.Value, out r))
                    redovi[d.PrugaID.Value] = r = new RedPruge { PrugaID = d.PrugaID.Value, Sifra = d.Sifra, Naziv = d.Naziv, Kategorija = d.Kategorija };
                r.Km += d.Km;
                if (d.Elektrificirana == true) r.KmElektro += d.Km;
                else if (d.Elektrificirana == false) r.KmBezMreze += d.Km;
                else r.KmNepoznato += d.Km;
            }

            // KiM ulazi samo ako drugog puta nema, isto kao na ruti.
            Graf bezKim = new Graf(deonice.Where(d => !d.KiM).Select(UVezu).ToList());
            Graf sve = new Graf(deonice.Select(UVezu).ToList());
            var pruge = new Dictionary<string, List<int>>();
            Func<ParUpotrebe, List<int>> prugeZa = p =>
            {
                if (!p.OdID.HasValue || !p.DoID.HasValue || p.OdID == p.DoID) return null;
                string kljuc = Math.Min(p.OdID.Value, p.DoID.Value) + "|" + Math.Max(p.OdID.Value, p.DoID.Value);
                List<int> lista;
                if (pruge.TryGetValue(kljuc, out lista)) return lista;
                List<DeonicaRute> ruta = bezKim.Najkraca(p.OdID.Value, p.DoID.Value) ?? sve.Najkraca(p.OdID.Value, p.DoID.Value);
                lista = ruta == null || ruta.Count == 0 ? null : ruta.Where(d => d.PrugaID.HasValue).Select(d => d.PrugaID.Value).Distinct().ToList();
                pruge[kljuc] = lista;
                return lista;
            };
            foreach (ParUpotrebe p in trase)
            {
                List<int> lista = prugeZa(p);
                if (lista == null) { rezultat.TraseBezRute += p.Broj; continue; }
                foreach (int id in lista) redovi[id].Trase += p.Broj;
            }
            foreach (ParUpotrebe p in najave)
            {
                List<int> lista = prugeZa(p);
                if (lista == null) { rezultat.NajaveBezRute += p.Broj; continue; }
                foreach (int id in lista) redovi[id].Najave += p.Broj;
            }
            rezultat.Redovi = redovi.Values.OrderByDescending(r => r.Najave).ThenByDescending(r => r.Trase).ThenBy(r => r.Sifra).ToList();
            return rezultat;
        }

        private static VezaMreze UVezu(DeonicaPruge d)
        {
            return new VezaMreze { ID = d.VezaID, A = d.A, B = d.B, Km = d.Km, PrugaID = d.PrugaID, Elektrificirana = d.Elektrificirana, KiM = d.KiM };
        }

        // Pruge redom kojim ih ruta prolazi.
        internal static string TekstRute(RezultatRute ruta, IList<DeonicaPruge> deonice)
        {
            Dictionary<int, DeonicaPruge> poVezi = deonice.ToDictionary(d => d.VezaID);
            var delovi = new List<RedPruge>();
            foreach (DeonicaRute d in ruta.Deonice)
            {
                DeonicaPruge dp;
                if (!poVezi.TryGetValue(d.VezaID, out dp)) continue;
                RedPruge r = delovi.LastOrDefault();
                if (r == null || r.PrugaID != (dp.PrugaID ?? 0))
                    delovi.Add(r = new RedPruge { PrugaID = dp.PrugaID ?? 0, Sifra = dp.Sifra, Naziv = dp.Naziv });
                r.Km += dp.Km;
                if (dp.Elektrificirana == true) r.KmElektro += dp.Km;
                else if (dp.Elektrificirana == false) r.KmBezMreze += dp.Km;
                else r.KmNepoznato += dp.Km;
            }
            return "PRUGE NA RUTI:" + Environment.NewLine + string.Join(Environment.NewLine,
                delovi.Select(r => "• " + r.Sifra + " " + r.Naziv + ": " + r.Km.ToString("N1") + " km, elektrificirana: " + r.Elektrifikacija));
        }
    }
}
