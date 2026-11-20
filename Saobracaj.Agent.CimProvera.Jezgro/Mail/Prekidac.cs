using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MimeKit;

namespace Saobracaj.Agent.CimProvera.Mail
{
    /// <summary>
    /// v3.0.5: prekidač slanja – proverava se pri SVAKOM slanju (Slanje.Posalji) i na početku svakog prolaza agenta (AgentRad).
    /// <list type="bullet">
    /// <item>fajl <see cref="ZaustavnoIme"/> u folderu agenta (hitno isključivanje) → ne šalje se ništa;</item>
    /// <item>Rezim = Test → šalje se isključivo na agenti@kprevoz.co.rs (svaki drugi primalac se uklanja, pa se proverava da nije ostao nijedan);</item>
    /// <item>Rezim = Live, samo uz odobren spisak pošiljalaca (OdobreniPosiljaoci) → spoljni primaoci samo sa spiska, interni domen uvek;</item>
    /// <item>bilo šta drugo (prazno, pogrešno napisano, Live bez spiska, config se ne čita) → ne šalje se ništa, INTERNO (log + stanje\neposlato).</item>
    /// </list>
    /// Vrednost Rezim se čita iz exe.config pri svakom slanju (izmena važi odmah); ako se proces pokrenuo u Test režimu, važi Test.
    /// </summary>
    internal static class Prekidac
    {
        /// <summary>Fajl hitnog isključivanja (Iskljuci-Hitno.ps1) u folderu agenta: dok postoji, agent ne šalje ništa i ne obrađuje poštu.</summary>
        public const string ZaustavnoIme = "SLANJE_ZAUSTAVLJENO";

        /// <summary>Folder programa u kom se traži fajl hitnog isključivanja (testovi ga menjaju).</summary>
        internal static Func<string> FolderAgenta = () => AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>Režim koji važi sada: „Test“, „Live“ ili null (slanje nije dozvoljeno) uz razlog.</summary>
        internal static Rezim? Vazeci(AgentConfig cfg, out string razlog)
        {
            razlog = null;
            try
            {
                var stop = Path.Combine(FolderAgenta() ?? "", ZaustavnoIme);
                if (File.Exists(stop)) { razlog = "hitno isključivanje (fajl " + ZaustavnoIme + " u folderu agenta)"; return null; }
            }
            catch (Exception ex) { razlog = "fajl hitnog isključivanja ne može da se proveri: " + ex.Message; return null; }
            string tekst;
            try { tekst = cfg.RezimSada != null ? cfg.RezimSada() : cfg.RezimTekst; }
            catch (Exception ex) { razlog = "ključ Rezim ne može da se pročita iz config-a: " + ex.Message; return null; }
            // ključa nema (null) = podrazumevano Test; prazna ili pogrešna vrednost = ne šalje se ništa
            tekst = tekst == null ? "Test" : tekst.Trim();
            // proces pokrenut u Test režimu ostaje u Test-u (poruke su sastavljene za Test)
            if (string.Equals(tekst, "Test", StringComparison.OrdinalIgnoreCase) || cfg.Rezim == Rezim.Test && string.Equals(tekst, "Live", StringComparison.OrdinalIgnoreCase))
                return Rezim.Test;
            if (string.Equals(tekst, "Live", StringComparison.OrdinalIgnoreCase))
            {
                if (cfg.OdobreniPosiljaoci == null || cfg.OdobreniPosiljaoci.Count == 0) { razlog = "režim Live bez odobrenog spiska pošiljalaca (OdobreniPosiljaoci)"; return null; }
                return Rezim.Live;
            }
            razlog = tekst.Length == 0 ? "ključ Rezim je prazan (nije ni Test ni Live)" : "režim „" + tekst + "“ nije ni Test ni Live";
            return null;
        }

        /// <summary>Razlog zbog kog prolaz agenta ne sme da radi (null = sme).</summary>
        internal static string RazlogBlokade(AgentConfig cfg) => Vazeci(cfg, out var r) == null ? r : null;

        /// <summary>Adresa je na spisku: tačna adresa ili domen („@firma.rs“ ili „firma.rs“).</summary>
        internal static bool NaSpisku(string adresa, IEnumerable<string> spisak)
        {
            var a = PrimaociLive.Ocisti(adresa).ToLowerInvariant();
            var at = a.LastIndexOf('@');
            if (at < 0) return false;
            var domen = a.Substring(at + 1);
            foreach (var x0 in spisak ?? Enumerable.Empty<string>())
            {
                var x = PrimaociLive.Ocisti(x0).ToLowerInvariant();
                if (x.Length == 0) continue;
                if (x.Contains("@") && !x.StartsWith("@")) { if (x == a) return true; }
                else if (x.TrimStart('@') == domen) return true;
            }
            return false;
        }

        /// <summary>
        /// Odluka za jednu poruku: null = sme da se pošalje (primaoci su već svedeni na dozvoljene), inače razlog blokade.
        /// Svaki uklonjen primalac se upisuje kroz <paramref name="zapis"/>.
        /// </summary>
        internal static string Proveri(AgentConfig cfg, MimeMessage m, Action<string> zapis)
        {
            var rezim = Vazeci(cfg, out var razlog);
            if (rezim == null) return razlog;
            if (rezim == Rezim.Test)
            {
                Slanje.SamoTestnaAdresa(m, zapis);
                var svi = Svi(m);
                if (svi.Count == 0 || svi.Any(x => !string.Equals((x.Address ?? "").Trim(), Slanje.TestnaAdresa, StringComparison.OrdinalIgnoreCase)))
                    return "režim Test: posle zaštite je ostao primalac koji nije " + Slanje.TestnaAdresa;
                return null;
            }
            // Live: interni domen uvek, spoljni samo sa odobrenog spiska
            foreach (var lista in new[] { m.To, m.Cc, m.Bcc })
                foreach (var a in lista.ToList())
                {
                    var adrese = a is MailboxAddress mb ? new[] { mb } : a is GroupAddress g ? g.Members.Mailboxes.ToArray() : new MailboxAddress[0];
                    if (a is MailboxAddress && adrese.All(x => cfg.JeInterni(x.Address) || NaSpisku(x.Address, cfg.OdobreniPosiljaoci))) continue;
                    lista.Remove(a);
                    foreach (var x in adrese) zapis?.Invoke("LIVE: uklonjen primalac van odobrenog spiska " + x.Address);
                }
            return Svi(m).Count == 0 ? "režim Live: nijedan primalac nije na odobrenom spisku ni u internom domenu" : null;
        }

        private static List<MailboxAddress> Svi(MimeMessage m) => m.To.Mailboxes.Concat(m.Cc.Mailboxes).Concat(m.Bcc.Mailboxes).ToList();
    }
}
