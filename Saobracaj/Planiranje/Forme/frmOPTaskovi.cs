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
    // Najave (jobs) → izbor rute → task; task se deli po ruti ili brutu, spaja, dobija lokomotive i prolazi statuse.
    public partial class frmOPTaskovi : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmOPTaskovi";
        public bool Pravo { get; }

        private readonly DateTimePicker dtOd = new DateTimePicker { Format = DateTimePickerFormat.Short };
        private readonly DateTimePicker dtDo = new DateTimePicker { Format = DateTimePickerFormat.Short };
        private readonly CheckBox chkSvi = new CheckBox { Text = "Svi taskovi perioda" };
        private readonly CheckBox chkStorno = new CheckBox { Text = "Prikaži stornirane" };
        private readonly Label lblStanje = new Label();
        private readonly DataGridView tabNajave = EkranOP.NovaTabela("PosaoID", "NajavaID");
        private readonly DataGridView tabRute = EkranOP.NovaTabela("RB");
        private readonly Label lblRute = new Label { Text = "Izaberite najavu." };
        private readonly DataGridView tabTaskovi = EkranOP.NovaTabela();
        private readonly DataGridView tabLokomotive = EkranOP.NovaTabela("LokomotivaID");
        private readonly DataGridView tabOsoblje = EkranOP.NovaTabela("ID");
        private readonly DataGridView tabStanice = EkranOP.NovaTabela();
        private readonly Label lblTask = new Label { Text = "Izaberite task." };

        private PlanKontekst kontekst;
        private List<RezultatPosla> poslovi = new List<RezultatPosla>();
        private List<TaskOP> taskovi = new List<TaskOP>();
        private DataTable aktivneLokomotive;
        private readonly Dictionary<int, RuteNajave> rute = new Dictionary<int, RuteNajave>();
        private RuteNajave prikazaneRute;
        private int? izabranaNajava;
        // Pravac JOBS-a (posao) izabran u listi najava.
        private int? izabraniPosao;
        private bool punjenje;

        private sealed class RuteNajave
        {
            internal int PosaoID;
            internal int NajavaID;
            internal string Najava;
            internal RezultatPosla Racun;
            internal List<KombinacijaTrasa> Kombinacije;
            internal DataTable Tabela;
        }

        private sealed class PodaciEkrana
        {
            internal PlanKontekst Kontekst;
            internal List<RezultatPosla> Poslovi;
            internal List<TaskOP> Taskovi;
            internal DataTable Lokomotive;
        }

        public frmOPTaskovi()
        {
            InitializeComponent();
            dtOd.Value = DateTime.Today;
            dtDo.Value = DateTime.Today.AddDays(7);
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        // Kartica Taskovi na ekranu Transport: svi taskovi perioda, odmah filtrirani.
        // Za otvorene i taskove koji kasne period se pri otvaranju širi na sve takve taskove.
        internal frmOPTaskovi(FilterTaskova filter) : this()
        {
            chkSvi.Checked = true;
            cboFilter.SelectedIndex = (int)filter;
            prosiriPeriod = filter == FilterTaskova.Otvoreni || filter == FilterTaskova.Kasne;
        }

        private readonly ComboBox cboFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private bool prosiriPeriod;

        private FilterTaskova Filter => cboFilter.SelectedIndex < 0 ? FilterTaskova.Svi : (FilterTaskova)cboFilter.SelectedIndex;

        private async Task PrvoOsvezavanje()
        {
            if (prosiriPeriod)
            {
                bool kasne = Filter == FilterTaskova.Kasne;
                Tuple<DateTime?, DateTime?> p = null;
                try { p = await Task.Run(() => TaskRepo.PeriodOtvorenih(kasne)); }
                catch (Exception) { }
                DateTime danas = DateTime.Today;
                if (p != null && p.Item1.HasValue && p.Item1.Value < danas) dtOd.Value = p.Item1.Value;
                if (p != null && p.Item2.HasValue && !kasne && p.Item2.Value > danas) dtDo.Value = p.Item2.Value;
                if (kasne) dtDo.Value = danas;
            }
            else if (Filter == FilterTaskova.Danas)
                dtDo.Value = DateTime.Today;
            await Osvezi();
        }

        private DateTime PeriodOd => dtOd.Value.Date;
        private DateTime PeriodDo => dtDo.Value.Date.AddDays(1);

        private async Task Osvezi(bool saKontekstom = true)
        {
            DateTime od = PeriodOd, doDatuma = PeriodDo;
            if (doDatuma <= od) { EkranOP.Poruka(this, "Datum Do mora biti posle datuma Od."); return; }
            bool storno = chkStorno.Checked;
            PlanKontekst stari = saKontekstom ? null : kontekst;
            DataTable lok = aktivneLokomotive;
            await EkranOP.Ucitaj(this, () =>
            {
                // Isti poslovi i isti račun (km, bruto, BTTO, telegrami) kao na nedeljnom planu.
                PlanKontekst k = stari ?? PlanRepo.Ucitaj(od, doDatuma);
                return new PodaciEkrana
                {
                    Kontekst = k,
                    Poslovi = PlanRepo.Poslovi(od, doDatuma).Select(p => PlanRacun.Izracunaj(k, p)).ToList(),
                    Taskovi = TaskRepo.ZaPeriod(od, doDatuma, storno),
                    Lokomotive = lok ?? LokomotiveRepo.Lokomotive(true)
                };
            }, d =>
            {
                if (saKontekstom) rute.Clear();
                kontekst = d.Kontekst;
                poslovi = d.Poslovi;
                taskovi = d.Taskovi;
                aktivneLokomotive = d.Lokomotive;
                PrikaziNajave();
                PrikaziTaskove();
            });
        }

        // Posle promene taska čitaju se samo taskovi; poslovi (km, bruto, telegrami) se tada ne menjaju, a njihov račun je spor.
        // Kolona „Taskovi“ u najavama se računa iz taskova, pa se i ona osvežava.
        private async Task OsveziTaskove()
        {
            DateTime od = PeriodOd, doDatuma = PeriodDo;
            bool storno = chkStorno.Checked;
            lblStanje.Text = "Osvežavam taskove...";
            await EkranOP.Ucitaj(this, () => TaskRepo.ZaPeriod(od, doDatuma, storno), t =>
            {
                taskovi = t;
                PrikaziNajave();
                PrikaziTaskove();
            });
        }

        // Task koji se bira posle sledećeg prikaza (novi delovi posle splita ili spajanja).
        private int? izaberiPosle;
        private readonly List<Control> trakeAkcija = new List<Control>();

        private const string Zastavica = "⚑";

        // Kolone kao „Bez lokomotive“ na nedeljnom planu, uz taskove najave.
        private void PrikaziNajave()
        {
            int? najava = izabranaNajava;
            var t = new DataTable();
            t.Columns.Add("PosaoID", typeof(int));
            t.Columns.Add("NajavaID", typeof(int));
            foreach (string k in new[] { "Najava", "Telegram", "Relacija", "Prijem", "Km", "Bruto (t)", "BTTO", "Broj kola", "Tip kola", "Taskovi" })
                t.Columns.Add(k);
            foreach (RezultatPosla r in poslovi)
            {
                PosaoPlan p = r.Posao;
                // Red je pravac JOBS-a (posao): taskovi i oznaka su po pravcu („1000005/2“).
                List<TaskOP> njeni = taskovi.Where(x => x.Status != StatusTaska.Storniran && x.Najave.Any(n => n.PosaoID == p.ID)).ToList();
                t.Rows.Add(p.ID, p.NajavaID, p.BrojNajave + (string.IsNullOrEmpty(p.StatusNajave) ? "" : " / " + p.StatusNajave),
                    r.Telegrami.Count == 0 ? "" : Zastavica + " " + r.Telegrami.Count, p.Relacija, p.Prijem.ToString("dd.MM. HH:mm"),
                    r.Ruta.Count == 0 ? "nema rute" : r.Km.ToString("#,##0.0"), r.Bruto.ToString("#,##0.#"), r.Ruta.Count == 0 ? "–" : r.Btto.ToString("#,##0"),
                    p.BrojKola?.ToString() ?? "–", p.TipKola ?? "–", OpisTaskova(njeni));
            }
            punjenje = true;
            tabNajave.DataSource = t;
            punjenje = false;
            lblStanje.Text = "Najava: " + poslovi.Count + " · bez taskova: " + t.Rows.Cast<DataRow>().Count(x => Convert.ToString(x["Taskovi"]).Length == 0)
                + " · aktivnih taskova: " + taskovi.Count(x => x.Status != StatusTaska.Storniran)
                + " · lansiranih: " + taskovi.Count(x => x.Status == StatusTaska.Lansiran);
            if (najava.HasValue)
                foreach (DataGridViewRow r in tabNajave.Rows)
                    if (Convert.ToInt32(r.Cells["NajavaID"].Value) == najava.Value && (!izabraniPosao.HasValue || Convert.ToInt32(r.Cells["PosaoID"].Value) == izabraniPosao.Value))
                    {
                        // Bez međukoraka „ništa nije izabrano“, koji bi ispraznio taskove i izgubio izabran task.
                        punjenje = true;
                        tabNajave.ClearSelection();
                        tabNajave.CurrentCell = r.Cells.Cast<DataGridViewCell>().First(x => x.Visible);
                        r.Selected = true;
                        punjenje = false;
                        IzabranaNajavaPromenjena();
                        return;
                    }
            IzabranaNajavaPromenjena();
        }

        // „3 · 1 lans.“: broj aktivnih taskova najave, i koliko je lansirano ili završeno.
        private static string OpisTaskova(List<TaskOP> lista)
        {
            if (lista.Count == 0) return "";
            int lans = lista.Count(x => x.Status == StatusTaska.Lansiran), zav = lista.Count(x => x.Status == StatusTaska.Zavrsen);
            return lista.Count + (lans > 0 ? " · " + lans + " lans." : "") + (zav > 0 ? " · " + zav + " zav." : "");
        }

        private string OpisTelegramaNajave(int posaoId)
        {
            RezultatPosla r = Racun(posaoId);
            if (r == null || r.Telegrami.Count == 0) return null;
            return string.Join("\n", r.Telegrami.Select(x => "Telegram " + x.Broj + " (" + (x.Vrsta == "ZATVOR" ? "zatvor" : "upozorenje") + "): "
                + Naziv(x.OdID ?? 0) + " – " + Naziv(x.DoID ?? 0)));
        }

        private RezultatPosla Racun(int posaoId)
        {
            return poslovi.FirstOrDefault(r => r.Posao.ID == posaoId);
        }

        private void IzabranaNajavaPromenjena()
        {
            if (punjenje) return;
            DataRow r = EkranOP.Izabran(tabNajave);
            int? najava = r == null ? (int?)null : Convert.ToInt32(r["NajavaID"]);
            bool promena = najava != izabranaNajava;
            izabranaNajava = najava;
            izabraniPosao = r == null ? (int?)null : Convert.ToInt32(r["PosaoID"]);
            if (!chkSvi.Checked && promena) PrikaziTaskove();
            if (r == null) { tabRute.DataSource = PraznaTabelaRuta(); prikazaneRute = null; lblRute.Text = "Izaberite najavu."; return; }
            RezultatPosla racun = Racun(izabraniPosao.Value);
            var _ = UcitajRute(izabraniPosao.Value, najava.Value, racun != null ? racun.Posao.BrojNajave : Convert.ToString(r["NajavaID"]));
        }

        // Rute kojima se posao može završiti: isti račun kao ponuda trasa na ekranu Posao.
        private async Task UcitajRute(int posaoId, int najavaId, string oznaka)
        {
            RuteNajave gotove;
            if (rute.TryGetValue(posaoId, out gotove)) { PrikaziRute(gotove); return; }
            tabRute.DataSource = PraznaTabelaRuta();
            lblRute.Text = "Računam rute za najavu " + oznaka + "...";
            PlanKontekst k = kontekst;
            RezultatPosla racun = Racun(posaoId);
            if (racun == null) return;
            await EkranOP.Ucitaj(this, () =>
            {
                List<KombinacijaTrasa> komb = frmOPPosao.KombinacijeZa(k, racun);
                return new RuteNajave { PosaoID = posaoId, NajavaID = najavaId, Najava = oznaka, Racun = racun, Kombinacije = komb, Tabela = EkranOP.TabelaKombinacija(komb, k.NaziviStanica, true) };
            }, d =>
            {
                rute[posaoId] = d;
                if (izabranaNajava == najavaId) PrikaziRute(d);
            });
        }

        // Tabela ruta se nikad ne prazni sa DataSource = null niti dobija istu tabelu dvaput:
        // DataGridView tada pada pri crtanju kad se poveća (NullReferenceException u GetEditedFormattedValue).
        private DataTable PraznaTabelaRuta()
        {
            return EkranOP.TabelaKombinacija(new List<KombinacijaTrasa>(), kontekst?.NaziviStanica ?? new Dictionary<int, string>(), true);
        }

        private void PrikaziRute(RuteNajave d)
        {
            prikazaneRute = d;
            tabRute.DataSource = d.Tabela.Copy();
            PosaoPlan p = d.Racun.Posao;
            // Isti tekst kao „Ponuda trasa“ na ekranu Posao.
            lblRute.Text = d.Kombinacije.Count == 0
                ? "Najava " + d.Najava + " · " + p.Relacija + ": nema rute."
                : "Najava " + d.Najava + " · " + p.Relacija + ". Trase koje dozvoljavaju bruto " + Bro(d.Racun.Bruto, " t") + (p.DuzinaM.HasValue ? " i dužinu " + Bro(p.DuzinaM, " m") : "")
                  + ". Trase nemaju vremena, pa presedanje nije provereno.";
        }

        // Izabrana ruta u ponudi trasa (dugme iznad tabele ili dvoklik).
        private void NapraviIzIzabraneRute()
        {
            DataRow r = EkranOP.Izabran(tabRute);
            if (r == null) { EkranOP.Poruka(this, "Izaberite rutu u ponudi trasa."); return; }
            var _ = NapraviTask(Convert.ToInt32(r["RB"]));
        }

        private static string Bro(decimal? v, string jedinica = "")
        {
            return v.HasValue ? v.Value.ToString("#,##0.##", CultureInfo.GetCultureInfo("sr-Latn-RS")) + jedinica : "–";
        }

        private string Naziv(int id)
        {
            return kontekst == null ? id.ToString() : kontekst.Naziv(id);
        }

        // Red taska po najavi: spojen task sa dve najave ima red pod svakom od njih.
        private void PrikaziTaskove()
        {
            int? izabran = izaberiPosle ?? IzabraniTaskovi().Select(t => (int?)t.ID).FirstOrDefault();
            izaberiPosle = null;
            var redNajave = new Dictionary<int, int>();
            for (int i = 0; i < poslovi.Count; i++)
                if (!redNajave.ContainsKey(poslovi[i].Posao.NajavaID)) redNajave[poslovi[i].Posao.NajavaID] = i;
            FilterTaskova filter = Filter;
            DateTime danas = DateTime.Today;
            var redovi = taskovi.Where(t => KarticeTransporta.Prolazi(t, filter, danas)).SelectMany(t => t.Najave.Select(n => new { T = t, N = n }))
                .Where(x => chkSvi.Checked || x.N.NajavaID == izabranaNajava)
                .OrderBy(x => redNajave.ContainsKey(x.N.NajavaID) ? redNajave[x.N.NajavaID] : int.MaxValue)
                .ThenBy(x => x.N.NajavaID)
                .ThenBy(x => x.N.PosaoID ?? 0)
                .ThenBy(x => x.T.Status == StatusTaska.Storniran ? 1 : 0)
                .ThenBy(x => x.N.KmOd).ThenBy(x => x.N.RBDela).ThenBy(x => x.T.ID).ToList();

            var tab = new DataTable();
            tab.Columns.Add("Redosled");
            tab.Columns.Add("Task", typeof(int));
            foreach (string k in new[] { "Najava", "Od", "Do", "Km", "Bruto t", "EA", "Dužina m", "ETD", "ATD", "ETA", "ATA", "Trajanje", "Zauzeta do", "Status", "Lokomotive", "Nastao", "Upozorenje", "Ruta" })
                tab.Columns.Add(k);
            foreach (var x in redovi)
            {
                TaskOP t = x.T;
                tab.Rows.Add(TaskPravila.Redosled(x.N.DeoRute, x.N.DelovaBruto, x.N.RBDela), t.ID,
                    t.Najave.Count > 1 ? x.N.BrojNajave + " (+" + string.Join(", ", t.Najave.Where(n => n != x.N).Select(n => n.BrojNajave)) + ")" : x.N.BrojNajave,
                    Naziv(t.OdID), Naziv(t.DoID), Bro(t.Km), Bro(t.BrutoT), t.BrojKola, Bro(t.DuzinaM), Vreme(t.ETD), Vreme(t.ATD), Vreme(t.ETA), Vreme(t.ATA), TaskPravila.TekstTrajanja(t.TrajanjeMin), Vreme(t.Do),
                    StatusTaska.Naziv(t.Status), TaskRepo.OpisLokomotiva(t.Lokomotive), NastanakTaska.Naziv(t.Nastanak), TaskPravila.UpozorenjeVremena(t, taskovi), t.OpisRute);
            }
            tabTaskovi.DataSource = tab;
            if (izabran.HasValue)
                foreach (DataGridViewRow r in tabTaskovi.Rows)
                    if (Convert.ToInt32(r.Cells["Task"].Value) == izabran.Value)
                    {
                        // Posle povezivanja je izabran i prvi red; bez brisanja bi ostala dva izabrana, pa bi sledeće osvežavanje uzelo prvi.
                        tabTaskovi.ClearSelection();
                        tabTaskovi.CurrentCell = r.Cells["Redosled"];
                        r.Selected = true;
                        break;
                    }
            PrikaziDetaljeTaska();
        }

        private static string Vreme(DateTime? v)
        {
            return v.HasValue ? v.Value.ToString("dd.MM.yy HH:mm") : "";
        }

        private List<TaskOP> IzabraniTaskovi()
        {
            var ids = new List<int>();
            foreach (DataGridViewRow r in tabTaskovi.SelectedRows)
            {
                int id = Convert.ToInt32(r.Cells["Task"].Value);
                if (!ids.Contains(id)) ids.Add(id);
            }
            if (ids.Count == 0 && tabTaskovi.CurrentRow != null && tabTaskovi.Columns.Contains("Task"))
                ids.Add(Convert.ToInt32(tabTaskovi.CurrentRow.Cells["Task"].Value));
            return ids.Select(id => taskovi.FirstOrDefault(t => t.ID == id)).Where(t => t != null).ToList();
        }

        private TaskOP JedanTask()
        {
            List<TaskOP> lista = IzabraniTaskovi();
            if (lista.Count != 1) { EkranOP.Poruka(this, "Izaberite jedan task."); return null; }
            return lista[0];
        }

        private void PrikaziDetaljeTaska()
        {
            List<TaskOP> lista = IzabraniTaskovi();
            TaskOP t = lista.Count == 1 ? lista[0] : null;
            var lok = new DataTable();
            lok.Columns.Add("LokomotivaID", typeof(int));
            lok.Columns.Add("Lokomotiva");
            lok.Columns.Add("Uloga");
            var st = new DataTable();
            foreach (string k in new[] { "RB", "Mesto", "Km", "Trasa" }) st.Columns.Add(k);
            if (t != null)
            {
                foreach (TaskLokomotiva l in t.Lokomotive)
                    lok.Rows.Add(l.LokomotivaID, l.Broj, l.Uloga == TaskLokomotiva.Vucna ? "Vučna" : "Potisak");
                for (int i = 0; i < t.Stanice.Count; i++)
                {
                    TaskStanica s = t.Stanice[i];
                    TrasaOP trasa = s.TrasaID.HasValue ? kontekst?.Trase.FirstOrDefault(x => x.ID == s.TrasaID.Value) : null;
                    st.Rows.Add(i + 1, Naziv(s.StanicaID), Bro(s.Km), i == 0 ? "" : trasa != null ? trasa.BrojVoza : s.TrasaID.HasValue ? "#" + s.TrasaID : "bez trase");
                }
                lblTask.Text = "Task " + t.ID + " · " + StatusTaska.Naziv(t.Status) + " · " + Naziv(t.OdID) + " – " + Naziv(t.DoID) + " · " + Bro(t.Km, " km")
                    + " · " + Bro(t.BrutoT, " t") + " · " + t.BrojKola + " EA · najave: " + t.OznakaNajava;
            }
            else lblTask.Text = lista.Count > 1 ? "Izabrano taskova: " + lista.Count + " (za spajanje)." : "Izaberite task.";
            tabLokomotive.DataSource = lok;
            var os = new DataTable();
            os.Columns.Add("ID", typeof(int));
            foreach (string k in new[] { "Radno mesto", "Mesto", "Od", "Do", "Osoba", "Zadatak", "Napomena" }) os.Columns.Add(k);
            if (t != null)
                foreach (TaskOsoba m in t.Osoblje)
                    os.Rows.Add(m.ID, TaskOsoba.NazivUloge(m.Uloga), Naziv(m.StanicaID), m.Od.ToString("dd.MM. HH:mm"), m.Do.ToString("dd.MM. HH:mm"),
                        m.Osoba ?? "–", m.StatusZadatka == null ? "nije izabrano" : m.StatusZadatka.ToLowerInvariant(), m.Napomena);
            tabOsoblje.DataSource = os;
            tabStanice.DataSource = st;
        }

        // Akcija nad taskom: upis van UI niti, pa novo čitanje taskova. Dugmad akcija su zaključana dok traju upis
        // i osvežavanje, da se sledeća akcija ne bi izvršila nad starim stanjem.
        private Task Radnja(Action upis, string poruka = null)
        {
            return Radnja(() => { upis(); return (int?)null; }, poruka);
        }

        // upis vraća task koji se bira posle osvežavanja (prvi novi deo), ili null da ostane isti.
        private async Task Radnja(Func<int?> upis, string poruka = null)
        {
            ZakljucajAkcije(true);
            try
            {
                bool upisano = false;
                await EkranOP.Ucitaj(this, upis, id =>
                {
                    upisano = true;
                    if (id.HasValue) izaberiPosle = id;
                    if (poruka != null) EkranOP.Poruka(this, poruka);
                });
                if (upisano && !IsDisposed) await OsveziTaskove();
            }
            finally
            {
                if (!IsDisposed) ZakljucajAkcije(false);
            }
        }

        private void ZakljucajAkcije(bool zakljucaj)
        {
            foreach (Control c in trakeAkcija) c.Enabled = !zakljucaj;
        }

        private async Task NapraviTask(int rb)
        {
            RuteNajave d = prikazaneRute;
            if (d == null || rb < 0 || rb >= d.Kombinacije.Count) return;
            List<TaskOP> postojeci = taskovi.Where(t => t.Status != StatusTaska.Storniran && t.Najave.Any(n => n.PosaoID == d.PosaoID)).ToList();
            if (postojeci.Any(t => !t.Otvoren))
            {
                EkranOP.Poruka(this, "Najava " + d.Najava + " ima lansiran ili završen task (" + string.Join(", ", postojeci.Where(t => !t.Otvoren).Select(t => t.ID)) + "), pa se nova ruta ne može izabrati.");
                return;
            }
            if (postojeci.Count > 0 && !EkranOP.Potvrdi(this, "Najava " + d.Najava + " već ima taskove " + string.Join(", ", postojeci.Select(t => t.ID))
                + ".\n\nStornirati ih i napraviti novi task iz izabrane rute?")) return;
            DataRow red = d.Tabela.Rows[rb];
            string opis = Convert.ToString(red["Ruta"]) + ": " + Convert.ToString(red["Kombinacija trasa"]);
            TaskOP novi = TaskPravila.IzRute(d.Racun, d.Kombinacije[rb], opis);
            bool storno = postojeci.Count > 0;
            await Radnja(() => (int?)TaskRepo.Kreiraj(novi, storno), null);
        }

        private async Task SplitRute()
        {
            TaskOP t = JedanTask();
            if (t == null) return;
            if (!t.Otvoren || t.Stanice.Count < 3) { EkranOP.Poruka(this, !t.Otvoren ? "Deli se samo otvoren task." : "Ruta taska nema stanicu između početne i krajnje."); return; }
            List<int> mesta;
            using (var dlg = new DijalogSplitRute(t, Naziv, TaskPravila.PromeneTrase(t)))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                mesta = dlg.Izabrana;
            }
            List<TaskOP> delovi;
            try { delovi = TaskPravila.SplitRute(t, mesta); }
            catch (Exception ex) { EkranOP.Greska(this, ex); return; }
            if (!EkranOP.Potvrdi(this, "Task " + t.ID + " se stornira, a nastaju " + delovi.Count + " taska:\n\n"
                + string.Join("\n", delovi.Select((x, i) => (i + 1) + ". " + Naziv(x.OdID) + " – " + Naziv(x.DoID) + " (" + Bro(x.Km, " km") + ", ETD " + Vreme(x.ETD) + ")")))) return;
            await Radnja(() => (int?)TaskRepo.Zameni(new[] { t }, delovi, NastanakTaska.SplitRute).FirstOrDefault());
        }

        private async Task SplitBruto()
        {
            TaskOP t = JedanTask();
            if (t == null) return;
            var dlg = new MaliDijalog("Split bruto · task " + t.ID);
            TextBox bruto = dlg.Tekst("Bruto prvog dela, t (ukupno " + Bro(t.BrutoT) + " t)");
            TextBox kola = dlg.Tekst("Broj kola prvog dela, EA (ukupno " + t.BrojKola + ")");
            DateTime? predlogEtd, predlogEta;
            TaskPravila.PredlogVremenaDrugogDela(t, out predlogEtd, out predlogEta);
            DateTimePicker etd = DatumPrazno(dlg, "ETD drugog dela (prvi zadržava " + Vreme(t.ETD) + ")", predlogEtd);
            DateTimePicker eta = DatumPrazno(dlg, "ETA drugog dela", predlogEta);
            ComboBox lok = t.Lokomotive.Count == 0 ? null
                : dlg.Izbor("Drugi deo zadržava lokomotive " + TaskRepo.OpisLokomotiva(t.Lokomotive), "Da", "Ne");
            using (dlg)
            {
                if (!dlg.Prikazi(this)) return;
            }
            DateTime? vEtd = Vrednost(etd), vEta = Vrednost(eta);
            bool lokDrugom = lok != null && lok.SelectedIndex == 0;
            decimal b;
            int k;
            if (!decimal.TryParse(bruto.Text.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out b)) { EkranOP.Poruka(this, "Bruto mora biti broj."); return; }
            if (!int.TryParse(kola.Text.Trim().Length == 0 ? "0" : kola.Text.Trim(), out k)) { EkranOP.Poruka(this, "Broj kola mora biti ceo broj."); return; }
            List<TaskOP> delovi;
            try { delovi = TaskPravila.SplitBruto(t, b, k, vEtd, vEta, lokDrugom); }
            catch (Exception ex) { EkranOP.Greska(this, ex); return; }
            Func<TaskOP, string> opis = x => Bro(x.BrutoT, " t") + ", " + x.BrojKola + " EA, ETD " + Vreme(x.ETD) + ", ETA " + Vreme(x.ETA)
                + (x.Lokomotive.Count > 0 ? ", " + TaskRepo.OpisLokomotiva(x.Lokomotive) : ", bez lokomotiva");
            if (!EkranOP.Potvrdi(this, "Task " + t.ID + " se stornira, a nastaju dva taska iste rute:\n\n1. " + opis(delovi[0]) + "\n2. " + opis(delovi[1]))) return;
            await Radnja(() => (int?)TaskRepo.Zameni(new[] { t }, delovi, NastanakTaska.SplitBruto).FirstOrDefault());
        }

        private async Task Spoji()
        {
            List<TaskOP> lista = IzabraniTaskovi();
            TaskOP spojen;
            try { spojen = TaskPravila.Spoji(lista); }
            catch (Exception ex) { EkranOP.Greska(this, ex); return; }
            if (!EkranOP.Potvrdi(this, "Taskovi " + string.Join(", ", lista.Select(t => t.ID)) + " se storniraju, a nastaje jedan task:\n\n"
                + Naziv(spojen.OdID) + " – " + Naziv(spojen.DoID) + ", " + Bro(spojen.Km, " km") + ", " + Bro(spojen.BrutoT, " t") + ", " + spojen.BrojKola + " EA"
                + "\nNajave: " + spojen.OznakaNajava + (spojen.Lokomotive.Count > 0 ? "\nLokomotive: " + TaskRepo.OpisLokomotiva(spojen.Lokomotive) : ""))) return;
            await Radnja(() => (int?)TaskRepo.Zameni(lista, new[] { spojen }, NastanakTaska.Spajanje).FirstOrDefault());
        }

        // Vučna ili potisak iz aktivnih lokomotiva; nova vučna prebacuje dosadašnju u potisak.
        private async Task DodajLokomotivu(string uloga)
        {
            TaskOP t = JedanTask();
            if (t == null) return;
            if (!t.Otvoren) { EkranOP.Poruka(this, "Lokomotive se menjaju na otvorenom tasku."); return; }
            if (kontekst == null) return;
            if (!t.Od.HasValue) { EkranOP.Poruka(this, "Task nema ETD. Upišite vremena (dugme Vremena), pa dodelite lokomotivu."); return; }
            // Ponuda po udaljenosti, vremenu dolaska, vuči i zauzetosti na drugim taskovima.
            LokomotivaOP izabrana;
            using (var dlg = new DijalogLokomotiveTaska(kontekst, t, taskovi, uloga, Naziv))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK || dlg.Izabrana == null) return;
                izabrana = dlg.Izabrana;
                uloga = dlg.Uloga;
            }
            List<TaskLokomotiva> nove = t.Lokomotive.Select(l => new TaskLokomotiva
            {
                LokomotivaID = l.LokomotivaID,
                Broj = l.Broj,
                Uloga = uloga == TaskLokomotiva.Vucna ? TaskLokomotiva.Potisak : l.Uloga
            }).ToList();
            nove.Add(new TaskLokomotiva { LokomotivaID = izabrana.ID, Broj = izabrana.Broj, Uloga = uloga == TaskLokomotiva.Potisak && nove.Count == 0 ? TaskLokomotiva.Vucna : uloga });
            await Radnja(() => TaskRepo.SacuvajLokomotive(t, nove));
        }

        private int? IzabranaLokomotiva()
        {
            DataRow r = EkranOP.Izabran(tabLokomotive);
            return r == null ? (int?)null : Convert.ToInt32(r["LokomotivaID"]);
        }

        private async Task UkloniLokomotivu()
        {
            TaskOP t = JedanTask();
            int? lok = IzabranaLokomotiva();
            if (t == null) return;
            if (!lok.HasValue) { EkranOP.Poruka(this, "Izaberite lokomotivu taska."); return; }
            if (!t.Otvoren) { EkranOP.Poruka(this, "Lokomotive se menjaju na otvorenom tasku."); return; }
            List<TaskLokomotiva> nove = t.Lokomotive.Where(l => l.LokomotivaID != lok.Value)
                .Select(l => new TaskLokomotiva { LokomotivaID = l.LokomotivaID, Broj = l.Broj, Uloga = l.Uloga }).ToList();
            // Kad se ukloni vučna, prva potisna postaje vučna.
            if (nove.Count > 0 && nove.All(l => l.Uloga != TaskLokomotiva.Vucna)) nove[0].Uloga = TaskLokomotiva.Vucna;
            await Radnja(() => TaskRepo.SacuvajLokomotive(t, nove));
        }

        private async Task PostaviVucnu()
        {
            TaskOP t = JedanTask();
            int? lok = IzabranaLokomotiva();
            if (t == null) return;
            if (!lok.HasValue) { EkranOP.Poruka(this, "Izaberite lokomotivu taska."); return; }
            if (!t.Otvoren) { EkranOP.Poruka(this, "Lokomotive se menjaju na otvorenom tasku."); return; }
            List<TaskLokomotiva> nove = t.Lokomotive.Select(l => new TaskLokomotiva
            {
                LokomotivaID = l.LokomotivaID,
                Broj = l.Broj,
                Uloga = l.LokomotivaID == lok.Value ? TaskLokomotiva.Vucna : TaskLokomotiva.Potisak
            }).ToList();
            await Radnja(() => TaskRepo.SacuvajLokomotive(t, nove));
        }

        private static DateTimePicker DatumPrazno(MaliDijalog dlg, string natpis, DateTime? vrednost)
        {
            DateTimePicker d = dlg.Datum(natpis, vrednost ?? DateTime.Now, true);
            d.ShowCheckBox = true;
            d.Checked = vrednost.HasValue;
            return d;
        }

        private static DateTime? Vrednost(DateTimePicker d)
        {
            if (!d.Checked) return null;
            DateTime v = d.Value;
            return new DateTime(v.Year, v.Month, v.Day, v.Hour, v.Minute, 0);
        }

        private async Task Vremena()
        {
            TaskOP t = JedanTask();
            if (t == null) return;
            if (t.Status == StatusTaska.Storniran) { EkranOP.Poruka(this, "Storniran task se ne menja."); return; }
            var dlg = new MaliDijalog("Vremena · task " + t.ID);
            DateTimePicker etd = DatumPrazno(dlg, "ETD (planirani polazak)", t.ETD), atd = DatumPrazno(dlg, "ATD (stvarni polazak)", t.ATD),
                eta = DatumPrazno(dlg, "ETA (planirani dolazak)", t.ETA), ata = DatumPrazno(dlg, "ATA (stvarni dolazak)", t.ATA);
            using (dlg)
            {
                if (!dlg.Prikazi(this)) return;
            }
            DateTime? vEtd = Vrednost(etd), vAtd = Vrednost(atd), vEta = Vrednost(eta), vAta = Vrednost(ata);
            await Radnja(() => TaskRepo.SacuvajVremena(t, vEtd, vAtd, vEta, vAta));
        }

        private async Task Lansiraj()
        {
            TaskOP t = JedanTask();
            if (t == null) return;
            await Radnja(() => TaskRepo.PromeniStatus(t, StatusTaska.Lansiran));
        }

        private async Task Zavrsi()
        {
            TaskOP t = JedanTask();
            if (t == null) return;
            if (t.Status != StatusTaska.Lansiran) { EkranOP.Poruka(this, "Završava se samo lansiran task."); return; }
            var dlg = new MaliDijalog("Završetak · task " + t.ID);
            DateTimePicker atd = dlg.Datum("ATD (stvarni polazak)", t.ATD ?? t.ETD ?? DateTime.Now, true);
            DateTimePicker ata = dlg.Datum("ATA (stvarni dolazak)", t.ATA ?? DateTime.Now, true);
            using (dlg)
            {
                if (!dlg.Prikazi(this)) return;
            }
            DateTime? vAtd = Vrednost(atd), vAta = Vrednost(ata);
            await Radnja(() => TaskRepo.PromeniStatus(t, StatusTaska.Zavrsen, vAtd, vAta));
        }

        private async Task VratiUOtvoren()
        {
            TaskOP t = JedanTask();
            if (t == null) return;
            await Radnja(() => TaskRepo.PromeniStatus(t, StatusTaska.Otvoren));
        }

        private async Task Storniraj()
        {
            TaskOP t = JedanTask();
            if (t == null) return;
            if (!EkranOP.Potvrdi(this, "Stornirati task " + t.ID + " (" + Naziv(t.OdID) + " – " + Naziv(t.DoID) + ")?")) return;
            await Radnja(() => TaskRepo.PromeniStatus(t, StatusTaska.Storniran));
        }

        // Radno mesto taska: uloga, stanica (sa rute taska) i vreme; predlog je polazna stanica za mašinovođu i
        // pregledača na prijemu (pregledač sat pre polaska), a krajnja za pregledača na predaji (sat posle dolaska).
        // Predlog za ulogu: rad u polaznoj stanici sat pre polaska (prijem, manevra), vožnja sa vozom od ETD
        // do kraja zauzetosti taska (mašinovođa, vozovođa, ostali), a predaja u krajnjoj stanici sat posle dolaska.
        private static void PredlogMesta(TaskOP t, TaskOsoba m)
        {
            DateTime polazak = t.Od ?? DateTime.Now, dolazak = t.ETA ?? t.Do ?? polazak;
            switch (m.Uloga)
            {
                case Pravila.PonudaOsoblja.UlogaPrijem:
                case Pravila.PonudaOsoblja.UlogaManevrista:
                case Pravila.PonudaOsoblja.UlogaMasinovodjaManevra:
                    m.StanicaID = t.OdID; m.Od = polazak.AddHours(-1); m.Do = polazak; break;
                case Pravila.PonudaOsoblja.UlogaPredaja:
                    m.StanicaID = t.DoID; m.Od = dolazak; m.Do = dolazak.AddHours(1); break;
                default:
                    m.StanicaID = t.OdID; m.Od = polazak; m.Do = t.Do ?? polazak.AddHours(1); break;
            }
        }

        // Radno mesto taska: uloga (samo kod novog), stanica sa rute taska, vreme i napomena.
        private bool UrediMesto(TaskOP t, TaskOsoba m, bool novo)
        {
            List<int> stanice = t.Stanice.Select(s => s.StanicaID).Distinct().ToList();
            if (!stanice.Contains(m.StanicaID)) stanice.Insert(0, m.StanicaID);
            var dlg = new MaliDijalog((novo ? "Novo radno mesto" : TaskOsoba.NazivUloge(m.Uloga)) + " · task " + t.ID);
            ComboBox uloga = dlg.Izbor("Radno mesto", TaskOsoba.Uloge.Select(TaskOsoba.NazivUloge).ToArray());
            uloga.SelectedIndex = Array.IndexOf(TaskOsoba.Uloge, m.Uloga);
            uloga.Enabled = novo;
            ComboBox stanica = dlg.Izbor("Stanica", stanice.Select(Naziv).ToArray());
            stanica.SelectedIndex = stanice.IndexOf(m.StanicaID);
            DateTimePicker od = dlg.Datum("Od", m.Od, true), doV = dlg.Datum("Do", m.Do, true);
            TextBox napomena = dlg.Tekst(novo ? "Napomena (za „Ostali“: koji posao)" : "Napomena");
            napomena.Text = m.Napomena ?? "";
            // Nova uloga menja predlog stanice i vremena.
            uloga.SelectedIndexChanged += (s, e) =>
            {
                m.Uloga = TaskOsoba.Uloge[uloga.SelectedIndex];
                PredlogMesta(t, m);
                stanica.SelectedIndex = Math.Max(0, stanice.IndexOf(m.StanicaID));
                od.Value = m.Od;
                doV.Value = m.Do;
            };
            using (dlg)
            {
                if (!dlg.Prikazi(this)) return false;
            }
            m.Uloga = TaskOsoba.Uloge[uloga.SelectedIndex];
            m.StanicaID = stanice[stanica.SelectedIndex];
            m.Od = Vrednost(od).Value;
            m.Do = Vrednost(doV).Value;
            m.Napomena = napomena.Text.Trim().Length == 0 ? null : napomena.Text.Trim();
            return true;
        }

        private async Task DodajMesto()
        {
            TaskOP t = JedanTask();
            if (t == null) return;
            var m = new TaskOsoba { TaskID = t.ID, Uloga = Pravila.PonudaOsoblja.UlogaMasinovodja };
            PredlogMesta(t, m);
            if (!UrediMesto(t, m, true)) return;
            await Radnja(() => TaskRepo.SacuvajOsobu(t, m));
        }

        private TaskOsoba IzabranoMesto(TaskOP t)
        {
            DataRow r = EkranOP.Izabran(tabOsoblje);
            return r == null ? null : t.Osoblje.FirstOrDefault(x => x.ID == Convert.ToInt32(r["ID"]));
        }

        private async Task IzmeniMesto()
        {
            TaskOP t = JedanTask();
            if (t == null) return;
            TaskOsoba m = IzabranoMesto(t);
            if (m == null) { EkranOP.Poruka(this, "Izaberite radno mesto."); return; }
            if (!UrediMesto(t, m, false)) return;
            await Radnja(() => TaskRepo.SacuvajOsobu(t, m));
        }

        private async Task UkloniMesto()
        {
            TaskOP t = JedanTask();
            if (t == null) return;
            TaskOsoba m = IzabranoMesto(t);
            if (m == null) { EkranOP.Poruka(this, "Izaberite radno mesto."); return; }
            if (!EkranOP.Potvrdi(this, "Ukloniti radno mesto „" + TaskOsoba.NazivUloge(m.Uloga) + ", " + Naziv(m.StanicaID) + "“ sa taska " + t.ID + "?")) return;
            await Radnja(() => TaskRepo.ObrisiOsobu(t, m));
        }

        // Konkretnu osobu za radna mesta taska bira Ponuda osoblja; po zatvaranju se taskovi čitaju iznova.
        private void OdaberiOsoblje()
        {
            TaskOP t = JedanTask();
            if (t == null) return;
            if (t.Osoblje.Count == 0) { EkranOP.Poruka(this, "Task nema radna mesta. Dodajte mašinovođu ili pregledača, pa odaberite osoblje."); return; }
            var f = new frmOPPonudaOsoblja(null, t.ID);
            if (!f.Pravo) { f.Dispose(); return; }
            f.FormClosed += (s, e) => { if (!IsDisposed) { var _ = OsveziTaskove(); } };
            f.Show(this);
        }

        // Gantogram zauzetosti lokomotiva ili osoblja za period ekrana; dvoklik na task u gantogramu ga bira ovde.
        private void OtvoriGantogram(frmOPGanttTaskova.Vrsta v)
        {
            if (kontekst == null) { EkranOP.Poruka(this, "Sačekajte da se taskovi učitaju."); return; }
            var f = new frmOPGanttTaskova(v, PeriodOd, PeriodDo.AddDays(-1), kontekst, taskovi.ToList(), IzaberiTask);
            f.Show(this);
        }

        private void IzaberiTask(int id)
        {
            if (IsDisposed) return;
            TaskOP t = taskovi.FirstOrDefault(x => x.ID == id);
            if (t == null) { EkranOP.Poruka(this, "Task " + id + " nije u periodu ekrana. Osvežite taskove."); return; }
            izaberiPosle = id;
            // Task druge najave se vidi tek uz „Svi taskovi perioda“.
            if (!chkSvi.Checked && t.Najave.All(n => n.NajavaID != izabranaNajava)) chkSvi.Checked = true;
            else PrikaziTaskove();
            Activate();
        }

        // Za probni rad: najava se vraća na početak (bez ijednog taska), pa se task pravi iznova iz rute.
        private async Task ObrisiTaskoveNajave()
        {
            if (!izabranaNajava.HasValue) { EkranOP.Poruka(this, "Izaberite najavu."); return; }
            int najava = izabranaNajava.Value;
            List<TaskOP> njeni = taskovi.Where(t => t.Najave.Any(n => n.NajavaID == najava)).ToList();
            List<TaskOP> spojeni = njeni.Where(t => t.Najave.Count > 1).ToList();
            if (!EkranOP.Potvrdi(this, "Obrisati iz baze SVE taskove najave " + najava + ", i stornirane?"
                + (njeni.Any(t => t.Status == StatusTaska.Lansiran || t.Status == StatusTaska.Zavrsen) ? "\n\nMeđu njima ima lansiranih ili završenih taskova." : "")
                + (spojeni.Count > 0 ? "\n\nTaskovi " + string.Join(", ", spojeni.Select(t => t.ID)) + " su spojeni sa drugim najavama, pa nestaju i iz njih." : "")
                + "\n\nBrisanje se ne može poništiti.")) return;
            await EkranOP.Ucitaj(this, () => TaskRepo.ObrisiZaNajavu(najava), n =>
            {
                EkranOP.Poruka(this, "Obrisano taskova: " + n + ". Najava " + najava + " je bez taskova.");
                var _ = OsveziTaskove();
            });
        }

        private async Task UzmiNajave()
        {
            DateTime od = PeriodOd, doDatuma = PeriodDo;
            await EkranOP.Ucitaj(this, () => PosaoRepo.UzmiNajave(od, doDatuma), n =>
            {
                EkranOP.Poruka(this, "Novih poslova iz najava: " + n + ".");
                var _ = Osvezi(false);
            });
        }
    }
}
