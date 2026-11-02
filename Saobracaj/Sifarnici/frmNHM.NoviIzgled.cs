using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Sifarnici
{
    public partial class frmNHM
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Šifarnici / Osnovni", "NHM", toolStrip1,
                () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            izgled.Mreza.MaxKolona = 1;

            izgled.Mreza.Dodaj(new KarticaPolja("NHM")
                .Polje(label1, txtSifra)
                .Polje(label2, txtBroj)
                .Polje(lblNaziv, txtNaziv, siroko: true)
                .Cekiranja(chkRid));

            izgled.Zavrsi(dataGridView1, 1000, 640);
        }
    }
}
