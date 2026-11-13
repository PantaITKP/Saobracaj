using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MimeKit;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>Tovarni list CIM/CUV kao Excel/Word obrazac, nove vrste priloga i poruka kada tovarni list nije nađen (mail „HER-SAD 79“).</summary>
    [TestClass]
    public class TabelarniTests
    {
        private const string Share = @"\\192.168.1.6\Cloude\Agenti\CimProvera\testovi";
        private static string Folder => Environment.GetEnvironmentVariable("CIMPROVERA_TESTOVI") is string f && f.Length > 0 ? f : Share;
        private static string HerSad => Path.Combine(Folder, "nezavisni", "HER-SAD79");

        private static PravilaSkup pravila;
        private static LokalniCitac citac;

        [ClassInitialize]
        public static void Init(TestContext _)
        {
            var cfg = RegresijaTests.Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje"));
            pravila = PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null);
            citac = new LokalniCitac(cfg, null, pravila.Kp);
        }

        [ClassCleanup] public static void Kraj() => citac?.Dispose();

        // ------------------------------------------------------------------------------------------- sintetička tabela

        private static List<Celija> Obrazac(Action<List<Celija>> dopuna = null)
        {
            var c = new List<Celija>();
            void D(int r, int k, string t) => c.Add(new Celija { Red = r, Kol = k, Tekst = t });
            D(1, 4, "30"); D(1, 5, "Frachtbrief CIM"); D(2, 13, "X"); D(1, 19, "Wagenbrief CUV");
            D(4, 4, "1"); D(4, 5, "Absender (Name, Anschrift)"); D(6, 4, "Firma GmbH\nStrasse 1\n44653 Herne DE");
            D(4, 29, "7"); D(4, 30, "Erklärung des Absenders"); D(6, 30, "16. Frachtzahler fuer SRB: Lokomotiva d.o.o.");
            D(11, 4, "4"); D(11, 5, "Empfänger (Name, Anschrift, Land)"); D(13, 4, "DP World\nCarinska 1\n21000 Novi Sad RS");
            D(18, 0, "10"); D(20, 1, "Novi Sad ranžirna"); D(18, 21, "12"); for (int i = 0; i < 8; i++) D(19, 21 + i, "72168716"[i].ToString());
            D(18, 29, "16"); D(20, 33, "Wanne-Eickel DE 11-10-14");
            D(22, 29, "18"); D(24, 30, "po spisku kola");
            D(24, 0, "13"); D(26, 1, "3. SRB: KP 3212"); D(24, 17, "14"); D(25, 18, "KP 2026/7");
            D(27, 29, "20"); D(30, 0, "15");
            D(32, 0, "21"); D(36, 1, "Kontejneri, 20 kola"); D(32, 13, "22"); D(32, 28, "23"); D(32, 32, "24"); D(35, 33, "262190");
            D(32, 39, "25"); D(35, 40, "1.082.918 kg");
            D(71, 17, "57"); D(73, 18, "KP 3212"); D(79, 0, "58");
            D(79, 40, "62"); for (int i = 0; i < 8; i++) D(81, 46 + i, "80101287"[i].ToString()); D(85, 41, "3127"); D(85, 49, "HER-SAD79");
            D(87, 40, "29"); D(90, 41, "Herne, 10.11.2026");
            dopuna?.Invoke(c);
            return c;
        }

        [TestMethod]
        public void Excel_obrazac_prepoznat_i_procitan_iz_celija()
        {
            var c = Obrazac();
            Assert.IsTrue(TabelarniCim.JeTovarniList(c, out var razlog), razlog);
            var cim = TabelarniCim.Procitaj(c, citac.JeNatpisCelije, citac.Pragovi, out var po);
            Assert.AreEqual("CIM", cim.Obrazac); Assert.IsTrue(cim.R30.Cim);
            StringAssert.Contains(cim.R1.Tekst, "Firma GmbH"); StringAssert.Contains(cim.R7.Tekst, "Lokomotiva");
            StringAssert.Contains(cim.R4.Tekst, "Carinska 1");
            Assert.AreEqual("2026-11-10", cim.R29.Datum); StringAssert.Contains(cim.R25.Tekst, "1.082.918");
            Assert.AreEqual("80", cim.R62.Zemlja); Assert.AreEqual("3127", cim.R62.Preduzece); Assert.AreEqual("HER-SAD79", cim.R62.Broj);
            Assert.IsFalse(po.Values.SelectMany(x => x).Any(x => x.Tekst.StartsWith("Absender") || x.Tekst.StartsWith("Erklärung")), "natpisi nisu vrednosti");
        }

        [TestMethod]
        public void Ime_fajla_nije_dokaz_bez_naslova_i_brojeva_rubrika()
        {
            var c = new List<Celija> { new Celija { Red = 0, Kol = 0, Tekst = "Wagen" }, new Celija { Red = 1, Kol = 0, Tekst = "37 80 4956 385-1" } };
            Assert.IsFalse(TabelarniCim.JeTovarniList(c, out _));
            Assert.IsNull(citac.TovarniIzTabele(new Prilog { Naziv = "CIM Novi Sad.xlsx" }, new List<List<Celija>> { c }, "excel"));
        }

        [TestMethod]
        public void Naslov_bez_rubrika_je_rucna_provera_a_ne_nema_tovarnog_lista()
        {
            var c = new List<Celija> { new Celija { Red = 0, Kol = 0, Tekst = "Frachtbrief CIM" }, new Celija { Red = 2, Kol = 0, Tekst = "nešto" } };
            var d = citac.TovarniIzTabele(new Prilog { Naziv = "list.docx" }, new List<List<Celija>> { c }, "word");
            Assert.IsNotNull(d);
            Assert.IsTrue(TipDokumenta.JeTovarniList(d.Tip));
            StringAssert.Contains(d.Cim[0].NeprepoznatObrazac, "Word obrazac tovarnog lista nije pouzdano pročitan");
        }

        private static byte[] Docx(string[][] redovi)
        {
            var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body><w:tbl>");
            foreach (var r in redovi) sb.Append("<w:tr>" + string.Concat(r.Select(c => "<w:tc><w:p><w:r><w:t>" + System.Security.SecurityElement.Escape(c) + "</w:t></w:r></w:p></w:tc>")) + "</w:tr>");
            sb.Append("</w:tbl></w:body></w:document>");
            using (var ms = new MemoryStream())
            {
                using (var z = new ZipArchive(ms, ZipArchiveMode.Create, true))
                using (var w = new StreamWriter(z.CreateEntry("word/document.xml").Open(), new UTF8Encoding(false))) w.Write(sb.ToString());
                return ms.ToArray();
            }
        }

        [TestMethod]
        public void Word_obrazac_tabela_prepoznat()
        {
            // svaka rubrika: broj u prvoj koloni, vrednost u drugoj
            var red = new List<string[]> { new[] { "30", "Frachtbrief CIM", "X" } };
            foreach (var (n, v) in new[] { ("1", "Firma GmbH, Herne DE"), ("4", "DP World, Novi Sad RS"), ("7", "16. Frachtzahler SRB: Lokomotiva"), ("10", "Novi Sad"), ("13", "KP 3212"),
                                          ("16", "Herne DE 11-10-14"), ("18", "po spisku"), ("20", "Franko"), ("21", "Kontejneri"), ("24", "262190"), ("25", "20000 kg"), ("29", "Herne, 10.11.2026"), ("57", "KP 3212"), ("62", "80 101287 3127 12345-6") })
            { red.Add(new[] { n, "" }); red.Add(new[] { "", v }); }
            var docx = Docx(red.ToArray());
            var listovi = TabelarniCim.IzWorda(docx);
            Assert.IsTrue(TabelarniCim.JeTovarniList(listovi[0], out var razlog), razlog);
            var d = citac.TovarniIzTabele(new Prilog { Naziv = "CIM.docx", Sadrzaj = docx }, listovi, "word");
            Assert.AreEqual(TipDokumenta.Cim, d.Tip); StringAssert.Contains(d.Cim[0].R7.Tekst, "Lokomotiva");
        }

        [TestMethod]
        public void Isti_list_kao_PDF_i_Excel_proverava_se_jednom()
        {
            CimList L(string broj) => new CimList { R62 = new Identifikacija { Zemlja = "80", Stanica = "101287", Preduzece = "3127", Broj = broj, Status = StatusRubrike.Popunjeno } };
            var excel = new Dokument { Tip = TipDokumenta.Cim, Izvor = "excel", Fajl = "CIM.xlsx", Cim = { L("HER-SAD79") } };
            var pdf = new Dokument { Tip = TipDokumenta.Cim, Izvor = "tekst", Fajl = "CIM.pdf", Cim = { L("HER-SAD79") } };
            var docs = new List<Dokument> { excel, pdf };
            Obrada.IstiListDvaput(docs, null);
            Assert.AreEqual(1, docs.Count(d => TipDokumenta.JeTovarniList(d.Tip)), "nepotpisan PDF: proverava se Excel");
            Assert.IsTrue(TipDokumenta.JeTovarniList(excel.Tip)); StringAssert.Contains(excel.Opis, "proveren je CIM.xlsx");
            var e2 = new Dokument { Tip = TipDokumenta.Cim, Izvor = "excel", Fajl = "CIM.xlsx", Cim = { L("HER-SAD79") } };
            var p2 = new Dokument { Tip = TipDokumenta.Cim, Izvor = "ocr", Fajl = "CIM-sken.pdf", Cim = { L("HER-SAD79") } };
            var d2 = new List<Dokument> { e2, p2 };
            Obrada.IstiListDvaput(d2, null);
            Assert.IsTrue(TipDokumenta.JeTovarniList(p2.Tip) && !TipDokumenta.JeTovarniList(e2.Tip), "potpisan/pečatiran (sken) PDF ima prednost");
        }

        // ------------------------------------------------------------------------------------------- poruka kada tovarni list nije nađen

        private static AgentConfig CfgRezim(string rezim) =>
            AgentConfig.Ucitaj(new NameValueCollection { ["CitajSanduce"] = "agenti@kprevoz.co.rs", ["TestPrimalac"] = "agenti@kprevoz.co.rs", ["Rezim"] = rezim, ["KopijaNa"] = "priprema@kprevoz.co.rs" });

        [TestMethod]
        public void Neprepoznat_prilog_bez_tovarnog_lista_interna_poruka_ne_posiljaocu()
        {
            var o = new Obrada(pravila, citac, null, 0.6);
            var ishod = o.Proveri(new[] { ("CIM HER-SAD.doc", new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 1, 2, 3 }) }, "posiljalac@firma.de", false, new DateTime(2026, 11, 13), false);
            Assert.IsFalse(ishod.Provera.ImaTovarniList);
            CollectionAssert.Contains(ishod.Neprepoznati, "CIM HER-SAD.doc");
            var m = new MimeMessage(); m.From.Add(MailboxAddress.Parse("posiljalac@firma.de")); m.Subject = "HER-SAD 79";
            foreach (var (rezim, ocekivano) in new[] { ("Test", "agenti@kprevoz.co.rs"), ("Live", "priprema@kprevoz.co.rs") })
            {
                var (naslov, telo, primaoci) = AgentRad.InternaRucnaProvera(CfgRezim(rezim), m, ishod);
                CollectionAssert.Contains(primaoci, ocekivano, rezim);
                CollectionAssert.DoesNotContain(primaoci, "posiljalac@firma.de", "ne ide pošiljaocu");
                StringAssert.Contains(telo, "Tovarni list nije automatski prepoznat. Prilog CIM HER-SAD.doc nije mogao da se pročita – proverite ručno.");
                StringAssert.StartsWith(naslov, "[INTERNO]");
            }
        }

        // ------------------------------------------------------------------------------------------- stvarni prilozi HER-SAD 79

        private static (string ime, byte[] b) Fajl(string ime)
        {
            var p = Path.Combine(HerSad, ime);
            if (!File.Exists(p)) Assert.Inconclusive("Nema " + p);
            return (ime, File.ReadAllBytes(p));
        }

        private const string ExcelCim = "CIM Novi Sad via Kelebia (NEU).xlsx";
        private const string HslXls = "2026-11-10 HSL Wagenliste x von Herne nach NoviSad.xls";
        private const string Trainlist = "Trainlist.xlsx";
        private const string Vtk = "VTK_46837_2026-11-13_2026_146757_15_Rajka.pdf";

        [TestMethod, TestCategory("Regresija")]
        public void HerSad_excel_CIM_prepoznat_kao_tovarni_list()
        {
            var o = new Obrada(pravila, citac, null, 0.6);
            var i = o.Proveri(new[] { Fajl(ExcelCim) }, "posiljalac@firma.de", false, new DateTime(2026, 11, 13), false);
            Assert.AreEqual(1, i.Provera.Listovi.Count);
            var c = i.Provera.Listovi[0].Cim;
            Assert.AreEqual("TABELA", c.Sablon); Assert.AreEqual("CIM", c.Obrazac); Assert.IsTrue(c.R30.Cim);
            Assert.AreEqual("Tovarni list CIM 80 10128-7 3127 HER-SAD79 – potrebne ispravke", Izvestaj.Naslov(i.Provera, pravila, false));
            Assert.IsFalse(i.Provera.Listovi[0].Nalazi.Any(n => n.Pravilo == "CITLJIVOST"), "čitanje iz ćelija nije OCR");
        }

        [TestMethod, TestCategory("Regresija")]
        public void HerSad_xls_spisak_kola_i_trainlist()
        {
            var o = new Obrada(pravila, citac, null, 0.6);
            var i = o.Proveri(new[] { Fajl(HslXls), Fajl(Trainlist) }, "posiljalac@firma.de", false, new DateTime(2026, 11, 13), false);
            Assert.IsFalse(i.Provera.ImaTovarniList, "samo spisak kola – i dalje „spisak kola bez tovarnog lista“");
            Assert.AreEqual(0, i.Neprepoznati.Count);
            var hsl = i.Provera.Dokumenti.Single(d => d.Fajl == HslXls);
            Assert.AreEqual(TipDokumenta.SpisakKola, hsl.Tip);
            Assert.AreEqual(19, hsl.Spisak[0].Kola.Select(k => k.Broj).Distinct().Count(), "broj kola iz pet ćelija (37 | 84 | 4956 | 011 | 9)");
            Assert.IsTrue(hsl.Spisak[0].MasaUpisana);
            CollectionAssert.Contains(i.OpisPriloga, HslXls + " – spisak kola");
            CollectionAssert.Contains(i.OpisPriloga, Trainlist + " – spisak tovarnih jedinica");
        }

        [TestMethod, TestCategory("Regresija")]
        public void HerSad_VTK_je_izvestaj_o_sastavu_voza_bez_greske()
        {
            var o = new Obrada(pravila, citac, null, 0.6);
            var i = o.Proveri(new[] { Fajl(ExcelCim), Fajl(Vtk) }, "posiljalac@firma.de", false, new DateTime(2026, 11, 13), false);
            Assert.IsTrue(i.Provera.Dokumenti.Any(d => d.Fajl == Vtk && d.Tip == TipDokumenta.SastavVoza));
            CollectionAssert.Contains(i.OpisPriloga, Vtk + " – izveštaj o sastavu voza (ne proverava se)");
            var nalazi = i.Provera.Listovi.SelectMany(l => l.Nalazi).ToList();
            Assert.IsFalse(nalazi.Any(n => (n.Detalj + n.Tekst).Contains("VTK_")), "VTK ne izaziva nalaz: " + string.Join(" | ", nalazi));
        }

        /// <summary>Ceo mail HER-SAD 79 (4 priloga): nalazi, kako ih je agent dao 2026-11-13. Puna referenca – posle potvrde.</summary>
        [TestMethod, TestCategory("Regresija")]
        public void HerSad_ceo_mail_prilozi_i_nalazi()
        {
            var o = new Obrada(pravila, citac, null, 0.6);
            var i = o.Proveri(new[] { Fajl(HslXls), Fajl(ExcelCim), Fajl(Trainlist), Fajl(Vtk) }, "posiljalac@firma.de", false, new DateTime(2026, 11, 13), false);
            var r = i.Provera;
            Assert.AreEqual(1, r.Listovi.Count);
            var telo = Izvestaj.Telo(r, pravila, false);
            StringAssert.Contains(telo, "Prilozi: " + HslXls + " – spisak kola; " + ExcelCim + " – tovarni list CIM (Excel); " + Trainlist + " – spisak tovarnih jedinica; " + Vtk + " – izveštaj o sastavu voza (ne proverava se).");
            StringAssert.Contains(telo, "Platilac za deonicu kroz Srbiju: Budamar Logistics a.s.");
            var svi = r.Listovi.SelectMany(l => l.Nalazi).ToList();
            string Opis() => string.Join("\n", svi.Select(n => n.Tezina + " " + n.Pravilo + " r" + n.Rubrika + ": " + n.Detalj));
            foreach (var (pr, rub) in new[] { ("R16", "16"), ("R24", "24"), ("R1", "1") }) Assert.IsTrue(svi.Any(n => n.Pravilo == pr && n.Rubrika == rub && n.Tezina == Tezina.Greska), pr + "\n" + Opis());
            Assert.IsTrue(svi.Any(n => n.Rubrika == "9" && n.Tezina == Tezina.Greska), "r9 prazna\n" + Opis());
            foreach (var (pr, rub) in new[] { ("R4-ISO", "4"), ("R14", "14"), ("R22", "22"), ("X5", "7") }) Assert.IsTrue(svi.Any(n => n.Pravilo == pr && n.Rubrika == rub && n.Tezina == Tezina.Upozorenje), pr + "\n" + Opis());
            foreach (var pr in new[] { "KP7-PLATILAC", "KP7-NIJE-PLATILAC", "KP3", "KP57", "R18-SPISAK", "R25-MASA", "R29", "R30", "CITLJIVOST", "R10" })
                Assert.IsFalse(svi.Any(n => n.Pravilo == pr), "ne sme " + pr + "\n" + Opis());
        }

        /// <summary>Tovarni list poslat kao Excel: primer je PDF (list nacrtan iz ćelija, ispravke crveno, napomene narandžasto).</summary>
        [TestMethod, TestCategory("Regresija")]
        public void HerSad_primer_iz_Excela_je_PDF()
        {
            var prilozi = new[] { Fajl(HslXls), Fajl(ExcelCim), Fajl(Trainlist), Fajl(Vtk) };
            var o = new Obrada(pravila, citac, null, 0.6);
            var r = o.Proveri(prilozi, "posiljalac@firma.de", false, new DateTime(2026, 11, 13), false).Provera;
            var pr = PrimerPdf.Napravi(r, pravila, false, prilozi);
            Assert.IsTrue(pr.Napravljen, pr.Razlog + " " + pr.Greska);
            Assert.AreEqual("PRIMER_801012873127HER-SAD79.pdf", pr.Ime);
            Assert.AreEqual(2, pr.Strane.Count, "legenda + list");
            var (_, isp, nap, leg) = pr.PoListu.Single();
            Assert.AreEqual(4, isp, "r1, r9, r16, r24 ucrtane na listu");
            Assert.IsTrue(nap >= 4, "r4, r7, r14, r22 na listu: " + nap);
            Assert.IsTrue(pr.Pdf.Length > 1000 && pr.Pdf.Length < PrimerPdf.MaxBajtova);
            StringAssert.StartsWith(System.Text.Encoding.ASCII.GetString(pr.Pdf, 0, 5), "%PDF");
        }

        [TestMethod]
        public void Word_obrazac_primer_je_PDF()
        {
            var red = new List<string[]> { new[] { "30", "Frachtbrief CIM", "X" } };
            foreach (var (n, v) in new[] { ("1", "Firma GmbH, Herne"), ("4", "DP World, Novi Sad RS"), ("7", "Lokomotiva"), ("10", "Novi Sad"), ("13", "KP 3212"),
                                          ("16", "Herne DE 11-10-14"), ("18", "po spisku"), ("20", "Franko"), ("21", "Kontejneri"), ("24", ""), ("25", "20000 kg"), ("29", "Herne, 10.11.2026"), ("57", "KP 3212"), ("62", "80 101287 3127 12345-6") })
            { red.Add(new[] { n, "" }); red.Add(new[] { "", v }); }
            var docx = Docx(red.ToArray());
            var o = new Obrada(pravila, citac, null, 0.6);
            var r = o.Proveri(new[] { ("CIM.docx", docx) }, "posiljalac@firma.de", false, new DateTime(2026, 11, 13), false).Provera;
            Assert.IsTrue(r.ImaTovarniList);
            var pr = PrimerPdf.Napravi(r, pravila, false, new[] { ("CIM.docx", docx) });
            Assert.IsTrue(pr.Napravljen, pr.Razlog + " " + pr.Greska);
            Assert.IsTrue(pr.PoListu.Single().ispravki > 0, "ispravke ucrtane na nacrtanom Word obrascu");
        }
    }
}
