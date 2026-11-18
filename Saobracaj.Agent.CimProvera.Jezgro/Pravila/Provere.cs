using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Saobracaj.Agent.CimProvera.Model;

namespace Saobracaj.Agent.CimProvera.Pravila
{
    /// <summary>
    /// Implementacija pravila po ID-u iz cim_validation_rules_*.json. Svaka metoda vraća pogotke (kada pravilo NIJE
    /// ispunjeno). Pravila iz JSON-a koja ovde nemaju metodu se ne izvršavaju i navode se u izlazu kao "nije implementirano".
    /// </summary>
    internal static class Provere
    {
        public static readonly Dictionary<string, Func<Kontekst, IEnumerable<Pogodak>>> Sve =
            new Dictionary<string, Func<Kontekst, IEnumerable<Pogodak>>>(StringComparer.OrdinalIgnoreCase)
            {
                // general_rules
                // v2.7 (interna odluka KP 2026-11-12): G2, G11, R13, R18, R21-WAGON, X10, X15, R57, R57-DEONICA i KP7 se ne proveravaju.
                ["G1"] = G1, ["G3"] = G3, ["G4"] = G4, ["G5"] = G5, ["G6"] = G6, ["G9"] = G9, ["G12"] = G12,
                // field_rules
                ["R1"] = R1, ["R1-VAT"] = R1Vat, ["R1-ISO"] = k => ZemljaRecima(k, k.C.R1, "1"), ["R4"] = R4, ["R4-VAT"] = R4Vat, ["R4-ISO"] = k => ZemljaRecima(k, k.C.R4, "4"),
                ["R7"] = R7, ["R9"] = R9, ["R10"] = R10,
                ["R12"] = R12, ["R14"] = R14, ["R16"] = R16, ["R18-SPISAK"] = R18Spisak, ["R20"] = R20,
                ["X30"] = X30,
                ["R20-INCO"] = R20Inco, ["R20-INCO-ZASTARELO"] = R20IncoZastarelo, ["R21-MRN99"] = R21Mrn99, ["R20-RUTA"] = R20Ruta, ["R21"] = R21, ["R22"] = R22, ["R23"] = R23,
                ["R24"] = R24, ["R24-NHM6"] = R24Nhm6, ["R25-MASA"] = R25Masa,
                ["R26"] = k => Iznos(k, k.C.R26, "26"), ["R27"] = k => Iznos(k, k.C.R27, "27"), ["R28"] = k => Iznos(k, k.C.R28, "28"),
                ["R29"] = R29, ["R30"] = R30, ["R58A"] = R58A, ["R62"] = R62, ["R49"] = R49, ["R50"] = R50,
                // r21_subrules
                ["R21-DESC"] = R21Desc, ["R21-UTI"] = R21Uti, ["R21-IMDG"] = R21Imdg,
                ["R21-MRN"] = R21Mrn, ["R21-EWC"] = R21Ewc, ["R21-CUV"] = R21Cuv,
                // cross_rules
                ["X1"] = X1, ["X2"] = X2, ["X3"] = X3, ["X4"] = X4, ["X5"] = X5, ["X6"] = X6, ["X7"] = X7, ["X8"] = X8,
                ["X9"] = X9, ["X11"] = X11, ["X12"] = X12, ["X13"] = X13, ["X14"] = X14,
                ["X16"] = X16, ["X17"] = X17, ["X19"] = X19, ["X20"] = X20, ["X21"] = X21, ["X22"] = X22, ["X23"] = X23,
                ["X24"] = X24, ["X25"] = X25, ["X26"] = X26, ["X27"] = X27, ["X28"] = X28, ["X29"] = X29, ["X29-NHM6"] = X29Nhm6,
                // company_rules
                ["KP7-PLATILAC"] = KP7Platilac, ["KP7-NIJE-PLATILAC"] = KP7NijePlatilac, ["KP7-NEJASNO"] = KP7Nejasno, ["KP7-INFO"] = KP7Info,
                ["KP57"] = KP57, ["KP2"] = KP2, ["KP3"] = KP3, ["KP5"] = KP5,   // KP6 obrisan u v2.8
                // recommendations (stalni ID iz JSON-a; REC2 obrisan u v2.7)
                ["REC1"] = Rec1, ["REC7"] = Rec7, ["REC8"] = Rec8,   // REC3 i REC6 obrisani u v2.8, REC4 (r57 status/deonica) u v2.8.3
            };

        /// <summary>
        /// Pravila za rubrike prevoznika (49, 50, 58a, 62) – važe samo kada je pošiljalac interni ili je u r62 šifra 3212.
        /// Sva ostala pravila (i R57, KP7, KP57) važe za SVAKI CIM/CUV: na priprema@ stižu samo pošiljke u kojima KP učestvuje.
        /// </summary>
        // v2.8: R58A i R62 važe za svaki list (test 2026-11-13: GK30, CUV_257, CIM COAL 16); KP2 se proverava kada je r62 = 3212
        public static readonly HashSet<string> SamoKp = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "R49", "R50" };

