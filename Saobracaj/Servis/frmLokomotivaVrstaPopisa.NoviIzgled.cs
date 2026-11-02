using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Servis
{
    public partial class frmLokomotivaVrstaPopisa
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Servis / Šifarnici", "Popisne stavke", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, txtSifra)
                .Polje(label2, cboLokomotiva, dodaci: new Control[] { button1 })
                .Polje(label3, cboVrstaPopisa, dodaci: new Control[] { button2 })
                .Polje(label11, txtKolicina)
                .Polje(label14, txtReferetnaKolicina));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1000, 640);
        }
    }
}
