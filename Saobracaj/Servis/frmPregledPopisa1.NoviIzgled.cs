using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Servis
{
    public partial class frmPregledPopisa1
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Servis / Popis lokomotiva", "Popis pregled 1", null);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label2, cboLokomotiva)
                .Dugmad(button2, button1, button3));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1000, 640);
        }
    }
}
