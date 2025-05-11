using Microsoft.ReportingServices.Diagnostics.Internal;
using Syncfusion.Windows.Forms.Diagram;
using Syncfusion.Windows.Forms.Grid;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Saobracaj.ML_Training
{
    public partial class PutniListML : Form
    {
        string connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"].ConnectionString;
        public PutniListML()
        {
            InitializeComponent();
            FillCombo();
            string modelPath = "arrival_model.zip";
            if (!File.Exists(modelPath))
            {
                MessageBox.Show("Model nije pronadjen. Trenirajte model prvo!");
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {

        }
        private void FillCombo()
        {
            var select = "select Trase.ID as ID,(RTrim(s1.Opis)+'-'+RTrim(s2.Opis)) as Opis From Trase inner join stanice as s1 on Trase.Pocetna=s1.ID inner join stanice as s2 on Trase.Krajnja=s2.ID Where Trase.ID in (1964,1916,1337,681)";
            SqlConnection conn = new SqlConnection(connection);
            var da = new SqlDataAdapter(select, conn);
            var ds = new System.Data.DataSet();
            da.Fill(ds);
            comboBox1.DataSource = ds.Tables[0];
            comboBox1.DisplayMember = "Opis";
            comboBox1.ValueMember = "ID";

            var select2 = "select distinct BrojRN as RN from AIPutniList";
            var da2=new SqlDataAdapter(select2, conn);
            var ds2=new System.Data.DataSet();
            da2.Fill(ds2);
            cboRN.DataSource = ds2.Tables[0];
            cboRN.DisplayMember = "RN";
            cboRN.ValueMember = "RN";

        }

        private void button2_Click(object sender, EventArgs e)
        {
            var data = RealVsPredictedLoader.LoadRealVsPredicted(connection, Convert.ToInt32(cboRN.SelectedValue));

            dgvCompare.Columns.Clear();
            dgvCompare.Rows.Clear();

            dgvCompare.Columns.Add("Stanica", "Stanica");
            dgvCompare.Columns.Add("Real", "Real");
            dgvCompare.Columns.Add("Predicted", "Predicted");
            dgvCompare.Columns.Add("Difference", "Difference (sec)");

            foreach (var (stanica, real, predicted, diff) in data)
            {
                dgvCompare.Rows.Add(
                    stanica,
                    real.ToString(@"hh\:mm\:ss"),
                    predicted.ToString(@"hh\:mm\:ss"),
                    diff
                );
            }

        }

        private void trenirajModelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var data = DataLoader.Load(connection);
            ModelTrainer.Train(data);
            MessageBox.Show("Model treniran i sacuvan!");
        }

        private void predvidiVremePutaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DateTime start = dateTimePicker1.Value;
            float startHour = (float)start.TimeOfDay.TotalHours;

            int trasa = Convert.ToInt32(comboBox1.SelectedValue);
            var stanice = RouteStationLoader.LoadStations(connection, trasa);
            var redosledi = RouteStationLoader.LoadStationOrder(connection, trasa);
            var stationNames = RouteStationLoader.LoadStationNames(connection);


            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.Columns.Add("Stanica", "Stanica");
            dataGridView1.Columns.Add("StanicaNaziv", "Naziv");
            dataGridView1.Columns.Add("Datum", "Datum");
            dataGridView1.Columns.Add("VremeDolaska", "VremeDolaska");

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            var stationPredictions = new List<(int Stanica, DateTime Arrival, TimeSpan Delta)>();

            // First station (starting point)
            int firstStanica = stanice[0];
            stationPredictions.Add((firstStanica, start, TimeSpan.Zero));

            // Predict the rest
            foreach (var stanica in stanice.Skip(1))  // skip the start station
            {
                int redosled = redosledi.ContainsKey(stanica) ? redosledi[stanica] : 0;
                float predictedSeconds = ModelTrainer.Predict(trasa, stanica, startHour, redosled);
                DateTime arrival = start.AddSeconds(predictedSeconds);
                TimeSpan delta = TimeSpan.FromSeconds(predictedSeconds);

                stationPredictions.Add((stanica, arrival, delta));
            }

            // Sort by route order
            stationPredictions = stationPredictions
                .OrderBy(sp => redosledi.ContainsKey(sp.Stanica) ? redosledi[sp.Stanica] : 999)
                .ToList();

            // Display
            foreach (var sp in stationPredictions)
            {
                string naziv = stationNames.ContainsKey(sp.Stanica) ? stationNames[sp.Stanica] : "???";

                dataGridView1.Rows.Add(
                    sp.Stanica,
                    naziv,
                    sp.Arrival.ToString("yyyy-MM-dd HH:mm:ss"),
                    sp.Delta.ToString(@"hh\:mm\:ss")
                );
            }
        }

        private void btnUsvoji_Click(object sender, EventArgs e)
        {
            int trasa = Convert.ToInt32(comboBox1.SelectedValue);
            DateTime datum = dateTimePicker1.Value;

            if (!int.TryParse(txtRN.Text, out int rn))
            {
                MessageBox.Show("Unesite validan broj RN.");
                return;
            }

            var conn = new SqlConnection(connection);
            conn.Open();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                int stanica = Convert.ToInt32(row.Cells["Stanica"].Value);
                string vremeStr = row.Cells["VremeDolaska"].Value?.ToString();

                if (!TimeSpan.TryParse(vremeStr, out TimeSpan vreme)) continue;

                //Datum for this row = start time + VremeDolaska
                DateTime arrivalTime = datum.Add(vreme);

                var cmd = new SqlCommand(@"
        INSERT INTO AIUsvojenaVremenaTrase (Trasa, Datum, RN, Stanica, Vreme)
        VALUES (@Trasa, @Datum, @RN, @Stanica, @Vreme)", conn);

                cmd.Parameters.AddWithValue("@Trasa", trasa);
                cmd.Parameters.AddWithValue("@Datum", arrivalTime);   // ✅ Real arrival datetime
                cmd.Parameters.AddWithValue("@RN", rn);
                cmd.Parameters.AddWithValue("@Stanica", stanica);
                cmd.Parameters.AddWithValue("@Vreme", vreme);

                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Uspešno sačuvano u bazu!");
        }

        private void simulirajTrasuToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SimulirajVremeTrase frm = new SimulirajVremeTrase();
            frm.Show();
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FillCombo();
        }
    }
}
