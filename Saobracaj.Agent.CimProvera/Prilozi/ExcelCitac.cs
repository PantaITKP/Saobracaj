using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ExcelDataReader;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Common.Text;

namespace Saobracaj.Agent.CimProvera.Prilozi
{
    /// <summary>
    /// Excel (spisak kola CIT 23, tabela MRN) se čita direktno iz fajla: brojevi kola (12 cifara), UTI (ISO 6346), MRN,
    /// RID kolona, identifikacija pošiljke i oznaka KP. Interni obrasci KP (K-200, KOL-65, teretnica) i najave se
    /// prepoznaju po imenu fajla. Ista analiza redova (<see cref="AnalizaRedova"/>) koristi se i za spisak kola iz PDF-a/OCR-a.
    /// </summary>
    internal static class ExcelCitac
    {
        static ExcelCitac()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        private static readonly string[] InterniKljucevi = { "k200", "k-200", "kol65", "kol-65", "teretnica", "propratnica" };

        /// <summary>Vrsta po imenu fajla (interni obrasci KP, najava) ili null.</summary>
        internal static string TipPoImenu(string naziv)
        {
            var ime = TekstUtil.Kljuc(Path.GetFileNameWithoutExtension(naziv ?? "")).Replace(" ", "");
            if (InterniKljucevi.Any(k => ime.Contains(k.Replace("-", "")))) return TipDokumenta.Interni;
            var putanja = TekstUtil.Kljuc(naziv ?? "");
            if (new[] { "najava", "наява", "announcement", "avizo", "avis " }.Any(k => putanja.Contains(k))) return TipDokumenta.Najava;
            return null;
        }

        public static List<Dokument> Procitaj(Prilog p, PraviloKp kp)
        {
            var tip = TipPoImenu(p.Naziv);
            if (tip != null)
                return new List<Dokument> { new Dokument { Tip = tip, Strane = { 1 }, Opis = tip == TipDokumenta.Interni ? "interni obrazac KP (po imenu fajla)" : "najava (po imenu fajla)", Fajl = p.Naziv, Izvor = "excel" } };

            DataSet ds;
            using (var reader = ExcelReaderFactory.CreateReader(new MemoryStream(p.Sadrzaj)))
                ds = reader.AsDataSet();

            var redovi = new List<string[]>();
            foreach (DataTable t in ds.Tables)
                foreach (DataRow row in t.Rows)
                    redovi.Add(SpojeniBrojKola(row.ItemArray.Select(c => Convert.ToString(c, System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? "").ToArray()));
            return AnalizaRedova(redovi, kp, p.Naziv, "excel", "Excel");
        }

        /// <summary>
        /// Broj kola upisan u pet susednih ćelija (UIC: 2 | 2 | 4 | 3 | 1, npr. „37 | 84 | 4956 | 011 | 9“, HSL Wagenliste):
        /// spojeni 12-cifreni broj se dodaje kao nova ćelija reda.
        /// </summary>
        internal static string[] SpojeniBrojKola(string[] red)
        {
            var dodati = new List<string>();
            for (int i = 0; i + 4 < red.Length; i++)
                if (Regex.IsMatch(red[i], @"^\d{2}$") && Regex.IsMatch(red[i + 1], @"^\d{2}$") && Regex.IsMatch(red[i + 2], @"^\d{4}$") && Regex.IsMatch(red[i + 3], @"^\d{3}$") && Regex.IsMatch(red[i + 4], @"^\d$"))
                { dodati.Add(red[i] + red[i + 1] + red[i + 2] + red[i + 3] + red[i + 4]); i += 4; }
            return dodati.Count == 0 ? red : red.Concat(dodati).ToArray();
        }

        private static readonly Regex MrnRx = new Regex(@"\b\d{2}[A-Z]{2}[A-Z0-9]{14}\b", RegexOptions.Compiled);
        private static readonly Regex UtiRx = new Regex(@"\b[A-Z]{3}[UJZ]\s?-?\d{6}\s?-?\d\b", RegexOptions.Compiled);
        private static readonly Regex R62Rx = new Regex(@"\b(\d{2})\s*[-/ ]\s*(\d{5,7})\s*[-/ ]\s*(\d{4})\s*[-/ ]\s*(\d{5}\s*-?\s*\d)\b", RegexOptions.Compiled);
        internal static readonly string[] SpisakKljucevi =
            { "cit 23", "cit23", "cit-23", "spisak kola", "wagon list", "list of wagons", "wagenliste", "liste des wagons", "vagon listesi", "списак кола", "vykaz vozu" };

        /// <summary>
        /// Na spisku je upisana masa kola: kolona/natpis mase (bruto, neto, tara, masa, Gewicht, weight, poids, tömeg, kg) i bar jedan
        /// red sa brojem mase (3–6 cifara, ili sa decimalama/„t“/„kg“).
        /// </summary>
        /// <summary>
        /// v2.8.2: zbir masa sa spiska kola po kolonama „Tara“ / „Neto (Masa inc.)“ / „Bruto“ – samo vrste sa brojem kola.
        /// Vrednosti ispod 200 su u tonama (preračunavaju se u kg). null kada kolone nema ili u njoj nema nijednog broja.
        /// </summary>
        internal static (double? tara, double? neto, double? bruto) MaseKolona(List<string[]> redovi, Func<string, string> brojKola)
        {
            // red zaglavlja: bar dve kolone mase, ili kolona mase uz kolonu broja kola (rum. „Tara“ sama u zaglavlju lista znači „zemlja“)
            int kT = -1, kN = -1, kB = -1;
            foreach (var r in redovi)
            {
                int t0 = -1, n0 = -1, b0 = -1; var kola = false;
                for (int i = 0; i < r.Length; i++)
                {
                    var c = TekstUtil.Kljuc(r[i] ?? "");
                    if (t0 < 0 && Regex.IsMatch(c, @"^(tara|tare|tara vg|tara kola|eigengewicht)\b")) t0 = i;
                    else if (n0 < 0 && Regex.IsMatch(c, @"^(neto|netto|net|masa inc|masa incarcaturii|masa robe|weight of goods|ladegewicht)\b")) n0 = i;
                    else if (b0 < 0 && Regex.IsMatch(c, @"^(bruto|brutto|gross|masa bruta|bruttogewicht)\b")) b0 = i;
                    if (Regex.IsMatch(c, @"\b(vagon|vagona|wagon|wagen|kola|nr vagon|wagon no|wagen nr)\b")) kola = true;
                }
                var broj = (t0 >= 0 ? 1 : 0) + (n0 >= 0 ? 1 : 0) + (b0 >= 0 ? 1 : 0);
                if (broj >= 2 || (broj == 1 && kola)) { kT = t0; kN = n0; kB = b0; break; }
            }
            double? Zbir(int k)
            {
                if (k < 0) return null;
                double s = 0; int n = 0; double max = 0;
                foreach (var r in redovi.Where(x => x.Any(c => brojKola(c) != null)))
                {
                    if (k >= r.Length) continue;
                    var t = Regex.Replace(r[k] ?? "", @"\s", "").Replace(",", ".");
                    if (!double.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) || v <= 0) continue;
                    s += v; n++; max = Math.Max(max, v);
                }
                if (n == 0) return null;
                return max < 200 ? Math.Round(s * 1000) : Math.Round(s);
            }
            return (Zbir(kT), Zbir(kN), Zbir(kB));
        }

