using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>Broj listova u izveštaju (gramatika) i napomena o ručnoj proveri kada obrazac nije prepoznat.</summary>
    [TestClass]
    public class IzvestajListoviTests
    {
        [DataTestMethod]
        [DataRow(1, "1 tovarni list")]
        [DataRow(2, "2 tovarna lista")]
        [DataRow(4, "4 tovarna lista")]
        [DataRow(5, "5 tovarnih listova")]
        [DataRow(11, "11 tovarnih listova")]
        [DataRow(12, "12 tovarnih listova")]
        [DataRow(21, "21 tovarni list")]
        [DataRow(22, "22 tovarna lista")]
        public void Broj_listova_gramatika(int n, string ocekivano) => Assert.AreEqual(ocekivano, Izvestaj.BrojListova(n, false));

        private static RezultatProvere Rez(int listova, int dodatnih, string neprepoznat = "")
        {
            var r = new RezultatProvere { Dokumenti = new List<Dokument>() };
            for (int i = 0; i < listova; i++)
                r.Listovi.Add(new RezultatLista { Cim = new CimList { NeprepoznatObrazac = neprepoznat }, Oznaka = "a.pdf – list " + (i + 1), ZaRucnuProveru = neprepoznat.Length > 0 });
            for (int i = 0; i < dodatnih; i++) r.Dokumenti.Add(new Dokument { Tip = TipDokumenta.DodatniList });
            return r;
        }

        [TestMethod]
        public void Jedan_list_i_dodatni_list() => Assert.AreEqual("1 tovarni list + dodatni list", Izvestaj.OpisListova(Rez(1, 1), false));

        [TestMethod]
        public void Vise_listova_i_dodatnih() => Assert.AreEqual("5 tovarnih listova + 2 dodatna lista", Izvestaj.OpisListova(Rez(5, 2), false));

        [TestMethod]
        public void Neprepoznat_obrazac_nije_proveren()
        {
            var t = Izvestaj.RucnaProvera(Rez(1, 0, "obrazac nije prepoznat"), false).ToList();
            CollectionAssert.AreEqual(new[] { "Tovarni list nije proveren jer obrazac nije prepoznat; dokument je stavljen na ručnu proveru." }, t);
        }

        [TestMethod]
        public void Nepouzdane_rubrike_stara_napomena()
        {
            var r = Rez(1, 0); r.Listovi[0].ZaRucnuProveru = true;
            CollectionAssert.AreEqual(new[] { "Neke rubrike nisu pouzdano pročitane; dokument je stavljen i na ručnu proveru." }, Izvestaj.RucnaProvera(r, false).ToList());
        }
    }
}
