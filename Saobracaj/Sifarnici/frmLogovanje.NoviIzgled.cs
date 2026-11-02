using System;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Sifarnici
{
    // Novi izgled logovanja (staklena kartica). Prijava i učitavanje korisnika ostaju u frmLogovanje.cs.
    public partial class frmLogovanje
    {
        private GlassPanel povrsina;
        private IkonaDugme okoLozinke;
        private Panel isecakKorisnik;
        private Panel isecakLozinka;
        private GlassPanel.GlassPolje poljeKorisnik;
        private GlassPanel.GlassPolje poljeLozinka;
        private Timer animacijaDugmeta;
        private int vrhDugmeta;

        private void PrimeniNoviIzgled()
        {
            SuspendLayout();
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Rectangle radno = Screen.PrimaryScreen.WorkingArea;
            ClientSize = new Size(Math.Min(Theme.Dp(1120), radno.Width), Math.Min(Theme.Dp(700), radno.Height));

            povrsina = new GlassPanel { Dock = DockStyle.Fill };
            povrsina.MouseDown += PomeranjeProzora;
            Controls.Add(povrsina);
            pictureBox1.Visible = false;
            pictureBox2.Visible = false;

            isecakKorisnik = new Panel { Parent = povrsina };
            isecakLozinka = new Panel { Parent = povrsina };

            foreach (var oznaka in new[] { autoLabel1, autoLabel2 })
            {
                oznaka.Parent = povrsina;
                oznaka.Font = Theme.Tekstualni(9f, true);
                oznaka.ForeColor = Theme.RozeNaBordo;
                oznaka.BackColor = Color.Transparent;
            }
            autoLabel1.Text = "KORISNIK";
            autoLabel2.Text = "LOZINKA";

            cboKorisnik.Parent = isecakKorisnik;
            cboKorisnik.DropDownStyle = ComboBoxStyle.DropDownList;
            cboKorisnik.FlatStyle = FlatStyle.Flat;
            cboKorisnik.DrawMode = DrawMode.OwnerDrawFixed;
            cboKorisnik.Font = Theme.Tekstualni(11f);
            cboKorisnik.ItemHeight = Theme.Dp(26);
            cboKorisnik.ForeColor = Color.White;
            cboKorisnik.DrawItem += CrtajKorisnika;
            new KomboKuka(cboKorisnik, cboKorisnik.BackColor, Color.White);

            txtPassword.Parent = isecakLozinka;
            txtPassword.BorderStyle = BorderStyle.None;
            txtPassword.Font = Theme.Tekstualni(11f);
            txtPassword.ForeColor = Color.White;

            okoLozinke = new IkonaDugme(Ikona.Oko, "Prikaži lozinku") { Parent = povrsina, BojaIkone = Theme.RozeNaBordo };
            okoLozinke.Click += (s, e) => txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;

            button1.Parent = povrsina;
            button1.Text = "PRIJAVA";
            button1.Font = Theme.Tekstualni(10.5f, true);
            button1.Style.BackColor = Color.White;
            button1.Style.ForeColor = Theme.Primarna;
            button1.Style.HoverBackColor = Theme.ZaglavljeTabele;
            button1.Style.HoverForeColor = Theme.Primarna;
            button1.Style.PressedBackColor = Theme.SvetloNaBordo;
            button1.Style.PressedForeColor = Theme.Primarna;
            button1.FocusRectangleVisible = false;
            button1.Cursor = Cursors.Hand;
            button1.Image = Ikone.Slika(Ikona.StrelicaDesno, Theme.Dp(18), Theme.Primarna, 2.2f);
            button1.ImageSize = new Size(Theme.Dp(18), Theme.Dp(18));
            button1.TextImageRelation = TextImageRelation.TextBeforeImage;
            button1.TextAlign = ContentAlignment.MiddleRight;
            button1.ImageAlign = ContentAlignment.MiddleLeft;
            button1.MouseEnter += (s, e) => PomeriDugme(true);
            button1.MouseLeave += (s, e) => PomeriDugme(false);

            var zatvori = new IkonaDugme(Ikona.Zatvori, "Zatvori") { Parent = povrsina };
            zatvori.Click += (s, e) => Close();
            var umanji = new IkonaDugme(Ikona.Minus, "Umanji") { Parent = povrsina };
            umanji.Click += (s, e) => WindowState = FormWindowState.Minimized;

            cboKorisnik.Enter += (s, e) => povrsina.PostaviFokus(poljeKorisnik, true);
            cboKorisnik.Leave += (s, e) => povrsina.PostaviFokus(poljeKorisnik, false);
            txtPassword.Enter += (s, e) => povrsina.PostaviFokus(poljeLozinka, true);
            txtPassword.Leave += (s, e) => povrsina.PostaviFokus(poljeLozinka, false);

            povrsina.Size = ClientSize;
            Rasporedi(zatvori, umanji);
            povrsina.BringToFront();
            ResumeLayout(true);
        }

        private void Rasporedi(IkonaDugme zatvori, IkonaDugme umanji)
        {
            int s = ClientSize.Width, v = ClientSize.Height;
            int sirinaKartice = Math.Min(Theme.Dp(420), s - Theme.Dp(32));
            int visinaKartice = Theme.Dp(388);
            bool saUvodom = s >= Theme.Dp(860);
            int xKartice = saUvodom ? s - sirinaKartice - Theme.Dp(84) : (s - sirinaKartice) / 2;
            var kartica = new Rectangle(xKartice, (v - visinaKartice) / 2, sirinaKartice, visinaKartice);
            povrsina.Postavi(kartica, g => CrtajTekstove(g, kartica, saUvodom));

            int levo = kartica.X + Theme.Dp(36);
            int sirina = kartica.Width - 2 * Theme.Dp(36);
            int y = kartica.Y + Theme.Dp(116);
            int visinaPolja = Theme.Dp(48);

            autoLabel1.Location = new Point(levo - 3, y);
            var okvirKorisnik = new Rectangle(levo, y + Theme.Dp(22), sirina, visinaPolja);
            y = okvirKorisnik.Bottom + Theme.Dp(18);
            autoLabel2.Location = new Point(levo - 3, y);
            var okvirLozinka = new Rectangle(levo, y + Theme.Dp(22), sirina, visinaPolja);

            poljeKorisnik = povrsina.DodajPolje(okvirKorisnik, Ikona.Korisnik, boja =>
            {
                isecakKorisnik.BackColor = boja;
                cboKorisnik.BackColor = boja;
            });
            poljeLozinka = povrsina.DodajPolje(okvirLozinka, Ikona.Brava, boja =>
            {
                isecakLozinka.BackColor = boja;
                txtPassword.BackColor = boja;
                okoLozinke.Podloga = boja;
                okoLozinke.Invalidate();
            });

            int levoUnosa = Theme.Dp(46);
            // Isečak je za 2 px niži od kombo-a, pa njegova sistemska ivica ostaje van vidnog polja.
            int visinaIsecka = cboKorisnik.Height - 2;
            isecakKorisnik.Bounds = new Rectangle(okvirKorisnik.X + levoUnosa, okvirKorisnik.Y + (okvirKorisnik.Height - visinaIsecka) / 2, okvirKorisnik.Width - levoUnosa - 1, visinaIsecka);
            cboKorisnik.SetBounds(-1, -1, isecakKorisnik.Width + 2, cboKorisnik.Height);

            int mestoOka = Theme.Dp(40);
            isecakLozinka.Bounds = new Rectangle(okvirLozinka.X + levoUnosa, okvirLozinka.Y + 1, okvirLozinka.Width - levoUnosa - mestoOka - 2, okvirLozinka.Height - 2);
            txtPassword.SetBounds(0, (isecakLozinka.Height - txtPassword.Height) / 2, isecakLozinka.Width, txtPassword.Height);
            okoLozinke.Bounds = new Rectangle(okvirLozinka.Right - mestoOka - 2, okvirLozinka.Y + (visinaPolja - Theme.Dp(38)) / 2, mestoOka, Theme.Dp(38));

            vrhDugmeta = okvirLozinka.Bottom + Theme.Dp(26);
            button1.SetBounds(levo, vrhDugmeta, sirina, Theme.Dp(50));

            int mera = Theme.Dp(34);
            zatvori.Bounds = new Rectangle(s - mera - Theme.Dp(10), Theme.Dp(10), mera, mera);
            umanji.Bounds = new Rectangle(zatvori.Left - mera - Theme.Dp(4), Theme.Dp(10), mera, mera);
            zatvori.Podloga = povrsina.Uzorak(zatvori.Location);
            umanji.Podloga = povrsina.Uzorak(umanji.Location);
        }

        private void CrtajTekstove(Graphics g, Rectangle kartica, bool saUvodom)
        {
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using (var bela = new SolidBrush(Color.White))
            using (var svetla = new SolidBrush(Theme.SvetloNaBordo))
            using (var roze = new SolidBrush(Theme.RozeNaBordo))
            {
                g.DrawString("Logovanje", Theme.Naslovni(20f), bela, kartica.X + Theme.Dp(32), kartica.Y + Theme.Dp(30));
                g.DrawString("Prijavite se na svoj nalog.", Theme.Tekstualni(10.5f), svetla, kartica.X + Theme.Dp(33), kartica.Y + Theme.Dp(74));

                if (!saUvodom) return;
                int x = Theme.Dp(84);
                int y = (ClientSize.Height - Theme.Dp(280)) / 2;
                Ikone.Crtaj(g, Ikona.Strelice, new RectangleF(x - Theme.Dp(6), y - Theme.Dp(6), Theme.Dp(38), Theme.Dp(38)), Color.White, 2.2f);
                g.DrawString("KOMBINOVANI PREVOZ", Theme.Tekstualni(9f, true), roze, x + Theme.Dp(38), y + Theme.Dp(5));
                y += Theme.Dp(56);
                using (var linija = new Pen(Theme.RozeNaBordo))
                    g.DrawLine(linija, x, y + Theme.Dp(9), x + Theme.Dp(44), y + Theme.Dp(9));
                g.DrawString("ŽELEZNIČKI PREVOZ TERETA · SRBIJA", Theme.Tekstualni(9f, true), roze, x + Theme.Dp(52), y);
                y += Theme.Dp(30);
                g.DrawString("Saobraćaj", Theme.Naslovni(46f), bela, x - Theme.Dp(10), y);
                y += Theme.Dp(92);
                var mesto = new RectangleF(x - Theme.Dp(2), y, Theme.Dp(440), Theme.Dp(70));
                g.DrawString("Planiranje, najave i praćenje vozova i vagona na jednom mestu.", Theme.Tekstualni(13f), svetla, mesto);
            }
        }

        private void CrtajKorisnika(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool polje = (e.State & DrawItemState.ComboBoxEdit) != 0;
            bool izabran = (e.State & DrawItemState.Selected) != 0;
            Color pozadina = polje ? cboKorisnik.BackColor : izabran ? Theme.ObaveznoPodloga : Color.White;
            Color tekst = polje ? Color.White : izabran ? Theme.Primarna : Theme.Tekst;
            using (var c = new SolidBrush(pozadina))
                e.Graphics.FillRectangle(c, e.Bounds);
            Rectangle r = e.Bounds;
            if (!polje) r.X += Theme.Dp(8);
            TextRenderer.DrawText(e.Graphics, cboKorisnik.GetItemText(cboKorisnik.Items[e.Index]), e.Font, r, tekst,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }

        private void PomeriDugme(bool gore)
        {
            animacijaDugmeta?.Dispose();
            int pocetak = button1.Top;
            int cilj = vrhDugmeta - (gore ? Theme.Dp(2) : 0);
            double senkaOd = (vrhDugmeta - pocetak) / (double)Theme.Dp(2);
            double senkaDo = gore ? 1 : 0;
            animacijaDugmeta = Animator.Pokreni(this, Theme.TrajanjeHover, Animator.IzlazKubni, t =>
            {
                button1.Top = pocetak + (int)Math.Round((cilj - pocetak) * t);
                povrsina.PostaviSenku(button1.Bounds, senkaOd + (senkaDo - senkaOd) * t);
            });
        }

        private void PomeranjeProzora(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            Win32.ReleaseCapture();
            Win32.SendMessage(Handle, Win32.WM_NCLBUTTONDOWN, new IntPtr(Win32.HTCAPTION), IntPtr.Zero);
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