        internal static bool MasaNaSpisku(IEnumerable<string> redovi)
        {
            var lista = redovi.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            var natpis = lista.Any(r => Regex.IsMatch(r, @"(?<!\p{L})(bruto|brutto|gross|neto|netto|net|tara|tare|masa|mase|težina|tezina|gewicht|weight|poids|t[öo]meg|kg)(?!\p{L})", RegexOptions.IgnoreCase));
            var broj = lista.Count(r => Regex.IsMatch(r, @"(?<![\d.,])\d{1,3}(?:[.,]\d{1,3})\s*(?:t|kg)?(?![\d.,])|(?<![\d.,])\d{4,6}\s*kg\b|(?<![\d.,/-])\d{4,5}(?![\d.,/-])", RegexOptions.IgnoreCase));
            return natpis && broj >= 1;
        }

        /// <summary>Spisak kola i/ili tabela MRN iz redova tabele (ćelije). Izvor: "excel" ili "tekst"/"ocr".</summary>
        internal static List<Dokument> AnalizaRedova(List<string[]> redovi, PraviloKp kp, string fajl, string izvor, string opisIzvora, double pouzdanost = 0.9)
        {
            var rez = new List<Dokument>();
            var spisak = new SpisakKola();
            var carinski = new CarinskiPodaci();
            bool kljucSpiska = false;
            var sviTekstovi = new List<string>();
            int ridKolona = -1;
            // v2.9.0: red (ćelije) u kome su kola prvi put navedena – vrednosti po kolima za teretnicu
            var redKola = new Dictionary<string, string[]>();

            foreach (var celije in redovi)
            {
                var red = string.Join(" | ", celije.Where(c => c.Length > 0));
                if (red.Length == 0) continue;
                sviTekstovi.Add(red);
                var redKljuc = TekstUtil.Kljuc(red);
                if (SpisakKljucevi.Any(k => redKljuc.Contains(TekstUtil.Kljuc(k)))) kljucSpiska = true;

                for (int i = 0; i < celije.Length; i++)
                    if (ridKolona < 0 && Regex.IsMatch(celije[i], @"^\s*RID\b", RegexOptions.IgnoreCase)) ridKolona = i;

                var kola = celije.Select(BrojKola).Where(x => x != null).Distinct().ToList();
                var uti = UtiRx.Matches(red).Cast<Match>().Select(m => Regex.Replace(m.Value, @"[\s-]", "")).Distinct().ToList();
                var mrn = MrnRx.Matches(red.ToUpperInvariant()).Cast<Match>().Select(m => m.Value).Distinct().ToList();
                // v2.9.1: NHM tovarne jedinice (red prikolice/kontejnera, i u redu kola)
                if ((uti.Count > 0 || celije.Any(c => Regex.IsMatch((c ?? "").Trim(), @"^[A-Z]{4}\s?\d{6,7}$"))) && celije.FirstOrDefault(c => Regex.IsMatch(c ?? "", @"^\d{6}$") && !c.StartsWith("0") && BrojKola(c) == null) is string nhmJ) spisak.NhmJedinica.Add(nhmJ);

                foreach (var m in mrn)
                {
                    if (!carinski.Mrn.Contains(m)) carinski.Mrn.Add(m);
                    var uz = uti.FirstOrDefault() ?? kola.FirstOrDefault();
                    if (uz != null) carinski.MrnPoUti.Add(new Mrn { Broj = m, Uz = uz });
                }
                foreach (var k in kola)
                {
                    if (spisak.Kola.Any(x => x.Broj == k)) continue;
                    var stavka = new KolaNaSpisku { Broj = k, Uti = uti, Mrn = mrn };
                    if (ridKolona >= 0 && ridKolona < celije.Length)
                        stavka.Rid = Regex.IsMatch(celije[ridKolona], @"^(x|х|da|yes|ja|oui)$", RegexOptions.IgnoreCase);
                    var nhm = celije.FirstOrDefault(c => Regex.IsMatch(c, @"^\d{6}$") && !c.StartsWith("0") && BrojKola(c) == null);
                    // "Fly ashes, NHM 2620" / "NHM: 2620 00": šifra uz oznaku NHM (i kada nema 6 cifara – proverava X29)
                    if (nhm == null)
                        nhm = celije.Select(c => Regex.Match(c, @"\b(?:NHM|HS|NHM-?code)\b\W{0,3}(\d{2}(?:[ .]?\d{2}){1,3})(?![\d.,])", RegexOptions.IgnoreCase))
                            .Where(m => m.Success).Select(m => Regex.Replace(m.Groups[1].Value, @"[ .]", "")).FirstOrDefault();
                    if (nhm != null) { stavka.Nhm = nhm; stavka.Prazna = nhm.StartsWith("9921") || nhm.StartsWith("9922"); }
                    // v2.8.1: kola označena kao prazna u svojoj vrsti („Empty“, „Leer“, „prazna“, rum. „gol“)
                    if (celije.Any(c => Regex.IsMatch(c.Trim(), @"^(empty|leer|leere?|prazn[ao]?|gol|goale|vide|[üu]res)$", RegexOptions.IgnoreCase))) stavka.Prazna = true;
                    spisak.Kola.Add(stavka);
                    redKola[k] = celije;
                }
            }
            if (spisak.Kola.Any(k => k.Rid)) spisak.RidOznaka = true;
            try { PodaciPoKolima(redovi, spisak, redKola); ZaglavljeSpiska(redovi, spisak, opisIzvora); }
            catch { /* v2.9.0: podaci za teretnicu su dodatak – greška ne sme da promeni proveru */ }

            var sve = string.Join("\n", sviTekstovi);
            spisak.UnBrojevi = Regex.Matches(sve.ToUpperInvariant(), @"\bUN\s?-?(\d{4})\b").Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList();
            var r62 = R62Rx.Match(sve);
            if (r62.Success)
                spisak.R62 = new Identifikacija
                {
                    Status = StatusRubrike.Popunjeno, Tekst = r62.Value, Pouzdanost = pouzdanost,
                    Zemlja = r62.Groups[1].Value, Stanica = r62.Groups[2].Value, Preduzece = r62.Groups[3].Value,
                    Broj = Regex.Replace(r62.Groups[4].Value, @"\s", ""),
                };
            spisak.KpNaveden = sviTekstovi.Any(kp.JeKp);
            spisak.MasaUpisana = MasaNaSpisku(sviTekstovi);
            // v2.8.2: zbir masa po kolonama (za upis prave vrednosti u r25 PDF primera)
            var (tara, neto, bruto) = MaseKolona(redovi, BrojKola);
            spisak.TaraKg = tara; spisak.NetoKg = neto; spisak.BrutoKg = bruto;

            if (spisak.Kola.Count > 0 && (kljucSpiska || spisak.Kola.Count >= 2))
                rez.Add(new Dokument { Tip = TipDokumenta.SpisakKola, Strane = { 1 }, Opis = "spisak kola (" + opisIzvora + ", " + spisak.Kola.Count + " kola)", Spisak = { spisak }, Fajl = fajl, Izvor = izvor });
            if (carinski.Mrn.Count > 0)
                rez.Add(new Dokument { Tip = TipDokumenta.TabelaMrn, Strane = { 1 }, Opis = "tabela MRN (" + opisIzvora + ", " + carinski.Mrn.Count + " MRN)", Carinski = { carinski }, Fajl = fajl, Izvor = izvor });
            // v2.8.3: spisak tovarnih jedinica bez kola („Pos. | T2 | SV“, „1;LKWA 0509677“) – oznake UTI (4 slova + 6–7 cifara)
            var jedinica = sviTekstovi.SelectMany(t => Regex.Matches(t ?? "", @"(?<![A-Z])[A-Z]{4}\s?\d{6,7}(?!\d)").Cast<Match>().Select(m => Regex.Replace(m.Value, @"\s", ""))).Distinct().Count();
            if (rez.Count == 0 && jedinica >= 3)
                rez.Add(new Dokument { Tip = TipDokumenta.SpisakJedinica, Strane = { 1 }, Opis = "spisak tovarnih jedinica (" + opisIzvora + ", " + jedinica + " jedinica)", Fajl = fajl, Izvor = izvor });
            if (rez.Count == 0)
                rez.Add(new Dokument { Tip = TipDokumenta.Ostalo, Strane = { 1 }, Opis = opisIzvora + " bez spiska kola i MRN", Fajl = fajl, Izvor = izvor });
            return rez;
        }

