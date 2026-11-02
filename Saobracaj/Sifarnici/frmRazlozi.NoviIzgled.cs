using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Sifarnici
{
    public partial class frmRazlozi
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Šifarnici / Prevozi", "Razlozi", toolStrip1,
                () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            izgled.Mreza.MaxKolona = 1;

            izgled.Mreza.Dodaj(new KarticaPolja("Razlog")
                .Polje(label1, txtSifra)
                .Polje(label3, cboRazloziTip)
                .Polje(label2, txtOpis, siroko: true));

            izgled.Zavrsi(dataGridView1, 960, 600);
        }
    }
}
