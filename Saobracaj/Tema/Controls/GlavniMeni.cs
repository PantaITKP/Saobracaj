using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Saobracaj.Tema.Controls
{
    // Bočni meni: moduli, korisnik i skupljanje (76 px / 272 px, 300 ms).
    public class SideMenu : Panel
    {
        private readonly List<ModulDugme> moduli = new List<ModulDugme>();
        private readonly ModulDugme skupi;
        private readonly string korisnik;
        private Timer animacija;
        private bool skupljen;

        private static int Siroko => Theme.Dp(272);
        private static int Usko => Theme.Dp(76);
        private static int VrhModula => Theme.Dp(84);
        private static int VisinaModula => Theme.Dp(46);

        public event EventHandler<int> ModulIzabran;

        public SideMenu(string korisnik)
        {
            this.korisnik = korisnik ?? "";
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Width = Siroko;
            Dock = DockStyle.Left;
            skupi = new ModulDugme("Skupi meni", Ikona.Levo) { Parent = this, Tiho = true };
            skupi.Click += (s, e) => Prebaci();
        }

        public void DodajModul(string tekst, Ikona ikona)
        {
            int indeks = moduli.Count;
            var dugme = new ModulDugme(tekst, ikona) { Parent = this };
            dugme.Click += (s, e) => ModulIzabran?.Invoke(this, indeks);
            moduli.Add(dugme);
            Rasporedi();
        }

        public void Izaberi(int indeks)
        {
            for (int i = 0; i < moduli.Count; i++)
                moduli[i].Izabran = i == indeks;
        }

        private void Prebaci()
        {
            skupljen = !skupljen;
            skupi.Ikona = skupljen ? Ikona.Desno : Ikona.Levo;
            skupi.Text = skupljen ? "Proširi meni" : "Skupi meni";
            animacija?.Dispose();
            int od = Width;
            int cilj = skupljen ? Usko : Siroko;
            animacija = Animator.Pokreni(this, 300, Animator.Uvod, t => Width = od + (int)Math.Round((cilj - od) * t));
        }

        private void Rasporedi()
        {
            if (skupi == null) return;
            int s = Width - 2 * Theme.Dp(12);
            for (int i = 0; i < moduli.Count; i++)
                moduli[i].SetBounds(Theme.Dp(12), VrhModula + i * VisinaModula, s, VisinaModula - Theme.Dp(2));
            skupi.SetBounds(Theme.Dp(12), Height - Theme.Dp(52), s, Theme.Dp(42));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Rasporedi();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (Width <= 0 || Height <= 0) return;
            using (var c = new LinearGradientBrush(ClientRectangle, Theme.Tamna, Theme.Najtamnija, 90f))
                g.FillRectangle(c, ClientRectangle);

            Ikone.Crtaj(g, Ikona.Strelice, new RectangleF(Theme.Dp(22), Theme.Dp(22), Theme.Dp(32), Theme.Dp(32)), Color.White, 2.2f);
            bool prikaziTekst = Width > Theme.Dp(190);
            if (prikaziTekst)
            {
                TextRenderer.DrawText(g, "Saobraćaj", Theme.Naslovni(13.5f), new Point(Theme.Dp(62), Theme.Dp(17)), Color.White);
                TextRenderer.DrawText(g, "KOMBINOVANI PREVOZ", Theme.Tekstualni(7f, true), new Point(Theme.Dp(64), Theme.Dp(42)), Theme.ObaveznoIvica);
            }

            int y = Height - Theme.Dp(110);
            using (var linija = new Pen(Color.FromArgb(36, Color.White)))
                g.DrawLine(linija, Theme.Dp(12), y, Width - Theme.Dp(12), y);
            int d = Theme.Dp(36);
            var krug = new Rectangle(Theme.Dp(20), y + Theme.Dp(12), d, d);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var c = new SolidBrush(Theme.Akcent))
                g.FillEllipse(c, krug);
            g.SmoothingMode = SmoothingMode.Default;
            string slovo = korisnik.Length > 0 ? korisnik.Trim().Substring(0, 1).ToUpper() : "?";
            TextRenderer.DrawText(g, slovo, Theme.Naslovni(10.5f), krug, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            if (prikaziTekst)
            {
                var mesto = new Rectangle(krug.Right + Theme.Dp(12), krug.Y, Width - krug.Right - Theme.Dp(24), d);
                TextRenderer.DrawText(g, korisnik.Trim(), Theme.Tekstualni(10f, true), mesto, Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }
    }

    public class ModulDugme : Control
    {
        private bool pod;
        private bool izabran;
        private Ikona ikona;

        public bool Tiho { get; set; }

        public Ikona Ikona
        {
            get { return ikona; }
            set { ikona = value; Invalidate(); }
        }

        public bool Izabran
        {
            get { return izabran; }
            set { izabran = value; Invalidate(); }
        }

        public ModulDugme(string tekst, Ikona ikona)
        {
            Text = tekst;
            this.ikona = ikona;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
            AccessibleName = tekst;
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); pod = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); pod = false; Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) OnClick(EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (izabran || pod)
                using (var c = new SolidBrush(Color.FromArgb(izabran ? 28 : 20, Color.White)))
                    g.FillRectangle(c, ClientRectangle);
            if (izabran)
                using (var c = new SolidBrush(Color.White))
                    g.FillRectangle(c, 0, 0, 3, Height);
            if (Focused)
                using (var p = new Pen(Color.White) { DashStyle = DashStyle.Dot })
                    g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);

            Color boja = izabran || pod ? Color.White : Theme.RozeNaBordo;
            float d = Theme.Dp(20);
            Ikone.Crtaj(g, ikona, new RectangleF(Theme.Dp(16), (Height - d) / 2, d, d), boja, 1.8f);
            if (Width > Theme.Dp(120))
            {
                var mesto = new Rectangle(Theme.Dp(50), 0, Width - Theme.Dp(54), Height);
                TextRenderer.DrawText(g, Text, Theme.Tekstualni(Tiho ? 9.5f : 10.5f, izabran), mesto, boja, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
        }
    }

    // Bordo gornji deo glavnog menija: traka sa pretragom, naslov modula i brze prečice.
    public class HeroMeni : Panel
    {
        private readonly List<StakloPrecica> precice = new List<StakloPrecica>();
        private readonly TextBox pretraga;
        private readonly Panel isecakPretrage = new Panel();
        private readonly Label nagovestaj;
        private Bitmap pozadina;
        private string modul = "";

        private static int LevoDesno => Theme.Dp(32);
        private static int VisinaTrake => Theme.Dp(52);

        public TextBox Pretraga => pretraga;

        public HeroMeni()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Dock = DockStyle.Top;
            Height = Theme.Dp(306);

            isecakPretrage.Parent = this;
            pretraga = new TextBox { Parent = isecakPretrage, BorderStyle = BorderStyle.None, Font = Theme.Tekstualni(10.5f), ForeColor = Color.White, AccessibleName = "Pretraga formi" };
            nagovestaj = new Label { Parent = isecakPretrage, Text = "Pretraži forme…", Font = Theme.Tekstualni(10.5f), ForeColor = Theme.RozeNaBordo, AutoSize = true, BackColor = Color.Transparent };
            nagovestaj.BringToFront();
            nagovestaj.Click += (s, e) => pretraga.Focus();
            pretraga.TextChanged += (s, e) => nagovestaj.Visible = pretraga.TextLength == 0;
        }

        public void PostaviModul(string tekst)
        {
            modul = tekst;
            pozadina?.Dispose();
            pozadina = null;
            Invalidate();
        }

        public void DodajPrecicu(string tekst, Ikona ikona, Action radnja)
        {
            var p = new StakloPrecica(tekst, ikona) { Parent = this };
            p.Click += (s, e) => radnja();
            precice.Add(p);
            Rasporedi();
        }

        private Rectangle OkvirPretrage
        {
            get
            {
                int sirina = Math.Min(Theme.Dp(460), Math.Max(Theme.Dp(240), Width - Theme.Dp(420)));
                return new Rectangle((Width - sirina) / 2, Theme.Dp(22) + (VisinaTrake - Theme.Dp(38)) / 2, sirina, Theme.Dp(38));
            }
        }

        private void Rasporedi()
        {
            if (Width <= 0 || pretraga == null) return;
            Rectangle o = OkvirPretrage;
            int levo = Theme.Dp(38);
            isecakPretrage.Bounds = new Rectangle(o.X + levo, o.Y + 1, o.Width - levo - Theme.Dp(58), o.Height - 2);
            pretraga.SetBounds(0, (isecakPretrage.Height - pretraga.Height) / 2, isecakPretrage.Width, pretraga.Height);
            nagovestaj.Location = new Point(-2, (isecakPretrage.Height - nagovestaj.Height) / 2);

            if (precice.Count == 0) return;
            int razmak = Theme.Dp(14);
            int sirina = (Width - 2 * LevoDesno - (precice.Count - 1) * razmak) / precice.Count;
            int vrh = Height - Theme.Dp(88) - Theme.Dp(20);
            for (int i = 0; i < precice.Count; i++)
                precice[i].Postavi(new Rectangle(LevoDesno + i * (sirina + razmak), vrh, sirina, Theme.Dp(88)));
        }

        private void Pripremi()
        {
            if (pozadina != null || Width <= 0 || Height <= 0) return;
            pozadina = new Bitmap(Width, Height);
            using (var g = Graphics.FromImage(pozadina))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                BordoPozadina.Crtaj(g, ClientRectangle, 18f, new Point(Width - Theme.Dp(90), -Theme.Dp(40)), Theme.Dp(340), Theme.Sjaj, 190);

                float k = Width / 1200f, v = Height / 420f;
                using (var linija = new Pen(Color.FromArgb(34, Color.White)))
                    foreach (float pomak in new[] { 0f, 30f, 60f, 340f, 370f, 400f })
                        g.DrawLine(linija, (pomak - 40) * k, 430 * v, (pomak + 700) * k, -10 * v);

                var traka = new Rectangle(LevoDesno, Theme.Dp(22), Width - 2 * LevoDesno, VisinaTrake);
                using (var c = new SolidBrush(Color.FromArgb(26, Color.White)))
                    g.FillRectangle(c, traka);
                using (var p = new Pen(Color.FromArgb(52, Color.White)))
                    g.DrawRectangle(p, traka.X, traka.Y, traka.Width - 1, traka.Height - 1);
            }
            Color boja = Theme.Mesaj(pozadina.GetPixel(OkvirPretrage.X + OkvirPretrage.Width / 2, OkvirPretrage.Y + OkvirPretrage.Height / 2), Color.White, .1);
            using (var g = Graphics.FromImage(pozadina))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                using (var c = new SolidBrush(boja))
                    g.FillRectangle(c, OkvirPretrage);
                using (var p = new Pen(Color.FromArgb(56, Color.White)))
                    g.DrawRectangle(p, OkvirPretrage.X, OkvirPretrage.Y, OkvirPretrage.Width - 1, OkvirPretrage.Height - 1);
                float d = Theme.Dp(16);
                Ikone.Crtaj(g, Ikona.Pretraga, new RectangleF(OkvirPretrage.X + Theme.Dp(12), OkvirPretrage.Y + (OkvirPretrage.Height - d) / 2, d, d), Color.White, 2f);

                var oznaka = new Rectangle(OkvirPretrage.Right - Theme.Dp(52), OkvirPretrage.Y + Theme.Dp(9), Theme.Dp(44), Theme.Dp(20));
                using (var p = new Pen(Color.FromArgb(76, Color.White)))
                    g.DrawRectangle(p, oznaka);
                using (var f = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var c = new SolidBrush(Theme.RozeNaBordo))
                    g.DrawString("Ctrl K", Theme.Tekstualni(7.5f, true), c, oznaka, f);

                using (var c = new SolidBrush(Theme.RozeNaBordo))
                    g.DrawString("Saobraćaj  /", Theme.Tekstualni(9.5f), c, LevoDesno + Theme.Dp(14), Theme.Dp(39));
                float x = LevoDesno + Theme.Dp(14) + g.MeasureString("Saobraćaj  /", Theme.Tekstualni(9.5f)).Width;
                g.DrawString(modul, Theme.Tekstualni(9.5f, true), Brushes.White, x, Theme.Dp(39));

                using (var c = new Pen(Theme.RozeNaBordo))
                    g.DrawLine(c, LevoDesno, Theme.Dp(99), LevoDesno + Theme.Dp(44), Theme.Dp(99));
                using (var c = new SolidBrush(Theme.RozeNaBordo))
                    g.DrawString(modul.ToUpper(), Theme.Tekstualni(8.5f, true), c, LevoDesno + Theme.Dp(54), Theme.Dp(91));
                g.DrawString(modul, Theme.Naslovni(26f), Brushes.White, LevoDesno - Theme.Dp(6), Theme.Dp(108));
                using (var c = new SolidBrush(Theme.SvetloNaBordo))
                    g.DrawString("Izaberite formu iz grupa ispod ili pokrenite neku od čestih radnji.", Theme.Tekstualni(11f), c, LevoDesno - Theme.Dp(2), Theme.Dp(160));
            }
            isecakPretrage.BackColor = boja;
            pretraga.BackColor = boja;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            pozadina?.Dispose();
            pozadina = null;
            Rasporedi();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Pripremi();
            if (pozadina != null) e.Graphics.DrawImageUnscaled(pozadina, 0, 0);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) pozadina?.Dispose();
            base.Dispose(disposing);
        }
    }

    // Staklena prečica u hero delu; pri prelazu mišem podiže se za 4 px.
    public class StakloPrecica : Control
    {
        private readonly Ikona ikona;
        private Rectangle mesto;
        private Timer animacija;
        private bool pod;

        public StakloPrecica(string tekst, Ikona ikona)
        {
            Text = tekst;
            this.ikona = ikona;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
            AccessibleName = tekst;
        }

        public void Postavi(Rectangle okvir)
        {
            mesto = okvir;
            Bounds = okvir;
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); pod = true; Pomeri(true); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); pod = false; Pomeri(false); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) OnClick(EventArgs.Empty);
        }

        private void Pomeri(bool gore)
        {
            animacija?.Dispose();
            int od = Top;
            int cilj = mesto.Y - (gore ? Theme.Dp(4) : 0);
            Invalidate();
            animacija = Animator.Pokreni(this, 280, Animator.Uvod, t => Top = od + (int)Math.Round((cilj - od) * t));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            using (var c = new SolidBrush(Color.FromArgb(pod ? 50 : 30, Color.White)))
                g.FillRectangle(c, ClientRectangle);
            using (var p = new Pen(Focused ? Color.White : Color.FromArgb(pod ? 102 : 60, Color.White)))
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            float d = Theme.Dp(22);
            Ikone.Crtaj(g, ikona, new RectangleF(Theme.Dp(18), Theme.Dp(14), d, d), Color.White, 1.8f);
            var red = new Rectangle(Theme.Dp(18), Height - Theme.Dp(38), Width - Theme.Dp(48), Theme.Dp(24));
            TextRenderer.DrawText(g, Text, Theme.Tekstualni(10f, true), red, Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            float s = Theme.Dp(16);
            Ikone.Crtaj(g, Ikona.StrelicaDesno, new RectangleF(Width - Theme.Dp(34), red.Y + (red.Height - s) / 2, s, s), Color.White, 2f);
        }
    }

    // Kartica grupe u glavnom meniju: naslov, broj stavki i redovi koji pokreću postojeća dugmad.
    public class KarticaGrupe : Panel, IKartica
    {
        private readonly string naslov;
        private readonly List<StavkaReda> redovi = new List<StavkaReda>();
        private static int VisinaZaglavlja => Theme.Dp(50);
        private static int VisinaReda => Theme.Dp(36);

        public KarticaGrupe(string naslov)
        {
            this.naslov = naslov;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.White;
        }

        public void Dodaj(string tekst, Action radnja)
        {
            redovi.Add(new StavkaReda(tekst, radnja) { Parent = this, TabIndex = redovi.Count });
        }

        public int Rasporedi(int sirina)
        {
            for (int i = 0; i < redovi.Count; i++)
                redovi[i].SetBounds(1, VisinaZaglavlja + i * VisinaReda, sirina - 2, VisinaReda);
            return VisinaZaglavlja + redovi.Count * VisinaReda + 1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            using (var ivica = new Pen(Theme.Linija))
            {
                g.DrawRectangle(ivica, 0, 0, Width - 1, Height - 1);
                g.DrawLine(ivica, 1, VisinaZaglavlja - 1, Width - 2, VisinaZaglavlja - 1);
            }
            var red = new Rectangle(Theme.Dp(20), 0, Width - Theme.Dp(80), VisinaZaglavlja);
            TextRenderer.DrawText(g, naslov, Theme.Naslovni(11.5f), red, Theme.Primarna, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

            string broj = redovi.Count.ToString();
            Font f = Theme.Tekstualni(8.25f, true);
            Size t = TextRenderer.MeasureText(g, broj, f, Size.Empty, TextFormatFlags.NoPadding);
            int d = Theme.Dp(22);
            int sirinaPilule = Math.Max(d, t.Width + Theme.Dp(14));
            var pilula = new Rectangle(Width - sirinaPilule - Theme.Dp(16), (VisinaZaglavlja - d) / 2, sirinaPilule, d);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var putanja = new GraphicsPath())
            using (var c = new SolidBrush(Theme.Pozadina))
            {
                putanja.AddArc(pilula.X, pilula.Y, d, d, 90, 180);
                putanja.AddArc(pilula.Right - d, pilula.Y, d, d, 270, 180);
                putanja.CloseFigure();
                g.FillPath(c, putanja);
            }
            g.SmoothingMode = SmoothingMode.Default;
            TextRenderer.DrawText(g, broj, f, pilula, Theme.TekstSporedni, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    public class StavkaReda : Control
    {
        private readonly Action radnja;
        private Timer animacija;
        private double hover;

        public StavkaReda(string tekst, Action radnja)
        {
            Text = tekst;
            this.radnja = radnja;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable, true);
            BackColor = Color.White;
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
            AccessibleName = tekst;
        }

        public void Aktiviraj()
        {
            radnja();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Aktiviraj();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) Aktiviraj();
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); Animiraj(1); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); Animiraj(0); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Animiraj(1); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Animiraj(0); }

        private void Animiraj(double cilj)
        {
            animacija?.Dispose();
            double od = hover;
            animacija = Animator.Pokreni(this, Theme.TrajanjeHover, Animator.IzlazKubni, t =>
            {
                hover = od + (cilj - od) * t;
                Invalidate();
            });
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.Mesaj(Color.White, Theme.ZaglavljeTabele, hover));
            using (var p = new Pen(Theme.Pozadina))
                g.DrawLine(p, 0, Height - 1, Width, Height - 1);
            int levo = Theme.Dp(20) + (int)(Theme.Dp(4) * hover);
            Color boja = Theme.Mesaj(Theme.Tekst, Theme.Primarna, hover);
            var mesto = new Rectangle(levo, 0, Width - levo - Theme.Dp(36), Height);
            TextRenderer.DrawText(g, Text, Theme.Tekstualni(10f), mesto, boja, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            if (hover > .01)
            {
                float d = Theme.Dp(16);
                float x = Width - Theme.Dp(28) - (float)(Theme.Dp(6) * (1 - hover));
                Ikone.Crtaj(g, Ikona.StrelicaDesno, new RectangleF(x, (Height - d) / 2, d, d), Color.FromArgb((int)(255 * hover), Theme.Primarna), 2f);
            }
        }
    }
}
