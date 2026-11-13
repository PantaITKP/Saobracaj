using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Tovarni list CIM/CUV kao Word dokument (obrazac je slika u zaglavlju, podaci u plutajućim okvirima): pretvaranje u PDF
    /// (Syncfusion DocIO), šablon CIT 2006, dodela po bloku, „X“ preko kvadratića; ručna provera kada pretvaranje nije moguće.
    /// Testovi koji traže pretvaranje se preskaču (Inconclusive) kada Syncfusion ključ nije učitan (Syncfusion.dpapi).
    /// </summary>
    [TestClass]
    public class WordTests
    {
        private const string Share = @"\\192.168.1.6\Cloude\Agenti\CimProvera\testovi";
        private static string Folder => Environment.GetEnvironmentVariable("CIMPROVERA_TESTOVI") is string f && f.Length > 0 ? f : Share;
        private const string CuvIme = "CUV exGK33 Wittenberg - Futog.docx";

        private static PravilaSkup pravila;
        private static LokalniCitac citac;
        private static Ishod cuv;   // jedno čitanje stvarnog CUV_exGK33 za sve testove

        [ClassInitialize]
        public static void Init(TestContext _)
        {
            var cfg = RegresijaTests.Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje"));
            pravila = PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null);
            citac = new LokalniCitac(cfg, null, pravila.Kp);
            WordUPdf.UcitajKljuc();
        }

        [ClassCleanup] public static void Kraj() => citac?.Dispose();

        private static void ZahtevaKljuc()
        {
            if (!WordUPdf.KljucUcitan)
                Assert.Inconclusive("Preskočeno: Syncfusion licencni ključ nije učitan (" + WordUPdf.StanjeKljuca + "). Test traži pretvaranje Word → PDF; "
                                    + "bez ključa Syncfusion u PDF upisuje tekst probne licence, a agent tada Word prilog šalje na ručnu proveru.");
        }

        private static Ishod Cuv()
        {
            ZahtevaKljuc();
            if (cuv != null) return cuv;
            var p = Path.Combine(Folder, "nezavisni", "CUV_exGK33", CuvIme);
            if (!File.Exists(p)) Assert.Inconclusive("Nema " + p);
            return cuv = new Obrada(pravila, citac, null, 0.6).Proveri(new[] { (CuvIme, File.ReadAllBytes(p)) }, "neli.petcu@ro-ats.com", false, new DateTime(2026, 11, 13), true);
        }

        private static CimList List() => Cuv().Provera.Listovi.Single().Cim;

        // ------------------------------------------------------------------------------------------- stvarni CUV_exGK33

        [TestMethod, TestCategory("Regresija")]
        public void Word_bez_teksta_CIM_CUV_obrazac_slika_prepoznat_kao_tovarni_list()
        {
            var r = Cuv().Provera;
            Assert.IsTrue(r.ImaTovarniList);
            var d = r.Dokumenti.Single(x => TipDokumenta.JeTovarniList(x.Tip));
            Assert.AreEqual(TipDokumenta.Cuv, d.Tip);
            Assert.AreEqual("word", d.Izvor);
            Assert.AreEqual("CIT", d.Cim[0].Sablon, "šablon CIT 2006");
            CollectionAssert.Contains(Cuv().OpisPriloga, CuvIme + " – tovarni list CUV (Word)");
            Assert.AreEqual(0, Cuv().Neprepoznati.Count);
        }

        [TestMethod, TestCategory("Regresija")]
        public void X_preko_kvadratica_CUV_daje_r30_CUV()
        {
            var c = List();
            Assert.IsTrue(c.R30.Cuv); Assert.IsFalse(c.R30.Cim);
            Assert.AreEqual("CUV", c.Obrazac);
        }

        [TestMethod, TestCategory("Regresija")]
        public void Pokriven_X_u_r20_se_ne_vidi_i_ne_cita()
        {
            // okvir sa „X“ kod Incoterms je pokriven (ni Word ga ne prikazuje) → r20 bez oznake
            var c = List();
            Assert.AreNotEqual(StanjePolja.Oznaceno, c.R20Incoterms);
            Assert.AreNotEqual(StanjePolja.Oznaceno, c.R20Franko);
        }

        [TestMethod, TestCategory("Regresija")]
        public void Blok_iz_r13_koji_prelazi_u_r14_ostaje_u_r13()
        {
            var c = List();
            var r13 = string.Join(" ", c.R13.Stavke.Select(s => s.Sifra + " " + s.Tekst)) + " " + c.R13.Tekst;
            StringAssert.Contains(r13, "Bad Schandau");
            StringAssert.Contains(r13, "Štúrovo");
            StringAssert.Contains(r13, "KP");
            Assert.IsFalse(c.R14.Popunjena, "r14 je prazna: " + c.R14.Tekst);
        }

        [TestMethod, TestCategory("Regresija")]
        public void NHM_9922_00_je_992200_bez_preporuke_za_6_cifara()
        {
            CollectionAssert.AreEqual(new[] { "992200" }, List().R24.Stavke.Select(x => x.Sifra).ToArray());
            Assert.IsFalse(Cuv().Provera.Listovi.SelectMany(l => l.Nalazi).Any(n => n.Pravilo.StartsWith("R24")), "bez nalaza za r24");
        }

        [TestMethod, TestCategory("Regresija")]
        public void Vrednosti_iz_tekstualnog_sloja()
        {
            var c = List();
            StringAssert.Contains(c.R1.Tekst, "Louis Dreyfus Company Suisse");
            StringAssert.Contains(c.R4.Tekst, "Konzul d.o.o.");
            StringAssert.Contains(Regex.Replace(c.R12.Tekst ?? "", @"\s", ""), "72240036");
            Assert.AreEqual("80", c.R62.Zemlja); Assert.AreEqual("232496", c.R62.Stanica); Assert.AreEqual("5063", c.R62.Preduzece);
            Assert.IsTrue(string.IsNullOrEmpty(c.R62.Broj), "Exp. № je prazan");
            StringAssert.Contains(c.R58a.Naziv + " " + c.R58a.Tekst, "Budamar West");
            Assert.IsFalse(c.R16.Popunjena && !string.IsNullOrEmpty(c.R16.Datum));
        }

        /// <summary>
        /// „CUV_exGK33 (Word)“: referenca iz ručne provere (pravila v2.7.1). Razlike agent/referenca su namerno ostavljene i
        /// prijavljene (r4 greška umesto upozorenja – zemlja potpuno nedostaje; G5 za oblik r7; r62 se kod spoljnog prevoznika
        /// ne proverava) – agent se ne prilagođava na silu.
        /// </summary>
        [TestMethod, TestCategory("Regresija")]
        public void CUV_exGK33_Word_nalazi_prema_referenci()
        {
            var r = Cuv().Provera;
            var svi = r.Listovi.SelectMany(l => l.Nalazi).ToList();
            string Opis() => string.Join("\n", svi.Select(n => n.Tezina + " " + n.Pravilo + " r" + n.Rubrika + ": " + n.Detalj));
            bool Ima(string pravilo, string rub, string tezina) => svi.Any(n => n.Pravilo == pravilo && n.Rubrika == rub && n.Tezina == tezina);
            // greške iz reference
            Assert.IsTrue(Ima("R16", "16", Tezina.Greska), "r16\n" + Opis());
            Assert.IsTrue(Ima("R29", "29", Tezina.Greska), "r29\n" + Opis());
            Assert.IsTrue(svi.Any(n => n.Rubrika == "9" && n.Tezina == Tezina.Greska), "r9 – spisak kola nije priložen\n" + Opis());
            // upozorenja / preporuke iz reference
            Assert.IsTrue(Ima("R1-ISO", "1", Tezina.Upozorenje), "r1 DE\n" + Opis());
            Assert.IsTrue(Ima("R14", "14", Tezina.Upozorenje), "r14\n" + Opis());
            Assert.IsTrue(svi.Any(n => n.Pravilo == "R25-MASA" && n.Tezina == Tezina.Preporuka), "r25 samo preporuka (prazna kola)\n" + Opis());
            Assert.IsTrue(svi.Any(n => n.Rubrika == "4"), "r4 – zemlja nije upisana\n" + Opis());
            // bez nalaza
            foreach (var rub in new[] { "13", "57", "58a", "24", "12", "17", "30" })
                Assert.IsFalse(svi.Any(n => n.Rubrika == rub && n.Tezina != Tezina.Info), "r" + rub + " bez nalaza\n" + Opis());
            Assert.IsFalse(svi.Any(n => n.Pravilo == "CITLJIVOST" || n.Pravilo.StartsWith("KP5")), "bez nečitljivih rubrika i lažnog naziva uz 3212\n" + Opis());
            StringAssert.Contains(Izvestaj.Telo(r, pravila, true) + Izvestaj.Telo(r, pravila, false), "Budamar South, s.r.o.");
        }

        [TestMethod, TestCategory("Regresija")]
        public void PDF_primer_za_Word_tovarni_list()
        {
            var p = Path.Combine(Folder, "nezavisni", "CUV_exGK33", CuvIme);
            var pr = PrimerPdf.Napravi(Cuv().Provera, pravila, false, new[] { (CuvIme, File.ReadAllBytes(p)) });
            Assert.IsTrue(pr.Napravljen, pr.Razlog + " " + pr.Greska);
            Assert.AreEqual(2, pr.Strane.Count);
            Assert.IsTrue(pr.PoListu.Single().ispravki >= 4);
        }

        // ------------------------------------------------------------------------------------------- ručna provera

        [TestMethod]
        public void Ostecen_docx_rucna_provera_u_roku_od_60_s_bez_poruke_nema_tovarnog_lista()
        {
            ZahtevaKljuc();
            var p = Path.Combine(Folder, "nezavisni", "CUV_exGK33", CuvIme);
            if (!File.Exists(p)) Assert.Inconclusive("Nema " + p);
            var b = File.ReadAllBytes(p);
            var ostecen = b.Take(b.Length / 3).ToArray();
            var sw = Stopwatch.StartNew();
            var i = new Obrada(pravila, citac, null, 0.6).Proveri(new[] { ("CUV ostecen.docx", ostecen) }, "a@b.de", false, new DateTime(2026, 11, 13), false);
            Assert.IsTrue(sw.Elapsed.TotalSeconds < 60, "trajalo " + sw.Elapsed);
            Assert.IsFalse(i.Provera.ImaTovarniList);
            CollectionAssert.Contains(i.Neprepoznati, "CUV ostecen.docx");
            Assert.IsTrue(i.NisuProcitani.Any(x => x.Contains("ne može da se pretvori") && x.Contains("ručna provera")), string.Join(" | ", i.NisuProcitani));
            CollectionAssert.Contains(i.OpisPriloga, "CUV ostecen.docx – nije prepoznat");
        }

        [TestMethod]
        public void Bez_kljuca_Word_ide_na_rucnu_proveru_i_trazi_obavestenje()
        {
            WordUPdf.ZaTestBezKljuca = true;
            WordUPdf.NedostajaoKljuc = false;
            try
            {
                var i = new Obrada(pravila, citac, null, 0.6).Proveri(new[] { ("CUV.doc", new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 1, 2, 3, 4 }) }, "a@b.de", false, new DateTime(2026, 11, 13), false);
                Assert.IsFalse(i.Provera.ImaTovarniList);
                CollectionAssert.Contains(i.Neprepoznati, "CUV.doc");
                Assert.IsTrue(i.NisuProcitani.Any(x => x.Contains("licencni ključ nije učitan")), string.Join(" | ", i.NisuProcitani));
                Assert.IsTrue(WordUPdf.NedostajaoKljuc, "AgentRad šalje jedno interno obaveštenje");
            }
            finally { WordUPdf.ZaTestBezKljuca = false; WordUPdf.NedostajaoKljuc = false; }
        }

        [TestMethod]
        public void Odt_ide_na_rucnu_proveru_sa_razlogom()
        {
            var i = new Obrada(pravila, citac, null, 0.6).Proveri(new[] { ("CUV.odt", new byte[] { (byte)'P', (byte)'K', 3, 4, 0, 0 }) }, "a@b.de", false, new DateTime(2026, 11, 13), false);
            CollectionAssert.Contains(i.Neprepoznati, "CUV.odt");
            Assert.IsTrue(i.NisuProcitani.Any(x => x.Contains("OpenDocument")), string.Join(" | ", i.NisuProcitani));
        }

        // ------------------------------------------------------------------------------------------- blokovi (bez pretvaranja)

        private static Rec R(string t, double xmm, double ymm, double sirinaMm) =>
            new Rec { T = t, X0 = xmm / 210, X1 = (xmm + sirinaMm) / 210, Y0 = ymm / 297, Y1 = (ymm + 3.5) / 297, Velicina = 10, Font = Rec.FontWord };

        [TestMethod]
        public void Blokovi_okvira_redovi_jedan_ispod_drugog_i_rec_malo_iznad_reda()
        {
            // r13 iz CUV_exGK33: prvi red je dug (prelazi u r14), „Štúrovo“ je 0,6 mm iznad reda; „Futog … Serbia“ su daleko (dva bloka)
            var reci = new List<Rec>
            {
                R("1.", 10, 94.1, 3), R("Bad", 14, 94.1, 6), R("Schandau", 21, 94.1, 15), R("br./Decin", 37, 94.1, 12), R("br-", 50, 94.1, 3),
                R("Lanžhot", 54, 94.1, 10), R("br", 65, 94.1, 3), R("Kúty", 69, 94.1, 5), R("br", 75, 94.1, 2), R(">", 77.5, 94.1, 1),
                R("Štúrovo", 78.8, 93.5, 13), R("br/Szob", 92.5, 94.1, 12),
                R("Kelebia", 10, 97.7, 12), R("br/Subotica", 23, 97.7, 18),
                R("3.", 10, 101.4, 3), R("DE:", 14, 101.4, 6), R("(3212)", 10, 105.1, 10),
                R("Futog", 18, 82.8, 10), R("Serbia", 54, 82.8, 11),
            };
            PdfStrane.OznaciBlokove(reci, 595, 842);
            var blokR13 = reci.Where(r => r.Cy > 90.0 / 297).Select(r => r.Blok).Distinct().ToList();
            Assert.AreEqual(1, blokR13.Count, "ceo r13 je jedan blok");
            Assert.AreNotEqual(reci.Single(r => r.T == "Futog").Blok, reci.Single(r => r.T == "Serbia").Blok, "razmak 26 mm – posebni blokovi");
            Assert.AreNotEqual(blokR13[0], reci.Single(r => r.T == "Futog").Blok);
        }

        // ------------------------------------------------------------------------------------------- PDF sa poljima obrasca

        [TestMethod, TestCategory("Regresija")]
        public void GK30_vrednosti_iz_polja_obrasca_AcroForm()
        {
            var p = Path.Combine(Folder, "nezavisni", "GK30", "CIM draft GK30 Futog -Lovosice.pdf");
            if (!File.Exists(p)) Assert.Inconclusive("Nema " + p);
            var strane = PdfStrane.Procitaj(File.ReadAllBytes(p), null);
            Assert.IsTrue(strane[0].Reci.Any(r => r.Font == "AcroForm" && r.T == "Konzul"), "vrednost polja Expéditeur1");
            var i = new Obrada(pravila, citac, null, 0.6).Proveri(new[] { (Path.GetFileName(p), File.ReadAllBytes(p)) }, "a@b.de", false, new DateTime(2026, 11, 13), false);
            var c = i.Provera.Listovi.First().Cim;
            StringAssert.Contains(c.R1.Tekst, "Konzul d.o.o");
            StringAssert.Contains(c.R4.Tekst, "PREOL");
            Assert.IsTrue(c.R30.Cim);
        }
    }
}
