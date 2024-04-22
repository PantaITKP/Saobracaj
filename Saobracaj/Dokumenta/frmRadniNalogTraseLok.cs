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

namespace Saobracaj.Dokumenta
{
    public partial class frmRadniNalogTraseLok : Form
    {
        bool status = false;
        int pomTrasa = 0;

        public frmRadniNalogTraseLok()
        {
            InitializeComponent();
        }

        public frmRadniNalogTraseLok(string RadniNalog, string Voznja, Int32 Trasa)
        {
            InitializeComponent();
            txtSifraRN.Text = RadniNalog;
            txtBrojNajave.Text = Voznja;
            txtRB.Text = Voznja;
            //Ubaciti popunjavanje trasa
            pomTrasa = Trasa;
            
        }

        private void VratiLokomotive(int IDTrase, string IDRadnogNaloga)
        {
            var select = " SELECT IDRadnogNaloga, IDTrase, SmSifra,  CASE WHEN Vucna > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Vucna , Komentar FROM RadniNalogLokNaTrasi where IDRadnogNaloga =" + Convert.ToInt32(IDRadnogNaloga) + " and IDTrase = " + IDTrase;

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
            dataGridView2.Columns[0].HeaderText = "RN";
            dataGridView2.Columns[0].Width = 40;

            DataGridViewColumn column2 = dataGridView2.Columns[1];
            dataGridView2.Columns[1].HeaderText = "Trasa";
            dataGridView2.Columns[1].Width = 60;

            DataGridViewColumn column3 = dataGridView2.Columns[2];
            dataGridView2.Columns[2].HeaderText = "Lokomotiva";
            dataGridView2.Columns[2].Width = 60;

            DataGridViewColumn column4 = dataGridView2.Columns[3];
            dataGridView2.Columns[3].HeaderText = "Vučna";
            dataGridView2.Columns[3].Width = 40;

            DataGridViewColumn column5 = dataGridView2.Columns[4];
            dataGridView2.Columns[4].HeaderText = "Napomena";
            dataGridView2.Columns[4].Width = 460;
            
            
       
        }

        private void tsNew_Click(object sender, EventArgs e)
        {   
            chkRezi.Checked = false;
            chkPoslato.Checked = false;
            txtPlaniranaMasa.Value=0;
            txtDuzinaVoza.Value = 0;
            txtMasaVoza.Value = 0;
            txtBrutoVoza.Value = 0;
            txtNapomena.Text = "";
            dtpVremeDo.Value = Convert.ToDateTime("01.01.1900.".ToString());
            dtpVremeOdReal.Value = Convert.ToDateTime("01.01.1900.".ToString());
            dtpVremeOd.Value = Convert.ToDateTime("01.01.1900.".ToString());
            dtpVremeDoReal.Value = Convert.ToDateTime("01.01.1900.".ToString());
            txtMasaLokomotive.Value = 0;
            cboTrase.Text = "";
            cboStanicaDo.Text = "";
            cboStanicaOd.Text = "";
            txtRB.Text = "";
            txtVreme.Text = "";
            txtVremeReal.Text = "";

        }

