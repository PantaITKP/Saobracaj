using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Administracija
{
    public partial class frmNotifikacije
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.ZadrziRaspored(this, "Administracija / Prava pristupa", "Notifikacije", toolStrip1);
        }
    }
}
