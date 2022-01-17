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

namespace Saobracaj.Administracija
{
    public partial class frmKorisnici : Form
    {
        bool status = false;
        public frmKorisnici()
        {
            InitializeComponent();
        }

        private void RefreshDataGrid()
        {
            var select = " Select Korisnik, DeSifra from Korisnici";
            // var select = "SELECT RkShippingItemPak.ShippingItemPakId as ID, RkShipping.ShippingNo as BarkodUtovara, RkShipping.BrojIstovara as BrojUtovara, RkShipping.DatumIstovara as DatumUtovara, RkShipping.BrojUtovara as BrojIstovara,  RkShipping.DatumUtovara as DatumIstovara , Saloni.MestoIsporuke, RkShippingItemPak.PaketName, RkShippingItemPak.LargoPakId, RkShippingItemPak.LargoNaziv, RkShippingItemPak.Paleta, RkShippingItemPak.Tezina,  RkShippingItemPak.LargoDimenzija  FROM [dbo].RkShippingItemPak inner join RkShipping on [dbo].RkShippingItemPak.ShipingIDz = RkShipping.[ShippingID] inner join SysKomitenti on RkShipping.KupacIDz = SysKomitenti.KomintentID inner join Saloni on RkShipping.SalonIDz = Saloni.SifraKomintentaMestoIsporuke where RkShipping.Vozilo  = '" + cboVozila.Text + "' and RkShipping.DatumUtovara = '" + cboDatumUtovara.Text + "' and RkShipping.DatumUtovara = '" + cboDatumUtovara.Text + "'";
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];
            //string value = dataGridView3.Rows[0].Cells[0].Value.ToString();

            DataGridViewColumn column = dataGridView1.Columns[0];
            dataGridView1.Columns[0].HeaderText = "Korisnik";
            dataGridView1.Columns[0].Width = 150;

            DataGridViewColumn column2 = dataGridView1.Columns[1];
            dataGridView1.Columns[1].HeaderText = "Largo";
            dataGridView1.Columns[1].Width = 100;
        
        }

        private void frmKorisnici_Load(object sender, EventArgs e)
        {
            var select3 = "  Select DeSifra, (Rtrim(DePriimek) + ' ' +Rtrim(DeIme)) as Naziv from Delavci where DeSifStat = 'A' order by DePriimek"  ;
            var s_connection3 = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection3 = new SqlConnection(s_connection3);
            var c3 = new SqlConnection(s_connection3);
            var dataAdapter3 = new SqlDataAdapter(select3, c3);

            var commandBuilder3 = new SqlCommandBuilder(dataAdapter3);
            var ds3 = new DataSet();
            dataAdapter3.Fill(ds3);
            cboLargoKorisnik.DataSource = ds3.Tables[0];
            cboLargoKorisnik.DisplayMember = "Naziv";
            cboLargoKorisnik.ValueMember = "DeSifra";
        
            
            RefreshDataGrid();
        }

        private void tsNew_Click(object sender, EventArgs e)
        {
            txtSifra.Text = "";
            txtOpis.Text = "";
            status = true;
        }

        private void tsSave_Click(object sender, EventArgs e)
        {
            if (status == true)
            {
                InsertKorisnici ins = new InsertKorisnici();
                ins.InsKorisnici(txtSifra.Text,txtOpis.Text,0, Convert.ToInt32(cboLargoKorisnik.SelectedValue));
                RefreshDataGrid();
                status = false;
            }
          
        }

        private void tsDelete_Click(object sender, EventArgs e)
        {
            InsertKorisnici ins = new InsertKorisnici();
            ins.DeleteKorisnici(txtSifra.Text);
            RefreshDataGrid();
            status = false;
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
                        txtOpis.Text = row.Cells[1].Value.ToString();
                        cboLargoKorisnik.SelectedValue = row.Cells[2].Value.ToString();
                    }
                }


            }
            catch
            {
                MessageBox.Show("Nije uspela selekcija stavki");
            }
        }


    }
}
