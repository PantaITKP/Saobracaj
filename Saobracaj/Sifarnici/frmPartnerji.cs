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
    public partial class frmPartnerji : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmPartnerji";
        public bool Pravo;
        int idGrupe;
        int idForme;
        bool insert;
        bool update;
        bool delete;
        string Kor = Sifarnici.frmLogovanje.user.ToString();
        string niz = "";
        public frmPartnerji()
        {
            InitializeComponent();
            PrimeniNoviIzgled();
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
                        tsSave.Enabled = false;
                    }
                    delete = Convert.ToBoolean(reader["Brisanje"]);
                    if (delete == false)
                    {
                        tsDelete.Enabled = false;
                    }
                }
            }

            conn.Close();
        }
        private void frmPartnerji_Load(object sender, EventArgs e)
        {
            var select = " Select PaSifra, Rtrim(PaNaziv) as PaNaziv, Rtrim(UIC) as UIC, (CASE WHEN Prevoznik > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END)  as Prevoznik, (CASE WHEN Posiljalac > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END)  as Posiljalac, (CASE WHEN Primalac > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END)  as Primalac from Partnerji order by PaNaziv";
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            cboPartneri.DataSource = ds.Tables[0];
            cboPartneri.DisplayMember = "PaNaziv";
            cboPartneri.ValueMember = "PaSifra";

            /*
            var select4 = " Select Distinct PaSifra, RTrim(PaNaziv) as Partner From Partnerji";
            var s_connection4 = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection4 = new SqlConnection(s_connection4);
            var c4 = new SqlConnection(s_connection4);
            var dataAdapter4 = new SqlDataAdapter(select4, c4);

            var commandBuilder4 = new SqlCommandBuilder(dataAdapter4);
            var ds4 = new DataSet();
            dataAdapter4.Fill(ds4);
            cboPrevoznik.DataSource = ds4.Tables[0];
            cboPrevoznik.DisplayMember = "Partner";
            cboPrevoznik.ValueMember = "PaSifra";
            */


            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            DataGridViewColumn column = dataGridView1.Columns[0];
            dataGridView1.Columns[0].HeaderText = "Šifra";
            dataGridView1.Columns[0].Width = 50;

            DataGridViewColumn column2 = dataGridView1.Columns[1];
            dataGridView1.Columns[1].HeaderText = "Naziv";
            dataGridView1.Columns[1].Width = 250;

            DataGridViewColumn column3 = dataGridView1.Columns[2];
            dataGridView1.Columns[2].HeaderText = "UIC";
            dataGridView1.Columns[2].Width = 50;

            DataGridViewColumn column4 = dataGridView1.Columns[3];
            dataGridView1.Columns[3].HeaderText = "Prevoznik";
            dataGridView1.Columns[3].Width = 50;

            DataGridViewColumn column5 = dataGridView1.Columns[4];
            dataGridView1.Columns[4].HeaderText = "Pošiljalac";
            dataGridView1.Columns[4].Width = 50;

            DataGridViewColumn column6 = dataGridView1.Columns[5];
            dataGridView1.Columns[5].HeaderText = "Primalac";
            dataGridView1.Columns[5].Width = 50;

        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Selected)
                    {
                        //cboPartneri.Text = row.Cells[0].Value.ToString();
                        txtSifra.Text = row.Cells[0].Value.ToString();
                        txtOpis.Text = row.Cells[1].Value.ToString();
                        txtUIC.Text = row.Cells[2].Value.ToString();
                        chkPrevoznik.Checked = Convert.ToBoolean(row.Cells[3].Value.ToString());
                        chkPosiljalac.Checked = Convert.ToBoolean(row.Cells[4].Value.ToString());
                        chkPrimalac.Checked = Convert.ToBoolean(row.Cells[5].Value.ToString());
                       // RefreshDataGrid();
                        RefreshDataGrid2(txtSifra.Text);
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
            InsertPartnerji upd = new InsertPartnerji();
            upd.UpdPartneri(Convert.ToInt32(txtSifra.Text), txtUIC.Text, chkPrevoznik.Checked, chkPosiljalac.Checked, chkPrimalac.Checked);
            RefreshDataGrid();
        }

        private void RefreshDataGrid()
        {
            var select = " Select PaSifra, Rtrim(PaNaziv) as PaNaziv, Rtrim(UIC) as UIC, (CASE WHEN Prevoznik > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END)  as Prevoznik, (CASE WHEN Posiljalac > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END)  as Posiljalac, (CASE WHEN Primalac > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END)  as Primalac from Partnerji";
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
           
            /*
            var select4 = " Select Distinct PaSifra, RTrim(PaNaziv) as Partner From Partnerji";
            var s_connection4 = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection4 = new SqlConnection(s_connection4);
            var c4 = new SqlConnection(s_connection4);
            var dataAdapter4 = new SqlDataAdapter(select4, c4);

            var commandBuilder4 = new SqlCommandBuilder(dataAdapter4);
            var ds4 = new DataSet();
            dataAdapter4.Fill(ds4);
            cboPrevoznik.DataSource = ds4.Tables[0];
            cboPrevoznik.DisplayMember = "Partner";
            cboPrevoznik.ValueMember = "PaSifra";
            */


            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            DataGridViewColumn column = dataGridView1.Columns[0];
            dataGridView1.Columns[0].HeaderText = "Šifra";
            dataGridView1.Columns[0].Width = 50;

            DataGridViewColumn column2 = dataGridView1.Columns[1];
            dataGridView1.Columns[1].HeaderText = "Naziv";
            dataGridView1.Columns[1].Width = 250;

            DataGridViewColumn column3 = dataGridView1.Columns[2];
            dataGridView1.Columns[2].HeaderText = "UIC";
            dataGridView1.Columns[2].Width = 50;

            DataGridViewColumn column4 = dataGridView1.Columns[3];
            dataGridView1.Columns[3].HeaderText = "Prevoznik";
            dataGridView1.Columns[3].Width = 50;

            DataGridViewColumn column5 = dataGridView1.Columns[4];
            dataGridView1.Columns[4].HeaderText = "Pošiljalac";
            dataGridView1.Columns[4].Width = 50;

            DataGridViewColumn column6 = dataGridView1.Columns[5];
            dataGridView1.Columns[5].HeaderText = "Primalac";
            dataGridView1.Columns[5].Width = 50;

        }

        private void cboPartneri_Leave(object sender, EventArgs e)
        {

            /*
              var select = " Select PaSifra, Rtrim(PaNaziv) as PaNaziv, Rtrim(UIC) as UIC, (CASE WHEN Prevoznik > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END)  as Prevoznik, (CASE WHEN Posiljalac > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END)  as Posiljalac, (CASE WHEN Primalac > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END)  as Primalac from Partnerji";
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);

             */

           // var select = " Select PaSifra, Rtrim(PaNaziv) as PaNaziv, Rtrim(UIC) as UIC, (CASE WHEN Prevoznik > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END)  as Prevoznik, (CASE WHEN Posiljalac > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END)  as Posiljalac, (CASE WHEN Primalac > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END)  as Primalac from Partnerji";
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection conn = new SqlConnection(s_connection);
            conn.Open();
            SqlCommand command = new SqlCommand(" Select PaSifra, Rtrim(PaNaziv) as PaNaziv, Rtrim(UIC) as UIC, (CASE WHEN Prevoznik > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END)  as Prevoznik, (CASE WHEN Posiljalac > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END)  as Posiljalac, (CASE WHEN Primalac > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END)  as Primalac from Partnerji where PaNaziv=@NAZIV", conn);
            //
            // Add new SqlParameter to the command.
            //
            command.Parameters.AddWithValue("@NAZIV", cboPartneri.Text);
            int result = (Int32)(command.ExecuteScalar());
           SqlDataReader dr = command.ExecuteReader();
            if (dr.Read()) 
            {
                txtSifra.Text = dr[0].ToString();
                txtOpis.Text = dr[1].ToString();
                txtUIC.Text = dr[2].ToString();
                if (dr[3].ToString() == "True")
                {
                    chkPrevoznik.Checked = true;
                }
                else
                {
                    chkPrevoznik.Checked = false;
                }
                if (dr[4].ToString() == "True")
                {
                    chkPosiljalac.Checked = true;
                }
                else
                {
                    chkPosiljalac.Checked = false;
                }
                if (dr[5].ToString() == "True")
                {
                    chkPrimalac.Checked = true;
                }
                else
                {
                    chkPrimalac.Checked = false;
                }

               // dataGridView1.SelectedRows.Clear();
             /*
                foreach(DataGridViewRow row in dataGridView1.Rows) 
                {
                    if (dr[0].ToString() == row.Cells[0].Value.ToString())
                      row.Selected = true;
                   }
               */

    // read data for first record here
            }
            //RefreshDataGrid();

/*
            using (SqlDataReader reader = command.ExecuteReader())
            {
                // iterate your results here
                txtSifra.Text = String.Format("{0}", reader["PaSifra"]);
                //Console.WriteLine(String.Format("{0}", reader["id"]));

            }
 * */
            conn.Close();
        }

        private void RefreshDataGrid2(string SifraPartnera)
        {
            var select = " Select PaKoSifra, (Rtrim(PaKOPriimek) + ' ' + Rtrim(PaKoIme)) as Naziv, PaKoOddelek as Odeljenje, PaKoTel as Telefon, PaKoMail as Mail, PaKOOpomba as Napomena  from PartnerjiKOntOseba   where PaKOSifra =" + SifraPartnera;
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);

            /*
            var select4 = " Select Distinct PaSifra, RTrim(PaNaziv) as Partner From Partnerji";
            var s_connection4 = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection4 = new SqlConnection(s_connection4);
            var c4 = new SqlConnection(s_connection4);
            var dataAdapter4 = new SqlDataAdapter(select4, c4);

            var commandBuilder4 = new SqlCommandBuilder(dataAdapter4);
            var ds4 = new DataSet();
            dataAdapter4.Fill(ds4);
            cboPrevoznik.DataSource = ds4.Tables[0];
            cboPrevoznik.DisplayMember = "Partner";
            cboPrevoznik.ValueMember = "PaSifra";
            */


            dataGridView2.ReadOnly = true;
            dataGridView2.DataSource = ds.Tables[0];

            DataGridViewColumn column = dataGridView2.Columns[0];
            dataGridView2.Columns[0].HeaderText = "Šifra";
            dataGridView2.Columns[0].Width = 50;

            DataGridViewColumn column2 = dataGridView2.Columns[1];
            dataGridView2.Columns[1].HeaderText = "Naziv";
            dataGridView2.Columns[1].Width = 150;

            DataGridViewColumn column3 = dataGridView2.Columns[2];
            dataGridView2.Columns[2].HeaderText = "Odeljenje";
            dataGridView2.Columns[2].Width = 150;

            DataGridViewColumn column4 = dataGridView2.Columns[3];
            dataGridView2.Columns[3].HeaderText = "Telefon";
            dataGridView2.Columns[3].Width = 150;

            DataGridViewColumn column5 = dataGridView2.Columns[4];
            dataGridView2.Columns[4].HeaderText = "Email";
            dataGridView2.Columns[4].Width = 250;

            DataGridViewColumn column6 = dataGridView2.Columns[5];
            dataGridView2.Columns[5].HeaderText = "Napomena";
            dataGridView2.Columns[5].Width = 250;
        }

        private void cboPartneri_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            RefreshDataGrid();
        }
    }
}