        private void frmRadniNalogTraseLok_Load(object sender, EventArgs e)
        {
            var select = " Select ID, (Rtrim(Voz) + '-' + Rtrim(Relacija)) as Opis from Trase order by Voz ";
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            cboTrase.DataSource = ds.Tables[0];
            cboTrase.DisplayMember = "Opis";
            cboTrase.ValueMember = "ID";

            cboTrase.SelectedValue = pomTrasa;

            var select2 = " Select SmSifra, SmSifra as Opis from Mesta";
            var s_connection2 = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection2 = new SqlConnection(s_connection2);
            var c2 = new SqlConnection(s_connection2);
            var dataAdapter2 = new SqlDataAdapter(select2, c2);

            /*
            var commandBuilder2 = new SqlCommandBuilder(dataAdapter2);
            var ds2 = new DataSet();
            dataAdapter2.Fill(ds2);
            cboLokomotiva.DataSource = ds2.Tables[0];
            cboLokomotiva.DisplayMember = "Opis";
            cboLokomotiva.ValueMember = "SmSifra";
            */
            var select9 = " Select Distinct ID, RTrim(Opis) as Razlog From Razlozi";
            var s_connection9 = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection9 = new SqlConnection(s_connection9);
            var c9 = new SqlConnection(s_connection9);
            var dataAdapter9 = new SqlDataAdapter(select9, c9);

            var select6 = " Select ID, RTrim(Opis) as Stanica From Stanice order by opis";
            var s_connection6 = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection6 = new SqlConnection(s_connection6);
            var c6 = new SqlConnection(s_connection6);
            var dataAdapter6 = new SqlDataAdapter(select6, c6);

            var commandBuilder6 = new SqlCommandBuilder(dataAdapter6);
            var ds6 = new DataSet();
            dataAdapter6.Fill(ds6);
            cboStanicaOd.DataSource = ds6.Tables[0];
            cboStanicaOd.DisplayMember = "Stanica";
            cboStanicaOd.ValueMember = "ID";

            var select7 = " Select ID, RTrim(Opis) as Stanica From Stanice order by opis";
            var s_connection7 = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection7 = new SqlConnection(s_connection7);
            var c7 = new SqlConnection(s_connection7);
            var dataAdapter7 = new SqlDataAdapter(select7, c7);

            var commandBuilder7 = new SqlCommandBuilder(dataAdapter7);
            var ds7 = new DataSet();
            dataAdapter7.Fill(ds7);
            cboStanicaDo.DataSource = ds7.Tables[0];
            cboStanicaDo.DisplayMember = "Stanica";
            cboStanicaDo.ValueMember = "ID";

            var select8 = " Select ID, RTrim(Opis) as Opis From StatusTrasa order by ID";
            var s_connection8 = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection8 = new SqlConnection(s_connection8);
            var c8 = new SqlConnection(s_connection8);
            var dataAdapter8 = new SqlDataAdapter(select8, c8);

            var commandBuilder8 = new SqlCommandBuilder(dataAdapter8);
            var ds8 = new DataSet();
            dataAdapter8.Fill(ds8);
            cboStatusTrase.DataSource = ds8.Tables[0];
            cboStatusTrase.DisplayMember = "Opis";
            cboStatusTrase.ValueMember = "ID";

            VratiPodatke(pomTrasa, txtSifraRN.Text);
            VratiLokomotive(pomTrasa, txtSifraRN.Text);
            RefreshDataGrid2();


            var select61 = " Select Najava.ID, Najava.TehnologijaID, Tehnologija.PorudzbinaID as MP ,MpNaziv,Tezina as NajavaTezina,Duzina as NajavaDuzina , BrojKola as NajavaBrojKola, Tehnologija.Tonaza, Tehnologija.TonazaPovratak  from Najava " +
" inner join Tehnologija on Tehnologija.ID = Najava.TehnologijaID " +
" inner join MaticniPodatki on MaticniPodatki.MpSifra = Tehnologija.PorudzbinaID " +
" order by Najava.ID desc";

            var s_connection61 = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection61 = new SqlConnection(s_connection61);
            var c61 = new SqlConnection(s_connection61);
            var dataAdapter61 = new SqlDataAdapter(select61, c61);

            var commandBuilder61 = new SqlCommandBuilder(dataAdapter61);
            var ds61 = new DataSet();
            dataAdapter61.Fill(ds61);

            DataView view = new DataView(ds61.Tables[0]);
            //multiColumnComboBox1.ReadOnly = true;
            cboPorudzbinaID.DataSource = view;
            cboPorudzbinaID.DisplayMember = "ID";
            cboPorudzbinaID.ValueMember = "ID";

            RefreshDataGridNajave();
            RefreshDataGrid4();
        }

