using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Saobracaj.Agent.CimProvera.Citanje
{
    /// <summary>Pragovi za pouzdano poravnanje (App.config).</summary>
    internal sealed class PragoviPoravnanja
    {
        /// <summary>Najmanji zbir sidara svih vrsta (tekst, brojevi rubrika, linije okvira).</summary>
        public int MinSidara = 12;
        /// <summary>Najmanji broj sidara u svakoj trećini strane (gore, sredina, dole).</summary>
        public int MinPoTrecini = 2;
        /// <summary>Najveće dozvoljeno srednje odstupanje sidara posle poravnanja (udeo strane).</summary>
        public double MaxOdstupanje = 0.006;
        /// <summary>Najmanji udeo tekstualnih sidara koja se slažu sa poravnanjem (kada ih ima bar 8) – manji udeo znači da se dve grupe sidara ne slažu.</summary>
        public double MinUdeoTeksta = 0.8;
    }

    /// <summary>
    /// Šablon jednog obrasca (sablon\obrasci\*.json): pravougaonici rubrika, polja za označavanje, kućice, zaokruženi brojevi
    /// rubrika, tekstualna sidra i linije okvira – sve u koordinatama uzorka tog obrasca (0–1, y od vrha).
    /// </summary>
    internal sealed class ObrazacSablon
    {
        public string Id, Naziv, Fajl;
        public int Verzija;
        public Dictionary<string, double[]> Rubrike = new Dictionary<string, double[]>();
        public Dictionary<string, double[]> Polja = new Dictionary<string, double[]>();
        public Dictionary<string, double[]> Kucice = new Dictionary<string, double[]>();
        public Dictionary<string, (double x, double y)> Brojevi = new Dictionary<string, (double, double)>();
        /// <summary>Tekstualna sidra: normalizovana reč → položaj (samo reči koje se na uzorku javljaju jednom).</summary>
        public Dictionary<string, (double x, double y)> Sidra = new Dictionary<string, (double, double)>();
        public List<LinijaOkvira> Vodoravne = new List<LinijaOkvira>(), Uspravne = new List<LinijaOkvira>();
        public List<string> Poledjina = new List<string>();
        /// <summary>Sken: reč (OCR) niža od ovoga (udeo visine strane) sa velikim/visokim slovom je odštampani natpis obrasca; 0 = ne primenjuje se.</summary>
        public double VisinaNatpisa;
        /// <summary>Mastilo prazne rubrike (id) / kućice (#id) izmereno na uzorku: odštampani obrazac koji nije prepoznat kao natpis.</summary>
        public Dictionary<string, double> PraznoMastilo = new Dictionary<string, double>();
        /// <summary>
        /// Univerzalni CIT šablon: poravnanje po sidrima nezavisnim od jezika (brojevi rubrika iz teksta ili krugovi, linije okvira,
        /// oznaka „© 200x CIT“); tekstualna sidra su samo dodatna. Koristi se kada nijedan nacionalni šablon nije pouzdan.
        /// </summary>
        public bool Univerzalni;
        /// <summary>Položaj oznake „CIT“ (© 200x CIT) na uzorku, ako postoji.</summary>
        public (double x, double y)? CitOznaka;

        /// <summary>Broj rubrike iz ključa broja („71#2“ → „71“, „1#leg“ → „1“).</summary>
        public static string OsnovaBroja(string kljuc) { var i = kljuc.IndexOf('#'); return i < 0 ? kljuc : kljuc.Substring(0, i); }

        public static ObrazacSablon Ucitaj(string putanja)
        {
            var s = IzJson(JObject.Parse(File.ReadAllText(putanja, Encoding.UTF8)));
            s.Fajl = Path.GetFileName(putanja);
            return s;
        }

        public static ObrazacSablon IzJson(JObject j)
        {
            var s = new ObrazacSablon
            {
                Id = (string)j["id"], Naziv = (string)j["naziv"] ?? (string)j["id"], Verzija = (int?)j["verzija"] ?? 1,
            };
            void Pravougaonici(string kljuc, Dictionary<string, double[]> u)
            {
                if (j[kljuc] is JObject o) foreach (var p in o.Properties()) u[p.Name] = p.Value.Select(x => (double)x).ToArray();
            }
            Pravougaonici("rubrike", s.Rubrike);
            Pravougaonici("polja", s.Polja);
            Pravougaonici("kucice", s.Kucice);
            if (j["brojevi"] is JObject b) foreach (var p in b.Properties()) s.Brojevi[p.Name] = ((double)p.Value[0], (double)p.Value[1]);
            if (j["sidra"] is JArray sa)
                foreach (var a in sa)
                {
                    var n = CimSablon.Norm((string)a[0]);
                    if (n.Length >= 4 && !s.Sidra.ContainsKey(n)) s.Sidra[n] = ((double)a[1], (double)a[2]);
                }
            if (j["vodoravne"] is JArray v) foreach (var l in v) s.Vodoravne.Add(new LinijaOkvira { A = (double)l[0], B0 = (double)l[1], B1 = (double)l[2] });
            if (j["uspravne"] is JArray u2) foreach (var l in u2) s.Uspravne.Add(new LinijaOkvira { A = (double)l[0], B0 = (double)l[1], B1 = (double)l[2] });
            if (j["poledjina"] is JArray pol) s.Poledjina = pol.Select(x => Saobracaj.Common.Text.TekstUtil.Kljuc((string)x)).ToList();
            s.VisinaNatpisa = (double?)j["visina_natpisa"] ?? 0;
            if (j["prazno_mastilo"] is JObject pm) foreach (var p in pm.Properties()) s.PraznoMastilo[p.Name] = (double)p.Value;
            s.Univerzalni = (bool?)j["univerzalni"] ?? false;
            if (j["cit_oznaka"] is JArray co && co.Count == 2) s.CitOznaka = ((double)co[0], (double)co[1]);
            return s;
        }

        /// <summary>Rubrika (najmanji pravougaonik) u kojoj je tačka (koordinate šablona); null ako je van svih rubrika.</summary>
        public string RubrikaZa(double x, double y)
        {
            string najbolja = null; double povrsina = double.MaxValue;
            foreach (var kv in Rubrike)
            {
                var r = kv.Value;
                if (x < r[0] || x > r[2] || y < r[1] || y > r[3]) continue;
                var p = (r[2] - r[0]) * (r[3] - r[1]);
                if (p < povrsina || (p == povrsina && string.CompareOrdinal(kv.Key, najbolja) < 0)) { povrsina = p; najbolja = kv.Key; }
            }
            return najbolja;
        }
    }

    /// <summary>Par za poravnanje: tačka na strani i odgovarajuća tačka šablona (za liniju važi samo jedna koordinata).</summary>
    internal sealed class ParSidra
    {
        public string Vrsta;           // tekst | broj | vodoravna | uspravna
        public string Oznaka;
        public double Px, Py, Tx, Ty;
        public double Odstupanje;
    }

    /// <summary>
    /// Afino poravnanje strane na šablon: tx = X0·x + X1·y + X2, ty = Y0·x + Y1·y + Y2 (pomak, razmera i mali nagib skena).
    /// </summary>
    internal sealed class Poravnanje
    {
        public string Obrazac;
        public bool Univerzalni;
        public double[] X = { 1, 0, 0 }, Y = { 0, 1, 0 };
        public int Tekst, TekstKandidata, Brojevi, Linije;
        public int Ukupno => Tekst + Brojevi + Linije;
        public int[] PoTrecinama = new int[3];
        public double Odstupanje;
        public bool Pouzdano;
        public string Razlog = "";
        public List<ParSidra> Parovi = new List<ParSidra>();

        public (double x, double y) USablon(double x, double y) => (X[0] * x + X[1] * y + X[2], Y[0] * x + Y[1] * y + Y[2]);

        /// <summary>Iz koordinata šablona u koordinate strane (inverzna afina transformacija).</summary>
        public (double x, double y) NaStranu(double tx, double ty)
        {
            double a = X[0], b = X[1], c = Y[0], d = Y[1];
            var det = a * d - b * c;
            if (Math.Abs(det) < 1e-9) return (tx, ty);
            double u = tx - X[2], v = ty - Y[2];
            return ((d * u - b * v) / det, (-c * u + a * v) / det);
        }

        public string Opis => string.Format(CultureInfo.InvariantCulture,
            "{0}: sidara {1} (tekst {2}/{3}, brojevi {4}, linije {5}; po trećinama {6}/{7}/{8}), odstupanje {9:0.0000}, {10}{11}",
            Obrazac, Ukupno, Tekst, TekstKandidata, Brojevi, Linije, PoTrecinama[0], PoTrecinama[1], PoTrecinama[2], Odstupanje,
            Pouzdano ? "pouzdano" : "NIJE pouzdano", Razlog.Length > 0 ? " (" + Razlog + ")" : "");

        // ------------------------------------------------------------------------------------------- računanje

        public static Poravnanje Izracunaj(ObrazacSablon s, List<Rec> reci, Raster raster, PragoviPoravnanja prag, List<Rec> sveReci = null)
        {
            sveReci = sveReci ?? reci; // brojevi rubrika i oznaka CIT se traže među svim rečima strane (nisu uvek u fontu naziva)
            var p = new Poravnanje { Obrazac = s.Id, Univerzalni = s.Univerzalni };

            // 1) Tekstualna sidra: reč koja se na strani javlja jednom i postoji u šablonu.
            var brojPojava = new Dictionary<string, int>();
            foreach (var r in reci) { var n = CimSablon.Norm(r.T); if (n.Length >= 4) brojPojava[n] = brojPojava.TryGetValue(n, out var k) ? k + 1 : 1; }
            var tekst = new List<ParSidra>();
            foreach (var r in reci)
            {
                var n = CimSablon.Norm(r.T);
                if (n.Length < 4 || brojPojava[n] != 1 || !s.Sidra.TryGetValue(n, out var t)) continue;
                tekst.Add(new ParSidra { Vrsta = "tekst", Oznaka = r.T, Px = r.Cx, Py = r.Cy, Tx = t.x, Ty = t.y });
            }
            p.TekstKandidata = tekst.Count;

            // 2) Početna hipoteza: razmera i pomak po osi iz svakog para sidara; pobeđuje ona sa najviše saglasnih sidara.
            var krugovi = raster?.Krugovi() ?? new List<Oblik>();
            // Univerzalni šablon: brojevi rubrika (u PDF-u sa tekstom su i u tekstu, sitnim fontom) – ne zavise od jezika obrasca.
            var brojReci = s.Univerzalni ? BrojeviRubrika(sveReci) : new List<Rec>();
            var brojParovi = new List<ParSidra>();
            foreach (var kv in s.Brojevi)
            {
                var osnova = ObrazacSablon.OsnovaBroja(kv.Key);
                foreach (var w in brojReci.Where(w => w.T == osnova))
                    brojParovi.Add(new ParSidra { Vrsta = "broj", Oznaka = kv.Key, Px = w.Cx, Py = w.Cy, Tx = kv.Value.x, Ty = kv.Value.y });
            }
            var hipoteza = s.Univerzalni ? Hipoteza(brojParovi, 400, 8) : null;
            if (hipoteza == null) hipoteza = Hipoteza(tekst);
            if (hipoteza == null) hipoteza = HipotezaKrugovi(s, krugovi);
            if (hipoteza == null) { p.Razlog = "nema dovoljno sidara za početno poravnanje"; return p; }
            p.X = new[] { hipoteza.Value.ax, 0, hipoteza.Value.bx }; p.Y = new[] { 0, hipoteza.Value.ay, hipoteza.Value.by };

            // 3) Dorada: sidra svih vrsta u sve užoj toleranciji, pa afino poravnanje najmanjim kvadratima.
            var vod = raster?.Vodoravne ?? new List<LinijaOkvira>();
            var usp = raster?.Uspravne ?? new List<LinijaOkvira>();
            // Univerzalni šablon: prvo samo brojevi rubrika (jednoznačni), pa tek onda linije – linije bi inače mogle da „uhvate“
            // susedni red okvira (sve rubrike pomerene za jedan red).
            if (s.Univerzalni && brojParovi.Count > 0)
                foreach (var tol in new[] { 0.02, 0.012, 0.008, 0.006 })
                {
                    var samoBrojevi = Saglasni(p, s, new List<ParSidra>(), krugovi, new List<LinijaOkvira>(), new List<LinijaOkvira>(), tol, brojReci, sveReci);
                    if (samoBrojevi.Count >= 8) Fit(p, samoBrojevi);
                }
            foreach (var tol in s.Univerzalni && brojParovi.Count > 0 ? new[] { 0.006, 0.006, 0.006 } : new[] { 0.02, 0.012, 0.008, 0.006, 0.006 })
            {
                var parovi = Saglasni(p, s, tekst, krugovi, vod, usp, tol, brojReci, sveReci);
                p.Parovi = parovi;
                Fit(p, parovi);
            }
            p.Parovi = Saglasni(p, s, tekst, krugovi, vod, usp, 0.006, brojReci, sveReci);
            Oceni(p, prag);
            return p;
        }

        /// <summary>Odštampani brojevi rubrika: 1–2 cifre, sitan font (PDF) ili OCR reč; ne vrednosti polja obrasca.</summary>
        private static List<Rec> BrojeviRubrika(List<Rec> reci) =>
            reci.Where(w => !w.JePodatak && System.Text.RegularExpressions.Regex.IsMatch(w.T, @"^\d{1,2}$") && (w.Velicina == 0 || w.Velicina < 6.5)).ToList();

        private static (double ax, double bx, double ay, double by)? Hipoteza(List<ParSidra> t, int max = 120, int minSaglasnih = 3)
        {
            if (t.Count < 2) return null;
            var parovi = t.Take(max).ToList();
            (double ax, double bx, double ay, double by)? najbolja = null;
            int najvise = 0; double najmanje = double.MaxValue;
            void Proveri(double ax, double bx, double ay, double by)
            {
                if (ax < 0.7 || ax > 1.45 || ay < 0.7 || ay > 1.45) return;
                int n = 0; double suma = 0;
                foreach (var q in parovi)
                {
                    double ex = Math.Abs(ax * q.Px + bx - q.Tx), ey = Math.Abs(ay * q.Py + by - q.Ty);
                    if (ex < 0.015 && ey < 0.015) { n++; suma += ex + ey; }
                }
                if (n > najvise || (n == najvise && suma < najmanje)) { najvise = n; najmanje = suma; najbolja = (ax, bx, ay, by); }
            }
            for (int i = 0; i < parovi.Count; i++)
            {
                Proveri(1, parovi[i].Tx - parovi[i].Px, 1, parovi[i].Ty - parovi[i].Py);
                for (int j = i + 1; j < parovi.Count; j++)
                {
                    var a = parovi[i]; var b = parovi[j];
                    double dx = b.Px - a.Px, dy = b.Py - a.Py;
                    if (Math.Abs(dx) < 0.15 || Math.Abs(dy) < 0.15) continue;
                    var ax = (b.Tx - a.Tx) / dx; var ay = (b.Ty - a.Ty) / dy;
                    Proveri(ax, a.Tx - ax * a.Px, ay, a.Ty - ay * a.Py);
                }
            }
            return najvise >= minSaglasnih ? najbolja : null;
        }

        /// <summary>Kada OCR ne pročita dovoljno teksta: pomak iz krugova brojeva rubrika (razmera 1).</summary>
        private static (double ax, double bx, double ay, double by)? HipotezaKrugovi(ObrazacSablon s, List<Oblik> krugovi)
        {
            if (krugovi.Count < 5 || s.Brojevi.Count < 5) return null;
            var brojevi = s.Brojevi.Values.ToList();
            (double, double, double, double)? najbolja = null; int najvise = 0;
            foreach (var k in krugovi.Take(80))
                foreach (var b in brojevi)
                {
                    double bx = b.x - k.Cx, by = b.y - k.Cy;
                    if (Math.Abs(bx) > 0.1 || Math.Abs(by) > 0.1) continue;
                    var n = krugovi.Count(q => brojevi.Any(t => Math.Abs(q.Cx + bx - t.x) < 0.008 && Math.Abs(q.Cy + by - t.y) < 0.008));
                    if (n > najvise) { najvise = n; najbolja = (1, bx, 1, by); }
                }
            return najvise >= 6 ? najbolja : null;
        }

        private static List<ParSidra> Saglasni(Poravnanje p, ObrazacSablon s, List<ParSidra> tekst, List<Oblik> krugovi, List<LinijaOkvira> vod, List<LinijaOkvira> usp, double tol,
            List<Rec> brojReci = null, List<Rec> reci = null)
        {
            var rez = new List<ParSidra>();
            foreach (var t in tekst)
            {
                var (x, y) = p.USablon(t.Px, t.Py);
                if (Math.Abs(x - t.Tx) < tol * 1.5 && Math.Abs(y - t.Ty) < tol * 1.5)
                    rez.Add(new ParSidra { Vrsta = t.Vrsta, Oznaka = t.Oznaka, Px = t.Px, Py = t.Py, Tx = t.Tx, Ty = t.Ty, Odstupanje = Math.Sqrt((x - t.Tx) * (x - t.Tx) + (y - t.Ty) * (y - t.Ty)) });
            }
            // Oznaka „© 200x CIT“ (univerzalni šablon)
            if (s.CitOznaka.HasValue && reci != null)
            {
                var (cx, cy) = p.NaStranu(s.CitOznaka.Value.x, s.CitOznaka.Value.y);
                var w = reci.Where(r => r.T.IndexOf("CIT", StringComparison.Ordinal) >= 0).OrderBy(r => Math.Abs(r.Cx - cx) + Math.Abs(r.Cy - cy)).FirstOrDefault();
                if (w != null && Math.Abs(w.Cx - cx) < tol * 2 && Math.Abs(w.Cy - cy) < tol * 2)
                {
                    var (x, y) = p.USablon(w.Cx, w.Cy);
                    rez.Add(new ParSidra { Vrsta = "broj", Oznaka = "CIT", Px = w.Cx, Py = w.Cy, Tx = s.CitOznaka.Value.x, Ty = s.CitOznaka.Value.y, Odstupanje = Math.Sqrt(Math.Pow(x - s.CitOznaka.Value.x, 2) + Math.Pow(y - s.CitOznaka.Value.y, 2)) });
                }
            }
            foreach (var kv in s.Brojevi.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var (px, py) = p.NaStranu(kv.Value.x, kv.Value.y);
                // broj rubrike iz teksta (univerzalni šablon) – ima prednost nad krugom
                if (brojReci != null && brojReci.Count > 0)
                {
                    var osnova = ObrazacSablon.OsnovaBroja(kv.Key);
                    var w = brojReci.Where(r => r.T == osnova).OrderBy(r => Math.Abs(r.Cx - px) + Math.Abs(r.Cy - py)).FirstOrDefault();
                    if (w != null && Math.Sqrt((w.Cx - px) * (w.Cx - px) + (w.Cy - py) * (w.Cy - py)) < tol)
                    {
                        var (x, y) = p.USablon(w.Cx, w.Cy);
                        rez.Add(new ParSidra { Vrsta = "broj", Oznaka = kv.Key, Px = w.Cx, Py = w.Cy, Tx = kv.Value.x, Ty = kv.Value.y, Odstupanje = Math.Sqrt((x - kv.Value.x) * (x - kv.Value.x) + (y - kv.Value.y) * (y - kv.Value.y)) });
                        continue;
                    }
                }
                Oblik naj = null; double d = double.MaxValue;
                foreach (var k in krugovi)
                {
                    var dd = Math.Sqrt((k.Cx - px) * (k.Cx - px) + (k.Cy - py) * (k.Cy - py));
                    if (dd < d) { d = dd; naj = k; }
                }
                if (naj != null && d < tol)
                {
                    var (x, y) = p.USablon(naj.Cx, naj.Cy);
                    rez.Add(new ParSidra { Vrsta = "broj", Oznaka = kv.Key, Px = naj.Cx, Py = naj.Cy, Tx = kv.Value.x, Ty = kv.Value.y, Odstupanje = Math.Sqrt((x - kv.Value.x) * (x - kv.Value.x) + (y - kv.Value.y) * (y - kv.Value.y)) });
                }
            }
            // Linije okvira: linija šablona ↔ najbliža linija strane iste orijentacije, sa preklapanjem bar pola dužine.
            foreach (var l in s.Vodoravne.Where(l => l.Duzina >= 0.06))
            {
                var (ax, ay) = p.NaStranu(l.B0, l.A); var (bx, by) = p.NaStranu(l.B1, l.A);
                double y = (ay + by) / 2, x0 = Math.Min(ax, bx), x1 = Math.Max(ax, bx);
                LinijaOkvira naj = null; double d = double.MaxValue;
                foreach (var q in vod)
                {
                    var preklop = Math.Min(x1, q.B1) - Math.Max(x0, q.B0);
                    if (preklop < (x1 - x0) * 0.5) continue;
                    var dd = Math.Abs(q.A - y);
                    if (dd < d) { d = dd; naj = q; }
                }
                if (naj == null || d >= tol) continue;
                var cx = (Math.Max(x0, naj.B0) + Math.Min(x1, naj.B1)) / 2;
                var (_, ty) = p.USablon(cx, naj.A);
                rez.Add(new ParSidra { Vrsta = "vodoravna", Oznaka = l.ToString(), Px = cx, Py = naj.A, Tx = double.NaN, Ty = l.A, Odstupanje = Math.Abs(ty - l.A) });
            }
            foreach (var l in s.Uspravne.Where(l => l.Duzina >= 0.04))
            {
                var (ax, ay) = p.NaStranu(l.A, l.B0); var (bx, by) = p.NaStranu(l.A, l.B1);
                double x = (ax + bx) / 2, y0 = Math.Min(ay, by), y1 = Math.Max(ay, by);
                LinijaOkvira naj = null; double d = double.MaxValue;
                foreach (var q in usp)
                {
                    var preklop = Math.Min(y1, q.B1) - Math.Max(y0, q.B0);
                    if (preklop < (y1 - y0) * 0.5) continue;
                    var dd = Math.Abs(q.A - x);
                    if (dd < d) { d = dd; naj = q; }
                }
                if (naj == null || d >= tol) continue;
                var cy = (Math.Max(y0, naj.B0) + Math.Min(y1, naj.B1)) / 2;
                var (tx, _) = p.USablon(naj.A, cy);
                rez.Add(new ParSidra { Vrsta = "uspravna", Oznaka = l.ToString(), Px = naj.A, Py = cy, Tx = l.A, Ty = double.NaN, Odstupanje = Math.Abs(tx - l.A) });
            }
            return rez;
        }

        /// <summary>Najmanji kvadrati po osi: tx iz (x, y, 1) za sidra sa poznatim tx, ty isto. Nagib se računa samo uz dovoljno sidara.</summary>
        private static void Fit(Poravnanje p, List<ParSidra> parovi)
        {
            var zaX = parovi.Where(q => !double.IsNaN(q.Tx)).Select(q => (q.Px, q.Py, t: q.Tx)).ToList();
            var zaY = parovi.Where(q => !double.IsNaN(q.Ty)).Select(q => (q.Px, q.Py, t: q.Ty)).ToList();
            var nx = Resi(zaX, true); if (nx != null) p.X = nx;
            var ny = Resi(zaY, false); if (ny != null) p.Y = ny;
        }

        /// <summary>Rešenje za t = a·x + b·y + c; ako je tačaka malo ili su u jednom pravcu, samo razmera i pomak glavne ose.</summary>
        private static double[] Resi(List<(double x, double y, double t)> d, bool zaX)
        {
            if (d.Count < 3) return null;
            if (d.Count >= 10)
            {
                double sxx = 0, sxy = 0, syy = 0, sx = 0, sy = 0, sxt = 0, syt = 0, st = 0; int n = d.Count;
                foreach (var (x, y, t) in d) { sxx += x * x; sxy += x * y; syy += y * y; sx += x; sy += y; sxt += x * t; syt += y * t; st += t; }
                var m = new[,] { { sxx, sxy, sx }, { sxy, syy, sy }, { sx, sy, n } };
                var v = new[] { sxt, syt, st };
                var r = Gaus(m, v);
                if (r != null)
                {
                    var glavna = zaX ? r[0] : r[1]; var sporedna = zaX ? r[1] : r[0];
                    if (glavna > 0.6 && glavna < 1.6 && Math.Abs(sporedna) < 0.06) return r;
                }
            }
            // samo glavna osa
            var g = d.Select(q => (o: zaX ? q.x : q.y, q.t)).ToList();
            double so = g.Sum(q => q.o), stt = g.Sum(q => q.t), soo = g.Sum(q => q.o * q.o), sot = g.Sum(q => q.o * q.t);
            var den = g.Count * soo - so * so;
            double a = Math.Abs(den) < 1e-9 ? 1 : (g.Count * sot - so * stt) / den;
            if (a < 0.6 || a > 1.6) a = 1;
            var c = (stt - a * so) / g.Count;
            return zaX ? new[] { a, 0, c } : new[] { 0, a, c };
        }

        private static double[] Gaus(double[,] m, double[] v)
        {
            int n = v.Length;
            var a = (double[,])m.Clone(); var b = (double[])v.Clone();
            for (int i = 0; i < n; i++)
            {
                int piv = i;
                for (int k = i + 1; k < n; k++) if (Math.Abs(a[k, i]) > Math.Abs(a[piv, i])) piv = k;
                if (Math.Abs(a[piv, i]) < 1e-12) return null;
                if (piv != i) { for (int k = 0; k < n; k++) { var t = a[i, k]; a[i, k] = a[piv, k]; a[piv, k] = t; } var tb = b[i]; b[i] = b[piv]; b[piv] = tb; }
                for (int k = i + 1; k < n; k++)
                {
                    var f = a[k, i] / a[i, i];
                    for (int j = i; j < n; j++) a[k, j] -= f * a[i, j];
                    b[k] -= f * b[i];
                }
            }
            var x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                var s = b[i];
                for (int j = i + 1; j < n; j++) s -= a[i, j] * x[j];
                x[i] = s / a[i, i];
            }
            return x;
        }

        private static void Oceni(Poravnanje p, PragoviPoravnanja prag)
        {
            p.Tekst = p.Parovi.Count(q => q.Vrsta == "tekst");
            p.Brojevi = p.Parovi.Count(q => q.Vrsta == "broj");
            p.Linije = p.Parovi.Count(q => q.Vrsta == "vodoravna" || q.Vrsta == "uspravna");
            p.PoTrecinama = new int[3];
            foreach (var q in p.Parovi)
            {
                var ty = !double.IsNaN(q.Ty) ? q.Ty : p.USablon(q.Px, q.Py).y;
                p.PoTrecinama[Math.Max(0, Math.Min(2, (int)(ty * 3)))]++;
            }
            p.Odstupanje = p.Parovi.Count == 0 ? 1 : Math.Sqrt(p.Parovi.Average(q => q.Odstupanje * q.Odstupanje));
            var razlozi = new List<string>();
            if (p.Ukupno < prag.MinSidara) razlozi.Add("sidara " + p.Ukupno + " < " + prag.MinSidara);
            if (p.PoTrecinama.Any(x => x < prag.MinPoTrecini)) razlozi.Add("trećina strane sa manje od " + prag.MinPoTrecini + " sidra");
            if (p.Odstupanje > prag.MaxOdstupanje) razlozi.Add("odstupanje " + p.Odstupanje.ToString("0.0000", CultureInfo.InvariantCulture) + " > " + prag.MaxOdstupanje.ToString("0.000", CultureInfo.InvariantCulture));
            // univerzalni šablon: tekstualna sidra su samo dodatna (drugi jezik/izdanje) – njihov udeo se ne ocenjuje
            if (!p.Univerzalni && p.TekstKandidata >= 8 && p.Tekst < p.TekstKandidata * prag.MinUdeoTeksta)
                razlozi.Add("sidra se ne slažu (" + p.Tekst + " od " + p.TekstKandidata + " tekstualnih) – druga grupa sidara daje drugo poravnanje");
            if (p.X[0] < 0.6 || p.X[0] > 1.6 || p.Y[1] < 0.6 || p.Y[1] > 1.6) razlozi.Add("razmera van granica");
            p.Pouzdano = razlozi.Count == 0;
            p.Razlog = string.Join("; ", razlozi);
        }
    }

    /// <summary>Svi poznati obrasci; za stranu bira onaj sa najviše saglasnih sidara.</summary>
    internal sealed class ObrasciSkup
    {
        public readonly List<ObrazacSablon> Sabloni = new List<ObrazacSablon>();
        public PragoviPoravnanja Prag = new PragoviPoravnanja();

        public static ObrasciSkup Ucitaj(string folder, PragoviPoravnanja prag)
        {
            var s = new ObrasciSkup { Prag = prag ?? new PragoviPoravnanja() };
            if (Directory.Exists(folder))
                foreach (var f in Directory.GetFiles(folder, "*.json").OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                    s.Sabloni.Add(ObrazacSablon.Ucitaj(f));
            return s;
        }

        /// <summary>
        /// Svi obrasci, redom od najboljeg: pouzdan nacionalni šablon (fino podešen) ima prednost nad univerzalnim CIT šablonom,
        /// pa najviše sidara, manje odstupanje, id.
        /// </summary>
        public List<Poravnanje> Probaj(List<Rec> reci, Raster raster, List<Rec> sveReci = null) =>
            Sabloni.Select(s => Poravnanje.Izracunaj(s, reci, raster, Prag, sveReci))
                .OrderByDescending(p => p.Pouzdano).ThenBy(p => p.Univerzalni).ThenByDescending(p => p.Ukupno).ThenBy(p => p.Odstupanje).ThenBy(p => p.Obrazac, StringComparer.Ordinal).ToList();

        public ObrazacSablon Po(string id) => Sabloni.FirstOrDefault(s => s.Id == id);

        /// <summary>Poleđina lista (računski odseci): ključne reči poleđine, a bez pouzdanog poravnanja prednje strane.</summary>
        public string Poledjina(string tekstStrane)
        {
            var t = " " + Saobracaj.Common.Text.TekstUtil.Kljuc(tekstStrane) + " ";
            foreach (var s in Sabloni)
                if (s.Poledjina.Count > 0 && s.Poledjina.Count(k => t.Contains(k)) >= 1) return s.Id;
            return OpstaPoledjina.Any(k => t.Contains(Saobracaj.Common.Text.TekstUtil.Kljuc(k))) ? "" : null;
        }

        private static readonly string[] OpstaPoledjina =
            { "frachtberechnungsabschnitte", "racunski odsec", "рачунски одсец", "racunski odsjec", "sections de taxation", "charging sections", "berechnungsabschnitte",
              "sezioni di tassazione", "sezioni tassazione", "fuvardijszamitasi szakasz", "useky vypoctu", "sectiuni de taxare", "tarifni odseki", "ucretlendirme bolumleri" };
    }
}
