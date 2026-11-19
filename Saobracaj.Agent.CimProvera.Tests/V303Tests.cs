using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Baza;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Mail;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Radionica;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>v3.0.3 – kućica CIM sa slike (R30), CUV uz NHM prethodnog tovara, CIT 18 (mase i original po znacima), arhiviranje bez SMTP-a, izvor tmp podataka.</summary>
    [TestClass]
    public class V303Tests
    {
        private const string Share = @"\\192.168.1.6\Cloude\Agenti\CimProvera\testovi";
        private static string Folder => Environment.GetEnvironmentVariable("CIMPROVERA_TESTOVI") is string f && f.Length > 0 ? f : Share;
        private static readonly Dictionary<string, Ishod> kes = new Dictionary<string, Ishod>(StringComparer.OrdinalIgnoreCase);

        private static Ishod Proveri(string relativno)
        {
            lock (kes)
            {
                if (kes.TryGetValue(relativno, out var r)) return r;
                var p = Path.Combine(Folder, relativno);
                if (!File.Exists(p)) Assert.Inconclusive("Nema primera: " + p);
                var cfg = RegresijaTests.Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje303"));
                var pr = PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null);
                using (var c = new LokalniCitac(cfg, null, pr.Kp))
                    r = new Obrada(pr, c, null, cfg.PragPouzdanosti).Proveri(new[] { (Path.GetFileName(p), File.ReadAllBytes(p)) }, "posiljalac@firma.rs", false, new DateTime(2026, 11, 17), false);
                return kes[relativno] = r;
            }
        }

        private static IEnumerable<Nalaz> Svi(Ishod i) => i.Provera.Listovi.SelectMany(l => l.Nalazi).SelectMany(n => new[] { n }.Concat(n.Takodje));
        private static string Opis(Ishod i) => string.Join("\n", Svi(i).Select(n => n.Tezina + " " + n.Pravilo + " r" + n.Rubrika + (n.SamoInterno ? " INT" : "") + ": " + n.Tekst));

        // ------------------------------------------------------------------------------------------- 1.1 R30 – kućica CIM sa slike

        /// <summary>
        /// v3.0.3 (1.1): R30 „polje CIM nije označeno“ samo kada su obe kućice pouzdano prazne. UID 114 (treći primerak – šablon pomeren
        /// ~0,02), UID 3 (12 е въглища, 5 рилс), UID 48/52 (bled sken, kvačica preko ivice), UID 236/263 (odsečen vrh strane).
        /// </summary>
        [DataTestMethod, TestCategory("Regresija")]
        [DataRow(@"v302\CIM WAGON 020-1 stampa.pdf")]
        [DataRow(@"v303\12 е въглища.pdf")]
        [DataRow(@"v303\5 рилс.pdf")]
        [DataRow(@"v303\doc00429320261114120847.pdf")]
        [DataRow(@"v303\ex docs.pdf")]
        public void R30_nema_kada_kucica_CIM_nije_pouzdano_prazna(string fajl)
        {
            var i = Proveri(fajl);
            Assert.IsTrue(i.Provera.Listovi.Count > 0, fajl);
            Assert.IsFalse(Svi(i).Any(n => n.Pravilo == "R30"), fajl + ": R30\n" + Opis(i));
            // nesigurna kućica nije razlog da pošiljalac šalje novi dokument
            Assert.IsFalse(Svi(i).Any(n => n.Pravilo == "CITLJIVOST" && n.Rubrika == "30" && !n.SamoInterno), fajl + ": CITLJIVOST r30 ide pošiljaocu\n" + Opis(i));
        }

        [TestMethod, TestCategory("Regresija")]
        public void Kucica_CIM_trecg_primerka_UID_114_je_oznacena()
        {
            // treći primerak „CIM WAGON 020-1 stampa.pdf“: X u kućici CIM je jasno vidljiv (slika proverena) – svi listovi su CIM sa X
            var i = Proveri(@"v302\CIM WAGON 020-1 stampa.pdf");
            foreach (var l in i.Provera.Listovi) Assert.IsTrue(l.Cim.R30.Cim, l.Oznaka + ": " + l.Cim.R30.Status + "\n" + Opis(i));
        }

        // ------------------------------------------------------------------------------------------- 2.1 CIT 18

        [TestMethod]
        public void CIT18_i_original_po_ponudi_relaciji_robi_i_periodu()
        {
            string Mesto(int? id) => id == 54 || id == 55 ? "bor" : id == 457 ? "sturovo" : id == 1504 ? "zabrze" : "";
            var cit = new UpisUBazu.ZnaciCit18 { Ponuda = "8957", O = 457, U = 55, Nhm4 = "2704", Primljeno = new DateTime(2026, 11, 16, 15, 40, 0) };
            var orig = new UpisUBazu.ZnaciCit18 { Ponuda = "8957", O = 1504, U = 54, Nhm4 = "", Primljeno = new DateTime(2026, 11, 17, 9, 27, 0) };
            // UID 184 (CIT 18, Štúrovo → BOR TERETNA) i UID 230 (CIM – overen, Zabrze → BOR, NHM nepročitan): ista ponuda, isto mesto, 0,7 dana
            StringAssert.Contains(UpisUBazu.SlazeSeCit18(cit, orig, Mesto), "ponuda 8957");
            Assert.IsNull(UpisUBazu.SlazeSeCit18(cit, new UpisUBazu.ZnaciCit18 { Ponuda = "9001", U = 54, Primljeno = orig.Primljeno }, Mesto), "druga ponuda");
            Assert.IsNull(UpisUBazu.SlazeSeCit18(cit, new UpisUBazu.ZnaciCit18 { Ponuda = "8957", U = 54, Primljeno = orig.Primljeno.Value.AddDays(11) }, Mesto), "više od 10 dana");
            Assert.IsNull(UpisUBazu.SlazeSeCit18(cit, new UpisUBazu.ZnaciCit18 { Ponuda = "8957", U = 999, O = 998, Primljeno = orig.Primljeno }, Mesto), "druga relacija");
            Assert.IsNull(UpisUBazu.SlazeSeCit18(cit, new UpisUBazu.ZnaciCit18 { Ponuda = "8957", U = 54, Nhm4 = "1001", Primljeno = orig.Primljeno }, Mesto), "druga roba");
        }

        [TestMethod]
        public void Masa_jedinica_je_u_istom_redu_ne_oznaka_sledeceg_reda()
        {
            // „N: 41 050\nT: 21 900“ – „T“ iz sledećeg reda nije jedinica „t“ (3.0.2: neto 41 050 t → „više od 100 t po kolima“)
            var c = new CimList { R25 = new Masa { Status = StatusRubrike.Popunjeno, Tekst = "N: 41 050\nT: 21 900\nB: 62 950", Pouzdanost = 0.99 } };
            var (t, n, b, _) = PosaoIzDokumenata.Mase(c);
            Assert.AreEqual(41050, n); Assert.AreEqual(21900, t); Assert.AreEqual(62950, b);
            c.R25 = new Masa { Status = StatusRubrike.Popunjeno, Tekst = "Neto 41,05 t\nTara 21,9 t", Pouzdanost = 0.99 };
            (t, n, b, _) = PosaoIzDokumenata.Mase(c);
            Assert.AreEqual(41050, n); Assert.AreEqual(21900, t);
        }

        // ------------------------------------------------------------------------------------------- 3.2 arhiviranje bez SMTP-a

        [TestMethod]
        public void Arhiviranje_ne_inicijalizuje_SMTP()
        {
            Assert.IsTrue(Program.BezSlanja(new[] { "arhiviraj" }));
            Assert.IsTrue(Program.BezSlanja(new[] { "arhiviraj", "--od-uid", "191" }));
            foreach (var k in new[] { new string[0], new[] { "--dry-run" }, new[] { "ponovna-obrada" }, new[] { "validate", "x" } }) Assert.IsFalse(Program.BezSlanja(k), string.Join(" ", k));
            // bez postavljenog transporta (režim arhiviranja) slanje nije moguće – greška, ne mail
            var stari = Slanje.Transport;
            try
            {
                Slanje.Transport = (c, m) => throw new InvalidOperationException("Slanje nije podešeno (transport nije postavljen) – CimRadionica ne šalje mail.");
                var cfg = AgentConfig.Ucitaj(new NameValueCollection { ["Rezim"] = "Test", ["TestPrimalac"] = Slanje.TestnaAdresa });
                var m = new MimeKit.MimeMessage { Subject = "proba" };
                m.From.Add(MimeKit.MailboxAddress.Parse(Slanje.TestnaAdresa)); m.To.Add(MimeKit.MailboxAddress.Parse(Slanje.TestnaAdresa));
                m.Body = new MimeKit.TextPart("plain") { Text = "proba" };
                var ex = Assert.ThrowsException<InvalidOperationException>(() => Slanje.Posalji(cfg, m));
                StringAssert.Contains(ex.Message, "nije podešeno");
            }
            finally { Slanje.Transport = stari; }
        }

        // ------------------------------------------------------------------------------------------- 3.5 izvor tmp podataka

        [TestMethod]
        public void Izvor_alat_i_agent_brisanje_samo_svog_izvora()
        {
            Assert.AreEqual("Agent", AgentConfig.Ucitaj(new NameValueCollection()).IzvorUpisa);
            Assert.AreEqual("Alat", AgentConfig.Ucitaj(new NameValueCollection { ["IzvorUpisa"] = "alat" }).IzvorUpisa);
            Assert.AreEqual("Agent", AgentConfig.Ucitaj(new NameValueCollection { ["IzvorUpisa"] = "nešto" }).IzvorUpisa);
            var rp = RadniProstor.Otvori(Path.Combine(Path.GetTempPath(), "CimProveraTest", "rp303"));
            Assert.AreEqual("Alat", rp.Konfiguracija(new PodesavanjaAlata { Online = false }).IzvorUpisa, "alat piše Izvor = 'Alat'");
            // brisanje (--ispocetka / dugme u alatu / ponovna obrada agenta): svaka naredba samo nad svojim izvorom, tmpSinonimi i tmpNHM se ne diraju
            var naredbe = RadniProstor.ObrisiSql.Split(';').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
            Assert.AreEqual(8, naredbe.Count);
            Assert.IsTrue(naredbe.All(x => x.EndsWith("WHERE Izvor = @izvor", StringComparison.Ordinal)), string.Join("\n", naredbe));
            Assert.IsFalse(RadniProstor.ObrisiSql.Contains("tmpSinonimi") || RadniProstor.ObrisiSql.Contains("tmpNHM"));
        }

        [TestMethod]
        public void OP_919_skripta_izvor_zamenjena_i_razlozi_CIT18()
        {
            var p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Saobracaj\Planiranje\SQL\OP_919_tmp_Izvor_Zamenjena.sql");
            if (!File.Exists(p)) Assert.Inconclusive("Nema " + p);
            var s = File.ReadAllText(p);
            foreach (var t in new[] { "tmpJobs", "tmpJobsPolje", "tmpJobsIstorija", "tmpJobsDogadjaj", "tmpTeretnica", "tmpTeretnicaStavke", "tmpDokument" }) StringAssert.Contains(s, "N'" + t + "'");
            StringAssert.Contains(s, "0x416C6174");
            StringAssert.Contains(s, "ZamenjenaTeretnicaID");
            StringAssert.Contains(s, "'CIT18','CIT18_BEZ_ORIGINALA'");
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(s, @"(?i)(ALTER\s+TABLE|UPDATE|DELETE\s+FROM|INSERT)\s+dbo\."), "OP_919 dira samo planiranje.tmp*");
        }
    }
}
