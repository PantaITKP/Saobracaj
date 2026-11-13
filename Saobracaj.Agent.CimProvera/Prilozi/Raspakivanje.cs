using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using MimeKit;

namespace Saobracaj.Agent.CimProvera.Prilozi
{
    internal enum VrstaPriloga { Pdf, Slika, Excel, Word, Ostalo }

    /// <summary>Jedan fajl za proveru (posle raspakivanja ZIP/EML/MSG).</summary>
    internal sealed class Prilog
    {
        public string Naziv;          // putanja unutar maila, npr. "Dokumentacija.zip/T1.pdf"
        public byte[] Sadrzaj;
        public VrstaPriloga Vrsta;
        public string MediaType;      // za Claude: application/pdf, image/jpeg, …
        public string Hes => hes ?? (hes = Sha256(Sadrzaj));
        private string hes;

        public static string Sha256(byte[] b)
        {
            using (var s = SHA256.Create())
                return BitConverter.ToString(s.ComputeHash(b)).Replace("-", "").ToLowerInvariant();
        }

        public override string ToString() => Naziv + " (" + Sadrzaj.Length / 1024 + " KB, " + Hes.Substring(0, 12) + ")";
    }

    /// <summary>
    /// Raspakuje priloge maila do fajlova koje agent ume da pročita: PDF, slike, Excel. ZIP, EML i MSG se otvaraju
    /// rekurzivno (do 5 nivoa). Ostali tipovi (doc, docx, txt…) se preskaču i navode u logu.
    /// </summary>
    internal static class Raspakivanje
    {
        private const int MaxDubina = 5;

        public static List<Prilog> Raspakuj(string naziv, byte[] sadrzaj, List<string> preskoceno, int dubina = 0)
        {
            var rez = new List<Prilog>();
            if (sadrzaj == null || sadrzaj.Length == 0) return rez;
            var ext = (Path.GetExtension(naziv) ?? "").ToLowerInvariant();
            var vrsta = Prepoznaj(ext, sadrzaj, out var media);

            if (vrsta != VrstaPriloga.Ostalo)
            {
                rez.Add(new Prilog { Naziv = naziv, Sadrzaj = sadrzaj, Vrsta = vrsta, MediaType = media });
                return rez;
            }
            if (dubina >= MaxDubina) { preskoceno.Add(naziv + " (previše ugnježdeno)"); return rez; }

            try
            {
                if (ext == ".zip" || JeZip(sadrzaj))
                {
                    using (var z = new ZipArchive(new MemoryStream(sadrzaj), ZipArchiveMode.Read))
                        foreach (var e in z.Entries.Where(e => e.Length > 0 && !e.FullName.EndsWith("/")))
                            using (var s = e.Open())
                            using (var ms = new MemoryStream())
                            {
                                s.CopyTo(ms);
                                rez.AddRange(Raspakuj(naziv + "/" + e.FullName, ms.ToArray(), preskoceno, dubina + 1));
                            }
                    return rez;
                }
                if (ext == ".eml")
                {
                    var m = MimeMessage.Load(new MemoryStream(sadrzaj));
                    foreach (var (ime, b) in PriloziMaila(m))
                        rez.AddRange(Raspakuj(naziv + "/" + ime, b, preskoceno, dubina + 1));
                    return rez;
                }
                if (ext == ".msg")
                {
                    using (var msg = new MsgReader.Outlook.Storage.Message(new MemoryStream(sadrzaj)))
                        rez.AddRange(IzMsg(naziv, msg, preskoceno, dubina + 1));
                    return rez;
                }
            }
            catch (Exception ex)
            {
                preskoceno.Add(naziv + " (ne može da se otvori: " + ex.Message + ")");
                return rez;
            }

            preskoceno.Add(naziv + " (tip fajla se ne proverava)");
            return rez;
        }

        private static IEnumerable<Prilog> IzMsg(string naziv, MsgReader.Outlook.Storage.Message msg, List<string> preskoceno, int dubina)
        {
            foreach (var o in msg.Attachments)
            {
                if (o is MsgReader.Outlook.Storage.Attachment a && a.Data != null)
                    foreach (var p in Raspakuj(naziv + "/" + (a.FileName ?? "prilog"), a.Data, preskoceno, dubina)) yield return p;
                else if (o is MsgReader.Outlook.Storage.Message ugnjezdena)
                    foreach (var p in IzMsg(naziv + "/" + (ugnjezdena.Subject ?? "poruka"), ugnjezdena, preskoceno, dubina + 1)) yield return p;
            }
        }

        /// <summary>Svi prilozi MIME poruke (i ugnježdene poruke message/rfc822 kao .eml).</summary>
        public static IEnumerable<(string ime, byte[] sadrzaj)> PriloziMaila(MimeMessage m)
        {
            foreach (var e in m.Attachments)
            {
                if (e is MessagePart mp)
                {
                    using (var ms = new MemoryStream())
                    {
                        mp.Message.WriteTo(ms);
                        yield return ((mp.Message.Subject ?? "poruka") + ".eml", ms.ToArray());
                    }
                }
                else if (e is MimePart part)
                {
                    using (var ms = new MemoryStream())
                    {
                        part.Content.DecodeTo(ms);
                        yield return (part.FileName ?? part.ContentType?.Name ?? "prilog", ms.ToArray());
                    }
                }
            }
            // Slike ugrađene u telo (inline) sa imenom fajla – često fotografije CIM-a poslate sa telefona
            foreach (var part in m.BodyParts.OfType<MimePart>()
                         .Where(p => !p.IsAttachment && p.ContentType.MediaType == "image" && !string.IsNullOrEmpty(p.FileName)))
            {
                using (var ms = new MemoryStream())
                {
                    part.Content.DecodeTo(ms);
                    if (ms.Length > 30 * 1024) // logotipi iz potpisa se preskaču
                        yield return (part.FileName, ms.ToArray());
                }
            }
        }

        private static VrstaPriloga Prepoznaj(string ext, byte[] b, out string media)
        {
            media = null;
            if (ext == ".pdf" || (b.Length > 4 && b[0] == '%' && b[1] == 'P' && b[2] == 'D' && b[3] == 'F'))
            { media = "application/pdf"; return VrstaPriloga.Pdf; }
            switch (ext)
            {
                case ".jpg": case ".jpeg": media = "image/jpeg"; return VrstaPriloga.Slika;
                case ".png": media = "image/png"; return VrstaPriloga.Slika;
                case ".gif": media = "image/gif"; return VrstaPriloga.Slika;
                case ".webp": media = "image/webp"; return VrstaPriloga.Slika;
                case ".xls": case ".xlsx": case ".xlsm": return VrstaPriloga.Excel;
                case ".docx": case ".docm": return VrstaPriloga.Word;
            }
            if (b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) { media = "image/jpeg"; return VrstaPriloga.Slika; }
            if (b.Length > 8 && b[0] == 0x89 && b[1] == 'P' && b[2] == 'N' && b[3] == 'G') { media = "image/png"; return VrstaPriloga.Slika; }
            return VrstaPriloga.Ostalo;
        }

        private static bool JeZip(byte[] b) =>
            b.Length > 4 && b[0] == 'P' && b[1] == 'K' && b[2] == 3 && b[3] == 4;
    }
}
