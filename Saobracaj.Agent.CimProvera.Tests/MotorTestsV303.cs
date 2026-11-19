using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera.Tests
{
    public partial class MotorTests
    {
        // ------------------------------------------------------------------------------------------- v3.0.3 (1.2, 1.3)

        [TestMethod]
        public void CUV_sa_NHM_prethodnog_tovara_i_praznim_kolima_nije_nesigurna_vrsta()
        {
            // Lukavac02 (UID 45/75), ID 1661 (UID 162): X u kućici CUV, r24 NHM robe (prethodni tovar), prazna kola / tara bez neta
            foreach (var (r21, r25) in new[] { ("Leere Wagen / prazna kola, prethodni tovar koks", "Tara 23 500"), ("Koks", "Tara: 23 500 kg") })
            {
                var s = new Scenario();
                s.C.R30 = F(new OznakaDokumenta { Cuv = true }, "CUV X");
                s.C.R24 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "270400", Tekst = "Koks" } } }, "270400");
                s.C.R21 = F(new NaznacenjeRobe(), r21);
                s.C.R25 = F(new Masa(), r25);
                var r = Proveri(s);
                Assert.IsFalse(Ima(r, "VRSTA-DOKUMENTA"), r21 + ": " + string.Join(" | ", Svi(r)));
                Assert.IsFalse(Ima(r, "KP7-PLATILAC") || Ima(r, "KP3"), "CUV – bez platioca i KP3: " + string.Join(" | ", Svi(r)));
            }
            // bez znaka praznih kola (neto robe u r25) – NHM robe ostaje suprotan znak
            var n = new Scenario();
            n.C.R30 = F(new OznakaDokumenta { Cuv = true }, "CUV X");
            n.C.R24 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "270400", Tekst = "Koks" } } }, "270400");
            n.C.R21 = F(new NaznacenjeRobe(), "Koks");
            n.C.R25 = F(new Masa(), "Netto 41 050\nTara 21 900\nBrutto 62 950");
            Assert.IsTrue(Ima(Proveri(n), "VRSTA-DOKUMENTA"));
        }

        [TestMethod]
        public void KP3_izdavalac_3212_u_r62_znaci_da_je_KP_naveden()
        {
            // odluka korisnika 19.11.: KP kao izdavalac (3212 u r62) = KP je naveden – upozorenje „navesti KP i u rubrici 13“, ne greška
            var s = new Scenario(); s.C.R13 = F(new ListaRubrika(), "1. via Šid gr.; 3. HŽC 2178");
            BezKpVanR13(s); s.C.R62.Preduzece = "3212";
            var kp3 = Prvi(Proveri(s), "KP3");
            Assert.AreEqual(Tezina.Upozorenje, kp3.Tezina);
            StringAssert.Contains(kp3.Tekst, "62");
        }
    }
}
