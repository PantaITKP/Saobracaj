using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Mobile
{
    public partial class frmPrijavaSmene
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Evidencija rada / Prijave i odjave", "Prijave/Odjave terenci tel", null, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(metroLabel1, txtSifra)
                .Dugmad(metroButton1, metroButton2, metroButton3));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1013, 668);
        }
    }
}
