using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Saobracaj.Agenti_jobs
{
    // Excel „Centralni pregled vozova“: list „Stanje na mreži“ (blok lokomotiva i blok stanja na mreži) i list „Najave vozova“.
    // Delovi se traže po zaglavljima („LOKOMOTIVA:“, „TRENUTNA POZICIJA:“, „Ulazni granični prelaz“), a ne po broju reda,
    // jer se broj redova menja. Kolone su redom kao kolone za unos u OperationsRepository.EditableTables.
    public sealed class CentralniPregled
    {
        public List<string[]> Lokomotive = new List<string[]>();
        public List<string[]> Mreza = new List<string[]>();
        public List<string[]> Najave = new List<string[]>();

        private static readonly XNamespace N = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        // Ćelije jednog lista: red → kolona → tekst.
        private sealed class ListExcel
        {
            internal readonly SortedDictionary<int, Dictionary<int, string>> Redovi = new SortedDictionary<int, Dictionary<int, string>>();

            internal string Celija(int red, int kolona)
            {
                Dictionary<int, string> r;
                string v;
                return Redovi.TryGetValue(red, out r) && r.TryGetValue(kolona, out v) ? v : "";
            }

            // Prvi red u kome neka ćelija, posle normalizacije, počinje datim tekstom.
            internal int Nadji(string pocetak, int odReda = 0)
            {
                foreach (var r in Redovi)
                    if (r.Key >= odReda && r.Value.Values.Any(v => Kljuc(v).StartsWith(pocetak, StringComparison.Ordinal)))
                        return r.Key;
                return -1;
            }

            // Kolona u redu čije zaglavlje počinje datim tekstom.
            internal int Kolona(int red, string pocetak)
            {
                Dictionary<int, string> r;
                if (!Redovi.TryGetValue(red, out r)) return -1;
                foreach (var c in r.OrderBy(x => x.Key))
                    if (Kljuc(c.Value).StartsWith(pocetak, StringComparison.Ordinal)) return c.Key;
                return -1;
            }
        }

        private static string Kljuc(string s)
        {
            return StationNames.Normalize(s);
        }

        // Fajl može biti otvoren u Excel-u, pa se čita uz deljenje.
        public static CentralniPregled Procitaj(string putanja)
        {
            byte[] bajtovi;
            using (var f = new FileStream(putanja, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var m = new MemoryStream())
            {
                f.CopyTo(m);
                bajtovi = m.ToArray();
            }
            Dictionary<string, ListExcel> listovi;
            using (var stream = new MemoryStream(bajtovi))
                listovi = Listovi(stream);
            ListExcel stanje = listovi.FirstOrDefault(l => Kljuc(l.Key).StartsWith("stanje", StringComparison.Ordinal)).Value;
            ListExcel najave = listovi.FirstOrDefault(l => Kljuc(l.Key).StartsWith("najave", StringComparison.Ordinal)).Value;
            if (stanje == null || najave == null)
                throw new InvalidDataException("U fajlu nisu nađeni listovi „Stanje na mreži“ i „Najave vozova“. Listovi u fajlu: " + string.Join(", ", listovi.Keys) + ".");
            var p = new CentralniPregled();
            p.CitajLokomotive(stanje);
            p.CitajMrezu(stanje);
            p.CitajNajave(najave);
            return p;
        }

        // LOKOMOTIVA / SASTAV / BROJ VOZA / NAPOMENA / KLIJENT, a TPU je u redu iznad („URAĐEN TPU“). Blok ide do „STANJE NA MREŽI“.
        private void CitajLokomotive(ListExcel l)
        {
            int z = l.Nadji("lokomotiva");
            if (z < 0) throw new InvalidDataException("Na listu „Stanje na mreži“ nije nađeno zaglavlje „LOKOMOTIVA:“.");
            int kraj = l.Nadji("stanje na mrezi", z + 1);
            int[] k = { l.Kolona(z, "lokomotiva"), l.Kolona(z, "sastav"), l.Kolona(z, "broj voza"), l.Kolona(z, "napomena"), l.Kolona(z, "klijent"), Tpu(l, z) };
            foreach (int red in l.Redovi.Keys.Where(x => x > z && (kraj < 0 || x < kraj)).ToList())
            {
                string[] v = k.Select(c => c < 0 ? "" : l.Celija(red, c)).ToArray();
                if (v.All(x => x.Length == 0)) continue;
                // Oznaka bez cifre i bez ičega drugog u redu je ostatak u tabeli (npr. „C“), a ne lokomotiva.
                if (!Regex.IsMatch(v[0], @"\d") && v.Skip(1).All(x => x.Length == 0)) continue;
                Lokomotive.Add(v);
            }
        }

        // Stanica je upisana samo u prvom redu grupe, pa se prenosi naniže. Blok ide do prvog praznog reda.
        private void CitajMrezu(ListExcel l)
        {
            int z = l.Nadji("trenutna pozicija");
            if (z < 0) throw new InvalidDataException("Na listu „Stanje na mreži“ nije nađeno zaglavlje „TRENUTNA POZICIJA:“.");
            int[] k =
            {
                l.Kolona(z, "trenutna pozicija"), l.Kolona(z, "uputna"), l.Kolona(z, "vagoni"), l.Kolona(z, "id voza"),
                l.Kolona(z, "napomena"), l.Kolona(z, "kolosek"), l.Kolona(z, "broj voza"), l.Kolona(z, "klijent"),
                l.Kolona(z, "dodatne napomene"), l.Kolona(z, "carina"), Tpu(l, z)
            };
            string stanica = "";
            for (int red = z + 1; ; red++)
            {
                string[] v = k.Select(c => c < 0 ? "" : l.Celija(red, c)).ToArray();
                if (v.All(x => x.Length == 0)) break;
                if (v[0].Length > 0) stanica = v[0]; else v[0] = stanica;
                Mreza.Add(v);
            }
        }

        // Kolona „očekivano vreme“ nema naziv (prazno zaglavlje posle „ID voza“). Prelaz je upisan samo u prvom redu grupe;
        // naziv bez dvotačke u toj koloni (Ravenna, Koper...) je polazna stanica, pa ide ispred uputne, a prelaz ostaje isti.
        private void CitajNajave(ListExcel l)
        {
            int z = l.Nadji("ulazni granicni prelaz");
            if (z < 0) throw new InvalidDataException("Na listu „Najave vozova“ nije nađeno zaglavlje „Ulazni granični prelaz“.");
            int id = l.Kolona(z, "id voza");
            int ocekivano = l.Kolona(z, "ocekivano");
            if (ocekivano < 0 && id >= 0 && Kljuc(l.Celija(z, id + 1)).Length == 0) ocekivano = id + 1;
            int[] k =
            {
                l.Kolona(z, "ulazni"), l.Kolona(z, "uputna"), l.Kolona(z, "broj vagona"), id, ocekivano,
                l.Kolona(z, "broj voza"), l.Kolona(z, "prp"), l.Kolona(z, "operater"), l.Kolona(z, "napomena"),
                l.Kolona(z, "klijent"), l.Kolona(z, "dodatne napomene")
            };
            string prelaz = "";
            for (int red = z + 1; ; red++)
            {
                string[] v = k.Select(c => c < 0 ? "" : l.Celija(red, c)).ToArray();
                if (v.All(x => x.Length == 0)) break;
                if (Kljuc(v[0]).StartsWith("napomene", StringComparison.Ordinal)) break;
                if (v[0].EndsWith(":")) prelaz = v[0];
                else if (v[0].Length > 0)
                {
                    v[1] = v[1].Length > 0 ? v[0] + " - " + v[1] : v[0];
                    v[0] = prelaz;
                }
                else v[0] = prelaz;
                Najave.Add(v);
            }
        }

        // „URAĐEN TPU“ je u redu iznad zaglavlja bloka.
        private static int Tpu(ListExcel l, int zaglavlje)
        {
            int c = l.Kolona(zaglavlje, "uradjen tpu");
            return c >= 0 ? c : l.Kolona(zaglavlje - 1, "uradjen tpu");
        }

        private static Dictionary<string, ListExcel> Listovi(Stream izvor)
        {
            var rezultat = new Dictionary<string, ListExcel>();
            using (var zip = new ZipArchive(izvor, ZipArchiveMode.Read, true))
            {
                Func<string, XDocument> xml = name =>
                {
                    ZipArchiveEntry e = zip.GetEntry(name);
                    if (e == null) return null;
                    using (Stream s = e.Open()) return XDocument.Load(s);
                };
                XDocument ss = xml("xl/sharedStrings.xml");
                string[] shared = ss == null ? new string[0] : ss.Descendants(N + "si").Select(x => string.Concat(x.Descendants(N + "t").Select(t => t.Value))).ToArray();
                XDocument wb = xml("xl/workbook.xml");
                if (wb == null) throw new InvalidDataException("Fajl nije Excel (.xlsx).");
                var veze = xml("xl/_rels/workbook.xml.rels").Root.Elements().ToDictionary(x => (string)x.Attribute("Id"), x => (string)x.Attribute("Target"));
                foreach (XElement sheet in wb.Descendants(N + "sheet"))
                {
                    string cilj = veze[(string)sheet.Attribute(R + "id")];
                    cilj = cilj.StartsWith("/") ? cilj.TrimStart('/') : "xl/" + cilj;
                    XDocument doc = xml(cilj);
                    if (doc == null) continue;
                    var list = new ListExcel();
                    foreach (XElement c in doc.Descendants(N + "c"))
                    {
                        string tekst = Tekst(c, shared);
                        if (string.IsNullOrWhiteSpace(tekst)) continue;
                        Match a = Regex.Match((string)c.Attribute("r") ?? "", @"^([A-Z]+)(\d+)$");
                        if (!a.Success) continue;
                        int red = int.Parse(a.Groups[2].Value), kol = 0;
                        foreach (char ch in a.Groups[1].Value) kol = kol * 26 + (ch - 'A' + 1);
                        Dictionary<int, string> r;
                        if (!list.Redovi.TryGetValue(red, out r)) list.Redovi[red] = r = new Dictionary<int, string>();
                        r[kol] = Regex.Replace(tekst, @"\s+", " ").Trim();
                    }
                    rezultat[(string)sheet.Attribute("name")] = list;
                }
            }
            return rezultat;
        }

        private static string Tekst(XElement c, string[] shared)
        {
            string t = (string)c.Attribute("t");
            string v = (string)c.Element(N + "v");
            if (t == "s") return v == null ? "" : shared[int.Parse(v, CultureInfo.InvariantCulture)];
            if (t == "inlineStr") return string.Concat(c.Descendants(N + "t").Select(x => x.Value));
            if (v == null) return "";
            // Broj (broj voza, ID voza) bez decimalnog repa: 52439, a ne 52439.0.
            double d;
            if (t == null && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out d) && Math.Abs(d - Math.Round(d)) < 1e-9)
                return ((long)Math.Round(d)).ToString(CultureInfo.InvariantCulture);
            return v;
        }
    }

    public sealed class RezultatUvoza
    {
        public int Lokomotive, Mreza, Najave, MrezaZadrzano, NajaveZadrzano;
    }

    // Upis centralnog pregleda: tabele se pune sadržajem fajla. Red koji je postojao i pre uvoza (isto mesto i isti ID voza,
    // ili isto mesto, uputna i vagoni) zadržava vreme unosa, a vreme izmene samo ako se nije promenio; inače je to vreme uvoza.
    // Tako „Potrebna akcija“ na mapi i dalje računa koliko dugo stavka stoji.
    public static class CentralniPregledUvoz
    {
        private static readonly string[] KoloneMreze = OperationsRepository.EditableTables["OperativniPlanMreza"];
        private static readonly string[] KoloneNajava = OperationsRepository.EditableTables["OperativniPlanNajave"];
        private static readonly string[] KoloneLokomotiva = OperationsRepository.EditableTables["OperativniPlanLokomotive"];

        public static RezultatUvoza Uvezi(CentralniPregled p)
        {
            var rez = new RezultatUvoza { Lokomotive = p.Lokomotive.Count, Mreza = p.Mreza.Count, Najave = p.Najave.Count };
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OperativniPlanCentralniPregled");
                    rez.MrezaZadrzano = ZameniSaVremenima(c, tx, "OperativniPlanMreza", KoloneMreze, p.Mreza, "IDVoza", "UputnaStanica", "Vagoni");
                    rez.NajaveZadrzano = ZameniSaVremenima(c, tx, "OperativniPlanNajave", KoloneNajava, p.Najave, "IDVoza", "UputnaStanica", "Vagoni");
                    using (var cmd = Db.Command(c, tx, "DELETE FROM dbo.OperativniPlanLokomotive"))
                        cmd.ExecuteNonQuery();
                    foreach (string[] v in p.Lokomotive)
                        Upisi(c, tx, "OperativniPlanLokomotive", KoloneLokomotiva, v, null, null);
                    tx.Commit();
                    return rez;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        private static string K(object v)
        {
            return StationNames.Normalize(v == null || v == DBNull.Value ? "" : Convert.ToString(v));
        }

        // Vraća broj redova kojima je zadržano vreme unosa.
        private static int ZameniSaVremenima(System.Data.SqlClient.SqlConnection c, System.Data.SqlClient.SqlTransaction tx, string tabela, string[] kolone, List<string[]> novi, string id, string uputna, string vagoni)
        {
            DataTable stari = Db.Table(c, tx, "SELECT * FROM dbo.[" + tabela + "] ORDER BY ID");
            string mesto = kolone[0];
            Func<Func<int, string>, string> kljucId = polje => K(polje(0)) + "|" + K(polje(Array.IndexOf(kolone, id)));
            Func<Func<int, string>, string> kljucOpis = polje => K(polje(0)) + "|" + K(polje(Array.IndexOf(kolone, uputna))) + "|" + K(polje(Array.IndexOf(kolone, vagoni)));
            var poId = new Dictionary<string, Queue<DataRow>>();
            var poOpisu = new Dictionary<string, Queue<DataRow>>();
            foreach (DataRow r in stari.Rows)
            {
                Func<int, string> polje = i => Convert.ToString(r[kolone[i]]);
                Dodaj(K(r[id]).Length > 0 ? poId : poOpisu, K(r[id]).Length > 0 ? kljucId(polje) : kljucOpis(polje), r);
            }
            using (var cmd = Db.Command(c, tx, "DELETE FROM dbo.[" + tabela + "]"))
                cmd.ExecuteNonQuery();
            int zadrzano = 0;
            foreach (string[] v in novi)
            {
                Func<int, string> polje = i => v[i];
                DataRow bio = Uzmi(K(v[Array.IndexOf(kolone, id)]).Length > 0 ? poId : poOpisu,
                    K(v[Array.IndexOf(kolone, id)]).Length > 0 ? kljucId(polje) : kljucOpis(polje));
                DateTime? uneto = null, izmenjeno = null;
                if (bio != null)
                {
                    zadrzano++;
                    if (!bio.IsNull("CreatedUtc")) uneto = Convert.ToDateTime(bio["CreatedUtc"]);
                    bool isto = kolone.Select((k, i) => K(bio[k]) == K(v[i])).All(x => x);
                    if (isto) izmenjeno = Convert.ToDateTime(bio["IzmenjenoUtc"]);
                }
                Upisi(c, tx, tabela, kolone, v, uneto, izmenjeno);
            }
            return zadrzano;
        }

        private static void Dodaj(Dictionary<string, Queue<DataRow>> d, string kljuc, DataRow r)
        {
            Queue<DataRow> q;
            if (!d.TryGetValue(kljuc, out q)) d[kljuc] = q = new Queue<DataRow>();
            q.Enqueue(r);
        }

        private static DataRow Uzmi(Dictionary<string, Queue<DataRow>> d, string kljuc)
        {
            Queue<DataRow> q;
            return d.TryGetValue(kljuc, out q) && q.Count > 0 ? q.Dequeue() : null;
        }

        // Tekst se skraćuje na širinu kolone u bazi, da jedna duga ćelija ne obori ceo uvoz.
        private static void Upisi(System.Data.SqlClient.SqlConnection c, System.Data.SqlClient.SqlTransaction tx, string tabela, string[] kolone, string[] v, DateTime? uneto, DateTime? izmenjeno)
        {
            var imena = kolone.Select(x => "[" + x + "]").ToList();
            var vrednosti = kolone.Select((x, i) => "@p" + i).ToList();
            if (tabela != "OperativniPlanLokomotive")
            {
                imena.Add("CreatedUtc");
                vrednosti.Add(uneto.HasValue ? "@uneto" : "GETUTCDATE()");
            }
            imena.Add("IzmenjenoUtc");
            vrednosti.Add(izmenjeno.HasValue ? "@izmenjeno" : "GETUTCDATE()");
            string sql = "INSERT dbo.[" + tabela + "] (" + string.Join(",", imena) + ") VALUES (" + string.Join(",", vrednosti) + ")";
            using (var cmd = Db.Command(c, tx, sql))
            {
                for (int i = 0; i < kolone.Length; i++)
                    cmd.Parameters.AddWithValue("@p" + i, v[i].Length == 0 ? (object)DBNull.Value : Skrati(c, tx, tabela, kolone[i], v[i]));
                if (uneto.HasValue) cmd.Parameters.AddWithValue("@uneto", uneto.Value);
                if (izmenjeno.HasValue) cmd.Parameters.AddWithValue("@izmenjeno", izmenjeno.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static readonly Dictionary<string, int> sirine = new Dictionary<string, int>();

        private static string Skrati(System.Data.SqlClient.SqlConnection c, System.Data.SqlClient.SqlTransaction tx, string tabela, string kolona, string v)
        {
            int sirina;
            string kljuc = tabela + "." + kolona;
            lock (sirine)
                if (!sirine.TryGetValue(kljuc, out sirina))
                {
                    using (var cmd = Db.Command(c, tx, "SELECT CASE WHEN max_length = -1 THEN -1 ELSE max_length / 2 END FROM sys.columns WHERE object_id = OBJECT_ID(@t) AND name = @k", "@t", "dbo." + tabela, "@k", kolona))
                        sirina = Convert.ToInt32(cmd.ExecuteScalar());
                    sirine[kljuc] = sirina;
                }
            return sirina > 0 && v.Length > sirina ? v.Substring(0, sirina) : v;
        }
    }
}
