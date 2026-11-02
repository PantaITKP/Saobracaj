using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmEvidencijaRadaPromene
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Evidencija rada / Evidencija aktivnosti", "Promene na troškovima", null);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label6, cboZaposleni)
                .Polje(label1, dtpPredvidjenoPrimanje)
                .Polje(new Label { Text = "" }, dtpVremeDo)
                .Dugmad(button2, btnPretrazi));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1039, 640);
        }
    }
}
