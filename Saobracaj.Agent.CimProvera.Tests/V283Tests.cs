using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>v2.8.3: ispravke posle testa sa 6 mailova (13–14.11.2026).</summary>
    [TestClass]
    public class V283Tests
    {
        private static PravilaSkup pravila;

        [ClassInitialize]
        public static void Init(TestContext _)
        {
            var put = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pravila", "cim_validation_rules.json");
            pravila = PravilaSkup.IzTeksta(File.ReadAllText(put), put);
        }

        private static RezultatProvere Rezultat(CimList c, string fajl, int strana = 1, params Nalaz[] nalazi)
        {
            var d = new Dokument { Tip = TipDokumenta.Cim, Fajl = fajl, Strane = { strana }, Cim = { c } };
            var r = new RezultatProvere();
            r.Dokumenti.Add(d);
            var l = new RezultatLista { Dokument = d, Cim = c, Oznaka = "1" };
            l.Nalazi.AddRange(nalazi);
            r.Listovi.Add(l);
            return r;
        }

        private static CimList List() => MotorTests.IspravanCim();

        // ------------------------------------------------------------------------------------------- 3. interni redovi

        private static Nalaz Interni() => new Nalaz { Pravilo = "R49", Rubrika = "49", Tezina = Tezina.Preporuka, TezinaPravila = Tezina.Preporuka, SamoInterno = true, Tekst = "R49 popunjava KP", Uputstvo = "R49 popunjava KP" };
        private static Nalaz Greska() => new Nalaz { Pravilo = "R21", Rubrika = "21", Tezina = Tezina.Greska, TezinaPravila = Tezina.Greska, Tekst = "Nedostaje opis robe", Uputstvo = "Nedostaje opis robe" };

        [TestMethod]
        public void Live_izvestaj_posiljaocu_nema_internih_redova_ni_u_mailu_ni_u_html()
        {
            var r = Rezultat(List(), "cim.pdf", 1, Greska(), Interni());
            foreach (var interniPosiljalac in new[] { false, true })
            {
                var z = Izvestaj.ZaPosiljaoca(r, Rezim.Live, interniPosiljalac);
                Assert.IsFalse(z.ImaInternih);
                var telo = Izvestaj.Telo(z, pravila, false);
                var html = Izvestaj.Html(z, pravila, false);
                Assert.IsFalse(telo.Contains("INTERNO"), telo);
                Assert.IsFalse(html.Contains("INTERNO"), html);
                Assert.IsFalse(telo.Contains("Rubrika 49"), telo);
                StringAssert.Contains(telo, "Rubrika 21");
            }
        }

        [TestMethod]
        public void Test_rezim_interni_redovi_u_posebnom_odeljku_na_dnu()
        {
            var r = Rezultat(List(), "cim.pdf", 1, Greska(), Interni());
            var z = Izvestaj.ZaPosiljaoca(r, Rezim.Test, false);
            var telo = Izvestaj.Telo(z, pravila, false);
            var i = telo.IndexOf("INTERNO (ne ide pošiljaocu)", StringComparison.Ordinal);
            Assert.IsTrue(i > 0, telo);
            Assert.IsTrue(telo.IndexOf("Rubrika 49", StringComparison.Ordinal) > i, "r49 samo u internom odeljku");
            Assert.IsTrue(telo.IndexOf("S poštovanjem", StringComparison.Ordinal) < i, "interni odeljak je posle potpisa i futera");
            Assert.IsTrue(telo.Substring(0, i).Contains(new string((char)0x2500, 20)), "odvojen linijom");
            var html = Izvestaj.Html(z, pravila, false);
            StringAssert.Contains(html, "INTERNO (ne ide pošiljaocu)");
            // poređenje stavki (tekst i HTML) ne broji interni odeljak
            var izTeksta = Poredjenje.Stavke(telo);
            var izHtml = Poredjenje.Stavke(Poredjenje.HtmlUTekst(html));
            Assert.AreEqual(izTeksta.Count, izHtml.Count);
            Assert.IsFalse(izHtml.Any(x => x.Kljuc.Contains("49")));
        }

        [TestMethod]
        public void R8_referenca_nije_u_primeru()
        {
            var r8 = pravila.PoId["REC1"];
            var json = r8.Json.ToString();
            Assert.IsFalse(json.Contains("004NISBAR26"));
            StringAssert.Contains(json, "r8: broj voza ili referenca, npr. XXXXX");
        }

        // ------------------------------------------------------------------------------------------- 9. interni izveštaj

        [TestMethod]
        public void Interni_uvod_kratak_bez_pisma()
        {
            var c = new CimList { NeprepoznatObrazac = "tovarni list CIM: obrazac nije prepoznat – potreban je novi šablon, ručna provera (najbliži šablon CIT, 3 sidara)" };
            var r = Rezultat(c, "SET EXPORT TREN SARE.pdf", 4);
            r.Listovi[0].ZaRucnuProveru = true;
            var uvod = Izvestaj.UvodInternogKratak(Izvestaj.RazlogRucna, r, "stefan.obradovic@kprevoz.co.rs", "FW: FW test 6", new DateTime(2026, 11, 14, 0, 9, 0));
            Assert.AreEqual("INTERNO – ručna provera. Mail „FW: FW test 6“ (stefan.obradovic@kprevoz.co.rs, 14.11. 00:09). Tovarni list CIM (SET EXPORT TREN SARE.pdf, str. 4) nije proveren: obrazac nije prepoznat. Uradite ručnu proveru pre odgovora pošiljaocu.", uvod);
            var telo = Izvestaj.Telo(r, pravila, false, uvod, null, null, interni: true);
            foreach (var ne in new[] { "Poštovani", "S poštovanjem", "zahvaljujemo", "Unapred hvala", "nismo utvrdili" })
                Assert.IsFalse(telo.Contains(ne), "interni izveštaj ne sme da ima „" + ne + "“:\n" + telo);
            var html = Izvestaj.Html(r, pravila, false, uvod, null, null, interni: true);
            Assert.IsFalse(html.Contains("Poštovani") || html.Contains("S poštovanjem"), html);
        }

        // ------------------------------------------------------------------------------------------- 7. TCDD 3370A

        [DataTestMethod]
        [DataRow("FREIGHT PAYER FOR BG EXPRESS 5665 AND KP3212-CINKOTURKLOGISTIK")]
        [DataRow("FREIGHT PAYER BG EXPRESS 5665 KP3212-CINKOTURKLOGISTIK")]
        [DataRow("FREIGHT PAYER FOR BG EXPRESS 5665 AND KP3212 – CINKOTURK LOGISTIK")]
        [DataRow("EMTB TRADE S.R.O. SVATOPLUKOVA 181/12 FREIGHT PAYER FOR BG EXPRESS 5665 KP 3212 - CINKOTURK LOGISTIK")]
        public void TCDD_platilac_za_KP_deonicu_je_platilac_za_Srbiju(string r7)
        {
            var a = Provere.AnalizirajR7(r7, pravila.Kp);
            var p = a.ZaSrbiju.ToList();
            Assert.AreEqual(1, p.Count, string.Join(" | ", a.Platioci.Select(x => x.Zapis)));
            Assert.AreEqual("CINKOTURK LOGISTIK", p[0].Naziv);
            Assert.IsFalse(p[0].Kp, "platilac nije KP");
        }

        [TestMethod]
        public void TCDD_sablon_postoji_i_trazi_natpis_obrasca()
        {
            var put = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sablon", "obrasci", "tcdd-3370a.json");
            Assert.IsTrue(File.Exists(put), put);
            var s = ObrazacSablon.Ucitaj(put);
            Assert.AreEqual("TCDD-3370A", s.Id);
            CollectionAssert.Contains(s.Natpisi, "tasima belgesi");
            Assert.IsTrue(s.VisinaNatpisa > 0 && s.VisinaNatpisa < 0.0049, "podaci TCDD štampe (visina ~0,005) nisu natpisi");
            // bez natpisa „Taşıma Belgesi“ šablon se ne koristi (CIM drugih uprava ostaje na CIT/nacionalnom šablonu)
            var reci = new List<Rec> { new Rec { T = "Lettre", X0 = 0.1, X1 = 0.2, Y0 = 0.1, Y1 = 0.11 }, new Rec { T = "voiture", X0 = 0.21, X1 = 0.3, Y0 = 0.1, Y1 = 0.11 } };
            var p = Poravnanje.Izracunaj(s, reci, null, new PragoviPoravnanja());
            Assert.IsFalse(p.Pouzdano); Assert.IsFalse(p.PoNatpisu);
            StringAssert.Contains(p.Razlog, "nema natpisa obrasca");
        }

        // ------------------------------------------------------------------------------------------- regresija: pravi fajlovi (13–14.11.)

        private const string Nezavisni = @"\\192.168.1.6\Cloude\Agenti\CimProvera\testovi\nezavisni\2026-11-14";
        private static readonly Dictionary<string, Model.RezultatPriloga> procitano = new Dictionary<string, Model.RezultatPriloga>();

        private static Model.RezultatPriloga Procitaj(string relativno)
        {
            var folder = Environment.GetEnvironmentVariable("CIMPROVERA_TESTOVI") is string f && f.Length > 0 ? Path.Combine(f, "nezavisni", "2026-11-14") : Nezavisni;
            var put = Path.Combine(folder, relativno);
            if (!File.Exists(put)) Assert.Inconclusive("Nema " + put);
            lock (procitano)
            {
                if (procitano.TryGetValue(put, out var r)) return r;
                var cfg = RegresijaTests.Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje"));
                using (var c = new LokalniCitac(cfg, null, pravila.Kp))
                    r = c.Procitaj(new Prilozi.Prilog { Naziv = Path.GetFileName(put), Sadrzaj = File.ReadAllBytes(put), Vrsta = Prilozi.VrstaPriloga.Pdf, MediaType = "application/pdf" });
                return procitano[put] = r;
            }
        }

        /// <summary>v2.8.3 (8.2): CIT 2006 (Lettre de voiture CIM / Frachtbrief CIM) sa skena umanjenog ~8 % – poravnanje po okviru i krugovima.</summary>
        [TestMethod, TestCategory("Regresija")]
        public void SET_EXPORT_str4_CIT_2006_sa_skena_je_prepoznat()
        {
            var r = Procitaj(@"fw test 6\SET EXPORT TREN SARE.pdf");
            var d = r.Dokumenti.Single(x => TipDokumenta.JeTovarniList(x.Tip));
            CollectionAssert.Contains(d.Strane, 4);
            var c = d.Cim.Single();
            Assert.AreEqual("", c.NeprepoznatObrazac ?? "", "obrazac mora biti prepoznat: " + d.Opis);
            Assert.AreEqual("CIT", c.Sablon, d.Opis);
            StringAssert.Contains(c.R1.Tekst ?? "", "SALZBERGWERK");
        }

        /// <summary>v2.8.3 (7): TCDD 3370A – str. 3 „12 е въглища.pdf“: r1, r4, r16 (kućice 11 10 16), r62.</summary>
        [TestMethod, TestCategory("Regresija")]
        public void TCDD_3370A_str3_rubrike()
        {
            var r = Procitaj(@"FW test 3\12 е въглища.pdf");
            var listovi = r.Dokumenti.Where(x => TipDokumenta.JeTovarniList(x.Tip)).ToList();
            Assert.AreEqual(12, listovi.Count);
            var c = listovi.Single(x => x.Strane.Contains(3)).Cim.Single();
            Assert.AreEqual("TCDD-3370A", c.Sablon);
            StringAssert.Contains(c.R1.Tekst ?? "", "ANKARA");
            StringAssert.Contains(c.R4.Tekst ?? "", "MONICOM");
            StringAssert.Contains(c.R4.Tekst ?? "", "BULEVAR");
            // ostali listovi istog maila: isti šablon
            foreach (var l in listovi) Assert.AreEqual("TCDD-3370A", l.Cim.Single().Sablon, l.Opis);
        }

        // ------------------------------------------------------------------------------------------- 10. sitnice

        [TestMethod]
        public void Naslov_bez_ponovljenog_CIM_kada_ime_fajla_pocinje_sa_CIM()
        {
            var c = new CimList { Obrazac = "CIM" };
            var r = Rezultat(c, "CIM draft GK30 Futog -Lovosice.pdf", 1, Greska());
            var n = Izvestaj.Naslov(r, pravila, false);
            StringAssert.StartsWith(n, "Tovarni list CIM draft GK30");
            Assert.IsFalse(n.Contains("CIM CIM"), n);
        }

        [TestMethod]
        public void Primer_dopune_ne_ide_posle_ostatka_OCR_a()
        {
            var n = new Nalaz { Pravilo = "R4-ISO", Rubrika = "4", Tezina = Tezina.Greska, Dopuna = "CZ", Vrednost = "Lovochemie a.s. / Terezínská 57 / 410 02 Lovosice / o" };
            Assert.AreEqual("+ CZ (posle „410 02 Lovosice“)", Izvestaj.PrimerIzDopune(n, false));
        }

        [TestMethod]
        public void Interni_uvod_grupise_listove_po_fajlu()
        {
            var r = new RezultatProvere();
            foreach (var (f, s) in new[] { ("12 e.pdf", 1), ("12 e.pdf", 2), ("12 e.pdf", 3), ("5 r.pdf", 1), ("5 r.pdf", 2) })
            {
                var d = new Dokument { Tip = TipDokumenta.Cim, Fajl = f, Strane = { s }, Cim = { new CimList() } };
                r.Listovi.Add(new RezultatLista { Dokument = d, Cim = d.Cim[0], ZaRucnuProveru = true });
            }
            var u = Izvestaj.UvodInternogKratak(Izvestaj.RazlogRucna, r, "a@b.c", "FW: FW test 3", new DateTime(2026, 11, 13, 23, 53, 0));
            StringAssert.Contains(u, "5 tovarnih listova (12 e.pdf, str. 1–3; 5 r.pdf, str. 1, 2) nije pouzdano pročitano (sken)");
        }

        [TestMethod]
        public void Excel_sa_oznakama_UTI_bez_kola_je_spisak_tovarnih_jedinica()
        {
            var redovi = new List<string[]> { new[] { "Pos.", "T2", "SV" } };
            for (int i = 1; i <= 6; i++) redovi.Add(new[] { i + ";LKWA 20" + (38970 + i) });
            var d = Prilozi.ExcelCitac.AnalizaRedova(redovi, pravila.Kp, "Herne - Novi Sad (HERSAD80) T2 + SV.xlsx", "excel", "Excel").Single();
            Assert.AreEqual(TipDokumenta.SpisakJedinica, d.Tip, d.Opis);
            StringAssert.StartsWith(d.Opis, "spisak tovarnih jedinica");
        }

        // ------------------------------------------------------------------------------------------- 6. najava voza

        private const string NajavaTekst =
            "Naš broj i znak: 48080   Mob: +359 000 00 00 00\n" +
            "Najava voza   Train announcement   Zuganmeldung\n" +
            "Otkaz voza   Train cancellation   Zugkundigung\n" +
            "BROJ VOZA   TRAIN NUMBER   ZUGNUMMER   48080\n" +
            "POŠILJALAC   SENDER   Absender   PRIMER MADENCILIK\n" +
            "OTPRAVNA STANICA   FORWARDING STATION   VERSANDBAHNHOF   Lüleburgaz - 3510445\n" +
            "UPUTNA STANICA   STATION OF DESTINATION   BESTIMMUNGSBAHNHOF   Crveni Krst / SERBIA -72-125500\n" +
            "TEŽINA VOZA   TRAIN BRUTOWEIGHT   ZUG BRUTO GEWICHT   156t\n" +
            "DUŽINA VOZA   TRAINLENGTH   ZUGLÄNGE   105 m\n" +
            "BROJ KOLA   NUMBER OF WAGON   ANZAHL DER WAGEN   5\n" +
            "*ETA\n15.11.2026 02:40\n";

        [TestMethod]
        public void Najava_voza_polja_za_log()
        {
            var p = NajavaVoza.Procitaj(NajavaTekst);
            Assert.AreEqual("48080", p["voz"]);
            Assert.AreEqual("Lüleburgaz - 3510445", p["od"]);
            Assert.AreEqual("Crveni Krst / SERBIA -72-125500", p["do"]);
            Assert.AreEqual("5", p["kola"]);
            Assert.AreEqual("156t", p["tezina"]);
            Assert.AreEqual("105 m", p["duzina"]);
            Assert.AreEqual("15.11.2026 02:40", p["eta"]);
            var o = NajavaVoza.Opis(NajavaTekst);
            StringAssert.StartsWith(o, "najava voza 48080: Lüleburgaz - 3510445 → Crveni Krst");
            StringAssert.EndsWith(o, "(ne proverava se)");
        }

        [TestMethod]
        public void Najava_voza_je_poseban_tip_a_ne_tovarni_list()
        {
            var s = new Citanje.Strana { Broj = 1 };
            double y = 0.05;
            foreach (var red in NajavaTekst.Split('\n'))
            {
                double x = 0.05;
                foreach (var w in red.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) { s.Reci.Add(new Rec { T = w, X0 = x, X1 = x + 0.04, Y0 = y, Y1 = y + 0.01, Pouzdanost = 0.95 }); x += 0.05; }
                y += 0.03;
            }
            var cfg = RegresijaTests.Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje"));
            using (var c = new LokalniCitac(cfg, null, pravila.Kp))
                Assert.AreEqual(TipDokumenta.Najava, c.TipStrane(s, "Наява - 12.11.2026 – 48080-5. вагона.pdf"));
        }
    }
}
