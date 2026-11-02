using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmPravoAktivnostiViseRadnika
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.ZadrziRaspored(this, "Evidencija rada / Vrste aktivnosti", "Prava više radnika", null);
        }
    }
}
