using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MimeKit;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera.Radionica
{
    /// <summary>Uvezena poruka (CimRadionica): ulaz za obradu + podaci za listu, niti i analizu.</summary>
    internal sealed class UvezenaPoruka
    {
        /// <summary>Stabilan ključ poruke u radnom prostoru (Message-ID, ili heš sadržaja kada ga nema).</summary>
        public string Kljuc = "";
        public string Izvor = "";
        /// <summary>Putanja fajla/foldera iz kog je poruka uvezena (relativno ime za prikaz).</summary>
        public string Putanja = "";
        public MimeMessage Poruka;
        public DateTimeOffset Datum;
        public string Od = "", Naslov = "", MessageId = "";
        public List<string> Prilozi = new List<string>();
        /// <summary>Ključ niti (naslov bez RE/FW/AW/Odg + reference).</summary>
        public string Nit = "";
        /// <summary>Beleške korisnika uz poruku („X - napomena.txt/.docx“, „Zapazanja.docx/.txt“) – samo za ANALIZA.md.</summary>
        public List<(string ime, string tekst)> Beleske = new List<(string, string)>();
        public long Uid;

        public UlaznaPoruka ZaObradu() => new UlaznaPoruka { Poruka = Poruka, Uid = Uid, Folder = "RADIONICA", Kljuc = Kljuc, Izvor = Izvor };
    }

    /// <summary>
    /// v3.0 (2.1): uvoz poruka za CimRadionica – .msg (Outlook, MsgReader), .eml, folder (prilozi + mail.txt/mail.docx), .zip.
    /// Poruke se grupišu u niti i ređaju hronološki. Fajlovi „_Analiza“ i beleške korisnika se ne uvoze kao poruke.
    /// </summary>
    internal static class UvozPoruka
    {
        private static readonly string[] TeloFajlovi = { "mail.txt", "mail.docx", "telo.txt", "body.txt" };

        /// <summary>Beleška korisnika: „X - napomena.txt/.docx“, „Zapazanja.docx/.txt“ (ne uvozi se kao poruka ni kao prilog).</summary>
        public static bool JeBeleska(string ime)
        {
            var n = Path.GetFileName(ime ?? "");
            return Regex.IsMatch(n, @"(?i)^(.+\s-\s)?napomena\.(txt|docx)$") || Regex.IsMatch(n, @"(?i)^zapa(z|ž)anja\.(txt|docx)$");
        }

        /// <summary>Folder rezultata analize (ne uvozi se).</summary>
        public static bool JeAnaliza(string putanja) => (putanja ?? "").Split('\\', '/').Any(d => d.Equals("_Analiza", StringComparison.OrdinalIgnoreCase));

        /// <summary>Uvoz jedne putanje (fajl ili folder). Folder sa .eml/.msg fajlovima → svaka poruka posebno; inače folder = jedan mail.</summary>
        public static List<UvezenaPoruka> Uvezi(string putanja, List<string> greske = null)
        {
            greske = greske ?? new List<string>();
            var rez = new List<UvezenaPoruka>();
            try
            {
                if (JeAnaliza(putanja)) return rez;
                if (Directory.Exists(putanja))
                {
                    var mailovi = Directory.GetFiles(putanja).Where(f => Regex.IsMatch(f, @"(?i)\.(eml|msg)$")).ToList();
                    if (mailovi.Count > 0)
                    {
                        foreach (var f in mailovi) rez.AddRange(Uvezi(f, greske));
                        foreach (var d in Directory.GetDirectories(putanja).Where(d => !JeAnaliza(d))) rez.AddRange(Uvezi(d, greske));
                    }
                    else if (Directory.GetFiles(putanja).Any(f => !JeBeleska(f)))
                        rez.Add(IzFoldera(putanja));
                    else foreach (var d in Directory.GetDirectories(putanja).Where(d => !JeAnaliza(d))) rez.AddRange(Uvezi(d, greske));
                }
                else if (Regex.IsMatch(putanja, @"(?i)\.eml$")) rez.Add(Iz(MimeMessage.Load(putanja), "eml", putanja));
                else if (Regex.IsMatch(putanja, @"(?i)\.msg$")) rez.Add(Iz(IzMsg(File.ReadAllBytes(putanja)), "msg", putanja));
                else if (Regex.IsMatch(putanja, @"(?i)\.zip$")) rez.AddRange(IzZipa(putanja, greske));
                else greske.Add(Path.GetFileName(putanja) + ": nepoznat format (uvoze se .msg, .eml, folder i .zip)");
            }
            catch (Exception ex) { greske.Add(Path.GetFileName(putanja) + ": " + ex.Message); }
            foreach (var p in rez) DodajBeleske(p);
            return rez;
        }

        private static UvezenaPoruka Iz(MimeMessage m, string izvor, string putanja)
        {
            var mid = (m.MessageId ?? "").Trim('<', '>');
            var prilozi = Raspakivanje.PriloziMaila(m).ToList();
            var kljuc = mid.Length > 0 ? mid : "hes:" + Prilog.Sha256(Encoding.UTF8.GetBytes((m.Subject ?? "") + "|" + m.Date.ToString("o") + "|" + string.Join(",", prilozi.Select(x => x.ime)))
                .Concat(prilozi.SelectMany(x => x.sadrzaj)).ToArray()).Substring(0, 24);
            return new UvezenaPoruka
            {
                Poruka = m, Izvor = izvor, Putanja = putanja, MessageId = mid, Kljuc = kljuc,
                Datum = m.Date == DateTimeOffset.MinValue ? new DateTimeOffset(File.Exists(putanja) ? File.GetLastWriteTime(putanja) : DateTime.Now) : m.Date,
                Od = m.From.Mailboxes.FirstOrDefault()?.Address ?? "", Naslov = m.Subject ?? "",
                Prilozi = prilozi.Select(x => x.ime).ToList(),
                Nit = KljucNiti(m.Subject),
            };
        }

        /// <summary>Outlook .msg → MimeMessage (zaglavlja, telo tekst/HTML, prilozi; ugnežđena poruka kao message/rfc822).</summary>
        internal static MimeMessage IzMsg(byte[] sadrzaj)
        {
            using (var msg = new MsgReader.Outlook.Storage.Message(new MemoryStream(sadrzaj)))
                return IzMsg(msg);
        }

        private static MimeMessage IzMsg(MsgReader.Outlook.Storage.Message msg)
        {
            var m = new MimeMessage();
            var od = msg.Sender?.Email;
            if (!string.IsNullOrWhiteSpace(od) && od.Contains("@")) m.From.Add(new MailboxAddress(msg.Sender?.DisplayName ?? "", od));
            foreach (var r in msg.Recipients ?? new List<MsgReader.Outlook.Storage.Recipient>())
            {
                if (string.IsNullOrWhiteSpace(r.Email) || !r.Email.Contains("@")) continue;
                var a = new MailboxAddress(r.DisplayName ?? "", r.Email);
                if (r.Type == MsgReader.Outlook.RecipientType.Cc) m.Cc.Add(a);
                else if (r.Type == MsgReader.Outlook.RecipientType.Bcc) m.Bcc.Add(a);
                else m.To.Add(a);
            }
            m.Subject = msg.Subject ?? "";
            var datum = msg.SentOn ?? msg.ReceivedOn;
            if (datum.HasValue) m.Date = datum.Value;
            var h = msg.Headers;
            if (h != null)
            {
                if (!string.IsNullOrEmpty(h.MessageId)) m.MessageId = h.MessageId.Trim('<', '>');
                if (h.InReplyTo != null && h.InReplyTo.Count > 0) m.InReplyTo = h.InReplyTo[0].Trim('<', '>');
                foreach (var x in h.References ?? new List<string>()) m.References.Add(x.Trim('<', '>'));
            }
            var b = new BodyBuilder { TextBody = msg.BodyText ?? "", HtmlBody = string.IsNullOrEmpty(msg.BodyHtml) ? null : msg.BodyHtml };
            foreach (var o in msg.Attachments ?? new List<object>())
            {
                if (o is MsgReader.Outlook.Storage.Attachment a && a.Data != null && !a.IsInline)
                    b.Attachments.Add(string.IsNullOrEmpty(a.FileName) ? "prilog" : a.FileName, a.Data);
                else if (o is MsgReader.Outlook.Storage.Message u)
                {
                    var ug = IzMsg(u);
                    b.Attachments.Add(new MessagePart { Message = ug, ContentDisposition = new ContentDisposition(ContentDisposition.Attachment) { FileName = Regex.Replace(ug.Subject ?? "poruka", @"[\\/:*?""<>|]", "_") + ".eml" } });
                }
            }
            m.Body = b.ToMessageBody();
            return m;
        }

        /// <summary>Folder kao jedan mail: svi fajlovi su prilozi; mail.txt/mail.docx je telo (redovi „From:/Subject:/Date:“ na početku su zaglavlje).</summary>
        internal static UvezenaPoruka IzFoldera(string folder)
        {
            var fajlovi = Directory.GetFiles(folder).Where(f => !JeBeleska(f)).OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
            var teloFajl = fajlovi.FirstOrDefault(f => TeloFajlovi.Contains(Path.GetFileName(f).ToLowerInvariant()));
            var telo = teloFajl == null ? "" : TekstFajla(teloFajl);
            var m = new MimeMessage();
            string Zaglavlje(string ime) => Regex.Match(telo, @"(?im)^" + ime + @":\s*(.+)$").Groups[1].Value.Trim();
            var od = Zaglavlje("From"); var naslov = Zaglavlje("Subject"); var datumT = Zaglavlje("Date");
            var adresa = Regex.Match(od, @"[\w.+\-]+@[\w\-]+(\.[\w\-]+)+").Value;
            if (adresa.Length > 0) m.From.Add(new MailboxAddress(Regex.Replace(od, @"<.*?>|[\w.+\-]+@[\w\-.]+", "").Trim(' ', '"'), adresa));
            m.Subject = naslov.Length > 0 ? naslov : Path.GetFileName(folder);
            m.Date = DateTimeOffset.TryParse(datumT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var d) ? d
                : new DateTimeOffset(fajlovi.Select(x => File.GetLastWriteTime(x)).DefaultIfEmpty(DateTime.Now).Max());
            var b = new BodyBuilder { TextBody = Regex.Replace(telo, @"(?im)\A((From|To|Cc|Subject|Date):.*\r?\n)+\s*", "") };
            foreach (var f in fajlovi.Where(f => f != teloFajl)) b.Attachments.Add(Path.GetFileName(f), File.ReadAllBytes(f));
            m.Body = b.ToMessageBody();
            var p = Iz(m, "folder", folder);
            // folder bez Message-ID: ključ po imenu foldera i sadržaju (stabilan pri ponovnom uvozu)
            p.Kljuc = "folder:" + Prilog.Sha256(Encoding.UTF8.GetBytes(Path.GetFileName(folder) + "|" + string.Join("|", fajlovi.Select(Path.GetFileName)))
                .Concat(fajlovi.Where(f => f != teloFajl).SelectMany(File.ReadAllBytes)).ToArray()).Substring(0, 24);
            return p;
        }

        /// <summary>.zip: poruke (.eml/.msg) u zip-u, inače zip = folder jednog maila.</summary>
        private static List<UvezenaPoruka> IzZipa(string putanja, List<string> greske)
        {
            var tmp = Path.Combine(Path.GetTempPath(), "CimRadionica", "zip-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            try
            {
                using (var z = ZipFile.OpenRead(putanja))
                    foreach (var e in z.Entries.Where(e => e.Length > 0))
                    {
                        var cilj = Path.GetFullPath(Path.Combine(tmp, e.FullName.Replace('/', Path.DirectorySeparatorChar)));
                        if (!cilj.StartsWith(Path.GetFullPath(tmp), StringComparison.OrdinalIgnoreCase)) continue;   // zip slip
                        Directory.CreateDirectory(Path.GetDirectoryName(cilj));
                        e.ExtractToFile(cilj, true);
                    }
                var rez = Uvezi(tmp, greske);
                foreach (var p in rez) { p.Izvor = "zip"; p.Putanja = putanja + "!" + p.Putanja.Substring(Math.Min(p.Putanja.Length, tmp.Length)).TrimStart('\\'); }
                return rez;
            }
            finally { try { Directory.Delete(tmp, true); } catch { } }
        }

        /// <summary>Tekst .txt (UTF-8 / 1250) ili .docx (word/document.xml).</summary>
        internal static string TekstFajla(string f)
        {
            if (f.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                using (var z = ZipFile.OpenRead(f))
                {
                    var e = z.GetEntry("word/document.xml");
                    if (e == null) return "";
                    using (var r = new StreamReader(e.Open(), Encoding.UTF8))
                    {
                        var x = r.ReadToEnd();
                        x = Regex.Replace(x, @"</w:p>", "\n");
                        x = Regex.Replace(x, @"<w:tab/>", "\t");
                        x = Regex.Replace(x, @"<[^>]+>", "");
                        return System.Net.WebUtility.HtmlDecode(x).Trim();
                    }
                }
            }
            var b = File.ReadAllBytes(f);
            try { return new UTF8Encoding(false, true).GetString(b).TrimStart('﻿'); }
            catch { return Encoding.GetEncoding(1250).GetString(b); }
        }

        /// <summary>Beleške korisnika uz poruku: „&lt;ime poruke&gt; - napomena.txt/.docx“ pored fajla, „Zapazanja“ i „napomena“ u folderu maila.</summary>
        private static void DodajBeleske(UvezenaPoruka p)
        {
            try
            {
                var put = p.Putanja.Split('!')[0];
                var folder = Directory.Exists(put) ? put : Path.GetDirectoryName(put);
                if (folder == null || !Directory.Exists(folder)) return;
                var osnova = Directory.Exists(put) ? Path.GetFileName(put) : Path.GetFileNameWithoutExtension(put);
                foreach (var f in Directory.GetFiles(folder).Where(JeBeleska))
                {
                    var ime = Path.GetFileNameWithoutExtension(f);
                    var zaOvu = Directory.Exists(put) || ime.StartsWith(osnova + " - ", StringComparison.OrdinalIgnoreCase) || Regex.IsMatch(ime, @"(?i)^zapa(z|ž)anja$");
                    if (zaOvu && !p.Beleske.Any(x => x.ime == Path.GetFileName(f))) p.Beleske.Add((Path.GetFileName(f), TekstFajla(f)));
                }
            }
            catch { }
        }

        /// <summary>Ključ niti: naslov bez RE/FW/Fwd/AW/WG/Odg/Tr/SV/R prefiksa i bez [TEST]/[EXTERNAL] oznaka, malim slovima.</summary>
        public static string KljucNiti(string naslov)
        {
            var t = naslov ?? "";
            for (int i = 0; i < 6; i++)
                t = Regex.Replace(t, @"^\s*(\[[^\]]{1,20}\]\s*)?(re|fw|fwd|aw|wg|odg|odgovor|tr|sv|r|rif|vs)\s*(\[\d+\])?\s*:\s*", "", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"\[(external mail|external|test|interno)\]", "", RegexOptions.IgnoreCase);
            return Regex.Replace(t, @"\s+", " ").Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Niti: isti ključ naslova ili veza preko Message-ID/In-Reply-To/References. Svaka nit hronološki; niti po prvoj poruci.
        /// </summary>
        public static List<List<UvezenaPoruka>> Niti(IEnumerable<UvezenaPoruka> poruke)
        {
            var lista = poruke.OrderBy(p => p.Datum).ThenBy(p => p.Kljuc, StringComparer.Ordinal).ToList();
            var roditelj = Enumerable.Range(0, lista.Count).ToArray();
            int Koren(int i) { while (roditelj[i] != i) i = roditelj[i] = roditelj[roditelj[i]]; return i; }
            void Spoji(int a, int b) { a = Koren(a); b = Koren(b); if (a != b) roditelj[Math.Max(a, b)] = Math.Min(a, b); }
            var poNiti = new Dictionary<string, int>(StringComparer.Ordinal);
            var poMid = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < lista.Count; i++)
            {
                var k = lista[i].Nit;
                if (k.Length > 0) { if (poNiti.TryGetValue(k, out var j)) Spoji(i, j); else poNiti[k] = i; }
                if (lista[i].MessageId.Length > 0) poMid[lista[i].MessageId] = i;
            }
            for (int i = 0; i < lista.Count; i++)
            {
                var m = lista[i].Poruka;
                foreach (var r in m.References.Concat(string.IsNullOrEmpty(m.InReplyTo) ? new string[0] : new[] { m.InReplyTo }))
                    if (poMid.TryGetValue(r.Trim('<', '>'), out var j)) Spoji(i, j);
            }
            return Enumerable.Range(0, lista.Count).GroupBy(Koren).Select(g => g.Select(i => lista[i]).ToList()).OrderBy(g => g[0].Datum).ToList();
        }
    }
}
