using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Tehnologija
{
    public partial class frmTehnologijaPregled
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Priprema / Porudžbine", "Tehnologije pregled", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            IzgledForme.Akcija(toolStripButton1, "Otvori", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton2, "Povuci na RN", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton3, "Refresh", Ikona.Osvezi, Theme.Tekst);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, txtSifra));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1000, 640);
        }
    }
}
