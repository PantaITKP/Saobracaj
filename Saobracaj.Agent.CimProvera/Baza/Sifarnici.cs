using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Saobracaj.Agent.CimProvera.Baza
{
    internal sealed class StanicaZapis
    {
        public int Id; public string Opis = ""; public bool Granicna; public string Kod = ""; public string Drzava = "";
        internal string Norm; internal string KodCifre;
    }

    internal sealed class PartnerZapis
    {
        public int PaSifra; public string PaNaziv = ""; public string PaDoNaziv = ""; public string PaKraj = ""; public string Drzava = "";
        public string Uic = ""; public bool Prevoznik; public bool Posiljalac; public bool Primalac;
        internal string Norm1; internal string Norm2; internal HashSet<string> Reci1 = new HashSet<string>(); internal HashSet<string> Reci2 = new HashSet<string>();
    }

    /// <summary>v2.9.1: red šifarnika planiranje.tmpNHM (tarifa Srbija Kargo, Deo 2 – Klasifikacija robe).</summary>
    internal sealed class NhmZapis
    {
        public int Id; public string Kod8 = ""; public int Nivo = 8; public string Naziv = ""; public string NazivNorm = "";
        public string Kod6 => Kod8.Length >= 6 ? Kod8.Substring(0, 6) : Kod8;
        public string Kod4 => Kod8.Length >= 4 ? Kod8.Substring(0, 4) : Kod8;
        /// <summary>„2520 10 00“.</summary>
        public string Prikaz => Kod8.Length == 8 ? Kod8.Substring(0, 4) + " " + Kod8.Substring(4, 2) + " " + Kod8.Substring(6, 2) : Kod8;
        internal HashSet<string> Reci;
    }

    internal sealed class SinonimZapis { public string Tip = ""; public string Tekst = ""; public int CiljniId; }

    /// <summary>
    /// v2.9.0: šifarnici za prevod teksta iz dokumenata u šifre – dbo.stanice, dbo.Partnerji, dbo.NHM, planiranje.tmpSinonimi i
    /// učestalost stanica u postojećim planiranje.OP_Jobs i dbo.TeretnicaStavke (samo čitanje; za izbor između duplikata u šifarniku).
    /// Učitavaju se jednom po prolazu agenta.
    /// </summary>
    internal sealed class Sifarnici
    {
        public List<StanicaZapis> Stanice = new List<StanicaZapis>();
        public List<PartnerZapis> Partneri = new List<PartnerZapis>();
        public List<NhmZapis> Nhm = new List<NhmZapis>();
        public List<SinonimZapis> Sinonimi = new List<SinonimZapis>();
        /// <summary>Koliko puta se ID stanice koristi u postojećim OP_Jobs i TeretnicaStavke.</summary>
        public Dictionary<int, int> UpotrebaStanica = new Dictionary<int, int>();

        /// <summary>Priprema normalizovanih naziva (posle punjenja listi, i u testovima).</summary>
        public Sifarnici Pripremi()
        {
            foreach (var s in Stanice) { s.Norm = Slicnost.Stanica(s.Opis); s.KodCifre = Regex.Replace(s.Kod ?? "", @"\D", ""); s.Drzava = (s.Drzava ?? "").Trim().ToUpperInvariant(); }
            foreach (var p in Partneri)
            {
                p.Norm1 = Slicnost.Partner(p.PaNaziv, p.PaKraj); p.Norm2 = Slicnost.Partner(p.PaDoNaziv, p.PaKraj);
                p.Drzava = (p.Drzava ?? "").Trim().ToUpperInvariant(); p.Uic = Regex.Replace(p.Uic ?? "", @"\D", "");
                p.Reci1 = new HashSet<string>(Slicnost.Osnovno(p.PaNaziv).Split(' ')); p.Reci2 = new HashSet<string>(Slicnost.Osnovno(p.PaDoNaziv).Split(' '));
            }
            foreach (var n in Nhm)
            {
                n.Kod8 = Regex.Replace(n.Kod8 ?? "", @"\D", "");
                if (string.IsNullOrEmpty(n.NazivNorm)) n.NazivNorm = Slicnost.Osnovno(n.Naziv).ToLowerInvariant();
                n.Reci = new HashSet<string>(Slicnost.Osnovno(n.NazivNorm).Split(' ').Where(w => w.Length >= 3));
            }
            return this;
        }

        /// <summary>v2.9.1: naziv tmp tabele sa zadatim prefiksom („planiranje.tmp“; za probe „planiranje.tmpProba“).</summary>
        internal static string Tmp(string sql, string prefiks) =>
            string.IsNullOrEmpty(prefiks) || prefiks == UpisUBazu.PodrazumevaniPrefiks ? sql : sql.Replace(UpisUBazu.PodrazumevaniPrefiks, prefiks);

        public static Sifarnici Ucitaj(SqlConnection c, Log log, SqlTransaction tx = null, string prefiks = null)
        {
            var s = new Sifarnici();
            using (var cmd = new SqlCommand("SELECT ID, Opis, ISNULL(Granicna, 0), ISNULL(Kod, ''), ISNULL(Drzava, '') FROM dbo.stanice", c, tx))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) s.Stanice.Add(new StanicaZapis { Id = r.GetInt32(0), Opis = r.GetString(1).Trim(), Granicna = Convert.ToInt32(r.GetValue(2)) == 1, Kod = r.GetString(3).Trim(), Drzava = r.GetString(4).Trim() });
            using (var cmd = new SqlCommand("SELECT PaSifra, ISNULL(PaNaziv, ''), ISNULL(PaDoNaziv, ''), ISNULL(PaKraj, ''), ISNULL(PaSifDrzave, ''), ISNULL(UIC, ''), ISNULL(Prevoznik, 0), ISNULL(Posiljalac, 0), ISNULL(Primalac, 0) FROM dbo.Partnerji", c, tx))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                    s.Partneri.Add(new PartnerZapis
                    {
                        PaSifra = r.GetInt32(0), PaNaziv = r.GetString(1).Trim(), PaDoNaziv = r.GetString(2).Trim(), PaKraj = r.GetString(3).Trim(), Drzava = r.GetString(4).Trim(),
                        Uic = r.GetString(5).Trim(), Prevoznik = Convert.ToInt32(r.GetValue(6)) == 1, Posiljalac = Convert.ToInt32(r.GetValue(7)) == 1, Primalac = Convert.ToInt32(r.GetValue(8)) == 1,
                    });
            // v2.9.1: NHM iz tarife (planiranje.tmpNHM) umesto dbo.NHM; bez tabele/prava NHM ostaje NULL (upozorenje u logu)
            try
            {
                using (var cmd = new SqlCommand(Tmp("SELECT ID, Kod8, Nivo, ISNULL(NazivLat, ''), ISNULL(NazivNorm, '') FROM planiranje.tmpNHM", prefiks), c, tx))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) s.Nhm.Add(new NhmZapis { Id = r.GetInt32(0), Kod8 = r.GetString(1).Trim(), Nivo = Convert.ToInt32(r.GetValue(2)), Naziv = r.GetString(3).Trim(), NazivNorm = r.GetString(4).Trim() });
                if (s.Nhm.Count == 0) log?.Upozorenje("  upis u bazu: planiranje.tmpNHM je prazna – NHM se ne upisuje (pokrenuti OP_917_tmp_NHM_Podaci.sql).");
            }
            catch (SqlException ex) { log?.Upozorenje("  upis u bazu: planiranje.tmpNHM nije pročitana (" + ex.Message + ") – NHM se ne upisuje (OP_916/OP_917/OP_913)."); }
            using (var cmd = new SqlCommand(Tmp("SELECT Tip, TekstNormalizovan, CiljniID FROM planiranje.tmpSinonimi", prefiks), c, tx))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) s.Sinonimi.Add(new SinonimZapis { Tip = r.GetString(0), Tekst = r.GetString(1), CiljniId = r.GetInt32(2) });
            // učestalost: samo čitanje postojećih tabela; bez prava (OP_913) se preskače – duplikati se tada biraju po šifri i ID-u
            try
            {
                const string sql = @"SELECT ID, COUNT(*) FROM (
    SELECT OtpravnaID AS ID FROM planiranje.OP_Jobs UNION ALL SELECT UputnaID FROM planiranje.OP_Jobs UNION ALL
    SELECT Stanica1ID FROM planiranje.OP_Jobs UNION ALL SELECT Stanica3ID FROM planiranje.OP_Jobs UNION ALL
    SELECT Otpravna FROM dbo.TeretnicaStavke UNION ALL SELECT Uputna FROM dbo.TeretnicaStavke UNION ALL
    SELECT Uvozna FROM dbo.TeretnicaStavke UNION ALL SELECT Izvozna FROM dbo.TeretnicaStavke) x
