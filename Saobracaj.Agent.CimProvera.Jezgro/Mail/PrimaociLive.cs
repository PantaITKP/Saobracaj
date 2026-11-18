using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MimeKit;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>
    /// v2.8.4 (10): primaoci odgovora u Live režimu – „Odgovori svima“: From + To + Cc originalnog maila, bez duplikata, bez agenti@ i
    /// adresa koje automatski prosleđuju na agenti@ (ProsledjivanjeNaAgenti); KP adrese (priprema@, disp@) ostaju; Bcc nije poznat.
    /// Više od 25 primalaca → odgovor ide samo interno. Interni izveštaji (ručna provera, KP tovarni list) nikad ne idu ovim adresama.
    /// Samo priprema: u Test režimu sve ide na IzvestajAdresa, a lista se upisuje u zaglavlje i prvi red test maila.
    /// </summary>
    internal sealed class PrimaociLive
    {
        public const int MaxPrimalaca = 25;
        public const string ZaglavljeLive = "X-CimProvera-Live-Primaoci";

        public List<string> To { get; } = new List<string>();
        public List<string> Cc { get; } = new List<string>();
        /// <summary>Više od 25 primalaca: odgovor ide samo interno (KopijaNa), uz napomenu.</summary>
        public bool SamoInterno;
        public string Napomena = "";
        public int Ukupno => To.Count + Cc.Count;

        /// <summary>
        /// v3.0 (f): adresa bez viška znakova – pošiljaoci pišu „&lt;x@gmail.com'&gt;“ ili „'x@firma.rs'“ (UID 429: 158 takvih u sandučetu);
        /// navodnici i apostrofi oko adrese nisu deo adrese (u Live režimu bi odgovor otišao na neispravnu adresu).
        /// </summary>
        internal static string Ocisti(string a) => (a ?? "").Trim().Trim('<', '>', '\'', '"', '‘', '’', '“', '”', '`', ' ', ';', ',').Trim();

        private static string Adresa(string a) => Ocisti(a).ToLowerInvariant();

        /// <summary>v3.0 (f): očišćene adrese liste (za rezultat, analizu i Excel).</summary>
        internal static List<string> Adrese(InternetAddressList l) => (l?.Mailboxes ?? Enumerable.Empty<MailboxAddress>()).Select(x => Ocisti(x.Address)).Where(x => x.Length > 0).ToList();

        /// <summary>Primaoci za Live iz zaglavlja originalnog maila.</summary>
        public static PrimaociLive Iz(string from, IEnumerable<string> to, IEnumerable<string> cc, string nalogAgenta, IEnumerable<string> prosledjivanje)
        {
            var bez = new HashSet<string>(new[] { nalogAgenta }.Concat(prosledjivanje ?? Enumerable.Empty<string>()).Select(Adresa).Where(x => x.Length > 0));
            var p = new PrimaociLive();
            var vec = new HashSet<string>();
            void Dodaj(List<string> lista, string a)
            {
                var x = Adresa(a);
                if (x.Length == 0 || !x.Contains("@") || bez.Contains(x) || !vec.Add(x)) return;
                lista.Add(x);
            }
            Dodaj(p.To, from);
            foreach (var a in to ?? Enumerable.Empty<string>()) Dodaj(p.To, a);
            foreach (var a in cc ?? Enumerable.Empty<string>()) Dodaj(p.Cc, a);
            if (p.Ukupno > MaxPrimalaca)
            {
                p.SamoInterno = true;
                p.Napomena = "mail ima " + p.Ukupno + " primalaca (više od " + MaxPrimalaca + ") – odgovor ide samo interno";
            }
            return p;
        }

        public static PrimaociLive Iz(MimeMessage m, AgentConfig cfg) =>
            Iz(m.From.Mailboxes.FirstOrDefault()?.Address, m.To.Mailboxes.Select(x => x.Address), m.Cc.Mailboxes.Select(x => x.Address),
               cfg?.Nalog, cfg?.ProsledjivanjeNaAgenti);

        /// <summary>„To: a, b; Cc: c“ (ili „samo interno – …“).</summary>
        public string Opis() =>
            SamoInterno ? "samo interno – " + Napomena
            : "To: " + (To.Count > 0 ? string.Join(", ", To) : "–") + "; Cc: " + (Cc.Count > 0 ? string.Join(", ", Cc) : "–");

        /// <summary>Naslov odgovora u istoj niti: „RE: &lt;originalni naslov&gt; – CIM provera: potrebne ispravke“ (bez dvostrukog „RE:“).</summary>
        public static string Naslov(string original, bool en, bool imaGresaka = true)
        {
            var o = (original ?? "").Trim();
            var osnova = Regex.IsMatch(o, @"^(re|aw|odg|sv)\s*:", RegexOptions.IgnoreCase) ? o : "RE: " + o;
            var oznaka = en ? (imaGresaka ? "CIM check: corrections required" : "CIM check: remarks") : (imaGresaka ? "CIM provera: potrebne ispravke" : "CIM provera: napomene");
            return osnova + " – " + oznaka;
        }
    }
}
