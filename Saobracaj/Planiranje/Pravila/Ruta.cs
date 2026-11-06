using System;
using System.Collections.Generic;
using System.Linq;
using Saobracaj.Planiranje.Modeli;

namespace Saobracaj.Planiranje.Pravila
{
    // Neusmeren graf mreže (pravilo 8.2).
    internal sealed class Graf
    {
        private readonly Dictionary<int, List<VezaMreze>> susedi = new Dictionary<int, List<VezaMreze>>();

        internal Graf(IEnumerable<VezaMreze> veze)
        {
            foreach (VezaMreze v in veze)
            {
                Dodaj(v.A, v);
                Dodaj(v.B, v);
            }
        }

        private void Dodaj(int stanica, VezaMreze v)
        {
            List<VezaMreze> l;
            if (!susedi.TryGetValue(stanica, out l)) susedi[stanica] = l = new List<VezaMreze>();
            l.Add(v);
        }

        private void Dijkstra(int od, int? cilj, ISet<int> bezVeza, Dictionary<int, decimal> dist, Dictionary<int, VezaMreze> preko)
        {
            var red = new SortedSet<Tuple<decimal, int>>();
            dist[od] = 0;
            red.Add(Tuple.Create(0m, od));
            while (red.Count > 0)
            {
                Tuple<decimal, int> t = red.Min;
                red.Remove(t);
                if (cilj.HasValue && t.Item2 == cilj.Value) return;
                List<VezaMreze> l;
                if (!susedi.TryGetValue(t.Item2, out l)) continue;
                foreach (VezaMreze v in l)
                {
                    if (bezVeza != null && bezVeza.Contains(v.ID)) continue;
                    int drugi = v.A == t.Item2 ? v.B : v.A;
                    decimal nova = t.Item1 + v.Km;
                    decimal stara;
                    if (dist.TryGetValue(drugi, out stara))
                    {
                        if (nova >= stara) continue;
                        red.Remove(Tuple.Create(stara, drugi));
                    }
                    dist[drugi] = nova;
                    preko[drugi] = v;
                    red.Add(Tuple.Create(nova, drugi));
                }
            }
        }

        internal Dictionary<int, decimal> Rastojanja(int od)
        {
            var dist = new Dictionary<int, decimal>();
            Dijkstra(od, null, null, dist, new Dictionary<int, VezaMreze>());
            return dist;
        }

        // Yen: najkraća ruta i sledeće najkraće, bez ponavljanja stanica.
        internal List<List<DeonicaRute>> Alternative(int od, int doStanice, int koliko)
        {
            var rezultat = new List<List<DeonicaRute>>();
            List<DeonicaRute> prva = Najkraca(od, doStanice);
            if (prva == null) return rezultat;
            rezultat.Add(prva);
            var kandidati = new List<List<DeonicaRute>>();
            while (rezultat.Count < koliko)
            {
                List<DeonicaRute> poslednja = rezultat[rezultat.Count - 1];
                for (int i = 0; i < poslednja.Count; i++)
                {
                    List<DeonicaRute> koren = poslednja.Take(i).ToList();
                    var bezVeza = new HashSet<int>();
                    foreach (List<DeonicaRute> r in rezultat)
                        if (r.Count > i && r.Take(i).Select(d => d.VezaID).SequenceEqual(koren.Select(d => d.VezaID)))
                            bezVeza.Add(r[i].VezaID);
                    foreach (DeonicaRute d in koren)
                        foreach (VezaMreze v in VezeStanice(d.OdID))
                            bezVeza.Add(v.ID);
                    List<DeonicaRute> nastavak = Najkraca(poslednja[i].OdID, doStanice, bezVeza);
                    if (nastavak == null) continue;
                    List<DeonicaRute> cela = koren.Concat(nastavak).ToList();
                    bool ista = rezultat.Concat(kandidati).Any(r => r.Select(d => d.VezaID).SequenceEqual(cela.Select(d => d.VezaID)));
                    if (!ista) kandidati.Add(cela);
                }
                if (kandidati.Count == 0) break;
                List<DeonicaRute> sledeca = kandidati.OrderBy(r => r.Sum(d => d.Km)).First();
                kandidati.Remove(sledeca);
                for (int i = 0; i < sledeca.Count; i++) sledeca[i].RB = i + 1;
                rezultat.Add(sledeca);
            }
            return rezultat;
        }

        private IEnumerable<VezaMreze> VezeStanice(int stanica)
        {
            List<VezaMreze> l;
            return susedi.TryGetValue(stanica, out l) ? l : Enumerable.Empty<VezaMreze>();
        }

        // Null kad veze nema; prazna lista kad su početak i kraj ista stanica.
        internal List<DeonicaRute> Najkraca(int od, int doStanice, ISet<int> bezVeza = null)
        {
            var dist = new Dictionary<int, decimal>();
            var preko = new Dictionary<int, VezaMreze>();
            Dijkstra(od, doStanice, bezVeza, dist, preko);
            if (!dist.ContainsKey(doStanice)) return null;
            var deonice = new List<DeonicaRute>();
            int s = doStanice;
            while (s != od)
            {
                VezaMreze v = preko[s];
                int pre = v.A == s ? v.B : v.A;
                deonice.Add(new DeonicaRute { VezaID = v.ID, OdID = pre, DoID = s, PrugaID = v.PrugaID, Km = v.Km, Elektrificirana = v.Elektrificirana });
                s = pre;
            }
            deonice.Reverse();
            for (int i = 0; i < deonice.Count; i++) deonice[i].RB = i + 1;
            return deonice;
        }
    }

