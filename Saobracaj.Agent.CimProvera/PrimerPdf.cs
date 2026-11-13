using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>Rezultat pravljenja PDF primera (B2).</summary>
    internal sealed class PrimerRezultat
    {
        /// <summary>PDF primera, ili null kada primer nije napravljen (vidi <see cref="Razlog"/>).</summary>
        public byte[] Pdf;
        public string Ime = "";
        /// <summary>Zašto primer nije napravljen: "bez_nalaza", "neprepoznat", "bez_podloge", "greska".</summary>
        public string Razlog;
        public string Greska;
        /// <summary>Slike strana (PNG, 150 dpi): legenda, listovi, spisak kola – za proveru i testove.</summary>
        public List<byte[]> Strane = new List<byte[]>();
        /// <summary>Po listu: broj ispravki (crveno) i napomena (narandžasto) ucrtanih na strani lista.</summary>
        public List<(string list, int ispravki, int napomena, int samoULegendi)> PoListu = new List<(string, int, int, int)>();
        public bool Napravljen => Pdf != null;
    }

    /// <summary>
    /// PDF primer ispravnog tovarnog lista (B2): podloga je originalna strana lista (150 dpi), ispravne rubrike se ne diraju;
    /// rubrike sa greškom – crveni okvir, poluprovidna crvena podloga i ispravna vrednost (ili format iz pravila) crvenom bojom;
    /// rubrike sa upozorenjem/preporukom – narandžasti okvir sa brojem napomene. Prva strana: legenda i ista tabela kao u mailu.
    /// Na svakoj strani vodeni žig „PRIMER – NIJE VAŽEĆI TOVARNI LIST“ i traka u vrhu. Strane se crtaju preko GDI+ (font Arial,
    /// latinica sa dijakriticima i ćirilica) i slažu u PDF kao slike (PdfPig, Apache 2.0 – bez nove biblioteke).
    /// </summary>
    internal static class PrimerPdf
    {
        public const int Dpi = 150;
        public const long MaxBajtova = 5 * 1024 * 1024;
        private const string Traka = "Primer ispravnog popunjavanja – Kombinovani prevoz d.o.o., priprema@kprevoz.co.rs. Ispravljen tovarni list izdaje pošiljalac.";
        private const string TrakaEn = "Sample of correct completion – Kombinovani prevoz d.o.o., priprema@kprevoz.co.rs. The corrected consignment note is issued by the consignor.";
        private const string TekstZiga = "PRIMER – NIJE VAŽEĆI TOVARNI LIST";
        private static readonly Color Crvena = Color.FromArgb(200, 0, 0);
        private static readonly Color Narandzasta = Color.FromArgb(230, 120, 0);

        /// <summary>Ime fajla: PRIMER_&lt;r62 bez razmaka&gt;.pdf (npr. PRIMER_72165506321215690-0.pdf), inače PRIMER_&lt;ime fajla&gt;.pdf.</summary>
        public static string ImeFajla(RezultatProvere r)
        {
            var l = r.Listovi.FirstOrDefault();
            var i = l?.Cim?.R62;
            // Excel/Word obrazac: broj otpravljanja može biti alfanumerički („HER-SAD79“)
            var alfa = l?.Cim?.Sablon == "TABELA" && Regex.IsMatch(Regex.Replace(i?.Broj ?? "", @"\s", ""), @"^[A-Za-z0-9][A-Za-z0-9\-]{3,15}$");
            if (i != null && Regex.IsMatch(i.Zemlja ?? "", @"^\d{2}$") && Regex.IsMatch(i.Preduzece ?? "", @"^\d{4}$")
                && Regex.Replace(i.Stanica ?? "", @"\D", "").Length >= 5 && (alfa || Regex.Replace(i.Broj ?? "", @"\D", "").Length >= 5))
            {
                var broj = Regex.Replace(i.Broj, @"\s", "");
                if (!broj.Contains("-") && broj.Length == 6) broj = broj.Substring(0, 5) + "-" + broj.Substring(5);
                return "PRIMER_" + i.Zemlja + Regex.Replace(i.Stanica, @"\D", "") + i.Preduzece + broj + ".pdf";
            }
            var f = Path.GetFileNameWithoutExtension((l?.Dokument?.Fajl ?? "tovarni_list").Split('/', '\\').Last());
            return "PRIMER_" + Regex.Replace(f, @"[^\w\-\. ]", "_") + ".pdf";
        }

        /// <summary>Primer iz priloga maila (ZIP/MSG se raspakuju; podloga se traži po imenu fajla dokumenta).</summary>
        public static PrimerRezultat Napravi(RezultatProvere r, PravilaSkup p, bool en, IEnumerable<(string ime, byte[] sadrzaj)> prilozi)
        {
            var svi = new List<Prilog>();
            foreach (var (ime, b) in prilozi ?? Enumerable.Empty<(string, byte[])>())
                try { svi.AddRange(Raspakivanje.Raspakuj(ime, b, new List<string>())); } catch { }
            Prilog Nadji(string fajl)
            {
                var ime = Path.GetFileName((fajl ?? "").Split('/', '\\').Last());
                return svi.FirstOrDefault(x => string.Equals(x.Naziv, fajl, StringComparison.OrdinalIgnoreCase))
                    ?? svi.FirstOrDefault(x => string.Equals(Path.GetFileName((x.Naziv ?? "").Split('/', '\\').Last()), ime, StringComparison.OrdinalIgnoreCase));
            }
            return Napravi(r, p, en, Nadji);
        }

        /// <param name="podloga">Original priloga po imenu fajla (Dokument.Fajl): sadržaj i vrsta; null ako nije dostupan.</param>
        public static PrimerRezultat Napravi(RezultatProvere r, PravilaSkup p, bool en, Func<string, Prilog> podloga)
        {
            var rez = new PrimerRezultat { Ime = ImeFajla(r) };
            try
            {
                if (r.BrojGresaka + r.BrojUpozorenja == 0) { rez.Razlog = "bez_nalaza"; return rez; }
                // PDF/sken: okviri iz poravnatog šablona; Excel/Word (TABELA): list se crta iz ćelija, okviri iz brojeva rubrika
                var listovi = r.Listovi.Where(l => l.Cim != null && string.IsNullOrEmpty(l.Cim.NeprepoznatObrazac) && (l.Cim.Okviri.Count > 0 || l.Cim.Sablon == "TABELA")).ToList();
                if (listovi.Count == 0) { rez.Razlog = "neprepoznat"; return rez; }

                var ispravke = Izvestaj.RedoviIspravki(r, p, en);
                var napomene = Izvestaj.RedoviNapomena(r, p, en);
                var redovi = ispravke.Select(x => (red: x, greska: true)).Concat(napomene.Select(x => (red: x, greska: false))).ToList();

                var strane = new List<Bitmap>();
                try
                {
                    strane.AddRange(Legenda(r, p, en, redovi, rez.Ime));
                    foreach (var l in listovi)
                    {
                        var pr = podloga(l.Dokument.Fajl);
                        var okviri = l.Cim.Okviri;
                        var bmp = l.Cim.Sablon == "TABELA" ? PodlogaTabele(pr, out okviri)
                            : Podloga(pr, l.Cim.StranaLista > 0 ? l.Cim.StranaLista : l.Dokument.Strane.FirstOrDefault());
                        if (bmp == null) continue;
                        var (isp, nap, leg) = Ucrtaj(bmp, l, redovi, en, r, okviri);
                        rez.PoListu.Add((l.Oznaka, isp, nap, leg));
                        strane.Add(Oznaci(bmp, en));
                    }
                    if (strane.Count == 1) { rez.Razlog = "bez_podloge"; return rez; }
                    // spisak kola (CIT 23) samo kada na njemu ima nalaza (X19, X21, X29)
                    var naSpisku = redovi.Where(x => Regex.IsMatch(x.red.N.Pravilo, @"^X(19|21|29)") || x.red.N.Rubrika == "CIT 23").ToList();
                    if (naSpisku.Count > 0)
                        foreach (var d in r.Dokumenti.Where(d => d.Tip == TipDokumenta.SpisakKola && d.Izvor != "excel"))
                        {
                            var pr = podloga(d.Fajl);
                            foreach (var br in d.Strane.Take(3))
                            {
                                var bmp = Podloga(pr, br);
                                if (bmp == null) continue;
                                UcrtajSpisak(bmp, naSpisku, redovi, en);
                                strane.Add(Oznaci(bmp, en));
                            }
                        }
                    foreach (var s in strane) rez.Strane.Add(Png(s));
                    rez.Pdf = Pdf(strane, 85);
                    if (rez.Pdf.Length > MaxBajtova) rez.Pdf = Pdf(strane, 60);
                    if (rez.Pdf.Length > MaxBajtova) { rez.Pdf = null; rez.Razlog = "greska"; rez.Greska = "PDF primer je veći od 5 MB"; }
                }
                finally { foreach (var s in strane) s.Dispose(); }
            }
            catch (Exception ex) { rez.Pdf = null; rez.Razlog = "greska"; rez.Greska = ex.Message; }
            return rez;
        }

        // ------------------------------------------------------------------------------------------- podloga i oznake

        private static Bitmap Podloga(Prilog p, int strana)
        {
            if (p == null || p.Sadrzaj == null) return null;
            byte[] slika = p.Vrsta == VrstaPriloga.Pdf ? PdfCrtac.StranaUPng(p.Sadrzaj, Math.Max(1, strana), Dpi) : p.Sadrzaj;
            using (var ms = new MemoryStream(slika))
            using (var img = Image.FromStream(ms))
            {
                var bmp = new Bitmap(img.Width, img.Height, PixelFormat.Format24bppRgb);
                bmp.SetResolution(Dpi, Dpi);
                using (var g = Graphics.FromImage(bmp)) { g.Clear(Color.White); g.DrawImage(img, 0, 0, img.Width, img.Height); }
                return bmp;
            }
        }

        /// <summary>
        /// Podloga za tovarni list iz Excela/Worda: list obrasca nacrtan iz ćelija (širine kolona, visine vrsta, spojene ćelije),
        /// sa sivim okvirom svake rubrike (od broja rubrike do sledećeg broja). Okviri rubrika se vraćaju normalizovano (0–1).
        /// </summary>
        private static Bitmap PodlogaTabele(Prilog p, out Dictionary<string, double[]> okviri)
        {
            okviri = null;
            if (p?.Sadrzaj == null) return null;
            List<(List<Celija> celije, TabelarniCim.Raspored raspored)> listovi = null;
            if (p.Vrsta == VrstaPriloga.Excel) listovi = TabelarniCim.IzExcelaSaRasporedom(p.Sadrzaj);
            else if (p.Vrsta == VrstaPriloga.Word)
                listovi = TabelarniCim.IzWorda(p.Sadrzaj).Select(c =>
                {
                    var ras = new TabelarniCim.Raspored();
                    int nK = c.Count == 0 ? 1 : c.Max(x => x.Kol) + 1, nR = c.Count == 0 ? 1 : c.Max(x => x.Red) + 1;
                    for (int k = 0; k < nK; k++) ras.Sirine.Add(nK <= 6 ? 120 : 40);
                    for (int i = 0; i < nR; i++) ras.Visine.Add(11 * Math.Max(1, c.Where(x => x.Red == i).Select(x => x.Tekst.Split('\n').Length).DefaultIfEmpty(1).Max()) + 3);
                    return (c, ras);
                }).ToList();
            var izbor = listovi?.FirstOrDefault(x => x.celije.Count > 0 && TabelarniCim.JeTovarniList(x.celije, out _));
            if (izbor?.celije == null) return null;
            return NacrtajTabelu(izbor.Value.celije, izbor.Value.raspored, out okviri);
        }

        private static Bitmap NacrtajTabelu(List<Celija> celije, TabelarniCim.Raspored ras, out Dictionary<string, double[]> okviri)
        {
            var oblasti = TabelarniCim.Oblasti(celije);
            int nR = celije.Max(c => c.Red) + 1, nK = celije.Max(c => c.Kol) + 1;
            // prazne/skrivene vrednosti: ako nijedna širina/visina nije zadata, mreža je ravnomerna
            bool imaS = ras.Sirine.Any(x => x > 0), imaV = ras.Visine.Any(x => x > 0);
            double Sir(int k) => !imaS ? 40 : k < ras.Sirine.Count ? Math.Max(0, ras.Sirine[k]) : 48;
            double Vis(int i) => !imaV ? 15 : i < ras.Visine.Count ? Math.Max(0, ras.Visine[i]) : 15;
            var xs = new double[nK + 1]; for (int k = 0; k < nK; k++) xs[k + 1] = xs[k] + Sir(k);
            var ys = new double[nR + 1]; for (int i = 0; i < nR; i++) ys[i + 1] = ys[i] + Vis(i);
            double sw = Math.Max(1, xs[nK]), sh = Math.Max(1, ys[nR]);
            bool polozeno = sw > sh * 1.15;
            int W = polozeno ? 1754 : 1240, H = polozeno ? 1240 : 1754;
            const float m = 40f;
            float s = (float)Math.Min((W - 2 * m) / sw, (H - 2 * m) / sh);
            RectangleF R(int r0, int k0, int r1, int k1)
            {
                r0 = Math.Min(r0, nR); r1 = Math.Min(Math.Max(r1, r0), nR); k0 = Math.Min(k0, nK); k1 = Math.Min(Math.Max(k1, k0), nK);
                return new RectangleF(m + (float)(xs[k0] * s), m + (float)(ys[r0] * s), (float)((xs[k1] - xs[k0]) * s), (float)((ys[r1] - ys[r0]) * s));
            }

            var bmp = new Bitmap(W, H, PixelFormat.Format24bppRgb);
            bmp.SetResolution(Dpi, Dpi);
            okviri = new Dictionary<string, double[]>();
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                using (var pen = new Pen(Color.FromArgb(110, 110, 110), 1.2f))
                    foreach (var kv in oblasti)
                    {
                        var o = R(kv.Value.r0, kv.Value.k0, kv.Value.r1, kv.Value.k1);
                        g.DrawRectangle(pen, o.X, o.Y, o.Width, o.Height);
                        okviri[kv.Key] = new[] { (double)o.Left / W, (double)o.Top / H, (double)o.Right / W, (double)o.Bottom / H };
                    }
                // tekst ćelija: spojena ćelija – ceo opseg; inače do sledeće popunjene ćelije u vrsti (kao Excel), višelinijski tekst i naniže
                var zauzeto = new HashSet<(int, int)>(celije.Select(c => (c.Red, c.Kol)));
                var sidra = new HashSet<(int, int)>(oblasti.Values.Select(o => (o.r0, o.k0)));
                float fv = Math.Max(9f, 7.5f * s), fs = Math.Max(10f, 8.5f * s);
                using (var obican = new Font("Arial", fv, GraphicsUnit.Pixel))
                using (var debeo = new Font("Arial", fs, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var crna = new SolidBrush(Color.FromArgb(30, 30, 30)))
                using (var siva = new SolidBrush(Color.FromArgb(90, 90, 90)))
                    foreach (var c in celije)
                    {
                        var sp = ras.Spojene.Where(x => x.r0 == c.Red && x.k0 == c.Kol).ToList();
                        int r1, k1;
                        if (sp.Count > 0) { r1 = sp[0].r1 + 1; k1 = sp[0].k1 + 1; }
                        else
                        {
                            k1 = c.Kol + 1; while (k1 < nK && !zauzeto.Contains((c.Red, k1))) k1++;
                            r1 = c.Red + 1;
                            var linija = c.Tekst.Split('\n').Length;
                            while (r1 < nR && r1 < c.Red + linija && !zauzeto.Contains((r1, c.Kol))) r1++;
                        }
                        var o = R(c.Red, c.Kol, r1, k1);
                        if (o.Width < 2 || o.Height < 1) continue;
                        var sidro = sidra.Contains((c.Red, c.Kol));
                        var f = sidro ? debeo : obican;
                        var jedanRed = o.Height < f.Height * 1.9f;
                        var pr = new RectangleF(o.X + 1.5f, o.Y + 0.5f, o.Width - 2f, Math.Max(o.Height, f.Height * 1.1f));
                        using (var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = jedanRed ? StringFormatFlags.NoWrap : StringFormatFlags.LineLimit })
                        {
                            var tekst = jedanRed ? Regex.Replace(c.Tekst, @"\s*\n\s*", " ") : c.Tekst;
                            g.DrawString(tekst, f, sidro || Regex.IsMatch(c.Tekst, @"^\d$") ? crna : siva, pr, sf);
                        }
                    }
            }
            return bmp;
        }

        /// <summary>Okvir rubrike na strani (piksel): spoj svih okvira šablona za rubriku („30“ → „30cim“ + „30cuv“).</summary>
        private static RectangleF? Okvir(Dictionary<string, double[]> okviri, string rubrika, int w, int h)
        {
            if (okviri == null || string.IsNullOrEmpty(rubrika) || !Regex.IsMatch(rubrika, @"^\d")) return null;
            var kljucevi = okviri.Keys.Where(k => k == rubrika || Regex.IsMatch(k, "^" + Regex.Escape(rubrika) + @"(?![\dab])")).ToList();
            if (kljucevi.Count == 0) return null;
            var o = kljucevi.Select(k => okviri[k]).ToList();
            float x0 = (float)(o.Min(v => v[0]) * w), y0 = (float)(o.Min(v => v[1]) * h), x1 = (float)(o.Max(v => v[2]) * w), y1 = (float)(o.Max(v => v[3]) * h);
            return new RectangleF(x0, y0, Math.Max(10, x1 - x0), Math.Max(10, y1 - y0));
        }

        private static float Px(float pt) => pt * Dpi / 72f;

        /// <summary>Tekst ispravke na strani lista: bez izvora u zagradi sa imenom fajla (izvor ostaje u legendi).</summary>
        internal static string ZaStranu(string primer) => Regex.Replace(primer ?? "", @"\s*\([^()]*\.(pdf|png|jpe?g|tiff?|xlsx?)[^()]*\)", "", RegexOptions.IgnoreCase).Trim();

        /// <summary>Ucrtava nalaze lista. Vraća broj ispravki i napomena na strani i broj nalaza koji su samo u legendi.</summary>
        private static (int ispravki, int napomena, int legenda) Ucrtaj(Bitmap bmp, RezultatLista l, List<(Izvestaj.Red red, bool greska)> redovi, bool en, RezultatProvere r, Dictionary<string, double[]> okviri)
        {
            int isp = 0, nap = 0, leg = 0;
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                var badgeovi = new Dictionary<string, int>();
                for (int i = 0; i < redovi.Count; i++)
                {
                    var (red, greska) = redovi[i];
                    if (r.Listovi.Count > 1 && red.Listovi.Count > 0 && !red.Listovi.Contains(l.Oznaka)) continue;
                    var okv = Okvir(okviri, red.N.Rubrika, bmp.Width, bmp.Height);
                    if (okv == null) { leg++; continue; }
                    var o = okv.Value;
                    var boja = greska ? Crvena : Narandzasta;
                    var rb = i + 1;
                    if (greska)
                    {
                        using (var br = new SolidBrush(Color.FromArgb(38, Crvena))) g.FillRectangle(br, o);
                        using (var pen = new Pen(Crvena, 2.5f)) g.DrawRectangle(pen, o.X, o.Y, o.Width, o.Height);
                        Upisi(g, o, ZaStranu(red.Primer), Crvena, rb);
                        isp++;
                    }
                    else
                    {
                        using (var pen = new Pen(Narandzasta, 3f) { DashStyle = DashStyle.Solid }) g.DrawRectangle(pen, o.X, o.Y, o.Width, o.Height);
                        // siguran predlog (NHM sa 6 cifara iz T1/JCI/spiska) se upisuje; ostalo samo okvir i broj napomene
                        if (red.N.Pravilo == "R24-NHM6" && Regex.IsMatch(red.Primer ?? "", @"^\d{6}$")) Upisi(g, o, red.Primer, Narandzasta, rb);
                        nap++;
                    }
                    var n = badgeovi.TryGetValue(red.N.Rubrika, out var x) ? x : 0;
                    badgeovi[red.N.Rubrika] = n + 1;
                    Znacka(g, o.Right - Px(9) - n * Px(10), o.Y + Px(1), rb, boja);
                }
            }
            return (isp, nap, leg);
        }

        /// <summary>Ispravna vrednost (ili format) u donjem delu rubrike, na beloj podlozi; font 9 → 6 pt, inače „vidi (n)“.</summary>
        private static void Upisi(Graphics g, RectangleF o, string tekst, Color boja, int rb)
        {
            tekst = (tekst ?? "").Trim();
            if (tekst.Length == 0) tekst = "vidi (" + rb + ")";
            var sirina = o.Width - Px(4);
            var maxVisina = Math.Max(Px(8), o.Height * 0.62f);
            for (var pt = 9f; ; pt -= 0.5f)
            {
                using (var f = new Font("Arial", Px(pt), FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    var vel = g.MeasureString(tekst, f, (int)sirina);
                    if (vel.Height <= maxVisina || pt <= 6f)
                    {
                        if (vel.Height > maxVisina) { tekst = "vidi (" + rb + ")"; vel = g.MeasureString(tekst, f, (int)sirina); }
                        var pravougaonik = new RectangleF(o.X + Px(2), o.Bottom - vel.Height - Px(1.5f), sirina, vel.Height + Px(0.5f));
                        using (var b = new SolidBrush(Color.FromArgb(225, Color.White))) g.FillRectangle(b, pravougaonik);
                        using (var b = new SolidBrush(boja)) g.DrawString(tekst, f, b, pravougaonik);
                        return;
                    }
                }
            }
        }

        private static void Znacka(Graphics g, float x, float y, int rb, Color boja)
        {
            var d = Px(9);
            using (var b = new SolidBrush(boja)) g.FillEllipse(b, x, y, d, d);
            using (var f = new Font("Arial", Px(rb > 9 ? 5.5f : 6.5f), FontStyle.Bold, GraphicsUnit.Pixel))
            using (var b = new SolidBrush(Color.White))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(rb.ToString(), f, b, new RectangleF(x, y, d, d), sf);
        }

        /// <summary>Spisak kola: okvir oko cele strane i brojevi napomena u levom gornjem uglu.</summary>
        private static void UcrtajSpisak(Bitmap bmp, List<(Izvestaj.Red red, bool greska)> naSpisku, List<(Izvestaj.Red red, bool greska)> svi, bool en)
        {
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var greska = naSpisku.Any(x => x.greska);
                using (var pen = new Pen(greska ? Crvena : Narandzasta, 4f)) g.DrawRectangle(pen, Px(6), Px(6), bmp.Width - Px(12), bmp.Height - Px(12));
                float x = Px(10);
                foreach (var (red, gr) in naSpisku) { Znacka(g, x, Px(10), svi.FindIndex(s => s.red == red) + 1, gr ? Crvena : Narandzasta); x += Px(11); }
                using (var f = new Font("Arial", Px(8), FontStyle.Bold, GraphicsUnit.Pixel))
                using (var b = new SolidBrush(greska ? Crvena : Narandzasta))
                    g.DrawString(en ? "Wagon list – see the notes on page 1" : "Spisak kola – vidi napomene na prvoj strani", f, b, x + Px(2), Px(10));
            }
        }

        /// <summary>Traka u vrhu (dodata iznad originala, ništa ne prekriva) i dijagonalni vodeni žig preko cele strane.</summary>
        private static Bitmap Oznaci(Bitmap original, bool en)
        {
            var traka = (int)Px(22);
            var bmp = new Bitmap(original.Width, original.Height + traka, PixelFormat.Format24bppRgb);
            bmp.SetResolution(Dpi, Dpi);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.White);
                g.DrawImage(original, 0, traka, original.Width, original.Height);
                using (var b = new SolidBrush(Color.FromArgb(255, 240, 240, 240))) g.FillRectangle(b, 0, 0, bmp.Width, traka);
                using (var pen = new Pen(Color.FromArgb(160, 160, 160), 1f)) g.DrawLine(pen, 0, traka - 1, bmp.Width, traka - 1);
                using (var f = new Font("Arial", Px(8.5f), FontStyle.Bold, GraphicsUnit.Pixel))
                using (var b = new SolidBrush(Color.FromArgb(70, 70, 70)))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString(en ? TrakaEn : Traka, f, b, new RectangleF(Px(6), 0, bmp.Width - Px(12), traka), sf);
                Zig(g, bmp.Width, bmp.Height, traka, en);
            }
            original.Dispose();
            return bmp;
        }

        private static void Zig(Graphics g, int w, int h, int odVrha, bool en)
        {
            var tekst = en ? "SAMPLE – NOT A VALID CONSIGNMENT NOTE" : TekstZiga;
            var dijagonala = (float)Math.Sqrt(w * (double)w + (h - odVrha) * (double)(h - odVrha));
            var ugao = (float)(-Math.Atan2(h - odVrha, w) * 180 / Math.PI);
            var st = g.Save();
            g.TranslateTransform(w / 2f, odVrha + (h - odVrha) / 2f);
            g.RotateTransform(ugao);
            using (var f = new Font("Arial", dijagonala / tekst.Length * 1.45f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var b = new SolidBrush(Color.FromArgb(55, 150, 150, 150)))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(tekst, f, b, 0, 0, sf);
            g.Restore(st);
        }

        // ------------------------------------------------------------------------------------------- legenda (prva strana)

        private static List<Bitmap> Legenda(RezultatProvere r, PravilaSkup p, bool en, List<(Izvestaj.Red red, bool greska)> redovi, string ime)
        {
            int W = (int)(8.27 * Dpi), H = (int)(11.69 * Dpi);
            var strane = new List<Bitmap>();
            Bitmap bmp = null; Graphics g = null; float y = 0;
            var m = Px(36);
            Font naslov = new Font("Arial", Px(14), FontStyle.Bold, GraphicsUnit.Pixel), obican = new Font("Arial", Px(8.5f), GraphicsUnit.Pixel),
                 debeo = new Font("Arial", Px(8.5f), FontStyle.Bold, GraphicsUnit.Pixel);
            var kol = new[] { Px(26), Px(110), Px(200), 0f };
            kol[3] = W - 2 * m - kol[0] - kol[1] - kol[2];
            void NovaStrana()
            {
                g?.Dispose();
                bmp = new Bitmap(W, H, PixelFormat.Format24bppRgb); bmp.SetResolution(Dpi, Dpi);
                g = Graphics.FromImage(bmp); g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                strane.Add(bmp); y = m;
            }
            void Tekst(string t, Font f, Color c, float x, float sirina)
            {
                var v = g.MeasureString(t, f, (int)sirina);
                using (var b = new SolidBrush(c)) g.DrawString(t, f, b, new RectangleF(x, y, sirina, v.Height + 2));
                y += v.Height + Px(3);
            }
            try
            {
                NovaStrana();
                Tekst(en ? "Sample of the correctly completed consignment note" : "Primer ispravno popunjenog tovarnog lista", naslov, Color.Black, m, W - 2 * m);
                var id = Izvestaj.Identifikacija(r, en);
                Tekst((id.Length > 0 ? id + " · " : "") + ime, obican, Color.FromArgb(80, 80, 80), m, W - 2 * m);
                y += Px(4);
                // legenda boja
                void Boja(Color c, bool puna, string t)
                {
                    var kv = new RectangleF(m, y + Px(1), Px(18), Px(9));
                    if (puna) using (var b = new SolidBrush(Color.FromArgb(38, c))) g.FillRectangle(b, kv);
                    if (c != Color.Empty) using (var pen = new Pen(c, 2f)) g.DrawRectangle(pen, kv.X, kv.Y, kv.Width, kv.Height);
                    else using (var pen = new Pen(Color.Gray, 1f)) g.DrawRectangle(pen, kv.X, kv.Y, kv.Width, kv.Height);
                    var y0 = y; using (var b = new SolidBrush(Color.Black)) g.DrawString(t, obican, b, m + Px(24), y0); y += Px(13);
                }
                Boja(Crvena, true, en ? "red = correction (correct value or required format written in red)" : "crveno = ispravka (ispravna vrednost ili propisani oblik upisan crvenom bojom)");
                Boja(Narandzasta, false, en ? "orange = please check (note number in the circle)" : "narandžasto = proverite (broj napomene u krugu)");
                Boja(Color.Empty, false, en ? "unchanged = correct, copied from your document" : "nepromenjeno = ispravno, prepisano iz Vašeg dokumenta");
                y += Px(6);

                void Zaglavlje(string t, Color c)
                {
                    if (y > H - Px(80)) NovaStrana();
                    Tekst(t, debeo, c, m, W - 2 * m);
                    var kolone = en ? new[] { "No.", "Box", "Issue", "Correct entry (example)" } : new[] { "Rb.", "Rubrika", "Šta nije u redu", "Kako treba da izgleda (primer)" };
                    Red(kolone, debeo, Color.FromArgb(217, 217, 217), Color.Black);
                }
                void Red(string[] celije, Font f, Color pozadina, Color boja)
                {
                    var visina = celije.Select((t, i) => g.MeasureString(t ?? "", f, (int)(kol[i] - Px(6))).Height).Max() + Px(5);
                    if (y + visina > H - m) NovaStrana();
                    float x = m;
                    for (int i = 0; i < 4; i++)
                    {
                        var rc = new RectangleF(x, y, kol[i], visina);
                        if (pozadina != Color.Empty) using (var b = new SolidBrush(pozadina)) g.FillRectangle(b, rc);
                        using (var pen = new Pen(Color.FromArgb(153, 153, 153), 1f)) g.DrawRectangle(pen, rc.X, rc.Y, rc.Width, rc.Height);
                        using (var b = new SolidBrush(i == 0 ? boja : Color.Black)) g.DrawString(celije[i] ?? "", f, b, new RectangleF(x + Px(3), y + Px(2.5f), kol[i] - Px(6), visina));
                        x += kol[i];
                    }
                    y += visina;
                }
                var isp = redovi.Where(x => x.greska).ToList();
                var nap = redovi.Where(x => !x.greska).ToList();
                if (isp.Count > 0)
                {
                    Zaglavlje(en ? "CORRECTIONS REQUIRED" : "POTREBNE ISPRAVKE", Crvena);
                    foreach (var x in isp) Red(new[] { (redovi.IndexOf(x) + 1).ToString(), x.red.Rubrika, x.red.Sta, x.red.Primer }, obican, Color.Empty, Crvena);
                    y += Px(8);
                }
                if (nap.Count > 0)
                {
                    Zaglavlje(en ? "REMARKS (please check)" : "NAPOMENE (molimo proverite)", Narandzasta);
                    foreach (var x in nap) Red(new[] { (redovi.IndexOf(x) + 1).ToString(), x.red.Rubrika, x.red.Sta, x.red.Primer }, obican, Color.Empty, Narandzasta);
                    y += Px(8);
                }
                if (y > H - Px(60)) NovaStrana();
                Tekst(en ? "The sample is for guidance only – the corrected consignment note must be issued and signed by the consignor. No stamp or signature is added to the sample."
                         : "Primer služi samo kao pomoć pri ispravci – ispravan tovarni list izdaje i potpisuje pošiljalac. Na primer se ne stavlja pečat ni potpis.", obican, Color.FromArgb(80, 80, 80), m, W - 2 * m);
            }
            finally { g?.Dispose(); naslov.Dispose(); obican.Dispose(); debeo.Dispose(); }
            return strane.Select(s => Oznaci(s, en)).ToList();
        }

        // ------------------------------------------------------------------------------------------- PDF

        private static byte[] Png(Bitmap b) { using (var ms = new MemoryStream()) { b.Save(ms, ImageFormat.Png); return ms.ToArray(); } }

        private static byte[] Jpeg(Bitmap b, long kvalitet)
        {
            var enc = ImageCodecInfo.GetImageEncoders().First(e => e.FormatID == ImageFormat.Jpeg.Guid);
            using (var ep = new EncoderParameters(1))
            using (var ms = new MemoryStream())
            {
                ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, kvalitet);
                b.Save(ms, enc, ep);
                return ms.ToArray();
            }
        }

        /// <summary>Strane kao slike (JPEG) u PDF, širina A4 (595 pt), visina po razmeri strane.</summary>
        private static byte[] Pdf(List<Bitmap> strane, long kvalitet)
        {
            using (var b = new UglyToad.PdfPig.Writer.PdfDocumentBuilder())
            {
                foreach (var s in strane)
                {
                    double w = 595, h = 595.0 * s.Height / s.Width;
                    var str = b.AddPage(w, h);
                    str.AddJpeg(Jpeg(s, kvalitet), new UglyToad.PdfPig.Core.PdfRectangle(0, 0, w, h));
                }
                b.DocumentInformation.Title = "Primer ispravnog tovarnog lista";
                b.DocumentInformation.Producer = "CimProvera – Kombinovani prevoz d.o.o.";
                return b.Build();
            }
        }
    }
}
