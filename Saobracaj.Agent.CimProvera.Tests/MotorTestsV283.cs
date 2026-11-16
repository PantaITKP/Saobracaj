using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>v2.8.3: dopune bez izmišljenih naziva firmi, zemlja r1/r4 iz adrese, r58a iz r62, NHM šablon.</summary>
    public partial class MotorTests
    {
        private static readonly Regex PravniOblik = new Regex(
            @"(?i)(?<![\p{L}])(d\.?\s?o\.?\s?o\.?|a\.\s?d\.|gmbh|kft\.?|zrt\.?|s\.?r\.?l\.?|s\.p\.a\.?|spa|ltd\.?|s\.?r\.?o\.?|a\.ş\.?|şti\.?|sti\.?|ood|ead|b\.?v\.?|n\.?v\.?)(?![\p{L}])");

        private static IEnumerable<string> DelociDopune(Nalaz n) =>
            new[] { n.Dopuna }.Concat(n.DopunaRedovi ?? new List<string>()).Concat(n.DopunaPoPoljima?.Values ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x));

        /// <summary>
        /// v2.8.3 (1): naziv, adresa, platilac i primalac u dopuni (PDF primer, kolona „Kako treba da izgleda“) nikad nisu izmišljeni:
        /// svaki naziv sa pravnim oblikom (d.o.o., GmbH, Kft…) mora biti pročitan iz priloga ili je naziv KP-a. Pada ako se u primeru
        /// pojavi npr. „Transport Partner d.o.o.“.
        /// </summary>
        [TestMethod]
        public void Dopune_nikad_ne_izmisljaju_naziv_firme()
        {
            var greske = new List<string>();
            foreach (var id in Kvarovi.Keys)
            {
                var s = new Scenario();
                Kvarovi[id](s);
                var procitano = (JsonConvert.SerializeObject(s.C) + " " + JsonConvert.SerializeObject(s.Ostali) + " " + pravila.Kp.Naziv).ToLowerInvariant();
                foreach (var n in Svi(Proveri(s)))
                    foreach (var deo in DelociDopune(n))
                        foreach (var seg in Regex.Split(deo, @"[,;:\n/]|\s[-–]\s"))
                        {
                            if (!PravniOblik.IsMatch(seg)) continue;
                            var naziv = Regex.Replace(seg, @"^\s*(\d{4}\s+)?", "").Trim();
                            if (!procitano.Contains(naziv.ToLowerInvariant()))
                                greske.Add(id + " → " + n.Pravilo + " r" + n.Rubrika + ": „" + naziv + "“ (dopuna „" + deo + "“)");
                        }
            }
            Assert.AreEqual(0, greske.Count, "izmišljen naziv firme u dopuni:\n" + string.Join("\n", greske));
        }

        [TestMethod]
        public void Dopuna_prazne_r1_je_sablon_a_ne_izmisljena_firma()
        {
            var s = new Scenario();
            s.C.R1 = new Model.Strana();
            var n = Svi(Proveri(s)).First(x => x.Rubrika == "1" && !string.IsNullOrEmpty(x.Dopuna));
            StringAssert.StartsWith(n.Dopuna, "XXXXX, ulica i broj, poštanski broj mesto, ");
            Assert.IsFalse(n.Dopuna.Contains("Transport Partner"));
            // zemlja prazne r1: iz stanice te strane (r16 = 72 → RS), nikad iz r62 ili druge strane
            StringAssert.Contains(n.Dopuna, "RS");
        }

        [TestMethod]
        public void Dopuna_platioca_KP7_je_sablon()
        {
            var s = new Scenario();
            s.C.R7 = F(new ListaRubrika { Stavke = { new SifraTekst { Sifra = "16", Tekst = "Frachtzahler HU: Kombinovani prevoz" } } }, "16 Frachtzahler HU: Kombinovani prevoz");
            var n = Prvi(Proveri(s), "KP7-PLATILAC");
            Assert.AreEqual("Platilac za deonicu SRB: XXXXX", n.Dopuna);
        }

        // ------------------------------------------------------------------------------------------- 2. zemlja r1/r4

        [DataTestMethod]
        [DataRow("Hafenstraße 1, 44653 Herne", "DE")]
        [DataRow("D-44653 Herne", "DE")]
        [DataRow("Petőfi utca 1, 6200 Kiskőrös", "HU")]
        [DataRow("Váci út 1, 1132 Budapest", "HU")]
        [DataRow("Çankaya, Ankara", "TR")]
        [DataRow("Nişantaşı Cad. No: 503 Selçuklu/Konya", "TR")]
        [DataRow("Terezínská 1, 410 02 Lovosice", "CZ")]
        [DataRow("Via Roma 1, 46025 Poggio Rusco (MN)", "IT")]
        [DataRow("Via Emilia 5, 41015 Nonantola", "IT")]
        [DataRow("Ulica 1, 12345 Nepoznatovo", null)]
        [DataRow("", null)]
        public void Zemlja_iz_adrese(string adresa, string iso) => Assert.AreEqual(iso, Dopune.ZemljaIzAdrese(adresa));

        [DataTestMethod]
        [DataRow("1", "HSL Logistik GmbH\nHafenstr. 1\n44653 Herne", "DE")]
        [DataRow("1", "RCH Kft.\nPetőfi u. 1\n6200 Kiskőrös", "HU")]
        [DataRow("4", "CINKOTURK LOGISTIK\nAtatürk Bulvarı 1\nÇankaya Ankara", "TR")]
        [DataRow("4", "Futura SRL\nVia Roma 1\n46025 Poggio Rusco", "IT")]
        public void Dopuna_zemlje_r1_r4_iz_adrese_a_ne_iz_druge_strane(string rub, string tekst, string iso)
        {
            var s = new Scenario();
            var linije = tekst.Split('\n');
            var st = F(new Model.Strana { Naziv = linije[0], Adresa = string.Join(", ", linije.Skip(1)), SifraZemlje = "", NazivZemlje = "", Telefon = "+381 1", Email = "a@b.c" }, tekst);
            if (rub == "1") s.C.R1 = st; else s.C.R4 = st;
            var n = Svi(Proveri(s)).FirstOrDefault(x => x.Rubrika == rub && DelociDopune(x).Any());
            Assert.IsNotNull(n, "nalaz za zemlju r" + rub + ": " + string.Join(" | ", Svi(Proveri(s)).Select(x => x.Pravilo + " r" + x.Rubrika)));
            StringAssert.Contains(n.Dopuna, iso);
            // HERSAD79/80: r1 Herne nikad „RS“ (r62/druga strana)
            Assert.IsFalse(Regex.IsMatch(n.Dopuna, @"\bRS\b") && iso != "RS", n.Dopuna);
        }

        [TestMethod]
        public void Dopuna_zemlje_nepoznata_adresa_i_stanica_je_XX()
        {
            var s = new Scenario();
            s.C.R4 = F(new Model.Strana { Naziv = "Primalac AD", Adresa = "Ulica 2, 1000 Nepoznatovo", SifraZemlje = "", NazivZemlje = "", Email = "a@b.c" });
            s.C.R10 = new MestoIsporuke(); s.C.R12 = new TekstRubrika();
            var n = Svi(Proveri(s)).FirstOrDefault(x => x.Rubrika == "4" && DelociDopune(x).Any(d => Regex.IsMatch(d, @"\b[A-Z]{2}\b")));
            if (n != null) Assert.IsTrue(Regex.IsMatch(n.Dopuna, @"\bXX\b"), "nepoznata zemlja je „XX“, ne RS/BG: " + n.Dopuna);
        }

        // ------------------------------------------------------------------------------------------- 5. r58a, NHM

        [TestMethod]
        public void R58a_predlog_je_preduzece_iz_r62_a_ne_3212()
        {
            var s = new Scenario { Rezim = "auto" };
            s.C.R62.Preduzece = "3127";
            s.C.R57.Prevoznici.Clear();
            s.C.R57.Prevoznici.Add(new Prevoznik { Sifra = "3127", Naziv = "HSL Logistik GmbH", Deonica = "Herne - Kelebia", StatusPrevoznika = "1" });
            s.C.R58a = new UgovorniPrevoznik();
            var r = Proveri(s);
            var n = Svi(r).Where(x => x.Rubrika == "58a" || x.Rubrika == "58").FirstOrDefault(x => DelociDopune(x).Any());
            Assert.IsNotNull(n, string.Join(" | ", Svi(r).Select(x => x.Pravilo + " r" + x.Rubrika)));
            StringAssert.StartsWith(n.Dopuna, "3127");
            Assert.IsFalse(n.Dopuna.Contains("3212"), n.Dopuna);
        }

        [TestMethod]
        public void NHM_sablon_je_XXXXXX_a_ne_izmisljena_sifra()
        {
            var json = System.IO.File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pravila", "cim_validation_rules.json"));
            Assert.IsFalse(json.Contains("262190"), "primer NHM je „XXXXXX“");
            Assert.IsFalse(json.Contains("004NISBAR26"), "primer r8 je „XXXXX“");
        }
    }
}
