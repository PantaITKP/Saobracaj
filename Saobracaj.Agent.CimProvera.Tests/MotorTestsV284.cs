using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>v2.8.4 (4, 5, 7): lažne greške u r58a i r1/r4, r29, X19, dopune samo iz pouzdano pročitanih rubrika.</summary>
    public partial class MotorTests
    {
        // ------------------------------------------------------------------------------------------- 4.1 r58a

        [DataTestMethod]
        [DataRow("UIC: 3212")]
        [DataRow("UIC 3212")]
        [DataRow("UIC CODE KP 3212")]
        [DataRow("KP(3212)")]
        [DataRow("Kombinovani Prevoz\nUIC: 3212")]
        public void T4_R58a_UIC_oblici_su_popunjena_rubrika(string tekst)
        {
            foreach (var r62 in new[] { "3212", "3127" })
            {
                var s = new Scenario { Rezim = "auto" };
                s.C.R62.Preduzece = r62;
                s.C.R58a = F(new UgovorniPrevoznik(), tekst);
                var r = Proveri(s);
                var los = Svi(r).FirstOrDefault(n => n.Rubrika == "58a" && n.Tezina == Tezina.Greska);
                Assert.IsNull(los, "r62 " + r62 + ", r58a „" + tekst + "“: " + los?.Pravilo + " " + los?.Detalj);
            }
        }

        // ------------------------------------------------------------------------------------------- 4.2/4.3 zemlja

        [TestMethod]
        public void T4_Zemlja_na_kraju_reda_posle_mesta_je_zemlja()
        {
            // „CIM SAD HER 79“ r1: sve u jednom redu
            var st = new Model.Strana { Tekst = "DP World Serbia   Carinska 1 Novi Sad   Serbia", Naziv = "DP World Serbia Carinska 1 Novi Sad Serbia", NazivZemlje = "Serbia", Status = StatusRubrike.Popunjeno };
            Assert.IsTrue(Provere.ZemljaUAdresi(st), Provere.AdresaBezNaziva(st));
            // zemlja samo u nazivu firme i dalje nije zemlja (v2.8.2)
            var ld = new Model.Strana { Tekst = "Louis Dreyfus Company Hungary Kft.\nVáci út 1, 1132 Budapest", Naziv = "Louis Dreyfus Company Hungary Kft.", NazivZemlje = "Hungary", Status = StatusRubrike.Popunjeno };
            Assert.IsFalse(Provere.ZemljaUAdresi(ld));
            var ld1 = new Model.Strana { Tekst = "Louis Dreyfus Company Hungary Kft. Váci út 1 1132 Budapest", Naziv = "Louis Dreyfus Company Hungary Kft. Váci út 1 1132 Budapest", NazivZemlje = "Hungary", Status = StatusRubrike.Popunjeno };
            Assert.IsFalse(Provere.ZemljaUAdresi(ld1), Provere.AdresaBezNaziva(ld1));
        }

        [DataTestMethod]
        [DataRow("Italy", "IT")] [DataRow("Italia", "IT")] [DataRow("Germany", "DE")] [DataRow("Deutschland", "DE")] [DataRow("Srbija", "RS")] [DataRow("Serbia", "RS")]
        public void T4_R4_zemlja_recima_je_napomena_a_ne_greska(string zemlja, string iso)
        {
            var s = new Scenario();
            var tekst = "Borsari E&C SRL\nVia Roma 1\n36043 Camisano Vicentino (VI), " + zemlja;
            s.C.R4 = F(new Model.Strana { Naziv = "Borsari E&C SRL", Adresa = "Via Roma 1, 36043 Camisano Vicentino (VI), " + zemlja, SifraZemlje = "", NazivZemlje = zemlja, Email = "a@b.it" }, tekst);
            var r = Proveri(s);
            Assert.IsFalse(Svi(r).Any(n => n.Rubrika == "4" && n.Tezina == Tezina.Greska && (n.Detalj ?? "").Contains("zemlja")), string.Join(" | ", Svi(r).Where(n => n.Rubrika == "4").Select(n => n.Pravilo + " " + n.Detalj)));
            var n4 = Svi(r).FirstOrDefault(n => n.Pravilo == "R4-ISO");
            Assert.IsNotNull(n4, "napomena „" + zemlja + " → " + iso + "“");
            Assert.AreNotEqual(Tezina.Greska, n4.Tezina);
            StringAssert.Contains(n4.Detalj + " " + n4.Primer, iso);
        }

        /// <summary>CIM (Pansped) r4: OCR „Italy“ → „Hay“; uputna u Italiji (r10 ITALY, r12 83…) → napomena „proverite zemlju“, ne greška.</summary>
        [TestMethod]
        public void T4_R4_nepoznata_rec_na_kraju_adrese_uputna_u_inostranstvu_je_napomena()
        {
            var s = new Scenario();
            var tekst = "CEREAL DOCKS S.P.A.\nVia dell Innovazione 4\n36043 Camisano Vicenlino (VI), Hay\n39 044 419411";
            s.C.R4 = F(new Model.Strana { Naziv = "CEREAL DOCKS S.P.A.", Adresa = "Via dell Innovazione 4, 36043 Camisano Vicenlino (VI), Hay", SifraZemlje = "", NazivZemlje = "", Email = "a@b.it" }, tekst);
            s.C.R10 = F(new MestoIsporuke { Stanica = "PORTOGRUARO CAORLE", Zemlja = "ITALY" });
            s.C.R12 = F(new TekstRubrika(), "83032003");
            var r = Proveri(s);
            Assert.IsFalse(Svi(r).Any(n => n.Rubrika == "4" && n.Tezina == Tezina.Greska), string.Join(" | ", Svi(r).Where(n => n.Rubrika == "4").Select(n => n.Pravilo + " " + n.Tezina + " " + n.Detalj)));
            var n4 = Svi(r).Single(n => n.Rubrika == "4" && n.Pravilo == "R4");
            Assert.AreEqual(Tezina.Upozorenje, n4.Tezina);
            StringAssert.Contains(n4.Tekst + " " + n4.Uputstvo, "Proverite zemlju (prema r10/r12: IT)");
            // uputna u Srbiji ili bez reči na kraju adrese: i dalje greška „nedostaje zemlja“
            var s2 = new Scenario();
            s2.C.R4 = F(new Model.Strana { Naziv = "Primalac AD", Adresa = "Ulica 2, 1000 Nepoznatovo", SifraZemlje = "", NazivZemlje = "", Email = "a@b.c" }, "Primalac AD\nUlica 2\n1000 Nepoznatovo");
            Assert.IsTrue(Svi(Proveri(s2)).Any(n => n.Rubrika == "4" && n.Tezina == Tezina.Greska && (n.Detalj ?? "").Contains("zemlja")));
        }

        // ------------------------------------------------------------------------------------------- r62 rukom upisana (YU DIN COMMERCE)

        [TestMethod]
        public void R62_nepouzdana_na_skenu_ne_odredjuje_KP_ponavljanje_ni_dosije()
        {
            foreach (var (pouzdanost, kp) in new[] { (0.78, false), (0.95, true) })
            {
                var s = new Scenario { Rezim = "auto", IzvorCim = "ocr" };
                s.C.PouzdanostStrane = 0.80;
                s.C.R62 = F(new Identifikacija { Zemlja = "72", Stanica = "165092", Preduzece = "3212", Broj = "162600" });
                s.C.R62.PouzdanostCifara = pouzdanost;
                var r = Proveri(s);
                var l = r.Listovi.Single();
                Assert.AreEqual(kp, l.KpRezim, "r62 pouzdanost " + pouzdanost);
                var n = Svi(r).FirstOrDefault(x => x.Pravilo == "R62-NECITLJIVA");
                if (kp) { Assert.IsNull(n); StringAssert.StartsWith(Ponavljanja.KljucLista(l.Cim), "r62:"); continue; }
                Assert.IsNotNull(n, "r62 nečitljiva – ručna provera");
                Assert.IsTrue(n.SamoInterno);
                StringAssert.StartsWith(n.Tekst, "r62 nečitljiva – ručna provera");
                Assert.IsTrue(l.ZaRucnuProveru);
                var kljuc = Ponavljanja.KljucLista(l.Cim);
                Assert.IsTrue(kljuc == null || !kljuc.StartsWith("r62:"), "ponavljanje/dosije bez nepouzdane r62: " + kljuc);
            }
        }

        // ------------------------------------------------------------------------------------------- 11. interni izveštaji

        [TestMethod]
        public void T11_Nepouzdano_procitane_rubrike_u_jednom_redu()
        {
            var s = new Scenario { IzvorCim = "ocr" };
            s.C.PouzdanostStrane = 0.70;
            s.C.R1 = new Model.Strana(); s.C.R4 = new Model.Strana(); s.C.R29 = new MestoDatum(); s.C.R16 = new Preuzimanje();
            var r = Proveri(s);
            var n = Svi(r).Where(x => x.Pravilo == "NEPOUZDANO-OCR").ToList();
            Assert.AreEqual(1, n.Count, string.Join(" | ", Svi(r).Select(x => x.Pravilo + " r" + x.Rubrika)));
            StringAssert.StartsWith(n[0].Tekst, "Nepouzdano pročitane rubrike (OCR 0,70): 1, 4, ");
            StringAssert.EndsWith(n[0].Tekst, " – proveriti ručno.");
            StringAssert.Contains(n[0].Tekst, "29");
            Assert.IsTrue(n[0].SamoInterno);
            Assert.IsFalse(Svi(r).Any(x => (x.Kljuc ?? "").StartsWith("rucno|")), "nema posebnih redova po rubrici");
        }

        [TestMethod]
        public void T11_KP_interni_izvestaj_bez_recenice_za_posiljaoca()
        {
            var pr = new PrimerRezultat { Ime = "PRIMER_x.pdf", Pdf = new byte[] { 1 } };
            if (!pr.Napravljen) Assert.Inconclusive("PrimerRezultat.Napravljen zavisi od drugih polja");
            Assert.AreEqual("Primer ispravke je u prilogu.", Izvestaj.PasusPrimera(pr, false, true));
            StringAssert.Contains(Izvestaj.PasusPrimera(pr, false), "ispravan tovarni list izdaje i potpisuje pošiljalac");
        }

        // ------------------------------------------------------------------------------------------- 9. ko šta popunjava (GLV-CIM)

        // Odluka KP 2026-11-14: r57 i r58a popunjava pošiljalac; r62 pošiljalac ako zna (prazan/nepotpun = napomena);
        // r49 i r50 prevoznik – samo interno; ostale rubrike 1–30 pošiljalac.
        private const string OdlukaKp = "interna odluka KP 2026-11-14";

        [TestMethod]
        public void T9_Svako_pravilo_ima_popunjava_i_osnov()
        {
            var bez = pravila.Sva.Where(d => d.Popunjava == null && d.Json["ne_proverava_se"] == null).Select(d => d.Id).OrderBy(x => x).ToList();
            Assert.AreEqual(0, bez.Count, "bez „popunjava“: " + string.Join(", ", bez));
            foreach (var d in pravila.Sva.Where(d => d.Popunjava != null))
            {
                Assert.IsTrue(d.Popunjava == "pošiljalac" || d.Popunjava == "prevoznik", d.Id + ": " + d.Popunjava);
                var osnov = (string)d.Json["popunjava_basis"] ?? "";
                Assert.IsTrue(osnov.StartsWith("GLV-CIM") || osnov.StartsWith(OdlukaKp), d.Id + ": " + osnov);
            }
            foreach (var id in new[] { "R57", "R57-DEONICA", "REC4", "KP57", "KP5", "R58A", "KP2" })
            {
                Assert.AreEqual("pošiljalac", pravila.PoId[id].Popunjava, id);
                StringAssert.StartsWith((string)pravila.PoId[id].Json["popunjava_basis"], OdlukaKp + " – pošiljalac popunjava r57 i r58a", id);
            }
            foreach (var id in new[] { "R62", "X19" })
            {
                Assert.AreEqual("pošiljalac", pravila.PoId[id].Popunjava, id);
                StringAssert.StartsWith((string)pravila.PoId[id].Json["popunjava_basis"], OdlukaKp + " – r62 pošiljalac upisuje ako ga zna", id);
            }
            foreach (var id in new[] { "R49", "R50" })
            {
                Assert.AreEqual("prevoznik", pravila.PoId[id].Popunjava, id);
                Assert.IsTrue(pravila.PoId[id].PopunjavaPrevoznik, id);
            }
            foreach (var id in new[] { "R1", "R7", "R16", "R24", "R29", "KP3", "KP7-PLATILAC" }) Assert.AreEqual("pošiljalac", pravila.PoId[id].Popunjava, id);
            Assert.AreEqual(2, pravila.Sva.Count(d => d.Popunjava == "prevoznik"), "prevoznik popunjava samo r49 i r50");
        }

        [TestMethod]
        public void T9_KP57_kod_spoljnog_posiljaoca_je_greska_posiljaoca()
        {
            var s = new Scenario { Rezim = "spoljni" };
            s.C.R57.Prevoznici.Clear();
            s.C.R57.Prevoznici.Add(new Prevoznik { Sifra = "3127", Naziv = "HSL Logistik GmbH", Deonica = "Herne - Kelebia", StatusPrevoznika = "1" });
            s.C.R58a = F(new UgovorniPrevoznik { Sifra = "3127", Naziv = "HSL" }, "3127 HSL");
            var r = Proveri(s);
            var kp57 = Svi(r).FirstOrDefault(n => n.Pravilo == "KP57");
            Assert.IsNotNull(kp57, string.Join(" | ", Svi(r).Select(n => n.Pravilo)));
            Assert.AreEqual(Tezina.Greska, kp57.Tezina);
            Assert.IsFalse(kp57.SamoInterno, "r57 popunjava pošiljalac – u izveštaju za pošiljaoca");
            Assert.IsFalse(kp57.Tekst.Contains("popunjava prevoznik"), kp57.Tekst);
            Assert.IsTrue(Svi(r.BezInternih()).Any(n => n.Pravilo == "KP57"));
        }

        [TestMethod]
        public void T9_R58A_kod_spoljnog_posiljaoca_je_greska_posiljaoca()
        {
            var s = new Scenario { Rezim = "auto" };
            s.C.R62.Preduzece = "2154";
            s.C.R58a = new UgovorniPrevoznik();
            var r = Proveri(s);
            Assert.IsFalse(r.Listovi.Single().KpRezim);
            var n = Svi(r).FirstOrDefault(x => x.Pravilo == "R58A");
            Assert.IsNotNull(n, string.Join(" | ", Svi(r).Select(x => x.Pravilo)));
            Assert.AreEqual(Tezina.Greska, n.Tezina);
            Assert.IsFalse(n.SamoInterno);
            Assert.IsTrue(Svi(r.BezInternih()).Any(x => x.Pravilo == "R58A"));
        }

        [TestMethod]
        public void T9_Prazan_ili_nepotpun_r62_je_napomena_za_spoljnog_posiljaoca()
        {
            foreach (var r62 in new[] { new Identifikacija(), F(new Identifikacija { Zemlja = "80", Stanica = "101287", Preduzece = "2154", Broj = "" }) })
            {
                var s = new Scenario { Rezim = "spoljni" };
                s.C.R62 = r62;
                var r = Proveri(s);
                var n = Svi(r).Where(x => x.Rubrika == "62" && x.Tezina != Tezina.Info).ToList();
                Assert.IsTrue(n.Count > 0, "r62 „" + r62.Tekst + "“: " + string.Join(" | ", Svi(r).Select(x => x.Pravilo)));
                Assert.IsTrue(n.All(x => x.Tezina == Tezina.Upozorenje), string.Join(" | ", n.Select(x => x.Pravilo + " " + x.Tezina)));
                Assert.IsTrue(Svi(r.BezInternih()).Any(x => x.Pravilo == "R62" && x.Tezina == Tezina.Upozorenje), "napomena ide pošiljaocu");
            }
            // KP tovarni list (r62 = 3212): interni nalaz kao do sada – greška
            var kp = new Scenario();
            kp.C.R62.Broj = "";
            var rk = Proveri(kp);
            Assert.IsTrue(rk.Listovi.Single().KpRezim);
            Assert.AreEqual(Tezina.Greska, Prvi(rk, "R62").Tezina);
        }

        [TestMethod]
        public void T9_R49_i_R50_su_samo_interni()
        {
            var s = new Scenario { Rezim = "spoljni" };
            s.C.R49 = new TekstRubrika(); s.C.R50 = new TekstRubrika();
            var r = Proveri(s);
            foreach (var id in new[] { "R49", "R50" })
            {
                var n = Svi(r).FirstOrDefault(x => x.Pravilo == id);
                Assert.IsNotNull(n, id + ": " + string.Join(" | ", Svi(r).Select(x => x.Pravilo)));
                Assert.IsTrue(n.SamoInterno, id);
                StringAssert.EndsWith(n.Tekst, "– popunjava prevoznik (KP pri prijemu)");
                Assert.IsFalse(Svi(r.BezInternih()).Any(x => x.Pravilo == id), id + " ne ide pošiljaocu");
            }
            // rubrike pošiljaoca i dalje idu pošiljaocu
            var s2 = new Scenario { Rezim = "spoljni" }; s2.C.R16 = new Preuzimanje();
            Assert.IsTrue(Svi(Proveri(s2)).Any(n => n.Rubrika == "16" && !n.SamoInterno));
        }

        /// <summary>Naslov kada je r62 nečitljiva (YU DIN COMMERCE, 1722_261114123020_001.pdf): ime fajla umesto pročitanog broja.</summary>
        [TestMethod]
        public void T9_Naslov_bez_nepouzdane_r62()
        {
            var s = new Scenario { Rezim = "auto", IzvorCim = "ocr", Fajl = "1722_261114123020_001.pdf" };
            s.C.PouzdanostStrane = 0.80;
            s.C.R62 = F(new Identifikacija { Zemlja = "72", Stanica = "105022", Preduzece = "3242", Broj = "62600" });
            s.C.R62.PouzdanostCifara = 0.70;
            s.C.R8 = new TekstRubrika();   // kao na skenu YU DIN COMMERCE (r8 bez reference)
            var r = Proveri(s);
            // sa napomenom (prazna r8) status ostaje i dodaje se „r62 nečitljiva“
            Assert.AreEqual("[INTERNO] Ručna provera – Tovarni list CIM (1722_261114123020_001.pdf) – napomene, r62 nečitljiva", Izvestaj.Naslov(r, pravila, false));
            // kao YU DIN COMMERCE: bez grešaka i napomena – samo „r62 nečitljiva“
            foreach (var l in r.Listovi) l.Nalazi.RemoveAll(x => x.Tezina != Tezina.Info);
            var n = Izvestaj.Naslov(r, pravila, false);
            Assert.AreEqual("[INTERNO] Ručna provera – Tovarni list CIM (1722_261114123020_001.pdf) – r62 nečitljiva", n);
            Assert.IsFalse(n.Contains("3242"), n);
            StringAssert.EndsWith(Izvestaj.Naslov(r, pravila, true), "(1722_261114123020_001.pdf) – box 62 unreadable");
        }

        /// <summary>CIM Pansped: izveštaj za pošiljaoca sa nepouzdanom r62 – bez pročitanog broja, ali status ostaje „potrebne ispravke“.</summary>
        [TestMethod]
        public void T9_Naslov_za_posiljaoca_sa_nepouzdanom_r62_zadrzava_status()
        {
            var s = new Scenario { Rezim = "spoljni", IzvorCim = "ocr", Fajl = "20261114081659.pdf" };
            s.C.PouzdanostStrane = 0.80;
            s.C.R8 = new TekstRubrika();
            s.C.R62 = F(new Identifikacija { Zemlja = "72", Stanica = "105022", Preduzece = "2154", Broj = "" });
            s.C.R62.NepouzdanoCitanje = true;
            s.C.R16 = new Preuzimanje();
            var r = Proveri(s);
            Assert.IsTrue(r.Listovi.Single().Cim.R62.NepouzdanaZaOdluke);
            var n = Izvestaj.Naslov(r, pravila, false);
            if (n.StartsWith("[INTERNO]")) Assert.Inconclusive("scenario je završio na ručnoj proveri: " + n);
            Assert.AreEqual("Tovarni list CIM (20261114081659.pdf) – potrebne ispravke", n);
        }

        /// <summary>v2.8.4 (8.2): dokumenti iz dosijea važe samo za priloženost (R18-SPISAK, X22, X26/X27), ne za proveru sadržaja (X19…).</summary>
        [TestMethod]
        public void T8_Spisak_iz_dosijea_vazi_samo_za_prilozenost()
        {
            var s = new Scenario();
            s.C.R18 = F(new ListaRubrika(), "Vidi spisak kola CIT 23");
            Assert.IsTrue(Ima(Proveri(s), "R18-SPISAK"), "bez spiska u mailu");
            var d = Spisak(Kola("317866560015")); d.Spisak[0].R62.Preduzece = "3121"; d.IzDosijea = true; d.IzMaila = "UID 277";
            s.Ostali.Add(d);
            var r = Proveri(s);
            Assert.IsFalse(Ima(r, "R18-SPISAK"), "spisak iz dosijea je priložen");
            Assert.IsFalse(Ima(r, "X19"), "sadržaj spiska iz drugog maila se ne poredi sa r62");
        }

        // ------------------------------------------------------------------------------------------- 4.4 r29

        [DataTestMethod]
        [DataRow("Lukavac, 2026.10.26", "2026-10-26")]
        [DataRow("2026.10.26", "2026-10-26")]
        [DataRow("Novi Sad 13.11.2026.", "2026-11-13")]
        [DataRow("13.11.2026", "2026-11-13")]
        [DataRow("2026-11-13", "2026-11-13")]
        public void T4_R29_datum(string tekst, string datum) => Assert.AreEqual(datum, CimParser.NadjiDatum(tekst, out _, out _));

        // ------------------------------------------------------------------------------------------- 5. X19

        [TestMethod]
        public void T5_X19_identifikacija_nije_upisana_na_spisku_je_napomena()
        {
            var s = new Scenario();
            var d = Spisak(Kola("317866560015"));
            d.Spisak[0].R62 = new Identifikacija();
            d.Izvor = "ocr"; d.Fajl = "wagon list 16 - complet.pdf";
            s.Ostali.Add(d);
            var n = Svi(Proveri(s)).FirstOrDefault(x => x.Pravilo == "X19");
            Assert.IsNotNull(n);
            Assert.AreEqual(Tezina.Upozorenje, n.Tezina, "nije neslaganje – najviše napomena");
            StringAssert.Contains(n.Tekst + " " + n.Uputstvo, "Na spisku kola nije upisana identifikacija pošiljke (r62)");
            Assert.IsFalse((n.Tekst + n.Uputstvo).Contains("ne odgovara"));
        }

        [TestMethod]
        public void T5_X19_razlicita_identifikacija_i_dalje_greska()
        {
            var s = new Scenario();
            var d = Spisak(Kola("317866560015")); d.Spisak[0].R62.Preduzece = "3121"; s.Ostali.Add(d);
            Assert.AreEqual(Tezina.Greska, Prvi(Proveri(s), "X19").Tezina);
        }

        // ------------------------------------------------------------------------------------------- 7. dopune iz šuma

        [DataTestMethod]
        [DataRow("en jour—heure Nord j, 2026-11-03", true)]   // SKM r29
        [DataRow("jj 20", true)]                              // Lukavac r1
        [DataRow("jjjen. 20", true)]
        [DataRow("46/26; 93/26; 044j202j6;ı 03/26", true)]    // r7
        [DataRow("Novi Sаd", true)]                           // „а“ ćirilično u latiničnoj reči
        [DataRow("44653 Herne", false)]
        [DataRow("Camisano Vicentino (VI), Italy", false)]
        [DataRow("46/26; 93/26; 044/2026; 03/26", false)]
        [DataRow("Kombinovani prevoz d.o.o., UIC 3212", false)]
        [DataRow("Novi Sad ranžirna, Serbia", false)]
        [DataRow("Šid, 14.11.2026", false)]
        public void T7_Sum_OCR(string tekst, bool sum) => Assert.AreEqual(sum, Dopune.SumOcr(tekst), tekst);

        [TestMethod]
        public void T7_Sidro_posle_nije_od_suma()
        {
            var n = new Nalaz { Pravilo = "R1", Rubrika = "1", Tezina = Tezina.Greska, Dopuna = "RS", Vrednost = "Lukavac d.o.o. / jj 20" };
            Assert.AreEqual("+ RS", Izvestaj.PrimerIzDopune(n, false));
            n.Vrednost = "Lukavac d.o.o. / Lukavac 75300";
            Assert.AreEqual("+ RS (posle „Lukavac 75300“)", Izvestaj.PrimerIzDopune(n, false));
            // dopuna sa šumom se ne upisuje
            Assert.AreEqual("", Dopune.Ocisti("en jour—heure Nord j, 2026-11-03"));
        }

        /// <summary>Svi kvarovi: nijedna dopuna nema mešano pismo, „jj“ ni kratke reči kojih nema u rečniku (v2.8.4 test iz tačke 7.2).</summary>
        [TestMethod]
        public void T7_Dopune_nemaju_sum()
        {
            foreach (var id in Kvarovi.Keys)
            {
                var s = new Scenario();
                Kvarovi[id](s);
                foreach (var n in Svi(Proveri(s)))
                    foreach (var d in DelociDopune(n))
                        Assert.IsFalse(Dopune.SumOcr(d), id + " → " + n.Pravilo + " r" + n.Rubrika + ": „" + d + "“");
            }
        }
    }
}
