using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    public partial class frmAutomobili
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Administracija / Prava pristupa", "Automobili", toolStrip1, () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            IzgledForme.Akcija(toolStripButton1, "Skenirana dokumentacija", Ikona.Dokument, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton2, "Evidencija Servisa", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton3, "Evidencija registracija", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton4, "Evidencija troškova iz UF", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton5, "Evidencija kvarova", Ikona.Lista, Theme.Tekst);
            IzgledForme.Akcija(toolStripButton6, "Evidencija prijava i odjava", Ikona.Lista, Theme.Tekst);
            izgled.Mreza.Dodaj(new KarticaPolja("Osnovni podaci")
                .Polje(label1, txtSifra)
                .Polje(label3, txtMarka, siroko: true)
                .Polje(label6, txtRegBr)
                .Polje(label21, dtpDatumRegistracije)
                .Polje(label5, txtGorivo)
                .Polje(label8, txtKategorija)
                .Polje(label9, dtpVServisUradjen)
                .Polje(label10, txtVServisKM)
                .Polje(label11, dtpVServisSledeci)
                .Polje(label14, dtpMaliServisUradjen)
                .Polje(label13, txtMaliServisKM)
                .Polje(label12, dtpMaliServisSledeci));
            izgled.Mreza.Dodaj(new KarticaPolja("Dodatni podaci")
                .Polje(label2, cboZaposleni)
                .Polje(label35, txtModel)
                .Polje(label4, txtGodinaProizvodnje)
                .Polje(label7, txtZapreminaMotora)
                .Polje(label15, txtBrojPlombe1)
                .Polje(label16, txtBrojPlombe2)
                .Polje(label17, dtpPPAparatDatumOvere)
                .Polje(label18, dtpPPAparatDatumIsteka)
                .Polje(label19, txtPPAparatSeriskiBroj)
                .Polje(label20, dtpPrvaPomocDatumIsteka));
            izgled.Mreza.Dodaj(new KarticaPolja("Ostali podaci")
                .Polje(label34, cboMestoTroska)
                .Polje(label22, txtLokacijaLanci)
                .Polje(label33, txtCistocaSpolja)
                .Polje(label32, txtCistocaUnutra)
                .Polje(label23, txtZGDot)
                .Polje(label31, txtNivoUlja)
                .Polje(label24, txtZGLokacija)
                .Polje(label30, txtNepravilnosti)
                .Polje(label25, txtZGSare)
                .Polje(label28, txtLGgumeDOT)
                .Polje(label27, txtLGLokacija)
                .Polje(label26, txtLGSare)
                .Polje(label29, txtNapomena, siroko: true, visina: Theme.Dp(72))
                .Cekiranja(chkSluzbeni, chkImaTrougao, chkSajlaZaVucu, chkMarker, chkLanci));
            izgled.Zavrsi(dataGridView1, 1500, 860);
        }
    }
}
