using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmBolovanja
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Evidencija rada / Ostalo", "Bolovanja", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            IzgledForme.Akcija(toolStripButton1, "Dokumentacija", Ikona.Dokument, Theme.Tekst);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label7, txtSifra)
                .Polje(label21, dtpVremeOd)
                .Polje(label15, dtpVremeDo)
                .Polje(label6, txtUkupno)
                .Polje(label1, cboTipBolovanja, dodaci: new Control[] { label2 })
                .Polje(label4, cboZaposleni)
                .Polje(label8, txtNapomena, siroko: true, visina: Theme.Dp(72))
                .Dugmad(button3, button1, button2));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView2, 1000, 640);
        }
    }
}
