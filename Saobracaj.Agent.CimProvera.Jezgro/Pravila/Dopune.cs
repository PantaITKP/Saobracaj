using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Saobracaj.Agent.CimProvera.Model;

namespace Saobracaj.Agent.CimProvera.Pravila
{
    /// <summary>
    /// v2.8.1 – šta se u PDF primeru dopisuje u rubriku sa nalazom. Pravilo: kako rubrika STVARNO treba da izgleda – ono što je
    /// upisano ostaje, crvenom se dopunjuje samo ono što nedostaje.
    /// <list type="bullet">
    /// <item>Kada se vrednost može odrediti iz dokumenata – prava vrednost: zemlja iz mesta / r10 / r12 / r16 („Novi Sad“ → RS,
    /// r12 = 54 → CZ), datum iz r29 ili iz datuma maila, NHM sa 6 cifara iz JCI/T1, mesto iz r16, prevoznik iz r62/r57, KP 3212.</item>
    /// <item>Kada ne može (kontakt osoba, telefon, sat preuzimanja, masa) – realan primer u istom obliku („Marko Marković“,
    /// „11-13-08“), nikad opis („naziv, ulica i broj…“, „&lt;šifra&gt;“, „npr.“).</item>
    /// <item>Pogrešna vrednost – <see cref="Nalaz.Pogresno"/> se precrtava, a ispravna se upisuje pored.</item>
    /// <item>Napomene (narandžasto) – dopuna samo kada je predlog siguran (<see cref="Nalaz.DopunaSigurna"/>).</item>
    /// </list>
    /// </summary>
    /// <summary>
    /// v2.8.2: šabloni brojeva propisanog oblika – kada vrednost nije poznata iz dokumenata, upisuje se oblik polja sa X,
    /// nikad izmišljen broj (izmišljen broj može biti pravi broj drugog lista).
    /// </summary>
    internal static class Sablon
    {
        public const string BrojOtpravljanja = "XXXXX-X";
        public const string Stanica = "XX XXXXX-X";
        public const string StanicaBezZemlje = "XXXXX-X";
        public const string Zemlja = "XX";
        public const string Preduzece = "XXXX";
        public const string BrojKola = "XX XX XXXX XXX-X";
        public const string Mrn = "26RSXXXXXXXXXXXXX";
        public const string Cas = "XX";
        public const string Masa = "X";
        /// <summary>v2.8.3: naziv firme, platioca, primaoca, mesta – nikad izmišljen.</summary>
        public const string Naziv = "XXXXX";
        public const string Ulica = "ulica i broj";
        public const string UlicaEn = "street and number";
        public const string PostanskiMesto = "poštanski broj mesto";
        public const string MestoEn = "postcode town";
        public const string Nhm = "XXXXXX";
        /// <summary>v2.9.2 (3.5): kontakt osoba – nikad izmišljeno ime.</summary>
        public const string Kontakt = "XXXX";
    }

    internal static class Dopune
    {
        public static void Popuni(Nalaz n, Pogodak pg, Kontekst k)
        {
            n.Dopuna = ""; n.Pogresno = ""; n.DopunaSigurna = false; n.DopunaRedovi = null; n.DopunaUzOznake = false; n.DopunaPoPoljima = null; n.DopunaIspod = false; n.DopunaUz = null;
            if (n.SamoInterno || n.Tezina == Tezina.Info) return;
            // v2.8.3: na skenu se ništa ne dopisuje u rubriku koja nije pouzdano pročitana (nečitljiva, ispod praga) ni u „praznu“ –
            // agent ne zna šta stvarno piše (TCDD r4: OCR nije pročitao primaoca); ostaje samo okvir sa brojem
            if (k.Ocr && Motor.RubrikaLista(k.C, n.Rubrika) is Rubrika rub
                && (rub.Status == StatusRubrike.Necitljivo || !rub.Popunjena || rub.Pouzdanost < k.U.Prag || (k.C.PouzdanostStrane > 0 && k.C.PouzdanostStrane < Motor.PragSkena)))
                return;
            string d, p; bool s;
            if (pg.Dopuna != null) { d = pg.Dopuna; p = pg.Pogresno; s = pg.DopunaSigurna; }
            else (d, p, s) = Odredi(n, pg, k);
            d = Ocisti(d);
            n.Dopuna = d;
            n.Pogresno = d.Length > 0 ? (p ?? "").Trim() : "";
            n.DopunaSigurna = d.Length > 0 && s;
            if (d.Length == 0) n.DopunaRedovi = null;
        }