    internal static class Ruta
    {
        private const int NajviseObilazaka = 10;

        // Ruta sa stanjima deonica; zatvorena veza se obilazi dok alternativa postoji.
        internal static List<DeonicaRute> Izracunaj(PlanKontekst k, PosaoPlan posao, DateTime od, DateTime doVreme, out bool bezAlternative, List<Nalaz> nalazi, List<TelegramOP> uticu = null)
        {
            bezAlternative = false;
            if (uticu == null) uticu = new List<TelegramOP>();
            if (!posao.PrijemID.HasValue || !posao.PredajaID.HasValue) return new List<DeonicaRute>();
            var iskljucene = new HashSet<int>();
            List<DeonicaRute> poslednja = null;
            for (int i = 0; i <= NajviseObilazaka; i++)
            {
                List<DeonicaRute> ruta = k.Graf.Najkraca(posao.PrijemID.Value, posao.PredajaID.Value, iskljucene);
                if (ruta == null)
                {
                    if (poslednja == null) return new List<DeonicaRute>();
                    bezAlternative = true;
                    OznaciZatvore(k, poslednja, od, doVreme, uticu);
                    OznaciTrase(k, posao, poslednja);
                    return poslednja;
                }
                poslednja = ruta;
                List<DeonicaRute> zatvorene = OznaciZatvore(k, ruta, od, doVreme, uticu);
                if (zatvorene.Count == 0)
                {
                    OznaciTrase(k, posao, ruta);
                    DodajUpozorenjaTelegrama(k, ruta, posao, od, doVreme, nalazi, uticu);
                    return ruta;
                }
                foreach (DeonicaRute d in zatvorene) iskljucene.Add(d.VezaID);
            }
            bezAlternative = true;
            OznaciTrase(k, posao, poslednja);
            return poslednja;
        }

        private static List<DeonicaRute> OznaciZatvore(PlanKontekst k, List<DeonicaRute> ruta, DateTime od, DateTime doVreme, List<TelegramOP> uticu)
        {
            var zatvorene = new List<DeonicaRute>();
            foreach (DeonicaRute d in ruta)
            {
                List<TelegramOP> svi = k.Telegrami.Where(x => JeZatvor(x) && JeAktivan(x, od, doVreme) && Pokriva(k, x, d)).ToList();
                TelegramOP t = svi.FirstOrDefault();
                if (t == null) continue;
                foreach (TelegramOP x in svi) if (!uticu.Contains(x)) uticu.Add(x);
                d.Stanje = "ZATVOR";
                d.Napomena = "Telegram " + t.Broj;
                zatvorene.Add(d);
            }
            return zatvorene;
        }

        private static void DodajUpozorenjaTelegrama(PlanKontekst k, List<DeonicaRute> ruta, PosaoPlan posao, DateTime od, DateTime doVreme, List<Nalaz> nalazi, List<TelegramOP> uticu)
        {
            foreach (TelegramOP t in k.Telegrami.Where(x => !JeZatvor(x) && JeAktivan(x, od, doVreme)))
                if (ruta.Any(d => Pokriva(k, t, d)))
                {
                    if (!uticu.Contains(t)) uticu.Add(t);
                    nalazi.Add(new Nalaz(NivoNalaza.Upozorenje, "Telegram " + t.Broj, "Na ruti najave " + posao.BrojNajave + ": " + t.Napomena, posao.ID));
                }
        }

        // Važi u periodu posla, a dnevni interval mora da se preklopi sa vremenom posla.
        private static readonly TimeSpan KrajDana = new TimeSpan(23, 59, 0);

        // Zatvor pruge, ili zatvor svih koloseka (između stanica to je isto što i zatvor pruge).
        internal static bool JeZatvor(TelegramOP t)
        {
            if (t.Vrsta == "ZATVOR") return true;
            string k = (t.Kolosek ?? "").Trim().TrimEnd('.').ToLowerInvariant();
            return k == "svi" || k.StartsWith("svi kolos");
        }

        private static TimeSpan BezSekundi(TimeSpan v)
        {
            return TimeSpan.FromMinutes(Math.Floor(v.TotalMinutes));
        }

        private static bool CeoDan(TimeSpan a, TimeSpan b)
        {
            return a == TimeSpan.Zero && (b == TimeSpan.Zero || b >= KrajDana);
        }

