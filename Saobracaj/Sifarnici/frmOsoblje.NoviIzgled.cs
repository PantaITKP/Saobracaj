using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Sifarnici
{
    public partial class frmOsoblje
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Šifarnici / Osnovni", "Osoblje", toolStrip1);
            izgled.Mreza.MaxKolona = 1;
            IzgledForme.Akcija(toolStripButton1, "Dodeljivanje lokomotive", Ikona.Voz, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton2, "Dodeljivanje pruge", Ikona.Lista, Theme.Tekst);

            izgled.Mreza.Dodaj(new KarticaPolja("Radnik i dodele")
                .Polje(label1, cboPartneri, siroko: true)
                .Polje(label2, cboLokomotiva)
                .Polje(label3, cboPruga)
                .Cekiranja(chkMasinovodja, chkPomocnik, chkVozovodja, chkPregledacKola, chkManevrista));

            izgled.Zavrsi(tabControl1, 1100, 720);
        }
    }
}
