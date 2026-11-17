using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Radionica;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>v3.0 (2.10): događaji iz teksta maila – novi deo poruke, tipovi, vreme, zona, jezik.</summary>
    [TestClass]
    public class V300DogadjajiTests
    {
        private static readonly DateTimeOffset Mail = new DateTimeOffset(2026, 11, 17, 2, 31, 0, TimeSpan.Zero);

        [TestMethod]
        public void Zmajevo_ETA_Subotica_18_morning_i_oznaka_voza()
        {
            var d = IzvlacenjeDogadjaja.Izvuci("Zmajevo soy train  (SBGT26_SRB04)",
                "Dear all!\n\nCan you take over this train?\n\nETA Subotica 18/morning\n\nZmajevo - soy train (SBGT26_SRB04)\n\nThank you!\n\nBalogh Lajos\nÜzemeltetés", Mail, "railcargo.com");
            var eta = d.Single(x => x.Tip == "ETA");
            Assert.AreEqual("Subotica", eta.MestoTekst);
            Assert.AreEqual(new DateTime(2026, 11, 18, 8, 0, 0), eta.Vreme.Value.DateTime, "18. ujutru (mesec maila)");
            Assert.AreEqual("CET", eta.VremenskaZona);
            Assert.AreEqual(TimeSpan.FromHours(1), eta.Vreme.Value.Offset);
            Assert.AreEqual("en", eta.Jezik);
            Assert.IsTrue(d.Any(x => x.Tip == "BROJ_VOZA" && x.Vrednost == "SBGT26_SRB04"), string.Join(" | ", d));
        }

        [TestMethod]
        public void Citat_i_potpis_se_ne_citaju()
        {
            var d = IzvlacenjeDogadjaja.Izvuci("RE: Arrival", "Ok, hvala.\n\nS poštovanjem\nPetar\n\nFrom: x@y.ro\nSent: Monday\nETA Constanta 20.11. 14:00", Mail, "kprevoz.co.rs");
            Assert.IsFalse(d.Any(x => x.Tip == "ETA"), "ETA je u citiranom delu");
        }

        [TestMethod]
        public void Rumunski_posiljalac_EET_i_kasnjenje()
        {
            var d = IzvlacenjeDogadjaja.Izvuci("Tren 17", "Trenul are o întârziere de 3 ore, sosire estimată la Stamora 18.11.2026 10:30.", Mail, "pspcargo.ro");
            Assert.IsTrue(d.Any(x => x.Tip == "KASNJENJE" && x.Vrednost.StartsWith("3")), string.Join(" | ", d));
            var eta = d.Single(x => x.Tip == "ETA");
            Assert.AreEqual("ro", eta.Jezik);
            Assert.AreEqual("EET", eta.VremenskaZona);
            Assert.AreEqual(new DateTimeOffset(2026, 11, 18, 10, 30, 0, TimeSpan.FromHours(2)), eta.Vreme);
            Assert.AreEqual("Stamora", eta.MestoTekst);
        }

        [TestMethod]
        public void Srpski_broj_kola_ID_najave_MRN_i_otkacena_kola()
        {
            var d = IzvlacenjeDogadjaja.Izvuci("Dokumentacija za predaju ID 1655 Zmajevo",
                "Voz sa 27 vagona stigao u Suboticu u 06:40. Kola 3155 0824 291-6 otkačena zbog kvara. MRN 26RS045047C8UPQEJ0.", Mail, "kprevoz.co.rs");
            Assert.IsTrue(d.Any(x => x.Tip == "ID_NAJAVE" && x.Vrednost == "1655"), string.Join(" | ", d));
            Assert.IsTrue(d.Any(x => x.Tip == "BROJ_KOLA" && x.Vrednost == "27"), "bk " + string.Join(" | ", d));
            Assert.IsTrue(d.Any(x => x.Tip == "DOLAZAK" && x.VremeTekst.Contains("06:40")), "dol " + string.Join(" | ", d));
            Assert.IsTrue(d.Any(x => x.Tip == "OTKACENA_KOLA"), "otk " + string.Join(" | ", d));
            Assert.IsTrue(d.Any(x => x.Tip == "MRN" && x.Vrednost == "26RS045047C8UPQEJ0"), "mrn " + string.Join(" | ", d));
            Assert.IsTrue(d.All(x => x.Jezik == "sr"), "jez " + string.Join(" | ", d));
        }

        [TestMethod]
        public void Nemacki_madjarski_bugarski_turski_relativno_vreme()
        {
            var de = IzvlacenjeDogadjaja.Izvuci("Zug 49689", "Voraussichtliche Ankunft morgen 14:00 Uhr in Kelebia.", Mail, "railcargo.at");
            Assert.AreEqual("de", de.Single(x => x.Tip == "ETA").Jezik);
            Assert.AreEqual(new DateTime(2026, 11, 18, 14, 0, 0), de.Single(x => x.Tip == "ETA").Vreme.Value.DateTime);
            var hu = IzvlacenjeDogadjaja.Izvuci("Vonat", "A vonat várható érkezése holnap reggel Kelebiára.", Mail, "railcargo.com");
            Assert.AreEqual("hu", hu.Single(x => x.Tip == "ETA").Jezik);
            var bg = IzvlacenjeDogadjaja.Izvuci("Влак", "Влакът ще пристигне утре в 10:00 в Калотина.", Mail, "bdz.bg");
            Assert.AreEqual("EET", bg.Single(x => x.Tip == "ETA").VremenskaZona);
            var tr = IzvlacenjeDogadjaja.Izvuci("Tren", "Tren yarın 09:00 Kapıkule tahmini varış.", Mail, "tcdd.gov.tr");
            Assert.AreEqual("TRT", tr.Single(x => x.Tip == "ETA").VremenskaZona);
            Assert.AreEqual(TimeSpan.FromHours(3), tr.Single(x => x.Tip == "ETA").Vreme.Value.Offset);
        }

        [TestMethod]
        public void Recnik_izraza_po_jeziku_sa_brojem()
        {
            var d = IzvlacenjeDogadjaja.Izvuci("a", "ETA Subotica 18/morning", Mail).Concat(IzvlacenjeDogadjaja.Izvuci("b", "ETA Kelebia 19.11. 10:00", Mail)).ToList();
            var r = IzvlacenjeDogadjaja.RecnikIz(d);
            Assert.AreEqual(2, r.Single(x => x.tip == "ETA" && x.jezik == "en").broj);
        }
    }
}
