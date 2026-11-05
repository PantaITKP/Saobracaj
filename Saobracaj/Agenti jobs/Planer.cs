using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Saobracaj.Agenti_jobs
{
    public partial class Planer : Form
    {
        private DatabaseGrid gridMreza;
        private DatabaseGrid gridNajave;
        private DatabaseGrid gridLokomotive;
        private GridColumnFilters filterMreza;
        private GridColumnFilters filterNajave;
        private GridColumnFilters filterLokomotive;
        private readonly Timer refreshTimer = new Timer
        {
            Interval = 5000
        };
        private readonly Saobracaj.Telegrami.TelegramiTab telegramiTab;
        private PlanSnapshot editorPlan = new PlanSnapshot();
        private bool ready, dirty, busy, painting;
        private readonly int requestedPlan;
        public Planer() : this(-1, null)
        {
        }

        // -1 = latest plan; 0 = a new plan; positive = an existing plan ID.
        public Planer(int planId, string user = null)
        {
            InitializeComponent();
            if (!string.IsNullOrWhiteSpace(Saobracaj.Sifarnici.frmLogovanje.user))
                OperationalSession.UserName = Saobracaj.Sifarnici.frmLogovanje.user.Trim();
            filterMreza = new GridColumnFilters(dgvMreza);
            filterNajave = new GridColumnFilters(dgvNajave);
            filterLokomotive = new GridColumnFilters(dgvLokomotive);
            requestedPlan = planId;
            if (!string.IsNullOrWhiteSpace(user))
                OperationalSession.UserName = user;
            txtDisp.ReadOnly = true;
            txtDisp.HideSelection = false;
            txtPlaner.HideSelection = false;
            gridMreza = new DatabaseGrid(dgvMreza, "OperativniPlanMreza");
            gridNajave = new DatabaseGrid(dgvNajave, "OperativniPlanNajave");
            gridLokomotive = new DatabaseGrid(dgvLokomotive, "OperativniPlanLokomotive");
            sacuvajPlanToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            refreshTimer.Tick += refreshTimer_Tick;
            telegramiTab = new Saobracaj.Telegrami.TelegramiTab(tabTelegrami, dataGridView1);
        }

        private async Task Start()
        {
            try
            {
                editorPlan = await Task.Run(() => PlanRepository.Load(requestedPlan));
                if (IsDisposed || Disposing) return;
                painting = true;
                txtPlaner.Text = editorPlan.Text;
                painting = false;
                dirty = false;
                PlanTextView.Paint(txtDisp, editorPlan);
                FillDGV();
                ready = true;
                refreshTimer.Start();
                Text = "Planer — " + (editorPlan.Name == "" ? "novi plan" : editorPlan.Name);
            }
            catch (Exception ex)
            {
                painting = false;
                MessageBox.Show(this, ex.Message + "\nPokrenite SQL/001_OperativniPlan.sql u bazi aplikacije.", "Učitavanje planera");
            }
        }

        private async Task RefreshPlan()
        {
            if (!ready || busy || IsDisposed)
                return;
            busy = true;
            try
            {
                var snapshot = await Task.Run(() => PlanRepository.Load(requestedPlan == 0 && editorPlan.PlanID > 0 ? editorPlan.PlanID : requestedPlan));
                if (IsDisposed)
                    return;
                PlanTextView.Paint(txtDisp, snapshot);
                if (!dirty)
                {
                    painting = true;
                    txtPlaner.Text = snapshot.Text;
                    painting = false;
                    editorPlan = snapshot;
                }
            }
            catch (Exception ex)
            {
                Text = "Planer — osvežavanje nije uspelo: " + ex.Message;
            }
            finally
            {
                painting = false;
                busy = false;
            }
        }

        private void SaveGrids()
        {
            gridMreza.Save();
            gridNajave.Save();
            gridLokomotive.Save();
        }

        private void DisposeGrids()
        {
            gridMreza.Dispose();
            filterMreza.Dispose();
            gridNajave.Dispose();
            filterNajave.Dispose();
            gridLokomotive.Dispose();
            filterLokomotive.Dispose();
        }

        private void FillDGV()
        {
            FillDGVMreza();
            FillDGVNajave();
            FillDGVLokomotive();
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
            if (MessageBox.Show(this, "Obrisati označeni red?", "Potvrda", MessageBoxButtons.YesNo) != DialogResult.Yes)
                e.Cancel = true;
        }

        private void dgvMreza_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            gridMreza.Schedule();
        }

        private void dgvMreza_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;
            MessageBox.Show(this, "Neispravna vrednost: " + e.Exception.Message);
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
            e.ThrowException = false;
            MessageBox.Show(this, "Neispravna vrednost: " + e.Exception.Message);
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
            e.ThrowException = false;
            MessageBox.Show(this, "Neispravna vrednost: " + e.Exception.Message);
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








        private void txtPlaner_TextChanged(object sender, EventArgs e)
        {
            if (!painting)
                dirty = true;
        }

        private async void sacuvajPlanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //cuva i azurira novi plan koji je unet u txtPlaner
            if (busy || !ready)
                return;
            busy = true;
            sacuvajPlanToolStripMenuItem.Enabled = false;
            try
            {
                SaveGrids();
                var text = PlanRepository.Normalize(txtPlaner.Text);
                int plan = editorPlan.PlanID, version = editorPlan.VersionID;
                var saved = await Task.Run(() => PlanRepository.Save(plan, version, text));
                if (IsDisposed)
                    return;
                editorPlan = saved;
                dirty = PlanRepository.Normalize(txtPlaner.Text) != text;
                PlanTextView.Paint(txtDisp, saved);
                Text = "Planer — sačuvano " + DateTime.Now.ToString("HH:mm:ss") + " (verzija " + saved.VersionNumber + ")";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Čuvanje plana");
            }
            finally
            {
                busy = false;
                if (!IsDisposed)
                    sacuvajPlanToolStripMenuItem.Enabled = true;
            }
        }

        private async void ucitajExcelTraseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //ucitava excel fajl sa spisom trasa
            if (busy)
                return;
            using (var dialog = new OpenFileDialog
            {
                Filter = "Excel red vožnje (*.xlsx)|*.xlsx",
                Title = "Izaberite godišnji red vožnje"
            }

            )
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                DateTime validFrom, validTo;
                using (var period = new PeriodCenovnika(null, null, dialog.SafeFileName))
                {
                    if (period.ShowDialog(this) != DialogResult.OK) return;
                    validFrom = period.DatumOd;
                    validTo = period.DatumDo;
                }
                busy = true;
                ucitajExcelTraseToolStripMenuItem.Enabled = false;
                try
                {
                    var file = dialog.FileName;
                    int count = await Task.Run(() => OperationsRepository.ImportCatalog(file, validFrom, validTo));
                    MessageBox.Show(this, "Uvezeno trasa: " + count + ". Godina važenja: " + validTo.Year + ". Isti cenovnik dostupan je i na DispMain.", "Uvoz reda vožnje");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Uvoz reda vožnje");
                }
                finally
                {
                    busy = false;
                    ucitajExcelTraseToolStripMenuItem.Enabled = true;
                }
            }
        }

        private async void Planer_Shown(object sender, EventArgs e)
        {
            // Obaveštenja o novim telegramima dok je Planer otvoren
            Saobracaj.Telegrami.TelegramNotifier.Instance.Register(this, telegramiTab);
            var telegrami = telegramiTab.Ucitaj();
            await Start();
            await telegrami;
        }

        private async void refreshTimer_Tick(object sender, EventArgs e)
        {
            await RefreshPlan();
        }

        private void Planer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (busy)
            {
                e.Cancel = true;
                return;
            }

            if (dirty && MessageBox.Show(this, "Plan sadrži nesačuvane izmene. Zatvoriti bez čuvanja plana?", "Plan", MessageBoxButtons.YesNo) != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            try
            {
                SaveGrids();
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                MessageBox.Show(ex.Message);
            }
        }

        private void Planer_FormClosed(object sender, FormClosedEventArgs e)
        {
            refreshTimer.Stop();
            refreshTimer.Dispose();
            Saobracaj.Telegrami.TelegramNotifier.Instance.Unregister(this);
            telegramiTab.Dispose();
            DisposeGrids();
        }
    }
}
