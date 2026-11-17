using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MimeKit;

namespace Saobracaj.Agent.CimProvera.Mail
{
    /// <summary>
    /// Sastav i slanje odgovora agenta (v3.0: u jezgru). Zaštita režima Test u Posalji je ista kao ranije; stvarni SMTP je u agentu
    /// (SmtpSlanje), a CimRadionica umesto slanja samo pamti pripremljenu poruku.
    /// </summary>
    internal static class Slanje
    {
        public const string ZaglavljeAgenta = "X-KP-CimProvera";
        /// <summary>Zaglavlje svakog odgovora agenta (v2.7.1): „X-CimProvera: odgovor“ – takve poruke agent ne obrađuje.</summary>
        public const string ZaglavljeOdgovora = "X-CimProvera";
        /// <summary>Vrsta poruke agenta (npr. „poredjenje“ u režimu Test).</summary>
        public const string ZaglavljeVrste = "X-KP-CimProvera-Vrsta";

        /// <summary>Zaglavlja automatskog odgovora agenta (zaštita od petlji: odgovor stiže u isto sanduče koje agent čita).</summary>
        public static void OznaciOdgovor(MimeMessage m)
        {
            m.Headers.Replace(ZaglavljeAgenta, "1");
            m.Headers.Replace(ZaglavljeOdgovora, "odgovor");
            m.Headers.Replace("Auto-Submitted", "auto-replied");
            m.Headers.Replace("X-Auto-Response-Suppress", "All");
        }

        /// <summary>v2.8.4: jedina dozvoljena adresa u režimu Test (namerno fiksna – ne zavisi od config-a ni od ostalog koda).</summary>
        public const string TestnaAdresa = "agenti@kprevoz.co.rs";

        /// <summary>Log za zaštitu režima Test („TEST: uklonjen primalac …“); postavlja ga Program.</summary>
        public static Log Dnevnik;
        internal static Action<string> ZapisZastite = s => Dnevnik?.Upozorenje(s);

        /// <summary>
        /// Stvarno slanje (SMTP); testovi ga zamenjuju da bi videli šta bi bilo poslato. v3.0: jezgro ne šalje samo – agent postavlja
        /// SMTP (SmtpSlanje.Posalji) na početku rada; CimRadionica nikad ne postavlja SMTP (poruke samo priprema i čuva).
        /// </summary>
        internal static Action<AgentConfig, MimeMessage> Transport = (c, m) => throw new InvalidOperationException("Slanje nije podešeno (transport nije postavljen) – CimRadionica ne šalje mail.");

        public static void Posalji(AgentConfig cfg, MimeMessage m)
        {
            OznaciOdgovor(m);
            // v2.8.4: zaštita režima Test – dok je Rezim = Test, svaki primalac (To, Cc, Bcc) koji nije agenti@kprevoz.co.rs se uklanja
            // pre slanja, bez obzira na to šta je sastavio ostali kod
            if (cfg.Rezim == Rezim.Test) SamoTestnaAdresa(m, ZapisZastite);
            Transport(cfg, m);
        }

        /// <summary>Uklanja sve primaoce (To, Cc, Bcc; i grupe) osim <see cref="TestnaAdresa"/>; svaki uklonjen se upisuje u log.</summary>
        internal static void SamoTestnaAdresa(MimeMessage m, Action<string> zapis)
        {
            foreach (var lista in new[] { m.To, m.Cc, m.Bcc })
                foreach (var a in lista.ToList())
                {
                    var adrese = a is MailboxAddress mb ? new[] { mb } : a is GroupAddress g ? g.Members.Mailboxes.ToArray() : new MailboxAddress[0];
                    if (a is MailboxAddress mb1 && string.Equals((mb1.Address ?? "").Trim(), TestnaAdresa, StringComparison.OrdinalIgnoreCase)) continue;
                    lista.Remove(a);
                    foreach (var x in adrese.Where(x => !string.Equals((x.Address ?? "").Trim(), TestnaAdresa, StringComparison.OrdinalIgnoreCase)))
                        zapis?.Invoke("TEST: uklonjen primalac " + x.Address);
                    // agenti@ iz grupe ostaje kao pojedinačan primalac
                    foreach (var x in adrese.Where(x => string.Equals((x.Address ?? "").Trim(), TestnaAdresa, StringComparison.OrdinalIgnoreCase)))
                        if (!lista.Mailboxes.Any(y => string.Equals(y.Address, TestnaAdresa, StringComparison.OrdinalIgnoreCase))) lista.Add(MailboxAddress.Parse(TestnaAdresa));
                }
            if (!m.To.Mailboxes.Any() && !m.Cc.Mailboxes.Any() && !m.Bcc.Mailboxes.Any())
            {
                m.To.Add(MailboxAddress.Parse(TestnaAdresa));
                zapis?.Invoke("TEST: nijedan primalac nije ostao – mail ide na " + TestnaAdresa);
            }
        }

