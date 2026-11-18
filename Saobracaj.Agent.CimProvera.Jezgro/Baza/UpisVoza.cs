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
        /// <summary>v3.0: referenca lista (r8, normalizovana) – draft i konačni CIM istog voza su ista lista (LDC8).</summary>
        [JsonProperty("r8")] public string Ref = "";

        public string Grupa => Prazna ? "9922" : Nhm4 ?? "";

        public static ZapisTl Iz(ListPosla l)
        {
            var p = l.P;
            string S(string k) => p.V.TryGetValue(k, out var v) ? v as string ?? "" : "";
            var z = new ZapisTl
            {
                Kljuc = l.KljucTl, Obrazac = p.Obrazac, Prazna = p.Prazna, Nhm4 = p.Nhm4, NhmId = p.V.TryGetValue("NHM1", out var n) ? n as int? : null, OstaliNhm = p.OstaliNhm,
                BrojKola = l.BrojKola, Neto = p.NetoKg, Tara = p.TaraKg, Bruto = p.BrutoKg, Om = S("OMBroj1"), Rid = S("RIDBroj1"), Kola = p.Kola.ToList(),
                Ref = UpisUBazu.NormOznaka(S("Referenca")),
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
            // v3.0.1 (3.1, amonijak UID 259: 001784 i 001792, bruto 955 450 / 954 500 kg spojeni → 12 kola umesto 24): bez pouzdane r62 kopije su
            // SAMO listovi sa istim masama (± 0,1 %), istim kolima i istim (ili kompatibilnim) brojem otpravljanja. Nesiguran r62 nikad nije razlog za spajanje.
            if (!KompatibilanBroj(a.P.BrojOtpravljanja, b.P.BrojOtpravljanja)) return false;
            var (iste, razlicite) = IsteMase(a.P, b.P, 0.001);
            if (razlicite > 0) return false;
            if (a.P.BrojVoza.Length > 0 && b.P.BrojVoza.Length > 0 && !string.Equals(UpisUBazu.NormOznaka(a.P.BrojVoza), UpisUBazu.NormOznaka(b.P.BrojVoza), StringComparison.Ordinal)) return false;
            // kola: kada ih oba lista imaju – ista kola (kopija skena: OCR može da pogreši pojedine brojeve – dovoljno je 80 % istih)
            if (a.P.Kola.Count > 0 && b.P.Kola.Count > 0) return a.P.Kola.Intersect(b.P.Kola).Count() >= 0.8 * Math.Max(a.P.Kola.Count, b.P.Kola.Count) && (iste >= 1 || NemaMasa(a.P, b.P));
            if (a.P.Kola.Count > 0 || b.P.Kola.Count > 0) return false;
            // bez kola: iste mase (bar dve od tri, nijedna različita), ista otpravna i uputna – kopija skena sa nečitljivom r62 (Šabac: 11132026Scan130104.pdf)
            if (iste >= 2 && a.Otpravna.HasValue && a.Otpravna == b.Otpravna && a.Uputna.HasValue && a.Uputna == b.Uputna) return true;
            // isti sadržaj (ključ „sadrzaj:“/„list:“) samo uz bar jednu istu masu ili kada nijedan nema masu
            if (ka == kb && ka.Length > 0 && !ka.StartsWith("ref:", StringComparison.Ordinal) && (iste >= 1 || NemaMasa(a.P, b.P))) return true;
            return iste >= 1 && a.P.Posiljalac.Length >= 3 && Slicnost.Ocena(a.P.Posiljalac, b.P.Posiljalac) >= 0.9 && a.P.Primalac.Length >= 3 && Slicnost.Ocena(a.P.Primalac, b.P.Primalac) >= 0.9
                   && a.Otpravna.HasValue && a.Otpravna == b.Otpravna && a.Uputna.HasValue && a.Uputna == b.Uputna;
        }

        /// <summary>Broj parova masa (neto, tara, bruto) koji su oba navedena i isti u granici tolerancije, i broj različitih.</summary>
        internal static (int iste, int razlicite) IsteMase(PodaciPosla a, PodaciPosla b, double tolerancija)
        {
            var iste = 0; var razlicite = 0;
            foreach (var (x, y) in new[] { (a.NetoKg, b.NetoKg), (a.TaraKg, b.TaraKg), (a.BrutoKg, b.BrutoKg) })
                if (x > 0 && y > 0) { if (Math.Abs(x.Value - y.Value) <= tolerancija * Math.Max(x.Value, y.Value)) iste++; else razlicite++; }
            return (iste, razlicite);
        }

        private static bool NemaMasa(PodaciPosla a, PodaciPosla b) =>
            !(a.NetoKg > 0 || a.TaraKg > 0 || a.BrutoKg > 0) && !(b.NetoKg > 0 || b.TaraKg > 0 || b.BrutoKg > 0);

        /// <summary>
        /// v3.0.1 (3.1): brojevi otpravljanja su kompatibilni kada jedan nije pročitan ili su isti; OCR sme da pogreši najviše jednu cifru
        /// (iste dužine). „001784“ i „001792“ nisu kompatibilni.
        /// </summary>
        internal static bool KompatibilanBroj(string a, string b)
        {
            var x = Regex.Replace(a ?? "", @"\D", ""); var y = Regex.Replace(b ?? "", @"\D", "");
            if (x.Length < 4 || y.Length < 4) return true;
            if (x == y) return true;
            if (x.Length != y.Length) return false;
            return x.Zip(y, (p, q) => p != q).Count(r => r) <= 1;
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
            // v3.0.1 (3.6, Eurolog UID 170: 268F, 269F, 270F, 272F → bila su 3 posla): različit broj voza ili referenca r8 nikad nisu isti voz,
            // ni kada je deonica ista
            bool Moze(VozListova v, ListPosla l) => v.Listovi.All(y => IstiVozIliNepoznat(y, l));
            foreach (var l in listovi.Where(x => x.Stanica1.HasValue && x.Stanica3.HasValue))
            {
                var v = vozovi.FirstOrDefault(x => x.Listovi[0].Stanica1 == l.Stanica1 && x.Listovi[0].Stanica3 == l.Stanica3 && Moze(x, l));
                if (v == null) vozovi.Add(v = new VozListova());
                v.Listovi.Add(l);
            }
            foreach (var l in listovi.Where(x => !x.Stanica1.HasValue || !x.Stanica3.HasValue))
            {
                var bezDeonice = !l.Stanica1.HasValue && !l.Stanica3.HasValue;
                var v = vozovi.FirstOrDefault(x => ((l.Stanica1.HasValue && x.Listovi[0].Stanica1 == l.Stanica1) || (l.Stanica3.HasValue && x.Listovi[0].Stanica3 == l.Stanica3)) && Moze(x, l))
                        ?? (vozovi.Count == 1 && bezDeonice && Moze(vozovi[0], l) ? vozovi[0] : null)
                        // v2.9.2: list bez obe tačke deonice (nečitljiv sken – „12 е въглища.pdf“, listovi 6 i 9) ide u voz istog maila sa istom
                        // otpravnom/uputnom, pa sa istom grupom robe (NHM 4)
                        ?? (bezDeonice ? vozovi.FirstOrDefault(x => Moze(x, l) && x.Listovi.Any(y => (l.Otpravna.HasValue && y.Otpravna == l.Otpravna) || (l.Uputna.HasValue && y.Uputna == l.Uputna))) : null)
                        ?? (bezDeonice && l.P.Nhm4.Length > 0 ? vozovi.Where(x => Moze(x, l) && x.Listovi.Any(y => y.P.Nhm4 == l.P.Nhm4)).OrderByDescending(x => x.Listovi.Count).FirstOrDefault() : null);
                if (v == null) vozovi.Add(v = new VozListova());
                v.Listovi.Add(l);
            }
            // v2.9.2 (005NISBAR26): voz čiji listovi nemaju kola, a mase su iste kao mase lista drugog voza istog maila (CUV u prilogu
            // izvoznih dokumenata) – isti voz, ne poseban posao
            foreach (var v in vozovi.ToList())
            {
                if (vozovi.Count < 2 || !v.Listovi.All(l => l.P.Kola.Count == 0 && l.BrojKola == 0)) continue;
                var drugi = vozovi.FirstOrDefault(x => x != v && v.Listovi.All(b => Moze(x, b)) && x.Listovi.Any(a => v.Listovi.Any(b => IsteMase(ZapisTl.Iz(a), ZapisTl.Iz(b)))));
                if (drugi == null) continue;
                drugi.Listovi.AddRange(v.Listovi);
                vozovi.Remove(v);
            }
            return vozovi;
        }

        /// <summary>
        /// v3.0.1 (3.6): dva lista mogu biti isti voz samo kada se broj voza i referenca (r8) ne razlikuju – jednaki su ili jedan nije upisan.
        /// </summary>
        internal static bool IstiVozIliNepoznat(ListPosla a, ListPosla b)
        {
            string Ref(ListPosla x) => NormOznaka(x.P.V.TryGetValue("Referenca", out var v) ? v as string : null);
            var va = NormOznaka(a.P.BrojVoza); var vb = NormOznaka(b.P.BrojVoza);
            if (va.Length > 0 && vb.Length > 0 && va != vb) return false;
            var ra = Ref(a); var rb = Ref(b);
            if (ra.Length > 0 && rb.Length > 0 && ra != rb) return false;
            return true;
        }

        /// <summary>Bar dve iste mase (± 0,5 %), nijedna različita.</summary>
        private static bool IsteMase(ZapisTl a, ZapisTl b)
        {
            int iste = 0, razlicite = 0;
            foreach (var (x, y) in new[] { (a.Neto, b.Neto), (a.Tara, b.Tara), (a.Bruto, b.Bruto) })
                if (x > 0 && y > 0) { if (Math.Abs(x.Value - y.Value) <= 0.005 * Math.Max(x.Value, y.Value)) iste++; else razlicite++; }
            return iste >= 2 && razlicite == 0;
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

        /// <summary>
        /// v3.0 (0a.3): NHM za kola spiska (kada ga spisak nema) samo iz grupe posla čija su to kola – nikad iz druge grupe (ID 1654: IBC
        /// spisak je dobio NHM uglja iz grupe 1). Posao bez zapisa TL: NHM posla (kao ranije). Kola se ne poklapaju ni sa jednom grupom
        /// (a grupe imaju brojeve kola) → prazno (NULL).
        /// </summary>
        internal string NhmZaKola(int jobId, IEnumerable<string> kolaSpiska)
        {
            var gr = Grupe(ZapisiTl(jobId).Values);
            if (gr.Count == 0) return NhmPosla(jobId);
            return NhmGrupeZaKola(gr, kolaSpiska, id => sif.Nhm.FirstOrDefault(z => z.Id == id)?.Kod8);
        }

        internal static string NhmGrupeZaKola(List<List<ZapisTl>> gr, IEnumerable<string> kolaSpiska, Func<int, string> kod8)
        {
            string NhmGrupe(List<ZapisTl> g)
            {
                if (g[0].Prazna) return "992200";
                var ids = g.Select(z => z.NhmId).Where(x => x.HasValue).Distinct().ToList();
                if (ids.Count == 1 && kod8(ids[0].Value) is string k && k.Length > 0) return k;
                return g.Select(z => z.Nhm4).FirstOrDefault(x => !string.IsNullOrEmpty(x)) ?? "";
            }
            var kola = new HashSet<string>((kolaSpiska ?? Enumerable.Empty<string>()).Select(BrojKolaUic.Cifre).Where(x => x.Length > 0));
            var preklop = gr.Select(g => (g, n: g.SelectMany(z => z.Kola).Select(BrojKolaUic.Cifre).Distinct().Count(kola.Contains))).OrderByDescending(x => x.n).First();
            if (preklop.n > 0) return NhmGrupe(preklop.g);
            // tovarni listovi bez brojeva kola: samo kada je posao jedna grupa
            if (gr.Count == 1 && !gr[0].Any(z => z.Kola.Count > 0)) return NhmGrupe(gr[0]);
            return "";
        }

        /// <summary>Grupe posla iz zapisa TL: grupa 1 = natovarena kola sa najviše kola (ako ih nema – prazna), pa ostale po broju kola.</summary>
        internal static List<List<ZapisTl>> Grupe(IEnumerable<ZapisTl> zapisi)
        {
            var g = zapisi.GroupBy(z => z.Grupa).Select(x => x.ToList()).ToList();
            var prva = g.Where(x => !x[0].Prazna).OrderByDescending(x => x.Sum(z => z.BrojKola)).FirstOrDefault() ?? g.OrderByDescending(x => x.Sum(z => z.BrojKola)).FirstOrDefault();
            if (prva == null) return new List<List<ZapisTl>>();
            // v2.9.2 (3a.2, voz 005NISBAR26): grupa bez kola čije su mase iste kao mase grupe 1 (ili ih nema) nema sopstveni dokument
            // za mase – ne pravi se (grupa 2 dobija mase samo iz svojih dokumenata)
            double? Z(List<ZapisTl> x, Func<ZapisTl, double?> f) => x.Any(z => f(z) > 0) ? x.Sum(z => f(z) ?? 0) : (double?)null;
            bool IsteKaoPrva(List<ZapisTl> x)
            {
                var bez = !x.Any(z => z.Neto > 0 || z.Tara > 0 || z.Bruto > 0);
                if (bez) return true;
                foreach (var f in new Func<ZapisTl, double?>[] { z => z.Neto, z => z.Tara, z => z.Bruto })
                {
                    var a = Z(x, f); var b = Z(prva, f);
                    if (a.HasValue != b.HasValue || (a.HasValue && Math.Abs(a.Value - b.Value) > 0.005 * Math.Max(a.Value, b.Value))) return false;
                }
                return true;
            }
            g = g.Where(x => x == prva || !(x.All(z => z.BrojKola == 0 && z.Kola.Count == 0) && IsteKaoPrva(x))).ToList();
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
            decimal Tn(double? kg) => PosaoIzDokumenata.Tone(kg ?? 0);
            // v3.0.1 (3.8, Lukavac02: neto 4,945 t uz broj kola preuzet iz ranijeg zapisa): provera smisla i na nivou grupe posla
            if (PosaoIzDokumenata.NesmislenNeto(ne, bk, g.All(z => z.Prazna), null) is string bez)
            {
                p.Postavi("Neto" + s, 0m, opis + " – " + bez, "r25/r21", 0, "neto nije upisan: " + bez);
                p.Napomene.Add("Neto grupe " + s + " nije upisan: " + bez);
            }
            else
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

        /// <summary>Oznake voza iz teksta (r8, naslov): reči sa slovima i ciframa, 5+ znakova („SBGT26_SRB04“, „LDC8-HAMMNOV1“), normalizovane.</summary>
        internal static HashSet<string> OznakeVoza(params string[] tekstovi)
        {
            var s = new HashSet<string>(StringComparer.Ordinal);
            foreach (var t in tekstovi.Where(x => !string.IsNullOrWhiteSpace(x)))
                foreach (Match m in Regex.Matches(t, @"(?<![\p{L}\d])[\p{L}\d][\p{L}\d_\-/\.]{3,30}[\p{L}\d](?![\p{L}\d])"))
                {
                    var n = NormOznaka(m.Value);
                    if (n.Length >= 5 && Regex.IsMatch(n, @"\d") && Regex.IsMatch(n, @"[A-Z]") && !Regex.IsMatch(n, @"^\d+[A-Z]?$")) s.Add(n);
                }
            return s;
        }

        internal static string NormOznaka(string x) => Regex.Replace((x ?? "").ToUpperInvariant(), @"[^A-Z0-9]", "");

        /// <summary>
        /// v3.0 (0a.2, Zmajevo SBGT26_SRB04): prazna kola sa istom oznakom voza ili referencom (r8 lista, naslov maila) kao natovaren posao
        /// u suprotnom smeru – povratak praznih (grupa 2 istog posla), i kada se brojevi kola ne poklapaju.
        /// </summary>
        private (int? id, string opis) PovratakPraznihPoOznaci(VozListova voz, MailZaUpis m)
        {
            var oznake = OznakeVoza(voz.Listovi.Select(x => x.P.V.TryGetValue("Referenca", out var v) ? v as string : null).Concat(new[] { m.Naslov }).ToArray());
            if (oznake.Count == 0) return (null, "");
            var l0 = voz.Listovi[0];
            foreach (var k in NedavniPoslovi().OrderByDescending(x => x.Id))
            {
                var nadjena = OznakeVoza(k.Referenca, k.Naslovi).FirstOrDefault(oznake.Contains);
                if (nadjena == null || PosaoPraznih(k.Id)) continue;
                var j = Red("SELECT OtpravnaID, UputnaID, Stanica1ID, Stanica3ID, Referenca FROM planiranje.tmpJobs WHERE ID = @id", "@id", k.Id);
                bool Isto(object a, int? b) => a is int x && b.HasValue && x == b.Value;
                var suprotno = Isto(j["UputnaID"], l0.Otpravna) || Isto(j["OtpravnaID"], l0.Uputna) || Isto(j["Stanica3ID"], l0.Stanica1) || Isto(j["Stanica1ID"], l0.Stanica3);
                if (!suprotno) continue;
                var pune = ZapisiTl(k.Id).Values.Where(z => !z.Prazna).SelectMany(z => z.Kola);
                return (k.Id, OpisPovratka(j["Referenca"] as string ?? nadjena, pune, voz.Listovi.SelectMany(x => x.P.Kola)));
            }
            return (null, "");
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

        /// <summary>v3.0.1 (3.5): posao ranijeg maila iz dosijea („UID 46“) – JobID dokumenta tog maila u tmpDokument; null ako ga nema.</summary>
        private int? PosaoMaila(string oznaka, MailZaUpis m)
        {
            var u = Regex.Match(oznaka ?? "", @"^UID (\d+)$");
            if (!u.Success) return null;
            var j = Skalar("SELECT TOP 1 JobID FROM planiranje.tmpDokument WHERE UID = @u AND Sanduce = @s AND JobID IS NOT NULL ORDER BY ID DESC", "@u", long.Parse(u.Groups[1].Value), "@s", N(m.Sanduce, 200));
            return j == null ? (int?)null : Convert.ToInt32(j);
        }

        /// <summary>„ID 1602“, „ID 1602 + 1496“, „ID1657“ iz naslova maila → brojevi (3–5 cifara).</summary>
        internal static HashSet<string> IdIzNaslova(string naslov)
        {
            var s = new HashSet<string>();
            foreach (Match m in Regex.Matches(naslov ?? "", @"(?<![\p{L}\d])ID\s*[:#.]?\s*(?<b>\d{3,5})(?<dalje>(?:\s*[+,/&i]\s*\d{3,5}(?!\d))*)"))
            {
                s.Add(m.Groups["b"].Value);
                foreach (Match d in Regex.Matches(m.Groups["dalje"].Value, @"\d{3,5}")) s.Add(d.Value);
            }
            return s;
        }

        private sealed class ZnaciPosla
        {
            public int Id; public int? O, U; public int PouzdO, PouzdU; public string Referenca = ""; public string Kljuc = "";
            public HashSet<string> Ids = new HashSet<string>(); public DateTime? Primljeno; public HashSet<string> Kola = new HashSet<string>();
            public List<ZapisTl> Tl = new List<ZapisTl>(); public bool Nit; public string Ponuda = ""; public int BrojKola; public string BrojVoza = "";
        }

        /// <summary>
        /// v2.9.2 (3a.1): postojeći posao kada r62 nije pouzdan ključ (nečitljiva ili pogrešno pročitana – „12“ umesto „72“) – zbir znakova:
        /// isti „ID nnnn“ u naslovu maila ili ista prepiska (dosije); ista referenca r8; ista Otpravna i Uputna (relacija); isti NHM;
        /// kola ≥ 50 % (samo brojevi sa ispravnom kontrolnom cifrom UIC); mase ± 1 %; datum ± 5 dana. Dovoljno je: (ID iz naslova ili r8)
        /// + relacija, ili kola ≥ 50 % + relacija; uz to – ista prepiska + relacija + isti NHM + iste mase (Prahovo 05, skenovi bez kola i
        /// r8). Različite reference r8 (Coal 16 / Coal 17) nikad nisu isti voz, osim kada su kola ista. Relacija: ista uputna i ista
        /// otpravna – ili otpravna nepouzdano pročitana na jednoj strani (OCR šum u r16), i obrnuto.
        /// </summary>
        private (int? id, string opis) PosaoPoZnacima(VozListova voz, MailZaUpis m)
        {
            var l0 = voz.Listovi.OrderByDescending(x => !x.P.Prazna).ThenByDescending(x => x.BrojKola).First();
            int Pouzd(ListPosla l, string kol) => l.P.Poreklo.FirstOrDefault(po => po.Kolona == kol)?.Pouzdanost ?? 0;
            var o = l0.Otpravna; var u = l0.Uputna; var po0 = Pouzd(l0, "OtpravnaID"); var pu0 = Pouzd(l0, "UputnaID");
            if (!o.HasValue && !u.HasValue) return (null, null);
            string N(object x) => Regex.Replace(Slicnost.Osnovno(x as string ?? ""), @"\s", "");
            var ref0 = voz.Listovi.Select(x => N(x.P.V.TryGetValue("Referenca", out var v) ? v : null)).FirstOrDefault(x => x.Length > 0) ?? "";
            var ids0 = IdIzNaslova(m.Naslov);
            var vozBroj0 = NormOznaka(l0.P.BrojVoza);
            // kola voza: listovi + spiskovi kola istog maila; samo brojevi sa ispravnom kontrolnom cifrom
            var kola0 = new HashSet<string>(voz.Listovi.SelectMany(x => x.P.Kola).Concat((m.Provera?.Dokumenti ?? new List<Model.Dokument>())
                .Where(d => d.Tip == Model.TipDokumenta.SpisakKola && !d.IzDosijea).SelectMany(d => d.Spisak).SelectMany(s => s.Kola).Select(k => k.Broj))
                .Select(BrojKolaUic.Cifre).Where(BrojKolaUic.KontrolnaIspravna));
            var zapisi0 = voz.Listovi.Select(ZapisTl.Iz).ToList();
            var ponuda0 = voz.Listovi.Select(x => x.P.V.TryGetValue("BrojPonude", out var v) ? Convert.ToString(v) : "").FirstOrDefault(x => Regex.IsMatch(x ?? "", @"^\d{1,6}$")) ?? "";
            var brojKola0 = voz.Listovi.Sum(x => x.BrojKola);
            var nit = new HashSet<int>();
            if (m.Dosije != null)
                foreach (var oz in m.Dosije.Mailovi)
                {
                    var uu = Regex.Match(oz ?? "", @"^UID (\d+)$");
                    if (!uu.Success) continue;
                    foreach (var j in Kolona("SELECT DISTINCT JobID FROM planiranje.tmpDokument WHERE UID = @u AND Sanduce = @s AND JobID IS NOT NULL", "@u", long.Parse(uu.Groups[1].Value), "@s", N0(m.Sanduce, 200)))
                        if (int.TryParse(j, out var ji)) nit.Add(ji);
                }

            // kandidati: poslovi poslednjih dana (datum prijema dokumenata ± 5 dana od maila)
            var kand = new Dictionary<int, ZnaciPosla>();
            using (var cmd = Komanda(@"SELECT j.ID, j.OtpravnaID, j.UputnaID, ISNULL(j.Referenca, ''), ISNULL(j.KljucPosiljke, ''),
    ISNULL((SELECT TOP 1 p.Pouzdanost FROM planiranje.tmpJobsPolje p WHERE p.JobID = j.ID AND p.TeretnicaID IS NULL AND p.Kolona = 'OtpravnaID' ORDER BY p.ID DESC), 0),
    ISNULL((SELECT TOP 1 p.Pouzdanost FROM planiranje.tmpJobsPolje p WHERE p.JobID = j.ID AND p.TeretnicaID IS NULL AND p.Kolona = 'UputnaID' ORDER BY p.ID DESC), 0),
    (SELECT MAX(d.Primljeno) FROM planiranje.tmpDokument d WHERE d.JobID = j.ID), ISNULL(j.BrojPonude, ''), ISNULL(j.BrojKola1, 0)
FROM planiranje.tmpJobs j WHERE j.Kreirano >= DATEADD(day, -@dana, SYSDATETIME())", "@dana", DanaZaVezu))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                    kand[r.GetInt32(0)] = new ZnaciPosla
                    {
                        Id = r.GetInt32(0), O = r.IsDBNull(1) ? (int?)null : r.GetInt32(1), U = r.IsDBNull(2) ? (int?)null : r.GetInt32(2), Referenca = N(r.GetString(3)), Kljuc = r.GetString(4),
                        PouzdO = Convert.ToInt32(r.GetValue(5)), PouzdU = Convert.ToInt32(r.GetValue(6)), Primljeno = r.IsDBNull(7) ? (DateTime?)null : r.GetDateTime(7),
                        Ponuda = Convert.ToString(r.GetValue(8)).Trim(), BrojKola = Convert.ToInt32(r.GetValue(9)), BrojVoza = NormOznaka(ExcelCitac.BrojVoza(r.GetString(3))),
                    };
            if (kand.Count == 0) return (null, null);
            using (var cmd = Komanda("SELECT DISTINCT JobID, ISNULL(Naslov, '') FROM planiranje.tmpDokument WHERE JobID IS NOT NULL"))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) if (kand.TryGetValue(r.GetInt32(0), out var z)) foreach (var x in IdIzNaslova(r.GetString(1))) z.Ids.Add(x);
            using (var cmd = Komanda("SELECT JobID, TekstIzDokumenta FROM planiranje.tmpJobsPolje WHERE TeretnicaID IS NULL AND Kolona LIKE 'TL:%' AND JobID IS NOT NULL"))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                {
                    if (!kand.TryGetValue(r.GetInt32(0), out var z)) continue;
                    try { var t = JsonConvert.DeserializeObject<ZapisTl>(r.IsDBNull(1) ? "" : r.GetString(1)); if (t != null) { z.Tl.Add(t); foreach (var k in t.Kola) z.Kola.Add(BrojKolaUic.Cifre(k)); } } catch { }
                }
            using (var cmd = Komanda("SELECT t.JobID, s.BrojKola FROM planiranje.tmpTeretnicaStavke s JOIN planiranje.tmpTeretnica t ON t.ID = s.BrojTeretnice WHERE t.JobID IS NOT NULL"))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) if (kand.TryGetValue(r.GetInt32(0), out var z)) z.Kola.Add(BrojKolaUic.Cifre(r.IsDBNull(1) ? "" : r.GetString(1)));
            foreach (var j in nit) if (kand.TryGetValue(j, out var z)) z.Nit = true;

            bool Rel(ZnaciPosla z)
            {
                const int Pouzdano = 95;
                var istaO = o.HasValue && z.O == o; var istaU = u.HasValue && z.U == u;
                var nepO = !o.HasValue || !z.O.HasValue || po0 < Pouzdano || z.PouzdO < Pouzdano;
                var nepU = !u.HasValue || !z.U.HasValue || pu0 < Pouzdano || z.PouzdU < Pouzdano;
                return (istaO && istaU) || (istaU && nepO && o != z.U) || (istaO && nepU && u != z.O);
            }
            bool IsteMase(ZnaciPosla z) => z.Tl.Any(t => zapisi0.Any(x =>
            {
                int iste = 0, razl = 0;
                foreach (var (a, b) in new[] { (t.Neto, x.Neto), (t.Tara, x.Tara), (t.Bruto, x.Bruto) })
                    if (a > 0 && b > 0) { if (Math.Abs(a.Value - b.Value) <= 0.01 * Math.Max(a.Value, b.Value)) iste++; else razl++; }
                return iste >= 1 && razl == 0;
            }));
            bool IstiNhm(ZnaciPosla z) => z.Tl.Any(t => zapisi0.Any(x => x.Grupa.Length > 0 && t.Grupa == x.Grupa));
            // isti r62 osim oznake zemlje („12 16201-6 3212 16390-0“ = „72 16201-6 3212 16390-0“)
            string BezZemlje(string k) => Regex.Match(k ?? "", @"^r62x?:\d{2}-(.+)$").Groups[1].Value;
            var r62bz = voz.Listovi.Select(x => BezZemlje(x.P.KljucPosiljke)).FirstOrDefault(x => x.Length > 0) ?? "";

            var izbor = new List<(ZnaciPosla z, int poeni, string opis)>();
            foreach (var z in kand.Values)
            {
                if (z.Primljeno.HasValue && Math.Abs((z.Primljeno.Value - m.Primljeno).TotalDays) > 5) continue;
                if (!Rel(z)) continue;
                var istiKola = kola0.Count > 0 && z.Kola.Count > 0 ? kola0.Intersect(z.Kola).Count() : 0;
                var kola50 = istiKola > 0 && istiKola >= 0.5 * Math.Min(kola0.Count, z.Kola.Count);
                var refIsta = ref0.Length > 0 && z.Referenca.Length > 0 && ref0 == z.Referenca;
                var refRazl = ref0.Length > 0 && z.Referenca.Length > 0 && ref0 != z.Referenca;
                if (refRazl && !kola50) continue;   // Coal 16 / Coal 17 – različiti vozovi iste relacije
                // v3.0.1 (3.6): različit broj voza – nikad isti posao (ni uz ista kola: isti vagoni u sledećem obrtu)
                if (vozBroj0.Length > 0 && z.BrojVoza.Length > 0 && vozBroj0 != z.BrojVoza) continue;
                var idIsti = ids0.Count > 0 && z.Ids.Overlaps(ids0);
                var r62Isti = r62bz.Length > 0 && BezZemlje(z.Kljuc) == r62bz;
                // dve ispravne, različite r62 su dve pošiljke – isti voz samo uz isti ID iz naslova ili r8 (voz od više listova)
                var dveR62 = (z.Kljuc ?? "").StartsWith("r62:", StringComparison.Ordinal) && voz.Listovi.Any(x => (x.P.KljucPosiljke ?? "").StartsWith("r62:", StringComparison.Ordinal))
                             && !voz.Listovi.Any(x => x.P.KljucPosiljke == z.Kljuc);
                if (dveR62 && !idIsti && !refIsta) continue;
                var opis = new List<string>();
                if (idIsti) opis.Add("ID " + string.Join("/", ids0.Intersect(z.Ids)) + " iz naslova");
                if (refIsta) opis.Add("r8");
                if (kola50) opis.Add("kola " + istiKola + "/" + Math.Min(kola0.Count, z.Kola.Count));
                if (r62Isti) opis.Add("r62 osim oznake zemlje");
                var dovoljno = idIsti || refIsta || kola50 || r62Isti;
                if (!dovoljno && z.Nit && IstiNhm(z) && IsteMase(z)) { dovoljno = true; opis.Add("ista prepiska, NHM i mase ± 1 %"); }
                // ista ponuda KP (ceo broj) + isti NHM + isti broj kola (Bor ID 1657: CUV iz „Status najave_koks“ i „Dokumentacija za predaju“)
                if (!dovoljno && ponuda0.Length > 0 && z.Ponuda == ponuda0 && IstiNhm(z) && brojKola0 > 0 && z.BrojKola == brojKola0) { dovoljno = true; opis.Add("ista ponuda " + ponuda0 + ", NHM i broj kola"); }
                if (!dovoljno) continue;
                opis.Add("relacija");
                var poeni = (kola50 ? 4 : 0) + (r62Isti ? 4 : 0) + (refIsta ? 3 : 0) + (idIsti ? 3 : 0) + (z.Nit ? 1 : 0) + (IstiNhm(z) ? 1 : 0) + (IsteMase(z) ? 1 : 0);
                izbor.Add((z, poeni, string.Join(", ", opis)));
            }
            if (izbor.Count == 0) return (null, null);
            var naj = izbor.OrderByDescending(x => x.poeni).ThenBy(x => x.z.Id).First();
            return (naj.z.Id, "znaci: " + naj.opis);
        }

        private static string N0(string s, int n) => string.IsNullOrEmpty(s) ? "" : s.Length <= n ? s : s.Substring(0, n);

        /// <summary>Dva zapisa TL su isti tovarni list (kopija iz drugog maila sa drugim ključem): ista kola (≥ 80 %) ili iste mase (bar dve, nijedna različita).</summary>
        internal static bool IstiZapis(ZapisTl a, ZapisTl b)
        {
            if (a.Kola.Count > 0 && b.Kola.Count > 0) return a.Kola.Intersect(b.Kola).Count() >= 0.8 * Math.Min(a.Kola.Count, b.Kola.Count);
            // v3.0 (LDC8): ista referenca (r8), ista grupa robe i isti broj kola – nova verzija istog lista (draft → konačni CIM), i kada
            // se mase razlikuju (draft: tara 643,420; konačni: 651,600)
            if (!string.IsNullOrEmpty(a.Ref) && a.Ref == b.Ref && a.Grupa.Length > 0 && a.Grupa == b.Grupa && a.BrojKola > 1 && a.BrojKola == b.BrojKola) return true;   // samo list za više kola (CIM po vagonu – ID 1654 – ima istu referencu za svaki vagon)
            // v3.0.1 (3.3, Bor ID 1657: VTG UID 66 bez r62 + KP UID 103 → 74 kola): radna verzija (bez r62) i konačna (sa r62) istog lista – i za CUV.
            // Ista grupa robe, isti broj kola (više kola), reference iste ili jedna nije upisana, mase bez većih razlika (± 5 %) → zamena, ne zbir.
            if (a.Kljuc.StartsWith("r62:", StringComparison.Ordinal) != b.Kljuc.StartsWith("r62:", StringComparison.Ordinal) && a.Grupa.Length > 0 && a.Grupa == b.Grupa
                && a.BrojKola > 1 && a.BrojKola == b.BrojKola && (string.IsNullOrEmpty(a.Ref) || string.IsNullOrEmpty(b.Ref) || a.Ref == b.Ref)
                && new[] { (a.Neto, b.Neto), (a.Tara, b.Tara), (a.Bruto, b.Bruto) }.All(m => !(m.Item1 > 0 && m.Item2 > 0) || Math.Abs(m.Item1.Value - m.Item2.Value) <= 0.05 * Math.Max(m.Item1.Value, m.Item2.Value)))
                return true;
            int iste = 0, razlicite = 0;
            foreach (var (x, y) in new[] { (a.Neto, b.Neto), (a.Tara, b.Tara), (a.Bruto, b.Bruto) })
                if (x > 0 && y > 0) { if (Math.Abs(x.Value - y.Value) <= 0.001 * Math.Max(x.Value, y.Value)) iste++; else razlicite++; }   // v3.0.1 (3.1): kopija = iste mase ± 0,1 %
            // v2.9.2: kopije skena sa jednom masom (Prahovo 05: samo neto 924,4) – ista masa, ista grupa robe i isti broj kola
            // v3.0 (LDC8, UID 789): kopija sa jednom masom bez pročitanog broja kola (0) je isti list za VIŠE kola (ne za list jednih kola –
            // ID 1654: dva vagona uglja mogu imati isti neto 60,130)
            return razlicite == 0 && (iste >= 2 || (iste == 1 && a.Grupa.Length > 0 && a.Grupa == b.Grupa && (a.BrojKola == b.BrojKola || (a.BrojKola == 0 && b.BrojKola > 1) || (b.BrojKola == 0 && a.BrojKola > 1))));
        }

        /// <summary>v3.0: nova verzija lista preuzima iz ranije ono što sama nema (sken bez broja kola, kopija sa samo netom).</summary>
        internal static void DopuniZapis(ZapisTl novi, ZapisTl stari)
        {
            if (novi.Neto == null && stari.Neto != null && novi.Tara == null && novi.Bruto == null) { novi.Neto = stari.Neto; }
            // nova verzija sa delimičnim masama (samo neto) zadržava taru/bruto ranije pune verzije kada se neto slaže
            if (novi.Neto.HasValue && stari.Neto.HasValue && Math.Abs(novi.Neto.Value - stari.Neto.Value) <= 0.005 * Math.Max(novi.Neto.Value, stari.Neto.Value))
            { if (novi.Tara == null) novi.Tara = stari.Tara; if (novi.Bruto == null) novi.Bruto = stari.Bruto; }
            if (novi.BrojKola == 0) novi.BrojKola = stari.BrojKola;
            if (novi.Kola.Count == 0) novi.Kola = stari.Kola.ToList();
            if (string.IsNullOrEmpty(novi.Ref)) novi.Ref = stari.Ref;
            if (string.IsNullOrEmpty(novi.Nhm4)) { novi.Nhm4 = stari.Nhm4; novi.NhmId = stari.NhmId; }
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
            // v3.0.1 (3.5, YU DIN UID 54): dosije je prepoznao isti tovarni list iz ranijeg maila – mail ide u posao tog maila i kada je r62 nesiguran
            if (id == null)
                foreach (var l in voz.Listovi.Where(x => !string.IsNullOrEmpty(x.P.DosijeVeza)))
                    if (PosaoMaila(l.P.DosijeVeza, m) is int jd0) { id = jd0; veza = "dosije: isti tovarni list kao " + l.P.DosijeVeza + (l.P.OverenaVerzija ? " (skenirana i overena verzija)" : ""); break; }
            var glavni0 = voz.Listovi.OrderByDescending(x => !x.P.Prazna).ThenByDescending(x => x.BrojKola).First();
            if (id == null && glavni0.P.BrojVoza.Length > 0 && glavni0.Otpravna is int o0 && glavni0.Uputna is int u0)
            {
                var isti = NedavniPoslovi().Where(j => ExcelCitac.BrojVoza(j.Referenca) == glavni0.P.BrojVoza && j.Otpravna == o0 && j.Uputna == u0).OrderByDescending(j => j.Id).FirstOrDefault();
                if (isti != null) { id = isti.Id; veza = "isti voz " + glavni0.P.BrojVoza + " i relacija"; }
            }
            // v2.9.2 (3a.1): r62 nije pouzdan ključ – posao po zbiru znakova (ID iz naslova / r8 / kola + relacija, …)
            if (id == null && PosaoPoZnacima(voz, m) is var zn && zn.id.HasValue) { id = zn.id; veza = zn.opis; }
            if (id == null && PosaoIzDosijeaIsteDeonice(m, voz) is int jd) { id = jd; veza = "isti dosije i deonica KP"; }
            var povratak = false;
            if (id == null && voz.SvaPrazna && PovratakPraznih(voz) is int jp) { id = jp; povratak = true; veza = "povratak praznih kola uz posao " + jp; }
            // v3.0 (0a.2): drugi, nezavisan način – ista oznaka voza ili referenca (r8, naslov) + suprotan smer + prazna kola
            if (id == null && voz.SvaPrazna && PovratakPraznihPoOznaci(voz, m) is var poOzn && poOzn.id.HasValue) { id = poOzn.id; povratak = true; veza = poOzn.opis + " (posao " + poOzn.id + ")"; }
            if (veza.Length > 0 && !veza.StartsWith("ključ", StringComparison.Ordinal)) ishod.Napomene.Add(voz.Listovi.Count + " tovarnih listova → posao " + id + " (" + veza + ")");

            // zapisi TL: postojeći + listovi ovog maila
            var tl = id.HasValue ? ZapisiTl(id.Value) : new Dictionary<string, ZapisTl>(StringComparer.Ordinal);
            var noviKljucevi = new HashSet<string>();
            var alias = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var l in voz.Listovi)
            {
                var z = ZapisTl.Iz(l); z.Povratak = povratak;
                // v3.0.1 (3.5): overena verzija već upisanog lista (dosije) – isti list: dopunjuje postojeći zapis iste grupe, ne dodaje kola ni mase
                if (l.P.OverenaVerzija && !tl.ContainsKey(KolonaTl(z.Kljuc)))
                {
                    var postojeci = tl.Where(kv => !noviKljucevi.Contains(kv.Key) && (kv.Value.Grupa == z.Grupa || z.Grupa.Length == 0)).OrderByDescending(kv => kv.Value.BrojKola).Select(kv => kv.Key).FirstOrDefault();
                    if (postojeci != null)
                    {
                        DopuniZapis(tl[postojeci], z);
                        alias[KolonaTl(z.Kljuc)] = postojeci;
                        ishod.Napomene.Add("tovarni list " + l.Oznaka + " je skenirana i overena verzija lista " + postojeci + " (" + l.P.DosijeVeza + ") – isti zapis");
                        continue;
                    }
                }
                // v3.0 (LDC8, UID 789): isti list dva puta u istom mailu (konačni CIM i kopija „ex docs“ sa drugim ključem) – jedan zapis
                var istiUMailu = tl.Where(kv => noviKljucevi.Contains(kv.Key) && kv.Key != KolonaTl(z.Kljuc) && (kv.Value.BrojKola > 1 || z.BrojKola > 1) && IstiZapis(kv.Value, z)).Select(kv => kv.Key).FirstOrDefault();
                if (istiUMailu != null)
                {
                    DopuniZapis(tl[istiUMailu], z);
                    alias[KolonaTl(z.Kljuc)] = istiUMailu;
                    if (tl.ContainsKey(KolonaTl(z.Kljuc)) && !noviKljucevi.Contains(KolonaTl(z.Kljuc)))
                    {
                        tl.Remove(KolonaTl(z.Kljuc));
                        if (id.HasValue) Izvrsi("DELETE FROM planiranje.tmpJobsPolje WHERE JobID = @j AND TeretnicaID IS NULL AND Kolona = @k", "@j", id.Value, "@k", KolonaTl(z.Kljuc));
                    }
                    ishod.Napomene.Add("tovarni list " + l.Oznaka + " je isti list kao " + istiUMailu + " (u istom mailu) – jedan zapis");
                    continue;
                }
                // kopija istog lista iz ranijeg maila sa drugim ključem (sken „ref:…“ / r62) zamenjuje raniji zapis – mase se ne sabiraju dvaput
                foreach (var stari in tl.Where(kv => kv.Key != KolonaTl(z.Kljuc) && !noviKljucevi.Contains(kv.Key) && IstiZapis(kv.Value, z)).Select(kv => kv.Key).ToList())
                {
                    // v3.0: ono što nova kopija nema (mase, broj kola, kola, referenca) ostaje iz ranijeg zapisa
                    DopuniZapis(z, tl[stari]);
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
                // v3.0 (Zmajevo): grupa 2 iz spiska (povratak praznih – teretnica sa Grupa = 2) ostaje i kada tovarni listovi imaju samo grupu 1
                else if (i == 1 && !(id.HasValue && Skalar("SELECT TOP 1 ID FROM planiranje.tmpTeretnica WHERE JobID = @j AND Grupa = 2", "@j", id.Value) != null))
                    foreach (var k in new[] { "NHM2", "Bruto2", "BrojKola2", "Duzina2", "Neto2", "RIDBroj2", "OMBroj2", "SerijaKola2" }) zbir.V[k] = null;
            }
            zbir.Kola = voz.Listovi.SelectMany(x => x.P.Kola).Distinct().ToList();
            if (grupe.Count > 2) zbir.Napomene.Add("Više od dve grupe robe u vozu – ostale grupe: " + string.Join("; ", grupe.Skip(2).Select(g => g.Sum(z => z.BrojKola) + " kola NHM " + g[0].Grupa)));
            var dokId = glavni?.DokId ?? voz.Listovi.Select(x => x.DokId).FirstOrDefault(x => x.HasValue);
            if (id == null && zbir.KljucPosiljke == null) { zbir.KljucPosiljke = glavni0.P.KljucPosiljke; zbir.V["KljucPosiljke"] = PodaciPosla.Skrati(glavni0.P.KljucPosiljke, 200); }
            var jid = UpisiPosao(zbir, id, dokId, m, povratak ? "POVRATAK_PRAZNIH" : "NOVA_VERZIJA_CIM", ishod);
            // zapisi TL (poreklo grupa) i istorija povratka
            foreach (var l in voz.Listovi)
            {
                if (!tl.TryGetValue(alias.TryGetValue(KolonaTl(l.KljucTl), out var a0) ? a0 : KolonaTl(l.KljucTl), out var z)) continue;
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
            if (dokId.HasValue) Izvrsi("UPDATE planiranje.tmpDokument SET JobID = ISNULL(JobID, @j) WHERE ID = @d", "@j", j, "@d", dokId.Value);   // v2.9.2 (3a.7)
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
                var t = PosaoIzDokumenata.Tone(kg.Value);
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
                if (Raspakivanje.Ukras(f) != null || Raspakivanje.SopstveniPrimer(f) != null) continue;
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

        /// <summary>v3.0 (0a.2): poreklo povratka praznih – „povratak praznih po oznaci voza SBGT26_SRB04, kola se ne poklapaju“.</summary>
        internal static string OpisPovratka(string oznakaVoza, IEnumerable<string> kolaPunih, IEnumerable<string> kolaPraznih)
        {
            var pune = new HashSet<string>((kolaPunih ?? Enumerable.Empty<string>()).Select(BrojKolaUic.Cifre).Where(x => x.Length > 0));
            var prazna = (kolaPraznih ?? Enumerable.Empty<string>()).Select(BrojKolaUic.Cifre).Where(x => x.Length > 0).Distinct().ToList();
            var istih = prazna.Count(pune.Contains);
            var oznaka = string.IsNullOrWhiteSpace(oznakaVoza) ? "" : " po oznaci voza " + oznakaVoza.Trim();
            return "povratak praznih" + oznaka + (pune.Count == 0 ? "" : istih == 0 ? ", kola se ne poklapaju" : ", " + istih + " od " + prazna.Count + " kola ista");
        }

        /// <summary>v3.0 (0a.1): tekst INTERNO napomene – „spisak (28 kola) ≠ tovarni list (30 kola) – proveriti“.</summary>
        internal static string SpisakNijeTovarniList(int kolaSpiska, int kolaTl, int tudjih) =>
            "spisak (" + kolaSpiska + " kola) ≠ tovarni list (" + kolaTl + " kola)" + (tudjih > 0 ? ", " + tudjih + " kola nisu u tovarnom listu" : "") + " – proveriti";

        /// <summary>v3.0 (0a.1): da li spisak sme da menja broj kola i mase posla (ne sme: manje kola od tovarnog lista ili kola kojih nema u listu).</summary>
        internal static bool SpisakMenjaPosao(IEnumerable<string> kolaSpiska, IEnumerable<string> kolaTl, int brojKolaTl)
        {
            var tl = new HashSet<string>((kolaTl ?? Enumerable.Empty<string>()).Select(BrojKolaUic.Cifre).Where(x => x.Length > 0));
            var sp = (kolaSpiska ?? Enumerable.Empty<string>()).Select(BrojKolaUic.Cifre).ToList();
            var broj = Math.Max(brojKolaTl, tl.Count);
            if (broj == 0) return true;
            if (sp.Count < broj) return false;                       // manje kola od tovarnog lista (LDC8: 28 od 30)
            if (tl.Count == 0 || sp.All(tl.Contains)) return true;    // isti skup kola
            // spisak ima VIŠE kola od pročitanih tovarnih listova i sadrži sva njihova kola – neki list nije pročitan (ID 1654: 5 IBC kola,
            // pročitana 4 lista) – spisak ima prednost; inače (druga kola) ne menja posao
            return sp.Count > broj && tl.All(sp.Contains);
        }

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
            List<KolaNaSpisku> Stavke(int t)
            {
                var l = new List<KolaNaSpisku>();
                using (var cmd = Komanda("SELECT BrojKola, ISNULL(Serija, ''), ISNULL(BrojOsovina, 0), Duzina, Tara, Neto, ISNULL(VRNP, ''), ISNULL(RID, '') FROM planiranje.tmpTeretnicaStavke WHERE BrojTeretnice = @t ORDER BY RB", "@t", t))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                    {
                        double? T(int i) => r.IsDBNull(i) ? (double?)null : Convert.ToDouble(r.GetValue(i)) * 1000;   // stavke su u t, spisak u kg
                        var nhm = r.GetString(6);
                        l.Add(new KolaNaSpisku
                        {
                            Broj = BrojKolaUic.Cifre(r.GetString(0)), Serija = r.GetString(1).Length > 1 ? r.GetString(1) : "", Nhm = nhm, KemlerUn = r.GetString(7),
                            TaraKg = T(4) > 0 ? T(4) : null, NetoKg = T(5), DuzinaM = r.IsDBNull(3) ? (double?)null : Convert.ToDouble(r.GetValue(3)), DuzinaIzvor = r.IsDBNull(3) ? "" : izvorDuzine,
                            Prazna = Regex.IsMatch(nhm, "^992[12]"),
                        });
                    }
                return l;
            }
            s.Kola.AddRange(Stavke(tid));
            if (s.Kola.Count == 0) return;
            // v3.0.1 (3.1, amonijak UID 259: dva lista sa po jednim spiskom od 12 kola): spiskovi RAZLIČITIH kola iste robe u istom poslu (drugi
            // tovarni list istog voza) se sabiraju u grupu – drugi spisak ne gazi prvi (24 kola, ne 12)
            string Grupa(IEnumerable<KolaNaSpisku> k) => k.Select(x => x.Prazna ? "9922" : Poklapanje.Nhm4(x.Nhm)).GroupBy(x => x).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault() ?? "";
            var mojaGrupa = Grupa(s.Kola);
            var mojaKola = new HashSet<string>(s.Kola.Select(k => k.Broj));
            var drugiSpiskovi = 0;
            foreach (var t in Kolona("SELECT ID FROM planiranje.tmpTeretnica WHERE JobID = @j AND ID <> @t", "@j", jobId, "@t", tid).Select(int.Parse))
            {
                var druga = Stavke(t);
                if (druga.Count == 0 || druga.Any(k => mojaKola.Contains(k.Broj)) || Grupa(druga) != mojaGrupa) continue;
                s.Kola.AddRange(druga); foreach (var k in druga) mojaKola.Add(k.Broj);
                drugiSpiskovi++;
            }
            if (drugiSpiskovi > 0) ishod.Napomene.Add("spisak kola sabran sa " + drugiSpiskovi + " ranijim spiskom istog posla (druga kola iste robe) – ukupno " + s.Kola.Count + " kola");
            // v3.0 (0a.4): stavke čuvaju mase u tonama na 3 decimale (po kolima zaokruženo na kg) – kada je originalni spisak istog maila
            // dostupan, mase po kolima idu iz njega, pa se zbir zaokružuje jednom (SAD-HER 80: 694 940,48 kg → 694,940 t, ne 694,941)
            if (izvorni != null)
            {
                var poBroju = izvorni.Kola.GroupBy(k => BrojKolaUic.Cifre(k.Broj)).ToDictionary(g => g.Key, g => g.First());
                foreach (var k in s.Kola)
                    if (poBroju.TryGetValue(BrojKolaUic.Cifre(k.Broj), out var o))
                    {
                        if (o.TaraKg.HasValue && k.TaraKg.HasValue && Math.Abs(o.TaraKg.Value - k.TaraKg.Value) < 1) k.TaraKg = o.TaraKg;
                        if (o.NetoKg.HasValue && k.NetoKg.HasValue && Math.Abs(o.NetoKg.Value - k.NetoKg.Value) < 1) k.NetoKg = o.NetoKg;
                        // v3.0.1 (3.2): bruto po kolima (stavke ga ne čuvaju) – za proveru da li je spisak usklađen sam sa sobom
                        if (o.BrutoKg.HasValue) k.BrutoKg = o.BrutoKg;
                    }
            }
            // neto 0 u stavci je „nije navedeno“ kada nijedna stavka nema neto (spisak samo sa tarom)
            if (s.Kola.All(k => (k.NetoKg ?? 0) == 0) && !s.Kola.All(k => k.Prazna)) foreach (var k in s.Kola) k.NetoKg = null;
            if (izvorni != null && drugiSpiskovi == 0) { s.UkupnoTaraKg = izvorni.UkupnoTaraKg; s.UkupnoNetoKg = izvorni.UkupnoNetoKg; s.UkupnoBrutoKg = izvorni.UkupnoBrutoKg; }
            if (izvorni != null) { s.DuzinaVozaM = izvorni.DuzinaVozaM; s.PrelazniPut = izvorni.PrelazniPut; s.Prevoznici = izvorni.Prevoznici; s.NhmJedinica = izvorni.NhmJedinica; }
            var p = PosaoIzDokumenata.IzSpiska(s, pk, NhmZaKola(jobId, s.Kola.Select(k => k.Broj)), "");
            var stari = Red("SELECT * FROM planiranje.tmpJobs WHERE ID = @id", "@id", jobId);
            var rucno = Kolona("SELECT Kolona FROM planiranje.tmpJobsPolje WHERE JobID = @id AND TeretnicaID IS NULL AND RucnoIzmenjeno = 1", "@id", jobId);

            // grupa teretnice: po kolima u zapisima TL posla; spisak samo praznih kola → grupa praznih
            var gr = Grupe(ZapisiTl(jobId).Values);
            var grupa = 1;
            // v2.9.2: spisak praznih kola uz posao natovarenih kola (povratak praznih – Zmajevo soy → Zmajevo–Kelebia) je grupa 2
            if (p.Grupe.Count == 1 && p.Grupe[0].prazna && gr.Count >= 1 && !gr[0][0].Prazna) grupa = 2;
            if (p.Grupe.Count == 1 && gr.Count > 1)
            {
                var kola = new HashSet<string>(p.Grupe[0].kola);
                var preklop = gr.Select((g, i) => (i, n: g.SelectMany(z => z.Kola).Count(kola.Contains))).OrderByDescending(x => x.n).First();
                // spisak praznih kola sa istim kolima kao natovarena grupa je povratak praznih – ne gazi natovarenu grupu
                if (preklop.n > 0 && !(p.Grupe[0].prazna && !gr[preklop.i][0].Prazna)) grupa = preklop.i + 1;
                else if (p.Grupe[0].prazna && gr.FindIndex(g => g[0].Prazna) is int ip && ip >= 0) grupa = ip + 1;
                grupa = Math.Min(grupa, 2);
            }
            if (grupa == 2)
            {
                foreach (var k in KoloneGrupe) { p.V.Remove(k + "2"); if (p.V.TryGetValue(k + "1", out var v)) { p.V[k + "2"] = v; p.V.Remove(k + "1"); } }
                foreach (var po in p.Poreklo) if (po.Kolona.EndsWith("1")) po.Kolona = po.Kolona.Substring(0, po.Kolona.Length - 1) + "2";
            }
            // v3.0 (Zmajevo): spisak jedne grupe ne briše grupu 2 – ni drugu grupu tovarnih listova ni povratak praznih (teretnica sa Grupa = 2)
            else if (p.Grupe.Count == 1 && (gr.Count > 1 || Skalar("SELECT TOP 1 ID FROM planiranje.tmpTeretnica WHERE JobID = @j AND Grupa = 2 AND ID <> @t", "@j", jobId, "@t", tid) != null))
                foreach (var k in KoloneGrupe) { p.V.Remove(k + "2"); p.Poreklo.RemoveAll(x => x.Kolona == k + "2"); }
            Izvrsi("UPDATE planiranje.tmpTeretnica SET Grupa = @g WHERE ID = @t", "@g", (byte)grupa, "@t", tid);
            var g0 = grupa.ToString();
            // v3.0 (0a.2): prazna kola uz natovaren posao (povratak praznih) – poreklo grupe 2; kola ne moraju da se poklapaju
            if (grupa == 2 && p.Grupe.Count == 1 && p.Grupe[0].prazna && gr.Count >= 1 && !gr[0][0].Prazna)
            {
                var opisPovratka = OpisPovratka(Skalar("SELECT Referenca FROM planiranje.tmpJobs WHERE ID = @j", "@j", jobId) as string,
                    gr[0].SelectMany(z => z.Kola), p.Grupe[0].kola);
                foreach (var po in p.Poreklo.Where(x => x.Kolona.EndsWith("2"))) po.Kandidati = (po.Kandidati.Length > 0 ? po.Kandidati + "; " : "") + opisPovratka;
                ishod.Napomene.Add(opisPovratka);
                Istorija(jobId, tid, null, null, null, dokId, "POVRATAK_PRAZNIH", opisPovratka);
            }

            // NHM sa spiska samo kada ga spisak ima; iste 4 cifre kao NHM posla ne menjaju podgrupu iz tovarnog lista
            if (!s.Kola.Any(k => k.Nhm.Length > 0)) { p.V.Remove("NHM1"); p.V.Remove("NHM2"); }
            string Nhm4Id(object id) => id is int i ? Poklapanje.Nhm4(sif.Nhm.FirstOrDefault(z => z.Id == i)?.Kod8) : "";
            foreach (var k in new[] { "NHM1", "NHM2" })
                if (p.V.TryGetValue(k, out var nv) && stari.TryGetValue(k, out var sv) && sv != null && Nhm4Id(nv) == Nhm4Id(sv)) p.V.Remove(k);
            // broj kola u tovarnom listu i na spisku: razlika u istoriju i poreklo
            var izCim = Skalar("SELECT Vrednost FROM planiranje.tmpJobsPolje WHERE JobID = @j AND TeretnicaID IS NULL AND Kolona = @k AND Rubrika IN ('r18/r21', 'tovarni listovi')", "@j", jobId, "@k", "BrojKola" + g0);
            var kolaCim = izCim != null && int.TryParse(Convert.ToString(izCim), out var bk) ? bk : 0;
            var kolaSpisak = s.Kola.Count;
            // v3.0 (0a.1, LDC8): spisak sa MANJE kola od tovarnog lista, ili sa kolima kojih u tovarnom listu nema, ne menja broj kola
            // ni mase posla – stavke ostaju kao posebna verzija (teretnica), uz INTERNO napomenu
            var tlGrupe = gr.Count >= grupa ? gr[grupa - 1] : new List<ZapisTl>();
            var kolaTl = new HashSet<string>(tlGrupe.SelectMany(z => z.Kola).Select(BrojKolaUic.Cifre).Where(x => x.Length > 0));
            var brojTl = Math.Max(kolaCim, tlGrupe.Count > 0 && tlGrupe.All(z => z.Kola.Count > 0) ? kolaTl.Count : tlGrupe.Sum(z => z.BrojKola));
            var tudjih = kolaTl.Count > 0 ? s.Kola.Count(k => !kolaTl.Contains(BrojKolaUic.Cifre(k.Broj))) : 0;
            // spisak sa više grupa (LDC8 „louis dreyfus“: 28 kola robe + 1 sa drugim NHM-om) – poredi se grupa kojoj spisak pripada
            var kolaZaProveru = p.Grupe.Count > 1 && p.Grupe.Count >= grupa ? p.Grupe[grupa - 1].kola.ToList() : s.Kola.Select(k => k.Broj).ToList();
            if (!SpisakMenjaPosao(kolaZaProveru, kolaTl, brojTl))
            {
                var t = SpisakNijeTovarniList(kolaZaProveru.Count, brojTl, tudjih);
                ishod.Napomene.Add(t); ishod.Interno.Add(t);
                Istorija(jobId, tid, null, null, null, dokId, razlog, t);
                Izvrsi("UPDATE planiranje.tmpTeretnica SET Napomena = LEFT(ISNULL(Napomena + ' | ', '') + @n, 1000) WHERE ID = @t", "@n", t, "@t", tid);
                return;
            }
            var nap = kolaCim > 0 && kolaCim != kolaSpisak ? "Broj kola u tovarnom listu (" + kolaCim + ") ne odgovara spisku kola (" + kolaSpisak + ")" : null;
            if (nap != null)
            {
                ishod.Napomene.Add(nap);
                var po = p.Poreklo.FirstOrDefault(x => x.Kolona == "BrojKola" + g0);
                if (po != null) po.Kandidati = nap;
            }
            // v2.9.2 (3a.2): masa sa spiska jednaka DRUGOJ masi iz tovarnog lista (Coal 16: „neto“ spiska 2 425,806 = bruto r25; Prahovo 05:
            // „tara“ spiska 924,4 = neto r25) – kolona spiska je pogrešno pročitana: ta vrednost se ne upisuje + „proveriti kolone spiska“
            if (gr.Count >= grupa)
            {
                double? TlZbir(Func<ZapisTl, double?> f) { var g = gr[grupa - 1]; return g.Any(z => f(z) > 0) ? g.Sum(z => f(z) ?? 0) : (double?)null; }
                var tlMase = new Dictionary<string, double?> { ["Neto"] = TlZbir(z => z.Neto), ["Tara"] = TlZbir(z => z.Tara), ["Bruto"] = TlZbir(z => z.Bruto) };
                var uklonjeno = new List<string>();
                foreach (var kol in new[] { "Neto", "Tara", "Bruto" })
                {
                    if (!(p.V.TryGetValue(kol + g0, out var sv0) && sv0 is decimal sd && sd > 0)) continue;
                    var kg = (double)sd * 1000;
                    bool Isto(double? a) => a > 0 && Math.Abs(a.Value - kg) <= 0.001 * kg;
                    var druga = tlMase.Where(kv => kv.Key != kol && Isto(kv.Value)).Select(kv => kv.Key).FirstOrDefault();
                    if (druga == null || Isto(tlMase[kol])) continue;
                    // v3.0 (LDC8): vrednost iz tovarnog lista (ako postoji) vraća se u posao – ispravlja i raniju pogrešnu kolonu istog spiska
                    if (tlMase[kol] > 0)
                    {
                        p.V[kol + g0] = PosaoIzDokumenata.Tone(tlMase[kol].Value);
                        var po = p.Poreklo.FirstOrDefault(x => x.Kolona == kol + g0);
                        if (po != null) { po.Vrednost = p.V[kol + g0]; po.Rubrika = "r25/r21"; po.Tekst = "tovarni list (kolona spiska pogrešno pročitana: " + (kg / 1000).ToString("N3", CultureInfo.GetCultureInfo("sr-Latn-RS")) + " t = " + druga.ToLowerInvariant() + ")"; }
                    }
                    else p.V.Remove(kol + g0);
                    uklonjeno.Add(kol.ToLowerInvariant() + " sa spiska " + (kg / 1000).ToString("N3", CultureInfo.GetCultureInfo("sr-Latn-RS")) + " t = " + druga.ToLowerInvariant() + " iz tovarnog lista");
                }
                if (uklonjeno.Count > 0)
                {
                    // vrednosti izračunate iz pogrešne kolone („bruto izračunat (neto + tara)“) se takođe ne upisuju
                    foreach (var po in p.Poreklo.Where(x => x.Kolona.EndsWith(g0) && (x.Tekst ?? "").Contains("izračunat")).ToList()) p.V.Remove(po.Kolona);
                    var t = "Spisak kola: proveriti kolone spiska – " + string.Join("; ", uklonjeno) + " (sa spiska nije upisano; ostaje vrednost iz tovarnog lista)";
                    p.Napomene.Add(t);
                    Istorija(jobId, tid, null, null, null, dokId, razlog, PodaciPosla.Skrati(t, 400));
                }
            }
            // neto r25 ↔ spisak > 1 % (5.1): interna napomena u poreklu i istoriji
            var r25 = gr.Count >= grupa ? gr[grupa - 1].Where(z => z.Neto.HasValue).Sum(z => z.Neto.Value) : 0;
            // v3.0.1 (3.2): spisak nije usklađen sam sa sobom – mase sa spiska nisu upisane (ostaju iz r25), INTERNO „spisak – proveriti kolone“
            if (p.SpisakNeuskladjen) foreach (var x in p.Napomene.Where(x => x.StartsWith("Spisak kola – proveriti kolone", StringComparison.Ordinal))) ishod.Interno.Add(x);
            // v3.0.1 (3.2, amonijak UID 259: spisak „neto 1 630,5“ bez tare i bruta, r25 1 071,7 / 838,25 / 1 909,95): spisak samo sa netom ne
            // može da se proveri sam sa sobom – kada r25 ima sve tri mase usklađene (bruto = neto + tara ± 0,5 %) i razlikuje se > 5 %, važi r25
            if (gr.Count >= grupa && p.V.TryGetValue("Neto" + g0, out var nspS) && nspS is decimal nsS && nsS > 0
                && !(p.V.TryGetValue("Tara" + g0, out var tsp) && tsp is decimal tsd && tsd > 0) && !(p.V.TryGetValue("Bruto" + g0, out var bsp) && bsp is decimal bsd && bsd > 0))
            {
                var g1 = gr[grupa - 1];
                double? Z(Func<ZapisTl, double?> f) => g1.All(z => f(z) > 0) ? g1.Sum(z => f(z).Value) : (double?)null;
                var (n25, t25, b25) = (Z(z => z.Neto), Z(z => z.Tara), Z(z => z.Bruto));
                if (n25 > 0 && t25 > 0 && b25 > 0 && Math.Abs(b25.Value - (n25.Value + t25.Value)) <= 0.005 * b25.Value && Math.Abs((double)nsS * 1000 - n25.Value) > 0.05 * n25.Value)
                {
                    p.V["Neto" + g0] = PosaoIzDokumenata.Tone(n25.Value);
                    var po = p.Poreklo.FirstOrDefault(x => x.Kolona == "Neto" + g0);
                    var sr0 = CultureInfo.GetCultureInfo("sr-Latn-RS");
                    var t = "Spisak kola – proveriti kolone spiska: spisak ima samo neto " + nsS.ToString("N3", sr0) + " t (bez tare i bruta), a r25 je usklađen (neto "
                            + (n25.Value / 1000).ToString("N3", sr0) + " + tara " + (t25.Value / 1000).ToString("N3", sr0) + " = bruto " + (b25.Value / 1000).ToString("N3", sr0) + " t) – upisan r25";
                    if (po != null) { po.Vrednost = p.V["Neto" + g0]; po.Rubrika = "r25/r21"; po.Tekst = t; }
                    ishod.Napomene.Add(t); ishod.Interno.Add(t);
                    Istorija(jobId, tid, null, null, null, dokId, razlog, PodaciPosla.Skrati(t, 400));
                }
            }
            // v3.0.1 (3.8): provera smisla – neto spiska < 1 % ili > 300 % od r25 → ne upisuje se
            if (p.V.TryGetValue("Neto" + g0, out var nsp0) && nsp0 is decimal ns0 && ns0 > 0
                && PosaoIzDokumenata.NesmislenNeto((double)ns0 * 1000, 0, p.Grupe.Count > 0 && p.Grupe[Math.Min(grupa, p.Grupe.Count) - 1].prazna, r25 > 0 ? r25 : (double?)null) is string bez)
            {
                p.V.Remove("Neto" + g0); p.Poreklo.RemoveAll(x => x.Kolona == "Neto" + g0);
                var t = "Spisak kola: neto nije upisan – " + bez;
                ishod.Napomene.Add(t); ishod.Interno.Add(t);
                Istorija(jobId, tid, null, null, null, dokId, razlog, t);
            }
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
