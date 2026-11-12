using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Saobracaj.Agent.CimProvera.Citanje
{
    /// <summary>Linija okvira obrasca: vodoravna (A = y, od B0 do B1 po x) ili uspravna (A = x, od B0 do B1 po y), relativno 0–1.</summary>
    internal sealed class LinijaOkvira
    {
        public double A, B0, B1;
        public double Duzina => B1 - B0;
        public override string ToString() => A.ToString("0.0000") + " [" + B0.ToString("0.000") + "–" + B1.ToString("0.000") + "]";
    }

    /// <summary>Zaokružen broj rubrike ili kvadratić za oznaku: centar i veličina (relativno 0–1).</summary>
    internal sealed class Oblik
    {
        public double X0, Y0, X1, Y1;
        /// <summary>Pun (crn) krug sa belim brojem.</summary>
        public bool Pun;
        public double Cx => (X0 + X1) / 2;
        public double Cy => (Y0 + Y1) / 2;
    }

    /// <summary>
    /// Crno-bela slika strane (mastilo) u fiksnoj rezoluciji (~150 dpi): linije okvira, zaokruženi brojevi rubrika,
    /// kvadratići i gustina mastila u pravougaoniku (za prazne rubrike i polja za označavanje). Sve je deterministično.
    /// </summary>
    internal sealed class Raster
    {
        public readonly int W, H;
        private readonly bool[] ink;
        /// <summary>Svetliji prag (samo za linije okvira): na skenu su linije obrasca često svetlije od teksta.</summary>
        private readonly bool[] svetlo;
        private bool[] linija;          // piksel pripada dugoj liniji okvira
        private int[] integ, integBez;  // integralne slike: svo mastilo / mastilo bez linija okvira
        private List<LinijaOkvira> hl, vl;
        private List<Oblik> krugovi;

        private Raster(int w, int h, bool[] ink, bool[] svetlo = null) { W = w; H = h; this.ink = ink; this.svetlo = svetlo; }

        public bool Ink(int x, int y) => x >= 0 && y >= 0 && x < W && y < H && ink[y * W + x];

        /// <summary>Iz slike (PNG/JPEG…): smanjuje se na širinu ~sirina piksela, prag po Otsu metodi (ograničen na 100–200).</summary>
        public static Raster IzSlike(byte[] slika, int sirina = 1240)
        {
            using (var ms = new MemoryStream(slika))
            using (var orig = new Bitmap(ms))
            {
                var faktor = Math.Min(1.0, sirina / (double)orig.Width);
                int w = Math.Max(1, (int)Math.Round(orig.Width * faktor)), h = Math.Max(1, (int)Math.Round(orig.Height * faktor));
                using (var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.White);
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                        g.DrawImage(orig, 0, 0, w, h);
                    }
                    var siva = new byte[w * h];
                    var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    try
                    {
                        var red = new byte[data.Stride];
                        for (int y = 0; y < h; y++)
                        {
                            Marshal.Copy(data.Scan0 + y * data.Stride, red, 0, data.Stride);
                            for (int x = 0; x < w; x++)
                                siva[y * w + x] = (byte)((red[x * 3] * 114 + red[x * 3 + 1] * 587 + red[x * 3 + 2] * 299) / 1000);
                        }
                    }
                    finally { bmp.UnlockBits(data); }
                    var prag = Otsu(siva);
                    var ink = new bool[w * h];
                    var svetlo = new bool[w * h];
                    var pragLinija = Math.Min(225, prag + 55);
                    for (int i = 0; i < ink.Length; i++) { ink[i] = siva[i] < prag; svetlo[i] = siva[i] < pragLinija; }
                    return new Raster(w, h, ink, svetlo);
                }
            }
        }

        private static int Otsu(byte[] siva)
        {
            var hist = new long[256];
            foreach (var b in siva) hist[b]++;
            long ukupno = siva.Length; double suma = 0;
            for (int i = 0; i < 256; i++) suma += i * (double)hist[i];
            double sumB = 0; long wB = 0; double max = -1; int prag = 160;
            for (int t = 0; t < 256; t++)
            {
                wB += hist[t]; if (wB == 0) continue;
                var wF = ukupno - wB; if (wF == 0) break;
                sumB += t * (double)hist[t];
                double mB = sumB / wB, mF = (suma - sumB) / wF;
                var izmedju = (double)wB * wF * (mB - mF) * (mB - mF);
                if (izmedju > max) { max = izmedju; prag = t; }
            }
            return Math.Max(100, Math.Min(200, prag));
        }

        /// <summary>Kompaktan zapis (PNG 1 bit) za keš strana.</summary>
        public byte[] UPng() => UPng(ink);

        /// <summary>Mapa za linije okvira (svetliji prag); null ako je raster napravljen bez nje.</summary>
        public byte[] UPngLinije() => svetlo == null ? null : UPng(svetlo);

        private byte[] UPng(bool[] ink)
        {
            using (var bmp = new Bitmap(W, H, PixelFormat.Format1bppIndexed))
            {
                var data = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);
                try
                {
                    var red = new byte[data.Stride];
                    for (int y = 0; y < H; y++)
                    {
                        Array.Clear(red, 0, red.Length);
                        for (int x = 0; x < W; x++)
                            if (!ink[y * W + x]) red[x >> 3] |= (byte)(0x80 >> (x & 7)); // 1 = belo (paleta: 0 crno, 1 belo)
                        Marshal.Copy(red, 0, data.Scan0 + y * data.Stride, data.Stride);
                    }
                }
                finally { bmp.UnlockBits(data); }
                using (var ms = new MemoryStream()) { bmp.Save(ms, ImageFormat.Png); return ms.ToArray(); }
            }
        }

        /// <summary>Iz zapisa UPng (bez ponovnog praga i bez smanjivanja).</summary>
        public static Raster IzPng(byte[] png, byte[] pngLinije = null)
        {
            var r = IzPng1(png);
            return pngLinije == null ? r : new Raster(r.W, r.H, r.ink, IzPng1(pngLinije).ink);
        }

        private static Raster IzPng1(byte[] png)
        {
            using (var ms = new MemoryStream(png))
            using (var bmp = new Bitmap(ms))
            {
                int w = bmp.Width, h = bmp.Height;
                var ink = new bool[w * h];
                using (var b24 = new Bitmap(w, h, PixelFormat.Format24bppRgb))
                {
                    using (var g = Graphics.FromImage(b24)) g.DrawImage(bmp, 0, 0, w, h);
                    var data = b24.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    try
                    {
                        var red = new byte[data.Stride];
                        for (int y = 0; y < h; y++)
                        {
                            Marshal.Copy(data.Scan0 + y * data.Stride, red, 0, data.Stride);
                            for (int x = 0; x < w; x++) ink[y * w + x] = red[x * 3] < 128;
                        }
                    }
                    finally { b24.UnlockBits(data); }
                }
                return new Raster(w, h, ink);
            }
        }

        // ------------------------------------------------------------------------------------------- linije

        public List<LinijaOkvira> Vodoravne { get { Linije(); return hl; } }
        public List<LinijaOkvira> Uspravne { get { Linije(); return vl; } }

        /// <summary>
        /// Linije okvira: nizovi mastila duži od 3 % širine (vodoravne) / 2 % visine (uspravne), sa prekidom do 2 piksela.
        /// Delovi iz susednih redova (debljina linije, blago nakrivljen sken) se spajaju u jednu liniju.
        /// </summary>
        private void Linije()
        {
            if (hl != null) return;
            linija = new bool[W * H];
            var izvor = svetlo ?? ink;
            int minH = Math.Max(20, (int)(W * 0.03)), minV = Math.Max(20, (int)(H * 0.02));
            // Niz sa prekidima do 6 px (isprekidana, svetla linija skena), pokriven bar 60 %, i TANAK: 4 px iznad i ispod
            // (levo i desno) uglavnom nema mastila. Red teksta ima uspravne poteze, pa nije linija.
            bool Dark(int x, int y) => x >= 0 && y >= 0 && x < W && y < H && izvor[y * W + x];
            var hDelovi = new List<(int a, int b0, int b1)>();
            for (int y = 0; y < H; y++)
                foreach (var (x0, x1) in Nizovi(i => izvor[y * W + i], W, 6))
                    if (x1 - x0 + 1 >= minH && Pokriveno(i => izvor[y * W + i], x0, x1) >= 0.6 && Tanko(x0, x1, i => Dark(i, y), i => !Dark(i, y - 4) && !Dark(i, y + 4)) >= 0.6)
                        hDelovi.Add((y, x0, x1));
            var vDelovi = new List<(int a, int b0, int b1)>();
            for (int x = 0; x < W; x++)
                foreach (var (y0, y1) in Nizovi(i => izvor[i * W + x], H, 6))
                    if (y1 - y0 + 1 >= minV && Pokriveno(i => izvor[i * W + x], y0, y1) >= 0.6 && Tanko(y0, y1, i => Dark(x, i), i => !Dark(x - 4, i) && !Dark(x + 4, i)) >= 0.6)
                        vDelovi.Add((x, y0, y1));
            hl = Spoji(hDelovi).Select(s => new LinijaOkvira { A = s.a / (double)H, B0 = s.b0 / (double)W, B1 = (s.b1 + 1) / (double)W }).ToList();
            vl = Spoji(vDelovi).Select(s => new LinijaOkvira { A = s.a / (double)W, B0 = s.b0 / (double)H, B1 = (s.b1 + 1) / (double)H }).ToList();
            // Maska linija (za mastilo bez okvira i za ćelije): ceo pojas linije, ±2 px oko sredine.
            foreach (var d in hDelovi) for (int y = Math.Max(0, d.a - 1); y <= Math.Min(H - 1, d.a + 1); y++) for (int x = d.b0; x <= d.b1; x++) linija[y * W + x] = true;
            foreach (var d in vDelovi) for (int x = Math.Max(0, d.a - 1); x <= Math.Min(W - 1, d.a + 1); x++) for (int y = d.b0; y <= d.b1; y++) linija[y * W + x] = true;
        }

        private static double Tanko(int a, int b, Func<int, bool> je, Func<int, bool> okoPrazno)
        {
            int n = 0, tanko = 0;
            for (int i = a; i <= b; i++) if (je(i)) { n++; if (okoPrazno(i)) tanko++; }
            return n == 0 ? 0 : tanko / (double)n;
        }

        private static double Pokriveno(Func<int, bool> je, int a, int b)
        {
            int n = 0; for (int i = a; i <= b; i++) if (je(i)) n++;
            return n / (double)(b - a + 1);
        }

        private static IEnumerable<(int, int)> Nizovi(Func<int, bool> je, int n, int prekid)
        {
            int poc = -1, posl = -1;
            for (int i = 0; i < n; i++)
            {
                if (!je(i)) continue;
                if (poc < 0) { poc = posl = i; continue; }
                if (i - posl - 1 <= prekid) { posl = i; continue; }
                yield return (poc, posl);
                poc = posl = i;
            }
            if (poc >= 0) yield return (poc, posl);
        }

        /// <summary>Delovi u susednim redovima (razmak do 3 px) koji se preklapaju po dužini su ista linija.</summary>
        private static List<(double a, int b0, int b1)> Spoji(List<(int a, int b0, int b1)> delovi)
        {
            var grupe = new List<(List<(int a, int b0, int b1)> d, int poslA, int b0, int b1)>();
            foreach (var d in delovi.OrderBy(x => x.a).ThenBy(x => x.b0))
            {
                var i = grupe.FindIndex(g => d.a - g.poslA <= 3 && d.b0 <= g.b1 + 3 && d.b1 >= g.b0 - 3);
                if (i < 0) grupe.Add((new List<(int, int, int)> { d }, d.a, d.b0, d.b1));
                else { var g = grupe[i]; g.d.Add(d); grupe[i] = (g.d, d.a, Math.Min(g.b0, d.b0), Math.Max(g.b1, d.b1)); }
            }
            return grupe.Select(g => (g.d.Sum(x => (double)x.a * (x.b1 - x.b0 + 1)) / g.d.Sum(x => (double)(x.b1 - x.b0 + 1)), g.b0, g.b1))
                .OrderBy(x => x.Item1).ThenBy(x => x.b0).ToList();
        }

        // ------------------------------------------------------------------------------------------- gustina

        private void Integrali()
        {
            if (integ != null) return;
            Linije();
            integ = new int[(W + 1) * (H + 1)];
            integBez = new int[(W + 1) * (H + 1)];
            for (int y = 0; y < H; y++)
            {
                int red = 0, redBez = 0;
                for (int x = 0; x < W; x++)
                {
                    var i = y * W + x;
                    if (ink[i]) { red++; if (!linija[i]) redBez++; }
                    integ[(y + 1) * (W + 1) + x + 1] = integ[y * (W + 1) + x + 1] + red;
                    integBez[(y + 1) * (W + 1) + x + 1] = integBez[y * (W + 1) + x + 1] + redBez;
                }
            }
        }

        /// <summary>Broj piksela mastila u pravougaoniku (relativne koordinate); bezLinija = bez linija okvira obrasca.</summary>
        public int Piksela(double x0, double y0, double x1, double y1, bool bezLinija = true)
        {
            Integrali();
            int a = Clamp((int)Math.Floor(x0 * W), W), b = Clamp((int)Math.Floor(y0 * H), H), c = Clamp((int)Math.Ceiling(x1 * W), W), d = Clamp((int)Math.Ceiling(y1 * H), H);
            if (c <= a || d <= b) return 0;
            var t = bezLinija ? integBez : integ;
            return t[d * (W + 1) + c] - t[b * (W + 1) + c] - t[d * (W + 1) + a] + t[b * (W + 1) + a];
        }

        /// <summary>Udeo piksela sa mastilom u pravougaoniku (0–1).</summary>
        public double Gustina(double x0, double y0, double x1, double y1, bool bezLinija = true)
        {
            var povrsina = Math.Max(1.0, (x1 - x0) * W * ((y1 - y0) * H));
            return Piksela(x0, y0, x1, y1, bezLinija) / povrsina;
        }

        private static int Clamp(int v, int max) => v < 0 ? 0 : v > max ? max : v;

        // ------------------------------------------------------------------------------------------- oblici

        /// <summary>
        /// Zaokruženi brojevi rubrika: povezane grupe mastila (bez linija okvira) veličine 0,8–2,6 % širine strane,
        /// približno okrugle i šuplje (prsten), bez obzira na to da li je OCR pročitao broj u krugu.
        /// </summary>
        public List<Oblik> Krugovi()
        {
            if (krugovi != null) return krugovi;
            Integrali();
            krugovi = new List<Oblik>();
            foreach (var k in Komponente(i => ink[i] && !linija[i]))
            {
                int w = k.x1 - k.x0 + 1, h = k.y1 - k.y0 + 1;
                if (w < W * 0.008 || w > W * 0.026 || h < W * 0.008 || h > W * 0.026) continue;
                if (Math.Abs(w - h) > Math.Max(w, h) * 0.25) continue;
                var popunjenost = k.n / (double)(w * h);
                double cx = (k.x0 + k.x1) / 2.0, cy = (k.y0 + k.y1) / 2.0, r = (w + h) / 4.0;
                int blizu = 0, unutra = 0, uglovi = 0;
                foreach (var p in k.pikseli)
                {
                    var dist = Math.Sqrt((p % W - cx) * (p % W - cx) + (p / W - cy) * (p / W - cy));
                    if (Math.Abs(dist - r) <= Math.Max(2, r * 0.3)) blizu++;
                    if (dist < r * 0.45) unutra++;
                    if (dist > r * 1.12 + 1) uglovi++;
                }
                // prsten (broj u krugu): pikseli blizu poluprečnika, unutrašnjost prazna (cifre su posebna grupa)
                var prsten = popunjenost >= 0.08 && popunjenost <= 0.5 && blizu >= k.n * 0.8 && unutra <= k.n * 0.05;
                // pun krug (beli broj na crnom krugu): popunjen krug, uglovi pravougaonika prazni
                var disk = popunjenost >= 0.4 && popunjenost <= 0.8 && uglovi <= k.n * 0.04 && w >= W * 0.01;
                if (!prsten && !disk) continue;
                krugovi.Add(new Oblik { X0 = k.x0 / (double)W, Y0 = k.y0 / (double)H, X1 = (k.x1 + 1) / (double)W, Y1 = (k.y1 + 1) / (double)H, Pun = disk && !prsten });
            }
            krugovi = krugovi.OrderBy(o => o.Cy).ThenBy(o => o.Cx).ToList();
            return krugovi;
        }

        /// <summary>Prazni kvadratići (polja za označavanje): kvadratni okvir 1–3,2 % širine strane, mastilo uz ivice.</summary>
        public List<Oblik> Kvadratici()
        {
            Integrali();
            var rez = new List<Oblik>();
            foreach (var k in Komponente(i => ink[i]))
            {
                int w = k.x1 - k.x0 + 1, h = k.y1 - k.y0 + 1;
                if (w < W * 0.01 || w > W * 0.032 || h < W * 0.01 || h > W * 0.032) continue;
                if (Math.Abs(w - h) > Math.Max(w, h) * 0.3) continue;
                int traka = Math.Max(2, Math.Min(w, h) / 6), uzIvicu = 0;
                var strane = new int[4];
                foreach (var p in k.pikseli)
                {
                    int x = p % W, y = p / W;
                    bool l = x - k.x0 < traka, d = k.x1 - x < traka, g = y - k.y0 < traka, dn = k.y1 - y < traka;
                    if (l || d || g || dn) uzIvicu++;
                    if (l) strane[0]++; if (d) strane[1]++; if (g) strane[2]++; if (dn) strane[3]++;
                }
                if (uzIvicu < k.n * 0.85) continue;
                if (strane[0] < h * 0.7 || strane[1] < h * 0.7 || strane[2] < w * 0.7 || strane[3] < w * 0.7) continue;
                rez.Add(new Oblik { X0 = k.x0 / (double)W, Y0 = k.y0 / (double)H, X1 = (k.x1 + 1) / (double)W, Y1 = (k.y1 + 1) / (double)H });
            }
            return rez.OrderBy(o => o.Cy).ThenBy(o => o.Cx).ToList();
        }

        /// <summary>
        /// Zatvorene ćelije obrasca: oblasti omeđene linijama okvira (linije podebljane 2 px da se zatvore prekidi skena).
        /// Redom od vrha strane; preskaču se vrlo male oblasti i spoljašnjost obrasca.
        /// </summary>
        public List<Oblik> Celije(double minPovrsina = 0.0004)
        {
            Linije();
            var zid = new bool[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (linija[y * W + x])
                        for (int dy = -2; dy <= 2; dy++)
                            for (int dx = -2; dx <= 2; dx++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx >= 0 && ny >= 0 && nx < W && ny < H) zid[ny * W + nx] = true;
                            }
            var vidjeno = new bool[W * H];
            var rez = new List<Oblik>();
            var red = new Stack<int>();
            for (int start = 0; start < W * H; start++)
            {
                if (vidjeno[start] || zid[start]) continue;
                int x0 = int.MaxValue, y0 = int.MaxValue, x1 = 0, y1 = 0, n = 0; var ivica = false;
                red.Push(start); vidjeno[start] = true;
                while (red.Count > 0)
                {
                    var p = red.Pop(); int x = p % W, y = p / W; n++;
                    if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y;
                    if (x == 0 || y == 0 || x == W - 1 || y == H - 1) ivica = true;
                    if (x > 0 && !vidjeno[p - 1] && !zid[p - 1]) { vidjeno[p - 1] = true; red.Push(p - 1); }
                    if (x < W - 1 && !vidjeno[p + 1] && !zid[p + 1]) { vidjeno[p + 1] = true; red.Push(p + 1); }
                    if (y > 0 && !vidjeno[p - W] && !zid[p - W]) { vidjeno[p - W] = true; red.Push(p - W); }
                    if (y < H - 1 && !vidjeno[p + W] && !zid[p + W]) { vidjeno[p + W] = true; red.Push(p + W); }
                }
                if (ivica || n < minPovrsina * W * H) continue;
                rez.Add(new Oblik { X0 = (x0 - 2) / (double)W, Y0 = (y0 - 2) / (double)H, X1 = (x1 + 3) / (double)W, Y1 = (y1 + 3) / (double)H });
            }
            return rez.OrderBy(o => Math.Round(o.Y0, 2)).ThenBy(o => o.X0).ToList();
        }

        /// <summary>
        /// Mastilo podataka u pravougaoniku (udeo površine): povezane grupe mastila bez linija okvira, bez sitnih tačkica,
        /// bez tankih crtica (pregrade kućica, tačkasta linija za pisanje) i bez grupa čiji je centar u isključenim
        /// pravougaonicima (odštampani nazivi rubrika, krugovi sa brojem).
        /// </summary>
        public double MastiloPodataka(double rx0, double ry0, double rx1, double ry1, IEnumerable<double[]> iskljuci)
        {
            Linije();
            int a = Clamp((int)Math.Ceiling(rx0 * W), W), b = Clamp((int)Math.Ceiling(ry0 * H), H), c = Clamp((int)Math.Floor(rx1 * W), W), d = Clamp((int)Math.Floor(ry1 * H), H);
            if (c - a < 2 || d - b < 2) return 0;
            var isk = (iskljuci ?? Enumerable.Empty<double[]>()).ToList();
            var vidjeno = new HashSet<int>();
            var red = new Stack<int>();
            long suma = 0;
            for (int y = b; y < d; y++)
                for (int x = a; x < c; x++)
                {
                    var s = y * W + x;
                    if (!ink[s] || linija[s] || vidjeno.Contains(s)) continue;
                    int n = 0, x0 = x, x1 = x, y0 = y, y1 = y;
                    red.Push(s); vidjeno.Add(s);
                    while (red.Count > 0)
                    {
                        var p = red.Pop(); int px = p % W, py = p / W; n++;
                        if (px < x0) x0 = px; if (px > x1) x1 = px; if (py < y0) y0 = py; if (py > y1) y1 = py;
                        for (int dy = -1; dy <= 1; dy++)
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = px + dx, ny = py + dy;
                                if (nx < a || ny < b || nx >= c || ny >= d) continue;
                                var q = ny * W + nx;
                                if (!ink[q] || linija[q] || vidjeno.Contains(q)) continue;
                                vidjeno.Add(q); red.Push(q);
                            }
                    }
                    int w = x1 - x0 + 1, h = y1 - y0 + 1;
                    if (n < 6) continue;                                   // tačkica / šum skena
                    if (w <= 3 || h <= 2) continue;                        // tanka crtica (pregrada kućice, crta za pisanje)
                    double cx = (x0 + x1 + 1) / 2.0 / W, cy = (y0 + y1 + 1) / 2.0 / H;
                    if (isk.Any(r => cx >= r[0] && cx <= r[2] && cy >= r[1] && cy <= r[3])) continue;
                    suma += n;
                }
            return suma / (double)Math.Max(1, (c - a) * (d - b));
        }

        private sealed class Komponenta { public int x0 = int.MaxValue, y0 = int.MaxValue, x1, y1, n; public List<int> pikseli = new List<int>(); }

        /// <summary>Povezane grupe piksela (8 suseda), redom od vrha strane – deterministično. Veoma velike grupe se preskaču.</summary>
        private List<Komponenta> Komponente(Func<int, bool> je)
        {
            var vidjeno = new bool[W * H];
            var rez = new List<Komponenta>();
            var red = new Stack<int>();
            int max = (int)(W * 0.04) * (int)(W * 0.04);
            for (int start = 0; start < W * H; start++)
            {
                if (vidjeno[start] || !je(start)) continue;
                var k = new Komponenta();
                red.Push(start); vidjeno[start] = true;
                var preveliko = false;
                while (red.Count > 0)
                {
                    var p = red.Pop();
                    int x = p % W, y = p / W;
                    k.n++;
                    if (!preveliko) { k.pikseli.Add(p); if (k.pikseli.Count > max) { preveliko = true; k.pikseli.Clear(); } }
                    if (x < k.x0) k.x0 = x; if (x > k.x1) k.x1 = x; if (y < k.y0) k.y0 = y; if (y > k.y1) k.y1 = y;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                            var q = ny * W + nx;
                            if (vidjeno[q] || !je(q)) continue;
                            vidjeno[q] = true; red.Push(q);
                        }
                }
                if (!preveliko) rez.Add(k);
            }
            return rez;
        }

        /// <summary>Slika za crtanje (proveru): mastilo crno, linije okvira sivo.</summary>
        public Bitmap UBitmap()
        {
            Linije();
            var bmp = new Bitmap(W, H, PixelFormat.Format24bppRgb);
            var data = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try
            {
                var red = new byte[data.Stride];
                for (int y = 0; y < H; y++)
                {
                    for (int x = 0; x < W; x++)
                    {
                        var i = y * W + x;
                        byte v = !ink[i] ? (byte)255 : linija[i] ? (byte)150 : (byte)60;
                        red[x * 3] = red[x * 3 + 1] = red[x * 3 + 2] = v;
                    }
                    Marshal.Copy(red, 0, data.Scan0 + y * data.Stride, data.Stride);
                }
            }
            finally { bmp.UnlockBits(data); }
            return bmp;
        }
    }
}
