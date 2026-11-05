using System.Windows.Forms;
using Saobracaj.Tema;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPLokomotive
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Šifarnici planiranja", "Lokomotive i serije", null);

            poljaLok.Tekst("Broj", "Broj")
                .Tekst("Naziv", "Naziv")
                .Izbor("SerijaID", "Serija")
                .IzborVrednosti("Vlasnistvo", "Vlasništvo", "SOPSTVENA", "ZAKUP")
                .Tekst("Zakupodavac", "Zakupodavac")
                .Tekst("TechnodisID", "Technodis ID")
                .Izbor("MaticnaStanicaID", "Matična stanica")
                .BrojPrazno("MasaT", "Masa (t)")
                .Cekiranja("ImaGPS", "GPS", "Aktivna", "Aktivna")
                .Podrazumevano("Aktivna", true)
                .Podrazumevano("Vlasnistvo", "SOPSTVENA")
                .Dugmad(
                    EkranOP.Dugme("Nova", (s, e) => { tabLokomotive.ClearSelection(); poljaLok.Popuni(null); }),
                    EkranOP.Dugme("Sačuvaj", (s, e) => SacuvajLokomotivu()),
                    EkranOP.Dugme("Dopuni iz starog šifarnika", (s, e) => Dopuni()),
                    EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }));
            tabLokomotive.SelectionChanged += (s, e) => { if (tabLokomotive.SelectedRows.Count > 0) poljaLok.Popuni(EkranOP.Izabran(tabLokomotive)); };

            poljaSer.Tekst("Oznaka", "Oznaka")
                .Tekst("Opis", "Opis")
                .IzborVrednosti("VrstaVuce", "Vrsta vuče", "", "E", "D")
                .BrojPrazno("MasaT", "Masa (t)")
                .BrojPrazno("SnagaKW", "Snaga (kW)")
                .BrojPrazno("VmaxKmh", "Vmax (km/h)")
                .BrojPrazno("DuzinaM", "Dužina (m)")
                .Cekiranja("Aktivna", "Aktivna")
                .Podrazumevano("Aktivna", true)
                .Dugmad(
                    EkranOP.Dugme("Nova", (s, e) => { tabSerije.ClearSelection(); poljaSer.Popuni(null); }),
                    EkranOP.Dugme("Sačuvaj", (s, e) => SacuvajSeriju()),
                    EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }));
            tabSerije.SelectionChanged += (s, e) => { if (tabSerije.SelectedRows.Count > 0) poljaSer.Popuni(EkranOP.Izabran(tabSerije)); };

            poljaZau.Izbor("LokomotivaID", "Lokomotiva")
                .IzborVrednosti("Vrsta", "Vrsta", "SERVIS", "NAMENSKA", "PROJEKAT", "OSTALO")
                .Datum("Od", "Od", saVremenom: true)
                .Datum("Do", "Do", saVremenom: true)
                .Tekst("Projekat", "Projekat")
                .Tekst("Napomena", "Napomena")
                .Dugmad(
                    EkranOP.Dugme("Nova", (s, e) => { tabZauzetost.ClearSelection(); poljaZau.Popuni(null); }),
                    EkranOP.Dugme("Sačuvaj", (s, e) => SacuvajZauzetost()),
                    EkranOP.Dugme("Obriši", (s, e) => ObrisiZauzetost()),
                    EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }));
            tabZauzetost.SelectionChanged += (s, e) => { if (tabZauzetost.SelectedRows.Count > 0) poljaZau.Popuni(EkranOP.Izabran(tabZauzetost)); };

            var tabovi = new TabControl();
            var s1 = new TabPage("Lokomotive");
            var s2 = new TabPage("Serije");
            var s3 = new TabPage("Zauzetost");
            EkranOP.Strana(s1, poljaLok.Kartica, tabLokomotive);
            EkranOP.Strana(s2, poljaSer.Kartica, tabSerije);
            EkranOP.Strana(s3, poljaZau.Kartica, tabZauzetost);
            tabovi.TabPages.Add(s1);
            tabovi.TabPages.Add(s2);
            tabovi.TabPages.Add(s3);

            Load += (s, e) => { var _ = Osvezi(); };
            izgled.Zavrsi(tabovi, 1200, 780);
        }
    }
}