        public static MimeMessage Nova(AgentConfig cfg, IEnumerable<string> za, IEnumerable<string> cc, string naslov, string telo, MimeMessage odgovorNa = null, string html = null)
        {
            var m = new MimeMessage();
            m.From.Add(MailboxAddress.Parse(cfg.PosiljalacIzvestaja));
            if (!string.IsNullOrWhiteSpace(cfg.ReplyTo)) m.ReplyTo.Add(MailboxAddress.Parse(cfg.ReplyTo));
            foreach (var a in za.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)) m.To.Add(MailboxAddress.Parse(a));
            foreach (var a in (cc ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
                if (!m.To.Mailboxes.Any(x => string.Equals(x.Address, a, StringComparison.OrdinalIgnoreCase))) m.Cc.Add(MailboxAddress.Parse(a));
            m.Subject = naslov;
            if (odgovorNa != null && !string.IsNullOrEmpty(odgovorNa.MessageId))
            {
                m.InReplyTo = odgovorNa.MessageId;
                foreach (var r in odgovorNa.References) m.References.Add(r);
                m.References.Add(odgovorNa.MessageId);
            }
            if (string.IsNullOrEmpty(html)) m.Body = new TextPart("plain") { Text = telo };
            else m.Body = new BodyBuilder { TextBody = telo, HtmlBody = html }.ToMessageBody();
            OznaciOdgovor(m);
            return m;
        }

        /// <summary>Dodaje prilog (npr. PDF primer) u poruku sa tekstom i HTML-om (multipart/mixed oko multipart/alternative).</summary>
        public static void DodajPrilog(MimeMessage m, string ime, byte[] sadrzaj, string tip = "application/pdf")
        {
            var deo = new MimePart(tip)
            {
                Content = new MimeContent(new System.IO.MemoryStream(sadrzaj)),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = ime,
            };
            if (m.Body is Multipart mm && mm.ContentType.MediaSubtype == "mixed") { mm.Add(deo); return; }
            var mix = new Multipart("mixed") { m.Body, deo };
            m.Body = mix;
        }

        /// <summary>Automatske poruke, sopstveni izveštaji i sistemske adrese se ne obrađuju (zaštita od petlji).</summary>
        public static string RazlogPreskakanja(MimeMessage m, AgentConfig cfg, bool dolazni)
        {
            if (string.Equals((m.Headers[ZaglavljeOdgovora] ?? "").Trim(), "odgovor", StringComparison.OrdinalIgnoreCase)) return "sopstveni odgovor";
            if (m.Headers.Contains(ZaglavljeAgenta)) return "sopstveni izveštaj";
            // dodatna zaštita: naslov odgovora agenta („Tovarni list CIM … – potrebne ispravke / – napomene“) od adrese agenta
            var posiljalac = m.From.Mailboxes.FirstOrDefault()?.Address ?? "";
            var naslov = Regex.Replace(m.Subject ?? "", @"\s+", " ").Trim();
            if ((string.Equals(posiljalac, cfg.PosiljalacIzvestaja, StringComparison.OrdinalIgnoreCase) || string.Equals(posiljalac, cfg.Nalog, StringComparison.OrdinalIgnoreCase))
                && Regex.IsMatch(naslov, @"^(\[INTERNO\]\s*)?(Tovarni list (CIM|CUV)|Consignment note (CIM|CUV))\b.*[–-]\s*(potrebne ispravke|napomene|ispravan|corrections required|remarks|correct)$", RegexOptions.IgnoreCase))
                return "sopstveni odgovor (naslov)";
            var auto = m.Headers["Auto-Submitted"];
            if (!string.IsNullOrEmpty(auto) && !auto.Trim().Equals("no", StringComparison.OrdinalIgnoreCase)) return "Auto-Submitted: " + auto;
            var prec = (m.Headers["Precedence"] ?? "").Trim().ToLowerInvariant();
            if (prec == "bulk" || prec == "junk" || prec == "list" || prec == "auto_reply") return "Precedence: " + prec;
            if (dolazni && m.Headers.Contains("X-Auto-Response-Suppress") && (m.Headers["X-Auto-Response-Suppress"] ?? "").IndexOf("All", StringComparison.OrdinalIgnoreCase) >= 0)
                return "X-Auto-Response-Suppress";
            var od = m.From.Mailboxes.FirstOrDefault()?.Address?.ToLowerInvariant() ?? "";
            if (od.StartsWith("mailer-daemon") || od.StartsWith("postmaster") || od.Contains("no-reply") || od.Contains("noreply") || od.Contains("donotreply"))
                return "sistemska adresa " + od;
            return null;
        }
    }
}