        // ------------------------------------------------------------------------------------------- v2.9.0: podaci po kolima

        // v2.9.1 (5.1, 5.2): kolone kola imaju prednost nad kolonama tovarnih jedinica („Tara vagona“ pre „Tara“ prikolice, „Wg.Ladung“
        // pre „Netto“, „Ukupna težina“ pre „Brutto“); CER „Súly (t) – Saját / Rakomány / Elegy“, PSP „Tara vg. / Masa inc. / Masa brută“
        private static readonly (string polje, Regex rx)[] PrednostKola =
        {
            ("tara", new Regex(@"^(tara vagona|tara vg|tara kola|eigengewicht|sajat)\b", RegexOptions.Compiled)),
            ("neto", new Regex(@"^(wg ?\.? ?ladung|ladung|gewicht der ladung|rakomany|masa inc)\b", RegexOptions.Compiled)),
            ("bruto", new Regex(@"^(ukupna tezina|gesamt ?-? ?gewicht|gesamt|elegy|masa bruta)\b", RegexOptions.Compiled)),
        };

        private static readonly (string polje, Regex rx)[] KoloneKola =
        {
            ("rb", new Regex(@"^(rbr|rb|r br|redni broj|red br|no|nr|pos|lfd nr|lfn|br|#)\.?$", RegexOptions.Compiled)),
            ("tara", new Regex(@"^(tara|tare|dara|tara vg|tara kola|tara vagona|eigengewicht|sajat)\b", RegexOptions.Compiled)),
            ("neto", new Regex(@"^(neto|netto|net|masa inc|masa incarcaturii|masa robe|masa tereta|weight of goods|ladegewicht|wg ?\.? ?ladung|ladung|gewicht der ladung|rakomany)\b", RegexOptions.Compiled)),
            ("bruto", new Regex(@"^(bruto|brutto|btto|gross|masa bruta|bruttogewicht|ukupna masa|ukupna tezina|gesamt|elegy)\b", RegexOptions.Compiled)),
            ("osovine", new Regex(@"^(bros|br os|br\. os|broj osovina|osovin\w*|axles?|achsen?|achsz\w*|nb essieux|os|nr osii|tsz|tengely\w*)\b", RegexOptions.Compiled)),
            ("duzina", new Regex(@"^(duzina|duz|length|lup|lange|laenge|luep|lange uber puffer|lungime|hossz)\b", RegexOptions.Compiled)),
            ("serija", new Regex(@"^(serija|series|gattung|type|tip|vrsta kola|kategorija|wagon type|typ)\b", RegexOptions.Compiled)),
            ("napomena", new Regex(@"^(napomena|primedba|remarks?|bemerkung\w*|note|notes)\b", RegexOptions.Compiled)),
            ("kemler", new Regex(@"^(kemler|un broj|un nr|un no|un)\b", RegexOptions.Compiled)),
        };

