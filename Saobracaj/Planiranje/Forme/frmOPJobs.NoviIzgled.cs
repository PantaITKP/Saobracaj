using System;
using System.Windows.Forms;
using Saobracaj.Tema;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPJobs
    {
        // Uska traka u jednom redu, isto kao na ekranu Taskovi.
        private static FlowLayoutPanel Traka(params Control[] stavke)
        {
            var t = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Padding = new Padding(Theme.Dp(8), Theme.Dp(4), Theme.Dp(8), Theme.Dp(4)) };
            foreach (Control c in stavke)
            {
                var b = c as Button;
                if (b != null)
                {
                    ThemeApplier.StilDugmeta(b, false);
                    b.Height = Theme.Dp(28);
                    b.Width = TextRenderer.MeasureText(b.Text, Theme.Tekstualni(9f, true)).Width + Theme.Dp(22);
                    b.Margin = new Padding(0, Theme.Dp(2), Theme.Dp(6), Theme.Dp(2));
                }
                else if (c is CheckBox)
                {
                    ThemeApplier.StilCheckBoxa((CheckBox)c);
                    c.AutoSize = true;
                    c.Margin = new Padding(Theme.Dp(10), Theme.Dp(7), Theme.Dp(4), 0);
                }
                else if (c is Label)
                {
                    c.AutoSize = true;
                    c.ForeColor = Theme.TekstSporedni;
                    c.Font = Theme.Tekstualni(9f);
                    c.Margin = new Padding(Theme.Dp(8), Theme.Dp(8), Theme.Dp(4), 0);
                }
                t.Controls.Add(c);
            }
            return t;
        }

        private static Control Odeljak(string naslov, Control vrh, Control sadrzaj)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(Theme.Dp(6), Theme.Dp(2), Theme.Dp(6), Theme.Dp(4)) };
            sadrzaj.Dock = DockStyle.Fill;
            p.Controls.Add(sadrzaj);
            if (vrh != null) { vrh.Dock = DockStyle.Top; p.Controls.Add(vrh); }
            p.Controls.Add(new Label { Text = naslov, Dock = DockStyle.Top, Height = Theme.Dp(22), Font = Theme.Tekstualni(10f, true), ForeColor = Theme.Tekst });
            return p;
        }

        // Duge liste (stanice, partneri) se pretražuju kucanjem.
        private void Pretraga(UredjivacReda u, params string[] kolone)
        {
            foreach (string k in kolone)
            {
                var c = (ComboBox)u.Polje(k);
                c.AutoCompleteSource = AutoCompleteSource.ListItems;
                c.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            }
        }

        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Planiranje", "JOBS", null);
            izgled.Mreza.MaxKolona = 4;

            poljaOpste.Prikaz("ID", "Šifra")
                .Izbor("PlatilacID", "Platilac", true)
                .Izbor("OrganizatorID", "Organizator", true)
                .Izbor("TipPrevozaID", "Tip prevoza", true)
                .Izbor("OtpravnaID", "Otpravna")
                .Izbor("UputnaID", "Uputna")
                .Izbor("Status", "Status JOBS")
                .Tekst("Napomena", "Napomena", true, Theme.Dp(40));
            poljaRelacija.Izbor("PreuzimaOdID", "Preuzima od operatera", true)
                .Izbor("PredajeOperateruID", "Predaje operateru", true)
                .Izbor("Stanica1ID", "Stanica preuzimanja (1)", true)
                .Izbor("Stanica2ID", "Stanica istovara (2) – drugi pravac", true)
                .Izbor("Stanica3ID", "Stanica predaje (3)", true)
                .Datum("ETD", "ETD", true, true);
            poljaRoba1.Izbor("NHM1", "NHM 1").BrojPrazno("Bruto1", "Bruto 1 (t)").BrojPrazno("BrojKola1", "Broj kola 1").Izbor("SerijaKola1", "Serija kola 1")
                .BrojPrazno("Duzina1", "Dužina 1 (m)").BrojPrazno("Neto1", "Neto težina 1 (t)").Tekst("RIDBroj1", "RID broj 1").Tekst("OMBroj1", "OM broj 1");
            poljaRoba2.Izbor("NHM2", "NHM 2").BrojPrazno("Bruto2", "Bruto 2 (t)").BrojPrazno("BrojKola2", "Broj kola 2").Izbor("SerijaKola2", "Serija kola 2")
                .BrojPrazno("Duzina2", "Dužina 2 (m)").BrojPrazno("Neto2", "Neto težina 2 (t)").Tekst("RIDBroj2", "RID broj 2").Tekst("OMBroj2", "OM broj 2");
            Pretraga(poljaOpste, "PlatilacID", "OrganizatorID", "OtpravnaID", "UputnaID");
            Pretraga(poljaRelacija, "PreuzimaOdID", "PredajeOperateruID", "Stanica1ID", "Stanica2ID", "Stanica3ID");
            Pretraga(poljaRoba1, "NHM1");
            Pretraga(poljaRoba2, "NHM2");
            ((ComboBox)poljaRelacija.Polje("Stanica2ID")).SelectedIndexChanged += (s, e) => OmoguciPravac2();
            foreach (UredjivacReda u in SvaPolja) izgled.Mreza.Dodaj(u.Kartica);

            FlowLayoutPanel traka = Traka(
                EkranOP.Dugme("Novi", (s, e) => Novi()),
                EkranOP.Dugme("Sačuvaj", (s, e) => { var _ = Sacuvaj(); }),
                EkranOP.Dugme("Obriši", (s, e) => { var _ = Obrisi(); }),
                EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }),
                chkPlatilac, chkSvi,
                EkranOP.Dugme("LOG", (s, e) => PrikaziLog()),
                EkranOP.Dugme("Taskovi", (s, e) => OtvoriTaskove()),
                EkranOP.Dugme("Prenesi najave u JOBS (privremeno)", (s, e) => { var _ = PrenesiNajave(); }),
                lblStanje);
            chkSvi.CheckedChanged += (s, e) => { var _ = Osvezi(); };
            chkPlatilac.CheckedChanged += (s, e) => { if (!punjenje) FiltrirajPoPlatiocu(); };

            tabJobs.SelectionChanged += (s, e) => IzabranPosao();
            tabJobs.DataBindingComplete += (s, e) =>
            {
                tabJobs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                if (tabJobs.Columns.Contains("ETD")) tabJobs.Columns["ETD"].DefaultCellStyle.Format = "dd.MM.yyyy HH:mm";
            };
            tabTaskovi.DataBindingComplete += (s, e) => tabTaskovi.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            lblTaskovi.Dock = DockStyle.Top;
            lblTaskovi.Height = Theme.Dp(20);
            lblTaskovi.ForeColor = Theme.TekstSporedni;

            var podela = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterWidth = Theme.Dp(6) };
            podela.Panel1.Controls.Add(Odeljak("JOBS", null, tabJobs));
            podela.Panel2.Controls.Add(Odeljak("Taskovi izabranog posla", lblTaskovi, tabTaskovi));
            podela.SizeChanged += (s, e) => { if (podela.Width > Theme.Dp(200)) podela.SplitterDistance = (int)(podela.Width * 0.55); };

            var okvir = new Panel();
            okvir.Controls.Add(podela);
            okvir.Controls.Add(traka);

            Load += (s, e) => { var _ = UcitajSifarnike(); };
            izgled.Zavrsi(okvir, 1500, 900);
        }
    }
}
