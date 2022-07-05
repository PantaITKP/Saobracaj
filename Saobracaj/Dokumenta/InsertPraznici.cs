
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace Saobracaj.Dokumenta
{
    class InsertPraznici
    {

        public void InsPrznici(string Naziv,  DateTime DatumOd, DateTime DatumDo, int aktivan)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "InsertPraznici";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;





            SqlParameter parameter = new SqlParameter();
            parameter.ParameterName = "@Naziv";
            parameter.SqlDbType = SqlDbType.NVarChar;
            parameter.Size = 100;
            parameter.Direction = ParameterDirection.Input;
            parameter.Value = Naziv;
            myCommand.Parameters.Add(parameter);


            SqlParameter parameter4 = new SqlParameter();
            parameter4.ParameterName = "@DatumOd";
            parameter4.SqlDbType = SqlDbType.DateTime;
            parameter4.Direction = ParameterDirection.Input;
            parameter4.Value = DatumOd;
            myCommand.Parameters.Add(parameter4);

            SqlParameter parameter5 = new SqlParameter();
            parameter5.ParameterName = "@DatumDo";
            parameter5.SqlDbType = SqlDbType.DateTime;
            parameter5.Direction = ParameterDirection.Input;
            parameter5.Value = DatumDo;
            myCommand.Parameters.Add(parameter5);

            SqlParameter parameter6 = new SqlParameter();
            parameter6.ParameterName = "@Aktivan";
            parameter6.SqlDbType = SqlDbType.Int;
            parameter6.Direction = ParameterDirection.Input;
            parameter6.Value = aktivan;
            myCommand.Parameters.Add(parameter6);



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
                throw new Exception("Neuspešan upis cena u bazu");
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Nije uspeo upis cena", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                myConnection.Close();

                if (error)
                {
                    // Nedra.DataSet1TableAdapters.QueriesTableAdapter adapter = new Nedra.DataSet1TableAdapters.QueriesTableAdapter();
                }


            }
        }

        public void UpdPraznici(int ID, string Naziv, DateTime DatumOd, DateTime DatumDo, int aktivan)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdatePraznici";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;

            SqlParameter parameter = new SqlParameter();
            parameter.ParameterName = "@ID";
            parameter.SqlDbType = SqlDbType.Int;
            parameter.Direction = ParameterDirection.Input;
            parameter.Value = ID;
            myCommand.Parameters.Add(parameter);



            SqlParameter parameter2 = new SqlParameter();
            parameter2.ParameterName = "@Naziv";
            parameter2.SqlDbType = SqlDbType.NVarChar;
            parameter2.Size = 100;
            parameter2.Direction = ParameterDirection.Input;
            parameter2.Value = Naziv;
            myCommand.Parameters.Add(parameter2);


            SqlParameter parameter4 = new SqlParameter();
            parameter4.ParameterName = "@DatumOd";
            parameter4.SqlDbType = SqlDbType.DateTime;
            parameter4.Direction = ParameterDirection.Input;
            parameter4.Value = DatumOd;
            myCommand.Parameters.Add(parameter4);

            SqlParameter parameter5 = new SqlParameter();
            parameter5.ParameterName = "@DatumDo";
            parameter5.SqlDbType = SqlDbType.DateTime;
            parameter5.Direction = ParameterDirection.Input;
            parameter5.Value = DatumDo;
            myCommand.Parameters.Add(parameter5);

            SqlParameter parameter6 = new SqlParameter();
            parameter6.ParameterName = "@Aktivan";
            parameter6.SqlDbType = SqlDbType.Int;
            parameter6.Direction = ParameterDirection.Input;
            parameter6.Value = aktivan;
            myCommand.Parameters.Add(parameter6);

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
                throw new Exception("Neuspešan upis u Bazu");
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Nije uspeo upis ", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                myConnection.Close();

                if (error)
                {
                    // Nedra.DataSet1TableAdapters.QueriesTableAdapter adapter = new Nedra.DataSet1TableAdapters.QueriesTableAdapter();
                }


            }
        }

        public void DeletePraznici(int ID)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "DeletePraznici";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;

            SqlParameter parameter = new SqlParameter();
            parameter.ParameterName = "@ID";
            parameter.SqlDbType = SqlDbType.Int;
            parameter.Direction = ParameterDirection.Input;
            parameter.Value = ID;
            myCommand.Parameters.Add(parameter);

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
                throw new Exception("Brisanje neuspešno");
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Brisanje  uspešno završeno", "",
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
