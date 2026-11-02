using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Testiranje
{
    public partial class TestiranjeStampa
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Uprava / Testiranje", "Štampa testa", null);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label3, cboGrupaTesta)
                .Polje(label1, cboKorisnik)
                .Dugmad(sfButton1, sfButton2));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(reportViewer1, 1000, 640);
        }
    }
}
