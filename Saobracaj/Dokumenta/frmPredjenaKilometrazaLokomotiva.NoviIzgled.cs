using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmPredjenaKilometrazaLokomotiva
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Evidencija rada / Prijave i odjave", "Predjena KM u Periodu", null);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label15, dtpVremeDo)
                .Polje(label21, dtpVremeOd)
                .Dugmad(btnIzracunaj));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1000, 640);
        }
    }
}
