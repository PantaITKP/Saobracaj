using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Saobracaj.Agenti_jobs;
using Saobracaj.Common.Text;

namespace Saobracaj.Telegrami
{
    /// <summary>
    /// Pregled poruka koje je agent označio sa NeedsReview: dopuna pruge/stanica, potvrda (upis u Telegrami),
    /// odbacivanje, arhiviranje. Upis je isti kao u agentu (TelegramStore.UpisiTelegram):
    /// ID = MAX(ID)+1 pod zaključavanjem 'Telegrami.ID', bez procedure InsertTelegrami
    /// (procedura ima upisano ime baze [Perftech_Beograd]).
    /// </summary>
    internal static class TelegramiPregledRepository
    {
        public const string NaPregledu = "NeedsReview";

        internal static int BrojNaPregledu()
        {
            using (var c = Db.Open())
            {
                if (!TelegramiRepository.ImaMigraciju(c)) return 0;
                using (var cmd = Db.Command(c, null, "SELECT COUNT(*) FROM dbo.TelegramiMailLog WHERE Status = N'NeedsReview'"))
                    return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        internal static DataTable Poruke(string status, DateTime od, DateTime @do)
        {
            using (var c = Db.Open())
                return Db.Table(c, null, @"SELECT l.ID, l.Primljeno, l.Posiljalac, l.Naslov, l.OznakaTelegrama, l.BrojTelegrama, l.DatumTelegrama,
                        l.Vrsta, l.Status, l.Poruka, l.TekstTelegrama, l.TekstMaila, l.Pregledao, l.Pregledano,
                        (SELECT COUNT(*) FROM dbo.TelegramiMailStavke s WHERE s.MailLogID = l.ID) AS Stavki,
                        (SELECT COUNT(*) FROM dbo.TelegramiPrilozi p WHERE p.MailLogID = l.ID) AS Priloga
                    FROM dbo.TelegramiMailLog l
                    WHERE (@s IS NULL OR l.Status = @s) AND l.Primljeno >= @od AND l.Primljeno < DATEADD(day, 1, @do)
                    ORDER BY l.Primljeno DESC, l.ID DESC",
                    "@s", string.IsNullOrEmpty(status) ? null : status, "@od", od.Date, "@do", @do.Date);
        }

        internal static DataTable Stavke(int mailLogId)
        {
            using (var c = Db.Open())
                return Db.Table(c, null, @"SELECT ID, MailLogID, Tacka, Vrsta, BrojTelegrama, DatumTelegrama, PrugaTekst, PrugaID,
                        OdStaniceTekst, OdStanice, DoStaniceTekst, DoStanice, Kolosek, VaziOd, VaziDo, Napomena, NarocitaPosiljka,
                        Status, Upozorenje, TelegramiID, Potvrdio, Potvrdjeno
                    FROM dbo.TelegramiMailStavke WHERE MailLogID = @m ORDER BY ID", "@m", mailLogId);
        }

        internal static DataTable Pruge()
        {
            using (var c = Db.Open())
                return Db.Table(c, null, "SELECT ID, RTRIM(Oznaka) + CASE WHEN RTRIM(Oznaka) = '' THEN '' ELSE '-' END + LTRIM(RTRIM(Opis)) AS Opis FROM dbo.Pruga ORDER BY Opis");
        }

        internal static DataTable Stanice()
        {
            using (var c = Db.Open())
                return Db.Table(c, null, "SELECT ID, RTRIM(Opis) + CASE WHEN COUNT(*) OVER (PARTITION BY RTRIM(Opis)) > 1 THEN ' (' + CAST(ID AS varchar(10)) + ')' ELSE '' END AS Opis FROM dbo.stanice ORDER BY Opis");
        }

        /// <summary>Čuva izmene korisnika u stavci (nacrt), bez upisa u Telegrami.</summary>
        internal static void SacuvajNacrt(DataRow r)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Command(c, null, @"UPDATE dbo.TelegramiMailStavke SET BrojTelegrama = @b, DatumTelegrama = @dt, PrugaID = @p,
                    OdStanice = @od, DoStanice = @do, Kolosek = @k, VaziOd = @vod, VaziDo = @vdo, Napomena = @n
                WHERE ID = @id AND Status = N'NeedsReview'",
                "@b", r["BrojTelegrama"], "@dt", r["DatumTelegrama"], "@p", r["PrugaID"], "@od", r["OdStanice"], "@do", r["DoStanice"],
                "@k", Skrati(Db.Text(r, "Kolosek"), 50), "@vod", r["VaziOd"], "@vdo", r["VaziDo"], "@n", Skrati(Db.Text(r, "Napomena"), 250), "@id", r["ID"]))
                cmd.ExecuteNonQuery();
        }

        internal static int DodajStavku(int mailLogId)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Command(c, null, @"INSERT dbo.TelegramiMailStavke (MailLogID, Vrsta, BrojTelegrama, DatumTelegrama, Kolosek, VaziOd, VaziDo, Status, Upozorenje)
                SELECT ID, N'Zatvor', BrojTelegrama, DatumTelegrama, N'pruga', CAST(CONVERT(date, GETDATE()) AS datetime), DATEADD(minute, 1439, CAST(CONVERT(date, GETDATE()) AS datetime)),
                       N'NeedsReview', N'Ručno dodata stavka.' FROM dbo.TelegramiMailLog WHERE ID = @m;
                SELECT CAST(SCOPE_IDENTITY() AS int);", "@m", mailLogId))
                return Convert.ToInt32(cmd.ExecuteScalar());
        }

        internal sealed class RezultatPotvrde
        {
            public int Upisano;
            public int Duplikata;
            public List<string> Greske = new List<string>();
        }

        /// <summary>
        /// Upis izabranih stavki u Telegrami, u jednoj transakciji. Vrednosti se uzimaju iz grida (izmene korisnika).
        /// Za nazive koje agent nije prepoznao (ID je bio NULL) a korisnik ih je izabrao, upisuje se TelegramiAlias.
        /// </summary>
        internal static RezultatPotvrde Potvrdi(int mailLogId, IList<DataRow> stavke, bool zapamtiNazive, string korisnik)
        {
            var rez = new RezultatPotvrde();
            foreach (var r in stavke)
            {
                var red = (r["Tacka"] == DBNull.Value ? "" : "t." + r["Tacka"] + " ") + Db.Text(r, "OdStaniceTekst") + " - " + Db.Text(r, "DoStaniceTekst") + ": ";
                if (r.IsNull("BrojTelegrama")) rez.Greske.Add(red + "nedostaje broj telegrama.");
                if (r.IsNull("DatumTelegrama")) rez.Greske.Add(red + "nedostaje datum telegrama.");
                if (r.IsNull("PrugaID")) rez.Greske.Add(red + "izaberite prugu.");
                if (r.IsNull("OdStanice")) rez.Greske.Add(red + "izaberite stanicu Od.");
                if (r.IsNull("DoStanice")) rez.Greske.Add(red + "izaberite stanicu Do.");
                if (r.IsNull("VaziOd") || r.IsNull("VaziDo")) rez.Greske.Add(red + "nedostaje period važenja.");
                else if (Convert.ToDateTime(r["VaziOd"]) >= Convert.ToDateTime(r["VaziDo"])) rez.Greske.Add(red + "Važi od mora biti pre Važi do.");
            }
            if (rez.Greske.Count > 0) return rez;

            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                Db.Lock(c, tx, "Telegrami.ID");
                foreach (var r in stavke)
                {
                    var id = Convert.ToInt32(r["ID"]);
                    var pre = Db.Table(c, tx, "SELECT PrugaID, OdStanice, DoStanice, Status FROM dbo.TelegramiMailStavke WITH (UPDLOCK) WHERE ID = @id", "@id", id);
                    if (pre.Rows.Count == 0 || Db.Text(pre.Rows[0], "Status") != NaPregledu) continue; // već obrađeno (drugi korisnik)

                    var vod = BezSekundi(Convert.ToDateTime(r["VaziOd"]));
                    var vdo = BezSekundi(Convert.ToDateTime(r["VaziDo"]));
                    object dup;
                    using (var cmd = Db.Command(c, tx, @"SELECT TOP 1 ID FROM dbo.Telegrami
                        WHERE BrojTelegrama = @b AND OdStanice = @od AND DoStanice = @do
                          AND ABS(DATEDIFF(minute, VaziOd, @vod)) <= 1 AND ABS(DATEDIFF(minute, VaziDo, @vdo)) <= 1
                          AND (DatumTelegrama = @dt OR (DatumTelegrama IS NULL AND VaziOd >= DATEADD(day, -7, @dt) AND VaziOd < DATEADD(day, 120, @dt)))
                        ORDER BY ID",
                        "@b", r["BrojTelegrama"], "@od", r["OdStanice"], "@do", r["DoStanice"], "@vod", vod, "@vdo", vdo, "@dt", r["DatumTelegrama"]))
                        dup = cmd.ExecuteScalar();

                    int telegramiId;
                    string status;
                    if (dup != null && dup != DBNull.Value)
                    {
                        telegramiId = Convert.ToInt32(dup);
                        status = "Duplikat";
                        rez.Duplikata++;
                    }
                    else
                    {
                        using (var cmd = Db.Command(c, tx, @"DECLARE @id int;
                            SELECT @id = ISNULL(MAX(ID), 0) + 1 FROM dbo.Telegrami WITH (UPDLOCK, HOLDLOCK);
                            INSERT dbo.Telegrami (ID, BrojTelegrama, PrugaID, OdStanice, DoStanice, Kolosek, VaziOd, VaziDo, TrajeOd, TrajeDo,
                                                  Napomena, Aktivan, PDF, NarocitaPosiljka, DatumTelegrama, Vrsta, MailLogID, UnosAutomatski)
                            VALUES (@id, @b, @p, @od, @do, @k, @vod, @vdo, '00:00:00', '23:59:59', @n, 1, N'', @np, @dt, @vr, @m, 1);
                            SELECT @id;",
                            "@b", r["BrojTelegrama"], "@p", r["PrugaID"], "@od", r["OdStanice"], "@do", r["DoStanice"],
                            "@k", Skrati(Db.Text(r, "Kolosek"), 50), "@vod", vod, "@vdo", vdo, "@n", Skrati(Db.Text(r, "Napomena"), 250),
                            "@np", !r.IsNull("NarocitaPosiljka") && Convert.ToBoolean(r["NarocitaPosiljka"]), "@dt", r["DatumTelegrama"],
                            "@vr", r.IsNull("Vrsta") ? "Zatvor" : Db.Text(r, "Vrsta"), "@m", mailLogId))
                            telegramiId = Convert.ToInt32(cmd.ExecuteScalar());
                        status = "Potvrdjeno";
                        rez.Upisano++;
                    }

                    using (var cmd = Db.Command(c, tx, @"UPDATE dbo.TelegramiMailStavke SET BrojTelegrama = @b, DatumTelegrama = @dt, PrugaID = @p,
                            OdStanice = @od, DoStanice = @do, Kolosek = @k, VaziOd = @vod, VaziDo = @vdo, Napomena = @n,
                            Status = @s, TelegramiID = @tid, Potvrdio = @kor, Potvrdjeno = GETDATE()
                        WHERE ID = @id",
                        "@b", r["BrojTelegrama"], "@dt", r["DatumTelegrama"], "@p", r["PrugaID"], "@od", r["OdStanice"], "@do", r["DoStanice"],
                        "@k", Skrati(Db.Text(r, "Kolosek"), 50), "@vod", vod, "@vdo", vdo, "@n", Skrati(Db.Text(r, "Napomena"), 250),
                        "@s", status, "@tid", telegramiId, "@kor", korisnik, "@id", id))
                        cmd.ExecuteNonQuery();

                    if (zapamtiNazive)
                    {
                        var p0 = pre.Rows[0];
                        var prugaID = Convert.ToInt32(r["PrugaID"]);
                        if (p0.IsNull("PrugaID")) Alias(c, tx, "Pruga", Db.Text(r, "PrugaTekst"), null, prugaID, korisnik);
                        if (p0.IsNull("OdStanice")) AliasStanice(c, tx, Db.Text(r, "OdStaniceTekst"), prugaID, Convert.ToInt32(r["OdStanice"]), korisnik);
                        if (p0.IsNull("DoStanice")) AliasStanice(c, tx, Db.Text(r, "DoStaniceTekst"), prugaID, Convert.ToInt32(r["DoStanice"]), korisnik);
                    }
                }
                ZavrsiAkoJeGotovo(c, tx, mailLogId, korisnik, "Potvrdjeno");
                tx.Commit();
            }
            return rez;
        }

