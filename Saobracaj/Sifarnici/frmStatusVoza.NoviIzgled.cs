using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Sifarnici
{
    public partial class frmStatusVoza
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Šifarnici / Prevozi", "Status vozova", toolStrip1,
                () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            izgled.Mreza.MaxKolona = 1;

            izgled.Mreza.Dodaj(new KarticaPolja("Status voza")
                .Polje(label1, txtSifra)
                .Polje(label2, txtOpis));

            izgled.Zavrsi(dataGridView1, 960, 600);
        }
    }
}
