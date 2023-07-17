using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data.SqlClient;


//using iTextSharp.text;
//using iTextSharp.text.pdf;
//using iTextSharp;
using System.Text.RegularExpressions;

using System.Drawing.Imaging;


namespace Saobracaj.Dokumenta
{
    public partial class frmDokumentaRadnika : Form
    {
        bool status = false;
        int pomTipDokumenta = 0;
        int pomZaposleni = 0;
        int pomSifraZapisa = 0;
        public frmDokumentaRadnika()
        {
            InitializeComponent();
        }

        public frmDokumentaRadnika(int SifraZApisa, int Zaposleni, int TipDokumenta)
        {
           InitializeComponent();
           pomTipDokumenta = SifraZApisa;
           pomZaposleni = Zaposleni;
           pomSifraZapisa = TipDokumenta;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string PictureFolder = txtPutanja.Text;

            ofd1.InitialDirectory = PictureFolder;

            if (ofd1.ShowDialog() == DialogResult.OK)
            {
                txtPutanja.Text = fbd1.SelectedPath.ToString() + ofd1.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(txtPutanja.Text);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (status == true)
            {
                InsertDokumentaRadnika ins = new InsertDokumentaRadnika();
                KopirajFajlPoTipu(txtPutanja.Text, txtSifraNajave.Text, 6);
                ins.InsRadDokumenta(Convert.ToInt32(txtSifraNajave.Text), txtPutanja.Text, Convert.ToInt32(cboZaposleni.SelectedValue), Convert.ToInt32(cboTipDokumenta.SelectedValue), txtNapomena.Text, Convert.ToDateTime(dtpDatumDokumenta.Value));
                RefreshDataGrid();

                status = true;
            }
            else
            {

            }
        }

        private void RefreshDataGrid()
        {
            int pomNaj = Convert.ToInt32(txtSifraNajave.Text);
            var select = "select * from DokumentaRadnika where DokumentaRAdnika.IDDokumentaRadnika =  " + pomNaj;
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
            dataGridView1.Columns[1].HeaderText = "IDDokumentaRadnika";
            dataGridView1.Columns[1].Width = 50;

            DataGridViewColumn column3 = dataGridView1.Columns[2];
            dataGridView1.Columns[2].HeaderText = "Putanja";
            dataGridView1.Columns[2].Width = 550;
        }

        private void KopirajFajlPoTipu(string putanja, string FolderDestinacije, int Tip)
        {
            string fileName = ofd1.FileName; //Ovde ce trebati promena
            fileName = fileName.Replace(" ", "_");
            string sourcePath = fbd1.SelectedPath.ToString();
            string result = Path.GetFileName(fileName);
            string targetPath = "";

            targetPath = @"\\192.168.1.6\RadnikDokumenta\" + FolderDestinacije + @"\Bolovanja";

            string sourceFile = putanja;
            string destFile = System.IO.Path.Combine(targetPath, result);

            if (!System.IO.Directory.Exists(targetPath))
            {
                System.IO.Directory.CreateDirectory(targetPath);
            }

            var remote = Path.Combine(targetPath, result);
            File.Copy(sourceFile, remote);
            txtPutanja.Text = remote;

            if (System.IO.Directory.Exists(sourcePath))
            {
                string[] files = System.IO.Directory.GetFiles(sourcePath);
                // Copy the files and overwrite destination files if they already exist.
                foreach (string s in files)
                {
                    // Use static Path methods to extract only the file name from the path.
                    fileName = System.IO.Path.GetFileName(s);
                    destFile = System.IO.Path.Combine(targetPath, fileName);
                    System.IO.File.Copy(s, destFile, true);
                }
            }
        }

        private void tsNew_Click(object sender, EventArgs e)
        {
            status = true;
        }

        private void VratiPodatkeSelect(string ID)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection con = new SqlConnection(s_connection);

            con.Open();

           SqlCommand cmd = new SqlCommand("SELECT [ID]      ,[IDDokumentaRadnika]      ,[Putanja]      ,[Zaposleni] " +
                    " ,[TipDokumenta]      ,[Napomena]      ,[DatumDokumenta]  FROM [DokumentaRadnika] where ID=" + txtSifra.Text, con);
            SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {

                txtSifra.Text = dr["ID"].ToString();
                txtSifraNajave.Text = dr["IDDokumentaRadnika"].ToString();
                txtPutanja.Text = dr["Putanja"].ToString();
               
                cboZaposleni.SelectedValue = Convert.ToInt32(dr["Zaposleni"].ToString());
                cboTipDokumenta.SelectedValue = Convert.ToInt32(dr["TipDokumenta"].ToString());
                txtNapomena.Text = dr["Napomena"].ToString();
                dtpDatumDokumenta.Value = Convert.ToDateTime(dr["DatumDokumenta"].ToString());
                
               
            }

            con.Close();
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
                        VratiPodatkeSelect(txtSifra.Text);
                       // txtPutanja.Text = "\\\\" + row.Cells[2].Value.ToString();
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

        private void frmDokumentaRadnika_Load(object sender, EventArgs e)
        {

            var select = " Select DeSifra, Rtrim(DePriimek) + ' ' + Rtrim(DeIme) as Naziv From Delavci Order by DePriimek";
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            cboZaposleni.DataSource = ds.Tables[0];
            cboZaposleni.DisplayMember = "Naziv";
            cboZaposleni.ValueMember = "DeSifra";

            var selectL = " Select Distinct ID, RTrim(Naziv) as NAziv From RadnikTipDokumenta";
            var s_connectionL = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnectionL = new SqlConnection(s_connectionL);
            var cL = new SqlConnection(s_connectionL);
            var dataAdapterL = new SqlDataAdapter(selectL, cL);

            var commandBuilderL = new SqlCommandBuilder(dataAdapterL);
            var dsL = new DataSet();
            dataAdapterL.Fill(dsL);
            cboTipDokumenta.DataSource = dsL.Tables[0];
            cboTipDokumenta.DisplayMember = "Naziv";
            cboTipDokumenta.ValueMember = "ID";


            txtSifraNajave.Text = pomSifraZapisa.ToString();
            cboZaposleni.SelectedValue =  pomZaposleni;
            cboTipDokumenta.SelectedValue = pomSifraZapisa;

        }
    }
}
