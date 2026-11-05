using Saobracaj.Tema;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPTablicaMasa
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Šifarnici planiranja", "Tablica masa", null);

            polja.Izbor("SerijaID", "Serija")
                .IzborVrednosti("Sloj", "Sloj", "RUCNA", "FIZIKA", "NAUCENA")
                .Izbor("PrugaID", "Pruga")
                .IzborVrednosti("Smer", "Smer", "", "AB", "BA")
                .Izbor("StanicaOdID", "Relacija od")
                .Izbor("StanicaDoID", "Relacija do")
                .Broj("BrojLokomotiva", "Broj lokomotiva", 0, 2)
                .Broj("MaxMasaT", "Najveća masa (t)")
                .Datum("VaziOd", "Važi od", moguPrazno: true)
                .Datum("VaziDo", "Važi do", moguPrazno: true)
                .Tekst("Napomena", "Napomena", siroko: true)
                .Podrazumevano("BrojLokomotiva", 1)
                .Podrazumevano("Sloj", "RUCNA")
                .Dugmad(
                    EkranOP.Dugme("Nov", (s, e) => { tabela.ClearSelection(); polja.Popuni(null); }),
                    EkranOP.Dugme("Sačuvaj", (s, e) => Sacuvaj()),
                    EkranOP.Dugme("Obriši", (s, e) => Obrisi()),
                    EkranOP.Dugme("Uvoz iz Excela", (s, e) => Uvezi()),
                    EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }));
            tabela.SelectionChanged += (s, e) => { if (tabela.SelectedRows.Count > 0) polja.Popuni(EkranOP.Izabran(tabela)); };

            izgled.Mreza.MaxKolona = 1;
            izgled.Mreza.Dodaj(polja.Kartica);
            Load += (s, e) => { var _ = Osvezi(); };
            izgled.Zavrsi(tabela, 1100, 780);
        }
    }
}
