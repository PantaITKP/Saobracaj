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
using System.Net;
using System.Net.Mail;


using Microsoft.Reporting.WinForms;

namespace Saobracaj.Dokumenta
{
    public partial class frmPrekovremeniRad : Form
    {
        Boolean status = false;
        public frmPrekovremeniRad()
        {
            InitializeComponent();
            IdGrupe();
            IdForme();
            PravoPristupa();
        }
        public static string code = "frmPrekovremeniRad";
        public bool Pravo;
        int idGrupe;
        int idForme;
        bool insert;
        bool update;
        bool delete;
        string Kor = Sifarnici.frmLogovanje.user.ToString();
        string niz = "";
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
        private void frmPrekovremeniRad_Load(object sender, EventArgs e)
        {
            var select3 = " select DeSifra as ID, (Rtrim(DePriimek) + ' ' + RTrim(DeIme)) as Opis from Delavci where DeSifStat <> 'P' order by opis";
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
        }

        private void tsNew_Click(object sender, EventArgs e)
        {
            status = true;
            txtSifra.Text = "";
            txtSifra.Enabled = false;
            dtpVremeOd.Value = DateTime.Now;
            dtpVremeDo.Value = DateTime.Now;
            txtNapomena.Text = "";
        }

        private void tsSave_Click(object sender, EventArgs e)
        {
            int Praznik = 0;
            if (chkRadPraznikom.Checked == true)
            {
                Praznik = 1;

            }
            DateTime OD = dtpVremeOd.Value;
            DateTime DO = dtpVremeDo.Value;
            TimeSpan span = DO.Subtract(OD);

            int ukupno = Convert.ToInt32(span.TotalHours);
            txtUkupno.Text = ukupno.ToString();

            if (status == true)
            {

                InsertPrekovremeniRad ins = new InsertPrekovremeniRad();
                ins.InsPrekovremeniRad(dtpVremeOd.Value, dtpVremeDo.Value, Convert.ToInt32(txtUkupno.Text), Convert.ToInt32(cboZaposleni.SelectedValue), txtNapomena.Text, Praznik, Kor.TrimEnd().ToString());
                status = false;
                RefreshDataGrid1();

            }
            else
            {
                InsertPrekovremeniRad upd = new InsertPrekovremeniRad();
                upd.UpdPrekovremeniRad(Convert.ToInt32(txtSifra.Text), dtpVremeOd.Value, dtpVremeDo.Value, Convert.ToInt32(txtUkupno.Text), Convert.ToInt32(cboZaposleni.SelectedValue), txtNapomena.Text, Praznik, Kor.TrimEnd().ToString());
                RefreshDataGrid1();
            }
        }

        private void tsDelete_Click(object sender, EventArgs e)
        {
            InsertPrekovremeniRad del = new InsertPrekovremeniRad();
            del.DeletePrekovremeniRad(Convert.ToInt32(txtSifra.Text));
            RefreshDataGrid1();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            RefreshDataGrid1();
        }

        private void RefreshDataGrid1()
        {
            var select = "Select PrekovremeniRad.ID,  DatumOd, DatumDo, Ukupno, Napomena,  (Rtrim(Delavci.DePriimek) + ' ' + Rtrim(Delavci.DeIme)) as Radnik," +
                "RTrim(Kreirao) as Kreirao,RadPraznikom,zaposleniID " +
                "from PrekovremeniRad " +
                "inner join Delavci on PrekovremeniRad.ZaposleniID = Delavci.DeSifra " +
                "where PrekovremeniRad.ZaposleniID = " + Convert.ToInt32(cboZaposleni.SelectedValue) + " order by id desc";

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
            dataGridView2.Columns[0].HeaderText = "ID";
            dataGridView2.Columns[0].Width = 40;



            DataGridViewColumn column3 = dataGridView2.Columns[1];
            dataGridView2.Columns[1].HeaderText = "Vreme Od";
            dataGridView2.Columns[1].Width = 100;

            DataGridViewColumn column4 = dataGridView2.Columns[2];
            dataGridView2.Columns[2].HeaderText = "Vreme Do";
            dataGridView2.Columns[2].Width = 100;

            DataGridViewColumn column5 = dataGridView2.Columns[3];
            dataGridView2.Columns[3].HeaderText = "Ukupno";
            dataGridView2.Columns[3].Width = 50;

            DataGridViewColumn column6 = dataGridView2.Columns[4];
            dataGridView2.Columns[4].HeaderText = "Napomena";
            dataGridView2.Columns[4].Width = 120;

            DataGridViewColumn column7 = dataGridView2.Columns[5];
            dataGridView2.Columns[5].HeaderText = "Zaposleni";
            dataGridView2.Columns[5].Width = 120;

            dataGridView2.Columns[8].Visible = false;

        }

