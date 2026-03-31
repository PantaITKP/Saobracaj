using Newtonsoft.Json;
using Syncfusion.Windows.Forms.Gauge;
using Syncfusion.XlsIO.Parser.Biff_Records;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Saobracaj.Servis
{
    public partial class LokomotiveTechnodis : Form
    {
        private readonly string connection =
            ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
        private readonly HttpClient client = new HttpClient();
        private string token;
        private string fromDate;
        private string toDate;

        public LokomotiveTechnodis()
        {
            InitializeComponent();
        }
        private async void LokomotiveTechnodis_Load(object sender, EventArgs e)
        {
            try
            {
                VratiLokomotive();
                //BuildDynamicColumns();
                await VratiPodatke();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri učitavanju forme: " + ex.Message);
            }

        }


        DataTable loco = new DataTable();
        private void InitTable()
        {
            loco = new DataTable();
            loco.Columns.Add("Lokomotiva", typeof(string));
            loco.Columns.Add("ID", typeof(int));
        }
        private void VratiLokomotive()
        {
            InitTable();
            loco.Rows.Clear();

            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("Select Lokomotiva,IDLokomotive from LocoTechnodis order by IDLokomotive asc", conn))
                {
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            loco.Rows.Add(
                                dr["Lokomotiva"].ToString(),
                                Convert.ToInt32(dr["IDLokomotive"]));
                        }
                    }
                }
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
                MessageBox.Show("Greška prilikom generisanja tokena!\n" + ex.ToString());
            }

        }
        DateTime recordTime;
        private async Task<List<Record>> ReadData(string lokomotivaId)
        {
            string apiEndpoint = "http://85.25.177.168/gpstrackjourney/api/journey/records";

            var resp = new MultipartFormDataContent
            {
                { new StringContent(lokomotivaId), "vehicles" },
                { new StringContent(fromDate), "from" },
                { new StringContent(toDate), "to" }
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
        private async Task VratiPodatke()
        {
            DateTime datDo = DateTime.Now;
            DateTime datOd = datDo.AddSeconds(-10);
            //fromDate = datOd.ToString("yyyy-MM-ddTHH:mm:ss");
            //toDate = datum.ToString("yyyy-MM-ddTHH:mm:ss");
            //tesr

            string datumOd = "2025-06-13 11:14:00";
            string datumDo = "2025-06-13 11:14:10";
            fromDate = datumOd;
            toDate = datumDo;

            await ReadToken();

            int count = 1;

            foreach (DataRow row in loco.Rows)
            {
                string lokomotivaNaziv = row["Lokomotiva"].ToString();
                int lokomotivaID = Convert.ToInt32(row["ID"]);

                List<Record> records = await ReadData(lokomotivaID.ToString());
                Record lastRecord = records
                                   .OrderByDescending(x => x.Recordtime)
                                   .FirstOrDefault();
                LocoData data = ParseSs(lastRecord.SS, lokomotivaNaziv, lastRecord.Recordtime);

                if (count == 1)
                {
                    lblPrva.Text = lokomotivaNaziv;
                    lblVremePrva.Text = lastRecord.Recordtime.ToString();
                    prvaThrottleGauge.Value= data.ThrottlePosition;
                    prvaTargetPowerGauge.Value = data.TargetPower;
                    prvaTracPowerGauge.Value=data.TracPower;
                    lblPrva1State.Text = GetEngineStateText(data.Engine1State);
                    lblPrva2State.Text = GetEngineStateText(data.Engine2State);
                    prva1WorkHoursGauge.Value = data.Engine1WorkHours;
                    prva2WorkHoursGauge.Value = data.Engine2WorkHours;
                    prva1RPMGauge.Value = data.Engine1Rpm;
                    prva2RPMGauge.Value = data.Engine2Rpm;
                    prva1OilTGauge.Value = data.Engine1OilTemp;
                    prva2OilTGauge.Value = data.Engine2OilTemp;
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
                }
                if(count == 2)
                {
                    lblDruga.Text = lokomotivaNaziv;
                    lblVremeDruga.Text = lastRecord.Recordtime.ToString();
                    drugaThrottleGauge.Value = data.ThrottlePosition;
                    drugaTargetPowerGauge.Value = data.TargetPower;
                    drugaTracPowerGauge.Value = data.TracPower;
                    lblDruga1State.Text = GetEngineStateText(data.Engine1State);
                    lblDruga2State.Text = GetEngineStateText(data.Engine2State);
                    druga1WorkHoursGauge.Value = data.Engine1WorkHours;
                    druga2WorkHoursGauge.Value = data.Engine2WorkHours;
                    druga1RPMGauge.Value = data.Engine1Rpm;
                    druga2RPMGauge.Value = data.Engine2Rpm;
                    druga1OilTGauge.Value = data.Engine1OilTemp;
                    druga2OilTGauge.Value = data.Engine2OilTemp;
                    druga1WaterTGauge.Value = data.Engine1WaterTemp;
                    druga2WaterTGauge.Value = data.Engine2WaterTemp;
                    lblDrugaFaultCount.Text = "Active faults count: " + data.ActiveFaultCount;
                    activeFault1GaugeDruga.Value = data.ActiveFault1;
                    activeFault2GaugeDruga.Value = data.ActiveFault2;
                    activeFault3GaugeDruga.Value = data.ActiveFault3;
                    activeFault4GaugeDruga.Value = data.ActiveFault4;
                    activeFault5GaugeDruga.Value = data.ActiveFault5;
                    faultAckGaugeDruga.Value = data.FaultAck;
                    faultSyncGaugeDruga.Value = data.FaultSync;
                }
                if(count==3)
                {
                    lblTreca.Text = lokomotivaNaziv;
                    lblVremeTreca.Text = lastRecord.Recordtime.ToString();
                    trecaThrottleGauge.Value = data.ThrottlePosition;
                    trecaTargetPowerGauge.Value = data.TargetPower;
                    trecaTracPowerGauge.Value = data.TracPower;
                    lblTreca1State.Text = GetEngineStateText(data.Engine1State);
                    lblTreca2State.Text = GetEngineStateText(data.Engine2State);
                    treca1WorkHoursGauge.Value = data.Engine1WorkHours;
                    treca2WorkHoursGauge.Value = data.Engine2WorkHours;
                    treca1RPMGauge.Value = data.Engine1Rpm;
                    treca2RPMGauge.Value = data.Engine2Rpm;
                    treca1OilTGauge.Value = data.Engine1OilTemp;
                    treca2OilTGauge.Value = data.Engine2OilTemp;
                    treca1WaterTGauge.Value = data.Engine1WaterTemp;
                    treca2WaterTGauge.Value = data.Engine2WaterTemp;
                    lblTrecaFaultCount.Text = "Active faults count: " + data.ActiveFaultCount;
                    activeFault1GaugeTreca.Value = data.ActiveFault1;
                    activeFault2GaugeTreca.Value = data.ActiveFault2;
                    activeFault3GaugeTreca.Value = data.ActiveFault3;
                    activeFault4GaugeTreca.Value = data.ActiveFault4;
                    activeFault5GaugeTreca.Value = data.ActiveFault5;
                    faultAckGaugeTreca.Value = data.FaultAck;
                    faultSyncGaugeTreca.Value = data.FaultSync;
                }
                if (count == 4)
                {
                    lblCetvrta.Text = lokomotivaNaziv;
                    lblVremeCetvrta.Text = lastRecord.Recordtime.ToString();
                    cetvrtaThrottleGauge.Value = data.ThrottlePosition;
                    cetvrtaTargetPowerGauge.Value = data.TargetPower;
                    cetvrtaTracPowerGauge.Value = data.TracPower;
                    lblCetvrta1State.Text = GetEngineStateText(data.Engine1State);
                    lblCetvrta2State.Text = GetEngineStateText(data.Engine2State);
                    cetvrta1WorkHoursGauge.Value = data.Engine1WorkHours;
                    cetvrta2WorkHoursGauge.Value = data.Engine2WorkHours;
                    cetvrta1RPMGauge.Value = data.Engine1Rpm;
                    cetvrta2RPMGauge.Value = data.Engine2Rpm;
                    cetvrta1OilTGauge.Value = data.Engine1OilTemp;
                    cetvrta2OilTGauge.Value = data.Engine2OilTemp;
                    cetvrta1WaterTGauge.Value = data.Engine1WaterTemp;
                    cetvrta2WaterTGauge.Value = data.Engine2WaterTemp;
                    lblCetvrtaFaultCount.Text = "Active faults count: " + data.ActiveFaultCount;
                    activeFault1GaugeCetvrta.Value = data.ActiveFault1;
                    activeFault2GaugeCetvrta.Value = data.ActiveFault2;
                    activeFault3GaugeCetvrta.Value = data.ActiveFault3;
                    activeFault4GaugeCetvrta.Value = data.ActiveFault4;
                    activeFault5GaugeCetvrta.Value = data.ActiveFault5;
                    faultAckGaugeCetvrta.Value = data.FaultAck;
                    faultSyncGaugeCetvrta.Value = data.FaultSync;
                }
                count++;

            }
        }
        private LocoData ParseSs(string ss, string nazivLokomotive, DateTime? recordTime)
        {
            if (string.IsNullOrWhiteSpace(ss))
                return new LocoData { LokomotivaNaziv = nazivLokomotive, RecordTime = recordTime };

            return new LocoData
            {
                LokomotivaNaziv = nazivLokomotive,
                RecordTime = recordTime,

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

        /*
        #region UI
        
        private void BuildDynamicColumns()
        {
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel1.Controls.Clear();
            tableLayoutPanel1.ColumnStyles.Clear();
            tableLayoutPanel1.RowStyles.Clear();

            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.ColumnCount = loco.Rows.Count == 0 ? 1 : loco.Rows.Count;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _controlsByLocoId.Clear();

            int count = loco.Rows.Count == 0 ? 1 : loco.Rows.Count;
            float percent = 100f / count;

            for (int i = 0; i < count; i++)
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, percent));

            for (int i = 0; i < loco.Rows.Count; i++)
            {
                DataRow row = loco.Rows[i];
                string naziv = row["Lokomotiva"].ToString();
                int id = Convert.ToInt32(row["ID"]);

                LocoColumnControls controls = CreateLocoColumn(naziv);
                _controlsByLocoId[id] = controls;

                tableLayoutPanel1.Controls.Add(controls.RootPanel, i, 0);
            }

            tableLayoutPanel1.ResumeLayout();
        }
        private Panel CreateSectionPanel(int bottomMargin)
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, bottomMargin)
            };
        }
        private (Panel Container, Label Value) CreateStatePanel(string captionText)
        {
            Label caption = new Label
            {
                Text = captionText,
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                TextAlign = ContentAlignment.BottomLeft,
                ForeColor = Color.Black
            };

            Label value = new Label
            {
                Text = "Shutdown",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.TopLeft,
                ForeColor = Color.Black
            };

            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(4, 2, 4, 2)
            };
            panel.Controls.Add(value);
            panel.Controls.Add(caption);

            return (panel, value);
        }
        private Label CreateMiddleLabel(string text, float fontSize)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", fontSize, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Black
            };
        }

        private Label CreateTopCenteredLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Black
            };
        }

        private enum GaugeKind
        {
            Wide5,
            Small3,
            Single1
        }

        private Panel WrapGauge(Control control, int padding)
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(padding)
            };

            control.Dock = DockStyle.None;
            control.Anchor = AnchorStyles.None;
            panel.Controls.Add(control);
            panel.Resize += (s, e) =>
            {
                control.Left = (panel.ClientSize.Width - control.Width) / 2;
                control.Top = (panel.ClientSize.Height - control.Height) / 2;
            };

            return panel;
        }

        private DigitalGauge CreateDigitalGauge(string initialValue, GaugeKind kind)
        {
            var gauge = new DigitalGauge
            {
                Text = initialValue,
                VisualStyle = Syncfusion.Windows.Forms.Gauge.ThemeStyle.Office2016Colorful,
                //BorderStyle = BorderStyle.None
            };

            switch (kind)
            {
                case GaugeKind.Wide5:
                    gauge.Size = new Size(105, 42);
                    break;
                case GaugeKind.Small3:
                    gauge.Size = new Size(78, 42);
                    break;
                case GaugeKind.Single1:
                    gauge.Size = new Size(54, 42);
                    break;
            }

            return gauge;
        }
        private (Panel Container, DigitalGauge Gauge) CreateFaultGaugeBlock(string caption, string initialValue)
        {
            Panel container = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(4)
            };

            Label lbl = new Label
            {
                Text = caption,
                Dock = DockStyle.Top,
                Height = 16,
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Black
            };

            DigitalGauge gauge = caption == "FaultACK" || caption == "FaultSYNC"
                ? CreateDigitalGauge(initialValue, GaugeKind.Small3)
                : CreateDigitalGauge(initialValue, GaugeKind.Wide5);

            Panel host = WrapGauge(gauge, 2);
            host.Dock = DockStyle.Fill;

            container.Controls.Add(host);
            container.Controls.Add(lbl);

            return (container, gauge);
        }
        private LocoColumnControls CreateLocoColumn(string nazivLokomotive)
        {
            var result = new LocoColumnControls();

            Panel root = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6),
                BackColor = Color.FromArgb(230, 235, 240),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8)
            };

            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7,
                BackColor = Color.Transparent
            };

            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));   // header
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));   // state
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 250F));  // work + rpm
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));   // target/trac/throttle
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 200F));  // water/oil
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));   // faults title
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 210F));  // faults panel

            // HEADER
            TableLayoutPanel tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            Label lblTime = new Label
            {
                Text = "Vreme: -",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblTitle = new Label
            {
                Text = nazivLokomotive,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleRight
            };

            tblHeader.Controls.Add(lblTime, 0, 0);
            tblHeader.Controls.Add(lblTitle, 1, 0);
            main.Controls.Add(tblHeader, 0, 0);

            // STATE
            Panel pnlState = CreateSectionPanel(6);
            TableLayoutPanel tblState = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(6, 4, 6, 2)
            };
            tblState.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblState.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var eng1Panel = CreateStatePanel("Eng 1 state.");
            var eng2Panel = CreateStatePanel("Eng 2 state.");

            tblState.Controls.Add(eng1Panel.Container, 0, 0);
            tblState.Controls.Add(eng2Panel.Container, 1, 0);
            pnlState.Controls.Add(tblState);
            main.Controls.Add(pnlState, 0, 1);

            // WORK + RPM
            Panel pnlWorkRpm = CreateSectionPanel(6);
            TableLayoutPanel tblWorkRpm = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(3)
            };
            tblWorkRpm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
            tblWorkRpm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            tblWorkRpm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
            tblWorkRpm.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tblWorkRpm.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            DigitalGauge gaugeWork1 = CreateDigitalGauge("00000", GaugeKind.Wide5);
            DigitalGauge gaugeWork2 = CreateDigitalGauge("00000", GaugeKind.Wide5);
            DigitalGauge gaugeRpm1 = CreateDigitalGauge("00000", GaugeKind.Wide5);
            DigitalGauge gaugeRpm2 = CreateDigitalGauge("00000", GaugeKind.Wide5);

            tblWorkRpm.Controls.Add(WrapGauge(gaugeWork1, 2), 0, 0);
            tblWorkRpm.Controls.Add(CreateMiddleLabel("Work hours", 11F), 1, 0);
            tblWorkRpm.Controls.Add(WrapGauge(gaugeWork2, 2), 2, 0);

            tblWorkRpm.Controls.Add(WrapGauge(gaugeRpm1, 2), 0, 1);
            tblWorkRpm.Controls.Add(CreateMiddleLabel("RPM", 11F), 1, 1);
            tblWorkRpm.Controls.Add(WrapGauge(gaugeRpm2, 2), 2, 1);

            pnlWorkRpm.Controls.Add(tblWorkRpm);
            main.Controls.Add(pnlWorkRpm, 0, 2);

            // POWER
            Panel pnlPower = CreateSectionPanel(6);
            TableLayoutPanel tblPower = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(4)
            };
            tblPower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
            tblPower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
            tblPower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            tblPower.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tblPower.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label lblTarget = CreateTopCenteredLabel("Target power");
            Label lblTrac = CreateTopCenteredLabel("Trac power");
            Label lblThrottle = CreateTopCenteredLabel("Throttle position");

            DigitalGauge gaugeTarget = CreateDigitalGauge("00000", GaugeKind.Wide5);
            DigitalGauge gaugeTrac = CreateDigitalGauge("00000", GaugeKind.Wide5);
            DigitalGauge gaugeThrottle = CreateDigitalGauge("0", GaugeKind.Single1);

            tblPower.Controls.Add(lblTarget, 0, 0);
            tblPower.Controls.Add(lblTrac, 1, 0);
            tblPower.Controls.Add(lblThrottle, 2, 0);

            tblPower.Controls.Add(WrapGauge(gaugeTarget, 2), 0, 1);
            tblPower.Controls.Add(WrapGauge(gaugeTrac, 2), 1, 1);
            tblPower.Controls.Add(WrapGauge(gaugeThrottle, 2), 2, 1);

            pnlPower.Controls.Add(tblPower);
            main.Controls.Add(pnlPower, 0, 3);

            // TEMPS
            Panel pnlTemps = CreateSectionPanel(6);
            TableLayoutPanel tblTemps = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(4)
            };
            tblTemps.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            tblTemps.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));
            tblTemps.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            tblTemps.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tblTemps.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            DigitalGauge gaugeWater1 = CreateDigitalGauge("000", GaugeKind.Small3);
            DigitalGauge gaugeWater2 = CreateDigitalGauge("000", GaugeKind.Small3);
            DigitalGauge gaugeOil1 = CreateDigitalGauge("000", GaugeKind.Small3);
            DigitalGauge gaugeOil2 = CreateDigitalGauge("000", GaugeKind.Small3);

            tblTemps.Controls.Add(WrapGauge(gaugeWater1, 4), 0, 0);
            tblTemps.Controls.Add(CreateMiddleLabel("Water temperature", 11F), 1, 0);
            tblTemps.Controls.Add(WrapGauge(gaugeWater2, 4), 2, 0);

            tblTemps.Controls.Add(WrapGauge(gaugeOil1, 4), 0, 1);
            tblTemps.Controls.Add(CreateMiddleLabel("Oil temperature", 11F), 1, 1);
            tblTemps.Controls.Add(WrapGauge(gaugeOil2, 4), 2, 1);

            pnlTemps.Controls.Add(tblTemps);
            main.Controls.Add(pnlTemps, 0, 4);

            // FAULT COUNT
            Label lblFaultCount = new Label
            {
                Text = "Active faults count: -",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.Black
            };
            main.Controls.Add(lblFaultCount, 0, 5);

            // FAULTS
            Panel pnlFaults = CreateSectionPanel(4);
            TableLayoutPanel tblFaults = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2,
                Padding = new Padding(4)
            };
            tblFaults.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tblFaults.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tblFaults.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tblFaults.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tblFaults.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tblFaults.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            var fault1 = CreateFaultGaugeBlock("ActiveFault1", "00000");
            var fault2 = CreateFaultGaugeBlock("ActiveFault2", "00000");
            var fault3 = CreateFaultGaugeBlock("ActiveFault3", "00000");
            var fault4 = CreateFaultGaugeBlock("ActiveFault4", "00000");
            var fault5 = CreateFaultGaugeBlock("ActiveFault5", "00000");
            var faultAck = CreateFaultGaugeBlock("FaultACK", "000");
            var faultSync = CreateFaultGaugeBlock("FaultSYNC", "000");

            tblFaults.Controls.Add(fault1.Container, 0, 0);
            tblFaults.Controls.Add(fault2.Container, 1, 0);
            tblFaults.Controls.Add(fault3.Container, 2, 0);
            tblFaults.Controls.Add(fault4.Container, 3, 0);

            tblFaults.Controls.Add(fault5.Container, 0, 1);
            tblFaults.Controls.Add(faultAck.Container, 1, 1);
            tblFaults.Controls.Add(faultSync.Container, 2, 1);
            tblFaults.Controls.Add(new Panel { Dock = DockStyle.Fill }, 3, 1);

            pnlFaults.Controls.Add(tblFaults);
            main.Controls.Add(pnlFaults, 0, 6);

            root.Controls.Add(main);

            result.RootPanel = root;
            result.LblTime = lblTime;
            result.LblTitle = lblTitle;
            result.LblEng1StateValue = eng1Panel.Value;
            result.LblEng2StateValue = eng2Panel.Value;

            result.GaugeWorkHours1 = gaugeWork1;
            result.GaugeWorkHours2 = gaugeWork2;
            result.GaugeRpm1 = gaugeRpm1;
            result.GaugeRpm2 = gaugeRpm2;

            result.GaugeTargetPower = gaugeTarget;
            result.GaugeTracPower = gaugeTrac;
            result.GaugeThrottle = gaugeThrottle;

            result.GaugeWaterTemp1 = gaugeWater1;
            result.GaugeWaterTemp2 = gaugeWater2;
            result.GaugeOilTemp1 = gaugeOil1;
            result.GaugeOilTemp2 = gaugeOil2;

            result.LblFaultCount = lblFaultCount;

            result.GaugeFault1 = fault1.Gauge;
            result.GaugeFault2 = fault2.Gauge;
            result.GaugeFault3 = fault3.Gauge;
            result.GaugeFault4 = fault4.Gauge;
            result.GaugeFault5 = fault5.Gauge;
            result.GaugeFaultAck = faultAck.Gauge;
            result.GaugeFaultSync = faultSync.Gauge;

            return result;
        }
        #endregion

        private void label17_Click(object sender, EventArgs e)
        {

        }
    }*/
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
        public decimal Speed { get; set; }
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
    }
    public class LocoColumnControls
    {
        public Panel RootPanel { get; set; }
        public Label LblTime { get; set; }
        public Label LblTitle { get; set; }
        public Label LblEng1StateValue { get; set; }
        public Label LblEng2StateValue { get; set; }
        public DigitalGauge GaugeWorkHours1 { get; set; }
        public DigitalGauge GaugeWorkHours2 { get; set; }
        public DigitalGauge GaugeRpm1 { get; set; }
        public DigitalGauge GaugeRpm2 { get; set; }
        public DigitalGauge GaugeTargetPower { get; set; }
        public DigitalGauge GaugeTracPower { get; set; }
        public DigitalGauge GaugeThrottle { get; set; }
        public DigitalGauge GaugeWaterTemp1 { get; set; }
        public DigitalGauge GaugeWaterTemp2 { get; set; }
        public DigitalGauge GaugeOilTemp1 { get; set; }
        public DigitalGauge GaugeOilTemp2 { get; set; }
        public Label LblFaultCount { get; set; }
        public DigitalGauge GaugeFault1 { get; set; }
        public DigitalGauge GaugeFault2 { get; set; }
        public DigitalGauge GaugeFault3 { get; set; }
        public DigitalGauge GaugeFault4 { get; set; }
        public DigitalGauge GaugeFault5 { get; set; }
        public DigitalGauge GaugeFaultAck { get; set; }
        public DigitalGauge GaugeFaultSync { get; set; }
    }
}
