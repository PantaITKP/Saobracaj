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
using Syncfusion.Windows.Forms.Grid.Grouping;
using Syncfusion.Windows.Forms;

using Microsoft.Reporting.WinForms;
namespace Saobracaj.Sifarnici
{
    public partial class frmLokomotive : Syncfusion.Windows.Forms.Office2010Form
    {
        public static string code = "frmLokomotive";
        public bool Pravo;
        int idGrupe;
        int idForme;
        bool insert;
        bool update;
        bool delete;
        string Kor = Sifarnici.frmLogovanje.user.ToString();
        string niz = "";
        string Lokomotiva;

        public frmLokomotive()
        {
            InitializeComponent();
            IdGrupe();
            IdForme();
            PravoPristupa();
        }
        public string IdGrupe()
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            //Sifarnici.frmLogovanje frm = new Sifarnici.frmLogovanje();         
            string query = "Select IdGrupe from KorisnikGrupa Where Korisnik = " + "'" + Kor.TrimEnd() + "'";
            SqlConnection conn = new SqlConnection(s_connection);
            conn.Open();
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader dr = cmd.ExecuteReader();
            int count = 0;

            while (dr.Read())
            {
                if (count == 0)
                {
                    niz = dr["IdGrupe"].ToString();
                    count++;
                }
                else
                {
                    niz = niz + "," + dr["IdGrupe"].ToString();
                    count++;
                }

            }
            conn.Close();
            return niz;
        }
        private int IdForme()
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            string query = "Select IdForme from Forme where Rtrim(Code)=" + "'" + code + "'";
            SqlConnection conn = new SqlConnection(s_connection);
            conn.Open();
            SqlCommand cmd = new SqlCommand(query, conn);

            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                idForme = Convert.ToInt32(dr["IdForme"].ToString());
            }
            conn.Close();
            return idForme;
        }

        private void PravoPristupa()
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            string query = "Select * From GrupeForme Where IdGrupe in (" + niz + ") and IdForme=" + idForme;
            SqlConnection conn = new SqlConnection(s_connection);
            conn.Open();
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows == false)
            {
                MessageBox.Show("Nemate prava za pristup ovoj formi", code);
                Pravo = false;
            }
            else
            {
                Pravo = true;
                while (reader.Read())
                {
                    insert = Convert.ToBoolean(reader["Upis"]);
                    if (insert == false)
                    {
                        //tsNew.Enabled = false;
                    }
                    update = Convert.ToBoolean(reader["Izmena"]);
                    if (update == false)
                    {
                        //tsSave.Enabled = false;
                    }
                    delete = Convert.ToBoolean(reader["Brisanje"]);
                    if (delete == false)
                    {
                        //tsDelete.Enabled = false;
                    }
                }
            }

            conn.Close();
        }
        private void frmLokomotive_Load(object sender, EventArgs e)
        {
            RefreshDataGrid();
        }

        private void RefreshDataGrid()
        {
            var select = "";
            select = "select  SmSifra, SmNaziv, SmOpis, Password, StatusLokomotive, Dizel, SmPogDela from Mesta where Lokomotiva = 1  ";

            //  "  where  Aktivnosti.Masinovodja = 1 and Zaposleni = " + Convert.ToInt32(cboZaposleni.SelectedValue) + " order by Aktivnosti.ID desc";


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
            dataGridView1.Columns[0].HeaderText = "Lok";
            dataGridView1.Columns[0].Width = 80;

            DataGridViewColumn column1 = dataGridView1.Columns[1];
            dataGridView1.Columns[1].HeaderText = "Lok naz";
            dataGridView1.Columns[1].Width = 100;

            DataGridViewColumn column2 = dataGridView1.Columns[2];
            dataGridView1.Columns[2].HeaderText = "Lok opis";
            dataGridView1.Columns[2].Width = 150;

            DataGridViewColumn column3 = dataGridView1.Columns[3];
            dataGridView1.Columns[3].HeaderText = "Lozinka";
            dataGridView1.Columns[3].Width = 100;

            DataGridViewColumn column4 = dataGridView1.Columns[4];
            dataGridView1.Columns[4].HeaderText = "Aktivna";
            dataGridView1.Columns[4].Width = 60;

            DataGridViewColumn column5 = dataGridView1.Columns[5];
            dataGridView1.Columns[5].HeaderText = "Dizel";
            dataGridView1.Columns[5].Width = 80;

            DataGridViewColumn column6 = dataGridView1.Columns[6];
            dataGridView1.Columns[6].HeaderText = "Masa";
            dataGridView1.Columns[6].Width = 80;


        }

        private void RefreshDataGridSva()
        {
            var select = "";
            select = "select  SmSifra, SmNaziv, SmOpis, Password, StatusLokomotive, Dizel, SmPogDela, Lokomotiva, Vozilo from Mesta   ";

            //  "  where  Aktivnosti.Masinovodja = 1 and Zaposleni = " + Convert.ToInt32(cboZaposleni.SelectedValue) + " order by Aktivnosti.ID desc";


            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView2.ReadOnly = true;
            dataGridView2.DataSource = ds.Tables[0];

            DataGridViewColumn column = dataGridView2.Columns[0];
            dataGridView2.Columns[0].HeaderText = "Lok";
            dataGridView2.Columns[0].Width = 80;

            DataGridViewColumn column1 = dataGridView2.Columns[1];
            dataGridView2.Columns[1].HeaderText = "Lok naz";
            dataGridView2.Columns[1].Width = 100;

            DataGridViewColumn column2 = dataGridView2.Columns[2];
            dataGridView2.Columns[2].HeaderText = "Lok opis";
            dataGridView2.Columns[2].Width = 150;

            DataGridViewColumn column3 = dataGridView2.Columns[3];
            dataGridView2.Columns[3].HeaderText = "Lozinka";
            dataGridView2.Columns[3].Width = 100;

            DataGridViewColumn column4 = dataGridView2.Columns[4];
            dataGridView2.Columns[4].HeaderText = "Aktivna";
            dataGridView2.Columns[4].Width = 60;

            DataGridViewColumn column5 = dataGridView2.Columns[5];
            dataGridView2.Columns[5].HeaderText = "Dizel";
            dataGridView2.Columns[5].Width = 80;

            DataGridViewColumn column6 = dataGridView2.Columns[6];
            dataGridView2.Columns[6].HeaderText = "Masa";
            dataGridView2.Columns[6].Width = 80;


        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Selected)
                    {
                        txtLokomotiva.Text = row.Cells[0].Value.ToString();
                        txtPassword.Text = row.Cells[3].Value.ToString();
                        if (row.Cells[4].Value.ToString() == "1")
                        {
                            chkAktivna.Checked = true;
                        }
                        else
                        {
                            chkAktivna.Checked = false;
                        }
                        if (row.Cells[5].Value.ToString() == "1")
                        {
                            chkDizel.Checked = true;
                        }
                        else
                        {
                            chkDizel.Checked = false;
                        }
                        txtMasa.Value = Convert.ToDecimal(row.Cells[5].Value.ToString());


                    }
                }
            }
            catch
            {
                MessageBox.Show("Nije uspela selekcija stavki");
            }
        }

        private void tsSave_Click(object sender, EventArgs e)
        {

        }

        private void btnRacun_Click(object sender, EventArgs e)
        {

            int Aktivna = 0;
            int Dizel = 0;
            if (chkAktivna.Checked == true)
            {
                Aktivna = 1;
            }

            if (chkDizel.Checked == true)
            {
                Dizel = 1;
            }

            InsertLokomotive ins = new InsertLokomotive();
            ins.InsLokomotive(txtLokomotiva.Text, txtPassword.Text, Convert.ToDouble(txtMasa.Value), Dizel, Aktivna);
            RefreshDataGrid();
        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void btn_Dokumenta_Click(object sender, EventArgs e)
        {
            Lokomotiva = txtLokomotiva.Text.ToString().TrimEnd();
            Sifarnici.frmLokomotivaDokumenti lok = new frmLokomotivaDokumenti(Lokomotiva);
            if (txtLokomotiva.Text.Equals(""))
            {
                MessageBox.Show("Mora se izabrati lokomotiva");
            }
            else
            {
                lok.Show();
            }
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            RefreshDataGridSva();
        }

        private void dataGridView2_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView2.Rows)
                {
                    if (row.Selected)
                    {
                        txtLokomotiva.Text = row.Cells[0].Value.ToString();
                        txtPassword.Text = row.Cells[3].Value.ToString();
                        if (row.Cells[4].Value.ToString() == "1")
                        {
                            chkAktivna.Checked = true;
                        }
                        else
                        {
                            chkAktivna.Checked = false;
                        }
                        if (row.Cells[5].Value.ToString() == "1")
                        {
                            chkDizel.Checked = true;
                        }
                        else
                        {
                            chkDizel.Checked = false;
                        }
                        if (row.Cells[7].Value.ToString() == "1")
                        {
                            cboLokomotiva.Checked = true;
                        }
                        else
                        {
                            cboLokomotiva.Checked = false;
                        }

                        if (row.Cells[7].Value.ToString() == "1")
                        {
                            cboVozilo.Checked = true;
                        }
                        else
                        {
                            cboVozilo.Checked = false;
                        }
                        txtMasa.Value = Convert.ToDecimal(row.Cells[5].Value.ToString());


                    }
                }
            }
            catch
            {
                MessageBox.Show("Nije uspela selekcija stavki");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int Aktivna = 0;
            int Dizel = 0;
            int Lokomotiva = 0;
            int Auto = 0;
            if (chkAktivna.Checked == true)
            {
                Aktivna = 1;
            }

            if (cboLokomotiva.Checked == true)
            {
                Lokomotiva= 1;
            }

            if (chkDizel.Checked == true)
            {
                Dizel = 1;
            }
            if (cboVozilo.Checked == true)
            {
                Auto = 1;
            }
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            try
            {
                
             
                SqlCommand cmd;
                cmd = new SqlCommand("update Mesta set Lokomotiva=@a,Password=@b, Dizel =@c,Vozilo =@d  where SmSifra= '" + txtLokomotiva.Text +"'" , c);
                cmd.Parameters.AddWithValue("@a", Lokomotiva);
                cmd.Parameters.AddWithValue("@b", txtPassword.Text);
                cmd.Parameters.AddWithValue("@c", Dizel);
                cmd.Parameters.AddWithValue("@d", Auto);
                c.Open();
                int a = cmd.ExecuteNonQuery();
                if (a > 0)
                {
                    MessageBox.Show("Podaci promenjeni");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                c.Close();
                RefreshDataGridSva();
            }
        }
    }
}
