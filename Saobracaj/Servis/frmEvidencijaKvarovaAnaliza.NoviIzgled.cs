using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Servis
{
    public partial class frmEvidencijaKvarovaAnaliza
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Servis / Obrada kvarova", "Analiza evidencije kvarova", null);
            izgled.Zavrsi(gridGroupingControl1, 1000, 640);
        }
    }
}