        private void button2_Click(object sender, EventArgs e)
        {
            var select = "Select PrekovremeniRad.ID,  DatumOd, DatumDo, Ukupno, Napomena,  (Rtrim(Delavci.DePriimek) + ' ' + Rtrim(Delavci.DeIme)) as Radnik," +
                "RTrim(Kreirao) as Kreirao,RadPraznikom,zaposleniID " +
                "from PrekovremeniRad " +
                "inner join Delavci on PrekovremeniRad.ZaposleniID = Delavci.DeSifra order by id desc";


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
            dataGridView2.Columns[0].HeaderText = "ID";
            dataGridView2.Columns[0].Width = 40;



            DataGridViewColumn column3 = dataGridView2.Columns[1];
            dataGridView2.Columns[1].HeaderText = "Vreme Od";
            dataGridView2.Columns[1].Width = 100;

            DataGridViewColumn column4 = dataGridView2.Columns[2];
            dataGridView2.Columns[2].HeaderText = "Vreme Do";
            dataGridView2.Columns[2].Width = 100;

            DataGridViewColumn column5 = dataGridView2.Columns[3];
            dataGridView2.Columns[3].HeaderText = "Ukupno";
            dataGridView2.Columns[3].Width = 50;

            DataGridViewColumn column6 = dataGridView2.Columns[4];
            dataGridView2.Columns[4].HeaderText = "Napomena";
            dataGridView2.Columns[4].Width = 120;

            DataGridViewColumn column7 = dataGridView2.Columns[5];
            dataGridView2.Columns[5].HeaderText = "Zaposleni";
            dataGridView2.Columns[5].Width = 120;

            dataGridView2.Columns[8].Visible = false;
        }

