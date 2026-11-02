using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmPraznici
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Obračuni / Zarade", "Praznici", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, txtSifra)
                .Polje(lblNaziv, txtNaziv, siroko: true)
                .Polje(label21, dtpVremeOd)
                .Polje(label15, dtpVremeDo)
                .Cekiranja(chkAktivan));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1000, 640);
        }
    }
}
