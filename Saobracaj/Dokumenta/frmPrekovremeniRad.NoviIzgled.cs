using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmPrekovremeniRad
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Evidencija rada / Ostalo", "Prekovremeni/Praznik rad", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label7, txtSifra)
                .Polje(label21, dtpVremeOd)
                .Polje(label15, dtpVremeDo)
                .Polje(label6, txtUkupno)
                .Polje(label4, cboZaposleni)
                .Polje(label8, txtNapomena, siroko: true, visina: Theme.Dp(72))
                .Cekiranja(chkRadPraznikom)
                .Dugmad(button1, button2, button3, button4, button5, btnPosaljiMail));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView2, 1067, 702);
        }
    }
}
