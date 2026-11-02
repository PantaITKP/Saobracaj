using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MetroFramework;
using MetroFramework.Controls;
using MetroFramework.Interfaces;
using Saobracaj.Tema.Controls;
using Syncfusion.WinForms.Controls;
using Syncfusion.Windows.Forms.Grid.Grouping;
using Syncfusion.Windows.Forms.Tools;

namespace Saobracaj.Tema
{
    // Rekurzivno stilizuje postojeće kontrole forme bez izmene njihovih tipova, imena i događaja.
    public static class ThemeApplier
    {
        public static void Apply(Control koren)
        {
            foreach (Control c in koren.Controls)
            {
                if (c is OkvirPolja) continue;
                if (c.GetType().Name == "BunifuFlatButton")
                {
                    StilBunifu(c);
                    continue;
                }
                Stilizuj(c);
                PrevediIsticanje(c);
                if (!(c is DataGridView || c is GridGroupingControl || c is MultiColumnComboBox || c is NumericUpDown || c is MetroTextBox)) Apply(c);
                if (c is TabControl) TabeviNovogIzgleda((TabControl)c);
            }
        }

        // Žuto isticanje iz Designer-a prelazi u svetlu boju teme; ostale boje mogu da nose značenje i ostaju.
        private static void PrevediIsticanje(Control c)
        {
            Color b = c.BackColor;
            if (b.R == 255 && b.G == 255 && b.B < 255 && !(c is Label))
                c.BackColor = Theme.ObaveznoPodloga;
        }

        // Bunifu nije direktna zavisnost ovog sloja, pa se boje postavljaju po nazivu svojstva.
        private static void StilBunifu(Control c)
        {
            Action<string, Color> postavi = (ime, boja) =>
            {
                var svojstvo = c.GetType().GetProperty(ime);
                if (svojstvo != null && svojstvo.PropertyType == typeof(Color)) svojstvo.SetValue(c, boja);
            };
            postavi("Normalcolor", Theme.Primarna);
            postavi("OnHovercolor", Theme.Akcent);
            postavi("Activecolor", Theme.Tamna);
            postavi("Textcolor", Color.White);
            postavi("OnHoverTextColor", Color.White);
            c.BackColor = Theme.Primarna;
        }

        private static bool JePrimarno(string tekst)
        {
            return Regex.IsMatch(tekst ?? "", @"^\s*(sačuvaj|dodaj|unesi|potvrdi|izračunaj)", RegexOptions.IgnoreCase);
        }

        private static void Stilizuj(Control c)
        {
            if (c is MetroButton)
            {
                var m = (MetroButton)c;
                bool prim = JePrimarno(m.Text);
                m.UseCustomBackColor = true;
                m.UseCustomForeColor = true;
                m.BackColor = prim ? Theme.Primarna : Color.White;
                m.ForeColor = prim ? Color.White : Theme.TekstSporedni;
            }
            else if (c is SfButton) StilSfDugmeta((SfButton)c);
            else if (c is MetroLabel)
            {
                var m = (MetroLabel)c;
                m.UseCustomForeColor = true;
                m.ForeColor = Theme.TekstSporedni;
            }
            else if (c is MetroTextBox || c is MetroComboBox || c is MetroCheckBox || c is MetroGrid)
            {
                ((IMetroControl)c).Style = MetroColorStyle.Red;
                if (c is MetroGrid) StilTabele((DataGridView)c);
            }
            else if (c is GridGroupingControl) StilGrupneTabele((GridGroupingControl)c);
            else if (c is MenuStrip) c.Font = Theme.Tekstualni(9f);
            else if (c is ToolStripEx) { }
            else if (c is ToolStrip)
            {
                var traka = (ToolStrip)c;
                traka.Renderer = new TemaRenderer(VrstaTrake.SporedneAkcije);
                traka.GripStyle = ToolStripGripStyle.Hidden;
                traka.Font = Theme.Tekstualni(9f);
            }
            else if (c is GroupBox)
            {
                c.Font = Theme.Naslovni(9.5f);
                c.ForeColor = Theme.Primarna;
                foreach (Control dete in c.Controls)
                    dete.Font = Theme.Tekstualni(9f);
            }
            else if (c is Label) StilOznake((Label)c);
            else if (c is Button) StilDugmeta((Button)c, JePrimarno(c.Text));
            else if (c is CheckBox) StilCheckBoxa((CheckBox)c);
            else if (c is ComboBox) StilKomba((ComboBox)c);
            else if (c is DataGridView) StilTabele((DataGridView)c);
            else if (c is TabPage) c.BackColor = Color.White;
            else if (c is MultiColumnComboBox)
            {
                var kombo = (MultiColumnComboBox)c;
                kombo.Style = Syncfusion.Windows.Forms.VisualStyle.Metro;
                kombo.MetroColor = Theme.Primarna;
                kombo.Font = Theme.Tekstualni();
            }
            else if (c is DateTimePicker) c.Font = Theme.Sistemski();
            else if (c is TextBoxBase || c is NumericUpDown)
            {
                c.Font = Theme.Tekstualni(9f);
                c.ForeColor = Theme.Tekst;
            }
        }

        public static void StilSfDugmeta(SfButton b)
        {
            bool prim = JePrimarno(b.Text);
            b.Font = Theme.Tekstualni(9f, true);
            b.Style.BackColor = prim ? Theme.Primarna : Color.White;
            b.Style.ForeColor = prim ? Color.White : Theme.TekstSporedni;
            b.Style.HoverBackColor = prim ? Theme.Akcent : Theme.ObaveznoPodloga;
            b.Style.HoverForeColor = prim ? Color.White : Theme.Primarna;
            b.Style.PressedBackColor = prim ? Theme.Tamna : Theme.ObaveznoIvica;
            b.Style.PressedForeColor = prim ? Color.White : Theme.Primarna;
        }

