using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
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
        /// <summary>PDF nastao pretvaranjem Word priloga: natpisi obrasca su slika (OCR samo za sidra), vrednosti iz tekstualnog sloja.</summary>
        public bool IzWorda;
        /// <summary>Već pročitane strane (test podaci „&lt;ime&gt;.strane.json“ iz keša agenta): čitač ih koristi umesto ponovnog čitanja.</summary>
        public List<Citanje.Strana> Strane;
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

        /// <summary>
        /// v2.8.1: „&lt;ime&gt;.strane.json“ (već pročitane strane) se prihvata SAMO u testovima (jedinični testovi i
        /// <c>validate --test-strane</c>). U obradi maila je uvek isključeno – inače bi svako mogao da pošalje izmišljen
        /// „pročitan“ sadržaj koji bi agent proverio kao pravi tovarni list.
        /// </summary>
        internal static bool DozvoliProcitaneStrane;

        public const string NedozvoljenTip = "nedozvoljen tip priloga";

        public static List<Prilog> Raspakuj(string naziv, byte[] sadrzaj, List<string> preskoceno, int dubina = 0)
        {
            var rez = new List<Prilog>();
            if (sadrzaj == null || sadrzaj.Length == 0) return rez;
            var ext = (Path.GetExtension(naziv) ?? "").ToLowerInvariant();
            if (naziv.EndsWith(".strane.json", StringComparison.OrdinalIgnoreCase) && !DozvoliProcitaneStrane)
            {
                preskoceno.Add(naziv + " (" + NedozvoljenTip + ")");
                return rez;
            }
            // test podaci: pročitane strane iz keša agenta („CIM.pdf.strane.json“ = prilog „CIM.pdf“ čiji original nije dostupan)
            if (naziv.EndsWith(".strane.json", StringComparison.OrdinalIgnoreCase))
            {
                string json; using (var sr = new StreamReader(new MemoryStream(sadrzaj), System.Text.Encoding.UTF8, true)) json = sr.ReadToEnd();
                var strane = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Citanje.Strana>>(json);
                rez.Add(new Prilog { Naziv = naziv.Substring(0, naziv.Length - ".strane.json".Length), Sadrzaj = sadrzaj, Vrsta = VrstaPriloga.Pdf, MediaType = "application/pdf", Strane = strane });
                return rez;
            }
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

        private static readonly System.Text.RegularExpressions.Regex ImeUkrasa = new System.Text.RegularExpressions.Regex(
            @"logo|signature|signatur|potpis|podpis|unterschrift|banner|baner|facebook|linkedin|twitter|instagram|youtube|xing|icon|ikon|social|outlook",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// Slika iz potpisa maila, logo firme ili ikona (ne tovarni list): razlog za log/izveštaj, ili null kada slika može da bude
        /// dokument. Sken ili fotografija tovarnog lista je velika (≥ ~1000 px, desetine KB), pa ova pravila ne mogu da je preskoče:
        /// manja od 15 KB; manje od 250 000 piksela (npr. 500×500); traka/baner (odnos stranica ≥ 3, kraća strana &lt; 500 px);
        /// ime logo/potpis/ikona društvene mreže i manje od 1,5 Mpx.
        /// </summary>
        /// <summary>
        /// v2.9.2 (3.1c): sopstveni PDF primer agenta („PRIMER_…pdf“ ili prosleđen pod drugim imenom – „Untitled attachment 00093.pdf“,
        /// ID 1654): metapodaci PDF-a (Title „Primer ispravnog tovarnog lista“, Producer „CimProvera – Kombinovani prevoz d.o.o.“).
        /// Takav fajl se preskače – ne proverava se i ne upisuje. null = nije PRIMER.
        /// </summary>
        public static string SopstveniPrimer(Prilog p)
        {
            if (p?.Vrsta != VrstaPriloga.Pdf || p.Sadrzaj == null) return null;
            try
            {
                // metapodaci su u Info rečniku (na kraju fajla) – dovoljan je tekstualni pregled bez parsiranja celog PDF-a
                var n = Math.Min(p.Sadrzaj.Length, 64 * 1024);
                var kraj = System.Text.Encoding.ASCII.GetString(p.Sadrzaj, p.Sadrzaj.Length - n, n);
                var poc = System.Text.Encoding.ASCII.GetString(p.Sadrzaj, 0, Math.Min(p.Sadrzaj.Length, 4096));
                foreach (var t in new[] { kraj, poc })
                {
                    if (t.Contains("/Title (Primer ispravnog tovarnog lista)")) return "PDF primer agenta (naslov „Primer ispravnog tovarnog lista“)";
                    // Producer kao UTF-16 („\376\377\000C\000i\000m…“) ili ASCII
                    if (Regex.IsMatch(t, @"/Producer\s*\((?:CimProvera|\\376\\377\\000C\\000i\\000m\\000P\\000r\\000o\\000v\\000e\\000r\\000a)")) return "PDF primer agenta (proizvođač CimProvera)";
                }
            }
            catch { }
            return null;
        }

        /// <summary>v3.0.1 (4.1): isto kao <see cref="Ukras(Prilog)"/> za prilog maila (ime, sadržaj) – za ponavljanje i „samo slike iz potpisa“.</summary>
        public static string Ukras(string ime, byte[] sadrzaj)
        {
            if (sadrzaj == null) return null;
            var ext = Path.GetExtension(ime ?? "").ToLowerInvariant();
            var v = Prepoznaj(ext, sadrzaj, out var media);
            return v == VrstaPriloga.Slika ? Ukras(new Prilog { Naziv = ime ?? "", Sadrzaj = sadrzaj, Vrsta = v, MediaType = media }) : null;
        }

        /// <summary>v3.0.1 (4.2): ime fotografije/skena dokumenta – broj kola (11–12 cifara), „CIM“, „CUV“, „tovarni“, „wagon“, „scan“, viber/whatsapp fotografija.</summary>
        private static readonly System.Text.RegularExpressions.Regex ImeDokumenta = new System.Text.RegularExpressions.Regex(
            @"(?<!\d)\d{11}-?\d(?!\d)|(?<!\d)\d{4}\s?\d{4}\s?\d{3}-?\d(?!\d)|(?<![a-z])(cim|cuv|cit|tovarn\w*|frachtbrief|wagon\w*|vagon\w*|kola|scan\w*|sken\w*|img_\d{8}|viber_image|whatsapp image|photo|foto\w*|dokument\w*)(?![a-z])",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        public static string Ukras(Prilog p)
        {
            if (p?.Vrsta != VrstaPriloga.Slika || p.Sadrzaj == null) return null;
            if (p.Sadrzaj.Length < 15 * 1024) return "mala slika (" + p.Sadrzaj.Length / 1024 + " KB)";
            // v3.0.1 (4.2, UID 171 „26.11.15_Vrsac_ID1606_31785300434-7_K_R1….jpg“): fotografija nije logo – slika iz ZIP-a ili sa imenom
            // dokumenta (broj kola, CIM, scan, viber…) ide na klasifikaciju strane, kao sken (CIM ume da stigne kao fotografija)
            var ime0 = Path.GetFileNameWithoutExtension((p.Naziv ?? "").Split('/', '\\').Last());
            if ((p.Naziv ?? "").Contains("/") || ImeDokumenta.IsMatch(ime0)) return null;
            int w, h;
            try
            {
                using (var ms = new MemoryStream(p.Sadrzaj))
                using (var img = System.Drawing.Image.FromStream(ms, false, false)) { w = img.Width; h = img.Height; }
            }
            catch { return null; }   // ne može da se otvori – čita se (i ide na ručnu proveru ako ne uspe)
            var dim = w + "×" + h + " px";
            if ((long)w * h < 250_000) return "mala slika (" + dim + ")";
            // v3.0.1 (4.2): velika slika (> 1000 px) nije logo, osim trake/banera
            if (Math.Max(w, h) > 1000 && !(Math.Max(w, h) >= 3.0 * Math.Min(w, h) && Math.Min(w, h) < 500)) return null;
            if (Math.Max(w, h) >= 3.0 * Math.Min(w, h) && Math.Min(w, h) < 500) return "traka/baner (" + dim + ")";
            var ime = Path.GetFileNameWithoutExtension((p.Naziv ?? "").Split('/', '\\').Last());
            if (ImeUkrasa.IsMatch(ime) && (long)w * h < 1_500_000) return "logo/ikona po imenu (" + dim + ")";
            return null;
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
                // Word i RTF se pretvaraju u PDF (Syncfusion DocIO); .odt ide na ručnu proveru sa razlogom
                case ".docx": case ".docm": case ".doc": case ".dot": case ".dotx": case ".dotm": case ".rtf": case ".odt": return VrstaPriloga.Word;
            }
            if (b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) { media = "image/jpeg"; return VrstaPriloga.Slika; }
            if (b.Length > 8 && b[0] == 0x89 && b[1] == 'P' && b[2] == 'N' && b[3] == 'G') { media = "image/png"; return VrstaPriloga.Slika; }
            return VrstaPriloga.Ostalo;
        }

        private static bool JeZip(byte[] b) =>
            b.Length > 4 && b[0] == 'P' && b[1] == 'K' && b[2] == 3 && b[3] == 4;
    }
}
