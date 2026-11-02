using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Agenti_jobs
{
    public partial class IskoriscenjeTrasa
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.ZadrziRaspored(this, "Transport / Tabla", "Iskoriscenje trasa", null);
        }
    }
}
