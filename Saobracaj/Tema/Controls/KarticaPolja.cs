using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Saobracaj.Tema.Controls
{
    public interface IKartica
    {
        // Raspoređuje sadržaj za zadatu širinu i vraća potrebnu visinu.
        int Rasporedi(int sirina);
    }

    // Kartica sa naslovom i poljima u dve kolone; sadržaj raspoređuje sama prema širini.
    public class KarticaPolja : Panel, IKartica
    {
        private class Stavka
        {
            public Control Oznaka;
            public Control Glavna;
            public List<Control> Dodaci = new List<Control>();
            public List<CheckBox> Cekiranja;
            public List<Control> Dugmad;
            public bool Siroko;
            public int Visina;
        }

        private static int VisinaZaglavlja => Theme.Dp(38);
        private static int Unutra => Theme.Dp(14);
        private static int RazmakKolona => Theme.Dp(12);
        private static int RazmakRedova => Theme.Dp(4);
        private static int VisinaOznake => Theme.Dp(16);

        private readonly string naslov;
        private readonly List<Stavka> stavke = new List<Stavka>();

        public KarticaPolja(string naslov)
        {
            this.naslov = naslov;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.White;
        }

        public KarticaPolja Polje(Label oznaka, Control ulaz, bool siroko = false, bool obavezno = false, int visina = 0, params Control[] dodaci)
        {
            var stavka = new Stavka { Oznaka = oznaka, Siroko = siroko, Visina = visina };
            ThemeApplier.StilOznake(oznaka);
            oznaka.Text = oznaka.Text.Trim().TrimEnd(':').Trim() + (obavezno ? " *" : "");
            // Providna oznaka ne sme da pređe preko gornje ivice polja ispod sebe.
            oznaka.AutoSize = false;
            oznaka.Size = new Size(oznaka.PreferredWidth, VisinaOznake + OkvirPolja.Prsten);
            oznaka.Parent = this;
            stavka.Glavna = new OkvirPolja(ulaz, obavezno) { Parent = this };
            foreach (Control d in dodaci)
                stavka.Dodaci.Add(PripremiDodatak(d));
            stavke.Add(stavka);
            return this;
        }

        public KarticaPolja Sadrzaj(Control kontrola, int visina)
        {
            kontrola.Parent = this;
            kontrola.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            stavke.Add(new Stavka { Glavna = kontrola, Siroko = true, Visina = visina });
            return this;
        }

        public KarticaPolja Cekiranja(params CheckBox[] polja)
        {
            foreach (CheckBox c in polja)
            {
                ThemeApplier.StilCheckBoxa(c);
                c.AutoSize = true;
                c.Parent = this;
            }
            stavke.Add(new Stavka { Cekiranja = new List<CheckBox>(polja), Siroko = true });
            return this;
        }

        public KarticaPolja Dugmad(params Control[] dugmad)
        {
            foreach (Control b in dugmad)
            {
                if (b is Button && !(b is MetroFramework.Controls.MetroButton))
                {
                    ThemeApplier.StilDugmeta((Button)b, false);
                    b.Padding = Padding.Empty;
                }
                else if (b is Syncfusion.WinForms.Controls.SfButton)
                    ThemeApplier.StilSfDugmeta((Syncfusion.WinForms.Controls.SfButton)b);
                else if (b is MetroFramework.Controls.MetroButton)
                {
                    var m = (MetroFramework.Controls.MetroButton)b;
                    m.UseCustomBackColor = true;
                    m.UseCustomForeColor = true;
                    m.BackColor = Color.White;
                    m.ForeColor = Theme.TekstSporedni;
                }
                b.Parent = this;
                b.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                b.Height = Theme.Dp(28);
                b.Width = TextRenderer.MeasureText(b.Text, Theme.Tekstualni(9f, true)).Width + Theme.Dp(24);
            }
            stavke.Add(new Stavka { Dugmad = new List<Control>(dugmad), Siroko = true });
            return this;
        }

        // Oznaka koja nije vezana za polje; logika forme može da joj menja tekst tokom rada.
        public KarticaPolja Tekst(Label oznaka)
        {
            ThemeApplier.StilOznake(oznaka);
            oznaka.AutoSize = false;
            oznaka.Parent = this;
            oznaka.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            stavke.Add(new Stavka { Glavna = oznaka, Siroko = true, Visina = Theme.Dp(18) });
            return this;
        }

        private Control PripremiDodatak(Control d)
        {
            var dugme = d as Button;
            if (dugme != null)
            {
                ThemeApplier.StilDugmeta(dugme, false);
                dugme.Parent = this;
                dugme.Padding = Padding.Empty;
                dugme.Width = dugme.Text.Trim().Length > 1 ? TextRenderer.MeasureText(dugme.Text, dugme.Font).Width + Theme.Dp(14) : Theme.Dp(24);
                dugme.Height = OkvirPolja.VisinaPolja;
                return dugme;
            }
            var oznaka = d as Label;
            if (oznaka != null)
            {
                ThemeApplier.StilOznake(oznaka);
                oznaka.Text = oznaka.Text.Replace("(", "").Replace(")", "").Trim();
                oznaka.AutoSize = true;
                oznaka.Parent = this;
                return oznaka;
            }
            return new OkvirPolja(d) { Parent = this, Width = Theme.Dp(64) };
        }

        // Raspoređuje polja za zadatu širinu i vraća potrebnu visinu kartice.
        public int Rasporedi(int sirina)
        {
            int kolona = (sirina - 2 * Unutra - RazmakKolona) / 2;
            int y = VisinaZaglavlja + Theme.Dp(8);
            int x = 0;
            int visinaReda = 0;

            foreach (Stavka s in stavke)
            {
                if (s.Siroko || x == 2)
                {
                    if (x > 0) { y += visinaReda + RazmakRedova; x = 0; visinaReda = 0; }
                }
                int levo = Unutra + x * (kolona + RazmakKolona);
                int siroko = s.Siroko ? sirina - 2 * Unutra : kolona;
                int visina;
                if (s.Cekiranja != null)
                    visina = RasporediCekiranja(s, levo, y, siroko);
                else if (s.Dugmad != null)
                    visina = RasporediDugmad(s, levo, y, siroko);
                else if (s.Oznaka == null)
                {
                    s.Glavna.SetBounds(levo, y, siroko, s.Visina);
                    visina = s.Visina + Theme.Dp(4);
                }
                else
                    visina = RasporediPolje(s, levo, y, siroko);

                visinaReda = Math.Max(visinaReda, visina);
                if (s.Siroko)
                {
                    y += visinaReda + RazmakRedova;
                    visinaReda = 0;
                }
                else
                    x++;
            }
            if (x > 0) y += visinaReda + RazmakRedova;
            return y + Theme.Dp(8);
        }

        private int RasporediPolje(Stavka s, int levo, int y, int sirina)
        {
            s.Oznaka.Location = new Point(levo + OkvirPolja.Prsten - 3, y);
            int visinaOkvira = (s.Visina > 0 ? s.Visina : OkvirPolja.VisinaPolja) + 2 * OkvirPolja.Prsten;
            int vrh = y + VisinaOznake;

            int desno = levo + sirina;
            for (int i = s.Dodaci.Count - 1; i >= 0; i--)
            {
                Control d = s.Dodaci[i];
                if (d is OkvirPolja)
                {
                    d.SetBounds(desno - d.Width, vrh, d.Width, visinaOkvira);
                    desno -= d.Width;
                }
                else
                {
                    d.Location = new Point(desno - d.Width - OkvirPolja.Prsten, vrh + (visinaOkvira - d.Height) / 2);
                    desno -= d.Width + Theme.Dp(4);
                }
            }
            s.Glavna.SetBounds(levo, vrh, Math.Max(Theme.Dp(40), desno - levo), visinaOkvira);
            return VisinaOznake + visinaOkvira;
        }

        private int RasporediCekiranja(Stavka s, int levo, int y, int sirina)
        {
            int x = levo + OkvirPolja.Prsten;
            int red = y + Theme.Dp(6);
            int visina = Theme.Dp(22);
            foreach (CheckBox c in s.Cekiranja)
            {
                if (x + c.Width > levo + sirina && x > levo + OkvirPolja.Prsten)
                {
                    x = levo + OkvirPolja.Prsten;
                    red += visina;
                }
                c.Location = new Point(x, red);
                x += c.Width + Theme.Dp(14);
            }
            return red - y + visina;
        }

        private int RasporediDugmad(Stavka s, int levo, int y, int sirina)
        {
            int x = levo + OkvirPolja.Prsten;
            int red = y + Theme.Dp(6);
            int visina = Theme.Dp(32);
            foreach (Control b in s.Dugmad)
            {
                if (x + b.Width > levo + sirina && x > levo + OkvirPolja.Prsten)
                {
                    x = levo + OkvirPolja.Prsten;
                    red += visina;
                }
                b.Location = new Point(x, red);
                x += b.Width + Theme.Dp(8);
            }
            return red - y + visina;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            using (var ivica = new Pen(Theme.Linija))
                g.DrawRectangle(ivica, 0, 0, Width - 1, Height - 1);
            using (var linija = new Pen(Theme.Pozadina))
                g.DrawLine(linija, 1, VisinaZaglavlja, Width - 2, VisinaZaglavlja);
            using (var traka = new SolidBrush(Theme.Primarna))
                g.FillRectangle(traka, Unutra, Theme.Dp(11), 3, Theme.Dp(16));
            TextRenderer.DrawText(g, naslov, Theme.Naslovni(10.5f), new Point(Unutra + Theme.Dp(10), Theme.Dp(10)), Theme.Primarna);
        }
    }

    // Kartice u kolonama; broj kolona zavisi od širine, a višak visine se skroluje.
    public class MrezaKartica : Panel
    {
        private readonly List<Control> kartice = new List<Control>();
        private static int Razmak => Theme.Dp(14);
        private bool radi;

        public int MaxKolona { get; set; } = 3;
        public int PotrebnaVisina { get; private set; }
        public event EventHandler VisinaPromenjena;

        public MrezaKartica()
        {
            AutoScroll = true;
            DoubleBuffered = true;
            BackColor = Theme.Pozadina;
        }

        public void Dodaj<T>(T kartica) where T : Control, IKartica
        {
            kartice.Add(kartica);
            kartica.Parent = this;
        }

        public void Ocisti()
        {
            foreach (Control k in kartice)
                k.Dispose();
            kartice.Clear();
            AutoScrollPosition = Point.Empty;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Rasporedi();
        }

        public void Rasporedi()
        {
            if (radi || kartice.Count == 0 || ClientSize.Width <= 0) return;
            radi = true;
            SuspendLayout();
            int sirinaNaPocetku = ClientSize.Width;
            int dostupno = sirinaNaPocetku - 2 * Razmak;
            int kolona = Math.Min(MaxKolona, dostupno >= Theme.Dp(1020) ? 3 : dostupno >= Theme.Dp(680) ? 2 : 1);
            int sirina = (dostupno - (kolona - 1) * Razmak) / kolona;
            var visine = new int[kolona];
            int pomak = AutoScrollPosition.Y;

            for (int i = 0; i < kartice.Count; i++)
            {
                // Svaka kartica ide u trenutno najkraću kolonu, da prostor ostane ravnomeran.
                int k = Array.IndexOf(visine, visine.Min());
                int visina = ((IKartica)kartice[i]).Rasporedi(sirina);
                kartice[i].SetBounds(Razmak + k * (sirina + Razmak), Razmak + visine[k] + pomak, sirina, visina);
                visine[k] += visina + Razmak;
            }
            int najvise = visine.Max();
            AutoScrollMinSize = new Size(0, Razmak + najvise);
            ResumeLayout(true);
            radi = false;
            // Pojava vertikalnog skrola sužava površinu, pa kartice moraju ponovo da stanu po širini.
            if (ClientSize.Width != sirinaNaPocetku) Rasporedi();

            int potrebno = Razmak + najvise;
            if (potrebno != PotrebnaVisina)
            {
                PotrebnaVisina = potrebno;
                VisinaPromenjena?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
