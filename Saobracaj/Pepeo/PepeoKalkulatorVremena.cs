using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Saobracaj.Pepeo
{
    public class PepeoKalkulatorVremena
    {
        private readonly List<PepeoOperacijaDefinicija> _operacije;
        private readonly Dictionary<string, string> _parametri;

        public PepeoKalkulatorVremena(
            List<PepeoOperacijaDefinicija> operacije,
            Dictionary<string, string> parametri)
        {
            _operacije = (operacije ?? new List<PepeoOperacijaDefinicija>())
                .OrderBy(x => x.Redosled)
                .ToList();

            _parametri = parametri ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public List<PepeoKorakVremena> IzracunajCiklus(DateTime pocetnoVreme)
        {
            var rezultat = new List<PepeoKorakVremena>();
            DateTime prethodniKraj = pocetnoVreme;

            foreach (PepeoOperacijaDefinicija operacija in _operacije)
            {
                DateTime pocetak;
                DateTime kraj;

                IzracunajKorak(operacija, prethodniKraj, out pocetak, out kraj);

                rezultat.Add(new PepeoKorakVremena
                {
                    ID = 0,
                    CiklusID = 0,
                    Redosled = operacija.Redosled,
                    OperacijaID = operacija.OperacijaID,
                    SifraOperacije = operacija.Sifra,
                    Operacija = operacija.Naziv,
                    Drzava = operacija.Drzava,
                    TipPravila = operacija.TipPravila,
                    TrajanjeMinuta = operacija.TrajanjeMinuta,
                    PlaniraniPocetak = pocetak,
                    PlaniraniKraj = kraj,
                    StvarniPocetak = null,
                    StvarniKraj = null,
                    OdstupanjeMinuta = 0,
                    RazlogKasnjenjaID = null,
                    RazlogOdstupanja = string.Empty,
                    Partner = string.Empty,
                    Napomena = string.Empty
                });

                prethodniKraj = kraj;
            }

            return rezultat;
        }

        /// <summary>
        /// Izmenjeni korak zadrzava svoj planirani interval i dobija stvarni kraj.
        /// Svi naredni koraci, ukljucujuci naredne cikluse u prosledjenom lancu,
        /// ponovo se racunaju po poslovnim pravilima.
        /// Ako neki naredni korak vec ima potvrdeno stvarno vreme, ono ostaje fiksna tacka.
        /// </summary>
        public void PreracunajLanacPosleStvarnogVremena(
            List<PepeoKorakVremena> lanac,
            int indeksIzmenjenog,
            DateTime stvarniKraj,
            int razlogKasnjenjaID,
            string razlog,
            string partner,
            string napomena)
        {
            if (lanac == null || indeksIzmenjenog < 0 || indeksIzmenjenog >= lanac.Count)
                throw new ArgumentException("Neispravan korak za preracunavanje.");

            PepeoKorakVremena izmenjeni = lanac[indeksIzmenjenog];

            DateTime stvarniPocetak = izmenjeni.PlaniraniPocetak;
            if (indeksIzmenjenog > 0)
            {
                PepeoKorakVremena prethodni = lanac[indeksIzmenjenog - 1];
                stvarniPocetak = prethodni.StvarniKraj.HasValue
                    ? prethodni.StvarniKraj.Value
                    : prethodni.PlaniraniKraj;
            }

            izmenjeni.StvarniPocetak = stvarniPocetak;
            izmenjeni.StvarniKraj = stvarniKraj;
            izmenjeni.OdstupanjeMinuta = Convert.ToInt32(
                Math.Round((stvarniKraj - izmenjeni.PlaniraniKraj).TotalMinutes, MidpointRounding.AwayFromZero));
            izmenjeni.RazlogKasnjenjaID = razlogKasnjenjaID;
            izmenjeni.RazlogOdstupanja = razlog ?? string.Empty;
            izmenjeni.Partner = partner ?? string.Empty;
            izmenjeni.Napomena = string.IsNullOrWhiteSpace(napomena) ? string.Empty : napomena.Trim();

            DateTime prethodniKraj = stvarniKraj;

            for (int i = indeksIzmenjenog + 1; i < lanac.Count; i++)
            {
                PepeoKorakVremena korak = lanac[i];
                PepeoOperacijaDefinicija definicija = PronadjiDefiniciju(korak.OperacijaID, korak.SifraOperacije);

                DateTime noviPocetak;
                DateTime noviKraj;
                IzracunajKorak(definicija, prethodniKraj, out noviPocetak, out noviKraj);

                korak.PlaniraniPocetak = noviPocetak;
                korak.PlaniraniKraj = noviKraj;

                // Vec potvrdena stvarna vremena ostaju stvarna istorija.
                if (korak.StvarniKraj.HasValue)
                {
                    if (!korak.StvarniPocetak.HasValue)
                        korak.StvarniPocetak = noviPocetak;

                    korak.OdstupanjeMinuta = Convert.ToInt32(
                        Math.Round((korak.StvarniKraj.Value - noviKraj).TotalMinutes, MidpointRounding.AwayFromZero));

                    prethodniKraj = korak.StvarniKraj.Value;
                }
                else
                {
                    korak.OdstupanjeMinuta = 0;
                    korak.RazlogKasnjenjaID = null;
                    korak.RazlogOdstupanja = string.Empty;
                    korak.Partner = string.Empty;
                    korak.Napomena = string.Empty;
                    prethodniKraj = noviKraj;
                }
            }
        }

        private PepeoOperacijaDefinicija PronadjiDefiniciju(int operacijaID, string sifra)
        {
            PepeoOperacijaDefinicija d = _operacije.FirstOrDefault(x => x.OperacijaID == operacijaID);
            if (d == null && !string.IsNullOrWhiteSpace(sifra))
                d = _operacije.FirstOrDefault(x => string.Equals(x.Sifra, sifra, StringComparison.OrdinalIgnoreCase));

            if (d == null)
                throw new Exception("Nije pronadjena definicija operacije: " + sifra);

            return d;
        }

        private void IzracunajKorak(
            PepeoOperacijaDefinicija operacija,
            DateTime prethodniKraj,
            out DateTime pocetak,
            out DateTime kraj)
        {
            string pravilo = (operacija.TipPravila ?? string.Empty).Trim().ToUpperInvariant();

            if (pravilo == "SURCIN_LOADING")
            {
                // Surcin Loading je spoj stare Jakovo delivery + Jakovo loading logike.
                // Vidljivi pocetak operacije je trenutak pocetka delivery dela,
                // zatim se racuna 30 min delivery-a, pa 30 radnih sati loading-a.
                pocetak = IzracunajPocetakSurcinLoading(prethodniKraj);

                int deliveryMinuta = VratiInt("SurcinLoadingDeliveryDurationMinutes", 30);
                DateTime pocetakUtovara = PodesiPocetakSurcinRadnogDana(
                    pocetak.AddMinutes(deliveryMinuta));

                kraj = DodajRadneMinuteSurcinLoading(
                    pocetakUtovara,
                    operacija.TrajanjeMinuta);

                return;
            }

            if (pravilo == "CUSTOMS")
            {
                pocetak = IzracunajPocetakCarine(prethodniKraj);
                kraj = pocetak.AddMinutes(operacija.TrajanjeMinuta);
                return;
            }

            if (pravilo == "ALESD_UNLOADING")
            {
                pocetak = PodesiPocetakAlesdIstovara(prethodniKraj);
                kraj = DodajRadneMinuteAlesdIstovar(pocetak, operacija.TrajanjeMinuta);
                return;
            }

            // Sve spojene transportne operacije imaju fiksno trajanje 24/7.
            pocetak = prethodniKraj;
            kraj = prethodniKraj.AddMinutes(operacija.TrajanjeMinuta);
        }

        private DateTime IzracunajPocetakSurcinLoading(DateTime dolazak)
        {
            TimeSpan granica1 = VratiVreme("SurcinLoadingDeliveryCutoff1", "07:30");
            TimeSpan granica2 = VratiVreme("SurcinLoadingDeliveryCutoff2", "14:00");
            TimeSpan jutarnjiStart = VratiVreme("SurcinLoadingDeliveryStart", "08:00");
            int posleDolaska = VratiInt("SurcinLoadingAfterArrivalMinutes", 30);

            DateTime kandidat;

            if (dolazak.TimeOfDay < granica1)
                kandidat = dolazak.Date.Add(jutarnjiStart);
            else if (dolazak.TimeOfDay <= granica2)
                kandidat = dolazak.AddMinutes(posleDolaska);
            else
                kandidat = dolazak.Date.AddDays(1).Add(jutarnjiStart);

            kandidat = PomeriNedeljuNaPonedeljak(kandidat, jutarnjiStart);
            return PodesiPocetakSurcinRadnogDana(kandidat);
        }

        private DateTime PodesiPocetakSurcinRadnogDana(DateTime vrednost)
        {
            TimeSpan start = VratiVreme("SurcinLoadingWorkStart", "08:00");
            TimeSpan weekdayEnd = VratiVreme("SurcinLoadingWeekdayEnd", "21:00");
            TimeSpan saturdayEnd = VratiVreme("SurcinLoadingSaturdayEnd", "15:00");
            DateTime t = vrednost;

            while (true)
            {
                if (t.DayOfWeek == DayOfWeek.Sunday)
                {
                    t = t.Date.AddDays(1).Add(start);
                    continue;
                }

                if (t.TimeOfDay < start)
                    t = t.Date.Add(start);

                TimeSpan end = t.DayOfWeek == DayOfWeek.Saturday ? saturdayEnd : weekdayEnd;
                if (t.TimeOfDay >= end)
                {
                    t = t.Date.AddDays(1).Add(start);
                    continue;
                }

                return t;
            }
        }

        private DateTime DodajRadneMinuteSurcinLoading(DateTime pocetak, int ukupnoMinuta)
        {
            TimeSpan start = VratiVreme("SurcinLoadingWorkStart", "08:00");
            TimeSpan weekdayEnd = VratiVreme("SurcinLoadingWeekdayEnd", "21:00");
            TimeSpan saturdayEnd = VratiVreme("SurcinLoadingSaturdayEnd", "15:00");
            int maxDnevno = VratiInt("SurcinLoadingMaxDailyMinutes", 600);

            DateTime t = PodesiPocetakSurcinRadnogDana(pocetak);
            int preostalo = ukupnoMinuta;

            while (preostalo > 0)
            {
                t = PodesiPocetakSurcinRadnogDana(t);

                TimeSpan end = t.DayOfWeek == DayOfWeek.Saturday ? saturdayEnd : weekdayEnd;
                DateTime krajDana = t.Date.Add(end);
                DateTime krajPoLimitu = t.AddMinutes(maxDnevno);
                DateTime dozvoljeniKraj = krajPoLimitu < krajDana ? krajPoLimitu : krajDana;

                int dostupno = Convert.ToInt32((dozvoljeniKraj - t).TotalMinutes);
                if (dostupno <= 0)
                {
                    t = t.Date.AddDays(1).Add(start);
                    continue;
                }

                int rad = Math.Min(preostalo, dostupno);
                t = t.AddMinutes(rad);
                preostalo -= rad;

                if (preostalo > 0)
                    t = t.Date.AddDays(1).Add(start);
            }

            return t;
        }

        private DateTime IzracunajPocetakCarine(DateTime prethodniKraj)
        {
            TimeSpan start = VratiVreme("CustomsStart", "08:00");
            TimeSpan cutoff = VratiVreme("CustomsCutoff", "14:00");
            TimeSpan saturdayCutoff = VratiVreme("CustomsSaturdayCutoff", "10:00");
            DateTime t = prethodniKraj;

            while (true)
            {
                if (t.DayOfWeek == DayOfWeek.Sunday)
                {
                    t = t.Date.AddDays(1).Add(start);
                    continue;
                }

                if (t.DayOfWeek == DayOfWeek.Saturday && t.TimeOfDay > saturdayCutoff)
                    return SledeciPonedeljak(t).Add(start);

                if (t.TimeOfDay < start)
                    return t.Date.Add(start);

                if (t.TimeOfDay <= cutoff)
                    return t;

                t = t.Date.AddDays(1).Add(start);
            }
        }

        private DateTime PodesiPocetakAlesdIstovara(DateTime vrednost)
        {
            TimeSpan start = VratiVreme("AlesdUnloadingStart", "08:00");
            TimeSpan skipAfter = VratiVreme("AlesdUnloadingSkipAfter", "18:00");
            TimeSpan weekdayEnd = VratiVreme("AlesdUnloadingWeekdayEnd", "20:00");
            TimeSpan saturdayEnd = VratiVreme("AlesdUnloadingSaturdayEnd", "15:00");
            DateTime t = vrednost;

            while (true)
            {
                if (t.DayOfWeek == DayOfWeek.Sunday)
                {
                    t = t.Date.AddDays(1).Add(start);
                    continue;
                }

                if (t.TimeOfDay < start)
                    t = t.Date.Add(start);

                if (t.DayOfWeek == DayOfWeek.Saturday)
                {
                    if (t.TimeOfDay >= saturdayEnd)
                    {
                        t = SledeciPonedeljak(t).Add(start);
                        continue;
                    }

                    return t;
                }

                if (t.TimeOfDay > skipAfter || t.TimeOfDay >= weekdayEnd)
                {
                    t = t.Date.AddDays(1).Add(start);
                    continue;
                }

                return t;
            }
        }

        private DateTime DodajRadneMinuteAlesdIstovar(DateTime pocetak, int ukupnoMinuta)
        {
            TimeSpan start = VratiVreme("AlesdUnloadingStart", "08:00");
            TimeSpan weekdayEnd = VratiVreme("AlesdUnloadingWeekdayEnd", "20:00");
            TimeSpan saturdayEnd = VratiVreme("AlesdUnloadingSaturdayEnd", "15:00");
            int maxDnevno = VratiInt("AlesdUnloadingMaxDailyMinutes", 720);

            DateTime t = PodesiPocetakAlesdIstovara(pocetak);
            int preostalo = ukupnoMinuta;

            while (preostalo > 0)
            {
                t = PodesiPocetakAlesdIstovara(t);

                TimeSpan end = t.DayOfWeek == DayOfWeek.Saturday ? saturdayEnd : weekdayEnd;
                DateTime krajDana = t.Date.Add(end);
                DateTime krajPoDnevnomLimitu = t.AddMinutes(maxDnevno);
                DateTime dozvoljeniKraj = krajPoDnevnomLimitu < krajDana ? krajPoDnevnomLimitu : krajDana;

                int dostupno = Convert.ToInt32((dozvoljeniKraj - t).TotalMinutes);
                if (dostupno <= 0)
                {
                    t = t.Date.AddDays(1).Add(start);
                    continue;
                }

                int rad = Math.Min(preostalo, dostupno);
                t = t.AddMinutes(rad);
                preostalo -= rad;

                if (preostalo > 0)
                    t = t.Date.AddDays(1).Add(start);
            }

            return t;
        }

        private DateTime PomeriNedeljuNaPonedeljak(DateTime vrednost, TimeSpan vreme)
        {
            if (vrednost.DayOfWeek == DayOfWeek.Sunday)
                return vrednost.Date.AddDays(1).Add(vreme);

            return vrednost;
        }

        private DateTime SledeciPonedeljak(DateTime vrednost)
        {
            DateTime dan = vrednost.Date;
            do
            {
                dan = dan.AddDays(1);
            }
            while (dan.DayOfWeek != DayOfWeek.Monday);

            return dan;
        }

        private TimeSpan VratiVreme(string kljuc, string podrazumevano)
        {
            string vrednost;
            if (!_parametri.TryGetValue(kljuc, out vrednost) || string.IsNullOrWhiteSpace(vrednost))
                vrednost = podrazumevano;

            TimeSpan parsed;
            if (TimeSpan.TryParseExact(vrednost.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out parsed))
                return parsed;
            if (TimeSpan.TryParse(vrednost.Trim(), out parsed))
                return parsed;

            return TimeSpan.Parse(podrazumevano, CultureInfo.InvariantCulture);
        }

        private int VratiInt(string kljuc, int podrazumevano)
        {
            string vrednost;
            if (!_parametri.TryGetValue(kljuc, out vrednost) || string.IsNullOrWhiteSpace(vrednost))
                return podrazumevano;

            int parsed;
            return int.TryParse(vrednost.Trim(), out parsed) ? parsed : podrazumevano;
        }
    }
}
