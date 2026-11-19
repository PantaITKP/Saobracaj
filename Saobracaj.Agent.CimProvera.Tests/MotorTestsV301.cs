using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>v3.0.1 – odluke korisnika 18.11. (R16, R1/R4) i lažne greške prema pošiljaocu (R30, nečitljiva polja, samoprovera).</summary>
    public partial class MotorTests
    {
        private static string Opis(RezultatProvere r) => string.Join(" | ", Svi(r));

        /// <summary>v3.0.2: scenario za testove van klase MotorTests (V302Tests).</summary>
        internal sealed class ScenarioJavni
        {
            public CimList C = IspravanCim();
            public string Rezim = "kp";
            public string Fajl = "cim.pdf";
            public string IzvorCim = "tekst";
            public RezultatProvere Proveri()
            {
                if (pravila == null) Init(null);
                return MotorTests.Proveri(new Scenario { C = C, Rezim = Rezim, Fajl = Fajl, IzvorCim = IzvorCim });
            }
        }

        internal static T Popuni<T>(T r, string tekst = null) where T : Rubrika => F(r, tekst);

        // ------------------------------------------------------------------------------------------- 1.1 R16 – datum i vreme preuzimanja

        private static Scenario BezDatumaR16(string izvor, double mastilo58a)
        {
            var s = new Scenario { IzvorCim = izvor };
            s.C.R16.Datum = ""; s.C.R16.Cas = "";
            s.C.MastiloR58a = mastilo58a;
            return s;
        }

        [TestMethod]
        public void R16_radna_verzija_bez_pecata_datum_i_vreme_su_upozorenje_sa_napomenom()
        {
            var r = Proveri(BezDatumaR16("tekst", -1));
            Assert.IsFalse(Svi(r).Any(n => n.Pravilo == "R16" && n.Tezina == Tezina.Greska), Opis(r));
            var n0 = Svi(r).Single(n => n.Pravilo == "R16");
            Assert.AreEqual(Tezina.Upozorenje, n0.Tezina);
            StringAssert.Contains(n0.Tekst, "Posle overe (pečat prevoznika) nedostatak datuma i vremena smatraće se greškom.");
            StringAssert.Contains(n0.Tekst, "datum");
            Assert.IsFalse(Svi(r).Any(n => (n.Tekst ?? "").Contains("pečat nije pouzdano prepoznat")), "digitalni dokument – pečat sigurno ne postoji");
        }

        [TestMethod]
        public void R16_sken_sa_pecatom_prevoznika_u_r58a_greska()
        {
            var r = Proveri(BezDatumaR16("ocr", 0.06));
            var n0 = Svi(r).Single(n => n.Pravilo == "R16" && !n.SamoInterno);
            Assert.AreEqual(Tezina.Greska, n0.Tezina, Opis(r));
            Assert.IsFalse(n0.Tekst.Contains("Posle overe"));
        }

        [TestMethod]
        public void R16_pecat_nije_pouzdano_prepoznat_upozorenje_i_interno()
        {
            var r = Proveri(BezDatumaR16("ocr", 0.012));
            Assert.AreEqual(Tezina.Upozorenje, Svi(r).Single(n => n.Pravilo == "R16" && !n.SamoInterno).Tezina, Opis(r));
            var i = Svi(r).Single(n => n.Pravilo == "R16" && n.SamoInterno);
            StringAssert.Contains(i.Tekst, "pečat nije pouzdano prepoznat");
        }

        [TestMethod]
        public void R16_mesto_nedostaje_ostaje_greska_i_u_radnoj_verziji()
        {
            var s = BezDatumaR16("tekst", -1); s.C.R16.Mesto = ""; s.C.R16.SifraStanice = "";
            var r = Proveri(s);
            Assert.IsTrue(Svi(r).Any(n => n.Pravilo == "R16" && n.Tezina == Tezina.Greska && n.Tekst.Contains("mesto/stanica")), Opis(r));
            Assert.IsTrue(Svi(r).Any(n => n.Pravilo == "R16" && n.Tezina == Tezina.Upozorenje && n.Tekst.Contains("datum")), Opis(r));
        }

        [TestMethod]
        public void R16_ispis_elektronskog_lista_je_overen_greska()
        {
            var s = BezDatumaR16("tekst", -1); s.C.ElektronskiIspis = true; s.C.NapomenaIspisa = true;
            Assert.AreEqual(Tezina.Greska, Svi(Proveri(s)).Single(n => n.Pravilo == "R16").Tezina);
        }

        // ------------------------------------------------------------------------------------------- 1.2 R1/R4 – zemlja

        [TestMethod]
        public void R4_bez_zemlje_i_kad_je_grad_jednoznacan_greska()
        {
            var s = new Scenario();
            s.C.R4 = F(new Strana { Naziv = "VRANJICA d.o.o.", Adresa = "Kraljice Katarine 158, 11030 Belgrade", Email = "a@b.rs" }, "VRANJICA d.o.o.\nKraljice Katarine 158\n11030 Belgrade");
            var r = Proveri(s);
            Assert.IsTrue(Svi(r).Any(n => n.Pravilo == "R4" && n.Tezina == Tezina.Greska && n.Tekst.Contains("zemlja")), Opis(r));
        }

        [TestMethod]
        public void R4_zemlja_u_obliku_koji_nije_ISO_samo_upozorenje_zemlju_upisati_po_ISO()
        {
            foreach (var (oblik, iso) in new[] { ("SRB", "RS"), ("Srbia", "RS"), ("Србија", "RS"), ("Deutchland", "DE"), ("Szerbia", "RS"), ("Česká republika", "CZ"), ("Macaristan", "HU") })
            {
                var s = new Scenario();
                var tekst = "Primalac d.o.o.\nKraljice Katarine 158\n11030 Grad, " + oblik;
                s.C.R4 = F(new Strana { Naziv = "Primalac d.o.o.", Adresa = "Kraljice Katarine 158, 11030 Grad, " + oblik, NazivZemlje = Zemlje.Nadji(tekst), Email = "a@b.rs" }, tekst);
                var r = Proveri(s);
                Assert.IsFalse(Svi(r).Any(n => n.Pravilo == "R4" && n.Tekst.Contains("zemlja")), oblik + ": " + Opis(r));
                var w = Svi(r).Single(n => n.Pravilo == "R4-ISO");
                Assert.AreEqual(Tezina.Upozorenje, w.Tezina, oblik);
                StringAssert.Contains(w.Tekst, "zemlju upisati po ISO 3166: " + iso, oblik);
            }
        }

        // ------------------------------------------------------------------------------------------- 2.1 R30 – CUV i CIT 18

        [TestMethod]
        public void R30_CUV_polje_CIM_s_pravom_prazno_nema_greske()
        {
            var s = new Scenario { Fajl = "Bor Teretna-Zabrze CUV.pdf" };
            s.C.R30 = new OznakaDokumenta { Status = StatusRubrike.Prazno, Pouzdanost = 0.95 };
            Assert.IsFalse(Ima(Proveri(s), "R30"));
            // prazna kola NHM 9922 (bez CUV u imenu fajla)
            var s2 = new Scenario();
            s2.C.R30 = new OznakaDokumenta { Status = StatusRubrike.Prazno, Pouzdanost = 0.95 };
            s2.C.R24 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "9922" } } }, "9922");
            s2.C.R21 = F(new NaznacenjeRobe { OpisRobe = "25 EMPTY WAGONS" }, "25 EMPTY WAGONS - SEE WAGON LIST ATTACHED");
            Assert.IsFalse(Ima(Proveri(s2), "R30"));
            // običan CIM sa praznim poljem CIM – greška ostaje
            var s3 = new Scenario();
            s3.C.R30 = new OznakaDokumenta { Status = StatusRubrike.Prazno, Pouzdanost = 0.95 };
            Assert.AreEqual(Tezina.Greska, Prvi(Proveri(s3), "R30").Tezina);
        }

        [TestMethod]
        public void R30_CIT18_nije_CIM_ne_primenjuje_se()
        {
            var s = new Scenario { Fajl = "CIT 18 31555958441-4.pdf" };
            s.C.R30 = new OznakaDokumenta { Status = StatusRubrike.Prazno, Pouzdanost = 0.95 };
            Assert.IsFalse(Ima(Proveri(s), "R30"));
        }

        [TestMethod]
        public void R30_X_u_polju_CIM_a_dokument_je_CUV_upozorenje()
        {
            var s = new Scenario { Fajl = "CUV 26X609.pdf" };
            s.C.R30 = F(new OznakaDokumenta { Cim = true }, "CIM X ");
            // v3.0.2 (2.2): X u kućici CIM, a naziv fajla kaže CUV – znaci se ne slažu: INTERNO „vrsta dokumenta nesigurna“, bez R30
            var r = Proveri(s);
            Assert.IsFalse(Ima(r, "R30"));
            var n = Prvi(r, "VRSTA-DOKUMENTA");
            Assert.IsTrue(n.SamoInterno);
            StringAssert.Contains(n.Tekst, "nesigurna");
        }

        // ------------------------------------------------------------------------------------------- 2.2 nečitljivo polje – nema tvrdnje

        [TestMethod]
        public void Necitljiva_r7_nema_greske_KP7_samo_interno_rucna_provera()
        {
            Scenario S(double pokrivenost)
            {
                var s = new Scenario { IzvorCim = "ocr", Rezim = "spoljni" };
                s.C.R7 = F(new ListaRubrika(), "1 verfügungsberechtigt ; DC: s.r.o.,\n28976096, 91320410 ZSSK s.r.0. ICO 31729002 DIC\n2020482881; Freight payer Logistics Hungaria Kft.");
                s.C.R7.Pokrivenost = pokrivenost;
                s.C.MastiloR58a = 0.06;
                return s;
            }
            var citljivo = Proveri(S(0.95));
            Assert.IsTrue(Svi(citljivo).Any(n => n.Rubrika == "7" && !n.SamoInterno && (n.Tezina == Tezina.Greska || n.Tezina == Tezina.Upozorenje)), "kontrola: " + Opis(citljivo));
            var r = Proveri(S(0.25));
            Assert.IsFalse(Svi(r).Any(n => n.Rubrika == "7" && !n.SamoInterno && (n.Tezina == Tezina.Greska || n.Tezina == Tezina.Upozorenje)), Opis(r));
            var i = Svi(r).Single(n => n.Pravilo == "NECITLJIVO-POLJE");
            Assert.IsTrue(i.SamoInterno);
            StringAssert.StartsWith(i.Tekst, "r7 nečitljiva – ručna provera");
            StringAssert.Contains(i.Tekst, "malo prepoznatih reči");
            Assert.IsTrue(r.Listovi[0].ZaRucnuProveru);
            Assert.AreEqual(0, r.BezDokaza.Count, "samoprovera: svaki nalaz za pošiljaoca ima pouzdan dokaz");
        }

        [TestMethod]
        public void Mera_citljivosti_sum_i_cist_tekst()
        {
            Assert.IsTrue(Citljivost.Smisao("Kraljice Katarine 158\n11030 Belgrade") > 0.95);
            Assert.IsTrue(Citljivost.Smisao("Freight payer Logistics Hungaria Kft.; 3212, 8331.") > 0.9);
            Assert.IsTrue(Citljivost.Smisao("© (iméno, ~ jjj £4 _547893 |aGnacTb| rPyn ¦¦ $K20") < Citljivost.PragSmisla);
            var m = Citljivost.Izmeri(new TekstRubrika { Status = StatusRubrike.Popunjeno, Tekst = "Lider-Pro doo", Pouzdanost = 0.45, Pokrivenost = 0.9 }, true);
            Assert.IsTrue(m.Necitljivo); StringAssert.Contains(m.Razlog(), "niska pouzdanost OCR-a");
            Assert.IsFalse(Citljivost.Izmeri(new TekstRubrika { Status = StatusRubrike.Popunjeno, Tekst = "©©© ~~", Pouzdanost = 0.2 }, false).Necitljivo, "tekst PDF-a se ne meri");
        }

        [TestMethod]
        public void Samoprovera_nalaz_bez_pouzdanog_dokaza_lista_na_rucnu_proveru()
        {
            var c = IspravanCim();
            c.R7.Status = StatusRubrike.Necitljivo;
            var l = new RezultatLista { Cim = c, Dokument = new Dokument { Izvor = "ocr" }, Nalazi = { new Nalaz { Pravilo = "KP7-PLATILAC", Rubrika = "7", Tezina = Tezina.Greska } } };
            Assert.AreEqual(1, Motor.BezDokaza(l).Count);
            l.Nalazi[0].SamoInterno = true;
            Assert.AreEqual(0, Motor.BezDokaza(l).Count, "interni nalaz ne ide pošiljaocu");
        }
    }
}
