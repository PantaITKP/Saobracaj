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

namespace Saobracaj.Dokumenta
{
    public partial class frmPPKGlavna : Form
    {
        public frmPPKGlavna()
        {
            InitializeComponent();
            FillGV();
        }
        private void FillGV()
        {
            var select = "Select Aktivnosti.ID as [Aktivnost],(Rtrim(DePriimek) + ' ' + RTrim(DeIme)) as Zaposleni,VremeOD,VremeDo,Opis,AktivnostiStavke.ID as [Aktivnosti Stavke]," +
                "Napomena,DatumZavrsetka " +
                "From Aktivnosti " +
                "Inner join AktivnostiStavke on Aktivnosti.ID = AktivnostiStavke.IDNadredjena " +
                "Inner join Delavci on Aktivnosti.Zaposleni = Delavci.DeSifra " +
                "Where VrstaAktivnostiID = 9 order by Aktivnosti.ID desc";

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
