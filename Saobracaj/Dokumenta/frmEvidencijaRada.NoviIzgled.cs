using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmEvidencijaRada
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.Akcija(toolStripButton1, "Plaćeno", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton3, "Računi", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton2, "Kontrolisano outside", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton4, "Log file", Ikona.Sat, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton5, "PPK", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton6, "Arhiiraj zapise", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton7, "Kontrolisano dispecer", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton8, "Kontrolisano Admin", Ikona.Lista, Theme.Tekst);
            IzgledForme.ZadrziRaspored(this, "Evidencija rada / Evidencija aktivnosti", "Unos rada radnika", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
        }
    }
}
