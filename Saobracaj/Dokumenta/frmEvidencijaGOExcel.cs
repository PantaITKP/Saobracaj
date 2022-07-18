using Microsoft.Office.Interop.Excel;
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
    public partial class frmEvidencijaGOExcel : Form
    {
        public frmEvidencijaGOExcel()
        {
            InitializeComponent();
            dateTimePicker1.Value = DateTime.Now;
            dateTimePicker2.Value = DateTime.Now;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string Od = dateTimePicker1.Value.ToString("yyyy-MM-dd");
            string Do = dateTimePicker2.Value.ToString("yyyy-MM-dd");
            var select = "Select distinct ID,RTrim(Delavci.DeIme)+ ' ' +RTrim(Delavci.DePriimek) as Zaposleni,VremeOD,VremeDO,Ukupno,StatusGodmora " +
                "From DopustStavke " +
                "Inner join Dopust on DopustStavke.IDNadredjena=Dopust.DoStZapisa " +
                "Inner join Delavci on Dopust.DoSifDe=Delavci.DeSifra " +
                "Where (VremeOD Between '"+Od+"' and '"+Do+"') order by ID desc";

            var s_connection = ConfigurationManager.ConnectionStrings["WindowsFormsApplication1.Properties.Settings.NedraConnectionString"].ConnectionString;
            SqlConnection myConnection = new SqlConnection(s_connection);
            var c = new SqlConnection(s_connection);
            var dataAdapter = new SqlDataAdapter(select, c);

            var ds = new DataSet();
            dataAdapter.Fill(ds);

            Microsoft.Office.Interop.Excel.Application excel = new Microsoft.Office.Interop.Excel.Application();
            object missing = System.Reflection.Missing.Value;
            Workbook wBook = excel.Workbooks.Add(missing);

            Worksheet wSheet = new Worksheet();
            try
            {

                wSheet = (Worksheet)wBook.Worksheets.get_Item(1);
                for (int i = 0; i <= ds.Tables[0].Rows.Count - 1; i++)
                {
                    for (int j = 0; j <= ds.Tables[0].Columns.Count - 1; j++)
                    {
                        wSheet.Cells[1, 55].EntireRow.Font.Bold = true;
                        wSheet.Range["A1:F1"].Interior.Color = System.Drawing.Color.Red;
                        wSheet.Cells[1, "A"] = "ID";
                        wSheet.Cells[1, "B"] = "Zaposelni";
                        wSheet.Cells[1, "C"] = "Vreme od";
                        wSheet.Cells[1, "D"] = "Vreme do";
                        wSheet.Cells[1, "E"] = "Ukupno";
                        wSheet.Cells[1, "F"] = "Status";
                       
                        wSheet.Cells[i + 2, j + 1] = ds.Tables[0].Rows[i].ItemArray[j].ToString();
                        wSheet.Cells[i + 2, j + 1].EntireColumn.AutoFit();
                        Borders border = wSheet.Cells[i + 2, j + 1].Borders;
                        border.Weight = 2d;

                    }
                }

                string date = dateTimePicker1.Value.ToString("dd-MM-yyyy");
                string path = Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
                object filename = @"Izvestaj GO od " + date + ".xlsx";
                wBook.SaveAs(filename);
                wBook.Close();
                excel.Quit();
                excel = null;
                wBook = null;
                wSheet = null;


                MessageBox.Show("Dokument je kreiran");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }
    }
}
