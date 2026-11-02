using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmMupMesto
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Administracija / MUP Dozvole", "Trenutne adrese", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, txtSifra)
                .Polje(label6, txtAdresa, siroko: true, visina: Theme.Dp(55))
                .Polje(label4, cboZaposleni)
                .Cekiranja(chkStan));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1000, 640);
        }
    }
}
