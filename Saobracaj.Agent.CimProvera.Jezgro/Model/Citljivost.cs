using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Saobracaj.Agent.CimProvera.Model
{
    /// <summary>
    /// v3.0.1 (tačka 2.2): mera čitljivosti JEDNOG polja (rubrike). Tri dela, svaki 0–1:
    /// <list type="bullet">
    /// <item>OCR – pouzdanost čitanja rubrike;</item>
    /// <item>pokrivenost – koliki deo mastila podataka u rubrici pokrivaju pročitane reči (malo prepoznatih reči = mnogo nepročitanog mastila);</item>
    /// <item>smisao – udeo znakova koji čine smislene reči ili brojeve (šum: „aapeca) - / TOB rPyn ETPMOY“, „£4 _5478“).</item>
    /// </list>
    /// Polje je nečitljivo kada je bilo koji deo ispod svog praga. Tada nijedno pravilo ne tvrdi da u njemu nešto „nije navedeno“ ili
    /// „nedostaje“ – samo INTERNO „rubrika N nečitljiva – ručna provera“.
    /// </summary>
    internal static class Citljivost
    {
        public const double PragOcr = 0.6, PragPokrivenosti = 0.35, PragSmisla = 0.55;   // kalibracija 18.11. (18 mailova iz „Osnovni“)

        public sealed class Mera
        {
            public double Ocr = 1, Pokrivenost = -1, Smisao = 1;
            public bool Merena;
            public bool Necitljivo => Merena && (Ocr < PragOcr || (Pokrivenost >= 0 && Pokrivenost < PragPokrivenosti) || Smisao < PragSmisla);
            public double Ukupno => Math.Min(Ocr, Math.Min(Pokrivenost >= 0 ? Pokrivenost : 1, Smisao));
            public string Opis() => "čitljivost " + Ukupno.ToString("0.00") + " (OCR " + Ocr.ToString("0.00")
                + (Pokrivenost >= 0 ? ", pokriveno rečima " + (Pokrivenost * 100).ToString("0") + " %" : "") + ", smislenih znakova " + (Smisao * 100).ToString("0") + " %)";
            public string Razlog()
            {
                if (!Necitljivo) return "";
                var r = new System.Collections.Generic.List<string>();
                if (Ocr < PragOcr) r.Add("niska pouzdanost OCR-a");
                if (Pokrivenost >= 0 && Pokrivenost < PragPokrivenosti) r.Add("malo prepoznatih reči");
                if (Smisao < PragSmisla) r.Add("većina znakova bez smisla");
                return string.Join(", ", r);
            }
        }

        /// <summary>Mera za popunjenu rubriku skena; za tekst PDF-a / Excel / Word (<paramref name="sken"/> = false) polje je uvek čitljivo.</summary>
        public static Mera Izmeri(Rubrika r, bool sken)
        {
            var m = new Mera();
            if (r == null || !sken || r.Status != StatusRubrike.Popunjeno || string.IsNullOrWhiteSpace(r.Tekst)) return m;
            m.Merena = true;
            m.Ocr = r.Pouzdanost;
            m.Pokrivenost = r.Pokrivenost;
            m.Smisao = Smisao(r.Tekst);
            return m;
        }

        /// <summary>
        /// v3.0.1 (2.3): dokaz za nalaz u rubrici je pouzdan kada je rubrika pouzdano prazna (tvrdnja „prazna / nije upisano“) ili
        /// popunjena, sa pročitanim tekstom, i čitljiva po meri. Nečitljiva rubrika nikad nije dokaz.
        /// </summary>
        public static bool DokazPouzdan(string status, string tekst, Mera m)
        {
            if (status == StatusRubrike.Necitljivo) return false;
            if (status == StatusRubrike.Prazno || status == StatusRubrike.Oznake) return true;
            if (string.IsNullOrWhiteSpace(tekst)) return false;
            return m == null || !m.Necitljivo;
        }

        private static readonly Regex Token = new Regex(@"[^\s/]+", RegexOptions.Compiled);   // „Nachweisung/Wagenliste“ = dve reči

        /// <summary>Udeo znakova (bez razmaka) koji su u smislenim rečima ili brojevima.</summary>
        public static double Smisao(string tekst)
        {
            // tokeni od jednog znaka („x“, „1“, „-“) su neutralni – ne govore ništa o čitljivosti
            var tokeni = Token.Matches(tekst ?? "").Cast<Match>().Select(x => x.Value).Where(x => x.Length > 1 && !Regex.IsMatch(x, @"^[\-–—.,;:/()'""„“”!?]+$")).ToList();
            var ukupno = tokeni.Sum(t => t.Length);
            if (ukupno == 0) return 1;
            var dobro = tokeni.Where(Smislen).Sum(t => t.Length);
            return (double)dobro / ukupno;
        }

        /// <summary>
        /// Smislen token: broj (sa tačkama, zarezima, crticama, kosim crtama), reč od slova jednog pisma sa samoglasnikom, skraćenica
        /// velikim slovima (KP, ŽSSK, s.r.o.), interpunkcija uz reč. Šum: mešano pismo u reči, simboli (©£$|_), 3 ista slova, reč bez samoglasnika.
        /// </summary>
        internal static bool Smislen(string t)
        {
            var s = t.Trim('.', ',', ';', ':', '(', ')', '"', '„', '“', '\'', '/', '-', '–', '!', '?');
            if (s.Length == 0) return t.Length <= 1;   // sama interpunkcija („;“, „-“) je neutralna kada je kratka
            // donja crta je deo oznake („SBGT26_SRB04“), ali ne na početku/kraju („_547893“)
            if ((Regex.IsMatch(s, @"[©£$|~^<>{}\[\]\\@#%*=+¦]") || Regex.IsMatch(s, @"^_|_$")) &&!Regex.IsMatch(s, @"^[\w.\-]+@[\w.\-]+$") && !Regex.IsMatch(s, @"^\d+%$")) return false;
            if (Regex.IsMatch(s, @"^[\d.,/\-:]+$")) return true;                      // broj, datum, šifra
            if (Regex.IsMatch(s, @"^[A-Za-z]{0,4}[\d][\d.,/\-]*[A-Za-z]{0,3}$")) return true;   // CZ28976096, 1D, 41a
            if (Regex.IsMatch(s, @"\p{IsCyrillic}") && Regex.IsMatch(s, @"[A-Za-z]")) return false;   // mešano pismo („rPyn“, „Byn.“ sa ćirilicom)
            if (Regex.IsMatch(s, @"(\p{L})\1\1", RegexOptions.IgnoreCase)) return false;
            var slova = Regex.Replace(s, @"[^\p{L}]", "");
            if (slova.Length == 0) return false;
            if (Regex.IsMatch(s, @"^[A-Z0-9][A-Z0-9_\-.]*$") && Regex.IsMatch(s, @"\d")) return true;   // oznaka voza / šifra: „SBGT26_SRB04“, „LDC8-HAMMNOV1“
            if (Regex.IsMatch(s, @"\d") && slova.Length > 3) return false;            // „s.r.0.“ je dozvoljeno ispod, „6ya“ nije
            if (slova.Length <= 4 && (slova == slova.ToUpperInvariant() || Regex.IsMatch(s, @"^(\p{L}\.){1,4}\p{L}?\.?$"))) return true;   // KP, ŽSSK, s.r.o.
            // česte skraćenice bez samoglasnika (pravni oblici, oznake)
            if (Regex.IsMatch(slova, @"^(kft|zrt|gmbh|mbh|ltd|nr|br|str|tel|fax|srl|bb|kg|sp|spz|dpd|mrn|pdv|pib|dič|dic|ičo|nhm|cmr|rid|bvb|hzc|rch|pkp|zssk|žssk|čd|šd|sž)$", RegexOptions.IgnoreCase)) return true;
            if (slova.Length >= 3 && !Regex.IsMatch(slova, @"[aeiouyáéíóúýäöüőűàèìòùâêîôûăąęěůаеиоуяюыэєіїёо]", RegexOptions.IgnoreCase)) return false;
            // malo-veliko-malo usred reči („TPAHC“ je u redu, „aGnacTb“ nije)
            if (Regex.IsMatch(slova, @"\p{Ll}\p{Lu}\p{Ll}")) return false;
            return slova.Length >= 2;
        }
    }
}
