using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Saobracaj.Dokumenta
{
    public partial class frmPPK : Form
    {
        int aktivnost;
        int zaposleni;
        bool status = false;
        OpenFileDialog ofd1 = new OpenFileDialog();
        FolderBrowserDialog fbd1 = new FolderBrowserDialog();
        public frmPPK()
        {
            InitializeComponent();
            
        }
        public frmPPK(int ID,int Zaposleni)
        {
            aktivnost = ID;
            zaposleni = Zaposleni;
            InitializeComponent();
            FillGV();
            txt_ID.Enabled = false;
            txt_IDAktivnost.Enabled = false;
            txt_IDAktivnost.Text = aktivnost.ToString();
        }
        private void FillGV()
        {
            var select = "Select distinct Aktivnosti.Zaposleni,Aktivnosti.VremeOd,Aktivnosti.VremeDo,Ukupno,UkupniTroskovi,Opis,RN,AktivnostiStavke.ID,IDNadredjena," +
                "VrstaAktivnostiID,Sati,Koeficijent,Napomena,BrojVagona,Razlog,Nalogodavac,Vozilo,Posao,DatumZavrsetka,Nadlezni " +
                "from AktivnostiStavke " +
                "Inner join Aktivnosti on AktivnostiStavke.IDNadredjena = Aktivnosti.ID " +
                "Where AktivnostiStavke.VrstaAktivnostiID = 9 and Zaposleni ="+zaposleni+" order by AktivnostiStavke.ID desc";

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
            dataGridView1.Columns[3].Width = 60;
            dataGridView1.Columns[4].HeaderText = "Σ Troskovi";
            dataGridView1.Columns[4].Width = 60;
            dataGridView1.Columns[5].Width = 150;
            dataGridView1.Columns[6].Width = 50;
            dataGridView1.Columns[7].Width = 60;
            dataGridView1.Columns[8].HeaderText = "Aktivnost";
            dataGridView1.Columns[8].Width = 60;
            dataGridView1.Columns[9].Visible = false;
            dataGridView1.Columns[10].Width = 50;
            dataGridView1.Columns[11].Width = 60;
            dataGridView1.Columns[12].Width = 120;
            dataGridView1.Columns[13].HeaderText = "Br Vagona";
            dataGridView1.Columns[13].Width = 60;
            dataGridView1.Columns[15].Width = 60;

            var svi = "Select * from PPK order by ID desc";
            var da3 = new SqlDataAdapter(svi, c);
            var ds3 = new DataSet();
            da3.Fill(ds3);
            dataGridView3.ReadOnly = true;
            dataGridView3.DataSource = ds3.Tables[0];

            var ppk = "Select PPK.ID,IDStavke,Slika,VremeZavrsetka,Napomena " +
                "from PPK " +
                "Inner join Aktivnosti on PPK.IDStavke = Aktivnosti.ID " +
                "WHere Aktivnosti.Zaposleni ="+zaposleni+" order by PPK.ID desc";
            var da2 = new SqlDataAdapter(ppk, c);
            var ds2 = new DataSet();
            da2.Fill(ds2);
            dataGridView2.ReadOnly = true;
            dataGridView2.DataSource = ds2.Tables[0];
        }

        private void btn_UcitajSliku_Click(object sender, EventArgs e)
        {
            string folder = txt_Slika.Text;
            ofd1.InitialDirectory = folder;
            if (ofd1.ShowDialog() == DialogResult.OK)
            {
                txt_Slika.Text = fbd1.SelectedPath.ToString() + ofd1.FileName;
            }
        }
        private void KopirajFajlPoTipu(string putanja, string FolderDestinacije, int ID)
        {
            string fileName = ofd1.FileName; //Ovde ce trebati promena
            fileName = fileName.Replace(" ", "_");
            string sourcePath = fbd1.SelectedPath.ToString();
            string result = Path.GetFileName(fileName);
            string targetPath = "";

            targetPath = @"\\192.168.1.6\ppk\" + FolderDestinacije + ID;

            string sourceFile = putanja;
            string destFile = System.IO.Path.Combine(targetPath, result);

            if (!System.IO.Directory.Exists(targetPath))
            {
                System.IO.Directory.CreateDirectory(targetPath);
            }

            var remote = Path.Combine(targetPath, result);
            File.Copy(sourceFile, remote);
            txt_Slika.Text = targetPath;

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

        private void btn_SacuvajSliku_Click(object sender, EventArgs e)
        {
            KopirajFajlPoTipu(txt_Slika.Text, txt_ID.Text, aktivnost);
        }

        private void btn_Otvori_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(txt_Slika.Text);
        }

        private void tsNew_Click(object sender, EventArgs e)
        {
            status = true;
            txt_ID.Text = "";
        }

        private void tsSave_Click(object sender, EventArgs e)
        {
            InsertPPK ppk = new InsertPPK();
            if (status == true)
            {
                ppk.InsPPK(aktivnost, txt_Slika.Text.ToString().TrimEnd(), Convert.ToDateTime(dateTimePicker1.Value), txt_Napomena.Text.ToString().TrimEnd());
            }
            FillGV();
        }
    }
}