        /// <summary>Reči i znaci opisa formata – takva dopuna se ne upisuje (sigurnosna mreža; v2.8.1 test „bez opisa“).</summary>
        internal static readonly Regex OpisFormata = new Regex(
            @"<|>|\bnpr\.|\be\.g\.|naziv, ulica|name, street|po[sš]tanski broj i mesto|postcode and place|godina-mesec-dan|year-month-day|mesec-dan-[cč]as|month-day-hour|\bupisati\b|\bunesite\b|\benter\b|\bformat\b|\bsee (description|wagon list)|\bvidi opis|…|\.\.\.",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal static string Ocisti(string d)
        {
            d = Regex.Replace((d ?? "").Replace("\n", ", "), @"\s+", " ").Trim(' ', ',', ';');
            // v2.8.4 (7): dopuna sa šumom OCR-a („en jour—heure Nord j“, „jj 20“) se ne upisuje
            return d.Length == 0 || OpisFormata.IsMatch(d) || SumOcr(d) ? "" : d;
        }

        /// <summary>Kratke reči (1–2 slova, mala slova) koje su prave reči ili oznake – sve ostale takve reči su šum OCR-a.</summary>
        private static readonly HashSet<string> KratkeReci = new HashSet<string>(StringComparer.Ordinal)
        {
            "i", "u", "a", "o", "e", "y", "s", "z", "na", "za", "od", "do", "se", "je", "br", "ul", "bb", "uz", "iz", "po", "sa", "ka", "ko", "li",
            "of", "in", "on", "to", "at", "by", "is", "it", "no", "nr", "st", "kg", "km", "an", "as", "or", "de", "la", "le", "du", "et", "en", "au",
            "am", "im", "um", "zu", "ab", "al", "il", "el", "di", "da", "ag", "sa", "kp", "ok", "ex", "vi", "ii", "hr", "rs", "hu", "mm", "dd",
        };

        /// <summary>
        /// v2.8.4 (7): tekst je šum OCR-a: reč sa mešanim pismom (latinica i ćirilica u istoj reči), niz „jj“, slovo između cifara
        /// („044j202j6“), ili reč od 1–2 mala slova koje nema u rečniku kratkih reči („j“, „ı“, „ij“). Velika slova od 2 znaka
        /// (oznake zemalja, pokrajina „(VI)“) nisu šum.
        /// </summary>
        internal static bool SumOcr(string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return false;
            foreach (var tok in Regex.Split(t, @"[\s/,;:()\[\]„“""'—–\-]+").Where(x => x.Length > 0))
            {
                if (Regex.IsMatch(tok, @"\p{IsCyrillic}") && Regex.IsMatch(tok, @"[A-Za-zčćžšđČĆŽŠĐ]")) return true;
                if (tok.IndexOf("jj", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (Regex.IsMatch(tok, @"\d[a-zıj]\d|ı")) return true;
                var slova = Regex.Replace(tok, @"[^\p{L}]", "");
                if (slova.Length == tok.Trim('.').Length && slova.Length >= 1 && slova.Length <= 2 && slova != slova.ToUpperInvariant() && !KratkeReci.Contains(slova.ToLowerInvariant())) return true;
            }
            return false;
        }

        private static string Cif(string s) => Regex.Replace(s ?? "", @"\D", "");
        private static bool Prazno(string s) => string.IsNullOrWhiteSpace(s);

        private static (string dopuna, string pogresno, bool sigurna) Odredi(Nalaz n, Pogodak pg, Kontekst k)
        {
            var en = k.U.Engleski;
            // samo konkretan nalaz („Nedostaje: zemlja, kontakt.“), ne opšta poruka pravila (ona nabraja sve delove rubrike)
            var det = n.Detalj ?? "";
            bool Fali(params string[] reci) => reci.Any(r => det.IndexOf(r, StringComparison.OrdinalIgnoreCase) >= 0);
            var prazna = Regex.IsMatch(det, @"rubrika je prazna|box is empty", RegexOptions.IgnoreCase);
            var greska = n.Tezina == Tezina.Greska;
            switch (n.Pravilo)
            {
                case "R1":
                case "R4":
                {
                    var broj = n.Pravilo == "R1" ? "1" : "4";
                    var st = broj == "1" ? k.C.R1 : k.C.R4;
                    var delovi = new List<string>();
                    // v2.8.3: naziv, adresa, platilac i primalac se NIKAD ne izmišljaju – nepoznato je šablon; zemlja iz adrese, pa iz
                    // stanice te strane (r1 → r16/r17, r4 → r10/r12), nikad iz druge strane, r62 ni podrazumevano – nepoznata „XX“
                    var iso = Zemlja(k, broj, st);
                    if (prazna || Fali("naziv", "name")) delovi.Add(Sablon.Naziv);
                    if (prazna || Fali("adresa", "address")) { delovi.Add(en ? Sablon.UlicaEn : Sablon.Ulica); delovi.Add(en ? Sablon.MestoEn : Sablon.PostanskiMesto); }
                    else if (Fali("poštanski broj i mesto", "postcode and place")) delovi.Add(en ? Sablon.MestoEn : Sablon.PostanskiMesto);
                    if (prazna || Fali("zemlja", "country")) delovi.Add(iso ?? Sablon.Zemlja);
                    // kontakt pošiljaoca (primer GIPS r1: „RS, Marko Marković“) – kada u rubrici nema telefona ni e-maila
                    // v2.9.2 (3.5): ime kontakt osobe se ne izmišlja („Marko Marković“, „John Smith“) – šablon „XXXX“
                    if (broj == "1" && Prazno(st.Telefon) && Prazno(st.Email)) delovi.Add(Sablon.Kontakt);
                    return (string.Join(", ", delovi.Where(x => !Prazno(x))), null, false);
                }
                case "R1-ISO":
                case "R4-ISO":
                {
                    var iso = Regex.Match(pg.Predlog ?? "", @"^[A-Z]{2}\b").Value;
                    var rec = pg.V.TryGetValue("country", out var c) ? c : null;
                    // zemlja u nazivu firme („Louis Dreyfus Company Hungary Kft.“) se ne precrtava – samo se dopisuje ISO šifra
                    var st = n.Pravilo == "R1-ISO" ? k.C.R1 : k.C.R4;
                    if (rec != null && Regex.IsMatch(st.Tekst ?? "", Regex.Escape(rec) + @"\s+(Kft|Zrt|d\.?\s?o\.?\s?o|GmbH|Ltd|Limited|s\.?r\.?o|S\.?A|SRL|S\.?p\.?A|AG|Inc|a\.?s|a\.?d)\b", RegexOptions.IgnoreCase)) rec = null;
                    return iso.Length == 2 ? (iso, rec, true) : ("", null, false);
                }
                case "R16":
                {
                    var delovi = new List<string>();
                    if (prazna || Fali("mesto/stanica", "place/station"))
                        delovi.Add(Mesto(PouzdanoMesto(k.C.R29, k)) ?? Sablon.Naziv);   // mesto iz r29 tog lista (v2.8.4: samo pouzdano pročitano), inače šablon (nikad izmišljeno)
                    var bezDatuma = prazna || Fali("datum", "date");
                    var bezCasa = prazna || Fali("vreme (čas)", "time (hour)");
                    // v2.8.2: datum SAMO iz dokumenta (r29, priloženi spisak kola), nikad datum maila; nepoznato – „MM-DD“, sat „XX“
                    var (_, mesec, dan) = DatumIzDokumenta(PouzdanDatum(k.C.R29, k), k);
                    var md = mesec != null ? mesec + "-" + dan : "MM-DD";
                    if (bezDatuma) delovi.Add(md + (bezCasa ? "-" + Sablon.Cas : ""));
                    else if (bezCasa) delovi.Add(Sablon.Cas);
                    return (string.Join(", ", delovi), null, false);
                }
                case "R29":
                {
                    if (n.Detalj != null && Regex.IsMatch(n.Detalj, "budućnosti|future")) return ("", null, false);
                    var delovi = new List<string>();
                    if (prazna || Fali("mesto", "place")) delovi.Add(Mesto(PouzdanoMesto(k.C.R16, k)) ?? Sablon.Naziv);
                    if (prazna || Fali("datum ispostavljanja", "date of issue"))
                    {
                        // v2.8.2: datum SAMO iz dokumenta (r16, priloženi spisak kola); nepoznat deo – X („XXXX-XX-XX“, „XXXX-11-13“)
                        var (god, mes, dan) = DatumIzDokumenta(PouzdanDatum(k.C.R16, k), k);
                        delovi.Add((god ?? "XXXX") + "-" + (mes ?? "XX") + "-" + (dan ?? "XX"));
                    }
                    return (string.Join(", ", delovi), null, false);
                }
                case "R62":
                {
                    var r = k.C.R62;
                    var delovi = new List<string>();
                    var bez = prazna;
                    var zemlja = !Prazno(r.Zemlja) ? null : UicZemlje(k);
                    // v2.8.2: brojevi propisanog oblika se ne izmišljaju – prava vrednost iz lista (r16, r58a) ili šablon sa X
                    if (bez || Fali("zemlja", "country")) delovi.Add(zemlja ?? Sablon.Zemlja);
                    if (bez || Fali("stanica", "station")) delovi.Add(Stanica(k.C.R16.SifraStanice) ?? Sablon.StanicaBezZemlje);
                    if (bez || Fali("šifra preduzeća", "company code")) delovi.Add(Cif(k.C.R58a.Sifra).Length == 4 ? Cif(k.C.R58a.Sifra) : k.P.Kp.JeKp(k.C.R58a.Tekst) ? k.P.Kp.Uic : Sablon.Preduzece);
                    var losBroj = Fali("u obliku 12345-6", "in the format 12345-6");
                    if (bez || Fali("broj otpravljanja", "consignment number")) delovi.Add(Sablon.BrojOtpravljanja);
                    // v2.8.2: svaki deo u svoje polje obrasca (62.1 zemlja, 62.2 stanica, 62.3 preduzeće, 62.4 broj otpravljanja)
                    n.DopunaPoPoljima = new Dictionary<string, string>();
                    if (bez || Fali("zemlja", "country")) n.DopunaPoPoljima["62.1"] = zemlja ?? Sablon.Zemlja;
                    if (bez || Fali("stanica", "station")) n.DopunaPoPoljima["62.2"] = Stanica(k.C.R16.SifraStanice) ?? Sablon.StanicaBezZemlje;
                    if (bez || Fali("šifra preduzeća", "company code")) n.DopunaPoPoljima["62.3"] = Cif(k.C.R58a.Sifra).Length == 4 ? Cif(k.C.R58a.Sifra) : k.P.Kp.JeKp(k.C.R58a.Tekst) ? k.P.Kp.Uic : Sablon.Preduzece;
                    if (bez || Fali("broj otpravljanja", "consignment number")) n.DopunaPoPoljima["62.4"] = Sablon.BrojOtpravljanja;
                    return (string.Join(" ", delovi), losBroj ? r.Broj : null, false);
                }
                case "R58A":
                {
                    // ugovorni prevoznik = preduzeće izdavaoca iz r62; naziv iz dokumenta (r57/r13/r7, ista šifra).
                    // v3.0 (2.9.3 tačka 3): bez pouzdane šifre u r62 nema predloga (nikad prvi prevoznik iz r57, nikad 3212 za tuđi list)
                    var sifra = Provere.IzdavalacR62(k);
                    if (sifra == null) return ("", null, false);
                    return ((sifra + " " + (Provere.NazivPrevoznika(k, sifra) ?? "")).Trim(), null, false);
                }
                case "KP2":
                {
                    var r = k.C.R58a;
                    var pogresno = r.Popunjena && !Prazno(r.Sifra) && !k.P.Kp.JeKpSifra(r.Sifra) ? r.Sifra : null;
                    return (k.P.Kp.Uic + " " + k.P.Kp.Naziv, pogresno, false);
                }
                case "KP3":
                    return (k.P.Kp.Uic + " KP", null, false);
                case "KP57":
                    // v2.9.2 (2): KP vozi samo unutar Srbije – dopuna u novom redu, uz srpsku deonicu ako je ima; nikad pored strane deonice
                    n.DopunaIspod = true;
                    n.DopunaUz = Provere.SrpskaDeonicaR57(k.C.R57);
                    return (k.P.Kp.Uic + " KP", null, false);
                case "KP7-PLATILAC":
                    return (en ? "Payer for SRB: " + Sablon.Naziv : "Platilac za deonicu SRB: " + Sablon.Naziv, null, false);
                case "R30":
                    return ("X", null, false);
                case "R9":
                {
                    var potrebni = k.Svi.Where(d => TipDokumenta.JeCarinski(d.Tip) || d.Tip == TipDokumenta.SpisakKola || d.Tip == TipDokumenta.DodatniList)
                        .Select(d => d.Tip).Distinct().Select(t => (Provere.SifraPriloga(t, en) is var s && Regex.IsMatch(s ?? "", @"^\d{3}$") ? s + " " : "") + Kratko(t, en));
                    return (string.Join("; ", potrebni), null, false);
                }
                case "G3":
                case "X9":
                case "X20":
                    return (Kratko(TipDokumenta.SpisakKola, en), null, false);
                case "X22":
                {
                    var tip = new[] { TipDokumenta.IzvoznaDeklaracija, TipDokumenta.TranzitnaDeklaracija, TipDokumenta.DrugaCarinska }
                        .FirstOrDefault(t => (n.Kljuc ?? "").Contains("r9-" + t) || (pg.Kljuc ?? "").Contains("r9-" + t));
                    if (tip == null) return ("", null, false);
                    var sif = Provere.SifraPriloga(tip, en);
                    var mrn = k.Svi.Where(d => d.Tip == tip).SelectMany(d => d.Carinski).SelectMany(c => c.Mrn).FirstOrDefault();
                    var br = k.Svi.Where(d => d.Tip == tip).SelectMany(d => d.Carinski).Select(c => c.BrojDeklaracije).FirstOrDefault(x => !Prazno(x));
                    return (((Regex.IsMatch(sif ?? "", @"^\d{3}$") ? sif + " " : "") + Kratko(tip, en) + (mrn != null ? " " + mrn : br != null ? " " + br : "")).Trim(), null, false);
                }
                case "X23":
                {
                    var tipovi = new[] { TipDokumenta.Eur1, TipDokumenta.UverenjeOPoreklu, TipDokumenta.Sertifikat, TipDokumenta.Faktura }.Where(t => k.Tip(t).Any()).ToList();
                    return (string.Join("; ", tipovi.Select(t => (Provere.SifraPriloga(t, en) is var s && Regex.IsMatch(s ?? "", @"^\d{3}$") ? s + " " : "") + Kratko(t, en))), null, true);
                }
                case "R24":
                    return (Regex.IsMatch(pg.Predlog ?? "", @"^\d{6}$") ? pg.Predlog : "", null, true);
                case "R24-NHM6":
                {
                    // „2520“ → precrtano, pored „252010“ (sigurno samo kada je šifra iz T1/JCI/spiska ili prazna kola 9922 → 992200)
                    var upisano = Regex.Match(n.Detalj ?? "", @"\d{4,5}").Value;
                    return Regex.IsMatch(pg.Predlog ?? "", @"^\d{6}$") ? (pg.Predlog, upisano, true) : ("", null, false);
                }
                case "R21-MRN":
                {
                    var mrn = Regex.Match(pg.Predlog ?? "", @"\b\d{2}[A-Z]{2}[A-Z0-9]{14}\b").Value;
                    return mrn.Length > 0 ? ((n.Primer ?? "").StartsWith("T", StringComparison.Ordinal) ? "T MRN " + mrn : "E MRN " + mrn, null, true) : ("", null, false);
                }
                case "R25-MASA":
                {
                    // v2.8.2: mase sa priloženog spiska kola kada se mogu izračunati, inače šablon „X kg“; u redove koje obrazac ima
                    // („T= / N= / G=“, „Netto: / Tare: / Brutto:“), inače „Tara: X kg, Neto: X kg, Bruto: X kg“
                    double? Suma(Func<SpisakKola, double?> f) { var v = k.Spiskovi.Select(x => f(x.s)).Where(x => x.HasValue).ToList(); return v.Count == 0 ? (double?)null : v.Sum(x => x.Value); }
                    double? tara = Suma(s => s.TaraKg), neto = Suma(s => s.NetoKg), bruto = Suma(s => s.BrutoKg);
                    if (!bruto.HasValue && tara.HasValue && neto.HasValue) bruto = tara + neto;
                    if (!neto.HasValue && tara.HasValue && bruto.HasValue && bruto > tara) neto = bruto - tara;
                    var poznato = tara.HasValue || neto.HasValue || bruto.HasValue;
                    if (!greska && !poznato) return ("", null, false);   // preporuka bez poznate mase: samo oznaka
                    string Kg(double? v) => (v.HasValue ? v.Value.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ") : Sablon.Masa) + " kg";   // hiljade razmakom („703 000 kg“)
                    var vrednost = new Dictionary<string, string> { ["tara"] = Kg(tara), ["neto"] = Kg(neto), ["bruto"] = Kg(bruto) };
                    var naziv = new Dictionary<string, string> { ["tara"] = en ? "Tare" : "Tara", ["neto"] = en ? "Net" : "Neto", ["bruto"] = en ? "Gross" : "Bruto" };
                    var redovi = OznakeMase(k.C.R25.Tekst);
                    if (redovi.Count == 0 && k.C.ReciRubrika != null && k.C.ReciRubrika.TryGetValue("25", out var upisane))
                        redovi = OznakeMase(string.Join("\n", Citanje.Raspored.Linije(upisane).Select(l => l.Tekst)));
                    // prazna rubrika: redovi koje obrazac ima (štampano „T= / N= / G=“)
                    if (redovi.Count == 0 && k.C.NatpisiRubrika != null && k.C.NatpisiRubrika.TryGetValue("25", out var natpisi))
                        redovi = OznakeMase(string.Join("\n", Citanje.Raspored.Linije(natpisi).Select(l => l.Tekst)));
                    if (redovi.Count > 0) { n.DopunaRedovi = redovi.Select(x => vrednost[x]).ToList(); n.DopunaUzOznake = true; }
                    else { redovi = new List<string> { "tara", "neto", "bruto" }; n.DopunaRedovi = redovi.Select(x => naziv[x] + ": " + vrednost[x]).ToList(); n.DopunaUzOznake = false; }
                    return (string.Join(", ", redovi.Select(x => naziv[x] + ": " + vrednost[x])), null, !greska && poznato);
                }
                default:
                    // predlog pravila iz pratećih dokumenata ili istog lista – samo kada je to vrednost, a ne opis
                    if (greska && !Prazno(pg.Predlog)) return (pg.Predlog, null, false);
                    return ("", null, false);
            }
        }

        /// <summary>Redovi oznaka mase koje obrazac/pošiljalac ima u r25, redom: „T= …“ → tara, „N= …“ → neto, „G= …“ / „Brutto:“ → bruto.</summary>
        internal static List<string> OznakeMase(string tekst)
        {
            var rez = new List<string>();
            foreach (var red in (tekst ?? "").Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0))
            {
                var m = Regex.Match(red, @"^(tara|tare|taragewicht|t)\b|^(neto|netto|net|n)\b|^(bruto|brutto|gross|g|b)\b", RegexOptions.IgnoreCase);
                if (!m.Success) continue;
                var vrsta = m.Groups[1].Success ? "tara" : m.Groups[2].Success ? "neto" : "bruto";
                if (!rez.Contains(vrsta)) rez.Add(vrsta);
            }
            return rez;
        }

        // ------------------------------------------------------------------------------------------- kontrola pre slanja

        /// <summary>
        /// v2.9.2 (3.5): reči koje PDF primer sme da dopiše, a nisu iz dokumenta – oznake zemalja, šabloni, oznake masa, nazivi
        /// dokumenata u r9, „Platilac za deonicu SRB“, KP. Sve ostale reči dopune moraju biti iz dokumenata maila.
        /// </summary>
        private static readonly HashSet<string> RecnikPrimera = new HashSet<string>(StringComparer.Ordinal)
        {
            "platilac", "za", "deonicu", "deonica", "srb", "payer", "for", "ulica", "i", "broj", "postanski", "mesto", "street", "and", "number", "postcode",
            "town", "kp", "kombinovani", "prevoz", "doo", "d", "o", "uic", "tara", "neto", "bruto", "netto", "tare", "brutto", "gross", "net", "kg", "t", "n", "g",
            "mrn", "e", "odobrenje", "br", "approval", "no", "spisak", "kola", "wagon", "list", "faktura", "invoice", "izvozna", "deklaracija", "export", "declaration",
            "carinski", "dokument", "customs", "document", "uverenje", "o", "poreklu", "certificate", "of", "origin", "sertifikat", "dodatni", "additional", "sheet",
            "eur", "x", "cim", "cuv", "cit", "nhm", "un", "rid", "adr", "svi", "prevoznici", "all", "carriers", "franko", "nefranko", "dap", "ddp", "fca", "exw", "cpt", "cip",
            "kombinovani", "prevoz", "d.o.o",
        };

        /// <summary>v2.9.2 (3.5): reči dopune (sa slovima) koje nisu iz dokumenata maila, X šablon ni iz rečnika primera; prazno = dopuna je ispravna.</summary>
        internal static List<string> NepoznateReci(Nalaz n, Kontekst k)
        {
            var tekst = n.DopunaPoPoljima != null && n.DopunaPoPoljima.Count > 0 ? string.Join(" ", n.DopunaPoPoljima.Values)
                : n.DopunaRedovi != null ? string.Join(" ", n.DopunaRedovi) : n.Dopuna ?? "";
            var rez = new List<string>();
            if (tekst.Length == 0) return rez;
            // navod izvora u zagradi („(indeksni brojevi iz dozvole, ruma.pdf str. 8, 9)“, „(u T1: 262190)“) nije dopisan podatak lista
            tekst = Regex.Replace(tekst, @"\([^()]*(?:\.\w{3,4}\b|\bstr\.|\bpage\b|\bu T1\b|\biz\b)[^()]*\)", " ");
            var dok = ReciDokumenata(k);
            foreach (Match m in Regex.Matches(tekst, @"[\p{L}\d][\p{L}\d]*"))
            {
                var w = m.Value;
                if (!Regex.IsMatch(w, @"\p{L}")) continue;                       // brojevi (mase, datumi, šifre) – oblik proverava pravilo
                if (Regex.IsMatch(w, @"^(?:\d*[A-Z]{0,2}\d*X+\d*|X+)$")) continue;    // šablon sa X („XXXXX“, „26RSXXXX…“, „XX“)
                var nw = Norm(w);
                if (nw.Length == 0 || RecnikPrimera.Contains(nw) || Regex.IsMatch(w, @"^[A-Z]{2}$") || dok.Contains(nw)) continue;
                rez.Add(w);
            }
            return rez.Distinct().ToList();
        }

        private static string Norm(string s) => Regex.Replace(Saobracaj.Common.Text.TekstUtil.BezDijakritika(s ?? "").ToLowerInvariant(), @"[^a-z0-9]", "");

        /// <summary>Sve reči iz pročitanih dokumenata maila (rubrike, spiskovi, carinski…), normalizovane.</summary>
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Kontekst, HashSet<string>> reciKonteksta = new System.Runtime.CompilerServices.ConditionalWeakTable<Kontekst, HashSet<string>>();

        private static HashSet<string> ReciDokumenata(Kontekst k) => reciKonteksta.GetValue(k, ReciDokumenata0);

        private static HashSet<string> ReciDokumenata0(Kontekst k)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(k.SviSaDosijeom, new Newtonsoft.Json.JsonSerializerSettings { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore });
            var s = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in Regex.Matches(json, @"[\p{L}\d]+")) s.Add(Norm(m.Value));
            foreach (var w in Regex.Matches(k.P.Kp.Naziv ?? "", @"\p{L}+").Cast<Match>()) s.Add(Norm(w.Value));
            return s;
        }

        // ------------------------------------------------------------------------------------------- vrednosti iz lista

        private static string Kratko(string tip, bool en)
        {
            switch (tip)
            {
                case TipDokumenta.SpisakKola: return en ? "Wagon list" : "Spisak kola";
                case TipDokumenta.IzvoznaDeklaracija: return en ? "Export declaration" : "Izvozna deklaracija";
                case TipDokumenta.TranzitnaDeklaracija: return "T1";
                case TipDokumenta.DrugaCarinska: return en ? "Customs document" : "Carinski dokument";
                case TipDokumenta.Faktura: return en ? "Invoice" : "Faktura";
                case TipDokumenta.Eur1: return "EUR.1";
                case TipDokumenta.UverenjeOPoreklu: return en ? "Certificate of origin" : "Uverenje o poreklu";
                case TipDokumenta.Sertifikat: return en ? "Certificate" : "Sertifikat";
                case TipDokumenta.DodatniList: return en ? "Additional sheet" : "Dodatni list";
                default: return Provere.NazivTipa(tip, en);
            }
        }

        /// <summary>ISO šifra zemlje strane: iz same rubrike (naziv zemlje), iz lista (r1 → r16/r17, r4 → r10/r12), iz poštanskog broja i mesta.</summary>
        private static string Zemlja(Kontekst k, string broj, Model.Strana st)
        {
            // 1. iz adrese: naziv zemlje u adresi (ne u nazivu firme), mesto, poštanski broj sa oznakom zemlje („D-44653“)
            if (Provere.ZemljaUAdresi(st) && Zemlje.Iso(st.NazivZemlje) is string iz) return iz;
            var adresa = Provere.AdresaBezNaziva(st);
            var izAdrese = ZemljaIzAdrese(adresa);
            if (izAdrese != null) return izAdrese;
            // 2. iz šifre zemlje stanice TE strane (r1 → r16/r17, r4 → r10/r12); nikad druga strana, r62 ni podrazumevano
            return Provere.ZemljaLista(k, broj);
        }

        /// <summary>
        /// v2.8.3: ISO zemlja iz teksta adrese – poštanski broj sa oznakom zemlje („D-44653“, „H-6200“, „TR-06420“) ili poznato mesto
        /// („44653 Herne“ → DE, „Kiskőrös“ → HU, „Ankara“ → TR). null kada se ne može odrediti.
        /// </summary>
        internal static string ZemljaIzAdrese(string tekst)
        {
            if (string.IsNullOrWhiteSpace(tekst)) return null;
            var p = Regex.Match(tekst, @"(?<![\p{L}\d])(D|DE|A|AT|H|HU|CZ|SK|PL|I|IT|RO|BG|TR|SLO|SI|HR|SRB|RS|CH|NL|B|F)\s?-\s?\d{3,6}\b");
            if (p.Success)
            {
                switch (p.Groups[1].Value)
                {
                    case "D": case "DE": return "DE"; case "A": case "AT": return "AT"; case "H": case "HU": return "HU"; case "I": case "IT": return "IT";
                    case "SLO": case "SI": return "SI"; case "SRB": case "RS": return "RS"; case "B": return "BE"; case "F": return "FR";
                    default: return p.Groups[1].Value;
                }
            }
            var norm = " " + Regex.Replace(Saobracaj.Common.Text.TekstUtil.BezDijakritika(tekst).ToLowerInvariant(), @"[^a-z0-9]+", " ") + " ";
            foreach (var kv in Gradovi.OrderByDescending(x => x.Key.Length))
                if (norm.Contains(" " + kv.Key + " ")) return kv.Value;
            // v2.9.2 (3.4): poljski poštanski broj „41-800“ (jedini oblik NN-NNN u Evropi) i PDV broj sa oznakom zemlje („PL6292256576“)
            if (Regex.IsMatch(tekst, @"(?<![\d\-/.])\d{2}-\d{3}(?![\d\-/.])")) return "PL";
            var pdv = Regex.Match(tekst, @"(?<![A-Z])(PL|SK|CZ|HU|DE|AT|RO|BG|HR|SI|IT|TR|GR|NL|BE|FR|ATU)\s?(\d{8,12})(?!\d)");
            if (pdv.Success) return pdv.Groups[1].Value == "ATU" ? "AT" : pdv.Groups[1].Value;
            return null;
        }

        /// <summary>Gradovi i stanice (bez dijakritika, mala slova) → ISO zemlja – za zemlju iz adrese (v2.8.3).</summary>
        private static readonly Dictionary<string, string> Gradovi = new Dictionary<string, string>
        {
            // DE
            ["herne"] = "DE", ["hamburg"] = "DE", ["bremen"] = "DE", ["bremerhaven"] = "DE", ["duisburg"] = "DE", ["dortmund"] = "DE", ["koln"] = "DE", ["koeln"] = "DE",
            ["berlin"] = "DE", ["munchen"] = "DE", ["muenchen"] = "DE", ["nurnberg"] = "DE", ["nuernberg"] = "DE", ["hannover"] = "DE", ["mannheim"] = "DE",
            ["ludwigshafen"] = "DE", ["wittenberg"] = "DE", ["lutherstadt wittenberg"] = "DE", ["leipzig"] = "DE", ["neuss"] = "DE", ["frankfurt"] = "DE", ["stuttgart"] = "DE",
            // HU
            ["budapest"] = "HU", ["kiskoros"] = "HU", ["szeged"] = "HU", ["kelebia"] = "HU", ["debrecen"] = "HU", ["gyor"] = "HU", ["sopron"] = "HU", ["miskolc"] = "HU", ["zahony"] = "HU",
            // TR
            ["ankara"] = "TR", ["konya"] = "TR", ["istanbul"] = "TR", ["izmir"] = "TR", ["luleburgaz"] = "TR", ["cankaya"] = "TR", ["kapikule"] = "TR", ["halkali"] = "TR",
            ["cerkezkoy"] = "TR", ["bursa"] = "TR", ["kocaeli"] = "TR", ["mersin"] = "TR", ["tekirdag"] = "TR", ["edirne"] = "TR",
            // CZ, SK, PL, AT
            ["lovosice"] = "CZ", ["praha"] = "CZ", ["prague"] = "CZ", ["ostrava"] = "CZ", ["brno"] = "CZ", ["decin"] = "CZ", ["svetec"] = "CZ", ["usti nad labem"] = "CZ",
            ["bratislava"] = "SK", ["kosice"] = "SK", ["dunajska streda"] = "SK", ["gdansk"] = "PL", ["warszawa"] = "PL", ["katowice"] = "PL",
            // v2.9.2 (3.4): CIT 18 koks (Zabrze 41-800 → PL)
            ["zabrze"] = "PL", ["gliwice"] = "PL", ["krakow"] = "PL", ["wroclaw"] = "PL", ["poznan"] = "PL", ["sosnowiec"] = "PL", ["slawkow"] = "PL", ["dabrowa gornicza"] = "PL",
            ["chalupki"] = "PL", ["rybnik"] = "PL", ["jastrzebie zdroj"] = "PL", ["zilina"] = "SK", ["trnava"] = "SK", ["nitra"] = "SK", ["presov"] = "SK", ["sturovo"] = "SK",
            ["cadca"] = "SK", ["bohumin"] = "CZ", ["plzen"] = "CZ", ["olomouc"] = "CZ",
            ["wien"] = "AT", ["vienna"] = "AT", ["linz"] = "AT", ["graz"] = "AT", ["villach"] = "AT", ["salzburg"] = "AT", ["enns"] = "AT",
            // IT, SI, HR
            ["poggio rusco"] = "IT", ["nonantola"] = "IT", ["milano"] = "IT", ["verona"] = "IT", ["trieste"] = "IT", ["bologna"] = "IT", ["padova"] = "IT", ["busca"] = "IT", ["san zeno"] = "IT",
            ["ljubljana"] = "SI", ["koper"] = "SI", ["maribor"] = "SI", ["dobova"] = "SI", ["sezana"] = "SI",
            ["zagreb"] = "HR", ["rijeka"] = "HR", ["tovarnik"] = "HR", ["savski marof"] = "HR",
            // RO, BG, GR, MK, ME, BA
            ["agigea"] = "RO", ["constanta"] = "RO", ["bucuresti"] = "RO", ["bucharest"] = "RO", ["arad"] = "RO", ["timisoara"] = "RO", ["beclean pe somes"] = "RO",
            ["sofia"] = "BG", ["svilengrad"] = "BG", ["plovdiv"] = "BG", ["varna"] = "BG", ["ruse"] = "BG", ["kalotina"] = "BG", ["dragoman"] = "BG",
            ["thessaloniki"] = "GR", ["skopje"] = "MK", ["podgorica"] = "ME", ["sarajevo"] = "BA",
            // RS
            ["novi sad"] = "RS", ["beograd"] = "RS", ["belgrade"] = "RS", ["novi beograd"] = "RS", ["sabac"] = "RS", ["ruma"] = "RS", ["sremska mitrovica"] = "RS",
            ["pancevo"] = "RS", ["zrenjanin"] = "RS", ["subotica"] = "RS", ["prokuplje"] = "RS", ["nis"] = "RS", ["kragujevac"] = "RS", ["vrbas"] = "RS", ["stara pazova"] = "RS",
            ["futog"] = "RS", ["vrsac"] = "RS", ["sid"] = "RS", ["dimitrovgrad"] = "RS", ["presevo"] = "RS", ["kikinda"] = "RS", ["smederevo"] = "RS", ["vreoci"] = "RS", ["lapovo"] = "RS",
        };

        /// <summary>Naziv mesta iz rubrike (bez šifre stanice, zemlje i interpunkcije): „Agigea Nord   Romania“ → „Agigea Nord“.</summary>
        private static string Mesto(string s)
        {
            if (Prazno(s)) return null;
            // OCR navodnici, uspravne crte i zagrade nisu deo naziva mesta („„Šabac |“ → „Šabac“)
            s = Regex.Replace(s, "[„“”\"'|\\[\\]{}()«»]", " ");
            var t = Regex.Split(s.Trim(), @"\s*[\n,;/]\s*|\s{3,}")[0];
            t = Regex.Replace(t, @"\b\d[\d\s\-]*\b", " ");
            foreach (var z in Provere.IsoPoNazivu.Keys) t = Regex.Replace(t, @"(?<!\p{L})" + Regex.Escape(z) + @"(?!\p{L})", " ", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"\s+", " ").Trim(' ', '.', '-', '–', ':');
            if (!Regex.IsMatch(t, @"\p{L}{3}") || SumOcr(t)) return null;
            return t.Length > 1 && t == t.ToUpperInvariant() ? CultureInfo.GetCultureInfo("sr-Latn-RS").TextInfo.ToTitleCase(t.ToLowerInvariant()) : t;
        }

        /// <summary>
        /// v2.8.2: datum iz dokumenta – zadata rubrika lista (r29 ili r16), pa datum na priloženom spisku kola; nikad datum maila.
        /// Delovi su null kada nisu poznati (godina „0000“ u r16 = godina nije upisana).
        /// </summary>
        /// <summary>v2.8.4 (7): mesto druge rubrike (r29 → r16, r16 → r29) samo kada je ona pouzdano pročitana (SKM r29: „en jour—heure Nord j“ je šum).</summary>
        private static string PouzdanoMesto(Rubrika r, Kontekst k)
        {
            var m = r is MestoDatum md ? md.Mesto : r is Preuzimanje pr ? pr.Mesto : null;
            if (k.Ocr && (r.Status == StatusRubrike.Necitljivo || r.Pouzdanost < 0.9 || (k.C.PouzdanostStrane > 0 && k.C.PouzdanostStrane < Motor.PragSkena))) return null;
            return m;
        }

        /// <summary>v2.8.3: datum druge rubrike samo kada je ona pouzdano pročitana (sken: „0.11.2026“ nije 1. novembar).</summary>
        private static string PouzdanDatum(Rubrika r, Kontekst k)
        {
            var d = r is MestoDatum md ? md.Datum : r is Preuzimanje pr ? pr.Datum : null;
            if (k.Ocr && (r.Status == StatusRubrike.Necitljivo || r.Pouzdanost < 0.9 || (k.C.PouzdanostStrane > 0 && k.C.PouzdanostStrane < Motor.PragSkena))) return null;
            return d;
        }

        private static (string god, string mes, string dan) DatumIzDokumenta(string iz, Kontekst k)
        {
            foreach (var d in new[] { iz }.Concat(k.Spiskovi.Select(s => s.s.Datum)))
            {
                var m = Regex.Match(d ?? "", @"^(\d{4})-(\d{2})-(\d{2})$");
                if (!m.Success || m.Groups[2].Value == "00") continue;
                return (m.Groups[1].Value == "0000" ? null : m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value);
            }
            return (null, null, null);
        }

        private static DateTime? Datum(string iso, DateTime referentni)
        {
            if (Prazno(iso)) return null;
            if (DateTime.TryParseExact(iso.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d.Year < 2000 ? new DateTime(referentni.Year, d.Month, d.Day) : d;
            var m = Regex.Match(iso, @"^0000-(\d{2})-(\d{2})");
            if (m.Success && int.TryParse(m.Groups[1].Value, out var mm) && int.TryParse(m.Groups[2].Value, out var dd) && mm >= 1 && mm <= 12 && dd >= 1 && dd <= 31)
                try { return new DateTime(referentni.Year, mm, dd); } catch { }
            return null;
        }

        /// <summary>UIC šifra zemlje otpravne stanice (r16 šifra stanice, inače ISO zemlje iz r16 → UIC).</summary>
        private static string UicZemlje(Kontekst k)
        {
            var s = Cif(k.C.R16.SifraStanice);
            if (s.Length >= 7) return s.Substring(0, 2);
            var iso = Provere.ZemljaLista(k, "1");
            return iso == null ? null : Provere.UicIso.FirstOrDefault(x => x.Value == iso).Key;
        }

        private static string Stanica(string sifra)
        {
            var c = Cif(sifra);
            if (c.Length == 8) c = c.Substring(2);
            return c.Length == 6 ? c.Substring(0, 5) + "-" + c.Substring(5) : null;
        }
    }
}