        /// <summary>
        /// v2.9.0: vrednosti po kolima iz kolona spiska (red zaglavlja: bar dve prepoznate kolone): tara/neto/bruto (kg; kolona sa
        /// vrednostima ispod 200 je u tonama), broj osovina, dužina (m; vrednosti preko 1000 su u mm, 100–1000 u cm), serija, napomena,
        /// Kemler/UN i redni broj. Kolona koje nema ostaje null.
        /// </summary>
        internal static void PodaciPoKolima(List<string[]> redovi, SpisakKola spisak, Dictionary<string, string[]> redKola)
        {
            if (spisak.Kola.Count == 0) return;
            Dictionary<string, int> kol = null;
            int iZaglavlja = -1;
            Dictionary<string, int> Kolone(string[] r)
            {
                var k = new Dictionary<string, int>();
                for (int i = 0; i < r.Length; i++)
                {
                    var c = TekstUtil.Kljuc(r[i] ?? "").Trim();
                    if (c.Length == 0 || c.Length > 40) continue;
                    foreach (var (polje, rx) in PrednostKola)
                        if (!k.ContainsKey(polje) && rx.IsMatch(c)) k[polje] = i;
                }
                for (int i = 0; i < r.Length; i++)
                {
                    var c = TekstUtil.Kljuc(r[i] ?? "").Trim();
                    if (c.Length == 0 || c.Length > 40) continue;
                    foreach (var (polje, rx) in KoloneKola)
                        if (!k.ContainsKey(polje) && !k.ContainsValue(i) && rx.IsMatch(c)) { k[polje] = i; break; }
                }
                return k;
            }
            for (int ri = 0; ri < redovi.Count; ri++)
            {
                var r = redovi[ri];
                if (r.Any(c => BrojKola(c) != null)) continue;
                var k = Kolone(r);
                if (k.Count < 2) continue;
                // zaglavlje u dva reda (CER: „Súly (t)“ → „Saját | Rakomány | Elegy“): drugi red dopunjuje kolone koje nedostaju
                for (int dj = 1; dj <= 2 && ri + dj < redovi.Count && !redovi[ri + dj].Any(c => BrojKola(c) != null); dj++)
                    foreach (var kv in Kolone(redovi[ri + dj]))
                        if (!k.ContainsKey(kv.Key) || (PrednostKola.Any(x => x.polje == kv.Key && x.rx.IsMatch(TekstUtil.Kljuc(redovi[ri + dj][kv.Value] ?? "").Trim())) && !PrednostKola.Any(x => x.polje == kv.Key && x.rx.IsMatch(TekstUtil.Kljuc(r[k[kv.Key]] ?? "").Trim()))))
                            k[kv.Key] = kv.Value;
                kol = k; iZaglavlja = ri;
                break;
            }
            // CIT 23 (Prilog 4 GLV-TC, „Spisak kola – Wagenliste“): red sa brojevima kolona 1–9 – kolona 6 = neto, 7 = tara, 8 = bruto, 3 = osovine
            var cit23 = redovi.Any(r => r.Any(c => Regex.IsMatch(TekstUtil.Kljuc(c ?? ""), @"spisak kola\s*-?\s*wagenliste|wagenliste|glv\s*-?\s*tc|cit\s*-?\s*23|prilog\s*-?\s*anlage\s*4")));
            foreach (var r in cit23 ? redovi : new List<string[]>())
            {
                var brojevi = new Dictionary<int, int>();
                for (int i = 0; i < r.Length; i++) if (Regex.IsMatch((r[i] ?? "").Trim(), @"^[1-9]$")) { var b = int.Parse(r[i].Trim()); if (!brojevi.ContainsKey(b)) brojevi[b] = i; }
                if (!(brojevi.Count >= 7 && Enumerable.Range(1, 8).All(brojevi.ContainsKey) && Enumerable.Range(1, 7).All(b => brojevi[b] < brojevi[b + 1]))) continue;
                kol = kol ?? new Dictionary<string, int>();
                kol["neto"] = brojevi[6]; kol["tara"] = brojevi[7]; kol["bruto"] = brojevi[8]; kol["osovine"] = brojevi[3]; kol["rb"] = brojevi[1];
                break;
            }
            if (kol == null && !cit23) return;
            kol = kol ?? new Dictionary<string, int>();
            double? Broj(string s)
            {
                var t = Regex.Replace(s ?? "", @"\s", "");
                // „79 747“, „79.747“ (hiljade) i „79,747“ / „79.75“ (decimale): tačka/zarez sa tačno tri cifre posle je razdvajanje hiljada
                if (Regex.IsMatch(t, @"^\d{1,3}([.,]\d{3})+$") && !Regex.IsMatch(t, @"^\d{1,2}[.,]\d{3}$")) t = Regex.Replace(t, @"[.,]", "");
                t = t.Replace(",", ".");
                return double.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) && v > 0 ? v : (double?)null;
            }
            string Celija(string broj, string polje) => kol.TryGetValue(polje, out var i) && redKola.TryGetValue(broj, out var r) && i < r.Length ? (r[i] ?? "").Trim() : null;
            double? MaxKolone(string polje) => spisak.Kola.Select(x => Broj(Celija(x.Broj, polje))).Where(v => v.HasValue).Select(v => v.Value).DefaultIfEmpty(0).Max();
            double Faktor(string polje) => MaxKolone(polje) is double m && m > 0 && m < 200 ? 1000 : 1;
            var fT = Faktor("tara"); var fN = Faktor("neto"); var fB = Faktor("bruto");
            if (Environment.GetEnvironmentVariable("CIMPROVERA_DBG") == "1")
            {
                Console.Error.WriteLine("[dbg spisak] kolone: " + string.Join(", ", kol.Select(x => x.Key + "=" + x.Value)) + "; faktori t/n/b " + fT + "/" + fN + "/" + fB);
                foreach (var r in redovi) Console.Error.WriteLine("[dbg red] " + string.Join(" | ", r.Select((c, i) => i + ":" + c)));
            }
            // v2.9.1: red „Ukupno / Insgesamt / zbir“ – posle prvih kola, bez broja kola, sa masom koja je zbir (bar 3× veća od mase jednih kola)
            int iPrvih = redovi.FindIndex(x => x.Any(c => BrojKola(c) != null));
            if (iPrvih >= 0)
                for (int ri = redovi.Count - 1; ri > iPrvih; ri--)
                {
                    var r = redovi[ri];
                    if (r.Any(c => BrojKola(c) != null)) break;
                    double? U(string polje, double f0) => kol.TryGetValue(polje, out var i) && i < r.Length && Broj(r[i]) is double v ? v * f0 : (double?)null;
                    double? MaxKola(string polje, double f0) => spisak.Kola.Select(x => Broj(Celija(x.Broj, polje))).Where(v => v.HasValue).Select(v => v.Value * f0).DefaultIfEmpty(0).Max();
                    double? Jed(double? u, string polje, double f0)
                    {
                        // zbir kola u istoj jedinici; od tri tumačenja (×1, ÷1000, ×1000) uzima se ono najbliže zbiru stavki
                        var zbir = spisak.Kola.Select(x => Broj(Celija(x.Broj, polje))).Where(v => v.HasValue).Sum(v => v.Value * f0);
                        if (!u.HasValue || zbir <= 0) return u;
                        return new[] { u.Value, u.Value / 1000, u.Value * 1000 }.OrderBy(x => Math.Abs(x - zbir)).First();
                    }
                    double? Verovatan(double? u, string polje, double f0)
                    {
                        var zbir = spisak.Kola.Select(x => Broj(Celija(x.Broj, polje))).Where(v => v.HasValue).Sum(v => v.Value * f0);
                        return u.HasValue && zbir > 0 && (u < 0.5 * zbir || u > 2 * zbir) ? null : u;
                    }
                    var ut = Verovatan(Jed(U("tara", fT), "tara", fT), "tara", fT); var un = Verovatan(Jed(U("neto", fN), "neto", fN), "neto", fN); var ub = Verovatan(Jed(U("bruto", fB), "bruto", fB), "bruto", fB);
                    bool Zbir(double? u, string polje, double f0) => u.HasValue && u >= 1.5 * (MaxKola(polje, f0) ?? 0) && (MaxKola(polje, f0) ?? 0) > 0 && spisak.Kola.Count >= 2;
                    var oznacen = r.Any(c => Regex.IsMatch(TekstUtil.Kljuc(c ?? ""), @"^(ukupno|insgesamt|total|zbir|summe|osszesen|totaluri?)\b"));
                    if (!(oznacen || Zbir(ut, "tara", fT) || Zbir(un, "neto", fN) || Zbir(ub, "bruto", fB))) continue;
                    if (Zbir(ut, "tara", fT) || (oznacen && ut > 0)) spisak.UkupnoTaraKg = Math.Round(ut.Value);
                    if (Zbir(un, "neto", fN) || (oznacen && un > 0)) spisak.UkupnoNetoKg = Math.Round(un.Value);
                    if (Zbir(ub, "bruto", fB) || (oznacen && ub > 0)) spisak.UkupnoBrutoKg = Math.Round(ub.Value);
                    break;
                }
            var maxD = MaxKolone("duzina") ?? 0;
            var fD = maxD >= 1000 ? 0.001 : maxD >= 100 ? 0.01 : 1;
            foreach (var x in spisak.Kola)
            {
                if (Broj(Celija(x.Broj, "tara")) is double t) x.TaraKg = Math.Round(t * fT);
                if (Broj(Celija(x.Broj, "neto")) is double n) x.NetoKg = Math.Round(n * fN);
                if (Broj(Celija(x.Broj, "bruto")) is double b) x.BrutoKg = Math.Round(b * fB);
                if (Broj(Celija(x.Broj, "osovine")) is double o && o >= 2 && o <= 16 && Math.Abs(o - Math.Round(o)) < 0.01) x.Osovine = (int)Math.Round(o);
                if (Broj(Celija(x.Broj, "duzina")) is double d && d * fD >= 5 && d * fD <= 50) { x.DuzinaM = Math.Round(d * fD, 2); x.DuzinaIzvor = "spisak"; }
                var s = Celija(x.Broj, "serija");
                if (!string.IsNullOrEmpty(s) && Regex.IsMatch(s, @"^\p{Lu}\p{Ll}{0,12}$")) x.Serija = s;
                var nap = Celija(x.Broj, "napomena");
                if (!string.IsNullOrEmpty(nap)) x.Napomena = nap.Length > 100 ? nap.Substring(0, 100) : nap;
                var ke = Celija(x.Broj, "kemler");
                if (!string.IsNullOrEmpty(ke) && Regex.IsMatch(ke, @"^\d{2,3}X?\s*/\s*\d{4}$|^\d{4}$")) x.KemlerUn = Regex.Replace(ke, @"\s", "");
                if (Broj(Celija(x.Broj, "rb")) is double rb && rb >= 1 && rb < 1000 && Math.Abs(rb - Math.Round(rb)) < 0.01) x.Rb = (int)rb;
            }
            // provera smisla (OCR skena pomera kolone): tara kola 8–40 t, neto 0 ili 1–100 t, bruto ≥ tara; kolona koja za većinu
            // kola nema smisla se odbacuje (bolje bez mase nego pogrešna masa)
            bool Vecina(Func<KolaNaSpisku, double?> v, Func<double, bool> ok) { var l = spisak.Kola.Select(v).Where(x => x.HasValue).Select(x => x.Value).ToList(); return l.Count == 0 || l.Count(ok) >= 0.8 * l.Count; }
            if (cit23) PoSablonuCit23(redovi, spisak, redKola);
            if (!Vecina(k => k.TaraKg, t => t >= 8000 && t <= 40000)) foreach (var k in spisak.Kola) k.TaraKg = null;
            if (!Vecina(k => k.NetoKg, n => n == 0 || n >= 1000 && n <= 100000)) foreach (var k in spisak.Kola) k.NetoKg = null;
            if (!Vecina(k => k.BrutoKg, b => b >= 8000 && b <= 140000)) foreach (var k in spisak.Kola) k.BrutoKg = null;
            if (spisak.Kola.Any(k => k.TaraKg.HasValue && k.BrutoKg.HasValue) && !Vecina(k => k.TaraKg.HasValue && k.BrutoKg.HasValue ? k.BrutoKg - k.TaraKg : (double?)null, r => r >= -1))
                foreach (var k in spisak.Kola) { k.TaraKg = null; k.NetoKg = null; k.BrutoKg = null; }
        }

