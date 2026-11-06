using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Saobracaj.Agenti_jobs;

namespace Saobracaj.Telegrami
{
    /// <summary>
    /// Čitanje telegrama za Planer (tab Telegrami) i TelegramNotifier. Aplikacija samo čita;
    /// u Telegrami upisuje agent Saobracaj.Agent.TelegramiImport na serveru.
    /// Radi i pre migracije 001 (kolone DatumTelegrama/MailLogID/UnosAutomatski tada ne postoje).
    /// </summary>
    internal static class TelegramiRepository
    {
        private static bool? imaMigraciju;

        internal static bool ImaMigraciju(SqlConnection c)
        {
            if (imaMigraciju == null)
                using (var cmd = Db.Command(c, null, "SELECT CASE WHEN COL_LENGTH(N'dbo.Telegrami', N'UnosAutomatski') IS NULL OR OBJECT_ID(N'dbo.TelegramiMailLog', N'U') IS NULL THEN 0 ELSE 1 END"))
                    imaMigraciju = Convert.ToInt32(cmd.ExecuteScalar()) == 1;
            return imaMigraciju.Value;
        }

        private static string Kolone(bool migracija)
        {
            return @"t.ID, t.BrojTelegrama, " + (migracija ? "t.DatumTelegrama, t.MailLogID, t.Vrsta," : "CAST(NULL AS date) AS DatumTelegrama, CAST(NULL AS int) AS MailLogID, CAST(NULL AS nvarchar(30)) AS Vrsta,") + @"
                RTRIM(p.Opis) AS Pruga, RTRIM(a.Opis) AS OdStanice, RTRIM(b.Opis) AS DoStanice, RTRIM(t.Kolosek) AS Kolosek,
                t.VaziOd, t.VaziDo, t.Napomena, t.NarocitaPosiljka, RTRIM(t.PDF) AS PDF";
        }

        private const string Join = @" FROM dbo.Telegrami t
                LEFT JOIN dbo.Pruga p ON p.ID = t.PrugaID
                LEFT JOIN dbo.stanice a ON a.ID = t.OdStanice
                LEFT JOIN dbo.stanice b ON b.ID = t.DoStanice ";

        /// <summary>Aktivni telegrami: Aktivan = 1 i period nije istekao.</summary>
        internal static DataTable Aktivni()
        {
            using (var c = Db.Open())
                return Db.Table(c, null, "SELECT " + Kolone(ImaMigraciju(c)) + Join +
                    "WHERE t.Aktivan = 1 AND (t.VaziDo IS NULL OR t.VaziDo >= GETDATE()) ORDER BY t.VaziOd, t.BrojTelegrama, t.ID");
        }

        internal static Tuple<int, int> PoslednjiID()
        {
            using (var c = Db.Open())
            {
                int t = Convert.ToInt32(Db.Command(c, null, "SELECT ISNULL(MAX(ID), 0) FROM dbo.Telegrami").ExecuteScalar());
                int l = ImaMigraciju(c) ? Convert.ToInt32(Db.Command(c, null, "SELECT ISNULL(MAX(ID), 0) FROM dbo.TelegramiMailLog").ExecuteScalar()) : 0;
                return Tuple.Create(t, l);
            }
        }

