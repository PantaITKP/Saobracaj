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

namespace Saobracaj.Administracija
{
    public partial class frmDodeliGrupu : Form
    {
        string Kor = Sifarnici.frmLogovanje.user.ToString();
        public string connect = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
        bool status = false;
        int count;
        int idPom;
        string nazivPom;

        public frmDodeliGrupu()
        {
            InitializeComponent();
            RefreshGV();
            FillCombo();
        }
        public void RefreshGV()
        {
            var query = "select k.Korisnik,Rtrim(DeIme) + ' ' +Rtrim(DePriimek)as Zaposleni,k.IdGrupe,g.Naziv as 'Naziv Grupe' " +
                "From KorisnikGrupa k,GrupeKorisnik g,Delavci d,Korisnici a " +
                "Where k.IdGrupe=g.IdGrupe and a.Korisnik=k.Korisnik and a.DeSifra=d.DeSifra order by Zaposleni";
            SqlConnection conn = new SqlConnection(connect);
            SqlDataAdapter da = new SqlDataAdapter(query, conn);
            DataSet ds = new DataSet();
            da.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];
            dataGridView1.Columns[0].Width = 100;
            dataGridView1.Columns[1].Width = 180;
            dataGridView1.Columns[2].HeaderText = "ID Grupe";
            dataGridView1.Columns[2].Width = 80;
            dataGridView1.Columns[3].Width = 150;
        }
        public void FillCombo()
        {
            SqlConnection conn = new SqlConnection(connect);
            SqlDataAdapter da = new SqlDataAdapter();
            var query = "Select k.Korisnik,Rtrim(DeIme) + ' ' +Rtrim(DePriimek)as Zaposleni From Delavci d,Korisnici k Where d.DeSifra=k.DeSifra Order By DeIme";
            da = new SqlDataAdapter(query, conn);
            var ds = new DataSet();
            da.Fill(ds);
            combo_Korisnik.DataSource = ds.Tables[0];
            combo_Korisnik.DisplayMember = "Zaposleni";
            combo_Korisnik.ValueMember = "Korisnik";
           

            var query1 = "Select * From GrupeKorisnik";
            da = new SqlDataAdapter(query1, conn);
            var ds1 = new DataSet();
            da.Fill(ds1);
            combo_Grupa.DataSource = ds1.Tables[0];
            combo_Grupa.DisplayMember = "Naziv";
            combo_Grupa.ValueMember = "IdGrupe";
            
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Selected)
                    {
                        combo_Korisnik.Text = row.Cells[1].Value.ToString();
                        combo_Grupa.Text = row.Cells[3].Value.ToString();
                    }
                }
            }
            catch { }
        }

        private void tsNew_Click(object sender, EventArgs e)
        {
            status = true;
        }


        private void tsSave_Click(object sender, EventArgs e)
        {

            string query = "select Count(korisnik) as Pom from KorisnikGrupa where korisnik=" + "'" + combo_Korisnik.SelectedValue.ToString().TrimEnd() + "'";
            SqlConnection conn = new SqlConnection(connect);
            conn.Open();
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                count = Convert.ToInt32(dr["Pom"].ToString());
            }
            conn.Close();

            conn.Open();
            string query2= "Select Naziv,IdGrupe from GrupeKorisnik Where IdGrupe in" +
                "(Select IdGrupe From KorisnikGrupa Where Korisnik =" + "'" + combo_Korisnik.SelectedValue.ToString().TrimEnd() + "'"+")";
            SqlCommand cmd2 = new SqlCommand(query2, conn);
            SqlDataReader dr2 = cmd2.ExecuteReader();
            while (dr2.Read())
            {
                nazivPom = dr2["Naziv"].ToString().TrimEnd();
                idPom = Convert.ToInt32(dr2["IdGrupe"].ToString());
            }
            conn.Close();

            if (count >= 1)
            {
                DialogResult dialogResult = MessageBox.Show("Korsinik može biti u samo jednoj grupi.\nDa li želite da obrišete korisnika: "+combo_Korisnik.Text.TrimEnd()+" iz grupe "+ nazivPom.TrimEnd() +" i ubacite u novu grupu "+combo_Grupa.Text,"Izbor", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    Administracija.InsertKorisnikGrupa korisnikGrupa = new InsertKorisnikGrupa();
                    korisnikGrupa.DeleteKorisnikGrupa(combo_Korisnik.SelectedValue.ToString().TrimEnd(),idPom);
                    status = true;
                    korisnikGrupa.InsKorisnikGrupa(Convert.ToString(combo_Korisnik.SelectedValue.ToString().TrimEnd()), Convert.ToInt32(combo_Grupa.SelectedValue.ToString()));
                    status = false;
                }
                else if (dialogResult == DialogResult.No)
                {
                    //do something else
                };
                RefreshGV();
            }
            else
            {
                Administracija.InsertKorisnikGrupa korisnikGrupa = new InsertKorisnikGrupa();
                if (status == true)
                {
                    korisnikGrupa.InsKorisnikGrupa(Convert.ToString(combo_Korisnik.SelectedValue.ToString().TrimEnd()), Convert.ToInt32(combo_Grupa.SelectedValue.ToString()));
                    RefreshGV();
                    status = false;
                }
            }
        }
        private void tsDelete_Click(object sender, EventArgs e)
        {
            Administracija.InsertKorisnikGrupa korisnikGrupa = new InsertKorisnikGrupa();
            korisnikGrupa.DeleteKorisnikGrupa(combo_Korisnik.SelectedValue.ToString(), Convert.ToInt32(combo_Grupa.SelectedValue.ToString()));
            RefreshGV();
        }
    }
}
