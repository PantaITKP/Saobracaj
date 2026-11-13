using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera
{
    /// <summary>Sažetak obrađenog tovarnog lista (stanje\listovi.json) – za vezu CIT 7 sa listom iz ranijeg maila.</summary>
    internal sealed class ZapisLista
    {
        public DateTime Datum { get; set; }
        public string Mail { get; set; } = "";
        public string Fajl { get; set; } = "";
        public string Tip { get; set; } = "";
        public string Zemlja { get; set; } = "";
        public string Stanica { get; set; } = "";
        public string Preduzece { get; set; } = "";
        public string Broj { get; set; } = "";
        public string R1 { get; set; } = "";
        public string R4 { get; set; } = "";
        public string R7 { get; set; } = "";
        public string R10 { get; set; } = "";
        public string R12 { get; set; } = "";
        public string R16 { get; set; } = "";
        public List<string> Kola { get; set; } = new List<string>();
        [JsonIgnore] public bool IzIstogMaila;
        [JsonIgnore] public RezultatLista Lista;
        [JsonIgnore] public string Identifikacija => Regex.Replace(string.Join(" ", new[] { Zemlja, Stanica, Preduzece, Broj }.Where(x => !string.IsNullOrWhiteSpace(x))), @"\s+", " ").Trim();
        [JsonIgnore] public string Opis => Path.GetFileName((Fajl ?? "").Split('/', '\\').Last()) + (Identifikacija.Length > 0 ? " (r62 " + Identifikacija + ")" : "")
                                          + (IzIstogMaila ? " – u istom mailu" : " – ranije obrađen " + Datum.ToString("dd.MM.yyyy") + (Mail.Length > 0 ? ", mail „" + Mail + "“" : ""));
    }

    /// <summary>
    /// v2.8.1: istorija obrađenih tovarnih listova (stanje\listovi.json, najviše 3000 zapisa i 180 dana): r62, strane, relacija,
    /// r7 i brojevi kola – bez ostalog sadržaja dokumenta. Služi samo za vezu CIT 7 sa listom koji nije u istom mailu.
    /// </summary>
    internal static class IstorijaListova
    {
        public const string ImeFajla = "listovi.json";
        public const int MaxZapisa = 3000;
        public const int Dana = 180;

        public static List<ZapisLista> Ucitaj(string folder)
        {
            try
            {
                var f = Path.Combine(folder ?? "", ImeFajla);
                return File.Exists(f) ? JsonConvert.DeserializeObject<List<ZapisLista>>(File.ReadAllText(f, Encoding.UTF8)) ?? new List<ZapisLista>() : new List<ZapisLista>();
            }
            catch { return new List<ZapisLista>(); }
        }

        public static void Dodaj(string folder, IEnumerable<ZapisLista> novi)
        {
            var sve = Ucitaj(folder);
            var lista = novi.ToList();
            // ponovna obrada istog maila ne pravi duplikate
            sve.RemoveAll(x => lista.Any(n => n.Mail == x.Mail && n.Fajl == x.Fajl && n.Datum == x.Datum));
            sve.AddRange(lista);
            var granica = DateTime.Now.AddDays(-Dana);
            sve = sve.Where(x => x.Datum >= granica).OrderByDescending(x => x.Datum).Take(MaxZapisa).OrderBy(x => x.Datum).ToList();
            Directory.CreateDirectory(folder);
            var f = Path.Combine(folder, ImeFajla);
            var tmp = f + ".tmp";
            File.WriteAllText(tmp, JsonConvert.SerializeObject(sve, Formatting.Indented), new UTF8Encoding(false));
            if (File.Exists(f)) File.Replace(tmp, f, null); else File.Move(tmp, f);
        }

        public static ZapisLista Iz(RezultatLista l, IEnumerable<Dokument> svi, DateTime datum, string mail)
        {
            var c = l.Cim;
            var kola = c.R18.Stavke.SelectMany(s => Regex.Matches(Regex.Replace(s.Sifra + " " + s.Tekst, @"(?<=\d)[\s\-](?=\d)", ""), @"(?<!\d)\d{12}(?!\d)").Cast<Match>().Select(m => m.Value)).ToList();
            var listova = svi.Where(d => TipDokumenta.JeTovarniList(d.Tip)).Sum(d => d.Cim.Count);
            string Broj(Identifikacija r) => Regex.Replace(r?.Broj ?? "", @"\D", "");
            foreach (var s in svi.Where(d => d.Tip == TipDokumenta.SpisakKola).SelectMany(d => d.Spisak))
                if (listova == 1 || Broj(s.R62).Length == 0 || Broj(s.R62) == Broj(c.R62)) kola.AddRange(s.Kola.Select(k => Regex.Replace(k.Broj ?? "", @"\D", "")).Where(k => k.Length == 12));
            return new ZapisLista
            {
                Datum = datum, Mail = mail ?? "", Fajl = l.Dokument?.Fajl ?? "", Tip = l.Dokument?.Tip ?? "",
                Zemlja = c.R62.Zemlja ?? "", Stanica = c.R62.Stanica ?? "", Preduzece = c.R62.Preduzece ?? "", Broj = c.R62.Broj ?? "",
                R1 = Jedan(c.R1.Tekst), R4 = Jedan(c.R4.Tekst), R10 = Jedan(c.R10.Tekst), R12 = Jedan(c.R12.Tekst), R16 = Jedan(c.R16.Tekst),
                // šifra 7.1 (codelists.r7) = „Primalac nema pravo raspolaganja“
                R7 = Jedan(c.R7.Tekst) + (c.R7.Stavke.Any(s => Regex.Replace(s.Sifra ?? "", @"\D", "") == "1") ? " [7.1 Primalac nema pravo raspolaganja]" : ""),
                Kola = kola.Distinct().ToList(),
            };
        }

        private static string Jedan(string t) => Regex.Replace(Regex.Replace(t ?? "", @"\s*\n\s*", ", "), @"\s+", " ").Trim();
    }

    /// <summary>Rezultat provere jednog CIT 7.</summary>
    internal sealed class Cit7Rezultat
    {
        public string Fajl = "";
        public Cit7Zahtev Z;
        public ZapisLista Veza;
        public List<string> Osnov = new List<string>();
        public List<string> Razlike = new List<string>();
        public List<(string rubrika, string stara, string nova)> Izmene = new List<(string, string, string)>();
        public List<string> Upozorenja = new List<string>();
        public List<string> Napomene = new List<string>();
        /// <summary>„pošiljalac“, „primalac“ ili opis kada nije utvrđeno.</summary>
        public string Podnosilac = "";
    }

    /// <summary>
    /// v2.8.1: CIT 7 – naknadni zahtev (GLV-CIM, Prilog 7; čl. 18 i 19 CIM). Veza sa tovarnim listom (r62, brojevi kola, relacija i
    /// strane), šta se menja (stara → nova vrednost) i provere kao UPOZORENJA U INTERNOM IZVEŠTAJU: duplikat lista sa istim izmenama,
    /// „Primalac nema pravo raspolaganja“ u r7 kada zahtev podnosi pošiljalac, deljenje pošiljke, promena carinskog područja.
    /// Ništa od ovoga se ne šalje pošiljaocu.
    /// </summary>
    internal static class Cit7Provera
    {
        private const int PragVeze = 35;

        public static List<Cit7Rezultat> Proveri(RezultatProvere r, List<ZapisLista> ranije, DateTime datum, string mail)
        {
            var rez = new List<Cit7Rezultat>();
            var cit7 = r.Dokumenti.Where(d => d.Tip == TipDokumenta.Cit7 && d.Cit7 != null).ToList();
            if (cit7.Count == 0) return rez;
            var uMailu = r.Listovi.Where(l => l.Cim != null && string.IsNullOrEmpty(l.Cim.NeprepoznatObrazac))
                .Select(l => { var x = IstorijaListova.Iz(l, r.Dokumenti, datum, mail); x.IzIstogMaila = true; x.Lista = l; return x; }).ToList();
            var raniji = (ranije ?? new List<ZapisLista>()).ToList();
            foreach (var d in cit7)
            {
                var z = d.Cit7;
                var c = new Cit7Rezultat { Fajl = d.Fajl, Z = z };
                var (veza, osnov, _) = Najbolji(z, uMailu);
                if (veza == null) (veza, osnov, _) = Najbolji(z, raniji);
                c.Veza = veza; c.Osnov = osnov ?? new List<string>();
                if (veza != null) c.Razlike = Razlike(z, veza);

                foreach (var iz in z.Izmene)
                    c.Izmene.Add((iz.Rubrika, StaraVrednost(iz.Rubrika, z, veza), iz.Vrednost));

                // 1. duplikat tovarnog lista sa istim izmenama
                if (c.Izmene.Count > 0)
                {
                    var dup = uMailu.FirstOrDefault(k => c.Izmene.All(x => Slicno(x.nova, Polje(k, x.rubrika))));
                    if (dup != null) c.Napomene.Add("Priložen je duplikat tovarnog lista sa istim izmenama: " + Path.GetFileName(dup.Fajl) + ".");
                    else c.Upozorenja.Add("Uz zahtev nije priložen duplikat tovarnog lista sa istim izmenama (" + string.Join(", ", c.Izmene.Select(x => "r" + x.rubrika)) + ") – duplikat sa upisanim izmenama se prilaže uz CIT 7.");
                }

                // 2. ko podnosi zahtev; pošiljalac → r7 „Primalac nema pravo raspolaganja“
                // poređenje po NAZIVU (prvi deo do zareza), ne po adresi – „EurologSystem Beograd“ nije primalac iz Novog Beograda
                var jePrimalac = Slicno(z.Podnosilac, NazivStrane(veza?.R4)) || Slicno(z.Podnosilac, NazivStrane(z.Primalac));
                var jePosiljalac = !jePrimalac && (Slicno(z.Podnosilac, NazivStrane(veza?.R1)) || Slicno(z.Podnosilac, NazivStrane(z.Posiljalac)));
                c.Podnosilac = jePrimalac ? "primalac" : jePosiljalac ? "pošiljalac"
                    : z.Podnosilac.Length > 0 ? "nije utvrđeno – potpisao „" + z.Podnosilac + "“" + (Slicno(z.Podnosilac, veza?.R7) ? ", naveden u r7 lista (platilac/zastupnik)" : "") + "; nije ni pošiljalac ni primalac iz lista" : "nije utvrđeno (potpis nije pročitan)";
                if (!jePrimalac)
                {
                    if (veza == null) c.Napomene.Add("Tovarni list nije pronađen – ne može da se proveri da li u r7 piše „Primalac nema pravo raspolaganja“.");
                    else if (!PrimalacNemaPravo(veza.R7))
                        c.Upozorenja.Add((jePosiljalac ? "Zahtev podnosi pošiljalac" : "Zahtev ne podnosi primalac (" + c.Podnosilac + ")")
                            + ", a u r7 tovarnog lista ne piše „Primalac nema pravo raspolaganja“ (Le destinataire n'a pas le droit de disposer / Empfänger nicht verfügungsberechtigt) – i primalac ima pravo raspolaganja; proveriti ovlašćenje podnosioca.");
                    else c.Napomene.Add("U r7 tovarnog lista piše da primalac nema pravo raspolaganja – zahtev pošiljaoca je u skladu sa tim.");
                }

                // 3. deljenje pošiljke
                var deli = Regex.IsMatch(z.Napomene ?? "", @"deo po[sš]iljke|deli[mč]i[cč]n|podel|razdvoj|\bsplit|partial|teilweise|teil(ung|sendung|lieferung)|fraction|partag", RegexOptions.IgnoreCase)
                           || c.Izmene.Where(x => x.rubrika == "10" || x.rubrika == "12").GroupBy(x => x.rubrika).Any(g => g.Select(x => Kljuc(x.nova)).Distinct().Count() > 1)
                           || (z.BrojeviKola.Count > 0 && veza != null && veza.Kola.Count > z.BrojeviKola.Count && z.BrojeviKola.All(veza.Kola.Contains));
                if (deli) c.Upozorenja.Add("Zahtevom se pošiljka deli (izmena za deo kola ili više novih uputnih stanica) – deljenje pošiljke naknadnim zahtevom nije dozvoljeno.");

                // 4. carinsko područje
                var stara = ZemljaUputne(z, veza, false);
                var nova = ZemljaUputne(z, veza, true);
                if (stara != null && nova != null)
                {
                    if (Podrucje(stara) != Podrucje(nova))
                        c.Upozorenja.Add("Izmena menja carinsko područje (" + Naziv(stara) + " → " + Naziv(nova) + ") – potreban pristanak carinarnice."
                            + (z.BezCarine ? " U CIT 7 je označeno „Nije neophodno obavestiti otpravnu carinu“." : ""));
                    else c.Napomene.Add("Carinsko područje se ne menja (" + Naziv(stara) + (stara != nova ? " → " + Naziv(nova) : "") + ").");
                }
                else if (c.Izmene.Any(x => x.rubrika == "4" || x.rubrika == "10" || x.rubrika == "12"))
                    c.Napomene.Add("Nije utvrđeno da li izmena menja carinsko područje – proveriti zemlju nove uputne stanice.");
                if (c.Izmene.Count == 0) c.Upozorenja.Add("Iz CIT 7 nije pročitano šta se menja (šifra " + (z.Sifre.Count > 0 ? string.Join(", ", z.Sifre) : "nije označena") + ") – proveriti ručno.");
                rez.Add(c);
            }
            return rez;
        }

        // ------------------------------------------------------------------------------------------- veza sa listom

        private static (ZapisLista veza, List<string> osnov, int bod) Najbolji(Cit7Zahtev z, List<ZapisLista> kandidati)
        {
            ZapisLista naj = null; List<string> najOsnov = null; var najBod = 0;
            foreach (var k in kandidati)
            {
                var osnov = new List<string>();
                var b = Bodovi(z, k, osnov);
                if (b > najBod || (b == najBod && naj != null && k.Datum > naj.Datum)) { naj = k; najOsnov = osnov; najBod = b; }
            }
            return najBod >= PragVeze ? (naj, najOsnov, najBod) : (null, null, 0);
        }

        private static string Cif(string s) => Regex.Replace(s ?? "", @"\D", "");

        private static int Bodovi(Cit7Zahtev z, ZapisLista k, List<string> osnov)
        {
            var b = 0;
            if (Cif(z.Broj).Length >= 5 && Cif(z.Broj) == Cif(k.Broj) && Cif(z.Stanica).Length >= 5 && Cif(z.Stanica) == Cif(k.Stanica)) { b += 100; osnov.Add("identifikacija r62 " + z.Identifikacija); }
            else if (Cif(z.Stanica).Length >= 5 && Cif(z.Stanica) == Cif(k.Stanica) && Cif(z.Zemlja) == Cif(k.Zemlja)) { b += 30; osnov.Add("otpravna stanica r62 " + z.Zemlja + " " + z.Stanica); }
            var zajednicka = z.BrojeviKola.Intersect(k.Kola).Count();
            if (zajednicka > 0) { b += 60; osnov.Add("brojevi kola (" + zajednicka + ")"); }
            var st = Cif(z.StanicaIzdavanja);
            if (st.Length >= 7 && Cif(k.R12).Length >= 7 && Cif(k.R12).Substring(0, 7) == st.Substring(0, 7)) { b += 25; osnov.Add("uputna stanica r12 " + z.StanicaIzdavanja); }
            if (Slicno(z.MestoIzdavanja, k.R10)) { b += 20; osnov.Add("mesto isporuke r10"); }
            if (Slicno(NazivStrane(z.Primalac), NazivStrane(k.R4))) { b += 15; osnov.Add("primalac r4"); }
            if (Slicno(NazivStrane(z.Posiljalac), NazivStrane(k.R1))) { b += 10; osnov.Add("pošiljalac r1"); }
            if (Slicno(z.Preuzimanje, k.R16)) { b += 10; osnov.Add("mesto preuzimanja r16"); }
            return b;
        }

        private static List<string> Razlike(Cit7Zahtev z, ZapisLista k)
        {
            var r = new List<string>();
            void Uporedi(string naziv, string cit7, string list, string rub)
            {
                if (!string.IsNullOrWhiteSpace(cit7) && !string.IsNullOrWhiteSpace(list) && !Slicno(NazivStrane(cit7), NazivStrane(list)) && !Slicno(cit7, list))
                    r.Add(naziv + ": u CIT 7 „" + Skrati(cit7) + "“, u " + rub + " lista „" + Skrati(list) + "“");
            }
            Uporedi("pošiljalac", z.Posiljalac, k.R1, "r1");
            Uporedi("primalac", z.Primalac, k.R4, "r4");
            Uporedi("mesto preuzimanja", z.Preuzimanje, k.R16, "r16");
            if (z.Identifikacija.Length > 0 && k.Identifikacija.Length > 0 && Cif(z.Identifikacija) != Cif(k.Identifikacija).Substring(0, Math.Min(Cif(k.Identifikacija).Length, Cif(z.Identifikacija).Length)))
                r.Add("identifikacija: u CIT 7 „" + z.Identifikacija + "“, u r62 lista „" + k.Identifikacija + "“");
            else if (z.Identifikacija.Length > 0 && k.Identifikacija.Length == 0)
                r.Add("identifikacija: u CIT 7 „" + z.Identifikacija + "“, r62 lista je prazna – ne može da se uporedi");
            return r;
        }

        private static string Polje(ZapisLista k, string rubrika)
        {
            switch (rubrika)
            {
                case "1": return k?.R1; case "4": return k?.R4; case "7": return k?.R7; case "10": return k?.R10; case "12": return k?.R12; case "16": return k?.R16;
                default: return null;
            }
        }

        /// <summary>Naziv strane: prvi deo upisa do zareza ili novog reda („DELTA AGRAR DOO, Vladimira Popovića 8A, …“ → „DELTA AGRAR DOO“).</summary>
        private static string NazivStrane(string s) => string.IsNullOrWhiteSpace(s) ? s : Regex.Split(s.Trim(), @"\s*[,\n]\s*")[0];

        private static string StaraVrednost(string rubrika, Cit7Zahtev z, ZapisLista k)
        {
            var izLista = Polje(k, rubrika);
            // šifra stanice po kućicama „7 2 1 6 5 0 3 5“ → „72 16503-5“
            if (rubrika == "12" && Cif(izLista).Length == 8) return Cif(izLista).Substring(0, 2) + " " + Cif(izLista).Substring(2, 5) + "-" + Cif(izLista).Substring(7);
            if (!string.IsNullOrWhiteSpace(izLista)) return izLista;
            switch (rubrika)
            {
                case "4": return z.Primalac;
                case "10": return z.MestoIzdavanja;
                case "12": return Regex.Match(z.StanicaIzdavanja ?? "", @"\d{2}\s?\d{5}-?\d").Value;
                case "1": return z.Posiljalac;
                case "16": return z.Preuzimanje;
                default: return "";
            }
        }

        // ------------------------------------------------------------------------------------------- pomoćno

        private static readonly HashSet<string> Opste = new HashSet<string> { "doo", "d", "o", "srl", "s", "r", "l", "gmbh", "kft", "sa", "ag", "ltd", "ad", "zrt", "spa", "sro", "s.r.o", "a", "e", "c", "um", "u", "m" };

        private static string Kljuc(string s) => PraviloKp.Normalizuj(s ?? "");

        /// <summary>Isti subjekt/mesto: bar jedna značajna reč (4+ slova, nije pravni oblik) ili ista šifra stanice.</summary>
        internal static bool Slicno(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
            var ca = Cif(a); var cb = Cif(b);
            if (ca.Length >= 7 && cb.Length >= 7 && ca.Substring(0, 7) == cb.Substring(0, 7)) return true;
            var ra = Kljuc(a).Split(' ').Where(w => w.Length >= 4 && !Opste.Contains(w) && !Regex.IsMatch(w, @"^\d+$") && !Zemlje.Contains(w)).ToList();
            var rb = new HashSet<string>(Kljuc(b).Split(' '));
            return ra.Count > 0 && ra.Take(3).Any(rb.Contains);
        }

        private static readonly HashSet<string> Zemlje = new HashSet<string> { "srbija", "serbia", "serbien", "slovenia", "slovenija", "italy", "italia", "hungary", "romania", "croatia", "hrvatska", "austria", "germany", "deutschland" };

        private static bool PrimalacNemaPravo(string r7) =>
            Regex.IsMatch(Kljuc(r7), @"primalac nema pravo raspolaganja|destinataire n a pas le droit de disposer|empfanger (ist )?nicht verfugungsberechtigt|consignee (has )?no right (of|to) dispos");

        private static string ZemljaUputne(Cit7Zahtev z, ZapisLista k, bool nova)
        {
            string IzSifre(string s) { var c = Cif(s); return c.Length >= 7 && Provere.UicIso.TryGetValue(c.Substring(0, 2), out var iso) ? iso : null; }
            string IzTeksta(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return null;
                foreach (var kv in Provere.IsoPoNazivu) if (Regex.IsMatch(s, @"(?<!\p{L})" + Regex.Escape(kv.Key) + @"(?!\p{L})", RegexOptions.IgnoreCase)) return kv.Value;
                var m = Regex.Match(s, @"(?<![\p{L}])([A-Z]{2})(?![\p{L}])\s*$");
                return m.Success && (Provere.Eu.Contains(m.Groups[1].Value) || Provere.UicIso.ContainsValue(m.Groups[1].Value)) ? m.Groups[1].Value : null;
            }
            if (nova)
            {
                var r12 = z.Izmene.FirstOrDefault(x => x.Rubrika == "12")?.Vrednost;
                var r10 = z.Izmene.FirstOrDefault(x => x.Rubrika == "10")?.Vrednost;
                var r4 = z.Izmene.FirstOrDefault(x => x.Rubrika == "4")?.Vrednost;
                if (r12 == null && r10 == null) return r4 == null ? null : ZemljaUputne(z, k, false); // menja se samo primalac – uputna ostaje
                return IzSifre(r12) ?? IzTeksta(r10) ?? IzTeksta(r12);
            }
            return IzSifre(k?.R12) ?? IzSifre(z.StanicaIzdavanja) ?? IzTeksta(z.StanicaIzdavanja) ?? IzTeksta(k?.R10) ?? IzTeksta(z.MestoIzdavanja);
        }

        private static string Podrucje(string iso) => Provere.Eu.Contains(iso) ? "EU" : iso;
        private static string Naziv(string iso) => Provere.Eu.Contains(iso) ? iso + " (EU)" : iso;
        private static string Skrati(string s) { s = Regex.Replace(s ?? "", @"\s+", " ").Trim(); return s.Length > 80 ? s.Substring(0, 77) + "…" : s; }

        // ------------------------------------------------------------------------------------------- izveštaj (interni, srpski)

        private static string StaSeMenja(string rubrika)
        {
            switch (rubrika)
            {
                case "4": return "primalac";
                case "10": return "mesto isporuke (uputna)";
                case "12": return "šifra uputne stanice";
                case "1": return "pošiljalac";
                case "20": return "plaćanje troškova";
                case "13": return "komercijalni uslovi";
                default: return "rubrika " + rubrika;
            }
        }

        public static string Naslov(IEnumerable<Cit7Rezultat> l, string naslovMaila) =>
            "[INTERNO] CIT 7 – naknadni zahtev – " + Regex.Replace(naslovMaila ?? "", @"\s+", " ").Trim();

        public static string Tekst(List<Cit7Rezultat> lista, PravilaSkup p, string uvod = null)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(uvod)) sb.AppendLine(uvod).AppendLine();
            sb.AppendLine("CIT 7 – NAKNADNI ZAHTEV (interno – ne šalje se pošiljaocu)");
            foreach (var c in lista)
            {
                var z = c.Z;
                sb.AppendLine();
                sb.AppendLine("Fajl: " + Path.GetFileName(c.Fajl));
                sb.AppendLine("Zahtev za " + (z.Cuv ? "kolski list CUV" : "tovarni list CIM") + (z.Identifikacija.Length > 0 ? " " + z.Identifikacija : "")
                              + (z.Broj.Length == 0 && z.Identifikacija.Length > 0 ? " (broj otpravljanja nije upisan)" : "")
                              + (z.Sifre.Count > 0 ? "; šifra zahteva " + string.Join(", ", z.Sifre) : ""));
                sb.AppendLine("Podnosilac: " + c.Podnosilac + (z.Datum.Length > 0 ? ", " + z.Datum : ""));
                sb.AppendLine("Tovarni list: " + (c.Veza != null ? c.Veza.Opis + " – veza po: " + string.Join(", ", c.Osnov) : "NIJE PRONAĐEN (ni u ovom mailu ni među ranije obrađenim listovima) – proveriti ručno"));
                foreach (var x in c.Razlike) sb.AppendLine("  razlika – " + x);
                if (c.Razlike.Count > 0) sb.AppendLine("  → proveriti da li se CIT 7 odnosi na ovaj list.");
                if (c.Izmene.Count > 0)
                {
                    sb.AppendLine("Šta se menja: " + string.Join(", ", c.Izmene.Select(x => StaSeMenja(x.rubrika)).Distinct()));
                    foreach (var x in c.Izmene)
                        sb.AppendLine("  Rubrika " + Izvestaj.RubrikaKolona(p, x.rubrika, false) + ": " + (string.IsNullOrWhiteSpace(x.stara) ? "(nije poznato)" : x.stara) + "  →  " + x.nova);
                }
                if (c.Upozorenja.Count > 0)
                {
                    sb.AppendLine("Upozorenja:");
                    for (int i = 0; i < c.Upozorenja.Count; i++) sb.AppendLine("  " + (i + 1) + ". " + c.Upozorenja[i]);
                }
                foreach (var n in c.Napomene) sb.AppendLine("Napomena: " + n);
            }
            return sb.ToString();
        }

        public static string Html(List<Cit7Rezultat> lista, PravilaSkup p, string uvod = null)
        {
            string E(string s) => WebUtility.HtmlEncode(s ?? "");
            var sb = new StringBuilder("<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:13px\">");
            if (!string.IsNullOrEmpty(uvod)) sb.Append("<p>" + E(uvod).Replace("\n", "<br>") + "</p>");
            sb.Append("<h3 style=\"margin:8px 0\">CIT 7 – naknadni zahtev <span style=\"font-weight:normal;color:#666\">(interno – ne šalje se pošiljaocu)</span></h3>");
            foreach (var c in lista)
            {
                var z = c.Z;
                sb.Append("<p><b>" + E(Path.GetFileName(c.Fajl)) + "</b><br>Zahtev za " + E((z.Cuv ? "kolski list CUV" : "tovarni list CIM") + (z.Identifikacija.Length > 0 ? " " + z.Identifikacija : "")
                    + (z.Broj.Length == 0 && z.Identifikacija.Length > 0 ? " (broj otpravljanja nije upisan)" : "")) + "<br>Podnosilac: " + E(c.Podnosilac + (z.Datum.Length > 0 ? ", " + z.Datum : ""))
                    + "<br>Tovarni list: " + (c.Veza != null ? E(c.Veza.Opis) + " – veza po: " + E(string.Join(", ", c.Osnov)) : "<b style=\"color:#b00020\">nije pronađen – proveriti ručno</b>") + "</p>");
                if (c.Razlike.Count > 0)
                    sb.Append("<p style=\"color:#8a5a00\">Razlike između CIT 7 i lista:<br>" + string.Join("<br>", c.Razlike.Select(x => "– " + E(x))) + "<br>→ proveriti da li se CIT 7 odnosi na ovaj list.</p>");
                if (c.Izmene.Count > 0)
                {
                    sb.Append("<table style=\"border-collapse:collapse;font-size:13px\"><tr style=\"background:#d9d9d9\"><th style=\"border:1px solid #999;padding:3px 6px\">Rubrika</th><th style=\"border:1px solid #999;padding:3px 6px\">Sada (stara vrednost)</th><th style=\"border:1px solid #999;padding:3px 6px\">Nova vrednost</th></tr>");
                    foreach (var x in c.Izmene)
                        sb.Append("<tr><td style=\"border:1px solid #999;padding:3px 6px\">" + E("Rubrika " + Izvestaj.RubrikaKolona(p, x.rubrika, false)) + "</td><td style=\"border:1px solid #999;padding:3px 6px\">" + E(string.IsNullOrWhiteSpace(x.stara) ? "(nije poznato)" : x.stara)
                                  + "</td><td style=\"border:1px solid #999;padding:3px 6px\"><b>" + E(x.nova) + "</b></td></tr>");
                    sb.Append("</table>");
                }
                if (c.Upozorenja.Count > 0) sb.Append("<p style=\"color:#b06000\"><b>Upozorenja</b></p><ol>" + string.Join("", c.Upozorenja.Select(x => "<li>" + E(x) + "</li>")) + "</ol>");
                foreach (var n in c.Napomene) sb.Append("<p style=\"color:#555\">Napomena: " + E(n) + "</p>");
            }
            return sb.Append("</div>").ToString();
        }
    }
}
