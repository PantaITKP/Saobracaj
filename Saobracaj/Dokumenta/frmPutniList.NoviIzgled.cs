using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmPutniList
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Radni nalozi", "Putni list", null);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(new Label { Text = "" }, textBox1)
                .Dugmad(button1));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1000, 710);
        }
    }
}
