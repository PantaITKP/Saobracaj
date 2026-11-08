using System;
using System.Collections.Generic;
using System.Linq;
using Saobracaj.Planiranje.Modeli;

namespace Saobracaj.Planiranje.Pravila
{
    // Pravila taskova (odeljak 22 specifikacije): task iz izabrane rute, split rute, split bruta i spajanje.
    // Ovde se samo računa; upis i ponovna provera su u TaskRepo.
    internal static class TaskPravila
    {
        private const int NajduziOpis = 1000;

        // Task prepisuje podatke najave; vreme vožnje se srazmerno km prenosi sa računa posla na izabranu rutu.
        internal static TaskOP IzRute(RezultatPosla r, KombinacijaTrasa k, string opisRute)
        {
            if (k.Ruta.Deonice.Count == 0) throw new InvalidOperationException("Izabrana ruta nema deonica.");
            PosaoPlan p = r.Posao;
            var t = new TaskOP
            {
                BrutoT = r.Bruto,
                BrojKola = p.BrojKola ?? 0,
                DuzinaM = p.DuzinaM,
                TipKola = p.TipKola,
                OpisRute = Skrati(opisRute),
                Nastanak = NastanakTaska.Ruta
            };
            t.Stanice.Add(new TaskStanica { StanicaID = k.Ruta.Deonice[0].OdID });
            decimal km = 0;
            int deo = 0;
            foreach (DeonicaRute d in k.Ruta.Deonice)
            {
                km += d.Km;
                int? trasa = deo < k.Delovi.Count && k.Delovi[deo].Trasa != null ? (int?)k.Delovi[deo].Trasa.ID : null;
                t.Stanice.Add(new TaskStanica { StanicaID = d.DoID, Km = km, PrugaID = d.PrugaID, TrasaID = trasa });
                if (deo < k.Delovi.Count && d.DoID == k.Delovi[deo].DoID) deo++;
            }
            t.Km = km;
            t.ETD = r.Polazak;
            double minuta = (r.Dolazak - r.Polazak).TotalMinutes;
            if (r.Km > 0 && km > 0) minuta = minuta * (double)(km / r.Km);
            t.ETA = Minut(r.Polazak.AddMinutes(minuta));
            t.Najave.Add(new TaskNajavaDeo
            {
                NajavaID = p.NajavaID,
                PosaoID = p.ID,
                BrojNajave = p.BrojNajave,
                KmOd = 0,
                KmDo = km,
                BrutoT = t.BrutoT,
                BrojKola = t.BrojKola
            });
            return t;
        }

        internal const decimal PodrazumevanaObradaMin = 180;
        internal const decimal PodrazumevanaBrzinaKmh = 40;

        // Trajanje taska = obrada (3 h) + vožnja po pruzi (km / brzina voza).
        internal static int TrajanjeMin(decimal km, decimal obradaMin, decimal brzinaKmh)
        {
            if (brzinaKmh <= 0) brzinaKmh = PodrazumevanaBrzinaKmh;
            return (int)Math.Ceiling(obradaMin + km / brzinaKmh * 60);
        }

        internal static string TekstTrajanja(int? minuta)
        {
            if (!minuta.HasValue) return "";
            return minuta.Value / 60 + " h " + (minuta.Value % 60).ToString("00") + " min";
        }

        // Stanice na kojima ruta prelazi sa jedne trase na drugu (ili na deo bez trase): predlog mesta za split rute.
        internal static List<int> PromeneTrase(TaskOP t)
        {
            var lista = new List<int>();
            for (int i = 1; i < t.Stanice.Count - 1; i++)
                if (t.Stanice[i].TrasaID != t.Stanice[i + 1].TrasaID) lista.Add(i);
            return lista;
        }

