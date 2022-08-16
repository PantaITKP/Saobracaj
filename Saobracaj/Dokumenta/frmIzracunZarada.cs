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
    public partial class frmIzracunZarada : Form
    {
        public frmIzracunZarada()
        {
            InitializeComponent();
            IdGrupe();
            IdForme();
            PravoPristupa();
        }
        string niz = "";
        public static string code = "frmIzracunZarada";
        public bool Pravo;
        int idGrupe;
        int idForme;
        bool insert;
        bool update;
        bool delete;
        string Kor = Sifarnici.frmLogovanje.user.ToString();
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
                       // tsNew.Enabled = false;
                    }
                    update = Convert.ToBoolean(reader["Izmena"]);
                    if (update == false)
                    {
                        //tsSave.Enabled = false;
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
        private void dtpVremeDo_ValueChanged(object sender, EventArgs e)
        {

        }
        public void StornirajCelePN(int Zaposleni, int PNCeli, int PNPola, int PNCeliBrisanje, int PNPolaBrisanje)
        { 

            
             var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection con = new SqlConnection(s_connection);

            con.Open();

            SqlCommand cmd = new SqlCommand("select top " + PNCeliBrisanje + "  PnStZapisa from PotNal " +
" where PnDelavec = " + Zaposleni + " and PnZnesOrg = 2349 and PnStatus = 'OD' " + 
" Order By PnStZapisa desc ", con);
            SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {

                InsertObracunSati ins = new InsertObracunSati();
                ins.UpdPN(Convert.ToInt32(dr["PnZapisa"].ToString()));
               
            }

            con.Close();
        
        }

        private void PNZaBrisanje()
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
                    SqlConnection con = new SqlConnection(s_connection);

                    con.Open();
                  
                    string Top1 = Convert.ToInt32(row.Cells[19].Value).ToString();
                    string Top2 = Convert.ToInt32(row.Cells[17].Value).ToString();
                    string Radnik = Convert.ToInt32(row.Cells[0].Value).ToString();
                    string sqlupit = " select Top " + Top1 + " t1.PnStZapisa as PnStZapisa, t1.Duration from ( "
 + "select top  " + Top2 + "  PnStZapisa,DATEDIFF(MINUTE,PnDatOdh, PnDatPrih) AS Duration from PotNal  where PnDelavec = " + Radnik + " and PnZnesOrg = 2617 and PnStatus = 'OD'  Order By PnStZapisa desc) t1 "
 + "order by t1.Duration asc ";

                    SqlCommand cmd = new SqlCommand(sqlupit, con);
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {

                        InsertObracunSati ins = new InsertObracunSati();
                        ins.UpdPN(Convert.ToInt32(dr["PnStZapisa"].ToString()));

                    }

                    con.Close();
                   // StornirajCelePN(Convert.ToInt32(row.Cells[0].Value.ToString()), Convert.ToInt32(row.Cells[14].Value.ToString()), Convert.ToInt32(row.Cells[15].Value.ToString()), Convert.ToInt32(row.Cells[16].Value.ToString()), Convert.ToInt32(row.Cells[17].Value.ToString()));
                }
            }
            catch
            {
                MessageBox.Show("Nije uspela selekcija stavki");
            }

        }

        private void PNZaBrisanjePola()
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
                    SqlConnection con = new SqlConnection(s_connection);

                    con.Open();
                    string Top1 = Convert.ToInt32(row.Cells[20].Value).ToString();
                    string Top2 = Convert.ToInt32(row.Cells[18].Value).ToString();
                    string Radnik = Convert.ToInt32(row.Cells[0].Value).ToString();
                    SqlCommand cmd = new SqlCommand(" select Top " + Top1 + " t1.PnStZapisa as PnStZapisa, t1.Duration from ( "
+ "select top  " + Top2 + "  PnStZapisa,DATEDIFF(MINUTE,PnDatOdh, PnDatPrih) AS 'Duration' from PotNal  where PnDelavec = " + Radnik + " and PnZnesOrg = 1308 and PnStatus = 'OD'  Order By PnStZapisa desc) t1 "
+ "order by t1.Duration asc ", con);
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {

                        InsertObracunSati ins = new InsertObracunSati();
                        ins.UpdPN(Convert.ToInt32(dr["PnStZapisa"].ToString()));

                    }

                    con.Close();
                    // StornirajCelePN(Convert.ToInt32(row.Cells[0].Value.ToString()), Convert.ToInt32(row.Cells[14].Value.ToString()), Convert.ToInt32(row.Cells[15].Value.ToString()), Convert.ToInt32(row.Cells[16].Value.ToString()), Convert.ToInt32(row.Cells[17].Value.ToString()));
                }
            }
            catch
            {
                MessageBox.Show("Nije uspela selekcija stavki");
            }

        }

        private void RefreshDataGrid()
        {
            if (txtPassword.Text != "iv4321")
            {
                return;

            }
            var select = "";

            select = " SELECT ObracunZaposleni.[ID] ,ObracunZaposleni.[Zaposleni],[VanLokomotive],[Lokomotiva]  ,[Milsped] as Praznik   ,Kragujevac as Prekovremeno, Remont as Bol65, Smederevo as Bol100,[Ukupno1] ,[Ciljna] , " +
" ObracunZaposleni.[Osnovna],[Kazna],[UkupnoDIN],PrimaMinimalac, KaznaMinimalac, KaznaUkupno,[PutniNalozi],[PutniNaloziBroj], PutniNaloziBrojPola, " +
" PutniNaloziBrisanjeCeli,PutniNaloziBrisanjePola, Dodatak, [MinusPutni] , " +
" [MinusPutniOsnovna], ObracunZaposleni.Prevoz, UkupnoSaPrevozom as IznosDIN,  " +
 " CenaSata, OsnovnaZarada1, OsnovnaZarada2, IznosPraznik, IznosPrekovremeno, IznosBolovanje65, IznosBolovanje100, ObracunZaposleni.Regres, ObracunZaposleni.TopliObrok, GoSati, GOIznos, RedovnoSati,RedovnoSatiIznos,  [BrutoCSata] "+
  "    ,[MesecniFondSati]       ,[PrekovremeneoSati]       ,[RedovanRadSati]       ,[UcinakSati] "+
   "   ,[RedovanRadIZ]      ,[GodOSati]       ,[GodIZNOS]      ,[BOL100Sati]      ,[BOL100IZNOS]      ,[BOL65Sati] "+
    " ,[BOL65IZNOS] from ObracunZaposleni inner join Zarada on ObracunZaposleni.ID = Zarada.Zaposleni  where Zarada.Fiksna = 0  ";

            //PoreskaOlaksica,BrutoZarada,BrutoCenaSata,PrekovremeniCenaSata, PrekovremeniBrutoIznos

            /*
       SELECT [ID]
      ,[VanLokomotive]
      ,[Lokomotiva]
      ,[Milsped]
      ,[Zaposleni]
      ,[PutniNalozi]
      ,[PutniNaloziBroj]
      ,[Ukupno1]
      ,[Ciljna]
      ,[Osnovna]
       ,[Kazna]
      ,[UkupnoDIN]
      ,[MinusPutni]
      ,[MinusPutniOsnovna]
      FROM [Perftech_Beograd].[dbo].[ObracunZaposleni]
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
            dataGridView1.Columns[1].HeaderText = "Zaposleni";
            dataGridView1.Columns[1].Width = 130;


             


            DataGridViewColumn column2 = dataGridView1.Columns[2];
            dataGridView1.Columns[2].HeaderText = "VanLokomotive";
            dataGridView1.Columns[2].Width = 50;

            DataGridViewColumn column3 = dataGridView1.Columns[3];
            dataGridView1.Columns[3].HeaderText = "Lokomotiva";
            dataGridView1.Columns[3].Width = 50;

            DataGridViewColumn column4 = dataGridView1.Columns[4];
            dataGridView1.Columns[4].HeaderText = "Praznik";
            dataGridView1.Columns[4].Width = 50;

            DataGridViewColumn column5 = dataGridView1.Columns[5];
            dataGridView1.Columns[5].HeaderText = "Prekovremeno";
            dataGridView1.Columns[5].Width = 50;

            DataGridViewColumn column6 = dataGridView1.Columns[6];
            dataGridView1.Columns[6].HeaderText = "Bol 65";
            dataGridView1.Columns[6].Width = 50;

            DataGridViewColumn column7 = dataGridView1.Columns[7];
            dataGridView1.Columns[7].HeaderText = "Bol 100";
            dataGridView1.Columns[7].Width = 50;

            DataGridViewColumn column8 = dataGridView1.Columns[8];
            dataGridView1.Columns[8].HeaderText = "Ukupno1";
            dataGridView1.Columns[8].Width = 80;

            DataGridViewColumn column9 = dataGridView1.Columns[9];
            dataGridView1.Columns[9].HeaderText = "Ciljna";
            dataGridView1.Columns[9].Width = 70;

            DataGridViewColumn column10 = dataGridView1.Columns[10];
            dataGridView1.Columns[10].HeaderText = "Osnovna";
            dataGridView1.Columns[10].Width = 70;

            DataGridViewColumn column11 = dataGridView1.Columns[11];
            dataGridView1.Columns[11].HeaderText = "Kazna";
            dataGridView1.Columns[11].Visible = false;
            dataGridView1.Columns[11].Width = 30;

            DataGridViewColumn column12 = dataGridView1.Columns[12];
            dataGridView1.Columns[12].HeaderText = "UkupnoRSD";
            dataGridView1.Columns[12].Width = 80;

            DataGridViewColumn column13 = dataGridView1.Columns[13];
            dataGridView1.Columns[13].HeaderText = "Prima min";
            dataGridView1.Columns[13].Visible = false;
            dataGridView1.Columns[13].Width = 50;

            DataGridViewColumn column14 = dataGridView1.Columns[14];
            dataGridView1.Columns[14].HeaderText = "Kazna min";
            dataGridView1.Columns[14].Visible = false;
            dataGridView1.Columns[14].Width = 50;

            DataGridViewColumn column15 = dataGridView1.Columns[15];
            dataGridView1.Columns[15].HeaderText = "Kazna sum";
            dataGridView1.Columns[15].Visible = false;
            dataGridView1.Columns[15].Width = 50;

            DataGridViewColumn column16 = dataGridView1.Columns[16];
            dataGridView1.Columns[16].HeaderText = "PN";
            dataGridView1.Columns[16].Width = 80;


            DataGridViewColumn column17 = dataGridView1.Columns[17];
            dataGridView1.Columns[17].HeaderText = "PN broj";
            dataGridView1.Columns[17].Width = 70;


            DataGridViewColumn column18 = dataGridView1.Columns[18];
            dataGridView1.Columns[18].HeaderText = "PN broj pola";
            dataGridView1.Columns[18].Width = 70;

            DataGridViewColumn column19 = dataGridView1.Columns[19];
            dataGridView1.Columns[19].HeaderText = "PN brisanje celi";
            dataGridView1.Columns[19].Width = 70;

            DataGridViewColumn column20 = dataGridView1.Columns[20];
            dataGridView1.Columns[20].HeaderText = "PN brisanje pola";
            dataGridView1.Columns[20].Width = 70;


            DataGridViewColumn column21 = dataGridView1.Columns[21];
            dataGridView1.Columns[21].HeaderText = "Dodatak";
            dataGridView1.Columns[21].Width = 70;

            DataGridViewColumn column22 = dataGridView1.Columns[22];
            dataGridView1.Columns[22].HeaderText = "Minus putni";
            dataGridView1.Columns[22].Width = 70;

            DataGridViewColumn column23 = dataGridView1.Columns[23];
            dataGridView1.Columns[23].HeaderText = "Minus putni osnovna";
            dataGridView1.Columns[23].Width = 70;

            DataGridViewColumn column24 = dataGridView1.Columns[24];
            dataGridView1.Columns[24].HeaderText = "Prevoz";
            dataGridView1.Columns[24].Width = 80;


            DataGridViewColumn column25 = dataGridView1.Columns[25];
            dataGridView1.Columns[25].HeaderText = "Iznos RSD";
            dataGridView1.Columns[25].Width = 80;

            DataGridViewColumn column26 = dataGridView1.Columns[26];
            dataGridView1.Columns[26].HeaderText = "Cena sata";
            dataGridView1.Columns[26].Width = 80;

            DataGridViewColumn column27 = dataGridView1.Columns[27];
            dataGridView1.Columns[27].HeaderText = "Osnovna zarada 1";
            dataGridView1.Columns[27].Width = 80;

            DataGridViewColumn column28 = dataGridView1.Columns[28];
            dataGridView1.Columns[28].HeaderText = "Osnovna zarada 2";
            dataGridView1.Columns[28].Width = 80;

            DataGridViewColumn column29 = dataGridView1.Columns[29];
            dataGridView1.Columns[29].HeaderText = "Iznos Praznik";
            dataGridView1.Columns[29].Width = 80;


            DataGridViewColumn column30 = dataGridView1.Columns[30];
            dataGridView1.Columns[30].HeaderText = "Iznos prekovremeno";
            dataGridView1.Columns[30].Width = 80;

            DataGridViewColumn column31 = dataGridView1.Columns[31];
            dataGridView1.Columns[31].HeaderText = "Iznos Bol 65";
            dataGridView1.Columns[31].Width = 80;

            DataGridViewColumn column32 = dataGridView1.Columns[32];
            dataGridView1.Columns[32].HeaderText = "Iznos Bol 100";
            dataGridView1.Columns[32].Width = 80;

            DataGridViewColumn column33 = dataGridView1.Columns[33];
            dataGridView1.Columns[33].HeaderText = "Regres";
            dataGridView1.Columns[33].Width = 80;

            DataGridViewColumn column34 = dataGridView1.Columns[34];
            dataGridView1.Columns[34].HeaderText = "Topli obrok";
            dataGridView1.Columns[34].Width = 80;

            DataGridViewColumn column35 = dataGridView1.Columns[35];
            dataGridView1.Columns[35].HeaderText = "GO sati";
            dataGridView1.Columns[35].Width = 50;

            DataGridViewColumn column36 = dataGridView1.Columns[36];
            dataGridView1.Columns[36].HeaderText = "GO Iznos";
            dataGridView1.Columns[36].Width = 80;


            DataGridViewColumn column37 = dataGridView1.Columns[37];
            dataGridView1.Columns[37].HeaderText = "Redovni sati";
            dataGridView1.Columns[37].Width = 50;

            DataGridViewColumn column38 = dataGridView1.Columns[38];
            dataGridView1.Columns[38].HeaderText = "Redovni Iznos";
            dataGridView1.Columns[38].Width = 80;

            DataGridViewColumn column39 = dataGridView1.Columns[39];
            dataGridView1.Columns[39].HeaderText = "Bruto cena sata";
            dataGridView1.Columns[39].Width = 80;

            DataGridViewColumn column40 = dataGridView1.Columns[40];
            dataGridView1.Columns[40].HeaderText = "Mesečno sati";
            dataGridView1.Columns[40].Width = 50;

            DataGridViewColumn column41 = dataGridView1.Columns[41];
            dataGridView1.Columns[41].HeaderText = "Prekovremeno";
            dataGridView1.Columns[41].Width = 50;

            DataGridViewColumn column42 = dataGridView1.Columns[42];
            dataGridView1.Columns[42].HeaderText = "Redovno";
            dataGridView1.Columns[42].Width = 50;

            DataGridViewColumn column43 = dataGridView1.Columns[43];
            dataGridView1.Columns[43].HeaderText = "Učinak";
            dataGridView1.Columns[43].Width = 50;

            DataGridViewColumn column44 = dataGridView1.Columns[44];
            dataGridView1.Columns[44].HeaderText = "Redovno Iznos";
            dataGridView1.Columns[44].Width = 80;

            DataGridViewColumn column45 = dataGridView1.Columns[45];
            dataGridView1.Columns[45].HeaderText = "GO sati";
            dataGridView1.Columns[45].Width = 50;


            DataGridViewColumn column46 = dataGridView1.Columns[46];
            dataGridView1.Columns[46].HeaderText = "GO Iznos";
            dataGridView1.Columns[46].Width = 80;

            DataGridViewColumn column47 = dataGridView1.Columns[47];
            dataGridView1.Columns[47].HeaderText = "BOL 100 sati";
            dataGridView1.Columns[47].Width = 50;

            DataGridViewColumn column48 = dataGridView1.Columns[48];
            dataGridView1.Columns[48].HeaderText = "BOL 100 IZ";
            dataGridView1.Columns[48].Width = 80;

            DataGridViewColumn column49 = dataGridView1.Columns[49];
            dataGridView1.Columns[49].HeaderText = "BOL 65 sati";
            dataGridView1.Columns[49].Width = 50;

            DataGridViewColumn column50 = dataGridView1.Columns[50];
            dataGridView1.Columns[50].HeaderText = "BOL 65 IZ";
            dataGridView1.Columns[50].Width = 80;
    


        }

        private void btnIzracunaj_Click(object sender, EventArgs e)
        {
            InsertObracunSati ins = new InsertObracunSati();

            ins.DelObracun();
            ins.InsObracun(Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value));
            ins.UpdLokomotiva(Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value));
            //ins.UpdMilsped(Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value));
            
            //ins.UpdKragujevac(Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value));
           
            //ins.UpdRemont(Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value));
            
            // ins.UpdSmederevo(Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value));
            
            ins.UpdINO(Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value));
            
            ins.UpdPutniNalozi(Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value));
            ins.UpdUkupno1(Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value));

            // ins.UpdUkupnoMinimalac(Convert.ToDouble(txtMinimalac.Value));
            RefreshDataGrid();
            PovuciRadPraznikom(); 
            PovuciPrekovremeni();
            PovuciBolovanje65();
            PovuciBolovanje100();
           
            ins.UpdKragujevacPrekovremeniVarijabilniGodisnjiOdmor(Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value));
            ins.UpdGO(Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value));

            ins.UpdUkupno(Convert.ToDouble(txtKurs.Value), Convert.ToDouble(txtSatiMesec.Value), Convert.ToDouble(txtPoreskoOslobodjenje.Value));
            ins.DodatnaObrada(Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value), Convert.ToDouble(txtSatiMesec.Value));
            
            //ins.UpdRedovnoSati(Convert.ToDouble(txtSatiMesec.Value));
            RefreshDataGrid();
            MessageBox.Show("Gotovo, to ti je završeno");
           
        }
        private void PovuciRadPraznikom()
        {/*
            try
            {
                InsertObracunSati ins = new InsertObracunSati();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
                    SqlConnection con = new SqlConnection(s_connection);

                    con.Open();

                    SqlCommand cmd = new SqlCommand("Select Cast((isnull(Sum(Ukupno),0)) as integer) as UK from PrekovremeniRad where RadPraznikom = 1 and ZaposleniID = " + row.Cells[0].Value +
                        " and Convert(nvarchar(10), DatumOd, 126) > '" + dtpVremeOd2.Text + "' and Convert(nvarchar(10), DatumDo, 126) < '" + dtpVremeDo2.Text + "'", con);
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        ins.UpdMilspedPraznikom(Convert.ToInt32(row.Cells[0].Value), Convert.ToInt32(dr["UK"].ToString()));
                    }
                    con.Close();
                }


            }
            catch
            {
                MessageBox.Show("Nije uspelo povlacenje rada praznikom");
            }
*/
            InsertObracunSati ins = new InsertObracunSati();
            ins.UpdMilspedPraznici(Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value));
        }

        private void PovuciPrekovremeni()
        {
            /*  try
              {

                  InsertObracunSati ins = new InsertObracunSati();
                  foreach (DataGridViewRow row in dataGridView1.Rows)
                  {
                      var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
                      SqlConnection con = new SqlConnection(s_connection);

                      con.Open();

                      SqlCommand cmd = new SqlCommand("Select Cast((isnull(Sum(Ukupno),0)) as integer) as UK from PrekovremeniRad where RadPraznikom = 0 and ZaposleniID = " + row.Cells[0].Value +
                          " and Convert(nvarchar(10), DatumOd, 126) > '" + dtpVremeOd2.Text + "' and Convert(nvarchar(10), DatumDo, 126) < '" + dtpVremeDo2.Text + "'", con);
                      SqlDataReader dr = cmd.ExecuteReader();

                      while (dr.Read())
                      {
                          ins.UpdKragujevacPrekovremeni(Convert.ToInt32(row.Cells[0].Value), Convert.ToInt32(dr["UK"].ToString()));
                      }
                      con.Close();
                  }
           

              }
              catch
              {
                  MessageBox.Show("Nije uspelo prekovremeni");
              }
                  */

            InsertObracunSati ins = new InsertObracunSati();
            ins.UpdKragujevacPrekovremeniVarijabilni(Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value), Convert.ToInt32(txtSatiMesec.Value));
        }

        private void PovuciBolovanje65()
        {
            try
            {
                InsertObracunSati ins = new InsertObracunSati();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
                    SqlConnection con = new SqlConnection(s_connection);

                    con.Open();


                    SqlCommand cmd = new SqlCommand("Select Cast((isnull(Sum(Ukupno),0)) as integer) as UK from Bolovanje where ZaposleniID = " + row.Cells[0].Value +
                        " and Convert(nvarchar(10), DatumOd, 126) >= '" + dtpVremeOd2.Text + "' and Convert(nvarchar(10), DatumDo, 126) <= '" + dtpVremeDo2.Text + "' and TipBolovanja = '65'", con);
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        ins.UpdRemontBolovanje65(Convert.ToInt32(row.Cells[0].Value), Convert.ToInt32(dr["UK"].ToString()));
                    }
                    con.Close();
                }


            }
            catch
            {
                MessageBox.Show("Nije uspelo bolovanje 65");
            }

        }

        private void PovuciBolovanje100()
        {
            try
            {
                InsertObracunSati ins = new InsertObracunSati();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
                    SqlConnection con = new SqlConnection(s_connection);

                    con.Open();


                    SqlCommand cmd = new SqlCommand("Select Cast((isnull(Sum(Ukupno),0) ) as integer) as UK from Bolovanje where ZaposleniID = " + row.Cells[0].Value +
                        " and Convert(nvarchar(10), DatumOd, 126) >= '" + dtpVremeOd2.Text + "' and Convert(nvarchar(10), DatumDo, 126) <= '" + dtpVremeDo2.Text + "' and TipBolovanja = '100'", con);
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        ins.UpdRemontBolovanje100(Convert.ToInt32(row.Cells[0].Value), Convert.ToInt32(dr["UK"].ToString()));
                    }
                    con.Close();
                }


            }
            catch
            {
                MessageBox.Show("Nije uspelo bolovanje 100");
            }

        }
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Selected)
                    {
                        InsertObracunSati ins = new InsertObracunSati();
                        ins.UpdUkupnoUEUR(Convert.ToInt32(row.Cells[0].Value.ToString()), Convert.ToDouble(txtZarada.Value));
                        RefreshDataGrid();
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
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Selected)
                    {
                        InsertObracunSati ins = new InsertObracunSati();
                        ins.UpdUkupnoSmanjenje(Convert.ToInt32(row.Cells[0].Value.ToString()), Convert.ToDouble(txtSmanjenje.Value));
                        RefreshDataGrid();
                    }
                }


            }
            catch
            {
                MessageBox.Show("Nije uspela selekcija stavki");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            InsertObracunSati ins = new InsertObracunSati();
            ins.UpdUkupnoMinimalac(Convert.ToDouble(txtMinimalac.Value));
            RefreshDataGrid();
            MessageBox.Show("Gotovo");
        }

        private void frmIzracunZarada_Load(object sender, EventArgs e)
        {
            RefreshDataGrid();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            InsertObracunSati ins = new InsertObracunSati();

           
            ins.UpdPutniNalozi(Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value));
            ins.UpdPutniNaloziPola(Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value));
           
            RefreshDataGrid();
            MessageBox.Show("Gotovo");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            InsertObracunSati ins = new InsertObracunSati();
            RefreshDataGrid();
        }

        private void button8_Click(object sender, EventArgs e)
        {
           ///PPPP2
            
            PNZaBrisanje();
            PNZaBrisanjePola();
            MessageBox.Show("Gotovo");
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //PPP
            InsertObracunSati ins = new InsertObracunSati();
            ins.SelectPNBrisanje();
            ins.SelectPNBrisanje2();
            ins.SelectPNBrisanje3();
            ins.SelectPNBrisanje4();
            ins.SelectPNBrisanje5();
           // ins.Select5();
            RefreshDataGrid();
            MessageBox.Show("Gotovo");
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            InsertObracunSati ins = new InsertObracunSati();
            ins.UpdUkupnoMinimalac(Convert.ToDouble(txtMinimalac.Value));
            RefreshDataGrid();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            InsertObracunSati ins = new InsertObracunSati();
            ins.UpdKazna();
            RefreshDataGrid();
        }

        private void button1_Click_2(object sender, EventArgs e)
        {
            InsertObracunSati ins = new InsertObracunSati();
            ins.UpdZakljucavanjeSmene(Convert.ToDateTime(dtpZakljucavanjeSmene.Value));
            MessageBox.Show("Zaključano na zadati datum"); 
        }

        private void button9_Click_1(object sender, EventArgs e)
        {
            RefreshDataGrid();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            // SelectObracunMMVPrevoz
            InsertObracunSati ins = new InsertObracunSati();

            ins.SelectObracunMMVPrevoz();

            RefreshDataGrid();
        }

        private void button6_Click_1(object sender, EventArgs e)
        {

        }

        private void button11_Click(object sender, EventArgs e)
        {
            frmIzracunZaradaNovi fno = new frmIzracunZaradaNovi();
            fno.Show();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (txtPassword.Text != "iv4321")
            {
                return;

            }
            var select = "";

            select = " SELECT Zarada.Zaposleni, Rtrim(Delavci.DeIme) + ' ' + Rtrim(Delavci.DePriimek) as Zaposleni,Zarada.Minimalna, Zarada.Osnovna, Zarada.Prevoz, Zarada.Regres, Zarada.TopliObrok, (Osnovna - Regres - TopliObrok + Prevoz) as OsnovnaZarada2 FROM Zarada inner join Delavci on Delavci.DeSifra = Zarada.Zaposleni "+
   " WHERE Zarada.Zaposleni NOT IN(SELECT ObracunZaposleni.ID FROM ObracunZaposleni) and Zarada.Fiksna = 0 and DeSifStat not in ('P') ";

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

        private void button13_Click(object sender, EventArgs e)
        {
         
           
            MessageBox.Show("I ovo ti je zavrseno");
        }
    }
}
