using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Servis
{
    public partial class frmPrijavaKvaraAuto
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.Akcija(tsb_New, "New", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(tsb_Save, "Save", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(tsb_Delete, "Delete", Ikona.Lista, Theme.Tekst);
            IzgledForme.ZadrziRaspored(this, "Servis / Automobili", "Prijava kvara Auto", toolStrip1);
        }
    }
}
