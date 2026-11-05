using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.TelegramiImport.Parsing;
using Saobracaj.Agent.TelegramiImport.Pdf;

namespace Saobracaj.Agent.TelegramiImport.Tests
{
    /// <summary>
    /// Kriterijumi prihvatanja (specifikacija, odeljak 5) nad pravim PDF-ovima iz izvoza sandučeta.
    /// Izvoz sadrži lične podatke i NIJE u repou: putanja se zadaje promenljivom TELEGRAMI_TESTDATA
    /// (folder telegrami_export sa podfolderima 0001_..., 0002_...). Bez nje su testovi Inconclusive.
    /// </summary>
    [TestClass]
    public class TelegramiPdfTests
    {
        private static string Koren
        {
            get
            {
                var p = Environment.GetEnvironmentVariable("TELEGRAMI_TESTDATA");
                if (string.IsNullOrEmpty(p))
                    p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Telegrami", "telegrami_export");
                return p;
            }
        }

        private static ParsiranTelegram Ucitaj(string rb, string deoNazivaPdf = null)
        {
            if (!Directory.Exists(Koren)) Assert.Inconclusive("Nema test podataka (TELEGRAMI_TESTDATA): " + Koren);
            var dir = Directory.GetDirectories(Koren, rb + "_*").FirstOrDefault();
            if (dir == null) Assert.Inconclusive("Nema foldera " + rb + " u " + Koren);
            var pdf = Directory.GetFiles(dir, "*.pdf").FirstOrDefault(f => deoNazivaPdf == null || Path.GetFileName(f).Contains(deoNazivaPdf));
            Assert.IsNotNull(pdf, "PDF u " + dir);

            string naslov = null; DateTime? datum = null;
            var poruka = Path.Combine(dir, "poruka.txt");
            if (File.Exists(poruka))
                foreach (var l in File.ReadLines(poruka).Take(10))
                {
                    if (l.StartsWith("Naslov: ")) naslov = l.Substring(8);
                    if (l.StartsWith("Datum: ") && DateTimeOffset.TryParse(l.Substring(7), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) datum = d.LocalDateTime;
                }
            return TelegramParser.Parsiraj(PdfTekst.Izvuci(File.ReadAllBytes(pdf)), naslov, datum);
        }

        private static DateTime D(string s) { return DateTime.ParseExact(s, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture); }

        private static void Red(Stavka s, string od, string @do, string vaziOd, string vaziDo, string kolosek = null)
        {
            Assert.AreEqual(od, s.OdTekst, s.ToString());
            Assert.AreEqual(@do, s.DoTekst, s.ToString());
            Assert.AreEqual(D(vaziOd), s.VaziOd, s.ToString());
            Assert.AreEqual(D(vaziDo), s.VaziDo, s.ToString());
            if (kolosek != null) Assert.AreEqual(kolosek, s.Kolosek, s.ToString());
        }

        [TestMethod]
        public void T288F_SestTacaka()
        {
            var t = Ucitaj("0090");
            Assert.AreEqual(288, t.Broj);
            Assert.AreEqual(new DateTime(2026, 10, 29), t.Datum);
            Assert.IsTrue(t.ZaUpis);
            Assert.AreEqual(6, t.Stavke.Count);
            Red(t.Stavke[0], "Stevanac", "Braljina", "02.11.2026 09:00", "02.11.2026 12:15", "kolosek");
            Red(t.Stavke[1], "Dunis", "Korman", "03.11.2026 08:00", "03.11.2026 14:00", "desni");
            Red(t.Stavke[2], "Aleksinac", "Grejac", "04.11.2026 08:00", "04.11.2026 14:00", "desni");
            Red(t.Stavke[3], "Bagrdan", "Jagodina", "03.11.2026 09:00", "04.11.2026 14:00", "levi");
            Red(t.Stavke[4], "Aleksinac", "Grejac", "05.11.2026 08:00", "05.11.2026 14:00", "levi");
            Red(t.Stavke[5], "Paracin", "Cicevac", "06.11.2026 08:00", "06.11.2026 14:00", "levi");
            StringAssert.StartsWith(t.Stavke[3].Napomena, "4.");
        }

        [TestMethod]
        public void T3F_TriDnevnaIntervala_KaoRucniUnos9130do9132()
        {
            var t = Ucitaj("0124");
            Assert.AreEqual(3, t.Broj);
            Assert.AreEqual(3, t.Stavke.Count);
            Red(t.Stavke[0], "Razana", "Kosjeric", "03.11.2026 07:50", "06.11.2026 09:30", "pruga");
            Red(t.Stavke[1], "Razana", "Kosjeric", "03.11.2026 09:50", "06.11.2026 10:50");
            Red(t.Stavke[2], "Razana", "Kosjeric", "03.11.2026 12:10", "06.11.2026 14:50");
        }

        [TestMethod]
        public void T259f_OpsegDanaSaDnevnimIntervalom()
        {
            var t = Ucitaj("0049");
            Assert.AreEqual(1, t.Stavke.Count);
            Red(t.Stavke[0], "Kragujevac", "Grosnica", "27.10.2026 08:00", "23.11.2026 12:00");
        }

        [TestMethod]
        public void T301F_NocniIntervali()
        {
            var t = Ucitaj("0098");
            Assert.AreEqual(3, t.Stavke.Count);
            Red(t.Stavke[0], "Pancevo Glavna", "Pancevo Glavna", "02.11.2026 23:30", "04.11.2026 02:30", "1,2,3,4,5");
            Red(t.Stavke[1], "Krnjaca", "Krnjaca", "04.11.2026 23:30", "05.11.2026 02:30", "1");
            Red(t.Stavke[2], "Krnjaca", "Krnjaca", "05.11.2026 23:30", "06.11.2026 02:30", "4");
        }

        [TestMethod]
        public void T12f_TackeABC_NapomeneSeNeMesaju()
        {
            var t = Ucitaj("0126");
            Assert.AreEqual(12, t.Broj);
            Assert.AreEqual(3, t.Stavke.Count);
            Red(t.Stavke[0], "Vrbas", "Vrbas", "03.11.2026 06:00", "03.11.2026 18:00", "1,2,5,6,7,8");
            Red(t.Stavke[1], "Vrbas", "Vrbas", "04.11.2026 06:00", "06.11.2026 18:00", "svi");
            Red(t.Stavke[2], "Vrbas", "Vrbas", "07.11.2026 06:00", "15.11.2026 18:00", "1,2,5,6,7,8");
            StringAssert.StartsWith(t.Stavke[0].Napomena, "a)");
            StringAssert.StartsWith(t.Stavke[1].Napomena, "b)");
            Assert.IsFalse(t.Stavke[0].Napomena.Contains("b)"));
        }

        [TestMethod]
        public void T270F_CetiriOdvojenaDana()
        {
            var t = Ucitaj("0067");
            Assert.AreEqual(new DateTime(2026, 10, 27), t.Datum, "datum iz PDF-a, ne iz naslova (27.11)");
            Assert.AreEqual(4, t.Stavke.Count);
            var dani = new[] { 3, 10, 17, 24 };
            for (int i = 0; i < 4; i++)
                Red(t.Stavke[i], "Bor Terena", "Rgotina", dani[i].ToString("00") + ".11.2026 12:30", dani[i].ToString("00") + ".11.2026 14:30");
        }

        [TestMethod]
        public void T4f_DvaReda_DrzavnaGranica()
        {
            var t = Ucitaj("0117");
            Assert.AreEqual(2, t.Stavke.Count);
            Red(t.Stavke[0], "Bujanovac", "Bukarevac", "03.11.2026 09:00", "03.11.2026 14:00");
            Red(t.Stavke[1], "Ristovac", "#GRANICA", "04.11.2026 09:00", "04.11.2026 14:00");
        }

        [TestMethod]
        public void T10F_VanSnageCelog290()
        {
            var t = Ucitaj("0121");
            Assert.IsFalse(t.ZaUpis);
            Assert.AreEqual(1, t.Ciljevi.Count);
            var c = t.Ciljevi[0];
            Assert.AreEqual(290, c.Broj);
            Assert.AreEqual(new DateTime(2026, 10, 30), c.Datum);
            Assert.AreEqual(Vrsta.VanSnage, c.Akcija);
            Assert.IsNull(c.Tacka);
            Assert.AreEqual(0, c.Dani.Count, "van snage celog telegrama, ne samo dana 03-06.11");
            Assert.AreEqual("VAN SNAGE TEL. BR. 10F ", CiljniTelegram.Oznaka(c.Akcija, t.Oznaka));
        }

        [TestMethod]
        public void T244F_Otkazuje209F()
        {
            var t = Ucitaj("0028");
            Assert.AreEqual(1, t.Ciljevi.Count);
            Assert.AreEqual(209, t.Ciljevi[0].Broj);
            Assert.AreEqual(new DateTime(2026, 10, 22), t.Ciljevi[0].Datum);
            Assert.AreEqual(Vrsta.Otkazivanje, t.Ciljevi[0].Akcija);
        }

        [TestMethod]
        public void T273F_VanSnage166_NoviJePutniPrelaz_NeUpisujeSe()
        {
            var t = Ucitaj("0077");
            Assert.IsTrue(t.Ciljevi.Any(c => c.Broj == 166 && c.Akcija == Vrsta.VanSnage));
            Assert.IsTrue(t.Vrste.Contains(Vrsta.PutniPrelaz));
            Assert.IsFalse(t.ZaUpis);
        }

        [TestMethod]
        public void T222F_LaganaVoznja_NeUpisujeSe()
        {
            var t = Ucitaj("0029");
            Assert.IsTrue(t.Vrste.Contains(Vrsta.LaganaVoznja));
            Assert.IsFalse(t.ZaUpis);
        }

        [TestMethod]
        public void T217f_RedVoznjeRV_NeUpisujeSe()
        {
            var t = Ucitaj("0010");
            Assert.AreEqual("RV", t.Izdavac);
            Assert.IsTrue(t.Vrste.Contains(Vrsta.RedVoznje));
            Assert.IsFalse(t.ZaUpis);
        }

        [TestMethod]
        public void T303F_Radovi_NeUpisujeSe()
        {
            var t = Ucitaj("0103");
            Assert.IsTrue(t.Vrste.Contains(Vrsta.Radovi));
            Assert.IsFalse(t.ZaUpis);
        }

        [TestMethod]
        public void T307F_Skretnica_DoOpoziva_KaoID9111()
        {
            var t = Ucitaj("0107");
            Assert.IsTrue(t.ZaUpis);
            Assert.AreEqual(1, t.Stavke.Count);
            Assert.AreEqual(Vrsta.Skretnica, t.Stavke[0].Vrsta);
            Red(t.Stavke[0], "Korman", "Korman", "30.10.2026 00:00", "31.12.2026 00:00", "skretnica br.14");
        }

        [TestMethod]
        public void T236_NarocitaPosiljka_KaoID9082()
        {
            var t = Ucitaj("0055");
            Assert.AreEqual(236, t.Broj);
            Assert.AreEqual(1, t.Stavke.Count);
            Assert.IsTrue(t.Stavke[0].NarocitaPosiljka);
            Red(t.Stavke[0], "Koper Luka", "Zemun", "27.10.2026 00:00", "31.12.2026 00:00", "pruga");
        }

        [TestMethod]
        public void Skenirani_PDF_NemaTeksta()
        {
            var t = Ucitaj("0058", "Scan20261027100248");
            Assert.IsTrue(t.BezTeksta);
        }
    }
}
