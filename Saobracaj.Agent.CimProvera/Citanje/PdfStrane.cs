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

        /// <summary>
        /// Vrednosti polja PDF obrasca (AcroForm) na strani kao reči na položaju polja: tekst po redovima od vrha polja,
        /// označeno polje za krstić kao „X“. Font „AcroForm“ (veličina ≥ 7) – podatak, nikad natpis rubrike.
        /// </summary>
        internal static List<Rec> PoljaObrasca(PdfDocument doc, int strana, double w, double h)
        {
            var rez = new List<Rec>();
            UglyToad.PdfPig.AcroForms.AcroForm form;
            try { if (!doc.TryGetForm(out form) || form == null) return rez; } catch { return rez; }
            IEnumerable<UglyToad.PdfPig.AcroForms.Fields.AcroFieldBase> Svi(IEnumerable<UglyToad.PdfPig.AcroForms.Fields.AcroFieldBase> polja)
            {
                foreach (var f in polja)
                    if (f is UglyToad.PdfPig.AcroForms.Fields.AcroNonTerminalField nt) { foreach (var x in Svi(nt.Children)) yield return x; }
                    else yield return f;
            }
            // vrednosti polja po imenu (polje bez strane/položaja: položaj daje widget anotacija na strani)
            var vrednosti = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var f in Svi(form.Fields))
            {
                var ime = f.Information?.PartialName;
                if (string.IsNullOrEmpty(ime) || vrednosti.ContainsKey(ime)) continue;
                if (f is UglyToad.PdfPig.AcroForms.Fields.AcroTextField tf) vrednosti[ime] = tf.Value ?? "";
                else if (f is UglyToad.PdfPig.AcroForms.Fields.AcroCheckboxField cb) vrednosti[ime] = cb.IsChecked ? "\u0001X" : "";
                else if (f is UglyToad.PdfPig.AcroForms.Fields.AcroComboBoxField cf) vrednosti[ime] = string.Join(" ", cf.SelectedOptions?.Select(o => o.ToString()) ?? new string[0]);
            }
            string Tekst(UglyToad.PdfPig.Tokens.DictionaryToken d, string kljuc) =>
                d != null && d.TryGet(UglyToad.PdfPig.Tokens.NameToken.Create(kljuc), out var t) ? (t as UglyToad.PdfPig.Tokens.StringToken)?.Data ?? (t as UglyToad.PdfPig.Tokens.HexToken)?.Data ?? (t as UglyToad.PdfPig.Tokens.NameToken)?.Data : null;
            UglyToad.PdfPig.Tokens.DictionaryToken Roditelj(UglyToad.PdfPig.Tokens.DictionaryToken d)
            {
                if (d == null || !d.TryGet(UglyToad.PdfPig.Tokens.NameToken.Parent, out var p)) return null;
                if (p is UglyToad.PdfPig.Tokens.DictionaryToken pd) return pd;
                if (p is UglyToad.PdfPig.Tokens.IndirectReferenceToken r) try { return doc.Structure.GetObject(r.Data)?.Data as UglyToad.PdfPig.Tokens.DictionaryToken; } catch { }
                return null;
            }
            var stranaPdf = doc.GetPage(strana);
            IEnumerable<UglyToad.PdfPig.Annotations.Annotation> anotacije;
            try { anotacije = stranaPdf.ExperimentalAccess.GetAnnotations().Where(a => a.Type == UglyToad.PdfPig.Annotations.AnnotationType.Widget).ToList(); } catch { return rez; }
            foreach (var an in anotacije)
            {
                var d = an.AnnotationDictionary;
                var ime = Tekst(d, "T") ?? Tekst(Roditelj(d), "T");
                if (ime == null || !vrednosti.TryGetValue(ime, out var vrednost)) continue;
                var b = an.Rectangle;
                double x0 = Math.Min(b.Left, b.Right), x1 = Math.Max(b.Left, b.Right), gore = Math.Max(b.Top, b.Bottom), dole = Math.Min(b.Top, b.Bottom);
                if (vrednost.StartsWith("\u0001") || vrednost.Length == 0 && Tekst(d, "AS") is string st && st != "Off")
                {
                    // polje za krstić: stanje widgeta (/AS) ili vrednost polja
                    var stanje = Tekst(d, "AS");
                    if (stanje != null && stanje != "Off" || stanje == null && vrednost.StartsWith("\u0001"))
                        rez.Add(new Rec { T = "X", X0 = x0 / w, X1 = x1 / w, Y0 = 1 - gore / h, Y1 = 1 - dole / h, Pouzdanost = 0.99, Velicina = 8, Font = "AcroForm" });
                    continue;
                }
                if (string.IsNullOrWhiteSpace(vrednost)) continue;
                var redovi = vrednost.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
                var visinaReda = Math.Min((gore - dole) / Math.Max(1, redovi.Length), 11.0);
                var vel = Math.Max(7, visinaReda * 0.8);
                for (int i = 0; i < redovi.Length; i++)
                {
                    double y1 = gore - i * visinaReda, y0 = y1 - visinaReda, x = x0 + 2;
                    var sirinaZnaka = Math.Min(vel * 0.55, (x1 - x0 - 2) / Math.Max(1, redovi[i].Length));
                    foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(redovi[i], @"\S+"))
                    {
                        double a = x0 + 2 + m.Index * sirinaZnaka, z = a + m.Length * sirinaZnaka;
                        rez.Add(new Rec { T = m.Value, X0 = a / w, X1 = Math.Min(z, x1) / w, Y0 = 1 - y1 / h, Y1 = 1 - y0 / h, Pouzdanost = 0.99, Velicina = vel, Font = "AcroForm" });
                    }
                }
            }
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
                        // uspravno odštampani natpisi obrasca (npr. uz r19) se preskaču – osim oznake „© 200x CIT“ (sidro univerzalnog šablona)
                        if (wd.TextOrientation != TextOrientation.Horizontal && !System.Text.RegularExpressions.Regex.IsMatch(t, @"CIT")) continue;
                        var font = wd.Letters.Count > 0 ? (wd.Letters[0].FontName ?? "") : "";
                        var plus = font.IndexOf('+');
                        if (plus >= 0 && plus < font.Length - 1) font = font.Substring(plus + 1); // podskup fonta "AAAAAD+Arial" → "Arial"
                        t = BezDvostrukihSlova(t, wd.Letters);
                        s.Reci.Add(new Rec { T = t, X0 = b.Left / w, X1 = b.Right / w, Y0 = 1 - b.Top / h, Y1 = 1 - b.Bottom / h, Pouzdanost = 0.98, Velicina = vel, Font = font });
                    }
                    // Popunjen PDF obrazac (AcroForm): podaci su vrednosti polja, ne tekst strane (npr. CIT obrazac popunjen u Acrobatu).
                    s.Reci.AddRange(PoljaObrasca(doc, page.Number, w, h));
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
