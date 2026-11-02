using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmRegistator
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Uprava / Delovodna knjiga", "Delovodna knjiga", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            IzgledForme.Akcija(toolStripButton1, "Štampa", Ikona.Stampa, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton2, "Dokumenta", Ikona.Dokument, Theme.Tekst);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label9, txtSifra)
                .Polje(label3, txtPredmet, siroko: true, visina: Theme.Dp(54))
                .Polje(label1, cboPartner)
                .Polje(label2, cboZaposleni)
                .Polje(label4, dtpDatum)
                .Tekst(metroLabel5));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(tabControl1, 1115, 640);
        }
    }
}
