using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Saobracaj.Agent.CimProvera.Baza
{
    /// <summary>
    /// v2.9.0: normalizacija naziva (partneri, stanice) i ocena sličnosti 0–1: Jaro-Winkler nad celim nazivom (i bez razmaka – OCR
    /// „EurologSys em“ = „EUROLOGSYSTEM“) i poklapanje reči (svaka reč traži najbližu reč drugog naziva).
    /// </summary>
    internal static class Slicnost
    {
        /// <summary>Velika slova, bez kvačica (Đ → DJ), bez interpunkcije i višestrukih razmaka.</summary>
        public static string Osnovno(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            var t = s.Replace("đ", "dj").Replace("Đ", "DJ").Replace("ß", "ss");
            t = t.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(t.Length);
            foreach (var ch in t)
            {
                var kat = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (kat == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(char.IsLetterOrDigit(ch) ? char.ToUpperInvariant(ch) : ' ');
            }
            // ćirilica → latinica nije potrebna za šifarnike KP (nazivi su latinicom); ostaje kako jeste
            return Regex.Replace(sb.ToString().Normalize(NormalizationForm.FormC), @"\s+", " ").Trim();
        }

        // pravni oblici kao niz reči posle uklanjanja interpunkcije („S.R.O.“ → „S R O“)
        private static readonly string[][] PravniNizovi =
        {
            new[] { "D", "O", "O" }, new[] { "S", "R", "O" }, new[] { "S", "P", "A" }, new[] { "S", "R", "L" }, new[] { "SP", "Z", "O", "O" },
            new[] { "SP", "ZOO" }, new[] { "A", "S" }, new[] { "A", "D" }, new[] { "D", "D" }, new[] { "S", "A" }, new[] { "A", "G" }, new[] { "S", "C" },
            new[] { "L", "L", "C" }, new[] { "N", "V" }, new[] { "B", "V" }, new[] { "Z", "R", "T" }, new[] { "K", "F", "T" }, new[] { "E", "O", "O", "D" }, new[] { "O", "O", "D" },
            // v2.9.1: „Joint stock company“, „J.P.“ (javno preduzeće), „Public enterprise“
            new[] { "JOINT", "STOCK", "COMPANY" }, new[] { "J", "P" }, new[] { "PUBLIC", "ENTERPRISE" },
        };

        private static readonly HashSet<string> PravneReci = new HashSet<string>
        {
            "DOO", "AD", "SRO", "AS", "GMBH", "KFT", "SRL", "SPA", "LTD", "LLC", "SA", "AG", "ZRT", "NYRT", "OOD", "EOOD", "DD", "NV", "BV", "SPZOO", "INC", "CO", "KG", "SC",
        };

        /// <summary>Gradovi koji se često dopisuju uz naziv firme („EurologSystem doo Beograd“) – ne ulaze u poređenje.</summary>
        private static readonly HashSet<string> Gradovi = new HashSet<string>
        {
            "BEOGRAD", "BELGRADE", "BELGRAD", "BG", "ZAGREB", "BUDAPEST", "BUCURESTI", "BUCHAREST", "BRATISLAVA", "LJUBLJANA", "SOFIA", "SOFIJA", "WIEN", "VIENNA", "PRAHA", "PRAGUE",
        };

        /// <summary>
        /// v2.9.1: nazivi zemalja uz naziv firme („DP World Serbia“ = „DP WORLD“) – izostavljaju se samo kada ostanu bar dve druge reči
        /// („CER Hungary“ i „CER Slovakia“ ostaju različiti).
        /// </summary>
        private static readonly HashSet<string> Zemlje = new HashSet<string>
        {
            "SERBIA", "SRBIJA", "SRBIJE", "SERBIEN", "HUNGARY", "MAGYARORSZAG", "ROMANIA", "CROATIA", "HRVATSKA", "SLOVAKIA", "AUSTRIA", "GERMANY", "DEUTSCHLAND", "POLAND", "POLSKA", "BULGARIA", "TURKEY",
        };

        /// <summary>Naziv partnera za poređenje: osnovno, bez delova u zagradama, pravnih oblika i gradova.</summary>
        public static string Partner(string s, string grad = null)
        {
            var bez = Regex.Replace(s ?? "", @"\([^)]*\)|\[[^\]]*\]", " ");
            var reci = Osnovno(bez).Split(' ').Where(x => x.Length > 0).ToList();
            foreach (var niz in PravniNizovi)
                for (int i = 0; i + niz.Length <= reci.Count; i++)
                    if (Enumerable.Range(0, niz.Length).All(j => reci[i + j] == niz[j])) { reci.RemoveRange(i, niz.Length); i--; }
            var g = Osnovno(grad);
            reci = reci.Where(x => !PravneReci.Contains(x) && !Gradovi.Contains(x) && x != g && x.Length >= 2).ToList();
            if (reci.Count(x => !Zemlje.Contains(x)) >= 2) reci = reci.Where(x => !Zemlje.Contains(x)).ToList();
            return string.Join(" ", reci);
        }

        /// <summary>
        /// Pravni oblik iz naziva → verovatne države (šifre kao u dbo.Partnerji.PaSifDrzave). v2.9.2 (3a.3, odluka 17.11.): s.r.o. / a.s. →
        /// SK, CZ; d.o.o. → SRB, HR, SI, BA; Kft. / Zrt. → HU; GmbH → DE, AT; Sp. z o.o. / S.A. → PL; S.R.L. → RO, IT; A.Ş. → TR.
        /// </summary>
        public static string[] DrzaveIzPravnogOblika(string s)
        {
            // A.Ş. (turski) pre normalizacije – posle nje je isto što i a.s.
            if (Regex.IsMatch(s ?? "", @"(?<!\p{L})A\.?\s?Ş\.?(?!\p{L})")) return new[] { "TR" };
            var t = " " + Osnovno(s) + " ";
            if (Regex.IsMatch(t, @" (SP Z O O|SP ZOO|SPZOO|S A) ")) return new[] { "PL" };
            if (Regex.IsMatch(t, @" (S R O|SRO|A S) ")) return new[] { "SK", "CZ" };
            if (Regex.IsMatch(t, @" (KFT|ZRT|NYRT|K F T|Z R T) ")) return new[] { "HU" };
            if (Regex.IsMatch(t, @" (SRL|S R L) ")) return new[] { "RO", "IT" };
            if (Regex.IsMatch(t, @" (GMBH) ")) return new[] { "DE", "AT" };
            if (Regex.IsMatch(t, @" (EOOD|OOD|E O O D|O O D) ")) return new[] { "BG" };
            if (Regex.IsMatch(t, @" (D O O|DOO) ")) return new[] { "SRB", "HR", "SI", "BA" };
            return new string[0];
        }

        /// <summary>
        /// v2.9.2 (3a.3): naziv za poređenje sa sinonimom partnera (tmpSinonimi.TekstNormalizovan) – osnovno, bez delova u zagradama,
        /// pravna forma OSTAJE kao jedna reč („Budamar South, s.r.o.“ → „BUDAMAR SOUTH SRO“, „Budamar South d.o.o. Beograd“ →
        /// „BUDAMAR SOUTH DOO BEOGRAD“). Sinonim važi kada je ceo (reč po reč) sadržan u nazivu; najduži sinonim ima prednost.
        /// </summary>
        public static string SinonimPartnera(string s)
        {
            var bez = Regex.Replace(s ?? "", @"\([^)]*\)|\[[^\]]*\]", " ");
            var reci = Osnovno(bez).Split(' ').Where(x => x.Length > 0).ToList();
            foreach (var niz in PravniNizovi.Where(n => n.Length > 1).OrderByDescending(n => n.Length))
                for (int i = 0; i + niz.Length <= reci.Count; i++)
                    if (Enumerable.Range(0, niz.Length).All(j => reci[i + j] == niz[j])) { reci.RemoveRange(i, niz.Length); reci.Insert(i, string.Concat(niz)); }
            return string.Join(" ", reci);
        }

        /// <summary>Naziv stanice za poređenje: osnovno, bez delova u zagradama („PANČEVO GLAVNA (U.M. PANČEVO VAROŠ)“ → „PANCEVO GLAVNA“).</summary>
        public static string Stanica(string s)
        {
            var bez = Regex.Replace(s ?? "", @"\([^)]*\)?|\[[^\]]*\]", " ");
            bez = Regex.Replace(bez, @"(?i)\bu\.?\s?m\.?\s.*$", " ");   // „u.m. Kukujevci“ bez zagrade
            // OCR natpisa obrasca ćirilicom („Преузимање“, „станица“) uz latinični naziv – ćirilične reči su šum
            if (Regex.IsMatch(bez, @"\p{IsBasicLatin}{3,}") && Regex.IsMatch(bez, @"[Ѐ-ӿ]")) bez = Regex.Replace(bez, @"\S*[Ѐ-ӿ]\S*", " ");
            return string.Join(" ", Osnovno(bez).Split(' ').Where(w => w.Length >= 2 || Regex.IsMatch(w, @"^\d$")));
        }

        // ------------------------------------------------------------------------------------------- Jaro-Winkler

        public static double JaroWinkler(string a, string b)
        {
            if (a == b) return 1;
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;
            int la = a.Length, lb = b.Length;
            int opseg = Math.Max(0, Math.Max(la, lb) / 2 - 1);
            var ma = new bool[la]; var mb = new bool[lb];
            int m = 0;
            for (int i = 0; i < la; i++)
            {
                int od = Math.Max(0, i - opseg), doo = Math.Min(lb - 1, i + opseg);
                for (int j = od; j <= doo; j++)
                    if (!mb[j] && a[i] == b[j]) { ma[i] = mb[j] = true; m++; break; }
            }
            if (m == 0) return 0;
            int t = 0, k = 0;
            for (int i = 0; i < la; i++)
            {
                if (!ma[i]) continue;
                while (!mb[k]) k++;
                if (a[i] != b[k]) t++;
                k++;
            }
            double jaro = ((double)m / la + (double)m / lb + (m - t / 2.0) / m) / 3.0;
            int pref = 0;
            while (pref < Math.Min(4, Math.Min(la, lb)) && a[pref] == b[pref]) pref++;
            return jaro + pref * 0.1 * (1 - jaro);
        }

        /// <summary>Opšte reči u nazivima firmi – razlika u njima ne razlikuje firme (nazivi zemalja NISU ovde: Rail Cargo Hungaria ≠ Austria).</summary>
        private static readonly HashSet<string> OpsteReci = new HashSet<string>
        {
            "RAIL", "RAILWAY", "RAILWAYS", "CARGO", "LOGISTIC", "LOGISTICS", "LOGISTIK", "LOGISTIKA", "TRANS", "TRANSPORT", "TRANSPORTS", "GROUP", "SERVICE",
            "SERVICES", "SPEDITION", "SPEDICIJA", "SHIPPING", "TRADE", "TRADING", "INTERNATIONAL", "COMPANY", "SOLUTIONS", "DRUSTVO", "PREDUZECE", "ZA", "AND", "THE", "OF", "I",
        };

        private const double Prag = 0.9;

        /// <summary>
        /// Sličnost dva već normalizovana naziva, 0–1: najveća od Jaro-Winkler-a bez razmaka (OCR deli ili spaja reči: „EUROLOGSYS EM“ =
        /// „EUROLOGSYSTEM“) i poklapanja reči (udeo slova kandidata pokriven rečima iz dokumenta sa JW ≥ 0,9, malo umanjen za višak reči
        /// dokumenta). Ako u bilo kom od dva naziva postoji značajna reč (bar 3 slova, nije opšta) bez para u drugom, ocena je najviše
        /// 0,84 (ispod praga za upis), osim kada su nazivi bez razmaka skoro isti (JW ≥ 0,95). Kratko poklapanje (ispod 6 slova) najviše 0,8.
        /// </summary>
        public static double Ocena(string dokument, string kandidat)
        {
            if (string.IsNullOrEmpty(dokument) || string.IsNullOrEmpty(kandidat)) return 0;
            if (dokument == kandidat) return 1;
            var jw2 = JaroWinkler(dokument.Replace(" ", ""), kandidat.Replace(" ", ""));
            var rd = dokument.Split(' ').Where(x => x.Length > 0).ToList();
            var rk = kandidat.Split(' ').Where(x => x.Length > 0).ToList();
            bool Ima(string w, List<string> u) => u.Any(x => JaroWinkler(w, x) >= Prag);
            double Pokriveno(List<string> od, List<string> u)
            {
                double ukupno = od.Sum(x => x.Length), pogodak = 0;
                foreach (var w in od) { var best = u.Select(x => JaroWinkler(w, x)).DefaultIfEmpty(0).Max(); if (best >= Prag) pogodak += w.Length * best; }
                return ukupno == 0 ? 0 : pogodak / ukupno;
            }
            var pk = Pokriveno(rk, rd);   // koliko kandidata je u dokumentu
            var pd = Pokriveno(rd, rk);   // koliko dokumenta je u kandidatu
            var reci = pk * (0.85 + 0.15 * pd);
            if (rk.Where(w => Ima(w, rd)).Sum(w => w.Length) < 6) reci = Math.Min(reci, 0.8);
            // JW bez razmaka samo za nazive slične dužine (OCR greška), ne za „BUDAMAR“ ↔ „BUDAMAR LOGISTICS“ (bonus za isti početak)
            int la = dokument.Replace(" ", "").Length, lb = kandidat.Replace(" ", "").Length;
            if (Math.Min(la, lb) < 0.8 * Math.Max(la, lb)) jw2 = Math.Min(jw2, 0.8);
            var ocena = Math.Max(reci, jw2);
            bool Znacajna(string w) => w.Length >= 3 && !OpsteReci.Contains(w);
            var bezPara = rk.Any(w => Znacajna(w) && !Ima(w, rd)) || rd.Any(w => Znacajna(w) && !Ima(w, rk));
            if (bezPara && jw2 < 0.95) ocena = Math.Min(ocena, 0.84);
            return Math.Round(ocena, 4);
        }
    }
}
