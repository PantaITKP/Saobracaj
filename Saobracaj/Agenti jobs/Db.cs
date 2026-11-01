using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Saobracaj.Agenti_jobs
{
    public static class OperationalSession
    {
        // Set these once after your application's login, before opening either form.
        public static string ConnectionString { get; set; }
        public static string UserName { get; set; } = Environment.UserName;
    }

    internal static class Db
    {
        internal static SqlConnection Open()
        {
            var cs = OperationalSession.ConnectionString;
            if (string.IsNullOrWhiteSpace(cs))
            {
                var settings = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"] ?? ConfigurationManager.ConnectionStrings.Cast<ConnectionStringSettings>().FirstOrDefault(s => s.Name.EndsWith(".NedraConnectionString", StringComparison.OrdinalIgnoreCase) || s.Name.Equals("NedraConnectionString", StringComparison.OrdinalIgnoreCase));
                if (settings == null)
                    throw new InvalidOperationException("NedraConnectionString nije podešen u App.config.");
                cs = settings.ConnectionString;
            }

            var c = new SqlConnection(cs);
            try
            {
                c.Open();
                // Eksplicitno podesiti svaku dobijenu konekciju, ukljucujuci pooled konekcije.
                // Potrebno za XML .value(), indeksirane prikaze i racunate/filtrirane indekse.
                using (var settings = new SqlCommand(@"SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;", c))
                {
                    settings.CommandTimeout = 60;
                    settings.ExecuteNonQuery();
                }
                return c;
            }
            catch
            {
                c.Dispose();
                throw;
            }
        }

        internal static SqlCommand Command(SqlConnection c, SqlTransaction tx, string sql, params object[] args)
        {
            var cmd = new SqlCommand(sql, c, tx)
            {
                CommandTimeout = 60
            };
            for (int i = 0; i < args.Length; i += 2)
            {
                var value = args[i + 1] ?? DBNull.Value;
                var par = cmd.Parameters.AddWithValue((string)args[i], value);
                if (value is string)
                {
                    par.SqlDbType = SqlDbType.NVarChar;
                    par.Size = -1;
                }
            }

            return cmd;
        }

        internal static DataTable Table(SqlConnection c, SqlTransaction tx, string sql, params object[] args)
        {
            using (var cmd = Command(c, tx, sql, args))
            using (var a = new SqlDataAdapter(cmd))
            {
                var t = new DataTable();
                a.Fill(t);
                return t;
            }
        }

        internal static void Lock(SqlConnection c, SqlTransaction tx, string resource)
        {
            using (var cmd = Command(c, tx, @"DECLARE @r int;EXEC @r=sys.sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;IF @r<0 RAISERROR(N'Nije moguće dobiti zaključavanje. Pokušajte ponovo.',16,1);", "@resource", resource))
                cmd.ExecuteNonQuery();
        }

        internal static long ScalarId(SqlCommand cmd)
        {
            return Convert.ToInt64(cmd.ExecuteScalar());
        }

        internal static string Text(DataRow r, string col)
        {
            return r.IsNull(col) ? "" : Convert.ToString(r[col]);
        }
    }

    internal static class OperationalDiagnostics
    {
        private static readonly object Sync = new object();
        internal static string LogPath
        {
            get { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Saobracaj", "AgentiJobs-dijagnostika.log"); }
        }
        internal static void Write(string message)
        {
            try
            {
                lock (Sync)
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(LogPath));
                    System.IO.File.AppendAllText(LogPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + Environment.NewLine);
                }
            }
            catch { /* Diagnostics must not prevent normal application use. */ }
        }
        internal static T Measure<T>(string operation, Func<T> action)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try { return action(); }
            finally { Write(operation + ": " + watch.ElapsedMilliseconds + " ms"); }
        }
    }
}
