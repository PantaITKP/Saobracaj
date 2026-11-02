using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.SyncForm
{
    public partial class frmAnalizaAktivnosti
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Evidencija rada / Evidencija aktivnosti", "Pretraga aktivnosti", null);
            izgled.Zavrsi(gridGroupingControl1, 1000, 640);
        }
    }
}
