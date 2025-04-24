using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Configuration;

namespace Saobracaj.Sifarnici
{
    public partial class frmPrugeSpisak : Form
    {
        public frmPrugeSpisak()
        {
            InitializeComponent();
        }

        private void RefreshDataGrid()
        {
            var select = " Select * from Pruga";
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            //string value = dataGridView3.Rows[0].Cells[0].Value.ToString();
            DataGridViewColumn column = dataGridView1.Columns[0];
            dataGridView1.Columns[0].HeaderText = "ID";
            dataGridView1.Columns[0].Width = 50;

            DataGridViewColumn column2 = dataGridView1.Columns[1];
            dataGridView1.Columns[1].HeaderText = "Oznaka";
            dataGridView1.Columns[1].Width = 50;

            DataGridViewColumn column3 = dataGridView1.Columns[2];
            dataGridView1.Columns[2].HeaderText = "Naziv";
            dataGridView1.Columns[2].Width = 500;

        }
        int ID,Elekt;
        string Oznaka, NazivPruge;
        private void frmPrugeSpisak_Load(object sender, EventArgs e)
        {
            RefreshDataGrid();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Selected)
                {
                    ID = Convert.ToInt32(row.Cells[0].Value.ToString());
                    Oznaka = row.Cells[1].Value.ToString().TrimEnd();
                    NazivPruge = row.Cells[2].Value.ToString().TrimEnd();
                    Elekt = Convert.ToInt32(row.Cells[3].Value.ToString());
                }
            }
            frmPruge frm = new frmPruge(ID,Oznaka,NazivPruge,Elekt);
            frm.Show();
        }
    }
}
