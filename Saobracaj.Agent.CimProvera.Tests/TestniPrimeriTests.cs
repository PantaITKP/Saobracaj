using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>
    /// v2.8 – ispravke posle testa 13.11.2026 („Testni primeri“): lažne i propuštene greške na 9 stvarnih dokumenata.
    /// Podaci: testovi\nezavisni\2026-11-13\ (*.strane.json = pročitane strane iz keša agenta, isti tekst/OCR kao u radu).
    /// Napomena: Excel spiskovi kola iz tih mailova nisu dostupni (55 16261, CUV_257, CIM COAL 16) – X26 se zato ne proverava.
    /// </summary>
    [TestClass]
    public class TestniPrimeriTests
    {
        private const string Share = @"\\192.168.1.6\Cloude\Agenti\CimProvera\testovi";
        private static string Folder => Environment.GetEnvironmentVariable("CIMPROVERA_TESTOVI") is string f && f.Length > 0 ? f : Share;
        private static string Primeri => Path.Combine(Folder, "nezavisni", "2026-11-13");

        private static PravilaSkup pravila;
        private static LokalniCitac citac;
        private static AgentConfig cfg;
        private static readonly Dictionary<string, Ishod> kes = new Dictionary<string, Ishod>();

        [ClassInitialize]
        public static void Init(TestContext _)
        {
            cfg = RegresijaTests.Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje"));
            pravila = PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null);
            citac = new LokalniCitac(cfg, null, pravila.Kp);
        }

        [ClassCleanup] public static void Kraj() => citac?.Dispose();

        /// <summary>Slučaj (folder) kao jedan mail: svi *.strane.json (ili original kada keša nema), pošiljalac iz loga.</summary>
        private static Ishod Mail(string slucaj, string posiljalac)
        {
            if (kes.TryGetValue(slucaj, out var i)) return i;
            var d = Path.Combine(Primeri, slucaj);
            if (!Directory.Exists(d)) Assert.Inconclusive("Nema " + d);
            var json = Directory.GetFiles(d, "*.strane.json");
            var fajlovi = json.Length > 0 ? json : Directory.GetFiles(d, "*.pdf");
            var prilozi = fajlovi.Select(f => (Path.GetFileName(f), File.ReadAllBytes(f))).ToList();
            var en = Obrada.EngleskiZaPosiljaoca(posiljalac, cfg.JeInterni);
            return kes[slucaj] = new Obrada(pravila, citac, null, 0.6).Proveri(prilozi, posiljalac, cfg.JeInterni(posiljalac), new DateTime(2026, 11, 13), en);
        }

        private static List<Nalaz> Nalazi(Ishod i) => i.Provera.Listovi.SelectMany(l => l.Nalazi).ToList();
        private static string Opis(Ishod i) => string.Join("\n", i.Provera.Listovi.SelectMany(l => l.Nalazi.Select(n => l.Oznaka + " | " + n.Tezina + " " + n.Pravilo + " r" + n.Rubrika + ": " + n.Detalj)));
        private static bool Greska(Ishod i, string rubrika, string pravilo = null) => Nalazi(i).Any(n => n.Tezina == Tezina.Greska && n.Rubrika == rubrika && (pravilo == null || n.Pravilo == pravilo));
        private static void NemaGreske(Ishod i, string rubrika) => Assert.IsFalse(Greska(i, rubrika), "ne sme greška u r" + rubrika + "\n" + Opis(i));
        private static void ImaGresku(Ishod i, string rubrika, string pravilo = null) => Assert.IsTrue(Greska(i, rubrika, pravilo), "očekivana greška " + pravilo + " u r" + rubrika + "\n" + Opis(i));

        // ------------------------------------------------------------------------------------------- 1. r7 platilac

        [TestMethod, TestCategory("Regresija")]
        public void R7_nijedan_od_sest_dokumenata_nema_gresku()
        {
            foreach (var (s, od) in new[] { ("GK30", "ste.obr06@gmail.com"), ("55_16261", "filep.veronika@rthkft.hu"), ("CUV_257", "stefan.janjic@eurologsystem.rs"),
                                           ("CIM COAL 16", "tibor.tasnadi@pspcargo.ro"), ("GIPS", "bosstradezonasabac@gmail.com"), ("cuv za 1 vagon", "strahinja.tadic@pansped.rs") })
            {
                var i = Mail(s, od);
                NemaGreske(i, "7");
                Assert.IsFalse(Nalazi(i).Any(n => n.Pravilo.StartsWith("KP7-") && n.Pravilo != "KP7-INFO"), s + "\n" + Opis(i));
                Assert.IsTrue(Nalazi(i).Any(n => n.Pravilo == "KP7-INFO"), s + ": platilac za Srbiju pronađen\n" + Opis(i));
            }
        }

        // ------------------------------------------------------------------------------------------- 2. tip strane

        [TestMethod, TestCategory("Regresija")]
        public void SET_EXPORT_str4_je_tovarni_list_a_ne_izvozna_po_imenu_fajla()
        {
            var i = Mail("SET EXPORT TREN SARE", "andrei.terescu@pspcargo.ro");
            Assert.IsTrue(i.Provera.ImaTovarniList, "nikad „nema tovarnog lista“");
            Assert.IsTrue(i.Provera.Dokumenti.Any(d => TipDokumenta.JeTovarniList(d.Tip) && d.Strane.Contains(4)));
            Assert.IsTrue(i.Provera.Dokumenti.Any(d => d.Tip == TipDokumenta.Faktura && d.Strane.Contains(3)), "str. 3 je faktura po sadržaju");
            Assert.IsTrue(i.Provera.Dokumenti.Any(d => d.Tip == TipDokumenta.SpisakKola && d.Strane.Contains(5)), "str. 5 je spisak kola („WAGEN LISTE … Lista de vagoane“)");
            // sken lošeg kvaliteta (OCR 0,69): obrazac se ne poravna – interna ručna provera, ništa pošiljaocu
            Assert.AreEqual(Izvestaj.RazlogRucna, Izvestaj.RazlogInternog(i.Provera, pravila));
        }

        [TestMethod, TestCategory("Regresija")]
        public void Kopije_i_polegjine_lista_nisu_faktura_i_pripajaju_se_listu()
        {
            var i = Mail("11132026Scan122207", "priprema@kprevoz.co.rs");
            Assert.IsFalse(i.Provera.Dokumenti.Any(d => d.Tip == TipDokumenta.Faktura), "str. 2–3 su kopije lista, ne faktura");
            CollectionAssert.IsSubsetOf(new[] { 1, 2, 3 }, i.Provera.Listovi.Single().Dokument.Strane.ToList());
            var c = Mail("cuv za 1 vagon", "strahinja.tadic@pansped.rs");
            Assert.AreEqual(1, c.Provera.Listovi.Count);
            Assert.IsFalse(c.Provera.Dokumenti.Any(d => d.Tip == TipDokumenta.Faktura), "poleđine („Frankaturrechnung“) nisu faktura");
            Assert.IsFalse(Nalazi(c).Any(n => n.Pravilo == "X23"), "faktura se ne traži u r9");
        }

        [TestMethod, TestCategory("Regresija")]
        public void CIT7_je_poseban_dokument_a_ne_CUV_sa_nepoznatim_obrascem()
        {
            var i = Mail("CUV_257", "stefan.janjic@eurologsystem.rs");
            Assert.IsTrue(i.Provera.Dokumenti.Any(d => d.Tip == TipDokumenta.Cit7));
            Assert.AreEqual(2, i.Provera.Listovi.Count, "1.CUV_257_1 i 1.CUV_255_1");
            Assert.IsFalse(Nalazi(i).Any(n => n.Pravilo == "OBRAZAC"), Opis(i));
            Assert.IsTrue(i.OpisPriloga.Any(x => x.Contains("CIT 7 – naknadni nalog")), string.Join(" | ", i.OpisPriloga));
        }

        // ------------------------------------------------------------------------------------------- 3. KP izdao list → interno

        [TestMethod, TestCategory("Regresija")]
        public void List_koji_je_izdao_KP_je_interni_izvestaj()
        {
            foreach (var (s, od) in new[] { ("GIPS", "bosstradezonasabac@gmail.com"), ("11132026Scan130104", "priprema@kprevoz.co.rs"), ("cuv za 1 vagon", "strahinja.tadic@pansped.rs") })
            {
                var i = Mail(s, od);
                Assert.AreEqual(Izvestaj.RazlogKp, Izvestaj.RazlogInternog(i.Provera, pravila), s);
                StringAssert.StartsWith(Izvestaj.Naslov(i.Provera, pravila, false), "[INTERNO] KP tovarni list – ", s);
                Assert.IsTrue(i.Provera.Listovi.All(l => l.KpRezim), s + ": režim „KP popunjava“");
            }
            // interni izveštaj je uvek na srpskom (GIPS: pošiljalac sa gmail.com)
            Assert.IsFalse(Mail("GIPS", "bosstradezonasabac@gmail.com").Engleski);
        }

        [TestMethod, TestCategory("Regresija")]
        public void Cuv_za_1_vagon_propustene_greske_KP2_r62_broj_r29_mesto()
        {
            var i = Mail("cuv za 1 vagon", "strahinja.tadic@pansped.rs");
            var c = i.Provera.Listovi.Single().Cim;
            Assert.AreEqual("72", c.R62.Zemlja); Assert.AreEqual("163501", c.R62.Stanica); Assert.AreEqual("3212", c.R62.Preduzece); Assert.AreEqual("", c.R62.Broj);
            ImaGresku(i, "58a", "KP2"); ImaGresku(i, "62", "R62"); ImaGresku(i, "29", "R29"); ImaGresku(i, "16");
            NemaGreske(i, "10"); NemaGreske(i, "20"); NemaGreske(i, "7");
            StringAssert.Contains(c.R10.Tekst, "Kelebia"); StringAssert.Contains(c.R20.Tekst, "Kelebia");
        }

        // ------------------------------------------------------------------------------------------- 5. nečitljiv sken

        [TestMethod, TestCategory("Regresija")]
        public void Necitljiv_sken_bez_gresaka_samo_interne_napomene()
        {
            var i = Mail("11132026Scan122207", "priprema@kprevoz.co.rs");
            Assert.AreEqual(0, i.Provera.BrojGresaka, Opis(i));
            Assert.AreEqual(0, i.Provera.BrojUpozorenja, Opis(i));
            Assert.IsTrue(Nalazi(i).All(n => n.Tezina == Tezina.Info || n.Tezina == Tezina.Preporuka), Opis(i));
            Assert.IsTrue(Nalazi(i).Where(n => n.Tezina == Tezina.Info && n.Pravilo != "KP7-INFO").All(n => n.SamoInterno), "napomene su samo interne");
            Assert.IsNotNull(Izvestaj.RazlogInternog(i.Provera, pravila), "ništa pošiljaocu");
            StringAssert.Contains(Izvestaj.Naslov(i.Provera, pravila, false), "[INTERNO]");
        }

        // ------------------------------------------------------------------------------------------- 6. i 7. lažne/propuštene

        [TestMethod, TestCategory("Regresija")]
        public void GK30_greske_bez_r7_i_r57()
        {
            var i = Mail("GK30", "ste.obr06@gmail.com");
            ImaGresku(i, "4", "R4"); ImaGresku(i, "16"); ImaGresku(i, "25", "R25-MASA"); ImaGresku(i, "29"); ImaGresku(i, "62", "R62");
            NemaGreske(i, "7"); Assert.IsFalse(Nalazi(i).Any(n => n.Rubrika == "57"), Opis(i));
            Assert.IsFalse(Nalazi(i).Any(n => n.Pravilo == "CITLJIVOST"), "PDF sa poljima nije sken");
            var r4 = Nalazi(i).First(n => n.Pravilo == "R4");
            StringAssert.Contains(r4.Primer, "CZ", "primer zemlje iz r10/r12 (Lovosice, 54)");
            var c = i.Provera.Listovi.Single().Cim;
            Assert.AreEqual("72", c.R62.Zemlja); Assert.AreEqual("240036", c.R62.Stanica); Assert.AreEqual("", c.R62.Preduzece); Assert.AreEqual("", c.R62.Broj);
        }

        [TestMethod, TestCategory("Regresija")]
        public void CIM_COAL_16_adresa_postoji_r58a_prazna_KP3()
        {
            var i = Mail("CIM COAL 16", "tibor.tasnadi@pspcargo.ro");
            NemaGreske(i, "4"); NemaGreske(i, "7");
            ImaGresku(i, "58a", "R58A"); ImaGresku(i, "13", "KP3"); ImaGresku(i, "16"); ImaGresku(i, "29");
        }

        [TestMethod, TestCategory("Regresija")]
        public void CUV_257_r62_prazna_jedna_greska_bez_r58a()
        {
            var i = Mail("CUV_257", "stefan.janjic@eurologsystem.rs");
            foreach (var l in i.Provera.Listovi)
            {
                Assert.IsTrue(l.Nalazi.Any(n => n.Pravilo == "R62" && n.Tezina == Tezina.Greska), l.Oznaka);
                Assert.IsFalse(l.Nalazi.Any(n => n.Pravilo == "R58A"), "r62 i r58a obe prazne – prijavljuje se jednom (R62)");
            }
        }

        [TestMethod, TestCategory("Regresija")]
        public void Scan130104_dva_lista_r20_i_r50_nisu_greske_pošiljaocu()
        {
            var i = Mail("11132026Scan130104", "priprema@kprevoz.co.rs");
            Assert.AreEqual(2, i.Provera.Listovi.Count);
            Assert.AreEqual(2, i.Provera.Listovi.Select(l => l.Oznaka).Distinct().Count(), "svaki list ima svoju oznaku");
            NemaGreske(i, "20"); NemaGreske(i, "50");
            Assert.IsTrue(Nalazi(i).Where(n => n.Pravilo == "R50").All(n => n.SamoInterno));
            var telo = Izvestaj.Telo(i.Provera, pravila, false);
            StringAssert.Contains(telo, "Tovarni list 1: 72 16350-1 3212 15890-0");
        }

        // ------------------------------------------------------------------------------------------- 4. jezik

        [DataTestMethod]
        [DataRow("priprema@kprevoz.co.rs", false)] [DataRow("dragan.petric@selecta.co.rs", false)] [DataRow("x@firma.ba", false)] [DataRow("x@firma.me", false)]
        [DataRow("x@firma.hr", false)] [DataRow("goran.tominac@slo-zeleznice.si", false)] [DataRow("andrei.terescu@pspcargo.ro", true)]
        [DataRow("filep.veronika@rthkft.hu", true)] [DataRow("bosstradezonasabac@gmail.com", true)] [DataRow("x@lkw-walter.com", true)]
        public void Jezik_po_domenu_posiljaoca(string adresa, bool engleski)
        {
            var c = AgentConfig.Ucitaj(new System.Collections.Specialized.NameValueCollection { ["InterniDomeni"] = "kprevoz.co.rs" });
            Assert.AreEqual(engleski, Obrada.EngleskiZaPosiljaoca(adresa, c.JeInterni), adresa);
        }

        private static readonly string[] SrpskeReci = { "šifra", "npr.", "upisati", "rubrika", "rubrici", "platilac", "spisak", "zaglavlje", "naziv", "mesto", "nedostaje", "upisano", "priložen" };

        [TestMethod, TestCategory("Regresija")]
        public void Engleski_izvestaj_bez_cirilice_i_srpskih_reci()
        {
            foreach (var (s, od) in new[] { ("55_16261", "filep.veronika@rthkft.hu"), ("CIM COAL 16", "tibor.tasnadi@pspcargo.ro"), ("GK30", "ste.obr06@gmail.com") })
            {
                var i = Mail(s, od);
                Assert.IsTrue(i.Engleski, s);
                var r = i.Provera.BezInternih();
                var t = Izvestaj.Telo(r, pravila, true) + "\n" + Izvestaj.Html(r, pravila, true);
                // ime fajla i podaci sa lista mogu biti bilo kog jezika – proverava se tekst izveštaja bez vrednosti sa lista
                var bezPodataka = r.Listovi.SelectMany(l => l.Nalazi).Aggregate(t, (x, n) => string.IsNullOrEmpty(n.Vrednost) ? x : x.Replace(n.Vrednost, ""));
                foreach (var d in i.Provera.Dokumenti) bezPodataka = bezPodataka.Replace(d.Fajl ?? "", "");
                Assert.IsFalse(Regex.IsMatch(bezPodataka, @"[\u0400-\u04FF]"), s + ": ćirilica u engleskom izveštaju");
                foreach (var w in SrpskeReci)
                    Assert.IsFalse(Regex.IsMatch(bezPodataka, @"(?<!\p{L})" + Regex.Escape(w) + @"(?!\p{L})", RegexOptions.IgnoreCase), s + ": srpska reč „" + w + "“ u engleskom izveštaju\n" + bezPodataka);
            }
        }

        [TestMethod]
        public void Svako_aktivno_pravilo_ima_engleski_tekst_i_primer()
        {
            var bez = pravila.Sva.Where(d => d.Json["ne_proverava_se"] == null && ((string)d.Json["message"] != null && (string)d.Json["message_en"] == null
                                                                                     || (string)d.Json["primer"] != null && (string)d.Json["primer_en"] == null)).Select(d => d.Id).ToList();
            Assert.AreEqual(0, bez.Count, string.Join(", ", bez));
        }

        // ------------------------------------------------------------------------------------------- 8. šum i samo interno

        [TestMethod, TestCategory("Regresija")]
        public void Sum_REC3_KP6_REC6_uklonjen_REC1_REC8_samo_interno()
        {
            foreach (var (s, od) in new[] { ("GK30", "ste.obr06@gmail.com"), ("55_16261", "filep.veronika@rthkft.hu"), ("CIM COAL 16", "tibor.tasnadi@pspcargo.ro") })
            {
                var i = Mail(s, od);
                Assert.IsFalse(Nalazi(i).Any(n => n.Pravilo == "REC3" || n.Pravilo == "KP6" || n.Pravilo == "REC6"), s);
                var zaPosiljaoca = i.Provera.BezInternih().Listovi.SelectMany(l => l.Nalazi).ToList();
                Assert.IsFalse(zaPosiljaoca.Any(n => n.Pravilo == "REC1" || n.Pravilo == "REC8" || n.Pravilo == "R49" || n.Pravilo == "R50"), s);
                Assert.IsFalse(Izvestaj.Telo(i.Provera.BezInternih(), pravila, i.Engleski).Contains("004NISBAR26"), "primer iz pravila nije vrednost za upis");
            }
        }

        [TestMethod, TestCategory("Regresija")]
        public void Samo_interno_se_filtrira_i_iz_PDF_primera()
        {
            var i = Mail("cuv za 1 vagon", "strahinja.tadic@pansped.rs");
            Assert.IsTrue(Nalazi(i).Any(n => n.Pravilo == "R49" && n.SamoInterno));
            var r = i.Provera.BezInternih();
            Assert.IsFalse(Izvestaj.RedoviNapomena(r, pravila, false).Concat(Izvestaj.RedoviIspravki(r, pravila, false)).Any(x => x.N.SamoInterno), "PDF primer koristi iste redove");
        }

        [TestMethod]
        public void Pravila_bez_pokvarenih_znakova()
        {
            var t = File.ReadAllText(Path.Combine(cfg.PravilaFolder, cfg.PravilaFajl), System.Text.Encoding.UTF8);
            var m = Regex.Matches(t, "[ÃÅÄ][\u0080-¿]");
            Assert.AreEqual(0, m.Count, "UTF-8 pročitan kao ANSI: " + string.Join(" ", m.Cast<Match>().Take(5).Select(x => x.Value)));
        }

        [TestMethod]
        public void Primer_za_spisak_kola_bez_nepotvrdjene_sifre_271()
        {
            foreach (var d in pravila.Sva)
                foreach (var k in new[] { "primer", "primer_en" })
                    Assert.IsFalse(Regex.IsMatch((string)d.Json[k] ?? "", @"271 (Spisak|Wagon)"), d.Id + " " + k);
        }
    }
}
