using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Saobracaj.Dokumenta
{
    class InsertPPK
    {
        public void InsPPK(int IdStavke,string Slika,DateTime VremeZavrsetka,string Napomena)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            SqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandText = "InsertPPK";
            myCommand.CommandType = CommandType.StoredProcedure;

            SqlParameter idStavke = new SqlParameter();
            idStavke.ParameterName = "@IDStavke";
            idStavke.SqlDbType = SqlDbType.Int;
            idStavke.Direction = ParameterDirection.Input;
            idStavke.Value = IdStavke;
            myCommand.Parameters.Add(idStavke);

            SqlParameter slika = new SqlParameter();
            slika.ParameterName = "@Slika";
            slika.SqlDbType = SqlDbType.NVarChar;
            slika.Size = 500;
            slika.Direction = ParameterDirection.Input;
            slika.Value = Slika;
            myCommand.Parameters.Add(slika);

            SqlParameter vreme = new SqlParameter();
            vreme.ParameterName = "@VremeZavrsetka";
            vreme.SqlDbType = SqlDbType.DateTime;
            vreme.Direction = ParameterDirection.Input;
            vreme.Value = VremeZavrsetka;
            myCommand.Parameters.Add(vreme);

            SqlParameter napomena = new SqlParameter();
            napomena.ParameterName = "@Napomena";
            napomena.SqlDbType = SqlDbType.NVarChar;
            napomena.Size = 500;
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
                throw new Exception("Brisanje neuspešno");
            }

            finally
            {
                if (!error)
                {
                    myTransaction.Commit();
                    MessageBox.Show("Brisanje Teretnice uspešno završeno", "",
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
