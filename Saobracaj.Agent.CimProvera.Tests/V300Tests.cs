using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Baza;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>
    /// v3.0 (0a.5): keš strana i keš rezultata čuvaju sve što obrada koristi – obrada bez keša, iz keša rezultata
    /// i samo iz keša strana daje identičan rezultat (dokumenti, nalazi, izveštaj, upisi PRIMER-a).
    /// </summary>
    [TestClass]
    public class V300KesTests
    {
        private const string Share = @"\\192.168.1.6\Cloude\Agenti\CimProvera\testovi";
        private static string Folder => Environment.GetEnvironmentVariable("CIMPROVERA_TESTOVI") is string f && f.Length > 0 ? f : Share;

        private static readonly string[] Ekstenzije = { ".pdf", ".xls", ".xlsx", ".doc", ".docx", ".rtf", ".jpg", ".jpeg", ".png", ".tif", ".tiff" };

        /// <summary>Potpis obrade jednog maila: sve što ide u proveru, izveštaj, PRIMER i upis (bez vremena).</summary>
        internal static string Potpis(Ishod i, PravilaSkup pr, IEnumerable<(string ime, byte[] b)> prilozi)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Json.U(i.Provera.Dokumenti));
            sb.AppendLine(string.Join("|", i.Preskoceno) + "#" + string.Join("|", i.NisuProcitani) + "#" + string.Join("|", i.Neprepoznati)
                + "#" + string.Join("|", i.OpisPriloga) + "#" + string.Join("|", i.NisaPouzdanost));
            sb.AppendLine(string.Join("\n", i.Provera.Listovi.SelectMany(l => l.Nalazi).SelectMany(n => new[] { n }.Concat(n.Takodje))
                .Select(n => n.Tezina + "|" + n.Pravilo + "|r" + n.Rubrika + "|" + n.Detalj + "|" + n.Primer + "|" + string.Join("/", n.DopunaRedovi ?? new List<string>()) + "|" + n.SamoInterno)));
            sb.AppendLine(Izvestaj.Telo(i.Provera, pr, false));
            var primer = PrimerPdf.Napravi(i.Provera, pr, false, prilozi);
            sb.AppendLine("PRIMER " + primer.Razlog + " " + primer.Ime);
            foreach (var u in primer.Upisi) sb.AppendLine(u + " " + u.Krug + " " + u.Tekst + " " + u.Okvir + " " + u.Pt);
            return sb.ToString();
        }

        private sealed class Tri { public string BezKesa, KesRezultata, KesStrana; public double Sekundi; }

        /// <summary>Isti mail tri puta: prazan keš, keš rezultata, samo keš strana (keš rezultata obrisan).</summary>
        private static Tri ObradiTriPuta(IList<(string ime, byte[] b)> prilozi)
        {
            var stanje = Path.Combine(Path.GetTempPath(), "CimProveraTest", "kes-" + Guid.NewGuid().ToString("N"));
            var cfg = RegresijaTests.Cfg(stanje);
            var pr = PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null);
            string Jednom()
            {
                using (var c = new LokalniCitac(cfg, null, pr.Kp))
                    return Potpis(new Obrada(pr, c, null, 0.6).Proveri(prilozi, "posiljalac@gmail.com", false, new DateTime(2026, 11, 17), false), pr, prilozi);
            }
            try
            {
                var sw = Stopwatch.StartNew();
                var t = new Tri { BezKesa = Jednom() };
                t.Sekundi = sw.Elapsed.TotalSeconds;
                t.KesRezultata = Jednom();
                var kes = Path.Combine(stanje, "kes");
                if (Directory.Exists(kes))
                    foreach (var f in Directory.GetFiles(kes).Where(x => !Path.GetFileName(x).Contains("_strane_"))) File.Delete(f);
                t.KesStrana = Jednom();
                return t;
            }
            finally { try { Directory.Delete(stanje, true); } catch { } }
        }

        private static string PrvaRazlika(string a, string b)
        {
            var la = a.Split('\n'); var lb = b.Split('\n');
            for (int k = 0; k < Math.Max(la.Length, lb.Length); k++)
            {
                var x = k < la.Length ? la[k] : "<kraj>"; var y = k < lb.Length ? lb[k] : "<kraj>";
                if (x != y) return "red " + (k + 1) + ":\n    bez keša: " + Skrati(x) + "\n    iz keša:  " + Skrati(y);
            }
            return "";
        }

        private static string Skrati(string s) => s.Length > 300 ? s.Substring(0, 300) + "…" : s;

        private static void Uporedi(string ime, Tri t, List<string> razlike)
        {
            if (t.BezKesa != t.KesRezultata) razlike.Add(ime + " – keš rezultata: " + PrvaRazlika(t.BezKesa, t.KesRezultata));
            if (t.BezKesa != t.KesStrana) razlike.Add(ime + " – keš strana: " + PrvaRazlika(t.BezKesa, t.KesStrana));
        }

        /// <summary>Brza provera u običnom skupu testova: GK30 (dopuna r25 iz natpisa obrasca), PDF sa tekstom i sken.</summary>
        [DataTestMethod, TestCategory("Regresija")]
        [DataRow(@"nezavisni\2026-11-13\GK30\CIM draft GK30 Futog -Lovosice.pdf")]
        [DataRow(@"1.CIM_263_1.pdf")]
        [DataRow(@"HOLCIM (4)-opet.PDF")]
        public void Kes_i_bez_kesa_isti_rezultat(string fajl)
        {
            var p = Path.Combine(Folder, fajl);
            if (!File.Exists(p)) Assert.Inconclusive("Nema " + p);
            var razlike = new List<string>();
            Uporedi(fajl, ObradiTriPuta(new[] { (Path.GetFileName(p), File.ReadAllBytes(p)) }), razlike);
            Assert.AreEqual(0, razlike.Count, string.Join("\n", razlike));
        }

        /// <summary>
        /// Ceo regresioni skup (testovi\*.pdf i testovi\nezavisni\**, bez keša teksta uid1254-kes): svaki dokument kao jedan mail.
        /// Traje dugo (svaki dokument se čita OCR-om) – pokreće se sa CIMPROVERA_KES_CEO=1. Izveštaj: %TEMP%\CimProveraTest\kes_identitet.txt.
        /// </summary>
        [TestMethod, TestCategory("KesIdentitet")]
        public void Kes_i_bez_kesa_isti_rezultat_ceo_regresioni_skup()
        {
            if (Environment.GetEnvironmentVariable("CIMPROVERA_KES_CEO") != "1") Assert.Inconclusive("Ceo skup: CIMPROVERA_KES_CEO=1");
            if (!Directory.Exists(Folder)) Assert.Inconclusive("Nema " + Folder);
            var fajlovi = Directory.GetFiles(Folder).Concat(Directory.GetFiles(Path.Combine(Folder, "nezavisni"), "*", SearchOption.AllDirectories))
                .Where(f => Ekstenzije.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Where(f => f.IndexOf(@"\uid1254-kes\", StringComparison.OrdinalIgnoreCase) < 0 && f.IndexOf(@"\rezultati\", StringComparison.OrdinalIgnoreCase) < 0)
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(f => f, StringComparer.Ordinal).ToList();
            var razlike = new List<string>(); var izvestaj = new List<string>();
            foreach (var f in fajlovi)
            {
                var ime = f.Substring(Folder.Length).TrimStart('\\');
                try
                {
                    var t = ObradiTriPuta(new[] { (Path.GetFileName(f), File.ReadAllBytes(f)) });
                    var pre = razlike.Count;
                    Uporedi(ime, t, razlike);
                    izvestaj.Add((razlike.Count == pre ? "isto  " : "RAZLIKA") + "\t" + t.Sekundi.ToString("0") + " s\t" + ime);
                }
                catch (Exception ex) { razlike.Add(ime + " – izuzetak: " + ex.Message); izvestaj.Add("IZUZETAK\t\t" + ime + "\t" + ex.Message); }
            }
            var put = Path.Combine(Path.GetTempPath(), "CimProveraTest", "kes_identitet.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(put));
            File.WriteAllLines(put, new[] { "dokumenata " + fajlovi.Count + ", razlika " + razlike.Count }.Concat(izvestaj).Concat(new[] { "" }).Concat(razlike), new UTF8Encoding(false));
            Assert.AreEqual(0, razlike.Count, "dokumenata " + fajlovi.Count + "\n" + string.Join("\n", razlike));
        }
    }

    /// <summary>v3.0 (0a): upis poslova – LDC8, Zmajevo, ID 1654, SAD-HER 80 (testovi\nezavisni\2026-11-17\).</summary>
    [TestClass]
    public class V300UpisTests
    {
        private const string Share = @"\\192.168.1.6\Cloude\Agenti\CimProvera\testovi";
        private static string Folder => Environment.GetEnvironmentVariable("CIMPROVERA_TESTOVI") is string f && f.Length > 0 ? f : Share;
        internal static string Slucaj(string ime) => Path.Combine(Folder, "nezavisni", "2026-11-17", ime);

        private static PravilaSkup pravila;
        private static readonly Dictionary<string, List<Dokument>> procitano = new Dictionary<string, List<Dokument>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Dokumenti jednog fajla (čitač agenta, keš u %TEMP%\CimProveraTest\stanje).</summary>
        internal static List<Dokument> Dokumenti(string putanja)
        {
            if (!File.Exists(putanja)) Assert.Inconclusive("Nema " + putanja);
            lock (procitano)
            {
                if (procitano.TryGetValue(putanja, out var d)) return d;
                var cfg = RegresijaTests.Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje"));
                pravila = pravila ?? PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null);
                using (var c = new LokalniCitac(cfg, null, pravila.Kp))
                    d = new Obrada(pravila, c, null, 0.6).Proveri(new[] { (Path.GetFileName(putanja), File.ReadAllBytes(putanja)) }, "posiljalac@gmail.com", false, new DateTime(2026, 11, 17), false).Provera.Dokumenti;
                return procitano[putanja] = d;
            }
        }

        // ------------------------------------------------------------------------------------------- 0a.4 zaokruživanje

        [TestMethod, TestCategory("Regresija")]
        public void SAD_HER_80_zbir_masa_u_celim_kg()
        {
            var s = Dokumenti(Path.Combine(Slucaj("Aviso 4728416  SAD-HER 80"), "Specification Novi Sad-Herne 80 final.xlsx")).SelectMany(d => d.Spisak).Single();
            var p = PosaoIzDokumenata.IzSpiska(s, new Poklapanje(UpisUBazuTests.Sif()), "", "");
            Console.WriteLine(string.Join(Environment.NewLine, s.Kola.Select(k => k.Broj + " t=" + k.TaraKg + " n=" + k.NetoKg + " b=" + k.BrutoKg + " pr=" + k.Prazna + " nhm=" + k.Nhm)));
            Console.WriteLine("uk n=" + s.UkupnoNetoKg + " t=" + s.UkupnoTaraKg + " b=" + s.UkupnoBrutoKg + " | " + string.Join(" | ", p.Napomene));
            Console.WriteLine(string.Join("; ", p.V.Where(x => x.Value != null).Select(x => x.Key + "=" + x.Value)));
            Assert.AreEqual(19, p.V["BrojKola1"]);
            Assert.AreEqual(694.940m, p.V["Neto1"], "neto 694 940,48 kg → 694,940 t");
            Assert.AreEqual(666.700m, p.V["Tara1"]);
            Assert.AreEqual(1361.640m, p.V["Bruto1"], "bruto 1 361 640,48 kg → 1 361,640 t");
        }

        // ------------------------------------------------------------------------------------------- 0a.1 spisak ≠ tovarni list (LDC8)

        [TestMethod]
        public void Spisak_sa_manje_kola_ili_tudjim_kolima_ne_menja_posao()
        {
            var tl = Enumerable.Range(0, 30).Select(i => (338549920000L + i * 10).ToString()).ToList();
            Assert.IsTrue(UpisUBazu.SpisakMenjaPosao(tl, tl, 30), "isti skup kola – spisak ima prednost");
            Assert.IsFalse(UpisUBazu.SpisakMenjaPosao(tl.Take(28), tl, 30), "28 od 30 kola");
            Assert.IsFalse(UpisUBazu.SpisakMenjaPosao(tl.Take(29).Concat(new[] { "335649932019" }), tl, 30), "kola kojih nema u tovarnom listu");
            Assert.IsFalse(UpisUBazu.SpisakMenjaPosao(tl.Take(28), new string[0], 30), "tovarni list bez brojeva kola, 30 kola u r21");
            Assert.IsTrue(UpisUBazu.SpisakMenjaPosao(tl, new string[0], 30));
            // ID 1654: pročitana 4 lista (4 kola), spisak 5 kola i sadrži sva 4 – neki list nije pročitan, spisak ima prednost
            Assert.IsTrue(UpisUBazu.SpisakMenjaPosao(tl.Take(5), tl.Take(4), 4));
            Assert.IsFalse(UpisUBazu.SpisakMenjaPosao(tl.Skip(1).Take(5), tl.Take(4), 4), "više kola, ali ne sadrži kola iz lista");
            Assert.IsTrue(UpisUBazu.SpisakMenjaPosao(tl, new string[0], 0), "posao bez tovarnog lista");
            Assert.AreEqual("spisak (28 kola) ≠ tovarni list (30 kola) – proveriti", UpisUBazu.SpisakNijeTovarniList(28, 30, 0));
        }

        // ------------------------------------------------------------------------------------------- 0a.2 povratak praznih po oznaci voza (Zmajevo)

        [TestMethod, TestCategory("Regresija")]
        public void Zmajevo_spisak_praznih_po_zaglavlju_empty_wagons_9922()
        {
            // ispravan spisak (UID 597): Zmajevo → Kelebia, 27 praznih kola, „Ware | empty wagons | 9922“, tara 728 260 kg
            var s = Dokumenti(Path.Combine(Slucaj("CIT 23 Zmajevo-CR (UID 596-597)"), "597 CIT 23 Zmajevo-CR.xlsx")).SelectMany(d => d.Spisak).Single();
            Assert.AreEqual(27, s.Kola.Count);
            Assert.IsTrue(s.Kola.All(k => k.Prazna), "sva kola prazna");
            Assert.AreEqual("992200", s.NhmZaglavlje);
            Assert.AreEqual(false, UpisUBazu.Natovaren(s));
            var p = PosaoIzDokumenata.IzSpiska(s, new Poklapanje(UpisUBazuTests.Sif()), "", "");
            Assert.AreEqual(728.260m, p.V["Tara1"], "tara sa spiska");
            Assert.AreEqual(0m, p.V["Neto1"], "neto 0");
            Assert.AreEqual(27, p.V["BrojKola1"]);
        }

        [TestMethod]
        public void Oznaka_voza_sa_donjom_crtom_i_poreklo_povratka()
        {
            Assert.AreEqual("SBGT26_SRB04", PosaoIzDokumenata.Oznaka("SBGT26_SRB04"));
            CollectionAssert.AreEquivalent(new[] { "SBGT26SRB04" }, UpisUBazu.OznakeVoza("Zmajevo soy train  (SBGT26_SRB04)").ToList());
            Assert.IsTrue(UpisUBazu.OznakeVoza("49689/17 - SBGT26_SRB04 szója - KP - Predaviza").Contains("SBGT26SRB04"));
            Assert.AreEqual(0, UpisUBazu.OznakeVoza("Dokumentacija za predaju ID 1655 Zmajevo", "RE: CIT 23 Zmajevo-CR.xlsx").Count(x => x.StartsWith("SBGT")));
            Assert.AreEqual("povratak praznih po oznaci voza SBGT26_SRB04, kola se ne poklapaju",
                UpisUBazu.OpisPovratka("SBGT26_SRB04", new[] { "315508241959", "315508242353" }, new[] { "315508242916", "315506557430" }));
            Assert.AreEqual("povratak praznih po oznaci voza SBGT26_SRB04, 1 od 2 kola ista",
                UpisUBazu.OpisPovratka("SBGT26_SRB04", new[] { "315508241959", "315508242353" }, new[] { "315508241959", "315506557430" }));
        }

        // ------------------------------------------------------------------------------------------- 0a.3 roba po grupi

        private static ZapisTl Z(string nhm4, int? nid, params string[] kola) => new ZapisTl { Kljuc = Guid.NewGuid().ToString("N"), Nhm4 = nhm4, NhmId = nid, BrojKola = kola.Length, Kola = kola.ToList(), Neto = 60000 * kola.Length };

        [TestMethod]
        public void NHM_za_kola_spiska_samo_iz_grupe_tih_kola()
        {
            // ID 1654: 12 kola uglja (2702) + 5 kola IBC (3923)
            var gr = UpisUBazu.Grupe(new[] { Z("2702", 3319, "317553793587", "317553794924", "317553792803"), Z("3923", 5274, "317535472292", "317535463903") });
            Func<int, string> kod8 = id => id == 3319 ? "27020000" : id == 5274 ? "39230000" : null;
            Assert.AreEqual("39230000", UpisUBazu.NhmGrupeZaKola(gr, new[] { "3175 3547 229-2", "3175 3546 390-3" }, kod8), "IBC spisak → NHM grupe IBC");
            Assert.AreEqual("27020000", UpisUBazu.NhmGrupeZaKola(gr, new[] { "317553793587" }, kod8));
            Assert.AreEqual("", UpisUBazu.NhmGrupeZaKola(gr, new[] { "335649932019" }, kod8), "kola nisu ni u jednoj grupi → NULL, nikad NHM grupe 1");
            // jedna grupa bez brojeva kola u tovarnom listu: NHM te grupe
            Assert.AreEqual("27020000", UpisUBazu.NhmGrupeZaKola(UpisUBazu.Grupe(new[] { Z("2702", 3319) }), new[] { "335649932019" }, kod8));
        }

        [TestMethod, TestCategory("Regresija")]
        public void ID_1654_IBC_spisak_NHM_iz_zaglavlja_ne_iz_grupe_1()
        {
            var s = Dokumenti(Path.Combine(Slucaj("Dokumentacija za prijem ID 1654"), "11.11..2026 5 VAGON IBC.xlsx")).SelectMany(d => d.Spisak).Single();
            Assert.AreEqual("392390", s.NhmZaglavlje, "„24. NHM KODU 392390000000“");
            var p = PosaoIzDokumenata.IzSpiska(s, new Poklapanje(UpisUBazuTests.Sif()), "27020000", "");
            Console.WriteLine(string.Join("; ", p.V.Where(x => x.Value != null).Select(x => x.Key + "=" + x.Value)) + " | " + string.Join(" | ", p.Napomene));
            Console.WriteLine(string.Join(Environment.NewLine, s.Kola.Select(k => k.Broj + " t=" + k.TaraKg + " n=" + k.NetoKg + " b=" + k.BrutoKg + " nhm=" + k.Nhm)));
            Assert.AreEqual(5, p.V["BrojKola1"]);
            Assert.AreEqual(21.360m, p.V["Neto1"]);
            Assert.IsTrue(p.Poreklo.Any(x => x.Kolona == "NHM1" && (x.Tekst ?? "").StartsWith("3923")), string.Join(" | ", p.Poreklo.Where(x => x.Kolona == "NHM1").Select(x => x.Tekst)));
        }

        [TestMethod]
        public void Zbir_kg_se_racuna_u_celim_kilogramima()
        {
            // 19 kola × x,59 kg: zaokruživanje po kolima bi dalo +8 kg; zbir se zaokružuje jednom, na kraju
            var s = new SpisakKola();
            for (int i = 0; i < 19; i++) s.Kola.Add(new KolaNaSpisku { Broj = (335649930410L + i * 10).ToString(), TaraKg = 35000, NetoKg = 36000.59 });
            var p = PosaoIzDokumenata.IzSpiska(s, new Poklapanje(UpisUBazuTests.Sif()), "", "");
            Assert.AreEqual(684.011m, p.V["Neto1"], "19 × 36 000,59 = 684 011,21 kg → 684 011 kg → 684,011 t");
            Assert.AreEqual(1349.011m, p.V["Bruto1"], "665 000 + 684 011,21 kg → 1 349,011 t");
        }
    }
}
