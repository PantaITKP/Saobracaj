using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Modeli;
using Saobracaj.Planiranje.Podaci;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Forme
{
    // JOBS (Zadatak 14): posao koji zamenjuje najavu. Po čuvanju se za svaki pravac pravi posao i, kad ima rute, task.
    public partial class frmOPJobs : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmOPJobs";
        public bool Pravo { get; }

        private readonly UredjivacReda poljaOpste = new UredjivacReda("Opšti podaci");
        private readonly UredjivacReda poljaRelacija = new UredjivacReda("Relacija");
        private readonly UredjivacReda poljaRoba1 = new UredjivacReda("Podaci o robi · pravac 1");
        private readonly UredjivacReda poljaRoba2 = new UredjivacReda("Podaci o robi · pravac 2");
        private readonly DataGridView tabJobs = EkranOP.NovaTabela();
        private readonly DataGridView tabTaskovi = EkranOP.NovaTabela();
        private readonly CheckBox chkSvi = new CheckBox { Text = "Svi JOBS" };
        private readonly CheckBox chkPlatilac = new CheckBox { Text = "Filtriraj po platiocu" };
        private readonly ComboBox cboTaskovi = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Label lblStanje = new Label();
        private readonly Label lblTaskovi = new Label { Text = "Izaberite posao." };

        private int? filterPlatilac;
        private bool punjenje;
        private Dictionary<int, string> naziviStanica = new Dictionary<int, string>();

        public frmOPJobs()
        {
            InitializeComponent();
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        // Kartica Jobs na ekranu Transport: lista odmah filtrirana po taskovima.
        internal frmOPJobs(FilterJobsTaskova taskovi) : this()
        {
            cboTaskovi.SelectedIndex = (int)taskovi;
        }

        private FilterJobsTaskova FilterTaskova => cboTaskovi.SelectedIndex < 0 ? FilterJobsTaskova.Svi : (FilterJobsTaskova)cboTaskovi.SelectedIndex;

        private IEnumerable<UredjivacReda> SvaPolja { get { return new[] { poljaOpste, poljaRelacija, poljaRoba1, poljaRoba2 }; } }

        private async Task UcitajSifarnike()
        {
            await EkranOP.Ucitaj(this, () => new { S = JobsRepo.Sifarnici(), N = JobsRepo.NaziviStanica() }, d =>
            {
                naziviStanica = d.N;
                poljaOpste.IzvorIzbora("PlatilacID", d.S.Partneri, "Naziv", "ID", true);
                poljaOpste.IzvorIzbora("OrganizatorID", d.S.Partneri, "Naziv", "ID", true);
                poljaOpste.IzvorIzbora("TipPrevozaID", d.S.TipoviPrevoza, "Naziv", "ID", true);
                poljaOpste.IzvorIzbora("OtpravnaID", d.S.Stanice, "Naziv", "ID", true);
                poljaOpste.IzvorIzbora("UputnaID", d.S.Stanice, "Naziv", "ID", true);
                poljaOpste.IzvorIzbora("Status", d.S.Statusi, "Naziv", "ID", false);
                poljaRelacija.IzvorIzbora("PreuzimaOdID", d.S.Prevoznici, "Naziv", "ID", true);
                poljaRelacija.IzvorIzbora("PredajeOperateruID", d.S.Partneri, "Naziv", "ID", true);
                poljaRelacija.IzvorIzbora("Stanica1ID", d.S.Stanice, "Naziv", "ID", true);
                poljaRelacija.IzvorIzbora("Stanica2ID", d.S.Stanice, "Naziv", "ID", true);
                poljaRelacija.IzvorIzbora("Stanica3ID", d.S.Stanice, "Naziv", "ID", true);
                poljaRoba1.IzvorIzbora("NHM1", d.S.NHM, "Naziv", "ID", true);
                poljaRoba2.IzvorIzbora("NHM2", d.S.NHM, "Naziv", "ID", true);
                poljaRoba1.IzvorIzbora("SerijaKola1", d.S.SerijeKola, "Naziv", "ID", true);
                poljaRoba2.IzvorIzbora("SerijaKola2", d.S.SerijeKola, "Naziv", "ID", true);
                Novi();
                if (d.S.Upozorenje != null) EkranOP.Poruka(this, d.S.Upozorenje);
                var _ = Osvezi();
            });
        }

        private async Task Osvezi(int? izaberi = null)
        {
            bool svi = chkSvi.Checked;
            int? platilac = filterPlatilac;
            FilterJobsTaskova taskovi = FilterTaskova;
            int? trenutni = izaberi ?? poljaOpste.ID;
            await EkranOP.Ucitaj(this, () => JobsRepo.Lista(svi, platilac, taskovi), t =>
            {
                punjenje = true;
                tabJobs.DataSource = t;
                punjenje = false;
                lblStanje.Text = "Poslova: " + t.Rows.Count + (svi ? " (svi)" : " (bez predatih)") + (platilac.HasValue ? " · filter po platiocu" : "")
                    + (taskovi == FilterJobsTaskova.BezTaskova ? " · bez taskova" : taskovi == FilterJobsTaskova.SaTaskovima ? " · sa taskovima" : "");
                if (!trenutni.HasValue) return;
                foreach (DataGridViewRow r in tabJobs.Rows)
                    if (Convert.ToInt32(r.Cells["Sifra"].Value) == trenutni.Value)
                    {
                        tabJobs.CurrentCell = r.Cells["Sifra"];
                        r.Selected = true;
                        return;
                    }
            });
        }

        private void IzabranPosao()
        {
            if (punjenje) return;
            DataRow r = EkranOP.Izabran(tabJobs);
            if (r == null) return;
            var _ = Prikazi(Convert.ToInt32(r["Sifra"]));
        }

        private async Task Prikazi(int id)
        {
            await EkranOP.Ucitaj(this, () => new { Red = JobsRepo.Jedan(id), Taskovi = TaskRepo.ZaNajavu(id, false), Pravci = JobsRepo.Pravci(id) }, d =>
            {
                if (d.Red == null) { EkranOP.Poruka(this, "Posao " + id + " više ne postoji."); return; }
                ObezbediSeriju(poljaRoba1, "SerijaKola1", d.Red);
                ObezbediSeriju(poljaRoba2, "SerijaKola2", d.Red);
                foreach (UredjivacReda p in SvaPolja) p.Popuni(d.Red);
                PrikaziTaskove(id, d.Taskovi, d.Pravci);
                OmoguciPravac2();
            });
        }

        private void PrikaziTaskove(int id, List<TaskOP> taskovi, DataTable pravci)
        {
            Dictionary<int, int> pravacPosla = pravci.Rows.Cast<DataRow>().ToDictionary(r => Convert.ToInt32(r["ID"]), r => Convert.ToInt32(r["RBDeonice"]));
            var t = new DataTable();
            t.Columns.Add("Pravac", typeof(int));
            foreach (string k in new[] { "Redosled", "Task", "Od", "Do", "Km", "Bruto t", "ETD", "ETA", "Status", "Lokomotive" }) t.Columns.Add(k);
            Func<int, string> naziv = s => { string n; return naziviStanica.TryGetValue(s, out n) ? n : s.ToString(); };
            foreach (var x in taskovi.SelectMany(tk => tk.Najave.Where(n => n.NajavaID == id).Select(n => new { T = tk, N = n }))
                .OrderBy(x => x.N.PosaoID.HasValue && pravacPosla.ContainsKey(x.N.PosaoID.Value) ? pravacPosla[x.N.PosaoID.Value] : 9)
                .ThenBy(x => x.N.KmOd).ThenBy(x => x.N.RBDela))
                t.Rows.Add(x.N.PosaoID.HasValue && pravacPosla.ContainsKey(x.N.PosaoID.Value) ? (object)pravacPosla[x.N.PosaoID.Value] : DBNull.Value,
                    TaskPravila.Redosled(x.N.DeoRute, x.N.DelovaBruto, x.N.RBDela), x.T.ID, naziv(x.T.OdID), naziv(x.T.DoID),
                    x.T.Km.ToString("#,##0.0"), x.T.BrutoT.ToString("#,##0.#"), Vreme(x.T.ETD), Vreme(x.T.ETA), StatusTaska.Naziv(x.T.Status), TaskRepo.OpisLokomotiva(x.T.Lokomotive));
            tabTaskovi.DataSource = t;
            lblTaskovi.Text = "Posao " + id + " · pravaca: " + pravci.Rows.Count + " · taskova: " + taskovi.Count;
        }

        private static string Vreme(DateTime? v)
        {
            return v.HasValue ? v.Value.ToString("dd.MM. HH:mm") : "";
        }

        private void Novi()
        {
            foreach (UredjivacReda p in SvaPolja) p.Isprazni();
            poljaOpste.IzaberiID("Status", 1);
            tabTaskovi.DataSource = null;
            lblTaskovi.Text = "Nov posao: taskovi se prave posle čuvanja.";
            OmoguciPravac2();
        }

        // Roba drugog pravca se unosi samo kad postoji stanica istovara (2).
        private void OmoguciPravac2()
        {
            bool ima = poljaRelacija.IzabranID("Stanica2ID").HasValue;
            foreach (string k in new[] { "NHM2", "Bruto2", "BrojKola2", "SerijaKola2", "Duzina2", "Neto2", "RIDBroj2", "OMBroj2" }) poljaRoba2.Omoguci(k, ima);
        }

        private static int? CeoIliNull(UredjivacReda u, string kolona, string natpis)
        {
            decimal? v = u.DecimalIliNull(kolona, natpis);
            if (!v.HasValue) return null;
            if (v.Value != Math.Truncate(v.Value)) throw new FormatException("Polje „" + natpis + "“ mora biti ceo broj.");
            return (int)v.Value;
        }

        // Serija koje nema u dbo.VagoniSerije (stara najava, ugašena serija) dodaje se u listu, da se čuvanjem ne izgubi.
        private static void ObezbediSeriju(UredjivacReda u, string kolona, DataRow red)
        {
            if (!red.Table.Columns.Contains(kolona) || red.IsNull(kolona)) return;
            string s = Convert.ToString(red[kolona]).Trim();
            var t = ((ComboBox)u.Polje(kolona)).DataSource as DataTable;
            if (s.Length == 0 || t == null || t.Rows.Cast<DataRow>().Any(r => string.Equals(Convert.ToString(r["ID"]).Trim(), s, StringComparison.OrdinalIgnoreCase))) return;
            t.Rows.Add(s, s);
        }

        private static string Serija(UredjivacReda u, string kolona)
        {
            string s = ((ComboBox)u.Polje(kolona)).SelectedValue as string;
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }

        private RobaPravca Roba(UredjivacReda u, int i)
        {
            return new RobaPravca
            {
                NHM = u.IzabranID("NHM" + i),
                Bruto = u.DecimalIliNull("Bruto" + i, "Bruto " + i),
                BrojKola = CeoIliNull(u, "BrojKola" + i, "Broj kola " + i),
                SerijaKola = Serija(u, "SerijaKola" + i),
                Duzina = u.DecimalIliNull("Duzina" + i, "Dužina " + i),
                Neto = u.DecimalIliNull("Neto" + i, "Neto težina " + i),
                RIDBroj = u.TekstIliNull("RIDBroj" + i),
                OMBroj = u.TekstIliNull("OMBroj" + i)
            };
        }

        private JobOP IzPolja()
        {
            return new JobOP
            {
                ID = poljaOpste.ID ?? 0,
                RowVer = poljaOpste.RowVer,
                PlatilacID = poljaOpste.IzabranID("PlatilacID"),
                OrganizatorID = poljaOpste.IzabranID("OrganizatorID"),
                TipPrevozaID = poljaOpste.IzabranID("TipPrevozaID"),
                OtpravnaID = poljaOpste.IzabranID("OtpravnaID"),
                UputnaID = poljaOpste.IzabranID("UputnaID"),
                Status = poljaOpste.IzabranID("Status") ?? 1,
                Napomena = poljaOpste.TekstIliNull("Napomena"),
                PreuzimaOdID = poljaRelacija.IzabranID("PreuzimaOdID"),
                PredajeOperateruID = poljaRelacija.IzabranID("PredajeOperateruID"),
                Stanica1ID = poljaRelacija.IzabranID("Stanica1ID") ?? 0,
                Stanica2ID = poljaRelacija.IzabranID("Stanica2ID"),
                Stanica3ID = poljaRelacija.IzabranID("Stanica3ID") ?? 0,
                ETD = poljaRelacija.DatumIliNull("ETD"),
                Roba1 = Roba(poljaRoba1, 1),
                Roba2 = Roba(poljaRoba2, 2)
            };
        }

        private async Task Sacuvaj()
        {
            JobOP j;
            try { j = IzPolja(); }
            catch (Exception ex) { EkranOP.Greska(this, ex); return; }
            if (!j.ETD.HasValue && !EkranOP.Potvrdi(this, "Posao nema ETD, pa se taskovi ne mogu napraviti. Sačuvati ipak?")) return;
            var napomene = new List<string>();
            await EkranOP.Ucitaj(this, () =>
            {
                int id = JobsRepo.Sacuvaj(j, napomene);
                napomene.AddRange(NapraviTaskove(id));
                return id;
            }, id =>
            {
                EkranOP.Poruka(this, "Posao " + id + " je sačuvan." + (napomene.Count > 0 ? "\n\n" + string.Join("\n", napomene) : ""));
                var _ = Osvezi(id);
                var __ = Prikazi(id);
            });
        }

        // Za svaki pravac bez taska pravi se task iz najbolje ponuđene rute (kao „Napravi task“ na ekranu Taskovi).
        // Pravac 2 kreće kad se završi task prvog pravca. Ruta se posle može promeniti na ekranu Taskovi.
        private static List<string> NapraviTaskove(int jobId)
        {
            var poruke = new List<string>();
            TaskOP prethodni = null;
            foreach (DataRow pr in JobsRepo.Pravci(jobId).Rows)
            {
                int posaoId = Convert.ToInt32(pr["ID"]), pravac = Convert.ToInt32(pr["RBDeonice"]);
                if (Convert.ToInt32(pr["Taskova"]) > 0)
                {
                    prethodni = TaskRepo.ZaNajavu(jobId, false).Where(t => t.Najave.Any(n => n.PosaoID == posaoId)).OrderByDescending(t => t.Do ?? DateTime.MinValue).FirstOrDefault();
                    continue;
                }
                if (pravac > 1 && prethodni != null && prethodni.Do.HasValue) JobsRepo.PomeriPrijem(posaoId, prethodni.Do.Value);
                PosaoPlan p = PlanRepo.Posao(posaoId);
                if (p == null) { poruke.Add("Pravac " + pravac + ": nema ETD, task nije napravljen."); prethodni = null; continue; }
                PlanKontekst k = PlanRepo.Ucitaj(p.Prijem.Date, p.Prijem.Date.AddDays(7));
                RezultatPosla r = PlanRacun.Izracunaj(k, p);
                List<KombinacijaTrasa> komb = frmOPPosao.KombinacijeZa(k, r);
                if (komb.Count == 0) { poruke.Add("Pravac " + pravac + " (" + p.Relacija + "): nema rute, task nije napravljen."); prethodni = null; continue; }
                DataTable tab = EkranOP.TabelaKombinacija(komb, k.NaziviStanica, true);
                string opis = Convert.ToString(tab.Rows[0]["Ruta"]) + ": " + Convert.ToString(tab.Rows[0]["Kombinacija trasa"]);
                TaskOP novi = TaskPravila.IzRute(r, komb[0], opis);
                int id = TaskRepo.Kreiraj(novi, false);
                poruke.Add("Pravac " + pravac + ": task " + id + " (" + p.Relacija + ", " + Convert.ToString(tab.Rows[0]["Ruta km"]) + " km).");
                prethodni = novi;
            }
            return poruke;
        }

        private async Task Obrisi()
        {
            int? id = poljaOpste.ID;
            byte[] rv = poljaOpste.RowVer;
            if (!id.HasValue) { EkranOP.Poruka(this, "Izaberite posao."); return; }
            if (!EkranOP.Potvrdi(this, "Obrisati posao " + id + "? Poslovi njegovih pravaca se otkazuju.")) return;
            await EkranOP.Ucitaj(this, () => { JobsRepo.Obrisi(id.Value, rv); return true; }, _ =>
            {
                Novi();
                var __ = Osvezi();
            });
        }

        private void FiltrirajPoPlatiocu()
        {
            if (chkPlatilac.Checked)
            {
                filterPlatilac = poljaOpste.IzabranID("PlatilacID");
                if (!filterPlatilac.HasValue)
                {
                    EkranOP.Poruka(this, "Izaberite platioca u opštim podacima, pa uključite filter.");
                    punjenje = true;
                    chkPlatilac.Checked = false;
                    punjenje = false;
                    return;
                }
            }
            else filterPlatilac = null;
            var _ = Osvezi();
        }

        private void PrikaziLog()
        {
            int? id = poljaOpste.ID;
            if (!id.HasValue) { EkranOP.Poruka(this, "Izaberite posao."); return; }
            DataTable t;
            try { t = JobsRepo.LogZa(id.Value); }
            catch (Exception ex) { EkranOP.Greska(this, ex); return; }
            using (var f = new Form { Text = "LOG · posao " + id, StartPosition = FormStartPosition.CenterParent, ClientSize = new System.Drawing.Size(Saobracaj.Tema.Theme.Dp(900), Saobracaj.Tema.Theme.Dp(420)), MinimizeBox = false })
            {
                DataGridView g = EkranOP.NovaTabela();
                g.Dock = DockStyle.Fill;
                g.DataBindingComplete += (s, e) =>
                {
                    g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
                    if (g.Columns.Contains("Opis")) { g.Columns["Opis"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; g.Columns["Opis"].DefaultCellStyle.WrapMode = DataGridViewTriState.True; }
                    g.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
                };
                f.Controls.Add(g);
                f.Shown += (s, e) => g.DataSource = t;
                f.ShowDialog(this);
            }
        }

        // Privremeno: najave od datuma prelaze u JOBS sa istim brojem.
        private async Task PrenesiNajave()
        {
            DateTime od;
            using (var dlg = new MaliDijalog("Prenos najava u JOBS"))
            {
                DateTimePicker d = dlg.Datum("Najave unete ili predviđene od", DateTime.Today.AddDays(-30));
                if (!dlg.Prikazi(this)) return;
                od = d.Value.Date;
            }
            if (!EkranOP.Potvrdi(this, "Preneti u JOBS najave unete ili predviđene od " + od.ToString("dd.MM.yyyy") + "?\n\nNajava zadržava svoj broj i ima jedan pravac (otpravna → uputna). Već prenete se preskaču.")) return;
            await EkranOP.Ucitaj(this, () => JobsRepo.PrenesiNajave(od), n =>
            {
                EkranOP.Poruka(this, "Preneto najava: " + n[0] + "." + (n[1] > 0 ? "\nPreskočeno (bez dve različite stanice): " + n[1] + "." : "") +
                    "\n\nTaskovi za prenete najave prave se na ekranu Taskovi (Napravi task iz izabrane rute).");
                var _ = Osvezi();
            });
        }

        private void OtvoriTaskove()
        {
            var f = new frmOPTaskovi();
            if (f.Pravo) f.Show(); else f.Dispose();
        }
    }
}
