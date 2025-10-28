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
    public partial class frmPutniList : Form
    {
        public frmPutniList()
        {
            InitializeComponent();
        }

        private void frmPutniList_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            var select = "";

            select = " select PutniList.Id,IDVoznje,PutniList.Datum, BrojRN, Lokomotiva,  Korisnik  , " + 
                        " stanice.Opis as Stanica, PutniList.Tip as Tip, Razlozi.Opis, PutniList.Napomena, " +
                        " Trase.Voz ,Trase.Relacija, Nalog, KM " +
                        " from PutniList " +
                        " inner join stanice on PutniList.Stanica = stanice.ID " +
                        " inner join Trase on Trase.ID = IDTrase " +
                        " inner join KOrisnici on Korisnici.Korisnik = Zaposleni " +
                        " left join Razlozi on Razlozi.ID = UzrokKasnjenja " +
                        " order by PutniList.ID desc ";

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

           
        }
    }
}
