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

namespace Saobracaj.Dokumenta
{
    public partial class frmPraznici : Form
    {
        Boolean status = false;
        public frmPraznici()
        {
            InitializeComponent();
        }


        private void RefreshDataGrid()
        {
            var select = " Select ID,Naziv, DatumOd, DatumDo, Aktivan from Praznici";
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
            dataGridView1.Columns[1].HeaderText = "Naziv";
            dataGridView1.Columns[1].Width = 150;

            DataGridViewColumn column3 = dataGridView1.Columns[2];
            dataGridView1.Columns[2].HeaderText = "Datum od";
            dataGridView1.Columns[2].Width = 100;

            DataGridViewColumn column4 = dataGridView1.Columns[3];
            dataGridView1.Columns[3].HeaderText = "Datum do";
            dataGridView1.Columns[3].Width = 100;

        }

        private void tsSave_Click(object sender, EventArgs e)
        {
            int akt = 0;

            if (chkAktivan.Checked == true)
            {
                akt = 1;
            }
            
            if (status == true)
            {
                InsertPraznici ins = new InsertPraznici();
                ins.InsPrznici(txtNaziv.Text, Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value), akt);
                RefreshDataGrid();
                status = false;
            }
            else
            {
                InsertPraznici upd = new InsertPraznici();
                upd.UpdPraznici(Convert.ToInt32(txtSifra.Text), txtNaziv.Text, Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value),akt);
                status = false;
                txtSifra.Enabled = false;
                RefreshDataGrid();
            }
        }

        private void tsNew_Click(object sender, EventArgs e)
        {
            txtSifra.Text = "";
            txtSifra.Enabled = false;
       
            txtNaziv.Text = "";

            status = true;
        }

        private void tsDelete_Click(object sender, EventArgs e)
        {
            InsertPraznici del = new InsertPraznici();
            del.DeletePraznici(Convert.ToInt32(txtSifra.Text));
            status = false;
            txtSifra.Enabled = false;
            RefreshDataGrid();
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {

                    if (row.Selected)
                    {
                        txtSifra.Text = row.Cells[0].Value.ToString();
                        txtNaziv.Text = row.Cells[1].Value.ToString();
                        dtpVremeOd.Value = Convert.ToDateTime(row.Cells[2].Value.ToString());
                        dtpVremeDo.Value = Convert.ToDateTime(row.Cells[3].Value.ToString());
                        if (row.Cells[4].Value.ToString() == "1")
                        {
                            chkAktivan.Checked = true;
                        }
                        else
                        {
                            chkAktivan.Checked = false;
                        }
                    }
                   
                }
            }
            catch
            {
                MessageBox.Show("Nije uspela promena stavki");
            }
        }

        private void frmPraznici_Load(object sender, EventArgs e)
        {
         RefreshDataGrid();
        }
    }
}