        private void VratiPodatke(int trasa, string RN)
        {

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection con = new SqlConnection(s_connection);

            con.Open();

            SqlCommand cmd = new SqlCommand("select   IDTrase, DatumPolaskaReal, DatumDolaskaReal, DatumPolaska,DatumDolaska, Vreme,VremeReal, PlaniranaMasa,MasaLokomotive, MasaVoza, BrutoMasa,Napomena, Rezi, StanicaOd, StanicaDo, StatusTrase, Poslato from RadniNalogTrase where IDRadnogNaloga=" + RN + "and IDTrase = " + trasa, con);
            SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                dtpVremeOdReal.Value = Convert.ToDateTime(dr["DatumPolaskaReal"].ToString());
                dtpVremeDoReal.Value = Convert.ToDateTime(dr["DatumDolaskaReal"].ToString());
                dtpVremeOd.Value = Convert.ToDateTime(dr["DatumPolaska"].ToString());
                dtpVremeDo.Value = Convert.ToDateTime(dr["DatumDolaska"].ToString());
                txtVreme.Text = dr["Vreme"].ToString();
                txtVremeReal.Text = dr["VremeReal"].ToString();
                txtPlaniranaMasa.Value = Convert.ToDecimal(dr["PlaniranaMasa"].ToString());
                txtMasaLokomotive.Value = Convert.ToDecimal(dr["MasaLokomotive"].ToString());
                txtMasaVoza.Value = Convert.ToDecimal(dr["MasaVoza"].ToString());
                txtBrutoVoza.Value = Convert.ToDecimal(dr["BrutoMasa"].ToString());
                txtNapomena.Text = dr["Napomena"].ToString();

                if (dr["Rezi"].ToString() == "1")
                {
                    chkRezi.Checked = true;
                }
                else
                {
                    chkRezi.Checked = false;
                }
                cboStanicaOd.SelectedValue = Convert.ToInt32(dr["StanicaOd"].ToString());
                cboStanicaDo.SelectedValue = Convert.ToInt32(dr["StanicaDo"].ToString());
                cboTrase.SelectedValue = Convert.ToInt32(dr["IDTrase"].ToString());

                if (dr["Poslato"].ToString() == "1")
                {
                    chkPoslato.Checked = true;
                }
                else
                {
                    chkPoslato.Checked = false;
                }
                cboStatusTrase.SelectedValue = Convert.ToInt32(dr["StatusTrase"].ToString());
            }

            con.Close();
        }

        private void tsSave_Click(object sender, EventArgs e)
        {
            InsertRadniNalogTraseLok ins = new InsertRadniNalogTraseLok();
            ins.InsRNTL(Convert.ToInt32(txtSifraRN.Text), Convert.ToInt32(cboTrase.SelectedValue), "", dtpVremeOd.Value, dtpVremeDo.Value, Convert.ToInt32(txtVreme.Text));
            RefreshDataGrid2();
            status = false;
        }

