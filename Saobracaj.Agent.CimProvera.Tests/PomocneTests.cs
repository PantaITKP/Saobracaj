using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Saobracaj.Agent.CimProvera;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera.Tests
{
    [TestClass]
    public class PomocneTests
    {
        private static readonly PraviloKp Kp = new PraviloKp("3212", "Kombinovani prevoz d.o.o.",
            new[] { "KP", "K prevoz", "K-prevoz", "Kombinovani prevoz", "KP UIC 3212", "KPUIC3212", "UIC 3212", "3212 KP" });

        [DataTestMethod]
        [DataRow("KP - Kombinovani prevoz d.o.o., UIC 3212")]
        [DataRow("k-prevoz")]
        [DataRow("K PREVOZ")]
        [DataRow("KPUIC3212")]
        [DataRow("3212")]
        [DataRow("Kombinovani Prevoz D.O.O.")]
        public void Kp_se_prepoznaje_po_aliasima(string t) => Assert.IsTrue(Kp.JeKp(t));

        [DataTestMethod]
        [DataRow("13212")]
        [DataRow("SKP Beograd")]
        [DataRow("VTG Railtrain")]
        [DataRow("")]
        public void Nije_kp(string t) => Assert.IsFalse(Kp.JeKp(t));

        [TestMethod]
        public void Naslov_maila_je_naslov_primljenog_maila_i_naziv_fajla()
        {
            Assert.AreEqual("Holcim izvoz – HOLCIM (4).PDF", Izvestaj.NaslovMaila(" Holcim\r\n izvoz ", new[] { "HOLCIM (4).PDF", "holcim (4).pdf" }, false));
            Assert.AreEqual("Fw: CIM – a.pdf, b.pdf", Izvestaj.NaslovMaila("Fw: CIM", new[] { "a.pdf", "b.pdf" }, false));
            Assert.AreEqual("(bez naslova) – a.pdf", Izvestaj.NaslovMaila("", new[] { "a.pdf" }, false));
        }

        [TestMethod]
        public void Kp_naziv_bez_sifre()
        {
            Assert.IsTrue(Kp.JeKpNaziv("Kombinovani prevoz d.o.o."));
            Assert.IsFalse(Kp.JeKpNaziv("3212"));
            Assert.IsFalse(Kp.JeKpNaziv("VTG Railtrain 3212"));
        }

        [DataTestMethod]
        [DataRow("31 78 6656 001-5", "317866560015")]
        [DataRow("317866560015", "317866560015")]
        [DataRow("3178665600", null)]
        [DataRow("MSCU1234567", null)]
        public void Broj_kola_iz_celije(string celija, string ocekivano) => Assert.AreEqual(ocekivano, ExcelCitac.BrojKola(celija));

        [TestMethod]
        public void Kes_json_se_cita_nazad()
        {
            var r = Json.Iz<RezultatPriloga>("{\"dokumenti\":[{\"tip\":\"cim\",\"strane\":[1],\"cim\":[{\"r16\":{\"status\":\"prazno\"}}]}]}");
            Assert.AreEqual(TipDokumenta.Cim, r.Dokumenti[0].Tip);
            Assert.AreEqual(StatusRubrike.Prazno, r.Dokumenti[0].Cim[0].R16.Status);
        }

        // ---------------------------------------------------------------- lokalni čitač: šablon i parser rubrika

        private static Rec R(string t, double x, double y, double p = 0.98) => new Rec { T = t, X0 = x, X1 = x + 0.02 * Math.Max(1, t.Length / 4.0), Y0 = y, Y1 = y + 0.008, Pouzdanost = p, Velicina = 8 };

        private static CimSablon Sablon() =>
            CimSablon.Ucitaj(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sablon", "cim_sablon.json"),
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sablon", "obrasci"), new PragoviPoravnanja());

        /// <summary>Sidra obrasca ZS-EUROLOG ravnomerno po strani (tekst naziva rubrika na položaju iz šablona), pomerena za dx/dy.</summary>
        private static List<Rec> SidraEurolog(double dx = 0, double dy = 0)
        {
            var j = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sablon", "obrasci", "zs-eurolog.json")));
            var sva = j["sidra"].Select(a => ((string)a[0], (double)a[1], (double)a[2])).ToList();
            return Enumerable.Range(0, 30).Select(i => sva[i * sva.Count / 30])
                .Select(a => new Rec { T = a.Item1, X0 = a.Item2 - 0.01 + dx, X1 = a.Item2 + 0.01 + dx, Y0 = a.Item3 - 0.002 + dy, Y1 = a.Item3 + 0.002 + dy, Pouzdanost = 0.98, Velicina = 5, Font = "Arial-Narrow" }).ToList();
        }

        [TestMethod]
        public void Rec_ide_u_rubriku_po_polozaju_i_nazivi_rubrika_se_izbacuju()
        {
            var s = Sablon();
            // pomeren ispis (2 % udesno, 1 % naniže): poravnanje po sidrima vraća reči u prave rubrike
            var reci = SidraEurolog(0.02, 0.01);
            reci.AddRange(new[]
            {
                R("Vranjica", 0.15, 0.18), R("d.o.o.", 0.23, 0.18),                   // r4
                R("54", 0.83, 0.865), R("547893", 0.89, 0.865),                         // r62
                R("2154", 0.80, 0.898), R("500819", 0.89, 0.898),
            });
            var rez = s.Rasporedi(reci);
            Assert.AreEqual("ZS-EUROLOG", rez.Obrazac?.Id, rez.Poravnanje?.Opis);
            Assert.IsTrue(rez.Poravnato, rez.Poravnanje?.Opis);
            Assert.AreEqual(1.0, rez.Poravnanje.X[0], 0.01); Assert.AreEqual(-0.02, rez.Poravnanje.X[2], 0.005); Assert.AreEqual(-0.01, rez.Poravnanje.Y[2], 0.005);
            CollectionAssert.AreEqual(new[] { "Vranjica", "d.o.o." }, rez.Rubrika("4").Select(r => r.T).ToList());
            var r62 = CimParser.Identifikacija(rez.Rubrika("62"));
            Assert.AreEqual("54", r62.Zemlja); Assert.AreEqual("547893", r62.Stanica); Assert.AreEqual("2154", r62.Preduzece); Assert.AreEqual("500819", r62.Broj);
        }

        [TestMethod]
        public void R62_cifre_po_kucicama()
        {
            var l1 = new Linija(); l1.Reci.Add(new Rec { T = "7 2   1 6 3 5 0 1" });
            var l2 = new Linija(); l2.Reci.Add(new Rec { T = "3 1 2 1   1 5 3 8 0 0" });
            var r = new Identifikacija();
            CimParser.PopuniR62(r, new List<Linija> { l1, l2 });
            Assert.AreEqual("72", r.Zemlja); Assert.AreEqual("163501", r.Stanica); Assert.AreEqual("3121", r.Preduzece); Assert.AreEqual("153800", r.Broj);
        }

        [DataTestMethod]
        [DataRow("Ledvice 230 Světec 547893 28.10.2026", "2026-10-28")]
        [DataRow("Niš, 5.11.26", "2026-11-05")]
        [DataRow("2026-11-16", "2026-11-16")]
        [DataRow("Šabac", "")]
        [DataRow("32.13.2026", "")]
        public void Datum_iz_teksta(string t, string ocekivano) => Assert.AreEqual(ocekivano, CimParser.NadjiDatum(t, out _, out _));

        [TestMethod]
        public void Sablon_prepoznaje_nazive_rubrika_kao_sidra()
        {
            var s = Sablon();
            var reci = SidraEurolog();
            Assert.IsTrue(s.BrojSidara(reci) >= 20);
            var rez = s.Rasporedi(reci.Concat(new[] { R("Firma", 0.15, 0.10) }));
            Assert.IsTrue(rez.Rubrika("1").Any(r => r.T == "Firma"));
        }

        [TestMethod]
        public void Neprepoznat_obrazac_ne_dodeljuje_rubrike()
        {
            // samo 6 sidara (ispod praga 12): obrazac se ne prepoznaje i rubrike se ne dodeljuju nasumično
            var rez = Sablon().Rasporedi(SidraEurolog().Take(6).Concat(new[] { R("Firma", 0.15, 0.10) }));
            Assert.IsFalse(rez.Poravnato);
            Assert.AreEqual(0, rez.Rubrike.Count);
            StringAssert.Contains(rez.Poravnanje?.Razlog ?? "nema dovoljno sidara", "sidar");
        }

        [DataTestMethod]
        [DataRow("Sehr geehrte Damen und Herren, anbei der Frachtbrief. Mit freundlichen Grüßen", true)]
        [DataRow("Dear Sir, please find attached the CIM. Best regards", true)]
        [DataRow("Poštovani, u prilogu šaljem tovarni list. Pozdrav", false)]
        [DataRow("", false)]
        public void Jezik_odgovora(string tekst, bool en) => Assert.AreEqual(en, Obrada.JeEngleskiIliNemacki(tekst));

        [DataTestMethod]
        [DataRow("830 Izvozna deklaracija", TipDokumenta.IzvoznaDeklaracija, true)]
        [DataRow("EX-A", TipDokumenta.IzvoznaDeklaracija, true)]
        [DataRow("T1 MRN 26RS…", TipDokumenta.TranzitnaDeklaracija, true)]
        [DataRow("Spisak kola CIT 23", TipDokumenta.SpisakKola, true)]
        [DataRow("Wagenliste", TipDokumenta.SpisakKola, true)]
        [DataRow("Faktura", TipDokumenta.IzvoznaDeklaracija, false)]
        [DataRow("Text", TipDokumenta.TranzitnaDeklaracija, false)]
        public void Prepoznavanje_dokumenta_u_r9(string tekst, string tip, bool ocekivano) => Assert.AreEqual(ocekivano, Provere.Pominje(tekst, tip));
    }
}
