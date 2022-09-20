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

namespace Saobracaj.Dokumenta
{
    public partial class frmTeretnicePregled : Form
    {
        public static string code = "frmTeretnicePregled";
        public bool Pravo;
        int idGrupe;
        int idForme;
        bool insert;
        bool update;
        bool delete;
        string Kor = Sifarnici.frmLogovanje.user.ToString();
        public int Teren;
        string niz = "";

        string Korisnik = "";
        public frmTeretnicePregled()
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
            }

            conn.Close();
        }
        public frmTeretnicePregled(string KorisnikPregled)
        {
            InitializeComponent();
            Korisnik = KorisnikPregled;
            IdGrupe();
            IdForme();
            PravoPristupa();
        }
        private void RefreshGV()
        {
            var select = " SELECT  top 3000 d1.ID, d1.BrojTeretnice, stanice.Opis AS StanicaOd, " +
" stanice_1.Opis AS StanicaDo, stanice_2.Opis AS StanicaPopisa, d1.VremeOd, " +
" d1.VremeDo, d1.BrojLista,( " +
" SELECT  " +
"  STUFF( " +
 "   ( " +
 "   SELECT distinct " +
 "     '/' + Cast(IDNajave as nvarchar(6)) " +
 "   FROM TeretnicaStavke " +
 "   where TeretnicaStavke.BrojTeretnice = d1.ID " +
 "   FOR XML PATH('') " +
 "   ), 1, 1, '' " +
 " ) As Skupljen) as Najave, d1.Korisnik " +
" FROM Teretnica d1 INNER JOIN  stanice ON d1.StanicaOd = stanice.ID " +
" INNER JOIN stanice AS stanice_1 ON d1.StanicaDo = stanice_1.ID " +
" INNER JOIN  stanice AS stanice_2 ON d1.StanicaPopisa = stanice_2.ID  " +
"  order by d1.ID desc";
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
            dataGridView1.Columns[0].HeaderText = "ID";
            dataGridView1.Columns[0].Width = 30;

            DataGridViewColumn column2 = dataGridView1.Columns[1];
            dataGridView1.Columns[1].HeaderText = "Voz broj";
            dataGridView1.Columns[1].Width = 100;

            DataGridViewColumn column3 = dataGridView1.Columns[2];
            dataGridView1.Columns[2].HeaderText = "Stanica Od";
            dataGridView1.Columns[2].Width = 120;

            DataGridViewColumn column4 = dataGridView1.Columns[3];
            dataGridView1.Columns[3].HeaderText = "Stanica Do";
            dataGridView1.Columns[3].Width = 120;

            DataGridViewColumn column5 = dataGridView1.Columns[4];
            dataGridView1.Columns[4].HeaderText = "Stanica Popisa";
            dataGridView1.Columns[4].Width = 120;

            DataGridViewColumn column6 = dataGridView1.Columns[5];
            dataGridView1.Columns[5].HeaderText = "Vreme Od";
            dataGridView1.Columns[5].Width = 100;

            DataGridViewColumn column7 = dataGridView1.Columns[6];
            dataGridView1.Columns[6].HeaderText = "Vreme Do";
            dataGridView1.Columns[6].Width = 100;

            DataGridViewColumn column8 = dataGridView1.Columns[7];
            dataGridView1.Columns[7].HeaderText = "Broj lista";
            dataGridView1.Columns[7].Width = 100;

            DataGridViewColumn column9 = dataGridView1.Columns[8];
            dataGridView1.Columns[8].HeaderText = "Najave";
            dataGridView1.Columns[8].Width = 150;

            DataGridViewColumn column10 = dataGridView1.Columns[9];
            dataGridView1.Columns[9].HeaderText = "Korisnik";
            dataGridView1.Columns[9].Width = 150;
        }
        private void frmTeretnicePregled_Load(object sender, EventArgs e)
        {
            RefreshGV();  
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
                       
                       
                    }
                }


            }
            catch
            {
                MessageBox.Show("Nije uspela selekcija stavki");
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            frmTeretnica ter = new frmTeretnica(txtSifra.Text,Korisnik);
            ter.Show();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            var select = " SELECT  top 100 d1.ID, d1.BrojTeretnice, stanice.Opis AS StanicaOd, " +
" stanice_1.Opis AS StanicaDo, stanice_2.Opis AS StanicaPopisa, d1.VremeOd, " +
" d1.VremeDo, d1.BrojLista,( " +
" SELECT  " +
"  STUFF( " +
"   ( " +
"   SELECT distinct " +
"     '/' + Cast(IDNajave as nvarchar(6)) " +
"   FROM TeretnicaStavke " +
"   where TeretnicaStavke.BrojTeretnice = d1.ID " +
"   FOR XML PATH('') " +
"   ), 1, 1, '' " +
" ) As Skupljen) as Najave, d1.Korisnik " +
" FROM Teretnica d1 INNER JOIN  stanice ON d1.StanicaOd = stanice.ID " +
" INNER JOIN stanice AS stanice_1 ON d1.StanicaDo = stanice_1.ID " +
" INNER JOIN  stanice AS stanice_2 ON d1.StanicaPopisa = stanice_2.ID  " +
"  order by d1.ID desc";
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
            dataGridView1.Columns[0].HeaderText = "ID";
            dataGridView1.Columns[0].Width = 30;

            DataGridViewColumn column2 = dataGridView1.Columns[1];
            dataGridView1.Columns[1].HeaderText = "Voz broj";
            dataGridView1.Columns[1].Width = 100;

            DataGridViewColumn column3 = dataGridView1.Columns[2];
            dataGridView1.Columns[2].HeaderText = "Stanica Od";
            dataGridView1.Columns[2].Width = 120;

            DataGridViewColumn column4 = dataGridView1.Columns[3];
            dataGridView1.Columns[3].HeaderText = "Stanica Do";
            dataGridView1.Columns[3].Width = 120;

            DataGridViewColumn column5 = dataGridView1.Columns[4];
            dataGridView1.Columns[4].HeaderText = "Stanica Popisa";
            dataGridView1.Columns[4].Width = 120;

            DataGridViewColumn column6 = dataGridView1.Columns[5];
            dataGridView1.Columns[5].HeaderText = "Vreme Od";
            dataGridView1.Columns[5].Width = 100;

            DataGridViewColumn column7 = dataGridView1.Columns[6];
            dataGridView1.Columns[6].HeaderText = "Vreme Do";
            dataGridView1.Columns[6].Width = 100;

            DataGridViewColumn column8 = dataGridView1.Columns[7];
            dataGridView1.Columns[7].HeaderText = "Broj lista";
            dataGridView1.Columns[7].Width = 100;

            DataGridViewColumn column9 = dataGridView1.Columns[8];
            dataGridView1.Columns[8].HeaderText = "Najave";
            dataGridView1.Columns[8].Width = 150;

            DataGridViewColumn column10 = dataGridView1.Columns[9];
            dataGridView1.Columns[9].HeaderText = "Korisnik";
            dataGridView1.Columns[9].Width = 150;

        }

        private void tsDelete_Click(object sender, EventArgs e)
        {

        }

        private void tsNew_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            var select = "Select * from Teretnica Where ID=" + Convert.ToInt32(txtSifra.Text.ToString());
            SqlConnection conn = new SqlConnection(s_connection);
            conn.Open();
            SqlCommand cmd = new SqlCommand(select, conn);
            SqlDataReader dr = cmd.ExecuteReader();
            int StanicaOD, StanicaDO, StanicaPopisa, Prijemna, Predajna, Prevozna, RN;
            int id = 0;
            string brTeretnice, brLista;
            DateTime VremeOD, VremeDO;
            while (dr.Read())
            {
                id = Convert.ToInt32(dr["ID"].ToString());
                brTeretnice = dr["BrojTeretnice"].ToString();
                StanicaOD = Convert.ToInt32(dr["StanicaOd"].ToString());
                StanicaDO = Convert.ToInt32(dr["StanicaDo"].ToString());
                StanicaPopisa = Convert.ToInt32(dr["StanicaPopisa"].ToString());
                VremeOD = Convert.ToDateTime(dr["VremeOd"].ToString());
                VremeDO = Convert.ToDateTime(dr["VremeDo"].ToString());
                brLista = dr["BrojLista"].ToString();
                Prijemna = Convert.ToInt32(dr["Prijemna"].ToString());
                Predajna = Convert.ToInt32(dr["Predajna"].ToString());
                Prevozna = Convert.ToInt32(dr["Prevozna"].ToString());
                RN = Convert.ToInt32(dr["RN"].ToString());

                InsertTeretnica ins = new InsertTeretnica();
                ins.InsTeretnica(brTeretnice, StanicaDO, StanicaOD, StanicaPopisa, VremeOD, VremeDO, brLista, Predajna, Prijemna, Korisnik, Prevozna, RN);
            }
            conn.Close();

            var select2 = "Select Max(ID) From Teretnica";
            conn.Open();
            SqlCommand cmd2 = new SqlCommand(select2, conn);
            SqlDataReader dr2 = cmd2.ExecuteReader();
            int IdPom = 0;
            while (dr2.Read())
            {
                IdPom = Convert.ToInt32(dr2[0].ToString());
            }
            conn.Close();

            var query = "Select * from TeretnicaStavke Where BrojTeretnice=" + id;
            conn.Open();
            SqlCommand cm = new SqlCommand(query, conn);
            SqlDataReader reader = cm.ExecuteReader();
            if (reader.HasRows == true)
            {
                while (reader.Read())
                {
                    int IDNajave, Uvrstena, Otkacena, Otpravna, Uputna, Uvozna, Izvozna;
                    string BrojKola, Serija, VRNP;
                    double BrojOsovina, Duzina, Tara, RucKoc;

                    IDNajave = Convert.ToInt32(reader["IDNajave"].ToString());
                    Uvrstena = Convert.ToInt32(reader["Uvrstena"].ToString());
                    Otkacena = Convert.ToInt32(reader["Otkacena"].ToString());
                    BrojKola = reader["BrojKola"].ToString();
                    Serija = reader["Serija"].ToString();
                    Otpravna = Convert.ToInt32(reader["Otpravna"].ToString());
                    Uputna = Convert.ToInt32(reader["Uputna"].ToString());
                    Uvozna = Convert.ToInt32(reader["Uvozna"].ToString());
                    Izvozna = Convert.ToInt32(reader["Izvozna"].ToString());
                    VRNP = reader["VRNP"].ToString();
                    BrojOsovina = Convert.ToDouble(reader["BrojOsovina"].ToString());
                    Duzina = Convert.ToDouble(reader["Duzina"].ToString());
                    Tara = Convert.ToDouble(reader["Tara"].ToString());
                    RucKoc = Convert.ToDouble(reader["RucKoc"].ToString());

                    InsertTeretnicaStavke ins = new InsertTeretnicaStavke();
                    ins.InsTeretnicaStavke(IdPom, IDNajave, Uvrstena, Otkacena, BrojKola, Serija, BrojOsovina, Duzina, Tara, 0, 0, 0, 0, 0, VRNP, Uputna, Otpravna, "","",
                        RucKoc, Izvozna, Uvozna, "", "");
                }
            }
            conn.Close();
            RefreshGV();
        }
    }
}
