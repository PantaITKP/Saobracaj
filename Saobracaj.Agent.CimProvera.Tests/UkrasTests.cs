using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Prilozi;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>Logo firme, slike iz potpisa i ikone se ne čitaju i ne vode na ručnu proveru; sken/fotografija dokumenta se čita.</summary>
    [TestClass]
    public class UkrasTests
    {
        /// <summary>PNG sa šumom (da ne bude premali posle kompresije).</summary>
        private static byte[] Slika(int w, int h, ImageFormat f = null)
        {
            var rnd = new Random(w * 31 + h);
            using (var b = new Bitmap(w, h))
            {
                for (int y = 0; y < h; y += 2)
                    for (int x = 0; x < w; x += 2)
                        b.SetPixel(x, y, Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256)));
                using (var ms = new MemoryStream()) { b.Save(ms, f ?? ImageFormat.Png); return ms.ToArray(); }
            }
        }

        private static Prilog P(string ime, byte[] b) => Raspakivanje.Raspakuj(ime, b, new System.Collections.Generic.List<string>()).Single();

        [TestMethod]
        public void Mala_slika_iz_potpisa_se_preskace()
        {
            StringAssert.StartsWith(Raspakivanje.Ukras(P("image001.png", Slika(200, 80))), "mala slika");
            StringAssert.StartsWith(Raspakivanje.Ukras(P("image002.png", Slika(480, 480))), "mala slika");
        }

        [TestMethod]
        public void Baner_se_preskace()
        {
            StringAssert.StartsWith(Raspakivanje.Ukras(P("image003.png", Slika(1500, 300))), "traka/baner");
        }

        [TestMethod]
        public void Logo_po_imenu_se_preskace_a_ista_slika_drugog_imena_se_cita()
        {
            var b = Slika(1000, 800);
            StringAssert.StartsWith(Raspakivanje.Ukras(P("Logo_KP.png", b)), "logo/ikona po imenu");
            StringAssert.StartsWith(Raspakivanje.Ukras(P("linkedin.png", b)), "logo/ikona po imenu");
            Assert.IsNull(Raspakivanje.Ukras(P("CIM_GK30.png", b)));
        }

        [TestMethod]
        public void Velika_slika_moze_biti_tovarni_list_i_cita_se()
        {
            // fotografija/sken sa telefona, i kada se zove kao slika iz Outlook-a
            Assert.IsNull(Raspakivanje.Ukras(P("image001.jpg", Slika(1600, 1200, ImageFormat.Jpeg))));
            Assert.IsNull(Raspakivanje.Ukras(P("IMG_2034.png", Slika(1240, 1754))));
            Assert.IsNull(Raspakivanje.Ukras(P("CIM.pdf", new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F', 1, 2 })), "samo slike");
        }

        [TestMethod]
        public void Mail_samo_sa_slikama_iz_potpisa_nema_rucne_provere()
        {
            var cfg = RegresijaTests.Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje"));
            var pravila = PravilaSkup.Ucitaj(cfg.PravilaFolder, cfg.PravilaFajl, cfg.StanjeFolder, null);
            using (var citac = new LokalniCitac(cfg, null, pravila.Kp))
            {
                var i = new Obrada(pravila, citac, null, 0.6).Proveri(new[] { ("image001.png", Slika(220, 90)), ("image002.png", Slika(1400, 280)), ("image003.jpg", Slika(64, 64, ImageFormat.Jpeg)) },
                    "disp@kprevoz.co.rs", true, new DateTime(2026, 11, 13), false);
                Assert.IsFalse(i.Provera.ImaTovarniList);
                Assert.AreEqual(0, i.Neprepoznati.Count, "slike iz potpisa nisu razlog za ručnu proveru");
                Assert.AreEqual(0, i.NisuProcitani.Count);
                Assert.AreEqual(3, i.OpisPriloga.Count(x => x.EndsWith("slika iz potpisa/logo (ne proverava se)")), string.Join(" | ", i.OpisPriloga));
            }
        }
    }
}
