using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Sifarnici
{
    partial class frmSerijeLokomotiva
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Šifarnici / Prevozni šifarnici", "Serije lokomotive", toolStrip1,
                () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            izgled.Mreza.MaxKolona = 1;

            izgled.Mreza.Dodaj(new KarticaPolja("Podaci o seriji")
                .Polje(label1, txtSifra)
                .Polje(label2, txtOznaka)
                .Polje(label3, txtOpis, siroko: true)
                .Polje(lblUkupnoT, txtTezina)
                .Polje(label4, txtSnaga));

            izgled.Zavrsi(dataGridView1, 1000, 640);
        }
    }
}
