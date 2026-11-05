using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.TelegramiImport.Data;
using Saobracaj.Agent.TelegramiImport.Parsing;

namespace Saobracaj.Agent.TelegramiImport.Tests
{
    /// <summary>Testovi bez PDF-ova i bez baze (mogu u repo).</summary>
    [TestClass]
    public class ParserBezFajlovaTests
    {
        private static readonly DateTime Ref = new DateTime(2026, 10, 30);

        private static VremenskiIzraz V(string t) { return VremenskiIzraz.Parsiraj(t, Ref); }

        [TestMethod]
        public void ListaDana_MesecIGodinaNaKraju()
        {
            var v = V("dana 02, 03, 04, 05. i 06.11.2026. godine odobrava se zatvor od 11.00 do 13.50 casova");
            CollectionAssert.AreEqual(Enumerable.Range(2, 5).Select(d => new DateTime(2026, 11, d)).ToList(), v.Dani.ToList());
            Assert.AreEqual(1, v.Intervali.Count);
            Assert.AreEqual(new TimeSpan(11, 0, 0), v.Intervali[0].Od);
        }

        [TestMethod]
        public void VisNoci()
        {
            var v = V("za dane 26/27. i 27/28.10.2026 od 23:00 do 03:00");
            CollectionAssert.AreEqual(new[] { new DateTime(2026, 10, 26), new DateTime(2026, 10, 27) }, v.Noci.ToList());
            Assert.IsTrue(v.Intervali[0].PrekoPonoci);
        }

        [TestMethod]
        public void NociBezReciDana_IGodinaSpojena()
        {
            var v = V("izmedu ukr.veliki borak i st.lazarevac 30/31.10. 2026godine u vremenu od 23:00 do 03:00 casova");
            CollectionAssert.AreEqual(new[] { new DateTime(2026, 10, 30) }, v.Noci.ToList());
        }

        [TestMethod]
        public void ListaNociSaMesecomPosleSvake()
        {
            var v = V("za dane 28/29.10, 29/30.10, 30/31.10.2026.godina u vremenskom intervalu od 23.00 do 03.00 casova");
            Assert.AreEqual(3, v.Noci.Count);
            Assert.AreEqual(new DateTime(2026, 10, 28), v.Noci.Min);
        }

        [TestMethod]
        public void OpsegBezSubotaINedelja()
        {
            var v = V("u periodu od 01.11 do 30.11.2026. godine od 08.00 do 18.00 casova (osim subote i nedelje)");
            Assert.AreEqual(21, v.Dani.Count);
            Assert.IsFalse(v.Dani.Any(d => d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday));
            Assert.AreEqual(5, v.Nizovi().Count(), "5 radnih nedelja = 5 redova");
        }

        [TestMethod]
        public void IntervalBezReciOd()
        {
            var v = V("dana 29.10.2026. godine odobrava se zatvor 3. i 4. koloseka u vremenskom intervalu 00:30 do 02.30 casova");
            Assert.AreEqual(1, v.Intervali.Count);
            Assert.AreEqual(new TimeSpan(0, 30, 0), v.Intervali[0].Od);
            Assert.AreEqual(new TimeSpan(2, 30, 0), v.Intervali[0].Do);
        }

        [TestMethod]
        public void DoOpoziva()
        {
            var v = V("dana 30.10.2026. godine sa vaznoscu odmah u stanici korman zabranjuje se saobracaj");
            Assert.IsTrue(v.DoOpoziva);
            CollectionAssert.AreEqual(new[] { new DateTime(2026, 10, 30) }, v.Dani.ToList());
        }

        [TestMethod]
        public void DatumNijeVreme()
        {
            var v = V("u periodu od 01.11 do 30.11.2026");
            Assert.AreEqual(0, v.Intervali.Count);
            Assert.AreEqual(30, v.Dani.Count);
        }

        [TestMethod]
        public void Kolosek_PrviNavodPosleZatvora()
        {
            Assert.AreEqual("levi", TelegramParser.Kolosek("odobrava se zatvor levog koloseka otvorena pruga batajnica - nova pazova, zatvor skretnica br. 2 i 10", Vrsta.Zatvor));
            Assert.AreEqual("skretnica br.2", TelegramParser.Kolosek("odobrava se zatvor skretnice broj 2 u st. bela reka", Vrsta.Zatvor));
            Assert.AreEqual("1,2,5,6,7,8", TelegramParser.Kolosek("odobrava se zatvor koloseka br. 1, 2, 5, 6, 7 i 8 stanice vrbas", Vrsta.Zatvor));
            Assert.AreEqual("svi", TelegramParser.Kolosek("odobrava se zatvor svih koloseka stanice vrbas", Vrsta.Zatvor));
        }

        [TestMethod]
        public void Lokacija_PrvaRelacijaPosleZatvora()
        {
            var l = TelegramParser.Lokacija("odobrava se zatvor koloseka br. 1, 2 stanice Vrbas, dok ce se ... na delu pruge Vrbas Nova - Vrbas radi");
            Assert.AreEqual("Vrbas", l.Item1);
            Assert.AreEqual("Vrbas", l.Item2);
            l = TelegramParser.Lokacija("Izmedu sluzbenih mesta Lajkovac-Slovac-Divci - Valjevo za dan 27/28.10.2026");
            Assert.AreEqual("Lajkovac", l.Item1);
            Assert.AreEqual("Valjevo", l.Item2);
        }

