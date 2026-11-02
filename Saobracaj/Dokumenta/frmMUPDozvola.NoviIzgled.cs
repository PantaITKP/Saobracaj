using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmMUPDozvola
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Administracija / MUP Dozvole", "MUP dozvole", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            IzgledForme.Akcija(toolStripButton2, "Pošalji e mail", Ikona.Mejl, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton1, "Štampa aktivnosti", Ikona.Stampa, Theme.Tekst);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label15, dtpVremeDo)
                .Polje(label1, txtSifra)
                .Polje(label4, cboZaposleni)
                .Polje(label21, dtpVremeOd)
                .Polje(label2, txtJMBG)
                .Polje(label3, txtRadnoMesto)
                .Polje(label5, cboLokacija)
                .Polje(label6, txtAdresa, siroko: true, visina: Theme.Dp(55))
                .Polje(label18, txtEmail)
                .Polje(label7, txtRelacija)
                .Polje(label8, cboAutomobili)
                .Cekiranja(chkStan, chkSluzbeni)
                .Dugmad(btnPosaljiMail, btnStampa));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(tabControl1, 1000, 802);
        }
    }
}
