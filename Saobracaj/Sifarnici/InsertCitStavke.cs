using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data;

namespace Saobracaj.Sifarnici
{
    class InsertCitStavke
    {
        public void InsCITStavke(int IDCit, int BR, string BrojVagona, double Masa, double Tara, double Bruto, string VrstaRobe, string RID, string Napomena)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "InsertCITStavke";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;

            SqlParameter idC = new SqlParameter();
            idC.ParameterName = "@IDCit";
            idC.SqlDbType = SqlDbType.Int;
            idC.Direction = ParameterDirection.Input;
            idC.Value = IDCit;
            myCommand.Parameters.Add(idC);

            SqlParameter br = new SqlParameter();
            br.ParameterName = "@BR";
            br.SqlDbType = SqlDbType.Int;
            br.Direction = ParameterDirection.Input;
            br.Value = BR;
            myCommand.Parameters.Add(br);

            SqlParameter brVagona = new SqlParameter();
            brVagona.ParameterName = "@BrojVagona";
            brVagona.SqlDbType = SqlDbType.NVarChar;
            brVagona.Size = 50;
            brVagona.Direction = ParameterDirection.Input;
            brVagona.Value = BrojVagona;
            myCommand.Parameters.Add(brVagona);

            SqlParameter masa = new SqlParameter();
            masa.ParameterName = "@Masa";
            masa.SqlDbType = SqlDbType.Decimal;
            masa.Direction = ParameterDirection.Input;
            masa.Value = Masa;
            myCommand.Parameters.Add(masa);

            SqlParameter tara = new SqlParameter();
            tara.ParameterName = "@Tara";
            tara.SqlDbType = SqlDbType.Decimal;
            tara.Direction = ParameterDirection.Input;
            tara.Value = Tara;
            myCommand.Parameters.Add(tara);

            SqlParameter bruto = new SqlParameter();
            bruto.ParameterName = "@Bruto";
            bruto.SqlDbType = SqlDbType.Decimal;
            bruto.Direction = ParameterDirection.Input;
            bruto.Value = Bruto;
            myCommand.Parameters.Add(bruto);

            SqlParameter vrsta = new SqlParameter();
            vrsta.ParameterName = "@VrstaRobe";
            vrsta.SqlDbType = SqlDbType.NVarChar;
            vrsta.Size = 100;
            vrsta.Direction = ParameterDirection.Input;
            vrsta.Value = VrstaRobe;
            myCommand.Parameters.Add(vrsta);

            SqlParameter rid = new SqlParameter();
            rid.ParameterName = "@RID";
            rid.SqlDbType = SqlDbType.NVarChar;
            rid.Size = 50;
            rid.Direction = ParameterDirection.Input;
            rid.Value = RID;
            myCommand.Parameters.Add(rid);

            SqlParameter napomena = new SqlParameter();
            napomena.ParameterName = "@Napomena";
            napomena.SqlDbType = SqlDbType.NVarChar;
            napomena.Size = 100;
            napomena.Direction = ParameterDirection.Input;
            napomena.Value = Napomena;
            myCommand.Parameters.Add(napomena);

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
        public void UpdCITStavke(int ID,int IDCit, int BR, string BrojVagona, double Masa, double Tara, double Bruto, string VrstaRobe, string RID, string Napomena)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "UpdateCITStavke";
            myCommand.CommandType = System.Data.CommandType.StoredProcedure;

            SqlParameter id = new SqlParameter();
            id.ParameterName = "@ID";
            id.SqlDbType = SqlDbType.Int;
            id.Direction = ParameterDirection.Input;
            id.Value = ID;
            myCommand.Parameters.Add(id);

            SqlParameter idC = new SqlParameter();
            idC.ParameterName = "@IDCit";
            idC.SqlDbType = SqlDbType.Int;
            idC.Direction = ParameterDirection.Input;
            idC.Value = IDCit;
            myCommand.Parameters.Add(idC);

            SqlParameter br = new SqlParameter();
            br.ParameterName = "@BR";
            br.SqlDbType = SqlDbType.Int;
            br.Direction = ParameterDirection.Input;
            br.Value = BR;
            myCommand.Parameters.Add(br);

            SqlParameter brVagona = new SqlParameter();
            brVagona.ParameterName = "@BrojVagona";
            brVagona.SqlDbType = SqlDbType.NVarChar;
            brVagona.Size = 50;
            brVagona.Direction = ParameterDirection.Input;
            brVagona.Value = BrojVagona;
            myCommand.Parameters.Add(brVagona);

            SqlParameter masa = new SqlParameter();
            masa.ParameterName = "@Masa";
            masa.SqlDbType = SqlDbType.Decimal;
            masa.Direction = ParameterDirection.Input;
            masa.Value = Masa;
            myCommand.Parameters.Add(masa);

            SqlParameter tara = new SqlParameter();
            tara.ParameterName = "@Tara";
            tara.SqlDbType = SqlDbType.Decimal;
            tara.Direction = ParameterDirection.Input;
            tara.Value = Tara;
            myCommand.Parameters.Add(tara);

            SqlParameter bruto = new SqlParameter();
            bruto.ParameterName = "@Bruto";
            bruto.SqlDbType = SqlDbType.Decimal;
            bruto.Direction = ParameterDirection.Input;
            bruto.Value = Bruto;
            myCommand.Parameters.Add(bruto);

            SqlParameter vrsta = new SqlParameter();
            vrsta.ParameterName = "@VrstaRobe";
            vrsta.SqlDbType = SqlDbType.NVarChar;
            vrsta.Size = 100;
            vrsta.Direction = ParameterDirection.Input;
            vrsta.Value = VrstaRobe;
            myCommand.Parameters.Add(vrsta);

            SqlParameter rid = new SqlParameter();
            rid.ParameterName = "@RID";
            rid.SqlDbType = SqlDbType.NVarChar;
            rid.Size = 50;
            rid.Direction = ParameterDirection.Input;
            rid.Value = RID;
            myCommand.Parameters.Add(rid);

            SqlParameter napomena = new SqlParameter();
            napomena.ParameterName = "@Napomena";
            napomena.SqlDbType = SqlDbType.NVarChar;
            napomena.Size = 100;
            napomena.Direction = ParameterDirection.Input;
            napomena.Value = Napomena;
            myCommand.Parameters.Add(napomena);

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
        public void DelCITStavke(int ID)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "DeleteCITStavke";
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
