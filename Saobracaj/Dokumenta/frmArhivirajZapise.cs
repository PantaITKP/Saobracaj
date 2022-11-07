using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Saobracaj.Dokumenta
{
    public partial class frmArhivirajZapise : Form
    {
        public frmArhivirajZapise()
        {
            InitializeComponent();

        }

        private void ProcitajZapise()
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;  
            string query = "Select Min(ID) as idPrvi, Max(ID) as idPoslednji from Aktivnosti";
            SqlConnection conn = new SqlConnection(s_connection);
            conn.Open();
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader dr = cmd.ExecuteReader();
            int prvi=0;
            int poslednji = 0;

            while (dr.Read())
            {
                prvi = Convert.ToInt32(dr["idPrvi"].ToString());
                poslednji = Convert.ToInt32(dr["idPoslednji"].ToString());
            }
            txtPrvi.Text = prvi.ToString();
            txt_Poslednji.Text = poslednji.ToString();
            conn.Close();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            InsertAktivnostiArhiva ins = new InsertAktivnostiArhiva();

            ins.InsAktivnostiStavkeArh(Convert.ToInt32(txtPrvi.Text), Convert.ToInt32(txt_Poslednji.Text));
            ins.InsAktivnostiArh(Convert.ToInt32(txtPrvi.Text), Convert.ToInt32(txt_Poslednji.Text));

            MessageBox.Show("Arhivirano");
        }

        private void frmArhivirajZapise_Load(object sender, EventArgs e)
        {
            ProcitajZapise();
        }
    }
}
