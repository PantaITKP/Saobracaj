using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Sifarnici
{
    public partial class frmPruge
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Šifarnici / Prevozni šifarnici", "Pruge", toolStrip1,
                () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();

            IzgledForme.Akcija(toolStripTextBox1, "Spisak pruga", Ikona.Lista, Theme.Tekst);

            // Smer je bio posebna oznaka iznad kolona; sada je deo naziva polja, pa grupe više nisu potrebne.
            groupBox1.Visible = false;
            groupBox2.Visible = false;
            label10.Text = "MaxDV A→B";
            label11.Text = "Koloseci A→B";
            label13.Text = "MaxDV B→A";
            label12.Text = "Koloseci B→A";
            label4.Text = "Stanica do";
            label5.Text = "Stanica od";
            label29.Text = "Osovinsko opterećenje";

            izgled.Mreza.Dodaj(new KarticaPolja("Pruga")
                .Polje(label1, txtSifra)
                .Polje(label2, txtOznaka, dodaci: new Control[] { button2 })
                .Polje(lblNaziv, txtOpis, siroko: true)
                .Cekiranja(chkElektrificirana));

            izgled.Mreza.Dodaj(new KarticaPolja("Stavka: deonica")
                .Polje(RB, txtRB)
                .Polje(label28, cmbKlasa)
                .Polje(label5, StanicaOD)
                .Polje(label4, stanicaDO)
                .Polje(label3, RastojanjeKM, dodaci: new Control[] { label22, RastojanjeM, label23 })
                .Polje(label24, StacionazaKM, dodaci: new Control[] { label26, StacionazaM, label25 })
                .Polje(label6, Vaga)
                .Polje(label15, PutTer)
                .Polje(label19, Nagib, dodaci: new Control[] { label27 })
                .Polje(label29, txtOsovinsko, dodaci: new Control[] { label30 }));

            izgled.Mreza.Dodaj(new KarticaPolja("Stavka: otpori i brzine")
                .Polje(label21, MNU)
                .Polje(label20, MNP)
                .Polje(label17, MNOU)
                .Polje(label16, MNOP)
                .Polje(label7, VmaxL)
                .Polje(label8, VmaxD)
                .Polje(label18, OsOtpor, siroko: true)
                .Polje(label10, MaxDVA)
                .Polje(label11, KoliseciA)
                .Polje(label13, MaxDVB)
                .Polje(label12, KoliseciB)
                .Dugmad(button1, button3, button4));

            izgled.Zavrsi(dataGridView1, 1400, 900);
        }
    }
}
