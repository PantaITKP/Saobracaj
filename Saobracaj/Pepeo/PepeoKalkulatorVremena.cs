using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Saobracaj.Pepeo
{
    public class PepeoKalkulatorVremena
    {
        private readonly List<PepeoSablonOperacije> _sabloni;
        private readonly Dictionary<string, string> _parametri;

        public PepeoKalkulatorVremena(List<PepeoSablonOperacije> sabloni, Dictionary<string, string> parametri)
        {
            _sabloni = (sabloni ?? new List<PepeoSablonOperacije>()).OrderBy(x => x.RedosledKoraka).ToList();
            _parametri = parametri ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public List<PepeoKorakVremena> Izracunaj(DateTime prviPocetak, int brojCiklusa, int idTrase, int najava)
        {
            var rezultat = new List<PepeoKorakVremena>();
            DateTime pocetakCiklusa = prviPocetak;

            if (brojCiklusa <= 0)
                brojCiklusa = 1;

            for (int ciklus = 1; ciklus <= brojCiklusa; ciklus++)
            {
                var koraciCiklusa = IzracunajJedanCiklus(pocetakCiklusa, ciklus, idTrase, najava);
                rezultat.AddRange(koraciCiklusa);

                if (koraciCiklusa.Count > 0)
                    pocetakCiklusa = koraciCiklusa[koraciCiklusa.Count - 1].PlaniranoVreme;
            }

            return rezultat;
        }

        public void PreracunajPosleRucneIzmene(List<PepeoKorakVremena> koraci, int indeksIzmenjenogReda, DateTime novoStvarnoVreme, string napomena)
        {
            if (koraci == null || indeksIzmenjenogReda < 0 || indeksIzmenjenogReda >= koraci.Count)
                return;

            PepeoKorakVremena izmenjeni = koraci[indeksIzmenjenogReda];
            izmenjeni.StvarnoVreme = novoStvarnoVreme;
            izmenjeni.RucnoIzmenjeno = true;
            izmenjeni.KasnjenjeMin = Convert.ToInt32((novoStvarnoVreme - izmenjeni.PlaniranoVreme).TotalMinutes);
            izmenjeni.Napomena = string.IsNullOrWhiteSpace(napomena)
                ? "Rucna izmena vremena. Sledeci koraci su preracunati."
                : napomena.Trim();

            DateTime prethodniKraj = novoStvarnoVreme;

            for (int i = indeksIzmenjenogReda + 1; i < koraci.Count; i++)
            {
                PepeoKorakVremena stari = koraci[i];
                PepeoSablonOperacije sablon = _sabloni.FirstOrDefault(x => string.Equals(x.SifraKoraka, stari.SifraKoraka, StringComparison.OrdinalIgnoreCase));
                if (sablon == null)
                    continue;

                DateTime pocetak;
                DateTime kraj;
                IzracunajKorak(sablon, prethodniKraj, out pocetak, out kraj);

                stari.PlaniraniPocetak = pocetak;
                stari.PlaniranoVreme = kraj;
                stari.StvarnoVreme = null;
                stari.RucnoIzmenjeno = false;
                stari.KasnjenjeMin = 0;
                stari.Napomena = "Preracunato zbog prethodne rucne izmene.";

                prethodniKraj = kraj;
            }
        }

        private List<PepeoKorakVremena> IzracunajJedanCiklus(DateTime pocetakCiklusa, int ciklus, int idTrase, int najava)
        {
            var rezultat = new List<PepeoKorakVremena>();
            DateTime prethodniKraj = pocetakCiklusa;

            foreach (PepeoSablonOperacije sablon in _sabloni)
            {
                DateTime pocetak;
                DateTime kraj;
                IzracunajKorak(sablon, prethodniKraj, out pocetak, out kraj);

                rezultat.Add(new PepeoKorakVremena
                {
                    IDTrase = idTrase,
                    Najava = najava,
                    Ciklus = ciklus,
                    RB = ((ciklus - 1) * _sabloni.Count) + sablon.RedosledKoraka,
                    SifraKoraka = sablon.SifraKoraka,
                    Stanica = sablon.StanicaID,
                    StanicaOpis = sablon.StanicaOpis,
                    Operacija = sablon.NazivKoraka,
                    UlazakUStanicu = sablon.UlazakUStanicu,
                    PlaniraniPocetak = pocetak,
                    PlaniranoVreme = kraj,
                    StvarnoVreme = null,
                    RucnoIzmenjeno = false,
                    KasnjenjeMin = 0,
                    Napomena = sablon.Napomena
                });

                prethodniKraj = kraj;
            }

            return rezultat;
        }

        private void IzracunajKorak(PepeoSablonOperacije sablon, DateTime prethodniKraj, out DateTime pocetak, out DateTime kraj)
        {
            string pravilo = (sablon.TipPravila ?? string.Empty).Trim().ToUpperInvariant();

            if (pravilo == "INPUT_EVENT" || pravilo == "EVENT_AFTER_PREVIOUS")
            {
                pocetak = prethodniKraj;
                kraj = prethodniKraj;
                return;
            }

            if (pravilo == "JAKOVO_DELIVERY")
            {
                pocetak = IzracunajPocetakJakovoIsporuke(prethodniKraj);
                kraj = pocetak.AddMinutes(sablon.TrajanjeMinuta);
                return;
            }

            if (pravilo == "JAKOVO_LOADING")
            {
                pocetak = PodesiPocetakJakovoUtovara(prethodniKraj);
                kraj = DodajRadneMinuteJakovoUtovar(pocetak, sablon.TrajanjeMinuta);
                return;
            }

            if (pravilo == "CUSTOMS")
            {
                pocetak = IzracunajPocetakCarine(prethodniKraj);
                kraj = pocetak.AddMinutes(sablon.TrajanjeMinuta);
                return;
            }

            if (pravilo == "ALESD_UNLOADING")
            {
                pocetak = PodesiPocetakAlesdIstovara(prethodniKraj);
                kraj = DodajRadneMinuteAlesdIstovar(pocetak, sablon.TrajanjeMinuta);
                return;
            }

            // TRAVEL_FIXED, FIXED_DURATION i ostali jednostavni koraci.
            pocetak = prethodniKraj;
            kraj = prethodniKraj.AddMinutes(sablon.TrajanjeMinuta);
        }

        private DateTime IzracunajPocetakJakovoIsporuke(DateTime dolazak)
        {
            TimeSpan vremeDolaska = dolazak.TimeOfDay;
            TimeSpan granica1 = VratiVreme("JakovoDeliveryCutoff1", "07:30");
            TimeSpan granica2 = VratiVreme("JakovoDeliveryCutoff2", "14:00");
            TimeSpan pocetakDana = VratiVreme("JakovoDeliveryStart", "08:00");
            int minutaPosleDolaska = VratiInt("JakovoDeliveryAfterArrivalMinutes", 30);

            DateTime pocetak;

            if (vremeDolaska < granica1)
                pocetak = dolazak.Date.Add(pocetakDana);
            else if (vremeDolaska >= granica1 && vremeDolaska <= granica2)
                pocetak = dolazak.AddMinutes(minutaPosleDolaska);
            else
                pocetak = dolazak.Date.AddDays(1).Add(pocetakDana);

            return PomeriNedeljuNaPonedeljak(pocetak, pocetakDana);
        }

        private DateTime PodesiPocetakJakovoUtovara(DateTime vrednost)
        {
            TimeSpan pocetakDana = VratiVreme("JakovoLoadingDefaultStart", "08:00");
            TimeSpan krajRadnimDanom = VratiVreme("JakovoLoadingWeekdayCutoff", "21:00");
            TimeSpan krajSubotom = VratiVreme("JakovoLoadingSaturdayCutoff", "15:00");
            DateTime trenutno = vrednost;

            while (true)
            {
                if (trenutno.DayOfWeek == DayOfWeek.Sunday)
                {
                    trenutno = trenutno.Date.AddDays(1).Add(pocetakDana);
                    continue;
                }

                TimeSpan krajDana = trenutno.DayOfWeek == DayOfWeek.Saturday ? krajSubotom : krajRadnimDanom;

                if (trenutno.TimeOfDay < pocetakDana)
                    trenutno = trenutno.Date.Add(pocetakDana);

                if (trenutno.TimeOfDay >= krajDana)
                {
                    trenutno = trenutno.Date.AddDays(1).Add(pocetakDana);
                    continue;
                }

                return trenutno;
            }
        }

        private DateTime DodajRadneMinuteJakovoUtovar(DateTime pocetak, int ukupnoMinuta)
        {
            TimeSpan pocetakDana = VratiVreme("JakovoLoadingDefaultStart", "08:00");
            TimeSpan krajRadnimDanom = VratiVreme("JakovoLoadingWeekdayCutoff", "21:00");
            TimeSpan krajSubotom = VratiVreme("JakovoLoadingSaturdayCutoff", "15:00");
            int maksimumDnevno = VratiInt("JakovoLoadingMaxDailyMinutes", 600);

            DateTime trenutno = PodesiPocetakJakovoUtovara(pocetak);
            int preostalo = ukupnoMinuta;

            while (preostalo > 0)
            {
                trenutno = PodesiPocetakJakovoUtovara(trenutno);

                TimeSpan krajDana = trenutno.DayOfWeek == DayOfWeek.Saturday ? krajSubotom : krajRadnimDanom;
                DateTime krajRadnogDana = trenutno.Date.Add(krajDana);
                DateTime krajPoDnevnomLimitu = trenutno.AddMinutes(maksimumDnevno);
                DateTime dozvoljeniKraj = krajPoDnevnomLimitu < krajRadnogDana ? krajPoDnevnomLimitu : krajRadnogDana;

                int dostupno = Convert.ToInt32((dozvoljeniKraj - trenutno).TotalMinutes);
                if (dostupno <= 0)
                {
                    trenutno = trenutno.Date.AddDays(1).Add(pocetakDana);
                    continue;
                }

                int iskorisceno = Math.Min(preostalo, dostupno);
                trenutno = trenutno.AddMinutes(iskorisceno);
                preostalo -= iskorisceno;

                if (preostalo > 0)
                    trenutno = trenutno.Date.AddDays(1).Add(pocetakDana);
            }

            return trenutno;
        }

        private DateTime IzracunajPocetakCarine(DateTime prethodniKraj)
        {
            TimeSpan pocetakDana = VratiVreme("CustomsStart", "08:00");
            TimeSpan granica = VratiVreme("CustomsCutoff", "14:00");
            TimeSpan subotnjaGranica = VratiVreme("CustomsSaturdayCutoff", "10:00");
            DateTime trenutno = prethodniKraj;

            while (true)
            {
                if (trenutno.DayOfWeek == DayOfWeek.Sunday)
                {
                    trenutno = trenutno.Date.AddDays(1).Add(pocetakDana);
                    continue;
                }

                if (trenutno.DayOfWeek == DayOfWeek.Saturday && trenutno.TimeOfDay > subotnjaGranica)
                {
                    trenutno = SledeciPonedeljak(trenutno).Add(pocetakDana);
                    continue;
                }

                if (trenutno.TimeOfDay < pocetakDana)
                    return trenutno.Date.Add(pocetakDana);

                if (trenutno.TimeOfDay <= granica)
                    return trenutno;

                trenutno = trenutno.Date.AddDays(1).Add(pocetakDana);
            }
        }

        private DateTime PodesiPocetakAlesdIstovara(DateTime vrednost)
        {
            TimeSpan pocetakDana = VratiVreme("AlesdUnloadingStart", "08:00");
            TimeSpan poslednjiStartRadnimDanom = VratiVreme("AlesdUnloadingWeekdayLastStart", "16:00");
            TimeSpan krajSubotom = VratiVreme("AlesdUnloadingSaturdayEnd", "10:00");
            DateTime trenutno = vrednost;

            while (true)
            {
                if (trenutno.DayOfWeek == DayOfWeek.Sunday)
                {
                    trenutno = trenutno.Date.AddDays(1).Add(pocetakDana);
                    continue;
                }

                if (trenutno.TimeOfDay < pocetakDana)
                    trenutno = trenutno.Date.Add(pocetakDana);

                if (trenutno.DayOfWeek == DayOfWeek.Saturday)
                {
                    // Operacija traje 2h; subotom mora da se zavrsi do 10:00.
                    if (trenutno.AddMinutes(120).TimeOfDay <= krajSubotom)
                        return trenutno;

                    trenutno = SledeciPonedeljak(trenutno).Add(pocetakDana);
                    continue;
                }

                if (trenutno.TimeOfDay > poslednjiStartRadnimDanom)
                {
                    trenutno = trenutno.Date.AddDays(1).Add(pocetakDana);
                    continue;
                }

                return trenutno;
            }
        }

        private DateTime DodajRadneMinuteAlesdIstovar(DateTime pocetak, int ukupnoMinuta)
        {
            TimeSpan pocetakDana = VratiVreme("AlesdUnloadingStart", "08:00");
            TimeSpan krajRadnimDanom = VratiVreme("AlesdUnloadingWeekdayEnd", "18:00");
            TimeSpan krajSubotom = VratiVreme("AlesdUnloadingSaturdayEnd", "10:00");

            DateTime trenutno = PodesiPocetakAlesdIstovara(pocetak);
            int preostalo = ukupnoMinuta;

            while (preostalo > 0)
            {
                trenutno = PodesiPocetakAlesdIstovara(trenutno);
                TimeSpan krajDana = trenutno.DayOfWeek == DayOfWeek.Saturday ? krajSubotom : krajRadnimDanom;
                DateTime dozvoljeniKraj = trenutno.Date.Add(krajDana);

                int dostupno = Convert.ToInt32((dozvoljeniKraj - trenutno).TotalMinutes);
                if (dostupno <= 0)
                {
                    trenutno = trenutno.Date.AddDays(1).Add(pocetakDana);
                    continue;
                }

                int iskorisceno = Math.Min(preostalo, dostupno);
                trenutno = trenutno.AddMinutes(iskorisceno);
                preostalo -= iskorisceno;

                if (preostalo > 0)
                    trenutno = trenutno.Date.AddDays(1).Add(pocetakDana);
            }

            return trenutno;
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
            while (dan.DayOfWeek != DayOfWeek.Monday)
                dan = dan.AddDays(1);

            return dan;
        }

        private TimeSpan VratiVreme(string kljuc, string podrazumevano)
        {
            string vrednost;
            if (!_parametri.TryGetValue(kljuc, out vrednost) || string.IsNullOrWhiteSpace(vrednost))
                vrednost = podrazumevano;

            TimeSpan result;
            if (TimeSpan.TryParseExact(vrednost.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out result))
                return result;

            if (TimeSpan.TryParse(vrednost.Trim(), out result))
                return result;

            return TimeSpan.Parse(podrazumevano, CultureInfo.InvariantCulture);
        }

        private int VratiInt(string kljuc, int podrazumevano)
        {
            string vrednost;
            if (!_parametri.TryGetValue(kljuc, out vrednost) || string.IsNullOrWhiteSpace(vrednost))
                return podrazumevano;

            int result;
            if (int.TryParse(vrednost.Trim(), out result))
                return result;

            return podrazumevano;
        }
    }
}