        // Dnevni interval telegrama; null znači da važi neprekidno od VaziOd do VaziDo.
        // Agent i ručni unos upisuju TrajeOd/TrajeDo = 00:00–23:59, a interval je u vremenu VaziOd/VaziDo
        // (prvi dan + početak, poslednji dan + kraj), kao TelegramRed.VaziU na tabu Telegrami. Ručni unos ima i sekunde (07:00:53).
        // Telegram sa „neprekidan“ u napomeni važi bez prekida i kad traje više dana.
        internal static bool DnevniProzor(TelegramOP t, out TimeSpan od, out TimeSpan doVreme)
        {
            od = doVreme = TimeSpan.Zero;
            if (t.TrajeOd.HasValue && t.TrajeDo.HasValue && !CeoDan(BezSekundi(t.TrajeOd.Value), BezSekundi(t.TrajeDo.Value)))
            {
                od = t.TrajeOd.Value;
                doVreme = t.TrajeDo.Value;
                return true;
            }
            if (!t.VaziDo.HasValue || (t.Napomena ?? "").IndexOf("neprekid", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            TimeSpan a = BezSekundi(t.VaziOd.TimeOfDay), b = BezSekundi(t.VaziDo.Value.TimeOfDay);
            if (CeoDan(a, b) || a == b) return false;
            od = a;
            doVreme = b;
            return true;
        }

        internal static bool JeAktivan(TelegramOP t, DateTime od, DateTime doVreme)
        {
            if (t.VaziOd > doVreme || (t.VaziDo.HasValue && t.VaziDo.Value < od)) return false;
            TimeSpan x, y;
            if (!DnevniProzor(t, out x, out y) || (doVreme - od).TotalHours >= 24) return true;
            TimeSpan a = od.TimeOfDay, b = doVreme.TimeOfDay;
            var delovi = new List<Tuple<TimeSpan, TimeSpan>>();
            if (b >= a) delovi.Add(Tuple.Create(a, b));
            else
            {
                delovi.Add(Tuple.Create(a, TimeSpan.FromHours(24)));
                delovi.Add(Tuple.Create(TimeSpan.Zero, b));
            }
            if (y >= x) return delovi.Any(p => p.Item1 < y && p.Item2 > x);
            return delovi.Any(p => p.Item2 > x || p.Item1 < y);
        }

        internal static bool Pokriva(PlanKontekst k, TelegramOP t, DeonicaRute d)
        {
            if (t.PrugaID.HasValue && d.PrugaID.HasValue && t.PrugaID != d.PrugaID) return false;
            if (!t.OdID.HasValue || !t.DoID.HasValue) return t.PrugaID.HasValue && t.PrugaID == d.PrugaID;
            bool iste = (t.OdID == d.OdID && t.DoID == d.DoID) || (t.OdID == d.DoID && t.DoID == d.OdID);
            if (iste) return true;
            if (!d.PrugaID.HasValue) return false;
            // Mesta deonice moraju biti između stanica iz telegrama na istoj pruzi.
            int? a = k.RBNaPruzi(d.PrugaID.Value, t.OdID.Value), b = k.RBNaPruzi(d.PrugaID.Value, t.DoID.Value);
            int? x = k.RBNaPruzi(d.PrugaID.Value, d.OdID), y = k.RBNaPruzi(d.PrugaID.Value, d.DoID);
            if (!a.HasValue || !b.HasValue || !x.HasValue || !y.HasValue) return false;
            int min = Math.Min(a.Value, b.Value), max = Math.Max(a.Value, b.Value);
            return Math.Min(x.Value, y.Value) >= min && Math.Max(x.Value, y.Value) <= max;
        }

        private static void OznaciTrase(PlanKontekst k, PosaoPlan posao, List<DeonicaRute> ruta)
        {
            var mesto = new Dictionary<int, int>();
            for (int i = 0; i < ruta.Count; i++)
            {
                if (!mesto.ContainsKey(ruta[i].OdID)) mesto[ruta[i].OdID] = i;
                if (i == ruta.Count - 1 && !mesto.ContainsKey(ruta[i].DoID)) mesto[ruta[i].DoID] = i + 1;
            }
            var vazece = new List<Tuple<TrasaOP, int, int>>();
            foreach (TrasaOP t in k.TraseZa(posao))
            {
                int a, b;
                if (t.PocetnaID.HasValue && t.KrajnjaID.HasValue && mesto.TryGetValue(t.PocetnaID.Value, out a) && mesto.TryGetValue(t.KrajnjaID.Value, out b) && a < b)
                    vazece.Add(Tuple.Create(t, a, b));
            }
            for (int i = 0; i < ruta.Count; i++)
            {
                if (ruta[i].Stanje == "ZATVOR") continue;
                Tuple<TrasaOP, int, int> izbor = vazece.Where(v => v.Item2 <= i && i < v.Item3)
                    .OrderBy(v => v.Item1.AdHocStatus == "ZAHTEV" ? 1 : 0).ThenByDescending(v => v.Item3 - v.Item2).FirstOrDefault();
                if (izbor == null) { ruta[i].Stanje = "NEMAMO_TRASU"; continue; }
                ruta[i].TrasaID = izbor.Item1.ID;
                ruta[i].Stanje = izbor.Item1.AdHocStatus == "ZAHTEV" ? "ZAHTEV_ADHOC" : "IMA_TRASU";
            }
        }
    }
}
