using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.SyncForm
{
    public partial class frmPlanRada
    {
        private void PrimeniNoviIzgled()
        {
            ThemeApplier.SamoTema(this);
        }
    }
}
