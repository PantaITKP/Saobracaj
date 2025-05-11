using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saobracaj.ML_Training
{
    public static class DataLoader
    {
        public static List<TrainArrivalData> Load(string connectionString)
        {
            var data=new List<TrainArrivalData>();
            var conn=new SqlConnection(connectionString);
            conn.Open();

            var cmd = new SqlCommand("SELECT Trasa, Stanica, StartHour, Redosled, ArrivalTimeSeconds FROM AIPutniListMLData", conn);
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                data.Add(new TrainArrivalData
                {
                    Trasa = Convert.ToSingle(reader["Trasa"]),
                    Stanica = Convert.ToSingle(reader["Stanica"]),
                    StartHour = Convert.ToSingle(reader["StartHour"]),
                    Redosled = Convert.ToSingle(reader["Redosled"]),
                    ArrivalTimeSeconds = Convert.ToSingle(reader["ArrivalTimeSeconds"])
                }) ;
            }
            return data;
        }
    }
}
