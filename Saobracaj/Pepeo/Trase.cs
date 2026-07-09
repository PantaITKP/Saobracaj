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

namespace Saobracaj.Pepeo
{
    public partial class Trase : Form
    {
        private readonly string connection =
            ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.Perftech2"].ConnectionString;
        public Trase()
        {
            InitializeComponent();
        }

        private void Trase_Load(object sender, EventArgs e)
        {
            FillCombo();
        }
        private void FillCombo()
        {
            SqlConnection conn = new SqlConnection(connection);

            var query = "Select ID,Opis From Stanice Where Longitude<>0 and Latitude<>0 order by ID asc";
            var da = new SqlDataAdapter(query, conn);
            var ds = new DataSet();
            da.Fill(ds);
            comboBox1.DataSource = ds.Tables[0];
            comboBox1.DisplayMember = "Opis";
            comboBox1.ValueMember = "ID";

            var trasa = @"
 SET ARITHABORT ON;
SELECT 
    Glavni.IDTrase AS ID,
    CAST(Glavni.IDTrase AS VARCHAR(10)) + ' - ' + 
    STUFF((
        SELECT '/' + CAST(RTRIM(Stanice.Opis) AS VARCHAR(50))
        FROM Stanice 
        INNER JOIN PepeoTrase AS Podupit ON Stanice.ID = Podupit.Stanica 
        WHERE Podupit.IDTrase = Glavni.IDTrase  -- Ovde spajamo unutrašnji i spoljašnji upit
        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '') AS stanice
FROM PepeoTrase AS Glavni
GROUP BY Glavni.IDTrase;";
            var daTrase = new SqlDataAdapter(trasa, conn);
            var dsTrase = new DataSet();
            daTrase.Fill(dsTrase);
            cboTrasa.DataSource = dsTrase.Tables[0];
            cboTrasa.DisplayMember = "stanice";
            cboTrasa.ValueMember = "ID";
        }

        private void btnNova_Click(object sender, EventArgs e)
        {
            using(SqlConnection conn=new SqlConnection(connection))
            {
                conn.Open();
                using(SqlCommand cmd=new SqlCommand("Select ISNull(Max(IDTrase),0)+1 From PepeoTrase", conn))
                {
                    using(SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            textBox1.Text = dr[0].ToString();
                        }
                    }
                }
            }
        }
        public class Trasa
        {
            public int IDTrase { get; set; }
            public int RB { get; set; }
            public int StanicaID { get; set; }
            public string Stanica { get; set; }
        }
        private List<Trasa> list = new List<Trasa>();
        private int rb = 1;

        private void btnDodaj_Click(object sender, EventArgs e)
        {
            try
            {
                var stanica = new Trasa
                {
                    IDTrase = Convert.ToInt32(textBox1.Text),
                    RB = rb,
                    StanicaID = Convert.ToInt32(comboBox1.SelectedValue),
                    Stanica = comboBox1.Text.ToString()
                };

                list.Add(stanica);

                ReorderRB();
                OsveziGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri unosu/izmeni stavke: " + ex.Message);
            }
        }
        private Trasa VratiSelektovanuStavkuIzGrida()
        {
            try
            {
                if (gridGroupingControl1.Table == null)
                    return null;

                if (gridGroupingControl1.Table.CurrentRecord == null)
                    return null;

                return gridGroupingControl1.Table.CurrentRecord.GetData() as Trasa;
            }
            catch
            {
                return null;
            }
        }
        private void ReorderRB()
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i].RB = i + 1;
            }

            rb = list.Count + 1;
        }
        private void OsveziGrid()
        {
            gridGroupingControl1.DataSource = null;
            gridGroupingControl1.DataSource = list.ToList();
            gridGroupingControl1.Refresh();

            if (list.Count > 0)
                rb = list.Max(x => x.RB) + 1;
            else
                rb = 1;
        }
        private void btnIzbaciStanicu_Click(object sender, EventArgs e)
        {
            try
            {
                var stavka = VratiSelektovanuStavkuIzGrida();

                if (stavka == null)
                {
                    MessageBox.Show("Nijedna stavka nije selektovana.");
                    return;
                }

                var rezultat = MessageBox.Show(
                    "Da li ste sigurni da želite da uklonite selektovanu stavku?",
                    "Potvrda brisanja",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (rezultat != DialogResult.Yes)
                    return;

                list.Remove(stavka);

                ReorderRB();
                OsveziGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri uklanjanju stavke: " + ex.Message);
            }
        }

        private void btnSacuvaj_Click(object sender, EventArgs e)
        {
            InsertPepeo ins = new InsertPepeo();
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                using(SqlCommand cmd=new SqlCommand("delete from PepeoTrase Where IDTrase=" + Convert.ToInt32(textBox1.Text), conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            foreach(var i in list)
            {
                ins.InsTrasu(i.IDTrase, i.RB, i.StanicaID);
            }

            MessageBox.Show("Sačuvana trasa");
        }

        private void cboTrasa_SelectionChangeCommitted(object sender, EventArgs e)
        {
            list.Clear();
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();
                string query = @"select IDTrase,RB,Stanica,RTrim(Opis) as Opis 
From PepeoTrase 
inner join stanice on PepeoTrase.Stanica=Stanice.ID 
Where IDTrase=@ID 
order by rb asc";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", Convert.ToInt32(cboTrasa.SelectedValue));
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var stavka = new Trasa
                            {
                                IDTrase = Convert.ToInt32(dr["IDTrase"]),
                                RB = Convert.ToInt32(dr["RB"]),
                                StanicaID = Convert.ToInt32(dr["Stanica"]),
                                Stanica = dr["Opis"].ToString()
                            };
                            list.Add(stavka);
                        }
                    }
                }
            }
            rb = list.Count > 0 ? list.Max(x => x.RB) + 1 : 1;
            OsveziGrid();
            foreach(var i in list)
            {
                textBox1.Text = i.IDTrase.ToString();
            }
        }
    }
}
