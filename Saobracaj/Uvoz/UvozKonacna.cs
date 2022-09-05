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
    public partial class UvozKonacna : Form
    {
        public string connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"].ConnectionString;
        string nalogodavci = "";
        string usluge = "";
        public UvozKonacna()
        {
            InitializeComponent();
            FillCheck();
            FillCombo();
            FillGV();
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            Uvoz uv = new Uvoz();
            uv.Show();
        }
        private void FillGV()
        {
            var select = "Select * From UvozKonacna order by ID desc";
            SqlConnection conn = new SqlConnection(connection);
            var da = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            da.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];
        }
        private void FillCombo()
        {
            SqlConnection conn = new SqlConnection(connection);

            var dir = "Select ID,Naziv from PredefinisanePoruke order by ID";
            var dirAD = new SqlDataAdapter(dir, conn);
            var dirDS = new DataSet();
            dirAD.Fill(dirDS);
            cbDirigacija.DataSource = dirDS.Tables[0];
            cbDirigacija.DisplayMember = "Naziv";
            cbDirigacija.ValueMember = "ID";
            //carinski postupak
            var dir2 = "Select ID,Naziv from PredefinisanePoruke order by ID";
            var dirAD2 = new SqlDataAdapter(dir2, conn);
            var dirDS2 = new DataSet();
            dirAD2.Fill(dirDS2);
            cboCarinskiPostupak.DataSource = dirDS2.Tables[0];
            cboCarinskiPostupak.DisplayMember = "Naziv";
            cboCarinskiPostupak.ValueMember = "ID";
            //postupak roba/kont
            var dir3 = "Select ID,Naziv from PredefinisanePoruke order by ID";
            var dirAD3 = new SqlDataAdapter(dir3, conn);
            var dirDS3 = new DataSet();
            dirAD3.Fill(dirDS3);
            cbPostupak.DataSource = dirDS3.Tables[0];
            cbPostupak.DisplayMember = "Naziv";
            cbPostupak.ValueMember = "ID";
            //nacin pakovanja
            var dir4 = "Select ID,Naziv from PredefinisanePoruke order by ID";
            var dirAD4 = new SqlDataAdapter(dir4, conn);
            var dirDS4 = new DataSet();
            dirAD4.Fill(dirDS4);
            cbNacinPakovanja.DataSource = dirDS4.Tables[0];
            cbNacinPakovanja.DisplayMember = "Naziv";
            cbNacinPakovanja.ValueMember = "ID";
            //napomena pozicioniranje
            var dir5 = "Select ID,Naziv from PredefinisanePoruke order by ID";
            var dirAD5 = new SqlDataAdapter(dir5, conn);
            var dirDS5 = new DataSet();
            dirAD5.Fill(dirDS5);
            cbNapomenaPoz.DataSource = dirDS5.Tables[0];
            cbNapomenaPoz.DisplayMember = "Naziv";
            cbNapomenaPoz.ValueMember = "ID";

            var brod = "Select ID,Naziv From Brodovi order by ID";
            var brodAD = new SqlDataAdapter(brod, conn);
            var brodDS = new DataSet();
            brodAD.Fill(brodDS);
            cbBrod.DataSource = brodDS.Tables[0];
            cbBrod.DisplayMember = "Naziv";
            cbBrod.ValueMember = "ID";

            var car = "Select ID,Naziv From Carinarnice order by ID";
            var carAD = new SqlDataAdapter(car, conn);
            var carDS = new DataSet();
            carAD.Fill(carDS);
            cbOcarina.DataSource = carDS.Tables[0];
            cbOcarina.DisplayMember = "Naziv";
            cbOcarina.ValueMember = "ID";

            var partner = "Select PaSifra,PaNaziv From Partnerji order by PaSifra";
            var partAD = new SqlDataAdapter(partner, conn);
            var partDS = new DataSet();
            partAD.Fill(partDS);
            cbVlasnikKont.DataSource = partDS.Tables[0];
            cbVlasnikKont.DisplayMember = "PaNaziv";
            cbVlasnikKont.ValueMember = "PaSifra";
            //uvoznik
            var partner2 = "Select PaSifra,PaNaziv From Partnerji order by PaSifra";
            var partAD2 = new SqlDataAdapter(partner2, conn);
            var partDS2 = new DataSet();
            partAD2.Fill(partDS2);
            cboUvoznik.DataSource = partDS2.Tables[0];
            cboUvoznik.DisplayMember = "PaNaziv";
            cboUvoznik.ValueMember = "PaSifra";
            //spedicija na granici
            var partner3 = "Select PaSifra,PaNaziv From Partnerji order by PaSifra";
            var partAD3 = new SqlDataAdapter(partner3, conn);
            var partDS3 = new DataSet();
            partAD3.Fill(partDS3);
            cboSpedicijaG.DataSource = partDS3.Tables[0];
            cboSpedicijaG.DisplayMember = "PaNaziv";
            cboSpedicijaG.ValueMember = "PaSifra";
            //spedicija rtc luka leget
            var partner4 = "Select PaSifra,PaNaziv From Partnerji order by PaSifra";
            var partAD4 = new SqlDataAdapter(partner4, conn);
            var partDS4 = new DataSet();
            partAD4.Fill(partDS4);
            cboSpedicijaRTC.DataSource = partDS4.Tables[0];
            cboSpedicijaRTC.DisplayMember = "PaNaziv";
            cboSpedicijaRTC.ValueMember = "PaSifra";
            //odredisna spedicija
            var partner5 = "Select PaSifra,PaNaziv From Partnerji order by PaSifra";
            var partAD5 = new SqlDataAdapter(partner5, conn);
            var partDS5 = new DataSet();
            partAD5.Fill(partDS5);
            cbOspedicija.DataSource = partDS5.Tables[0];
            cbOspedicija.DisplayMember = "PaNaziv";
            cbOspedicija.ValueMember = "PaSifra";

            var nhm = "Select ID,Broj,Naziv from NHM order by ID";
            var nhmAD = new SqlDataAdapter(nhm, conn);
            var nhmDS = new DataSet();
            nhmAD.Fill(nhmDS);
            cboNHM.DataSource = nhmDS.Tables[0];
            cboNHM.DisplayMember = "Broj";
            cboNHM.ValueMember = "ID";
            //naziv robe
            cboNazivRobe.DataSource = nhmDS.Tables[0];
            cboNazivRobe.DisplayMember = "Naziv";
            cboNazivRobe.ValueMember = "Naziv";
        }
        private void FillCheck()
        {
            var query = "Select PaSifra,PaNaziv From Nalogodavci";
            SqlConnection conn = new SqlConnection(connection);
            var da = new SqlDataAdapter(query, conn);
            var ds = new DataSet();
            da.Fill(ds);
            clNalogodavac.DataSource = ds.Tables[0];
            clNalogodavac.DisplayMember = "PaNaziv";
            clNalogodavac.ValueMember = "PaSifra";

            var select = "Select Naziv,TipManipulacije from VrstaManipulacije";
            var da2 = new SqlDataAdapter(select, conn);
            var ds2 = new DataSet();
            da2.Fill(ds2);
            clVrstaUsluga.DataSource = ds2.Tables[0];
            clVrstaUsluga.DisplayMember = "Naziv";
            clVrstaUsluga.ValueMember = "Naziv";
        }

        private void tsDelete_Click(object sender, EventArgs e)
        {
            InsertUvozKonacna uv = new InsertUvozKonacna();
            uv.DelUvozKonacna(Convert.ToInt32(txtID.Text));
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Selected)
                {
                    txtID.Text = row.Cells[0].Value.ToString();
                    dtEtaRijeka.Value = Convert.ToDateTime(row.Cells[1].Value.ToString());
                    dtAtaRijeka.Value = Convert.ToDateTime(row.Cells[2].Value.ToString());
                    txtStatus.Text = row.Cells[3].Value.ToString();
                    txtBrKont.Text = row.Cells[4].Value.ToString();
                    txtTipKont.Text = row.Cells[5].Value.ToString();
                    dtNalogBrodara.Value = Convert.ToDateTime(row.Cells[6].Value.ToString());
                    txtBZ.Text = row.Cells[7].Value.ToString();
                    txtNapomena.Text = row.Cells[8].Value.ToString();
                    txtPIN.Text = row.Cells[9].Value.ToString();
                    cbDirigacija.SelectedValue = row.Cells[10].Value.ToString();
                    cbBrod.SelectedValue = row.Cells[11].Value.ToString();
                    txtTeretnica.Text = row.Cells[12].Value.ToString();
                    txtADR.Text = row.Cells[13].Value.ToString();
                    cbVlasnikKont.SelectedValue = row.Cells[14].Value.ToString();
                    txtBuking.Text = row.Cells[15].Value.ToString();
                    cboUvoznik.SelectedValue = row.Cells[18].Value.ToString();
                    cboNHM.SelectedValue = row.Cells[19].Value.ToString();
                    cboSpedicijaG.SelectedValue = row.Cells[21].Value.ToString();
                    cboSpedicijaRTC.SelectedValue = row.Cells[22].Value.ToString();
                    cboCarinskiPostupak.SelectedValue = row.Cells[23].Value.ToString();
                    cbPostupak.SelectedValue = row.Cells[24].Value.ToString();
                    cbNacinPakovanja.SelectedValue = row.Cells[25].Value.ToString();
                    cbOcarina.SelectedValue = row.Cells[26].Value.ToString();
                    cbOspedicija.SelectedValue = row.Cells[27].Value.ToString();
                    txtMesto.Text = row.Cells[28].Value.ToString();
                    txtKontaktOsoba.Text = row.Cells[29].Value.ToString();
                    txtMail.Text = row.Cells[30].Value.ToString();
                    txtPlomba1.Text = row.Cells[31].Value.ToString();
                    txtPlomba2.Text = row.Cells[32].Value.ToString();
                    txtNetoR.Value = Convert.ToDecimal(row.Cells[33].Value.ToString());
                    txtBrutoR.Value = Convert.ToDecimal(row.Cells[34].Value.ToString());
                    txtTaraK.Value = Convert.ToDecimal(row.Cells[35].Value.ToString());
                    txtBrutoK.Value = Convert.ToDecimal(row.Cells[36].Value.ToString());
                    cbNapomenaPoz.SelectedValue = row.Cells[37].Value.ToString();
                    dtAtaOtprema.Value = Convert.ToDateTime(row.Cells[38].Value.ToString());
                    txtBrojVoza.Text = row.Cells[39].Value.ToString();
                    txtRelacija.Text = row.Cells[40].Value.ToString();
                    dtAtaDolazak.Value = Convert.ToDateTime(row.Cells[41].Value.ToString());

                    string pomNal = row.Cells[16].Value.ToString();
                    string[] nal = pomNal.Split(',');
                    foreach (var word in nal)
                    {
                        for (int i = 0; i < nal.Length; i++)
                        {

                            //if (clNalogodavac.GetSelected(i))
                            //{
                            clNalogodavac.SetItemChecked(i, true);
                            //}

                        }
                    }
                    string pomRob = row.Cells[17].Value.ToString();
                    string[] rob = pomRob.Split(',');
                    foreach (var r in rob)
                    {
                        for (int i = 0; i < rob.Length; i++)
                        {
                            clVrstaUsluga.SetItemChecked(i, true);
                        }
                    }
                }
            }
        }

        private void tsSave_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clNalogodavac.Items.Count; i++)
            {
                if (clNalogodavac.GetItemCheckState(i) == CheckState.Checked)
                {
                    if (i == 0)
                    {
                        clNalogodavac.SetSelected(i, true);
                        nalogodavci = clNalogodavac.SelectedValue.ToString();
                    }
                    else
                    {
                        clNalogodavac.SetSelected(i, true);
                        nalogodavci = nalogodavci + "," + clNalogodavac.SelectedValue.ToString();
                    }
                }
            }
            for (int i = 0; i < clVrstaUsluga.Items.Count; i++)
            {
                if (clVrstaUsluga.GetItemCheckState(i) == CheckState.Checked)
                {
                    if (i == 0)
                    {
                        clVrstaUsluga.SetSelected(i, true);
                        usluge = clVrstaUsluga.SelectedValue.ToString();
                    }
                    else
                    {
                        clVrstaUsluga.SetSelected(i, true);
                        usluge = usluge + "," + clVrstaUsluga.SelectedValue.ToString();
                    }
                }
            }
            InsertUvozKonacna ins = new InsertUvozKonacna();
            ins.UpdUvozKonacna(Convert.ToInt32(txtID.Text), Convert.ToDateTime(dtEtaRijeka.Value.ToString()),
                Convert.ToDateTime(dtAtaRijeka.Value.ToString()), txtStatus.Text.ToString().TrimEnd(), Convert.ToInt32(txtBrKont.Text),
                txtTipKont.Text.ToString().TrimEnd(), Convert.ToDateTime(dtNalogBrodara.Value.ToString()), txtBZ.Text.ToString().TrimEnd(),
                txtNapomena.Text.ToString().TrimEnd(), txtPIN.Text.ToString().TrimEnd(), Convert.ToInt32(cbDirigacija.SelectedValue), Convert.ToInt32(cbBrod.SelectedValue),
                Convert.ToInt32(txtTeretnica.Text), Convert.ToInt32(txtADR.Text), Convert.ToInt32(cbVlasnikKont.SelectedValue), Convert.ToInt32(txtBuking.Text), nalogodavci,
                usluge, Convert.ToInt32(cboUvoznik.SelectedValue), Convert.ToInt32(cboNHM.SelectedValue), cboNazivRobe.SelectedValue.ToString(), Convert.ToInt32(cboSpedicijaG.SelectedValue),
                Convert.ToInt32(cboSpedicijaRTC.SelectedValue), Convert.ToInt32(cboCarinskiPostupak.SelectedValue), Convert.ToInt32(cbPostupak.SelectedValue),
                Convert.ToInt32(cbNacinPakovanja.SelectedValue), Convert.ToInt32(cbOcarina.SelectedValue), Convert.ToInt32(cbOspedicija.SelectedValue),
                txtMesto.Text.ToString().TrimEnd(), txtKontaktOsoba.Text.ToString().TrimEnd(), txtMail.Text.ToString(), Convert.ToInt32(txtPlomba1.Text),
                Convert.ToInt32(txtPlomba2.Text), Convert.ToDecimal(txtNetoR.Value), Convert.ToDecimal(txtBrutoR.Value), Convert.ToDecimal(txtTaraK.Value),
                Convert.ToDecimal(txtBrutoK.Value), Convert.ToInt32(cbNapomenaPoz.SelectedValue), Convert.ToDateTime(dtAtaOtprema.Value.ToString()),
                Convert.ToInt32(txtBrojVoza.Text), txtRelacija.Text.ToString().TrimEnd(), Convert.ToDateTime(dtAtaDolazak.Value.ToString()));
            FillGV();
        }
    }
}
