using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>Potpis u mailu sa izveštajem (iz config-a: PotpisIme, PotpisTelefon, PotpisOdeljenje, PotpisEmail).</summary>
    internal sealed class PotpisIzvestaja
    {
        public string Ime = "", Telefon = "", Odeljenje = "Priprema saobraćaja", Email = "priprema@kprevoz.co.rs";

        public static PotpisIzvestaja Iz(AgentConfig cfg) => new PotpisIzvestaja
        {
            Ime = cfg?.PotpisIme ?? "", Telefon = cfg?.PotpisTelefon ?? "",
            Odeljenje = string.IsNullOrWhiteSpace(cfg?.PotpisOdeljenje) ? "Priprema saobraćaja" : cfg.PotpisOdeljenje,
            Email = string.IsNullOrWhiteSpace(cfg?.PotpisEmail) ? "priprema@kprevoz.co.rs" : cfg.PotpisEmail,
        };

        /// <summary>Redovi potpisa: [ime], odeljenje, [telefon], firma, adresa, e-mail – bez placeholdera.</summary>
        public List<string> Redovi(PravilaSkup p, bool en)
        {
            var r = new List<string>();
            if (!string.IsNullOrWhiteSpace(Ime)) r.Add(Ime.Trim());
            r.Add(en && Odeljenje == "Priprema saobraćaja" ? "Traffic preparation" : Odeljenje);
            if (!string.IsNullOrWhiteSpace(Telefon)) r.Add((en ? "Tel. " : "Tel. ") + Telefon.Trim());
            r.Add(p?.Kp?.Naziv ?? "Kombinovani prevoz d.o.o.");
            r.Add(p?.Kp?.Adresa ?? "Jug Bogdanova 28, 18400 Prokuplje");
            if (!string.IsNullOrWhiteSpace(Email)) r.Add(Email.Trim());
            return r.Where(x => !string.IsNullOrWhiteSpace(x) && !Regex.IsMatch(x, @"\[[^\]]*\]")).ToList();
        }
    }

    /// <summary>
    /// Mail sa izveštajem (v2.7): zahvalnica sa relacijom, tabela POTREBNE ISPRAVKE (greške), tabela NAPOMENE (upozorenja i
    /// preporuke), informacije, potpis. Kolone: Rb. | Rubrika | Šta nije u redu | Kako treba da izgleda (primer).
    /// HTML (jednostavne tabele sa inline stilovima) i tekst (numerisana lista sa istim kolonama).
    /// </summary>
    internal static class Izvestaj
    {
        public const int MaxPreporuka = 5;

        /// <summary>Potpis za sve izveštaje (postavlja ga agent iz config-a).</summary>
        public static PotpisIzvestaja Potpis = new PotpisIzvestaja();

        public static string Referenca(RezultatProvere r)
        {
            var prvi = r.Listovi.FirstOrDefault();
            if (prvi == null) return "";
            var c = prvi.Cim;
            var r62 = string.Join(" ", new[] { c.R62.Zemlja, c.R62.Stanica, c.R62.Preduzece, c.R62.Broj }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var refx = r62.Length > 0 ? r62 : !string.IsNullOrWhiteSpace(c.R8.Tekst) ? c.R8.Tekst.Trim() : System.IO.Path.GetFileName(prvi.Dokument.Fajl);
            if (r.Listovi.Count > 1) refx += " (+" + (r.Listovi.Count - 1) + ")";
            return Skrati(refx, 60);
        }

        private static string Skrati(string s, int max) => s.Length <= max ? s : s.Substring(0, max).TrimEnd() + "…";

        /// <summary>Naslov za obaveštenja bez rezultata provere (npr. samo spisak kola): "naslov primljenog maila – fajl".</summary>
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

        // ------------------------------------------------------------------------------------------- podaci pošiljke

        private static bool Pouzdano(Rubrika x) => x != null && x.Popunjena && x.Status == StatusRubrike.Popunjeno && x.Pouzdanost >= 0.8;

        private static string R62Id(CimList c)
        {
            var i = c.R62;
            var pouzdan = i.Status == StatusRubrike.Popunjeno && i.Pouzdanost >= 0.6 && Regex.IsMatch(i.Zemlja ?? "", @"^\d{2}$") && Regex.IsMatch(Regex.Replace(i.Stanica ?? "", @"\D", ""), @"^\d{5,6}$")
                          && Regex.IsMatch(i.Preduzece ?? "", @"^\d{4}$") && Regex.IsMatch(Regex.Replace(i.Broj ?? "", @"[\s\-]", ""), @"^\d{5,6}$");
            if (!pouzdan) return null;
            string SaKontrolnom(string x) { var c6 = Regex.Replace(x ?? "", @"\D", ""); return c6.Length == 6 ? c6.Substring(0, 5) + "-" + c6.Substring(5) : x; }
            return string.Join(" ", i.Zemlja, SaKontrolnom(i.Stanica), i.Preduzece, SaKontrolnom(i.Broj));
        }

        /// <summary>Referenca pošiljaoca (r8), samo kada je pouzdano pročitana i kratka.</summary>
        private static string ReferencaPosiljaoca(CimList c)
        {
            if (!Pouzdano(c.R8)) return null;
            var t = Regex.Replace(c.R8.Tekst ?? "", @"\s+", " ").Trim().Trim(',', ';', '.');
            t = Regex.Replace(t, @"^(?:ref\w*|reference|referenz|order\s*no|auftrag\w*)\s*[:.\-]?\s*", "", RegexOptions.IgnoreCase);
            return t.Length >= 3 && t.Length <= 30 ? t : null;
        }

        /// <summary>
        /// Identifikacija pošiljke za prvi red tela: r62 ako je pouzdano pročitan (sve četiri grupe ispravnog oblika), inače
        /// r8 i broj voza iz r15. Nepouzdano pročitan r62 se ne prikazuje.
        /// </summary>
        public static string Identifikacija(RezultatProvere r, bool en)
        {
            var delovi = new List<string>();
            foreach (var l in r.Listovi.Where(x => x.Cim != null))
            {
                var c = l.Cim;
                var id = R62Id(c);
                if (id == null)
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

        private static string Obrazac(RezultatProvere r) => r.Listovi.FirstOrDefault()?.Cim?.Obrazac == "CUV" ? "CUV" : "CIM";

        /// <summary>
        /// Naslov maila: „Tovarni list CIM 72 16550-6 3212 15690-0 (RuSz008/26) – potrebne ispravke“; samo upozorenja: „– napomene“;
        /// bez nalaza: „– ispravan“. Bez pouzdanog r62: naziv fajla; bez reference pošiljaoca: bez zagrade.
        /// </summary>
        public static string Naslov(RezultatProvere r, PravilaSkup p, bool en)
        {
            var prvi = r.Listovi.FirstOrDefault();
            var id = prvi?.Cim != null ? R62Id(prvi.Cim) : null;
            if (id == null) id = System.IO.Path.GetFileName((prvi?.Dokument?.Fajl ?? "").Split('/').Last());
            if (r.Listovi.Count > 1) id += " (+" + (r.Listovi.Count - 1) + ")";
            var refp = prvi?.Cim != null ? ReferencaPosiljaoca(prvi.Cim) : null;
            var status = r.BrojGresaka > 0 ? p.SablonStatus("errors", en, en ? "corrections required" : "potrebne ispravke")
                       : r.BrojUpozorenja > 0 ? p.SablonStatus("warnings", en, en ? "remarks" : "napomene")
                       : p.SablonStatus("ok", en, en ? "correct" : "ispravan");
            var sablon = p.Sablon("subject", en, en ? "Consignment note {form} {shipment_ref}{sender_ref} – {status}" : "Tovarni list {form} {shipment_ref}{sender_ref} – {status}");
            var n = sablon.Replace("{form}", Obrazac(r)).Replace("{shipment_ref}", id).Replace("{sender_ref}", refp != null ? " (" + refp + ")" : "").Replace("{status}", status);
            return Regex.Replace(n, @"\s{2,}", " ").Trim();
        }

        /// <summary>Zahvalnica: relacija (r16 – r10), broj kola, roba i referenca – samo ono što je pouzdano pročitano.</summary>
        internal static string Zahvalnica(RezultatProvere r, bool en)
        {
            var listova = r.Listovi.Count;
            var c = r.Listovi.FirstOrDefault()?.Cim;
            var uvod = en ? "Thank you for the documents" : "zahvaljujemo na dostavljenoj dokumentaciji";
            if (c == null || listova > 1)
                return uvod + (listova > 1 ? " (" + OpisListova(r, en) + ")." : ".");
            var ocr = r.Listovi.FirstOrDefault()?.Dokument?.Izvor == "ocr";
            // Mesto: prvi deo upisa (bez zemlje, šifara i reči od jednog slova), samo slova; inače se izostavlja.
            string Mesto(Rubrika rub, string s)
            {
                if (!Pouzdano(rub) || (ocr && rub.Pouzdanost < 0.9) || string.IsNullOrWhiteSpace(s)) return null;
                var prvi = Regex.Split(s, @"[,;\n(]|\s[-–—]\s")[0];
                var reci = Regex.Matches(prvi, @"[\p{L}.]+").Cast<Match>().Select(m => m.Value.Trim('.'))
                    .Where(w => w.Length >= 2 && !Regex.IsMatch(w, @"^(srbija|serbia|serbien|italia|italy|italija|romania|rumunija|hungary|magyarorsz\w*|deutschland|austria|österreich)$", RegexOptions.IgnoreCase)).ToList();
                if (reci.Count == 0 || reci.Count > 3 || reci.Any(w => w.Length > 15) || Regex.IsMatch(prvi, @"[^\p{L}\s.,\-–\d/]")) return null;
                var m0 = string.Join(" ", reci);
                return m0 == m0.ToUpperInvariant() ? System.Globalization.CultureInfo.GetCultureInfo("sr-Latn-RS").TextInfo.ToTitleCase(m0.ToLowerInvariant()) : m0;
            }
            var od = Mesto(c.R16, string.IsNullOrWhiteSpace(c.R16.Mesto) ? c.R16.Tekst : c.R16.Mesto);
            var @do = Mesto(c.R10, string.IsNullOrWhiteSpace(c.R10.Stanica) ? c.R10.Tekst : c.R10.Stanica);
            var relacija = od != null && @do != null ? od + " – " + @do : null;

            var detalji = new List<string>();
            if (BrojDodatnih(r) > 0) detalji.Add(OpisListova(r, en));   // „1 tovarni list + dodatni list“
            var kola = Math.Max(r.Dokumenti.Where(d => d.Tip == TipDokumenta.SpisakKola).SelectMany(d => d.Spisak).Select(s => s.Kola.Count).DefaultIfEmpty(0).Max(),
                Pouzdano(c.R21) ? Provere.BrojKolaUTekstu(c.R21.Tekst) : 0);
            if (kola > 0) detalji.Add(kola + (en ? (kola == 1 ? " wagon" : " wagons") : " kola"));
            // Roba: naziv posle NHM šifre u r21 („NHM-7204 10 ostaci i otpaci od čelika“), inače NHM iz r24; OCR tekst se ne prepisuje.
            string roba = null;
            if (Pouzdano(c.R21) && !ocr)
            {
                var m = Regex.Match(c.R21.Tekst ?? "", @"NHM[\s\-:]*\d{4,6}(?:\s\d{2})?\s+([\p{L}][\p{L} ]{3,40}?)(?=\s*(?:[,;/\n(]|$))", RegexOptions.IgnoreCase);
                if (m.Success) roba = m.Groups[1].Value.Trim();
            }
            var nhm = Pouzdano(c.R24) ? (c.R24.Stavke.Select(x => Regex.Replace(x.Sifra + x.Tekst, @"\D", "")).FirstOrDefault(x => x.Length >= 4) ?? Regex.Replace(c.R24.Tekst ?? "", @"\D", "")) : "";
            if (roba == null && Regex.IsMatch(nhm, @"^\d{4,8}$")) roba = "NHM " + nhm;
            if (roba != null) detalji.Add(roba);
            var refp = ReferencaPosiljaoca(c);
            if (refp != null) detalji.Add((en ? "reference " : "referenca ") + refp);
            var t = uvod;
            if (relacija != null) t += (en ? " for the consignment " : " za pošiljku ") + relacija;
            if (detalji.Count > 0) t += " (" + string.Join(", ", detalji) + ")";
            return t + ".";
        }

        // ------------------------------------------------------------------------------------------- stavke tabela

        internal sealed class Red
        {
            public string Rubrika, Sta, Primer;
            /// <summary>Oznake listova na koje se red odnosi (za PDF primer).</summary>
            public List<string> Listovi = new List<string>();
            public Nalaz N;
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
                    var s = stavke.FirstOrDefault(x => x.N.Pravilo == n.Pravilo && x.N.Tekst == n.Tekst && (x.N.Vrednost == n.Vrednost || tezina == Tezina.Preporuka));
                    if (s == null) stavke.Add(s = new Stavka { N = n });
                    if (!s.Listovi.Contains(l.Oznaka)) s.Listovi.Add(l.Oznaka);
                }
            return stavke.OrderBy(s => s.Listovi.FirstOrDefault() == null ? 0 : r.Listovi.FindIndex(l => l.Oznaka == s.Listovi[0]))
                .ThenBy(s => RubrikaRed(s.N.Rubrika)).ThenBy(s => s.N.Pravilo, StringComparer.Ordinal).ToList();
        }

        /// <summary>Naziv dokumenta: ime fajla, uz broj lista kada PDF ima više listova; "fajl.pdf (svi listovi, 14)".</summary>
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

        private static string Fajl(string oznaka) => Regex.Replace(oznaka ?? "", @"\s+–\s+list\s+\d+$", "").Split('/', '\\').Last();

        private static string List(string oznaka)
        {
            var m = Regex.Match(oznaka ?? "", @"–\s+list\s+(\d+)$");
            return m.Success ? m.Groups[1].Value : "";
        }

        /// <summary>Opis bez uvodnog "Rubrika 16:" (rubrika je posebna kolona).</summary>
        private static string BezRubrike(string t) => Regex.Replace(t ?? "", @"^\s*(Rubrika|Box|Preporuka|Recommendation)\s*\d*[a-z]?\s*(\([^)]*\))?\s*[:\-–]\s*", "", RegexOptions.IgnoreCase);

        private static double RubrikaRed(string r)
        {
            var m = Regex.Match(r ?? "", @"^(\d+)([a-z]?)");
            return m.Success ? int.Parse(m.Groups[1].Value) + (m.Groups[2].Value.Length > 0 ? 0.5 : 0) : -1;
        }

        /// <summary>Kolona „Rubrika“: „16 – Preuzimanje na prevoz“, „Spisak kola (CIT 23)“, „Ceo dokument“.</summary>
        internal static string RubrikaKolona(PravilaSkup p, string rubrika, bool en)
        {
            var naziv = p.NazivRubrike(rubrika, en);
            if (rubrika == "–" || rubrika == "CIT 23") return naziv.Length > 0 ? naziv : rubrika;
            return naziv.Length > 0 ? rubrika + " – " + naziv : rubrika;
        }

        /// <summary>Kolona „Šta nije u redu“: konkretan nalaz (ili opšte uputstvo) i upisana vrednost.</summary>
        private static string StaNije(RezultatProvere r, Stavka s, bool en)
        {
            var n = s.N;
            var upu = Veliko(BezRubrike(n.Uputstvo.Length > 0 ? n.Uputstvo : n.Tekst).Trim());
            var det = (n.Detalj ?? "").Trim();
            string t;
            if (det.Length == 0) t = upu;
            else if (Regex.IsMatch(det, @"^\((rubrika je prazna|box is empty)\)$")) t = (en ? "The box is empty. " : "Rubrika je prazna. ") + upu;
            else if (det.StartsWith("(") || det.Length < 30 || n.Takodje.Any(x => x.Pravilo != n.Pravilo))
                t = upu + " " + det;   // spojeni nalaz drugog pravila: zadržava se i poruka prvog
            else t = det;
            // upisana vrednost: samo kratka i kada nalaz nema svoj opis (dugačak sirovi tekst rubrike se ne prepisuje)
            if (det.Length == 0 && n.Tezina != Tezina.Preporuka && n.Vrednost.Length > 0 && n.Vrednost.Length <= 60 && n.Vrednost != "nečitljivo")
                t += (en ? " Entered: " : " Upisano: ") + n.Vrednost + ".";
            if (n.SamoInterno) t += en ? " (internal – not sent to the consignor)" : " (interno – ne šalje se pošiljaocu)";
            if (r.Listovi.Count > 1) t = "[" + NazivDokumenta(r, s, en) + "] " + t;
            return Regex.Replace(t, @"\s{2,}", " ").Trim();
        }

        private static string Veliko(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        private static string Primer(Nalaz n, bool en) =>
            !string.IsNullOrWhiteSpace(n.Primer) ? n.Primer.Trim() : (en ? "see description" : "vidi opis");

        /// <summary>Redovi tabele POTREBNE ISPRAVKE (greške).</summary>
        internal static List<Red> RedoviIspravki(RezultatProvere r, PravilaSkup p, bool en) =>
            Grupisi(r, Tezina.Greska).Select(s => new Red { N = s.N, Listovi = s.Listovi, Rubrika = RubrikaKolona(p, s.N.Rubrika, en), Sta = StaNije(r, s, en), Primer = Primer(s.N, en) }).ToList();

        /// <summary>Redovi tabele NAPOMENE: upozorenja, preporuke pravila (npr. NHM 6 cifara) i opšte preporuke (najviše 5).</summary>
        internal static List<Red> RedoviNapomena(RezultatProvere r, PravilaSkup p, bool en)
        {
            var prep = Grupisi(r, Tezina.Preporuka);
            var opste = prep.Where(s => s.N.Pravilo.StartsWith("REC", StringComparison.Ordinal)).Take(MaxPreporuka);
            return Grupisi(r, Tezina.Upozorenje).Concat(prep.Where(s => !s.N.Pravilo.StartsWith("REC", StringComparison.Ordinal))).Concat(opste)
                .Select(s => new Red { N = s.N, Listovi = s.Listovi, Rubrika = RubrikaKolona(p, s.N.Rubrika, en), Sta = StaNije(r, s, en), Primer = Primer(s.N, en) }).ToList();
        }

        /// <summary>Informacije ispod tabela (npr. „Platilac za deonicu kroz Srbiju: …“).</summary>
        internal static List<string> Informacije(RezultatProvere r) =>
            r.Listovi.SelectMany(l => l.Nalazi).Where(n => n.Tezina == Tezina.Info).Select(n => n.Tekst.Trim()).Distinct().ToList();

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

        // ------------------------------------------------------------------------------------------- tekst pisma

        private static string Uvodna(RezultatProvere r, bool en)
        {
            var obr = Obrazac(r);
            var vise = r.Listovi.Count > 1;
            if (r.BrojGresaka > 0)
                return en ? "When checking the " + obr + " consignment note" + (vise ? "s" : "") + " we found some deficiencies. Please correct them and send us the corrected consignment note" + (vise ? "s" : "") + ", so that the consignment can be dispatched without delay."
                          : "Prilikom provere tovarn" + (vise ? "ih listova " : "og lista ") + obr + " utvrdili smo nekoliko nedostataka. Molimo Vas da ih ispravite i dostavite nam ispravljen" + (vise ? "e tovarne listove" : " tovarni list") + ", kako bi pošiljka mogla biti otpremljena bez zastoja.";
            if (r.BrojUpozorenja > 0)
                return en ? "When checking the " + obr + " consignment note" + (vise ? "s" : "") + " we found no errors, but please check the remarks below."
                          : "Prilikom provere tovarn" + (vise ? "ih listova " : "og lista ") + obr + " nismo utvrdili greške, ali Vas molimo da proverite napomene u nastavku.";
            return en ? "We checked the " + obr + " consignment note" + (vise ? "s" : "") + " and found no deficiencies."
                      : "Proverili smo tovarn" + (vise ? "e listove " : "i list ") + obr + " i nismo utvrdili nedostatke.";
        }

        internal static string PrimerIstaknuto(bool en) => en ? "sample of the correctly completed consignment note" : "primer ispravno popunjenog tovarnog lista";

        /// <summary>
        /// Pasus o PDF primeru (B3) između uvoda i tabela: primer priložen; obrazac nije prepoznat (primer nije priložen);
        /// null kada nema nalaza ili primer nije napravljen zbog greške (mail ide bez priloga).
        /// </summary>
        internal static string PasusPrimera(PrimerRezultat pr, bool en)
        {
            if (pr == null) return null;
            if (pr.Napravljen)
                return en ? "Please find attached a " + PrimerIstaknuto(true) + " (" + pr.Ime + "). Correct entries are copied from your document; corrections are marked in red and boxes to be checked in orange. The sample is for guidance only – the corrected consignment note must be issued and signed by the consignor."
                          : "U prilogu Vam šaljemo " + PrimerIstaknuto(false) + " (" + pr.Ime + "). Ispravno upisani podaci prepisani su iz Vašeg dokumenta, a ispravke su označene crvenom bojom, dok su rubrike koje treba proveriti označene narandžastom. Primer služi samo kao pomoć pri ispravci – ispravan tovarni list izdaje i potpisuje pošiljalac.";
            if (pr.Razlog == "neprepoznat")
                return en ? "A sample of the correctly completed consignment note is not attached because the form was not recognised automatically."
                          : "Primer ispravnog tovarnog lista nije priložen jer obrazac nije automatski prepoznat.";
            return null;
        }

        private static string NaslovIspravki(bool en) => en ? "CORRECTIONS REQUIRED" : "POTREBNE ISPRAVKE";
        private static string NaslovNapomena(bool en) => en ? "REMARKS (please check)" : "NAPOMENE (molimo proverite)";
        private static string[] Kolone(bool en) => en ? new[] { "No.", "Box", "Issue", "Correct entry (example)" } : new[] { "Rb.", "Rubrika", "Šta nije u redu", "Kako treba da izgleda (primer)" };

        private static string NisuProcitaniTekst(IList<string> nisu, bool en) =>
            (en ? "Attachments not checked (could not be read): " : "Prilozi koji nisu provereni (nisu mogli da se pročitaju): ") + string.Join("; ", nisu) + ".";

        private static string Zavrsetak(bool en) => en
            ? "If you have any questions or need help with the corrections, please do not hesitate to contact us." + Environment.NewLine + Environment.NewLine + "Thank you in advance for your cooperation."
            : "Ukoliko imate bilo kakvih pitanja ili Vam je potrebna pomoć oko ispravke, slobodno nas kontaktirajte." + Environment.NewLine + Environment.NewLine + "Unapred hvala na saradnji.";

        private static string Futer(PravilaSkup p, bool en) =>
            (en ? "Checked automatically by the CIM check agent, rules version " : "Automatska provera agenta za CIM, verzija pravila ") + p.Verzija + ".";

        /// <summary>
        /// Tekstualni deo maila. <paramref name="uvod"/> = napomena na vrhu (režim Test, odlazni mail, interna kopija) – nije deo pisma.
        /// </summary>
        public static string Telo(RezultatProvere r, PravilaSkup p, bool en, string uvod = null, IList<string> nisuProcitani = null, string primer = null)
        {
            var nl = Environment.NewLine;
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(uvod)) sb.AppendLine(uvod).AppendLine();
            sb.AppendLine(en ? "Dear Sir or Madam," : "Poštovani,").AppendLine();
            sb.AppendLine(Zahvalnica(r, en)).AppendLine();
            sb.AppendLine(Uvodna(r, en)).AppendLine();
            if (!string.IsNullOrEmpty(primer)) sb.AppendLine(primer).AppendLine();

            void Tabela(string naslov, List<Red> redovi)
            {
                if (redovi.Count == 0) return;
                var k = Kolone(en);
                sb.AppendLine(naslov);
                int i = 0;
                foreach (var x in redovi)
                {
                    i++;
                    sb.AppendLine(i + ". " + (Regex.IsMatch(x.Rubrika ?? "", @"^\d") ? (en ? "Box " : "Rubrika ") : "") + x.Rubrika);
                    sb.AppendLine("   " + k[2] + ": " + x.Sta);
                    sb.AppendLine("   " + k[3] + ": " + x.Primer);
                }
                sb.AppendLine();
            }
            Tabela(NaslovIspravki(en), RedoviIspravki(r, p, en));
            Tabela(NaslovNapomena(en), RedoviNapomena(r, p, en));

            foreach (var t in Informacije(r)) sb.AppendLine(t);
            if (Informacije(r).Count > 0) sb.AppendLine();
            if (nisuProcitani != null && nisuProcitani.Count > 0) sb.AppendLine(NisuProcitaniTekst(nisuProcitani, en)).AppendLine();
            foreach (var t in RucnaProvera(r, en)) sb.AppendLine(t).AppendLine();

            sb.AppendLine(Zavrsetak(en)).AppendLine();
            sb.AppendLine(en ? "Kind regards," : "S poštovanjem,").AppendLine();
            foreach (var x in Potpis.Redovi(p, en)) sb.AppendLine(x);
            sb.Append(nl).AppendLine("--");
            sb.AppendLine(Futer(p, en));
            return sb.ToString();
        }

        // ------------------------------------------------------------------------------------------- HTML

        private static string H(string s) => System.Net.WebUtility.HtmlEncode(s ?? "");

        private const string Font = "font-family:Arial,Calibri,sans-serif;font-size:10pt;color:#222222";
        private const string Th = "style=\"background-color:#d9d9d9;border:1px solid #999999;padding:5px 7px;text-align:left;vertical-align:top;font-family:Arial,Calibri,sans-serif;font-size:10pt\"";
        private const string Td = "style=\"border:1px solid #999999;padding:5px 7px;vertical-align:top;font-family:Arial,Calibri,sans-serif;font-size:10pt\"";

        /// <summary>HTML deo maila: jednostavne tabele sa inline stilovima (bez klasa, slika i skripti – zbog Outlook-a).</summary>
        public static string Html(RezultatProvere r, PravilaSkup p, bool en, string uvod = null, IList<string> nisuProcitani = null, string primer = null)
        {
            var sb = new StringBuilder();
            sb.Append("<div style=\"" + Font + "\">");
            if (!string.IsNullOrEmpty(uvod))
                sb.Append("<p style=\"background-color:#f2f2f2;border:1px solid #cccccc;padding:6px 8px;font-size:9pt;color:#555555\">" + H(uvod).Replace("\n", "<br>") + "</p>");
            sb.Append("<p>" + (en ? "Dear Sir or Madam," : "Poštovani,") + "</p>");
            sb.Append("<p>" + H(Zahvalnica(r, en)) + "</p>");
            sb.Append("<p>" + H(Uvodna(r, en)) + "</p>");
            if (!string.IsNullOrEmpty(primer)) sb.Append("<p>" + H(primer).Replace(H(PrimerIstaknuto(en)), "<b>" + H(PrimerIstaknuto(en)) + "</b>") + "</p>");

            void Tabela(string naslov, List<Red> redovi, string boja)
            {
                if (redovi.Count == 0) return;
                sb.Append("<h3 style=\"font-family:Arial,Calibri,sans-serif;font-size:11pt;color:" + boja + ";margin:14px 0 6px 0\">" + H(naslov) + "</h3>");
                sb.Append("<table cellspacing=\"0\" cellpadding=\"0\" style=\"border-collapse:collapse;border:1px solid #999999\"><tr>");
                foreach (var k in Kolone(en)) sb.Append("<th " + Th + ">" + H(k) + "</th>");
                sb.Append("</tr>");
                int i = 0;
                foreach (var x in redovi)
                {
                    i++;
                    sb.Append("<tr><td " + Td + ">" + i + "</td><td " + Td + "><b>" + H(x.Rubrika) + "</b></td><td " + Td + ">" + H(x.Sta) + "</td><td " + Td + ">" + H(x.Primer) + "</td></tr>");
                }
                sb.Append("</table>");
            }
            Tabela(NaslovIspravki(en), RedoviIspravki(r, p, en), "#b00020");
            Tabela(NaslovNapomena(en), RedoviNapomena(r, p, en), "#8a5a00");

            var info = Informacije(r);
            if (info.Count > 0) sb.Append("<p style=\"margin-top:14px\">" + string.Join("<br>", info.Select(H)) + "</p>");
            if (nisuProcitani != null && nisuProcitani.Count > 0) sb.Append("<p>" + H(NisuProcitaniTekst(nisuProcitani, en)) + "</p>");
            foreach (var t in RucnaProvera(r, en)) sb.Append("<p>" + H(t) + "</p>");

            sb.Append("<p style=\"margin-top:14px\">" + H(Zavrsetak(en)).Replace(Environment.NewLine + Environment.NewLine, "</p><p>") + "</p>");
            sb.Append("<p>" + (en ? "Kind regards," : "S poštovanjem,") + "</p>");
            sb.Append("<p>" + string.Join("<br>", Potpis.Redovi(p, en).Select(H)) + "</p>");
            sb.Append("<p style=\"color:#888888;font-size:8pt;margin-top:16px\">" + H(Futer(p, en)) + "</p>");
            sb.Append("</div>");
            return sb.ToString();
        }

        // ------------------------------------------------------------------------------------------- poređenje

        /// <summary>
        /// Stavke izveštaja u ranijem obliku („GREŠKE / UPOZORENJA, N. [dokument] Rubrika X – opis“) – za poređenje sa prvim
        /// izveštajima (Poredjenje). Preporuke i informacije nisu uključene.
        /// </summary>
        internal static string TekstZaPoredjenje(RezultatProvere r, bool en)
        {
            var sb = new StringBuilder();
            foreach (var (tez, naslov) in new[] { (Tezina.Greska, en ? "ERRORS" : "GREŠKE"), (Tezina.Upozorenje, en ? "WARNINGS" : "UPOZORENJA") })
            {
                var stavke = Grupisi(r, tez);
                if (stavke.Count == 0) continue;
                sb.AppendLine(naslov);
                int i = 0;
                foreach (var s in stavke)
                {
                    i++;
                    var rub = Regex.IsMatch(s.N.Rubrika ?? "", @"^\d") ? (en ? "Box " : "Rubrika ") + s.N.Rubrika + " – " : "";
                    var opis = BezRubrike(string.IsNullOrEmpty(s.N.Uputstvo) ? s.N.Tekst : s.N.Uputstvo);
                    sb.AppendLine(i + ". [" + NazivDokumenta(r, s, en) + "] " + rub + (string.IsNullOrEmpty(s.N.Detalj) ? opis : s.N.Detalj + "  " + opis));
                }
                sb.AppendLine();
            }
            sb.AppendLine("--");
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
