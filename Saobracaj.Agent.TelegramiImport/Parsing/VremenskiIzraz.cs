using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Saobracaj.Agent.TelegramiImport.Parsing
{
    public struct DnevniInterval
    {
        public TimeSpan Od;
        public TimeSpan Do;
        public bool PrekoPonoci { get { return Do <= Od; } }
        public override string ToString() { return Fmt(Od) + "-" + Fmt(Do); }
        private static string Fmt(TimeSpan t) { return ((int)t.TotalHours).ToString("00") + ":" + t.Minutes.ToString("00"); }
    }

    /// <summary>
    /// Datumi, noći i dnevni intervali iz teksta jedne tačke telegrama.
    /// Ulaz je tekst bez dijakritika, mala slova (TelegramParser to priprema).
    /// </summary>
    public sealed class VremenskiIzraz
    {
        public SortedSet<DateTime> Dani { get; } = new SortedSet<DateTime>();
        /// <summary>Noći koje prelaze ponoć; vrednost je dan početka (20/21.10 → 20.10).</summary>
        public SortedSet<DateTime> Noci { get; } = new SortedSet<DateTime>();
        public List<DnevniInterval> Intervali { get; } = new List<DnevniInterval>();
        public bool DoOpoziva { get; private set; }
        public DateTime? VaziDoDatuma { get; private set; }
        public List<string> Upozorenja { get; } = new List<string>();

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private const RegexOptions O = RegexOptions.CultureInvariant;

        public static VremenskiIzraz Parsiraj(string t, DateTime refDatum)
        {
            var r = new VremenskiIzraz();
            var sb = new StringBuilder(" " + Pripremi(t) + " ");

            var iskljuceniDani = new List<DayOfWeek>();
            var iskljuceniDatumi = new HashSet<DateTime>();

            // 1. Izuzeci: "(osim subote i nedelje)", "(izuzev nedeljom)", "osim nedeljom i dana 12.12.2026"
            foreach (Match m in Regex.Matches(sb.ToString(), @"\b(?:osim|izuzev|sem)\b([^)\n;]*)", O))
            {
                var klauza = m.Groups[1].Value;
                if (Regex.IsMatch(klauza, @"\bsubot", O)) iskljuceniDani.Add(DayOfWeek.Saturday);
                if (Regex.IsMatch(klauza, @"\bnedelj", O)) iskljuceniDani.Add(DayOfWeek.Sunday);
                if (Regex.IsMatch(klauza, @"\bvikend", O)) { iskljuceniDani.Add(DayOfWeek.Saturday); iskljuceniDani.Add(DayOfWeek.Sunday); }
                if (Regex.IsMatch(klauza, @"\bprazni", O)) r.Upozorenja.Add("Izuzeti su praznici; nisu automatski uklonjeni.");
                var kd = Regex.Match(klauza, @"\b(?:dana|dane|dan)\b", O);
                if (kd.Success)
                    foreach (var d in ListaDana(klauza, kd.Index + kd.Length, refDatum, out _))
                        iskljuceniDatumi.Add(d.Dan);
                if (iskljuceniDani.Count > 0 || iskljuceniDatumi.Count > 0 || klauza.Contains("prazni"))
                    Prebrisi(sb, m.Index, m.Length);
            }

            // 2. Dnevni intervali "od 08.00 do 12.15", "od 23:00 do 03:00 casova", "- od 07.50 do 09.30"
            var tekst = sb.ToString();
            foreach (Match m in Regex.Matches(tekst, @"(?:(?<![\d.])od|(?<=(?:interval\w*|vremenu|vreme)\s*:?\s*))\s*(\d{1,2})\s*[.:,]\s*(\d{2})\s*\.?\s*(?:h\b|cas\w*\s*)?\s*(?:do|-)\s*(\d{1,2})\s*[.:,]\s*(\d{2})(?!\s*[.]?\s*\d)", O))
            {
                int h1 = Int(m.Groups[1]), m1 = Int(m.Groups[2]), h2 = Int(m.Groups[3]), m2 = Int(m.Groups[4]);
                if (h1 > 24 || h2 > 24 || m1 > 59 || m2 > 59) continue;
                var pre = tekst.Substring(Math.Max(0, m.Index - 14), Math.Min(14, m.Index));
                if (Regex.IsMatch(pre, @"(dana|periodu|perioda)\s*$", O) && m1 <= 12 && m2 <= 12) continue; // "od dana 03.11 do 06.11"
                r.DodajInterval(new TimeSpan(h1, m1, 0), new TimeSpan(h2, m2, 0));
                Prebrisi(sb, m.Index, m.Length);
            }
            tekst = sb.ToString();
            foreach (Match m in Regex.Matches(tekst, @"(?<![\d.])od\s*(\d{1,2})\s*(?:h|cas\w*)?\s*do\s*(\d{1,2})\s*(?:h\b|cas)", O))
            {
                int h1 = Int(m.Groups[1]), h2 = Int(m.Groups[2]);
                if (h1 > 24 || h2 > 24) continue;
                r.DodajInterval(new TimeSpan(h1, 0, 0), new TimeSpan(h2, 0, 0));
                Prebrisi(sb, m.Index, m.Length);
            }
            tekst = sb.ToString();
            foreach (Match m in Regex.Matches(tekst, @"(?:pocev\s*)?od\s*(\d{1,2})\s*[.:]\s*(\d{2}).{0,60}?u trajanju od\s*(\d+)\s*(minut|sat|cas)", O))
            {
                var od = new TimeSpan(Int(m.Groups[1]), Int(m.Groups[2]), 0);
                var trajanje = m.Groups[4].Value == "minut" ? TimeSpan.FromMinutes(Int(m.Groups[3])) : TimeSpan.FromHours(Int(m.Groups[3]));
                r.DodajInterval(od, od + trajanje);
                Prebrisi(sb, m.Index, m.Length);
            }

            // 3. Bez kraja / kraj važenja
            tekst = sb.ToString();
            if (Regex.IsMatch(tekst, @"do opoziva|vaznoscu odmah|vaznost odmah|do dalj(?:n)?jeg", O))
                r.DoOpoziva = true;
            var vd = Regex.Match(tekst, @"vazi\s+do\s*(\d{1,2})\.\s*(\d{1,2})\.\s*(\d{4})", O);
            if (vd.Success)
            {
                r.VaziDoDatuma = Datum(Int(vd.Groups[1]), Int(vd.Groups[2]), Int(vd.Groups[3]));
                Prebrisi(sb, vd.Index, vd.Length);
            }

            // 4. Opseg "od (dana) 27.10.2026 do (dana) 23.11.2026", "u periodu od 07. do 15.11.2026", "od 01.11.2026 do opoziva"
            tekst = sb.ToString();
            foreach (Match m in Regex.Matches(tekst, @"\bod\s+(?:dana\s+)?(\d{1,2})\.\s*(?:(\d{1,2})\.?\s*)?(?:(\d{4})\.?\s*)?do\s+(?:dana\s+)?(?:(\d{1,2})\.\s*(\d{1,2})\.?\s*(\d{4})?|(opoziva))", O))
            {
                DateTime? kraj = null;
                int god2 = refDatum.Year, mes2 = refDatum.Month;
                if (!m.Groups[7].Success)
                {
                    mes2 = Int(m.Groups[5]);
                    god2 = m.Groups[6].Success ? Int(m.Groups[6]) : GodinaZa(mes2, refDatum);
                    kraj = Datum(Int(m.Groups[4]), mes2, god2);
                    if (kraj == null) continue;
                }
                else r.DoOpoziva = true;
                int mes1 = m.Groups[2].Success ? Int(m.Groups[2]) : mes2;
                int god1 = m.Groups[3].Success ? Int(m.Groups[3]) : (m.Groups[2].Success ? GodinaZa(mes1, refDatum) : god2);
                var pocetak = Datum(Int(m.Groups[1]), mes1, god1);
                if (pocetak == null) continue;
                if (kraj == null) { r.Dani.Add(pocetak.Value); }
                else
                {
                    if (kraj < pocetak || (kraj.Value - pocetak.Value).TotalDays > 400) { r.Upozorenja.Add("Neispravan opseg datuma: " + m.Value.Trim()); continue; }
                    for (var d = pocetak.Value; d <= kraj.Value; d = d.AddDays(1)) r.Dani.Add(d);
                }
                Prebrisi(sb, m.Index, m.Length);
            }

            // 5. Liste dana posle "dana/dane/dan": "02, 03, 04, 05. i 06.11.2026", "03.11,04.11 i 06.11.2026", "02/03. i 03/04.11.2026"
            tekst = sb.ToString();
            foreach (Match m in Regex.Matches(tekst, @"\b(?:dana|dane|dan|danima)\b", O))
            {
                foreach (var d in ListaDana(tekst, m.Index + m.Length, refDatum, out int kraj))
                {
                    if (d.Noc) r.Noci.Add(d.Dan); else r.Dani.Add(d.Dan);
                }
            }
            // Noći i bez reči "dana": "između ukr. Veliki Borak i st. Lazarevac 03/04.11.2026"
            foreach (Match m in Regex.Matches(tekst, @"(?<![\d/.])\d{1,2}\s*/\s*\d{1,2}\s*\.\s*\d{0,2}", O))
            {
                foreach (var d in ListaDana(tekst, m.Index, refDatum, out int kraj))
                    if (d.Noc) r.Noci.Add(d.Dan);
            }

            // 6. Ako nema ničega: pojedinačni puni datumi u tekstu
            if (r.Dani.Count == 0 && r.Noci.Count == 0)
            {
                foreach (Match m in Regex.Matches(tekst, @"(?<![\d.])(\d{1,2})\.\s*(\d{1,2})\.\s*(\d{4})", O))
                {
                    var d = Datum(Int(m.Groups[1]), Int(m.Groups[2]), Int(m.Groups[3]));
                    if (d != null) r.Dani.Add(d.Value);
                }
            }

            // Izuzeci
            if (iskljuceniDani.Count > 0 || iskljuceniDatumi.Count > 0)
            {
                r.Dani.RemoveWhere(d => iskljuceniDani.Contains(d.DayOfWeek) || iskljuceniDatumi.Contains(d));
                r.Noci.RemoveWhere(d => iskljuceniDani.Contains(d.DayOfWeek) || iskljuceniDatumi.Contains(d));
            }
            return r;
        }

        private void DodajInterval(TimeSpan od, TimeSpan @do)
        {
            var iv = new DnevniInterval { Od = od, Do = @do };
            if (!Intervali.Any(x => x.Od == od && x.Do == @do)) Intervali.Add(iv);
        }

        /// <summary>Nizovi uzastopnih dana (jedan red u bazi po nizu × intervalu).</summary>
        public IEnumerable<Tuple<DateTime, DateTime, bool>> Nizovi()
        {
            foreach (var n in Grupisi(Dani)) yield return Tuple.Create(n.Item1, n.Item2, false);
            foreach (var n in Grupisi(Noci)) yield return Tuple.Create(n.Item1, n.Item2, true);
        }

        private static IEnumerable<Tuple<DateTime, DateTime>> Grupisi(IEnumerable<DateTime> dani)
        {
            DateTime? start = null, prev = null;
            foreach (var d in dani)
            {
                if (start == null) { start = prev = d; continue; }
                if ((d - prev.Value).TotalDays == 1) { prev = d; continue; }
                yield return Tuple.Create(start.Value, prev.Value);
                start = prev = d;
            }
            if (start != null) yield return Tuple.Create(start.Value, prev.Value);
        }

        private struct DanToken { public DateTime Dan; public bool Noc; }

        /// <summary>Čita listu dana od pozicije <paramref name="pos"/>; mesec i godina se preuzimaju s desna na levo.</summary>
        private static List<DanToken> ListaDana(string t, int pos, DateTime refDatum, out int kraj)
        {
            var tokeni = new List<int[]>(); // [dan, mesec|-1, godina|-1, dan2|-1 (noć)]
            int i = pos;
            var sep = new Regex(@"\G(?:\s|,|;|\.|\bi\b|\bte\b)+", O);
            var tok = new Regex(@"\G(\d{1,2})\s*/\s*(\d{1,2})\s*\.?(?:\s*(\d{1,2})(?![\d/])\s*\.?\s*(?:(\d{4})\.?)?)?|\G(\d{1,2})\.\s*(\d{1,2})\.\s*(\d{4})\.?|\G(\d{1,2})\.(\d{1,2})(?![\d/])\.?|\G(\d{1,2})(?![\d/])\.?", O);
            while (true)
            {
                var s = sep.Match(t, i);
                if (s.Success) i += s.Length;
                var m = tok.Match(t, i);
                if (!m.Success || m.Length == 0) break;
                if (m.Groups[1].Success)
                    tokeni.Add(new[] { Int(m.Groups[1]), m.Groups[3].Success ? Int(m.Groups[3]) : -1, m.Groups[4].Success ? Int(m.Groups[4]) : -1, Int(m.Groups[2]) });
                else if (m.Groups[5].Success)
                    tokeni.Add(new[] { Int(m.Groups[5]), Int(m.Groups[6]), Int(m.Groups[7]), -1 });
                else if (m.Groups[8].Success)
                    tokeni.Add(new[] { Int(m.Groups[8]), Int(m.Groups[9]), -1, -1 });
                else
                    tokeni.Add(new[] { Int(m.Groups[10]), -1, -1, -1 });
                i += m.Length;
            }
            kraj = i;
            var rez = new List<DanToken>();
            if (tokeni.Count == 0) return rez;
            // Bez ijednog meseca to nije lista dana (npr. "dana 3 voza")
            if (!tokeni.Any(x => x[1] > 0)) return rez;
            int mes = -1, god = -1;
            for (int k = tokeni.Count - 1; k >= 0; k--)
            {
                var x = tokeni[k];
                if (x[1] > 0) mes = x[1];
                if (x[2] > 0) god = x[2];
                if (mes < 0) continue;
                int g = god > 0 ? god : GodinaZa(mes, refDatum);
                if (x[3] > 0)
                {
                    // noć d1/d2: mesec se odnosi na d2; ako je d1 > d2, d1 je u prethodnom mesecu
                    var d2 = Datum(x[3], mes, g);
                    if (d2 == null) continue;
                    var d1 = x[0] > x[3] ? d2.Value.AddDays(-1) : Datum(x[0], mes, g);
                    if (d1 == null) continue;
                    rez.Add(new DanToken { Dan = d1.Value, Noc = true });
                }
                else
                {
                    var d = Datum(x[0], mes, g);
                    if (d != null) rez.Add(new DanToken { Dan = d.Value });
                }
            }
            return rez;
        }

        private static string Pripremi(string t)
        {
            t = t ?? "";
            t = Regex.Replace(t, @"\(\s*ugovor[^)]*\)", " ", O);
            t = Regex.Replace(t, @"\(\s*(?:od\s*)?km[^)]*\)", " ", O);
            t = Regex.Replace(t, @"\bkm\s*\d+\s*\+\s*\d+", " ", O);
            t = Regex.Replace(t, @"\d+\s*\+\s*\d+", " ", O);
            t = Regex.Replace(t, @"\(\s*(?:ponedeljak|utorak|sreda|cetvrtak|petak|subota|nedelja)\s*\)", " ", O);
            t = Regex.Replace(t, @"(?:\b|(?<=\d))godin\w*|\bgod\.", " ", O);
            t = Regex.Replace(t, @"\.{2,}", ".", O);
            t = Regex.Replace(t, @"(\d{1,2})\.\s+(\d{1,2})\.\s*(\d{4})", "$1.$2.$3", O);
            t = Regex.Replace(t, @"(\d{1,2})\.(\d{1,2})\.\s+(\d{4})", "$1.$2.$3", O);
            t = Regex.Replace(t, @"\s+", " ", O);
            return t;
        }

        private static void Prebrisi(StringBuilder sb, int index, int length)
        {
            for (int i = index; i < index + length && i < sb.Length; i++) sb[i] = ' ';
        }

        private static int Int(Group g) { return int.Parse(g.Value, Inv); }

        private static int GodinaZa(int mesec, DateTime refDatum)
        {
            // Telegram iz decembra za januar sledeće godine
            if (refDatum.Month >= 11 && mesec <= 2) return refDatum.Year + 1;
            if (refDatum.Month <= 2 && mesec >= 11) return refDatum.Year - 1;
            return refDatum.Year;
        }

        private static DateTime? Datum(int d, int m, int g)
        {
            if (m < 1 || m > 12 || d < 1 || g < 2000 || g > 2100) return null;
            if (d > DateTime.DaysInMonth(g, m)) return null;
            return new DateTime(g, m, d);
        }
    }
}
