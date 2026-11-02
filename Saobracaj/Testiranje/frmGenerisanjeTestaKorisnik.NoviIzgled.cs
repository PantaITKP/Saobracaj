using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Testiranje
{
    public partial class frmGenerisanjeTestaKorisnik
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.Akcija(toolStripButton1, "Generiši Test", Ikona.Lista, Theme.Tekst);
            IzgledForme.ZadrziRaspored(this, "Uprava / Testiranje", "Generisanje testa", toolStrip1);
        }
    }
}
