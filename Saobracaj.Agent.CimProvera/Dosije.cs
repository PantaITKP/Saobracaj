using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>Podaci o mailu za povezivanje u dosije (bez sadržaja dokumenata).</summary>
    internal sealed class MailZaDosije
    {
        public string Oznaka = "";           // „UID 262“ ili ime slučaja u validate
        public string MessageId = "";
        public List<string> Prethodni = new List<string>();   // In-Reply-To + References
        public string Naslov = "";
        public string Posiljalac = "";
        public DateTime Vreme;
    }

    /// <summary>
    /// v2.8.4 (8): dosije pošiljke – više mailova i dokumenata iste pošiljke (rok 7 dana). Veza: In-Reply-To/References, naslov bez
    /// RE:/FW:/Fwd:/Odg:/AW: (kratak opšti naslov „CIM“ samo uz još jedan znak), ključ lista (r62; pošiljalac + r8 + relacija), r8,
    /// brojevi kola (bar 3 ista), naslov koji pominje pošiljaoca lista iz dosijea. Čuva se u StanjeFolder\posiljke.json (reset briše).
    /// Bez sadržaja dokumenata: samo izvučeni podaci (vrsta, fajl, MRN, kola, ključevi, nalazi po rubrikama).
    /// </summary>
    internal sealed class Dosije
    {
        public const string Ime = "posiljke.json";
        public const int Dana = 7;

        public sealed class ListZapis
        {
            public string Kljuc { get; set; } = "";
            public string Otisak { get; set; } = "";
            public string Oznaka { get; set; } = "";
            public DateTime Vreme { get; set; }
            /// <summary>Digitalni list (tekst PDF-a, Excel, Word) – sken iste pošiljke je „skenirana i overena verzija“.</summary>
            public bool Digitalni { get; set; }
            /// <summary>Nalazi (greške i upozorenja) kao „pravilo|rubrika“.</summary>
            public List<string> Nalazi { get; set; } = new List<string>();
        }

        public sealed class Posiljka
        {
            public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 12);
            public DateTime Prvi { get; set; }
            public DateTime Poslednji { get; set; }
            public List<string> Mailovi { get; set; } = new List<string>();
            public List<string> MessageIds { get; set; } = new List<string>();
            public List<string> Naslovi { get; set; } = new List<string>();
            public List<string> Kljucevi { get; set; } = new List<string>();
            public List<string> R8 { get; set; } = new List<string>();
            public List<string> Posiljaoci { get; set; } = new List<string>();
            public List<string> Kola { get; set; } = new List<string>();
            /// <summary>Prateći dokumenti iz ranijih mailova (bez tovarnih listova) – priloženost na celom dosijeu.</summary>
            public List<Dokument> Dokumenti { get; set; } = new List<Dokument>();
            public List<ListZapis> Listovi { get; set; } = new List<ListZapis>();
        }

        public List<Posiljka> Posiljke { get; set; } = new List<Posiljka>();
        [Newtonsoft.Json.JsonIgnore] private string putanja;

        public static Dosije Ucitaj(string folder)
        {
            var p = Path.Combine(folder ?? ".", Ime);
            Dosije d = null;
            try { if (File.Exists(p)) d = Json.Iz<Dosije>(File.ReadAllText(p, Encoding.UTF8)); } catch { d = null; }
            d = d ?? new Dosije();
            d.putanja = p;
            return d;
        }

        public void Sacuvaj()
        {
            Posiljke.RemoveAll(x => x.Poslednji < DateTime.Now.AddDays(-30));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(putanja)));
            var tmp = putanja + ".tmp";
            File.WriteAllText(tmp, Json.U(this), Encoding.UTF8);
            if (File.Exists(putanja)) File.Replace(tmp, putanja, null); else File.Move(tmp, putanja);
        }

        // ------------------------------------------------------------------------------------------- ključevi

        private static string N(string s) => Regex.Replace(PraviloKp.Normalizuj(s ?? ""), @"\s+", " ").Trim();

        /// <summary>Naslov bez „RE:/FW:/Fwd:/Odg:/AW:/SV:/TR:/WG:“ (i ponovljenih), normalizovan.</summary>
        internal static string Naslov(string s)
        {
            var t = (s ?? "").Trim();
            for (Match m; (m = Regex.Match(t, @"^\s*(?:re|fw|fwd|odg|aw|sv|tr|wg|antw)\s*(?:\[\d+\])?\s*:\s*", RegexOptions.IgnoreCase)).Success;) t = t.Substring(m.Length);
            return N(t);
        }

        /// <summary>Opšti, kratak naslov („CIM“, „CUV“, „Documents“) – sam nije dovoljan za vezu.</summary>
        private static bool OpstiNaslov(string n) => n.Length < 6 || Regex.IsMatch(n, @"^(cim|cuv|cim cuv|documents?|dokumenta(cija)?|tovarni list|fw|scan\w*)$");

        private static string R8(CimList c)
        {
            var t = N(Motor.RubrikaLista(c, "8")?.Tekst);
            return t.Length >= 4 && Regex.IsMatch(t, @"\d") ? t.Replace("train no ", "train no") : "";
        }

        /// <summary>Pošiljalac (r1 naziv): prve dve značajne reči („yu din commerce“).</summary>
        private static string Posiljalac(CimList c)
        {
            var reci = N(c.R1?.Naziv ?? "").Split(' ').Where(w => w.Length >= 2 && !Regex.IsMatch(w, @"^(d|o|doo|ad|gmbh|kft|srl|spa|sa|as|ltd)$")).Take(3).ToList();
            var t = string.Join(" ", reci);
            return t.Replace(" ", "").Length >= 6 ? t : "";
        }

        private static IEnumerable<string> Kola(IEnumerable<Dokument> docs) =>
            docs.SelectMany(d => d.Spisak.SelectMany(s => s.Kola.Select(k => Regex.Replace(k.Broj ?? "", @"\D", ""))))
                .Concat(docs.SelectMany(d => d.Cim).SelectMany(c => (c.R18?.Stavke ?? new List<SifraTekst>()).Select(x => Regex.Replace(x.Tekst ?? "", @"\D", ""))))
                .Where(k => k.Length == 12).Distinct();

        private static IEnumerable<CimList> Listovi(IEnumerable<Dokument> docs) =>
            docs.Where(d => TipDokumenta.JeTovarniList(d.Tip)).SelectMany(d => d.Cim).Where(c => string.IsNullOrEmpty(c.NeprepoznatObrazac));

        // ------------------------------------------------------------------------------------------- povezivanje

        /// <summary>Pošiljka (dosije) za mail i njegove dokumente – poslednja aktivna u roku od 7 dana; null ako je nova.</summary>
        public Posiljka Nadji(MailZaDosije mail, List<Dokument> docs, out string razlog)
        {
            razlog = null;
            var aktivne = Posiljke.Where(p => p.Poslednji >= mail.Vreme.AddDays(-Dana) && p.Prvi <= mail.Vreme.AddMinutes(1)).OrderByDescending(p => p.Poslednji).ToList();
            if (aktivne.Count == 0) return null;
            var naslov = Naslov(mail.Naslov);
            var listovi = Listovi(docs).ToList();
            var kljucevi = listovi.Select(Ponavljanja.KljucLista).Where(k => k != null).ToList();
            var r8 = listovi.Select(R8).Where(x => x.Length > 0).ToList();
            var kola = Kola(docs).ToList();
            foreach (var p in aktivne)
            {
                if (mail.Prethodni.Any(x => p.MessageIds.Contains(x))) { razlog = "nit (In-Reply-To/References)"; return p; }
                if (kljucevi.Any(k => p.Kljucevi.Contains(k))) { razlog = "isti tovarni list (" + kljucevi.First(k => p.Kljucevi.Contains(k)) + ")"; return p; }
                var istiR8 = r8.FirstOrDefault(x => p.R8.Contains(x));
                if (istiR8 != null) { razlog = "r8 „" + istiR8 + "“"; return p; }
                var zajednickaKola = kola.Count(k => p.Kola.Contains(k));
                if (zajednickaKola >= 3) { razlog = zajednickaKola + " ista broja kola"; return p; }
                if (naslov.Length > 0 && p.Naslovi.Contains(naslov))
                {
                    if (!OpstiNaslov(naslov)) { razlog = "naslov „" + naslov + "“"; return p; }
                    // kratak opšti naslov („CIM“) uz još jedan znak: isti pošiljalac lista
                    if (listovi.Select(Posiljalac).Any(x => x.Length > 0 && p.Posiljaoci.Contains(x))) { razlog = "naslov „" + naslov + "“ i isti pošiljalac"; return p; }
                }
                // naslov pominje pošiljaoca lista iz dosijea („CIM YU DIN COMMERCE“ ↔ r1 „Yu Din Commerce D.o.o“)
                var pos = p.Posiljaoci.FirstOrDefault(x => x.Length >= 6 && (" " + naslov + " ").Contains(" " + x + " "));
                if (pos != null) { razlog = "naslov pominje pošiljaoca „" + pos + "“"; return p; }
            }
            return null;
        }

        /// <summary>Zapis maila u dosije (nova pošiljka ako p == null): nit, naslov, ključevi, r8, kola, prateći dokumenti i nalazi listova.</summary>
        public Posiljka Zapamti(Posiljka p, MailZaDosije mail, List<Dokument> docs, RezultatProvere r)
        {
            if (p == null) { p = new Posiljka { Prvi = mail.Vreme }; Posiljke.Add(p); }
            p.Poslednji = mail.Vreme > p.Poslednji ? mail.Vreme : p.Poslednji;
            if (!p.Mailovi.Contains(mail.Oznaka)) p.Mailovi.Add(mail.Oznaka);
            void Dodaj(List<string> l, IEnumerable<string> x) { foreach (var v in x.Where(v => !string.IsNullOrEmpty(v))) if (!l.Contains(v)) l.Add(v); }
            Dodaj(p.MessageIds, new[] { mail.MessageId });
            Dodaj(p.Naslovi, new[] { Naslov(mail.Naslov) });
            var listovi = Listovi(docs).ToList();
            Dodaj(p.Kljucevi, listovi.Select(Ponavljanja.KljucLista));
            Dodaj(p.R8, listovi.Select(R8));
            Dodaj(p.Posiljaoci, listovi.Select(Posiljalac));
            Dodaj(p.Kola, Kola(docs));
            foreach (var d in docs.Where(d => !TipDokumenta.JeTovarniList(d.Tip) && !d.IzDosijea && d.Tip != TipDokumenta.Ostalo))
                if (!p.Dokumenti.Any(x => x.Tip == d.Tip && x.Fajl == d.Fajl && x.Strane.SequenceEqual(d.Strane)))
                    p.Dokumenti.Add(new Dokument { Tip = d.Tip, Fajl = d.Fajl, Strane = d.Strane.ToList(), Opis = d.Opis, Carinski = d.Carinski, Spisak = d.Spisak, Izvor = d.Izvor, IzMaila = mail.Oznaka });
            if (r != null)
                foreach (var l in r.Listovi.Where(x => x.Cim != null && string.IsNullOrEmpty(x.Cim.NeprepoznatObrazac)))
                {
                    var k = Ponavljanja.KljucLista(l.Cim);
                    if (k == null) continue;
                    p.Listovi.Add(new ListZapis
                    {
                        Kljuc = k, Otisak = Ponavljanja.Otisak(l.Cim), Oznaka = mail.Oznaka, Vreme = mail.Vreme,
                        Digitalni = l.Dokument?.Izvor != "ocr",
                        Nalazi = l.Nalazi.Where(n => n.Tezina == Tezina.Greska || n.Tezina == Tezina.Upozorenje).Select(n => n.Pravilo + "|" + n.Rubrika).Distinct().ToList(),
                    });
                }
            return p;
        }

        /// <summary>Prateći dokumenti iz dosijea (ranijih mailova) kao dodatni dokumenti provere (priloženost na celom dosijeu).</summary>
        public static List<Dokument> DokumentiIzDosijea(Posiljka p) =>
            p?.Dokumenti.Select(d => new Dokument { Tip = d.Tip, Fajl = d.Fajl, Strane = d.Strane.ToList(), Opis = (d.Opis ?? "") + " (iz dosijea: " + d.IzMaila + ")", Carinski = d.Carinski, Spisak = d.Spisak, Izvor = d.Izvor, IzDosijea = true, IzMaila = d.IzMaila }).ToList()
            ?? new List<Dokument>();

        // ------------------------------------------------------------------------------------------- verzije

        /// <summary>Rubrike koje se proveravaju na skeniranoj i overenoj verziji već proverenog digitalnog lista: pečat (r58a), r62, r29.</summary>
        public static readonly HashSet<string> RubrikeOverene = new HashSet<string> { "58a", "62", "29" };

        /// <summary>
        /// Posle provere: (1) skenirana i overena verzija već proverenog digitalnog lista – ostaju samo nalazi za pečat (r58a), r62 i r29;
        /// (2) nova verzija istog lista (isti ključ, drugi sadržaj) – „Ovo je nova verzija tovarnog lista od &lt;datum&gt;. Ispravljeno: …
        /// Ostalo neispravljeno: …“. Uvod se upisuje u <see cref="RezultatProvere.UvodDosijea"/>.
        /// </summary>
        public static void Primeni(Posiljka p, RezultatProvere r, bool en)
        {
            if (p == null || r == null) return;
            var uvodi = new List<string>();
            foreach (var l in r.Listovi.Where(x => x.Cim != null && string.IsNullOrEmpty(x.Cim.NeprepoznatObrazac)))
            {
                var k = Ponavljanja.KljucLista(l.Cim);
                var sken = l.Dokument?.Izvor == "ocr";
                var raniji = p.Listovi.Where(x => k != null && x.Kljuc == k).OrderByDescending(x => x.Vreme).FirstOrDefault();
                // skenirana i overena verzija: sken, u dosijeu proveren digitalni list (isti ključ, ili jedini digitalni list kada r62 skena nije pouzdana)
                var digitalni = sken ? (p.Listovi.Where(x => x.Digitalni && (x.Kljuc == k || k == null || !k.StartsWith("r62:"))).OrderByDescending(x => x.Vreme).FirstOrDefault()) : null;
                if (digitalni != null && (raniji == null || raniji.Digitalni))
                {
                    l.Nalazi.RemoveAll(n => !n.SamoInterno && n.Rubrika != "–" && !RubrikeOverene.Contains(n.Rubrika) && n.Pravilo != "KP2");
                    uvodi.Add(en
                        ? "This is the scanned and stamped version of the consignment note already checked on " + digitalni.Vreme.ToString("dd.MM.yyyy") + " (" + digitalni.Oznaka + ", digital). Only what is new is checked: stamp (box 58a), box 62 and box 29."
                        : "Ovo je skenirana i overena verzija tovarnog lista koji je već proveren " + digitalni.Vreme.ToString("dd.MM.yyyy") + " (" + digitalni.Oznaka + ", digitalni). Proverava se samo ono što je novo: pečat (r58a), r62 i r29.");
                    continue;
                }
                if (raniji == null || raniji.Otisak == Ponavljanja.Otisak(l.Cim)) continue;
                var sada = l.Nalazi.Where(n => n.Tezina == Tezina.Greska || n.Tezina == Tezina.Upozorenje).Select(n => n.Rubrika).Where(x => x != "–").Distinct().ToList();
                var pre = raniji.Nalazi.Select(x => x.Split('|').Last()).Where(x => x != "–").Distinct().ToList();
                var ispravljeno = pre.Where(x => !sada.Contains(x)).ToList();
                var ostalo = pre.Where(x => sada.Contains(x)).ToList();
                string Lista(List<string> x) => x.Count == 0 ? (en ? "none" : "ništa") : string.Join(", ", Sortiraj(x).Select(y => "r" + y));
                uvodi.Add(en
                    ? "This is a new version of the consignment note of " + raniji.Vreme.ToString("dd.MM.yyyy") + ". Corrected: " + Lista(ispravljeno) + ". Still not corrected: " + Lista(ostalo) + "."
                    : "Ovo je nova verzija tovarnog lista od " + raniji.Vreme.ToString("dd.MM.yyyy") + ". Ispravljeno: " + Lista(ispravljeno) + ". Ostalo neispravljeno: " + Lista(ostalo) + ".");
            }
            if (uvodi.Count > 0) r.UvodDosijea = string.Join(Environment.NewLine, uvodi.Distinct());
        }

        private static IEnumerable<string> Sortiraj(IEnumerable<string> rubrike) =>
            rubrike.OrderBy(x => int.TryParse(Regex.Match(x, @"^\d+").Value, out var b) ? b : 999).ThenBy(x => x, StringComparer.Ordinal);
    }
}
