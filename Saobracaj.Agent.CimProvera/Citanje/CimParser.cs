using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Saobracaj.Agent.CimProvera.Model;

namespace Saobracaj.Agent.CimProvera.Citanje
{
    /// <summary>
    /// Iz reči raspoređenih po rubrikama (CimSablon) pravi CimList: tekst rubrike, status (prazno/popunjeno), pouzdanost
    /// (prosek OCR pouzdanosti reči; tekst iz PDF-a 0.98) i delove koje proveravaju pravila (datumi, šifre, MRN…).
    /// Parser ne ocenjuje greške – to radi rules engine.
    /// </summary>
    internal static class CimParser
    {
        private static readonly HashSet<string> IsoZemlje = new HashSet<string>
        {
            "AT","BE","BG","HR","CY","CZ","DK","EE","FI","FR","DE","GR","EL","HU","IE","IT","LV","LT","LU","MT","NL","PL","PT","RO","SK","SI","ES","SE",
            "RS","BA","ME","MK","AL","XK","TR","UA","MD","CH","NO","GB","RU","BY","GE","AM","AZ","KZ","CN","IR","MA","DZ","TN",
        };
        private static readonly Dictionary<string, string> NaziviZemalja = new Dictionary<string, string>
        {
            ["srbija"] = "RS", ["serbia"] = "RS", ["serbien"] = "RS", ["serbie"] = "RS", ["србија"] = "RS",
            ["madjarska"] = "HU", ["mađarska"] = "HU", ["hungary"] = "HU", ["ungarn"] = "HU", ["hongrie"] = "HU",
            ["grcka"] = "GR", ["grčka"] = "GR", ["greece"] = "GR", ["griechenland"] = "GR",
            ["bugarska"] = "BG", ["bulgaria"] = "BG", ["bulgarien"] = "BG", ["rumunija"] = "RO", ["romania"] = "RO", ["rumänien"] = "RO",
            ["hrvatska"] = "HR", ["croatia"] = "HR", ["kroatien"] = "HR", ["slovenija"] = "SI", ["slovenia"] = "SI",
            ["turska"] = "TR", ["turkey"] = "TR", ["türkiye"] = "TR", ["turkei"] = "TR", ["türkei"] = "TR",
            ["makedonija"] = "MK", ["macedonia"] = "MK", ["crna gora"] = "ME", ["montenegro"] = "ME",
            ["austrija"] = "AT", ["austria"] = "AT", ["österreich"] = "AT", ["nemacka"] = "DE", ["nemačka"] = "DE", ["germany"] = "DE", ["deutschland"] = "DE",
            ["ceska"] = "CZ", ["češka"] = "CZ", ["czech"] = "CZ", ["tschechien"] = "CZ", ["slovacka"] = "SK", ["slovakia"] = "SK", ["slowakei"] = "SK",
            ["italija"] = "IT", ["italy"] = "IT", ["italien"] = "IT", ["italia"] = "IT", ["poljska"] = "PL", ["poland"] = "PL", ["polen"] = "PL",
            // rečnik zemalja za r1, r4, r10 (oblici na jezicima CIT obrazaca)
            ["italie"] = "IT", ["olaszország"] = "IT", ["italien"] = "IT", ["românia"] = "RO", ["roumanie"] = "RO", ["románia"] = "RO", ["rumunjska"] = "RO", ["rumunsko"] = "RO", ["rumänien"] = "RO",
            ["görögország"] = "GR", ["grèce"] = "GR", ["grecia"] = "GR", ["grčija"] = "GR", ["ελλάδα"] = "GR", ["гърция"] = "GR",
            ["szerbia"] = "RS", ["serbija"] = "RS", ["srbsko"] = "RS", ["sırbistan"] = "RS", ["сърбия"] = "RS",
            ["magyarország"] = "HU", ["ungheria"] = "HU", ["maďarsko"] = "HU", ["madžarska"] = "HU", ["ungaria"] = "HU", ["унгария"] = "HU",
            ["bulgarie"] = "BG", ["bulharsko"] = "BG", ["bulgária"] = "BG", ["българия"] = "BG", ["bugaristan"] = "BG",
            ["croatie"] = "HR", ["croazia"] = "HR", ["horvátország"] = "HR", ["chorvatsko"] = "HR", ["hırvatistan"] = "HR",
            ["slovénie"] = "SI", ["slovenien"] = "SI", ["szlovénia"] = "SI", ["slovinsko"] = "SI", ["slovenija"] = "SI",
            ["autriche"] = "AT", ["ausztria"] = "AT", ["rakousko"] = "AT", ["allemagne"] = "DE", ["germania"] = "DE", ["németország"] = "DE", ["německo"] = "DE",
            ["république tchèque"] = "CZ", ["csehország"] = "CZ", ["česko"] = "CZ", ["slovaquie"] = "SK", ["szlovákia"] = "SK", ["slovensko"] = "SK",
            ["turquie"] = "TR", ["turchia"] = "TR", ["törökország"] = "TR", ["turecko"] = "TR", ["türkiye cumhuriyeti"] = "TR",
            ["macédoine du nord"] = "MK", ["north macedonia"] = "MK", ["severna makedonija"] = "MK", ["bosnia"] = "BA", ["bosna i hercegovina"] = "BA",
            ["pologne"] = "PL", ["polonia"] = "PL", ["lengyelország"] = "PL", ["polsko"] = "PL",
        };

        private static readonly Regex Datum = new Regex(@"\b(?<d>[0-3]?\d)\s?[./-]\s?(?<m>[01]?\d)\s?[./-]\s?(?<g>(?:20)?\d{2})\b", RegexOptions.Compiled);
        private static readonly Regex DatumIso = new Regex(@"\b(?<g>20\d{2})-(?<m>[01]\d)-(?<d>[0-3]\d)\b", RegexOptions.Compiled);
        private static readonly Regex MrnRx = new Regex(@"(?<p>\b(?:EXS|ENS|TS|E|T)\s*-?\s*MRN|\bMRN)?[\s:.\-]*\b(?<n>\d{2}[A-Z]{2}[A-Z0-9]{14})\b", RegexOptions.Compiled);
        private static readonly Regex UtiRx = new Regex(@"\b[A-Z]{3}[UJZ]\s?-?\d{6}\s?-?\d\b", RegexOptions.Compiled);
        private static readonly Regex KolaRx = new Regex(@"(?<!\d)(\d[\d \-]{10,17}\d)(?!\d)", RegexOptions.Compiled);
        private static readonly Regex XRx = new Regex(@"^[\[\(|]?[xXхХ×✗✘]{1,2}[\]\)|.,;]?$", RegexOptions.Compiled);

        // ------------------------------------------------------------------------------------------- osnovno

