using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Saobracaj.Agent.CimProvera.Radionica
{
    /// <summary>Red Zbirnog Excel-a: jedan mail i jedan tovarni list (mail bez lista = jedan red).</summary>
    internal sealed class RedZbirnog
    {
        public string Kljuc = "";
        public int Rb;
        public StavkaProstora Stavka;
        public string TovarniList = "", Odluka = "", Status = "", Interno = "", Dogadjaji = "", Folder = "";
        public int Gresaka, Upozorenja;
        public Dictionary<string, string> Rubrike = new Dictionary<string, string>();
        public Dictionary<string, string> Posao = new Dictionary<string, string>();
        public double Trajanje;
        public string Tacno = "", Komentar = "";
        /// <summary>Skup nalaza (pravilo|rubrika|težina) – za poređenje verzija.</summary>
        public SortedSet<string> Nalazi = new SortedSet<string>(StringComparer.Ordinal);
    }

    /// <summary>
    /// v3.0 (2.4, 2.5, 2.9): Zbirni Excel (Zbirno_&lt;verzija&gt;.xlsx), poređenje verzija (Excel + HTML, regresija) i paket za analizu.
    /// </summary>
    internal static class Zbirno
    {
        public static readonly string[] Rubrike = { "1", "4", "7", "8", "9", "13", "14", "16", "18", "20", "21", "22", "23", "24", "25", "29", "57", "58a", "62", "99" };
        public static readonly string[] PoljaPosla = { "ID", "OtpravnaID", "UputnaID", "Stanica1ID", "Stanica3ID", "PlatilacID", "NHM1", "BrojKola1", "Neto1", "Tara1", "Bruto1", "NHM2", "BrojKola2", "Neto2", "Tara2", "Bruto2", "Referenca" };

        public static string Putanja(RadniProstor rp, string verzija) => Path.Combine(rp.Koren, "Zbirno_" + RadniProstor.BezbednoIme(verzija, 30) + ".xlsx");

        public static JObject Rezultat(RadniProstor rp, StavkaProstora s, string verzija)
        {
            if (!s.Rezultati.TryGetValue(verzija, out var rel)) return null;
            var f = Path.Combine(rp.Koren, rel, "rezultat.json");
            return File.Exists(f) ? JObject.Parse(File.ReadAllText(f, Encoding.UTF8)) : null;
        }

        /// <summary>Redovi iz rezultat.json svih obrađenih poruka verzije (hronološki).</summary>
        public static List<RedZbirnog> Redovi(RadniProstor rp, string verzija)
        {
            var rez = new List<RedZbirnog>();
            int rb = 0;
            foreach (var s in rp.Hronoloski())
            {
                rb++;
                var j = Rezultat(rp, s, verzija);
                if (j == null) continue;
                var listovi = (j["provera"]?["listovi"] as JArray)?.Cast<JObject>().ToList() ?? new List<JObject>();
                var dog = string.Join("; ", (j["dogadjaji"] as JArray ?? new JArray()).Select(d => d["tip"] + " " + string.Join(" ", new[] { (string)d["mesto"], (string)d["vreme_tekst"], (string)d["vrednost"] }.Where(x => !string.IsNullOrEmpty(x)))));
                var interno = string.Join("; ", (j["upis"]?["interno"] as JArray ?? new JArray()).Select(x => (string)x).Concat((j["primer"]?["interno"] as JArray ?? new JArray()).Select(x => (string)x)));
                var posloviUpis = (j["upis"]?["poslovi"] as JArray ?? new JArray()).Cast<JObject>().ToList();
                var posloviPredlog = (j["poslovi"] as JArray ?? new JArray()).Cast<JObject>().ToList();
                foreach (var l in listovi.Count > 0 ? listovi.Cast<JObject>() : new JObject[] { null })
                {
                    var r = new RedZbirnog
                    {
                        Rb = rb, Stavka = s, Odluka = (string)j["odluka"] ?? "", Status = (string)j["status"] ?? "", Dogadjaji = dog, Interno = interno,
                        Trajanje = (double?)j["trajanje"]?["ukupno"] ?? 0, Folder = s.Rezultati.TryGetValue(verzija, out var rel) ? rel : "",
                    };
                    if (l != null)
                    {
                        r.TovarniList = ((string)l["r62"] ?? "") + " (" + Path.GetFileName(((string)l["fajl"] ?? "").Split('/').Last()) + ")";
                        foreach (var n in (JArray)l["nalazi"])
                        {
                            var t = (string)n["tezina"];
                            if (t == "error") r.Gresaka++; else if (t == "warning") r.Upozorenja++;
                            var rub = (string)n["rubrika"] ?? "";
                            var oznaka = (t == "error" ? "G" : t == "warning" ? "U" : t == "recommendation" ? "P" : "I") + ":" + n["pravilo"] + ((bool?)n["samo_interno"] == true ? "(int)" : "");
                            var kol = Rubrike.Contains(rub) ? rub : "ostalo";
                            r.Rubrike[kol] = r.Rubrike.TryGetValue(kol, out var st) ? st + "; " + oznaka : oznaka;
                            r.Nalazi.Add(n["pravilo"] + "|" + rub + "|" + t);
                        }
                        var predlog = posloviPredlog.FirstOrDefault(p => (string)p["list"] == (string)l["oznaka"]);
                        if (predlog != null) foreach (var p in (JObject)predlog["polja"]) r.Posao[p.Key] = (string)p.Value;
                    }
                    if (posloviUpis.Count > 0)
                    {
                        // online: poslovi maila (više poslova – ID-evi razdvojeni zarezom; polja prvog posla)
                        r.Posao["ID"] = string.Join(", ", posloviUpis.Select(p => (string)p["ID"]));
                        foreach (var p in posloviUpis[0]) if (p.Key != "ID") r.Posao[p.Key] = (string)p.Value;
                    }
                    r.Kljuc = s.Kljuc + "|" + ((string)l?["oznaka"] ?? "");
                    rez.Add(r);
                }
            }
            return rez;
        }

        /// <summary>Ocene „Tačno?“ i „Komentar“ iz postojećeg Zbirnog (po ključu) – ostaju pri ponovnoj obradi.</summary>
        public static Dictionary<string, (string tacno, string komentar)> Ocene(string putanja)
        {
            var d = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
            try
            {
                foreach (var red in Xlsx.Procitaj(putanja, "Zbirno"))
                    if (red.TryGetValue("Ključ", out var k) && k.Length > 0)
                        d[k] = (red.TryGetValue("Tačno?", out var t) ? t : "", red.TryGetValue("Komentar", out var c) ? c : "");
            }
            catch { }
            return d;
        }

        /// <summary>Zbirno_&lt;verzija&gt;.xlsx: postojeće ocene se prenose; ocene iz prethodne verzije (isti ključ) kada nove još nema.</summary>
        public static string Sacuvaj(RadniProstor rp, string verzija, string prethodnaVerzija = null)
        {
            var put = Putanja(rp, verzija);
            var ocene = Ocene(put);
            var stare = prethodnaVerzija != null ? Ocene(Putanja(rp, prethodnaVerzija)) : new Dictionary<string, (string, string)>();
            var redovi = Redovi(rp, verzija);
            foreach (var r in redovi)
            {
                if (ocene.TryGetValue(r.Kljuc, out var o) || stare.TryGetValue(r.Kljuc, out o)) { r.Tacno = o.tacno; r.Komentar = o.komentar; }
            }
            var list = new XlsxList { Ime = "Zbirno" };
            list.Zaglavlje.AddRange(new[] { "NNN", "Datum", "Od", "Naslov", "Tovarni list", "Odluka", "Grešaka", "Upozorenja" });
            list.Zaglavlje.AddRange(Rubrike.Select(x => "r" + x));
            list.Zaglavlje.Add("ostalo");
            list.Zaglavlje.AddRange(PoljaPosla.Select(x => x == "ID" ? "JobID" : x));
            list.Zaglavlje.AddRange(new[] { "INTERNO", "Događaji", "Trajanje (s)", "Tačno?", "Komentar", "Ključ", "Folder" });
            foreach (var r in redovi)
            {
                var red = new List<object> { r.Rb.ToString("000"), r.Stavka.Datum.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), r.Stavka.Od, r.Stavka.Naslov, r.TovarniList, r.Odluka, r.Gresaka, r.Upozorenja };
                red.AddRange(Rubrike.Select(x => (object)(r.Rubrike.TryGetValue(x, out var v) ? v : "")));
                red.Add(r.Rubrike.TryGetValue("ostalo", out var o) ? o : "");
                red.AddRange(PoljaPosla.Select(x => (object)(r.Posao.TryGetValue(x, out var v) ? v : "")));
                red.AddRange(new object[] { r.Interno, r.Dogadjaji, Math.Round(r.Trajanje, 1), r.Tacno, r.Komentar, r.Kljuc, r.Folder });
                list.Redovi.Add(red.ToArray());
                if (r.Interno.Length > 0) list.BojaReda[list.Redovi.Count - 1] = "FFFFF2CC";
                else if (r.Tacno.StartsWith("ne", StringComparison.OrdinalIgnoreCase)) list.BojaReda[list.Redovi.Count - 1] = "FFF8D7DA";
            }
            list.Sirine[3] = 45; list.Sirine[4] = 35; list.Sirine[5] = 35;
            var iTacno = list.Zaglavlje.IndexOf("Tačno?"); list.Sirine[iTacno] = 10; list.Sirine[iTacno + 1] = 40;
            list.Prelom.Add(list.Zaglavlje.IndexOf("Događaji")); list.Prelom.Add(list.Zaglavlje.IndexOf("INTERNO"));
            var uputstvo = new XlsxList { Ime = "Uputstvo" };
            uputstvo.Zaglavlje.Add("Kolona"); uputstvo.Zaglavlje.Add("Značenje");
            foreach (var (k, z) in new[] { ("Tačno?", "da / ne / delimično – ocena korisnika; ostaje pri ponovnoj obradi (po koloni Ključ)."), ("Komentar", "slobodan tekst; ostaje pri ponovnoj obradi."),
                ("r1 … r99", "nalazi po rubrici: G = greška, U = upozorenje, P = preporuka, I = informacija; (int) = samo interno."), ("INTERNO", "interne napomene upisa i PRIMER-a (npr. spisak ≠ tovarni list)."),
                ("JobID …", "online: poslovi u planiranje.tmpJobs posle upisa; offline: predlog iz lokalnih šifarnika (ništa nije upisano)."), ("Ključ", "ne menjati – po njemu se prenose ocene.") })
                uputstvo.Redovi.Add(new object[] { k, z });
            uputstvo.Sirine[1] = 100;
            Xlsx.Sacuvaj(put, list, uputstvo);
            return put;
        }

        // ------------------------------------------------------------------------------------------- poređenje verzija (2.5)

        internal sealed class Razlika
        {
            public string Kljuc = "", Naslov = "", TovarniList = "", Polje = "", Bilo = "", Sada = "";
            public bool Regresija;
            public string Tacno = "";
        }

        /// <summary>Razlike između dve verzije (po mailu i tovarnom listu): odluka, broj grešaka/upozorenja, nalazi, polja posla. Regresija: ranije „Tačno? = da“.</summary>
        public static List<Razlika> Uporedi(RadniProstor rp, string bila, string sada)
        {
            var a = Redovi(rp, bila).ToDictionary(r => r.Kljuc, StringComparer.Ordinal);
            var b = Redovi(rp, sada).ToDictionary(r => r.Kljuc, StringComparer.Ordinal);
            var ocene = Ocene(Putanja(rp, bila));
            var rez = new List<Razlika>();
            foreach (var k in a.Keys.Union(b.Keys).OrderBy(x => (a.TryGetValue(x, out var r1) ? r1 : b[x]).Rb))
            {
                a.TryGetValue(k, out var x); b.TryGetValue(k, out var y);
                var naslov = (x ?? y).Stavka.Naslov; var tl = (x ?? y).TovarniList;
                var tacno = ocene.TryGetValue(k, out var o) ? o.tacno : "";
                void Dodaj(string polje, string p, string q) { if ((p ?? "") != (q ?? "")) rez.Add(new Razlika { Kljuc = k, Naslov = naslov, TovarniList = tl, Polje = polje, Bilo = p ?? "", Sada = q ?? "", Tacno = tacno, Regresija = tacno.StartsWith("da", StringComparison.OrdinalIgnoreCase) }); }
                if (x == null) { Dodaj("red", "(nema)", "nov red"); continue; }
                if (y == null) { Dodaj("red", "postoji", "(nema u novoj verziji)"); continue; }
                Dodaj("Odluka", x.Odluka, y.Odluka);
                Dodaj("Grešaka", x.Gresaka.ToString(), y.Gresaka.ToString());
                Dodaj("Upozorenja", x.Upozorenja.ToString(), y.Upozorenja.ToString());
                var izgubljeni = x.Nalazi.Except(y.Nalazi).ToList(); var novi = y.Nalazi.Except(x.Nalazi).ToList();
                if (izgubljeni.Count + novi.Count > 0) Dodaj("Nalazi", string.Join("; ", izgubljeni), string.Join("; ", novi));
                foreach (var p in PoljaPosla) Dodaj(p, x.Posao.TryGetValue(p, out var v1) ? v1 : "", y.Posao.TryGetValue(p, out var v2) ? v2 : "");
                Dodaj("INTERNO", x.Interno, y.Interno);
            }
            return rez;
        }

        /// <summary>Poredjenje_&lt;bila&gt;_&lt;sada&gt;.xlsx i .html. Vraća putanju Excel-a.</summary>
        public static string SacuvajPoredjenje(RadniProstor rp, string bila, string sada)
        {
            var raz = Uporedi(rp, bila, sada);
            var ime = Path.Combine(rp.Koren, "Poredjenje_" + RadniProstor.BezbednoIme(bila, 20) + "_" + RadniProstor.BezbednoIme(sada, 20));
            var l = new XlsxList { Ime = "Razlike" };
            l.Zaglavlje.AddRange(new[] { "Regresija", "Naslov", "Tovarni list", "Polje", bila, sada, "Tačno? (" + bila + ")", "Ključ" });
            foreach (var r in raz)
            {
                l.Redovi.Add(new object[] { r.Regresija ? "REGRESIJA" : "", r.Naslov, r.TovarniList, r.Polje, r.Bilo, r.Sada, r.Tacno, r.Kljuc });
                if (r.Regresija) l.BojaReda[l.Redovi.Count - 1] = "FFF8D7DA";
            }
            l.Sirine[1] = 40; l.Sirine[4] = 45; l.Sirine[5] = 45; l.Prelom.Add(4); l.Prelom.Add(5);
            Xlsx.Sacuvaj(ime + ".xlsx", l);
            string H(string s) => WebUtility.HtmlEncode(s ?? "");
            var sb = new StringBuilder("<!doctype html><meta charset=\"utf-8\"><title>Poređenje " + H(bila) + " → " + H(sada) + "</title><body style=\"font-family:Segoe UI,Arial;font-size:13px\">");
            sb.Append("<h2>Poređenje " + H(bila) + " → " + H(sada) + "</h2><p>Razlika: " + raz.Count + ", regresija (ranije „Tačno? = da“): <b>" + raz.Count(x => x.Regresija) + "</b>.</p>");
            sb.Append("<table border=\"1\" cellspacing=\"0\" cellpadding=\"4\" style=\"border-collapse:collapse;font-size:12px\"><tr><th></th><th>Naslov</th><th>Tovarni list</th><th>Polje</th><th>" + H(bila) + "</th><th>" + H(sada) + "</th></tr>");
            foreach (var r in raz) sb.Append("<tr" + (r.Regresija ? " style=\"background:#f8d7da\"" : "") + "><td>" + (r.Regresija ? "REGRESIJA" : "") + "</td><td>" + H(r.Naslov) + "</td><td>" + H(r.TovarniList) + "</td><td>" + H(r.Polje) + "</td><td>" + H(r.Bilo) + "</td><td>" + H(r.Sada) + "</td></tr>");
            File.WriteAllText(ime + ".html", sb.Append("</table></body>").ToString(), new UTF8Encoding(false));
            return ime + ".xlsx";
        }

        // ------------------------------------------------------------------------------------------- paket (2.9)

        private static readonly Regex Tajna = new Regex(@"(?i)(password\s*=|pwd\s*=|user id\s*=|integrated security\s*=|data source\s*=|initial catalog\s*=|\.dpapi\b|lozinka\s*[:=])", RegexOptions.Compiled);

        /// <summary>
        /// Paket za analizu (zip): Zbirni Excel, poređenje (ako postoji), za svaki izabran mail .eml + folder rezultata + _Analiza (ako postoji),
        /// MANIFEST.txt. Bez tajni: preskaču se *.dpapi, *.config i svaki tekstualni fajl u kome se pojavi oblik lozinke ili connection string-a.
        /// <paramref name="samoNeDelimicno"/> – samo redovi sa „Tačno?“ = ne / delimično.
        /// </summary>
        public static string Paket(RadniProstor rp, string verzija, string izlaz, bool samoNeDelimicno, out List<string> preskoceno)
        {
            preskoceno = new List<string>();
            var redovi = Redovi(rp, verzija);
            var ocene = Ocene(Putanja(rp, verzija));
            var izabrane = redovi.Where(r => !samoNeDelimicno || (ocene.TryGetValue(r.Kljuc, out var o) && Regex.IsMatch(o.tacno ?? "", "^(ne|del)", RegexOptions.IgnoreCase)))
                .Select(r => r.Stavka).Distinct().ToList();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(izlaz)));
            if (File.Exists(izlaz)) File.Delete(izlaz);
            var manifest = new StringBuilder();
            manifest.AppendLine("CimRadionica – paket za analizu").AppendLine("Radni prostor: " + rp.Ime + ", verzija " + verzija + ", napravljen " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            manifest.AppendLine("Izbor: " + (samoNeDelimicno ? "samo „Tačno?“ = ne / delimično" : "svi obrađeni mailovi") + "; poruka: " + izabrane.Count).AppendLine("Bez lozinki, DPAPI fajlova i connection string-a.").AppendLine();
            var lokalno = preskoceno;
            using (var z = ZipFile.Open(izlaz, ZipArchiveMode.Create))
            {
                void Dodaj(string fajl, string ime)
                {
                    var n = Path.GetFileName(fajl);
                    if (Regex.IsMatch(n, @"(?i)\.(dpapi|config)$")) { lokalno.Add(ime + " (tajna)"); return; }
                    if (Regex.IsMatch(n, @"(?i)\.(txt|json|jsonl|md|html|csv|log)$") && Tajna.IsMatch(File.ReadAllText(fajl))) { lokalno.Add(ime + " (sadrži oblik lozinke/connection string-a)"); return; }
                    z.CreateEntryFromFile(fajl, ime.Replace('\\', '/'), CompressionLevel.Optimal);
                }
                var zb = Putanja(rp, verzija);
                if (File.Exists(zb)) Dodaj(zb, Path.GetFileName(zb));
                foreach (var p in Directory.GetFiles(rp.Koren, "Poredjenje_*")) Dodaj(p, Path.GetFileName(p));
                foreach (var s in izabrane)
                {
                    var rb = rp.Rb(s).ToString("000");
                    manifest.AppendLine(rb + "  " + s.Datum.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + "  " + s.Od + "  " + s.Naslov);
                    var eml = Path.Combine(rp.Poruke, s.Fajl);
                    if (File.Exists(eml)) Dodaj(eml, "poruke/" + rb + "_" + s.Fajl);
                    if (s.Rezultati.TryGetValue(verzija, out var rel) && Directory.Exists(Path.Combine(rp.Koren, rel)))
                        foreach (var f in Directory.GetFiles(Path.Combine(rp.Koren, rel), "*", SearchOption.AllDirectories)) Dodaj(f, "rezultati/" + rb + "/" + f.Substring(Path.Combine(rp.Koren, rel).Length).TrimStart('\\'));
                    var an = Directory.Exists(Path.Combine(rp.Koren, "_Analiza", RadniProstor.BezbednoIme(verzija, 30)))
                        ? Directory.GetDirectories(Path.Combine(rp.Koren, "_Analiza", RadniProstor.BezbednoIme(verzija, 30)), rb + "_*").FirstOrDefault() : null;
                    if (an != null) foreach (var f in Directory.GetFiles(an, "*", SearchOption.AllDirectories)) Dodaj(f, "_Analiza/" + Path.GetFileName(an) + "/" + f.Substring(an.Length).TrimStart('\\'));
                }
                if (lokalno.Count > 0) manifest.AppendLine().AppendLine("Izostavljeno:").AppendLine(string.Join(Environment.NewLine, lokalno));
                var e = z.CreateEntry("MANIFEST.txt");
                using (var w = new StreamWriter(e.Open(), new UTF8Encoding(false))) w.Write(manifest.ToString());
            }
            return izlaz;
        }

        // ------------------------------------------------------------------------------------------- paket za Claude (odluka 18.11.)

        /// <summary>Slika strane za paket: siva skala, rezolucija <paramref name="dpi"/> (izvor je <see cref="Analiza.DpiStrana"/>), JPEG kvaliteta 75.</summary>
        internal static byte[] SlikaSivo(string fajl, int dpi = 80)
        {
            using (var src = new System.Drawing.Bitmap(fajl))
            {
                var f = Math.Min(1.0, dpi / (double)Analiza.DpiStrana);
                int w = Math.Max(1, (int)Math.Round(src.Width * f)), h = Math.Max(1, (int)Math.Round(src.Height * f));
                using (var dst = new System.Drawing.Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                {
                    using (var g = System.Drawing.Graphics.FromImage(dst))
                    using (var ia = new System.Drawing.Imaging.ImageAttributes())
                    {
                        ia.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(new[]
                        {
                            new float[] { 0.299f, 0.299f, 0.299f, 0, 0 }, new float[] { 0.587f, 0.587f, 0.587f, 0, 0 }, new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
                            new float[] { 0, 0, 0, 1, 0 }, new float[] { 0, 0, 0, 0, 1 },
                        }));
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(src, new System.Drawing.Rectangle(0, 0, w, h), 0, 0, src.Width, src.Height, System.Drawing.GraphicsUnit.Pixel, ia);
                    }
                    var enc = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders().First(x => x.MimeType == "image/jpeg");
                    using (var ep = new System.Drawing.Imaging.EncoderParameters(1))
                    using (var ms = new MemoryStream())
                    {
                        ep.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 75L);
                        dst.Save(ms, enc, ep);
                        return ms.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// v3.0 (odluka 18.11.): paket za analizu u Claude-u – bez originalnih mailova i bez rezultati\: samo _Analiza\&lt;verzija&gt;
        /// (slike strana u sivoj skali, 80 dpi), Zbirno, Poređenje i dodatni fajlovi (Rečnik, PROCITAJ). Delovi ≤ <paramref name="maxBajtova"/>;
        /// mail (folder NNN_…) se nikad ne deli između delova. Bez tajni (isti filter kao <see cref="Paket"/>). Vraća putanje delova.
        /// </summary>
        /// <param name="analizaKoren">v3.0.5: koren u kom je _Analiza (Testiranje: folder Testiranje); null = radni prostor.</param>
        /// <param name="osnovaImena">v3.0.5: ime paketa bez .zip (npr. „Claude_Testiranje_2026-11-20“); null = Claude_&lt;prostor&gt;_&lt;verzija&gt;.</param>
        /// <param name="ukljuciMail">v3.0.5: samo folderi mailova za koje vraća true (npr. mailovi obrađeni u ovoj obradi); null = svi.</param>
        public static List<string> PaketZaClaude(RadniProstor rp, string verzija, string izlazFolder, IEnumerable<string> dodatni, out List<string> preskoceno, long maxBajtova = 100L * 1024 * 1024,
            string analizaKoren = null, string osnovaImena = null, Func<string, bool> ukljuciMail = null)
        {
            var lokalno = preskoceno = new List<string>();
            var baza = Path.Combine(analizaKoren ?? rp.Koren, "_Analiza", RadniProstor.BezbednoIme(verzija, 30));
            if (!Directory.Exists(baza)) throw new DirectoryNotFoundException("Nema " + baza + " – obradite sa „napravi _Analiza“.");
            Directory.CreateDirectory(izlazFolder);
            // stavke: (ime u zip-u, bajtovi, grupa) – grupa je folder maila; zajednički fajlovi su grupa „“ (prvi deo)
            byte[] Bezbedno(string fajl, string ime)
            {
                var n = Path.GetFileName(fajl);
                if (Regex.IsMatch(n, @"(?i)\.(dpapi|config)$")) { lokalno.Add(ime + " (tajna)"); return null; }
                if (Regex.IsMatch(n, @"(?i)\.(jpe?g|png)$") && fajl.Replace('/', '\\').Contains("\\strane\\")) return SlikaSivo(fajl);
                var b = File.ReadAllBytes(fajl);
                if (Regex.IsMatch(n, @"(?i)\.(txt|json|jsonl|md|html|csv|log)$") && Tajna.IsMatch(Encoding.UTF8.GetString(b))) { lokalno.Add(ime + " (sadrži oblik lozinke/connection string-a)"); return null; }
                return b;
            }
            var zajednicki = new List<(string ime, byte[] b)>();
            void Zaj(string fajl) { if (File.Exists(fajl) && Bezbedno(fajl, Path.GetFileName(fajl)) is byte[] b) zajednicki.Add((Path.GetFileName(fajl), b)); }
            Zaj(Putanja(rp, verzija));
            foreach (var p in Directory.GetFiles(rp.Koren, "Poredjenje_*")) Zaj(p);
            foreach (var d in dodatni ?? Enumerable.Empty<string>()) Zaj(d);
            foreach (var f in Directory.GetFiles(baza)) if (Bezbedno(f, "_Analiza/" + Path.GetFileName(f)) is byte[] b) zajednicki.Add(("_Analiza/" + verzija + "/" + Path.GetFileName(f), b));
            var mailovi = new List<(string folder, List<(string ime, byte[] b)> fajlovi)>();
            foreach (var dir in Directory.GetDirectories(baza).OrderBy(x => x, StringComparer.Ordinal))
            {
                var ime = Path.GetFileName(dir);
                if (ukljuciMail != null && !ukljuciMail(ime)) continue;
                var l = new List<(string, byte[])>();
                foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                {
                    var rel = "_Analiza/" + verzija + "/" + ime + "/" + f.Substring(dir.Length).TrimStart('\\').Replace('\\', '/');
                    if (Bezbedno(f, rel) is byte[] b) l.Add((rel, b));
                }
                mailovi.Add((ime, l));
            }
            // procena veličine u zip-u: slike/PDF ≈ ista veličina, tekst ≈ trećina
            long Procena(IEnumerable<(string ime, byte[] b)> x) => x.Sum(y => Regex.IsMatch(y.ime, @"(?i)\.(jpe?g|png|pdf|xlsx)$") ? y.b.LongLength : y.b.LongLength / 3 + 200);
            var delovi = new List<List<(string ime, byte[] b)>> { new List<(string, byte[])>(zajednicki) };
            long tekuci = Procena(zajednicki);
            foreach (var (folder, fajlovi) in mailovi)
            {
                var vel = Procena(fajlovi);
                if (tekuci + vel > maxBajtova * 0.95 && delovi.Last().Count > zajednicki.Count) { delovi.Add(new List<(string, byte[])>()); tekuci = 0; }
                delovi.Last().AddRange(fajlovi); tekuci += vel;
            }
            var osnova = osnovaImena ?? "Claude_" + RadniProstor.BezbednoIme(rp.Ime, 30) + "_" + RadniProstor.BezbednoIme(verzija, 30);
            var putanje = delovi.Select((d, i) => Path.Combine(izlazFolder, osnova + (delovi.Count > 1 ? "_deo" + (i + 1) + "od" + delovi.Count : "") + ".zip")).ToList();
            for (int i = 0; i < delovi.Count; i++)
            {
                if (File.Exists(putanje[i])) File.Delete(putanje[i]);
                using (var z = ZipFile.Open(putanje[i], ZipArchiveMode.Create))
                {
                    foreach (var (ime, b) in delovi[i])
                    {
                        var e = z.CreateEntry(ime, Regex.IsMatch(ime, @"(?i)\.(jpe?g|png|pdf|xlsx)$") ? CompressionLevel.NoCompression : CompressionLevel.Optimal);
                        using (var s = e.Open()) s.Write(b, 0, b.Length);
                    }
                    var m = new StringBuilder();
                    m.AppendLine("CimRadionica – paket za analizu (Claude)").AppendLine("Radni prostor: " + rp.Ime + ", verzija " + verzija + ", napravljen " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                    m.AppendLine("Deo " + (i + 1) + " od " + delovi.Count + ": " + string.Join(", ", putanje.Select(Path.GetFileName)));
                    m.AppendLine("Sadržaj: samo _Analiza (slike strana u sivoj skali, 80 dpi), Zbirno, Poređenje, Rečnik, PROCITAJ – bez originalnih mailova i bez rezultati\\.");
                    m.AppendLine("Mailova u ovom delu: " + delovi[i].Select(x => x.ime.Split('/')).Where(x => x.Length > 3).Select(x => x[2]).Distinct().Count() + ". Bez lozinki, DPAPI fajlova i connection string-a.");
                    if (lokalno.Count > 0) m.AppendLine().AppendLine("Izostavljeno:").AppendLine(string.Join(Environment.NewLine, lokalno));
                    using (var w = new StreamWriter(z.CreateEntry("MANIFEST.txt").Open(), new UTF8Encoding(false))) w.Write(m.ToString());
                }
            }
            return putanje;
        }
    }
}
