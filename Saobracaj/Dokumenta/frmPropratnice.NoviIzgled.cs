using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmPropratnice
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.Akcija(toolStripButton1, "Sve propratnice", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton2, "Pošalji mail", Ikona.Mejl, Theme.Tekst);
            IzgledForme.ZadrziRaspored(this, "Priprema / Najave", "Propratnice", toolStrip1);
        }
    }
}
