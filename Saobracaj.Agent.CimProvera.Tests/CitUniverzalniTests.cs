using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>Pomoćne funkcije univerzalnog CIT šablona: broj kola iz teksta, nazivi primeraka, ključevi brojeva rubrika.</summary>
    [TestClass]
    public class CitUniverzalniTests
    {
        [DataTestMethod]
        [DataRow("Nr. 30 empty private wagons NHM 9922", 30)]
        [DataRow("Lásd kocsijegyzéken (6 Kocsi) / Siehe Wagenliste (6 Wg)", 6)]
        [DataRow("30 kola tovarenih sa LETEĆIM PEPELOM", 30)]
        [DataRow("22 kola otpada", 22)]
        [DataRow("UN 1791 Hipoklorit oldat, 8, II", 0)]
        public void Broj_kola_iz_r21(string tekst, int ocekivano) => Assert.AreEqual(ocekivano, Provere.BrojKolaUTekstu(tekst));

        private static Strana Strana(params (string t, double cy)[] reci)
        {
            var s = new Strana { Broj = 2 };
            foreach (var (t, cy) in reci) s.Reci.Add(new Rec { T = t, X0 = 0.3, X1 = 0.35, Y0 = cy - 0.005, Y1 = cy + 0.005 });
            return s;
        }

        [TestMethod]
        public void Naziv_primerka_IT_FR() => Assert.AreEqual("Foglio di via / Feuille de route", LokalniCitac.NazivPrimerka(Strana(("Foglio", 0.97), ("di", 0.97), ("via", 0.97), ("Feuille", 0.98), ("de", 0.98), ("route", 0.98))));

        [TestMethod]
        public void Naziv_primerka_duplicata() => Assert.AreEqual("Duplicato / Duplicata", LokalniCitac.NazivPrimerka(Strana(("Duplicato", 0.97), ("Duplicata", 0.98))));

        [TestMethod]
        public void Naziv_primerka_samo_u_dnu_strane() => Assert.IsNull(LokalniCitac.NazivPrimerka(Strana(("Duplicata", 0.40))));

        [DataTestMethod]
        [DataRow("71#2", "71")]
        [DataRow("1#leg", "1")]
        [DataRow("62", "62")]
        public void Osnova_broja_rubrike(string kljuc, string osnova) => Assert.AreEqual(osnova, ObrazacSablon.OsnovaBroja(kljuc));
    }
}
