using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using Saobracaj.Common.Data;
using Saobracaj.Common.Text;

namespace Saobracaj.Agent.TelegramiImport.Data
{
    /// <summary>
    /// Razrešava nazive pruga i stanica iz telegrama u ID-jeve iz tabela Pruga i stanice.
    /// Ako naziv ne može pouzdano da se upari, vraća null (poruka ide na NeedsReview).
    /// </summary>
    public sealed class Sifarnik
    {
        public sealed class Stanica { public int ID; public string Opis; public string Kljuc; }
        public sealed class Pruga { public int ID; public string Opis; public string[] Tokeni; }

        private readonly List<Stanica> stanice;
        private readonly List<Pruga> pruge;
        private readonly Dictionary<string, List<Stanica>> poKljucu;
        private readonly Dictionary<int, int> upotrebaStanice;               // stanica → broj redova u Telegrami
        private readonly Dictionary<int, Dictionary<int, int>> prugeStanice; // stanica → (pruga → broj redova)
        private readonly Dictionary<string, int> aliasStanica = new Dictionary<string, int>();      // "pruga|kljuc" ili "|kljuc"
        private readonly Dictionary<string, int> aliasPruga = new Dictionary<string, int>();

        public Sifarnik(IEnumerable<Stanica> stanice, IEnumerable<Pruga> pruge,
                        IEnumerable<Tuple<int, int?, int>> istorija = null,   // (StanicaID, PrugaID, broj)
                        IEnumerable<Tuple<string, string, int?, int>> alias = null) // (Tip, Tekst, PrugaID, RefID)
        {
            this.stanice = stanice.ToList();
            foreach (var s in this.stanice) s.Kljuc = Kljuc(s.Opis);
            this.pruge = pruge.ToList();
            foreach (var p in this.pruge) p.Tokeni = TokeniPruge(p.Opis);
            poKljucu = this.stanice.GroupBy(s => s.Kljuc).ToDictionary(g => g.Key, g => g.ToList());

            upotrebaStanice = new Dictionary<int, int>();
            prugeStanice = new Dictionary<int, Dictionary<int, int>>();
            foreach (var h in istorija ?? Enumerable.Empty<Tuple<int, int?, int>>())
            {
                upotrebaStanice[h.Item1] = (upotrebaStanice.TryGetValue(h.Item1, out var n) ? n : 0) + h.Item3;
                if (h.Item2 == null || h.Item2 <= 0) continue;
                if (!prugeStanice.TryGetValue(h.Item1, out var d)) prugeStanice[h.Item1] = d = new Dictionary<int, int>();
                d[h.Item2.Value] = (d.TryGetValue(h.Item2.Value, out var m) ? m : 0) + h.Item3;
            }
            foreach (var a in alias ?? Enumerable.Empty<Tuple<string, string, int?, int>>())
            {
                if (a.Item1 == "Pruga") aliasPruga[a.Item2] = a.Item4;
                else aliasStanica[(a.Item3?.ToString() ?? "") + "|" + a.Item2] = a.Item4;
            }
        }

        public static Sifarnik Ucitaj(SqlConnection c, SqlTransaction tx)
        {
            var st = SqlDb.Table(c, tx, "SELECT ID, RTRIM(Opis) AS Opis FROM dbo.stanice")
                .Rows.Cast<DataRow>().Select(r => new Stanica { ID = Convert.ToInt32(r["ID"]), Opis = SqlDb.Text(r, "Opis") });
            var pr = SqlDb.Table(c, tx, "SELECT ID, RTRIM(Opis) AS Opis FROM dbo.Pruga")
                .Rows.Cast<DataRow>().Select(r => new Pruga { ID = Convert.ToInt32(r["ID"]), Opis = SqlDb.Text(r, "Opis") });
            var ist = SqlDb.Table(c, tx, @"SELECT s, PrugaID, COUNT(*) AS n FROM (
                    SELECT OdStanice AS s, PrugaID FROM dbo.Telegrami WHERE OdStanice IS NOT NULL
                    UNION ALL SELECT DoStanice, PrugaID FROM dbo.Telegrami WHERE DoStanice IS NOT NULL AND DoStanice <> OdStanice) x
                  GROUP BY s, PrugaID")
                .Rows.Cast<DataRow>().Select(r => Tuple.Create(Convert.ToInt32(r["s"]), SqlDb.Int(r, "PrugaID"), Convert.ToInt32(r["n"])));
            IEnumerable<Tuple<string, string, int?, int>> al = Enumerable.Empty<Tuple<string, string, int?, int>>();
            if (SqlDb.Scalar(c, tx, "SELECT OBJECT_ID(N'dbo.TelegramiAlias', N'U')") != null)
                al = SqlDb.Table(c, tx, "SELECT Tip, Tekst, PrugaID, RefID FROM dbo.TelegramiAlias")
                    .Rows.Cast<DataRow>().Select(r => Tuple.Create(SqlDb.Text(r, "Tip"), SqlDb.Text(r, "Tekst"), SqlDb.Int(r, "PrugaID"), Convert.ToInt32(r["RefID"])));
            return new Sifarnik(st, pr, ist.ToList(), al.ToList());
        }

