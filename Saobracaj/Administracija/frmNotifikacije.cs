using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Saobracaj.Administracija
{
    public partial class frmNotifikacije : Form
    {
        string connect = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
        string Kor = Sifarnici.frmLogovanje.user.ToString();
        string niz = "";
        bool status = false;
        public static string code = "frmNotifikacije";
        public bool Pravo;
        int idForme;
        bool insert;
        bool update;
        bool delete;
        int kreirao;
        public frmNotifikacije()
        {
            InitializeComponent();
            FillGV();
            FillCheck();
            IdGrupe();
            IdForme();
            PravoPristupa();
            FillCombo();

            txt_ID.Enabled = false;

            SqlConnection conn = new SqlConnection(connect);
            conn.Open();
            SqlCommand cmd = new SqlCommand("Select DeSifra from Korisnici Where Korisnik='" + Kor.ToString() + "'", conn);
            SqlDataReader dr = cmd.ExecuteReader();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    kreirao = Convert.ToInt32(dr["DeSifra"].ToString());
                }
            }
            conn.Close();

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
        private void FillGV()
        {
            var query = "Select ID,Kreirao,RTrim(Korisnici.Korisnik) as KorisnikKreirao,RTrim(Poruka) as Poruka,Notifikacije.Korisnik,(RTrim(Delavci.DePriimek)+' '+RTrim(Delavci.DeIme)) as Zaposleni," +
                "DatumSlanja,Procitao,DatumCitanja, VrstaObavestenja, Putanja " +
                "From Notifikacije " +
                "Inner join Delavci on Notifikacije.Korisnik = Delavci.DeSifra " +
                "inner join Korisnici on Notifikacije.Kreirao = Korisnici.DeSifra " +
                "order by ID desc";
            SqlConnection conn = new SqlConnection(connect);
            var da = new SqlDataAdapter(query, conn);
            var ds = new DataSet();
            da.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            dataGridView1.Columns[0].Width = 50;
            dataGridView1.Columns[1].Visible = false;
            dataGridView1.Columns[2].HeaderText = "Kreirao";
            dataGridView1.Columns[2].Width = 90;
            dataGridView1.Columns[3].Width = 180;
            dataGridView1.Columns[4].Visible = false;
            dataGridView1.Columns[5].Width = 120;
            dataGridView1.Columns[7].Width = 50;
        }

        private void FillGVByUser()
        {
            var query = "Select ID,Kreirao,RTrim(Korisnici.Korisnik) as KorisnikKreirao,RTrim(Poruka) as Poruka,Notifikacije.Korisnik,(RTrim(Delavci.DePriimek)+' '+RTrim(Delavci.DeIme)) as Zaposleni," +
                "DatumSlanja,Procitao,DatumCitanja, VrstaObavestenja, Putanja " +
                "From Notifikacije " +
                "Inner join Delavci on Notifikacije.Korisnik = Delavci.DeSifra " +
                "inner join Korisnici on Notifikacije.Kreirao = Korisnici.DeSifra" +
                " where Korisnici.Korisnik =  '" + txtKreirao.Text + "'"+ 
                " order by ID desc";
            SqlConnection conn = new SqlConnection(connect);
            var da = new SqlDataAdapter(query, conn);
            var ds = new DataSet();
            da.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            dataGridView1.Columns[0].Width = 50;
            dataGridView1.Columns[1].Visible = false;
            dataGridView1.Columns[2].HeaderText = "Kreirao";
            dataGridView1.Columns[2].Width = 90;
            dataGridView1.Columns[3].Width = 180;
            dataGridView1.Columns[4].Visible = false;
            dataGridView1.Columns[5].Width = 120;
            dataGridView1.Columns[7].Width = 50;
        }
        private void FillCheck()
        {
            var query = "Select Distinct DeSifra,Korisnik From Korisnici where Korisnik <> '' order by Korisnik";
            SqlConnection conn = new SqlConnection(connect);
            var da = new SqlDataAdapter(query, conn);
            var ds = new DataSet();
            da.Fill(ds);
            cbList_Korisnici.DataSource = ds.Tables[0];
            cbList_Korisnici.DisplayMember = "Korisnik";
            cbList_Korisnici.ValueMember = "DeSifra";
        }
        private void FillCombo()
        {
            var query = "select distinct DmNaziv from DelovnaMesta order by DmNaziv";
            SqlConnection conn = new SqlConnection(connect);
            var da = new SqlDataAdapter(query, conn);
            var ds = new DataSet();
            da.Fill(ds);
            combo_RadnoMesto.DataSource = ds.Tables[0];
            combo_RadnoMesto.DisplayMember = "DmNaziv";
            combo_RadnoMesto.ValueMember = "DmNaziv";

            cboTipNotifikacije.SelectedIndex = 0;
        }
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                for (int i = 0; i < cbList_Korisnici.Items.Count; i++)
                {
                    if (cbList_Korisnici.GetItemCheckState(i) == CheckState.Checked)
                    {
                        cbList_Korisnici.SetItemChecked(i, false);
                    }
                }
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Selected)
                    {
                        txt_ID.Text = row.Cells[0].Value.ToString();
                        txt_Obavestenje.Text = row.Cells[3].Value.ToString();
                        dt_Slanje.Value = Convert.ToDateTime(row.Cells[6].Value.ToString());
                        dt_Citanje.Value = Convert.ToDateTime(row.Cells[8].Value.ToString());
                        bool procitao = Convert.ToBoolean(row.Cells[7].Value.ToString());
                        if (procitao == true) { cb_Procitan.Checked = true; }
                        if (procitao == false)
                        { cb_Procitan.Checked = false; }
                        cbList_Korisnici.SelectedValue = Convert.ToInt32(row.Cells[4].Value.ToString());
                        for (int i = 0; i < cbList_Korisnici.Items.Count; i++)
                        {
                            if (cbList_Korisnici.GetSelected(i))
                            {
                                cbList_Korisnici.SetItemChecked(i, true);
                            }

                        }
                        cboTipNotifikacije.Text = row.Cells[9].Value.ToString();
                            txt_putanja.Text = row.Cells[10].Value.ToString();
                    }

                }
            }
            catch
            {
            }
        }

        private void frmNotifikacije_Load(object sender, EventArgs e)
        {
            txtKreirao.Text = Sifarnici.frmLogovanje.user;
            InsertObavestenje obavestenja = new InsertObavestenje();
            obavestenja.ArhivirajObavestenje();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void tsNew_Click(object sender, EventArgs e)
        {
            status = true;
            txt_ID.Text = "";
        }

        private void KopirajFajlPoTipu(string putanja, string FolderDestinacije)
        {
            string fileName = ofd1.FileName; //Ovde ce trebati promena
            fileName = fileName.Replace(" ", "_");
            string sourcePath = fbd1.SelectedPath.ToString();
            string result = Path.GetFileName(fileName);
            string targetPath = "";

            targetPath = @"\\192.168.1.6\Notifikacije\"   + FolderDestinacije;

            string sourceFile = putanja;
            string destFile = System.IO.Path.Combine(targetPath, result);

            if (!System.IO.Directory.Exists(targetPath))
            {
                System.IO.Directory.CreateDirectory(targetPath);
            }

            var remote = Path.Combine(targetPath, result);
            File.Copy(sourceFile, remote);
            txt_putanja.Text = remote;

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

      int VratiMax1()
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection con = new SqlConnection(s_connection);

            con.Open();

            SqlCommand cmd = new SqlCommand(" Select Max(ID) +1  as ID from Notifikacije ", con);


            SqlDataReader dr = cmd.ExecuteReader();
            int SledeciBroj = 0;
            while (dr.Read())
            {
                SledeciBroj = Convert.ToInt32(dr["ID"].ToString());

            }
            con.Close();
            return SledeciBroj;
        }

        private void tsSave_Click(object sender, EventArgs e)
        {
           
            InsertObavestenje obavestenja = new InsertObavestenje();
            if (status == true)
            {
                txt_ID.Text = VratiMax1().ToString();
                if (txt_putanja.Text != "")
                { 
                 KopirajFajlPoTipu(txt_putanja.Text, txt_ID.Text);
                
                }
               
                for (int i = 0; i < cbList_Korisnici.Items.Count; i++)
                {
                    if (cbList_Korisnici.GetItemCheckState(i) == CheckState.Checked)
                    {
                        cbList_Korisnici.SetSelected(i, true);
                        int PaSifra = Convert.ToInt32(cbList_Korisnici.SelectedValue);
                        obavestenja.InsObavestenje(kreirao, PaSifra, txt_Obavestenje.Text.ToString().TrimEnd(), Convert.ToDateTime(dt_Slanje.Value.ToString()), false, Convert.ToDateTime(dt_Citanje.Value.ToString()), cboTipNotifikacije.Text, txt_putanja.Text, Convert.ToDateTime(dtpDatumVazenja.Value.ToString()));
                    }
                }
                for (int i = 0; i < cbList_Korisnici.Items.Count; i++)
                {
                    if (cbList_Korisnici.GetItemCheckState(i) == CheckState.Checked)
                    {
                        cbList_Korisnici.SetItemChecked(i, false);
                    }
                }
                status = false;
            }
            else
            {
                bool procitan;
                int PaSifra = Convert.ToInt32(cbList_Korisnici.SelectedValue);
                if (cb_Procitan.Checked == true) { procitan = true; } else { procitan = false; }
                obavestenja.UpdObavestenje(Convert.ToInt32(txt_ID.Text), kreirao, PaSifra, txt_Obavestenje.Text.ToString().TrimEnd(), Convert.ToDateTime(dt_Slanje.Value.ToString()),
                    procitan, Convert.ToDateTime(dt_Citanje.Value.ToString()),  cboTipNotifikacije.Text, txt_putanja.Text, Convert.ToDateTime(dtpDatumVazenja.Value.ToString()));
            }
            FillGV();
        }

        private void tsDelete_Click(object sender, EventArgs e)
        {
            InsertObavestenje obavestenja = new InsertObavestenje();
            obavestenja.DelObavestenje(Convert.ToInt32(txt_ID.Text));
            FillGV();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var query = "select Korisnici.DeSifra,Korisnik,DmNaziv,DmSifra " +
                "From Korisnici " +
                "Inner join Delavci on Korisnici.DeSifra = Delavci.DeSifra " +
                "Inner join DelovnaMesta on Delavci.DeSifDelMes = DelovnaMesta.DmSifra " +
                "Where Korisnici.Korisnik <> '' and DmNaziv ='"+combo_RadnoMesto.SelectedValue+"' order by Korisnici.DeSifra";
            SqlConnection conn = new SqlConnection(connect);
            var da = new SqlDataAdapter(query, conn);
            var ds = new DataSet();
            da.Fill(ds);
            cbList_Korisnici.DataSource = ds.Tables[0];
            cbList_Korisnici.DisplayMember = "Korisnik";
            cbList_Korisnici.ValueMember = "DeSifra";
        }

        private void btnOsvezi_Click(object sender, EventArgs e)
        {
            FillGV();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string PictureFolder = txt_putanja.Text;
            ofd1.InitialDirectory = PictureFolder;

            if (ofd1.ShowDialog() == DialogResult.OK)
            {
                txt_putanja.Text = fbd1.SelectedPath.ToString() + ofd1.FileName;
            }
        }

        private void btn_Otvori_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(txt_putanja.Text);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            FillGVByUser();
        }
    }
}
