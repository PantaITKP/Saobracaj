using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Sifarnici
{
    // FreightMind login (04_Izgled_softvera/Mockups/Logovanje/freightmind-login: login.html + README).
    // Sve osim polja za unos, oka i dugmeta PRIJAVA crta se na samoj formi, u koordinatama dizajna 1120 x 700
    // pomnoženim sa "k". Prijava i učitavanje korisnika ostaju u frmLogovanje.cs.
    public partial class frmLogovanje
    {
        private const float SirinaDizajna = 1120f, VisinaDizajna = 700f;
        private const float TrajanjeUvoda = .9f, KasnjenjeKartice = .25f;
        private const float CiklusVoza = 12f, CiklusIsprekidane = 1.4f;

        // Šina po kojoj vozi lokomotiva: M0 596 C 330 536, 635 395, 885 120.
        private static readonly PointF[] sina = { new PointF(0, 596), new PointF(330, 536), new PointF(635, 395), new PointF(885, 120) };

        private static readonly Color KpDuboka = ColorTranslator.FromHtml("#51111C");
        private static readonly Color Papir = ColorTranslator.FromHtml("#EFEDE9");
        private static readonly Color Zuta = ColorTranslator.FromHtml("#F2C500");

        private float k = 1f;
        private bool animacije;
        private RectangleF kartica, brend, znak, okvirKorisnik, okvirLozinka;
        private Bitmap pozadina, slojBrenda, slojKartice, lokomotiva, sjajLokomotive;
        private readonly List<Font> fontovi = new List<Font>();
        private readonly StringFormat tipografski = NapraviFormat();

        private Panel isecakKorisnik;
        private IkonaDugme okoLozinke;
        private int fokus; // 0 nijedno, 1 korisnik, 2 lozinka

        private Timer otkucaj;
        private readonly Stopwatch sat = new Stopwatch();
        private float vreme;
        private bool uvodGotov;
        private Region oblastIsprekidane;
        private Rectangle oblastVoza;
        private float[] duzineLuka;

        private void PrimeniNoviIzgled()
        {
            SuspendLayout();
            animacije = Theme.AnimacijeUkljucene;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "FreightMind – Logovanje";
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            using (Stream s = Resurs("Saobracaj.Tema.Slike.freightmind.ico"))
                if (s != null) Icon = new Icon(s);

            Rectangle radno = Screen.PrimaryScreen.WorkingArea;
            k = Math.Min(Theme.Dp(1000) / 1000f, Math.Min(radno.Width / SirinaDizajna, radno.Height / VisinaDizajna));
            ClientSize = new Size((int)Math.Round(SirinaDizajna * k), (int)Math.Round(VisinaDizajna * k));

            pictureBox1.Visible = false;
            pictureBox2.Visible = false;
            autoLabel1.Visible = false;
            autoLabel2.Visible = false;

            Rasporedi();
            NapraviPozadinu();
            NapraviSlojBrenda();
            NapraviSlojKartice();
            PostaviKontrole();
            if (animacije) PripremiAnimacije();

            MouseDown += KlikNaPovrsinu;
            Shown += (s, e) => PokreniAnimacije();
            Disposed += (s, e) => Oslobodi();
            ResumeLayout(true);
        }

        // ===== Raspored (sve u px dizajna) =====

        private void Rasporedi()
        {
            brend = new RectangleF(86, 250, 440, 0);
            znak = new RectangleF(86, 250, 30, 30);

            // Kartica: padding 32/36/36, razmak između blokova 20, okvir 1 px.
            float y = 156 + 1 + 32;
            y += Linija(Naslovna(28)) + 8 + Linija(Tekstualna(14));
            y += 20;
            y += Linija(Tekstualna(12, FontStyle.Bold)) + 8;
            okvirKorisnik = new RectangleF(618 + 1 + 36, y, 420 - 2 - 72, 48);
            y = okvirKorisnik.Bottom + 20 + Linija(Tekstualna(12, FontStyle.Bold)) + 8;
            okvirLozinka = new RectangleF(okvirKorisnik.X, y, okvirKorisnik.Width, 48);
            y = okvirLozinka.Bottom + 20 + 8 + 50 + 36 + 1;
            kartica = new RectangleF(618, 156, 420, y - 156);
        }

        private Rectangle UPikselima(RectangleF r)
        {
            return Rectangle.FromLTRB((int)Math.Floor(r.Left * k), (int)Math.Floor(r.Top * k), (int)Math.Ceiling(r.Right * k), (int)Math.Ceiling(r.Bottom * k));
        }

        // ===== Fontovi =====

        private Font Pismo(FontFamily porodica, float px, FontStyle stil)
        {
            Font f;
            try { f = new Font(porodica, px, stil, GraphicsUnit.Pixel); }
            catch (ArgumentException) { f = new Font("Segoe UI", px, stil, GraphicsUnit.Pixel); }
            fontovi.Add(f);
            return f;
        }

        private Font Tekstualna(float px, FontStyle stil = FontStyle.Regular)
        {
            return Pismo(Theme.Tekstualni().FontFamily, px, stil);
        }

        private Font Naslovna(float px)
        {
            FontFamily porodica = Theme.Naslovni(10f).FontFamily;
            return Pismo(porodica, px, porodica.Name.StartsWith("Manrope") ? FontStyle.Regular : FontStyle.Bold);
        }

        private Font NaslovnaSrednja(float px)
        {
            FontFamily srednja = Theme.Porodica("Manrope Medium");
            return srednja != null ? Pismo(srednja, px, FontStyle.Regular) : Naslovna(px);
        }

        private Font Italik(float px)
        {
            FontFamily italik = Theme.Porodica("DM Sans 14pt");
            return italik != null ? Pismo(italik, px, FontStyle.Italic) : Pismo(new FontFamily("Segoe UI"), px, FontStyle.Italic);
        }

        // CSS line-height: normal.
        private static float Linija(Font f)
        {
            FontFamily p = f.FontFamily;
            return f.Size * p.GetLineSpacing(f.Style) / p.GetEmHeight(f.Style);
        }

        private static float Uspon(Font f)
        {
            FontFamily p = f.FontFamily;
            return f.Size * p.GetCellAscent(f.Style) / p.GetEmHeight(f.Style);
        }

        // Osnovna linija teksta u linijskom okviru visine "visina" (polovina proreda iznad i ispod, kao u CSS-u).
        private static float Osnova(Font f, float vrh, float visina)
        {
            FontFamily p = f.FontFamily;
            float em = p.GetEmHeight(f.Style);
            float uspon = f.Size * p.GetCellAscent(f.Style) / em;
            float pad = f.Size * p.GetCellDescent(f.Style) / em;
            return vrh + (visina - uspon - pad) / 2 + uspon;
        }

        private static StringFormat NapraviFormat()
        {
            var f = (StringFormat)StringFormat.GenericTypographic.Clone();
            f.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoWrap;
            return f;
        }

        // Crta tekst na osnovnoj liniji; "razmak" je letter-spacing u px. Vraća x posle teksta.
        private float Pisi(Graphics g, string tekst, Font f, Brush cetka, float x, float osnova, float razmak = 0)
        {
            float y = osnova - Uspon(f);
            if (razmak == 0)
            {
                g.DrawString(tekst, f, cetka, x, y, tipografski);
                return x + g.MeasureString(tekst, f, PointF.Empty, tipografski).Width;
            }
            foreach (char c in tekst)
            {
                string slovo = c.ToString();
                g.DrawString(slovo, f, cetka, x, y, tipografski);
                x += g.MeasureString(slovo, f, PointF.Empty, tipografski).Width + razmak;
            }
            return x;
        }

        // ===== Statični slojevi (iscrtavaju se jednom) =====

        private void NapraviPozadinu()
        {
            int s = ClientSize.Width, v = ClientSize.Height;
            pozadina = new Bitmap(s, v, PixelFormat.Format32bppPArgb);

            // radial-gradient(ellipse at 18% 8%, ...): farthest-corner elipsa, poluose = najdalje stranice × √2.
            float cx = .18f * s, cy = .08f * v;
            float rx = Math.Max(cx, s - cx) * 1.41421356f, ry = Math.Max(cy, v - cy) * 1.41421356f;
            Color[] boje = { Theme.Sjaj, Theme.Primarna, KpDuboka, ColorTranslator.FromHtml("#3D0D16") };
            float[] mesta = { 0f, .38f, .72f, 1f };
            var piksel = new int[s * v];
            for (int y = 0; y < v; y++)
            {
                float dy = (y + .5f - cy) / ry;
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + .5f - cx) / rx;
                    float d = (float)Math.Sqrt(dx * dx + dy * dy);
                    int i = 0;
                    while (i < mesta.Length - 2 && d > mesta[i + 1]) i++;
                    float u = Math.Max(0, Math.Min(1, (d - mesta[i]) / (mesta[i + 1] - mesta[i])));
                    Color a = boje[i], b = boje[i + 1];
                    piksel[y * s + x] = unchecked((int)0xFF000000)
                        | (int)(a.R + (b.R - a.R) * u + .5f) << 16
                        | (int)(a.G + (b.G - a.G) * u + .5f) << 8
                        | (int)(a.B + (b.B - a.B) * u + .5f);
                }
            }
            BitmapData podaci = pozadina.LockBits(new Rectangle(0, 0, s, v), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
            for (int y = 0; y < v; y++)
                Marshal.Copy(piksel, y * s, podaci.Scan0 + y * podaci.Stride, s);
            pozadina.UnlockBits(podaci);

            using (var g = Graphics.FromImage(pozadina))
            using (var olovka = new Pen(Color.FromArgb(46, Color.White), 1.5f))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.ScaleTransform(k, k);
                g.DrawBezier(olovka, 0, 580, 320, 520, 620, 380, 870, 110);
                g.DrawBezier(olovka, 0, 612, 340, 552, 650, 410, 900, 130);
            }
        }

        private Bitmap NoviSloj(RectangleF r, out Graphics g)
        {
            Rectangle p = UPikselima(r);
            var bmp = new Bitmap(Math.Max(1, p.Width), Math.Max(1, p.Height), PixelFormat.Format32bppPArgb);
            g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.TranslateTransform(-p.X, -p.Y);
            g.ScaleTransform(k, k);
            return bmp;
        }

        private RectangleF OkvirSlojaBrenda()
        {
            return new RectangleF(brend.X - 8, brend.Y - 8, brend.Width + 40, 210);
        }

        private void NapraviSlojBrenda()
        {
            Graphics g;
            slojBrenda = NoviSloj(OkvirSlojaBrenda(), out g);
            using (g)
            using (var bela = new SolidBrush(Color.White))
            using (var roze = new SolidBrush(Theme.RozeNaBordo))
            using (var crta = new SolidBrush(Color.FromArgb(179, Color.White)))
            {
                // Red 1: znak 30 × 30 (crta se uživo zbog animacije) + KOMBINOVANI PREVOZ.
                Font zaglavlje = Tekstualna(13, FontStyle.Bold);
                Pisi(g, "KOMBINOVANI PREVOZ", zaglavlje, bela, brend.X + 30 + 12, Osnova(zaglavlje, brend.Y, 30), 13 * .06f);

                // Red 2: crta 44 × 1 + ŽELEZNIČKI PREVOZ TERETA · SRBIJA.
                Font podnaslov = Tekstualna(12, FontStyle.Bold);
                float y = brend.Y + 30 + 22;
                float visina = Linija(podnaslov);
                g.FillRectangle(crta, brend.X, y + visina / 2 - .5f, 44, 1);
                Pisi(g, "ŽELEZNIČKI PREVOZ TERETA · SRBIJA", podnaslov, bela, brend.X + 44 + 12, Osnova(podnaslov, y, visina), 12 * .06f);

                // FreightMind: 64px, line-height 1.05, letter-spacing -.02em.
                y += visina + 22;
                Font freight = Naslovna(64), mind = NaslovnaSrednja(64);
                float osnova = Osnova(freight, y, 64 * 1.05f);
                float x = Pisi(g, "Freight", freight, bela, brend.X, osnova, -64 * .02f);
                Pisi(g, "Mind", mind, roze, x, osnova, -64 * .02f);

                y += 64 * 1.05f + 6;
                Font moto = Italik(20);
                Pisi(g, "Mens agitat molem", moto, roze, brend.X, Osnova(moto, y, Linija(moto)));
                brend.Height = y + Linija(moto) - brend.Y;
            }
        }

        private void NapraviSlojKartice()
        {
            Graphics g;
            slojKartice = NoviSloj(kartica, out g);
            using (g)
            using (var staklo = new SolidBrush(Color.FromArgb(20, Color.White)))
            using (var ivica = new Pen(Color.FromArgb(71, Color.White)))
            using (var bela = new SolidBrush(Color.White))
            using (var bleda = new SolidBrush(Color.FromArgb(217, Color.White)))
            {
                g.FillRectangle(staklo, kartica);
                g.SmoothingMode = SmoothingMode.None;
                g.DrawRectangle(ivica, kartica.X + .5f, kartica.Y + .5f, kartica.Width - 1, kartica.Height - 1);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                float x = kartica.X + 1 + 36;
                float y = kartica.Y + 1 + 32;
                Font naslov = Naslovna(28);
                Pisi(g, "Logovanje", naslov, bela, x, Osnova(naslov, y, Linija(naslov)));
                y += Linija(naslov) + 8;
                Font pozdrav = Tekstualna(14);
                Pisi(g, Pozdrav() + " — šta prevozimo danas?", pozdrav, bleda, x, Osnova(pozdrav, y, Linija(pozdrav)));

                Font oznaka = Tekstualna(12, FontStyle.Bold);
                float visinaOznake = Linija(oznaka);
                Pisi(g, "KORISNIK", oznaka, bela, x, Osnova(oznaka, okvirKorisnik.Y - 8 - visinaOznake, visinaOznake), 12 * .06f);
                Pisi(g, "LOZINKA", oznaka, bela, x, Osnova(oznaka, okvirLozinka.Y - 8 - visinaOznake, visinaOznake), 12 * .06f);

                CrtajPolje(g, okvirKorisnik, Ikona.Korisnik);
                CrtajPolje(g, okvirLozinka, Ikona.Brava);
            }
        }

        private static string Pozdrav()
        {
            int sat = DateTime.Now.Hour;
            return sat < 12 ? "Dobro jutro" : sat < 18 ? "Dobar dan" : "Dobro veče";
        }

        // Polje je puno (kontrole za unos ne mogu biti providne): belo 14% preko stakla kartice, okvir belo 30%.
        private Color BojaPolja(RectangleF polje)
        {
            Rectangle p = UPikselima(polje);
            Color uzorak = pozadina.GetPixel(p.X + p.Width / 2, p.Y + p.Height / 2);
            return Theme.Mesaj(Theme.Mesaj(uzorak, Color.White, .08), Color.White, .14);
        }

        private void CrtajPolje(Graphics g, RectangleF polje, Ikona ikona)
        {
            using (var c = new SolidBrush(BojaPolja(polje)))
                g.FillRectangle(c, polje);
            using (var o = new Pen(Color.FromArgb(77, Color.White)))
            {
                g.SmoothingMode = SmoothingMode.None;
                g.DrawRectangle(o, polje.X + .5f, polje.Y + .5f, polje.Width - 1, polje.Height - 1);
                g.SmoothingMode = SmoothingMode.AntiAlias;
            }
            Ikone.Crtaj(g, ikona, new RectangleF(polje.X + 16, polje.Y + 15, 18, 18), Color.White, 1.8f);
        }

        // ===== Prave kontrole =====

        private void PostaviKontrole()
        {
            Rectangle korisnik = UPikselima(okvirKorisnik), lozinka = UPikselima(okvirLozinka);
            Color podlogaKorisnik = BojaPolja(okvirKorisnik), podlogaLozinka = BojaPolja(okvirLozinka);
            int levoUnosa = (int)Math.Round(50 * k);

            cboKorisnik.DropDownStyle = ComboBoxStyle.DropDownList;
            cboKorisnik.FlatStyle = FlatStyle.Flat;
            cboKorisnik.DrawMode = DrawMode.OwnerDrawFixed;
            cboKorisnik.Font = Tekstualna(16 * k);
            cboKorisnik.ItemHeight = (int)Math.Round(28 * k);
            cboKorisnik.BackColor = podlogaKorisnik;
            cboKorisnik.ForeColor = Color.White;
            cboKorisnik.Cursor = Cursors.Hand;
            cboKorisnik.DrawItem += CrtajKorisnika;
            new KomboKuka(cboKorisnik, podlogaKorisnik, Color.White);

            // Isečak je za 2 px niži od kombo-a, pa njegova sistemska ivica ostaje van vidnog polja.
            // Desni kraj je takav da sistemska strelica padne na mesto strelice iz dizajna (14 px od ivice).
            int strelicaSirina = SystemInformation.VerticalScrollBarWidth + 3;
            int desno = Math.Min(korisnik.Right - 1, korisnik.Right - (int)Math.Round(20 * k) + strelicaSirina / 2);
            int visinaIsecka = cboKorisnik.Height - 2;
            isecakKorisnik = new Panel { Parent = this, BackColor = podlogaKorisnik };
            isecakKorisnik.Bounds = new Rectangle(korisnik.X + levoUnosa, korisnik.Y + (korisnik.Height - visinaIsecka) / 2, desno - korisnik.X - levoUnosa, visinaIsecka);
            cboKorisnik.Parent = isecakKorisnik;
            cboKorisnik.SetBounds(-1, -1, isecakKorisnik.Width + 2, cboKorisnik.Height);

            int oko = (int)Math.Round(44 * k);
            txtPassword.Parent = this;
            txtPassword.BorderStyle = BorderStyle.None;
            txtPassword.Font = Tekstualna(16 * k);
            txtPassword.BackColor = podlogaLozinka;
            txtPassword.ForeColor = Color.White;
            txtPassword.SetBounds(lozinka.X + levoUnosa, lozinka.Y + (lozinka.Height - txtPassword.Height) / 2,
                lozinka.Width - levoUnosa - oko - (int)Math.Round(8 * k), txtPassword.Height);

            okoLozinke = new IkonaDugme(Ikona.Oko, "Prikaži lozinku") { Parent = this, Podloga = podlogaLozinka, BojaIkone = Color.White };
            okoLozinke.Bounds = new Rectangle(lozinka.Right - (int)Math.Round(4 * k) - oko, lozinka.Y + (int)Math.Round(2 * k), oko, oko);
            okoLozinke.Click += (s, e) =>
            {
                txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
                okoLozinke.AccessibleName = txtPassword.UseSystemPasswordChar ? "Prikaži lozinku" : "Sakrij lozinku";
            };

            // PRIJAVA: papir, tekst KP duboka, na hover belo i strelica 3 px udesno.
            int mera = (int)Math.Round(16 * k), pomak = (int)Math.Round(3 * k);
            button1.Parent = this;
            button1.Text = "PRIJAVA";
            button1.Font = Tekstualna(15 * k, FontStyle.Bold);
            button1.Style.BackColor = Papir;
            button1.Style.ForeColor = KpDuboka;
            button1.Style.HoverBackColor = Color.White;
            button1.Style.HoverForeColor = KpDuboka;
            button1.Style.PressedBackColor = Color.White;
            button1.Style.PressedForeColor = KpDuboka;
            button1.FocusRectangleVisible = false;
            button1.Cursor = Cursors.Hand;
            // SfButton oslobađa staru sliku kad dobije novu, pa se pri svakoj zameni pravi nova bitmapa.
            button1.Image = Strelica(mera, pomak, 0);
            button1.ImageSize = new Size(mera + pomak, mera);
            button1.TextImageRelation = TextImageRelation.TextBeforeImage;
            button1.TextAlign = ContentAlignment.MiddleRight;
            button1.ImageAlign = ContentAlignment.MiddleLeft;
            button1.MouseEnter += (s, e) => button1.Image = Strelica(mera, pomak, pomak);
            button1.MouseLeave += (s, e) => button1.Image = Strelica(mera, pomak, 0);
            button1.Bounds = UPikselima(new RectangleF(okvirLozinka.X, okvirLozinka.Bottom + 20 + 8, okvirLozinka.Width, 50));

            // Dugmad prozora: 36 × 28, gore 12, desno 16, razmak 4.
            var zatvori = new IkonaDugme(Ikona.Zatvori, "Zatvori") { Parent = this };
            zatvori.Bounds = UPikselima(new RectangleF(SirinaDizajna - 16 - 36, 12, 36, 28));
            zatvori.Click += (s, e) => Close();
            var umanji = new IkonaDugme(Ikona.Minus, "Umanji prozor") { Parent = this };
            umanji.Bounds = UPikselima(new RectangleF(SirinaDizajna - 16 - 36 - 4 - 36, 12, 36, 28));
            umanji.Click += (s, e) => WindowState = FormWindowState.Minimized;
            foreach (var dugme in new[] { zatvori, umanji })
            {
                dugme.Podloga = pozadina.GetPixel(dugme.Left + dugme.Width / 2, dugme.Top + dugme.Height / 2);
                dugme.BojaIkone = Theme.Mesaj(dugme.Podloga, Color.White, .8);
            }

            cboKorisnik.Enter += (s, e) => PostaviFokus(1);
            cboKorisnik.Leave += (s, e) => PostaviFokus(0);
            txtPassword.Enter += (s, e) => PostaviFokus(2);
            txtPassword.Leave += (s, e) => PostaviFokus(0);

            // Kontrole kartice se pojavljuju tek kad kartica završi ulazak.
            PrikaziKontrole(!animacije);
        }

        private Bitmap Strelica(int mera, int rezerva, int pomak)
        {
            var bmp = new Bitmap(mera + rezerva, mera);
            using (var g = Graphics.FromImage(bmp))
                Ikone.Crtaj(g, Ikona.StrelicaDesno, new RectangleF(pomak, 0, mera, mera), KpDuboka, 2.2f);
            return bmp;
        }

        private void PrikaziKontrole(bool vidljive)
        {
            isecakKorisnik.Visible = vidljive;
            txtPassword.Visible = vidljive;
            okoLozinke.Visible = vidljive;
            button1.Visible = vidljive;
        }

        private void PostaviFokus(int polje)
        {
            fokus = polje;
            foreach (RectangleF r in new[] { okvirKorisnik, okvirLozinka })
            {
                RectangleF prsten = r;
                prsten.Inflate(5, 5);
                Invalidate(UPikselima(prsten));
            }
        }

        private void CrtajKorisnika(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool polje = (e.State & DrawItemState.ComboBoxEdit) != 0;
            bool izabran = (e.State & DrawItemState.Selected) != 0;
            Color pozadinaStavke = polje ? cboKorisnik.BackColor : izabran ? Theme.ObaveznoPodloga : Color.White;
            Color tekst = polje ? Color.White : izabran ? Theme.Primarna : Theme.Tekst;
            using (var c = new SolidBrush(pozadinaStavke))
                e.Graphics.FillRectangle(c, e.Bounds);
            Rectangle r = e.Bounds;
            if (!polje) r.X += Theme.Dp(8);
            TextRenderer.DrawText(e.Graphics, cboKorisnik.GetItemText(cboKorisnik.Items[e.Index]), e.Font, r, tekst,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }

        // Klik u polje (i na ikonicu) fokusira unos; ostatak površine pomera prozor.
        private void KlikNaPovrsinu(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (uvodGotov || !animacije)
            {
                if (UPikselima(okvirKorisnik).Contains(e.Location))
                {
                    cboKorisnik.Focus();
                    cboKorisnik.DroppedDown = true;
                    return;
                }
                if (UPikselima(okvirLozinka).Contains(e.Location))
                {
                    txtPassword.Focus();
                    return;
                }
            }
            Win32.ReleaseCapture();
            Win32.SendMessage(Handle, Win32.WM_NCLBUTTONDOWN, new IntPtr(Win32.HTCAPTION), IntPtr.Zero);
        }

        // ===== Animacije =====

        private void PripremiAnimacije()
        {
            using (Stream s = Resurs("Saobracaj.Tema.Slike.lokomotiva-480.png"))
            {
                if (s != null)
                    using (var izvor = new Bitmap(s))
                    {
                        lokomotiva = new Bitmap(izvor.Width, izvor.Height, PixelFormat.Format32bppPArgb);
                        using (var g = Graphics.FromImage(lokomotiva))
                            g.DrawImage(izvor, new Rectangle(0, 0, izvor.Width, izvor.Height));
                        // drop-shadow(0 0 10px rgba(242,197,0,.35)): σ = 5 px dizajna = 10 px slike.
                        sjajLokomotive = Sjaj(izvor, 30, 10, Zuta, .35f);
                    }
            }

            // Tabela dužine luka, da voz ide ravnomerno (offset-distance je po dužini, a ne po t).
            const int koraka = 600;
            duzineLuka = new float[koraka + 1];
            PointF pre = sina[0];
            for (int i = 1; i <= koraka; i++)
            {
                PointF p = Tacka(i / (float)koraka);
                duzineLuka[i] = duzineLuka[i - 1] + (float)Math.Sqrt((p.X - pre.X) * (p.X - pre.X) + (p.Y - pre.Y) * (p.Y - pre.Y));
                pre = p;
            }

            using (var putanja = new GraphicsPath())
            using (var sirina = new Pen(Color.Black, 6))
            using (var mera = new Matrix())
            {
                putanja.AddBezier(sina[0], sina[1], sina[2], sina[3]);
                putanja.Widen(sirina);
                mera.Scale(k, k);
                putanja.Transform(mera);
                oblastIsprekidane = new Region(putanja);
            }

            otkucaj = new Timer { Interval = 16 };
            otkucaj.Tick += Otkucaj;
        }

        private void PokreniAnimacije()
        {
            if (!animacije)
            {
                uvodGotov = true;
                return;
            }
            sat.Start();
            otkucaj.Start();
        }

        // Prozor ostaje otvoren iza glavne forme posle prijave: tada (i kad je umanjen) animacija miruje.
        private bool Miruje
        {
            get { return WindowState == FormWindowState.Minimized || !Visible || Application.OpenForms.Count > 1; }
        }

        private void Otkucaj(object sender, EventArgs e)
        {
            vreme = (float)sat.Elapsed.TotalSeconds;

            // Uvod se završava i kad animacija miruje, da kontrole za prijavu sigurno postanu vidljive.
            if (!uvodGotov)
            {
                Invalidate(UPikselima(Prosiri(OkvirSlojaBrenda(), 0, 16)));
                Invalidate(UPikselima(Prosiri(kartica, 0, 16)));
                if (vreme >= KasnjenjeKartice + TrajanjeUvoda)
                {
                    uvodGotov = true;
                    PrikaziKontrole(true);
                    txtPassword.Focus();
                }
            }
            if (Miruje) return;

            Invalidate(oblastIsprekidane);
            Invalidate(UPikselima(Prosiri(znak, 12, 12)));

            Rectangle novaOblast = OblastVoza();
            Invalidate(Rectangle.Union(oblastVoza, novaOblast));
            oblastVoza = novaOblast;
        }

        private static RectangleF Prosiri(RectangleF r, float x, float y)
        {
            r.Inflate(x, y);
            return r;
        }

        private static PointF Tacka(float t)
        {
            float u = 1 - t;
            float a = u * u * u, b = 3 * u * u * t, c = 3 * u * t * t, d = t * t * t;
            return new PointF(a * sina[0].X + b * sina[1].X + c * sina[2].X + d * sina[3].X,
                              a * sina[0].Y + b * sina[1].Y + c * sina[2].Y + d * sina[3].Y);
        }

        private static float Ugao(float t)
        {
            float u = 1 - t;
            float dx = 3 * u * u * (sina[1].X - sina[0].X) + 6 * u * t * (sina[2].X - sina[1].X) + 3 * t * t * (sina[3].X - sina[2].X);
            float dy = 3 * u * u * (sina[1].Y - sina[0].Y) + 6 * u * t * (sina[2].Y - sina[1].Y) + 3 * t * t * (sina[3].Y - sina[2].Y);
            return (float)(Math.Atan2(dy, dx) * 180 / Math.PI);
        }

        private float TPoDuzini(float udeo)
        {
            float cilj = udeo * duzineLuka[duzineLuka.Length - 1];
            int a = 0, b = duzineLuka.Length - 1;
            while (b - a > 1)
            {
                int m = (a + b) / 2;
                if (duzineLuka[m] < cilj) a = m; else b = m;
            }
            float deo = duzineLuka[b] - duzineLuka[a];
            float u = deo > 0 ? (cilj - duzineLuka[a]) / deo : 0;
            return (a + u) / (duzineLuka.Length - 1);
        }

        private float NapredakVoza()
        {
            return vreme % CiklusVoza / CiklusVoza;
        }

        // Transformacija voza: tačka na šini, rotacija po tangenti, skala 1 → .42 oko tačke prianjanja (62% / 92%).
        private Matrix MatricaVoza(float napredak)
        {
            float t = TPoDuzini(napredak);
            PointF p = Tacka(t);
            float skala = 1 - .58f * napredak;
            var m = new Matrix();
            m.Scale(k, k);
            m.Translate(p.X, p.Y);
            m.Rotate(Ugao(t));
            m.Scale(skala, skala);
            m.Translate(-240 * .62f, -64 * .92f);
            return m;
        }

        private Rectangle OblastVoza()
        {
            if (lokomotiva == null) return Rectangle.Empty;
            using (Matrix m = MatricaVoza(NapredakVoza()))
            {
                PointF[] uglovi = { new PointF(-15, -15), new PointF(255, -15), new PointF(255, 79), new PointF(-15, 79) };
                m.TransformPoints(uglovi);
                float x1 = float.MaxValue, y1 = float.MaxValue, x2 = float.MinValue, y2 = float.MinValue;
                foreach (PointF u in uglovi)
                {
                    x1 = Math.Min(x1, u.X); y1 = Math.Min(y1, u.Y);
                    x2 = Math.Max(x2, u.X); y2 = Math.Max(y2, u.Y);
                }
                return Rectangle.FromLTRB((int)x1 - 2, (int)y1 - 2, (int)x2 + 3, (int)y2 + 3);
            }
        }

        private static float Uvod(float t)
        {
            return KubniBezier(t, .2f, .7f, .2f, 1f);
        }

        // CSS cubic-bezier(x1, y1, x2, y2).
        private static float KubniBezier(float t, float x1, float y1, float x2, float y2)
        {
            if (t <= 0) return 0;
            if (t >= 1) return 1;
            float a = 0, b = 1, u = t;
            for (int i = 0; i < 24; i++)
            {
                u = (a + b) / 2;
                float x = 3 * (1 - u) * (1 - u) * u * x1 + 3 * (1 - u) * u * u * x2 + u * u * u;
                if (x < t) a = u; else b = u;
            }
            return 3 * (1 - u) * (1 - u) * u * y1 + 3 * (1 - u) * u * u * y2 + u * u * u;
        }

        // Keyframes 0%,100% → 1, 50% → najmanje, ease-in-out na svakoj polovini.
        private static float Puls(float vreme, float ciklus, float najmanje)
        {
            if (vreme < 0) return 1;
            float f = vreme % ciklus / ciklus;
            float polovina = f < .5f ? f * 2 : (f - .5f) * 2;
            float e = KubniBezier(polovina, .42f, 0, .58f, 1);
            return f < .5f ? 1 - (1 - najmanje) * e : najmanje + (1 - najmanje) * e;
        }

        // ===== Crtanje =====

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.DrawImage(pozadina, e.ClipRectangle, e.ClipRectangle, GraphicsUnit.Pixel);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            GraphicsState pocetno = g.Save();
            g.ScaleTransform(k, k);
            CrtajIsprekidanu(g);
            g.Restore(pocetno);

            if (animacije && sat.IsRunning) CrtajVoz(g);

            float uvodBrenda = animacije ? Uvod(Math.Min(1, vreme / TrajanjeUvoda)) : 1;
            float uvodKartice = animacije ? Uvod(Math.Max(0, Math.Min(1, (vreme - KasnjenjeKartice) / TrajanjeUvoda))) : 1;

            CrtajSloj(g, slojBrenda, OkvirSlojaBrenda(), uvodBrenda);
            pocetno = g.Save();
            g.ScaleTransform(k, k);
            g.TranslateTransform(0, 14 * (1 - uvodBrenda));
            CrtajZnak(g, uvodBrenda);
            g.Restore(pocetno);

            CrtajSloj(g, slojKartice, kartica, uvodKartice);

            if (fokus != 0)
            {
                pocetno = g.Save();
                g.ScaleTransform(k, k);
                RectangleF r = fokus == 1 ? okvirKorisnik : okvirLozinka;
                r.Inflate(2, 2);
                using (var prsten = new Pen(Theme.RozeNaBordo, 2))
                    g.DrawRectangle(prsten, r.X, r.Y, r.Width, r.Height);
                g.Restore(pocetno);
            }
        }

        private void CrtajIsprekidanu(Graphics g)
        {
            // stroke-dasharray 14 12 (u širinama olovke 7 i 6), dashoffset 0 → -52 za 1.4 s.
            using (var olovka = new Pen(Color.FromArgb(140, Zuta), 2) { DashStyle = DashStyle.Custom, DashPattern = new[] { 7f, 6f } })
            {
                if (animacije) olovka.DashOffset = -(vreme % CiklusIsprekidane / CiklusIsprekidane) * 26;
                g.DrawBezier(olovka, sina[0], sina[1], sina[2], sina[3]);
            }
        }

        private void CrtajSloj(Graphics g, Bitmap sloj, RectangleF okvir, float uvod)
        {
            if (uvod <= 0) return;
            Rectangle p = UPikselima(okvir);
            float y = p.Y + 14 * k * (1 - uvod);
            if (uvod >= 1)
            {
                g.DrawImage(sloj, new Rectangle(p.X, p.Y, sloj.Width, sloj.Height));
                return;
            }
            using (var atributi = Providnost(uvod))
                g.DrawImage(sloj, new[] { new PointF(p.X, y), new PointF(p.X + sloj.Width, y), new PointF(p.X, y + sloj.Height) },
                    new RectangleF(0, 0, sloj.Width, sloj.Height), GraphicsUnit.Pixel, atributi);
        }

        private static ImageAttributes Providnost(float alfa)
        {
            var atributi = new ImageAttributes();
            atributi.SetColorMatrix(new ColorMatrix { Matrix33 = alfa });
            return atributi;
        }

        // Znak: dve KP strelice koje pulsiraju (2.4 s, druga kasni .3 s) i talas oko žutog čvora (2 s).
        private void CrtajZnak(Graphics g, float alfa)
        {
            if (alfa <= 0) return;
            g.TranslateTransform(znak.X, znak.Y);
            g.ScaleTransform(znak.Width / 120f, znak.Height / 120f);

            float prva = animacije ? Puls(vreme, 2.4f, .4f) : 1;
            float druga = animacije ? Puls(vreme - .3f, 2.4f, .4f) : 1;
            using (var p1 = new Pen(Color.FromArgb((int)(255 * alfa * prva), Color.White), 14) { LineJoin = LineJoin.Miter })
                g.DrawLines(p1, new[] { new PointF(16, 22), new PointF(48, 60), new PointF(16, 98) });
            using (var p2 = new Pen(Color.FromArgb((int)(255 * alfa * druga), Color.White), 14) { LineJoin = LineJoin.Miter })
                g.DrawLines(p2, new[] { new PointF(44, 22), new PointF(76, 60), new PointF(44, 98) });

            // ease-out = cubic-bezier(0, 0, .58, 1); skala 1 → 2.6, providnost .9 → 0.
            float talas = animacije ? KubniBezier(vreme % 2f / 2f, 0, 0, .58f, 1) : 0;
            float razmera = 1 + 1.6f * talas;
            float providnost = animacije ? .9f * (1 - talas) : 1;
            using (var prsten = new Pen(Color.FromArgb((int)(255 * alfa * providnost), Zuta), 4 * razmera))
                g.DrawEllipse(prsten, 100 - 12 * razmera, 60 - 12 * razmera, 24 * razmera, 24 * razmera);
            using (var cvor = new SolidBrush(Color.FromArgb((int)(255 * alfa), Zuta)))
                g.FillEllipse(cvor, 88, 48, 24, 24);
        }

        private void CrtajVoz(Graphics g)
        {
            if (lokomotiva == null) return;
            float napredak = NapredakVoza();
            // Pojavljivanje prvih 6%, nestajanje posle 86%.
            float alfa = napredak < .06f ? napredak / .06f : napredak > .86f ? (1 - napredak) / .14f : 1;
            if (alfa <= 0) return;

            GraphicsState stanje = g.Save();
            using (Matrix m = MatricaVoza(napredak))
                g.Transform = m;
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            using (var atributi = Providnost(alfa))
            {
                g.DrawImage(sjajLokomotive, new Rectangle(-15, -15, 270, 94), 0, 0, sjajLokomotive.Width, sjajLokomotive.Height, GraphicsUnit.Pixel, atributi);
                g.DrawImage(lokomotiva, new Rectangle(0, 0, 240, 64), 0, 0, lokomotiva.Width, lokomotiva.Height, GraphicsUnit.Pixel, atributi);
            }
            // Far treperi (1 → .55, 1.2 s): preko njega se nanosi žuta boja tela lokomotive.
            float far = Puls(vreme, 1.2f, .55f);
            using (var telo = new SolidBrush(Color.FromArgb((int)(255 * (1 - far) * alfa), Zuta)))
                g.FillEllipse(telo, 235.5f - 2.6f, 43 - 2.6f, 5.2f, 5.2f);
            g.Restore(stanje);
        }

        // Žuti sjaj oko lokomotive: alfa slike zamućena sa tri prolaza box blur-a (≈ Gausov σ).
        private static Bitmap Sjaj(Bitmap izvor, int okvir, float sigma, Color boja, float jacina)
        {
            int s = izvor.Width + 2 * okvir, v = izvor.Height + 2 * okvir;
            var alfa = new float[s * v];
            using (var argb = new Bitmap(izvor.Width, izvor.Height, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(argb))
                    g.DrawImage(izvor, new Rectangle(0, 0, izvor.Width, izvor.Height));
                BitmapData podaci = argb.LockBits(new Rectangle(0, 0, argb.Width, argb.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var red = new int[argb.Width];
                for (int y = 0; y < argb.Height; y++)
                {
                    Marshal.Copy(podaci.Scan0 + y * podaci.Stride, red, 0, argb.Width);
                    for (int x = 0; x < argb.Width; x++)
                        alfa[(y + okvir) * s + x + okvir] = (red[x] >> 24 & 0xFF) / 255f;
                }
                argb.UnlockBits(podaci);
            }

            int r = (int)((Math.Sqrt(12 * sigma * sigma / 3 + 1) - 1) / 2);
            var pomocni = new float[alfa.Length];
            for (int prolaz = 0; prolaz < 3; prolaz++)
            {
                Zamuti(alfa, pomocni, s, v, r, 1, s);
                Zamuti(pomocni, alfa, v, s, r, s, 1);
            }

            var rezultat = new Bitmap(s, v, PixelFormat.Format32bppPArgb);
            var piksel = new int[s * v];
            for (int i = 0; i < piksel.Length; i++)
            {
                int a = (int)(Math.Min(1, alfa[i]) * jacina * 255);
                piksel[i] = a << 24 | boja.R * a / 255 << 16 | boja.G * a / 255 << 8 | boja.B * a / 255;
            }
            BitmapData cilj = rezultat.LockBits(new Rectangle(0, 0, s, v), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
            for (int y = 0; y < v; y++)
                Marshal.Copy(piksel, y * s, cilj.Scan0 + y * cilj.Stride, s);
            rezultat.UnlockBits(cilj);
            return rezultat;
        }

        // Box blur duž jedne ose: "duzina" elemenata po liniji, "linija" linija, korak unutar linije i između linija.
        private static void Zamuti(float[] iz, float[] u, int duzina, int linija, int r, int korak, int skok)
        {
            float delilac = 2 * r + 1;
            for (int l = 0; l < linija; l++)
            {
                int pocetak = l * skok;
                float zbir = 0;
                for (int i = -r; i <= r; i++)
                    if (i >= 0 && i < duzina) zbir += iz[pocetak + i * korak];
                for (int i = 0; i < duzina; i++)
                {
                    u[pocetak + i * korak] = zbir / delilac;
                    int izlazi = i - r, ulazi = i + r + 1;
                    if (izlazi >= 0) zbir -= iz[pocetak + izlazi * korak];
                    if (ulazi < duzina) zbir += iz[pocetak + ulazi * korak];
                }
            }
        }

        private static Stream Resurs(string ime)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(ime);
        }

        private void Oslobodi()
        {
            otkucaj?.Dispose();
            oblastIsprekidane?.Dispose();
            foreach (var bmp in new[] { pozadina, slojBrenda, slojKartice, lokomotiva, sjajLokomotive })
                bmp?.Dispose();
            foreach (Font f in fontovi)
                f.Dispose();
            tipografski.Dispose();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x00020000;
                CreateParams p = base.CreateParams;
                p.ClassStyle |= CS_DROPSHADOW;
                return p;
            }
        }
    }
}
