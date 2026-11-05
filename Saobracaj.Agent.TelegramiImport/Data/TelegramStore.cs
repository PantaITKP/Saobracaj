using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Saobracaj.Agent.TelegramiImport.Mail;
using Saobracaj.Agent.TelegramiImport.Parsing;
using Saobracaj.Common.Data;
using Saobracaj.Common.Text;

namespace Saobracaj.Agent.TelegramiImport.Data
{
    /// <summary>Upisi u bazu za jednu poruku; sve u transakciji koju otvara <see cref="Obrada"/>.</summary>
    internal sealed class TelegramStore
    {
        private readonly SqlConnection c;
        private readonly SqlTransaction tx;

        public TelegramStore(SqlConnection c, SqlTransaction tx)
        {
            this.c = c;
            this.tx = tx;
        }

        // ------------------------------------------------------------------ idempotentnost

        public bool VecObradjena(Poruka p)
        {
            var n = SqlDb.Scalar(c, tx, @"SELECT COUNT(*) FROM dbo.TelegramiMailLog WITH (UPDLOCK, HOLDLOCK)
                WHERE (Folder = @f AND UidValidity = @v AND Uid = @u) OR (@mid IS NOT NULL AND MessageId = @mid)",
                "@f", p.Folder, "@v", p.UidValidity, "@u", p.Uid, "@mid", string.IsNullOrEmpty(p.MessageId) ? null : p.MessageId);
            return Convert.ToInt32(n) > 0;
        }

        public static long PoslednjiUid(SqlConnection c, string folder, long uidValidity)
        {
            var v = SqlDb.Scalar(c, null, "SELECT MAX(Uid) FROM dbo.TelegramiMailLog WHERE Folder = @f AND UidValidity = @v", "@f", folder, "@v", uidValidity);
            return v == null ? 0 : Convert.ToInt64(v);
        }

        // ------------------------------------------------------------------ log poruke i prilozi

        public int UpisiMailLog(Poruka p, bool dryRun)
        {
            var id = SqlDb.Scalar(c, tx, @"INSERT dbo.TelegramiMailLog (Folder, UidValidity, Uid, MessageId, Primljeno, Posiljalac, Naslov, Status, TekstMaila, DryRun)
                VALUES (@f, @v, @u, @mid, @prim, @pos, @nas, N'Greska', @tm, @dry); SELECT CAST(SCOPE_IDENTITY() AS int);",
                "@f", p.Folder, "@v", p.UidValidity, "@u", p.Uid, "@mid", Skrati(p.MessageId, 255), "@prim", p.Primljeno,
                "@pos", Skrati(p.Posiljalac, 255), "@nas", Skrati(p.Naslov, 500), "@tm", p.TekstMaila, "@dry", dryRun);
            int mailLogId = Convert.ToInt32(id);
            foreach (var a in p.Prilozi)
            {
                string sha;
                using (var h = SHA256.Create()) sha = string.Concat(h.ComputeHash(a.Sadrzaj).Select(b => b.ToString("x2")));
                SqlDb.Execute(c, tx, @"INSERT dbo.TelegramiPrilozi (MailLogID, NazivFajla, ContentType, Velicina, Sha256, Sadrzaj)
                    VALUES (@m, @n, @ct, @vel, @sha, @s)",
                    "@m", mailLogId, "@n", Skrati(a.Naziv ?? "prilog", 260), "@ct", Skrati(a.ContentType, 100), "@vel", a.Sadrzaj.Length, "@sha", sha, "@s", a.Sadrzaj);
            }
            return mailLogId;
        }

