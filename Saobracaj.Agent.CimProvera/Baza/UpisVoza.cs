using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera.Baza
{
    /// <summary>v2.9.1: jedan tovarni list u upisu (posle spajanja kopija) – vrednosti, dokument i oznaka.</summary>
    internal sealed class ListPosla
    {
        public PodaciPosla P;
        public int? DokId;
        public List<int> SviDokumenti = new List<int>();
        public string Oznaka = "";
        public int Kopija = 1;
        public int? Stanica1 => P.V.TryGetValue("Stanica1ID", out var v) ? v as int? : null;
        public int? Stanica3 => P.V.TryGetValue("Stanica3ID", out var v) ? v as int? : null;
        public int? Otpravna => P.V.TryGetValue("OtpravnaID", out var v) ? v as int? : null;
        public int? Uputna => P.V.TryGetValue("UputnaID", out var v) ? v as int? : null;
        public int BrojKola => P.V.TryGetValue("BrojKola1", out var v) && v is int i ? i : 0;
        /// <summary>Ključ zapisa TL: ključ lista; bez pouzdane r62 dopunjen brojevima kola (listovi istog voza dele ključ „ref:“).</summary>
        public string KljucTl => (P.KljucPosiljke ?? "").StartsWith("r62:", StringComparison.Ordinal) || P.Kola.Count == 0 ? P.KljucPosiljke ?? ""
            : P.KljucPosiljke + "|kola:" + string.Join(",", P.Kola.OrderBy(x => x, StringComparer.Ordinal).Take(3)) + (P.Kola.Count > 3 ? ",+" + (P.Kola.Count - 3) : "");
        public override string ToString() => Oznaka + " [" + P.KljucPosiljke + "]";
    }

    /// <summary>v2.9.1: tovarni listovi istog voza u jednom mailu (ista deonica KP: Stanica1 i Stanica3).</summary>
    internal sealed class VozListova
    {
        public List<ListPosla> Listovi = new List<ListPosla>();
        public bool SvaPrazna => Listovi.Count > 0 && Listovi.All(x => x.P.Prazna);
    }

    /// <summary>
    /// v2.9.1: zapis tovarnog lista u poslu (red u tmpJobsPolje, Kolona „TL:&lt;ključ&gt;“, JSON u TekstIzDokumenta) – od njih se računaju
    /// grupe posla (grupa 1 = natovarena kola, grupa 2 = prazna kola / druga roba) i kada stignu u različitim mailovima.
    /// </summary>
    internal sealed class ZapisTl
    {
        [JsonProperty("k")] public string Kljuc = "";
        [JsonProperty("o")] public string Obrazac = "";
        [JsonProperty("p")] public bool Prazna;
        [JsonProperty("n4")] public string Nhm4 = "";
        [JsonProperty("nid")] public int? NhmId;
        [JsonProperty("on")] public string OstaliNhm = "";
        [JsonProperty("bk")] public int BrojKola;
        [JsonProperty("ne")] public double? Neto;
        [JsonProperty("ta")] public double? Tara;
        [JsonProperty("br")] public double? Bruto;
        [JsonProperty("om")] public string Om = "";
        [JsonProperty("rid")] public string Rid = "";
        [JsonProperty("pov")] public bool Povratak;
        [JsonProperty("kola")] public List<string> Kola = new List<string>();

        public string Grupa => Prazna ? "9922" : Nhm4 ?? "";

        public static ZapisTl Iz(ListPosla l)
        {
            var p = l.P;
            string S(string k) => p.V.TryGetValue(k, out var v) ? v as string ?? "" : "";
            var z = new ZapisTl
            {
                Kljuc = l.KljucTl, Obrazac = p.Obrazac, Prazna = p.Prazna, Nhm4 = p.Nhm4, NhmId = p.V.TryGetValue("NHM1", out var n) ? n as int? : null, OstaliNhm = p.OstaliNhm,
                BrojKola = l.BrojKola, Neto = p.NetoKg, Tara = p.TaraKg, Bruto = p.BrutoKg, Om = S("OMBroj1"), Rid = S("RIDBroj1"), Kola = p.Kola.ToList(),
            };
            // prazna kola: neto 0; bruto = tara kada je navedena samo jedna
            if (z.Prazna) { z.Neto = z.Neto ?? 0; if (z.Bruto == null && z.Tara.HasValue) z.Bruto = z.Tara; if (z.Tara == null && z.Bruto.HasValue) z.Tara = z.Bruto; }
            return z;
        }

        public string Json()
        {
            var t = JsonConvert.SerializeObject(this);
            while (t.Length > 1000 && Kola.Count > 0) { Kola.RemoveAt(Kola.Count - 1); t = JsonConvert.SerializeObject(this); }
            return t;
        }

        public string Opis(int grupa) => "grupa " + grupa + ": " + (Obrazac.Length > 0 ? Obrazac + ", " : "") + BrojKola + " kola, NHM " + (Prazna ? "9922 (prazna)" : Nhm4)
            + ", neto " + T(Neto) + " t, tara " + T(Tara) + " t, bruto " + T(Bruto) + " t" + (Povratak ? ", povratak praznih kola" : "");

        private static string T(double? kg) => kg.HasValue ? (kg.Value / 1000).ToString("0.000", CultureInfo.InvariantCulture) : "–";
    }

    internal sealed partial class UpisUBazu
    {
        // ------------------------------------------------------------------------------------------- kopije i vozovi

        /// <summary>Ključ za kolonu tmpJobsPolje (do 128 znakova).</summary>
        internal static string KolonaTl(string kljuc) =>
            "TL:" + ((kljuc ?? "").Length <= 120 ? kljuc : kljuc.Substring(0, 100) + "#" + Prilog.Sha256(System.Text.Encoding.UTF8.GetBytes(kljuc)).Substring(0, 12));

        /// <summary>
        /// v2.9.1 (4.1): listovi istog kompleta (original, faktura, list prijema, duplikat…) i kopije u istom skenu su jedan tovarni list:
        /// isti ključ (r62); kada r62 nije pouzdana – isti brojevi kola, ili (bez kola) isti pošiljalac, primalac, otpravna i uputna.
        /// Za svako polje uzima se najbolje pročitana kopija (veća pouzdanost porekla).
        /// </summary>
        internal static List<ListPosla> SpojiKopije(List<ListPosla> listovi)
        {
            var rez = new List<ListPosla>();
            bool R62(ListPosla x) => (x.P.KljucPosiljke ?? "").StartsWith("r62:", StringComparison.Ordinal);
            foreach (var l in listovi.OrderByDescending(R62).ThenByDescending(x => x.P.Poreklo.Sum(po => po.Vrednost == null ? 0 : po.Pouzdanost)))
            {
                var isti = rez.FirstOrDefault(x => IstiList(x, l));
                if (isti == null) { rez.Add(l); continue; }
                Dopuni(isti, l);
            }
            return rez;
        }

        private static bool IstiList(ListPosla a, ListPosla b)
        {
            var ka = a.P.KljucPosiljke ?? ""; var kb = b.P.KljucPosiljke ?? "";
            bool r62a = ka.StartsWith("r62:", StringComparison.Ordinal), r62b = kb.StartsWith("r62:", StringComparison.Ordinal);
            if (r62a && r62b) return ka == kb;   // dve pouzdane r62: isti broj = isti list
            // bez pouzdane r62 (ključ „ref:“ je isti za sve listove istog voza): odlučuju brojevi kola kada ih oba lista imaju
            // (kopija skena: OCR može da pogreši pojedine brojeve – dovoljno je 80 % istih kola), uz istu masu robe (± 2 %)
            if (a.P.NetoKg > 0 && b.P.NetoKg > 0 && Math.Abs(a.P.NetoKg.Value - b.P.NetoKg.Value) > 0.02 * Math.Max(a.P.NetoKg.Value, b.P.NetoKg.Value)) return false;
            if (a.P.Kola.Count > 0 && b.P.Kola.Count > 0) return a.P.Kola.Intersect(b.P.Kola).Count() >= 0.8 * Math.Min(a.P.Kola.Count, b.P.Kola.Count);
            // ista otpravna i uputna i iste mase (bar dve od tri, nijedna različita) – kopija skena sa nečitljivom r62 (Šabac: 11132026Scan130104.pdf)
            var iste = 0; var razlicite = 0;
            foreach (var (x, y) in new[] { (a.P.NetoKg, b.P.NetoKg), (a.P.TaraKg, b.P.TaraKg), (a.P.BrutoKg, b.P.BrutoKg) })
                if (x > 0 && y > 0) { if (Math.Abs(x.Value - y.Value) <= 0.005 * Math.Max(x.Value, y.Value)) iste++; else razlicite++; }
            if (iste >= 2 && razlicite == 0 && a.Otpravna.HasValue && a.Otpravna == b.Otpravna && a.Uputna.HasValue && a.Uputna == b.Uputna) return true;
            if (ka == kb && ka.Length > 0 && a.P.Kola.Count == 0 && b.P.Kola.Count == 0) return true;
            if (a.P.Kola.Count > 0 || b.P.Kola.Count > 0) return false;
            return a.P.Posiljalac.Length >= 3 && Slicnost.Ocena(a.P.Posiljalac, b.P.Posiljalac) >= 0.9 && a.P.Primalac.Length >= 3 && Slicnost.Ocena(a.P.Primalac, b.P.Primalac) >= 0.9
                   && a.Otpravna.HasValue && a.Otpravna == b.Otpravna && a.Uputna.HasValue && a.Uputna == b.Uputna;
        }

        /// <summary>Kopija dopunjuje list: polje se zamenjuje kada je u kopiji pročitano pouzdanije (ili nije bilo pročitano).</summary>
        private static void Dopuni(ListPosla l, ListPosla kopija)
        {
            l.Kopija++;
            if (kopija.DokId.HasValue && !l.SviDokumenti.Contains(kopija.DokId.Value)) l.SviDokumenti.Add(kopija.DokId.Value);
            foreach (var po in kopija.P.Poreklo)
            {
                if (po.Kolona == "KljucPosiljke" || po.Vrednost == null) continue;
                var staro = l.P.Poreklo.FirstOrDefault(x => x.Kolona == po.Kolona);
                var bolje = staro == null || staro.Vrednost == null || (staro.Vrednost is decimal d && d == 0 && !(po.Vrednost is decimal d2 && d2 == 0)) || po.Pouzdanost > staro.Pouzdanost + 5;
                if (!bolje) continue;
                l.P.Postavi(po.Kolona, po.Vrednost, po.Tekst + " [kopija " + System.IO.Path.GetFileName(po.Fajl) + "]", po.Rubrika, po.Pouzdanost, po.Kandidati, po.Fajl);
            }
            if (l.P.NetoKg == null) l.P.NetoKg = kopija.P.NetoKg;
            if (l.P.TaraKg == null) l.P.TaraKg = kopija.P.TaraKg;
            if (l.P.BrutoKg == null) l.P.BrutoKg = kopija.P.BrutoKg;
            if (l.P.Kola.Count == 0) l.P.Kola = kopija.P.Kola;
            if (l.P.Nhm4.Length == 0) l.P.Nhm4 = kopija.P.Nhm4;
            if (l.P.BrojVoza.Length == 0) l.P.BrojVoza = kopija.P.BrojVoza;
        }

        /// <summary>
        /// v2.9.1 (4.2): tovarni listovi istog maila sa istom deonicom KP (Stanica1 i Stanica3) su jedan voz; list bez neke od tih stanica
        /// ide u voz sa istom drugom stanicom (ili u jedini voz maila).
        /// </summary>
        internal static List<VozListova> Vozovi(List<ListPosla> listovi)
        {
            var vozovi = new List<VozListova>();
            foreach (var l in listovi.Where(x => x.Stanica1.HasValue && x.Stanica3.HasValue))
            {
                var v = vozovi.FirstOrDefault(x => x.Listovi[0].Stanica1 == l.Stanica1 && x.Listovi[0].Stanica3 == l.Stanica3);
                if (v == null) vozovi.Add(v = new VozListova());
                v.Listovi.Add(l);
            }
            foreach (var l in listovi.Where(x => !x.Stanica1.HasValue || !x.Stanica3.HasValue))
            {
                var v = vozovi.FirstOrDefault(x => (l.Stanica1.HasValue && x.Listovi[0].Stanica1 == l.Stanica1) || (l.Stanica3.HasValue && x.Listovi[0].Stanica3 == l.Stanica3))
                        ?? (vozovi.Count == 1 && !l.Stanica1.HasValue && !l.Stanica3.HasValue ? vozovi[0] : null);
                if (v == null) vozovi.Add(v = new VozListova());
                v.Listovi.Add(l);
            }
            return vozovi;
        }

        // ------------------------------------------------------------------------------------------- posao voza

        /// <summary>Posao kome pripada tovarni list sa ključem: ključ posla, dokument sa tim ključem, ili zapis TL u poslu.</summary>
        private int? PosaoPoKljucu(string kljuc)
        {
            if (string.IsNullOrEmpty(kljuc)) return null;
            var k = PodaciPosla.Skrati(kljuc, 200);
            var o = Skalar("SELECT ID FROM planiranje.tmpJobs WHERE KljucPosiljke = @k", "@k", k)
                    ?? Skalar("SELECT TOP 1 JobID FROM planiranje.tmpJobsPolje WHERE TeretnicaID IS NULL AND Kolona = @c ORDER BY ID DESC", "@c", KolonaTl(kljuc))
                    ?? Skalar("SELECT TOP 1 JobID FROM planiranje.tmpDokument WHERE KljucPosiljke = @k AND JobID IS NOT NULL ORDER BY ID DESC", "@k", k);
            return o == null ? (int?)null : Convert.ToInt32(o);
        }

        /// <summary>Zapisi TL posla.</summary>
        private Dictionary<string, ZapisTl> ZapisiTl(int jobId)
        {
            var d = new Dictionary<string, ZapisTl>(StringComparer.Ordinal);
            using (var cmd = Komanda("SELECT Kolona, TekstIzDokumenta FROM planiranje.tmpJobsPolje WHERE JobID = @j AND TeretnicaID IS NULL AND Kolona LIKE 'TL:%'", "@j", jobId))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                    try { var z = JsonConvert.DeserializeObject<ZapisTl>(r.IsDBNull(1) ? "" : r.GetString(1)); if (z != null) d[r.GetString(0)] = z; } catch { }
            return d;
        }

        /// <summary>Grupe posla iz zapisa TL: grupa 1 = natovarena kola sa najviše kola (ako ih nema – prazna), pa ostale po broju kola.</summary>
        internal static List<List<ZapisTl>> Grupe(IEnumerable<ZapisTl> zapisi)
        {
            var g = zapisi.GroupBy(z => z.Grupa).Select(x => x.ToList()).ToList();
            var prva = g.Where(x => !x[0].Prazna).OrderByDescending(x => x.Sum(z => z.BrojKola)).FirstOrDefault() ?? g.OrderByDescending(x => x.Sum(z => z.BrojKola)).FirstOrDefault();
            if (prva == null) return new List<List<ZapisTl>>();
            return new[] { prva }.Concat(g.Where(x => x != prva).OrderByDescending(x => x[0].Prazna).ThenByDescending(x => x.Sum(z => z.BrojKola))).ToList();
        }

        /// <summary>Vrednosti grupe n (1 ili 2) iz zapisa TL: NHM, broj kola, mase (zbir), serija, OM/RID.</summary>
        private void VrednostiGrupe(PodaciPosla p, int n, List<ZapisTl> g, int? dokId)
        {
            var s = n.ToString();
            var opis = string.Join("; ", g.Select(z => z.Opis(n)));
            var nhmIds = g.Select(z => z.NhmId).Distinct().ToList();
            var nhm = nhmIds.Count == 1 && nhmIds[0].HasValue ? new Pogodak { Id = nhmIds[0], Pouzdanost = 95, Tekst = g[0].Nhm4 }
                      : pk.Nhm(g[0].Prazna && g.All(z => z.Nhm4.Length == 0) ? "9922" : g[0].Grupa);
            if (g.Any(z => z.OstaliNhm.Length > 0)) nhm.Napomena = (nhm.Napomena + "; ostali NHM: " + string.Join(", ", g.Where(z => z.OstaliNhm.Length > 0).Select(z => z.OstaliNhm))).Trim(' ', ';');
            p.Postavi("NHM" + s, nhm, "tovarni listovi", "");
            var kola = g.SelectMany(z => z.Kola).Distinct().Count();
            var bk = g.All(z => z.Kola.Count > 0) ? kola : g.Sum(z => z.BrojKola);
            p.Postavi("BrojKola" + s, bk, opis, "tovarni listovi", bk > 0 ? 80 : 0);
            double? Zbir(Func<ZapisTl, double?> f) => g.Any(z => f(z).HasValue) ? g.Sum(z => f(z) ?? 0) : (double?)null;
            var ne = Zbir(z => z.Neto); var ta = Zbir(z => z.Tara); var br = Zbir(z => z.Bruto);
            decimal Tn(double? kg) => Math.Round((decimal)(kg ?? 0) / 1000m, 3);
            p.Postavi("Neto" + s, Tn(ne), opis, "r25/r21", ne.HasValue ? 90 : 0);
            p.Postavi("Tara" + s, Tn(ta), opis, "r25/r21", ta.HasValue ? 90 : 0);
            p.Postavi("Bruto" + s, Tn(br), opis, "r25/r21", br.HasValue ? 90 : 0);
            var ser = BrojKolaUic.NajcescaOznaka(g.SelectMany(z => z.Kola).Distinct().Select(k => (k, "")));
            p.Postavi("SerijaKola" + s, ser.Length == 0 ? null : ser, "iz brojeva kola (UIC 438)", "r18", ser.Length > 0 ? 80 : 0);
            var om = string.Join(", ", g.Select(z => z.Om).Where(x => x.Length > 0).Distinct());
            p.Postavi("OMBroj" + s, om.Length == 0 ? null : PodaciPosla.Skrati(om, 100), om, "r21", om.Length > 0 ? 80 : 0);
            var rid = g.Select(z => z.Rid).FirstOrDefault(x => x.Length > 0);
            p.Postavi("RIDBroj" + s, rid, rid ?? "", "r7", rid != null ? 80 : 0);
            p.V["Duzina" + s] = null;
        }

        /// <summary>Posao sa praznim kolima u suprotnom smeru (30 dana, ≥ 80 % istih kola) – povratak praznih kola (tačka 4.3).</summary>
        private int? PovratakPraznih(VozListova voz)
        {
            var kola = voz.Listovi.SelectMany(x => x.P.Kola).Distinct().ToList();
            if (kola.Count == 0) return null;
            var brojevi = kola.Select(BrojKolaUic.ZaTeretnicu).ToList();
            var imena = new List<string>(); var arg = new List<object>();
            for (int i = 0; i < brojevi.Count && i < 200; i++) { imena.Add("@k" + i); arg.Add("@k" + i); arg.Add(brojevi[i]); }
            // kola iz teretnica posla i iz zapisa TL (brojevi kola u JSON-u)
            var kandidati = new Dictionary<int, HashSet<string>>();
            using (var cmd = Komanda(@"SELECT t.JobID, s.BrojKola FROM planiranje.tmpTeretnicaStavke s JOIN planiranje.tmpTeretnica t ON t.ID = s.BrojTeretnice
WHERE t.JobID IS NOT NULL AND s.BrojKola IN (" + string.Join(", ", imena) + ") AND t.Kreirano >= DATEADD(day, -30, SYSDATETIME())", arg.ToArray()))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) { var j = r.GetInt32(0); if (!kandidati.ContainsKey(j)) kandidati[j] = new HashSet<string>(); kandidati[j].Add(BrojKolaUic.Cifre(r.GetString(1))); }
            using (var cmd = Komanda(@"SELECT p.JobID, p.TekstIzDokumenta FROM planiranje.tmpJobsPolje p JOIN planiranje.tmpJobs j ON j.ID = p.JobID
WHERE p.TeretnicaID IS NULL AND p.Kolona LIKE 'TL:%' AND j.Kreirano >= DATEADD(day, -30, SYSDATETIME())"))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                {
                    ZapisTl z; try { z = JsonConvert.DeserializeObject<ZapisTl>(r.IsDBNull(1) ? "" : r.GetString(1)); } catch { continue; }
                    if (z == null || z.Prazna) continue;
                    var j = r.GetInt32(0); if (!kandidati.ContainsKey(j)) kandidati[j] = new HashSet<string>();
                    foreach (var k in z.Kola.Intersect(kola)) kandidati[j].Add(k);
                }
            var l0 = voz.Listovi[0];
            foreach (var kv in kandidati.Where(x => x.Value.Count >= 0.8 * kola.Count).OrderByDescending(x => x.Value.Count).ThenByDescending(x => x.Key))
            {
                var j = Red("SELECT OtpravnaID, UputnaID, Stanica1ID, Stanica3ID FROM planiranje.tmpJobs WHERE ID = @id", "@id", kv.Key);
                bool Isto(object a, int? b) => a is int x && b.HasValue && x == b.Value;
                var suprotno = Isto(j["UputnaID"], l0.Otpravna) || Isto(j["OtpravnaID"], l0.Uputna) || Isto(j["Stanica3ID"], l0.Stanica1) || Isto(j["Stanica1ID"], l0.Stanica3);
                if (suprotno) return kv.Key;
            }
            return null;
        }

        /// <summary>Posao iz dosijea pošiljke sa istom deonicom KP (Stanica1 i Stanica3) – drugi tovarni list istog voza iz iste prepiske.</summary>
        private int? PosaoIzDosijeaIsteDeonice(MailZaUpis m, VozListova voz)
        {
            var j = PosaoIzDosijea(m);
            if (j == null) return null;
            var red = Red("SELECT Stanica1ID, Stanica3ID, Referenca FROM planiranje.tmpJobs WHERE ID = @id", "@id", j.Value);
            var l0 = voz.Listovi[0];
            if (!(red["Stanica1ID"] is int s1 && red["Stanica3ID"] is int s3 && l0.Stanica1 == s1 && l0.Stanica3 == s3)) return null;
            // v2.9.1 (posle ponovne obrade 17.11.): ista prepiska i deonica nisu dovoljni – „Budamar Coal 16“ i „Coal 17“ su dva voza iste
            // relacije u istoj niti. Isti voz samo kada se poklapa referenca (r8), kola (≥ 80 %) ili su mase iste (kopija lista)
            string N(object x) => Regex.Replace(Slicnost.Osnovno(x as string ?? ""), @"\s", "");
            var refPosla = N(red["Referenca"]);
            var refVoza = voz.Listovi.Select(x => N(x.P.V.TryGetValue("Referenca", out var v) ? v : null)).FirstOrDefault(x => x.Length > 0) ?? "";
            if (refPosla.Length > 0 && refVoza.Length > 0) return refPosla == refVoza ? j : null;
            var tl = ZapisiTl(j.Value).Values.ToList();
            foreach (var l in voz.Listovi) { var z = ZapisTl.Iz(l); if (tl.Any(t => IstiZapis(t, z))) return j; }
            return null;
        }

        /// <summary>Dva zapisa TL su isti tovarni list (kopija iz drugog maila sa drugim ključem): ista kola (≥ 80 %) ili iste mase (bar dve, nijedna različita).</summary>
        internal static bool IstiZapis(ZapisTl a, ZapisTl b)
        {
            if (a.Kola.Count > 0 && b.Kola.Count > 0) return a.Kola.Intersect(b.Kola).Count() >= 0.8 * Math.Min(a.Kola.Count, b.Kola.Count);
            int iste = 0, razlicite = 0;
            foreach (var (x, y) in new[] { (a.Neto, b.Neto), (a.Tara, b.Tara), (a.Bruto, b.Bruto) })
                if (x > 0 && y > 0) { if (Math.Abs(x.Value - y.Value) <= 0.005 * Math.Max(x.Value, y.Value)) iste++; else razlicite++; }
            return iste >= 2 && razlicite == 0;
        }

        private static readonly string[] KoloneZaglavlja =
        {
            "PlatilacID", "OrganizatorID", "TipPrevozaID", "OtpravnaID", "UputnaID", "Status", "PreuzimaOdID", "PredajeOperateruID", "PredajePartneruID",
            "Stanica1ID", "Stanica2ID", "Stanica3ID", "ETD", "KljucPosiljke", "Referenca", "BrojPonude", "KPTovarniList",
        };

        /// <summary>
        /// v2.9.1 (tačka 4): voz (tovarni listovi istog maila sa istom deonicom KP) → jedan posao. Postojeći posao: ključ bilo kog lista,
        /// isti voz i relacija, dosije sa istom deonicom, povratak praznih kola. Zapisi TL svih listova posla daju grupe (1 = natovarena,
        /// 2 = prazna / druga roba; više od dve → Napomena); zaglavlje iz glavnog lista grupe 1 (samo kada je taj list u ovom mailu).
        /// </summary>
        private int UpisiVoz(VozListova voz, MailZaUpis m, IshodUpisa ishod)
        {
            int? id = null; var veza = "";
            foreach (var l in voz.Listovi) { id = PosaoPoKljucu(l.P.KljucPosiljke); if (id != null) { veza = "ključ " + l.P.KljucPosiljke; break; } }
            var glavni0 = voz.Listovi.OrderByDescending(x => !x.P.Prazna).ThenByDescending(x => x.BrojKola).First();
            if (id == null && glavni0.P.BrojVoza.Length > 0 && glavni0.Otpravna is int o0 && glavni0.Uputna is int u0)
            {
                var isti = NedavniPoslovi().Where(j => ExcelCitac.BrojVoza(j.Referenca) == glavni0.P.BrojVoza && j.Otpravna == o0 && j.Uputna == u0).OrderByDescending(j => j.Id).FirstOrDefault();
                if (isti != null) { id = isti.Id; veza = "isti voz " + glavni0.P.BrojVoza + " i relacija"; }
            }
            if (id == null && PosaoIzDosijeaIsteDeonice(m, voz) is int jd) { id = jd; veza = "isti dosije i deonica KP"; }
            var povratak = false;
            if (id == null && voz.SvaPrazna && PovratakPraznih(voz) is int jp) { id = jp; povratak = true; veza = "povratak praznih kola uz posao " + jp; }
            if (veza.Length > 0 && !veza.StartsWith("ključ", StringComparison.Ordinal)) ishod.Napomene.Add(voz.Listovi.Count + " tovarnih listova → posao " + id + " (" + veza + ")");

            // zapisi TL: postojeći + listovi ovog maila
            var tl = id.HasValue ? ZapisiTl(id.Value) : new Dictionary<string, ZapisTl>(StringComparer.Ordinal);
            var noviKljucevi = new HashSet<string>();
            foreach (var l in voz.Listovi)
            {
                var z = ZapisTl.Iz(l); z.Povratak = povratak;
                // kopija istog lista iz ranijeg maila sa drugim ključem (sken „ref:…“ / r62) zamenjuje raniji zapis – mase se ne sabiraju dvaput
                foreach (var stari in tl.Where(kv => kv.Key != KolonaTl(z.Kljuc) && !noviKljucevi.Contains(kv.Key) && IstiZapis(kv.Value, z)).Select(kv => kv.Key).ToList())
                {
                    tl.Remove(stari);
                    if (id.HasValue) Izvrsi("DELETE FROM planiranje.tmpJobsPolje WHERE JobID = @j AND TeretnicaID IS NULL AND Kolona = @k", "@j", id.Value, "@k", stari);
                    ishod.Napomene.Add("tovarni list " + l.Oznaka + " je kopija ranijeg (" + stari + ") – zapis zamenjen");
                }
                tl[KolonaTl(z.Kljuc)] = z;
                noviKljucevi.Add(KolonaTl(z.Kljuc));
            }
            var kljuceviTl = new HashSet<string>(voz.Listovi.Select(x => x.KljucTl));
            var grupe = Grupe(tl.Values);
            var zbir = new PodaciPosla();
            // zaglavlje: glavni list grupe 1 (najviše kola, pa najviše kopija) – samo ako je u ovom mailu i posao nije povratak praznih
            var kljuceviG1 = new HashSet<string>(grupe.Count > 0 ? grupe[0].Select(z => z.Kljuc) : new string[0]);
            var glavni = voz.Listovi.Where(x => kljuceviG1.Contains(x.KljucTl)).OrderByDescending(x => x.BrojKola).ThenByDescending(x => x.Kopija).FirstOrDefault();
            if (glavni != null && !povratak)
            {
                foreach (var k in KoloneZaglavlja.Where(glavni.P.V.ContainsKey)) zbir.V[k] = glavni.P.V[k];
                foreach (var po in glavni.P.Poreklo.Where(x => KoloneZaglavlja.Contains(x.Kolona))) zbir.Poreklo.Add(po);
                // Stanica2 iz bilo kog lista voza
                if (zbir.V["Stanica2ID"] == null && voz.Listovi.Select(x => x.P.Poreklo.FirstOrDefault(po => po.Kolona == "Stanica2ID" && po.Vrednost != null)).FirstOrDefault(x => x != null) is PoljePorekla s2)
                { zbir.V["Stanica2ID"] = s2.Vrednost; zbir.Poreklo.Add(s2); }
                zbir.KljucPosiljke = glavni.P.KljucPosiljke;
                zbir.BrojVoza = glavni.P.BrojVoza;
            }
            if (!povratak)
            {
                var greske = voz.Listovi.SelectMany(x => x.P.Poreklo.Where(po => po.Kolona == "ImaGreske" && po.Vrednost is bool b && b).Select(po => po.Tekst)).Distinct().ToList();
                zbir.Postavi("ImaGreske", greske.Count > 0, greske.Count == 0 ? "bez grešaka" : PodaciPosla.Skrati(string.Join("; ", greske), 900), "provera");
            }
            for (int i = 0; i < 2; i++)
            {
                if (povratak && i == 0) continue;   // povratak praznih: menja se samo grupa 2
                if (i < grupe.Count) VrednostiGrupe(zbir, i + 1, grupe[i], null);
                else if (i == 1) foreach (var k in new[] { "NHM2", "Bruto2", "BrojKola2", "Duzina2", "Neto2", "RIDBroj2", "OMBroj2", "SerijaKola2" }) zbir.V[k] = null;
            }
            zbir.Kola = voz.Listovi.SelectMany(x => x.P.Kola).Distinct().ToList();
            if (grupe.Count > 2) zbir.Napomene.Add("Više od dve grupe robe u vozu – ostale grupe: " + string.Join("; ", grupe.Skip(2).Select(g => g.Sum(z => z.BrojKola) + " kola NHM " + g[0].Grupa)));
            var dokId = glavni?.DokId ?? voz.Listovi.Select(x => x.DokId).FirstOrDefault(x => x.HasValue);
            if (id == null && zbir.KljucPosiljke == null) { zbir.KljucPosiljke = glavni0.P.KljucPosiljke; zbir.V["KljucPosiljke"] = PodaciPosla.Skrati(glavni0.P.KljucPosiljke, 200); }
            var jid = UpisiPosao(zbir, id, dokId, m, povratak ? "POVRATAK_PRAZNIH" : "NOVA_VERZIJA_CIM", ishod);
            // zapisi TL (poreklo grupa) i istorija povratka
            foreach (var l in voz.Listovi)
            {
                var z = tl[KolonaTl(l.KljucTl)];
                var gi = grupe.FindIndex(g => g.Contains(z)) + 1;
                UpisiPoreklo(jid, null, new PoljePorekla { Kolona = KolonaTl(z.Kljuc), Vrednost = z.Opis(gi > 0 ? gi : 3), Tekst = z.Json(), Rubrika = "tovarni list", Pouzdanost = 100, Kandidati = l.Oznaka + (l.Kopija > 1 ? " (" + l.Kopija + " kopije)" : "") }, l.DokId);
            }
            if (povratak) Istorija(jid, null, null, null, voz.Listovi.Sum(x => x.BrojKola) + " praznih kola", dokId, "POVRATAK_PRAZNIH", "povratak praznih kola uz posao " + jid + " (grupa 2)");
            if (grupe.Count > 2) Istorija(jid, null, null, null, null, dokId, "NOVA_VERZIJA_CIM", PodaciPosla.Skrati(zbir.Napomene.Last(), 400));
            return jid;
        }

        // ------------------------------------------------------------------------------------------- CIT 7 (5.6)

        /// <summary>
        /// v2.9.1 (5.6): CIT 7 povezan sa poslom (broj pošiljke r62 / ključ lista, pa brojevi kola, pa dosije) ažurira posao: „Box 25: …“ →
        /// neto (spisak kola ima prednost – razlika ide u napomenu), „wagon … detached“ → stavka Otkacena = 1 (ako su kola na spisku),
        /// Box 4 / 10 / 12 → primalac / uputna (i PredajePartneru kada je KP poslednji). Svaka izmena u istoriju (izvor CIT 7, datum) i
        /// jedan red u Napomeni.
        /// </summary>
        private void PrimeniCit7(Cit7Rezultat c7, int? dokId, MailZaUpis m, IshodUpisa ishod)
        {
            if (c7?.Z == null) return;
            var z = c7.Z;
            int? jobId = null;
            if (c7.Veza?.Lista?.Cim != null) jobId = PosaoPoKljucu(Ponavljanja.KljucLista(c7.Veza.Lista.Cim));
            if (jobId == null)
            {
                var r62 = R62Kljuc(new Identifikacija { Zemlja = z.Zemlja, Stanica = z.Stanica, Preduzece = z.Preduzece, Broj = z.Broj });
                if (r62 != null) jobId = PosaoPoKljucu(r62);
            }
            var kola = z.BrojeviKola.Concat(c7.Veza?.Kola ?? new List<string>()).Distinct().ToList();
            if (jobId == null && kola.Count > 0 && TeretnicaIstihKola(kola, true) is var tk && tk?.jobId != null) jobId = tk.Value.jobId;
            if (jobId == null && kola.Count > 0)
            {
                var po = Skalar("SELECT TOP 1 t.JobID FROM planiranje.tmpTeretnicaStavke s JOIN planiranje.tmpTeretnica t ON t.ID = s.BrojTeretnice WHERE t.JobID IS NOT NULL AND s.BrojKola IN ("
                                + string.Join(", ", kola.Select((k, i) => "@k" + i)) + ") ORDER BY t.ID DESC", kola.SelectMany((k, i) => new object[] { "@k" + i, BrojKolaUic.ZaTeretnicu(k) }).ToArray());
                if (po != null) jobId = Convert.ToInt32(po);
            }
            if (jobId == null) jobId = PosaoIzDosijea(m);
            if (jobId == null) { ishod.Napomene.Add("CIT 7 (" + c7.Fajl + "): posao nije pronađen – samo tmpDokument"); return; }
            var j = jobId.Value;
            if (dokId.HasValue) Izvrsi("UPDATE planiranje.tmpDokument SET JobID = @j WHERE ID = @d", "@j", j, "@d", dokId.Value);
            var datum = z.Datum.Length > 0 ? z.Datum : m.Primljeno.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            var izvor = "CIT 7 (" + System.IO.Path.GetFileName(c7.Fajl) + ", " + datum + ")";
            var stari = Red("SELECT * FROM planiranje.tmpJobs WHERE ID = @id", "@id", j);
            var rucno = Kolona("SELECT Kolona FROM planiranje.tmpJobsPolje WHERE JobID = @id AND TeretnicaID IS NULL AND RucnoIzmenjeno = 1", "@id", j);
            var p = new PodaciPosla();
            var redovi = new List<string>();
            var sr = CultureInfo.GetCultureInfo("sr-Latn-RS");

            // Box 25: masa robe → neto (spisak kola ima prednost)
            foreach (var iz in z.Izmene.Where(x => x.Rubrika == "25"))
            {
                var vr = Regex.Match(iz.Vrednost, @"\d[\d.,\s]*\d(?:\s*(?:kg|t|to)\b)?", RegexOptions.IgnoreCase).Value.Trim();
                var kg = PosaoIzDokumenata.Kg(vr);
                if (kg == null) continue;
                iz.Vrednost = vr;
                var t = Math.Round((decimal)kg.Value / 1000m, 3);
                var izSpiska = Skalar("SELECT Rubrika FROM planiranje.tmpJobsPolje WHERE JobID = @j AND TeretnicaID IS NULL AND Kolona = 'Neto1'", "@j", j) as string == "spisak";
                if (izSpiska)
                {
                    var tekst = "Box 25: " + iz.Vrednost + " (" + t.ToString("N3", sr) + " t) – neto ostaje sa spiska kola (" + (stari["Neto1"] is decimal sn ? sn.ToString("N3", sr) : "–") + " t)";
                    Istorija(j, null, "Neto1", Tekst(stari["Neto1"]), Tekst(t), dokId, "CIT7", PodaciPosla.Skrati(izvor + ": " + tekst, 400));
                    redovi.Add(tekst);
                }
                else { p.Postavi("Neto1", t, izvor + ": Box 25: " + iz.Vrednost, "CIT 7 r25", 90); redovi.Add("Box 25: " + iz.Vrednost); }
            }
            // otkačena kola („wagon 358049566241 detached“)
            foreach (Match mm in Regex.Matches(z.Napomene ?? "", @"(?i)(?:wagon|kola|wagen|vagon)?\s*((?:\d[\s\-]?){12})\s*(?:is\s+|je\s+|wurde\s+)?(detached|otka[čc]en\w*|abgeh[äa]ngt|ausgesetzt|isklju[čc]en\w*|uncoupled)"))
            {
                var broj = BrojKolaUic.Cifre(mm.Groups[1].Value);
                if (broj.Length != 12) continue;
                if (!BrojKolaUic.KontrolnaIspravna(broj))
                {
                    Istorija(j, null, "Otkacena", null, null, dokId, "CIT7", PodaciPosla.Skrati(izvor + ": broj kola „" + broj + "“ nema ispravnu kontrolnu cifru – ništa se ne otkačuje", 400));
                    redovi.Add("kola " + broj + " „detached“ – broj nije ispravan, ništa se ne otkačuje");
                    continue;
                }
                var st = Skalar("SELECT TOP 1 s.ID FROM planiranje.tmpTeretnicaStavke s JOIN planiranje.tmpTeretnica t ON t.ID = s.BrojTeretnice WHERE t.JobID = @j AND s.BrojKola = @b ORDER BY s.ID DESC", "@j", j, "@b", BrojKolaUic.ZaTeretnicu(broj));
                // CIT 7 sa skena (OCR): broj kola može biti pogrešno pročitan (HER-SAD 80: „624-1“ pročitano kao „824-1“, kola sa spiska) –
                // Otkacena se ne postavlja, samo napomena za proveru
                var ocrCit7 = m.Provera?.Dokumenti.FirstOrDefault(d => d.Tip == TipDokumenta.Cit7 && d.Fajl == c7.Fajl)?.Izvor == "ocr";
                if (st != null && ocrCit7)
                {
                    Istorija(j, null, "Otkacena", null, null, dokId, "CIT7", PodaciPosla.Skrati(izvor + ": kola " + BrojKolaUic.ZaTeretnicu(broj) + " „detached“ (pročitano OCR-om sa skena) – Otkacena nije postavljena, proveriti", 400));
                    redovi.Add("kola " + BrojKolaUic.ZaTeretnicu(broj) + " „detached“ (sken, OCR) – proveriti, nije otkačeno");
                }
                else if (st != null)
                {
                    Izvrsi("UPDATE planiranje.tmpTeretnicaStavke SET Otkacena = 1 WHERE ID = @s", "@s", Convert.ToInt32(st));
                    Istorija(j, null, "Otkacena", "0", "1", dokId, "CIT7", PodaciPosla.Skrati(izvor + ": kola " + BrojKolaUic.ZaTeretnicu(broj) + " otkačena", 400));
                    redovi.Add("kola " + BrojKolaUic.ZaTeretnicu(broj) + " otkačena");
                }
                else
                {
                    Istorija(j, null, "Otkacena", null, null, dokId, "CIT7", PodaciPosla.Skrati(izvor + ": kola " + BrojKolaUic.ZaTeretnicu(broj) + " nisu na spisku posla – ništa se ne otkačuje", 400));
                    redovi.Add("kola " + BrojKolaUic.ZaTeretnicu(broj) + " „detached“ – nisu na spisku, ništa se ne otkačuje");
                }
            }
            // Box 10 / 12 → uputna; Box 4 → primalac (PredajePartneru kada je KP poslednji – voz se završava u Srbiji)
            var upIzmena = z.Izmene.Where(x => x.Rubrika == "10" || x.Rubrika == "12").Select(x => x.Vrednost).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            bool kraj = stari["UputnaID"] is int u0 && pk.DrzavaStanice(u0) == "SRB";
            if (upIzmena.Count > 0)
            {
                var tekst = string.Join("\n", upIzmena);
                var sifra = Regex.Match(tekst, @"\d{2}\s?\d{5}-?\d").Value;
                var st = pk.Stanica(tekst, sifra.Length > 0 ? sifra : null);
                p.Postavi("UputnaID", st, "CIT 7 r10/r12", c7.Fajl);
                kraj = st.Id.HasValue && pk.DrzavaStanice(st.Id.Value) == "SRB";
                if (kraj) p.Postavi("Stanica3ID", st, "CIT 7 r10/r12 (voz se završava u Srbiji)", c7.Fajl);
                redovi.Add("Box 10/12: " + Regex.Replace(tekst, @"\s+", " "));
            }
            var prim = z.Izmene.Where(x => x.Rubrika == "4").Select(x => x.Vrednost).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            if (prim != null)
            {
                if (kraj) p.Postavi("PredajePartneruID", pk.Partner(prim.Split('\n', ',')[0], null, null, "primalac"), "CIT 7 r4 (primalac)", c7.Fajl);
                redovi.Add("Box 4: " + Regex.Replace(prim, @"\s+", " "));
            }
            var napomena = redovi.Count == 0 ? null : SpojiNapomenu("[" + izvor + "] " + string.Join("; ", redovi), stari["Napomena"] as string);
            if (napomena != null && !string.IsNullOrEmpty(stari["Napomena"] as string) && ((string)stari["Napomena"]).Contains("[" + izvor + "]")) napomena = null;
            var promene = Uporedi(stari, p.V, p.V.Keys.ToList());
            if (promene.Count > 0) PrimeniPromene(j, stari, p, promene, rucno, dokId, "CIT7", napomena);
            else if (napomena != null) Izvrsi("UPDATE planiranje.tmpJobs SET Napomena = @n WHERE ID = @j", "@n", napomena, "@j", j);
            if (redovi.Count > 0 || promene.Count > 0) ishod.IzmenjenihPoslova++;
            ishod.Napomene.Add("CIT 7 → posao " + j + (redovi.Count > 0 ? ": " + string.Join("; ", redovi) : " (bez izmena)"));
        }

        // ------------------------------------------------------------------------------------------- ponovljeni mail (5.7)

        /// <summary>
        /// v2.9.1 (5.7): ponovljeni mail (isti prilozi, odgovor se ne šalje): prilozi se beleže u tmpDokument (novi red ili BrojPrijema + 1 i
        /// PoslednjePrimljeno), a tekst maila ide u Napomenu posla sa kojim su ti prilozi povezani (tačno jednom).
        /// </summary>
        private void UpisiPonovljeni(MailZaUpis m, IshodUpisa ishod)
        {
            var fajlovi = new List<Prilog>();
            foreach (var (ime, b) in m.Prilozi) fajlovi.AddRange(Raspakivanje.Raspakuj(ime, b, new List<string>()));
            var ids = new List<int>();
            foreach (var f in fajlovi.GroupBy(x => x.Hes).Select(g => g.First()))
            {
                if (Raspakivanje.Ukras(f) != null) continue;
                ids.Add(UpisiDokument(m, f, new List<Model.Dokument>()));
                ishod.Dokumenata++;
            }
            if (ids.Count == 0) return;
            var poslovi = Kolona("SELECT DISTINCT JobID FROM planiranje.tmpDokument WHERE JobID IS NOT NULL AND ID IN (" + string.Join(", ", ids) + ")").Select(int.Parse).ToList();
            foreach (var j in poslovi)
            {
                var stara = Skalar("SELECT Napomena FROM planiranje.tmpJobs WHERE ID = @j", "@j", j) as string;
                var nap = NovaNapomena(j, m, BlokNapomene(m) + "\n[agent] ponovljen mail – isti prilozi, odgovor nije poslat", stara);
                if (nap == null) continue;
                Izvrsi("UPDATE planiranje.tmpJobs SET Napomena = @n, Izmenio = @izm, Izmenjeno = SYSDATETIME() WHERE ID = @j", "@n", nap, "@izm", Korisnik, "@j", j);
                Istorija(j, null, "Napomena", null, ZaglavljeNapomene(m), ids[0], "PONOVLJEN_MAIL", "ponovljen mail (isti prilozi) – tekst maila dodat u Napomenu");
                ishod.IstihPoslova++;
            }
            ishod.Napomene.Add("ponovljen mail – prijem zabeležen (" + ids.Count + " priloga" + (poslovi.Count > 0 ? ", posao " + string.Join(", ", poslovi) : "") + ")");
        }

        // ------------------------------------------------------------------------------------------- teretnica → posao (po grupama)

        private static readonly string[] KoloneGrupe = { "NHM", "Bruto", "BrojKola", "Duzina", "Neto", "Tara", "OMBroj", "SerijaKola" };

        /// <summary>
        /// Posle povezivanja: posao iz stavki teretnice, razlog SPISAK_KOLA. v2.9.1: teretnica pripada grupi posla čija su kola na njoj
        /// (grupa 1 = natovarena, 2 = prazna / druga roba); vrednosti spiska menjaju samo tu grupu (prazan vagon iz posebnog CUV-a ostaje u
        /// grupi 2). Razlika neto r25 ↔ spisak &gt; 1 % → napomena u poreklu i istoriji. Granični prelaz sa spiska (r69 „Prelazni put“, CER
        /// „Érkezési állomás“) kada posao nema pouzdan prelaz.
        /// </summary>
        private void PosaoIzTeretnice(int jobId, int tid, int? dokId, string razlog, IshodUpisa ishod, SpisakKola izvorni = null)
        {
            var s = new SpisakKola();
            var izvorDuzine = Skalar("SELECT Rubrika FROM planiranje.tmpJobsPolje WHERE TeretnicaID = @t AND Kolona = 'Duzina'", "@t", tid) as string == "istorija" ? "istorija" : "spisak";
            using (var cmd = Komanda("SELECT BrojKola, ISNULL(Serija, ''), ISNULL(BrojOsovina, 0), Duzina, Tara, Neto, ISNULL(VRNP, ''), ISNULL(RID, '') FROM planiranje.tmpTeretnicaStavke WHERE BrojTeretnice = @t ORDER BY RB", "@t", tid))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                {
                    double? T(int i) => r.IsDBNull(i) ? (double?)null : Convert.ToDouble(r.GetValue(i)) * 1000;   // stavke su u t, spisak u kg
                    var nhm = r.GetString(6);
                    s.Kola.Add(new KolaNaSpisku
                    {
                        Broj = BrojKolaUic.Cifre(r.GetString(0)), Serija = r.GetString(1).Length > 1 ? r.GetString(1) : "", Nhm = nhm, KemlerUn = r.GetString(7),
                        TaraKg = T(4) > 0 ? T(4) : null, NetoKg = T(5), DuzinaM = r.IsDBNull(3) ? (double?)null : Convert.ToDouble(r.GetValue(3)), DuzinaIzvor = r.IsDBNull(3) ? "" : izvorDuzine,
                        Prazna = Regex.IsMatch(nhm, "^992[12]"),
                    });
                }
            if (s.Kola.Count == 0) return;
            // neto 0 u stavci je „nije navedeno“ kada nijedna stavka nema neto (spisak samo sa tarom)
            if (s.Kola.All(k => (k.NetoKg ?? 0) == 0) && !s.Kola.All(k => k.Prazna)) foreach (var k in s.Kola) k.NetoKg = null;
            if (izvorni != null) { s.UkupnoTaraKg = izvorni.UkupnoTaraKg; s.UkupnoNetoKg = izvorni.UkupnoNetoKg; s.UkupnoBrutoKg = izvorni.UkupnoBrutoKg; s.DuzinaVozaM = izvorni.DuzinaVozaM; s.PrelazniPut = izvorni.PrelazniPut; s.Prevoznici = izvorni.Prevoznici; s.NhmJedinica = izvorni.NhmJedinica; }
            var p = PosaoIzDokumenata.IzSpiska(s, pk, NhmPosla(jobId), "");
            var stari = Red("SELECT * FROM planiranje.tmpJobs WHERE ID = @id", "@id", jobId);
            var rucno = Kolona("SELECT Kolona FROM planiranje.tmpJobsPolje WHERE JobID = @id AND TeretnicaID IS NULL AND RucnoIzmenjeno = 1", "@id", jobId);

            // grupa teretnice: po kolima u zapisima TL posla; spisak samo praznih kola → grupa praznih
            var gr = Grupe(ZapisiTl(jobId).Values);
            var grupa = 1;
            if (p.Grupe.Count == 1 && gr.Count > 1)
            {
                var kola = new HashSet<string>(p.Grupe[0].kola);
                var preklop = gr.Select((g, i) => (i, n: g.SelectMany(z => z.Kola).Count(kola.Contains))).OrderByDescending(x => x.n).First();
                if (preklop.n > 0) grupa = preklop.i + 1;
                else if (p.Grupe[0].prazna && gr.FindIndex(g => g[0].Prazna) is int ip && ip >= 0) grupa = ip + 1;
                grupa = Math.Min(grupa, 2);
            }
            if (grupa == 2)
            {
                foreach (var k in KoloneGrupe) { p.V.Remove(k + "2"); if (p.V.TryGetValue(k + "1", out var v)) { p.V[k + "2"] = v; p.V.Remove(k + "1"); } }
                foreach (var po in p.Poreklo) if (po.Kolona.EndsWith("1")) po.Kolona = po.Kolona.Substring(0, po.Kolona.Length - 1) + "2";
            }
            else if (p.Grupe.Count == 1 && gr.Count > 1)
                foreach (var k in KoloneGrupe) { p.V.Remove(k + "2"); p.Poreklo.RemoveAll(x => x.Kolona == k + "2"); }
            Izvrsi("UPDATE planiranje.tmpTeretnica SET Grupa = @g WHERE ID = @t", "@g", (byte)grupa, "@t", tid);
            var g0 = grupa.ToString();

            // NHM sa spiska samo kada ga spisak ima; iste 4 cifre kao NHM posla ne menjaju podgrupu iz tovarnog lista
            if (!s.Kola.Any(k => k.Nhm.Length > 0)) { p.V.Remove("NHM1"); p.V.Remove("NHM2"); }
            string Nhm4Id(object id) => id is int i ? Poklapanje.Nhm4(sif.Nhm.FirstOrDefault(z => z.Id == i)?.Kod8) : "";
            foreach (var k in new[] { "NHM1", "NHM2" })
                if (p.V.TryGetValue(k, out var nv) && stari.TryGetValue(k, out var sv) && sv != null && Nhm4Id(nv) == Nhm4Id(sv)) p.V.Remove(k);
            // broj kola u tovarnom listu i na spisku: razlika u istoriju i poreklo
            var izCim = Skalar("SELECT Vrednost FROM planiranje.tmpJobsPolje WHERE JobID = @j AND TeretnicaID IS NULL AND Kolona = @k AND Rubrika IN ('r18/r21', 'tovarni listovi')", "@j", jobId, "@k", "BrojKola" + g0);
            var kolaCim = izCim != null && int.TryParse(Convert.ToString(izCim), out var bk) ? bk : 0;
            var kolaSpisak = s.Kola.Count;
            var nap = kolaCim > 0 && kolaCim != kolaSpisak ? "Broj kola u tovarnom listu (" + kolaCim + ") ne odgovara spisku kola (" + kolaSpisak + ")" : null;
            if (nap != null)
            {
                ishod.Napomene.Add(nap);
                var po = p.Poreklo.FirstOrDefault(x => x.Kolona == "BrojKola" + g0);
                if (po != null) po.Kandidati = nap;
            }
            // neto r25 ↔ spisak > 1 % (5.1): interna napomena u poreklu i istoriji
            var r25 = gr.Count >= grupa ? gr[grupa - 1].Where(z => z.Neto.HasValue).Sum(z => z.Neto.Value) : 0;
            string napMase = null;
            if (p.V.TryGetValue("Neto" + g0, out var nsp) && nsp is decimal ns && ns > 0 && r25 > 0 && Math.Abs((double)ns * 1000 - r25) > 0.01 * r25)
            {
                var sr = CultureInfo.GetCultureInfo("sr-Latn-RS");
                napMase = "Neto u tovarnom listu (r25) " + (r25 / 1000).ToString("N3", sr) + " t ≠ spisak kola " + ns.ToString("N3", sr) + " t – upisan spisak";
                ishod.Napomene.Add(napMase);
                var po = p.Poreklo.FirstOrDefault(x => x.Kolona == "Neto" + g0);
                if (po != null) po.Kandidati = napMase;
            }
            foreach (var x in p.Napomene) ishod.Napomene.Add(x);
            var promene = Uporedi(stari, p.V, PosaoIzDokumenata.KoloneSpiska);
            if (nap != null && !promene.Contains("BrojKola" + g0)) Istorija(jobId, tid, "BrojKola" + g0, kolaCim.ToString(), kolaSpisak.ToString(), dokId, razlog, nap);
            if (napMase != null && !promene.Contains("Neto" + g0)) Istorija(jobId, tid, "Neto" + g0, null, null, dokId, razlog, napMase);
            GranicaSaSpiska(jobId, s, dokId, stari);
            PrevoznikSaSpiska(jobId, s, dokId, stari);
            if (promene.Count == 0) return;
            foreach (var po in p.Poreklo) if (po.Rubrika != "istorija") po.Rubrika = "spisak";
            PrimeniPromene(jobId, stari, p, promene, rucno, dokId, razlog, null);
            foreach (var (kol, tekst) in new[] { ("BrojKola" + g0, nap), ("Neto" + g0, napMase) })
                if (tekst != null && promene.Contains(kol))
                    Izvrsi("UPDATE planiranje.tmpJobsIstorija SET Napomena = @n WHERE ID = (SELECT MAX(ID) FROM planiranje.tmpJobsIstorija WHERE JobID = @j AND Kolona = @k)", "@n", PodaciPosla.Skrati(tekst, 400), "@j", jobId, "@k", kol);
        }

        /// <summary>
        /// v2.9.1 (5.3): PreuzimaOd sa spiska voza („CER Hungary Zrt. UIC: 3085“) kada ga posao nema (KP nije u r57), a voz se završava u
        /// Srbiji – po nazivu (UIC sa spiska samo za potvrdu); pouzdanost najviše 60, napomena u poreklu.
        /// </summary>
        private void PrevoznikSaSpiska(int jobId, SpisakKola s, int? dokId, Dictionary<string, object> stari)
        {
            if (Environment.GetEnvironmentVariable("CIMPROVERA_DBG") == "1") Console.Error.WriteLine("[dbg prevoznik spiska] " + string.Join(" | ", s.Prevoznici) + " ; PreuzimaOd=" + stari["PreuzimaOdID"] + " ; Uputna=" + stari["UputnaID"]);
            if (s.Prevoznici.Count == 0 || stari["PreuzimaOdID"] != null) return;
            if (!(stari["UputnaID"] is int u) || pk.DrzavaStanice(u) != "SRB") return;
            foreach (var t in s.Prevoznici)
            {
                var naziv = Regex.Replace(t.Split('\n')[0], @"(?i)\bUIC\s*:?\s*\d{4}.*$", "").Trim(' ', ',', ';', '-');
                var g = pk.Partner(naziv, null, null, "prevoznik");
                if (!g.Id.HasValue) continue;
                g.Pouzdanost = Math.Min(60, g.Pouzdanost);
                g.Napomena = (g.Napomena + "; izvedeno: KP nije u r57 – prevoznik sa spiska voza").Trim(' ', ';');
                g.Tekst = PodaciPosla.Skrati(t, 400);
                var p = new PodaciPosla(); p.Postavi("PreuzimaOdID", g, "spisak voza (izvedeno)", "");
                PrimeniPromene(jobId, stari, p, new List<string> { "PreuzimaOdID" }, Kolona("SELECT Kolona FROM planiranje.tmpJobsPolje WHERE JobID = @id AND TeretnicaID IS NULL AND RucnoIzmenjeno = 1", "@id", jobId), dokId, "SPISAK_KOLA", null);
                return;
            }
        }

        /// <summary>Granični prelaz sa spiska (prelazni put) za Stanica1 (ulaz) ili Stanica3 (izlaz) kada posao nema pouzdan prelaz (pouzdanost ≤ 60).</summary>
        private void GranicaSaSpiska(int jobId, SpisakKola s, int? dokId, Dictionary<string, object> stari)
        {
            if (string.IsNullOrWhiteSpace(s.PrelazniPut)) return;
            var g = pk.Granica(s.PrelazniPut);
            if (!g.Id.HasValue) return;
            var polazi = stari["OtpravnaID"] is int o && pk.DrzavaStanice(o) == "SRB";
            var kol = polazi ? "Stanica3ID" : "Stanica1ID";
            var pouzd = Skalar("SELECT Pouzdanost FROM planiranje.tmpJobsPolje WHERE JobID = @j AND TeretnicaID IS NULL AND Kolona = @k", "@j", jobId, "@k", kol);
            if (stari[kol] != null && pouzd != null && Convert.ToInt32(pouzd) > 60) return;
            if (stari[kol] is int st && st == g.Id.Value) return;
            g.Tekst = "spisak kola – prelazni put: " + s.PrelazniPut;
            var p = new PodaciPosla(); p.Postavi(kol, g, "spisak (prelazni put)", "");
            PrimeniPromene(jobId, stari, p, new List<string> { kol }, Kolona("SELECT Kolona FROM planiranje.tmpJobsPolje WHERE JobID = @id AND TeretnicaID IS NULL AND RucnoIzmenjeno = 1", "@id", jobId), dokId, "SPISAK_KOLA", null);
        }
    }
}
