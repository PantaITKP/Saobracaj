using System;
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
    public partial class frmFormePrava : Form
    {
        public string connect = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
        public frmFormePrava()
        {
            InitializeComponent();
            var query = "SELECT g.IdGrupe,g.Naziv,f.IdForme,f.Naziv,f.Code,a.Upis,a.Izmena,a.Brisanje " +
                "FROM GrupeKorisnik g,Forme f,GrupeForme a " +
                "WHERE g.IdGrupe=a.IdGrupe and f.IdForme=a.IdForme order by g.IdGrupe";
            SqlConnection conn = new SqlConnection(connect);
            SqlDataAdapter da = new SqlDataAdapter(query, conn);
            DataSet ds = new DataSet();
            da.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];
            dataGridView1.Columns[0].HeaderText = "ID Grupe";
            dataGridView1.Columns[0].Width = 50;
            dataGridView1.Columns[1].HeaderText = "Naziv Grupe";
            dataGridView1.Columns[1].Width = 170;
            dataGridView1.Columns[2].HeaderText = "ID Forme";
            dataGridView1.Columns[2].Width = 50;
            dataGridView1.Columns[3].HeaderText = "Naziv Forme";
            dataGridView1.Columns[3].Width = 130;
            dataGridView1.Columns[4].HeaderText = "Code Forme";
            dataGridView1.Columns[4].Width = 130;
            dataGridView1.Columns[5].Width = 50;
            dataGridView1.Columns[6].Width = 50;
            dataGridView1.Columns[7].Width = 50;

        }
    }
}
