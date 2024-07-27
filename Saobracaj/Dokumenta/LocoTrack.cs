﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Syncfusion.Windows.Forms.Gauge;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.PeerToPeer.Collaboration;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace Saobracaj.Dokumenta
{
    public partial class LocoTrack : Form
    {
        string connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"].ConnectionString;
        bool status = true;
        public LocoTrack()
        {
            InitializeComponent();
            SetWebBrowserVersion(11001); // 11001 = IE11
            webBrowser1.ScriptErrorsSuppressed = true;
            webBrowser1.ObjectForScripting = true;

            rpm1Gauge.MajorDifference = 1000;
            rpm1Gauge.MinorDifference = 500;
            rpm1Gauge.MaximumValue = 5000;

            rpm2Gauge.MajorDifference = 1000;
            rpm2Gauge.MinorDifference = 500;
            rpm2Gauge.MaximumValue = 5000;
        }
        DateTime poslednjiZapis;
        private void LocoTrack_Load(object sender, EventArgs e)
        {
            var select = "Select Max(DatumUpisa) From WebMap";
            SqlConnection conn = new SqlConnection(connection);
            SqlCommand cmd = new SqlCommand(select, conn);
            conn.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                poslednjiZapis = Convert.ToDateTime(reader[0].ToString());
            }
            conn.Close();

            lokomotiva = "2946";
            fromDt = poslednjiZapis.ToString("yyyy-MM-dd");
            toDt = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");

            ReadData();
            FillCombo();
            LoadMap();
            FillGV();
        }
        public class Record
        {
            public DateTime Recordtime { get; set; }
            public long IdRecords { get; set; }
            public string Vehicles_idVehicles { get; set; }
            public decimal Altitude { get; set; }
            public int SV { get; set; }
            public int Eventid { get; set; }
            public decimal Mainvoltage { get; set; }
            public bool IO0 { get; set; }
            public bool Valid { get; set; }
            public decimal Latitude { get; set; }
            public decimal Longitude { get; set; }
            public decimal Speed { get; set; }
            public decimal Direction { get; set; }
            public string SS { get; set; }
        }

        #region PomAPI

        List<Record> rec;
        HttpClient client = new HttpClient();
        string token = "";
        string lokomotiva, fromDt, toDt;
        int iDRecord, sV;
        string iDVehicles, ss;
        DateTime recordTime;
        decimal latitude, longitude, altitude, direction, mainVoltage, speedApi;

        string year, month, day, hour, minute, second, locomotiveId, driverId, nforceFw, locoMode, speed,
         gboxState, pwrContactState, throttlePos, tracMotorAvgCurr, targetPower, tracPower, tracForce,
         tracMotor1Curr, tracMotor2Curr, tracMotor3Curr, tracMotor4Curr, compressorState,
         mainResPress, brakeCylPress, brakePipePress, mcoMask, asMask, alerterMask, trip,
         asssState, excLimit, wheelSlip, resetCnt, secEngAllow, eng1State, eng1Rpm, eng1WorkHours,
         eng1WaterTemp, eng1OilTemp, eng1OilLevel, eng1FuelCons, eng2State, eng2Rpm, eng2WorkHours,
         eng2WaterTemp, eng2OilTemp, eng2OilLevel, eng2FuelCons, activeFaultCnt, activeFault1,
         activeFault2, activeFault3, activeFault4, activeFault5, faultSync, faultAck;
        #endregion
        private async void ReadData()
        {
            try
            {
                string path = "http://85.25.177.168/egoidentityserver/connect/token";
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("scope", "MyApi"),
                    new KeyValuePair<string, string>("client_id", "lokomotive"),
                    new KeyValuePair<string, string>("client_secret", "loccoo#moto147")
                });
                HttpResponseMessage responseToken = await client.PostAsync(path, formData);
                if (responseToken.IsSuccessStatusCode)
                {
                    string responseText = await responseToken.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(responseText);
                    token = data.access_token;
                    string apiEndpoint = "http://85.25.177.168/gpstrackjourney/api/journey/records";
                    var resp = new MultipartFormDataContent
                    {
                        { new StringContent(lokomotiva), "vehicles" },
                        { new StringContent(fromDt), "from" },
                        { new StringContent(toDt), "to" }
                    };
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, apiEndpoint);
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    request.Content = resp;

                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        rec = JsonConvert.DeserializeObject<List<Record>>(content);

                        foreach (var i in rec)
                        {
                            iDRecord = Convert.ToInt32(i.IdRecords);
                            iDVehicles = i.Vehicles_idVehicles;
                            recordTime = Convert.ToDateTime(i.Recordtime);
                            latitude = Convert.ToDecimal(i.Latitude);
                            longitude = Convert.ToDecimal(i.Longitude);
                            altitude = Convert.ToDecimal(i.Altitude);
                            direction = Convert.ToDecimal(i.Direction);
                            sV = Convert.ToInt32(i.SV);
                            mainVoltage = Convert.ToDecimal(i.Mainvoltage);
                            speedApi = Convert.ToDecimal(i.Speed);
                            ss = i.SS;

                            year = ss.Substring(0, 4);
                            month = ss.Substring(4, 2);
                            day = ss.Substring(6, 2);
                            hour = ss.Substring(8, 2);
                            minute = ss.Substring(10, 2);
                            second = ss.Substring(12, 2);
                            locomotiveId = ss.Substring(14, 5);
                            driverId = ss.Substring(19, 5);
                            nforceFw = ss.Substring(24, 5);
                            locoMode = ss.Substring(29, 5);
                            speed = ss.Substring(34, 5);
                            gboxState = ss.Substring(39, 3);
                            pwrContactState = ss.Substring(42, 3);
                            throttlePos = ss.Substring(45, 3);
                            tracMotorAvgCurr = ss.Substring(48, 5);
                            targetPower = ss.Substring(53, 5);
                            tracPower = ss.Substring(58, 5);
                            tracForce = ss.Substring(63, 5);
                            tracMotor1Curr = ss.Substring(68, 5);
                            tracMotor2Curr = ss.Substring(73, 5);
                            tracMotor3Curr = ss.Substring(78, 5);
                            tracMotor4Curr = ss.Substring(83, 5);
                            compressorState = ss.Substring(88, 3);
                            mainResPress = ss.Substring(91, 5);
                            brakeCylPress = ss.Substring(96, 5);
                            brakePipePress = ss.Substring(101, 5);
                            mcoMask = ss.Substring(106, 3);
                            asMask = ss.Substring(109, 3);
                            alerterMask = ss.Substring(112, 3);
                            trip = ss.Substring(115, 8);
                            asssState = ss.Substring(123, 3);
                            excLimit = ss.Substring(126, 3);
                            wheelSlip = ss.Substring(129, 3);
                            resetCnt = ss.Substring(132, 3);
                            secEngAllow = ss.Substring(135, 3);
                            eng1State = ss.Substring(138, 3);
                            eng1Rpm = ss.Substring(141, 5);
                            eng1WorkHours = ss.Substring(146, 5);
                            eng1WaterTemp = ss.Substring(151, 5);
                            eng1OilTemp = ss.Substring(156, 5);
                            eng1OilLevel = ss.Substring(161, 5);
                            eng1FuelCons = ss.Substring(166, 5);
                            eng2State = ss.Substring(171, 3);
                            eng2Rpm = ss.Substring(174, 5);
                            eng2WorkHours = ss.Substring(179, 5);
                            eng2WaterTemp = ss.Substring(184, 5);
                            eng2OilTemp = ss.Substring(189, 5);
                            eng2OilLevel = ss.Substring(194, 5);
                            eng2FuelCons = ss.Substring(199, 5);
                            activeFaultCnt = ss.Substring(204, 3);
                            activeFault1 = ss.Substring(207, 5);
                            activeFault2 = ss.Substring(212, 5);
                            activeFault3 = ss.Substring(217, 5);
                            activeFault4 = ss.Substring(222, 5);
                            activeFault5 = ss.Substring(227, 5);
                            faultSync = ss.Substring(232, 3);
                            faultAck = ss.Substring(235, 3);

                            var select = "select count(ID) From WebMap Where IDRecords=" + iDRecord;
                            SqlConnection conn = new SqlConnection(connection);
                            SqlCommand cmd = new SqlCommand(select, conn);
                            conn.Open();
                            SqlDataReader reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                int count = Convert.ToInt32(reader[0].ToString());
                                if (count == 0)
                                {
                                    string dateTimeStr = $"{year}-{month}-{day} {hour}:{minute}:{second}";
                                    DateTime dt;
                                    DateTime.TryParseExact(dateTimeStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);

                                    InsertWebMap ins = new InsertWebMap();
                                    ins.InsertAPI(iDRecord, iDVehicles, recordTime, latitude, longitude, altitude, direction, sV, mainVoltage, speedApi, DateTime.Now, locomotiveId, driverId, nforceFw, locoMode, speed, gboxState,
                                        pwrContactState, throttlePos, tracMotorAvgCurr, targetPower, tracPower, tracForce, tracMotor1Curr, tracMotor2Curr, tracMotor3Curr, tracMotor4Curr, compressorState, mainResPress,
                                        brakeCylPress, brakePipePress, mcoMask, asMask, alerterMask, trip, asssState, excLimit, wheelSlip, resetCnt, secEngAllow, eng1State, eng1Rpm, eng1WorkHours, eng1WaterTemp,
                                        eng1OilTemp, eng1OilLevel, eng1FuelCons, eng2State, eng2Rpm, eng2WorkHours, eng2WaterTemp, eng2OilTemp, eng2OilLevel, eng2FuelCons, activeFaultCnt, activeFault1,
                                        activeFault2, activeFault3, activeFault4, activeFault5, faultSync, faultAck, DateTime.Now);
                                }
                            }
                            conn.Close();
                        }
                    }
                    else
                    {
                        throw new HttpRequestException($"Failed to retrieve records. Status code: {response.StatusCode}");
                    }
                }
                else
                {
                    throw new HttpRequestException($"Failed to obtain access token. Status code: {responseToken.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }
        private void FillGV()
        {
            var select = "select top 300 * from WebMap order by ID desc";
            SqlConnection conn = new SqlConnection(connection);
            var dataAdapter = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];
            dataGridView1.MultiSelect = false;


            dataGridView1.Columns[0].Width = 60;
            dataGridView1.Columns[1].Width = 60;
            dataGridView1.Columns[3].Width = 60;
            dataGridView1.Columns[4].Visible = false;
            dataGridView1.Columns[5].Visible = false;

            DesignGV();
        }
        private void DesignGV()
        {
            dataGridView1.BorderStyle = BorderStyle.FixedSingle;
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(238, 239, 249);
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.DarkTurquoise;
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.WhiteSmoke;
            dataGridView1.BackgroundColor = Color.White;

            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(20, 25, 72);
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        }
        private void FillCombo()
        {
            var select = "Select distinct IDVehicles From WebMap";
            SqlConnection conn = new SqlConnection(connection);
            var dataAdapter = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            comboBox1.DataSource = ds.Tables[0];
            comboBox1.DisplayMember = "IDVehicles";
            comboBox1.ValueMember = "IDVehicles";
        }
        private void LoadMap()
        {
            string html = System.IO.File.ReadAllText("map.html");
            webBrowser1.DocumentText = html;
        }
        private void AddMarker(decimal latitude, decimal longitude)
        {
            if (status == true)
            {
                webBrowser1.Document.InvokeScript("addMarker", new object[] { latitude, longitude });
            }
        }
        private void SetWebBrowserVersion(int version)
        {
            const string keyName = @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";
            string appName = System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            Microsoft.Win32.Registry.SetValue(
                Microsoft.Win32.Registry.CurrentUser + "\\" + keyName,
                appName,
                version,
                Microsoft.Win32.RegistryValueKind.DWord);
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            decimal longitude, latitude;
            int speed, gboxState, pwrContact, throtlePos, targetPower, tracPower, tracForce, compressorState, mainResPres, brakeCylPress, brakePipePress, wheelSlip, eng1State, eng1Rpm, eng1WorkHours, eng1WaterTemp,
                eng1OilTemp, eng1OilLevel, eng1FuelCons, eng2State, eng2RPM, eng2WorkHours, eng2WaterTemp, eng2OilTemp, eng2OilLevel, eng2FuelCons, activeFaultCnt, activeFault1, activeFault2, activeFault3,
                activeFault4, activeFault5, faultSync, faultAck;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Selected)
                {
                    longitude = Convert.ToDecimal(row.Cells["Longitude"].Value);
                    latitude = Convert.ToDecimal(row.Cells["Latitude"].Value);

                    AddMarker(latitude, longitude);

                    speed = Convert.ToInt32(row.Cells["SPEED"].Value);
                    gboxState = Convert.ToInt32(row.Cells["GBOX_STATE"].Value);
                    pwrContact = Convert.ToInt32(row.Cells["PWR_CONTACT_STATE"].Value);
                    throtlePos = Convert.ToInt32(row.Cells["THROTTLE_POS"].Value);
                    targetPower = Convert.ToInt32(row.Cells["TARGET_POWER"].Value);
                    tracPower = Convert.ToInt32(row.Cells["TRAC_POWER"].Value);
                    tracForce = Convert.ToInt32(row.Cells["TRAC_FORCE"].Value);
                    compressorState = Convert.ToInt32(row.Cells["COMPRESSOR_STATE"].Value);
                    mainResPres = Convert.ToInt32(row.Cells["MAIN_RES_PRESS"].Value);
                    brakeCylPress = Convert.ToInt32(row.Cells["BRAKE_CYL_PRESS"].Value);
                    brakePipePress = Convert.ToInt32(row.Cells["BRAKE_PIPE_PRESS"].Value);
                    wheelSlip = Convert.ToInt32(row.Cells["WHEEL_SLIP"].Value);
                    eng1State = Convert.ToInt32(row.Cells["ENG_1_STATE"].Value);
                    eng1Rpm = Convert.ToInt32(row.Cells["ENG_1_RPM"].Value);
                    eng1WorkHours = Convert.ToInt32(row.Cells["ENG_1_WORK_HOURS"].Value);
                    eng1WaterTemp = Convert.ToInt32(row.Cells["ENG_1_WATER_TEMP"].Value);
                    eng1OilTemp = Convert.ToInt32(row.Cells["ENG_1_OIL_TEMP"].Value);
                    eng1OilLevel = Convert.ToInt32(row.Cells["ENG_1_OIL_LEVEL"].Value);
                    eng1FuelCons = Convert.ToInt32(row.Cells["ENG_1_FUEL_CONS"].Value);
                    eng2State = Convert.ToInt32(row.Cells["ENG_2_STATE"].Value);
                    eng2RPM = Convert.ToInt32(row.Cells["ENG_2_RPM"].Value);
                    eng2WorkHours = Convert.ToInt32(row.Cells["ENG_2_WORK_HOURS"].Value);
                    eng2WaterTemp = Convert.ToInt32(row.Cells["ENG_2_WATER_TEMP"].Value);
                    eng2OilTemp = Convert.ToInt32(row.Cells["ENG_2_OIL_TEMP"].Value);
                    eng2OilLevel = Convert.ToInt32(row.Cells["ENG_2_OIL_LEVEL"].Value);
                    eng2FuelCons = Convert.ToInt32(row.Cells["ENG_2_FUEL_CONS"].Value);
                    activeFaultCnt = Convert.ToInt32(row.Cells["ACTIVE_FAULT_CNT"].Value);
                    activeFault1 = Convert.ToInt32(row.Cells["ACTIVE_FAULT_1"].Value);
                    activeFault2 = Convert.ToInt32(row.Cells["ACTIVE_FAULT_2"].Value);
                    activeFault3 = Convert.ToInt32(row.Cells["ACTIVE_FAULT_3"].Value);
                    activeFault4 = Convert.ToInt32(row.Cells["ACTIVE_FAULT_4"].Value);
                    activeFault5 = Convert.ToInt32(row.Cells["ACTIVE_FAULT_5"].Value);
                    faultSync = Convert.ToInt32(row.Cells["FAULT_SYNC"].Value);
                    faultAck = Convert.ToInt32(row.Cells["FAULT_ACK"].Value);

                    UpdateGauge(speed, gboxState, pwrContact, throtlePos, targetPower, tracPower, tracForce, compressorState, mainResPres, brakeCylPress, brakePipePress,
                        wheelSlip, eng1State, eng1Rpm, eng1WorkHours, eng1WaterTemp, eng1OilTemp, eng1OilLevel, eng1FuelCons, eng2State, eng2RPM, eng2WorkHours,
                        eng2WaterTemp, eng2OilTemp, eng2OilLevel, eng2FuelCons, activeFaultCnt, activeFault1, activeFault2, activeFault3, activeFault4, activeFault5,
                        faultSync, faultAck);
                    if (status == false)
                    {
                        webBrowser1.Document.InvokeScript("focusPin", new object[] { latitude, longitude });
                    }
                }
            }
        }
        private void UpdateGauge(int speed, int gboxState, int pwrContact, int throtlePos, int targetPower, int tracPower, int tracForce, int compressorState, int mainResPres, int brakeCylPress,
            int brakePipePress, int wheelSlip, int eng1State, int eng1Rpm, int eng1WorkHours, int eng1WaterTemp, int eng1OilTemp, int eng1OilLevel, int eng1FuelCons, int eng2State, int eng2RPM,
            int eng2WorkHours, int eng2WaterTemp, int eng2OilTemp, int eng2OilLevel, int eng2FuelCons, int activeFaultCnt, int activeFault1, int activeFault2, int activeFault3, int activeFault4,
            int activeFault5, int faultSync, int faultAck)
        {
            speedGauge.Value = speed;
            gStateGauge.Value = gboxState.ToString();
            pwrGauge.Value = pwrContact.ToString();
            throttlePosGauge.Value = throtlePos;
            targetPowerGauge.Value = targetPower.ToString();
            tracPowerGauge.Value = tracPower.ToString();
            tracForceGauge.Value = tracForce.ToString();
            compresorStateGauge.Value = compressorState.ToString();
            MainResPressGauge.Value = mainResPres.ToString();
            brakeCylPressGauge.Value = brakeCylPress.ToString();
            BrakePipePressGauge.Value = brakePipePress.ToString();
            WheelSlipGauge.Value = wheelSlip.ToString();
            eng1StateGauge.Value = eng1State;
            rpm1Gauge.Value = eng1Rpm;
            eng1WorkHoursGauge.Value = eng1WorkHours.ToString();
            eng1WaterTempGauge.Value = eng1WaterTemp;
            eng1OilTempGauge.Value = eng1OilTemp;
            eng1oilLevelGauge.Value = eng1OilLevel;
            eng1FuelComp.Text = "Eng1FuelCons:" + eng1FuelCons.ToString();
            eng2StateGauge.Value = eng2State;
            rpm2Gauge.Value = eng2RPM;
            eng2WorkHoursGauge.Value = eng2WorkHours.ToString();
            eng2WaterTempGauge.Value = eng2WaterTemp;
            Eng2OilTempGauge.Value = eng2OilTemp;
            eng2OilLevelGauge.Value = eng2OilLevel;
            eng2FuelComp.Text = "Eng2FuelCons:" + eng2FuelCons.ToString();
            activeFaultCNTGauge.Value = activeFaultCnt.ToString();
            activeFault1Gauge.Value = activeFault1.ToString();
            activeFault2Gauge.Value = activeFault2.ToString();
            activeFault3Gauge.Value = activeFault3.ToString();
            activeFault4Gauge.Value = activeFault4.ToString();
            activeFautl5Gauge.Value = activeFault5.ToString();
            faultSyncGauge.Value = faultSync.ToString();
            faultAckGauge.Value = faultAck.ToString();
        }
        private void btn24H_Click(object sender, EventArgs e)
        {
            status = true;
            webBrowser1.Document.InvokeScript("clearMap");

            FillGV();
            DesignGV();
        }
        private void btnLokomotiva_Click(object sender, EventArgs e)
        {
            status = false;
            webBrowser1.Document.InvokeScript("clearMap");

            string odPom, doPom;
            odPom = dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm:ss.fff");
            doPom = dateTimePicker2.Value.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var select = "select * from WebMap Where IDVehicles=" + comboBox1.SelectedValue + " and RecordTime between '" + odPom + "' and '" + doPom + "' order by ID desc";
            SqlConnection conn = new SqlConnection(connection);
            var dataAdapter = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];
            dataGridView1.MultiSelect = false;

            DesignGV();

            webBrowser1.Document.InvokeScript("eval", new object[] { "map.eachLayer(function (layer) { if (layer instanceof L.Marker) { map.removeLayer(layer); } });" });
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                decimal longitude = Convert.ToDecimal(row.Cells["Longitude"].Value);
                decimal latitude = Convert.ToDecimal(row.Cells["Latitude"].Value);
                webBrowser1.Document.InvokeScript("addPin", new object[] { latitude, longitude });
            }
        }
        private void btnPozicije_Click(object sender, EventArgs e)
        {
            status = false;
            webBrowser1.Document.InvokeScript("clearMap");

            string select = "SELECT * FROM (SELECT *, ROW_NUMBER() OVER(PARTITION BY IDVehicles ORDER BY RecordTime DESC) AS RowNum FROM WebMap) AS LastRecord WHERE RowNum = 1";
            SqlConnection conn = new SqlConnection(connection);
            var dataAdapter = new SqlDataAdapter(select, conn);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];
            dataGridView1.MultiSelect = false;


            dataGridView1.Columns[0].Width = 60;
            dataGridView1.Columns[1].Width = 60;
            dataGridView1.Columns[3].Width = 60;
            dataGridView1.Columns[4].Visible = false;
            dataGridView1.Columns[5].Visible = false;

            DesignGV();
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                decimal longitude = Convert.ToDecimal(row.Cells["Longitude"].Value);
                decimal latitude = Convert.ToDecimal(row.Cells["Latitude"].Value);
                int id = Convert.ToInt32(row.Cells["ID"].Value);
                string locomotive = row.Cells["IDVehicles"].Value.ToString().TrimEnd();
                webBrowser1.Document.InvokeScript("addLoco", new object[] { latitude, longitude,id,locomotive });
            }
        }
        private void rbMaxSpeed_CheckedChanged(object sender, EventArgs e)
        {
            webBrowser1.Document.InvokeScript("updateLayer", new object[] { "maxspeed" });
        }

        private void rbSignals_CheckedChanged(object sender, EventArgs e)
        {
            webBrowser1.Document.InvokeScript("updateLayer", new object[] { "signals" });
        }

        private void rbStandard_CheckedChanged(object sender, EventArgs e)
        {
            webBrowser1.Document.InvokeScript("updateLayer", new object[] { "standard" });
        }
    }
}