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
        /// <summary>Slike strana (PNG, 150 dpi): listovi i spisak kola – za proveru i testove (v2.8.1: bez strane sa legendom).</summary>
        public List<byte[]> Strane = new List<byte[]>();
        /// <summary>Po listu: broj ispravki (crveno) i napomena (narandžasto) ucrtanih na strani lista; nalazi bez rubrike na strani (samo u tabeli maila).</summary>
        public List<(string list, int ispravki, int napomena, int bezRubrike)> PoListu = new List<(string, int, int, int)>();
        /// <summary>v2.8.1: šta je upisano u koju rubriku (za proveru izgleda i testove).</summary>
        public List<Upis> Upisi = new List<Upis>();
        /// <summary>v3.0 (2.9.3 tačka 2): INTERNO napomene („poravnanje strane 2 nepouzdano“).</summary>
        public List<string> Interno = new List<string>();
        public bool Napravljen => Pdf != null;
    }

    /// <summary>Jedan nalaz ucrtan na strani lista (v2.8.1).</summary>
    internal sealed class Upis
    {
        public string List = "", Rubrika = "", Pravilo = "";
        /// <summary>Broj u krugu – isti kao u tabeli maila (ispravke i napomene se broje posebno).</summary>
        public int Broj;
        public bool Greska;
        public string Dopuna = "", Pogresno = "";
        /// <summary>„ispod teksta“, „desno od teksta“, „pored precrtanog“, „delom na margini“, „na margini“, „samo oznaka“.</summary>
        public string Mesto = "";
        public float Pt;
        /// <summary>Krug (piksel, na strani pre trake na vrhu) i okvir upisane dopune.</summary>
        public RectangleF Krug, Tekst, Okvir;
        public override string ToString() => List + " r" + Rubrika + " (" + (Greska ? "ispravka " : "napomena ") + Broj + "): " + (Dopuna.Length > 0 ? "„" + Dopuna + "“ – " + Mesto : Mesto);
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

                // v2.8.1: brojevi u krugovima = brojevi u tabelama maila (ispravke 1…n crveno, napomene 1…m narandžasto);
                // nalazi „samo interno“ se u primeru ne prikazuju
                var ispravke = Izvestaj.RedoviIspravki(r, p, en);
                var napomene = Izvestaj.RedoviNapomena(r, p, en);
                var redovi = ispravke.Select((x, i) => new Stavka { Red = x, Greska = true, Broj = x.Broj > 0 ? x.Broj : i + 1 })
                    .Concat(napomene.Select((x, i) => new Stavka { Red = x, Greska = false, Broj = x.Broj > 0 ? x.Broj : i + 1 }))
                    .Where(x => !x.Red.N.SamoInterno).ToList();

                var strane = new List<Bitmap>();
                try
                {
                    // v2.8.1: bez strane sa legendom i tabelom (tabela je u mailu) – samo strane lista sa upisanim ispravkama
                    foreach (var l in listovi)
                    {
                        var pr = podloga(l.Dokument.Fajl);
                        var okviri = l.Cim.Okviri;
                        var bmp = l.Cim.Sablon == "TABELA" ? PodlogaTabele(pr, out okviri)
                            : Podloga(pr, l.Cim.StranaLista > 0 ? l.Cim.StranaLista : l.Dokument.Strane.FirstOrDefault());
                        if (bmp == null) continue;
                        // v3.0 (2.9.3 tačka 2): nijedna oznaka ni tekst van strane – okviri koji izlaze se uklapaju u stranu, uz INTERNO napomenu
                        if (okviri != null && Prepust(okviri) > ObrasciSkup.DozvoljenPrepust)
                        {
                            var stranaPrimera = strane.Count + 1;
                            var tekst = "poravnanje strane " + stranaPrimera + " nepouzdano (" + System.IO.Path.GetFileName((l.Dokument?.Fajl ?? "").Split('/', '\\').Last())
                                + (l.Cim.StranaLista > 0 ? ", str. " + l.Cim.StranaLista : "") + ") – okviri rubrika izlazili su " + (Prepust(okviri) * 100).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                                + " % van strane; oznake su pomerene na stranu, proveriti PRIMER pre slanja";
                            okviri = UStranu(okviri);
                            rez.Interno.Add(tekst);
                            if (!l.Nalazi.Any(x => x.Pravilo == "PRIMER-PORAVNANJE"))
                                l.Nalazi.Add(new Nalaz { Pravilo = "PRIMER-PORAVNANJE", Rubrika = "", Tezina = Tezina.Info, TezinaPravila = Tezina.Info, SamoInterno = true, Tekst = tekst, Detalj = tekst });
                        }
                        var (isp, nap, bez) = Ucrtaj(bmp, l, redovi, en, r, okviri, rez.Upisi, out var naMargini);
                        rez.PoListu.Add((l.Oznaka, isp, nap, bez));
                        strane.Add(Oznaci(bmp, en, naMargini));
                    }
                    if (strane.Count == 0) { rez.Razlog = "bez_podloge"; return rez; }
                    // spisak kola (CIT 23) samo kada na njemu ima nalaza (X19, X21, X29)
                    var naSpisku = redovi.Where(x => Regex.IsMatch(x.Red.N.Pravilo, @"^X(19|21|29)") || x.Red.N.Rubrika == "CIT 23").ToList();
                    if (naSpisku.Count > 0)
                        foreach (var d in r.Dokumenti.Where(d => d.Tip == TipDokumenta.SpisakKola && d.Izvor != "excel"))
                        {
                            var pr = podloga(d.Fajl);
                            foreach (var br in d.Strane.Take(3))
                            {
                                var bmp = Podloga(pr, br);
                                if (bmp == null) continue;
                                UcrtajSpisak(bmp, naSpisku, en);
                                strane.Add(Oznaci(bmp, en, null));
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

        /// <summary>Najveći prepust okvira rubrika van strane (normalizovane koordinate 0–1).</summary>
        internal static double Prepust(Dictionary<string, double[]> okviri) =>
            okviri == null || okviri.Count == 0 ? 0 : Math.Max(0, okviri.Values.Max(o => Math.Max(Math.Max(-o[0], o[2] - 1), Math.Max(-o[1], o[3] - 1))));

        /// <summary>
        /// v3.0 (2.9.3 tačka 2): okviri rubrika koji izlaze van strane – zajednički pomak i razmera tako da svi budu na strani (margina 1 %).
        /// Relativni raspored rubrika ostaje isti (oznake ostaju uz svoje rubrike, uz ivicu strane).
        /// </summary>
        internal static Dictionary<string, double[]> UStranu(Dictionary<string, double[]> okviri)
        {
            const double m = 0.01;
            double x0 = Math.Min(m, okviri.Values.Min(o => o[0])), y0 = Math.Min(m, okviri.Values.Min(o => o[1]));
            double x1 = Math.Max(1 - m, okviri.Values.Max(o => o[2])), y1 = Math.Max(1 - m, okviri.Values.Max(o => o[3]));
            double sx = (1 - 2 * m) / (x1 - x0), sy = (1 - 2 * m) / (y1 - y0);
            return okviri.ToDictionary(kv => kv.Key, kv => new[] { m + (kv.Value[0] - x0) * sx, m + (kv.Value[1] - y0) * sy, m + (kv.Value[2] - x0) * sx, m + (kv.Value[3] - y0) * sy });
        }

        // ------------------------------------------------------------------------------------------- podloga i oznake

        private static Bitmap Podloga(Prilog p, int strana)
        {
            // test podaci „.strane.json“ (pročitane strane bez originala) nisu podloga
            if (p == null || p.Sadrzaj == null || p.Strane != null) return null;
            // Word: podloga je PDF dobijen pretvaranjem (isti kao za čitanje; WordUPdf ga čuva u memoriji)
            if (p.Vrsta == VrstaPriloga.Word)
            {
                var pdf = WordUPdf.Pretvori(p, out _, out _);
                if (pdf == null) return null;
                p = new Prilog { Naziv = p.Naziv, Sadrzaj = pdf, Vrsta = VrstaPriloga.Pdf };
            }
            byte[] slika = p.Vrsta == VrstaPriloga.Pdf ? PdfCrtac.StranaUPng(p.Sadrzaj, Math.Max(1, strana), Dpi) : PdfCrtac.SlikaOkrenuta(p.Sadrzaj);
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
                            g.DrawString(tekst, f, crna, pr, sf);   // v2.8.1: upisan (ispravan) tekst ostaje crn; dopune su crvene
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

        /// <summary>Tekst ispravke na strani lista: bez izvora u zagradi sa imenom fajla.</summary>
        internal static string ZaStranu(string primer) => Regex.Replace(primer ?? "", @"\s*\([^()]*\.(pdf|png|jpe?g|tiff?|xlsx?)[^()]*\)", "", RegexOptions.IgnoreCase).Trim();

        /// <summary>Red tabele maila sa brojem iz tabele (ispravke i napomene se broje posebno).</summary>
        internal sealed class Stavka
        {
            public Izvestaj.Red Red;
            public bool Greska;
            public int Broj;
        }

        /// <summary>Mapa mastila originalne strane (pre bilo kakvog crtanja): gde je rubrika prazna.</summary>
        private sealed class Tinta
        {
            public readonly int W, H;
            private readonly bool[] ink;

            private readonly int[] zbir;   // integralna slika mastila (W+1)×(H+1)

            public Tinta(Bitmap b)
            {
                W = b.Width; H = b.Height; ink = new bool[W * H];
                var data = b.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    var red = new byte[data.Stride];
                    for (int y = 0; y < H; y++)
                    {
                        System.Runtime.InteropServices.Marshal.Copy(data.Scan0 + y * data.Stride, red, 0, data.Stride);
                        for (int x = 0; x < W; x++) ink[y * W + x] = (red[x * 3] * 114 + red[x * 3 + 1] * 587 + red[x * 3 + 2] * 299) / 1000 < 170;
                    }
                }
                finally { b.UnlockBits(data); }
                zbir = new int[(W + 1) * (H + 1)];
                for (int y = 0; y < H; y++)
                {
                    int red = 0;
                    for (int x = 0; x < W; x++) { if (ink[y * W + x]) red++; zbir[(y + 1) * (W + 1) + x + 1] = zbir[y * (W + 1) + x + 1] + red; }
                }
            }

            /// <summary>Udeo mastila u pravougaoniku (integralna slika, O(1)).</summary>
            public double Pokrivenost(int x0, int y0, int x1, int y1)
            {
                x0 = Math.Max(0, Math.Min(W, x0)); x1 = Math.Max(0, Math.Min(W, x1)); y0 = Math.Max(0, Math.Min(H, y0)); y1 = Math.Max(0, Math.Min(H, y1));
                if (x1 <= x0 || y1 <= y0) return 0;
                var s = zbir[y1 * (W + 1) + x1] - zbir[y0 * (W + 1) + x1] - zbir[y1 * (W + 1) + x0] + zbir[y0 * (W + 1) + x0];
                return s / (double)((x1 - x0) * (y1 - y0));
            }

            /// <summary>
            /// Pomak tekstualnog sloja u odnosu na iscrtanu stranu (npr. CropBox ≠ MediaBox): (dx, dy) u pikselima pri kome okviri
            /// reči najbolje pokrivaju mastilo. (0, 0) kada pomak nije jasno bolji.
            /// </summary>
            public (int dx, int dy) Pomak(IEnumerable<Rec> reci)
            {
                var r = reci.Where(w => w.X1 > w.X0 && w.Y1 > w.Y0 && w.T.Length >= 2 && !w.JePodatak)   // polja obrasca: okvir polja, ne teksta
                    .Select(w => (x0: (int)(w.X0 * W), y0: (int)(w.Y0 * H), x1: (int)(w.X1 * W), y1: (int)(w.Y1 * H))).Take(400).ToList();
                if (r.Count < 5) return (0, 0);
                double Ocena(int dx, int dy) => r.Average(x => Pokrivenost(x.x0 + dx, x.y0 + dy, x.x1 + dx, x.y1 + dy));
                var nula = Ocena(0, 0);
                (int, int) naj = (0, 0); var najO = nula;
                for (int dy = -24; dy <= 24; dy += 2)
                    for (int dx = -72; dx <= 72; dx += 2)
                    {
                        var o = Ocena(dx, dy);
                        if (o > najO) { najO = o; naj = (dx, dy); }
                    }
                return najO > nula * 1.2 + 0.005 ? naj : (0, 0);
            }

            public int Red(int y, int x0, int x1)
            {
                if (y < 0 || y >= H) return 0;
                int n = 0; x0 = Math.Max(0, x0); x1 = Math.Min(W, x1);
                for (int x = x0; x < x1; x++) if (ink[y * W + x]) n++;
                return n;
            }

            public int Kolona(int x, int y0, int y1)
            {
                if (x < 0 || x >= W) return 0;
                int n = 0; y0 = Math.Max(0, y0); y1 = Math.Min(H, y1);
                for (int y = y0; y < y1; y++) if (ink[y * W + x]) n++;
                return n;
            }

            public double Udeo(RectangleF r)
            {
                int x0 = Math.Max(0, (int)r.X), x1 = Math.Min(W, (int)r.Right), y0 = Math.Max(0, (int)r.Y), y1 = Math.Min(H, (int)r.Bottom);
                if (x1 <= x0 || y1 <= y0) return 1;
                int n = 0;
                for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++) if (ink[y * W + x]) n++;
                return n / (double)((x1 - x0) * (y1 - y0));
            }
        }

        private const float NajveciPt = 9f, NajmanjiPt = 6f;

        /// <summary>
        /// Ucrtava nalaze lista (v2.8.1): ispravan tekst se ne prekriva – samo tanak okvir oko rubrike; u gornji desni ugao krug sa
        /// brojem iz tabele maila (više nalaza – brojevi jedan pored drugog); dopuna (stvarna vrednost ili realan primer) u prvi prazan
        /// red ispod ili desno od postojećeg teksta, istim stilom (Arial 9 → 6 pt), crveno; pogrešan deo se precrtava tankom linijom
        /// i ispravka se upisuje pored. Napomene: narandžast okvir i broj, tekst samo kada je predlog siguran. Što ne stane ni sa 6 pt
        /// ide na marginu strane (strana se proširi udesno), pored rubrike, sa istim brojem. Vraća broj ispravki, napomena i nalaza bez rubrike.
        /// </summary>
        private static (int ispravki, int napomena, int bezRubrike) Ucrtaj(Bitmap bmp, RezultatLista l, List<Stavka> redovi, bool en, RezultatProvere r,
            Dictionary<string, double[]> okviri, List<Upis> upisi, out List<(Stavka s, string rubrika, string tekst)> naMargini)
        {
            int isp = 0, nap = 0, bez = 0;
            var tinta = new Tinta(bmp);
            var visinaStrane = bmp.Height; var sirinaStrane = bmp.Width;
            var zauzeto = new List<RectangleF>();
            // pomak tekstualnog sloja prema iscrtanoj strani (samo PDF/sken; tabela se crta iz ćelija)
            var pomak = l.Cim.Sablon == "TABELA" || l.Cim.ReciRubrika == null ? (0, 0) : tinta.Pomak(l.Cim.ReciRubrika.Values.SelectMany(x => x));
            var margina = new List<(Stavka s, string rubrika, string tekst)>();
            var tabela = l.Cim.Sablon == "TABELA";
            // nalazi ovog lista, po rubrici (više nalaza iste rubrike – jedan okvir, brojevi jedan pored drugog)
            var zaList = redovi.Where(x => r.Listovi.Count <= 1 || x.Red.Listovi.Count == 0 || x.Red.Listovi.Contains(l.Oznaka)).ToList();
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                foreach (var grupa in zaList.GroupBy(x => x.Red.N.Rubrika))
                {
                    var okv = Okvir(okviri, grupa.Key, bmp.Width, bmp.Height);
                    if (okv == null)
                    {
                        bez += grupa.Count();
                        foreach (var s in grupa) upisi.Add(new Upis { List = l.Oznaka, Rubrika = grupa.Key, Pravilo = s.Red.N.Pravilo, Broj = s.Broj, Greska = s.Greska, Mesto = "bez rubrike na strani (samo u tabeli maila)" });
                        continue;
                    }
                    var o = okv.Value;
                    // v2.8.2: štampani natpisi obrasca na vrhu rubrike – dopuna nikad ne ide u taj red
                    var dnoNatpisa = DnoNatpisa(tinta, o);
                    var stavke = grupa.OrderBy(x => x.Greska ? 0 : 1).ThenBy(x => x.Broj).ToList();
                    var imaGresku = stavke.Any(x => x.Greska);
                    using (var pen = new Pen(imaGresku ? Crvena : Narandzasta, imaGresku ? Px(0.8f) : Px(1f))) g.DrawRectangle(pen, o.X, o.Y, o.Width, o.Height);
                    // krugovi sa brojevima: gornji desni ugao, ne preko teksta
                    // okviri drugih rubrika unutar ove (npr. r14 u gornjem desnom uglu r13, r2/r3 uz r1) – krug ne sme u njih
                    var drugi = okviri.Where(kv => kv.Key != grupa.Key && !Regex.IsMatch(kv.Key, "^" + Regex.Escape(grupa.Key) + @"(?![\dab])"))
                        .Select(kv => new RectangleF((float)(kv.Value[0] * bmp.Width), (float)(kv.Value[1] * bmp.Height), (float)((kv.Value[2] - kv.Value[0]) * bmp.Width), (float)((kv.Value[3] - kv.Value[1]) * bmp.Height)))
                        .Where(rr => rr.IntersectsWith(o) && !(rr.Contains(o))).Select(rr => RectangleF.Inflate(rr, -Px(1), -Px(1))).ToList();
                    var krugovi = Krugovi(tinta, o, stavke.Count, zauzeto, drugi);
                    for (int i = 0; i < stavke.Count; i++) { Znacka(g, krugovi[i].X, krugovi[i].Y, stavke[i].Broj, stavke[i].Greska ? Crvena : Narandzasta); zauzeto.Add(krugovi[i]); }

                    for (int i = 0; i < stavke.Count; i++)
                    {
                        var s = stavke[i];
                        if (s.Greska) isp++; else nap++;
                        var n = NalazLista(l, s.Red.N);
                        var boja = s.Greska ? Crvena : Narandzasta;
                        var upis = new Upis { List = l.Oznaka, Rubrika = grupa.Key, Pravilo = n.Pravilo, Broj = s.Broj, Greska = s.Greska, Krug = krugovi[i], Okvir = o };
                        upisi.Add(upis);
                        var tekst = (n.Dopuna ?? "").Trim();
                        if (tekst.Length == 0 || (!s.Greska && !n.DopunaSigurna)) { upis.Mesto = "samo oznaka"; continue; }
                        upis.Dopuna = tekst;
                        // pogrešan deo: precrtava se, ispravka odmah pored (ili ispod)
                        RectangleF? precrtano = null;
                        if (!string.IsNullOrEmpty(n.Pogresno) && !tabela) precrtano = Precrtaj(g, l.Cim, grupa.Key, n.Pogresno, bmp.Width, bmp.Height, boja, pomak);
                        if (precrtano != null) upis.Pogresno = n.Pogresno;
                        // dno upisanog teksta rubrike (reči rubrike, sa pomakom tekstualnog sloja); tabela – od vrha ćelije
                        var reciRub = tabela || l.Cim.ReciRubrika == null ? new List<Rec>()
                            : l.Cim.ReciRubrika.Where(kv => kv.Key == grupa.Key || Regex.IsMatch(kv.Key, "^" + Regex.Escape(grupa.Key) + @"(?![\dab])")).SelectMany(kv => kv.Value)
                                .Where(w => w.Cy * visinaStrane + pomak.Item2 >= o.Top && w.Cy * visinaStrane + pomak.Item2 <= o.Bottom).ToList();
                        var dnoTeksta = reciRub.Count == 0 ? 0f : (float)(reciRub.Max(w => w.Y1) * visinaStrane + pomak.Item2);
                        // redovi upisanog teksta (piksel): reči istog reda spojene
                        var redoviTeksta = reciRub.GroupBy(w => Math.Round(w.Cy * 200)).Select(gr => RectangleF.FromLTRB((float)(gr.Min(w => w.X0) * sirinaStrane + pomak.Item1), (float)(gr.Min(w => w.Y0) * visinaStrane + pomak.Item2),
                            (float)(gr.Max(w => w.X1) * sirinaStrane + pomak.Item1), (float)(gr.Max(w => w.Y1) * visinaStrane + pomak.Item2))).Where(rr => rr.Right <= o.Right && rr.Left >= o.Left - 2).ToList();
                        // r25 (v2.8.2): mase uz oznake koje obrazac ima („T= …“, „Netto:“) – svaka u svoj red
                        if (n.DopunaRedovi != null && n.DopunaUzOznake)
                        {
                            var ne = UpisiUzOznake(g, tinta, o, dnoNatpisa, n.DopunaRedovi, boja, zauzeto, upis);
                            upis.Mesto = ne.Count == 0 ? "uz oznake mase" : "delom na donjoj margini";
                            if (ne.Count > 0) margina.Add((s, grupa.Key, string.Join(", ", ne)));
                            continue;
                        }
                        // r62 (v2.8.2): svaki deo u svoje polje obrasca (kućice 62.1–62.4); ostatak – u prazan prostor rubrike
                        if (n.DopunaPoPoljima != null && n.DopunaPoPoljima.Count > 0 && !tabela && l.Cim.KuciceOkviri != null)
                        {
                            var ostalo = new List<string>();
                            foreach (var kv in n.DopunaPoPoljima.OrderBy(x => x.Key, StringComparer.Ordinal))
                            {
                                if (!l.Cim.KuciceOkviri.TryGetValue(kv.Key, out var kc)) { ostalo.Add(kv.Value); continue; }
                                var polje = RectangleF.FromLTRB((float)(kc[0] * bmp.Width), (float)(kc[1] * bmp.Height), (float)(kc[2] * bmp.Width), (float)(kc[3] * bmp.Height));
                                var ok = false;
                                for (var ptp = NajveciPt; ptp >= NajmanjiPt && !ok; ptp -= 0.5f)
                                    using (var f = new Font("Arial", Px(ptp), FontStyle.Regular, GraphicsUnit.Pixel))
                                    {
                                        var v = g.MeasureString(kv.Value, f);
                                        if (v.Width > polje.Width - Px(1) && ptp > NajmanjiPt) continue;
                                        var kk = new RectangleF(polje.X + Px(1), polje.Y + polje.Height / 2 - v.Height / 2, v.Width, v.Height);
                                        using (var b = new SolidBrush(boja)) g.DrawString(kv.Value, f, b, kk.Location);
                                        zauzeto.Add(kk);
                                        upis.Tekst = upis.Tekst.Width > 0 ? RectangleF.Union(upis.Tekst, kk) : kk;
                                        upis.Pt = ptp; ok = true;
                                    }
                                if (!ok) ostalo.Add(kv.Value);
                            }
                            upis.Mesto = "u poljima rubrike";
                            if (ostalo.Count == 0) continue;
                            tekst = string.Join(" ", ostalo);
                        }
                        if (n.DopunaRedovi != null) tekst = string.Join("\n", n.DopunaRedovi);
                        var (mesto, pt, ostatak, gde) = Smesti(g, tinta, o, tekst, zauzeto, precrtano, dnoTeksta, redoviTeksta, dnoNatpisa, n.DopunaIspod);
                        if (mesto != null)
                        {
                            var deo = ostatak == null ? tekst : tekst.Substring(0, tekst.Length - ostatak.Length).TrimEnd(' ', ',');
                            using (var f = new Font("Arial", Px(pt), FontStyle.Regular, GraphicsUnit.Pixel))
                            using (var b = new SolidBrush(boja))
                                g.DrawString(deo, f, b, mesto.Value);
                            zauzeto.Add(mesto.Value);
                            upis.Tekst = mesto.Value; upis.Pt = pt; upis.Mesto = gde;
                        }
                        if (mesto == null || ostatak != null)
                        {
                            margina.Add((s, grupa.Key, (mesto == null ? tekst : ostatak).Replace("\n", ", ")));
                            upis.Mesto = mesto == null ? "na donjoj margini" : "delom na donjoj margini";
                        }
                    }
                }
            }
            naMargini = margina;
            return (isp, nap, bez);
        }

        /// <summary>Nalaz baš ovog lista (isti red tabele na više listova ima svoju dopunu po listu – npr. datum iz r29 tog lista).</summary>
        private static Nalaz NalazLista(RezultatLista l, Nalaz n) =>
            l.Nalazi.FirstOrDefault(x => x.Pravilo == n.Pravilo && x.Rubrika == n.Rubrika && x.Tekst == n.Tekst)
            ?? l.Nalazi.FirstOrDefault(x => x.Pravilo == n.Pravilo && x.Rubrika == n.Rubrika) ?? n;

        /// <summary>
        /// Krugovi sa brojevima u gornjem desnom uglu rubrike (s desna na levo), unutar rubrike, van okvira drugih rubrika;
        /// po mogućstvu bez teksta ispod kruga.
        /// </summary>
        private static List<RectangleF> Krugovi(Tinta t, RectangleF o, int broj, List<RectangleF> zauzeto, List<RectangleF> drugi)
        {
            // v2.8.2: krug je UVEK unutar svoje rubrike (gornji desni ugao), nikad u susednoj rubrici ni u okviru druge rubrike unutar nje
            var d = Px(9); var razmak = Px(1.5f);
            List<RectangleF> Red(float xDesno, float y) => Enumerable.Range(0, broj).Select(i => new RectangleF(xDesno - d - i * (d + razmak), y, d, d)).ToList();
            bool UnutraOk(RectangleF k) => k.X >= o.X + Px(1) && k.Right <= o.Right - Px(0.5f) && k.Y >= o.Y && k.Bottom <= o.Bottom
                                           && !drugi.Any(r => r.IntersectsWith(k)) && !zauzeto.Any(z => z.IntersectsWith(k));
            var y0 = o.Y + Px(1.5f);
            // 1. gornji red, s desna ulevo, bez mastila; 2. isto, dozvoljene tanke linije (do 5 % mastila); 3. bez obzira na mastilo
            foreach (var prag in new[] { 0.006, 0.05, 1.0 })
                for (float pomak = 0; pomak <= o.Width; pomak += d / 3)
                {
                    var red = Red(o.Right - Px(1.5f) - pomak, y0);
                    if (red.All(k => UnutraOk(k) && t.Udeo(RectangleF.Inflate(k, Px(0.5f), Px(0.5f))) <= prag)) return red;
                }
            return Red(o.Right - Px(1.5f), y0);
        }
        /// <summary>Precrtava pogrešan deo upisa (reči rubrike čiji tekst odgovara) tankom linijom; vraća okvir precrtanog ili null.</summary>
        private static RectangleF? Precrtaj(Graphics g, CimList c, string rubrika, string pogresno, int w, int h, Color boja, (int dx, int dy) pomak)
        {
            if (c.ReciRubrika == null) return null;
            string Kljuc(string s) => Regex.Replace(Saobracaj.Common.Text.TekstUtil.Kljuc(s ?? ""), @"\s+", "");
            var cilj = Kljuc(pogresno);
            if (cilj.Length == 0) return null;
            var reci = c.ReciRubrika.Where(kv => kv.Key == rubrika || Regex.IsMatch(kv.Key, "^" + Regex.Escape(rubrika) + @"(?![\dab])")).SelectMany(kv => kv.Value)
                // polja PDF obrasca (AcroForm): položaj reči je procena (višeredno polje) – ne precrtava se, dopuna ide u prazan prostor
                .Where(x => x.X1 > x.X0 && x.Font != "AcroForm").OrderBy(x => Math.Round(x.Cy, 2)).ThenBy(x => x.X0).ToList();
            // jedna reč ili niz susednih reči čiji spoj daje traženi tekst
            for (int i = 0; i < reci.Count; i++)
            {
                var spoj = "";
                for (int j = i; j < reci.Count && j < i + 6; j++)
                {
                    spoj += Kljuc(reci[j].T);
                    if (spoj == cilj || (j == i && spoj.Length >= 3 && spoj.Contains(cilj)))
                    {
                        var deo = reci.Skip(i).Take(j - i + 1).ToList();
                        var rc = RectangleF.FromLTRB((float)(deo.Min(x => x.X0) * w) + pomak.dx, (float)(deo.Min(x => x.Y0) * h) + pomak.dy, (float)(deo.Max(x => x.X1) * w) + pomak.dx, (float)(deo.Max(x => x.Y1) * h) + pomak.dy);
                        using (var pen = new Pen(boja, Px(0.7f))) g.DrawLine(pen, rc.X - Px(0.5f), rc.Y + rc.Height * 0.55f, rc.Right + Px(0.5f), rc.Y + rc.Height * 0.55f);
                        return rc;
                    }
                    if (!cilj.StartsWith(spoj, StringComparison.Ordinal)) break;
                }
            }
            return null;
        }

        /// <summary>
        /// Prazno mesto u rubrici za dopunu (pravougaonik bez mastila, integralna slika): odmah desno od precrtanog ili ispod njega;
        /// prvi slobodan red ispod postojećeg teksta (sleva); bilo gde u praznom prostoru rubrike (prvo ispod, pa desno od teksta).
        /// Font 9 → 6 pt. Ako ni sa 6 pt ne staje ceo tekst – koliko staje, ostatak se vraća za marginu.
        /// </summary>
        private static (RectangleF? mesto, float pt, string ostatak, string gde) Smesti(Graphics g, Tinta t, RectangleF o, string tekst, List<RectangleF> zauzeto, RectangleF? precrtano, float dnoTeksta, List<RectangleF> redoviTeksta, float dnoNatpisa, bool samoIspod = false)
        {
            float ivica = Px(1.8f);
            int x0 = (int)(o.X + ivica), x1 = (int)(o.Right - ivica), y0 = (int)Math.Max(o.Y + ivica, dnoNatpisa + Px(1)), y1 = (int)(o.Bottom - ivica);
            if (x1 - x0 < Px(12) || y1 - y0 < Px(5)) return (null, 0, tekst, "");
            int sir = x1 - x0;
            // dno postojećeg teksta: upisane reči rubrike; inače poslednji red sa mastilom (natpisi obrasca se ne računaju kao tekst)
            int dno = dnoTeksta > 0 ? (int)Math.Ceiling(dnoTeksta) : y0;
            bool Slobodno(RectangleF k) => k.Left >= x0 - 1 && k.Right <= x1 + 1 && k.Top >= y0 - 1 && k.Bottom <= y1 + 1
                                           && t.Pokrivenost((int)(k.Left - Px(1.2f)), (int)(k.Top - Px(0.5f)), (int)Math.Ceiling(k.Right + Px(1.2f)), (int)Math.Ceiling(k.Bottom + Px(0.3f))) <= 0.004   // razmak ~1 pt od teksta
                                           && !zauzeto.Any(z => z.IntersectsWith(k));
            RectangleF? Trazi(float w, float h, int od, int @do)
            {
                for (int y = Math.Max(y0, od); y + h <= y1 && y <= @do; y += 2)
                    for (int x = x0; x + w <= x1; x += 3)
                    {
                        var k = new RectangleF(x, y, w, h);
                        if (Slobodno(k)) return k;
                    }
                return null;
            }

            for (var pt = NajveciPt; pt >= NajmanjiPt; pt -= 0.5f)
                using (var f = new Font("Arial", Px(pt), FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    var jedan = g.MeasureString(tekst, f);
                    // 1. pored precrtanog dela (isti red), pa ispod njega
                    if (precrtano != null)
                    {
                        var k = new RectangleF(precrtano.Value.Right + Px(1.5f), precrtano.Value.Y + precrtano.Value.Height / 2 - jedan.Height / 2, jedan.Width, jedan.Height);
                        if (Slobodno(k)) return (k, pt, null, "pored precrtanog");
                        var ispod = new RectangleF(precrtano.Value.X, precrtano.Value.Bottom + Px(0.5f), jedan.Width, jedan.Height);
                        if (Slobodno(ispod)) return (ispod, pt, null, "ispod precrtanog");
                    }
                    // tekst u jednom redu ako staje u širinu rubrike, inače prelomljen na širinu rubrike
                    var vel = jedan.Width <= sir ? jedan : g.MeasureString(tekst, f, sir);
                    float w = Math.Min(sir, vel.Width + 2), h = (float)Math.Ceiling(vel.Height);
                    // 2. prvi slobodan red ispod postojećeg teksta
                    var mesto = Trazi(w, h, dno + 1, y1);
                    if (mesto != null) return (mesto, pt, null, dnoTeksta > 0 ? "ispod teksta" : "u praznom redu");
                    // 3. desno od reda upisanog teksta (od poslednjeg reda naviše), u istoj visini
                    // (v2.9.2: ne za dopunu koja ide samo u novi red – r57 „3212 KP“ nikad pored strane deonice)
                    foreach (var red in (samoIspod ? new List<RectangleF>() : redoviTeksta ?? new List<RectangleF>()).OrderByDescending(r => r.Bottom))
                    {
                        var k = new RectangleF(red.Right + Px(2.5f), red.Y + red.Height / 2 - jedan.Height / 2, jedan.Width, jedan.Height);
                        if (Slobodno(k)) return (k, pt, null, "desno od teksta");
                    }
                }
            // 4. bilo gde u praznom prostoru rubrike (od 9 do 6 pt)
            for (var pt = NajveciPt; pt >= NajmanjiPt && !samoIspod; pt -= 0.5f)
                using (var f = new Font("Arial", Px(pt), FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    var jedan = g.MeasureString(tekst, f);
                    var vel = jedan.Width <= sir ? jedan : g.MeasureString(tekst, f, sir);
                    var mesto = Trazi(Math.Min(sir, vel.Width + 2), (float)Math.Ceiling(vel.Height), y0, y1);
                    if (mesto != null) return (mesto, pt, null, "u praznom prostoru rubrike");
                }
            // 5. ni 6 pt: koliko staje (jedan red), ostatak na marginu
            using (var f = new Font("Arial", Px(NajmanjiPt), FontStyle.Regular, GraphicsUnit.Pixel))
            {
                var reci = tekst.Split(' ');
                for (int n = reci.Length - 1; n >= 1; n--)
                {
                    var deo = string.Join(" ", reci.Take(n)).TrimEnd(',', ';');
                    var v = g.MeasureString(deo, f);
                    var k = Trazi(v.Width + 2, (float)Math.Ceiling(v.Height), y0, y1);
                    if (k != null) return (k, NajmanjiPt, string.Join(" ", reci.Skip(n)), "u rubrici");
                }
            }
            return (null, 0, tekst, "");
        }
        /// <summary>
        /// v2.8.2: dno štampanih natpisa na vrhu rubrike („4 Consignee (name, address, country) / Destinataire …“): prvi pojas
        /// mastila koji počinje odmah ispod gornje ivice i sledeći pojasevi sa malim razmakom. Dopuna se ne upisuje iznad tog dna.
        /// </summary>
        private static float DnoNatpisa(Tinta t, RectangleF o)
        {
            int x0 = (int)(o.X + Px(1.8f)), x1 = (int)(o.Right - Px(1.8f)), y0 = (int)(o.Y + Px(1.5f)), y1 = (int)(o.Bottom - Px(1.5f));
            if (x1 - x0 < 10 || y1 - y0 < 10) return o.Y;
            int prag = Math.Max(1, (int)((x1 - x0) * 0.01));
            bool Ima(int y) => t.Red(y, x0, x1) > prag;
            int y = y0;
            while (y < y1 && !Ima(y)) y++;
            if (y >= y1 || y - y0 > Px(14)) return o.Y;          // nema natpisa na vrhu
            int dno = y;
            bool prvi = true;
            while (true)
            {
                while (dno < y1 && Ima(dno)) dno++;               // kraj pojasa
                if (dno - y > Px(10)) return prvi ? o.Y : y - 1;  // previsok pojas – nije natpis (upisan tekst ili crtež)
                // v2.8.3: pojas viši od reda natpisa odmah ispod natpisa je upisan tekst („Wittenberg“ u r29 CUV-a), ne natpis
                if (!prvi && dno - y > Px(5.5f)) return y - 1;
                prvi = false;
                int sledeci = dno;
                while (sledeci < y1 && !Ima(sledeci)) sledeci++;
                if (sledeci >= y1 || sledeci - dno > Px(3) || sledeci - y0 > Px(22)) return dno;
                y = sledeci; dno = sledeci;
            }
        }

        /// <summary>
        /// r25: svaka vrednost mase desno od svog reda oznake („T= ……“, „Netto:“) – redovi su pojasevi mastila ispod natpisa rubrike.
        /// Vraća vrednosti koje nisu stale (idu na donju marginu).
        /// </summary>
        private static List<string> UpisiUzOznake(Graphics g, Tinta t, RectangleF o, float dnoNatpisa, List<string> vrednosti, Color boja, List<RectangleF> zauzeto, Upis upis)
        {
            int x0 = (int)(o.X + Px(1.8f)), x1 = (int)(o.Right - Px(1.8f)), y0 = (int)Math.Max(o.Y + Px(1.8f), dnoNatpisa + Px(1)), y1 = (int)(o.Bottom - Px(1.8f));
            int prag = Math.Max(1, (int)((x1 - x0) * 0.01));
            var pojasevi = new List<(int od, int @do)>();
            for (int y = y0; y < y1; y++)
            {
                if (t.Red(y, x0, x1) <= prag) continue;
                int p = y; while (y < y1 && t.Red(y, x0, x1) > prag) y++;
                if (y - p >= Px(3)) pojasevi.Add((p, y));
            }
            var ne = new List<string>();
            // red koji je svetliji od praga (npr. „G= ………“ bledo odštampan): po istom razmaku kao prethodni redovi
            while (pojasevi.Count >= 2 && pojasevi.Count < vrednosti.Count)
            {
                var korak = pojasevi[pojasevi.Count - 1].od - pojasevi[pojasevi.Count - 2].od;
                var sledeci = (pojasevi[pojasevi.Count - 1].od + korak, pojasevi[pojasevi.Count - 1].@do + korak);
                if (korak <= 0 || sledeci.Item2 > y1) break;
                pojasevi.Add(sledeci);
            }
            for (int i = 0; i < vrednosti.Count; i++)
            {
                if (i >= pojasevi.Count) { ne.Add(vrednosti[i]); continue; }
                var (od, @do) = pojasevi[i];
                int desno = x0;
                // kraj teksta u redu; uspravne linije (pregrade kućica, ivica) koje idu iznad i ispod reda se preskaču
                for (int x = x1 - 1; x >= x0; x--)
                    if (t.Kolona(x, od, @do) > 0 && t.Kolona(x, od - (int)Px(4), @do + (int)Px(4)) < (@do - od) + Px(6)) { desno = x + 1; break; }
                RectangleF? mesto = null; float pt = NajveciPt;
                for (; pt >= NajmanjiPt && mesto == null; pt -= 0.5f)
                    using (var f = new Font("Arial", Px(pt), FontStyle.Regular, GraphicsUnit.Pixel))
                    {
                        var v = g.MeasureString(vrednosti[i], f);
                        var k = new RectangleF(desno + Px(2), (od + @do) / 2f - v.Height / 2, v.Width, v.Height);
                        if (k.Right <= x1 && !zauzeto.Any(z => z.IntersectsWith(k)) && t.Pokrivenost((int)k.X, (int)k.Y, (int)k.Right, (int)k.Bottom) <= 0.01)
                        {
                            g.DrawString(vrednosti[i], f, new SolidBrush(boja), k.Location);
                            mesto = k;
                        }
                    }
                if (mesto == null) { ne.Add(vrednosti[i]); continue; }
                zauzeto.Add(mesto.Value);
                upis.Tekst = upis.Tekst.Width > 0 ? RectangleF.Union(upis.Tekst, mesto.Value) : mesto.Value;
                upis.Pt = pt + 0.5f;
            }
            return ne;
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

        /// <summary>Spisak kola: okvir oko cele strane i brojevi iz tabele maila u levom gornjem uglu.</summary>
        private static void UcrtajSpisak(Bitmap bmp, List<Stavka> naSpisku, bool en)
        {
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var greska = naSpisku.Any(x => x.Greska);
                using (var pen = new Pen(greska ? Crvena : Narandzasta, Px(1f))) g.DrawRectangle(pen, Px(6), Px(6), bmp.Width - Px(12), bmp.Height - Px(12));
                float x = Px(10);
                foreach (var s in naSpisku) { Znacka(g, x, Px(10), s.Broj, s.Greska ? Crvena : Narandzasta); x += Px(11); }
                using (var f = new Font("Arial", Px(8), FontStyle.Regular, GraphicsUnit.Pixel))
                using (var b = new SolidBrush(greska ? Crvena : Narandzasta))
                    g.DrawString(en ? "Wagon list – see the same numbers in the table in our e-mail" : "Spisak kola – isti brojevi su u tabeli u našem mailu", f, b, x + Px(2), Px(10));
            }
        }

        /// <summary>
        /// v2.8.2: strana ostaje A4 (150 dpi): traka u vrhu, list umanjen tako da stane, ispod njega donja margina sa dopunama
        /// koje nisu stale u rubriku (broj u krugu + rubrika + tekst) i dijagonalni vodeni žig preko cele strane.
        /// </summary>
        private static Bitmap Oznaci(Bitmap original, bool en, List<(Stavka s, string rubrika, string tekst)> naMargini)
        {
            var polozeno = original.Width > original.Height * 1.1;
            int W = polozeno ? 1754 : 1240, H = polozeno ? 1240 : 1754;
            var traka = (int)Px(22);
            var bmp = new Bitmap(W, H, PixelFormat.Format24bppRgb);
            bmp.SetResolution(Dpi, Dpi);
            using (var g = Graphics.FromImage(bmp))
            using (var fm = new Font("Arial", Px(8f), FontStyle.Regular, GraphicsUnit.Pixel))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.Clear(Color.White);
                // visina donje margine: sve napomene, svaka u svom redu
                var sirTeksta = W - Px(36);
                var redovi = (naMargini ?? new List<(Stavka, string, string)>())
                    .Select(x => (x.s, tekst: (Regex.IsMatch(x.rubrika ?? "", @"^\d") ? (en ? "Box " : "Rubrika ") + x.rubrika + ": " : "") + x.tekst)).ToList();
                float margina = redovi.Count == 0 ? 0 : redovi.Sum(x => Math.Max(g.MeasureString(x.tekst, fm, (int)sirTeksta).Height, Px(10)) + Px(3)) + Px(10);
                // list: umanjen da stane između trake i donje margine, centriran
                float prostorH = H - traka - margina, k = Math.Min(1f, Math.Min(W / (float)original.Width, prostorH / original.Height));
                float w = original.Width * k, h = original.Height * k, x0 = (W - w) / 2;
                g.DrawImage(original, x0, traka, w, h);
                using (var b = new SolidBrush(Color.FromArgb(255, 240, 240, 240))) g.FillRectangle(b, 0, 0, W, traka);
                using (var pen = new Pen(Color.FromArgb(160, 160, 160), 1f)) g.DrawLine(pen, 0, traka - 1, W, traka - 1);
                using (var f = new Font("Arial", Px(8.5f), FontStyle.Bold, GraphicsUnit.Pixel))
                using (var b = new SolidBrush(Color.FromArgb(70, 70, 70)))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString(en ? TrakaEn : Traka, f, b, new RectangleF(Px(6), 0, W - Px(12), traka), sf);
                if (redovi.Count > 0)
                {
                    float y = traka + h + Px(5);
                    using (var pen = new Pen(Color.FromArgb(190, 190, 190), 1f) { DashStyle = DashStyle.Dash }) g.DrawLine(pen, Px(10), y, W - Px(10), y);
                    y += Px(4);
                    foreach (var (s, tekst) in redovi)
                    {
                        var boja = s.Greska ? Crvena : Narandzasta;
                        Znacka(g, Px(12), y, s.Broj, boja);
                        var v = g.MeasureString(tekst, fm, (int)sirTeksta);
                        using (var b = new SolidBrush(boja)) g.DrawString(tekst, fm, b, new RectangleF(Px(24), y, sirTeksta, v.Height + 2));
                        y += Math.Max(v.Height, Px(10)) + Px(3);
                    }
                }
                Zig(g, W, H, traka, en);
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
