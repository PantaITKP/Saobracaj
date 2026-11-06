using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Saobracaj.Planiranje.Modeli;

namespace Saobracaj.Planiranje.Pravila
{
    internal sealed class TrasaZaRutu
    {
        internal int ID;
        internal string Broj;
        internal int? OdID;
        internal int? DoID;
        internal string OdNaziv;
        internal string DoNaziv;
        internal decimal? MaxMasaT;
        internal decimal? MaxDuzinaM;
    }

    internal sealed class StanicaZaTrasu
    {
        internal int ID;
        internal string Naziv;
        internal string NazivStari;
        internal string Drzava;
        internal bool GranicniPrelaz;
        internal int? DuplikatOd;
    }

    // Deo bez trase ima Trasa = null.
    internal sealed class DeoKombinacije
    {
        internal TrasaZaRutu Trasa;
        internal int OdID;
        internal int DoID;
        internal decimal Km;
        internal int DrugihTrasa;
    }

    internal sealed class KombinacijaTrasa
    {
        internal RezultatRute Ruta;
        // Redni broj alternativne rute (1 = najkraća dozvoljena); null za lanac trasa preko mreže.
        internal int? RutaRB;
        internal List<DeoKombinacije> Delovi = new List<DeoKombinacije>();
        internal decimal KmBezTrase { get { return Delovi.Where(d => d.Trasa == null).Sum(d => d.Km); } }
        internal int BrojTrasa { get { return Delovi.Count(d => d.Trasa != null); } }
        internal decimal? NajmanjaMasa { get { return Delovi.Where(d => d.Trasa != null && d.Trasa.MaxMasaT.HasValue).Select(d => d.Trasa.MaxMasaT).Min(); } }
        internal decimal? NajmanjaDuzina { get { return Delovi.Where(d => d.Trasa != null && d.Trasa.MaxDuzinaM.HasValue).Select(d => d.Trasa.MaxDuzinaM).Min(); } }
    }

    // Trase koje pokrivaju rutu, u smeru vožnje. Trasa važi ako su joj obe krajnje stanice na ruti, kao u pravilu 8.2.
    // Trase nemaju vremena, pa se presedanje ne proverava.
    internal static class KombinacijeTrasa
    {
        private const int NajviseKombinacija = 2000;

        // Trasa bez povezane stanice se vezuje po tačnom nazivu, a strana stanica po graničnom mestu sa tom prugom.
        internal static void PoveziStanice(IList<TrasaZaRutu> trase, IList<StanicaZaTrasu> stanice)
        {
            var poNazivu = new Dictionary<string, List<int>>();
            var granica = new Dictionary<string, int>();
            var poId = stanice.ToDictionary(s => s.ID);
            foreach (StanicaZaTrasu s in stanice)
            {
                int glavna = s.DuplikatOd ?? s.ID;
                foreach (string naziv in new[] { s.Naziv, s.NazivStari })
                {
                    string n = NaziviStanica.Normalizuj(naziv);
                    if (n.Length == 0) continue;
                    List<int> l;
                    if (!poNazivu.TryGetValue(n, out l)) poNazivu[n] = l = new List<int>();
                    if (!l.Contains(glavna)) l.Add(glavna);
                }
                // Državna granica (Erdut): sused je u zagradi naziva iz Registra.
                Match m = Regex.Match(s.NazivStari ?? s.Naziv ?? "", @"Dr[žz]avna granica \((.+)\)", RegexOptions.IgnoreCase);
                if (s.GranicniPrelaz && m.Success) granica[NaziviStanica.Normalizuj(m.Groups[1].Value.Replace('ӧ', 'o'))] = s.ID;
            }
            foreach (TrasaZaRutu t in trase)
            {
                t.OdID = Stanica(t.OdID, t.OdNaziv, true, poId, poNazivu, granica);
                t.DoID = Stanica(t.DoID, t.DoNaziv, false, poId, poNazivu, granica);
            }
        }

