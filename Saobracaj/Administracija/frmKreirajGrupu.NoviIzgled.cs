using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Administracija
{
    public partial class frmKreirajGrupu
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Administracija / Prava pristupa", "Kreiraj Grupu", toolStrip1);
            IzgledForme.Akcija(toolStripButton1, "Dodeli Grupu", Ikona.Lista, Theme.Tekst);
            izgled.Mreza.Dodaj(new KarticaPolja("Podaci")
                .Polje(label3, txt_Sifra)
                .Polje(label1, txt_IdGrupe)
                .Polje(label2, txt_Naziv));
            izgled.Mreza.MaxKolona = 1;
            izgled.Zavrsi(dataGridView1, 1000, 766);
        }
    }
}
