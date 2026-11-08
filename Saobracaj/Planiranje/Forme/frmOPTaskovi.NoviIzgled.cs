using System;
using System.Drawing;
using System.Windows.Forms;
using Saobracaj.Planiranje.Modeli;
using Saobracaj.Tema;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPTaskovi
    {
        private static readonly Color BojaStorno = ColorTranslator.FromHtml("#9AA0A6");
        private static readonly Color BojaUpozorenje = ColorTranslator.FromHtml("#8E1B2A");
        private static readonly Color BojaLansiran = ColorTranslator.FromHtml("#2A5878");
        private static readonly Color BojaZavrsen = ColorTranslator.FromHtml("#2F6B4F");

        // Naslov odeljka, pa trake i natpisi odozgo, a poslednja kontrola (tabela) popunjava ostatak.
        private static Control Odeljak(string naslov, params Control[] sadrzaj)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(Theme.Dp(6), Theme.Dp(2), Theme.Dp(6), Theme.Dp(4)) };
            for (int i = sadrzaj.Length - 1; i >= 0; i--)
            {
                if (i == sadrzaj.Length - 1) sadrzaj[i].Dock = DockStyle.Fill;
                else if (sadrzaj[i].Dock == DockStyle.None) sadrzaj[i].Dock = DockStyle.Top;
                p.Controls.Add(sadrzaj[i]);
            }
            p.Controls.Add(new Label { Text = naslov, Dock = DockStyle.Top, Height = Theme.Dp(22), Font = Theme.Tekstualni(10f, true), ForeColor = Theme.Tekst });
            return p;
        }

        private static Label Natpis(Label l, int visina)
        {
            l.Dock = DockStyle.Top;
            l.Height = Theme.Dp(visina);
            l.ForeColor = Theme.TekstSporedni;
            l.Font = Theme.Tekstualni(9f);
            return l;
        }

        // Uska traka u jednom redu (prelazi u novi red samo kad nema mesta): polja, čekiranja, tekst i dugmad.
        private static FlowLayoutPanel Traka(params Control[] stavke)
        {
            var t = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Padding = new Padding(0, Theme.Dp(2), 0, Theme.Dp(2)) };
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
                    c.Margin = new Padding(Theme.Dp(4), Theme.Dp(8), Theme.Dp(4), 0);
                }
                else
                {
                    c.Font = Theme.Tekstualni(9.5f);
                    c.Margin = new Padding(0, Theme.Dp(4), Theme.Dp(6), 0);
                }
                t.Controls.Add(c);
            }
            return t;
        }

        private static Label Oznaka(string tekst)
        {
            return new Label { Text = tekst, Font = Theme.Tekstualni(9f, true) };
        }

        // Podela zadržava odnos pri promeni veličine prozora, a korisnik može da pomeri granicu.
        private static SplitContainer Podela(Orientation smer, Control prvi, Control drugi, double udeo)
        {
            var s = new SplitContainer { Dock = DockStyle.Fill, Orientation = smer, SplitterWidth = Theme.Dp(6), BackColor = Theme.Pozadina2 };
            prvi.Dock = DockStyle.Fill;
            drugi.Dock = DockStyle.Fill;
            s.Panel1.BackColor = Theme.Pozadina;
            s.Panel2.BackColor = Theme.Pozadina;
            s.Panel1.Controls.Add(prvi);
            s.Panel2.Controls.Add(drugi);
            double odnos = udeo;
            bool postavljam = false;
            Func<int> ukupno = () => smer == Orientation.Vertical ? s.Width : s.Height;
            s.SizeChanged += (o, e) =>
            {
                int u = ukupno();
                if (u < Theme.Dp(120)) return;
                postavljam = true;
                try { s.SplitterDistance = Math.Max(Theme.Dp(40), Math.Min(u - Theme.Dp(40), (int)(u * odnos))); }
                catch (InvalidOperationException) { }
                postavljam = false;
            };
            s.SplitterMoved += (o, e) =>
            {
                int u = ukupno();
                if (!postavljam && u > 0) odnos = (double)s.SplitterDistance / u;
            };
            return s;
        }

        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Planiranje", "Taskovi", null);
            izgled.Mreza.MaxKolona = 1;

            dtOd.Width = Theme.Dp(110);
            dtDo.Width = Theme.Dp(110);
            FlowLayoutPanel zaglavlje = Traka(Oznaka("Od"), dtOd, Oznaka("Do"), dtDo,
                EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }),
                EkranOP.Dugme("Uzmi najave", (s, e) => { var _ = UzmiNajave(); }),
                chkSvi, chkStorno, lblStanje,
                EkranOP.Dugme("Gantogram lokomotiva", (s, e) => OtvoriGantogram(frmOPGanttTaskova.Vrsta.Lokomotive)),
                EkranOP.Dugme("Gantogram osoblja", (s, e) => OtvoriGantogram(frmOPGanttTaskova.Vrsta.Osoblje)));
            zaglavlje.Padding = new Padding(Theme.Dp(8), Theme.Dp(4), Theme.Dp(8), Theme.Dp(4));
            chkSvi.CheckedChanged += (s, e) => PrikaziTaskove();
            chkStorno.CheckedChanged += (s, e) => { var _ = OsveziTaskove(); };

            tabNajave.SelectionChanged += (s, e) => IzabranaNajavaPromenjena();
            PopuniSirinu(tabNajave);
            // Kao na nedeljnom planu: status i zastavica se vide bez pomeranja, a zastavica je plava.
            tabNajave.DataBindingComplete += (s, e) =>
            {
                if (tabNajave.Columns.Contains("Najava")) tabNajave.Columns["Najava"].MinimumWidth = Theme.Dp(170);
                if (tabNajave.Columns.Contains("Relacija")) tabNajave.Columns["Relacija"].MinimumWidth = Theme.Dp(220);
                if (tabNajave.Columns.Contains("Telegram")) tabNajave.Columns["Telegram"].FillWeight = 40;
            };
            tabNajave.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                string kolona = tabNajave.Columns[e.ColumnIndex].Name;
                if (kolona == "Telegram")
                {
                    e.CellStyle.ForeColor = Color.RoyalBlue;
                    e.CellStyle.SelectionForeColor = Color.RoyalBlue;
                    e.CellStyle.Font = Theme.Tekstualni(9.5f, true);
                }
                else if (kolona == "Taskovi" && Convert.ToString(e.Value).Length == 0)
                {
                    e.Value = "nema";
                    e.CellStyle.ForeColor = BojaUpozorenje;
                    e.FormattingApplied = true;
                }
            };
            tabNajave.CellToolTipTextNeeded += (s, e) =>
            {
                if (e.RowIndex < 0 || tabNajave.Columns[e.ColumnIndex].Name != "Telegram") return;
                e.ToolTipText = OpisTelegramaNajave(Convert.ToInt32(tabNajave.Rows[e.RowIndex].Cells["PosaoID"].Value));
            };

            EkranOP.StilKombinacija(tabRute);
            tabRute.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && !EkranOP.UcitavaSe(this)) NapraviIzIzabraneRute(); };
            Button napravi = EkranOP.Dugme("Napravi task iz izabrane rute", (s, e) => NapraviIzIzabraneRute());
            lblRute.Dock = DockStyle.None;
            FlowLayoutPanel trakaRuta = Traka(napravi, lblRute);

            tabTaskovi.MultiSelect = true;
            tabTaskovi.SelectionChanged += (s, e) => PrikaziDetaljeTaska();
            tabTaskovi.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && !EkranOP.UcitavaSe(this)) { var _ = Vremena(); } };
            tabTaskovi.CellFormatting += BojaTaska;
            tabTaskovi.DataBindingComplete += (s, e) =>
            {
                tabTaskovi.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
                if (tabTaskovi.Columns.Contains("Ruta")) tabTaskovi.Columns["Ruta"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            };

            FlowLayoutPanel akcije = Traka(
                EkranOP.Dugme("Split rute", (s, e) => { var _ = SplitRute(); }),
                EkranOP.Dugme("Split bruto", (s, e) => { var _ = SplitBruto(); }),
                EkranOP.Dugme("Spoji izabrane", (s, e) => { var _ = Spoji(); }),
                EkranOP.Dugme("Vremena", (s, e) => { var _ = Vremena(); }),
                EkranOP.Dugme("Lansiraj", (s, e) => { var _ = Lansiraj(); }),
                EkranOP.Dugme("Završi", (s, e) => { var _ = Zavrsi(); }),
                EkranOP.Dugme("Vrati u otvoren", (s, e) => { var _ = VratiUOtvoren(); }),
                EkranOP.Dugme("Storniraj", (s, e) => { var _ = Storniraj(); }),
                EkranOP.Dugme("Obriši taskove najave (proba)", (s, e) => { var _ = ObrisiTaskoveNajave(); }));
            FlowLayoutPanel lok = Traka(
                EkranOP.Dugme("+ Vučna", (s, e) => { var _ = DodajLokomotivu(TaskLokomotiva.Vucna); }),
                EkranOP.Dugme("+ Potisak", (s, e) => { var _ = DodajLokomotivu(TaskLokomotiva.Potisak); }),
                EkranOP.Dugme("Kao vučna", (s, e) => { var _ = PostaviVucnu(); }),
                EkranOP.Dugme("Ukloni", (s, e) => { var _ = UkloniLokomotivu(); }));
            FlowLayoutPanel osoblje = Traka(
                EkranOP.Dugme("+ Radno mesto", (s, e) => { var _ = DodajMesto(); }),
                EkranOP.Dugme("Izmeni", (s, e) => { var _ = IzmeniMesto(); }),
                EkranOP.Dugme("Ukloni", (s, e) => { var _ = UkloniMesto(); }),
                EkranOP.Dugme("Odaberi osoblje", (s, e) => OdaberiOsoblje()));
            tabOsoblje.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && !EkranOP.UcitavaSe(this)) { var _ = IzmeniMesto(); } };
            trakeAkcija.AddRange(new Control[] { akcije, lok, osoblje, napravi });
            PopuniSirinu(tabLokomotive);
            PopuniSirinu(tabOsoblje);
            PopuniSirinu(tabStanice);

            // Gore najave, ispod ponuda trasa (obe preko cele širine), a na dnu taskovi i detalji izabranog taska.
            Control najave = Odeljak("Najave (jobs)", tabNajave);
            Control trase = Odeljak("Ponuda trasa", trakaRuta, tabRute);
            Control listaTaskova = Odeljak("Taskovi (redosled po najavi i ruti)", akcije, tabTaskovi);
            Natpis(lblTask, 34);
            lblTask.AutoEllipsis = true;
            lblTask.Padding = new Padding(Theme.Dp(6), Theme.Dp(4), Theme.Dp(6), 0);
            // Gore desno je panel Lokomotive ili panel Osoblje (radna mesta taska); natpis izabranog taska je iznad oba.
            panelLokomotiva = Odeljak("Lokomotive taska", lok, tabLokomotive);
            panelOsoblja = Odeljak("Osoblje taska (radna mesta)", osoblje, tabOsoblje);
            panelOsoblja.Visible = false;
            var desnoGore = new Panel { Dock = DockStyle.Fill };
            desnoGore.Controls.Add(panelOsoblja);
            desnoGore.Controls.Add(panelLokomotiva);
            desnoGore.Controls.Add(lblTask);
            SplitContainer detalji = Podela(Orientation.Horizontal, desnoGore, Odeljak("Stanice rute taska", tabStanice), 0.45);
            SplitContainer dole = Podela(Orientation.Vertical, listaTaskova, detalji, 0.72);
            SplitContainer gore = Podela(Orientation.Horizontal, najave, trase, 0.55);
            SplitContainer sadrzaj = Podela(Orientation.Horizontal, gore, dole, 0.55);

            FlowLayoutPanel prikaz = Traka(Oznaka("Prikaži:"), chkPrikazNajave, chkPrikazTrasa, chkPrikazTaskova, chkPrikazLokomotiva, chkPrikazOsoblja);
            prikaz.Padding = new Padding(Theme.Dp(8), 0, Theme.Dp(8), Theme.Dp(4));
            foreach (CheckBox c in new[] { chkPrikazNajave, chkPrikazTrasa, chkPrikazTaskova, chkPrikazLokomotiva, chkPrikazOsoblja })
            {
                c.Checked = c != chkPrikazOsoblja;
                CheckBox ovaj = c;
                c.CheckedChanged += (s, e) =>
                {
                    // Lokomotive i osoblje dele isto mesto: uključivanje jednog gasi drugo.
                    if (ovaj.Checked && ovaj == chkPrikazOsoblja) chkPrikazLokomotiva.Checked = false;
                    if (ovaj.Checked && ovaj == chkPrikazLokomotiva) chkPrikazOsoblja.Checked = false;
                    PrikaziDelove(ovaj, sadrzaj, gore, dole);
                };
            }

            var okvir = new Panel();
            okvir.Controls.Add(sadrzaj);
            okvir.Controls.Add(prikaz);
            okvir.Controls.Add(zaglavlje);

            Load += (s, e) => { var _ = Osvezi(); };
            izgled.Zavrsi(okvir, 1500, 900);
        }

        private readonly CheckBox chkPrikazNajave = new CheckBox { Text = "Najave" };
        private readonly CheckBox chkPrikazTrasa = new CheckBox { Text = "Ponuda trasa" };
        private readonly CheckBox chkPrikazTaskova = new CheckBox { Text = "Taskovi" };
        private readonly CheckBox chkPrikazLokomotiva = new CheckBox { Text = "Lokomotive" };
        private readonly CheckBox chkPrikazOsoblja = new CheckBox { Text = "Osoblje" };
        private Control panelLokomotiva, panelOsoblja;

        // Sakriva i vraća delove ekrana; prostor sakrivenog dela preuzimaju ostali. Jedan deo uvek ostaje vidljiv.
        // Desni deo (sa stanicama rute) vidi se kad su uključene lokomotive ili osoblje.
        private void PrikaziDelove(CheckBox promenjen, SplitContainer sadrzaj, SplitContainer gore, SplitContainer dole)
        {
            bool najave = chkPrikazNajave.Checked, trase = chkPrikazTrasa.Checked, taskovi = chkPrikazTaskova.Checked;
            bool lok = chkPrikazLokomotiva.Checked || chkPrikazOsoblja.Checked;
            if (!najave && !trase && !taskovi && !lok)
            {
                promenjen.Checked = true;
                return;
            }
            panelOsoblja.Visible = chkPrikazOsoblja.Checked;
            panelLokomotiva.Visible = !chkPrikazOsoblja.Checked;
            // SplitContainer ne dozvoljava da oba dela budu skrivena, zato se prvo otvara ono što ostaje.
            gore.Panel1Collapsed = false;
            gore.Panel2Collapsed = false;
            dole.Panel1Collapsed = false;
            dole.Panel2Collapsed = false;
            sadrzaj.Panel1Collapsed = false;
            sadrzaj.Panel2Collapsed = false;
            if (!najave && trase) gore.Panel1Collapsed = true;
            if (najave && !trase) gore.Panel2Collapsed = true;
            if (!taskovi && lok) dole.Panel1Collapsed = true;
            if (taskovi && !lok) dole.Panel2Collapsed = true;
            if (!najave && !trase) sadrzaj.Panel1Collapsed = true;
            if (!taskovi && !lok) sadrzaj.Panel2Collapsed = true;
        }

        private static void PopuniSirinu(DataGridView g)
        {
            g.DataBindingComplete += (s, e) => g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        // Storniran je siv, lansiran plav, završen zelen, a upozorenje o vremenu crveno.
        private void BojaTaska(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || !tabTaskovi.Columns.Contains("Status")) return;
            DataGridViewRow r = tabTaskovi.Rows[e.RowIndex];
            string status = Convert.ToString(r.Cells["Status"].Value);
            string kolona = tabTaskovi.Columns[e.ColumnIndex].Name;
            if (status == StatusTaska.Naziv(StatusTaska.Storniran)) { e.CellStyle.ForeColor = BojaStorno; e.CellStyle.Font = Theme.Tekstualni(9f, false); }
            else if (kolona == "Upozorenje" && Convert.ToString(e.Value).Length > 0) e.CellStyle.ForeColor = BojaUpozorenje;
            else if (kolona == "Status" && status == StatusTaska.Naziv(StatusTaska.Lansiran)) e.CellStyle.ForeColor = BojaLansiran;
            else if (kolona == "Status" && status == StatusTaska.Naziv(StatusTaska.Zavrsen)) e.CellStyle.ForeColor = BojaZavrsen;
            if (kolona == "Redosled" || kolona == "Task") e.CellStyle.Font = Theme.Tekstualni(9f, true);
        }
    }
}
