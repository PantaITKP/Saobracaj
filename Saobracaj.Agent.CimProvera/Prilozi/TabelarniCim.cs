using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ExcelDataReader;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Model;

namespace Saobracaj.Agent.CimProvera.Prilozi
{
    /// <summary>Jedna ćelija tabele (Excel ili Word): vrsta i kolona od 0.</summary>
    internal sealed class Celija
    {
        public int Red, Kol;
        public string Tekst = "";
        public string Adresa => KolonaSlovo(Kol) + (Red + 1);
        internal static string KolonaSlovo(int k) { var s = ""; k++; while (k > 0) { var m = (k - 1) % 26; s = (char)('A' + m) + s; k = (k - m) / 26; } return s; }
        public override string ToString() => Adresa + "=" + Tekst;
    }

    /// <summary>
    /// Tovarni list CIM/CUV popunjen kao tabela (Excel obrazac, Word tabela). Čita se direktno iz ćelija, bez OCR-a:
    /// broj rubrike je posebna ćelija, a vrednost rubrike je tekst ispod i desno od nje (najbliži broj rubrike gore-levo,
    /// vrsta ima veću težinu od kolone). Natpisi obrasca (rečnik natpisa u cim_sablon.json) nisu vrednost. Cifre upisane
    /// ćelija po ćelija (r12, r17, r62) spajaju se u jednu vrednost. Kućice: „X“ uz natpis CIM/CUV (r30), „ja“ (r22, r23),
    /// Franko/Incoterms (r20). Rubrike se predaju istom parseru kao kod PDF-a, pa važe ista pravila.
    /// </summary>
    internal static class TabelarniCim
    {
        private static readonly Regex Naslov = new Regex(
            @"(fracht ?brief|n[aá]kladn[ií] list|lettre de voiture|fuvarlev[eé]l|tovarni list|товарни лист|consignment note|lettera di vettura|list przewozowy|scrisoare de tr[aă]sur[aă]|wagenbrief|vozov[yý] list|vagonski list|kolski list|carro|wagon note)\W{0,3}(cim|cuv)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly HashSet<int> ZnaceajneRubrike = new HashSet<int> { 1, 4, 7, 10, 13, 16, 18, 20, 21, 24, 25, 29, 57, 62 };
        /// <summary>Kućice za šifre (cifra po cifra) – tekst sa slovima u njima pripada rubrici levo (npr. „Serbia“ u r10).</summary>
        private static readonly HashSet<string> SifarskeKucice = new HashSet<string> { "11", "12", "17", "2", "3", "5", "6" };

        // ------------------------------------------------------------------------------------------- ćelije

        /// <summary>Raspored lista za crtanje primera: širine kolona i visine vrsta (u tačkama) i spojene ćelije (inkluzivno).</summary>
        internal sealed class Raspored
        {
            public List<double> Sirine = new List<double>();
            public List<double> Visine = new List<double>();
            public List<(int r0, int k0, int r1, int k1)> Spojene = new List<(int, int, int, int)>();
        }

        /// <summary>Ćelije svih listova Excel fajla (xls/xlsx/xlsm), po listu.</summary>
        public static List<List<Celija>> IzExcela(byte[] sadrzaj) => IzExcelaSaRasporedom(sadrzaj).Select(x => x.celije).ToList();

        /// <summary>Ćelije i raspored (širine, visine, spojene ćelije) svih listova Excel fajla.</summary>
        public static List<(List<Celija> celije, Raspored raspored)> IzExcelaSaRasporedom(byte[] sadrzaj)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var rez = new List<(List<Celija>, Raspored)>();
            using (var reader = ExcelReaderFactory.CreateReader(new MemoryStream(sadrzaj)))
            {
                do
                {
                    var lista = new List<Celija>();
                    var ras = new Raspored();
                    int i = 0;
                    while (reader.Read())
                    {
                        ras.Visine.Add(reader.RowHeight);
                        for (int j = 0; j < reader.FieldCount; j++)
                        {
                            var v = reader.GetValue(j);
                            if (v == null || v == DBNull.Value) continue;
                            var s = Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? "";
                            if (s.Length > 0) lista.Add(new Celija { Red = i, Kol = j, Tekst = s });
                        }
                        i++;
                    }
                    // širina kolone u znakovima → tačke (≈ 5,6 pt po znaku, kao pri štampi iz Excela)
                    for (int j = 0; j < reader.FieldCount; j++)
                    {
                        double w;
                        try { w = reader.GetColumnWidth(j); } catch { w = 8.43; }
                        ras.Sirine.Add(w * 5.6);
                    }
                    try { if (reader.MergeCells != null) foreach (var m in reader.MergeCells) ras.Spojene.Add((m.FromRow, m.FromColumn, m.ToRow, m.ToColumn)); } catch { }
                    rez.Add((lista, ras));
                } while (reader.NextResult());
            }
            return rez;
        }

