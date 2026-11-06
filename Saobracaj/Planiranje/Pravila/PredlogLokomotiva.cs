using System;
using System.Collections.Generic;
using System.Linq;
using Saobracaj.Planiranje.Modeli;

namespace Saobracaj.Planiranje.Pravila
{
    internal sealed class StavkaNacrta
    {
        internal PosaoPlan Posao;
        internal RezultatPosla Rezultat;
        internal KandidatLokomotiva Izbor;
        internal string Razlog;
        internal int? Masinovodja;
    }

    // Pravila 8.5a i 8.6.
    internal static class PredlogLokomotiva
    {
        private const int NajviseZaParove = 6;

        // Vraća ono što lokomotivu sprečava da vozi posao u terminu.
        internal static ZauzetostLok Prepreka(IEnumerable<ZauzetostLok> kalendar, int lokomotivaId, PosaoPlan posao, DateTime od, DateTime doVreme)
        {
            foreach (ZauzetostLok z in kalendar)
            {
                if (z.LokomotivaID != lokomotivaId || z.PosaoID == posao.ID) continue;
                if (z.Od >= doVreme || z.Do <= od) continue;
                bool namenska = z.Vrsta == "NAMENSKA" || z.Vrsta == "PROJEKAT";
                if (namenska && !string.IsNullOrEmpty(z.Projekat) && string.Equals(z.Projekat, posao.Projekat, StringComparison.OrdinalIgnoreCase)) continue;
                return z;
            }
            return null;
        }

        internal static string OpisPrepreke(ZauzetostLok z)
        {
            string period = z.Od.ToString("dd.MM. HH:mm") + " – " + z.Do.ToString("dd.MM. HH:mm");
            switch (z.Vrsta)
            {
                case "POSAO": return "već je na poslu " + period + ".";
                case "SERVIS": return "na servisu je " + period + ".";
                case "NAMENSKA":
                case "PROJEKAT": return "namenska je za projekat " + z.Projekat + " " + period + ".";
                default: return "zauzeta je " + period + ".";
            }
        }

        private sealed class Lokacija
        {
            internal int? StanicaID;
            internal DateTime SlobodnaOd = DateTime.MinValue;
            internal string Izvor;
        }

        // Posao koji se tek završava pre prijema određuje lokaciju (stanica predaje), jer GPS pokazuje gde je lokomotiva sada.
        // Inače važi GPS pozicija, pa stanica predaje poslednjeg posla, pa matična stanica.
        private static Lokacija NadjiLokaciju(PlanKontekst k, LokomotivaOP l, IEnumerable<ZauzetostLok> kalendar, PosaoPlan posao, IDictionary<int, GpsLokacija> gps)
        {
            ZauzetostLok poslednji = kalendar
                .Where(z => z.LokomotivaID == l.ID && z.Vrsta == "POSAO" && z.PosaoID != posao.ID && z.Do <= posao.Prijem)
                .OrderByDescending(z => z.Do).FirstOrDefault();
            GpsLokacija g;
            if (gps != null && gps.TryGetValue(l.ID, out g) && g.StanicaID.HasValue && (poslednji == null || poslednji.Do <= DateTime.Now))
                return new Lokacija { StanicaID = g.StanicaID, Izvor = "GPS" };
            if (poslednji != null)
                return new Lokacija { StanicaID = poslednji.PredajaStanicaID, SlobodnaOd = poslednji.Do, Izvor = "Poslednji posao" };
            return new Lokacija { StanicaID = l.MaticnaStanicaID, Izvor = l.MaticnaStanicaID.HasValue ? "Matična stanica" : "Nepoznato" };
        }

