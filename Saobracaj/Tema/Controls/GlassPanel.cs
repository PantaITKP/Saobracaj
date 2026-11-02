using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Saobracaj.Tema.Controls
{
    // Bordo površina sa staklenom karticom. Pozadina je statična, pa se staklo dobija prozirnim
    // slojevima preko unapred iscrtane slike; polja imaju punu boju uzorkovanu sa te slike,
    // jer standardne kontrole za unos ne mogu biti poluprovidne.
    public class GlassPanel : Control
    {
        public class GlassPolje
        {
            public Rectangle Okvir;
            public Ikona Ikona;
            public bool Fokus;
            public Action<Color> PromenaBoje;
        }

        private readonly List<GlassPolje> polja = new List<GlassPolje>();
        private Bitmap pozadina;
        private Action<Graphics> crtanjeSadrzaja;
        private Rectangle kartica;
        private Rectangle senka;
        private double udeoSenke;

        public GlassPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Theme.Tamna;
        }

        public void Postavi(Rectangle kartica, Action<Graphics> crtanjeSadrzaja)
        {
            this.kartica = kartica;
            this.crtanjeSadrzaja = crtanjeSadrzaja;
            polja.Clear();
            pozadina?.Dispose();
            pozadina = null;
            Invalidate();
        }

        public GlassPolje DodajPolje(Rectangle okvir, Ikona ikona, Action<Color> promenaBoje)
        {
            var polje = new GlassPolje { Okvir = okvir, Ikona = ikona, PromenaBoje = promenaBoje };
            polja.Add(polje);
            promenaBoje(BojaPolja(polje));
            return polje;
        }

        public void PostaviFokus(GlassPolje polje, bool fokus)
        {
            polje.Fokus = fokus;
            polje.PromenaBoje(BojaPolja(polje));
            Rectangle r = polje.Okvir;
            r.Inflate(Theme.Dp(5), Theme.Dp(5));
            Invalidate(r);
        }

        public void PostaviSenku(Rectangle okvir, double udeo)
        {
            Rectangle staro = senka;
            senka = okvir;
            udeoSenke = udeo;
            staro.Inflate(Theme.Dp(22), Theme.Dp(22));
            Invalidate(staro);
            Rectangle novo = okvir;
            novo.Inflate(Theme.Dp(22), Theme.Dp(22));
            Invalidate(novo);
        }

        public Color BojaPolja(GlassPolje polje)
        {
            UcitajPozadinu();
            Color uzorak = pozadina.GetPixel(polje.Okvir.X + polje.Okvir.Width / 2, polje.Okvir.Y + polje.Okvir.Height / 2);
            return Theme.Mesaj(uzorak, Color.White, polje.Fokus ? .2 : .12);
        }

        public Color Uzorak(Point tacka)
        {
            UcitajPozadinu();
            return pozadina.GetPixel(tacka.X, tacka.Y);
        }

        private void UcitajPozadinu()
        {
            if (pozadina != null && pozadina.Size == ClientSize) return;
            pozadina?.Dispose();
            pozadina = new Bitmap(Math.Max(1, ClientSize.Width), Math.Max(1, ClientSize.Height));
            using (var g = Graphics.FromImage(pozadina))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                int s = ClientSize.Width, v = ClientSize.Height;
                BordoPozadina.Crtaj(g, ClientRectangle, 35f, new Point(s / 8, v / 8), Theme.Dp(460), Theme.Sjaj, 200);
                BordoPozadina.MekiSjaj(g, new Point(s - s / 8, v), Theme.Dp(420), Theme.Akcent, 170);
                CrtajSine(g, s, v);
                CrtajKarticu(g);
                crtanjeSadrzaja?.Invoke(g);
            }
        }

        private static void CrtajSine(Graphics g, int s, int v)
        {
            float kx = s / 1440f, ky = v / 900f;
            using (var olovka = new Pen(Color.FromArgb(50, Color.White), 1.2f))
            using (var sredina = new Pen(Color.FromArgb(50, Color.White), 2f) { DashStyle = DashStyle.Custom, DashPattern = new[] { 5f, 7f } })
            {
                g.DrawBezier(olovka, -100 * kx, 760 * ky, 300 * kx, 700 * ky, 700 * kx, 520 * ky, 1100 * kx, 140 * ky);
                g.DrawBezier(sredina, -100 * kx, 780 * ky, 300 * kx, 720 * ky, 700 * kx, 540 * ky, 1120 * kx, 160 * ky);
                g.DrawBezier(olovka, -100 * kx, 800 * ky, 300 * kx, 740 * ky, 700 * kx, 560 * ky, 1140 * kx, 180 * ky);
            }
        }

        private void CrtajKarticu(Graphics g)
        {
            if (kartica.IsEmpty) return;
            for (int i = Theme.Dp(30); i > 0; i -= 3)
            {
                Rectangle r = kartica;
                r.Inflate(i, i);
                r.Offset(0, Theme.Dp(18));
                using (var c = new SolidBrush(Color.FromArgb(5, Theme.Najtamnija)))
                    g.FillRectangle(c, r);
            }
            // Kartica "seče" senku i nosi malo svetla, kao zamagljeno staklo.
            using (var c = new SolidBrush(Theme.Mesaj(Uzorak(g, kartica), Color.White, .1)))
                g.FillRectangle(c, kartica);
            using (var p = new Pen(Color.FromArgb(66, Color.White)))
                g.DrawRectangle(p, kartica.X, kartica.Y, kartica.Width - 1, kartica.Height - 1);
            using (var p = new Pen(Color.FromArgb(76, Color.White)))
                g.DrawLine(p, kartica.X + 1, kartica.Y + 1, kartica.Right - 2, kartica.Y + 1);
        }

        private Color Uzorak(Graphics g, Rectangle r)
        {
            return pozadina.GetPixel(r.X + r.Width / 2, r.Y + r.Height / 2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            UcitajPozadinu();
            Graphics g = e.Graphics;
            g.DrawImageUnscaled(pozadina, 0, 0);

            if (udeoSenke > 0)
            {
                for (int i = Theme.Dp(20); i > 0; i -= 3)
                {
                    Rectangle r = senka;
                    r.Inflate(i, i);
                    r.Offset(0, Theme.Dp(8));
                    using (var c = new SolidBrush(Color.FromArgb((int)(9 * udeoSenke), Theme.Najtamnija)))
                        g.FillRectangle(c, r);
                }
            }

            foreach (GlassPolje p in polja)
            {
                if (p.Fokus)
                {
                    Rectangle prsten = p.Okvir;
                    prsten.Inflate(Theme.Dp(3), Theme.Dp(3));
                    using (var c = new SolidBrush(Color.FromArgb(36, Color.White)))
                        g.FillRectangle(c, prsten);
                }
                using (var c = new SolidBrush(BojaPolja(p)))
                    g.FillRectangle(c, p.Okvir);
                using (var o = new Pen(p.Fokus ? Color.White : Color.FromArgb(76, Color.White)))
                    g.DrawRectangle(o, p.Okvir.X, p.Okvir.Y, p.Okvir.Width - 1, p.Okvir.Height - 1);
                float d = Theme.Dp(20);
                Ikone.Crtaj(g, p.Ikona, new RectangleF(p.Okvir.X + Theme.Dp(14), p.Okvir.Y + (p.Okvir.Height - d) / 2, d, d), Color.White, 1.8f);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) pozadina?.Dispose();
            base.Dispose(disposing);
        }
    }

    // Malo dugme sa ikonom na bordo površini (oko lozinke, zatvaranje prozora).
    public class IkonaDugme : Control
    {
        private readonly Ikona ikona;
        private bool pod;

        public Color Podloga { get; set; }
        public Color BojaIkone { get; set; } = Color.White;

        public IkonaDugme(Ikona ikona, string opis)
        {
            this.ikona = ikona;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            TabStop = false;
            Cursor = Cursors.Hand;
            AccessibleName = opis;
            AccessibleRole = AccessibleRole.PushButton;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            pod = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            pod = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(pod ? Theme.Mesaj(Podloga, Color.White, .14) : Podloga);
            float d = Math.Min(Width, Height) * .5f;
            Ikone.Crtaj(e.Graphics, ikona, new RectangleF((Width - d) / 2, (Height - d) / 2, d, d), BojaIkone, 1.8f);
        }
    }
}
