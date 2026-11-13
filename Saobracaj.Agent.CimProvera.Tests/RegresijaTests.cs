using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>
    /// Regresioni testovi na ORIGINALNIM PDF-ovima (testovi\ na share-u, ili folder iz CIMPROVERA_TESTOVI): test prolazi samo
    /// ako je sve očekivano prijavljeno i nijedan nalaz sa liste „ne sme“ nije prijavljen. Bez foldera testovi su Inconclusive.
    /// </summary>
    [TestClass]
    public class RegresijaTests
    {
        private const string Share = @"\\192.168.1.6\Cloude\Agenti\CimProvera\testovi";
        private static readonly Dictionary<string, Ishod> Rezultati = new Dictionary<string, Ishod>(StringComparer.OrdinalIgnoreCase);
        private static readonly object Sync = new object();
        private static PravilaSkup pravila;

        private static string Folder => Environment.GetEnvironmentVariable("CIMPROVERA_TESTOVI") is string f && f.Length > 0 ? f : Share;

        internal static AgentConfig Cfg(string stanje)
        {
            var b = AppDomain.CurrentDomain.BaseDirectory;
            var cfg = AgentConfig.Ucitaj();
            cfg.TessdataFolder = Path.Combine(b, "tessdata");
            cfg.SablonFajl = Path.Combine(b, "sablon", "cim_sablon.json");
            cfg.ObrasciFolder = Path.Combine(b, "sablon", "obrasci");
            cfg.PravilaFolder = Path.Combine(b, "pravila");
            cfg.PravilaFajl = "cim_validation_rules.json";
            cfg.StanjeFolder = stanje;
            cfg.RezultatiFolder = "";
            cfg.OcrJezici = "eng+deu+fra+srp_latn+srp";
            cfg.OcrDodatniJezici = "hun+ita+ron+bul+slv+hrv";
            return cfg;
        }

        private static string Putanja(string fajl)
        {
            var p = Path.Combine(Folder, fajl);
            if (!File.Exists(p)) Assert.Inconclusive("Nema testnog dokumenta: " + p);
            return p;
        }

        /// <summary>Jedan dokument = jedan mail (spoljni pošiljalac, datum prijema 10.11.2026). Čita se jednom po pokretanju testova.</summary>
        private static Ishod Proveri(string fajl)
        {
            var p = Putanja(fajl);
            lock (Sync)
            {
                if (Rezultati.TryGetValue(fajl, out var r)) return r;
                var cfg = Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje"));
                pravila = pravila ?? PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null);
                using (var c = new LokalniCitac(cfg, null, pravila.Kp))
                    r = new Obrada(pravila, c, null, cfg.PragPouzdanosti).Proveri(new[] { (Path.GetFileName(p), File.ReadAllBytes(p)) }, "posiljalac@gmail.com", false, new DateTime(2026, 11, 10), false);
                Rezultati[fajl] = r;
                return r;
            }
        }

        private static IEnumerable<Nalaz> Svi(Ishod i) => i.Provera.Listovi.SelectMany(l => l.Nalazi).SelectMany(n => new[] { n }.Concat(n.Takodje));

        private static string Opis(Ishod i) => string.Join(Environment.NewLine, i.Provera.Listovi.SelectMany(l => l.Nalazi)
            .Select(n => n.Tezina + " " + n.Pravilo + " r" + n.Rubrika + ": " + n.Detalj + " | " + n.Uputstvo + " | primer: " + n.Primer));

        private static void Ocekuje(Ishod i, string tezina, string rubrika, string pravilo = null, string sadrzi = null, string primer = null)
        {
            var ok = Svi(i).Any(n => (n.Tezina == tezina || n.TezinaPravila == tezina) && n.Rubrika == rubrika && (pravilo == null || n.Pravilo == pravilo)
                && (sadrzi == null || (n.Detalj + " " + n.Tekst).IndexOf(sadrzi, StringComparison.OrdinalIgnoreCase) >= 0)
                && (primer == null || (n.Primer ?? "").IndexOf(primer, StringComparison.OrdinalIgnoreCase) >= 0));
            Assert.IsTrue(ok, "Očekivano: " + tezina + " r" + rubrika + " " + pravilo + " „" + sadrzi + "“" + (primer != null ? " primer „" + primer + "“" : "") + Environment.NewLine + Opis(i));
        }

        private static void NeSme(Ishod i, string pravilo, string rubrika = null, string sadrzi = null)
        {
            var los = Svi(i).FirstOrDefault(n => n.Pravilo == pravilo && (rubrika == null || n.Rubrika == rubrika)
                && (sadrzi == null || (n.Detalj + " " + n.Tekst).IndexOf(sadrzi, StringComparison.OrdinalIgnoreCase) >= 0));
            Assert.IsNull(los, "Ne sme: " + pravilo + " r" + rubrika + " „" + sadrzi + "“, a prijavljeno je: " + los + Environment.NewLine + Opis(i));
        }

        private static CimList List(Ishod i) => i.Provera.Listovi.Single().Cim;

        // ------------------------------------------------------------------------------------------- HOLCIM

        [TestMethod, TestCategory("Regresija")]
        public void Holcim_opet()
        {
            var i = Proveri("HOLCIM (4)-opet.PDF");
            Assert.AreEqual(1, i.Provera.Listovi.Count, Opis(i));
            Assert.AreEqual("K-504", List(i).Sablon);
            // greške
            Ocekuje(i, Tezina.Greska, "9", "R9", "spisak kola"); Ocekuje(i, Tezina.Greska, "9", "R9", "izvozna"); Ocekuje(i, Tezina.Greska, "9", "R9", "tranzitna");
            Assert.AreEqual(1, i.Provera.Listovi[0].Nalazi.Count(n => n.Rubrika == "9" && n.Tezina == Tezina.Greska), "r9 je jedna stavka");
            Ocekuje(i, Tezina.Preporuka, "24", "R24-NHM6", "(u T1: 262190)", primer: "262190"); NeSme(i, "R24");
            Ocekuje(i, Tezina.Greska, "29", "R29");
            Ocekuje(i, Tezina.Preporuka, "CIT 23", "X29-NHM6", "2620", primer: "262190"); NeSme(i, "X29");
            // v2.7: KP je u r7 naveden kao platilac za RS
            Ocekuje(i, Tezina.Greska, "7", "KP7-NIJE-PLATILAC", "Kombinovani Prevoz");
            // upozorenja
            Ocekuje(i, Tezina.Upozorenje, "21", "R21-MRN", primer: "rok 13.11.2026");
            Ocekuje(i, Tezina.Upozorenje, "20", "X28", "DAP");
            Ocekuje(i, Tezina.Upozorenje, "14", "R14", "broj ponude"); NeSme(i, "G2"); NeSme(i, "G11");
            // EUR.1 nije priložen (str. 3–4 su faktura – faktura ostaje faktura; EUR.1 samo po naslovu „Movement certificate“)
            Ocekuje(i, Tezina.Upozorenje, "9", "X23", "faktura");
            // str. 5: T1 sa nečitljivim naslovom – prepoznata po poljima LRN/UCR/TIR i odredišnoj ispostavi
            Assert.IsTrue(i.Provera.Dokumenti.Any(d => d.Tip == TipDokumenta.TranzitnaDeklaracija && d.Strane.Contains(5)), "str. 5 je tranzitna deklaracija");
            // r58a: pečat koji OCR ne čita – upozorenje, ne greška
            Ocekuje(i, Tezina.Upozorenje, "58a", "R58A", "pečat");
            Assert.IsFalse(i.Provera.Listovi[0].Nalazi.Any(n => n.Rubrika == "58a" && n.Tezina == Tezina.Greska), "r58a nije greška");
            Ocekuje(i, Tezina.Upozorenje, "1", "R1-ISO", "Srbija");
            // ne sme
            NeSme(i, "R24", "24", "prazna"); NeSme(i, "CITLJIVOST", "16"); NeSme(i, "CITLJIVOST", "30"); NeSme(i, "R16"); NeSme(i, "R30");
            NeSme(i, "KP7-PLATILAC"); NeSme(i, "KP7-INFO"); NeSme(i, "KP57"); NeSme(i, "R57"); NeSme(i, "R57-DEONICA");
        }

        [TestMethod, TestCategory("Regresija")]
        public void Holcim_uid1259_isti_rezultat_kao_opet()
        {
            if (!File.Exists(Path.Combine(Folder, "HOLCIM (4).PDF"))) Assert.Inconclusive("Nema HOLCIM (4).PDF u " + Folder);
            var a = Proveri("HOLCIM (4).PDF"); var b = Proveri("HOLCIM (4)-opet.PDF");
            CollectionAssert.AreEqual(a.Provera.Dokumenti.Select(d => d.Tip + ":" + string.Join(",", d.Strane)).ToList(), b.Provera.Dokumenti.Select(d => d.Tip + ":" + string.Join(",", d.Strane)).ToList());
            CollectionAssert.AreEquivalent(Svi(b).Select(n => n.Pravilo + "|" + n.Rubrika).Distinct().ToList(), Svi(a).Select(n => n.Pravilo + "|" + n.Rubrika).Distinct().ToList());
        }

        // ------------------------------------------------------------------------------------------- CIM_258 / CIM_263

        private static void Eurolog(Ishod i)
        {
            Assert.AreEqual(1, i.Provera.Listovi.Count, Opis(i));
            Assert.AreEqual("ZS-EUROLOG", List(i).Sablon);
            Ocekuje(i, Tezina.Greska, "16", "R16", "datum");
            Ocekuje(i, Tezina.Greska, "25", "R25-MASA", "samo oznake"); NeSme(i, "R25");
            Ocekuje(i, Tezina.Greska, "29", "R29");
            NeSme(i, "R14"); // „KP 2026“ je broj – v2.7
            Ocekuje(i, Tezina.Greska, "9", "X26", "nije priložen");
            Ocekuje(i, Tezina.Upozorenje, "18", "R18-SPISAK", "CIT 23");
            NeSme(i, "R57"); NeSme(i, "R57-DEONICA");
            Ocekuje(i, Tezina.Info, "7", "KP7-INFO", "EurologSystem");
            Ocekuje(i, Tezina.Upozorenje, "1", "R1-ISO"); Ocekuje(i, Tezina.Upozorenje, "4", "R4-ISO");
            Ocekuje(i, Tezina.Upozorenje, "9", "G6");
            Ocekuje(i, Tezina.Upozorenje, "9", "X27", "faktura"); Ocekuje(i, Tezina.Upozorenje, "9", "X27", "EUR.1");
            NeSme(i, "R24"); NeSme(i, "R30"); NeSme(i, "R12"); NeSme(i, "R7"); NeSme(i, "KP3"); NeSme(i, "KP7-PLATILAC"); NeSme(i, "KP7-NIJE-PLATILAC"); NeSme(i, "KP57"); NeSme(i, "REC1");
        }

        [TestMethod, TestCategory("Regresija")]
        public void Cim_258()
        {
            var i = Proveri("UPDATED 1.CIM_258_1.pdf");
            Eurolog(i);
            var c = List(i);
            var r1 = Regex.Replace(c.R1.Tekst, @"\s+", " ");
            foreach (var x in new[] { "EAST GRAIN D.O.O.", "Jove Vučerića 33", "21000 Novi Sad, Srbija", "113940170" }) StringAssert.Contains(r1, x);
            Assert.IsFalse(r1.Contains("MOLINO"), "r1 ne sme da sadrži tekst iz r4: " + r1);
            Assert.AreEqual("100100", c.R24.Stavke.FirstOrDefault()?.Sifra);
            Assert.AreEqual("83053199", Regex.Replace(c.R12.Tekst, @"\D", ""));
            StringAssert.Contains(c.R8.Tekst, "258F");
        }

        [TestMethod, TestCategory("Regresija")]
        public void Cim_263()
        {
            var i = Proveri("1.CIM_263_1.pdf");
            Eurolog(i);
            var c = List(i);
            var r1 = Regex.Replace(c.R1.Tekst, @"\s+", " ");
            foreach (var x in new[] { "AGROGLOBE D.O.O.", "Bulevar Oslobodjenja 60", "21000 Novi Sad" }) StringAssert.Contains(r1, x);
            Assert.AreEqual("83025155", Regex.Replace(c.R12.Tekst, @"\D", ""));
            StringAssert.Contains(c.R16.SifraStanice + " " + c.R16.Tekst, "23306");
            StringAssert.Contains(c.R8.Tekst, "263F");
        }

        // ------------------------------------------------------------------------------------------- CIM.pdf (vo1621)

        [TestMethod, TestCategory("Regresija")]
        public void Cim_vo1621_cuv_sa_poledjinom()
        {
            var i = Proveri("CIM.pdf");
            Assert.AreEqual(1, i.Provera.Listovi.Count, "jedan list (strana 2 je poleđina): " + Opis(i));
            Assert.AreEqual("CUV", List(i).Obrazac);
            Assert.AreEqual("K-505", List(i).Sablon);
            CollectionAssert.Contains(i.Provera.Listovi[0].Dokument.Strane, 2);
            Ocekuje(i, Tezina.Greska, "16", "R16", "datum");
            Ocekuje(i, Tezina.Greska, "29", "R29");
            NeSme(i, "R57"); NeSme(i, "R57-DEONICA");
            Ocekuje(i, Tezina.Upozorenje, "7", "KP7-NEJASNO", "16.IŽS: KP 3212");
            Ocekuje(i, Tezina.Upozorenje, "9", "G6");
            Ocekuje(i, Tezina.Upozorenje, "1", "R1-ISO");
            NeSme(i, "R22"); NeSme(i, "X5"); NeSme(i, "R23"); NeSme(i, "X1");
            NeSme(i, "X26"); NeSme(i, "G3"); NeSme(i, "X9"); NeSme(i, "X20");
            NeSme(i, "R12"); NeSme(i, "X19"); NeSme(i, "R62"); NeSme(i, "G2"); NeSme(i, "KP7-PLATILAC"); NeSme(i, "KP7-NIJE-PLATILAC"); NeSme(i, "KP57");
            Assert.IsFalse(Svi(i).Any(n => n.Pravilo == "CITLJIVOST" && Regex.IsMatch(n.Rubrika, "^(1|4|7|10|13|14|18|20|21|2[3-9]|30)$")),
                "upozorenja „nečitljivo“ sa poleđine: " + Opis(i));
        }

        // ------------------------------------------------------------------------------------------- Vo 1569 Ruma – San Zeno (nezavisni K-504)

        [TestMethod, TestCategory("Regresija")]
        public void Ruma_SanZeno_vo1569_otpad()
        {
            var i = Proveri(@"nezavisni\Documentation 22 wagons Ruma-San Zeno, RuSz008-26 31.10.2026. Kosanovic.pdf");
            Assert.AreEqual(1, i.Provera.Listovi.Count, Opis(i));
            Assert.AreEqual("K-504", List(i).Sablon);
            Assert.AreEqual("Pošiljka: 72 16550-6 3212 15690-0", Izvestaj.Identifikacija(i.Provera, false));
            // klasifikacija strana
            var tipovi = i.Provera.Dokumenti.ToDictionary(d => d.Strane[0], d => d.Tip);
            Assert.AreEqual(TipDokumenta.IzvoznaDeklaracija, tipovi[1]); Assert.AreEqual(TipDokumenta.Faktura, tipovi[3]);
            Assert.AreEqual(TipDokumenta.SpisakKola, tipovi[6]); Assert.AreEqual(TipDokumenta.DozvolaOtpad, tipovi[8]);
            Assert.IsFalse(i.Provera.Dokumenti.Any(d => d.Tip == TipDokumenta.TranzitnaDeklaracija || d.Tip == TipDokumenta.Eur1), "nema T1 ni EUR.1: " + string.Join(", ", i.Provera.Dokumenti.Select(d => d.Tip + "[" + string.Join(",", d.Strane) + "]")));
            // greške
            NeSme(i, "KP7-PLATILAC"); NeSme(i, "KP7-NIJE-PLATILAC"); NeSme(i, "KP7-NEJASNO");
            Assert.AreEqual("Platilac za deonicu kroz Srbiju: Lokomotiva doo.", Svi(i).Single(n => n.Pravilo == "KP7-INFO").Tekst);
            Ocekuje(i, Tezina.Greska, "16", "R16", "datum");
            Ocekuje(i, Tezina.Greska, "21", "R21-EWC", "otpad", primer: "12 01 01"); Ocekuje(i, Tezina.Greska, "21", "R21-EWC", primer: "19 12 02");
            Ocekuje(i, Tezina.Greska, "29", "R29", "Ruma"); Ocekuje(i, Tezina.Greska, "29", "R29", "datum ispostavljanja");
            NeSme(i, "R57"); NeSme(i, "R57-DEONICA");
            Assert.AreEqual(3, i.Provera.BrojGresaka, Opis(i));
            // upozorenja
            Ocekuje(i, Tezina.Upozorenje, "1", "R1-ISO", "Srbija"); Ocekuje(i, Tezina.Upozorenje, "4", "R4-ISO", "Italia");
            Ocekuje(i, Tezina.Upozorenje, "9", "G6");
            Ocekuje(i, Tezina.Upozorenje, "9", "X27", "radiološk"); Ocekuje(i, Tezina.Upozorenje, "9", "X27", "Annex VII");
            Ocekuje(i, Tezina.Upozorenje, "20", "X28", "DAP Verona"); Ocekuje(i, Tezina.Upozorenje, "20", "X28", "faktura");
            Ocekuje(i, Tezina.Upozorenje, "21", "R21-MRN", "izvozne deklaracije", primer: "42072/C1/23309/2026");
            Ocekuje(i, Tezina.Upozorenje, "49", "R49");
            Assert.IsTrue(Svi(i).Single(n => n.Pravilo == "R49").SamoInterno, "r49 samo u internoj kopiji");
            Assert.IsFalse(Svi(i.Provera.BezInternih() is var b ? new Ishod { Provera = b } : null).Any(n => n.Pravilo == "R49"), "r49 ne ide spoljnom pošiljaocu");
            // ne sme
            NeSme(i, "X22"); NeSme(i, "R21-MRN", "21", "T MRN (tranzitna"); NeSme(i, "R21-MRN", "21", "E MRN (izvozna"); NeSme(i, "X23", "9", "EUR.1");
            NeSme(i, "X20"); NeSme(i, "X26"); NeSme(i, "G3"); NeSme(i, "X9"); NeSme(i, "X19"); NeSme(i, "R62");
            NeSme(i, "R58A"); NeSme(i, "KP2"); NeSme(i, "CITLJIVOST", "14"); NeSme(i, "CITLJIVOST", "58a"); NeSme(i, "KP5");
            NeSme(i, "R24"); NeSme(i, "R25"); NeSme(i, "R25-MASA"); NeSme(i, "R30"); NeSme(i, "R22"); NeSme(i, "R23"); NeSme(i, "R12");
        }

        // ------------------------------------------------------------------------------------------- HU-RCH fuvarlevél (informativni ispis)

        [TestMethod, TestCategory("Regresija")]
        public void HU_0081264631_informativni_ispis_i_dodatni_list()
        {
            var i = Proveri(@"nezavisni\0081264631 CIM fuvarlevél.pdf");
            Assert.AreEqual(1, i.Provera.Listovi.Count, "str. 2 je dodatni list, ne drugi tovarni list" + Environment.NewLine + Opis(i));
            Assert.AreEqual("HU-RCH", List(i).Sablon);
            Assert.IsTrue(i.Provera.Dokumenti.Any(d => d.Tip == TipDokumenta.DodatniList && d.Strane.Contains(2)), "str. 2 = dodatni_list");
            Assert.AreEqual("1 tovarni list + dodatni list", Izvestaj.OpisListova(i.Provera, false));
            StringAssert.Contains(Izvestaj.Telo(i.Provera, pravila, false), "(1 tovarni list + dodatni list");
            Assert.AreEqual("Pošiljka: 55 12740-7 2155 71163-0", Izvestaj.Identifikacija(i.Provera, false));
            // r7 i r13 dodatnog lista spojeni sa listom
            StringAssert.Contains(List(i).R7.Tekst, "Frachtzahler"); StringAssert.Contains(Tekst(List(i).R13), "KP UIC 3212");
            Assert.IsFalse(i.Provera.Listovi[0].KpRezim, "ugovorni prevoznik je RCH (2155), ne KP");
            // v2.7: platilac za SRB je naveden (Frachtzahler fuer SRB: VTG Rail Logistics Austria) – bez grešaka
            Assert.AreEqual(0, i.Provera.BrojGresaka, Opis(i));
            Ocekuje(i, Tezina.Info, "7", "KP7-INFO", "VTG Rail Logistics Austria");
            // upozorenja
            Ocekuje(i, Tezina.Upozorenje, "21", "R21-MRN99", "26HU4110004THDTAK8", primer: "26HU4110004THDTAK8");
            Ocekuje(i, Tezina.Upozorenje, "9", "X27", "faktura"); Ocekuje(i, Tezina.Upozorenje, "9", "X27", "tranzitna");
            Ocekuje(i, Tezina.Upozorenje, "20", "R20-INCO-ZASTARELO", "DAF"); Ocekuje(i, Tezina.Upozorenje, "20", "R20-INCO-ZASTARELO", "„711“");
            Ocekuje(i, Tezina.Upozorenje, "–", "G12", "informativni ispis");
            // ne sme
            NeSme(i, "KP57"); NeSme(i, "KP3"); NeSme(i, "R1-ISO"); NeSme(i, "R4-ISO"); NeSme(i, "R10"); NeSme(i, "G6");
            NeSme(i, "G2"); NeSme(i, "G11"); NeSme(i, "R14"); NeSme(i, "R16"); NeSme(i, "R18-SPISAK"); NeSme(i, "KP7-PLATILAC"); NeSme(i, "KP7-NIJE-PLATILAC"); NeSme(i, "X26"); NeSme(i, "X20");
            NeSme(i, "X1"); NeSme(i, "X2"); NeSme(i, "R23"); NeSme(i, "R24"); NeSme(i, "R25"); NeSme(i, "R25-MASA"); NeSme(i, "R29"); NeSme(i, "R30"); NeSme(i, "R57");
            NeSme(i, "R58A"); NeSme(i, "R49"); NeSme(i, "R62"); NeSme(i, "OBRAZAC"); NeSme(i, "R20-INCO");
        }

        private static string Tekst(ListaRubrika r) => (r.Tekst ?? "") + " " + string.Join(" ", r.Stavke.Select(s => s.Sifra + " " + s.Tekst));

        /// <summary>
        /// Nezavisna provera šablona HU-RCH: ostali mađarski listovi iz maila UID 1254 (10.11.2026), iz keša teksta strana
        /// (PDF tekst sa servera, testovi\nezavisni\uid1254-kes). Svaki list + njegova faktura i MRN.pdf = jedan mail.
        /// </summary>
        [DataTestMethod, TestCategory("Regresija")]
        [DataRow("0081264633", 6, "55 12740-7 2155 71166-3", "26HU411000XFQGKFK7")]
        [DataRow("0081264634", 4, "55 12740-7 2155 71167-1", "26HU411000GJMLXSK1")]
        [DataRow("0081264635", 11, "55 12740-7 2155 71165-5", "26HU411000BIFN5EK3")]
        [DataRow("0081264636", 8, "55 12740-7 2155 71164-8", "26HU411000D0ROAZK7")]
        public void HU_uid1254_nezavisni_listovi(string broj, int kola, string identifikacija, string mrn)
        {
            var folder = Path.Combine(Folder, "nezavisni", "uid1254-kes");
            var fajlovi = Directory.Exists(folder) ? Directory.GetFiles(folder, broj + " *.strane.json") : new string[0];
            if (fajlovi.Length == 0) Assert.Inconclusive("Nema keša teksta za " + broj + " u " + folder);
            var cfg = Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje"));
            pravila = pravila ?? PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null);
            var dokumenti = new List<Dokument>();
            using (var c = new LokalniCitac(cfg, null, pravila.Kp))
                foreach (var f in fajlovi.OrderBy(x => x, StringComparer.Ordinal))
                {
                    var ime = Path.GetFileName(f).Replace(".strane.json", "");
                    var strane = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Citanje.Strana>>(File.ReadAllText(f, System.Text.Encoding.UTF8));
                    var rp = c.Klasifikuj(new Prilog { Naziv = ime }, strane);
                    foreach (var d in rp.Dokumenti) { d.Fajl = ime; dokumenti.Add(d); }
                }
            var r = new Motor(pravila).Proveri(new UlazProvere { Dokumenti = dokumenti, ReferentniDatum = new DateTime(2026, 11, 10), Prag = 0.6 });
            var i = new Ishod { Provera = r };
            Assert.AreEqual(1, r.Listovi.Count, Opis(i));
            Assert.AreEqual("HU-RCH", List(i).Sablon);
            Assert.AreEqual("Pošiljka: " + identifikacija, Izvestaj.Identifikacija(r, false));
            var sp = dokumenti.Where(d => d.Tip == TipDokumenta.SpisakKola).SelectMany(d => d.Spisak).ToList();
            Assert.AreEqual(1, sp.Count, "spisak kola (Kocsijegyzék)"); Assert.AreEqual(kola, sp[0].Kola.Count, "broj kola na spisku = r21 (" + kola + " Kocsi)");
            Assert.IsTrue(sp[0].Kola.All(k => k.Nhm.Length == 6 && k.Rid), "NHM i RID uz svaka kola");
            Assert.IsTrue(dokumenti.Any(d => d.Tip == TipDokumenta.TranzitnaDeklaracija), "MRN.pdf = tranzitna deklaracija T2");
            StringAssert.Contains(Tekst(List(i).R13), "KP UIC 3212");
            // v2.7: bez grešaka – platilac za SRB je naveden (VTG)
            Assert.AreEqual(0, r.BrojGresaka, Opis(i));
            Ocekuje(i, Tezina.Info, "7", "KP7-INFO", "VTG");
            // upozorenja
            Ocekuje(i, Tezina.Upozorenje, "–", "G12");
            Ocekuje(i, Tezina.Upozorenje, "21", "R21-MRN99", mrn);
            Ocekuje(i, Tezina.Upozorenje, "20", "R20-INCO-ZASTARELO", "DAF");
            // ne sme
            NeSme(i, "KP57"); NeSme(i, "KP3"); NeSme(i, "R24"); NeSme(i, "R21-DESC"); NeSme(i, "X1"); NeSme(i, "X2", "21"); NeSme(i, "X26"); NeSme(i, "X9");
            NeSme(i, "R62"); NeSme(i, "R58A"); NeSme(i, "R49"); NeSme(i, "R18"); NeSme(i, "R21-WAGON"); NeSme(i, "OBRAZAC"); NeSme(i, "X27", "9", "tranzitna");
        }

        // ------------------------------------------------------------------------------------------- univerzalni CIT šablon

        /// <summary>
        /// Provera samo sa univerzalnim CIT šablonom (bez nacionalnih) – nezavisna provera šablona na dokumentu od koga NIJE napravljen
        /// (šablon je napravljen od LDV CIT 2004; provera na HU CIT 2005 i CIM via Kelebija CIT 2006).
        /// </summary>
        private static Ishod ProveriSamoCit(string fajl)
        {
            var p = Putanja(fajl);
            var folder = Path.Combine(Path.GetTempPath(), "CimProveraTest", "samo-cit");
            Directory.CreateDirectory(folder);
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sablon", "obrasci", "cit-univerzalni.json"), Path.Combine(folder, "cit-univerzalni.json"), true);
            var cfg = Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje-cit"));
            cfg.ObrasciFolder = folder;
            pravila = pravila ?? PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null);
            using (var c = new LokalniCitac(cfg, null, pravila.Kp))
                return new Obrada(pravila, c, null, cfg.PragPouzdanosti).Proveri(new[] { (Path.GetFileName(p), File.ReadAllBytes(p)) }, "posiljalac@gmail.com", false, new DateTime(2026, 11, 12), false);
        }

        [TestMethod, TestCategory("Regresija")]
        public void Stavke_izvestaja_tekst_i_html_isto_LDV()
        {
            // Poređenje sa prvim izveštajem čita stavke iz teksta (ili HTML-a) izveštaja: oba oblika daju iste stavke.
            var r = Proveri(@"nezavisni\LDV NHM 9922 Busca - Carpinis_31 wgs.pdf").Provera;
            // interni oblik za poređenje: samo greške i upozorenja
            var zaPoredjenje = Poredjenje.Stavke(Izvestaj.TekstZaPoredjenje(r, false));
            Assert.AreEqual(r.BrojGresaka, zaPoredjenje.Count(x => x.Vrsta == "greška"), string.Join("\n", zaPoredjenje));
            Assert.AreEqual(r.BrojUpozorenja, zaPoredjenje.Count(x => x.Vrsta == "upozorenje"), string.Join("\n", zaPoredjenje));
            // mail v2.7 (tekst i HTML): iste stavke; napomene uključuju i preporuke
            var izTeksta = Poredjenje.Stavke(Izvestaj.Telo(r, pravila, false));
            Assert.AreEqual(r.BrojGresaka, izTeksta.Count(x => x.Vrsta == "greška"), string.Join("\n", izTeksta));
            var izHtml = Poredjenje.Stavke(Poredjenje.HtmlUTekst(Izvestaj.Html(r, pravila, false)));
            CollectionAssert.AreEquivalent(izTeksta.Select(x => x.Kljuc).ToList(), izHtml.Select(x => x.Kljuc).ToList(), string.Join("\n", izHtml));
            var (nove, nestale) = Poredjenje.Razlike(izTeksta, izHtml);
            Assert.AreEqual(0, nove.Count + nestale.Count);
        }

        [TestMethod, TestCategory("Regresija")]
        public void LDV_Busca_Carpinis_CUV_univerzalni_sablon()
        {
            var i = Proveri(@"nezavisni\LDV NHM 9922 Busca - Carpinis_31 wgs.pdf");
            // 1 list CUV u 5 primeraka (+ 5 poleđina), ne 5 listova
            Assert.AreEqual(1, i.Provera.Listovi.Count, Opis(i));
            Assert.AreEqual("CIT", List(i).Sablon); Assert.AreEqual("CUV", List(i).Obrazac);
            var dok = i.Provera.Listovi[0].Dokument;
            Assert.AreEqual(TipDokumenta.Cuv, dok.Tip);
            CollectionAssert.IsSubsetOf(new[] { 1, 2, 3, 4, 5 }, dok.Strane.ToList(), "strane 2–5 su primerci istog lista: " + dok.Opis);
            Assert.IsFalse(i.Provera.Listovi[0].KpRezim, "ugovorni prevoznik je EVM (3832), ne KP");
            Assert.AreEqual("Pošiljka: 83 00704-7 3832 95147-8", Izvestaj.Identifikacija(i.Provera, false));
            // greške
            Ocekuje(i, Tezina.Greska, "1", "R1", "zemlja");
            Ocekuje(i, Tezina.Greska, "9", "G3", "WAGON LIST");
            Ocekuje(i, Tezina.Upozorenje, "18", "R18-SPISAK", "WAGON LIST"); NeSme(i, "X26");
            Ocekuje(i, Tezina.Greska, "16", "R16", "datum");
            Ocekuje(i, Tezina.Preporuka, "24", "R24-NHM6", "9922", primer: "992200"); NeSme(i, "R24");
            Ocekuje(i, Tezina.Preporuka, "25", "R25-MASA", "praznih kola", primer: "Tara"); NeSme(i, "R25"); Assert.IsFalse(Svi(i).Any(n => n.Pravilo == "R25-MASA" && n.Tezina == Tezina.Greska), "9922: samo preporuka");
            Ocekuje(i, Tezina.Greska, "7", "KP7-PLATILAC", "Payer for SZ is Ferest Rail");
            Assert.AreEqual(4, i.Provera.BrojGresaka, Opis(i));
            // upozorenja
            Ocekuje(i, Tezina.Upozorenje, "4", "R4-ISO", "Romania");
            Ocekuje(i, Tezina.Upozorenje, "21", "X30", "31 wgs");
            Ocekuje(i, Tezina.Upozorenje, "14", "R14");
            Assert.AreEqual(4, i.Provera.BrojUpozorenja, Opis(i));
            // ne sme
            NeSme(i, "KP7-NIJE-PLATILAC"); NeSme(i, "KP7-INFO"); NeSme(i, "KP57"); NeSme(i, "KP3"); NeSme(i, "R30"); NeSme(i, "R57"); NeSme(i, "R57-DEONICA"); NeSme(i, "R12"); NeSme(i, "R20"); NeSme(i, "R29"); NeSme(i, "R62");
            NeSme(i, "R58A"); NeSme(i, "R49"); NeSme(i, "OBRAZAC");
        }

        [TestMethod, TestCategory("Regresija")]
        public void Univerzalni_CIT_nezavisno_HU_0081264631()
        {
            var i = ProveriSamoCit(@"nezavisni\0081264631 CIM fuvarlevél.pdf");
            Assert.AreEqual(1, i.Provera.Listovi.Count, Opis(i));
            Assert.AreEqual("CIT", List(i).Sablon, "samo univerzalni šablon");
            Assert.AreEqual("CIM", List(i).Obrazac, "CIM označen (kvadratić izdanja 2005 je pomeren u odnosu na 2004)");
            Assert.AreEqual("Pošiljka: 55 12740-7 2155 71163-0", Izvestaj.Identifikacija(i.Provera, false));
            // isti suštinski nalazi kao sa nacionalnim šablonom HU-RCH
            Ocekuje(i, Tezina.Info, "7", "KP7-INFO", "VTG Rail Logistics Austria"); NeSme(i, "KP7-PLATILAC"); NeSme(i, "KP7-NIJE-PLATILAC");
            Ocekuje(i, Tezina.Upozorenje, "–", "G12");
            Ocekuje(i, Tezina.Upozorenje, "21", "R21-MRN99", "26HU4110004THDTAK8");
            Ocekuje(i, Tezina.Upozorenje, "20", "R20-INCO-ZASTARELO", "DAF");
            NeSme(i, "OBRAZAC"); NeSme(i, "KP57"); NeSme(i, "KP3"); NeSme(i, "R24"); NeSme(i, "R25"); NeSme(i, "R25-MASA"); NeSme(i, "R29"); NeSme(i, "R10"); NeSme(i, "R1");
        }

        [TestMethod, TestCategory("Regresija")]
        public void Univerzalni_CIT_nezavisno_CIM_via_Kelebija_2006()
        {
            var fajl = Directory.Exists(Path.Combine(Folder, "nezavisni")) ? Directory.GetFiles(Path.Combine(Folder, "nezavisni"), "CIM via Kelebija*.pdf").FirstOrDefault() : null;
            if (fajl == null) Assert.Inconclusive("Nema „CIM via Kelebija - poslednja nova verzija.pdf“ u testovi\\nezavisni");
            var i = ProveriSamoCit(@"nezavisni\" + Path.GetFileName(fajl));
            Assert.IsTrue(i.Provera.Listovi.Count >= 1, Opis(i));
            Assert.IsTrue(i.Provera.Listovi.All(l => l.Cim.Sablon == "CIT" && string.IsNullOrEmpty(l.Cim.NeprepoznatObrazac)), "univerzalni šablon se poravnava (CIT 2006, EN/FR): " + Opis(i));
            NeSme(i, "OBRAZAC");
            // v2.7: platilac Budamar za ceo put, KP naveden kao prevoznik – bez greške u r7
            NeSme(i, "KP7-PLATILAC"); NeSme(i, "KP7-NIJE-PLATILAC");
        }

        // ------------------------------------------------------------------------------------------- KP – negativni

        /// <summary>Kopija lista CIM_258 (stvarno pročitane rubrike) sa izmenom, provera bez ponovnog čitanja.</summary>
        private static RezultatProvere KopijaCim258(Action<CimList> izmena)
        {
            var i = Proveri("UPDATED 1.CIM_258_1.pdf");
            var dok = Json.Iz<Dokument>(Json.U(i.Provera.Listovi[0].Dokument));
            izmena(dok.Cim[0]);
            var u = new UlazProvere { Dokumenti = new List<Dokument> { dok }, ReferentniDatum = new DateTime(2026, 11, 10), Prag = 0.6 };
            return new Motor(pravila).Proveri(u);
        }

        [TestMethod, TestCategory("Regresija")]
        public void Kp_bez_kp_u_r57_i_r58a_daje_KP57_a_r7_bez_kp_nije_KP7()
        {
            var r = KopijaCim258(c =>
            {
                c.R7.Tekst = "7.16 Frachtzahler auf GPR und SŽ-TP ist EurologSystem doo Beograd."; c.R7.Stavke = new List<SifraTekst> { new SifraTekst { Sifra = "16", Tekst = "Frachtzahler auf GPR und SŽ-TP ist EurologSystem doo Beograd." } };
                c.R57.Tekst = "7853 GPRAIL   ŠID/TOVARNIK-DOBOVA"; c.R57.Prevoznici = new List<Prevoznik> { new Prevoznik { Sifra = "7853", Naziv = "GPRAIL", Deonica = "ŠID/TOVARNIK-DOBOVA" } };
                c.R13.Stavke.RemoveAll(s => s.Sifra == "3"); c.R58a = new UgovorniPrevoznik(); c.R62 = new Identifikacija();
            });
            var svi = r.Listovi.SelectMany(l => l.Nalazi).SelectMany(n => new[] { n }.Concat(n.Takodje)).ToList();
            // v2.7: „bez KP u r7“ više nije greška KP7 (KP7 obrisan); platilac za Srbiju ovde nije naveden → KP7-PLATILAC
            Assert.IsFalse(svi.Any(n => n.Pravilo == "KP7" || n.Pravilo == "KP7-NIJE-PLATILAC"), string.Join(" | ", svi));
            Assert.IsTrue(svi.Any(n => n.Pravilo == "KP7-PLATILAC"), string.Join(" | ", svi));
            Assert.IsTrue(svi.Any(n => n.Pravilo == "KP57" && n.Tezina == Tezina.Greska), string.Join(" | ", svi));
        }

        [TestMethod, TestCategory("Regresija")]
        public void Kp_samo_3212_u_r57_je_dovoljno()
        {
            var r = KopijaCim258(c =>
            {
                c.R57.Tekst = "3212   ŠID (U.M. KUKUJEVCI)-ŠID/TOVARNIK"; c.R57.Prevoznici = new List<Prevoznik> { new Prevoznik { Sifra = "3212", Naziv = "", Deonica = "ŠID (U.M. KUKUJEVCI)-ŠID/TOVARNIK" } };
                c.R58a = new UgovorniPrevoznik(); c.R62 = new Identifikacija();
            });
            Assert.IsFalse(r.Listovi.SelectMany(l => l.Nalazi).SelectMany(n => new[] { n }.Concat(n.Takodje)).Any(n => n.Pravilo == "KP57"));
        }

        // ------------------------------------------------------------------------------------------- snimak nalaza (pre/posle izmene pravila)

        private static readonly string[] SviRegresioni =
        {
            "HOLCIM (4)-opet.PDF", "HOLCIM (4).PDF", "UPDATED 1.CIM_258_1.pdf", "1.CIM_263_1.pdf", "CIM.pdf",
            @"nezavisni\Documentation 22 wagons Ruma-San Zeno, RuSz008-26 31.10.2026. Kosanovic.pdf",
            @"nezavisni\0081264631 CIM fuvarlevél.pdf", @"nezavisni\LDV NHM 9922 Busca - Carpinis_31 wgs.pdf",
        };

        /// <summary>Mađarski listovi iz UID 1254 (keš teksta strana): list + faktura + MRN.pdf = jedan mail.</summary>
        private static RezultatProvere HuMail(string broj)
        {
            var folder = Path.Combine(Folder, "nezavisni", "uid1254-kes");
            var fajlovi = Directory.Exists(folder) ? Directory.GetFiles(folder, broj + " *.strane.json") : new string[0];
            if (fajlovi.Length == 0) return null;
            var cfg = Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje"));
            pravila = pravila ?? PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null);
            var dokumenti = new List<Dokument>();
            using (var c = new LokalniCitac(cfg, null, pravila.Kp))
                foreach (var f in fajlovi.OrderBy(x => x, StringComparer.Ordinal))
                {
                    var ime = Path.GetFileName(f).Replace(".strane.json", "");
                    var strane = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Citanje.Strana>>(File.ReadAllText(f, System.Text.Encoding.UTF8));
                    foreach (var d in c.Klasifikuj(new Prilog { Naziv = ime }, strane).Dokumenti) { d.Fajl = ime; dokumenti.Add(d); }
                }
            return new Motor(pravila).Proveri(new UlazProvere { Dokumenti = dokumenti, ReferentniDatum = new DateTime(2026, 11, 10), Prag = 0.6 });
        }

        /// <summary>Folder sa očekivanim snimcima nalaza (u repozitorijumu, pored ovog fajla).</summary>
        private static string FolderSnimka([System.Runtime.CompilerServices.CallerFilePath] string ovaj = "") => Path.Combine(Path.GetDirectoryName(ovaj), "Snimak");

        /// <summary>Snimak jednog dokumenta: broj grešaka/upozorenja i nalazi (težina, pravilo, rubrika), po redu.</summary>
        private static List<string> SnimakNalaza(RezultatProvere r)
        {
            var redovi = new List<string> { "grešaka " + r.BrojGresaka + ", upozorenja " + r.BrojUpozorenja + ", listova " + r.Listovi.Count };
            redovi.AddRange(r.Listovi.SelectMany(l => l.Nalazi).Select(n => n.Tezina + "\t" + n.Pravilo + "\tr" + n.Rubrika));
            return redovi;
        }

        private static IEnumerable<(string ime, Func<RezultatProvere> r)> DokumentiSnimka() =>
            SviRegresioni.Where(f => File.Exists(Path.Combine(Folder, f))).Select(f => (Path.GetFileNameWithoutExtension(f), (Func<RezultatProvere>)(() => Proveri(f).Provera)))
                .Concat(new[] { "0081264633", "0081264634", "0081264635", "0081264636" }
                    .Where(b => Directory.Exists(Path.Combine(Folder, "nezavisni", "uid1254-kes")) && Directory.GetFiles(Path.Combine(Folder, "nezavisni", "uid1254-kes"), b + " *.strane.json").Length > 0)
                    .Select(b => (b + " (UID 1254)", (Func<RezultatProvere>)(() => HuMail(b)))));

        /// <summary>
        /// Snimak nalaza (v2.7.1): nalazi svakog regresionog dokumenta moraju biti isti kao u snimku u repozitorijumu
        /// (Saobracaj.Agent.CimProvera.Tests\Snimak). Namerna promena pravila: CIMPROVERA_SNIMAK_AZURIRAJ=1 prepisuje snimak.
        /// Dokument kojeg nema u folderu testova se preskače (navodi se u poruci); bar jedan mora postojati.
        /// </summary>
        [TestMethod, TestCategory("Snimak")]
        public void Snimak_nalaza_se_nije_promenio()
        {
            var folder = FolderSnimka();
            var azuriraj = Environment.GetEnvironmentVariable("CIMPROVERA_SNIMAK_AZURIRAJ") == "1";
            Directory.CreateDirectory(folder);
            var razlike = new List<string>(); var provereno = new List<string>();
            foreach (var (ime, rez) in DokumentiSnimka())
            {
                var sada = SnimakNalaza(rez());
                var put = Path.Combine(folder, ime + ".txt");
                if (azuriraj || !File.Exists(put)) { File.WriteAllLines(put, sada, new System.Text.UTF8Encoding(false)); Console.WriteLine("snimak " + (azuriraj ? "ažuriran" : "napravljen (prvi put)") + ": " + ime); continue; }
                var bilo = File.ReadAllLines(put).ToList();
                provereno.Add(ime);
                if (!bilo.SequenceEqual(sada))
                    razlike.Add(ime + ":\n  bilo: " + string.Join("\n        ", bilo.Except(sada)) + "\n  sada: " + string.Join("\n        ", sada.Except(bilo)));
            }
            if (provereno.Count == 0 && !azuriraj && razlike.Count == 0) Assert.Inconclusive("Nijedan regresioni dokument nije dostupan u " + Folder);
            Assert.AreEqual(0, razlike.Count, "Nalazi su se promenili (namerna izmena: CIMPROVERA_SNIMAK_AZURIRAJ=1):\n" + string.Join("\n", razlike));
        }

        /// <summary>Izvoz nalaza, teksta i HTML-a izveštaja za tabelu pre/posle (CIMPROVERA_SNIMAK = folder); bez promenljive ne radi ništa.</summary>
        [TestMethod, TestCategory("Snimak")]
        public void Snimak_izvoz_za_poredjenje()
        {
            var izlaz = Environment.GetEnvironmentVariable("CIMPROVERA_SNIMAK");
            if (string.IsNullOrEmpty(izlaz)) return;
            Directory.CreateDirectory(izlaz);
            void Pisi(string ime, RezultatProvere r)
            {
                var redovi = new List<string> { "grešaka " + r.BrojGresaka + ", upozorenja " + r.BrojUpozorenja };
                string J(string t) => (t ?? "").Replace("\n", " / ");
                redovi.AddRange(r.Listovi.Where(l => l.Cim != null).Select(l => "r7: " + J(Tekst(l.Cim.R7)) + " || r13: " + J(Tekst(l.Cim.R13)) + " || r14: " + l.Cim.R14.Tekst
                    + " || r18: " + J(l.Cim.R18.Tekst) + " || r57: " + J(l.Cim.R57.Tekst) + " || r58a: " + J(l.Cim.R58a.Tekst)));
                redovi.AddRange(r.Listovi.SelectMany(l => l.Nalazi).Select(n => n.Tezina + "\t" + n.Pravilo + "\tr" + n.Rubrika + "\t" + n.Detalj
                    + (n.Takodje.Count > 0 ? "\t[takođe: " + string.Join(",", n.Takodje.Select(t => t.Pravilo)) + "]" : "")));
                File.WriteAllLines(Path.Combine(izlaz, ime + ".txt"), redovi, System.Text.Encoding.UTF8);
                File.WriteAllText(Path.Combine(izlaz, ime + ".izvestaj.txt"), Izvestaj.Telo(r, pravila, false), System.Text.Encoding.UTF8);
                File.WriteAllText(Path.Combine(izlaz, ime + ".izvestaj.html"), Izvestaj.Html(r, pravila, false), System.Text.Encoding.UTF8);
            }
            foreach (var f in SviRegresioni.Where(f => File.Exists(Path.Combine(Folder, f))))
                Pisi(Path.GetFileNameWithoutExtension(f), Proveri(f).Provera);
            foreach (var b in new[] { "0081264633", "0081264634", "0081264635", "0081264636" })
                if (HuMail(b) is RezultatProvere r) Pisi(b + " (UID 1254)", r);
        }

        // ------------------------------------------------------------------------------------------- isti rezultat svaki put

        /// <summary>Isti fajl, dva nezavisna čitanja bez keša: isti šablon, isto poravnanje, isti dokumenti i isti nalazi.</summary>
        [DataTestMethod, TestCategory("Regresija")]
        [DataRow("1.CIM_263_1.pdf")]
        [DataRow("HOLCIM (4)-opet.PDF")]
        public void Isti_dokument_dva_puta_isti_rezultat(string fajl)
        {
            var p = Putanja(fajl);
            string Jednom()
            {
                var stanje = Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje-" + Guid.NewGuid().ToString("N"));
                var cfg = Cfg(stanje);
                var pr = PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null);
                try
                {
                    using (var c = new LokalniCitac(cfg, null, pr.Kp))
                    {
                        var prilog = Raspakivanje.Raspakuj(Path.GetFileName(p), File.ReadAllBytes(p), new List<string>()).Single();
                        var strane = c.Strane(prilog);
                        var poravnanja = string.Join(";", strane.Where(s => s.Raster != null).Select(s => c.Rasporedi(prilog, s).Poravnanje?.Opis));
                        var rp = c.Klasifikuj(prilog, strane);
                        var r = new Motor(pr).Proveri(new UlazProvere { Dokumenti = rp.Dokumenti, ReferentniDatum = new DateTime(2026, 11, 10) });
                        return poravnanja + "\n" + Json.U(rp) + "\n" + Izvestaj.Telo(r, pr, false);
                    }
                }
                finally { try { Directory.Delete(stanje, true); } catch { } }
            }
            var a = Jednom(); var b = Jednom();
            Assert.AreEqual(a, b);
        }
    }
}