        // Naziv iz reda vožnje: „(Stamora Moravita) - Vršac“ kreće sa granice, „Kraljevo - Stalać“ kreće iz Kraljeva preko Stalaća,
        // a „Novi Sad ranžirna - Subotica teretna“ kao krajnja stanica završava u Subotici.
        private static int? Stanica(int? id, string naziv, bool pocetak, Dictionary<int, StanicaZaTrasu> poId, Dictionary<string, List<int>> poNazivu, Dictionary<string, int> granica)
        {
            StanicaZaTrasu s;
            int g;
            if (id.HasValue)
            {
                bool strana = poId.TryGetValue(id.Value, out s) && !string.Equals((s.Drzava ?? "").Trim(), "SRB", StringComparison.OrdinalIgnoreCase);
                return strana && granica.TryGetValue(NaziviStanica.Normalizuj(s.Naziv), out g) ? g : id;
            }
            string tekst = (naziv ?? "").Replace("\"", "");
            int? cela = PoNazivu(tekst, poId, poNazivu, granica);
            if (cela.HasValue) return cela;

            // Sused u zagradi: granično mesto prema njemu je stvarni kraj dela trase kod nas.
            foreach (Match m in Regex.Matches(tekst, @"\(([^()]*)\)"))
            {
                string[] delovi = Regex.Split(m.Groups[1].Value, @"\s*-\s*");
                string sused = pocetak ? delovi[delovi.Length - 1] : delovi[0];
                if (granica.TryGetValue(NaziviStanica.Normalizuj(sused), out g)) return g;
            }
            string[] mesta = Regex.Split(Regex.Replace(tekst, @"\([^()]*\)", " "), @"\s*-\s*").Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
            if (mesta.Length == 0) return null;
            int? izbor = PoNazivu(pocetak ? mesta[0] : mesta[mesta.Length - 1], poId, poNazivu, granica);
            if (izbor.HasValue) return izbor;
            foreach (string mesto in mesta)
                if (granica.TryGetValue(NaziviStanica.Normalizuj(mesto), out g)) return g;
            return null;
        }

        private static int? PoNazivu(string naziv, Dictionary<int, StanicaZaTrasu> poId, Dictionary<string, List<int>> poNazivu, Dictionary<string, int> granica)
        {
            string n = NaziviStanica.Normalizuj(naziv);
            int g;
            if (granica.TryGetValue(n, out g)) return g;
            List<int> kandidati;
            // Beograd ranžirna bez oznake je u starom šifarniku grupa A.
            if (!poNazivu.TryGetValue(n, out kandidati) && !poNazivu.TryGetValue(n + " a", out kandidati)) return null;
            StanicaZaTrasu s;
            // „Šid“ i „Šid gr.“ imaju isti normalizovan naziv: trasa bez oznake granice ide u stanicu.
            List<int> bezGranice = kandidati.Where(x => poId.TryGetValue(x, out s) && !s.GranicniPrelaz).ToList();
            if (bezGranice.Count == 1) return bezGranice[0];
            return kandidati.Count == 1 ? kandidati[0] : (int?)null;
        }

        // Isti račun za mapu i za ponudu na poslu: trase uz rutu, sa zastojima kao obaveštenjem.
        internal static List<KombinacijaTrasa> SaZastojima(PlanKontekst k, RuteSaZastojima rute, ZahtevRute z, IList<TrasaZaRutu> trase, decimal? brutoT)
        {
            if (rute.Rute.Count == 0) return new List<KombinacijaTrasa>();
            return Izracunaj(rute.Rute, trase, brutoT, z.DuzinaM, 5, rute.Graf, k.Graf, deonice =>
            {
                RezultatRute r = Rutiranje.IzDeonica(k, deonice, z);
                r.Upozorenja.Clear();
                Rutiranje.DodajZastoje(k, r, z);
                return r;
            });
        }

