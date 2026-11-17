using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Baza;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>
    /// v2.9.0: upis poslova i teretnica u planiranje.tmp* – jedinični testovi bez baze (šifarnici u memoriji, ID-evi kao u
    /// Perftech_Beograd 14.11.2026): normalizacija i sličnost naziva, stanice („gr.“), NHM, deonica KP za tri relacije, mase, verzije.
    /// </summary>
    [TestClass]
    public class UpisUBazuTests
    {
        private static PravilaSkup pravila;

        [ClassInitialize]
        public static void Init(TestContext _)
        {
            var put = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pravila", "cim_validation_rules.json");
            pravila = PravilaSkup.IzTeksta(File.ReadAllText(put), put);
        }

        internal static Sifarnici Sif()
        {
            var s = new Sifarnici();
            void St(int id, string opis, string drz, bool gr = false, string kod = "") => s.Stanice.Add(new StanicaZapis { Id = id, Opis = opis, Drzava = drz, Granicna = gr, Kod = kod });
            St(380, "Šid", "SRB", kod: "165167"); St(381, "Šid gr.", "SRB", true, "165175");
            St(327, "Subotica", "SRB", kod: "234500"); St(328, "Subotica gr.", "SRB", true, "234997"); St(329, "Subotica fabrika", "SRB");
            St(317, "Sremska Mitrovica", "SRB", kod: "165092"); St(246, "Pančevo Glavna", "SRB", kod: "220012"); St(1464, "Pančevo", "");
            St(404, "Pančevo Varoš", "SRB"); St(383, "Kukujevci", "SRB"); St(405, "Pančevo Vojlovica", "SRB"); St(1088, "Pančevo Vojlovica", "SRB");
            St(407, "Tovarnik", "HR", true); St(445, "Kelebia", "HU", true); St(441, "Zagreb", "HR"); St(9001, "Budapest Ferencváros", "HU");
            St(92, "Vršac", "SRB", kod: "210096"); St(93, "Vršac gr.", "SRB", false, "210997"); St(117, "Dimitrovgrad", "SRB"); St(118, "Dimitrovgrad gr.", "SRB");
            St(968, "Portogruaro Caorle", "IT", kod: "03200-3"); St(1009, "Portogruaro Caorle", "IT", kod: "8303200"); St(621, "Sidenor", "GR");
            s.UpotrebaStanica[1009] = 57; s.UpotrebaStanica[968] = 0; s.UpotrebaStanica[405] = 60; s.UpotrebaStanica[1088] = 3;
            void Pa(int sif, string naziv, string drz, string uic = "", bool prev = false, string doNaziv = "", string kraj = "") =>
                s.Partneri.Add(new PartnerZapis { PaSifra = sif, PaNaziv = naziv, PaDoNaziv = doNaziv, Drzava = drz, Uic = uic, Prevoznik = prev, PaKraj = kraj });
            Pa(1857, "EUROLOGSYSTEM DOO", "SRB", "", true, "Društvo za proizvodnju, trgovinu i inženjering Eurologsystem d.o.o. Bg", "BEOGRAD");
            Pa(3355, "BUDAMAR SOUTH, s.r.o.", "SK", "", true, "BUDAMAR SOUTH, s.r.o.");
            Pa(3143, "BUDAMAR SOUTH D.O.O. BEOGRAD", "SRB", "", true, "BUDAMAR SOUTH D.O.O. BEOGRAD");
            Pa(1580, "BUDAMAR LOGISTICS, a.s.", "SK", "", true, "Budamar Logistics a.s.");
            Pa(1969, "PANŠPED DOO", "SRB", "", true, "PANŠPED DOO"); Pa(2251, "PANŠPED CG", "MNE", "", true, "PANŠPED CG");
            Pa(3440, "GRAMPET RAIL D.O.O.", "HR", "7853", true); Pa(2527, "SŽ - TP", "SI", "2179", true);
            Pa(1647, "HŽ CARGO DOO", "HR", "2178", true); Pa(1597, "RAIL CARGO HUNGARIA", "HU", "2155", true);
            Pa(2233, "Rail Cargo Austria GmbH", "AT"); Pa(3229, "Rail Cargo Logistics - Serbia d.o.o", "SRB", "", true, "Rail Cargo Logistics - Serbia d.o.o.");
            Pa(2246, "Victoriaoil d.o.o.", "SRB", "", true, "VICTORIAOIL DOO ŠID"); Pa(2669, "GRANEXPORT DOO", "SRB", "", true, "GRANEXPORT DOO");
            Pa(2965, "URSA DOO BEOGRAD", "SRB", "", true, "URSA DOO BEOGRAD"); Pa(2823, "EVRO", "BG", "5082", true, "EVROINJENERING LTD");
            // v2.9.1: NHM iz tarife (planiranje.tmpNHM): Kod8, nivo (4 = „XXXX 00 00“, 6 = „XXXX XX 00“, 8 = ostalo), naziv latinicom
            void Nh(int id, string kod8, int nivo, string naziv) => s.Nhm.Add(new NhmZapis { Id = id, Kod8 = kod8, Nivo = nivo, Naziv = naziv });
            Nh(8, "10010000", 4, "Pšenica i suražica"); Nh(9, "10019900", 6, "Pšenica, osim tvrde pšenice");
            Nh(92, "25200000", 4, "Gips; anhidrid gipsa, pečeni gips"); Nh(93, "25201000", 6, "Gips; anhidrid gipsa"); Nh(94, "25202000", 6, "Gips, pečeni");
            Nh(268, "25210000", 4, "Krečnjački kamen"); Nh(6, "99220000", 4, "Železnička kola (vagon) kao prevozno sredstvo, prazan");
            Nh(243, "99221000", 6, "Železnička kola, prazna, kontrola"); Nh(188, "99224000", 6, "Železnička kola, prazna, iznajmljivanje");
            Nh(300, "28330000", 4, "Sulfati; stipse; peroksosulfati"); Nh(301, "28332900", 6, "Sulfati, osim od natrijuma, magnezijuma, aluminijuma");
            Nh(302, "28332920", 8, "Sulfati kadmijuma, hroma, cinka"); Nh(303, "28332930", 8, "Sulfati kobalta, titana");
            Nh(310, "48100000", 4, "Hartija i karton, premazani kaolinom"); Nh(311, "48109200", 6, "Hartija i karton, višeslojni, premazani");
            Nh(320, "99310000", 4, "Kontejneri, veliki, prazni, korišćeni"); Nh(330, "34020000", 4, "Organske površinski aktivne materije");
            Nh(331, "34025000", 6, "Preparati za pranje, pripremljeni za pakovanje"); Nh(340, "31050000", 4, "Mineralna ili hemijska đubriva");
            Nh(350, "21060000", 4, "Prehrambeni proizvodi, na drugom mestu nepomenuti"); Nh(360, "27020000", 4, "Mrki ugalj (lignit)");
            Nh(370, "15120000", 4, "Ulje od semena suncokreta");
            // sinonimi kao u bazi (OP_914_tmp_Sinonimi.sql)
            s.Sinonimi.Add(new SinonimZapis { Tip = "PARTNER", Tekst = "PANSPED", CiljniId = 1969 });
            // v2.9.2: odluka 17.11. (Budamar) – najduži sinonim ima prednost, pravna forma ostaje u sinonimu
            s.Sinonimi.Add(new SinonimZapis { Tip = "PARTNER", Tekst = "BUDAMAR SOUTH SRO", CiljniId = 3355 });
            s.Sinonimi.Add(new SinonimZapis { Tip = "PARTNER", Tekst = "BUDAMAR SOUTH", CiljniId = 3143 });
            s.Sinonimi.Add(new SinonimZapis { Tip = "PARTNER", Tekst = "BUDAMAR LOGISTICS", CiljniId = 1580 });
            s.Sinonimi.Add(new SinonimZapis { Tip = "PARTNER", Tekst = "BUDAMAR", CiljniId = 3143 });
            s.Sinonimi.Add(new SinonimZapis { Tip = "STANICA", Tekst = "PANCEVO", CiljniId = 246 });
            s.Sinonimi.Add(new SinonimZapis { Tip = "STANICA", Tekst = "PANCEVO GLAVNA", CiljniId = 246 });
            return s.Pripremi();
        }

        private static Poklapanje Pk() => new Poklapanje(Sif());

        // ------------------------------------------------------------------------------------------- partneri

        [DataTestMethod]
        [DataRow("Panšped", 1969)]
        [DataRow("PANSPED DOO", 1969)]
        [DataRow("Pansped i.", 1969)]
        [DataRow("EurologSys em doo Beograd", 1857)]
        [DataRow("EurologSystem doo Beograd", 1857)]
        [DataRow("Victoriaoil d.o.o.", 2246)]
        public void Partner_po_nazivu_PaSifra(string tekst, int ocekivano)
        {
            var p = Pk().Partner(tekst);
            Assert.AreEqual(ocekivano, p.Id, p + " | " + p.Kandidati);
        }

        [DataTestMethod]
        [DataRow("Rail Cargo Hungaria")]          // u šifarniku je samo Rail Cargo Austria – ne sme da je izabere
        [DataRow("Rail Cargo Carrier - Serbia")]  // RCL Serbia nije isto što i RCC Serbia
        [DataRow("Evro Trans doo")]
        public void Partner_ispod_praga_NULL_sa_kandidatima(string tekst)
        {
            var pk = new Poklapanje(new Sifarnici { Partneri = Sif().Partneri.Where(z => z.PaSifra != 1597).ToList() }.Pripremi());
            var p = pk.Partner(tekst);
            Assert.IsNull(p.Id, "pogrešan partner: " + p + " | " + p.Kandidati);
        }

        [TestMethod]
        public void Prevoznik_po_UIC_prvo_naziv_za_potvrdu()
        {
            var pk = Pk();
            Assert.AreEqual(3440, pk.Partner("GPRAIL", "7853", null, "prevoznik").Id);   // 7853 → GRAMPET RAIL (odluka 14.11.)
            Assert.AreEqual(2527, pk.Partner("SZ", "2179", null, "prevoznik").Id);
            Assert.AreEqual(1597, pk.Partner("RCH", "2155", null, "prevoznik").Id);
            Assert.IsNull(pk.Partner("XYZ", "9999", null, "prevoznik").Id);
        }

        [TestMethod]
        public void Dva_bliska_kandidata_NULL_i_kandidati()
        {
            // 3355 „BUDAMAR SOUTH, s.r.o.“ i 3143 „BUDAMAR SOUTH D.O.O. BEOGRAD“ imaju isti naziv posle normalizacije (razlika < 0,05).
            // v2.9.2 (3a.3, odluka 17.11.): bez sinonima – pravna forma je znak zemlje (s.r.o. → SK/CZ) → 3355; bez pravne forme i bez
            // zemlje u dokumentu → NULL i kandidati
            var bezSin = Sif(); bezSin.Sinonimi.RemoveAll(x => x.Tekst.StartsWith("BUDAMAR"));
            var pk0 = new Poklapanje(bezSin);
            var p1 = pk0.Partner("Budamar South, s.r.o.");
            Assert.AreEqual(3355, p1.Id, p1 + " | " + p1.Napomena);
            StringAssert.Contains(p1.Napomena, "pravnoj formi");
            var p = pk0.Partner("Budamar South");
            Assert.IsNull(p.Id, p.ToString());
            StringAssert.Contains(p.Kandidati, "3355"); StringAssert.Contains(p.Kandidati, "3143");
            StringAssert.Contains(p.Napomena, "dva bliska kandidata");
            // zemlja iz adrese u dokumentu (bez pravne forme) bira jednog
            Assert.AreEqual(3143, pk0.Partner("Budamar South", null, new[] { "SRB" }).Id);
            // izuzetak: naziv iz tmpSinonimi
            var s = Sif(); s.Sinonimi.RemoveAll(x => x.Tekst.StartsWith("BUDAMAR")); s.Sinonimi.Add(new SinonimZapis { Tip = "PARTNER", Tekst = "BUDAMAR SOUTH", CiljniId = 3355 });
            Assert.AreEqual(3355, new Poklapanje(s).Partner("Budamar South, s.r.o.").Id);
            // Panšped: sinonim → 1969 (odluka 14.11.)
            Assert.AreEqual(1969, Pk().Partner("Panšped").Id);
            StringAssert.Contains(Pk().Partner("Panšped").Napomena, "sinonim");
        }

        [TestMethod]
        public void Platilac_samo_iz_r7_firma_iz_dokumenta()
        {
            var c = List("Herne", "80 12345-6", "Novi Sad", "72 11111-1", "13.3 2143 GYSEV, 3212 KP", new[] { "2143 GYSEV HEGYESHALOM-KELEBIA", "3212 KP SUBOTICA-NOVI SAD" });
            c.R7 = new ListaRubrika { Status = StatusRubrike.Popunjeno, Tekst = "Frachtzahler strecke DE+CZ+SK+HU+SRB ist Budamar Logistics a.s" };
            Assert.AreEqual(1580, PosaoIzDokumenata.Napravi(c, null, null, Pk(), pravila.Kp).V["PlatilacID"], "firma iz r7 (ne iz najave)");
            // firma koje nema u Partnerji: NULL i kandidati
            c.R7 = new ListaRubrika { Status = StatusRubrike.Popunjeno, Tekst = "Frachtzahler für SRB ist Nepoznata Spedicija Novak GmbH" };
            var p = PosaoIzDokumenata.Napravi(c, null, null, Pk(), pravila.Kp);
            Assert.IsNull(p.V["PlatilacID"], "nepoznata firma: " + p.V["PlatilacID"] + " | " + p.Poreklo.First(x => x.Kolona == "PlatilacID").Kandidati);
            StringAssert.Contains(p.Poreklo.First(x => x.Kolona == "PlatilacID").Tekst, "Nepoznata Spedicija Novak");
            // r15 se ne koristi (samo r7)
            c.R7 = new ListaRubrika { Status = StatusRubrike.Popunjeno, Tekst = "7.12 Isporuka bez zadrške" };   // r7 bez platioca
            c.R15 = new TekstRubrika { Status = StatusRubrike.Popunjeno, Tekst = "FREIGHT PAYER FOR KP 3212 – EurologSystem doo Beograd" };
            var p3 = PosaoIzDokumenata.Napravi(c, null, null, Pk(), pravila.Kp);
            Assert.IsNull(p3.V["PlatilacID"], "r15: " + p3.V["PlatilacID"] + " | " + p3.Poreklo.First(x => x.Kolona == "PlatilacID").Tekst);
        }

        [TestMethod]
        public void Stanica2_utovarno_mesto()
        {
            var c = List("ŠID (U M KUKUJEVCI) j", "", "San Giovanni", "", "13.3 3212 KP, 7053 GPR", new[] { "3212 KP ŠID-ŠID", "7853 GPR ŠID-DOBOVA" });
            var p = PosaoIzDokumenata.Napravi(c, null, null, Pk(), pravila.Kp);
            Assert.AreEqual(380, p.V["OtpravnaID"]); Assert.AreEqual(380, p.V["Stanica1ID"]);
            Assert.AreEqual(383, p.V["Stanica2ID"], "u.m. Kukujevci");
            StringAssert.Contains(p.Poreklo.First(x => x.Kolona == "Stanica2ID").Tekst, "KUKUJEVCI");
            // iz naslova maila
            var c2 = List("Šid", "", "San Giovanni", "", "13.3 3212 KP", new[] { "3212 KP ŠID-ŠID" });
            Assert.AreEqual(383, PosaoIzDokumenata.Napravi(c2, null, null, Pk(), pravila.Kp, "CIT23 za voz 258 Šid (u.m. Kukujevci) - San Giovanni In Persiceto").V["Stanica2ID"]);
            // bez utovarnog mesta: NULL
            Assert.IsNull(PosaoIzDokumenata.Napravi(c2, null, null, Pk(), pravila.Kp, "CIM").V["Stanica2ID"]);
            // Pančevo → 246 (sinonim, i kao utovarno mesto jednako otpravnoj nije Stanica2)
            Assert.AreEqual(246, Pk().Stanica("PANČEVO").Id);
            Assert.AreEqual(404, PosaoIzDokumenata.Stanica2(Pk(), 246, ("spisak", "PANČEVO GLAVNA (U.M. PANČEVO VAROŠ) 72 21001-3")).Id);
            // OCR šum posle naziva („(U M PANČEVO jr“): Pančevo (1464) se preusmerava sinonimom na 246 = otpravna → nije Stanica2
            Assert.IsNull(PosaoIzDokumenata.Stanica2(Pk(), 246, ("r16", "P Пре зимањен PANČEVO GLAVNA (U M PANČEVO jr")));
            Assert.AreEqual(246, Pk().Stanica("PANČEVO jr").Id);
        }

        [TestMethod]
        public void Duzina_spisak_istorija_ili_NULL()
        {
            var s = new SpisakKola();
            s.Kola.Add(new KolaNaSpisku { Broj = "338393382019", DuzinaM = 14.8, DuzinaIzvor = "spisak", TaraKg = 19990, NetoKg = 59757 });
            s.Kola.Add(new KolaNaSpisku { Broj = "338793320940", DuzinaM = 15.6, DuzinaIzvor = "istorija", TaraKg = 19080, NetoKg = 60645 });
            var p = PosaoIzDokumenata.IzSpiska(s, Pk(), "100100", "");
            Assert.AreEqual(30.4m, p.V["Duzina1"]);
            Assert.AreEqual("istorija", p.Poreklo.First(x => x.Kolona == "Duzina1").Rubrika);
            s.Kola.Add(new KolaNaSpisku { Broj = "338706746991", TaraKg = 20400, NetoKg = 59553 });   // bez dužine i u istoriji
            p = PosaoIzDokumenata.IzSpiska(s, Pk(), "100100", "");
            Assert.IsNull(p.V["Duzina1"], "dužina nije poznata za sva kola → NULL");
            StringAssert.Contains(p.Poreklo.First(x => x.Kolona == "Duzina1").Tekst, "1 bez dužine");
            // tovarni list nema dužinu → NULL (ne 0)
            Assert.IsNull(PosaoIzDokumenata.Napravi(List("Šid", "", "Zagreb", "", "", new string[0]), null, null, Pk(), pravila.Kp).V["Duzina1"]);
        }

        [TestMethod]
        public void Sinonim_ima_prednost()
        {
            var s = Sif(); s.Sinonimi.Add(new SinonimZapis { Tip = "PARTNER", Tekst = Slicnost.Partner("Pansped Beograd ekspedicija"), CiljniId = 1969 });
            Assert.AreEqual(1969, new Poklapanje(s).Partner("Pansped Beograd ekspedicija").Id);
        }

        [TestMethod]
        public void Normalizacija_naziva()
        {
            Assert.AreEqual("PANSPED", Slicnost.Partner("Panšped d.o.o."));
            Assert.AreEqual("BUDAMAR SOUTH", Slicnost.Partner("Budamar South, s.r.o."));
            Assert.AreEqual("EUROLOGSYS EM", Slicnost.Partner("EurologSys em doo Beograd"));
            Assert.AreEqual("PANCEVO GLAVNA", Slicnost.Stanica("PANČEVO GLAVNA (U.M. PANČEVO VAROŠ)"));
            Assert.AreEqual("SID", Slicnost.Stanica("Šid (u.m. Kukujevci)"));
            Assert.AreEqual("DJURDJEVO", Slicnost.Osnovno("Đurđevo"));
        }

        // ------------------------------------------------------------------------------------------- stanice

        [TestMethod]
        public void Stanice_po_nazivu_i_gr()
        {
            var pk = Pk();
            Assert.AreEqual(380, pk.Stanica("Šid").Id, "uputna Šid → Šid");
            Assert.AreEqual(380, pk.Stanica("SID").Id);
            Assert.AreEqual(381, pk.Stanica("Šid", granicna: true).Id, "Stanica1 Šid → Šid gr.");
            Assert.AreEqual(328, pk.Stanica("Subotica", granicna: true).Id, "Stanica3 Subotica → Subotica gr.");
            Assert.AreEqual(93, pk.Stanica("Vršac", granicna: true).Id, "Vršac gr. ima Granicna = 0 – po nazivu");
            Assert.AreEqual(118, pk.Stanica("Dimitrovgrad", granicna: true).Id);
            Assert.AreEqual(246, pk.Stanica("Pančevo Glavna", granicna: true).Id, "bez „gr.“ varijante → stanica koja postoji");
            Assert.AreEqual(246, pk.Stanica("PANČEVO GLAVNA (U.M. PANČEVO VAROŠ)", "72 21001-3").Id);
            Assert.AreEqual(445, pk.Stanica("Kelebia", null, "HU").Id);
        }

        [TestMethod]
        public void Stanice_duplikati_najcesce_korisceni()
        {
            var p = Pk().Stanica("PORTOGRUARO CAORLE", "83 03200-3");
            Assert.AreEqual(1009, p.Id, p.Napomena);
            StringAssert.Contains(p.Napomena, "968");
            Assert.AreEqual(405, Pk().Stanica("Pančevo Vojlovica").Id);
        }

        [TestMethod]
        public void Stanica_samo_u_Srbiji_za_predaju()
        {
            // OCR „ÉID/TOVARNK“: Tovarnik (HR) nije tačka predaje KP
            var p = Pk().Stanica("ÉID/TOVARNK", granicna: true, samoDrzave: new[] { "SRB" });
            Assert.IsNull(p.Id, p.ToString());
            Assert.AreEqual(381, Pk().Stanica("ŠID/TOVARNIK", granicna: true, samoDrzave: new[] { "SRB" }).Id);
        }

        [TestMethod]
        public void Kod_stanice_razni_zapisi()
        {
            Assert.IsTrue(Poklapanje.KodPoklapa("83 03200-3", "032003"));
            Assert.IsTrue(Poklapanje.KodPoklapa("83032003", "8303200"));
            Assert.IsTrue(Poklapanje.KodPoklapa("72 16509-2", "165092"));
            Assert.IsFalse(Poklapanje.KodPoklapa("72 21001-3", "220012"));
        }

        // ------------------------------------------------------------------------------------------- NHM

        [TestMethod]
        public void Nhm_sifre()
        {
            // v2.9.1 (3.1): 8 cifara → tačan Kod8; Kod6 sa tačno jednim redom → taj red; nijedan ili više → grupa Kod4; bez Kod4 → NULL
            var pk = Pk();
            Assert.AreEqual(93, pk.Nhm("252010").Id, "252010 → 2520 10 00 (jedan red Kod6)");
            Assert.AreEqual(6, pk.Nhm("992200").Id, "992200 → grupa 9922 (Kod6 nema reda)");
            Assert.AreEqual(6, pk.Nhm("9922").Id, "9922 bez podgrupe → grupa 9922 (odluka 3)");
            Assert.AreEqual(243, pk.Nhm("992210").Id);
            Assert.AreEqual(8, pk.Nhm("100100").Id, "100100 → grupa 1001");
            Assert.AreEqual(8, pk.Nhm("1001.00").Id);
            var p = pk.Nhm("992290");
            Assert.AreEqual(6, p.Id); StringAssert.Contains(p.Napomena, "grupa 9922");
            Assert.AreEqual(300, pk.Nhm("283329").Id, "2833 29: tri reda sa istim Kod6 → grupa 2833");
            Assert.AreEqual(302, pk.Nhm("28332920").Id, "tačan Kod8");
            Assert.AreEqual(311, pk.Nhm("48109200").Id, "CER „48109200 – Papír“ → tačan Kod8");
            Assert.AreEqual(320, pk.Nhm("993100").Id, "„993100 TANK KONTEYNER“ → grupa 9931");
            Assert.AreEqual(331, pk.Nhm("340250").Id, "340250 → 3402 50 00");
            var n = pk.Nhm("123456");
            Assert.IsNull(n.Id); StringAssert.Contains(n.Napomena, "nije u tarifi");
        }

        [TestMethod]
        public void Nhm_po_nazivu_i_iz_teksta()
        {
            var pk = Pk();
            Assert.AreEqual(360, pk.NhmPoNazivu("Mrki ugalj").Id, "jedan siguran kandidat");
            Assert.AreEqual(92, pk.NhmPoNazivu("Gips").Id, "više redova iste grupe → red grupe Kod4");
            Assert.IsNull(pk.NhmPoNazivu("GYPSUM").Id, "strani naziv → NULL (tmpSinonimi tip NHM)");
            CollectionAssert.AreEqual(new[] { "993100" }, PosaoIzDokumenata.NhmIzTeksta("993100 TANK KONTEYNER").ToList());
            CollectionAssert.AreEqual(new[] { "48109200" }, PosaoIzDokumenata.NhmIzTeksta("48109200 – Papír és karton").ToList());
        }

        // ------------------------------------------------------------------------------------------- deonica KP

        private static CimList List(string otp, string otpSifra, string up, string upSifra, string r13, string[] r57, string r1 = "Pošiljalac doo", string r4 = "Primalac doo")
        {
            var c = new CimList();
            c.R16 = new Preuzimanje { Status = StatusRubrike.Popunjeno, Mesto = otp, SifraStanice = otpSifra, Tekst = otp };
            c.R10 = new MestoIsporuke { Status = StatusRubrike.Popunjeno, Stanica = up, SifraStanice = upSifra, Tekst = up };
            c.R13 = new ListaRubrika { Status = StatusRubrike.Popunjeno, Tekst = r13 };
            c.R57 = new PrevozniciRubrika { Status = StatusRubrike.Popunjeno, Tekst = string.Join("\n", r57) };
            foreach (var x in r57) { var d = x.Split(new[] { ' ' }, 3); c.R57.Prevoznici.Add(new Model.Prevoznik { Sifra = d[0], Naziv = d[1], Deonica = d.Length > 2 ? d[2] : "" }); }
            c.R1 = new Model.Strana { Status = StatusRubrike.Popunjeno, Naziv = r1 };
            c.R4 = new Model.Strana { Status = StatusRubrike.Popunjeno, Naziv = r4 };
            c.R8 = new TekstRubrika { Status = StatusRubrike.Popunjeno, Tekst = "Train no: 999" };
            c.R62 = new Identifikacija { Zemlja = "72", Stanica = "16509", Preduzece = "3212", Broj = "16150-0", Status = StatusRubrike.Popunjeno };
            return c;
        }

        [TestMethod]
        public void Relacija_ulaz_iz_Hrvatske_izlaz_u_Madjarsku()
        {
            var c = List("Zagreb", "78 72480-0", "Budapest Ferencváros", "55 12345-6", "13.3 2178 HZC, 3212 KP, 2155 RCH",
                new[] { "2178 HZC ZAGREB-TOVARNIK", "3212 KP ŠID-SUBOTICA", "2155 RCH KELEBIA-BUDAPEST" });
            var p = PosaoIzDokumenata.Napravi(c, null, null, Pk(), pravila.Kp);
            Assert.AreEqual(1647, p.V["PreuzimaOdID"], "HŽ Cargo");
            Assert.AreEqual(1597, p.V["PredajeOperateruID"], "RCH");
            Assert.AreEqual(381, p.V["Stanica1ID"], "Šid gr.");
            Assert.AreEqual(328, p.V["Stanica3ID"], "Subotica gr.");
            Assert.AreEqual(441, p.V["OtpravnaID"]);
        }

        [TestMethod]
        public void Relacija_polazak_iz_Srbije_izlaz_u_Hrvatsku()
        {
            var c = List("Sremska Mitrovica", "72 16509-2", "Zagreb", "78 72480-0", "13.3 3212 KP, 2178 HZC",
                new[] { "3212 KP SREMSKA MITROVICA-ŠID", "2178 HZC TOVARNIK-ZAGREB" }, r1: "Victoriaoil d.o.o.");
            var p = PosaoIzDokumenata.Napravi(c, null, null, Pk(), pravila.Kp);
            Assert.IsNull(p.V["PreuzimaOdID"], "KP je prvi u lancu prevoznika → NULL (odluka 14.11.)");
            StringAssert.Contains(p.Poreklo.First(x => x.Kolona == "PreuzimaOdID").Tekst, "KP je prvi u lancu");
            Assert.AreEqual(317, p.V["Stanica1ID"], "otpravna");
            Assert.AreEqual(317, p.V["OtpravnaID"]);
            Assert.AreEqual(381, p.V["Stanica3ID"], "Šid gr.");
            Assert.AreEqual(1647, p.V["PredajeOperateruID"]);
        }

        [TestMethod]
        public void Relacija_ulaz_iz_Madjarske_kraj_u_Srbiji()
        {
            var c = List("Budapest Ferencváros", "55 12345-6", "Pančevo Varoš", "72 22002-0", "13.3 2155 RCH, 3212 KP",
                new[] { "2155 RCH BUDAPEST-KELEBIA", "3212 KP SUBOTICA-PANČEVO VAROŠ" }, r4: "GRANEXPORT D.O.O.");
            var p = PosaoIzDokumenata.Napravi(c, null, null, Pk(), pravila.Kp);
            Assert.AreEqual(1597, p.V["PreuzimaOdID"]);
            Assert.AreEqual(328, p.V["Stanica1ID"], "Subotica gr.");
            Assert.AreEqual(404, p.V["Stanica3ID"], "uputna");
            Assert.IsNull(p.V["PredajeOperateruID"], "KP je poslednji u lancu prevoznika → NULL (odluka 14.11.)");
        }

        [TestMethod]
        public void Voz256_iz_procitanih_rubrika()
        {
            // rubrike kako ih OCR čita na skenu 20261114081659.pdf (v2.8.4, posle ciljanog OCR-a r7)
            var c = new CimList();
            c.R1 = new Model.Strana { Status = StatusRubrike.Popunjeno, Naziv = "CHS SERBIA DOO" };
            c.R7 = new ListaRubrika { Status = StatusRubrike.Popunjeno, Tekst = "7 16 KP, GPR, SŽ-TP is EurologSys em doo Beograd\njana za vrši Paniped u aperalions" };
            c.R8 = new TekstRubrika { Status = StatusRubrike.Popunjeno, Tekst = "Train no: 256F" };
            c.R10 = new MestoIsporuke { Status = StatusRubrike.Popunjeno, Tekst = "}\nPORTOGRUARO CAORLE\nтаница – - ITALY" };
            c.R12 = new TekstRubrika { Status = StatusRubrike.Popunjeno, Tekst = "83032003" };
            c.R13 = new ListaRubrika { Status = StatusRubrike.Popunjeno, Tekst = "13.4 VIA ÉID/TOVARNK DOBOVAISAVSKI MAROF-SEZANAMILL A OPICINA\n13.3 3212 KP, 7853 GPRAIL, 2179 SZ, 2183 MIR\nZE 4011.00, MR INMP0021" };
            c.R16 = new Preuzimanje { Status = StatusRubrike.Popunjeno, Mesto = "P Пре зимањен PANČEVO GLAVNA (U M. PANČEVO", Tekst = "P Пре зимањен\n- PANČEVO GLAVNA (U M. PANČEVO" };
            c.R18 = new ListaRubrika { Status = StatusRubrike.Popunjeno, Tekst = "6\n6p. –\nVidi spisak kola CIT 23" };
            c.R21 = new NaznacenjeRobe { Status = StatusRubrike.Popunjeno, Tekst = "PŠENICA za ljudsku upotrebu // Wheat human consumption\nTara: 562.670\nNetio: 1.671.863\nBrute: 2.234.533" };
            c.R24 = new ListaRubrika { Status = StatusRubrike.Popunjeno, Stavke = { new SifraTekst { Sifra = "100100" } } };
            c.R25 = new Masa { Status = StatusRubrike.Popunjeno, Tekst = "Tara 562 670 Netta 1 671 863 bruto 2 234533" };
            c.R57 = new PrevozniciRubrika { Status = StatusRubrike.Popunjeno, Tekst = "3212 KP PANČEVO GLAVNA UM PANČEVO VAROË)-ÉID/TOVARNK\n7853 ŠID/TOVARNK-DOBOVA/SAVSKI MAROF\n24179 SZ DOBOVA/SAVSKI MAROF-SEZANAVILL A OPICINA\n2183 MIR" };
            c.R57.Prevoznici.Add(new Model.Prevoznik { Sifra = "3212", Deonica = "PANČEVO GLAVNA UM PANČEVO VAROË)-ÉID/TOVARNK" });
            c.R57.Prevoznici.Add(new Model.Prevoznik { Sifra = "7853" });
            c.R57.Prevoznici.Add(new Model.Prevoznik { Sifra = "2183" });
            var s = Sif(); s.Partneri.Add(new PartnerZapis { PaSifra = 3000, PaNaziv = "CHS SERBIA DOO", Drzava = "SRB", Posiljalac = true }); s.Pripremi();
            var p = PosaoIzDokumenata.Napravi(c, null, null, new Poklapanje(s), pravila.Kp);
            Assert.AreEqual(1857, p.V["PlatilacID"], "EurologSystem doo Beograd");
            Assert.AreEqual(246, p.V["OtpravnaID"], "Pančevo Glavna");
            Assert.AreEqual(1009, p.V["UputnaID"], "Portogruaro Caorle (najčešće korišćen duplikat)");
            Assert.IsNull(p.V["PreuzimaOdID"], "voz 256: KP je prvi u lancu (3212 KP, 7853 GPRAIL…) → NULL");
            Assert.IsNull(p.V["Stanica2ID"], "r16 „(U M. PANČEVO“ je odsečeno – Pančevo = otpravna (sinonim), nije utovarno mesto");
            Assert.AreEqual(3440, p.V["PredajeOperateruID"], "7853 GP Rail");
            Assert.AreEqual(246, p.V["Stanica1ID"]);
            Assert.AreEqual(381, p.V["Stanica3ID"], "Šid gr.");
            Assert.AreEqual(2234.533m, p.V["Bruto1"]);
            Assert.AreEqual(1671.863m, p.V["Neto1"]);
            Assert.AreEqual(8, p.V["NHM1"], "NHM 1001");
            Assert.AreEqual("256", p.BrojVoza);
        }

        // ------------------------------------------------------------------------------------------- mase, spisak, verzije

        [TestMethod]
        public void Mase_nula_kada_nisu_navedene()
        {
            var c = List("Šid", "", "Zagreb", "", "", new string[0]);
            var p = PosaoIzDokumenata.Napravi(c, null, null, Pk(), pravila.Kp);
            Assert.AreEqual(0m, p.V["Bruto1"]); Assert.AreEqual(0m, p.V["Neto1"]); Assert.IsNull(p.V["Duzina1"]); Assert.AreEqual(0, p.V["BrojKola1"]);
            // v2.9.1 (5.1): samo tara – Tara1 = tara, bruto i neto 0 (tara nikad ne ide u bruto)
            c.R25 = new Masa { Status = StatusRubrike.Popunjeno, Tekst = "Tara: 650.000 kg" };
            p = PosaoIzDokumenata.Napravi(c, null, null, Pk(), pravila.Kp);
            Assert.AreEqual(650m, p.V["Tara1"]); Assert.AreEqual(0m, p.V["Bruto1"]); Assert.AreEqual(0m, p.V["Neto1"]);
        }

        [TestMethod]
        public void Mase_OCR_bez_razdvajanja_hiljada()
        {
            // pravi OCR r25 voza 256 (20261114081659.pdf): „Bruto: 2.234533“
            Assert.AreEqual(2234533, PosaoIzDokumenata.Kg("2.234533"));
            Assert.AreEqual(562670, PosaoIzDokumenata.Kg("562.670"));
            Assert.AreEqual(1627580, PosaoIzDokumenata.Kg("1627,580 t"));
            Assert.AreEqual(2090520, Math.Round(PosaoIzDokumenata.Kg("2.090,52 to").Value), "GIPS r25: „Btto: 2.090,52 to“");
            var g = List("Šid", "", "Zagreb", "", "", new string[0]);
            g.R25 = new Masa { Status = StatusRubrike.Popunjeno, Tekst = "Ntto:1.510,26 to\nTara: 580,26 to\nBtto: 2.090,52 to" };
            var pg = PosaoIzDokumenata.Napravi(g, null, null, Pk(), pravila.Kp);
            Assert.AreEqual(2090.52m, pg.V["Bruto1"]); Assert.AreEqual(1510.26m, pg.V["Neto1"]);
            var c = List("Pančevo Glavna", "", "Portogruaro Caorle", "", "", new string[0]);
            c.R25 = new Masa { Status = StatusRubrike.Popunjeno, Tekst = "Tara: 562.670\nNetta: 1.671.863\nBruto: 2.234" };
            var p = PosaoIzDokumenata.Napravi(c, null, null, Pk(), pravila.Kp);
            Assert.AreEqual(2234.533m, p.V["Bruto1"], "bruto = tara + neto kada je bruto pročitan u pogrešnoj jedinici");
        }

        [TestMethod]
        public void Spisak_kola_zbirovi_i_serija()
        {
            var s = new SpisakKola { NhmZaglavlje = "100100" };
            s.Kola.Add(new KolaNaSpisku { Broj = "338393382019", TaraKg = 19990, NetoKg = 59757 });
            s.Kola.Add(new KolaNaSpisku { Broj = "338793320940", TaraKg = 19080, NetoKg = 60645 });
            s.Kola.Add(new KolaNaSpisku { Broj = "338706746991", TaraKg = 20400, NetoKg = 59553 });
            var p = PosaoIzDokumenata.IzSpiska(s, Pk(), "", "");
            Assert.AreEqual(3, p.V["BrojKola1"]);
            Assert.AreEqual(239.425m, p.V["Bruto1"]);
            Assert.AreEqual(179.955m, p.V["Neto1"]);
            Assert.IsNull(p.V["Duzina1"]);
            Assert.AreEqual("Ua", p.V["SerijaKola1"], "93xx → U (2 od 3 kola)");
            Assert.AreEqual(8, p.V["NHM1"]);
        }

        [TestMethod]
        public void Broj_kola_UIC()
        {
            Assert.AreEqual("33839338201-9", BrojKolaUic.ZaTeretnicu("3383 9338 2019"));
            Assert.IsTrue(BrojKolaUic.KontrolnaIspravna("338393382019"));
            Assert.AreEqual("U", BrojKolaUic.Slovo("338793320940")); Assert.AreEqual(4, BrojKolaUic.Osovine("338793320940"));
            Assert.AreEqual("T", BrojKolaUic.Slovo("338706746991"));
            Assert.AreEqual("S", BrojKolaUic.Slovo("338548502107")); Assert.AreEqual(6, BrojKolaUic.Osovine("338548502107"));
            Assert.AreEqual("", BrojKolaUic.Slovo("338533123456"), "33 – K/O/R nije pouzdano");
            Assert.AreEqual("Sg", BrojKolaUic.DvoslovnaIzSerije("Sggrss"));
            Assert.AreEqual("", BrojKolaUic.NajcescaOznaka(new[] { ("338793320940", ""), ("338533123456", ""), ("338533123457", "") }), "manje od polovine – NULL");
        }

        [TestMethod]
        public void Nova_verzija_menja_samo_promenjena_polja()
        {
            var stari = new Dictionary<string, object> { ["PlatilacID"] = 1857, ["Bruto1"] = 2234.533m, ["BrojKola1"] = 28, ["ImaGreske"] = true, ["RIDBroj1"] = null };
            var novi = new Dictionary<string, object> { ["PlatilacID"] = 1857, ["Bruto1"] = 2234.5330m, ["BrojKola1"] = 27, ["ImaGreske"] = false, ["RIDBroj1"] = null };
            CollectionAssert.AreEqual(new[] { "BrojKola1", "ImaGreske" }, UpisUBazu.Uporedi(stari, novi, novi.Keys).ToArray());
        }

        [TestMethod]
        public void Napomena_najnoviji_blok_gore_i_najvise_2000()
        {
            var m = new MailZaUpis { Posiljalac = "a@b.rs", Naslov = "CIM", Primljeno = new DateTime(2026, 11, 14, 8, 20, 0), Telo = "Poštovani,\nu prilogu CIM.\nS poštovanjem,\nMarko\n\nFrom: x@y\nstari citat" };
            var b = UpisUBazu.BlokNapomene(m);
            StringAssert.StartsWith(b, "14.11.2026 08:20 a@b.rs: CIM");
            Assert.IsFalse(b.Contains("stari citat"));
            var n = UpisUBazu.SpojiNapomenu("NOVO", new string('x', 2500));
            Assert.AreEqual(2000, n.Length); StringAssert.StartsWith(n, "NOVO"); StringAssert.EndsWith(n, "…");
        }

        [TestMethod]
        public void Spisak_iz_Excela_vrednosti_po_kolima()
        {
            var redovi = new List<string[]>
            {
                new[] { "CIT 23" }, new[] { "Train nr. 256" }, new[] { "NHM", "1001.00" },
                new[] { "Otpravna stanica", "PANČEVO GLAVNA (U.M. PANČEVO VAROŠ) 72 21001-3" }, new[] { "Uputna stanica", "PORTOGRUARO CAORLE 83 03200-3" },
                new[] { "Rbr", "Broj vagona", "BrOs", "Vlasništvo", "Tara [kg]", "Neto [kg]", "Bruto [kg]", "Plombe", "NHM" },
                new[] { "1", "3383 9338 2019", "4", "P", "19990", "59757", "79747", "017472", "100100" },
                new[] { "2", "3383 9338 3413", "4", "P", "19800", "59962", "79762", "017495", "100100" },
            };
            var d = ExcelCitac.AnalizaRedova(redovi, pravila.Kp, "CIT-23.xlsx", "excel", "Excel").First(x => x.Tip == TipDokumenta.SpisakKola);
            var s = d.Spisak[0];
            Assert.AreEqual("256", s.BrojVoza);
            Assert.AreEqual("100100", s.NhmZaglavlje);
            StringAssert.StartsWith(s.OtpravnaTekst, "PANČEVO GLAVNA");
            Assert.AreEqual(19990, s.Kola[0].TaraKg); Assert.AreEqual(59757, s.Kola[0].NetoKg); Assert.AreEqual(79747, s.Kola[0].BrutoKg);
            Assert.AreEqual(4, s.Kola[0].Osovine); Assert.AreEqual(2, s.Kola[1].Rb);
        }

        [TestMethod]
        public void Spisak_OCR_pomerene_kolone_se_odbacuju()
        {
            // sken CIT 23 voza 256 („Customs documents … .pdf“): u koloni „Tara“ su neto mase, u „Neto“ broj ispod 1 t
            var redovi = new List<string[]>
            {
                new[] { "Rbr", "Broj vagona", "BrOs", "Tara", "Neto" },
                new[] { "1", "3383 9338 2019", "4", "59267", "59" },
                new[] { "2", "3383 9338 3413", "4", "59300", "60" },
                new[] { "3", "3383 9338 3504", "4", "59945", "1250" },   // kolona nije u tonama (ima vrednost ≥ 200) – 59 i 60 su kg
            };
            var s = ExcelCitac.AnalizaRedova(redovi, pravila.Kp, "sken.pdf", "ocr", "OCR").First(x => x.Tip == TipDokumenta.SpisakKola).Spisak[0];
            Assert.IsNull(s.Kola[0].TaraKg, "tara 59 t nije tara kola");
            Assert.IsNull(s.Kola[0].NetoKg, "neto 59 kg nije neto");
            Assert.AreEqual(4, s.Kola[0].Osovine);
        }

        [TestMethod]
        public void Spisak_bez_tare_ne_menja_bruto()
        {
            var s = new SpisakKola { NhmZaglavlje = "252010" };
            s.Kola.Add(new KolaNaSpisku { Broj = "338753123456", NetoKg = 44000 });
            s.Kola.Add(new KolaNaSpisku { Broj = "338753123457", NetoKg = 45000 });
            var p = PosaoIzDokumenata.IzSpiska(s, Pk(), "", "");
            Assert.IsFalse(p.V.ContainsKey("Bruto1"), "samo neto na spisku – bruto ostaje iz tovarnog lista");
            Assert.AreEqual(89m, p.V["Neto1"]);
            var bez = new SpisakKola(); bez.Kola.Add(new KolaNaSpisku { Broj = "338753123456" });
            var p2 = PosaoIzDokumenata.IzSpiska(bez, Pk(), "", "");
            Assert.IsFalse(p2.V.ContainsKey("Bruto1") || p2.V.ContainsKey("Neto1"), "spisak bez masa ne briše mase tovarnog lista");
        }

        [TestMethod]
        public void Napomena_broj_kola_u_izvestaju()
        {
            var c = List("Pančevo Glavna", "", "Portogruaro Caorle", "", "", new string[0]);
            c.R21 = new NaznacenjeRobe { Status = StatusRubrike.Popunjeno, Tekst = "Pšenica, 26 vagona" };
            var r = new RezultatProvere();
            r.Listovi.Add(new RezultatLista { Cim = c, Oznaka = "CIM list 1" });
            var s = new SpisakKola();
            for (int i = 0; i < 28; i++) s.Kola.Add(new KolaNaSpisku { Broj = (338793320000 + i).ToString() });
            r.Dokumenti.Add(new Dokument { Tip = TipDokumenta.SpisakKola, Spisak = { s } });
            Obrada.NapomenaBrojaKola(r);
            var n = r.Listovi[0].Nalazi.Single();
            Assert.AreEqual("Broj kola u r21 (26) ne odgovara spisku kola (28).", n.Tekst);
            Assert.IsTrue(n.SamoInterno); Assert.AreEqual(Tezina.Info, n.Tezina);
        }

        [TestMethod]
        public void Konekcija_bez_lozinke_u_otvorenom_tekstu()
        {
            // connection string se čuva samo DPAPI zaštićen (Konekcija.dpapi) – otvoreni tekst se ne koristi
            Assert.IsNull(AgentConfig.KonekcijaIz(null, "Data Source=.;Initial Catalog=X;User ID=a;Password=b"));
            Assert.IsNull(AgentConfig.KonekcijaIz(null, "Data Source=.\\SQLEXPRESS2008K;Initial Catalog=X;Integrated Security=True"));
            Assert.IsTrue(AgentConfig.OtvorenaKonekcija);
            Assert.AreEqual("iz-dpapi", AgentConfig.KonekcijaIz("iz-dpapi", null));
            Assert.IsFalse(AgentConfig.Ucitaj(new System.Collections.Specialized.NameValueCollection()).UpisUBazu, "podrazumevano isključeno (jedinični testovi)");
        }

        // =========================================================================================== v2.9.1 (zadatak 16.11.)

        private static Poklapanje Pk291()
        {
            var s = Sif();
            s.Partneri.Add(new PartnerZapis { PaSifra = 1722, PaNaziv = "CENTRAL RAILWAYS A.S. CRW", PaDoNaziv = "CENTRAL RAILWAYS A.S. CRW", Drzava = "SK", Prevoznik = true });
            s.Partneri.Add(new PartnerZapis { PaSifra = 1692, PaNaziv = "CER Hungary", PaDoNaziv = "CER Hungary", Drzava = "HU", Prevoznik = true });
            s.Partneri.Add(new PartnerZapis { PaSifra = 1641, PaNaziv = "CER Slovakia a.s.", PaDoNaziv = "CER Slovakia a.s.", Drzava = "HU", Uic = "3085", Prevoznik = false });
            s.Partneri.Add(new PartnerZapis { PaSifra = 3531, PaNaziv = "FORTRACK RAIL KFT", PaDoNaziv = "FORTRACK RAIL KFT", Drzava = "HU", Uic = "5059", Prevoznik = true });
            s.Stanice.Add(new StanicaZapis { Id = 273, Opis = "Preševo gr.", Drzava = "SRB" });
            s.Stanice.Add(new StanicaZapis { Id = 1385, Opis = "Kwidzyn", Drzava = "PL", Kod = "51023119" });
            foreach (var (t, id) in new[] { ("KELEBIA", 328), ("0711", 328), ("HU", 328), ("TOVARNIK", 381), ("HR", 381), ("STAMORA", 93), ("RO", 93), ("KAPIKULE", 118), ("BG", 118), ("TABANOVCI", 273), ("MK", 273) })
                s.Sinonimi.Add(new SinonimZapis { Tip = "GRANICA", Tekst = t, CiljniId = id });
            return new Poklapanje(s.Pripremi());
        }

        [DataTestMethod]
        [DataRow("il 1 42-0003-26", "42-0003-26")]
        [DataRow("©j 520 o 0", null)]
        [DataRow("Bj 5 2 0 1 0 0", null)]
        [DataRow("sie –", null)]
        [DataRow("LDC8-HAMMNOV1", "LDC8-HAMMNOV1")]
        [DataRow("SAD HER 79", "SAD HER 79")]
        [DataRow("Budamar Coal 17", "Budamar Coal 17")]
        public void V291_Referenca_i_ponuda_samo_kada_lici_na_oznaku(string tekst, string ocekivano)
        {
            Assert.AreEqual(ocekivano, PosaoIzDokumenata.Oznaka(tekst));
        }

        [TestMethod]
        public void V291_Broj_ponude_iz_r7()
        {
            Assert.AreEqual("8957", PosaoIzDokumenata.PonudaIzR7("Payer for SRB: Panšped doo\nKP Ponuda 8957"));
            Assert.IsNull(PosaoIzDokumenata.PonudaIzR7("Payer for SRB: Panšped doo"));
        }

        [TestMethod]
        public void V291_Mase_oznake_tara_nikad_u_bruto()
        {
            var c = List("Šid", "", "Hamm", "", "", new string[0]);
            // LDC8: „Netto: x.xxx,xxx Tare: 643.420 Bruto: x.xxx,xxx“ → Tara 643,420; neto i bruto nisu navedeni (0)
            c.R25 = new Masa { Status = StatusRubrike.Popunjeno, Tekst = "Netto: x.xxx,xxx kg\nTare: 643.420 kg\nBruto: x.xxx,xxx kg", Ukupno = "643.420" };
            var (t, n, b, _) = PosaoIzDokumenata.Mase(c);
            Assert.AreEqual(643420, t); Assert.IsNull(n); Assert.IsNull(b);
            var p = PosaoIzDokumenata.Napravi(c, null, null, Pk(), pravila.Kp);
            Assert.AreEqual(643.42m, p.V["Tara1"]); Assert.AreEqual(0m, p.V["Bruto1"]); Assert.AreEqual(0m, p.V["Neto1"]);
            // r25 bez oznaka = masa robe → neto (SAD-HER 79: 713 123 kg)
            c.R25 = new Masa { Status = StatusRubrike.Popunjeno, Tekst = "713.123", Ukupno = "713.123" };
            (t, n, b, _) = PosaoIzDokumenata.Mase(c);
            Assert.AreEqual(713123, n); Assert.IsNull(t); Assert.IsNull(b);
            // dve od tri → treća (izračunato)
            c.R25 = new Masa { Status = StatusRubrike.Popunjeno, Tekst = "Ntto: 1.510,26 to\nTara: 580,26 to" };
            var m = PosaoIzDokumenata.Mase(c);
            Assert.AreEqual(2090520, Math.Round(m.bruto.Value)); StringAssert.Contains(m.opis, "izračunat");
            // TCDD „Ağırlık 8000 27600 35600“: tri broja bez oznaka, zbir dva = treći
            c.R25 = new Masa { Status = StatusRubrike.Popunjeno, Tekst = "Agirlik 8000 27600 35600", Ukupno = "35600", Pojedinacne = new List<string> { "8000", "27600" } };
            (t, n, b, _) = PosaoIzDokumenata.Mase(c);
            Assert.AreEqual(8000, t); Assert.AreEqual(27600, n); Assert.AreEqual(35600, b);
            // ostale oznake: Ladung / Gesamt / Dara
            c.R25 = new Masa { Status = StatusRubrike.Popunjeno, Tekst = "Wg.Ladung: 31112 kg Dara: 35100 kg Gesamt: 66212 kg" };
            (t, n, b, _) = PosaoIzDokumenata.Mase(c);
            Assert.AreEqual(35100, t); Assert.AreEqual(31112, n); Assert.AreEqual(66212, b);
        }

        [TestMethod]
        public void V291_Kljuc_bez_r62_ref_r8_relacija()
        {
            var c = List("Agigea", "", "Vreoci", "", "", new string[0]);
            c.R62 = new Identifikacija { Zemlja = "53", Stanica = "", Preduzece = "", Broj = "" };
            Assert.AreEqual("ref:BUDAMARCOAL17|1|2", PosaoIzDokumenata.Kljuc(c, "Budamar Coal 17", 1, 2));
            c.R62 = new Identifikacija { Zemlja = "72", Stanica = "16509", Preduzece = "3212", Broj = "16150-0" };
            StringAssert.StartsWith(PosaoIzDokumenata.Kljuc(c, "Budamar Coal 17", 1, 2), "r62:");
        }

        [TestMethod]
        public void V291_Platilac_for_KP_firma_nije_greska_KP7()
        {
            var a = Provere.AnalizirajR7("Payer on IZS: for KP Pansped doo\nMÁV: H-KÁRPÁT 3404, RS: KP 3212", pravila.Kp);
            var za = a.ZaSrbiju.ToList();
            Assert.AreEqual(1, za.Count); Assert.IsFalse(za[0].Kp, "KP nije platilac – firma plaća deonicu KP");
            Assert.AreEqual("Pansped doo", za[0].Naziv);
            Assert.AreEqual("CINKOTURK LOJISTIK", Provere.SkiniPrefiksKp("KP-CINKOTURK LOJISTIK", pravila.Kp));
            // CIM WAGON (TCDD, ID 1496): „FREIGHT PAYER EXPRESS IN⏎KP-CINKOTURK LOJISTIK“ – bez šifre 3212
            var c = Provere.AnalizirajR7("FREIGHT PAYER   EXPRESS IN\nKP-CINKOTURK LOJISTIK", pravila.Kp).ZaSrbiju.ToList();
            Assert.AreEqual(1, c.Count); Assert.AreEqual("CINKOTURK LOJISTIK", c[0].Naziv); Assert.IsFalse(c[0].Kp);
            Assert.AreEqual("Panšped doo", Provere.SkiniPrefiksKp("za KP Panšped doo", pravila.Kp));
            Assert.AreEqual("EurologSystem doo", Provere.SkiniPrefiksKp("für KP EurologSystem doo", pravila.Kp));
            Assert.AreEqual("Panšped", Provere.SkiniPrefiksKp("KP: Panšped", pravila.Kp));
            // prava greška ostaje: KP naveden kao platilac
            Assert.AreEqual("KP UIC 3212, KP", Provere.SkiniPrefiksKp("KP UIC 3212, KP", pravila.Kp));
            Assert.IsTrue(Provere.AnalizirajR7("Frachtzahler SRB: KP UIC 3212, KP", pravila.Kp).ZaSrbiju.Any(x => x.Kp));
        }

        [TestMethod]
        public void V291_Prevoznik_UIC_svi_partneri_i_skracenica()
        {
            var pk = Pk291();
            // „3277 CRW“: UIC nije u šifarniku → skraćenica „CRW“ kao posebna reč naziva
            var p = pk.Partner("CRW", "3277", null, "prevoznik");
            Assert.AreEqual(1722, p.Id, p + " | " + p.Kandidati); Assert.AreEqual(70, p.Pouzdanost, "UIC iz dokumenta nije u šifarniku – najviše 70");
            Assert.IsTrue(pk.ZaSifarnik.Any(x => x.Contains("1722") && x.Contains("UIC 3277")), string.Join("; ", pk.ZaSifarnik));
            // tačan UIC među svim partnerima (Prevoznik nije 1) → uzima se, napomena
            var c = pk.Partner("CER", "3085", null, "prevoznik");
            Assert.AreEqual(1641, c.Id); StringAssert.Contains(c.Napomena, "oznaka Prevoznik nije postavljena");
            Assert.AreEqual(3531, pk.Partner("Fortrack", "5059", null, "prevoznik").Id);
            // nije nađen → lista za šifarnik
            Assert.IsNull(pk.Partner("XYZ Rail", "9999", null, "prevoznik").Id);
            Assert.IsTrue(pk.ZaSifarnik.Any(x => x.StartsWith("nije nađen u Partnerji: UIC 9999")));
        }

        [TestMethod]
        public void V291_Granicni_prelaz_i_stanica_po_kodu()
        {
            var pk = Pk291();
            Assert.AreEqual(328, pk.Granica("Subotica br./Kelebia gr.").Id);
            Assert.AreEqual(328, pk.Granica("55071126 - Kelebia oh.").Id);
            Assert.AreEqual(328, pk.Granica("granični kod 0711").Id);
            Assert.AreEqual(93, pk.Granica("Agigea Nord - Stamora M - Vreoci").Id);
            Assert.AreEqual(118, pk.Granica("13.4 VIA KAPIKULE/SVILENGRAD").Id);
            var z = pk.Granica(null, "HU");
            Assert.AreEqual(328, z.Id); Assert.AreEqual(60, z.Pouzdanost, "samo po zemlji – pouzdanost 60");
            Assert.IsNull(pk.Granica("Beograd").Id);
            // LDC8: r62 72 165167 → 380 Šid (naziv „Industrial siding of Victoriaoil Šid“ ne prolazi prag)
            Assert.AreEqual(380, pk.Stanica("Industrial siding of Victoriaoil", "72165167").Id);
            Assert.AreEqual(1385, pk.Stanica("Kwidzyn", "51 023119").Id);
            // šifra bez kontrolne cifre („72 16509“) = Kod sa kontrolnom cifrom (165092 Sremska Mitrovica)
            Assert.IsTrue(Poklapanje.KodPoklapa("72 16509", "165092"));
            Assert.IsFalse(Poklapanje.KodPoklapa("72 16508", "165092"));
        }

        [TestMethod]
        public void V291_Relacija_KP_nije_u_r57_izvedeni_prevoznici_i_PredajePartneru()
        {
            var pk = Pk291();
            // voz polazi iz Srbije, KP nije u r57 → PredajeOperateru = prevoznik prve strane deonice (pouzdanost ≤ 60)
            var c = List("Šid", "72 16516-7", "Budapest Ferencváros", "55 12345-6", "", new[] { "5059 FORTRACK KELEBIA-BUDAPEST" });
            var p = PosaoIzDokumenata.Napravi(c, null, null, pk, pravila.Kp);
            Assert.AreEqual(3531, p.V["PredajeOperateruID"]);
            Assert.IsTrue(p.Poreklo.First(x => x.Kolona == "PredajeOperateruID").Pouzdanost <= 60);
            Assert.IsNull(p.V["PredajePartneruID"], "KP nije poslednji");
            // voz se završava u Srbiji, KP nije u r57 → PreuzimaOd = poslednji prevoznik; PredajePartneru = primalac iz r4
            c = List("Budapest Ferencváros", "55 12345-6", "Šid", "72 16516-7", "", new[] { "5059 FORTRACK BUDAPEST-KELEBIA" }, r4: "Victoriaoil d.o.o.");
            p = PosaoIzDokumenata.Napravi(c, null, null, pk, pravila.Kp);
            Assert.AreEqual(3531, p.V["PreuzimaOdID"]);
            Assert.AreEqual(2246, p.V["PredajePartneruID"], "KP poslednji → primalac iz r4");
            Assert.AreEqual(328, p.V["Stanica1ID"], "Kelebia → Subotica gr.");
            // ni prevoznika u r57 → pošiljalac maila sa Prevoznik = 1 (SURC 015: CER Hungary)
            c = List("Gdynia", "51 60036-1", "Šid", "72 16516-7", "", new string[0]);
            p = PosaoIzDokumenata.Napravi(c, null, null, pk, pravila.Kp, null, "henrietta.varga.szalai@cercargo.eu");
            Assert.AreEqual(1692, p.V["PreuzimaOdID"]);
            Assert.IsTrue(p.Poreklo.First(x => x.Kolona == "PreuzimaOdID").Pouzdanost <= 60);
        }

        [TestMethod]
        public void V291_Ispravke_16_11_sifra_vise_stanica_skracenica_HR_deonica_KP()
        {
            var s = Sif();
            s.Stanice.Add(new StanicaZapis { Id = 1380, Opis = "Wlasna", Drzava = "PL", Kod = "51023119" });
            s.Stanice.Add(new StanicaZapis { Id = 1385, Opis = "Kwidzyn", Drzava = "PL", Kod = "51023119" });
            s.Stanice.Add(new StanicaZapis { Id = 51, Opis = "Bogojevo gr.", Drzava = "SRB" });
            s.Stanice.First(x => x.Id == 404).Kod = "210013";   // korisnik dodao šifru 16.11.
            s.Partneri.Add(new PartnerZapis { PaSifra = 2338, PaNaziv = "PKP Cargo Connect Sp z.o.o", PaDoNaziv = "PKP Cargo Connect Sp z.o.o", Drzava = "PL", Uic = "9952", Prevoznik = true });
            s.Partneri.Add(new PartnerZapis { PaSifra = 1722, PaNaziv = "CENTRAL RAILWAYS A.S. CRW", PaDoNaziv = "CENTRAL RAILWAYS A.S. CRW", Drzava = "SK", Prevoznik = true });
            s.Partneri.Add(new PartnerZapis { PaSifra = 1641, PaNaziv = "CER Slovakia a.s.", PaDoNaziv = "CER Slovakia a.s.", Drzava = "HU", Uic = "3085", Prevoznik = true });
            foreach (var (t, id) in new[] { ("KELEBIA", 328), ("0711", 328), ("HU", 328), ("TOVARNIK", 381), ("ERDUT", 51) })
                s.Sinonimi.Add(new SinonimZapis { Tip = "GRANICA", Tekst = t, CiljniId = id });
            var pk = new Poklapanje(s.Pripremi());
            // (2) ista šifra kod više stanica: nijedna korišćena → NULL i kandidati; korišćena → ona
            var w = pk.Stanica("Ont P", "51023119");
            Assert.IsNull(w.Id, w.ToString()); StringAssert.Contains(w.Kandidati, "1385"); StringAssert.Contains(w.Napomena, "nijedna nije korišćena");
            s.UpotrebaStanica[1385] = 12;
            Assert.AreEqual(1385, new Poklapanje(s).Stanica("Ont P", "51023119").Id, "pravilo 6 – najviše korišćena");
            s.UpotrebaStanica.Remove(1385);
            // (3) skraćenica: partner sa drugim UIC-om nije pogodak (PKP 2151 ≠ 2338); bez UIC-a – pouzdanost ≤ 70
            Assert.IsNull(pk.Partner("PKP Cargo Spolka Akcyjna in restructuring", "2151", null, "prevoznik").Id);
            var crw = pk.Partner("CRW", "3277", null, "prevoznik");
            Assert.AreEqual(1722, crw.Id); Assert.IsTrue(crw.Pouzdanost <= 70);
            Assert.IsTrue(pk.ZaSifarnik.Any(x => x.StartsWith("proveriti") && x.Contains("1722")));
            // (5) HR samo po zemlji – dva prelaza → NULL i kandidati
            var hr = pk.Granica(null, "HR");
            Assert.IsNull(hr.Id); StringAssert.Contains(hr.Kandidati, "381"); StringAssert.Contains(hr.Kandidati, "51");
            Assert.AreEqual(51, pk.Granica("via Erdut").Id);
            // (1) deonica KP iz šifara r57 (Drenik: „3212 KP 72 0711 210013“): Stanica1 328, Stanica3 i Uputna 404 (r10/r12 nečitljivi)
            var c = List("Ont P", "", "- Kea)", "", "", new[] { "2151 PKP 54 0825 0890", "3085 CER 55 0887 0711", "3212 KP 72 0711 210013" });
            c.R62 = new Identifikacija { Zemlja = "51", Stanica = "023119", Preduzece = "2151", Broj = "532986", Status = StatusRubrike.Popunjeno };
            var p = PosaoIzDokumenata.Napravi(c, null, null, pk, pravila.Kp);
            Assert.AreEqual(328, p.V["Stanica1ID"]); Assert.AreEqual(404, p.V["Stanica3ID"]); Assert.AreEqual(404, p.V["UputnaID"]);
            Assert.AreEqual(1641, p.V["PreuzimaOdID"], "CER 3085");
            Assert.IsNull(p.V["OtpravnaID"], "Wlasna/Kwidzyn ista šifra, nijedna korišćena");
            // ista granična stanica na oba kraja (bez šifara deonice) → Stanica3 NULL i kandidati
            c = List("Budapest Ferencváros", "55 12345-6", "Zagreb", "78 72480-0", "", new[] { "2155 RCH BUDAPEST-KELEBIA", "3212 KP KELEBIA-SUBOTICA", "2178 HZC SUBOTICA-ZAGREB" });
            p = PosaoIzDokumenata.Napravi(c, null, null, pk, pravila.Kp);
            Assert.AreEqual(328, p.V["Stanica1ID"]); Assert.IsNull(p.V["Stanica3ID"], "ne prepisuje se granična stanica na drugi kraj");
        }

        [TestMethod]
        public void V291_Kopija_lista_iz_drugog_maila_i_razliciti_vozovi()
        {
            // Coal 16: CIM (r62) i sken iz drugog maila (ključ „ref:“) – iste mase → isti list; Coal 17 – druge mase → drugi list
            var c16 = new ZapisTl { Kljuc = "r62:53-810168-3414-092542", Neto = 1725636, Tara = 700170, Bruto = 2425806 };
            var c16sken = new ZapisTl { Kljuc = "ref:BUDAMARCOAL16|1479|90", Neto = 1725636, Tara = 700170, Bruto = 2425806 };
            var c17 = new ZapisTl { Kljuc = "ref:BUDAMARCOAL17|1479|90", Tara = 700620 };
            Assert.IsTrue(UpisUBazu.IstiZapis(c16, c16sken));
            Assert.IsFalse(UpisUBazu.IstiZapis(c16, c17));
            // kola odlučuju kada ih oba zapisa imaju
            Assert.IsFalse(UpisUBazu.IstiZapis(new ZapisTl { Kola = { "315554000242" }, Neto = 5700, Bruto = 29100 }, new ZapisTl { Kola = { "315554000010" }, Neto = 5700, Bruto = 29100 }));
        }

        private static ListPosla Lp(string kljuc, string[] kola, int s1 = 246, int s3 = 118, bool prazna = false, double? neto = null, double? tara = null, string nhm4 = "2520")
        {
            var p = new PodaciPosla { KljucPosiljke = kljuc, Kola = kola.ToList(), Prazna = prazna, Nhm4 = nhm4, NetoKg = neto, TaraKg = tara };
            p.V["Stanica1ID"] = s1; p.V["Stanica3ID"] = s3; p.V["OtpravnaID"] = s1; p.V["UputnaID"] = 993; p.V["BrojKola1"] = kola.Length;
            p.Postavi("Referenca", null, "", "r8", 0);
            return new ListPosla { P = p, Oznaka = kljuc };
        }

        [TestMethod]
        public void V291_Kopije_istog_lista_i_jedan_voz()
        {
            // isti r62 u više listova kompleta → jedan; ključ „ref:“ isti, a kola različita → dva lista
            var a = Lp("r62:72-220012-3212-160010", new[] { "315554000242" });
            var b = Lp("r62:72-220012-3212-160010", new[] { "315554000242" });
            b.P.Postavi("Referenca", "WAGON 1", "WAGON 1", "r8", 90);
            var c = Lp("ref:X|246|993", new[] { "315554000010" });
            var d = Lp("ref:X|246|993", new[] { "315554000291" });
            var sp = UpisUBazu.SpojiKopije(new List<ListPosla> { a, b, c, d });
            Assert.AreEqual(3, sp.Count);
            Assert.AreEqual("WAGON 1", sp.First(x => x.P.KljucPosiljke.StartsWith("r62")).P.V["Referenca"], "polje iz bolje pročitane kopije");
            Assert.AreNotEqual(c.KljucTl, d.KljucTl, "zapis TL je jedinstven po listu");
            // ista deonica KP → jedan voz; druga Stanica1 (Paraćin) → drugi voz
            var e = Lp("ref:Y|247|993", new[] { "315554000999" }, s1: 247);
            var vozovi = UpisUBazu.Vozovi(new List<ListPosla> { a, c, d, e });
            Assert.AreEqual(2, vozovi.Count);
            Assert.AreEqual(3, vozovi.First(v => v.Listovi[0].Stanica1 == 246).Listovi.Count);
        }

        [TestMethod]
        public void V291_Grupe_posla_natovarena_i_prazna_kola()
        {
            var tl = new List<ZapisTl>
            {
                ZapisTl.Iz(Lp("r62:a", Enumerable.Range(0, 27).Select(i => "3155540" + i.ToString("00000")).ToArray(), neto: 1510260, tara: 580260)),
                ZapisTl.Iz(Lp("r62:b", new[] { "338549920001" }, prazna: true, tara: 21600, nhm4: "9922")),
            };
            tl[0].BrojKola = 27;
            var g = UpisUBazu.Grupe(tl);
            Assert.AreEqual(2, g.Count);
            Assert.IsFalse(g[0][0].Prazna, "grupa 1 = natovarena kola");
            Assert.IsTrue(g[1][0].Prazna);
            Assert.AreEqual(21600, g[1][0].Bruto, "prazna kola: bruto = tara");
            Assert.AreEqual(0, g[1][0].Neto);
        }

        [TestMethod]
        public void V291_Spisak_red_ukupno_samo_tara_intermodal()
        {
            var pk = Pk();
            // zbir stavki se ne slaže sa redom „Ukupno“ (> 0,5 %) → upisuje se „Ukupno“, napomena za ručnu proveru
            var s = new SpisakKola { NhmZaglavlje = "252010", UkupnoNetoKg = 100000 };
            s.Kola.Add(new KolaNaSpisku { Broj = "338753123456", NetoKg = 44000, TaraKg = 20000 });
            s.Kola.Add(new KolaNaSpisku { Broj = "338753123457", NetoKg = 45000, TaraKg = 21000 });
            var p = PosaoIzDokumenata.IzSpiska(s, pk, "", "");
            Assert.AreEqual(100m, p.V["Neto1"]); Assert.AreEqual(41m, p.V["Tara1"]); Assert.AreEqual(130m, p.V["Bruto1"], "bruto = neto + tara");
            Assert.IsTrue(p.Napomene.Any(x => x.Contains("ručnu proveru")));
            // samo tara (Coal 17: kola se tek tovare) → Tara, bez neta i bruta
            s = new SpisakKola();
            s.Kola.Add(new KolaNaSpisku { Broj = "317853001574", TaraKg = 21700 });
            s.Kola.Add(new KolaNaSpisku { Broj = "317853003273", TaraKg = 21150 });
            p = PosaoIzDokumenata.IzSpiska(s, pk, "", "");
            Assert.AreEqual(42.85m, p.V["Tara1"]); Assert.IsFalse(p.V.ContainsKey("Neto1")); Assert.IsFalse(p.V.ContainsKey("Bruto1"));
            // intermodalni voz: NHM1 = najčešća grupa po jedinicama, sva natovarena kola u jednoj grupi
            s = new SpisakKola { NhmJedinica = new List<string> { "340250", "340250", "340250", "310540", "210610" } };
            s.Kola.Add(new KolaNaSpisku { Broj = "378049925456", Nhm = "340250", Uti = { "LKWA9039609" } });
            s.Kola.Add(new KolaNaSpisku { Broj = "358049562889", Nhm = "310540", Uti = { "LKWA2027817" } });
            p = PosaoIzDokumenata.IzSpiska(s, pk, "", "");
            Assert.AreEqual(330, p.V["NHM1"], "grupa 3402 (Kod4)"); Assert.AreEqual(2, p.V["BrojKola1"]); Assert.IsNull(p.V["NHM2"]);
            Assert.IsTrue(p.Napomene.Any(x => x.Contains("3105 ×1")));
            // dužina voza sa spiska (CER „Vonat hossza m“) kada dužine kola nisu poznate
            s = new SpisakKola { DuzinaVozaM = 479.6 };
            s.Kola.Add(new KolaNaSpisku { Broj = "215124694328", NetoKg = 27194 });
            p = PosaoIzDokumenata.IzSpiska(s, pk, "", "");
            Assert.AreEqual(479.6m, p.V["Duzina1"]);
        }

        [TestMethod]
        public void V291_Spisak_kolone_LKW_CIT23_i_red_ukupno()
        {
            var kp = pravila.Kp;
            // LKW Walter „Specification…“: kolone kola (Tara vagona, Wg.Ladung, Ukupna težina) imaju prednost nad kolonama prikolica
            var redovi = new List<string[]>
            {
                new[] { "Pos.", "Wagon Number", "Tara vagona", "Wg.Ladung", "Registration of trailer", "Unit-Nr", "Tara", "Netto", "Brutto", "NHM", "Ukupna težina" },
                new[] { "1", "378049925456", "35100", "31112.99", "HROM3960", "LKWA9039609", "7000", "9893.64", "16893.64", "340250", "66212.99" },
                new[] { "", "", "", "", "HROM4067", "LKWA9040678", "6650", "7569.35", "14219.35", "340250", "" },
                new[] { "2", "358049566328", "35000", "43915.45", "HROB4918", "LKWA2049185", "7000", "17151.84", "24151.84", "340250", "78915.45" },
                new[] { "3", "358049564703", "35000", "37466.65", "HROM3912", "LKWA9039126", "6650", "16595.29", "23245.29", "310540", "72466.65" },
                new[] { "", "", "", "112495.09", "", "", "", "", "112495.09", "", "217595.09" },
            };
            var d = ExcelCitac.AnalizaRedova(redovi, kp, "Specification.xls", "excel", "Excel").First(x => x.Tip == TipDokumenta.SpisakKola);
            var s = d.Spisak[0];
            Assert.AreEqual(3, s.Kola.Count, "kola = redovi sa brojem kola, ne prikolice");
            // v3.0 (0a.4): mase po kolima se ne zaokružuju pre sabiranja (zbir se zaokružuje jednom, na ceo kg)
            Assert.AreEqual(35100, s.Kola[0].TaraKg); Assert.AreEqual(31112.99, s.Kola[0].NetoKg.Value, 1e-9); Assert.AreEqual(66212.99, s.Kola[0].BrutoKg.Value, 1e-9);
            Assert.AreEqual(112495, s.UkupnoNetoKg); Assert.AreEqual(217595, s.UkupnoBrutoKg);
            Assert.AreEqual(4, s.NhmJedinica.Count, "NHM po jedinicama (prikolicama)");
            // CIT 23 (Prilog 4 GLV-TC): red sa brojevima kolona 1–9 – kolona 6 = neto, 7 = tara, 8 = bruto
            redovi = new List<string[]>
            {
                new[] { "Spisak kola - Wagenliste", "Prilog - Anlage 4 GLV - TC 23 GTM - CIT" },
                new[] { "Red. Br.", "Kola br.", "Osovine", "Naznačenje robe", "Pozicija NHM", "RID", "Masa", "", "Tara kola", "Bruto masa", "Napomene" },
                new[] { "1", "2", "3", "4", "", "5", "6", "", "7", "8", "9" },
                new[] { "1", "314354200367", "4", "gypsum", "2833 29", "", "54100", "56200", "21100", "75200", "" },
                new[] { "2", "314354200276", "4", "gypsum", "2833 29", "", "55200", "55200", "21000", "76200", "" },
                new[] { "Ukupno - Insgesamt", "", "8", "osovina", "", "", "109300", "", "42100", "151400", "" },
            };
            s = ExcelCitac.AnalizaRedova(redovi, kp, "CIT 23 GTM.xls", "excel", "Excel").First(x => x.Tip == TipDokumenta.SpisakKola).Spisak[0];
            Assert.AreEqual(54100, s.Kola[0].NetoKg); Assert.AreEqual(21100, s.Kola[0].TaraKg); Assert.AreEqual(75200, s.Kola[0].BrutoKg);
            Assert.AreEqual(109300, s.UkupnoNetoKg); Assert.AreEqual(42100, s.UkupnoTaraKg);
        }

        [TestMethod]
        public void V291_Zaglavlje_spiska_CER_i_PSP()
        {
            var redovi = new List<string[]>
            {
                new[] { "", "Vonat hossza m", "", "Feladási állomás" },
                new[] { "", "479.6", "", "51600361 - Gdynia Port-Zmpg" },
                new[] { "", "Szállítandó áru", "Rakomány tömege t", "Érkezési állomás" },
                new[] { "", "48109200 - Papír és karton", "948.946", "55071126 - Kelebia oh." },
                new[] { "#", "Pályaszám", "Tsz.", "Hossz", "Saját", "Rakomány", "Elegy" },
                new[] { "1.", "21 51 2469 432-8", "2", "15.5", "15", "27.194", "42.194" },
                new[] { "2.", "21 51 2469 149-8", "2", "15.5", "14.9", "27.15", "42.05" },
            };
            var s = ExcelCitac.AnalizaRedova(redovi, pravila.Kp, "Kocsilista.xlsx", "excel", "Excel").First(x => x.Tip == TipDokumenta.SpisakKola).Spisak[0];
            Assert.AreEqual(479.6, s.DuzinaVozaM); Assert.AreEqual(948946, s.UkupnoNetoKg);
            StringAssert.Contains(s.PrelazniPut, "Kelebia"); StringAssert.StartsWith(s.NhmZaglavlje, "48109200");
            Assert.AreEqual(15000, s.Kola[0].TaraKg); Assert.AreEqual(27194, s.Kola[0].NetoKg); Assert.AreEqual(42194, s.Kola[0].BrutoKg); Assert.AreEqual(2, s.Kola[0].Osovine);
            // PSP „Lista de vagoane“: Itinerariu → prelazni put
            redovi = new List<string[]>
            {
                new[] { "Itinerariu", "" }, new[] { "Agigea Nord - Stamora M - Vreoci", "" },
                new[] { "Nr.", "Nr. Vagon", "Nr. osii", "Masa inc.", "Tara vg.", "Masa bruta" },
                new[] { "1", "3178 530 0157-4", "4", "", "21700", "" },
                new[] { "2", "3178 530 0327-3", "4", "", "21150", "" },
            };
            s = ExcelCitac.AnalizaRedova(redovi, pravila.Kp, "wagon list 17.xlsx", "excel", "Excel").First(x => x.Tip == TipDokumenta.SpisakKola).Spisak[0];
            StringAssert.Contains(s.PrelazniPut, "Stamora");
            Assert.AreEqual(21700, s.Kola[0].TaraKg); Assert.IsNull(s.Kola[0].NetoKg);
        }

        [TestMethod]
        public void V291_CIT7_ispravka_prevoznik_i_box25()
        {
            var iz = Citanje.Cit7Citac.Izmene("Correction: Box 25: 941 389 kg");
            Assert.AreEqual(1, iz.Count); Assert.AreEqual("25", iz[0].Rubrika); StringAssert.Contains(iz[0].Vrednost, "941 389");
            var z = new Cit7Zahtev { Napomene = "Remarks: wagon 358049566241 detached\nCorrection: Box 25: 941 389 kg", Podnosilac = "Bratislava CENTRAL RAILWAYS a.s.", Kola = "378049564271 + 17 wagons" };
            Assert.IsTrue(Cit7Provera.PrevoznikIliIspravka(z));
            Assert.IsFalse(Cit7Provera.PrevoznikIliIspravka(new Cit7Zahtev { Napomene = "Box 4: GRANEXPORT", Podnosilac = "Panšped doo" }));
        }

        [TestMethod]
        public void V291_Napomena_bez_potpisa_linkova_i_telefona()
        {
            var m = new MailZaUpis
            {
                Posiljalac = "dispo@crw.sk", Naslov = "ARR - Aviso: 46836/15", Primljeno = new DateTime(2026, 11, 15, 11, 56, 0),
                Telo = "Hello,\nthe train arrived.\n\nBest regards\nJán Novák\nDispatcher\nTel.: +421 2 1234 5678\nwww.crw.sk\nThis e-mail and any attachments are confidential.",
            };
            var b = UpisUBazu.BlokNapomene(m);
            StringAssert.StartsWith(b, "15.11.2026 11:56 dispo@crw.sk: ARR - Aviso: 46836/15");
            StringAssert.Contains(b, "the train arrived"); StringAssert.Contains(b, "Ján Novák");
            Assert.IsFalse(b.Contains("Dispatcher") || b.Contains("+421") || b.Contains("www.") || b.Contains("confidential"), b);
        }
    }
}
