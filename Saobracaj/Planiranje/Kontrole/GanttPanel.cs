using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Saobracaj.Tema;

namespace Saobracaj.Planiranje.Kontrole
{
    internal enum VrstaTrake { Posao, DvostrukaVuca, Zauzetost, BezLokomotive, Nacrt }

    internal sealed class GanttRed
    {
        internal int? Kljuc;
        internal string Naslov;
        internal string Podnaslov;
        internal int Traka;
    }

    internal sealed class GanttTraka
    {
        internal int? RedKljuc;
        internal DateTime Od;
        internal DateTime Do;
        internal string Tekst;
        internal VrstaTrake Vrsta;
        internal bool Konflikt;
        internal int? PosaoID;
        internal int Traka;
        internal Rectangle Okvir;
    }

    // Crtanje na OnPaint: red je lokomotiva, a trake se ređaju u trake reda kad se preklapaju.
    internal sealed class GanttPanel : Panel
    {
        private static readonly Color Zuta = ColorTranslator.FromHtml("#E0A800");
        private static readonly Color Servis = ColorTranslator.FromHtml("#8A8078");

        private readonly List<GanttRed> redovi = new List<GanttRed>();
        private readonly List<GanttTraka> trake = new List<GanttTraka>();
        private DateTime od = DateTime.Today, doVreme = DateTime.Today.AddDays(7);
        private bool poSatima;
        private GanttTraka vucena;
        private Point pritisak;
        private Point poslednja;
        private bool prevlacenje;

        internal event Action<GanttTraka> DvoklikTraka;
        internal event Action<GanttTraka, int> PrevucenaTraka;
        internal event Action<GanttTraka, Point> DesniKlikTraka;

        private int VisinaTrake => Theme.Dp(20);
        private int RazmakTrake => Theme.Dp(2);
        private int SirinaNaslova => Theme.Dp(170);
        private int VisinaZaglavlja => Theme.Dp(30);

        internal GanttPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            AutoScroll = true;
            BackColor = Color.White;
            Font = Theme.Tekstualni(8.5f);
            // Zaglavlje je zakucano na vrhu, pa pomeranje mora da precrta sve; inače se kopija zaglavlja pomeri sa slikom.
            Scroll += (s, e) => Invalidate();
            MouseWheel += (s, e) => Invalidate();
        }

        internal void Prikazi(DateTime pocetak, DateTime kraj, bool satno, IEnumerable<GanttRed> noviRedovi, IEnumerable<GanttTraka> noveTrake)
        {
            od = pocetak;
            doVreme = kraj;
            poSatima = satno;
            redovi.Clear();
            redovi.AddRange(noviRedovi);
            trake.Clear();
            trake.AddRange(noveTrake.Where(t => t.Do > od && t.Od < doVreme));
            RasporediTrake();
            PodesiVisinu();
            Invalidate();
        }

        private void RasporediTrake()
        {
            foreach (GanttRed red in redovi)
            {
                var krajevi = new List<DateTime>();
                foreach (GanttTraka t in trake.Where(x => x.RedKljuc == red.Kljuc).OrderBy(x => x.Od))
                {
                    int i = krajevi.FindIndex(k => k <= t.Od);
                    if (i < 0) { krajevi.Add(t.Do); i = krajevi.Count - 1; }
                    else krajevi[i] = t.Do;
                    t.Traka = i;
                }
                red.Traka = Math.Max(1, krajevi.Count);
            }
        }

        private int VisinaReda(GanttRed r)
        {
            return r.Traka * (VisinaTrake + RazmakTrake) + Theme.Dp(6);
        }

        private void PodesiVisinu()
        {
            AutoScrollMinSize = new Size(0, VisinaZaglavlja + redovi.Sum(VisinaReda));
        }

        private float X(DateTime t, int sirina)
        {
            double ukupno = (doVreme - od).TotalMinutes;
            double deo = (t - od).TotalMinutes / ukupno;
            return SirinaNaslova + (float)(Math.Max(0, Math.Min(1, deo)) * sirina);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int sirina = Math.Max(1, ClientSize.Width - SirinaNaslova);
            int pomak = AutoScrollPosition.Y;
            int y = VisinaZaglavlja + pomak;

            using (var linija = new Pen(Theme.Linija))
            using (var sporedni = new SolidBrush(Theme.TekstSporedni))
            using (var tekst = new SolidBrush(Theme.Tekst))
            {
                Font naslov = Theme.Tekstualni(9f, true);
                foreach (GanttRed r in redovi)
                {
                    int h = VisinaReda(r);
                    g.DrawLine(linija, 0, y + h, ClientSize.Width, y + h);
                    g.DrawString(r.Naslov, naslov, tekst, new RectangleF(Theme.Dp(8), y + Theme.Dp(4), SirinaNaslova - Theme.Dp(12), h - Theme.Dp(4)));
                    if (!string.IsNullOrEmpty(r.Podnaslov))
                        g.DrawString(r.Podnaslov, Font, sporedni, new RectangleF(Theme.Dp(8), y + Theme.Dp(22), SirinaNaslova - Theme.Dp(12), Theme.Dp(16)));
                    foreach (GanttTraka t in trake.Where(x => x.RedKljuc == r.Kljuc))
                        NacrtajTraku(g, t, y, sirina);
                    y += h;
                }
                NacrtajMrezu(g, linija, sporedni, naslov, sirina, pomak);
            }

            if (prevlacenje && vucena != null)
                using (var okvir = new Pen(Theme.Primarna, 2) { DashStyle = DashStyle.Dash })
                    g.DrawRectangle(okvir, poslednja.X - vucena.Okvir.Width / 2, poslednja.Y - VisinaTrake / 2, vucena.Okvir.Width, VisinaTrake);
        }