        /// <summary>
        /// Oblast svake rubrike u mreži (vrsta/kolona, kraj isključen): od broja rubrike desno do sledećeg broja rubrike u istoj
        /// vrsti (±1) i dole do prvog broja rubrike ispod čija se oblast preklapa po kolonama. Za okvire na PDF primeru.
        /// </summary>
        public static Dictionary<string, (int r0, int k0, int r1, int k1)> Oblasti(List<Celija> celije)
        {
            var sidra = Sidra(celije);
            int maxRed = celije.Max(c => c.Red) + 1, maxKol = celije.Max(c => c.Kol) + 1;
            // desno: sledeći broj rubrike u istoj vrsti (±1) ili do 3 vrste iznad (rubrika desno koja je počela malo više);
            // dole: prvi broj ispod čija se oblast preklapa po kolonama
            var desno = sidra.ToDictionary(s => s.Key, s => sidra.Values.Where(o => o != s.Value && o.Kol > s.Value.Kol
                    && o.Red - s.Value.Red <= 1 && s.Value.Red - o.Red <= 3)
                .Select(o => o.Kol).DefaultIfEmpty(maxKol).Min());
            var rez = new Dictionary<string, (int, int, int, int)>();
            foreach (var s in sidra)
            {
                int r0 = s.Value.Red, k0 = s.Value.Kol, k1 = desno[s.Key];
                var r1 = sidra.Where(o => o.Key != s.Key && o.Value.Red > r0 && !(o.Value.Red - r0 <= 1 && o.Value.Kol > k0) && o.Value.Kol < k1 && desno[o.Key] > k0)
                    .Select(o => o.Value.Red).DefaultIfEmpty(maxRed).Min();
                rez[s.Key] = (r0, k0, Math.Max(r0 + 1, r1), Math.Max(k0 + 1, k1));
            }
            return rez;
        }

        /// <summary>Ćelije tabela iz Word .docx (svaka tabela je mreža vrsta × kolona; gridSpan pomera kolonu).</summary>
        public static List<List<Celija>> IzWorda(byte[] sadrzaj)
        {
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            XDocument doc;
            using (var zip = new ZipArchive(new MemoryStream(sadrzaj), ZipArchiveMode.Read))
            {
                var e = zip.GetEntry("word/document.xml") ?? throw new InvalidDataException("nema word/document.xml");
                using (var s = e.Open()) doc = XDocument.Load(s);
            }
            var rez = new List<List<Celija>>();
            var sve = new List<Celija>();
            int pomak = 0;
            foreach (var tbl in doc.Descendants(w + "tbl").Where(t => !t.Ancestors(w + "tbl").Any()))
            {
                int red = pomak;
                foreach (var tr in tbl.Elements(w + "tr"))
                {
                    int kol = 0;
                    foreach (var tc in tr.Elements(w + "tc"))
                    {
                        var span = (int?)tc.Element(w + "tcPr")?.Element(w + "gridSpan")?.Attribute(w + "val") ?? 1;
                        var tekst = string.Join("\n", tc.Elements(w + "p").Select(p => string.Concat(p.Descendants(w + "t").Select(x => x.Value))).Where(x => x.Trim().Length > 0)).Trim();
                        if (tekst.Length > 0) sve.Add(new Celija { Red = red, Kol = kol, Tekst = tekst });
                        kol += Math.Max(1, span);
                    }
                    red++;
                }
                pomak = red + 1;
            }
            rez.Add(sve);
            return rez;
        }

