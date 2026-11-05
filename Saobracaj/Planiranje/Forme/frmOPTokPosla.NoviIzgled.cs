using System.Drawing;
using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPTokPosla
    {
        private static readonly string[] Koraci = { "Najava", "Plan lokomotive", "Osoblje i zadatak", "Prijem i pregled", "U putu", "Predaja u toku", "Predato" };
        private static readonly string[] Uloge = { "Služba pripreme", "Planer", "Mašinski dispečer", "Pregledač", "Dispečer", "Pregledač", "Dispečer" };
        private static readonly Color BojaZavrseno = ColorTranslator.FromHtml("#2F6B4F");

        private static Control Odeljak(string naslov, Control sadrzaj)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(Theme.Dp(6)) };
            sadrzaj.Dock = DockStyle.Fill;
            p.Controls.Add(sadrzaj);
            p.Controls.Add(new Label { Text = naslov, Dock = DockStyle.Top, Height = Theme.Dp(24), Font = Theme.Tekstualni(10f, true), ForeColor = Theme.Tekst });
            return p;
        }

        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Planiranje", "Tok posla", null);
            izgled.Mreza.MaxKolona = 1;

            cboPosao.SelectedIndexChanged += (s, e) =>
            {
                if (punjenje || !(cboPosao.SelectedValue is int)) return;
                PosaoId = (int)cboPosao.SelectedValue;
                var _ = Osvezi();
            };
            var glava = new KarticaPolja("Posao");
            glava.Polje(new Label { Text = "Najava" }, cboPosao, siroko: true);
            glava.Dugmad(
                EkranOP.Dugme("Prijem i pregled završen", (s, e) => { var _ = PrijemZavrsen(); }),
                EkranOP.Dugme("Predaja u toku", (s, e) => { var _ = PredajaUToku(); }),
                EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }));
            izgled.Mreza.Dodaj(glava);

            panelKoraka.Paint += CrtajKorake;
            panelKoraka.Resize += (s, e) => panelKoraka.Invalidate();
            tabDogadjaji.DataBindingComplete += (s, e) => tabDogadjaji.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            lblSledeci.Font = Theme.Tekstualni(9f);
            lblVreme.Font = Theme.Tekstualni(9f);
            lblSledeci.ForeColor = Theme.Tekst;
            lblVreme.ForeColor = Theme.Tekst;

            var desno = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            desno.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            desno.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            desno.Controls.Add(Odeljak("Sledeći koraci", lblSledeci), 0, 0);
            desno.Controls.Add(Odeljak("Vreme po parametrima", lblVreme), 0, 1);

            var kolone = new TableLayoutPanel { ColumnCount = 2, RowCount = 2 };
            kolone.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.Dp(110)));
            kolone.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            kolone.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
            kolone.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            var korak = Odeljak("Faze posla i ko ih vodi", panelKoraka);
            kolone.Controls.Add(korak, 0, 0);
            kolone.SetColumnSpan(korak, 2);
            kolone.Controls.Add(Odeljak("Evidencija događaja", tabDogadjaji), 0, 1);
            kolone.Controls.Add(desno, 1, 1);

            Load += (s, e) => { var _ = UcitajPoslove(); };
            izgled.Zavrsi(kolone, 1400, 860);
        }

        // Završeno zeleno, trenutno crveno, sledeće sivo.
        private void CrtajKorake(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int faza = posao?.Faza ?? 0;
            int sirina = panelKoraka.ClientSize.Width / Koraci.Length;
            Font naslov = Theme.Tekstualni(8.5f, true), obican = Theme.Tekstualni(8f);
            for (int i = 0; i < Koraci.Length; i++)
            {
                int rb = i + 1, x = i * sirina;
                Color boja = rb < faza ? BojaZavrseno : rb == faza ? Theme.Akcent : Theme.LinijaHover;
                using (var b = new SolidBrush(boja))
                {
                    if (i + 1 < Koraci.Length) g.FillRectangle(b, x + Theme.Dp(24), Theme.Dp(12), sirina, Theme.Dp(3));
                    g.FillEllipse(b, x + Theme.Dp(12), Theme.Dp(2), Theme.Dp(22), Theme.Dp(22));
                }
                using (var t = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString(rb.ToString(), naslov, Brushes.White, new RectangleF(x + Theme.Dp(12), Theme.Dp(2), Theme.Dp(22), Theme.Dp(22)), t);
                using (var tb = new SolidBrush(Theme.Tekst)) g.DrawString(Koraci[i], naslov, tb, x + Theme.Dp(6), Theme.Dp(28));
                using (var sb = new SolidBrush(Theme.TekstSporedni)) g.DrawString(Uloge[i], obican, sb, x + Theme.Dp(6), Theme.Dp(44));
            }
        }
    }
}
