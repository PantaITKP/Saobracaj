using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Sifarnici
{
    public partial class frmVagoniSerije
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Šifarnici / Prevozi", "Vagoni serije", toolStrip1);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, txt_ID)
                .Polje(label2, txt_Serija));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1000, 640);
        }
    }
}