        private void RefreshDataGrid()
        {
            var select = "SELECT     RadniNalogTraseLok.IDRadnogNaloga, RadniNalogTraseLok.RB, RadniNalogTraseLok.IDTrase, RadniNalogTraseLok.SMSifra, RadniNalogTraseLok.DatumPolaska, " +
                     " RadniNalogTraseLok.DatumDolaska, RadniNalogTraseLok.Vreme, Trase.Pocetna, Trase.Krajnja, Trase.Relacija, Trase.Voz " +
                    " FROM         Trase INNER JOIN " +
                    "  RadniNalogTraseLok INNER JOIN " +
                    "  Mesta ON RadniNalogTraseLok.SMSifra = Mesta.SmSifra ON Trase.ID = RadniNalogTraseLok.IDTrase where RadniNalogTraseLok.IDRadnogNaloga =  " + Convert.ToInt32(txtSifraRN.Text) + " and IDTrase = " + Convert.ToInt32(cboTrase.SelectedValue) + " order by rb";
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];
        
        }

        private void RefreshDataGrid2()
        {
             var select = "SELECT  RadniNalogTrase.IDRadnogNaloga, RadniNalogTrase.RB, RadniNalogTrase.IDTrase, Trase.Voz, " +
                "StatusTrasa.Opis," +
                "RadniNalogTrase.Poslato," +
                "RadniNalogTrase.DatumPolaska " +
                ",RadniNalogTrase.DatumDolaska " +
                ",RadniNalogTrase.Vreme " +
                ",RadniNalogTrase.DatumPolaskaReal " +
                ",RadniNalogTrase.DatumDolaskaReal " +
                ",RadniNalogTrase.VremeReal " +
                ",RadniNalogTrase.PlaniranaMasa " +
                ",RadniNalogTrase.MasaLokomotive " +
                ",RadniNalogTrase.MasaVoza " +
                ",RadniNalogTrase.BrutoMasa " +
                ",RadniNalogTrase.Napomena " +
                ",stanice_2.Opis AS RN_Pocetna " +
                ",stanice_3.Opis AS RN_Krajnja " + 
                ",stanice.Opis AS Trasa_Pocetna " +
                ",stanice_1.Opis AS Trasa_Krajnja,stanice_2.ID as PocetnaID,stanice_3.ID as KrajnjaID " + 
                " FROM RadniNalogTrase INNER JOIN " +
                " Trase ON RadniNalogTrase.IDTrase = Trase.ID INNER JOIN " + 
                " stanice ON Trase.Pocetna = stanice.ID INNER JOIN " +
                " stanice AS stanice_1 ON Trase.Krajnja = stanice_1.ID " +
                " inner join stanice as stanice_2 ON RadniNalogTrase.StanicaOd = stanice_2.ID INNER JOIN  stanice AS stanice_3 ON RadniNalogTrase.StanicaDo = stanice_3.ID " + 
                " INNER JOIN  StatusTrasa  ON RadniNalogTrase.StatusTrase = StatusTrasa.ID " + 
                " where RadniNalogTrase.IDRadnogNaloga = " + Convert.ToInt32(txtSifraRN.Text) + " order by IDRadnogNaloga, RadniNalogTrase.RB";
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
             dataGridView1.Columns[0].HeaderText = "RN";
             dataGridView1.Columns[0].Width = 30;

             DataGridViewColumn column2 = dataGridView1.Columns[1];
             dataGridView1.Columns[1].HeaderText = "RB";
             dataGridView1.Columns[1].Width = 20;

             DataGridViewColumn column3 = dataGridView1.Columns[2];
             dataGridView1.Columns[2].HeaderText = "ID Trasa";
             dataGridView1.Columns[2].Width = 30;

             DataGridViewColumn column4 = dataGridView1.Columns[3];
             dataGridView1.Columns[3].HeaderText = "Trasa";
             dataGridView1.Columns[3].Width = 70;

             DataGridViewColumn column5 = dataGridView1.Columns[4];
             dataGridView1.Columns[4].HeaderText = "Status Trase";
             dataGridView1.Columns[4].Width = 100;

             DataGridViewColumn column6 = dataGridView1.Columns[5];
             dataGridView1.Columns[5].HeaderText = "Poslato";
             dataGridView1.Columns[5].Width = 100;

             DataGridViewColumn column7 = dataGridView1.Columns[6];
             dataGridView1.Columns[6].HeaderText = "Plan polaska";
             dataGridView1.Columns[6].Width = 100;

             DataGridViewColumn column8 = dataGridView1.Columns[7];
             dataGridView1.Columns[7].HeaderText = "Plan dolaska";
             dataGridView1.Columns[7].Width = 100;

             DataGridViewColumn column9 = dataGridView1.Columns[8];
             dataGridView1.Columns[8].HeaderText = "Planirano vreme";
             dataGridView1.Columns[8].Width = 50;

             DataGridViewColumn column10 = dataGridView1.Columns[9];
             dataGridView1.Columns[9].HeaderText = "Realiz. polaska ";
             dataGridView1.Columns[9].Width = 100;

             DataGridViewColumn column11 = dataGridView1.Columns[10];
             dataGridView1.Columns[10].HeaderText = "Realiz. dolaska";
             dataGridView1.Columns[10].Width = 100;

             DataGridViewColumn column12 = dataGridView1.Columns[11];
             dataGridView1.Columns[11].HeaderText = "Realizovano vreme";
             dataGridView1.Columns[11].Width = 50;

             DataGridViewColumn column13 = dataGridView1.Columns[12];
             dataGridView1.Columns[12].HeaderText = "Planirana masa";
             dataGridView1.Columns[12].Width = 50;

             DataGridViewColumn column14 = dataGridView1.Columns[13];
             dataGridView1.Columns[13].HeaderText = "Masa lokomotive";
             dataGridView1.Columns[13].Width = 50;

             DataGridViewColumn column15 = dataGridView1.Columns[14];
             dataGridView1.Columns[14].HeaderText = "Masa voza";
             dataGridView1.Columns[14].Width = 50;

             DataGridViewColumn column16 = dataGridView1.Columns[15];
             dataGridView1.Columns[15].HeaderText = "Bruto masa";
             dataGridView1.Columns[15].Width = 50;

            //dataGridView1.Columns[21].Visible = false;
            //dataGridView1.Columns[22].Visible = false;
        }

        private void dtpVremeDo_Leave(object sender, EventArgs e)
        {
            TimeSpan span =  dtpVremeDo.Value.Subtract(dtpVremeOd.Value);
            txtVreme.Text = Convert.ToString(Convert.ToInt32(span.TotalMinutes));
        }

        private void dtpVremeKasnjenjaDo_Leave(object sender, EventArgs e)
        {
           // dtpVremeKasnjenjaDo
           
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            InsertRadniNalogTrase ins = new InsertRadniNalogTrase();
            ins.InsRNT(Convert.ToInt32(txtSifraRN.Text), Convert.ToInt32(cboTrase.SelectedValue), Convert.ToDateTime(dtpVremeOdReal.Value), Convert.ToDateTime(dtpVremeDoReal.Value), Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value), Convert.ToInt32(txtVreme.Text), Convert.ToInt32(txtVremeReal.Text), Convert.ToDouble(txtPlaniranaMasa.Text), Convert.ToDouble(txtMasaLokomotive.Text), Convert.ToDouble(txtMasaVoza.Text), Convert.ToDouble(txtBrutoVoza.Text), txtNapomena.Text, chkRezi.Checked, Convert.ToInt32(cboStanicaOd.SelectedValue), Convert.ToInt32(cboStanicaDo.SelectedValue), chkPoslato.Checked, Convert.ToInt32(cboStatusTrase.SelectedValue));
            RefreshDataGrid2();
            status = false;
        }

        private void dtpVremeDoReal_Leave(object sender, EventArgs e)
        {
            TimeSpan span = dtpVremeDoReal.Value.Subtract(dtpVremeOdReal.Value);
            txtVremeReal.Text =  Convert.ToString(Convert.ToInt32(span.TotalMinutes));
        }

        private void button5_Click(object sender, EventArgs e)
        {
            InsertRadniNalogTrase upd = new InsertRadniNalogTrase();
            upd.UpdRNT(Convert.ToInt32(txtSifraRN.Text), Convert.ToInt32(cboTrase.SelectedValue), Convert.ToDateTime(dtpVremeOdReal.Value), Convert.ToDateTime(dtpVremeDoReal.Value), Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value), Convert.ToInt32(txtVreme.Text), Convert.ToInt32(txtVremeReal.Text), Convert.ToDouble(txtPlaniranaMasa.Text), Convert.ToDouble(txtMasaLokomotive.Text), Convert.ToDouble(txtMasaVoza.Text), Convert.ToDouble(txtBrutoVoza.Text), txtNapomena.Text, chkRezi.Checked, Convert.ToInt32(cboStanicaOd.SelectedValue), Convert.ToInt32(cboStanicaDo.SelectedValue), chkPoslato.Checked, Convert.ToInt32(cboStatusTrase.SelectedValue), Convert.ToInt32(txtRB.Text));
            RefreshDataGrid2();
            status = false;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            InsertRadniNalogTrase del = new InsertRadniNalogTrase();
            del.delRNT(Convert.ToInt32(txtSifraRN.Text), Convert.ToInt32(cboTrase.SelectedValue), Convert.ToInt32(txtRB.Text));
            RefreshDataGrid2();
            status = false;

        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            frmKasnjenja kas = new frmKasnjenja(txtSifraRN.Text, Convert.ToInt32(cboTrase.SelectedValue));
            kas.Show();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            frmPravljenjeVoza fpv = new frmPravljenjeVoza(Convert.ToInt32(txtSifraRN.Text), Convert.ToInt32(cboStanicaOd.SelectedValue), Convert.ToInt32(cboStanicaOd.SelectedValue), Convert.ToInt32(cboStanicaDo.SelectedValue), dtpVremeOd.Value);
            fpv.Show();
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            int Trasa = 0;
            int RB = 0;
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Selected)
                    {
                        Trasa = Convert.ToInt32(row.Cells[2].Value.ToString());
                        RB = Convert.ToInt32(row.Cells[1].Value.ToString());
                        txtRB.Text = row.Cells[1].Value.ToString();
                        VratiPodatkeTrasa(Trasa, RB);
                        cboStanicaOd.SelectedValue = row.Cells[21].Value.ToString();
                        cboStanicaDo.SelectedValue = row.Cells[22].Value.ToString();
                        
                    }
                }
                RefreshDataGridNajave();
                RefreshDataGrid4();
            }
            catch
            {
                MessageBox.Show("Nije uspela selekcija stavki");
            }
        }

        private void VratiPodatkeTrasa(int Trasa, int RB)
        {
           
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection con = new SqlConnection(s_connection);

            con.Open();

            SqlCommand cmd = new SqlCommand("Select IDRadnogNaloga, RB, IDTrase, DatumPolaskaReal, DatumDolaskaReal, DatumPolaska, " +
            " DatumDolaska, Vreme, VremeReal, PlaniranaMasa, MasaLokomotive, MasaVoza, BrutoMasa, Napomena, " +
            " StanicaOd, StanicaDo, Rezi, Poslato, StatusTrase from RadniNalogTrase where RadniNalogTrase.IDRadnogNaloga =  " + Convert.ToInt32(txtSifraRN.Text) + " and RadniNalogTrase.IDTrase = " + Trasa +  " and RadniNalogTrase.RB = " + RB + " order by rb", con);
            SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                txtRB.Text = dr["RB"].ToString();
                dtpVremeOdReal.Value = Convert.ToDateTime(dr["DatumPolaskaReal"].ToString());
                dtpVremeDoReal.Value = Convert.ToDateTime(dr["DatumDolaskaReal"].ToString());
                txtVreme.Text = dr["Vreme"].ToString();
                dtpVremeOd.Value = Convert.ToDateTime(dr["DatumPolaska"].ToString());
                dtpVremeDo.Value = Convert.ToDateTime(dr["DatumDolaska"].ToString());
                txtVremeReal.Text = dr["VremeReal"].ToString();
                txtPlaniranaMasa.Value = Convert.ToDecimal(dr["PlaniranaMasa"].ToString());
                txtMasaLokomotive.Value = Convert.ToDecimal(dr["MasaLokomotive"].ToString());
                txtMasaVoza.Value = Convert.ToDecimal(dr["MasaVoza"].ToString());
                txtBrutoVoza.Value = Convert.ToDecimal(dr["BrutoMasa"].ToString());
                cboStanicaOd.SelectedValue = Convert.ToInt32(dr["StanicaOd"].ToString());
                cboStanicaDo.SelectedValue = Convert.ToInt32(dr["StanicaDo"].ToString());
                txtNapomena.Text = dr["Napomena"].ToString();
                
                if (dr["Rezi"].ToString() == "1")
                {
                    chkRezi.Checked = true;
                }
                else
                {
                    chkRezi.Checked = false;
                    
                }
                cboTrase.SelectedValue = Convert.ToInt32(dr["IDTrase"].ToString());

                if (dr["Poslato"].ToString() == "1")
                {
                    chkPoslato.Checked = true;
                }
                else
                {
                    chkPoslato.Checked = false;
                }
                cboStatusTrase.SelectedValue = Convert.ToInt32(dr["StatusTrase"].ToString());
            }
            con.Close();

            VratiPodatkeTrasa(); // Bez parametara
        }

        private void VratiPodatkeTrasa()
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection con = new SqlConnection(s_connection);

            con.Open();

            SqlCommand cmd = new SqlCommand("select Pocetna, Krajnja, TezinaVoza, TezinaLokomotive, DuzinaVoza from Trase where ID = " + cboTrase.SelectedValue, con);
            SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                txtPlaniranaMasa.Value = Convert.ToDecimal(dr["TezinaVoza"].ToString());
                txtMasaLokomotive.Value = Convert.ToDecimal(dr["TezinaLokomotive"].ToString());
                txtDuzinaVoza.Value = Convert.ToDecimal(dr["DuzinaVoza"].ToString());
                cboStanicaOd.SelectedValue = Convert.ToInt32(dr["Pocetna"].ToString());
                cboStanicaDo.SelectedValue = Convert.ToInt32(dr["Krajnja"].ToString());
            }
            con.Close();


            var s_connection2 = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection con2 = new SqlConnection(s_connection2);

            con2.Open();

            SqlCommand cmd2 = new SqlCommand("select Najava.Tezina, Najava.Duzina from RadniNalog inner join Najava on Najava.ID = RadniNalog.TehnologijaID where RadniNalog.ID =  " + Convert.ToInt32(txtSifraRN.Text), con2);
            SqlDataReader dr2 = cmd2.ExecuteReader();

            while (dr2.Read())
            {
                txtMasaVoza.Value = Convert.ToDecimal(dr2["Duzina"].ToString());
                txtBrutoVoza.Value = Convert.ToDecimal(dr2["Tezina"].ToString());
            }
            con2.Close();

        }

        private void cboTrase_Leave(object sender, EventArgs e)
        {
            VratiPodatkeTrasa();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            frmPodtrase podtrase = new frmPodtrase(txtSifraRN.Text, cboTrase.SelectedValue.ToString(),  txtRB.Text);
            podtrase.Show();
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            InsertRadniNalog ins = new InsertRadniNalog();
            ins.InsRadniNalogVezaNajava2(Convert.ToInt32(txtSifraRN.Text), Convert.ToInt32(cboPorudzbinaID.Text), Convert.ToInt32(txtBrojKola.Value), Convert.ToInt32(txtRB.Text));
            RefreshDataGridNajave();
        }

        private void RefreshDataGridNajave()
        {
            if (txtRB.Text != "")
            {
                var select = " select * from RadniNalogVezaNajave where IDRadnogNaloga = " + Convert.ToInt32(txtSifraRN.Text) + " and RB = " + txtRB.Text;

                var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
                SqlConnection myConnection = new SqlConnection(s_connection);
                var c = new SqlConnection(s_connection);
                var dataAdapter = new SqlDataAdapter(select, c);

                var commandBuilder = new SqlCommandBuilder(dataAdapter);
                var ds = new DataSet();
                dataAdapter.Fill(ds);
                dataGridView3.ReadOnly = true;
                dataGridView3.DataSource = ds.Tables[0];

                DataGridViewColumn column = dataGridView3.Columns[0];
                dataGridView3.Columns[0].HeaderText = "ID";
                dataGridView3.Columns[0].Width = 40;

                DataGridViewColumn column2 = dataGridView3.Columns[1];
                dataGridView3.Columns[1].HeaderText = "IDRadnogNaloga";
                dataGridView3.Columns[1].Width = 60;

                DataGridViewColumn column3 = dataGridView3.Columns[2];
                dataGridView3.Columns[2].HeaderText = "IDNajave";
                dataGridView3.Columns[2].Width = 60;

                DataGridViewColumn column4 = dataGridView3.Columns[3];
                dataGridView3.Columns[3].HeaderText = "Broj kola";
                dataGridView3.Columns[3].Width = 60;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            InsertRadniNalog ins = new InsertRadniNalog();
            ins.DeleteRNVezaNajava(Convert.ToInt32(txtSifraZap.Text));
            RefreshDataGridNajave();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            InsertTeretnica ins = new InsertTeretnica();
            ins.PrekopirajTeretnicaPoNajavamaSL1(Convert.ToInt32(txtSifraRN.Text), Convert.ToInt32(txtRB.Text));
            MessageBox.Show("Kopira se Teretnica koja je prijemna(cekirano) i koja ima najavu po unetom podatku");
            RefreshDataGrid4();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            InsertTeretnica ins = new InsertTeretnica();
            ins.PrekopirajTerPoNajavamaSL2(Convert.ToInt32(txtSifraRN.Text), Convert.ToInt32(txtRB.Text));
            MessageBox.Show("Kopira zadnje zaglavlje teretnice po definisanim Najavama i uzima zadnje teretnice po najavama i formira listu, uzima sve stavke iz Najava");
            RefreshDataGrid4();
        }

        private void ProglasiObaveznomStavkom()
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView4.Rows)
                {
                    if (row.Selected)
                    {
                       // Trasa = Convert.ToInt32(row.Cells[2].Value.ToString());
                        InsertTeretnica ins = new InsertTeretnica();
                        ins.ProglasiObavezan(Convert.ToInt32(row.Cells[0].Value.ToString()));
                      //  MessageBox.Show("Kopira zadnje zaglavlje teretnice po definisanim Najavama i uzima zadnje teretnice po najavama i formira listu, uzima sve stavke iz Najava");
                        RefreshDataGrid4();
                    }
                }
            }
            catch
            {
                MessageBox.Show("Nije uspela selekcija stavki");
            }



        }

       

        private void RefreshDataGrid4()
        {
            if (txtRB.Text != "")
            { 
            var select = " SELECT TeretnicaStavke.ID, TeretnicaStavke.RB, TeretnicaStavke.IDNajave, TeretnicaStavke.Obavezna, stanice.Opis as Uvrstena, stanice_1.Opis AS Otkacena, TeretnicaStavke.BrojKola, TeretnicaStavke.Serija, " +
                "TeretnicaStavke.BrojOsovina, TeretnicaStavke.Duzina, TeretnicaStavke.Tara, TeretnicaStavke.Neto, TeretnicaStavke.G, TeretnicaStavke.P, TeretnicaStavke.R, " +
                "TeretnicaStavke.RR, TeretnicaStavke.VRNP, stanice_3.Opis AS Otpravna, stanice_2.Opis AS Uputna, TeretnicaStavke.Reon, TeretnicaStavke.Primedba, " +
                "TeretnicaStavke.RucKoc,  stanice_5.Opis as Izvozna, stanice_4.Opis as Uvozna, RID, Dokument,AVV_Neispravnosti.SifraKvara as [Sifra kvara],AVV_Neispravnosti.Opis as [Opis kvara] " +
                "FROM Teretnica INNER JOIN " +
                "TeretnicaStavke ON Teretnica.ID = TeretnicaStavke.BrojTeretnice INNER JOIN " +
                "stanice ON TeretnicaStavke.Uvrstena = stanice.ID INNER JOIN " +
                "stanice AS stanice_1 ON TeretnicaStavke.Otkacena = stanice_1.ID INNER JOIN " +
                "stanice AS stanice_3 ON TeretnicaStavke.Otpravna = stanice_3.ID INNER JOIN " +
                "stanice AS stanice_2 ON TeretnicaStavke.Uputna = stanice_2.ID INNER JOIN " +
                "stanice AS stanice_4 ON TeretnicaStavke.Uvozna = stanice_4.ID INNER JOIN " +
                "stanice AS stanice_5 ON TeretnicaStavke.Izvozna = stanice_5.ID LEFT JOIN " +
                "AVV_Neispravnosti on TeretnicaStavke.SifraKvaraID = AVV_Neispravnosti.ID " +
" where Teretnica.RN = " + Convert.ToInt32(txtSifraRN.Text) + " and Teretnica.RB =  " + txtRB.Text  + 
" and Prevozna = 1 ";
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView4.ReadOnly = true;
            dataGridView4.DataSource = ds.Tables[0];
            }
        }
        private void VratiPodatkeIDNajave()
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView3.Rows)
                {
                    if (row.Selected)
                    {
                        txtSifraZap.Text = row.Cells[0].Value.ToString();
                        cboPorudzbinaID.Text = row.Cells[2].Value.ToString();
                        txtBrojKola.Value = Convert.ToDecimal(row.Cells[3].Value.ToString());
                    }
                }
            }
            catch
            {
                MessageBox.Show("Nije uspela selekcija stavki");
            }


        }
        private void dataGridView3_SelectionChanged(object sender, EventArgs e)
        {
            VratiPodatkeIDNajave();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            ProglasiObaveznomStavkom();
        }
    }
}
