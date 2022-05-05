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
    public partial class frmPPKGlavna : Form
    {
        int zaposleniID;
        public frmPPKGlavna()
        {
            InitializeComponent();
            FillGV();
            txt_ID.Enabled = false;
            txt_Zaposleni.Enabled = false;
        }
        private void FillGV()
        {
            var select = "Select TOP 1000 IDNadredjena as ID,(Rtrim(DePriimek) + ' ' + RTrim(DeIme)) as Zaposleni,Aktivnosti.VremeOD,Aktivnosti.VremeDo,Aktivnosti.Opis,Napomena,Razlog," +
                "DatumZavrsetka,Aktivnosti.Zaposleni " +
                "From AktivnostiStavke " +
                "Inner join Aktivnosti on AktivnostiStavke.IDNadredjena = Aktivnosti.ID " +
                "Inner join Delavci on Aktivnosti.Zaposleni = Delavci.DeSifra " +
                "Where VrstaAktivnostiID = 9 order by Aktivnosti.ID desc";

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            dataGridView1.Columns[0].Width = 60;
            dataGridView1.Columns[1].Width = 120;
            dataGridView1.Columns[4].Width = 180;
            dataGridView1.Columns[6].Width = 120;
            //dataGridView1.Columns[8].Visible = false;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btn_PPK_Click(object sender, EventArgs e)
        {
            
            if (txt_ID.Text == "")
            {
                MessageBox.Show("Mora se izabrati zapis");
            }
            else
            {
                Dokumenta.frmPPK ppk = new frmPPK(Convert.ToInt32(txt_ID.Text), zaposleniID);
                ppk.Show();
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Selected)
                    {
                        txt_Zaposleni.Text = row.Cells[1].Value.ToString().TrimEnd();
                        txt_ID.Text = row.Cells[0].Value.ToString();
                        zaposleniID = Convert.ToInt32(row.Cells[8].Value.ToString());
                    }
                }
            }
            catch
            {
                MessageBox.Show("Nije uspela selekcija stavki");
            }
        }
    }
}