        public void ZavrsiMailLog(int mailLogId, string status, ParsiranTelegram t, int brojRedova, IEnumerable<string> poruke)
        {
            var tekst = string.Join(Environment.NewLine, poruke.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
            SqlDb.Execute(c, tx, @"UPDATE dbo.TelegramiMailLog SET Status = @s, Vrsta = @vr, BrojTelegrama = @b, OznakaTelegrama = @o,
                    DatumTelegrama = @d, Poruka = @p, BrojRedova = @n, TekstTelegrama = @tt, Obradjeno = GETDATE()
                WHERE ID = @id",
                "@s", status, "@vr", t == null ? null : Skrati(t.VrsteTekst, 100), "@b", t?.Broj, "@o", t?.Oznaka, "@d", t?.Datum,
                "@p", tekst.Length == 0 ? null : tekst, "@n", brojRedova, "@tt", t?.Telo, "@id", mailLogId);
        }

        public void UpisiStavku(int mailLogId, ParsiranTelegram t, Stavka s, RazresenaStavka r, string status, int? telegramiId)
        {
            SqlDb.Execute(c, tx, @"INSERT dbo.TelegramiMailStavke (MailLogID, Tacka, Vrsta, BrojTelegrama, DatumTelegrama, PrugaTekst, PrugaID,
                    OdStaniceTekst, OdStanice, DoStaniceTekst, DoStanice, Kolosek, VaziOd, VaziDo, Napomena, NarocitaPosiljka, Status, Upozorenje, TelegramiID)
                VALUES (@m, @ta, @vr, @b, @d, @pt, @p, @ot, @o, @dt, @do, @k, @vod, @vdo, @n, @np, @s, @u, @tid)",
                "@m", mailLogId, "@ta", s.Tacka, "@vr", s.Vrsta.ToString(), "@b", t.Broj, "@d", t.Datum,
                "@pt", Skrati(s.PrugaTekst, 300), "@p", r.PrugaID, "@ot", Skrati(s.OdTekst, 100), "@o", r.OdStanice,
                "@dt", Skrati(s.DoTekst, 100), "@do", r.DoStanice, "@k", Skrati(s.Kolosek, 50), "@vod", s.VaziOd, "@vdo", s.VaziDo,
                "@n", Skrati(s.Napomena, TelegramParser.NapomenaMax), "@np", s.NarocitaPosiljka, "@s", status,
                "@u", Skrati(string.Join(" | ", r.Upozorenja.Concat(s.Upozorenja).Distinct()), 1000), "@tid", telegramiId);
        }

        // ------------------------------------------------------------------ Telegrami

        /// <summary>Postojeći red sa istim brojem, relacijom i periodom (do na sekunde), npr. ručni unos istog telegrama.</summary>
        public int? Duplikat(ParsiranTelegram t, RazresenaStavka r, Stavka s)
        {
            var v = SqlDb.Scalar(c, tx, @"SELECT TOP 1 ID FROM dbo.Telegrami
                WHERE BrojTelegrama = @b AND OdStanice = @od AND DoStanice = @do
                  AND ABS(DATEDIFF(minute, VaziOd, @vod)) <= 1 AND ABS(DATEDIFF(minute, VaziDo, @vdo)) <= 1
                  AND (DatumTelegrama = @dt OR (DatumTelegrama IS NULL AND VaziOd >= DATEADD(day, -7, @dt) AND VaziOd < DATEADD(day, 120, @dt)))
                ORDER BY ID",
                "@b", t.Broj, "@od", r.OdStanice, "@do", r.DoStanice, "@vod", s.VaziOd, "@vdo", s.VaziDo, "@dt", t.Datum ?? s.VaziOd.Date);
            return v == null ? (int?)null : Convert.ToInt32(v);
        }

        /// <summary>Upis jednog reda. ID = MAX(ID)+1 kao u proceduri InsertTelegrami, ali pod zaključavanjem.</summary>
        public int UpisiTelegram(ParsiranTelegram t, Stavka s, RazresenaStavka r, int mailLogId, string pdf)
        {
            SqlDb.LockTransaction(c, tx, "Telegrami.ID");
            var id = SqlDb.Scalar(c, tx, @"DECLARE @id int;
                SELECT @id = ISNULL(MAX(ID), 0) + 1 FROM dbo.Telegrami WITH (UPDLOCK, HOLDLOCK);
                INSERT dbo.Telegrami (ID, BrojTelegrama, PrugaID, OdStanice, DoStanice, Kolosek, VaziOd, VaziDo, TrajeOd, TrajeDo,
                                      Napomena, Aktivan, PDF, NarocitaPosiljka, DatumTelegrama, Vrsta, MailLogID, UnosAutomatski)
                VALUES (@id, @b, @p, @od, @do, @k, @vod, @vdo, '00:00:00', '23:59:59', @n, 1, @pdf, @np, @dt, @vr, @m, 1);
                SELECT @id;",
                "@b", t.Broj, "@p", r.PrugaID, "@od", r.OdStanice, "@do", r.DoStanice, "@k", Skrati(s.Kolosek, 50),
                "@vod", BezSekundi(s.VaziOd), "@vdo", BezSekundi(s.VaziDo), "@n", Skrati(s.Napomena, TelegramParser.NapomenaMax),
                "@pdf", pdf ?? "", "@np", s.NarocitaPosiljka, "@dt", t.Datum, "@vr", s.Vrsta.ToString(), "@m", mailLogId);
            return Convert.ToInt32(id);
        }

