using System.Data;
using System.Data.SqlClient;
using Saobracaj.Agenti_jobs;

namespace Saobracaj.Planiranje.Podaci
{
    internal static class DogadjajRepo
    {
        // Poziva se u transakciji promene posla, da događaj i promena uspeju ili padnu zajedno.
        internal static void Upisi(SqlConnection c, SqlTransaction tx, int posaoId, string tip, string opis, string json = null)
        {
            using (var cmd = Db.Command(c, tx,
                "INSERT planiranje.OP_PosaoDogadjaj (PosaoID, Tip, Opis, Korisnik, PodaciJson) VALUES (@p, @t, @o, @k, @j)",
                "@p", posaoId, "@t", tip, "@o", opis, "@k", PlaniranjeDb.Korisnik, "@j", (object)json))
                cmd.ExecuteNonQuery();
        }

        internal static DataTable ZaPosao(int posaoId)
        {
            using (var c = Db.Open())
                return Db.Table(c, null,
                    "SELECT ID, Vreme, Tip, Opis, Korisnik FROM planiranje.OP_PosaoDogadjaj WHERE PosaoID = @p ORDER BY Vreme DESC, ID DESC",
                    "@p", posaoId);
        }
    }
}
