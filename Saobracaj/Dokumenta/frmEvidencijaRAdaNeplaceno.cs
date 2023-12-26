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
using Syncfusion.XlsIO;

namespace Saobracaj.Dokumenta
{
    public partial class frmEvidencijaRAdaNeplaceno : Form
    {
        public frmEvidencijaRAdaNeplaceno()
        {
            InitializeComponent();
            IdGrupe();
            IdForme();
            PravoPristupa();
        }
        public static string code = "frmEvidencijaRAdaNeplaceno";
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
                     //   tsNew.Enabled = false;
                    }
                    update = Convert.ToBoolean(reader["Izmena"]);
                    if (update == false)
                    {
                       // tsSave.Enabled = false;
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
        private void FillGVSvi()
        {
            var select = "";

            /*
            select = "Select Aktivnosti.ID as Zapis,  Aktivnosti.Oznaka, " +
                           " (RTrim(DeIme) + ' ' + RTRim(DePriimek)) as Zaposleni,  " +
                           "  VremeOD, VremeDo, Ukupno, UkupniTroskovi, Aktivnosti.Opis, RN,  " +
                            "   CASE WHEN Aktivnosti.PoslatEmail > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as PoslatEmail,  " +
                             " CASE WHEN Aktivnosti.Placeno > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Placeno,   RAcun, Kartica, " +
                               " CASE WHEN Aktivnosti.Masinovodja > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Masinovodja , Mesto," +
                                " CASE WHEN Aktivnosti.PlacenoRacun > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as PlaceniRacuni," +
                                 " CASE WHEN Aktivnosti.Pregledano > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Pregledano, " +
                                  " CASE WHEN Aktivnosti.Milsped > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Milsped" +
                            " , (SELECT COUNT(*) FROM AktivnostiDokumenta where AktivnostiDokumenta.IDAktivnosti = Aktivnosti.ID) as Zapisa " +
                                  " from Aktivnosti  " +
                           " inner join Delavci on Delavci.DeSifra = Aktivnosti.Zaposleni  " +
                            "  where Aktivnosti.Placeno = 0 and (Aktivnosti.PlacenoRacun = 0) and (UkupniTroskovi + RAcun) > 0 " +
                              " and CONVERT(varchar,VremeOd,104)      + ' '      + SUBSTRING(CONVERT(varchar,VremeOd,108),1,5)  <=  " +
            " CONVERT(varchar, '" + Convert.ToDateTime(dtpVremeDo.Value) + "',104)      + ' '      + SUBSTRING(CONVERT(varchar,'" + Convert.ToDateTime(dtpVremeDo.Value) + "',108),1,5) " +
                            " order by Aktivnosti.ID desc";
            */
            select = " Select Aktivnosti.ID as Zapis,  Aktivnosti.Oznaka, " +
             "    (RTrim(DeIme) + ' ' + RTRim(DePriimek)) as Zaposleni, " +
             " VremeOD, VremeDo, Ukupno, UkupniTroskovi, Aktivnosti.Opis, RN, " +
             " CASE WHEN Aktivnosti.PoslatEmail > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as PoslatEmail, " +
             " CASE WHEN Aktivnosti.Placeno > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Placeno,   RAcun, Kartica,  CASE WHEN Aktivnosti.Masinovodja > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Masinovodja , Mesto, " +
             " CASE WHEN Aktivnosti.PlacenoRacun > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as PlaceniRacuni, CASE WHEN Aktivnosti.Pregledano > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Pregledano,  CASE WHEN Aktivnosti.Milsped > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Milsped ," +
             " (SELECT COUNT(*) FROM AktivnostiDokumenta where AktivnostiDokumenta.IDAktivnosti = Aktivnosti.ID) as Zapisa,  " +
             " CASE WHEN Aktivnosti.PregledanoTroskovi > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as PregledanoTroskovi," +
              " CASE WHEN Aktivnosti.StigaoRacun > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as StigaoRacun" +
              " from Aktivnosti " +
             " inner join Delavci on Delavci.DeSifra = Aktivnosti.Zaposleni    where Aktivnosti.Placeno = 0 and (Aktivnosti.PlacenoRacun = 0) and (UkupniTroskovi + RAcun + Kartica) > 0 " +
             " And  Convert(nvarchar(10),VremeDo,126) <=  '" + dtpVremeDo.Text + "' order by Aktivnosti.ID desc";

            /*
                        select = "Select Aktivnosti.ID as Zapis,  Aktivnosti.Oznaka, " +
                                 " (RTrim(DeIme) + ' ' + RTRim(DePriimek)) as Zaposleni,  " +
                                 "  VremeOD, VremeDo, Ukupno, UkupniTroskovi, Aktivnosti.Opis, RN,  " +
                                  "   CASE WHEN Aktivnosti.PoslatEmail > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as PoslatEmail,  " +
                                   " CASE WHEN Aktivnosti.Placeno > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Placeno,   RAcun, Kartica, " +
                                     " CASE WHEN Aktivnosti.Masinovodja > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Masinovodja , Mesto," +
                                      " CASE WHEN Aktivnosti.PlacenoRacun > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as PlaceniRacuni," +
                                       " CASE WHEN Aktivnosti.Pregledano > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Pregledano, " +
                                        " CASE WHEN Aktivnosti.Milsped > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Milsped" +
                                  " , (SELECT COUNT(*) FROM AktivnostiDokumenta where AktivnostiDokumenta.IDAktivnosti = Aktivnosti.ID) as Zapisa " +
                                        " from Aktivnosti  " +
                                 " inner join Delavci on Delavci.DeSifra = Aktivnosti.Zaposleni  " +
                                  "  where Aktivnosti.Placeno = 0 and (Aktivnosti.PlacenoRacun = 0) and (UkupniTroskovi + RAcun) > 0 " +
                                  " And  VremeOd <=  '" +
                                   dtpVremeDo.Text +
                                  "' order by Aktivnosti.ID desc";
            */
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

            DataGridViewColumn column1 = dataGridView1.Columns[1];
            dataGridView1.Columns[1].HeaderText = "Oznaka";
            dataGridView1.Columns[1].Width = 30;

            DataGridViewColumn column2 = dataGridView1.Columns[2];
            dataGridView1.Columns[2].HeaderText = "Zaposleni";
            dataGridView1.Columns[2].Width = 100;

            DataGridViewColumn column3 = dataGridView1.Columns[3];
            dataGridView1.Columns[3].HeaderText = "Vreme od";
            dataGridView1.Columns[3].Width = 100;

            DataGridViewColumn column4 = dataGridView1.Columns[4];
            dataGridView1.Columns[4].HeaderText = "Vreme do";
            dataGridView1.Columns[4].Width = 100;

            DataGridViewColumn column5 = dataGridView1.Columns[5];
            dataGridView1.Columns[5].HeaderText = "Ukupno";
            dataGridView1.Columns[5].Width = 50;

            DataGridViewColumn column6 = dataGridView1.Columns[6];
            dataGridView1.Columns[6].HeaderText = "Ukupni troškovi";
            dataGridView1.Columns[6].DefaultCellStyle.BackColor = Color.Aqua;
            dataGridView1.Columns[6].Width = 50;

            DataGridViewColumn column7 = dataGridView1.Columns[7];
            dataGridView1.Columns[7].HeaderText = "Opis";
            dataGridView1.Columns[7].Width = 120;

            DataGridViewColumn column8 = dataGridView1.Columns[8];
            dataGridView1.Columns[8].HeaderText = "RN";
            dataGridView1.Columns[8].Width = 50;

            DataGridViewColumn column9 = dataGridView1.Columns[9];
            dataGridView1.Columns[9].HeaderText = "Poslat Email";
            dataGridView1.Columns[9].Width = 50;

            DataGridViewColumn column10 = dataGridView1.Columns[10];
            dataGridView1.Columns[10].HeaderText = "Plaćeno";
            dataGridView1.Columns[10].DefaultCellStyle.BackColor = Color.OrangeRed;
            dataGridView1.Columns[10].Width = 50;

            DataGridViewColumn column11 = dataGridView1.Columns[11];
            dataGridView1.Columns[11].HeaderText = "Računi";
            dataGridView1.Columns[11].DefaultCellStyle.BackColor = Color.CadetBlue;
            dataGridView1.Columns[11].Width = 50;

            DataGridViewColumn column12 = dataGridView1.Columns[12];
            dataGridView1.Columns[12].HeaderText = "Kartice";
            dataGridView1.Columns[12].Width = 50;

            DataGridViewColumn column13 = dataGridView1.Columns[13];
            dataGridView1.Columns[13].HeaderText = "Masinovodja";
            dataGridView1.Columns[13].Width = 50;

            DataGridViewColumn column14 = dataGridView1.Columns[14];
            dataGridView1.Columns[14].HeaderText = "Mesto";
            dataGridView1.Columns[14].Width = 100;

            DataGridViewColumn column15 = dataGridView1.Columns[15];
            dataGridView1.Columns[15].HeaderText = "Pregledano računi";
            dataGridView1.Columns[15].Width = 50;

            DataGridViewColumn column16 = dataGridView1.Columns[16];
            dataGridView1.Columns[16].HeaderText = "Pregledano kartice";
            dataGridView1.Columns[16].Width = 50;

            DataGridViewColumn column17 = dataGridView1.Columns[17];
            dataGridView1.Columns[17].HeaderText = "Milšped";
            dataGridView1.Columns[17].Width = 50;

            DataGridViewColumn column18 = dataGridView1.Columns[18];
            dataGridView1.Columns[18].HeaderText = "Zapisa";
            dataGridView1.Columns[18].Width = 50;


            dataGridView1.Columns[13].Visible = false;
            dataGridView1.Columns[14].Visible = false;
            dataGridView1.Columns[15].Visible = false;
            dataGridView1.Columns[16].Visible = false;
            dataGridView1.Columns[17].Visible = false;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if ((bool)row.Cells[19].Value == true)
                {
                    row.DefaultCellStyle.BackColor = Color.Yellow;
                }
                if ((bool)row.Cells[20].Value == true)
                {
                    row.DefaultCellStyle.BackColor = Color.Yellow;
                }
                if ((bool)row.Cells[19].Value == true && (bool)row.Cells[20].Value == true)
                {
                    row.DefaultCellStyle.BackColor = Color.Green;
                }
            }

            dataGridView1.Columns[20].HeaderText = "Stigli računi";

        }
        int svi = 0;
        int radnik = 0;
        private void btnPretrazi_Click(object sender, EventArgs e)
        {
            FillGVSvi();
            svi = 1;
            radnik = 0;
        }
        private void FillGVRadnik()
        {
            var select = "";

            select = "Select Aktivnosti.ID as Zapis,  Aktivnosti.Oznaka, " +
                           " (RTrim(DeIme) + ' ' + RTRim(DePriimek)) as Zaposleni,  " +
                           "  VremeOD, VremeDo, Ukupno, UkupniTroskovi, Aktivnosti.Opis, RN,  " +
                            "   CASE WHEN Aktivnosti.PoslatEmail > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as PoslatEmail,  " +
                             " CASE WHEN Aktivnosti.Placeno > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Placeno,   RAcun, Kartica, " +
                               " CASE WHEN Aktivnosti.Masinovodja > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Masinovodja , MestoUpucivanja," +
                                " CASE WHEN Aktivnosti.PlacenoRacun > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as PlaceniRacuni," +
                                 " CASE WHEN Aktivnosti.Pregledano > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Pregledano, " +
                                  " CASE WHEN Aktivnosti.Milsped > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Milsped" +
                            " , (SELECT COUNT(*) FROM AktivnostiDokumenta where AktivnostiDokumenta.IDAktivnosti = Aktivnosti.ID) as Zapisa " +
                                  " from Aktivnosti  " +
                           " inner join Delavci on Delavci.DeSifra = Aktivnosti.Zaposleni  " +
                            "  where Aktivnosti.Placeno = 0 and (Aktivnosti.PlacenoRacun = 0) and (UkupniTroskovi + RAcun + Kartica) > 0  > 0 and Zaposleni = " + Convert.ToInt32(cboZaposleni.SelectedValue) +
                             " And  Convert(nvarchar(10),VremeDo,126) <  '" + dtpVremeDo.Text + "' order by Aktivnosti.ID desc";


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

            DataGridViewColumn column1 = dataGridView1.Columns[1];
            dataGridView1.Columns[1].HeaderText = "Oznaka";
            dataGridView1.Columns[1].Width = 30;

            DataGridViewColumn column2 = dataGridView1.Columns[2];
            dataGridView1.Columns[2].HeaderText = "Zaposleni";
            dataGridView1.Columns[2].Width = 100;

            DataGridViewColumn column3 = dataGridView1.Columns[3];
            dataGridView1.Columns[3].HeaderText = "Vreme od";
            dataGridView1.Columns[3].Width = 100;

            DataGridViewColumn column4 = dataGridView1.Columns[4];
            dataGridView1.Columns[4].HeaderText = "Vreme do";
            dataGridView1.Columns[4].Width = 100;

            DataGridViewColumn column5 = dataGridView1.Columns[5];
            dataGridView1.Columns[5].HeaderText = "Ukupno";
            dataGridView1.Columns[5].Width = 50;

            DataGridViewColumn column6 = dataGridView1.Columns[6];
            dataGridView1.Columns[6].HeaderText = "Ukupni troškovi";
            dataGridView1.Columns[6].DefaultCellStyle.BackColor = Color.Aqua;
            dataGridView1.Columns[6].Width = 50;

            DataGridViewColumn column7 = dataGridView1.Columns[7];
            dataGridView1.Columns[7].HeaderText = "Opis";
            dataGridView1.Columns[7].Width = 120;

            DataGridViewColumn column8 = dataGridView1.Columns[8];
            dataGridView1.Columns[8].HeaderText = "RN";
            dataGridView1.Columns[8].Width = 50;

            DataGridViewColumn column9 = dataGridView1.Columns[9];
            dataGridView1.Columns[9].HeaderText = "Poslat Email";
            dataGridView1.Columns[9].Width = 50;

            DataGridViewColumn column10 = dataGridView1.Columns[10];
            dataGridView1.Columns[10].HeaderText = "Plaćeno";
            dataGridView1.Columns[10].DefaultCellStyle.BackColor = Color.OrangeRed;
            dataGridView1.Columns[10].Width = 50;

            DataGridViewColumn column11 = dataGridView1.Columns[11];
            dataGridView1.Columns[11].HeaderText = "Računi";
            dataGridView1.Columns[11].DefaultCellStyle.BackColor = Color.CadetBlue;
            dataGridView1.Columns[11].Width = 50;

            DataGridViewColumn column12 = dataGridView1.Columns[12];
            dataGridView1.Columns[12].HeaderText = "Kartice";
            dataGridView1.Columns[12].Width = 50;

            DataGridViewColumn column13 = dataGridView1.Columns[13];
            dataGridView1.Columns[13].HeaderText = "Masinovodja";
            dataGridView1.Columns[13].Width = 50;

            DataGridViewColumn column14 = dataGridView1.Columns[14];
            dataGridView1.Columns[14].HeaderText = "Mesto";
            dataGridView1.Columns[14].Width = 100;

            DataGridViewColumn column15 = dataGridView1.Columns[15];
            dataGridView1.Columns[15].HeaderText = "Pregledano računi";
            dataGridView1.Columns[15].Width = 50;

            DataGridViewColumn column16 = dataGridView1.Columns[16];
            dataGridView1.Columns[16].HeaderText = "Pregledano kartice";
            dataGridView1.Columns[16].Width = 50;

            DataGridViewColumn column17 = dataGridView1.Columns[17];
            dataGridView1.Columns[17].HeaderText = "Milšped";
            dataGridView1.Columns[17].Width = 50;

            DataGridViewColumn column18 = dataGridView1.Columns[18];
            dataGridView1.Columns[18].HeaderText = "Zapisa";
            dataGridView1.Columns[18].Width = 50;

            dataGridView1.Columns[13].Visible = false;
            dataGridView1.Columns[14].Visible = false;
            dataGridView1.Columns[15].Visible = false;
            dataGridView1.Columns[16].Visible = false;
            dataGridView1.Columns[17].Visible = false;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if ((bool)row.Cells[19].Value == true)
                {
                    row.DefaultCellStyle.BackColor = Color.Yellow;
                }
                if ((bool)row.Cells[20].Value == true)
                {
                    row.DefaultCellStyle.BackColor = Color.Yellow;
                }
                if ((bool)row.Cells[19].Value == true && (bool)row.Cells[20].Value == true)
                {
                    row.DefaultCellStyle.BackColor = Color.Green;
                }
            }
            dataGridView1.Columns[20].HeaderText = "Stigli računi";
        }
        private void button1_Click(object sender, EventArgs e)
        {
            FillGVRadnik();
            radnik = 1;
            svi = 0;
        }

        private void frmEvidencijaRAdaNeplaceno_Load(object sender, EventArgs e)
        {
            tabControl1.TabPages.Remove(tabPage2);
            panel1.Visible = false;
            panel2.Visible = false;
            button14.Visible = false;
            button15.Visible = false;
            button6.Text = "Prebaci u banku";

                     var select3 = " select DeSifra as ID, (Rtrim(DePriimek) + ' ' + RTrim(DeIme)) as Opis from Delavci  order by opis";
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


            var select4 = " select DeSifra as ID, (Rtrim(DePriimek) + ' ' + RTrim(DeIme)) as Opis from Delavci where DeSifStat <> 'P' order by opis";
            var s_connection4 = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection4 = new SqlConnection(s_connection4);
            var c4 = new SqlConnection(s_connection4);
            var dataAdapter4 = new SqlDataAdapter(select4, c4);

            var commandBuilder4 = new SqlCommandBuilder(dataAdapter4);
            var ds4 = new DataSet();
            dataAdapter4.Fill(ds4);
            cboPregledac.DataSource = ds4.Tables[0];
            cboPregledac.DisplayMember = "Opis";
            cboPregledac.ValueMember = "ID";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (txtSifra.Text == "DejanIvan")
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Selected == true)
                    {
                        InsertAktivnosti ins = new InsertAktivnosti();
                        ins.UpdateAktivnostiPlaceno(Convert.ToInt32(row.Cells[0].Value.ToString()), Convert.ToDateTime(dtpVremePlaceno.Value));

                    }
                }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Selected == true)
                {
                    InsertAktivnosti ins = new InsertAktivnosti();
                    ins.UpdateAktivnostiPlacenoRacuni(Convert.ToInt32(row.Cells[0].Value.ToString()), Convert.ToInt32(cboPregledac.SelectedValue));
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Selected == true)
                {
                    {
                        frmEvidencijaRada er = new frmEvidencijaRada(Convert.ToInt32(row.Cells[0].Value.ToString()), "");
                        er.Show();
                    }
                }
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Selected == true)
                {
                    //Misli se na troskove
                    InsertAktivnosti ins = new InsertAktivnosti();
                    ins.UpdateAktivnostiPregledanoKartice(Convert.ToInt32(row.Cells[0].Value.ToString()));
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            PremostiDataGridView();
       
            var select = "";

            int tmpStigao = 0;
            int tmpPregledan = 0;

            if (chkStigli.Checked == true)
            { tmpStigao = 1; }

            if (chkPregledani.Checked == true)
            { tmpPregledan = 1; }

            select = " Select Aktivnosti.ID as Zapis,  Aktivnosti.Oznaka,  " +
 " (RTrim(DeIme) + ' ' + RTRim(DePriimek)) as Zaposleni, " +
 "   VremeOD, VremeDo, Ukupno as UkupnoVreme,     RAcun,  " +
  "   CASE WHEN Aktivnosti.StigaoRacun > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as StigaoRAcun, " +
  "   CASE WHEN Aktivnosti.Pregledano > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Pregledano, " +
     "     (SELECT COUNT(*) FROM AktivnostiDokumenta where AktivnostiDokumenta.IDAktivnosti = Aktivnosti.ID) as Zapisa " +
     "   from Aktivnosti inner join Delavci on Delavci.DeSifra = Aktivnosti.Zaposleni    " +
     "where (RACUN) > 0 and Convert(nvarchar(10), VremeDo, 126) >='" + dtpVremeOd.Text + 
     "' And Convert(nvarchar(10), VremeDo, 126) <='" + dtpVremeDo.Text + "'  and StigaoRacun = " + tmpStigao + "And Pregledano = " + tmpPregledan +
"  order by Aktivnosti.ID desc ";
           

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

            DataGridViewColumn column1 = dataGridView1.Columns[1];
            dataGridView1.Columns[1].HeaderText = "Oznaka";
            dataGridView1.Columns[1].Visible = false;
            dataGridView1.Columns[1].Width = 30;

            DataGridViewColumn column2 = dataGridView1.Columns[2];
            dataGridView1.Columns[2].HeaderText = "Zaposleni";
            dataGridView1.Columns[2].Width = 100;

            RefreshCellColor();

            
        }
        private void RefreshCellColor()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (Convert.ToInt32(row.Cells[7].Value) == 0)
                {
                    row.Cells[6].Style.BackColor = Color.Red;
                }
                else if (Convert.ToInt32(row.Cells[7].Value) == 1 && Convert.ToInt32(row.Cells[8].Value) == 0)
                {
                    // row.DefaultCellStyle.BackColor = Color.LightSalmon; // Use it in order to colorize all cells of the row

                    row.Cells[6].Style.BackColor = Color.Yellow;
                }
                else
                {
                    row.Cells[6].Style.BackColor = Color.Green;
                }
            }



        }

        private void RefreshCellColorSvi()
        {
            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                if (Convert.ToInt32(row.Cells[7].Value) == 0 && Convert.ToDecimal(row.Cells[6].Value.ToString()) > 0)
                {
                    row.Cells[6].Style.BackColor = Color.Red;
                }
                else if (Convert.ToInt32(row.Cells[7].Value) == 1 && Convert.ToInt32(row.Cells[8].Value) == 0 && Convert.ToDecimal(row.Cells[6].Value) > 0)
                {
                    // row.DefaultCellStyle.BackColor = Color.LightSalmon; // Use it in order to colorize all cells of the row

                    row.Cells[6].Style.BackColor = Color.Yellow;
                }
                else
                {
                    row.Cells[6].Style.BackColor = Color.Green;
                }

                if (Convert.ToInt32(row.Cells[11].Value) == 0 && Convert.ToDecimal(row.Cells[10].Value) > 0)
                {
                    row.Cells[10].Style.BackColor = Color.Red;
                }
                else if (Convert.ToInt32(row.Cells[11].Value) == 1 && Convert.ToInt32(row.Cells[12].Value) == 0 && Convert.ToDecimal(row.Cells[10].Value) > 0)
                {
                    // row.DefaultCellStyle.BackColor = Color.LightSalmon; // Use it in order to colorize all cells of the row

                    row.Cells[10].Style.BackColor = Color.Yellow;
                }
                else
                {
                    row.Cells[10].Style.BackColor = Color.Green;
                }

                if (Convert.ToInt32(row.Cells[15].Value) == 0 && Convert.ToDecimal(row.Cells[14].Value) > 0)
                {
                    row.Cells[14].Style.BackColor = Color.Red;
                }
                else if (Convert.ToInt32(row.Cells[15].Value) == 1 && Convert.ToInt32(row.Cells[16].Value) == 0 && Convert.ToDecimal(row.Cells[14].Value) > 0)
                {
                    row.Cells[14].Style.BackColor = Color.Yellow;
                }
                else
                {
                    row.Cells[14].Style.BackColor = Color.Green;
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            PremostiDataGridView();

            var select = "";

            int tmpStigao = 0;
            int tmpPregledan = 0;

            if (chkStigli.Checked == true)
            { tmpStigao = 1; }

            if (chkPregledani.Checked == true)
            { tmpPregledan = 1; }

            select = " Select Aktivnosti.ID as Zapis,  Aktivnosti.Oznaka,  " +
 " (RTrim(DeIme) + ' ' + RTRim(DePriimek)) as Zaposleni, " +
 "   VremeOD, VremeDo, Ukupno as UkupnoVreme,     UkupniTroskovi,  " +
  "   CASE WHEN Aktivnosti.StigaoTrosak > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as StigaoTrosak, " +
  "   CASE WHEN Aktivnosti.PregledanoTroskovi > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as PregledanoTrosak, " +
     "     (SELECT COUNT(*) FROM AktivnostiDokumenta where AktivnostiDokumenta.IDAktivnosti = Aktivnosti.ID) as Zapisa " +
     "   from Aktivnosti inner join Delavci on Delavci.DeSifra = Aktivnosti.Zaposleni    " +
     "where (UkupniTroskovi) > 0 and Convert(nvarchar(10), VremeDo, 126) >='" + dtpVremeOd.Text +
     "' And Convert(nvarchar(10), VremeDo, 126) <='" + dtpVremeDo.Text + "'  and StigaoTrosak= " + tmpStigao + "And PregledanoTroskovi = " + tmpPregledan +
"  order by Aktivnosti.ID desc ";


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

            DataGridViewColumn column1 = dataGridView1.Columns[1];
            dataGridView1.Columns[1].HeaderText = "Oznaka";
            dataGridView1.Columns[1].Visible = false;
            dataGridView1.Columns[1].Width = 30;

            DataGridViewColumn column2 = dataGridView1.Columns[2];
            dataGridView1.Columns[2].HeaderText = "Zaposleni";
            dataGridView1.Columns[2].Width = 100;

            RefreshCellColor();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            PremostiDataGridView();
    

            var select = "";

            int tmpStigao = 0;
            int tmpPregledan = 0;

            if (chkStigli.Checked == true)
            { tmpStigao = 1; }

            if (chkPregledani.Checked == true)
            { tmpPregledan = 1; }

            select = " Select Aktivnosti.ID as Zapis,  Aktivnosti.Oznaka,  " +
            " (RTrim(DeIme) + ' ' + RTRim(DePriimek)) as Zaposleni, " +
            "   VremeOD, VremeDo, Ukupno as UkupnoVreme,     Kartica,  " +
            "   CASE WHEN Aktivnosti.StigaoKartica > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as StigaoKartica, " +
            "  CASE WHEN Aktivnosti.PregledanoKartice > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as PregledanoKartice, " +
     "     (SELECT COUNT(*) FROM AktivnostiDokumenta where AktivnostiDokumenta.IDAktivnosti = Aktivnosti.ID) as Zapisa " +
     "   from Aktivnosti inner join Delavci on Delavci.DeSifra = Aktivnosti.Zaposleni    " +
     "where (Kartica) > 0 and Convert(nvarchar(10), VremeDo, 126) >='" + dtpVremeOd.Text +
     "' And Convert(nvarchar(10), VremeDo, 126) <='" + dtpVremeDo.Text + "'  and StigaoKartica = " + tmpStigao + " and PregledanoKartice = " + tmpPregledan +
"  order by Aktivnosti.ID desc ";


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

            DataGridViewColumn column1 = dataGridView1.Columns[1];
            dataGridView1.Columns[1].HeaderText = "Oznaka";
            dataGridView1.Columns[1].Width = 30;

            DataGridViewColumn column2 = dataGridView1.Columns[2];
            dataGridView1.Columns[2].HeaderText = "Zaposleni";
            dataGridView1.Columns[2].Width = 100;

            RefreshCellColor();

        }

        private void button10_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Selected == true)
                {
                    //Misli se na racune
                    InsertAktivnosti ins = new InsertAktivnosti();
                    ins.UpdateAktivnostiPregledano(Convert.ToInt32(row.Cells[0].Value.ToString()));
                }
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Selected == true)
                {
                    //Misli se na troskove
                    InsertAktivnosti ins = new InsertAktivnosti();
                    ins.UpdateAktivnostiPregledanoTroskovi(Convert.ToInt32(row.Cells[0].Value.ToString()));
                }
            }
        }

        private void PremostiDataGridView()
        {
            dataGridView1.Refresh();
            var select = "";

            select = " Select 1 ";

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

        private void button11_Click(object sender, EventArgs e)
        {
           

       
        }

        private void button11_Click_1(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Selected == true)
                {
                    //Misli se na racune
                    InsertAktivnosti ins = new InsertAktivnosti();
                    ins.UpdateAktivnostiStigao(Convert.ToInt32(row.Cells[0].Value.ToString()));
                }
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Selected == true)
                {
                    //Misli se na troskove
                    InsertAktivnosti ins = new InsertAktivnosti();
                    ins.UpdateAktivnostiStigaoTroskovi(Convert.ToInt32(row.Cells[0].Value.ToString()));
                }
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Selected == true)
                {
                    //Misli se na troskove
                    InsertAktivnosti ins = new InsertAktivnosti();
                    ins.UpdateAktivnostiStigaoKartice(Convert.ToInt32(row.Cells[0].Value.ToString()));
                }
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            PremostiDataGridView();

            var select = "";

          
            select = " Select Aktivnosti.ID as Zapis,  Aktivnosti.Oznaka,  " +
  " (RTrim(DeIme) + ' ' + RTRim(DePriimek)) as Zaposleni, " +
  "   VremeOD, VremeDo, Ukupno as UkupnoVreme,     RAcun,  " +
   "   CASE WHEN Aktivnosti.StigaoRacun > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as StigaoRAcun, " +
   "   CASE WHEN Aktivnosti.Pregledano > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as Pregledano, " +
   "   CASE WHEN Aktivnosti.PlacenoRacun > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as PlaceniRacuni, " +
    "   UkupniTroskovi,  " +
     "   CASE WHEN Aktivnosti.StigaoTrosak > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as StigaoTrosak," +
     "    CASE WHEN Aktivnosti.PregledanoTroskovi > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as PregledanoTroskovi," +
      "    CASE WHEN Aktivnosti.PlacenoTroskovi > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as PlacenoTroskovi," +
      "     Kartica ," +
       "    CASE WHEN Aktivnosti.StigaoKartica > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as StigaoKArtica, " +
       "  CASE WHEN Aktivnosti.PregledanoKartice > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as PregledanoKartice, " + 
      "     (SELECT COUNT(*) FROM AktivnostiDokumenta where AktivnostiDokumenta.IDAktivnosti = Aktivnosti.ID) as Zapisa " +
      "   from Aktivnosti inner join Delavci on Delavci.DeSifra = Aktivnosti.Zaposleni    where (RACUN + UkupniTroskovi + Kartica) > 0 and Convert(nvarchar(10), VremeDo, 126) >='" + dtpVremeOd.Text + "' And Convert(nvarchar(10), VremeDo, 126) <='" + dtpVremeDo.Text + "'  " +
