using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmTeretnicePregled
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Priprema / Teretnice", "Pregled teretnica", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            IzgledForme.Akcija(toolStripButton1, "Otvori teretnicu", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton2, "Refresh", Ikona.Osvezi, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton3, "Preslikaj teretnicu", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton4, "Prijemne", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton5, "Prevozne", Ikona.Voz, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton6, "Komercijalne", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton7, "Random vrednosti", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton8, "Predajne", Ikona.Lista, Theme.Tekst);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, txtSifra)
                .Polje(label12, cboPosiljalac)
                .Polje(label5, cboStanicaOd)
                .Polje(label4, cboStanicaDo));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1225, 684);
        }
    }
}
