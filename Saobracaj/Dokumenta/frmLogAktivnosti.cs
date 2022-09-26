using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Drawing.Imaging;


namespace Saobracaj.Dokumenta
{
    public partial class frmLogAktivnosti : Form
    {
        public frmLogAktivnosti()
        {
            InitializeComponent();
        }

        public frmLogAktivnosti(string Sifra)
        {
            InitializeComponent();
            txtSifra.Text = Sifra;
        }

        private void frmLogAktivnosti_Load(object sender, EventArgs e)
        {
            RefreshDataGrid();
        }

        private void RefreshDataGrid()
        {
            var select = " select * from LogAktivnosti where IDAktivnosti =   " + Convert.ToInt32(txtSifra.Text) + " order by ID desc ";

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];


        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
