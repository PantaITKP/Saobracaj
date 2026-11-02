using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Servis
{
    public partial class frmPlombe
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Servis / Namirenja", "Plombe", toolStrip1);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label1, txt_ID)
                .Polje(label2, dateTimePicker1)
                .Polje(label3, combo_Korisnik)
                .Polje(label4, combo_Najava)
                .Polje(label5, txt_BrPlombi)
                .Polje(label6, txt_Slika)
                .Dugmad(btn_UcitajSliku, btn_SacuvajSliku, btn_Otvori));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1261, 847);
        }
    }
}
