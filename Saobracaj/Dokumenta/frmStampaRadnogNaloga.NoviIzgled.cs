using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmStampaRadnogNaloga
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Radni nalozi", "Štampa radnog naloga", null, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, txtSifra)
                .Dugmad(btnStampa));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(reportViewer1, 1000, 640);
        }
    }
}
