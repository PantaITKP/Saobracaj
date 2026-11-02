using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Sifarnici
{
    public partial class frmStanice
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Šifarnici / Prevozni šifarnici", "Stanice", toolStrip1,
                () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            izgled.Mreza.MaxKolona = 1;

            izgled.Mreza.Dodaj(new KarticaPolja("Podaci o stanici")
                .Polje(label1, txtSifra)
                .Polje(lblNaziv, txtOpis)
                .Polje(label4, txtLongitude)
                .Polje(label5, txtLatitude)
                .Polje(label3, txtKod)
                .Polje(label2, cboDrzava)
                .Cekiranja(chkGranicna));

            izgled.Zavrsi(dataGridView1, 1000, 680);
        }
    }
}
