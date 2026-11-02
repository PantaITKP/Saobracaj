using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.SyncForm
{
    public partial class frmPregledPopisa2
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Servis / Popis lokomotiva", "Popis pregled 2", null);
            izgled.Zavrsi(gridGroupingControl1, 1000, 640);
        }
    }
}
