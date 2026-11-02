using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Saobracaj.Tema
{
    // Boje, fontovi i trajanja novog izgleda na jednom mestu (Plan_Razvoja_Novi_Izgled.md, poglavlja 3 i 7).
    public static class Theme
    {
        public static readonly Color Primarna = ColorTranslator.FromHtml("#791A28");
        public static readonly Color Akcent = ColorTranslator.FromHtml("#9B2030");
        public static readonly Color Tamna = ColorTranslator.FromHtml("#51111C");
        public static readonly Color Najtamnija = ColorTranslator.FromHtml("#350B12");
        public static readonly Color Sjaj = ColorTranslator.FromHtml("#A61D2D");
        public static readonly Color Pozadina = ColorTranslator.FromHtml("#EFEDE9");
        public static readonly Color Pozadina2 = ColorTranslator.FromHtml("#E6E2DD");
        public static readonly Color Tekst = ColorTranslator.FromHtml("#35282C");
        public static readonly Color TekstSporedni = ColorTranslator.FromHtml("#6D6163");
        public static readonly Color Linija = ColorTranslator.FromHtml("#D2C9C6");
        public static readonly Color LinijaHover = ColorTranslator.FromHtml("#B7ABA8");
        public static readonly Color SvetloNaBordo = ColorTranslator.FromHtml("#F5E8E7");
        public static readonly Color RozeNaBordo = ColorTranslator.FromHtml("#FFDCE1");
        public static readonly Color ObaveznoPodloga = ColorTranslator.FromHtml("#FFF5F6");
        public static readonly Color ObaveznoIvica = ColorTranslator.FromHtml("#E4BDC3");
        public static readonly Color ZaglavljeTabele = ColorTranslator.FromHtml("#FAF7F7");

        public const int TrajanjeHover = 200;
        public const int TrajanjeForme = 500;

        private static readonly PrivateFontCollection kolekcija = new PrivateFontCollection();
        private static readonly Dictionary<string, Font> keš = new Dictionary<string, Font>();
        private static readonly float skala;
        private static FontFamily dmSans, manrope;

        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, ref uint pcFonts);

        static Theme()
        {
            using (var g = Graphics.FromHwnd(IntPtr.Zero))
                skala = g.DpiX / 96f;
            UcitajFontove();
        }

        private static void UcitajFontove()
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var ime in new[] { "DMSans-Regular.ttf", "DMSans-Bold.ttf", "Manrope-ExtraBold.ttf" })
            {
                using (Stream s = asm.GetManifestResourceStream("Saobracaj.Tema.Fontovi." + ime))
                {
                    if (s == null) continue;
                    var bajtovi = new byte[s.Length];
                    s.Read(bajtovi, 0, bajtovi.Length);
                    // Memorija ostaje zauzeta do kraja rada aplikacije, kolekcija je koristi.
                    IntPtr mem = Marshal.AllocCoTaskMem(bajtovi.Length);
                    Marshal.Copy(bajtovi, 0, mem, bajtovi.Length);
                    kolekcija.AddMemoryFont(mem, bajtovi.Length);
                    // Standardne kontrole crtaju preko GDI-ja, koji ne vidi PrivateFontCollection.
                    uint broj = 0;
                    AddFontMemResourceEx(mem, (uint)bajtovi.Length, IntPtr.Zero, ref broj);
                }
            }
            dmSans = kolekcija.Families.FirstOrDefault(f => f.Name == "DM Sans");
            manrope = kolekcija.Families.FirstOrDefault(f => f.Name.StartsWith("Manrope"));
        }

        private static Font Uzmi(FontFamily porodica, float velicina, FontStyle stil)
        {
            string kljuc = (porodica?.Name ?? "") + "|" + velicina + "|" + stil;
            Font f;
            if (!keš.TryGetValue(kljuc, out f))
            {
                f = porodica != null
                    ? new Font(porodica, velicina, stil, GraphicsUnit.Point)
                    : new Font("Segoe UI", velicina, stil, GraphicsUnit.Point);
                keš[kljuc] = f;
            }
            return f;
        }

        public static Font Tekstualni(float velicina = 9.5f, bool bold = false)
        {
            return Uzmi(dmSans, velicina, bold ? FontStyle.Bold : FontStyle.Regular);
        }

        public static Font Sistemski(float velicina = 9f)
        {
            return Uzmi(null, velicina, FontStyle.Regular);
        }

        public static Font Naslovni(float velicina)
        {
            return Uzmi(manrope, velicina, manrope != null ? FontStyle.Regular : FontStyle.Bold);
        }

        public static int Dp(int vrednost)
        {
            return (int)Math.Round(vrednost * skala);
        }

        // Prekidač "Animacije": <add key="Animacije" value="false" /> u appSettings.
        public static bool AnimacijeUkljucene
        {
            get
            {
                string podesavanje = ConfigurationManager.AppSettings["Animacije"];
                if (string.Equals(podesavanje, "false", StringComparison.OrdinalIgnoreCase)) return false;
                return SystemInformation.UIEffectsEnabled && !SystemInformation.TerminalServerSession;
            }
        }

        public static Color Mesaj(Color podloga, Color boja, double udeo)
        {
            return Color.FromArgb(
                (int)(podloga.R + (boja.R - podloga.R) * udeo),
                (int)(podloga.G + (boja.G - podloga.G) * udeo),
                (int)(podloga.B + (boja.B - podloga.B) * udeo));
        }
    }
}
