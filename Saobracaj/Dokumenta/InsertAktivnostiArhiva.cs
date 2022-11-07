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
    class InsertAktivnostiArhiva
    {
        public string connect = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
        public void InsAktivnostiArh(int Prvi, int Poslednji)
        {
            SqlConnection conn = new SqlConnection(connect);
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "InsertAktivnostiArhiva";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter prvi = new SqlParameter();
            prvi.ParameterName = "@IdPrvi";
            prvi.SqlDbType = SqlDbType.Int;
            prvi.Direction = ParameterDirection.Input;
            prvi.Value = Prvi;
            cmd.Parameters.Add(prvi);

            SqlParameter poslednji = new SqlParameter();
            poslednji.ParameterName = "@IdPoslednji";
            poslednji.SqlDbType = SqlDbType.Int;
            poslednji.Direction = ParameterDirection.Input;
            poslednji.Value = Poslednji;
            cmd.Parameters.Add(poslednji);

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
                throw new Exception("Neuspešan upis");
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
            }
            if (error)
            {

            }
        }
        public void InsAktivnostiStavkeArh(int Prvi, int Poslednji)
        {
            SqlConnection conn = new SqlConnection(connect);
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "InsertAktivnostiStavkeArhiva";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter prvi = new SqlParameter();
            prvi.ParameterName = "@IdPrvi";
            prvi.SqlDbType = SqlDbType.Int;
            prvi.Direction = ParameterDirection.Input;
            prvi.Value = Prvi;
            cmd.Parameters.Add(prvi);

            SqlParameter poslednji = new SqlParameter();
            poslednji.ParameterName = "@IdPoslednji";
            poslednji.SqlDbType = SqlDbType.Int;
            poslednji.Direction = ParameterDirection.Input;
            poslednji.Value = Poslednji;
            cmd.Parameters.Add(poslednji);

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
                throw new Exception("Neuspešan upis");
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
            }
            if (error)
            {

            }
        }
    }
}
