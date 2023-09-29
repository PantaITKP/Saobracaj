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
namespace Saobracaj.Mobile
{
    public partial class frmPrijavaSmene2 : Form
    {
        public frmPrijavaSmene2()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var select = "";

            select = "Select ZaposleniPrijava.ID,Zaposleni as ZaposleniID, Rtrim(DePriimek) + ' ' + Rtrim(DeIme) as Zaposleni,DatumPrijave," +
                " DatumOdjave, GetDAte() as TekuceVreme, DateDiff(minute,IsNUll(DatumOdjave,GetDate()), GEtDAte() ) as Minuta, " +
" DateDiff(hour, IsNUll(DatumOdjave, GetDate()), GEtDAte()) as Sati,  DateDiff(minute, DatumPrijave, DatumOdjave) as MinutaTrajanja," +
" DateDiff(hour, DatumPrijave, IsNUll(DatumOdjave, GetDate())) as SatiTrajanjaSmene, AktivnostID,  DateDiff(hour,  IsNUll(DatumOdjave, GetDate()),GEtDAte()) as SatiOdOdjave  from ZaposleniPrijava" +
"   inner join Delavci on DeSifra = ZaposleniPrijava.Zaposleni where DateDiff(hour, DatumPrijave, GEtDAte() ) < 48 order by ZaposleniPrijava.ID desc";

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                var pom = Convert.ToInt32(row.Cells[9].Value.ToString());
                foreach (DataGridViewColumn col in dataGridView1.Columns)
                {
                    if (pom >= 6 && pom < 12)
                    {

                        dataGridView1[9, row.Index].Style.BackColor = Color.Red; //doesn't work
                        dataGridView1[9, row.Index].Style.SelectionBackColor = Color.Red; //doesn't work
                        dataGridView1[9, row.Index].Style.ForeColor = Color.Yellow;  //doesn't work
                       // row.DefaultCellStyle.BackColor = Color.Red;
                       // row.DefaultCellStyle.SelectionBackColor = Color.Red;
                       // row.DefaultCellStyle.ForeColor = Color.Yellow;
                    }
                    else if (pom >= 12)
                    {
                        dataGridView1[9, row.Index].Style.BackColor = Color.Black; //doesn't work
                        dataGridView1[9, row.Index].Style.SelectionBackColor = Color.Black; //doesn't work
                        dataGridView1[9, row.Index].Style.ForeColor = Color.White;  //doesn't work
                       // row.DefaultCellStyle.BackColor = Color.Black;
                       // row.DefaultCellStyle.SelectionBackColor = Color.Black;
                       // row.DefaultCellStyle.ForeColor = Color.White;

                    }

                    //row.Cells[col.Index].Style.BackColor = Color.Green; //doesn't work
                    //col.Cells[row.Index].Style.BackColor = Color.Green; //doesn't work
                    
                }


               



            }
            dataGridView1.Columns[2].DefaultCellStyle.BackColor = Color.Purple;
            dataGridView1.Columns[2].DefaultCellStyle.ForeColor = Color.White;
            dataGridView1.Refresh();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var select = "";