        public static void StilGrupneTabele(GridGroupingControl g)
        {
            g.GridVisualStyles = Syncfusion.Windows.Forms.GridVisualStyles.Metro;
            g.BackColor = Color.White;
            g.BorderStyle = BorderStyle.None;
        }

        // Tema bez ikakvog pomeranja kontrola, za forme čija logika zavisi od rasporeda.
        public static void SamoTema(Form forma)
        {
            forma.BackColor = Theme.Pozadina;
            Apply(forma);
        }

        public static void StilOznake(Label l)
        {
            l.Font = Theme.Tekstualni(8.25f, true);
            l.ForeColor = Theme.TekstSporedni;
            l.BackColor = Color.Transparent;
        }

        public static void StilDugmeta(Button b, bool primarno)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.UseVisualStyleBackColor = false;
            b.Cursor = Cursors.Hand;
            b.Font = Theme.Tekstualni(9f, true);
            b.FlatAppearance.BorderSize = primarno ? 0 : 1;
            b.FlatAppearance.BorderColor = Theme.Linija;
            b.BackColor = primarno ? Theme.Primarna : Color.White;
            b.ForeColor = primarno ? Color.White : Theme.TekstSporedni;
            b.FlatAppearance.MouseOverBackColor = primarno ? Theme.Akcent : Theme.ObaveznoPodloga;
            b.FlatAppearance.MouseDownBackColor = primarno ? Theme.Tamna : Theme.ObaveznoIvica;
        }

        public static void StilCheckBoxa(CheckBox c)
        {
            c.Font = Theme.Tekstualni();
            c.ForeColor = Theme.Tekst;
            c.BackColor = Color.Transparent;
            c.FlatStyle = FlatStyle.Flat;
            c.FlatAppearance.BorderColor = Theme.LinijaHover;
            c.RightToLeft = RightToLeft.No;
            c.CheckAlign = ContentAlignment.MiddleLeft;
            c.TextAlign = ContentAlignment.MiddleLeft;
            c.Cursor = Cursors.Hand;
            // Sistemski kvadratić ne prima boju, zato se bordo oznaka crta preko njega.
            c.Paint += (s, e) =>
            {
                int d = 13;
                var r = new Rectangle(0, (c.Height - d) / 2, d - 1, d - 1);
                using (var pozadina = new SolidBrush(c.Checked ? Theme.Primarna : Color.White))
                    e.Graphics.FillRectangle(pozadina, r.X, r.Y, d, d);
                using (var ivica = new Pen(c.Checked ? Theme.Primarna : Theme.LinijaHover))
                    e.Graphics.DrawRectangle(ivica, r);
                if (c.Checked)
                    Ikone.Crtaj(e.Graphics, Ikona.Cekiraj, new RectangleF(r.X, r.Y, d, d), Color.White, 3f);
            };
        }

        public static void StilKomba(ComboBox c)
        {
            c.FlatStyle = FlatStyle.Flat;
            c.Font = Theme.Tekstualni();
            c.ForeColor = Theme.Tekst;
            new KomboKuka(c, c.BackColor);
        }

        public static void StilTabele(DataGridView g)
        {
            g.EnableHeadersVisualStyles = false;
            g.BackgroundColor = Color.White;
            g.BorderStyle = BorderStyle.None;
            g.GridColor = Theme.Pozadina;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            g.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            g.RowTemplate.Height = Theme.Dp(28);

            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = Theme.Dp(34);
            var zaglavlje = g.ColumnHeadersDefaultCellStyle;
            zaglavlje.BackColor = Theme.ZaglavljeTabele;
            zaglavlje.ForeColor = Theme.TekstSporedni;
            zaglavlje.SelectionBackColor = Theme.ZaglavljeTabele;
            zaglavlje.SelectionForeColor = Theme.TekstSporedni;
            zaglavlje.Font = Theme.Tekstualni(8.25f, true);
            zaglavlje.WrapMode = DataGridViewTriState.False;

            var red = g.RowHeadersDefaultCellStyle;
            red.BackColor = Theme.ZaglavljeTabele;
            red.ForeColor = Theme.Primarna;
            red.SelectionBackColor = Theme.ObaveznoPodloga;
            red.SelectionForeColor = Theme.Primarna;
            g.RowHeadersWidth = Theme.Dp(26);

            var celija = g.DefaultCellStyle;
            celija.BackColor = Color.White;
            celija.ForeColor = Theme.Tekst;
            celija.Font = Theme.Tekstualni(9f);
            celija.SelectionBackColor = Theme.ObaveznoPodloga;
            celija.SelectionForeColor = Theme.Primarna;
            celija.Padding = new Padding(4, 0, 4, 0);
        }

        // Kontrola popunjava stranu taba i prati njenu veličinu, ispod reda sa poljima ako ga ima.
        public static void Popuni(TabPage strana, Control sadrzaj, int vrh = 0)
        {
            sadrzaj.Dock = DockStyle.None;
            sadrzaj.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            EventHandler rasporedi = (s, e) => sadrzaj.SetBounds(0, vrh, strana.ClientSize.Width, Math.Max(0, strana.ClientSize.Height - vrh));
            strana.Resize += rasporedi;
            rasporedi(null, EventArgs.Empty);
        }

        public static void TabeviNovogIzgleda(TabControl tabovi)
        {
            tabovi.Font = Theme.Tekstualni(9f, true);
            tabovi.Padding = new Point(Theme.Dp(14), Theme.Dp(8));
            if (tabovi.Alignment == TabAlignment.Top)
                new TabKuka(tabovi);
        }
    }
}
