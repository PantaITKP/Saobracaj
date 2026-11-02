using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Administracija
{
    public partial class frmPolozenePruge
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Administracija / Dozvole mašinovođe", "Položene pruge", toolStrip1);
            IzgledForme.Akcija(toolStripButton1, "Položene Lokomotive", Ikona.Voz, Theme.Tekst);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, txt_Sifra)
                .Polje(label2, combo_Zaposleni)
                .Polje(label3, combo_Pruga, siroko: true)
                .Polje(label4, dateTimePicker1)
                .Dugmad(btn_zapolseni, btn_pruga));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1115, 647);
        }
    }
}
