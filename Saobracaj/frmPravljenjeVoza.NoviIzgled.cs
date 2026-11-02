using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj
{
    public partial class frmPravljenjeVoza
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.ZadrziRaspored(this, "Transport / Radni nalozi", "Pokretanje vagoni", null);
        }
    }
}
