using Saobracaj.Tema;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPTurnusi
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Šifarnici planiranja", "Turnusi", null);

            polja.Tekst("Naziv", "Naziv")
                .Tekst("Pocetak", "Početak (ČČ:MM)")
                .Broj("TrajanjeMin", "Trajanje (min)", 0, 1440)
                .Broj("DanaRada", "Dana rada", 0, 60)
                .Broj("DanaOdmora", "Dana odmora", 0, 60)
                .Cekiranja("Nocna", "Noćna", "Aktivan", "Aktivan")
                .Podrazumevano("Aktivan", true)
                .Podrazumevano("TrajanjeMin", 720)
                .Podrazumevano("DanaRada", 2)
                .Podrazumevano("DanaOdmora", 2)
                .Dugmad(
                    EkranOP.Dugme("Nov", (s, e) => { tabela.ClearSelection(); polja.Popuni(null); }),
                    EkranOP.Dugme("Sačuvaj", (s, e) => Sacuvaj()),
                    EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }));
            tabela.SelectionChanged += (s, e) => { if (tabela.SelectedRows.Count > 0) polja.Popuni(EkranOP.Izabran(tabela)); };

            poljaGen.Datum("Od", "Od")
                .Datum("Do", "Do")
                .Dugmad(EkranOP.Dugme("Generiši smene", (s, e) => { var _ = Generisi(); }));

            izgled.Mreza.MaxKolona = 2;
            izgled.Mreza.Dodaj(polja.Kartica);
            izgled.Mreza.Dodaj(poljaGen.Kartica);

            Load += (s, e) => { var _ = Osvezi(); };
            izgled.Zavrsi(tabela, 1100, 720);
        }
    }
}
