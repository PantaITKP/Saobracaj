using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmOsnovnaZarada
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Obračuni / Zarade", "Osnovna zarada", toolStrip1);
            IzgledForme.Akcija(toolStripButton1, "Povuci iz plata", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton2, "Prikaži samo fiksne", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton3, "Prikaži one koji nisu fiksni", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton4, "Prikaži Log Dodatnih Prevoza", Ikona.Sat, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton5, "Praznici", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton6, "Unos Bonusa", Ikona.Lista, Theme.Tekst);
            groupBox1.Visible = false;
            izgled.Mreza.Dodaj(new KarticaPolja("Osnovni podaci")
                .Polje(label6, cboZaposleni)
                .Polje(label2, txtCiljna)
                .Polje(label18, txtMinimalna)
                .Polje(label1, txtPrviDeo)
                .Polje(label7, cboTipRadnika));
            izgled.Mreza.Dodaj(new KarticaPolja("Dodatni podaci")
                .Polje(label5, txtMinimalnaDrzavna)
                .Polje(label3, txtDrugiDeo)
                .Polje(label8, txtPrevoz)
                .Polje(label9, txtRegres)
                .Polje(label10, txtTopliObrok)
                .Polje(label13, txtProsecnaCena)
                .Polje(label14, txtProsecnaCena100)
                .Polje(label17, nmStazRanije, dodaci: new Control[] { label19 }));
            izgled.Mreza.Dodaj(new KarticaPolja("Ostali podaci")
                .Polje(label4, txtPassword)
                .Polje(label15, nmUkupno12)
                .Polje(label16, nmProsla)
                .Cekiranja(chkPregleFiksni, chkSmenski, chkParametar1, chkParametar2, chkFiksna, chkBenificirani, chkBonus)
                .Dugmad(btnStampa, metroButton1, btnMinimalnaDrzavna, metroButton2, metroButton4, btnPostaviPrviDeo, metroButton3, metroButton7));
            izgled.Mreza.Dodaj(new KarticaPolja("Fond sati")
                .Polje(label11, nmMesecniFS)
                .Polje(label12, nmRadnikFS)
                .Dugmad(metroButton5, metroButton6));
            izgled.Zavrsi(dataGridView1, 1000, 640);
        }
    }
}
