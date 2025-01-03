using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using Syncfusion.Windows.Forms.Grid.Grouping;

namespace Saobracaj.Dokumenta
{
    internal class InsertWebMap
    {
        string connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"].ConnectionString;
        public void InsertAPI(int IDRecords, string IDVehicles, DateTime RecordTime, decimal Latitude, decimal Longitude, decimal Altitude, decimal Direction, int SV, decimal MainVoltage, decimal SpeedAPI,
    DateTime DatumUpisa, string SS)
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = transaction;
                            cmd.CommandText = "InsertLocoTrackApp";
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.Add(new SqlParameter("@IDRecords", SqlDbType.Int) { Value = IDRecords });
                            cmd.Parameters.Add(new SqlParameter("@IDVehicles", SqlDbType.NVarChar, 20) { Value = IDVehicles });
                            cmd.Parameters.Add(new SqlParameter("@RecordTime", SqlDbType.DateTime) { Value = RecordTime });
                            cmd.Parameters.Add(new SqlParameter("@Latitude", SqlDbType.Decimal) { Value = Latitude });
                            cmd.Parameters.Add(new SqlParameter("@Longitude", SqlDbType.Decimal) { Value = Longitude });
                            cmd.Parameters.Add(new SqlParameter("@Altitude", SqlDbType.Decimal) { Value = Altitude });
                            cmd.Parameters.Add(new SqlParameter("@Direction", SqlDbType.Decimal) { Value = Direction });
                            cmd.Parameters.Add(new SqlParameter("@SV", SqlDbType.Int) { Value = SV });
                            cmd.Parameters.Add(new SqlParameter("@MainVoltage", SqlDbType.Decimal) { Value = MainVoltage });
                            cmd.Parameters.Add(new SqlParameter("@SpeedAPI", SqlDbType.Decimal) { Value = SpeedAPI });
                            cmd.Parameters.Add(new SqlParameter("@DatumUpisa", SqlDbType.DateTime) { Value = DatumUpisa });
                            cmd.Parameters.Add(new SqlParameter("@SS", SqlDbType.NVarChar, 250) { Value = SS });

                            cmd.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Neuspešan upis u bazu", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        MessageBox.Show(ex.ToString());
                    }
                }
                conn.Close();
            }
        }
    }
}
