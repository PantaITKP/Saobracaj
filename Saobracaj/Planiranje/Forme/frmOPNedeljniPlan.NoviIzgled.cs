using System.Drawing;
using System.Windows.Forms;
using Saobracaj.Planiranje.Modeli;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPNedeljniPlan
    {
        private static readonly Color BojaKonflikt = ColorTranslator.FromHtml("#8E1B2A");
        private static readonly Color BojaUpozorenje = ColorTranslator.FromHtml("#E0A800");
        private static readonly Color BojaInfo = ColorTranslator.FromHtml("#2A5878");

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
            string naslov = SamoBezLokomotive ? "Poslovi bez lokomotive" : Vrsta == VrstaPlana.Operativni ? "Operativni plan" : "Nedeljni plan";
            var izgled = new IzgledForme(this, "Transport / Planiranje", naslov, null);
            izgled.Mreza.MaxKolona = 1;

            cboVrsta.Items.AddRange(new object[] { "Nedeljni", "Operativni" });
            cboVrsta.SelectedIndex = Vrsta == VrstaPlana.Operativni ? 1 : 0;
            cboVrsta.SelectedIndexChanged += (s, e) =>
            {
                Vrsta = cboVrsta.SelectedIndex == 1 ? VrstaPlana.Operativni : VrstaPlana.Nedeljni;
                var _ = Osvezi();
            };
            dtNedelja.ValueChanged += (s, e) => { var _ = Osvezi(); };

            var kartica = new KarticaPolja("Plan");
            kartica.Polje(new Label { Text = "Nedelja / dan" }, dtNedelja);
            kartica.Polje(new Label { Text = "Vrsta plana" }, cboVrsta);
            kartica.Tekst(lblVerzija);
            kartica.Tekst(lblBrojaci);
            kartica.Dugmad(
                EkranOP.Dugme("Uzmi najave", (s, e) => { var _ = UzmiNajave(); }),
                EkranOP.Dugme("Sačuvaj verziju", (s, e) => { var _ = SacuvajVerziju(); }),
                EkranOP.Dugme("Predloži plan", (s, e) => { var _ = Predlozi(); }),
                btnPrihvati,
                EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }));
            izgled.Mreza.Dodaj(kartica);

            lstNalazi.MeasureItem += (s, e) => e.ItemHeight = Theme.Dp(46);
            lstNalazi.DrawItem += CrtajNalaz;
            lstNalazi.DoubleClick += (s, e) =>
            {
                var n = lstNalazi.SelectedItem as Nalaz;
                if (n?.PosaoID != null) OtvoriPosao(n.PosaoID.Value);
            };
            tabBezLok.CellContentClick += (s, e) =>
            {
                if (e.RowIndex < 0 || tabBezLok.Columns[e.ColumnIndex].Name != "Ponudi") return;
                OtvoriPosao(System.Convert.ToInt32(tabBezLok.Rows[e.RowIndex].Cells["ID"].Value));
            };
            // Plava zastavica: na rutu utiče telegram, a desni klik vodi do njega.
            tabBezLok.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || tabBezLok.Columns[e.ColumnIndex].Name != "Telegram") return;
                e.CellStyle.ForeColor = System.Drawing.Color.RoyalBlue;
                e.CellStyle.SelectionForeColor = System.Drawing.Color.RoyalBlue;
                e.CellStyle.Font = Theme.Tekstualni(9.5f, true);
            };
            tabBezLok.CellMouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;
                tabBezLok.ClearSelection();
                tabBezLok.Rows[e.RowIndex].Selected = true;
                ContextMenuStrip meni = MeniTelegrama(System.Convert.ToInt32(tabBezLok.Rows[e.RowIndex].Cells["ID"].Value));
                meni.Show(System.Windows.Forms.Cursor.Position);
            };
            gantt.DesniKlikTraka += (t, tacka) => MeniTelegrama(t.PosaoID.Value).Show(gantt, tacka);
            gantt.DvoklikTraka += t => OtvoriPosao(t.PosaoID.Value);
            gantt.PrevucenaTraka += (t, lok) => OtvoriPosao(t.PosaoID.Value, lok);

            var desno = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            desno.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            desno.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            desno.Controls.Add(Odeljak("Konflikti i upozorenja", lstNalazi), 0, 0);
            desno.Controls.Add(Odeljak("Poslovi bez lokomotive", tabBezLok), 0, 1);

            var podela = new SplitContainer { FixedPanel = FixedPanel.Panel2 };
            podela.Panel1.Controls.Add(Odeljak("Lokomotive po danima", gantt));
            podela.Panel2.Controls.Add(desno);
            podela.HandleCreated += (s, e) => { podela.Panel2MinSize = Theme.Dp(260); podela.SplitterDistance = System.Math.Max(Theme.Dp(300), podela.Width - Theme.Dp(380)); };

            Load += (s, e) => { var _ = Osvezi(); };
            izgled.Zavrsi(podela, 1500, 900);
        }

        private void CrtajNalaz(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index < 0 || e.Index >= lstNalazi.Items.Count) return;
            var n = (Nalaz)lstNalazi.Items[e.Index];
            Color boja = n.Nivo == NivoNalaza.Konflikt ? BojaKonflikt : n.Nivo == NivoNalaza.Upozorenje ? BojaUpozorenje : BojaInfo;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(boja))
                e.Graphics.FillEllipse(b, e.Bounds.X + Theme.Dp(4), e.Bounds.Y + Theme.Dp(7), Theme.Dp(9), Theme.Dp(9));
            bool izabran = (e.State & DrawItemState.Selected) != 0;
            using (var t = new SolidBrush(izabran ? e.ForeColor : Theme.Tekst))
            using (var s = new SolidBrush(izabran ? e.ForeColor : Theme.TekstSporedni))
            using (var f = new StringFormat { Trimming = StringTrimming.EllipsisCharacter })
            {
                e.Graphics.DrawString(n.Naslov, Theme.Tekstualni(9f, true), t, e.Bounds.X + Theme.Dp(18), e.Bounds.Y + Theme.Dp(3));
                e.Graphics.DrawString(n.Opis, Theme.Tekstualni(8.5f), s, new RectangleF(e.Bounds.X + Theme.Dp(18), e.Bounds.Y + Theme.Dp(20), e.Bounds.Width - Theme.Dp(22), Theme.Dp(26)), f);
            }
        }
    }
}
