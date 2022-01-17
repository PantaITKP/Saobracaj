﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Configuration;
using Syncfusion.Windows.Forms.Grid.Grouping;

namespace Saobracaj.SyncForm
{
    public partial class frmNajavaArhivaAnaliza : Syncfusion.Windows.Forms.Office2010Form
    {
        public frmNajavaArhivaAnaliza()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MzcwMDg5QDMxMzgyZTM0MmUzMFhQSmlDM0M2bGpxcXVtT1VScTg1a0dtVTFLcUZiK0tLRnpvRTYyRFpMc3M9");
            InitializeComponent();
        }

        private void frmNajavaArhivaAnaliza_Load(object sender, EventArgs e)
        {
          
            var select = "  SELECT    najava.ID, stanice_4.opis as Granicna, " +
" Najava.BrojNajave, Najava.Voz, Partnerji_1.PaNaziv as Posiljalac,  " +
" Partnerji.PaNaziv AS Prevoznik, Partnerji_2.PaNaziv AS Primalac, " +
" stanice.Opis AS Uputna, stanice_1.Opis AS Otpravna,  Najava.PrevozniPut as Relacija , " +
" Najava.PredvidjenoPrimanje, Najava.StvarnoPrimanje, " +
" Najava.PredvidjenaPredaja, Najava.StvarnaPredaja, " +
" CASE WHEN Najava.RID > 0 THEN Cast(1 as bit) ELSE Cast(0 as BIT) END as StatusN , " +
" Najava.ONBroj,  Najava.Status, Najava.Tezina, Najava.Duzina, " +
" Najava.BrojKola, Najava.DatumUnosa, Partnerji_3.PaNaziv as PrevoznikZa, Najava.Faktura, Najava.Korisnik, " +
" najava.RobaNHM " +
" FROM  Najava INNER JOIN Partnerji AS Partnerji_1 ON " +
" Najava.Posiljalac = Partnerji_1.PaSifra " +
" INNER JOIN Partnerji ON Najava.Prevoznik = Partnerji.PaSifra " +
" INNER JOIN Partnerji AS Partnerji_2 ON Najava.Primalac = Partnerji_2.PaSifra " +
" INNER JOIN  stanice ON Najava.Uputna = stanice.ID " +
" INNER JOIN  stanice AS stanice_1 ON Najava.Otpravna = stanice_1.ID " +
" inner JOIN  stanice AS stanice_4 ON Najava.Granicna = stanice_4.ID " +
" INNER JOIN Partnerji as Partnerji_3 ON Najava.PrevoznikZa = Partnerji_3.PaSifra " +
" order by Najava.ID desc ";

            var s_connection = ConfigurationManager.ConnectionStrings["Saobracaj.Properties.Settings.Perftech_BeogradConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            // dataGridView1.ReadOnly = true;
            gridGroupingControl1.DataSource = ds.Tables[0];
            gridGroupingControl1.ShowGroupDropArea = true;
            this.gridGroupingControl1.TopLevelGroupOptions.ShowFilterBar = true;
            foreach (GridColumnDescriptor column in this.gridGroupingControl1.TableDescriptor.Columns)
            {
                column.AllowFilter = true;
            }
        }
    }
}