        private static string Tekst(List<Rec> reci) => string.Join("\n", Raspored.Linije(reci).Select(l => l.Tekst)).Trim();

        /// <summary>Reči koje su samo šum OCR-a (pojedinačni znaci, crte, okviri).</summary>
        private static List<Rec> Ocisti(List<Rec> reci) =>
            reci.Where(r => Regex.IsMatch(r.T, @"[\p{L}\p{N}]") && !(r.T.Length == 1 && !char.IsLetterOrDigit(r.T[0]))).ToList();

        /// <summary>Red koji je samo oznaka obrasca/šablona bez vrednosti: „Tara: …“, „Netto: ...“, „+- - -“, „-“.</summary>
        private static readonly Regex OznakaRed = new Regex(@"^\s*(?:(?:tara|tare|netto|neto|bruto|brutto|gross|net|masa|tel|fax|e-?mail)\s*[:.]?\s*)*[\s.…\-–+_/:|]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Red OCR šuma: niska pouzdanost ili same jedno-dvoslovne „reči“ bez cifara („jj“, „ater“…).</summary>
        private static bool JeSum(Linija l)
        {
            if (l.Reci.Any(w => w.Velicina > 0 || !string.IsNullOrEmpty(w.Font))) return false; // tekst iz PDF-a nije šum
            if (l.Reci.Average(w => w.Pouzdanost) < 0.55) return true;
            // bar 60 % reči su smeće: 1–2 slova bez cifara, isto slovo ponovljeno ("jjjjjjjj"), ili slova bez samoglasnika
            var smece = l.Reci.Count(w => JeSmece(w.T));
            return smece >= Math.Max(1, l.Reci.Count * 0.6);
        }

        private static bool JeSmece(string t)
        {
            if (XRx.IsMatch(t) || Regex.IsMatch(t, @"\d")) return false;
            var n = CimSablon.Norm(t);
            if (n.Length <= 2) return !(Regex.IsMatch(t.Trim('.', ',', ':'), @"^[A-Z]{2}$") && (IsoZemlje.Contains(t.Trim('.', ',', ':')) || t.StartsWith("KP")));
            if (Regex.IsMatch(n, @"(.)\1{2,}")) return true;
            return n.Length >= 4 && !Regex.IsMatch(n, @"[aeiouyаеиоу]");
        }

        /// <summary>Kratke prave reči (predlozi, veznici) koje se ne brišu kao šum kada se red zadržava.</summary>
        private static readonly HashSet<string> KratkeReci = new HashSet<string> { "o", "i", "a", "u", "uz", "za", "na", "od", "do", "of", "de", "und", "et" };

        /// <summary>Nazivi pratećih dokumenata (r9): red sa takvom reči nije šum ni kada je OCR nesiguran – brišu se samo smeće-reči u njemu.</summary>
        private static readonly Regex DokumentRec = new Regex(@"faktur|invoice|rechnung|izve[sš]ta|report|radio|certifi|uverenj|spisak|wagon|deklarac|declarat|annex|prilog|dozvol|permit|otpad|waste|specifik|analiz|atest|potvrd|sertifik|packing", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static T Pop<T>(T rub, List<Rec> reci, Regex zadrziRed = null) where T : Rubrika
        {
            var smeca = reci.Count(r => Ocr.Smece(r.T));
            var sve = reci.Where(r => !Regex.IsMatch(r.T, @"^[|_=~]+$") && !Ocr.Smece(r.T)).ToList();
            bool imaOznaka = false, imaSuma = smeca > 0 && sve.Count == 0;
            int sumLinija = 0;
            var zadrzane = new List<Linija>();
            foreach (var l in Raspored.Linije(sve))
            {
                if (OznakaRed.IsMatch(l.Tekst)) { if (Regex.IsMatch(l.Tekst, @"\p{L}{3,}|\.{2,}|…|\+-")) imaOznaka = true; continue; }
                if (JeSum(l))
                {
                    if (zadrziRed != null && l.Reci.Any(w => w.T.Length >= 5 && w.Pouzdanost >= 0.45 && zadrziRed.IsMatch(w.T)))
                    {
                        var ost = l.Reci.Where(w => !JeSmece(w.T) || KratkeReci.Contains(CimSablon.Norm(w.T))).ToList();
                        if (ost.Count > 0) { zadrzane.AddRange(Raspored.Linije(ost)); continue; }
                    }
                    imaSuma = true; sumLinija++; continue;
                }
                zadrzane.Add(l);
            }
            // Sken na kome je većina redova šum (npr. tekst prekriven pečatom): ostatak nije pouzdan – nečitljivo.
            if (zadrzane.Count > 0 && sumLinija >= 2 * zadrzane.Count && sumLinija >= 3)
            {
                rub.Status = StatusRubrike.Necitljivo; rub.Tekst = string.Join("\n", zadrzane.Select(l => l.Tekst)).Trim(); rub.Pouzdanost = 0.3;
                return rub;
            }
            var znacajne = Ocisti(zadrzane.SelectMany(l => l.Reci).ToList()).Where(r => r.T.Length >= 2 || char.IsDigit(r.T[0]) || XRx.IsMatch(r.T)).ToList();
            if (znacajne.Count == 0)
            {
                if (imaOznaka) { rub.Status = StatusRubrike.Oznake; rub.Tekst = Tekst(sve); rub.Pouzdanost = 1; }
                else if (imaSuma) { rub.Status = StatusRubrike.Necitljivo; rub.Tekst = ""; rub.Pouzdanost = 0.3; }
                else { rub.Status = StatusRubrike.Prazno; rub.Tekst = ""; rub.Pouzdanost = 1; }
                return rub;
            }
            rub.Status = StatusRubrike.Popunjeno;
            rub.Tekst = string.Join("\n", zadrzane.Select(l => l.Tekst)).Trim();
            rub.Pouzdanost = Math.Round(znacajne.Average(r => r.Pouzdanost), 2);
            return rub;
        }

        private static string Cifre(string s) => Regex.Replace(s ?? "", @"\D", "");

        /// <summary>Rubrika čiji je podatak broj (šifra stanice, NHM, masa, iznos, identifikacija): bez ijedne cifre se smatra praznom (ostatak naziva rubrike ili šum OCR-a).</summary>
        private static T PopBroj<T>(T rub, List<Rec> reci) where T : Rubrika
        {
            Pop(rub, reci);
            if (rub.Popunjena && !Regex.IsMatch(rub.Tekst ?? "", @"\d")) { rub.Status = StatusRubrike.Prazno; rub.Tekst = ""; rub.Pouzdanost = 1; }
            return rub;
        }

        /// <summary>Cifre upisane po kućicama ili razdvojene zarezom/tačkom ("1,0 2 3 1,0", "1,6,2032") se spajaju u grupe.</summary>
        internal static string SpojiKucice(string t)
        {
            if (string.IsNullOrEmpty(t)) return t ?? "";
            t = Regex.Replace(t, @"(?<=\d)[,.](?=\d(?!\d{2}))", "");          // 1,0 → 10 ; 1,6,2032 → 162032 ; 1.650.585 ostaje
            t = Regex.Replace(t, @"(?<![\d])(\d)(?:\s(\d)){3,7}(?![\d])", m => Regex.Replace(m.Value, @"\s", ""));
            return t;
        }
        private static bool ImaX(List<Rec> reci) => reci.Any(r => XRx.IsMatch(r.T));

        internal static string NadjiDatum(string t, out int poz, out int duz)
        {
            poz = -1; duz = 0;
            var m = DatumIso.Match(t ?? "");
            if (m.Success) { poz = m.Index; duz = m.Length; return m.Value; }
            m = Datum.Match(t ?? "");
            if (!m.Success) return "";
            int d = int.Parse(m.Groups["d"].Value), mes = int.Parse(m.Groups["m"].Value), g = int.Parse(m.Groups["g"].Value);
            if (g < 100) g += 2000;
            if (mes < 1 || mes > 12 || d < 1 || d > 31) return "";
            poz = m.Index; duz = m.Length;
            return g.ToString("0000") + "-" + mes.ToString("00") + "-" + d.ToString("00");
        }

        // ------------------------------------------------------------------------------------------- CIM list

        /// <summary>Pragovi mastila (App.config): prazna rubrika, označeno / prazno polje.</summary>
        internal sealed class Pragovi
        {
            public double Mastilo = 0.004, Oznaka = 0.15, PraznoPolje = 0.05;
        }

        private static readonly HashSet<string> Sifre7 = new HashSet<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "24", "26", "42" };
        private static readonly HashSet<string> Sifre13 = new HashSet<string> { "1", "2", "3", "4", "5" };

        /// <summary>Naslov odeljka dodatnog lista: „Rovat 7 / Feld 7:“, „Rubrika 13:“, „Box 7“…</summary>
        private static readonly Regex OdeljakDodatnog = new Regex(@"^\s*(?:rovat|feld|rubrika|рубрика|box|case|kästchen)\s*(\d{1,2})\b[^:]*:?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex KrajOdeljka = new Regex(@"^\s*(?:Nr\.\s|\d{1,3}\.\)\s*\d{2}\s?\d{2}|Összesítés|Summen|Wagen\s+Bezeichnung)|Kocsiszám|Wagennummer", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Dodatni list (Pótlap / Zusatzblatt): tekst odeljaka „Rovat 7 / Feld 7“ i „Rovat 13 / Feld 13“ se dodaje rubrikama 7 i 13
        /// prednje strane. Odeljak bez šifre na početku je nastavak poslednje stavke sa prednje strane. Vraća brojeve spojenih rubrika.
        /// </summary>
        internal static List<string> DopuniIzDodatnogLista(CimList c, Strana s)
        {
            var odeljci = new Dictionary<string, List<Rec>>();
            List<Rec> tekuci = null;
            foreach (var l in Raspored.Linije(s.Reci))
            {
                var m = OdeljakDodatnog.Match(l.Tekst);
                if (m.Success)
                {
                    var broj = m.Groups[1].Value;
                    if (!odeljci.TryGetValue(broj, out tekuci)) odeljci[broj] = tekuci = new List<Rec>();
                    // reči naslova („Rovat 7 / Feld 7:“) se preskaču, ostatak reda je sadržaj
                    var kraj = l.Reci.FindIndex(w => w.T.EndsWith(":"));
                    tekuci.AddRange(l.Reci.Skip(kraj >= 0 && kraj < 6 ? kraj + 1 : 4));
                    continue;
                }
                // kraj odeljka: zaglavlje ili red tabele kola („Nr. Kocsiszám…“, „1.) 37 80 …“), zbir
                if (KrajOdeljka.IsMatch(l.Tekst)) { tekuci = null; continue; }
                tekuci?.AddRange(l.Reci);
            }
            var spojene = new List<string>();
            void Spoji(ListaRubrika cilj, ListaRubrika dod, string broj)
            {
                if (dod == null || !dod.Popunjena || dod.Stavke.Count == 0) return;
                var stavke = dod.Stavke.ToList();
                if (stavke[0].Sifra == "" && cilj.Stavke.Count > 0) { cilj.Stavke[cilj.Stavke.Count - 1].Tekst += " " + stavke[0].Tekst; stavke.RemoveAt(0); }
                cilj.Stavke.AddRange(stavke);
                cilj.Tekst = ((cilj.Tekst ?? "") + "\n" + dod.Tekst).Trim();
                if (!cilj.Popunjena) { cilj.Status = StatusRubrike.Popunjeno; cilj.Pouzdanost = dod.Pouzdanost; }
                spojene.Add(broj);
            }
            if (odeljci.TryGetValue("7", out var r7)) Spoji(c.R7, Lista(r7, "7", Sifre7), "7");
            if (odeljci.TryGetValue("13", out var r13)) Spoji(c.R13, Lista(r13, "13", Sifre13), "13");
            return spojene;
        }

        public static CimList Napravi(CimSablon.Rezultat r, Strana strana, Pragovi prag = null)
        {
            prag = prag ?? new Pragovi();
            var c = new CimList { RedniBroj = 1 };
            var sve = strana.Tekst;
            var n = sve.ToLowerInvariant();

            c.R1 = StranaRub(r.Rubrika("1"));
            c.R4 = StranaRub(r.Rubrika("4"));
            c.R7 = Lista(r.Rubrika("7"), "7", Sifre7);
            c.R8 = Pop(new TekstRubrika(), r.Rubrika("8"));
            c.R9 = Prilozi(r.Rubrika("9"));
            c.R10 = Isporuka(r.Rubrika("10"), r.Rubrika("12"));
            c.R12 = PopBroj(new TekstRubrika(), r.Rubrika("12"));
            c.R13 = Lista(r.Rubrika("13"), "13", Sifre13);
            c.R14 = Pop(new TekstRubrika(), r.Rubrika("14"));
            c.R15 = Pop(new TekstRubrika(), r.Rubrika("15"));
            c.R16 = Preuzimanje(r.Rubrika("16"), r.Rubrika("17"));
            c.R18 = Kola(r.Rubrika("18"));
            c.R19 = Pop(new TekstRubrika(), r.Rubrika("19"));
            c.R20 = Pop(new TekstRubrika(), r.Rubrika("20"));
            c.R21 = Roba(r.Rubrika("21"));
            c.R22 = Oznaka(r.Rubrika("22"));
            c.R23 = Oznaka(r.Rubrika("23"));
            c.R24 = Nhm(r.Rubrika("24"));
            c.R25 = Mase(r.Rubrika("25"));
            if (c.R25.Status == StatusRubrike.Popunjeno && !Regex.IsMatch(c.R25.Tekst ?? "", @"\d")) c.R25 = new Masa();
            c.R26 = IznosRub(r.Rubrika("26"));
            c.R27 = IznosRub(r.Rubrika("27"));
            c.R28 = IznosRub(r.Rubrika("28"));
            c.R29 = MestoDatumRub(r.Rubrika("29"));
            c.R49 = PopBroj(new TekstRubrika(), r.Rubrika("49"));
            c.R50 = Pop(new TekstRubrika(), r.Rubrika("50"));
            c.R57 = Prevoznici(r.Rubrika("57"));
            c.R58a = Ugovorni(r.Rubrika("58a"));
            c.R62 = Identifikacija(r.Rubrika("62"));
            c.R99 = Pop(new TekstRubrika(), r.Rubrika("99"));

            // Polja za označavanje: gustina mastila u kvadratiću iz šablona (oznaceno / prazno / nesigurno), ne OCR.
            string Polje(string id) => r.Polje(id, prag.Oznaka, prag.PraznoPolje);
            string pCim = Polje("30cim"), pCuv = Polje("30cuv");
            bool cim, cuv; string st30;
            if (pCim != null || pCuv != null)
            {
                cim = pCim == StanjePolja.Oznaceno; cuv = pCuv == StanjePolja.Oznaceno;
                st30 = cim || cuv ? StatusRubrike.Popunjeno : pCim == StanjePolja.Nesigurno || pCuv == StanjePolja.Nesigurno ? StatusRubrike.Necitljivo : StatusRubrike.Prazno;
            }
            else
            {
                cim = ImaX(r.Rubrika("30cim")); cuv = ImaX(r.Rubrika("30cuv"));
                st30 = cim || cuv ? StatusRubrike.Popunjeno : StatusRubrike.Prazno;
            }
            c.R30 = new OznakaDokumenta { Cim = cim, Cuv = cuv, Status = st30, Tekst = (cim ? "CIM X " : "") + (cuv ? "CUV X" : ""), Pouzdanost = st30 == StatusRubrike.Necitljivo ? 0.4 : 0.95 };
            c.Obrazac = cuv && !cim ? "CUV" : "CIM";
            c.R22 = OznakaPolja(Polje("22")) ?? c.R22;
            c.R23 = OznakaPolja(Polje("23")) ?? c.R23;
            c.R20Franko = Polje("20franko") ?? "";
            c.R20Incoterms = Polje("20inco") ?? "";
            c.R52Polje = Polje("52") ?? "";
            c.R58bPolje = Polje("58b") ?? "";
            if (c.R20Franko == StanjePolja.Oznaceno && !Regex.IsMatch(c.R20.Tekst ?? "", @"franko|franco|prevozn|fracht", RegexOptions.IgnoreCase))
            {
                c.R20.Tekst = ("Franko prevoznina (X) " + (c.R20.Tekst ?? "")).Trim();
                if (!c.R20.Popunjena) { c.R20.Status = StatusRubrike.Popunjeno; c.R20.Pouzdanost = 0.95; }
            }

            // Kućice mesec–dan–čas (r16): bez pročitanog datuma – prazne (greška) ili sa mastilom (nečitljivo).
            if (c.R16.Datum.Length == 0 && r.MastiloKucica("16") is double mk)
                c.R16.KuciceStanje = mk > prag.Mastilo ? StatusRubrike.Necitljivo : StatusRubrike.Prazno;

            // Rubrika bez pročitanog teksta, a sa mastilom koje nije odštampani obrazac (rukopis, pečat, bled sken): nečitljiva, ne prazna.
            foreach (var prop in typeof(CimList).GetProperties().Where(x => typeof(Rubrika).IsAssignableFrom(x.PropertyType) && Regex.IsMatch(x.Name, @"^R\d+[ab]?$")))
            {
                var id = prop.Name.Substring(1);
                if (id == "30" || id == "22" || id == "23") continue;
                var rub = (Rubrika)prop.GetValue(c);
                if (rub == null) continue;
                var mast = r.Mastilo(id);
                if (rub.Status == StatusRubrike.Prazno && mast is double m && m > prag.Mastilo) { rub.Status = StatusRubrike.Necitljivo; rub.Pouzdanost = 0.3; }
                // Samo šum OCR-a (ostaci natpisa), a bez mastila podataka: prazno, ne nečitljivo.
                else if (rub.Status == StatusRubrike.Necitljivo && string.IsNullOrEmpty(rub.Tekst) && mast is double m2 && m2 <= prag.Mastilo) { rub.Status = StatusRubrike.Prazno; rub.Pouzdanost = 1; }
            }

            c.ElektronskiIspis = Regex.IsMatch(n, @"ispis elektronskog|ausdruck des elektronischen|print-?out of the electronic|impression de la lettre de voiture electronique");
            c.NapomenaIspisa = c.ElektronskiIspis;
            c.PomorskaDeonica = Regex.IsMatch(n, @"\bimdg\b|pomorsk|seaport|by sea|vessel|\bship\b|seehafen");
            var p = strana.SrednjaPouzdanost;
            c.KvalitetSkena = strana.Izvor != IzvorTeksta.Ocr ? "dobar" : p < 0.6 ? "los" : p < 0.75 ? "srednji" : "dobar";
            return c;
        }

        private static Model.Strana StranaRub(List<Rec> reci)
        {
            var s = Pop(new Model.Strana(), reci);
            if (!s.Popunjena) return s;
            var linije = s.Tekst.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            var t = s.Tekst;
            s.Email = Regex.Match(t, @"[\w.\-+]+@[\w\-]+\.[\w.\-]+").Value;
            s.Telefon = Regex.Match(t, @"(?:\+|00)\d[\d \-/().]{6,}\d|\b0\d{1,3}[ /\-]?\d{3}[ \-]?\d{3,4}\b").Value.Trim();
            var pdv = Regex.Match(t, @"(?:VAT|PDV|PIB|DIČ|DIC|USt|TVA|MWSt|ИБ|ПИБ)[\w\s.\-]{0,12}?[:\s]+([A-Z]{0,2}\s?\d[\d ]{6,13})", RegexOptions.IgnoreCase);
            if (!pdv.Success) pdv = Regex.Match(t, @"\b((?:CZ|SK|HU|EL|GR|BG|RO|HR|SI|AT|DE|IT|PL|RS)\d{8,12})\b");
            s.Pdv = pdv.Success ? pdv.Groups[1].Value.Trim() : "";
            // Šifra zemlje: dva velika slova kao posebna reč (npr. "CZ 110 00 Praha", "HU-2601", "RS"), iz liste ISO 3166.
            // (PDV/PIB broj "RS101938497" nije šifra zemlje: šifra mora biti odvojena od cifara razmakom ili crticom)
            foreach (Match m in Regex.Matches(t, @"(?<![A-Za-z0-9])([A-Z]{2})(?=[\s\-]+\d|\s*$|\s*\n|,)"))
                if (IsoZemlje.Contains(m.Groups[1].Value)) { s.SifraZemlje = m.Groups[1].Value == "EL" ? "GR" : m.Groups[1].Value; break; }
            var nk = t.ToLowerInvariant();
            s.NazivZemlje = NaziviZemalja.Keys.FirstOrDefault(z => Regex.IsMatch(nk, @"(?<!\p{L})" + Regex.Escape(z) + @"(?!\p{L})")) ?? "";
            var bezKontakta = linije.Where(l => !Regex.IsMatch(l, @"@|(?:\+|00)\d{5}|\b(tel|fax|e-?mail|vat|pdv|pib|dič|ust|tva)\b", RegexOptions.IgnoreCase)).ToList();
            s.Naziv = bezKontakta.FirstOrDefault() ?? "";
            s.Adresa = string.Join(", ", bezKontakta.Skip(1));
            // Potpis (grafika) lokalno ne može pouzdano da se utvrdi; ne prijavljuje se kao nedostatak.
            s.Potpis = true;
            return s;
        }

        /// <summary>
        /// Stavke sa šifrom (r7 / r13), red po red: "1: tekst", "16. tekst", "7.16 tekst", više stavki u redu odvojenih sa ";".
        /// Red bez šifre se pripaja prethodnoj stavci. Redovi bez šifre i bez cifara pre prve stavke su ostatak naziva rubrike (OCR) i ne računaju se.
        /// </summary>
        private static ListaRubrika Lista(List<Rec> reci, string rubrika, HashSet<string> sifre)
        {
            var r = Pop(new ListaRubrika(), reci);
            if (!r.Popunjena) return r;
            var pocetak = new Regex(@"^(?:" + rubrika + @"\.)?(\d{1,2})(?:\s*[:.)\-'’`]\s*|\s+|$)(.*)$");
            var bezSifre = new List<string>();
            var tekst = r.Tekst;
            // r13: stavke „2.“, „3.“ usred reda („…Idomeni, 2.“ / „8 Tervezett irányvonat 3. RCH…“) počinju novi red
            if (rubrika == "13") tekst = Regex.Replace(tekst, @"[ \t]+(?=(?:13\.)?[1-5]\.(?:\s|$))", "\n");
            foreach (var red in tekst.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0))
                foreach (var d in Regex.Split(red, @"\s*;\s*|\s+(?=(?:" + rubrika + @"\.)\d{1,2}\b)").Select(x => x.Trim()).Where(x => x.Length > 0))
                {
                    var m = pocetak.Match(d);
                    if (m.Success && sifre.Contains(m.Groups[1].Value))
                        r.Stavke.Add(new SifraTekst { Sifra = m.Groups[1].Value, Tekst = m.Groups[2].Value.Trim() });
                    else if (r.Stavke.Count > 0)
                        r.Stavke[r.Stavke.Count - 1].Tekst += "; " + d;
                    else
                        bezSifre.Add(d);
                }
            // Tekst pre prve šifre: ako je to stvarni podatak (ima cifre ili nema nijedne stavke sa šifrom), to je stavka bez šifre (G5).
            var podatak = bezSifre.Where(d => Regex.IsMatch(d, @"\d") || r.Stavke.Count == 0 && Regex.IsMatch(d, @"\p{L}{4,}")).ToList();
            if (podatak.Count > 0 && (r.Stavke.Count == 0 || podatak.Any(d => Regex.IsMatch(d, @"\d"))))
                r.Stavke.Insert(0, new SifraTekst { Sifra = "", Tekst = string.Join("; ", podatak) });
            return r;
        }

