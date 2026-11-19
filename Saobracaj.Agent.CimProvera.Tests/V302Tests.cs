using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Baza;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>v3.0.2 – kućice sa X sa slike, vrsta dokumenta, KP tolerancija, CIT 18, regresije u masama, odluke 19.11.</summary>
    [TestClass]
    public class V302Tests
    {
        private const string Share = @"\\192.168.1.6\Cloude\Agenti\CimProvera\testovi\v302";
        private static string Folder => Environment.GetEnvironmentVariable("CIMPROVERA_TESTOVI") is string f && f.Length > 0 ? Path.Combine(f, "v302") : Share;
        private static PravilaSkup pravila;
        private static readonly Dictionary<string, Ishod> kes = new Dictionary<string, Ishod>(StringComparer.OrdinalIgnoreCase);

        [ClassInitialize]
        public static void Init(TestContext _)
        {
            var put = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pravila", "cim_validation_rules.json");
            pravila = PravilaSkup.IzTeksta(File.ReadAllText(put), put);
        }

        private static string Fajl(string ime)
        {
            var p = Path.Combine(Folder, ime);
            if (!File.Exists(p)) Assert.Inconclusive("Nema primera: " + p);
            return p;
        }

        private static Ishod Proveri(string ime, string posiljalac = "posiljalac@firma.rs")
        {
            lock (kes)
            {
                if (kes.TryGetValue(ime + "|" + posiljalac, out var r)) return r;
                var p = Fajl(ime);
                var cfg = RegresijaTests.Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje302"));
                var pr = PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null);
                using (var c = new LokalniCitac(cfg, null, pr.Kp))
                    r = new Obrada(pr, c, null, cfg.PragPouzdanosti).Proveri(new[] { (Path.GetFileName(p), File.ReadAllBytes(p)) }, posiljalac, false, new DateTime(2026, 11, 17), false);
                return kes[ime + "|" + posiljalac] = r;
            }
        }

        private static IEnumerable<Nalaz> Svi(Ishod i) => i.Provera.Listovi.SelectMany(l => l.Nalazi).SelectMany(n => new[] { n }.Concat(n.Takodje));
        private static string Opis(Ishod i) => string.Join("\n", Svi(i).Select(n => n.Tezina + " " + n.Pravilo + " r" + n.Rubrika + (n.SamoInterno ? " INT" : "") + ": " + n.Tekst));

        // ------------------------------------------------------------------------------------------- 2. kućice sa X (slika)

        [TestMethod, TestCategory("Regresija")]
        public void Kucica_CUV_sa_vektorskim_X_u_tekstualnom_PDF_je_CUV_bez_KP7_KP3_KP57_R30()
        {
            foreach (var ime in new[] { "Bor Teretna-Zabrze CUV.pdf", "CUV 26X609.pdf" })
            {
                var i = Proveri(ime);
                var l = i.Provera.Listovi.First();
                Assert.IsTrue(l.Cim.R30.Cuv, ime + ": X u kućici CUV (obrazac Unicom Tranzit, X nacrtan vektorom)");
                Assert.IsFalse(l.Cim.R30.Cim, ime);
                Assert.AreEqual("CUV", l.Cim.Obrazac, ime);
                foreach (var pr in new[] { "KP7-PLATILAC", "KP3", "KP57", "R30" })
                    Assert.IsFalse(Svi(i).Any(n => n.Pravilo == pr && !n.SamoInterno), ime + ": ne sme " + pr + "\n" + Opis(i));
            }
        }

        [TestMethod, TestCategory("Regresija")]
        public void Kucica_CIM_sa_X_na_obrascu_TCDD_strana_1_i_2()
        {
            var i = Proveri("CIM WAGON 020-1 stampa.pdf");
            var listovi = i.Provera.Listovi.Concat(i.Provera.KopijeListova).ToList();
            Assert.IsTrue(listovi.Count(l => l.Cim.R30.Cim) >= 2, "X u kućici CIM (UID 114): " + string.Join(", ", listovi.Select(l => l.Oznaka + " cim=" + l.Cim.R30.Cim)));
            Assert.IsFalse(Svi(i).Any(n => n.Pravilo == "R30" && !n.SamoInterno), "R30 samo INTERNO\n" + Opis(i));
        }

        [TestMethod]
        public void Vrsta_dokumenta_nesigurna_bez_pravila_koja_zavise_od_vrste()
        {
            var s = new MotorTests.ScenarioJavni { Fajl = "CUV 26X609.pdf" };
            s.C.R30 = new OznakaDokumenta { Cim = true, Status = StatusRubrike.Popunjeno, Pouzdanost = 0.95 };   // X u CIM, a ime fajla CUV
            var r = s.Proveri();
            var svi = r.Listovi.SelectMany(l => l.Nalazi).ToList();
            Assert.IsTrue(svi.Any(n => n.Pravilo == "VRSTA-DOKUMENTA" && n.SamoInterno), string.Join(" | ", svi));
            foreach (var pr in new[] { "R30", "KP7-PLATILAC", "KP3", "KP57" }) Assert.IsFalse(svi.Any(n => n.Pravilo == pr && !n.SamoInterno), pr);
        }

        [TestMethod]
        public void KP57_nema_kada_je_KP_prvi_prevoznik_ili_izdavalac()
        {
            var s = new MotorTests.ScenarioJavni();
            s.C.R57 = new PrevozniciRubrika { Status = StatusRubrike.Popunjeno, Tekst = "2154 CD Cargo", Pouzdanost = 0.95, Prevoznici = { new Prevoznik { Sifra = "2154", Naziv = "CD Cargo" } } };
            // IspravanCim: r62 = 3212 (KP izdavalac), r16 Niš – KP je ugovorni prevoznik (r58a), ne u r57
            Assert.IsFalse(s.Proveri().Listovi.SelectMany(l => l.Nalazi).Any(n => n.Pravilo == "KP57"));
        }

        // ------------------------------------------------------------------------------------------- 1.2 KP3 / 3. KP tolerancija

        [TestMethod]
        public void KP_naveden_uz_OCR_toleranciju()
        {
            foreach (var t in new[] { "IŽS: Kombinovani Prevoz 3212", "12S: Kombinovani 212", "SRB: KP 32I2", "KP UIC 3Z12", "Комбиновани превоз", "KP 3213 Ruma - Šid" })
                Assert.IsTrue(pravila.Kp.NavedenTolerantno(t), t);
            foreach (var t in new[] { "Lokorail 3080", "HU: RCH 2155", "12S: 212" })
                Assert.IsFalse(pravila.Kp.NavedenTolerantno(t), t);
        }

        [TestMethod]
        public void KP3_KP_u_r57_ili_r7_a_ne_u_r13_upozorenje_ublazeno_nigde_greska()
        {
            var s = new MotorTests.ScenarioJavni();
            s.C.R13 = MotorTests.Popuni(new ListaRubrika(), "1 via Kelebia - Subotica");
            var n = s.Proveri().Listovi.SelectMany(l => l.Nalazi).Single(x => x.Pravilo == "KP3");
            Assert.AreEqual(Tezina.Upozorenje, n.Tezina); Assert.IsTrue(n.Ublazeno);
            StringAssert.Contains(n.Tekst, "navesti KP i u rubrici 13 (tačka 3)");
            // KP nigde (r57, r58a, r7, r62)
            var b = new MotorTests.ScenarioJavni { Rezim = "spoljni" };
            b.C.R13 = MotorTests.Popuni(new ListaRubrika(), "1 via Kelebia - Subotica");
            b.C.R57 = MotorTests.Popuni(new PrevozniciRubrika { Prevoznici = { new Prevoznik { Sifra = "2155", Naziv = "RCH" } } }, "2155 RCH");
            b.C.R58a = MotorTests.Popuni(new UgovorniPrevoznik { Sifra = "2155", Naziv = "RCH" }, "2155 RCH");
            b.C.R62 = MotorTests.Popuni(new Identifikacija { Zemlja = "55", Stanica = "123456", Preduzece = "2155", Broj = "12345-6" });
            b.C.R7 = MotorTests.Popuni(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "16", Tekst = "Platilac za deonicu SRB: Lokomotiva d.o.o." } } }, "16 Platilac za deonicu SRB: Lokomotiva d.o.o.");
            Assert.AreEqual(Tezina.Greska, b.Proveri().Listovi.SelectMany(l => l.Nalazi).Single(x => x.Pravilo == "KP3").Tezina);
        }

        // ------------------------------------------------------------------------------------------- 1.3 R29 / R62 po overi

        [TestMethod]
        public void R29_i_R62_radna_verzija_upozorenje_overen_greska()
        {
            var s = new MotorTests.ScenarioJavni();
            s.C.R29 = new MestoDatum { Status = StatusRubrike.Prazno };
            s.C.R62.Broj = ""; s.C.R62.Preduzece = "";
            var svi = s.Proveri().Listovi.SelectMany(l => l.Nalazi).ToList();
            foreach (var pr in new[] { "R29", "R62" })
            {
                var n = svi.First(x => x.Pravilo == pr && !x.SamoInterno);
                Assert.AreEqual(Tezina.Upozorenje, n.Tezina, pr);
                StringAssert.Contains(n.Tekst, "Posle overe (pečat prevoznika) nedostatak će se smatrati greškom.", pr);
            }
            var o = new MotorTests.ScenarioJavni();
            o.C.R29 = new MestoDatum { Status = StatusRubrike.Prazno }; o.C.ElektronskiIspis = true; o.C.NapomenaIspisa = true;
            Assert.AreEqual(Tezina.Greska, o.Proveri().Listovi.SelectMany(l => l.Nalazi).First(x => x.Pravilo == "R29").Tezina);
        }

        // ------------------------------------------------------------------------------------------- 4. CIT 18

        [TestMethod, TestCategory("Regresija")]
        public void CIT18_samo_INTERNO_otkacena_kola_ne_ide_posiljaocu()
        {
            var i = Proveri("CIT 18 31555958441-4.pdf");
            Assert.IsTrue(i.Provera.Listovi.All(l => l.Cit18), Opis(i));
            Assert.IsFalse(Svi(i).Any(n => !n.SamoInterno && (n.Tezina == Tezina.Greska || n.Tezina == Tezina.Upozorenje)), Opis(i));
            StringAssert.StartsWith(Svi(i).Single(n => n.Pravilo == "CIT18").Tekst, "CIT 18 – otkačena kola");
            Assert.AreEqual(Izvestaj.RazlogRucna, Izvestaj.RazlogInternog(i.Provera, pravila));
            foreach (var z in new[] { "Accompanying document", "Feuille d'accompagnement", "Begleitschein" }) Assert.IsTrue(Provere.JeCit18("x.pdf", z), z);
        }

        [TestMethod, TestCategory("Regresija")]
        public void CIT18_mase_neto_tara_bruto_ostaju()
        {
            // v3.0.3 (2.1, UID 184): u 3.0.1 41,05 / 21,9 / 62,95 t; u 3.0.2 je ostao samo bruto
            var i = Proveri("CIT 18 31555958441-4.pdf");
            var l = i.Provera.Listovi.Single();
            var p = PosaoIzDokumenata.Napravi(l.Cim, l.Dokument, l, new Poklapanje(UpisUBazuTests.Sif()), pravila.Kp, "Status najave_koks za Bor", "x@y.pl");
            Console.WriteLine("r25: " + l.Cim.R25.Tekst + " | obrazac " + l.Cim.Obrazac + " | " + string.Join("; ", p.Napomene));
            Assert.AreEqual(41.05m, p.V["Neto1"]); Assert.AreEqual(21.9m, p.V["Tara1"]); Assert.AreEqual(62.95m, p.V["Bruto1"]);
            Assert.IsFalse(p.Prazna, "CIT 18 natovarenih kola koksa nije prazan");
        }

        // ------------------------------------------------------------------------------------------- 1.1 (3.7) neto intermodalnih spiskova

        private static SpisakKola SpisakHer(string folder)
        {
            var docs = Directory.GetFiles(Path.Combine(Folder, folder)).OrderByDescending(f => f.Contains("HSL"))
                .SelectMany(f => ExcelCitac.Procitaj(Raspakivanje.Raspakuj(Path.GetFileName(f), File.ReadAllBytes(f), new List<string>()).Single(), pravila.Kp))
                .Where(d => d.Tip == TipDokumenta.SpisakKola && d.Spisak.Count > 0).ToList();
            if (docs.Count == 0) Assert.Inconclusive("Nema spiska u " + folder);
            return UpisUBazu.Spoji(docs);
        }

        [TestMethod, TestCategory("Regresija")]
        public void HER_SAD_neto_je_masa_jedinica_sa_robom_bruto_neto_plus_tara_kola()
        {
            var pk = new Poklapanje(UpisUBazuTests.Sif());
            foreach (var (f, neto) in new[] { ("her81", 886.9m), ("her80", 995.1m) })
            {
                var s = SpisakHer(f);
                var p = PosaoIzDokumenata.IzSpiska(s, pk, "", "");
                Assert.IsFalse(p.SpisakNeuskladjen, f + ": " + string.Join(" | ", p.Napomene));
                Assert.AreEqual(neto, p.V["Neto1"], f + " neto = Gewicht der Ladung");
                Assert.AreEqual((decimal)p.V["Neto1"] + (decimal)p.V["Tara1"], (decimal)p.V["Bruto1"], f + " bruto = neto + tara kola");
                Assert.IsTrue(s.NetoRobeKg > 0, f + ": čista roba (Trainlist „Netto“) u napomeni");
                Assert.IsTrue(p.Napomene.Any(x => x.StartsWith("Masa robe (Netto tovarnih jedinica)")), f);
                Assert.IsFalse(p.Napomene.Any(x => x.Contains("bruto bez tare")), f + ": sva kola imaju taru");
            }
        }

        [TestMethod]
        public void Bruto_bez_tare_za_N_kola_oznaka()
        {
            var s = new SpisakKola();
            for (int i = 0; i < 11; i++) s.Kola.Add(new KolaNaSpisku { Broj = "3780495642" + i.ToString("00"), NetoKg = 55000, TaraKg = 35000, Nhm = "860900" });
            s.Kola.Add(new KolaNaSpisku { Broj = "335249560806", NetoKg = 39200, Nhm = "860900" });   // bez tare i bez bruta
            var p = PosaoIzDokumenata.IzSpiska(s, new Poklapanje(UpisUBazuTests.Sif()), "", "");
            Assert.IsTrue(p.Napomene.Any(x => x.Contains("bruto bez tare za 1 kola")), string.Join(" | ", p.Napomene));
        }

        // ------------------------------------------------------------------------------------------- 5. upis poslova

        [TestMethod]
        public void Spisak_istih_kola_sa_OCR_varijantama_je_isti_spisak_razlicita_kola_se_sabiraju()
        {
            var a = new List<string> { "315655400242", "315655400010", "315655400291", "315655401000" };
            var ocr = new List<string> { "315655400242", "315655400016", "315655400291", "315655407000" };   // OCR: 0 → 6, 1 → 7
            Assert.IsTrue(UpisUBazu.UdeoIstihKola(a, ocr) >= 0.8);
            Assert.AreEqual(0, UpisUBazu.UdeoIstihKola(a, new List<string> { "335278130331", "335278130026" }));
        }

        [TestMethod]
        public void Mase_r25_sa_oznakama_N_T_B_i_OCR_oblicima()
        {
            var c = new CimList();
            c.R25 = new Masa { Status = StatusRubrike.Popunjeno, Tekst = "T: 647,000\nN: 1.845,680\nB: 2.492,680", Pouzdanost = 0.9 };
            var (t, n, b, _) = PosaoIzDokumenata.Mase(c);
            Assert.AreEqual(1845680, n); Assert.AreEqual(647000, t); Assert.AreEqual(2492680, b);
            c.R25 = new Masa { Status = StatusRubrike.Popunjeno, Tekst = "Tara:563.150 k\nNelio 1.824.000 kg\nBruio:2.447,150 kg", Pouzdanost = 0.9 };
            c.R21 = new NaznacenjeRobe { Status = StatusRubrike.Popunjeno, Tekst = "PSENICA\nTara   563.150 kg\nNetto:1   4.000 kg\nBruto:2.447.150 kg" };
            (t, n, b, _) = PosaoIzDokumenata.Mase(c);
            Assert.AreEqual(1824000, n, "„Nelio“ = neto iz r25, ne šum iz r21"); Assert.AreEqual(563150, t); Assert.AreEqual(2447150, b);
            StringAssert.Contains(PosaoIzDokumenata.NesmislenNeto(3200000, 28, false, null), "više od 100 t po kolima");
        }

        [TestMethod]
        public void Broj_voza_iz_reference_bez_natpisa()
        {
            // v3.0.2 (5.5 → 3.6): referenca se upisuje bez „Train no.“, a broj voza posla mora i dalje da se prepozna
            // (Osnovni 3.0.2, UID 57: spisak voza 256 je bez ovoga otišao u posao voza 258)
            Assert.AreEqual("256", ExcelCitac.BrojVozaIzReference("256F"));
            Assert.AreEqual("258", ExcelCitac.BrojVozaIzReference("Train no: 258F"));
            Assert.AreEqual("47811", ExcelCitac.BrojVozaIzReference("47811"));
            Assert.AreEqual("", ExcelCitac.BrojVozaIzReference("DOSNISBAR26"));
            Assert.AreEqual("", ExcelCitac.BrojVozaIzReference("SBGT26_SRB04"));
            Assert.AreEqual("", ExcelCitac.BrojVozaIzReference("Budamar Coal 17"));
        }

        [TestMethod]
        public void OCR_varijanta_ne_menja_tacan_tekst_i_referenca_bez_natpisa()
        {
            Assert.IsTrue(UpisUBazu.OcrVarijanta("SBGT26_SRB04", "SBGT26_SRBO4"));
            Assert.IsTrue(UpisUBazu.OcrVarijanta("LDC8-HAMM", "LDCB-HAMM"));
            Assert.IsFalse(UpisUBazu.OcrVarijanta("272F", "270F"));
            foreach (var (u, o) in new[] { ("Train no. 256F", "256F"), ("Train no: 258F", "258F"), ("Zug Nr. 47110", "47110"), ("Voz br. 41402", "41402"), ("Vlak č. 1234", "1234"), ("trainhungary 5", "trainhungary 5"), ("SAD HER 79", "SAD HER 79") })
                Assert.AreEqual(o, PosaoIzDokumenata.BezNatpisaVoza(u), u);
        }
    }
}
