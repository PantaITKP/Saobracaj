using System;
using System.Drawing;
using System.Windows.Forms;
using Syncfusion.Windows.Forms.Tools;

namespace Saobracaj.Tema.Controls
{
    // Omotač oko postojeće ulazne kontrole: ivica, fokus i stanje obavezno/onemogućeno.
    // Sama kontrola ostaje ista, pa sva postojeća logika forme radi nepromenjeno.
    public class OkvirPolja : Panel
    {
        public const int Prsten = 2;
        public static int VisinaPolja => Theme.Dp(26);

        private readonly Control ulaz;
        private readonly Panel isecak = new Panel();
        private readonly bool obavezno;
        private readonly KomboKuka kuka;
        private bool fokus;

        public Control Ulaz => ulaz;

        public OkvirPolja(Control ulaz, bool obavezno = false)
        {
            this.ulaz = ulaz;
            // Polja koja su u Designer-u istaknuta žutom ili zelenom bojom zadržavaju isticanje, u novoj boji.
            this.obavezno = obavezno || IstaknutoUDizajneru(ulaz.BackColor);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.White;

            ulaz.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            ulaz.Dock = DockStyle.None;
            ulaz.Margin = Padding.Empty;
            // Polja DateTimePicker-a imaju širinu za cifre jednake širine; u DM Sans-u se nule seku.
            ulaz.Font = ulaz is DateTimePicker ? Theme.Sistemski() : Theme.Tekstualni();
            ulaz.ForeColor = Theme.Tekst;

            var mkombo = ulaz as MultiColumnComboBox;
            if (mkombo != null)
            {
                mkombo.Style = Syncfusion.Windows.Forms.VisualStyle.Metro;
                mkombo.MetroColor = Theme.Primarna;
            }

            var tekst = ulaz as TextBoxBase;
            if (tekst != null) tekst.BorderStyle = BorderStyle.None;
            var broj = ulaz as NumericUpDown;
            if (broj != null) broj.BorderStyle = BorderStyle.None;
            var kombo = ulaz as ComboBox;
            if (kombo != null)
            {
                kombo.FlatStyle = FlatStyle.Flat;
                kuka = new KomboKuka(kombo, Color.White);
            }

            isecak.Parent = this;
            ulaz.Parent = isecak;

            ulaz.Enter += (s, e) => { fokus = true; Invalidate(); };
            ulaz.Leave += (s, e) => { fokus = false; Invalidate(); };
            ulaz.EnabledChanged += (s, e) => OsveziBoje();
            isecak.Click += (s, e) => ulaz.Focus();
            OsveziBoje();
        }

        private static bool IstaknutoUDizajneru(Color c)
        {
            return c.ToArgb() == Color.GreenYellow.ToArgb() || (c.R == 255 && c.G == 255 && c.B < 255);
        }

        // Isečak sakriva sistemsku ivicu kontrole koja je ne može sama isključiti.
        private void Iseci(Rectangle unutra, int ivica)
        {
            int visina = Math.Min(unutra.Height, ulaz.Height - 2 * ivica);
            isecak.SetBounds(unutra.X, unutra.Y + (unutra.Height - visina) / 2, unutra.Width, visina);
            ulaz.SetBounds(-ivica, -ivica, unutra.Width + 2 * ivica, ulaz.Height);
        }

        private Color Podloga
        {
            get
            {
                if (!ulaz.Enabled) return Theme.Pozadina;
                return obavezno ? Theme.ObaveznoPodloga : Color.White;
            }
        }

        private void OsveziBoje()
        {
            Color boja = Podloga;
            isecak.BackColor = boja;
            if (!(ulaz is DateTimePicker)) ulaz.BackColor = boja;
            Invalidate();
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            var unutra = new Rectangle(Prsten + 1, Prsten + 1, Math.Max(0, Width - 2 * Prsten - 2), Math.Max(0, Height - 2 * Prsten - 2));
            isecak.Bounds = unutra;

            var tekst = ulaz as TextBoxBase;
            if (tekst != null && tekst.Multiline)
                ulaz.SetBounds(6, 3, unutra.Width - 8, unutra.Height - 5);
            else if (tekst != null)
                ulaz.SetBounds(6, Math.Max(0, (unutra.Height - ulaz.Height) / 2), unutra.Width - 10, ulaz.Height);
            else if (ulaz is NumericUpDown)
                ulaz.SetBounds(4, Math.Max(0, (unutra.Height - ulaz.Height) / 2), unutra.Width - 4, ulaz.Height);
            else if (ulaz is ComboBox)
            {
                kuka?.PostaviVisinu(unutra.Height - 4);
                Iseci(unutra, 1);
            }
            else if (ulaz is MultiColumnComboBox)
                Iseci(unutra, 1);
            else if (ulaz is DateTimePicker)
                Iseci(unutra, 2);
            else
                // Kontrole koje same crtaju okvir (npr. MetroTextBox): sopstvena ivica ostaje van isečka.
                ulaz.SetBounds(-1, -1, unutra.Width + 2, unutra.Height + 2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var r = new Rectangle(Prsten, Prsten, Width - 2 * Prsten - 1, Height - 2 * Prsten - 1);
            if (fokus)
            {
                var spolja = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var prsten = new SolidBrush(Theme.Mesaj(Color.White, Theme.Primarna, .16)))
                    g.FillRectangle(prsten, spolja);
            }
            using (var pozadina = new SolidBrush(Podloga))
                g.FillRectangle(pozadina, r);

            Color ivica = !ulaz.Enabled ? Theme.Pozadina2
                : fokus ? Theme.Primarna
                : obavezno ? Theme.ObaveznoIvica
                : Theme.Linija;
            using (var p = new Pen(ivica))
                g.DrawRectangle(p, r);
        }
    }
}
