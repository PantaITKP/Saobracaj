using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tesseract;

namespace Saobracaj.Agent.CimProvera.Citanje
{
    /// <summary>
    /// Lokalni OCR (Tesseract, besplatan, open source – Apache 2.0). Radi samo na serveru, bez interneta i bez ključeva.
    /// Jezici: tessdata\*.traineddata (eng, deu, fra, srp, srp_latn, ces, tur…). Orijentacija strane se prepoznaje (osd).
    /// </summary>
    internal sealed class Ocr : IDisposable
    {
        private readonly string tessdata;
        private readonly string jezici;
        private readonly List<string> dodatni;
        private readonly double minPouzdanostReci;
        private TesseractEngine engine;
        private readonly Dictionary<string, TesseractEngine> sDodatnim = new Dictionary<string, TesseractEngine>();
        private TesseractEngine osd;
        private readonly object sync = new object();

        public Ocr(string tessdata, string jezici, double minPouzdanostReci = 0.3, string dodatniJezici = "")
        {
            this.tessdata = tessdata; this.jezici = jezici; this.minPouzdanostReci = minPouzdanostReci;
            dodatni = (dodatniJezici ?? "").Split(new[] { '+', ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim())
                .Where(x => File.Exists(Path.Combine(tessdata, x + ".traineddata"))).ToList();
        }

        /// <summary>
        /// Jezici koji se dodaju u drugom čitanju strane kada tekst prvog čitanja pokazuje obrazac te zemlje (svi jezici
        /// odjednom kvare OCR: npr. "àîàîhnà…" umesto "Povelyanovo").
        /// </summary>
        private static readonly Dictionary<string, string[]> NagovestajJezika = new Dictionary<string, string[]>
        {
            // samo natpisi obrasca (ne imena zemalja iz adresa: "Italia" u adresi primaoca ne znači italijanski obrazac)
            ["hun"] = new[] { "fuvarlevel", "felado", "cimzett", "rendeltetesi" },
            ["ita"] = new[] { "lettera di vettura", "mittente", "destinatario", "stazione di destinazione" },
            ["ron"] = new[] { "scrisoare de trasura", "expeditor", "destinatar" },
            ["bul"] = new[] { "tovaritelnica", "izprastac", "izpraschach", "poluchatel" },
            ["slv"] = new[] { "tovorni list", "prejemnik", "postaja" },
            ["hrv"] = new[] { "kolodvor", "primatelj", "posiljatelj", "teretni list", "prijevozni" },
        };

        /// <summary>Reč-smeće OCR-a (npr. "àîàîààî", "jjjj"): većina znakova su akcentovana slova bez smisla ili isto slovo ponovljeno.</summary>
        internal static bool Smece(string t)
        {
            if (string.IsNullOrEmpty(t) || t.Length < 3) return false;
            int akc = t.Count(c => "àáâãäåèéêëìíîïòóôõöùúûüýÿ".IndexOf(char.ToLowerInvariant(c)) >= 0);
            return akc >= t.Length * 0.4 || System.Text.RegularExpressions.Regex.IsMatch(t, @"(.)\1{3,}");
        }

        /// <summary>Ocena čitanja: zbir pouzdanosti reči koje nisu smeće (smeće se računa negativno).</summary>
        private static double Ocena(List<Rec> r) => r.Sum(w => Smece(w.T) ? -w.Pouzdanost : w.Pouzdanost);

        private List<string> DodatniZa(List<Rec> reci)
        {
            if (dodatni.Count == 0 || reci.Count == 0) return new List<string>();
            var t = " " + Saobracaj.Common.Text.TekstUtil.Kljuc(string.Join(" ", reci.Select(r => r.T))) + " ";
            return dodatni.Where(j => NagovestajJezika.TryGetValue(j, out var k) && k.Any(x => t.Contains(" " + x))).ToList();
        }

        public static bool Dostupan(string tessdata, string jezici, out string razlog)
        {
            razlog = null;
            if (!Directory.Exists(tessdata)) { razlog = "nema foldera " + tessdata; return false; }
            foreach (var j in jezici.Split('+'))
                if (!File.Exists(Path.Combine(tessdata, j + ".traineddata"))) { razlog = "nema " + j + ".traineddata u " + tessdata; return false; }
            return true;
        }

        private TesseractEngine Engine() => engine ?? (engine = new TesseractEngine(tessdata, jezici, EngineMode.LstmOnly));

        /// <summary>OCR slike (JPEG/PNG/TIFF bajtovi). Koordinate reči su relativne (0–1) u odnosu na sliku posle ispravljanja orijentacije.</summary>
        public List<Rec> Prepoznaj(byte[] slika, out string napomena, int dpi = 300)
        {
            napomena = "";
            lock (sync)
            {
                using (var original = Pix.LoadFromMemory(slika))
                {
                    if (original.XRes <= 1) { original.XRes = Math.Max(150, Math.Min(dpi, 600)); original.YRes = original.XRes; }
                    List<Rec> prvo;
                    using (var page = Engine().Process(original, PageSegMode.SparseText))
                        prvo = Reci(page, original.Width, original.Height);
                    // Drugo čitanje sa jezikom zemlje obrasca (hun/ita/ron/bul/slv/hrv) – zadržava se bolje čitanje.
                    var jez = DodatniZa(prvo);
                    if (jez.Count > 0)
                    {
                        var kljuc = jezici + "+" + string.Join("+", jez);
                        if (!sDodatnim.TryGetValue(kljuc, out var e2)) sDodatnim[kljuc] = e2 = new TesseractEngine(tessdata, kljuc, EngineMode.LstmOnly);
                        List<Rec> sa;
                        using (var page = e2.Process(original, PageSegMode.SparseText))
                            sa = Reci(page, original.Width, original.Height);
                        if (Ocena(sa) > Ocena(prvo)) { prvo = sa; napomena = "OCR sa jezikom: " + string.Join(", ", jez); }
                    }
                    // Orijentacija se proverava samo kada je prvo čitanje loše (okrenut sken) – štedi vreme na ispravnim stranama.
                    var dobro = prvo.Count >= 40 && Prosek(prvo) >= 0.6;
                    if (dobro) return prvo;
                    var ugao = Orijentacija(original);
                    if (ugao == 0) return prvo;
                    var pix = ugao == 180 ? original.Rotate90(1).Rotate90(1) : original.Rotate90(ugao == 90 ? -1 : 1);
                    try
                    {
                        List<Rec> drugo;
                        using (var page = Engine().Process(pix, PageSegMode.SparseText))
                            drugo = Reci(page, pix.Width, pix.Height);
                        if (drugo.Count * Prosek(drugo) <= prvo.Count * Prosek(prvo)) return prvo;
                        napomena = "strana okrenuta za " + ugao + "°";
                        return drugo;
                    }
                    finally { pix.Dispose(); }
                }
            }
        }

        /// <summary>Ugao (0, 90, 180, 270) za koji treba okrenuti sliku; 0 ako osd nije dostupan ili nije siguran.</summary>
        private int Orijentacija(Pix pix)
        {
            if (!File.Exists(Path.Combine(tessdata, "osd.traineddata"))) return 0;
            try
            {
                if (osd == null) osd = new TesseractEngine(tessdata, "osd", EngineMode.TesseractOnly);
                using (var page = osd.Process(pix, PageSegMode.OsdOnly))
                {
                    page.DetectBestOrientation(out int ugao, out float pouzdanost);
                    return pouzdanost > 2 ? ((ugao % 360) + 360) % 360 : 0;
                }
            }
            catch { return 0; }
        }

        private TesseractEngine cifre;

        /// <summary>
        /// Ciljani OCR cifara (broj rubrike u krugu, kućice datuma, NHM, r62): samo cifre i dozvoljeni znaci, jedan red.
        /// Vraća pročitan tekst i pouzdanost (0–1); prazno ako ništa nije pročitano.
        /// </summary>
        public (string tekst, double pouzdanost) Cifre(byte[] slika, string dozvoljeno = "0123456789", bool jednaRec = false)
        {
            lock (sync)
            {
                if (cifre == null)
                {
                    cifre = new TesseractEngine(tessdata, "eng", EngineMode.LstmOnly);
                    cifre.SetVariable("classify_bln_numeric_mode", "1");
                }
                cifre.SetVariable("tessedit_char_whitelist", dozvoljeno);
                using (var pix = Pix.LoadFromMemory(slika))
                {
                    if (pix.XRes <= 1) { pix.XRes = 300; pix.YRes = 300; }
                    using (var page = cifre.Process(pix, jednaRec ? PageSegMode.SingleWord : PageSegMode.SingleLine))
                    {
                        var t = (page.GetText() ?? "").Trim();
                        // Prosečna pouzdanost strane je u ovom režimu često 0 – računa se po rečima.
                        var konf = new List<float>();
                        using (var it = page.GetIterator())
                        {
                            it.Begin();
                            do
                            {
                                var w = it.GetText(PageIteratorLevel.Word);
                                if (!string.IsNullOrWhiteSpace(w)) konf.Add(it.GetConfidence(PageIteratorLevel.Word));
                            } while (it.Next(PageIteratorLevel.Word));
                        }
                        var p = konf.Count > 0 ? konf.Average() / 100.0 : page.GetMeanConfidence();
                        return (System.Text.RegularExpressions.Regex.Replace(t, @"\s+", " "), p);
                    }
                }
            }
        }

        /// <summary>OCR isečka (npr. jedne rubrike) zadatim jezicima; koordinate reči relativne u odnosu na isečak.</summary>
        public List<Rec> PrepoznajJezicima(byte[] slika, string jeziciIsecka, int dpi = 400)
        {
            var dostupni = string.Join("+", (jeziciIsecka ?? "").Split('+').Where(j => File.Exists(Path.Combine(tessdata, j + ".traineddata"))));
            if (dostupni.Length == 0) dostupni = jezici;
            lock (sync)
            {
                if (!sDodatnim.TryGetValue(dostupni, out var e)) sDodatnim[dostupni] = e = new TesseractEngine(tessdata, dostupni, EngineMode.LstmOnly);
                using (var pix = Pix.LoadFromMemory(slika))
                {
                    if (pix.XRes <= 1) { pix.XRes = dpi; pix.YRes = dpi; }
                    using (var page = e.Process(pix, PageSegMode.SparseText)) return Reci(page, pix.Width, pix.Height);
                }
            }
        }

        private TesseractEngine alfanum;

        /// <summary>Cela strana samo velikim latiničnim slovima i ciframa (MRN, šifre) – opšti OCR sa ćirilicom ih kvari.</summary>
        public string Alfanum(byte[] slika)
        {
            lock (sync)
            {
                if (alfanum == null)
                {
                    alfanum = new TesseractEngine(tessdata, "eng", EngineMode.LstmOnly);
                    alfanum.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789:-/.");
                }
                using (var pix = Pix.LoadFromMemory(slika))
                {
                    if (pix.XRes <= 1) { pix.XRes = 300; pix.YRes = 300; }
                    using (var page = alfanum.Process(pix, PageSegMode.SparseText)) return page.GetText() ?? "";
                }
            }
        }

        private static double Prosek(List<Rec> r) => r.Count == 0 ? 0 : r.Average(x => x.Pouzdanost);

        private List<Rec> Reci(Page page, int w, int h)
        {
            var rez = new List<Rec>();
            using (var it = page.GetIterator())
            {
                it.Begin();
                do
                {
                    if (!it.TryGetBoundingBox(PageIteratorLevel.Word, out var b)) continue;
                    var t = it.GetText(PageIteratorLevel.Word)?.Trim();
                    // OCR šum unutar reči ("·ЦАРИ·НСКА", "~·~") se uklanja
                    t = t == null ? null : System.Text.RegularExpressions.Regex.Replace(t, "[·~•¬]", "");
                    if (string.IsNullOrEmpty(t)) continue;
                    var c = it.GetConfidence(PageIteratorLevel.Word) / 100.0;
                    if (c < minPouzdanostReci) continue;
                    rez.Add(new Rec { T = t, X0 = b.X1 / (double)w, Y0 = b.Y1 / (double)h, X1 = b.X2 / (double)w, Y1 = b.Y2 / (double)h, Pouzdanost = c });
                } while (it.Next(PageIteratorLevel.Word));
            }
            return rez;
        }

        public void Dispose()
        {
            engine?.Dispose();
            osd?.Dispose();
            cifre?.Dispose();
            foreach (var e in sDodatnim.Values) e.Dispose();
            alfanum?.Dispose();
        }
    }
}