        /// <summary>
        /// v2.9.1 (5.1, 5.2): CIT 23 (Prilog 4 GLV-TC) po šablonu – kolone 6 (neto), 7 (tara) i 8 (bruto) su tri uzastopna broja u redu kola
        /// za koje važi neto + tara = bruto (± 1 %). Tako se čita i kada OCR pomeri kolone (Šabac GIPS.pdf str. 7: prvih 9 redova bez rednog
        /// broja). Primenjuje se samo kada daje dosledan rezultat za bar 80 % kola; isto za red „Ukupno – Insgesamt“.
        /// </summary>
        private static void PoSablonuCit23(List<string[]> redovi, SpisakKola spisak, Dictionary<string, string[]> redKola)
        {
            (double n, double t, double b)? Trojka(string[] r, int od)
            {
                (double n, double t, double b)? nadjeno = null;
                var v = r.Skip(od).Select(c => Regex.IsMatch((c ?? "").Trim(), @"^\d{1,3}(?:[.,]\d{3})+$|^\d{1,3}[.,]\d{1,3}$|^\d{4,7}$") ? Baza.PosaoIzDokumenata.Kg((c ?? "").Trim()) : null).ToList();
                for (int i = 0; i + 2 < v.Count; i++)
                    if (v[i] is double a && v[i + 1] is double t && v[i + 2] is double b && a > 0 && t >= 5000 && t <= 45000 && Math.Abs(a + t - b) <= 0.01 * b && b <= 140000)
                        nadjeno = (a, t, b);   // poslednja dosledna trojka u redu
                return nadjeno;
            }
            var po = new Dictionary<string, (double n, double t, double b)>();
            foreach (var k in spisak.Kola)
                if (redKola.TryGetValue(k.Broj, out var r))
                {
                    var iw = Array.FindIndex(r, c => BrojKola(c) == k.Broj);
                    if (Trojka(r, iw + 1) is var tr && tr != null) po[k.Broj] = tr.Value;
                }
            if (spisak.Kola.Count == 0 || po.Count < 0.8 * spisak.Kola.Count) return;
            foreach (var k in spisak.Kola)
                if (po.TryGetValue(k.Broj, out var tr)) { k.NetoKg = tr.n; k.TaraKg = tr.t; k.BrutoKg = tr.b; }
                else { k.NetoKg = null; k.TaraKg = null; k.BrutoKg = null; k.Napomena = (k.Napomena + " mase nisu pročitane – ručna provera").Trim(); }
            // red „Ukupno – Insgesamt“
            foreach (var r in redovi)
                if (r.Any(c => Regex.IsMatch(TekstUtil.Kljuc(c ?? ""), @"^(ukupno|insgesamt)\b")))
                {
                    var v = r.Select(c => Regex.IsMatch((c ?? "").Trim(), @"^\d{1,3}(?:[.,]\d{3})+$|^\d{4,8}$") ? Baza.PosaoIzDokumenata.Kg((c ?? "").Trim()) : null).ToList();
                    for (int i = 0; i + 2 < v.Count; i++)
                        if (v[i] is double a && v[i + 1] is double t && v[i + 2] is double b && Math.Abs(a + t - b) <= 0.01 * b)
                        { spisak.UkupnoNetoKg = a; spisak.UkupnoTaraKg = t; spisak.UkupnoBrutoKg = b; }
                }
        }

