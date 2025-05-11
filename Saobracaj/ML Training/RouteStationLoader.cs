using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Saobracaj.ML_Training
{
    public static class RouteStationLoader
    {
        public static List<int> LoadStations(string connectionString, int trasa)
        {
            var result = new List<int>();
            var conn = new SqlConnection(connectionString);
            conn.Open();

            var cmd = new SqlCommand("SELECT IDStanice FROM TrasaStanice WHERE IDTrase = @Trasa ORDER BY RB", conn);
            cmd.Parameters.AddWithValue("@Trasa", trasa);

            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(Convert.ToInt32(reader["IDStanice"]));
            }

            return result;
        }

        public static Dictionary<int, int> LoadStationOrder(string connectionString, int trasa)
        {
            var dict = new Dictionary<int, int>();
            var conn = new SqlConnection(connectionString);
            conn.Open();

            var cmd = new SqlCommand("SELECT IDStanice, RB FROM TrasaStanice WHERE IDTrase = @Trasa", conn);
            cmd.Parameters.AddWithValue("@Trasa", trasa);

            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int stanica = Convert.ToInt32(reader["IDStanice"]);
                int redosled = Convert.ToInt32(reader["RB"]);
                dict[stanica] = redosled;
            }

            return dict;
        }

        public static Dictionary<int, string> LoadStationNames(string connectionString)
        {
            var dict = new Dictionary<int, string>();
            var conn = new SqlConnection(connectionString);
            conn.Open();

            var cmd = new SqlCommand("SELECT ID, RTrim(Opis) as Opis FROM stanice", conn);
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int id = Convert.ToInt32(reader["ID"]);
                string name = reader["Opis"].ToString();
                dict[id] = name;
            }

            return dict;
        }
    }
}
