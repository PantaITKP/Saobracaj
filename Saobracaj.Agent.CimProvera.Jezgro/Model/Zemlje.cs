using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Saobracaj.Agent.CimProvera.Model
{
    /// <summary>
    /// v3.0.1 (odluka korisnika 18.11., R1/R4): oblici zemlje koji NISU ISO 3166 šifra – nazivi na jezicima CIT obrazaca i suseda
    /// (latinica, ćirilica, engleski, nemački, francuski, italijanski, mađarski, rumunski, bugarski, turski, češki/slovački), česte
    /// greške u pisanju („Deutchland“, „Srbia“) i skraćenice od 2–3 slova („SRB“, „SR“, „HUN“, „D“ nije). Zajednički za čitanje (CimParser),
    /// pravila (R1/R4, R1-ISO/R4-ISO, R10) i dopune. Zemlja u bilo kom od ovih oblika postoji → samo upozorenje „upisati po ISO 3166“.
    /// </summary>
    internal static class Zemlje
    {
        /// <summary>Pun naziv (mala slova) → ISO 3166 alpha-2. Bez skraćenica (one su u <see cref="Skracenice"/>).</summary>
        public static readonly Dictionary<string, string> Nazivi = Napravi(new Dictionary<string, string[]>
        {
            ["RS"] = new[] { "srbija", "serbia", "serbien", "serbie", "serbija", "srbsko", "sırbistan", "sirbistan", "szerbia", "srbia", "serbiya", "republika srbija", "republic of serbia",
                             "србија", "сърбия", "сербия", "сербија", "република србија" },
            ["HU"] = new[] { "madjarska", "mađarska", "madarska", "hungary", "ungarn", "hongrie", "ungheria", "magyarország", "magyarorszag", "maďarsko", "madarsko", "madžarska",
                             "ungaria", "macaristan", "węgry", "wegry", "мађарска", "унгария", "венгрия" },
            ["RO"] = new[] { "rumunija", "romania", "românia", "rumänien", "rumanien", "roumanie", "románia", "rumunjska", "rumunsko", "romanya", "rumunia", "румунија", "румъния", "румыния" },
            ["BG"] = new[] { "bugarska", "bulgaria", "bulgarien", "bulgarie", "bulharsko", "bulgária", "bugaristan", "bułgaria", "бугарска", "българия", "болгария" },
            ["HR"] = new[] { "hrvatska", "croatia", "kroatien", "croatie", "croazia", "horvátország", "horvatorszag", "chorvatsko", "hırvatistan", "hirvatistan", "croația", "croatia",
                             "chorwacja", "хрватска", "хърватия", "хорватия" },
            ["SI"] = new[] { "slovenija", "slovenia", "slowenien", "slovenien", "slovénie", "szlovénia", "slovinsko", "slovenya", "słowenia", "словенија", "словения" },
            ["DE"] = new[] { "nemacka", "nemačka", "njemačka", "njemacka", "germany", "deutschland", "deutchland", "deutschand", "deutsland", "allemagne", "germania", "németország",
                             "nemetorszag", "německo", "nemecko", "almanya", "niemcy", "bundesrepublik deutschland", "немачка", "германия" },
            ["AT"] = new[] { "austrija", "austria", "österreich", "osterreich", "oesterreich", "autriche", "ausztria", "rakousko", "rakúsko", "avusturya", "austrija", "аустрија", "австрия" },
            ["CZ"] = new[] { "ceska", "češka", "czech", "czechia", "czech republic", "tschechien", "tschechische republik", "česká republika", "ceska republika", "česko", "cesko",
                             "république tchèque", "csehország", "csehorszag", "cehia", "çekya", "cekya", "czechy", "чешка", "чехия" },
            ["SK"] = new[] { "slovacka", "slovačka", "slovakia", "slowakei", "slovaquie", "szlovákia", "szlovakia", "slovensko", "slovenská republika", "slovacia", "slovakya",
                             "słowacja", "словачка", "словакия" },
            ["PL"] = new[] { "poljska", "poland", "polen", "pologne", "polonia", "lengyelország", "polsko", "polska", "polonya", "пољска", "полша", "польша" },
            ["GR"] = new[] { "grcka", "grčka", "greece", "griechenland", "grèce", "grecia", "grčija", "görögország", "yunanistan", "ελλάδα", "ελλαδα", "hellas", "грчка", "гърция", "греция" },
            ["TR"] = new[] { "turska", "turkey", "türkiye", "turkiye", "turkei", "türkei", "turquie", "turchia", "törökország", "turecko", "turcia", "türkiye cumhuriyeti", "turcja",
                             "турска", "турция" },
            ["IT"] = new[] { "italija", "italy", "italien", "italia", "italie", "olaszország", "itálie", "italya", "włochy", "италија", "италия" },
            ["MK"] = new[] { "makedonija", "macedonia", "north macedonia", "severna makedonija", "macédoine du nord", "nordmazedonien", "északmacedónia", "kuzey makedonya",
                             "македонија", "северна македонија", "северна македония", "македония" },
            ["ME"] = new[] { "crna gora", "montenegro", "monténégro", "karadağ", "karadag", "crnagora", "црна гора", "черна гора", "черногория" },
            ["BA"] = new[] { "bosna", "bosnia", "bosna i hercegovina", "bosnia and herzegovina", "bosnien", "bosnien und herzegowina", "bosznia", "bosna hersek", "босна",
                             "босна и херцеговина", "босна и херцеговина" },
            ["AL"] = new[] { "albanija", "albania", "albanien", "shqipëria", "arnavutluk", "албанија", "албания" },
            ["UA"] = new[] { "ukrajina", "ukraine", "ukraina", "ukrajna", "ucraina", "ukrayna", "україна", "украјина", "украйна", "украина" },
            ["MD"] = new[] { "moldavija", "moldova", "moldawien", "moldavia", "молдавија", "молдова" },
            ["CH"] = new[] { "švajcarska", "svajcarska", "switzerland", "schweiz", "suisse", "svizzera", "svájc", "isviçre", "швајцарска" },
            ["NL"] = new[] { "holandija", "netherlands", "niederlande", "nederland", "pays-bas", "hollandia", "холандија" },
            ["BE"] = new[] { "belgija", "belgium", "belgien", "belgique", "белгија" },
            ["FR"] = new[] { "francuska", "france", "frankreich", "franciaország", "франција" },
        });

        /// <summary>Skraćenice od 2–3 slova koje nisu ISO 3166 alpha-2 (ili jesu, ali se u adresi koriste za drugu zemlju: „SR“ = Srbija).</summary>
        public static readonly Dictionary<string, string> Skracenice = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SRB"] = "RS", ["SR"] = "RS", ["SCG"] = "RS", ["YU"] = "RS", ["HUN"] = "HU", ["MAD"] = "HU", ["ROU"] = "RO", ["ROM"] = "RO", ["BGR"] = "BG",
            ["HRV"] = "HR", ["CRO"] = "HR", ["SLO"] = "SI", ["SVN"] = "SI", ["GER"] = "DE", ["DEU"] = "DE", ["BRD"] = "DE", ["AUT"] = "AT", ["CZE"] = "CZ",
            ["CR"] = "CZ", ["ČR"] = "CZ", ["SVK"] = "SK", ["POL"] = "PL", ["GRC"] = "GR", ["GRE"] = "GR", ["TUR"] = "TR", ["ITA"] = "IT", ["MKD"] = "MK",
            ["MNE"] = "ME", ["CG"] = "ME", ["BIH"] = "BA", ["ALB"] = "AL", ["UKR"] = "UA", ["MDA"] = "MD", ["SUI"] = "CH", ["CHE"] = "CH",
        };

        private static Dictionary<string, string> Napravi(Dictionary<string, string[]> po)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in po) foreach (var n in kv.Value) d[n] = kv.Key;
            return d;
        }

        // duži nazivi prvo („bosna i hercegovina“ pre „bosna“, „czech republic“ pre „czech“)
        private static readonly string[] PoDuzini = Nazivi.Keys.OrderByDescending(x => x.Length).ToArray();

        /// <summary>
        /// Oblik zemlje upisan u tekstu (kako je upisan: „Serbia“, „SRB“, „Srbijja“), ili "" ako ga nema. Skraćenica se priznaje samo
        /// velikim slovima, kao posebna reč na kraju reda ili ispred poštanskog broja („SRB-11000“, „…, 21000 Novi Sad, SRB“) – „Bul.“,
        /// „Pol.“ i slično u adresi nisu zemlja. Pokvaren naziv (jedno slovo viška/manjka, bar 7 slova) se priznaje samo kao zasebna
        /// reč na kraju reda adrese.
        /// </summary>
        public static string Nadji(string tekst)
        {
            var t = tekst ?? "";
            if (t.Length == 0) return "";
            foreach (var z in PoDuzini)
            {
                var m = Regex.Match(t, @"(?<!\p{L})" + Regex.Escape(z) + @"(?!\p{L})", RegexOptions.IgnoreCase);
                if (m.Success) return m.Value;
            }
            foreach (Match m in Regex.Matches(t, @"(?<![\p{L}\d.])(\p{Lu}{2,3})(?=\s*-\s*\d|\s+\d{4,6}\b|\s*[.,;]?\s*$|\s*[.,;]?\s*\n|\s*,)", RegexOptions.Multiline))
                if (Skracenice.ContainsKey(m.Groups[1].Value) && !SlovackaSr(t, m.Groups[1].Value)) return m.Groups[1].Value;
            foreach (Match m in Regex.Matches(t, @"(?<![\p{L}])(\p{L}{7,20})\s*[.,]?\s*$", RegexOptions.Multiline))
                if (Priblizno(m.Groups[1].Value) != null) return m.Groups[1].Value;
            return "";
        }

        /// <summary>ISO 3166 alpha-2 za oblik iz <see cref="Nadji"/> (ili bilo koji naziv/skraćenicu); null ako nije zemlja.</summary>
        public static string Iso(string oblik)
        {
            var o = (oblik ?? "").Trim().Trim('.', ',');
            if (o.Length == 0) return null;
            if (Nazivi.TryGetValue(o, out var iso)) return iso;
            if (Skracenice.TryGetValue(o, out iso)) return iso;
            return o.Length >= 7 ? Priblizno(o) : null;
        }

        /// <summary>Slovačko „SR“ (Slovenská republika) uz slovačku adresu nije Srbija.</summary>
        private static bool SlovackaSr(string t, string skr) =>
            skr == "SR" && Regex.IsMatch(t, @"(?i)bratislav|košice|kosice|žilin|zilin|slovensk|\bSK\s?\d{8,10}\b|\bIČ\s?DPH\b");

        /// <summary>Pokvaren naziv: Levenshtein ≤ 1 prema nazivu od bar 6 slova („Srbijja“ → srbija, „Hungry“ → hungary).</summary>
        private static string Priblizno(string rec)
        {
            var r = rec.ToLowerInvariant();
            foreach (var z in PoDuzini)
                if (z.Length >= 6 && !z.Contains(" ") && Math.Abs(z.Length - r.Length) <= 1 && Udaljenost(z, r) <= 1) return Nazivi[z];
            return null;
        }

        private static int Udaljenost(string a, string b)
        {
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
