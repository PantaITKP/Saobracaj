using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Agenti_jobs;

namespace Saobracaj.Telegrami
{
    /// <summary>
    /// Tab "Telegrami" na formi Planer: aktivni telegrami (Aktivan = 1 i VaziDo &gt;= sada).
    /// Kontrole iznad grida se dodaju u kodu, da se Planer.Designer.cs ne bi menjao.
    /// </summary>
    internal sealed class TelegramiTab : IDisposable
    {
        private readonly TabPage tab;
        private readonly DataGridView grid;
        private readonly TextBox txtPretraga = new TextBox { Width = 220 };
        private readonly ComboBox cboPeriod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
        private readonly Button btnOsvezi = new Button { Text = "Osveži", AutoSize = true };
        private readonly Button btnOtvori = new Button { Text = "Otvori telegram", AutoSize = true, Enabled = false };
        private readonly Button btnPregled = new Button { Text = "Na pregled", AutoSize = true, Visible = false };
        private readonly Label lblStatus = new Label { AutoSize = true, ForeColor = Color.DimGray, Padding = new Padding(8, 6, 0, 0) };
        private readonly Timer osvezavanje = new Timer { Interval = 5 * 60 * 1000 };
        private readonly ToolTip tip = new ToolTip();
        private DataTable podaci;
        private bool ucitava;
        private Font podebljan;

        private static readonly Color BojaSada = Color.FromArgb(255, 205, 205);
        private static readonly Color BojaDanas = Color.FromArgb(255, 236, 214);
        private static readonly Color BojaUskoro = Color.FromArgb(232, 240, 255);

