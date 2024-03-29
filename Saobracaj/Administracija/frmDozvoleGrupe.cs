﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Saobracaj.Administracija
{
    public partial class frmDozvoleGrupe : Form
    {
        public string connect = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
        public frmDozvoleGrupe()
        {
            InitializeComponent();

            var query = "select f.IdGrupe,g.Naziv,IdForme,f.Upis,f.Izmena,f.Brisanje " +
                    "from GrupeForme f, GrupeKorisnik g " +
                    "Where f.IdGrupe=g.IdGrupe order by f.IdGrupe";
            SqlConnection conn = new SqlConnection(connect);
            SqlDataAdapter da = new SqlDataAdapter(query, conn);
            DataSet ds = new DataSet();
            da.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];
            dataGridView1.Columns[0].Width = 70;
            dataGridView1.Columns[1].HeaderText = "Naziv Grupe";
            dataGridView1.Columns[1].Width = 200;
            dataGridView1.Columns[2].Width = 70;
            dataGridView1.Columns[3].Width = 60;
            dataGridView1.Columns[4].Width = 60;
            dataGridView1.Columns[5].Width = 60;
            

        }

        private void frmDozvoleGrupe_Load(object sender, EventArgs e)
        {

        }
    }
}
