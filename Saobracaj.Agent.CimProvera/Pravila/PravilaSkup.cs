using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Saobracaj.Common.Text;

namespace Saobracaj.Agent.CimProvera.Pravila
{
    /// <summary>Jedno pravilo iz JSON-a (bilo koje grupe).</summary>
    internal sealed class PraviloDef
    {
        public string Id;
        public string Grupa;
        public JObject Json;
        public string Tezina => (string)Json["severity"] ?? "error";
        public string Polje => (string)Json["field"];
        public bool PopunjavaPrevoznik => string.Equals((string)Json["filled_by"], "carrier", StringComparison.OrdinalIgnoreCase);
        public string Poruka(bool en) => (en ? (string)Json["message_en"] : null) ?? (string)Json["message"] ?? (string)Json["check"] ?? Id;
    }

    /// <summary>
    /// cim_validation_rules_*.json – jedini izvor pravila (težine, tekstovi poruka, šifarnici, šablon poruke).
    /// Učitava se pri svakom pokretanju. Ako share nije dostupan, koristi se poslednja učitana kopija iz lokalnog keša.
    /// Pri promeni fajla prethodna verzija se čuva u pravila\istorija\.
    /// </summary>
    internal sealed class PravilaSkup
    {
        public static readonly string[] RedGrupa =
            { "general_rules", "field_rules", "r21_subrules", "cross_rules", "attachment_rules", "company_rules", "recommendations" };

        public JObject Json { get; private set; }
        public string Verzija => (string)Json["meta"]?["version"] ?? "?";
        public string Izvor { get; private set; }
        public string Hes { get; private set; }
        public List<PraviloDef> Sva { get; } = new List<PraviloDef>();
        public Dictionary<string, PraviloDef> PoId { get; } = new Dictionary<string, PraviloDef>(StringComparer.OrdinalIgnoreCase);
        public PraviloKp Kp { get; private set; }
        public List<string> Upozorenja { get; } = new List<string>();

        public static PravilaSkup IzTeksta(string json, string izvor = "(tekst)")
        {
            var p = new PravilaSkup { Izvor = izvor };
            p.Postavi(JObject.Parse(json), Encoding.UTF8.GetBytes(json));
            return p;
        }

        /// <summary>
        /// Učitava pravila sa share-a (folder iz config-a), uz lokalni keš u StanjeFolder\pravila_kes.
        /// </summary>
        public static PravilaSkup Ucitaj(string folder, string fajl, string stanjeFolder, Log log)
        {
            var putanja = Path.Combine(folder, fajl);
            var kesFolder = Path.Combine(stanjeFolder, "pravila_kes");
            var kes = Path.Combine(kesFolder, fajl);
            byte[] bajtovi;
            string izvor;
            try
            {
                bajtovi = File.ReadAllBytes(putanja);
                izvor = putanja;
            }
            catch (Exception ex)
            {
                var uzExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pravila", fajl);
                var rezerva = File.Exists(kes) ? kes : File.Exists(uzExe) ? uzExe : null;
                if (rezerva == null) throw new InvalidOperationException("Pravila nisu dostupna (" + putanja + ") i nema lokalne kopije.", ex);
                log?.Upozorenje("Pravila nisu dostupna na " + putanja + " (" + ex.Message + "). Koristi se lokalna kopija " + rezerva);
                bajtovi = File.ReadAllBytes(rezerva);
                izvor = rezerva + " (lokalna kopija)";
                kes = rezerva;
            }

            JObject j;
            try { j = JObject.Parse(Encoding.UTF8.GetString(bajtovi)); }
            catch (Exception ex)
            {
                if (!File.Exists(kes) || izvor.StartsWith(kes)) throw new InvalidOperationException("Neispravan JSON pravila: " + putanja, ex);
                log?.Greska("Neispravan JSON pravila " + putanja + " – koristi se prethodna verzija iz keša. " + ex.Message);
                bajtovi = File.ReadAllBytes(kes);
                j = JObject.Parse(Encoding.UTF8.GetString(bajtovi));
                izvor = kes + " (prethodna ispravna verzija)";
            }

            var p = new PravilaSkup { Izvor = izvor };
            p.Postavi(j, bajtovi);

            // Istorija: ako se sadržaj promenio u odnosu na keš, stara verzija ide u pravila\istorija (na share-u, ili lokalno).
            try
            {
                Directory.CreateDirectory(kesFolder);
                if (File.Exists(kes))
                {
                    var stari = File.ReadAllBytes(kes);
                    if (Prilozi.Prilog.Sha256(stari) != p.Hes && !izvor.StartsWith(kes))
                    {
                        var ist = Path.Combine(folder, "istorija");
                        try { Directory.CreateDirectory(ist); } catch { ist = Path.Combine(kesFolder, "istorija"); Directory.CreateDirectory(ist); }
                        var ime = DateTime.Now.ToString("yyyy-MM-dd_HHmmss") + "_pre_izmene_" + fajl;
                        File.WriteAllBytes(Path.Combine(ist, ime), stari);
                        log?.Info("Pravila su izmenjena; prethodna verzija sačuvana kao istorija\\" + ime);
                    }
                }
                if (!izvor.StartsWith(kes)) File.WriteAllBytes(kes, bajtovi);
            }
            catch (Exception ex) { log?.Upozorenje("Istorija/keš pravila nije upisan: " + ex.Message); }

            log?.Info("Pravila v" + p.Verzija + " iz " + izvor + " (" + p.Sva.Count + " pravila, heš " + p.Hes.Substring(0, 12) + ")");
            return p;
        }

