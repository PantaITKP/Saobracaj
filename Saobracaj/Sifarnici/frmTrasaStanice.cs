﻿using System;
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

namespace Saobracaj.Sifarnici
{
    public partial class frmTrasaStanice : Form
    {
        bool status = true;
        int PrviUlazak = 0;
        public frmTrasaStanice()
        {
            InitializeComponent();
        }

        public frmTrasaStanice(string SifraTrase, string OznakaTrase)
        {
            InitializeComponent();
            txtSifra.Text = SifraTrase;
            txtVoz.Text = OznakaTrase;
            //
            RefreshDataGrid();

        }
        //txtSifra.Text, txtVoz.Text

        private void btnUbaci_Click(object sender, EventArgs e)
        {
            if (status == true)
            {
                InsertTrasaStanice ins = new InsertTrasaStanice();
                ins.InsTStan(Convert.ToInt32((cboPruga.SelectedValue)), Convert.ToInt32(txtSifra.Text), Convert.ToInt32(cmbPocetna.SelectedValue), Convert.ToInt32(cboKrajnja.SelectedValue));
                RefreshDataGrid();
                //status = false;
            }
            else
            {
                /*
                Insertnhm upd = new Insertnhm();
                upd.UpdStanice(Convert.ToInt32(txtSifra.Text), txtBroj.Text, txtNaziv.Text);
                status = false;
                txtSifra.Enabled = false;
                RefreshDataGrid();
                 * */
            }
        }

        private void RefreshDataGrid()
        {

            //SELECT     TrasaStanice.IDTrase, TrasaStanice.RB, stanice.Opis, stanice.Kod FROM TrasaStanice INNER JOIN  PrugaStavke ON TrasaStanice.IDStanice = PrugaStavke.ID INNER JOIN stanice ON PrugaStavke.StanicaOd = stanice.ID
            var select = " SELECT     TrasaStanice.RB, TrasaStanice.IDTrase, stanice.Opis, stanice.Kod FROM TrasaStanice INNER JOIN stanice ON TrasaStanice.IDStanice = stanice.ID where TrasaStanice.IdTrase = " + txtSifra.Text;
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
            dataGridView1.Columns[0].HeaderText = "ID trase";
            dataGridView1.Columns[0].Width = 50;

            DataGridViewColumn column2 = dataGridView1.Columns[1];
            dataGridView1.Columns[1].HeaderText = "RB";
            dataGridView1.Columns[1].Width = 50;

            DataGridViewColumn column3 = dataGridView1.Columns[2];
            dataGridView1.Columns[2].HeaderText = "Stanica";
            dataGridView1.Columns[2].Width = 250;

            DataGridViewColumn column4 = dataGridView1.Columns[3];
            dataGridView1.Columns[3].HeaderText = "Broj stanice";
            dataGridView1.Columns[3].Width = 100;


        }

        private void frmTrasaStanice_Load(object sender, EventArgs e)
        {
            var select = " Select Distinct ID, Oznaka, (RTrim(Oznaka) + '-' + Rtrim(Opis)) as Opis From Pruga";
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            cboPruga.DataSource = ds.Tables[0];
            cboPruga.DisplayMember = "Opis";
            cboPruga.ValueMember = "ID";


            var select3 = " Select Stanice.ID, Rtrim(Opis) as Opis from  Stanice Order by Opis  ";
            var s_connection3= ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection3 = new SqlConnection(s_connection3);
            var c3 = new SqlConnection(s_connection3);
            var dataAdapter3= new SqlDataAdapter(select3, c3);

            var commandBuilder3 = new SqlCommandBuilder(dataAdapter3);
            var ds3 = new DataSet();
            dataAdapter3.Fill(ds3);
            cmbPocetna.DataSource = ds3.Tables[0];
            cmbPocetna.DisplayMember = "Opis";
            cmbPocetna.ValueMember = "ID";


            /////////////Stanica do

            var select2 = " Select Stanice.ID, RTrim(Stanice.OPis) as Opis from Stanice Order by Opis " ;
            var s_connection2 = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection2 = new SqlConnection(s_connection2);
            var c2 = new SqlConnection(s_connection2);
            var dataAdapter2 = new SqlDataAdapter(select2, c2);

            var commandBuilder2 = new SqlCommandBuilder(dataAdapter2);
            var ds2 = new DataSet();
            dataAdapter2.Fill(ds2);
            cboKrajnja.DataSource = ds2.Tables[0];
            cboKrajnja.DisplayMember = "Opis";
            cboKrajnja.ValueMember = "ID";

            // cmbPocetna
            //     cmbKrajnja
        }

        private void cboPruga_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PrviUlazak != 0)
            {
           
            }

        }

        private void cboPruga_Leave(object sender, EventArgs e)
        {
            var select = " Select Stanice.ID, RTrim(Stanice.OPis) as OPis from PRugaStavke inner join Stanice on PrugaStavke.StanicaOd = Stanice.Id where PrugaStavke.IdPruge =  " + cboPruga.SelectedValue;
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            cmbPocetna.DataSource = ds.Tables[0];
            cmbPocetna.DisplayMember = "Opis";
            cmbPocetna.ValueMember = "ID";


            /////////////Stanica do

            var select2 = " Select Stanice.ID, RTrim(Stanice.OPis) as Opis from PRugaStavke inner join Stanice on PrugaStavke.StanicaDo= Stanice.Id where PrugaStavke.IdPruge =  " + cboPruga.SelectedValue;
            var s_connection2 = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection2 = new SqlConnection(s_connection2);
            var c2 = new SqlConnection(s_connection2);
            var dataAdapter2 = new SqlDataAdapter(select2, c2);

            var commandBuilder2 = new SqlCommandBuilder(dataAdapter2);
            var ds2 = new DataSet();
            dataAdapter2.Fill(ds2);
            cboKrajnja.DataSource = ds2.Tables[0];
            cboKrajnja.DisplayMember = "Opis";
            cboKrajnja.ValueMember = "ID";

        }

        private void tsSave_Click(object sender, EventArgs e)
        {

        }

        private void tsDelete_Click(object sender, EventArgs e)
        {
            //Napraviti brisanje Trasa Stanice
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {

                    if (row.Selected)
                    {
                        InsertTrasaStanice ins = new InsertTrasaStanice();
                        ins.DelTStan( Convert.ToInt32(txtSifra.Text), Convert.ToInt32(row.Cells[0].Value.ToString()));
                       
                    }
                }
                RefreshDataGrid();

            }
            catch
            {
                MessageBox.Show("Nije uspelo brisanje stavki");
            }

          
        }
    }
}
