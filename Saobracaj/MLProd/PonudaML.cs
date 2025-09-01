using Microsoft.ReportingServices.Diagnostics.Internal;
using Saobracaj.Dokumenta;
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
using System.Windows.Forms;
using DataSet = System.Data.DataSet;

namespace Saobracaj.MLProd
{
    public partial class PonudaML : Form
    {
        private readonly string connection = ConfigurationManager
            .ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"]
            .ConnectionString;

        // === ML (retrain) ===
        private EtaService _eta;
        private readonly string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "global_segment_model.zip");
        private CancellationTokenSource cts;

        // Data + UI
        private DataTable _dtPutevi;
        private int RN;                 // izabrani RN iz grid-a
        private double VremeRN;         // trajanje iz grid-a (u minutima)

        public PonudaML()
        {
            InitializeComponent();
            FillCombo();
            FillChkList();
            TryInitEtaService();
            if (lblRNVreme != null) lblRNVreme.Visible = false;
        }

        private void TryInitEtaService()
        {
            try { _eta = new EtaService(connection, modelPath); }
            catch { _eta = null; }
        }

        #region UI: Zahtevi i check liste

        private void FillCombo()
        {
            SqlConnection conn = new SqlConnection(connection);

            // Zahtevi
            var select = "select PkStPov, Cast(PkStPov as nvarchar(5))+'-'+RTrim(PKNaziv) as Naziv From povKupcev order by PkstPov desc";
            SqlDataAdapter da = new SqlDataAdapter(select, conn);
            DataSet ds = new DataSet();
            da.Fill(ds);
            comboBox1.DataSource = ds.Tables[0];
            comboBox1.DisplayMember = "Naziv";
            comboBox1.ValueMember = "PkStPov";

            // Kurs
            DateTime datum = DateTime.Now;
            string datumPom = datum.ToString("yyyy-MM-dd");
            string query = "Select distinct TeValuta,TeVrednost from Tecaj Where TeDatum='" + datumPom + "'";
            SqlDataAdapter da2 = new SqlDataAdapter(query, conn);
            DataSet ds2 = new DataSet();
            da2.Fill(ds2);
            comboBox2.DataSource = ds2.Tables[0];
            comboBox2.DisplayMember = "TeValuta";
            comboBox2.ValueMember = "TeVrednost";
        }

        private void FillChkList()
        {
            SqlConnection conn = new SqlConnection(connection);
            SqlDataAdapter da = new SqlDataAdapter(
                "SELECT ID, RTRIM(Naziv) AS Naziv FROM VrstaAktivnosti ORDER BY ID ASC", conn);
            DataTable dt = new DataTable();
            da.Fill(dt);

            DataTable dt2 = dt.Copy();

            chkListPrijem.DataSource = dt;
            chkListPrijem.DisplayMember = "Naziv";
            chkListPrijem.ValueMember = "ID";
            chkListPrijem.CheckOnClick = true;

            chkListPredaja.DataSource = dt2;
            chkListPredaja.DisplayMember = "Naziv";
            chkListPredaja.ValueMember = "ID";
            chkListPredaja.CheckOnClick = true;
        }

        private void comboBox1_SelectionChangeCommitted(object sender, EventArgs e)
        {
            VratiPrevozniPut();
        }

