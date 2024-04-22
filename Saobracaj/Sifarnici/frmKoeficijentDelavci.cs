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

using Microsoft.Reporting.WinForms;

namespace Saobracaj.Sifarnici
{
    public partial class frmKoeficijentDelavci : Form
    {
        bool status = false;
        public frmKoeficijentDelavci()
        {
            InitializeComponent();
        }

        private void tsNew_Click(object sender, EventArgs e)
        {
            status = true;
        }

        private void tsSave_Click(object sender, EventArgs e)
        {
      

            if (status == true)
            {
                InsertEvidencijaSatiPoRadniku ins = new InsertEvidencijaSatiPoRadniku();
                ins.InsKoeficijentPoRadniku(Convert.ToInt32(cboZaposleni.SelectedValue),  Convert.ToInt32(txtCena.Text));
                status = false;
            }
            else
            {
                InsertEvidencijaSatiPoRadniku upd = new InsertEvidencijaSatiPoRadniku();
                upd.UpdKoefPoRadniku(Convert.ToInt32(cboZaposleni.SelectedValue), Convert.ToInt32(txtCena.Text));
            }
            RefreshDataGRid();
        }

        private void RefreshDataGRid()
        {
           



            var select = "  Select DelavciKoeficijenti.DeSifra as ID, (Rtrim(DePriimek) + ' ' + RTrim(DeIme)) as Opis, Koeficijent from " +
                " DelavciKoeficijenti inner join Delavci on Delavci.DeSifra = DelavciKoeficijenti.DeSifra order by opis";

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
            dataGridView1.Columns[0].HeaderText = "Šifra";
            dataGridView1.Columns[0].Width = 50;

            DataGridViewColumn column2 = dataGridView1.Columns[1];
            dataGridView1.Columns[1].HeaderText = "Zaposleni";
            dataGridView1.Columns[1].Width = 350;

            DataGridViewColumn column3 = dataGridView1.Columns[2];
            dataGridView1.Columns[2].HeaderText = "Koeficijent";
            dataGridView1.Columns[2].Width = 50;


            

        }

        private void frmKoeficijentDelavci_Load(object sender, EventArgs e)
        {
            var select3 = " select DeSifra as ID, (Rtrim(DePriimek) + ' ' + RTrim(DeIme)) as Opis  from " +
               " Delavci order by opis";
            var s_connection3 = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection3 = new SqlConnection(s_connection3);
            var c3 = new SqlConnection(s_connection3);
            var dataAdapter3 = new SqlDataAdapter(select3, c3);

            var commandBuilder3 = new SqlCommandBuilder(dataAdapter3);
            var ds3 = new DataSet();
            dataAdapter3.Fill(ds3);
            cboZaposleni.DataSource = ds3.Tables[0];
            cboZaposleni.DisplayMember = "Opis";
            cboZaposleni.ValueMember = "ID";

            RefreshDataGRid();
        }
        private void VratiPodatke(int Zaposleni)
        {
            bool temp = false;

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection con = new SqlConnection(s_connection);

            con.Open();

            SqlCommand cmd = new SqlCommand("select * from DelavciKoeficijenti where DeSifra=" + Zaposleni, con);
            SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                txtCena.Text = dr["Koeficijent"].ToString();
                /*
                StanicaOD.SelectedValue = Convert.ToInt32(dr["StanicaOd"].ToString());
                stanicaDO.SelectedValue = Convert.ToInt32(dr["StanicaDo"].ToString());
               
                RastojanjeM.Value = Convert.ToInt32(dr["RastojanjeM"].ToString());
                StacionazaKM.Value = Convert.ToInt32(dr["StacionazaKM"].ToString());
                StacionazaM.Value = Convert.ToInt32(dr["StacionazaM"].ToString());
                VmaxL.Value = Convert.ToInt32(dr["VmaxL"].ToString());
                VmaxD.Value = Convert.ToInt32(dr["VmaxD"].ToString());
                OsOtpor.Text = dr["OsOtpor"].ToString();
                Vaga.Text = dr["Vaga"].ToString();
                PutTer.Text = dr["PutTer"].ToString();
                Nagib.Value = Convert.ToDecimal(dr["Nagib"].ToString());
                MNU.Value = Convert.ToDecimal(dr["MNU"].ToString());
                MNP.Value = Convert.ToDecimal(dr["MNP"].ToString());
             */



            }

   
            con.Close();
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {

                    if (row.Selected)
                    {
                       cboZaposleni.SelectedValue = Convert.ToInt32(row.Cells[0].Value.ToString());
                        VratiPodatke(Convert.ToInt32(row.Cells[0].Value.ToString()));

                    }
                }
            }
            catch
            {
                MessageBox.Show("Nije uspela promena stavki");
            }
        }

        private void tsDelete_Click(object sender, EventArgs e)
        {
            InsertEvidencijaSatiPoRadniku ins = new InsertEvidencijaSatiPoRadniku();
            ins.DeleteEvidencijaKoefRadnik(Convert.ToInt32(cboZaposleni.SelectedValue));
            RefreshDataGRid();
            status = false;
        }
    }
}
