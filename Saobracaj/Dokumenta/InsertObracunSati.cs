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
    class InsertObracunSati
    {
        public void UpdMilspedPraznikom(int ID, int UkupnoSati)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdateRadPraznikom";
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

        public void UpdKragujevacPrekovremeni(int ID, int UkupnoSati)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdateKragujevacPrekovremeni";
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

        public void UpdRemontBolovanje65(int ID, int UkupnoSati)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdateBolovanje65";
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

        public void UpdRemontBolovanje100(int ID, int UkupnoSati)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdateBolovanje100";
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
        public void InsObracun(DateTime VremeOd, DateTime VremeDo)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "SelectObracunMMV";
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
            /*
            SqlParameter parameter3 = new SqlParameter();
            parameter3.ParameterName = "@TopliObrok";
            parameter3.SqlDbType = SqlDbType.Int;
            parameter3.Direction = ParameterDirection.Input;
            parameter3.Value = TopliObrok;
            myCommand.Parameters.Add(parameter3);

            SqlParameter parameter4 = new SqlParameter();
            parameter4.ParameterName = "@Regres";
            parameter4.SqlDbType = SqlDbType.Int;
            parameter4.Direction = ParameterDirection.Input;
            parameter4.Value = Regres;
            myCommand.Parameters.Add(parameter4);

            SqlParameter parameter5 = new SqlParameter();
            parameter4.ParameterName = "@MesecnoSati";
            parameter4.SqlDbType = SqlDbType.Int;
            parameter4.Direction = ParameterDirection.Input;
            parameter4.Value = MesecnoSati;
            myCommand.Parameters.Add(parameter4);

            */

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


        public void InsObracunFiksni(DateTime VremeOd, DateTime VremeDo, double kurs, double FondSati, double PraznicnihSati, double minCenaRadaNeto, double minNetoZarada, double PoreskoOslobodjenje, double minBruto)
        {
/*
            NBS srednji kurs EUR
Mesečni fond časova
Broj prazničkih dana u mesecu
Mesečni fond prazničkih čaosva
Minimalna cena rada neto
Minimalna neto zarada
Poresko oslobođenje
Minimalna bruto zarada
*/



            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "InsertObracunFiksniExcel";
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
            parameter5.ParameterName = "@PraznicnihSati";
            parameter5.SqlDbType = SqlDbType.Decimal;
            parameter5.Direction = ParameterDirection.Input;
            parameter5.Value = PraznicnihSati;
            myCommand.Parameters.Add(parameter5);

            SqlParameter parameter6 = new SqlParameter();
            parameter6.ParameterName = "@minCenaRadaNeto";
            parameter6.SqlDbType = SqlDbType.Decimal;
            parameter6.Direction = ParameterDirection.Input;
            parameter6.Value = minCenaRadaNeto;
            myCommand.Parameters.Add(parameter6);

            SqlParameter parameter7 = new SqlParameter();
            parameter7.ParameterName = "@minNetoZarada";
            parameter7.SqlDbType = SqlDbType.Decimal;
            parameter7.Direction = ParameterDirection.Input;
            parameter7.Value = minNetoZarada;
            myCommand.Parameters.Add(parameter7);

            SqlParameter parameter8 = new SqlParameter();
            parameter8.ParameterName = "@PoreskoOslobodjenje";
            parameter8.SqlDbType = SqlDbType.Decimal;
            parameter8.Direction = ParameterDirection.Input;
            parameter8.Value = PoreskoOslobodjenje;
            myCommand.Parameters.Add(parameter8);

            SqlParameter parameter9 = new SqlParameter();
            parameter9.ParameterName = "@minBruto";
            parameter9.SqlDbType = SqlDbType.Decimal;
            parameter9.Direction = ParameterDirection.Input;
            parameter9.Value = minBruto;
            myCommand.Parameters.Add(parameter9);


            //double kurs, double FondSati, double PraznicnihSati, double minCenaRadaNeto, double minNetoZarada, double PoreskoOslobodjenje, double minBruto
            /*
            SqlParameter parameter3 = new SqlParameter();
            parameter3.ParameterName = "@TopliObrok";
            parameter3.SqlDbType = SqlDbType.Int;
            parameter3.Direction = ParameterDirection.Input;
            parameter3.Value = TopliObrok;
            myCommand.Parameters.Add(parameter3);

            SqlParameter parameter4 = new SqlParameter();
            parameter4.ParameterName = "@Regres";
            parameter4.SqlDbType = SqlDbType.Int;
            parameter4.Direction = ParameterDirection.Input;
            parameter4.Value = Regres;
            myCommand.Parameters.Add(parameter4);

            SqlParameter parameter5 = new SqlParameter();
            parameter4.ParameterName = "@MesecnoSati";
            parameter4.SqlDbType = SqlDbType.Int;
            parameter4.Direction = ParameterDirection.Input;
            parameter4.Value = MesecnoSati;
            myCommand.Parameters.Add(parameter4);

            */

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

        public void InsObracunFIKSNI(DateTime VremeOd, DateTime VremeDo, int MesecnoSati)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "SelectObracunMMVFIKSNI";
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
            parameter3.ParameterName = "@MesecniSati";
            parameter3.SqlDbType = SqlDbType.Int;
            parameter3.Direction = ParameterDirection.Input;
            parameter3.Value = MesecnoSati;
            myCommand.Parameters.Add(parameter3);
            /*
            SqlParameter parameter3 = new SqlParameter();
            parameter3.ParameterName = "@TopliObrok";
            parameter3.SqlDbType = SqlDbType.Int;
            parameter3.Direction = ParameterDirection.Input;
            parameter3.Value = TopliObrok;
            myCommand.Parameters.Add(parameter3);

            SqlParameter parameter4 = new SqlParameter();
            parameter4.ParameterName = "@Regres";
            parameter4.SqlDbType = SqlDbType.Int;
            parameter4.Direction = ParameterDirection.Input;
            parameter4.Value = Regres;
            myCommand.Parameters.Add(parameter4);

            SqlParameter parameter5 = new SqlParameter();
            parameter4.ParameterName = "@MesecnoSati";
            parameter4.SqlDbType = SqlDbType.Int;
            parameter4.Direction = ParameterDirection.Input;
            parameter4.Value = MesecnoSati;
            myCommand.Parameters.Add(parameter4);

            */

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

        public void UpdUkupno1(DateTime VremeOd, DateTime VremeDo)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "SelectObracunMMVUkupno1";
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

        public void UpdLokomotiva(DateTime VremeOd, DateTime VremeDo)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "SelectObracunMMVLokomotiva";
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

        public void UpdMilsped(DateTime VremeOd, DateTime VremeDo)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "SelectObracunMMVMilsped";
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

        public void UpdKragujevac(DateTime VremeOd, DateTime VremeDo)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "SelectObracunMMVKragujevac";
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

        public void UpdRemont(DateTime VremeOd, DateTime VremeDo)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "SelectObracunMMVRemont";
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

        public void UpdSmederevo(DateTime VremeOd, DateTime VremeDo)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "SelectObracunMMVSmederevo";
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

        public void UpdINO(DateTime VremeOd, DateTime VremeDo)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "SelectObracunMMVIno";
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

        public void UpdGO(DateTime VremeOd, DateTime VremeDo)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdGOObracunZaposleni";
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

        public void UpdPutniNalozi(DateTime VremeOd, DateTime VremeDo)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "SelectPutniNalozi";
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

        public void UpdZakljucavanjeSmene(DateTime VremeOd)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdateZakljucavanjeSmene";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;



            SqlParameter parameter1 = new SqlParameter();
            parameter1.ParameterName = "@DatumOd";
            parameter1.SqlDbType = SqlDbType.DateTime;
            parameter1.Direction = ParameterDirection.Input;
            parameter1.Value = VremeOd;
            myCommand.Parameters.Add(parameter1);


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

        public void UpdPutniNaloziPola(DateTime VremeOd, DateTime VremeDo)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "SelectPutniNaloziPola";
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

        public void UpdKragujevacPrekovremeniVarijabilni(DateTime VremeOd, DateTime VremeDo, int MesecnoSati)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdKragujevacPrekovremeniVarijabilni";
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
            parameter3.ParameterName = "@MesecnoSati";
            parameter3.SqlDbType = SqlDbType.Int;
            parameter3.Direction = ParameterDirection.Input;
            parameter3.Value = MesecnoSati;
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

        public void UpdKragujevacPrekovremeniVarijabilniGodisnjiOdmor(DateTime VremeOd, DateTime VremeDo)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdKragujevacPrekovremeniVarijabilniGodisnjiOdmor";
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

        public void UpdMilspedPraznici(DateTime VremeOd, DateTime VremeDo, int FondPraznici)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdMilspedPraznici";
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
            parameter3.ParameterName = "@FondPraznici";
            parameter3.SqlDbType = SqlDbType.Int;
            parameter3.Direction = ParameterDirection.Input;
            parameter3.Value = FondPraznici;
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

        public void UpdUkupno(double Kurs, double MesecnoSati, double PoreskaOlaksica, double Minimalac)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdateObracunZarade";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;


            SqlParameter parameter1 = new SqlParameter();
            parameter1.ParameterName = "@Kurs";
            parameter1.SqlDbType = SqlDbType.Decimal;
            parameter1.Direction = ParameterDirection.Input;
            parameter1.Value = Kurs;
            myCommand.Parameters.Add(parameter1);

            SqlParameter parameter2 = new SqlParameter();
            parameter2.ParameterName = "@MesecnoSati";
            parameter2.SqlDbType = SqlDbType.Decimal;
            parameter2.Direction = ParameterDirection.Input;
            parameter2.Value = MesecnoSati;
            myCommand.Parameters.Add(parameter2);

            SqlParameter parameter3 = new SqlParameter();
            parameter3.ParameterName = "@PoreskaOlaksica";
            parameter3.SqlDbType = SqlDbType.Decimal;
            parameter3.Direction = ParameterDirection.Input;
            parameter3.Value = PoreskaOlaksica;
            myCommand.Parameters.Add(parameter3);

            SqlParameter parameter4 = new SqlParameter();
            parameter4.ParameterName = "@Minimalac";
            parameter4.SqlDbType = SqlDbType.Decimal;
            parameter4.Direction = ParameterDirection.Input;
            parameter4.Value = Minimalac;
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


        public void UpdRedovnoSati(double MesecnoSati)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdateRedovnoSati";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;


            SqlParameter parameter2 = new SqlParameter();
            parameter2.ParameterName = "@MesecnoSati";
            parameter2.SqlDbType = SqlDbType.Decimal;
            parameter2.Direction = ParameterDirection.Input;
            parameter2.Value = MesecnoSati;
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

        public void UpdUkupnoFIKSNI(double Kurs, double MesecnoSati)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdateObracunZaradeFIKSNI";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;


            SqlParameter parameter1 = new SqlParameter();
            parameter1.ParameterName = "@Kurs";
            parameter1.SqlDbType = SqlDbType.Decimal;
            parameter1.Direction = ParameterDirection.Input;
            parameter1.Value = Kurs;
            myCommand.Parameters.Add(parameter1);

            SqlParameter parameter2 = new SqlParameter();
            parameter2.ParameterName = "@MesecnoSati";
            parameter2.SqlDbType = SqlDbType.Decimal;
            parameter2.Direction = ParameterDirection.Input;
            parameter2.Value = MesecnoSati;
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

        //UpdateUkupnoUEUR
        public void UpdUkupnoMinimalac(double Minimalac)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdateMinimalac";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;


            SqlParameter parameter1 = new SqlParameter();
            parameter1.ParameterName = "@Minimalac";
            parameter1.SqlDbType = SqlDbType.Decimal;
            parameter1.Direction = ParameterDirection.Input;
            parameter1.Value = Minimalac;
            myCommand.Parameters.Add(parameter1);

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


        public void DodatnaObrada(DateTime VremeOd, DateTime VremeDo, double MesecnoSati, double Minimalac)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "InsertObracunVarijabilniExcel";
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
            parameter3.ParameterName = "@FondSati";
            parameter3.SqlDbType = SqlDbType.Decimal;
            parameter3.Direction = ParameterDirection.Input;
            parameter3.Value = MesecnoSati;
            myCommand.Parameters.Add(parameter3);

            SqlParameter parameter4 = new SqlParameter();
            parameter4.ParameterName = "@Minimalac";
            parameter4.SqlDbType = SqlDbType.Decimal;
            parameter4.Direction = ParameterDirection.Input;
            parameter4.Value = Minimalac;
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

        public void UpdPN(int PnZapisa)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdatePnZapisa";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;


            SqlParameter parameter1 = new SqlParameter();
            parameter1.ParameterName = "@PnZapisa";
            parameter1.SqlDbType = SqlDbType.Decimal;
            parameter1.Direction = ParameterDirection.Input;
            parameter1.Value = PnZapisa;
            myCommand.Parameters.Add(parameter1);

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

        public void UpdUkupnoUEUR(int ID, double Ukupno)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdateUkupnoUEUR";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;

            SqlParameter parameter1 = new SqlParameter();
            parameter1.ParameterName = "@ID";
            parameter1.SqlDbType = SqlDbType.Int;
            parameter1.Direction = ParameterDirection.Input;
            parameter1.Value = ID;
            myCommand.Parameters.Add(parameter1);


            SqlParameter parameter2 = new SqlParameter();
            parameter2.ParameterName = "@Ukupno";
            parameter2.SqlDbType = SqlDbType.Decimal;
            parameter2.Direction = ParameterDirection.Input;
            parameter2.Value = Ukupno;
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

        public void UpdKazna()
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdateKazne";
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

        public void UpdUkupnoSmanjenje(int ID, double Kazna)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdateUkupnoUSmanjenje";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;

            SqlParameter parameter1 = new SqlParameter();
            parameter1.ParameterName = "@ID";
            parameter1.SqlDbType = SqlDbType.Int;
            parameter1.Direction = ParameterDirection.Input;
            parameter1.Value = ID;
            myCommand.Parameters.Add(parameter1);


            SqlParameter parameter2 = new SqlParameter();
            parameter2.ParameterName = "@Kazna";
            parameter2.SqlDbType = SqlDbType.Decimal;
            parameter2.Direction = ParameterDirection.Input;
            parameter2.Value =Kazna;
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

        public void DelObracun()
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "DeleteObracunZarade";
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
                throw new Exception("Neuspešan brisanje obracuna u Bazu");
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

        public void DelObracunfiksni()
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "DeleteObracunZaradeFiksni";
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
                throw new Exception("Neuspešan brisanje obracuna u Bazu");
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

        public void SelectPNBrisanje()
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "SelectPNBrisanje";
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
                throw new Exception("Neuspešan brisanje obracuna u Bazu");
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

        public void SelectPNBrisanje2()
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "SelectPNBrisanje2";
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
                throw new Exception("Neuspešan brisanje obracuna u Bazu");
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

        public void SelectPNBrisanje3()
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "SelectPNBrisanje3";
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
                throw new Exception("Neuspešan brisanje obracuna u Bazu");
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

        public void SelectPNBrisanje4()
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "SelectPNBrisanje4";
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
                throw new Exception("Neuspešan brisanje obracuna u Bazu");
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

        public void SelectPNBrisanje5()
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "SelectPNBrisanje5";
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
                throw new Exception("Neuspešan brisanje obracuna u Bazu");
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

        public void SelectObracunMMVPrevoz()
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "SelectObracunMMVPrevoz";
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
                throw new Exception("Neuspešan brisanje obracuna u Bazu");
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


        public void UpdObracunSve(DateTime VremeOd, DateTime VremeDo, double kurs, double FondSati, double Minimalac)
        {




            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdateObracunSve";
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
