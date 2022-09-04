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

namespace Saobracaj.Uvoz
{
    public partial class Nalogodavci : Form
    {
        bool status = false;
        string connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"].ConnectionString;
        public Nalogodavci()
        {
            InitializeComponent();
            FillGV();
            FillCombo();
        }
        private void FillGV()
        {
            var select = "Select * From Nalogodavci order by ID desc";
            SqlConnection conn = new SqlConnection(connection);
            var da = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            da.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];
        }
        private void FillCombo()
        {
            var select = "Select PaSifra,PaNaziv From Partnerji order by PaNaziv";
            SqlConnection conn = new SqlConnection(connection);
            var da = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            da.Fill(ds);
            cboPartner.DataSource = ds.Tables[0];
            cboPartner.DisplayMember = "PaNaziv";
            cboPartner.ValueMember = "PaSifra";
        }

        private void tsNew_Click(object sender, EventArgs e)
        {
            status = true;
            tsNew.Enabled = false;
        }

        private void tsSave_Click(object sender, EventArgs e)
        {
            InsertNalogodavci ins = new InsertNalogodavci();
            if (status == true)
            {
                ins.InsNalogodavci(Convert.ToInt32(cboPartner.SelectedValue.ToString()), cboPartner.SelectedText.ToString());
            }
            else
            {
                ins.UpdNalogodavci(Convert.ToInt32(txtID.Text.ToString()), Convert.ToInt32(cboPartner.SelectedValue.ToString()), cboPartner.SelectedText.ToString());
            }
            FillGV();
            tsNew.Enabled = true;
            status = false;
        }

        private void tsDelete_Click(object sender, EventArgs e)
        {
            InsertNalogodavci ins = new InsertNalogodavci();
            ins.DelNalogodavci(Convert.ToInt32(txtID.Text));
            FillGV();
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                foreach(DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Selected)
                    {
                        txtID.Text = row.Cells[0].Value.ToString();
                    }
                }
            }
            catch { }
        }
    }
}
