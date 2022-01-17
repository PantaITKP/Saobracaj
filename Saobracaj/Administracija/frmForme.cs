﻿using System;
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

namespace Saobracaj.Administracija
{
    public partial class frmForme : Form
    {
        public static string code = "Test";
        int idGrupe;
        int idForme;
        bool insert;
        bool update;
        bool delete;
        string Kor = Sifarnici.frmLogovanje.user.ToString();
        public string connect = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
        bool status = false;
        public frmForme()
        {
            InitializeComponent();
            txt_Sifra.Enabled = false;
        }

        public int IdGrupe()
        {
            //Sifarnici.frmLogovanje frm = new Sifarnici.frmLogovanje();         
            string query = "Select IdGrupe from KorisnikGrupa Where Korisnik = " +"'" +Kor.TrimEnd()+"'";
            SqlConnection conn = new SqlConnection(connect);
            conn.Open();
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                idGrupe = Convert.ToInt32(dr["IdGrupe"].ToString());
            }
            conn.Close();
            return idGrupe;
        }
        private int IdForme()
        {
            string query = "Select IdForme from Forme where Rtrim(Code)=" + "'"+code+"'";
            SqlConnection conn = new SqlConnection(connect);
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
            string query = "Select * From GrupeForme Where IdGrupe=" + idGrupe + " and IdForme=" + idForme;
            SqlConnection conn = new SqlConnection(connect);
            conn.Open();
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows == false)
            {
                MessageBox.Show("Nemate prava za pristup ovoj formi");
                this.Close();
            }
            while (reader.Read())
            {
                insert = Convert.ToBoolean(reader["Upis"]);
                if (insert == false)
                {
                    tsNew.Enabled = false;
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
            conn.Close();
        }
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            Administracija.frmFormePrava formePrava = new frmFormePrava();
            formePrava.Show();
        }

        private void frmForme_Load(object sender, EventArgs e)
        {
            RefreshDG();
            IdGrupe();
            IdForme();
            
            
            PravoPristupa();
        }
        private void RefreshDG()
        {
            var query = "select * from Forme";
            SqlConnection conn = new SqlConnection(connect);
            SqlDataAdapter da = new SqlDataAdapter(query, conn);
            DataSet ds = new DataSet();
            da.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];
            dataGridView1.Columns[0].HeaderText = "ID Forme";
            dataGridView1.Columns[0].Width = 55;
            dataGridView1.Columns[1].HeaderText = "Naziv Forme";
            dataGridView1.Columns[1].Width = 180;
            dataGridView1.Columns[2].HeaderText = "Code Forme";
            dataGridView1.Columns[2].Width = 150;
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Selected)
                    {
                        txt_Sifra.Text = row.Cells[0].Value.ToString();
                        txt_NazivForme.Text = row.Cells[1].Value.ToString();
                        txt_Code.Text = row.Cells[2].Value.ToString();
                    }
                }
            }
            catch
            {
            }
        }

        private void tsNew_Click(object sender, EventArgs e)
        {
            txt_Sifra.Text = "";
            status = true;
        }

        private void tsSave_Click(object sender, EventArgs e)
        {

            InsertForme forme = new InsertForme();
            if (status == true)
            {
                forme.InsForme(txt_NazivForme.Text.TrimEnd(), txt_Code.Text.TrimEnd());
                RefreshDG();
                status = false;
            }
            else
            {
                forme.UpdateForme(Convert.ToInt32(txt_Sifra.Text.TrimEnd().ToString()), txt_NazivForme.Text.TrimEnd(), txt_Code.Text.TrimEnd());
                RefreshDG();
            }
        }

        private void tsDelete_Click(object sender, EventArgs e)
        {
            InsertForme forme = new InsertForme();
            forme.DeleteForme(Convert.ToInt32(txt_Sifra.Text));
            RefreshDG();
        }
    }
}
