using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Saobracaj.Agent.TelegramiImport.Pdf
{
    /// <summary>
    /// Tekst PDF-a po redovima (gore → dole, levo → desno). Telegrami IŽS-a su Word PDF-ovi sa tekstom;
    /// skenirani PDF nema reči i vraća prazan string.
    /// </summary>
    public static class PdfTekst
    {
        public static string Izvuci(byte[] pdf)
        {
            using (var doc = PdfDocument.Open(pdf))
            {
                var sb = new StringBuilder();
                foreach (var page in doc.GetPages())
                {
                    foreach (var red in Redovi(page))
                        sb.Append(red).Append('\n');
                    sb.Append('\n');
                }
                return sb.ToString();
            }
        }

        private static IEnumerable<string> Redovi(Page page)
        {
            var reci = page.GetWords().Where(w => !string.IsNullOrWhiteSpace(w.Text)).ToList();
            if (reci.Count == 0) yield break;

            // Grupisanje po osnovnoj liniji; tolerancija = 40% visine slova
            var redovi = new List<List<Word>>();
            foreach (var w in reci.OrderByDescending(w => w.BoundingBox.Bottom).ThenBy(w => w.BoundingBox.Left))
            {
                var h = Math.Max(2.0, w.BoundingBox.Height);
                var red = redovi.FirstOrDefault(r => Math.Abs(r[0].BoundingBox.Bottom - w.BoundingBox.Bottom) <= h * 0.4);
                if (red == null) redovi.Add(new List<Word> { w });
                else red.Add(w);
            }

            double levaIvica = reci.Min(w => w.BoundingBox.Left);
            foreach (var red in redovi.OrderByDescending(r => r.Average(w => w.BoundingBox.Bottom)))
            {
                var sorted = red.OrderBy(w => w.BoundingBox.Left).ToList();
                var sb = new StringBuilder();
                // uvlačenje (za prepoznavanje tačaka nije potrebno, ali pomaže čitljivosti loga)
                int uvuceno = (int)Math.Round((sorted[0].BoundingBox.Left - levaIvica) / 6.0);
                if (uvuceno > 0) sb.Append(' ', Math.Min(uvuceno, 40));
                for (int i = 0; i < sorted.Count; i++)
                {
                    if (i > 0)
                    {
                        var razmak = sorted[i].BoundingBox.Left - sorted[i - 1].BoundingBox.Right;
                        if (razmak > 0.15 * Math.Max(sorted[i].BoundingBox.Height, 4)) sb.Append(' ');
                    }
                    sb.Append(sorted[i].Text);
                }
                yield return sb.ToString();
            }
        }
    }
}
