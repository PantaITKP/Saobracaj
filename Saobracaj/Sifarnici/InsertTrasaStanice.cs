using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace Saobracaj.Sifarnici
{
    class InsertTrasaStanice
    {
        public void InsertStaniceTrase(int IDTrase,int Stanica,int RB)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "InsertStaniceTrase";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;

            SqlParameter parameter = new SqlParameter();
            parameter.ParameterName = "@IDTrase";
            parameter.SqlDbType = SqlDbType.Int;
            parameter.Direction = ParameterDirection.Input;
            parameter.Value = IDTrase;
            myCommand.Parameters.Add(parameter);

            SqlParameter parameter2 = new SqlParameter();
            parameter2.ParameterName = "@Stanica";
            parameter2.SqlDbType = SqlDbType.Int;
            parameter2.Direction = ParameterDirection.Input;
            parameter2.Value = Stanica;
            myCommand.Parameters.Add(parameter2);

            SqlParameter parameter3 = new SqlParameter();
            parameter3.ParameterName = "@RB";
            parameter3.SqlDbType = SqlDbType.Int;
            parameter3.Direction = ParameterDirection.Input;
            parameter3.Value = RB;
            myCommand.Parameters.Add(parameter3);

            myConnection.Open();
            SqlTransaction myTransaction = myConnection.BeginTransaction();
            myCommand.Transaction = myTransaction;
            bool error = true;
            try
            {
                myCommand.ExecuteNonQuery();
                myTransaction.Commit();
                myTransaction = myConnection.BeginTransaction();
                myCommand.Transaction = myTransaction;
            }

            catch (SqlException ex)
            {
                throw new Exception(ex.ToString());
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Unos NHM broja je uspešno završena", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                myConnection.Close();

                if (error)
                {
                    // Nedra.DataSet1TableAdapters.QueriesTableAdapter adapter = new Nedra.DataSet1TableAdapters.QueriesTableAdapter();
                }
            }
        }
        public void InsTStan(int IDPruge, int IDTrasa, int StanicaOd, int StanicaDo)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "InsertTrasaStanice";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;

            SqlParameter parameter = new SqlParameter();
            parameter.ParameterName = "@IDPruge";
            parameter.SqlDbType = SqlDbType.Int;
            parameter.Direction = ParameterDirection.Input;
            parameter.Value = IDPruge;
            myCommand.Parameters.Add(parameter);

            SqlParameter parameter2 = new SqlParameter();
            parameter2.ParameterName = "@IDTrasa";
            parameter2.SqlDbType = SqlDbType.Int;
            parameter2.Direction = ParameterDirection.Input;
            parameter2.Value = IDTrasa;
            myCommand.Parameters.Add(parameter2);

            SqlParameter parameter3 = new SqlParameter();
            parameter3.ParameterName = "@StanicaOd";
            parameter3.SqlDbType = SqlDbType.Int;
            parameter3.Direction = ParameterDirection.Input;
            parameter3.Value = StanicaOd;
            myCommand.Parameters.Add(parameter3);


            SqlParameter parameter4 = new SqlParameter();
            parameter4.ParameterName = "@StanicaDo";
            parameter4.SqlDbType = SqlDbType.Int;
            parameter4.Direction = ParameterDirection.Input;
            parameter4.Value = StanicaDo;
            myCommand.Parameters.Add(parameter4);

            myConnection.Open();
            SqlTransaction myTransaction = myConnection.BeginTransaction();
            myCommand.Transaction = myTransaction;
            bool error = true;
            try
            {
                myCommand.ExecuteNonQuery();
                myTransaction.Commit();
                myTransaction = myConnection.BeginTransaction();
                myCommand.Transaction = myTransaction;
            }

            catch (SqlException)
            {
                throw new Exception("Neuspešan upis Stanica na trasi brojeva");
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Unos NHM broja je uspešno završena", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                myConnection.Close();

                if (error)
                {
                    // Nedra.DataSet1TableAdapters.QueriesTableAdapter adapter = new Nedra.DataSet1TableAdapters.QueriesTableAdapter();
                }
            }


        }

        public void DelTStan(int IDTrasa, int RB)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "DeleteStaniceTrase";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;

          

            SqlParameter parameter2 = new SqlParameter();
            parameter2.ParameterName = "@IDTrase";
            parameter2.SqlDbType = SqlDbType.Int;
            parameter2.Direction = ParameterDirection.Input;
            parameter2.Value = IDTrasa;
            myCommand.Parameters.Add(parameter2);

            SqlParameter parameter3 = new SqlParameter();
            parameter3.ParameterName = "@RB";
            parameter3.SqlDbType = SqlDbType.Int;
            parameter3.Direction = ParameterDirection.Input;
            parameter3.Value = RB;
            myCommand.Parameters.Add(parameter3);


           

            myConnection.Open();
            SqlTransaction myTransaction = myConnection.BeginTransaction();
            myCommand.Transaction = myTransaction;
            bool error = true;
            try
            {
                myCommand.ExecuteNonQuery();
                myTransaction.Commit();
                myTransaction = myConnection.BeginTransaction();
                myCommand.Transaction = myTransaction;
            }

            catch (SqlException)
            {
                throw new Exception("Neuspešno brisanje Stanica na trasi brojeva");
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Unos NHM broja je uspešno završena", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                myConnection.Close();

                if (error)
                {
                    // Nedra.DataSet1TableAdapters.QueriesTableAdapter adapter = new Nedra.DataSet1TableAdapters.QueriesTableAdapter();
                }
            }


        }

        public void PrekopirajStanice(int TrasaIz, int TrasaU)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "InsertPrekopirajStaniceTrasa";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;

            SqlParameter parameter = new SqlParameter();
            parameter.ParameterName = "@TrasaIz";
            parameter.SqlDbType = SqlDbType.Int;
            parameter.Direction = ParameterDirection.Input;
            parameter.Value = TrasaIz;
            myCommand.Parameters.Add(parameter);

            SqlParameter parameter2 = new SqlParameter();
            parameter2.ParameterName = "@TrasaU";
            parameter2.SqlDbType = SqlDbType.Int;
            parameter2.Direction = ParameterDirection.Input;
            parameter2.Value = TrasaU;
            myCommand.Parameters.Add(parameter2);

           

            myConnection.Open();
            SqlTransaction myTransaction = myConnection.BeginTransaction();
            myCommand.Transaction = myTransaction;
            bool error = true;
            try
            {
                myCommand.ExecuteNonQuery();
                myTransaction.Commit();
                myTransaction = myConnection.BeginTransaction();
                myCommand.Transaction = myTransaction;
            }

            catch (SqlException ex)
            {
                throw new Exception(ex.ToString());
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Unos NHM broja je uspešno završena", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                myConnection.Close();

                if (error)
                {
                    // Nedra.DataSet1TableAdapters.QueriesTableAdapter adapter = new Nedra.DataSet1TableAdapters.QueriesTableAdapter();
                }
            }
        }



    }
}
