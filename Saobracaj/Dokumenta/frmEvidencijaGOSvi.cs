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

namespace Saobracaj.Dokumenta
{
    public partial class frmEvidencijaGOSvi : Form
    {
        string s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
        public frmEvidencijaGOSvi()
        {
            InitializeComponent();
            FillGV();
            FillCombo();
        }
        private void FillGV()
        {
            var select = "Select DopustStavke.ID as StavkaID, DoLeto as Godina, DoSifDe as SifraRadnik," +
                " (Rtrim(Delavci.DeIme) + ' ' + RTrim(Delavci.DePriimek)) as Radnik, VremeOd, VremeDo, Ukupno, Napomena, Razlog,StatusGodmora,Odobrio as SifraOdobrio," +
                " (Rtrim(o.DeIme) + ' ' + RTrim(o.DePriimek)) as Odobrio " +
                "from DopustStavke " +
                "inner join Dopust on Dopust.DoStZapisa = DopustStavke.IdNadredjena " +
                "inner join Delavci on Delavci.DeSifra = DoSifDe " +
                "inner join Delavci o on o.DeSifra = Odobrio " +
                "order by StavkaID desc";
           
            SqlConnection conn = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            dataGridView1.Columns[0].Width = 50;
            dataGridView1.Columns[3].Width = 150;
            dataGridView1.Columns[6].Width = 50;
            dataGridView1.Columns[11].Width = 150;
            dataGridView1.Columns[2].Visible = false;
            dataGridView1.Columns[10].Visible = false;
        }
        private void FillCombo()
        {
            var query = "select DeSifra, (RTrim(delavci.DeIme)+' '+RTrim(delavci.DePriimek)) as Zaposleni From Delavci order by Zaposleni asc";
            SqlConnection conn = new SqlConnection(s_connection);
            SqlDataAdapter da = new SqlDataAdapter(query, conn);
            DataSet ds = new DataSet();
            da.Fill(ds);
            cboZaposleni.DataSource = ds.Tables[0];
            cboZaposleni.DisplayMember = "Zaposleni";
            cboZaposleni.ValueMember = "DeSifra";
        }

        private void btnZaposleni_Click(object sender, EventArgs e)
        {
            var select = "Select DopustStavke.ID as StavkaID, DoLeto as Godina, DoSifDe as SifraRadnik," +
                " (Rtrim(Delavci.DeIme) + ' ' + RTrim(Delavci.DePriimek)) as Radnik, VremeOd, VremeDo, Ukupno, Napomena, Razlog,StatusGodmora,Odobrio as SifraOdobrio," +
                " (Rtrim(o.DeIme) + ' ' + RTrim(o.DePriimek)) as Odobrio " +
                "from DopustStavke " +
                "inner join Dopust on Dopust.DoStZapisa = DopustStavke.IdNadredjena " +
                "inner join Delavci on Delavci.DeSifra = DoSifDe " +
                "inner join Delavci o on o.DeSifra = Odobrio " +
                "where DoSifDe =" + Convert.ToInt32(cboZaposleni.SelectedValue)+
                "order by StavkaID desc";

            SqlConnection conn = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            dataGridView1.Columns[0].Width = 50;
            dataGridView1.Columns[3].Width = 150;
            dataGridView1.Columns[6].Width = 50;
            dataGridView1.Columns[11].Width = 150;
            dataGridView1.Columns[2].Visible = false;
            dataGridView1.Columns[10].Visible = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DateTime dtOd = dt_VremeOd.Value.Date;
            DateTime dtDo = dt_VremeDo.Value.Date;
            var select = "Select DopustStavke.ID as StavkaID, DoLeto as Godina, DoSifDe as SifraRadnik," +
                            " (Rtrim(Delavci.DeIme) + ' ' + RTrim(Delavci.DePriimek)) as Radnik, VremeOd, VremeDo, Ukupno, Napomena, Razlog,StatusGodmora,Odobrio as SifraOdobrio," +
                            " (Rtrim(o.DeIme) + ' ' + RTrim(o.DePriimek)) as Odobrio " +
                            "from DopustStavke " +
                            "inner join Dopust on Dopust.DoStZapisa = DopustStavke.IdNadredjena " +
                            "inner join Delavci on Delavci.DeSifra = DoSifDe " +
                            "inner join Delavci o on o.DeSifra = Odobrio " +
                             "Where(VremeOd Between Convert(Date,'"+dtOd.ToString("MM.dd.yyyy")+"') and Convert(Date,'"+ dtDo.ToString("MM.dd.yyyy") + "'))" +
                            "order by VremeOd";

            SqlConnection conn = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            dataGridView1.Columns[0].Width = 50;
            dataGridView1.Columns[3].Width = 150;
            dataGridView1.Columns[6].Width = 50;
            dataGridView1.Columns[11].Width = 150;
            dataGridView1.Columns[2].Visible = false;
            dataGridView1.Columns[10].Visible = false;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            FillGV();
        }
    }
}
