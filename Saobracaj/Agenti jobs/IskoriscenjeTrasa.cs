using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Saobracaj.Agenti_jobs
{
    public partial class IskoriscenjeTrasa : Form
    {
        private readonly Timer periodTimer = new Timer { Interval = 350 };
        private GridColumnFilters filterDetalji;
        private GridColumnFilters filterZbir;
        private IskoriscenjeZbir zbir;
        private bool spremna, povezivanje, cuvanjeCene;
        private int zahtev;

        public IskoriscenjeTrasa()
        {
            InitializeComponent();

            dateTimePicker1.Format = DateTimePickerFormat.Custom;
            dateTimePicker1.CustomFormat = "dd.MM.yyyy";
            dateTimePicker2.Format = DateTimePickerFormat.Custom;
            dateTimePicker2.CustomFormat = "dd.MM.yyyy";

            GridTitles.Configure(dataGridView1, false);
            GridTitles.Configure(dataGridView2, true);
            DodajDugmeRadnogNaloga();
            dataGridView1.CellContentClick -= dataGridView1_CellContentClick;
            dataGridView1.CellContentClick += dataGridView1_CellContentClick;
            dataGridView2.AllowUserToAddRows = false;
            dataGridView2.AllowUserToDeleteRows = false;
            filterDetalji = new GridColumnFilters(dataGridView1);
            filterZbir = new GridColumnFilters(dataGridView2);

            // Named events; keeping an existing Designer hookup to the same method is safe.
            Shown -= IskoriscenjeTrasa_Shown;
            Shown += IskoriscenjeTrasa_Shown;
            FormClosing -= IskoriscenjeTrasa_FormClosing;
            FormClosing += IskoriscenjeTrasa_FormClosing;
            FormClosed -= IskoriscenjeTrasa_FormClosed;
            FormClosed += IskoriscenjeTrasa_FormClosed;
            dateTimePicker1.ValueChanged -= dateTimePicker1_ValueChanged;
            dateTimePicker1.ValueChanged += dateTimePicker1_ValueChanged;
            dateTimePicker2.ValueChanged -= dateTimePicker2_ValueChanged;
            dateTimePicker2.ValueChanged += dateTimePicker2_ValueChanged;
            dataGridView1.ColumnHeaderMouseClick -= dataGridView1_ColumnHeaderMouseClick;
            dataGridView1.ColumnHeaderMouseClick += dataGridView1_ColumnHeaderMouseClick;
            dataGridView2.ColumnHeaderMouseClick -= dataGridView2_ColumnHeaderMouseClick;
            dataGridView2.ColumnHeaderMouseClick += dataGridView2_ColumnHeaderMouseClick;
            dataGridView1.DataBindingComplete -= dataGridView1_DataBindingComplete;
            dataGridView1.DataBindingComplete += dataGridView1_DataBindingComplete;
            dataGridView2.DataBindingComplete -= dataGridView2_DataBindingComplete;
            dataGridView2.DataBindingComplete += dataGridView2_DataBindingComplete;
            dataGridView2.CellBeginEdit -= dataGridView2_CellBeginEdit;
            dataGridView2.CellBeginEdit += dataGridView2_CellBeginEdit;
            dataGridView2.CellValidating -= dataGridView2_CellValidating;
            dataGridView2.CellValidating += dataGridView2_CellValidating;
            dataGridView2.CellParsing -= dataGridView2_CellParsing;
            dataGridView2.CellParsing += dataGridView2_CellParsing;
            dataGridView2.CellEndEdit -= dataGridView2_CellEndEdit;
            dataGridView2.CellEndEdit += dataGridView2_CellEndEdit;
            dataGridView1.CellFormatting -= dataGridView1_CellFormatting;
            dataGridView1.CellFormatting += dataGridView1_CellFormatting;
            dataGridView1.CellToolTipTextNeeded -= dataGridView1_CellToolTipTextNeeded;
            dataGridView1.CellToolTipTextNeeded += dataGridView1_CellToolTipTextNeeded;
            dataGridView1.ShowCellToolTips = true;
            dataGridView1.DataError -= dataGridView1_DataError;
            dataGridView1.DataError += dataGridView1_DataError;
            dataGridView2.DataError -= dataGridView2_DataError;
            dataGridView2.DataError += dataGridView2_DataError;
            periodTimer.Tick += periodTimer_Tick;
        }

        private async void IskoriscenjeTrasa_Shown(object sender, EventArgs e)
        {
            spremna = true;
            await FillDGV();
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            if (!spremna) return;
            periodTimer.Stop();
            periodTimer.Start();
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            if (!spremna) return;
            periodTimer.Stop();
            periodTimer.Start();
        }

        private async void periodTimer_Tick(object sender, EventArgs e)
        {
            periodTimer.Stop();
            if (cuvanjeCene) { periodTimer.Start(); return; }
            await FillDGV();
        }

        private async Task FillDGV(bool nakonCuvanja = false)
        {
            if (!nakonCuvanja)
            {
                if (cuvanjeCene) { periodTimer.Stop(); periodTimer.Start(); return; }
                if (!ValidateChildren() || !dataGridView2.EndEdit()) return;
                if (cuvanjeCene) { periodTimer.Stop(); periodTimer.Start(); return; }
            }
            int brojZahteva = ++zahtev;
            DateTime od = dateTimePicker1.Value.Date;
            DateTime doDana = dateTimePicker2.Value.Date;
            if (od > doDana)
            {
                dataGridView1.DataSource = null;
                dataGridView2.DataSource = null;
                zbir = null;
                dataGridView1.Enabled = true;
                dataGridView2.Enabled = true;
                UseWaitCursor = false;
                MessageBox.Show(this, "Datum od ne može biti posle datuma do.", "Period");
                return;
            }

            dataGridView1.Enabled = false;
            dataGridView2.Enabled = false;
            UseWaitCursor = true;
            try
            {
                DateTime doIskljucivo = IskoriscenjeTrasaObracun.KrajPerioda(doDana);
                var podaci = await Task.Run(() =>
                {
                    var detalji = IskoriscenjeTrasaPodaci.Ucitaj(od, doIskljucivo);
                    var grupisano = IskoriscenjeTrasaObracun.Grupisi(detalji);
                    var infrastruktura = IskoriscenjeTrasaPodaci.UcitajInfrastrukturu(od, doIskljucivo, detalji);
                    infrastruktura.Columns.Add("BrojeviRadnihNaloga", typeof(string));
                    foreach (DataRow red in infrastruktura.Rows)
                        red["BrojeviRadnihNaloga"] = string.Join(", ", ProcitajBrojeveNaloga(Convert.ToString(red["RadniNalozi"])));
                    GridColumnFilters.Prepare(infrastruktura);
                    GridColumnFilters.Prepare(grupisano.Tabela);
                    return new { Detalji = infrastruktura, Zbir = grupisano };
                });
                if (IsDisposed || Disposing || brojZahteva != zahtev) return;
                povezivanje = true;
                try
                {
                    FillDGV1(podaci.Detalji);
                    FillDGV2(podaci.Zbir);
                }
                finally { povezivanje = false; }
            }
            catch (Exception ex)
            {
                if (!IsDisposed && !Disposing && brojZahteva == zahtev)
                {
                    dataGridView1.DataSource = null;
                    dataGridView2.DataSource = null;
                    zbir = null;
                    MessageBox.Show(this, ex.Message, "Učitavanje iskorišćenja trasa");
                }
            }
            finally
            {
                if (!IsDisposed && !Disposing && brojZahteva == zahtev)
                {
                    dataGridView1.Enabled = true;
                    dataGridView2.Enabled = true;
                    UseWaitCursor = false;
                }
            }
        }

        private void FillDGV1(DataTable podaci)
        {
            dataGridView1.DataSource = podaci;
        }

        private void FillDGV2(IskoriscenjeZbir podaci)
        {
            zbir = podaci;
            dataGridView2.DataSource = podaci.Tabela;
        }

        private void dataGridView1_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            filterDetalji.Bound(sender, e);
            string[] columns = { "RedniBroj", "Datum", "VremePolaska", "VremeDolaska", "BrojVoza", "Relacija", "Lokomotive", "BrojeviRadnihNaloga" };
            string[] titles = { "Redni broj", "Datum", "Vreme polaska", "Vreme dolaska", "Broj voza", "Relacija", "Serija vucnog vozila", "Radni nalog" };
            int[] widths = { 80, 110, 110, 110, 150, 380, 190, 140 };
            foreach (DataGridViewColumn column in dataGridView1.Columns)
                column.Visible = columns.Contains(column.DataPropertyName) || column.Name == "btnOtvoriRadniNalog";
            for (int i = 0; i < columns.Length; i++)
            {
                var column = dataGridView1.Columns[columns[i]];
                if (column == null) continue;
                column.HeaderText = titles[i]; column.DisplayIndex = i; column.Width = widths[i]; column.ReadOnly = true;
                column.DefaultCellStyle.NullValue = "";
                if (columns[i] == "Datum") column.DefaultCellStyle.Format = "dd.MM.yyyy";
                if (columns[i] == "VremePolaska" || columns[i] == "VremeDolaska") column.DefaultCellStyle.Format = "HH:mm";
            }
            DodajDugmeRadnogNaloga();
            dataGridView1.Columns["btnOtvoriRadniNalog"].DisplayIndex = columns.Length;
            dataGridView1.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
        }
        private void DodajDugmeRadnogNaloga()
        {
            if (dataGridView1.Columns.Contains("btnOtvoriRadniNalog")) return;
            var column = new DataGridViewButtonColumn();
            column.Name = "btnOtvoriRadniNalog";
            column.HeaderText = "";
            column.Text = "Otvori RN";
            column.UseColumnTextForButtonValue = true;
            column.Width = 100;
            column.ReadOnly = true;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            dataGridView1.Columns.Add(column);
        }
        internal static int[] ProcitajBrojeveNaloga(string veze)
        {
            return (veze ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => { int id; return int.TryParse(x.Split('/')[0].Trim(), out id) && id > 0 ? id : 0; })
                .Where(x => x > 0).Distinct().OrderBy(x => x).ToArray();
        }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
                dataGridView1.Columns[e.ColumnIndex].Name != "btnOtvoriRadniNalog") return;
            var red = dataGridView1.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (red == null) return;
            try
            {
                int[] brojevi = ProcitajBrojeveNaloga(Convert.ToString(red["RadniNalozi"]));
                if (brojevi.Length == 0)
                {
                    MessageBox.Show(this, "Za ovaj red nije sacuvana pouzdana veza sa radnim nalogom.");
                    return;
                }
                int broj = brojevi[0];
                if (brojevi.Length > 1)
                {
                    var podaci = new DataTable();
                    podaci.Columns.Add("RadniNalog", typeof(int));
                    foreach (int id in brojevi) podaci.Rows.Add(id);
                    using (var izbor = new IzborStaniceCenovnika("Radni nalog", "Ova voznja je povezana sa vise naloga. Izaberite koji zelite da otvorite.", podaci))
                    {
                        if (izbor.ShowDialog(this) != DialogResult.OK) return;
                        broj = Convert.ToInt32(izbor.IzabraniRed["RadniNalog"]);
                    }
                }
                var ter = new Saobracaj.Dokumenta.frmRadniNalog(broj.ToString(System.Globalization.CultureInfo.InvariantCulture));
                ter.Show();
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Otvaranje radnog naloga"); }
        }
        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex <= 0 || e.ColumnIndex < 0) return;
            string name = dataGridView1.Columns[e.ColumnIndex].DataPropertyName;
            if (name != "RedniBroj" && name != "Datum" && name != "BrojVoza") return;
            var current = dataGridView1.Rows[e.RowIndex].DataBoundItem as DataRowView;
            var previous = dataGridView1.Rows[e.RowIndex - 1].DataBoundItem as DataRowView;
            if (current != null && previous != null && Equals(current["GrupaVoznje"], previous["GrupaVoznje"]) && Equals(current[name], previous[name]))
            { e.Value = ""; e.FormattingApplied = true; }
        }
        private void dataGridView1_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var view = dataGridView1.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (view == null) return;
            e.ToolTipText = Convert.ToString(view["IzvorPodatka"]) + "\nRN/RB: " + view["RadniNalozi"] +
                "\nPolazak: " + (view.Row.IsNull("VremePolaska") ? "nije unet" : Convert.ToDateTime(view["VremePolaska"]).ToString("dd.MM.yyyy HH:mm:ss")) +
                "\nDolazak: " + (view.Row.IsNull("VremeDolaska") ? "nije unet" : Convert.ToDateTime(view["VremeDolaska"]).ToString("dd.MM.yyyy HH:mm:ss"));
        }

        private void dataGridView2_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            filterZbir.Bound(sender, e);
            foreach (DataGridViewColumn column in dataGridView2.Columns)
            {
                column.ReadOnly = column.DataPropertyName != "Cena";
                if (!column.Visible) continue;
                column.Width = 140;
                if (column.DataPropertyName == "Cena" || column.DataPropertyName == "CenaUkupno")
                {
                    column.DefaultCellStyle.Format = "N2";
                    column.DefaultCellStyle.NullValue = "";
                }
            }
            foreach (DataGridViewRow gridRow in dataGridView2.Rows)
            {
                var view = gridRow.DataBoundItem as DataRowView;
                if (view != null)
                    gridRow.Cells["Cena"].ReadOnly = !Convert.ToBoolean(view["adHoc"]);
            }
        }

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            filterDetalji.Header(sender, e);
        }

        private void dataGridView2_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            filterZbir.Header(sender, e);
        }

        private void dataGridView2_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            var view = dataGridView2.Rows[e.RowIndex].DataBoundItem as DataRowView;
            e.Cancel = povezivanje || cuvanjeCene || view == null ||
                dataGridView2.Columns[e.ColumnIndex].DataPropertyName != "Cena" ||
                !Convert.ToBoolean(view["adHoc"]);
        }

        private void dataGridView2_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (povezivanje || e.RowIndex < 0 ||
                dataGridView2.Columns[e.ColumnIndex].DataPropertyName != "Cena") return;
            try
            {
                IskoriscenjeTrasaObracun.ProcitajCenu(e.FormattedValue);
                dataGridView2.Rows[e.RowIndex].ErrorText = "";
            }
            catch (FormatException ex)
            {
                e.Cancel = true;
                dataGridView2.Rows[e.RowIndex].ErrorText = ex.Message;
                MessageBox.Show(this, ex.Message, "Cena ad hoc trase");
            }
        }

        private void dataGridView2_CellParsing(object sender, DataGridViewCellParsingEventArgs e)
        {
            if (e.RowIndex < 0 || dataGridView2.Columns[e.ColumnIndex].DataPropertyName != "Cena") return;
            try
            {
                e.Value = (object)IskoriscenjeTrasaObracun.ProcitajCenu(e.Value) ?? DBNull.Value;
                e.ParsingApplied = true;
            }
            catch (FormatException) { e.ParsingApplied = false; }
        }

        private async void dataGridView2_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (povezivanje || cuvanjeCene || zbir == null || e.RowIndex < 0 ||
                dataGridView2.Columns[e.ColumnIndex].DataPropertyName != "Cena") return;
            var view = dataGridView2.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (view == null || !Convert.ToBoolean(view["adHoc"])) return;
            dataGridView2.BindingContext[dataGridView2.DataSource].EndCurrentEdit();
            var row = view.Row;
            System.Collections.Generic.List<CenaPokretanja> pokretanja;
            if (!zbir.Grupe.TryGetValue(row, out pokretanja)) return;
            decimal? stara = pokretanja[0].StaraCena;
            decimal? nova = row.IsNull("Cena") ? (decimal?)null : Convert.ToDecimal(row["Cena"]);
            if (stara == nova) return;

            cuvanjeCene = true;
            dateTimePicker1.Enabled = false;
            dateTimePicker2.Enabled = false;
            dataGridView1.Enabled = false;
            dataGridView2.Enabled = false;
            dataGridView2.Refresh(); // The expression column recalculates immediately.
            try
            {
                await Task.Run(() => IskoriscenjeTrasaPodaci.SacuvajCenu(pokretanja, nova));
                if (!IsDisposed && !Disposing) await FillDGV(true);
            }
            catch (Exception ex)
            {
                if (!IsDisposed && !Disposing)
                {
                    povezivanje = true;
                    try { row["Cena"] = (object)stara ?? DBNull.Value; row.AcceptChanges(); }
                    finally { povezivanje = false; }
                    MessageBox.Show(this, ex.Message, "Cena nije sačuvana");
                }
            }
            finally
            {
                cuvanjeCene = false;
                if (!IsDisposed && !Disposing)
                {
                    dateTimePicker1.Enabled = true;
                    dateTimePicker2.Enabled = true;
                    dataGridView1.Enabled = true;
                    dataGridView2.Enabled = true;
                }
            }
        }

        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            DatabaseGrid.ReportDataError(dataGridView1, e);
        }

        private void dataGridView2_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            DatabaseGrid.ReportDataError(dataGridView2, e);
        }

        private void IskoriscenjeTrasa_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (cuvanjeCene) { e.Cancel = true; return; }
            if (!ValidateChildren() || !dataGridView2.EndEdit()) { e.Cancel = true; return; }
            if (cuvanjeCene) e.Cancel = true;
        }

        private void IskoriscenjeTrasa_FormClosed(object sender, FormClosedEventArgs e)
        {
            zahtev++;
            periodTimer.Stop();
            periodTimer.Dispose();
            filterDetalji.Dispose();
            filterZbir.Dispose();
        }
    }
}
