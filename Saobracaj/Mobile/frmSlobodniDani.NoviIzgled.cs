using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Mobile
{
    public partial class frmSlobodniDani
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Evidencija rada / Zahtevi", "Mobilni obrada", null);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(autoLabel1, txtID)
                .Polje(autoLabel2, cboZaposleni)
                .Polje(autoLabel3, txtNapomena, siroko: true, visina: Theme.Dp(72))
                .Dugmad(metroButton1, metroButton2, metroButton3));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1013, 761);
        }
    }
}
