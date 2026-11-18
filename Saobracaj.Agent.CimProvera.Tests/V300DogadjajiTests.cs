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

        // ------------------------------------------------------------------------------------------- (c) pregled probnog _Analiza, 18.11.

        [TestMethod]
        public void Vreme_madjarski_datum_i_engleski_naziv_meseca()
        {
            var mail = new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.FromHours(1));
            var (v1, t1) = IzvlacenjeDogadjaja.Vreme("A vonat érkezése Kelebiára 2026.11.15.-én / 11:57", mail, "CET");
            Assert.AreEqual(new DateTime(2026, 11, 15, 11, 57, 0), v1.Value.DateTime, t1);
            var (v2, t2) = IzvlacenjeDogadjaja.Vreme("The train arrived at Kelebia on 15 November 2026 at 11:57.", mail, "CET");
            Assert.AreEqual(new DateTime(2026, 11, 15, 11, 57, 0), v2.Value.DateTime, t2);
            var (v3, _) = IzvlacenjeDogadjaja.Vreme("Expected arrival at Subotica: November 15 in the morning", mail, "CET");
            Assert.AreEqual(new DateTime(2026, 11, 15, 8, 0, 0), v3.Value.DateTime);
            var (v4, _) = IzvlacenjeDogadjaja.Vreme("Mail 15 attached", mail, "CET");
            Assert.IsNull(v4, "„Mail 15“ nije maj");
        }

        [TestMethod]
        public void ID_najave_samo_iz_oblika_ID_nnnn_aviso_nije_ID()
        {
            var d = IzvlacenjeDogadjaja.Izvuci("Aviso: 46836/15 / lkw walter SAD-HER 79", "Aviso 46836/15 attached. ETA Subotica 16.11. 10:00", Mail, "crw.sk");
            Assert.IsFalse(d.Any(x => x.Tip == "ID_NAJAVE"), string.Join(" | ", d));
            var e = IzvlacenjeDogadjaja.Izvuci("Dokumentacija za predaju ID 1615 Kukujevci - Šid gr", "U prilogu dokumentacija.", Mail, "kprevoz.co.rs");
            Assert.AreEqual("1615", e.Single(x => x.Tip == "ID_NAJAVE").Vrednost);
        }

        // ------------------------------------------------------------------------------------------- 8 ispravki iz PROCITAJ.md (18.11.)

        [TestMethod]
        public void Datum_posle_kose_crte_00_15_nije_datum()
        {
            var mail = new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.FromHours(2));
            var (v, t) = IzvlacenjeDogadjaja.Vreme("ETA Carpinis: 12:00/15.11.2026", mail, "EET");
            Assert.AreEqual(new DateTime(2026, 11, 15, 12, 0, 0), v.Value.DateTime, t);
            var (v2, t2) = IzvlacenjeDogadjaja.Vreme("ETD Stamora:12:00/14.11.2026", mail, "EET");
            Assert.AreEqual(new DateTime(2026, 11, 14, 12, 0, 0), v2.Value.DateTime, t2);
        }

        [TestMethod]
        public void Dan_kosa_crta_vreme_17_12_30_je_dan_nikad_mesec()
        {
            var mail = new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.FromHours(1));
            var (v, t) = IzvlacenjeDogadjaja.Vreme("The train will arrive at 17/12:30 in Subotica.", mail, "CET");
            Assert.AreEqual(new DateTime(2026, 11, 17, 12, 30, 0), v.Value.DateTime, t);
        }

        [TestMethod]
        public void Srpski_bez_dijakritika_nije_turski_posiljalac_rs_i_KP()
        {
            var d = IzvlacenjeDogadjaja.Izvuci("RE: Pozicije kola", "Zavrsio kao voz 45660 13.03. u 01h (29vg)\n29 vagona zavrsilo voznju 23.10. u 01:30h", Mail, "pansped.rs");
            Assert.IsTrue(d.Count > 0 && d.All(x => x.Jezik == "sr"), string.Join(" | ", d));
            Assert.AreEqual("sr", IzvlacenjeDogadjaja.Jezik("Ostalo za utovar 1 vagon", "kprevoz.co.rs"));
            Assert.AreEqual("tr", IzvlacenjeDogadjaja.Jezik("Vagonlar yarın saat 10:00'da varacak, saygılarımla", "firma.com.tr"));
        }

        [TestMethod]
        public void Cid_i_nazivi_slika_se_izbacuju_pre_analize()
        {
            var d = IzvlacenjeDogadjaja.Izvuci("AW: Loading of Herne - Novi Sad (HERSAD81) 14-11",
                "[cid:ac2c37c3-fde9-4599-9a1b-1234567890ab]\n[cid:image001.png@01DC5A2B.12345670]\nimage002.jpg\nAbfahrt, für 02:00 Uhr am 16.11 wäre nicht das Problem.", Mail, "whe.de");
            Assert.IsFalse(d.Any(x => x.Tip == "BROJ_VOZA" && (x.Vrednost.Contains("fde9") || x.Vrednost.Contains("image"))), string.Join(" | ", d));
            Assert.IsTrue(d.Any(x => x.Tip == "POLAZAK"), string.Join(" | ", d));
        }

        [TestMethod]
        public void Mesto_posle_dvotacke_Arrival_station_of_train()
        {
            Assert.AreEqual("Prahovo", IzvlacenjeDogadjaja.Mesto("DO / Arrival station of train: Prahovo"));
            Assert.AreEqual("Svetec", IzvlacenjeDogadjaja.Mesto("IZ / Departure station of train: Svetec"));
        }

        [TestMethod]
        public void Broj_kola_raspon_4_5_vagona_tekst_niska_pouzdanost()
        {
            var d = IzvlacenjeDogadjaja.Izvuci("Utovar", "Natovarena 4,5 vagona treće izmene.", Mail, "kprevoz.co.rs");
            var k = d.Single(x => x.Tip == "BROJ_KOLA");
            Assert.AreEqual("4,5", k.Vrednost);
            Assert.IsTrue(k.Raspon);
            Assert.IsTrue(k.Pouzdanost <= 50);
            var e = IzvlacenjeDogadjaja.Izvuci("Utovar", "Danas će biti završen istovar svih 32 vagona.", Mail, "kprevoz.co.rs").Single(x => x.Tip == "BROJ_KOLA");
            Assert.AreEqual("32", e.Vrednost); Assert.IsFalse(e.Raspon);
        }

        [TestMethod]
        public void Broj_iz_oznake_voza_nikad_nije_kasnjenje()
        {
            Assert.AreEqual("24", IzvlacenjeDogadjaja.Kasnjenje("HER-SAD 81 has a 24 delay."));
            Assert.AreEqual("", IzvlacenjeDogadjaja.Kasnjenje("HER-SAD 81 has a delay."));
            Assert.AreEqual("3 ore", IzvlacenjeDogadjaja.Kasnjenje("Trenul are o întârziere de 3 ore"));
            var d = IzvlacenjeDogadjaja.Izvuci("HER-SAD 81", "HER-SAD 81 has a 24 delay.", Mail, "ct-herne.com");
            Assert.IsFalse(d.Any(x => x.Tip == "KASNJENJE" && x.Vrednost.StartsWith("81")), string.Join(" | ", d));
        }

        [TestMethod]
        public void Disclaimer_iz_potpisa_nije_OSTALO_VAZNO()
        {
            var d = IzvlacenjeDogadjaja.Izvuci("RE: [External mail] Re: [External mail]",
                "Köszönöm.\nAmennyiben ezt a levelet sérülten kapta meg vagy nem Ön annak valós címzettje, kérjük értesítse a feladót.\n"
                + "This e-mail is confidential and intended solely for the addressee. If you have received this e-mail in error, please delete it; damaged attachments are not our responsibility.", Mail, "markikft.hu");
            Assert.IsFalse(d.Any(x => x.Tip == "OSTALO_VAZNO"), string.Join(" | ", d));
            Assert.IsTrue(IzvlacenjeDogadjaja.Izvuci("Otkaz", "Voz PRAHOVO 005 je otkazan.", Mail, "kprevoz.co.rs").Any(x => x.Tip == "OSTALO_VAZNO"), "pravi otkaz ostaje");
        }
    }
}
