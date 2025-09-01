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
using System.Windows.Forms.DataVisualization.Charting;

namespace Saobracaj.MLProd
{
    public partial class AnalizaVozova : Form
    {
        private string connection = ConfigurationManager
            .ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"]
            .ConnectionString;

        public AnalizaVozova()
        {
            InitializeComponent();

            // podrazumevani period za Chart1 (poslednjih 30 dana)
            if (dtpOd != null && dtpDo != null)
            {
                dtpDo.Value = DateTime.Today;
                dtpOd.Value = DateTime.Today.AddDays(-30);
            }

            LoadDGV();               // puni grid + chart2 (RN po lokomotivi)
            LoadChart1FromPickers(); // puni chart1 (sati rada iz PutniList)
        }

        // Helper: izvršava upit kao "SET ARITHABORT ON; <sql>"
        private DataTable ExecDT(string sql, Action<SqlCommand> addParams = null)
        {
            using (var cn = new SqlConnection(connection))
            using (var cmd = new SqlCommand("SET ARITHABORT ON; " + sql, cn))
            using (var da = new SqlDataAdapter(cmd))
            {
                addParams?.Invoke(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private void LoadDGV()
        {
            string sql = @"
;WITH Tally AS (
    SELECT TOP (2000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects
),
DigitsPerRow AS (
    SELECT 
        rn.ID                           AS RNID,
        rn.StatusRN,
        RTRIM(n.PrevozniPut)            AS Relacija,
        (
            SELECT '' + SUBSTRING(rnlt.SmSifra, t.n, 1)
            FROM Tally t
            WHERE t.n <= LEN(rnlt.SmSifra)
              AND SUBSTRING(rnlt.SmSifra, t.n, 1) LIKE '[0-9]'
            FOR XML PATH(''), TYPE
        ).value('.','nvarchar(200)')     AS LokomotivaDigits
    FROM RadniNalog rn
    JOIN RadniNalogLokNaTrasi rnlt ON rn.ID = rnlt.IDRadnogNaloga
    JOIN Najava n ON rn.TehnologijaID = n.ID
    WHERE rn.StatusRN = 'PL'
),
AggByRN AS (
    SELECT
        d.RNID,
        StatusRN = MIN(d.StatusRN),
        Relacija = MIN(d.Relacija),
        Lokomotiva = ISNULL(STUFF((
            SELECT DISTINCT ',' + d2.LokomotivaDigits
            FROM DigitsPerRow d2
            WHERE d2.RNID = d.RNID AND ISNULL(d2.LokomotivaDigits,'') <> ''
            FOR XML PATH(''), TYPE
        ).value('.','nvarchar(max)'), 1, 1, ''), '')
    FROM DigitsPerRow d
    GROUP BY d.RNID
)
SELECT 
    RNID        AS ID,
    Lokomotiva,
    StatusRN,
    Relacija
FROM AggByRN
ORDER BY RNID;";

            var dt = ExecDT(sql);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = dt;

            // stil
            dataGridView1.BorderStyle = BorderStyle.Fixed3D;
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.LightGray;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.DarkTurquoise;
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.WhiteSmoke;
            dataGridView1.BackgroundColor = Color.FromArgb(125, 158, 192);
            dataGridView1.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Sunken;
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.DarkOrange;
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;

            BuildChart2FromGrid(dt);
        }

        // Chart2: broj jedinstvenih RN po lokomotivi (StatusRN='PL')
        private void BuildChart2FromGrid(DataTable dt)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (DataRow r in dt.Rows)
            {
                string locoField = Convert.ToString(r["Lokomotiva"]);
                if (string.IsNullOrWhiteSpace(locoField)) continue;

                // jedna RN može imati više lokomotiva (zarezom odvojene)
                var perRowLocos = locoField
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var loco in perRowLocos)
                {
                    counts.TryGetValue(loco, out int val);
                    counts[loco] = val + 1;  // 1 RN = +1 za tu lokomotivu
                }
            }

            chart2.Series.Clear();
            chart2.ChartAreas.Clear();
            chart2.Titles.Clear();

            var area = new ChartArea("area_rn");
            area.AxisX.Interval = 1;
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            area.AxisX.Title = "Lokomotiva";
            area.AxisY.Title = "Broj jedinstvenih RN (PL)";
            chart2.ChartAreas.Add(area);

            var seriesRN = new Series("BrojRN");
            seriesRN.ChartType = SeriesChartType.Column;
            seriesRN.XValueType = ChartValueType.String;
            seriesRN.YValueType = ChartValueType.Int32;
            seriesRN.IsValueShownAsLabel = true;
            chart2.Series.Add(seriesRN);

            foreach (var kv in counts.OrderByDescending(k => k.Value).ThenBy(k => k.Key))
            {
                seriesRN.Points.AddXY(kv.Key, kv.Value);
            }

            chart2.Titles.Add("Planirane vožnje po lokomotivi (StatusRN = PL)");
        }

        // ===================== CHART1: sati rada po lokomotivi (PutniList) =====================

        private DataTable FetchLocoWorkMinutesFromPutniList(DateTime fromIncl, DateTime toExcl)
        {
            string sql = @"
WITH Base AS (
    SELECT
        BrojRN,
        CAST(Lokomotiva AS NVARCHAR(64)) AS Lokomotiva,
        Datum
    FROM PutniList
    WHERE Datum IS NOT NULL
      AND Datum > '1900-01-01'
      AND Datum >= @from
      AND Datum <  @to
),
Agg AS (
    SELECT
        BrojRN,
        MAX(Lokomotiva)                          AS Lokomotiva,
        MIN(Datum)                               AS StartTime,
        MAX(Datum)                               AS EndTime,
        DATEDIFF(MINUTE, MIN(Datum), MAX(Datum)) AS DurationMin
    FROM Base
    GROUP BY BrojRN
)
SELECT
    Lokomotiva,
    SUM(DurationMin) AS Minutes,
    CAST(SUM(DurationMin)/60.0 AS DECIMAL(18,2)) AS DurationHours
FROM Agg
WHERE DurationMin > 0
  AND ISNULL(Lokomotiva,'') <> ''
GROUP BY Lokomotiva
ORDER BY Lokomotiva;";

            return ExecDT(sql, cmd =>
            {
                cmd.Parameters.AddWithValue("@from", fromIncl);
                cmd.Parameters.AddWithValue("@to", toExcl);
            });
        }

        private void BuildChart1FromPutniList(DataTable dt)
        {
            chart1.Series.Clear();
            chart1.ChartAreas.Clear();
            chart1.Titles.Clear();

            var area = new ChartArea("pl_area");
            area.AxisX.Interval = 1;
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.Title = "Lokomotiva";
            area.AxisY.Title = "Sati rada (period)";
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            chart1.ChartAreas.Add(area);

            var seriesHours = new Series("SatiRada");
            seriesHours.ChartType = SeriesChartType.Column;
            seriesHours.IsValueShownAsLabel = true;
            seriesHours.XValueType = ChartValueType.String;
            seriesHours.YValueType = ChartValueType.Double;
            chart1.Series.Add(seriesHours);

            foreach (DataRow r in dt.Rows)
            {
                string loco = Convert.ToString(r["Lokomotiva"]);
                double hours = Convert.ToDouble(r["DurationHours"]);
                seriesHours.Points.AddXY(loco, hours);
            }

            chart1.Titles.Add("Sati rada po lokomotivi (PutniList)");
        }

        private void LoadChart1FromPickers()
        {
            if (dtpOd == null || dtpDo == null) return;

            var from = dtpOd.Value.Date;
            var toExcl = dtpDo.Value.Date.AddDays(1); // uključivo do kraja dana

            var dt = FetchLocoWorkMinutesFromPutniList(from, toExcl);
            BuildChart1FromPutniList(dt);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            LoadChart1FromPickers();
        }
    }
}
