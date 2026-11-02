using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmNajaveBezTeretnice
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Priprema / Teretnice", "Teretnice bez najave", null);
            izgled.Zavrsi(dataGridView1, 1000, 640);
        }
    }
}