        // ------------------------------------------------------------------------------------------- prepoznavanje

        /// <summary>
        /// Tabela je tovarni list CIM/CUV: naslov obrasca (bilo koji jezik) i bar 10 brojeva rubrika kao zasebne ćelije
        /// (od toga bar 8 iz osnovnog skupa 1, 4, 7, 10, 13, 16, 18, 20, 21, 24, 25, 29, 57, 62). Ime fajla nije dokaz.
        /// </summary>
        public static bool JeTovarniList(List<Celija> celije, out string razlog)
        {
            var naslov = celije.FirstOrDefault(c => Naslov.IsMatch(c.Tekst));
            var brojevi = Sidra(celije).Keys.ToList();
            var osnovni = brojevi.Count(b => int.TryParse(Regex.Match(b, @"^\d+").Value, out var n) && ZnaceajneRubrike.Contains(n));
            razlog = (naslov != null ? "naslov „" + Regex.Replace(naslov.Tekst, @"\s+", " ") + "“" : "bez naslova obrasca") + ", brojeva rubrika " + brojevi.Count + " (osnovnih " + osnovni + ")";
            return naslov != null && brojevi.Count >= 10 && osnovni >= 8;
        }

        /// <summary>Ćelije u nizu jednocifrenih vrednosti u istoj vrsti (≥ 3 susedne) – šifre upisane cifra po cifra.</summary>
        private static HashSet<Celija> NiziCifara(List<Celija> celije, out List<(int red, int kol, string tekst)> spojeni)
        {
            var u = new HashSet<Celija>();
            spojeni = new List<(int, int, string)>();
            foreach (var red in celije.GroupBy(c => c.Red))
            {
                var cifre = red.Where(c => Regex.IsMatch(c.Tekst, @"^\d$")).OrderBy(c => c.Kol).ToList();
                for (int i = 0; i < cifre.Count;)
                {
                    // susedne ćelije, uz najviše jednu praznu između grupa („8 0 _ 1 0 1 2 8 7“ = zemlja + stanica)
                    int j = i;
                    while (j + 1 < cifre.Count && cifre[j + 1].Kol - cifre[j].Kol <= 2) j++;
                    if (j - i + 1 >= 3)
                    {
                        var niz = cifre.Skip(i).Take(j - i + 1).ToList();
                        foreach (var c in niz) u.Add(c);
                        var s = niz[0].Tekst;
                        for (int k = 1; k < niz.Count; k++) s += (niz[k].Kol - niz[k - 1].Kol > 1 ? " " : "") + niz[k].Tekst;
                        if (!s.Contains(" ") && s.Length == 8) s = s.Substring(0, 2) + " " + s.Substring(2);   // zemlja + stanica: „72 168716“
                        spojeni.Add((niz[0].Red, niz[0].Kol, s));
                    }
                    i = j + 1;
                }
            }
            return u;
        }

        /// <summary>
        /// Brojevi rubrika (sidra): ćelija sa brojem 1–99 (ili „58“ → 58a, „19 Trať“) koja nije deo niza cifara.
        /// Kada se isti broj javlja više puta, važi onaj uz koji je natpis (ćelija desno ili ispod nije broj).
        /// </summary>
        private static Dictionary<string, Celija> Sidra(List<Celija> celije)
        {
            var nizovi = NiziCifara(celije, out _);
            var poAdresi = celije.ToDictionary(c => (c.Red, c.Kol));
            var kandidati = new List<(string id, Celija c, bool natpis)>();
            foreach (var c in celije.Where(x => !nizovi.Contains(x)))
            {
                var m = Regex.Match(c.Tekst, @"^(\d{1,2})(?:\s+\p{L}.*)?$");
                if (!m.Success) continue;
                var n = int.Parse(m.Groups[1].Value);
                if (n < 1 || n > 99 || (n > 30 && n < 40) || (n >= 63 && n <= 69)) continue;
                var id = n == 58 ? "58a" : n.ToString();
                bool Tekst(int r, int k) => poAdresi.TryGetValue((r, k), out var x) && Regex.IsMatch(x.Tekst, @"\p{L}{3}");
                var natpis = m.Groups[0].Value.Length > m.Groups[1].Value.Length || Tekst(c.Red, c.Kol + 1) || Tekst(c.Red, c.Kol + 2) || Tekst(c.Red + 1, c.Kol);
                kandidati.Add((id, c, natpis));
            }
            return kandidati.GroupBy(k => k.id).ToDictionary(g => g.Key, g => (g.FirstOrDefault(x => x.natpis).c ?? g.OrderBy(x => x.c.Red).ThenBy(x => x.c.Kol).First().c));
        }