        internal static List<KandidatLokomotiva> Kandidati(PlanKontekst k, RezultatPosla r, IList<ZauzetostLok> kalendar, out List<string> iskljuceni, IDictionary<int, GpsLokacija> gps = null)
        {
            iskljuceni = new List<string>();
            PosaoPlan p = r.Posao;
            bool samoDizel = r.Ruta.Any(d => d.Elektrificirana == false);
            Dictionary<int, decimal> dist = p.PrijemID.HasValue ? k.Graf.Rastojanja(p.PrijemID.Value) : new Dictionary<int, decimal>();
            decimal brzina = k.Parametar("BRZINA_PRAZNE_VOZNJE_KMH", p.Prijem, null, null, null, 50);
            decimal tezinaRavn = k.Parametar("TEZINA_RAVNOMERNOST", p.Prijem, null, null, null, 30);
            decimal tezinaServis = k.Parametar("TEZINA_SERVIS", p.Prijem, null, null, null, 120);

            DateTime nedeljaOd = p.Prijem.Date.AddDays(-(((int)p.Prijem.DayOfWeek + 6) % 7)), nedeljaDo = nedeljaOd.AddDays(7);
            List<LokomotivaOP> aktivne = k.Lokomotive;
            Dictionary<int, int> poslova = aktivne.ToDictionary(l => l.ID, l => kalendar.Count(z => z.LokomotivaID == l.ID && z.Vrsta == "POSAO" && z.Od >= nedeljaOd && z.Od < nedeljaDo));
            decimal prosek = aktivne.Count == 0 ? 0 : (decimal)poslova.Values.Average();

            var pojedinacni = new List<KandidatLokomotiva>();
            foreach (LokomotivaOP l in aktivne)
            {
                if (!l.SerijaID.HasValue) { iskljuceni.Add(l.Broj + ": nema seriju."); continue; }
                if (samoDizel && l.Vuca != "D") { iskljuceni.Add(l.Broj + ": ruta nije elektrificirana."); continue; }
                ZauzetostLok prepreka = Prepreka(kalendar, l.ID, p, p.Prijem, r.Predaja);
                if (prepreka != null) { iskljuceni.Add(l.Broj + ": " + OpisPrepreke(prepreka)); continue; }

                Lokacija lok = NadjiLokaciju(k, l, kalendar, p, gps);
                decimal km;
                bool poznato = lok.StanicaID.HasValue && dist.TryGetValue(lok.StanicaID.Value, out km);
                km = poznato ? dist[lok.StanicaID.Value] : 0;
                int prazna = brzina > 0 ? (int)Math.Ceiling(km / brzina * 60) : 0;
                if (poznato && lok.SlobodnaOd.AddMinutes(prazna) > p.Prijem) { iskljuceni.Add(l.Broj + ": ne stiže do prijema."); continue; }

                decimal ocena = prazna + tezinaRavn * (poslova[l.ID] - prosek);
                bool servisPosle = kalendar.Any(z => z.LokomotivaID == l.ID && z.Vrsta == "SERVIS" && z.Od >= r.Predaja && z.Od <= r.Predaja.AddHours(48));
                if (servisPosle) ocena += tezinaServis;
                pojedinacni.Add(new KandidatLokomotiva
                {
                    Lokomotive = new List<LokomotivaOP> { l },
                    Lokacija = lok.StanicaID.HasValue ? k.Naziv(lok.StanicaID) : "Nepoznata",
                    LokacijaID = lok.StanicaID,
                    IzvorLokacije = lok.Izvor,
                    KmDoPrijema = poznato ? km : (decimal?)null,
                    PraznaVoznjaMin = prazna,
                    LokacijaNepoznata = !poznato,
                    Ocena = ocena,
                    Razlog = (poznato ? "Prazna vožnja " + prazna + " min" : "Lokacija ili veza do prijema nije poznata")
                        + ", poslova u nedelji " + poslova[l.ID] + (servisPosle ? ", servis u narednih 48 h" : "")
                });
            }

            var rezultat = new List<KandidatLokomotiva>();
            foreach (IGrouping<int, KandidatLokomotiva> serija in pojedinacni.GroupBy(c => c.Lokomotive[0].SerijaID.Value))
            {
                MasaZaSeriju masa = BrojLokomotiva.Odredi(k, r.Bruto, serija.First().Lokomotive[0], r.Ruta, p.Prijem);
                if (masa.Broj == 1) rezultat.AddRange(serija);
                else if (masa.Broj == 2) rezultat.AddRange(Parovi(serija.OrderBy(c => c.Ocena).Take(NajviseZaParove).ToList()));
                else iskljuceni.Add("Serija " + serija.First().Lokomotive[0].Serija + ": " + masa.Tekst);
            }
            // Mesta bez poznate lokacije idu iza onih čija se prazna vožnja može izračunati.
            return rezultat.OrderBy(c => c.LokacijaNepoznata ? 1 : 0).ThenBy(c => c.Ocena).ThenBy(c => c.Oznaka).ToList();
        }

        private static IEnumerable<KandidatLokomotiva> Parovi(List<KandidatLokomotiva> lista)
        {
            for (int i = 0; i < lista.Count; i++)
                for (int j = i + 1; j < lista.Count; j++)
                    yield return new KandidatLokomotiva
                    {
                        Lokomotive = new List<LokomotivaOP> { lista[i].Lokomotive[0], lista[j].Lokomotive[0] },
                        Lokacija = lista[i].Lokacija + " / " + lista[j].Lokacija,
                        LokacijaID = lista[i].LokacijaID,
                        IzvorLokacije = lista[i].IzvorLokacije == lista[j].IzvorLokacije ? lista[i].IzvorLokacije : lista[i].IzvorLokacije + " / " + lista[j].IzvorLokacije,
                        KmDoPrijema = lista[i].KmDoPrijema.HasValue && lista[j].KmDoPrijema.HasValue ? Math.Max(lista[i].KmDoPrijema.Value, lista[j].KmDoPrijema.Value) : (decimal?)null,
                        PraznaVoznjaMin = Math.Max(lista[i].PraznaVoznjaMin, lista[j].PraznaVoznjaMin),
                        LokacijaNepoznata = lista[i].LokacijaNepoznata || lista[j].LokacijaNepoznata,
                        Ocena = lista[i].Ocena + lista[j].Ocena,
                        Razlog = "Dvostruka vuča; " + lista[i].Razlog + " / " + lista[j].Razlog
                    };
        }

