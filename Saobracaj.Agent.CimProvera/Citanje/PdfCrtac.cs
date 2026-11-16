using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Saobracaj.Agent.CimProvera.Citanje
{
    /// <summary>
    /// Iscrtavanje strane PDF-a u sliku (PDFium preko PdfiumViewer, Apache 2.0, lokalno). Koristi se kada je tekst iz PDF-a
    /// neupotrebljiv (pokvareno kodiranje fonta, npr. "C/M Consignment Eote", cifre "ϮϬ"), pa se strana čita OCR-om.
    /// </summary>
    internal static class PdfCrtac
    {
        // v2.8.4 (2): orijentacija strane (u smeru kazaljke) utvrđena pri OCR-u – svako sledeće iscrtavanje iste strane (raster okvira,
        // kućice, ponovni OCR rubrike, PDF primer) je okrenuto isto, da se koordinate reči i slika poklapaju. Ključ: sadržaj fajla + strana.
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> uglovi = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();

        /// <summary>Kratak ključ sadržaja (dužina + heš početka i kraja) – isti fajl u više mailova/kopija niza daje isti ključ.</summary>
        internal static string KljucSadrzaja(byte[] b)
        {
            if (b == null) return "";
            ulong h = 1469598103934665603UL;
            void Dodaj(int od, int do_) { for (int i = Math.Max(0, od); i < Math.Min(b.Length, do_); i++) { h ^= b[i]; h *= 1099511628211UL; } }
            Dodaj(0, 16384); Dodaj(b.Length - 16384, b.Length);
            return b.Length + ":" + h.ToString("x16");
        }

        public static void ZapamtiUgao(byte[] sadrzaj, int brojStrane, int cw)
        {
            var k = KljucSadrzaja(sadrzaj) + "#" + brojStrane;
            if (cw % 360 == 0) uglovi.TryRemove(k, out _); else uglovi[k] = ((cw % 360) + 360) % 360;
        }

        public static int Ugao(byte[] sadrzaj, int brojStrane) => uglovi.TryGetValue(KljucSadrzaja(sadrzaj) + "#" + brojStrane, out var u) ? u : 0;

        public static byte[] StranaUPng(byte[] pdf, int brojStrane, int dpi = 300)
        {
            var cw = Ugao(pdf, brojStrane);
            using (var ms = new MemoryStream(pdf))
            using (var doc = PdfiumViewer.PdfDocument.Load(ms))
            {
                var i = brojStrane - 1;
                var vel = doc.PageSizes[i];                       // u tačkama (1/72 inča)
                int w = (int)Math.Round(vel.Width / 72.0 * dpi), h = (int)Math.Round(vel.Height / 72.0 * dpi);
                // w/h su već u pikselima (bez CorrectFromDpi, koji bi ih još jednom pomnožio sa dpi/72)
                using (var img = doc.Render(i, w, h, dpi, dpi, PdfiumViewer.PdfRenderFlags.Annotations))
                using (var izlaz = new MemoryStream())
                {
                    Okreni(img, cw);
                    img.Save(izlaz, ImageFormat.Png);
                    return izlaz.ToArray();
                }
            }
        }

        /// <summary>Slika (fotografija/sken kao prilog) okrenuta kao pri OCR-u; bez zapamćenog ugla – isti bajtovi.</summary>
        public static byte[] SlikaOkrenuta(byte[] slika)
        {
            var cw = Ugao(slika, 1);
            if (cw == 0) return slika;
            using (var ms = new MemoryStream(slika))
            using (var img = System.Drawing.Image.FromStream(ms))
            using (var izlaz = new MemoryStream())
            {
                Okreni(img, cw);
                img.Save(izlaz, ImageFormat.Png);
                return izlaz.ToArray();
            }
        }

        private static void Okreni(System.Drawing.Image img, int cw)
        {
            if (cw == 90) img.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone);
            else if (cw == 180) img.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipNone);
            else if (cw == 270) img.RotateFlip(System.Drawing.RotateFlipType.Rotate270FlipNone);
        }

        /// <summary>
        /// Tekst iz PDF-a je pokvaren kada font nema ispravnu mapu znakova: slova i cifre dolaze kao znakovi iz grčkog/koptskog
        /// i proširenog latiničnog opsega (Ϯ, ϱ, ǁ, Ŭ, ď, ͘…). Prag: bar 2 % takvih znakova među slovima i ciframa.
        /// </summary>
        public static bool TekstPokvaren(System.Collections.Generic.IEnumerable<Rec> reci)
        {
            int ukupno = 0, sumnjivo = 0;
            foreach (var r in reci)
                foreach (var c in r.T)
                {
                    if (!char.IsLetterOrDigit(c) && c != '͘' && c != 'Ͳ') continue;
                    ukupno++;
                    if ((c >= 'Ͱ' && c <= 'Ͽ') || (c >= 'ǀ' && c <= 'ɏ') || (c >= '̀' && c <= 'ͯ')
                        || (c >= 'ĸ' && c <= 'ſ' && c != 'ő' && c != 'ű' && c != 'ł' && c != 'ń' && c != 'ś' && c != 'ź' && c != 'ż' && c != 'ř' && c != 'š' && c != 'ž' && c != 'ť' && c != 'ň' && c != 'ľ' && c != 'ĺ' && c != 'ŕ'))
                        sumnjivo++;
                }
            return ukupno >= 50 && sumnjivo >= ukupno * 0.02;
        }
    }
}
