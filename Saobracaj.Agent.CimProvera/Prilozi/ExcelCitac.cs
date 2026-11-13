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
                    redovi.Add(row.ItemArray.Select(c => Convert.ToString(c, System.Globalization.CultureInfo.InvariantCulture)?.Trim() ?? "").ToArray());
            return AnalizaRedova(redovi, kp, p.Naziv, "excel", "Excel");
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
                    spisak.Kola.Add(stavka);
                }
            }
            if (spisak.Kola.Any(k => k.Rid)) spisak.RidOznaka = true;

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

            if (spisak.Kola.Count > 0 && (kljucSpiska || spisak.Kola.Count >= 2))
                rez.Add(new Dokument { Tip = TipDokumenta.SpisakKola, Strane = { 1 }, Opis = "spisak kola (" + opisIzvora + ", " + spisak.Kola.Count + " kola)", Spisak = { spisak }, Fajl = fajl, Izvor = izvor });
            if (carinski.Mrn.Count > 0)
                rez.Add(new Dokument { Tip = TipDokumenta.TabelaMrn, Strane = { 1 }, Opis = "tabela MRN (" + opisIzvora + ", " + carinski.Mrn.Count + " MRN)", Carinski = { carinski }, Fajl = fajl, Izvor = izvor });
            if (rez.Count == 0)
                rez.Add(new Dokument { Tip = TipDokumenta.Ostalo, Strane = { 1 }, Opis = opisIzvora + " bez spiska kola i MRN", Fajl = fajl, Izvor = izvor });
            return rez;
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
