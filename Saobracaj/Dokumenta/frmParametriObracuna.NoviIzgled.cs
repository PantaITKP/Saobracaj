using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmParametriObracuna
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Obračuni / Zarade", "Parametri obrčuna", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, txtSifra)
                .Polje(label13, txtMinimalna));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(null, 1000, 560);
        }
    }
}