        // ------------------------------------------------------------------------------------------- čitanje

        /// <summary>
        /// Tovarni list iz ćelija: rubrike (reči po rubrikama) za CimParser i lista dodela (adresa ćelije → rubrika) za primer.
        /// </summary>
        public static CimList Procitaj(List<Celija> celije, Func<string, bool> jeNatpis, CimParser.Pragovi prag, out Dictionary<string, List<Celija>> poRubrikama)
        {
            var sidra = Sidra(celije);
            var nizovi = NiziCifara(celije, out var spojeni);
            int maxRed = Math.Max(1, celije.Max(c => c.Red) + 1), maxKol = Math.Max(1, celije.Max(c => c.Kol) + 1);
            poRubrikama = new Dictionary<string, List<Celija>>();
            var reci = new Dictionary<string, List<Rec>>();
            var sidroCelije = new HashSet<Celija>(sidra.Values);

            // vrednosti: obične ćelije (bez sidara i pojedinačnih cifara iz nizova) + spojeni nizovi cifara
            // ponovljeni brojevi rubrika (odeljci A/B/C sa 70–79, oznaka primerka „1“) nisu vrednosti
            var vrednosti = celije.Where(c => !sidroCelije.Contains(c) && !nizovi.Contains(c) && !(Regex.IsMatch(c.Tekst, @"^\d{1,2}$") && sidra.ContainsKey(c.Tekst))).ToList();
            vrednosti.AddRange(spojeni.Select(s => new Celija { Red = s.red, Kol = s.kol, Tekst = s.tekst }));

            // najbliži broj rubrike gore-levo: vrsta teži 1, kolona 0,25; šifarske kućice ne primaju tekst sa slovima
            double Rastojanje(Celija od, Celija c) => (c.Red - od.Red) + 0.25 * (c.Kol - od.Kol);
            string Sidro(Celija c, bool slova) =>
                sidra.Where(s => s.Value.Red <= c.Red && s.Value.Kol <= c.Kol && !(slova && SifarskeKucice.Contains(s.Key) && s.Value.Red < c.Red - 1))
                    .OrderBy(s => Rastojanje(s.Value, c)).Select(s => s.Key).FirstOrDefault();
            // natpisi obrasca su putokazi: svaki pripada rubrici svog broja, a vrednost ide najbližem broju ili natpisu gore-levo
            var natpisi = celije.Where(c => !sidroCelije.Contains(c) && !nizovi.Contains(c) && !Regex.IsMatch(c.Tekst, @"^[xX×]$") && jeNatpis(c.Tekst))
                .Select(c => (c, rub: Sidro(c, true))).Where(x => x.rub != null).ToList();
            string Rubrika(Celija c, bool slova)
            {
                var s = Sidro(c, slova);
                var dS = s == null ? double.MaxValue : Rastojanje(sidra[s], c);
                var n = natpisi.Where(x => x.c.Red <= c.Red && x.c.Kol <= c.Kol && !(slova && SifarskeKucice.Contains(x.rub) && x.c.Red < c.Red - 1))
                    .OrderBy(x => Rastojanje(x.c, c)).FirstOrDefault();
                return n.c != null && Rastojanje(n.c, c) < dS ? n.rub : s;
            }

            foreach (var c in vrednosti.OrderBy(x => x.Red).ThenBy(x => x.Kol))
            {
                var tekst = c.Tekst;
                if (!Regex.IsMatch(tekst, @"^[xX×]$") && jeNatpis(tekst)) continue;   // natpis obrasca
                var id = Rubrika(c, Regex.IsMatch(tekst, @"\p{L}{2}"));
                if (id == null) continue;
                // kućice: X uz natpis
                if (Regex.IsMatch(tekst, @"^[xX×]$"))
                {
                    var susedi = celije.Where(s => Math.Abs(s.Red - c.Red) <= 1 && Math.Abs(s.Kol - c.Kol) <= 8 && s != c && Regex.IsMatch(s.Tekst, @"\p{L}{2}")).OrderBy(s => Math.Abs(s.Kol - c.Kol) + 2 * Math.Abs(s.Red - c.Red)).ToList();
                    if (id == "30")
                    {
                        var levo = susedi.Where(s => s.Kol < c.Kol).OrderBy(s => c.Kol - s.Kol).FirstOrDefault(s => Regex.IsMatch(s.Tekst, @"\b(CIM|CUV)\b"));
                        id = levo != null && Regex.IsMatch(levo.Tekst, @"\bCUV\b") ? "30cuv" : "30cim";
                        tekst = "X";
                    }
                    else if (id == "20")
                    {
                        var natpis = susedi.FirstOrDefault(s => Regex.IsMatch(s.Tekst, @"franko|franco|fracht|vyplaceno|incoterms", RegexOptions.IgnoreCase));
                        tekst = natpis == null ? "X" : Regex.IsMatch(natpis.Tekst, "incoterms", RegexOptions.IgnoreCase) ? "Incoterms (X)" : "Franko prevoznina (X)";
                    }
                    else tekst = "X";
                }
                if (!poRubrikama.TryGetValue(id, out var lc)) poRubrikama[id] = lc = new List<Celija>();
                lc.Add(c);
                if (!reci.TryGetValue(id, out var lr)) reci[id] = lr = new List<Rec>();
                // reči u koordinatama „strane“ (vrsta/kolona tabele), red po red ćelije
                // novi red u ćeliji, ili niz od 3+ razmaka (Excel obrasci često poravnavaju redove razmacima)
                var linije = Regex.Split(tekst.Replace("\r", ""), @"\n|[ \t]{3,}").Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
                for (int li = 0; li < linije.Count; li++)
                {
                    double y0 = (c.Red + li * 0.3) / maxRed, y1 = y0 + 0.25 / maxRed, x = (double)c.Kol / maxKol;
                    foreach (Match w in Regex.Matches(linije[li], @"\S+"))
                    {
                        var x0 = x + w.Index * 0.0035;
                        lr.Add(new Rec { T = w.Value, X0 = x0, X1 = x0 + w.Length * 0.0035, Y0 = y0, Y1 = y1, Pouzdanost = 1, Velicina = 9, Font = "Tabela" });
                    }
                }
            }

            var rez = new CimSablon.Rezultat();
            foreach (var kv in reci) rez.Rubrike[kv.Key] = kv.Value;
            var strana = new Citanje.Strana { Broj = 1, Izvor = IzvorTeksta.Tekst };
            strana.Reci.AddRange(reci.Values.SelectMany(x => x));
            strana.Reci.AddRange(celije.Where(c => jeNatpis(c.Tekst)).Select(c => new Rec { T = c.Tekst, Pouzdanost = 1, Font = "Tabela" }));
            var cim = CimParser.Napravi(rez, strana, prag);
            cim.Sablon = "TABELA";
            // r62: broj otpravljanja može biti alfanumerički („HER-SAD79“) – uzima se cela vrednost ćelije
            if (poRubrikama.TryGetValue("62", out var c62))
            {
                var broj = c62.Select(x => x.Tekst.Trim()).FirstOrDefault(x => Regex.IsMatch(x, @"^[A-Za-z0-9][A-Za-z0-9\-/ ]{3,15}$") && Regex.IsMatch(x, @"\p{L}") && Regex.IsMatch(x, @"\d"));
                if (broj != null) cim.R62.Broj = broj;
            }
            cim.Zaglavlje = string.Join(" ", celije.Where(c => c.Red <= 3).Select(c => c.Tekst));
            // čitanje iz ćelija je pouzdano: nema nečitljivih rubrika zbog OCR-a
            CimParser.BezNecitljivih(cim);
            return cim;
        }
    }
}