        private static readonly Regex BrojVozaRx = new Regex(@"(?<![\p{L}])(?:train|voz|vlak|zug|tren|vonat)\s*(?:nr|no|br|broj|number|nummer|n°)?\s*[.:]?\s*(\d{3,6})\s*[A-Z]?(?!\d)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>v2.9.0: broj voza iz teksta („Train nr. 256“, „CIT23 za voz 256 …“, „Train no: 256F“); prazno ako ga nema.</summary>
        internal static string BrojVoza(string tekst)
        {
            var m = BrojVozaRx.Match(tekst ?? "");
            return m.Success ? m.Groups[1].Value : "";
        }

        /// <summary>v2.9.0: zaglavlje spiska – broj voza, otpravna i uputna stanica, NHM (redovi „Otpravna stanica | …“).</summary>
        internal static void ZaglavljeSpiska(List<string[]> redovi, SpisakKola spisak, string opisIzvora)
        {
            // v2.9.1: natpis u jednom redu, vrednost ispod (ista kolona): „Vonat hossza m“ → 479.6, „Érkezési állomás“ → „55071126 - Kelebia oh.“,
            // „Prevozni put - Leitungweg“ → „Subotica br./Kelebia gr.“, „Itinerariu“ → „Agigea Nord - Stamora M - Vreoci“
            for (int ri = 0; ri < redovi.Count; ri++)
            {
                var r = redovi[ri];
                for (int i = 0; i < r.Length; i++)
                {
                    var n = TekstUtil.Kljuc(r[i] ?? "").Trim();
                    // prevoznik naveden na spisku („CER Hungary Zrt. UIC: 3085 …“)
                    if (Regex.IsMatch(r[i] ?? "", @"(?i)\p{L}{3,}.*\bUIC\s*:?\s*\d{4}\b") && !spisak.Prevoznici.Contains(r[i].Trim())) spisak.Prevoznici.Add(r[i].Trim());
                    if (n.Length == 0 || n.Length > 60) continue;
                    string Ispod()
                    {
                        // ista kolona u sledeća dva reda, ili prva popunjena ćelija desno u istom redu („Itinerariu“)
                        for (int dj = 1; dj <= 2 && ri + dj < redovi.Count; dj++)
                        {
                            var x = redovi[ri + dj];
                            for (int k = i; k < x.Length && k <= i + 3; k++) if (!string.IsNullOrWhiteSpace(x[k])) return x[k].Trim();
                            for (int k = i - 1; k >= 0 && k >= i - 6; k--) if (!string.IsNullOrWhiteSpace(x[k]) && (k + 1 >= x.Length || string.IsNullOrWhiteSpace(x[k + 1]))) return x[k].Trim();
                        }
                        return "";
                    }
                    double? T(string s) { var t = Regex.Replace(s ?? "", @"[^\d.,]", "").Replace(",", "."); return double.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) && v > 0 ? v : (double?)null; }
                    if (spisak.DuzinaVozaM == null && Regex.IsMatch(n, @"^(vonat hossza|duzina voza|zuglange|zuglaenge|train length|lungime tren)")) { if (T(Ispod()) is double d && d >= 20 && d <= 1500) spisak.DuzinaVozaM = d; }
                    else if (spisak.UkupnoBrutoKg == null && Regex.IsMatch(n, @"^(elegytomeg|bruto voza|zuggewicht|train weight)")) { if (T(Ispod()) is double b) spisak.UkupnoBrutoKg = Math.Round(b < 5000 ? b * 1000 : b); }
                    else if (spisak.UkupnoNetoKg == null && Regex.IsMatch(n, @"^(rakomany tomege|neto voza|ladungsgewicht)")) { if (T(Ispod()) is double b) spisak.UkupnoNetoKg = Math.Round(b < 5000 ? b * 1000 : b); }
                    else if (spisak.PrelazniPut.Length == 0 && Regex.IsMatch(n, @"^(erkezesi allomas|prelazni put|prevozni put|leitungs?weg|itinerariu|granicni prelaz|border crossing|grenzubergang)")) spisak.PrelazniPut = Ispod();
                    else if (spisak.NhmZaglavlje.Length == 0 && Regex.IsMatch(n, @"^(szallitando aru)")) { var d = Regex.Match(Ispod(), @"^\s*(\d{4,8})").Groups[1].Value; if (d.Length >= 4) spisak.NhmZaglavlje = d; }
                    else if (spisak.OtpravnaTekst.Length == 0 && Regex.IsMatch(n, @"^(feladasi allomas|statia de expediere)")) spisak.OtpravnaTekst = Ispod();
                    else if (spisak.UputnaTekst.Length == 0 && Regex.IsMatch(n, @"^(rendeltetesi allomas|statia de destinatie)")) spisak.UputnaTekst = Ispod();
                }
            }
            if (spisak.PrelazniPut.Length == 0)
                foreach (var r in redovi.Where(x => !x.Any(c => BrojKola(c) != null)))
                    foreach (var c in r)
                        if (spisak.PrelazniPut.Length == 0 && Regex.IsMatch(c ?? "", @"(?i)\p{L}{3,}\s+(?:br|gr)\.?\s*/\s*\p{L}{3,}.*\b(?:gr|br)\.?"))
                            spisak.PrelazniPut = c.Trim();
            foreach (var r in redovi)
            {
                var c = r.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();
                if (c.Count == 0 || c.Any(x => BrojKola(x) != null)) continue;
                var red = string.Join(" ", c);
                if (spisak.BrojVoza.Length == 0) spisak.BrojVoza = BrojVoza(red);
                var oznaka = TekstUtil.Kljuc(c[0]);
                var vrednost = c.Count > 1 ? string.Join(" ", c.Skip(1)) : Regex.Replace(c[0], @"^[^:]*:\s*", "");
                if (vrednost.Length == 0 || vrednost == c[0] && !c[0].Contains(":")) continue;
                if (spisak.OtpravnaTekst.Length == 0 && Regex.IsMatch(oznaka, @"^(otpravna stanica|otpravna|station of departure|departure station|dispatching station|versandbahnhof|gare expeditrice)")) spisak.OtpravnaTekst = vrednost;
                else if (spisak.UputnaTekst.Length == 0 && Regex.IsMatch(oznaka, @"^(uputna stanica|uputna|destination station|station of destination|bestimmungsbahnhof|gare destinataire)")) spisak.UputnaTekst = vrednost;
                else if (spisak.NhmZaglavlje.Length == 0 && Regex.IsMatch(oznaka, @"^(nhm|nhm code|nhm kod|nhm sifra)$"))
                {
                    var d = Regex.Replace(vrednost, @"\D", "");
                    if (d.Length >= 4 && d.Length <= 8) spisak.NhmZaglavlje = d.Length > 6 ? d.Substring(0, 6) : d;
                }
            }
        }

        /// <summary>12-cifreni broj kola iz ćelije ("31 78 6656 001-5", "317866560015"); null ako ćelija nije broj kola.</summary>
        internal static string BrojKola(string celija)
        {
            // redni broj reda ispred broja kola: „1.) 37 80 7846 137 3“, „12) 31 78 …“
            celija = Regex.Replace(celija ?? "", @"^\s*\d{1,3}\s*(?:\.\)|\)|\.)\s+(?=\d{2}\s?\d{2})", "");
            if (string.IsNullOrEmpty(celija) || !Regex.IsMatch(celija, @"^[\d\s\-\.]+$")) return null;
            if (Regex.IsMatch(celija.Trim(), @"^\d{6}\s+\d{6}$")) return null; // opseg plombi „209012 209014“ (crtica u drugom redu PDF-a)
            var cifre = Regex.Replace(celija, @"\D", "");
            return cifre.Length == 12 ? cifre : null;
        }
    }
}