        private static ListaRubrika Prilozi(List<Rec> reci)
        {
            var r = Pop(new ListaRubrika(), reci, DokumentRec);
            if (!r.Popunjena) return r;
            foreach (var d in Regex.Split(r.Tekst, @"\s*[;\n]\s*").Select(x => x.Trim()).Where(x => x.Length > 1))
            {
                var m = Regex.Match(d, @"^(\d{3})\b[\s:.\-]*(.*)$");
                r.Stavke.Add(m.Success ? new SifraTekst { Sifra = m.Groups[1].Value, Tekst = m.Groups[2].Value.Trim() } : new SifraTekst { Sifra = "", Tekst = d });
            }
            return r;
        }

        private static string Zemlja(string t)
        {
            var nk = (t ?? "").ToLowerInvariant();
            var naziv = NaziviZemalja.Keys.FirstOrDefault(z => Regex.IsMatch(nk, @"(?<!\p{L})" + Regex.Escape(z) + @"(?!\p{L})"));
            if (naziv != null) return naziv;
            var m = Regex.Match(t ?? "", @"(?<![A-Za-z])(RS|HU|RO|BG|GR|HR|SI|MK|ME|BA|TR|CZ|SK|AT|DE|IT|PL)(?![A-Za-z])");
            return m.Success ? m.Value : "";
        }