"  order by Aktivnosti.ID desc ";
       

            
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
            dataGridView2.Columns[0].Width = 30;

            DataGridViewColumn column1 = dataGridView2.Columns[1];
            dataGridView2.Columns[1].HeaderText = "Oznaka";
            dataGridView2.Columns[1].Visible = false;
            dataGridView2.Columns[1].Width = 30;

            DataGridViewColumn column2 = dataGridView2.Columns[2];
            dataGridView2.Columns[2].HeaderText = "Zaposleni";
            dataGridView2.Columns[2].Width = 100;


            dataGridView1.Refresh();
            RefreshCellColorSvi();
        }

        private void button15_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                if (row.Selected == true)
                {
                    {
                        frmEvidencijaRada er = new frmEvidencijaRada(Convert.ToInt32(row.Cells[0].Value.ToString()), "");
                        er.Show();
                    }
                }
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Selected == true)
                {
                    //Misli se na troskove
                    InsertAktivnosti ins = new InsertAktivnosti();
                    ins.UpdateAktivnostiPregledanoTroskovi(Convert.ToInt32(row.Cells[0].Value.ToString()));
                }
            }
            if (radnik == 1) { FillGVRadnik(); }else if (svi == 1) { FillGVSvi(); } else { return; }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Selected == true)
                {
                    //Misli se na racune
                    InsertAktivnosti ins = new InsertAktivnosti();
                    ins.UpdateAktivnostiStigao(Convert.ToInt32(row.Cells[0].Value.ToString()));
                }
            }
            if (radnik == 1) { FillGVRadnik(); } else if (svi == 1) { FillGVSvi(); } else { return; }
        }
    }
}