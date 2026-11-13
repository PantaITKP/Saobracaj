using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MimeKit;
using Saobracaj.Agent.CimProvera.Mail;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>v2.7.1 B1: sanduče agenti@, adrese iz podešavanja, zaštita od petlje (sopstveni odgovor se ne obrađuje).</summary>
    [TestClass]
    public class MailTests
    {
        private const string Agenti = "agenti@kprevoz.co.rs";

        private static AgentConfig Cfg(params (string k, string v)[] kv)
        {
            var a = new NameValueCollection();
            foreach (var (k, v) in kv) a[k] = v;
            return AgentConfig.Ucitaj(a);
        }

        private static AgentConfig CfgAgenti() => Cfg(("CitajSanduce", Agenti), ("From", Agenti), ("ReplyTo", Agenti), ("TestPrimalac", Agenti), ("Rezim", "Test"));

        [TestMethod]
        public void Odgovor_ima_From_ReplyTo_i_To_agenti()
        {
            var cfg = CfgAgenti();
            var m = Slanje.Nova(cfg, new[] { cfg.IzvestajAdresa }, null, "Tovarni list CIM 72 16550-6 3212 15690-0 – potrebne ispravke", "telo", html: "<p>telo</p>");
            Assert.AreEqual(Agenti, m.From.Mailboxes.Single().Address);
            Assert.AreEqual(Agenti, m.ReplyTo.Mailboxes.Single().Address);
            Assert.AreEqual(Agenti, m.To.Mailboxes.Single().Address);
            Assert.AreEqual("odgovor", m.Headers["X-CimProvera"]);
            Assert.AreEqual("auto-replied", m.Headers["Auto-Submitted"]);
        }

        [TestMethod]
        public void Adrese_nisu_u_kodu_sve_iz_podesavanja()
        {
            var cfg = Cfg(("CitajSanduce", "a@x.rs"), ("From", "b@x.rs"), ("ReplyTo", "c@x.rs"), ("TestPrimalac", "d@x.rs"));
            Assert.AreEqual("a@x.rs", cfg.Nalog); Assert.AreEqual("b@x.rs", cfg.PosiljalacIzvestaja);
            Assert.AreEqual("c@x.rs", cfg.ReplyTo); Assert.AreEqual("d@x.rs", cfg.IzvestajAdresa);
            // bez ključeva: nema podrazumevane (zakucane) adrese
            var prazno = Cfg();
            Assert.AreEqual("", prazno.Nalog);
            // stari ključevi i dalje rade (server dok se config ne promeni)
            var star = Cfg(("Nalog", "stari@x.rs"), ("IzvestajAdresa", "stari@x.rs"));
            Assert.AreEqual("stari@x.rs", star.Nalog); Assert.AreEqual("stari@x.rs", star.PosiljalacIzvestaja); Assert.AreEqual("stari@x.rs", star.ReplyTo);
        }

        [TestMethod]
        public void Agent_ne_cita_stefan_kada_je_podeseno_agenti()
        {
            var cfg = Cfg(("Nalog", "stefan.obradovic@kprevoz.co.rs"), ("CitajSanduce", Agenti), ("IzvestajAdresa", "stefan.obradovic@kprevoz.co.rs"), ("TestPrimalac", Agenti));
            Assert.AreEqual(Agenti, cfg.Nalog, "CitajSanduce ima prednost nad starim ključem Nalog");
            Assert.AreEqual(Agenti, cfg.IzvestajAdresa, "TestPrimalac ima prednost nad IzvestajAdresa");
            // stanje po sanduču: zapisi za stefan.obradovic@ se ne koriste za agenti@
            var s = new Stanje();
            s.Folderi["INBOX"] = new Stanje.FolderStanje { UidValidity = 1638103329, PoslednjiUid = 1277 };
            s.Folderi[Stanje.KljucSanduceta("stefan.obradovic@kprevoz.co.rs", "INBOX")] = new Stanje.FolderStanje { UidValidity = 1, PoslednjiUid = 1300 };
            var sada = new DateTimeOffset(2026, 11, 13, 9, 0, 0, TimeSpan.FromHours(1));
            var fs = s.FolderSanduceta(cfg.Nalog, "INBOX", sada);
            Assert.AreEqual(0, fs.PoslednjiUid, "novo sanduče kreće od početka, ne od UID-a drugog sanduča");
            Assert.AreEqual(sada, fs.NultaTacka, "nulta tačka novog sanduča = prvi prolaz nove verzije (stare poruke se ne obrađuju)");
            Assert.AreNotEqual(Stanje.KljucPoruke("stefan.obradovic@kprevoz.co.rs", "<a@b>"), Stanje.KljucPoruke(Agenti, "<a@b>"));
            Assert.AreSame(fs, s.FolderSanduceta(Agenti.ToUpperInvariant(), "INBOX", sada.AddHours(1)), "isto sanduče = isto stanje; nulta tačka se ne pomera");
            Assert.AreEqual(sada, fs.NultaTacka);
        }

        [TestMethod]
        public void Posle_reseta_novo_sanduce_koristi_nultu_tacku_reseta()
        {
            var reset = new DateTimeOffset(2026, 11, 13, 8, 0, 0, TimeSpan.FromHours(1));
            var s = new Stanje { NultaTacka = reset };
            Assert.AreEqual(reset, s.FolderSanduceta(Agenti, "INBOX", reset.AddMinutes(2)).NultaTacka);
        }

        /// <summary>Odgovor agenta (sa PDF prilogom), kako stiže u isto sanduče, ne pokreće novu proveru.</summary>
        [TestMethod]
        public void Odgovor_agenta_ne_pokrece_novu_proveru()
        {
            var cfg = CfgAgenti();
            var m = Slanje.Nova(cfg, new[] { cfg.IzvestajAdresa }, null, "Tovarni list CIM 72 16550-6 3212 15690-0 (RuSz008/26) – potrebne ispravke", "telo", html: "<p>telo</p>");
            Slanje.DodajPrilog(m, "PRIMER_72165506321215690-0.pdf", new byte[] { 37, 80, 68, 70 });
            MimeMessage primljen;
            using (var ms = new MemoryStream()) { m.WriteTo(ms); ms.Position = 0; primljen = MimeMessage.Load(ms); }
            Assert.AreEqual("sopstveni odgovor", Slanje.RazlogPreskakanja(primljen, cfg, true));
            Assert.AreEqual(1, primljen.Attachments.Count());
            Assert.IsTrue(primljen.BodyParts.OfType<TextPart>().Any(t => t.IsHtml) && primljen.BodyParts.OfType<TextPart>().Any(t => t.IsPlain), "multipart: tekst + HTML + prilog");
        }

        [TestMethod]
        public void Naslov_odgovora_od_agenti_se_preskace_i_bez_zaglavlja()
        {
            var cfg = CfgAgenti();
            foreach (var naslov in new[] { "Tovarni list CIM 72 16550-6 3212 15690-0 – potrebne ispravke", "Tovarni list CIM 55 12740-7 2155 71163-0 (0081264631) – napomene" })
            {
                var m = new MimeMessage(); m.From.Add(MailboxAddress.Parse(Agenti)); m.To.Add(MailboxAddress.Parse(Agenti)); m.Subject = naslov; m.Body = new TextPart("plain") { Text = "x" };
                Assert.IsNotNull(Slanje.RazlogPreskakanja(m, cfg, true), naslov);
            }
            // isti naslov od pošiljaoca koji nije agent = obična poruka
            var t = new MimeMessage(); t.From.Add(MailboxAddress.Parse("posiljalac@firma.rs")); t.Subject = "Tovarni list CIM 72 – potrebne ispravke"; t.Body = new TextPart("plain") { Text = "x" };
            Assert.IsNull(Slanje.RazlogPreskakanja(t, cfg, true));
        }
    }
}
