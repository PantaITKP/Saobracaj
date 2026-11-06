using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Saobracaj.Common.Text
{
    /// <summary>
    /// Normalizacija teksta za poređenje naziva (stanice, pruge) i za parsiranje telegrama.
    /// </summary>
    public static class TekstUtil
    {
        private static readonly string[] Cirilica =
        {
            "А","A","Б","B","В","V","Г","G","Д","D","Ђ","Đ","Е","E","Ж","Ž","З","Z","И","I","Ј","J","К","K",
            "Л","L","Љ","Lj","М","M","Н","N","Њ","Nj","О","O","П","P","Р","R","С","S","Т","T","Ћ","Ć","У","U",
            "Ф","F","Х","H","Ц","C","Ч","Č","Џ","Dž","Ш","Š",
            "а","a","б","b","в","v","г","g","д","d","ђ","đ","е","e","ж","ž","з","z","и","i","ј","j","к","k",
            "л","l","љ","lj","м","m","н","n","њ","nj","о","o","п","p","р","r","с","s","т","t","ћ","ć","у","u",
            "ф","f","х","h","ц","c","ч","č","џ","dž","ш","š"
        };

        /// <summary>Ćirilica → latinica (srpska transliteracija). Ostali znakovi ostaju.</summary>
        public static string UZLatinicu(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (ch >= 'Ѐ' && ch <= 'ӿ')
                {
                    var c = ch.ToString();
                    string zamena = null;
                    for (int i = 0; i < Cirilica.Length; i += 2)
                        if (Cirilica[i] == c) { zamena = Cirilica[i + 1]; break; }
                    sb.Append(zamena ?? c);
                }
                else sb.Append(ch);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Uklanja dijakritike (č ć š ž đ → c c s z d), ćirilicu prevodi u latinicu. Veličina slova ostaje.
        /// Dužina rezultata ne mora biti ista kao ulaz (Љ → Lj).
        /// </summary>
        public static string BezDijakritika(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            s = UZLatinicu(s);
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case 'đ': sb.Append('d'); continue;
                    case 'Đ': sb.Append('D'); continue;
                }
                var d = ch.ToString().Normalize(NormalizationForm.FormD);
                foreach (var x in d)
                    if (CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark)
                        sb.Append(x);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Ujednačava znakove iz Word/PDF dokumenata: navodnici, crtice, razmaci.
        /// Redovi ostaju, višestruki razmaci unutar reda se sažimaju.
        /// </summary>
        public static string UjednaciZnakove(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case ' ': case ' ': case ' ': case '\t': sb.Append(' '); break;
                    case '–': case '—': case '‒': case '−': case '­': sb.Append('-'); break;
                    case '„': case '“': case '”': case '″': case '«': case '»': sb.Append('"'); break;
                    case '‘': case '’': case '‚': case '′': case '´': case '`': sb.Append('\''); break;
                    case '•': case '●': case '▪': case '': sb.Append('-'); break;
                    case '\r': break;
                    default: sb.Append(ch); break;
                }
            }
            var t = sb.ToString().Replace(",,", "\"").Replace("''", "\"");
            t = Regex.Replace(t, "[ ]{2,}", " ");
            return t;
        }

        /// <summary>Ključ za poređenje naziva: bez dijakritika, mala slova, samo slova/cifre i jedan razmak.</summary>
        public static string Kljuc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var t = BezDijakritika(UjednaciZnakove(s)).ToLowerInvariant();
            var sb = new StringBuilder(t.Length);
            bool razmak = false;
            foreach (var ch in t)
            {
                if (char.IsLetterOrDigit(ch)) { sb.Append(ch); razmak = false; }
                else if (ch == '.') { sb.Append('.'); razmak = false; }
                else if (!razmak && sb.Length > 0) { sb.Append(' '); razmak = true; }
            }
            return sb.ToString().Trim();
        }

        /// <summary>
        /// Ključ naziva stanice/pruge za TelegramiAlias. Agent i aplikacija (pregled NeedsReview) MORAJU koristiti
        /// ovu istu funkciju, inače naučeni naziv neće biti prepoznat.
        /// </summary>
        public static string KljucNaziva(string s)
        {
            return Kljuc(s).Replace(" .", ".");
        }

        /// <summary>Skraćuje tekst na najviše <paramref name="max"/> znakova, na granici reči.</summary>
        public static string SkratiNaRec(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? "";
            var cut = s.LastIndexOf(' ', max);
            if (cut < max / 2) cut = max;
            return s.Substring(0, cut).TrimEnd(' ', ',', ';');
        }

        /// <summary>Levenshtein rastojanje (za tolerisanje jedne greške u kucanju naziva).</summary>
        public static int Rastojanje(string a, string b)
        {
            a = a ?? ""; b = b ?? "";
            var d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
                for (int j = 1; j <= b.Length; j++)
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            return d[a.Length, b.Length];
        }
    }
}
