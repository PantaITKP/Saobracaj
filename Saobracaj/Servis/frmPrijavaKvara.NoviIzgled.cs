using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Servis
{
    public partial class frmPrijavaKvara
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Servis / Obrada kvarova", "Prijava kvara", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            IzgledForme.Akcija(toolStripButton1, "Arhiviraj prethodnu godinu", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton2, "Arhiva", Ikona.Lista, Theme.Tekst);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, txtSifra)
                .Polje(label9, txtNapomena, siroko: true, visina: Theme.Dp(72))
                .Polje(label2, cboLokomotiva)
                .Polje(label3, cboPrijavio)
                .Polje(label5, dtpDatumPrijave)
                .Polje(label4, cboKvar)
                .Polje(label6, cboStatusKvara)
                .Polje(label7, cboPromenio)
                .Polje(label8, dtpDatumPromene)
                .Dugmad(btn_PromeniVise, button1));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1000, 718);
        }
    }
}
