using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using Microsoft.Office.Interop.Word;
using Microsoft.Reporting.WinForms;
using Microsoft.Office.Interop.Excel;


namespace Saobracaj.Dokumenta
{
    public partial class frmRAdniNalogPregledServisneUluge : Form
    {
        public frmRAdniNalogPregledServisneUluge()
        {
            InitializeComponent();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string pom = "'1'";
            var select =
 " SELECT 'TRASE PODACI',d1.[IDRadnogNaloga], RTrim(Trase.Voz) as Voz,  RN.StatusRN,      d1.[RB]      ,d1.[IDTrase]      ,d1.[DatumPolaskaReal]      ,d1.[DatumDolaskaReal]      ,d1.[DatumPolaska] " +
  "     ,d1.[DatumDolaska]      ,d1.[Vreme]      ,d1.[VremeReal]      ,d1.[PlaniranaMasa]      ,d1.[MasaLokomotive]      ,d1.[MasaVoza]      ,d1.[BrutoMasa]      ,d1.[Napomena] " +
   "    ,d1.[Rezi]     , RTrim(stanice_2.Opis) as StanicaOd, RTrim(stanice_3.Opis) as StanicaDo,  " +
   "    d1.[StatusTrase],      RTrim(stanice.Opis) AS TRPocetna,             RTrim(stanice_1.Opis) AS TRKrajnja, Trase.Relacija , " +
   "     'LokomotivePOdaci', RadniNalogLokNaTrasi.SMSifra,RadniNalogLokNaTrasi.StanicaOd, Stanice_5.Opis as STOD, RadniNalogLokNaTrasi.StanicaDo, Stanice_6.Opis AS StanicaDoLok,  RadniNalogLokNaTrasi.Vreme " +
     "          FROM RadniNalogTrase d1 INNER JOIN  Trase ON d1.IDTrase = Trase.ID " +
      "         inner Join RadniNalogLokNaTrasi on RadniNalogLokNaTrasi.IDRadnogNaloga = d1.IDRadnogNaloga " +
   "  and RadniNalogLokNaTrasi.IdTrase = d1.IDTrase " +
      "         INNER JOIN  stanice ON Trase.Pocetna = stanice.ID " +
      "         INNER JOIN  stanice AS stanice_2 ON d1.StanicaOd = stanice_2.ID " +
       "        INNER JOIN  stanice AS stanice_3 ON d1.StanicaDo = stanice_3.ID " +
       "        INNER JOIN  stanice AS stanice_1 ON Trase.Krajnja = stanice_1.ID " +
       "          INNER JOIN  stanice AS stanice_5 ON RadniNalogLokNaTrasi.StanicaOD = stanice_5.ID " +
        "       INNER JOIN  stanice AS stanice_6 ON RadniNalogLokNaTrasi.StanicaDo = stanice_6.ID " +
       "        inner Join RadniNalog as RN ON d1.IDRadnogNaloga = RN.ID " +
       "        inner Join Delavci as Zaposleni ON RN.Planer = Zaposleni.DeSifra " +
"  where RN.StatusRN in ('ZA') order by d1.IDRadnogNaloga desc, d1.RB ";

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var commandBuilder = new SqlCommandBuilder(dataAdapter);
            var ds = new DataSet();
            dataAdapter.Fill(ds);
            dataGridView1.ReadOnly = true;
            dataGridView1.DataSource = ds.Tables[0];


        }
    }
}
