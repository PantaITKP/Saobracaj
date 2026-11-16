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
            void Nh(int id, string broj, string naziv) => s.Nhm.Add(new NhmZapis { Id = id, Broj = broj, Naziv = naziv });
            Nh(8, "1001", "PŠENICA"); Nh(92, "2520", "GIPS"); Nh(268, "2521", "KAMEN KREČNJAČKI"); Nh(6, "9922 00", "PRAZNI VAGONI-PREVOZ NA UTOVAR");
            Nh(243, "9922 10", "PRAZNI VAGONI"); Nh(188, "9922 40", "PRAZNI VAGONI"); Nh(162, "9920", "PRAZAN"); Nh(267, "9943/9944", "POLUPRIKOLICE");
            // sinonimi kao u bazi (OP_914_tmp_Sinonimi.sql)
            s.Sinonimi.Add(new SinonimZapis { Tip = "PARTNER", Tekst = "PANSPED", CiljniId = 1969 });
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
        [DataRow("Budamar")]                      // tri Budamar partnera – ne pogađa
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
            // 3355 „BUDAMAR SOUTH, s.r.o.“ i 3143 „BUDAMAR SOUTH D.O.O. BEOGRAD“ imaju isti naziv posle normalizacije (razlika < 0,05)
            var p = Pk().Partner("Budamar South, s.r.o.");
            Assert.IsNull(p.Id, p.ToString());
            StringAssert.Contains(p.Kandidati, "3355"); StringAssert.Contains(p.Kandidati, "3143");
            StringAssert.Contains(p.Napomena, "dva bliska kandidata");
            // izuzetak: naziv iz tmpSinonimi
            var s = Sif(); s.Sinonimi.Add(new SinonimZapis { Tip = "PARTNER", Tekst = "BUDAMAR SOUTH", CiljniId = 3355 });
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
            var pk = Pk();
            Assert.AreEqual(92, pk.Nhm("252010").Id, "252010 → 2520");
            Assert.AreEqual(6, pk.Nhm("992200").Id, "992200 → 9922 00");
            Assert.AreEqual(243, pk.Nhm("992210").Id);
            Assert.AreEqual(8, pk.Nhm("100100").Id);
            Assert.AreEqual(8, pk.Nhm("1001.00").Id);
            var p = pk.Nhm("992290");   // podgrupa 90 ne postoji → najmanji ID, izbor u poreklu
            Assert.AreEqual(6, p.Id); StringAssert.Contains(p.Napomena, "najmanji ID");
            Assert.AreEqual(267, pk.Nhm("994300").Id);
            Assert.IsNull(pk.Nhm("123456").Id);
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
            // samo tara (prazna kola): bruto = tara, neto = 0
            c.R25 = new Masa { Status = StatusRubrike.Popunjeno, Tekst = "Tara: 650.000 kg" };
            p = PosaoIzDokumenata.Napravi(c, null, null, Pk(), pravila.Kp);
            Assert.AreEqual(650m, p.V["Bruto1"]); Assert.AreEqual(0m, p.V["Neto1"]);
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
    }
}
