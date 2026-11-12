using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Saobracaj.Agent.CimProvera.Citanje
{
    /// <summary>Isečci strane za ciljani OCR i slike sa ucrtanim okvirima (za proveru šablona i dodele rubrika).</summary>
    internal static class Slike
    {
        /// <summary>
        /// Isečak strane (relativne koordinate) kao PNG za OCR cifara: uklanjaju se linije kućica (dugi vodoravni i uspravni
        /// nizovi mastila), dodaje se beli rub i sitan isečak se uvećava.
        /// </summary>
        /// <param name="krug">0 = obično; 1 = broj u krugu (prsten se briše); 2 = beli broj na crnom krugu (unutrašnjost se obrće).</param>
        public static byte[] Isecak(Bitmap strana, double x0, double y0, double x1, double y1, bool ukloniLinije = true, int krug = 0)
        {
            int a = Math.Max(0, (int)(x0 * strana.Width)), b = Math.Max(0, (int)(y0 * strana.Height));
            int c = Math.Min(strana.Width, (int)Math.Ceiling(x1 * strana.Width)), d = Math.Min(strana.Height, (int)Math.Ceiling(y1 * strana.Height));
            int w = Math.Max(1, c - a), h = Math.Max(1, d - b);
            var siva = new byte[w * h];
            using (var crop = strana.Clone(new Rectangle(a, b, w, h), PixelFormat.Format24bppRgb))
            {
                var data = crop.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    var red = new byte[data.Stride];
                    for (int y = 0; y < h; y++)
                    {
                        Marshal.Copy(data.Scan0 + y * data.Stride, red, 0, data.Stride);
                        for (int x = 0; x < w; x++) siva[y * w + x] = (byte)((red[x * 3] * 114 + red[x * 3 + 1] * 587 + red[x * 3 + 2] * 299) / 1000);
                    }
                }
                finally { crop.UnlockBits(data); }
            }
            var ink = siva.Select(v => v < 150).ToArray();
            if (krug > 0)
            {
                double cx = w / 2.0, cy = h / 2.0, r = Math.Min(w, h) / 2.0;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        var dist = Math.Sqrt((x + 0.5 - cx) * (x + 0.5 - cx) + (y + 0.5 - cy) * (y + 0.5 - cy));
                        var i = y * w + x;
                        if (krug == 1) { if (dist > r * 0.68) ink[i] = false; }
                        else ink[i] = dist <= r * 0.86 && !ink[i];
                    }
            }
            if (ukloniLinije)
            {
                var brisi = new bool[w * h];
                // vodoravne linije: niz duži od 45 % širine; uspravne (pregrade kućica): duže od 55 % visine
                for (int y = 0; y < h; y++) { int poc = -1; for (int x = 0; x <= w; x++) { var je = x < w && ink[y * w + x]; if (je && poc < 0) poc = x; if (!je && poc >= 0) { if (x - poc >= w * 0.45) for (int k = poc; k < x; k++) brisi[y * w + k] = true; poc = -1; } } }
                for (int x = 0; x < w; x++) { int poc = -1; for (int y = 0; y <= h; y++) { var je = y < h && ink[y * w + x]; if (je && poc < 0) poc = y; if (!je && poc >= 0) { if (y - poc >= h * 0.55) for (int k = poc; k < y; k++) brisi[k * w + x] = true; poc = -1; } } }
                for (int i = 0; i < ink.Length; i++) if (brisi[i]) ink[i] = false;
            }
            var uvecanje = h < 40 ? 3 : h < 70 ? 2 : 1;
            int rub = 12, W = w * uvecanje + 2 * rub, H = h * uvecanje + 2 * rub;
            using (var izlaz = new Bitmap(W, H, PixelFormat.Format24bppRgb))
            {
                var data = izlaz.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                try
                {
                    var red = new byte[data.Stride];
                    for (int y = 0; y < H; y++)
                    {
                        for (int x = 0; x < W; x++)
                        {
                            int sx = (x - rub) / uvecanje, sy = (y - rub) / uvecanje;
                            var crno = x >= rub && y >= rub && sx < w && sy < h && ink[sy * w + sx];
                            red[x * 3] = red[x * 3 + 1] = red[x * 3 + 2] = crno ? (byte)0 : (byte)255;
                        }
                        Marshal.Copy(red, 0, data.Scan0 + y * data.Stride, data.Stride);
                    }
                }
                finally { izlaz.UnlockBits(data); }
                izlaz.SetResolution(300, 300);
                using (var ms = new MemoryStream()) { izlaz.Save(ms, ImageFormat.Png); return ms.ToArray(); }
            }
        }

        public static Bitmap Ucitaj(byte[] png) { using (var ms = new MemoryStream(png)) return new Bitmap(new Bitmap(ms)); }

        // ------------------------------------------------------------------------------------------- crtanje za proveru

        private static readonly Color[] Boje =
        {
            Color.FromArgb(220, 30, 30), Color.FromArgb(30, 110, 220), Color.FromArgb(20, 150, 60), Color.FromArgb(200, 110, 0),
            Color.FromArgb(150, 40, 180), Color.FromArgb(0, 150, 160), Color.FromArgb(180, 140, 0), Color.FromArgb(200, 40, 130),
        };

        public static Color Boja(string id)
        {
            var h = 0; foreach (var ch in id ?? "") h = h * 31 + ch;
            return Boje[(h & 0x7fffffff) % Boje.Length];
        }

        /// <summary>Slika nacrta (uvećana 2×): numerisane ćelije okvira (c…), kvadratići (k…) i krugovi brojeva rubrika.</summary>
        public static byte[] Nacrt(Raster raster, List<Oblik> celije, List<Oblik> kvadratici, List<Oblik> krugovi)
        {
            int W = raster.W * 2, H = raster.H * 2;
            using (var mala = raster.UBitmap())
            using (var bmp = new Bitmap(W, H))
            using (var g = Graphics.FromImage(bmp))
            using (var font = new Font("Arial", 11, FontStyle.Bold))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(mala, 0, 0, W, H);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                for (int i = 0; i < celije.Count; i++)
                {
                    var c = celije[i]; var boja = Boja("c" + i);
                    using (var pen = new Pen(Color.FromArgb(140, boja), 2)) g.DrawRectangle(pen, (float)(c.X0 * W) + 3, (float)(c.Y0 * H) + 3, (float)((c.X1 - c.X0) * W) - 6, (float)((c.Y1 - c.Y0) * H) - 6);
                    var t = "c" + (i + 1);
                    var sz = g.MeasureString(t, font);
                    float x = (float)(c.Cx * W) - sz.Width / 2, y = (float)(c.Cy * H) - sz.Height / 2;
                    using (var br = new SolidBrush(Color.FromArgb(220, Color.White))) g.FillRectangle(br, x, y, sz.Width, sz.Height);
                    using (var br = new SolidBrush(boja)) g.DrawString(t, font, br, x, y);
                }
                for (int i = 0; i < kvadratici.Count; i++)
                {
                    var k = kvadratici[i];
                    using (var pen = new Pen(Color.DarkOrange, 3)) g.DrawRectangle(pen, (float)(k.X0 * W), (float)(k.Y0 * H), (float)((k.X1 - k.X0) * W), (float)((k.Y1 - k.Y0) * H));
                    g.DrawString("k" + (i + 1), font, Brushes.DarkOrange, (float)(k.X1 * W) + 2, (float)(k.Y0 * H) - 4);
                }
                foreach (var k in krugovi)
                    using (var pen = new Pen(Color.RoyalBlue, 2)) g.DrawEllipse(pen, (float)(k.X0 * W) - 2, (float)(k.Y0 * H) - 2, (float)((k.X1 - k.X0) * W) + 4, (float)((k.Y1 - k.Y0) * H) + 4);
                using (var ms = new MemoryStream()) { bmp.Save(ms, ImageFormat.Png); return ms.ToArray(); }
            }
        }

        /// <summary>
        /// Slika strane sa ucrtanim pravougaonicima rubrika i polja (preslikanim na stranu po poravnanju), rečima obojenim
        /// po dodeljenoj rubrici i sidrima (zeleno tekst, plavo brojevi, ljubičasto linije).
        /// </summary>
        public static byte[] Okviri(Raster raster, ObrazacSablon s, Poravnanje p, IEnumerable<(Rec rec, string rubrika, bool naziv)> reci, string naslov)
        {
            using (var bmp = raster.UBitmap())
            using (var g = Graphics.FromImage(bmp))
            using (var font = new Font("Arial", 9, FontStyle.Bold))
            using (var mali = new Font("Arial", 7))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int W = raster.W, H = raster.H;
                PointF Tacka(double tx, double ty) { var (x, y) = p.NaStranu(tx, ty); return new PointF((float)(x * W), (float)(y * H)); }
                void Pravougaonik(double[] r, Color boja, string oznaka, float debljina)
                {
                    var t = new[] { Tacka(r[0], r[1]), Tacka(r[2], r[1]), Tacka(r[2], r[3]), Tacka(r[0], r[3]) };
                    using (var pen = new Pen(boja, debljina)) g.DrawPolygon(pen, t);
                    using (var br = new SolidBrush(Color.FromArgb(235, Color.White))) g.FillRectangle(br, t[0].X + 1, t[0].Y + 1, g.MeasureString(oznaka, font).Width, 13);
                    using (var br = new SolidBrush(boja)) g.DrawString(oznaka, font, br, t[0].X + 1, t[0].Y);
                }
                if (s != null)
                {
                    foreach (var kv in s.Rubrike.OrderBy(k => k.Key, StringComparer.Ordinal)) Pravougaonik(kv.Value, Boja(kv.Key), kv.Key, 2f);
                    foreach (var kv in s.Polja.OrderBy(k => k.Key, StringComparer.Ordinal)) Pravougaonik(kv.Value, Color.DarkOrange, "□" + kv.Key, 1.5f);
                    foreach (var kv in s.Kucice.OrderBy(k => k.Key, StringComparer.Ordinal)) Pravougaonik(kv.Value, Color.DarkCyan, "#" + kv.Key, 1.5f);
                    using (var plav = new SolidBrush(Color.RoyalBlue))
                        foreach (var kv in s.Brojevi.OrderBy(k => k.Key, StringComparer.Ordinal))
                        {
                            var t = Tacka(kv.Value.x, kv.Value.y);
                            using (var pen = new Pen(Color.RoyalBlue, 1.5f)) g.DrawEllipse(pen, t.X - 8, t.Y - 8, 16, 16);
                            g.DrawString(kv.Key, mali, plav, t.X + 8, t.Y - 12);
                        }
                }
                foreach (var (rec, rub, naziv) in reci)
                {
                    var boja = naziv ? Color.FromArgb(160, 160, 160) : rub == null ? Color.Black : Boja(rub);
                    using (var pen = new Pen(boja, naziv ? 0.6f : 1.2f))
                        g.DrawRectangle(pen, (float)(rec.X0 * W), (float)(rec.Y0 * H), (float)Math.Max(1, (rec.X1 - rec.X0) * W), (float)Math.Max(1, (rec.Y1 - rec.Y0) * H));
                }
                if (p != null)
                    foreach (var q in p.Parovi)
                    {
                        var boja = q.Vrsta == "tekst" ? Color.LimeGreen : q.Vrsta == "broj" ? Color.RoyalBlue : Color.Magenta;
                        using (var br = new SolidBrush(boja))
                            g.FillEllipse(br, (float)(q.Px * W) - 3, (float)(q.Py * H) - 3, 6, 6);
                    }
                using (var br = new SolidBrush(Color.FromArgb(230, 255, 255, 210))) g.FillRectangle(br, 0, 0, W, 30);
                g.DrawString(naslov ?? "", font, Brushes.Black, 4, 2);
                if (p != null) g.DrawString(p.Opis, mali, Brushes.Black, 4, 16);
                using (var ms = new MemoryStream()) { bmp.Save(ms, ImageFormat.Png); return ms.ToArray(); }
            }
        }
    }
}
