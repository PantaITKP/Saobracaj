using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Saobracaj.Agenti_jobs
{
    public sealed class GridColumnFilters : IDisposable
    {
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DataGridView, GridColumnFilters> Instances = new System.Runtime.CompilerServices.ConditionalWeakTable<DataGridView, GridColumnFilters>();
        public static void Reset(DataGridView grid)
        {
            GridColumnFilters filter;
            if (Instances.TryGetValue(grid, out filter))
                filter.Clear();
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DataTable, object> Prepared =
            new System.Runtime.CompilerServices.ConditionalWeakTable<DataTable, object>();

        public static void Prepare(DataTable data)
        {
            object marker;
            if (data == null || Prepared.TryGetValue(data, out marker)) return;
            data.BeginLoadData();
            try
            {
                var columns = data.Columns.Cast<DataColumn>()
                    .Where(c => !c.ColumnName.StartsWith("__filter_") && c.DataType != typeof(byte[])).ToArray();
                foreach (var column in columns)
                {
                    string key = "__filter_" + column.ColumnName;
                    if (!data.Columns.Contains(key)) data.Columns.Add(key, typeof(string));
                }
                foreach (DataRow row in data.Rows)
                    foreach (var column in columns)
                        row["__filter_" + column.ColumnName] = Normalize(row[column]);
                data.AcceptChanges();
                Prepared.Add(data, new object());
            }
            finally { data.EndLoadData(); }
        }

        private readonly DataGridView grid;
        private readonly Dictionary<string, string> filters = new Dictionary<string, string>();
        private readonly ToolStripDropDown popup = new ToolStripDropDown();
        private readonly ToolStripTextBox input = new ToolStripTextBox
        {
            AutoSize = false,
            Width = 260
        };
        private DataTable table;
        private bool filling;
        private string active = "";
        private readonly Dictionary<string, string> captions = new Dictionary<string, string>();
        public GridColumnFilters(DataGridView target)
        {
            grid = target;
            Instances.Remove(grid);
            Instances.Add(grid, this);
            popup.Items.Add(input);
            popup.Items.Add("Obriši filter", null, clearFilter_Click);
            popup.Items.Add("Vrati sve", null, resetFilters_Click);
            input.TextChanged += input_TextChanged;
        }

        private void clearFilter_Click(object sender, EventArgs e)
        {
            input.Text = "";
        }

        private void resetFilters_Click(object sender, EventArgs e)
        {
            Clear();
        }

        private void input_TextChanged(object sender, EventArgs e)
        {
            if (filling || active == "")
                return;
            if (input.Text.Trim() == "")
                filters.Remove(active);
            else
                filters[active] = input.Text.Trim();
            Apply();
        }

        public void Bound(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (filling) return;
            var view = grid.DataSource as DataView;
            var data = grid.DataSource as DataTable;
            data = data ?? (view == null ? null : view.Table);
            if (data != null && table != data)
            {
                if (table != null)
                    table.ColumnChanged -= Changed;
                table = data;
                Setup();
                table.ColumnChanged += Changed;
            }

            Headers();
        }

        private void Setup()
        {
            filling = true;
            try { Prepare(table); }
            finally { filling = false; }
        }

        private void Changed(object sender, DataColumnChangeEventArgs e)
        {
            if (filling || e.Column.ColumnName.StartsWith("__filter_"))
                return;
            var key = "__filter_" + e.Column.ColumnName;
            if (!table.Columns.Contains(key))
                return;
            filling = true;
            try
            {
                e.Row[key] = Normalize(e.ProposedValue);
            }
            finally
            {
                filling = false;
            }
        }

        private static string Normalize(object value)
        {
            return StationNames.Normalize(value == DBNull.Value ? "" : Convert.ToString(value, CultureInfo.CurrentCulture));
        }

        public void Header(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || table == null)
                return;
            if (!grid.EndEdit())
                return;
            grid.BindingContext[grid.DataSource].EndCurrentEdit();
            var col = grid.Columns[e.ColumnIndex];
            active = col.DataPropertyName;
            if (active == "" || !table.Columns.Contains("__filter_" + active))
                return;
            filling = true;
            input.Text = filters.ContainsKey(active) ? filters[active] : "";
            filling = false;
            var rect = grid.GetCellDisplayRectangle(e.ColumnIndex, -1, true);
            popup.Show(grid, new Point(Math.Max(0, rect.Left), rect.Bottom));
            input.Focus();
            input.SelectAll();
        }

        private void Apply()
        {
            if (table == null)
                return;
            var parts = new List<string>();
            foreach (var pair in filters)
            {
                var col = "__filter_" + pair.Key;
                if (!table.Columns.Contains(col))
                    continue;
                var text = StationNames.Normalize(pair.Value).Replace("'", "''");
                parts.Add("[" + col.Replace("]", "\\]") + "] LIKE '%" + text + "%'");
            }

            table.DefaultView.RowFilter = string.Join(" AND ", parts);
            Headers();
        }

        private void Headers()
        {
            foreach (DataGridViewColumn col in grid.Columns)
            {
                var key = col.DataPropertyName;
                if (key.StartsWith("__filter_") || key == "VerzijaReda" || key == "FullId")
                {
                    col.Visible = false;
                    continue;
                }

                if (!captions.ContainsKey(key))
                    captions[key] = col.HeaderText.Split('\n')[0];
                col.HeaderText = captions[key] + (filters.ContainsKey(key) ? "\nFilter: " + filters[key] : "");
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
                col.ToolTipText = "Klik za pretragu ove kolone. Vrati sve je u meniju desnog klika.";
            }

            if (table != null && table.DefaultView.RowFilter == "" && filters.Count > 0)
                ApplyInitial();
        }

        private void ApplyInitial()
        {
            var valid = filters.Where(f => table.Columns.Contains("__filter_" + f.Key)).ToList();
            if (valid.Count > 0)
                table.DefaultView.RowFilter = string.Join(" AND ", valid.Select(f => "[__filter_" + f.Key + "] LIKE '%" + StationNames.Normalize(f.Value).Replace("'", "''") + "%'"));
        }

        public void Clear()
        {
            filters.Clear();
            if (table != null)
                table.DefaultView.RowFilter = "";
            filling = true;
            input.Text = "";
            filling = false;
            popup.Close();
            Headers();
        }

        public void Dispose()
        {
            if (table != null)
                table.ColumnChanged -= Changed;
            grid.ColumnHeaderMouseClick -= Header;
            grid.DataBindingComplete -= Bound;
            popup.Dispose();
        }
    }

    public static class GridTitles
    {
        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            { "StartedAtLocal", "Vreme pokretanja" },
            { "Completed", "Završen" },
            { "CompletedAtLocal", "Vreme završetka" },
            { "Kola", "Broj i serija vagona" },
            {
                "From",
                "Otpravna stanica"
            },
            {
                "To",
                "Uputna stanica"
            },
            {
                "Train",
                "Broj voza"
            },
            {
                "Weight",
                "Težina (t)"
            },
            {
                "MaxWeight",
                "Dozvoljena težina (t)"
            },
            {
                "Price",
                "Cena iz Excela"
            },
            {
                "Departure",
                "Polazak"
            },
            {
                "Arrival",
                "Dolazak"
            },
            {
                "Days",
                "Saobraća dana"
            },
            {
                "Locomotive",
                "Lokomotive"
            },
            {
                "Length",
                "Dužina"
            },
            {
                "Note",
                "Napomena"
            },
            {
                "LocomotiveNumber",
                "Broj lokomotive"
            },
            {
                "AnnouncementNumber",
                "Broj najave"
            },
            {
                "AnnouncementPayer",
                "Platilac najave"
            },
            {
                "IsAdHoc",
                "Ad hoc"
            },
            {
                "Started",
                "Pokrenut"
            },
            {
                "StartedAtUtc",
                "Vreme pokretanja (UTC)"
            },
            {
                "Stopped",
                "Izostaje"
            },
            {
                "StopStation",
                "Stanica zaustavljanja"
            },
            {
                "Stanica",
                "Trenutna stanica"
            },
            {
                "UputnaStanica",
                "Uputna stanica"
            },
            {
                "IDVoza",
                "ID voza"
            },
            {
                "BrojVoza",
                "Broj voza"
            },
            {
                "GranicniPrelaz",
                "Granični prelaz"
            },
            {
                "OcekivanoVreme",
                "Očekivano vreme"
            },
            {
                "StatusNajave",
                "Status najave"
            },
            {
                "IzmenjenoUtc",
                "Izmenjeno (UTC)"
            }
        };
        public static void Configure(DataGridView grid, bool editable)
        {
            grid.AutoGenerateColumns = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.BackgroundColor = Color.White;
            grid.ReadOnly = !editable;
            grid.AllowUserToAddRows = editable;
            grid.AllowUserToDeleteRows = editable;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            grid.ColumnHeadersHeight = 42;
            grid.AllowUserToResizeColumns = true;
        }

        public static void Apply(DataGridView grid)
        {
            foreach (DataGridViewColumn col in grid.Columns)
            {
                var key = col.DataPropertyName;
                if (Names.ContainsKey(key))
                    col.HeaderText = Names[key];
                if (key == "CenovnikDokumentID" || key == "CenovnikVaziOd" || key == "CenovnikVaziDo" || key == "CenovnikGodina" || key == "SifarnikTrasaID" || key == "PokretanjeID" || key == "StartedAtUtc" || key == "CompletionRecorded" || key == "CompletedRecordedAtLocal" || key == "AnnouncementWagons" || key == "ID" || key == "DocumentID" || key == "RouteID" || key == "OriginalTrain" || key == "FullId" || key == "AnnouncementFullId" || key == "VerzijaReda" || key == "CreatedUtc" || key == "CreatedBy" || key == "IzmenjenoUtc" || key.StartsWith("__filter_"))
                    col.Visible = false;
            }
        }
    }

    // Uses existing Designer-created grids; no replacement controls are instantiated here.
    public sealed class DatabaseGrid : IDisposable
    {
        private readonly DataGridView grid;
        private readonly string name;
        private readonly System.Windows.Forms.Timer saveTimer = new System.Windows.Forms.Timer
        {
            Interval = 800
        };
        private bool saving, loaded, configuringColumns;
        public bool IsLoaded { get { return loaded && !saving; } }
        public bool IsEditing
        {
            get
            {
                var view = grid.CurrentRow == null ? null : grid.CurrentRow.DataBoundItem as DataRowView;
                return grid.IsCurrentCellInEditMode || (view != null && view.IsEdit);
            }
        }
        public DataTable Data { get; private set; }

        public DatabaseGrid(DataGridView g, string table)
        {
            grid = g;
            name = table;
            GridTitles.Configure(g, true);
            saveTimer.Tick += saveTimer_Tick;
        }

        private void saveTimer_Tick(object sender, EventArgs e)
        {
            saveTimer.Stop();
            if (grid.IsDisposed) return;
            var current = grid.CurrentRow == null ? null : grid.CurrentRow.DataBoundItem as DataRowView;
            if (grid.ContainsFocus && (grid.IsCurrentCellInEditMode ||
                (current != null && (current.IsEdit || current.IsNew))))
            {
                saveTimer.Start();
                return;
            }
            try
            {
                Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Čuvanje tabele");
            }
        }

        public void Schedule()
        {
            if (!loaded || saving)
                return;
            saveTimer.Stop();
            saveTimer.Start();
        }

        public void Reload()
        {
            if (HasPending)
                throw new InvalidOperationException("Prvo sačuvajte ili odbacite lokalne izmene tabele.");
            Bind(LoadPrepared(name));
        }

        public static DataTable LoadPrepared(string table, bool synchronizeStarts = true)
        {
            var data = OperationsRepository.LoadGrid(table, synchronizeStarts);
            GridColumnFilters.Prepare(data);
            return data;
        }

        public void Bind(DataTable data)
        {
            if (HasPending)
                throw new InvalidOperationException("Tabela ima nesačuvane izmene.");
            loaded = false;
            grid.SuspendLayout();
            try
            {
                Data = data;
                GridColumnFilters.Prepare(Data);
                grid.DataSource = Data;
                ConfigureTrainColumns();
                ConfigureWidths();
            }
            finally { loaded = true; grid.ResumeLayout(); }
        }

        private void ConfigureWidths()
        {
            foreach (DataGridViewColumn c in grid.Columns)
            {
                if (!c.Visible) continue;
                string key = c.DataPropertyName;
                c.Width = c is DataGridViewCheckBoxColumn ? 82 :
                    (key == "Note" || key == "Napomena") ? 260 :
                    (key == "From" || key == "To" || key.Contains("Stanica")) ? 180 :
                    (key == "StartedAtLocal") ? 180 :
                    (key == "CompletedAtLocal") ? 180 : 125;
            }
        }

        private void ConfigureTrainColumns()
        {
            if (name != "OperativniPlanVozovi" || configuringColumns)
                return;
            configuringColumns = true;
            try
            {
                grid.AllowUserToAddRows = false;
                foreach (DataGridViewColumn c in grid.Columns)
                {
                    c.ReadOnly = !OperationsRepository.EditableTables[name].Contains(c.DataPropertyName);
                    if (c.DataPropertyName == "StartedAtLocal" || c.DataPropertyName == "StartedAtUtc")
                        c.ReadOnly = true;
                    if (c.DataPropertyName.StartsWith("Cenovnik") || c.DataPropertyName == "SifarnikTrasaID" || c.DataPropertyName == "PokretanjeID" || c.DataPropertyName == "Price" || c.DataPropertyName == "StartedAtUtc" ||
                        c.DataPropertyName == "CompletionRecorded" || c.DataPropertyName == "CompletedRecordedAtLocal" ||
                        c.DataPropertyName == "AnnouncementWagons")
                        c.Visible = false;
                    if (c.DataPropertyName == "StartedAtLocal")
                    {
                        c.HeaderText = "Vreme pokretanja";
                        c.DefaultCellStyle.Format = "dd.MM.yyyy HH:mm:ss";
                    }
                }
                grid.Columns["Completed"].HeaderText = "Završen";
                grid.Columns["Completed"].DisplayIndex = grid.Columns["Stopped"].DisplayIndex + 1;
                grid.Columns["CompletedAtLocal"].DisplayIndex = grid.Columns["Completed"].DisplayIndex + 1;
                grid.Columns["CompletedAtLocal"].HeaderText = "Vreme završetka";
                grid.Columns["CompletedAtLocal"].DefaultCellStyle.Format = "dd.MM.yyyy HH:mm:ss";
                grid.Columns["CompletedAtLocal"].DefaultCellStyle.NullValue = "";
                grid.Columns["CompletedAtLocal"].ToolTipText = "Unesite datum i vreme završetka: dd.MM.yyyy HH:mm:ss. Dozvoljen je i unos bez sekundi.";
            }
            finally { configuringColumns = false; }
        }

        public void Save()
        {
            saveTimer.Stop();
            if (!loaded || saving || Data == null)
                return;
            if (!grid.EndEdit())
                throw new InvalidOperationException("Završite unos u tabeli.");
            grid.BindingContext[grid.DataSource].EndCurrentEdit();
            saving = true;
            try
            {
                if (OperationsRepository.SaveGrid(name, Data))
                {
                    var active = grid.CurrentRow == null ? null : grid.CurrentRow.DataBoundItem as DataRowView;
                    object id = active == null || active.Row.RowState == DataRowState.Added ? null : active.Row["ID"];
                    loaded = false;
                    Data = LoadPrepared(name, false);
                    grid.SuspendLayout();
                    try
                    {
                        grid.DataSource = Data;
                        ConfigureTrainColumns();
                        ConfigureWidths();
                    }
                    finally { grid.ResumeLayout(); }
                    if (id != null)
                        foreach (DataGridViewRow row in grid.Rows)
                        {
                            var rv = row.DataBoundItem as DataRowView;
                            if (rv != null && Equals(rv["ID"], id))
                            {
                                grid.CurrentCell = row.Cells.Cast<DataGridViewCell>().First(c => c.Visible);
                                break;
                            }
                        }
                }
            }
            finally
            {
                loaded = true;
                saving = false;
            }
        }

        public void DiscardAndReload()
        {
            saveTimer.Stop();
            if (Data != null)
                Data.RejectChanges();
            Reload();
        }

        public bool HasPending
        {
            get
            {
                return Data != null && Data.GetChanges() != null;
            }
        }

        public static void ReportDataError(DataGridView target, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            string column = e.ColumnIndex >= 0 && e.ColumnIndex < target.Columns.Count
                ? target.Columns[e.ColumnIndex].HeaderText : "?";
            string message = target.Name + ", red " + (e.RowIndex + 1) + ", kolona " + column +
                ", kontekst " + e.Context;
            var error = e.Exception ?? new InvalidOperationException("Nepoznata greška tabele.");
            OperationalDiagnostics.Write(message + Environment.NewLine + error);
            MessageBox.Show("Greška u tabeli " + target.Name + ", kolona " + column +
                ": " + error.Message, "Podaci u tabeli");
        }

        public void Dispose()
        {
            saveTimer.Stop();
            saveTimer.Dispose();
        }
    }

}
