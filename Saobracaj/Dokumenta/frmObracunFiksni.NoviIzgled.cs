using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmObracunFiksni
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Obračuni / Zarade", "Obračun I dela zarade", null);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label21, dtpVremeOd)
                .Polje(label15, dtpVremeDo)
                .Polje(label6, txtPassword)
                .Polje(label8, dtpVremeOd2)
                .Polje(label7, dtpVremeDo2)
                .Polje(label1, txtKurs)
                .Polje(label9, txtSatiMesec)
                .Polje(label4, txtMinimalac)
                .Polje(label2, txtPrazniciFond)
                .Tekst(label10)
                .Dugmad(btnIzracunaj, button9, button1, button2));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1500, 769);
        }
    }
}
