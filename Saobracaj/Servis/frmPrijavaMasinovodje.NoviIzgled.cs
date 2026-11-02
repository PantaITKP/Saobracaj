using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Servis
{
    public partial class frmPrijavaMasinovodje
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.ZadrziRaspored(this, "Evidencija rada / Prijave i odjave", "Prijava odjava mašinovođe", null, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
        }
    }
}