        // Pohlepno po prijemu, uz privremenu zauzetost u memoriji. Ništa se ne upisuje.
        internal static List<StavkaNacrta> Nacrt(PlanKontekst k, IEnumerable<PosaoPlan> poslovi)
        {
            var kalendar = new List<ZauzetostLok>(k.Kalendar);
            var nacrt = new List<StavkaNacrta>();
            foreach (PosaoPlan p in poslovi.OrderBy(x => x.Prijem))
            {
                if (kalendar.Any(z => z.PosaoID == p.ID && z.Vrsta == "POSAO")) continue;
                RezultatPosla r = PlanRacun.Izracunaj(k, p);
                List<string> iskljuceni;
                KandidatLokomotiva izbor = Kandidati(k, r, kalendar, out iskljuceni).FirstOrDefault();
                nacrt.Add(new StavkaNacrta { Posao = p, Rezultat = r, Izbor = izbor, Razlog = izbor?.Razlog ?? "Nema slobodne lokomotive." });
                if (izbor == null) continue;
                int rb = 1;
                foreach (LokomotivaOP l in izbor.Lokomotive)
                    kalendar.Add(new ZauzetostLok { LokomotivaID = l.ID, Vrsta = "POSAO", PosaoID = p.ID, Od = p.Prijem, Do = r.Predaja, PredajaStanicaID = p.PredajaID, RB = rb++ });
            }
            return nacrt;
        }
    }

    // Pravilo 8.7: konflikti na nivou plana. Osoblje i zadaci su u PonudaOsoblja.Nalazi.
    internal static class KonfliktiPlana
    {
        internal static List<Nalaz> Izracunaj(PlanKontekst k, IEnumerable<RezultatPosla> rezultati)
        {
            var nalazi = new List<Nalaz>();
            List<RezultatPosla> lista = rezultati.ToList();
            foreach (RezultatPosla r in lista) nalazi.AddRange(r.Nalazi);

            foreach (IGrouping<int, ZauzetostLok> g in k.Kalendar.Where(z => z.Vrsta == "POSAO").GroupBy(z => z.LokomotivaID))
            {
                LokomotivaOP l = k.Lokomotive.FirstOrDefault(x => x.ID == g.Key);
                List<ZauzetostLok> poslovi = g.OrderBy(z => z.Od).ToList();
                for (int i = 0; i + 1 < poslovi.Count; i++)
                    if (poslovi[i + 1].Od < poslovi[i].Do && poslovi[i + 1].PosaoID != poslovi[i].PosaoID)
                        nalazi.Add(new Nalaz(NivoNalaza.Konflikt, "Preklapanje lokomotive", (l?.Broj ?? "?") + ": " + poslovi[i].Od.ToString("dd.MM. HH:mm") + " – " + poslovi[i].Do.ToString("HH:mm") + " i " + poslovi[i + 1].Od.ToString("dd.MM. HH:mm") + ".", poslovi[i + 1].PosaoID));
            }

            foreach (RezultatPosla r in lista)
            {
                List<ZauzetostLok> dodele = k.Kalendar.Where(z => z.PosaoID == r.Posao.ID && z.Vrsta == "POSAO").ToList();
                foreach (ZauzetostLok d in dodele)
                {
                    ZauzetostLok namenska = k.Kalendar.FirstOrDefault(z => z.LokomotivaID == d.LokomotivaID && (z.Vrsta == "NAMENSKA" || z.Vrsta == "PROJEKAT") && z.Od < d.Do && z.Do > d.Od
                        && !string.Equals(z.Projekat, r.Posao.Projekat, StringComparison.OrdinalIgnoreCase));
                    if (namenska != null)
                        nalazi.Add(new Nalaz(NivoNalaza.Konflikt, "Posao u namenskom periodu", "Najava " + r.Posao.BrojNajave + ": lokomotiva je namenska za projekat " + namenska.Projekat + ".", r.Posao.ID));
                }
                LokomotivaOP uzorak = dodele.Select(d => k.Lokomotive.FirstOrDefault(l => l.ID == d.LokomotivaID)).FirstOrDefault(l => l != null);
                if (uzorak == null) continue;
                MasaZaSeriju m = BrojLokomotiva.Odredi(k, r.Bruto, uzorak, r.Ruta, r.Posao.Prijem);
                if (m.Broj == 0)
                    nalazi.Add(new Nalaz(NivoNalaza.Konflikt, "Više od 2 lokomotive", "Najava " + r.Posao.BrojNajave + ": " + m.Tekst, r.Posao.ID));
                else if (m.Broj == 2)
                    nalazi.Add(new Nalaz(NivoNalaza.Upozorenje, "Masa iznad tablice za jednu lokomotivu", "Najava " + r.Posao.BrojNajave + ": " + m.Tekst, r.Posao.ID));
            }
            return nalazi;
        }
    }
}