        public string NazivStanice(int id) { return stanice.FirstOrDefault(s => s.ID == id)?.Opis; }
        public string NazivPruge(int id) { return pruge.FirstOrDefault(p => p.ID == id)?.Opis; }

        // ------------------------------------------------------------------ stanice

        public int? StanicaID(string tekst, int? prugaID, List<string> upozorenja)
        {
            if (string.IsNullOrWhiteSpace(tekst)) return null;
            if (tekst == "#GRANICA")
            {
                // korisnik je u pregledu izabrao stanicu za "državnu granicu" na ovoj pruzi
                if (prugaID != null && aliasStanica.TryGetValue(prugaID + "|#GRANICA", out var g)) return g;
                return Granica(prugaID, upozorenja);
            }

            var k = Kljuc(tekst);
            if (prugaID != null && aliasStanica.TryGetValue(prugaID + "|" + k, out var a1)) return a1;
            if (aliasStanica.TryGetValue("|" + k, out var a2)) return a2;

            foreach (var v in Varijante(k))
            {
                var s = Tacno(v, prugaID, upozorenja, tekst);
                if (s != null) return s;
            }

            // jedna greška u kucanju (Kosjerić / Kosijerić), samo ako je jednoznačno
            if (k.Length >= 5)
            {
                var bliske = stanice.Where(s => Math.Abs(s.Kljuc.Length - k.Length) <= 1 && TekstUtil.Rastojanje(s.Kljuc, k) <= 1)
                    .GroupBy(s => s.Kljuc).ToList();
                if (bliske.Count == 1)
                {
                    var izbor = Najkoriscenija(bliske[0].ToList(), prugaID);
                    upozorenja.Add("Stanica \"" + tekst + "\" upisana kao \"" + izbor.Opis + "\" (razlika u jednom slovu).");
                    return izbor.ID;
                }
            }
            // "Orlovat" → "Orlovat stajalište" i slično: jedinstven naziv koji počinje istom reči
            var poc = stanice.Where(s => s.Kljuc.StartsWith(k + " ")).GroupBy(s => s.Kljuc).ToList();
            if (poc.Count == 1 && k.Length >= 4)
            {
                var izbor = Najkoriscenija(poc[0].ToList(), prugaID);
                upozorenja.Add("Stanica \"" + tekst + "\" upisana kao \"" + izbor.Opis + "\".");
                return izbor.ID;
            }
            // Na kraju: odbacivanje reči malim slovom na kraju ("Pancevo Glavna u" ...)
            var reci = tekst.Trim().Split(' ');
            for (int n = reci.Length - 1; n >= 1 && reci[n].Length > 0 && !char.IsUpper(reci[n][0]); n--)
            {
                var s = Tacno(Kljuc(string.Join(" ", reci.Take(n))), prugaID, upozorenja, tekst);
                if (s != null) return s;
            }
            return null;
        }

        private int? Tacno(string k, int? prugaID, List<string> upozorenja, string tekst)
        {
            if (!poKljucu.TryGetValue(k, out var lista)) return null;
            if (lista.Count == 1) return lista[0].ID;
            var izbor = Najkoriscenija(lista, prugaID);
            upozorenja.Add("Stanica \"" + tekst + "\" postoji " + lista.Count + " puta u šifarniku; izabrana ID " + izbor.ID + ".");
            return izbor.ID;
        }

        private Stanica Najkoriscenija(List<Stanica> lista, int? prugaID)
        {
            return lista.OrderByDescending(s => prugaID != null && prugeStanice.TryGetValue(s.ID, out var d) && d.ContainsKey(prugaID.Value) ? d[prugaID.Value] : 0)
                        .ThenByDescending(s => upotrebaStanice.TryGetValue(s.ID, out var n) ? n : 0)
                        .ThenBy(s => s.ID).First();
        }

        private static IEnumerable<string> Varijante(string k)
        {
            yield return k;
            var v = k;
            v = Regex.Replace(v, @"\brasp\.\s*", "rasputnica ");
            v = Regex.Replace(v, @"\bgl\.?$", "glavna");
            v = Regex.Replace(v, @"\branz\.?\b", "ranzirna");
            v = Regex.Replace(v, @"\bter\.?$", "teretna");
            v = Regex.Replace(v, @"\s+", " ").Trim();
            if (v != k) yield return v;
            var bez = Regex.Replace(v, @"^(?:rasputnica|stajaliste|ukrsnica|st\.|stanica)\s+", "");
            if (bez != v) yield return bez;
            var bezStaj = Regex.Replace(v, @"\s+(?:stajaliste|staj\.?)$", "");
            if (bezStaj != v) yield return bezStaj;
        }

