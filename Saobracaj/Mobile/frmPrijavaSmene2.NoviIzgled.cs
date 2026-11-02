using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Mobile
{
    public partial class frmPrijavaSmene2
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.ZadrziRaspored(this, "Evidencija rada / Prijave i odjave", "Pregled tekucih smena", null);
        }
    }
}
