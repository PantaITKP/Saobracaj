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
    public partial class frmPreostaliOdmor : Form
    {
        string s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
        public frmPreostaliOdmor()
        {
            InitializeComponent();
        }
        private void frmPreostaliOdmor_Load(object sender, EventArgs e)
        {
            FillGV();
            FillCombo();
        }
        private void FillGV()
        {
            var select = "select Min(DoSkupaj) as Ukupno,Sum(Ukupno) as Iskoristio,Min(DoSkupaj)-Sum(Ukupno) as Preostalo, A.Godina, A.Radnik " +
                "from( " +
                "Select DopustStavke.ID as StavkaID, Doskupaj, DoLeto as Godina, DoSifDe as SifraRadnik, (Rtrim(i.DeIme) + ' ' + RTrim(i.DePriimek)) as Radnik, " +
                "VremeOd, VremeDo, Ukupno, Napomena, Razlog, StatusGodmora, Odobrio as SifraOdobrio, " +
                "(Rtrim(o.DeIme) + ' ' + RTrim(o.DePriimek)) as Odobrio, DatumZahteva, DatumPovratka, DmSifra, DmNaziv as RadnoMest " +
                "from DopustStavke " +
                "inner join Dopust on Dopust.DoStZapisa = DopustStavke.IdNadredjena " +
                "inner join Delavci i on i.DeSifra = DoSifDe " +
                "inner join Delavci o on o.DeSifra = Odobrio " +
                "inner join DelovnaMesta on i.DeSifDelMes = DelovnaMesta.DmSifra) as A " +
                "group by A.Godina  , A.Radnik";

            SqlConnection conn = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            dataGridView1.Columns[0].Width = 60;
            dataGridView1.Columns[1].Width = 60;
            dataGridView1.Columns[2].Width = 60;
            dataGridView1.Columns[3].Width = 60;
            dataGridView1.Columns[4].Width = 170;
        }
        private void FillCombo()
        {
            var query = "Select distinct DoLeto From Dopust order by DoLeto desc";
            SqlConnection conn = new SqlConnection(s_connection);
            SqlDataAdapter da = new SqlDataAdapter(query, conn);
            DataSet ds = new DataSet();
            da.Fill(ds);
            cbo_Godina.DataSource = ds.Tables[0];
            cbo_Godina.DisplayMember = "DoLeto";
            cbo_Godina.ValueMember = "DoLeto";

            var query2 = "Select DeSifra,(Rtrim(DeIme) + ' ' + RTrim(DePriimek)) as Radnik From Delavci order by Radnik asc";
            SqlDataAdapter da2 = new SqlDataAdapter(query2, conn);
            DataSet ds2 = new DataSet();
            da2.Fill(ds2);
            cbo_Zaposleni.DataSource = ds2.Tables[0];
            cbo_Zaposleni.DisplayMember = "Radnik";
            cbo_Zaposleni.ValueMember = "DeSifra";

            var query3 = "Select DmSifra,DmNaziv from DelovnaMesta order by DmSifra";
            SqlDataAdapter da3 = new SqlDataAdapter(query3, conn);
            DataSet ds3 = new DataSet();
            da3.Fill(ds3);
            cbo_RM.DataSource = ds3.Tables[0];
            cbo_RM.DisplayMember = "DmNaziv";
            cbo_RM.ValueMember = "DmSifra";

            var query4 = "Select distinct Delavci.DeOdobrava,(Rtrim(i.DeIme) + ' ' + RTrim(i.DePriimek)) as Radnik " +
                "From Delavci inner join Delavci as i on Delavci.DeOdobrava=i.DeSifra";
            SqlDataAdapter da4 = new SqlDataAdapter(query4, conn);
            DataSet ds4 = new DataSet();
            da4.Fill(ds4);
            cbo_Nadredjeni.DataSource = ds4.Tables[0];
            cbo_Nadredjeni.DisplayMember = "Radnik";
            cbo_Nadredjeni.ValueMember = "Delavci.DeOdobrava";
        }

        private void btn_rm_Click(object sender, EventArgs e)
        {
            var select = "select Min(DoSkupaj) as Ukupno,Sum(Ukupno) as Iskoristio,Min(DoSkupaj)-Sum(Ukupno) as Preostalo, A.Godina, A.Radnik " +
    "from( " +
    "Select DopustStavke.ID as StavkaID, Doskupaj, DoLeto as Godina, DoSifDe as SifraRadnik, (Rtrim(i.DeIme) + ' ' + RTrim(i.DePriimek)) as Radnik, " +
    "VremeOd, VremeDo, Ukupno, Napomena, Razlog, StatusGodmora, Odobrio as SifraOdobrio, " +
    "(Rtrim(o.DeIme) + ' ' + RTrim(o.DePriimek)) as Odobrio, DatumZahteva, DatumPovratka, DmSifra, DmNaziv as RadnoMest " +
    "from DopustStavke " +
    "inner join Dopust on Dopust.DoStZapisa = DopustStavke.IdNadredjena " +
    "inner join Delavci i on i.DeSifra = DoSifDe " +
    "inner join Delavci o on o.DeSifra = Odobrio " +
    "inner join DelovnaMesta on i.DeSifDelMes = DelovnaMesta.DmSifra Where DmSifra="+Convert.ToInt32(cbo_RM.SelectedValue.ToString())+") as A " +
    "group by A.Godina  , A.Radnik";

            SqlConnection conn = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            dataGridView1.Columns[0].Width = 60;
            dataGridView1.Columns[1].Width = 60;
            dataGridView1.Columns[2].Width = 60;
            dataGridView1.Columns[3].Width = 60;
            dataGridView1.Columns[4].Width = 170;
        }

        private void btn_godina_Click(object sender, EventArgs e)
        {
            var select = "select Min(DoSkupaj) as Ukupno,Sum(Ukupno) as Iskoristio,Min(DoSkupaj)-Sum(Ukupno) as Preostalo, A.Godina, A.Radnik " +
"from( " +
"Select DopustStavke.ID as StavkaID, Doskupaj, DoLeto as Godina, DoSifDe as SifraRadnik, (Rtrim(i.DeIme) + ' ' + RTrim(i.DePriimek)) as Radnik, " +
"VremeOd, VremeDo, Ukupno, Napomena, Razlog, StatusGodmora, Odobrio as SifraOdobrio, " +
"(Rtrim(o.DeIme) + ' ' + RTrim(o.DePriimek)) as Odobrio, DatumZahteva, DatumPovratka, DmSifra, DmNaziv as RadnoMest " +
"from DopustStavke " +
"inner join Dopust on Dopust.DoStZapisa = DopustStavke.IdNadredjena " +
"inner join Delavci i on i.DeSifra = DoSifDe " +
"inner join Delavci o on o.DeSifra = Odobrio " +
"inner join DelovnaMesta on i.DeSifDelMes = DelovnaMesta.DmSifra Where DoLeto=" + Convert.ToInt32(cbo_Godina.SelectedValue.ToString()) + ") as A " +
"group by A.Godina  , A.Radnik";

            SqlConnection conn = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            dataGridView1.Columns[0].Width = 60;
            dataGridView1.Columns[1].Width = 60;
            dataGridView1.Columns[2].Width = 60;
            dataGridView1.Columns[3].Width = 60;
            dataGridView1.Columns[4].Width = 170;
        }

        private void btn_zaposleni_Click(object sender, EventArgs e)
        {
            var select = "select Min(DoSkupaj) as Ukupno,Sum(Ukupno) as Iskoristio,Min(DoSkupaj)-Sum(Ukupno) as Preostalo, A.Godina, A.Radnik " +
"from( " +
"Select DopustStavke.ID as StavkaID, Doskupaj, DoLeto as Godina, DoSifDe as SifraRadnik, (Rtrim(i.DeIme) + ' ' + RTrim(i.DePriimek)) as Radnik, " +
"VremeOd, VremeDo, Ukupno, Napomena, Razlog, StatusGodmora, Odobrio as SifraOdobrio, " +
"(Rtrim(o.DeIme) + ' ' + RTrim(o.DePriimek)) as Odobrio, DatumZahteva, DatumPovratka, DmSifra, DmNaziv as RadnoMest " +
"from DopustStavke " +
"inner join Dopust on Dopust.DoStZapisa = DopustStavke.IdNadredjena " +
"inner join Delavci i on i.DeSifra = DoSifDe " +
"inner join Delavci o on o.DeSifra = Odobrio " +
"inner join DelovnaMesta on i.DeSifDelMes = DelovnaMesta.DmSifra Where DoSifDe=" + Convert.ToInt32(cbo_Zaposleni.SelectedValue.ToString()) + ") as A " +
"group by A.Godina  , A.Radnik";

            SqlConnection conn = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            dataGridView1.Columns[0].Width = 60;
            dataGridView1.Columns[1].Width = 60;
            dataGridView1.Columns[2].Width = 60;
            dataGridView1.Columns[3].Width = 60;
            dataGridView1.Columns[4].Width = 170;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var select = "select Min(DoSkupaj) as Ukupno,Sum(Ukupno) as Iskoristio,Min(DoSkupaj)-Sum(Ukupno) as Preostalo, A.Godina, A.Radnik " +
"from( " +
"Select DopustStavke.ID as StavkaID, Doskupaj, DoLeto as Godina, DoSifDe as SifraRadnik, (Rtrim(i.DeIme) + ' ' + RTrim(i.DePriimek)) as Radnik, " +
"VremeOd, VremeDo, Ukupno, Napomena, Razlog, StatusGodmora, Odobrio as SifraOdobrio, " +
"(Rtrim(o.DeIme) + ' ' + RTrim(o.DePriimek)) as Odobrio, DatumZahteva, DatumPovratka, DmSifra, DmNaziv as RadnoMest " +
"from DopustStavke " +
"inner join Dopust on Dopust.DoStZapisa = DopustStavke.IdNadredjena " +
"inner join Delavci i on i.DeSifra = DoSifDe " +
"inner join Delavci o on o.DeSifra = Odobrio " +
"inner join DelovnaMesta on i.DeSifDelMes = DelovnaMesta.DmSifra Where DoLeto=" + Convert.ToInt32(cbo_Godina.SelectedValue.ToString()) + " and DmSifra="+ Convert.ToInt32(cbo_RM.SelectedValue.ToString())+") as A " +
"group by A.Godina  , A.Radnik";

            SqlConnection conn = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            dataGridView1.Columns[0].Width = 60;
            dataGridView1.Columns[1].Width = 60;
            dataGridView1.Columns[2].Width = 60;
            dataGridView1.Columns[3].Width = 60;
            dataGridView1.Columns[4].Width = 170;
        }

        private void btn_odobrava_Click(object sender, EventArgs e)
        {
            var select = "select Min(DoSkupaj) as Ukupno,Sum(Ukupno) as Iskoristio,Min(DoSkupaj)-Sum(Ukupno) as Preostalo, A.Godina, A.Radnik " +
"from( " +
"Select DopustStavke.ID as StavkaID, Doskupaj, DoLeto as Godina, DoSifDe as SifraRadnik, (Rtrim(i.DeIme) + ' ' + RTrim(i.DePriimek)) as Radnik, " +
"VremeOd, VremeDo, Ukupno, Napomena, Razlog, StatusGodmora, Odobrio as SifraOdobrio, " +
"(Rtrim(o.DeIme) + ' ' + RTrim(o.DePriimek)) as Odobrio, DatumZahteva, DatumPovratka, DmSifra, DmNaziv as RadnoMest " +
"from DopustStavke " +
"inner join Dopust on Dopust.DoStZapisa = DopustStavke.IdNadredjena " +
"inner join Delavci i on i.DeSifra = DoSifDe " +
"inner join Delavci o on o.DeSifra = Odobrio " +
"inner join DelovnaMesta on i.DeSifDelMes = DelovnaMesta.DmSifra Where i.DeOdobrava=" + Convert.ToInt32(cbo_Nadredjeni.SelectedValue.ToString()) + ") as A " +
"group by A.Godina  , A.Radnik";

            SqlConnection conn = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            dataGridView1.Columns[0].Width = 60;
            dataGridView1.Columns[1].Width = 60;
            dataGridView1.Columns[2].Width = 60;
            dataGridView1.Columns[3].Width = 60;
            dataGridView1.Columns[4].Width = 170;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var select = "select Min(DoSkupaj) as Ukupno,Sum(Ukupno) as Iskoristio,Min(DoSkupaj)-Sum(Ukupno) as Preostalo, A.Godina, A.Radnik " +
"from( " +
"Select DopustStavke.ID as StavkaID, Doskupaj, DoLeto as Godina, DoSifDe as SifraRadnik, (Rtrim(i.DeIme) + ' ' + RTrim(i.DePriimek)) as Radnik, " +
"VremeOd, VremeDo, Ukupno, Napomena, Razlog, StatusGodmora, Odobrio as SifraOdobrio, " +
"(Rtrim(o.DeIme) + ' ' + RTrim(o.DePriimek)) as Odobrio, DatumZahteva, DatumPovratka, DmSifra, DmNaziv as RadnoMest " +
"from DopustStavke " +
"inner join Dopust on Dopust.DoStZapisa = DopustStavke.IdNadredjena " +
"inner join Delavci i on i.DeSifra = DoSifDe " +
"inner join Delavci o on o.DeSifra = Odobrio " +
"inner join DelovnaMesta on i.DeSifDelMes = DelovnaMesta.DmSifra Where DoLeto=" + Convert.ToInt32(cbo_Godina.SelectedValue.ToString()) + " and i.DeOdobrava=" + Convert.ToInt32(cbo_Nadredjeni.SelectedValue.ToString()) + ") as A " +
"group by A.Godina  , A.Radnik";

            SqlConnection conn = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            dataGridView1.Columns[0].Width = 60;
            dataGridView1.Columns[1].Width = 60;
            dataGridView1.Columns[2].Width = 60;
            dataGridView1.Columns[3].Width = 60;
            dataGridView1.Columns[4].Width = 170;
        }
    }
}
