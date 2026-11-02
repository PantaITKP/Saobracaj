using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Servis
{
    public partial class frmNamirenja
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.ZadrziRaspored(this, "Servis / Namirenja", "Namirenje gorivom i uljem", null, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
        }
    }
}
