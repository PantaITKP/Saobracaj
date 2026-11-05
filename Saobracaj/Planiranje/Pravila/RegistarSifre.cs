using System.Collections.Generic;

namespace Saobracaj.Planiranje.Pravila
{
    // Šifre iz legende Registra (kolone G i R).
    internal static class RegistarSifre
    {
        private static readonly Dictionary<string, string> Vrste = new Dictionary<string, string>
        {
            { "1", "stanica" }, { "2", "ukrsnica" }, { "3", "stajalište" }, { "4", "rasputnica i odjavnica" },
            { "5", "odjavnica i stajalište" }, { "6", "rasputnica" }, { "7", "rasputnica i stajalište" }, { "8", "tovarište i stajalište" },
            { "9", "prelom brzine" }, { "10", "TO i stajalište" }, { "11", "STO" }, { "12", "tovarište" },
            { "13", "TO" }, { "14", "odvojna skretnica" }, { "15", "državna granica" }, { "16", "odjavnica" }
        };

        private static readonly Dictionary<string, string> Osiguranja = new Dictionary<string, string>
        {
            { "1", "Elektrorelejni, komplet. centralizacija" }, { "2", "Elektrorelejni, delimična centralizacija" },
            { "3", "Elektrorelejni, ključevna zavisnost" }, { "4", "Elektrorelejni, međusignalna zavisnost" },
            { "5", "Elektromehanički blok aparat" }, { "6", "Elektromehanički zabravni blok" },
            { "7", "Mehaničke signalne postavnice" }, { "8", "Elektromehanički privolni blok" },
            { "9", "Prosta signalna postavnica" }, { "10", "Električne štitnice" }, { "11", "Prilazni signali" }
        };

        internal static string Vrsta(string sifra)
        {
            string v;
            return Vrste.TryGetValue((sifra ?? "").Trim(), out v) ? v : null;
        }

        internal static string Osiguranje(string sifra)
        {
            string v;
            return Osiguranja.TryGetValue((sifra ?? "").Trim(), out v) ? v : null;
        }
    }
}
