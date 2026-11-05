using System;
using System.Data;
using System.Data.SqlClient;
using Saobracaj.Agenti_jobs;

namespace Saobracaj.Planiranje.Podaci
{
    internal static class PlaniranjeDb
    {
        internal const string Sema = "planiranje";
        internal const string PorukaKonkurentnost = "Podatke je u međuvremenu promenio drugi korisnik. Osvežite prikaz.";

        internal static string Korisnik
        {
            get
            {
                return string.IsNullOrWhiteSpace(Saobracaj.Sifarnici.frmLogovanje.user)
                    ? Environment.UserName
                    : Saobracaj.Sifarnici.frmLogovanje.user.Trim();
            }
        }

        // UPDATE tabele sa RowVer: nula promenjenih redova znači da je zapis u međuvremenu menjan.
        internal static void IzvrsiSaRowVer(SqlConnection c, SqlTransaction tx, string sql, params object[] args)
        {
            using (var cmd = Db.Command(c, tx, sql, args))
                if (cmd.ExecuteNonQuery() == 0)
                    throw new DBConcurrencyException(PorukaKonkurentnost);
        }

        internal static object Vrednost(object v)
        {
            return v ?? DBNull.Value;
        }
    }
}
