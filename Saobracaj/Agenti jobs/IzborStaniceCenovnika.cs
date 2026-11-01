using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Saobracaj.Agenti_jobs
{
    public partial class IzborStaniceCenovnika : Form
    {
        private readonly DataTable podaci;
        public DataRow IzabraniRed { get; private set; }

        public IzborStaniceCenovnika() : this("Izbor", "", new DataTable())
        {
        }

        public IzborStaniceCenovnika(string naslov, string uputstvo, DataTable izbor)
        {
            InitializeComponent();
            Text = naslov;
            lblUputstvo.Text = uputstvo;
            podaci = izbor;
            FillDGV();
        }

        private void FillDGV()
        {
            dgvIzbor.DataSource = podaci;
            dgvIzbor.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            foreach (DataGridViewColumn c in dgvIzbor.Columns)
                c.Width = c.DataPropertyName == "Opis" || c.DataPropertyName == "Relacija" ? 340 : 150;
        }

        private void txtPretraga_TextChanged(object sender, EventArgs e)
        {
            string query = StationNames.Normalize(txtPretraga.Text);
            if (query.Length == 0) { podaci.DefaultView.RowFilter = ""; return; }
            // Filter an independent view via a hidden Boolean column; never modifies source reference data.
            if (!podaci.Columns.Contains("__odgovara")) podaci.Columns.Add("__odgovara", typeof(bool));
            foreach (DataRow row in podaci.Rows)
                row["__odgovara"] = podaci.Columns.Cast<DataColumn>().Where(c => c.ColumnName != "__odgovara")
                    .Any(c => StationNames.Normalize(Convert.ToString(row[c], CultureInfo.CurrentCulture)).Contains(query));
            dgvIzbor.Columns["__odgovara"].Visible = false;
            podaci.DefaultView.RowFilter = "[__odgovara] = true";
        }

        private void btnIzaberi_Click(object sender, EventArgs e)
        {
            var row = dgvIzbor.CurrentRow == null ? null : dgvIzbor.CurrentRow.DataBoundItem as DataRowView;
            if (row == null) { MessageBox.Show(this, "Izaberite red."); return; }
            IzabraniRed = row.Row;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void dgvIzbor_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) btnIzaberi_Click(sender, EventArgs.Empty);
        }

        private void btnOdustani_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
