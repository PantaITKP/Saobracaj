using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Dokumenta
{
    // Novi izgled forme Najava. Kontrole iz Designer-a se samo prerasporede u kartice,
    // pa imena, događaji i sva logika u frmNajava.cs ostaju isti.
    public partial class frmNajava
    {
        private void PrimeniNoviIzgled()
        {
            IzgledForme.Akcija(toolStripButton4, "Refresh", Ikona.Osvezi, System.Drawing.Color.White);

            var izgled = new IzgledForme(this, "Transport / Kontrola dokumentacije", "Najava", toolStrip1,
                () => string.IsNullOrWhiteSpace(txtSifra.Text) ? "" : "#" + txtSifra.Text.Trim(),
                () => string.IsNullOrWhiteSpace(cboStatusPredaje.Text) ? "" : "Status: " + cboStatusPredaje.Text.Trim(),
                new ToolStripButton[] { toolStripButton4 });
            txtSifra.TextChanged += (s, e) => izgled.OsveziZaglavlje();
            cboStatusPredaje.TextChanged += (s, e) => izgled.OsveziZaglavlje();

            SrediSporedneAkcije();
            NapraviKartice(izgled.Mreza);
            SrediTabove();
            izgled.Zavrsi(tabNajave);
        }

        private void SrediSporedneAkcije()
        {
            var boja = Theme.Tekst;
            IzgledForme.Akcija(toolStripButton7, "Filtriraj po platiocu", Ikona.Filter, boja);
            IzgledForme.Akcija(toolStripButton8, "Sve najave", Ikona.Lista, boja);
            IzgledForme.Akcija(toolStripButton1, "Dokumenta prijem", Ikona.Dokument, boja);
            IzgledForme.Akcija(toolStripButton2, "Pošalji email", Ikona.Mejl, boja);
            IzgledForme.Akcija(toolStripButton3, "Štampaj izveštaj", Ikona.Stampa, boja);
            IzgledForme.Akcija(toolStripButton5, "Pronađi vagon", Ikona.Pretraga, boja);
            IzgledForme.Akcija(toolStripButton9, "Excel kašnjenja", Ikona.Preuzmi, boja);
            IzgledForme.Akcija(toolStripButton6, "LOG", Ikona.Sat, boja);

            toolStrip1.Items.Clear();
            toolStrip1.Items.AddRange(new ToolStripItem[] { toolStripButton7, toolStripButton8, toolStripButton1, toolStripButton2, toolStripButton3, toolStripButton5, toolStripButton9, toolStripButton6 });
        }

        private void NapraviKartice(MrezaKartica mreza)
        {
            label16.Text = "Predviđeno primanje";
            label17.Text = "Stvarno primanje";
            label21.Text = "Predviđena predaja";
            label15.Text = "Stvarna predaja";
            label13.Text = "Bruto težina";
            label32.Text = "Neto težina";
            label35.Text = "Tehnologija ID";
            label33.Text = "Porudžbina ID";
            lblNaziv.Text = "Br. vožnje";
            label7.Text = "RID broj";
            label24.Text = "OM broj";
            var oznakaNhm = new Label { Text = "NHM iz porudžbine" };

            ThemeApplier.StilTabele(dataGridView5);

            mreza.Dodaj(new KarticaPolja("Osnovni podaci")
                .Polje(label1, txtSifra, dodaci: new Control[] { txtVerzija })
                .Polje(lblNaziv, txtOpis)
                .Polje(label2, cmbVoz, dodaci: new Control[] { button2 })
                .Polje(label19, cboVozP)
                .Polje(label12, cboPosiljalac, siroko: true, dodaci: new Control[] { button3 })
                .Polje(label35, cboTehnologijaID, siroko: true)
                .Sadrzaj(dataGridView5, Theme.Dp(72)));

            mreza.Dodaj(new KarticaPolja("Relacija i stanice")
                .Polje(label3, cboPrevoznik, siroko: true, dodaci: new Control[] { button9, button1 })
                .Polje(label5, cboOtpravna, dodaci: new Control[] { button4 })
                .Polje(label4, cboUputna, dodaci: new Control[] { button5 })
                .Polje(label25, cboGranicna, dodaci: new Control[] { button6 })
                .Polje(label6, cboPrimalac, dodaci: new Control[] { button7 })
                .Polje(label44, cboOtpravnaGr, dodaci: new Control[] { button14 })
                .Polje(label43, cboUputnaGr, dodaci: new Control[] { button15 }));

            mreza.Dodaj(new KarticaPolja("Teret i plaćanje")
                .Polje(label27, cboPrevoznikZa, dodaci: new Control[] { button10 })
                .Polje(label26, cboPlatilac, dodaci: new Control[] { button8 })
                .Polje(label30, cboTipPrevoza, siroko: true)
                .Polje(label13, txtNetoTezina, obavezno: true, dodaci: new Control[] { label9 })
                .Polje(label11, txtDuzinaM, dodaci: new Control[] { label10 })
                .Polje(label14, txtBrojKola, dodaci: new Control[] { combo_SerijaVagona })
                .Polje(label32, txtNetoTezinaM, dodaci: new Control[] { label31 })
                .Cekiranja(chkTovareno));

            mreza.Dodaj(new KarticaPolja("Vremena")
                .Polje(label8, txtRelacija, siroko: true, obavezno: true)
                .Polje(label16, dtpPredvidjenoPrimanje)
                .Polje(label17, dtpStvarnoPrimanje)
                .Polje(label21, dtpPredvidjenaPredaja)
                .Polje(label15, dtpStvarnaPredaja));

            mreza.Dodaj(new KarticaPolja("Status i dokumentacija")
                .Polje(label18, cboStatusPredaje)
                .Polje(label33, multiColumnComboBox1, dodaci: new Control[] { button13 })
                .Polje(label29, txtDispecerRID)
                .Polje(label22, txtUgovor)
                .Polje(label24, txtRIDBroj)
                .Polje(label7, txtRID)
                .Polje(label23, cboNHM)
                .Polje(label36, cboNHM2)
                .Cekiranja(chkRID, chkAdHoc, chkCIM, chkImaPovrat, chkIzNajave));
            // label20 i txtRazlog su skriveni u Designer-u, a čuvanje čita txtRazlog.SelectedValue: ostaju na formi.

            mreza.Dodaj(new KarticaPolja("Napomene")
                .Polje(label42, txtNapomenaPriprema, siroko: true, visina: Theme.Dp(36))
                .Polje(label28, txtZadatak, visina: Theme.Dp(60))
                .Polje(label37, txtPorDodatno, visina: Theme.Dp(60))
                .Polje(oznakaNhm, txtNHMPorudzbina, siroko: true, visina: Theme.Dp(36)));
        }

        private void SrediTabove()
        {
            txtKomentar.BorderStyle = BorderStyle.None;
            ThemeApplier.Popuni(tabPage4, dataGridView3);
            ThemeApplier.Popuni(tabPage5, reportViewer1);
            ThemeApplier.Popuni(tabPage6, txtKomentar);
            ThemeApplier.Popuni(tabPage7, dataGridView4, Theme.Dp(44));
            ThemeApplier.Popuni(tabPage8, dataGridView6, Theme.Dp(46));
        }
    }
}