        public void PostaviPdf(IEnumerable<int> ids, string pdf)
        {
            foreach (var id in ids)
                SqlDb.Execute(c, tx, "UPDATE dbo.Telegrami SET PDF = @p WHERE ID = @id", "@p", pdf, "@id", id);
        }

        // ------------------------------------------------------------------ otkazivanje / van snage

        public sealed class RezultatOtkazivanja
        {
            public int Ugaseno;
            public bool CiljPostoji;
            public string Opis;
        }

        /// <param name="dozvoliStareRedove">
        /// Ručno uneti redovi nemaju DatumTelegrama; uparuju se po broju samo ako se otkazivanje odnosi na zatvor/skretnicu/NP
        /// i red još važi. Inače bi "van snage tel. 96 (posedanje)" ugasio nevezan zatvor br. 96 iz istog perioda.
        /// </param>
        public RezultatOtkazivanja PrimeniOtkazivanje(ParsiranTelegram novi, CiljniTelegram cilj, bool dozvoliStareRedove)
        {
            var rez = new RezultatOtkazivanja();
            if (cilj.Datum == null)
            {
                rez.Opis = "Ciljni telegram " + cilj.Broj + " nema datum.";
                return rez;
            }
            var redovi = SqlDb.Table(c, tx, @"SELECT t.ID, t.Napomena, t.VaziOd, t.VaziDo, t.Aktivan, s.Tacka
                FROM dbo.Telegrami t WITH (UPDLOCK)
                LEFT JOIN dbo.TelegramiMailStavke s ON s.TelegramiID = t.ID
                WHERE t.BrojTelegrama = @b
                  AND (t.DatumTelegrama = @d
                       OR (@stari = 1 AND t.DatumTelegrama IS NULL
                           AND t.VaziOd >= DATEADD(day, -1, @d) AND t.VaziOd < DATEADD(day, 60, @d)
                           AND t.VaziDo >= DATEADD(day, -1, @sada)))",
                "@b", cilj.Broj, "@d", cilj.Datum.Value, "@stari", dozvoliStareRedove, "@sada", novi.Datum ?? DateTime.Today)
                .Rows.Cast<DataRow>().ToList();

            if (cilj.Tacka != null)
                redovi = redovi.Where(r =>
                {
                    var tacka = SqlDb.Text(r, "Tacka").TrimEnd(')');
                    var nap = SqlDb.Text(r, "Napomena").TrimStart();
                    return tacka == cilj.Tacka || nap.StartsWith(cilj.Tacka + ".") || nap.StartsWith(cilj.Tacka + ")");
                }).ToList();
            if (cilj.Dani.Count > 0)
                redovi = redovi.Where(r =>
                {
                    var od = Convert.ToDateTime(r["VaziOd"]).Date;
                    var @do = Convert.ToDateTime(r["VaziDo"]).Date;
                    return cilj.Dani.Any(d => d >= od && d <= @do);
                }).ToList();

            rez.CiljPostoji = redovi.Count > 0;
            var oznaka = CiljniTelegram.Oznaka(cilj.Akcija, novi.Oznaka ?? novi.Broj?.ToString());
            foreach (var r in redovi)
            {
                var nap = SqlDb.Text(r, "Napomena");
                var aktivan = !r.IsNull("Aktivan") && Convert.ToBoolean(r["Aktivan"]);
                if (!aktivan && nap.Contains(oznaka.Trim())) continue; // već primenjeno (isti telegram stigao ponovo)
                if (!nap.Contains(oznaka.Trim())) nap = oznaka + nap;
                SqlDb.Execute(c, tx, "UPDATE dbo.Telegrami SET Aktivan = 0, Napomena = @n WHERE ID = @id",
                    "@n", TekstUtil.SkratiNaRec(nap, TelegramParser.NapomenaMax), "@id", Convert.ToInt32(r["ID"]));
                rez.Ugaseno++;
            }
            if (!rez.CiljPostoji)
            {
                // Ciljni telegram je možda stigao mail-om, ali nije upisivan u Telegrami (lagana vožnja, posedanje ...)
                var log = SqlDb.Scalar(c, tx, "SELECT TOP 1 ID FROM dbo.TelegramiMailLog WHERE BrojTelegrama = @b AND DatumTelegrama = @d",
                    "@b", cilj.Broj, "@d", cilj.Datum.Value);
                if (log != null)
                {
                    rez.CiljPostoji = true;
                    rez.Opis = "Ciljni telegram " + cilj.Broj + "/" + cilj.Datum.Value.ToString("dd.MM.yyyy") + " nije upisivan u Telegrami (arhiviran u MailLog ID " + log + ").";
                }
                else rez.Opis = "Ciljni telegram " + cilj.Broj + "/" + cilj.Datum.Value.ToString("dd.MM.yyyy") + " nije pronađen u bazi.";
            }
            else rez.Opis = cilj.Akcija + ": telegram " + cilj.Broj + "/" + cilj.Datum.Value.ToString("dd.MM.yyyy") + ", ugašeno redova: " + rez.Ugaseno + ".";
            return rez;
        }

