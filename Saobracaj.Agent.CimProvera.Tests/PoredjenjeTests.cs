using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MailKit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Model;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>Komanda reset-stanje i poređenje novog izveštaja sa prvim izveštajem za isti fajl.</summary>
    [TestClass]
    public class PoredjenjeTests
    {
        private const string TeloPre = @"Pošiljka: 83 00704-7 3832 95147-8
[TEST] Dolazni mail od a@b.c …

Provereno: LDV.pdf

GREŠKE
1. [LDV.pdf] Rubrika 16 – Nedostaje datum.  Upisati datum i čas.
   Upisano: 12.11.
2. [LDV.pdf] Rubrika 24 – NHM ima 4 cifre.
3. [drugi.pdf] Rubrika 1 – Bez zemlje.
4. [LDV.pdf] Nije pronađen tovarni list za spisak.

UPOZORENJA
1. [LDV.pdf] Rubrika 4 – Zemlja „Romania“ (RO).

PREPORUKE
1. [LDV.pdf] Rubrika 99 – Preporuka.

--
Verzija pravila 2.5.";

        private const string TeloEn = @"Checked: a.pdf

ERRORS
1. [a.pdf] Box 13.3 – Missing.
WARNINGS
1. [a.pdf] Box 57 – Section.
--
Rules version 2.6.";

        [TestMethod]
        public void Stavke_iz_teksta_izvestaja()
        {
            var s = Poredjenje.Stavke(TeloPre);
            Assert.AreEqual(5, s.Count, string.Join("\n", s));
            Assert.AreEqual(4, s.Count(x => x.Vrsta == "greška"));
            Assert.AreEqual("16|24|1|", string.Join("|", s.Where(x => x.Vrsta == "greška").Select(x => x.Rubrika)));
            Assert.AreEqual("LDV.pdf", s[0].Dokument);
            StringAssert.StartsWith(s[0].Tekst, "Nedostaje datum");
            Assert.AreEqual("upozorenje|4", s[4].Kljuc);
            var en = Poredjenje.Stavke(TeloEn);
            Assert.AreEqual("greška|13.3;upozorenje|57", string.Join(";", en.Select(x => x.Kljuc)));
        }

        [TestMethod]
        public void Stavke_samo_za_fajl()
        {
            var s = Poredjenje.ZaFajlove(Poredjenje.Stavke(TeloPre), new[] { "ldv.PDF" });
            Assert.AreEqual(4, s.Count);
            Assert.IsFalse(s.Any(x => x.Dokument == "drugi.pdf"));
        }

        [TestMethod]
        public void Razlike_po_rubrici_i_vrsti()
        {
            var pre = Poredjenje.Stavke(TeloPre);
            var sada = Poredjenje.Stavke(@"GREŠKE
1. [LDV.pdf] Rubrika 16 – Rubrika 16 bez datuma i časa (drugi tekst).
2. [LDV.pdf] Rubrika 9 – Prazna, a r18 WAGON LIST.
3. [LDV.pdf] Nije pronađen tovarni list za spisak.
UPOZORENJA
1. [LDV.pdf] Rubrika 4 – Zemlja „Romania“ (RO).
2. [LDV.pdf] Rubrika 24 – Sada samo upozorenje.
--");
            var (nove, nestale) = Poredjenje.Razlike(pre, sada);
            Assert.AreEqual("greška|9;upozorenje|24", string.Join(";", nove.Select(x => x.Kljuc).OrderBy(x => x)));
            Assert.AreEqual("greška|1;greška|24", string.Join(";", nestale.Select(x => x.Kljuc).OrderBy(x => x)));
        }

        [TestMethod]
        public void Tekst_poredjenja()
        {
            var prvi = new PrviIzvestaj { Uid = 1276, Vreme = new DateTimeOffset(2026, 11, 12, 8, 57, 0, TimeSpan.FromHours(1)), Status = "Greske+RucnaProvera", Greske = 4, Upozorenja = 1,
                                          Pravila = { "R16", "R24", "OBRAZAC" }, Fajlovi = { "LDV.pdf", "drugi.pdf" }, IzvestajNaslov = "Cim novi – LDV.pdf – 4 greške, 1 upozorenje" };
            var sada = "GREŠKE\n1. [LDV.pdf] Rubrika 9 – Prazna.\n2. [LDV.pdf] Rubrika 16 – Bez datuma.\n--\n";
            var t = Poredjenje.Tekst(new List<Poredjenje.PrviZaFajl> { new Poredjenje.PrviZaFajl { Prvi = prvi, Fajlovi = new List<string> { "LDV.pdf" }, StavkePre = Poredjenje.Stavke(TeloPre) } },
                null, 2, 0, new[] { "R16", "G3" }, sada, out var zaLog);
            StringAssert.Contains(t, "Fajl: LDV.pdf");
            StringAssert.Contains(t, "Prvi izveštaj: 12.11.2026 08:57 (UID 1276");
            StringAssert.Contains(t, "prvi mail je imao i: drugi.pdf");
            StringAssert.Contains(t, "Pre: 4 grešaka, 1 upozorenja");
            StringAssert.Contains(t, "Sada: 2 grešaka, 0 upozorenja");
            StringAssert.Contains(t, "Pravila – nova: G3; nestala: OBRAZAC, R24");
            StringAssert.Contains(t, "Nove stavke (1):\r\n  + greška, rubrika 9 [LDV.pdf] – Prazna.");
            StringAssert.Contains(t, "Nestale stavke (3):");
            StringAssert.Contains(t, "  − greška, rubrika 24 [LDV.pdf]");
            StringAssert.Contains(t, "  − upozorenje, rubrika 4 [LDV.pdf]");
            Assert.IsFalse(t.Contains("drugi.pdf] – Bez zemlje"), t);
            Assert.AreEqual("UID 1276 4/1 → 2/0, nove stavke 1, nestale 3", zaLog);
        }

        [TestMethod]
        public void Tekst_poredjenja_kada_sada_nema_izvestaja_i_prvi_nije_u_sanducetu()
        {
            var prvi = new PrviIzvestaj { Uid = 1254, Vreme = DateTimeOffset.Now, Status = "Greske", Greske = 117, Upozorenja = 15, Fajlovi = { "a.pdf" } };
            var t = Poredjenje.Tekst(new List<Poredjenje.PrviZaFajl> { new Poredjenje.PrviZaFajl { Prvi = prvi, Fajlovi = new List<string> { "a.pdf" }, StavkePre = null } },
                "tovarni list nije pronađen", null, null, null, null, out var zaLog);
            StringAssert.Contains(t, "Pre: 117 grešaka, 15 upozorenja");
            StringAssert.Contains(t, "Sada: tovarni list nije pronađen");
            StringAssert.Contains(t, "Tekst prvog izveštaja nije pronađen u sandučetu");
            var nemaCim = new PrviIzvestaj { Uid = 1253, Status = "NemaCim", Fajlovi = { "a.pdf" } };
            var t2 = Poredjenje.Tekst(new List<Poredjenje.PrviZaFajl> { new Poredjenje.PrviZaFajl { Prvi = nemaCim, Fajlovi = new List<string> { "a.pdf" } } },
                null, 3, 1, new[] { "R1" }, "GREŠKE\n1. [a.pdf] Rubrika 1 – x\n--", out _);
            StringAssert.Contains(t2, "Pre: bez izveštaja (nije pronađen tovarni list)");
            StringAssert.Contains(t2, "Nove stavke (1):");
        }

        [TestMethod]
        public void Html_izvestaj_u_stavke()
        {
            var html = "<h3>GREŠKE (2)</h3><table><tr><th>#</th></tr><tr><td>1</td><td>a.pdf</td><td><b>16</b></td><td>x</td><td>y</td><td><b>Detalj</b> opis</td></tr>"
                       + "<tr><td>2</td><td>a.pdf</td><td><b>G</b></td><td>x</td><td></td><td>Opšte</td></tr></table>"
                       + "<h3>UPOZORENJA (1)</h3><table><tr><td>a.pdf</td><td><b>21</b></td><td>30 kola</td><td></td><td>Broj kola</td></tr></table>";
            var s = Poredjenje.Stavke(Poredjenje.HtmlUTekst(html));
            Assert.AreEqual("greška|16;greška|;upozorenje|21", string.Join(";", s.Select(x => x.Kljuc)));
            Assert.AreEqual("Detalj opis", s[0].Tekst);
        }

        // ------------------------------------------------------------------------------------------ indeks i reset

        private static string NoviFolder()
        {
            var d = Path.Combine(Path.GetTempPath(), "cimprovera-test-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(d);
            return d;
        }

        private static AgentConfig Cfg(string koren)
        {
            var cfg = AgentConfig.Ucitaj();
            cfg.StanjeFolder = Path.Combine(koren, "stanje");
            cfg.LogFolder = Path.Combine(koren, "logs");
            Directory.CreateDirectory(cfg.StanjeFolder); Directory.CreateDirectory(cfg.LogFolder);
            return cfg;
        }

        private static void PripremiStanje(AgentConfig cfg)
        {
            var s = Stanje.Ucitaj(cfg.StanjeFolder);
            s.Folder("INBOX").UidValidity = 77; s.Folder("INBOX").PoslednjiUid = 1277;
            s.Poruke["m1@x"] = new Stanje.PorukaStanje { Status = "Greske", Folder = "INBOX", Uid = 1254, Vreme = new DateTime(2026, 11, 10, 14, 37, 0), HesPriloga = "aa", Greske = 117, Upozorenja = 15 };
            s.Poruke["m2@x"] = new Stanje.PorukaStanje { Status = "Greske+RucnaProvera", Folder = "INBOX", Uid = 1276, Vreme = new DateTime(2026, 11, 12, 8, 57, 0), HesPriloga = "bb", Upozorenja = 5 };
            s.Poruke["m3@x"] = new Stanje.PorukaStanje { Status = "Preskoceno", Folder = "INBOX", Uid = 1277, Vreme = new DateTime(2026, 11, 12, 8, 59, 0) };
            s.Upozorenja["imap"] = DateTime.Now;
            s.Sacuvaj();
            var kes = Path.Combine(cfg.StanjeFolder, "kes"); Directory.CreateDirectory(kes);
            File.WriteAllText(Path.Combine(kes, "aa_strane_1234abcd.json"), "[]");
            File.WriteAllText(Path.Combine(kes, "aa_L15_ab_cd.json"), "{}");
            File.WriteAllText(Path.Combine(kes, "bb_L2_ab.json"), "{}");
            var pk = Path.Combine(cfg.StanjeFolder, "pravila_kes"); Directory.CreateDirectory(pk);
            File.WriteAllText(Path.Combine(pk, "cim_validation_rules.json"), "{}");
            File.WriteAllText(Path.Combine(cfg.StanjeFolder, "heartbeat.txt"), "x");
            File.WriteAllText(Path.Combine(cfg.LogFolder, AgentConfig.AgentNaziv + "-20261112.log"), string.Join("\n",
                "2026-11-12 08:57:02 +01:00 INFO  INBOX (dolazni): novih poruka 1",
                "2026-11-12 08:57:02 +01:00 INFO    UID 1276 m2@x: 1 priloga, od a@b.c",
                "2026-11-12 08:57:07 +01:00 INFO    prilog LDV NHM 9922 Busca - Carpinis_31 wgs.pdf (Pdf): str.1 cim(HU-RCH)",
                "2026-11-12 08:57:07 +01:00 INFO    UID 1276: 5 tovarnih listova, grešaka 0, upozorenja 5, RUČNA PROVERA. Nalazi: OBRAZAC",
                "2026-11-12 08:57:07 +01:00 INFO      izveštaj poslat na t@x: Cim novi 12.11 – LDV NHM 9922 Busca - Carpinis_31 wgs.pdf – 0 grešaka, 5 upozorenja",
                "2026-11-10 14:37:02 +01:00 INFO    UID 1254 m1@x: 18 priloga, od p@x",
                "2026-11-10 14:37:04 +01:00 INFO    UID 1254: 8 list(ova), grešaka 117, upozorenja 15. Nalazi: G5 R1 X2",
                "") + "\n", Encoding.UTF8);
        }

        [TestMethod]
        public void Reset_stanja_kopija_brisanje_i_nulta_tacka()
        {
            var koren = NoviFolder();
            try
            {
                var cfg = Cfg(koren);
                PripremiStanje(cfg);
                var log = new Log(cfg.LogFolder) { Tiho = true };
                var pre = DateTimeOffset.Now.AddSeconds(-1);

                Assert.AreEqual(0, ResetStanja.Pokreni(cfg, log, "nebitno", bezZadatka: true));

                var s = Stanje.Ucitaj(cfg.StanjeFolder);
                Assert.AreEqual(0, s.Poruke.Count); Assert.AreEqual(0, s.Folderi.Count);
                Assert.AreEqual(1, s.Upozorenja.Count, "vreme upozorenja ostaje");
                Assert.IsTrue(s.NultaTacka >= pre && s.PrviIzvestajiIzgraditi);
                Assert.AreEqual(0, Directory.GetFiles(Path.Combine(cfg.StanjeFolder, "kes")).Length);
                Assert.IsTrue(File.Exists(Path.Combine(cfg.StanjeFolder, "pravila_kes", "cim_validation_rules.json")));
                Assert.IsTrue(File.Exists(Path.Combine(cfg.StanjeFolder, "heartbeat.txt")));

                // rezervna kopija celog foldera stanja
                Assert.IsTrue(s.ResetArhiva.StartsWith(Path.Combine(cfg.StanjeFolder, "arhiva")));
                var stara = Json.Iz<Stanje>(File.ReadAllText(Path.Combine(s.ResetArhiva, "stanje.json")));
                Assert.AreEqual(3, stara.Poruke.Count);
                Assert.IsTrue(File.Exists(Path.Combine(s.ResetArhiva, "kes", "aa_strane_1234abcd.json")));
                Assert.IsTrue(File.Exists(Path.Combine(s.ResetArhiva, "pravila_kes", "cim_validation_rules.json")));

                var izv = File.ReadAllText(s.ResetArhiva + "_izvestaj.txt");
                StringAssert.Contains(izv, "spisak obrađenih mailova u stanje.json: 3 poruka");
                StringAssert.Contains(izv, "INBOX (poslednji UID 1277)");
                StringAssert.Contains(izv, "OCR keš (tekst strana): 1 fajlova");
                StringAssert.Contains(izv, "keš rezultata provere: 2 fajlova");
                StringAssert.Contains(izv, "pravila_kes\\ (1 fajlova)");
                var logTekst = File.ReadAllText(Directory.GetFiles(cfg.LogFolder, AgentConfig.AgentNaziv + "-" + DateTime.Now.ToString("yyyyMMdd") + ".log")[0]);
                StringAssert.Contains(logTekst, "Stanje resetovano ");
                StringAssert.Contains(logTekst, ", rezervna kopija u " + s.ResetArhiva);

                // drugi reset ne kopira ranije arhive u novu
                System.Threading.Thread.Sleep(1100);
                Assert.AreEqual(0, ResetStanja.Pokreni(cfg, log, "nebitno", bezZadatka: true));
                var s2 = Stanje.Ucitaj(cfg.StanjeFolder);
                Assert.IsFalse(Directory.Exists(Path.Combine(s2.ResetArhiva, "arhiva")));
            }
            finally { try { Directory.Delete(koren, true); } catch { } }
        }

        [TestMethod]
        public void Indeks_prvih_iz_rezervne_kopije_i_loga()
        {
            var koren = NoviFolder();
            try
            {
                var cfg = Cfg(koren);
                PripremiStanje(cfg);
                var log = new Log(cfg.LogFolder) { Tiho = true };
                Assert.AreEqual(0, ResetStanja.Pokreni(cfg, log, "nebitno", bezZadatka: true));
                var s = Stanje.Ucitaj(cfg.StanjeFolder);

                var ind = Poredjenje.Izgradi(cfg, s, new IMailFolder[0], null);
                Assert.AreEqual(2, ind.Poruke.Count, "preskočene poruke ne ulaze");
                var ldv = ind.Za(new[] { "LDV NHM 9922 BUSCA - Carpinis_31 wgs.pdf", "nepoznat.pdf" });
                Assert.AreEqual(1, ldv.Count);
                Assert.AreEqual(1276, ldv[0].prvi.Uid);
                Assert.AreEqual("OBRAZAC", string.Join(" ", ldv[0].prvi.Pravila));
                Assert.AreEqual(5, ldv[0].prvi.Upozorenja);
                StringAssert.StartsWith(ldv[0].prvi.IzvestajNaslov, "Cim novi 12.11");
                Assert.AreEqual("G5 R1 X2", string.Join(" ", ind.Poruke.First(p => p.Uid == 1254).Pravila));

                ind.Sacuvaj(cfg.StanjeFolder);
                Assert.AreEqual(1, IndeksPrvih.Ucitaj(cfg.StanjeFolder).Fajlovi.Count);
            }
            finally { try { Directory.Delete(koren, true); } catch { } }
        }
    }
}