        // mreza: cela mreža bez telegrama. Trasa ide svojim (najkraćim) putem, pa ne važi na delu rute koji je mnogo duži,
        // na primer na obilasku zatvora, jer bi stvarno prošla kroz zatvorenu deonicu.
        internal static List<KombinacijaTrasa> Izracunaj(IEnumerable<RezultatRute> rute, IList<TrasaZaRutu> trase, decimal? brutoT, decimal? duzinaM, int koliko = 5, Graf graf = null, Graf mreza = null, Func<List<DeonicaRute>, RezultatRute> saberi = null)
        {
            List<TrasaZaRutu> dozvoljene = trase.Where(t => t.OdID.HasValue && t.DoID.HasValue
                && (!brutoT.HasValue || !t.MaxMasaT.HasValue || t.MaxMasaT.Value >= brutoT.Value)
                && (!duzinaM.HasValue || !t.MaxDuzinaM.HasValue || t.MaxDuzinaM.Value >= duzinaM.Value)).ToList();
            var prirodno = new Dictionary<int, Dictionary<int, decimal>>();
            Func<int, int, decimal?> duzinaTrase = (od, doS) =>
            {
                if (mreza == null) return null;
                Dictionary<int, decimal> d;
                if (!prirodno.TryGetValue(od, out d)) prirodno[od] = d = mreza.Rastojanja(od);
                decimal km;
                return d.TryGetValue(doS, out km) ? km : (decimal?)null;
            };
            Func<int, int, decimal, bool> svojimPutem = (od, doS, km) =>
            {
                decimal? p = duzinaTrase(od, doS);
                return !p.HasValue || km <= p.Value * NajvecaDuzinaLanca + 1;
            };
            var sve = new List<KombinacijaTrasa>();
            List<RezultatRute> sveRute = rute.ToList();
            for (int i = 0; i < sveRute.Count; i++)
                foreach (KombinacijaTrasa k in ZaRutu(sveRute[i], dozvoljene, svojimPutem)) { k.RutaRB = i + 1; sve.Add(k); }
            if (graf != null && saberi != null && sveRute.Count > 0)
                sve.AddRange(LanciPrekoMreze(sveRute[0], dozvoljene, graf, saberi, svojimPutem));
            List<KombinacijaTrasa> redom = sve
                .OrderBy(k => k.KmBezTrase).ThenBy(k => k.BrojTrasa).ThenByDescending(k => k.NajmanjaMasa ?? 0).ThenBy(k => k.Ruta.Km)
                .GroupBy(Kljuc).Select(g => g.OrderBy(k => k.RutaRB ?? int.MaxValue).First()).ToList();
            // Svaka alternativna ruta ima bar jedan predlog, pa tek onda ostale kombinacije po redu.
            var izbor = sveRute.Select((r, i) => redom.FirstOrDefault(k => k.RutaRB == i + 1)).Where(k => k != null).ToList();
            izbor.AddRange(redom.Where(k => !izbor.Contains(k)).Take(Math.Max(0, koliko - izbor.Count)));
            return izbor
                .OrderBy(k => k.KmBezTrase).ThenBy(k => k.BrojTrasa).ThenByDescending(k => k.NajmanjaMasa ?? 0).ThenBy(k => k.Ruta.Km)
                .ToList();
        }

        // Ista kombinacija po istom putu; dve rute bez trase između istih stanica nisu isto.
        private static string Kljuc(KombinacijaTrasa k)
        {
            return string.Join(",", k.Ruta.Deonice.Select(d => d.VezaID)) + "#" + string.Join("|", k.Delovi.Select(d => d.Trasa != null ? "T" + d.Trasa.ID : d.OdID + "-" + d.DoID));
        }

        // Dozvoljeno produženje u odnosu na najkraću rutu: lanac preko Beograd ranžirne je duži, ali je stvarni put voza.
        private const decimal NajvecaDuzinaLanca = 1.4m;
        private const decimal KaznaBezTrase = 10m;
        private const decimal KaznaPoTrasi = 5m;
        private const int BrojLanaca = 3;

        private sealed class Korak
        {
            internal int Od;
            internal int Do;
            internal TrasaZaRutu Trasa;
            internal decimal Km;
            internal int Drugih;
        }

