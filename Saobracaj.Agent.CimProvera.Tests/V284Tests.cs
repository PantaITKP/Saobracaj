using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Mail;
using Strana = Saobracaj.Agent.CimProvera.Citanje.Strana;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>v2.8.4: ispravke posle pravog saobraćaja 13–14.11.2026 (testovi\nezavisni\2026-11-14-pravi).</summary>
    [TestClass]
    public class V284Tests
    {
        private static PravilaSkup pravila;

        [ClassInitialize]
        public static void Init(TestContext _)
        {
            var put = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pravila", "cim_validation_rules.json");
            pravila = PravilaSkup.IzTeksta(File.ReadAllText(put), put);
        }

        /// <summary>Strana od redova teksta (reči ravnomerno po redu, pouzdanost 0,98 – tekst PDF-a).</summary>
        internal static Strana StranaIzTeksta(string tekst, int broj = 1, IzvorTeksta izvor = IzvorTeksta.Tekst, double pouzdanost = 0.98)
        {
            var s = new Strana { Broj = broj, Izvor = izvor };
            double y = 0.05;
            foreach (var red in tekst.Split('\n'))
            {
                double x = 0.03;
                foreach (var w in red.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) { s.Reci.Add(new Rec { T = w, X0 = x, X1 = x + 0.035, Y0 = y, Y1 = y + 0.008, Pouzdanost = pouzdanost }); x += 0.04; }
                y += 0.02;
            }
            return s;
        }

        private static LokalniCitac Citac() => new LokalniCitac(RegresijaTests.Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje")), null, pravila.Kp);

        // ------------------------------------------------------------------------------------------- 1. strana 2/2 spiska kola

        /// <summary>Poslednja strana spiska kola CIT 23 EurologSystem-a („1.64-276F-2026_ŠID … _258.pdf“, str. 2) – tekst PDF-a.</summary>
        internal const string Spisak22 =
            "Kola br.-Wagen Nr.   Naznačenje vrste robe   Pozicija NHM   Masa   [kg]   Tara kola   Bruto masa   Napomene-Bemerkungen\n" +
            "Bezeichnung des Gutes   Code NHM   Masse   Eigenmasse   Bruttomasse\n" +
            "Predajni list br.   des wagens\n" +
            "UTI br. / tip UTI / dužina   Übergabeschein Nr.   Neto masa   Tara UTI   Bruto masa UTI   (Plombe na kolima i UTI i dr.\n" +
            "UTI-Nr./UTI-Typ/Länge UTI   Carinska dokumenta (isprave)   Nettomasse   Eigenmasse   Bruttomasse   Verschlüsse - Plomben Nr. Am Wagen und UTI u.a.)\n" +
            "Stanje UTI - Zustand UTI   Zolldokument   UTI (Tara)   UTI\n" +
            "1   2   3   4   5   6   7   8   9\n" +
            "Σ   112   1.884.000   563.150   2.447.150\n" +
            "Ispostavio (naziv, adresa)   Mesto i datum   Potpis\n" +
            "Anschrift des Unternehmens   Ort und Datum   14.11.2026   Unterschrift\n" +
            "EurologSystem   14.11.2026.   2 | 2";

        [TestMethod]
        public void Nastavak_spiska_kola_po_zaglavlju_kolona_i_zbiru()
        {
            Assert.IsTrue(LokalniCitac.NastavakSpiska(Spisak22, out var o), "strana 2/2 spiska kola");
            StringAssert.Contains(o, "nastavak spiska kola");
            // samo red zbirova (OCR čita Σ kao „S“) i dva zaglavlja
            Assert.IsTrue(LokalniCitac.NastavakSpiska("Wagen Nr.   Code NHM\nS   112   1.884.000   563.150   2.447.150", out _));
            // prednja strana CIM lista: „Wagen Nr“ (r18) i „NHM-Code“ (r24) nisu dovoljni
            Assert.IsFalse(LokalniCitac.NastavakSpiska("Frachtbrief CIM\n18 Wagen Nr.\n24 NHM-Code\n25 Masse\n62 Identifikation der Sendung", out _));
        }

        [TestMethod]
        public void Strana_2_od_2_spiska_kola_je_nastavak_spiska_a_ne_tovarni_list()
        {
            using (var c = Citac())
            {
                var s2 = StranaIzTeksta(Spisak22, 2);
                Assert.AreEqual(TipDokumenta.SpisakKola, c.TipStrane(s2, "1.64-276F-2026_ŠID (u.m. KUKUJEVCI) - SEŽANA_VILLA OPICINA_258.pdf"));
                // i kada stoji sama u fajlu (carinski set: „Customs documents for the train no. 256 …pdf“, str. 6)
                var rez = c.Klasifikuj(new Prilozi.Prilog { Naziv = "Customs documents for the train no. 256 Pančevo-Portogruaro Caorle.pdf" }, new List<Strana> { s2 });
                Assert.IsFalse(rez.Dokumenti.Any(d => TipDokumenta.JeTovarniList(d.Tip)), string.Join("; ", rez.StraneOpis));
            }
        }

        [TestMethod]
        public void Natpisi_rubrika_bez_natpisa_obrasca_nisu_neprepoznat_CIM()
        {
            // 8+ natpisa koji liče na rubrike, bez „Frachtbrief / Lettre de voiture / Tovarni list…“, tekst PDF-a, ime fajla bez CIM
            var t = "Absender Expéditeur\nEmpfänger Destinataire\nBestimmungsbahnhof\nVersandbahnhof\nBezeichnung des Gutes\nMasse\nNHM-Code\nWagen Nr.\nErklärungen des Absenders\nBeigefügte Dokumente\nZahlungsvermerke\nOrt und Datum";
            using (var c = Citac())
            {
                var s = StranaIzTeksta(t, 1);
                var rez = c.Klasifikuj(new Prilozi.Prilog { Naziv = "Dokumentacija 1637.pdf" }, new List<Strana> { s });
                Assert.IsFalse(rez.Dokumenti.Any(d => TipDokumenta.JeTovarniList(d.Tip)), string.Join("; ", rez.StraneOpis));
                // ali liči na obrazac (12 natpisa rubrika) i nije pročitana kao list → ručna provera, nikad „bez odgovora“
                Assert.IsTrue(rez.NeprocitaneStrane > 0, "strana koja liči na obrazac ide na ručnu proveru: " + rez.Napomena);
                StringAssert.Contains(rez.Napomena, "liči na obrazac CIM/CUV");
                // ime fajla „CIM …“: oprez – i dalje tovarni list (ručna provera ako obrazac nije prepoznat)
                var rez2 = c.Klasifikuj(new Prilozi.Prilog { Naziv = "CIM 256F.pdf" }, new List<Strana> { StranaIzTeksta(t, 1) });
                Assert.IsTrue(rez2.Dokumenti.Any(d => TipDokumenta.JeTovarniList(d.Tip)), string.Join("; ", rez2.StraneOpis));
            }
        }

        [TestMethod]
        public void Log_za_mail_bez_tovarnog_lista()
        {
            var docs = new List<Dokument>
            {
                new Dokument { Tip = TipDokumenta.SpisakKola, Fajl = "1.64-276F-2026_258.pdf", Strane = { 1, 2 } },
                new Dokument { Tip = TipDokumenta.SpisakKola, Fajl = "CIT-23 258.xlsx", Strane = { 1 } },
                new Dokument { Tip = TipDokumenta.IzvoznaDeklaracija, Fajl = "Customs.pdf", Strane = { 1, 2 } },
            };
            var l = Izvestaj.LogBezTovarnogLista(docs);
            StringAssert.StartsWith(l, "nema CIM/CUV – bez odgovora; dokumenti: ");
            StringAssert.Contains(l, "spisak_kola (1.64-276F-2026_258.pdf str. 1, 2; CIT-23 258.xlsx str. 1)");
            StringAssert.Contains(l, "izvozna_deklaracija (Customs.pdf str. 1, 2)");
        }

        // ------------------------------------------------------------------------------------------- 2. položeni i crno-beli skenovi

        private static byte[] SlikaTeksta(int w, int h, System.Drawing.RotateFlipType okret, bool crnoBela = false)
        {
            using (var bmp = new System.Drawing.Bitmap(w, h))
            {
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                using (var font = new System.Drawing.Font("Arial", 22))
                {
                    g.Clear(System.Drawing.Color.White);
                    g.TextRenderingHint = crnoBela ? System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit : System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    var redovi = new[] { "FRACHTBRIEF CIM LETTRE DE VOITURE", "Absender Expediteur EAST GRAIN DOO NOVI SAD", "Empfanger Destinataire MOLINO DI SAN GIOVANNI",
                        "Identifikation der Sendung 72 16513-4 3212 161500", "Bezeichnung des Gutes PSENICA NHM 100100", "Ort und Datum SID 14.11.2026" };
                    for (int i = 0; i < 12; i++) g.DrawString(redovi[i % redovi.Length], font, System.Drawing.Brushes.Black, 40, 40 + i * 70);
                }
                bmp.RotateFlip(okret);
                using (var ms = new MemoryStream()) { bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png); return ms.ToArray(); }
            }
        }

        [TestMethod]
        public void Polozen_sken_se_okrece_pre_OCR_a()
        {
            var cfg = RegresijaTests.Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje"));
            if (!Ocr.Dostupan(cfg.TessdataFolder, cfg.OcrJezici, out var razlog)) Assert.Inconclusive(razlog);
            using (var o = new Ocr(cfg.TessdataFolder, cfg.OcrJezici))
            {
                // A4 uspravno 1240×1754, sadržaj zarotiran za 90° → slika 1754×1240 (kao 3507×2480 u „doc00429320261114120847.pdf“)
                var reci = o.Prepoznaj(SlikaTeksta(1240, 1000, System.Drawing.RotateFlipType.Rotate90FlipNone), out var nap);
                Assert.AreEqual(270, o.PoslednjiUgao, nap);
                var t = string.Join(" ", reci.Select(r => r.T));
                StringAssert.Contains(t, "FRACHTBRIEF", t);
                StringAssert.Contains(t, "3212", t);
                StringAssert.Contains(nap, "strana okrenuta za 270°");
                // ispravna strana se ne okreće
                o.Prepoznaj(SlikaTeksta(1240, 1000, System.Drawing.RotateFlipType.RotateNoneFlipNone), out _);
                Assert.AreEqual(0, o.PoslednjiUgao);
            }
        }

        [TestMethod]
        public void Crno_beli_sken_se_prepoznaje()
        {
            Assert.IsTrue(Ocr.CrnoBela(SlikaTeksta(600, 500, System.Drawing.RotateFlipType.RotateNoneFlipNone, crnoBela: true)));
            Assert.IsFalse(Ocr.CrnoBela(SlikaTeksta(600, 500, System.Drawing.RotateFlipType.RotateNoneFlipNone, crnoBela: false)), "zaglađene ivice slova (sivi tonovi)");
            // fotografija u boji (gradijent) nije crno-bela
            using (var bmp = new System.Drawing.Bitmap(300, 200))
            {
                for (int x = 0; x < 300; x++) for (int y = 0; y < 200; y++) bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(x * 255 / 300, 120, y * 255 / 200));
                using (var ms = new MemoryStream()) { bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png); Assert.IsFalse(Ocr.CrnoBela(ms.ToArray())); }
            }
        }

        [TestMethod]
        public void Ugao_strane_vazi_za_svako_iscrtavanje_istog_fajla()
        {
            var a = new byte[50000]; new Random(7).NextBytes(a);
            var kopija = (byte[])a.Clone();          // isti fajl iz drugog maila (drugi niz bajtova)
            PdfCrtac.ZapamtiUgao(a, 8, 270);
            Assert.AreEqual(270, PdfCrtac.Ugao(kopija, 8));
            Assert.AreEqual(0, PdfCrtac.Ugao(kopija, 7));
            PdfCrtac.ZapamtiUgao(a, 8, 0);
            Assert.AreEqual(0, PdfCrtac.Ugao(a, 8));
        }

        // ------------------------------------------------------------------------------------------- 3. r7 – nove varijante platioca

        private static Provere.AnalizaR7 R7(string t) => Provere.AnalizirajR7(t, pravila.Kp);

        [DataTestMethod]
        // CIM (Pansped, 256F): upisano i OCR verzija
        [DataRow("7.16 Frachtzahler auf KP, GPR, SŽ-TP und MIR ist EurologSystem doo Beograd", "EurologSystem doo Beograd")]
        [DataRow("7.16 Frachtzahler auf KP, GPR, SŽ-TP ud MIR is EurologSystem doo Beograd", "EurologSystem doo Beograd")]
        [DataRow("7.16 Frachtzahler auf KP, GPR, SŽ-TP urd MIR lst EurologSystem doo Beograd", "EurologSystem doo Beograd")]
        [DataRow("7.16 Frachtzahler fiir KP, GPR und MIR 1st EurologSystem doo Beograd", "EurologSystem doo Beograd")]
        // OCR izgubi „Frachtzahler auf“: šifra 16 u r7 je izjava o platiocu
        [DataRow("7 16 KP, GPR, SŽ-TP is EurologSystem doo Beograd / Pošiljalac jamči za težinu.", "EurologSystem doo Beograd")]
        // Sremska Mitrovica – Revere
        [DataRow("Plaćanje u RS za KP(3212): Panšped\nInrail 3128", "Panšped")]
        [DataRow("Placanje u RS za KP(3212): Panšped doo", "Panšped doo")]
        // naziv do kraja reda ili do tačke, bez sečenja
        [DataRow("Payer on IZS: Panšped doo\nES-5026; MAV Cargo Logistics", "Panšped doo")]
        [DataRow("Frachtzahler SRB: Kopax Rail\nServices doo", "Kopax Rail Services doo")]
        [DataRow("Frachtzahler SRB ist Kopax Rail Services doo. CIM 6: Zollbehandlung", "Kopax Rail Services doo")]
        [DataRow("Freight paid by Panšped doo", "Panšped doo")]
        [DataRow("Zahlung SRB: Panšped doo", "Panšped doo")]
        [DataRow("Piatilac za deonicu SRB: Panšped doo", "Panšped doo")]
        public void T3_Platilac_za_Srbiju(string r7, string naziv)
        {
            var a = R7(r7);
            var p = a.ZaSrbiju.ToList();
            Assert.AreEqual(1, p.Count, string.Join(" | ", a.Platioci.Select(x => x.Zapis + " [" + x.Opseg + "]")));
            Assert.AreEqual(naziv, p[0].Naziv);
            Assert.IsFalse(p[0].Kp, "platilac nije KP");
        }

        /// <summary>Šifra 16 u r7 su po GLV-CIM „Ostale izjave“ – platilac samo u obliku 16 + prevoznici/deonice sa KP + ist/is/=/je + naziv.</summary>
        [DataTestMethod]
        [DataRow("16. Ovlašćeno lice: Petar Petrović, KP Novi Sad")]
        [DataRow("16 Ovlašćeno lice je Petar Petrović")]
        [DataRow("16")]
        [DataRow("16. Pošiljalac jamči za težinu")]
        [DataRow("7.16 GPR, SŽ-TP is EurologSystem doo Beograd")]   // bez KP u nizu prevoznika
        public void T3_Sifra_16_bez_propisanog_oblika_nije_platilac(string r7)
        {
            var a = R7(r7);
            Assert.AreEqual(0, a.Platioci.Count(p => p.Naziv.Length > 0), string.Join(" | ", a.Platioci.Select(x => x.Zapis)));
        }

        [DataTestMethod]
        [DataRow("16 KP, GPR ist Panšped doo", "Panšped doo")]
        [DataRow("7.16 KP (3212), SŽ = EurologSystem doo Beograd", "EurologSystem doo Beograd")]
        [DataRow("16. KP 3212 je Panšped doo", "Panšped doo")]
        public void T3_Sifra_16_u_propisanom_obliku_je_platilac(string r7, string naziv) => Assert.AreEqual(naziv, R7(r7).ZaSrbiju.Single().Naziv);

        [TestMethod]
        public void T3_Lukavac_posle_SRB_je_prevoznik_a_ne_platilac_nejasno()
        {
            var a = R7("Platilac za prugo ZFBH/ZRS je Rail Cargo logistics-BH d.o.o. SRB: KP 3212; HU: Rail Cargo Hungaria");
            Assert.IsFalse(a.ZaSrbiju.Any(), string.Join(" | ", a.ZaSrbiju.Select(x => x.Zapis)));
            Assert.AreEqual(1, a.Nejasni.Count);
            // a ne: KP kao platilac za SRB (KP7-NIJE-PLATILAC) ni drugi naziv posle „SRB:“
            Assert.AreEqual(0, R7("Frachtzahler SRB: KP 3212").Nejasni.Count);
            Assert.AreEqual(0, R7("Platilac za prugu ZFBH je RCL BH d.o.o. SRB: Panšped doo").Nejasni.Count);
            // šifra 16 bez reči za platioca, posle oznake deonice KP – i dalje nejasno, ne „KP je platilac“
            var a16 = R7("16. IŽS: KP 3212");
            Assert.AreEqual(1, a16.Nejasni.Count);
            Assert.IsFalse(a16.ZaSrbiju.Any(p => p.Kp));
        }

        [DataTestMethod]
        [DataRow("ud", "und")] [DataRow("urd", "und")] [DataRow("unv", "und")]
        [DataRow("is", "is")] [DataRow("lst", "ist")] [DataRow("1st", "ist")]
        [DataRow("fur", "für")] [DataRow("fiir", "für")]
        [DataRow("list", "list")] [DataRow("in", "in")] [DataRow("Wagon", "Wagon")]
        [DataRow("Paver", "Payer")] [DataRow("Platiiac", "Platilac")]
        public void T3_Veznici_i_reci_platioca_tolerancija_1_slovo(string ulaz, string izlaz) => Assert.AreEqual(izlaz, Provere.IspraviVeznike(ulaz));

        // ------------------------------------------------------------------------------------------- 6. ponavljanja

        private static string NovFolder() { var f = Path.Combine(Path.GetTempPath(), "CimProveraTest", "pon-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(f); return f; }

        [TestMethod]
        public void T6_Isti_prilog_se_ne_proverava_ponovo()
        {
            var folder = NovFolder();
            var pdf = new byte[200 * 1024]; new Random(1849).NextBytes(pdf);
            var logo = new byte[2 * 1024]; new Random(1).NextBytes(logo);
            var p = Ponavljanja.Ucitaj(folder);
            var prvi = new List<(string, byte[])> { ("1849_261114093806_001.pdf", pdf), ("image001.jpg", logo) };
            Assert.IsNull(p.IstiPrilozi(prvi));
            p.ZapamtiPriloge(prvi, "UID 284", "interni izveštaj poslat", new DateTime(2026, 11, 14, 9, 57, 0));
            p.Sacuvaj();
            // drugi mail (rss.sk): isti PDF pod istim imenom, drugi logo → isti prilog, već obrađeno
            var drugi = new List<(string, byte[])> { ("1849_261114093806_001.pdf", (byte[])pdf.Clone()), ("image002.png", new byte[3000]) };
            var z = Ponavljanja.Ucitaj(folder).IstiPrilozi(drugi);
            Assert.IsNotNull(z);
            Assert.AreEqual("UID 284", z.Oznaka);
            // novi prilog uz stari → proverava se (isti list proverava tačka 6.2)
            var k200 = new byte[50 * 1024]; new Random(200).NextBytes(k200);
            Assert.IsNull(Ponavljanja.Ucitaj(folder).IstiPrilozi(new List<(string, byte[])> { ("doc.pdf", pdf), ("K200.pdf", k200) }));
            // samo mali prilozi (logo) → nikad „isti prilog“
            Assert.IsNull(Ponavljanja.Ucitaj(folder).IstiPrilozi(new List<(string, byte[])> { ("image001.jpg", logo) }));
        }

        private static RezultatProvere RezultatSaListom(CimList c, string fajl = "CIM.pdf")
        {
            var d = new Dokument { Tip = TipDokumenta.Cim, Fajl = fajl, Strane = { 1 }, Cim = { c } };
            var r = new RezultatProvere(); r.Dokumenti.Add(d); r.Listovi.Add(new RezultatLista { Dokument = d, Cim = c, Oznaka = "1" });
            return r;
        }

        [TestMethod]
        public void T6_Isti_tovarni_list_sa_nepromenjenim_rubrikama_ne_prijavljuje_se_ponovo()
        {
            var folder = NovFolder();
            var p = Ponavljanja.Ucitaj(folder);
            var c = MotorTests.IspravanCim();
            Assert.AreEqual("r62:72-723456-3212-123456", Ponavljanja.KljucLista(c));
            Assert.IsNull(p.IstiListovi(RezultatSaListom(c)));
            p.ZapamtiListove(RezultatSaListom(c), "UID 201", new DateTime(2026, 11, 14, 0, 7, 0));
            p.Sacuvaj();
            // isti list (HER-SAD 79 u „ARR – Aviso“): isti sadržaj → već prijavljeno
            var z = Ponavljanja.Ucitaj(folder).IstiListovi(RezultatSaListom(MotorTests.IspravanCim(), "CIM Novi Sad via Kelebia (NEU).xlsx"));
            Assert.IsNotNull(z); Assert.AreEqual("UID 201", z.Oznaka);
            // promenjena rubrika (nova verzija) → prijavljuje se
            var c2 = MotorTests.IspravanCim(); c2.R9.Tekst = "830 Izvozna deklaracija";
            Assert.IsNull(Ponavljanja.Ucitaj(folder).IstiListovi(RezultatSaListom(c2)));
        }

        [TestMethod]
        public void T6_Kljuc_lista_bez_r62_posiljalac_r8_relacija()
        {
            var c = MotorTests.IspravanCim();
            c.R62 = new Identifikacija();
            c.R8.Tekst = "HER-SAD 79"; c.R16.Tekst = "Novi Sad ranžirna"; c.R10.Tekst = "Wanne-Eickel";
            var k = Ponavljanja.KljucLista(c);
            StringAssert.StartsWith(k, "list:");
            StringAssert.Contains(k, "79");
            Assert.AreEqual(k, Ponavljanja.KljucLista(c), "isti list – isti ključ");
            // neprepoznat obrazac nema ključ (uvek se prijavljuje – ručna provera)
            Assert.IsNull(Ponavljanja.KljucLista(new CimList { NeprepoznatObrazac = "obrazac nije prepoznat" }));
        }

        /// <summary>„fw test 4“ (14.11. 00:07) i „ARR – Aviso: 46837/13 HER-SAD 79“ (12:53): isti „CIM Novi Sad via Kelebia (NEU).xlsx“ → drugi bez novog izveštaja.</summary>
        [TestMethod, TestCategory("Regresija")]
        public void T6_ARR_Aviso_HER_SAD_79_isti_tovarni_list_kao_fw_test_4()
        {
            var fw4 = @"\\192.168.1.6\Cloude\Agenti\CimProvera\testovi\nezavisni\2026-11-14\fw test 4";
            if (!Directory.Exists(fw4)) Assert.Inconclusive("Nema " + fw4);
            Ishod Proveri(IEnumerable<string> f, string od)
            {
                var cfg = RegresijaTests.Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje"));
                using (var c = new LokalniCitac(cfg, null, pravila.Kp))
                    return new Obrada(pravila, c, null, cfg.PragPouzdanosti).Proveri(f.Select(x => (Path.GetFileName(x), File.ReadAllBytes(x))).ToList(), od, cfg.JeInterni(od), new DateTime(2026, 11, 14), false);
            }
            var prvi = Proveri(Directory.GetFiles(fw4), "stefan.obradovic@kprevoz.co.rs");
            var drugi = Proveri(Directory.GetFiles(Path.GetDirectoryName(PutanjaPravi(@"ARR - Aviso 4683713  HER-SAD 79\Trainlist.xlsx"))), "dispo@crw.sk");
            var l1 = prvi.Provera.Listovi.Single(x => x.Cim != null).Cim;
            Assert.IsNotNull(Ponavljanja.KljucLista(l1), "list mora imati ključ (r62 80 101287 3127 HER-SAD79)");
            var p = Ponavljanja.Ucitaj(NovFolder());
            p.ZapamtiListove(prvi.Provera, "UID 201", new DateTime(2026, 11, 14, 0, 7, 0));
            var z = p.IstiListovi(drugi.Provera);
            Assert.IsNotNull(z, "isti fajl mora dati isti otisak: " + Ponavljanja.KljucLista(drugi.Provera.Listovi.Single(x => x.Cim != null).Cim));
            Assert.AreEqual("UID 201", z.Oznaka);
        }

        [TestMethod, TestCategory("Regresija")]
        public void T6_Lukavac_1849_od_trainhungary_pa_od_rss_isti_prilog()
        {
            var a = File.ReadAllBytes(PutanjaPravi(@"Lukavac02 (NHM 2836)\1849_261114093806_001.pdf"));
            var b = File.ReadAllBytes(PutanjaPravi(@"Lukavac02 (NHM 2836)(2)\1849_261114093806_001.pdf"));
            var p = Ponavljanja.Ucitaj(NovFolder());
            p.ZapamtiPriloge(new List<(string, byte[])> { ("1849_261114093806_001.pdf", a) }, "UID 284", "interni izveštaj poslat", new DateTime(2026, 11, 14, 9, 57, 0));
            Assert.AreEqual("UID 284", p.IstiPrilozi(new List<(string, byte[])> { ("1849_261114093806_001.pdf", b) })?.Oznaka);
            // „doc00429320261114120847.pdf“ u tri maila (Izvozna dokumenta ×2, Dokumentacija 1615)
            var d1 = File.ReadAllBytes(PutanjaPravi(@"Izvozna dokumenta\doc00429320261114120847.pdf"));
            var d2 = File.ReadAllBytes(PutanjaPravi(@"Izvozna dokumenta\doc00429320261114120847(1).pdf"));
            p.ZapamtiPriloge(new List<(string, byte[])> { ("doc00429320261114120847.pdf", d1) }, "UID 320", "interni izveštaj poslat", new DateTime(2026, 11, 14, 12, 13, 0));
            Assert.AreEqual("UID 320", p.IstiPrilozi(new List<(string, byte[])> { ("doc00429320261114120847.pdf", d2) })?.Oznaka);
        }

        // ------------------------------------------------------------------------------------------- 10. primaoci u Live režimu

        private const string Agenti = "agenti@kprevoz.co.rs";

        /// <summary>„RE: Arrival schedule in Constanta“ (14.11. 09:19): From + 11 adresa u To (iz zapisa test odgovora; Cc nije zapisan).</summary>
        [TestMethod]
        public void T10_RE_Arrival_schedule_svi_iz_prepiske()
        {
            var to = "stefan.musetescu@tts-group.ro, bogdan.barbulet@pspcargo.ro, disp@kprevoz.co.rs, tibor.tasnadi@pspcargo.ro, madalin.antone@pspcargo.ro, Walentyna.Czeczuga@telfbt.ch, Bojan.Stankovic@tvbratstvo.rs, nagy@budamar.sk, Alan.Goh@telfbt.ch, loy@budamar.sk, mihai.iurian@pspcargo.ro".Split(',').Select(x => x.Trim());
            var p = PrimaociLive.Iz("andrei.terescu@pspcargo.ro", to.Concat(new[] { Agenti }), null, Agenti, null);
            Console.WriteLine("RE: Arrival schedule – Live: " + p.Opis());
            Assert.AreEqual(12, p.To.Count);
            Assert.AreEqual("andrei.terescu@pspcargo.ro", p.To[0], "pošiljalac prvi");
            CollectionAssert.Contains(p.To, "disp@kprevoz.co.rs", "KP adrese ostaju");
            CollectionAssert.DoesNotContain(p.To, Agenti);
            Assert.IsFalse(p.SamoInterno);
        }

        /// <summary>„ARR – Aviso: 46837/13 HER-SAD 79“: 19 adresa u To, sa duplikatima (lacko, havran, dvorak, disp@).</summary>
        [TestMethod]
        public void T10_ARR_Aviso_bez_duplikata()
        {
            var to = ("andras.bachta@crw.sk, disp@kprevoz.co.rs, lacko@budamar.sk, havran@budamar.sk, dolniak@budamar.sk, dvorak@budamar.sk, prepravy@crw.sk, ildi78szekeres@gmail.com, priprema@kprevoz.co.rs, "
                    + "bojan.pavlovic@tvbratstvo.rs, milos.mandic@tvbratstvo.rs, dvorak@budamar.sk, sak@lokorail.sk, dispo@budamar.sk, lacko@budamar.sk, voscek@budamar.sk, havran@budamar.sk, disp@kprevoz.co.rs, aurelijusvirus@gmail.com")
                    .Split(',').Select(x => x.Trim()).ToList();
            Assert.AreEqual(19, to.Count);
            var p = PrimaociLive.Iz("dispo@crw.sk", to, null, Agenti, null);
            Console.WriteLine("ARR – Aviso – Live: " + p.Opis());
            Assert.AreEqual(16, p.Ukupno, "From + 15 različitih adresa");
            Assert.AreEqual(p.To.Count, p.To.Distinct().Count());
            CollectionAssert.Contains(p.To, "priprema@kprevoz.co.rs");
        }

        [TestMethod]
        public void T10_CIM_Pansped_posiljalac_i_priprema()
        {
            var p = PrimaociLive.Iz("branislav.kolarov@pansped.rs", new[] { "priprema@kprevoz.co.rs", Agenti }, null, Agenti, null);
            Console.WriteLine("CIM (Pansped) – Live: " + p.Opis());
            CollectionAssert.AreEqual(new[] { "branislav.kolarov@pansped.rs", "priprema@kprevoz.co.rs" }, p.To);
            Assert.AreEqual("RE: CIM – CIM provera: potrebne ispravke", PrimaociLive.Naslov("CIM", false));
            Assert.AreEqual("RE: CIM – CIM check: corrections required", PrimaociLive.Naslov("CIM", true));
            Assert.AreEqual("RE: Arrival schedule in Constanta – CIM provera: potrebne ispravke", PrimaociLive.Naslov("RE: Arrival schedule in Constanta", false), "bez „RE: RE:“");
        }

        [TestMethod]
        public void T10_Cc_prosledjivanje_i_vise_od_25_primalaca()
        {
            var p = PrimaociLive.Iz("a@x.rs", new[] { "b@x.rs", "fwd@kprevoz.co.rs" }, new[] { "c@x.rs", "B@X.rs", Agenti }, Agenti, new[] { "fwd@kprevoz.co.rs" });
            CollectionAssert.AreEqual(new[] { "a@x.rs", "b@x.rs" }, p.To);
            CollectionAssert.AreEqual(new[] { "c@x.rs" }, p.Cc, "Cc bez duplikata iz To, bez agenti@");
            var mnogo = PrimaociLive.Iz("a@x.rs", Enumerable.Range(1, 26).Select(i => "p" + i + "@x.rs"), null, Agenti, null);
            Assert.IsTrue(mnogo.SamoInterno);
            StringAssert.StartsWith(mnogo.Opis(), "samo interno – mail ima 27 primalaca");
        }

        // ------------------------------------------------------------------------------------------- 8. dosije pošiljke

        private static MailZaDosije Mail8(string oznaka, string naslov, DateTime vreme, string messageId = "", params string[] prethodni) =>
            new MailZaDosije { Oznaka = oznaka, Naslov = naslov, Vreme = vreme, MessageId = messageId, Prethodni = prethodni.ToList() };

        private static List<Dokument> SaListom(CimList c, string izvor = "tekst") => new List<Dokument> { new Dokument { Tip = TipDokumenta.Cim, Fajl = "CIM.pdf", Strane = { 1 }, Izvor = izvor, Cim = { c } } };

        [TestMethod]
        public void T8_Povezivanje_nit_naslov_r8_i_rok_7_dana()
        {
            var d = Dosije.Ucitaj(NovFolder());
            var t0 = new DateTime(2026, 11, 13, 10, 0, 0);
            var spisak = new List<Dokument> { new Dokument { Tip = TipDokumenta.SpisakKola, Fajl = "CIT23 256.pdf", Strane = { 1, 2 } } };
            var p = d.Zapamti(null, Mail8("UID 277", "CIT23 za voz 256 Pančevo Glavna - Portogruaro", t0, "<a@x>"), spisak, null);
            // nit (In-Reply-To)
            Assert.AreSame(p, d.Nadji(Mail8("UID 280", "Nešto drugo", t0.AddHours(2), "<b@x>", "<a@x>"), new List<Dokument>(), out var r1)); StringAssert.Contains(r1, "nit");
            // naslov bez RE:/FW:/Fwd:/Odg:/AW:
            Assert.AreSame(p, d.Nadji(Mail8("UID 281", "Fwd: RE: Odg: CIT23 za voz 256 Pančevo Glavna - Portogruaro", t0.AddHours(3)), new List<Dokument>(), out var r2)); StringAssert.Contains(r2, "naslov");
            // rok 7 dana
            Assert.IsNull(d.Nadji(Mail8("UID 290", "RE: CIT23 za voz 256 Pančevo Glavna - Portogruaro", t0.AddDays(8)), new List<Dokument>(), out _));
            // r8 („Train no: 256F“ / „Train no. 256F“)
            var c1 = MotorTests.IspravanCim(); c1.R62 = new Identifikacija(); c1.R8.Tekst = "Train no: 256F";
            var p2 = d.Zapamti(null, Mail8("UID 262", "CIM", t0), SaListom(c1, "ocr"), null);
            var c2 = MotorTests.IspravanCim(); c2.R62 = new Identifikacija(); c2.R8.Tekst = "Train no. 256F"; c2.R1.Naziv = "Drugi pošiljalac";
            Assert.AreSame(p2, d.Nadji(Mail8("UID 271", "RE: CIM", t0.AddMinutes(16)), SaListom(c2, "ocr"), out var r3)); StringAssert.Contains(r3, "r8");
            // kratak opšti naslov („CIM“) sam nije veza
            Assert.IsNull(d.Nadji(Mail8("UID 299", "CIM", t0.AddHours(5)), new List<Dokument>(), out _));
        }

        [TestMethod]
        public void T8_Naslov_pominje_posiljaoca_lista_iz_dosijea()
        {
            var d = Dosije.Ucitaj(NovFolder());
            var c = MotorTests.IspravanCim(); c.R1.Naziv = "Yu Din Commerce D.o.o";
            var p = d.Zapamti(null, Mail8("UID 315", "Sremska Mitrovica - Revere carinjenje", new DateTime(2026, 11, 14, 11, 55, 0)), SaListom(c), null);
            Assert.AreSame(p, d.Nadji(Mail8("UID 335", "CIM YU DIN COMMERCE", new DateTime(2026, 11, 14, 12, 25, 0)), new List<Dokument>(), out var r));
            StringAssert.Contains(r, "yu din commerce");
        }

        [TestMethod]
        public void T8_Dokumenti_iz_dosijea_vaze_kao_prilozeni()
        {
            var folder = NovFolder();
            var d = Dosije.Ucitaj(folder);
            var docs = new List<Dokument> { new Dokument { Tip = TipDokumenta.IzvoznaDeklaracija, Fajl = "EX.pdf", Strane = { 1 } }, new Dokument { Tip = TipDokumenta.Faktura, Fajl = "Faktura.pdf", Strane = { 1 } } };
            d.Zapamti(null, Mail8("UID 100", "IZVOZ GIPS", new DateTime(2026, 11, 13, 10, 0, 0)), docs, null);
            d.Sacuvaj();
            var p = Dosije.Ucitaj(folder).Posiljke.Single();
            var iz = Dosije.DokumentiIzDosijea(p);
            Assert.AreEqual(2, iz.Count);
            Assert.IsTrue(iz.All(x => x.IzDosijea && x.IzMaila == "UID 100"));
            // dokumenti iz dosijea se ne navode kao dokumenti ovog maila
            Assert.AreEqual("nema CIM/CUV – bez odgovora; dokumenti: nema prepoznatih", Izvestaj.LogBezTovarnogLista(iz));
        }

        [TestMethod]
        public void T8_Nova_verzija_istog_lista_ispravljeno_i_ostalo()
        {
            var p = new Dosije.Posiljka { Prvi = new DateTime(2026, 11, 13), Poslednji = new DateTime(2026, 11, 13) };
            var c = MotorTests.IspravanCim();
            p.Listovi.Add(new Dosije.ListZapis
            {
                Kljuc = Ponavljanja.KljucLista(c), Otisak = "staro", Oznaka = "UID 100", Vreme = new DateTime(2026, 11, 13, 9, 0, 0), Digitalni = true,
                Nalazi = new List<string> { "R9|9", "R24-NHM6|24", "R1|1", "R16|16" },
            });
            var r = RezultatSaListom(c);
            r.Listovi[0].Nalazi.Add(new Nalaz { Pravilo = "R1", Rubrika = "1", Tezina = Tezina.Greska });
            r.Listovi[0].Nalazi.Add(new Nalaz { Pravilo = "R16", Rubrika = "16", Tezina = Tezina.Greska });
            Dosije.Primeni(p, r, false);
            Assert.AreEqual("Ovo je nova verzija tovarnog lista od 13.11.2026. Ispravljeno: r9, r24. Ostalo neispravljeno: r1, r16.", r.UvodDosijea);
            StringAssert.Contains(Izvestaj.Telo(r, pravila, false), "Ovo je nova verzija tovarnog lista od 13.11.2026.");
        }

        [TestMethod]
        public void T8_Skenirana_overena_verzija_proverava_samo_pecat_r62_r29()
        {
            var p = new Dosije.Posiljka { Prvi = new DateTime(2026, 11, 14), Poslednji = new DateTime(2026, 11, 14) };
            var c = MotorTests.IspravanCim();
            p.Listovi.Add(new Dosije.ListZapis { Kljuc = Ponavljanja.KljucLista(c), Otisak = "digitalni", Oznaka = "UID 315", Vreme = new DateTime(2026, 11, 14, 11, 55, 0), Digitalni = true });
            var d = new Dokument { Tip = TipDokumenta.Cim, Fajl = "1722_scan.pdf", Strane = { 1 }, Izvor = "ocr", Cim = { c } };
            var r = new RezultatProvere(); r.Dokumenti.Add(d);
            var l = new RezultatLista { Dokument = d, Cim = c, Oznaka = "1" };
            foreach (var rb in new[] { "1", "4", "16", "24", "58a", "62", "29" }) l.Nalazi.Add(new Nalaz { Pravilo = "R" + rb, Rubrika = rb, Tezina = Tezina.Greska });
            r.Listovi.Add(l);
            Dosije.Primeni(p, r, false);
            CollectionAssert.AreEquivalent(new[] { "58a", "62", "29" }, l.Nalazi.Select(n => n.Rubrika).ToList());
            StringAssert.StartsWith(r.UvodDosijea, "Ovo je skenirana i overena verzija tovarnog lista koji je već proveren 14.11.2026 (UID 315, digitalni).");
        }

        // ------------------------------------------------------------------------------------------- zaštita režima Test

        [TestMethod]
        public void Test_rezim_salje_samo_na_agenti()
        {
            var cfg = RegresijaTests.Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje"));
            cfg.Rezim = Rezim.Test;
            var transport = Slanje.Transport; var zapis = Slanje.ZapisZastite;
            var poslato = new List<MimeKit.MimeMessage>(); var log = new List<string>();
            try
            {
                Slanje.Transport = (c, m) => poslato.Add(m);
                Slanje.ZapisZastite = log.Add;
                var m1 = new MimeKit.MimeMessage();
                m1.From.Add(MimeKit.MailboxAddress.Parse("agenti@kprevoz.co.rs"));
                m1.To.Add(MimeKit.MailboxAddress.Parse("branislav.kolarov@pansped.rs"));
                m1.Cc.Add(MimeKit.MailboxAddress.Parse("priprema@kprevoz.co.rs"));
                m1.Cc.Add(MimeKit.MailboxAddress.Parse("Agenti@KPrevoz.co.rs"));
                m1.Bcc.Add(MimeKit.MailboxAddress.Parse("neko@gmail.com"));
                m1.Subject = "test"; m1.Body = new MimeKit.TextPart("plain") { Text = "x" };
                Slanje.Posalji(cfg, m1);
                var p1 = poslato.Single();
                var svi = p1.To.Mailboxes.Concat(p1.Cc.Mailboxes).Concat(p1.Bcc.Mailboxes).Select(x => x.Address.ToLowerInvariant()).ToList();
                CollectionAssert.AreEqual(new[] { "agenti@kprevoz.co.rs" }, svi);
                CollectionAssert.AreEquivalent(new[] { "TEST: uklonjen primalac branislav.kolarov@pansped.rs", "TEST: uklonjen primalac priprema@kprevoz.co.rs", "TEST: uklonjen primalac neko@gmail.com" }, log);
                // samo spoljna adresa → ide samo na agenti@
                var m2 = new MimeKit.MimeMessage();
                m2.To.Add(MimeKit.MailboxAddress.Parse("dispo@crw.sk")); m2.Subject = "t"; m2.Body = new MimeKit.TextPart("plain") { Text = "x" };
                Slanje.Posalji(cfg, m2);
                CollectionAssert.AreEqual(new[] { "agenti@kprevoz.co.rs" }, poslato[1].To.Mailboxes.Select(x => x.Address).ToList());
                Assert.AreEqual(0, poslato[1].Cc.Count + poslato[1].Bcc.Count);
                CollectionAssert.Contains(log, "TEST: uklonjen primalac dispo@crw.sk");
            }
            finally { Slanje.Transport = transport; Slanje.ZapisZastite = zapis; }
        }

        // ------------------------------------------------------------------------------------------- regresija: pravi saobraćaj 14.11.

        private const string Pravi = @"\\192.168.1.6\Cloude\Agenti\CimProvera\testovi\nezavisni\2026-11-14-pravi\prilozi";
        private static readonly Dictionary<string, Ishod> ishodi = new Dictionary<string, Ishod>(StringComparer.OrdinalIgnoreCase);

        internal static string PutanjaPravi(string relativno)
        {
            var folder = Environment.GetEnvironmentVariable("CIMPROVERA_TESTOVI") is string f && f.Length > 0 ? Path.Combine(f, "nezavisni", "2026-11-14-pravi", "prilozi") : Pravi;
            var put = Path.Combine(folder, relativno);
            if (!File.Exists(put)) Assert.Inconclusive("Nema " + put);
            return put;
        }

        /// <summary>Jedan mail iz pravog saobraćaja (svi prilozi); čita se jednom po pokretanju testova.</summary>
        internal static Ishod Mail(string posiljalac, DateTime datum, params string[] fajlovi)
        {
            var putanje = fajlovi.Select(PutanjaPravi).ToList();
            var kljuc = posiljalac + "|" + string.Join("|", putanje);
            lock (ishodi)
            {
                if (ishodi.TryGetValue(kljuc, out var r)) return r;
                var cfg = RegresijaTests.Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje"));
                using (var c = new LokalniCitac(cfg, null, pravila.Kp))
                    r = new Obrada(pravila, c, null, cfg.PragPouzdanosti).Proveri(putanje.Select(p => (Path.GetFileName(p), File.ReadAllBytes(p))).ToList(),
                        posiljalac, cfg.JeInterni(posiljalac), datum, false);
                return ishodi[kljuc] = r;
            }
        }

        /// <summary>Mail bez tovarnog lista: nema lista, nema nepročitanih/neprepoznatih priloga → nikakav odgovor (ni interni).</summary>
        private static void BezOdgovora(Ishod i)
        {
            var opis = Izvestaj.Dokumenti(i.Provera.Dokumenti) + " | nepročitano: " + string.Join("; ", i.NisuProcitani) + " | nepouzdano: " + string.Join("; ", i.NisaPouzdanost)
                       + " | neprepoznato: " + string.Join("; ", i.Neprepoznati);
            Assert.IsFalse(i.Provera.ImaTovarniList, "nema tovarnog lista: " + opis);
            Assert.AreEqual(0, i.NisuProcitani.Count + i.Neprepoznati.Count + i.NisaPouzdanost.Count, "ništa ne sme ići na ručnu proveru: " + opis);
        }

        [TestMethod, TestCategory("Regresija")]
        public void T1_CIT23_za_voz_258_bez_odgovora() => BezOdgovora(Mail("stefan.kalicanin@eurologsystem.rs", new DateTime(2026, 11, 14, 8, 27, 0),
            @"CIT23 za voz 258 Šid (u.m. Kukujevci) - San Giovanni In Persiceto\1.64-276F-2026_ŠID (u.m. KUKUJEVCI) - SEŽANA_VILLA OPICINA_258.pdf",
            @"CIT23 za voz 258 Šid (u.m. Kukujevci) - San Giovanni In Persiceto\CIT-23 for the train no. 258 Šid (Kukujevci)-San Giovanni In Persiceto (KP, GPR, SŽ, RTC).xlsx"));

        [TestMethod, TestCategory("Regresija")]
        public void T1_CIT23_za_voz_256_bez_odgovora() => BezOdgovora(Mail("sara.markovic@eurologsystem.rs", new DateTime(2026, 11, 14, 9, 15, 0),
            @"CIT23 za voz 256 Pančevo Glavna (Pančevo Varoš)-Portogruaro Caorle\1.218-274F-2026_PANČEVO GLAVNA (u.m. PANČEVO VAROŠ) - PORTOGRUARO CAORLE_256.pdf",
            @"CIT23 za voz 256 Pančevo Glavna (Pančevo Varoš)-Portogruaro Caorle\CIT-23 for the train no. 256 Pančevo G. (Pančevo Varoš)-Portogruaro Caorle (KP, GPR, SŽ, MIR).xlsx"));

        [TestMethod, TestCategory("Regresija")]
        public void T1_CHS_Cereal_docks_bez_odgovora() => BezOdgovora(Mail("branislav.kolarov@pansped.rs", new DateTime(2026, 11, 14, 12, 29, 0),
            @"CHS Cereal docks\20261114122219.pdf"));

        [TestMethod, TestCategory("Regresija")]
        public void T1_RE_CHS_Customs_documents_256_bez_odgovora() => BezOdgovora(Mail("sara.markovic@eurologsystem.rs", new DateTime(2026, 11, 14, 12, 41, 0),
            @"CHS Cereal docks\Customs documents for the train no. 256 Pančevo-Portogruaro Caorle.pdf"));

        /// <summary>Voz 258 (East Grain → Molino di San Giovanni, r62 72 16513-4 / 3212 / 161500): položeni sken, KP tovarni list → interni izveštaj.</summary>
        private static void Voz258(Ishod i, params int[] strane)
        {
            var opis = Izvestaj.Dokumenti(i.Provera.Dokumenti) + " | nepouzdano: " + string.Join("; ", i.NisaPouzdanost);
            Assert.IsTrue(i.Provera.ImaTovarniList, "tovarni list voza 258 mora biti pročitan: " + opis);
            foreach (var s in strane) Assert.IsTrue(i.Provera.Dokumenti.Any(d => TipDokumenta.JeTovarniList(d.Tip) && d.Strane.Contains(s)), "str. " + s + " je tovarni list: " + opis);
            var l = i.Provera.Listovi.Single(x => x.Cim != null && string.IsNullOrEmpty(x.Cim.NeprepoznatObrazac));
            StringAssert.Contains(l.Cim.R1.Tekst ?? "", "EAST GRAIN", "r1: " + l.Cim.R1.Tekst);
            StringAssert.Contains((l.Cim.R4.Tekst ?? "").ToUpperInvariant(), "MOLINO", "r4: " + l.Cim.R4.Tekst);
            // samo interni izveštaj (KP tovarni list kada je r62 = 3212 pročitana; inače ručna provera – r62 je rukom upisana) – nikad pošiljaocu
            Assert.IsNotNull(Izvestaj.RazlogInternog(i.Provera, pravila), "samo interni izveštaj");
            Console.WriteLine("voz 258: r62 „" + l.Cim.R62.Tekst + "“, KP režim " + l.KpRezim + ", razlog " + Izvestaj.RazlogInternog(i.Provera, pravila) + "; " + opis);
        }

        [TestMethod, TestCategory("Regresija")]
        public void T2_Izvozna_dokumenta_polozen_sken_voz_258() => Voz258(Mail("office@evroline.rs", new DateTime(2026, 11, 14, 12, 13, 0),
            @"Izvozna dokumenta\doc00429320261114120847.pdf"), 9);

        [TestMethod, TestCategory("Regresija")]
        public void T2_JCI_i_CIM_258_polozen_sken() => Voz258(Mail("stefan.kalicanin@eurologsystem.rs", new DateTime(2026, 11, 14, 12, 31, 0),
            @"JCI i CIM  za voz 258 Šid (u.m. Kukujevci) - San Giovanni In Persiceto\JCI and CIM for the train no. 258 Šid (Kukujevci)-San Giovanni In Persiceto.pdf"), 2);

        [TestMethod, TestCategory("Regresija")]
        public void T2_Dokumentacija_za_predaju_1615_polozen_sken_voz_258() => Voz258(Mail("priprema@kprevoz.co.rs", new DateTime(2026, 11, 14, 12, 23, 0),
            @"Dokumentacija za predaju ID 1615 Kukujevci - Šid gr\doc00429320261114120847.pdf", @"Dokumentacija za predaju ID 1615 Kukujevci - Šid gr\K200.pdf",
            @"Dokumentacija za predaju ID 1615 Kukujevci - Šid gr\Kol65.pdf"), 9);

        /// <summary>„CIM YU DIN COMMERCE“ (crno-beli sken, 2.8.3: OCR 0,70): pouzdanost ne sme biti lošija; vrednost pre/posle ide u izveštaj.</summary>
        [TestMethod, TestCategory("Regresija")]
        public void T2_CIM_YU_DIN_COMMERCE_crno_beli_sken()
        {
            var i = Mail("negovansm@gmail.com", new DateTime(2026, 11, 14, 12, 25, 0), @"CIM YU DIN COMMERCE\1722_261114123020_001.pdf");
            var d = i.Provera.Dokumenti.Single(x => TipDokumenta.JeTovarniList(x.Tip));
            var p = d.Cim.Single().PouzdanostStrane;
            Console.WriteLine("CIM YU DIN COMMERCE: pouzdanost OCR-a " + p.ToString("0.00") + " (2.8.3: 0,70); " + d.Opis);
            Assert.IsTrue(p >= 0.70, "pouzdanost " + p + " – " + d.Opis);
            // r62 je rukom upisana („72 / 165092 / 3212 / 162600“) – OCR je ne čita pouzdano: ne koristi se za odluke
            var l = i.Provera.Listovi.Single();
            Assert.IsTrue(l.Cim.R62.NepouzdanaZaOdluke, "r62: " + l.Cim.R62.Tekst);
            Assert.IsFalse(l.KpRezim, "nepouzdana r62 ne određuje KP tovarni list");
            Assert.IsTrue(l.Nalazi.Any(n => n.Rubrika == "62" && n.SamoInterno && (n.Tekst ?? "").StartsWith("r62 nečitljiva")), "r62 nečitljiva – ručna provera");
            Assert.IsFalse((Ponavljanja.KljucLista(l.Cim) ?? "").StartsWith("r62:"), "ključ ponavljanja/dosijea ne sme biti nepouzdana r62");
        }

        /// <summary>
        /// „Bez odgovora“ samo kad je sigurno: Lukavac02 zip – WhatsApp fotografije sa 8–12 natpisa rubrika (bez pročitanog natpisa obrasca)
        /// nisu tovarni listovi, ali se ne smeju prećutati – idu na ručnu proveru.
        /// </summary>
        [TestMethod, TestCategory("Regresija")]
        public void T1_Lukavac_zip_fotografije_koje_lice_na_obrazac_idu_na_rucnu_proveru()
        {
            var i = Mail("dispo@trainhungary.hu", new DateTime(2026, 11, 14, 7, 53, 0), @"Lukavac02\LUKAVAC02_Hidasnémeti_Oh-_Subotica.zip");
            var opis = Izvestaj.Dokumenti(i.Provera.Dokumenti) + " | nepročitano: " + string.Join("; ", i.NisuProcitani);
            Assert.IsTrue(i.NisuProcitani.Any(x => x.Contains("liči na obrazac CIM/CUV")), opis);
        }

        [TestMethod, TestCategory("Regresija")]
        public void T1_Dokumentacija_za_predaju_1637_bez_odgovora() => BezOdgovora(Mail("priprema@kprevoz.co.rs", new DateTime(2026, 11, 14, 12, 47, 0),
            @"Dokumentacija za predaju ID 1615 Kukujevci - Šid gr\K200.pdf", @"Dokumentacija za predaju ID 1615 Kukujevci - Šid gr\Kol65.pdf",
            @"CHS Cereal docks\Customs documents for the train no. 256 Pančevo-Portogruaro Caorle.pdf"));
    }
}
