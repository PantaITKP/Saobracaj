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

    /// <summary>v3.0: stvarno SMTP slanje agenta (postavlja se kao Slanje.Transport u Program-u). Zaštita režima Test je u Slanje.Posalji.</summary>
    internal static class SmtpSlanje
    {
        public static void Posalji(AgentConfig cfg, MimeMessage m)
        {
            using (var smtp = new SmtpClient())
            {
                smtp.Timeout = 120000;
                smtp.Connect(cfg.SmtpHost, cfg.SmtpPort, cfg.SmtpSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
                smtp.Authenticate(cfg.Nalog, cfg.Lozinka);
                smtp.Send(m);
                smtp.Disconnect(true);
            }
        }
    }
}