WHERE ID IS NOT NULL GROUP BY ID";
                using (var cmd = new SqlCommand(sql, c, tx) { CommandTimeout = 120 })
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) s.UpotrebaStanica[r.GetInt32(0)] = r.GetInt32(1);
            }
            catch (SqlException ex) { log?.Upozorenje("  upis u bazu: učestalost stanica nije pročitana (" + ex.Message + ") – duplikati se biraju po šifri i ID-u."); }
            return s.Pripremi();
        }
    }

    /// <summary>Rezultat prevoda teksta u šifru: ID (null = nije pronađeno), pouzdanost 0–100, tekst i kandidati za poreklo polja.</summary>
    internal sealed class Pogodak
    {
        public int? Id;
        public int Pouzdanost;
        public string Tekst = "";
        public string Kandidati = "";
        public string Napomena = "";
        public override string ToString() => (Id?.ToString() ?? "NULL") + " (" + Pouzdanost + ") " + Napomena;
    }

    /// <summary>v2.9.0: prevod stanica, partnera i NHM-a iz dokumenta u šifre šifarnika (tačka 2 zahteva).</summary>
    internal sealed class Poklapanje
    {
        public const double PragPartnera = 0.85;
        public const double PragStanice = 0.88;
        /// <summary>Partner: kada je drugi kandidat bliži od ovoga prvom, ne upisuje se nijedan (NULL i kandidati).</summary>
        public const double RazlikaKandidata = 0.05;
        private const double Blizu = 0.03;
        private readonly Sifarnici s;

        public Poklapanje(Sifarnici s) { this.s = s; }

        /// <summary>UIC šifra zemlje (UIC 920-14) → oznaka države u dbo.stanice / dbo.Partnerji.</summary>
        internal static readonly Dictionary<string, string[]> UicZemlje = new Dictionary<string, string[]>
        {
            ["72"] = new[] { "SRB" }, ["78"] = new[] { "HR" }, ["79"] = new[] { "SI" }, ["55"] = new[] { "HU" }, ["53"] = new[] { "RO" }, ["52"] = new[] { "BG" },
            ["83"] = new[] { "IT" }, ["81"] = new[] { "AT", "AU" }, ["80"] = new[] { "DE" }, ["54"] = new[] { "CZ" }, ["56"] = new[] { "SK" }, ["51"] = new[] { "PL" },
            ["44"] = new[] { "BA" }, ["50"] = new[] { "BA" }, ["62"] = new[] { "MNE" }, ["65"] = new[] { "MK" }, ["75"] = new[] { "TR" }, ["87"] = new[] { "FR" },
            ["85"] = new[] { "CH" }, ["84"] = new[] { "NL" }, ["88"] = new[] { "BE" }, ["73"] = new[] { "GR" }, ["41"] = new[] { "AL" }, ["22"] = new[] { "UA" },
        };

        /// <summary>Države iz teksta zemlje („72“, „Serbia“, „IT“, „ITALY“).</summary>
        internal static string[] Drzave(string zemlja)
        {
            var z = Slicnost.Osnovno(zemlja);
            if (z.Length == 0) return new string[0];
            var d = Regex.Match(z, @"^\d{2}").Value;
            if (d.Length == 2 && UicZemlje.TryGetValue(d, out var u)) return u;
            var mapa = new Dictionary<string, string>
            {
                ["RS"] = "SRB", ["SRB"] = "SRB", ["SRBIJA"] = "SRB", ["SERBIA"] = "SRB", ["SERBIEN"] = "SRB", ["IT"] = "IT", ["ITALY"] = "IT", ["ITALIA"] = "IT", ["ITALIJA"] = "IT",
                ["HR"] = "HR", ["CROATIA"] = "HR", ["HRVATSKA"] = "HR", ["HU"] = "HU", ["HUNGARY"] = "HU", ["MADJARSKA"] = "HU", ["RO"] = "RO", ["ROMANIA"] = "RO", ["RUMUNIJA"] = "RO",
                ["BG"] = "BG", ["BULGARIA"] = "BG", ["BUGARSKA"] = "BG", ["SI"] = "SI", ["SLOVENIA"] = "SI", ["SLOVENIJA"] = "SI", ["AT"] = "AT", ["AUSTRIA"] = "AT", ["DE"] = "DE",
                ["GERMANY"] = "DE", ["SK"] = "SK", ["SLOVAKIA"] = "SK", ["CZ"] = "CZ", ["BA"] = "BA", ["BIH"] = "BA", ["ME"] = "MNE", ["MNE"] = "MNE", ["MK"] = "MK", ["TR"] = "TR", ["TURKEY"] = "TR", ["PL"] = "PL",
            };
            foreach (var w in z.Split(' ').AsEnumerable().Reverse())
                if (mapa.TryGetValue(w, out var x)) return x == "AT" ? new[] { "AT", "AU" } : new[] { x };
            return new string[0];
        }

        // ------------------------------------------------------------------------------------------- stanice

        /// <summary>Delovi teksta stanice: ceo tekst, delovi razdvojeni sa „/“, novim redom ili „ - “ („ŠID/TOVARNIK“ → ŠID, TOVARNIK).</summary>
        private static IEnumerable<string> Varijante(string tekst)
        {
            var t = (tekst ?? "").Trim();
            if (t.Length == 0) yield break;
            yield return t;
            foreach (var d in Regex.Split(t, @"\s*(?:/|\r?\n|\s-\s|\s–\s|;)\s*")) if (d.Trim().Length >= 2 && d.Trim() != t) yield return d.Trim();
        }

        /// <summary>Šifra stanice iz dokumenta („83 03200-3“, „83032003“, „72 16509-2“) se poklapa sa Kod iz šifarnika (razni zapisi).</summary>
        internal static bool KodPoklapa(string sifraDokumenta, string kodCifre)
        {
            var d = Regex.Replace(sifraDokumenta ?? "", @"\D", "");
            if (d.Length < 5 || string.IsNullOrEmpty(kodCifre) || kodCifre.Length < 5) return false;
            var zemlja = d.Length >= 7 ? d.Substring(0, 2) : "";
            var st = d.Length >= 7 ? d.Substring(2) : d;              // 5 cifara + kontrolna
            var st5 = st.Length >= 5 ? st.Substring(0, 5) : st;
            return kodCifre == d || kodCifre == st || kodCifre == st5 || kodCifre == zemlja + st5 || kodCifre == zemlja + st
                   || (kodCifre.Length >= 7 && kodCifre.EndsWith(st, StringComparison.Ordinal))
                   // v2.9.1: šifra bez kontrolne cifre („72 14305“) i Kod sa kontrolnom cifrom („143057“)
                   || (st.Length == 5 && kodCifre.Length == 6 && kodCifre.StartsWith(st, StringComparison.Ordinal));
        }

        private string ImeStanice(int id) => s.Stanice.FirstOrDefault(x => x.Id == id)?.Opis ?? id.ToString();

        /// <summary>v2.9.1: stanice iz šifarnika sa istom šifrom (npr. 1380 Wlasna i 1385 Kwidzyn – obe 51023119).</summary>
        public List<int> StaniceSaKodom(string sifra) => s.Stanice.Where(x => KodPoklapa(sifra, x.KodCifre)).Select(x => x.Id).ToList();

        /// <summary>Država stanice iz šifarnika („SRB“, „IT“…), prazno ako nije upisana.</summary>
        public string DrzavaStanice(int id) => s.Stanice.FirstOrDefault(x => x.Id == id)?.Drzava ?? "";

        /// <summary>Granična stanica: „X gr.“ po nazivu ili oznaka Granicna.</summary>
        public bool JeGranicna(int id) => s.Stanice.FirstOrDefault(x => x.Id == id) is StanicaZapis z && (z.Granicna || Regex.IsMatch(z.Norm ?? "", @" GR$"));

        /// <summary>
        /// Stanica po nazivu (tačka 2.1): najbliži naziv (prag 0,88); kod skoro jednakih (razlika &lt; 0,03) prednost ima država iz
        /// dokumenta, pa (za isti naziv – duplikati) stanica koja se najčešće koristi u OP_Jobs/TeretnicaStavke, pa šifra stanice iz
        /// dokumenta, pa manji ID. <paramref name="granicna"/>: ako za stanicu postoji „X gr.“, upisuje se ona (Granicna samo kod izbora).
        /// </summary>
        public Pogodak Stanica(string tekst, string sifra = null, string zemlja = null, bool granicna = false, string[] drzavePrednost = null, string[] samoDrzave = null)
        {
            var p = new Pogodak { Tekst = (tekst ?? "").Trim() };
            bool Dozvoljena(StanicaZapis z) => samoDrzave == null || samoDrzave.Length == 0 || z.Drzava.Length == 0 || samoDrzave.Contains(z.Drzava);
            var drzave = Drzave(zemlja).Concat(Drzave(sifra)).Concat(drzavePrednost ?? new string[0]).Distinct().ToList();
            var ocene = new Dictionary<int, double>();
            var normalizovani = Varijante(tekst).Select(Slicnost.Stanica).Where(n => n.Length >= 2).Distinct().ToList();
            OceniVarijante(normalizovani, ocene, Dozvoljena);
            // OCR šum oko naziva („P PRE PANCEVO GLAVNA“, „} PORTOGRUARO CAORLE ITALY“): nizovi od 1–4 uzastopne reči
            if (ocene.Count == 0 || ocene.Values.Max() < PragStanice)
            {
                var prozori = new List<string>();
                foreach (var n in normalizovani)
                {
                    var w = n.Split(' ');
                    for (int duz = Math.Min(4, w.Length - 1); duz >= 1; duz--)
                        for (int i = 0; i + duz <= w.Length; i++)
                        {
                            var deo = string.Join(" ", w.Skip(i).Take(duz));
                            if (deo.Replace(" ", "").Length >= 4 && Regex.IsMatch(deo, @"[A-Z]{3}")) prozori.Add(deo);
                        }
                }
                OceniVarijante(prozori.Distinct().ToList(), ocene, Dozvoljena);
            }
            ZavrsiStanicu(p, tekst, sifra, drzave, ocene, granicna, Dozvoljena);
            return p;
        }

        private void OceniVarijante(List<string> varijante, Dictionary<int, double> ocene, Func<StanicaZapis, bool> dozvoljena)
        {
            foreach (var n in varijante)
            {
                var sin = s.Sinonimi.FirstOrDefault(x => x.Tip == "STANICA" && x.Tekst == n);
                if (sin != null && s.Stanice.Any(x => x.Id == sin.CiljniId)) ocene[sin.CiljniId] = 1.01;
                foreach (var st in s.Stanice)
                {
                    if (!dozvoljena(st)) continue;
                    var o = Slicnost.Ocena(n, st.Norm);
                    if (o > 0.5 && (!ocene.TryGetValue(st.Id, out var b) || o > b)) ocene[st.Id] = o;
                }
            }
        }

        /// <summary>v2.9.1: SRB kodovi stanica iz dokumenata kojih nema u šifarniku (kod → naziv iz dokumenta, predlog ID) – CSV predlog za korisnika.</summary>
        public readonly SortedDictionary<string, string> KodoviBezSifre = new SortedDictionary<string, string>(StringComparer.Ordinal);

        private void ZavrsiStanicu(Pogodak p, string tekst, string sifra, List<string> drzave, Dictionary<int, double> ocene, bool granicna, Func<StanicaZapis, bool> dozvoljena)
        {
            try { ZavrsiStanicu0(p, tekst, sifra, drzave, ocene, granicna, dozvoljena); }
            finally
            {
                var d = Regex.Replace(sifra ?? "", @"\D", "");
                // SRB šifra: „72“ + stanica, ili 5–6 cifara kada je dokument/stanica iz Srbije (strane šifre bez zemlje se ne prijavljuju)
                // „21001-3“ (5 cifara, crtica, kontrolna cifra) je oblik šifre stanice ŽS/KP i bez oznake zemlje
                var izSrbije = drzave.Contains("SRB") || (p.Id.HasValue && DrzavaStanice(p.Id.Value) == "SRB") || Regex.IsMatch(sifra ?? "", @"^\s*(?:72\s*)?\d{5}\s*-\s*\d\s*$");
                var srb = d.Length >= 7 && d.StartsWith("72") ? d.Substring(2) : (d.Length == 6 || d.Length == 5) && izSrbije ? d : "";
                if (srb.Length >= 5 && !s.Stanice.Any(x => x.Drzava == "SRB" && KodPoklapa("72" + srb, x.KodCifre)) && !KodoviBezSifre.ContainsKey(srb))
                    KodoviBezSifre[srb] = Regex.Replace(tekst ?? "", @"\s+", " ").Trim() + ";" + (p.Id.HasValue && DrzavaStanice(p.Id.Value) == "SRB" ? p.Id + " " + ImeStanice(p.Id.Value) : "");
            }
        }

        private void ZavrsiStanicu0(Pogodak p, string tekst, string sifra, List<string> drzave, Dictionary<int, double> ocene, bool granicna, Func<StanicaZapis, bool> dozvoljena)
        {
            // šifra iz dokumenta bez naziva (ili OCR naziv): stanica sa istom šifrom je kandidat
            if (!string.IsNullOrWhiteSpace(sifra))
                foreach (var st in s.Stanice.Where(x => dozvoljena(x) && KodPoklapa(sifra, x.KodCifre)))
                    if (!ocene.ContainsKey(st.Id) || ocene[st.Id] < 0.9) ocene[st.Id] = Math.Max(ocene.TryGetValue(st.Id, out var b) ? b : 0, 0.9);
            if (ocene.Count == 0) { p.Napomena = "stanica nije pronađena"; return; }
            var lista = ocene.Select(kv => (st: s.Stanice.First(x => x.Id == kv.Key), o: kv.Value)).OrderByDescending(x => x.o).ToList();
            var najbolja = lista[0].o;
            var bliski = lista.Where(x => x.o >= najbolja - Blizu).ToList();
            int Upotreba(StanicaZapis z) => s.UpotrebaStanica.TryGetValue(z.Id, out var u) ? u : 0;
            var izbor = bliski
                .OrderByDescending(x => drzave.Count > 0 && drzave.Contains(x.st.Drzava))
                .ThenByDescending(x => x.o >= najbolja - 0.0001)
                .ThenByDescending(x => Upotreba(x.st))
                .ThenByDescending(x => sifra != null && KodPoklapa(sifra, x.st.KodCifre))
                .ThenBy(x => x.st.Id).First();
            p.Kandidati = string.Join("; ", lista.Take(3).Select(x => x.st.Id + " " + x.st.Opis + (x.st.Drzava.Length > 0 ? " (" + x.st.Drzava + ")" : "") + " " + x.o.ToString("0.00", CultureInfo.InvariantCulture)));
            if (izbor.o < PragStanice) { p.Napomena = "najbliži naziv ispod praga " + PragStanice.ToString("0.00", CultureInfo.InvariantCulture); p.Pouzdanost = (int)Math.Round(izbor.o * 100); return; }
            var dup = bliski.Where(x => x.st.Norm == izbor.st.Norm && x.st.Id != izbor.st.Id).ToList();
            if (dup.Count > 0) p.Napomena = "duplikat u šifarniku: izabran " + izbor.st.Id + " (upotreba " + Upotreba(izbor.st) + "), ostali: " + string.Join(", ", dup.Select(x => x.st.Id + " (upotreba " + Upotreba(x.st) + ")"));
            var st0 = izbor.st;
            // sinonim važi i za izabranu stanicu: naziv izabrane stanice je u tmpSinonimi („Pančevo“ 1464 → 246) – i kada OCR šum
            // („PANČEVO jr“) sprečava tačno poklapanje teksta
            var preusmeren = s.Sinonimi.FirstOrDefault(x => x.Tip == "STANICA" && x.Tekst == st0.Norm && x.CiljniId != st0.Id);
            if (preusmeren != null && s.Stanice.FirstOrDefault(x => x.Id == preusmeren.CiljniId) is StanicaZapis cilj)
            {
                p.Napomena = (p.Napomena.Length > 0 ? p.Napomena + "; " : "") + "sinonim „" + preusmeren.Tekst + "“: " + st0.Id + " → " + cilj.Id;
                st0 = cilj;
            }
            p.Id = st0.Id; p.Pouzdanost = (int)Math.Round(Math.Min(1, izbor.o) * 100);
            if (granicna && !Regex.IsMatch(st0.Norm, @" GR$"))
            {
                var gr = s.Stanice.Where(x => x.Norm == st0.Norm + " GR")
                    .OrderByDescending(x => x.Granicna).ThenByDescending(x => x.Drzava == st0.Drzava).ThenByDescending(Upotreba).ThenBy(x => x.Id).FirstOrDefault();
                if (gr != null)
                {
                    p.Id = gr.Id;
                    p.Napomena = (p.Napomena.Length > 0 ? p.Napomena + "; " : "") + "granična varijanta „" + gr.Opis + "“ za " + st0.Id + " " + st0.Opis;
                }
            }
        }

        // ------------------------------------------------------------------------------------------- partneri

        /// <summary>
        /// v2.9.1: partneri koji nisu nađeni ili su nađeni preko šifre/skraćenice bez UIC-a ili oznake Prevoznik – za dnevnu internu listu
        /// („nije nađen u Partnerji: UIC …, naziv …“, „partner postoji, ali nema UIC / oznaku Prevoznik: PaSifra …“).
        /// </summary>
        public readonly SortedSet<string> ZaSifarnik = new SortedSet<string>(StringComparer.Ordinal);

        private static readonly HashSet<string> NisuSkracenice = new HashSet<string>
        {
            "KP", "SRB", "RS", "DOO", "AD", "SRO", "KFT", "ZRT", "GMBH", "SRL", "SPA", "LTD", "THE", "AND", "FOR", "RAIL", "CARGO", "VIA",
        };

        /// <summary>Skraćenice prevoznika iz teksta r57/r13 („3277 CRW“ → CRW): reči od 3–6 velikih slova.</summary>
        internal static List<string> Skracenice(string tekst) =>
            Regex.Matches(tekst ?? "", @"(?<![\p{L}\d])\p{Lu}{3,6}(?![\p{L}\d])").Cast<Match>().Select(m => Slicnost.Osnovno(m.Value))
                .Where(x => x.Length >= 3 && !NisuSkracenice.Contains(x)).Distinct().ToList();

        /// <summary>
        /// Partner po nazivu → PaSifra. Redosled (v2.9.1): (1) tmpSinonimi; za prevoznika: (2) tačan UIC među Prevoznik = 1, (3) tačan UIC
        /// među svim partnerima (napomena „oznaka Prevoznik nije postavljena“), (4) skraćenica iz r57/r13 kao posebna reč naziva
        /// (PaNaziv, PaDoNaziv), prednost Prevoznik = 1 i zemlja deonice – jedan kandidat → pouzdanost 90, više → NULL i kandidati;
        /// (5) sličnost naziva ≥ 0,85. Dva bliska kandidata (razlika &lt; 0,05) → NULL, osim kada samo jedan odgovara zemlji iz dokumenta
        /// ili ulozi (Prevoznik / Posiljalac / Primalac) – filter bira između kandidata, ne isključuje ostale.
        /// </summary>
        public Pogodak Partner(string naziv, string uic = null, string[] drzave = null, string uloga = null)
        {
            var p = new Pogodak { Tekst = (naziv ?? "").Trim() };
            var n = Slicnost.Partner(naziv);
            var u = Regex.Replace(uic ?? "", @"\D", "");
            var dr = (drzave ?? new string[0]).Concat(Slicnost.DrzaveIzPravnogOblika(naziv)).Distinct().ToList();
            if (n.Length > 0)
            {
                var sin = s.Sinonimi.FirstOrDefault(x => x.Tip == "PARTNER" && x.Tekst == n && s.Partneri.Any(z => z.PaSifra == x.CiljniId));
                if (sin != null) { p.Id = sin.CiljniId; p.Pouzdanost = 100; p.Napomena = "sinonim (tmpSinonimi)"; return p; }
            }
            bool Uloga(PartnerZapis z) => uloga == "prevoznik" ? z.Prevoznik : uloga == "posiljalac" ? z.Posiljalac : uloga == "primalac" ? z.Primalac : true;
            double Ocena(PartnerZapis z) => Math.Max(Slicnost.Ocena(n, z.Norm1), Slicnost.Ocena(n, z.Norm2));
            string Opis(PartnerZapis z, double o) => z.PaSifra + " " + z.PaNaziv + (z.Drzava.Length > 0 ? " (" + z.Drzava + ")" : "") + (z.Uic.Length > 0 ? " UIC " + z.Uic : "") + " " + o.ToString("0.00", CultureInfo.InvariantCulture);
            string Oznaka(PartnerZapis z) => z.PaSifra + " " + z.PaNaziv;
            if (uloga == "prevoznik" && u.Length == 4)
            {
                var poUic = s.Partneri.Where(z => z.Prevoznik && z.Uic.TrimStart('0') == u.TrimStart('0')).ToList();
                var bezOznake = false;
                if (poUic.Count == 0) { poUic = s.Partneri.Where(z => z.Uic.Length > 0 && z.Uic.TrimStart('0') == u.TrimStart('0')).ToList(); bezOznake = poUic.Count > 0; }
                if (poUic.Count > 0)
                {
                    var ocene = poUic.Select(z => (z, o: n.Length > 0 ? Ocena(z) : 0)).OrderByDescending(x => x.o).ThenByDescending(x => dr.Contains(x.z.Drzava)).ThenBy(x => x.z.PaSifra).ToList();
                    var z0 = ocene[0];
                    // ista UIC šifra kod više partnera, a nazivi ne razlikuju jednog (razlika < 0,05): NULL i kandidati (odluka 14.11.)
                    if (ocene.Count > 1 && ocene[0].o - ocene[1].o < RazlikaKandidata)
                    {
                        p.Kandidati = string.Join("; ", ocene.Take(3).Select(x => Opis(x.z, x.o)));
                        p.Napomena = "UIC " + u + ": " + poUic.Count + " partnera sa istom šifrom, nazivi bliski (razlika < " + RazlikaKandidata.ToString("0.00", CultureInfo.InvariantCulture) + ") – nije upisano";
                        p.Pouzdanost = (int)Math.Round(80 * z0.o);
                        return p;
                    }
                    p.Id = z0.z.PaSifra; p.Pouzdanost = poUic.Count == 1 ? 95 : (int)Math.Round(80 + 15 * z0.o);
                    p.Kandidati = string.Join("; ", ocene.Take(3).Select(x => Opis(x.z, x.o)));
                    p.Napomena = "UIC " + u + (poUic.Count > 1 ? " (" + poUic.Count + " partnera sa istom šifrom, izbor po nazivu)" : "") + (n.Length > 0 && z0.o < 0.5 ? "; naziv iz dokumenta se ne poklapa (" + z0.o.ToString("0.00", CultureInfo.InvariantCulture) + ")" : "")
                                 + (bezOznake ? "; oznaka Prevoznik nije postavljena" : "");
                    if (bezOznake) ZaSifarnik.Add("partner postoji, ali nema oznaku Prevoznik: PaSifra " + Oznaka(z0.z) + " (UIC " + u + ")");
                    return p;
                }
                p.Napomena = "UIC " + u + " nije u šifarniku; ";
            }
            // skraćenica prevoznika („3277 CRW“ → „CENTRAL RAILWAYS A.S. CRW“): posebna reč naziva – odmah kada je naziv samo skraćenica,
            // inače tek kada naziv ne prolazi prag („CER Hungary Zrt.“ se traži po nazivu, ne po „CER“)
            var samoSkracenica = Slicnost.Osnovno(naziv).Split(' ').Where(w => w.Length > 0).ToList() is var tok && tok.Count > 0 && tok.Count <= 2 && tok.All(w => w.Length <= 6);
            Pogodak PoSkracenici()
            {
                if (uloga != "prevoznik") return null;
                foreach (var sk in Skracenice(naziv))
                {
                    var kand = s.Partneri.Where(z => z.Reci1.Contains(sk) || z.Reci2.Contains(sk)).ToList();
                    if (kand.Count == 0) continue;
                    var uz = kand.Where(z => z.Prevoznik).ToList(); if (uz.Count > 0) kand = uz;
                    if (dr.Count > 0) { var zz = kand.Where(z => dr.Contains(z.Drzava)).ToList(); if (zz.Count > 0) kand = zz; }
                    p.Kandidati = string.Join("; ", kand.Take(3).Select(z => Opis(z, 1)));
                    if (kand.Count > 1) { p.Napomena += "skraćenica „" + sk + "“: " + kand.Count + " partnera – nije upisano"; p.Pouzdanost = 50; return p; }
                    var z1 = kand[0];
                    p.Id = z1.PaSifra; p.Pouzdanost = 90;
                    p.Napomena += "skraćenica „" + sk + "“ u nazivu partnera";
                    if (u.Length == 4 || !z1.Prevoznik)
                        ZaSifarnik.Add("partner postoji, ali nema " + (u.Length == 4 && z1.Uic.Length == 0 ? "UIC " + u : "") + (u.Length == 4 && z1.Uic.Length == 0 && !z1.Prevoznik ? " ni " : "") + (!z1.Prevoznik ? "oznaku Prevoznik" : "") + ": PaSifra " + Oznaka(z1));
                    return p;
                }
                return null;
            }
            if (samoSkracenica && PoSkracenici() is Pogodak ps) return ps;
            if (n.Length < 2)
            {
                p.Napomena += "naziv nije pročitan";
                if (uloga == "prevoznik" && u.Length == 4) ZaSifarnik.Add("nije nađen u Partnerji: UIC " + u + ", naziv " + (p.Tekst.Length > 0 ? p.Tekst : "–"));
                return p;
            }
            var lista = s.Partneri.Select(z => (z, o: Ocena(z))).Where(x => x.o > 0.5)
                .OrderByDescending(x => x.o).ThenByDescending(x => Uloga(x.z)).ThenByDescending(x => dr.Contains(x.z.Drzava)).ToList();
            if ((lista.Count == 0 || lista[0].o < PragPartnera) && !samoSkracenica && PoSkracenici() is Pogodak ps2) return ps2;
            if (lista.Count == 0 || lista[0].o < PragPartnera)
            {
                if (uloga == "prevoznik") ZaSifarnik.Add("nije nađen u Partnerji: " + (u.Length == 4 ? "UIC " + u + ", " : "") + "naziv " + p.Tekst);
                if (lista.Count == 0) { p.Napomena += "partner nije pronađen"; return p; }
            }
            var izbor = lista[0];
            p.Kandidati = string.Join("; ", lista.Take(3).Select(x => Opis(x.z, x.o)));
            if (izbor.o < PragPartnera) { p.Napomena += "najbliži naziv ispod praga " + PragPartnera.ToString("0.00", CultureInfo.InvariantCulture); p.Pouzdanost = (int)Math.Round(izbor.o * 100); return p; }
            // dva bliska kandidata (razlika < 0,05): NULL i kandidati – pogrešan partner je gori od praznog (odluka 14.11.; izuzetak su
            // nazivi iz tmpSinonimi); v2.9.1: ako samo jedan od bliskih odgovara zemlji iz dokumenta (ili ulozi), bira se on
            var bliski = lista.Where(x => izbor.o - x.o < RazlikaKandidata).ToList();
            if (bliski.Count > 1)
            {
                var drDok = (drzave ?? new string[0]).ToList();   // samo zemlja navedena u dokumentu (pravni oblik nije dovoljan)
                var poZemlji = drDok.Count > 0 ? bliski.Where(x => drDok.Contains(x.z.Drzava)).ToList() : bliski;
                var poUlozi = poZemlji.Where(x => Uloga(x.z)).ToList();
                var jedan = poZemlji.Count == 1 ? poZemlji[0] : poUlozi.Count == 1 && poZemlji.Count > 1 ? poUlozi[0] : ((PartnerZapis z, double o)?)null;
                if (jedan != null && jedan.Value.o >= PragPartnera)
                {
                    p.Id = jedan.Value.z.PaSifra; p.Pouzdanost = (int)Math.Round(jedan.Value.o * 85);
                    p.Napomena += "bliski kandidati – izabran po " + (poZemlji.Count == 1 && drDok.Count > 0 ? "zemlji (" + string.Join("/", drDok) + ")" : "oznaci uloge (" + uloga + ")");
                    return p;
                }
                p.Napomena += "dva bliska kandidata (" + bliski[0].z.PaSifra + " " + bliski[0].o.ToString("0.00", CultureInfo.InvariantCulture) + ", " + bliski[1].z.PaSifra + " "
                              + bliski[1].o.ToString("0.00", CultureInfo.InvariantCulture) + "; razlika < " + RazlikaKandidata.ToString("0.00", CultureInfo.InvariantCulture) + ") – nije upisano";
                p.Pouzdanost = (int)Math.Round(izbor.o * 50);
                return p;
            }
            p.Id = izbor.z.PaSifra; p.Pouzdanost = (int)Math.Round(izbor.o * 100);
            if (uloga == "prevoznik" && (!izbor.z.Prevoznik || (u.Length == 4 && izbor.z.Uic.Length == 0)))
                ZaSifarnik.Add("partner postoji, ali nema " + (u.Length == 4 && izbor.z.Uic.Length == 0 ? "UIC " + u : "oznaku Prevoznik") + ": PaSifra " + Oznaka(izbor.z));
            return p;
        }

        /// <summary>Država partnera iz šifarnika (PaSifDrzave), prazno ako nije upisana.</summary>
        public string DrzavaPartnera(int paSifra) => s.Partneri.FirstOrDefault(x => x.PaSifra == paSifra)?.Drzava ?? "";

        /// <summary>Partner sa oznakom Prevoznik = 1 po adresi pošiljaoca maila (domen u nazivu: „cercargo.eu“ → CER …) – samo kandidat za izvođenje.</summary>
        public Pogodak PrevoznikPoDomenu(string adresa)
        {
            var p = new Pogodak { Tekst = adresa ?? "" };
            var dom = Regex.Match(adresa ?? "", @"@([^@\s>]+)$").Groups[1].Value.ToLowerInvariant();
            if (dom.Length == 0) return p;
            var deo = dom.Split('.')[0];
            // „cercargo“ → „CER“ + „CARGO“: prva reč naziva partnera je početak domena
            var kand = s.Partneri.Where(z => z.Prevoznik && z.Reci1.Count > 0).Where(z =>
            {
                var prva = Slicnost.Osnovno(z.PaNaziv).Split(' ')[0].ToLowerInvariant();
                return prva.Length >= 3 && deo.StartsWith(prva, StringComparison.Ordinal);
            }).ToList();
            if (kand.Count == 0) { p.Napomena = "nema prevoznika za domen " + dom; return p; }
            p.Kandidati = string.Join("; ", kand.Take(5).Select(z => z.PaSifra + " " + z.PaNaziv + " (" + z.Drzava + ")"));
            var tld = dom.Substring(dom.LastIndexOf('.') + 1).ToUpperInvariant();
            var poZemlji = kand.Where(z => z.Drzava == tld || (tld == "EU" && false)).ToList();
            if (kand.Count == 1) { p.Id = kand[0].PaSifra; p.Pouzdanost = 60; p.Napomena = "pošiljalac maila (" + dom + ")"; }
            else if (poZemlji.Count == 1) { p.Id = poZemlji[0].PaSifra; p.Pouzdanost = 55; p.Napomena = "pošiljalac maila (" + dom + "), zemlja " + tld; }
            else p.Napomena = "pošiljalac maila (" + dom + "): više prevoznika – nije upisano";
            return p;
        }

        // ------------------------------------------------------------------------------------------- granični prelazi

        /// <summary>
        /// v2.9.1: granični prelaz (tmpSinonimi tip GRANICA: „KELEBIA“, „0711“, „HU“ → 328 Subotica gr.) iz teksta (naziv prelaza, granični
        /// kod) ili zemlje susednog prevoznika. Po nazivu/kodu pouzdanost 90, samo po zemlji 60.
        /// </summary>
        public Pogodak Granica(string tekst, string zemlja = null)
        {
            var p = new Pogodak { Tekst = ((tekst ?? "") + (string.IsNullOrEmpty(zemlja) ? "" : " [" + zemlja + "]")).Trim() };
            var gr = s.Sinonimi.Where(x => x.Tip == "GRANICA" && s.Stanice.Any(z => z.Id == x.CiljniId)).ToList();
            if (gr.Count == 0) { p.Napomena = "tabela prelaza (tmpSinonimi GRANICA) je prazna"; return p; }
            var t = " " + Slicnost.Osnovno(tekst) + " ";
            // naziv prelaza ili kod (duži tekstovi prvo: „STAMORA MORAVITA“ pre „HU“); oznake zemalja samo iz parametra zemlja
            foreach (var x in gr.Where(x => x.Tekst.Length > 3 || Regex.IsMatch(x.Tekst, @"^\d+$")).OrderByDescending(x => x.Tekst.Length))
                if (t.Contains(" " + x.Tekst + " ")) { p.Id = x.CiljniId; p.Pouzdanost = 90; p.Napomena = "granični prelaz „" + x.Tekst + "“"; return p; }
            foreach (var z in Drzave(zemlja))
            {
                var x = gr.FirstOrDefault(y => y.Tekst == z);
                if (x != null) { p.Id = x.CiljniId; p.Pouzdanost = 60; p.Napomena = "granični prelaz samo po zemlji susednog prevoznika (" + z + ")"; return p; }
            }
            p.Napomena = "granični prelaz nije prepoznat";
            return p;
        }

        // ------------------------------------------------------------------------------------------- NHM

        private string OpisNhm(NhmZapis z) => z.Id + " " + z.Prikaz + " " + PodaciPosla.Skrati(z.Naziv, 60);

        /// <summary>
        /// v2.9.1 (tačka 3.1): NHM iz šifre (samo cifre) → ID iz planiranje.tmpNHM. 8 cifara → tačan Kod8; inače redovi sa istim Kod6:
        /// tačno jedan → taj red; nijedan ili više → red grupe Kod4 (Nivo 4); bez Kod4 → NULL i kandidati.
        /// </summary>
        public Pogodak Nhm(string sifra)
        {
            var d = Regex.Replace(sifra ?? "", @"\D", "");
            var p = new Pogodak { Tekst = sifra ?? "" };
            if (d.Length < 4) { p.Napomena = "NHM nije pročitan"; return p; }
            if (s.Nhm.Count == 0) { p.Napomena = "šifarnik planiranje.tmpNHM nije učitan"; return p; }
            if (d.Length > 8) d = d.Substring(0, 8);
            if (d.Length == 8 && s.Nhm.FirstOrDefault(z => z.Kod8 == d) is NhmZapis t8) { p.Id = t8.Id; p.Pouzdanost = 100; p.Napomena = "Kod8 " + t8.Prikaz; return p; }
            var d4 = d.Substring(0, 4);
            if (d.Length >= 6)
            {
                var d6 = d.Substring(0, 6);
                var k6 = s.Nhm.Where(z => z.Kod6 == d6 && z.Nivo >= 6).ToList();
                if (k6.Count == 1) { p.Id = k6[0].Id; p.Pouzdanost = 95; p.Napomena = "Kod6 " + d6 + " → " + k6[0].Prikaz; return p; }
                if (k6.Count > 1) p.Kandidati = string.Join("; ", k6.Take(4).Select(OpisNhm));
                p.Napomena = "Kod6 " + d6 + (k6.Count == 0 ? " ne postoji" : ": " + k6.Count + " redova") + " – ";
            }
            var g = s.Nhm.Where(z => z.Kod4 == d4 && z.Nivo == 4).OrderBy(z => z.Id).FirstOrDefault();
            if (g != null) { p.Id = g.Id; p.Pouzdanost = d.Length == 4 ? 100 : 85; p.Napomena += "grupa " + d4; return p; }
            var sl = s.Nhm.Where(z => z.Kod4 == d4).ToList();
            if (sl.Count == 0) sl = s.Nhm.Where(z => z.Kod8.StartsWith(d.Substring(0, 2), StringComparison.Ordinal) && z.Nivo <= 4).ToList();
            p.Kandidati = string.Join("; ", sl.Take(3).Select(OpisNhm));
            p.Napomena += "NHM " + d + ": grupa " + d4 + " nije u tarifi";
            return p;
        }

        private static readonly HashSet<string> NhmStop = new HashSet<string>
        {
            "ILI", "SVE", "VRSTE", "OSIM", "THE", "AND", "FOR", "BULK", "RINFUZA", "RASUTO", "TERET", "ROBA", "NEW", "USED", "OSTALO", "OSTALI", "WAGON", "KOLA", "VAGON",
        };

        /// <summary>
        /// v2.9.1 (tačka 3.1.2): NHM po nazivu robe kada šifre nema – tmpSinonimi (tip NHM, CiljniID = tmpNHM.ID), pa reči naziva u
        /// nazivu iz tarife (NazivNorm). Jedan kandidat → njegov red; više kandidata iste grupe → red Kod4; ostalo → NULL i kandidati.
        /// </summary>
        public Pogodak NhmPoNazivu(string naziv)
        {
            var p = new Pogodak { Tekst = (naziv ?? "").Trim() };
            var n = Slicnost.Osnovno(naziv);
            if (n.Length < 3) { p.Napomena = "naziv robe nije pročitan"; return p; }
            var sin = s.Sinonimi.FirstOrDefault(x => x.Tip == "NHM" && x.Tekst == n && s.Nhm.Any(z => z.Id == x.CiljniId));
            if (sin != null) { p.Id = sin.CiljniId; p.Pouzdanost = 100; p.Napomena = "sinonim (tmpSinonimi)"; return p; }
            if (s.Nhm.Count == 0) { p.Napomena = "šifarnik planiranje.tmpNHM nije učitan"; return p; }
            var reci = n.Split(' ').Where(w => w.Length >= 3 && !NhmStop.Contains(w) && !Regex.IsMatch(w, @"\d")).Distinct().ToList();
            if (reci.Count == 0 || reci.Count > 6) { p.Napomena = "naziv robe nije upotrebljiv za traženje"; return p; }
            bool Rec(NhmZapis z, string w) => z.Reci.Contains(w) || (w.Length >= 5 && z.Reci.Any(x => x.Length >= 5 && (x.StartsWith(w.Substring(0, Math.Min(w.Length, x.Length) - 1), StringComparison.Ordinal) || w.StartsWith(x.Substring(0, x.Length - 1), StringComparison.Ordinal))));
            var kand = s.Nhm.Where(z => z.Nivo >= 4 && reci.All(w => Rec(z, w))).OrderBy(z => z.Reci.Count).ThenBy(z => z.Nivo).ToList();
            if (kand.Count == 0) { p.Napomena = "naziv robe „" + p.Tekst + "“ nije pronađen u tarifi (strani naziv → tmpSinonimi tip NHM)"; return p; }
            p.Kandidati = string.Join("; ", kand.Take(3).Select(OpisNhm));
            if (kand.Count == 1) { p.Id = kand[0].Id; p.Pouzdanost = 80; p.Napomena = "po nazivu robe"; return p; }
            var grupe = kand.Select(z => z.Kod4).Distinct().ToList();
            if (grupe.Count == 1 && s.Nhm.FirstOrDefault(z => z.Kod4 == grupe[0] && z.Nivo == 4) is NhmZapis g)
            { p.Id = g.Id; p.Pouzdanost = 75; p.Napomena = "po nazivu robe: " + kand.Count + " redova iste grupe " + grupe[0]; return p; }
            p.Napomena = "po nazivu robe: " + kand.Count + " kandidata u " + grupe.Count + " grupa – nije upisano";
            return p;
        }

        /// <summary>Red tmpNHM po ID-u (null ako ga nema).</summary>
        public NhmZapis NhmRed(int? id) => id.HasValue ? s.Nhm.FirstOrDefault(z => z.Id == id.Value) : null;

        /// <summary>NHM sa 4 cifre (VRNP u stavkama teretnice), npr. „1001“.</summary>
        public static string Nhm4(string sifra)
        {
            var d = Regex.Replace(sifra ?? "", @"\D", "");
            return d.Length >= 4 ? d.Substring(0, 4) : "";
        }
    }
}