        /// <summary>Novi automatski upisani redovi i nova otkazivanja posle zadatih ID-jeva.</summary>
        internal static List<TelegramInfo> Novi(int posleTelegramID, int posleMailLogID, out int noviTelegramID, out int noviMailLogID)
        {
            noviTelegramID = posleTelegramID;
            noviMailLogID = posleMailLogID;
            var rez = new List<TelegramInfo>();
            using (var c = Db.Open())
            {
                if (!ImaMigraciju(c)) return rez;

                var redovi = Db.Table(c, null, "SELECT " + Kolone(true) + ", l.OznakaTelegrama" + Join +
                    "LEFT JOIN dbo.TelegramiMailLog l ON l.ID = t.MailLogID WHERE t.ID > @id AND t.UnosAutomatski = 1 ORDER BY t.ID", "@id", posleTelegramID);
                foreach (var g in redovi.Rows.Cast<DataRow>().GroupBy(r => r.IsNull("MailLogID") ? -Convert.ToInt32(r["ID"]) : Convert.ToInt32(r["MailLogID"])))
                {
                    var prvi = g.First();
                    rez.Add(new TelegramInfo
                    {
                        MailLogID = prvi.IsNull("MailLogID") ? (int?)null : Convert.ToInt32(prvi["MailLogID"]),
                        Broj = prvi.IsNull("BrojTelegrama") ? 0 : Convert.ToInt32(prvi["BrojTelegrama"]),
                        Oznaka = Db.Text(prvi, "OznakaTelegrama"),
                        Datum = prvi.IsNull("DatumTelegrama") ? (DateTime?)null : Convert.ToDateTime(prvi["DatumTelegrama"]),
                        Vrsta = Db.Text(prvi, "Vrsta"),
                        Pruga = Db.Text(prvi, "Pruga"),
                        Redovi = g.Select(TelegramRed.Iz).ToList()
                    });
                    noviTelegramID = Math.Max(noviTelegramID, g.Max(r => Convert.ToInt32(r["ID"])));
                }

                var otk = Db.Table(c, null, @"SELECT ID, BrojTelegrama, OznakaTelegrama, DatumTelegrama FROM dbo.TelegramiMailLog
                    WHERE ID > @id AND Status = N'Otkazano' ORDER BY ID", "@id", posleMailLogID);
                foreach (DataRow l in otk.Rows)
                {
                    var oznaka = Db.Text(l, "OznakaTelegrama");
                    var info = new TelegramInfo
                    {
                        MailLogID = Convert.ToInt32(l["ID"]),
                        Broj = l.IsNull("BrojTelegrama") ? 0 : Convert.ToInt32(l["BrojTelegrama"]),
                        Oznaka = oznaka,
                        Datum = l.IsNull("DatumTelegrama") ? (DateTime?)null : Convert.ToDateTime(l["DatumTelegrama"]),
                        Vrsta = "Otkazivanje",
                        Otkazivanje = true
                    };
                    // Redovi koje je ovaj telegram ugasio: oznaka "... TEL. BR. 10F " na početku napomene
                    if (!string.IsNullOrEmpty(oznaka))
                        info.Ugaseni = Db.Table(c, null, "SELECT " + Kolone(true) + Join +
                            "WHERE t.Aktivan = 0 AND t.Napomena LIKE N'%TEL. BR. ' + @o + N' %' AND (@d IS NULL OR t.VaziDo >= DATEADD(day, -1, @d)) ORDER BY t.ID",
                            "@o", oznaka, "@d", info.Datum).Rows.Cast<DataRow>().Select(TelegramRed.Iz).ToList();
                    rez.Add(info);
                }
                var maxLog = Db.Command(c, null, "SELECT ISNULL(MAX(ID), 0) FROM dbo.TelegramiMailLog").ExecuteScalar();
                noviMailLogID = Math.Max(posleMailLogID, Convert.ToInt32(maxLog));
            }
            return rez;
        }

        /// <summary>Snima prilog u %TEMP% i otvara ga podrazumevanim programom.</summary>
        internal static void OtvoriPrilog(int mailLogId, Tuple<string, byte[]> prilog)
        {
            var ime = string.Concat(prilog.Item1.Select(ch => System.IO.Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
            var putanja = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Telegram_" + mailLogId + "_" + ime);
            System.IO.File.WriteAllBytes(putanja, prilog.Item2);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(putanja) { UseShellExecute = true });
        }

        /// <summary>Originalni PDF telegrama (prvi PDF prilog poruke).</summary>
        internal static Tuple<string, byte[]> Pdf(int mailLogId)
        {
            using (var c = Db.Open())
            {
                var t = Db.Table(c, null, @"SELECT TOP 1 NazivFajla, Sadrzaj FROM dbo.TelegramiPrilozi
                    WHERE MailLogID = @m ORDER BY CASE WHEN NazivFajla LIKE N'%.pdf' THEN 0 ELSE 1 END, ID", "@m", mailLogId);
                if (t.Rows.Count == 0) return null;
                return Tuple.Create(Db.Text(t.Rows[0], "NazivFajla"), (byte[])t.Rows[0]["Sadrzaj"]);
            }
        }
    }

    /// <summary>Podaci o primljenom telegramu; argument događaja TelegramNotifier.TelegramPrimljen.</summary>
    public sealed class TelegramInfo
    {
        public int? MailLogID { get; set; }
        public int Broj { get; set; }
        public string Oznaka { get; set; }
        public DateTime? Datum { get; set; }
        public string Vrsta { get; set; }
        public string Pruga { get; set; }
        public bool Otkazivanje { get; set; }
        public List<TelegramRed> Redovi { get; set; } = new List<TelegramRed>();
        public List<TelegramRed> Ugaseni { get; set; } = new List<TelegramRed>();

        public string Naziv
        {
            get { return (string.IsNullOrEmpty(Oznaka) ? Broj.ToString() : Oznaka) + (Datum.HasValue ? " od " + Datum.Value.ToString("dd.MM.yyyy") : ""); }
        }
    }

    public sealed class TelegramRed
    {
        public int ID { get; set; }
        public int Broj { get; set; }
        public DateTime? DatumTelegrama { get; set; }
        public string Pruga { get; set; }
        public string Od { get; set; }
        public string Do { get; set; }
        public string Kolosek { get; set; }
        public DateTime VaziOd { get; set; }
        public DateTime VaziDo { get; set; }
        public string Napomena { get; set; }

        internal static TelegramRed Iz(DataRow r)
        {
            return new TelegramRed
            {
                ID = Convert.ToInt32(r["ID"]),
                Broj = r.IsNull("BrojTelegrama") ? 0 : Convert.ToInt32(r["BrojTelegrama"]),
                DatumTelegrama = r.IsNull("DatumTelegrama") ? (DateTime?)null : Convert.ToDateTime(r["DatumTelegrama"]),
                Pruga = Db.Text(r, "Pruga"),
                Od = Db.Text(r, "OdStanice"),
                Do = Db.Text(r, "DoStanice"),
                Kolosek = Db.Text(r, "Kolosek"),
                VaziOd = r.IsNull("VaziOd") ? DateTime.MinValue : Convert.ToDateTime(r["VaziOd"]),
                VaziDo = r.IsNull("VaziDo") ? DateTime.MaxValue : Convert.ToDateTime(r["VaziDo"]),
                Napomena = Db.Text(r, "Napomena")
            };
        }

        /// <summary>Dnevni interval iz vremena VaziOd/VaziDo (TrajeOd/TrajeDo su uvek 00:00-23:59).</summary>
        public static string Dnevno(DateTime vaziOd, DateTime vaziDo)
        {
            var od = vaziOd.TimeOfDay;
            var @do = vaziDo.TimeOfDay;
            if (od == TimeSpan.Zero && (@do == TimeSpan.Zero || @do >= new TimeSpan(23, 59, 0))) return "ceo dan";
            return vaziOd.ToString("HH:mm") + "–" + vaziDo.ToString("HH:mm");
        }

        /// <summary>Važi li red u datom trenutku (dan u periodu i vreme u dnevnom intervalu; noćni interval prelazi ponoć).</summary>
        public static bool VaziU(DateTime vaziOd, DateTime vaziDo, DateTime t)
        {
            if (t < vaziOd || t > vaziDo) return false;
            var od = vaziOd.TimeOfDay;
            var @do = vaziDo.TimeOfDay;
            if (Dnevno(vaziOd, vaziDo) == "ceo dan") return true;
            var x = t.TimeOfDay;
            return od <= @do ? x >= od && x <= @do : x >= od || x <= @do;
        }

        public string Opis()
        {
            var rel = Od == Do || string.IsNullOrEmpty(Do) ? Od : Od + " – " + Do;
            if (!string.IsNullOrEmpty(Kolosek) && Kolosek != "pruga") rel += " (" + (Kolosek.All(ch => char.IsDigit(ch) || ch == ',' || ch == '-') ? "kol. " + Kolosek : Kolosek) + ")";
            string period;
            var dnevno = Dnevno(VaziOd, VaziDo);
            var nocni = VaziDo.TimeOfDay < VaziOd.TimeOfDay && dnevno != "ceo dan";
            var poslednjiDan = nocni ? VaziDo.Date.AddDays(-1) : VaziDo.Date;
            if (VaziOd.Date == poslednjiDan)
                period = nocni ? VaziOd.ToString("dd") + "/" + VaziDo.ToString("dd.MM.yyyy") + " " + dnevno : VaziOd.ToString("dd.MM.yyyy") + " " + dnevno;
            else if (nocni)
                period = "noći " + VaziOd.ToString("dd") + "/" + VaziOd.AddDays(1).ToString("dd.MM.") + "–" + poslednjiDan.ToString("dd") + "/" + VaziDo.ToString("dd.MM.yyyy") + ", " + dnevno;
            else
                period = VaziOd.ToString("dd.MM.") + "–" + poslednjiDan.ToString("dd.MM.yyyy") + ", " + (dnevno == "ceo dan" ? "ceo dan" : "svaki dan " + dnevno);
            return rel + ", " + period;
        }
    }
}
