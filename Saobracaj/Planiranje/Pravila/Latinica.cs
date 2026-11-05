using System.Collections.Generic;
using System.Text;

namespace Saobracaj.Planiranje.Pravila
{
    internal static class Latinica
    {
        private static readonly Dictionary<char, string> Mapa = new Dictionary<char, string>
        {
            {'а',"a"},{'б',"b"},{'в',"v"},{'г',"g"},{'д',"d"},{'ђ',"đ"},{'е',"e"},{'ж',"ž"},{'з',"z"},{'и',"i"},
            {'ј',"j"},{'к',"k"},{'л',"l"},{'љ',"lj"},{'м',"m"},{'н',"n"},{'њ',"nj"},{'о',"o"},{'п',"p"},{'р',"r"},
            {'с',"s"},{'т',"t"},{'ћ',"ć"},{'у',"u"},{'ф',"f"},{'х',"h"},{'ц',"c"},{'ч',"č"},{'џ',"dž"},{'ш',"š"}
        };

        internal static string IzCirilice(string tekst)
        {
            if (string.IsNullOrEmpty(tekst)) return tekst;
            var sb = new StringBuilder(tekst.Length + 4);
            for (int i = 0; i < tekst.Length; i++)
            {
                char z = tekst[i];
                string lat;
                if (!Mapa.TryGetValue(char.ToLowerInvariant(z), out lat))
                {
                    sb.Append(z);
                    continue;
                }
                if (!char.IsUpper(z))
                {
                    sb.Append(lat);
                    continue;
                }
                // Digraf u reči pisanoj velikim slovima je ceo veliki (ЉИГ -> LJIG), inače samo prvo slovo (Љиг -> Ljig).
                bool celaVelika = (i + 1 < tekst.Length && char.IsUpper(tekst[i + 1]))
                    || (i > 0 && char.IsUpper(tekst[i - 1]));
                sb.Append(celaVelika ? lat.ToUpperInvariant() : char.ToUpperInvariant(lat[0]) + lat.Substring(1));
            }
            return sb.ToString();
        }
    }
}
