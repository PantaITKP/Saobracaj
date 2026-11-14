using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saobracaj.Agent.CimProvera.Baza;
using Saobracaj.Agent.CimProvera.Citanje;
using Saobracaj.Agent.CimProvera.Model;
using Saobracaj.Agent.CimProvera.Pravila;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>
    /// v2.9.0: integracioni testovi upisa nad pravom bazom, u transakciji koja se na kraju UVEK vraća (rollback) – u bazi ne ostaje ništa.
    /// Konekcija iz CIMPROVERA_KONEKCIJA_TEST (ili CIMPROVERA_KONEKCIJA); bez nje testovi su Inconclusive.
    /// </summary>
    [TestClass]
    public class UpisUBazuIntegracijaTests
    {
        private static PravilaSkup pravila;

        [ClassInitialize]
        public static void Init(TestContext _)
        {
            var put = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pravila", "cim_validation_rules.json");
            pravila = PravilaSkup.IzTeksta(File.ReadAllText(put), put);
        }

        private static string Konekcija()
        {
            var k = Environment.GetEnvironmentVariable("CIMPROVERA_KONEKCIJA_TEST");
            if (string.IsNullOrEmpty(k)) k = Environment.GetEnvironmentVariable("CIMPROVERA_KONEKCIJA");
            if (string.IsNullOrEmpty(k)) Assert.Inconclusive("Nema CIMPROVERA_KONEKCIJA_TEST – integracioni test upisa se ne izvršava.");
            return k;
        }

        private static object Skalar(SqlTransaction tx, string sql, params object[] arg)
        {
            using (var cmd = new SqlCommand(sql, tx.Connection, tx))
            {
                for (int i = 0; i + 1 < arg.Length; i += 2) cmd.Parameters.AddWithValue((string)arg[i], arg[i + 1]);
                var v = cmd.ExecuteScalar();
                return v is DBNull ? null : v;
            }
        }

        private static void URollbacku(Action<SqlTransaction, UpisUBazu> telo)
        {
            using (var c = UpisUBazu.Otvori(Konekcija()))
            {
                var tx = c.BeginTransaction();
                try
                {
                    var u = new UpisUBazu(null, null, pravila.Kp);
                    telo(tx, u);
                }
                finally { tx.Rollback(); }
            }
        }

        private static (string, byte[]) Fajl(string ime) => (ime, System.Text.Encoding.UTF8.GetBytes(ime + " " + Guid.NewGuid()));

        [TestMethod]
        public void Spisak_pre_tovarnog_lista_pa_naknadno_povezivanje_i_nova_verzija()
        {
            var voz = new Random().Next(70000, 99999).ToString();
            var broj = new Random().Next(10000, 99999) + "-0";
            URollbacku((tx, u) =>
            {
                // 1. mail: samo spisak kola (CIT 23) – teretnica bez posla
                var s = new SpisakKola { BrojVoza = voz, NhmZaglavlje = "100100" };
                // nasumični brojevi kola (93xx = U): test ne sme da se poveže sa pravim teretnicama u tmp tabelama
                var rnd = new Random();
                var k1 = "3387939" + rnd.Next(10000, 99999); var k2 = "3387939" + rnd.Next(10000, 99999);
                s.Kola.Add(new KolaNaSpisku { Broj = k1, TaraKg = 19990, NetoKg = 59757, Osovine = 4 });
                s.Kola.Add(new KolaNaSpisku { Broj = k2, TaraKg = 19080, NetoKg = 60645, Osovine = 4 });
                var f1 = Fajl("CIT-23 voz " + voz + ".xlsx");
                var r1 = new RezultatProvere();
                r1.Dokumenti.Add(new Dokument { Tip = TipDokumenta.SpisakKola, Fajl = f1.Item1, Spisak = { s }, Strane = { 1 }, Izvor = "excel" });
                var i1 = u.Upisi(new MailZaUpis { MessageId = "test-1@" + voz, Naslov = "CIT23 za voz " + voz, Posiljalac = "test@primer.rs", Prilozi = { f1 }, Provera = r1, Sanduce = "test" }, tx);
                Assert.AreEqual(1, i1.Teretnica);
                var tid = Convert.ToInt32(Skalar(tx, "SELECT ID FROM planiranje.tmpTeretnica WHERE KljucSpiska LIKE @k", "@k", "voz:" + voz + "#%"));
                Assert.IsNull(Skalar(tx, "SELECT JobID FROM planiranje.tmpTeretnica WHERE ID = @t", "@t", tid), "spisak pre tovarnog lista – JobID = NULL");
                Assert.AreEqual(2, Skalar(tx, "SELECT COUNT(*) FROM planiranje.tmpTeretnicaStavke WHERE BrojTeretnice = @t", "@t", tid));
                Assert.AreEqual(BrojKolaUic.ZaTeretnicu(k1), Skalar(tx, "SELECT BrojKola FROM planiranje.tmpTeretnicaStavke WHERE BrojTeretnice = @t AND RB = 1", "@t", tid));
                Assert.AreEqual("U", Skalar(tx, "SELECT Serija FROM planiranje.tmpTeretnicaStavke WHERE BrojTeretnice = @t AND RB = 1", "@t", tid));

                // 2. mail: tovarni list istog voza – nov posao, teretnica se naknadno povezuje, posao dobija podatke sa spiska
                var c = new CimList
                {
                    R1 = new Model.Strana { Status = StatusRubrike.Popunjeno, Naziv = "Test pošiljalac doo" },
                    R4 = new Model.Strana { Status = StatusRubrike.Popunjeno, Naziv = "Test primalac srl" },
                    R8 = new TekstRubrika { Status = StatusRubrike.Popunjeno, Tekst = "Train no: " + voz + "F" },
                    R16 = new Preuzimanje { Status = StatusRubrike.Popunjeno, Mesto = "Pančevo Glavna", Tekst = "Pančevo Glavna" },
                    R10 = new MestoIsporuke { Status = StatusRubrike.Popunjeno, Stanica = "Portogruaro Caorle", SifraStanice = "83 03200-3" },
                    R13 = new ListaRubrika { Status = StatusRubrike.Popunjeno, Tekst = "13.3 3212 KP, 7853 GPRAIL" },
                    R57 = new PrevozniciRubrika { Status = StatusRubrike.Popunjeno, Tekst = "3212 KP PANČEVO GLAVNA-ŠID\n7853 ŠID-DOBOVA" },
                    R21 = new NaznacenjeRobe { Status = StatusRubrike.Popunjeno, Tekst = "Pšenica, 26 vagona" },
                    R24 = new ListaRubrika { Status = StatusRubrike.Popunjeno, Stavke = { new SifraTekst { Sifra = "100100" } } },
                    R62 = new Identifikacija { Status = StatusRubrike.Popunjeno, Zemlja = "72", Stanica = "22001", Preduzece = "3212", Broj = broj },
                    R7 = new ListaRubrika { Status = StatusRubrike.Popunjeno, Tekst = "7.16 Frachtzahler auf KP ist EurologSystem doo Beograd" },
                };
                var f2 = Fajl("cim " + voz + ".pdf");
                var r2 = new RezultatProvere();
                var d2 = new Dokument { Tip = TipDokumenta.Cim, Fajl = f2.Item1, Cim = { c }, Strane = { 1 } };
                r2.Dokumenti.Add(d2); r2.Listovi.Add(new RezultatLista { Cim = c, Dokument = d2, Oznaka = "CIM list 1" });
                var i2 = u.Upisi(new MailZaUpis { MessageId = "test-2@" + voz, Naslov = "CIM", Posiljalac = "test@primer.rs", Telo = "Poštovani,\nšaljemo CIM.\nPozdrav", Prilozi = { f2 }, Provera = r2, Sanduce = "test" }, tx);
                Assert.AreEqual(1, i2.NovihPoslova); Assert.AreEqual(1, i2.Povezano, i2.ToString());
                var jid = Convert.ToInt32(Skalar(tx, "SELECT JobID FROM planiranje.tmpTeretnica WHERE ID = @t", "@t", tid));
                Assert.AreEqual(1857, Skalar(tx, "SELECT PlatilacID FROM planiranje.tmpJobs WHERE ID = @j", "@j", jid));
                Assert.AreEqual(3440, Skalar(tx, "SELECT PredajeOperateruID FROM planiranje.tmpJobs WHERE ID = @j", "@j", jid));
                Assert.AreEqual(381, Skalar(tx, "SELECT Stanica3ID FROM planiranje.tmpJobs WHERE ID = @j", "@j", jid));
                Assert.AreEqual(2, Skalar(tx, "SELECT BrojKola1 FROM planiranje.tmpJobs WHERE ID = @j", "@j", jid), "spisak ima prednost nad r21");
                Assert.AreEqual(159.472m, Skalar(tx, "SELECT Bruto1 FROM planiranje.tmpJobs WHERE ID = @j", "@j", jid));
                Assert.AreEqual(381, Skalar(tx, "SELECT StanicaDo FROM planiranje.tmpTeretnica WHERE ID = @t", "@t", tid));
                Assert.AreEqual(1009, Skalar(tx, "SELECT TOP 1 Uvozna FROM planiranje.tmpTeretnicaStavke WHERE BrojTeretnice = @t", "@t", tid), "Uvozna = krajnja uputna");
                Assert.AreEqual(1, Skalar(tx, "SELECT COUNT(*) FROM planiranje.tmpJobsIstorija WHERE JobID = @j AND Razlog = 'NOVI_POSAO'", "@j", jid));
                Assert.IsNotNull(Skalar(tx, "SELECT TOP 1 ID FROM planiranje.tmpJobsIstorija WHERE JobID = @j AND Kolona = 'BrojKola1' AND Napomena LIKE 'Broj kola u tovarnom listu (26)%'", "@j", jid), "razlika 26/2 u istoriji");
                StringAssert.Contains((string)Skalar(tx, "SELECT Napomena FROM planiranje.tmpJobs WHERE ID = @j", "@j", jid), "šaljemo CIM");

                // 3. mail: nova verzija lista (promenjena samo r14) – Verzija 2, istorija samo za BrojPonude, kolone spiska ostaju
                c.R14 = new TekstRubrika { Status = StatusRubrike.Popunjeno, Tekst = "KP2026" };
                var f3 = Fajl("cim " + voz + " v2.pdf");
                var d3 = new Dokument { Tip = TipDokumenta.Cim, Fajl = f3.Item1, Cim = { c }, Strane = { 1 } };
                var r3 = new RezultatProvere(); r3.Dokumenti.Add(d3); r3.Listovi.Add(new RezultatLista { Cim = c, Dokument = d3, Oznaka = "CIM list 1" });
                var i3 = u.Upisi(new MailZaUpis { MessageId = "test-3@" + voz, Naslov = "CIM ispravka", Posiljalac = "test@primer.rs", Prilozi = { f3 }, Provera = r3, Sanduce = "test" }, tx);
                Assert.AreEqual(1, i3.IzmenjenihPoslova);
                Assert.AreEqual(2, Skalar(tx, "SELECT Verzija FROM planiranje.tmpJobs WHERE ID = @j", "@j", jid));
                Assert.AreEqual(1, Skalar(tx, "SELECT COUNT(*) FROM planiranje.tmpJobsIstorija WHERE JobID = @j AND Razlog = 'NOVA_VERZIJA_CIM'", "@j", jid));
                Assert.AreEqual("BrojPonude", Skalar(tx, "SELECT Kolona FROM planiranje.tmpJobsIstorija WHERE JobID = @j AND Razlog = 'NOVA_VERZIJA_CIM'", "@j", jid));
                Assert.AreEqual(2, Skalar(tx, "SELECT BrojKola1 FROM planiranje.tmpJobs WHERE ID = @j", "@j", jid), "kolone spiska se ne vraćaju na r21");

                // 4. isti list, isti sadržaj – ništa se ne menja
                var i4 = u.Upisi(new MailZaUpis { MessageId = "test-4@" + voz, Naslov = "CIM ponovo", Posiljalac = "test@primer.rs", Prilozi = { f3 }, Provera = r3, Sanduce = "test" }, tx);
                Assert.AreEqual(1, i4.IstihPoslova); Assert.AreEqual(2, Skalar(tx, "SELECT Verzija FROM planiranje.tmpJobs WHERE ID = @j", "@j", jid));
                Assert.AreEqual(2, Skalar(tx, "SELECT BrojPrijema FROM planiranje.tmpDokument WHERE ImeFajla = @f", "@f", f3.Item1), "isti fajl – isti red tmpDokument");

                // ručno izmenjeno polje se ne menja – razlika ide u istoriju
                using (var cmd = new SqlCommand("UPDATE planiranje.tmpJobsPolje SET RucnoIzmenjeno = 1 WHERE JobID = @j AND TeretnicaID IS NULL AND Kolona = 'BrojPonude'", tx.Connection, tx)) { cmd.Parameters.AddWithValue("@j", jid); cmd.ExecuteNonQuery(); }
                c.R14 = new TekstRubrika { Status = StatusRubrike.Popunjeno, Tekst = "KP2027" };
                u.Upisi(new MailZaUpis { MessageId = "test-5@" + voz, Naslov = "CIM 3", Posiljalac = "test@primer.rs", Prilozi = { Fajl("cim v3.pdf") }, Provera = r3, Sanduce = "test" }, tx);
                Assert.AreEqual("KP2026", Skalar(tx, "SELECT BrojPonude FROM planiranje.tmpJobs WHERE ID = @j", "@j", jid));
                Assert.IsNotNull(Skalar(tx, "SELECT TOP 1 ID FROM planiranje.tmpJobsIstorija WHERE JobID = @j AND Napomena = N'ručno izmenjeno – nije menjano'", "@j", jid));
            });
        }

        // ------------------------------------------------------------------------------------------- voz 256 (tačka 7a)

        private const string Pravi = @"\\192.168.1.6\Cloude\Agenti\CimProvera\testovi\nezavisni\2026-11-14-pravi\prilozi";

        private static List<(string, byte[])> Prilozi(string folder)
        {
            var d = Path.Combine(Pravi, folder);
            if (!Directory.Exists(d)) Assert.Inconclusive("Nema testnih dokumenata: " + d);
            return Directory.GetFiles(d).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Select(f => (Path.GetFileName(f), File.ReadAllBytes(f))).ToList();
        }

        /// <summary>Tačan test iz tačke 7a: posao i teretnica voza 256F Pančevo – Portogruaro Caorle iz mailova „CIM“ i „CIT23 za voz 256 …“.</summary>
        [TestMethod]
        public void Voz256_posao_i_teretnica()
        {
            var k = Konekcija();
            var cim = Prilozi("CIM");
            var cit = Prilozi("CIT23 za voz 256 Pančevo Glavna (Pančevo Varoš)-Portogruaro Caorle");
            var cfg = RegresijaTests.Cfg(Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje256"));
            Ishod i1, i2;
            using (var c = new LokalniCitac(cfg, null, pravila.Kp))
            {
                var o = new Obrada(pravila, c, null, cfg.PragPouzdanosti);
                i1 = o.Proveri(cim, "branislav.kolarov@pansped.rs", false, new DateTime(2026, 11, 14, 8, 20, 0), false);
                i2 = o.Proveri(cit, "sara.markovic@eurologsystem.rs", false, new DateTime(2026, 11, 14, 9, 13, 0), false);
            }
            URollbacku((tx, u) =>
            {
                u.Upisi(new MailZaUpis { MessageId = "test-256-cim", Naslov = "CIM", Posiljalac = "branislav.kolarov@pansped.rs", Primljeno = new DateTime(2026, 11, 14, 8, 20, 0), Prilozi = cim, Provera = i1.Provera, Sanduce = "test" }, tx);
                var ish = u.Upisi(new MailZaUpis { MessageId = "test-256-cit", Naslov = "CIT23 za voz 256 Pančevo Glavna (Pančevo Varoš)-Portogruaro Caorle", Posiljalac = "sara.markovic@eurologsystem.rs", Primljeno = new DateTime(2026, 11, 14, 9, 13, 0), Prilozi = cit, Provera = i2.Provera, Sanduce = "test" }, tx);
                var jid = Convert.ToInt32(Skalar(tx, "SELECT TOP 1 ID FROM planiranje.tmpJobs WHERE Referenca LIKE '%256F%' ORDER BY ID DESC"));
                object J(string kol) => Skalar(tx, "SELECT " + kol + " FROM planiranje.tmpJobs WHERE ID = @j", "@j", jid);
                Assert.AreEqual(1857, J("PlatilacID"), "EurologSystem doo Beograd");
                Assert.AreEqual(246, J("OtpravnaID"), "Pančevo Glavna");
                Assert.AreEqual(1009, J("UputnaID"), "Portogruaro Caorle");
                Assert.AreEqual(3440, J("PredajeOperateruID"), "GP Rail (7853)");
                Assert.AreEqual(246, J("Stanica1ID"));
                Assert.AreEqual(381, J("Stanica3ID"), "Šid gr.");
                Assert.AreEqual(2234.533m, J("Bruto1"));
                Assert.AreEqual(1671.863m, J("Neto1"));
                Assert.AreEqual(28, J("BrojKola1"), "najnoviji spisak kola");
                Assert.AreEqual(8, J("NHM1"), "NHM 1001");
                Assert.AreEqual("Ua", J("SerijaKola1"));
                Assert.AreEqual(0m, J("Duzina1"));
                Assert.IsNotNull(J("PreuzimaOdID"), "polazak iz Srbije → pošiljalac (CHS Serbia), " + Skalar(tx, "SELECT Kandidati FROM planiranje.tmpJobsPolje WHERE JobID = @j AND Kolona = 'PreuzimaOdID'", "@j", jid));
                var tid = Convert.ToInt32(Skalar(tx, "SELECT TOP 1 ID FROM planiranje.tmpTeretnica WHERE JobID = @j ORDER BY ID DESC", "@j", jid));
                Assert.AreEqual(28, Skalar(tx, "SELECT COUNT(*) FROM planiranje.tmpTeretnicaStavke WHERE BrojTeretnice = @t", "@t", tid), ish.ToString());
                Assert.AreEqual(28, Skalar(tx, "SELECT COUNT(*) FROM planiranje.tmpTeretnicaStavke WHERE BrojTeretnice = @t AND BrojOsovina = 4 AND VRNP = '1001' AND Tara > 0 AND Neto > 0", "@t", tid));
                Assert.AreEqual(1671.863m, Skalar(tx, "SELECT SUM(Neto) FROM planiranje.tmpTeretnicaStavke WHERE BrojTeretnice = @t", "@t", tid));
                Assert.AreEqual(19.990m, Skalar(tx, "SELECT Tara FROM planiranje.tmpTeretnicaStavke WHERE BrojTeretnice = @t AND BrojKola = '33839338201-9'", "@t", tid));
            });
        }
    }
}
