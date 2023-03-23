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
    public partial class frmVrstaAktivnostiArhiv : Form
    {
        public string connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;

        public frmVrstaAktivnostiArhiv()
        {
            InitializeComponent();
        }

        private void frmVrstaAktivnostiArhiv_Load(object sender, EventArgs e)
        {
            var planutovara = "select ID, Naziv from CenovnikVrstaAktivnosti " +
           " order by CenovnikVrstaAktivnosti.ID desc";
            var planutovaraSAD = new SqlDataAdapter(planutovara, connection);
            var planutovaraSDS = new DataSet();
            planutovaraSAD.Fill(planutovaraSDS);

             cboCenovnik.DataSource = planutovaraSDS.Tables[0];
            cboCenovnik.DisplayMember = "Naziv";
            cboCenovnik.ValueMember = "ID";
        }

        private void btn_dani_Click(object sender, EventArgs e)
        {
           
               
                    InsertCenovnikVrstaAktivnosti ins = new InsertCenovnikVrstaAktivnosti();
                    ins.PrenesiCenovnikVrstaAktivnosti(Convert.ToInt32(cboCenovnik.SelectedValue));
         
           // RefreshDataGrid1();
           // RefreshDataGrid2();
        }

        private void RefreshDataGrid()
        {
            //
            //    ,PotrebanRazlog = @PotrebanRazlog
            // ,PotrebanNalogodavac = @PotrebanNalogodavac
            //,PotrebnoVozilo = @PotrebnoVozilo
            //,ObaveznaNapomena = @ObaveznaNapomena
            //
            var select = " Select ID, Naziv, " +
            " CASE WHEN ObracunPoSatu > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as obracunPoSatu, " +
            " CASE WHEN PotrebanRazlog > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as PotrebanRazlog, " +
            " CASE WHEN PotrebanNalogodavac> 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as PotrebanNalogodavac, " +
            " CASE WHEN PotrebnoVozilo> 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as PotrebnoVozilo, " +
            " CASE WHEN ObaveznaNapomena> 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as ObaveznaNapomena, " +
            " Cena, Opis , " +
            " CASE WHEN Smederevo> 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Smederevo, " +
            " CASE WHEN Kragujevac> 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Kragujevac, " +
            " CASE WHEN CG> 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as CG, " +
            " CASE WHEN Remont> 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Remont, " +
             " CASE WHEN Milsped> 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Milsped, " +
              " CASE WHEN Dnevnica> 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as UlaziUDnevnicu, MaxSati, MaxVagona " +
            " from VrstaAktivnostiArhiv where CenovnikID = " + Convert.ToInt32(cboCenovnik.SelectedValue);
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
            dataGridView1.Columns[1].Width = 350;

            DataGridViewColumn column3 = dataGridView1.Columns[2];
            dataGridView1.Columns[2].HeaderText = "Obračun po satu";
            dataGridView1.Columns[2].Width = 50;

            DataGridViewColumn column4 = dataGridView1.Columns[3];
            dataGridView1.Columns[3].HeaderText = "Potreban razlog";
            dataGridView1.Columns[3].Width = 50;

            DataGridViewColumn column5 = dataGridView1.Columns[4];
            dataGridView1.Columns[4].HeaderText = "Potreban nalogodavac";
            dataGridView1.Columns[4].Width = 50;

            DataGridViewColumn column6 = dataGridView1.Columns[5];
            dataGridView1.Columns[5].HeaderText = "Potrebno vozilo";
            dataGridView1.Columns[5].Width = 50;

            DataGridViewColumn column7 = dataGridView1.Columns[6];
            dataGridView1.Columns[6].HeaderText = "Obavezna napomena";
            dataGridView1.Columns[6].Width = 50;



            DataGridViewColumn column8 = dataGridView1.Columns[7];
            dataGridView1.Columns[7].HeaderText = "Cena";
            dataGridView1.Columns[7].Width = 50;

            DataGridViewColumn column9 = dataGridView1.Columns[8];
            dataGridView1.Columns[8].HeaderText = "Opis";
            dataGridView1.Columns[8].Width = 500;

            DataGridViewColumn column10 = dataGridView1.Columns[9];
            dataGridView1.Columns[9].HeaderText = "Smederevo";
            dataGridView1.Columns[9].Width = 50;

            DataGridViewColumn column11 = dataGridView1.Columns[10];
            dataGridView1.Columns[10].HeaderText = "Kragujevac";
            dataGridView1.Columns[10].Width = 50;

            DataGridViewColumn column12 = dataGridView1.Columns[11];
            dataGridView1.Columns[11].HeaderText = "CG";
            dataGridView1.Columns[11].Width = 50;

            DataGridViewColumn column13 = dataGridView1.Columns[12];
            dataGridView1.Columns[12].HeaderText = "Remont";
            dataGridView1.Columns[12].Width = 50;

            DataGridViewColumn column14 = dataGridView1.Columns[13];
            dataGridView1.Columns[13].HeaderText = "Milsped";
            dataGridView1.Columns[13].Width = 50;

            DataGridViewColumn column15 = dataGridView1.Columns[14];
            dataGridView1.Columns[14].HeaderText = "Dnevnica";
            dataGridView1.Columns[14].Width = 50;


            DataGridViewColumn column16 = dataGridView1.Columns[15];
            dataGridView1.Columns[15].HeaderText = "MaxSATI";
            dataGridView1.Columns[15].Width = 50;

            DataGridViewColumn column17 = dataGridView1.Columns[16];
            dataGridView1.Columns[16].HeaderText = "MaxVagona";
            dataGridView1.Columns[16].Width = 50;
        }


        private void button1_Click(object sender, EventArgs e)
        {
            RefreshDataGrid();
        }

        private void button2_Click(object sender, EventArgs e)
        {
        
               
                    InsertCenovnikVrstaAktivnosti ins = new InsertCenovnikVrstaAktivnosti();
                    ins.PrenesiCenovnikVrstaAktivnostiUTekuci(Convert.ToInt32(cboCenovnik.SelectedValue));
               
           
        }
    }
}
