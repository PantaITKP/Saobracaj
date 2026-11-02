using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Sifarnici
{
    public partial class frmCenaPoRadniku
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Evidencija rada / Vrste aktivnosti", "Cene po radniku", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, txtSifra)
                .Polje(label2, cboZaposleni)
                .Polje(label6, txtCena)
                .Cekiranja(chkPoSatu));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1000, 640);
        }
    }
}
