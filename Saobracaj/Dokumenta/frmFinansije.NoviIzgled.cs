using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmFinansije
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Obračuni / Putni nalozi", "Troškovi banaka", null, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, dtpVremeOd)
                .Polje(label2, dtpVremeDo)
                .Polje(label5, txtSifra)
                .Polje(label21, dtpVremePlaceno)
                .Polje(label4, cboZaposleni)
                .Dugmad(btnStampa, button1, button5, button8, button9, button2, button4, button3, button6, button7, button10, button11));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(tabControl1, 1023, 877);
        }
    }
}
