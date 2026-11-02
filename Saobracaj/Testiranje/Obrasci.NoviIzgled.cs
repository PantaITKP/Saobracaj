using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Testiranje
{
    public partial class Obrasci
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Uprava / Testiranje", "Štampa 0 br 6", null);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, cboKorisnik)
                .Polje(label5, dtpDatumTesta)
                .Polje(label10, txtMesto)
                .Dugmad(sfButton1));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(tabControl1, 1000, 640);
        }
    }
}
