using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Sifarnici
{
    public partial class frmTraseAnaliticki
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Šifarnici / Prevozni šifarnici", "Trase analitički", null);
            izgled.Zavrsi(gridGroupingControl1, 1000, 640);
        }
    }
}
