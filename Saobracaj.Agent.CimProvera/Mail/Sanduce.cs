using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailKit.Security;
using MimeKit;

namespace Saobracaj.Agent.CimProvera.Mail
{
    /// <summary>
    /// IMAP SAMO ZA ČITANJE: folder se otvara readonly (EXAMINE), poruke se preuzimaju bez menjanja flagova,
    /// ništa se ne briše ni ne premešta (kao TelegramiImport).
    /// </summary>
    internal sealed class Sanduce : IDisposable
    {
        private readonly ImapClient client = new ImapClient();
        private readonly AgentConfig cfg;

        public Sanduce(AgentConfig cfg) { this.cfg = cfg; }

        public void Povezi()
        {
            if (string.IsNullOrEmpty(cfg.Lozinka))
                throw new InvalidOperationException(AgentConfig.NedesifrovaneTajne.Contains("Lozinka")
                    ? "Lozinka za " + cfg.Nalog + " u config-u ne može da se dešifruje pod nalogom " + Environment.UserName + " – ponovo pokrenuti Instaliraj.ps1."
                    : "Lozinka za " + cfg.Nalog + " nije podešena (Lozinka.dpapi u config-u ili CIMPROVERA_LOZINKA).");
            client.Timeout = 120000;
            client.Connect(cfg.ImapHost, cfg.ImapPort, cfg.ImapSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
            client.Authenticate(cfg.Nalog, cfg.Lozinka);
        }

        /// <summary>Folder po punom imenu (za čitanje prvog izveštaja pri poređenju).</summary>
        public IMailFolder Folder(string ime) =>
            string.Equals(ime, "INBOX", StringComparison.OrdinalIgnoreCase) ? client.Inbox : client.GetFolder(ime);

        public IMailFolder Dolazni() =>
            string.Equals(cfg.DolazniFolder, "INBOX", StringComparison.OrdinalIgnoreCase) ? client.Inbox : client.GetFolder(cfg.DolazniFolder);

        /// <summary>Folder poslatih: iz config-a, ili poseban folder Sent sa servera, ili po uobičajenim imenima.</summary>
        public IMailFolder Odlazni()
        {
            if (!string.IsNullOrWhiteSpace(cfg.OdlazniFolder)) return client.GetFolder(cfg.OdlazniFolder);
            if ((client.Capabilities & (ImapCapabilities.SpecialUse | ImapCapabilities.XList)) != 0)
            {
                try { var s = client.GetFolder(SpecialFolder.Sent); if (s != null) return s; } catch { }
            }
            var personal = client.GetFolder(client.PersonalNamespaces[0]);
            var imena = new[] { "Sent", "INBOX.Sent", "Sent Items", "Sent Messages", "Poslato", "INBOX.Poslato", "Poslate poruke" };
            foreach (var f in personal.GetSubfolders(false).Concat(client.Inbox.GetSubfolders(false)))
                if (imena.Any(i => string.Equals(i, f.FullName, StringComparison.OrdinalIgnoreCase) || string.Equals(i, f.Name, StringComparison.OrdinalIgnoreCase)))
                    return f;
            throw new InvalidOperationException("Folder poslatih poruka nije pronađen; upisati OdlazniFolder u config.");
        }

        public static IList<UniqueId> Nove(IMailFolder folder, long poslednjiUid, DateTime importOd)
        {
            if (!folder.IsOpen) folder.Open(FolderAccess.ReadOnly);
            SearchQuery upit = poslednjiUid > 0
                ? (SearchQuery)SearchQuery.Uids(new UniqueIdRange(new UniqueId((uint)poslednjiUid + 1), UniqueId.MaxValue))
                : SearchQuery.DeliveredAfter(importOd.AddDays(-1));
            return folder.Search(upit).Where(u => u.Id > poslednjiUid).OrderBy(u => u.Id).ToList();
        }

        public void Dispose()
        {
            try { if (client.IsConnected) client.Disconnect(true); } catch { }
            client.Dispose();
        }
    }

    internal static class Slanje
    {
        public const string ZaglavljeAgenta = "X-KP-CimProvera";
        /// <summary>Zaglavlje svakog odgovora agenta (v2.7.1): „X-CimProvera: odgovor“ – takve poruke agent ne obrađuje.</summary>
        public const string ZaglavljeOdgovora = "X-CimProvera";

        /// <summary>Zaglavlja automatskog odgovora agenta (zaštita od petlji: odgovor stiže u isto sanduče koje agent čita).</summary>
        public static void OznaciOdgovor(MimeMessage m)
        {
            m.Headers.Replace(ZaglavljeAgenta, "1");
            m.Headers.Replace(ZaglavljeOdgovora, "odgovor");
            m.Headers.Replace("Auto-Submitted", "auto-replied");
            m.Headers.Replace("X-Auto-Response-Suppress", "All");
        }

        public static void Posalji(AgentConfig cfg, MimeMessage m)
        {
            OznaciOdgovor(m);
            using (var smtp = new SmtpClient())
            {
                smtp.Timeout = 120000;
                smtp.Connect(cfg.SmtpHost, cfg.SmtpPort, cfg.SmtpSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
                smtp.Authenticate(cfg.Nalog, cfg.Lozinka);
                smtp.Send(m);
                smtp.Disconnect(true);
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
