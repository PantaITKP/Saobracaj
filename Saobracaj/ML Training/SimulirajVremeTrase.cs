using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Saobracaj.ML_Training
{
    public partial class SimulirajVremeTrase : Form
    {
        string connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"].ConnectionString;
        private int selectedTrasa;

        public SimulirajVremeTrase()
        {
            InitializeComponent();
            FillCombo();
        }

        private void FillCombo()
        {
            var select = "SELECT DISTINCT RN FROM AIUsvojenaVremenaTrase";
            var conn = new SqlConnection(connection);
            var da = new SqlDataAdapter(select, conn);
            var ds = new System.Data.DataSet();
            da.Fill(ds);
            comboBox1.DataSource = ds.Tables[0];
            comboBox1.DisplayMember = "RN";
            comboBox1.ValueMember = "RN";
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedValue == null) return;
            RefreshGV();
        }

        private void RefreshGV()
        {
            int rn = Convert.ToInt32(comboBox1.SelectedValue);

            var conn = new SqlConnection(connection);
            conn.Open();

            // Load Trasa for the selected RN
            var cmdTrasa = new SqlCommand("SELECT TOP 1 Trasa FROM AIUsvojenaVremenaTrase WHERE RN = @RN", conn);
            cmdTrasa.Parameters.AddWithValue("@RN", rn);
            int trasa = Convert.ToInt32(cmdTrasa.ExecuteScalar());

            // Load stations for that Trasa (ordered)
            var stationsCmd = new SqlCommand(@"
        SELECT ts.IDStanice, s.Opis
        FROM TrasaStanice ts
        JOIN stanice s ON s.ID = ts.IDStanice
        WHERE ts.IDTrase = @Trasa
        ORDER BY ts.RB", conn);
            stationsCmd.Parameters.AddWithValue("@Trasa", trasa);

            var stationsReader = stationsCmd.ExecuteReader();
            var stations = new List<(int IDStanice, string Naziv)>();
            while (stationsReader.Read())
            {
                stations.Add((
                    Convert.ToInt32(stationsReader["IDStanice"]),
                    stationsReader["Opis"].ToString()
                ));
            }
            stationsReader.Close();

            // Load already inserted times from AIPutniList
            var existingCmd = new SqlCommand("SELECT Stanica, Datum FROM AIPutniList WHERE BrojRN = @RN", conn);
            existingCmd.Parameters.AddWithValue("@RN", rn);

            var existingReader = existingCmd.ExecuteReader();
            var insertedTimes = new Dictionary<int, DateTime>();
            while (existingReader.Read())
            {
                insertedTimes[Convert.ToInt32(existingReader["Stanica"])] = Convert.ToDateTime(existingReader["Datum"]);
            }
            existingReader.Close();

            // Load first scheduled Datum from AIUsvojenaVremenaTrase
            var getStartTime = new SqlCommand(@"
        SELECT TOP 1 Datum
        FROM AIUsvojenaVremenaTrase
        WHERE RN = @RN
        ORDER BY Vreme", conn);
            getStartTime.Parameters.AddWithValue("@RN", rn);
            DateTime? firstDatum = getStartTime.ExecuteScalar() as DateTime?;

            // Fill DataGridView
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
            dataGridView1.Columns.Add("Stanica", "Stanica");
            dataGridView1.Columns.Add("Naziv", "Naziv");
            dataGridView1.Columns.Add("Datum", "Datum");
            dataGridView1.Columns["Datum"].Width = 170;
            dataGridView1.Columns["Datum"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            for (int i = 0; i < stations.Count; i++)
            {
                var (id, naziv) = stations[i];
                string datumStr = "";

                if (insertedTimes.ContainsKey(id))
                {
                    datumStr = insertedTimes[id].ToString("yyyy-MM-dd HH:mm:ss");
                }
                else if (i == 0 && firstDatum.HasValue)
                {
                    // First station, not inserted yet — use scheduled time
                    datumStr = firstDatum.Value.ToString("yyyy-MM-dd HH:mm:ss");
                }

                int rowIndex = dataGridView1.Rows.Add(id, naziv, datumStr);

                if (insertedTimes.ContainsKey(id) || (i == 0 && firstDatum.HasValue))
                    dataGridView1.Rows[rowIndex].ReadOnly = true;
            }

            this.selectedTrasa = trasa;
        }
        private void InsertPrva()
        {
            // ✅ Insert first station automatically if it's missing
            using (var conn2 = new SqlConnection(connection))
            {
                conn2.Open();

                var checkFirst = new SqlCommand(@"
        SELECT TOP 1 p.Stanica, p.Datum, p.Trasa
        FROM AIUsvojenaVremenaTrase p
        WHERE p.RN = @RN
        ORDER BY p.Vreme", conn2);
                checkFirst.Parameters.AddWithValue("@RN", comboBox1.SelectedValue);

                using (var reader = checkFirst.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int firstStanica = Convert.ToInt32(reader["Stanica"]);
                        DateTime firstDatum = Convert.ToDateTime(reader["Datum"]);
                        int trasa = Convert.ToInt32(reader["Trasa"]);

                        reader.Close();

                        // Check if it exists in AIPutniList
                        var exists = new SqlCommand(@"
                SELECT COUNT(*) 
                FROM AIPutniList 
                WHERE BrojRN = @RN AND Stanica = @Stanica", conn2);
                        exists.Parameters.AddWithValue("@RN", comboBox1.SelectedValue);
                        exists.Parameters.AddWithValue("@Stanica", firstStanica);
                        int count = (int)exists.ExecuteScalar();

                        if (count == 0)
                        {
                            var insert = new SqlCommand("InsertAIPutniListAndMLData", conn2);
                            insert.CommandType = System.Data.CommandType.StoredProcedure;
                            insert.Parameters.AddWithValue("@BrojRN", comboBox1.SelectedValue);
                            insert.Parameters.AddWithValue("@Lokomotiva", "simulacija");
                            insert.Parameters.AddWithValue("@Zaposleni", 0);
                            insert.Parameters.AddWithValue("@Trasa", trasa);
                            insert.Parameters.AddWithValue("@Stanica", firstStanica);
                            insert.Parameters.AddWithValue("@DatumOverride", firstDatum);
                            insert.ExecuteNonQuery();
                        }
                    }
                }
            }

        }
        private void button1_Click(object sender, EventArgs e)
        {
            InsertPrva();


            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Izaberite stanicu.");
                return;
            }

            var row = dataGridView1.CurrentRow;

            if (row.ReadOnly || !string.IsNullOrWhiteSpace(row.Cells["Datum"].Value?.ToString()))
            {
                MessageBox.Show("Vreme za ovu stanicu je već uneto.");
                return;
            }

            int stanica = Convert.ToInt32(row.Cells["Stanica"].Value);
            DateTime currentTime = dateTimePicker1.Value;
            int rn = Convert.ToInt32(comboBox1.SelectedValue);

            using (var conn = new SqlConnection(connection))
            {
                conn.Open();

                // ✅ Step 1: Check if any record already exists for this RN
                var checkCmd = new SqlCommand("SELECT COUNT(*) FROM AIPutniList WHERE BrojRN = @RN", conn);
                checkCmd.Parameters.AddWithValue("@RN", rn);
                int count = (int)checkCmd.ExecuteScalar();

                // ✅ Step 2: Insert
                var insert = new SqlCommand("InsertAIPutniListAndMLData", conn);
                insert.CommandType = System.Data.CommandType.StoredProcedure;
                insert.Parameters.AddWithValue("@BrojRN", rn);
                insert.Parameters.AddWithValue("@Lokomotiva", "simulacija");
                insert.Parameters.AddWithValue("@Zaposleni", 0);
                insert.Parameters.AddWithValue("@Trasa", selectedTrasa);
                insert.Parameters.AddWithValue("@Stanica", stanica);
                insert.Parameters.AddWithValue("@DatumOverride", currentTime);
                insert.ExecuteNonQuery();
            }

            // ✅ Step 3: Update UI
            row.Cells["Datum"].Value = currentTime.ToString("yyyy-MM-dd HH:mm:ss");
            row.ReadOnly = true;

            MessageBox.Show("Vreme uspešno upisano!");
            RefreshGV();
        }

    }
}