        /// <summary>"državna granica" → stanica "&lt;poslednja stanica pre granice&gt; gr." iz opisa pruge (Preševo gr., Vrbnica gr.).</summary>
        private int? Granica(int? prugaID, List<string> upozorenja)
        {
            if (prugaID == null) return null;
            var p = pruge.FirstOrDefault(x => x.ID == prugaID);
            if (p == null) return null;
            var delovi = p.Opis.Split('-').Select(x => Kljuc(x.Trim().Trim('(', ')'))).ToList();
            int i = delovi.FindIndex(x => x.StartsWith("drzavna granica"));
            if (i <= 0) return null;
            var kljuc = delovi[i - 1] + " gr.";
            if (poKljucu.TryGetValue(kljuc, out var lista))
            {
                upozorenja.Add("\"Državna granica\" upisana kao stanica \"" + lista[0].Opis + "\".");
                return lista[0].ID;
            }
            return null;
        }

        // ------------------------------------------------------------------ pruge

        public int? PrugaID(string tekst, IEnumerable<string> staniceTekst, List<string> upozorenja)
        {
            if (!string.IsNullOrWhiteSpace(tekst))
            {
                var k = Kljuc(tekst);
                if (aliasPruga.TryGetValue(k, out var a)) return a;

                var tok = TokeniPruge(tekst);
                // Pruge sa istim nizom stanica (u šifarniku ima duplikata, npr. ID 35/96/97) → jedna, najkorišćenija u ranijim telegramima
                var ocene = pruge.Select(p => new { p, s = Slicnost(tok, p.Tokeni) })
                    .GroupBy(x => string.Join("|", x.p.Tokeni))
                    .Select(g => g.OrderByDescending(x => UpotrebaPruge(x.p.ID)).ThenBy(x => x.p.ID).First())
                    .OrderByDescending(x => x.s).ToList();
                if (ocene.Count > 0 && ocene[0].s >= 0.5 && (ocene.Count == 1 || ocene[0].s - ocene[1].s >= 0.1))
                    return ocene[0].p.ID;
                if (ocene.Count > 0 && ocene[0].s >= 0.3)
                    upozorenja.Add("Pruga \"" + tekst + "\" nije jednoznačna (najbliže: " + ocene[0].p.Opis + ").");
            }

            // Bez naziva pruge (12f "U stanici Vrbas ..."): pruga na kojoj su ove stanice ranije bile u telegramima
            var ids = (staniceTekst ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrEmpty(x) && x != "#GRANICA")
                .Select(x => StanicaID(x, null, new List<string>())).Where(x => x != null).Select(x => x.Value).Distinct().ToList();
            var zbir = new Dictionary<int, int>();
            foreach (var id in ids)
                if (prugeStanice.TryGetValue(id, out var d))
                    foreach (var kv in d) zbir[kv.Key] = (zbir.TryGetValue(kv.Key, out var n) ? n : 0) + kv.Value;
            if (zbir.Count > 0)
            {
                var ukupno = zbir.Values.Sum();
                var naj = zbir.OrderByDescending(x => x.Value).First();
                if (naj.Value >= 2 && naj.Value >= 0.6 * ukupno)
                {
                    upozorenja.Add("Pruga određena prema ranijim telegramima za iste stanice: " + NazivPruge(naj.Key) + ".");
                    return naj.Key;
                }
            }
            return null;
        }

        private int UpotrebaPruge(int prugaID)
        {
            return prugeStanice.Values.Sum(d => d.TryGetValue(prugaID, out var n) ? n : 0);
        }

        internal static string[] TokeniPruge(string opis)
        {
            return (opis ?? "").Split('-', '–')
                .Select(x => Kljuc(x.Replace("\"", "").Trim().Trim('(', ')', ':', ',')))
                .Select(x => Regex.Replace(x, @"\bglavna stanica\b", "glavna"))
                .Select(x => Regex.Replace(x, @"^rasputnica (\w)\b", "rasputnica $1"))
                .Where(x => x.Length > 0 && !x.StartsWith("drzavna granica") && x != "dg")
                .Distinct().ToArray();
        }

        /// <summary>Dice sličnost nizova stanica; "beograd" = "beograd centar" (prefiks na granici reči).</summary>
        internal static double Slicnost(string[] a, string[] b)
        {
            if (a.Length == 0 || b.Length == 0) return 0;
            int m = 0;
            var iskorisceno = new bool[b.Length];
            foreach (var x in a)
                for (int j = 0; j < b.Length; j++)
                {
                    if (iskorisceno[j]) continue;
                    var y = b[j];
                    if (x == y || x.StartsWith(y + " ") || y.StartsWith(x + " ") || (x.Length >= 5 && TekstUtil.Rastojanje(x, y) <= 1))
                    { m++; iskorisceno[j] = true; break; }
                }
            return 2.0 * m / (a.Length + b.Length);
        }

        public static string Kljuc(string s)
        {
            return TekstUtil.KljucNaziva(s);
        }
    }
}
