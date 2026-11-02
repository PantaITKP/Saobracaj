using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Administracija
{
    public partial class frmFormePrava
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Administracija / Prava pristupa", "Forme Prava", null);
            izgled.Zavrsi(dataGridView1, 1013, 680);
        }
    }
}
