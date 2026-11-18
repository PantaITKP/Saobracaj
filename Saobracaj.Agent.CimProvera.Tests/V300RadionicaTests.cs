using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MimeKit;
using Newtonsoft.Json.Linq;
using Saobracaj.Agent.CimProvera.Mail;
using Saobracaj.Agent.CimProvera.Radionica;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>
    /// v3.0 (tačka 4): CimRadionica – uvoz (.eml, .msg, folder, .zip), niti, rezultat.json (šema; alat = agent), Zbirni Excel (prenos ocena),
    /// poređenje verzija i regresija, paket bez tajni, praćenje foldera (beleške, _Analiza), ANALIZA.md, alat nikad ne šalje.
    /// Stvarni mailovi: testovi\nezavisni\2026-11-17\eml\ (UID 242 bez priloga, 429/455 isti prilozi, 782 RID amonijak).
    /// </summary>
    [TestClass]
    public class V300RadionicaTests
    {
        private const string Share = @"\\192.168.1.6\Cloude\Agenti\CimProvera\testovi";
        private static string Folder => Environment.GetEnvironmentVariable("CIMPROVERA_TESTOVI") is string f && f.Length > 0 ? f : Share;
        private static string Eml(string uid) { var p = Path.Combine(Folder, "nezavisni", "2026-11-17", "eml", "INBOX__" + uid + ".eml"); if (!File.Exists(p)) Assert.Inconclusive("Nema " + p); return p; }

        private static string NoviFolder(string ime) { var d = Path.Combine(Path.GetTempPath(), "CimRadionicaTest", ime + "-" + Guid.NewGuid().ToString("N").Substring(0, 8)); Directory.CreateDirectory(d); return d; }

        private static PodesavanjaAlata Offline() => new PodesavanjaAlata { Online = false, Verzija = "test", KesFolder = Path.Combine(Path.GetTempPath(), "CimProveraTest", "stanje", "kes") };

        // ------------------------------------------------------------------------------------------- uvoz i niti

        [TestMethod]
        public void Uvoz_folder_mail_txt_i_beleske_i_zip_i_niti()
        {
            var koren = NoviFolder("uvoz");
            var m1 = Path.Combine(koren, "Aviso 1"); Directory.CreateDirectory(m1);
            File.WriteAllText(Path.Combine(m1, "mail.txt"), "From: Petar <petar@firma.rs>\nSubject: Aviso SAD-HER 80\nDate: 16 Nov 2026 10:00:00 +0100\n\nETA Subotica 18/morning", Encoding.UTF8);
            File.WriteAllBytes(Path.Combine(m1, "spisak.xlsx"), new byte[] { 1, 2, 3 });
            File.WriteAllText(Path.Combine(m1, "Aviso 1 - napomena.txt"), "proveriti tara", Encoding.UTF8);
            var a = UvozPoruka.Uvezi(m1);
            Assert.AreEqual(1, a.Count);
            Assert.AreEqual("Aviso SAD-HER 80", a[0].Naslov);
            Assert.AreEqual("petar@firma.rs", a[0].Od);
            CollectionAssert.AreEqual(new[] { "spisak.xlsx" }, a[0].Prilozi, "mail.txt i beleška nisu prilozi");
            Assert.AreEqual(1, a[0].Beleske.Count);
            StringAssert.Contains(a[0].Poruka.TextBody, "ETA Subotica");
            // .eml + odgovor u istoj niti (RE:) i .zip sa .eml
            var e1 = new MimeMessage { Subject = "Aviso SAD-HER 80", MessageId = "a1@x", Date = new DateTimeOffset(2026, 11, 16, 11, 0, 0, TimeSpan.Zero) };
            e1.From.Add(MailboxAddress.Parse("x@y.rs")); e1.Body = new TextPart("plain") { Text = "a" };
            var e2 = new MimeMessage { Subject = "RE: Aviso SAD-HER 80", MessageId = "a2@x", Date = new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero), InReplyTo = "a1@x" };
            e2.From.Add(MailboxAddress.Parse("y@y.rs")); e2.Body = new TextPart("plain") { Text = "b" };
            var z = Path.Combine(koren, "mailovi.zip");
            using (var zip = ZipFile.Open(z, ZipArchiveMode.Create))
                foreach (var (m, ime) in new[] { (e1, "1.eml"), (e2, "2.eml") })
                    using (var s = zip.CreateEntry(ime).Open()) m.WriteTo(s);
            var b = UvozPoruka.Uvezi(z);
            Assert.AreEqual(2, b.Count);
            Assert.IsTrue(b.All(x => x.Izvor == "zip"));
            var niti = UvozPoruka.Niti(a.Concat(b));
            Assert.AreEqual(1, niti.Count, "isti naslov bez RE: i veza In-Reply-To → jedna nit");
            CollectionAssert.AreEqual(new[] { "Aviso SAD-HER 80", "Aviso SAD-HER 80", "RE: Aviso SAD-HER 80" }, niti[0].Select(x => x.Naslov).ToArray(), "hronološki");
            Assert.IsTrue(UvozPoruka.JeBeleska("X - napomena.docx") && UvozPoruka.JeBeleska("Zapazanja.txt") && !UvozPoruka.JeBeleska("napomena.docx.pdf"));
            Assert.IsTrue(UvozPoruka.JeAnaliza(@"C:\Testiranje\_Analiza\3.0.0\001"));
        }

        [TestMethod]
        public void Uvoz_msg_kao_mime_poruka()
        {
            // .msg iz stvarnog sandučeta (prilog „poruka“ u mailu nije uvek dostupan) – minimalno: msg napravljen u Outlook-u na share-u
            var msg = Directory.Exists(Path.Combine(Folder, "nezavisni")) ? Directory.GetFiles(Path.Combine(Folder, "nezavisni"), "*.msg", SearchOption.AllDirectories).FirstOrDefault() : null;
            if (msg == null) Assert.Inconclusive("Nema .msg u testovima");
            var p = UvozPoruka.Uvezi(msg).Single();
            Assert.AreEqual("msg", p.Izvor);
            Assert.IsTrue(p.Naslov.Length > 0);
        }

        // ------------------------------------------------------------------------------------------- obrada, rezultat.json, analiza

        [TestMethod, TestCategory("Regresija")]
        public void Radni_prostor_obrada_bez_slanja_rezultat_zbirno_analiza_paket()
        {
            var koren = NoviFolder("Osnovni");
            var rp = RadniProstor.Otvori(Path.Combine(koren, "Radni", "Proba"));
            var nove = rp.Uvezi(new[] { Eml("242"), Eml("429"), Eml("455") });
            Assert.AreEqual(3, nove.Count);
            Assert.AreEqual(0, rp.Uvezi(new[] { Eml("429") }).Count, "ista poruka se ne uvozi dvaput");
            var poslato = new List<MimeMessage>();
            var pa = Offline();
            var rez = new List<RezultatAlata>();
            using (var rad = new RadnikObrade(rp, pa))
            {
                foreach (var s in rp.Hronoloski()) rez.Add(rad.Obradi(s));
                // alat nikad ne šalje: transport je prazan (Slanje.Transport ne zove SMTP)
                Slanje.Transport(null, new MimeMessage());
            }
            rp.Sacuvaj();
            var bez = rez.Single(x => x.Stavka.Naslov.Contains("exGK33"));
            // UID 242: samo slike iz potpisa – nema dokumenata (BezPriloga ili NemaCim), bez odgovora
            Assert.IsTrue(bez.Obrada.Status == "BezPriloga" || bez.Obrada.Status == "NemaCim", bez.Obrada.Status);
            Assert.AreEqual(0, bez.Obrada.Poruke.Count);
            var prvi = rez.Single(x => x.Stavka.MessageId.Length > 0 && x.Stavka.Kljuc == rp.Hronoloski()[1].Kljuc);
            var ponovljen = rez.Last();
            Assert.AreEqual("IstiPrilog", ponovljen.Obrada.Status, "isti prilozi kao UID 429");
            // rezultat.json: šema
            foreach (var k in new[] { "sema", "verzija", "rezim", "poruka", "korak", "status", "odluka", "odgovor", "dogadjaji", "trajanje" })
                Assert.IsNotNull(prvi.Json[k], "rezultat.json nema " + k);
            Assert.AreEqual("offline", (string)prvi.Json["rezim"]);
            Assert.IsTrue(File.Exists(Path.Combine(prvi.Folder, "rezultat.json")) && File.Exists(Path.Combine(prvi.Folder, "odgovor.html")) && File.Exists(Path.Combine(prvi.Folder, "log.txt")));
            StringAssert.Contains(File.ReadAllText(Path.Combine(prvi.Folder, "odgovor.html")), "NIJE poslat");
            // Zbirni Excel: ocene ostaju posle ponovne obrade
            var zb = Zbirno.Sacuvaj(rp, "test");
            var redovi = Xlsx.Procitaj(zb, "Zbirno");
            Assert.AreEqual(Zbirno.Redovi(rp, "test").Count, redovi.Count);
            Assert.IsTrue(redovi[0].ContainsKey("Tačno?") && redovi[0].ContainsKey("Ključ") && redovi[0].ContainsKey("r58a"));
            var l = new XlsxList { Ime = "Zbirno" };
            l.Zaglavlje.AddRange(new[] { "Ključ", "Tačno?", "Komentar" });
            foreach (var r in redovi) l.Redovi.Add(new object[] { r["Ključ"], "da", "ok " + r["NNN"] });
            Xlsx.Sacuvaj(zb, l);
            Zbirno.Sacuvaj(rp, "test");
            var posle = Xlsx.Procitaj(zb, "Zbirno");
            Assert.IsTrue(posle.All(r => r["Tačno?"] == "da" && r["Komentar"].StartsWith("ok ")), "ocene su prenete");
            // ANALIZA.md za sve tri vrste
            var an = new List<(int, RezultatAlata, string)>();
            foreach (var ra in rez) an.Add((rp.Rb(ra.Stavka), ra, Analiza.Napisi(rp.Koren, "test", rp.Rb(ra.Stavka), ra)));
            Analiza.Zbirno(rp.Koren, "test", an);
            foreach (var (_, ra, f) in an)
            {
                var md = File.ReadAllText(Path.Combine(f, "ANALIZA.md"));
                foreach (var deo in new[] { "## Tekst maila", "### Događaji", "## Prilozi i strane", "## Odgovor", "## Upis posla", "## Nesigurnosti", "## Beleške korisnika" })
                    StringAssert.Contains(md, deo, Path.GetFileName(f));
            }
            Assert.IsTrue(File.Exists(Path.Combine(rp.Koren, "_Analiza", "test", "INDEKS.md")) && File.Exists(Path.Combine(rp.Koren, "_Analiza", "test", "SAZETAK.md")) && File.Exists(Path.Combine(rp.Koren, "_Analiza", "test", "rezultati.jsonl")));
            // paket bez tajni (lažna tajna u folderu rezultata se ne pakuje)
            File.WriteAllText(Path.Combine(prvi.Folder, "Konekcija.dpapi"), "AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAA");
            File.WriteAllText(Path.Combine(prvi.Folder, "beleska.txt"), "Data Source=.\\SQLEXPRESS;Integrated Security=True");
            var paket = Zbirno.Paket(rp, "test", Path.Combine(koren, "paket.zip"), false, out var izost);
            using (var z = ZipFile.OpenRead(paket))
            {
                Assert.IsFalse(z.Entries.Any(e => e.Name.EndsWith(".dpapi") || e.Name == "beleska.txt"), string.Join(", ", z.Entries.Select(e => e.FullName)));
                Assert.IsTrue(z.Entries.Any(e => e.Name == "MANIFEST.txt") && z.Entries.Any(e => e.FullName.StartsWith("poruke/")));
            }
            Assert.AreEqual(2, izost.Count);
            // samo „ne/delimično“: nema takvih redova → paket bez poruka
            Zbirno.Paket(rp, "test", Path.Combine(koren, "paket2.zip"), true, out _);
            using (var z = ZipFile.OpenRead(Path.Combine(koren, "paket2.zip"))) Assert.IsFalse(z.Entries.Any(e => e.FullName.StartsWith("poruke/")));
        }

        [TestMethod]
        public void Pad_radnika_nije_obradjen_greska_okruzenja_u_Excelu_INDEKS_SAZETAK_i_obradi_ponovo_neuspele()
        {
            // v3.0 (Osnovni 18.11.: Smart App Control blokirao Saobracaj.Common.dll, radnik pao, 143 maila prećutno neobrađena)
            var koren = NoviFolder("neuspeh");
            var rp = RadniProstor.Otvori(Path.Combine(koren, "Radni", "N"));
            var emlovi = new List<string>();
            foreach (var i in new[] { 1, 2 })
            {
                var m = new MimeMessage { Subject = "Aviso " + i, MessageId = "n" + i + "@x", Date = new DateTimeOffset(2026, 11, 18, 10, i, 0, TimeSpan.FromHours(1)) };
                m.From.Add(MailboxAddress.Parse("a@b.rs")); m.Body = new TextPart("plain") { Text = "tekst" };
                var f = Path.Combine(koren, i + ".eml"); m.WriteTo(f); emlovi.Add(f);
            }
            rp.Uvezi(emlovi);
            var kljucevi = rp.Hronoloski().Select(x => x.Kljuc).ToList();
            // radnik je javio ishod samo za prvu poruku, pa pao (blokiran DLL)
            var razlog = "radnik je završio sa kodom -532462766: Unhandled Exception: System.IO.FileLoadException: Could not load file or assembly 'Saobracaj.Common' … An Application Control policy has blocked this file. (Exception from HRESULT: 0x800711C7)";
            var bez = RadniProstor.ZabeleziNeobradjene(rp.Koren, kljucevi, new HashSet<string> { kljucevi[0] }, "v", razlog, false, true);
            Assert.AreEqual(1, bez.Count);
            rp = RadniProstor.Otvori(rp.Koren);
            var druga = rp.Hronoloski()[1];
            Assert.AreEqual("nije obrađen – greška okruženja (blokiran DLL)", druga.Odluka);
            CollectionAssert.AreEqual(new[] { kljucevi[1] }, rp.Neuspeli("v").Select(x => x.Kljuc).ToArray(), "obradi ponovo neuspele");
            var red = Zbirno.Redovi(rp, "v").Single(r => r.Stavka.Kljuc == kljucevi[1]);
            Assert.AreEqual("nije obrađen – greška okruženja (blokiran DLL)", red.Odluka);
            Assert.AreEqual(RadniProstor.StatusNeuspeh, red.Status);
            Analiza.ZbirnoIzFoldera(rp.Koren, "v");
            StringAssert.Contains(File.ReadAllText(Path.Combine(rp.Koren, "_Analiza", "v", "INDEKS.md")), "nije obrađen – greška okruženja (blokiran DLL)");
            var sz = File.ReadAllText(Path.Combine(rp.Koren, "_Analiza", "v", "SAZETAK.md"));
            StringAssert.Contains(sz, "## Nisu obrađeni (greška okruženja): 1");
            StringAssert.Contains(sz, "Aviso 2");
            // stari rezultat iste verzije se zamenjuje zapisom o neuspehu (ne ostaje kao da je nov)
            Assert.AreEqual(RadniProstor.StatusNeuspeh, (string)Zbirno.Rezultat(rp, druga, "v")["status"]);
        }

        [TestMethod, TestCategory("Regresija")]
        public void Vise_od_25_primalaca_i_interni_nalazi_jedna_interna_kopija()
        {
            // Osnovni, UID 488 (HER-SAD81, 41 primalac): izveštaj sa internim nalazima za mail sa više od 25 primalaca pravio je dve interne kopije
            var pdf = Path.Combine(Folder, "nezavisni", "2026-11-13", "CIM COAL 16", "CIM COAL 16.pdf");
            if (!File.Exists(pdf)) Assert.Inconclusive("Nema " + pdf);
            var koren = NoviFolder("primaoci");
            var m = new MimeMessage { Subject = "Coal 16 – documents", MessageId = "p25@firma.de", Date = new DateTimeOffset(2026, 11, 16, 3, 1, 0, TimeSpan.FromHours(1)) };
            m.From.Add(MailboxAddress.Parse("disposition@firma.de"));
            foreach (var i in Enumerable.Range(1, 30)) m.To.Add(MailboxAddress.Parse("primalac" + i + "@firma" + i + ".de"));
            m.To.Add(MailboxAddress.Parse("priprema@kprevoz.co.rs"));
            var b = new BodyBuilder { TextBody = "Please find attached the CIM." };
            b.Attachments.Add("CIM COAL 16.pdf", File.ReadAllBytes(pdf));
            m.Body = b.ToMessageBody();
            var eml = Path.Combine(koren, "p25.eml"); m.WriteTo(eml);
            var rp = RadniProstor.Otvori(Path.Combine(koren, "Radni", "P"));
            rp.Uvezi(new[] { eml });
            RezultatAlata r;
            using (var rad = new RadnikObrade(rp, Offline())) r = rad.Obradi(rp.Hronoloski()[0]);
            var vrste = r.Obrada.Poruke.Select(p => p.Vrsta).ToList();
            Assert.AreEqual(1, vrste.Count(v => v == "kopija"), string.Join(", ", vrste));
            Assert.AreEqual(0, vrste.Count(v => v == "izvestaj"), "više od 25 primalaca – pošiljaocu ništa");
            StringAssert.StartsWith((string)r.Json["odluka"], "izveštaj samo interno");
            Assert.IsTrue(r.Obrada.Poruke.Single(p => p.Vrsta == "kopija").Poruka.TextBody.Contains("INTERNO"), "puna interna kopija (sa internim nalazima)");
        }

        [TestMethod]
        public void Paket_za_Claude_samo_Analiza_sivo_80_dpi_delovi_bez_mailova()
        {
            var koren = NoviFolder("claude");
            var rp = RadniProstor.Otvori(Path.Combine(koren, "Radni", "C"));
            var baza = Path.Combine(rp.Koren, "_Analiza", "v");
            var rnd = new Random(7);
            for (int i = 1; i <= 4; i++)
            {
                var d = Path.Combine(baza, i.ToString("000") + "_20261118-1000_Mail " + i);
                Directory.CreateDirectory(Path.Combine(d, "strane"));
                File.WriteAllText(Path.Combine(d, "ANALIZA.md"), "# " + i, Encoding.UTF8);
                File.WriteAllText(Path.Combine(d, "rezultat.json"), "{}", Encoding.UTF8);
                using (var b = new System.Drawing.Bitmap(1000, 1400))
                {
                    for (int x = 0; x < 1000; x += 4) for (int y = 0; y < 1400; y += 4) b.SetPixel(x, y, System.Drawing.Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256)));
                    b.Save(Path.Combine(d, "strane", "CIM_str1.jpg"), System.Drawing.Imaging.ImageFormat.Jpeg);
                }
            }
            File.WriteAllText(Path.Combine(baza, "INDEKS.md"), "# Indeks", Encoding.UTF8);
            // originalni mail i rezultati\ postoje, ali ne idu u paket
            Directory.CreateDirectory(rp.Poruke); File.WriteAllText(Path.Combine(rp.Poruke, "x.eml"), "From: a@b", Encoding.UTF8);
            Directory.CreateDirectory(Path.Combine(rp.Rezultati, "v", "m")); File.WriteAllText(Path.Combine(rp.Rezultati, "v", "m", "log.txt"), "log", Encoding.UTF8);
            var procitaj = Path.Combine(koren, "PROCITAJ.md"); File.WriteAllText(procitaj, "# Paket", Encoding.UTF8);
            var tajna = Path.Combine(baza, "001_20261118-1000_Mail 1", "beleska.txt"); File.WriteAllText(tajna, "Data Source=.\\SQLEXPRESS;Integrated Security=True");
            var velicinaSlike = Zbirno.SlikaSivo(Path.Combine(baza, "001_20261118-1000_Mail 1", "strane", "CIM_str1.jpg")).LongLength;
            // granica tako da stanu po ~2 maila u deo
            var delovi = Zbirno.PaketZaClaude(rp, "v", Path.Combine(koren, "izlaz"), new[] { procitaj }, out var izost, (long)(velicinaSlike * 2.3));
            Assert.IsTrue(delovi.Count >= 2, "podela na delove: " + delovi.Count);
            Assert.IsTrue(delovi.All(d => Path.GetFileName(d).Contains("_deo")));
            Assert.AreEqual(1, izost.Count, "beleška sa connection string-om se ne pakuje");
            var mailUDelu = new Dictionary<string, int>();
            for (int i = 0; i < delovi.Count; i++)
                using (var z = ZipFile.OpenRead(delovi[i]))
                {
                    Assert.IsFalse(z.Entries.Any(e => e.FullName.StartsWith("poruke/") || e.FullName.StartsWith("rezultati/") || e.Name.EndsWith(".eml")), string.Join(", ", z.Entries.Select(e => e.FullName)));
                    Assert.IsTrue(z.Entries.Any(e => e.Name == "MANIFEST.txt"));
                    foreach (var e in z.Entries.Where(e => e.FullName.Split('/').Length > 3))
                    {
                        var m = e.FullName.Split('/')[2];
                        if (mailUDelu.TryGetValue(m, out var di)) Assert.AreEqual(di, i, "mail " + m + " je podeljen između delova");
                        mailUDelu[m] = i;
                    }
                    foreach (var e in z.Entries.Where(e => e.FullName.EndsWith(".jpg")))
                        using (var st = e.Open()) using (var ms = new MemoryStream()) { st.CopyTo(ms); ms.Position = 0;
                            using (var b = new System.Drawing.Bitmap(ms))
                            {
                                Assert.AreEqual(800, b.Width, "80 dpi (izvor 100 dpi)");
                                var px = b.GetPixel(4, 4);
                                Assert.IsTrue(Math.Abs(px.R - px.G) <= 2 && Math.Abs(px.G - px.B) <= 2, "siva skala");
                            } }
                }
            using (var z = ZipFile.OpenRead(delovi[0]))
                Assert.IsTrue(z.Entries.Any(e => e.Name == "PROCITAJ.md") && z.Entries.Any(e => e.FullName == "_Analiza/v/INDEKS.md"), "zajednički fajlovi u prvom delu");
            Assert.AreEqual(4, mailUDelu.Count);
        }

        [TestMethod]
        public void Kes_upis_atomican_drugi_upis_istog_kljuca_se_preskace()
        {
            var d = NoviFolder("kes");
            var f = Path.Combine(d, "x_L23.json");
            Saobracaj.Agent.CimProvera.Citanje.LokalniCitac.UpisiAtomicno(f, "{\"a\":1}");
            Saobracaj.Agent.CimProvera.Citanje.LokalniCitac.UpisiAtomicno(f, "{\"a\":2}");
            Assert.AreEqual("{\"a\":1}", File.ReadAllText(f));
            Assert.AreEqual(0, Directory.GetFiles(d, "*.tmp").Length, "bez zaostalih privremenih fajlova");
        }

        [TestMethod, TestCategory("Regresija")]
        public void Isti_ulaz_isti_rezultat_json_alat_i_agent_i_poredjenje_verzija()
        {
            var koren = NoviFolder("isti");
            var rp1 = RadniProstor.Otvori(Path.Combine(koren, "Radni", "A"));
            var rp2 = RadniProstor.Otvori(Path.Combine(koren, "Radni", "B"));
            rp1.Uvezi(new[] { Eml("429") }); rp2.Uvezi(new[] { Eml("429") });
            JObject a, b;
            using (var rad = new RadnikObrade(rp1, Offline())) a = rad.Obradi(rp1.Hronoloski()[0]).Json;
            // „agent“: isti tok (ObradaPoruke) sa istom konfiguracijom, kroz ulaz iz sandučeta (UID, folder) – razlika samo u izvoru poruke
            using (var rad = new RadnikObrade(rp2, Offline())) b = rad.Obradi(rp2.Hronoloski()[0]).Json;
            var x = RezultatJson.BezVremena(a); var y = RezultatJson.BezVremena(b);
            ((JObject)x["poruka"]).Remove("folder"); ((JObject)y["poruka"]).Remove("folder");
            Assert.IsTrue(JToken.DeepEquals(x, y), "rezultat.json se razlikuje:\n" + RezultatJson.U(x).Substring(0, 500));
            // dve verzije u istom radnom prostoru: bez promene – nema razlika; ranije „Tačno? = da“ i promena → regresija
            var pa2 = Offline(); pa2.Verzija = "test2";
            rp1.KreniIspocetka();   // nova verzija = obrada od početka (dosije i ponavljanja radnog prostora se brišu; keš ostaje)
            using (var rad = new RadnikObrade(rp1, pa2)) rad.Obradi(rp1.Hronoloski()[0]);
            rp1.Sacuvaj();
            Zbirno.Sacuvaj(rp1, "test"); Zbirno.Sacuvaj(rp1, "test2");
            Assert.AreEqual(0, Zbirno.Uporedi(rp1, "test", "test2").Count, "ista obrada – nema razlika");
            var rez2 = Path.Combine(rp1.Koren, rp1.Hronoloski()[0].Rezultati["test2"], "rezultat.json");
            var j = JObject.Parse(File.ReadAllText(rez2)); j["odluka"] = "izmenjeno"; File.WriteAllText(rez2, j.ToString());
            var redovi = Xlsx.Procitaj(Zbirno.Putanja(rp1, "test"), "Zbirno");
            var l = new XlsxList { Ime = "Zbirno" }; l.Zaglavlje.AddRange(new[] { "Ključ", "Tačno?", "Komentar" });
            foreach (var r in redovi) l.Redovi.Add(new object[] { r["Ključ"], "da", "" });
            Xlsx.Sacuvaj(Zbirno.Putanja(rp1, "test"), l);
            var raz = Zbirno.Uporedi(rp1, "test", "test2");
            Assert.IsTrue(raz.Any(r => r.Polje == "Odluka" && r.Regresija));
            Assert.IsTrue(File.Exists(Zbirno.SacuvajPoredjenje(rp1, "test", "test2")));
        }
    }
}