        /// <summary>Pravila sa filled_by = carrier u JSON-u koja ipak važe uvek (ne zavise od režima KP).</summary>
        public static readonly HashSet<string> UvekVazi = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "KP57", "R58A", "R62", "KP2" };

        private static readonly string[] Incoterms = { "EXW", "FCA", "CPT", "CIP", "DAP", "DPU", "DDP", "DAF", "DDU" };
        internal static readonly HashSet<string> Eu = new HashSet<string>
        {
            "AT","BE","BG","HR","CY","CZ","DK","EE","FI","FR","DE","GR","HU","IE","IT","LV","LT","LU","MT","NL","PL","PT","RO","SK","SI","ES","SE"
        };

        // =========================================================================================== pomoćne

        private static Pogodak P(string rubrika, string kljuc = null) => new Pogodak(rubrika, kljuc);

        internal static string N(string s) => PraviloKp.Normalizuj(s);
        private static string Cifre(string s) => Regex.Replace(s ?? "", @"\D", "");
        private static bool Prazno(string s) => string.IsNullOrWhiteSpace(s);

        private static string Tekst(ListaRubrika r) =>
            string.Join(" ; ", new[] { r.Tekst }.Concat(r.Stavke.Select(s => s.Sifra + " " + s.Tekst)).Where(x => !Prazno(x)));

        private static bool R7Ima(Kontekst k, string sifra) => k.C.R7.Stavke.Any(s => Cifre(s.Sifra) == sifra);
        private static IEnumerable<SifraTekst> R7Stavke(Kontekst k, string sifra) => k.C.R7.Stavke.Where(s => Cifre(s.Sifra) == sifra);

        /// <summary>Datum YYYY-MM-DD (godina 0000 = nije upisana → uzima se godina iz drugog datuma ili referentnog).</summary>
        internal static DateTime? Datum(string s, int podrazumevanaGodina)
        {
            if (Prazno(s)) return null;
            var m = Regex.Match(s, @"^(\d{4})-(\d{1,2})-(\d{1,2})");
            if (!m.Success) return null;
            int g = int.Parse(m.Groups[1].Value), mes = int.Parse(m.Groups[2].Value), d = int.Parse(m.Groups[3].Value);
            if (g == 0) g = podrazumevanaGodina;
            try { return new DateTime(g, mes, d); } catch { return null; }
        }

        private static double? Broj(string s)
        {
            if (Prazno(s)) return null;
            var t = Regex.Replace(s, @"[^\d,\.]", "");
            if (t.Length == 0) return null;
            // 21.500 / 21,500 / 21500,5 → hiljade vs decimale
            if (Regex.IsMatch(t, @"^\d{1,3}([\.,]\d{3})+$")) t = Regex.Replace(t, @"[\.,]", "");
            else t = t.Replace(",", ".");
            return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : (double?)null;
        }

        // ---- prepoznavanje dokumenata u rubrici 9 / 7.10 ----
        private static readonly Dictionary<string, string[]> KljuceviR9 = new Dictionary<string, string[]>
        {
            [TipDokumenta.IzvoznaDeklaracija] = new[] { "izvoz", "export", "ausfuhr", "vdd", "ex", "ex1", "exa", "ecs", "e mrn", "eas", "830", "jci" },
            [TipDokumenta.TranzitnaDeklaracija] = new[] { "t1", "t2", "t2l", "tranzit", "transit", "ncts", "tad", "versand", "t mrn", "ts mrn", "820", "821" },
            [TipDokumenta.DrugaCarinska] = new[] { "carinsk", "customs", "zoll", "jci", "ens", "deklarac", "declaration", "mrn" },
            // v3.0: „wagonlist“ (bez razmaka, Zmajevo UID 660/763) i slovački „výkaz vozňov“ (FORTRACK UID 380/577) u r9
            [TipDokumenta.SpisakKola] = new[] { "spisak kola", "spisku kola", "wagon list", "wagonlist", "list of wagons", "wagenliste", "liste des wagons", "cit 23", "cit23", "vagon listesi", "kocsijegyz", "vykaz voz" },
            [TipDokumenta.DodatniList] = new[] { "dodatni list", "dodatnih list", "dodatni listovi", "additional sheet", "zusatzblatt", "erganzungsblatt", "erganzungsblaett", "ergaenzungsbl", "potlap", "feuille complementaire" },
            [TipDokumenta.Eur1] = new[] { "eur 1", "eur1", "movement certificate" },
            [TipDokumenta.UverenjeOPoreklu] = new[] { "porekl", "origin", "ursprung", "861" },
            [TipDokumenta.Sertifikat] = new[] { "sertifikat", "certifikat", "certificate", "zertifikat", "fito", "phyto", "veterin", "analiz", "kontrol" },
            [TipDokumenta.Faktura] = new[] { "faktur", "invoice", "rechnung", "facture", "380" },
            [TipDokumenta.AnnexVii] = new[] { "annex vi", "prilog vii", "anhang vii", "kretanju otpada", "movement of waste" },
            [TipDokumenta.IzvestajRadioloski] = new[] { "radiolog", "radiometr", "radio", "radioaktiv" },
            [TipDokumenta.DozvolaOtpad] = new[] { "dozvol", "permit", "genehmigung", "resenje" },
        };

        internal static bool Pominje(string tekst, string tip)
        {
            if (!KljuceviR9.TryGetValue(tip, out var kljucevi)) return false;
            var t = " " + N(tekst) + " ";
            return kljucevi.Any(x =>
            {
                var kx = N(x);
                // kratke oznake (ex, t1, tad…) samo kao cela reč; duže i kao početak reči
                return kx.Length <= 4 ? t.Contains(" " + kx + " ") : t.Contains(" " + kx);
            });
        }

        /// <summary>UN/EDIFACT 1001 šifra stavke r9 → vrsta dokumenta (stavka sa šifrom je samo taj dokument: „916 … Frachtbr. CIT 23“ je dodatni list).</summary>
        private static readonly Dictionary<string, string> TipPoSifri = new Dictionary<string, string>
        {
            ["271"] = TipDokumenta.SpisakKola, ["830"] = TipDokumenta.IzvoznaDeklaracija, ["820"] = TipDokumenta.TranzitnaDeklaracija,
            ["821"] = TipDokumenta.TranzitnaDeklaracija, ["822"] = TipDokumenta.TranzitnaDeklaracija, ["380"] = TipDokumenta.Faktura,
            ["865"] = TipDokumenta.Eur1, ["861"] = TipDokumenta.UverenjeOPoreklu, ["916"] = TipDokumenta.DodatniList,
        };

        private static bool R9Pominje(Kontekst k, string tip)
        {
            var stavke = k.C.R9.Stavke;
            if (stavke.Count == 0 || !stavke.Any(s => TipPoSifri.ContainsKey(s.Sifra ?? ""))) return Pominje(Tekst(k.C.R9), tip);
            var bezSifre = new List<string>();
            foreach (var s in stavke)
            {
                if (TipPoSifri.TryGetValue(s.Sifra ?? "", out var t)) { if (t == tip) return true; }
                else bezSifre.Add(s.Tekst);
            }
            return Pominje(string.Join(" ", bezSifre), tip);
        }

        private static bool R710Pominje(Kontekst k, string tip) => R7Stavke(k, "10").Any(s => Pominje(s.Tekst, tip) || (tip != TipDokumenta.SpisakKola && !Prazno(s.Tekst) && Pominje(s.Tekst, TipDokumenta.DrugaCarinska)));

        internal static string NazivTipa(string tip, bool en)
        {
            switch (tip)
            {
                case TipDokumenta.IzvoznaDeklaracija: return en ? "export declaration (EX/VDD)" : "izvozna deklaracija (EX/VDD)";
                case TipDokumenta.TranzitnaDeklaracija: return en ? "transit declaration (T1/T2)" : "tranzitna deklaracija (T1/T2)";
                case TipDokumenta.DrugaCarinska: return en ? "customs document" : "carinski dokument";
                case TipDokumenta.Eur1: return "EUR.1";
                case TipDokumenta.UverenjeOPoreklu: return en ? "certificate of origin" : "uverenje o poreklu";
                case TipDokumenta.Sertifikat: return en ? "certificate" : "sertifikat";
                case TipDokumenta.Faktura: return en ? "invoice" : "faktura";
                case TipDokumenta.SpisakKola: return en ? "wagon list (CIT 23)" : "spisak kola (CIT 23)";
                case TipDokumenta.DodatniList: return en ? "additional sheet" : "dodatni list";
                case TipDokumenta.AnnexVii: return en ? "waste movement document (Annex VII)" : "dokument o kretanju otpada (Annex VII)";
                case TipDokumenta.IzvestajRadioloski: return en ? "radiological report" : "izveštaj o radiološkoj kontroli";
                case TipDokumenta.DozvolaOtpad: return en ? "waste export permit" : "dozvola za izvoz otpada";
                default: return tip;
            }
        }

        /// <summary>Fajl i strane (kada je u jednom PDF-u više dokumenata, npr. "HOLCIM (4).PDF str. 8").</summary>
        private static string Fajlovi(IEnumerable<Dokument> d) =>
            string.Join(", ", d.GroupBy(x => System.IO.Path.GetFileName((x.Fajl ?? "").Split('/').Last()))
                .Select(g => g.Key + (g.SelectMany(x => x.Strane).Any(s => s > 1) || g.Count() > 1
                    ? " str. " + string.Join(", ", g.SelectMany(x => x.Strane).Distinct().OrderBy(s => s)) : "")));

        // ---- kola / UTI ----
        private static List<string> KolaR18(Kontekst k) =>
            k.C.R18.Stavke.Select(s => Cifre(s.Sifra + s.Tekst)).Concat(new[] { Cifre(k.C.R18.Tekst) })
                .Where(x => x.Length == 12).Distinct().ToList();

        private static int BrojKolaUkupno(Kontekst k) =>
            Math.Max(Math.Max(KolaR18(k).Count, BrojKolaUR21(k)), k.Spiskovi.Select(s => s.s.Kola.Select(x => Cifre(x.Broj)).Distinct().Count()).DefaultIfEmpty(0).Max());

        /// <summary>Broj kola naveden rečima u r21: „Nr. 30 empty private wagons“, „30 kola“, „(6 Kocsi)“, „6 Wg“, „31 carri“. 0 ako ga nema.</summary>
        internal static int BrojKolaUR21(Kontekst k) => BrojKolaUTekstu(k.C.R21.Tekst);

        internal static int BrojKolaUTekstu(string t)
        {
            var m = Regex.Match(t ?? "", @"(?<![\d.,/])(\d{1,3})\s*(?:\p{L}+\s+){0,3}?(?:kola|vagona|vagon[ai]?|wagons?|wgs?\.?|wagen|kocsi|carri|wagons|vozů|vozu|vagoane|вагона|кола)\b", RegexOptions.IgnoreCase);
            return m.Success && int.TryParse(m.Groups[1].Value, out var n) && n > 0 && n < 200 ? n : 0;
        }

        private static int BrojUti(Kontekst k) =>
            Math.Max(k.C.R21.Uti.Select(u => N(u.Broj)).Where(x => x.Length > 0).Distinct().Count(),
                k.Spiskovi.Select(s => s.s.Kola.SelectMany(x => x.Uti).Select(N).Distinct().Count()).DefaultIfEmpty(0).Max());

        private static bool ViseKolaIliTriUti(Kontekst k) => BrojKolaUkupno(k) > 1 || BrojUti(k) >= 3;

        private static bool R18PominjeSpisak(Kontekst k)
        {
            var t = Tekst(k.C.R18);
            return Pominje(t, TipDokumenta.SpisakKola) || Regex.IsMatch(N(t), @"\b(po spisku|prema spisku|vidi spisak|see (wagon )?list|siehe|selon liste|lt spisak|specifikacij\w*|wagon ?list)\b");
        }

        private static bool JeRid(Kontekst k) => N(k.C.R23.Tekst) == "x" || k.C.R21.UnBrojevi.Any(u => !Prazno(u));

        // =========================================================================================== general_rules

        private static IEnumerable<Pogodak> G1(Kontekst k)
        {
            var j = k.C.JeziciPopunjavanja.Select(x => x.ToLowerInvariant()).ToList();
            if (j.Count > 0 && !j.Any(x => x == "de" || x == "fr" || x == "en"))
                yield return P("–", "jezik").D("(" + string.Join(", ", j) + ")");
        }

        /// <summary>Rubrika 9 je prazna, a u mailu su prateći dokumenti: R9 ih navodi sve u jednoj stavci (G3/X9/X20/X22 se tada ne ponavljaju).</summary>
        private static bool R9PraznaObuhvata(Kontekst k) => !k.C.R9.Popunjena && R9Potrebni(k).Count > 0;

        private static List<Dokument> R9Potrebni(Kontekst k) =>
            k.Svi.Where(d => TipDokumenta.JeCarinski(d.Tip) || d.Tip == TipDokumenta.SpisakKola || d.Tip == TipDokumenta.DodatniList).ToList();

        private static IEnumerable<Pogodak> G3(Kontekst k)
        {
            if (!ViseKolaIliTriUti(k) || R9PraznaObuhvata(k)) yield break;
            if (k.Spiskovi.Count > 0 && R9Pominje(k, TipDokumenta.SpisakKola)) yield break;
            if (k.Spiskovi.Count == 0 && SpisakNaveden(k) == "18" && !k.C.R9.Popunjena)
            {
                // r9 prazna, a r18 upućuje na spisak („WAGON LIST“): spisak se navodi i u r9 (nepriloženost prijavljuje X26)
                yield return P("9", "spisak-r9").D((k.U.Engleski ? "Box 9 is empty, but box 18 says „" : "Rubrika 9 je prazna, a u rubrici 18 piše „") + (k.C.R18.Tekst ?? "").Replace("\n", " / ").Trim()
                    + (k.U.Engleski ? "“ – the wagon list must also be listed in box 9." : "“ – spisak kola se navodi i u rubrici 9."));
                yield break;
            }
            if (k.Spiskovi.Count == 0 && SpisakNaveden(k) != null) yield break; // naveden, a nije priložen: X26
            yield return P("9", "spisak-r9").D(k.Spiskovi.Count > 0
                ? (k.U.Engleski ? "The wagon list is attached (" : "Spisak kola je priložen (") + Fajlovi(k.Spiskovi.Select(s => s.dok)) + (k.U.Engleski ? "), but it is not listed in box 9." : "), ali nije naveden u rubrici 9.")
                : (k.U.Engleski ? "The wagon list is not attached." : "Spisak kola nije priložen."));
        }

        private static IEnumerable<Pogodak> G4(Kontekst k)
        {
            var dodatni = k.Tip(TipDokumenta.DodatniList).ToList();
            if (dodatni.Count == 0) yield break;
            if (!R9Pominje(k, TipDokumenta.DodatniList) || dodatni.SelectMany(d => d.DodatniList).Any(x => !x.BrojPosiljke || !x.DatumPreuzimanja))
                yield return P("9", "dodatni-list");
        }

        /// <summary>G5: rubrika 7 sa šifrom. Od v2.7 se ne odnosi na r13 i r57 (šifra i broj tačke nisu obavezni).</summary>
        private static IEnumerable<Pogodak> G5(Kontekst k)
        {
            if (k.C.R7.Stavke.Any(s => Prazno(s.Sifra) && !Prazno(s.Tekst))) yield return P("7", "sifra-7").S("field", "7");
        }

        private static IEnumerable<Pogodak> G6(Kontekst k)
        {
            if (k.C.R9.Stavke.Any(s => Prazno(s.Sifra) && !Prazno(s.Tekst))) yield return P("9", "r9-sifre");
        }

        private static IEnumerable<Pogodak> G9(Kontekst k)
        {
            if (k.C.ElektronskiIspis && !k.C.NapomenaIspisa) yield return P("–", "ispis");
        }

        /// <summary>G12: zaglavlje informativnog ispisa („Nem fuvarlevél / Kein Frachtbrief“) – dokument nije tovarni list.</summary>
        internal static bool JeInformativniIspis(PravilaSkup p, CimList c)
        {
            if (!p.PoId.TryGetValue("G12", out var def) || string.IsNullOrWhiteSpace(c?.Zaglavlje)) return false;
            var z = " " + Saobracaj.Common.Text.TekstUtil.Kljuc(c.Zaglavlje) + " ";
            var markeri = (def.Json["header_markers"] as Newtonsoft.Json.Linq.JArray)?.Select(x => (string)x) ?? Enumerable.Empty<string>();
            return markeri.Any(m => z.Contains(Saobracaj.Common.Text.TekstUtil.Kljuc(m)));
        }

        private static IEnumerable<Pogodak> G12(Kontekst k)
        {
            if (JeInformativniIspis(k.P, k.C)) yield return P("–", "g12");
        }

        // =========================================================================================== field_rules

        private static IEnumerable<Pogodak> StranaDelovi(Kontekst k, Strana s, string broj, bool potpis)
        {
            if (!s.Popunjena) { yield return P(broj, "r" + broj).D(k.U.Engleski ? "(box is empty)" : "(rubrika je prazna)"); yield break; }
            if (!k.Citljivo(s, broj)) yield break;
            var fali = new List<string>();
            if (Prazno(s.Naziv)) fali.Add(k.U.Engleski ? "name" : "naziv");
            // v2.8: adresa po sadržaju cele rubrike (i kada je u istom redu sa nazivom – CIM COAL 16: „…, Balkanska street 13,
            // 11000 Belgrade, Serbia“); greška „adresa“ samo kada nema ni ulice ni mesta, „mesto“ kada je upisana samo ulica (GK30)
            var ceo = Regex.Replace((s.Tekst ?? "") + " " + (s.Adresa ?? ""), @"\s+", " ");
            var imaMesto = Regex.IsMatch(ceo, @"(?<!\d)\d{4,6}(?!\d)\s*-?\s*\p{L}{3,}|\p{L}{3,}\s*,?\s*(?<!\d)\d{4,6}(?!\d)\s*(,|$)|(?<!\p{L})[A-Z]{1,2}-\d{4,5}\s+\p{L}{3,}")
                           || (!Prazno(s.Adresa) && !Regex.IsMatch(s.Adresa.Trim(), @"\p{L}{2,}\.?\s+\d{1,5}\s*[a-zA-Z]?$"));
            var imaUlicu = Regex.IsMatch(ceo, @"\p{L}{3,}\.?\s+\d{1,4}\s*[a-zA-Z]?(?!\d)|(?<!\p{L})(bb|b\.b\.)(?!\p{L})", RegexOptions.IgnoreCase);
            if (!imaMesto && !imaUlicu) fali.Add(k.U.Engleski ? "address" : "adresa");
            else if (!imaMesto) fali.Add(k.U.Engleski ? "postcode and place" : "poštanski broj i mesto");
            // Greška je samo kada zemlje nema; zemlja upisana rečima („Srbija“, „Italy“) je upozorenje R1-ISO / R4-ISO.
            // v2.8.2: zemlja se traži samo u redovima adrese – „Louis Dreyfus Company Hungary Kft.“ nije zemlja
            Pogodak proveriteZemlju = null;
            if (!Regex.IsMatch((s.SifraZemlje ?? "").Trim(), @"^[A-Za-z]{2,3}$") && !ZemljaUAdresi(s))
            {
                // v2.8.4: r4 sa nepoznatom reči na kraju adrese („36043 Camisano Vicenlino (VI), Hay“ – OCR pokvario „Italy“), a uputna
                // stanica je u inostranstvu (r10/r12) – napomena „proverite zemlju“, ne greška „nedostaje zemlja“
                var iso4 = broj == "4" ? ZemljaLista(k, "4") : null;
                var rec = broj == "4" ? NepoznataRecNaKrajuAdrese(s) : null;
                if (iso4 != null && iso4 != "RS" && rec != null)
                {
                    proveriteZemlju = P(broj, "r" + broj).D(k.U.Engleski
                        ? "Please check the country (according to box 10/12: " + iso4 + ") – at the end of the address is entered: „" + rec + "“."
                        : "Proverite zemlju (prema r10/r12: " + iso4 + ") – na kraju adrese upisano: „" + rec + "“.").Pr(iso4);
                    proveriteZemlju.Tezina = Tezina.Upozorenje;
                    proveriteZemlju.Poruka = k.U.Engleski ? "Please check the country (according to box 10/12: " + iso4 + ")." : "Proverite zemlju (prema r10/r12: " + iso4 + ").";
                }
                else fali.Add(k.U.Engleski ? "country (ISO 3166 code)" : "zemlja (ISO 3166 šifra)");
            }
            if (proveriteZemlju != null) yield return proveriteZemlju;
            if (potpis && !s.Potpis && !k.C.R62.Popunjena) fali.Add(k.U.Engleski ? "signature" : "potpis");
            if (fali.Count > 0)
            {
                var pg = P(broj, "r" + broj).D((k.U.Engleski ? "Missing: " : "Nedostaje: ") + string.Join(", ", fali) + ".");
                // v2.8: primer zemlje iz samog lista (r4: uputna stanica r10/r12; r1: r16/r17), ne fiksni „npr. HU“
                var iso = ZemljaLista(k, broj);
                var osnovni = k.P.PoId.TryGetValue("R" + broj, out var def) ? (string)def.Json[k.U.Engleski ? "primer_en" : "primer"] ?? (string)def.Json["primer"] : null;
                if (iso != null && osnovni != null && Regex.IsMatch(osnovni, @"\((npr|e\.g)\.\s*[A-Z]{2}\)"))
                    pg.Pr(Regex.Replace(osnovni, @"\((npr|e\.g)\.\s*[A-Z]{2}\)", m => "(" + m.Groups[1].Value + ". " + iso + ")"));
                yield return pg;
            }
        }

        /// <summary>
        /// v2.8.4: reč na kraju adrese, posle poštanskog broja i mesta i zareza („…, 36043 Camisano Vicenlino (VI), Hay“), koja nije
        /// naziv zemlje – verovatno pokvaren naziv zemlje; null ako je nema.
        /// </summary>
        internal static string NepoznataRecNaKrajuAdrese(Strana s)
        {
            var redovi = ((s?.Tekst ?? "") + "\n" + (s?.Adresa ?? "")).Split('\n').Select(x => x.Trim()).Where(x => Regex.IsMatch(x, @"\p{L}")).ToList();
            foreach (var red in redovi.AsEnumerable().Reverse())
            {
                var m = Regex.Match(red, @"(?<!\d)\d{4,6}(?!\d)[^,\n]*,\s*(?<rec>\p{L}{2,15})\s*\.?\s*$");
                if (!m.Success) continue;
                var rec = m.Groups["rec"].Value;
                return IsoPoNazivu.ContainsKey(rec) || Regex.IsMatch(rec, @"^[A-Z]{2}$") ? null : rec;
            }
            return null;
        }

        /// <summary>UIC 920-14 šifra zemlje → ISO 3166 (za primer zemlje u r1/r4).</summary>
        internal static readonly Dictionary<string, string> UicIso = new Dictionary<string, string>
        {
            ["72"] = "RS", ["55"] = "HU", ["54"] = "CZ", ["56"] = "SK", ["53"] = "RO", ["52"] = "BG", ["78"] = "HR", ["79"] = "SI", ["80"] = "DE", ["81"] = "AT",
            ["83"] = "IT", ["51"] = "PL", ["44"] = "BA", ["50"] = "BA", ["62"] = "ME", ["65"] = "MK", ["73"] = "GR", ["75"] = "TR", ["87"] = "FR", ["88"] = "BE",
            ["84"] = "NL", ["85"] = "CH", ["86"] = "DK", ["74"] = "SE", ["70"] = "GB", ["71"] = "ES", ["94"] = "PT", ["25"] = "LV", ["24"] = "LT", ["26"] = "EE",
        };

        /// <summary>ISO šifra zemlje strane iz lista: r4 → uputna stanica (r10 zemlja, r12 UIC), r1 → otpravna stanica (r16, r17); null ako se ne zna.</summary>
        internal static string ZemljaLista(Kontekst k, string broj)
        {
            string IzNaziva(string n) => string.IsNullOrWhiteSpace(n) ? null : IsoPoNazivu.TryGetValue(n.Trim(), out var x) ? x
                : Regex.IsMatch(n.Trim(), @"^\d{2}$") && UicIso.TryGetValue(n.Trim(), out var u) ? u : Regex.IsMatch(n.Trim(), @"^[A-Za-z]{2}$") ? n.Trim().ToUpperInvariant() : null;
            string IzSifre(string s) { var c = Regex.Replace(s ?? "", @"\D", ""); return c.Length >= 2 && UicIso.TryGetValue(c.Substring(0, 2), out var u) ? u : null; }
            return broj == "4" ? IzNaziva(k.C.R10.Zemlja) ?? IzSifre(k.C.R12.Tekst) ?? IzSifre(k.C.R10.SifraStanice)
                               : IzNaziva(k.C.R16.Zemlja) ?? IzSifre(k.C.R16.SifraStanice);
        }

        internal static readonly Dictionary<string, string> IsoPoNazivu = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["srbija"] = "RS", ["serbia"] = "RS", ["serbien"] = "RS", ["србија"] = "RS", ["madjarska"] = "HU", ["mađarska"] = "HU", ["hungary"] = "HU",
            ["romania"] = "RO", ["rumunija"] = "RO", ["bulgaria"] = "BG", ["bugarska"] = "BG", ["italy"] = "IT", ["italia"] = "IT", ["italija"] = "IT",
            ["hrvatska"] = "HR", ["croatia"] = "HR", ["slovenija"] = "SI", ["slovenia"] = "SI", ["turska"] = "TR", ["turkey"] = "TR", ["grcka"] = "GR",
            ["grčka"] = "GR", ["greece"] = "GR", ["austrija"] = "AT", ["austria"] = "AT", ["nemacka"] = "DE", ["nemačka"] = "DE", ["germany"] = "DE",
            ["ceska"] = "CZ", ["češka"] = "CZ", ["slovacka"] = "SK", ["slovakia"] = "SK", ["poljska"] = "PL", ["poland"] = "PL", ["makedonija"] = "MK",
            ["crna gora"] = "ME", ["montenegro"] = "ME", ["bosna"] = "BA",
            ["czech"] = "CZ", ["czechia"] = "CZ", ["czech republic"] = "CZ", ["tschechien"] = "CZ", ["česká republika"] = "CZ", ["ceska republika"] = "CZ",
            ["deutschland"] = "DE", ["magyarorszag"] = "HU", ["magyarország"] = "HU", ["romania"] = "RO", ["românia"] = "RO", ["slovensko"] = "SK", ["österreich"] = "AT", ["osterreich"] = "AT",
        };

        /// <summary>
        /// v2.8.2: naziv zemlje je u redovima ADRESE, a ne samo u nazivu firme („Louis Dreyfus Company Hungary Kft.“, „Elektroprivreda
        /// Srbije“). Iz teksta rubrike se izbacuje naziv (prvi deo upisa), pa se zemlja traži u ostatku.
        /// </summary>
        internal static bool ZemljaUAdresi(Strana s)
        {
            if (s == null || Prazno(s.NazivZemlje)) return false;
            return Regex.IsMatch(AdresaBezNaziva(s), @"(?<!\p{L})" + Regex.Escape(s.NazivZemlje.Trim()) + @"(?!\p{L})", RegexOptions.IgnoreCase);
        }

        /// <summary>Tekst rubrike bez naziva firme (prvi deo upisa) – redovi adrese, za zemlju (v2.8.2/2.8.3).</summary>
        internal static string AdresaBezNaziva(Strana s)
        {
            if (s == null) return "";
            var tekst = s.Tekst ?? "";
            string adresa;
            // naziv: parsirani naziv, a kada je on ceo upis (cifre, predugačak) – prvi deo do zareza ili novog reda
            var naziv = (s.Naziv ?? "").Trim();
            if (Prazno(naziv) || Regex.IsMatch(naziv, @"\d") || naziv.Length > 60) naziv = Regex.Split(tekst.Trim(), @"\s*[,\n]\s*")[0];
            // v2.8.4 (4.2): ceo upis u jednom redu („DP World Serbia Carinska 1 Novi Sad Serbia“) – naziv je samo deo do adrese
            // (ulica sa brojem ili poštanski broj); zemlja POSLE mesta/poštanskog broja je zemlja, a ne deo naziva
            var adr = Regex.Match(naziv, @"(?<=\s)\p{L}[\p{L}\-]{2,}\.?\s+\d{1,4}[a-zA-Z]?(?!\d)|(?<=\s)(?:[A-Z]{1,2}-)?\d{4,6}(?!\d)");
            if (adr.Success && adr.Index > 0) naziv = naziv.Substring(0, adr.Index).Trim();
            if (!Prazno(naziv) && tekst.IndexOf(naziv, StringComparison.OrdinalIgnoreCase) is int i && i >= 0)
                adresa = tekst.Remove(i, naziv.Length);
            else
            {
                var redovi = tekst.Split('\n');
                adresa = redovi.Length > 1 ? string.Join("\n", redovi.Skip(1)) : tekst;
            }
            adresa += "\n" + (s.Adresa ?? "");
            return adresa;
        }

        /// <summary>R1-ISO / R4-ISO: zemlja upisana rečima umesto ISO 3166 šifre – upozorenje, sa predlogom iz istog upisa.</summary>
        private static IEnumerable<Pogodak> ZemljaRecima(Kontekst k, Strana s, string broj)
        {
            if (!s.Popunjena || !k.Citljivo(s, broj) || Regex.IsMatch((s.SifraZemlje ?? "").Trim(), @"^[A-Za-z]{2,3}$") || !ZemljaUAdresi(s)) yield break;
            var iso = IsoPoNazivu.TryGetValue(s.NazivZemlje.Trim(), out var x) ? x : "";
            var upisano = Regex.Match(s.Tekst ?? "", @"(?<!\p{L})" + Regex.Escape(s.NazivZemlje) + @"(?!\p{L})", RegexOptions.IgnoreCase).Value;
            if (upisano.Length == 0) upisano = s.NazivZemlje;
            // predlog: ISO šifra sa nazivom zemlje („RS (Srbija)“) – adresa sa lista se ne prepisuje (OCR može biti nepouzdan)
            var naziv = upisano == upisano.ToUpperInvariant() ? CultureInfo.GetCultureInfo("sr-Latn-RS").TextInfo.ToTitleCase(upisano.ToLowerInvariant()) : upisano;
            string predlog = iso.Length > 0 ? iso + " (" + naziv + ")" : null;
            yield return P(broj, "iso-" + broj).S("country", upisano)
                .D((k.U.Engleski ? "Country entered in words („" : "Zemlja je upisana rečima („") + upisano + "“" + (iso.Length > 0 ? (k.U.Engleski ? ") instead of the ISO 3166 code " : ") umesto ISO 3166 šifre ") + iso + "." : ")."))
                .Pr(predlog);
        }

        private static IEnumerable<Pogodak> R1(Kontekst k) => StranaDelovi(k, k.C.R1, "1", true);
        private static IEnumerable<Pogodak> R4(Kontekst k) => StranaDelovi(k, k.C.R4, "4", false);

        private static bool EuRazmena(Kontekst k) =>
            Eu.Contains((k.C.R1.SifraZemlje ?? "").ToUpperInvariant()) && Eu.Contains((k.C.R4.SifraZemlje ?? "").ToUpperInvariant());

        private static IEnumerable<Pogodak> R1Vat(Kontekst k)
        {
            if (EuRazmena(k) && Prazno(k.C.R1.Pdv)) yield return P("1", "pdv-1");
        }

        private static IEnumerable<Pogodak> R4Vat(Kontekst k)
        {
            if (EuRazmena(k) && Prazno(k.C.R4.Pdv)) yield return P("4", "pdv-4");
        }

        private static readonly HashSet<string> R7SamoZnacenje = new HashSet<string> { "1", "2", "6", "7", "8", "24" };

        private static IEnumerable<Pogodak> R7(Kontekst k)
        {
            if (!k.C.R7.Popunjena || !k.Citljivo(k.C.R7, "7")) yield break;
            foreach (var s in k.C.R7.Stavke)
            {
                var sifra = Cifre(s.Sifra);
                if (sifra.Length == 0) continue;
                var def = k.P.R7[sifra];
                if (def == null) continue;
                if (Prazno(s.Tekst))
                    yield return P("7", "r7-" + sifra).S("code", "7." + sifra).S("requires", (string)def["requires"] == "meaning_text" ? (string)def["text"] : (string)def["requires"]);
            }
        }

        private static IEnumerable<Pogodak> R9(Kontekst k)
        {
            if (k.C.R9.Popunjena) yield break;
            var potrebni = R9Potrebni(k);
            if (potrebni.Count > 0 || k.C.R21.Mrn.Count > 0 || k.C.R21.Arc.Count > 0)
                yield return P("9", "r9-prazno").D((k.U.Engleski ? "Box 9 is empty; enter: " : "Rubrika 9 je prazna; navesti: ")
                    + string.Join(", ", potrebni.GroupBy(d => d.Tip).Select(g => NazivTipa(g.Key, k.U.Engleski) + " (" + Fajlovi(g) + ")")) + ".")
                    .Pr(string.Join("; ", potrebni.Select(d => d.Tip).Distinct().Select(t => (SifraPriloga(t, k.U.Engleski) + " " + NazivTipa(t, k.U.Engleski)).Trim())));
        }

        /// <summary>UN/EDIFACT 1001 šifra priloga za predlog upisa u r9.</summary>
        internal static string SifraPriloga(string tip, bool en = false)
        {
            switch (tip)
            {
                case TipDokumenta.SpisakKola: return en ? "<code>" : "<šifra>";   // v2.8: šifra za spisak kola nije potvrđena (271 = packing list)
                case TipDokumenta.IzvoznaDeklaracija: return "830";
                case TipDokumenta.TranzitnaDeklaracija: return "820";
                case TipDokumenta.Faktura: return "380";
                case TipDokumenta.Eur1: return "865";
                case TipDokumenta.UverenjeOPoreklu: return "861";
                default: return "";
            }
        }

        private static IEnumerable<Pogodak> R10(Kontekst k)
        {
            var r = k.C.R10;
            if (!r.Popunjena) { yield return P("10", "r10"); yield break; }
            if (k.Citljivo(r, "10") && (Prazno(r.Stanica) || Prazno(r.Zemlja))) yield return P("10", "r10");
        }

        private static IEnumerable<Pogodak> R12(Kontekst k)
        {
            if (!k.C.R12.Popunjena && Prazno(k.C.R10.SifraStanice)) yield return P("12", "r12");
        }

        /// <summary>R14 (upozorenje, v2.7): samo kada je r14 prazna – preporuka da se upiše broj ponude KP. Bilo kakav broj je dovoljan.</summary>
        private static IEnumerable<Pogodak> R14(Kontekst k)
        {
            var r = k.C.R14;
            if (r.Popunjena || r.Status == StatusRubrike.Necitljivo) yield break;
            // v2.9.2 (1): broj ponude KP je već naveden u r7 ili r13 („KP Ponuda 8957“) – preporuka nije potrebna
            if (PonudaKonteksta(k).Broj.HasValue) yield break;
            yield return P("14", "r14");
        }

        private static IEnumerable<Pogodak> R16(Kontekst k)
        {
            var r = k.C.R16;
            if (!r.Popunjena) { yield return P("16", "r16").D(k.U.Engleski ? "(box is empty)" : "(rubrika je prazna)"); yield break; }
            if (!k.Citljivo(r, "16")) yield break;
            var fali = new List<string>();
            if (Prazno(r.Mesto) && Prazno(r.SifraStanice)) fali.Add(k.U.Engleski ? "place/station" : "mesto/stanica");
            // Kućice mesec–dan–čas sa mastilom koje OCR ne može da pročita: upozorenje o čitljivosti; prazne kućice: greška.
            var kuciceNecitljive = Prazno(r.Datum) && r.KuciceStanje == StatusRubrike.Necitljivo;
            if (kuciceNecitljive) k.Necitljive.Add("16");
            else
            {
                if (Prazno(r.Datum)) fali.Add(k.U.Engleski ? "date" : "datum");
                if (Prazno(r.Cas)) fali.Add(k.U.Engleski ? "time (hour)" : "vreme (čas)");
            }
            if (fali.Count > 0) yield return P("16", "r16").D((k.U.Engleski ? "Missing: " : "Nedostaje: ") + string.Join(", ", fali) + ".");
        }

        /// <summary>R18-SPISAK (upozorenje, v2.7 – jedini nalaz za r18): r18 upućuje na spisak kola, a spisak nije priložen.</summary>
        private static IEnumerable<Pogodak> R18Spisak(Kontekst k)
        {
            if (k.SpiskoviSaDosijeom.Count > 0 || !R18PominjeSpisak(k)) yield break;   // v2.8.4: spisak iz dosijea pošiljke je priložen
            yield return P("18", "r18-spisak").D((k.U.Engleski ? "Entered: „" : "Upisano: „") + Regex.Replace(k.C.R18.Tekst ?? "", @"\s+", " ").Trim() + "“.");
        }

        private static IEnumerable<Pogodak> R20(Kontekst k)
        {
            if (!k.C.R20.Popunjena) yield return P("20", "r20");
        }

        private static (string kod, string mesto)? Incoterm(Kontekst k)
        {
            // „D A F“ u kućicama obrasca → „DAF“
            var t = k.C.R20.Tekst ?? "";
            foreach (var kod in Incoterms) t = Regex.Replace(t, @"(?<![A-Za-z])" + kod[0] + @"\s" + kod[1] + @"\s" + kod[2] + @"(?![A-Za-z])", kod);
            foreach (var kod in Incoterms)
            {
                var m = Regex.Match(t, @"(?<![A-Za-z])" + kod + @"(?![A-Za-z])");
                if (!m.Success) continue;
                var mesto = t.Substring(m.Index + m.Length);
                mesto = Regex.Replace(mesto, @"\bINCOTERMS?\s*(20\d\d)?\b", "", RegexOptions.IgnoreCase);
                mesto = mesto.Trim(' ', ':', '-', '–', ',', '.', '(', ')', '"', '„', '“', '\n', '\r', '\t');
                // v3.0 (odluka korisnika 18.11., mail 782 „Franko prevoznina (X) x Dragoman / FCA“): mesto upisano u koloni „jusqu'à / bis“
                // (isti red sa „Franko“, ispred šifre) je mesto uz Incoterms – nema upozorenja da mesto nedostaje
                if (mesto.Length == 0) mesto = MestoIzKoloneBis(t.Substring(0, m.Index) + " " + t.Substring(m.Index + m.Length));
                return (kod, mesto);
            }
            return null;
        }

        /// <summary>
        /// v3.0: mesto iz kolone „jusqu'à / bis / do“ rubrike 20 – tekst rubrike bez natpisa polja (Franko, prevoznina, Frachtfrei, port payé,
        /// oznaka „X“, Incoterms). Reč od bar 3 slova je mesto („Dragoman“); inače prazno.
        /// </summary>
        internal static string MestoIzKoloneBis(string tekst)
        {
            var t = Regex.Replace(tekst ?? "", @"(?i)\b(franko\w*|franco\w*|frankatur\w*|nefranko|unfrei|prevoznin\w*|troškov\w*|troskov\w*|plaćanj\w*|placanj\w*|frachtfrei|fracht\w*|kostenfrei|port|pay[ée]|jusqu'?\s*[àa]|bis|do|incoterms?|20(10|20)|carriage|paid|free|delivered|to)\b", " ");
            t = Regex.Replace(t, @"\(\s*[xX]\s*\)|(?<!\p{L})[xX](?!\p{L})", " ");
            var reci = Regex.Matches(t, @"\p{L}[\p{L}\-\.]{2,}").Cast<Match>().Select(x => x.Value.Trim('.', '-')).Where(x => x.Length >= 3).ToList();
            return reci.Count == 0 ? "" : string.Join(" ", reci);
        }

        private static IEnumerable<Pogodak> R20Inco(Kontekst k)
        {
            if (!k.C.R20.Popunjena || !k.Citljivo(k.C.R20, "20")) yield break;
            var inc = Incoterm(k);
            if (inc != null && Prazno(inc.Value.mesto))
            {
                var pg = P("20", "r20-inco").S("code", inc.Value.kod);
                // v2.8: sken – mesto je često u koloni „jusqu'à / bis“ iste rubrike, koju OCR ne pročita: upozorenje, ne greška
                if (k.Ocr)
                {
                    pg.Tezina = Tezina.Upozorenje;
                    pg.Dodatak += k.U.Engleski ? " (Scan: the place may be in the column „jusqu'à / bis“ – check manually.)" : " (Sken: mesto može biti u koloni „jusqu'à / bis“ – proveriti ručno.)";
                }
                yield return pg;
            }
        }

        /// <summary>X30 (upozorenje): broj kola u r21 i broj u nazivu fajla („…_31 wgs.pdf“) se razlikuju.</summary>
        private static IEnumerable<Pogodak> X30(Kontekst k)
        {
            var r21 = BrojKolaUR21(k);
            var fajl = k.Dok?.Fajl ?? "";
            var m = Regex.Match(System.IO.Path.GetFileNameWithoutExtension(fajl), @"(?<!\d)(\d{1,3})\s*[_\- ]?\s*(?:wgs?|wagons?|wagen|kola|vagona|kocsi|carri)\b", RegexOptions.IgnoreCase);
            if (r21 == 0 || !m.Success || !int.TryParse(m.Groups[1].Value, out var uFajlu) || uFajlu == r21) yield break;
            var opis = Regex.Match(k.C.R21.Tekst ?? "", @"(?<![\d.,/])\d{1,3}\s*(?:\p{L}+\s+){0,3}?(?:kola|vagona|vagon[ai]?|wagons?|wgs?\.?|wagen|kocsi|carri|vagoane)\b", RegexOptions.IgnoreCase).Value;
            yield return P("21", "x30").S("r21", "„" + opis + "“").S("fajl", System.IO.Path.GetFileName(fajl)).S("broj", m.Value);
        }

        /// <summary>R20-INCO-ZASTARELO: DAF/DDU (nisu u Incoterms 2020) ili mesto samo brojčanom šifrom („711“) – upozorenje.</summary>
        private static IEnumerable<Pogodak> R20IncoZastarelo(Kontekst k)
        {
            if (!k.C.R20.Popunjena || !k.Citljivo(k.C.R20, "20")) yield break;
            var inc = Incoterm(k);
            if (inc == null) yield break;
            var (kod, mesto) = inc.Value;
            var problemi = new List<string>();
            if (kod == "DAF" || kod == "DDU")
                problemi.Add(k.U.Engleski ? kod + " is not an Incoterms 2020 code (obsolete)" : kod + " nije u važećim Incoterms 2020 (zastarela šifra)");
            var prvo = Regex.Match(mesto ?? "", @"^\S+").Value;
            if (Regex.IsMatch(prvo, @"^\d{2,6}$") && !Regex.IsMatch(mesto, @"\p{L}{3,}"))
                problemi.Add(k.U.Engleski ? "the place is given only as the code „" + prvo + "“" : "mesto je upisano samo kao šifra „" + prvo + "“");
            if (problemi.Count > 0)
                yield return P("20", "r20-inco-zastarelo").S("problem", string.Join(k.U.Engleski ? "; " : "; ", problemi)).S("code", kod);
        }

        /// <summary>R21-MRN99: MRN upisan samo u rubrici 99 (carinske napomene), a rubrika 21 nema MRN – upozorenje.</summary>
        private static IEnumerable<Pogodak> R21Mrn99(Kontekst k)
        {
            if (k.C.R21.Mrn.Count > 0) yield break;
            var r99 = (k.C.R99.Tekst ?? "").ToUpperInvariant();
            var m = Regex.Match(r99, @"(?:\b(T2L?|T1|TS?|E|EX)\s*[@:\-]?\s*)?(\d{2}[A-Z]{2}[A-Z0-9]{14})(?![A-Z0-9])");
            if (!m.Success || (k.C.R21.Tekst ?? "").ToUpperInvariant().Contains(m.Groups[2].Value)) yield break;
            var oznaka = m.Groups[1].Value;
            var pref = oznaka.StartsWith("E") ? "E MRN" : "T MRN";
            yield return P("21", "r21-mrn99").S("mrn", (oznaka.Length > 0 ? oznaka + " " : "") + m.Groups[2].Value)
                .D((k.U.Engleski ? "Box 99: „" : "Rubrika 99: „") + (k.C.R99.Tekst ?? "").Trim() + "“.")
                .Pr(pref + " " + m.Groups[2].Value + (k.U.Engleski ? " (in box 21)" : " (u rubrici 21)"));
        }

        private static readonly HashSet<string> NeznacajneReci = new HashSet<string>
            { "gr", "granica", "granice", "border", "grenze", "station", "stanica", "bahnhof", "rail", "railway", "terminal", "do", "to", "bis", "incoterms", "2010", "2020" };

        /// <summary>R20-RUTA: mesto uz Incoterms nije među stanicama prevoznog puta navedenim na listu (r16, r10, r13, r50, r57).</summary>
        private static IEnumerable<Pogodak> R20Ruta(Kontekst k)
        {
            if (!k.C.R20.Popunjena || !k.Citljivo(k.C.R20, "20")) yield break;
            var inc = Incoterm(k);
            if (inc == null) yield break;
            var reci = N(inc.Value.mesto).Split(' ').Where(r => r.Length >= 4 && !NeznacajneReci.Contains(r)).ToList();
            if (reci.Count == 0) yield break;
            var put = " " + N(string.Join(" ", new[]
            {
                k.C.R16.Mesto, k.C.R16.Tekst, k.C.R10.Stanica, k.C.R10.Tekst, Tekst(k.C.R13), k.C.R50.Tekst,
                string.Join(" ", k.C.R57.Prevoznici.Select(x => x.Deonica)), k.C.R57.Tekst, k.C.R4.Adresa, k.C.R1.Adresa,
            })) + " ";
            if (!reci.Any(r => put.Contains(" " + r)))
                yield return P("20", "r20-ruta").S("code", inc.Value.kod).S("place", inc.Value.mesto);
        }

        private static IEnumerable<Pogodak> R21(Kontekst k)
        {
            if (!k.C.R21.Popunjena) yield return P("21", "r21");
        }

        private static IEnumerable<Pogodak> R23(Kontekst k)
        {
            var r = k.C.R23;
            if (r.Popunjena && k.Citljivo(r, "23") && N(r.Tekst) != "x") yield return P("23", "rid");
        }

        private static IEnumerable<Pogodak> R24(Kontekst k)
        {
            var r = k.C.R24;
            // Više kola: r21 upućuje na spisak kola, a NHM je uz svaka kola na priloženom spisku
            if (!r.Popunjena && Pominje(k.C.R21.Tekst, TipDokumenta.SpisakKola) && k.Spiskovi.Any(x => x.s.Kola.Count > 0 && x.s.Kola.All(kk => !Prazno(kk.Nhm)))) yield break;
            if (!r.Popunjena) { yield return P("24", "r24"); yield break; }
            if (!k.Citljivo(r, "24")) yield break;
            // v2.7: greška samo kada NHM ima manje od 4 cifre (4–5 cifara: preporuka R24-NHM6)
            foreach (var s in SifreR24(k).Where(s => Cifre(s).Length < 4))
                yield return P("24", "r24").D((k.U.Engleski ? "Entered: " : "Upisano: ") + s + " (" + Cifre(s).Length + (k.U.Engleski ? " digits)." : (Cifre(s).Length == 1 ? " cifra)." : " cifre).")))
                    .Pr(PredlogNhm(k, s));
        }

        private static List<string> SifreR24(Kontekst k)
        {
            var r = k.C.R24;
            var sifre = r.Stavke.Select(s => Regex.Replace(s.Sifra + s.Tekst, @"[\s\.]", "")).Where(x => x.Length > 0).ToList();
            if (sifre.Count == 0) sifre.Add(Regex.Replace(r.Tekst ?? "", @"[\s\.]", ""));
            return sifre.Where(x => x.Length > 0).ToList();
        }

        /// <summary>R24-NHM6 (preporuka): NHM u r24 ima 4 ili 5 cifara – poželjno 6 (sa šifrom iz pratećeg dokumenta ako postoji).</summary>
        private static IEnumerable<Pogodak> R24Nhm6(Kontekst k)
        {
            var r = k.C.R24;
            if (!r.Popunjena || !k.Citljivo(r, "24")) yield break;
            foreach (var s in SifreR24(k).Select(Cifre).Where(c => c.Length == 4 || c.Length == 5).Distinct())
            {
                var (sest, izvor) = Nhm6(k, s);
                yield return P("24", "r24-nhm6-" + s).S("izvor", izvor ?? "")
                    .D((k.U.Engleski ? "Entered: " : "Upisano: ") + s + " (" + s.Length + (k.U.Engleski ? " digits)." : " cifre)."))
                    .Pr(sest);
            }
        }

        /// <summary>
        /// NHM sa 6 cifara za upisanu šifru od 4–5 cifara: iz carinskog dokumenta (T1, JCI) ili sa spiska kola; za prazna kola
        /// (9921/9922) dopuna nulama. Izvor: „ (u T1: 262190)“.
        /// </summary>
        internal static (string sifra, string izvor) Nhm6(Kontekst k, string upisano)
        {
            var cif = Cifre(upisano);
            if (Regex.IsMatch(cif, "^992[12]")) return (cif.PadRight(6, '0'), null);   // prazna kola: 9922 → 992200
            string Oznaka(string tip)
            {
                switch (tip)
                {
                    case TipDokumenta.TranzitnaDeklaracija: return "T1";
                    case TipDokumenta.IzvoznaDeklaracija: return "JCI";
                    case TipDokumenta.SpisakKola: return k.U.Engleski ? "wagon list" : "spisku kola";
                    default: return k.U.Engleski ? "customs document" : "carinskom dokumentu";
                }
            }
            var tarife = k.Svi.Where(d => TipDokumenta.JeCarinski(d.Tip)).SelectMany(d => d.Carinski.SelectMany(c => c.Tarife.Select(t => (t: Cifre(t), d.Tip))))
                .Concat(k.Spiskovi.SelectMany(s => s.s.Kola.Select(x => (t: Cifre(x.Nhm), Tip: TipDokumenta.SpisakKola))))
                .Where(x => x.t.Length >= 6).ToList();
            // najduže poklapanje početka (NHM 2620 → tarifna oznaka 262190 u T1), najmanje 3 cifre
            IGrouping<string, (string t, string Tip)> nadjeno = null;
            for (var duz = cif.Length; duz >= 3 && nadjeno == null; duz--)
                nadjeno = tarife.Where(x => x.t.StartsWith(cif.Substring(0, duz), StringComparison.Ordinal)).GroupBy(x => x.t.Substring(0, 6)).OrderByDescending(g => g.Count()).FirstOrDefault();
            if (nadjeno != null)
                return (nadjeno.Key, " (" + (k.U.Engleski ? "in " : "u ") + Oznaka(nadjeno.OrderBy(x => x.Tip == TipDokumenta.SpisakKola ? 1 : 0).First().Tip) + ": " + nadjeno.Key + ")");
            if (Regex.IsMatch(cif, "^992[12]")) return (cif.PadRight(6, '0'), null);
            return (null, null);
        }

        /// <summary>Predlog NHM iz carinskog dokumenta u mailu: tarifna oznaka (prvih 6 cifara HS) koja počinje kao upisana šifra.</summary>
        private static string PredlogNhm(Kontekst k, string upisano)
        {
            var tarife = k.Svi.Where(d => TipDokumenta.JeCarinski(d.Tip)).SelectMany(d => d.Carinski.SelectMany(c => c.Tarife.Select(t => (t, d)))).ToList();
            var cif = Cifre(upisano);
            foreach (var duz in new[] { 4, 3, 2 })
            {
                if (cif.Length < duz) continue;
                var nadjeno = tarife.Where(x => x.t.StartsWith(cif.Substring(0, duz), StringComparison.Ordinal)).GroupBy(x => x.t).OrderByDescending(g => g.Count()).FirstOrDefault();
                if (nadjeno != null)
                    return nadjeno.Key + " (" + (k.U.Engleski ? "tariff code in: " : "tarifna oznaka u dokumentu: ") + NazivTipa(nadjeno.First().d.Tip, k.U.Engleski) + ", " + Fajlovi(nadjeno.Select(x => x.d).Distinct()) + ")";
            }
            return null;
        }

        /// <summary>
        /// Prazna kola (R25-MASA): NHM 9922 u r24 („9922“, „992200“, „99 22“), u r30 označen CUV (a ne CIM), ili u r21/r24 izričito
        /// „prazna kola / empty wagons / Leerwagen / üres kocsi / wagons vides / vagoni vuoti…“.
        /// </summary>
        internal static bool PraznaKola(Kontekst k)
        {
            var nhm = k.C.R24.Stavke.Select(s => Cifre(s.Sifra + s.Tekst)).Concat(new[] { Cifre(k.C.R24.Tekst) });
            if (nhm.Any(x => x.StartsWith("9922", StringComparison.Ordinal))) return true;
            if ((k.C.R30.Cuv && !k.C.R30.Cim) || k.C.Obrazac == "CUV") return true;
            var t = (k.C.R21.Tekst ?? "") + " " + (k.C.R24.Tekst ?? "");
            return Regex.IsMatch(t, @"(?<!\p{L})(prazn\p{L}* (kola|vagon\p{L}*)|empty (private )?wagons?|leerwagen\p{L}*|leere wagen|[üu]res kocsi\p{L}*|wagons? vides?|vagoni vuoti)(?!\p{L})", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// R25-MASA (v2.7.1, zamenjuje R25): masa je obavezna za tovarena kola (greška), za prazna kola poželjna (preporuka).
        /// Prazna r25 koja upućuje (r25/r21) na priloženi spisak kola sa upisanom masom nije greška. Nečitljiva r25 → CITLJIVOST.
        /// </summary>
        private static IEnumerable<Pogodak> R25Masa(Kontekst k)
        {
            var r = k.C.R25;
            if (r.Popunjena && !k.Citljivo(r, "25")) yield break;          // nečitljivo (OCR): upozorenje „Rubrika 25 nije čitljiva“, ne greška
            var prazna = !r.Popunjena || (Prazno(r.Ukupno) && r.Pojedinacne.Count == 0 && Prazno(r.Tara));
            if (!prazna) yield break;
            if (PraznaKola(k))
            {
                var def = k.P.PoId["R25-MASA"].Json;
                yield return new Pogodak("25", "r25-masa") { Tezina = Tezina.Preporuka, Poruka = (string)def[k.U.Engleski ? "message_prazna_en" : "message_prazna"] }
                    .Pr((string)def[k.U.Engleski ? "primer_prazna_en" : "primer_prazna"]);
                yield break;
            }
            // masa na priloženom spisku kola, na koji upućuje r25 ili r21
            var upucuje = Pominje(r.Tekst, TipDokumenta.SpisakKola) || Pominje(k.C.R21.Tekst, TipDokumenta.SpisakKola)
                          || Regex.IsMatch(N((r.Tekst ?? "") + " " + (k.C.R21.Tekst ?? "")), @"\b(po spisku|prema spisku|vidi spisak|siehe|see (wagon )?list|lt spisak)\b");
            if (upucuje && k.Spiskovi.Any(s => s.s.MasaUpisana)) yield break;
            // v2.8 (CIM COAL 16): priložen spisak kola SA MASAMA, a r25 prazna – preporuka, ne greška, i bez upućivanja u r25/r21
            if (k.Spiskovi.Any(s => s.s.MasaUpisana))
            {
                yield return new Pogodak("25", "r25-masa-spisak") { Tezina = Tezina.Preporuka,
                    Poruka = k.U.Engleski ? "Box 25: the masses are on the attached wagon list – we recommend entering the total mass in box 25 as well (or a reference to the wagon list)."
                                          : "Rubrika 25: mase su na priloženom spisku kola – preporučujemo da ukupnu masu upišete i u rubriku 25 (ili uputite na spisak kola)." }
                    .Pr(k.U.Engleski ? "e.g. 1 082 918 kg, see wagon list" : "npr. 1 082 918 kg, vidi spisak kola");
                yield break;
            }
            // v2.8.1 (CIM COAL 16 + wagon list 16.xlsx): spisak je priložen, ali bez mase robe – konkretan opis umesto „spisak nije priložen“
            var spisakBezMase = k.Spiskovi.Count > 0
                ? (k.U.Engleski ? " The attached wagon list (" : " Priloženi spisak kola (") + Fajlovi(k.Spiskovi.Select(s => s.dok)) + (k.U.Engleski ? ") has no mass of the goods" : ") nema masu robe")
                  + (k.Spiskovi.All(s => s.s.Kola.Count > 0 && s.s.Kola.All(x => x.Prazna)) && !PraznaKola(k)
                      ? (k.U.Engleski ? " – all wagons on it are marked as empty (only the tare is entered), although box 21 describes goods." : " – sva kola su na njemu označena kao prazna (upisana je samo tara), a u rubrici 21 je roba.")
                      : ".")
                : "";
            // v2.8.2: u polje za masu je upisano nešto što nije broj (natpis, tačkice, tekst) – greška „masa nije upisana kao broj“
            // (i kada parser rubriku vodi kao praznu, a u njoj su samo oznake i tačkice: „T= ……… / N= ……… / G= ………“)
            var reci25 = k.C.ReciRubrika != null && k.C.ReciRubrika.TryGetValue("25", out var rr) ? string.Join("\n", Citanje.Raspored.Linije(rr).Select(l => l.Tekst)) : "";
            var tekst25 = !Prazno(r.Tekst) ? r.Tekst : reci25;
            if ((r.Popunjena || r.Status == StatusRubrike.Oznake || Regex.IsMatch(tekst25, @"[\p{L}.…]")) && !Prazno(tekst25) && !upucuje && !Regex.IsMatch(tekst25, @"\d{2,}"))
            {
                var upisano = Regex.Replace(Regex.Replace(tekst25, @"\s*\n\s*", " / "), @"\s+", " ").Trim();
                yield return new Pogodak("25", "r25-masa")
                {
                    Poruka = k.U.Engleski ? "Box 25: the mass is not entered as a number." : "Rubrika 25: masa nije upisana kao broj.",
                }.D((k.U.Engleski ? "The mass is not entered as a number. Entered: „" : "Masa nije upisana kao broj. Upisano: „") + upisano + "“." + spisakBezMase);
                yield break;
            }
            yield return P("25", "r25-masa").D((r.Status == StatusRubrike.Oznake ? (k.U.Engleski ? "Only the form labels are entered, without values." : "Upisane su samo oznake, bez vrednosti.")
                : upucuje && k.Spiskovi.Count == 0 ? (k.U.Engleski ? "Box 25 refers to the wagon list, but no wagon list with masses is attached." : "Rubrika 25/21 upućuje na spisak kola, a spisak sa masom nije priložen.")
                : (k.U.Engleski ? "(box is empty)" : "(rubrika je prazna)")) + spisakBezMase);
        }

        // iznos: 1–3 cifre sa grupama hiljada ili ceo broj bez vodeće nule, opciono decimale („,-“), opciono oznaka valute pre ili posle
        private const string ValutaOznaka = @"(?:[A-Za-z]{3}|€|\$|£|din\.?|kn)";
        private static readonly Regex IznosRegex = new Regex(
            @"^(?:" + ValutaOznaka + @"\s*)?(?:[1-9]\d{0,2}(?:[ .,'’]\d{3})+|[1-9]\d*|0)(?:[.,](?:\d{1,2}|-{1,2}))?(?:\s*" + ValutaOznaka + @")?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>v3.0: tekst rubrike 26–28 liči na iznos („1.250,00 EUR“, „EUR 980“, „1200“) – ne na OCR šum („56: 0128“, „0128“, „78 nnarj“).</summary>
        internal static bool LiciNaIznos(string tekst)
        {
            var t = Regex.Replace(tekst ?? "", @"\s+", " ").Trim().Trim(';', ':', '.', ',').Trim();
            if (t.Length == 0 || t == "0") return false;
            return IznosRegex.IsMatch(t);
        }

        private static IEnumerable<Pogodak> Iznos(Kontekst k, Iznos r, string broj, bool napomena = false)
        {
            // Neobavezne rubrike (26–28): mastilo bez pročitanog teksta (sitni natpisi obrasca) nije razlog za upozorenje.
            if (!r.Popunjena || (r.Status == StatusRubrike.Necitljivo && Prazno(r.Tekst)) || !k.Citljivo(r, broj)) yield break;
            // v3.0 (odluka korisnika 18.11.): upozorenje za valutu samo kada je u rubrici pouzdano pročitan IZNOS (cifre, opciono decimale
            // i oznaka valute). OCR šum („56: 0128“, „6. 2% 81“, „78 nnarj“) ne pokreće pravilo – najviše INTERNO „r26 nečitljiva“.
            if (!LiciNaIznos(r.Tekst))
            {
                if (napomena) yield return new Pogodak(broj, "valuta-" + broj + "-necitljivo")
                {
                    Tezina = Tezina.Info, SamoInterno = true,
                    Poruka = k.U.Engleski ? "Box " + broj + " is not readable as an amount – check manually." : "r" + broj + " nečitljiva – proveriti ručno.",
                }.D((k.U.Engleski ? "Agent read: „" : "Agent je pročitao: „") + Regex.Replace(r.Tekst ?? "", @"\s+", " ").Trim() + "“ (" + (k.U.Engleski ? "not an amount" : "ne liči na iznos") + ").");
                yield break;
            }
            if (Prazno(r.Vrednost) || !k.P.Valute.Contains((r.Valuta ?? "").Trim().ToUpperInvariant()))
                yield return P(broj, "valuta-" + broj);
        }

        private static IEnumerable<Pogodak> R29(Kontekst k)
        {
            var r = k.C.R29;
            if (!r.Popunjena) { yield return P("29", "r29").D(k.U.Engleski ? "(box is empty)" : "(rubrika je prazna)"); yield break; }
            if (!k.Citljivo(r, "29")) yield break;
            var fali = new List<string>();
            // mesto ima bar dva slova („.“ ili „13.11.2026.“ nije mesto – cuv za 1 vagon)
            if (Prazno(r.Mesto) || !Regex.IsMatch(r.Mesto, @"\p{L}{2}")) fali.Add(k.U.Engleski ? "place" : "mesto");
            if (Prazno(r.Datum)) fali.Add(k.U.Engleski ? "date of issue (year-month-day)" : "datum ispostavljanja (godina-mesec-dan)");
            if (fali.Count > 0) yield return P("29", "r29").D((k.U.Engleski ? "Entered: " : "Upisano: ") + Regex.Replace(r.Tekst ?? "", @"\s+", " ").Trim() + ". " + (k.U.Engleski ? "Missing: " : "Nedostaje: ") + string.Join(", ", fali) + ".")
                .Pr(!Prazno(r.Mesto) && Prazno(r.Datum) && k.Citljivo(r, "29") ? r.Mesto.Trim() + (k.U.Engleski ? ", year-month-day" : ", godina-mesec-dan") : null);
            // "mesto != naziv firme"
            if (!Prazno(r.Mesto) && (N(r.Mesto) == N(k.C.R1.Naziv) || k.P.Kp.JeKpNaziv(r.Mesto) || Regex.IsMatch(N(r.Mesto), @"\b(d o o|doo|a d|gmbh|ltd|s a|srl|kft|as)\b")))
                yield return P("29", "r29-mesto").D((k.U.Engleski ? "Instead of a place, a company name is entered: „" : "Umesto mesta upisan je naziv firme: „") + r.Mesto + "“.");
            // "datum nije u budućnosti" – isti problem kao X25 (prikazuje se jednom)
            var d = Datum(r.Datum, k.U.ReferentniDatum.Year);
            if (d.HasValue && d.Value.Date > k.U.ReferentniDatum.Date)
                yield return P("29", "r29-buducnost").S("date", d.Value.ToString("dd.MM.yyyy"))
                    .D((k.U.Engleski ? "The date " : "Datum ") + d.Value.ToString("dd.MM.yyyy") + (k.U.Engleski ? " is in the future." : " je u budućnosti."));
            // "datum <= r16": priručnik (Prilog 2, rubr. 29) to ne propisuje → samo X14 (upozorenje), vidi odluku u istoriji pravila v2.1.
        }

        private static IEnumerable<Pogodak> R30(Kontekst k)
        {
            if (k.C.Obrazac == "CUV") yield break;
            var r = k.C.R30;
            if (!k.Citljivo(r, "30") || r.Cim) yield break;
            yield return P("30", "r30").D(k.U.Engleski ? "(the CIM box is empty)" : "(polje CIM nije označeno)");
        }

        private static IEnumerable<Pogodak> R49(Kontekst k)
        {
            // kućice bez ijedne cifre (samo natpis / pregrade) = prazno
            var r = k.C.R49;
            if (!r.Popunjena || (r.Status == StatusRubrike.Necitljivo && !Regex.IsMatch(r.Tekst ?? "", @"\d"))) yield return P("49", "r49");
        }

        private static IEnumerable<Pogodak> R50(Kontekst k)
        {
            if (!k.C.R50.Popunjena) yield return P("50", "r50");
        }

        private static IEnumerable<Pogodak> R58A(Kontekst k)
        {
            var r = k.C.R58a;
            // r62 = 3212 (list izdao KP): r58a proverava KP2 (3212 ili KP) – isti problem se ne prijavljuje dvaput
            if (k.P.Kp.JeKpSifra(k.C.R62.Preduzece)) yield break;
            // r62 i r58a obe prazne na KP listu: prijavljuje se jednom, kao R62. v2.8.4 (odluka 2026-11-14): kod spoljnog
            // pošiljaoca je prazna r62 samo napomena, a r58a popunjava pošiljalac – prazna r58a je njegova greška (CUV_257)
            if (!r.Popunjena && !k.C.R62.Popunjena && k.Kp) yield break;
            // v3.0 (2.9.3 tačka 3): prazna r58a zavisi od izdavaoca lista (preduzeće u r62). KP (3212) → KP2 (greška, predlog 3212).
            // Drugi prevoznik → samo upozorenje, predlog = šifra i naziv izdavaoca iz r62 – nikad 3212. r62 nečitljiv/prazan → upozorenje „proveriti“.
            if (!r.Popunjena)
            {
                var izdavalac = IzdavalacR62(k);
                if (izdavalac == null)
                {
                    var pg0 = P("58a", "r58a-proveri").D(k.U.Engleski ? "(the company code in box 62 could not be read)" : "(šifra preduzeća u r62 nije pročitana)");
                    pg0.Tezina = Tezina.Upozorenje;
                    pg0.Poruka = k.U.Engleski ? "Box 58a is empty – check (the contractual carrier is the company that issued the consignment note, box 62)." : "Rubrika 58a je prazna – proveriti (ugovorni prevoznik je preduzeće koje je izdalo tovarni list, r62).";
                    pg0.Predlog = k.U.Engleski ? "code and name of the carrier from box 62" : "šifra i naziv prevoznika iz r62";
                    yield return pg0; yield break;
                }
                var naziv = NazivPrevoznika(k, izdavalac);
                var pg1 = P("58a", "r58a").D(k.U.Engleski ? "(box is empty)" : "(rubrika je prazna)");
                pg1.Tezina = Tezina.Upozorenje;
                pg1.Poruka = k.U.Engleski ? "Box 58a: enter the contractual carrier – the company that issued the consignment note (box 62: " + izdavalac + ")."
                                          : "Rubrika 58a: upisati ugovornog prevoznika – preduzeće koje je izdalo tovarni list (r62: " + izdavalac + ").";
                pg1.Predlog = (izdavalac + " " + (naziv ?? "")).Trim();
                yield return pg1; yield break;
            }
            if (!k.Citljivo(r, "58a")) yield break;
            // v2.7: dovoljna je šifra preduzeća (UIC) ili prepoznatljiv naziv prevoznika; naziv, adresa i potpis nisu obavezni.
            if (ImaPrevoznika58a(k, r)) yield break;
            var pg = P("58a", "r58a").D((k.U.Engleski ? "Entered: „" : "Upisano: „") + Regex.Replace(r.Tekst ?? "", @"\s+", " ").Trim()
                + (k.U.Engleski ? "“ – no company code (UIC) and no carrier name." : "“ – nema šifre preduzeća (UIC) ni naziva prevoznika."));
            if (PecatNecitljiv(k, r))
            {
                pg.Tezina = Tezina.Upozorenje;
                pg.Dodatak += k.U.Engleski ? " (OCR reading of the stamp is unreliable – check manually.)" : " (OCR čitanje pečata nije pouzdano – proveriti ručno.)";
            }
            yield return pg;
        }

        /// <summary>v3.0 (2.9.3 tačka 3): šifra preduzeća izdavaoca iz r62 (4 cifre, pouzdano pročitana) – null ako je r62 prazna ili nečitljiva.</summary>
        internal static string IzdavalacR62(Kontekst k)
        {
            var s = Cifre(k.C.R62.Preduzece);
            if (s.Length != 4 || !k.C.R62.Popunjena) return null;
            if (k.Ocr && k.C.R62.Pouzdanost < 0.6) return null;
            return s;
        }

        /// <summary>
        /// Naziv prevoznika sa date šifre iz samog dokumenta (r57, r13, r7: „3057 Bulmarket“, „BMRC 3057“, „Unicom Tranzit 3154“);
        /// KP iz pravila. Naziv se ne izmišlja – null kada ga dokument nema.
        /// </summary>
        internal static string NazivPrevoznika(Kontekst k, string sifra)
        {
            if (k.P.Kp.JeKpSifra(sifra)) return k.P.Kp.Naziv;
            string Ocisti(string x) => Regex.Replace(Regex.Replace(x ?? "", @"^\s*[A-ZŽŠČĆĐ]{2,4}\s*:\s*", ""), @"\b(UIC|CIT|code|šifra|sifra)\b|\d{4}", "", RegexOptions.IgnoreCase).Trim(' ', ',', '-', ':', '(', ')', ';', '/');
            // naziv: bar 3 slova u nizu, nije oznaka mreže („BDŽ:“, „SRB:“ – reč iza koje je dvotačka), nije KP ni reč „via/za/carrier“
            bool Dobar(string n) => Regex.IsMatch(n ?? "", @"\p{L}{3,}") && !k.P.Kp.JeKp(n)
                && !Regex.IsMatch(n, @"^(SRB|RS|BG|HU|RO|HR|BA|MK|IŽS|IZS|via|for|za|carrier|prevoznik)$", RegexOptions.IgnoreCase);
            var izR57 = Ocisti(k.C.R57.Prevoznici.FirstOrDefault(x => Cifre(x.Sifra) == sifra)?.Naziv);
            if (Dobar(izR57)) return izR57;
            foreach (var t in new[] { k.C.R57.Tekst, k.C.R13.Tekst, string.Join("\n", k.C.R13.Stavke.Select(x => x.Tekst)), k.C.R7.Tekst, string.Join("\n", k.C.R7.Stavke.Select(x => x.Tekst)) })
            {
                foreach (var red in (t ?? "").Split('\n'))
                {
                    // reči naziva odvojene jednim razmakom (tri razmaka = granica kolone, iza je deonica: „3057 B   Povelyanovo“)
                    var m = Regex.Match(red, @"(?<!\d)" + sifra + @"(?!\d) ?[-–]? ?(?<n>\p{L}[\p{L}\.&]*(?: \p{L}[\p{L}\.&]*){0,2})(?!\s*:)(?![\p{L}\.&])");
                    if (!m.Success) m = Regex.Match(red, @"(?<![\p{L}\.&])(?<n>\p{L}[\p{L}\.&]*(?: \p{L}[\p{L}\.&]*){0,2})(?!\s*:) ?(?:UIC ?)?[-–]? ?(?<!\d)" + sifra + @"(?!\d)");
                    var n = m.Success ? Ocisti(m.Groups["n"].Value) : "";
                    if (Dobar(n)) return n;
                }
            }
            return null;
        }

        /// <summary>r58a ima šifru preduzeća (UIC, 4 cifre) ili prepoznatljiv naziv prevoznika (KP u bilo kom obliku, ili reč od 4+ slova).</summary>
        private static bool ImaPrevoznika58a(Kontekst k, Rubrika r58)
        {
            var r = k.C.R58a;
            if (Regex.IsMatch(Cifre(r.Sifra), @"^\d{4}$")) return true;
            var t = k.P.Kp.OcrIspravka(r.Tekst ?? "");
            if (Regex.IsMatch(t, @"(?<![\d.,/\-])\d{4}(?![\d.,/\-])")) return true;
            if (k.P.Kp.PominjeNaziv(t)) return true;
            var reci = Regex.Matches(r.Naziv + " " + t, @"\p{L}{4,}").Cast<Match>().Select(m => m.Value.ToLowerInvariant())
                .Where(w => !new[] { "ugovorni", "prevoznik", "naziv", "adresa", "potpis", "carrier", "contractual", "name", "address", "signature", "frachtf" }.Any(w.StartsWith));
            return reci.Any();
        }

        private static IEnumerable<Pogodak> R62(Kontekst k)
        {
            var r = k.C.R62;
            if (!r.Popunjena) { yield return P("62", "r62").D(k.U.Engleski ? "(box is empty)" : "(rubrika je prazna)"); yield break; }
            // v2.8.1: sken na kome cifre r62 nisu pouzdano pročitane – interna napomena „r62 nečitljiva – proveriti ručno“, ne greška
            var nepotpuna = Prazno(r.Zemlja) || Prazno(r.Stanica) || Prazno(r.Preduzece) || Prazno(r.Broj);
            if (k.Ocr && (r.NepouzdanoCitanje || ((r.Status == StatusRubrike.Necitljivo || r.Pouzdanost < k.U.Prag || r.OcrSum) && nepotpuna)))
            {
                var procitano = string.Join(" / ", new[] { r.Zemlja, r.Stanica, r.Preduzece, r.Broj }.Select(x => Prazno(x) ? "?" : x));
                yield return new Pogodak("62", "r62-necitljivo")
                {
                    Tezina = Tezina.Info, SamoInterno = true,
                    Poruka = k.U.Engleski ? "Box 62 is not readable on the scan – check manually." : "r62 nečitljiva – proveriti ručno.",
                }.D((k.U.Engleski ? "Agent read: " : "Agent je pročitao: ") + procitano + " („" + Motor.TekstRubrike(k.C, "62") + "“).");
                yield break;
            }
            if (!k.Citljivo(r, "62")) yield break;
            var fali = new List<string>();
            if (Prazno(r.Zemlja)) fali.Add(k.U.Engleski ? "country" : "zemlja");
            if (Prazno(r.Stanica)) fali.Add(k.U.Engleski ? "station" : "stanica");
            if (Prazno(r.Preduzece)) fali.Add(k.U.Engleski ? "company code" : "šifra preduzeća");
            var obrazac = (string)k.P.PoId["R62"].Json["pattern_broj_otpravljanja"] ?? @"^\d{5}-?\d$|^\d{6}$";
            if (Prazno(r.Broj)) fali.Add(k.U.Engleski ? "consignment number" : "broj otpravljanja");
            // broj sa slovima („HER-SAD79“, Excel obrazac) se ne proverava po obliku 12345-6
            else if (!Regex.IsMatch(r.Broj, @"\p{L}") && !Regex.IsMatch(Regex.Replace(r.Broj, @"\s", ""), obrazac)) fali.Add((k.U.Engleski ? "consignment number in the format 12345-6 (entered: " : "broj otpravljanja u obliku 12345-6 (upisano: ") + r.Broj + ")");
            if (fali.Count > 0) yield return P("62", "r62").D((k.U.Engleski ? "Missing: " : "Nedostaje: ") + string.Join(", ", fali) + ".");
        }

        // =========================================================================================== r21_subrules

        /// <summary>r21 upućuje na spisak kola („Siehe Wagenliste“), a priloženi spisak ima UN brojeve – UN broj je na spisku.</summary>
        /// v3.0 (2.9.3 tačka 1): UN broj na bilo kom priloženom spisku (i po kolima, Kemler/UN) – RID podaci mogu biti u r21 ili na spisku.
        private static bool UnNaSpisku(Kontekst k) => k.Spiskovi.Any(x => x.s.UnBrojevi.Count > 0 || x.s.Kola.Any(kk => UnBroj(kk.KemlerUn) != null));

        /// <summary>UN broj iz teksta: „UN 1005“, „UN-Nr. 1005“, „UN No. 1005“, „ООН 1005“, „1005“ uz Kemler („268/1005“).</summary>
        internal static string UnBroj(string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return null;
            var m = Regex.Match(t, @"(?:\bUN|ООН|OON)\s*[-.]?\s*(?:Nr\.?|No\.?|number|broj|br\.?)?\s*[:.]?\s*(\d{4})(?!\d)", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;
            m = Regex.Match(t.Trim(), @"^\d{2,3}X?\s*/\s*(\d{4})$");
            return m.Success ? m.Groups[1].Value : null;
        }

        /// <summary>UN brojevi iz r21 (pročitani i u tekstu rubrike).</summary>
        private static List<string> UnUR21(Kontekst k) =>
            k.C.R21.UnBrojevi.Where(u => !Prazno(u)).Concat(Regex.Matches(k.C.R21.Tekst ?? "", @"(?:\bUN|ООН)\s*[-.]?\s*(?:Nr\.?|No\.?)?\s*[:.]?\s*(\d{4})(?!\d)", RegexOptions.IgnoreCase).Cast<Match>().Select(m => m.Groups[1].Value))
                .Distinct().ToList();

        private static IEnumerable<Pogodak> R21Desc(Kontekst k)
        {
            var r = k.C.R21;
            if (!r.Popunjena || !k.Citljivo(r, "21")) yield break;
            if (Prazno(r.OpisRobe)) yield return P("21", "r21");
            if (r.ZaostaliTekstSablona)
                yield return P("21", "r21-sablon").D((k.U.Engleski ? "Leftover template text: „" : "Zaostali tekst šablona: „") + r.ZaostaliTekst + "“.");
            // v3.0 (2.9.3 tačka 1): X u r23, a UN broja nema ni u r21 ni na spisku → greška „nedostaju RID podaci“
            if (N(k.C.R23.Tekst) == "x" && UnUR21(k).Count == 0 && !UnNaSpisku(k))
            {
                var pg = P("21", "r21-un").D(k.U.Engleski ? "Box 23 (RID) is marked, but there is no UN number in box 21 or on the wagon list." : "Rubrika 23 (RID) je označena, a UN broja nema ni u rubrici 21 ni na spisku kola.");
                pg.Poruka = k.U.Engleski ? "Box 21: RID data are missing (UN number, name, class)." : "Rubrika 21: nedostaju RID podaci (UN broj, naziv, klasa).";
                pg.Predlog = k.U.Engleski ? "UN 1005 AMMONIA, ANHYDROUS, 2.3 (8), RID" : "UN 1005 AMONIJAK, BEZVODNI, 2.3 (8), RID";
                yield return pg;
            }
        }

        private static IEnumerable<Pogodak> R21Uti(Kontekst k)
        {
            var r = k.C.R21;
            if (!r.Popunjena || !k.Citljivo(r, "21")) yield break;
            if (r.Uti.Any(u => Prazno(u.Broj))) yield return P("21", "r21-uti");
            if (k.C.Obrazac == "CIM_KOMBINOVANI" && r.Uti.Any(u => Prazno(u.Tip) || Prazno(u.Duzina) || Prazno(u.Neto) || Prazno(u.Tara)))
                yield return P("21", "r21-uti").D(k.U.Engleski ? "(combined transport: UTI number, type, length, net mass, tare)" : "(kombinovani saobraćaj: broj, tip, dužina, neto masa i tara UTI)");
        }

        private static IEnumerable<Pogodak> R21Imdg(Kontekst k)
        {
            if (JeRid(k) && k.C.PomorskaDeonica && !k.C.R21.ImdgIzjava) yield return P("21", "imdg");
        }

        /// <summary>
        /// R21-MRN: priručnik (Prilog 2, rubr. 21) – MRN je obavezan kada je podneta izvozna (E MRN) ili tranzitna (T/TS MRN)
        /// deklaracija; uslov se smatra ispunjenim kada je ta deklaracija u mailu. MRN bez prefiksa je takođe greška.
        /// </summary>
        private static IEnumerable<Pogodak> R21Mrn(Kontekst k)
        {
            var r = k.C.R21;
            var prefiksi = r.Mrn.Select(m => N(m.Prefiks)).ToList();
            bool ImaPrefiks(params string[] p) => prefiksi.Any(x => p.Contains(x));
            // Jedna stavka za sve deklaracije bez MRN u r21, sa predlogom iz same deklaracije (MRN, rok, sigurnost → TS MRN).
            var fale = new List<(string prefiks, string tip, List<Dokument> docs)>();
            var izvozne = k.Tip(TipDokumenta.IzvoznaDeklaracija).ToList();
            var jci = izvozne.SelectMany(d => d.Carinski).FirstOrDefault(c => c.Mrn.Count == 0 && !Prazno(c.BrojDeklaracije));
            if (izvozne.Any() && !ImaPrefiks("e mrn", "exs mrn"))
            {
                // JCI bez MRN-a: u r21 treba broj izvozne deklaracije (npr. 42072/C1/23309/2026), ne T MRN
                if (jci != null) { if (!jci.BrojDeklaracije.Split('/').Where(x => x.Length == 5).All(d => Cifre(r.Tekst).Contains(d))) fale.Add(("JCI", TipDokumenta.IzvoznaDeklaracija, izvozne)); }
                else fale.Add(("E MRN", TipDokumenta.IzvoznaDeklaracija, izvozne));
            }
            if (k.Tip(TipDokumenta.TranzitnaDeklaracija).Any() && !ImaPrefiks("t mrn", "ts mrn"))
            {
                var t1 = k.Tip(TipDokumenta.TranzitnaDeklaracija).ToList();
                var sig = t1.SelectMany(d => d.Carinski).Select(c => c.Sigurnost).FirstOrDefault(x => !Prazno(x));
                fale.Add((sig != null && sig != "0" ? "TS MRN" : "T MRN", TipDokumenta.TranzitnaDeklaracija, t1));
            }
            if (fale.Count > 0)
            {
                var predlozi = new List<string>();
                string Sta(string pref) => pref == "JCI" ? (k.U.Engleski ? "the export declaration number (JCI)" : "broja izvozne deklaracije (JCI)") : (k.U.Engleski ? "an MRN with the prefix '" : "MRN sa prefiksom '") + pref + "'";
                foreach (var (pref, tip, docs) in fale)
                {
                    if (pref == "JCI") { predlozi.Add((k.U.Engleski ? "Export declaration (JCI) " : "Izvozna deklaracija (JCI) ") + jci.BrojDeklaracije + (Prazno(jci.Carinarnica) ? "" : ", " + jci.Carinarnica)); continue; }
                    var c = docs.SelectMany(d => d.Carinski).ToList();
                    var mrn = c.SelectMany(x => x.Mrn).FirstOrDefault();
                    var rok = c.Select(x => x.Rok).FirstOrDefault(x => !Prazno(x));
                    if (mrn != null) predlozi.Add(pref + " " + mrn + (pref != "E MRN" && rok != null ? (k.U.Engleski ? ", deadline " : ", rok ") + rok : ""));
                    else if (rok != null && pref != "E MRN") predlozi.Add(pref + " " + (k.U.Engleski ? "<MRN from " : "<MRN iz ") + NazivTipa(tip, k.U.Engleski) + ">" + (k.U.Engleski ? ", deadline " : ", rok ") + rok);
                }
                yield return P("21", "r21-mrn").S("prefix", string.Join(k.U.Engleski ? " and " : " i ", fale.Select(f => Sta(f.prefiks)))).S("doc", string.Join(k.U.Engleski ? " and " : " i ", fale.Select(f => NazivTipa(f.tip, k.U.Engleski))))
                    .D((k.U.Engleski ? "Missing: " : "Nedostaje: ") + string.Join("; ", fale.Select(f => (f.prefiks == "JCI" ? (k.U.Engleski ? "export declaration number (JCI " : "broj izvozne deklaracije (JCI ") + jci.BrojDeklaracije + ")" : f.prefiks) + " (" + NazivTipa(f.tip, k.U.Engleski) + ", " + Fajlovi(f.docs) + ")")) + ".")
                    .Pr(predlozi.Count > 0 ? string.Join(" / ", predlozi) : null);
            }
            var dozvoljeni = new HashSet<string>(k.P.MrnPrefiksi.Select(N));
            foreach (var m in r.Mrn.Where(m => !dozvoljeni.Contains(N(m.Prefiks))))
                yield return P("21", "r21-mrn-prefiks-" + m.Broj).S("prefix", "E MRN / T MRN / TS MRN / EXS MRN / ENS MRN").S("doc", "MRN " + m.Broj)
                    .D((k.U.Engleski ? "(MRN without prefix: " : "(MRN bez prefiksa: ") + (m.Prefiks + " " + m.Broj).Trim() + ")");
        }

        /// <summary>
        /// Roba je otpad: NHM iz liste otpada (JSON codelists.waste_nhm: 7204, 4707, 3915, 7602, 7404…), reči otpad/otpaci/ostaci/
        /// scrap/Abfall/rifiuti/waste u r21, Annex VII u r9, dozvola za otpad u mailu, ili šifra 7.26.
        /// </summary>
        internal static bool JeOtpad(Kontekst k, out string razlog)
        {
            razlog = "";
            var nhm = k.C.R24.Stavke.Select(s => Cifre(s.Sifra + s.Tekst)).Concat(new[] { Cifre(k.C.R24.Tekst) }).Where(x => x.Length >= 4).ToList();
            var lista = k.P.OtpadNhm.FirstOrDefault(o => nhm.Any(x => x.StartsWith(o, StringComparison.Ordinal)));
            if (lista != null) { razlog = "NHM " + lista; return true; }
            var r21 = N(k.C.R21.Tekst);
            var rec = Regex.Match(r21, @"\b(otpad\w*|otpaci|ostaci|scrap|abfall\w*|rifiuti|waste)\b").Value;
            if (rec.Length > 0) { razlog = "„" + rec + "“ u r21"; return true; }
            if (Regex.IsMatch(N(Tekst(k.C.R9)), @"\b(annex vi|prilog vii|kretanju otpada)")) { razlog = "Annex VII u r9"; return true; }
            if (k.Tip(TipDokumenta.DozvolaOtpad).Any()) { razlog = "dozvola za otpad u mailu"; return true; }
            if (R7Ima(k, "26")) { razlog = "šifra 7.26"; return true; }
            return false;
        }

        /// <summary>R21-EWC (i X6): otpad bez EWC šifre u r21; predlog: indeksni brojevi iz dozvole ministarstva u mailu.</summary>
        private static IEnumerable<Pogodak> R21Ewc(Kontekst k)
        {
            if (k.C.R21.Ewc.Count > 0 || !JeOtpad(k, out var razlog)) yield break;
            var dozvole = k.Tip(TipDokumenta.DozvolaOtpad).ToList();
            var ewc = dozvole.SelectMany(d => d.Carinski.SelectMany(c => c.Ewc)).Distinct().ToList();
            yield return P("21", "ewc").D((k.U.Engleski ? "The goods are waste (" : "Roba je otpad (") + razlog + (k.U.Engleski ? "), box 21 has no EWC code." : "), a u rubrici 21 nema EWC šifre."))
                .Pr(ewc.Count > 0 ? "EWC " + string.Join(" / ", ewc) + (k.U.Engleski ? " (index numbers in the permit, " : " (indeksni brojevi iz dozvole, ") + Fajlovi(dozvole) + ")" : null);
        }

        private static bool PraznaKolaNaSpisku(Kontekst k) =>
            k.Spiskovi.Any(s => s.s.Kola.Any(x => x.Prazna || Regex.IsMatch(Cifre(x.Nhm), "^992[12]")))
            && k.Spiskovi.Any(s => s.s.Kola.Any(x => !x.Prazna && !Regex.IsMatch(Cifre(x.Nhm), "^992[12]")));

        private static IEnumerable<Pogodak> R21Cuv(Kontekst k)
        {
            if (k.C.Obrazac != "CUV" && PraznaKolaNaSpisku(k) && !k.C.R21.CuvNapomena) yield return P("21", "cuv");
        }

        // =========================================================================================== cross_rules

        private static IEnumerable<Pogodak> X1(Kontekst k)
        {
            // v3.0 (2.9.3 tačka 1): UN broj u r21 ili na spisku, a u r23 nema X → greška „treba X u r23“
            if ((UnUR21(k).Count > 0 || UnNaSpisku(k)) && N(k.C.R23.Tekst) != "x" && (!k.C.R23.Popunjena || k.Citljivo(k.C.R23, "23")))
            {
                var pg = P("23", "rid");
                pg.Poruka = k.U.Engleski ? "Box 23: the goods are dangerous (UN number in box 21 or on the wagon list) – mark X in box 23." : "Rubrika 23: roba je opasna (UN broj u rubrici 21 ili na spisku kola) – treba X u r23.";
                yield return pg;
            }
        }

        private static IEnumerable<Pogodak> X2(Kontekst k)
        {
            if (N(k.C.R23.Tekst) != "x") yield break;
            // v3.0: „X bez UN broja“ prijavljuje R21 (r21-un, greška „nedostaju RID podaci“); ovde samo preporuka za šifru 7.5
            if (!R7Ima(k, "5")) yield return P("7", "r7-5");
        }

        private static IEnumerable<Pogodak> X3(Kontekst k)
        {
            if (!JeRid(k) || !k.C.PomorskaDeonica) yield break;
            if (!k.C.R21.ImdgIzjava) yield return P("21", "imdg");
            var r9 = N(Tekst(k.C.R9));
            if (!Regex.IsMatch(r9, @"\b(5 4 2|5 4 5|sertifikat o pakovanju|packing certificate|container packing|beladebescheinigung)\b")) yield return P("9", "imdg-r9");
        }

        private static IEnumerable<Pogodak> X4(Kontekst k)
        {
            var pref = k.C.R21.Mrn.Select(m => N(m.Prefiks)).ToList();
            if (pref.Contains("e mrn") && !R9Pominje(k, TipDokumenta.IzvoznaDeklaracija) && !R9Pominje(k, TipDokumenta.DrugaCarinska))
                yield return P("9", "r9-" + TipDokumenta.IzvoznaDeklaracija).S("mrn_type", "E MRN");
            if ((pref.Contains("t mrn") || pref.Contains("ts mrn")) && !R9Pominje(k, TipDokumenta.TranzitnaDeklaracija) && !R9Pominje(k, TipDokumenta.DrugaCarinska))
                yield return P("9", "r9-" + TipDokumenta.TranzitnaDeklaracija).S("mrn_type", pref.Contains("ts mrn") ? "TS MRN" : "T MRN");
            if (k.C.R21.Arc.Count > 0 && !Regex.IsMatch(N(Tekst(k.C.R9)), @"\b(e ad|ead|e sad|akciz\w*|excise|arc)\b"))
                yield return P("9", "r9-arc").S("mrn_type", "ARC");
        }

        private static IEnumerable<Pogodak> X5(Kontekst k)
        {
            if (k.C.R22.Popunjena && !R7Stavke(k, "11").Any(s => !Prazno(s.Tekst))) yield return P("7", "r7-11");
        }

        /// <summary>R22 (upozorenje, v2.7): označena naročita pošiljka – poseban red za r22 (šifra 7.11 je X5, rubrika 7).</summary>
        private static IEnumerable<Pogodak> R22(Kontekst k)
        {
            if (k.C.R22.Popunjena) yield return P("22", "r22");
        }

        private static IEnumerable<Pogodak> X6(Kontekst k) => R21Ewc(k);

        private static IEnumerable<Pogodak> X7(Kontekst k)
        {
            if (R7Stavke(k, "3").Any(s => Prazno(s.Tekst))) yield return P("7", "r7-3");
        }

        private static IEnumerable<Pogodak> X8(Kontekst k)
        {
            if (R7Stavke(k, "42").Any(s => Prazno(s.Tekst))) yield return P("7", "r7-42");
        }

        private static IEnumerable<Pogodak> X9(Kontekst k)
        {
            if (!ViseKolaIliTriUti(k) || R9PraznaObuhvata(k)) yield break;
            if (k.Spiskovi.Count == 0 && SpisakNaveden(k) != null) yield break; // X26
            if (k.Spiskovi.Count == 0 || !R9Pominje(k, TipDokumenta.SpisakKola)) yield return P("9", "spisak-r9");
        }

        private static IEnumerable<Pogodak> X11(Kontekst k)
        {
            if (k.C.Obrazac == "CUV" || !PraznaKolaNaSpisku(k)) yield break;
            if (!k.C.R30.Cim || !k.C.R21.CuvNapomena) yield return P("21", "cuv");
        }

        private static IEnumerable<Pogodak> X12(Kontekst k)
        {
            foreach (var (r, b) in new[] { (k.C.R26, "26"), (k.C.R27, "27"), (k.C.R28, "28") })
                foreach (var x in Iznos(k, r, b, napomena: true)) yield return x;   // INTERNO „nečitljiva“ samo jednom (X12), ne i iz R26–R28
        }

        private static IEnumerable<Pogodak> X13(Kontekst k)
        {
            var r = k.C.R25;
            if (!r.Popunjena || !k.Citljivo(r, "25")) yield break;
            var mase = r.Pojedinacne.Select(Broj).Where(x => x.HasValue).Select(x => x.Value).ToList();
            var ukupno = Broj(r.Ukupno);
            if (mase.Count < 2 || !ukupno.HasValue) yield break;
            var zbir = mase.Sum();
            var tara = Broj(r.Tara) ?? 0;
            if (Math.Abs(zbir - ukupno.Value) > 1 && Math.Abs(zbir + tara - ukupno.Value) > 1)
                yield return P("25", "x13").S("sum", zbir.ToString("0.##", CultureInfo.InvariantCulture)).S("total", ukupno.Value.ToString("0.##", CultureInfo.InvariantCulture));
        }

        private static IEnumerable<Pogodak> X14(Kontekst k)
        {
            var d29 = Datum(k.C.R29.Datum, k.U.ReferentniDatum.Year);
            if (!d29.HasValue) yield break;
            var d16 = Datum(k.C.R16.Datum, d29.Value.Year);
            if (d16.HasValue && d29.Value.Date > d16.Value.Date) yield return P("29", "x14");
        }

        private static IEnumerable<Pogodak> X16(Kontekst k)
        {
            if (EuRazmena(k) && (Prazno(k.C.R1.Pdv) || Prazno(k.C.R4.Pdv))) yield return P("1", "pdv-1");
        }

        private static IEnumerable<Pogodak> X17(Kontekst k)
        {
            var zastarele = k.P.ValuteZastarele;
            if (new[] { k.C.R26, k.C.R27, k.C.R28 }.Any(r => r.Popunjena && zastarele.Contains((r.Valuta ?? "").Trim().ToUpperInvariant())))
                yield return P("26", "hrk");
        }

        private static string R62Tekst(Identifikacija r) =>
            string.Join(" ", new[] { r.Zemlja, r.Stanica, r.Preduzece, r.Broj }.Where(x => !Prazno(x))).Trim() is var t && t.Length > 0 ? t : r.Tekst;

        private static IEnumerable<Pogodak> X19(Kontekst k)
        {
            if (k.Spiskovi.Count == 0) yield break;
            var c = k.C.R62;
            if (!c.Popunjena) yield break; // nema sa čim da se poredi (R62 prijavljuje prazno)
            // Više strana prepoznatih kao spisak: porede se one koje imaju identifikaciju pošiljke.
            var saIdentifikacijom = k.Spiskovi.Where(x => x.s.R62.Popunjena && !Prazno(x.s.R62.Stanica) && !Prazno(x.s.R62.Broj)).ToList();
            foreach (var (dok, s) in saIdentifikacijom.Count > 0 ? saIdentifikacijom : k.Spiskovi)
            {
                var l = s.R62;
                // OCR spiska: identifikacija pročitana samo delimično (npr. "1 620 2") ne može pouzdano da se poredi.
                if (l.Popunjena && dok.Izvor == "ocr" && (Prazno(l.Zemlja) || Prazno(l.Stanica) || Prazno(l.Preduzece) || Prazno(l.Broj))) continue;
                if (!l.Popunjena)
                {
                    // v2.8.4 (5): identifikacija nije upisana na spisku (COAL 16+, SKM) – to nije neslaganje; najviše napomena
                    // (greška X19 samo kada je identifikacija upisana i na spisku i u r62, a razlikuju se)
                    if (dok.Izvor != "excel") // Excel bez prepoznate identifikacije: možda je samo drugačiji raspored kolona
                    {
                        var pg = P("62", "x19-" + dok.Fajl).S("list_value", k.U.Engleski ? "not entered" : "nije upisana").S("cim_value", R62Tekst(c)).D("(" + dok.Fajl + ")");
                        pg.Tezina = Tezina.Upozorenje;
                        pg.Poruka = k.U.Engleski ? "The consignment identification (box 62) is not entered on the wagon list." : "Na spisku kola nije upisana identifikacija pošiljke (r62).";
                        yield return pg;
                    }
                    continue;
                }
                // OCR može da pročita jednu cifru pogrešno (npr. ivica kućice kao "1"): kod OCR-a se toleriše razlika od jedne cifre.
                var tolerancija = dok.Izvor == "ocr" || k.Dok.Izvor == "ocr" ? 1 : 0;
                bool Razlika(string a, string b) => !Prazno(a) && !Prazno(b) && Saobracaj.Common.Text.TekstUtil.Rastojanje(Cifre(a), Cifre(b)) > tolerancija;
                if (Razlika(l.Zemlja, c.Zemlja) || Razlika(l.Stanica, c.Stanica) || Razlika(l.Preduzece, c.Preduzece) || Razlika(l.Broj, c.Broj))
                    yield return P("62", "x19-" + dok.Fajl).S("list_value", R62Tekst(l)).S("cim_value", R62Tekst(c)).D("(" + dok.Fajl + ")");
            }
        }

        private static IEnumerable<Pogodak> X20(Kontekst k)
        {
            if (!ViseKolaIliTriUti(k)) yield break;
            if (k.Spiskovi.Count == 0 && SpisakNaveden(k) != null) yield break; // X26
            if (!R9PraznaObuhvata(k) && (k.Spiskovi.Count == 0 || !R9Pominje(k, TipDokumenta.SpisakKola))) yield return P("9", "spisak-r9");
        }

        private static IEnumerable<Pogodak> X21(Kontekst k)
        {
            // v3.0 (2.9.3 tačka 1): kolona RID na spisku je potrebna samo kada kola nose RAZLIČITU opasnu robu (prazna kolona RID kod
            // iste robe nije greška – amonijak 52 290239 3057 001784: UN 1005 u r21, svih 12 kola NHM 281410)
            if (k.Spiskovi.Count == 0 || N(k.C.R23.Tekst) != "x") yield break;
            var kola = k.Spiskovi.SelectMany(s => s.s.Kola).Where(x => !x.Prazna).ToList();
            var unR21 = UnUR21(k);
            var nhm = kola.Select(x => Cifre(x.Nhm)).Where(x => x.Length >= 4).Select(x => x.Substring(0, 4)).Distinct().ToList();
            var razlicita = unR21.Count > 1 || (nhm.Count > 1 && unR21.Count != nhm.Count);
            var poKolima = kola.Count > 0 && kola.All(x => x.Rid || UnBroj(x.KemlerUn) != null);
            if (razlicita && !poKolima)
            {
                var pg = P("23", "x21").D(k.U.Engleski ? "(UN numbers in box 21: " + string.Join(", ", unR21) + "; NHM on the list: " + string.Join(", ", nhm) + ")"
                                                       : "(UN brojevi u r21: " + string.Join(", ", unR21) + "; NHM na spisku: " + string.Join(", ", nhm) + ")");
                pg.Poruka = k.U.Engleski ? "The wagons carry different dangerous goods – the wagon list must contain the RID data for each wagon." : "Kola nose različitu opasnu robu – na spisku kola treba RID podaci po kolima.";
                pg.Predlog = k.U.Engleski ? "wagon list: UN number, name and class next to each wagon" : "spisak kola: UN broj, naziv i klasa uz svaka kola";
                yield return pg;
            }
        }

        /// <summary>X22: carinski dokument u mailu ili MRN u r21 → naveden u r9 ili r7 šifra 10 (Prilog 2: „Prateći dokument mora da se upiše u rubriku 9“).</summary>
        private static IEnumerable<Pogodak> X22(Kontekst k)
        {
            if (R9PraznaObuhvata(k)) yield break; // R9 navodi sve priložene dokumente
            foreach (var tip in new[] { TipDokumenta.IzvoznaDeklaracija, TipDokumenta.TranzitnaDeklaracija, TipDokumenta.DrugaCarinska })
            {
                var docs = k.TipSaDosijeom(tip).ToList();   // v2.8.4: carinski dokumenti iz celog dosijea
                if (docs.Count == 0) continue;
                if (R9Pominje(k, tip) || R710Pominje(k, tip)) continue;
                if (tip == TipDokumenta.DrugaCarinska && (R9Pominje(k, TipDokumenta.IzvoznaDeklaracija) || R9Pominje(k, TipDokumenta.TranzitnaDeklaracija))) continue;
                yield return P("9", "r9-" + tip).S("doc", NazivTipa(tip, k.U.Engleski) + " – " + Fajlovi(docs));
            }
        }

        private static IEnumerable<Pogodak> X23(Kontekst k)
        {
            var nisu = new List<string>();
            foreach (var tip in new[] { TipDokumenta.Eur1, TipDokumenta.UverenjeOPoreklu, TipDokumenta.Sertifikat, TipDokumenta.Faktura })
            {
                var docs = k.Tip(tip).ToList();
                if (docs.Count == 0 || R9Pominje(k, tip)) continue;
                nisu.Add(NazivTipa(tip, k.U.Engleski) + " (" + Fajlovi(docs) + ")");
            }
            if (nisu.Count > 0) yield return P("9", "r9-ostali").S("doc", string.Join(k.U.Engleski ? " and " : " i ", nisu));
        }

        /// <summary>Spisak kola naveden u r9 ili r18 (sinonimi: Wagon list, Wagenliste, Spisak kola, CIT 23, siehe Wagenliste, Vidi spisak kola…); null ako nije.</summary>
        private static string SpisakNaveden(Kontekst k)
        {
            if (R9Pominje(k, TipDokumenta.SpisakKola)) return "9";
            if (R18PominjeSpisak(k)) return "18";
            return null;
        }

        /// <summary>X26: spisak kola je naveden u r9, a nije priložen u mailu (za r18 od v2.7 važi R18-SPISAK, upozorenje).</summary>
        private static IEnumerable<Pogodak> X26(Kontekst k)
        {
            if (k.SpiskoviSaDosijeom.Count > 0 || !R9Pominje(k, TipDokumenta.SpisakKola)) yield break;   // v2.8.4: i iz dosijea
            var pomen = k.C.R9.Stavke.Select(s => (s.Sifra + " " + s.Tekst).Trim()).Concat(new[] { (k.C.R9.Tekst ?? "").Replace("\n", " / ") })
                .FirstOrDefault(x => Pominje(x, TipDokumenta.SpisakKola)) ?? "";
            yield return P("9", "spisak-nije-prilozen")
                .D((k.U.Engleski ? "Box 9: „" : "U rubrici 9 piše „") + pomen + (k.U.Engleski ? "“, but no wagon list is attached to the mail." : "“, a spisak kola nije priložen u mailu."));
        }

        /// <summary>X27: prilog naveden u r9 (faktura, EUR.1, sertifikat…), a nije u mailu – upozorenje.</summary>
        private static IEnumerable<Pogodak> X27(Kontekst k)
        {
            var nema = new List<string>();
            foreach (var tip in new[] { TipDokumenta.Faktura, TipDokumenta.Eur1, TipDokumenta.UverenjeOPoreklu, TipDokumenta.Sertifikat, TipDokumenta.IzvoznaDeklaracija, TipDokumenta.TranzitnaDeklaracija,
                                        TipDokumenta.IzvestajRadioloski, TipDokumenta.AnnexVii })
                if (R9Pominje(k, tip) && !k.TipSaDosijeom(tip).Any() && !(TipDokumenta.JeCarinski(tip) && k.SviSaDosijeom.Any(d => TipDokumenta.JeCarinski(d.Tip))))
                    nema.Add(NazivTipa(tip, k.U.Engleski));
            if (nema.Count > 0) yield return P("9", "r9-nije-u-mailu").S("doc", string.Join(", ", nema));
        }

        /// <summary>X28: uslov isporuke u fakturi/deklaraciji (npr. DAP Alesd) razlikuje se od plaćanja troškova u r20 (franko / Incoterms).</summary>
        private static IEnumerable<Pogodak> X28(Kontekst k)
        {
            var izvor = k.Svi.Where(d => !TipDokumenta.JeTovarniList(d.Tip) && !Prazno(d.Incoterms)).ToList();
            if (izvor.Count == 0) yield break;
            var inco = izvor.GroupBy(d => d.Incoterms.Split(' ')[0]).OrderByDescending(g => g.Count()).First();
            var kod = inco.Key;
            var r20 = (k.C.R20.Tekst ?? "").ToUpperInvariant();
            if (Regex.IsMatch(r20, @"(?<![A-Z])" + kod + @"(?![A-Z])")) yield break;
            var cim = k.C.R20Incoterms == "oznaceno" || Incoterm(k) != null ? (Incoterm(k)?.kod ?? "Incoterms") : k.C.R20Franko == "oznaceno" || Regex.IsMatch(r20, "FRANKO|FRANCO") ? (k.U.Engleski ? "carriage paid (franco)" : "franko prevoznina") : (k.U.Engleski ? "not entered" : "nije upisano");
            var docs = inco.ToList();
            var uslov = docs.Select(d => d.Incoterms).OrderByDescending(x => x.Length).First(); // sa mestom ("DAP Verona"), ne samo "DAP"
            yield return P("20", "x28").S("cim", cim).S("inco", uslov).S("doc", string.Join(", ", docs.Select(d => NazivTipa(d.Tip, k.U.Engleski)).Distinct()))
                .D("(" + string.Join(", ", docs.Select(d => NazivTipa(d.Tip, k.U.Engleski) + " " + Fajlovi(new[] { d }))) + ")").Pr(uslov);
        }

        /// <summary>X29 (greška): NHM na spisku kola ima manje od 4 cifre; X29-NHM6 (preporuka): 4–5 cifara (kao r24, v2.7).</summary>
        private static IEnumerable<Pogodak> X29(Kontekst k) => NhmSpisak(k, x => x.Length < 4, "x29-", false);

        private static IEnumerable<Pogodak> X29Nhm6(Kontekst k) => NhmSpisak(k, x => x.Length == 4 || x.Length == 5, "x29-nhm6-", true);

        private static IEnumerable<Pogodak> NhmSpisak(Kontekst k, Func<string, bool> los, string kljuc, bool sest)
        {
            foreach (var (dok, s) in k.Spiskovi)
            {
                var lose = s.Kola.Select(x => Cifre(x.Nhm)).Where(x => x.Length > 0 && los(x)).ToList();
                if (lose.Count == 0) continue;
                var g = lose.GroupBy(x => x).OrderByDescending(x => x.Count()).First();
                var opis = (k.U.Engleski ? "Wagon list (" : "Spisak kola (") + Fajlovi(new[] { dok }) + "): " + (k.U.Engleski ? "entered " : "upisano ") + g.Key + " (" + g.Key.Length
                           + (k.U.Engleski ? " digits) in " + lose.Count + " rows." : " cifre) u " + lose.Count + " " + (lose.Count % 10 == 1 && lose.Count % 100 != 11 ? "redu." : "redova."));
                var pg = P("CIT 23", kljuc + dok.Fajl).S("nhm", g.Key).D(opis);
                if (sest)
                {
                    var (sifra, izvor) = Nhm6(k, g.Key);
                    pg.S("izvor", izvor ?? "").Pr(sifra);
                    if (izvor != null) pg.Dodatak = pg.Dodatak.TrimEnd('.') + (k.U.Engleski ? "; 6 digits preferred" : "; poželjno 6 cifara") + izvor + ".";
                }
                else pg.Pr(PredlogNhm(k, g.Key));
                yield return pg;
            }
        }

        private static IEnumerable<Pogodak> X24(Kontekst k)
        {
            var uR21 = new HashSet<string>(k.C.R21.Mrn.Select(m => N(m.Broj).Replace(" ", "")));
            var naSpisku = new HashSet<string>(k.Spiskovi.SelectMany(s => s.s.Kola.SelectMany(x => x.Mrn)).Select(m => N(m).Replace(" ", "")));
            var parovi = k.Svi.Where(d => d.Tip == TipDokumenta.TabelaMrn).SelectMany(d => d.Carinski).SelectMany(c => c.MrnPoUti)
                .Where(m => !Prazno(m.Broj)).GroupBy(m => N(m.Broj).Replace(" ", "")).Select(g => g.First()).ToList();
            var nema = parovi.Where(m => !uR21.Contains(N(m.Broj).Replace(" ", "")) && !naSpisku.Contains(N(m.Broj).Replace(" ", ""))).ToList();
            if (nema.Count == 0) yield break;
            yield return P("21", "x24").S("mrn", string.Join(", ", nema.Select(m => m.Broj))).S("uti", string.Join(", ", nema.Select(m => Prazno(m.Uz) ? "?" : m.Uz)));
        }

        private static IEnumerable<Pogodak> X25(Kontekst k)
        {
            var d = Datum(k.C.R29.Datum, k.U.ReferentniDatum.Year);
            if (d.HasValue && d.Value.Date > k.U.ReferentniDatum.Date) yield return P("29", "r29-buducnost").S("date", d.Value.ToString("dd.MM.yyyy"));
        }

        // =========================================================================================== company_rules

        private static bool KpUgovorni(Kontekst k) => k.P.Kp.JeKpSifra(k.C.R62.Preduzece) || k.P.Kp.JeKpSifra(k.C.R58a.Sifra);

        // ------------------------------------------------------------------ r7: platilac za deonicu kroz Srbiju (v2.7)

        /// <summary>Jedan zapis o platiocu u r7: „Frachtzahler RS ist …“, „Payer SRB/HR/SI/IT is …“, „16. Frachtzahler: …“.</summary>
        internal sealed class PlatilacR7
        {
            public string Zapis = "", Opseg = "", Naziv = "";
            /// <summary>Opseg pokriva Srbiju (SRB, RS, IŽS, ŽS, Srbija… ili deonica KP).</summary>
            public bool Srbija;
            /// <summary>Platilac za ceo put, bez deonica (pokriva i Srbiju).</summary>
            public bool CeoPut;
            /// <summary>Platilac je KP (KP, K prevoz, Kombinovani prevoz, КП, 3212).</summary>
            public bool Kp;
            public bool PokrivaSrbiju => Srbija || CeoPut;
        }

        internal sealed class AnalizaR7
        {
            public List<PlatilacR7> Platioci = new List<PlatilacR7>();
            /// <summary>Zapisi bez reči platilac/prevoznik sa KP uz oznaku srpske deonice („16. IŽS: KP 3212“).</summary>
            public List<string> Nejasni = new List<string>();
            public IEnumerable<PlatilacR7> ZaSrbiju => Platioci.Where(p => p.PokrivaSrbiju && p.Naziv.Length > 0);
        }

        // v2.8.4 (3): i „plaćanje / placanje / plaća / placa / Zahlung / payment / paid by“ („Plaćanje u RS za KP(3212): Panšped“)
        private static readonly Regex RecPlatioca = new Regex(@"(?<!\p{L})(?:freight\s*payer|payeur\s+du\s+fret|frachtzahler|platilac|platioc\p{L}*|payeur|payer|zahler|k[öo]lts[ée]gvisel[őöo]\p{L}*|pla[ćc]anje|pla[ćc]a|zahlung|payment|paid\s+by)(?!\p{L})", RegexOptions.IgnoreCase);
        private static readonly Regex RecPrevoznika = new Regex(@"(?<!\p{L})(?:prevoznik\p{L}*|bef[öo]e?rderer|carrier|transporteur|vettore|fuvaroz[óo]\p{L}*)(?!\p{L})", RegexOptions.IgnoreCase);
        private static readonly Regex Razdvajac = new Regex(@"\s*(?::|=|(?<!\p{L})(?:is|ist|je|est)(?!\p{L})|\s[–-]\s)\s*", RegexOptions.IgnoreCase);
        private static readonly HashSet<string> SrpskeOznake = new HashSet<string>
        {
            "srb", "rs", "srbija", "srbiji", "srbije", "srbiju", "serbia", "serbien", "serbie", "szerbia", "serbian", "serbischen", "serbische", "srpsku", "srpska", "srpskoj", "srpske",
            "izs", "zs", "infrastruktura", "zeleznice", "srbije",
        };
        private static readonly HashSet<string> ReciOpsega = new HashSet<string>
        {
            "za", "deonicu", "deonica", "deonici", "deonice", "ceo", "celu", "put", "relaciju", "relacija", "for", "fo", "the", "section", "sections", "whole", "route", "routes", "entire",
            "path", "part", "teil", "fuer", "fur", "auf", "die", "der", "den", "strecke", "ganze", "gesamte", "pour", "la", "le", "de", "du", "sur", "az", "a", "szakaszon", "szakasz",
            "on", "in", "i", "und", "and", "et", "kroz", "through", "durch",
        };

        /// <summary>Pravni oblik firme na kraju naziva (posle njega naziv je završen).</summary>
        private static readonly Regex PravniOblik = new Regex(@"(?<!\p{L})(d\.?\s?o\.?\s?o\.?|gmbh|kft\.?|a\.\s?s\.?|s\.\s?r\.\s?o\.?|srl|s\.r\.l\.?|ltd\.?|ag|sa|s\.p\.a\.?|spa|bv|nv|a\.?d\.?|zrt\.?|sro|inc\.?)\s*$", RegexOptions.IgnoreCase);

        /// <summary>Reč za platioca koju analiza umeće posle šifre 16 (bez razmaka – razlikuje se od upisanog „Freight payer“).</summary>
        private const string UmetnutPlatilac = "FreightPayer";

        /// <summary>Oznaka novog reda u analizi r7 (kraj naziva platioca).</summary>
        private static readonly string NR = ((char)0x2029).ToString();

        /// <summary>OCR greške u reči „Frachtzahler“ („Frachjzahjler“, „Frachtzahier“): reč do 3 slova razlike → „Frachtzahler“.</summary>
        private static string IspraviRecPlatioca(string t)
        {
            t = Regex.Replace(t, @"(?<!\p{L})\p{L}{9,15}(?!\p{L})", m =>
                !RecPlatioca.IsMatch(m.Value) && m.Value.StartsWith("Fr", StringComparison.OrdinalIgnoreCase)
                && Saobracaj.Common.Text.TekstUtil.Rastojanje(m.Value.ToLowerInvariant(), "frachtzahler") <= 3 ? "Frachtzahler" : m.Value);
            return IspraviVeznike(t);
        }

        /// <summary>Reči za platioca kojima OCR često pokvari jedno slovo („Piatilac“, „Paver“, „Zahiung“) – tolerancija 1 slovo.</summary>
        private static readonly string[] ReciPlatiocaTolerancija = { "platilac", "payer", "zahler", "placanje", "zahlung", "payment" };

        /// <summary>
        /// v2.8.4 (3): OCR greške u veznicima i rečima za platioca u r7 – samo za te reči, tolerancija 1 slovo:
        /// „und“ ↔ „ud/urd/unv“, „ist“ ↔ „is/lst/1st“, „für“ ↔ „fur/fiir“; „Piatilac“ → „Platilac“, „Paver“ → „Payer“.
        /// (Ostale reči se ne menjaju: „list“, „in“, „it“ nisu „ist“.)
        /// </summary>
        internal static string IspraviVeznike(string t)
        {
            t = Regex.Replace(t ?? "", @"(?<![\p{L}\d])(?:ud|urd|unv|umd|uod|und)(?!\p{L})", "und", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"(?<![\p{L}\d])(?:lst|1st|isl|ist)(?!\p{L})", "ist", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"(?<![\p{L}\d])(?:fur|fiir|f[üu]r)(?!\p{L})", "für", RegexOptions.IgnoreCase);
            return Regex.Replace(t, @"(?<![\p{L}\d])\p{L}{4,9}(?!\p{L})", m =>
            {
                if (RecPlatioca.IsMatch(m.Value)) return m.Value;
                var k = PraviloKp.Normalizuj(m.Value);
                foreach (var r in ReciPlatiocaTolerancija)
                    if (Math.Abs(k.Length - r.Length) <= 1 && Saobracaj.Common.Text.TekstUtil.Rastojanje(k, r) == 1)
                        return char.IsUpper(m.Value[0]) ? char.ToUpperInvariant(r[0]) + r.Substring(1) : r;
                return m.Value;
            });
        }

        /// <summary>
        /// „Payer for HU: A; SRB: B“ (i „, SRB: B“): reč za platioca važi i za sledeće „oznaka deonice: naziv“ u istoj rečenici –
        /// ispred takve oznake se umeće reč „Payer“, pa se analizira kao poseban zapis.
        /// </summary>
        private static string NastavciPlatioca(string t, Func<string, bool> samoOpseg)
        {
            var sb = new System.Text.StringBuilder();
            int poz = 0;
            foreach (Match m in Regex.Matches(t, @"[;,]\s*(?<o>[\p{L}][\p{L}\-\s]{0,30}?)\s*(?=:)"))
            {
                var pre = t.Substring(0, m.Index);
                var poslednji = RecPlatioca.Matches(pre).Cast<Match>().LastOrDefault();
                if (poslednji == null || !samoOpseg(m.Groups["o"].Value) || RecPlatioca.IsMatch(m.Groups["o"].Value) || RecPrevoznika.IsMatch(m.Groups["o"].Value)) continue;
                // ista rečenica: između reči za platioca i oznake nema kraja rečenice („… doo. CIM 6: …“)
                if (Regex.IsMatch(t.Substring(poslednji.Index, m.Index - poslednji.Index), @"(?<=\p{L}{2}|\d|\))\.\s+(?=\p{Lu})")) continue;
                // v2.9.1: novi red je nov zapis („Payer on IZS: for KP Pansped doo⏎MÁV: H-KÁRPÁT 3404, RS: KP 3212“ – drugi red su prevoznici)
                if (t.Substring(poslednji.Index, m.Index - poslednji.Index).Contains(NR)) continue;
                sb.Append(t, poz, m.Index - poz).Append("; Payer ");
                poz = m.Groups["o"].Index;
            }
            sb.Append(t, poz, t.Length - poz);
            return sb.ToString();
        }

        /// <summary>Platioci iz teksta r7 (sa opsegom deonica) i nejasni zapisi KP uz oznaku srpske deonice.</summary>
        internal static AnalizaR7 AnalizirajR7(string tekst, PraviloKp kp)
        {
            var a = new AnalizaR7();
            // red koji se završava zarezom/tačkom-zarezom završava zapis („Payer for SZ is Ferest Rail,⏎CIM 6§6: …“)
            // novi red: oznaka NR (kraj naziva platioca), inače kao razmak („… und KP⏎ist EurologSystem“ je jedan zapis)
            var t = Regex.Replace(Regex.Replace(tekst ?? "", @"\s*[,;]\s*\r?\n\s*", "; "), @"\s*\r?\n\s*", " " + NR + " ");
            t = Regex.Replace(t, @"[ \t]{2,}", " ");
            t = IspraviRecPlatioca(t);
            // v2.8.4 (3): šifra 16 u r7 su po GLV-CIM „Ostale izjave“, ne platilac. Kao izjava o platiocu prihvata se samo oblik
            // 16 + niz prevoznika/deonica koji sadrži KP + „ist / is / = / je“ + naziv firme („7 16 KP, GPR, SŽ-TP is EurologSystem doo
            // Beograd“ – OCR izgubio „Frachtzahler auf“). „16. Ovlašćeno lice: …“ i samo „16“ nisu platilac.
            t = Regex.Replace(t, @"(?<![\d.,/])(?:7\s?[.,]?\s?)?16\s?[.)]?\s+(?!\s*(?:frachtzahler|freight|payer|payeur|platilac|platioc|zahler|zahlung|pla[ćc]|payment|paid)\b)(?=(?<deo>[^:;=" + NR + @"]{1,80}?)\s*(?:(?<!\p{L})(?:ist|is|je)(?!\p{L})|=)\s*(?<naziv>\p{L}[^;" + NR + @"]{2,}))",
                m => kp.PominjeNaziv(m.Groups["deo"].Value) || Regex.IsMatch(kp.OcrIspravka(m.Groups["deo"].Value), @"(?<!\d)" + kp.Uic + @"(?!\d)")
                    ? m.Value.TrimEnd() + " " + UmetnutPlatilac + " " : m.Value, RegexOptions.IgnoreCase);
            bool SamoOpseg0(string s) => Regex.Matches(s, @"\p{L}+").Count > 0 && Regex.Matches(s, @"[\p{L}\d]+").Cast<Match>().All(w =>
                ReciOpsega.Contains(PraviloKp.Normalizuj(w.Value)) || SrpskeOznake.Contains(PraviloKp.Normalizuj(w.Value)) || Regex.IsMatch(w.Value, @"^[A-ZŠĐČĆŽÁÉÖŐÜ]{2,5}$"));
            t = NastavciPlatioca(t, SamoOpseg0);
            var granice = RecPlatioca.Matches(t).Cast<Match>().Select(m => m.Index)
                .Concat(RecPrevoznika.Matches(t).Cast<Match>().Select(m => m.Index))
                .Concat(Regex.Matches(t, ";").Cast<Match>().Select(m => m.Index)).OrderBy(x => x).ToList();
            bool SamoOpseg(string s) => SamoOpseg0(s);
            // v2.8.3 (TCDD): „FREIGHT PAYER FOR BG EXPRESS 5665 AND KP3212 – CINKOTURK LOGISTIK“ – platilac za deonice navedenih
            // prevoznika, među kojima je KP (deonica kroz Srbiju) → naziv posle KP 3212 je platilac za Srbiju
            var tcdd = new List<(int od, int doo)>();
            // („FOR“ OCR često ispusti: „FREIGHT PAYER BG EXPRESS 5665 KP3212-CINKOTURKLOGISTIK“; „KP3212.“ umesto „KP3212 –“)
            foreach (Match m in Regex.Matches(t, @"(?<!\p{L})(?:freight\s*payer|payer)\s+(?:for\s+)?(?<deo>[^;" + NR + @"]*?)(?<!\p{L})KP\s*-?\s*" + Regex.Escape(kp.Uic)
                                                   + @"\s*[-–:.]\s*(?<naziv>[^;" + NR + @"]+)", RegexOptions.IgnoreCase))
            {
                var naziv = Regex.Replace(m.Groups["naziv"].Value, @"\s+", " ").Trim(' ', ',', ';', '.', '-', '–');
                // OCR spaja reči („CINKOTURKLOGISTIK“) – razmak ispred uobičajenog završetka naziva
                naziv = Regex.Replace(naziv, @"(?<=\p{L}{3})(LOGISTIK|LOGISTICS|LOJISTIK|LOGISTIC)(?!\p{L})", " $1");
                if (naziv.Length == 0) continue;
                var deo = Regex.Replace(m.Groups["deo"].Value, @"\s+", " ").Trim();
                deo = Regex.Replace(deo, @"(?<!\p{L})AND$", "", RegexOptions.IgnoreCase).Trim();
                a.Platioci.Add(new PlatilacR7
                {
                    Zapis = "Freight payer for " + (deo.Length > 0 ? deo + " and " : "") + "KP " + kp.Uic + " – " + naziv,
                    Opseg = (deo + " KP " + kp.Uic).Trim(), Naziv = naziv, Srbija = true,
                    Kp = kp.PominjeNaziv(naziv) || Regex.IsMatch(kp.OcrIspravka(naziv), @"(?<!\d)" + kp.Uic + @"(?!\d)"),
                });
                tcdd.Add((m.Index, m.Index + m.Length));
            }
            // v2.9.1 (5.3): „FREIGHT PAYER [FOR BG] EXPRESS IN⏎KP-CINKOTURK LOJISTIK“ – „KP-firma“ posle reči za platioca (bez šifre 3212):
            // firma plaća deonicu KP
            foreach (Match m in Regex.Matches(t, @"(?<!\p{L})(?:freight\s*payer|payer|frachtzahler|platilac)(?!\p{L})(?<deo>[^;]{0,80}?)(?<!\p{L})KP\s*[-–:]\s*(?<naziv>\p{L}[^;" + NR + @"]{2,})", RegexOptions.IgnoreCase))
            {
                if (tcdd.Any(x => m.Index >= x.od && m.Index < x.doo)) continue;
                var naziv = Regex.Replace(m.Groups["naziv"].Value, @"\s+", " ").Trim(' ', ',', ';', '.', '-', '–');
                naziv = Regex.Replace(naziv, @"(?<=\p{L}{3})(LOGISTIK|LOGISTICS|LOJISTIK|LOGISTIC)(?!\p{L})", " $1");
                if (naziv.Length < 3 || kp.JeKp(naziv) && Regex.Replace(kp.OcrIspravka(naziv), @"(?i)\bKP\b|" + kp.Uic + @"|[\s.,()\-]", "").Length <= 3) continue;
                a.Platioci.Add(new PlatilacR7 { Zapis = Regex.Replace(m.Value.Replace(NR, " "), @"\s+", " ").Trim(), Opseg = "KP", Naziv = naziv, Srbija = true, Kp = false });
                tcdd.Add((m.Index, m.Index + m.Length));
            }
            foreach (Match m in RecPlatioca.Matches(t))
            {
                if (tcdd.Any(x => m.Index >= x.od && m.Index < x.doo)) continue;
                var kraj = granice.FirstOrDefault(g => g > m.Index);
                if (kraj <= m.Index) kraj = t.Length;
                var zapis = t.Substring(m.Index, kraj - m.Index).Trim(' ', ';', ',');
                var klauza = t.Substring(m.Index + m.Length, kraj - m.Index - m.Length);
                var predznak = Regex.Match(t.Substring(0, m.Index), @"([A-Z]{2,4})_$").Groups[1].Value;   // „CH_Freightpayer:“
                var raz = Razdvajac.Match(klauza);
                // v2.8.4 (3): „paid by Panšped doo“ – naziv sledi odmah (bez „:“ / „ist“)
                var paidBy = Regex.IsMatch(m.Value, @"^paid", RegexOptions.IgnoreCase);
                if (!raz.Success && !paidBy) continue;
                var opseg = paidBy ? predznak.Trim() : (predznak + " " + klauza.Substring(0, raz.Index)).Trim();
                var ostatak = paidBy ? klauza : klauza.Substring(raz.Index + raz.Length);
                // „Frachtzahler fuer: SRB: VTG …“ – oznaka deonice posle prvog razdvajača
                for (Match x; (x = Regex.Match(ostatak, @"^\s*([^:;,]{1,30}?)\s*(?::|(?<!\p{L})(?:is|ist)(?!\p{L}))\s*", RegexOptions.IgnoreCase)).Success && SamoOpseg(x.Groups[1].Value);)
                {
                    opseg = (opseg + " " + x.Groups[1].Value).Trim();
                    ostatak = ostatak.Substring(x.Length);
                }
                // naziv se završava na kraju rečenice („EurologSystem doo Beograd. CIM 6: … KP/26“ – „KP/26“ nije deo naziva)
                // i na novom redu („Kopax Rail Services doo⏎- Import customs clearance“)
                var redovi = ostatak.Split(NR[0]).Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
                var prviRed = redovi.FirstOrDefault() ?? "";
                // naziv prelomljen u dva reda („VTG Rail⏎Logistics Austria Gmbh“): prvi red bez pravnog oblika firme, drugi počinje
                // rečju bez cifara i nema „:“ – nastavak naziva; inače novi red je novi zapis („doo⏎- Import …“, „doo⏎16. ES-5026“)
                // v2.8.4 (3): naziv je do kraja reda; sledeći red je nastavak samo kada ZAVRŠAVA naziv (pravni oblik na kraju, bez cifara):
                // „Panšped⏎Inrail 3128“ → „Panšped“, „Kopax Rail⏎Services doo“ → „Kopax Rail Services doo“
                for (int ri = 1; ri < redovi.Count && !PravniOblik.IsMatch(prviRed) && !prviRed.Contains(".")
                                 && Regex.IsMatch(redovi[ri], @"^\p{L}[\p{L}&]*(\s|$)") && !Regex.IsMatch(redovi[ri], @"\d") && !redovi[ri].Contains(":") && PravniOblik.IsMatch(redovi[ri]); ri++)
                    prviRed += " " + redovi[ri];
                var naziv = Regex.Split(prviRed, @"(?<=\p{L}{2}|\d|\))\.\s+(?=\p{Lu})|,\s*(?:[A-Z]{2,4})\s+(?:ist|is)\s|\s/\s")[0].Trim(' ', ',', ';', '.', ':', '-', '–');
                // v2.9.1: „for KP Panšped doo“, „KP-CINKOTURK LOJISTIK“, „KP: firma“, „za KP firma“, „für KP firma“ – firma plaća deonicu KP
                var bezPrefiksa = SkiniPrefiksKp(naziv, kp);
                if (bezPrefiksa != naziv) { opseg = (opseg + " KP").Trim(); naziv = bezPrefiksa; }
                opseg = opseg.Replace(NR, " ").Trim();
                if (naziv.Length > 80) naziv = naziv.Substring(0, 77).TrimEnd() + "…";
                var sep = paidBy ? "" : raz.Value.Trim();
                zapis = Regex.Replace(m.Value + " " + opseg + (sep == ":" ? ": " : " " + sep + " ") + naziv, @"\s+", " ").Replace(" :", ":").Trim();
                var tokeni = PraviloKp.Normalizuj(opseg).Split(' ').Where(x => x.Length > 0).ToList();
                var p = new PlatilacR7
                {
                    Zapis = zapis, Opseg = opseg, Naziv = naziv,
                    Srbija = tokeni.Any(SrpskeOznake.Contains) || kp.PominjeNaziv(opseg),
                    CeoPut = tokeni.All(x => ReciOpsega.Contains(x) || Regex.IsMatch(x, @"^\d+$")),
                    Kp = naziv.Length > 0 && (kp.PominjeNaziv(naziv) || Regex.IsMatch(kp.OcrIspravka(naziv), @"(?<!\d)" + kp.Uic + @"(?!\d)")),
                };
                // „16. IŽS: KP 3212“ (šifra 16 bez reči za platioca, posle oznake deonice KP) – ostaje nejasno (KP7-NEJASNO), nije „KP je platilac“
                if (m.Value == UmetnutPlatilac && p.Kp) continue;
                if (m.Value == UmetnutPlatilac) p.Zapis = Regex.Replace(p.Zapis, "^" + UmetnutPlatilac + @"\s*", "16 ");
                a.Platioci.Add(p);
            }
            // „16. IŽS: KP 3212“ – bez reči platilac/prevoznik
            // v2.8.4 (3): i u zapisu sa rečju za platioca: „Platilac za prugu ZFBH/ZRS je Rail Cargo logistics-BH d.o.o. SRB: KP 3212; HU: …“ –
            // posle „SRB:“ je naveden prevoznik (KP), a ne platilac → nejasno ko plaća deonicu SRB (upozorenje, ne greška)
            foreach (var seg in Regex.Split(tekst ?? "", @"[;\n]").Select(x => x.Trim()).Where(x => x.Length > 0))
            {
                if (RecPrevoznika.IsMatch(seg)) continue;
                foreach (Match m in Regex.Matches(seg, @"(?<!\p{L})(?:SRB|RS|IŽS|IZS|ŽS|ZS|Srbija|Serbia|Serbien)\s*:\s*(?<v>.+?)(?=[,]?\s+(?:[A-Z]{2,4}|[A-ZŠĐČĆŽ][a-zšđčćž]+)\s*:|$)", RegexOptions.IgnoreCase))
                {
                    var v = m.Groups["v"].Value.Trim();
                    // samo KP (naziv ili 3212) bez drugog naziva: „SRB: KP 3212“, a ne „SRB: Panšped (KP 3212 prevoznik)“
                    if (!kp.JeKp(v) || Regex.Replace(kp.OcrIspravka(v), @"(?i)\bKP\b|kombinovani\s+prevoz|" + kp.Uic + @"|[\s.,()\-]", "").Length > 3) continue;
                    if (RecPlatioca.IsMatch(seg) && a.Platioci.Any(p => p.Kp && p.PokrivaSrbiju)) continue;   // „Frachtzahler SRB: KP“ – KP7-NIJE-PLATILAC
                    a.Nejasni.Add(seg);
                    break;
                }
            }
            // v2.9.2 (1): oznaka ponude nije deo naziva platioca („Panšped doo, offer nr 5856“ → „Panšped doo“)
            foreach (var p in a.Platioci)
            {
                var bez = PonudaRx.Replace(p.Naziv ?? "", " ");
                for (var pre = ""; pre != bez;) { pre = bez; bez = System.Text.RegularExpressions.Regex.Replace(bez, @"(?i)(?<!\p{L})(according|acc|to|po|prema|za)[\s.,;]*$", "").Trim(' ', ',', ';', '-', '–'); }
                bez = bez.Trim(' ', ',', ';', '-', '–');
                if (bez.Length >= 2 && bez != p.Naziv) p.Naziv = bez;
            }
            return a;
        }

        /// <summary>
        /// v2.9.1: prefiks „za deonicu KP“ ispred naziva platioca („for KP Panšped doo“, „za KP firma“, „für KP firma“, „KP-CINKOTURK
        /// LOJISTIK“, „KP: firma“) se skida – firma plaća deonicu KP, KP nije platilac. Bez naziva firme („KP 3212“, „KP UIC 3212, KP“)
        /// tekst ostaje (KP7-NIJE-PLATILAC ostaje).
        /// </summary>
        internal static string SkiniPrefiksKp(string naziv, PraviloKp kp)
        {
            var m = Regex.Match(naziv ?? "", @"^\s*(?:(?<za>for|za|f[üu]e?r|pour|per)\s+)?(?:KP|K\.P\.|Kombinovani\s+prevoz)\s*(?:\(?\s*(?:UIC\s*)?" + Regex.Escape(kp?.Uic ?? "3212") + @"\s*\)?)?(?<sep>\s*[:\-–]\s*|\s+)(?<f>.+)$", RegexOptions.IgnoreCase);
            if (!m.Success) return naziv;
            if (!m.Groups["za"].Success && m.Groups["sep"].Value.Trim().Length == 0) return naziv;   // „KP Panšped“ bez „for/za“ i bez „:“/„-“ ostaje
            var f = m.Groups["f"].Value.Trim(' ', ',', ';', '.', ':', '-', '–');
            // ostatak mora biti firma: reč od bar 3 slova koja nije KP / UIC / šifra
            var reci = Regex.Matches(f, @"\p{L}{3,}").Cast<Match>().Select(x => x.Value).Where(x => !Regex.IsMatch(x, @"^(?i)(uic|kombinovani|prevoz)$")).ToList();
            if (reci.Count == 0 || (kp != null && kp.JeKp(f) && Regex.Replace(kp.OcrIspravka(f), @"(?i)\bKP\b|kombinovani\s+prevoz|" + kp.Uic + @"|uic|[\s.,()\-]", "").Length <= 3)) return naziv;
            return f;
        }

        private static AnalizaR7 R7Analiza(Kontekst k)
        {
            var r = k.C.R7;
            var t = !Prazno(r.Tekst) ? r.Tekst : string.Join("\n", r.Stavke.Select(s => (s.Sifra + " " + s.Tekst).Trim()));
            var a = AnalizirajR7(t, k.P.Kp);
            // v2.8.3: TCDD ponavlja platioca u r15 („… STREIGHT PAYER FOR BG EXPRESS 5665 KP 3212 – CINKOTURK LOGISTIK“) –
            // kada r7 nema platioca za Srbiju, uzima se iz r15
            var r15 = k.C.R15?.Tekst ?? "";
            if (!a.ZaSrbiju.Any() && r15.Length > 0 && RecPlatioca.IsMatch(Regex.Replace(r15, @"(?i)streight\s*payer", "freight payer")))
            {
                var b = AnalizirajR7(Regex.Replace(r15, @"(?i)streight\s*payer", "freight payer"), k.P.Kp);
                a.Platioci.AddRange(b.ZaSrbiju);
            }
            return a;
        }

        /// <summary>KP7-PLATILAC (greška): u r7 nije naveden platilac za deonicu kroz Srbiju (ni platilac za ceo put).</summary>
        private static IEnumerable<Pogodak> KP7Platilac(Kontekst k)
        {
            var r = k.C.R7;
            if (r.Popunjena && !k.Citljivo(r, "7")) yield break;
            var a = R7Analiza(k);
            if (a.ZaSrbiju.Any() || a.Nejasni.Count > 0) yield break;
            // v2.9.2 (1): broj ponude KP određuje platioca deonice KP – nađena ponuda (ili baza nedostupna) → nema greške
            var pon = PonudaKonteksta(k);
            if (pon.Stanje == Baza.Ponude.Stanje.Nadjena || pon.Stanje == Baza.Ponude.Stanje.Nedostupna) yield break;
            var ostali = a.Platioci.Where(p => p.Naziv.Length > 0).Select(p => p.Zapis).ToList();
            yield return P("7", "kp7-platilac").D((!r.Popunjena ? (k.U.Engleski ? "(box is empty)" : "(rubrika je prazna)")
                : ostali.Count > 0 ? (k.U.Engleski ? "No payer is stated for the section through Serbia; entered only: „" : "Nije naveden platilac za deonicu kroz Srbiju; upisano je samo: „") + string.Join("“; „", ostali) + "“."
                : (k.U.Engleski ? "No payer for the section through Serbia is stated in box 7." : "U rubrici 7 nije naveden platilac za deonicu kroz Srbiju.")) + PonudaNijeNadjena(pon, k.U.Engleski));
        }

        /// <summary>KP7-NIJE-PLATILAC (greška): KP je naveden kao platilac za srpsku deonicu ili za ceo put.</summary>
        private static IEnumerable<Pogodak> KP7NijePlatilac(Kontekst k)
        {
            var r = k.C.R7;
            if (!r.Popunjena || !k.Citljivo(r, "7")) yield break;
            var kp = R7Analiza(k).ZaSrbiju.Where(p => p.Kp).ToList();
            if (kp.Count == 0) yield break;
            // v2.9.2 (1): „SRB: KP UIC 3212, KP Ponuda 8957“ – nađena ponuda određuje platioca (nema greške); baza nedostupna – interno
            var pon = PonudaKonteksta(k);
            if (pon.Stanje == Baza.Ponude.Stanje.Nadjena || pon.Stanje == Baza.Ponude.Stanje.Nedostupna) yield break;
            yield return P("7", "kp7-nije-platilac").D((k.U.Engleski ? "Kombinovani prevoz is stated as the payer for the section through Serbia: „" : "Kombinovani prevoz je naveden kao platilac za deonicu kroz Srbiju: „")
                + string.Join("“; „", kp.Select(p => p.Zapis)) + (k.U.Engleski ? "“. KP can be the payer only for sections outside Serbia." : "“. KP može biti platilac samo za deonice van Srbije.") + PonudaNijeNadjena(pon, k.U.Engleski));
        }

        /// <summary>KP7-NEJASNO (upozorenje): „16. IŽS: KP 3212“ – nije jasno da li je KP platilac ili prevoznik.</summary>
        private static IEnumerable<Pogodak> KP7Nejasno(Kontekst k)
        {
            var r = k.C.R7;
            if (!r.Popunjena || !k.Citljivo(r, "7")) yield break;
            var a = R7Analiza(k);
            var pon = PonudaKonteksta(k);
            if (pon.Stanje == Baza.Ponude.Stanje.Nadjena || pon.Stanje == Baza.Ponude.Stanje.Nedostupna) yield break;   // v2.9.2 (1)
            // v2.8.4 (3): „nije jasno ko plaća deonicu SRB – proverite“
            if (a.Nejasni.Count > 0 && !a.ZaSrbiju.Any()) yield return P("7", "kp7-nejasno").D((k.U.Engleski
                    ? "It is not clear who pays for the section through Serbia (SRB) – after „SRB:“ KP is stated (a carrier), not the payer: „"
                    : "Nije jasno ko plaća deonicu kroz Srbiju (SRB) – posle „SRB:“ je naveden KP (prevoznik), a ne platilac: „")
                + string.Join("“; „", a.Nejasni) + (k.U.Engleski ? "“. Please check." : "“. Proverite.") + PonudaNijeNadjena(pon, k.U.Engleski));
        }

        /// <summary>KP7-INFO (informacija): platilac za deonicu kroz Srbiju (nije KP).</summary>
        private static IEnumerable<Pogodak> KP7Info(Kontekst k)
        {
            var r = k.C.R7;
            var pon = PonudaKonteksta(k);
            var za0 = r.Popunjena && r.Status != StatusRubrike.Necitljivo ? R7Analiza(k).ZaSrbiju.ToList() : new List<PlatilacR7>();
            // v2.9.2 (1): broj ponude – samo interno (pošiljaocu ništa)
            if (pon.Stanje == Baza.Ponude.Stanje.Nedostupna)
            {
                var pg = P("7", "ponuda-baza");
                pg.Tezina = Tezina.Upozorenje; pg.SamoInterno = true;
                pg.Poruka = "Ponuda " + pon.Broj + " nije proverena – baza nedostupna (platilac za deonicu SRB nije proveren; ponavlja se u sledećem prolazu).";
                yield return pg;
            }
            else if (pon.Stanje == Baza.Ponude.Stanje.Nadjena)
            {
                var x = za0.Where(p => !p.Kp).Select(p => p.Naziv).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
                var pg = P("7", "ponuda");
                pg.SamoInterno = true;
                if (x != null && !IstiPartner(x, pon.Naziv))
                {
                    pg.Tezina = Tezina.Upozorenje;
                    pg.Poruka = "r7 navodi " + x + ", ponuda " + pon.Broj + " glasi na " + pon.Naziv + " (šifra " + pon.PartnerId + ") – za platioca deonice SRB uzima se ponuda.";
                }
                else pg.Poruka = "Platilac za deonicu SRB preko ponude " + pon.Broj + ": " + pon.Naziv + " (šifra " + pon.PartnerId + ").";
                yield return pg;
                yield break;
            }
            if (!r.Popunjena || !k.Citljivo(r, "7")) yield break;
            var za = za0;
            if (za.Count == 0 || za.Any(p => p.Kp)) yield break;
            var p0 = za.FirstOrDefault(p => p.Srbija) ?? za[0];
            yield return P("7", "kp7-info").S("payer", p0.Naziv);
        }

        // ------------------------------------------------------------------ v2.9.2 (1): platilac deonice KP preko broja ponude

        /// <summary>Broj ponude KP pročitan iz r7, r13 ili r14 i rezultat provere u bazi (dbo.Ponudba).</summary>
        internal sealed class PonudaLista
        {
            /// <summary>Oznaka kako je upisana („8957“, „42-0003-26“); null = nema broja ponude.</summary>
            public string Oznaka;
            /// <summary>Ceo broj (1–6 cifara) – samo on se traži u tabeli ponuda.</summary>
            public int? Broj;
            public string Rubrika = "";
            public string Zapis = "";
            public Baza.Ponude.Stanje? Stanje;
            public int? PartnerId;
            public string Naziv = "";
        }

        /// <summary>„Ponuda 8957“, „Ponuda8957“, „Ponuda br. 8957“, „ponuda: 8957“, „KP Ponuda 8957“, „po ponudi 8926“, „prema ponudi …“, „Offer 8957“,
        /// „offer nr 5856“, „according offer nr …“, „acc. to offer no. …“, „Angebot (Nr.) 8957“, „Ajánlat 8957“.</summary>
        internal static readonly Regex PonudaRx = new Regex(
            @"(?<![\p{L}])(?:ponud\p{L}*|offer|angebot\p{L}*|aj[aá]nlat\p{L}*)(?:\s+KP)?(?:\s*\(?\s*(?:br|no|nr|num|number|nummer|n°|#)\s*\.?\s*\)?)?\s*[:.#]?\s*(?<b>\d[\d\-/]*(?<=\d)(?:[A-Za-z]{1,3}(?![\p{L}]))?)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex OznakaSrbije = new Regex(@"(?<![\p{L}])(?:SRB|RS|KP|Srbij\p{L}*|Serbi\p{L}*)(?![\p{L}])|(?<!\d)3212(?!\d)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Broj ponude iz teksta: kada ih ima više – najbliži oznaci SRB / KP / 3212.</summary>
        internal static (string oznaka, int? broj, string zapis) BrojPonude(string tekst)
        {
            if (string.IsNullOrWhiteSpace(tekst)) return (null, null, "");
            var pogodci = PonudaRx.Matches(tekst).Cast<Match>().ToList();
            if (pogodci.Count == 0) return (null, null, "");
            var oznake = OznakaSrbije.Matches(tekst).Cast<Match>().Select(m => m.Index).ToList();
            var m0 = pogodci.OrderBy(m => oznake.Count == 0 ? 0 : oznake.Min(o => Math.Abs(o - m.Index))).First();
            var o0 = m0.Groups["b"].Value.TrimEnd('-', '/');
            int? broj = Regex.IsMatch(o0, @"^\d{1,6}$") && int.TryParse(o0, out var b) && b > 0 ? b : (int?)null;
            return (o0, broj, Regex.Replace(m0.Value, @"\s+", " ").Trim());
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<CimList, PonudaLista> ponudeListova = new System.Runtime.CompilerServices.ConditionalWeakTable<CimList, PonudaLista>();

        /// <summary>Broj ponude lista (r7, pa r13, pa r14) i platilac iz baze – jednom po listu.</summary>
        internal static PonudaLista PonudaKonteksta(Kontekst k) => k.C == null ? new PonudaLista() : PonudaLista0(k.C);

        /// <summary>Broj ponude lista i platilac iz baze (keš po listu – provera i upis koriste isti rezultat).</summary>
        internal static PonudaLista PonudaLista0(CimList c) => ponudeListova.GetValue(c, x => PonudaZaList(x));

        internal static PonudaLista PonudaZaList(CimList c)
        {
            var p = new PonudaLista();
            var izvori = new[] { ("r7", c.R7 == null ? "" : Tekst(c.R7)), ("r13", c.R13 == null ? "" : Tekst(c.R13)), ("r14", c.R14?.Tekst ?? "") };
            foreach (var (rub, t) in izvori)
            {
                var (o, b, z) = BrojPonude(t);
                if (o == null) continue;
                p.Oznaka = o; p.Broj = b; p.Rubrika = rub; p.Zapis = z;
                break;
            }
            if (p.Broj.HasValue)
            {
                var r = Baza.Ponude.Nadji(p.Broj.Value);
                p.Stanje = r.Stanje; p.PartnerId = r.PartnerId; p.Naziv = r.Naziv ?? "";
            }
            return p;
        }

        private static string PonudaNijeNadjena(PonudaLista p, bool en) => p.Stanje == Baza.Ponude.Stanje.NijeNadjena
            ? (en ? " Offer " + p.Broj + " was not found in the KP system." : " Ponuda " + p.Broj + " nije pronađena u sistemu KP.") : "";

        /// <summary>Isti partner po nazivu (bez pravne forme): „VTG Rail Logistics Austria GmbH“ = „VTG RAIL LOGISTICS AUSTRIA GMBH“.</summary>
        internal static bool IstiPartner(string a, string b)
        {
            var x = Baza.Slicnost.Osnovno(a ?? ""); var y = Baza.Slicnost.Osnovno(b ?? "");
            if (x.Length == 0 || y.Length == 0) return false;
            if (x == y || x.Contains(y) || y.Contains(x)) return true;
            var rx = x.Split(' ').Where(w => w.Length >= 3).ToList(); var ry = y.Split(' ').Where(w => w.Length >= 3).ToList();
            return rx.Count > 0 && ry.Count > 0 && rx.Intersect(ry).Count() >= Math.Min(rx.Count, ry.Count) * 0.6;
        }

        /// <summary>
        /// KP57 (v2.9.2, odluka korisnika 17.11.): KP u r57 u bilo kom obliku („3212“, „3212 KP“, „KP 3212“, „RS: 3212 KP“, „SRB: KP UIC
        /// 3212“, „3212 Kombinovani Prevoz“, „Kombinovani prevoz d.o.o.“). Naziv i UIC – ispravno; samo naziv – upozorenje „dopisati UIC
        /// 3212“; samo UIC – upozorenje „dopisati naziv“; ni naziv ni UIC – greška. Ako je KP ugovorni prevoznik u r58a, uslov je
        /// ispunjen. Važi za svaki CIM/CUV.
        /// </summary>
        private static IEnumerable<Pogodak> KP57(Kontekst k)
        {
            var r = k.C.R57;
            if (r.Popunjena && !k.Citljivo(r, "57")) yield break;
            var (naziv, uic) = KpUR57(r, k.P.Kp);
            if (naziv && uic) yield break;
            var en = k.U.Engleski;
            // v3.0 (odluka korisnika 18.11., mail 782): na skenu OCR pročita samo deo šifre uz KP („212 KP“), a r13 navodi 3212 – nije
            // upozorenje pošiljaocu („dopisati UIC 3212“), nego INTERNO „proveriti ručno“
            if (naziv && !uic && k.Ocr && r.Popunjena && DeoSifreUzKp(r.Tekst, k.P.Kp.Uic)
                && k.C.R13 != null && Regex.IsMatch(k.P.Kp.OcrIspravka(Tekst(k.C.R13)), @"(?<!\d)" + Regex.Escape(k.P.Kp.Uic) + @"(?!\d)"))
            {
                var pg = P("57", "kp57-sken");
                pg.Tezina = Tezina.Info; pg.SamoInterno = true;
                pg.Poruka = "r57: šifra KP delimično pročitana na skenu (pročitano: „" + Regex.Replace(r.Tekst ?? "", @"\s*\n\s*", " / ") + "“; u r13 je UIC " + k.P.Kp.Uic + ") – proveriti ručno.";
                yield return pg;
                yield break;
            }
            if (naziv || uic)
            {
                var pg = P("57", naziv ? "kp57-uic" : "kp57-naziv");
                pg.Tezina = Tezina.Upozorenje;
                pg.Poruka = naziv
                    ? (en ? "Box 57: Kombinovani prevoz is listed without the UIC code – please add UIC 3212." : "Rubrika 57: Kombinovani prevoz je naveden bez UIC šifre – dopisati UIC 3212.")
                    : (en ? "Box 57: only the code 3212 is listed for KP – please add the name (Kombinovani prevoz)." : "Rubrika 57: za KP je navedena samo šifra 3212 – dopisati naziv (Kombinovani prevoz).");
                yield return pg;
                yield break;
            }
            if (k.P.Kp.JeKpSifra(k.C.R58a.Sifra) || k.P.Kp.PominjeNaziv(k.C.R58a.Tekst)) yield break;
            // v2.9.2 (2): sken – srpska deonica u r57 sa ostatkom šifre KP („32   Ostružnica – Subotica br.“: OCR ne čita „3212 KP“) ili
            // KP sa 3212 u r13 – nije greška pošiljaocu, nego interna napomena za ručnu proveru
            var srb = SrpskaDeonicaR57(r);
            var deoSifre = srb != null && (r.Tekst ?? "").Split('\n').Any(red => red.Contains(srb.Split(new[] { "   " }, StringSplitOptions.None).Last().Trim()) && Regex.IsMatch(red, @"^\s*(32|321|212|3212?|3Z1Z)\b"));
            var r13Kp = k.C.R13 != null && k.P.Kp.PominjeNaziv(Tekst(k.C.R13)) && Regex.IsMatch(k.P.Kp.OcrIspravka(Tekst(k.C.R13)), @"(?<!\d)" + k.P.Kp.Uic + @"(?!\d)");
            if (k.Ocr && r.Popunjena && (deoSifre || (srb != null && r13Kp)))
            {
                var pg = P("57", "kp57-sken");
                pg.Tezina = Tezina.Info; pg.SamoInterno = true;
                pg.Poruka = "r57: KP nije pouzdano pročitan na skenu (pročitano: „" + Regex.Replace(r.Tekst ?? "", @"\s*\n\s*", " / ") + "“" + (srb != null ? "; srpska deonica „" + srb + "“" : "")
                            + (r13Kp ? "; KP sa UIC 3212 naveden u r13" : "") + ") – proveriti ručno.";
                yield return pg;
                yield break;
            }
            yield return P("57", "kp57").D(r.Popunjena ? (en ? "KP is not listed among the carriers in box 57." : "U rubrici 57 KP nije naveden među prevoznicima.") : (en ? "(box is empty)" : "(rubrika je prazna)"));
        }

        /// <summary>v3.0: uz „KP“ u r57 stoji deo UIC šifre (2–3 cifre koje su deo „3212“: „212 KP“, „KP 321“) – OCR nije pročitao celu šifru.</summary>
        internal static bool DeoSifreUzKp(string tekst, string uic)
        {
            foreach (Match m in Regex.Matches(tekst ?? "", @"(?<![\d])(\d{2,3})[\s,.;:]*KP\b|\bKP[\s,.;:]*(\d{2,3})(?!\d)"))
            {
                var c = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
                if (c.Length >= 2 && (uic ?? "").Contains(c)) return true;
            }
            return false;
        }

        /// <summary>v2.9.2 (2): da li r57 sadrži naziv KP (KP, Kombinovani prevoz…) i UIC šifru 3212 – bilo gde u rubrici, bilo kojim redom.</summary>
        internal static (bool naziv, bool uic) KpUR57(PrevozniciRubrika r, PraviloKp kp)
        {
            var tekst = kp.OcrIspravka(string.Join(Environment.NewLine, new[] { r.Tekst ?? "" }.Concat(r.Prevoznici.Select(x => (x.Sifra + " " + x.Naziv).Trim()))));
            var uic = r.Prevoznici.Any(x => kp.JeKpSifra(x.Sifra)) || Regex.IsMatch(tekst, @"(?<![\d.,/\-])" + Regex.Escape(kp.Uic) + @"(?![\d.,/\-])");
            var bezSifre = Regex.Replace(tekst, @"(?<!\d)" + Regex.Escape(kp.Uic) + @"(?!\d)", " ");
            var naziv = kp.PominjeNaziv(bezSifre);
            return (naziv, uic);
        }

        /// <summary>
        /// v2.9.2 (2): deonica KP u r57 – red sa srpskom stanicom („Subotica br. – Bor Teretna“); KP vozi samo unutar Srbije. null =
        /// u r57 nema srpske deonice.
        /// </summary>
        internal static string SrpskaDeonicaR57(PrevozniciRubrika r)
        {
            foreach (var red in r.Prevoznici.Select(x => x.Deonica).Concat((r.Tekst ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)))
                if (!string.IsNullOrWhiteSpace(red) && SrpskaStanica.IsMatch(PraviloKp.Normalizuj(red))) return red.Trim();
            return null;
        }

        /// <summary>Srpske stanice i granični prelazi koji se javljaju u deonicama (normalizovano, bez dijakritika).</summary>
        internal static readonly Regex SrpskaStanica = new Regex(@"(?<![a-z])(subotica|sid|dimitrovgrad|presevo|vrsac|kikinda|bogojevo|bor|prahovo|zajecar|nis|beograd|ruma|sabac|novi sad|pancevo|zmajevo|ostruznica|batajnica|vreoci|lazarevac|futog|smederevo|pozarevac|kraljevo|cacak|uzice|priboj|lapovo|jagodina|cuprija|paracin|vrbas|sombor|senta|zrenjanin|indjija|stara pazova|sremska mitrovica|mladenovac|valjevo|loznica|majdanpek|negotin|vrsac|prokuplje|leskovac|vranje|kragujevac|resnik|rakovica|topcider|zemun|krnjaca|surcin|pancevo varos|bukovacki salasi|novi beograd|srb|rs)(?![a-z])", RegexOptions.Compiled);

        private static IEnumerable<Pogodak> KP2(Kontekst k)
        {
            if (!k.P.Kp.JeKpSifra(k.C.R62.Preduzece)) yield break;
            var r = k.C.R58a;
            if (r.Popunjena && !k.Citljivo(r, "58a")) yield break; // nečitljivo → samo upozorenje CITLJIVOST
            // v2.7: dovoljno je 3212 ili bilo koji oblik naziva KP; adresa i potpis se ne traže
            if (!r.Popunjena || !(k.P.Kp.JeKpSifra(r.Sifra) || k.P.Kp.JeKp(r.Tekst) || k.P.Kp.JeKp(r.Naziv)))
            {
                var pg = P("58a", "r58a");
                if (r.Popunjena && PecatNecitljiv(k, r))
                {
                    pg.Tezina = Tezina.Upozorenje;
                    pg.Dodatak += k.U.Engleski ? " (OCR reading of the stamp is unreliable – check manually.)" : " (OCR čitanje pečata nije pouzdano – proveriti ručno.)";
                }
                yield return pg;
            }
        }

        /// <summary>Skenirani pečat r58a koji OCR ne čita (nema ni jedne reči od 4+ slova): nesigurno čitanje – upozorenje, ne greška.</summary>
        private static bool PecatNecitljiv(Kontekst k, Rubrika r) => k.Ocr && (Regex.Matches(r.Tekst ?? "", @"\p{L}{4,}").Count == 0
            // v2.9.2 (2): pečat preko r58 („UIC CODE KP 3212“, VOZ 16.11.2026) koji OCR čita kao „janjc, 7 p, / signa“ – nema šifre (4 cifre)
            // ni naziva prevoznika: čitanje nije pouzdano
            || (!Regex.IsMatch(r.Tekst ?? "", @"(?<!\d)\d{4}(?!\d)") && !Regex.IsMatch(r.Tekst ?? "", @"(?i)cargo|rail|prevoz|transport|bahn|vasut|vasút|zeleznic|železnic|uic|code")));

        /// <summary>
        /// KP3 (greška, v2.7): KP mora biti naveden u r13 kao prevoznik – bilo koji oblik (KP, K prevoz, Kombinovani prevoz, КП,
        /// UIC 3212, samo 3212), u bilo kom delu r13 (i sa dodatnog lista); deonica i broj tačke nisu potrebni.
        /// </summary>
        private static IEnumerable<Pogodak> KP3(Kontekst k)
        {
            var r = k.C.R13;
            if (r.Popunjena && !k.Citljivo(r, "13")) yield break;
            if (k.P.Kp.JeKp(Tekst(r))) yield break;
            yield return P("13", "kp-r13").D(r.Popunjena ? (k.U.Engleski ? "KP is not stated in box 13." : "U rubrici 13 KP nije naveden.") : (k.U.Engleski ? "(box is empty)" : "(rubrika je prazna)"));
        }

        private static IEnumerable<Pogodak> KP5(Kontekst k)
        {
            var nazivi = new List<string>();
            foreach (var x in k.C.R57.Prevoznici.Where(x => k.P.Kp.JeKpSifra(x.Sifra) && !Prazno(x.Naziv) && !k.P.Kp.JeKpNaziv(x.Naziv))) nazivi.Add(x.Naziv);
            if (k.P.Kp.JeKpSifra(k.C.R58a.Sifra) && !Prazno(k.C.R58a.Naziv) && !k.P.Kp.JeKpNaziv(k.C.R58a.Naziv)) nazivi.Add(k.C.R58a.Naziv);
            foreach (var s in k.C.R13.Stavke.Concat(k.C.R7.Stavke).Where(s => !k.P.Kp.JeKpNaziv(s.Tekst)))
                foreach (Match m in Regex.Matches(s.Tekst ?? "", @"(?<!\d)" + k.P.Kp.Uic + @"(?!\d)\s*[-–:,]?\s*([A-Za-zÀ-žА-я][^,;\n\d]{2,40})"))
                    if (!k.P.Kp.JeKpNaziv(m.Groups[1].Value)) nazivi.Add(m.Groups[1].Value.Trim());
            foreach (var p in k.Spiskovi.SelectMany(s => s.s.Prevoznici))
                if (Regex.IsMatch(p, @"(?<!\d)" + k.P.Kp.Uic + @"(?!\d)") && !k.P.Kp.JeKpNaziv(p) && Regex.IsMatch(p, @"[A-Za-z]{3}"))
                    nazivi.Add(Regex.Replace(p, @"(?<!\d)" + k.P.Kp.Uic + @"(?!\d)", "").Trim(' ', '-', ',', ':'));
            // "SRB:", "HR:", "SI:", "FS:", "IŽS:", "ŽS:", "RS:" su oznake zemlje/mreže, ne naziv prevoznika
            string BezOznake(string n) => Regex.Replace(n ?? "", @"^\s*(?:SRB|RS|HR|SI|SLO|FS|IT|IŽS|IZS|ŽS|ZS|SŽ|HU|BG|RO|MK|BIH|BA|AT|DE|ME|CH)\s*:?\s*", "").Trim(' ', ',', ';', ':', '-');
            // naziv ima bar dva slova: „KP(3212)“ ostavlja samo „)“ iza šifre – to nije drugi naziv
            foreach (var n in nazivi.Select(BezOznake).Where(x => Regex.IsMatch(x, @"\p{L}{2}") && !k.P.Kp.JeKpNaziv(x) && !k.P.Kp.PominjeNaziv(x)).Distinct(StringComparer.OrdinalIgnoreCase))
                yield return P("57", "kp5-" + N(n)).S("name", n);
        }

        private static IEnumerable<Pogodak> KP6(Kontekst k)
        {
            if (k.Spiskovi.Count > 0 && !k.Spiskovi.Any(s => s.s.KpNaveden || s.s.Prevoznici.Any(k.P.Kp.JeKp))) yield return P("–", "kp6");
        }

        // =========================================================================================== recommendations

        private static IEnumerable<Pogodak> Rec1(Kontekst k) { if (!k.C.R8.Popunjena) yield return P("8", "rec1"); }

        private static IEnumerable<Pogodak> Rec3(Kontekst k)
        {
            var tekstovi = new List<string> { Tekst(k.C.R13), k.C.R57.Tekst, k.C.R58a.Tekst }.Concat(k.C.R57.Prevoznici.Select(x => x.Sifra + " " + x.Naziv));
            var kp = tekstovi.Where(k.P.Kp.JeKp).ToList();
            if (kp.Count > 0 && kp.Any(t => !(N(t).Contains("kombinovani prevoz") && t.Contains(k.P.Kp.Uic)))) yield return P("–", "rec3");
        }

        private static IEnumerable<Pogodak> Rec4(Kontekst k)
        {
            var kp = k.C.R57.Prevoznici.Where(x => k.P.Kp.JeKpSifra(x.Sifra) || k.P.Kp.JeKp(x.Naziv)).ToList();
            if (kp.Any(x => !Regex.IsMatch(x.Deonica ?? "", @"\d{4,}") || Prazno(x.StatusPrevoznika))) yield return P("57", "rec4");
        }

        private static IEnumerable<Pogodak> Rec6(Kontekst k)
        {
            if (k.Spiskovi.Count > 0 && k.Spiskovi.All(s => s.dok.Izvor != "excel")) yield return P("–", "rec6");
        }

        private static IEnumerable<Pogodak> Rec7(Kontekst k) { if (k.C.KvalitetSkena == "los") yield return P("–", "rec7"); }

        private static IEnumerable<Pogodak> Rec8(Kontekst k)
        {
            if ((Prazno(k.C.R1.Telefon) && Prazno(k.C.R1.Email)) || (Prazno(k.C.R4.Telefon) && Prazno(k.C.R4.Email))) yield return P("1", "rec8");
        }
    }
}
