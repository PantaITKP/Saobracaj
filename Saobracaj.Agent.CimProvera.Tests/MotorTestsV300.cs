using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>v3.0 – pravila iz PROMPT_v2.9.3: r23 (RID), r58a po izdavaocu, numeracija i poravnanje PRIMER-a.</summary>
    public partial class MotorTests
    {
        // ------------------------------------------------------------------------------------------- 1. r23 (RID)

        private static Scenario Rid(bool x, string[] unR21 = null, Dokument spisak = null)
        {
            var s = new Scenario();
            if (x) s.C.R23 = F(new TekstRubrika(), "X");
            if (unR21 != null) { s.C.R21.UnBrojevi.AddRange(unR21); s.C.R21.Tekst = "12 wagons loaded with: 268, " + string.Join(", ", unR21.Select(u => "UN " + u)) + ", Anhydrous ammonia, 2.3+8 (+13)"; }
            if (spisak != null) s.Ostali.Add(spisak);
            return s;
        }

        private static bool RidGreska(RezultatProvere r, string pravilo) => Svi(r).Any(n => n.Pravilo == pravilo && n.Tezina == Tezina.Greska);

        [TestMethod]
        public void RID_X_i_UN_u_r21_ispravno_i_prazna_kolona_RID_na_spisku_iste_robe()
        {
            // amonijak 52 290239 3057 001784: X u r23, UN 1005 u r21, spisak 12 kola NHM 281410 bez kolone RID
            var r = Proveri(Rid(true, new[] { "1005" }, Spisak(Kola("335278130331", "281410"), Kola("335278130026", "281410"))));
            Assert.IsFalse(Ima(r, "X21"), "prazna kolona RID kod iste robe nije greška");
            Assert.IsFalse(Ima(r, "X1"));
            Assert.IsFalse(Svi(r).Any(n => n.Rubrika == "21" && n.Tekst.Contains("RID")), string.Join(" | ", Svi(r)));
        }

        [TestMethod]
        public void RID_X_bez_UN_broja_greska_nedostaju_RID_podaci()
        {
            var r = Proveri(Rid(true));
            var n = Svi(r).FirstOrDefault(x => x.Rubrika == "21" && x.Tekst.Contains("nedostaju RID podaci"));
            Assert.IsNotNull(n, string.Join(" | ", Svi(r)));
            Assert.AreEqual(Tezina.Greska, n.Tezina);
            // UN broj samo na spisku (Kemler/UN po kolima) je dovoljan
            var sp = Spisak(Kola("335278130331", "281410")); sp.Spisak[0].Kola[0].KemlerUn = "268/1005";
            Assert.IsFalse(Svi(Proveri(Rid(true, null, sp))).Any(x => x.Tekst.Contains("nedostaju RID podaci")));
        }

        [TestMethod]
        public void RID_UN_bez_X_greska_treba_X_u_r23()
        {
            var r = Proveri(Rid(false, new[] { "1005" }));
            Assert.IsTrue(RidGreska(r, "X1"), string.Join(" | ", Svi(r)));
            StringAssert.Contains(Prvi(r, "X1").Tekst, "treba X u r23");
            // UN broj samo na spisku
            var sp = Spisak(Kola("335278130331", "281410")); sp.Spisak[0].UnBrojevi.Add("1005");
            Assert.IsTrue(RidGreska(Proveri(Rid(false, null, sp)), "X1"));
        }

        [TestMethod]
        public void RID_ni_X_ni_UN_nista()
        {
            var r = Proveri(Rid(false, null, Spisak(Kola("317866560015"))));
            Assert.IsFalse(Ima(r, "X1") || Ima(r, "X21") || Svi(r).Any(x => x.Tekst.Contains("RID")), string.Join(" | ", Svi(r)));
        }

        [TestMethod]
        public void RID_razlicita_opasna_roba_bez_RID_po_kolima_greska()
        {
            var r = Proveri(Rid(true, new[] { "1005", "1203" }, Spisak(Kola("317866560015", "281410"), Kola("317866560023", "271012"))));
            Assert.IsTrue(RidGreska(r, "X21"), string.Join(" | ", Svi(r)));
            StringAssert.Contains(Prvi(r, "X21").Tekst, "RID podaci po kolima");
            // RID podaci po kolima postoje → ispravno
            var sp = Spisak(Kola("317866560015", "281410"), Kola("317866560023", "271012"));
            sp.Spisak[0].Kola[0].KemlerUn = "268/1005"; sp.Spisak[0].Kola[1].KemlerUn = "33/1203";
            Assert.IsFalse(Ima(Proveri(Rid(true, new[] { "1005", "1203" }, sp)), "X21"));
        }

        [TestMethod]
        public void UN_broj_iz_teksta()
        {
            Assert.AreEqual("1005", Provere.UnBroj("UN 1005"));
            Assert.AreEqual("1005", Provere.UnBroj("UN-Nr. 1005"));
            Assert.AreEqual("1005", Provere.UnBroj("UN No. 1005"));
            Assert.AreEqual("1005", Provere.UnBroj("ООН 1005"));
            Assert.AreEqual("1005", Provere.UnBroj("268/1005"));
            Assert.IsNull(Provere.UnBroj("281410"));
        }

        // ------------------------------------------------------------------------------------------- 3. r58a po izdavaocu

        private static Scenario R58(string r62Preduzece, string r58a = null)
        {
            var s = new Scenario { Rezim = "spoljni" };
            s.C.R62 = F(new Identifikacija { Zemlja = "53", Stanica = "245129", Preduzece = r62Preduzece, Broj = "00139-4" });
            if (string.IsNullOrEmpty(r62Preduzece)) s.C.R62.Pouzdanost = 0.4;
            s.C.R57 = F(new PrevozniciRubrika { Prevoznici = { new Prevoznik { Sifra = "3154", Naziv = "Unicom Tranzit", Deonica = "Constanta - Stamora" }, new Prevoznik { Sifra = "3212", Naziv = "KP", Deonica = "Vršac - Vreoci" } } },
                "3154 Unicom Tranzit Constanta - Stamora\n3212 KP Vršac - Vreoci");
            s.C.R58a = r58a == null ? new UgovorniPrevoznik() : F(new UgovorniPrevoznik(), r58a);
            return s;
        }

        [TestMethod]
        public void R58a_prazna_na_KP_listu_greska_predlog_3212()
        {
            var s = R58("3212"); s.Rezim = "kp";
            var r = Proveri(s);
            var n = Svi(r).Single(x => x.Rubrika == "58a");
            Assert.AreEqual("KP2", n.Pravilo);
            Assert.AreEqual(Tezina.Greska, n.Tezina);
            StringAssert.Contains(n.Primer + n.Dopuna, "3212");
        }

        [TestMethod]
        public void R58a_prazna_na_listu_drugog_prevoznika_upozorenje_predlog_iz_r62_nikad_3212()
        {
            // Coal 17 PSP: r62 53 245129 3154 001394 (Unicom Tranzit)
            var r = Proveri(R58("3154"));
            var n = Svi(r).Single(x => x.Rubrika == "58a");
            Assert.AreEqual(Tezina.Upozorenje, n.Tezina, n.ToString());
            Assert.AreEqual("3154 Unicom Tranzit", n.Primer);
            Assert.IsFalse((n.Primer + " " + n.Dopuna + " " + n.Tekst).Contains("3212"), "nikad 3212 za tuđi list: " + n.Tekst + " / " + n.Primer + " / " + n.Dopuna);
        }

        [TestMethod]
        public void R58a_predlog_bez_izmisljenog_naziva_oznaka_mreze_nije_naziv()
        {
            // amonijak 001792 (OCR): r13 „13.3 BDŽ: 3057; 3212“, r57 „3057 B …“ – naziv nije pročitan → predlog samo šifra, nikad „BDZ“
            var s = R58("3057");
            s.C.R57 = F(new PrevozniciRubrika { Prevoznici = { new Prevoznik { Sifra = "3057", Naziv = "B", Deonica = "Povelyanovo - Dragoman gr." } } }, "3057 B   Povelyanovo - Dragoman gr.");
            s.C.R13 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "3", Tekst = "BDŽ:   3057; 3212" } } }, "13.3 BDŽ:   3057;   3212");
            Assert.AreEqual("3057", Svi(Proveri(s)).Single(x => x.Rubrika == "58a").Primer);
            // r13 „BDŽ: BMRC 3057“ – naziv iz dokumenta
            s.C.R13 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "3", Tekst = "BDŽ: BMRC 3057; RS: KP 3212" } } }, "13.3 BDŽ: BMRC 3057; RS: KP 3212");
            Assert.AreEqual("3057 BMRC", Svi(Proveri(s)).Single(x => x.Rubrika == "58a").Primer);
        }

        [TestMethod]
        public void R58a_popunjena_bilo_kojim_prevoznikom_na_tudjem_listu_ispravno()
        {
            Assert.IsFalse(Svi(Proveri(R58("3154", "3154"))).Any(x => x.Rubrika == "58a"));
            Assert.IsFalse(Svi(Proveri(R58("3154", "2154 SŽ Cargo"))).Any(x => x.Rubrika == "58a"));
            Assert.IsFalse(Svi(Proveri(R58("3154", "Unicom Tranzit"))).Any(x => x.Rubrika == "58a"));
        }

        [TestMethod]
        public void R58a_prazna_r62_necitljiv_upozorenje_proveriti_bez_3212()
        {
            var r = Proveri(R58(""));
            var n = Svi(r).Single(x => x.Rubrika == "58a");
            Assert.AreEqual(Tezina.Upozorenje, n.Tezina);
            StringAssert.Contains(n.Tekst, "proveriti");
            Assert.IsFalse((n.Primer + " " + n.Dopuna).Contains("3212"), n.Primer + " / " + n.Dopuna);
        }

        [TestMethod]
        public void R58a_na_KP_listu_pecat_UIC_CODE_KP_3212_ispravno()
        {
            var s = R58("3212", "UIC CODE KP 3212"); s.Rezim = "kp";
            Assert.IsFalse(Svi(Proveri(s)).Any(x => x.Rubrika == "58a"));
        }

        // ------------------------------------------------------------------------------------------- 2. numeracija i poravnanje PRIMER-a

        [TestMethod]
        public void Numeracija_od_1_na_svakom_tovarnom_listu()
        {
            Izvestaj.Red R(params string[] l) => new Izvestaj.Red { Listovi = l.ToList() };
            var redovi = Izvestaj.Numerisi(new List<Izvestaj.Red> { R("a", "b"), R("a", "b"), R("a"), R("a"), R("b"), R("b"), R("b") });
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 3, 4, 5 }, redovi.Select(x => x.Broj).ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, Izvestaj.Numerisi(new List<Izvestaj.Red> { R("a"), R("a"), R("a") }).Select(x => x.Broj).ToArray());
        }

        [TestMethod]
        public void PRIMER_okviri_van_strane_se_uklapaju_u_stranu()
        {
            var okviri = new Dictionary<string, double[]> { ["9"] = new[] { 0.52, 0.20, 1.04, 0.28 }, ["21"] = new[] { -0.03, 0.43, 0.60, 0.63 }, ["62"] = new[] { 0.72, 0.88, 1.02, 0.93 } };
            Assert.IsTrue(PrimerPdf.Prepust(okviri) > 0.03);
            var u = PrimerPdf.UStranu(okviri);
            Assert.AreEqual(0, PrimerPdf.Prepust(u), 1e-9);
            Assert.IsTrue(u.Values.All(o => o[0] >= 0 && o[2] <= 1 && o[1] >= 0 && o[3] <= 1));
            // raspored ostaje: r21 levo od r9, r62 dole desno
            Assert.IsTrue(u["21"][0] < u["9"][0] && u["62"][1] > u["9"][3]);
            Assert.AreEqual(0, PrimerPdf.Prepust(new Dictionary<string, double[]> { ["1"] = new[] { 0.1, 0.1, 0.5, 0.2 } }));
        }
    }
}
