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

namespace Saobracaj.Sifarnici
{
    public partial class CIT : Form
    {
        bool statusCIT = false;
        bool statusCITStavke = false;
        public CIT()
        {
            InitializeComponent();
            FillGV();
            FillCombo();
        }

        private void FillGV()
        {
            
            var connection=ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"].ConnectionString;
            SqlConnection conn = new SqlConnection(connection);

            var select = "Select * from CIT";
            var da = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            da.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];

            var select2 = "Select * from CITStavke";
            var da2 = new SqlDataAdapter(select2, conn);
            var ds2 = new DataSet();
            da2.Fill(ds2);
            dataGridView2.ReadOnly = true;
            dataGridView2.DataSource = ds2.Tables[0];
            

        }
        private void FillCombo()
        {
            var connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            var select = "Select Broj From NHM";
            SqlConnection conn = new SqlConnection(connection);
            var da = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            da.Fill(ds);
            comboBox1.DataSource = ds.Tables[0];
            comboBox1.DisplayMember = "Broj";
            comboBox1.ValueMember = "Broj";
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            statusCIT = true;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            InsertCIT ins = new InsertCIT();
            if (statusCIT == true)
            {
                ins.InsCIT(txtOtpravna.Text, txtUputna.Text, txtPrevozniPut.Text, txtPosiljalac.Text, txtCarDokument.Text, txtDrzava.Text, txtPreduzece.Text, txtPrimalac.Text,
                    txtPratilac.Text, txtStanica.Text, txtOtpravljanje.Text, txtPreuzima.Text, txtSluzbenaNapomena.Text, txtPotpisnik.Text);
            }
            else
            {
                ins.UpdCIT(Convert.ToInt32(txt_Sifra.Text), txtOtpravna.Text, txtUputna.Text, txtPrevozniPut.Text, txtPosiljalac.Text, txtCarDokument.Text, txtDrzava.Text, txtPreduzece.Text, txtPrimalac.Text,
                    txtPratilac.Text, txtStanica.Text, txtOtpravljanje.Text, txtPreuzima.Text, txtSluzbenaNapomena.Text, txtPotpisnik.Text);
            }
            FillGV();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            InsertCIT ins = new InsertCIT();
            ins.DelCIT(Convert.ToInt32(txt_Sifra.Text));
        }

        private void btnNew2_Click(object sender, EventArgs e)
        {
            statusCITStavke = true;
        }

        private void btnSave2_Click(object sender, EventArgs e)
        {
            InsertCitStavke ins = new InsertCitStavke();
            if (statusCITStavke == true)
            {
                ins.InsCITStavke(Convert.ToInt32(txtIDCit.Text), Convert.ToInt32(txtBR.Text), comboBox1.SelectedValue.ToString(), Convert.ToDouble(txtMasa.Text.ToString()),
                    Convert.ToDouble(txtTara.Text.ToString()), Convert.ToDouble(txtBruto.Text.ToString()), txtVrstaRobe.Text, txtRID.Text, txtNapomena.Text);
            }
            else
            {
                ins.UpdCITStavke(Convert.ToInt32(txtID.Text), Convert.ToInt32(txtIDCit.Text), Convert.ToInt32(txtBR.Text), comboBox1.SelectedValue.ToString(), Convert.ToDouble(txtMasa.Text.ToString()),
                    Convert.ToDouble(txtTara.Text.ToString()), Convert.ToDouble(txtBruto.Text.ToString()), txtVrstaRobe.Text, txtRID.Text, txtNapomena.Text);
            }
            FillGV();
        }

        private void btnDelete2_Click(object sender, EventArgs e)
        {
            InsertCitStavke ins = new InsertCitStavke();
            ins.DelCITStavke(Convert.ToInt32(txtID.Text));
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Selected)
                    {
                        txt_Sifra.Text = row.Cells[0].Value.ToString();
                        txtOtpravna.Text = row.Cells[1].Value.ToString();
                        txtUputna.Text = row.Cells[2].Value.ToString();
                        txtPrevozniPut.Text = row.Cells[3].Value.ToString();
                        txtPosiljalac.Text = row.Cells[4].Value.ToString();
                        txtCarDokument.Text = row.Cells[5].Value.ToString();
                        txtDrzava.Text = row.Cells[6].Value.ToString();
                        txtPreduzece.Text = row.Cells[7].Value.ToString();
                        txtPrimalac.Text = row.Cells[8].Value.ToString();
                        txtPratilac.Text = row.Cells[9].Value.ToString();
                        txtStanica.Text = row.Cells[10].Value.ToString();
                        txtOtpravljanje.Text = row.Cells[11].Value.ToString();
                        txtPreuzima.Text = row.Cells[12].Value.ToString();
                        txtSluzbenaNapomena.Text = row.Cells[13].Value.ToString();
                        txtPotpisnik.Text = row.Cells[14].Value.ToString();

                    }
                }
            }
            catch
            {
                MessageBox.Show("Nije uspela selekcija stavki");
            }
        }

        private void dataGridView2_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                foreach(DataGridViewRow row in dataGridView2.Rows)
                {
                    if (row.Selected)
                    {
                        txtID.Text = row.Cells[0].Value.ToString();
                        txtIDCit.Text = row.Cells[1].Value.ToString();
                        txtBR.Text = row.Cells[2].Value.ToString();
                        comboBox1.SelectedValue = row.Cells[3].Value.ToString();
                        txtMasa.Text = row.Cells[4].Value.ToString();
                        txtTara.Text = row.Cells[5].Value.ToString();
                        txtBruto.Text = row.Cells[6].Value.ToString();
                        txtVrstaRobe.Text = row.Cells[7].Value.ToString();
                        txtRID.Text = row.Cells[8].Value.ToString();
                        txtNapomena.Text = row.Cells[9].Value.ToString();
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
