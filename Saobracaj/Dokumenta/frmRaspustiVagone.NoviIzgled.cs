using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmRaspustiVagone
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Radni nalozi", "Raspuštanje vagoni", null);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label8, cboRadniNalog)
                .Polje(label21, dtpVremeOd)
                .Polje(label9, cboStanicaOd)
                .Polje(label10, cboStanicaDo)
                .Polje(label2, cboStanicaIsklj)
                .Tekst(label3)
                .Dugmad(button3, button1, button2));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView3, 1000, 640);
        }
    }
}
