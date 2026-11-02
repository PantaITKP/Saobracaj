using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Administracija
{
    public partial class frmSistematizacijaPovezivanje
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Evidencija rada / Sistematizacija", "Veza sistematizacija", toolStrip1);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label3, cboSistematizacijaDincic)
                .Polje(label1, cboSistematizacija));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1000, 640);
        }
    }
}
