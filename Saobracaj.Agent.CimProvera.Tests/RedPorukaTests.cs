using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Common.Text;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>Neispravan Unicode tekst i red poruka: jedna poruka ne sme da zaustavi ostale (incident 13.11.2026 od 08:49).</summary>
    [TestClass]
    public class RedPorukaTests
    {
        private const string Hi = "\uD83D", Lo = "\uDE42";   // polovine para za 🙂 (U+1F642)

        [TestMethod]
        public void BezDijakritika_usamljeni_surogat_pocetak_sredina_kraj()
        {
            foreach (var s in new[] { Hi + "Šifra", "Ši" + Hi + "fra", "Šifra" + Hi, Lo + "abc", "ab" + Lo + "c", "abc" + Lo, Lo + Hi })
            {
                var r = TekstUtil.BezDijakritika(s);
                Assert.IsFalse(r.Any(char.IsSurrogate), "ostao usamljeni surogat: " + string.Join(" ", r.Select(c => ((int)c).ToString("X4"))));
                Assert.IsTrue(r.Contains("�"));
            }
            Assert.AreEqual("Sifra�", TekstUtil.BezDijakritika("Šifra" + Hi));
        }

        [TestMethod]
        public void BezDijakritika_null_prazno_kontrolni_znakovi_emoji()
        {
            Assert.AreEqual("", TekstUtil.BezDijakritika(null));
            Assert.AreEqual("", TekstUtil.BezDijakritika(""));
            Assert.AreEqual("a\0b\u0001\u001F\u007F\tc\r\n", TekstUtil.BezDijakritika("a\0b\u0001\u001F\u007F\tc\r\n"));
            // ispravan par surogata (emoji) – to je izazvalo pad: polovina para se normalizovala posebno
            Assert.AreEqual("Hvala " + Hi + Lo + " cdz", TekstUtil.BezDijakritika("Hvala " + Hi + Lo + " čđž"));
            Assert.AreEqual("Ljubicic", TekstUtil.BezDijakritika("Љубичић"));
        }

        [TestMethod]
        public void Normalizuj_i_Ocisti_ne_bacaju_izuzetak()
        {
            Assert.AreEqual("a�b", TekstUtil.Ocisti("a" + Hi + "b"));
            Assert.AreEqual("a" + Hi + Lo, TekstUtil.Ocisti("a" + Hi + Lo));
            Assert.AreEqual(2, TekstUtil.BrojNeispravnih(Hi + "x" + Hi + Lo + Lo));
            Assert.AreEqual("x�", TekstUtil.Normalizuj("x" + Lo, System.Text.NormalizationForm.FormD));
            Assert.AreEqual("", TekstUtil.Normalizuj(null, System.Text.NormalizationForm.FormC));
        }

        [TestMethod]
        public void Tekst_maila_sa_neispravnim_unicode_ne_rusi_obradu()
        {
            var tekst = "Re: CIM " + Hi + " Futog – Lovosice\r\nDear colleagues, please find attached " + Lo + Hi + Lo + " Best regards";
            Obrada.JeEngleskiIliNemacki(tekst);
            PraviloKp.Normalizuj(tekst);
            CimSablon.Norm(tekst);
            Assert.AreEqual("U+1F642", AgentRad.ZnakoviVanBmp("ok " + Hi + Lo + " " + Hi + Lo));
        }

        // ------------------------------------------------------------------------------------------- red poruka

        private static RedPoruka.Stavka S(long uid) => new RedPoruka.Stavka { Uid = uid, Folder = "INBOX", Kljuc = "agenti@kprevoz.co.rs|m" + uid + "@test", MessageId = "m" + uid + "@test", Od = "a@b.c", Naslov = "test " + uid };

        [TestMethod]
        public void Tri_poruke_druga_baca_izuzetak_prva_i_treca_obradjene()
        {
            var stanje = new Stanje();
            var fs = new Stanje.FolderStanje { PoslednjiUid = 10 };
            var obradjene = new List<long>();
            var q = new RedPoruka(stanje, fs, 3, null, false);
            var n = q.Obradi(new[] { S(11), S(12), S(13) }, (s, ps) =>
            {
                if (s.Uid == 12) throw new ArgumentException("Invalid Unicode code point found at index 0.");
                obradjene.Add(s.Uid); ps.Status = "Greske"; return Korak.Obradjena;
            });
            CollectionAssert.AreEqual(new long[] { 11, 13 }, obradjene);
            Assert.AreEqual(2, n);
            var p12 = stanje.Poruke[S(12).Kljuc];
            Assert.AreEqual(RedPoruka.StatusPrivremena, p12.Status);
            Assert.AreEqual(1, p12.Pokusaja);
            StringAssert.Contains(p12.PoslednjaGreska, "Invalid Unicode code point");
            Assert.AreEqual("Greske", stanje.Poruke[S(13).Kljuc].Status);
            Assert.AreEqual(11, fs.PoslednjiUid, "poslednji UID ne prelazi poruku koja čeka ponovni pokušaj");
        }

        [TestMethod]
        public void Poruka_pada_3_puta_rucna_provera_jedno_obavestenje_bez_cetvrtog_pokusaja()
        {
            var stanje = new Stanje();
            var fs = new Stanje.FolderStanje { PoslednjiUid = 10 };
            int pokusaja = 0, obavestenja = 0, druga = 0;
            for (int krug = 1; krug <= 4; krug++)
            {
                var q = new RedPoruka(stanje, fs, 3, null, false) { PosleZadnjegPokusaja = (s, ps) => obavestenja++ };
                q.Obradi(new[] { S(11), S(12) }, (s, ps) =>
                {
                    if (s.Uid == 11) { pokusaja++; throw new InvalidOperationException("pad " + pokusaja); }
                    druga++; ps.Status = "NemaCim"; return Korak.Gotova;
                });
                if (krug < 3) Assert.AreEqual(10, fs.PoslednjiUid, "krug " + krug);
            }
            Assert.AreEqual(3, pokusaja, "četvrti pokušaj se ne radi");
            Assert.AreEqual(1, obavestenja, "jedno interno obaveštenje");
            Assert.AreEqual(1, druga, "druga poruka je obrađena već u prvom krugu, i samo jednom");
            var p = stanje.Poruke[S(11).Kljuc];
            Assert.AreEqual(RedPoruka.StatusRucna, p.Status);
            Assert.AreEqual(3, p.Pokusaja);
            Assert.AreEqual(12, fs.PoslednjiUid);
        }

        [TestMethod]
        public void Upozorenje_najvise_jednom_na_sat_neuspelo_slanje_se_ne_trosi()
        {
            var st = new Stanje();
            var t0 = new DateTime(2026, 11, 13, 8, 49, 0);
            Assert.IsTrue(st.SmeUpozorenje("imap", 1, t0));
            st.ZapisiUpozorenje("imap", false, 1, t0);                 // 535 – nije poslato
            Assert.IsFalse(st.SmeUpozorenje("imap", 1, t0.AddMinutes(2)));
            Assert.IsTrue(st.SmeUpozorenje("imap", 1, t0.AddMinutes(11)), "ponovni pokušaj posle 10 min");
            st.ZapisiUpozorenje("imap", true, 1, t0.AddMinutes(11));
            Assert.IsFalse(st.SmeUpozorenje("imap", 1, t0.AddMinutes(50)));
            Assert.IsTrue(st.SmeUpozorenje("imap", 1, t0.AddMinutes(72)), "posle sat vremena ponovo");
        }
    }
}
