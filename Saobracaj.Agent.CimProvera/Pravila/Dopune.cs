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
    internal static class Dopune
    {
        public static void Popuni(Nalaz n, Pogodak pg, Kontekst k)
        {
            n.Dopuna = ""; n.Pogresno = ""; n.DopunaSigurna = false;
            if (n.SamoInterno || n.Tezina == Tezina.Info) return;
            string d, p; bool s;
            if (pg.Dopuna != null) { d = pg.Dopuna; p = pg.Pogresno; s = pg.DopunaSigurna; }
            else (d, p, s) = Odredi(n, pg, k);
            d = Ocisti(d);
            n.Dopuna = d;
            n.Pogresno = d.Length > 0 ? (p ?? "").Trim() : "";
            n.DopunaSigurna = d.Length > 0 && s;
        }

        /// <summary>Reči i znaci opisa formata – takva dopuna se ne upisuje (sigurnosna mreža; v2.8.1 test „bez opisa“).</summary>
        internal static readonly Regex OpisFormata = new Regex(
            @"<|>|\bnpr\.|\be\.g\.|naziv, ulica|ulica i broj|street and number|name, street|po[sš]tanski broj i mesto|postcode and place|godina-mesec-dan|year-month-day|mesec-dan-[cč]as|month-day-hour|\bupisati\b|\bunesite\b|\benter\b|\bformat\b|\bsee (description|wagon list)|\bvidi opis|…|\.\.\.",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal static string Ocisti(string d)
        {
            d = Regex.Replace((d ?? "").Replace("\n", ", "), @"\s+", " ").Trim(' ', ',', ';');
            return d.Length == 0 || OpisFormata.IsMatch(d) ? "" : d;
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
                    var iso = Zemlja(k, broj, st);
                    var mesto = broj == "1" ? Mesto(k.C.R16.Mesto) ?? Mesto(k.C.R29.Mesto) : Mesto(k.C.R10.Stanica) ?? Mesto(k.C.R10.Tekst);
                    if (prazna || Fali("naziv", "name")) delovi.Add(en ? "Transport Partner Ltd." : "Transport Partner d.o.o.");
                    if (prazna || Fali("adresa", "address")) { delovi.Add(Ulica(iso)); delovi.Add(PostanskiIMesto(iso, mesto)); }
                    else if (Fali("poštanski broj i mesto", "postcode and place")) delovi.Add(PostanskiIMesto(iso, mesto));
                    if (prazna || Fali("zemlja", "country")) delovi.Add(iso ?? "RS");
                    // kontakt pošiljaoca (primer GIPS r1: „RS, Marko Marković“) – kada u rubrici nema telefona ni e-maila
                    if (broj == "1" && Prazno(st.Telefon) && Prazno(st.Email)) delovi.Add(en ? "John Smith" : "Marko Marković");
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
                        delovi.Add(Mesto(k.C.R29.Mesto) ?? Mesto(k.C.R1.Adresa) ?? (en ? "Novi Sad" : "Novi Sad"));
                    var datum = Datum(k.C.R29.Datum, k.U.ReferentniDatum) ?? k.U.ReferentniDatum;
                    var bezDatuma = prazna || Fali("datum", "date");
                    var bezCasa = prazna || Fali("vreme (čas)", "time (hour)");
                    if (bezDatuma) delovi.Add(datum.ToString("MM-dd", CultureInfo.InvariantCulture) + (bezCasa ? "-08" : ""));
                    else if (bezCasa) delovi.Add("08");
                    return (string.Join(", ", delovi), null, false);
                }
                case "R29":
                {
                    if (n.Detalj != null && Regex.IsMatch(n.Detalj, "budućnosti|future")) return ("", null, false);
                    var delovi = new List<string>();
                    if (prazna || Fali("mesto", "place")) delovi.Add(Mesto(k.C.R16.Mesto) ?? Mesto(k.C.R1.Adresa) ?? "Novi Sad");
                    if (prazna || Fali("datum ispostavljanja", "date of issue"))
                        delovi.Add((Datum(k.C.R16.Datum, k.U.ReferentniDatum) ?? k.U.ReferentniDatum).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    return (string.Join(", ", delovi), null, false);
                }
                case "R62":
                {
                    var r = k.C.R62;
                    var delovi = new List<string>();
                    var bez = prazna;
                    var zemlja = !Prazno(r.Zemlja) ? null : UicZemlje(k);
                    if (bez || Fali("zemlja", "country")) delovi.Add(zemlja ?? "72");
                    if (bez || Fali("stanica", "station")) delovi.Add(Stanica(k.C.R16.SifraStanice) ?? "16350-1");
                    if (bez || Fali("šifra preduzeća", "company code")) delovi.Add(Cif(k.C.R58a.Sifra).Length == 4 ? Cif(k.C.R58a.Sifra) : k.P.Kp.Uic);
                    var losBroj = Fali("u obliku 12345-6", "in the format 12345-6");
                    if (bez || Fali("broj otpravljanja", "consignment number")) delovi.Add("15890-0");
                    return (string.Join(" ", delovi), losBroj ? r.Broj : null, false);
                }
                case "R58A":
                {
                    // ugovorni prevoznik = preduzeće iz r62; naziv iz r57 (ista šifra)
                    var sifra = Cif(k.C.R62.Preduzece);
                    if (sifra.Length != 4) sifra = k.C.R57.Prevoznici.Select(x => Cif(x.Sifra)).FirstOrDefault(x => x.Length == 4) ?? "";
                    if (sifra.Length != 4) return ("", null, false);
                    var naziv = k.P.Kp.JeKpSifra(sifra) ? k.P.Kp.Naziv : k.C.R57.Prevoznici.FirstOrDefault(x => Cif(x.Sifra) == sifra)?.Naziv;
                    // „RO: PSP UIC“ → „PSP“ (oznaka mreže i reč UIC nisu deo naziva)
                    naziv = Regex.Replace(Regex.Replace(naziv ?? "", @"^\s*[A-ZŽŠČĆĐ]{2,4}\s*:\s*", ""), @"\b(UIC|CIT)\b|\d{4}", "").Trim(' ', ',', '-', ':', '(', ')');
                    return ((sifra + " " + (naziv ?? "")).Trim(), null, false);
                }
                case "KP2":
                {
                    var r = k.C.R58a;
                    var pogresno = r.Popunjena && !Prazno(r.Sifra) && !k.P.Kp.JeKpSifra(r.Sifra) ? r.Sifra : null;
                    return (k.P.Kp.Uic + " " + k.P.Kp.Naziv, pogresno, false);
                }
                case "KP3":
                case "KP57":
                    return (k.P.Kp.Uic + " KP", null, false);
                case "KP7-PLATILAC":
                    return (en ? "Payer for SRB: Transport Partner Ltd." : "Platilac za deonicu SRB: Transport Partner d.o.o.", null, false);
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
                    // masa robe se ne može odrediti iz dokumenata – realan primer (prazna kola: preporuka bez upisa)
                    return greska ? (en ? "Net 1,980,000 kg" : "Neto 1 980 000 kg", null, false) : ("", null, false);
                default:
                    // predlog pravila iz pratećih dokumenata ili istog lista – samo kada je to vrednost, a ne opis
                    if (greska && !Prazno(pg.Predlog)) return (pg.Predlog, null, false);
                    return ("", null, false);
            }
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
            if (!Prazno(st.NazivZemlje) && Provere.IsoPoNazivu.TryGetValue(st.NazivZemlje.Trim(), out var iz)) return iz;
            var t = (st.Tekst ?? "") + " " + (st.Adresa ?? "");
            foreach (var kv in PoznataMesta) if (Regex.IsMatch(t, @"(?<!\p{L})" + Regex.Escape(kv.Key) + @"(?!\p{L})", RegexOptions.IgnoreCase)) return kv.Value.iso;
            return Provere.ZemljaLista(k, broj);
        }

        /// <summary>Poznata mesta (stanice i gradovi iz dosadašnjih listova): poštanski broj i zemlja – za dopunu „21000 Novi Sad, RS“.</summary>
        private static readonly Dictionary<string, (string psb, string iso)> PoznataMesta = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Novi Sad"] = ("21000", "RS"), ["Beograd"] = ("11000", "RS"), ["Belgrade"] = ("11000", "RS"), ["Novi Beograd"] = ("11070", "RS"), ["Šabac"] = ("15000", "RS"), ["Sabac"] = ("15000", "RS"),
            ["Ruma"] = ("22400", "RS"), ["Sremska Mitrovica"] = ("22000", "RS"), ["Pančevo"] = ("26000", "RS"), ["Zrenjanin"] = ("23000", "RS"), ["Subotica"] = ("24000", "RS"),
            ["Prokuplje"] = ("18400", "RS"), ["Niš"] = ("18000", "RS"), ["Kragujevac"] = ("34000", "RS"), ["Vrbas"] = ("21460", "RS"), ["Stara Pazova"] = ("22300", "RS"),
            ["Lovosice"] = ("410 02", "CZ"), ["Budapest"] = ("1051", "HU"), ["Kiskőrös"] = ("6200", "HU"), ["Ljubljana"] = ("1000", "SI"), ["Zagreb"] = ("10000", "HR"),
            ["Constanta"] = ("900001", "RO"), ["Constanța"] = ("900001", "RO"), ["Wien"] = ("1010", "AT"), ["Nonantola"] = ("41015", "IT"),
        };

        private static string PostanskiIMesto(string iso, string mesto)
        {
            if (mesto != null && PoznataMesta.TryGetValue(mesto, out var pm)) return pm.psb + " " + mesto;
            if (mesto != null) return mesto;
            switch (iso ?? "RS")
            {
                case "CZ": return "110 00 Praha";
                case "HU": return "1051 Budapest";
                case "RO": return "900001 Constanța";
                case "SI": return "1000 Ljubljana";
                case "HR": return "10000 Zagreb";
                case "AT": return "1010 Wien";
                case "DE": return "10115 Berlin";
                case "IT": return "00184 Roma";
                case "BG": return "1000 Sofia";
                case "SK": return "811 01 Bratislava";
                default: return "21000 Novi Sad";
            }
        }

        private static string Ulica(string iso)
        {
            switch (iso ?? "RS")
            {
                case "RS": case "BA": case "ME": case "HR": return "Bulevar oslobođenja 12";
                case "DE": case "AT": return "Hauptstraße 12";
                case "HU": return "Fő utca 12";
                case "CZ": return "Nádražní 12";
                case "RO": return "Strada Portului 12";
                default: return "Main Street 12";
            }
        }

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
            if (!Regex.IsMatch(t, @"\p{L}{3}")) return null;
            return t.Length > 1 && t == t.ToUpperInvariant() ? CultureInfo.GetCultureInfo("sr-Latn-RS").TextInfo.ToTitleCase(t.ToLowerInvariant()) : t;
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
