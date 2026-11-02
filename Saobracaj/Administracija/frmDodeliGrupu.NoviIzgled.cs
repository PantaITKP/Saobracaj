using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Administracija
{
    public partial class frmDodeliGrupu
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Administracija / Prava pristupa", "Dodela Grupe", toolStrip1);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, combo_Korisnik, siroko: true)
                .Polje(label2, combo_Grupa));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1000, 657);
        }
    }
}
