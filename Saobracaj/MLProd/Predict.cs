using Microsoft.Office.Interop.Excel;
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
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using DataTable = System.Data.DataTable;
using Label = System.Windows.Forms.Label;

namespace Saobracaj.MLProd
{
    public partial class Predict : Form
    {
        string connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"].ConnectionString;
        private CancellationTokenSource cts;
        private readonly string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "global_segment_model.zip");
        private EtaService _eta;
        private bool _mapReady = false;
        private string _mapHtmlPath;
        private bool _includeActivitiesNextPrediction = false;

        public Predict()
        {
            InitializeComponent();
            TryInitEtaService();
            FillCombo();
            panel1.Visible = false;
            InitMap();
            DesignGV();
            panel2.Visible = false;
            FillChkList();
        }
        private void InitMap()
        {
            try
            {
                _mapHtmlPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MapML.html");
                if (!System.IO.File.Exists(_mapHtmlPath))
                {
                    MessageBox.Show("MapML.html nije pronađen pored exe-a.", "Mapa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                webBrowser1.ScriptErrorsSuppressed = true;
                webBrowser1.DocumentCompleted += (s, e) => { _mapReady = true; };
                webBrowser1.Navigate(_mapHtmlPath);
            }
            catch { /* ignore */ }
        }
        private void TryInitEtaService()
        {
            try
            {
                _eta = new EtaService(connection, modelPath);
            }
            catch
            {
                // ako model još ne postoji, _eta ostaje null; korisnik može pokrenuti retreniranje iz menija
                _eta = null;
            }
        }
        private void FillCombo()
        {
            var select = "select IDRadnogNaloga as ID,Cast(IDRadnogNaloga as nvarchar(10))+' / '+RTrim(s1.Opis)+' - '+RTrim(s2.Opis) as Trasa " +
                "From RadniNalogTrase " +
                "inner join RadniNalog on RadniNalogTrase.IDRadnogNaloga=RadniNalog.ID " +
                "inner join Stanice as s1 on RadniNalogTrase.StanicaOd=s1.ID " +
                "inner join Stanice as s2 on RadniNalogTrase.StanicaDo=s2.ID " +
                "Where StatusRN='OD' or StatusRN='PL' order by IDRadnogNaloga desc";
            SqlConnection conn = new SqlConnection(connection);
            var da = new SqlDataAdapter(select, conn);
            var ds = new System.Data.DataSet();
            da.Fill(ds);
            comboBox1.DataSource = ds.Tables[0];
            comboBox1.DisplayMember = "Trasa";
            comboBox1.ValueMember = "ID";
        }
        private string ResolveStanicaName(int id)
        {
            SqlConnection conn = new SqlConnection(connection);
            conn.Open();
            SqlCommand cmd = new SqlCommand("SELECT Opis FROM Stanice WHERE ID=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            var o = cmd.ExecuteScalar();
            return o == null ? id.ToString() : o.ToString();
        }
        private System.Data.DataTable LoadSegmentPlan(int rn)
        {
            using (var cn = new SqlConnection(connection))
            {
                cn.Open();
                using (var da = new SqlDataAdapter(
                    "SELECT RBTrase, RBFrom, StanicaFromID, StanicaToID FROM dbo.fn_Segments_For_RN(@rn) ORDER BY RBTrase, RBFrom", cn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@rn", rn);
                    var dt = new System.Data.DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }
        private void PredvidiVremena()
        {
            if (comboBox1.SelectedValue == null)
            {
                MessageBox.Show("Izaberi BrojRN u padajućoj listi.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_eta == null)
            {
                MessageBox.Show("Model nije učitan. Pokreni 'Retreniraj' iz menija.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int rn = Convert.ToInt32(comboBox1.SelectedValue);

            try
            {
                // 1) ETA + verovatnoća zaustavljanja
                var (chain, start, final) = _eta.PredictChainForRN_WithStops(rn, 0.5f);
                _lastRn = rn;
                _lastStart = start;
                _lastEtaWithStops = final;

                // paralelno izračunaj ETA bez zaustavljanja (prag > 1 => praktično "nema stajanja")
                var (_, __, finalNoStops) = _eta.PredictChainForRN_WithStops(rn, 1.1f);
                _lastEtaNoStops = finalNoStops;

                // 2) Plan segmenata → mapiranje za RBStanice
                var plan = LoadSegmentPlan(rn);
                // Mapiranje: (RBTrase, StanicaToID) -> RBStanice (RBFrom+1)
                var mapToOrder = new Dictionary<string, int>(); // ključ "rbTrase|stanicaTo"
                                                                // Takođe pronađi "početnu" stanicu RBTrase=1 (RBFrom=1)
                int? firstStationId = null;
                foreach (DataRow r in plan.Rows)
                {
                    int rbTrase = Convert.ToInt32(r["RBTrase"]);
                    int rbFrom = Convert.ToInt32(r["RBFrom"]);
                    int stFrom = Convert.ToInt32(r["StanicaFromID"]);
                    int stTo = Convert.ToInt32(r["StanicaToID"]);

                    // upiši destinacionu stanicu rednog broja RBFrom+1
                    string key = rbTrase.ToString() + "|" + stTo.ToString();
                    mapToOrder[key] = rbFrom + 1;

                    // uhvati početnu stanicu (RBTrase=1, RBFrom=1)
                    if (rbTrase == 1 && rbFrom == 1)
                        firstStationId = stFrom;
                }

                // 3) Napuni grid
                var dt1 = new System.Data.DataTable();
                dt1.Columns.Add("RB Trase", typeof(int));
                dt1.Columns.Add("RB Stanice", typeof(int));
                dt1.Columns.Add("Stanica", typeof(string));
                dt1.Columns.Add("P(stop)", typeof(string));     // npr. "23 %"
                dt1.Columns.Add("Razlog", typeof(string));
                dt1.Columns.Add("Čekanje [min]", typeof(double));
                dt1.Columns.Add("ETA", typeof(DateTime));

                // dodaj "početnu" stanicu (ako postoji) kao prvi red
                if (firstStationId.HasValue)
                {
                    var r0 = dt1.NewRow();
                    r0["RB Trase"] = 1;
                    r0["RB Stanice"] = 1;
                    r0["Stanica"] = ResolveStanicaName(firstStationId.Value);
                    r0["P(stop)"] = "0 %";
                    r0["Razlog"] = "";
                    r0["Čekanje [min]"] = 0.0;
                    r0["ETA"] = start;   // polazno vreme
                    dt1.Rows.Add(r0);
                }

                // zatim destinacione stanice iz chain-a
                foreach (var item in chain)
                {
                    int rbTrase = item.rbTrase;
                    int stanicaId = item.stanicaTo;
                    DateTime eta = item.eta;
                    float pStop = item.pStop;
                    string reason = item.reason ?? "";
                    double wait = Math.Round(item.waitMin, 1);

                    int rbStanice = 0;
                    string k = rbTrase.ToString() + "|" + stanicaId.ToString();
                    if (mapToOrder.TryGetValue(k, out int ord))
                        rbStanice = ord;

                    var r = dt1.NewRow();
                    r["RB Trase"] = rbTrase;
                    r["RB Stanice"] = rbStanice;
                    r["Stanica"] = ResolveStanicaName(stanicaId);
                    r["P(stop)"] = (pStop * 100f).ToString("0.#") + " %";
                    r["Razlog"] = reason;
                    r["Čekanje [min]"] = wait;
                    r["ETA"] = eta;
                    dt1.Rows.Add(r);
                }

                dataGridView1.DataSource = dt1;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

                // 4) Preporuke — sada odvojeno: dataGridView2 (mašinovođa), dataGridView3 (lokomotiva)
                BuildRecommendationsGrids(rn, start, final);
                UpdateSummaryPanel(rn, start, final);
                panel1.Visible = true;
                UpdateMapFromPrediction(start, chain);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška u predikciji: " + ex.Message, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void predvidiVremenaToolStripMenuItem_Click(object sender, EventArgs e)
        {

            var ans = MessageBox.Show(
        "Želite li da uključite aktivnosti (prijem/predaja) u predviđanje?",
        "Uključiti aktivnosti?",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question);

            if (ans == DialogResult.Yes)
            {
                // Otvori panel za izbor aktivnosti; proračun ćemo pokrenuti tek nakon potvrde
                _includeActivitiesNextPrediction = true;
                panel2.Visible = true;
                panel2.BringToFront();
                return; // čekamo klik na 'Potvrdi aktivnosti'
            }
            else if (ans == DialogResult.No)
            {
                // Standardni flow – bez aktivnosti
                PredvidiVremena();
                return;
            }
        }
        public async void Retrain()
        {
            var dlg = new ProgressDialog("Priprema...");
            dlg.Show(this);

            cts = new CancellationTokenSource();

            string session = TrainLogger.StartSession();           // <<< NOVO: start sesije
            TrainLogger.Info($"ModelPath={modelPath}");            // gde snimamo

            var progress = new Progress<TrainProgress>(p =>
            {
                dlg.SetProgress(p.Percent);
                dlg.SetText(string.IsNullOrWhiteSpace(p.Message) ? p.Stage : $"{p.Stage} — {p.Message}");
                TrainLogger.Progress(p);                           // <<< NOVO: loguj svaku fazu i % napretka
            });

            try
            {
                TrainLogger.Info("Time model: training started");
                var metricsTime = await GlobalTrainer.TrainAndSaveAsync(connection, modelPath, progress, cts.Token);
                TrainLogger.Info("Time model: training finished: " + metricsTime);

                // Ako koristiš i stop-modele (StopTrainer) — ostavi; ako ne, ove 4 linije možeš obrisati.
                var binPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stop_bin.zip");
                var reasonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stop_reason.zip");
                var waitPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stop_wait.zip");
                try
                {
                    TrainLogger.Info("Stop models: training started");
                    var (binM, reasonM, waitM) = await StopTrainer.TrainAllAsync(connection, binPath, reasonPath, waitPath, progress, cts.Token);
                    TrainLogger.Info($"Stop models: finished | {binM} | {waitM}");
                }
                catch (Exception exStop)
                {
                    // Ako hoćeš da stop-modeli budu “best effort”, loguj grešku ali dozvoli da vreme-model stoji
                    TrainLogger.Error("Stop models: FAILED", exStop);
                }

                // reload servis
                _eta = new EtaService(connection, modelPath);

                dlg.SetProgress(100);
                dlg.SetText("Trening gotov.");
                TrainLogger.Info("Retraining: SUCCESS");
            }
            catch (OperationCanceledException oce)
            {
                dlg.SetText("Trening otkazan.");
                TrainLogger.Warn("Retraining: CANCELED — " + oce.Message);
            }
            catch (Exception ex)
            {
                dlg.SetText("Greška: " + ex.Message);
                TrainLogger.Error("Retraining: FAILED", ex);
            }
            finally
            {
                await Task.Delay(800);
                dlg.Close();
                TrainLogger.EndSession(session);                   // <<< NOVO: kraj sesije
            }
        }

        private void retrenirajModelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Retrain();
        }

        private void prekiniTreningToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                cts?.Cancel();
                MessageBox.Show("Prekid u toku (po završetku trenutne iteracije).", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch { /* ignore */ }
        }
        private void BuildRecommendationsGrids(int rn, DateTime start, DateTime final)
        {
            // 1) Planirani A->B parovi
            var plannedPairs = new DataTable();
            using (var cn = new SqlConnection(connection))
            {
                cn.Open();
                using (var da = new SqlDataAdapter(
                    "SELECT DISTINCT StanicaFromID, StanicaToID FROM dbo.fn_Segments_For_RN(@rn)", cn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@rn", rn);
                    da.Fill(plannedPairs);
                }
            }
            if (plannedPairs.Rows.Count == 0)
            {
                dataGridView2.DataSource = null;
                dataGridView3.DataSource = null;
                return;
            }

            // (A,B) uslov: ( (A=... AND B=...) OR ... )
            var sb = new StringBuilder();
            foreach (DataRow pr in plannedPairs.Rows)
            {
                if (sb.Length > 0) sb.Append(" OR ");
                sb.AppendFormat("(g.StanicaFromID={0} AND g.StanicaToID={1})",
                    Convert.ToInt32(pr["StanicaFromID"]),
                    Convert.ToInt32(pr["StanicaToID"]));
            }
            string wherePairs = sb.ToString();

            // zajednički CTE
            string cte = $@"
WITH Voy AS (
    SELECT IDVoznje,
           MAX(CAST(Zaposleni AS NVARCHAR(64)))  AS MasinovodjaID,
           MAX(CAST(Lokomotiva AS NVARCHAR(64))) AS Lokomotiva
    FROM PutniList
    WHERE Datum IS NOT NULL
    GROUP BY IDVoznje
),
Pairs AS (
    SELECT g.IDVoznje, g.SegmentTimeMinutes
    FROM dbo.vw_SegmentPairs_Global g
    WHERE {wherePairs}
)";

            // 2) MAŠINOVOĐE (sa imenom i prezimenom)
            string sqlMas = cte + @"
SELECT TOP 5
    v.MasinovodjaID AS ID,
    COALESCE(RTRIM(d.DeIme)+' '+RTRIM(d.DePriimek), v.MasinovodjaID) AS ImePrezime,
    COUNT(*) AS Uzorci,
    AVG(CAST(p.SegmentTimeMinutes AS FLOAT)) AS AvgSegMin
FROM Pairs p
JOIN Voy v ON v.IDVoznje = p.IDVoznje
LEFT JOIN Delavci d
       ON d.DeSifra = CASE WHEN ISNUMERIC(v.MasinovodjaID)=1
                           THEN CONVERT(INT, v.MasinovodjaID)
                           ELSE -2147483648 END
GROUP BY v.MasinovodjaID, d.DeIme, d.DePriimek
HAVING COUNT(*) >= 5
ORDER BY AvgSegMin ASC;";

            // 3) LOKOMOTIVE
            string sqlLok = cte + @"
SELECT TOP 5
    v.Lokomotiva AS ID,
    COUNT(*) AS Uzorci,
    AVG(CAST(p.SegmentTimeMinutes AS FLOAT)) AS AvgSegMin
FROM Pairs p
JOIN Voy v ON v.IDVoznje = p.IDVoznje
GROUP BY v.Lokomotiva
HAVING COUNT(*) >= 5
ORDER BY AvgSegMin ASC;";

            var dtMas = new DataTable();
            var dtLok = new DataTable();
            using (var cn = new SqlConnection(connection))
            {
                cn.Open();
                using (var da1 = new SqlDataAdapter(sqlMas, cn)) da1.Fill(dtMas);
                using (var da2 = new SqlDataAdapter(sqlLok, cn)) da2.Fill(dtLok);
            }

            // 4) Izračunaj procenu za celu rutu i ETA po kandidatu
            int segCount = plannedPairs.Rows.Count;

            void AddCommonCols(DataTable dt)
            {
                if (!dt.Columns.Contains("Procena minuta za rutu"))
                    dt.Columns.Add("Procena minuta za rutu", typeof(double));
                if (!dt.Columns.Contains("Start"))
                    dt.Columns.Add("Start", typeof(string));
                if (!dt.Columns.Contains("ETA"))
                    dt.Columns.Add("ETA", typeof(DateTime));
            }

            AddCommonCols(dtMas);
            AddCommonCols(dtLok);

            foreach (DataRow r in dtMas.Rows)
            {
                double avg = r["AvgSegMin"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["AvgSegMin"]);
                double est = Math.Round(avg * segCount, 1);
                r["Procena minuta za rutu"] = est;
                r["Start"] = start.ToString("yyyy-MM-dd HH:mm");
                r["ETA"] = start.AddMinutes(est);
            }
            foreach (DataRow r in dtLok.Rows)
            {
                double avg = r["AvgSegMin"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["AvgSegMin"]);
                double est = Math.Round(avg * segCount, 1);
                r["Procena minuta za rutu"] = est;
                r["Start"] = start.ToString("yyyy-MM-dd HH:mm");
                r["ETA"] = start.AddMinutes(est);
            }

            // 5) Lepši nazivi
            if (dtMas.Columns.Contains("ID")) dtMas.Columns["ID"].ColumnName = "Mašinovođa ID";
            if (dtMas.Columns.Contains("ImePrezime")) dtMas.Columns["ImePrezime"].ColumnName = "Mašinovođa";
            if (dtMas.Columns.Contains("Uzorci")) dtMas.Columns["Uzorci"].ColumnName = "Uzorci";
            if (dtMas.Columns.Contains("AvgSegMin")) dtMas.Columns["AvgSegMin"].ColumnName = "Prosek/min po segmentu";

            if (dtLok.Columns.Contains("ID")) dtLok.Columns["ID"].ColumnName = "Lokomotiva";
            if (dtLok.Columns.Contains("Uzorci")) dtLok.Columns["Uzorci"].ColumnName = "Uzorci";
            if (dtLok.Columns.Contains("AvgSegMin")) dtLok.Columns["AvgSegMin"].ColumnName = "Prosek/min po segmentu";

            // 6) Prikaži u gridovima
            dataGridView2.DataSource = dtMas; // mašinovođe
            dataGridView3.DataSource = dtLok; // lokomotive
            dataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView3.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        }
        private string ResolveMasinovodjaName(int deSifra)
        {
            try
            {
                using (var conn = new SqlConnection(connection))
                using (var cmd = new SqlCommand("SELECT RTRIM(DeIme)+' '+RTRIM(DePriimek) FROM Delavci WHERE DeSifra=@id", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@id", deSifra);
                    var o = cmd.ExecuteScalar();
                    if (o != null && o != DBNull.Value) return o.ToString();
                }
            }
            catch { }
            return deSifra.ToString();
        }
        /// <summary>
        /// Ponderiše prosečno vreme po segmentu prema broju uzoraka:
        /// adjusted = (avg*Samples + prior*K) / (Samples + K)
        /// K ~ koliko uzoraka "verujemo" prioru (tipično 8-15).
        /// </summary>
        private static double ShrinkAvg(double avgSegMin, int samples, double priorSegMin, int K = 10)
        {
            return ((avgSegMin * samples) + (priorSegMin * K)) / Math.Max(1, (samples + K));
        }

        private void UpdateSummaryPanel(int rn, DateTime tripStart, DateTime etaWithStops)
        {
            // 0) ETA bez zaustavljanja iz postojećeg modela
            DateTime etaNoStops = etaWithStops;
            try
            {
                var basic = _eta.PredictChainForRN(rn);
                etaNoStops = basic.final;
                // opciono: ako želiš strogo isti start, možeš:
                // etaNoStops = tripStart.Add(basic.final - basic.start);
            }
            catch { /* ako model nije tu, koristi etaWithStops kao fallback */ }

            // 1) Skupi planirane A->B parove za dati RN
            var plannedPairs = new DataTable();
            using (var cn = new SqlConnection(connection))
            {
                cn.Open();
                using (var da = new SqlDataAdapter("SELECT DISTINCT StanicaFromID, StanicaToID FROM dbo.fn_Segments_For_RN(@rn)", cn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@rn", rn);
                    da.Fill(plannedPairs);
                }
            }
            int plannedSegmentsCount = plannedPairs.Rows.Count;
            if (plannedSegmentsCount == 0)
            {
                // nema planiranih segmenata – očisti panele i izađi
                if (lblBestLoco != null) lblBestLoco.Text = "Najbolje vreme Lokomotiva: n/a";
                if (lblBestDriver != null) lblBestDriver.Text = "Najbolje vreme Mašinovođa: n/a";
                if (lblEtaNoStops != null) lblEtaNoStops.Text = "ETA bez zaustavljanja: n/a";
                if (lblEtaWithStops != null) lblEtaWithStops.Text = "ETA sa zaustavljanjima: n/a";
                return;
            }

            // WHERE ( (A=.. AND B=..) OR ... )
            var orPairs = new StringBuilder();
            foreach (DataRow pr in plannedPairs.Rows)
            {
                if (orPairs.Length > 0) orPairs.Append(" OR ");
                orPairs.AppendFormat("(g.StanicaFromID={0} AND g.StanicaToID={1})",
                    Convert.ToInt32(pr["StanicaFromID"]), Convert.ToInt32(pr["StanicaToID"]));
            }
            string wherePairs = orPairs.ToString();

            // 2) Izvuci kandidate mašinovođa
            string sqlDrv = $@"
WITH Voy AS (
  SELECT IDVoznje,
         MAX(CAST(Zaposleni AS INT)) AS MasinovodjaID
  FROM PutniList
  WHERE Datum IS NOT NULL
  GROUP BY IDVoznje
),
Pairs AS (
  SELECT g.IDVoznje, g.SegmentTimeMinutes
  FROM dbo.vw_SegmentPairs_Global g
  WHERE {wherePairs}
)
SELECT v.MasinovodjaID AS ID, COUNT(*) AS Samples,
       AVG(CAST(p.SegmentTimeMinutes AS FLOAT)) AS AvgSegMin
FROM Pairs p
JOIN Voy v ON v.IDVoznje=p.IDVoznje
GROUP BY v.MasinovodjaID
HAVING COUNT(*) >= 3;";  // min pokrivenost

            // 3) Izvuci kandidate lokomotiva
            string sqlLoco = $@"
WITH Voy AS (
  SELECT IDVoznje,
         MAX(CAST(Lokomotiva AS NVARCHAR(32))) AS Lokomotiva
  FROM PutniList
  WHERE Datum IS NOT NULL
  GROUP BY IDVoznje
),
Pairs AS (
  SELECT g.IDVoznje, g.SegmentTimeMinutes
  FROM dbo.vw_SegmentPairs_Global g
  WHERE {wherePairs}
)
SELECT v.Lokomotiva AS ID, COUNT(*) AS Samples,
       AVG(CAST(p.SegmentTimeMinutes AS FLOAT)) AS AvgSegMin
FROM Pairs p
JOIN Voy v ON v.IDVoznje=p.IDVoznje
GROUP BY v.Lokomotiva
HAVING COUNT(*) >= 3;";

            var drivers = new DataTable();
            var locos = new DataTable();
            using (var cn = new SqlConnection(connection))
            {
                cn.Open();
                using (var da = new SqlDataAdapter(sqlDrv, cn)) da.Fill(drivers);
                using (var da = new SqlDataAdapter(sqlLoco, cn)) da.Fill(locos);
            }

            // 4) Prior proseci po segmentu (da bi shrink radio kako treba)
            Func<DataTable, double> priorSeg = (dt) =>
            {
                if (dt.Rows.Count == 0) return 0.0;
                double wSum = 0, w = 0;
                foreach (DataRow r in dt.Rows)
                {
                    var s = Convert.ToInt32(r["Samples"]);
                    var a = Convert.ToDouble(r["AvgSegMin"]);
                    wSum += s * a; w += s;
                }
                return w > 0 ? wSum / w : dt.AsEnumerable().Average(x => Convert.ToDouble(x["AvgSegMin"]));
            };

            double priorDrvSeg = priorSeg(drivers);
            double priorLocSeg = priorSeg(locos);
            int K = 10; // verovanje prioru

            // 5) Izaberimo “najboljeg” mašinovođu (min ponderisan total)
            int bestDrvId = 0; string bestDrvName = "n/a";
            double bestDrvTotalMin = double.MaxValue;
            foreach (DataRow r in drivers.Rows)
            {
                int id = Convert.ToInt32(r["ID"]);
                int n = Convert.ToInt32(r["Samples"]);
                double avg = Convert.ToDouble(r["AvgSegMin"]);
                double adjSeg = ShrinkAvg(avg, n, priorDrvSeg, K);   // ponderisan seg
                double total = adjSeg * plannedSegmentsCount;         // vreme rute
                if (total < bestDrvTotalMin)
                {
                    bestDrvTotalMin = total;
                    bestDrvId = id;
                }
            }
            if (bestDrvId != 0) bestDrvName = ResolveMasinovodjaName(bestDrvId);
            var bestDrvEta = tripStart.AddMinutes(bestDrvTotalMin);

            // 6) Izaberimo “najbolju” lokomotivu
            string bestLocoId = "n/a";
            double bestLocoTotalMin = double.MaxValue;
            foreach (DataRow r in locos.Rows)
            {
                string id = r["ID"].ToString();
                int n = Convert.ToInt32(r["Samples"]);
                double avg = Convert.ToDouble(r["AvgSegMin"]);
                double adjSeg = ShrinkAvg(avg, n, priorLocSeg, K);
                double total = adjSeg * plannedSegmentsCount;
                if (total < bestLocoTotalMin)
                {
                    bestLocoTotalMin = total;
                    bestLocoId = id;
                }
            }
            var bestLocoEta = tripStart.AddMinutes(bestLocoTotalMin);

            // 7) Upis u panel (zameni nazive labela ako su drugačiji)
            if (lblBestLoco != null)
                lblBestLoco.Text = $"Najbolje vreme Lokomotiva: {bestLocoId} — {Math.Round(bestLocoTotalMin, 1)} min (ETA {bestLocoEta:yyyy-MM-dd HH:mm})";
            if (lblBestDriver != null)
                lblBestDriver.Text = $"Najbolje vreme Mašinovođa: {bestDrvName} — {Math.Round(bestDrvTotalMin, 1)} min (ETA {bestDrvEta:yyyy-MM-dd HH:mm})";
            if (lblEtaNoStops != null)
                lblEtaNoStops.Text = $"ETA bez zaustavljanja: {etaNoStops:yyyy-MM-dd HH:mm}";
            if (lblEtaWithStops != null)
                lblEtaWithStops.Text = $"ETA zaustavljanja: {etaWithStops:yyyy-MM-dd HH:mm}";
        }

        private (DataTable dtDrv, DataTable dtLoco,
         (int id, string name, double totalMin, DateTime eta) bestDrv,
         (string id, double totalMin, DateTime eta) bestLoco)
    BuildRecommendationsSplit(int rn, DateTime tripStart)
        {
            // 1) Skupi planirane A->B parove
            var plannedPairs = new DataTable();
            using (var cn = new SqlConnection(connection))
            {
                cn.Open();
                using (var da = new SqlDataAdapter("SELECT DISTINCT StanicaFromID, StanicaToID FROM dbo.fn_Segments_For_RN(@rn)", cn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@rn", rn);
                    da.Fill(plannedPairs);
                }
            }
            int plannedSegmentsCount = plannedPairs.Rows.Count;
            if (plannedSegmentsCount == 0)
                return (new DataTable(), new DataTable(), (0, "", 0, tripStart), ("", 0, tripStart));

            // WHERE ( (A=.. AND B=..) OR ... )
            var orPairs = new StringBuilder();
            foreach (DataRow pr in plannedPairs.Rows)
            {
                if (orPairs.Length > 0) orPairs.Append(" OR ");
                orPairs.AppendFormat("(g.StanicaFromID={0} AND g.StanicaToID={1})",
                    Convert.ToInt32(pr["StanicaFromID"]), Convert.ToInt32(pr["StanicaToID"]));
            }
            string wherePairs = orPairs.ToString();

            // 2) Učitaj agregate za mašinovođe i lokomotive
            string sqlDrv = $@"
WITH Voy AS (
  SELECT IDVoznje,
         MAX(CAST(Zaposleni AS INT))   AS MasinovodjaID
  FROM PutniList
  WHERE Datum IS NOT NULL
  GROUP BY IDVoznje
),
Pairs AS (
  SELECT g.IDVoznje, g.SegmentTimeMinutes
  FROM dbo.vw_SegmentPairs_Global g
  WHERE {wherePairs}
)
SELECT v.MasinovodjaID AS ID, COUNT(*) AS Samples,
       AVG(CAST(p.SegmentTimeMinutes AS FLOAT)) AS AvgSegMin
FROM Pairs p
JOIN Voy v ON v.IDVoznje=p.IDVoznje
GROUP BY v.MasinovodjaID
HAVING COUNT(*)>=3
ORDER BY AvgSegMin ASC;";

            string sqlLoco = $@"
WITH Voy AS (
  SELECT IDVoznje,
         MAX(CAST(Lokomotiva AS NVARCHAR(32))) AS Lokomotiva
  FROM PutniList
  WHERE Datum IS NOT NULL
  GROUP BY IDVoznje
),
Pairs AS (
  SELECT g.IDVoznje, g.SegmentTimeMinutes
  FROM dbo.vw_SegmentPairs_Global g
  WHERE {wherePairs}
)
SELECT v.Lokomotiva AS ID, COUNT(*) AS Samples,
       AVG(CAST(p.SegmentTimeMinutes AS FLOAT)) AS AvgSegMin
FROM Pairs p
JOIN Voy v ON v.IDVoznje=p.IDVoznje
GROUP BY v.Lokomotiva
HAVING COUNT(*)>=3
ORDER BY AvgSegMin ASC;";

            var drv = new DataTable();
            var loco = new DataTable();
            using (var cn = new SqlConnection(connection))
            {
                cn.Open();
                using (var da = new SqlDataAdapter(sqlDrv, cn)) da.Fill(drv);
                using (var da = new SqlDataAdapter(sqlLoco, cn)) da.Fill(loco);
            }

            // 3) Prior (po segmentu) i ponderisanje
            Func<DataTable, double> priorSeg = (dt) =>
            {
                if (dt.Rows.Count == 0) return 0.0;
                double wSum = 0, w = 0;
                foreach (DataRow r in dt.Rows)
                {
                    var s = Convert.ToInt32(r["Samples"]);
                    var a = Convert.ToDouble(r["AvgSegMin"]);
                    wSum += s * a; w += s;
                }
                return w > 0 ? wSum / w : dt.AsEnumerable().Average(x => Convert.ToDouble(x["AvgSegMin"]));
            };

            double priorDrvSeg = priorSeg(drv);
            double priorLocSeg = priorSeg(loco);
            int K = 10; // koliko verujemo prioru, slobodno promeni

            // 4) Obogati tabele, izračunaj ETA po kandidatu i nađi najboljeg
            DataTable dtDrv = new DataTable();
            dtDrv.Columns.Add("Mašinovođa", typeof(string));
            dtDrv.Columns.Add("Uzorci", typeof(int));
            dtDrv.Columns.Add("Prosek/min po segmentu", typeof(double));
            dtDrv.Columns.Add("Ponder/min po segmentu", typeof(double));
            dtDrv.Columns.Add("Procena minuta rute", typeof(double));
            dtDrv.Columns.Add("Start", typeof(string));
            dtDrv.Columns.Add("ETA", typeof(string));

            var bestDrvPick = (id: 0, name: "", totalMin: double.MaxValue, eta: tripStart);

            // keširaj imena
            var nameCache = new Dictionary<int, string>();

            foreach (DataRow r in drv.Rows)
            {
                int id = Convert.ToInt32(r["ID"]);
                int n = Convert.ToInt32(r["Samples"]);
                double avg = Convert.ToDouble(r["AvgSegMin"]);
                double adjSeg = ShrinkAvg(avg, n, priorDrvSeg, K);
                double total = adjSeg * plannedSegmentsCount;
                var eta = tripStart.AddMinutes(total);

                string name;
                if (!nameCache.TryGetValue(id, out name))
                {
                    name = ResolveMasinovodjaName(id);
                    nameCache[id] = name;
                }

                var row = dtDrv.NewRow();
                row["Mašinovođa"] = name;
                row["Uzorci"] = n;
                row["Prosek/min po segmentu"] = Math.Round(avg, 2);
                row["Ponder/min po segmentu"] = Math.Round(adjSeg, 2);
                row["Procena minuta rute"] = Math.Round(total, 1);
                row["Start"] = tripStart.ToString("yyyy-MM-dd HH:mm");
                row["ETA"] = eta.ToString("yyyy-MM-dd HH:mm");
                dtDrv.Rows.Add(row);

                if (total < bestDrvPick.totalMin) bestDrvPick = (id, name, total, eta);
            }

            DataTable dtLoco = new DataTable();
            dtLoco.Columns.Add("Lokomotiva", typeof(string));
            dtLoco.Columns.Add("Uzorci", typeof(int));
            dtLoco.Columns.Add("Prosek/min po segmentu", typeof(double));
            dtLoco.Columns.Add("Ponder/min po segmentu", typeof(double));
            dtLoco.Columns.Add("Procena minuta rute", typeof(double));
            dtLoco.Columns.Add("Start", typeof(string));
            dtLoco.Columns.Add("ETA", typeof(string));

            var bestLocoPick = (id: "", totalMin: double.MaxValue, eta: tripStart);

            foreach (DataRow r in loco.Rows)
            {
                string id = r["ID"].ToString();
                int n = Convert.ToInt32(r["Samples"]);
                double avg = Convert.ToDouble(r["AvgSegMin"]);
                double adjSeg = ShrinkAvg(avg, n, priorLocSeg, K);
                double total = adjSeg * plannedSegmentsCount;
                var eta = tripStart.AddMinutes(total);

                var row = dtLoco.NewRow();
                row["Lokomotiva"] = id;
                row["Uzorci"] = n;
                row["Prosek/min po segmentu"] = Math.Round(avg, 2);
                row["Ponder/min po segmentu"] = Math.Round(adjSeg, 2);
                row["Procena minuta rute"] = Math.Round(total, 1);
                row["Start"] = tripStart.ToString("yyyy-MM-dd HH:mm");
                row["ETA"] = eta.ToString("yyyy-MM-dd HH:mm");
                dtLoco.Rows.Add(row);

                if (total < bestLocoPick.totalMin) bestLocoPick = (id, total, eta);
            }

            return (dtDrv, dtLoco, bestDrvPick, bestLocoPick);
        }

        private sealed class ProgressDialog : Form
        {
            private readonly ProgressBar _bar;
            private readonly Label _label;

            public ProgressDialog(string text)
            {
                Text = "Treniranje modela";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                StartPosition = FormStartPosition.CenterParent;
                Width = 420;
                Height = 130;

                _label = new Label { Left = 12, Top = 10, Width = 380, Text = text };
                _bar = new ProgressBar
                {
                    Left = 12,
                    Top = 40,
                    Width = 380,
                    Height = 20,
                    Minimum = 0,
                    Maximum = 100,
                    Style = ProgressBarStyle.Marquee
                };

                Controls.Add(_label);
                Controls.Add(_bar);
            }

            public void SetText(string text)
            {
                if (InvokeRequired) { BeginInvoke(new Action<string>(SetText), text); return; }
                _label.Text = text;
            }

            public void SetProgress(int percent)
            {
                if (InvokeRequired) { BeginInvoke(new Action<int>(SetProgress), percent); return; }

                // Clamp 0..100
                percent = Math.Max(0, Math.Min(100, percent));

                if (percent == 0 || percent == 100)
                {
                    _bar.Style = ProgressBarStyle.Blocks;
                    _bar.Value = percent;
                }
                else
                {
                    if (_bar.Style != ProgressBarStyle.Blocks)
                        _bar.Style = ProgressBarStyle.Blocks;

                    _bar.Value = percent;
                }
            }
        }

        private void otvoriLOGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TrainLogger.OpenLogFolder();
        }
        private sealed class StationInfo
        {
            public int Id;
            public string Name;
            public double Lat;
            public double Lon;
        }

        // vrati info za zadati skup stanica
        private Dictionary<int, StationInfo> GetStationsInfo(IEnumerable<int> stationIds)
        {
            var ids = stationIds.Distinct().ToList();
            var map = new Dictionary<int, StationInfo>();
            if (ids.Count == 0) return map;

            // IN (...) – za SQL2008 dinamika
            var idList = string.Join(",", ids.Select(i => i.ToString()));
            var sql = $@"SELECT ID, RTrim(Opis) AS Opis, Latitude, Longitude 
                 FROM Stanice WHERE ID IN ({idList})";

            using (var cn = new SqlConnection(connection))
            using (var da = new SqlDataAdapter(sql, cn))
            {
                var dt = new DataTable();
                da.Fill(dt);
                foreach (DataRow r in dt.Rows)
                {
                    int id = Convert.ToInt32(r["ID"]);
                    double lat = r["Latitude"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Latitude"]);
                    double lon = r["Longitude"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Longitude"]);
                    var name = r["Opis"]?.ToString() ?? id.ToString();

                    map[id] = new StationInfo { Id = id, Name = name, Lat = lat, Lon = lon };
                }
            }
            return map;
        }
        private sealed class MapPoint
        {
            public double lat;
            public double lon;
            public string name;
            public string eta;  // kao "yyyy-MM-dd HH:mm"
            public double p;    // verovatnoća stajanja na segmentu pre ove tačke (0..1)
        }

        private void UpdateMapFromPrediction(
            DateTime start,
            List<(int rbTrase, int stanicaTo, DateTime eta, float pStop, string reason, float waitMin)> chain)
        {
            if (!_mapReady || webBrowser1.Document == null) return;

            // 1) Rekonstruiši put: početna stanica + sve destinacije po redu
            //    Početnu stanicu uzimamo iz plana (RBTrase=1, RBFrom=1)
            var plan = LoadSegmentPlan(Convert.ToInt32(comboBox1.SelectedValue)); // već imaš ovu metodu
            int? firstStationId = null;
            foreach (DataRow r in plan.Rows)
            {
                if (Convert.ToInt32(r["RBTrase"]) == 1 && Convert.ToInt32(r["RBFrom"]) == 1)
                {
                    firstStationId = Convert.ToInt32(r["StanicaFromID"]);
                    break;
                }
            }

            var stationOrder = new List<int>();
            if (firstStationId.HasValue) stationOrder.Add(firstStationId.Value);
            stationOrder.AddRange(chain.Select(x => x.stanicaTo));

            // 2) Prebaci u koordinate + imena
            var info = GetStationsInfo(stationOrder);
            // ako fale koordinate, preskačemo te tačke
            // 3) Sastavi map points: prvi ima p=0 (nema segment pre njega)
            var points = new List<MapPoint>();

            // Start point (ako ga imamo)
            if (firstStationId.HasValue && info.TryGetValue(firstStationId.Value, out var s0) && s0.Lat != 0.0 && s0.Lon != 0.0)
            {
                points.Add(new MapPoint
                {
                    lat = s0.Lat,
                    lon = s0.Lon,
                    name = s0.Name,
                    eta = start.ToString("yyyy-MM-dd HH:mm"),
                    p = 0.0
                });
            }

            // Destinacije
            foreach (var seg in chain)
            {
                if (info.TryGetValue(seg.stanicaTo, out var si) && si.Lat != 0.0 && si.Lon != 0.0)
                {
                    points.Add(new MapPoint
                    {
                        lat = si.Lat,
                        lon = si.Lon,
                        name = si.Name,
                        eta = seg.eta.ToString("yyyy-MM-dd HH:mm"),
                        p = Math.Max(0.0, Math.Min(1.0, seg.pStop))  // clamp 0..1
                    });
                }
            }

            if (points.Count == 0) return;

            // 4) JSON → JS
            var js = new JavaScriptSerializer();
            string json = js.Serialize(points);

            try
            {
                webBrowser1.Document.InvokeScript("setRouteFromJson", new object[] { json });
            }
            catch
            {
                // fallback: probaj preko window
                webBrowser1.Document.InvokeScript("eval", new object[] { $"window.setRouteFromJson({json});" });
            }
        }
        private void DesignGV()
        {
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

            dataGridView2.BorderStyle = BorderStyle.Fixed3D;
            dataGridView2.AlternatingRowsDefaultCellStyle.BackColor = Color.LightGray;
            dataGridView2.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridView2.DefaultCellStyle.SelectionBackColor = Color.DarkTurquoise;
            dataGridView2.DefaultCellStyle.SelectionForeColor = Color.WhiteSmoke;
            dataGridView2.BackgroundColor = Color.FromArgb(125, 158, 192);
            dataGridView2.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;

            dataGridView2.EnableHeadersVisualStyles = false;
            dataGridView2.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Sunken;
            dataGridView2.ColumnHeadersDefaultCellStyle.BackColor = Color.DarkOrange;
            dataGridView2.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;

            dataGridView3.BorderStyle = BorderStyle.Fixed3D;
            dataGridView3.AlternatingRowsDefaultCellStyle.BackColor = Color.LightGray;
            dataGridView3.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridView3.DefaultCellStyle.SelectionBackColor = Color.DarkTurquoise;
            dataGridView3.DefaultCellStyle.SelectionForeColor = Color.WhiteSmoke;
            dataGridView3.BackgroundColor = Color.FromArgb(125, 158, 192);
            dataGridView3.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;

            dataGridView3.EnableHeadersVisualStyles = false;
            dataGridView3.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Sunken;
            dataGridView3.ColumnHeadersDefaultCellStyle.BackColor = Color.DarkOrange;
            dataGridView3.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        }

        private void uToolStripMenuItem_Click(object sender, EventArgs e)
        {
            panel2.Visible = true;
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
        private void UncheckAll(CheckedListBox ch)
        {
            ch.BeginUpdate();
            for (int i = 0; i < ch.Items.Count; i++)
                ch.SetItemChecked(i, false);
            ch.EndUpdate();
        }

        private void btnNazad_Click(object sender, EventArgs e)
        {
            UncheckAll(chkListPrijem);
            UncheckAll(chkListPredaja);
            panel2.Visible = false;
        }
        private int _lastRn = 0;
        private DateTime _lastStart = DateTime.MinValue;
        private DateTime _lastEtaWithStops = DateTime.MinValue;
        private DateTime _lastEtaNoStops = DateTime.MinValue;
        private List<int> GetCheckedIds(CheckedListBox clb)
        {
            var list = new List<int>();
            foreach (var it in clb.CheckedItems)
            {
                var drv = it as DataRowView;
                if (drv != null)
                    list.Add(Convert.ToInt32(drv["ID"]));
                else if (it is int id)
                    list.Add(id);
            }
            return list;
        }
        private sealed class ActivityStat
        {
            public int Id { get; set; }
            public int Samples { get; set; }
            public double MedianMin { get; set; }
            public double TrimMeanMin { get; set; }
        }
        private sealed class GlobalActivityStat
        {
            public int Samples { get; set; }
            public double GlobalMedianMin { get; set; }
            public double GlobalTrimMeanMin { get; set; }
        }

        private static double MedianMinutes(List<double> hours)
        {
            if (hours == null || hours.Count == 0) return 0.0;
            hours.Sort();
            int n = hours.Count;
            if ((n & 1) == 1) // neparan
                return hours[n / 2] * 60.0;
            // paran -> sredina dva
            return ((hours[n / 2 - 1] + hours[n / 2]) / 2.0) * 60.0;
        }
        private Dictionary<int, ActivityStat> LoadActivityStatsById(IEnumerable<int> ids)
        {
            var map = new Dictionary<int, ActivityStat>();
            var idList = ids?.Distinct().ToList() ?? new List<int>();
            if (idList.Count == 0) return map;

            var inList = string.Join(",", idList); // SQL2008 friendly
            var sql = $@"
SELECT VrstaAktivnostiID, Samples, MedianMin, TrimMeanMin
FROM dbo.vw_AktivnostiStats
WHERE VrstaAktivnostiID IN ({inList});";

            using (var cn = new SqlConnection(connection))
            using (var da = new SqlDataAdapter(sql, cn))
            {
                var dt = new DataTable();
                da.Fill(dt);
                foreach (DataRow r in dt.Rows)
                {
                    var st = new ActivityStat
                    {
                        Id = Convert.ToInt32(r["VrstaAktivnostiID"]),
                        Samples = r["Samples"] == DBNull.Value ? 0 : Convert.ToInt32(r["Samples"]),
                        MedianMin = r["MedianMin"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["MedianMin"]),
                        TrimMeanMin = r["TrimMeanMin"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["TrimMeanMin"])
                    };
                    map[st.Id] = st;
                }
            }
            return map;
        }

        private GlobalActivityStat LoadGlobalActivityStats()
        {
            var g = new GlobalActivityStat();
            const string sql = @"SELECT TOP 1 Samples, GlobalMedianMin, GlobalTrimMeanMin FROM dbo.vw_AktivnostiGlobalStats;";
            using (var cn = new SqlConnection(connection))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        g.Samples = rd["Samples"] == DBNull.Value ? 0 : Convert.ToInt32(rd["Samples"]);
                        g.GlobalMedianMin = rd["GlobalMedianMin"] == DBNull.Value ? 0.0 : Convert.ToDouble(rd["GlobalMedianMin"]);
                        g.GlobalTrimMeanMin = rd["GlobalTrimMeanMin"] == DBNull.Value ? 0.0 : Convert.ToDouble(rd["GlobalTrimMeanMin"]);
                    }
                }
            }
            return g;
        }
        private double RobustMinutesForActivity(ActivityStat s, GlobalActivityStat g)
        {
            if (s == null)
            {
                // fallback samo global
                if (g.GlobalTrimMeanMin > 0) return g.GlobalTrimMeanMin;
                return g.GlobalMedianMin;
            }

            if (s.Samples >= 20 && s.TrimMeanMin > 0)
                return s.TrimMeanMin;

            if (s.Samples >= 3 && s.MedianMin > 0)
                return s.MedianMin;

            // premalo uzoraka -> global
            if (g.GlobalTrimMeanMin > 0) return g.GlobalTrimMeanMin;
            return g.GlobalMedianMin;
        }

        private double SumRobustMinutesFor(IEnumerable<int> ids)
        {
            var list = (ids ?? Enumerable.Empty<int>()).Distinct().ToList();
            if (list.Count == 0) return 0.0;

            var stats = LoadActivityStatsById(list);
            var g = LoadGlobalActivityStats();

            double sum = 0.0;
            foreach (var id in list)
            {
                stats.TryGetValue(id, out var st);
                sum += RobustMinutesForActivity(st, g);
            }
            return sum;
        }
        private Dictionary<int, double> LoadActivityMedianMinutesById(IEnumerable<int> ids)
        {
            var result = new Dictionary<int, double>();
            var idList = ids?.Distinct().ToList() ?? new List<int>();
            if (idList.Count == 0) return result;

            // napravi IN (...) listu
            var inList = string.Join(",", idList);
            var sql = $@"
SELECT VrstaAktivnostiID, Sati
FROM AktivnostiStavke
WHERE Sati IS NOT NULL AND Sati > 0
  AND VrstaAktivnostiID IN ({inList})";

            var buckets = new Dictionary<int, List<double>>();
            using (var cn = new SqlConnection(connection))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        int aId = Convert.ToInt32(rd["VrstaAktivnostiID"]);
                        double h = Convert.ToDouble(rd["Sati"]);
                        if (!buckets.TryGetValue(aId, out var list))
                        {
                            list = new List<double>();
                            buckets[aId] = list;
                        }
                        list.Add(h);
                    }
                }
            }

            foreach (var kv in buckets)
                result[kv.Key] = MedianMinutes(kv.Value); // u MINUTIMA

            // aktivnosti koje nemaju ni jedan red >0 sati neće ući u buckets → tretiramo kao 0 min
            foreach (var id in idList)
                if (!result.ContainsKey(id))
                    result[id] = 0.0;

            return result;
        }


        private double SumMedianMinutesFor(IEnumerable<int> ids)
        {
            var dict = LoadActivityMedianMinutesById(ids);
            return dict.Values.Sum();
        }

        private void UkljuciAktivnosti()
        {
            try
            {
                // 0) moramo imati “poslednje” vreme polaska i ETA-e
                if (_lastStart == DateTime.MinValue || _lastEtaWithStops == DateTime.MinValue || _lastEtaNoStops == DateTime.MinValue)
                {
                    MessageBox.Show("Prvo izračunaj predikciju (ETA) pa potvrdi aktivnosti.", "Info",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 1) skupi izabrane aktivnosti
                var prijemIds = GetCheckedIds(chkListPrijem);
                var predajaIds = GetCheckedIds(chkListPredaja);

                // 2) izračunaj optimalno trajanje (suma medijana) u minutima
                double prijemMin = SumRobustMinutesFor(prijemIds);
                double predajaMin = SumRobustMinutesFor(predajaIds);

                // 3) predlog vremena za početak prijema (unazad od _lastStart)
                var prepStart = _lastStart.AddMinutes(-prijemMin);

                // 4) predaja voza: dodaj na obe ETA varijante
                var handoverNoStops = _lastEtaNoStops.AddMinutes(predajaMin);
                var handoverWithStops = _lastEtaWithStops.AddMinutes(predajaMin);

                // 5) ispis na panel (promeni nazive labela ako su drugačiji)
                if (lblPrepStart != null)
                {
                    var txt = prijemMin > 0
                        ? $"Početak prijema voza: {prepStart:yyyy-MM-dd HH:mm} (trajanje prijema ≈ {Math.Round(prijemMin, 1)} min)"
                        : "Početak prijema voza: nema izabranih aktivnosti (0 min)";
                    lblPrepStart.Text = txt;
                }

                if (lblHandoverNoStops != null)
                {
                    var baseTxt = $"ETA bez zaustavljanja: {_lastEtaNoStops:yyyy-MM-dd HH:mm}";
                    var withAct = predajaMin > 0
                        ? $" | Predaja: {handoverNoStops:yyyy-MM-dd HH:mm} (+{Math.Round(predajaMin, 1)} min)"
                        : " | Predaja: isto (0 min)";
                    lblHandoverNoStops.Text = baseTxt + withAct;
                }

                if (lblHandoverWithStops != null)
                {
                    var baseTxt = $"ETA zaustavljanja: {_lastEtaWithStops:yyyy-MM-dd HH:mm}";
                    var withAct = predajaMin > 0
                        ? $" | Predaja: {handoverWithStops:yyyy-MM-dd HH:mm} (+{Math.Round(predajaMin, 1)} min)"
                        : " | Predaja: isto (0 min)";
                    lblHandoverWithStops.Text = baseTxt + withAct;
                }

                // (opciono) možeš i popup za kratak sažetak:
                // MessageBox.Show($"Prijem: {prijemMin:F1} min\nPredaja: {predajaMin:F1} min", "Aktivnosti");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri obradi aktivnosti: " + ex.Message, "Greška",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnPotvridAktivnosti_Click(object sender, EventArgs e)
        {
            if (_includeActivitiesNextPrediction)
            {
                // 1) prvo izračunaj ETA (tvoja postojeća logika)
                PredvidiVremena();

                // 2) odmah potom uključi aktivnosti (tvoja postojeća metoda)
                UkljuciAktivnosti();

                // (opciono) sakrij panel posle potvrde
                panel2.Visible = false;

                // resetuj režim "prvo aktivnosti pa ETA"
                _includeActivitiesNextPrediction = false;
            }
            else
            {
                // standardni slučaj: korisnik naknadno uključuje aktivnosti na već izračunate ETA
                UkljuciAktivnosti();
            }
        }
        private Dictionary<int, string> LoadActivityNames(IEnumerable<int> ids)
        {
            var map = new Dictionary<int, string>();
            var idList = ids?.Distinct().ToList() ?? new List<int>();
            if (idList.Count == 0) return map;

            var inList = string.Join(",", idList);
            var sql = $@"SELECT ID, RTRIM(Naziv) AS Naziv FROM VrstaAktivnosti WHERE ID IN ({inList})";

            using (var cn = new SqlConnection(connection))
            using (var da = new SqlDataAdapter(sql, cn))
            {
                var dt = new DataTable();
                da.Fill(dt);
                foreach (DataRow r in dt.Rows)
                    map[Convert.ToInt32(r["ID"])] = Convert.ToString(r["Naziv"]);
            }
            return map;
        }
    }
}