        private void VratiPrevozniPut()
        {
            if (comboBox1.SelectedValue == null)
            {
                MessageBox.Show("Izaberi zahtev u padajućoj listi.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int stPov = Convert.ToInt32(comboBox1.SelectedValue);

            SqlConnection conn = new SqlConnection(connection);
            SqlDataAdapter da = new SqlDataAdapter(@"
                SELECT DISTINCT 
                    pk.PkPSifra,
                    RTRIM(mp.MpNaziv) AS MpNaziv
                FROM PovKupcevPostav pk
                INNER JOIN MaticniPodatki mp ON pk.PkPSifra = mp.MpSifra
                WHERE pk.PkPStPov = @stpov
                ORDER BY pk.PkPSifra", conn);

            da.SelectCommand.Parameters.AddWithValue("@stpov", stPov);
            DataTable dt = new DataTable();
            da.Fill(dt);

            if (!dt.Columns.Contains("Izaberi"))
            {
                DataColumn col = new DataColumn("Izaberi", typeof(bool)) { DefaultValue = false };
                dt.Columns.Add(col);
                dt.Columns["Izaberi"].SetOrdinal(0);
            }

            _dtPutevi = dt;
            BindPuteviGrid();
        }

        private void BindPuteviGrid()
        {
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.Columns.Clear();

            DataGridViewCheckBoxColumn colChk = new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "Izaberi",
                HeaderText = "",
                Name = "colIzaberi",
                Width = 30
            };
            DataGridViewTextBoxColumn colSifra = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "PkPSifra",
                HeaderText = "Šifra puta",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            };
            DataGridViewTextBoxColumn colNaziv = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "MpNaziv",
                HeaderText = "Naziv puta",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

            dataGridView1.Columns.AddRange(colChk, colSifra, colNaziv);
            dataGridView1.DataSource = _dtPutevi;

            dataGridView1.CurrentCellDirtyStateChanged -= Dgv1_CurrentCellDirtyStateChanged;
            dataGridView1.CurrentCellDirtyStateChanged += Dgv1_CurrentCellDirtyStateChanged;

            // malo stila
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
        }

        private void Dgv1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellDirty)
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        public List<(int PkPSifra, string MpNaziv)> GetSelektovaniPutevi()
        {
            List<(int, string)> list = new List<(int, string)>();
            if (_dtPutevi == null) return list;

            foreach (DataRow r in _dtPutevi.Rows)
            {
                bool sel = r.Field<bool>("Izaberi");
                if (sel)
                {
                    int sifra = Convert.ToInt32(r["PkPSifra"]);
                    string naziv = Convert.ToString(r["MpNaziv"]);
                    list.Add((sifra, naziv));
                }
            }
            return list;
        }

        #endregion

