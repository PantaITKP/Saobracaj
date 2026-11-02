using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.SyncForm
{
    public partial class frmNajavaArhivaAnaliza
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Priprema / Najave", "Sve najave", null);
            izgled.Zavrsi(gridGroupingControl1, 1000, 640);
        }
    }
}