        /// <summary>Mesto isporuke; zemlja i šifra stanice se traže i u susednoj kućici 12 (često prelaze granicu).</summary>
        private static MestoIsporuke Isporuka(List<Rec> reci, List<Rec> r12)
        {
            var r = Pop(new MestoIsporuke(), reci);
            if (!r.Popunjena) return r;
            var sa12 = r.Tekst + "\n" + Tekst(r12);
            var kucice = Regex.Replace(sa12, @"(?<!\d)(\d)(?:\s(\d)){5,7}(?!\d)", m => Regex.Replace(m.Value, @"\s", ""));
            r.SifraStanice = Regex.Match(kucice, @"(?<!\d)\d{2}\s?\d{4,6}(?!\d)").Value;
            r.Zemlja = Zemlja(sa12);
            if (r.Zemlja.Length == 0 && Regex.IsMatch(r.SifraStanice, @"^\d{2}")) r.Zemlja = r.SifraStanice.Substring(0, 2);
            r.Stanica = Regex.Replace(r.Tekst, @"\d{4,}|\b(" + Regex.Escape(r.Zemlja) + @")\b", " ", RegexOptions.IgnoreCase).Trim(' ', ',', '-', '\n');
            r.Stanica = Regex.Replace(r.Stanica, @"\s+", " ");
            return r;
        }

