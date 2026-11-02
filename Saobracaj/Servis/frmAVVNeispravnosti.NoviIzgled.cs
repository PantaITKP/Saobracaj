using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Servis
{
    public partial class frmAVVNeispravnosti
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Servis / Ostalo", "AVV neispravnosti", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, txtSifra)
                .Polje(lblNaziv, txtSifraKvara, siroko: true)
                .Polje(label2, txtOpisPostupka, siroko: true, visina: Theme.Dp(59)));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1000, 683);
        }
    }
}
