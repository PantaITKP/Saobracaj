using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmAutomobiliPregledPrijava
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.ZadrziRaspored(this, "Servis / Automobili", "Zaduzivanje / Razduzivanje", toolStrip1);
        }
    }
}
