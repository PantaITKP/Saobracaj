using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Mobile
{
    public partial class frmAnalizaGOSum
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Evidencija rada / Zahtevi", "Pregled svi", null);
            izgled.Zavrsi(gridGroupingControl1, 1000, 640);
        }
    }
}
