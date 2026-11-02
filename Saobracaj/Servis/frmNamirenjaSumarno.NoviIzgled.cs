using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Servis
{
    public partial class frmNamirenjaSumarno
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Servis / Namirenja", "Namirenja sumarno", null);
            izgled.Zavrsi(gridGroupingControl1, 1000, 640);
        }
    }
}
