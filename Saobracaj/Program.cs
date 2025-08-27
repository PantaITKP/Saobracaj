using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Syncfusion.Licensing;

namespace Saobracaj
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MzcwMDg5QDMxMzgyZTM0MmUzMFhQSmlDM0M2bGpxcXVtT1VScTg1a0dtVTFLcUZiK0tLRnpvRTYyRFpMc3M9");
            EnsureIe11Emulation();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Sifarnici.frmLogovanje());
           // Application.Run(new Dokumenta.frmTest());
        }

        static void EnsureIe11Emulation()
        {
            try
            {
                // ime procesa npr. "Saobracaj.exe"
                string exe = Process.GetCurrentProcess().ProcessName + ".exe";
                using (var k = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
                {
                    // 11001 = IE11 Edge mode
                    k?.SetValue(exe, 11001, RegistryValueKind.DWord);
                }
                // (opciono) bolje skaliranje fontova/slika u IE
                using (var k = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_96DPI_PIXEL"))
                {
                    k?.SetValue(exe, 1, RegistryValueKind.DWord);
                }
            }
            catch { /* ignore */ }
        }
    }
}
