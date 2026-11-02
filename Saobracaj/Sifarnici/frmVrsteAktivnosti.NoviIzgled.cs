using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Sifarnici
{
    public partial class frmVrsteAktivnosti
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Evidencija rada / Vrste aktivnosti", "Vrste aktivnosti", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            IzgledForme.Akcija(toolStripButton1, "Cenovnici", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton2, "Prebacivanje cena u Arhivski cenonik", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton3, "Koeficjent Zaposlenog", Ikona.Lista, Theme.Tekst);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label7, txtOpis, siroko: true, visina: Theme.Dp(72))
                .Polje(label1, txtSifra)
                .Polje(lblNaziv, txtNaziv, siroko: true)
                .Polje(label18, txtCena, dodaci: new Control[] { label17 })
                .Polje(label3, txtFiksniDeo, dodaci: new Control[] { label2 })
                .Polje(label4, txtVremeVagon)
                .Polje(label6, txtMaxSati)
                .Polje(label5, txtMaxVagona)
                .Cekiranja(chkSmederevo, chkKragujevac, chkCG, chkRemont, chkMilsped, chkObracunPoSatu, chkPotrebanNalogodavac, chkUlaziUDnevnicu, chkPotrebanRazlog, chkPotrebnoVozilo, chkObaveznaNapomena));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1000, 640);
        }
    }
}
