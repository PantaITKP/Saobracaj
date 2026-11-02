using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.SyncForm
{
    public partial class frmPregledMobilni
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Evidencija rada / Prijave i odjave", "Analiza terenci tel", null);
            izgled.Zavrsi(gridGroupingControl1, 1000, 649);
        }
    }
}
