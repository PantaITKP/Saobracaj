using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Saobracaj.Planiranje.Pravila
{
    // Pravila usklađivanja sa Registrom (RAZVOJ_Mreza_i_stanice, odeljak 4).
    internal static class NaziviRegistra
    {
        // Šifra koju u Registru dele dva mesta: Beograd ranžirna A i B.
        internal const int DeljenaSifra = 16201;

        // Opšte imenice ostaju malim slovom, osim na početku naziva.
        private static readonly HashSet<string> OpsteImenice = new HashSet<string>
        {
            "теретна", "ранжирна", "путничка", "станица", "стајалиште", "пристаниште", "ложионица", "болница", "фабрика",
            "поље", "центар", "главна", "варош", "село", "рудник", "луг", "брдо", "мост"
        };

        // Luhn, isti format kao kolona Kod: 13001 -> 130013.
        internal static string Kod(int sifra)
        {
            string s = sifra.ToString(CultureInfo.InvariantCulture);
            int zbir = 0;
            for (int i = 0; i < s.Length; i++)
            {
                int c = s[s.Length - 1 - i] - '0';
                if (i % 2 == 0)
                {
                    c *= 2;
                    if (c > 9) c -= 9;
                }
                zbir += c;
            }
            return s + ((10 - zbir % 10) % 10).ToString(CultureInfo.InvariantCulture);
        }

        internal static string UIC(int sifra)
        {
            return "72" + sifra.ToString(CultureInfo.InvariantCulture);
        }

        // Šifra iz Registra je pet cifara; ostalo u koloni nije šifra.
        internal static int? Sifra(string kolona)
        {
            string s = (kolona ?? "").Trim();
            int v;
            return Regex.IsMatch(s, @"^\d{5}$") && int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out v) ? (int?)v : null;
        }

        // Nekoliko naziva u Registru ima latinična slova među ćirilicom (РАСПУТНИЦА 2 KАЋ).
        internal static string UjednaciCirilicu(string naziv)
        {
            if (string.IsNullOrEmpty(naziv)) return naziv;
            int cirilica = naziv.Count(z => z >= '\u0400' && z <= '\u04FF');
            if (cirilica * 3 <= naziv.Length) return naziv;
            const string latinicna = "KAEOPCTMHXBaeopcxy", cirilicna = "КАЕОРСТМНХВаеорсху";
            var sb = new System.Text.StringBuilder(naziv);
            for (int i = 0; i < sb.Length; i++)
            {
                int k = latinicna.IndexOf(sb[i]);
                if (k >= 0) sb[i] = cirilicna[k];
            }
            return sb.ToString();
        }

        // Isto kao norm() u skriptama koje su napravile prve ključeve mesta.
        internal static string Normalizuj(string naziv)
        {
            string s = (naziv ?? "").ToLowerInvariant().Replace('ё', 'е');
            s = Regex.Replace(s, @"\(.*?\)", "");
            s = Regex.Replace(s, "[\"„“”'.,\\-–/]", " ");
            s = Regex.Replace(s, @"(?<![\p{L}\p{N}_])(железничка станица|стајалиште|станица|ст)(?![\p{L}\p{N}_])", "");
            return Regex.Replace(s, @"\s+", " ").Trim();
        }

        internal static string Kljuc(int? sifra, string nazivRegistar)
        {
            return sifra.HasValue && sifra.Value != DeljenaSifra
                ? "U:" + sifra.Value.ToString(CultureInfo.InvariantCulture)
                : "N:" + Normalizuj(nazivRegistar);
        }

        // Registar je pisan velikim slovima: prvo slovo reči je veliko, a opšte imenice ostaju male.
        internal static string CirilicaLepo(string registar)
        {
            string s = Regex.Replace((registar ?? "").Trim(), @"\s+", " ").ToLowerInvariant();
            var izlaz = new List<string>();
            string[] delovi = Regex.Split(s, @"(\s+|-|/|\()");
            for (int i = 0; i < delovi.Length; i++)
            {
                string d = delovi[i];
                if (d.Length == 0 || Regex.IsMatch(d, @"^(\s+|-|/|\()$")) { izlaz.Add(d); continue; }
                if (i > 0 && d.Length <= 2 && Regex.IsMatch(d, @"^(а|б|в|г|к|р|т|к1|а1|1а|2а)$")) { izlaz.Add(d.ToUpperInvariant()); continue; }
                if (i > 0 && OpsteImenice.Contains(d.Trim('(', ')', '"', '.'))) { izlaz.Add(d); continue; }
                izlaz.Add(char.ToUpperInvariant(d[0]) + d.Substring(1));
            }
            return string.Concat(izlaz);
        }

        internal static string LatinicaLepo(string registar)
        {
            return Latinica.IzCirilice(CirilicaLepo(registar));
        }

        // Validacija pri čuvanju: Srbija je između ovih koordinata; zamenjene vrednosti ne prolaze.
        internal static string GreskaKoordinata(string drzava, double? lat, double? lon)
        {
            if (!string.Equals((drzava ?? "").Trim(), "SRB", StringComparison.OrdinalIgnoreCase)) return null;
            if (lat.HasValue && (lat.Value < 41.8 || lat.Value > 46.3))
                return "Geografska širina (Latitude) za Srbiju je između 41,8 i 46,3.";
            if (lon.HasValue && (lon.Value < 18.8 || lon.Value > 23.1))
                return "Geografska dužina (Longitude) za Srbiju je između 18,8 i 23,1.";
            return null;
        }
    }
}