        private void dataGridView2_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView2.Rows)
                {
                    if (row.Selected)
                    {

                        txtSifra.Text = row.Cells[0].Value.ToString();
                        dtpVremeOd.Value = Convert.ToDateTime(row.Cells[1].Value);
                        dtpVremeDo.Value = Convert.ToDateTime(row.Cells[2].Value);
                        txtUkupno.Text = row.Cells[3].Value.ToString();
                        txtNapomena.Text = row.Cells[4].Value.ToString();
                        int pom = Convert.ToInt32(row.Cells[7].Value);
                        if (pom == 1)
                        { chkRadPraznikom.Checked = true; }
                        if (pom == 0)
                        { chkRadPraznikom.Checked = false; }
                        cboZaposleni.SelectedValue = row.Cells[8].Value;

                        // txtOpis.Text = row.Cells[1].Value.ToString();
                    }
                }


            }
            catch
            {
                MessageBox.Show("Nije uspela selekcija stavki");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var select = "Select PrekovremeniRad.ID,  DatumOd, DatumDo, Ukupno, Napomena,  (Rtrim(Delavci.DePriimek) + ' ' + Rtrim(Delavci.DeIme)) as Radnik," +
                "RTrim(Kreirao) as Kreirao,RadPraznikom,zaposleniID " +
                "from PrekovremeniRad " +
                "inner join Delavci on PrekovremeniRad.ZaposleniID = Delavci.DeSifra " +
                "where RadPraznikom = 0 order by id desc";

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
            dataGridView2.Columns[0].HeaderText = "ID";
            dataGridView2.Columns[0].Width = 40;



            DataGridViewColumn column3 = dataGridView2.Columns[1];
            dataGridView2.Columns[1].HeaderText = "Vreme Od";
            dataGridView2.Columns[1].Width = 100;

            DataGridViewColumn column4 = dataGridView2.Columns[2];
            dataGridView2.Columns[2].HeaderText = "Vreme Do";
            dataGridView2.Columns[2].Width = 100;

            DataGridViewColumn column5 = dataGridView2.Columns[3];
            dataGridView2.Columns[3].HeaderText = "Ukupno";
            dataGridView2.Columns[3].Width = 50;

            DataGridViewColumn column6 = dataGridView2.Columns[4];
            dataGridView2.Columns[4].HeaderText = "Napomena";
            dataGridView2.Columns[4].Width = 120;

            DataGridViewColumn column7 = dataGridView2.Columns[5];
            dataGridView2.Columns[5].HeaderText = "Zaposleni";
            dataGridView2.Columns[5].Width = 120;

            dataGridView2.Columns[8].Visible = false;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var select = "Select PrekovremeniRad.ID,  DatumOd, DatumDo, Ukupno, Napomena,  (Rtrim(Delavci.DePriimek) + ' ' + Rtrim(Delavci.DeIme)) as Radnik," +
                "RTrim(Kreirao) as Kreirao,RadPraznikom,zaposleniID " +
                "from PrekovremeniRad " +
                "inner join Delavci on PrekovremeniRad.ZaposleniID = Delavci.DeSifra " +
                "where PrekovremeniRad.RadPraznikom=1 order by id desc";

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
            dataGridView2.Columns[0].HeaderText = "ID";
            dataGridView2.Columns[0].Width = 40;



            DataGridViewColumn column3 = dataGridView2.Columns[1];
            dataGridView2.Columns[1].HeaderText = "Vreme Od";
            dataGridView2.Columns[1].Width = 100;

            DataGridViewColumn column4 = dataGridView2.Columns[2];
            dataGridView2.Columns[2].HeaderText = "Vreme Do";
            dataGridView2.Columns[2].Width = 100;

            DataGridViewColumn column5 = dataGridView2.Columns[3];
            dataGridView2.Columns[3].HeaderText = "Ukupno";
            dataGridView2.Columns[3].Width = 50;

            DataGridViewColumn column6 = dataGridView2.Columns[4];
            dataGridView2.Columns[4].HeaderText = "Napomena";
            dataGridView2.Columns[4].Width = 120;

            DataGridViewColumn column7 = dataGridView2.Columns[5];
            dataGridView2.Columns[5].HeaderText = "Zaposleni";
            dataGridView2.Columns[5].Width = 120;

            dataGridView2.Columns[8].Visible = false;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            var select = "Select PrekovremeniRad.ID,  DatumOd, DatumDo, Ukupno, Napomena,  (Rtrim(Delavci.DePriimek) + ' ' + Rtrim(Delavci.DeIme)) as Radnik," +
                "RTrim(Kreirao) as Kreirao,RadPraznikom,zaposleniID " +
                "from PrekovremeniRad " +
                "inner join Delavci on PrekovremeniRad.ZaposleniID = Delavci.DeSifra " +
                "where DatumOd between '"+dtpVremeOd.Value.ToString("MM-dd-yyyy hh:mm:ss")+"' and '"+ dtpVremeDo.Value.ToString("MM-dd-yyyy hh:mm:ss")+"' order by DatumOd desc";

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
            dataGridView2.Columns[0].HeaderText = "ID";
            dataGridView2.Columns[0].Width = 40;



            DataGridViewColumn column3 = dataGridView2.Columns[1];
            dataGridView2.Columns[1].HeaderText = "Vreme Od";
            dataGridView2.Columns[1].Width = 100;

            DataGridViewColumn column4 = dataGridView2.Columns[2];
            dataGridView2.Columns[2].HeaderText = "Vreme Do";
            dataGridView2.Columns[2].Width = 100;

            DataGridViewColumn column5 = dataGridView2.Columns[3];
            dataGridView2.Columns[3].HeaderText = "Ukupno";
            dataGridView2.Columns[3].Width = 50;

            DataGridViewColumn column6 = dataGridView2.Columns[4];
            dataGridView2.Columns[4].HeaderText = "Napomena";
            dataGridView2.Columns[4].Width = 120;

            DataGridViewColumn column7 = dataGridView2.Columns[5];
            dataGridView2.Columns[5].HeaderText = "Zaposleni";
            dataGridView2.Columns[5].Width = 120;

            dataGridView2.Columns[8].Visible = false;
        }
    }
}

