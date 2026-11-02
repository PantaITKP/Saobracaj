using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmRAdniNalogPregledServisneUluge
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Radni nalozi", "Servisne usluge", null);
            izgled.Mreza.Dodaj(new KarticaPolja("Akcije")
                .Dugmad(button3));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1225, 824);
        }
    }
}
