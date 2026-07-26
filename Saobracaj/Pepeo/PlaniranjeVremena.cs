using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace Saobracaj.Pepeo
{
    public partial class PlaniranjeVremena : Form
    {
        private readonly InsertPepeo insert = new InsertPepeo();
        private readonly List<PepeoKorakVremena> trenutniKoraci = new List<PepeoKorakVremena>();

        private DataTable trenutnaTabelaKoraka;
        private PepeoGanttChart gantt;
        private int trenutniIDPrevoza = 0;
        private bool sinhronizacijaSelekcije = false;
        private bool sinhronizacijaCiklusa = false;

        public PlaniranjeVremena()
        {
            InitializeComponent();
        }

        private void PlaniranjeVremena_Load(object sender, EventArgs e)
        {
            dtpPocetakPrevoza.CustomFormat = "dd.MM.yyyy HH:mm:ss";
            dtpPocetakPrevoza.Format = DateTimePickerFormat.Custom;
            dtpPocetakPrevoza.Value = DateTime.Now;

            dtpKasnjenje.CustomFormat = "dd.MM.yyyy HH:mm:ss";
            dtpKasnjenje.Format = DateTimePickerFormat.Custom;
            dtpKasnjenje.Value = DateTime.Now;

            txtBrojCiklusa.Text = "3";

            PripremiTaboveCiklusa();
            PripremiGrid();
            PoveziDogadjaje();
            FillCombo();
        }

        private void PoveziDogadjaje()
        {
            dgvVremena.SelectionChanged -= dgvVremena_SelectionChanged;
            dgvVremena.SelectionChanged += dgvVremena_SelectionChanged;

            tabCiklusi.SelectedIndexChanged -= tabCiklusi_SelectedIndexChanged;
            tabCiklusi.SelectedIndexChanged += tabCiklusi_SelectedIndexChanged;

            cboCiklus.SelectedIndexChanged -= cboCiklus_SelectedIndexChanged;
            cboCiklus.SelectedIndexChanged += cboCiklus_SelectedIndexChanged;

            btnGenerisiPlan.Click -= btnGenerisiPlan_Click;
            btnGenerisiPlan.Click += btnGenerisiPlan_Click;

            btnUcitajPlan.Click -= btnUcitajPlan_Click;
            btnUcitajPlan.Click += btnUcitajPlan_Click;

            btnPotvrdiKasnjenje.Click -= btnPotvrdiKasnjenje_Click;
            btnPotvrdiKasnjenje.Click += btnPotvrdiKasnjenje_Click;

            btnPoveziNajavu.Click -= btnPoveziNajavu_Click;
            btnPoveziNajavu.Click += btnPoveziNajavu_Click;

            btnZavrsiCiklus.Click -= btnZavrsiCiklus_Click;
            btnZavrsiCiklus.Click += btnZavrsiCiklus_Click;
        }

        private void PripremiTaboveCiklusa()
        {
            tabCiklusi.TabPages.Clear();
            tabCiklusi.Alignment = TabAlignment.Bottom;
            tabCiklusi.Multiline = true;
            tabCiklusi.SizeMode = TabSizeMode.Normal;
            tabCiklusi.Padding = new System.Drawing.Point(18, 5);
        }

        private void PripremiGrid()
        {
            dgvVremena.AutoGenerateColumns = true;
            dgvVremena.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvVremena.MultiSelect = false;
            dgvVremena.AllowUserToAddRows = false;
            dgvVremena.AllowUserToDeleteRows = false;
            dgvVremena.ReadOnly = true;
            dgvVremena.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dgvVremena.RowHeadersVisible = false;
        }

        private void FillCombo()
        {
            try
            {
                DataTable dtTrase = insert.UcitajTrase();
                cboTrasa.DataSource = dtTrase;
                cboTrasa.DisplayMember = "Stanice";
                cboTrasa.ValueMember = "ID";

                DataTable dtNajave = insert.UcitajNajave();
                cboNajava.DataSource = dtNajave;
                cboNajava.DisplayMember = "Put";
                cboNajava.ValueMember = "ID";
                cboNajava.SelectedIndex = -1;

                DataTable dtKasnjenja = insert.UcitajSifarnikKasnjenja();
                DataRow prazanRazlog = dtKasnjenja.NewRow();
                prazanRazlog["ID"] = 0;
                prazanRazlog["Razlog"] = "-- izaberite razlog kašnjenja --";
                dtKasnjenja.Rows.InsertAt(prazanRazlog, 0);

                cboKasnjenje.DropDownStyle = ComboBoxStyle.DropDownList;
                cboKasnjenje.DataSource = dtKasnjenja;
                cboKasnjenje.DisplayMember = "Razlog";
                cboKasnjenje.ValueMember = "ID";
                cboKasnjenje.SelectedValue = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Greška pri učitavanju trasa/najava/šifarnika kašnjenja:" + Environment.NewLine + ex.Message,
                    "Greška",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnGenerisiPlan_Click(object sender, EventArgs e)
        {
            int idTrase = VratiIntIzCombo(cboTrasa.SelectedValue);
            if (idTrase <= 0)
            {
                MessageBox.Show("Izaberite trasu.");
                return;
            }

            int brojCiklusa;
            if (!int.TryParse(txtBrojCiklusa.Text.Trim(), out brojCiklusa) || brojCiklusa <= 0)
            {
                MessageBox.Show("Unesite ispravan broj ciklusa.");
                return;
            }

            try
            {
                List<PepeoSablonOperacije> sabloni = insert.UcitajSabloneOperacija();
                Dictionary<string, string> parametri = insert.UcitajParametrePravila();

                if (sabloni == null || sabloni.Count == 0)
                {
                    MessageBox.Show("Nije pronađen šablon operacija. Pokrenite SQL skriptu za pepeo vremena.");
                    return;
                }

                PepeoKalkulatorVremena kalkulator = new PepeoKalkulatorVremena(sabloni, parametri);

                // Najava se ne vezuje prilikom generisanja, zato se prosleđuje 0.
                List<PepeoKorakVremena> koraci = kalkulator.Izracunaj(
                    dtpPocetakPrevoza.Value,
                    brojCiklusa,
                    idTrase,
                    0);

                trenutniIDPrevoza = insert.KreirajNoviPrevozSaCiklusima(
                    idTrase,
                    dtpPocetakPrevoza.Value,
                    koraci);

                UcitajPrevoz(trenutniIDPrevoza, null, null);

                MessageBox.Show(
                    "Kreiran je novi prevoz i generisano je " + brojCiklusa.ToString() + " ciklusa.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Greška pri generisanju plana:" + Environment.NewLine + ex.Message,
                    "Greška",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnUcitajPlan_Click(object sender, EventArgs e)
        {
            int idTrase = VratiIntIzCombo(cboTrasa.SelectedValue);
            if (idTrase <= 0)
            {
                MessageBox.Show("Izaberite trasu.");
                return;
            }

            try
            {
                int idPrevoza = insert.UcitajAktivniPrevozZaTrasu(idTrase);
                if (idPrevoza <= 0)
                {
                    MessageBox.Show("Za izabranu trasu ne postoji aktivan prevoz. Prvo generišite novi plan.");
                    return;
                }

                trenutniIDPrevoza = idPrevoza;
                UcitajPrevoz(trenutniIDPrevoza, null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Greška pri učitavanju plana:" + Environment.NewLine + ex.Message,
                    "Greška",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnPotvrdiKasnjenje_Click(object sender, EventArgs e)
        {
            PepeoKorakVremena selektovaniKorak = VratiSelektovaniKorak();
            if (selektovaniKorak == null)
            {
                MessageBox.Show("Izaberite red/operaciju za koju upisujete kašnjenje.");
                return;
            }

            int razlogKasnjenjaId = VratiIntIzCombo(cboKasnjenje.SelectedValue);
            if (razlogKasnjenjaId <= 0)
            {
                MessageBox.Show("Izaberite razlog kašnjenja iz šifarnika.");
                cboKasnjenje.Focus();
                return;
            }

            try
            {
                OsveziListuKoraka();

                int indeks = trenutniKoraci.FindIndex(x => x.ID == selektovaniKorak.ID);
                if (indeks < 0)
                {
                    MessageBox.Show("Selektovana operacija nije pronađena u trenutnom planu.");
                    return;
                }

                DateTime novoVreme = dtpKasnjenje.Value;
                string napomena = txtNapomena.Text.Trim();

                PepeoKalkulatorVremena kalkulator = NapraviKalkulator();
                kalkulator.PreracunajPosleRucneIzmene(
                    trenutniKoraci,
                    indeks,
                    novoVreme,
                    napomena);

                trenutniKoraci[indeks].RazlogKasnjenjaID = razlogKasnjenjaId;
                trenutniKoraci[indeks].RazlogKasnjenja = cboKasnjenje.Text.Trim();

                insert.AzurirajKorakePosleIzmene(
                    trenutniKoraci,
                    indeks,
                    selektovaniKorak.ID,
                    novoVreme,
                    razlogKasnjenjaId,
                    napomena);

                UcitajPrevoz(
                    trenutniIDPrevoza,
                    selektovaniKorak.CiklusID,
                    selektovaniKorak.ID);

                cboKasnjenje.SelectedValue = 0;

                MessageBox.Show("Kašnjenje je upisano i svi naredni koraci su preračunati.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Greška pri upisu kašnjenja:" + Environment.NewLine + ex.Message,
                    "Greška",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnPoveziNajavu_Click(object sender, EventArgs e)
        {
            int ciklusId = VratiIntIzCombo(cboCiklus.SelectedValue);
            int najavaId = VratiIntIzCombo(cboNajava.SelectedValue);

            if (ciklusId <= 0)
            {
                MessageBox.Show("Izaberite ciklus.");
                return;
            }

            if (najavaId <= 0)
            {
                MessageBox.Show("Izaberite najavu.");
                return;
            }

            try
            {
                insert.PoveziNajavuSaCiklusom(ciklusId, najavaId);
                UcitajPrevoz(trenutniIDPrevoza, ciklusId, null);
                MessageBox.Show("Najava je povezana sa izabranim ciklusom.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Greška pri povezivanju najave:" + Environment.NewLine + ex.Message,
                    "Greška",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnZavrsiCiklus_Click(object sender, EventArgs e)
        {
            int ciklusId = VratiIntIzCombo(cboCiklus.SelectedValue);
            if (ciklusId <= 0)
            {
                MessageBox.Show("Izaberite ciklus koji završavate.");
                return;
            }

            if (trenutniIDPrevoza <= 0)
            {
                MessageBox.Show("Nije učitan prevoz.");
                return;
            }

            try
            {
                OsveziListuKoraka();

                List<PepeoKorakVremena> koraciCiklusa = trenutniKoraci
                    .Where(x => x.CiklusID == ciklusId)
                    .OrderBy(x => x.RB)
                    .ToList();

                if (koraciCiklusa.Count == 0)
                {
                    MessageBox.Show("Nema koraka za izabrani ciklus.");
                    return;
                }

                if (string.Equals(
                    koraciCiklusa[0].StatusCiklusa,
                    "ZAVRSEN",
                    StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Izabrani ciklus je već završen.");
                    return;
                }

                PepeoKorakVremena poslednjiKorakCiklusa = koraciCiklusa[koraciCiklusa.Count - 1];
                int indeks = trenutniKoraci.FindIndex(x => x.ID == poslednjiKorakCiklusa.ID);

                DateTime stvarniKraj = dtpKasnjenje.Value;
                string napomena = txtNapomena.Text.Trim();

                int? razlogKasnjenjaId = null;
                bool vremeJePromenjeno = Math.Abs(
                    (stvarniKraj - poslednjiKorakCiklusa.PlaniranoVreme).TotalMinutes) >= 1.0;

                if (vremeJePromenjeno)
                {
                    int izabraniRazlog = VratiIntIzCombo(cboKasnjenje.SelectedValue);
                    if (izabraniRazlog <= 0)
                    {
                        MessageBox.Show(
                            "Pošto se stvarno vreme razlikuje od planiranog, izaberite razlog kašnjenja.");
                        cboKasnjenje.Focus();
                        return;
                    }

                    razlogKasnjenjaId = izabraniRazlog;
                }

                PepeoKalkulatorVremena kalkulator = NapraviKalkulator();
                kalkulator.PreracunajPosleRucneIzmene(
                    trenutniKoraci,
                    indeks,
                    stvarniKraj,
                    napomena);

                trenutniKoraci[indeks].RazlogKasnjenjaID = razlogKasnjenjaId;
                trenutniKoraci[indeks].RazlogKasnjenja = razlogKasnjenjaId.HasValue
                    ? cboKasnjenje.Text.Trim()
                    : string.Empty;

                insert.AzurirajKorakePosleIzmene(
                    trenutniKoraci,
                    indeks,
                    poslednjiKorakCiklusa.ID,
                    stvarniKraj,
                    razlogKasnjenjaId,
                    napomena);

                insert.ZavrsiCiklus(
                    ciklusId,
                    stvarniKraj,
                    razlogKasnjenjaId,
                    napomena);

                // Kada se završi jedan ciklus, dodaje se novi na kraj postojećeg plana.
                DodajSledeciCiklus();

                // Ostani na sheet-u upravo završenog ciklusa.
                UcitajPrevoz(trenutniIDPrevoza, ciklusId, poslednjiKorakCiklusa.ID);
                cboKasnjenje.SelectedValue = 0;

                MessageBox.Show("Ciklus je završen i automatski je dodat novi sledeći ciklus.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Greška pri završetku ciklusa:" + Environment.NewLine + ex.Message,
                    "Greška",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void DodajSledeciCiklus()
        {
            if (trenutniIDPrevoza <= 0)
                return;

            OsveziListuKoraka();

            if (trenutniKoraci.Count == 0)
                return;

            int idTrase = trenutniKoraci[0].IDTrase;
            int sledeciRedniBroj = trenutniKoraci.Max(x => x.Ciklus) + 1;

            PepeoKorakVremena poslednjiKorak = trenutniKoraci
                .OrderBy(x => x.Ciklus)
                .ThenBy(x => x.RB)
                .Last();

            DateTime pocetakNovogCiklusa = poslednjiKorak.StvarnoVreme.HasValue
                ? poslednjiKorak.StvarnoVreme.Value
                : poslednjiKorak.PlaniranoVreme;

            PepeoKalkulatorVremena kalkulator = NapraviKalkulator();
            List<PepeoKorakVremena> noviKoraci = kalkulator.Izracunaj(
                pocetakNovogCiklusa,
                1,
                idTrase,
                0);

            foreach (PepeoKorakVremena korak in noviKoraci)
            {
                korak.Ciklus = sledeciRedniBroj;
                korak.IDTrase = idTrase;
            }

            insert.DodajCiklusPrevozu(
                trenutniIDPrevoza,
                idTrase,
                sledeciRedniBroj,
                noviKoraci);
        }

        private void UcitajPrevoz(
            int idPrevoza,
            int? preferiraniCiklusId,
            int? preferiraniKorakId)
        {
            if (idPrevoza <= 0)
                return;

            trenutniIDPrevoza = idPrevoza;

            if (!preferiraniCiklusId.HasValue)
            {
                int trenutnoIzabranCiklus = VratiIzabraniCiklusIdIzTaba();
                if (trenutnoIzabranCiklus > 0)
                    preferiraniCiklusId = trenutnoIzabranCiklus;
            }

            trenutnaTabelaKoraka = insert.UcitajKorakePrevozaTabela(idPrevoza);
            OsveziListuKoraka();
            UcitajCikluseCombo(preferiraniCiklusId);
            NapraviTaboveCiklusa(preferiraniCiklusId);
            PrikaziIzabraniCiklus();
            UrediGrid();

            if (preferiraniKorakId.HasValue && preferiraniKorakId.Value > 0)
                SelektujKorakPoId(preferiraniKorakId.Value);
        }

        private void OsveziListuKoraka()
        {
            trenutniKoraci.Clear();

            if (trenutniIDPrevoza > 0)
                trenutniKoraci.AddRange(insert.UcitajKorakePrevoza(trenutniIDPrevoza));
        }

        private void UcitajCikluseCombo(int? preferiraniCiklusId)
        {
            sinhronizacijaCiklusa = true;
            try
            {
                DataTable dt = insert.UcitajCiklusePrevoza(trenutniIDPrevoza);
                cboCiklus.DataSource = dt;
                cboCiklus.DisplayMember = "Prikaz";
                cboCiklus.ValueMember = "CiklusID";

                if (preferiraniCiklusId.HasValue && preferiraniCiklusId.Value > 0)
                    cboCiklus.SelectedValue = preferiraniCiklusId.Value;
                else if (dt.Rows.Count > 0)
                    cboCiklus.SelectedIndex = 0;
            }
            finally
            {
                sinhronizacijaCiklusa = false;
            }
        }

        private void NapraviTaboveCiklusa(int? preferiraniCiklusId)
        {
            sinhronizacijaCiklusa = true;
            tabCiklusi.SuspendLayout();

            try
            {
                while (tabCiklusi.TabPages.Count > 0)
                {
                    TabPage stariTab = tabCiklusi.TabPages[0];
                    tabCiklusi.TabPages.RemoveAt(0);
                    stariTab.Dispose();
                }
                gantt = null;

                List<IGrouping<int, PepeoKorakVremena>> grupe = trenutniKoraci
                    .GroupBy(x => x.CiklusID)
                    .OrderBy(x => x.Min(k => k.Ciklus))
                    .ToList();

                int indeksZaIzbor = 0;

                for (int i = 0; i < grupe.Count; i++)
                {
                    IGrouping<int, PepeoKorakVremena> grupa = grupe[i];
                    List<PepeoKorakVremena> koraciCiklusa = grupa
                        .OrderBy(x => x.RB)
                        .ToList();

                    if (koraciCiklusa.Count == 0)
                        continue;

                    TabPage tab = new TabPage();
                    tab.Name = "tabCiklus_" + grupa.Key.ToString();
                    tab.Tag = grupa.Key;
                    tab.Text = NapraviNazivTaba(koraciCiklusa);
                    tab.Padding = new Padding(0);
                    tab.UseVisualStyleBackColor = true;

                    PepeoGanttChart chart = new PepeoGanttChart();
                    chart.Name = "ganttCiklus_" + grupa.Key.ToString();
                    chart.Dock = DockStyle.Fill;
                    chart.BrojDanaPrikaza = 14;
                    chart.SelectionChanged += gantt_SelectionChanged;
                    chart.Prikazi(koraciCiklusa);

                    tab.Controls.Add(chart);
                    tabCiklusi.TabPages.Add(tab);

                    if (preferiraniCiklusId.HasValue && grupa.Key == preferiraniCiklusId.Value)
                        indeksZaIzbor = tabCiklusi.TabPages.Count - 1;
                }

                if (tabCiklusi.TabPages.Count > 0)
                    tabCiklusi.SelectedIndex = indeksZaIzbor;
            }
            finally
            {
                tabCiklusi.ResumeLayout();
                sinhronizacijaCiklusa = false;
            }
        }

        private string NapraviNazivTaba(List<PepeoKorakVremena> koraciCiklusa)
        {
            PepeoKorakVremena prvi = koraciCiklusa[0];

            string najava = prvi.Najava.HasValue && prvi.Najava.Value > 0
                ? prvi.Najava.Value.ToString()
                : "bez najave";

            string status = string.Equals(
                prvi.StatusCiklusa,
                "ZAVRSEN",
                StringComparison.OrdinalIgnoreCase)
                ? "  ✓"
                : string.Empty;

            return "C" + prvi.Ciklus.ToString() + " - " + najava + status;
        }

        private void PrikaziIzabraniCiklus()
        {
            if (tabCiklusi.SelectedTab == null)
            {
                dgvVremena.DataSource = null;
                gantt = null;
                return;
            }

            int ciklusId = VratiInt(tabCiklusi.SelectedTab.Tag);
            if (ciklusId <= 0)
                return;

            gantt = tabCiklusi.SelectedTab.Controls
                .OfType<PepeoGanttChart>()
                .FirstOrDefault();

            FiltrirajGridNaCiklus(ciklusId);
            PostaviCiklusCombo(ciklusId);
            PostaviNajavuAktivnogCiklusa(ciklusId);
        }

        private void FiltrirajGridNaCiklus(int ciklusId)
        {
            if (trenutnaTabelaKoraka == null ||
                !trenutnaTabelaKoraka.Columns.Contains("CiklusID"))
            {
                dgvVremena.DataSource = null;
                return;
            }

            DataView pregled = new DataView(trenutnaTabelaKoraka);
            pregled.RowFilter = "CiklusID = " + ciklusId.ToString();

            if (trenutnaTabelaKoraka.Columns.Contains("RB"))
                pregled.Sort = "RB ASC";

            sinhronizacijaSelekcije = true;
            try
            {
                dgvVremena.DataSource = pregled;
                UrediGrid();
                dgvVremena.ClearSelection();

                if (dgvVremena.Rows.Count > 0)
                {
                    dgvVremena.Rows[0].Selected = true;
                    DataGridViewColumn prvaKolona = VratiPrvuVidljivuKolonu();
                    if (prvaKolona != null)
                        dgvVremena.CurrentCell = dgvVremena.Rows[0].Cells[prvaKolona.Index];
                }
            }
            finally
            {
                sinhronizacijaSelekcije = false;
            }
        }

        private void PostaviCiklusCombo(int ciklusId)
        {
            if (cboCiklus.DataSource == null)
                return;

            sinhronizacijaCiklusa = true;
            try
            {
                cboCiklus.SelectedValue = ciklusId;
            }
            finally
            {
                sinhronizacijaCiklusa = false;
            }
        }

        private void PostaviNajavuAktivnogCiklusa(int ciklusId)
        {
            PepeoKorakVremena prvi = trenutniKoraci
                .Where(x => x.CiklusID == ciklusId)
                .OrderBy(x => x.RB)
                .FirstOrDefault();

            if (prvi == null || !prvi.Najava.HasValue || prvi.Najava.Value <= 0)
            {
                cboNajava.SelectedIndex = -1;
                return;
            }

            cboNajava.SelectedValue = prvi.Najava.Value;
        }

        private void tabCiklusi_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sinhronizacijaCiklusa)
                return;

            PrikaziIzabraniCiklus();
        }

        private void cboCiklus_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sinhronizacijaCiklusa)
                return;

            int ciklusId = VratiIntIzCombo(cboCiklus.SelectedValue);
            if (ciklusId <= 0)
                return;

            for (int i = 0; i < tabCiklusi.TabPages.Count; i++)
            {
                if (VratiInt(tabCiklusi.TabPages[i].Tag) == ciklusId)
                {
                    tabCiklusi.SelectedIndex = i;
                    break;
                }
            }
        }

        private int VratiIzabraniCiklusIdIzTaba()
        {
            if (tabCiklusi.SelectedTab == null)
                return 0;

            return VratiInt(tabCiklusi.SelectedTab.Tag);
        }

        private void UrediGrid()
        {
            if (dgvVremena.Columns.Count == 0)
                return;

            SakrijKolonu("ID");
            SakrijKolonu("IDPrevoza");
            SakrijKolonu("CiklusID");
            SakrijKolonu("IDTrase");
            SakrijKolonu("SifraKoraka");
            SakrijKolonu("Stanica");
            SakrijKolonu("UlazakUStanicu");
            SakrijKolonu("RazlogKasnjenjaID");

            PromeniHeader("Ciklus", "Ciklus");
            PromeniHeader("Najava", "Najava");
            PromeniHeader("StatusCiklusa", "Status");
            PromeniHeader("RB", "RB");
            PromeniHeader("StanicaOpis", "Stanica");
            PromeniHeader("Operacija", "Operacija");
            PromeniHeader("TipDogadjaja", "Tip");
            PromeniHeader("PlaniraniPocetak", "Plan. početak");
            PromeniHeader("PlaniranoVreme", "Plan. kraj");
            PromeniHeader("StvarnoVreme", "Stvarno vreme");
            PromeniHeader("RucnoIzmenjeno", "Ručno");
            PromeniHeader("KasnjenjeMin", "Kašnjenje min");
            PromeniHeader("RazlogKasnjenja", "Razlog kašnjenja");
            PromeniHeader("Napomena", "Napomena");
        }

        private void SakrijKolonu(string naziv)
        {
            if (dgvVremena.Columns.Contains(naziv))
                dgvVremena.Columns[naziv].Visible = false;
        }

        private void PromeniHeader(string naziv, string header)
        {
            if (dgvVremena.Columns.Contains(naziv))
                dgvVremena.Columns[naziv].HeaderText = header;
        }

        private DataGridViewColumn VratiPrvuVidljivuKolonu()
        {
            foreach (DataGridViewColumn kolona in dgvVremena.Columns)
            {
                if (kolona.Visible)
                    return kolona;
            }

            return null;
        }

        private PepeoKalkulatorVremena NapraviKalkulator()
        {
            List<PepeoSablonOperacije> sabloni = insert.UcitajSabloneOperacija();
            Dictionary<string, string> parametri = insert.UcitajParametrePravila();
            return new PepeoKalkulatorVremena(sabloni, parametri);
        }

        private PepeoKorakVremena VratiSelektovaniKorak()
        {
            if (dgvVremena.CurrentRow != null &&
                dgvVremena.Columns.Contains("ID") &&
                dgvVremena.CurrentRow.Cells["ID"] != null)
            {
                int id = VratiInt(dgvVremena.CurrentRow.Cells["ID"].Value);
                PepeoKorakVremena izGrida = trenutniKoraci.FirstOrDefault(x => x.ID == id);

                if (izGrida != null)
                    return izGrida;
            }

            if (gantt != null && gantt.SelectedItem != null)
                return gantt.SelectedItem;

            return null;
        }

        private void dgvVremena_SelectionChanged(object sender, EventArgs e)
        {
            if (sinhronizacijaSelekcije)
                return;

            if (dgvVremena.CurrentRow == null || !dgvVremena.Columns.Contains("ID"))
                return;

            int id = VratiInt(dgvVremena.CurrentRow.Cells["ID"].Value);
            if (id <= 0)
                return;

            PepeoKorakVremena korak = trenutniKoraci.FirstOrDefault(x => x.ID == id);
            if (korak == null)
                return;

            sinhronizacijaSelekcije = true;
            try
            {
                if (gantt != null)
                    gantt.SelektujPoId(id);

                dtpKasnjenje.Value = korak.StvarnoVreme.HasValue
                    ? korak.StvarnoVreme.Value
                    : korak.PlaniranoVreme;

                txtNapomena.Text = korak.Napomena ?? string.Empty;
                PostaviRazlogKasnjenja(korak.RazlogKasnjenjaID);
            }
            finally
            {
                sinhronizacijaSelekcije = false;
            }
        }

        private void gantt_SelectionChanged(object sender, EventArgs e)
        {
            if (sinhronizacijaSelekcije)
                return;

            PepeoGanttChart chart = sender as PepeoGanttChart;
            if (chart == null || chart.SelectedItem == null)
                return;

            gantt = chart;
            PepeoKorakVremena korak = chart.SelectedItem;

            sinhronizacijaSelekcije = true;
            try
            {
                dtpKasnjenje.Value = korak.StvarnoVreme.HasValue
                    ? korak.StvarnoVreme.Value
                    : korak.PlaniranoVreme;

                txtNapomena.Text = korak.Napomena ?? string.Empty;
                PostaviRazlogKasnjenja(korak.RazlogKasnjenjaID);

                SelektujRedGridaPoId(korak.ID);
            }
            finally
            {
                sinhronizacijaSelekcije = false;
            }
        }

        private void SelektujKorakPoId(int id)
        {
            if (id <= 0)
                return;

            PepeoKorakVremena korak = trenutniKoraci.FirstOrDefault(x => x.ID == id);
            if (korak == null)
                return;

            if (VratiIzabraniCiklusIdIzTaba() != korak.CiklusID)
            {
                for (int i = 0; i < tabCiklusi.TabPages.Count; i++)
                {
                    if (VratiInt(tabCiklusi.TabPages[i].Tag) == korak.CiklusID)
                    {
                        tabCiklusi.SelectedIndex = i;
                        break;
                    }
                }
            }

            if (gantt != null)
                gantt.SelektujPoId(id);

            SelektujRedGridaPoId(id);
        }

        private void SelektujRedGridaPoId(int id)
        {
            if (dgvVremena.Rows.Count == 0 || !dgvVremena.Columns.Contains("ID"))
                return;

            foreach (DataGridViewRow row in dgvVremena.Rows)
            {
                if (VratiInt(row.Cells["ID"].Value) == id)
                {
                    dgvVremena.ClearSelection();
                    row.Selected = true;

                    DataGridViewColumn prvaKolona = VratiPrvuVidljivuKolonu();
                    if (prvaKolona != null)
                        dgvVremena.CurrentCell = row.Cells[prvaKolona.Index];

                    break;
                }
            }
        }

        private void PostaviRazlogKasnjenja(int? razlogKasnjenjaId)
        {
            if (cboKasnjenje.DataSource == null)
                return;

            if (razlogKasnjenjaId.HasValue && razlogKasnjenjaId.Value > 0)
                cboKasnjenje.SelectedValue = razlogKasnjenjaId.Value;
            else
                cboKasnjenje.SelectedValue = 0;
        }

        private int VratiIntIzCombo(object value)
        {
            if (value == null || value == DBNull.Value || value is DataRowView)
                return 0;

            return VratiInt(value);
        }

        private int VratiInt(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0;

            int result;
            if (int.TryParse(value.ToString(), out result))
                return result;

            return 0;
        }
    }
}