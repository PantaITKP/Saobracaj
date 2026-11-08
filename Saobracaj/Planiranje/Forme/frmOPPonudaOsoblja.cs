using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Modeli;
using Saobracaj.Planiranje.Podaci;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPPonudaOsoblja : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmOPPonudaOsoblja";
        public bool Pravo { get; }
        public int? PosaoId { get; private set; }
        // Osoblje se određuje za task (ekran Taskovi); po najavi samo kad se otvori sa posaoId.
        public int? TaskId { get; private set; }
        private readonly bool zaTask;
        private readonly Dictionary<string, TaskOsoba> mesta = new Dictionary<string, TaskOsoba>();
        private TerminPosla terminPrijem, terminPredaja;
        private TaskOP task;

        private readonly ComboBox cboPosao = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Label lblZaglavlje = new Label();
        private readonly Label lblIzabrani = new Label { Text = "Izabrani: niko" };
        private readonly Label lblNaslovMasinovodja = new Label { Text = "Mašinovođa" };
        private readonly Label lblNaslovOstalo = new Label { Text = "Ostala radna mesta" };
        private readonly Label lblNaslovPrijem = new Label();
        private readonly Label lblNaslovPredaja = new Label();
        private readonly TextBox txtNapomena = new TextBox { Multiline = true };
        private readonly DataGridView tabMasinovodje = EkranOP.NovaTabela("DeSifra");
        private readonly DataGridView tabPrijem = EkranOP.NovaTabela("DeSifra");
        private readonly DataGridView tabPredaja = EkranOP.NovaTabela("DeSifra");
        private readonly DataGridView tabOdgovori = EkranOP.NovaTabela("ID");
        // Ostala radna mesta taska (manevrista, vozovođa, mašinovođa na manevri, ostali): jedno po jedno, iz liste.
        private readonly ComboBox cboOstalo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly DataGridView tabOstali = EkranOP.NovaTabela("DeSifra");
        private List<TaskOsoba> ostalaMesta = new List<TaskOsoba>();
        private Dictionary<int, List<KandidatOsoba>> poMestu = new Dictionary<int, List<KandidatOsoba>>();
        private List<KandidatOsoba> ostali = new List<KandidatOsoba>();

        private readonly Dictionary<string, KandidatOsoba> izabrani = new Dictionary<string, KandidatOsoba>();
        private TerminPosla termin;
        private OsobljeKontekst kontekst;
        private readonly List<string> greskePrevoza = new List<string>();
        private List<KandidatOsoba> masinovodje = new List<KandidatOsoba>(), zaPrijem = new List<KandidatOsoba>(), zaPredaju = new List<KandidatOsoba>();
        private List<ZadatakOsobe> zadaci = new List<ZadatakOsobe>();
        private bool punjenje;

        public frmOPPonudaOsoblja(int? posaoId = null, int? taskId = null)
        {
            PosaoId = posaoId;
            TaskId = taskId;
            zaTask = !posaoId.HasValue;
            InitializeComponent();
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        private async Task UcitajPoslove()
        {
            await EkranOP.Ucitaj(this, () =>
            {
                if (zaTask) return TaskRepo.ZaIzborOsoblja();
                TokPoslaRepo.UskladiSve();
                return TokPoslaRepo.Aktivni();
            }, t =>
            {
                punjenje = true;
                cboPosao.DataSource = t;
                cboPosao.DisplayMember = "Naziv";
                cboPosao.ValueMember = "ID";
                int? id = zaTask ? TaskId : PosaoId;
                if (id.HasValue) cboPosao.SelectedValue = id.Value;
                else if (t.Rows.Count > 0) id = Convert.ToInt32(cboPosao.SelectedValue);
                if (zaTask) TaskId = id; else PosaoId = id;
                punjenje = false;
                var _ = Osvezi();
            });
        }

        // Izbor u padajućoj listi: task ili posao najave.
        private void IzabranoUListi(int id)
        {
            if (zaTask) TaskId = id; else PosaoId = id;
            var _ = Osvezi();
        }

        private sealed class Podaci
        {
            internal TerminPosla Termin;
            internal OsobljeKontekst Kontekst;
            internal string Lokomotive;
            internal PosaoPlan Posao;
            internal string StanicaPrijema;
            internal string StanicaPredaje;
            internal List<KandidatOsoba> Masinovodje, Prijem, Predaja;
            internal List<ZadatakOsobe> Zadaci;
            internal TaskOP Task;
            internal TerminPosla TerminPrijem, TerminPredaja;
            internal Dictionary<string, TaskOsoba> Mesta = new Dictionary<string, TaskOsoba>();
            internal List<TaskOsoba> OstalaMesta = new List<TaskOsoba>();
            internal Dictionary<int, List<KandidatOsoba>> PoMestu = new Dictionary<int, List<KandidatOsoba>>();
            internal Dictionary<int, string> NaziviStanica = new Dictionary<int, string>();
        }

        // Task: svako radno mesto (uloga, stanica, od–do) ima svoj termin; mašinovođa i pregledač za prijem
        // u istoj stanici mogu da putuju zajedno (par), kao kod posla.
        private static Podaci PripremiTask(int id)
        {
            TaskOP t = TaskRepo.Jedan(id);
            if (t == null) throw new InvalidOperationException("Task " + id + " više ne postoji.");
            int? posao = t.Najave.Select(n => n.PosaoID).FirstOrDefault(x => x.HasValue);
            if (!posao.HasValue) throw new InvalidOperationException("Task " + id + " nije vezan za posao najave.");
            DateTime baza = (t.Od ?? DateTime.Today).Date;
            PlanKontekst k = PlanRepo.Ucitaj(baza.AddDays(-1), baza.AddDays(7));
            OsobljeKontekst o = ZadatakRepo.Kontekst(k, baza.AddDays(-1), baza.AddDays(7));
            var d = new Podaci { Task = t, Kontekst = o, Zadaci = ZadatakRepo.ZaTask(id), Lokomotive = TaskRepo.OpisLokomotiva(t.Lokomotive) };
            foreach (TaskOsoba m in t.Osoblje)
                if (!d.Mesta.ContainsKey(m.Uloga)) d.Mesta[m.Uloga] = m;
            TaskOsoba mas, pri, pre;
            d.Mesta.TryGetValue(PonudaOsoblja.UlogaMasinovodja, out mas);
            d.Mesta.TryGetValue(PonudaOsoblja.UlogaPrijem, out pri);
            d.Mesta.TryGetValue(PonudaOsoblja.UlogaPredaja, out pre);
            Func<TerminPosla> osnova = () =>
            {
                var x = new TerminPosla
                {
                    PosaoID = posao.Value, TaskID = id, BrojNajave = t.OznakaNajava,
                    Prijem = t.Od ?? baza, Polazak = t.Od ?? baza, Dolazak = t.ETA ?? t.Do ?? baza, Predaja = t.Do ?? baza, PrijemID = t.OdID, PredajaID = t.DoID
                };
                x.Lokomotive.AddRange(t.Lokomotive.Select(l => l.LokomotivaID));
                x.Pruge.AddRange(t.Stanice.Where(s => s.PrugaID.HasValue).Select(s => s.PrugaID.Value).Distinct());
                return x;
            };
            TerminPosla tm = osnova(), tp = osnova(), td = osnova();
            if (mas != null)
            {
                tm.PrijemID = mas.StanicaID; tm.Polazak = mas.Od; tm.Predaja = mas.Do;
                tm.Prijem = pri != null && pri.StanicaID == mas.StanicaID ? pri.Od : mas.Od;
            }
            if (pri != null) { tp.PrijemID = pri.StanicaID; tp.Prijem = pri.Od; tp.Polazak = pri.Do; }
            if (pre != null) { td.PredajaID = pre.StanicaID; td.Dolazak = pre.Od; td.Predaja = pre.Do; td.Prijem = pre.Od; }
            d.Termin = tm;
            d.TerminPrijem = tp;
            d.TerminPredaja = td;
            d.Masinovodje = mas != null ? PonudaOsoblja.Masinovodje(o, tm) : new List<KandidatOsoba>();
            d.Prijem = pri != null ? PonudaOsoblja.Pregledaci(o, tp, PonudaOsoblja.UlogaPrijem) : new List<KandidatOsoba>();
            d.Predaja = pre != null ? PonudaOsoblja.Pregledaci(o, td, PonudaOsoblja.UlogaPredaja) : new List<KandidatOsoba>();
            if (mas != null && pri != null && pri.StanicaID == mas.StanicaID) PonudaOsoblja.Upari(o, tm, d.Masinovodje, d.Prijem);
            d.StanicaPrijema = pri != null ? k.Naziv(pri.StanicaID) : null;
            d.StanicaPredaje = pre != null ? k.Naziv(pre.StanicaID) : null;
            // Ostala radna mesta: svako ima svoju ponudu za stanicu i vreme mesta.
            foreach (TaskOsoba m in t.Osoblje.Where(x => PonudaOsoblja.OstaloMesto(x.Uloga)))
            {
                List<KandidatOsoba> lista = PonudaOsoblja.ZaMesto(o, tm, m.Uloga, m.StanicaID, m.Od, m.Do, m.ID);
                foreach (KandidatOsoba c in lista) c.MestoID = m.ID;
                d.OstalaMesta.Add(m);
                d.PoMestu[m.ID] = lista;
                d.NaziviStanica[m.StanicaID] = k.Naziv(m.StanicaID);
            }
            return d;
        }


        private static string Mesto(TaskOsoba m, string stanica)
        {
            return stanica + ", " + m.Od.ToString("dd.MM. HH:mm") + " – " + m.Do.ToString("dd.MM. HH:mm");
        }

        private static Podaci Pripremi(int id)
        {
            PosaoPlan p = PlanRepo.Posao(id);
            if (p == null) throw new InvalidOperationException("Posao više ne postoji ili nema termin prijema.");
            PlanKontekst k = PlanRepo.Ucitaj(p.Prijem.Date, p.Prijem.Date.AddDays(7));
            OsobljeKontekst o = ZadatakRepo.Kontekst(k, p.Prijem.Date, p.Prijem.Date.AddDays(7));
            RezultatPosla r = PlanRacun.Izracunaj(k, p);
            TerminPosla t = PonudaOsoblja.Termin(k, r);
            List<KandidatOsoba> ponudaMasinovodja = PonudaOsoblja.Masinovodje(o, t), ponudaPrijem = PonudaOsoblja.Pregledaci(o, t, PonudaOsoblja.UlogaPrijem);
            PonudaOsoblja.Upari(o, t, ponudaMasinovodja, ponudaPrijem);
            IEnumerable<string> brojevi = k.Kalendar.Where(z => z.PosaoID == id && z.Vrsta == "POSAO").Select(z => k.Lokomotive.FirstOrDefault(l => l.ID == z.LokomotivaID)?.Broj ?? "?");
            return new Podaci
            {
                Termin = t,
                Kontekst = o,
                Posao = p,
                Lokomotive = string.Join(" + ", brojevi),
                StanicaPrijema = k.Naziv(p.PrijemID),
                StanicaPredaje = k.Naziv(p.PredajaID),
                Masinovodje = ponudaMasinovodja,
                Prijem = ponudaPrijem,
                Predaja = PonudaOsoblja.Pregledaci(o, t, PonudaOsoblja.UlogaPredaja),
                Zadaci = ZadatakRepo.ZaPosao(id)
            };
        }

        private async Task Osvezi()
        {
            if (zaTask ? !TaskId.HasValue : !PosaoId.HasValue) { lblZaglavlje.Text = zaTask ? "Nema otvorenih taskova." : "Nema aktivnih poslova."; return; }
            int id = zaTask ? TaskId.Value : PosaoId.Value;
            bool poTasku = zaTask;
            await EkranOP.Ucitaj(this, () => poTasku ? PripremiTask(id) : Pripremi(id), d =>
            {
                task = d.Task;
                terminPrijem = d.TerminPrijem;
                terminPredaja = d.TerminPredaja;
                mesta.Clear();
                foreach (KeyValuePair<string, TaskOsoba> m in d.Mesta) mesta[m.Key] = m.Value;
                termin = d.Termin;
                kontekst = d.Kontekst;
                masinovodje = d.Masinovodje;
                zaPrijem = d.Prijem;
                zaPredaju = d.Predaja;
                zadaci = d.Zadaci;
                izabrani.Clear();
                greskePrevoza.Clear();
                ZadatakOsobe predlog = zadaci.FirstOrDefault(z => z.Status == "PREDLOZEN" && z.Uloga == PonudaOsoblja.UlogaMasinovodja);
                KandidatOsoba predlozen = predlog == null ? null : masinovodje.FirstOrDefault(c => c.Prolazi && c.Osoba.DeSifra == predlog.DeSifra);
                if (predlozen != null) Izaberi(PonudaOsoblja.UlogaMasinovodja, predlozen);
                if (d.Task != null)
                {
                    PlanKontekst pk = kontekst.Plan;
                    TaskOsoba m;
                    lblZaglavlje.Text = "Ponuda osoblja · Task " + d.Task.ID + " · najava " + d.Task.OznakaNajava + " · " + pk.Naziv(d.Task.OdID) + " – " + pk.Naziv(d.Task.DoID)
                        + "   ·   Lokomotive: " + (d.Lokomotive.Length > 0 ? d.Lokomotive : "nisu dodeljene")
                        + "   ·   Mašinovođa: " + (mesta.TryGetValue(PonudaOsoblja.UlogaMasinovodja, out m) ? Mesto(m, pk.Naziv(m.StanicaID)) : "nije traženo na tasku")
                        + (d.Task.Osoblje.Count > mesta.Count ? "   ·   Za svaku ulogu nudi se prvo radno mesto taska." : "");
                    lblNaslovMasinovodja.Text = "Mašinovođa" + (mesta.TryGetValue(PonudaOsoblja.UlogaMasinovodja, out m) ? ": " + Mesto(m, pk.Naziv(m.StanicaID)) : " – nije traženo na tasku");
                    lblNaslovPrijem.Text = "Pregledač: prijem" + (mesta.TryGetValue(PonudaOsoblja.UlogaPrijem, out m) ? ", " + Mesto(m, d.StanicaPrijema) : " – nije traženo na tasku");
                    lblNaslovPredaja.Text = "Pregledač: predaja" + (mesta.TryGetValue(PonudaOsoblja.UlogaPredaja, out m) ? ", " + Mesto(m, d.StanicaPredaje) : " – nije traženo na tasku");
                }
                else
                {
                    lblZaglavlje.Text = "Ponuda osoblja · Najava " + d.Posao.BrojNajave + "   ·   Lokomotive: " + (d.Lokomotive.Length > 0 ? d.Lokomotive : "nisu potvrđene")
                        + "   ·   Polazak: " + termin.Polazak.ToString("dd.MM. HH:mm") + ", " + d.StanicaPrijema + "   ·   Predaja: " + termin.Predaja.ToString("dd.MM. HH:mm") + ", " + d.StanicaPredaje;
                    lblNaslovMasinovodja.Text = "Mašinovođa";
                    lblNaslovPrijem.Text = "Pregledač: prijem, " + d.StanicaPrijema;
                    lblNaslovPredaja.Text = "Pregledač: predaja, " + d.StanicaPredaje;
                }
                tabMasinovodje.DataSource = TabelaMasinovodja(masinovodje);
                tabPrijem.DataSource = TabelaPregledaca(zaPrijem);
                tabPredaja.DataSource = TabelaPregledaca(zaPredaju);
                ostalaMesta = d.OstalaMesta;
                poMestu = d.PoMestu;
                punjenje = true;
                cboOstalo.Items.Clear();
                foreach (TaskOsoba m in ostalaMesta)
                {
                    string stanica;
                    d.NaziviStanica.TryGetValue(m.StanicaID, out stanica);
                    cboOstalo.Items.Add(TaskOsoba.NazivUloge(m.Uloga) + " · " + Mesto(m, stanica) + (m.Napomena != null ? " · " + m.Napomena : ""));
                }
                cboOstalo.Enabled = ostalaMesta.Count > 0;
                punjenje = false;
                if (ostalaMesta.Count > 0) cboOstalo.SelectedIndex = 0; else PrikaziOstale();
                PrikaziOdgovore();
                PrikaziIzabrane();
            });
        }

        private static DataTable TabelaMasinovodja(IEnumerable<KandidatOsoba> lista)
        {
            var t = new DataTable();
            t.Columns.Add("DeSifra", typeof(int));
            foreach (string k in new[] { "Ime", "Turnus", "Lokacija", "Serija", "Pruga", "Zauzetost", "Ocena" }) t.Columns.Add(k);
            foreach (KandidatOsoba c in lista)
                t.Rows.Add(c.Osoba.DeSifra, c.Osoba.Naziv, c.Osoba.Turnus, c.Lokacija, c.ImaSeriju == true ? "Da" : "Ne", c.ImaPrugu == true ? "Da" : "Ne", c.Zauzetost, c.Ocena);
            return t;
        }

        private static DataTable TabelaPregledaca(IEnumerable<KandidatOsoba> lista)
        {
            var t = new DataTable();
            t.Columns.Add("DeSifra", typeof(int));
            foreach (string k in new[] { "Ime", "Turnus", "Lokacija", "Zauzetost", "Ocena" }) t.Columns.Add(k);
            foreach (KandidatOsoba c in lista)
                t.Rows.Add(c.Osoba.DeSifra, c.Osoba.Naziv, c.Osoba.Turnus, c.Lokacija, c.Zauzetost, c.Ocena);
            return t;
        }

        private void PrikaziOdgovore()
        {
            var t = new DataTable();
            t.Columns.Add("ID", typeof(int));
            foreach (string k in new[] { "Osoba", "Uloga", "Status", "Telefon" }) t.Columns.Add(k);
            foreach (ZadatakOsobe z in zadaci) t.Rows.Add(z.ID, z.Osoba, OpisUloge(z.Uloga), z.Status, z.Telefon);
            tabOdgovori.DataSource = t;
            if (!tabOdgovori.Columns.Contains("Prihvatio"))
            {
                tabOdgovori.Columns.Add(new DataGridViewButtonColumn { Name = "Prihvatio", HeaderText = "", Text = "Prihvatio", UseColumnTextForButtonValue = true, MinimumWidth = Saobracaj.Tema.Theme.Dp(70) });
                tabOdgovori.Columns.Add(new DataGridViewButtonColumn { Name = "Odbio", HeaderText = "", Text = "Odbio", UseColumnTextForButtonValue = true, MinimumWidth = Saobracaj.Tema.Theme.Dp(60) });
            }
        }

        private static string OpisUloge(string u)
        {
            if (PonudaOsoblja.JePrevoz(u)) return "Prevoz: " + OpisUloge(PonudaOsoblja.UlogaPutnika(u)).ToLowerInvariant();
            return TaskOsoba.NazivUloge(u);
        }

        private void IzaberiRed(DataGridView g, List<KandidatOsoba> lista, int red, string uloga)
        {
            if (red < 0 || red >= lista.Count) return;
            int sifra = Convert.ToInt32(g.Rows[red].Cells["DeSifra"].Value);
            KandidatOsoba c = lista.FirstOrDefault(x => x.Osoba.DeSifra == sifra);
            if (c == null) return;
            if (!c.Prolazi) { EkranOP.Poruka(this, c.Osoba.Naziv + " se ne može izabrati: " + c.Ocena + " (" + c.Zauzetost + ")."); return; }
            Izaberi(uloga, c);
            PrikaziIzabrane();
        }

        // Vozač ide uz osobu koju vozi: novi izbor menja i njega. Ko nema AUTO dobija i par, ako par još nije izabran.
        private void Izaberi(string uloga, KandidatOsoba c)
        {
            izabrani[uloga] = c;
            if (uloga == PonudaOsoblja.UlogaPredaja)
            {
                if (c.Prevoz != null) izabrani[PonudaOsoblja.UlogaPrevozPredaja] = c.Prevoz;
                else izabrani.Remove(PonudaOsoblja.UlogaPrevozPredaja);
                return;
            }
            string drugaUloga = uloga == PonudaOsoblja.UlogaMasinovodja ? PonudaOsoblja.UlogaPrijem : PonudaOsoblja.UlogaMasinovodja;
            if (c.Partner != null && !izabrani.ContainsKey(drugaUloga) && !zadaci.Any(z => z.Uloga == drugaUloga && z.Aktivan))
                izabrani[drugaUloga] = c.Partner;
            UskladiPar();
        }

        // Izabrana osoba ili osoba kojoj je zadatak već poslat.
        private KandidatOsoba UParu(string uloga, List<KandidatOsoba> lista)
        {
            KandidatOsoba c;
            if (izabrani.TryGetValue(uloga, out c)) return c;
            ZadatakOsobe z = zadaci.FirstOrDefault(x => x.Uloga == uloga && x.Aktivan);
            return z == null ? null : lista.FirstOrDefault(x => x.Osoba.DeSifra == z.DeSifra);
        }

        private void UskladiPar()
        {
            izabrani.Remove(PonudaOsoblja.UlogaPrevozMasinovodje);
            izabrani.Remove(PonudaOsoblja.UlogaPrevozPrijem);
            greskePrevoza.Clear();
            if (kontekst == null || termin == null) return;
            foreach (KandidatOsoba v in PonudaOsoblja.Prevozi(kontekst, termin, UParu(PonudaOsoblja.UlogaMasinovodja, masinovodje), UParu(PonudaOsoblja.UlogaPrijem, zaPrijem), greskePrevoza))
                if (!zadaci.Any(z => z.Uloga == v.Uloga && z.Aktivan))
                    izabrani[v.Uloga] = v;
        }

        private void PrikaziIzabrane()
        {
            lblIzabrani.Text = izabrani.Count == 0 ? "Izabrani: niko" : "Izabrani:\n" + string.Join("\n", izabrani.Select(p => OpisUloge(p.Value.MestoID.HasValue ? p.Value.Uloga : p.Key) + ": " + p.Value.Osoba.Naziv));
        }

        // Ponuda za radno mesto izabrano u listi „Ostala radna mesta“.
        private void PrikaziOstale()
        {
            TaskOsoba m = cboOstalo.SelectedIndex >= 0 && cboOstalo.SelectedIndex < ostalaMesta.Count ? ostalaMesta[cboOstalo.SelectedIndex] : null;
            ostali = m != null && poMestu.ContainsKey(m.ID) ? poMestu[m.ID] : new List<KandidatOsoba>();
            tabOstali.DataSource = TabelaPregledaca(ostali);
            if (m == null)
                lblNaslovOstalo.Text = zaTask ? "Ostala radna mesta – task ih nema (manevrista, vozovođa, mašinovođa na manevri, ostali)" : "Ostala radna mesta – samo za task";
            else
                lblNaslovOstalo.Text = "Ostala radna mesta (" + ostalaMesta.Count + ")";
        }

        private static string Kljuc(TaskOsoba m)
        {
            return "MESTO_" + m.ID;
        }

        private void IzaberiOstalog(int red)
        {
            if (red < 0 || red >= tabOstali.Rows.Count || cboOstalo.SelectedIndex < 0) return;
            int sifra = Convert.ToInt32(tabOstali.Rows[red].Cells["DeSifra"].Value);
            KandidatOsoba c = ostali.FirstOrDefault(x => x.Osoba.DeSifra == sifra);
            if (c == null) return;
            if (!c.Prolazi) { EkranOP.Poruka(this, c.Osoba.Naziv + " se ne može izabrati: " + c.Ocena + " (" + c.Zauzetost + ")."); return; }
            TaskOsoba m = ostalaMesta[cboOstalo.SelectedIndex];
            if (zadaci.Any(z => z.TaskOsobljeID == m.ID && z.Aktivan)) { EkranOP.Poruka(this, "Za ovo radno mesto zadatak je već poslat."); return; }
            izabrani[Kljuc(m)] = c;
            PrikaziIzabrane();
        }

        private async Task Posalji()
        {
            if (termin == null) return;
            // Task traži samo uloge za koje ima radno mesto; posao najave uvek traži mašinovođu.
            bool trebaMasinovodja = !zaTask || mesta.ContainsKey(PonudaOsoblja.UlogaMasinovodja);
            if (trebaMasinovodja && !izabrani.ContainsKey(PonudaOsoblja.UlogaMasinovodja) && !zadaci.Any(z => z.Uloga == PonudaOsoblja.UlogaMasinovodja && z.Aktivan))
            {
                EkranOP.Poruka(this, "Izaberite mašinovođu.");
                return;
            }
            if (izabrani.Count == 0) { EkranOP.Poruka(this, "Izaberite osobu iz ponude."); return; }
            if (greskePrevoza.Count > 0) { EkranOP.Poruka(this, string.Join("\n", greskePrevoza)); return; }
            TerminPosla t = termin;
            string napomena = string.IsNullOrWhiteSpace(txtNapomena.Text) ? null : txtNapomena.Text.Trim();
            List<KandidatOsoba> osobe = izabrani.Values.ToList();
            List<ZadatakRepo.NoviZadatak> novi = osobe.Select(c => zaTask ? NoviZadatakTaska(c, napomena) : NoviZadatak(c, t, napomena)).ToList();
            int? taskId = zaTask ? TaskId : null;
            await EkranOP.Ucitaj(this, () =>
            {
                ZadatakRepo.Posalji(t.PosaoID, novi, napomena, taskId);
                string naslov = taskId.HasValue ? "Zadatak: task " + taskId + " (najava " + t.BrojNajave + ")" : "Zadatak: najava " + t.BrojNajave;
                string tekst = "Polazak " + t.Polazak.ToString("dd.MM.yyyy HH:mm") + ", predaja " + t.Predaja.ToString("dd.MM.yyyy HH:mm") + "." + (napomena != null ? "\n\n" + napomena : "");
                return osobe.Where(c => !ZadatakRepo.PosaljiMail(c.Osoba.Email, naslov, PonudaOsoblja.JePrevoz(c.Uloga) ? c.Napomena : tekst + (c.Napomena != null ? "\n\n" + c.Napomena : ""))).ToList();
            }, bezMaila =>
            {
                EkranOP.Poruka(this, bezMaila.Count == 0 ? "Zadatak je poslat mailom." : "Zadatak je upisan. Javiti telefonom:\n\n" + string.Join("\n", bezMaila.Select(c => c.Osoba.Naziv + ": " + (c.Osoba.Telefon ?? "nema telefona"))));
                txtNapomena.Clear();
                var _ = Osvezi();
            });
        }

        private static ZadatakRepo.NoviZadatak NoviZadatak(KandidatOsoba c, TerminPosla t, string napomena)
        {
            if (PonudaOsoblja.JePrevoz(c.Uloga))
                return new ZadatakRepo.NoviZadatak { DeSifra = c.Osoba.DeSifra, Naziv = c.Osoba.Naziv, Uloga = c.Uloga, StanicaID = c.StanicaID, Od = c.TerminOd.Value, Do = c.TerminDo.Value, Napomena = c.Napomena };
            if (c.Uloga == PonudaOsoblja.UlogaMasinovodja)
                return new ZadatakRepo.NoviZadatak { DeSifra = c.Osoba.DeSifra, Naziv = c.Osoba.Naziv, Uloga = c.Uloga, StanicaID = t.PrijemID, Od = c.TerminOd ?? t.Polazak, Do = t.Predaja, Napomena = c.Napomena == null ? null : c.Napomena + (napomena != null ? " " + napomena : "") };
            bool prijem = c.Uloga == PonudaOsoblja.UlogaPrijem;
            return new ZadatakRepo.NoviZadatak { DeSifra = c.Osoba.DeSifra, Naziv = c.Osoba.Naziv, Uloga = c.Uloga, Napomena = c.Napomena, StanicaID = prijem ? t.PrijemID : t.PredajaID, Od = c.TerminOd ?? (prijem ? t.Prijem : t.Dolazak), Do = prijem ? t.Polazak : t.Predaja };
        }

        // Zadatak za radno mesto taska: stanica i vreme su sa radnog mesta, a vožnja para ili prevoz može da počne ranije.
        private ZadatakRepo.NoviZadatak NoviZadatakTaska(KandidatOsoba c, string napomena)
        {
            if (PonudaOsoblja.JePrevoz(c.Uloga))
                return new ZadatakRepo.NoviZadatak { DeSifra = c.Osoba.DeSifra, Naziv = c.Osoba.Naziv, Uloga = c.Uloga, StanicaID = c.StanicaID, Od = c.TerminOd.Value, Do = c.TerminDo.Value, Napomena = c.Napomena };
            TaskOsoba m = c.MestoID.HasValue ? ostalaMesta.First(x => x.ID == c.MestoID.Value) : mesta[c.Uloga];
            string tekst = c.Uloga == PonudaOsoblja.UlogaMasinovodja && c.Napomena != null ? c.Napomena + (napomena != null ? " " + napomena : "") : c.Napomena;
            return new ZadatakRepo.NoviZadatak
            {
                TaskOsobljeID = m.ID, DeSifra = c.Osoba.DeSifra, Naziv = c.Osoba.Naziv, Uloga = c.Uloga, StanicaID = m.StanicaID,
                Od = c.TerminOd.HasValue && c.TerminOd.Value < m.Od ? c.TerminOd.Value : m.Od, Do = m.Do, Napomena = tekst ?? m.Napomena
            };
        }

        private async Task Odgovori(int id, bool prihvatio)
        {
            ZadatakOsobe z = zadaci.FirstOrDefault(x => x.ID == id);
            if (z == null) return;
            if (z.Status != "POSLAT") { EkranOP.Poruka(this, "Na ovaj zadatak je već odgovoreno."); return; }
            string razlog = null;
            if (!prihvatio)
            {
                using (var d = new MaliDijalog("Odbijanje zadatka"))
                {
                    TextBox t = d.Tekst("Razlog");
                    if (!d.Prikazi(this)) return;
                    razlog = t.Text.Trim();
                }
                if (razlog.Length == 0) { EkranOP.Poruka(this, "Unesite razlog."); return; }
            }
            int posao = z.PosaoID;
            int? taskZadatka = z.TaskID;
            string uloga = z.Uloga;
            await EkranOP.Ucitaj(this, () => { ZadatakRepo.Odgovori(z, prihvatio, razlog); return taskZadatka.HasValue ? PripremiTask(taskZadatka.Value) : Pripremi(posao); }, d =>
            {
                var _ = OsveziPosleOdgovora(prihvatio, uloga, d);
            });
        }

        private async Task OsveziPosleOdgovora(bool prihvatio, string uloga, Podaci d)
        {
            await Osvezi();
            if (prihvatio) return;
            // Ko je ostao bez para ili vozača dobija novi predlog.
            UskladiPar();
            PrikaziIzabrane();
            if (PonudaOsoblja.JePrevoz(uloga))
            {
                PredloziDrugogVozaca(uloga, d);
                return;
            }
            // Posle odbijanja odmah se predlaže sledeći kandidat iste uloge.
            if (PonudaOsoblja.OstaloMesto(uloga))
            {
                EkranOP.Poruka(this, OpisUloge(uloga) + ": zadatak je odbijen. Izaberite drugu osobu u odeljku „Ostala radna mesta“.");
                return;
            }
            List<KandidatOsoba> lista = uloga == PonudaOsoblja.UlogaMasinovodja ? d.Masinovodje : uloga == PonudaOsoblja.UlogaPrijem ? d.Prijem : d.Predaja;
            KandidatOsoba sledeci = lista.FirstOrDefault(c => c.Prolazi);
            if (sledeci == null) { EkranOP.Poruka(this, "Nema drugog kandidata za ulogu: " + OpisUloge(uloga) + "." + (greskePrevoza.Count > 0 ? "\n" + string.Join("\n", greskePrevoza) : "")); return; }
            Izaberi(uloga, sledeci);
            PrikaziIzabrane();
            EkranOP.Poruka(this, "Sledeći kandidat: " + sledeci.Osoba.Naziv + " (" + sledeci.Ocena + "). Pošaljite mu zadatak.");
        }

        // Vozač je odbio: osoba koju vozi ostaje, a traži se drugi vozač.
        private void PredloziDrugogVozaca(string prevoz, Podaci d)
        {
            string ulogaPutnika = PonudaOsoblja.UlogaPutnika(prevoz);
            ZadatakOsobe putnik = d.Zadaci.FirstOrDefault(z => z.Uloga == ulogaPutnika && z.Aktivan);
            if (putnik == null) return;
            if (prevoz == PonudaOsoblja.UlogaPrevozPredaja)
            {
                KandidatOsoba zaPredajuVozac = d.Predaja.FirstOrDefault(c => c.Osoba.DeSifra == putnik.DeSifra)?.Prevoz;
                if (zaPredajuVozac != null) izabrani[prevoz] = zaPredajuVozac;
                PrikaziIzabrane();
            }
            KandidatOsoba vozac;
            if (!izabrani.TryGetValue(prevoz, out vozac)) { EkranOP.Poruka(this, "Nema drugog vozača za osobu " + putnik.Osoba + "."); return; }
            EkranOP.Poruka(this, "Sledeći vozač: " + vozac.Osoba.Naziv + ". Pošaljite mu zadatak.");
        }
    }
}
