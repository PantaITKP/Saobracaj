using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Saobracaj.Sifarnici
{
    class InsertLokomotivaDokumenti
    {
        string connect = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;

        public void InsLokDok(string Lokomotiva, string Opis, string Kreirao, string Putanja)
        {
            SqlConnection conn = new SqlConnection(connect);
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "InsertLokomotivaDokumenti";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter lok = new SqlParameter();
            lok.ParameterName = "@Lokomotiva";
            lok.SqlDbType = SqlDbType.NVarChar;
            lok.Size = 50;
            lok.Direction = ParameterDirection.Input;
            lok.Value = Lokomotiva;
            cmd.Parameters.Add(lok);

            SqlParameter opis = new SqlParameter();
            opis.ParameterName = "@Opis";
            opis.SqlDbType = SqlDbType.NVarChar;
            opis.Size = 100;
            opis.Direction = ParameterDirection.Input;
            opis.Value = Opis;
            cmd.Parameters.Add(opis);

            SqlParameter kreirao = new SqlParameter();
            kreirao.ParameterName = "@Kreirao";
            kreirao.SqlDbType = SqlDbType.NVarChar;
            kreirao.Size = 50;
            kreirao.Direction = ParameterDirection.Input;
            kreirao.Value = Kreirao;
            cmd.Parameters.Add(kreirao);

            SqlParameter putanja = new SqlParameter();
            putanja.ParameterName = "@Putanja";
            putanja.SqlDbType = SqlDbType.NVarChar;
            putanja.Size = 500;
            putanja.Direction = ParameterDirection.Input;
            putanja.Value = Putanja;
            cmd.Parameters.Add(putanja);

            conn.Open();
            SqlTransaction tran = conn.BeginTransaction();
            cmd.Transaction = tran;
            bool error = true;
            try
            {
                cmd.ExecuteNonQuery();
                tran.Commit();
                tran = conn.BeginTransaction();
                cmd.Transaction = tran;
            }

            catch (SqlException ex)
            {
                MessageBox.Show(ex.ToString());
                throw new Exception("Neuspešan upis Telegrama brojeva");
            }

            finally
            {
                if (!error)
                {
                    tran.Commit();
                    MessageBox.Show("Unos NHM broja je uspešno završena", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                conn.Close();

                if (error)
                {
                    // Nedra.DataSet1TableAdapters.QueriesTableAdapter adapter = new Nedra.DataSet1TableAdapters.QueriesTableAdapter();
                }
            }

        }
        public void UpdLokDok(int ID, string Lokomotiva, string Opis, string Kreirao, string Putanja)
        {
            SqlConnection conn = new SqlConnection(connect);
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "UpdateLokomotivaDokumenti";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter id = new SqlParameter();
            id.ParameterName = "@ID";
            id.SqlDbType = SqlDbType.Int;
            id.Direction = ParameterDirection.Input;
            id.Value = ID;
            cmd.Parameters.Add(id);

            SqlParameter lok = new SqlParameter();
            lok.ParameterName = "@Lokomotiva";
            lok.SqlDbType = SqlDbType.NVarChar;
            lok.Size = 50;
            lok.Direction = ParameterDirection.Input;
            lok.Value = Lokomotiva;
            cmd.Parameters.Add(lok);

            SqlParameter opis = new SqlParameter();
            opis.ParameterName = "@Opis";
            opis.SqlDbType = SqlDbType.NVarChar;
            opis.Size = 100;
            opis.Direction = ParameterDirection.Input;
            opis.Value = Opis;
            cmd.Parameters.Add(opis);

            SqlParameter kreirao = new SqlParameter();
            kreirao.ParameterName = "@Kreirao";
            kreirao.SqlDbType = SqlDbType.NVarChar;
            kreirao.Size = 50;
            kreirao.Direction = ParameterDirection.Input;
            kreirao.Value = Kreirao;
            cmd.Parameters.Add(kreirao);

            SqlParameter putanja = new SqlParameter();
            putanja.ParameterName = "@Putanja";
            putanja.SqlDbType = SqlDbType.NVarChar;
            putanja.Size = 500;
            putanja.Direction = ParameterDirection.Input;
            putanja.Value = Putanja;
            cmd.Parameters.Add(putanja);

            conn.Open();
            SqlTransaction tran = conn.BeginTransaction();
            cmd.Transaction = tran;
            bool error = true;
            try
            {
                cmd.ExecuteNonQuery();
                tran.Commit();
                tran = conn.BeginTransaction();
                cmd.Transaction = tran;
            }

            catch (SqlException ex)
            {
                MessageBox.Show(ex.ToString());
                throw new Exception("Neuspešan upis Telegrama brojeva");
            }

            finally
            {
                if (!error)
                {
                    tran.Commit();
                    MessageBox.Show("Unos NHM broja je uspešno završena", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                conn.Close();

                if (error)
                {
                    // Nedra.DataSet1TableAdapters.QueriesTableAdapter adapter = new Nedra.DataSet1TableAdapters.QueriesTableAdapter();
                }
            }
        }
        public void DelLokDok(int ID)
        {
            SqlConnection conn = new SqlConnection(connect);
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "DeleteLokomotivaDokumenti";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter id = new SqlParameter();
            id.ParameterName = "@ID";
            id.SqlDbType = SqlDbType.Int;
            id.Direction = ParameterDirection.Input;
            id.Value = ID;
            cmd.Parameters.Add(id);

            conn.Open();
            SqlTransaction tran = conn.BeginTransaction();
            cmd.Transaction = tran;
            bool error = true;
            try
            {
                cmd.ExecuteNonQuery();
                tran.Commit();
                tran = conn.BeginTransaction();
                cmd.Transaction = tran;
            }

            catch (SqlException ex)
            {
                MessageBox.Show(ex.ToString());
                throw new Exception("Neuspešan upis Telegrama brojeva");
            }

            finally
            {
                if (!error)
                {
                    tran.Commit();
                    MessageBox.Show("Unos NHM broja je uspešno završena", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                conn.Close();

                if (error)
                {
                    // Nedra.DataSet1TableAdapters.QueriesTableAdapter adapter = new Nedra.DataSet1TableAdapters.QueriesTableAdapter();
                }
            }
        }
    }
}