        private static Preuzimanje Preuzimanje(List<Rec> reci, List<Rec> r17 = null)
        {
            var r = Pop(new Preuzimanje(), reci);
            if (!r.Popunjena) return r;
            // Kućice mesec–dan–čas su uz desnu ivicu rubrike 16 i često padnu u susednu kućicu 17 – traže se i tamo.
            var sa17 = r.Tekst + (r17 == null || r17.Count == 0 ? "" : "\n" + Tekst(r17));
            // Cifre upisane po kućicama ("1 0 2 8 1 2", "1,0 2 3 1,0") se spajaju.
            var t = SpojiKucice(sa17);
            // Šifra stanice (r17, npr. "72 23306-4") se uzima pre traženja datuma (posle se iz teksta uklanja da ne liči na datum).
            var sifraStanice = Regex.Match(t, @"(?<!\d)\d{2}\s?\d{5}-\d(?!\d)").Value;
            r.Datum = NadjiDatum(t, out var poz, out var duz);
            var bezDatuma = poz >= 0 ? t.Remove(poz, duz) : t;
            var cas = Regex.Match(bezDatuma, @"\b([01]?\d|2[0-4])[:.h]([0-5]\d)\b");
            if (cas.Success) r.Cas = cas.Value;
            if (r.Datum.Length == 0)
            {
                // Polja mesec – dan – čas: "10 28 12" ili "102812" (šifra stanice iz 17, npr. "72 16203-2", se ne uzima)
                bezDatuma = Regex.Replace(bezDatuma, @"\b\d{2}\s?\d{5}-?\d\b", " ");
                var m = Regex.Match(bezDatuma, @"(?<!\d)([01]?\d)\s+([0-3]?\d)\s+([0-2]?\d)(?!\d)");
                if (!m.Success) m = Regex.Match(bezDatuma, @"(?<!\d)([01]\d)([0-3]\d)([0-2]\d)(?!\d)");
                if (m.Success)
                {
                    int mes = int.Parse(m.Groups[1].Value), dan = int.Parse(m.Groups[2].Value), sat = int.Parse(m.Groups[3].Value);
                    if (mes >= 1 && mes <= 12 && dan >= 1 && dan <= 31 && sat <= 24)
                    {
                        r.Datum = "0000-" + mes.ToString("00") + "-" + dan.ToString("00");
                        if (r.Cas.Length == 0) r.Cas = sat.ToString("00");
                        bezDatuma = bezDatuma.Remove(m.Index, m.Length);
                    }
                }
            }
            r.SifraStanice = sifraStanice.Length > 0 ? sifraStanice : Regex.Match(bezDatuma, @"(?<!\d)\d{2}\s?\d{4,6}(?!\d)").Value;
            r.Zemlja = Zemlja(bezDatuma);
            r.Mesto = Regex.Replace(Regex.Replace(bezDatuma, @"[\d:.,\-]+", " "), @"\s+", " ").Trim();
            return r;
        }

