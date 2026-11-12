using System;
using System.Drawing;
using System.Windows.Forms;

namespace Saobracaj.Tema.Controls
{
    // Kartica stanja u hero delu (zamena za staklenu prečicu, iste mere i boje): ikona i naslov gore,
    // dva broja sa opisom, sitna napomena gore desno i strelica dole desno.
    // Klik na karticu otvara formu bez filtera, a klik na broj ili napomenu formu sa filterom tog broja.
    public class KarticaStanja : Control
    {
        public const string Ucitava = "…";
        public const string Greska = "–";

        // Istaknut broj: topla akcentna boja, čitljiva na bordo pozadini.
        private static readonly Color BojaIsticanja = ColorTranslator.FromHtml("#FFD27A");

        private readonly Ikona ikona;
        private readonly string opisB;
        private string opisA;
        private readonly ToolTip savet = new ToolTip { InitialDelay = 400, ReshowDelay = 200 };
        private Rectangle mesto, zonaA, zonaB, zonaC;
        private Timer animacija;
        private bool pod;
        private string brojA = Ucitava, brojB = Ucitava, tekstC, savetA, savetB, savetC, aktivniSavet;
        private bool istakniA, istakniB, istakniC, omogucena = true;

        public event EventHandler KlikA, KlikB, KlikC;

        public KarticaStanja(string naslov, Ikona ikona, string opisA, string opisB)
        {
            Text = naslov;
            this.ikona = ikona;
            this.opisA = opisA;
            this.opisB = opisB;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
            // Klik mišem se deli po zonama u OnMouseUp; bez ovoga bi klik na broj otvorio i formu bez filtera.
            SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, false);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
            AccessibleName = naslov;
        }

        // Brojevi kao tekst: broj, Ucitava dok se čeka ili Greska kad učitavanje ne uspe. B može biti null (kartica sa jednim brojem).
        // C je sitna napomena gore desno (npr. „kasni: 2“); istaknuta samo kad traži pažnju.
        public void Postavi(string a, string b, bool istakniA, bool istakniB, string c = null, bool istakniC = false)
        {
            brojA = a;
            brojB = b;
            this.istakniA = istakniA;
            this.istakniB = istakniB;
            tekstC = c;
            this.istakniC = istakniC;
            Invalidate();
        }

        // Opis ispod broja A (npr. „nema zatvora“ umesto „zatvora danas“ kad je broj 0).
        public string OpisA
        {
            get { return opisA; }
            set { opisA = value; Invalidate(); }
        }

        public void Saveti(string a, string b, string c = null)
        {
            savetA = a;
            savetB = b;
            savetC = c;
            aktivniSavet = null;
        }

        // Bez prava na ciljnu formu kartica je zatamnjena, bez brojeva i bez klika.
        public bool Omogucena
        {
            get { return omogucena; }
            set
            {
                if (omogucena == value) return;
                omogucena = value;
                Cursor = value ? Cursors.Hand : Cursors.Default;
                TabStop = value;
                if (!value) { pod = false; Pomeri(false); savet.Hide(this); }
                Invalidate();
            }
        }

        public void PostaviOkvir(Rectangle okvir)
        {
            mesto = okvir;
            Bounds = okvir;
        }

