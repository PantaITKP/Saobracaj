using System;
using System.Collections.Generic;

namespace Saobracaj.Agent.TelegramiImport.Mail
{
    public sealed class Prilog
    {
        public string Naziv { get; set; }
        public string ContentType { get; set; }
        public byte[] Sadrzaj { get; set; }

        public bool JePdf
        {
            get
            {
                return (Naziv ?? "").EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    public sealed class Poruka
    {
        public string Folder { get; set; } = "INBOX";
        public long UidValidity { get; set; }
        public long Uid { get; set; }
        public string MessageId { get; set; }
        public DateTime? Primljeno { get; set; }
        public string Posiljalac { get; set; }
        public string Naslov { get; set; }
        public string TekstMaila { get; set; }
        public List<Prilog> Prilozi { get; } = new List<Prilog>();
    }
}
