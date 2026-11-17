using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Saobracaj.Agent.CimProvera.Baza
{
    /// <summary>
    /// v2.9.0: podaci iz 12-cifrenog broja kola (UIC 438): oblik zapisa u teretnici („33854850210-7“), slovna oznaka vrste (5. cifra =
    /// kategorija), dvoslovna oznaka (kao dbo.VagoniSerije: Ea, Fa, Ha, La, Sg, Ta, Ua, Za…) i broj osovina.
    /// Tabela po 5. i 6. cifri je izvedena iz postojećih podataka (dbo.TeretnicaStavke, 519 378 stavki, 14.11.2026): uzete su samo
    /// kombinacije gde ista oznaka ima bar ~85 % stavki. Ostalo (npr. 3x – K/O/R, 44 – L/S, 67 – E/F) ostaje prazno: nije pouzdano.
    /// </summary>
    internal static class BrojKolaUic
    {
        /// <summary>(5. i 6. cifra) → (slovo vrste, broj osovina; 0 = nije pouzdano).</summary>
        private static readonly Dictionary<string, (string slovo, int osovine)> Tabela = new Dictionary<string, (string, int)>
        {
            ["06"] = ("T", 4), ["07"] = ("T", 4), ["08"] = ("T", 4), ["09"] = ("T", 4),
            ["24"] = ("H", 0), ["27"] = ("H", 4), ["28"] = ("H", 4), ["29"] = ("H", 4),
            ["35"] = ("R", 4), ["36"] = ("R", 4), ["37"] = ("R", 4), ["39"] = ("R", 4),
            ["43"] = ("L", 4),
            ["45"] = ("S", 4), ["46"] = ("S", 4), ["47"] = ("S", 4), ["48"] = ("S", 6), ["49"] = ("S", 0),
            ["53"] = ("E", 4), ["54"] = ("E", 4), ["58"] = ("E", 4), ["59"] = ("E", 4),
            ["66"] = ("F", 4), ["69"] = ("F", 4),
            ["77"] = ("Z", 4), ["78"] = ("Z", 4), ["79"] = ("Z", 4),
            ["91"] = ("U", 4), ["92"] = ("U", 4), ["93"] = ("U", 4),
        };

        /// <summary>Slovo vrste → dvoslovna oznaka iz dbo.VagoniSerije (R ima dve: Rg i Ri – nije jednoznačno).</summary>
        private static readonly Dictionary<string, string> Dvoslovna = new Dictionary<string, string>
        {
            ["E"] = "Ea", ["F"] = "Fa", ["G"] = "Ga", ["H"] = "Ha", ["K"] = "Kg", ["L"] = "La", ["S"] = "Sg", ["T"] = "Ta", ["U"] = "Ua", ["Z"] = "Za",
        };

        public static string Cifre(string broj) => Regex.Replace(broj ?? "", @"\D", "");

        /// <summary>„338393382019“ → „33839338201-9“ (11 cifara + crtica + kontrolna, kao u dbo.TeretnicaStavke).</summary>
        public static string ZaTeretnicu(string broj)
        {
            var c = Cifre(broj);
            return c.Length == 12 ? c.Substring(0, 11) + "-" + c.Substring(11) : (broj ?? "").Trim();
        }

        /// <summary>Kontrolna cifra UIC (Luhn nad prvih 11 cifara) je ispravna.</summary>
        public static bool KontrolnaIspravna(string broj)
        {
            var c = Cifre(broj);
            if (c.Length != 12) return false;
            int zbir = 0;
            for (int i = 0; i < 11; i++)
            {
                var d = c[i] - '0';
                var x = i % 2 == 0 ? d * 2 : d;
                zbir += x / 10 + x % 10;
            }
            return (10 - zbir % 10) % 10 == c[11] - '0';
        }

        public static string Slovo(string broj) { var c = Cifre(broj); return c.Length == 12 && Tabela.TryGetValue(c.Substring(4, 2), out var t) ? t.slovo : ""; }

        public static int Osovine(string broj) { var c = Cifre(broj); return c.Length == 12 && Tabela.TryGetValue(c.Substring(4, 2), out var t) ? t.osovine : 0; }

        /// <summary>Dvoslovna oznaka (Ea, Ua…) iz broja kola; prazno ako nije pouzdana.</summary>
        public static string DvoslovnaOznaka(string broj) => Dvoslovna.TryGetValue(Slovo(broj), out var d) ? d : "";

        /// <summary>Dvoslovna oznaka iz serije upisane na spisku („Sgns“ → „Sg“, „Eanos“ → „Ea“) kada postoji u dbo.VagoniSerije.</summary>
        public static string DvoslovnaIzSerije(string serija)
        {
            var s = (serija ?? "").Trim();
            if (s.Length < 2 || !char.IsUpper(s[0])) return "";
            var d = s.Substring(0, 2);
            return Dvoslovna.ContainsValue(d) || d == "Rg" || d == "Ri" ? d : "";
        }

        /// <summary>Najčešća dvoslovna oznaka grupe kola kada je ima bar polovina kola (inače prazno – nije pouzdano).</summary>
        public static string NajcescaOznaka(IEnumerable<(string broj, string serija)> kola)
        {
            var l = kola.Select(k => DvoslovnaIzSerije(k.serija) is var s && s.Length > 0 ? s : DvoslovnaOznaka(k.broj)).ToList();
            if (l.Count == 0) return "";
            var g = l.Where(x => x.Length > 0).GroupBy(x => x).OrderByDescending(x => x.Count()).FirstOrDefault();
            return g != null && g.Count() * 2 >= l.Count ? g.Key : "";
        }
    }
}