        private void NacrtajMrezu(Graphics g, Pen linija, Brush sporedni, Font naslov, int sirina, int pomak)
        {
            using (var pozadina = new SolidBrush(Theme.ZaglavljeTabele))
                g.FillRectangle(pozadina, 0, 0, ClientSize.Width, VisinaZaglavlja);
            g.DrawLine(linija, 0, VisinaZaglavlja, ClientSize.Width, VisinaZaglavlja);
            g.DrawLine(linija, SirinaNaslova, 0, SirinaNaslova, ClientSize.Height);
            TimeSpan korak = poSatima ? TimeSpan.FromHours(3) : TimeSpan.FromDays(1);
            for (DateTime t = od; t < doVreme; t += korak)
            {
                float x = X(t, sirina);
                g.DrawLine(linija, x, poSatima && t.Hour != 0 ? VisinaZaglavlja - Theme.Dp(6) : 0, x, ClientSize.Height);
                string s = poSatima ? t.ToString(t.Hour == 0 ? "dd.MM." : "HH:mm") : Skraceno(t) + " " + t.ToString("dd.MM.");
                g.DrawString(s, naslov, sporedni, x + Theme.Dp(3), Theme.Dp(7));
            }
        }

        private static string Skraceno(DateTime t)
        {
            string[] dani = { "ned", "pon", "uto", "sre", "čet", "pet", "sub" };
            return dani[(int)t.DayOfWeek];
        }

        private void NacrtajTraku(Graphics g, GanttTraka t, int yReda, int sirina)
        {
            float x1 = X(t.Od, sirina), x2 = X(t.Do, sirina);
            int y = yReda + Theme.Dp(5) + t.Traka * (VisinaTrake + RazmakTrake);
            var r = new Rectangle((int)x1, y, Math.Max(Theme.Dp(4), (int)(x2 - x1)), VisinaTrake);
            t.Okvir = r;
            Color tekst = Color.White;
            switch (t.Vrsta)
            {
                case VrstaTrake.Zauzetost:
                    using (var srafura = new HatchBrush(HatchStyle.WideUpwardDiagonal, Servis, Theme.Pozadina2))
                        g.FillRectangle(srafura, r);
                    tekst = Theme.Tekst;
                    break;
                case VrstaTrake.BezLokomotive:
                case VrstaTrake.Nacrt:
                    using (var b = new SolidBrush(Theme.ObaveznoPodloga)) g.FillRectangle(b, r);
                    using (var okvir = new Pen(Theme.Akcent, 1.5f) { DashStyle = DashStyle.Dash }) g.DrawRectangle(okvir, r);
                    tekst = Theme.Primarna;
                    break;
                case VrstaTrake.DvostrukaVuca:
                    using (var b = new SolidBrush(Theme.Tamna)) g.FillRectangle(b, r);
                    break;
                default:
                    using (var b = new SolidBrush(Theme.Akcent)) g.FillRectangle(b, r);
                    break;
            }
            if (t.Konflikt)
                using (var okvir = new Pen(Zuta, 3)) g.DrawRectangle(okvir, r);
            using (var b = new SolidBrush(tekst))
            using (var f = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                g.DrawString(t.Tekst, Font, b, new RectangleF(r.X + Theme.Dp(4), r.Y, Math.Max(0, r.Width - Theme.Dp(6)), r.Height), f);
        }

        private GanttTraka TrakaNa(Point p)
        {
            return trake.LastOrDefault(t => t.Okvir.Contains(p));
        }

        // Prazan rezultat kad je tačka na naslovu ili ispod poslednjeg reda; drugi izlaz razlikuje red „Bez lokomotive“.
        private bool RedNa(Point p, out int? kljuc)
        {
            kljuc = null;
            int y = VisinaZaglavlja + AutoScrollPosition.Y;
            foreach (GanttRed r in redovi)
            {
                int h = VisinaReda(r);
                if (p.Y >= y && p.Y < y + h) { kljuc = r.Kljuc; return true; }
                y += h;
            }
            return false;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            GanttTraka t = TrakaNa(e.Location);
            vucena = t != null && t.PosaoID.HasValue && t.Vrsta != VrstaTrake.Nacrt ? t : null;
            pritisak = e.Location;
            prevlacenje = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (vucena == null || e.Button != MouseButtons.Left) return;
            if (!prevlacenje && Math.Abs(e.X - pritisak.X) + Math.Abs(e.Y - pritisak.Y) < Theme.Dp(6)) return;
            prevlacenje = true;
            poslednja = e.Location;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Right)
            {
                GanttTraka desna = TrakaNa(e.Location);
                if (desna != null && desna.PosaoID.HasValue) DesniKlikTraka?.Invoke(desna, e.Location);
                return;
            }
            GanttTraka t = vucena;
            bool bilo = prevlacenje;
            vucena = null;
            prevlacenje = false;
            if (!bilo || t == null) return;
            Invalidate();
            int? kljuc;
            if (RedNa(e.Location, out kljuc) && kljuc.HasValue && kljuc != t.RedKljuc)
                PrevucenaTraka?.Invoke(t, kljuc.Value);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            GanttTraka t = TrakaNa(e.Location);
            if (t != null && t.PosaoID.HasValue) DvoklikTraka?.Invoke(t);
        }
    }
}
