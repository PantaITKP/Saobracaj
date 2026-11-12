using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Forme;
using Saobracaj.Planiranje.Podaci;
using Saobracaj.Planiranje.Pravila;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj
{
    // Kartice stanja na ekranu Transport (RAZVOJ_Transport_kartice.md): Jobs, Taskovi, Mapa mreže i Osoblje.
    // Brojevi se čitaju u pozadini, osvežavaju na 60 s dok se Transport vidi i odmah po povratku na glavni prozor.
    public partial class MainP
    {
        private const int OsvezavanjeKarticaMs = 60000;
        // Povratak na glavni prozor češće od ovoga ne čita bazu ponovo.
        private static readonly TimeSpan NajmanjeIzmedjuOsvezavanja = TimeSpan.FromSeconds(10);

        private KarticaStanja karticaJobs, karticaTaskovi, karticaMapa, karticaOsoblje;
        private readonly Timer tajmerKartica = new Timer { Interval = OsvezavanjeKarticaMs };
        private PravaKartica pravaKartica;
        private bool karticeSeUcitavaju;
        private DateTime poslednjeOsvezavanje = DateTime.MinValue;

        private void DodajKarticeStanja()
        {
            karticaJobs = new KarticaStanja("Jobs", Ikona.Aktovka, "novih", "sa taskovima");
            karticaJobs.Click += (s, e) => Otvori(new frmOPJobs());
            karticaJobs.KlikA += (s, e) => Otvori(new frmOPJobs(FilterJobsTaskova.BezTaskova));
            karticaJobs.KlikB += (s, e) => Otvori(new frmOPJobs(FilterJobsTaskova.SaTaskovima));

            karticaTaskovi = new KarticaStanja("Taskovi", Ikona.Klipbord, "danas", "otvorenih");
            karticaTaskovi.Click += (s, e) => Otvori(new frmOPTaskovi());
            karticaTaskovi.KlikA += (s, e) => Otvori(new frmOPTaskovi(FilterTaskova.Danas));
            karticaTaskovi.KlikB += (s, e) => Otvori(new frmOPTaskovi(FilterTaskova.Otvoreni));
            karticaTaskovi.KlikC += (s, e) => Otvori(new frmOPTaskovi(FilterTaskova.Kasne));

            karticaMapa = new KarticaStanja("Mapa mreže", Ikona.Pin, "zatvora danas", null);
            karticaMapa.Click += (s, e) => Otvori(frmOPStanice.NaMapi());
            karticaMapa.KlikA += (s, e) => Otvori(frmOPStanice.NaMapi());

            karticaOsoblje = new KarticaStanja("Osoblje", Ikona.Korisnik, "raspoređeno", "slobodno");
            karticaOsoblje.Click += (s, e) => Otvori(new frmOPDostupnost());
            karticaOsoblje.KlikA += (s, e) => Otvori(new frmOPDostupnost(StatusOsobe.Rasporedjen));
            karticaOsoblje.KlikB += (s, e) => Otvori(new frmOPDostupnost(StatusOsobe.Slobodan));
            karticaOsoblje.KlikC += (s, e) => Otvori(new frmOPDostupnost(StatusOsobe.Odsutan));

            karticaJobs.Saveti("Aktivni poslovi (nisu predati) bez ijednog taska.", "Aktivni poslovi sa bar jednim taskom koji nije storniran.");
            karticaTaskovi.Saveti("Otvoreni i lansirani taskovi čiji period (ETD – ETA) obuhvata današnji datum.",
                "Svi otvoreni i lansirani taskovi, bez obzira na datum.", "Otvoreni taskovi čiji je kraj (ETA) pre današnjeg datuma.");
            karticaMapa.Saveti("Telegrami sa zatvorom (cela pruga ili svi koloseci) koji su u toku ili još važe danas.", null);
            karticaOsoblje.Saveti("Aktivne osobe u planiranju koje upravo sada imaju poslat ili prihvaćen zadatak.",
                "Aktivne osobe u planiranju bez odsustva i slobodnog dana danas, koje sada nisu raspoređene.",
                "Aktivne osobe u planiranju sa odsustvom (godišnji, bolovanje, ostalo) ili slobodnim danom danas.");

            foreach (KarticaStanja k in new[] { karticaJobs, karticaTaskovi, karticaMapa, karticaOsoblje })
                hero.DodajKarticu(k);
            PrikaziCekanje();

            hero.OsveziKartice += (s, e) => { var _ = OsveziKartice(); };
            Shown += (s, e) => PrikaziKarticeZaModul();
            tajmerKartica.Tick += (s, e) => { if (KarticeVidljive) { var _ = OsveziKartice(); } };
            Activated += (s, e) =>
            {
                if (KarticeVidljive && DateTime.Now - poslednjeOsvezavanje >= NajmanjeIzmedjuOsvezavanja) { var _ = OsveziKartice(); }
            };
            Resize += (s, e) => PodesiTajmerKartica();
            Disposed += (s, e) => tajmerKartica.Dispose();
        }

        private bool KarticeVidljive => aktivniModul == PocetniModul && Visible && WindowState != FormWindowState.Minimized;

        // Poziva se pri promeni modula: tajmer radi samo dok se Transport vidi, a povratak na Transport odmah osvežava.
        private void PrikaziKarticeZaModul()
        {
            if (hero == null || karticaJobs == null) return;
            hero.PrikaziPrecice(aktivniModul == PocetniModul);
            PodesiTajmerKartica();
            if (KarticeVidljive && IsHandleCreated) { var _ = OsveziKartice(); }
        }

        private void PodesiTajmerKartica()
        {
            tajmerKartica.Enabled = KarticeVidljive;
        }

        private static void Otvori(Saobracaj.Tema.BaseForm forma)
        {
            bool pravo = forma is frmOPJobs j ? j.Pravo : forma is frmOPTaskovi t ? t.Pravo : forma is frmOPStanice m ? m.Pravo : forma is frmOPDostupnost d && d.Pravo;
            if (pravo) forma.Show(); else forma.Dispose();
        }

        private void PrikaziCekanje()
        {
            karticaJobs.Postavi(KarticaStanja.Ucitava, KarticaStanja.Ucitava, false, false);
            karticaTaskovi.Postavi(KarticaStanja.Ucitava, KarticaStanja.Ucitava, false, false);
            karticaMapa.Postavi(KarticaStanja.Ucitava, null, false, false);
            karticaOsoblje.Postavi(KarticaStanja.Ucitava, KarticaStanja.Ucitava, false, false);
        }

        private async Task OsveziKartice()
        {
            if (karticeSeUcitavaju || karticaJobs == null) return;
            karticeSeUcitavaju = true;
            poslednjeOsvezavanje = DateTime.Now;
            try
            {
                PravaKartica prava = pravaKartica;
                StanjeKartica stanje = null;
                try
                {
                    await Task.Run(() =>
                    {
                        // Prava se čitaju jednom po pokretanju programa; bez njih brojevi se ipak učitavaju.
                        if (prava == null)
                            try { prava = KarticeRepo.Prava(); }
                            catch (Exception) { }
                        stanje = KarticeRepo.Ucitaj();
                    });
                }
                catch (Exception) { }
                if (IsDisposed) return;
                if (prava != null) pravaKartica = prava;
                PrikaziStanje(stanje, pravaKartica);
            }
            finally
            {
                karticeSeUcitavaju = false;
            }
        }

        // Bez veze sa bazom brojevi su „–“, a kartice i dalje otvaraju forme (pravo proverava sama forma).
        private void PrikaziStanje(StanjeKartica s, PravaKartica prava)
        {
            karticaJobs.Omogucena = prava == null || prava.Jobs;
            karticaTaskovi.Omogucena = prava == null || prava.Taskovi;
            karticaMapa.Omogucena = prava == null || prava.Mapa;
            karticaOsoblje.Omogucena = prava == null || prava.Osoblje;

            karticaJobs.Postavi(Broj(s?.JobsNovi), Broj(s?.JobsSaTaskovima), s?.JobsNovi > 0, false);
            if (s?.JobsNovi > 0 && s.NajstarijiNovi.HasValue)
            {
                int dana = Math.Max(0, (int)(s.Sada.Date - s.NajstarijiNovi.Value.Date).TotalDays);
                karticaJobs.Saveti("Aktivni poslovi (nisu predati) bez ijednog taska. Najstariji: " + (dana == 0 ? "danas" : "pre " + dana + (dana == 1 ? " dan" : " dana")) + ".",
                    "Aktivni poslovi sa bar jednim taskom koji nije storniran.");
            }

            int kasni = s?.TaskoviKasni ?? 0;
            karticaTaskovi.Postavi(Broj(s?.TaskoviDanas), Broj(s?.TaskoviOtvoreni), false, false, kasni > 0 ? "kasni: " + kasni : null, true);

            // Zatvor nije greška u planu, pa se broj ne ističe; bez zatvora ispod broja piše „nema zatvora“.
            karticaMapa.OpisA = s?.ZatvoriDanas == 0 ? "nema zatvora" : "zatvora danas";
            karticaMapa.Postavi(Broj(s?.ZatvoriDanas), null, false, false);

            karticaOsoblje.Postavi(Broj(s?.OsobljeRasporedjeno), Broj(s?.OsobljeSlobodno), false, s?.OsobljeSlobodno == 0,
                s?.OsobljeOdsutno.HasValue == true ? "odsutno: " + s.OsobljeOdsutno.Value : null);

            hero.PostaviAzurirano(s == null ? "Nije ažurirano" : "Ažurirano " + s.Sada.ToString("HH:mm"));
        }

        private static string Broj(int? n)
        {
            return n.HasValue ? n.Value.ToString("#,##0") : KarticaStanja.Greska;
        }
    }
}
