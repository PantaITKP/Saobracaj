using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmNajava20
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Priprema / Najave", "Najave tabla", null);
            izgled.Zavrsi(dataGridView1, 1142, 744);
        }
    }
}