        private static ListaRubrika Kola(List<Rec> reci)
        {
            var r = Pop(new ListaRubrika(), reci);
            if (!r.Popunjena) return r;
            var nadjeno = false;
            foreach (Match m in KolaRx.Matches(r.Tekst))
                if (Cifre(m.Value).Length >= 8) { r.Stavke.Add(new SifraTekst { Tekst = m.Value.Trim() }); nadjeno = true; }
            var ostatak = KolaRx.Replace(r.Tekst, " ").Trim();
            if (!nadjeno || Regex.IsMatch(ostatak, @"\p{L}{3,}")) r.Stavke.Add(new SifraTekst { Tekst = Regex.Replace(ostatak, @"\s+", " ") });
            return r;
        }

        private static NaznacenjeRobe Roba(List<Rec> reci)
        {
            var r = Pop(new NaznacenjeRobe(), reci);
            if (!r.Popunjena) return r;
            var t = r.Tekst;
            var gornja = t.ToUpperInvariant();
            r.UnBrojevi = Regex.Matches(gornja, @"\bUN\s?-?(\d{4})\b").Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList();
            foreach (Match m in MrnRx.Matches(gornja))
            {
                var pref = Regex.Replace(m.Groups["p"].Value, @"[\s\-]+", " ").Trim();
                r.Mrn.Add(new Mrn { Prefiks = pref, Broj = m.Groups["n"].Value });
            }
            r.Arc = Regex.Matches(gornja, @"\bARC[\s:.\-]*(\d{2}[A-Z]{2}[A-Z0-9]{16,17})\b").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            r.Uti = UtiRx.Matches(gornja).Cast<Match>().Select(m => new Uti { Broj = Regex.Replace(m.Value, @"[\s\-]", "") }).ToList();
            // „224112 - 224118“ (opseg plombi, crtica sa razmacima) nije broj kola; kontrolna cifra se piše „147-9“ ili „147 9“.
            r.BrojeviKola = KolaRx.Matches(t).Cast<Match>().Where(m => !Regex.IsMatch(m.Value, @"\s-|-\s|-\d{2}") && !Regex.IsMatch(m.Value.Trim(), @"^\d{6}\s+\d{6}$")).Select(m => Cifre(m.Value)).Where(x => x.Length == 12).Distinct().ToList();
            r.Ewc = Regex.Matches(gornja, @"\b(?:EWC|EAK|KATALO\w* OTPADA)[\s:.\-]*(\d{2}\s?\d{2}\s?\d{2}\*?)").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            r.Izvoz = Regex.IsMatch(gornja, @"\b(IZVOZ|EXPORT|ИЗВОЗ|AUSFUHR)\b");
            r.ImdgIzjava = Regex.IsMatch(gornja, @"\bIMDG\b|MARINE POLLUTANT|POMORSKO ZAGA");
            r.CuvNapomena = Regex.IsMatch(gornja, @"\bCUV\b") && Regex.IsMatch(gornja, @"SNAG|WIRKUNG|EFFECT|VALEUR|KOLSK|WAGENBRIEF|WAGON NOTE");
            r.Plombe = Regex.IsMatch(gornja, @"PLOMB|\bSEAL|VERSCHL|ПЛОМБ");
            var sablon = Regex.Match(t, @"X{4,}|x{4,}|naziv robe|upisati|enter (the )?goods|beispiel|primer:|lorem ipsum|\.\.\.\.\.", RegexOptions.IgnoreCase);
            if (sablon.Success) { r.ZaostaliTekstSablona = true; r.ZaostaliTekst = sablon.Value; }
            var opis = MrnRx.Replace(gornja, " ");
            r.OpisRobe = Regex.IsMatch(opis, @"\p{L}{3,}") ? t.Split('\n')[0].Trim() : "";
            return r;
        }

        private static TekstRubrika Oznaka(List<Rec> reci)
        {
            // Kućica je označena samo ako je u njoj X; ostali tekst u tom delu obrasca je šum ili prelazak susedne rubrike.
            var r = Pop(new TekstRubrika(), reci);
            if (r.Popunjena && ImaX(reci)) r.Tekst = "X";
            else { r.Status = StatusRubrike.Prazno; r.Tekst = ""; r.Pouzdanost = 1; }
            return r;
        }

