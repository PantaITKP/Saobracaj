using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;

namespace Saobracaj.Agent.TelegramiImport.Mail
{
    /// <summary>
    /// Čita nove poruke iz sandučeta SAMO ZA ČITANJE: folder se otvara readonly (EXAMINE), poruke se preuzimaju
    /// sa BODY.PEEK[] (MailKit GetMessage), flagovi se ne menjaju, ništa se ne briše ni ne premešta.
    /// </summary>
    internal sealed class ImapIzvor : IDisposable
    {
        private readonly ImapClient client = new ImapClient();
        private IMailFolder folder;

        public long UidValidity { get; private set; }

        public void Otvori(AgentConfig cfg)
        {
            if (string.IsNullOrEmpty(cfg.ImapLozinka))
                throw new InvalidOperationException("IMAP lozinka nije podešena (ImapLozinka.dpapi u App.config).");
            client.Timeout = 60000;
            client.Connect(cfg.ImapHost, cfg.ImapPort, cfg.ImapSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
            client.Authenticate(cfg.ImapKorisnik, cfg.ImapLozinka);
            folder = string.Equals(cfg.ImapFolder, "INBOX", StringComparison.OrdinalIgnoreCase) ? client.Inbox : client.GetFolder(cfg.ImapFolder);
            folder.Open(FolderAccess.ReadOnly);
            UidValidity = folder.UidValidity;
        }

        /// <summary>UID-ovi poruka posle poslednjeg obrađenog; pri prvom pokretanju od datuma ImportOd.</summary>
        public IList<UniqueId> Nove(long poslednjiUid, DateTime importOd)
        {
            SearchQuery upit = poslednjiUid > 0
                ? (SearchQuery)SearchQuery.Uids(new UniqueIdRange(new UniqueId((uint)poslednjiUid + 1), UniqueId.MaxValue))
                : SearchQuery.DeliveredAfter(importOd.AddDays(-1));
            return folder.Search(upit).Where(u => u.Id > poslednjiUid).OrderBy(u => u.Id).ToList();
        }

        public Poruka Preuzmi(UniqueId uid)
        {
            var m = folder.GetMessage(uid);
            var p = new Poruka
            {
                Folder = folder.FullName,
                UidValidity = UidValidity,
                Uid = uid.Id,
                MessageId = m.MessageId,
                Primljeno = m.Date == DateTimeOffset.MinValue ? (DateTime?)null : m.Date.LocalDateTime,
                Posiljalac = m.From.Mailboxes.FirstOrDefault()?.Address,
                Naslov = m.Subject,
                TekstMaila = m.TextBody ?? m.HtmlBody
            };
            foreach (var a in m.Attachments.OfType<MimePart>())
            {
                using (var ms = new MemoryStream())
                {
                    a.Content.DecodeTo(ms);
                    p.Prilozi.Add(new Prilog { Naziv = a.FileName, ContentType = a.ContentType?.MimeType, Sadrzaj = ms.ToArray() });
                }
            }
            return p;
        }

        public void Dispose()
        {
            try { if (client.IsConnected) client.Disconnect(true); } catch { /* zatvaranje ne sme da obori agent */ }
            client.Dispose();
        }
    }

    /// <summary>
    /// Izvor za test i prvo poređenje: izvoz sandučeta (folderi sa poruka.txt + prilozi, skripta analiza_telegrama.py).
    /// Folder = "EXPORT", UID = redni broj foldera, tako da se ne meša sa IMAP porukama.
    /// </summary>
    internal static class FolderIzvor
    {
        public static IEnumerable<Poruka> Citaj(string koren)
        {
            foreach (var dir in Directory.GetDirectories(koren).OrderBy(x => x, StringComparer.Ordinal))
            {
                var ime = Path.GetFileName(dir);
                if (!long.TryParse(ime.Substring(0, Math.Min(4, ime.Length)), out var rb)) continue;
                var p = new Poruka { Folder = "EXPORT", UidValidity = 1, Uid = rb, MessageId = "export:" + ime };
                var txt = Path.Combine(dir, "poruka.txt");
                if (File.Exists(txt))
                {
                    var linije = File.ReadAllLines(txt);
                    int telo = Array.FindIndex(linije, l => l.StartsWith("-----"));
                    foreach (var l in linije.Take(telo < 0 ? 10 : telo))
                    {
                        if (l.StartsWith("Naslov: ")) p.Naslov = l.Substring(8);
                        else if (l.StartsWith("Od: ")) p.Posiljalac = l.Substring(4);
                        else if (l.StartsWith("Datum: ") && DateTimeOffset.TryParse(l.Substring(7), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d))
                            p.Primljeno = d.LocalDateTime;
                    }
                    if (telo >= 0) p.TekstMaila = string.Join("\n", linije.Skip(telo + 1));
                }
                foreach (var f in Directory.GetFiles(dir).Where(f => !f.EndsWith("poruka.txt", StringComparison.OrdinalIgnoreCase)))
                    p.Prilozi.Add(new Prilog { Naziv = Path.GetFileName(f), Sadrzaj = File.ReadAllBytes(f),
                        ContentType = f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : null });
                yield return p;
            }
        }
    }
}
