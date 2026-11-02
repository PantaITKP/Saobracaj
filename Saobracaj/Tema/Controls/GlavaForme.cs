using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Saobracaj.Tema.Controls
{
    // Bordo pozadina (gradijent i meki sjaj) koju dele zaglavlje forme i logovanje.
    public static class BordoPozadina
    {
        public static void Crtaj(Graphics g, Rectangle r, float ugao, Point sjaj, int poluprecnik, Color boja, int alfa)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            using (var gradijent = new LinearGradientBrush(r, Theme.Primarna, Theme.Najtamnija, ugao))
            {
                gradijent.InterpolationColors = new ColorBlend
                {
                    Colors = new[] { Theme.Primarna, Theme.Tamna, Theme.Najtamnija },
                    Positions = new[] { 0f, .6f, 1f }
                };
                g.FillRectangle(gradijent, r);
            }
            MekiSjaj(g, sjaj, poluprecnik, boja, alfa);
        }

        public static void MekiSjaj(Graphics g, Point centar, int poluprecnik, Color boja, int alfa)
        {
            using (var putanja = new GraphicsPath())
            {
                putanja.AddEllipse(centar.X - poluprecnik, centar.Y - poluprecnik, 2 * poluprecnik, 2 * poluprecnik);
                using (var cetka = new PathGradientBrush(putanja))
                {
                    cetka.CenterColor = Color.FromArgb(alfa, boja);
                    cetka.SurroundColors = new[] { Color.FromArgb(0, boja) };
                    cetka.FocusScales = new PointF(.1f, .1f);
                    g.FillPath(cetka, putanja);
                }
            }
        }
    }

    // Zaglavlje radne forme: putanja, naslov, oznaka zapisa, status i glavne akcije.
    public class GlavaForme : Panel
    {
        private readonly string putanja;
        private readonly string naslov;
        private readonly Func<string> oznakaZapisa;
        private readonly Func<string> status;
        private Bitmap pozadina;
        private ToolStrip akcije;

        public GlavaForme(string putanja, string naslov, Func<string> oznakaZapisa, Func<string> status)
        {
            this.putanja = putanja;
            this.naslov = naslov;
            this.oznakaZapisa = oznakaZapisa;
            this.status = status;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Height = Theme.Dp(82);
            BackColor = Theme.Tamna;
        }

        public void PostaviAkcije(ToolStrip traka)
        {
            akcije = traka;
            traka.Parent = this;
            traka.Dock = DockStyle.None;
            traka.AutoSize = true;
            traka.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            traka.SizeChanged += (s, e) => RasporediAkcije();
            RasporediAkcije();
        }

        private void RasporediAkcije()
        {
            if (akcije == null) return;
            akcije.Location = new Point(Width - akcije.Width - Theme.Dp(20), (Height - akcije.Height) / 2);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            pozadina?.Dispose();
            pozadina = null;
            RasporediAkcije();
        }

        // Traka akcija prepisuje ovaj isti deo pozadine, pa izgleda providno.
        public void CrtajPozadinu(Graphics g, Rectangle deo)
        {
            if (Width <= 0 || Height <= 0) return;
            if (pozadina == null)
            {
                pozadina = new Bitmap(Width, Height);
                using (var b = Graphics.FromImage(pozadina))
                {
                    b.SmoothingMode = SmoothingMode.AntiAlias;
                    BordoPozadina.Crtaj(b, new Rectangle(0, 0, Width, Height), 8f, new Point(Width - Theme.Dp(90), -Theme.Dp(30)), Theme.Dp(240), Theme.Sjaj, 190);
                }
            }
            g.DrawImage(pozadina, new Rectangle(Point.Empty, deo.Size), deo, GraphicsUnit.Pixel);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            CrtajPozadinu(g, new Rectangle(0, 0, Width, Height));

            int levo = Theme.Dp(20);
            TextRenderer.DrawText(g, putanja, Theme.Tekstualni(8.5f, true), new Point(levo, Theme.Dp(10)), Theme.RozeNaBordo);

            Font fontNaslova = Theme.Naslovni(20f);
            Point mesto = new Point(levo - Theme.Dp(2), Theme.Dp(30));
            TextRenderer.DrawText(g, naslov, fontNaslova, mesto, Color.White);
            int x = mesto.X + TextRenderer.MeasureText(g, naslov, fontNaslova).Width + Theme.Dp(8);
            int sredina = mesto.Y + TextRenderer.MeasureText(g, naslov, fontNaslova).Height / 2;

            string zapis = oznakaZapisa();
            if (!string.IsNullOrWhiteSpace(zapis))
            {
                Font f = Theme.Tekstualni(9f, true);
                Size t = TextRenderer.MeasureText(g, zapis, f, Size.Empty, TextFormatFlags.NoPadding);
                var r = new Rectangle(x, sredina - Theme.Dp(12), t.Width + Theme.Dp(20), Theme.Dp(24));
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var putanjaPil = Pilula(r))
                using (var cetka = new SolidBrush(Color.FromArgb(36, Color.White)))
                using (var olovka = new Pen(Color.FromArgb(76, Color.White)))
                {
                    g.FillPath(cetka, putanjaPil);
                    g.DrawPath(olovka, putanjaPil);
                }
                g.SmoothingMode = SmoothingMode.Default;
                TextRenderer.DrawText(g, zapis, f, r, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                x = r.Right + Theme.Dp(14);
            }

            string tekstStatusa = status();
            if (!string.IsNullOrWhiteSpace(tekstStatusa))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var cetka = new SolidBrush(Theme.RozeNaBordo))
                    g.FillEllipse(cetka, x, sredina - Theme.Dp(4), Theme.Dp(8), Theme.Dp(8));
                g.SmoothingMode = SmoothingMode.Default;
                x += Theme.Dp(14);
                Font f = Theme.Tekstualni(9f, true);
                Size t = TextRenderer.MeasureText(g, tekstStatusa, f, Size.Empty, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, tekstStatusa, f, new Point(x, sredina - t.Height / 2), Theme.RozeNaBordo, TextFormatFlags.NoPadding);
            }
        }

        private static GraphicsPath Pilula(Rectangle r)
        {
            var p = new GraphicsPath();
            int d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 90, 180);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 180);
            p.CloseFigure();
            return p;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) pozadina?.Dispose();
            base.Dispose(disposing);
        }
    }

    public enum VrstaTrake { Glava, SporedneAkcije }

    public class TemaRenderer : ToolStripRenderer
    {
        private readonly VrstaTrake vrsta;

        public TemaRenderer(VrstaTrake vrsta)
        {
            this.vrsta = vrsta;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            if (vrsta == VrstaTrake.Glava)
            {
                var glava = e.ToolStrip.Parent as GlavaForme;
                if (glava != null)
                    glava.CrtajPozadinu(e.Graphics, e.ToolStrip.Bounds);
                return;
            }
            using (var c = new SolidBrush(Theme.Pozadina2))
                e.Graphics.FillRectangle(c, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (vrsta != VrstaTrake.SporedneAkcije) return;
            using (var p = new Pen(Theme.Linija))
                e.Graphics.DrawLine(p, 0, e.ToolStrip.Height - 1, e.ToolStrip.Width, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int x = e.Item.Width / 2;
            using (var p = new Pen(vrsta == VrstaTrake.Glava ? Color.FromArgb(70, Color.White) : Theme.Linija))
                e.Graphics.DrawLine(p, x, Theme.Dp(6), x, e.Item.Height - Theme.Dp(6));
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var g = e.Graphics;
            ToolStripItem s = e.Item;
            var r = new Rectangle(0, 0, s.Width - 1, s.Height - 1);
            bool primarno = Equals(s.Tag, "primarno");
            bool aktivno = s.Enabled && (s.Selected || s.Pressed);

            Color pozadina, ivica;
            if (vrsta == VrstaTrake.SporedneAkcije)
            {
                pozadina = Color.White;
                ivica = aktivno ? Theme.Primarna : Theme.Linija;
            }
            else if (primarno)
            {
                pozadina = !s.Enabled ? Color.FromArgb(120, Color.White) : aktivno ? Theme.ZaglavljeTabele : Color.White;
                ivica = pozadina;
            }
            else
            {
                pozadina = Color.FromArgb(aktivno ? 56 : 30, Color.White);
                ivica = Color.FromArgb(aktivno ? 150 : 88, Color.White);
            }
            using (var c = new SolidBrush(pozadina))
                g.FillRectangle(c, r);
            using (var p = new Pen(ivica))
                g.DrawRectangle(p, r);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            bool primarno = Equals(e.Item.Tag, "primarno");
            if (vrsta == VrstaTrake.SporedneAkcije)
                e.TextColor = !e.Item.Enabled ? Theme.LinijaHover : e.Item.Selected ? Theme.Primarna : Theme.Tekst;
            else if (primarno)
                e.TextColor = Theme.Primarna;
            else
                e.TextColor = e.Item.Enabled ? Color.White : Theme.Mesaj(Theme.Tamna, Color.White, .5);
            base.OnRenderItemText(e);
        }
    }
}
