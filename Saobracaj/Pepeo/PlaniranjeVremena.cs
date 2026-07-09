using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace Saobracaj.Pepeo
{
    public partial class PlaniranjeVremena : Form
    {
        private readonly InsertPepeo insert = new InsertPepeo();
        private List<PepeoKorakVremena> trenutniKoraci = new List<PepeoKorakVremena>();

        public PlaniranjeVremena()
        {
            InitializeComponent();
        }

        private void PlaniranjeVremena_Load(object sender, EventArgs e)
        {
            dateTimePicker1.Value = DateTime.Now;
            dateTimePicker2.Value = DateTime.Now;
            numBrojCiklusa.Value = 3;
            FillCombo();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Pepeo.Trase frm = new Trase();
            frm.Show();
        }

        private void FillCombo()
        {
            try
            {
                DataTable dtTrase = insert.UcitajTrase();
                cboTrasa.DataSource = dtTrase;
                cboTrasa.DisplayMember = "Stanice";
                cboTrasa.ValueMember = "ID";

                DataTable dtNajave = insert.UcitajNajave();
                cboNajava.DataSource = dtNajave;
                cboNajava.DisplayMember = "Put";
                cboNajava.ValueMember = "ID";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri učitavanju trasa/najava:" + Environment.NewLine + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnIzracunajVreme_Click(object sender, EventArgs e)
        {
            int idTrase = VratiIntIzCombo(cboTrasa.SelectedValue);
            int najava = VratiIntIzCombo(cboNajava.SelectedValue);

            if (idTrase <= 0)
            {
                MessageBox.Show("Izaberite trasu.");
                return;
            }

            if (najava <= 0)
            {
                MessageBox.Show("Izaberite najavu.");
                return;
            }

            DateTime pocetakPrevoza = dateTimePicker1.Value;
            int brojCiklusa = Convert.ToInt32(numBrojCiklusa.Value);

            try
            {
                List<PepeoSablonOperacije> sabloni = insert.UcitajSabloneOperacija();
                Dictionary<string, string> parametri = insert.UcitajParametrePravila();

                if (sabloni.Count == 0)
                {
                    MessageBox.Show("Nije pronađen šablon operacija. Pokrenite SQL skripte 01, 02 i 03.");
                    return;
                }

                PepeoKalkulatorVremena kalkulator = new PepeoKalkulatorVremena(sabloni, parametri);
                trenutniKoraci = kalkulator.Izracunaj(pocetakPrevoza, brojCiklusa, idTrase, najava);

                insert.SacuvajPlanVremena(idTrase, najava, trenutniKoraci, true);

                DataTable dt = insert.UcitajPlanVremena(najava, idTrase);
                dataGridView1.DataSource = dt;
                trenutniKoraci = insert.UcitajKorakeVremena(najava, idTrase);

                SrediGrid();
                MessageBox.Show("Plan vremena je izračunat i upisan u bazu.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri izračunavanju vremena:" + Environment.NewLine + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int najava = VratiIntIzCombo(cboNajava.SelectedValue);
            int idTrase = VratiIntIzCombo(cboTrasa.SelectedValue);

            if (najava <= 0)
            {
                MessageBox.Show("Izaberite najavu.");
                return;
            }

            if (idTrase <= 0)
            {
                MessageBox.Show("Izaberite trasu.");
                return;
            }

            try
            {
                DataTable dt = insert.UcitajPlanVremena(najava, idTrase);

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Za izabranu najavu i trasu ne postoji prethodno izračunat plan.");
                    dataGridView1.DataSource = null;
                    trenutniKoraci = new List<PepeoKorakVremena>();
                    return;
                }

                dataGridView1.DataSource = dt;
                trenutniKoraci = insert.UcitajKorakeVremena(najava, idTrase);
                SrediGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri učitavanju postojećeg plana:" + Environment.NewLine + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Izaberite red koji želite da izmenite.");
                return;
            }

            int idTrase = VratiIntIzCombo(cboTrasa.SelectedValue);
            int najava = VratiIntIzCombo(cboNajava.SelectedValue);
            int id = VratiIntIzCelije(dataGridView1.CurrentRow.Cells["ID"].Value);

            if (id <= 0)
            {
                MessageBox.Show("Nije pronađen ID selektovanog reda.");
                return;
            }

            try
            {
                trenutniKoraci = insert.UcitajKorakeVremena(najava, idTrase);
                int indeks = trenutniKoraci.FindIndex(x => x.ID == id);

                if (indeks < 0)
                {
                    MessageBox.Show("Selektovani red nije pronađen u učitanom planu.");
                    return;
                }

                DateTime novoStvarnoVreme = dateTimePicker2.Value;
                string napomena = textBox1.Text;

                List<PepeoSablonOperacije> sabloni = insert.UcitajSabloneOperacija();
                Dictionary<string, string> parametri = insert.UcitajParametrePravila();
                PepeoKalkulatorVremena kalkulator = new PepeoKalkulatorVremena(sabloni, parametri);

                kalkulator.PreracunajPosleRucneIzmene(trenutniKoraci, indeks, novoStvarnoVreme, napomena);
                insert.AzurirajKorakePosleIzmene(trenutniKoraci, indeks, id, novoStvarnoVreme, napomena);

                DataTable dt = insert.UcitajPlanVremena(najava, idTrase);
                dataGridView1.DataSource = dt;
                trenutniKoraci = insert.UcitajKorakeVremena(najava, idTrase);
                SrediGrid();

                MessageBox.Show("Izmena je upisana, a svi sledeći koraci su preračunati.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri primeni kašnjenja:" + Environment.NewLine + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
                return;

            if (dataGridView1.Columns.Contains("StvarnoVreme") && dataGridView1.CurrentRow.Cells["StvarnoVreme"].Value != DBNull.Value)
            {
                dateTimePicker2.Value = Convert.ToDateTime(dataGridView1.CurrentRow.Cells["StvarnoVreme"].Value);
            }
            else if (dataGridView1.Columns.Contains("PlaniranoVreme") && dataGridView1.CurrentRow.Cells["PlaniranoVreme"].Value != DBNull.Value)
            {
                dateTimePicker2.Value = Convert.ToDateTime(dataGridView1.CurrentRow.Cells["PlaniranoVreme"].Value);
            }
        }

        private int VratiIntIzCombo(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0;

            if (value is DataRowView)
                return 0;

            int result;
            if (int.TryParse(value.ToString(), out result))
                return result;

            return 0;
        }

        private int VratiIntIzCelije(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0;

            int result;
            if (int.TryParse(value.ToString(), out result))
                return result;

            return 0;
        }

        private void SrediGrid()
        {
            DataGridView dgv = dataGridView1;

            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            SakrijKolonu("IDTrase");
            SakrijKolonu("Stanica");
            SakrijKolonu("UlazakUStanicu");
            SakrijKolonu("SifraKoraka");
            SakrijKolonu("RucnoIzmenjeno");

            PostaviNazivKolone("ID", "ID");
            PostaviNazivKolone("Ciklus", "Ciklus");
            PostaviNazivKolone("RB", "RB");
            PostaviNazivKolone("Najava", "Najava");
            PostaviNazivKolone("StanicaOpis", "Stanica");
            PostaviNazivKolone("Operacija", "Operacija");
            PostaviNazivKolone("TipDogadjaja", "Tip događaja");
            PostaviNazivKolone("PlaniraniPocetak", "Planirani početak");
            PostaviNazivKolone("PlaniranoVreme", "Planirani kraj/vreme");
            PostaviNazivKolone("StvarnoVreme", "Stvarno vreme");
            PostaviNazivKolone("KasnjenjeMin", "Kašnjenje min");
            PostaviNazivKolone("Napomena", "Napomena");

            FormatirajDatum("PlaniraniPocetak");
            FormatirajDatum("PlaniranoVreme");
            FormatirajDatum("StvarnoVreme");

            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
        }

        private void SakrijKolonu(string nazivKolone)
        {
            if (dataGridView1.Columns.Contains(nazivKolone))
                dataGridView1.Columns[nazivKolone].Visible = false;
        }

        private void PostaviNazivKolone(string nazivKolone, string tekst)
        {
            if (dataGridView1.Columns.Contains(nazivKolone))
                dataGridView1.Columns[nazivKolone].HeaderText = tekst;
        }

        private void FormatirajDatum(string nazivKolone)
        {
            if (dataGridView1.Columns.Contains(nazivKolone))
                dataGridView1.Columns[nazivKolone].DefaultCellStyle.Format = "dd.MM.yyyy HH:mm";
        }
    }
}
