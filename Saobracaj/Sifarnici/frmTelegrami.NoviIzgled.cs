using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Sifarnici
{
    public partial class frmTelegrami
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Šifarnici / Telegrami", "Telegrami", toolStrip1);
            IzgledForme.Akcija(toolStripButton1, "Prikaži telegrame", Ikona.Lista, Theme.Tekst);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label7, combo_OdStanice)
                .Polje(label10, dt_VaziOd)
                .Polje(label13, dt_TrajeOd)
                .Polje(label2, txt_ID)
                .Polje(label1, txt_BrTelegrama)
                .Polje(label6, txt_kolosek)
                .Polje(label9, combo_DoStanice)
                .Polje(label11, dt_VaziDo)
                .Polje(label12, dt_TrajeDo)
                .Polje(label5, cboPruga)
                .Polje(label8, txt_Napomena, siroko: true, visina: Theme.Dp(45))
                .Polje(new Label { Text = "" }, txt_PDF)
                .Cekiranja(cb_Aktivni, cb_Narocita)
                .Dugmad(btn_pdf, btn_prikazi, button1, btn_dani, btn_Aktivni, btn_narocite, btn_svi, btn_Sacuvaj));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1256, 734);
        }
    }
}