        // Split rute: task se deli na stanicama sa izabranim indeksima (bez prve i poslednje).
        // Svaki deo nosi ceo sastav i iste lokomotive, a vremena se dele srazmerno km.
        internal static List<TaskOP> SplitRute(TaskOP t, IEnumerable<int> mesta)
        {
            ProveriOtvoren(t);
            List<int> granice = mesta.Distinct().OrderBy(i => i).ToList();
            if (granice.Count == 0) throw new InvalidOperationException("Izaberite bar jednu stanicu na kojoj se ruta deli.");
            if (granice[0] < 1 || granice[granice.Count - 1] > t.Stanice.Count - 2)
                throw new InvalidOperationException("Ruta se deli samo na stanici između početne i krajnje.");
            granice.Insert(0, 0);
            granice.Add(t.Stanice.Count - 1);

            var delovi = new List<TaskOP>();
            for (int g = 0; g + 1 < granice.Count; g++)
            {
                int a = granice[g], b = granice[g + 1];
                decimal kmA = t.Stanice[a].Km, kmB = t.Stanice[b].Km;
                TaskOP d = Kopija(t, NastanakTaska.SplitRute);
                d.Stanice = t.Stanice.Skip(a).Take(b - a + 1).Select((s, i) => new TaskStanica
                {
                    StanicaID = s.StanicaID,
                    Km = s.Km - kmA,
                    PrugaID = i == 0 ? null : s.PrugaID,
                    TrasaID = i == 0 ? null : s.TrasaID
                }).ToList();
                d.Km = kmB - kmA;
                d.ETD = Vreme(t.ETD, t.ETA, Udeo(t, a));
                d.ETA = Vreme(t.ETD, t.ETA, Udeo(t, b));
                d.OpisRute = Skrati(t.OpisRute + " · deo " + (g + 1) + " od " + (granice.Count - 1));
                foreach (TaskNajavaDeo n in t.Najave)
                {
                    TaskNajavaDeo x = n.Kopija();
                    x.KmOd = n.KmOd + kmA;
                    x.KmDo = n.KmOd + kmB;
                    d.Najave.Add(x);
                }
                d.Lokomotive = t.Lokomotive.Select(Kopija).ToList();
                delovi.Add(d);
            }
            return delovi;
        }

        // Drugi bruto deo podrazumevano polazi kad prvi stigne i vozi isto toliko.
        internal static void PredlogVremenaDrugogDela(TaskOP t, out DateTime? etd, out DateTime? eta)
        {
            if (t.ETD.HasValue && t.ETA.HasValue)
            {
                etd = t.ETA;
                eta = t.ETA.Value + (t.ETA.Value - t.ETD.Value);
            }
            else
            {
                etd = t.ETD;
                eta = t.ETA;
            }
        }

        // Split bruta: prvi deo ima uneti bruto i broj kola i vremena izvornog taska, a drugi ostatak i svoja vremena.
        // Oba voze istu rutu. Prvi deo zadržava lokomotive, a drugi samo kad korisnik tako izabere;
        // ako se tada vremena preklapaju, upis odbija provera preklapanja lokomotiva.
        internal static List<TaskOP> SplitBruto(TaskOP t, decimal bruto, int kola, DateTime? etdDrugog, DateTime? etaDrugog, bool lokomotiveDrugom)
        {
            if (etdDrugog.HasValue && etaDrugog.HasValue && etaDrugog.Value <= etdDrugog.Value)
                throw new InvalidOperationException("ETA drugog dela mora biti posle ETD.");
            ProveriOtvoren(t);
            if (t.Najave.Count != 1)
                throw new InvalidOperationException("Task nosi više najava. Split bruta radi se na tasku jedne najave.");
            if (bruto <= 0 || bruto >= t.BrutoT)
                throw new InvalidOperationException("Bruto prvog dela mora biti veći od 0 i manji od " + t.BrutoT.ToString("#,##0.###") + " t.");
            if (t.BrojKola > 0 && (kola < 1 || kola >= t.BrojKola))
                throw new InvalidOperationException("Broj kola prvog dela mora biti od 1 do " + (t.BrojKola - 1) + ".");
            if (t.BrojKola == 0 && kola != 0)
                throw new InvalidOperationException("Task nema upisan broj kola, pa ni prvi deo ne može da ih ima.");

            TaskOP prvi = DeoMase(t, bruto, kola), drugi = DeoMase(t, t.BrutoT - bruto, t.BrojKola - kola);
            prvi.Lokomotive = t.Lokomotive.Select(Kopija).ToList();
            drugi.ETD = etdDrugog;
            drugi.ETA = etaDrugog;
            if (lokomotiveDrugom) drugi.Lokomotive = t.Lokomotive.Select(Kopija).ToList();
            return new List<TaskOP> { prvi, drugi };
        }

