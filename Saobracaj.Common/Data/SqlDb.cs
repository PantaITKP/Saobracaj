using System;
using System.Data;
using System.Data.SqlClient;

namespace Saobracaj.Common.Data
{
    /// <summary>
    /// Pristup bazi po istom obrascu kao "Agenti jobs/Db.cs" u aplikaciji:
    /// parametri kao naizmenični niz (ime, vrednost), string → nvarchar(max), timeout 60 s.
    /// </summary>
    public static class SqlDb
    {
        public static SqlConnection Open(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Connection string nije podešen.");
            var c = new SqlConnection(connectionString);
            try
            {
                c.Open();
                return c;
            }
            catch
            {
                c.Dispose();
                throw;
            }
        }

        public static SqlCommand Command(SqlConnection c, SqlTransaction tx, string sql, params object[] args)
        {
            var cmd = new SqlCommand(sql, c, tx) { CommandTimeout = 60 };
            for (int i = 0; i < args.Length; i += 2)
            {
                var value = args[i + 1] ?? DBNull.Value;
                var par = cmd.Parameters.AddWithValue((string)args[i], value);
                if (value is string)
                {
                    par.SqlDbType = SqlDbType.NVarChar;
                    par.Size = -1;
                }
                else if (value is byte[])
                {
                    par.SqlDbType = SqlDbType.VarBinary;
                    par.Size = -1;
                }
            }
            return cmd;
        }

        public static DataTable Table(SqlConnection c, SqlTransaction tx, string sql, params object[] args)
        {
            using (var cmd = Command(c, tx, sql, args))
            using (var a = new SqlDataAdapter(cmd))
            {
                var t = new DataTable();
                a.Fill(t);
                return t;
            }
        }

        public static int Execute(SqlConnection c, SqlTransaction tx, string sql, params object[] args)
        {
            using (var cmd = Command(c, tx, sql, args))
                return cmd.ExecuteNonQuery();
        }

        public static object Scalar(SqlConnection c, SqlTransaction tx, string sql, params object[] args)
        {
            using (var cmd = Command(c, tx, sql, args))
            {
                var v = cmd.ExecuteScalar();
                return v == DBNull.Value ? null : v;
            }
        }

        /// <summary>
        /// sp_getapplock vezan za sesiju. Vraća true ako je zaključavanje dobijeno.
        /// timeoutMs = 0: ne čeka (drugi proces odmah izlazi).
        /// </summary>
        public static bool TryLockSession(SqlConnection c, string resource, int timeoutMs = 0)
        {
            using (var cmd = Command(c, null, "DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource=@res, @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=@t; SELECT @r;",
                "@res", resource, "@t", timeoutMs))
            {
                return Convert.ToInt32(cmd.ExecuteScalar()) >= 0;
            }
        }

        /// <summary>sp_getapplock vezan za transakciju; baca grešku ako ne uspe.</summary>
        public static void LockTransaction(SqlConnection c, SqlTransaction tx, string resource, int timeoutMs = 15000)
        {
            Execute(c, tx, "DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource=@res, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=@t; IF @r < 0 RAISERROR(N'Nije moguće dobiti zaključavanje.',16,1);",
                "@res", resource, "@t", timeoutMs);
        }

        public static string Text(DataRow r, string col)
        {
            return r.IsNull(col) ? "" : Convert.ToString(r[col]);
        }

        public static int? Int(DataRow r, string col)
        {
            return r.IsNull(col) ? (int?)null : Convert.ToInt32(r[col]);
        }

        public static DateTime? Date(DataRow r, string col)
        {
            return r.IsNull(col) ? (DateTime?)null : Convert.ToDateTime(r[col]);
        }
    }
}
