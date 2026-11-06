using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Agenti_jobs;

namespace Saobracaj.Telegrami
{
    /// <summary>
    /// Tab "Na pregled" u frmTelegrami: poruke koje je agent označio sa NeedsReview (nepoznata pruga/stanica,
    /// skenirani PDF, poruka bez PDF-a ...). Korisnik dopuni prugu i stanice u gridu i potvrdi → upis u Telegrami.
    /// Kontrole se prave u kodu, da se frmTelegrami.Designer.cs ne menja.
    /// </summary>
    internal sealed class TelegramiPregledTab : IDisposable
    {
        private readonly TabPage tab;
        private readonly bool smeUpis;
        private readonly string korisnik;

        private readonly ComboBox cboStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
        private readonly DateTimePicker dtOd = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 100 };
        private readonly DateTimePicker dtDo = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 100 };
        private readonly Button btnOsvezi = new Button { Text = "Osveži", AutoSize = true };
        private readonly Button btnPdf = new Button { Text = "Otvori PDF", AutoSize = true, Enabled = false };
        private readonly Button btnArhiviraj = new Button { Text = "Arhiviraj poruku", AutoSize = true, Enabled = false };
        private readonly Button btnDodaj = new Button { Text = "Dodaj stavku", AutoSize = true, Enabled = false };
        private readonly Button btnOdbaci = new Button { Text = "Odbaci stavku", AutoSize = true, Enabled = false };
        private readonly Button btnPotvrdi = new Button { Text = "Potvrdi izabrane", AutoSize = true, Enabled = false, Font = new Font(Control.DefaultFont, FontStyle.Bold) };
        private readonly Button btnPotvrdiSve = new Button { Text = "Potvrdi sve", AutoSize = true, Enabled = false };
        private readonly CheckBox chkZapamti = new CheckBox { Text = "Zapamti nazive", AutoSize = true, Checked = true, Padding = new Padding(6, 4, 0, 0) };
        private readonly Label lblStatus = new Label { AutoSize = true, ForeColor = Color.DimGray, Padding = new Padding(8, 6, 0, 0) };
        private readonly DataGridView dgvPoruke = new DataGridView();
        private readonly DataGridView dgvStavke = new DataGridView();
        private readonly RichTextBox txtTekst = new RichTextBox { ReadOnly = true, Dock = DockStyle.Fill, BackColor = SystemColors.Window, Font = new Font("Segoe UI", 9f) };
        private readonly ToolTip tip = new ToolTip();

        private DataTable poruke, stavke, pruge, stanice;
        private bool ucitava;
        private static readonly Color BojaNedostaje = Color.FromArgb(255, 210, 210);
        private static readonly CultureInfo Sr = CultureInfo.GetCultureInfo("sr-Latn-RS");

        public event EventHandler BrojPromenjen;

        public TelegramiPregledTab(TabPage tab, bool smeUpis, string korisnik)
        {
            this.tab = tab;
            this.smeUpis = smeUpis;
            this.korisnik = korisnik;

            // --- traka sa filterima i akcijama
            var traka = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(4), WrapContents = true };
            cboStatus.Items.AddRange(new object[] { "NeedsReview", "Greska", "Arhivirano", "Potvrdjeno", "Svi" });
            cboStatus.SelectedIndex = 0;
            dtOd.Value = DateTime.Today.AddDays(-30);
            dtDo.Value = DateTime.Today;
            traka.Controls.AddRange(new Control[]
            {
                new Label { Text = "Status:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, cboStatus,
                new Label { Text = "Primljeno od:", AutoSize = true, Padding = new Padding(6, 6, 0, 0) }, dtOd,
                new Label { Text = "do:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, dtDo,
                btnOsvezi, btnPdf, btnArhiviraj, new Label { Width = 12 }, btnDodaj, btnOdbaci, btnPotvrdi, btnPotvrdiSve, chkZapamti, lblStatus
            });
            tip.SetToolTip(chkZapamti, "Naziv iz telegrama i izabrana pruga/stanica se pamte (TelegramiAlias), pa ih agent sledeći put prepoznaje sam.");
            tip.SetToolTip(btnArhiviraj, "Poruka nije telegram za upis (reklama, prijava stanice, najava...). Preostale stavke se odbacuju.");

            // --- poruke (levo), tekst (desno gore), stavke (desno dole)
            Pripremi(dgvPoruke, true);
            KolonaTekst(dgvPoruke, "Primljeno", "Primljeno", 105, "dd.MM.yyyy HH:mm");
            KolonaTekst(dgvPoruke, "OznakaTelegrama", "Telegram", 65);
            KolonaTekst(dgvPoruke, "DatumTelegrama", "Datum", 75, "dd.MM.yyyy");
            KolonaTekst(dgvPoruke, "Naslov", "Naslov", 220);
            KolonaTekst(dgvPoruke, "Posiljalac", "Pošiljalac", 140);
            KolonaTekst(dgvPoruke, "Status", "Status", 80);
            var razlog = KolonaTekst(dgvPoruke, "Poruka", "Razlog", 300);
            razlog.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            Pripremi(dgvStavke, false);

            var desno = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            desno.Panel1.Controls.Add(txtTekst);
            desno.Panel2.Controls.Add(dgvStavke);
            var glavni = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
            glavni.Panel1.Controls.Add(dgvPoruke);
            glavni.Panel2.Controls.Add(desno);
            tab.Controls.Add(glavni);
            tab.Controls.Add(traka);
            tab.Controls.Add(new TelegramiLegenda()
                .Pozadina(BojaNedostaje, "Nedostaje podatak (pruga, stanica, broj, datum ili vreme) — izaberite ga pre potvrde")
                .Slova(Color.Gray, "Stavka je već obrađena (upisana, potvrđena, odbačena ili duplikat)"));
            glavni.BringToFront(); // Fill se raspoređuje poslednji: traka gore, legenda dole
            // SplitterDistance tek kada kontrole dobiju stvarnu veličinu (inače InvalidOperationException)
            bool podeseno = false;
            glavni.SizeChanged += (s, e) =>
            {
                if (podeseno || glavni.Width < 600 || glavni.Height < 300) return;
                podeseno = true;
                glavni.SplitterDistance = Math.Min(430, glavni.Width / 3);
                desno.SplitterDistance = Math.Min(170, desno.Height / 3);
            };

            btnOsvezi.Click += async (s, e) => await Ucitaj();
            cboStatus.SelectedIndexChanged += async (s, e) => await Ucitaj();
            // CurrentCellChanged, ne SelectionChanged: SelectionChanged stiže pre nego što se CurrentRow promeni
            dgvPoruke.CurrentCellChanged += async (s, e) => await PrikaziPoruku();
            btnPdf.Click += (s, e) => { var id = TrenutnaPoruka(); if (id != null) OtvoriPdf(id.Value); };
            btnArhiviraj.Click += async (s, e) => await Arhiviraj();
            btnDodaj.Click += async (s, e) => await DodajStavku();
            btnOdbaci.Click += async (s, e) => await Odbaci();
            btnPotvrdi.Click += async (s, e) => await Potvrdi(false);
            btnPotvrdiSve.Click += async (s, e) => await Potvrdi(true);
            dgvStavke.CellFormatting += DgvStavke_CellFormatting;
            dgvStavke.RowValidated += DgvStavke_RowValidated;
            dgvStavke.EditingControlShowing += DgvStavke_EditingControlShowing;
            dgvStavke.DataError += (s, e) => { Status("Neispravna vrednost: " + e.Exception?.Message); e.ThrowException = false; };
            dgvStavke.SelectionChanged += (s, e) => OsveziDugmad();
        }

        // ------------------------------------------------------------------ učitavanje

        public async Task Ucitaj()
        {
            if (ucitava || tab.IsDisposed) return;
            ucitava = true;
            try
            {
                var izabrana = TrenutnaPoruka();
                var status = cboStatus.SelectedItem as string;
                if (status == "Svi") status = null;
                var od = dtOd.Value; var @do = dtDo.Value;
                var rez = await Task.Run(() => new
                {
                    Poruke = TelegramiPregledRepository.Poruke(status, od, @do),
                    Pruge = pruge ?? TelegramiPregledRepository.Pruge(),
                    Stanice = stanice ?? TelegramiPregledRepository.Stanice()
                });
                if (tab.IsDisposed) return;
                if (pruge == null) { pruge = rez.Pruge; stanice = rez.Stanice; NapraviKoloneStavki(); }
                poruke = rez.Poruke;
                dgvPoruke.DataSource = poruke;
                if (izabrana != null)
                    foreach (DataGridViewRow row in dgvPoruke.Rows)
                        if (Convert.ToInt32(((DataRowView)row.DataBoundItem)["ID"]) == izabrana) { row.Selected = true; dgvPoruke.CurrentCell = row.Cells[0]; break; }
                Status("Poruka: " + poruke.Rows.Count);
                BrojPromenjen?.Invoke(this, EventArgs.Empty);
                await PrikaziPoruku();
            }
            catch (Exception ex)
            {
                Status("Učitavanje nije uspelo: " + ex.Message);
                OperationalDiagnostics.Write("Telegrami pregled: " + ex);
            }
            finally
            {
                ucitava = false;
            }
        }

        private int? prikazana;

        private async Task PrikaziPoruku()
        {
            var id = TrenutnaPoruka();
            if (id == prikazana && stavke != null) { OsveziDugmad(); return; }
            prikazana = id;
            if (id == null)
            {
                txtTekst.Clear();
                dgvStavke.DataSource = null;
                stavke = null;
                OsveziDugmad();
                return;
            }
            var r = ((DataRowView)dgvPoruke.CurrentRow.DataBoundItem).Row;
            txtTekst.Clear();
            Dodaj(txtTekst, "Razlog: " + Db.Text(r, "Poruka") + Environment.NewLine + Environment.NewLine, Color.DarkRed, true);
            var telo = Db.Text(r, "TekstTelegrama");
            Dodaj(txtTekst, telo.Length > 0 ? telo : "(nema teksta telegrama)" + Environment.NewLine + Db.Text(r, "TekstMaila"), SystemColors.WindowText, false);
            txtTekst.SelectionStart = 0;
            txtTekst.ScrollToCaret();
            try
            {
                var t = await Task.Run(() => TelegramiPregledRepository.Stavke(id.Value));
                if (TrenutnaPoruka() != id) return;
                stavke = t;
                dgvStavke.DataSource = stavke;
            }
            catch (Exception ex) { Status("Stavke: " + ex.Message); }
            OsveziDugmad();
        }

        // ------------------------------------------------------------------ akcije

        private async Task Potvrdi(bool sve)
        {
            var id = TrenutnaPoruka();
            if (id == null || stavke == null) return;
            dgvStavke.EndEdit();
            var redovi = (sve ? dgvStavke.Rows.Cast<DataGridViewRow>() : dgvStavke.SelectedRows.Cast<DataGridViewRow>())
                .Select(x => ((DataRowView)x.DataBoundItem).Row)
                .Where(x => Db.Text(x, "Status") == TelegramiPregledRepository.NaPregledu).ToList();
            if (redovi.Count == 0) { Status("Nema stavki za potvrdu (izaberite red sa statusom NeedsReview)."); return; }

            var zapamti = chkZapamti.Checked;
            try
            {
                var rez = await Task.Run(() => TelegramiPregledRepository.Potvrdi(id.Value, redovi, zapamti, korisnik));
                if (rez.Greske.Count > 0)
                {
                    MessageBox.Show(tab.FindForm(), string.Join(Environment.NewLine, rez.Greske), "Stavke nisu potpune", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Status("Upisano u Telegrami: " + rez.Upisano + (rez.Duplikata > 0 ? ", već postoji: " + rez.Duplikata : ""));
                prikazana = null;
                await Ucitaj();
            }
            catch (Exception ex)
            {
                MessageBox.Show(tab.FindForm(), ex.Message, "Potvrda nije uspela", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task Odbaci()
        {
            var id = TrenutnaPoruka();
            var ids = dgvStavke.SelectedRows.Cast<DataGridViewRow>().Select(x => ((DataRowView)x.DataBoundItem).Row)
                .Where(x => Db.Text(x, "Status") == TelegramiPregledRepository.NaPregledu).Select(x => Convert.ToInt32(x["ID"])).ToList();
            if (id == null || ids.Count == 0) return;
            if (MessageBox.Show(tab.FindForm(), "Odbaciti " + ids.Count + " stavki? Neće biti upisane u Telegrami.", "Odbaci", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            await Izvrsi(() => TelegramiPregledRepository.Odbaci(ids, id.Value, korisnik), "Odbačeno stavki: " + ids.Count);
        }

        private async Task Arhiviraj()
        {
            var id = TrenutnaPoruka();
            if (id == null) return;
            if (MessageBox.Show(tab.FindForm(), "Arhivirati poruku? Ništa se neće upisati u Telegrami.", "Arhiviraj", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            await Izvrsi(() => TelegramiPregledRepository.Arhiviraj(id.Value, korisnik), "Poruka arhivirana.");
        }

        private async Task DodajStavku()
        {
            var id = TrenutnaPoruka();
            if (id == null) return;
            await Izvrsi(() => TelegramiPregledRepository.DodajStavku(id.Value), "Dodata stavka; dopunite je i potvrdite.");
        }

        private async Task Izvrsi(Action a, string poruka)
        {
            try
            {
                await Task.Run(a);
                Status(poruka);
                prikazana = null;
                await Ucitaj();
            }
            catch (Exception ex)
            {
                MessageBox.Show(tab.FindForm(), ex.Message, "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void DgvStavke_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            // izmene se čuvaju kao nacrt, da se ne izgube ako se poruka zatvori pre potvrde
            if (stavke == null || e.RowIndex < 0 || e.RowIndex >= dgvStavke.Rows.Count) return;
            var drv = dgvStavke.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (drv == null || drv.Row.RowState != DataRowState.Modified || Db.Text(drv.Row, "Status") != TelegramiPregledRepository.NaPregledu) return;
            var r = drv.Row;
            try
            {
                await Task.Run(() => TelegramiPregledRepository.SacuvajNacrt(r));
                r.AcceptChanges();
            }
            catch (Exception ex) { Status("Izmena nije sačuvana: " + ex.Message); }
        }

        // ------------------------------------------------------------------ grid stavki

        private void NapraviKoloneStavki()
        {
            dgvStavke.Columns.Clear();
            KolonaTekst(dgvStavke, "Tacka", "T.", 30).ReadOnly = true;
            KolonaTekst(dgvStavke, "BrojTelegrama", "Broj", 50);
            KolonaTekst(dgvStavke, "DatumTelegrama", "Datum tel.", 75, "dd.MM.yyyy");
            KolonaTekst(dgvStavke, "PrugaTekst", "Pruga (telegram)", 160).ReadOnly = true;
            KolonaIzbor("PrugaID", "Pruga", 200, pruge);
            KolonaTekst(dgvStavke, "OdStaniceTekst", "Od (telegram)", 100).ReadOnly = true;
            KolonaIzbor("OdStanice", "Od", 130, stanice);
            KolonaTekst(dgvStavke, "DoStaniceTekst", "Do (telegram)", 100).ReadOnly = true;
            KolonaIzbor("DoStanice", "Do", 130, stanice);
            KolonaTekst(dgvStavke, "Kolosek", "Kolosek", 70);
            KolonaTekst(dgvStavke, "VaziOd", "Važi od", 110, "dd.MM.yyyy HH:mm");
            KolonaTekst(dgvStavke, "VaziDo", "Važi do", 110, "dd.MM.yyyy HH:mm");
            KolonaTekst(dgvStavke, "Napomena", "Napomena", 250);
            KolonaTekst(dgvStavke, "Status", "Status", 80).ReadOnly = true;
            var u = KolonaTekst(dgvStavke, "Upozorenje", "Upozorenje", 250);
            u.ReadOnly = true;
            u.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (DataGridViewColumn c in dgvStavke.Columns) c.DefaultCellStyle.FormatProvider = Sr;
        }

        private void KolonaIzbor(string polje, string naslov, int sirina, DataTable izvor)
        {
            var c = new DataGridViewComboBoxColumn
            {
                DataPropertyName = polje, Name = polje, HeaderText = naslov, Width = sirina,
                DataSource = izvor, ValueMember = "ID", DisplayMember = "Opis",
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton, FlatStyle = FlatStyle.Flat
            };
            dgvStavke.Columns.Add(c);
        }

        private void DgvStavke_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            // 1485 stanica: kucanje sa dopunjavanjem umesto skrolovanja liste
            if (e.Control is ComboBox cb)
            {
                cb.DropDownStyle = ComboBoxStyle.DropDown;
                cb.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                cb.AutoCompleteSource = AutoCompleteSource.ListItems;
            }
        }

        private void DgvStavke_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var drv = dgvStavke.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (drv == null) return;
            var ime = dgvStavke.Columns[e.ColumnIndex].Name;
            var naPregledu = Db.Text(drv.Row, "Status") == TelegramiPregledRepository.NaPregledu;
            if (!naPregledu) e.CellStyle.ForeColor = Color.Gray;
            else if ((ime == "PrugaID" || ime == "OdStanice" || ime == "DoStanice" || ime == "BrojTelegrama" || ime == "DatumTelegrama" || ime == "VaziOd" || ime == "VaziDo")
                     && drv.Row.IsNull(ime))
                e.CellStyle.BackColor = BojaNedostaje;
            if (ime == "Upozorenje" || ime == "Napomena")
                dgvStavke.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = Convert.ToString(drv[ime]);
        }

        // ------------------------------------------------------------------ pomoćno

        private void OsveziDugmad()
        {
            var r = dgvPoruke.CurrentRow?.DataBoundItem as DataRowView;
            var naPregledu = r != null && Db.Text(r.Row, "Status") == TelegramiPregledRepository.NaPregledu;
            btnPdf.Enabled = r != null && Convert.ToInt32(r["Priloga"]) > 0;
            btnArhiviraj.Enabled = smeUpis && naPregledu;
            btnDodaj.Enabled = smeUpis && naPregledu;
            var imaStavki = stavke != null && stavke.Rows.Cast<DataRow>().Any(x => Db.Text(x, "Status") == TelegramiPregledRepository.NaPregledu);
            btnPotvrdi.Enabled = btnOdbaci.Enabled = smeUpis && naPregledu && dgvStavke.SelectedRows.Count > 0;
            btnPotvrdiSve.Enabled = smeUpis && naPregledu && imaStavki;
            dgvStavke.ReadOnly = !(smeUpis && naPregledu);
        }

        private int? TrenutnaPoruka()
        {
            var r = dgvPoruke.CurrentRow?.DataBoundItem as DataRowView;
            return r == null ? (int?)null : Convert.ToInt32(r["ID"]);
        }

        private async void OtvoriPdf(int mailLogId)
        {
            try
            {
                var pdf = await Task.Run(() => TelegramiRepository.Pdf(mailLogId));
                if (pdf == null) { Status("Poruka nema priloga."); return; }
                TelegramiRepository.OtvoriPrilog(mailLogId, pdf);
            }
            catch (Exception ex) { Status("Otvaranje nije uspelo: " + ex.Message); }
        }

        private static void Pripremi(DataGridView g, bool samoCitanje)
        {
            g.Dock = DockStyle.Fill;
            g.AutoGenerateColumns = false;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.ReadOnly = samoCitanje;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.MultiSelect = !samoCitanje;
            g.RowHeadersWidth = 24;
            g.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        }

        private static DataGridViewTextBoxColumn KolonaTekst(DataGridView g, string polje, string naslov, int sirina, string format = null)
        {
            var c = new DataGridViewTextBoxColumn { DataPropertyName = polje, Name = polje, HeaderText = naslov, Width = sirina };
            if (format != null) c.DefaultCellStyle.Format = format;
            g.Columns.Add(c);
            return c;
        }

        private static void Dodaj(RichTextBox t, string tekst, Color boja, bool podebljano)
        {
            t.SelectionStart = t.TextLength;
            t.SelectionColor = boja;
            t.SelectionFont = new Font(t.Font, podebljano ? FontStyle.Bold : FontStyle.Regular);
            t.AppendText(tekst);
        }

        private void Status(string tekst)
        {
            if (!lblStatus.IsDisposed) lblStatus.Text = tekst;
        }

        public void Dispose()
        {
            tip.Dispose();
        }
    }
}
