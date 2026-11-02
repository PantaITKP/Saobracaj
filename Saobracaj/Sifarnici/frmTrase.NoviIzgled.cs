using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Sifarnici
{
    public partial class frmTrase
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Šifarnici / Prevozni šifarnici", "Trase", toolStrip1,
                () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim());
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();

            IzgledForme.Akcija(toolStripButton1, "Trasa ruta", Ikona.Strelice, Theme.Tekst);
            // Dugme iz Designer-a bez teksta i bez događaja.
            toolStripButton2.Visible = false;

            label6.Text = "Voz / trasa oznaka";
            label10.Text = "Težina lokomotive";
            label26.Text = "Cena kalkulisana";
            label30.Text = "Regionalno";
            label32.Text = "Lokalno";

            izgled.Mreza.Dodaj(new KarticaPolja("Osnovni podaci")
                .Polje(label1, txtSifra)
                .Polje(label7, cmbRang)
                .Polje(label2, cmbPocetna)
                .Polje(label3, cboKrajnja, dodaci: new Control[] { btnSastaviStanice })
                .Polje(label4, txtRelacija, siroko: true)
                .Polje(label5, txtOpisRelacije, siroko: true, visina: Theme.Dp(44))
                .Polje(label6, txtVoz, siroko: true, dodaci: new Control[] { button1, button2 })
                .Polje(label39, txtTrase, siroko: true, dodaci: new Control[] { btnRacun, btnRacunSacuvaj }));

            izgled.Mreza.Dodaj(new KarticaPolja("Teret i cena")
                .Polje(label12, cboPrevoznik, siroko: true)
                .Polje(label8, txtTezinaVozaM, dodaci: new Control[] { label9 })
                .Polje(label10, txtTezinaLokM, dodaci: new Control[] { label11 })
                .Polje(label13, txtDuzinaLokomM, dodaci: new Control[] { label14 })
                .Polje(label16, txtProcenatKocenjaM)
                .Polje(label18, txtCenaM, dodaci: new Control[] { label17 })
                .Polje(label26, txtCenaKalk, dodaci: new Control[] { label15 }));

            izgled.Mreza.Dodaj(new KarticaPolja("Rastojanje i vreme")
                .Polje(label19, txtRastojanjeM, dodaci: new Control[] { label20 })
                .Polje(label41, txtRastojanjeP, dodaci: new Control[] { label40 })
                .Polje(label27, txtMagistralno, dodaci: new Control[] { label28 })
                .Polje(label30, txtRegionalno, dodaci: new Control[] { label29 })
                .Polje(label32, txtLokalne, dodaci: new Control[] { label31 })
                .Polje(label33, txtDana)
                .Polje(label21, dtpVremePolaska)
                .Polje(label22, dtpVremeDolaska)
                .Polje(label23, txtDuzinaTrajanjaM, dodaci: new Control[] { label24 }));

            izgled.Mreza.Dodaj(new KarticaPolja("Tehnički podaci")
                .Polje(label36, txtNajveciOtpor)
                .Polje(label35, txtNajkraciKolosek)
                .Polje(label34, txtOsovinskoOpterecenje)
                .Polje(label25, cboGodinaVazenja)
                .Polje(label38, txtDizel)
                .Polje(label37, txtElektro)
                .Cekiranja(chkDE, chkRezi));

            izgled.Zavrsi(tabControl1, 1400, 900);
        }
    }
}
