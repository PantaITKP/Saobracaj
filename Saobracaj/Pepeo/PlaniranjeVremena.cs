using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Saobracaj.Pepeo
{
    public partial class PlaniranjeVremena : Form
    {
        private readonly InsertPepeo insert = new InsertPepeo();
        private bool prikaziZavrsene = false;
        private bool dogadjajiPovezani = false;
        private bool sinhronizujemSelekciju = false;
        private List<PepeoCiklusInfo> trenutniCiklusi = new List<PepeoCiklusInfo>();
        private List<PepeoKorakVremena> trenutniKoraci = new List<PepeoKorakVremena>();

        public PlaniranjeVremena()
        {
            InitializeComponent();
        }

        private void PlaniranjeVremena_Load(object sender, EventArgs e)
        {
            PoveziDogadjaje();

            dtpPocetakPrevoza.Value = DateTime.Now;
            dtpKasnjenje.Value = DateTime.Now;
            txtBrojCiklusa.Text = "3";

            UcitajSifarnike();
            prikaziZavrsene = false;
            OsveziPrikaz();
        }

        private void PoveziDogadjaje()
        {
            if (dogadjajiPovezani)
                return;

            btnGenerisiPlan.Click += btnGenerisiPlan_Click;
            btnUcitajPlan.Click += btnUcitajPlan_Click;
            btnPoveziNajavu.Click += btnPoveziNajavu_Click;
            btnPotvrdiKasnjenje.Click += btnPotvrdiKasnjenje_Click;
            btnZavrsiCiklus.Click += btnZavrsiCiklus_Click;
            btnAktivniPrevozi.Click += btnAktivniPrevozi_Click;
            btnZavrseniPrevozi.Click += btnZavrseniPrevozi_Click;

            cboTrasa.SelectedIndexChanged += cboTrasa_SelectedIndexChanged;
            cboCiklus.SelectedIndexChanged += cboCiklus_SelectedIndexChanged;
            tabCiklusi.SelectedIndexChanged += tabCiklusi_SelectedIndexChanged;
            dgvVremena.SelectionChanged += dgvVremena_SelectionChanged;

            dogadjajiPovezani = true;
        }

        private void UcitajSifarnike()
        {
            try
            {
                DataTable trase = insert.UcitajTrase();
                cboTrasa.DataSource = trase;
                cboTrasa.DisplayMember = "Naziv";
                cboTrasa.ValueMember = "ID";

                DataTable najave = insert.UcitajNajave();
                cboNajava.DataSource = najave;
                cboNajava.DisplayMember = "Put";
                cboNajava.ValueMember = "ID";

                DataTable razlozi = insert.UcitajSifarnikKasnjenja();
                cboKasnjenje.DataSource = razlozi;
                cboKasnjenje.DisplayMember = "Razlog";
                cboKasnjenje.ValueMember = "ID";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri učitavanju šifarnika:" + Environment.NewLine + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private PepeoKalkulatorVremena NapraviKalkulator(int trasaID)
        {
            return new PepeoKalkulatorVremena(
                insert.UcitajOperacijeZaTrasu(trasaID),
                insert.UcitajParametrePravila());
        }

        private void btnGenerisiPlan_Click(object sender, EventArgs e)
        {
            int trasaID = VratiInt(cboTrasa.SelectedValue);
            if (trasaID <= 0)
            {
                MessageBox.Show("Izaberite trasu.");
                return;
            }

            int brojCiklusa;
            if (!int.TryParse(txtBrojCiklusa.Text.Trim(), out brojCiklusa) || brojCiklusa <= 0 || brojCiklusa > 20)
            {
                MessageBox.Show("Unesite broj ciklusa od 1 do 20.");
                return;
            }

            try
            {
                PepeoKalkulatorVremena kalkulator = NapraviKalkulator(trasaID);
                insert.GenerisiPocetneCikluse(trasaID, dtpPocetakPrevoza.Value, brojCiklusa, kalkulator);

                prikaziZavrsene = false;
                OsveziPrikaz();
                MessageBox.Show("Generisano je " + brojCiklusa + " ciklusa. Broj ciklusa je njegov jedinstveni ID.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri generisanju:" + Environment.NewLine + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUcitajPlan_Click(object sender, EventArgs e)
        {
            OsveziPrikaz();
        }

        private void btnAktivniPrevozi_Click(object sender, EventArgs e)
        {
            prikaziZavrsene = false;
            OsveziPrikaz();
        }

        private void btnZavrseniPrevozi_Click(object sender, EventArgs e)
        {
            prikaziZavrsene = true;
            OsveziPrikaz();
        }

        private void cboTrasa_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (IsHandleCreated)
                OsveziPrikaz();
        }

        private void OsveziPrikaz()
        {
            int trasaID = VratiInt(cboTrasa.SelectedValue);
            if (trasaID <= 0)
                return;

            try
            {
                trenutniCiklusi = insert.UcitajCikluse(prikaziZavrsene, trasaID);
                NapuniCiklusCombo();
                NapuniTabove();
                PostaviRezimKontrola();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri učitavanju ciklusa:" + Environment.NewLine + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NapuniCiklusCombo()
        {
            cboCiklus.DataSource = null;
            cboCiklus.DisplayMember = "TekstZaCombo";
            cboCiklus.ValueMember = "CiklusID";
            cboCiklus.DataSource = trenutniCiklusi.ToList();
        }

        private void NapuniTabove()
        {
            tabCiklusi.TabPages.Clear();
            trenutniKoraci.Clear();
            dgvVremena.DataSource = null;

            foreach (PepeoCiklusInfo ciklus in trenutniCiklusi)
            {
                List<PepeoKorakVremena> koraci = insert.UcitajKorakeCiklusa(ciklus.CiklusID);
                trenutniKoraci.AddRange(koraci);

                string najava = ciklus.NajavaID.HasValue ? " | N" + ciklus.NajavaID.Value : "";
                string naziv = "C" + ciklus.CiklusID + najava;
                if (string.Equals(ciklus.StatusCiklusa, "ZAVRSEN", StringComparison.OrdinalIgnoreCase))
                    naziv += " ✓";

                var page = new TabPage(naziv);
                page.Tag = ciklus;

                var gantt = new PepeoGanttChart();
                gantt.Dock = DockStyle.Fill;
                gantt.Tag = ciklus.CiklusID;
                gantt.SelectionChanged += gantt_SelectionChanged;
                gantt.Prikazi(koraci);

                page.Controls.Add(gantt);
                tabCiklusi.TabPages.Add(page);
            }

            if (tabCiklusi.TabPages.Count > 0)
            {
                tabCiklusi.SelectedIndex = 0;
                PrikaziSelektovaniCiklus();
            }
        }

        private void tabCiklusi_SelectedIndexChanged(object sender, EventArgs e)
        {
            PrikaziSelektovaniCiklus();
        }

        private void cboCiklus_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ciklusID = VratiInt(cboCiklus.SelectedValue);
            if (ciklusID <= 0)
                return;

            for (int i = 0; i < tabCiklusi.TabPages.Count; i++)
            {
                PepeoCiklusInfo info = tabCiklusi.TabPages[i].Tag as PepeoCiklusInfo;
                if (info != null && info.CiklusID == ciklusID)
                {
                    if (tabCiklusi.SelectedIndex != i)
                        tabCiklusi.SelectedIndex = i;
                    break;
                }
            }
        }

        private void PrikaziSelektovaniCiklus()
        {
            if (tabCiklusi.SelectedTab == null)
            {
                dgvVremena.DataSource = null;
                return;
            }

            PepeoCiklusInfo info = tabCiklusi.SelectedTab.Tag as PepeoCiklusInfo;
            if (info == null)
                return;

            if (VratiInt(cboCiklus.SelectedValue) != info.CiklusID)
                cboCiklus.SelectedValue = info.CiklusID;

            List<PepeoKorakVremena> koraci = trenutniKoraci
                .Where(x => x.CiklusID == info.CiklusID)
                .OrderBy(x => x.Redosled)
                .ToList();

            dgvVremena.AutoGenerateColumns = true;
            dgvVremena.DataSource = koraci;
            UrediGrid();

            if (info.NajavaID.HasValue)
            {
                try { cboNajava.SelectedValue = info.NajavaID.Value; } catch { }
            }

            if (koraci.Count > 0)
            {
                PepeoKorakVremena poslednji = koraci[koraci.Count - 1];
                dtpKasnjenje.Value = poslednji.StvarniKraj ?? poslednji.PlaniraniKraj;
            }
        }

        private void UrediGrid()
        {
            if (dgvVremena.Columns.Count == 0)
                return;

            string[] vidljive =
            {
                "Redosled", "Operacija", "Drzava", "PlaniraniPocetak", "PlaniraniKraj",
                "StvarniKraj", "OdstupanjeMinuta", "RazlogOdstupanja", "Napomena"
            };

            foreach (DataGridViewColumn c in dgvVremena.Columns)
                c.Visible = vidljive.Contains(c.Name);

            PostaviHeader("Redosled", "RB", 45);
            PostaviHeader("Operacija", "Operacija", 175);
            PostaviHeader("Drzava", "Država", 55);
            PostaviHeader("PlaniraniPocetak", "Planirani početak", 130);
            PostaviHeader("PlaniraniKraj", "Planirani kraj", 130);
            PostaviHeader("StvarniKraj", "Stvarni kraj", 130);
            PostaviHeader("OdstupanjeMinuta", "Odstupanje min", 90);
            PostaviHeader("RazlogOdstupanja", "Razlog odstupanja", 170);
            PostaviHeader("Napomena", "Napomena", 180);

            dgvVremena.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvVremena.MultiSelect = false;
            dgvVremena.ReadOnly = true;
            dgvVremena.AllowUserToAddRows = false;
        }

        private void PostaviHeader(string name, string text, int width)
        {
            if (dgvVremena.Columns.Contains(name))
            {
                dgvVremena.Columns[name].HeaderText = text;
                dgvVremena.Columns[name].Width = width;
            }
        }

        private void gantt_SelectionChanged(object sender, EventArgs e)
        {
            PepeoGanttChart chart = sender as PepeoGanttChart;
            if (chart == null || chart.SelectedItem == null)
                return;

            PepeoKorakVremena korak = chart.SelectedItem;

            SelektujRedUGRidu(korak.ID);
            PopuniVremeIzKoraka(korak);
        }

        private void dgvVremena_SelectionChanged(object sender, EventArgs e)
        {
            if (sinhronizujemSelekciju)
                return;

            PepeoKorakVremena k = VratiSelektovaniKorak();
            if (k == null)
                return;

            PopuniVremeIzKoraka(k);

            if (tabCiklusi.SelectedTab != null && tabCiklusi.SelectedTab.Controls.Count > 0)
            {
                PepeoGanttChart chart = tabCiklusi.SelectedTab.Controls[0] as PepeoGanttChart;
                if (chart != null)
                    chart.SelektujPoID(k.ID);
            }
        }

        private void PopuniVremeIzKoraka(PepeoKorakVremena k)
        {
            dtpKasnjenje.Value = k.StvarniKraj ?? k.PlaniraniKraj;
            txtNapomena.Text = k.Napomena ?? string.Empty;
            if (k.RazlogKasnjenjaID.HasValue)
            {
                try { cboKasnjenje.SelectedValue = k.RazlogKasnjenjaID.Value; } catch { }
            }
        }

        private void SelektujRedUGRidu(int korakID)
        {
            if (dgvVremena.Rows.Count == 0)
                return;

            sinhronizujemSelekciju = true;

            try
            {
                dgvVremena.ClearSelection();

                foreach (DataGridViewRow row in dgvVremena.Rows)
                {
                    PepeoKorakVremena k = row.DataBoundItem as PepeoKorakVremena;
                    if (k == null || k.ID != korakID)
                        continue;

                    row.Selected = true;

                    DataGridViewCell prvaVidljivaCelija = row.Cells
                        .Cast<DataGridViewCell>()
                        .Where(x => x.Visible)
                        .OrderBy(x => x.OwningColumn.DisplayIndex)
                        .FirstOrDefault();

                    if (prvaVidljivaCelija != null)
                        dgvVremena.CurrentCell = prvaVidljivaCelija;

                    if (row.Index >= 0)
                    {
                        try
                        {
                            dgvVremena.FirstDisplayedScrollingRowIndex = Math.Max(0, row.Index - 1);
                        }
                        catch
                        {
                            // Nije kriticno ako grid u tom trenutku jos nije potpuno iscrtan.
                        }
                    }

                    break;
                }
            }
            finally
            {
                sinhronizujemSelekciju = false;
            }
        }

        private PepeoKorakVremena VratiSelektovaniKorak()
        {
            if (dgvVremena.SelectedRows.Count == 0)
                return null;

            return dgvVremena.SelectedRows[0].DataBoundItem as PepeoKorakVremena;
        }

        private void btnPotvrdiKasnjenje_Click(object sender, EventArgs e)
        {
            if (prikaziZavrsene)
            {
                MessageBox.Show("Završeni ciklusi su samo za pregled.");
                return;
            }

            PepeoKorakVremena selectedGrid = VratiSelektovaniKorak();
            if (selectedGrid == null)
            {
                MessageBox.Show("Izaberite operaciju kojoj menjate stvarno vreme.");
                return;
            }

            DateTime stvarniKraj = dtpKasnjenje.Value;
            int odstupanje = Convert.ToInt32(Math.Round((stvarniKraj - selectedGrid.PlaniraniKraj).TotalMinutes));
            if (odstupanje == 0)
            {
                MessageBox.Show("Novo vreme je isto kao planirano vreme. Nema odstupanja za upis.");
                return;
            }

            int razlogID = VratiInt(cboKasnjenje.SelectedValue);
            if (razlogID <= 0)
            {
                MessageBox.Show("Izaberite razlog odstupanja. Razlog je obavezan i za kašnjenje i za raniji dolazak.");
                cboKasnjenje.Focus();
                return;
            }

            int trasaID = selectedGrid.TrasaID;

            try
            {
                List<PepeoKorakVremena> lanac = insert.UcitajOtvoreniLanacZaTrasu(trasaID);
                int indeks = lanac.FindIndex(x => x.ID == selectedGrid.ID);
                if (indeks < 0)
                    throw new Exception("Selektovani korak nije pronađen u aktivnom lancu.");

                DateTime stariPlaniraniKraj = lanac[indeks].PlaniraniKraj;
                string razlog = cboKasnjenje.Text.Trim();
                string partner = VratiPartnerIzRazloga();
                string napomena = txtNapomena.Text.Trim();

                PepeoKalkulatorVremena kalkulator = NapraviKalkulator(trasaID);
                kalkulator.PreracunajLanacPosleStvarnogVremena(
                    lanac, indeks, stvarniKraj, razlogID, razlog, partner, napomena);

                insert.SacuvajIzmenuILanac(
                    lanac,
                    indeks,
                    stariPlaniraniKraj,
                    Environment.UserName);

                OsveziPrikaz();

                string tekst = odstupanje > 0
                    ? "Kašnjenje je upisano i naredni raspored je preračunat."
                    : "Raniji dolazak je upisan i naredni raspored je preračunat.";
                MessageBox.Show(tekst);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri promeni vremena:" + Environment.NewLine + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnPoveziNajavu_Click(object sender, EventArgs e)
        {
            int ciklusID = VratiInt(cboCiklus.SelectedValue);
            int najavaID = VratiInt(cboNajava.SelectedValue);

            if (ciklusID <= 0 || najavaID <= 0)
            {
                MessageBox.Show("Izaberite ciklus i najavu.");
                return;
            }

            try
            {
                insert.PoveziNajavuSaCiklusom(ciklusID, najavaID);
                OsveziPrikaz();
                MessageBox.Show("Najava je povezana sa ciklusom C" + ciklusID + ".");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri povezivanju najave:" + Environment.NewLine + ex.Message);
            }
        }

        private void btnZavrsiCiklus_Click(object sender, EventArgs e)
        {
            if (prikaziZavrsene)
            {
                MessageBox.Show("Prebacite se na aktivne prevoze.");
                return;
            }

            int ciklusID = VratiInt(cboCiklus.SelectedValue);
            PepeoCiklusInfo ciklus = trenutniCiklusi.FirstOrDefault(x => x.CiklusID == ciklusID);
            if (ciklus == null)
            {
                MessageBox.Show("Izaberite ciklus.");
                return;
            }

            if (!string.Equals(ciklus.StatusCiklusa, "TEKUCI", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Može se završiti samo tekući ciklus.");
                return;
            }

            try
            {
                List<PepeoKorakVremena> ciklusKoraci = insert.UcitajKorakeCiklusa(ciklusID);
                PepeoKorakVremena poslednji = ciklusKoraci.OrderBy(x => x.Redosled).Last();
                DateTime stvarniKraj = dtpKasnjenje.Value;

                // Ako je poslednji korak vec ranije potvrden, ne trazimo isti razlog ponovo.
                bool koristiVecPotvrdjeno = poslednji.StvarniKraj.HasValue &&
                    Math.Abs((stvarniKraj - poslednji.StvarniKraj.Value).TotalSeconds) < 1.0;

                if (koristiVecPotvrdjeno)
                {
                    stvarniKraj = poslednji.StvarniKraj.Value;
                }
                else
                {
                    int odstupanje = Convert.ToInt32(Math.Round((stvarniKraj - poslednji.PlaniraniKraj).TotalMinutes));
                    if (odstupanje != 0)
                    {
                        int razlogID = VratiInt(cboKasnjenje.SelectedValue);
                        if (razlogID <= 0)
                        {
                            MessageBox.Show("Konačno vreme odstupa od planiranog. Izaberite razlog odstupanja.");
                            return;
                        }

                        List<PepeoKorakVremena> lanac = insert.UcitajOtvoreniLanacZaTrasu(ciklus.TrasaID);
                        int indeks = lanac.FindIndex(x => x.ID == poslednji.ID);
                        if (indeks < 0)
                            throw new Exception("Poslednji korak ciklusa nije pronadjen u aktivnom lancu.");

                        DateTime stariPlan = lanac[indeks].PlaniraniKraj;

                        PepeoKalkulatorVremena kalkulator = NapraviKalkulator(ciklus.TrasaID);
                        kalkulator.PreracunajLanacPosleStvarnogVremena(
                            lanac,
                            indeks,
                            stvarniKraj,
                            razlogID,
                            cboKasnjenje.Text.Trim(),
                            VratiPartnerIzRazloga(),
                            txtNapomena.Text.Trim());

                        insert.SacuvajIzmenuILanac(lanac, indeks, stariPlan, Environment.UserName);
                    }
                }

                insert.ZavrsiCiklus(ciklusID, stvarniKraj);

                // Svaki zavrseni ciklus automatski dodaje jedan novi na kraj lanca.
                PepeoKalkulatorVremena noviKalkulator = NapraviKalkulator(ciklus.TrasaID);
                int noviCiklusID = insert.DodajSledeciCiklus(ciklus.TrasaID, noviKalkulator);

                prikaziZavrsene = false;
                OsveziPrikaz();
                MessageBox.Show("Ciklus C" + ciklusID + " je završen. Automatski je generisan novi ciklus C" + noviCiklusID + ".");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška pri završavanju ciklusa:" + Environment.NewLine + ex.Message,
                    "Greška", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PostaviRezimKontrola()
        {
            btnAktivniPrevozi.Enabled = prikaziZavrsene;
            btnZavrseniPrevozi.Enabled = !prikaziZavrsene;

            btnPotvrdiKasnjenje.Enabled = !prikaziZavrsene;
            btnZavrsiCiklus.Enabled = !prikaziZavrsene;
            dtpKasnjenje.Enabled = !prikaziZavrsene;
            cboKasnjenje.Enabled = !prikaziZavrsene;
            txtNapomena.Enabled = !prikaziZavrsene;

            // Najava se sme povezati i naknadno, cak i sa zavrsenim ciklusom.
            btnPoveziNajavu.Enabled = trenutniCiklusi.Count > 0;
        }

        private string VratiPartnerIzRazloga()
        {
            DataRowView drv = cboKasnjenje.SelectedItem as DataRowView;
            if (drv != null && drv.Row.Table.Columns.Contains("Partner"))
                return Convert.ToString(drv["Partner"]).Trim();
            return string.Empty;
        }

        private int VratiInt(object value)
        {
            if (value == null || value == DBNull.Value || value is DataRowView)
                return 0;

            int result;
            return int.TryParse(value.ToString(), out result) ? result : 0;
        }
    }
}
