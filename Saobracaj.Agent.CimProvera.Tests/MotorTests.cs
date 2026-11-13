using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>
    /// Rules engine nad ručno napravljenim podacima: polazni CIM je ispravan (nijedno pravilo se ne javlja), a svaki test
    /// kvari jednu stvar i proverava da se javi tačno očekivano pravilo. Za svako implementirano pravilo postoji prolaz
    /// (Ispravan_list_nema_nalaza + Prolaz_*) i pad (Pad_*).
    /// </summary>
    [TestClass]
    public class MotorTests
    {
        private static PravilaSkup pravila;
        private static readonly DateTime Danas = new DateTime(2026, 11, 10);

        [ClassInitialize]
        public static void Init(TestContext _)
        {
            var put = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pravila", "cim_validation_rules.json");
            pravila = PravilaSkup.IzTeksta(File.ReadAllText(put), put);
        }

        // ------------------------------------------------------------------------------------------- scenario

        internal sealed class Scenario
        {
            public CimList C = IspravanCim();
            public List<Dokument> Ostali = new List<Dokument>();
            public string Rezim = "kp";
            public bool Interni;
            public DateTime Datum = Danas;
            public string IzvorCim = "tekst";
            public string Fajl = "cim.pdf";
        }

        private static T F<T>(T r, string tekst = null) where T : Rubrika
        {
            r.Status = StatusRubrike.Popunjeno;
            r.Pouzdanost = 0.95;
            r.Tekst = tekst ?? (string.IsNullOrEmpty(r.Tekst) ? "x" : r.Tekst);
            return r;
        }

        internal static CimList IspravanCim()
        {
            var c = new CimList { Obrazac = "CIM", JeziciPopunjavanja = { "sr", "en" } };
            c.R1 = F(new Strana { Naziv = "Pošiljalac d.o.o.", Adresa = "Ulica 1, 18000 Niš", SifraZemlje = "RS", Potpis = true, Telefon = "+381 18 123456" });
            c.R4 = F(new Strana { Naziv = "Primalac AD", Adresa = "Ulica 2, 1000 Sofija", SifraZemlje = "BG", Email = "a@b.bg" });
            c.R7 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "16", Tekst = "Platilac za deonicu SRB: Lokomotiva d.o.o. Ponuda KP br. 15/26" } } }, "16 Platilac za deonicu SRB: Lokomotiva d.o.o.; Ponuda KP br. 15/26");
            c.R8 = F(new TekstRubrika(), "004NISBAR26");
            c.R10 = F(new MestoIsporuke { Stanica = "Dimitrovgrad gr.", SifraStanice = "721234", Zemlja = "72" });
            c.R12 = F(new TekstRubrika(), "721234");
            c.R13 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "3", Tekst = "KP - Kombinovani prevoz d.o.o., UIC 3212, Niš - Dimitrovgrad gr., 1" } } });
            c.R14 = F(new TekstRubrika(), "1 12345");
            c.R16 = F(new Preuzimanje { Mesto = "Niš", SifraStanice = "723456", Zemlja = "72", Datum = "2026-11-05", Cas = "14" });
            c.R18 = F(new ListaRubrika { Stavke = { new SifraTekst { Tekst = "31 78 6656 001-5" } } });
            c.R20 = F(new TekstRubrika(), "DAP Dimitrovgrad");
            c.R21 = F(new NaznacenjeRobe { OpisRobe = "Ugalj", Plombe = true });
            c.R24 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "270111" } } });
            c.R25 = F(new Masa { Pojedinacne = { "60000" }, Ukupno = "60000" });
            c.R29 = F(new MestoDatum { Mesto = "Niš", Datum = "2026-11-05" });
            c.R30 = F(new OznakaDokumenta { Cim = true });
            c.R49 = F(new TekstRubrika(), "12");
            c.R50 = F(new TekstRubrika(), "723456 - 721234");
            c.R57 = F(new PrevozniciRubrika { Prevoznici = { new Prevoznik { Sifra = "3212", Naziv = "KP - Kombinovani prevoz d.o.o., UIC 3212", Deonica = "723456 - 721234", StatusPrevoznika = "1" } } });
            c.R58a = F(new UgovorniPrevoznik { Sifra = "3212", Naziv = "Kombinovani prevoz d.o.o.", Adresa = "Beograd", Potpis = true }, "3212 Kombinovani prevoz d.o.o., UIC 3212, Beograd");
            c.R62 = F(new Identifikacija { Zemlja = "72", Stanica = "723456", Preduzece = "3212", Broj = "12345-6" });
            return c;
        }

        private static RezultatProvere Proveri(Scenario s)
        {
            var u = new UlazProvere
            {
                Dokumenti = new List<Dokument> { new Dokument { Tip = TipDokumenta.Cim, Fajl = s.Fajl, Izvor = s.IzvorCim, Strane = { 1 }, Cim = { s.C } } }.Concat(s.Ostali).ToList(),
                RezimKp = s.Rezim, InterniPosiljalac = s.Interni, ReferentniDatum = s.Datum, Prag = 0.7,
            };
            return new Motor(pravila).Proveri(u);
        }

        private static IEnumerable<Nalaz> Svi(RezultatProvere r) => r.Listovi.SelectMany(l => l.Nalazi.Concat(l.Nalazi.SelectMany(n => n.Takodje)));
        private static bool Ima(RezultatProvere r, string id) => Svi(r).Any(n => n.Pravilo == id);
        private static Nalaz Prvi(RezultatProvere r, string id) => Svi(r).First(n => n.Pravilo == id);

        private static Dokument Spisak(params KolaNaSpisku[] kola)
        {
            var s = new SpisakKola { KpNaveden = true, R62 = F(new Identifikacija { Zemlja = "72", Stanica = "723456", Preduzece = "3212", Broj = "12345-6" }) };
            s.Kola.AddRange(kola);
            return new Dokument { Tip = TipDokumenta.SpisakKola, Fajl = "spisak.xlsx", Izvor = "excel", Spisak = { s } };
        }

        private static KolaNaSpisku Kola(string broj, string nhm = "270111", bool prazna = false) => new KolaNaSpisku { Broj = broj, Nhm = nhm, Prazna = prazna };

        private static void DvaKola(Scenario s)
        {
            s.C.R18.Stavke = new List<SifraTekst> { new SifraTekst { Tekst = "317866560015" }, new SifraTekst { Tekst = "317866560023" } };
        }

        // ------------------------------------------------------------------------------------------- prolaz

        [TestMethod]
        public void Ispravan_list_nema_nalaza()
        {
            var r = Proveri(new Scenario());
            var svi = Svi(r).Where(n => n.Tezina != Tezina.Info).ToList();
            Assert.AreEqual(0, svi.Count, "Ispravan list ne sme imati nalaze, a ima: " + string.Join(" | ", svi));
            Assert.IsTrue(r.Ispravan);
            // jedina informacija: platilac za deonicu kroz Srbiju
            Assert.AreEqual("Platilac za deonicu kroz Srbiju: Lokomotiva d.o.o.", Prvi(r, "KP7-INFO").Tekst);
        }

        [TestMethod]
        public void Sva_pravila_iz_json_a_sa_tezinom_su_implementirana_osim_poznatih()
        {
            var ni = Motor.NijeImplementirano(pravila);
            var poznato = new[] { "G7", "G10", "R19", "R21-PIECES", "R21-PACK", "R21-HS", "R21-SEALS", "R21-ARC", "R21-EXPORT", "R21-CPC", "X18", "REC5" };
            CollectionAssert.AreEquivalent(poznato, ni, "Nova pravila u JSON-u bez implementacije: " + string.Join(", ", ni.Except(poznato)));
        }

        [DataTestMethod]
        [DataRow("R49")] [DataRow("R50")] [DataRow("R58A")] [DataRow("R62")] [DataRow("KP2")]
        public void Prolaz_rubrike_prevoznika_se_ne_traze_od_spoljnog_posiljaoca(string id)
        {
            var s = new Scenario { Rezim = "spoljni" };
            s.C.R49 = new TekstRubrika(); s.C.R50 = new TekstRubrika(); s.C.R57 = F(new PrevozniciRubrika { Prevoznici = { new Prevoznik { Sifra = "3212" } } });
            s.C.R58a = new UgovorniPrevoznik(); s.C.R62 = new Identifikacija();
            Assert.IsFalse(Ima(Proveri(s), id));
        }

        [TestMethod]
        public void Prolaz_auto_rezim_kp_po_r62_3212()
        {
            var s = new Scenario { Rezim = "auto" };
            s.C.R58a = new UgovorniPrevoznik();
            Assert.IsTrue(Ima(Proveri(s), "R58A"), "r62 = 3212 → režim KP → r58a se proverava");
            s.C.R62.Preduzece = "2154";
            Assert.IsFalse(Ima(Proveri(s), "R58A"), "spoljni pošiljalac, r62 ≠ 3212 → r58a se ne proverava");
        }

        [TestMethod] public void Prolaz_R14_bilo_koji_broj() { foreach (var t in new[] { "1 264410", "KP 2026", "ŽRS 264410.02" }) { var s = new Scenario(); s.C.R14.Tekst = t; Assert.IsFalse(Ima(Proveri(s), "R14"), t); } }
        [TestMethod] public void Prolaz_R20_INCO_sa_mestom_na_putu() { var s = new Scenario(); s.C.R20.Tekst = "DAP Dimitrovgrad gr."; var r = Proveri(s); Assert.IsFalse(Ima(r, "R20-INCO")); Assert.IsFalse(Ima(r, "R20-RUTA")); }
        [TestMethod] public void Prolaz_G3_vise_kola_sa_spiskom_u_r9_i_r18()
        {
            var s = new Scenario(); DvaKola(s);
            s.C.R9 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "271", Tekst = "Spisak kola" } } });
            s.C.R18.Stavke.Add(new SifraTekst { Tekst = "po spisku kola" });
            s.Ostali.Add(Spisak(Kola("317866560015"), Kola("317866560023")));
            var r = Proveri(s);
            Assert.IsFalse(Ima(r, "G3")); Assert.IsFalse(Ima(r, "X9")); Assert.IsFalse(Ima(r, "X20")); Assert.IsFalse(Ima(r, "G2"));
        }
        [TestMethod] public void Prolaz_X22_carinski_u_r9()
        {
            var s = new Scenario();
            s.Ostali.Add(new Dokument { Tip = TipDokumenta.IzvoznaDeklaracija, Fajl = "VDD.pdf" });
            s.C.R9 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "830", Tekst = "Izvozna deklaracija (VDD)" } } });
            s.C.R21.Mrn.Add(new Mrn { Prefiks = "E MRN", Broj = "26RS000000000001A1" });
            var r = Proveri(s);
            Assert.IsFalse(Ima(r, "X22")); Assert.IsFalse(Ima(r, "R21-MRN")); Assert.IsFalse(Ima(r, "X4")); Assert.IsFalse(Ima(r, "R9"));
        }
        [TestMethod] public void Prolaz_X22_carinski_u_r7_sifra_10()
        {
            var s = new Scenario();
            s.Ostali.Add(new Dokument { Tip = TipDokumenta.TranzitnaDeklaracija, Fajl = "T1.pdf" });
            s.C.R7.Stavke.Add(new SifraTekst { Sifra = "10", Tekst = "T1 dostupan kod carinarnice Niš" });
            Assert.IsFalse(Ima(Proveri(s), "X22"));
        }
        [TestMethod] public void Prolaz_X19_spisak_odgovara_r62() { var s = new Scenario(); s.Ostali.Add(Spisak(Kola("317866560015"))); Assert.IsFalse(Ima(Proveri(s), "X19")); }
        [TestMethod] public void Prolaz_X13_zbir_sa_tarom() { var s = new Scenario(); s.C.R25.Pojedinacne = new List<string> { "30000", "28000" }; s.C.R25.Tara = "2000"; s.C.R25.Ukupno = "60.000"; Assert.IsFalse(Ima(Proveri(s), "X13")); }
        [TestMethod] public void Prolaz_X25_datum_danas() { var s = new Scenario(); s.C.R29.Datum = "2026-11-10"; s.C.R16.Datum = "2026-11-10"; Assert.IsFalse(Ima(Proveri(s), "X25")); }
        [TestMethod] public void Prolaz_KP1_kp_u_r57_sa_deonicom()
        {
            var s = new Scenario(); s.C.R62.Preduzece = "2154"; s.C.R58a.Sifra = "2154"; s.C.R58a.Naziv = "SŽ Cargo";
            Assert.IsFalse(Ima(Proveri(s), "KP1"));
        }

        // ------------------------------------------------------------------------------------------- pad

        private static readonly Dictionary<string, Action<Scenario>> Kvarovi = new Dictionary<string, Action<Scenario>>
        {
            ["G1"] = s => s.C.JeziciPopunjavanja = new List<string> { "sr" },
            ["G3"] = DvaKola,
            ["G4"] = s => s.Ostali.Add(new Dokument { Tip = TipDokumenta.DodatniList, Fajl = "dodatni.pdf", DodatniList = { new DodatniListPodaci { BrojPosiljke = true, DatumPreuzimanja = true } } }),
            ["G5"] = s => s.C.R7.Stavke.Add(new SifraTekst { Tekst = "Platilac za deonicu u Srbiji je X" }),
            ["G6"] = s => s.C.R9 = F(new ListaRubrika { Stavke = { new SifraTekst { Tekst = "Faktura" } } }),
            ["G9"] = s => s.C.ElektronskiIspis = true,
            ["R1"] = s => s.C.R1.SifraZemlje = "",
            ["R1-VAT"] = s => { s.C.R1.SifraZemlje = "HU"; s.C.R4.SifraZemlje = "AT"; },
            ["R4"] = s => s.C.R4 = new Strana(),
            ["R4-VAT"] = s => { s.C.R1.SifraZemlje = "HU"; s.C.R4.SifraZemlje = "AT"; },
            ["R7"] = s => s.C.R7.Stavke.Add(new SifraTekst { Sifra = "5", Tekst = "" }),
            ["R9"] = s => s.Ostali.Add(new Dokument { Tip = TipDokumenta.IzvoznaDeklaracija, Fajl = "VDD.pdf" }),
            ["R10"] = s => s.C.R10.Stanica = "",
            ["R12"] = s => { s.C.R12 = new TekstRubrika(); s.C.R10.SifraStanice = ""; },
            ["R14"] = s => s.C.R14 = new TekstRubrika(),
            ["R16"] = s => s.C.R16.Datum = "",
            ["R20"] = s => s.C.R20 = new TekstRubrika(),
            ["R20-INCO"] = s => s.C.R20.Tekst = "DAP",
            ["R20-RUTA"] = s => s.C.R20.Tekst = "DAP Curtici Gr",
            ["R20-INCO-ZASTARELO"] = s => s.C.R20.Tekst = "X D A F 711",
            ["R21-MRN99"] = s => { s.C.R21.Mrn.Clear(); s.C.R99 = F(new TekstRubrika(), "T2@26HU4110004THDTAK8"); },
            ["G12"] = s => s.C.Zaglavlje = "Küldemény adatok információ / Nem fuvarlevél",
            ["X30"] = s => { s.Fajl = "LDV Busca - Carpinis_31 wgs.pdf"; s.C.R21.Tekst = "Nr. 30 empty private wagons NHM 9922"; },
            ["R21"] = s => s.C.R21 = new NaznacenjeRobe(),
            ["R22"] = s => s.C.R22 = F(new TekstRubrika(), "X"),
            ["R23"] = s => s.C.R23 = F(new TekstRubrika(), "Y"),
            ["R24"] = s => s.C.R24.Stavke = new List<SifraTekst> { new SifraTekst { Sifra = "252" } },
            ["R24-NHM6"] = s => s.C.R24.Stavke = new List<SifraTekst> { new SifraTekst { Sifra = "2520" } },
            ["R18-SPISAK"] = s => s.C.R18 = F(new ListaRubrika { Stavke = { new SifraTekst { Tekst = "po spisku kola" } } }, "po spisku kola"),
            ["R25-MASA"] = s => s.C.R25 = new Masa(),
            ["R26"] = s => s.C.R26 = F(new Iznos { Vrednost = "100", Valuta = "XXX" }),
            ["R27"] = s => s.C.R27 = F(new Iznos { Vrednost = "100", Valuta = "" }),
            ["R28"] = s => s.C.R28 = F(new Iznos { Vrednost = "", Valuta = "EUR" }),
            ["R29"] = s => s.C.R29.Mesto = "",
            ["R30"] = s => s.C.R30.Cim = false,
            ["R49"] = s => s.C.R49 = new TekstRubrika(),
            ["R50"] = s => s.C.R50 = new TekstRubrika(),
            ["R58A"] = s => s.C.R58a = F(new UgovorniPrevoznik(), "- / -"),
            ["R62"] = s => { s.C.R62.Zemlja = ""; s.C.R62.Stanica = ""; s.C.R62.Preduzece = ""; },
            ["R21-DESC"] = s => { s.C.R21.ZaostaliTekstSablona = true; s.C.R21.ZaostaliTekst = "XXXX naziv robe"; },
            ["R21-UTI"] = s => s.C.R21.Uti.Add(new Uti { Tip = "22G1" }),
            ["R21-IMDG"] = s => { s.C.R23 = F(new TekstRubrika(), "X"); s.C.R21.UnBrojevi.Add("1203"); s.C.PomorskaDeonica = true; },
            ["R21-MRN"] = s => s.Ostali.Add(new Dokument { Tip = TipDokumenta.TranzitnaDeklaracija, Fajl = "T1.pdf" }),
            ["R21-EWC"] = s => s.C.R7.Stavke.Add(new SifraTekst { Sifra = "26", Tekst = "Izjava o otpadu" }),
            ["R21-CUV"] = s => s.Ostali.Add(Spisak(Kola("317866560015"), Kola("317866560023", "992100", true))),
            ["X1"] = s => s.C.R21.UnBrojevi.Add("1203"),
            ["X2"] = s => s.C.R23 = F(new TekstRubrika(), "X"),
            ["X3"] = s => { s.C.R23 = F(new TekstRubrika(), "X"); s.C.R21.UnBrojevi.Add("1203"); s.C.PomorskaDeonica = true; },
            ["X4"] = s => s.C.R21.Mrn.Add(new Mrn { Prefiks = "E MRN", Broj = "26RS000000000001A1" }),
            ["X5"] = s => s.C.R22 = F(new TekstRubrika(), "X"),
            ["X6"] = s => s.C.R7.Stavke.Add(new SifraTekst { Sifra = "26", Tekst = "Izjava o otpadu" }),
            ["X7"] = s => s.C.R7.Stavke.Add(new SifraTekst { Sifra = "3", Tekst = "" }),
            ["X8"] = s => s.C.R7.Stavke.Add(new SifraTekst { Sifra = "42", Tekst = "" }),
            ["X9"] = DvaKola,
            ["X11"] = s => s.Ostali.Add(Spisak(Kola("317866560015"), Kola("317866560023", "992100", true))),
            ["X12"] = s => s.C.R26 = F(new Iznos { Vrednost = "100", Valuta = "XXX" }),
            ["X13"] = s => { s.C.R25.Pojedinacne = new List<string> { "30000", "20000" }; s.C.R25.Ukupno = "60000"; },
            ["X14"] = s => s.C.R29.Datum = "2026-11-06",
            ["X16"] = s => { s.C.R1.SifraZemlje = "HU"; s.C.R4.SifraZemlje = "AT"; },
            ["X17"] = s => s.C.R26 = F(new Iznos { Vrednost = "100", Valuta = "HRK" }),
            ["X19"] = s => { var d = Spisak(Kola("317866560015")); d.Spisak[0].R62.Preduzece = "3121"; s.Ostali.Add(d); },
            ["X20"] = s => { DvaKola(s); s.C.R9 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "380", Tekst = "Faktura" } } }, "380 Faktura"); s.Ostali.Add(Spisak(Kola("317866560015"), Kola("317866560023"))); },
            ["X21"] = s => { var d = Spisak(Kola("317866560015")); d.Spisak[0].RidOznaka = true; s.Ostali.Add(d); },
            ["X22"] = s => { s.C.R9 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "380", Tekst = "Faktura" } } }, "380 Faktura"); s.Ostali.Add(new Dokument { Tip = TipDokumenta.TranzitnaDeklaracija, Fajl = "T1.pdf" }); },
            ["X26"] = s => { s.C.R9 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "271", Tekst = "Wagon list" } } }, "271 Wagon list"); },
            ["X27"] = s => { s.C.R9 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "380", Tekst = "Faktura/Invoice" } } }, "380 Faktura/Invoice"); },
            ["X28"] = s => { s.C.R20 = F(new TekstRubrika(), "Franko prevoznina"); s.Ostali.Add(new Dokument { Tip = TipDokumenta.Faktura, Fajl = "faktura.pdf", Incoterms = "DAP Alesd" }); },
            ["X29"] = s => { var d = Spisak(Kola("317866560015", "262")); s.Ostali.Add(d); },
            ["X29-NHM6"] = s => { var d = Spisak(Kola("317866560015", "2620")); s.Ostali.Add(d); },
            ["R1-ISO"] = s => { s.C.R1.SifraZemlje = ""; s.C.R1.NazivZemlje = "srbija"; s.C.R1.Tekst = "Pošiljalac d.o.o.\nBulevar 12\n11000 Beograd, Srbija"; },
            ["R4-ISO"] = s => { s.C.R4.SifraZemlje = ""; s.C.R4.NazivZemlje = "italy"; s.C.R4.Tekst = "Primalac SPA\nVia Roma 1\n45100 Rovigo, Italy"; },
            ["X23"] = s => s.Ostali.Add(new Dokument { Tip = TipDokumenta.Eur1, Fajl = "EUR1.pdf" }),
            ["X24"] = s => s.Ostali.Add(new Dokument { Tip = TipDokumenta.TabelaMrn, Fajl = "TABELICA.xlsx", Carinski = { new CarinskiPodaci { Mrn = { "26RS000000000009A9" }, MrnPoUti = { new Mrn { Broj = "26RS000000000009A9", Uz = "MSCU1234567" } } } } }),
            ["X25"] = s => { s.C.R29.Datum = "2026-11-16"; s.C.R16.Datum = "2026-11-16"; },
            ["KP57"] = s => { s.C.R62.Preduzece = "2154"; s.C.R58a.Sifra = "2154"; s.C.R58a.Naziv = "SŽ Cargo"; s.C.R58a.Tekst = "2154 SŽ Cargo"; s.C.R57.Prevoznici.Clear(); s.C.R57.Tekst = "2154 SŽ Cargo"; },
            ["KP2"] = s => s.C.R58a = F(new UgovorniPrevoznik { Sifra = "2154", Naziv = "VTG Railtrain" }, "2154 VTG Railtrain"),
            ["KP3"] = s => s.C.R13.Stavke[0].Tekst = "SŽ Cargo, Niš - Dimitrovgrad",
            ["KP7-PLATILAC"] = s => s.C.R7 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "16", Tekst = "Frachtzahler HU: Kombinovani prevoz" } } }, "16 Frachtzahler HU: Kombinovani prevoz"),
            ["KP7-NIJE-PLATILAC"] = s => s.C.R7 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "16", Tekst = "Frachtzahler SRB: Kombinovani prevoz" } } }, "16 Frachtzahler SRB: Kombinovani prevoz"),
            ["KP7-NEJASNO"] = s => s.C.R7 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "16", Tekst = "IŽS: KP 3212" } } }, "16. IŽS: KP 3212"),
            ["KP7-INFO"] = s => { },
            ["KP5"] = s => s.C.R57.Prevoznici[0].Naziv = "VTG Railtrain",
            ["KP6"] = s => { var d = Spisak(Kola("317866560015")); d.Spisak[0].KpNaveden = false; s.Ostali.Add(d); },
            ["REC1"] = s => s.C.R8 = new TekstRubrika(),
            ["REC3"] = s => s.C.R13.Stavke[0].Tekst = "KP, Niš - Dimitrovgrad",
            ["REC4"] = s => s.C.R57.Prevoznici[0].Deonica = "Niš - Dimitrovgrad",
            ["REC6"] = s => { var d = Spisak(Kola("317866560015")); d.Izvor = "ocr"; s.Ostali.Add(d); },
            ["REC7"] = s => s.C.KvalitetSkena = "los",
            ["REC8"] = s => s.C.R1.Telefon = "",
        };

        public static IEnumerable<object[]> SvaPravila => Kvarovi.Keys.Select(k => new object[] { k });

        [DataTestMethod]
        [DynamicData(nameof(SvaPravila))]
        public void Pad_pravilo_se_javlja(string id)
        {
            var s = new Scenario();
            Kvarovi[id](s);
            var r = Proveri(s);
            Assert.IsTrue(Ima(r, id), id + " se nije javilo. Nalazi: " + string.Join(" | ", Svi(r)));
            var def = pravila.PoId[id];
            var ocekivano = def.Tezina;
            Assert.AreEqual(ocekivano, Prvi(r, id).TezinaPravila, "težina mora biti iz JSON-a");
        }

        [DataTestMethod]
        [DynamicData(nameof(SvaPravila))]
        public void Prolaz_pravilo_se_ne_javlja_na_ispravnom_listu(string id)
        {
            if (pravila.PoId[id].Tezina == Tezina.Info) return; // informacija (npr. platilac za SRB) postoji i na ispravnom listu
            Assert.IsFalse(Ima(Proveri(new Scenario()), id));
        }

        [TestMethod]
        public void Svako_implementirano_pravilo_ima_test_pada()
        {
            var bezTesta = Provere.Sve.Keys.Where(k => !Kvarovi.ContainsKey(k)).ToList();
            Assert.AreEqual(0, bezTesta.Count, "Pravila bez testa pada: " + string.Join(", ", bezTesta));
        }

        // ------------------------------------------------------------------------------------------- posebni slučajevi

        [TestMethod]
        public void Kucice_r16_i_polje_r30_po_mastilu()
        {
            // kućice mesec–dan–čas sa mastilom koje se ne čita i nesigurno polje CIM → upozorenje o čitljivosti
            var s = new Scenario { IzvorCim = "ocr" }; s.C.R30.Cim = false; s.C.R30.Status = StatusRubrike.Necitljivo; s.C.R30.Pouzdanost = 0.4;
            s.C.R16.Datum = ""; s.C.R16.Cas = ""; s.C.R16.KuciceStanje = StatusRubrike.Necitljivo;
            var r = Proveri(s);
            Assert.IsFalse(Ima(r, "R30")); Assert.IsFalse(Ima(r, "R16"));
            var c = string.Join(" | ", Svi(r));
            StringAssert.Contains(c, "Rubrika 16 nije"); StringAssert.Contains(c, "Rubrika 30 nije");
            // prazne kućice i prazno polje (bez mastila) → greške, i na skenu
            var t = new Scenario { IzvorCim = "ocr" }; t.C.R30.Cim = false; t.C.R16.Datum = ""; t.C.R16.Cas = ""; t.C.R16.KuciceStanje = StatusRubrike.Prazno;
            var rt = Proveri(t);
            Assert.IsTrue(Ima(rt, "R30")); Assert.IsTrue(Ima(rt, "R16"));
        }

        [TestMethod]
        public void R57_bez_statusa_i_deonice_nije_greska()
        {
            var s = new Scenario { Rezim = "spoljni" };
            s.C.R57.Prevoznici = new List<Prevoznik> { new Prevoznik { Sifra = "3212", Naziv = "KP" }, new Prevoznik { Sifra = "2179", Naziv = "SŽ" } };
            s.C.R57.Tekst = "3212 KP\n2179 SŽ";
            var r = Proveri(s);
            Assert.AreEqual(0, r.Listovi[0].Nalazi.Count(n => n.Rubrika == "57"), string.Join(" | ", Svi(r)));
        }

        // ------------------------------------------------------------------------------------------- r7: platilac za Srbiju (v2.7)

        private static RezultatProvere SaR7(string r7)
        {
            var s = new Scenario(); s.C.R7 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "16", Tekst = r7 } } }, r7);
            return Proveri(s);
        }

        private static string Kp7(RezultatProvere r) => string.Join(",", new[] { "KP7-PLATILAC", "KP7-NIJE-PLATILAC", "KP7-NEJASNO" }.Where(id => Ima(r, id)));

        [DataTestMethod]
        [DataRow("KP UIC 3212")] [DataRow("KP-UIC 3212")] [DataRow("KPUIC3212")] [DataRow("UIC 3212")] [DataRow("3212 KP")] [DataRow("IŽS: KP 3212")]
        [DataRow("Kombinovani Prevoz d.o.o.")] [DataRow("K-prevoz")] [DataRow("КП")] [DataRow("Комбиновани превоз")] [DataRow("3Z12 KP")] [DataRow("K P 32l2")]
        public void KP7_kp_kao_platilac_za_srbiju_u_svim_oblicima_je_greska(string oblik)
        {
            Assert.AreEqual("KP7-NIJE-PLATILAC", Kp7(SaR7("16. Frachtzahler für RS ist " + oblik)), oblik);
        }

        [DataTestMethod]
        [DataRow("16. Platilac za deonicu SRB: Lokomotiva d.o.o.", "Lokomotiva d.o.o")]
        [DataRow("16. Payer SRB/HR/SI/IT is Lokomotiva doo. Export in S.Mitrovica", "Lokomotiva doo")]
        [DataRow("16. Frachtzahler auf EVM, SŽTP, HŽC und KP ist EurologSystem", "EurologSystem")]
        [DataRow("16. Frachtzahler fuer SRB: VTG Rail Logistics Austria GmbH", "VTG Rail Logistics Austria GmbH")]
        [DataRow("16. Frachtzahler fuer: SRB: VTG Rail\nLogistics Austria Gmbh", "VTG Rail Logistics Austria Gmbh")]
        [DataRow("16. Költségviselő SRB: Lokomotiva d.o.o.", "Lokomotiva d.o.o")]
        [DataRow("16. Platilac za Kombinovani prevoz: MBOX", "MBOX")]
        [DataRow("16. Frachtzahler: Budamar", "Budamar")]
        [DataRow("16. Frachtzahler HU: Kombinovani prevoz d.o.o.; Frachtzahler SRB: Lokomotiva d.o.o.", "Lokomotiva d.o.o")]
        [DataRow("16. Payer for MK/GR is KP 3212, payer for SRB is MBOX", "MBOX")]
        public void KP7_platilac_za_srbiju_pronadjen_bez_greske(string r7, string platilac)
        {
            var r = SaR7(r7);
            Assert.AreEqual("", Kp7(r), r7 + " | " + string.Join(" | ", Svi(r)));
            Assert.AreEqual("Platilac za deonicu kroz Srbiju: " + platilac + ".", Prvi(r, "KP7-INFO").Tekst, r7);
        }

        [DataTestMethod]
        [DataRow("16. Frachtzahler RS: Kombinovani Prevoz d.o.o., UIC 3212", "KP7-NIJE-PLATILAC")]
        [DataRow("16. Frachtzahler: Kombinovani prevoz", "KP7-NIJE-PLATILAC")]
        [DataRow("16. Payer for HU/SRB: KP", "KP7-NIJE-PLATILAC")]
        [DataRow("16. Frachtzahler HU: Kombinovani prevoz", "KP7-PLATILAC")]
        [DataRow("16: Payer for SZ is Ferest Rail,\nCIM 6§6: EVM 3832, KP 3212", "KP7-PLATILAC")]
        [DataRow("16. Beförderer strecke SRB KP 3212", "KP7-PLATILAC")]
        [DataRow("16. Prevoznik za deonicu SRB: KP", "KP7-PLATILAC")]
        [DataRow("16.IŽS: KP 3212; BDZ:BMRC 3057", "KP7-NEJASNO")]
        public void KP7_nalazi(string r7, string ocekivano)
        {
            var r = SaR7(r7);
            Assert.AreEqual(ocekivano, Kp7(r), r7 + " | " + string.Join(" | ", Svi(r)));
            Assert.IsFalse(Ima(r, "KP7-INFO"), "bez informacije kada platilac za Srbiju nije ispravno naveden");
        }

        [TestMethod]
        public void KP7_prazna_r7_je_greska_platilac()
        {
            var s = new Scenario(); s.C.R7 = new ListaRubrika();
            Assert.AreEqual("KP7-PLATILAC", Kp7(Proveri(s)));
        }

        [TestMethod]
        public void KP3_kp_bilo_gde_u_r13_bez_tacke_i_deonice()
        {
            foreach (var t in new[] { "3. SRB: KP UIC 3212", "KP 3212", "5. EVM 3832, KP 3212, PSP RO 3414", "Kombinovani prevoz" })
            {
                var s = new Scenario(); s.C.R13 = F(new ListaRubrika(), t);
                var r = Proveri(s);
                Assert.IsFalse(Ima(r, "KP3"), t); Assert.IsFalse(Ima(r, "G5"), t);
            }
            var bez = new Scenario(); bez.C.R13 = F(new ListaRubrika(), "1. via Šid gr.; 3. HŽC 2178");
            Assert.AreEqual(Tezina.Greska, Prvi(Proveri(bez), "KP3").Tezina);
        }

        [TestMethod]
        public void R58A_dovoljna_sifra_ili_naziv()
        {
            foreach (var (sifra, tekst) in new[] { ("3212", "3212"), ("", "Kombinovani prevoz"), ("3832", "3832 EVM"), ("", "EVM Rail") })
            {
                var s = new Scenario(); s.C.R58a = F(new UgovorniPrevoznik { Sifra = sifra }, tekst);
                var r = Proveri(s);
                Assert.IsFalse(Ima(r, "R58A"), tekst);
            }
            var kp = new Scenario(); kp.C.R58a = F(new UgovorniPrevoznik { Sifra = "3212" }, "3212");
            Assert.IsFalse(Ima(Proveri(kp), "KP2"), "KP2: 3212 je dovoljno, adresa i potpis se ne traže");
        }

        [TestMethod]
        public void R24_cetiri_cifre_je_preporuka_sa_sifrom_iz_T1()
        {
            var s = new Scenario(); s.C.R24.Stavke = new List<SifraTekst> { new SifraTekst { Sifra = "2620" } };
            s.Ostali.Add(new Dokument { Tip = TipDokumenta.TranzitnaDeklaracija, Fajl = "T1.pdf", Carinski = { new CarinskiPodaci { Tarife = { "262190" } } } });
            s.C.R9 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "820", Tekst = "T1" } } }, "820 T1");
            var r = Proveri(s);
            Assert.IsFalse(Ima(r, "R24"));
            var n = Prvi(r, "R24-NHM6");
            Assert.AreEqual(Tezina.Preporuka, n.Tezina);
            StringAssert.Contains(n.Tekst, "poželjno je upisati NHM šifru sa 6 cifara (u T1: 262190)");
            Assert.AreEqual("262190", n.Primer);
            var p = new Scenario(); p.C.R24.Stavke = new List<SifraTekst> { new SifraTekst { Sifra = "9922" } };
            Assert.AreEqual("992200", Prvi(Proveri(p), "R24-NHM6").Primer);
        }

        // ------------------------------------------------------------------------------------------- R25-MASA (v2.7.1)

        private static string R25Nalaz(Action<Scenario> izmena)
        {
            var s = new Scenario(); s.C.R25 = new Masa(); izmena(s);
            var n = Svi(Proveri(s)).Where(x => x.Rubrika == "25").ToList();
            return string.Join(",", n.Select(x => x.Pravilo + ":" + x.Tezina));
        }

        [TestMethod] public void R25_tovarena_kola_prazna_r25_greska() => Assert.AreEqual("R25-MASA:error", R25Nalaz(s => { }));
        [TestMethod] public void R25_nhm_9922_samo_preporuka() => Assert.AreEqual("R25-MASA:recommendation", R25Nalaz(s => s.C.R24.Stavke = new List<SifraTekst> { new SifraTekst { Sifra = "99 22" } }));
        [TestMethod] public void R25_cuv_u_r30_samo_preporuka() => Assert.AreEqual("R25-MASA:recommendation", R25Nalaz(s => { s.C.R30.Cim = false; s.C.R30.Cuv = true; s.C.Obrazac = "CUV"; }));
        [TestMethod] public void R25_r21_prazna_kola_samo_preporuka() => Assert.AreEqual("R25-MASA:recommendation", R25Nalaz(s => s.C.R21.Tekst = "20 prazna kola, serija Eanos"));

        [TestMethod]
        public void R25_masa_na_prilozenom_spisku_bez_nalaza()
        {
            Assert.AreEqual("", R25Nalaz(s =>
            {
                s.C.R25 = F(new Masa(), "po spisku kola");
                var d = Spisak(Kola("317866560015")); d.Spisak[0].MasaUpisana = true; s.Ostali.Add(d);
                s.C.R9 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "271", Tekst = "Spisak kola" } } }, "271 Spisak kola");
            }));
        }

        [TestMethod]
        public void R25_spisak_naveden_a_nije_prilozen_greska() => Assert.AreEqual("R25-MASA:error", R25Nalaz(s => s.C.R25 = F(new Masa(), "po spisku kola")));

        [TestMethod]
        public void R25_prazna_kola_poruka_i_primer()
        {
            var s = new Scenario(); s.C.R25 = new Masa(); s.C.R24.Stavke = new List<SifraTekst> { new SifraTekst { Sifra = "9922" } };
            var n = Prvi(Proveri(s), "R25-MASA");
            StringAssert.StartsWith(n.Tekst, "Rubrika 25: poželjno je upisati masu (taru) praznih kola.");
            Assert.AreEqual("Tara: 24 500 kg", n.Primer);
            Assert.AreEqual(0, Proveri(s).BrojGresaka + Proveri(s).BrojUpozorenja - Svi(Proveri(s)).Count(x => x.Tezina != Tezina.Preporuka && x.Tezina != Tezina.Info && x.Rubrika != "25"), "r25 prazna kola nije greška ni upozorenje");
        }

        [TestMethod]
        public void R25_necitljiva_je_upozorenje_citljivosti()
        {
            var s = new Scenario { IzvorCim = "ocr" }; s.C.R25 = F(new Masa(), "~~"); s.C.R25.Pouzdanost = 0.3;
            var r = Proveri(s);
            Assert.IsFalse(Ima(r, "R25-MASA"));
            Assert.IsTrue(Svi(r).Any(n => n.Pravilo == "CITLJIVOST" && n.Rubrika == "25" && n.Tezina == Tezina.Upozorenje));
        }

        // ------------------------------------------------------------------------------------------- A2: jedan nalaz = jedan red

        [TestMethod]
        public void Razlicita_pravila_iste_rubrike_su_posebni_redovi()
        {
            var s = new Scenario(); s.C.R23 = F(new TekstRubrika(), "X"); s.C.R21.UnBrojevi.Add("1203");
            s.C.R7 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "16", Tekst = "IŽS: KP 3212" } } }, "16.IŽS: KP 3212");
            var r = Proveri(s);
            var r7 = r.Listovi[0].Nalazi.Where(n => n.Rubrika == "7").ToList();
            CollectionAssert.AreEquivalent(new[] { "X2", "KP7-NEJASNO" }, r7.Select(n => n.Pravilo).ToList(), string.Join(" | ", r7));
            Assert.IsTrue(r7.All(n => n.Takodje.Count == 0));
            var telo = Izvestaj.Telo(r, pravila, false);
            Assert.AreEqual(2, telo.Split('\n').Count(l => l.StartsWith("") && l.Contains("Rubrika 7 – Izjave pošiljaoca")), telo);
        }

        [TestMethod]
        public void Nazivi_r26_r28_upozorenja_r27_greska()
        {
            var s = new Scenario(); s.C.R26 = F(new Iznos { Vrednost = "100", Valuta = "XXX" }); s.C.R27 = F(new Iznos { Vrednost = "100", Valuta = "" }); s.C.R28 = F(new Iznos { Vrednost = "", Valuta = "EUR" });
            var r = Proveri(s);
            Assert.AreEqual(Tezina.Upozorenje, r.Listovi[0].Nalazi.First(n => n.Rubrika == "26").Tezina);
            Assert.AreEqual(Tezina.Upozorenje, r.Listovi[0].Nalazi.First(n => n.Rubrika == "28").Tezina);
            Assert.AreEqual(Tezina.Greska, r.Listovi[0].Nalazi.First(n => n.Rubrika == "27").Tezina);
        }

        [TestMethod]
        public void KP57_samo_sifra_3212_je_dovoljna_a_3212_u_duzem_broju_nije()
        {
            var s = new Scenario(); s.C.R57 = F(new PrevozniciRubrika { Prevoznici = { new Prevoznik { Sifra = "3212", Deonica = "Šid - Dobova", StatusPrevoznika = "1" } } }, "3212   Šid - Dobova   1");
            s.C.R58a = new UgovorniPrevoznik(); s.C.R62.Preduzece = "2154";
            Assert.IsFalse(Ima(Proveri(s), "KP57"));
            var t = new Scenario(); t.C.R57 = F(new PrevozniciRubrika { Prevoznici = { new Prevoznik { Sifra = "2179", Naziv = "SŽ", Deonica = "tel. 0113212555" } } }, "2179 SŽ tel. 0113212555");
            t.C.R58a = new UgovorniPrevoznik(); t.C.R62.Preduzece = "2154";
            Assert.IsTrue(Ima(Proveri(t), "KP57"));
        }

        [TestMethod]
        public void Tezina_iz_json_a()
        {
            var s = new Scenario(); s.C.R16.Datum = ""; s.C.R20 = new TekstRubrika();
            var r = Proveri(s);
            Assert.AreEqual(Tezina.Greska, Prvi(r, "R16").Tezina);
            Assert.AreEqual(Tezina.Upozorenje, Prvi(r, "R20").Tezina);
        }

        [TestMethod]
        public void Preklapanje_prikazuje_prvo_pojavljivanje_ostala_kao_referenca()
        {
            var s = new Scenario(); DvaKola(s);
            var r = Proveri(s);
            var nalazi = r.Listovi[0].Nalazi;
            var spisak = nalazi.Single(n => n.Kljuc == "spisak-r9");
            Assert.AreEqual("G3", spisak.Pravilo, "prvo pojavljivanje po redu grupa");
            CollectionAssert.IsSubsetOf(new[] { "X9", "X20" }, spisak.Takodje.Select(t => t.Pravilo).ToList());
            Assert.AreEqual(1, nalazi.Count(n => n.Pravilo == "X9" || n.Pravilo == "G3"), "isti problem se ne ponavlja kao posebna stavka");
        }

        [TestMethod]
        public void R29_buducnost_i_X25_jedna_stavka()
        {
            var s = new Scenario(); s.C.R29.Datum = "2026-11-16"; s.C.R16.Datum = "2026-11-16";
            var n = Proveri(s).Listovi[0].Nalazi.Single(x => x.Kljuc == "r29-buducnost");
            Assert.AreEqual("R29", n.Pravilo);
            Assert.IsTrue(n.Takodje.Any(t => t.Pravilo == "X25"));
            Assert.AreEqual(Tezina.Greska, n.Tezina);
        }

        [TestMethod]
        public void X14_redosled_datuma_je_upozorenje_ne_greska_R29()
        {
            var s = new Scenario(); s.C.R29.Datum = "2026-11-06";
            var r = Proveri(s);
            Assert.AreEqual(Tezina.Upozorenje, Prvi(r, "X14").Tezina);
            Assert.IsFalse(Ima(r, "R29"));
        }

        [TestMethod]
        public void Necitljiva_rubrika_je_upozorenje_a_ne_greska_posiljaoca()
        {
            var s = new Scenario();
            s.C.R16.Status = StatusRubrike.Necitljivo; s.C.R16.Datum = "";
            var r = Proveri(s);
            Assert.IsFalse(Ima(r, "R16"), "nečitljivo ≠ greška pošiljaoca");
            var n = Prvi(r, "CITLJIVOST");
            Assert.AreEqual(Tezina.Upozorenje, n.Tezina);
            StringAssert.Contains(n.Tekst, "16");
            Assert.IsTrue(r.ZaRucnuProveru);
        }

        [TestMethod]
        public void Niska_pouzdanost_je_necitljivo()
        {
            var s = new Scenario(); s.C.R29.Mesto = ""; s.C.R29.Pouzdanost = 0.4;
            var r = Proveri(s);
            Assert.IsFalse(Ima(r, "R29"));
            Assert.IsTrue(Ima(r, "CITLJIVOST"));
        }

        [TestMethod]
        public void Prazno_nije_necitljivo()
        {
            var s = new Scenario(); s.C.R29 = new MestoDatum();
            var r = Proveri(s);
            Assert.IsTrue(Ima(r, "R29"));
            Assert.IsFalse(Ima(r, "CITLJIVOST"));
        }

        [TestMethod]
        public void Engleska_poruka()
        {
            var s = new Scenario(); s.C.R16.Datum = "";
            var u = new UlazProvere { Dokumenti = { new Dokument { Tip = TipDokumenta.Cim, Fajl = "a.pdf", Cim = { s.C } } }, RezimKp = "kp", ReferentniDatum = Danas, Engleski = true };
            var r = new Motor(pravila).Proveri(u);
            StringAssert.StartsWith(Prvi(r, "R16").Tekst, "Box 16");
        }

        [TestMethod]
        public void Vise_listova_isti_nalaz_jednom_u_poruci()
        {
            var a = IspravanCim(); a.R29.Datum = ""; a.RedniBroj = 1;
            var b = IspravanCim(); b.R29.Datum = ""; b.RedniBroj = 2;
            var u = new UlazProvere { Dokumenti = { new Dokument { Tip = TipDokumenta.Cim, Fajl = "TCDD.pdf", Cim = { a, b } } }, RezimKp = "kp", ReferentniDatum = Danas };
            var r = new Motor(pravila).Proveri(u);
            Assert.AreEqual(2, r.Listovi.Count);
            var telo = Izvestaj.Telo(r, pravila, false);
            Assert.AreEqual(1, telo.Split('\n').Count(l => l.Contains("Rubrika 29 –")), telo);
            StringAssert.Contains(telo, "[TCDD.pdf (svi listovi, 2)]");
            StringAssert.Contains(Izvestaj.Html(r, pravila, false), "TCDD.pdf (svi listovi, 2)");
        }

        [TestMethod]
        public void Naslov_po_sablonu()
        {
            var s = new Scenario(); s.C.R16.Datum = ""; s.C.R20 = new TekstRubrika();
            var r = Proveri(s);
            Assert.AreEqual("Tovarni list CIM 72 72345-6 3212 12345-6 (004NISBAR26) – potrebne ispravke", Izvestaj.Naslov(r, pravila, false), string.Join(" | ", Svi(r)));
            var samoUpoz = new Scenario(); samoUpoz.C.R20 = new TekstRubrika();
            Assert.AreEqual("Tovarni list CIM 72 72345-6 3212 12345-6 (004NISBAR26) – napomene", Izvestaj.Naslov(Proveri(samoUpoz), pravila, false));
            Assert.AreEqual("Tovarni list CIM 72 72345-6 3212 12345-6 (004NISBAR26) – ispravan", Izvestaj.Naslov(Proveri(new Scenario()), pravila, false));
            var bezRef = new Scenario(); bezRef.C.R8 = new TekstRubrika(); bezRef.C.R16.Datum = "";
            Assert.AreEqual("Tovarni list CIM 72 72345-6 3212 12345-6 – potrebne ispravke", Izvestaj.Naslov(Proveri(bezRef), pravila, false), "bez reference nema zagrade");
            Assert.AreEqual("Consignment note CIM 72 72345-6 3212 12345-6 (004NISBAR26) – corrections required", Izvestaj.Naslov(r, pravila, true));
        }

        [TestMethod]
        public void Mail_sa_tabelama_zahvalnicom_informacijom_i_potpisom()
        {
            var s = new Scenario(); s.C.R16.Datum = ""; s.C.R14 = new TekstRubrika(); s.C.R24.Stavke = new List<SifraTekst> { new SifraTekst { Sifra = "2620" } };
            var r = Proveri(s);
            var telo = Izvestaj.Telo(r, pravila, false);
            StringAssert.StartsWith(telo, "Poštovani,");
            StringAssert.Contains(telo, "zahvaljujemo na dostavljenoj dokumentaciji za pošiljku Niš – Dimitrovgrad gr (NHM 2620, referenca 004NISBAR26).");
            StringAssert.Contains(telo, "POTREBNE ISPRAVKE\r\n1. Rubrika 16 – Preuzimanje na prevoz\r\n   Šta nije u redu: ");
            StringAssert.Contains(telo, "Nedostaje: datum.");
            StringAssert.Contains(telo, "   Kako treba da izgleda (primer): ");
            StringAssert.Contains(telo, "NAPOMENE (molimo proverite)");
            StringAssert.Contains(telo, "Rubrika 14 – Broj sporazuma ili tarife");
            StringAssert.Contains(telo, "Rubrika 24 – NHM šifra");
            StringAssert.Contains(telo, "Platilac za deonicu kroz Srbiju: Lokomotiva d.o.o.");
            StringAssert.Contains(telo, "S poštovanjem,\r\n\r\nPriprema saobraćaja\r\nKombinovani prevoz d.o.o.\r\nJug Bogdanova 28, 18400 Prokuplje\r\npriprema@kprevoz.co.rs");
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(telo, @"\[(Ime|ime|Name|Telefon)[^\]]*\]"), "bez placeholdera");
            var html = Izvestaj.Html(r, pravila, false);
            foreach (var k in new[] { "<table", ">Rb.<", ">Rubrika<", ">Šta nije u redu<", ">Kako treba da izgleda (primer)<", "POTREBNE ISPRAVKE", "NAPOMENE (molimo proverite)", "16 – Preuzimanje na prevoz", "background-color:#d9d9d9", "border:1px solid", "Arial" })
                StringAssert.Contains(html, k.Replace("Š", System.Net.WebUtility.HtmlEncode("Š")), k);
            Assert.IsFalse(html.Contains("class=") || html.Contains("<img") || html.Contains("<script"), "bez CSS klasa, slika i skripti");
            Assert.IsFalse(html.Contains("[R16]") || html.Contains(">Pravilo<"), "šifre pravila se ne prikazuju");
            // engleski: iste tabele sa engleskim kolonama
            var en = Izvestaj.Html(r, pravila, true);
            foreach (var k in new[] { ">No.<", ">Box<", ">Issue<", ">Correct entry (example)<" }) StringAssert.Contains(en, k);
        }

        [TestMethod]
        public void Potpis_iz_podesavanja()
        {
            var stari = Izvestaj.Potpis;
            try
            {
                Izvestaj.Potpis = new PotpisIzvestaja { Ime = "Petar Petrović", Telefon = "+381 27 123 456" };
                var telo = Izvestaj.Telo(Proveri(new Scenario()), pravila, false);
                StringAssert.Contains(telo, "S poštovanjem,\r\n\r\nPetar Petrović\r\nPriprema saobraćaja\r\nTel. +381 27 123 456\r\nKombinovani prevoz d.o.o.");
            }
            finally { Izvestaj.Potpis = stari; }
        }

        [TestMethod]
        public void Nema_tovarnog_lista()
        {
            var u = new UlazProvere { Dokumenti = { new Dokument { Tip = TipDokumenta.SpisakKola, Spisak = { new SpisakKola() } } } };
            var r = new Motor(pravila).Proveri(u);
            Assert.IsFalse(r.ImaTovarniList);
            StringAssert.Contains(Izvestaj.NemaTovarnogLista(pravila, false), "nije pronađen tovarni list");
        }
    }
}
