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
    public partial class frmTrasaStanice : Form
    {
        bool status = true;
        int PrviUlazak = 0;
        public frmTrasaStanice()
        {
            InitializeComponent();
        }

        public frmTrasaStanice(string SifraTrase, string OznakaTrase)
        {
            InitializeComponent();
            txtSifra.Text = SifraTrase;
            txtVoz.Text = OznakaTrase;
            RefreshDataGrid();
        }
        private void FillStanicePruga()
        {
            //SELECT     TrasaStanice.IDTrase, TrasaStanice.RB, stanice.Opis, stanice.Kod FROM TrasaStanice INNER JOIN  PrugaStavke ON TrasaStanice.IDStanice = PrugaStavke.ID INNER JOIN stanice ON PrugaStavke.StanicaOd = stanice.ID
            var select = "select PrugaStavke.ID as ID,IDPruge as Pruga,RTrim(stanice.Opis) as Stanica,stanicaOd " +
                "From PrugaStavke " +
                "inner join stanice on PrugaStavke.StanicaOd=stanice.ID " +
                "Where IDPruge="+Convert.ToInt32(cboPruga.SelectedValue.ToString())+" order by RB asc";
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView2.ReadOnly = true;
            dataGridView2.DataSource = ds.Tables[0];

            dataGridView2.Columns[0].Width = 50;
            dataGridView2.Columns[1].Width = 50;
            dataGridView2.Columns[2].Width = 250;
            dataGridView2.Columns[3].Visible = false;
        }
        private void btnUbaci_Click(object sender, EventArgs e)
        {
            int RB = dataGridView1.RowCount;
            if (RB == 0)
            {
                RB = 1;
            }

            var selectedRows = dataGridView2.SelectedRows
                    .Cast<DataGridViewRow>()
                    .OrderBy(r => r.Index)
                    .ToList();
            InsertTrasaStanice ins = new InsertTrasaStanice();
            foreach (var row in selectedRows)
            {
                try
                {
                    ins.InsertStaniceTrase(Convert.ToInt32(txtSifra.Text.ToString()), Convert.ToInt32(row.Cells[3].Value), RB);
                    RB++;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }


            RefreshDataGrid();
        }

        private void RefreshDataGrid()
        {
            
            //SELECT     TrasaStanice.IDTrase, TrasaStanice.RB, stanice.Opis, stanice.Kod FROM TrasaStanice INNER JOIN  PrugaStavke ON TrasaStanice.IDStanice = PrugaStavke.ID INNER JOIN stanice ON PrugaStavke.StanicaOd = stanice.ID
            var select = " SELECT     TrasaStanice.RB, TrasaStanice.IDTrase, stanice.Opis, stanice.Kod FROM TrasaStanice INNER JOIN stanice ON TrasaStanice.IDStanice = stanice.ID where TrasaStanice.IdTrase = " + txtSifra.Text;
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            DataGridViewColumn column = dataGridView1.Columns[0];
            dataGridView1.Columns[0].HeaderText = "RB";
            dataGridView1.Columns[0].Width = 50;

            DataGridViewColumn column2 = dataGridView1.Columns[1];
            dataGridView1.Columns[1].HeaderText = "ID Trase";
            dataGridView1.Columns[1].Width = 80;

            DataGridViewColumn column3 = dataGridView1.Columns[2];
            dataGridView1.Columns[2].HeaderText = "Stanica";
            dataGridView1.Columns[2].Width = 250;

            DataGridViewColumn column4 = dataGridView1.Columns[3];
            dataGridView1.Columns[3].HeaderText = "Broj stanice";
            dataGridView1.Columns[3].Width = 100;
            

        }

        private void frmTrasaStanice_Load(object sender, EventArgs e)
        {
            var select = " Select Distinct ID, Oznaka, (RTrim(Oznaka) + '-' + Rtrim(Opis)) as Opis From Pruga";
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            cboPruga.DataSource = ds.Tables[0];
            cboPruga.DisplayMember = "Opis";
            cboPruga.ValueMember = "ID";
        }

        private void cboPruga_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PrviUlazak != 0)
            {
           
            }

        }

        private void cboPruga_Leave(object sender, EventArgs e)
        {

        }

        private void tsSave_Click(object sender, EventArgs e)
        {

        }

        private void tsDelete_Click(object sender, EventArgs e)
        {
            //Napraviti brisanje Trasa Stanice
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {

                    if (row.Selected)
                    {
                        InsertTrasaStanice ins = new InsertTrasaStanice();
                        ins.DelTStan( Convert.ToInt32(txtSifra.Text), Convert.ToInt32(row.Cells[0].Value.ToString()));
                       
                    }
                }
                RefreshDataGrid();

            }
            catch
            {
                MessageBox.Show("Nije uspelo brisanje stavki");
            }

          
        }

        private void cboPruga_SelectionChangeCommitted(object sender, EventArgs e)
        {
            FillStanicePruga();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int RB = dataGridView1.RowCount;
            if (RB == 0)
            {
                RB = 1;
            }


            var selectedRows = dataGridView2.SelectedRows
                    .Cast<DataGridViewRow>()
                    .OrderByDescending(r => r.Index)
                    .ToList();
            InsertTrasaStanice ins = new InsertTrasaStanice();
            foreach (var row in selectedRows)
            {
                try
                {
                    ins.InsertStaniceTrase(Convert.ToInt32(txtSifra.Text.ToString()), Convert.ToInt32(row.Cells[3].Value), RB);
                    RB++;

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            RefreshDataGrid();
        }
    }
}
