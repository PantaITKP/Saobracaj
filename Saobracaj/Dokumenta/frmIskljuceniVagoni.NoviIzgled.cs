using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmIskljuceniVagoni
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Priprema / Teretnice", "Isključeni vagoni", null);
            izgled.Zavrsi(dataGridView3, 1000, 640);
        }
    }
}