        public TelegramiTab(TabPage tab, DataGridView grid)
        {
            this.tab = tab;
            this.grid = grid;

            var traka = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(4, 4, 4, 0), WrapContents = false };
            traka.Controls.Add(new Label { Text = "Pretraga:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            traka.Controls.Add(txtPretraga);
            cboPeriod.Items.AddRange(new object[] { "Danas", "Narednih 7 dana", "Svi aktivni" });
            cboPeriod.SelectedIndex = 2;
            traka.Controls.Add(cboPeriod);
            traka.Controls.Add(btnOsvezi);
            traka.Controls.Add(btnOtvori);
            traka.Controls.Add(btnPregled);
            traka.Controls.Add(lblStatus);
            btnPregled.Click += (s, e) =>
            {
                // poruke koje agent nije mogao sam da upiše (nepoznata pruga/stanica ...)
                var f = new Saobracaj.Sifarnici.frmTelegrami();
                if (!f.Pravo) { f.Dispose(); return; }
                f.Show();
                f.PrikaziPregled();
            };
            tab.Controls.Add(traka);
            traka.SendToBack(); // grid je Dock=Fill, traka iznad

            var legenda = new TelegramiLegenda()
                .Pozadina(BojaSada, "Zatvor je u toku (upravo sada)", true)
                .Pozadina(BojaDanas, "Važi danas, ali ne u ovom trenutku (dnevni interval)")
                .Pozadina(BojaUskoro, "Počinje u naredna 3 dana")
                .Pozadina(SystemColors.Window, "Počinje kasnije")
                .Slova(Color.DarkRed, "Dugme „Na pregled“: telegrami koje agent nije mogao sam da upiše");
            tab.Controls.Add(legenda);
            legenda.SendToBack();

            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = true;
            grid.RowHeadersVisible = false;
            grid.AutoGenerateColumns = false;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.Columns.Clear();
            Kolona("BrojTelegrama", "Broj", 55);
            Kolona("DatumTelegrama", "Datum telegrama", 85, "dd.MM.yyyy");
            Kolona("Pruga", "Pruga", 220);
            Kolona("OdStanice", "Od", 120);
            Kolona("DoStanice", "Do", 120);
            Kolona("Kolosek", "Kolosek", 80);
            Kolona("VaziOd", "Važi od", 115, "dd.MM.yyyy HH:mm");
            Kolona("VaziDo", "Važi do", 115, "dd.MM.yyyy HH:mm");
            Kolona("Dnevno", "Dnevno", 85);
            var nap = Kolona("Napomena", "Napomena", 400);
            nap.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            nap.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

            grid.CellFormatting += Grid_CellFormatting;
            grid.CellDoubleClick += Grid_CellDoubleClick;
            grid.SelectionChanged += (s, e) => btnOtvori.Enabled = MozeDaOtvori(Trenutni());
            txtPretraga.TextChanged += (s, e) => PrimeniFilter();
            cboPeriod.SelectedIndexChanged += (s, e) => PrimeniFilter();
            btnOsvezi.Click += async (s, e) => await Ucitaj();
            btnOtvori.Click += (s, e) => OtvoriTelegram(Trenutni());
            osvezavanje.Tick += async (s, e) => await Ucitaj();
            osvezavanje.Start();
        }

        private DataGridViewTextBoxColumn Kolona(string polje, string naslov, int sirina, string format = null)
        {
            var c = new DataGridViewTextBoxColumn { DataPropertyName = polje, Name = polje, HeaderText = naslov, Width = sirina, SortMode = DataGridViewColumnSortMode.Automatic };
            if (format != null) c.DefaultCellStyle.Format = format;
            grid.Columns.Add(c);
            return c;
        }

        public void Status(string tekst)
        {
            if (lblStatus.IsDisposed) return;
            lblStatus.Text = tekst;
        }

        /// <summary>Učitavanje u pozadini; čuva selekciju i poziciju skrola.</summary>
        public async Task Ucitaj(ICollection<int> selektujID = null)
        {
            if (ucitava || grid.IsDisposed) return;
            ucitava = true;
            try
            {
                var izabrani = selektujID ?? grid.SelectedRows.Cast<DataGridViewRow>().Select(RedID).Where(x => x > 0).ToList();
                int skrol = grid.FirstDisplayedScrollingRowIndex;
                var t = await Task.Run(() => TelegramiRepository.Aktivni());
                if (grid.IsDisposed) return;
                t.Columns.Add("Dnevno", typeof(string));
                foreach (DataRow r in t.Rows)
                    if (!r.IsNull("VaziOd") && !r.IsNull("VaziDo"))
                        r["Dnevno"] = TelegramRed.Dnevno(Convert.ToDateTime(r["VaziOd"]), Convert.ToDateTime(r["VaziDo"]));
                podaci = t;
                grid.DataSource = new DataView(podaci);
                PrimeniFilter();

                grid.ClearSelection();
                DataGridViewRow prvi = null;
                foreach (DataGridViewRow row in grid.Rows)
                    if (izabrani.Contains(RedID(row))) { row.Selected = true; if (prvi == null) prvi = row; }
                if (selektujID != null && prvi != null) grid.FirstDisplayedScrollingRowIndex = prvi.Index;
                else if (skrol >= 0 && skrol < grid.RowCount) grid.FirstDisplayedScrollingRowIndex = skrol;
                Status("Aktivnih: " + grid.RowCount + " · osveženo " + DateTime.Now.ToString("HH:mm"));
                var naPregledu = await Task.Run(() => TelegramiPregledRepository.BrojNaPregledu());
                if (!btnPregled.IsDisposed)
                {
                    btnPregled.Text = "Na pregled (" + naPregledu + ")";
                    btnPregled.Visible = naPregledu > 0;
                    btnPregled.ForeColor = naPregledu > 0 ? Color.DarkRed : SystemColors.ControlText;
                }
            }
            catch (Exception ex)
            {
                Status("Učitavanje nije uspelo: " + ex.Message);
                OperationalDiagnostics.Write("Telegrami tab: " + ex);
            }
            finally
            {
                ucitava = false;
            }
        }

        private void PrimeniFilter()
        {
            if (!(grid.DataSource is DataView dv)) return;
            var uslovi = new List<string>();
            var danas = DateTime.Today;
            if (cboPeriod.SelectedIndex == 0)
                uslovi.Add(string.Format("VaziOd < #{0:MM/dd/yyyy}# AND VaziDo >= #{1:MM/dd/yyyy}#", danas.AddDays(1), danas));
            else if (cboPeriod.SelectedIndex == 1)
                uslovi.Add(string.Format("VaziOd < #{0:MM/dd/yyyy}#", danas.AddDays(8)));
            var q = txtPretraga.Text.Trim().Replace("'", "''").Replace("[", "[[]").Replace("*", "[*]").Replace("%", "[%]");
            if (q.Length > 0)
                uslovi.Add(string.Format("(Pruga LIKE '%{0}%' OR OdStanice LIKE '%{0}%' OR DoStanice LIKE '%{0}%' OR Napomena LIKE '%{0}%' OR Convert(BrojTelegrama, 'System.String') LIKE '%{0}%')", q));
            try { dv.RowFilter = string.Join(" AND ", uslovi); }
            catch (Exception ex) { Status("Filter: " + ex.Message); }
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var drv = grid.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (drv == null || drv.Row.IsNull("VaziOd") || drv.Row.IsNull("VaziDo")) return;
            var od = Convert.ToDateTime(drv["VaziOd"]);
            var @do = Convert.ToDateTime(drv["VaziDo"]);
            var sada = DateTime.Now;
            if (TelegramRed.VaziU(od, @do, sada))
            {
                e.CellStyle.BackColor = BojaSada;
                e.CellStyle.Font = podebljan ?? (podebljan = new Font(grid.Font, FontStyle.Bold));
            }
            else if (od.Date <= sada.Date && @do >= sada.Date) e.CellStyle.BackColor = BojaDanas;
            else if (od.Date > sada.Date && od.Date <= sada.Date.AddDays(3)) e.CellStyle.BackColor = BojaUskoro;

            if (grid.Columns[e.ColumnIndex].Name == "Napomena")
                grid.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = Convert.ToString(drv["Napomena"]);
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var drv = grid.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (drv == null) return;
            if (grid.Columns[e.ColumnIndex].Name == "Napomena")
            {
                MessageBox.Show(grid.FindForm(), Convert.ToString(drv["Napomena"]), "Telegram br. " + drv["BrojTelegrama"], MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            OtvoriTelegram(drv);
        }

        private DataRowView Trenutni()
        {
            return grid.CurrentRow?.DataBoundItem as DataRowView;
        }

        private static bool MozeDaOtvori(DataRowView r)
        {
            if (r == null) return false;
            if (!r.Row.IsNull("MailLogID")) return true;
            var pdf = Convert.ToString(r["PDF"]);
            return !string.IsNullOrWhiteSpace(pdf);
        }

        /// <summary>
        /// Originalni PDF iz TelegramiPrilozi (agentov unos) se snima u %TEMP% i otvara podrazumevanim programom.
        /// Za ručno unete redove otvara se putanja iz kolone PDF (\\192.168.1.6\Telegrami\...), ako postoji.
        /// </summary>
        private async void OtvoriTelegram(DataRowView r)
        {
            if (r == null) return;
            try
            {
                if (!r.Row.IsNull("MailLogID"))
                {
                    var id = Convert.ToInt32(r["MailLogID"]);
                    var pdf = await Task.Run(() => TelegramiRepository.Pdf(id));
                    if (pdf == null) { Status("Za ovaj telegram nema sačuvanog priloga."); return; }
                    var ime = string.Concat(pdf.Item1.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
                    var putanja = Path.Combine(Path.GetTempPath(), "Telegram_" + id + "_" + ime);
                    File.WriteAllBytes(putanja, pdf.Item2);
                    Process.Start(new ProcessStartInfo(putanja) { UseShellExecute = true });
                    return;
                }
                var pdfPut = Convert.ToString(r["PDF"]).Trim();
                if (pdfPut.Length > 0 && (Directory.Exists(pdfPut) || File.Exists(pdfPut)))
                    Process.Start(new ProcessStartInfo(pdfPut) { UseShellExecute = true });
                else
                    Status("PDF nije dostupan: " + pdfPut);
            }
            catch (Exception ex)
            {
                Status("Otvaranje telegrama nije uspelo: " + ex.Message);
            }
        }

        private static int RedID(DataGridViewRow row)
        {
            var drv = row.DataBoundItem as DataRowView;
            return drv == null || drv.Row.IsNull("ID") ? -1 : Convert.ToInt32(drv["ID"]);
        }

        public void Dispose()
        {
            osvezavanje.Stop();
            osvezavanje.Dispose();
            tip.Dispose();
            podebljan?.Dispose();
        }
    }
}
