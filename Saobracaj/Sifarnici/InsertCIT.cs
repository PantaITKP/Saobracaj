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
    class InsertCIT
    {
        public void InsCIT(string Otpravna, string Uputna, string PrevozniPut, string Posiljalac, string CarinskiDokument, string Drzava, string Preduzece, string Primalac, string Pratilac, string Stanica
            , string Otpravljanje, string PreuzimaNaPrevoz, string SluzbeneNapomeneCarine, string Potpisnik)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "InsertCIT";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;

            SqlParameter otpravna = new SqlParameter();
            otpravna.ParameterName = "@Otpravna";
            otpravna.SqlDbType = SqlDbType.NVarChar;
            otpravna.Size = 50;
            otpravna.Direction = ParameterDirection.Input;
            otpravna.Value = Otpravna;
            myCommand.Parameters.Add(otpravna);

            SqlParameter uputna = new SqlParameter();
            uputna.ParameterName = "@Uputna";
            uputna.SqlDbType = SqlDbType.NVarChar;
            uputna.Size = 50;
            uputna.Direction = ParameterDirection.Input;
            uputna.Value = Uputna;
            myCommand.Parameters.Add(uputna);

            SqlParameter put = new SqlParameter();
            put.ParameterName = "@PrevozniPut";
            put.SqlDbType = SqlDbType.NVarChar;
            put.Size = 200;
            put.Direction = ParameterDirection.Input;
            put.Value = PrevozniPut;
            myCommand.Parameters.Add(put);

            SqlParameter posiljalac = new SqlParameter();
            posiljalac.ParameterName = "@Posiljalac";
            posiljalac.SqlDbType = SqlDbType.NVarChar;
            posiljalac.Size = 100;
            posiljalac.Direction = ParameterDirection.Input;
            posiljalac.Value = Posiljalac;
            myCommand.Parameters.Add(posiljalac);

            SqlParameter dok = new SqlParameter();
            dok.ParameterName = "@CarinskiDokument";
            dok.SqlDbType = SqlDbType.NVarChar;
            dok.Size = 50;
            dok.Direction = ParameterDirection.Input;
            dok.Value = CarinskiDokument;
            myCommand.Parameters.Add(dok);

            SqlParameter drzava = new SqlParameter();
            drzava.ParameterName = "@Drzava";
            drzava.SqlDbType = SqlDbType.NVarChar;
            drzava.Size = 50;
            drzava.Direction = ParameterDirection.Input;
            drzava.Value = Drzava;
            myCommand.Parameters.Add(drzava);

            SqlParameter preduzece = new SqlParameter();
            preduzece.ParameterName = "@Preduzece";
            preduzece.SqlDbType = SqlDbType.NVarChar;
            preduzece.Size = 200;
            preduzece.Direction = ParameterDirection.Input;
            preduzece.Value = Preduzece;
            myCommand.Parameters.Add(preduzece);

            SqlParameter primalac = new SqlParameter();
            primalac.ParameterName = "@Primalac";
            primalac.SqlDbType = SqlDbType.NVarChar;
            primalac.Size = 100;
            primalac.Direction = ParameterDirection.Input;
            primalac.Value = Primalac;
            myCommand.Parameters.Add(primalac);

            SqlParameter pratilac = new SqlParameter();
            pratilac.ParameterName = "@Pratilac";
            pratilac.SqlDbType = SqlDbType.NVarChar;
            pratilac.Size = 100;
            pratilac.Direction = ParameterDirection.Input;
            pratilac.Value = Pratilac;
            myCommand.Parameters.Add(pratilac);

            SqlParameter stanica = new SqlParameter();
            stanica.ParameterName = "@Stanica";
            stanica.SqlDbType = SqlDbType.NVarChar;
            stanica.Size = 50;
            stanica.Direction = ParameterDirection.Input;
            stanica.Value = Stanica;
            myCommand.Parameters.Add(stanica);

            SqlParameter otpravljanje = new SqlParameter();
            otpravljanje.ParameterName = "@Otpravljanje";
            otpravljanje.SqlDbType = SqlDbType.NVarChar;
            otpravljanje.Size = 50;
            otpravljanje.Direction = ParameterDirection.Input;
            otpravljanje.Value = Otpravljanje;
            myCommand.Parameters.Add(otpravljanje);

            SqlParameter preuzima = new SqlParameter();
            preuzima.ParameterName = "@PreuzimaNaPrevoz";
            preuzima.SqlDbType = SqlDbType.NVarChar;
            preuzima.Size = 50;
            preuzima.Direction = ParameterDirection.Input;
            preuzima.Value = PreuzimaNaPrevoz;
            myCommand.Parameters.Add(preuzima);

            SqlParameter napomena = new SqlParameter();
            napomena.ParameterName = "@SluzbeneNapomeneCarine";
            napomena.SqlDbType = SqlDbType.NVarChar;
            napomena.Size = 200;
            napomena.Direction = ParameterDirection.Input;
            napomena.Value = SluzbeneNapomeneCarine;
            myCommand.Parameters.Add(napomena);

            SqlParameter potpisnik = new SqlParameter();
            potpisnik.ParameterName = "@Potpisnik";
            potpisnik.SqlDbType = SqlDbType.NVarChar;
            potpisnik.Size = 50;
            potpisnik.Direction = ParameterDirection.Input;
            potpisnik.Value = Potpisnik;
            myCommand.Parameters.Add(potpisnik);

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
                throw new Exception("Neuspešan upis ");
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Unos uspešno završen", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                myConnection.Close();

                if (error)
                {
                    // Nedra.DataSet1TableAdapters.QueriesTableAdapter adapter = new Nedra.DataSet1TableAdapters.QueriesTableAdapter();
                }
            }
        }
        public void UpdCIT(int ID, string Otpravna, string Uputna, string PrevozniPut, string Posiljalac, string CarinskiDokument, string Drzava, string Preduzece, string Primalac, string Pratilac, string Stanica
            , string Otpravljanje, string PreuzimaNaPrevoz, string SluzbeneNapomeneCarine, string Potpisnik)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdateCIT";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;

            SqlParameter id = new SqlParameter();
            id.ParameterName = "@ID";
            id.SqlDbType = SqlDbType.Int;
            id.Direction = ParameterDirection.Input;
            id.Value = ID;
            myCommand.Parameters.Add(id);

            SqlParameter otpravna = new SqlParameter();
            otpravna.ParameterName = "@Otpravna";
            otpravna.SqlDbType = SqlDbType.NVarChar;
            otpravna.Size = 50;
            otpravna.Direction = ParameterDirection.Input;
            otpravna.Value = Otpravna;
            myCommand.Parameters.Add(otpravna);

            SqlParameter uputna = new SqlParameter();
            uputna.ParameterName = "@Uputna";
            uputna.SqlDbType = SqlDbType.NVarChar;
            uputna.Size = 50;
            uputna.Direction = ParameterDirection.Input;
            uputna.Value = Uputna;
            myCommand.Parameters.Add(uputna);

            SqlParameter put = new SqlParameter();
            put.ParameterName = "@PrevozniPut";
            put.SqlDbType = SqlDbType.NVarChar;
            put.Size = 200;
            put.Direction = ParameterDirection.Input;
            put.Value = PrevozniPut;
            myCommand.Parameters.Add(put);

            SqlParameter posiljalac = new SqlParameter();
            posiljalac.ParameterName = "@Posiljalac";
            posiljalac.SqlDbType = SqlDbType.NVarChar;
            posiljalac.Size = 100;
            posiljalac.Direction = ParameterDirection.Input;
            posiljalac.Value = Posiljalac;
            myCommand.Parameters.Add(posiljalac);

            SqlParameter dok = new SqlParameter();
            dok.ParameterName = "@CarinskiDokument";
            dok.SqlDbType = SqlDbType.NVarChar;
            dok.Size = 50;
            dok.Direction = ParameterDirection.Input;
            dok.Value = CarinskiDokument;
            myCommand.Parameters.Add(dok);

            SqlParameter drzava = new SqlParameter();
            drzava.ParameterName = "@Drzava";
            drzava.SqlDbType = SqlDbType.NVarChar;
            drzava.Size = 50;
            drzava.Direction = ParameterDirection.Input;
            drzava.Value = Drzava;
            myCommand.Parameters.Add(drzava);

            SqlParameter preduzece = new SqlParameter();
            preduzece.ParameterName = "@Preduzece";
            preduzece.SqlDbType = SqlDbType.NVarChar;
            preduzece.Size = 200;
            preduzece.Direction = ParameterDirection.Input;
            preduzece.Value = Preduzece;
            myCommand.Parameters.Add(preduzece);

            SqlParameter primalac = new SqlParameter();
            primalac.ParameterName = "@Primalac";
            primalac.SqlDbType = SqlDbType.NVarChar;
            primalac.Size = 100;
            primalac.Direction = ParameterDirection.Input;
            primalac.Value = Primalac;
            myCommand.Parameters.Add(primalac);

            SqlParameter pratilac = new SqlParameter();
            pratilac.ParameterName = "@Pratilac";
            pratilac.SqlDbType = SqlDbType.NVarChar;
            pratilac.Size = 100;
            pratilac.Direction = ParameterDirection.Input;
            pratilac.Value = Pratilac;
            myCommand.Parameters.Add(pratilac);

            SqlParameter stanica = new SqlParameter();
            stanica.ParameterName = "@Stanica";
            stanica.SqlDbType = SqlDbType.NVarChar;
            stanica.Size = 50;
            stanica.Direction = ParameterDirection.Input;
            stanica.Value = Stanica;
            myCommand.Parameters.Add(stanica);

            SqlParameter otpravljanje = new SqlParameter();
            otpravljanje.ParameterName = "@Otpravljanje";
            otpravljanje.SqlDbType = SqlDbType.NVarChar;
            otpravljanje.Size = 50;
            otpravljanje.Direction = ParameterDirection.Input;
            otpravljanje.Value = Otpravljanje;
            myCommand.Parameters.Add(otpravljanje);

            SqlParameter preuzima = new SqlParameter();
            preuzima.ParameterName = "@PreuzimaNaPrevoz";
            preuzima.SqlDbType = SqlDbType.NVarChar;
            preuzima.Size = 50;
            preuzima.Direction = ParameterDirection.Input;
            preuzima.Value = PreuzimaNaPrevoz;
            myCommand.Parameters.Add(preuzima);

            SqlParameter napomena = new SqlParameter();
            napomena.ParameterName = "@SluzbeneNapomeneCarinika";
            napomena.SqlDbType = SqlDbType.NVarChar;
            napomena.Size = 200;
            napomena.Direction = ParameterDirection.Input;
            napomena.Value = SluzbeneNapomeneCarine;
            myCommand.Parameters.Add(napomena);

            SqlParameter potpisnik = new SqlParameter();
            potpisnik.ParameterName = "@Potpisnik";
            potpisnik.SqlDbType = SqlDbType.NVarChar;
            potpisnik.Size = 50;
            potpisnik.Direction = ParameterDirection.Input;
            potpisnik.Value = Potpisnik;
            myCommand.Parameters.Add(potpisnik);

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
                throw new Exception("Neuspešan upis ");
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Unos uspešno završen", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                myConnection.Close();

                if (error)
                {
                    // Nedra.DataSet1TableAdapters.QueriesTableAdapter adapter = new Nedra.DataSet1TableAdapters.QueriesTableAdapter();
                }
            }
        }
        public void DelCIT(int ID)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "DeleteCIT";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;

            SqlParameter id = new SqlParameter();
            id.ParameterName = "@ID";
            id.SqlDbType = SqlDbType.Int;
            id.Direction = ParameterDirection.Input;
            id.Value = ID;
            myCommand.Parameters.Add(id);

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
                throw new Exception("Neuspešan upis ");
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Unos uspešno završen", "",
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
