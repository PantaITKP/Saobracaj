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
    public partial class frmEvidencijaRadaZakljucavanje : Form
    {
        string TekuciKorisnik = "";
        public frmEvidencijaRadaZakljucavanje()
        {
            InitializeComponent();
        }

        public frmEvidencijaRadaZakljucavanje(string Korisnik)
        {
            InitializeComponent();
            TekuciKorisnik = Korisnik;
           
        }



        private void button2_Click(object sender, EventArgs e)
        {
            var select = "";

                select = "Select Aktivnosti.ID as Zapis,  Aktivnosti.DatumInserta, Aktivnosti.Oznaka, " +
 " (RTrim(DeIme) + ' ' + RTRim(DePriimek)) as Zaposleni,    VremeOD, VremeDo, Ukupno, " +
 " UkupniTroskovi, Aktivnosti.Opis, RN,       RAcun, Kartica, " +
 " Outside, Aktivnosti.KontrolisaoDispecer, Aktivnosti.KontrolisaoAdmin" +
 "    from Aktivnosti   inner join Delavci on Delavci.DeSifra = Aktivnosti.Zaposleni  " +
  "   inner join Kraji on Kraji.KrSifra = Aktivnosti.MestoUpucivanja  " +
                              "  where DatumInserta > (select data from Config where RTrim(code) = 'DatumUnosaSmena')   order by Aktivnosti.ID desc";

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
            dataGridView1.Columns[1].HeaderText = "Datum inserta";
            dataGridView1.Columns[1].Width = 130;

          
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection con = new SqlConnection(s_connection);
          
            SqlCommand cmd = new SqlCommand("Update Config set Data ='" + dateTimePicker1.Text.ToString() + "' Where Rtrim(Code) = 'DatumUnosaSmena'", con);
            con.Open();
            var q = cmd.ExecuteNonQuery();
            con.Close();
        }
    }
}
