using System;
using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Saobracaj.Agent.CimProvera.Citanje
{
    /// <summary>
    /// Strane PDF-a: tekst iz PDF-a (PdfPig, kao TelegramiImport) i, kada je strana sken (velika slika preko strane),
    /// OCR te slike. Ako strana ima i tekst i sken (npr. odštampani nazivi rubrika kao tekst, a podaci kao slika),
    /// koriste se obe vrste reči.
    /// </summary>
    internal static class PdfStrane
    {
        public static List<Strana> Procitaj(byte[] pdf, Ocr ocr, int maxStrana = 60, bool uvekOcr = false, Func<IEnumerable<Rec>, int> brojSidara = null, Func<List<Rec>, bool> trebaSlika = null)
        {
            var rez = ProcitajTekst(pdf, ocr, maxStrana, uvekOcr, brojSidara);
            // Slika strane (linije okvira, kućice, prazne rubrike) samo za strane koje liče na tovarni list.
            foreach (var s in rez)
                if (trebaSlika != null && trebaSlika(s.Reci))
                    try { s.PostaviRaster(Raster.IzSlike(PdfCrtac.StranaUPng(pdf, s.Broj, 150))); }
                    catch (Exception ex) { s.Napomena += (s.Napomena.Length > 0 ? "; " : "") + "slika strane nije iscrtana (" + ex.Message + ")"; }
            return rez;
        }

        private static List<Strana> ProcitajTekst(byte[] pdf, Ocr ocr, int maxStrana, bool uvekOcr, Func<IEnumerable<Rec>, int> brojSidara)
        {
            var rez = new List<Strana>();
            using (var doc = PdfDocument.Open(pdf))
            {
                string BezDvostrukihSlova(string t, IReadOnlyList<UglyToad.PdfPig.Content.Letter> slova)
                {
                    // „Podebljano“ ispisom istog slova dvaput na (skoro) istom mestu: „KKaazziinn…“ → „Kazin…“
                    if (slova.Count != t.Length || t.Length < 6 || t.Length % 2 != 0) return t;
                    for (int i = 0; i < t.Length; i += 2)
                        if (t[i] != t[i + 1] || Math.Abs(slova[i].GlyphRectangle.Left - slova[i + 1].GlyphRectangle.Left) > slova[i].GlyphRectangle.Width * 0.5 + 0.5) return t;
                    return new string(t.Where((c, i) => i % 2 == 0).ToArray());
                }
                foreach (var page in doc.GetPages().Take(maxStrana))
                {
                    var s = new Strana { Broj = page.Number };
                    double w = page.Width, h = page.Height;
                    foreach (var wd in page.GetWords())
                    {
                        var t = wd.Text?.Trim();
                        if (string.IsNullOrEmpty(t)) continue;
                        var b = wd.BoundingBox;
                        var vel = wd.Letters.Count > 0 ? wd.Letters.Average(l => l.PointSize) : 0;
                        if (wd.Letters.Any(l => (l.FontName ?? "").IndexOf("Wingdings", StringComparison.OrdinalIgnoreCase) >= 0)) continue; // oznake polja
                        if (wd.TextOrientation != TextOrientation.Horizontal) continue; // uspravno odštampani natpisi obrasca (npr. uz r19)
                        var font = wd.Letters.Count > 0 ? (wd.Letters[0].FontName ?? "") : "";
                        var plus = font.IndexOf('+');
                        if (plus >= 0 && plus < font.Length - 1) font = font.Substring(plus + 1); // podskup fonta "AAAAAD+Arial" → "Arial"
                        t = BezDvostrukihSlova(t, wd.Letters);
                        s.Reci.Add(new Rec { T = t, X0 = b.Left / w, X1 = b.Right / w, Y0 = 1 - b.Top / h, Y1 = 1 - b.Bottom / h, Pouzdanost = 0.98, Velicina = vel, Font = font });
                    }
                    s.Izvor = s.Reci.Count > 0 ? IzvorTeksta.Tekst : IzvorTeksta.Nema;

                    // Pokvaren tekst (font bez ispravne mape znakova): strana se iscrtava i čita OCR-om.
                    if (ocr != null && (uvekOcr || PdfCrtac.TekstPokvaren(s.Reci)))
                    {
                        try
                        {
                            var png = PdfCrtac.StranaUPng(pdf, page.Number);
                            var ocrReci = ocr.Prepoznaj(png, out var nap0, 300);
                            if (ocrReci.Count > 0)
                            {
                                s.Reci = ocrReci;
                                s.Izvor = IzvorTeksta.Ocr;
                                s.Napomena = "tekst PDF-a je pokvaren (kodiranje fonta) – strana pročitana OCR-om" + (nap0.Length > 0 ? ", " + nap0 : "");
                                rez.Add(s);
                                continue;
                            }
                        }
                        catch (Exception ex) { s.Napomena = "tekst PDF-a je pokvaren, a iscrtavanje strane nije uspelo (" + ex.Message + ")"; }
                    }

                    var slika = VelikaSlika(page, w, h);
                    if (slika != null && ocr != null)
                    {
                        List<Rec> ocrReci = null;
                        string nap = "";
                        // 1) slika skena direktno (JPEG/PNG)
                        if (SlikaBajtovi(slika, out var bajtovi))
                        {
                            var dpi = (int)Math.Round(slika.WidthInSamples / Math.Max(slika.Bounds.Width / 72.0, 0.1));
                            try
                            {
                                ocrReci = ocr.Prepoznaj(bajtovi, out nap, dpi);
                                var bb = slika.Bounds;
                                // Koordinate OCR-a su u odnosu na sliku → u odnosu na stranu.
                                foreach (var r in ocrReci)
                                {
                                    r.X0 = (bb.Left + r.X0 * bb.Width) / w; r.X1 = (bb.Left + r.X1 * bb.Width) / w;
                                    r.Y0 = 1 - (bb.Top - r.Y0 * bb.Height) / h; r.Y1 = 1 - (bb.Top - r.Y1 * bb.Height) / h;
                                }
                            }
                            catch (Exception ex) { ocrReci = null; nap = "sken nije pročitan direktno (" + ex.Message + ")"; }
                        }
                        // 2) format slike koji Tesseract ne učitava (JBIG2, CCITT, JPEG2000…): strana se iscrtava PDFium-om
                        if (ocrReci == null || ocrReci.Count == 0)
                        {
                            try
                            {
                                ocrReci = ocr.Prepoznaj(PdfCrtac.StranaUPng(pdf, page.Number), out var nap2, 300);
                                nap = "sken pročitan posle iscrtavanja strane" + (nap2.Length > 0 ? ", " + nap2 : "");
                            }
                            catch (Exception ex) { ocrReci = new List<Rec>(); nap = (nap.Length > 0 ? nap + "; " : "") + "iscrtavanje skena nije uspelo (" + ex.Message + ")"; }
                        }
                        s.Napomena = nap;
                        if (ocrReci.Count > 0)
                        {
                            // Sken sa "nevidljivim" tekstom koji je napravio skener (često loš): ako naš OCR prepozna više naziva
                            // rubrika CIM obrasca, koristi se samo naš OCR; inače se tekst iz PDF-a dopunjuje OCR rečima.
                            if (s.Reci.Count > 0 && brojSidara != null && brojSidara(ocrReci) > brojSidara(s.Reci))
                            {
                                s.Reci = ocrReci;
                                s.Napomena += (s.Napomena.Length > 0 ? "; " : "") + "tekst skenera zamenjen OCR-om";
                            }
                            else
                                foreach (var r in ocrReci)
                                    if (!s.Reci.Any(x => Math.Abs(x.Cx - r.Cx) < 0.01 && Math.Abs(x.Cy - r.Cy) < 0.006))
                                        s.Reci.Add(r);
                            s.Izvor = IzvorTeksta.Ocr;
                        }
                    }
                    rez.Add(s);
                }
            }
            return rez;
        }

        /// <summary>Slika koja pokriva bar polovinu strane (sken); null ako je nema.</summary>
        private static IPdfImage VelikaSlika(Page page, double w, double h)
        {
            try
            {
                return page.GetImages()
                    .Where(i => i.Bounds.Width * i.Bounds.Height >= 0.5 * w * h && i.WidthInSamples >= 600)
                    .OrderByDescending(i => i.Bounds.Width * i.Bounds.Height).FirstOrDefault();
            }
            catch { return null; }
        }

        private static bool SlikaBajtovi(IPdfImage img, out byte[] bajtovi)
        {
            bajtovi = null;
            try
            {
                var filter = img.ImageDictionary.Data.TryGetValue("Filter", out var f) ? f.ToString() : "";
                if (filter.Contains("DCTDecode")) { bajtovi = img.RawBytes.ToArray(); return true; }   // JPEG
                if (img.TryGetPng(out var png)) { bajtovi = png; return true; }
            }
            catch { }
            return false;
        }
    }
}
