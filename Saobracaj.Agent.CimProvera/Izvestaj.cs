using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>Sastavljanje poruke po response_template iz JSON-a: GREŠKE, UPOZORENJA, PREPORUKE (najviše 5), podnožje.</summary>
    internal static class Izvestaj
    {
        public const int MaxPreporuka = 5;

        public static string Referenca(RezultatProvere r)
        {
            var prvi = r.Listovi.FirstOrDefault();
            if (prvi == null) return "";
            var c = prvi.Cim;
            var r62 = string.Join(" ", new[] { c.R62.Zemlja, c.R62.Stanica, c.R62.Preduzece, c.R62.Broj }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var refx = r62.Length > 0 ? r62 : !string.IsNullOrWhiteSpace(c.R8.Tekst) ? c.R8.Tekst.Trim() : System.IO.Path.GetFileName(prvi.Dokument.Fajl);
            if (r.Listovi.Count > 1) refx += " (+" + (r.Listovi.Count - 1) + ")";
            return TekstUtilSkrati(refx, 60);
        }

        private static string TekstUtilSkrati(string s, int max) => s.Length <= max ? s : s.Substring(0, max).TrimEnd() + "…";

        /// <summary>
        /// Naslov maila agenta: "naslov primljenog maila – naziv fajla – N grešaka, M upozorenja" ili "… – ispravan"
        /// (više fajlova razdvojeno zarezom; bez rezultata provere samo naslov i fajl).
        /// </summary>
        public static string NaslovMaila(string naslovMaila, IEnumerable<string> fajlovi, bool en, RezultatProvere r = null)
        {
            var s = Regex.Replace(naslovMaila ?? "", @"\s+", " ").Trim();
            if (s.Length == 0) s = en ? "(no subject)" : "(bez naslova)";
            var f = string.Join(", ", (fajlovi ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase));
            var naslov = f.Length == 0 ? s : s + " – " + f;
            if (r == null || !r.ImaTovarniList) return naslov;
            if (r.BrojGresaka == 0 && r.BrojUpozorenja == 0) return naslov + " – " + (en ? "correct" : "ispravan");
            return naslov + " – " + Broj(r.BrojGresaka, en ? "error" : "greška", en ? "errors" : "greške", en ? "errors" : "grešaka", en)
                   + ", " + Broj(r.BrojUpozorenja, en ? "warning" : "upozorenje", en ? "warnings" : "upozorenja", en ? "warnings" : "upozorenja", en);
        }

        /// <summary>Srpska množina: 1 greška, 2–4 greške, 5+ grešaka (11–14 grešaka).</summary>
        private static string Broj(int n, string jedan, string dva, string pet, bool en)
        {
            if (en) return n + " " + (n == 1 ? jedan : pet);
            var d = n % 10; var s = n % 100;
            return n + " " + (d == 1 && s != 11 ? jedan : d >= 2 && d <= 4 && (s < 12 || s > 14) ? dva : pet);
        }

        /// <summary>
        /// Identifikacija pošiljke za prvi red tela: r62 ako je pouzdano pročitan (sve četiri grupe ispravnog oblika), inače
        /// r8 (npr. "Train no: 258F") i broj voza iz r15. Nepouzdano pročitan r62 se ne prikazuje.
        /// </summary>
        public static string Identifikacija(RezultatProvere r, bool en)
        {
            var delovi = new List<string>();
            foreach (var l in r.Listovi.Where(x => x.Cim != null))
            {
                var c = l.Cim; var i = c.R62;
                var pouzdan = i.Status == StatusRubrike.Popunjeno && i.Pouzdanost >= 0.6 && Regex.IsMatch(i.Zemlja ?? "", @"^\d{2}$") && Regex.IsMatch(Regex.Replace(i.Stanica ?? "", @"\D", ""), @"^\d{5,6}$")
                              && Regex.IsMatch(i.Preduzece ?? "", @"^\d{4}$") && Regex.IsMatch(Regex.Replace(i.Broj ?? "", @"[\s\-]", ""), @"^\d{5,6}$");
                string id;
                // "72 16550-6 3212 15690-0": stanica i broj otpravljanja sa kontrolnom cifrom posle crtice
                string SaKontrolnom(string x) { var c6 = Regex.Replace(x ?? "", @"\D", ""); return c6.Length == 6 ? c6.Substring(0, 5) + "-" + c6.Substring(5) : x; }
                if (pouzdan) id = string.Join(" ", i.Zemlja, SaKontrolnom(i.Stanica), i.Preduzece, SaKontrolnom(i.Broj));
                else
                {
                    var p = new List<string>();
                    if (c.R8.Popunjena && !string.IsNullOrWhiteSpace(c.R8.Tekst)) p.Add(Regex.Replace(c.R8.Tekst, @"\s+", " ").Trim());
                    var voz = Regex.Match(c.R15.Tekst ?? "", @"(?:voz|vlak|train|zug)\w*\s*(?:br\.?|no\.?|nr\.?)?\s*(\d{2,6}\w?)", RegexOptions.IgnoreCase);
                    if (voz.Success) p.Add((en ? "train " : "voz br. ") + voz.Groups[1].Value);
                    id = string.Join(", ", p);
                }
                if (id.Length > 0 && !delovi.Contains(id)) delovi.Add(id);
            }
            return delovi.Count == 0 ? "" : (en ? "Consignment: " : "Pošiljka: ") + string.Join("; ", delovi);
        }

        /// <summary>Fajlovi u kojima su pronađeni tovarni listovi.</summary>
        public static IEnumerable<string> FajloviListova(RezultatProvere r) =>
            r.Dokumenti.Where(d => d.Tip == TipDokumenta.Cim || d.Cim.Count > 0).Select(d => d.Fajl);

        public static string Naslov(RezultatProvere r, PravilaSkup p, bool en)
        {
            var sablon = p.Sablon("subject", en, "Tovarni list CIM {shipment_ref} - {status}");
            string status;
            if (r.BrojGresaka == 0 && r.BrojUpozorenja == 0) status = p.SablonStatus("ok", en, "ispravan");
            else status = p.SablonStatus("errors", en, "potrebne ispravke ({n_errors} gresaka, {n_warnings} upozorenja)")
                .Replace("{n_errors}", r.BrojGresaka.ToString()).Replace("{n_warnings}", r.BrojUpozorenja.ToString());
            return sablon.Replace("{shipment_ref}", Referenca(r)).Replace("{status}", status).Replace("  ", " ");
        }

        private sealed class Stavka
        {
            public Nalaz N;
            public List<string> Listovi = new List<string>();
        }

        /// <summary>Isti nalaz na više listova (npr. 14 listova TCDD) prikazuje se jednom, sa spiskom listova.</summary>
        private static List<Stavka> Grupisi(RezultatProvere r, string tezina)
        {
            var stavke = new List<Stavka>();
            foreach (var l in r.Listovi)
                foreach (var n in l.Nalazi.Where(x => x.Tezina == tezina))
                {
                    // preporuke se ne ponavljaju (isti tekst = ista preporuka, bez obzira na upisanu vrednost)
                    var s = stavke.FirstOrDefault(x => x.N.Pravilo == n.Pravilo && x.N.Tekst == n.Tekst && (x.N.Vrednost == n.Vrednost || tezina == Tezina.Preporuka));
                    if (s == null) stavke.Add(s = new Stavka { N = n });
                    if (!s.Listovi.Contains(l.Oznaka)) s.Listovi.Add(l.Oznaka);
                }
            return stavke.OrderBy(s => s.Listovi.FirstOrDefault() == null ? 0 : r.Listovi.FindIndex(l => l.Oznaka == s.Listovi[0]))
                .ThenBy(s => RubrikaRed(s.N.Rubrika)).ThenBy(s => s.N.Pravilo, StringComparer.Ordinal).ToList();
        }

        /// <summary>
        /// Naziv dokumenta za kolonu "Dokument": samo ime fajla (bez putanje u ZIP-u), uz broj lista kada PDF ima više listova.
        /// Isti nalaz na svim listovima jednog fajla: "fajl.pdf (svi listovi, 14)".
        /// </summary>
        private static string NazivDokumenta(RezultatProvere r, Stavka s, bool en)
        {
            var delovi = new List<string>();
            foreach (var g in s.Listovi.Select(o => (fajl: Fajl(o), list: List(o))).GroupBy(x => x.fajl))
            {
                var ukupnoUFajlu = r.Listovi.Count(l => Fajl(l.Oznaka) == g.Key);
                var brojevi = g.Where(x => x.list.Length > 0).Select(x => x.list).ToList();
                if (brojevi.Count == 0 || ukupnoUFajlu <= 1) delovi.Add(g.Key);
                else if (brojevi.Count == ukupnoUFajlu) delovi.Add(g.Key + (en ? " (all sheets, " : " (svi listovi, ") + ukupnoUFajlu + ")");
                else delovi.Add(g.Key + (en ? " (sheet " : " (list ") + string.Join(", ", brojevi) + ")");
            }
            return string.Join("; ", delovi);
        }

        private static string Fajl(string oznaka)
        {
            var f = Regex.Replace(oznaka ?? "", @"\s+–\s+list\s+\d+$", "");
            return f.Split('/', '\\').Last();
        }

        private static string List(string oznaka)
        {
            var m = Regex.Match(oznaka ?? "", @"–\s+list\s+(\d+)$");
            return m.Success ? m.Groups[1].Value : "";
        }

        /// <summary>Opis: prvo konkretan nalaz, pa opšte uputstvo iz pravila (bez ponovljenog "Rubrika N:").</summary>
        private static string Opis(Nalaz n)
        {
            var uputstvo = BezRubrike(string.IsNullOrEmpty(n.Uputstvo) ? n.Tekst : n.Uputstvo);
            return (string.IsNullOrEmpty(n.Detalj) ? uputstvo : n.Detalj + "  " + uputstvo) + (n.SamoInterno ? " (interno – ne šalje se pošiljaocu)" : "");
        }

        private static double RubrikaRed(string r)
        {
            var m = Regex.Match(r ?? "", @"^(\d+)([a-z]?)");
            return m.Success ? int.Parse(m.Groups[1].Value) + (m.Groups[2].Value.Length > 0 ? 0.5 : 0) : -1;
        }

        /// <summary>„1 tovarni list“, „2–4 tovarna lista“, „5+ tovarnih listova“ (21 list, 22 lista, 11–14 listova).</summary>
        internal static string BrojListova(int n, bool en)
        {
            if (en) return n + (n == 1 ? " consignment note" : " consignment notes");
            int d = n % 10, s = n % 100;
            if (d == 1 && s != 11) return n + " tovarni list";
            if (d >= 2 && d <= 4 && (s < 12 || s > 14)) return n + " tovarna lista";
            return n + " tovarnih listova";
        }

        private static int BrojDodatnih(RezultatProvere r) => (r.Dokumenti ?? new List<Dokument>()).Count(d => d.Tip == TipDokumenta.DodatniList);

        /// <summary>„1 tovarni list + dodatni list“, „8 tovarnih listova + 2 dodatna lista“.</summary>
        internal static string OpisListova(RezultatProvere r, bool en)
        {
            var dod = BrojDodatnih(r);
            var t = BrojListova(r.Listovi.Count, en);
            if (dod == 0) return t;
            if (en) return t + " + " + (dod == 1 ? "additional sheet" : dod + " additional sheets");
            int d = dod % 10, s = dod % 100;
            return t + " + " + (dod == 1 ? "dodatni list" : dod + (d >= 2 && d <= 4 && (s < 12 || s > 14) ? " dodatna lista" : d == 1 && s != 11 ? " dodatni list" : " dodatnih listova"));
        }

        /// <summary>Napomena o ručnoj proveri: obrazac nije prepoznat (list nije proveren) ili su neke rubrike nepouzdano pročitane.</summary>
        internal static IEnumerable<string> RucnaProvera(RezultatProvere r, bool en)
        {
            var neprep = r.Listovi.Where(l => l.ZaRucnuProveru && !string.IsNullOrEmpty(l.Cim?.NeprepoznatObrazac)).ToList();
            if (neprep.Count > 0)
                yield return en ? (neprep.Count == 1 ? "The consignment note was not checked because the form was not recognised; the document has been queued for manual review."
                                                     : neprep.Count + " consignment notes were not checked because the form was not recognised; the documents have been queued for manual review.")
                                : (neprep.Count == 1 ? "Tovarni list nije proveren jer obrazac nije prepoznat; dokument je stavljen na ručnu proveru."
                                                     : "Tovarni listovi nisu provereni jer obrazac nije prepoznat (" + BrojListova(neprep.Count, false) + "); dokumenti su stavljeni na ručnu proveru.");
            if (r.Listovi.Any(l => l.ZaRucnuProveru && string.IsNullOrEmpty(l.Cim?.NeprepoznatObrazac)))
                yield return en ? "Some boxes could not be read reliably; the document has also been queued for manual review."
                                : "Neke rubrike nisu pouzdano pročitane; dokument je stavljen i na ručnu proveru.";
        }

        public static string Telo(RezultatProvere r, PravilaSkup p, bool en, string uvod = null, IList<string> nisuProcitani = null)
        {
            var sb = new StringBuilder();
            var ident = Identifikacija(r, en);
            if (ident.Length > 0) sb.AppendLine(ident);
            if (!string.IsNullOrEmpty(uvod)) sb.AppendLine(uvod).AppendLine();
            else if (ident.Length > 0) sb.AppendLine();

            var listovi = r.Listovi.Select(l => l.Oznaka).Distinct().ToList();
            var dodatni = BrojDodatnih(r);
            sb.AppendLine((en ? "Checked: " : "Provereno: ") + (listovi.Count == 1 ? listovi[0] + (dodatni > 0 ? " – " + OpisListova(r, en) : "") : OpisListova(r, en)));
            sb.AppendLine();

            Sekcija(sb, r, Tezina.Greska, en ? "ERRORS" : "GREŠKE", en, int.MaxValue);
            Sekcija(sb, r, Tezina.Upozorenje, en ? "WARNINGS" : "UPOZORENJA", en, int.MaxValue);
            Sekcija(sb, r, Tezina.Preporuka, en ? "RECOMMENDATIONS" : "PREPORUKE", en, MaxPreporuka);

            if (r.BrojGresaka == 0 && r.BrojUpozorenja == 0)
                sb.AppendLine(en ? "No errors or warnings were found." : "Nisu pronađene greške ni upozorenja.").AppendLine();

            if (nisuProcitani != null && nisuProcitani.Count > 0)
            {
                sb.AppendLine(en ? "ATTACHMENTS NOT CHECKED (could not be read)" : "PRILOZI KOJI NISU PROVERENI (nisu mogli da se pročitaju)");
                foreach (var x in nisuProcitani) sb.AppendLine("- " + x);
                sb.AppendLine();
            }

            foreach (var t in RucnaProvera(r, en)) sb.AppendLine(t).AppendLine();

            sb.AppendLine("--");
            sb.AppendLine(p.Sablon("footer", en, "Izvor pravila: Prirucnik GLV-CIM (Spt 51.01), Prilog 2.") + " " + (en ? "Rules version " : "Verzija pravila ") + p.Verzija + ".");
            sb.AppendLine(en ? "This message was generated automatically by the CIM check agent of Kombinovani prevoz d.o.o."
                             : "Poruku je automatski sastavio agent za proveru CIM-a, Kombinovani prevoz d.o.o.");
            return sb.ToString();
        }

        private static void Sekcija(StringBuilder sb, RezultatProvere r, string tezina, string naslov, bool en, int max)
        {
            var stavke = Grupisi(r, tezina);
            if (stavke.Count == 0) return;
            sb.AppendLine(naslov);
            int i = 0;
            foreach (var s in stavke.Take(max))
            {
                i++;
                var rub = Regex.IsMatch(s.N.Rubrika ?? "", @"^\d") ? (en ? "Box " : "Rubrika ") + s.N.Rubrika + " – " : "";
                sb.AppendLine(i + ". [" + NazivDokumenta(r, s, en) + "] " + rub + Opis(s.N));
                if (s.N.Vrednost.Length > 0) sb.AppendLine("   " + (en ? "Entered: " : "Upisano: ") + s.N.Vrednost);
                if (s.N.Primer.Length > 0 && tezina != Tezina.Preporuka) sb.AppendLine("   " + (en ? "Example: " : "Primer: ") + s.N.Primer);
            }
            if (stavke.Count > max) sb.AppendLine("   (+" + (stavke.Count - max) + ")");
            sb.AppendLine();
        }

        // ------------------------------------------------------------------------------------------- HTML (tabela)

        private static string H(string s) => System.Net.WebUtility.HtmlEncode(s ?? "");

        /// <summary>Opis bez uvodnog "Rubrika 16:" (rubrika je posebna kolona).</summary>
        private static string BezRubrike(string t) => Regex.Replace(t ?? "", @"^\s*(Rubrika|Box)\s+\d+[a-z]?\s*(\([^)]*\))?\s*[:\-–]\s*", "", RegexOptions.IgnoreCase);

        private const string Stil = "font-family:Segoe UI,Arial,sans-serif;font-size:13px;color:#222";
        private const string Th = "style=\"background:#f0f0f0;border:1px solid #ccc;padding:5px 7px;text-align:left;vertical-align:top\"";
        private const string Td = "style=\"border:1px solid #ccc;padding:5px 7px;vertical-align:top\"";

        public static string Html(RezultatProvere r, PravilaSkup p, bool en, string uvod = null, IList<string> nisuProcitani = null)
        {
            var sb = new StringBuilder();
            sb.Append("<div style=\"" + Stil + "\">");
            var ident = Identifikacija(r, en);
            if (ident.Length > 0) sb.Append("<p style=\"font-size:14px\"><b>" + H(ident) + "</b></p>");
            if (!string.IsNullOrEmpty(uvod)) sb.Append("<p>" + H(uvod).Replace("\n", "<br>") + "</p>");
            var fajlovi = r.Listovi.Select(l => Fajl(l.Oznaka)).Distinct().ToList();
            sb.Append("<p><b>" + (en ? "Checked: " : "Provereno: ") + "</b>" + H(string.Join(", ", fajlovi))
                      + (r.Listovi.Count > fajlovi.Count || BrojDodatnih(r) > 0 ? " (" + OpisListova(r, en) + ")" : "") + "</p>");

            var greske = Grupisi(r, Tezina.Greska);
            if (greske.Count > 0)
            {
                sb.Append("<h3 style=\"color:#b00020;margin:14px 0 6px\">" + (en ? "ERRORS" : "GREŠKE") + " (" + greske.Count + ")</h3>");
                sb.Append("<table style=\"border-collapse:collapse;" + Stil + "\"><tr>");
                foreach (var k in en ? new[] { "#", "Document", "Box", "Entered value", "Example of correct entry", "Description of the error" }
                                     : new[] { "#", "Dokument", "Rubrika", "Upisana vrednost", "Primer ispravnog upisa", "Opis greške" })
                    sb.Append("<th " + Th + ">" + k + "</th>");
                sb.Append("</tr>");
                int i = 0;
                foreach (var s in greske)
                {
                    i++;
                    var opis = string.IsNullOrEmpty(s.N.Detalj)
                        ? H(BezRubrike(s.N.Uputstvo.Length > 0 ? s.N.Uputstvo : s.N.Tekst))
                        : "<b>" + H(s.N.Detalj) + "</b> " + H(BezRubrike(s.N.Uputstvo.Length > 0 ? s.N.Uputstvo : s.N.Tekst));
                    sb.Append("<tr><td " + Td + ">" + i + "</td><td " + Td + ">" + H(NazivDokumenta(r, s, en)) + "</td><td " + Td + "><b>" + H(s.N.Rubrika) + "</b></td>"
                        + "<td " + Td + ">" + (s.N.Vrednost.Length > 0 ? H(s.N.Vrednost) : "<i style=\"color:#999\">" + (en ? "(empty)" : "(prazno)") + "</i>") + "</td>"
                        + "<td style=\"border:1px solid #ccc;padding:5px 7px;vertical-align:top;color:#1b5e20\">" + H(s.N.Primer) + "</td>"
                        + "<td " + Td + ">" + opis + "</td></tr>");
                }
                sb.Append("</table>");
            }

            var upoz = Grupisi(r, Tezina.Upozorenje);
            if (upoz.Count > 0)
            {
                sb.Append("<h3 style=\"color:#b26a00;margin:16px 0 6px\">" + (en ? "WARNINGS" : "UPOZORENJA") + " (" + upoz.Count + ")</h3>");
                sb.Append("<table style=\"border-collapse:collapse;" + Stil + "\"><tr>");
                foreach (var k in en ? new[] { "Document", "Box", "Entered value", "Suggested entry", "Description" } : new[] { "Dokument", "Rubrika", "Upisana vrednost", "Predlog upisa", "Opis" })
                    sb.Append("<th " + Th + ">" + k + "</th>");
                sb.Append("</tr>");
                foreach (var s in upoz)
                    sb.Append("<tr><td " + Td + ">" + H(NazivDokumenta(r, s, en)) + "</td><td " + Td + "><b>" + H(s.N.Rubrika) + "</b></td><td " + Td + ">" + H(s.N.Vrednost)
                              + "</td><td style=\"border:1px solid #ccc;padding:5px 7px;vertical-align:top;color:#1b5e20\">" + H(s.N.Primer) + "</td><td " + Td + ">" + H(Opis(s.N)) + "</td></tr>");
                sb.Append("</table>");
            }

            var prep = Grupisi(r, Tezina.Preporuka).Take(MaxPreporuka).ToList();
            if (prep.Count > 0)
            {
                sb.Append("<h3 style=\"color:#1565c0;margin:16px 0 6px\">" + (en ? "RECOMMENDATIONS" : "PREPORUKE") + "</h3><ul style=\"margin-top:0\">");
                foreach (var s in prep) sb.Append("<li>" + H(Regex.Replace(s.N.Tekst, @"^(Preporuka|Recommendation):\s*", "")) + "</li>");
                sb.Append("</ul>");
            }

            if (greske.Count == 0 && upoz.Count == 0)
                sb.Append("<p style=\"color:#1b5e20\"><b>" + (en ? "No errors or warnings were found." : "Nisu pronađene greške ni upozorenja.") + "</b></p>");
            if (nisuProcitani != null && nisuProcitani.Count > 0)
            {
                sb.Append("<h3 style=\"margin:16px 0 6px\">" + (en ? "ATTACHMENTS NOT CHECKED" : "PRILOZI KOJI NISU PROVERENI") + "</h3><ul>");
                foreach (var x in nisuProcitani) sb.Append("<li>" + H(x) + "</li>");
                sb.Append("</ul>");
            }
            foreach (var t in RucnaProvera(r, en)) sb.Append("<p>" + H(t) + "</p>");
            sb.Append("<p style=\"color:#777;font-size:11px;margin-top:18px\">" + H(p.Sablon("footer", en, "Izvor pravila: Prirucnik GLV-CIM (Spt 51.01), Prilog 2."))
                      + " " + (en ? "Rules version " : "Verzija pravila ") + H(p.Verzija) + ".<br>"
                      + (en ? "Generated automatically by the CIM check agent of Kombinovani prevoz d.o.o." : "Poruku je automatski sastavio agent za proveru CIM-a, Kombinovani prevoz d.o.o.") + "</p>");
            sb.Append("</div>");
            return sb.ToString();
        }

        /// <summary>Tekst za CLI kada nema tovarnog lista.</summary>
        public static string NemaTovarnogLista(PravilaSkup p, bool en) =>
            p.Sistemska("nema_tovarnog_lista", en, "U mailu nije pronađen tovarni list CIM/CUV.");

        /// <summary>Pregled prepoznatih dokumenata (za CLI i log).</summary>
        public static string Dokumenti(IEnumerable<Dokument> docs) =>
            string.Join(Environment.NewLine, docs.Select(d => "  - " + d.Fajl + " [" + string.Join(",", d.Strane) + "]: " + d.Tip + (string.IsNullOrEmpty(d.Opis) ? "" : " – " + d.Opis)));
    }
}
