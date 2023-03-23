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
    public partial class frmObracunFiksni : Form
    {
        public frmObracunFiksni()
        {
            InitializeComponent();
            IdGrupe();
            IdForme();
            PravoPristupa();
        }
        public static string code = "frmObracunFiksni";
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
        private void btnPostaviPrviDeo_Click(object sender, EventArgs e)
        {

 
           



          

        }

        private void RefreshDataGrid()
        {
            var select = " SELECT [ID]       ,PrezimeIme, [MesecniFondSati] " +
     " ,[GoSati]      ,[Bol65Sati] " +
     "  ,[Bol100Sati]      ,[FondPraznik] " +
     "  ,[PraznikSati]      ,[PrekovremenoSati] " +
     "  ,[NaknadaPraznik]      ,[RedovanRadSati] " +
     "  ,[RadPoUcinku]      ,[CenaRada] " +
     "  ,ObracunZaposleniFiksni.[Osnovna]      ,ObracunZaposleniFiksni.[ProsecnaCena] " +
     "  ,[GOIznos]      ,[Bol65Iznos] " +
     "  ,[Bol100Iznos]      ,[Praznik110Iznos] " +
     "  ,[Praznik100Iznos],( PrekovremenoSati * 0.26 * CenaRada) as PrekovremeniIznos     ,[RedovanRadIznos] " +
     "  ,[RadPoUcinkuIznos]      ,[GodinaStaza] " +
     "  ,[MinuliRadIznos], Prevoz  FROM [ObracunZaposleniFiksni]" +
     "inner join Zarada on Zarada.Zaposleni =  ObracunZaposleniFiksni.ID";
            
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = false;
            dataGridView1.DataSource = ds.Tables[0];



        }

        private void RefreshDataGridSamoFiksni()
        {
            var select = " SELECT [ID]       ,PrezimeIme, [MesecniFondSati] " +
     " ,[GoSati]      ,[Bol65Sati] " +
     "  ,[Bol100Sati]      ,[FondPraznik] " +
     "  ,[PraznikSati]      ,[PrekovremenoSati] " +
     "  ,[NaknadaPraznik]      ,[RedovanRadSati] " +
     "  ,[RadPoUcinku]      ,[CenaRada] " +
     "  ,ObracunZaposleniFiksni.[Osnovna]      ,ObracunZaposleniFiksni.[ProsecnaCena] " +
     "  ,[GOIznos]      ,[Bol65Iznos] " +
     "  ,[Bol100Iznos]      ,[Praznik110Iznos] " +
     "  ,[Praznik100Iznos],( PrekovremenoSati * 0.26 * CenaRada) as PrekovremeniIznos     ,[RedovanRadIznos] " +
     "  ,[RadPoUcinkuIznos]      ,[GodinaStaza] " +
     "  ,[MinuliRadIznos], Prevoz  FROM [ObracunZaposleniFiksni]" +
     "inner join Zarada on Zarada.Zaposleni =  ObracunZaposleniFiksni.ID Where Zarada.Fiksna = 1";

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = false;
            dataGridView1.DataSource = ds.Tables[0];



        }

        private void RefreshDataGridSamoVarijabilni()
        {
            var select = " SELECT [ID]       ,PrezimeIme, [MesecniFondSati] " +
     " ,[GoSati]      ,[Bol65Sati] " +
     "  ,[Bol100Sati]      ,[FondPraznik] " +
     "  ,[PraznikSati]      ,[PrekovremenoSati] " +
     "  ,[NaknadaPraznik]      ,[RedovanRadSati] " +
     "  ,[RadPoUcinku]      ,[CenaRada] " +
     "  ,ObracunZaposleniFiksni.[Osnovna]      ,ObracunZaposleniFiksni.[ProsecnaCena] " +
     "  ,[GOIznos]      ,[Bol65Iznos] " +
     "  ,[Bol100Iznos]      ,[Praznik110Iznos] " +
     "  ,[Praznik100Iznos],( PrekovremenoSati * 0.26 * CenaRada) as PrekovremeniIznos     ,[RedovanRadIznos] " +
     "  ,[RadPoUcinkuIznos]      ,[GodinaStaza] " +
     "  ,[MinuliRadIznos], Prevoz  FROM [ObracunZaposleniFiksni]" +
     "inner join Zarada on Zarada.Zaposleni =  ObracunZaposleniFiksni.ID Where Zarada.Fiksna = 0";

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = false;
            dataGridView1.DataSource = ds.Tables[0];



        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
          
        }
        private void PovuciRadPraznikom()
        {
            try
            {
                InsertObracunSatiFiksni ins = new InsertObracunSatiFiksni();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
                    SqlConnection con = new SqlConnection(s_connection);

                    con.Open();

                    SqlCommand cmd = new SqlCommand("Select (isnull(Cast(Sum(Ukupno) as Int),0)) as UK from PrekovremeniRad where RadPraznikom = 1 and ZaposleniID = " + row.Cells[0].Value +
                        " and DatumOd >= '" + dtpVremeOd2.Value.ToString("yyyy-MM-dd 00:00") + "' and  DatumDo <= '" + dtpVremeDo2.Value.ToString("yyyy-MM-dd 23:59") + "'", con);

                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        ins.UpdPraznikSatiFiksni(Convert.ToInt32(row.Cells[0].Value), Convert.ToInt32(dr["UK"].ToString()));

                       // row.Cells[7].Value = dr["UK"].ToString();
                    }
                    con.Close();
                }
                UpucajFondSati(Convert.ToInt32(txtPrazniciFond.Value));

            }
            catch
            {
                MessageBox.Show("Nije uspelo povlacenje rada praznikom");
            }

    }
        private void UpucajFondSati(int FondSati)
        {

            try
            {
                InsertObracunSatiFiksni ins = new InsertObracunSatiFiksni();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
                    SqlConnection con = new SqlConnection(s_connection);

                    con.Open();
                    ins.UpdUkupanFondSatiPraznik(FondSati, Convert.ToInt32(txtSatiMesec.Value));
                   
                    con.Close();
                }


            }
            catch
            {
                MessageBox.Show("Nije uspelo povlacenje rada praznikom");
            }


        }
        private void PovuciSateGodisnjiOdmor()
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
                    SqlConnection con = new SqlConnection(s_connection);

                    con.Open();

                    SqlCommand cmd = new SqlCommand("Select (isnull(Sum(Ukupno),0) * 8) as UK from DopustStavke " +
                    " inner join Dopust on Dopust.DoStZapisa = DopustStavke.IdNadredjena  where  Convert(nvarchar(10), VremeOd, 126) > '" + dtpVremeOd.Text + "' and Convert(nvarchar(10), VremeDo, 126) < '"+ dtpVremeDo.Text + "'  And Dopust.DoSifDe = " + row.Cells[0].Value, con);
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        row.Cells[8].Value = dr["UK"].ToString();
                    }
                    con.Close();
                }


            }
            catch
            {
                MessageBox.Show("Nije uspelo odmora");
            }

        }

        private void PovuciBolovanje65()
        {
            try
            {
                InsertObracunSatiFiksni ins = new InsertObracunSatiFiksni();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
                    SqlConnection con = new SqlConnection(s_connection);

                    con.Open();


                    SqlCommand cmd = new SqlCommand("Select Cast((isnull(Sum(Ukupno) ,0) ) as integer) as UK from Bolovanje where ZaposleniID = " + row.Cells[0].Value +
                        " and DatumOd >= '" + dtpVremeOd2.Value.ToString("yyyy-MM-dd 00:00") + "' and  DatumDo <= '" + dtpVremeDo2.Value.ToString("yyyy-MM-dd 23:59") + "' and RTRIM(TipBolovanja) = '65'", con);
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        ins.UpdBolovanje65Fiksni(Convert.ToInt32(row.Cells[0].Value), Convert.ToInt32(dr["UK"].ToString()));
                    }
                    con.Close();
                }


            }
            catch
            {
                MessageBox.Show("Nije uspelo povlacenje rada praznikom");
            }

        }

        private void PovuciBolovanje100()
        {
            try
            {
                InsertObracunSatiFiksni ins = new InsertObracunSatiFiksni();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
                    SqlConnection con = new SqlConnection(s_connection);

                    con.Open();


                    SqlCommand cmd = new SqlCommand("Select Cast((isnull(Sum(Ukupno),0)) as integer) as UK from Bolovanje where ZaposleniID = " + row.Cells[0].Value +
                          " and DatumOd >= '" + dtpVremeOd2.Value.ToString("yyyy-MM-dd 00:00") + "' and  DatumDo <= '" + dtpVremeDo2.Value.ToString("yyyy-MM-dd 23:59") + "' and RTRIM(TipBolovanja) = '100'", con);
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        ins.UpdBolovanje100Fiksni(Convert.ToInt32(row.Cells[0].Value), Convert.ToInt32(dr["UK"].ToString()));
                    }
                    con.Close();
                }


            }
            catch
            {
                MessageBox.Show("Nije uspelo povlacenje rada praznikom");
            }

        }

     
        private void metroButton2_Click(object sender, EventArgs e)
        {
    
        PovuciRadPraznikom();
        PovuciSateGodisnjiOdmor();
        PovuciBolovanje65();
        PovuciBolovanje100();
      
        }

        private void PovuciPrekovremeni()
        {
            try
            {
              
                InsertObracunSatiFiksni ins = new InsertObracunSatiFiksni();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
                    SqlConnection con = new SqlConnection(s_connection);

                    con.Open();

                    SqlCommand cmd = new SqlCommand("Select Cast((isnull(Sum(Ukupno),0)) as Int) as UK from PrekovremeniRad where ZaposleniID = " + row.Cells[0].Value +
                        " and DatumOd >= '" + dtpVremeOd2.Value.ToString("yyyy-MM-dd 00:00") + "' and  DatumDo <= '" + dtpVremeDo2.Value.ToString("yyyy-MM-dd 23:59") + "' and RadPraznikom = 0", con);


                    
                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        ins.UpdPrekovremeniFiksni(Convert.ToInt32(row.Cells[0].Value), Convert.ToInt32(dr["UK"].ToString()));
                    }
                    con.Close();
                }


            }
            catch
            {
                MessageBox.Show("Nije uspelo povlacenje rada praznikom");
            }


        }

        private void btnIzracunaj_Click(object sender, EventArgs e)
        {
            if (txtPassword.Text != "iv4321")
            {
                return;

            }
            InsertObracunSatiFiksni ins = new InsertObracunSatiFiksni();
            ins.InsObracunFiksni();
            ins.UpdGO(Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value));
            
            PovuciBolovanje65();
            PovuciRadPraznikom();
            RefreshDataGrid();
            PovuciBolovanje100();
            PovuciPrekovremeni();
            ins.UpdObracunFiksniSve(Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value), Convert.ToDouble(txtKurs.Value), Convert.ToDouble(txtSatiMesec.Value), Convert.ToDouble(txtMinimalac.Value));
            RefreshDataGrid();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            RefreshDataGrid();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (txtPassword.Text != "iv4321")
            {
                return;

            }

            RefreshDataGridSamoFiksni();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (txtPassword.Text != "iv4321")
            {
                return;

            }

            RefreshDataGridSamoVarijabilni();
        }
    }
}
