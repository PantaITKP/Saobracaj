using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Servis
{
    public partial class frmGrupaKvarovaAuto
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Servis / Automobili", "Grupe kvarova", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, txtSifra)
                .Polje(lblNaziv, txtOpis, siroko: true));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1000, 640);
        }
    }
}