            select = "Select ZaposleniPrijava.ID,Zaposleni as ZaposleniID, Rtrim(DePriimek) + ' ' + Rtrim(DeIme) as Zaposleni,DatumPrijave," +
                " DatumOdjave, GetDAte() as TekuceVreme, DateDiff(minute,IsNUll(DatumOdjave,GetDate()), GEtDAte() ) as Minuta, " +
" DateDiff(hour, IsNUll(DatumOdjave, GetDate()), GEtDAte()) as Sati,  DateDiff(minute, DatumPrijave, DatumOdjave) as MinutaTrajanja," +
" DateDiff(hour, DatumPrijave, IsNUll(DatumOdjave, GetDate())) as SatiTrajanjaSmene, AktivnostID, DateDiff(hour,  IsNUll(DatumOdjave, GetDate()),GEtDAte()) as SatiOdOdjave from ZaposleniPrijava" +
"   inner join Delavci on DeSifra = ZaposleniPrijava.Zaposleni where DatumOdjave is null order by ZaposleniPrijava.ID desc";

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                var pom = Convert.ToInt32(row.Cells[9].Value.ToString());
                foreach (DataGridViewColumn col in dataGridView1.Columns)
                {
                    if (pom >= 6 && pom < 12)
                    {

                        dataGridView1[9, row.Index].Style.BackColor = Color.Red; //doesn't work
                        dataGridView1[9, row.Index].Style.SelectionBackColor = Color.Red; //doesn't work
                        dataGridView1[9, row.Index].Style.ForeColor = Color.Yellow;  //doesn't work
                                                                                     // row.DefaultCellStyle.BackColor = Color.Red;
                                                                                     // row.DefaultCellStyle.SelectionBackColor = Color.Red;
                                                                                     // row.DefaultCellStyle.ForeColor = Color.Yellow;
                    }
                    else if (pom >= 12)
                    {
                        dataGridView1[9, row.Index].Style.BackColor = Color.Black; //doesn't work
                        dataGridView1[9, row.Index].Style.SelectionBackColor = Color.Black; //doesn't work
                        dataGridView1[9, row.Index].Style.ForeColor = Color.White;  //doesn't work
                                                                                    // row.DefaultCellStyle.BackColor = Color.Black;
                                                                                    // row.DefaultCellStyle.SelectionBackColor = Color.Black;
                                                                                    // row.DefaultCellStyle.ForeColor = Color.White;

                    }

                    //row.Cells[col.Index].Style.BackColor = Color.Green; //doesn't work
                    //col.Cells[row.Index].Style.BackColor = Color.Green; //doesn't work

                }






            }
            dataGridView1.Columns[2].DefaultCellStyle.BackColor = Color.Purple;
            dataGridView1.Columns[2].DefaultCellStyle.ForeColor = Color.White;
            dataGridView1.Refresh();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var select = "";

            select = "select Lokomotivaprijava.ID as ID, Lokomotiva, (select case when Smer = 1 then 'ODJAVA' else 'PRIJAVA' end) as Smer," +
                " DateDiff(hour, IsNUll(LokomotivaPrijava.Datum, GetDate()), GEtDAte()) as SatiOdPrijave," + 
                " LokomotivaPrijava.Datum, Rtrim(Delavci.DeIme)+ ' ' + Rtrim(Delavci.DePriimek) as Zaposleni," +
                " AktivnostID  from LokomotivaPrijava " +
                " inner join Delavci on Delavci.DeSifra = LokomotivaPrijava.Zaposleni " +
                " where Smer = 0 and  DateDiff(hour, IsNUll(LokomotivaPrijava.Datum, GetDate()), GEtDAte()) < 48 order by LokomotivaPrijava.ID desc ";

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                var pom = Convert.ToInt32(row.Cells[3].Value.ToString());
                foreach (DataGridViewColumn col in dataGridView1.Columns)
                {
                    if (pom >= 6 && pom < 12)
                    {

                        dataGridView1[3, row.Index].Style.BackColor = Color.Red; //doesn't work
                        dataGridView1[3, row.Index].Style.SelectionBackColor = Color.Red; //doesn't work
                        dataGridView1[3, row.Index].Style.ForeColor = Color.Yellow;  //doesn't work
                                                                                     // row.DefaultCellStyle.BackColor = Color.Red;
                                                                                     // row.DefaultCellStyle.SelectionBackColor = Color.Red;
                                                                                     // row.DefaultCellStyle.ForeColor = Color.Yellow;
                    }
                    else if (pom >= 12)
                    {
                        dataGridView1[3, row.Index].Style.BackColor = Color.Black; //doesn't work
                        dataGridView1[3, row.Index].Style.SelectionBackColor = Color.Black; //doesn't work
                        dataGridView1[3, row.Index].Style.ForeColor = Color.White;  //doesn't work
                                                                                    // row.DefaultCellStyle.BackColor = Color.Black;
                                                                                    // row.DefaultCellStyle.SelectionBackColor = Color.Black;
                                                                                    // row.DefaultCellStyle.ForeColor = Color.White;

                    }

                    //row.Cells[col.Index].Style.BackColor = Color.Green; //doesn't work
                    //col.Cells[row.Index].Style.BackColor = Color.Green; //doesn't work

                }
                int pom2 = Convert.ToInt32(row.Cells[0].Value.ToString());
                int Postoji = 0;
                //Proveri zadnjih 48 sati
                //Convert.ToDateTime(row.Cells[4].Value.ToString())

                Postoji = ProveriIDZeleno(pom2);
                if (Postoji == 1)
                {
                    dataGridView1[1, row.Index].Style.BackColor = Color.Green;//doesn't work
                    dataGridView1[1, row.Index].Style.SelectionBackColor = Color.Green;
                    dataGridView1[1, row.Index].Style.ForeColor = Color.Orange;
                }

            }

     
            
           

            dataGridView1.Columns[5].DefaultCellStyle.BackColor = Color.Purple;
            dataGridView1.Columns[5].DefaultCellStyle.ForeColor = Color.White;
            dataGridView1.Refresh();
        }

        int ProveriIDZeleno(int ID)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection con = new SqlConnection(s_connection);

            con.Open();

            SqlCommand cmd = new SqlCommand(
                " select count(0) as Postoji from " +
                " (Select t1.Zaposleni, t1.ID from " +
                " (select  sum(case when smer = 0 then 1 else 0 end) Prijave,  " +
                " sum(case when smer <> 0 then 1 else 0 end) Odjave, " +
                 " LokomotivaPrijava.Zaposleni,Max(LokomotivaPrijava.ID) as ID " +
                 " from LokomotivaPrijava  group by LokomotivaPrijava.Zaposleni) t1 " +
                " where t1.Prijave > t1.Odjave) t2 where t2.ID = " + ID, con);
            SqlDataReader dr = cmd.ExecuteReader();


            while (dr.Read())
            {
                if (dr["Postoji"].ToString() == "1")
                {
                    con.Close();
                    return 1;
                }
                else
                {
                    return 0;
                    con.Close();

                }
            }
            return 0;
            con.Close();
        }

        private void RefreshNapomeneDispecer()
        {

            var select = "";

            select = "select ID, ZaposleniID, (Rtrim(DeIme) + ' ' + Rtrim(DePriimek)) as Ime,  Vreme, Napomena from NapomenaDispicerima " +
" inner join Delavci on ZaposleniID = DeSifra  order by NapomenaDispicerima.ID desc";

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView2.ReadOnly = true;
            dataGridView2.DataSource = ds.Tables[0];

           
            dataGridView2.Refresh();













        }

        private void button4_Click(object sender, EventArgs e)
        {
            var select = "";

            select = "select Lokomotivaprijava.ID as ID, Lokomotiva, (select case when Smer = 1 then 'ODJAVA' else 'PRIJAVA' end) as Smer," +
                " DateDiff(hour, IsNUll(LokomotivaPrijava.Datum, GetDate()), GEtDAte()) as SatiOdOdjave," +
                " LokomotivaPrijava.Datum, Rtrim(Delavci.DeIme)+ ' ' + Rtrim(Delavci.DePriimek) as Zaposleni," +
                " AktivnostID  from LokomotivaPrijava " +
                " inner join Delavci on Delavci.DeSifra = LokomotivaPrijava.Zaposleni " +
                " where Smer = 1 and  DateDiff(hour, IsNUll(LokomotivaPrijava.Datum, GetDate()), GEtDAte()) < 48 order by LokomotivaPrijava.ID desc ";

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                var pom = Convert.ToInt32(row.Cells[3].Value.ToString());
                foreach (DataGridViewColumn col in dataGridView1.Columns)
                {
                    if (pom >= 6 && pom < 12)
                    {

                        dataGridView1[3, row.Index].Style.BackColor = Color.Red; //doesn't work
                        dataGridView1[3, row.Index].Style.SelectionBackColor = Color.Red; //doesn't work
                        dataGridView1[3, row.Index].Style.ForeColor = Color.Yellow;  //doesn't work
                                                                                     // row.DefaultCellStyle.BackColor = Color.Red;
                                                                                     // row.DefaultCellStyle.SelectionBackColor = Color.Red;
                                                                                     // row.DefaultCellStyle.ForeColor = Color.Yellow;
                    }
                    else if (pom >= 12)
                    {
                        dataGridView1[3, row.Index].Style.BackColor = Color.Black; //doesn't work
                        dataGridView1[3, row.Index].Style.SelectionBackColor = Color.Black; //doesn't work
                        dataGridView1[3, row.Index].Style.ForeColor = Color.White;  //doesn't work
                                                                                    // row.DefaultCellStyle.BackColor = Color.Black;
                                                                                    // row.DefaultCellStyle.SelectionBackColor = Color.Black;
                                                                                    // row.DefaultCellStyle.ForeColor = Color.White;

                    }

                    //row.Cells[col.Index].Style.BackColor = Color.Green; //doesn't work
                    //col.Cells[row.Index].Style.BackColor = Color.Green; //doesn't work

                }


            }
            dataGridView1.Columns[5].DefaultCellStyle.BackColor = Color.Purple;
            dataGridView1.Columns[5].DefaultCellStyle.ForeColor = Color.White;
            dataGridView1.Refresh();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            InsertOpste del = new InsertOpste();
            del.SelectLokomotivaPresek();
          


            {
                var select = "";

                select = "SELECT [Lokomotiva]      ,[ID]      ,[Zaposleni], (Rtrim(DePriimek) +  ' ' + Rtrim(DeIme))  as Zaposleni    ," +
                    " [VremeOd]      ,[VremeDo]      ,[Zatvorena]      ,[TrajanjeSmene]      ,[TrajanjeOdPrijave] " +
                 " ,[TrajanjeOdOdjave]  FROM [LokomotivaPresek] Inner join Delavci on DeSifra = Zaposleni";

                var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
                SqlConnection myConnection = new SqlConnection(s_connection);
                var c = new SqlConnection(s_connection);
                var dataAdapter = new SqlDataAdapter(select, c);

                var commandBuilder = new SqlCommandBuilder(dataAdapter);
                var ds = new DataSet();
                dataAdapter.Fill(ds);
                dataGridView1.ReadOnly = true;
                dataGridView1.DataSource = ds.Tables[0];

                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    var pom = Convert.ToInt32(row.Cells[7].Value.ToString());
                    var pom2 = Convert.ToInt32(row.Cells[8].Value.ToString());
                    var pom3 = Convert.ToInt32(row.Cells[9].Value.ToString());
                    foreach (DataGridViewColumn col in dataGridView1.Columns)
                    {
                        if (pom >= 6 && pom < 12)
                        {

                            dataGridView1[7, row.Index].Style.BackColor = Color.Red; //doesn't work
                            dataGridView1[7, row.Index].Style.SelectionBackColor = Color.Red; //doesn't work
                            dataGridView1[7, row.Index].Style.ForeColor = Color.Yellow;  //doesn't work
                                                                                         // row.DefaultCellStyle.BackColor = Color.Red;
                                                                                         // row.DefaultCellStyle.SelectionBackColor = Color.Red;
                                                                                         // row.DefaultCellStyle.ForeColor = Color.Yellow;
                        }
                        else if (pom >= 12)
                        {
                            dataGridView1[7, row.Index].Style.BackColor = Color.Black; //doesn't work
                            dataGridView1[7, row.Index].Style.SelectionBackColor = Color.Black; //doesn't work
                            dataGridView1[7, row.Index].Style.ForeColor = Color.White;  //doesn't work

                        }
                        if (pom2 >= 6 && pom2 < 12)
                        {

                            dataGridView1[8, row.Index].Style.BackColor = Color.IndianRed; //doesn't work
                            dataGridView1[8, row.Index].Style.SelectionBackColor = Color.IndianRed; //doesn't work
                            dataGridView1[8, row.Index].Style.ForeColor = Color.Yellow;  //doesn't work
                                                                                         // row.DefaultCellStyle.BackColor = Color.Red;
                                                                                         // row.DefaultCellStyle.SelectionBackColor = Color.Red;
                                                                                         // row.DefaultCellStyle.ForeColor = Color.Yellow;
                        }
                        else if (pom2 >= 12)
                        {
                            dataGridView1[8, row.Index].Style.BackColor = Color.DarkGray; //doesn't work
                            dataGridView1[8, row.Index].Style.SelectionBackColor = Color.DarkGray; //doesn't work
                            dataGridView1[8, row.Index].Style.ForeColor = Color.White;  //doesn't work

                        }

                        if (pom3 >= 6 && pom3 < 12)
                        {

                            dataGridView1[9, row.Index].Style.BackColor = Color.Red; //doesn't work
                            dataGridView1[9, row.Index].Style.SelectionBackColor = Color.Red; //doesn't work
                            dataGridView1[9, row.Index].Style.ForeColor = Color.Yellow;  //doesn't work
                                                                                         // row.DefaultCellStyle.BackColor = Color.Red;
                                                                                         // row.DefaultCellStyle.SelectionBackColor = Color.Red;
                                                                                         // row.DefaultCellStyle.ForeColor = Color.Yellow;
                        }
                        else if (pom3 >= 12)
                        {
                            dataGridView1[9, row.Index].Style.BackColor = Color.Black; //doesn't work
                            dataGridView1[9, row.Index].Style.SelectionBackColor = Color.Black; //doesn't work
                            dataGridView1[9, row.Index].Style.ForeColor = Color.White;  //doesn't work

                        }

                        //row.Cells[col.Index].Style.BackColor = Color.Green; //doesn't work
                        //col.Cells[row.Index].Style.BackColor = Color.Green; //doesn't work

                    }


                }
                dataGridView1.Columns[3].DefaultCellStyle.BackColor = Color.Purple;
                dataGridView1.Columns[3].DefaultCellStyle.ForeColor = Color.White;
                dataGridView1.Refresh();
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            RefreshNapomeneDispecer();
        }
    }
}