        private static TaskOP DeoMase(TaskOP t, decimal bruto, int kola)
        {
            TaskOP d = Kopija(t, NastanakTaska.SplitBruto);
            d.Stanice = t.Stanice.Select(Kopija).ToList();
            d.Km = t.Km;
            d.BrutoT = bruto;
            d.BrojKola = kola;
            decimal udeo = t.BrojKola > 0 ? (decimal)kola / t.BrojKola : bruto / t.BrutoT;
            d.DuzinaM = t.DuzinaM.HasValue ? (decimal?)Math.Round(t.DuzinaM.Value * udeo, 1) : null;
            d.ETD = t.ETD;
            d.ETA = t.ETA;
            d.OpisRute = t.OpisRute;
            TaskNajavaDeo n = t.Najave[0].Kopija();
            n.BrutoT = bruto;
            n.BrojKola = kola;
            d.Najave.Add(n);
            return d;
        }

        // Spajanje: taskovi iste rute daju jedan sastav (zbir bruta i kola, sve najave),
        // a taskovi istog sastava koji se nastavljaju jedan na drugi daju jednu dužu rutu.
        internal static TaskOP Spoji(IList<TaskOP> lista)
        {
            if (lista.Count < 2) throw new InvalidOperationException("Za spajanje izaberite bar dva taska.");
            foreach (TaskOP t in lista) ProveriOtvoren(t);
            List<int> ruta = lista[0].Stanice.Select(s => s.StanicaID).ToList();
            if (lista.All(t => t.Stanice.Select(s => s.StanicaID).SequenceEqual(ruta))) return SpojiSastav(lista);
            List<TaskOP> niz = Nastavak(lista);
            if (niz == null)
                throw new InvalidOperationException("Taskovi se spajaju ako voze istu rutu (sastavi idu zajedno) ili ako se nastavljaju jedan na drugi (krajnja stanica jednog je početna sledećeg).");
            return SpojiRutu(niz);
        }

        private static TaskOP SpojiSastav(IList<TaskOP> lista)
        {
            TaskOP prvi = lista[0];
            TaskOP s = Kopija(prvi, NastanakTaska.Spajanje);
            s.Stanice = prvi.Stanice.Select(Kopija).ToList();
            s.Km = prvi.Km;
            s.BrutoT = lista.Sum(t => t.BrutoT);
            s.BrojKola = lista.Sum(t => t.BrojKola);
            s.DuzinaM = lista.All(t => t.DuzinaM.HasValue) ? (decimal?)lista.Sum(t => t.DuzinaM.Value) : null;
            s.TipKola = SkratiNa(string.Join(", ", lista.Select(t => t.TipKola).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()), 100);
            s.ETD = lista.Min(t => t.ETD);
            s.ETA = lista.Max(t => t.ETA);
            s.OpisRute = prvi.OpisRute;
            s.Najave = lista.SelectMany(t => t.Najave).GroupBy(n => n.NajavaID).Select(g => new TaskNajavaDeo
            {
                NajavaID = g.Key,
                PosaoID = g.Select(n => n.PosaoID).FirstOrDefault(x => x.HasValue),
                BrojNajave = g.First().BrojNajave,
                KmOd = g.Min(n => n.KmOd),
                KmDo = g.Max(n => n.KmDo),
                RBDela = g.Min(n => n.RBDela),
                BrutoT = g.Sum(n => n.BrutoT),
                BrojKola = g.Sum(n => n.BrojKola)
            }).ToList();
            s.Lokomotive = SpojiLokomotive(lista);
            return s;
        }

