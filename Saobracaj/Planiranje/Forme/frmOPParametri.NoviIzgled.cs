using Saobracaj.Tema;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPParametri
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Šifarnici planiranja", "Parametri", null);

            polja.Tekst("Kljuc", "Ključ")
                .BrojPrazno("Vrednost", "Vrednost")
                .Izbor("StanicaID", "Stanica")
                .Izbor("StanicaDoID", "Stanica do")
                .Izbor("SerijaID", "Serija")
                .Tekst("Jedinica", "Jedinica")
                .Datum("VaziOd", "Važi od")
                .Datum("VaziDo", "Važi do", moguPrazno: true)
                .IzborVrednosti("Izvor", "Izvor", "", "RUCNO", "KNJIZICA")
                .Tekst("Opis", "Opis")
                .Dugmad(
                    EkranOP.Dugme("Nov", (s, e) => { tabela.ClearSelection(); polja.Popuni(null); }),
                    EkranOP.Dugme("Sačuvaj", (s, e) => Sacuvaj()),
                    EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }));
            tabela.SelectionChanged += (s, e) => { if (tabela.SelectedRows.Count > 0) polja.Popuni(EkranOP.Izabran(tabela)); };

            izgled.Mreza.MaxKolona = 1;
            izgled.Mreza.Dodaj(polja.Kartica);
            Load += (s, e) => { var _ = Osvezi(); };
            izgled.Zavrsi(tabela, 1100, 780);
        }
    }
}
