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
                ["R20-INCO"] = R20Inco, ["R20-INCO-ZASTARELO"] = R20IncoZastarelo, ["R21-MRN99"] = R21Mrn99, ["R20-RUTA"] = R20Ruta, ["R21"] = R21, ["R22"] = X5, ["R23"] = R23,
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
                ["KP57"] = KP57, ["KP2"] = KP2, ["KP3"] = KP3, ["KP5"] = KP5, ["KP6"] = KP6,
                // recommendations (stalni ID iz JSON-a; REC2 obrisan u v2.7)
                ["REC1"] = Rec1, ["REC3"] = Rec3, ["REC4"] = Rec4, ["REC6"] = Rec6, ["REC7"] = Rec7, ["REC8"] = Rec8,
            };

        /// <summary>
        /// Pravila za rubrike prevoznika (49, 50, 58a, 62) – važe samo kada je pošiljalac interni ili je u r62 šifra 3212.
        /// Sva ostala pravila (i R57, KP7, KP57) važe za SVAKI CIM/CUV: na priprema@ stižu samo pošiljke u kojima KP učestvuje.
        /// </summary>
        public static readonly HashSet<string> SamoKp = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "R49", "R50", "R58A", "R62", "KP2", "REC4" };

        /// <summary>Pravila sa filled_by = carrier u JSON-u koja ipak važe uvek (ne zavise od režima KP).</summary>
        public static readonly HashSet<string> UvekVazi = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "KP57" };

        private static readonly string[] Incoterms = { "EXW", "FCA", "CPT", "CIP", "DAP", "DPU", "DDP", "DAF", "DDU" };
        private static readonly HashSet<string> Eu = new HashSet<string>
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
            [TipDokumenta.SpisakKola] = new[] { "spisak kola", "spisku kola", "wagon list", "list of wagons", "wagenliste", "liste des wagons", "cit 23", "cit23", "vagon listesi", "kocsijegyz" },
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

        private static string NazivTipa(string tip, bool en)
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
            if (Prazno(s.Adresa)) fali.Add(k.U.Engleski ? "address" : "adresa");
            // Greška je samo kada zemlje nema; zemlja upisana rečima („Srbija“, „Italy“) je upozorenje R1-ISO / R4-ISO.
            if (!Regex.IsMatch((s.SifraZemlje ?? "").Trim(), @"^[A-Za-z]{2,3}$") && Prazno(s.NazivZemlje)) fali.Add(k.U.Engleski ? "country (ISO 3166 code)" : "zemlja (ISO 3166 šifra)");
            if (potpis && !s.Potpis && !k.C.R62.Popunjena) fali.Add(k.U.Engleski ? "signature" : "potpis");
            if (fali.Count > 0) yield return P(broj, "r" + broj).D((k.U.Engleski ? "Missing: " : "Nedostaje: ") + string.Join(", ", fali) + ".");
        }

        private static readonly Dictionary<string, string> IsoPoNazivu = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["srbija"] = "RS", ["serbia"] = "RS", ["serbien"] = "RS", ["србија"] = "RS", ["madjarska"] = "HU", ["mađarska"] = "HU", ["hungary"] = "HU",
            ["romania"] = "RO", ["rumunija"] = "RO", ["bulgaria"] = "BG", ["bugarska"] = "BG", ["italy"] = "IT", ["italia"] = "IT", ["italija"] = "IT",
            ["hrvatska"] = "HR", ["croatia"] = "HR", ["slovenija"] = "SI", ["slovenia"] = "SI", ["turska"] = "TR", ["turkey"] = "TR", ["grcka"] = "GR",
            ["grčka"] = "GR", ["greece"] = "GR", ["austrija"] = "AT", ["austria"] = "AT", ["nemacka"] = "DE", ["nemačka"] = "DE", ["germany"] = "DE",
            ["ceska"] = "CZ", ["češka"] = "CZ", ["slovacka"] = "SK", ["slovakia"] = "SK", ["poljska"] = "PL", ["poland"] = "PL", ["makedonija"] = "MK",
            ["crna gora"] = "ME", ["montenegro"] = "ME", ["bosna"] = "BA",
        };

        /// <summary>R1-ISO / R4-ISO: zemlja upisana rečima umesto ISO 3166 šifre – upozorenje, sa predlogom iz istog upisa.</summary>
        private static IEnumerable<Pogodak> ZemljaRecima(Kontekst k, Strana s, string broj)
        {
            if (!s.Popunjena || !k.Citljivo(s, broj) || Regex.IsMatch((s.SifraZemlje ?? "").Trim(), @"^[A-Za-z]{2,3}$") || Prazno(s.NazivZemlje)) yield break;
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
                    .Pr(string.Join("; ", potrebni.Select(d => d.Tip).Distinct().Select(t => SifraPriloga(t) + " " + NazivTipa(t, k.U.Engleski))));
        }

        /// <summary>UN/EDIFACT 1001 šifra priloga za predlog upisa u r9.</summary>
        private static string SifraPriloga(string tip)
        {
            switch (tip)
            {
                case TipDokumenta.SpisakKola: return "271";
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
            if (k.Spiskovi.Count > 0 || !R18PominjeSpisak(k)) yield break;
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
                return (kod, mesto);
            }
            return null;
        }

        private static IEnumerable<Pogodak> R20Inco(Kontekst k)
        {
            if (!k.C.R20.Popunjena || !k.Citljivo(k.C.R20, "20")) yield break;
            var inc = Incoterm(k);
            if (inc != null && Prazno(inc.Value.mesto))
                yield return P("20", "r20-inco").S("code", inc.Value.kod);
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
            yield return P("25", "r25-masa").D(r.Status == StatusRubrike.Oznake ? (k.U.Engleski ? "Only the form labels are entered, without values." : "Upisane su samo oznake, bez vrednosti.")
                : upucuje ? (k.U.Engleski ? "Box 25 refers to the wagon list, but no wagon list with masses is attached." : "Rubrika 25/21 upućuje na spisak kola, a spisak sa masom nije priložen.")
                : (k.U.Engleski ? "(box is empty)" : "(rubrika je prazna)"));
        }

        private static IEnumerable<Pogodak> Iznos(Kontekst k, Iznos r, string broj)
        {
            // Neobavezne rubrike (26–28): mastilo bez pročitanog teksta (sitni natpisi obrasca) nije razlog za upozorenje.
            if (!r.Popunjena || (r.Status == StatusRubrike.Necitljivo && Prazno(r.Tekst)) || !k.Citljivo(r, broj)) yield break;
            if (Prazno(r.Vrednost) || !k.P.Valute.Contains((r.Valuta ?? "").Trim().ToUpperInvariant()))
                yield return P(broj, "valuta-" + broj);
        }

        private static IEnumerable<Pogodak> R29(Kontekst k)
        {
            var r = k.C.R29;
            if (!r.Popunjena) { yield return P("29", "r29").D(k.U.Engleski ? "(box is empty)" : "(rubrika je prazna)"); yield break; }
            if (!k.Citljivo(r, "29")) yield break;
            var fali = new List<string>();
            if (Prazno(r.Mesto)) fali.Add(k.U.Engleski ? "place" : "mesto");
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
            if (!r.Popunjena) { yield return P("58a", "r58a").D(k.U.Engleski ? "(box is empty)" : "(rubrika je prazna)"); yield break; }
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
            if (!k.Citljivo(r, "62")) yield break;
            var fali = new List<string>();
            if (Prazno(r.Zemlja)) fali.Add(k.U.Engleski ? "country" : "zemlja");
            if (Prazno(r.Stanica)) fali.Add(k.U.Engleski ? "station" : "stanica");
            if (Prazno(r.Preduzece)) fali.Add(k.U.Engleski ? "company code" : "šifra preduzeća");
            var obrazac = (string)k.P.PoId["R62"].Json["pattern_broj_otpravljanja"] ?? @"^\d{5}-?\d$|^\d{6}$";
            if (Prazno(r.Broj)) fali.Add(k.U.Engleski ? "consignment number" : "broj otpravljanja");
            else if (!Regex.IsMatch(Regex.Replace(r.Broj, @"\s", ""), obrazac)) fali.Add((k.U.Engleski ? "consignment number in the format 12345-6 (entered: " : "broj otpravljanja u obliku 12345-6 (upisano: ") + r.Broj + ")");
            if (fali.Count > 0) yield return P("62", "r62").D((k.U.Engleski ? "Missing: " : "Nedostaje: ") + string.Join(", ", fali) + ".");
        }

        // =========================================================================================== r21_subrules

        /// <summary>r21 upućuje na spisak kola („Siehe Wagenliste“), a priloženi spisak ima UN brojeve – UN broj je na spisku.</summary>
        private static bool UnNaSpisku(Kontekst k) => Pominje(k.C.R21.Tekst, TipDokumenta.SpisakKola) && k.Spiskovi.Any(x => x.s.UnBrojevi.Count > 0);

        private static IEnumerable<Pogodak> R21Desc(Kontekst k)
        {
            var r = k.C.R21;
            if (!r.Popunjena || !k.Citljivo(r, "21")) yield break;
            if (Prazno(r.OpisRobe)) yield return P("21", "r21");
            if (r.ZaostaliTekstSablona)
                yield return P("21", "r21-sablon").D((k.U.Engleski ? "Leftover template text: „" : "Zaostali tekst šablona: „") + r.ZaostaliTekst + "“.");
            if (N(k.C.R23.Tekst) == "x" && r.UnBrojevi.Count == 0 && !UnNaSpisku(k))
                yield return P("21", "r21-un").D(k.U.Engleski ? "Box 23 (RID) is marked, but box 21 has no UN number." : "Rubrika 23 (RID) je označena, a u rubrici 21 nema UN broja.");
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
            if (k.C.R21.UnBrojevi.Any(u => !Prazno(u)) && N(k.C.R23.Tekst) != "x") yield return P("23", "rid");
        }

        private static IEnumerable<Pogodak> X2(Kontekst k)
        {
            if (N(k.C.R23.Tekst) != "x") yield break;
            if (k.C.R21.UnBrojevi.Count == 0 && !UnNaSpisku(k)) yield return P("21", "r21-un");
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
                foreach (var x in Iznos(k, r, b)) yield return x;
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
                    if (dok.Izvor != "excel") // Excel bez prepoznate identifikacije: možda je samo drugačiji raspored kolona
                        yield return P("62", "x19-" + dok.Fajl).S("list_value", k.U.Engleski ? "not entered" : "nije upisana").S("cim_value", R62Tekst(c)).D("(" + dok.Fajl + ")");
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
            if (k.Spiskovi.Count == 0) yield break;
            var cimRid = N(k.C.R23.Tekst) == "x";
            var spisakRid = k.Spiskovi.Any(s => s.s.RidOznaka || s.s.Kola.Any(x => x.Rid));
            if (cimRid != spisakRid) yield return P("23", "x21");
        }

        /// <summary>X22: carinski dokument u mailu ili MRN u r21 → naveden u r9 ili r7 šifra 10 (Prilog 2: „Prateći dokument mora da se upiše u rubriku 9“).</summary>
        private static IEnumerable<Pogodak> X22(Kontekst k)
        {
            if (R9PraznaObuhvata(k)) yield break; // R9 navodi sve priložene dokumente
            foreach (var tip in new[] { TipDokumenta.IzvoznaDeklaracija, TipDokumenta.TranzitnaDeklaracija, TipDokumenta.DrugaCarinska })
            {
                var docs = k.Tip(tip).ToList();
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
            if (k.Spiskovi.Count > 0 || !R9Pominje(k, TipDokumenta.SpisakKola)) yield break;
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
                if (R9Pominje(k, tip) && !k.Tip(tip).Any() && !(TipDokumenta.JeCarinski(tip) && k.Svi.Any(d => TipDokumenta.JeCarinski(d.Tip))))
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

        private static readonly Regex RecPlatioca = new Regex(@"(?<!\p{L})(?:freight\s*payer|payeur\s+du\s+fret|frachtzahler|platilac|platioc\p{L}*|payeur|payer|zahler|k[öo]lts[ée]gvisel[őöo]\p{L}*)(?!\p{L})", RegexOptions.IgnoreCase);
        private static readonly Regex RecPrevoznika = new Regex(@"(?<!\p{L})(?:prevoznik\p{L}*|bef[öo]e?rderer|carrier|transporteur|vettore|fuvaroz[óo]\p{L}*)(?!\p{L})", RegexOptions.IgnoreCase);
        private static readonly Regex Razdvajac = new Regex(@"\s*(?::|=|(?<!\p{L})(?:is|ist|je|est)(?!\p{L})|\s[–-]\s)\s*", RegexOptions.IgnoreCase);
        private static readonly HashSet<string> SrpskeOznake = new HashSet<string> { "srb", "rs", "srbija", "srbiji", "srbije", "serbia", "serbien", "szerbia", "izs", "zs" };
        private static readonly HashSet<string> ReciOpsega = new HashSet<string>
        {
            "za", "deonicu", "deonica", "deonici", "deonice", "ceo", "celu", "put", "relaciju", "relacija", "for", "the", "section", "sections", "whole", "route", "entire",
            "fuer", "fur", "auf", "die", "der", "strecke", "ganze", "gesamte", "pour", "la", "le", "de", "du", "sur", "az", "a", "szakaszon", "szakasz", "on", "in", "i", "und", "and", "et",
        };

        /// <summary>Platioci iz teksta r7 (sa opsegom deonica) i nejasni zapisi KP uz oznaku srpske deonice.</summary>
        internal static AnalizaR7 AnalizirajR7(string tekst, PraviloKp kp)
        {
            var a = new AnalizaR7();
            // red koji se završava zarezom/tačkom-zarezom završava zapis („Payer for SZ is Ferest Rail,⏎CIM 6§6: …“)
            var t = Regex.Replace(Regex.Replace(tekst ?? "", @"\s*[,;]\s*\r?\n\s*", "; "), @"\s*\r?\n\s*", " ");
            t = Regex.Replace(t, @"[ \t]{2,}", " ");
            var granice = RecPlatioca.Matches(t).Cast<Match>().Select(m => m.Index)
                .Concat(RecPrevoznika.Matches(t).Cast<Match>().Select(m => m.Index))
                .Concat(Regex.Matches(t, ";").Cast<Match>().Select(m => m.Index)).OrderBy(x => x).ToList();
            bool SamoOpseg(string s) => Regex.Matches(s, @"\p{L}+").Count > 0 && Regex.Matches(s, @"[\p{L}\d]+").Cast<Match>().All(w =>
                ReciOpsega.Contains(PraviloKp.Normalizuj(w.Value)) || Regex.IsMatch(w.Value, @"^[A-ZŠĐČĆŽÁÉÖŐÜ]{2,5}$"));
            foreach (Match m in RecPlatioca.Matches(t))
            {
                var kraj = granice.FirstOrDefault(g => g > m.Index);
                if (kraj <= m.Index) kraj = t.Length;
                var zapis = t.Substring(m.Index, kraj - m.Index).Trim(' ', ';', ',');
                var klauza = t.Substring(m.Index + m.Length, kraj - m.Index - m.Length);
                var predznak = Regex.Match(t.Substring(0, m.Index), @"([A-Z]{2,4})_$").Groups[1].Value;   // „CH_Freightpayer:“
                var raz = Razdvajac.Match(klauza);
                if (!raz.Success) continue;
                var opseg = (predznak + " " + klauza.Substring(0, raz.Index)).Trim();
                var ostatak = klauza.Substring(raz.Index + raz.Length);
                // „Frachtzahler fuer: SRB: VTG …“ – oznaka deonice posle prvog razdvajača
                for (Match x; (x = Regex.Match(ostatak, @"^\s*([^:;,]{1,30}?)\s*(?::|(?<!\p{L})(?:is|ist)(?!\p{L}))\s*", RegexOptions.IgnoreCase)).Success && SamoOpseg(x.Groups[1].Value);)
                {
                    opseg = (opseg + " " + x.Groups[1].Value).Trim();
                    ostatak = ostatak.Substring(x.Length);
                }
                var naziv = Regex.Split(ostatak, @"(?<=\p{L}{2}|\d)\.\s+(?=\p{Lu}\p{Ll})|,\s*(?:[A-Z]{2,4})\s+(?:ist|is)\s|\s/\s")[0].Trim(' ', ',', ';', '.', ':', '-', '–');
                if (naziv.Length > 80) naziv = naziv.Substring(0, 77).TrimEnd() + "…";
                var sep = raz.Value.Trim();
                zapis = Regex.Replace(m.Value + " " + opseg + (sep == ":" ? ": " : " " + sep + " ") + naziv, @"\s+", " ").Replace(" :", ":").Trim();
                var tokeni = PraviloKp.Normalizuj(opseg).Split(' ').Where(x => x.Length > 0).ToList();
                var p = new PlatilacR7
                {
                    Zapis = zapis, Opseg = opseg, Naziv = naziv,
                    Srbija = tokeni.Any(SrpskeOznake.Contains) || kp.PominjeNaziv(opseg),
                    CeoPut = tokeni.All(x => ReciOpsega.Contains(x) || Regex.IsMatch(x, @"^\d+$")),
                    Kp = naziv.Length > 0 && (kp.PominjeNaziv(naziv) || Regex.IsMatch(kp.OcrIspravka(naziv), @"(?<!\d)" + kp.Uic + @"(?!\d)")),
                };
                a.Platioci.Add(p);
            }
            // „16. IŽS: KP 3212“ – bez reči platilac/prevoznik
            foreach (var seg in Regex.Split(tekst ?? "", @"[;\n]").Select(x => x.Trim()).Where(x => x.Length > 0))
            {
                if (RecPlatioca.IsMatch(seg) || RecPrevoznika.IsMatch(seg)) continue;
                var m = Regex.Match(seg, @"(?<!\p{L})(?:SRB|RS|IŽS|IZS|ŽS|ZS|Srbija|Serbia|Serbien)\s*:\s*(.+)$", RegexOptions.IgnoreCase);
                if (m.Success && kp.JeKp(m.Groups[1].Value)) a.Nejasni.Add(seg);
            }
            return a;
        }

        private static AnalizaR7 R7Analiza(Kontekst k)
        {
            var r = k.C.R7;
            var t = !Prazno(r.Tekst) ? r.Tekst : string.Join("\n", r.Stavke.Select(s => (s.Sifra + " " + s.Tekst).Trim()));
            return AnalizirajR7(t, k.P.Kp);
        }

        /// <summary>KP7-PLATILAC (greška): u r7 nije naveden platilac za deonicu kroz Srbiju (ni platilac za ceo put).</summary>
        private static IEnumerable<Pogodak> KP7Platilac(Kontekst k)
        {
            var r = k.C.R7;
            if (r.Popunjena && !k.Citljivo(r, "7")) yield break;
            var a = R7Analiza(k);
            if (a.ZaSrbiju.Any() || a.Nejasni.Count > 0) yield break;
            var ostali = a.Platioci.Where(p => p.Naziv.Length > 0).Select(p => p.Zapis).ToList();
            yield return P("7", "kp7-platilac").D(!r.Popunjena ? (k.U.Engleski ? "(box is empty)" : "(rubrika je prazna)")
                : ostali.Count > 0 ? (k.U.Engleski ? "No payer is stated for the section through Serbia; entered only: „" : "Nije naveden platilac za deonicu kroz Srbiju; upisano je samo: „") + string.Join("“; „", ostali) + "“."
                : (k.U.Engleski ? "No payer for the section through Serbia is stated in box 7." : "U rubrici 7 nije naveden platilac za deonicu kroz Srbiju."));
        }

        /// <summary>KP7-NIJE-PLATILAC (greška): KP je naveden kao platilac za srpsku deonicu ili za ceo put.</summary>
        private static IEnumerable<Pogodak> KP7NijePlatilac(Kontekst k)
        {
            var r = k.C.R7;
            if (!r.Popunjena || !k.Citljivo(r, "7")) yield break;
            var kp = R7Analiza(k).ZaSrbiju.Where(p => p.Kp).ToList();
            if (kp.Count > 0) yield return P("7", "kp7-nije-platilac").D((k.U.Engleski ? "Kombinovani prevoz is stated as the payer for the section through Serbia: „" : "Kombinovani prevoz je naveden kao platilac za deonicu kroz Srbiju: „")
                + string.Join("“; „", kp.Select(p => p.Zapis)) + (k.U.Engleski ? "“. KP can be the payer only for sections outside Serbia." : "“. KP može biti platilac samo za deonice van Srbije."));
        }

        /// <summary>KP7-NEJASNO (upozorenje): „16. IŽS: KP 3212“ – nije jasno da li je KP platilac ili prevoznik.</summary>
        private static IEnumerable<Pogodak> KP7Nejasno(Kontekst k)
        {
            var r = k.C.R7;
            if (!r.Popunjena || !k.Citljivo(r, "7")) yield break;
            var a = R7Analiza(k);
            if (a.Nejasni.Count > 0 && !a.ZaSrbiju.Any()) yield return P("7", "kp7-nejasno").D((k.U.Engleski ? "It is not clear whether KP is stated as the payer or as the carrier: „" : "Nije jasno da li je KP naveden kao platilac ili kao prevoznik: „")
                + string.Join("“; „", a.Nejasni) + "“.");
        }

        /// <summary>KP7-INFO (informacija): platilac za deonicu kroz Srbiju (nije KP).</summary>
        private static IEnumerable<Pogodak> KP7Info(Kontekst k)
        {
            var r = k.C.R7;
            if (!r.Popunjena || !k.Citljivo(r, "7")) yield break;
            var za = R7Analiza(k).ZaSrbiju.ToList();
            if (za.Count == 0 || za.Any(p => p.Kp)) yield break;
            var p0 = za.FirstOrDefault(p => p.Srbija) ?? za[0];
            yield return P("7", "kp7-info").S("payer", p0.Naziv);
        }

        /// <summary>
        /// KP57: u r57 mora biti naveden KP (naziv u bilo kom obliku ili samo šifra 3212 kao šifra prevoznika). Ako je KP
        /// ugovorni prevoznik naveden u r58a, uslov je ispunjen. Važi za svaki CIM/CUV.
        /// </summary>
        private static IEnumerable<Pogodak> KP57(Kontekst k)
        {
            var r = k.C.R57;
            if (r.Popunjena && !k.Citljivo(r, "57")) yield break;
            if (r.Prevoznici.Any(x => k.P.Kp.JeKpSifra(x.Sifra) || k.P.Kp.PominjeNaziv(x.Naziv)) || k.P.Kp.PominjeNaziv(r.Tekst)) yield break;
            if (Regex.IsMatch(k.P.Kp.OcrIspravka(r.Tekst ?? ""), @"(?<![\d.,/\-])" + k.P.Kp.Uic + @"(?![\d.,/\-])")) yield break; // samo „3212“ kao šifra prevoznika
            if (k.P.Kp.JeKpSifra(k.C.R58a.Sifra) || k.P.Kp.PominjeNaziv(k.C.R58a.Tekst)) yield break;
            yield return P("57", "kp57").D(r.Popunjena ? (k.U.Engleski ? "KP is not listed among the carriers in box 57." : "U rubrici 57 KP nije naveden među prevoznicima.") : (k.U.Engleski ? "(box is empty)" : "(rubrika je prazna)"));
        }

        private static IEnumerable<Pogodak> KP2(Kontekst k)
        {
            if (!k.P.Kp.JeKpSifra(k.C.R62.Preduzece)) yield break;
            var r = k.C.R58a;
            if (r.Popunjena && !k.Citljivo(r, "58a")) yield break; // nečitljivo → samo upozorenje CITLJIVOST
            // v2.7: dovoljno je 3212 ili bilo koji oblik naziva KP; adresa i potpis se ne traže
            if (!r.Popunjena || !(k.P.Kp.JeKpSifra(r.Sifra) || k.P.Kp.JeKp(r.Tekst) || k.P.Kp.JeKp(r.Naziv)))
            {
                var pg = P("58a", "r58a");
                if (r.Popunjena && PecatNecitljiv(k, r)) pg.Tezina = Tezina.Upozorenje;
                yield return pg;
            }
        }

        /// <summary>Skenirani pečat r58a koji OCR ne čita (nema ni jedne reči od 4+ slova): nesigurno čitanje – upozorenje, ne greška.</summary>
        private static bool PecatNecitljiv(Kontekst k, Rubrika r) => k.Ocr && Regex.Matches(r.Tekst ?? "", @"\p{L}{4,}").Count == 0;

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
            foreach (var n in nazivi.Select(BezOznake).Where(x => x.Length > 0 && !k.P.Kp.JeKpNaziv(x) && !k.P.Kp.PominjeNaziv(x)).Distinct(StringComparer.OrdinalIgnoreCase))
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
