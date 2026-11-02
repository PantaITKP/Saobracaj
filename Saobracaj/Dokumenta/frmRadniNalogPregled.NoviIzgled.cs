using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmRadniNalogPregled
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Radni nalozi", "Po trasama", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            IzgledForme.Akcija(toolStripButton1, "Otvori radni nalog", Ikona.Sat, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton2, "Osveži", Ikona.Osvezi, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton3, "Novi radni nalog", Ikona.Sat, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton5, "Izvoz z ExceL - ZA", Ikona.Preuzmi, Theme.Tekst);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, txtSifra)
                .Polje(label3, txtSaobraca, siroko: true, visina: Theme.Dp(72))
                .Polje(label2, textBox1)
                .Polje(new Label { Text = "" }, txtNajava)
                .Polje(new Label { Text = "" }, txtTrasa)
                .Polje(label8, dtpVremeOd2)
                .Polje(label7, dtpVremeDo2)
                .Cekiranja(chkPR, chkPL, chkOD, chkLA, chkST, chkZA)
                .Dugmad(button4, button2, button5, button1, button3));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1036, 640);
        }
    }
}
