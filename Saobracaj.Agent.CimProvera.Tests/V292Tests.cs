using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera;
using Saobracaj.Agent.CimProvera.Baza;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>
    /// v2.9.2: platilac preko broja ponude (1), KP u r57 (2), ispravke posle testova 16.–17.11. (3) i nalazi ponovne obrade od UID 191 (3a).
    /// </summary>
    [TestClass]
    public class V292Tests
    {
        private static PravilaSkup pravila;

        [ClassInitialize]
        public static void Init(TestContext _)
        {
            var put = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pravila", "cim_validation_rules.json");
            pravila = PravilaSkup.IzTeksta(File.ReadAllText(put), put);
        }

        [TestCleanup]
        public void Ocisti() { Ponude.Zamena = null; Ponude.OcistiKes(); }

        private static T F<T>(T r, string tekst) where T : Rubrika { r.Status = StatusRubrike.Popunjeno; r.Pouzdanost = 0.95; r.Tekst = tekst; return r; }

        /// <summary>Spoljni pošiljalac (r62 drugog prevoznika), r7 zadat tekstom.</summary>
        private static CimList ListSaR7(string r7)
        {
            var c = MotorTests.IspravanCim();
            c.R62 = F(new Identifikacija { Zemlja = "55", Stanica = "123459", Preduzece = "2155", Broj = "12345-6" }, "55 12345-9 2155 12345-6");
            c.R58a = F(new UgovorniPrevoznik { Sifra = "2155", Naziv = "RCH" }, "2155 RCH");
            c.R7 = F(new ListaRubrika(), r7);
            c.R14 = new TekstRubrika();
            return c;
        }

        private static RezultatProvere Proveri(CimList c, string rezim = "spoljni")
        {
            var u = new UlazProvere
            {
                Dokumenti = new List<Dokument> { new Dokument { Tip = TipDokumenta.Cim, Fajl = "cim.pdf", Izvor = "tekst", Strane = { 1 }, Cim = { c } } },
                RezimKp = rezim, ReferentniDatum = new DateTime(2026, 11, 17), Prag = 0.7,
            };
            return new Motor(pravila).Proveri(u);
        }

        private static IEnumerable<Nalaz> Svi(RezultatProvere r) => r.Listovi.SelectMany(l => l.Nalazi.Concat(l.Nalazi.SelectMany(n => n.Takodje)));

        private static void Ponuda(int broj, int partner, string naziv) =>
            Ponude.Zamena = b => b == broj ? new Ponude.Rezultat { Stanje = Ponude.Stanje.Nadjena, Broj = b, PartnerId = partner, Naziv = naziv } : new Ponude.Rezultat { Stanje = Ponude.Stanje.NijeNadjena, Broj = b };

        // ------------------------------------------------------------------------------------------- 1. ponuda

        [DataTestMethod]
        [DataRow("SRB: KP UIC 3212, KP Ponuda 8957", "8957")]
        [DataRow("KP Ponuda8957; Import c.clearance", "8957")]
        [DataRow("Ponuda br. 8957", "8957")]
        [DataRow("ponuda: 8957", "8957")]
        [DataRow("Platilac za Kombinovani prevoz MBOX po ponudi 8926", "8926")]
        [DataRow("prema ponudi 8926", "8926")]
        [DataRow("Offer 8957", "8957")]
        [DataRow("Payer for SRB: Ferest according offer nr 5856", "5856")]
        [DataRow("acc. to offer no. 5856", "5856")]
        [DataRow("Angebot (Nr.) 8957", "8957")]
        [DataRow("Ajánlat 8957", "8957")]
        [DataRow("Ponuda KP br. 9241", "9241")]
        public void Broj_ponude_u_svim_oblicima(string tekst, string broj)
        {
            var (o, b, _) = Provere.BrojPonude(tekst);
            Assert.AreEqual(broj, o, tekst);
            Assert.AreEqual(int.Parse(broj), b, tekst);
        }

        [TestMethod]
        public void Broj_ponude_sa_crticom_nije_ceo_broj_i_ne_trazi_se()
        {
            var (o, b, _) = Provere.BrojPonude("Ponuda br. 42-0003-26");
            Assert.AreEqual("42-0003-26", o);
            Assert.IsNull(b, "oznaka sa crticama ostaje BrojPonude, ali se ne traži u tabeli ponuda");
            Assert.IsNull(Provere.BrojPonude("Frachtzahler SRB: Panšped doo").oznaka);
            // više brojeva – najbliži oznaci SRB / KP / 3212
            Assert.AreEqual(8957, Provere.BrojPonude("HU: Ajánlat 1111; SK: offer 2222; SRB: KP UIC 3212, KP Ponuda 8957").broj);
        }

        [TestMethod]
        public void Ponuda_nadjena_KP_za_SRB_nije_greska_i_r7_se_ne_oznacava()
        {
            Ponuda(8957, 1574, "VTG RAIL LOGISTICS AUSTRIA GMBH");
            var c = ListSaR7("16. Frachtzaler PL, CZ, SK: CDC Slovakia; HU: Fortrack Kft. UIC 5059; SRB: KP UIC 3212, KP Ponuda 8957; Import c.clearance in Bor");
            var r = Proveri(c);
            var svi = Svi(r).ToList();
            Assert.IsFalse(svi.Any(n => n.Pravilo == "KP7-NIJE-PLATILAC" || n.Pravilo == "KP7-PLATILAC" || n.Pravilo == "KP7-NEJASNO"), string.Join(" | ", svi.Select(n => n.Pravilo + ": " + n.Tekst)));
            var info = svi.Single(n => n.Kljuc == "ponuda");
            Assert.IsTrue(info.SamoInterno);
            StringAssert.Contains(info.Tekst, "VTG RAIL LOGISTICS AUSTRIA GMBH");
            Assert.IsFalse(svi.Any(n => n.Rubrika == "7" && !n.SamoInterno && (n.Dopuna ?? "").Contains("Platilac za deonicu SRB")), "u PRIMER-u nema „Platilac za deonicu SRB: XXXXX“");
            // upis: PlatilacID = PonPart, BrojPonude = broj, poreklo „ponuda 8957 → 1574 …“, pouzdanost 100
            var p = PosaoIzDokumenata.Napravi(c, null, null, new Poklapanje(UpisUBazuTests.Sif()), pravila.Kp);
            Assert.AreEqual(1574, p.V["PlatilacID"]);
            Assert.AreEqual("8957", p.V["BrojPonude"]);
            var po = p.Poreklo.Single(x => x.Kolona == "PlatilacID");
            StringAssert.StartsWith(po.Tekst, "ponuda 8957 → 1574 VTG RAIL LOGISTICS AUSTRIA GMBH");
            Assert.AreEqual(100, po.Pouzdanost);
        }

        [TestMethod]
        public void Ponuda_nije_nadjena_greska_kao_do_sada_uz_tekst()
        {
            Ponuda(1, 1, "X");
            var c = ListSaR7("Frachtzahler SRB: KP UIC 3212, KP Ponuda 8957");
            var svi = Svi(Proveri(c)).ToList();
            var g = svi.FirstOrDefault(n => n.Pravilo == "KP7-NIJE-PLATILAC" || n.Pravilo == "KP7-PLATILAC" || n.Pravilo == "KP7-NEJASNO");
            Assert.IsNotNull(g, string.Join(" | ", svi.Select(n => n.Pravilo)));
            StringAssert.Contains(g.Tekst, "Ponuda 8957 nije pronađena u sistemu KP");
            var p = PosaoIzDokumenata.Napravi(c, null, null, new Poklapanje(UpisUBazuTests.Sif()), pravila.Kp);
            Assert.IsNull(p.V["PlatilacID"]);
            StringAssert.Contains(p.Poreklo.Single(x => x.Kolona == "PlatilacID").Kandidati, "ponuda 8957 nije pronađena u sistemu KP");
        }

        [TestMethod]
        public void Ponuda_istog_partnera_kao_firma_u_r7_nista()
        {
            Ponuda(8926, 2109, "MBOX Terminals D.O.O.");
            var c = ListSaR7("Frachtzahler für SRB: MBOX Terminals doo, Ponuda 8926");
            var svi = Svi(Proveri(c)).ToList();
            Assert.IsFalse(svi.Any(n => n.Pravilo.StartsWith("KP7") && n.Tezina != Tezina.Info), string.Join(" | ", svi.Select(n => n.Pravilo + " " + n.Tezina)));
            Assert.IsFalse(svi.Any(n => n.Kljuc == "ponuda" && n.Tezina == Tezina.Upozorenje));
        }

        [TestMethod]
        public void Ponuda_drugog_partnera_interno_upozorenje_i_ponuda_ima_prednost()
        {
            Ponuda(5856, 1584, "FEREST RAIL SpA");
            var c = ListSaR7("Frachtzahler für SRB: Panšped doo, offer nr 5856");
            var svi = Svi(Proveri(c)).ToList();
            var u = svi.Single(n => n.Kljuc == "ponuda");
            Assert.IsTrue(u.SamoInterno, "pošiljaocu ništa");
            Assert.AreEqual(Tezina.Upozorenje, u.Tezina);
            StringAssert.Contains(u.Tekst, "r7 navodi Panšped doo, ponuda 5856 glasi na FEREST RAIL SpA");
            var p = PosaoIzDokumenata.Napravi(c, null, null, new Poklapanje(UpisUBazuTests.Sif()), pravila.Kp);
            Assert.AreEqual(1584, p.V["PlatilacID"], "ponuda ima prednost nad firmom iz r7");
        }

        [TestMethod]
        public void Bez_broja_ponude_pravila_kao_do_sada()
        {
            Ponuda(8957, 1574, "VTG");
            var svi = Svi(Proveri(ListSaR7("Frachtzahler SRB: KP UIC 3212"))).ToList();
            Assert.IsTrue(svi.Any(n => n.Pravilo == "KP7-NIJE-PLATILAC" || n.Pravilo == "KP7-NEJASNO"), string.Join(" | ", svi.Select(n => n.Pravilo)));
            Assert.IsFalse(svi.Any(n => n.Kljuc == "ponuda" || n.Kljuc == "ponuda-baza"));
        }

        [TestMethod]
        public void Baza_nedostupna_bez_greske_posiljaocu_interno_i_rucna_provera()
        {
            Ponude.Zamena = b => new Ponude.Rezultat { Stanje = Ponude.Stanje.Nedostupna, Broj = b };
            var c = ListSaR7("Frachtzahler SRB: KP UIC 3212, KP Ponuda 8957");
            var r = Proveri(c);
            var svi = Svi(r).ToList();
            Assert.IsFalse(svi.Any(n => (n.Pravilo == "KP7-NIJE-PLATILAC" || n.Pravilo == "KP7-PLATILAC") && !n.SamoInterno));
            var b0 = svi.Single(n => n.Kljuc == "ponuda-baza");
            Assert.IsTrue(b0.SamoInterno);
            StringAssert.Contains(b0.Tekst, "nije proverena – baza nedostupna");
            Assert.IsTrue(r.Listovi[0].ZaRucnuProveru, "izveštaj ide samo INTERNO dok se ponuda ne proveri");
            var p = PosaoIzDokumenata.Napravi(c, null, null, new Poklapanje(UpisUBazuTests.Sif()), pravila.Kp);
            StringAssert.Contains(p.Poreklo.Single(x => x.Kolona == "PlatilacID").Kandidati, "baza nedostupna");
        }

        [TestMethod]
        public void Oznaka_ponude_nije_deo_naziva_platioca()
        {
            var kp = pravila.Kp;
            Assert.AreEqual("Ferest", Provere.AnalizirajR7("Payer for SRB: Ferest according offer nr 5856", kp).ZaSrbiju.Single().Naziv);
            Assert.AreEqual("Panšped doo", Provere.AnalizirajR7("Frachtzahler für SRB: Panšped doo, acc. to offer no. 5856", kp).ZaSrbiju.Single().Naziv);
            Assert.IsTrue(Provere.IstiPartner("VTG Rail Logistics Austria GmbH", "VTG RAIL LOGISTICS AUSTRIA GMBH"));
            Assert.IsFalse(Provere.IstiPartner("Panšped doo", "FEREST RAIL SpA"));
        }

        [TestMethod]
        public void Sken_CIM_po_imenu_fajla_i_sadrzaju_nije_faktura()
        {
            // „CIM za voz br. 262 ….pdf“: crno-beli sken, natpisi obrasca nepročitani, r9 „Faktura/invoice“
            var t = " sunoko d o o frachtzaiter auf kp hz came und sz tp lat eurologsyatem cim6 kp 26 hzc 26 faktura involce list of wagons cit 23 13 3 3213 kp 2178 hzc tara 582 800 00 kg ";
            Assert.IsTrue(Citanje.LokalniCitac.SadrzajTovarnogLista(t) >= 3);
            Assert.IsTrue(Citanje.LokalniCitac.SadrzajTovarnogLista(" invoice total amount price eur ") < 3);
        }

        [TestMethod]
        public void Upit_ponude_cita_samo_PonPart_i_parametar()
        {
            StringAssert.Contains(Ponude.Upit, "WHERE PonStPon = @BrojPonude");
            StringAssert.StartsWith(Ponude.Upit, "SELECT PonPart AS SifraPartnera, RTRIM(PaNaziv) AS Platilac FROM dbo.Ponudba");
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(Ponude.Upit, @"(?i)\b(insert|update|delete|cena|iznos)\b"));
        }

        // ------------------------------------------------------------------------------------------- 2. r57

        private static CimList ListSaR57(string tekst, params Prevoznik[] prevoznici)
        {
            var c = MotorTests.IspravanCim();
            c.R16 = F(new Preuzimanje { Mesto = "Gliwice", SifraStanice = "510700", Zemlja = "51", Datum = "2026-11-05", Cas = "14" }, "510700 Gliwice 51");   // v3.0.2 (2.4): polazak iz inostranstva – KP57 važi
            c.R62 = F(new Identifikacija { Zemlja = "51", Stanica = "070409", Preduzece = "2154", Broj = "14331-0" }, "51 070409 2154 14331");
            c.R58a = F(new UgovorniPrevoznik { Sifra = "2154", Naziv = "ČD Cargo" }, "2154 ČD Cargo");
            c.R57 = F(new PrevozniciRubrika(), tekst);
            c.R57.Prevoznici.AddRange(prevoznici);
            return c;
        }

        [DataTestMethod]
        [DataRow("2154 CD Cargo   Bohumin br.-Mosty u Jablunkova br.\n3820 CDC Slovakia   Cadca br. - Sturovo br.\n5059 Fortrack Kft   Szob br. - Kelebia br.\n3212 Kombinovani Prevoz   Subotica br. - Bor Teretna")]
        [DataRow("RS: 3212 KP")]
        [DataRow("SRB: KP UIC 3212")]
        [DataRow("KP 3212 Subotica gr. - Bor teretna")]
        [DataRow("3212 KP")]
        public void R57_naziv_i_UIC_ispravno(string tekst)
        {
            var svi = Svi(Proveri(ListSaR57(tekst))).ToList();
            Assert.IsFalse(svi.Any(n => n.Pravilo == "KP57"), string.Join(" | ", svi.Where(n => n.Pravilo == "KP57").Select(n => n.Tekst)));
        }

        [TestMethod]
        public void R57_samo_naziv_ili_samo_UIC_upozorenje_nikad_greska_ni_naziv_ni_UIC_greska()
        {
            var a = Svi(Proveri(ListSaR57("2154 CD Cargo\nKombinovani prevoz d.o.o.   Subotica gr. - Bor"))).Where(n => n.Pravilo == "KP57").ToList();
            Assert.AreEqual(1, a.Count); Assert.AreEqual(Tezina.Upozorenje, a[0].Tezina); StringAssert.Contains(a[0].Tekst, "dopisati UIC 3212");
            var b = Svi(Proveri(ListSaR57("2154 CD Cargo\n3212   Subotica gr. - Bor"))).Where(n => n.Pravilo == "KP57").ToList();
            Assert.AreEqual(1, b.Count); Assert.AreEqual(Tezina.Upozorenje, b[0].Tezina); StringAssert.Contains(b[0].Tekst, "dopisati naziv");
            var lc = ListSaR57("2154 CD Cargo   Bohumin br. - Mosty br.\n3820 CDC Slovakia   Cadca br. - Sturovo br.\n   Subotica br. - Bor Teretna");
            MotorTests.BezKpUR7iR13(lc);   // v3.0.2 (3): KP u r7/r13 bi samoprovera ublažila na upozorenje
            var c = Svi(Proveri(lc)).Where(n => n.Pravilo == "KP57").ToList();
            Assert.AreEqual(1, c.Count); Assert.AreEqual(Tezina.Greska, c[0].Tezina);
            // dopuna „3212 KP“ samo uz srpsku deonicu (novi red), nikad pored strane
            Assert.AreEqual("3212 KP", c[0].Dopuna);
            Assert.IsTrue(c[0].DopunaIspod);
            Assert.AreEqual("Subotica br. - Bor Teretna", c[0].DopunaUz);
            StringAssert.Contains(Izvestaj.PrimerIzDopune(c[0], false), "uz deonicu „Subotica br. - Bor Teretna“");
        }

        [TestMethod]
        public void R57_KP_u_bilo_kom_obliku()
        {
            var kp = pravila.Kp;
            foreach (var t in new[] { "3212 KP", "KP 3212", "RS: 3212 KP", "SRB: KP UIC 3212", "3212 Kombinovani Prevoz" })
                Assert.AreEqual((true, true), Provere.KpUR57(F(new PrevozniciRubrika(), t), kp), t);
            Assert.AreEqual((true, false), Provere.KpUR57(F(new PrevozniciRubrika(), "Kombinovani prevoz d.o.o."), kp));
            Assert.AreEqual((false, true), Provere.KpUR57(F(new PrevozniciRubrika(), "3212   Subotica - Bor"), kp));
            Assert.AreEqual((false, false), Provere.KpUR57(F(new PrevozniciRubrika(), "2179 SŽ tel. 0113212555"), kp));
        }

        // ------------------------------------------------------------------------------------------- 3. ispravke 16.–17.11.

        [TestMethod]
        public void Sopstveni_PRIMER_po_metapodacima_PDF_a_se_preskace()
        {
            var b = System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj\n<</Type /Page>>\nendobj\n9 0 obj\n<</Title (Primer ispravnog tovarnog lista) /Producer (x)>>\nendobj\n%%EOF");
            Assert.IsNotNull(Raspakivanje.SopstveniPrimer(new Prilog { Naziv = "Untitled attachment 00093.pdf", Sadrzaj = b, Vrsta = VrstaPriloga.Pdf }));
            var c = System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\n<</Producer (\\376\\377\\000C\\000i\\000m\\000P\\000r\\000o\\000v\\000e\\000r\\000a)>>\n%%EOF");
            Assert.IsNotNull(Raspakivanje.SopstveniPrimer(new Prilog { Naziv = "a.pdf", Sadrzaj = c, Vrsta = VrstaPriloga.Pdf }));
            var d = System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\n<</Title (CIM) /Producer (Canon)>>\n%%EOF");
            Assert.IsNull(Raspakivanje.SopstveniPrimer(new Prilog { Naziv = "CIM.pdf", Sadrzaj = d, Vrsta = VrstaPriloga.Pdf }));
        }

        [TestMethod]
        public void Neprocitan_prilog_interna_poruka_samo_kada_mail_ili_strana_pominju_CIM()
        {
            var i = new Ishod(); i.NisuProcitani.Add("11172026Scan083933.pdf: 1 str. nije pročitano");
            Assert.IsFalse(Obrada.MozdaCim(i, "Spisak za dan 17.11. utorak", "Šaljem spisak."));
            Assert.IsFalse(Obrada.MozdaCim(i, "za NT-ove", ""));
            Assert.IsFalse(Obrada.MozdaCim(i, "Sporna dokumentacija", "HOLCIM cement"));
            Assert.IsTrue(Obrada.MozdaCim(i, "Dokumentacija", "u prilogu CIM za voz"));
            Assert.IsTrue(Obrada.MozdaCim(i, "Tovarni list za voz 262", ""));
            Assert.IsTrue(Obrada.MozdaCim(i, "x", "teretnica i Frachtbrief"));
            var j = new Ishod(); j.NisuProcitani.Add("sken.pdf: str. 1: liči na CIM/CUV obrazac, ali nije pouzdano pročitana");
            Assert.IsTrue(Obrada.MozdaCim(j, "x", ""));
        }

        [DataTestMethod]
        [DataRow("JSW KOKS SA, ul. Pawliczka 1, 41-800 Zabrze", "PL")]
        [DataRow("800 Zabrze, ul. Pawliczka 1", "PL")]
        [DataRow("KOKSOWNIA, ul. Zamkowa 9, PL6292256576", "PL")]
        [DataRow("Hlavna 5, Žilina", "SK")]
        public void Zemlja_iz_adrese_grad_postanski_broj_PDV(string adresa, string iso)
        {
            Assert.AreEqual(iso, Dopune.ZemljaIzAdrese(adresa));
        }

        [TestMethod]
        public void PRIMER_bez_izmisljenih_imena_i_kontrola_reci()
        {
            var s = new MotorTests.Scenario();
            s.C.R1 = F(new Model.Strana { Naziv = "JSW KOKS SA", Adresa = "ul. Pawliczka 1, 41-800 Zabrze", Potpis = true }, "JSW KOKS SA\nul. Pawliczka 1, 41-800 Zabrze");
            s.Rezim = "spoljni";
            var u = new UlazProvere { Dokumenti = new List<Dokument> { new Dokument { Tip = TipDokumenta.Cim, Fajl = "cim.pdf", Izvor = "tekst", Strane = { 1 }, Cim = { s.C } } }, RezimKp = "spoljni", ReferentniDatum = new DateTime(2026, 11, 17), Prag = 0.7 };
            var r = new Motor(pravila).Proveri(u);
            var r1 = Svi(r).Where(n => n.Rubrika == "1" && !string.IsNullOrEmpty(n.Dopuna)).ToList();
            Assert.IsTrue(r1.Count > 0, string.Join(" | ", Svi(r).Where(n => n.Rubrika == "1").Select(n => n.Pravilo)));
            foreach (var n in r1)
            {
                Assert.IsFalse(n.Dopuna.Contains("Marko") || n.Dopuna.Contains("John"), n.Dopuna);
                StringAssert.Contains(n.Dopuna, "PL", "zemlja iz adrese (41-800 Zabrze), ne iz r62/prevoznika");
            }
            // kontrola reči: izmišljeno ime nije ni u dokumentu ni u rečniku
            var k = new Kontekst(s.C, u.Dokumenti[0], u, pravila);
            Assert.AreEqual("Marko, Marković", string.Join(", ", Dopune.NepoznateReci(new Nalaz { Dopuna = "PL, Marko Marković" }, k)));
            Assert.AreEqual(0, Dopune.NepoznateReci(new Nalaz { Dopuna = "PL, XXXX" }, k).Count);
            Assert.AreEqual(0, Dopune.NepoznateReci(new Nalaz { Dopuna = "Platilac za deonicu SRB: XXXXX" }, k).Count);
            Assert.AreEqual(0, Dopune.NepoznateReci(new Nalaz { Dopuna = "+ Zabrze, 2026-11-13" }, k).Count, "reč iz dokumenta");
        }

        // ------------------------------------------------------------------------------------------- 3a. ponovna obrada

        [DataTestMethod]
        [DataRow("72", "162016", true)]
        [DataRow("12", "162016", false)]   // OCR „12“ umesto „72“ (gips 306)
        [DataRow("72", "162015", false)]   // pogrešna kontrolna cifra
        [DataRow("72", "143057", true)]
        [DataRow("51", "070409", true)]
        [DataRow("72", "16201", true)]     // bez kontrolne cifre – ne može da se proveri
        public void R62_kljuc_samo_kada_je_ispravna(string zemlja, string stanica, bool ispravna)
        {
            Assert.AreEqual(ispravna, PosaoIzDokumenata.R62Ispravna(new Identifikacija { Zemlja = zemlja, Stanica = stanica, Preduzece = "3212", Broj = "16390-0" }));
        }

        [TestMethod]
        public void Neispravna_r62_nije_kljuc_posla()
        {
            var c = MotorTests.IspravanCim();
            c.R62 = F(new Identifikacija { Zemlja = "12", Stanica = "162016", Preduzece = "3212", Broj = "16390-0" }, "12 16201-6 3212 16390-0");
            var k = PosaoIzDokumenata.Kljuc(c, null, 44, 1393);
            Assert.IsFalse(k.StartsWith("r62:"), k);
            StringAssert.StartsWith(k, "r62x:");
            c.R62.Zemlja = "72";
            Assert.AreEqual("r62:72-162016-3212-163900", PosaoIzDokumenata.Kljuc(c, null, 44, 1393));
        }

        [TestMethod]
        public void ID_iz_naslova_maila()
        {
            CollectionAssert.AreEquivalent(new[] { "1602", "1496" }, UpisUBazu.IdIzNaslova("FW: Dokumentacija za predaju ID 1602 + 1496").ToList());
            CollectionAssert.AreEquivalent(new[] { "1657" }, UpisUBazu.IdIzNaslova("Dokumentacija za predaju ID 1657 Bor teretna - Subotica gr").ToList());
            CollectionAssert.AreEquivalent(new[] { "1654" }, UpisUBazu.IdIzNaslova("Dokumentacija za prijem ID1654").ToList());
            Assert.AreEqual(0, UpisUBazu.IdIzNaslova("Re: Lukavac02 (NHM 2836)").Count);
        }

        private static KolaNaSpisku K(string broj, double? tara, double? neto, double? bruto, string nhm = "") => new KolaNaSpisku { Broj = broj, TaraKg = tara, NetoKg = neto, BrutoKg = bruto, Nhm = nhm };

        [TestMethod]
        public void Spisak_dve_kolone_sa_istim_zbirom_se_ne_upisuju()
        {
            var pk = new Poklapanje(UpisUBazuTests.Sif());
            // natovarena kola: tara = bruto (bruto kolona pročitana dvaput) – ni tara ni bruto
            var s = new SpisakKola(); s.Kola.Add(K("348078400028", 83650, null, 83650, "151211")); s.Kola.Add(K("348078400044", 83600, null, 83600, "151211"));
            var p = PosaoIzDokumenata.IzSpiska(s, pk, "", "");
            Assert.IsFalse(p.V.ContainsKey("Tara1") && p.V["Tara1"] != null, "tara = bruto → nije upisana");
            Assert.IsTrue(p.Napomene.Any(n => n.Contains("proveriti kolone spiska")), string.Join(" | ", p.Napomene));
            // prazna kola: tara = bruto je ispravno
            var e = new SpisakKola(); e.Kola.Add(K("348078400028", 21750, 0, 21750, "992200")); e.Kola.Add(K("348078400044", 21700, 0, 21700, "992200"));
            var pe = PosaoIzDokumenata.IzSpiska(e, pk, "", "");
            Assert.AreEqual(43.45m, pe.V["Tara1"]);
        }

        [TestMethod]
        public void Spisak_kome_fali_tara_za_neka_kola_napomena()
        {
            var s = new SpisakKola();
            s.Kola.Add(K("335649932019", null, 36000, null)); s.Kola.Add(K("335649932027", 22000, 36000, null)); s.Kola.Add(K("335649932035", 22000, 36000, null));
            var p = PosaoIzDokumenata.IzSpiska(s, new Poklapanje(UpisUBazuTests.Sif()), "", "");
            Assert.IsTrue(p.Napomene.Contains("Spisak kola: nedostaje tara: kola 3356 4993 201-9"), string.Join(" | ", p.Napomene));
        }

        [TestMethod]
        public void Natovaren_spisak_ne_ide_u_teretnicu_praznih_istih_kola()
        {
            var nat = new SpisakKola(); nat.Kola.Add(K("348078400028", 21750, 61900, 83650));
            var pr = new SpisakKola(); pr.Kola.Add(K("348078400028", 21750, 0, 21750)); pr.Kola[0].Prazna = true;
            var bez = new SpisakKola(); bez.Kola.Add(K("348078400028", null, null, null));
            Assert.AreEqual(true, UpisUBazu.Natovaren(nat));
            Assert.AreEqual(false, UpisUBazu.Natovaren(pr));
            Assert.IsNull(UpisUBazu.Natovaren(bez));
        }

        [TestMethod]
        public void Grupa_bez_kola_sa_masama_grupe_1_se_ne_pravi()
        {
            // voz 005NISBAR26 (286): CUV bez kola sa istim masama kao CIM nije grupa 2
            var cim = new ZapisTl { Kljuc = "a", Obrazac = "CIM", Nhm4 = "2502", Neto = 1301240, Tara = 459500, Bruto = 1760740 };
            var cuv = new ZapisTl { Kljuc = "b", Obrazac = "CUV", Prazna = true, Neto = 1301240, Tara = 459500, Bruto = 1760740 };
            var g = UpisUBazu.Grupe(new[] { cim, cuv });
            Assert.AreEqual(1, g.Count);
            // sa sopstvenim masama i kolima grupa 2 ostaje
            var prazna = new ZapisTl { Kljuc = "c", Obrazac = "CUV", Prazna = true, BrojKola = 5, Tara = 115000, Kola = { "315653770018" } };
            Assert.AreEqual(2, UpisUBazu.Grupe(new[] { cim, prazna }).Count);
        }

        private static ListPosla Lp(int? s1, int? s3, int? otp, int? up, string nhm4, double? neto, double? tara = null, double? bruto = null, int bk = 0, params string[] kola)
        {
            var p = new PodaciPosla();
            p.Postavi("Stanica1ID", s1, "", ""); p.Postavi("Stanica3ID", s3, "", ""); p.Postavi("OtpravnaID", otp, "", ""); p.Postavi("UputnaID", up, "", "");
            p.Postavi("BrojKola1", bk, "", "");
            p.Nhm4 = nhm4; p.NetoKg = neto; p.TaraKg = tara; p.BrutoKg = bruto; p.Kola = kola.ToList(); p.KljucPosiljke = "k" + Guid.NewGuid().ToString("N");
            return new ListPosla { P = p };
        }

        [DataTestMethod]
        [DataRow("1.848,150", 1848150.0)]   // LDC8 „Netto: 1.848,150“ – tone sa tri decimale
        [DataRow("2.499,750", 2499750.0)]
        [DataRow("651,600", 651600.0)]
        [DataRow("2.090,52", 2090.52)]
        [DataRow("1627,580 t", 1627580.0)]
        public void Masa_tone_sa_tri_decimale(string tekst, double kg)
        {
            Assert.AreEqual(kg, PosaoIzDokumenata.Kg(tekst, true).Value, 0.01, tekst);
        }

        [TestMethod]
        public void Kopije_lista_sa_jednom_masom_su_isti_list()
        {
            // Prahovo 05: četiri kopije skena sa samo netom 924,4 t – jedan list (mase se ne sabiraju)
            var a = new ZapisTl { Kljuc = "a", Nhm4 = "2702", Neto = 924400 };
            var b = new ZapisTl { Kljuc = "b", Nhm4 = "2702", Neto = 924400 };
            var c = new ZapisTl { Kljuc = "c", Nhm4 = "2702", Neto = 1181500 };
            Assert.IsTrue(UpisUBazu.IstiZapis(a, b));
            Assert.IsFalse(UpisUBazu.IstiZapis(a, c));
            Assert.IsFalse(UpisUBazu.IstiZapis(a, new ZapisTl { Kljuc = "d", Nhm4 = "2701", Neto = 924400 }), "druga roba");
        }

        [TestMethod]
        public void Vozovi_CUV_bez_kola_sa_masama_CIM_i_listovi_bez_deonice()
        {
            // 005NISBAR26: CUV bez kola sa istim masama kao CIM – isti voz
            var cim = Lp(225, 1365, 225, 26, "2502", 1301240, 459500, 1760740, 18);
            var cuv = Lp(224, null, 224, 26, "", 1301240, 459500, 1760740, 0);
            Assert.AreEqual(1, UpisUBazu.Vozovi(new List<ListPosla> { cim, cuv }).Count);
            // „12 е въглища.pdf“: listovi bez obe tačke deonice idu u voz iste robe; drugi voz (druga roba) ostaje poseban
            var v1 = Lp(118, 369, 1083, 369, "2702", 50000, null, null, 1, "317553796465");
            var v2 = Lp(118, 369, 1083, 369, "2702", 50000, null, null, 1, "317553796473");
            var ibc = Lp(118, 381, 1083, 380, "3824", 4000, null, null, 1, "317553796481");
            var bez = Lp(null, null, null, 370, "2702", 50000, null, null, 1, "317553796499");
            var vozovi = UpisUBazu.Vozovi(new List<ListPosla> { v1, v2, ibc, bez });
            Assert.AreEqual(2, vozovi.Count);
            Assert.IsTrue(vozovi.Single(v => v.Listovi.Contains(v1)).Listovi.Contains(bez));
        }

        [TestMethod]
        public void Budamar_sinonimi_najduzi_prvi()
        {
            var pk = new Poklapanje(UpisUBazuTests.Sif());
            Assert.AreEqual(3355, pk.Partner("Budamar South, s.r.o.").Id);
            Assert.AreEqual(3355, pk.Partner("BUDAMAR SOUTH s.r.o.").Id);
            Assert.AreEqual(3143, pk.Partner("Budamar South").Id);
            Assert.AreEqual(3143, pk.Partner("BUDAMAR").Id);
            Assert.AreEqual(3143, pk.Partner("Budamar South d.o.o. Beograd").Id);
            Assert.AreEqual(1580, pk.Partner("Budamar Logistics a.s.").Id);
            StringAssert.Contains(pk.Partner("Budamar South, s.r.o.").Napomena, "BUDAMAR SOUTH SRO");
        }

        [DataTestMethod]
        [DataRow("Budamar South, s.r.o.", new[] { "SK", "CZ" })]
        [DataRow("Kombinovani prevoz d.o.o.", new[] { "SRB", "HR", "SI", "BA" })]
        [DataRow("CER Hungary Zrt.", new[] { "HU" })]
        [DataRow("VTG Rail Logistics Austria GmbH", new[] { "DE", "AT" })]
        [DataRow("CDC Polska Sp. z o.o.", new[] { "PL" })]
        [DataRow("PKP Cargo S.A.", new[] { "PL" })]
        [DataRow("PSP Cargo S.R.L.", new[] { "RO", "IT" })]
        [DataRow("Cinkoturk Lojistik A.Ş.", new[] { "TR" })]
        public void Pravna_forma_kao_znak_zemlje(string naziv, string[] zemlje)
        {
            CollectionAssert.AreEqual(zemlje, Slicnost.DrzaveIzPravnogOblika(naziv));
        }

        [TestMethod]
        public void Sifra_stanice_sa_kosom_crtom()
        {
            Assert.AreEqual("72126011", Poklapanje.SifraIzTeksta("72/12601 1 671"));
            Assert.AreEqual("7212601", Poklapanje.SifraIzTeksta("72/12601"));
            Assert.AreEqual("72165167", Poklapanje.SifraIzTeksta("Šid 72 16516-7"));
            Assert.IsNull(Poklapanje.SifraIzTeksta("tel. 0113212555"));
            Assert.IsNull(Poklapanje.SifraIzTeksta("24/12345"), "24 nije UIC kod zemlje iz liste");
            var s = UpisUBazuTests.Sif(); s.Stanice.Add(new StanicaZapis { Id = 225, Opis = "Niš ranžirna", Drzava = "SRB", Kod = "126011" }); s.Pripremi();
            Assert.AreEqual(225, new Poklapanje(s).Stanica("72/12601 1 671").Id);
        }
    }
}
