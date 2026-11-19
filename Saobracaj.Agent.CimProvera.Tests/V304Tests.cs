using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>v3.0.4 – merilo pečata (tri stanja i znak), NHM koje nema u tarifi nije znak vrste dokumenta.</summary>
    [TestClass]
    public class V304Tests
    {
        private static List<Rec> R(params (string t, double p)[] reci) => reci.Select(x => new Rec { T = x.t, Pouzdanost = x.p }).ToList();

        /// <summary>15 slika iz paketa 3.0.3 (reči r58a i mastilo iz obrade) sa ishodom koji je korisnik utvrdio pregledom slika.</summary>
        [TestMethod]
        public void Pecat_15_slika_iz_paketa_303()
        {
            var slucajevi = new (string opis, List<Rec> reci, double mast, string ocekivano)[]
            {
                ("UID 3 str. 2 – r58a prazna", R(("jjjjjjjj", 0.3), ("р", 0.4)), 0.0012, "ne"),
                ("UID 34 – kucano „0022, UZ“", R(("58", 0.9), ("a)", 0.9), ("0022,", 0.95), ("UZ", 0.93)), 0.0032, "ne"),
                ("UID 39 – okrugli pečat PSP Cargo", R(("a)", 0.9), ("capte", 0.33), ("<", 0.77), ("res", 0.4), ("(car", 0.5), ("‘eur", 0.6), ("jj", 0.7), ("ln", 0.7), ("Pr", 0.7)), 0.0273, "da"),
                ("UID 42 – kucano „0022, UZ“", R(("58", 0.5), ("a)", 0.9), ("-", 0.9), ("0022,", 0.97), ("UZ", 0.97)), 0.0043, "ne"),
                ("UID 45 – kucano „0022, UZ“", R(("inne", 0.4), ("58", 0.9), ("a)", 0.9), ("0022,", 0.91), ("UZ", 0.91)), 0.0036, "ne"),
                ("UID 82 – KP pečat (PZ 009, UIC)", R(("prdvdg", 0.3), ("602727", 0.4), ("PZ", 0.5), ("009", 0.8), ("UIC", 0.8), ("jan", 0.4)), 0.1073, "da"),
                ("UID 97 – KP pečat sa datumom", R(("jj", 0.3), ("bi", 0.4), ("13", 0.93), ("4", 0.93), ("2026", 0.93)), 0.0253, "da"),
                ("UID 113 – kucano „0072“", R(("a)", 0.94), ("tractual", 0.94), ("Akdi", 0.94), ("0072", 0.94)), 0.0, "ne"),
                ("UID 3 str. 3 – r58a prazna", R(("ma", 0.3), ("yen", 0.5), ("on", 0.4)), 0.0018, "ne"),
                ("UID 3 str. 5 – r58a prazna", R(), 0.002, "ne"),
                ("UID 28 – KP pečat sa datumom", R(("a)", 0.9), ("UIC", 0.9), ("KP", 0.9), ("3212", 0.9), ("PZ", 0.9), ("jr", 0.4), ("009", 0.5), ("14.", 0.9), ("11", 0.9), ("2026", 0.9)), 0.0633, "da"),
                ("UID 52 – r58a prazna", R(), 0.0002, "ne"),
                ("UID 76 – KP pečat", R(("„Kjmbindvanji", 0.5), ("prevoz“", 0.8), ("UIC:3212,", 0.8), ("18400", 0.9), ("Prokuplje", 0.7), ("ferris!", 0.3), ("jnjjrscrijt", 0.2)), 0.0556, "da"),
                ("UID 174 – kucano „3212 KP“ (italijanski natpis)", R(("3212", 0.95), ("LU", 0.95)), 0.0302, "ne"),
                // obrada 3.0.4 (L27): OCR je iz istog okruglog pečata pročitao druge reči – fragmenti „j r j j j“
                ("UID 39 – okrugli pečat, fragmenti", R(("a)", 0.9), ("j", 0.7), ("r", 0.7), ("j", 0.7), ("j", 0.7), ("j", 0.7)), 0.027, "da"),
            };
            var greske = new List<string>();
            foreach (var (opis, reci, mast, ocekivano) in slucajevi)
            {
                var (st, znak) = CimParser.Pecat58a(reci, mast);
                if (st != ocekivano) greske.Add(opis + ": " + st + " (" + znak + "), očekivano " + ocekivano);
                Assert.IsFalse(string.IsNullOrWhiteSpace(znak), opis + ": znak odluke");
            }
            Assert.AreEqual(0, greske.Count, string.Join("\n", greske));
        }

        /// <summary>v3.0.4b – pregled korisnika slika iz paketa 3.0.4b (UID 62, 230, 146, 6/72) i pravne forme.</summary>
        [TestMethod]
        public void Pecat_elektronska_overa_potpis_pravne_forme_i_tamna_strana()
        {
            // UID 62: kucano „ČD Cargo, a.s.“ nije pečat, ali kod u polju potpisa sa delovima r62 i datumom jeste elektronska overa
            var r62 = new Identifikacija { Zemlja = "54", Stanica = "547893", Preduzece = "2154", Broj = "500892" };
            var reci62 = R(("Îsa", 0.73), ("a)", 0.93), ("-", 0.76), ("2154", 0.97), ("CD", 0.95), ("Cargo,", 0.92), ("a.s.", 0.91), ("ČD", 0.93), ("Cargo,", 0.93), ("a.s.", 0.92),
                           ("Jankovcova", 0.92), ("1569/2c", 0.91), ("17000", 0.96), ("Praha", 0.97), ("7", 0.97), ("54-547893-50089-2154-20261108", 0.52));
            var e = CimParser.Pecat58a(reci62, 0.0698, r62);
            Assert.AreEqual("da", e.stanje); StringAssert.Contains(e.znak, "elektronska overa");
            // bez koda: pravne forme i skraćenice velikim slovima nisu fragmenti pečata → kucani naziv prevoznika, nema
            var bezKoda = reci62.Take(reci62.Count - 1).ToList();
            Assert.AreEqual("ne", CimParser.Pecat58a(bezKoda, 0.0698, r62).stanje, CimParser.Pecat58a(bezKoda, 0.0698, r62).znak);
            // UID 230: rukom pisano ime, potpis, datum i vreme – OCR čita samo šum rukopisa
            var p = CimParser.Pecat58a(R(("UjU", 0.36), ("j", 0.93), ("g", 0.55), ("2", 0.65), ("U", 0.81)), 0.0484);
            Assert.AreEqual("da", p.stanje); StringAssert.Contains(p.znak, "potpis");
            // UID 146: samo kucano „2151 PKP Cargo Spolka Akcyjna…“ (OCR pročitao samo natpis „Podpis“) – nema
            Assert.AreEqual("ne", CimParser.Pecat58a(R(("jojpis-", 0.57)), 0.0328).stanje);
            // UID 6, 72: strana pretamna – nesigurno
            var t = CimParser.Pecat58a(R(), 0.9627);
            Assert.AreEqual("nesigurno", t.stanje); StringAssert.StartsWith(t.znak, "strana pretamna");
            // UID 198, 203: nagnut bled okvir – bez prepoznatog okvira ostaje nesigurno, sa prepoznatim okvirom postoji
            var r198 = R(("je", 0.54), ("rar", 0.60), ("е", 0.48), ("[", 0.85), ("janjc,", 0.46), ("7", 0.59), ("p,", 0.77), ("Sa,", 0.54), ("ce", 0.79), ("b", 0.52), ("signa", 0.57));
            Assert.AreEqual("nesigurno", CimParser.Pecat58a(r198, 0.0174).stanje);
            Assert.AreEqual("da", CimParser.Pecat58a(r198, 0.0174, null, 1).stanje);
        }

        [TestMethod]
        public void Pecat_kucani_KP_naziv_bez_pecata_nije_dokaz_a_ista_strana_daje_isti_rezultat()
        {
            // kucano „3212 Kombinovani prevoz“ – jedan znak teksta KP pečata (naziv), bez datuma i otiska: nije pečat
            Assert.AreEqual("ne", CimParser.Pecat58a(R(("3212", 0.95), ("Kombinovani", 0.95), ("prevoz", 0.95)), 0.02).stanje);
            // ista strana obrasca (isti ulaz) – isti rezultat
            var a = CimParser.Pecat58a(R(("ma", 0.3), ("yen", 0.5)), 0.0018);
            var b = CimParser.Pecat58a(R(("ma", 0.3), ("yen", 0.5)), 0.0018);
            Assert.AreEqual(a, b);
            // nejasno: mastilo bez čitljivih reči i bez otiska – nesigurno (greši se u korist upozorenja)
            Assert.AreEqual("nesigurno", CimParser.Pecat58a(R(("x", 0.3), ("ab", 0.4)), 0.015).stanje);
        }

        [TestMethod]
        public void NHM_koje_nema_u_tarifi_nije_znak_vrste_dokumenta()
        {
            var stare = Provere.NhmGrupe;
            try
            {
                Provere.NhmGrupe = new HashSet<string>(Enumerable.Range(1000, 1200).Select(x => x.ToString()).Where(x => !x.StartsWith("942")));
                var c = MotorTests.IspravanCim();
                c.R30 = new OznakaDokumenta { Cuv = true, Status = StatusRubrike.Popunjeno, Tekst = "CUV X", Pouzdanost = 0.95 };
                c.R24 = new ListaRubrika { Status = StatusRubrike.Popunjeno, Tekst = "942290", Stavke = { new SifraTekst { Sifra = "942290" } } };
                var k = new Kontekst(c, new Dokument { Fajl = "1854_261114114046_001.pdf", Izvor = "ocr" }, new UlazProvere { RezimKp = "spoljni" }, null);
                var v = Provere.VrstaDokumenta(k);
                Assert.AreEqual("CUV", v.vrsta); Assert.IsFalse(v.nesigurna, v.opis);
                c.R24 = new ListaRubrika { Status = StatusRubrike.Popunjeno, Tekst = "270400", Stavke = { new SifraTekst { Sifra = "270400" } } };
                Provere.NhmGrupe.Add("2704");
                Assert.IsTrue(Provere.VrstaDokumenta(new Kontekst(c, new Dokument { Fajl = "x.pdf", Izvor = "ocr" }, new UlazProvere { RezimKp = "spoljni" }, null)).nesigurna, "postojeći NHM robe i dalje je suprotan znak (bez znaka praznih kola)");
            }
            finally { Provere.NhmGrupe = stare; }
        }
    }
}