        private static TaskOP SpojiRutu(List<TaskOP> niz)
        {
            TaskOP prvi = niz[0], poslednji = niz[niz.Count - 1];
            var najave = new HashSet<int>(prvi.Najave.Select(n => n.NajavaID));
            foreach (TaskOP t in niz)
            {
                if (!najave.SetEquals(t.Najave.Select(n => n.NajavaID)))
                    throw new InvalidOperationException("Taskovi koji se nastavljaju spajaju se samo kad nose iste najave.");
                foreach (TaskNajavaDeo n in t.Najave)
                {
                    TaskNajavaDeo p = prvi.Najave.First(x => x.NajavaID == n.NajavaID);
                    if (n.BrutoT != p.BrutoT || n.BrojKola != p.BrojKola)
                        throw new InvalidOperationException("Taskovi koji se nastavljaju spajaju se samo kad nose isti sastav (isti bruto i broj kola po najavi).");
                }
            }
            TaskOP s = Kopija(prvi, NastanakTaska.Spajanje);
            s.Stanice = new List<TaskStanica> { Kopija(prvi.Stanice[0]) };
            decimal km = 0;
            foreach (TaskOP t in niz)
            {
                foreach (TaskStanica st in t.Stanice.Skip(1))
                    s.Stanice.Add(new TaskStanica { StanicaID = st.StanicaID, Km = km + st.Km, PrugaID = st.PrugaID, TrasaID = st.TrasaID });
                km += t.Km;
            }
            s.Km = km;
            s.BrutoT = prvi.BrutoT;
            s.BrojKola = prvi.BrojKola;
            s.DuzinaM = niz.Max(t => t.DuzinaM);
            s.TipKola = prvi.TipKola;
            s.ETD = prvi.ETD;
            s.ETA = poslednji.ETA;
            s.OpisRute = Skrati(string.Join("  →  ", niz.Select(t => t.OpisRute).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()));
            s.Najave = prvi.Najave.Select(n =>
            {
                TaskNajavaDeo x = n.Kopija();
                x.KmDo = poslednji.Najave.First(z => z.NajavaID == n.NajavaID).KmDo;
                return x;
            }).ToList();
            s.Lokomotive = SpojiLokomotive(niz);
            return s;
        }

        // Taskovi poređani tako da se svaki nastavlja na prethodni; null ako to nije moguće.
        private static List<TaskOP> Nastavak(IList<TaskOP> lista)
        {
            List<TaskOP> pocetni = lista.Where(t => !lista.Any(x => x != t && x.DoID == t.OdID)).ToList();
            if (pocetni.Count != 1) return null;
            var niz = new List<TaskOP> { pocetni[0] };
            while (niz.Count < lista.Count)
            {
                List<TaskOP> sledeci = lista.Where(t => !niz.Contains(t) && t.OdID == niz[niz.Count - 1].DoID).ToList();
                if (sledeci.Count != 1) return null;
                niz.Add(sledeci[0]);
            }
            return niz;
        }

        // Jedna vučna (prva po redu), ostale lokomotive su potisak.
        private static List<TaskLokomotiva> SpojiLokomotive(IEnumerable<TaskOP> lista)
        {
            var rezultat = new List<TaskLokomotiva>();
            List<TaskLokomotiva> sve = lista.SelectMany(t => t.Lokomotive.OrderBy(l => l.Uloga == TaskLokomotiva.Vucna ? 0 : 1)).ToList();
            TaskLokomotiva vucna = sve.FirstOrDefault(l => l.Uloga == TaskLokomotiva.Vucna);
            foreach (TaskLokomotiva l in sve)
            {
                if (rezultat.Any(x => x.LokomotivaID == l.LokomotivaID)) continue;
                TaskLokomotiva k = Kopija(l);
                k.Uloga = vucna != null && l.LokomotivaID == vucna.LokomotivaID ? TaskLokomotiva.Vucna : TaskLokomotiva.Potisak;
                rezultat.Add(k);
            }
            return rezultat.OrderBy(l => l.Uloga == TaskLokomotiva.Vucna ? 0 : 1).ToList();
        }