        private void Postavi(JObject j, byte[] bajtovi)
        {
            Json = j;
            Hes = Prilozi.Prilog.Sha256(bajtovi);
            foreach (var g in RedGrupa)
            {
                if (!(j[g] is JArray niz)) continue;
                int i = 0;
                foreach (var t in niz)
                {
                    i++;
                    if (t is JObject o && o["id"] != null)
                    {
                        var d = new PraviloDef { Id = (string)o["id"], Grupa = g, Json = o };
                        Sva.Add(d);
                        PoId[d.Id] = d;
                    }
                    else if (t.Type == JTokenType.String && g == "recommendations")
                    {
                        // Preporuke su u JSON-u tekst bez ID-a: dobijaju REC1, REC2… po redosledu.
                        var o2 = new JObject
                        {
                            ["id"] = "REC" + i, ["severity"] = "recommendation", ["message"] = "Preporuka: " + (string)t,
                        };
                        var en = (j["recommendations_en"] as JArray)?.ElementAtOrDefault(i - 1);
                        if (en != null) o2["message_en"] = "Recommendation: " + (string)en;
                        var d = new PraviloDef { Id = "REC" + i, Grupa = g, Json = o2 };
                        Sva.Add(d);
                        PoId[d.Id] = d;
                    }
                }
            }
            var aliasi = (j["meta"]?["company"]?["aliases"] as JArray)?.Select(x => (string)x).ToList() ?? new List<string>();
            Kp = new PraviloKp((string)j["meta"]?["company"]?["uic"] ?? "3212", (string)j["meta"]?["company"]?["name"] ?? "Kombinovani prevoz d.o.o.", aliasi);
            if ((string)j["meta"]?["company"]?["address"] is string adr && adr.Length > 0) Kp.Adresa = adr;
        }

        // ---------- šifarnici ----------
        public JObject R7 => Json["codelists"]?["r7"] as JObject ?? new JObject();
        public HashSet<string> Valute => Lista("currencies");
        public HashSet<string> ValuteZastarele => Lista("currencies_deprecated");
        public HashSet<string> MrnPrefiksi => Lista("r21_mrn_prefixes");
        /// <summary>NHM (početak šifre) robe koja je otpad (codelists.waste_nhm): 7204, 4707, 3915, 7602, 7404…</summary>
        public List<string> OtpadNhm => (Json["codelists"]?["waste_nhm"] as JArray)?.Select(x => (string)x).ToList() ?? new List<string> { "7204", "4707", "3915", "7602", "7404" };

        private HashSet<string> Lista(string kljuc) =>
            new HashSet<string>((Json["codelists"]?[kljuc] as JArray)?.Select(x => ((string)x).ToUpperInvariant()) ?? Enumerable.Empty<string>());

