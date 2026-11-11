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
        public static byte[] StranaUPng(byte[] pdf, int brojStrane, int dpi = 300)
        {
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
                    img.Save(izlaz, ImageFormat.Png);
                    return izlaz.ToArray();
                }
            }
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