        private bool ImaBroj(string b)
        {
            return omogucena && b != null && b != Ucitava && b != Greska;
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); if (!omogucena) return; pod = true; Pomeri(true); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); pod = false; Pomeri(false); aktivniSavet = null; savet.Hide(this); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!omogucena) return;
            string s = zonaA.Contains(e.Location) && ImaBroj(brojA) ? savetA
                : zonaB.Contains(e.Location) && ImaBroj(brojB) ? savetB
                : zonaC.Contains(e.Location) && !string.IsNullOrEmpty(tekstC) ? savetC : null;
            if (s == aktivniSavet) return;
            aktivniSavet = s;
            if (s == null) savet.Hide(this);
            else savet.Show(s, this, e.X + Theme.Dp(12), e.Y + Theme.Dp(18), 6000);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!omogucena || e.Button != MouseButtons.Left || !ClientRectangle.Contains(e.Location)) return;
            if (zonaA.Contains(e.Location) && ImaBroj(brojA) && KlikA != null) KlikA(this, EventArgs.Empty);
            else if (zonaB.Contains(e.Location) && ImaBroj(brojB) && KlikB != null) KlikB(this, EventArgs.Empty);
            else if (zonaC.Contains(e.Location) && !string.IsNullOrEmpty(tekstC) && KlikC != null) KlikC(this, EventArgs.Empty);
            else OnClick(EventArgs.Empty);
        }

        // Klik mišem ide kroz OnMouseUp (zone brojeva); ovde stiže samo Enter/Space i klik van zona.
        protected override void OnClick(EventArgs e)
        {
            if (omogucena) base.OnClick(e);
        }

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
            int alfa = omogucena ? 255 : 110;
            Color bela = Color.FromArgb(alfa, Color.White);
            using (var c = new SolidBrush(Color.FromArgb(omogucena && pod ? 50 : omogucena ? 30 : 16, Color.White)))
                g.FillRectangle(c, ClientRectangle);
            using (var p = new Pen(Focused ? Color.White : Color.FromArgb(pod ? 102 : omogucena ? 60 : 36, Color.White)))
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);

            int levo = Theme.Dp(18);
            float d = Theme.Dp(20);
            Ikone.Crtaj(g, ikona, new RectangleF(levo, Theme.Dp(11), d, d), bela, 1.8f);
            var naslov = new Rectangle(levo + Theme.Dp(30), Theme.Dp(9), Width - levo - Theme.Dp(30) - Theme.Dp(12), Theme.Dp(24));
            var zastavice = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;

            // Napomena (C) gore desno, pa naslov do nje.
            zonaC = Rectangle.Empty;
            if (omogucena && !string.IsNullOrEmpty(tekstC))
            {
                Font fc = Theme.Tekstualni(8.5f, istakniC);
                Size v = TextRenderer.MeasureText(g, tekstC, fc, Size.Empty, TextFormatFlags.NoPadding);
                zonaC = new Rectangle(Width - Theme.Dp(14) - v.Width, naslov.Y, v.Width, naslov.Height);
                TextRenderer.DrawText(g, tekstC, fc, zonaC, istakniC ? BojaIsticanja : Theme.RozeNaBordo, zastavice);
                naslov.Width = Math.Max(0, zonaC.X - Theme.Dp(8) - naslov.X);
            }
            TextRenderer.DrawText(g, Text, Theme.Tekstualni(10f, true), naslov, bela, zastavice);

            zonaA = zonaB = Rectangle.Empty;
            if (omogucena)
            {
                int vrh = Theme.Dp(34);
                int sirinaKolone = (Width - levo - Theme.Dp(44)) / 2;
                zonaA = Broj(g, brojA, opisA, istakniA, new Rectangle(levo, vrh, sirinaKolone, Height - vrh - Theme.Dp(6)));
                if (brojB != null)
                    zonaB = Broj(g, brojB, opisB, istakniB, new Rectangle(levo + sirinaKolone + Theme.Dp(8), vrh, sirinaKolone, Height - vrh - Theme.Dp(6)));
            }

            float s = Theme.Dp(16);
            Ikone.Crtaj(g, Ikona.StrelicaDesno, new RectangleF(Width - Theme.Dp(32), Height - Theme.Dp(30), s, s), bela, 2f);
        }

        // Broj i opis ispod njega; vraća zonu za klik (samo broj i opis, ne cela kolona).
        private Rectangle Broj(Graphics g, string broj, string opis, bool istakni, Rectangle kolona)
        {
            Font fb = Theme.Naslovni(istakni ? 17f : 16f);
            Font fo = Theme.Tekstualni(8.5f, istakni);
            Size vb = TextRenderer.MeasureText(g, broj, fb, Size.Empty, TextFormatFlags.NoPadding);
            Size vo = TextRenderer.MeasureText(g, opis ?? "", fo, Size.Empty, TextFormatFlags.NoPadding);
            Color boja = istakni ? BojaIsticanja : Color.White;
            var rb = new Rectangle(kolona.X, kolona.Y, kolona.Width, vb.Height);
            var ro = new Rectangle(kolona.X, rb.Bottom, kolona.Width, vo.Height + Theme.Dp(2));
            var zastavice = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, broj, fb, rb, boja, zastavice);
            TextRenderer.DrawText(g, opis ?? "", fo, ro, istakni ? BojaIsticanja : Theme.RozeNaBordo, zastavice);
            return new Rectangle(kolona.X, kolona.Y, Math.Min(kolona.Width, Math.Max(vb.Width, vo.Width) + Theme.Dp(6)), rb.Height + ro.Height);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { animacija?.Dispose(); savet.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
