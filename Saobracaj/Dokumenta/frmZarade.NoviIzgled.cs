using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmZarade
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Evidencija rada / Evidencija aktivnosti", "Izveštaj zarade", null);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label6, cboZaposleni)
                .Polje(label1, dtpVremeOd)
                .Polje(label2, dtpVremeDo)
                .Cekiranja(chkMilsped, chkLokomotiva, chkVanLokomotive)
                .Dugmad(btnStampa, button1));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(reportViewer1, 1000, 640);
        }
    }
}
