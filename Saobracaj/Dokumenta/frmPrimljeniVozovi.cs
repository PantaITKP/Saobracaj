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
    public partial class frmPrimljeniVozovi : Form
    {
        public frmPrimljeniVozovi()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string DatumOD = dateTimePicker1.Value.ToString("yyyy-MM-dd");
            string DatumDo = dateTimePicker2.Value.ToString("yyyy-MM-dd");
            var select = "Select Najava.ID,stanice1.Opis as [Uputna],stanice2.Opis as [Otpravna],StvarnoPrimanje,StvarnaPredaja,Tezina,BrojKola " +
                "From Najava " +
                "inner join stanice as stanice1 on Najava.Uputna = stanice1.ID " +
                "inner join stanice as stanice2 on Najava.Otpravna = stanice2.ID " +
                "Where(StvarnoPrimanje between '" +DatumOD + "' and '" + DatumDo + "') and(status = 4 or status = 5 or status = 7 or status = 9)";

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = false;
            dataGridView1.DataSource = ds.Tables[0];
        }
    }
}
