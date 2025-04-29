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
using Microsoft.Office.Interop.Excel;
using Syncfusion.XlsIO.Interfaces;

namespace Saobracaj.Dokumenta
{
    public partial class frmZarade : Form
    {
        System.Data.DataTable ndt;
        public frmZarade()
        {
            InitializeComponent();
        }

        private void btnStampa_Click(object sender, EventArgs e)
        {
           int pomMilsped = 0;
            if (chkMilsped.Checked == true)
                pomMilsped = 2;
          

            int pomLokomotiva = 0;
            if (chkLokomotiva.Checked == true)
                pomMilsped = 0;
           

            int pomVanLokomotive = 0;
            if (chkVanLokomotive.Checked == true)
                pomMilsped = 1;
          
            Perftech_BeogradDataSet3TableAdapters.SelectAktivnostiTableAdapter ta = new Perftech_BeogradDataSet3TableAdapters.SelectAktivnostiTableAdapter();
            Perftech_BeogradDataSet3.SelectAktivnostiDataTable dt = new Perftech_BeogradDataSet3.SelectAktivnostiDataTable();

            Perftech_BeogradDataSet3TableAdapters.SelectAktPPKTableAdapter tap = new Perftech_BeogradDataSet3TableAdapters.SelectAktPPKTableAdapter();
            Perftech_BeogradDataSet3.SelectAktPPKDataTable dtp = new Perftech_BeogradDataSet3.SelectAktPPKDataTable();

            Perftech_BeogradDataSet3TableAdapters.SelectAktDISPTableAdapter tad = new Perftech_BeogradDataSet3TableAdapters.SelectAktDISPTableAdapter();
            Perftech_BeogradDataSet3.SelectAktDISPDataTable dtd = new Perftech_BeogradDataSet3.SelectAktDISPDataTable();
            // Perftech_BeogradDataSet3TableAdapters. tap = new Perftech_BeogradDataSet3TableAdapters.SelectAktPPKTableAdapter();
           // Perftech_BeogradDataSet3.SelectAktPPKDataTable dtp = new Perftech_BeogradDataSet3.SelectAktPPKDataTable();
            Perftech_BeogradDataSet3TableAdapters.SelectObracunUkupnoTableAdapter tao = new Perftech_BeogradDataSet3TableAdapters.SelectObracunUkupnoTableAdapter();
            Perftech_BeogradDataSet3.SelectObracunUkupnoDataTable dto = new Perftech_BeogradDataSet3.SelectObracunUkupnoDataTable();


            Perftech_BeogradDataSet3TableAdapters.SelectAktivnostiStavkeTableAdapter tal = new Perftech_BeogradDataSet3TableAdapters.SelectAktivnostiStavkeTableAdapter();
            Perftech_BeogradDataSet3.SelectAktivnostiStavkeDataTable dtl = new Perftech_BeogradDataSet3.SelectAktivnostiStavkeDataTable();

            Perftech_BeogradDataSet3TableAdapters.SelectObracunUkupnoUmanjenTableAdapter tum = new Perftech_BeogradDataSet3TableAdapters.SelectObracunUkupnoUmanjenTableAdapter();
            Perftech_BeogradDataSet3.SelectObracunUkupnoUmanjenDataTable dum = new Perftech_BeogradDataSet3.SelectObracunUkupnoUmanjenDataTable();

            Perftech_BeogradDataSet3TableAdapters.SelectAktivnostiTrosakTableAdapter tao2 = new Perftech_BeogradDataSet3TableAdapters.SelectAktivnostiTrosakTableAdapter();
            Perftech_BeogradDataSet3.SelectAktivnostiTrosakDataTable dto2 = new Perftech_BeogradDataSet3.SelectAktivnostiTrosakDataTable();


            Perftech_BeogradDataSet3TableAdapters.SelectAktNocniTableAdapter Nap = new Perftech_BeogradDataSet3TableAdapters.SelectAktNocniTableAdapter();
            Perftech_BeogradDataSet3.SelectAktNocniDataTable ntp = new Perftech_BeogradDataSet3.SelectAktNocniDataTable();


            int dtMilsped = pomMilsped;
          
            ta.Fill(dt, Convert.ToInt32(cboZaposleni.SelectedValue), Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value), Convert.ToInt32(pomMilsped));
            tap.Fill(dtp, Convert.ToInt32(cboZaposleni.SelectedValue), Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value), Convert.ToInt32(pomMilsped));
            tad.Fill(dtd, Convert.ToInt32(cboZaposleni.SelectedValue), Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value), Convert.ToInt32(pomMilsped));
            tao.Fill(dto, Convert.ToInt32(cboZaposleni.SelectedValue), Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value), Convert.ToInt32(pomMilsped));
            tum.Fill(dum, Convert.ToInt32(cboZaposleni.SelectedValue), Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value), Convert.ToInt32(pomMilsped));
            tao2.Fill(dto2, Convert.ToInt32(cboZaposleni.SelectedValue), Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value), Convert.ToInt32(pomMilsped));
            Nap.Fill(ntp, Convert.ToInt32(cboZaposleni.SelectedValue), Convert.ToDateTime(dtpVremeOd.Value), Convert.ToDateTime(dtpVremeDo.Value), Convert.ToInt32(pomMilsped));

            ReportDataSource rds = new ReportDataSource();
            rds.Name = "DataSetZarada";
            rds.Value = dt;
            DateTime dtStartDate = dtpVremeOd.Value;
            DateTime dtEndDate = dtpVremeDo.Value;
           

            ReportDataSource rdsp = new ReportDataSource();
            rdsp.Name = "DataSetPPK";
            rdsp.Value = dtp;

            ReportDataSource rdsd = new ReportDataSource();
            rdsd.Name = "DataSetDISP";
            rdsd.Value = dtd;

            ReportDataSource rdso = new ReportDataSource();
            rdso.Name = "DataSetStav";
            rdso.Value = dto;

            ReportDataSource rdum = new ReportDataSource();
            rdum.Name = "DataSetUm";
            rdum.Value = dum;

            ReportDataSource rdum2 = new ReportDataSource();
            rdum2.Name = "DataSetTros";
            rdum2.Value = dto2;

            ReportDataSource rdsn = new ReportDataSource();
            rdsn.Name = "DataSetNocni";
            rdsn.Value = ntp;

            tal.Fill(dtl);
           
            ReportDataSource rdsl = new ReportDataSource();
            rdsl.Name = "DataSetZaradaStavke";
            rdsl.Value = dtl;
            ndt = dtl;

           

            ReportParameter[] par = new ReportParameter[4];
            par[0] = new ReportParameter("ID", cboZaposleni.SelectedValue.ToString());
            par[1] = new ReportParameter("DatumOd", dtStartDate.ToLongDateString(), false);
            par[2] = new ReportParameter("DatumDo", dtEndDate.ToLongDateString(), false);
            par[3] = new ReportParameter("Milsped", dtMilsped.ToString(), false);
          

            reportViewer1.LocalReport.DataSources.Clear();
            reportViewer1.LocalReport.ReportPath = "Zarada.rdlc";
            reportViewer1.LocalReport.SetParameters(par);
            reportViewer1.LocalReport.DataSources.Add(rds);
            reportViewer1.LocalReport.DataSources.Add(rdsp);
            reportViewer1.LocalReport.DataSources.Add(rdsd);
            reportViewer1.LocalReport.DataSources.Add(rdso);
            reportViewer1.LocalReport.DataSources.Add(rdum);
            reportViewer1.LocalReport.DataSources.Add(rdum2);
            reportViewer1.LocalReport.DataSources.Add(rdsn);
            reportViewer1.LocalReport.SubreportProcessing += new
                           SubreportProcessingEventHandler(SetSubDataSource);
            reportViewer1.RefreshReport();
        }

        public void SetSubDataSource(object sender, SubreportProcessingEventArgs e)
        {
            e.DataSources.Add(new ReportDataSource("DataSetZaradaStavke", ndt));
        }

        private void frmZarade_Load(object sender, EventArgs e)
        {
            var select3 = " select DeSifra as ID, (Rtrim(DePriimek) + ' ' + RTrim(DeIme)) as Opis from Delavci order by opis";
            var s_connection3 = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection3 = new SqlConnection(s_connection3);
            var c3 = new SqlConnection(s_connection3);
            var dataAdapter3 = new SqlDataAdapter(select3, c3);

            var commandBuilder3 = new SqlCommandBuilder(dataAdapter3);
            var ds3 = new DataSet();
            dataAdapter3.Fill(ds3);
            cboZaposleni.DataSource = ds3.Tables[0];
            cboZaposleni.DisplayMember = "Opis";
            cboZaposleni.ValueMember = "ID";
           // this.reportViewer1.RefreshReport();
           // this.reportViewer1.RefreshReport();
        }

        private void frmZarade_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection conn = new SqlConnection(s_connection);
            SqlCommand cmd = new SqlCommand("Update Aktivnosti Set KontrolisaoAdmin=1 Where Zaposleni=@id and VremeOd>=@vremeOd and VremeDo<=@vremeDo", conn);
            cmd.Parameters.AddWithValue("@id", Convert.ToInt32(cboZaposleni.SelectedValue));
            cmd.Parameters.AddWithValue("@vremeOd", Convert.ToDateTime(dtpVremeOd.Value));
            cmd.Parameters.AddWithValue("@vremeDo",Convert.ToDateTime(dtpVremeDo.Value));
            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                MessageBox.Show("Uspešno ažurirano!");
                conn.Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
