using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MimeKit;
using Saobracaj.Agent.CimProvera.Mail;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;
using Saobracaj.Agent.CimProvera.Radionica;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>v3.0.5 – Test režim na serveru: prekidač pri svakom slanju, nulta tačka iz arhive, dnevni sažetak, paket Testiranje.</summary>
    [TestClass]
    public class V305Tests
    {
        private static string Temp(string ime)
        {
            var d = Path.Combine(Path.GetTempPath(), "CimProvera305", ime + "_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(d);
            return d;
        }

        private static MimeMessage Poruka(params string[] za)
        {
            var m = new MimeMessage { Subject = "Tovarni list CIM 123 – napomene" };
            m.From.Add(MailboxAddress.Parse(Slanje.TestnaAdresa));
            foreach (var a in za) m.To.Add(MailboxAddress.Parse(a));
            m.Body = new TextPart("plain") { Text = "x" };
            return m;
        }

        /// <summary>Slanje kroz pravi Slanje.Posalji sa prekidačem (bez SMTP-a): šta je poslato, šta je blokirano, šta je u logu.</summary>
        private sealed class Probe : IDisposable
        {
            public readonly List<MimeMessage> Poslato = new List<MimeMessage>();
            public readonly List<string> Zapis = new List<string>();
            public readonly List<(bool ok, string razlog)> Ishodi = new List<(bool, string)>();
            public readonly string Folder = Temp("agent");
            public readonly string Stanje;
            private readonly Action<AgentConfig, MimeMessage> t = Slanje.Transport;
            private readonly Action<string> z = Slanje.ZapisZastite;
            private readonly bool sp = Slanje.SamoPriprema;
            private readonly Func<string> fa = Prekidac.FolderAgenta;
            private readonly Action<MimeMessage, bool, string> zs = Slanje.ZapisSlanja;
            private readonly Log dn = Slanje.Dnevnik;

            public Probe()
            {
                Stanje = Path.Combine(Folder, "stanje");
                Slanje.Transport = (c, m) => Poslato.Add(m);
                Slanje.ZapisZastite = Zapis.Add;
                Slanje.SamoPriprema = false;
                Slanje.Dnevnik = null;
                Prekidac.FolderAgenta = () => Folder;
                Slanje.ZapisSlanja = (m, ok, r) => Ishodi.Add((ok, r));
            }

            public AgentConfig Cfg(string rezim, Func<string> sada = null, string odobreni = null)
            {
                var nv = new NameValueCollection { ["TestPrimalac"] = Slanje.TestnaAdresa, ["InterniDomeni"] = "kprevoz.co.rs" };
                if (rezim != null) nv["Rezim"] = rezim;
                if (odobreni != null) nv["OdobreniPosiljaoci"] = odobreni;
                var c = AgentConfig.Ucitaj(nv);
                c.StanjeFolder = Stanje;
                c.RezimSada = sada;
                return c;
            }

            public void Dispose()
            {
                Slanje.Transport = t; Slanje.ZapisZastite = z; Slanje.SamoPriprema = sp; Prekidac.FolderAgenta = fa; Slanje.ZapisSlanja = zs; Slanje.Dnevnik = dn;
            }
        }

        [TestMethod]
        public void Prekidac_Test_salje_iskljucivo_na_agenti()
        {
            using (var p = new Probe())
            {
                var cfg = p.Cfg("Test");
                var m = Poruka("dispo@crw.sk", "priprema@kprevoz.co.rs");
                m.Cc.Add(MailboxAddress.Parse("neko@gmail.com"));
                Assert.IsTrue(Slanje.Posalji(cfg, m));
                var svi = p.Poslato.Single().To.Mailboxes.Concat(p.Poslato[0].Cc.Mailboxes).Concat(p.Poslato[0].Bcc.Mailboxes).Select(x => x.Address.ToLowerInvariant()).ToList();
                CollectionAssert.AreEqual(new[] { Slanje.TestnaAdresa }, svi);
                Assert.IsTrue(p.Ishodi.Single().ok);
                // config na disku se promeni u Live dok proces radi u Test-u → i dalje Test (samo agenti@)
                var cfg2 = p.Cfg("Test", () => "Live", "crw.sk");
                Assert.IsTrue(Slanje.Posalji(cfg2, Poruka("dispo@crw.sk")));
                CollectionAssert.AreEqual(new[] { Slanje.TestnaAdresa }, p.Poslato[1].To.Mailboxes.Select(x => x.Address).ToList());
            }
        }

        [TestMethod]
        public void Prekidac_rezim_koji_nije_Test_ni_Live_ne_salje_nista_INTERNO()
        {
            foreach (var (rezim, sadrzi) in new[] { ("Tset", "„Tset“ nije ni Test ni Live"), ("", "prazan"), ("live ", null), ("Produkcija", "nije ni Test ni Live") })
                using (var p = new Probe())
                {
                    // "live " bez spiska odobrenih pošiljalaca – takođe blokirano
                    var cfg = p.Cfg(rezim);
                    var m = Poruka("dispo@crw.sk");
                    Assert.IsFalse(Slanje.Posalji(cfg, m), rezim);
                    Assert.AreEqual(0, p.Poslato.Count, rezim);
                    var (ok, razlog) = p.Ishodi.Single();
                    Assert.IsFalse(ok);
                    StringAssert.Contains(razlog, sadrzi ?? "Live bez odobrenog spiska");
                    Assert.IsTrue(p.Zapis.Any(x => x.StartsWith("INTERNO: slanje blokirano")), rezim);
                    // poruka je sačuvana za pregled
                    Assert.AreEqual(1, Directory.GetFiles(Path.Combine(p.Stanje, "neposlato"), "*.eml").Length, rezim);
                    Assert.IsNotNull(Prekidac.RazlogBlokade(cfg), rezim);
                }
        }

        [TestMethod]
        public void Prekidac_proverava_config_pri_svakom_slanju_i_hitno_iskljucivanje()
        {
            using (var p = new Probe())
            {
                var rezim = "Live";
                var cfg = p.Cfg("Live", () => rezim, "crw.sk, pansped.rs");
                Assert.IsTrue(Slanje.Posalji(cfg, Poruka("dispo@crw.sk")));
                rezim = "Tset";   // izmena config-a dok agent radi – važi od sledećeg slanja
                Assert.IsFalse(Slanje.Posalji(cfg, Poruka("dispo@crw.sk")));
                rezim = "Live";
                File.WriteAllText(Path.Combine(p.Folder, Prekidac.ZaustavnoIme), "hitno");
                Assert.IsFalse(Slanje.Posalji(cfg, Poruka("dispo@crw.sk")));
                StringAssert.Contains(p.Ishodi.Last().razlog, "hitno isključivanje");
                StringAssert.Contains(Prekidac.RazlogBlokade(cfg), Prekidac.ZaustavnoIme);
                Assert.AreEqual(1, p.Poslato.Count);
                // i u Test režimu fajl hitnog isključivanja zaustavlja i poruku na agenti@
                Assert.IsFalse(Slanje.Posalji(p.Cfg("Test"), Poruka(Slanje.TestnaAdresa)));
                Assert.AreEqual(1, p.Poslato.Count);
            }
        }

        [TestMethod]
        public void Prekidac_Live_samo_odobreni_spisak_i_interni_domen()
        {
            using (var p = new Probe())
            {
                var cfg = p.Cfg("Live", null, "dispo@crw.sk; @pansped.rs");
                Assert.AreEqual(Rezim.Live, cfg.Rezim);
                var m = Poruka("dispo@crw.sk", "neko@crw.sk", "branislav@pansped.rs", "priprema@kprevoz.co.rs", "x@gmail.com");
                Assert.IsTrue(Slanje.Posalji(cfg, m));
                CollectionAssert.AreEquivalent(new[] { "dispo@crw.sk", "branislav@pansped.rs", "priprema@kprevoz.co.rs" }, p.Poslato.Single().To.Mailboxes.Select(x => x.Address).ToList());
                CollectionAssert.AreEquivalent(new[] { "LIVE: uklonjen primalac van odobrenog spiska neko@crw.sk", "LIVE: uklonjen primalac van odobrenog spiska x@gmail.com" }, p.Zapis);
                // nijedan dozvoljen primalac → ništa
                Assert.IsFalse(Slanje.Posalji(cfg, Poruka("x@gmail.com")));
                Assert.AreEqual(1, p.Poslato.Count);
                // Live bez spiska → ne šalje se ništa
                Assert.IsFalse(Slanje.Posalji(p.Cfg("Live"), Poruka("priprema@kprevoz.co.rs")));
            }
            Assert.IsTrue(Prekidac.NaSpisku("<Dispo@CRW.sk>", new[] { "dispo@crw.sk" }));
            Assert.IsTrue(Prekidac.NaSpisku("a@crw.sk", new[] { "crw.sk" }));
            Assert.IsFalse(Prekidac.NaSpisku("a@sub.crw.sk", new[] { "@crw.sk" }));
            Assert.IsFalse(Prekidac.NaSpisku("a@crw.sk", new[] { "b@crw.sk" }));
        }

        [TestMethod]
        public void Prekidac_ne_vazi_za_CimRadionicu_koja_nista_ne_salje()
        {
            using (var p = new Probe())
            {
                Slanje.SamoPriprema = true;
                var cfg = p.Cfg("Live");   // CimRadionica priprema kao Live, bez spiska – transport je prazan
                Assert.IsTrue(Slanje.Posalji(cfg, Poruka("dispo@crw.sk")));
                Assert.AreEqual("dispo@crw.sk", p.Poslato.Single().To.Mailboxes.Single().Address);
            }
        }

        // ------------------------------------------------------------------------------------------- nulta tačka iz arhive

        private const string Izvestaj19 = "Arhiva D:\\Cloude\\Agenti\\CimProvera\\arhiva – 2026-11-19 15:10 +01:00\r\nNovih: 0, već u arhivi: 709\r\n"
            + "INBOX: 709 poruka, 839.7 MB (UID 191–899); sopstveni odgovori agenta: 109, 80.7 MB\r\nSanduče (samo čitanje, bez slanja):\r\n"
            + "  INBOX (UIDVALIDITY 1794489150): na serveru 899 poruka, 1043.8 MB, UID 1–899; prilozi PRIMER: 98.0 MB u 80 poruka; za arhivu (od početne tačke): 709, poslednji UID 899; rupe (nema u arhivi): nema\r\n"
            + "  INBOX.Sent (UIDVALIDITY 1794489155): na serveru 6 poruka, 43.6 MB, UID 1–6; prilozi PRIMER: 0.0 MB u 0 poruka; za arhivu (od početne tačke): 0, poslednji UID –; rupe (nema u arhivi): nema\r\n";

        private static List<ZapisArhive> Indeks(long od, long @do) =>
            Enumerable.Range((int)od, (int)(@do - od + 1)).Select(u => new ZapisArhive { Folder = "INBOX", Uid = u, UidValidity = 1794489150 }).ToList();

        [TestMethod]
        public void Nulta_tacka_poslednji_UID_pokriven_arhivom()
        {
            var sada = new DateTimeOffset(2026, 11, 19, 15, 25, 0, TimeSpan.FromHours(1));
            var r = NultaTackaArhive.Izracunaj(Izvestaj19, Indeks(191, 899), sada, TimeSpan.FromMinutes(30));
            Assert.IsTrue(r.Ispravan, string.Join("; ", r.Greske));
            Assert.AreEqual(899, r.Uid["INBOX"]);
            Assert.AreEqual(6, r.Uid["INBOX.Sent"]);
            // stanje servera 20.11.: INBOX poslednji obrađen 846 → 899, Sent 6 ostaje 6
            var s = new Stanje();
            s.Folder(Stanje.KljucSanduceta("agenti@kprevoz.co.rs", "INBOX")).UidValidity = 1794489150;
            s.Folder(Stanje.KljucSanduceta("agenti@kprevoz.co.rs", "INBOX")).PoslednjiUid = 846;
            s.Folder(Stanje.KljucSanduceta("agenti@kprevoz.co.rs", "INBOX.Sent")).UidValidity = 1794489155;
            s.Folder(Stanje.KljucSanduceta("agenti@kprevoz.co.rs", "INBOX.Sent")).PoslednjiUid = 6;
            var opis = NultaTackaArhive.Primeni(s, "agenti@kprevoz.co.rs", r);
            Assert.AreEqual(899, s.Folder("agenti@kprevoz.co.rs|INBOX").PoslednjiUid);
            Assert.AreEqual(6, s.Folder("agenti@kprevoz.co.rs|INBOX.Sent").PoslednjiUid);
            Assert.AreEqual(899, s.NultaTackaUid);
            Assert.AreEqual(1794489150, s.NultaTackaUidValidity);
            Assert.AreEqual(r.VremeArhive, s.NultaTacka);
            StringAssert.Contains(opis[0], "846 → 899");
            // agent je već bio dalje od arhive (ne bi trebalo, ali se poziciji ne vraća unazad)
            s.Folder("agenti@kprevoz.co.rs|INBOX").PoslednjiUid = 905;
            NultaTackaArhive.Primeni(s, "agenti@kprevoz.co.rs", r);
            Assert.AreEqual(905, s.Folder("agenti@kprevoz.co.rs|INBOX").PoslednjiUid);
        }

        [TestMethod]
        public void Nulta_tacka_odbija_staru_arhivu_rupe_i_poruku_koja_nije_u_arhivi()
        {
            var sada = new DateTimeOffset(2026, 11, 19, 15, 25, 0, TimeSpan.FromHours(1));
            var stara = NultaTackaArhive.Izracunaj(Izvestaj19, Indeks(191, 899), sada.AddHours(3), TimeSpan.FromMinutes(30));
            Assert.IsFalse(stara.Ispravan); StringAssert.Contains(stara.Greske[0], "stariji od 30 min");
            var rupe = NultaTackaArhive.Izracunaj(Izvestaj19.Replace("poslednji UID 899; rupe (nema u arhivi): nema", "poslednji UID 899; rupe (nema u arhivi): 2 – UID 512, 513"), Indeks(191, 899), sada, TimeSpan.FromMinutes(30));
            Assert.IsFalse(rupe.Ispravan); StringAssert.Contains(rupe.Greske[0], "rupe");
            var nedostaje = NultaTackaArhive.Izracunaj(Izvestaj19, Indeks(191, 898), sada, TimeSpan.FromMinutes(30));
            Assert.IsFalse(nedostaje.Ispravan); StringAssert.Contains(nedostaje.Greske[0], "u arhivi 898");
            var bezVremena = NultaTackaArhive.Izracunaj("nešto drugo", Indeks(191, 899), sada, TimeSpan.FromMinutes(30));
            Assert.IsFalse(bezVremena.Ispravan);
            // prazan folder poslatih: nulta tačka 0
            var prazan = NultaTackaArhive.Izracunaj(Izvestaj19.Replace("na serveru 6 poruka, 43.6 MB, UID 1–6;", "na serveru 0 poruka, 0.0 MB;"), Indeks(191, 899), sada, TimeSpan.FromMinutes(30));
            Assert.IsTrue(prazan.Ispravan, string.Join("; ", prazan.Greske)); Assert.AreEqual(0, prazan.Uid["INBOX.Sent"]);
        }

        [TestMethod]
        public void Nulta_tacka_i_arhiviranje_ne_inicijalizuju_SMTP()
        {
            Assert.IsTrue(Program.BezSlanja(new[] { "nulta-tacka-arhiva" }));
            Assert.IsTrue(Program.BezSlanja(new[] { "nulta-tacka-arhiva", "--suvo" }));
            Assert.IsFalse(Program.BezSlanja(new[] { "--once" }));
        }

        // ------------------------------------------------------------------------------------------- dnevni sažetak

        [TestMethod]
        public void Dnevni_sazetak_mailovi_odluke_izvestaji_pravila_upis()
        {
            var st = Temp("dnevnik");
            var dan = new DateTime(2026, 11, 21);
            var cim = new RezultatProvere();
            cim.Listovi.Add(new RezultatLista { Nalazi = {
                new Nalaz { Pravilo = "R16", Tezina = Tezina.Greska }, new Nalaz { Pravilo = "R29", Tezina = Tezina.Upozorenje },
                new Nalaz { Pravilo = "R62", Tezina = Tezina.Info, SamoInterno = true } } });
            var r1 = new RezultatObrade { Korak = Korak.Obradjena, Status = "Greske+RucnaProvera", Ishod = new Ishod { Provera = cim },
                Upis = new Baza.IshodUpisa { NovihPoslova = 1, Teretnica = 1, Stavki = 22 } };
            r1.Poruke.Add(new PripremljenaPoruka { Vrsta = "interni" });
            DnevniSazetak.ZapisiPoruku(st, dan.AddHours(9), new UlaznaPoruka { Folder = "INBOX", Uid = 912, Poruka = Poruka("x@y.rs") }, r1);
            DnevniSazetak.ZapisiPoruku(st, dan.AddHours(9.1), new UlaznaPoruka { Folder = "INBOX", Uid = 913, Poruka = Poruka("x@y.rs") }, new RezultatObrade { Korak = Korak.Gotova, Status = "Preskoceno" });
            DnevniSazetak.ZapisiPoruku(st, dan.AddHours(10), new UlaznaPoruka { Folder = "INBOX", Uid = 914, Poruka = Poruka("x@y.rs") }, new RezultatObrade { Korak = Korak.Gotova, Status = "NemaCim", GreskaUpisa = "timeout" });
            DnevniSazetak.ZapisiPoruku(st, dan.AddHours(11), new UlaznaPoruka { Folder = "INBOX", Uid = 915, Poruka = Poruka("x@y.rs") }, null, "IOException: disk");
            DnevniSazetak.ZapisiSlanje(st, dan.AddHours(9), Poruka(Slanje.TestnaAdresa), true, null);
            DnevniSazetak.ZapisiSlanje(st, dan.AddHours(12), Poruka("dispo@crw.sk"), false, "režim „Tset“ nije ni Test ni Live");
            var t = DnevniSazetak.Sastavi(st, dan, "Test", "SRV", "3.0.5");
            StringAssert.Contains(t, "21.11.2026.");
            StringAssert.Contains(t, "MAILOVI: 3 (dolaznih 3, odlaznih 0); sopstvene poruke agenta (preskočene): 1; pokušaja sa privremenom greškom: 1");
            StringAssert.Contains(t, "Greske+RucnaProvera: 1");
            StringAssert.Contains(t, "NemaCim: 1");
            StringAssert.Contains(t, "pripremljeno po vrsti: interni 1");
            StringAssert.Contains(t, "poslato: 1; blokirano prekidačem: 1");
            StringAssert.Contains(t, "režim „Tset“ nije ni Test ni Live: 1");
            StringAssert.Contains(t, "R16            1 / 0 / 0");
            StringAssert.Contains(t, "R29            0 / 1 / 0");
            StringAssert.Contains(t, "R62            0 / 0 / 1");
            StringAssert.Contains(t, "poslova novih 1, izmenjenih 0, istih 0; teretnica 1, stavki 22; grešaka upisa 1");
            StringAssert.Contains(t, "UID 914");
            StringAssert.Contains(t, "IOException: disk");
            // prazan dan
            StringAssert.Contains(DnevniSazetak.Sastavi(st, dan.AddDays(1), "Test", "SRV", "3.0.5"), "MAILOVI: 0");
        }

        [TestMethod]
        public void Dnevni_sazetak_jednom_dnevno_posle_sata_za_prethodne_dane()
        {
            var st = Temp("oznaka");
            // prvi put (uključivanje 20.11. u 10:00): ništa, oznaka = 19.11.
            Assert.AreEqual(0, DnevniSazetak.ZaSlanje(st, new DateTime(2026, 11, 20, 10, 0, 0), 7).Count);
            // 21.11. u 06:58 – još ne; u 07:02 – 20.11.
            Assert.AreEqual(0, DnevniSazetak.ZaSlanje(st, new DateTime(2026, 11, 21, 6, 58, 0), 7).Count);
            CollectionAssert.AreEqual(new[] { new DateTime(2026, 11, 20) }, DnevniSazetak.ZaSlanje(st, new DateTime(2026, 11, 21, 7, 2, 0), 7));
            DnevniSazetak.OznaciPoslato(st, new DateTime(2026, 11, 20));
            Assert.AreEqual(0, DnevniSazetak.ZaSlanje(st, new DateTime(2026, 11, 21, 9, 0, 0), 7).Count);
            // agent nije radio 3 dana: sažetak za svaki propušten dan
            Assert.AreEqual(3, DnevniSazetak.ZaSlanje(st, new DateTime(2026, 11, 24, 8, 0, 0), 7).Count);
            // isključen (-1)
            Assert.AreEqual(0, DnevniSazetak.ZaSlanje(st, new DateTime(2026, 11, 24, 8, 0, 0), -1).Count);
        }

        [TestMethod]
        public void Config_prekidac_kljucevi()
        {
            var c = AgentConfig.Ucitaj(new NameValueCollection { ["Rezim"] = " Test ", ["OdobreniPosiljaoci"] = "a@b.rs, @c.rs", ["DnevniSazetakSat"] = "8" });
            Assert.AreEqual("Test", c.RezimTekst); Assert.AreEqual(Rezim.Test, c.Rezim);
            CollectionAssert.AreEqual(new[] { "a@b.rs", "@c.rs" }, c.OdobreniPosiljaoci);
            Assert.AreEqual(8, c.DnevniSazetakSat);
            Assert.AreEqual("Test", AgentConfig.Ucitaj(new NameValueCollection()).RezimTekst);
            Assert.AreEqual(7, AgentConfig.Ucitaj(new NameValueCollection()).DnevniSazetakSat);
            Assert.AreEqual("Agent", AgentConfig.Ucitaj(new NameValueCollection()).IzvorUpisa);
        }

        // ------------------------------------------------------------------------------------------- paket Testiranje

        [TestMethod]
        public void Paket_za_Claude_samo_mailovi_ove_obrade_iz_foldera_Testiranje()
        {
            var koren = Temp("prostor");
            var testiranje = Temp("Testiranje");
            var rp = RadniProstor.Otvori(Path.Combine(koren, "Testiranje"));
            var baza = Path.Combine(testiranje, "_Analiza", "3.0.5");
            foreach (var m in new[] { "001_20261120-0900_A", "002_20261120-0910_B", "003_20261120-0920_C" })
            {
                Directory.CreateDirectory(Path.Combine(baza, m));
                File.WriteAllText(Path.Combine(baza, m, "ANALIZA.md"), "# " + m);
            }
            File.WriteAllText(Path.Combine(baza, "INDEKS.md"), "indeks");
            var izlaz = Path.Combine(testiranje, "_Paket", "Claude");
            var z = Zbirno.PaketZaClaude(rp, "3.0.5", izlaz, null, out _, analizaKoren: testiranje, osnovaImena: "Claude_Testiranje_2026-11-20",
                ukljuciMail: ime => ime.StartsWith("002_") || ime.StartsWith("003_"));
            Assert.AreEqual(Path.Combine(izlaz, "Claude_Testiranje_2026-11-20.zip"), z.Single());
            using (var a = ZipFile.OpenRead(z[0]))
            {
                var imena = a.Entries.Select(e => e.FullName).ToList();
                Assert.IsTrue(imena.Contains("_Analiza/3.0.5/INDEKS.md"));
                Assert.IsTrue(imena.Any(x => x.StartsWith("_Analiza/3.0.5/002_")));
                Assert.IsTrue(imena.Any(x => x.StartsWith("_Analiza/3.0.5/003_")));
                Assert.IsFalse(imena.Any(x => x.StartsWith("_Analiza/3.0.5/001_")), string.Join(", ", imena));
            }
        }
    }
}
