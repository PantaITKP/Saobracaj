using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmAutomobiliKvarovi
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Servis / Automobili", "Prijavljeni kvarovi", null);
            izgled.Zavrsi(dataGridView1, 1000, 640);
        }
    }
}
