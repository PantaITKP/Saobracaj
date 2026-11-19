using System;
using System.Collections.Generic;
using System.Drawing;
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
    /// v2.8.1: dodati originali (CUV_255/257, CIT 7, print0, CUV exLDC7) i Excel spiskovi kola (55 16261, CUV_257, CIM COAL 16).
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

        /// <summary>
        /// Slučaj (folder) kao jedan mail: za PDF koji ima *.strane.json koristi se keš (brže), ostali PDF-ovi i Excel spiskovi kola
        /// (v2.8.1) kao originali. <paramref name="original"/> = samo originali (ciljani OCR kućica radi samo nad originalnim PDF-om).
        /// </summary>
        private static Ishod Mail(string slucaj, string posiljalac, bool original = false, string naslov = null)
        {
            var kljuc = slucaj + (original ? "|orig" : "");
            if (kes.TryGetValue(kljuc, out var i)) return i;
            var d = Path.Combine(Primeri, slucaj);
            if (!Directory.Exists(d)) Assert.Inconclusive("Nema " + d);
            var svi = Directory.GetFiles(d);
            var json = original ? new string[0] : svi.Where(f => f.EndsWith(".strane.json", StringComparison.OrdinalIgnoreCase)).ToArray();
            var pokriveni = new HashSet<string>(json.Select(f => Path.GetFileName(f).Substring(0, Path.GetFileName(f).Length - ".strane.json".Length)), StringComparer.OrdinalIgnoreCase);
            var fajlovi = json.Concat(svi.Where(f => Regex.IsMatch(f, @"\.(pdf|xlsx?|docx?)$", RegexOptions.IgnoreCase) && !pokriveni.Contains(Path.GetFileName(f))));
            if (original && !svi.Any(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))) Assert.Inconclusive("Nema originalnog PDF-a u " + d);
            var prilozi = fajlovi.Select(f => (Path.GetFileName(f), File.ReadAllBytes(f))).ToList();
            var en = Obrada.EngleskiZaPosiljaoca(posiljalac, cfg.JeInterni, naslov, null);
            Prilozi.Raspakivanje.DozvoliProcitaneStrane = true;   // samo u testovima (v2.8.1)
            try { return kes[kljuc] = new Obrada(pravila, citac, null, 0.6).Proveri(prilozi, posiljalac, cfg.JeInterni(posiljalac), new DateTime(2026, 11, 13), en); }
            finally { Prilozi.Raspakivanje.DozvoliProcitaneStrane = false; }
        }

        private static List<Nalaz> Nalazi(Ishod i) => i.Provera.Listovi.SelectMany(l => l.Nalazi).ToList();
        private static string Opis(Ishod i) => string.Join("\n", i.Provera.Listovi.SelectMany(l => l.Nalazi.Select(n => l.Oznaka + " | " + n.Tezina + " " + n.Pravilo + " r" + n.Rubrika + ": " + n.Detalj)));
        private static bool Greska(Ishod i, string rubrika, string pravilo = null) => Nalazi(i).Any(n => n.Tezina == Tezina.Greska && n.Rubrika == rubrika && (pravilo == null || n.Pravilo == pravilo));
        private static void NemaGreske(Ishod i, string rubrika) => Assert.IsFalse(Greska(i, rubrika), "ne sme greška u r" + rubrika + "\n" + Opis(i));
        /// <summary>v3.0.1 (odluka 18.11., R16): list bez pouzdanog pečata prevoznika – datum/vreme preuzimanja je upozorenje sa napomenom.</summary>
        /// <summary>v3.0.2: R29 i R62 kao R16 – bez pouzdanog pečata prevoznika upozorenje „Posle overe …“.</summary>
        private static void ImaBezOvere(Ishod i, string pravilo, string rubrika) => Assert.IsTrue(Nalazi(i).Any(n => n.Pravilo == pravilo && n.Rubrika == rubrika && n.Tezina == Tezina.Upozorenje && ((n.Tekst ?? "").Contains("Posle overe") || (n.Tekst ?? "").Contains("After validation"))), "očekivano " + pravilo + " upozorenje (bez overe)\n" + Opis(i));
        private static void ImaR16BezOvere(Ishod i) => Assert.IsTrue(Nalazi(i).Any(n => n.Pravilo == "R16" && n.Tezina == Tezina.Upozorenje && ((n.Tekst ?? "").Contains("Posle overe") || (n.Tekst ?? "").Contains("After validation"))), "očekivano R16 upozorenje (bez overe)\n" + Opis(i));
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
            Assert.IsTrue(i.OpisPriloga.Any(x => x.Contains("CIT 7 – naknadni zahtev")), string.Join(" | ", i.OpisPriloga));
        }

        // ------------------------------------------------------------------------------------------- 3. KP izdao list → interno

        [TestMethod, TestCategory("Regresija")]
        public void List_koji_je_izdao_KP_je_interni_izvestaj()
        {
            // v2.8.4: GIPS iz keša strana sa servera (13.11.) ima r62 „jj2 1 63501 / 3212) 498900“ – nepouzdano pročitana r62 se ne koristi
            // za odluku o KP tovarnom listu (odluka korisnika 14.11.) → interni izveštaj „ručna provera“ (vidi ispod); KP tovarni list ostaju ostala dva
            var gips = Mail("GIPS", "bosstradezonasabac@gmail.com");
            Assert.AreEqual(Izvestaj.RazlogRucna, Izvestaj.RazlogInternog(gips.Provera, pravila), "GIPS: r62 nepouzdana – ručna provera");
            Assert.IsTrue(gips.Provera.Listovi.All(l => l.Cim.R62.NepouzdanaZaOdluke && !l.KpRezim));
            foreach (var (s, od) in new[] { ("11132026Scan130104", "priprema@kprevoz.co.rs"), ("cuv za 1 vagon", "strahinja.tadic@pansped.rs") })
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
            ImaGresku(i, "58a", "KP2"); ImaBezOvere(i, "R62", "62"); ImaBezOvere(i, "R29", "29"); ImaR16BezOvere(i);
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
            ImaGresku(i, "4", "R4"); ImaR16BezOvere(i); ImaGresku(i, "25", "R25-MASA"); ImaBezOvere(i, "R29", "29"); ImaBezOvere(i, "R62", "62");
            NemaGreske(i, "7"); Assert.IsFalse(Nalazi(i).Any(n => n.Rubrika == "57"), Opis(i));
            Assert.IsFalse(Nalazi(i).Any(n => n.Pravilo == "CITLJIVOST"), "PDF sa poljima nije sken");
            var r4 = Nalazi(i).First(n => n.Pravilo == "R4");
            StringAssert.Contains(r4.Primer, "CZ", "primer zemlje iz r10/r12 (Lovosice, 54)");
            var c = i.Provera.Listovi.Single().Cim;
            // v2.8.4: r62 preduzeće 3212 upisano anotacijom (FreeText „pisaća mašina“, vidljiva i za štampu) – GK30 je KP tovarni list
            Assert.AreEqual("72", c.R62.Zemlja); Assert.AreEqual("240036", c.R62.Stanica); Assert.AreEqual("3212", c.R62.Preduzece, "r62 preduzeće 3212 upisano anotacijom"); Assert.AreEqual("", c.R62.Broj);
            Assert.IsTrue(i.Provera.Listovi.Single().KpRezim, "r62 preduzeće 3212 upisano anotacijom → KP tovarni list");
        }

        [TestMethod, TestCategory("Regresija")]
        public void CIM_COAL_16_adresa_postoji_r58a_prazna_KP3()
        {
            var i = Mail("CIM COAL 16", "tibor.tasnadi@pspcargo.ro");
            NemaGreske(i, "4"); NemaGreske(i, "7");
            // v3.0.2 (odluka 19.11.): KP je u r57, a nije u r13 – upozorenje „navesti KP i u rubrici 13“
            Assert.IsTrue(Nalazi(i).Any(n => n.Pravilo == "KP3" && n.Tezina == Tezina.Upozorenje), "očekivano KP3 upozorenje\n" + Opis(i));
            ImaR16BezOvere(i); ImaBezOvere(i, "R29", "29");
            // v3.0 (2.9.3 tačka 3): tuđi tovarni list – prazna r58a je upozorenje, nikad predlog 3212
            var r58 = Nalazi(i).Single(n => n.Rubrika == "58a");
            Assert.AreEqual(Tezina.Upozorenje, r58.Tezina); Assert.IsFalse((r58.Primer ?? "").Contains("3212"), r58.Primer);
        }

        [TestMethod, TestCategory("Regresija")]
        public void CUV_257_r62_prazna_napomena_r58a_greska()
        {
            var i = Mail("CUV_257", "stefan.janjic@eurologsystem.rs");
            foreach (var l in i.Provera.Listovi)
            {
                // v2.8.4 (odluka KP 2026-11-14): spoljni pošiljalac – prazna r62 je napomena, prazna r58a je greška pošiljaoca
                Assert.IsTrue(l.Nalazi.Any(n => n.Pravilo == "R62" && n.Tezina == Tezina.Upozorenje && !n.SamoInterno), l.Oznaka);
                Assert.IsFalse(l.Nalazi.Any(n => n.Pravilo == "R62" && n.Tezina == Tezina.Greska), l.Oznaka);
                // v3.0 (2.9.3 tačka 3): r62 prazna – r58a prazna je upozorenje „proveriti“ (izdavalac nepoznat), pošiljaocu
                Assert.IsTrue(l.Nalazi.Any(n => n.Pravilo == "R58A" && n.Tezina == Tezina.Upozorenje && !n.SamoInterno && n.Tekst.Contains("proveriti")), l.Oznaka + ": r58a prazna – proveriti");
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
            // v2.8.4: GK30 više nije u spisku – r62 preduzeće 3212 upisano anotacijom, pa je KP tovarni list (samo interni izveštaj, na srpskom)
            foreach (var (s, od) in new[] { ("55_16261", "filep.veronika@rthkft.hu"), ("CIM COAL 16", "tibor.tasnadi@pspcargo.ro") })
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

        // =========================================================================================== v2.8.1

        // ------------------------------------------------------------------------------------------- 1. .strane.json samo u testovima

        [TestMethod]
        public void StraneJson_van_testova_je_nedozvoljen_tip_priloga()
        {
            Prilozi.Raspakivanje.DozvoliProcitaneStrane = false;
            var preskoceno = new List<string>();
            var p = Prilozi.Raspakivanje.Raspakuj("CIM.pdf.strane.json", System.Text.Encoding.UTF8.GetBytes("[{\"Broj\":1,\"Reci\":[]}]"), preskoceno);
            Assert.AreEqual(0, p.Count);
            Assert.AreEqual(1, preskoceno.Count);
            StringAssert.Contains(preskoceno[0], "nedozvoljen tip priloga");
        }

        [TestMethod, TestCategory("Regresija")]
        public void StraneJson_u_mailu_ne_utice_na_rezultat()
        {
            var d = Path.Combine(Primeri, "CUV_257");
            var gk = Path.Combine(Primeri, "GK30", "CIM draft GK30 Futog -Lovosice.pdf");
            if (!File.Exists(gk) || !Directory.Exists(d)) Assert.Inconclusive("Nema test podataka");
            // izmišljen „pročitan“ tovarni list: pravi keš jednog CUV-a pod drugim imenom
            var lazni = File.ReadAllBytes(Path.Combine(d, "1.CUV_257_1.pdf.strane.json"));
            Prilozi.Raspakivanje.DozvoliProcitaneStrane = false;
            var obrada = new Obrada(pravila, citac, null, 0.6);
            var sam = obrada.Proveri(new[] { ("CIM.pdf.strane.json", lazni) }, "x@firma.com", false, new DateTime(2026, 11, 13), true);
            Assert.IsFalse(sam.Provera.ImaTovarniList, "samo .strane.json – nema tovarnog lista");
            Assert.IsTrue(sam.Preskoceno.Any(x => x.Contains("nedozvoljen tip priloga")), string.Join(" | ", sam.Preskoceno));

            var bez = obrada.Proveri(new[] { ("GK30.pdf", File.ReadAllBytes(gk)) }, "x@firma.com", false, new DateTime(2026, 11, 13), true);
            var sa = obrada.Proveri(new[] { ("GK30.pdf", File.ReadAllBytes(gk)), ("CUV.pdf.strane.json", lazni) }, "x@firma.com", false, new DateTime(2026, 11, 13), true);
            Assert.AreEqual(bez.Provera.Listovi.Count, sa.Provera.Listovi.Count);
            CollectionAssert.AreEqual(bez.Provera.Listovi.SelectMany(l => l.Nalazi).Select(n => n.Pravilo + "|" + n.Rubrika + "|" + n.Tezina).ToList(),
                                      sa.Provera.Listovi.SelectMany(l => l.Nalazi).Select(n => n.Pravilo + "|" + n.Rubrika + "|" + n.Tezina).ToList());
            Assert.IsFalse(sa.Provera.Dokumenti.Any(x => x.Fajl.Contains("CUV.pdf")), "prilog .strane.json nije dokument");
        }

        // ------------------------------------------------------------------------------------------- 2. GIPS r62

        [TestMethod, TestCategory("Regresija")]
        public void GIPS_r62_iz_originala_cita_sve_kucice()
        {
            var i = Mail("GIPS", "bosstradezonasabac@gmail.com", original: true, naslov: "IZVOZ");
            var c = i.Provera.Listovi.Single().Cim;
            Assert.AreEqual("72", c.R62.Zemlja); Assert.AreEqual("163501", c.R62.Stanica); Assert.AreEqual("3212", c.R62.Preduzece); Assert.AreEqual("158900", c.R62.Broj);
            Assert.IsFalse(Nalazi(i).Any(n => n.Rubrika == "62"), "r62 je ispravna\n" + Opis(i));
        }

        [TestMethod, TestCategory("Regresija")]
        public void GIPS_r62_nepouzdano_citanje_je_interna_napomena_a_ne_greska()
        {
            // keš strana bez ciljanog OCR-a kućica: „jj2 1 63501 / 3212) 498900“
            var i = Mail("GIPS", "bosstradezonasabac@gmail.com");
            NemaGreske(i, "62");
            var n = Nalazi(i).Single(x => x.Rubrika == "62");
            Assert.AreEqual(Tezina.Info, n.Tezina); Assert.IsTrue(n.SamoInterno);
            StringAssert.StartsWith(n.Tekst, "r62 nečitljiva – proveriti ručno");
            // v2.8.3: interni red je samo u odeljku „INTERNO (ne ide pošiljaocu)“ na dnu, ne među napomenama
            var telo = Izvestaj.Telo(i.Provera, pravila, false);
            var deo = telo.Split(new[] { "INTERNO (ne ide pošiljaocu)" }, StringSplitOptions.None);
            Assert.AreEqual(2, deo.Length, telo);
            StringAssert.Contains(deo[1], "Rubrika 62");
            Assert.IsFalse(deo[0].Contains("Rubrika 62"), "r62 nije u tabelama za pošiljaoca");
        }

        [TestMethod]
        public void R62_OCR_sum_slova_i_cifara_je_nepouzdano_citanje()
        {
            Rec W(string t, double x, double p = 0.9) => new Rec { T = t, X0 = x, X1 = x + 0.03, Y0 = 0.9, Y1 = 0.91, Pouzdanost = p };
            var sum = CimParser.Identifikacija(new List<Rec> { W("jj2", 0.1), W("163501", 0.2), W("3212", 0.4) });
            Assert.IsTrue(sum.OcrSum); Assert.AreEqual("", sum.Zemlja, "„jj2“ nije zemlja");
            Assert.IsTrue(CimParser.Identifikacija(new List<Rec> { W("72", 0.1), W("163501", 0.2, 0.4) }).NepouzdanoCitanje, "niska pouzdanost cifara");
            var cisto = CimParser.Identifikacija(new List<Rec> { W("72", 0.1), W("163501", 0.2), W("3212", 0.4) });
            Assert.IsFalse(cisto.NepouzdanoCitanje || cisto.OcrSum);
        }

        // ------------------------------------------------------------------------------------------- 3. jezik za opšte domene

        [DataTestMethod]
        [DataRow("bosstradezonasabac@gmail.com", "IZVOZ", "", false, "naslov „IZVOZ“")]
        [DataRow("ste.obr06@gmail.com", "Test", "", true, "bez srpskog teksta")]
        [DataRow("x@gmail.com", "CIM", "Poštovani,\nu prilogu CIM.\nPozdrav", false, "srpska slova")]
        [DataRow("x@yahoo.com", "CIM", "Dear Sir,\nplease find attached the consignment note.\nBest regards\nMarko Šapčanin, Šabac", true, "srpska slova samo u potpisu")]
        [DataRow("x@outlook.com", "RE: CIM", "Please check.\n\nFrom: priprema@kprevoz.co.rs\nSent: Friday\nPoštovani, tovarni list ima greške.", true, "srpski samo u citatu")]
        [DataRow("x@hotmail.com", "RE: CIM", "OK\n> Poštovani, šaljemo tovarni list", true, "citat sa >")]
        [DataRow("x@live.com", "Товарни лист", "", false, "ćirilica")]
        [DataRow("x@icloud.com", "wagons", "saljem vagone za sutra", false, "srpske reči bez dijakritika")]
        [DataRow("x@gmail.com", "Frachtbrief", "Sehr geehrte Damen und Herren, anbei der Frachtbrief. Mit freundlichen Grüßen", true, "nemački")]
        [DataRow("x@gmail.com", "CIM", "Dobrý den, posílám nákladní list. Děkuji", true, "češki (č, ř, ě nije srpski)")]
        [DataRow("x@pspcargo.ro", "IZVOZ", "Poštovani", true, "domen firme: po domenu, ne po tekstu")]
        public void Jezik_za_opste_domene_po_tekstu_posiljaoca(string adresa, string naslov, string telo, bool engleski, string opis)
        {
            var c = AgentConfig.Ucitaj(new System.Collections.Specialized.NameValueCollection { ["InterniDomeni"] = "kprevoz.co.rs" });
            Assert.AreEqual(engleski, Obrada.EngleskiZaPosiljaoca(adresa, c.JeInterni, naslov, telo), opis);
        }

        [TestMethod, TestCategory("Regresija")]
        public void GIPS_sa_gmail_IZVOZ_je_na_srpskom()
        {
            var c = AgentConfig.Ucitaj(new System.Collections.Specialized.NameValueCollection { ["InterniDomeni"] = "kprevoz.co.rs" });
            Assert.IsFalse(Obrada.EngleskiZaPosiljaoca("bosstradezonasabac@gmail.com", c.JeInterni, "IZVOZ", null));
        }

        [TestMethod]
        public void Telo_maila_iz_HTML_bez_citata()
        {
            var m = new MimeKit.MimeMessage();
            m.Body = new MimeKit.TextPart("html") { Text = "<p>Please see attached.</p><blockquote>Poštovani, tovarni list</blockquote>" };
            var t = ObradaPoruke.TeloMaila(m);
            StringAssert.Contains(t, "Please see attached");
            Assert.IsFalse(t.Contains("Poštovani"));
        }

        // ------------------------------------------------------------------------------------------- 4. Excel spiskovi kola

        [TestMethod, TestCategory("Regresija")]
        public void Excel_spiskovi_X26_vise_nije_greska()
        {
            foreach (var (s, od) in new[] { ("55_16261", "filep.veronika@rthkft.hu"), ("CUV_257", "stefan.janjic@eurologsystem.rs"), ("CIM COAL 16", "tibor.tasnadi@pspcargo.ro") })
            {
                var i = Mail(s, od);
                Assert.IsTrue(i.Provera.Dokumenti.Any(d => d.Tip == TipDokumenta.SpisakKola && d.Izvor == "excel"), s + ": Excel spisak kola pročitan");
                Assert.IsFalse(Nalazi(i).Any(n => n.Pravilo == "X26"), s + ": spisak je priložen\n" + Opis(i));
            }
        }

        [TestMethod, TestCategory("Regresija")]
        public void COAL_16_spisak_bez_mase_robe_r25_ostaje_greska_sa_tacnim_opisom()
        {
            // wagon list 16.xlsx: 33 kola, sva „Empty“, upisana samo tara – mase robe nema (CIM: STEAM COAL IN BULK, NHM 2702)
            var i = Mail("CIM COAL 16", "tibor.tasnadi@pspcargo.ro");
            var sp = i.Provera.Dokumenti.Single(d => d.Tip == TipDokumenta.SpisakKola).Spisak.Single();
            Assert.AreEqual(33, sp.Kola.Count);
            Assert.IsFalse(sp.MasaUpisana, "na spisku je samo tara");
            Assert.IsTrue(sp.Kola.All(k => k.Prazna), "kola su označena „Empty“");
            var n = Nalazi(i).Single(x => x.Pravilo == "R25-MASA");
            Assert.AreEqual(Tezina.Greska, n.Tezina);
            StringAssert.Contains(n.Detalj, "wagon list 16.xlsx");
            StringAssert.Contains(n.Detalj, "marked as empty");
            // v2.8.2: „Netto: / Tare: / Brutto:“ bez brojeva – masa nije upisana kao broj; tara sa spiska (33 kola, 703 t) je prava vrednost
            StringAssert.Contains(n.Tekst, "the mass is not entered as a number");
            Assert.AreEqual(703000d, sp.TaraKg);
            CollectionAssert.AreEqual(new[] { "X kg", "703 000 kg", "X kg" }, n.DopunaRedovi, "redovi obrasca: Netto, Tare, Brutto; hiljade razmakom");
            Assert.IsTrue(n.DopunaUzOznake);
        }

        [TestMethod, TestCategory("Regresija")]
        public void GK30_r25_tackice_nisu_broj_i_dopuna_X_kg_u_redove_obrasca()
        {
            var i = Mail("GK30", "ste.obr06@gmail.com");
            var n = Nalazi(i).Single(x => x.Pravilo == "R25-MASA");
            Assert.AreEqual(Tezina.Greska, n.Tezina);
            // v2.8.4: r62 preduzeće 3212 upisano anotacijom → KP tovarni list, interni izveštaj na srpskom (ranije engleski: „the mass is not entered as a number“)
            Assert.IsTrue(i.Provera.Listovi.Single().KpRezim, "r62 preduzeće 3212 upisano anotacijom");
            CollectionAssert.AreEqual(new[] { "X kg", "X kg", "X kg" }, n.DopunaRedovi, "redovi T= / N= / G=");
        }

        // ------------------------------------------------------------------------------------------- 5. CIT 7

        [TestMethod, TestCategory("Regresija")]
        public void CIT7_procitan_izmene_r4_r10_r12()
        {
            var i = Mail("CUV_257", "stefan.janjic@eurologsystem.rs");
            var z = i.Provera.Dokumenti.Single(d => d.Tip == TipDokumenta.Cit7).Cit7;
            Assert.IsNotNull(z);
            Assert.IsTrue(z.Cuv, "označen kolski list CUV");
            Assert.AreEqual("79", z.Zemlja); Assert.AreEqual("422113", z.Stanica); Assert.AreEqual("2179", z.Preduzece); Assert.AreEqual("", z.Broj);
            CollectionAssert.AreEqual(new[] { 6 }, z.Sifre);
            Assert.IsTrue(z.BezCarine, "„Nije neophodno obavestiti otpravnu carinu“");
            StringAssert.Contains(z.Primalac, "DELTA AGRAR DOO");
            StringAssert.Contains(z.MestoIzdavanja, "RUMA");
            StringAssert.Contains(z.Podnosilac, "EurologSystem");
            CollectionAssert.AreEqual(new[] { "4", "10", "12" }, z.Izmene.Select(x => x.Rubrika).ToList());
            StringAssert.StartsWith(z.Izmene[0].Vrednost, "GRANEXPORT D.O.O.");
            StringAssert.Contains(z.Izmene[0].Vrednost, "26000 Pančevo");
            Assert.AreEqual("Sremska Mitrovica", z.Izmene[1].Vrednost);
            Assert.AreEqual("72 16509-2", z.Izmene[2].Vrednost);
        }

        [TestMethod, TestCategory("Regresija")]
        public void CIT7_veza_sa_CUV_257_i_upozorenja_samo_interno()
        {
            var i = Mail("CUV_257", "stefan.janjic@eurologsystem.rs");
            var c = i.Provera.Cit7.Single();
            // v2.8.2: CIT 7 (79 422113 2179) i CUV-ovi u mailu (r62 prazna) nemaju istu identifikaciju, kola ni referencu – nepovezan;
            // ista uputna i primalac nisu dokaz veze (CIT 7 ih menja)
            Assert.IsNull(c.Veza, "nepovezan");
            StringAssert.Contains(Cit7Provera.Tekst(i.Provera.Cit7, pravila), "NEPOVEZAN");
            Assert.IsTrue(c.Napomene.Any(x => x.Contains("nisu dokaz veze")), string.Join("\n", c.Napomene));
            Assert.AreEqual("72 16503-5", c.Izmene.Single(x => x.rubrika == "12").stara, "stara vrednost iz zaglavlja CIT 7");
            StringAssert.Contains(c.Izmene.Single(x => x.rubrika == "4").stara, "DELTA AGRAR");
            Assert.IsTrue(c.Upozorenja.Any(x => x.Contains("duplikat")), string.Join("\n", c.Upozorenja));
            Assert.IsTrue(c.Napomene.Any(x => x.Contains("nepovezan")), "r7 ne može da se proveri\n" + string.Join("\n", c.Napomene));
            Assert.IsFalse(c.Upozorenja.Any(x => x.Contains("deli")), "pošiljka se ne deli\n" + string.Join("\n", c.Upozorenja));
            Assert.IsFalse(c.Upozorenja.Any(x => x.Contains("carinsko područje")), "RS → RS");
            Assert.IsTrue(c.Napomene.Any(x => x.Contains("Carinsko područje se ne menja")));
            // ništa od CIT 7 pošiljaocu: nema nalaza o CIT 7 u listovima, izveštaj za pošiljaoca ga ne pominje
            Assert.IsFalse(Nalazi(i).Any(n => (n.Tekst ?? "").Contains("CIT 7") && n.Tezina == Tezina.Greska));
            Assert.AreEqual(0, i.Provera.BezInternih().Cit7.Count);
            Assert.IsFalse(Izvestaj.Telo(i.Provera.BezInternih(), pravila, false).Contains("GRANEXPORT"));
            var t = Cit7Provera.Tekst(i.Provera.Cit7, pravila);
            StringAssert.Contains(t, "Rubrika 4 – Primalac");
            StringAssert.Contains(t, "→");
        }

        [TestMethod, TestCategory("Regresija")]
        public void CIT7_bez_lista_u_mailu_veza_sa_ranije_obradjenim()
        {
            var cuv = Mail("CUV_257", "stefan.janjic@eurologsystem.rs");
            var ranije = cuv.Provera.Listovi.Select(l => IstorijaListova.Iz(l, cuv.Provera.Dokumenti, new DateTime(2026, 11, 12), "RE: Status najave")).ToList();
            // ranije obrađen CUV iz Ljubljane Zalog sa istom identifikacijom (79 422113 2179) – jedini takav list
            ranije.Add(new ZapisLista { Datum = new DateTime(2026, 11, 11), Mail = "CUV Ljubljana Zalog", Fajl = "CUV_LJ_Zalog.pdf", Zemlja = "79", Stanica = "422113", Preduzece = "2179", Broj = "00123-4", R4 = "DELTA AGRAR DOO", R12 = "72 16503-5" });
            var cit7 = Path.Combine(Primeri, "CUV_257", "CIT7 for the empty train Ljubljana Zalog-Stara Pazova (Sremska Mitrovica, IK AGRIUM).pdf.strane.json");
            Prilozi.Raspakivanje.DozvoliProcitaneStrane = true;
            Ishod i;
            try { i = new Obrada(pravila, citac, null, 0.6) { RanijiListovi = () => ranije }.Proveri(new[] { (Path.GetFileName(cit7), File.ReadAllBytes(cit7)) }, "x@eurologsystem.rs", false, new DateTime(2026, 11, 13), false); }
            finally { Prilozi.Raspakivanje.DozvoliProcitaneStrane = false; }
            Assert.IsFalse(i.Provera.ImaTovarniList);
            var c = i.Provera.Cit7.Single();
            Assert.IsNotNull(c.Veza, "list iz ranijeg maila");
            Assert.IsFalse(c.Veza.IzIstogMaila);
            StringAssert.Contains(c.Veza.Opis, "ranije obrađen 11.11.2026");
            StringAssert.Contains(c.Veza.Fajl, "CUV_LJ_Zalog");
            Assert.IsTrue(c.Osnov.Any(x => x.Contains("identifikacija")), string.Join(", ", c.Osnov));

            // dva lista sa istom stanicom i preduzećem, a CIT 7 bez broja otpravljanja: veza nije pouzdana → nepovezan
            ranije.Add(new ZapisLista { Datum = new DateTime(2026, 11, 10), Fajl = "CUV_LJ_Zalog_2.pdf", Zemlja = "79", Stanica = "422113", Preduzece = "2179", Broj = "00125-0" });
            var r = new RezultatProvere { Dokumenti = { i.Provera.Dokumenti.Single(d => d.Tip == TipDokumenta.Cit7) } };
            Assert.IsNull(Cit7Provera.Proveri(r, ranije, new DateTime(2026, 11, 13), "x").Single().Veza);
        }

        // ------------------------------------------------------------------------------------------- reset-potpun (v2.8.2)

        [TestMethod]
        public void Reset_potpun_brise_stanje_rezultate_logove_i_zapisuje_vreme_nulte_tacke()
        {
            var koren = Path.Combine(Path.GetTempPath(), "CimProveraReset-" + Guid.NewGuid().ToString("N"));
            try
            {
                var c = RegresijaTests.Cfg(Path.Combine(koren, "stanje"));
                c.RezultatiFolder = Path.Combine(koren, "rezultati"); c.LogFolder = Path.Combine(koren, "logs");
                Directory.CreateDirectory(Path.Combine(c.StanjeFolder, "kes")); Directory.CreateDirectory(c.RezultatiFolder); Directory.CreateDirectory(c.LogFolder);
                File.WriteAllText(Path.Combine(c.StanjeFolder, "stanje.json"), "{\"Poruke\":{\"stefan.obradovic@kprevoz.co.rs|x\":{\"Status\":\"Greske\"}},\"Folderi\":{\"agenti@kprevoz.co.rs|INBOX\":{\"PoslednjiUid\":500}}}");
                File.WriteAllText(Path.Combine(c.StanjeFolder, "listovi.json"), "[]");
                File.WriteAllText(Path.Combine(c.StanjeFolder, "kes", "abc_strane_1.json"), "[]");
                File.WriteAllText(Path.Combine(c.RezultatiFolder, "PRIMER_x.pdf"), "x");
                File.WriteAllText(Path.Combine(c.LogFolder, "CimProvera-20261113.log"), "x");
                File.WriteAllText(Path.Combine(c.LogFolder, "drugo.txt"), "ostaje");
                var pre = DateTimeOffset.Now;
                var kod = ResetStanja.Potpun(c, new Log(Path.Combine(koren, "log-reseta")) { Tiho = true }, "nepostojeci", bezZadatka: true, suvo: false);
                Assert.AreEqual(0, kod);
                Assert.IsFalse(File.Exists(Path.Combine(c.StanjeFolder, "listovi.json")));
                Assert.IsFalse(Directory.Exists(Path.Combine(c.StanjeFolder, "kes")) && Directory.GetFiles(Path.Combine(c.StanjeFolder, "kes")).Length > 0);
                Assert.AreEqual(0, Directory.GetFiles(c.RezultatiFolder).Length);
                Assert.IsFalse(File.Exists(Path.Combine(c.LogFolder, "CimProvera-20261113.log")));
                Assert.IsTrue(File.Exists(Path.Combine(c.LogFolder, "drugo.txt")), "brišu se samo CimProvera-*.log");
                var st = Stanje.Ucitaj(c.StanjeFolder);
                Assert.AreEqual(0, st.Poruke.Count, "oba sandučeta");
                Assert.AreEqual(0, st.Folderi.Count, "UID se ne čita pri resetu – zapisuje ga agent po INTERNALDATE");
                Assert.AreEqual(0, st.NultaTackaUid);
                Assert.AreEqual(c.Nalog, st.NultaTackaNalog);
                Assert.IsTrue(st.NultaTacka.HasValue && st.NultaTacka.Value >= pre.AddSeconds(-1) && st.NultaTacka.Value <= DateTimeOffset.Now.AddSeconds(1));
            }
            finally { try { Directory.Delete(koren, true); } catch { } }
        }

        [TestMethod]
        public void CIT7_uputna_i_primalac_nisu_dokaz_veze_kola_i_referenca_jesu()
        {
            var z = new Cit7Zahtev { Primalac = "Primalac B", MestoIzdavanja = "Wien", StanicaIzdavanja = "81 01234-5", Tekst = "Box 10: Linz ref GK30" };
            var r = new RezultatProvere { Dokumenti = { new Dokument { Tip = TipDokumenta.Cit7, Fajl = "CIT7.pdf", Cit7 = z } } };
            var istiPrimalac = new ZapisLista { Fajl = "A.pdf", R4 = "Primalac B", R10 = "Wien", R12 = "81 01234-5" };
            Assert.IsNull(Cit7Provera.Proveri(r, new List<ZapisLista> { istiPrimalac }, new DateTime(2026, 11, 13), "x").Single().Veza, "uputna/primalac nisu veza");
            var poReferenci = new ZapisLista { Fajl = "B.pdf", R8 = "GK30" };
            Assert.AreEqual("B.pdf", Cit7Provera.Proveri(r, new List<ZapisLista> { istiPrimalac, poReferenci }, new DateTime(2026, 11, 13), "x").Single().Veza?.Fajl, "referenca r8");
            z.BrojeviKola.Add("338407649205");
            var poKolima = new ZapisLista { Fajl = "C.pdf", Kola = { "338407649205", "338407648942" } };
            Assert.AreEqual("C.pdf", Cit7Provera.Proveri(r, new List<ZapisLista> { istiPrimalac, poKolima }, new DateTime(2026, 11, 13), "x").Single().Veza?.Fajl, "brojevi kola");
        }

        // ------------------------------------------------------------------------------------------- 6. novi izgled PDF primera

        /// <summary>PDF primer za spoljnog pošiljaoca, kao u radu nad sandučetom (bez nalaza „samo interno“), iz originalnih priloga.</summary>
        private static (RezultatProvere r, PrimerRezultat pr, bool en) Primer(string slucaj, string od)
        {
            var i = Mail(slucaj, od, original: true);
            var r = Izvestaj.RazlogInternog(i.Provera, pravila) != null ? i.Provera : i.Provera.BezInternih();
            var d = Path.Combine(Primeri, slucaj);
            var prilozi = Directory.GetFiles(d).Where(f => Regex.IsMatch(f, @"\.(pdf|xlsx?)$", RegexOptions.IgnoreCase)).Select(f => (Path.GetFileName(f), File.ReadAllBytes(f))).ToList();
            var pr = PrimerPdf.Napravi(r, pravila, i.Engleski, prilozi);
            Assert.IsTrue(pr.Napravljen, slucaj + ": " + pr.Razlog + " " + pr.Greska);
            return (r, pr, i.Engleski);
        }

        [TestMethod, TestCategory("Regresija")]
        public void Primer_bez_strane_sa_legendom_samo_strana_lista()
        {
            foreach (var (s, od) in new[] { ("GK30", "ste.obr06@gmail.com"), ("55_16261", "filep.veronika@rthkft.hu") })
            {
                var (r, pr, _) = Primer(s, od);
                Assert.AreEqual(r.Listovi.Count, pr.Strane.Count, s + ": samo strane lista (spisak kola nema nalaza)");
                Assert.AreEqual(r.Listovi.Count, pr.PoListu.Count);
                // strana lista (obrazac sa trakom), ne A4 strana sa tabelom: ima nalaze ucrtane u rubrike
                Assert.IsTrue(pr.Upisi.Any(u => u.Krug.Width > 0), s);
            }
        }

        [TestMethod, TestCategory("Regresija")]
        public void Primer_dopuna_je_vrednost_ili_primer_a_ne_opis_formata()
        {
            foreach (var (s, od) in new[] { ("GK30", "ste.obr06@gmail.com"), ("55_16261", "filep.veronika@rthkft.hu"), ("CIM COAL 16", "tibor.tasnadi@pspcargo.ro"),
                                           ("GIPS", "bosstradezonasabac@gmail.com"), ("cuv za 1 vagon", "strahinja.tadic@pansped.rs"), ("CUV_257", "stefan.janjic@eurologsystem.rs") })
            {
                var i = Mail(s, od);
                foreach (var n in Nalazi(i).Where(n => (n.Dopuna ?? "").Length > 0))
                {
                    Assert.IsFalse(Regex.IsMatch(n.Dopuna, @"naziv, ulica i broj|ulica i broj|<|>|\bnpr\.|\be\.g\.|street and number", RegexOptions.IgnoreCase), s + " " + n.Pravilo + ": „" + n.Dopuna + "“");
                    Assert.IsFalse(Pravila.Dopune.OpisFormata.IsMatch(n.Dopuna), s + " " + n.Pravilo + ": „" + n.Dopuna + "“");
                    // v2.8.2: brojevi propisanog oblika se ne izmišljaju („15890-0“ je pravi broj drugog lista)
                    Assert.IsFalse(n.Dopuna.Contains("15890-0") || n.Dopuna.Contains("16350-1") || Regex.IsMatch(n.Dopuna, @"(-|\s)08$"), s + " " + n.Pravilo + ": „" + n.Dopuna + "“");
                }
            }
            var gk = Mail("GK30", "ste.obr06@gmail.com");
            // v2.8.4: r62 preduzeće 3212 upisano anotacijom – nedostaje samo broj otpravljanja
            Assert.AreEqual("XXXXX-X", Nalazi(gk).Single(n => n.Pravilo == "R62").Dopuna, "r62 preduzeće 3212 upisano anotacijom, broj kao šablon");
            Assert.AreEqual("MM-DD-XX", Nalazi(gk).Single(n => n.Pravilo == "R16").Dopuna, "datum nije u dokumentima (ne iz maila), sat XX");
            Assert.AreEqual("XXXX-XX-XX", Nalazi(gk).Single(n => n.Pravilo == "R29").Dopuna);
            var r62 = Nalazi(gk).Single(n => n.Pravilo == "R62").DopunaPoPoljima;
            Assert.AreEqual("XXXXX-X", r62["62.4"]); Assert.AreEqual(1, r62.Count, "zemlja, stanica i preduzeće su upisani (r62 preduzeće 3212 upisano anotacijom)");
            // 55 16261: datum iz r29 lista („Kiskoros, 13.11.2026“)
            Assert.AreEqual("11-13-XX", Nalazi(Mail("55_16261", "filep.veronika@rthkft.hu")).Single(n => n.Pravilo == "R16").Dopuna);
            Assert.AreEqual("XXXXX-X", Pravila.Sablon.BrojOtpravljanja); Assert.AreEqual("XX XXXXX-X", Pravila.Sablon.Stanica);
            Assert.AreEqual("XX XX XXXX XXX-X", Pravila.Sablon.BrojKola); Assert.AreEqual("26RSXXXXXXXXXXXXX", Pravila.Sablon.Mrn);
            // GIPS r1: „RS, XXXX“ (zemlja iz „21000 Novi Sad“; v2.9.2 (3.5): kontakt nikad izmišljeno ime); r24: „2520“ precrtano, „252010“ pored
            var g = Mail("GIPS", "bosstradezonasabac@gmail.com", original: true, naslov: "IZVOZ");
            Assert.AreEqual("RS, XXXX", Nalazi(g).Single(n => n.Pravilo == "R1").Dopuna);
            var nhm = Nalazi(g).Single(n => n.Pravilo == "R24-NHM6");
            Assert.AreEqual("252010", nhm.Dopuna); Assert.AreEqual("2520", nhm.Pogresno); Assert.IsTrue(nhm.DopunaSigurna);
            // opis formata se nikad ne upisuje
            Assert.AreEqual("", Pravila.Dopune.Ocisti("naziv, ulica i broj, poštanski broj i mesto, ISO šifra zemlje (npr. RS)"));
            Assert.AreEqual("", Pravila.Dopune.Ocisti("<šifra> Spisak kola"));
            Assert.AreEqual("11-13-08", Pravila.Dopune.Ocisti("11-13-08"));
        }

        [TestMethod, TestCategory("Regresija")]
        public void Primer_krug_sa_brojem_iz_tabele_za_svaki_nalaz_koji_nije_interni()
        {
            foreach (var (s, od) in new[] { ("GK30", "ste.obr06@gmail.com"), ("55_16261", "filep.veronika@rthkft.hu"), ("GIPS", "bosstradezonasabac@gmail.com") })
            {
                var (r, pr, en) = Primer(s, od);
                var tabela = Izvestaj.RedoviIspravki(r, pravila, en).Select((x, k) => (x, greska: true, broj: k + 1))
                    .Concat(Izvestaj.RedoviNapomena(r, pravila, en).Select((x, k) => (x, greska: false, broj: k + 1))).Where(x => !x.x.N.SamoInterno).ToList();
                foreach (var t in tabela)
                {
                    var u = pr.Upisi.FirstOrDefault(x => x.Greska == t.greska && x.Broj == t.broj && x.Rubrika == t.x.N.Rubrika);
                    Assert.IsNotNull(u, s + ": " + (t.greska ? "ispravka " : "napomena ") + t.broj + " (r" + t.x.N.Rubrika + ") nema oznaku u primeru\n" + string.Join("\n", pr.Upisi));
                    if (!u.Mesto.StartsWith("bez rubrike")) Assert.IsTrue(u.Krug.Width > 0, s + ": krug za " + u);
                }
                // nalazi „samo interno“ nisu u primeru; krugovi ne prekrivaju upisane dopune
                var interni = r.Listovi.SelectMany(l => l.Nalazi).Where(n => n.SamoInterno).Select(n => n.Pravilo + "|" + n.Rubrika).ToList();
                var javni = r.Listovi.SelectMany(l => l.Nalazi).Where(n => !n.SamoInterno).Select(n => n.Pravilo + "|" + n.Rubrika).ToList();
                Assert.IsFalse(pr.Upisi.Any(u => interni.Contains(u.Pravilo + "|" + u.Rubrika) && !javni.Contains(u.Pravilo + "|" + u.Rubrika)), s + ": interni nalaz u primeru");
                foreach (var u in pr.Upisi.Where(x => x.Tekst.Width > 0))
                    Assert.IsFalse(pr.Upisi.Any(x => x.Krug.Width > 0 && x.Krug.IntersectsWith(u.Tekst)), s + ": krug preko dopune " + u);
                // dopuna je u svojoj rubrici (ili na margini kada ne staje)
                foreach (var u in pr.Upisi.Where(x => x.Tekst.Width > 0))
                    Assert.IsTrue(RectangleF.Inflate(u.Okvir, 2, 2).Contains(u.Tekst), s + ": dopuna van rubrike " + u);
                // v2.8.2: krug je uvek u svojoj rubrici
                foreach (var u in pr.Upisi.Where(x => x.Krug.Width > 0))
                    Assert.IsTrue(RectangleF.Inflate(u.Okvir, 1, 1).Contains(u.Krug), s + ": krug van rubrike " + u);
            }
        }

        [TestMethod, TestCategory("Regresija")]
        public void Primer_strana_ostaje_A4()
        {
            foreach (var (s, od) in new[] { ("GK30", "ste.obr06@gmail.com"), ("55_16261", "filep.veronika@rthkft.hu") })
            {
                var (_, pr, _) = Primer(s, od);
                foreach (var png in pr.Strane)
                    using (var ms = new MemoryStream(png))
                    using (var b = new Bitmap(ms))
                        Assert.IsTrue((b.Width == 1240 && b.Height == 1754) || (b.Width == 1754 && b.Height == 1240), s + ": " + b.Width + "×" + b.Height);
            }
        }

        [TestMethod, TestCategory("Regresija")]
        public void R1_ISO_zemlja_u_nazivu_firme_nije_zemlja()
        {
            // „Louis Dreyfus Company Hungary Kft. / 1134 Budapest Váci út 23-27“ – zemlje u adresi nema: greška R1/R4, dopuna HU
            var i = Mail("55_16261", "filep.veronika@rthkft.hu");
            Assert.IsFalse(Nalazi(i).Any(n => n.Pravilo == "R1-ISO" || n.Pravilo == "R4-ISO"), Opis(i));
            var r1 = Nalazi(i).Single(n => n.Pravilo == "R1");
            StringAssert.Contains(r1.Detalj, "country");
            StringAssert.StartsWith(r1.Dopuna, "HU");
            Assert.IsTrue(Provere.ZemljaUAdresi(new Model.Strana { Naziv = "Firma d.o.o.", Tekst = "Firma d.o.o.\nBulevar 1, 21000 Novi Sad, Serbia", NazivZemlje = "Serbia" }));
            Assert.IsFalse(Provere.ZemljaUAdresi(new Model.Strana { Naziv = "Louis Dreyfus Company Hungary Kft.", Tekst = "Louis Dreyfus Company Hungary Kft.\n1134 Budapest Váci út 23-27", NazivZemlje = "Hungary" }));
        }

        [TestMethod]
        public void Pasus_o_primeru_posle_tabele_sa_brojevima_u_krugovima()
        {
            var pr = new PrimerRezultat { Ime = "PRIMER_1.pdf", Pdf = new byte[] { 1 } };
            StringAssert.Contains(Izvestaj.PasusPrimera(pr, false), "Brojevi u krugovima odgovaraju brojevima u tabeli iznad.");
            StringAssert.Contains(Izvestaj.PasusPrimera(pr, false), "Podaci koje ste upisali su ostali isti, a crvenom bojom smo dopisali ono što nedostaje ili ispravili ono što nije dobro.");
            StringAssert.Contains(Izvestaj.PasusPrimera(pr, true), "The numbers in circles match the numbers in the table above.");
            StringAssert.Contains(Izvestaj.PasusPrimera(pr, true), "Your entries are unchanged; what is missing or incorrect has been added or corrected in red.");
            var i = Mail("GK30", "ste.obr06@gmail.com");
            var telo = Izvestaj.Telo(i.Provera.BezInternih(), pravila, true, null, null, Izvestaj.PasusPrimera(pr, true));
            Assert.IsTrue(telo.IndexOf("The numbers in circles", StringComparison.Ordinal) > telo.IndexOf("CORRECTIONS REQUIRED", StringComparison.Ordinal), "pasus je posle tabele");
        }

        [TestMethod]
        public void CIT7_carinsko_podrucje_deljenje_i_r7()
        {
            var lista = new ZapisLista { Fajl = "CIM.pdf", Zemlja = "72", Stanica = "11000", Preduzece = "3212", Broj = "12345-6", R1 = "Firma A d.o.o., Beograd", R4 = "Primalac B, Wien", R10 = "Wien", R12 = "81 01234-5", R7 = "Primalac nema pravo raspolaganja", Kola = { "338407649205", "338407648942", "338407649007" }, IzIstogMaila = true };
            var z = new Cit7Zahtev { Zemlja = "72", Stanica = "11000", Preduzece = "3212", Broj = "12345-6", Primalac = "Primalac B", MestoIzdavanja = "Wien", StanicaIzdavanja = "81 01234-5", Podnosilac = "Firma A d.o.o.", Napomene = "Box 10: Beograd\nBox 12: 72 12345-6" };
            z.Izmene = Citanje.Cit7Citac.Izmene(z.Napomene);
            var r = new RezultatProvere { Dokumenti = { new Dokument { Tip = TipDokumenta.Cit7, Fajl = "CIT7.pdf", Cit7 = z } } };
            var c = Cit7Provera.Proveri(r, new List<ZapisLista> { lista }, new DateTime(2026, 11, 13), "x").Single();
            Assert.IsNotNull(c.Veza);
            Assert.AreEqual("pošiljalac", c.Podnosilac);
            Assert.IsTrue(c.Upozorenja.Any(x => x.Contains("menja carinsko područje") && x.Contains("potreban pristanak carinarnice")), string.Join("\n", c.Upozorenja));
            Assert.IsFalse(c.Upozorenja.Any(x => x.Contains("Primalac nema pravo")), "r7 to navodi");

            // deljenje: CIT 7 navodi samo deo kola sa lista
            var z2 = new Cit7Zahtev { Primalac = "Primalac B", MestoIzdavanja = "Wien", StanicaIzdavanja = "81 01234-5", BrojeviKola = { "338407649205" }, Izmene = { new Cit7Izmena { Rubrika = "10", Vrednost = "Linz" } } };
            var r2 = new RezultatProvere { Dokumenti = { new Dokument { Tip = TipDokumenta.Cit7, Fajl = "CIT7.pdf", Cit7 = z2 } } };
            var c2 = Cit7Provera.Proveri(r2, new List<ZapisLista> { lista }, new DateTime(2026, 11, 13), "x").Single();
            Assert.IsTrue(c2.Upozorenja.Any(x => x.Contains("pošiljka deli")), string.Join("\n", c2.Upozorenja));
        }
    }
}