        /// <summary>Polje za označavanje → rubrika: oznaceno = "X", prazno, nesigurno = nečitljivo; null ako polja nema u šablonu.</summary>
        private static TekstRubrika OznakaPolja(string stanje)
        {
            if (stanje == null) return null;
            if (stanje == StanjePolja.Oznaceno) return new TekstRubrika { Status = StatusRubrike.Popunjeno, Tekst = "X", Pouzdanost = 0.95 };
            if (stanje == StanjePolja.Prazno) return new TekstRubrika { Status = StatusRubrike.Prazno, Tekst = "", Pouzdanost = 0.95 };
            return new TekstRubrika { Status = StatusRubrike.Necitljivo, Tekst = "", Pouzdanost = 0.4 };
        }

        private static ListaRubrika Nhm(List<Rec> reci)
        {
            var r = PopBroj(new ListaRubrika(), reci);
            if (!r.Popunjena) return r;
            r.Tekst = SpojiKucice(r.Tekst);   // "2 6 2 0" → "2620"
            foreach (Match m in Regex.Matches(r.Tekst, @"(?<![\d.,])\d{2}(?:[ .]?\d{2}){1,3}(?![\d.,])"))
                r.Stavke.Add(new SifraTekst { Sifra = Regex.Replace(m.Value, @"[ .]", "") });
            if (r.Stavke.Count == 0) r.Stavke.Add(new SifraTekst { Sifra = Regex.Replace(r.Tekst, @"\s", "") });
            return r;
        }

        private static Masa Mase(List<Rec> reci)
        {
            var r = Pop(new Masa(), reci);
            if (!r.Popunjena) return r;
            // 55.970 / 55 970 (hiljade), 55970, 55970.0 / 55970,50 (decimale)
            var brojevi = Regex.Matches(r.Tekst, @"(?<![\d.,])\d{1,3}(?:[.,\s]\d{3})+(?![\d])|(?<![\d.,])\d{3,7}[.,]\d{1,2}(?![\d])|(?<![\d.,])\d{3,7}(?![\d.,])").Cast<Match>().Select(m => m.Value.Trim()).ToList();
            foreach (var l in r.Tekst.Split('\n').Where(l => Regex.IsMatch(l, @"tara|tare|eigen", RegexOptions.IgnoreCase)))
            {
                var b = Regex.Match(l, @"\d[\d.,\s]{2,}\d").Value;
                if (b.Length > 0) { r.Tara = b.Trim(); brojevi.Remove(r.Tara); }
            }
            if (brojevi.Count == 0) return r;
            var vrednosti = brojevi.Select(b => (b, v: double.TryParse(Regex.Replace(Regex.Replace(b, @"[.,]\d{1,2}$", ""), @"[.,\s]", ""), out var x) ? x : 0)).ToList();
            var iMax = vrednosti.Select((x, i) => (x.v, i)).OrderByDescending(x => x.v).First().i;
            r.Ukupno = vrednosti[iMax].b;
            if (vrednosti.Count <= 4)
                r.Pojedinacne = vrednosti.Count == 1 ? new List<string> { r.Ukupno } : vrednosti.Where((x, i) => i != iMax).Select(x => x.b).ToList();
            return r;
        }

        private static Iznos IznosRub(List<Rec> reci)
        {
            var r = PopBroj(new Iznos(), reci);
            if (!r.Popunjena) return r;
            var m = Regex.Match(r.Tekst, @"(\d[\d.,\s]*)\s*([A-Z]{3})\b|\b([A-Z]{3})\s*(\d[\d.,\s]*)");
            if (m.Success) { r.Vrednost = (m.Groups[1].Value + m.Groups[4].Value).Trim(); r.Valuta = m.Groups[2].Value + m.Groups[3].Value; }
            else r.Vrednost = Regex.Match(r.Tekst, @"\d[\d.,]*").Value;
            return r;
        }

        private static MestoDatum MestoDatumRub(List<Rec> reci)
        {
            var r = Pop(new MestoDatum(), reci);
            if (!r.Popunjena) return r;
            r.Datum = NadjiDatum(r.Tekst, out var poz, out var duz);
            var bez = poz >= 0 ? r.Tekst.Remove(poz, duz) : r.Tekst;
            r.Mesto = Regex.Replace(Regex.Replace(bez, @"\b\d+\b|[,;:]", " "), @"\s+", " ").Trim();
            return r;
        }

