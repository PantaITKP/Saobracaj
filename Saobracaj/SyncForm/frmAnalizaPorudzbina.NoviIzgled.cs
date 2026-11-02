using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.SyncForm
{
    public partial class frmAnalizaPorudzbina
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Priprema / Porudžbine", "Pregled porudžbina", null);
            izgled.Zavrsi(gridGroupingControl1, 1000, 640);
        }
    }
}