        /// <summary>Sistemska poruka (system_messages u JSON-u), npr. necitljivo, nema_tovarnog_lista.</summary>
        public string Sistemska(string kljuc, bool en, string podrazumevano)
        {
            var o = Json["system_messages"]?[kljuc];
            return (en ? (string)o?["message_en"] : null) ?? (string)o?["message"] ?? podrazumevano;
        }

        public string Sablon(string kljuc, bool en, string podrazumevano)
        {
            var t = (en ? Json["response_template_en"]?[kljuc] : null) ?? Json["response_template"]?[kljuc];
            return t?.Type == JTokenType.String ? (string)t : podrazumevano;
        }

        public string SablonStatus(string kljuc, bool en, string podrazumevano)
        {
            var t = (en ? Json["response_template_en"]?["status_values"]?[kljuc] : null) ?? Json["response_template"]?["status_values"]?[kljuc];
            return (string)t ?? podrazumevano;
        }
    }

    /// <summary>Prepoznavanje Kombinovanog prevoza po meta.company.aliases i UIC šifri (bez obzira na velika/mala slova, razmake, interpunkciju).</summary>
    internal sealed class PraviloKp
    {
        public string Uic { get; }
        public string Naziv { get; }
        private readonly List<string> kljucevi;

        /// <summary>Oblici naziva koji uvek važe (uz aliase iz JSON-a); ćirilica se preslovljava pre poređenja.</summary>
        private static readonly string[] OsnovniOblici = { "kp", "k prevoz", "kombinovani prevoz", "kombinovani prevoz doo", "kombinovani prevoz d o o" };

        public PraviloKp(string uic, string naziv, IEnumerable<string> aliasi)
        {
            Uic = uic; Naziv = naziv;
            kljucevi = aliasi.Concat(new[] { naziv }).Concat(OsnovniOblici).Select(Normalizuj).Where(k => k.Length > 0).Distinct().OrderByDescending(k => k.Length).ToList();
        }

