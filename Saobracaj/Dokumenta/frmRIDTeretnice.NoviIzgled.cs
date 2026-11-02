using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmRIDTeretnice
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Priprema / RID", "Teretnice bez RID-a", null);
            izgled.Zavrsi(dataGridView1, 1000, 640);
        }
    }
}
