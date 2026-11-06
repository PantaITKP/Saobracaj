using System;
using System.Drawing;
using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPPosao
    {
        private static Control Odeljak(string naslov, params Control[] sadrzaj)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(Theme.Dp(6)) };
            for (int i = sadrzaj.Length - 1; i >= 0; i--) p.Controls.Add(sadrzaj[i]);
            p.Controls.Add(new Label { Text = naslov, Dock = DockStyle.Top, Height = Theme.Dp(24), Font = Theme.Tekstualni(10f, true), ForeColor = Theme.Tekst });
            return p;
        }

        private static Label Natpis(Label l, int visina)
        {
            l.Dock = DockStyle.Top;
            l.Height = Theme.Dp(visina);
            l.ForeColor = Theme.Tekst;
            l.Font = Theme.Tekstualni(9f);
            return l;
        }

        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Planiranje", "Posao", null);
            izgled.Mreza.MaxKolona = 1;
            Natpis(lblNaslov, 26).Font = Theme.Tekstualni(11f, true);

            var dno = new KarticaPolja("Izbor lokomotiva");
            dno.Tekst(lblIzabrano);
            dno.Dugmad(
                EkranOP.Dugme("Potvrdi lokomotive i pošalji dispečeru", (s, e) => { var _ = Potvrdi(); }),
                EkranOP.Dugme("Odbaci izbor", (s, e) =>
                {
                    izabran = null;
                    lblIzabrano.Text = "Izabrano: ništa";
                    if (rezultat != null) PrikaziProvere(new System.Collections.Generic.List<Modeli.LokomotivaOP>());
                }),
                EkranOP.Dugme("Nazad na plan", (s, e) => Close()));
            dno.Dock = DockStyle.Bottom;

            trakaMase.Dock = DockStyle.Top;
            trakaMase.Height = Theme.Dp(22);
            trakaMase.BackColor = Theme.Pozadina2;
            trakaMase.Paint += CrtajMasu;

            tabRuta.Dock = DockStyle.Fill;
            tabPonuda.Dock = DockStyle.Fill;
            tabProvere.Dock = DockStyle.Fill;
            tabRuta.CellContentClick += (s, e) =>
            {
                if (e.RowIndex < 0 || tabRuta.Columns[e.ColumnIndex].Name != "AdHoc") return;
                var _ = ZatraziAdHoc(Convert.ToInt32(tabRuta.Rows[e.RowIndex].Cells["RB"].Value));
            };
            tabPonuda.CellContentClick += (s, e) =>
            {
                if (e.RowIndex < 0 || tabPonuda.Columns[e.ColumnIndex].Name != "Izaberi") return;
                Izaberi(prikazane[Convert.ToInt32(tabPonuda.Rows[e.RowIndex].Cells["Indeks"].Value)]);
            };

            var levo = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
            levo.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.Dp(120)));
            levo.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.Dp(92)));
            levo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            levo.Controls.Add(Odeljak("Podaci najave", Natpis(lblPodaci, 84)), 0, 0);
            levo.Controls.Add(Odeljak("Vremenska linija", Natpis(lblLinija, 54)), 0, 1);
            levo.Controls.Add(Odeljak("Ruta", tabRuta), 0, 2);

            var sredina = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            sredina.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.Dp(124)));
            sredina.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            sredina.Controls.Add(Odeljak("Potreban broj lokomotiva", Natpis(lblMasa, 54), trakaMase), 0, 0);
            sredina.Controls.Add(Odeljak("Predlog lokomotiva – dve najbliže po GPS-u", Natpis(lblPonuda, 40), tabPonuda), 0, 1);

            var kolone = new TableLayoutPanel { ColumnCount = 3, RowCount = 3 };
            kolone.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.Dp(32)));
            kolone.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            kolone.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.Dp(190)));
            kolone.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            kolone.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            kolone.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26));
            lblNaslov.Dock = DockStyle.Fill;
            kolone.Controls.Add(lblNaslov, 0, 0);
            kolone.SetColumnSpan(lblNaslov, 3);
            kolone.Controls.Add(levo, 0, 1);
            kolone.Controls.Add(sredina, 1, 1);
            kolone.Controls.Add(Odeljak("Provere", Natpis(lblProvere, 22), tabProvere), 2, 1);
            tabTrase.Dock = DockStyle.Fill;
            EkranOP.StilKombinacija(tabTrase);
            Control trase = Odeljak("Ponuda trasa", Natpis(lblTrase, 20), tabTrase);
            kolone.Controls.Add(trase, 0, 2);
            kolone.SetColumnSpan(trase, 3);

            bool prvi = true;
            Load += (s, e) =>
            {
                var _ = Osvezi(prvi ? predlozenaLokomotiva : null);
                prvi = false;
            };
            kolone.Dock = DockStyle.Fill;
            var okvir = new Panel();
            okvir.Controls.Add(kolone);
            okvir.Controls.Add(dno);
            okvir.Resize += (s, e) => { if (okvir.ClientSize.Width > 0) dno.Height = dno.Rasporedi(okvir.ClientSize.Width); };
            PopuniSirinu(tabRuta);
            PopuniSirinu(tabPonuda);
            PopuniSirinu(tabProvere);
            tabProvere.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            tabProvere.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            izgled.Zavrsi(okvir, 1500, 900);
        }

        private static void PopuniSirinu(DataGridView g)
        {
            g.DataBindingComplete += (s, e) => g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        // Traka: bruto prema najvećoj masi za seriju.
        private void CrtajMasu(object sender, PaintEventArgs e)
        {
            if (masa == null || !masa.MaxJedna.HasValue) return;
            decimal granica = masa.MaxDve ?? masa.MaxJedna.Value * 2;
            if (granica <= 0) return;
            int sirina = trakaMase.ClientSize.Width;
            int bruto = (int)Math.Min(sirina, sirina * masa.Bruto / granica);
            int jedna = (int)Math.Min(sirina, sirina * masa.MaxJedna.Value / granica);
            using (var b = new SolidBrush(masa.Broj == 1 ? ColorTranslator.FromHtml("#2F6B4F") : Theme.Akcent))
                e.Graphics.FillRectangle(b, 0, 0, bruto, trakaMase.Height);
            using (var p = new Pen(Theme.Najtamnija, 2))
                e.Graphics.DrawLine(p, jedna, 0, jedna, trakaMase.Height);
        }
    }
}