        /// <summary>Mala slova, bez dijakritika, ćirilica u latinici, samo slova i cifre razdvojeni jednim razmakom.</summary>
        internal static string Normalizuj(string s) =>
            Regex.Replace(TekstUtil.BezDijakritika(TekstUtil.UjednaciZnakove(s ?? "")).ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();

        /// <summary>OCR zamene u oznaci KP: 3Z12 / 321Z / 32l2 / 32I2 → 3212; „K P“ → „KP“.</summary>
        internal string OcrIspravka(string tekst)
        {
            if (string.IsNullOrEmpty(tekst)) return tekst ?? "";
            var t = Regex.Replace(tekst, @"(?<![\dA-Za-z])3[2Zz]1[2Zz](?![\dA-Za-z])|(?<![\dA-Za-z])32[lI|]2(?![\dA-Za-z])", Uic);
            // „K P“ → KP; latinično KP koje OCR pročita ćiriličnim slovima istog oblika („КР“, „КP“) → KP
            t = Regex.Replace(t, @"(?<![A-Za-zА-яЁё])[KК]\s*[PР](?![A-Za-zА-яЁё])", "KP");
            return t;
        }

        /// <summary>
        /// Naziv KP u bilo kom obliku (KP, K-prevoz, Kombinovani prevoz, КП, KPUIC3212, IŽS: KP…), bez oslanjanja na samu šifru.
        /// </summary>
        public bool PominjeNaziv(string tekst)
        {
            if (string.IsNullOrWhiteSpace(tekst)) return false;
            var t = " " + Normalizuj(OcrIspravka(tekst)) + " ";
            var spojeno = " " + t.Replace(" ", "") + " ";
            foreach (var k in kljucevi)
            {
                if (t.Contains(" " + k + " ")) return true;
                if (k.Length >= 6 && spojeno.Contains(k.Replace(" ", ""))) return true;
            }
            return Regex.IsMatch(t, @" kp ?(uic)? ?" + Uic + " | " + Uic + " ?kp ") || spojeno.Contains("kpuic" + Uic);
        }

        /// <summary>„3212“ kao samostalan broj uz „UIC“ / „Frachtzahler“ / platioca (r7), ne kao deo dužeg broja (telefon, PDV, broj otpravljanja).</summary>
        public bool SifraUzOznaku(string tekst) =>
            Regex.IsMatch(OcrIspravka(tekst ?? ""), @"(UIC|Frachtzahler|platilac|platioc|payer|zahler)\W{0,20}(?<!\d)" + Regex.Escape(Uic) + @"(?!\d)|(?<!\d)" + Regex.Escape(Uic) + @"(?!\d)\W{0,3}UIC", RegexOptions.IgnoreCase);

        /// <summary>Tekst pominje KP (alias kao cela reč/izraz, ili UIC šifru kao samostalan broj).</summary>
        public bool JeKp(string tekst)
        {
            if (string.IsNullOrWhiteSpace(tekst)) return false;
            tekst = OcrIspravka(tekst);
            var t = " " + Normalizuj(tekst) + " ";
            var spojeno = " " + t.Replace(" ", "") + " ";
            foreach (var k in kljucevi)
            {
                if (t.Contains(" " + k + " ")) return true;
                if (k.Length >= 6 && spojeno.Contains(k.Replace(" ", ""))) return true; // "KPUIC3212", "Kombinovaniprevoz"
            }
            return Regex.IsMatch(tekst, @"(?<!\d)" + Regex.Escape(Uic) + @"(?!\d)");
        }

        /// <summary>Naziv (bez šifre) odgovara KP-u.</summary>
        public bool JeKpNaziv(string naziv)
        {
            if (string.IsNullOrWhiteSpace(naziv)) return false;
            var bezSifre = Regex.Replace(naziv, @"(?<!\d)" + Regex.Escape(Uic) + @"(?!\d)", " ");
            bezSifre = Regex.Replace(bezSifre, @"\bUIC\b", " ", RegexOptions.IgnoreCase);
            return JeKp(bezSifre) && !string.IsNullOrWhiteSpace(Normalizuj(bezSifre));
        }

        /// <summary>Adresa KP (meta.company.address u JSON-u), npr. "Jug Bogdanova 28, 18400 Prokuplje".</summary>
        public string Adresa { get; set; } = "Jug Bogdanova 28, 18400 Prokuplje";

        /// <summary>
        /// Tekst (r58a, r57, pečat) su poznati podaci KP, približno (OCR greške): bar dva od – naziv (kombinovani / prevoz / KP),
        /// UIC 3212 (i "212" uz UIC), ulica, poštanski broj, mesto. "prevoz" UIC 212, / Bogdanova 28, / 8400 …" je KP.
        /// </summary>
        public bool JePoznatKp(string tekst)
        {
            if (string.IsNullOrWhiteSpace(tekst)) return false;
            var tokeni = Normalizuj(OcrIspravka(tekst)).Split(' ').Where(x => x.Length > 0).ToList();
            bool Slicno(string uzorak, int tol) => tokeni.Any(x => Math.Abs(x.Length - uzorak.Length) <= tol && TekstUtil.Rastojanje(x, uzorak) <= tol);
            var adresa = Normalizuj(Adresa).Split(' ').Where(x => x.Length >= 5 && !Regex.IsMatch(x, @"^\d+$")).ToList();
            var postanski = Regex.Match(Adresa, @"\b(\d{5})\b").Value;
            int poena = 0;
            if (Slicno("kombinovani", 2) || Slicno("prevoz", 1) || tokeni.Contains("kp")) poena++;
            if (Regex.IsMatch(OcrIspravka(tekst), @"UIC\W{0,3}3?212(?!\d)", RegexOptions.IgnoreCase) || JeKp(tekst) && Regex.IsMatch(tekst, @"(?<!\d)3212(?!\d)")) poena++;
            if (adresa.Any(a => Slicno(a, 2))) poena++;
            if (postanski.Length == 5 && tokeni.Any(x => x.Length >= 4 && postanski.EndsWith(x))) poena++;
            return poena >= 2;
        }

        public bool JeKpSifra(string sifra) => Regex.Replace(OcrIspravka(sifra ?? ""), @"\D", "") == Uic;
    }
}
