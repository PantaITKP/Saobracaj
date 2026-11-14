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
        internal string Norm1; internal string Norm2;
    }

    internal sealed class NhmZapis { public int Id; public string Broj = ""; public string Naziv = ""; }

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
            }
            return this;
        }

        public static Sifarnici Ucitaj(SqlConnection c, Log log, SqlTransaction tx = null)
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
            using (var cmd = new SqlCommand("SELECT ID, RTRIM(Broj), RTRIM(Naziv) FROM dbo.NHM", c, tx))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) s.Nhm.Add(new NhmZapis { Id = r.GetInt32(0), Broj = r.GetString(1), Naziv = r.GetString(2) });
            using (var cmd = new SqlCommand("SELECT Tip, TekstNormalizovan, CiljniID FROM planiranje.tmpSinonimi", c, tx))
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
                   || (kodCifre.Length >= 7 && kodCifre.EndsWith(st, StringComparison.Ordinal));
        }

        private string ImeStanice(int id) => s.Stanice.FirstOrDefault(x => x.Id == id)?.Opis ?? id.ToString();

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

        private void ZavrsiStanicu(Pogodak p, string tekst, string sifra, List<string> drzave, Dictionary<int, double> ocene, bool granicna, Func<StanicaZapis, bool> dozvoljena)
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
        /// Partner po nazivu (tačka 2.2) → PaSifra. Redosled: (1) tmpSinonimi; (2) za prevoznika sa UIC šifrom: tačan UIC među
        /// Prevoznik = 1, naziv za potvrdu i za iste šifre; (3) sličnost naziva (PaNaziv, PaDoNaziv) ≥ 0,85, kod skoro jednakih država iz
        /// dokumenta, pa UIC, pa oznaka (Prevoznik / Pošiljalac / Primalac), pa manja šifra. Ispod praga: null i 3 najbliža kandidata.
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
            double Ocena(PartnerZapis z) => Math.Max(Slicnost.Ocena(n, z.Norm1), Slicnost.Ocena(n, z.Norm2));
            string Opis(PartnerZapis z, double o) => z.PaSifra + " " + z.PaNaziv + (z.Drzava.Length > 0 ? " (" + z.Drzava + ")" : "") + (z.Uic.Length > 0 ? " UIC " + z.Uic : "") + " " + o.ToString("0.00", CultureInfo.InvariantCulture);
            if (uloga == "prevoznik" && u.Length == 4)
            {
                var poUic = s.Partneri.Where(z => z.Prevoznik && z.Uic.TrimStart('0') == u.TrimStart('0')).ToList();
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
                    p.Napomena = "UIC " + u + (poUic.Count > 1 ? " (" + poUic.Count + " partnera sa istom šifrom, izbor po nazivu)" : "") + (n.Length > 0 && z0.o < 0.5 ? "; naziv iz dokumenta se ne poklapa (" + z0.o.ToString("0.00", CultureInfo.InvariantCulture) + ")" : "");
                    return p;
                }
                p.Napomena = "UIC " + u + " nije u šifarniku (Prevoznik = 1); ";
            }
            if (n.Length < 2) { p.Napomena += "naziv nije pročitan"; return p; }
            var lista = s.Partneri.Select(z => (z, o: Ocena(z))).Where(x => x.o > 0.5).OrderByDescending(x => x.o).ToList();
            if (lista.Count == 0) { p.Napomena += "partner nije pronađen"; return p; }
            var izbor = lista[0];
            p.Kandidati = string.Join("; ", lista.Take(3).Select(x => Opis(x.z, x.o)));
            if (izbor.o < PragPartnera) { p.Napomena += "najbliži naziv ispod praga " + PragPartnera.ToString("0.00", CultureInfo.InvariantCulture); p.Pouzdanost = (int)Math.Round(izbor.o * 100); return p; }
            // dva bliska kandidata (razlika < 0,05): NULL i kandidati – pogrešan partner je gori od praznog (odluka 14.11.;
            // izuzetak su nazivi iz tmpSinonimi, obrađeni gore)
            if (lista.Count > 1 && lista[0].o - lista[1].o < RazlikaKandidata)
            {
                p.Napomena += "dva bliska kandidata (" + lista[0].z.PaSifra + " " + lista[0].o.ToString("0.00", CultureInfo.InvariantCulture) + ", " + lista[1].z.PaSifra + " "
                              + lista[1].o.ToString("0.00", CultureInfo.InvariantCulture) + "; razlika < " + RazlikaKandidata.ToString("0.00", CultureInfo.InvariantCulture) + ") – nije upisano";
                p.Pouzdanost = (int)Math.Round(izbor.o * 50);
                return p;
            }
            p.Id = izbor.z.PaSifra; p.Pouzdanost = (int)Math.Round(izbor.o * 100);
            return p;
        }

        // ------------------------------------------------------------------------------------------- NHM

        /// <summary>
        /// NHM (tačka 2.3): šifra iz dokumenta (6 cifara, npr. 252010) – prvo tačno poklapanje sa Broj bez razmaka, pa po prve 4 cifre;
        /// više redova iste 4-cifrene šifre („9922 10“ / „9922 40“) – onaj čija podgrupa odgovara 5. i 6. cifri, inače najmanji ID.
        /// </summary>
        public Pogodak Nhm(string sifra)
        {
            var d = Regex.Replace(sifra ?? "", @"\D", "");
            var p = new Pogodak { Tekst = sifra ?? "" };
            if (d.Length < 4) { p.Napomena = "NHM nije pročitan"; return p; }
            string B(NhmZapis z) => Regex.Replace(z.Broj ?? "", @"\s", "");
            var tacno = s.Nhm.Where(z => B(z) == d).OrderBy(z => z.Id).FirstOrDefault();
            if (tacno != null) { p.Id = tacno.Id; p.Pouzdanost = 100; return p; }
            var d4 = d.Substring(0, 4);
            var grupa = s.Nhm.Where(z => B(z).Length >= 4 && (B(z).Substring(0, 4) == d4 || B(z).Split('/').Any(x => x == d4))).OrderBy(z => z.Id).ToList();
            if (grupa.Count == 0) { p.Napomena = "NHM " + d + " nije u šifarniku"; return p; }
            var cetiri = grupa.FirstOrDefault(z => B(z) == d4);
            var pod = d.Length >= 6 ? grupa.FirstOrDefault(z => B(z).Length == 6 && B(z).Substring(4, 2) == d.Substring(4, 2)) : null;
            var izbor = pod ?? cetiri ?? grupa[0];
            p.Id = izbor.Id; p.Pouzdanost = pod != null || cetiri != null ? 95 : 70;
            if (grupa.Count > 1) p.Kandidati = string.Join("; ", grupa.Take(4).Select(z => z.Id + " " + z.Broj + " " + z.Naziv));
            if (pod == null && cetiri == null) p.Napomena = "više redova za " + d4 + ", podgrupa " + (d.Length >= 6 ? d.Substring(4, 2) : "?") + " ne postoji – izabran najmanji ID " + izbor.Id;
            return p;
        }

        /// <summary>NHM sa 4 cifre (VRNP u stavkama teretnice), npr. „1001“.</summary>
        public static string Nhm4(string sifra)
        {
            var d = Regex.Replace(sifra ?? "", @"\D", "");
            return d.Length >= 4 ? d.Substring(0, 4) : "";
        }
    }
}
