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
    class InsertObracunSatiFiksni
    {
        public void InsObracunFiksni()
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "InsertObracunFiksni1";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;


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
                throw new Exception("Neuspešan upis obracuna u Bazu");
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Nije uspeo ", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                myConnection.Close();

                if (error)
                {
                    // Nedra.DataSet1TableAdapters.QueriesTableAdapter adapter = new Nedra.DataSet1TableAdapters.QueriesTableAdapter();
                }


            }
        }

        public void UpdGO(DateTime VremeOd, DateTime VremeDo)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdGOObracunZaposleniFiksni";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;



            SqlParameter parameter1 = new SqlParameter();
            parameter1.ParameterName = "@DatumOd";
            parameter1.SqlDbType = SqlDbType.DateTime;
            parameter1.Direction = ParameterDirection.Input;
            parameter1.Value = VremeOd;
            myCommand.Parameters.Add(parameter1);

            SqlParameter parameter2 = new SqlParameter();
            parameter2.ParameterName = "@DatumDo";
            parameter2.SqlDbType = SqlDbType.DateTime;
            parameter2.Direction = ParameterDirection.Input;
            parameter2.Value = VremeDo;
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

            catch (SqlException)
            {
                throw new Exception("Neuspešan upis obracuna u Bazu");
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Nije uspeo ", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                myConnection.Close();

                if (error)
                {
                    // Nedra.DataSet1TableAdapters.QueriesTableAdapter adapter = new Nedra.DataSet1TableAdapters.QueriesTableAdapter();
                }


            }
        }

        public void UpdPraznikSatiFiksni(int ID, int UkupnoSati)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdatePraznikSatiFiksni";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;



            SqlParameter parameter1 = new SqlParameter();
            parameter1.ParameterName = "@ID";
            parameter1.SqlDbType = SqlDbType.Int;
            parameter1.Direction = ParameterDirection.Input;
            parameter1.Value = ID;
            myCommand.Parameters.Add(parameter1);


            SqlParameter parameter2 = new SqlParameter();
            parameter2.ParameterName = "@UkupnoSati";
            parameter2.SqlDbType = SqlDbType.Int;
            parameter2.Direction = ParameterDirection.Input;
            parameter2.Value = UkupnoSati;
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

            catch (SqlException)
            {
                throw new Exception("Neuspešan upis obracuna u Bazu");
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Nije uspeo ", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                myConnection.Close();

                if (error)
                {
                    // Nedra.DataSet1TableAdapters.QueriesTableAdapter adapter = new Nedra.DataSet1TableAdapters.QueriesTableAdapter();
                }


            }
        }

        public void UpdBolovanje65Fiksni(int ID, int UkupnoSati)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdateBolovanje65Fiksni";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;



            SqlParameter parameter1 = new SqlParameter();
            parameter1.ParameterName = "@ID";
            parameter1.SqlDbType = SqlDbType.Int;
            parameter1.Direction = ParameterDirection.Input;
            parameter1.Value = ID;
            myCommand.Parameters.Add(parameter1);


            SqlParameter parameter2 = new SqlParameter();
            parameter2.ParameterName = "@UkupnoSati";
            parameter2.SqlDbType = SqlDbType.Int;
            parameter2.Direction = ParameterDirection.Input;
            parameter2.Value = UkupnoSati;
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

            catch (SqlException)
            {
                throw new Exception("Neuspešan upis obracuna u Bazu");
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Nije uspeo ", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                myConnection.Close();

                if (error)
                {
                    // Nedra.DataSet1TableAdapters.QueriesTableAdapter adapter = new Nedra.DataSet1TableAdapters.QueriesTableAdapter();
                }


            }
        }


        public void UpdUkupanFondSatiPraznik(int FondSati, int MesecniFondUkupan)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdUkupanFondSatiPraznik";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;



            SqlParameter parameter1 = new SqlParameter();
            parameter1.ParameterName = "@FondSati";
            parameter1.SqlDbType = SqlDbType.Int;
            parameter1.Direction = ParameterDirection.Input;
            parameter1.Value = FondSati;
            myCommand.Parameters.Add(parameter1);

            SqlParameter parameter2 = new SqlParameter();
            parameter2.ParameterName = "@MesecniFondUkupan";
            parameter2.SqlDbType = SqlDbType.Int;
            parameter2.Direction = ParameterDirection.Input;
            parameter2.Value = MesecniFondUkupan;
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

            catch (SqlException)
            {
                throw new Exception("Neuspešan upis obracuna u Bazu");
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Nije uspeo ", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                myConnection.Close();

                if (error)
                {
                    // Nedra.DataSet1TableAdapters.QueriesTableAdapter adapter = new Nedra.DataSet1TableAdapters.QueriesTableAdapter();
                }


            }
        }

        public void UpdBolovanje100Fiksni(int ID, int UkupnoSati)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdateBolovanje100Fiksni";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;



            SqlParameter parameter1 = new SqlParameter();
            parameter1.ParameterName = "@ID";
            parameter1.SqlDbType = SqlDbType.Int;
            parameter1.Direction = ParameterDirection.Input;
            parameter1.Value = ID;
            myCommand.Parameters.Add(parameter1);


            SqlParameter parameter2 = new SqlParameter();
            parameter2.ParameterName = "@UkupnoSati";
            parameter2.SqlDbType = SqlDbType.Int;
            parameter2.Direction = ParameterDirection.Input;
            parameter2.Value = UkupnoSati;
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

            catch (SqlException)
            {
                throw new Exception("Neuspešan upis obracuna u Bazu");
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Nije uspeo ", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                myConnection.Close();

                if (error)
                {
                    // Nedra.DataSet1TableAdapters.QueriesTableAdapter adapter = new Nedra.DataSet1TableAdapters.QueriesTableAdapter();
                }


            }
        }

        public void UpdPrekovremeniFiksni(int ID, int UkupnoSati)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdatePrekovremeniFiksni";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;



            SqlParameter parameter1 = new SqlParameter();
            parameter1.ParameterName = "@ID";
            parameter1.SqlDbType = SqlDbType.Int;
            parameter1.Direction = ParameterDirection.Input;
            parameter1.Value = ID;
            myCommand.Parameters.Add(parameter1);


            SqlParameter parameter2 = new SqlParameter();
            parameter2.ParameterName = "@UkupnoSati";
            parameter2.SqlDbType = SqlDbType.Int;
            parameter2.Direction = ParameterDirection.Input;
            parameter2.Value = UkupnoSati;
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

            catch (SqlException)
            {
                throw new Exception("Neuspešan upis obracuna u Bazu");
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Nije uspeo ", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                myConnection.Close();

                if (error)
                {
                    // Nedra.DataSet1TableAdapters.QueriesTableAdapter adapter = new Nedra.DataSet1TableAdapters.QueriesTableAdapter();
                }


            }
        }

        public void UpdObracunFiksniSve(DateTime VremeOd, DateTime VremeDo, double kurs, double FondSati, double Minimalac)
        {
           



            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdateObracunFiksniSve";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;



            SqlParameter parameter1 = new SqlParameter();
            parameter1.ParameterName = "@DatumOd";
            parameter1.SqlDbType = SqlDbType.DateTime;
            parameter1.Direction = ParameterDirection.Input;
            parameter1.Value = VremeOd;
            myCommand.Parameters.Add(parameter1);

            SqlParameter parameter2 = new SqlParameter();
            parameter2.ParameterName = "@DatumDo";
            parameter2.SqlDbType = SqlDbType.DateTime;
            parameter2.Direction = ParameterDirection.Input;
            parameter2.Value = VremeDo;
            myCommand.Parameters.Add(parameter2);

            SqlParameter parameter3 = new SqlParameter();
            parameter3.ParameterName = "@kurs";
            parameter3.SqlDbType = SqlDbType.Decimal;
            parameter3.Direction = ParameterDirection.Input;
            parameter3.Value = kurs;
            myCommand.Parameters.Add(parameter3);


            SqlParameter parameter4 = new SqlParameter();
            parameter4.ParameterName = "@FondSati";
            parameter4.SqlDbType = SqlDbType.Decimal;
            parameter4.Direction = ParameterDirection.Input;
            parameter4.Value = FondSati;
            myCommand.Parameters.Add(parameter4);

            SqlParameter parameter5 = new SqlParameter();
            parameter5.ParameterName = "@Minimalac";
            parameter5.SqlDbType = SqlDbType.Decimal;
            parameter5.Direction = ParameterDirection.Input;
            parameter5.Value = Minimalac;
            myCommand.Parameters.Add(parameter5);


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
                throw new Exception("Neuspešan upis obracuna u Bazu");
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Nije uspeo ", "",
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
