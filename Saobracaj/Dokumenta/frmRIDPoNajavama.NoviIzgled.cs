using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmRIDPoNajavama
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Priprema / RID", "RID po najavama", null);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label17, dtpStvarnoPrimanje)
                .Polje(label1, dateTimePicker1)
                .Dugmad(button4));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView3, 1000, 640);
        }
    }
}
