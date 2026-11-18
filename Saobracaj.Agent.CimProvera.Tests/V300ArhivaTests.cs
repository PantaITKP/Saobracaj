using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MimeKit;
using Saobracaj.Agent.CimProvera.Mail;

namespace Saobracaj.Agent.CimProvera.Tests
{
    /// <summary>
    /// v3.0 (tačka 5, PROMPT_v2.9.3 tačka 4.3): arhiva sandučeta i uticaj brisanja poruka / promene UIDVALIDITY – poslednji UID,
    /// ponavljanja (Message-ID i heš priloga), arhiva samo za dopisivanje.
    /// </summary>
    [TestClass]
    public class V300ArhivaTests
    {
        private static string Folder() { var d = Path.Combine(Path.GetTempPath(), "CimProveraTest", "arhiva-" + Guid.NewGuid().ToString("N").Substring(0, 8)); Directory.CreateDirectory(d); return d; }

        private static MimeMessage Mail(string mid, string naslov, byte[] prilog = null, bool sopstveni = false)
        {
            var m = new MimeMessage { MessageId = mid, Subject = naslov, Date = new DateTimeOffset(2026, 11, 17, 10, 0, 0, TimeSpan.FromHours(1)) };
            m.From.Add(MailboxAddress.Parse("x@firma.rs"));
            var b = new BodyBuilder { TextBody = "telo" };
            if (prilog != null) b.Attachments.Add("CIM.pdf", prilog);
            m.Body = b.ToMessageBody();
            if (sopstveni) Slanje.OznaciOdgovor(m);
            return m;
        }

        [TestMethod]
        public void Arhiva_eml_po_uid_indeks_samo_dopisivanje_i_promena_uidvalidity()
        {
            var ar = new Arhiva(Folder());
            Assert.IsTrue(ar.Sacuvaj("INBOX", 100, 191, Mail("a@x", "Prvi", new byte[] { 1, 2, 3 })));
            Assert.IsTrue(ar.Sacuvaj("INBOX", 100, 192, Mail("b@x", "Drugi")));
            Assert.IsTrue(ar.Sacuvaj("INBOX.Sent", 7, 5, Mail("c@x", "Tovarni list CIM 1 – potrebne ispravke", null, sopstveni: true)));
            Assert.IsFalse(ar.Sacuvaj("INBOX", 100, 191, Mail("a2@x", "Izmenjen")), "postojeća poruka se ne prepisuje");
            Assert.AreEqual("a@x", MimeMessage.Load(ar.Putanja("INBOX", 100, 191)).MessageId);
            var z = ar.Procitaj();
            Assert.AreEqual(3, z.Count);
            Assert.AreEqual(16, z.Single(x => x.Uid == 191).HesPriloga.Length);
            Assert.IsTrue(z.Single(x => x.Folder == "INBOX.Sent").Sopstveni);
            CollectionAssert.AreEqual(new long[] { 192 }, ar.Poruke("INBOX", 192).Select(x => x.Uid).ToArray());
            Assert.AreEqual(0, ar.Poruke("INBOX.Sent", 0).Count, "sopstveni odgovori se ne obrađuju ponovo");
            // folder obrisan i napravljen ponovo (UIDVALIDITY 200): UID 191 ponovo postoji – stara arhiva ostaje, nova ide u INBOX@200
            var upozorenja = new List<string>();
            Assert.IsTrue(ar.Sacuvaj("INBOX", 200, 191, Mail("d@x", "Posle brisanja"), upozorenja.Add));
            Assert.AreEqual("a@x", MimeMessage.Load(ar.Putanja("INBOX", 100, 191)).MessageId);
            Assert.AreEqual("d@x", MimeMessage.Load(ar.Putanja("INBOX", 200, 191)).MessageId);
            Assert.AreEqual(1, upozorenja.Count);
            StringAssert.Contains(ar.Izvestaj(), "INBOX: 3 poruka");
            Assert.IsTrue((File.GetAttributes(ar.Putanja("INBOX", 100, 191)) & FileAttributes.ReadOnly) != 0, "arhiva je samo za čitanje");
        }

        [TestMethod]
        public void Brisanje_poruka_i_novi_UIDVALIDITY_bez_ponavljanja_i_bez_preskakanja()
        {
            var st = Folder();
            var stanje = Stanje.Ucitaj(st);
            var fs = stanje.FolderSanduceta("agenti@kprevoz.co.rs", "INBOX", DateTimeOffset.Now);
            RedPoruka.Stavka S(long uid, string mid) => new RedPoruka.Stavka { Uid = uid, Folder = "INBOX", MessageId = mid, Kljuc = Stanje.KljucPoruke("agenti@kprevoz.co.rs", mid) };
            var obradjene = new List<string>();
            Korak Obradi(RedPoruka.Stavka s, Stanje.PorukaStanje ps) { obradjene.Add(s.MessageId); ps.Status = "Ispravan"; return Korak.Obradjena; }
            new RedPoruka(stanje, fs, 3, null, false).Obradi(new[] { S(191, "a@x"), S(192, "b@x"), S(193, "c@x") }, Obradi);
            Assert.AreEqual(193, fs.PoslednjiUid);
            // 1) korisnik obriše UID 191–192 iz sandučeta: nova poruka 194 se obrađuje, ništa se ne ponavlja
            new RedPoruka(stanje, fs, 3, null, false).Obradi(new[] { S(194, "d@x") }, Obradi);
            Assert.AreEqual(194, fs.PoslednjiUid);
            // 2) UIDVALIDITY se promenio (folder obrisan i napravljen ponovo): agent čita od nulte tačke; iste poruke (novi UID-ovi)
            // se prepoznaju po Message-ID i ne obrađuju ponovo, nova poruka se obrađuje
            fs.UidValidity = 999; fs.PoslednjiUid = 0;
            new RedPoruka(stanje, fs, 3, null, false).Obradi(new[] { S(1, "c@x"), S(2, "d@x"), S(3, "e@x") }, Obradi);
            CollectionAssert.AreEqual(new[] { "a@x", "b@x", "c@x", "d@x", "e@x" }, obradjene, "svaka poruka tačno jednom");
            Assert.AreEqual(3, fs.PoslednjiUid);
        }

        [TestMethod]
        public void Ponovljen_mail_po_hesu_priloga_i_kada_je_raniji_obrisan()
        {
            var st = Folder();
            var pon = Ponavljanja.Ucitaj(st);
            var pdf = Enumerable.Range(0, 20000).Select(i => (byte)(i % 251)).ToArray();   // značajan prilog (≥ 10 KB)
            var prilozi = new List<(string, byte[])> { ("CIM.pdf", pdf) };
            pon.ZapamtiPriloge(prilozi, "UID 191", "izveštaj poslat", new DateTime(2026, 11, 17));
            pon.Sacuvaj();
            // raniji mail je obrisan iz sandučeta (i UIDVALIDITY promenjen) – ponavljanja su u stanju agenta, ne u sandučetu
            var posle = Ponavljanja.Ucitaj(st);
            Assert.IsNotNull(posle.IstiPrilozi(new List<(string, byte[])> { ("drugo ime.pdf", pdf.ToArray()) }), "isti heš, drugo ime");
            var drugi = pdf.ToArray(); drugi[100] ^= 1;
            Assert.IsNull(posle.IstiPrilozi(new List<(string, byte[])> { ("CIM.pdf", drugi) }));
        }
    }
}
