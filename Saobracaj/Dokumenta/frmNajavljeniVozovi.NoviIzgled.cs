using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmNajavljeniVozovi
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Kontrola dokumentacije", "Najavljeni vozovi", null);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, dateTimePicker1)
                .Polje(label2, dateTimePicker2)
                .Dugmad(button1));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1276, 873);
        }
    }
}
