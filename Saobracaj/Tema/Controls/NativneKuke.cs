using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Saobracaj.Tema.Controls
{
    internal static class Win32
    {
        public const int WM_PAINT = 0x000F;
        public const int WM_ERASEBKGND = 0x0014;
        public const int CB_SETITEMHEIGHT = 0x0153;
        public const int WM_NCLBUTTONDOWN = 0x00A1;
        public const int HTCAPTION = 2;

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool ValidateRect(IntPtr hWnd, IntPtr lpRect);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
    }

    // Kuka na postojeći ComboBox: isti tip kontrole, ali visina i strelica po novom izgledu.
    public class KomboKuka : NativeWindow
    {
        private readonly ComboBox kombo;
        private readonly Color strelica;
        private int visinaStavke = -1;

        public KomboKuka(ComboBox kombo, Color pozadina, Color? strelica = null)
        {
            this.kombo = kombo;
            this.strelica = strelica ?? Theme.TekstSporedni;
            kombo.HandleCreated += (s, e) => { Zakaci(); Odznaci(); };
            kombo.HandleDestroyed += (s, e) => ReleaseHandle();
            // Editabilni ComboBox sam označi tekst pri promeni veličine ili vrednosti.
            kombo.Resize += (s, e) => Odznaci();
            kombo.SelectedIndexChanged += (s, e) => Odznaci();
            kombo.TextChanged += (s, e) => Odznaci();
            if (kombo.IsHandleCreated) Zakaci();
        }

        private void Odznaci()
        {
            if (!kombo.IsHandleCreated || kombo.ContainsFocus || kombo.DropDownStyle == ComboBoxStyle.DropDownList) return;
            kombo.BeginInvoke((Action)(() =>
            {
                if (!kombo.IsDisposed && !kombo.ContainsFocus) kombo.SelectionLength = 0;
            }));
        }

        public void PostaviVisinu(int visina)
        {
            visinaStavke = visina;
            if (Handle != IntPtr.Zero)
                Win32.SendMessage(Handle, Win32.CB_SETITEMHEIGHT, new IntPtr(-1), new IntPtr(visina));
        }

        private void Zakaci()
        {
            AssignHandle(kombo.Handle);
            if (visinaStavke > 0)
                Win32.SendMessage(Handle, Win32.CB_SETITEMHEIGHT, new IntPtr(-1), new IntPtr(visinaStavke));
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg != Win32.WM_PAINT) return;

            int sirina = SystemInformation.VerticalScrollBarWidth + 3;
            var r = new Rectangle(kombo.ClientSize.Width - sirina, 1, sirina - 1, kombo.ClientSize.Height - 2);
            using (var g = Graphics.FromHwnd(Handle))
            using (var pozadina = new SolidBrush(kombo.BackColor))
            {
                g.FillRectangle(pozadina, r);
                float d = Theme.Dp(14);
                Ikone.Crtaj(g, Ikona.Dole, new RectangleF(r.X + (r.Width - d) / 2, r.Y + (r.Height - d) / 2, d, d), strelica, 2.2f);
            }
        }
    }

    // Kuka na postojeći TabControl: zaglavlje crta novi izgled, a izbor taba ostaje sistemski.
    public class TabKuka : NativeWindow
    {
        private readonly TabControl tabovi;
        private int pod = -1;

        public TabKuka(TabControl tabovi)
        {
            this.tabovi = tabovi;
            tabovi.HandleCreated += (s, e) => AssignHandle(tabovi.Handle);
            tabovi.HandleDestroyed += (s, e) => ReleaseHandle();
            tabovi.MouseMove += (s, e) => PostaviPod(TabNa(e.Location));
            tabovi.MouseLeave += (s, e) => PostaviPod(-1);
            // Cursor se ovde ne menja: nasleđuje se na decu, a MetroTextBox na strani taba tada ruši proces (StackOverflow).
            if (tabovi.IsHandleCreated) AssignHandle(tabovi.Handle);
        }

        private int TabNa(Point tacka)
        {
            for (int i = 0; i < tabovi.TabCount; i++)
                if (tabovi.GetTabRect(i).Contains(tacka)) return i;
            return -1;
        }

        private void PostaviPod(int i)
        {
            if (i == pod) return;
            pod = i;
            tabovi.Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Win32.WM_ERASEBKGND)
            {
                m.Result = (IntPtr)1;
                return;
            }
            if (m.Msg == Win32.WM_PAINT && m.WParam == IntPtr.Zero)
            {
                Crtaj();
                Win32.ValidateRect(Handle, IntPtr.Zero);
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
        }

        private void Crtaj()
        {
            Rectangle strana = tabovi.DisplayRectangle;
            using (var g = Graphics.FromHwnd(Handle))
            {
                g.ExcludeClip(strana);
                g.Clear(Color.White);
                int dno = tabovi.TabCount > 0 ? tabovi.GetTabRect(0).Bottom : strana.Top;
                using (var linija = new Pen(Theme.Linija))
                    g.DrawLine(linija, 0, dno, tabovi.Width, dno);

                for (int i = 0; i < tabovi.TabCount; i++)
                {
                    Rectangle r = tabovi.GetTabRect(i);
                    bool izabran = i == tabovi.SelectedIndex;
                    TextRenderer.DrawText(g, tabovi.TabPages[i].Text, tabovi.Font, r,
                        izabran || i == pod ? Theme.Primarna : Theme.TekstSporedni,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                    if (izabran)
                        using (var traka = new SolidBrush(Theme.Primarna))
                            g.FillRectangle(traka, r.X, dno - Theme.Dp(3) + 1, r.Width, Theme.Dp(3));
                }
            }
        }
    }
}
