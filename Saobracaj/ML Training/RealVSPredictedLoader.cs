using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saobracaj.ML_Training
{
    public static class RealVsPredictedLoader
    {
        public static List<(int Stanica, TimeSpan Real, TimeSpan Predicted, int DifferenceSec)> LoadRealVsPredicted(string connectionString, int brojRN)
        {
            var result = new List<(int, TimeSpan, TimeSpan, int)>();

            var conn = new SqlConnection(connectionString);
            conn.Open();

            var cmd = new SqlCommand(@"
            WITH StartReal AS (
                SELECT MIN(Datum) AS StartDatum
                FROM AIPutniList
                WHERE BrojRN = @BrojRN
            ),
            StartPredicted AS (
                SELECT MIN(Datum) AS StartDatum
                FROM AIUsvojenaVremenaTrase
                WHERE RN = @BrojRN
            )
            SELECT 
                r.Stanica,
                DATEDIFF(SECOND, sr.StartDatum, r.Datum) AS RealSeconds,
                DATEDIFF(SECOND, sp.StartDatum, p.Datum) AS PredictedSeconds
            FROM AIPutniList r
            JOIN AIUsvojenaVremenaTrase p ON r.BrojRN = p.RN AND r.Stanica = p.Stanica
            CROSS JOIN StartReal sr
            CROSS JOIN StartPredicted sp
            WHERE r.BrojRN = @BrojRN
            ORDER BY r.Datum", conn);

            cmd.Parameters.AddWithValue("@BrojRN", brojRN);

            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int stanica = Convert.ToInt32(reader["Stanica"]);
                int realSec = Convert.ToInt32(reader["RealSeconds"]);
                int predSec = Convert.ToInt32(reader["PredictedSeconds"]);
                int diff = predSec - realSec;

                result.Add((stanica, TimeSpan.FromSeconds(realSec), TimeSpan.FromSeconds(predSec), diff));
            }

            return result;
        }
    }
}