        // Lanac trasa preko cele mreže, jer trase često idu preko čvorova koje najkraća ruta zaobilazi.
        private static List<KombinacijaTrasa> LanciPrekoMreze(RezultatRute najkraca, List<TrasaZaRutu> trase, Graf graf, Func<List<DeonicaRute>, RezultatRute> saberi, Func<int, int, decimal, bool> svojimPutem)
        {
            int od = najkraca.Mesta[0], cilj = najkraca.Mesta[najkraca.Mesta.Count - 1];
            var rastojanja = new Dictionary<int, Dictionary<int, decimal>>();
            Func<int, Dictionary<int, decimal>> iz = s =>
            {
                Dictionary<int, decimal> d;
                if (!rastojanja.TryGetValue(s, out d)) rastojanja[s] = d = graf.Rastojanja(s);
                return d;
            };
            var koraci = new List<Korak>();
            foreach (var g in trase.Where(t => t.OdID != t.DoID).GroupBy(t => Tuple.Create(t.OdID.Value, t.DoID.Value)))
            {
                decimal km;
                if (!iz(g.Key.Item1).TryGetValue(g.Key.Item2, out km) || !svojimPutem(g.Key.Item1, g.Key.Item2, km)) continue;
                TrasaZaRutu najbolja = g.OrderByDescending(t => t.MaxMasaT ?? 0).ThenByDescending(t => t.MaxDuzinaM ?? 0).ThenBy(t => t.Broj).First();
                koraci.Add(new Korak { Od = g.Key.Item1, Do = g.Key.Item2, Trasa = najbolja, Km = km, Drugih = g.Count() - 1 });
            }
            var cvorovi = new HashSet<int>(koraci.SelectMany(x => new[] { x.Od, x.Do })) { od, cilj };
            var rezultat = new List<KombinacijaTrasa>();
            var iskljucene = new HashSet<int>();
            for (int i = 0; i < BrojLanaca; i++)
            {
                List<Korak> lanac = NajboljiLanac(od, cilj, cvorovi, koraci.Where(x => !iskljucene.Contains(x.Trasa.ID)).ToList(), iz);
                if (lanac == null) break;
                decimal ukupno = lanac.Sum(x => x.Km);
                if (ukupno > najkraca.Km * NajvecaDuzinaLanca) break;
                KombinacijaTrasa k = UKombinaciju(lanac, graf, saberi);
                if (k != null) rezultat.Add(k);
                Korak prvi = lanac.FirstOrDefault(x => x.Trasa != null);
                if (prvi == null) break;
                iskljucene.Add(prvi.Trasa.ID);
            }
            return rezultat;
        }

        private static List<Korak> NajboljiLanac(int od, int cilj, HashSet<int> cvorovi, List<Korak> koraci, Func<int, Dictionary<int, decimal>> iz)
        {
            var cena = new Dictionary<int, decimal> { { od, 0 } };
            var prethodni = new Dictionary<int, Korak>();
            var obidjeni = new HashSet<int>();
            var izCvora = koraci.ToLookup(x => x.Od);
            while (true)
            {
                int? u = null;
                foreach (KeyValuePair<int, decimal> p in cena)
                    if (!obidjeni.Contains(p.Key) && (!u.HasValue || p.Value < cena[u.Value])) u = p.Key;
                if (!u.HasValue) return null;
                if (u.Value == cilj) break;
                obidjeni.Add(u.Value);
                Action<Korak, decimal> probaj = (korak, c) =>
                {
                    decimal stara;
                    if (cena.TryGetValue(korak.Do, out stara) && stara <= c) return;
                    cena[korak.Do] = c;
                    prethodni[korak.Do] = korak;
                };
                foreach (Korak x in izCvora[u.Value]) probaj(x, cena[u.Value] + x.Km + KaznaPoTrasi);
                // Deo bez trase ide do bilo kog drugog čvora, ali je skup.
                foreach (KeyValuePair<int, decimal> p in iz(u.Value))
                    if (p.Key != u.Value && cvorovi.Contains(p.Key) && !obidjeni.Contains(p.Key))
                        probaj(new Korak { Od = u.Value, Do = p.Key, Km = p.Value }, cena[u.Value] + p.Value * KaznaBezTrase);
            }
            var lanac = new List<Korak>();
            for (int s = cilj; s != od; s = prethodni[s].Od) lanac.Add(prethodni[s]);
            lanac.Reverse();
            return lanac;
        }