        /// <summary>
        /// Redovi kojima je istekao VaziDo → Aktivan = 0 (ni aplikacija ni SQL job to ne rade; SQL Express nema SQL Agent).
        /// Važi za sve redove, i ručno unete. Redovi bez VaziDo se ne diraju. Vraća ID-jeve ugašenih redova.
        /// </summary>
        public static List<int> UgasiIstekle(SqlConnection c)
        {
            return SqlDb.Table(c, null, "UPDATE dbo.Telegrami SET Aktivan = 0 OUTPUT inserted.ID WHERE Aktivan = 1 AND VaziDo IS NOT NULL AND VaziDo < GETDATE()")
                .Rows.Cast<DataRow>().Select(r => Convert.ToInt32(r["ID"])).OrderBy(x => x).ToList();
        }

        // ------------------------------------------------------------------ pomoćno

        private static DateTime BezSekundi(DateTime d) { return new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0); }

        private static string Skrati(string s, int max)
        {
            if (s == null) return null;
            return s.Length <= max ? s : TekstUtil.SkratiNaRec(s, max);
        }

        /// <summary>Snima PDF-ove u folder kao u ručnom unosu (\\192.168.1.6\Telegrami\&lt;ID prvog reda&gt;). Vraća putanju ili null.</summary>
        public static string SnimiPdf(string pdfFolder, int prviId, IEnumerable<Prilog> pdfovi, List<string> upozorenja)
        {
            if (string.IsNullOrWhiteSpace(pdfFolder)) return null;
            try
            {
                var folder = Path.Combine(pdfFolder, prviId.ToString());
                Directory.CreateDirectory(folder);
                foreach (var p in pdfovi)
                {
                    var ime = string.Concat((p.Naziv ?? "telegram.pdf").Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
                    File.WriteAllBytes(Path.Combine(folder, ime), p.Sadrzaj);
                }
                return folder;
            }
            catch (Exception ex)
            {
                upozorenja.Add("PDF nije snimljen u " + pdfFolder + ": " + ex.Message);
                return null;
            }
        }
    }

    /// <summary>Stavka sa ID-jevima iz šifarnika.</summary>
    internal sealed class RazresenaStavka
    {
        public int? PrugaID;
        public int? OdStanice;
        public int? DoStanice;
        public List<string> Upozorenja = new List<string>();
        public bool Potpuna { get { return PrugaID != null && OdStanice != null && DoStanice != null; } }
    }
}
