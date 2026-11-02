using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmEvidencijaGodišnjihOdmora
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.Akcija(tsUnesiCenu, "Unesi cenu za zaposlenog", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton1, "Pripremi štampu/email", Ikona.Stampa, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton2, "LOG", Ikona.Sat, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton3, "Izvestaj excel", Ikona.Preuzmi, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton4, "Evidencija GO svi", Ikona.Lista, Theme.Tekst);
            IzgledForme.ZadrziRaspored(this, "Evidencija rada / Zahtevi", "Evidencija GO", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
        }
    }
}