        // Redosled taska u najavi: deo rute, a kad isti deo vozi više taskova i redni broj bruto dela („2.1“).
        internal static string Redosled(int? deoRute, int? delovaBruto, int rbDela)
        {
            if (!deoRute.HasValue) return "–";
            return delovaBruto.GetValueOrDefault() > 1 ? deoRute.Value + "." + rbDela : deoRute.Value.ToString();
        }

        // Task najave polazi pre nego što prethodni deo iste najave stigne.
        internal static string UpozorenjeVremena(TaskOP t, IEnumerable<TaskOP> svi)
        {
            if (t.Status == StatusTaska.Storniran || !t.Od.HasValue) return null;
            // Poredi se sa dolaskom prethodnog dela (ATA, pa ETA), a ne sa „Zauzeta do“, jer ona uključuje obradu posle dolaska.
            Func<TaskOP, DateTime?> dolazak = x => x.ATA ?? x.ETA ?? x.Do;
            foreach (TaskNajavaDeo n in t.Najave)
            {
                TaskOP prethodni = svi.FirstOrDefault(x => x.ID != t.ID && x.Status != StatusTaska.Storniran && dolazak(x).HasValue && dolazak(x).Value > t.Od.Value
                    && x.Najave.Any(z => z.NajavaID == n.NajavaID && z.PosaoID == n.PosaoID && z.KmDo == n.KmOd));
                if (prethodni != null)
                    return "Polazi pre dolaska prethodnog dela (task " + prethodni.ID + ", " + dolazak(prethodni).Value.ToString("dd.MM. HH:mm") + ").";
            }
            return null;
        }

        internal static void ProveriOtvoren(TaskOP t)
        {
            if (!t.Otvoren)
                throw new InvalidOperationException("Task " + t.ID + " je " + StatusTaska.Naziv(t.Status).ToLowerInvariant() + ". Menja se samo otvoren task.");
        }

        private static decimal Udeo(TaskOP t, int indeks)
        {
            if (t.Km > 0) return t.Stanice[indeks].Km / t.Km;
            return t.Stanice.Count > 1 ? (decimal)indeks / (t.Stanice.Count - 1) : 0;
        }

        internal static DateTime? Vreme(DateTime? od, DateTime? doV, decimal udeo)
        {
            if (!od.HasValue || !doV.HasValue) return udeo == 0 ? od : udeo == 1 ? doV : null;
            return Minut(od.Value.AddMinutes((doV.Value - od.Value).TotalMinutes * (double)udeo));
        }

        private static DateTime Minut(DateTime v)
        {
            return new DateTime(v.Year, v.Month, v.Day, v.Hour, v.Minute, 0).AddMinutes(v.Second >= 30 ? 1 : 0);
        }

        private static TaskOP Kopija(TaskOP t, string nastanak)
        {
            return new TaskOP
            {
                BrutoT = t.BrutoT,
                BrojKola = t.BrojKola,
                DuzinaM = t.DuzinaM,
                TipKola = t.TipKola,
                Napomena = t.Napomena,
                Nastanak = nastanak
            };
        }

        private static TaskStanica Kopija(TaskStanica s)
        {
            return new TaskStanica { StanicaID = s.StanicaID, Km = s.Km, PrugaID = s.PrugaID, TrasaID = s.TrasaID };
        }

        private static TaskLokomotiva Kopija(TaskLokomotiva l)
        {
            return new TaskLokomotiva { LokomotivaID = l.LokomotivaID, Broj = l.Broj, Uloga = l.Uloga };
        }

        private static string Skrati(string s)
        {
            return SkratiNa(s, NajduziOpis);
        }

        private static string SkratiNa(string s, int duzina)
        {
            if (string.IsNullOrEmpty(s)) return null;
            return s.Length <= duzina ? s : s.Substring(0, duzina - 1) + "…";
        }
    }
}