        private static KombinacijaTrasa UKombinaciju(List<Korak> lanac, Graf graf, Func<List<DeonicaRute>, RezultatRute> saberi)
        {
            var deonice = new List<DeonicaRute>();
            var delovi = new List<DeoKombinacije>();
            foreach (Korak x in lanac)
            {
                List<DeonicaRute> deo = graf.Najkraca(x.Od, x.Do);
                if (deo == null) return null;
                deonice.AddRange(deo);
                delovi.Add(new DeoKombinacije { Trasa = x.Trasa, OdID = x.Od, DoID = x.Do, Km = x.Km, DrugihTrasa = x.Drugih });
            }
            // Voz ne ide do granice da bi se vratio: lanac koji dva puta prolazi kroz istu stanicu se ne nudi.
            if (deonice.Count > 0 && deonice.Select(d => d.DoID).Concat(new[] { deonice[0].OdID }).Distinct().Count() != deonice.Count + 1) return null;
            return new KombinacijaTrasa { Ruta = saberi(deonice), Delovi = delovi };
        }

        private sealed class Segment
        {
            internal int Do;
            internal TrasaZaRutu Trasa;
            internal int Drugih;
        }

        private static List<KombinacijaTrasa> ZaRutu(RezultatRute r, List<TrasaZaRutu> trase, Func<int, int, decimal, bool> svojimPutem)
        {
            int n = r.Mesta.Count;
            var mesto = new Dictionary<int, int>();
            for (int i = 0; i < n; i++) if (!mesto.ContainsKey(r.Mesta[i])) mesto[r.Mesta[i]] = i;
            var km = new decimal[n];
            for (int i = 1; i < n; i++) km[i] = km[i - 1] + r.Deonice[i - 1].Km;

            // Za isti deo rute ostaje trasa sa najvećom masom, a ostale se samo broje.
            var pocetak = new Dictionary<int, List<Segment>>();
            foreach (var g in trase
                .Where(t => mesto.ContainsKey(t.OdID.Value) && mesto.ContainsKey(t.DoID.Value) && mesto[t.OdID.Value] < mesto[t.DoID.Value]
                    && svojimPutem(t.OdID.Value, t.DoID.Value, km[mesto[t.DoID.Value]] - km[mesto[t.OdID.Value]]))
                .GroupBy(t => Tuple.Create(mesto[t.OdID.Value], mesto[t.DoID.Value])))
            {
                TrasaZaRutu najbolja = g.OrderByDescending(t => t.MaxMasaT ?? 0).ThenByDescending(t => t.MaxDuzinaM ?? 0).ThenBy(t => t.Broj).First();
                List<Segment> l;
                if (!pocetak.TryGetValue(g.Key.Item1, out l)) pocetak[g.Key.Item1] = l = new List<Segment>();
                l.Add(new Segment { Do = g.Key.Item2, Trasa = najbolja, Drugih = g.Count() - 1 });
            }

            var rezultat = new List<KombinacijaTrasa>();
            if (n < 2) return rezultat;
            Dalje(0, new List<DeoKombinacije>(), r, km, pocetak, rezultat);
            return rezultat;
        }

        private static void Dalje(int i, List<DeoKombinacije> put, RezultatRute r, decimal[] km, Dictionary<int, List<Segment>> pocetak, List<KombinacijaTrasa> rezultat)
        {
            if (rezultat.Count >= NajviseKombinacija) return;
            int kraj = r.Mesta.Count - 1;
            if (i >= kraj)
            {
                rezultat.Add(new KombinacijaTrasa { Ruta = r, Delovi = new List<DeoKombinacije>(put) });
                return;
            }
            List<Segment> iduci;
            if (pocetak.TryGetValue(i, out iduci))
            {
                foreach (Segment s in iduci.OrderByDescending(x => x.Do))
                {
                    put.Add(new DeoKombinacije { Trasa = s.Trasa, OdID = r.Mesta[i], DoID = r.Mesta[s.Do], Km = km[s.Do] - km[i], DrugihTrasa = s.Drugih });
                    Dalje(s.Do, put, r, km, pocetak, rezultat);
                    put.RemoveAt(put.Count - 1);
                }
                return;
            }
            // Odavde nema trase: deo bez trase ide do prve stanice iz koje trasa kreće.
            int j = i + 1;
            while (j < kraj && !pocetak.ContainsKey(j)) j++;
            put.Add(new DeoKombinacije { OdID = r.Mesta[i], DoID = r.Mesta[j], Km = km[j] - km[i] });
            Dalje(j, put, r, km, pocetak, rezultat);
            put.RemoveAt(put.Count - 1);
        }
    }
}
