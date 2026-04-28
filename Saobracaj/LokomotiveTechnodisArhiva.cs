using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Saobracaj
{
    [ComVisible(true)]
    public partial class LokomotiveTechnodisArhiva : Form
    {
        private readonly string connection =
            ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;

        private readonly HttpClient client = new HttpClient();
        private string token;

        private string DatumOD;
        private string DatumDO;
        private string Lokomotiva;

        private List<Record> _records = new List<Record>();

        public LokomotiveTechnodisArhiva()
        {
            InitializeComponent();
            SetWebBrowserVersion(11001); // IE11
            webBrowser1.ScriptErrorsSuppressed = true;
            webBrowser1.ObjectForScripting = this;
        }

        private void LoadMap()
        {
            string fullPath = Path.Combine(Application.StartupPath, "mapTechnodis.html");

            if (!File.Exists(fullPath))
            {
                MessageBox.Show("mapTechnodis.html not found at: " + fullPath);
                return;
            }

            string fileUrl = new Uri(fullPath).AbsoluteUri;
            webBrowser1.Navigate(fileUrl);
        }

        private void SetWebBrowserVersion(int version)
        {
            const string keyName = @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";
            string appName = Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

            Microsoft.Win32.Registry.SetValue(
                Microsoft.Win32.Registry.CurrentUser + "\\" + keyName,
                appName,
                version,
                Microsoft.Win32.RegistryValueKind.DWord);
        }

        private void LokomotiveTechnodisArhiva_Load(object sender, EventArgs e)
        {
            VratiLokomotive();
            LoadMap();

            dateTimePicker1.Value = DateTime.Now.AddHours(-1);
            dateTimePicker2.Value = DateTime.Now;

            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.ReadOnly = true;
        }

        private void VratiLokomotive()
        {
            var select = "Select Lokomotiva,IDLokomotive from LocoTechnodis order by Lokomotiva asc";

            using (SqlConnection conn = new SqlConnection(connection))
            {
                var da = new SqlDataAdapter(select, conn);
                var ds = new DataSet();
                da.Fill(ds);

                comboBox1.DataSource = ds.Tables[0];
                comboBox1.DisplayMember = "Lokomotiva";
                comboBox1.ValueMember = "IDLokomotive";
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                DatumOD = dateTimePicker1.Value.ToString("yyyy-MM-ddTHH:mm:ss");
                DatumDO = dateTimePicker2.Value.ToString("yyyy-MM-ddTHH:mm:ss");
                Lokomotiva = comboBox1.SelectedValue.ToString();

                await ReadToken();
                _records = await ReadData(Lokomotiva);

                var prikaz = _records
                    .OrderByDescending(x => x.Recordtime)
                    .Select(x => new
                    {
                        IdRecords = x.IdRecords,
                        RecordTime = x.Recordtime,
                        Altitude = x.Altitude,
                        SV = x.SV,
                        MainVoltage = x.Mainvoltage,
                        Speed = x.Speed,
                        Direction = x.Direction
                    })
                    .ToList();

                dataGridView1.AutoGenerateColumns = true;
                dataGridView1.DataSource = prikaz;

                if (dataGridView1.Columns["IdRecords"] != null)
                    dataGridView1.Columns["IdRecords"].Visible = false;

                if (dataGridView1.Columns["RecordTime"] != null)
                {
                    dataGridView1.Columns["RecordTime"].HeaderText = "Vreme";
                    dataGridView1.Columns["RecordTime"].DefaultCellStyle.Format = "dd.MM.yyyy HH:mm:ss";
                }

                if (dataGridView1.Columns["MainVoltage"] != null)
                    dataGridView1.Columns["MainVoltage"].HeaderText = "Main Voltage";

                DrawRouteOnMap();

                if (dataGridView1.Rows.Count > 0)
                {
                    dataGridView1.ClearSelection();
                    dataGridView1.Rows[0].Selected = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška prilikom učitavanja podataka: " + ex.Message);
            }
        }

        private async Task ReadToken()
        {
            try
            {
                string urlToken = "http://85.25.177.168/egoidentityserver/connect/token";

                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("scope", "MyApi"),
                    new KeyValuePair<string, string>("client_id", "lokomotive"),
                    new KeyValuePair<string, string>("client_secret", "loccoo#moto147")
                });

                HttpResponseMessage responseToken = await client.PostAsync(urlToken, formData);

                if (!responseToken.IsSuccessStatusCode)
                    throw new HttpRequestException($"Failed to obtain access token. Status code: {responseToken.StatusCode}");

                string responseText = await responseToken.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(responseText);
                token = data.access_token;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška prilikom generisanja tokena!\n" + ex);
            }
        }

        private async Task<List<Record>> ReadData(string lokomotivaId)
        {
            string apiEndpoint = "http://85.25.177.168/gpstrackjourney/api/journey/records";

            var resp = new MultipartFormDataContent
            {
                { new StringContent(lokomotivaId), "vehicles" },
                { new StringContent(DatumOD), "from" },
                { new StringContent(DatumDO), "to" }
            };

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, apiEndpoint))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                request.Content = resp;

                HttpResponseMessage response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return new List<Record>();

                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Record>>(content) ?? new List<Record>();
            }
        }

        private void DrawRouteOnMap()
        {
            try
            {
                if (webBrowser1.Document == null)
                    return;

                var points = _records
                    .Where(x => x.Latitude != 0 && x.Longitude != 0)
                    .OrderBy(x => x.Recordtime)
                    .Select(x => new
                    {
                        Latitude = x.Latitude,
                        Longitude = x.Longitude
                    })
                    .ToList();

                string json = JsonConvert.SerializeObject(points);
                webBrowser1.Document.InvokeScript("drawRouteFromJson", new object[] { json });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri crtanju rute na mapi: " + ex.Message);
            }
        }

        private void FocusSelectedPointOnMap(decimal latitude, decimal longitude)
        {
            try
            {
                if (webBrowser1.Document == null)
                    return;

                webBrowser1.Document.InvokeScript("focusPoint", new object[]
                {
                    latitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri fokusiranju tačke na mapi: " + ex.Message);
            }
        }

        private LocoData ParseSs(string ss, string nazivLokomotive, DateTime? recordTime, string speed)
        {
            if (string.IsNullOrWhiteSpace(ss))
            {
                return new LocoData
                {
                    LokomotivaNaziv = nazivLokomotive,
                    RecordTime = recordTime,
                    Speed = string.IsNullOrWhiteSpace(speed) ? "0000" : speed,
                    ThrottlePosition = "000",
                    TargetPower = "00000",
                    TracPower = "00000",
                    Engine1State = "000",
                    Engine2State = "000",
                    Engine1Rpm = "00000",
                    Engine2Rpm = "00000",
                    Engine1WorkHours = "00000",
                    Engine2WorkHours = "00000",
                    Engine1WaterTemp = "000",
                    Engine2WaterTemp = "000",
                    Engine1OilTemp = "000",
                    Engine2OilTemp = "000",
                    ActiveFaultCount = "000",
                    ActiveFault1 = "00000",
                    ActiveFault2 = "00000",
                    ActiveFault3 = "00000",
                    ActiveFault4 = "00000",
                    ActiveFault5 = "00000",
                    FaultSync = "000",
                    FaultAck = "000"
                };
            }

            return new LocoData
            {
                LokomotivaNaziv = nazivLokomotive,
                RecordTime = recordTime,
                Speed = speed,
                ThrottlePosition = ss.Substring(45, 3),
                TargetPower = ss.Substring(53, 5),
                TracPower = ss.Substring(58, 5),
                Engine1State = ss.Substring(138, 3),
                Engine1Rpm = ss.Substring(141, 5),
                Engine1WorkHours = ss.Substring(146, 5),
                Engine1WaterTemp = ss.Substring(151, 5),
                Engine1OilTemp = ss.Substring(156, 5),
                Engine2State = ss.Substring(171, 3),
                Engine2Rpm = ss.Substring(174, 5),
                Engine2WorkHours = ss.Substring(179, 5),
                Engine2WaterTemp = ss.Substring(184, 5),
                Engine2OilTemp = ss.Substring(189, 5),
                ActiveFaultCount = ss.Substring(204, 3),
                ActiveFault1 = ss.Substring(207, 5),
                ActiveFault2 = ss.Substring(212, 5),
                ActiveFault3 = ss.Substring(217, 5),
                ActiveFault4 = ss.Substring(222, 5),
                ActiveFault5 = ss.Substring(227, 5),
                FaultSync = ss.Substring(232, 3),
                FaultAck = ss.Substring(235, 3)
            };
        }

        private string GetEngineStateText(string state)
        {
            switch (Convert.ToInt32(state))
            {
                case 0: return "Shutdown";
                case 1: return "Pre crank";
                case 2: return "Crank warning";
                case 3: return "Cranking";
                case 4: return "Running";
                case 5: return "Disabled";
                default: return "Shutdown";
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.SelectedRows.Count == 0)
                    return;

                var selectedRow = dataGridView1.SelectedRows[0];

                if (selectedRow.Cells["IdRecords"].Value == null)
                    return;

                long idRecords = Convert.ToInt64(selectedRow.Cells["IdRecords"].Value);

                Record selectedRecord = _records.FirstOrDefault(x => x.IdRecords == idRecords);
                if (selectedRecord == null)
                    return;

                string lokomotivaNaziv = comboBox1.Text;

                LocoData data = ParseSs(
                    selectedRecord.SS,
                    lokomotivaNaziv,
                    selectedRecord.Recordtime,
                    selectedRecord.Speed
                );

                lblPrva.Text = lokomotivaNaziv;
                lblVremePrva.Text = selectedRecord.Recordtime.ToString("dd.MM.yyyy HH:mm:ss");
                prvaThrottleGauge.Value = data.ThrottlePosition;
                prvaTargetPowerGauge.Value = data.TargetPower;
                prvaTracPowerGauge.Value = data.TracPower;
                lblPrva1State.Text = GetEngineStateText(data.Engine1State);
                lblPrva2State.Text = GetEngineStateText(data.Engine2State);
                prva1WorkHoursGauge.Value = data.Engine1WorkHours;
                prva2WorkHoursGauge.Value = data.Engine2WorkHours;
                prva1RPMGauge.Value = data.Engine1Rpm;
                prva2RPMGauge.Value = data.Engine2Rpm;
                prva1WaterTGauge.Value = data.Engine1WaterTemp;
                prva2WaterTGauge.Value = data.Engine2WaterTemp;
                lblPrvaFaultCount.Text = "Active faults count: " + data.ActiveFaultCount;
                activeFault1GaugePrva.Value = data.ActiveFault1;
                activeFault2GaugePrva.Value = data.ActiveFault2;
                activeFault3GaugePrva.Value = data.ActiveFault3;
                activeFault4GaugePrva.Value = data.ActiveFault4;
                activeFault5GaugePrva.Value = data.ActiveFault5;
                faultAckGaugePrva.Value = data.FaultAck;
                faultSyncGaugePrva.Value = data.FaultSync;
                prvaSpeed.Value = data.Speed;

                FocusSelectedPointOnMap(selectedRecord.Latitude, selectedRecord.Longitude);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri prikazu selektovanog reda: " + ex.Message);
            }
        }

        // ostavljeno ako koristiš iz JS preko window.external
        public void ShowTrasa(string trasa)
        {
        }
    }

    public class Record
    {
        public DateTime Recordtime { get; set; }
        public long IdRecords { get; set; }
        public string Vehicles_idVehicles { get; set; }
        public decimal Altitude { get; set; }
        public int SV { get; set; }
        public decimal Mainvoltage { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string Speed { get; set; }
        public decimal Direction { get; set; }
        public string SS { get; set; }
    }

    public class LocoData
    {
        public string LokomotivaNaziv { get; set; }
        public DateTime? RecordTime { get; set; }
        public string ThrottlePosition { get; set; }
        public string TargetPower { get; set; }
        public string TracPower { get; set; }
        public string Engine1State { get; set; }
        public string Engine1Rpm { get; set; }
        public string Engine1WorkHours { get; set; }
        public string Engine1WaterTemp { get; set; }
        public string Engine1OilTemp { get; set; }
        public string Engine2State { get; set; }
        public string Engine2Rpm { get; set; }
        public string Engine2WorkHours { get; set; }
        public string Engine2WaterTemp { get; set; }
        public string Engine2OilTemp { get; set; }
        public string ActiveFaultCount { get; set; }
        public string ActiveFault1 { get; set; }
        public string ActiveFault2 { get; set; }
        public string ActiveFault3 { get; set; }
        public string ActiveFault4 { get; set; }
        public string ActiveFault5 { get; set; }
        public string FaultSync { get; set; }
        public string FaultAck { get; set; }
        public string Speed { get; set; }
    }
}