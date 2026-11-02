using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmEvidencijaRAdaNeplaceno
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.ZadrziRaspored(this, "Obračuni / Putni nalozi", "Neplaćeno sređivanje", null, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
        }
    }
}
