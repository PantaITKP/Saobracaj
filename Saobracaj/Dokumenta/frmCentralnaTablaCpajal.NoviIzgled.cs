using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmCentralnaTablaCpajal
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.ZadrziRaspored(this, "Transport / Tabla", "CT Čpajak", null);
        }
    }
}
