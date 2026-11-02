using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Mobile
{
    public partial class frmZavrsnaDokumentacija
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.ZadrziRaspored(this, "Priprema / Završna dokumentacija", "Pristigla mobilnim", null, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
        }
    }
}
