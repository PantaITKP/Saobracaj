using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Saobracaj.Agenti_jobs
{
    public partial class DispMain : Form
    {
        private readonly Dictionary<long, bool> lastStarted = new Dictionary<long, bool>();
        private bool changingTrainStatus, committingCheck, statusSaveQueued;
        private DatabaseGrid gridMreza;
        private DatabaseGrid gridNajave;
        private DatabaseGrid gridLokomotive;
        private DatabaseGrid gridVozovi;
        private GridColumnFilters filterMreza;
        private GridColumnFilters filterNajave;
        private GridColumnFilters filterLokomotive;
        private GridColumnFilters filterVozovi;
        private GridColumnFilters filterTrase;
        private GridColumnFilters filterNajava;
        private readonly Timer refreshTimer = new Timer
        {
            Interval = 5000
        };
        private RouteCatalog catalog = new RouteCatalog();
        private List<RouteInfo> proposed = new List<RouteInfo>();
        private PlanSnapshot plan = new PlanSnapshot();
        private bool ready, busy, updating, shiftDown, notePainting, noteDirty;
        private ShiftNoteSnapshot shiftNote;
        private readonly Timer noteTimer = new Timer
        {
            Interval = 900
        };
        private string proposedFrom = "", proposedTo = "";
        private decimal proposedWeight;
        public DispMain() : this(null)
        {
        }

        public DispMain(string user)
        {
            InitializeComponent();
            txtOtpravna.TextChanged -= txtOtpravna_TextChanged;
            txtOtpravna.TextChanged += txtOtpravna_TextChanged;
            txtUputna.TextChanged -= txtUputna_TextChanged;
            txtUputna.TextChanged += txtUputna_TextChanged;
            txtOtpravna.KeyDown -= txtOtpravna_KeyDown;
            txtOtpravna.KeyDown += txtOtpravna_KeyDown;
            txtUputna.KeyDown -= txtUputna_KeyDown;
            txtUputna.KeyDown += txtUputna_KeyDown;
            // Your existing Designer button is retained; no replacement Designer is needed.
            btnEvidentiraj.Click -= btnEvidentiraj_Click;
            btnEvidentiraj.Click += btnEvidentiraj_Click;
            dgvVozovi.CellParsing -= dgvVozovi_CellParsing;
            dgvVozovi.CellParsing += dgvVozovi_CellParsing;
            dgvVozovi.CellValidating -= dgvVozovi_CellValidating;
            dgvVozovi.CellValidating += dgvVozovi_CellValidating;
            if (!string.IsNullOrWhiteSpace(Saobracaj.Sifarnici.frmLogovanje.user))
                OperationalSession.UserName = Saobracaj.Sifarnici.frmLogovanje.user.Trim();
            filterMreza = new GridColumnFilters(dgvMreza);
            filterNajave = new GridColumnFilters(dgvNajave);
            filterLokomotive = new GridColumnFilters(dgvLokomotive);
            filterVozovi = new GridColumnFilters(dgvVozovi);
            filterTrase = new GridColumnFilters(dgvTrase);
            filterNajava = new GridColumnFilters(dgvNajava);
            if (!string.IsNullOrWhiteSpace(user))
                OperationalSession.UserName = user;
            numericUpDown1.Maximum = 100000;
            numericUpDown1.Minimum = 0;
            numericUpDown2.Minimum = 0;
            numericUpDown2.Maximum = 100000;
            numericUpDown2.DecimalPlaces = 3;
            AdhocPolje.CheckedChanged -= chkAdhock_CheckedChanged;
            AdhocPolje.CheckedChanged -= chkAdhoc_CheckedChanged;
            AdhocPolje.CheckedChanged += chkAdhoc_CheckedChanged;
            richTextBox1.ReadOnly = true;
            richTextBox1.HideSelection = false;






            gridMreza = new DatabaseGrid(dgvMreza, "OperativniPlanMreza");
            gridNajave = new DatabaseGrid(dgvNajave, "OperativniPlanNajave");
            gridLokomotive = new DatabaseGrid(dgvLokomotive, "OperativniPlanLokomotive");
            gridVozovi = new DatabaseGrid(dgvVozovi, "OperativniPlanVozovi");
            GridTitles.Configure(dgvTrase, false);
            GridTitles.Configure(dgvNajava, false);
            SetAdhoc();
            noteTimer.Tick += noteTimer_Tick;
            refreshTimer.Tick += refreshTimer_Tick;
        }

        private void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Dispečer");
            }
        }

        private async Task Start()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            busy = true;
            try
            {
                // Independent reads use separate pooled connections. Other tabs load on demand.
                var catalogTask = Task.Run(() => OperationalDiagnostics.Measure("Učitavanje kataloga", () => OperationsRepository.LoadCatalog()));
                var planTask = Task.Run(() => OperationalDiagnostics.Measure("Učitavanje plana", () => PlanRepository.Load()));
                var noteTask = Task.Run(() => OperationalDiagnostics.Measure("Učitavanje napomene", () => ShiftNoteRepository.Load()));
                var trainsTask = Task.Run(() => OperationalDiagnostics.Measure("Učitavanje vozova", () => DatabaseGrid.LoadPrepared("OperativniPlanVozovi")));
                Exception announcementError = null;
                var announcementsTask = Task.Run(() =>
                {
                    try
                    {
                        return OperationalDiagnostics.Measure("Učitavanje najava iz baze", () =>
                        {
                            var data = OperationsRepository.LoadNajava();
                            GridColumnFilters.Prepare(data);
                            return data;
                        });
                    }
                    catch (Exception ex)
                    {
                        announcementError = ex;
                        OperationalDiagnostics.Write("Najave: " + ex);
                        return new DataTable();
                    }
                });
                await Task.WhenAll(catalogTask, planTask, noteTask, trainsTask);
                if (IsDisposed || Disposing) return;
                catalog = catalogTask.Result;
                plan = planTask.Result;
                shiftNote = noteTask.Result;

                notePainting = true;
                richTextBox2.Text = shiftNote.Text;
                notePainting = false;
                PlanTextView.Paint(richTextBox1, plan);
                gridVozovi.Bind(trainsTask.Result);
                OperationalDiagnostics.Write("Glavni spisak prikazan: " + watch.ElapsedMilliseconds + " ms");
                ready = true;
                Text = "Dispečer — " + (catalog.FileName == "" ? "Planer još nije uvezao red vožnje" : catalog.FileName);
                await LoadSelectedTab();
                var announcements = await announcementsTask;
                if (IsDisposed || Disposing) return;
                dgvNajava.DataSource = announcements;
                await LoadSelectedTab();
                if (announcementError != null)
                    MessageBox.Show(this, "Najave nisu učitane: " + announcementError.Message, "Najave iz baze");
                refreshTimer.Start();
            }
            catch (Exception ex)
            {
                notePainting = false;
                OperationalDiagnostics.Write("Otvaranje forme: " + ex);
                MessageBox.Show(this, ex.Message, "Učitavanje dispečera");
            }
            finally
            {
                busy = false;
                OperationalDiagnostics.Write("Otvaranje forme ukupno: " + watch.ElapsedMilliseconds + " ms");
            }
        }

        private async Task LoadSelectedTab(bool reload = false)
        {
            DatabaseGrid target = null;
            string table = null;
            if (tabControl1.SelectedTab == tabMreza) { target = gridMreza; table = "OperativniPlanMreza"; }
            if (tabControl1.SelectedTab == tabNajave) { target = gridNajave; table = "OperativniPlanNajave"; }
            if (tabControl1.SelectedTab == tabLokomotive) { target = gridLokomotive; table = "OperativniPlanLokomotive"; }
            if (target == null || (!reload && target.IsLoaded) || target.HasPending || target.IsEditing) return;
            var data = await Task.Run(() => OperationalDiagnostics.Measure("Učitavanje taba " + table, () => DatabaseGrid.LoadPrepared(table)));
            if (!IsDisposed && !Disposing && !target.HasPending && !target.IsEditing) target.Bind(data);
        }

        private async Task RefreshShared()
        {
            if (!ready || busy || IsDisposed || Control.MouseButtons == MouseButtons.Left)
                return;
            busy = true;
            try
            {
                var data = await Task.Run(() => new { Plan = PlanRepository.Load(), Catalog = OperationsRepository.ActiveCatalogID() == catalog.DocumentID ? catalog : OperationsRepository.LoadCatalog(), Note = ShiftNoteRepository.Load() });
                if (IsDisposed)
                    return;
                if (!noteDirty)
                {
                    shiftNote = data.Note;
                    notePainting = true;
                    richTextBox2.Text = shiftNote.Text;
                    notePainting = false;
                }

                bool changed = plan.VersionID != data.Plan.VersionID || plan.Text != data.Plan.Text ||
                    !plan.Marks.Select(m => m.ID).SequenceEqual(data.Plan.Marks.Select(m => m.ID));
                plan = data.Plan;
                if (changed) PlanTextView.Paint(richTextBox1, plan);
                if (catalog.DocumentID != data.Catalog.DocumentID)
                {
                    catalog = data.Catalog;

                    ClearProposal();
                    Text = "Dispečer — " + catalog.FileName;
                }
            }
            catch (Exception ex)
            {
                Text = "Dispečer — osvežavanje nije uspelo: " + ex.Message;
            }
            finally
            {
                busy = false;
            }
        }

        private CheckBox AdhocPolje
        {
            get
            {
                var control = Controls.Find("chkAdhoc", true).FirstOrDefault() as CheckBox;
                control = control ?? Controls.Find("chkAdhock", true).FirstOrDefault() as CheckBox;
                if (control == null) throw new InvalidOperationException("Nedostaje AdHoc checkbox chkAdhoc ili chkAdhock.");
                return control;
            }
        }
        private void chkAdhoc_CheckedChanged(object sender, EventArgs e) { SetAdhoc(); }
        private void numericUpDown2_ValueChanged(object sender, EventArgs e) { /* Tezina AdHoc voznje cita se pri dodavanju. */ }

        private void SetAdhoc()
        {
            txtOtpravnaAH.Enabled = AdhocPolje.Checked;
            txtUputnaAH.Enabled = AdhocPolje.Checked;
            txtTrasa.Enabled = AdhocPolje.Checked;
            btnAdhoc.Enabled = AdhocPolje.Checked;
            numericUpDown2.Enabled = AdhocPolje.Checked;
            txtOtpravna.Enabled = !AdhocPolje.Checked;
            txtUputna.Enabled = !AdhocPolje.Checked;
            btnPredloziTrase.Enabled = !AdhocPolje.Checked;
            ClearProposal();
        }

        private void ClearProposal()
        {
            proposed.Clear();
            dgvTrase.DataSource = null;
            btnDodeliTrasu.Enabled = false;
        }




        private static DataTable RouteTable(IEnumerable<RouteInfo> rows)
        {
            var t = new DataTable();
            foreach (var col in new[]
            {
                "ID",
                "Train",
                "From",
                "To",
                "MaxWeight",
                "Price",
                "Departure",
                "Arrival",
                "Days",
                "Locomotive",
                "Length",
                "Note"
            }

            )
                t.Columns.Add(col, col == "ID" ? typeof(long) : col == "MaxWeight" || col == "Price" ? typeof(decimal) : typeof(string));
            foreach (var r in rows)
                t.Rows.Add(r.ID, r.Train, r.From, r.To, (object)r.MaxWeight ?? DBNull.Value, (object)r.Price ?? DBNull.Value, r.Departure, r.Arrival, r.Days, r.Locomotive, r.Length, r.Note);
            return t;
        }

        private DataRow Selected(DataGridView grid)
        {
            var view = grid.CurrentRow == null ? null : grid.CurrentRow.DataBoundItem as DataRowView;
            if (view == null)
                throw new InvalidOperationException("Označite red u odgovarajućoj tabeli.");
            return view.Row;
        }

        private void FillDGVVozovi(long select = 0)
        {
            gridVozovi.Reload();
            GridColumnFilters.Reset(dgvVozovi);
            if (select > 0)
                foreach (DataGridViewRow row in dgvVozovi.Rows)
                {
                    var view = row.DataBoundItem as DataRowView;
                    if (view != null && Convert.ToInt64(view["ID"]) == select)
                    {
                        dgvVozovi.CurrentCell = row.Cells.Cast<DataGridViewCell>().First(c => c.Visible && !c.ReadOnly);
                        break;
                    }
                }
        }

        private void FillDGVNajava()
        {
            try
            {
                dgvNajava.DataSource = OperationsRepository.LoadNajava();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Najave iz postojeće baze nisu učitane: " + ex.Message);
            }
        }

        private void LinkAnnouncement(bool remove)
        {
            gridVozovi.Save();
            var train = Selected(dgvVozovi);
            DataRow announcement = remove ? null : Selected(dgvNajava);
            string full = remove ? null : Db.Text(announcement, "FullId");
            if (remove && MessageBox.Show(this, "Ukloniti vezu sa najavom?", "Potvrda", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;
            long id = Convert.ToInt64(train["ID"]);
            OperationsRepository.LinkNajava(id, (byte[])train["VerzijaReda"], full, remove ? null : Db.Text(announcement, "Platilac"), remove ? null : Db.Text(announcement, "Kola"));
            FillDGVVozovi(id);
        }

        private void SaveShiftNote()
        {
            noteTimer.Stop();
            if (!noteDirty || shiftNote == null)
                return;
            shiftNote = ShiftNoteRepository.Save(richTextBox2.Text, shiftNote.Version);
            noteDirty = false;
        }

        private static bool IsShift(KeyEventArgs e)
        {
            return e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey;
        }

        private async Task MarkSelection(KeyEventArgs e)
        {
            if (!IsShift(e) || shiftDown)
                return;
            shiftDown = true;
            if (busy || plan.VersionID == 0 || richTextBox1.SelectionLength == 0)
                return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            int start = richTextBox1.SelectionStart, len = richTextBox1.SelectionLength, planID = plan.PlanID, version = plan.VersionID;
            var text = PlanRepository.Normalize(richTextBox1.SelectedText);
            bool remove = e.Control;
            busy = true;
            try
            {
                await Task.Run(() => PlanRepository.Mark(planID, version, start, len, text, remove));
                plan = await Task.Run(() => PlanRepository.Load(planID));
                if (!IsDisposed)
                    PlanTextView.Paint(richTextBox1, plan);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Oznaka plana nije sačuvana");
            }
            finally
            {
                busy = false;
            }
        }

        private void SaveGrids()
        {
            gridMreza.Save();
            gridNajave.Save();
            gridLokomotive.Save();
            gridVozovi.Save();
        }

        private void DisposeGrids()
        {
            gridMreza.Dispose();
            filterMreza.Dispose();
            gridNajave.Dispose();
            filterNajave.Dispose();
            gridLokomotive.Dispose();
            filterLokomotive.Dispose();
            gridVozovi.Dispose();
            filterVozovi.Dispose();
            filterTrase.Dispose();
            filterNajava.Dispose();
        }

        private void FillDGV()
        {
            FillDGVMreza();
            FillDGVNajave();
            FillDGVLokomotive();
            FillDGVVozovi();
        }

        private void FillDGVMreza()
        {
            gridMreza.Reload();
        }

        private void FillDGVNajave()
        {
            gridNajave.Reload();
        }

        private void FillDGVLokomotive()
        {
            gridLokomotive.Reload();
        }

        private void FillDGVTrase()
        {
            dgvTrase.DataSource = RouteTable(proposed);
        }



        private void dgvMreza_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            filterMreza.Header(sender, e);
        }

        private void dgvMreza_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            GridTitles.Apply(dgvMreza);
            filterMreza.Bound(sender, e);
        }

        private void dgvMreza_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            gridMreza.Schedule();
        }

        private void dgvMreza_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && gridMreza.HasPending)
                gridMreza.Schedule();
        }

        private void dgvMreza_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            // Brisanje podreda ne zahteva potvrdu ni proveru pokretanja.
            // Glavni red uklanja ceo lanac; podred uklanja sebe i naredne nastavke.
        }

        private void dgvMreza_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            gridMreza.Schedule();
        }

        private void dgvMreza_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            DatabaseGrid.ReportDataError(dgvMreza, e);
        }

        private void dgvMreza_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvMreza.IsCurrentCellDirty && dgvMreza.CurrentCell is DataGridViewCheckBoxCell)
                dgvMreza.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void dgvMreza_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
                gridMreza.Schedule();
        }

        private void dgvNajave_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            filterNajave.Header(sender, e);
        }

        private void dgvNajave_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            GridTitles.Apply(dgvNajave);
            filterNajave.Bound(sender, e);
        }

        private void dgvNajave_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            gridNajave.Schedule();
        }

        private void dgvNajave_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && gridNajave.HasPending)
                gridNajave.Schedule();
        }

        private void dgvNajave_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (MessageBox.Show(this, "Obrisati označeni red?", "Potvrda", MessageBoxButtons.YesNo) != DialogResult.Yes)
                e.Cancel = true;
        }

        private void dgvNajave_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            gridNajave.Schedule();
        }

        private void dgvNajave_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            DatabaseGrid.ReportDataError(dgvNajave, e);
        }

        private void dgvNajave_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvNajave.IsCurrentCellDirty && dgvNajave.CurrentCell is DataGridViewCheckBoxCell)
                dgvNajave.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void dgvNajave_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
                gridNajave.Schedule();
        }

        private void dgvLokomotive_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            filterLokomotive.Header(sender, e);
        }

        private void dgvLokomotive_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            GridTitles.Apply(dgvLokomotive);
            filterLokomotive.Bound(sender, e);
        }

        private void dgvLokomotive_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            gridLokomotive.Schedule();
        }

        private void dgvLokomotive_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && gridLokomotive.HasPending)
                gridLokomotive.Schedule();
        }

        private void dgvLokomotive_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (MessageBox.Show(this, "Obrisati označeni red?", "Potvrda", MessageBoxButtons.YesNo) != DialogResult.Yes)
                e.Cancel = true;
        }

        private void dgvLokomotive_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            gridLokomotive.Schedule();
        }

        private void dgvLokomotive_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            DatabaseGrid.ReportDataError(dgvLokomotive, e);
        }

        private void dgvLokomotive_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvLokomotive.IsCurrentCellDirty && dgvLokomotive.CurrentCell is DataGridViewCheckBoxCell)
                dgvLokomotive.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void dgvLokomotive_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
                gridLokomotive.Schedule();
        }

        private void dgvVozovi_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            filterVozovi.Header(sender, e);
        }

        private void dgvVozovi_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            GridTitles.Apply(dgvVozovi);
            filterVozovi.Bound(sender, e);
            bool resetting = gridVozovi == null || !gridVozovi.IsLoaded;
            if (resetting) lastStarted.Clear();
            foreach (DataGridViewRow row in dgvVozovi.Rows)
            {
                var view = row.DataBoundItem as DataRowView;
                if (view != null && view.Row.RowState != DataRowState.Deleted)
                {
                    long id = Convert.ToInt64(view["ID"]);
                    if (resetting || !lastStarted.ContainsKey(id))
                        lastStarted[id] = Convert.ToBoolean(view["Started"]);
                }
            }
        }

        private void dgvVozovi_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            gridVozovi.Schedule();
        }

        private void dgvVozovi_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && gridVozovi.HasPending)
                gridVozovi.Schedule();
        }

        private void dgvVozovi_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (MessageBox.Show(this, "Obrisati označeni red?", "Potvrda", MessageBoxButtons.YesNo) != DialogResult.Yes)
                e.Cancel = true;
        }

        private void dgvVozovi_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            gridVozovi.Schedule();
        }

        private void dgvVozovi_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            DatabaseGrid.ReportDataError(dgvVozovi, e);
        }

        private void dgvVozovi_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (committingCheck || changingTrainStatus || !ready || gridVozovi == null || !gridVozovi.IsLoaded ||
                !dgvVozovi.IsCurrentCellDirty || !(dgvVozovi.CurrentCell is DataGridViewCheckBoxCell))
                return;
            committingCheck = true;
            try
            {
                // Commit both the checkbox cell and its bound row immediately.
                if (dgvVozovi.CommitEdit(DataGridViewDataErrorContexts.Commit))
                {
                    dgvVozovi.EndEdit();
                    dgvVozovi.BindingContext[dgvVozovi.DataSource].EndCurrentEdit();
                    dgvVozovi.Refresh();
                    QueueVozoviStatus();
                }
            }
            finally { committingCheck = false; }
        }

        private void dgvVozovi_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (changingTrainStatus || gridVozovi == null || !gridVozovi.IsLoaded || e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            var view = dgvVozovi.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (view == null) return;
            string column = dgvVozovi.Columns[e.ColumnIndex].DataPropertyName;
            if (column == "Dissolved")
            {
                changingTrainStatus = true;
                try
                {
                    RaspustanjeVoza.ProveriRed(view.Row);
                }
                catch (Exception ex)
                {
                    view["Dissolved"] = view.Row.HasVersion(DataRowVersion.Original) && Convert.ToBoolean(view.Row["Dissolved", DataRowVersion.Original]);
                    MessageBox.Show(this, ex.Message, "Raspustanje");
                }
                finally { changingTrainStatus = false; }
                QueueVozoviStatus();
                return;
            }
            if (column == "Rezi")
            {
                string composition = Db.Text(view.Row, "AnnouncementWagons");
                view["Note"] = TrainText.WithWagons(Db.Text(view.Row, "Note"), composition,
                    Convert.ToBoolean(view["Rezi"]) ? null : composition);
                gridVozovi.Schedule();
                return;
            }
            if (column == "Started" || column == "Completed")
            {
                changingTrainStatus = true;
                try
                {
                    if (column == "Started")
                    {
                        long id = Convert.ToInt64(view["ID"]);
                        bool current = Convert.ToBoolean(view["Started"]);
                        bool previous;
                        if (!lastStarted.TryGetValue(id, out previous))
                            previous = view.Row.HasVersion(DataRowVersion.Original) && Convert.ToBoolean(view.Row["Started", DataRowVersion.Original]);
                        if (previous && !current && MessageBox.Show(this,
                            "Da li ste sigurni da želite da poništite pokretanje voza " + view["Train"] +
                            "? Njegov zapis pokretanja biće obrisan.", "Poništavanje pokretanja",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                        {
                            view["Started"] = true;
                            return;
                        }
                        if (current != previous)
                        {
                            view["StartedAtLocal"] = current ? (object)DateTime.Now : DBNull.Value;
                            view["StartedAtUtc"] = current ? (object)DateTime.UtcNow : DBNull.Value;
                        }
                        lastStarted[id] = current;
                    }
                    else
                    {
                        bool completed = Convert.ToBoolean(view["Completed"]);
                        if (completed)
                        {
                            try { RadniNalogDopuna.ProveriNajavuZaZavrsetak(view.Row); }
                            catch (InvalidOperationException ex)
                            {
                                view["Completed"] = false;
                                view["CompletedAtLocal"] = DBNull.Value;
                                MessageBox.Show(this, ex.Message, "Najava je obavezna");
                                gridVozovi.Schedule();
                                return;
                            }
                        }
                        view["CompletedAtLocal"] = completed ?
                            (view.Row.IsNull("CompletedAtLocal") ? (object)DateTime.Now : view["CompletedAtLocal"]) : DBNull.Value;
                    }
                }
                finally { changingTrainStatus = false; }
                QueueVozoviStatus();
                return;
            }
            gridVozovi.Schedule();
        }

        private void dgvTrase_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            filterTrase.Header(sender, e);
        }

        private void dgvTrase_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            GridTitles.Apply(dgvTrase);
            filterTrase.Bound(sender, e);
        }

        private void dgvNajava_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            filterNajava.Header(sender, e);
        }

        private void dgvNajava_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            GridTitles.Apply(dgvNajava);
            filterNajava.Bound(sender, e);
        }








        private void chkAdhock_CheckedChanged(object sender, EventArgs e)
        {
            SetAdhoc();
        }

        private void txtOtpravna_TextChanged(object sender, EventArgs e)
        {
            if (!updating) ClearProposal();
        }

        private void txtUputna_TextChanged(object sender, EventArgs e)
        {
            if (!updating) ClearProposal();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            ClearProposal();
        }

        private void btnPredloziTrase_Click(object sender, EventArgs e)
        {
            // Slobodan unos dela naziva ili skraćenice (bg, ns, ru).
            // U rezultatima su stvarne stanice i dozvoljene trase iz Excel-a.
            try
            {
                if (AdhocPolje.Checked)
                    throw new InvalidOperationException("Za ad hoc koristite ručna polja i Dodaj Ad hoc.");
                catalog = OperationsRepository.LoadCatalog();
                if (catalog.Routes.Count == 0)
                    throw new InvalidOperationException("Planer prvo treba da uveze red vožnje.");
                proposedFrom = txtOtpravna.Text.Trim();
                proposedTo = txtUputna.Text.Trim();
                proposedWeight = numericUpDown1.Value;
                proposed = RouteRules.Suggest(catalog.Routes, proposedFrom, proposedTo, proposedWeight);
                FillDGVTrase();
                GridColumnFilters.Reset(dgvTrase);
                btnDodeliTrasu.Enabled = proposed.Count > 0;
                if (proposed.Count == 0)
                    MessageBox.Show(this, "Nema trase za zadatu stanicu/relaciju, smer i tezinu.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Dispečer");
            }
        }

        private void btnDodeliTrasu_Click(object sender, EventArgs e)
        {
            //izabranu trasu iz dgvTrase upisuje u dgvVozovi
            try
            {
                if (AdhocPolje.Checked)
                    throw new InvalidOperationException("Ad hoc se dodaje ručnim unosom.");
                var selected = Selected(dgvTrase);
                var route = proposed.Single(r => r.ID == Convert.ToInt64(selected["ID"]));
                if (proposedFrom != txtOtpravna.Text.Trim() || proposedTo != txtUputna.Text.Trim() || proposedWeight != numericUpDown1.Value)
                    throw new InvalidOperationException("Ponovo predložite trasu posle izmene unosa.");
                gridVozovi.Save();
                var id = OperationsRepository.AddTrain(route, route.From, route.To, route.Train, proposedWeight, false);
                FillDGVVozovi(id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Dispečer");
            }
        }

        private void btnAdhoc_Click(object sender, EventArgs e)
        {
            //u dgvVozovi upisuje adhoc trasu sa rucno unetim nazivom pocetne,krajnje  stanice, tezine i broja trase
            try
            {
                if (!AdhocPolje.Checked)
                    throw new InvalidOperationException("Prvo označite Ad hoc.");
                gridVozovi.Save();
                var id = OperationsRepository.AddTrain(null, txtOtpravnaAH.Text, txtUputnaAH.Text, txtTrasa.Text.Trim(), numericUpDown2.Value, true);
                // Ciscenje tek nakon uspesnog upisa; redovna tezina ostaje sacuvana.
                txtOtpravnaAH.Clear();
                txtUputnaAH.Clear();
                txtTrasa.Clear();
                numericUpDown2.Value = 0;
                AdhocPolje.Checked = false;
                SetAdhoc();
                FillDGVVozovi(id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Dispečer");
            }
        }

        private void btnObrisi_Click(object sender, EventArgs e)
        {
            //uklanja izabrani voz iz dgvVozovi
            try
            {
                var row = Selected(dgvVozovi);
                if (MessageBox.Show(this, "Obrisati voz " + row["Train"] + "?", "Potvrda", MessageBoxButtons.YesNo) != DialogResult.Yes)
                    return;
                dgvVozovi.Rows.Remove(dgvVozovi.CurrentRow);
                gridVozovi.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Dispečer");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Briše unete stanice i predloge trasa.
            try
            {
                updating = true;
                try
                {
                    txtOtpravna.Text = "";
                    txtUputna.Text = "";
                    txtOtpravnaAH.Clear();
                    txtUputnaAH.Clear();
                    txtTrasa.Clear();
                }
                finally
                {
                    updating = false;
                }

                ClearProposal();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Dispečer");
            }
        }

        private void btnPoveziNajavu_Click(object sender, EventArgs e)
        {
            //izabrani voz iz dgvVozovi povezuje sa najavom iz dgvNajava -> spisak najava koji se ucitava iz baze
            Run(() => LinkAnnouncement(false));
        }

        private void btnUkloniNajavu_Click(object sender, EventArgs e)
        {
            //izabrani voz iz dgvVozovi uklanja vezu sa najavom iz dgvNajava
            Run(() => LinkAnnouncement(true));
        }

        private void btnMail_Click(object sender, EventArgs e)
        {
            //otvara formu za slanje mail-a sa svim potrebnim podacima iz dgvVozovi
            try
            {
                gridVozovi.Save();
                var data = gridVozovi.Data;
                var all = OperationsRepository.Plans(data);
                var ids = new HashSet<long>(dgvVozovi.Rows.Cast<DataGridViewRow>().Where(r => r.DataBoundItem is DataRowView).Select(r => Convert.ToInt64(((DataRowView)r.DataBoundItem)["ID"])));
                // Dugme na ovoj formi određuje sadržaj: trenutno prikazani vozovi.
                var vozoviZaMail = all.Where(r => ids.Contains(r.ID)).ToList();
                var mail = DispatchEmail.Generate(vozoviZaMail);
                if (mail.Unknown.Count > 0)
                    MessageBox.Show(this, "Stanice bez prepoznatog geografskog položaja: " +
                        string.Join(", ", mail.Unknown) +
                        ". Njihovi vozovi su na kraju odgovarajućeg odeljka.", "Redosled vozova");

                using (MailPreview frm = new MailPreview(mail.Text))
                {
                    frm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Dispečer");
            }
        }

        private async void richTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            await MarkSelection(e);
        }

        private void richTextBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (IsShift(e))
                shiftDown = false;
        }

        private void richTextBox1_Leave(object sender, EventArgs e)
        {
            shiftDown = false;
        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {
            if (notePainting)
                return;
            noteDirty = true;
            noteTimer.Stop();
            noteTimer.Start();
        }

        private void noteTimer_Tick(object sender, EventArgs e)
        {
            noteTimer.Stop();
            Run(SaveShiftNote);
        }



        private async void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!ready || busy) return;
            busy = true;
            try { await LoadSelectedTab(true); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Učitavanje tabele"); }
            finally { busy = false; }
        }

        private async void DispMain_Shown(object sender, EventArgs e)
        {
            // Obaveštenja o novim telegramima dok je dispMain otvoren (posle poruke se ništa drugo ne radi)
            Saobracaj.Telegrami.TelegramNotifier.Instance.Register(this);
            await Start();
        }

        private async void refreshTimer_Tick(object sender, EventArgs e)
        {
            await RefreshShared();
        }

        private async void DispMain_Activated(object sender, EventArgs e)
        {
            if (ready)
                await RefreshShared();
        }

        private void DispMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (busy)
            {
                e.Cancel = true;
                return;
            }

            try
            {
                SaveShiftNote();
                SaveGrids();
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                MessageBox.Show(ex.Message);
            }
        }

        private void DispMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            refreshTimer.Stop();
            refreshTimer.Dispose();
            noteTimer.Stop();
            noteTimer.Dispose();
            Saobracaj.Telegrami.TelegramNotifier.Instance.Unregister(this);
            DisposeGrids();
        }








        private void SaveVozoviStatus()
        {
            try
            {
                if (IsDisposed || Disposing) return;
                dgvVozovi.EndEdit();
                dgvVozovi.BindingContext[dgvVozovi.DataSource].EndCurrentEdit();
                gridVozovi.Save(); // Successful save rebinds fresh values, without waiting for a focus change.
                dgvVozovi.Refresh();
            }
            catch (Exception ex)
            {
                OperationalDiagnostics.Write("Čuvanje statusa: " + ex);
                MessageBox.Show(this, ex.Message, "Status voza nije sačuvan");
            }
            finally { statusSaveQueued = false; }
        }

        private void QueueVozoviStatus()
        {
            if (statusSaveQueued || IsDisposed || Disposing || !IsHandleCreated) return;
            statusSaveQueued = true;
            BeginInvoke(new Action(SaveVozoviStatus));
        }

        private DateTime ReferentniDatumVoznje(int row, string column)
        {
            var view = dgvVozovi.Rows[row].DataBoundItem as DataRowView;
            return view == null || view.Row.IsNull(column) ? DateTime.Today : Convert.ToDateTime(view[column]).Date;
        }
        private void dgvVozovi_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string column = dgvVozovi.Columns[e.ColumnIndex].DataPropertyName;
            if (column != "CompletedAtLocal" && column != "StartedAtLocal" && column != "DissolvedAtLocal") return;
            try
            {
                var value = TrainText.ParseCompletionTime(Convert.ToString(e.FormattedValue), ReferentniDatumVoznje(e.RowIndex, column));
                var view = dgvVozovi.Rows[e.RowIndex].DataBoundItem as DataRowView;
                if (column == "StartedAtLocal" && view != null && !Convert.ToBoolean(view["Started"]) && value.HasValue)
                    throw new FormatException("Najpre oznacite da je voz pokrenut.");
                if (column == "StartedAtLocal" && view != null && Convert.ToBoolean(view["Started"]) && !value.HasValue)
                    throw new FormatException("Pokrenuti voz mora imati vreme polaska.");
                if (column == "DissolvedAtLocal" && view != null && Convert.ToBoolean(view["Dissolved"]) && Convert.ToBoolean(view["Started"]) && !value.HasValue)
                    throw new FormatException("Raspusteni voz mora imati vreme raspustanja.");
                dgvVozovi.Rows[e.RowIndex].ErrorText = "";
            }
            catch (FormatException)
            {
                e.Cancel = true;
                dgvVozovi.Rows[e.RowIndex].ErrorText = "Unesite datum i vreme ili samo vreme (14:30).";
                MessageBox.Show(this, "Unesite vreme kao 14:30 ili datum i vreme dd.MM.yyyy HH:mm:ss. Pokrenuti voz mora imati vreme polaska; za izmenu najpre oznacite da je pokrenut.", "Vreme voznje");
            }
        }
        private void dgvVozovi_CellParsing(object sender, DataGridViewCellParsingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string column = dgvVozovi.Columns[e.ColumnIndex].DataPropertyName;
            if (column != "CompletedAtLocal" && column != "StartedAtLocal" && column != "DissolvedAtLocal") return;
            if (e.Value is DateTime) { e.ParsingApplied = true; return; }
            try
            {
                e.Value = (object)TrainText.ParseCompletionTime(Convert.ToString(e.Value), ReferentniDatumVoznje(e.RowIndex, column)) ?? DBNull.Value;
                e.ParsingApplied = true;
            }
            catch (FormatException) { e.ParsingApplied = false; }
        }

        private async void btnEvidentiraj_Click(object sender, EventArgs e)
        {
            if (!ready || busy) return;
            busy = true;
            bool enabled = tabControl1.Enabled;
            tabControl1.Enabled = false;
            try
            {
                gridVozovi.Save();
                var originalSnapshot = gridVozovi.Data.Copy();
                var snapshot = await Task.Run(() => RaspustanjeVoza.Pripremi(originalSnapshot));
                var plans = await Task.Run(() => SifarnikTrasaServis.Ucitaj(snapshot));
                if (IsDisposed || Disposing) return;
                if (plans.Count == 0)
                {
                    // A second click can repair zeroes left by the earlier code without duplicating transport history.
                    int repaired = await Task.Run(() => SifarnikTrasaServis.DopuniVecEvidentirane());
                    if (!IsDisposed && !Disposing)
                        MessageBox.Show(this, repaired == 0 ?
                            "Nema novih završenih prevoza niti povezanih nultih vrednosti koje je moguće dopuniti." :
                            "Dopunjene su nulte vrednosti za već evidentirane trase: " + repaired +
                            ". Nisu kreirani novi prevozi ni zapisi pokretanja.");
                    return;
                }
                if (MessageBox.Show(this, "Evidentirati " + plans.Count +
                    " deonica zavrsenih voznji i proveriti godisnji sifarnik trasa? Uključeni su i označeni redovi skriveni filterom.",
                    "Završeni prevozi", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                foreach (var p in plans)
                {
                    if ((!p.Od.HasValue || !p.Do.HasValue) && p.Dokument != null &&
                        p.Dokument.Od.HasValue && p.Dokument.Do.HasValue)
                    {
                        p.Od = p.Dokument.Od; p.Do = p.Dokument.Do;
                    }
                    if (!p.Od.HasValue || !p.Do.HasValue)
                    {
                        using (var period = new PeriodCenovnika(null, null,
                            p.Dokument == null ? "Period cenovnika za ad hoc voz " + p.Broj :
                            "Ranije uvezen cenovnik nema period: " + p.Dokument.Naziv))
                        {
                            if (period.ShowDialog(this) != DialogResult.OK) return;
                            p.Od = period.DatumOd; p.Do = period.DatumDo;
                            if (p.Dokument != null) { p.Dokument.Od = p.Od; p.Dokument.Do = p.Do; }
                        }
                    }
                    p.Godina = CenovnikPravila.Godina(p.Od.Value, p.Do.Value);
                }
                var stations = await Task.Run(() => SifarnikTrasaServis.PripremiStanice(plans));
                if (IsDisposed || Disposing) return;
                var selected = new Dictionary<string, int>();
                foreach (var p in plans.Where(p => p.PotrebneStanice))
                {
                    int? from = IzaberiStanicuCenovnika(stations, p.Excel.From, p.Broj, "Otpravna", selected);
                    if (!from.HasValue) return;
                    int? to = IzaberiStanicuCenovnika(stations, p.Excel.To, p.Broj, "Uputna", selected);
                    if (!to.HasValue) return;
                    p.StanicaOd = from.Value; p.StanicaDo = to.Value;
                }
                var rnStations = await Task.Run(() => RadniNalogDopuna.Stanice());
                if (IsDisposed || Disposing) return;
                var rnMesta = await Task.Run(() => RadniNalogDopuna.Mesta());
                if (IsDisposed || Disposing) return;
                foreach (var p in plans)
                {
                    var voz = snapshot.Rows.Cast<DataRow>().Single(r => Convert.ToInt64(r["ID"]) == p.ID);
                    int? rnFrom = IzaberiStanicuCenovnika(rnStations, Db.Text(voz, "From"), p.BrojVoznje, "Otpravna realizacije", selected);
                    if (!rnFrom.HasValue) return;
                    int? rnTo = IzaberiStanicuCenovnika(rnStations, Db.Text(voz, "To"), p.BrojVoznje, "Uputna realizacije", selected);
                    if (!rnTo.HasValue) return;
                    p.RNStanicaOd = rnFrom.Value; p.RNStanicaDo = rnTo.Value;
                    p.RNStavke = await Task.Run(() => RadniNalogDopuna.PripremiAutomatski(voz));
                    if (IsDisposed || Disposing) return;
                    if (p.RNStavke == null)
                    {
                        using (var dialog = new DopunaRadnogNaloga(p, voz))
                            if (dialog.ShowDialog(this) != DialogResult.OK) return;
                    }
                    p.RNLokomotive = new List<string>();
                    foreach (var number in RNAutomatskiIzbor.BrojeviLokomotiva(Db.Text(voz, "LocomotiveNumber")))
                    {
                        var loks = RNAutomatskiIzbor.Lokomotive(rnMesta, number);
                        if (loks.Count == 0) throw new InvalidOperationException("Lokomotiva " + number + " nije pronadjena u Mesta. Ispravite broj u spisku vozova ili dopunite sifarnik.");
                        string lok;
                        if (loks.Count == 1) lok = loks[0];
                        else
                        {
                            var choices = rnMesta.Clone();
                            foreach (DataRow row in rnMesta.Rows) if (loks.Contains(Convert.ToString(row["SmSifra"]))) choices.ImportRow(row);
                            using (var pick = new IzborStaniceCenovnika("Izbor lokomotive", "Broj " + number + " nije jednoznacan. Izaberite sifru.", choices))
                            {
                                if (pick.ShowDialog(this) != DialogResult.OK) return;
                                lok = Convert.ToString(pick.IzabraniRed["SmSifra"]);
                            }
                        }
                        if (!p.RNLokomotive.Contains(lok)) p.RNLokomotive.Add(lok);
                    }
                    p.RNVucnaLokomotiva = p.RNLokomotive.FirstOrDefault();
                    if (p.RNLokomotive.Count > 1)
                    {
                        var choices = rnMesta.Clone();
                        foreach (DataRow row in rnMesta.Rows) if (p.RNLokomotive.Contains(Convert.ToString(row["SmSifra"]))) choices.ImportRow(row);
                        using (var pick = new IzborStaniceCenovnika("Vucna lokomotiva", "Izaberite vucnu lokomotivu za ovu deonicu. Ostale se upisuju kao nevucne.", choices))
                        {
                            if (pick.ShowDialog(this) != DialogResult.OK) return;
                            p.RNVucnaLokomotiva = Convert.ToString(pick.IzabraniRed["SmSifra"]);
                        }
                    }
                }
                int count = await Task.Run(() => OperationsRepository.RecordCompleted(plans));
                if (IsDisposed || Disposing) return;
                try { FillDGVVozovi(); }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Prevozi su evidentirani (" + count + "), ali prikaz nije osvežen. Ponovo otvorite formu.\n" + ex.Message);
                    return;
                }
                MessageBox.Show(this, "Evidentirano deonica: " + count);
            }
            catch (Exception ex)
            {
                if (!IsDisposed && !Disposing) MessageBox.Show(this, ex.Message, "Evidentiranje nije završeno");
            }
            finally
            {
                busy = false;
                if (!IsDisposed && !Disposing) tabControl1.Enabled = enabled;
            }
        }

        private int? IzaberiStanicuCenovnika(DataTable data, string name, string train, string direction, Dictionary<string, int> cache)
        {
            string key = StaniceCenovnika.NormalizujStanicu(name);
            if (key.Length == 0) throw new InvalidOperationException("Nije prepoznatljiv naziv stanice za trasu / realizaciju " + train);
            int cached;
            if (cache.TryGetValue(key, out cached)) return cached;
            var raw = data.Rows.Cast<DataRow>().Where(r => string.Equals(Convert.ToString(r["Opis"]).Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            string osnovna = raw.Count > 0 ? name.Trim() : RNAutomatskiIzbor.OsnovnaStanica(name);
            var candidates = RNAutomatskiIzbor.Predlozi(data, osnovna);
            var exact = candidates.Where(c => c.Ocena == 1).ToList();
            if (raw.Count == 0 && candidates.Count == 0)
                throw new InvalidOperationException("Nema stanica sa odgovarajucim nazivom: " + osnovna + ". Ispravite naziv stanice u spisku vozova.");
            int id;
            if (raw.Count == 1) id = Convert.ToInt32(raw[0]["ID"]);
            else if (raw.Count == 0 && exact.Count == 1) id = exact[0].ID;
            else
            {
                var table = new DataTable();
                table.Columns.Add("ID", typeof(int)); table.Columns.Add("Opis", typeof(string)); table.Columns.Add("Podudaranje", typeof(int));
                foreach (var candidate in candidates) table.Rows.Add(candidate.ID, candidate.Opis, (int)Math.Round(candidate.Ocena * 100));
                using (var dialog = new IzborStaniceCenovnika(direction + " stanica za trasu / realizaciju " + train,
                    "Uneti naziv: " + name + ". Potvrdite stanicu iz šifarnika.", table))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK) return null;
                    id = Convert.ToInt32(dialog.IzabraniRed["ID"]);
                }
            }
            cache[key] = id;
            return id;
        }

        private void txtOtpravna_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && btnPredloziTrase.Enabled)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                btnPredloziTrase_Click(sender, EventArgs.Empty);
            }
        }

        private void txtUputna_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && btnPredloziTrase.Enabled)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                btnPredloziTrase_Click(sender, EventArgs.Empty);
            }
        }
    }
}
