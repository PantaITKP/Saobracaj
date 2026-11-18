using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Saobracaj.Agent.CimProvera.Baza;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;
using Saobracaj.Agent.CimProvera.Radionica;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>v3.0.1 – ispravke posle analize obrade „Osnovni“ (IZVESTAJ_Osnovni_3.0.0): zemlje, upis poslova, prilozi, događaji, SAŽETAK.</summary>
    [TestClass]
    public class V301Tests
    {
        private static PravilaSkup pravila;

        [ClassInitialize]
        public static void Init(TestContext _)
        {
            var put = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pravila", "cim_validation_rules.json");
            pravila = PravilaSkup.IzTeksta(File.ReadAllText(put), put);
        }

        // ------------------------------------------------------------------------------------------- 1.2 oblici zemlje

        [TestMethod]
        public void Zemlje_prepoznavanje_oblika_suseda_i_cestih_zemalja()
        {
            foreach (var (tekst, oblik, iso) in new[]
            {
                ("Kraljice Katarine 158\n11030 Beograd\nSRB", "SRB", "RS"),
                ("SR-21000 Novi Sad", "SR", "RS"),
                ("11000 Beograd, Србија", "Србија", "RS"),
                ("19330 Prahovo\nSrbijja", "Srbijja", "RS"),
                ("44653 Herne, Deutchland", "Deutchland", "DE"),
                ("170 00 Praha 7, Česká republika", "Česká republika", "CZ"),
                ("1134 Budapest, Magyarország", "Magyarország", "HU"),
                ("Ankara, Macaristan", "Macaristan", "HU"),
                ("Constanța, Румъния", "Румъния", "RO"),
                ("Sofia, BGR", "BGR", "BG"),
                ("10000 Zagreb, Hrvatska", "Hrvatska", "HR"),
                ("Wien, Österreich", "Österreich", "AT"),
                ("SZERBIA", "SZERBIA", "RS"),
                ("Konya, Türkiye", "Türkiye", "TR"),
            })
            {
                Assert.AreEqual(oblik, Zemlje.Nadji(tekst), tekst);
                Assert.AreEqual(iso, Zemlje.Iso(oblik), oblik);
            }
            // nije zemlja: „Bul.“ u adresi, slovačko „SR“ (Slovenská republika), reč usred naziva
            Assert.AreEqual("", Zemlje.Nadji("Bul. Mihajla Pupina 10\n11070 Beograd"));
            Assert.AreEqual("", Zemlje.Nadji("Horárska 12\n81524 Bratislava, SR"));
            Assert.IsNull(Zemlje.Iso("Beograd"));
        }

        // ------------------------------------------------------------------------------------------- 3.1 kopije

        private static ListPosla L(string kljuc, double? neto, double? tara, double? bruto, string broj = "", string kola = null, string voz = "", string referenca = null, int s1 = 118, int s3 = 269)
        {
            var p = new PodaciPosla { KljucPosiljke = kljuc, NetoKg = neto, TaraKg = tara, BrutoKg = bruto, BrojOtpravljanja = broj, BrojVoza = voz, Nhm4 = "2814" };
            if (kola != null) p.Kola = kola.Split(',').ToList();
            p.V["OtpravnaID"] = 1260; p.V["UputnaID"] = 269; p.V["Stanica1ID"] = s1; p.V["Stanica3ID"] = s3; p.V["BrojKola1"] = 12;
            p.Postavi("Referenca", referenca, referenca ?? "", "r8", referenca == null ? 0 : 90);
            return new ListPosla { P = p, Oznaka = kljuc };
        }

        [TestMethod]
        public void Amonijak_razliciti_tovarni_listovi_nisu_kopije()
        {
            // UID 259: 001784 i 001792 – r62 nesiguran, mase 955 450 / 536 500 / 418 950 i 954 500 / 535 200 / 419 300
            var a = L("sadrzaj:x", 536500, 418950, 955450, "007777");
            var b = L("sadrzaj:x", 535200, 419300, 954500, "");
            Assert.AreEqual(2, UpisUBazu.SpojiKopije(new List<ListPosla> { a, b }).Count, "različite mase (± 0,1 %) – dva lista → 24 kola");
            // prava kopija (iste mase) se spaja
            var kopija = L("sadrzaj:y", 536500, 418950, 955450, "");
            Assert.AreEqual(1, UpisUBazu.SpojiKopije(new List<ListPosla> { a, kopija }).Count);
            // različit broj otpravljanja – nikad kopija
            Assert.AreEqual(2, UpisUBazu.SpojiKopije(new List<ListPosla> { L("sadrzaj:z", 536500, 418950, 955450, "001784"), L("sadrzaj:z", 536500, 418950, 955450, "001792") }).Count);
            Assert.IsTrue(UpisUBazu.KompatibilanBroj("001784", "001734"), "jedna cifra (OCR)");
            Assert.IsFalse(UpisUBazu.KompatibilanBroj("001784", "001792"));
            // r18 „See list: 12 Z-wagons“ – broj kola i uz reč sa crticom (2 × 12 = 24 kola)
            Assert.AreEqual(12, Provere.BrojKolaUTekstu("See   list:   12 Z-wagons"));
            // zapisi TL iz istog maila (± 0,1 %)
            Assert.IsFalse(UpisUBazu.IstiZapis(ZapisTl.Iz(a), ZapisTl.Iz(b)));
        }

        // ------------------------------------------------------------------------------------------- 3.2 spisak usklađen sam sa sobom

        private static SpisakKola Sp(int n, double? neto, double? tara, double? bruto, bool prazna = false, string nhm = "281410")
        {
            var s = new SpisakKola();
            for (int i = 0; i < n; i++) s.Kola.Add(new KolaNaSpisku { Broj = "3352781303" + i.ToString("00"), Nhm = nhm, NetoKg = neto, TaraKg = tara, BrutoKg = bruto, Prazna = prazna });
            return s;
        }

        [TestMethod]
        public void Spisak_koji_nije_uskladjen_sam_sa_sobom_ne_gazi_r25()
        {
            var pk = new Poklapanje(UpisUBazuTests.Sif());
            // bugarski CIT 23: „neto 815,9 / bruto 318,5“ – kolone pogrešno pročitane (bruto manji od neta)
            var p = PosaoIzDokumenata.IzSpiska(Sp(12, 67990, 34900, 26540), pk, "", "cit23.pdf");
            Assert.IsTrue(p.SpisakNeuskladjen);
            Assert.IsFalse(p.V.ContainsKey("Neto1") || p.V.ContainsKey("Bruto1") || p.V.ContainsKey("Tara1"), "mase spiska se ne upisuju – važi r25");
            StringAssert.Contains(string.Join(" ", p.Napomene), "Spisak kola – proveriti kolone");
            // bruto ≠ neto + tara (± 0,5 %)
            Assert.IsTrue(PosaoIzDokumenata.Neuskladjenost(Sp(18, 55832, 33088, 85391).Kola, false, 1004970, 595600, 1537000).Any(x => x.Contains("≠ neto")));
            // usklađen spisak ima prednost
            var ok = PosaoIzDokumenata.IzSpiska(Sp(12, 45000, 34900, 79900), pk, "", "cit23.pdf");
            Assert.IsFalse(ok.SpisakNeuskladjen);
            Assert.AreEqual(540.000m, ok.V["Neto1"]); Assert.AreEqual(958.800m, ok.V["Bruto1"]);
        }

        [TestMethod]
        public void HSL_red_zaglavlja_sa_brojem_kola_ne_zaklanja_red_kola()
        {
            // HER-SAD 81: red „1_2 | 3_4 | 5_8 | 9_11 | 12 | m | t | t | t | … | 35 80 4956 491 - 9“ pre reda prvih kola
            var redovi = new List<string[]>
            {
                new[] { "Lfn.", "Wagennummer", "Eigengewicht", "Gewicht der Ladung", "Gesamt- gewicht", "" },
                new[] { "", "1_2", "t", "t", "t", "35 80 4956 491 - 9" },
                new[] { "1", "35 80 4956 491 - 9", "35", "27", "62", "" },
                new[] { "2", "35 80 4956 566 - 8", "35", "51.5", "86.5", "" },
            };
            var d = ExcelCitac.AnalizaRedova(redovi, pravila.Kp, "hsl.xls", "excel", "Excel");
            var s = d.SelectMany(x => x.Spisak).First();
            Assert.AreEqual(2, s.Kola.Count);
            var k1 = s.Kola.First(k => k.Broj == "358049564919");
            Assert.AreEqual(35000, k1.TaraKg, "tara prvih kola iz reda kola, ne iz zaglavlja");
            Assert.AreEqual(27000, k1.NetoKg); Assert.AreEqual(62000, k1.BrutoKg);
        }

        // ------------------------------------------------------------------------------------------- 3.3 radna → konačna verzija (CUV)

        [TestMethod]
        public void Radna_verzija_CUV_bez_r62_i_konacna_sa_r62_se_zamenjuju_ne_sabiraju()
        {
            var radna = new ZapisTl { Kljuc = "sadrzaj:017e49", Obrazac = "CUV", Prazna = true, Nhm4 = "9922", BrojKola = 37 };
            var konacna = new ZapisTl { Kljuc = "r62:72-234500-3212-001657", Obrazac = "CUV", Prazna = true, Nhm4 = "9922", BrojKola = 37, Tara = 828750 };
            Assert.IsTrue(UpisUBazu.IstiZapis(radna, konacna), "Bor ID 1657 → 37 kola, ne 74");
            Assert.IsFalse(UpisUBazu.IstiZapis(radna, new ZapisTl { Kljuc = "r62:72-234500-3212-001658", Prazna = true, Nhm4 = "9922", BrojKola = 20 }), "drugi broj kola");
        }

        // ------------------------------------------------------------------------------------------- 3.4 spisak praznih kola

        [TestMethod]
        public void Spisak_praznih_kola_jedina_masa_je_tara()
        {
            var s = Sp(37, 22398, null, null, true, "9922");
            Assert.IsTrue(PosaoIzDokumenata.NormalizujPrazna(s, "CIT 23 prazno - Copy - Copy.pdf"));
            Assert.IsTrue(s.Kola.All(k => k.TaraKg == 22398 && k.NetoKg == 0 && k.BrutoKg == 22398));
            // bez NHM-a, „prazno“ u imenu fajla
            var s2 = Sp(3, 22000, null, null, false, "");
            Assert.IsTrue(PosaoIzDokumenata.NormalizujPrazna(s2, "CIT 23 prazno.pdf"));
            Assert.AreEqual(22000, s2.Kola[0].TaraKg);
            // natovarena kola se ne diraju
            var s3 = Sp(3, 60000, 22000, 82000);
            Assert.IsFalse(PosaoIzDokumenata.NormalizujPrazna(s3, "spisak.pdf"));
            Assert.AreEqual(60000, s3.Kola[0].NetoKg);
        }

        // ------------------------------------------------------------------------------------------- 3.5 veza iz dosijea

        [TestMethod]
        public void Overena_verzija_iz_dosijea_nosi_vezu_za_posao()
        {
            var p = new Dosije.Posiljka { Prvi = new DateTime(2026, 11, 14), Poslednji = new DateTime(2026, 11, 14) };
            var c = MotorTests.IspravanCim();
            p.Listovi.Add(new Dosije.ListZapis { Kljuc = Ponavljanja.KljucLista(c), Otisak = "digitalni", Oznaka = "UID 46", Vreme = new DateTime(2026, 11, 14, 11, 52, 0), Digitalni = true });
            var d = new Dokument { Tip = TipDokumenta.Cim, Fajl = "CIM YU DIN scan.pdf", Strane = { 1 }, Izvor = "ocr", Cim = { c } };
            var r = new RezultatProvere(); r.Dokumenti.Add(d);
            var l = new RezultatLista { Dokument = d, Cim = c, Oznaka = "1" };
            r.Listovi.Add(l);
            Dosije.Primeni(p, r, false);
            Assert.AreEqual("UID 46", l.DosijeVeza);
            Assert.IsTrue(l.OverenaVerzija);
            var pp = PosaoIzDokumenata.Napravi(c, d, l, new Poklapanje(UpisUBazuTests.Sif()), pravila.Kp);
            Assert.AreEqual("UID 46", pp.DosijeVeza); Assert.IsTrue(pp.OverenaVerzija);
        }

        // ------------------------------------------------------------------------------------------- 3.6 različit broj voza

        [TestMethod]
        public void Razlicit_broj_voza_ili_referenca_su_razliciti_poslovi()
        {
            var vozovi = UpisUBazu.Vozovi(new List<ListPosla>
            {
                L("ref:268F|1|2", 1, 1, 2, voz: "268F", kola: "315554000242"), L("ref:269F|1|2", 1, 1, 2, voz: "269F", kola: "315554000243"),
                L("ref:270F|1|2", 1, 1, 2, voz: "270F", kola: "315554000244"), L("ref:272F|1|2", 1, 1, 2, voz: "272F", kola: "315554000245"),
            });
            Assert.AreEqual(4, vozovi.Count, "Eurolog UID 170: 4 voza → 4 posla");
            Assert.IsFalse(UpisUBazu.IstiVozIliNepoznat(L("a", 1, 1, 2, referenca: "Budamar Coal 16"), L("b", 1, 1, 2, referenca: "Budamar Coal 17")));
            Assert.IsTrue(UpisUBazu.IstiVozIliNepoznat(L("a", 1, 1, 2, voz: "268F"), L("b", 1, 1, 2)), "jedan nema broj voza");
        }

        // ------------------------------------------------------------------------------------------- 3.8 / 3.9 provera smisla, BrojPonude, Referenca

        [TestMethod]
        public void Provera_smisla_neto_i_polja_bez_suma()
        {
            StringAssert.Contains(PosaoIzDokumenata.NesmislenNeto(4945, 26, false, null), "manje od 1 t po kolima");
            Assert.IsNull(PosaoIzDokumenata.NesmislenNeto(4945, 26, true, null), "prazna kola");
            Assert.IsNull(PosaoIzDokumenata.NesmislenNeto(1560000, 26, false, 1550000));
            StringAssert.Contains(PosaoIzDokumenata.NesmislenNeto(5000000, 0, false, 1000000), "300 %");
            // Stanica1 „Kostolac“ iz OCR šuma „Kost…“: tačka predaje iz teksta koja nije granična i nije pouzdana → NULL i kandidati
            var pk = new Poklapanje(UpisUBazuTests.Sif());
            var g = PosaoIzDokumenata.NesigurnaTackaPredaje(pk, new Saobracaj.Agent.CimProvera.Baza.Pogodak { Id = 380, Pouzdanost = 89, Tekst = "Franko prevoznina (X) Kost" });
            Assert.IsNull(g.Id); StringAssert.Contains(g.Kandidati, "380");
            Assert.AreEqual(381, PosaoIzDokumenata.NesigurnaTackaPredaje(pk, new Saobracaj.Agent.CimProvera.Baza.Pogodak { Id = 381, Pouzdanost = 89 }).Id, "granična stanica ostaje");
        }

        [TestMethod]
        public void BrojPonude_ceo_broj_ili_strani_ugovor_Referenca_bez_PIB_i_natpisa()
        {
            foreach (var x in new[] { "8957", "8331", "42-0003-26" }) Assert.IsTrue(PosaoIzDokumenata.JeBrojPonude(x), x);
            foreach (var x in new[] { "Kuty br/Lanzhot br", "KP2026", "0362.03", "", null }) Assert.IsFalse(PosaoIzDokumenata.JeBrojPonude(x), x ?? "null");
            var c = MotorTests.IspravanCim();
            c.R4.Tekst = "VRANJICA d.o.o.\nKraljice Katarine 158\n11030 Belgrade\nJČO:6007205003";
            StringAssert.Contains(PosaoIzDokumenata.NijeReferenca("\"600 7205003/", "\"600 7205003/", c), "PIB");
            StringAssert.Contains(PosaoIzDokumenata.NijeReferenca("\"600 7205003/", "\"600 7205003/", MotorTests.IspravanCim()), "PIB", "i kada PIB nije u pročitanim rubrikama");
            Assert.AreEqual("natpis obrasca", PosaoIzDokumenata.NijeReferenca("Рефернним", "Рефернним", c));
            Assert.IsNull(PosaoIzDokumenata.NijeReferenca("Budamar Coal 16", "Budamar Coal 16", c));
            Assert.IsNull(PosaoIzDokumenata.NijeReferenca("LUKAVAC02", "LUKAVAC02", c));
        }

        // ------------------------------------------------------------------------------------------- 4. prilozi

        private static byte[] Slika(int w, int h, ImageFormat f = null)
        {
            var rnd = new Random(w * 17 + h);
            var korak = (long)w * h > 400_000 ? 3 : 1;
            using (var b = new Bitmap(w, h))
            {
                for (int y = 0; y < h; y += korak) for (int x = 0; x < w; x += korak) b.SetPixel(x, y, Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256)));
                using (var ms = new MemoryStream()) { b.Save(ms, f ?? ImageFormat.Png); return ms.ToArray(); }
            }
        }

        [TestMethod]
        public void Slike_iz_potpisa_ne_ucestvuju_u_ponavljanju()
        {
            var baner = Slika(423, 197);   // image001.png iz Outlook potpisa (~190 KB)
            Assert.IsTrue(baner.Length > 100 * 1024, "veće od 100 KB, ali mala dimenzija – slika iz potpisa");
            Assert.IsNotNull(Raspakivanje.Ukras("image001.png", baner));
            var pon = Ponavljanja.Ucitaj(Path.Combine(Path.GetTempPath(), "CimProveraTest", "pon301_" + Guid.NewGuid().ToString("N")));
            var prilozi = new List<(string, byte[])> { ("image001.png", baner) };
            pon.ZapamtiPriloge(prilozi, "UID 1", "Gotova", DateTime.Now);
            Assert.IsNull(pon.IstiPrilozi(prilozi), "mail sa samo slikama iz potpisa nije „isti prilog kao ranije“");
            Assert.AreEqual(0, pon.Prilozi.Count);
        }

        [TestMethod]
        public void Fotografija_nije_logo_ide_na_klasifikaciju()
        {
            var foto = Slika(480, 360, ImageFormat.Jpeg);
            Prilog P(string ime) => new Prilog { Naziv = ime, Sadrzaj = foto, Vrsta = VrstaPriloga.Slika, MediaType = "image/jpeg" };
            // UID 171: fotografije iz ZIP-a sa brojem kola u imenu
            Assert.IsNull(Raspakivanje.Ukras(P("26.11.15_Vrsac_ID1606_fotografije.zip/26.11.15_Vrsac_ID1606_31785300434-7_K_R1_za_3.3.4.jpg")));
            Assert.IsNull(Raspakivanje.Ukras(P("31785300434-7.jpg")), "broj kola u imenu");
            Assert.IsNull(Raspakivanje.Ukras(P("viber_image_2026-11-15_10-20-11.jpg")), "CIM kao viber fotografija");
            Assert.IsNotNull(Raspakivanje.Ukras(P("image003.jpg")), "ista mala slika bez imena dokumenta – slika iz potpisa");
            // velika slika (> 1000 px) nije logo ni sa imenom „logo“
            Assert.IsNull(Raspakivanje.Ukras(new Prilog { Naziv = "logo.png", Sadrzaj = Slika(1240, 1100), Vrsta = VrstaPriloga.Slika }));
        }

        [TestMethod]
        public void Instalacija_alata_pravi_Syncfusion_dpapi_bez_kljuca_u_skripti()
        {
            var koren = AppDomain.CurrentDomain.BaseDirectory;
            string skripta = null;
            for (var d = new DirectoryInfo(koren); d != null && skripta == null; d = d.Parent)
            {
                var f = Path.Combine(d.FullName, "Saobracaj.Agent.CimRadionica", "Instaliraj-CimRadionica.ps1");
                if (File.Exists(f)) skripta = File.ReadAllText(f);
            }
            if (skripta == null) Assert.Inconclusive("skripta nije pronađena");
            StringAssert.Contains(skripta, "--podesi-syncfusion");
            StringAssert.Contains(skripta, "-AsSecureString");
            StringAssert.Contains(skripta, "Remove-Item Env:\\CIMPROVERA_SYNCFUSION");
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(skripta, @"[A-Za-z0-9+/]{60,}={0,2}"), "ključ nije u skripti");
            StringAssert.EndsWith(WordUPdf.PutanjeKljuca().Last(), Path.Combine("Saobracaj", "CimProvera", "Syncfusion.dpapi"));
        }

        // ------------------------------------------------------------------------------------------- 5. događaji i SAŽETAK

        [TestMethod]
        public void Naslov_niti_bez_RE_FW_AW_Odg()
        {
            var a = UpisUBazu.NaslovNiti("Announcement Surčin - Alesd (Flyash), No 12");
            foreach (var x in new[] { "RE: Announcement Surčin - Alesd (Flyash), No 12", "FW: RE: AW: Announcement Surčin - Alesd (Flyash), No 12", "Odg: [External mail] Re: Announcement Surčin - Alesd (Flyash), No 12", "WG: Announcement Surčin - Alesd (Flyash), No 12" })
                Assert.AreEqual(a, UpisUBazu.NaslovNiti(x), x);
            Assert.AreNotEqual(a, UpisUBazu.NaslovNiti("Announcement Surčin - Alesd (Flyash), No 13"));
        }

        [TestMethod]
        public void SAZETAK_samoprovera_izvestaja_i_dogadjaji_vezani_nevezani()
        {
            var koren = Path.Combine(Path.GetTempPath(), "CimProveraTest", "sz301_" + Guid.NewGuid().ToString("N"));
            JObject J(int uid, string odluka, bool necitljivo, string posao)
            {
                var rub = new JObject { ["r7"] = new JObject { ["status"] = "popunjeno", ["tekst"] = "1 verfügungsberechtigt", ["pouzdanost"] = 0.92, ["citljivost"] = new JObject { ["ukupno"] = 0.35, ["ocr"] = 0.92, ["pokrivenost"] = 0.35, ["smisao"] = 0.9, ["necitljivo"] = necitljivo } } };
                return new JObject
                {
                    ["poruka"] = new JObject { ["uid"] = uid, ["datum"] = "2026-11-14", ["od"] = "a@b.cz", ["naslov"] = "N" + uid },
                    ["odluka"] = odluka, ["status"] = "Greske",
                    ["provera"] = new JObject { ["gresaka"] = 1, ["upozorenja"] = 0, ["listovi"] = new JArray(new JObject { ["rubrike"] = rub, ["nalazi"] = new JArray(new JObject { ["pravilo"] = "KP7-PLATILAC", ["rubrika"] = "7", ["tezina"] = "error", ["samo_interno"] = false, ["tekst"] = "x" }) }) },
                    ["dogadjaji"] = new JArray(new JObject { ["tip"] = "ETA", ["posao"] = posao, ["kandidati"] = new JArray(posao.Length > 0 ? new string[0] : new[] { "posao 517: nit" }) }),
                };
            }
            Analiza.Zbirno(koren, "v", new List<(int, JObject, string)>
            {
                (1, J(62, "izveštaj pošiljaocu + interna kopija", true, ""), Path.Combine(koren, "001")),
                (2, J(66, "izveštaj pošiljaocu", false, "521"), Path.Combine(koren, "002")),
                (3, J(70, "interni izveštaj (KP tovarni list)", true, "530"), Path.Combine(koren, "003")),
            });
            var sz = File.ReadAllText(Path.Combine(koren, "_Analiza", "v", "SAZETAK.md"), Encoding.UTF8);
            StringAssert.Contains(sz, "izveštaji pošiljaocu: 2; od toga sa nalazom bez pouzdanog dokaza: 1 (001 UID 62: KP7-PLATILAC r7)");
            StringAssert.Contains(sz, "događaji: 3; vezani za posao: 2; nevezani: 1 (od toga sa kandidatima: 1)");
        }
    }
}
