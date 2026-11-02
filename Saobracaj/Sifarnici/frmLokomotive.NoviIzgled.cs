using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Sifarnici
{
    public partial class frmLokomotive
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Šifarnici / Prevozni šifarnici", "Lokomotive", null);

            label7.Text = "Serija";
            label4.Text = "Masa teretnog vozila";
            izgled.Mreza.MaxKolona = 2;

            izgled.Mreza.Dodaj(new KarticaPolja("Lokomotiva")
                .Polje(label1, txtLokomotiva)
                .Polje(label5, txtPassword)
                .Polje(label7, cboSerija)
                .Polje(label4, txtMasa, dodaci: new Control[] { label6 })
                .Cekiranja(chkAktivna, chkDizel, cboLokomotiva, cboVozilo));

            izgled.Mreza.Dodaj(new KarticaPolja("Osoblje i akcije")
                .Polje(label2, comboBox1)
                .Polje(label3, comboBox2)
                .Dugmad(btnRacun, button1, button2, btn_Dokumenta, button3));

            izgled.Zavrsi(tabControl1, 1100, 720);
        }
    }
}
