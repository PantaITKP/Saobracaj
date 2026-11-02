using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmIzracunZarada
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Obračuni / Zarade", "Obračun zarade", null);
            izgled.Mreza.Dodaj(new KarticaPolja("Osnovni podaci")
                .Polje(label21, dtpVremeOd)
                .Polje(label8, dtpVremeOd2)
                .Polje(label1, txtKurs)
                .Polje(label3, txtSmanjenje, dodaci: new Control[] { button3 })
                .Polje(label6, txtPassword));
            izgled.Mreza.Dodaj(new KarticaPolja("Dodatni podaci")
                .Polje(label15, dtpVremeDo)
                .Polje(label7, dtpVremeDo2)
                .Polje(label2, txtZarada, dodaci: new Control[] { button2 })
                .Polje(label9, txtSatiMesec));
            izgled.Mreza.Dodaj(new KarticaPolja("Ostali podaci")
                .Polje(label5, dtpZakljucavanjeSmene)
                .Polje(label4, txtMinimalac)
                .Polje(label11, txtPoreskoOslobodjenje)
                .Polje(label12, txtPrazniciFond)
                .Tekst(label10)
                .Tekst(label13)
                .Dugmad(btnIzracunaj, button10, button5, button7, button1, button8, button6, button4, button12, button9, button11));
            izgled.Zavrsi(tabControl1, 1143, 694);
        }
    }
}
