using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Sifarnici
{
    public partial class frmTipPrevoza
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Šifarnici / Prevozi", "Tip prevoza", toolStrip1,
                () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            izgled.Mreza.MaxKolona = 1;

            izgled.Mreza.Dodaj(new KarticaPolja("Tip prevoza")
                .Polje(label1, txtSifra)
                .Polje(lblNaziv, txtNaziv));

            izgled.Zavrsi(dataGridView1, 960, 600);
        }
    }
}
