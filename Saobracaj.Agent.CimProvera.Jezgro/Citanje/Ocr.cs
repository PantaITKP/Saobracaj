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
                    PoslednjiUgao = 0;
                    // v2.8.4 (2): orijentacija se proverava kada čitanje nije pouzdano (ispod 0,75) – položeni skenovi (3507×2480,
                    // sadržaj zarotiran za 90°) daju 0,63–0,69 i ranije nisu okretani (prag je bio 0,6).
                    var dobro = prvo.Count >= 40 && Prosek(prvo) >= 0.75;
                    if (!dobro)
                    {
                        var polozena = original.Width > original.Height * 1.15;
                        // 1) Tesseract OSD; 2) ako OSD nije siguran: proba 90° i 270° (položena slika ili vrlo loše čitanje)
                        var osd = Orijentacija(original);
                        var probe = new List<int>();
                        if (osd != 0) probe.Add(osd == 90 ? 270 : osd == 270 ? 90 : 180);
                        if (polozena || Prosek(prvo) < 0.6) foreach (var u in new[] { 90, 270 }) if (!probe.Contains(u)) probe.Add(u);
                        var najbolje = prvo; var najboljiUgao = 0;
                        foreach (var cw in probe)
                        {
                            using (var pix = Okreni(original, cw))
                            using (var page = Engine().Process(pix, PageSegMode.SparseText))
                            {
                                var r = Reci(page, pix.Width, pix.Height);
                                // okretanje se prihvata samo uz jasno bolje čitanje (bar 15 %)
                                if (Ocena(r) > Ocena(najbolje) * 1.15 && Prosek(r) > Prosek(najbolje)) { najbolje = r; najboljiUgao = cw; }
                            }
                        }
                        if (najboljiUgao != 0)
                        {
                            napomena = (napomena.Length > 0 ? napomena + ", " : "") + "strana okrenuta za " + najboljiUgao + "° (pouzdanost " + Prosek(prvo).ToString("0.00") + " → " + Prosek(najbolje).ToString("0.00") + ")";
                            prvo = najbolje; PoslednjiUgao = najboljiUgao;
                        }
                    }
                    // v2.8.4 (2): crno-beli sken (1-bit, CCITT): sivi tonovi, uvećanje sa zaglađivanjem (blago zamućenje ivica) i ispravljanje
                    // nagiba pre OCR-a – zadržava se bolje čitanje
                    if (Prosek(prvo) < 0.8 && CrnoBela(slika))
                    {
                        var pre = Prosek(prvo);
                        using (var okrenut = Okreni(original, PoslednjiUgao))
                        using (var siva = okrenut.Depth == 1 ? okrenut.ConvertTo8(0) : okrenut.Depth == 32 ? okrenut.ConvertRGBToGray() : okrenut.Clone())
                        using (var veca = siva.Scale(1.5f, 1.5f))
                        using (var ravna = veca.Deskew())
                        {
                            ravna.XRes = (int)Math.Round(original.XRes * 1.5); ravna.YRes = ravna.XRes;
                            List<Rec> r;
                            using (var page = Engine().Process(ravna, PageSegMode.SparseText)) r = Reci(page, ravna.Width, ravna.Height);
                            if (Ocena(r) > Ocena(prvo) && Prosek(r) >= pre)
                            {
                                napomena = (napomena.Length > 0 ? napomena + ", " : "") + "crno-beli sken: sivi tonovi, uvećanje i ispravljanje nagiba (pouzdanost " + pre.ToString("0.00") + " → " + Prosek(r).ToString("0.00") + ")";
                                prvo = r;
                            }
                            else napomena = (napomena.Length > 0 ? napomena + ", " : "") + "crno-beli sken: obrada ne poboljšava čitanje (" + pre.ToString("0.00") + " → " + Prosek(r).ToString("0.00") + ")";
                        }
                    }
                    return prvo;
                }
            }
        }

        /// <summary>Ugao (u smeru kazaljke, 0/90/180/270) za koji je okrenuta slika poslednjeg čitanja (<see cref="Prepoznaj"/>); koordinate reči su posle okretanja.</summary>
        public int PoslednjiUgao { get; private set; }

        /// <summary>Kopija slike okrenuta za cw stepeni u smeru kazaljke (0 = kopija).</summary>
        private static Pix Okreni(Pix p, int cw) => cw == 90 ? p.Rotate90(1) : cw == 270 ? p.Rotate90(-1) : cw == 180 ? Rot180(p) : p.Clone();

        private static Pix Rot180(Pix p) { using (var a = p.Rotate90(1)) return a.Rotate90(1); }

        /// <summary>
        /// Crno-beli sken: 1-bit slika (CCITT/JBIG2 posle iscrtavanja je 32-bit, ali samo crni i beli pikseli) – uzorak piksela,
        /// manje od 2 % sivih tonova.
        /// </summary>
        internal static bool CrnoBela(byte[] slika)
        {
            try
            {
                using (var ms = new MemoryStream(slika))
                using (var img = System.Drawing.Image.FromStream(ms, false, false))
                {
                    if (img.PixelFormat == System.Drawing.Imaging.PixelFormat.Format1bppIndexed) return true;
                    var b = img as System.Drawing.Bitmap;
                    if (b == null) return false;
                    // među pikselima koji nisu beli (mastilo i ivice slova): crno-beli sken nema sivih tonova, a sken u sivim tonovima ili
                    // tekst sa zaglađenim ivicama ima ih mnogo (ivica slova, papir)
                    int sivih = 0, mastila = 0;
                    var korak = Math.Max(1, (int)Math.Sqrt((double)b.Width * b.Height / 60000));
                    for (int y = 0; y < b.Height; y += korak)
                        for (int x = 0; x < b.Width; x += korak)
                        {
                            var c = b.GetPixel(x, y);
                            var g = (c.R + c.G + c.B) / 3;
                            if (g >= 215) continue;
                            mastila++;
                            if (g > 40) sivih++;
                        }
                    return mastila >= 50 && sivih < mastila * 0.05;
                }
            }
            catch { return false; }
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

        /// <summary>
        /// v2.8.4 (3): OCR isečka kao jednog bloka redova (PageSegMode.SingleBlock) – za tekst rubrike u redovima (r7), kada SparseText
        /// izgubi reči („Panšped doo“ → „Panšped i“). Bez praga pouzdanosti reči (proveru radi pozivalac).
        /// </summary>
        public List<Rec> PrepoznajBlok(byte[] slika, string jeziciIsecka, int dpi = 400)
        {
            var dostupni = string.Join("+", (jeziciIsecka ?? "").Split('+').Where(j => File.Exists(Path.Combine(tessdata, j + ".traineddata"))));
            if (dostupni.Length == 0) dostupni = jezici;
            lock (sync)
            {
                if (!sDodatnim.TryGetValue(dostupni, out var e)) sDodatnim[dostupni] = e = new TesseractEngine(tessdata, dostupni, EngineMode.LstmOnly);
                using (var pix = Pix.LoadFromMemory(slika))
                {
                    if (pix.XRes <= 1) { pix.XRes = dpi; pix.YRes = dpi; }
                    using (var page = e.Process(pix, PageSegMode.SingleBlock))
                    {
                        var rez = new List<Rec>();
                        using (var it = page.GetIterator())
                        {
                            it.Begin();
                            do
                            {
                                if (!it.TryGetBoundingBox(PageIteratorLevel.Word, out var b)) continue;
                                var t = it.GetText(PageIteratorLevel.Word)?.Trim();
                                if (string.IsNullOrEmpty(t)) continue;
                                rez.Add(new Rec { T = t, X0 = b.X1 / (double)pix.Width, Y0 = b.Y1 / (double)pix.Height, X1 = b.X2 / (double)pix.Width, Y1 = b.Y2 / (double)pix.Height, Pouzdanost = it.GetConfidence(PageIteratorLevel.Word) / 100.0 });
                            } while (it.Next(PageIteratorLevel.Word));
                        }
                        return rez;
                    }
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