        internal static void Odbaci(IEnumerable<int> stavkeIds, int mailLogId, string korisnik)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                foreach (var id in stavkeIds)
                    using (var cmd = Db.Command(c, tx, "UPDATE dbo.TelegramiMailStavke SET Status = N'Odbaceno', Potvrdio = @k, Potvrdjeno = GETDATE() WHERE ID = @id AND Status = N'NeedsReview'",
                        "@k", korisnik, "@id", id))
                        cmd.ExecuteNonQuery();
                ZavrsiAkoJeGotovo(c, tx, mailLogId, korisnik, "Potvrdjeno");
                tx.Commit();
            }
        }

        /// <summary>Poruka nije telegram za upis (reklama, prijava stanice, ...): arhivira se, preostale stavke se odbacuju.</summary>
        internal static void Arhiviraj(int mailLogId, string korisnik)
        {
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                using (var cmd = Db.Command(c, tx, "UPDATE dbo.TelegramiMailStavke SET Status = N'Odbaceno', Potvrdio = @k, Potvrdjeno = GETDATE() WHERE MailLogID = @m AND Status = N'NeedsReview'",
                    "@k", korisnik, "@m", mailLogId))
                    cmd.ExecuteNonQuery();
                using (var cmd = Db.Command(c, tx, "UPDATE dbo.TelegramiMailLog SET Status = N'Arhivirano', Pregledao = @k, Pregledano = GETDATE() WHERE ID = @m",
                    "@k", korisnik, "@m", mailLogId))
                    cmd.ExecuteNonQuery();
                tx.Commit();
            }
        }

        private static void ZavrsiAkoJeGotovo(SqlConnection c, SqlTransaction tx, int mailLogId, string korisnik, string status)
        {
            using (var cmd = Db.Command(c, tx, @"IF NOT EXISTS (SELECT 1 FROM dbo.TelegramiMailStavke WHERE MailLogID = @m AND Status = N'NeedsReview')
                    UPDATE dbo.TelegramiMailLog SET Status = @s, Pregledao = @k, Pregledano = GETDATE(),
                        BrojRedova = (SELECT COUNT(*) FROM dbo.TelegramiMailStavke WHERE MailLogID = @m AND Status IN (N'Potvrdjeno', N'Upisano'))
                    WHERE ID = @m AND Status = N'NeedsReview'",
                "@m", mailLogId, "@s", status, "@k", korisnik))
                cmd.ExecuteNonQuery();
        }

        private static void AliasStanice(SqlConnection c, SqlTransaction tx, string tekst, int prugaID, int stanicaID, string korisnik)
        {
            // "#GRANICA" zavisi od pruge; ostali nazivi važe na svim prugama (agent prvo traži alias za prugu, pa opšti)
            if (tekst == "#GRANICA") Alias(c, tx, "Stanica", tekst, prugaID, stanicaID, korisnik);
            else Alias(c, tx, "Stanica", tekst, null, stanicaID, korisnik);
        }

        private static void Alias(SqlConnection c, SqlTransaction tx, string tip, string tekst, int? prugaID, int refID, string korisnik)
        {
            var kljuc = tekst == "#GRANICA" ? "#GRANICA" : TekstUtil.KljucNaziva(tekst);
            if (string.IsNullOrWhiteSpace(kljuc)) return;
            using (var cmd = Db.Command(c, tx, @"IF EXISTS (SELECT 1 FROM dbo.TelegramiAlias WHERE Tip = @t AND Tekst = @x AND ((@p IS NULL AND PrugaID IS NULL) OR PrugaID = @p))
                    UPDATE dbo.TelegramiAlias SET RefID = @r, Uneo = @k, Uneto = GETDATE() WHERE Tip = @t AND Tekst = @x AND ((@p IS NULL AND PrugaID IS NULL) OR PrugaID = @p)
                ELSE
                    INSERT dbo.TelegramiAlias (Tip, Tekst, PrugaID, RefID, Uneo) VALUES (@t, @x, @p, @r, @k)",
                "@t", tip, "@x", kljuc.Length > 300 ? kljuc.Substring(0, 300) : kljuc, "@p", prugaID, "@r", refID, "@k", korisnik))
                cmd.ExecuteNonQuery();
        }

        private static DateTime BezSekundi(DateTime d) { return new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0); }

        private static string Skrati(string s, int max)
        {
            if (s == null || s.Length <= max) return s;
            return TekstUtil.SkratiNaRec(s, max);
        }
    }
}
