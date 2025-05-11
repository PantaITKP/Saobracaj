// ==== ShowMap.cs ====
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace Saobracaj.ML_Training
{
    public partial class ShowMap : Form
    {
        string connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.TestiranjeConnectionString"].ConnectionString;

        public ShowMap()
        {
            InitializeComponent();
            SetWebBrowserVersion(11001);
            webBrowser1.ScriptErrorsSuppressed = true;
            webBrowser1.ObjectForScripting = new ScriptInterface(this);
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

        private void ShowMap_Load(object sender, EventArgs e)
        {
            LoadMap();
        }

        private void LoadMap()
        {
            string fullPath = Path.Combine(Application.StartupPath, "map.html");
            if (!File.Exists(fullPath))
            {
                MessageBox.Show("map.html not found at: " + fullPath);
                return;
            }

            string fileUrl = "file:///" + fullPath.Replace("\\", "/");
            webBrowser1.Navigate(fileUrl);

            webBrowser1.DocumentCompleted += (s, args) =>
            {
                InjectNajavaPins();
            };
        }

        private void InjectNajavaPins()
        {
            string query = @"
                SET ARITHABORT ON;
                SELECT n.ID AS IDNajave, n.Otpravna, s.Longitude, s.Latitude,
                    STUFF((SELECT DISTINCT ',' + CAST(r2.IDTrase AS VARCHAR)
                           FROM RadniNalogVezaNajave rv2
                           INNER JOIN RadniNalogLokNaTrasi r2 ON rv2.IDRadnogNaloga = r2.IDRadnogNaloga
                           WHERE rv2.IDNajave = n.ID
                           FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '') AS TraseIDs,
                    rlt.SMSifra
                FROM Najava n
                INNER JOIN stanice s ON n.Otpravna = s.ID
                INNER JOIN RadniNalogVezaNajave rvn ON n.ID = rvn.IDNajave
                INNER JOIN RadniNalogLokNaTrasi rlt ON rvn.IDRadnogNaloga = rlt.IDRadnogNaloga
                INNER JOIN RadniNalog rn ON rn.ID = rvn.IDRadnogNaloga
                WHERE n.ID IN (SELECT TOP 30 ID FROM Najava ORDER BY ID DESC)
                  AND s.Longitude <> 0 AND s.Latitude <> 0
                GROUP BY n.ID, n.Otpravna, s.Longitude, s.Latitude, rlt.SMSifra;
            ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    try
                    {
                        string id = reader["IDNajave"].ToString();
                        string lat = Convert.ToDecimal(reader["Latitude"]).ToString(CultureInfo.InvariantCulture);
                        string lon = Convert.ToDecimal(reader["Longitude"]).ToString(CultureInfo.InvariantCulture);
                        string trase = reader["TraseIDs"].ToString();
                        string smsifra = reader["SMSifra"].ToString();

                        webBrowser1.Document.InvokeScript("addLoco", new object[] { lon, lat, id, trase, smsifra });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error injecting marker: " + ex.Message);
                    }
                }
            }
        }

        public void RenderTrasaPath(string trasaId)
        {
            string query = @"
        SET ARITHABORT ON;
        SELECT Latitude, Longitude 
        FROM stanice 
        INNER JOIN StaniceTrasaPom ON stanice.ID = StaniceTrasaPom.Stanica
        WHERE IDTrase = @trasa 
        ORDER BY RB ASC;
    ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@trasa", trasaId);
                conn.Open();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    decimal lat = Convert.ToDecimal(reader["Latitude"]);
                    decimal lon = Convert.ToDecimal(reader["Longitude"]);

                    webBrowser1.Document.InvokeScript("addPin", new object[]
                    {
                lon.ToString(CultureInfo.InvariantCulture),
                lat.ToString(CultureInfo.InvariantCulture)
                    });
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            LoadMap();
        }
    }

    [System.Runtime.InteropServices.ComVisible(true)]
    public class ScriptInterface
    {
        private readonly ShowMap _form;
        public ScriptInterface(ShowMap form) => _form = form;

        public void ShowTrasa(string trasaId)
        {
            _form.Invoke((MethodInvoker)(() => _form.RenderTrasaPath(trasaId)));
        }
    }
}