        [TestMethod]
        public void PodnasloviZaDan_RazlicitiIntervaliPoDanu_21F()
        {
            var tekst = string.Join("\n",
                "TELEGRAM BROJ 21F", "BEOGRAD 03.11.2026. godine",
                "Na pruzi (Beograd Centar) - Resnik - Požega - Vrbnica - državna granica - (Bijelo Polje), zbog zamene i",
                "zavarivanja isečenog DTŠ -a, odobrava se zatvor koloseka između sl.mesta Lazarevac i Lajkovac u",
                "sledećim vremenskim intervalima:",
                "za dan 04.11.2026.godine:",
                " -od 09.15 do 10.30 časova,tj. između vozova 2100 do 615,",
                " -od 12.35 do 13.25 časova,tj. između vozova 2102 do 2101,",
                "za dan 05.11.2026.godine:",
                " -od 09.15 do 10.30 časova,tj. između vozova 2100 do 615,",
                " -od 10.45 do 12.00 časova,tj. između vozova 615 do 713,",
                "Nadležno operativno odeljenje preduzeće operativne mere");
            var t = TelegramParser.Parsiraj(tekst);
            Assert.AreEqual(21, t.Broj);
            Assert.AreEqual(4, t.Stavke.Count);
            Assert.IsTrue(t.Stavke.All(s => s.OdTekst == "Lazarevac" && s.DoTekst == "Lajkovac"));
            Assert.AreEqual(new DateTime(2026, 11, 4, 12, 35, 0), t.Stavke[1].VaziOd);
            Assert.AreEqual(new DateTime(2026, 11, 4, 13, 25, 0), t.Stavke[1].VaziDo);
            Assert.AreEqual(new DateTime(2026, 11, 5, 10, 45, 0), t.Stavke[3].VaziOd);
        }

        // ------------------------------------------------------------------ šifarnik

        private static Sifarnik Sif()
        {
            var st = new[]
            {
                new Sifarnik.Stanica { ID = 171, Opis = "Kosijerić" },
                new Sifarnik.Stanica { ID = 300, Opis = "Ražana" },
                new Sifarnik.Stanica { ID = 273, Opis = "Preševo gr." },
                new Sifarnik.Stanica { ID = 500, Opis = "Ristovac" },
                new Sifarnik.Stanica { ID = 55, Opis = "Bor teretna" },
                new Sifarnik.Stanica { ID = 54, Opis = "Bor" },
                new Sifarnik.Stanica { ID = 600, Opis = "Rasputnica Ćuprija" },
                new Sifarnik.Stanica { ID = 601, Opis = "Ćuprija" },
                new Sifarnik.Stanica { ID = 405, Opis = "Pančevo Vojlovica" },
                new Sifarnik.Stanica { ID = 1088, Opis = "Pančevo Vojlovica" },
            };
            var pr = new[]
            {
                new Sifarnik.Pruga { ID = 2, Opis = "Beograd - Mladenovac - Lapovo - Nis - Presevo - drzavna granica - (Tabanovce)" },
                new Sifarnik.Pruga { ID = 11, Opis = "Beograd Centar - Rasputnica G - (Rakovica)" },
                new Sifarnik.Pruga { ID = 7, Opis = "(Beograd) - Resnik - Pozega - Vrbnica - drzavna granica - (Bijelo Polje)" },
                new Sifarnik.Pruga { ID = 35, Opis = " Banatsko Milosevo - Senta - Subotica" },
                new Sifarnik.Pruga { ID = 96, Opis = "Banatsko Milosevo-Senta-Subotica" },
            };
            var istorija = new[] { Tuple.Create(1088, (int?)72, 5), Tuple.Create(405, (int?)72, 1), Tuple.Create(171, (int?)35, 0) };
            return new Sifarnik(st, pr, istorija);
        }

        [TestMethod]
        public void Stanica_JednaGreskaUKucanju()
        {
            var u = new List<string>();
            Assert.AreEqual(171, Sif().StanicaID("Kosjeric", 7, u));
            Assert.AreEqual(1, u.Count);
        }

        [TestMethod]
        public void Stanica_BorTerenaNijeBor()
        {
            Assert.AreEqual(55, Sif().StanicaID("Bor Terena", null, new List<string>()));
        }

        [TestMethod]
        public void Stanica_DrzavnaGranicaPoPruzi()
        {
            Assert.AreEqual(273, Sif().StanicaID("#GRANICA", 2, new List<string>()));
            Assert.IsNull(Sif().StanicaID("#GRANICA", null, new List<string>()));
        }

        [TestMethod]
        public void Stanica_RasputnicaSkracenica()
        {
            Assert.AreEqual(600, Sif().StanicaID("Rasp.Cuprija", null, new List<string>()));
            Assert.AreEqual(601, Sif().StanicaID("Cuprija", null, new List<string>()));
        }

        [TestMethod]
        public void Stanica_DuplikatUSifarniku_NajkoriscenijaNaPruzi()
        {
            Assert.AreEqual(1088, Sif().StanicaID("Pancevo Vojlovica", 72, new List<string>()));
        }

        [TestMethod]
        public void Stanica_Nepoznata_Null()
        {
            Assert.IsNull(Sif().StanicaID("Nepostojeca Stanica", null, new List<string>()));
        }

        [TestMethod]
        public void Pruga_PoSlicnostiNiza()
        {
            Assert.AreEqual(2, Sif().PrugaID("Beograd Centar - Rasputnica \"G\"- Rakovica - Mladenovac - Lapovo - Nis - Presevo - drzavna granica - (Tabanovce)", null, new List<string>()));
            Assert.AreEqual(7, Sif().PrugaID("(Beograd Centar) - Resnik - Pozega - Vrbnica - drzavna granica - (Bijelo Polje)", null, new List<string>()));
        }

        [TestMethod]
        public void Pruga_DuplikatOpisa_Jednoznacno()
        {
            Assert.IsNotNull(Sif().PrugaID("Banatsko Milosevo - Senta - Subotica", null, new List<string>()));
        }
    }
}
