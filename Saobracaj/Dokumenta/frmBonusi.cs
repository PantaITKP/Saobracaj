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
using MetroFramework.Forms;
namespace Saobracaj.Dokumenta
{
    public partial class frmBonusi : Form
    {
        public frmBonusi()
        {
            InitializeComponent();
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Dodaje zaposlenog koji nije postojao, updetuje Iznos = 0, updejtuje Staz, brise koji nemaju pravo bonusa");
            InsertOsnovnaZarada ins = new InsertOsnovnaZarada();
            ins.PrebaciUBonuse();
            RefreshDataGrid();
            



        }

        private void RefreshDataGrid()
        {
           // if (txtPassword.Text != "iv4321")
           // {
           //     return;

          //  }
            var select = " Select Bonusi.[DeSifra], (Rtrim(DeLavci.DePriimek) + ' ' + RTrim(DeIme)) as Zaposleni " +
            " ,[Koef]       ,[Iznos]       ,[BrDanaTeren]       ,[RadniStaz] from Bonusi " +
           " inner join Delavci on Delavci.DeSifra = Bonusi.DeSifra ";
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly =false;
            dataGridView1.DataSource = ds.Tables[0];

        }

        private void frmBonusi_Load(object sender, EventArgs e)
        {

            var select3 = " select DeSifra as ID, (Rtrim(DePriimek) + ' ' + RTrim(DeIme)) as Opis from Delavci order by opis";
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

            RefreshDataGrid();
        }

        private void metroButton2_Click(object sender, EventArgs e)
        {
            double pomKoef = 0;
            int pomZap = 0;
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    pomZap = Convert.ToInt32(row.Cells[0].Value.ToString());
                    pomKoef = Convert.ToDouble(row.Cells[2].Value.ToString());
                    InsertOsnovnaZarada ins = new InsertOsnovnaZarada();
                    ins.IzracunajIznosBonusa(pomZap,pomKoef, Convert.ToDouble(valOsnovica.Value));
               



                }
                RefreshDataGrid();
            }
            catch
            {
                MessageBox.Show("Nije uspela promena stavki");
            }
        }

        private void metroButton5_Click(object sender, EventArgs e)
        {
            double pomKoefUkupni = 0;
            int pomZap = 0;
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                  
                    pomKoefUkupni = (pomKoefUkupni + Convert.ToDouble(row.Cells[2].Value.ToString())) ;
                   



                }
                pomKoefUkupni = pomKoefUkupni ;
                valKOefUkupni.Value = Convert.ToDecimal(pomKoefUkupni) ;
                valBruto.Value =  Convert.ToDecimal(Convert.ToDouble(valIznosZaRAspodelu.Value) / pomKoefUkupni);
                valOsnovica.Value = Convert.ToDecimal(valBruto.Value) / Convert.ToDecimal(1.6) ;
            }
            catch
            {
                MessageBox.Show("Nije uspela promena stavki");
            }
        }
    }
}
