using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera.Radionica
{
    /// <summary>
    /// v3.0 (2.12): folder _Analiza\&lt;verzija&gt;\&lt;NNN&gt;_&lt;datum&gt;_&lt;naslov&gt;\ – ANALIZA.md (zaglavlje, tekst maila – novi deo i događaji,
    /// prilozi po stranama, sve rubrike sa sirovim pročitanim tekstom, spisak i zbirovi, odluka i pun tekst odgovora, upis, nesigurnosti,
    /// beleške korisnika), PRIMER, strane\ (JPG 100 dpi: CIM/CUV/CIT/spisak), rezultat.json; plus INDEKS.md, SAZETAK.md, rezultati.jsonl.
    /// Bez lozinki, DPAPI fajlova i connection string-a.
    /// </summary>
    internal static class Analiza
    {
        public const int DpiStrana = 100;

        public static string Folder(string koren, string verzija, int rb, StavkaProstora s) =>
            Path.Combine(koren, "_Analiza", RadniProstor.BezbednoIme(verzija, 30),
                rb.ToString("000") + "_" + s.Datum.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture) + "_" + RadniProstor.BezbednoIme(s.Naslov, 60));

        /// <summary>Analiza jedne poruke (iz rezultata obrade). Vraća folder.</summary>
        public static string Napisi(string koren, string verzija, int rb, RezultatAlata ra, string prethodna = null, string sledeca = null)
        {
            var s = ra.Stavka;
            var f = Folder(koren, verzija, rb, s);
            Directory.CreateDirectory(f);
            foreach (var x in Directory.GetFiles(f)) File.Delete(x);
            var strane = Path.Combine(f, "strane");
            if (Directory.Exists(strane)) Directory.Delete(strane, true);
            File.WriteAllText(Path.Combine(f, "rezultat.json"), RezultatJson.U(ra.Json), new UTF8Encoding(false));
            if (ra.Obrada.Primer?.Napravljen == true) File.WriteAllBytes(Path.Combine(f, ra.Obrada.Primer.Ime), ra.Obrada.Primer.Pdf);
            var slike = Strane(ra.Obrada, strane);
            File.WriteAllText(Path.Combine(f, "ANALIZA.md"), Tekst(rb, ra, slike, prethodna, sledeca), new UTF8Encoding(false));
            return f;
        }

        /// <summary>v3.0: analiza maila koji nije obrađen (greška okruženja) – da se vidi u INDEKS.md i SAZETAK.md. Vraća folder.</summary>
        public static string NapisiNeuspeh(string koren, string verzija, int rb, StavkaProstora s, string folderRezultata)
        {
            var f = Folder(koren, verzija, rb, s);
            Directory.CreateDirectory(f);
            foreach (var x in Directory.GetFiles(f)) File.Delete(x);
            var strane = Path.Combine(f, "strane");
            if (Directory.Exists(strane)) Directory.Delete(strane, true);
            var json = File.ReadAllText(Path.Combine(folderRezultata, "rezultat.json"), Encoding.UTF8);
            File.WriteAllText(Path.Combine(f, "rezultat.json"), json, new UTF8Encoding(false));
            var j = JObject.Parse(json);
            var sb = new StringBuilder();
            sb.AppendLine("# " + rb.ToString("000") + " – " + s.Naslov).AppendLine();
            sb.AppendLine("| | |").AppendLine("|---|---|");
            sb.AppendLine("| Datum | " + s.Datum.ToString("dd.MM.yyyy HH:mm zzz", CultureInfo.InvariantCulture) + " |");
            sb.AppendLine("| Od | " + s.Od + " |");
            sb.AppendLine("| Message-ID | " + s.MessageId + " |");
            sb.AppendLine("| Odluka | **" + (string)j["odluka"] + "** (" + RadniProstor.StatusNeuspeh + ") |").AppendLine();
            sb.AppendLine("## Mail nije obrađen").AppendLine();
            sb.AppendLine("Obrada nije izvršena zbog greške okruženja, ne zbog sadržaja maila. Nema nalaza, odgovora ni upisa.").AppendLine();
            sb.AppendLine("```").AppendLine((string)j["greska"] ?? "").AppendLine("```").AppendLine();
            sb.AppendLine("Ponovna obrada: CimRadionica → **Obradi ponovo neuspele**, ili `CimRadionica.exe --obradi <radni prostor> --verzija " + verzija + " --neuspele`.");
            File.WriteAllText(Path.Combine(f, "ANALIZA.md"), sb.ToString(), new UTF8Encoding(false));
            return f;
        }

        /// <summary>JPG strane tovarnih listova, CIT dokumenata i spiskova kola (100 dpi). Vraća (fajl, strana, tip, ime slike).</summary>
        private static List<(string fajl, int strana, string tip, string slika)> Strane(RezultatObrade r, string folder)
        {
            var rez = new List<(string, int, string, string)>();
            var dok = r.Provera?.Dokumenti ?? new List<Dokument>();
            var vazni = dok.Where(d => !d.IzDosijea && (TipDokumenta.JeTovarniList(d.Tip) || d.Tip == TipDokumenta.SpisakKola || d.Tip == TipDokumenta.Cit7 || d.Tip == TipDokumenta.Poledjina)).ToList();
            if (vazni.Count == 0) return rez;
            var svi = new List<Prilog>();
            foreach (var (ime, b) in r.Prilozi) try { svi.AddRange(Raspakivanje.Raspakuj(ime, b, new List<string>())); } catch { }
            Directory.CreateDirectory(folder);
            foreach (var d in vazni)
            {
                var p = svi.FirstOrDefault(x => string.Equals(x.Naziv, d.Fajl, StringComparison.OrdinalIgnoreCase))
                        ?? svi.FirstOrDefault(x => string.Equals(Path.GetFileName(x.Naziv.Split('/').Last()), Path.GetFileName((d.Fajl ?? "").Split('/').Last()), StringComparison.OrdinalIgnoreCase));
                if (p == null || p.Sadrzaj == null || (p.Vrsta != VrstaPriloga.Pdf && p.Vrsta != VrstaPriloga.Slika)) continue;
                foreach (var br in d.Strane.DefaultIfEmpty(1).Distinct().Take(12))
                {
                    var ime = RadniProstor.BezbednoIme(Path.GetFileNameWithoutExtension(d.Fajl.Split('/').Last()), 40) + "_str" + br + ".jpg";
                    if (rez.Any(x => x.Item4 == ime)) continue;
                    try
                    {
                        var png = p.Vrsta == VrstaPriloga.Pdf ? PdfCrtac.StranaUPng(p.Sadrzaj, br, DpiStrana) : PdfCrtac.SlikaOkrenuta(p.Sadrzaj);
                        using (var ms = new MemoryStream(png))
                        using (var img = Image.FromStream(ms))
                        using (var bmp = Smanji(img, p.Vrsta == VrstaPriloga.Slika ? 1240 : int.MaxValue))
                        {
                            var enc = ImageCodecInfo.GetImageEncoders().First(e => e.FormatID == ImageFormat.Jpeg.Guid);
                            using (var ep = new EncoderParameters(1)) { ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 60L); bmp.Save(Path.Combine(folder, ime), enc, ep); }
                        }
                        rez.Add((d.Fajl, br, d.Tip, ime));
                    }
                    catch { }
                }
            }
            return rez;
        }

        private static Bitmap Smanji(Image img, int maxSirina)
        {
            var w = Math.Min(img.Width, maxSirina); var h = (int)Math.Round(img.Height * (w / (double)img.Width));
            var b = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(b)) { g.Clear(Color.White); g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic; g.DrawImage(img, 0, 0, w, h); }
            return b;
        }

        private static string M(string s) => (s ?? "").Replace("|", "\\|").Replace("\r", "").Replace("\n", " ⏎ ").Trim();

        internal static string Tekst(int rb, RezultatAlata ra, List<(string fajl, int strana, string tip, string slika)> slike, string prethodna, string sledeca)
        {
            var s = ra.Stavka; var j = ra.Json; var r = ra.Obrada;
            var sb = new StringBuilder();
            sb.AppendLine("# " + rb.ToString("000") + " – " + s.Naslov).AppendLine();
            sb.AppendLine("| | |").AppendLine("|---|---|");
            sb.AppendLine("| Datum | " + s.Datum.ToString("dd.MM.yyyy HH:mm zzz", CultureInfo.InvariantCulture) + " |");
            sb.AppendLine("| Od | " + M(s.Od) + " |");
            sb.AppendLine("| Za / Cc | " + M(string.Join(", ", (j["poruka"]?["za"] as JArray ?? new JArray()).Select(x => (string)x))) + (((j["poruka"]?["cc"] as JArray)?.Count ?? 0) > 0 ? " / " + M(string.Join(", ", ((JArray)j["poruka"]["cc"]).Select(x => (string)x))) : "") + " |");
            sb.AppendLine("| Message-ID | " + M(s.MessageId) + " |");
            sb.AppendLine("| Izvor | " + M(s.Izvor) + " – " + M(Path.GetFileName(s.Putanja.Split('!')[0])) + " |");
            if (prethodna != null || sledeca != null) sb.AppendLine("| Nit | " + (prethodna != null ? "prethodna: " + M(prethodna) : "") + (sledeca != null ? (prethodna != null ? "; " : "") + "sledeća: " + M(sledeca) : "") + " |");
            sb.AppendLine("| Verzija | program " + j["verzija"]?["program"] + ", pravila " + j["verzija"]?["pravila"] + ", čitač " + j["verzija"]?["citac"] + ", " + j["rezim"] + " |");
            sb.AppendLine("| Trajanje | " + string.Join(", ", (j["trajanje"] as JObject ?? new JObject()).Properties().Select(p => p.Name + " " + p.Value + " s")) + " |");
            sb.AppendLine("| Odluka | **" + M((string)j["odluka"]) + "** (" + M(r.Status) + ") |").AppendLine();

            // tekst maila – samo novi deo
            sb.AppendLine("## Tekst maila (novi deo, bez citata i potpisa)").AppendLine();
            sb.AppendLine("```").AppendLine(NoviDeo(ra).Trim()).AppendLine("```").AppendLine();
            var dog = j["dogadjaji"] as JArray ?? new JArray();
            sb.AppendLine("### Događaji (" + dog.Count + ")").AppendLine();
            if (dog.Count == 0) sb.AppendLine("_nema_").AppendLine();
            else
            {
                sb.AppendLine("| Tip | Vreme | Zona | Mesto | Vrednost | Jezik | Pouzd. | Rečenica |").AppendLine("|---|---|---|---|---|---|---|---|");
                foreach (var d in dog) sb.AppendLine("| " + d["tip"] + " | " + M((string)d["vreme_tekst"]) + (string.IsNullOrEmpty((string)d["vreme"]) ? "" : " → " + d["vreme"]) + " | " + d["zona"] + " | " + M((string)d["mesto"]) + " | " + M((string)d["vrednost"]) + " | " + d["jezik"] + " | " + d["pouzdanost"] + " | " + M((string)d["recenica"]) + " |");
                sb.AppendLine();
            }

            // prilozi po stranama
            sb.AppendLine("## Prilozi i strane").AppendLine();
            var kl = j["klasifikacija"] as JObject;
            if (kl != null)
            {
                foreach (var p in (JArray)kl["prilozi"]) sb.AppendLine("- " + M((string)p));
                foreach (var p in (JArray)kl["preskoceno"]) sb.AppendLine("- preskočeno: " + M((string)p));
                foreach (var p in (JArray)kl["nisu_procitani"]) sb.AppendLine("- **nije pročitano**: " + M((string)p));
                sb.AppendLine();
                sb.AppendLine("| Fajl | Strane | Tip | Opis | Izvor |").AppendLine("|---|---|---|---|---|");
                foreach (var d in (JArray)kl["dokumenti"]) sb.AppendLine("| " + M(Path.GetFileName(((string)d["fajl"] ?? "").Split('/').Last())) + " | " + string.Join(",", ((JArray)d["strane"]).Select(x => (int)x)) + " | " + d["tip"] + " | " + M((string)d["opis"]) + " | " + d["izvor"] + ((bool?)d["iz_dosijea"] == true ? " (dosije)" : "") + " |");
                sb.AppendLine();
            }
            else sb.AppendLine("_" + M(string.Join(", ", s.Prilozi)) + "_").AppendLine();
            if (slike.Count > 0)
            {
                sb.AppendLine("Slike strana (100 dpi):").AppendLine();
                foreach (var x in slike) sb.AppendLine("- [" + x.slika + "](strane/" + Uri.EscapeDataString(x.slika) + ") – " + x.tip + ", str. " + x.strana);
                sb.AppendLine();
            }

            // tovarni listovi: sve rubrike sa sirovim tekstom
            var listovi = j["provera"]?["listovi"] as JArray ?? new JArray();
            int n = 0;
            foreach (JObject l in listovi)
            {
                n++;
                sb.AppendLine("## Tovarni list " + n + ": " + M((string)l["r62"]) + " (" + M(Path.GetFileName(((string)l["fajl"] ?? "").Split('/').Last())) + ", str. " + string.Join(",", ((JArray)l["strane"]).Select(x => (int)x)) + ")").AppendLine();
                sb.AppendLine("Obrazac: " + M((string)l["obrazac"]) + (string.IsNullOrEmpty((string)l["sablon"]) ? "" : " / šablon " + M((string)l["sablon"])) + ", pouzdanost strane " + l["pouzdanost_strane"]
                    + ((bool?)l["kp_rezim"] == true ? ", **KP tovarni list (interno)**" : "") + ((bool?)l["rucna_provera"] == true ? ", **ručna provera**" : "")
                    + (string.IsNullOrEmpty((string)l["neprepoznat"]) ? "" : ", **obrazac nije prepoznat**: " + M((string)l["neprepoznat"]))).AppendLine();
                // v3.0 (e): nesigurne cifre r62 (rukom pisano / nizak OCR) – nisu siguran ključ
                if (l["r62_nesigurno"] is JArray nes62 && nes62.Count > 0)
                    sb.AppendLine("**r62 – nesigurne cifre:** " + string.Join("; ", nes62.Select(x => (string)x["deo"] + " „" + M((string)x["vrednost"]) + "“ (OCR " + ((double)x["pouzdanost"]).ToString("0.00", CultureInfo.GetCultureInfo("sr-Latn-RS")) + ")"))
                                 + ". r62 se ne koristi kao siguran ključ (ponavljanje, posao, KP).").AppendLine();
                else if ((bool?)l["r62_za_odluke"] == false)
                    sb.AppendLine("**r62 nije pouzdano pročitana** – ne koristi se kao siguran ključ (ponavljanje, posao, KP).").AppendLine();
                var nalazi = (JArray)l["nalazi"];
                sb.AppendLine("| Rubrika | Status | Pročitano (sirovo) | Pouzd. | Nalazi |").AppendLine("|---|---|---|---|---|");
                foreach (var p in (l["rubrike"] as JObject ?? new JObject()).Properties().OrderBy(p => Redosled(p.Name)))
                {
                    var rub = p.Name.Substring(1);
                    var naRub = nalazi.Where(x => string.Equals((string)x["rubrika"], rub, StringComparison.OrdinalIgnoreCase)).Select(x => TezinaKratko((string)x["tezina"]) + " " + x["pravilo"] + ((bool?)x["samo_interno"] == true ? " (interno)" : "")).ToList();
                    sb.AppendLine("| " + rub + " | " + p.Value["status"] + " | " + M((string)p.Value["tekst"]) + " | " + p.Value["pouzdanost"] + " | " + M(string.Join("; ", naRub)) + " |");
                }
                sb.AppendLine();
                sb.AppendLine("### Nalazi").AppendLine();
                if (nalazi.Count == 0) sb.AppendLine("_nema_");
                foreach (var x in nalazi)
                    sb.AppendLine("- **" + TezinaKratko((string)x["tezina"]) + "** " + x["pravilo"] + " r" + x["rubrika"] + ((bool?)x["samo_interno"] == true ? " _(interno)_" : "") + ": " + M((string)x["tekst"])
                        + (string.IsNullOrEmpty((string)x["primer"]) ? "" : " → primer: " + M((string)x["primer"])) + (string.IsNullOrEmpty((string)x["dokaz"]) ? "" : " · dokaz: „" + M((string)x["dokaz"]) + "“"));
                sb.AppendLine();
            }

            // spisak kola
            var spiskovi = (r.Provera?.Dokumenti ?? new List<Dokument>()).Where(d => d.Spisak.Count > 0 && d.Spisak[0].Kola.Count > 0).ToList();
            foreach (var d in spiskovi)
            {
                var sp = d.Spisak[0];
                string T(double? kg) => kg.HasValue ? (kg.Value / 1000).ToString("N3", CultureInfo.GetCultureInfo("sr-Latn-RS")) + " t" : "–";
                double? Z(Func<KolaNaSpisku, double?> f) => sp.Kola.Any(k => f(k).HasValue) ? sp.Kola.Sum(k => f(k) ?? 0) : (double?)null;
                sb.AppendLine("## Spisak kola: " + M(Path.GetFileName(d.Fajl.Split('/').Last())) + (d.IzDosijea ? " (iz dosijea)" : "")).AppendLine();
                sb.AppendLine("- kola: " + sp.Kola.Count + (sp.Kola.Any(k => k.Prazna) ? " (prazna: " + sp.Kola.Count(k => k.Prazna) + ")" : "") + (sp.BrojVoza.Length > 0 ? ", voz " + M(sp.BrojVoza) : "") + (sp.R62.Tekst?.Length > 0 ? ", r62 " + M(sp.R62.Tekst) : ""));
                sb.AppendLine("- zbir po kolima: tara " + T(Z(k => k.TaraKg)) + ", neto " + T(Z(k => k.NetoKg)) + ", bruto " + T(Z(k => k.BrutoKg))
                    + (sp.UkupnoNetoKg.HasValue || sp.UkupnoBrutoKg.HasValue || sp.UkupnoTaraKg.HasValue ? "; red „Ukupno“: tara " + T(sp.UkupnoTaraKg) + ", neto " + T(sp.UkupnoNetoKg) + ", bruto " + T(sp.UkupnoBrutoKg) : ""));
                var nhm = sp.Kola.Where(k => k.Nhm.Length > 0).GroupBy(k => k.Nhm.Length >= 4 ? k.Nhm.Substring(0, 4) : k.Nhm).Select(g => g.Key + " ×" + g.Count()).ToList();
                if (nhm.Count > 0 || sp.NhmZaglavlje.Length > 0) sb.AppendLine("- NHM: " + string.Join(", ", nhm) + (sp.NhmZaglavlje.Length > 0 ? " (zaglavlje " + sp.NhmZaglavlje + ")" : ""));
                var bezTare = sp.Kola.Count(k => !k.TaraKg.HasValue);
                if (bezTare > 0) sb.AppendLine("- **bez tare: " + bezTare + " kola**");
                var listR25 = r.Provera?.Listovi.Where(l => l.Cim != null).Select(l => l.Cim.R25?.Tekst).Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? new List<string>();
                if (listR25.Count > 0) sb.AppendLine("- r25 tovarnog lista: " + M(string.Join(" / ", listR25)));
                sb.AppendLine();
            }

            // odgovor
            sb.AppendLine("## Odgovor").AppendLine();
            sb.AppendLine("Odluka: **" + M((string)j["odluka"]) + "**. Odgovor je pripremljen kao u režimu Live i **nije poslat**.").AppendLine();
            foreach (var p in r.Poruke)
            {
                sb.AppendLine("### " + p.Vrsta + ": " + M(p.Naslov)).AppendLine();
                sb.AppendLine("- Za: " + M(string.Join(", ", p.Za)) + (p.Cc.Count > 0 ? "; Cc: " + M(string.Join(", ", p.Cc)) : "") + (string.IsNullOrEmpty(p.LivePrimaoci) ? "" : "; Live primaoci: " + M(p.LivePrimaoci)));
                if (p.Prilozi.Count > 0) sb.AppendLine("- Prilozi: " + M(string.Join(", ", p.Prilozi)));
                sb.AppendLine().AppendLine("```").AppendLine((p.Telo ?? "").Trim()).AppendLine("```").AppendLine();
            }
            if (r.Primer != null) sb.AppendLine("PRIMER: " + (r.Primer.Napravljen ? r.Primer.Ime + " (" + r.Primer.Strane.Count + " str.)" : "nije napravljen – " + r.Primer.Razlog)).AppendLine();

            // upis
            sb.AppendLine("## Upis posla").AppendLine();
            if (j["upis"] is JObject u)
            {
                sb.AppendLine("- " + M((string)u["ishod"]));
                foreach (var x in (JArray)u["interno"]) sb.AppendLine("- **INTERNO**: " + M((string)x));
                foreach (var x in (JArray)u["promene"]) sb.AppendLine("- izmena: " + M((string)x));
            }
            else if (j["poslovi"] is JArray pos)
            {
                sb.AppendLine("_offline – predlog iz lokalnih šifarnika (u tmp tabele nije upisano)_").AppendLine();
                foreach (JObject p in pos)
                {
                    sb.AppendLine("### " + M((string)p["list"]) + (string.IsNullOrEmpty((string)p["kljuc"]) ? "" : " – ključ " + M((string)p["kljuc"]))).AppendLine();
                    sb.AppendLine("| Kolona | Vrednost | Izvor | Pročitano | Pouzd. | Kandidati |").AppendLine("|---|---|---|---|---|---|");
                    foreach (var po in (JArray)p["poreklo"]) sb.AppendLine("| " + po["kolona"] + " | " + M((string)po["vrednost"]) + " | " + M((string)po["rubrika"]) + " | " + M((string)po["tekst"]) + " | " + po["pouzdanost"] + " | " + M((string)po["kandidati"]) + " |");
                    foreach (var x in (JArray)p["napomene"]) sb.AppendLine("- napomena: " + M((string)x));
                    sb.AppendLine();
                }
            }
            else sb.AppendLine("_upis nije rađen (" + j["rezim"] + ")_");
            sb.AppendLine();

            // nesigurnosti
            sb.AppendLine("## Nesigurnosti").AppendLine();
            var nes = new List<string>();
            foreach (JObject l in listovi)
            {
                if ((bool?)l["rucna_provera"] == true) nes.Add("tovarni list " + l["r62"] + ": ručna provera");
                foreach (var p in (l["rubrike"] as JObject ?? new JObject()).Properties().Where(p => (string)p.Value["status"] == StatusRubrike.Necitljivo)) nes.Add("r" + p.Name.Substring(1) + " nečitljiva");
                foreach (var x in ((JArray)l["nalazi"]).Where(x => Regex.IsMatch((string)x["tekst"] ?? "", "nepouzdan|nečitljiv|proveriti ručno|ručna provera", RegexOptions.IgnoreCase))) nes.Add(M((string)x["pravilo"] + ": " + (string)x["tekst"]));
            }
            foreach (var x in r.Primer?.Interno ?? new List<string>()) nes.Add(M(x));
            if (j["poslovi"] is JArray pos2) foreach (var po in pos2.SelectMany(p => (JArray)p["poreklo"]).Where(po => !string.IsNullOrEmpty((string)po["kandidati"]))) nes.Add(po["kolona"] + ": kandidati " + M((string)po["kandidati"]));
            foreach (var d in dog.Where(d => (int)d["pouzdanost"] < 70)) nes.Add("događaj " + d["tip"] + " niske pouzdanosti (" + d["pouzdanost"] + ")");
            if (nes.Count == 0) sb.AppendLine("_nema_");
            foreach (var x in nes.Distinct()) sb.AppendLine("- " + x);
            sb.AppendLine();

            // beleške korisnika
            sb.AppendLine("## Beleške korisnika").AppendLine();
            if (s.Beleske.Count == 0) sb.AppendLine("_nema_");
            foreach (var b in s.Beleske) sb.AppendLine("### " + M(b.Ime)).AppendLine().AppendLine(b.Tekst.Trim()).AppendLine();
            return sb.ToString();
        }

        /// <summary>Novi deo poruke (bez citata i potpisa) iz pripremljenog ulaza.</summary>
        private static string NoviDeo(RezultatAlata ra) => ra.Json["poruka"]?["telo_novo"] is JValue v ? (string)v ?? "" : "";

        private static string TezinaKratko(string t) => t == "error" ? "GREŠKA" : t == "warning" ? "upozorenje" : t == "recommendation" ? "preporuka" : t == "info" ? "info" : t;

        private static double Redosled(string imeRubrike)
        {
            var m = Regex.Match(imeRubrike, @"^r(\d+)([a-z]?)");
            return m.Success ? int.Parse(m.Groups[1].Value) + (m.Groups[2].Value.Length > 0 ? (m.Groups[2].Value[0] - 'a' + 1) / 10.0 : 0) : 999;
        }

        /// <summary>INDEKS.md, SAZETAK.md i rezultati.jsonl za skup analiza (iz rezultat.json svake analize).</summary>
        public static void Zbirno(string koren, string verzija, List<(int rb, RezultatAlata ra, string folder)> stavke) =>
            Zbirno(koren, verzija, stavke.Select(x => (x.rb, x.ra.Json, x.folder)).ToList());

        /// <summary>INDEKS.md, SAZETAK.md i rezultati.jsonl iz svih foldera _Analiza\&lt;verzija&gt;\NNN_… (rezultat.json).</summary>
        public static void ZbirnoIzFoldera(string koren, string verzija)
        {
            var baza = Path.Combine(koren, "_Analiza", RadniProstor.BezbednoIme(verzija, 30));
            if (!Directory.Exists(baza)) return;
            var l = new List<(int, JObject, string)>();
            foreach (var d in Directory.GetDirectories(baza))
            {
                var m = Regex.Match(Path.GetFileName(d), @"^(\d{3,})_");
                var f = Path.Combine(d, "rezultat.json");
                if (m.Success && File.Exists(f)) l.Add((int.Parse(m.Groups[1].Value), JObject.Parse(File.ReadAllText(f, Encoding.UTF8)), d));
            }
            Zbirno(koren, verzija, l);
        }

        public static void Zbirno(string koren, string verzija, List<(int rb, JObject json, string folder)> stavke)
        {
            var baza = Path.Combine(koren, "_Analiza", RadniProstor.BezbednoIme(verzija, 30));
            Directory.CreateDirectory(baza);
            var ind = new StringBuilder();
            ind.AppendLine("# Indeks – " + verzija).AppendLine();
            ind.AppendLine("| NNN | Datum | Od | Naslov | Odluka | Greške | Upoz. | Događaji | Folder |").AppendLine("|---|---|---|---|---|---|---|---|---|");
            using (var jl = new StreamWriter(Path.Combine(baza, "rezultati.jsonl"), false, new UTF8Encoding(false)))
                foreach (var (rb, j, folder) in stavke.OrderBy(x => x.rb))
                {
                    ind.AppendLine("| " + rb.ToString("000") + " | " + M((string)j["poruka"]?["datum"]) + " | " + M((string)j["poruka"]?["od"]) + " | " + M((string)j["poruka"]?["naslov"]) + " | " + M((string)j["odluka"])
                        + " | " + (j["provera"]?["gresaka"] ?? 0) + " | " + (j["provera"]?["upozorenja"] ?? 0) + " | " + ((j["dogadjaji"] as JArray)?.Count ?? 0) + " | [" + Path.GetFileName(folder) + "](" + Uri.EscapeDataString(Path.GetFileName(folder)) + "/ANALIZA.md) |");
                    jl.WriteLine(j.ToString(Newtonsoft.Json.Formatting.None));
                }
            File.WriteAllText(Path.Combine(baza, "INDEKS.md"), ind.ToString(), new UTF8Encoding(false));
            var sz = new StringBuilder();
            sz.AppendLine("# Sažetak – " + verzija).AppendLine();
            sz.AppendLine("- poruka: " + stavke.Count);
            foreach (var g in stavke.GroupBy(x => (string)x.json["odluka"]).OrderByDescending(g => g.Count())) sz.AppendLine("- " + M(g.Key) + ": " + g.Count());
            // v3.0: mailovi koji nisu obrađeni (greška okruženja) – uvek posebno, nikad tiho
            var neuspeli = stavke.Where(x => (string)x.json["status"] == RadniProstor.StatusNeuspeh).OrderBy(x => x.rb).ToList();
            sz.AppendLine().AppendLine("## Nisu obrađeni (greška okruženja): " + neuspeli.Count).AppendLine();
            if (neuspeli.Count == 0) sz.AppendLine("Nema.");
            else
            {
                sz.AppendLine("| NNN | Datum | Naslov | Razlog |").AppendLine("|---|---|---|---|");
                foreach (var (rb, j, _) in neuspeli) sz.AppendLine("| " + rb.ToString("000") + " | " + M((string)j["poruka"]?["datum"]) + " | " + M((string)j["poruka"]?["naslov"]) + " | " + M((string)j["odluka"]) + " |");
                sz.AppendLine().AppendLine("Ponovna obrada: **Obradi ponovo neuspele** ili `--neuspele`.");
            }
            var nalazi = stavke.SelectMany(x => (x.json["provera"]?["listovi"] as JArray ?? new JArray()).SelectMany(l => (JArray)l["nalazi"])).ToList();
            sz.AppendLine().AppendLine("## Najčešći nalazi").AppendLine().AppendLine("| Pravilo | Rubrika | Težina | Broj |").AppendLine("|---|---|---|---|");
            foreach (var g in nalazi.GroupBy(x => (string)x["pravilo"] + "|" + x["rubrika"] + "|" + x["tezina"]).OrderByDescending(g => g.Count()).Take(30))
            { var p = g.Key.Split('|'); sz.AppendLine("| " + p[0] + " | " + p[1] + " | " + TezinaKratko(p[2]) + " | " + g.Count() + " |"); }
            var dog = stavke.SelectMany(x => x.json["dogadjaji"] as JArray ?? new JArray()).ToList();
            sz.AppendLine().AppendLine("## Događaji po tipu").AppendLine();
            foreach (var g in dog.GroupBy(d => (string)d["tip"]).OrderByDescending(g => g.Count())) sz.AppendLine("- " + g.Key + ": " + g.Count());
            var trajanje = stavke.Select(x => (double?)x.json["trajanje"]?["ukupno"] ?? 0).ToList();
            if (trajanje.Count > 0) sz.AppendLine().AppendLine("Trajanje: ukupno " + trajanje.Sum().ToString("0", CultureInfo.InvariantCulture) + " s, prosečno " + trajanje.Average().ToString("0.0", CultureInfo.InvariantCulture) + " s po poruci.");
            File.WriteAllText(Path.Combine(baza, "SAZETAK.md"), sz.ToString(), new UTF8Encoding(false));
        }
    }
}