        private static PrevozniciRubrika Prevoznici(List<Rec> reci)
        {
            var r = Pop(new PrevozniciRubrika(), reci);
            if (!r.Popunjena) return r;
            var siroceDeonice = new List<string>();
            foreach (var l0 in r.Tekst.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0))
            {
                // Oznaka zemlje/mreže ispred prevoznika ("SRB:", "HR", "SI:", "FS:", "IŽS:") nije naziv prevoznika.
                var l = Regex.Replace(l0, @"^(?:SRB|RS|HR|SI|SLO|FS|IT|IŽS|IZS|ŽS|ZS|SŽ|HU|BG|RO|MK|BIH|BA|AT|DE|ME|CH)\s*:?(?=\s{1,}\d{4}(?!\d))\s*", "");
                // Kolone rubrike 57 (široki razmak): "šifra naziv" | deonica | status (1/2)
                var celije = l.Split(new[] { "   " }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
                if (celije.Count >= 2)
                {
                    var mc = Regex.Match(celije[0], @"^(?<pre>[^\d]*?)\s*(?<![\d/])(?<s>\d{4})(?![\d/])\s*[-–:,]?\s*(?<n>.*)$");
                    if (mc.Success)
                    {
                        var st = celije.Count >= 3 && Regex.IsMatch(celije.Last(), @"^[12]$") ? celije.Last() : "";
                        var deon = string.Join(" ", celije.Skip(1).Take(celije.Count - 1 - (st.Length > 0 ? 1 : 0)));
                        var m2 = Regex.Match(deon, @"^(.*?)\s+([12])$");
                        if (st.Length == 0 && m2.Success && Regex.IsMatch(m2.Groups[1].Value, @"\p{L}")) { deon = m2.Groups[1].Value; st = m2.Groups[2].Value; }
                        var naz = mc.Groups["n"].Value.Trim(' ', '-', '–', ',');
                        if (naz.Length == 0) naz = mc.Groups["pre"].Value.Trim(' ', '-', '–', ',');
                        // naziv u posebnoj koloni (polja obrasca): „2179 | SZ - 2178 HZ CARGO | BUSCA“ – kratak naziv velikim slovima
                        if (naz.Length == 0 && celije.Count >= 2 && Regex.IsMatch(celije[1], @"^\p{Lu}{2,}[\p{Lu}\d\s\-–./]{0,40}$") && !Regex.IsMatch(celije[1], @"\d{2}\s?\d{4,6}"))
                        {
                            naz = celije[1].Trim(' ', '-', '–', ',');
                            deon = string.Join(" ", celije.Skip(2).Take(Math.Max(0, celije.Count - 2 - (st.Length > 0 ? 1 : 0))));
                        }
                        r.Prevoznici.Add(new Prevoznik { Sifra = mc.Groups["s"].Value, Naziv = naz, Deonica = deon.Trim(' ', '-', '–'), StatusPrevoznika = st });
                        continue;
                    }
                }
                var m = Regex.Match(l, @"^(?<pre>[^\d]*?)\s*(?<![\d/])(?<s>\d{4})(?![\d/])\s*[-–:]?\s*(?<n>.*?)\s*(?<d>\d{2}\s?\d{4,6}(?:-\d)?\s*[-–].*?)?(?<st>[12])?$");
                if (!m.Success)
                {
                    // Red samo sa deonicom (kolona "Relacija" pomerena za red): "72/16550 6 72/0501 Ruma - Šid gr."
                    if (Regex.IsMatch(l, @"\d{2}\s*/\s*\d{4}.*[-–].*\p{L}{3,}")) siroceDeonice.Add(l.Trim(' ', '-', '–', '='));
                    continue;
                }
                var deonica = m.Groups["d"].Value.Trim(' ', '-', '–');
                var status = m.Groups["st"].Value;
                if (deonica.Length == 0) { status = ""; }
                var pre = m.Groups["pre"].Value.Trim(' ', '-', '–', ',');
                var naziv = m.Groups["n"].Value.Trim(' ', '-', '–');
                // Naziv ispred šifre ("KP 3212 Surčin 72 16203-2 - …"): tekst posle šifre je početak deonice (stanica), ne naziv.
                if (Regex.IsMatch(pre, @"\p{L}{2,}"))
                {
                    if (naziv.Length > 0) deonica = (naziv + " " + deonica).Trim();
                    naziv = pre;
                }
                r.Prevoznici.Add(new Prevoznik { Sifra = m.Groups["s"].Value, Naziv = naziv, Deonica = deonica, StatusPrevoznika = status });
            }
            // deonice bez prevoznika u istom redu idu prevoznicima koji nemaju deonicu (redom)
            foreach (var p in r.Prevoznici.Where(x => string.IsNullOrWhiteSpace(x.Deonica)))
            {
                if (siroceDeonice.Count == 0) break;
                p.Deonica = siroceDeonice[0]; siroceDeonice.RemoveAt(0);
            }
            return r;
        }

        private static UgovorniPrevoznik Ugovorni(List<Rec> reci)
        {
            var r = Pop(new UgovorniPrevoznik(), reci);
            if (!r.Popunjena) return r;
            var linije = r.Tekst.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
            if (linije.Count == 0) return r;
            var m = Regex.Match(linije[0], @"^(\d{4})\b\s*[-–:]?\s*(.*)$");
            if (m.Success) { r.Sifra = m.Groups[1].Value; r.Naziv = m.Groups[2].Value.Trim(); }
            else r.Naziv = linije[0];
            if (r.Naziv.Length == 0 && linije.Count > 1) { r.Naziv = linije[1]; linije.RemoveAt(1); }
            r.Adresa = string.Join(", ", linije.Skip(1).Where(l => !l.Equals(r.Naziv, StringComparison.OrdinalIgnoreCase)));
            r.Potpis = true; // potpis (grafika) lokalno ne može pouzdano da se utvrdi
            return r;
        }

        internal static Identifikacija Identifikacija(List<Rec> reci)
        {
            // Odštampan broj rubrike "(62)" / "62)" nije podatak; cifre razdvojene zarezima ("1,6,2032") se spajaju.
            var ciste = reci.Where(w => !Regex.IsMatch(w.T, @"^[\(\[|]?62[\)\]|]")).Select(w => new Rec
            {
                T = SpojiKucice(w.T), X0 = w.X0, X1 = w.X1, Y0 = w.Y0, Y1 = w.Y1, Pouzdanost = w.Pouzdanost, Velicina = w.Velicina, Font = w.Font,
            }).ToList();
            var r = PopBroj(new Identifikacija(), ciste);
            if (!r.Popunjena) return r;
            PopuniR62(r, Raspored.Linije(Ocisti(ciste)), ciste.All(w => string.IsNullOrEmpty(w.Font)));
            return r;
        }

        /// <summary>
        /// Delovi r62 (zemlja, stanica, preduzeće, broj) iz redova: grupe cifara razdvojene širokim razmakom; cifre upisane
        /// po kućicama ("7 2 / 1 6 3 5 0 1") se spajaju.
        /// </summary>
        internal static void PopuniR62(Identifikacija r, List<Linija> linije, bool ocr = false)
        {
            var grupe = new List<string>();
            foreach (var l in linije)
                foreach (var c0 in l.Celije)
                {
                    // OCR ivice kućica čita kao zarez/tačku između cifara ("1,6,2032")
                    var c = ocr ? Regex.Replace(c0, @"(?<=\d)[,.](?=\d)", "") : c0;
                    // cifre po kućicama, i sa kontrolnom cifrom: „1 2 7 4 0 -7“ → „12740-7“
                    var spojeno = Regex.IsMatch(c, @"^(\d\s+)+\d(\s*-\s*\d)?$") ? Regex.Replace(c, @"\s", "") : c;
                    foreach (Match m in Regex.Matches(spojeno, @"\d+(?:-\d)?")) grupe.Add(m.Value);
                }
            grupe = grupe.Where(g => g.Length >= 2).ToList();
            if (grupe.Count >= 4)
            {
                r.Zemlja = grupe[0]; r.Stanica = grupe[1]; r.Preduzece = grupe[2]; r.Broj = grupe[3];
            }
            else if (grupe.Count == 3 && grupe[0].Length == 2) { r.Zemlja = grupe[0]; r.Stanica = grupe[1]; r.Broj = grupe[2]; }
            else if (grupe.Count >= 1) r.Broj = grupe.Last();
            // Ako je zemlja zalepljena za stanicu ("72163501"), razdvoj.
            if (r.Zemlja.Length > 2 && r.Stanica.Length == 0) { r.Stanica = r.Zemlja.Substring(2); r.Zemlja = r.Zemlja.Substring(0, 2); }
            // UIC šifra preduzeća ima 4 cifre; OCR levu ivicu kućice čita kao "1" ("13212" → "3212"). Kod PDF teksta ostaje kako piše (R62 prijavljuje).
            if (ocr && r.Preduzece.Length == 5 && r.Preduzece[0] == '1') r.Preduzece = r.Preduzece.Substring(1);
        }
    }
}