        #region Retrain UI

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
                percent = Math.Max(0, Math.Min(100, percent));
                _bar.Style = ProgressBarStyle.Blocks;
                _bar.Value = percent;
            }
        }

        public async void Retrain()
        {
            ProgressDialog dlg = new ProgressDialog("Priprema...");
            dlg.Show(this);

            cts = new CancellationTokenSource();

            string session = TrainLogger.StartSession();
            TrainLogger.Info($"ModelPath={modelPath}");

            Progress<TrainProgress> progress = new Progress<TrainProgress>(p =>
            {
                dlg.SetProgress(p.Percent);
                dlg.SetText(string.IsNullOrWhiteSpace(p.Message) ? p.Stage : $"{p.Stage} — {p.Message}");
                TrainLogger.Progress(p);
            });

            try
            {
                TrainLogger.Info("Time model: training started");
                var metricsTime = await GlobalTrainer.TrainAndSaveAsync(connection, modelPath, progress, cts.Token);
                TrainLogger.Info("Time model: training finished: " + metricsTime);

                // best-effort stop modeli
                try
                {
                    string binPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stop_bin.zip");
                    string reasonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stop_reason.zip");
                    string waitPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stop_wait.zip");
                    var _ = await StopTrainer.TrainAllAsync(connection, binPath, reasonPath, waitPath, progress, cts.Token);
                }
                catch (Exception exStop)
                {
                    TrainLogger.Error("Stop models: FAILED", exStop);
                }

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
                TrainLogger.EndSession(session);
            }
        }

        private void retrenirajToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Retrain();
        }

        #endregion

        #region Istorijski model trajanja (median po trasi) i sabiranje po sekvenci

        /// DISTINCT RN za izabrane prevozne puteve.
        private List<int> FetchRnIdsForSelectedRoutes(int minNajavaId = 239999)
        {
            var putevi = GetSelektovaniPutevi();
            var sifre = putevi.Select(p => p.PkPSifra).Distinct().ToList();
            if (sifre.Count == 0) return new List<int>();

            string[] paramNames = sifre.Select((v, i) => "@p" + i).ToArray();
            string inClause = string.Join(",", paramNames);

            string sql = @"
SELECT DISTINCT v.IDRadnogNaloga
FROM Najava n
JOIN MaticniPodatki mp ON RTRIM(n.PrevozniPut) = RTRIM(mp.MpNaziv)
LEFT JOIN RadniNalogVezaNajave v ON v.IDNajave = n.ID
WHERE n.ID > @minId
  AND mp.MpSifra IN (" + inClause + @")
  AND v.IDRadnogNaloga IS NOT NULL;";

            List<int> list = new List<int>();
            SqlConnection conn = new SqlConnection(connection);
            SqlCommand cmd = new SqlCommand(sql, conn);
            conn.Open();
            cmd.Parameters.AddWithValue("@minId", minNajavaId);
            for (int i = 0; i < sifre.Count; i++) cmd.Parameters.AddWithValue(paramNames[i], sifre[i]);

            SqlDataReader rd = cmd.ExecuteReader();
            while (rd.Read()) list.Add(Convert.ToInt32(rd[0]));
            rd.Close();
            conn.Close();

            return list;
        }

        /// RN -> uređena lista IDTrase (RB=1..N). RB=0 šaljemo na kraj; fallback na datume ako postoje; tie-breaker IDTrase.
        private Dictionary<int, List<int>> FetchRouteSeqForRn(List<int> rnIds)
        {
            Dictionary<int, List<int>> map = new Dictionary<int, List<int>>();
            if (rnIds == null || rnIds.Count == 0) return map;

            string[] p = rnIds.Select((v, i) => "@p" + i).ToArray();
            string inClause = string.Join(",", p);

            string sql = @"
WITH X AS (
  SELECT
    r.IDRadnogNaloga,
    r.IDTrase,
    RBnorm = ROW_NUMBER() OVER (
               PARTITION BY r.IDRadnogNaloga
               ORDER BY 
                 CASE WHEN ISNULL(r.RB,0)=0 THEN 1 ELSE 0 END,
                 r.RB,
                 COALESCE(r.DatumPolaskaReal, r.DatumPolaska),
                 r.IDTrase
             )
  FROM RadniNalogTrase r
  WHERE r.IDRadnogNaloga IN (" + inClause + @")
)
SELECT IDRadnogNaloga, IDTrase, RBnorm
FROM X
ORDER BY IDRadnogNaloga, RBnorm;";

            SqlConnection conn = new SqlConnection(connection);
            SqlCommand cmd = new SqlCommand(sql, conn);
            for (int i = 0; i < rnIds.Count; i++) cmd.Parameters.AddWithValue(p[i], rnIds[i]);
            conn.Open();

            SqlDataReader rd = cmd.ExecuteReader();
            int currRn = -1; List<int> curr = null;
            while (rd.Read())
            {
                int rn = Convert.ToInt32(rd["IDRadnogNaloga"]);
                int idTrase = Convert.ToInt32(rd["IDTrase"]);
                if (rn != currRn)
                {
                    if (currRn != -1 && curr != null) map[currRn] = curr;
                    currRn = rn; curr = new List<int>();
                }
                curr.Add(idTrase);
            }
            if (currRn != -1 && curr != null) map[currRn] = curr;

            rd.Close(); conn.Close();
            return map;
        }

        private static double MedianIntList(IList<int> xs)
        {
            if (xs == null || xs.Count == 0) return 0.0;
            int[] arr = xs.OrderBy(v => v).ToArray();
            int n = arr.Length;
            if ((n & 1) == 1) return arr[n / 2];
            return (arr[n / 2 - 1] + arr[n / 2]) / 2.0;
        }

        /// Učitaj median minute po IDTrase (filtrira 1900-01-01 i out-of-range trajanja).
        private Dictionary<int, (double medianMin, int samples)> LoadRouteMedianMinutes(IEnumerable<int> routeIds, int maxMin = 2880)
        {
            Dictionary<int, (double, int)> res = new Dictionary<int, (double, int)>();
            List<int> ids = routeIds?.Distinct().ToList() ?? new List<int>();
            if (ids.Count == 0) return res;

            List<string> ps = ids.Select((v, i) => "@p" + i).ToList();
            string inClause = string.Join(",", ps);

            string sql = @"
SELECT r.IDTrase,
       DATEDIFF(MINUTE, r.DatumPolaskaReal, r.DatumDolaskaReal) AS Minuta
FROM RadniNalogTrase r
WHERE r.IDTrase IN (" + inClause + @")
  AND r.DatumPolaskaReal IS NOT NULL
  AND r.DatumDolaskaReal IS NOT NULL
  AND r.DatumPolaskaReal > '1900-01-01'
  AND r.DatumDolaskaReal > '1900-01-01'
  AND DATEDIFF(MINUTE, r.DatumPolaskaReal, r.DatumDolaskaReal) BETWEEN 1 AND @maxMin;";

            Dictionary<int, List<int>> buckets = new Dictionary<int, List<int>>();

            SqlConnection conn = new SqlConnection(connection);
            SqlCommand cmd = new SqlCommand(sql, conn);
            for (int i = 0; i < ids.Count; i++) cmd.Parameters.AddWithValue(ps[i], ids[i]);
            cmd.Parameters.AddWithValue("@maxMin", maxMin);
            conn.Open();

            SqlDataReader rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                int idTrase = Convert.ToInt32(rd["IDTrase"]);
                int min = Convert.ToInt32(rd["Minuta"]);
                if (!buckets.TryGetValue(idTrase, out List<int> list))
                {
                    list = new List<int>();
                    buckets[idTrase] = list;
                }
                list.Add(min);
            }
            rd.Close(); conn.Close();

            foreach (var kv in buckets)
            {
                double med = MedianIntList(kv.Value);
                res[kv.Key] = (med, kv.Value.Count);
            }

            return res;
        }

        private static string MakeRouteKey(List<int> seq)
            => (seq == null || seq.Count == 0) ? "" : string.Join("|", seq);

        private void IzracunajIstorijskePredikcijePoSekvencama()
        {
            var rnIds = FetchRnIdsForSelectedRoutes(minNajavaId: 239999);
            if (rnIds.Count == 0) { dataGridView2.DataSource = null; MessageBox.Show("Nema RN za izabrane puteve."); return; }

            var rnToSeq = FetchRouteSeqForRn(rnIds);
            if (rnToSeq.Count == 0) { dataGridView2.DataSource = null; MessageBox.Show("Nema trasa za izabrane RN."); return; }

            // deduplikacija po identičnoj sekvenci trasa
            Dictionary<string, int> repByKey = new Dictionary<string, int>();
            foreach (var kv in rnToSeq)
            {
                string key = MakeRouteKey(kv.Value);
                if (!string.IsNullOrEmpty(key) && !repByKey.ContainsKey(key))
                    repByKey[key] = kv.Key;
            }

            // median po trasi
            List<int> allRoutes = rnToSeq.Values.SelectMany(v => v).Distinct().ToList();
            var medByRoute = LoadRouteMedianMinutes(allRoutes, maxMin: 2880);
            double globalFallback = medByRoute.Count > 0 ? medByRoute.Values.Select(v => v.medianMin).Median() : 0.0;

            DataTable dt = new DataTable();
            dt.Columns.Add("IDRadnogNaloga", typeof(int));
            dt.Columns.Add("TrajanjeMin", typeof(double));

            foreach (var kv in repByKey)
            {
                int rn = kv.Value;
                List<int> seq = rnToSeq[rn];

                double total = 0.0;
                foreach (int idTrase in seq)
                {
                    if (medByRoute.TryGetValue(idTrase, out var stat))
                        total += stat.medianMin;
                    else
                        total += globalFallback; // ili 0 ako baš želiš preskočiti bez istorije
                }

                DataRow row = dt.NewRow();
                row["IDRadnogNaloga"] = rn;
                row["TrajanjeMin"] = Math.Round(total, 1);
                dt.Rows.Add(row);
            }

            dataGridView2.AutoGenerateColumns = true;
            dataGridView2.DataSource = dt;
            dataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            // stil i bojenje
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

            ColorizeGrid2ByMeanDistance();
            UpdateLblVremeFromGrid2();
            if (lblRNVreme != null) lblRNVreme.Visible = true;
        }

        private void odabirRNToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                IzracunajIstorijskePredikcijePoSekvencama();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška: " + ex.Message, "Ponuda", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static Color LerpColor(Color a, Color b, double t)
        {
            if (t < 0) t = 0; else if (t > 1) t = 1;
            int r = (int)Math.Round(a.R + (b.R - a.R) * t);
            int g = (int)Math.Round(a.G + (b.G - a.G) * t);
            int bb = (int)Math.Round(a.B + (b.B - a.B) * t);
            return Color.FromArgb(r, g, bb);
        }

        private double ColorizeGrid2ByMeanDistance()
        {
            DataGridView dgv = dataGridView2;
            const string colName = "TrajanjeMin";

            DataTable dt = dgv.DataSource as DataTable;
            if (dt == null || !dt.Columns.Contains(colName)) return double.NaN;

            List<double> vals = dt.AsEnumerable()
                                  .Where(r => r[colName] != DBNull.Value)
                                  .Select(r => Convert.ToDouble(r[colName]))
                                  .ToList();
            if (vals.Count == 0) return double.NaN;

            double mean = vals.Average();
            double maxDev = vals.Max(v => Math.Abs(v - mean));

            Color cMin = Color.FromArgb(70, 180, 70);   // jača zelena (najbliže proseku)
            Color cMax = Color.FromArgb(215, 65, 65);   // jača crvena (najdalje)

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;
                object o = row.Cells[colName].Value;
                if (o == null || o == DBNull.Value) continue;

                double v = Convert.ToDouble(o);
                double dev = Math.Abs(v - mean);
                double t = (maxDev > 0) ? (dev / maxDev) : 0.0;

                Color back = LerpColor(cMin, cMax, t);
                row.DefaultCellStyle.BackColor = back;
                row.DefaultCellStyle.SelectionBackColor = ControlPaint.Dark(back, 0.15f);
                row.DefaultCellStyle.SelectionForeColor = Color.Black;
                row.DefaultCellStyle.ForeColor = Color.Black;
            }
            return mean;
        }

        private void UpdateLblVremeFromGrid2()
        {
            const string colVal = "TrajanjeMin";
            const string colRn = "IDRadnogNaloga";

            DataTable dt = dataGridView2.DataSource as DataTable;
            if (dt == null || !dt.Columns.Contains(colVal) || !dt.Columns.Contains(colRn))
            {
                if (lblRNVreme != null) lblRNVreme.Visible = false;
                return;
            }

            var rows = dt.AsEnumerable()
                         .Where(r => r[colVal] != DBNull.Value && r[colRn] != DBNull.Value)
                         .ToList();
            if (rows.Count == 0)
            {
                if (lblRNVreme != null) lblRNVreme.Visible = false;
                return;
            }

            double mean = rows.Average(r => Convert.ToDouble(r[colVal]));
            DataRow best = rows.OrderBy(r => Math.Abs(Convert.ToDouble(r[colVal]) - mean)).First();

            RN = Convert.ToInt32(best[colRn]);
            VremeRN = Convert.ToDouble(best[colVal]);

            if (lblRNVreme != null)
            {
                lblRNVreme.Text = $"Najbolje vreme: {Math.Round(VremeRN, 0)} min, RN:{RN}";
                lblRNVreme.Visible = true;
            }
        }

        private void dataGridView2_SelectionChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                if (row.Selected && !row.IsNewRow)
                {
                    RN = Convert.ToInt32(row.Cells["IDRadnogNaloga"].Value);
                    VremeRN = Convert.ToDouble(row.Cells["TrajanjeMin"].Value);
                    VratiTraseRN();
                }
            }
        }

        private void otvoriRNToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmRadniNalog frm = new frmRadniNalog(RN.ToString());
            frm.Show();
        }

        private void VratiTraseRN()
        {
            string query = "Select RB,IDTrase,RTrim(s1.Opis) as StanicaOD,Rtrim(s2.Opis) as StanicaDo,Cena " +
                "From RadniNalogTrase " +
                "inner join stanice as s1 on RadniNalogTrase.StanicaOd = s1.ID " +
                "inner join stanice as s2 on RadniNalogTrase.StanicaDo = s2.ID " +
                "inner join Trase on RadniNalogTrase.IDTrase = Trase.ID " +
                "Where IDRadnogNaloga = " + RN + " order by RB asc";

            SqlConnection conn = new SqlConnection(connection);
            SqlDataAdapter da = new SqlDataAdapter(query, conn);
            DataSet ds = new DataSet();
            da.Fill(ds);
            dataGridView3.ReadOnly = true;
            dataGridView3.DataSource = ds.Tables[0];

            if (dataGridView3.Columns.Count >= 5)
            {
                dataGridView3.Columns[0].Width = 40; // RB
                dataGridView3.Columns[1].Width = 50; // IDTrase
                dataGridView3.Columns[2].Width = 120; // Od
                dataGridView3.Columns[3].Width = 120; // Do
                dataGridView3.Columns[4].Width = 80;  // Cena
            }

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

        #endregion

        #region Aktivnosti (prijem/predaja) – median sati + cena (ObracunPoSatu i broj kola)

        private List<int> GetCheckedIds(CheckedListBox clb)
        {
            List<int> ids = new List<int>();
            foreach (object it in clb.CheckedItems)
            {
                DataRowView drv = it as DataRowView;
                if (drv != null) ids.Add(Convert.ToInt32(drv["ID"]));
                else if (it is int id) ids.Add(id);
            }
            return ids;
        }

        // MEDIJAN sati po aktivnosti (u SATIMA). Sati=0/NULL se preskaču.
        private Dictionary<int, double> LoadActivityMedianHoursById(IEnumerable<int> ids)
        {
            Dictionary<int, double> map = new Dictionary<int, double>();
            List<int> list = ids?.Distinct().ToList() ?? new List<int>();
            if (list.Count == 0) return map;

            string inList = string.Join(",", list);
            string sql = @"
SELECT VrstaAktivnostiID, Sati
FROM AktivnostiStavke
WHERE Sati IS NOT NULL AND Sati > 0
  AND VrstaAktivnostiID IN (" + inList + ")";

            Dictionary<int, List<double>> buckets = new Dictionary<int, List<double>>();
            SqlConnection cn = new SqlConnection(connection);
            SqlCommand cmd = new SqlCommand(sql, cn);
            cn.Open();
            SqlDataReader rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                int id = Convert.ToInt32(rd["VrstaAktivnostiID"]);
                double h = Convert.ToDouble(rd["Sati"]);
                if (!buckets.TryGetValue(id, out List<double> lst)) { lst = new List<double>(); buckets[id] = lst; }
                lst.Add(h);
            }
            rd.Close(); cn.Close();

            foreach (var kv in buckets)
            {
                List<double> v = kv.Value; v.Sort();
                double med = 0; int n = v.Count;
                if (n > 0) med = (n % 2 == 1) ? v[n / 2] : (v[n / 2 - 1] + v[n / 2]) / 2.0;
                map[kv.Key] = med;      // sati
            }
            foreach (int id in list) if (!map.ContainsKey(id)) map[id] = 0.0;
            return map;
        }

        // Cene po aktivnosti + ObracunPoSatu flag
        private Dictionary<int, (decimal cena, bool poSatu)> LoadActivityPricingById(IEnumerable<int> ids)
        {
            Dictionary<int, (decimal cena, bool poSatu)> map = new Dictionary<int, (decimal cena, bool poSatu)>();
            List<int> list = ids?.Distinct().ToList() ?? new List<int>();
            if (list.Count == 0) return map;

            string inList = string.Join(",", list);
            string sql = @"
SELECT 
    ID, 
    CAST(Cena AS DECIMAL(18,2)) AS Cena,
    CASE WHEN ISNULL(ObracunPoSatu,1)=1 THEN 1 ELSE 0 END AS PoSatu
FROM VrstaAktivnosti
WHERE ID IN (" + inList + ")";

            SqlConnection cn = new SqlConnection(connection);
            SqlDataAdapter da = new SqlDataAdapter(sql, cn);
            DataTable dt = new DataTable();
            da.Fill(dt);

            foreach (DataRow r in dt.Rows)
            {
                int id = Convert.ToInt32(r["ID"]);
                decimal cena = r["Cena"] == DBNull.Value ? 0m : Convert.ToDecimal(r["Cena"]);
                bool poSatu = Convert.ToInt32(r["PoSatu"]) == 1;
                map[id] = (cena: cena, poSatu: poSatu);
            }
            foreach (int id in list) if (!map.ContainsKey(id)) map[id] = (cena: 0m, poSatu: true);
            return map;
        }

        private void IzracunajAktivnostiPrijemPredaja()
        {
            List<int> prijemIds = GetCheckedIds(chkListPrijem);
            List<int> predajaIds = GetCheckedIds(chkListPredaja);

            // broj kola (za po-kolu obračun)
            int brojKola = 0;
            if (!string.IsNullOrWhiteSpace(textBox2.Text))
            {
                int.TryParse(textBox2.Text, out brojKola);
                if (brojKola < 0) brojKola = 0;
            }

            // PRIJEM
            var prijemHrs = LoadActivityMedianHoursById(prijemIds);               // sati (median)
            var prijemPrice = LoadActivityPricingById(prijemIds);                 // cena, poSatu
            double sumH1 = 0; decimal sumC1 = 0m;

            foreach (int id in prijemIds.Distinct())
            {
                double h = prijemHrs.TryGetValue(id, out double hv) ? hv : 0.0;
                var pr = prijemPrice.TryGetValue(id, out var pv) ? pv : (cena: 0m, poSatu: true);

                if (pr.poSatu)
                {
                    // klasično: sati * cena
                    sumH1 += h;
                    sumC1 += (decimal)h * pr.cena;
                }
                else
                {
                    // po kolima: vreme i cena su "po kolu"
                    double effH = h * Math.Max(0, brojKola);
                    decimal effC = pr.cena * Math.Max(0, brojKola);
                    sumH1 += effH;
                    sumC1 += effC;
                }
            }

            // PREDAJA
            var predajaHrs = LoadActivityMedianHoursById(predajaIds);
            var predajaPrice = LoadActivityPricingById(predajaIds);
            double sumH2 = 0; decimal sumC2 = 0m;

            foreach (int id in predajaIds.Distinct())
            {
                double h = predajaHrs.TryGetValue(id, out double hv) ? hv : 0.0;
                var pr = predajaPrice.TryGetValue(id, out var pv) ? pv : (cena: 0m, poSatu: true);

                if (pr.poSatu)
                {
                    sumH2 += h;
                    sumC2 += (decimal)h * pr.cena;
                }
                else
                {
                    double effH = h * Math.Max(0, brojKola);
                    decimal effC = pr.cena * Math.Max(0, brojKola);
                    sumH2 += effH;
                    sumC2 += effC;
                }
            }

            // upis poruka (primeri iz zahteva)
            if (lblVremeAktivnostiPrijema != null)
                lblVremeAktivnostiPrijema.Text = $"Ukupno sati aktivnosti prijema:{Math.Round(sumH1, 2):0.##}";
            if (lblTrosakAktivnostiPrijema != null)
                lblTrosakAktivnostiPrijema.Text = $"Ukupna cena aktivnosti prijema:{Math.Round(sumC1, 2):#0.##}";
            if (lblVremeAktivnostiPredaje != null)
                lblVremeAktivnostiPredaje.Text = $"Ukupno sati aktivnosti predaje:{Math.Round(sumH2, 2):0.##}";
            if (lblTrosakAktivnostiPredaje != null)
                lblTrosakAktivnostiPredaje.Text = $"Ukupna cena aktivnosti predaje:{Math.Round(sumC2, 2):#0.##}";

            // ukupno vreme prevoza (sati): RN(min)/60 + prijem + predaja
            double ukupnoVremeH = (VremeRN / 60.0) + sumH1 + sumH2;
            if (lblVremePuta != null)
                lblVremePuta.Text = "Ukupno vreme prevoza:" + Math.Round(ukupnoVremeH, 2).ToString() + "h";

            // Ukupan trošak (primer)
            string valuta = comboBox2.Text.ToString().Trim();
            decimal kurs = 1m;
            if (comboBox2.SelectedValue != null)
                decimal.TryParse(comboBox2.SelectedValue.ToString(), out kurs);

            decimal cenaTrase = 0m;   // suma cena trasa iz grid-a, konverzija po kursu
            foreach (DataGridViewRow row in dataGridView3.Rows)
            {
                if (!row.IsNewRow && row.Cells.Count >= 5 && row.Cells[4].Value != null && row.Cells[4].Value != DBNull.Value)
                    cenaTrase += Convert.ToDecimal(row.Cells[4].Value);
            }
            if (kurs > 0) cenaTrase = cenaTrase / kurs;

            double satnica = 0.0;
            if (!string.IsNullOrWhiteSpace(textBox1.Text)) double.TryParse(textBox1.Text, out satnica);

            double cenaPutaRad = (VremeRN / 60.0) * satnica;
            double ukupanTrosak = Convert.ToDouble(cenaTrase) + cenaPutaRad + Convert.ToDouble(sumC1) + Convert.ToDouble(sumC2);

            if (lblTrosakPuta != null)
                lblTrosakPuta.Text = "Ukupan trosak prevoza:" + Math.Round(ukupanTrosak, 2).ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show("Mora se upisati cena sata!");
                return;
            }
            IzracunajAktivnostiPrijemPredaja();
        }

        #endregion

        private void izracunajPonuduToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }

    // ekstenzija za median nad IEnumerable<double>
    internal static class LinqStatsExt
    {
        public static double Median(this IEnumerable<double> source)
        {
            double[] arr = source?.OrderBy(x => x).ToArray() ?? Array.Empty<double>();
            int n = arr.Length;
            if (n == 0) return 0.0;
            if ((n & 1) == 1) return arr[n / 2];
            return (arr[n / 2 - 1] + arr[n / 2]) / 2.0;
        }
    }
}
