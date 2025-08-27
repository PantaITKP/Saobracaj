using Microsoft.ReportingServices.Diagnostics.Internal;
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

namespace Saobracaj.MLProd
{
    public partial class PonudaML : Form
    {
        string connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"].ConnectionString;

        public PonudaML()
        {
            InitializeComponent();
            FillCombo();
            FillChkList();
        }
        private void FillCombo()
        {
            var select = "select RTrim(Cast(PorudzbinaID as nvarchar(10)))+'-'+RTrim(PrevozniPut) as PrevozniPut,PorudzbinaID From Najava where PorudzbinaID<10000 order by PorudzbinaID desc";
            SqlConnection conn = new SqlConnection(connection);
            var da = new SqlDataAdapter(select, conn);
            var ds = new System.Data.DataSet();
            da.Fill(ds);
            comboBox1.DataSource = ds.Tables[0];
            comboBox1.DisplayMember = "PrevozniPut";
            comboBox1.ValueMember = "PorudzbinaID";
        }
        private void FillChkList()
        {
            using (var cn = new SqlConnection(connection))
            using (var da = new SqlDataAdapter(
                "SELECT ID, RTRIM(Naziv) AS Naziv FROM VrstaAktivnosti ORDER BY ID ASC", cn))
            {
                var dt = new DataTable();
                da.Fill(dt);

                var dt2 = dt.Copy();

                chkListPrijem.DataSource = dt;
                chkListPrijem.DisplayMember = "Naziv";
                chkListPrijem.ValueMember = "ID";
                chkListPrijem.CheckOnClick = true;

                chkListPredaja.DataSource = dt2;
                chkListPredaja.DisplayMember = "Naziv";
                chkListPredaja.ValueMember = "ID";
                chkListPredaja.CheckOnClick = true;
            }
        }
    }
}
