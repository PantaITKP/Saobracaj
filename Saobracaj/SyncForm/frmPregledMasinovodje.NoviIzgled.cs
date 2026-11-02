using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.SyncForm
{
    public partial class frmPregledMasinovodje
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Evidencija rada / Prijave i odjave", "Analiza prijave odjave tablet", null);
            izgled.Zavrsi(gridGroupingControl1, 1000, 640);
        }
    }
}
