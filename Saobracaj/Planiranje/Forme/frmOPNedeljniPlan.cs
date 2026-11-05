using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Kontrole;
using Saobracaj.Planiranje.Modeli;
using Saobracaj.Planiranje.Podaci;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPNedeljniPlan : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmOPNedeljniPlan";
        public bool Pravo { get; }
        public VrstaPlana Vrsta { get; private set; }
        public bool SamoBezLokomotive { get; }

        private readonly DateTimePicker dtNedelja = new DateTimePicker { Format = DateTimePickerFormat.Short };
        private readonly ComboBox cboVrsta = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Label lblVerzija = new Label();
        private readonly Label lblBrojaci = new Label();
        private readonly GanttPanel gantt = new GanttPanel();
        private readonly ListBox lstNalazi = new ListBox { DrawMode = DrawMode.OwnerDrawVariable, IntegralHeight = false, BorderStyle = BorderStyle.None };
        private readonly DataGridView tabBezLok = EkranOP.NovaTabela("ID");
        private readonly Button btnPrihvati;

        private PrikazPlana prikaz;
        private List<StavkaNacrta> nacrt;

        private sealed class PrikazPlana
        {
            internal PlanKontekst Kontekst;
            internal List<RezultatPosla> Rezultati;
            internal List<Nalaz> Nalazi;
            internal string Verzija;
        }

        public frmOPNedeljniPlan(VrstaPlana vrsta, bool samoBezLokomotive = false)
        {
            Vrsta = vrsta;
            SamoBezLokomotive = samoBezLokomotive;
            btnPrihvati = EkranOP.Dugme("Prihvati predlog", (s, e) => { var _ = Prihvati(); });
            btnPrihvati.Enabled = false;
            InitializeComponent();
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        private DateTime PeriodOd
        {
            get
            {
                DateTime d = dtNedelja.Value.Date;
                return Vrsta == VrstaPlana.Operativni ? d : d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
            }
        }

        private DateTime PeriodDo => PeriodOd.AddDays(Vrsta == VrstaPlana.Operativni ? 2 : 7);
        private string VrstaBaza => Vrsta == VrstaPlana.Operativni ? "OPERATIVNI" : "NEDELJNI";

        private static PrikazPlana Pripremi(DateTime od, DateTime doDatuma, string vrsta)
        {
            TokPoslaRepo.UskladiSve();
            PlanKontekst k = PlanRepo.Ucitaj(od, doDatuma);
            List<RezultatPosla> rezultati = PlanRepo.Poslovi(od, doDatuma).Select(p => PlanRacun.Izracunaj(k, p)).ToList();
            List<Nalaz> nalazi = KonfliktiPlana.Izracunaj(k, rezultati);
            nalazi.AddRange(PonudaOsoblja.Nalazi(ZadatakRepo.Kontekst(k, od, doDatuma), rezultati));
            return new PrikazPlana { Kontekst = k, Rezultati = rezultati, Nalazi = nalazi, Verzija = PosaoRepo.OznakaVerzije(vrsta, od) };
        }

        private async Task Osvezi()
        {
            DateTime od = PeriodOd, doDatuma = PeriodDo;
            string vrsta = VrstaBaza;
            nacrt = null;
            btnPrihvati.Enabled = false;
            await EkranOP.Ucitaj(this, () => Pripremi(od, doDatuma, vrsta), p =>
            {
                prikaz = p;
                Prikazi();
            });
        }

        private void Prikazi()
        {
            if (prikaz == null) return;
            PlanKontekst k = prikaz.Kontekst;
            lblVerzija.Text = prikaz.Verzija + (nacrt != null ? " · predlog nije upisan" : "");

            List<ZauzetostLok> poslovi = k.Kalendar.Where(z => z.Vrsta == "POSAO").ToList();
            List<RezultatPosla> bezLok = prikaz.Rezultati.Where(r => !poslovi.Any(z => z.PosaoID == r.Posao.ID)).ToList();
            HashSet<int> konflikti = new HashSet<int>(prikaz.Nalazi.Where(n => n.Nivo == NivoNalaza.Konflikt && n.PosaoID.HasValue).Select(n => n.PosaoID.Value));
            int dvostruka = prikaz.Rezultati.Count(r => poslovi.Count(z => z.PosaoID == r.Posao.ID) >= 2);
            lblBrojaci.Text = "Poslova u planu: " + prikaz.Rezultati.Count + "   ·   Sa lokomotivom: " + (prikaz.Rezultati.Count - bezLok.Count)
                + "   ·   Bez lokomotive: " + bezLok.Count + "   ·   Konflikata: " + prikaz.Nalazi.Count(n => n.Nivo == NivoNalaza.Konflikt)
                + "   ·   Dvostruka vuča: " + dvostruka;
            lblBrojaci.ForeColor = bezLok.Count > 0 || prikaz.Nalazi.Any(n => n.Nivo == NivoNalaza.Konflikt) ? Saobracaj.Tema.Theme.Akcent : Saobracaj.Tema.Theme.Tekst;

            var redovi = new List<GanttRed>();
            var trake = new List<GanttTraka>();
            if (!SamoBezLokomotive)
                foreach (LokomotivaOP l in k.Lokomotive)
                    redovi.Add(new GanttRed { Kljuc = l.ID, Naslov = NaslovLokomotive(l) });
            redovi.Add(new GanttRed { Kljuc = null, Naslov = "Bez lokomotive" });

            foreach (ZauzetostLok z in k.Kalendar)
            {
                if (z.Vrsta != "POSAO")
                {
                    trake.Add(new GanttTraka { RedKljuc = z.LokomotivaID, Od = z.Od, Do = z.Do, Vrsta = VrstaTrake.Zauzetost, Tekst = z.Vrsta == "SERVIS" ? "Servis" : z.Vrsta == "OSTALO" ? z.Napomena ?? "Zauzeta" : "Namenska " + z.Projekat });
                    continue;
                }
                RezultatPosla r = prikaz.Rezultati.FirstOrDefault(x => x.Posao.ID == z.PosaoID);
                if (r == null) continue;
                int koliko = poslovi.Count(x => x.PosaoID == z.PosaoID);
                trake.Add(new GanttTraka
                {
                    RedKljuc = z.LokomotivaID,
                    Od = z.Od,
                    Do = z.Do,
                    PosaoID = z.PosaoID,
                    Vrsta = koliko > 1 ? VrstaTrake.DvostrukaVuca : VrstaTrake.Posao,
                    Konflikt = konflikti.Contains(r.Posao.ID),
                    Tekst = (r.Telegrami.Count > 0 ? Zastavica + " " : "") + r.Posao.BrojNajave + " · " + r.Posao.Relacija + (koliko > 1 ? " (" + z.RB + " od " + koliko + ")" : "")
                });
            }
            foreach (RezultatPosla r in bezLok)
            {
                StavkaNacrta s = nacrt?.FirstOrDefault(x => x.Posao.ID == r.Posao.ID);
                if (s?.Izbor != null)
                {
                    foreach (LokomotivaOP l in s.Izbor.Lokomotive)
                        trake.Add(new GanttTraka { RedKljuc = l.ID, Od = r.Posao.Prijem, Do = r.Predaja, PosaoID = r.Posao.ID, Vrsta = VrstaTrake.Nacrt, Tekst = r.Posao.BrojNajave + " · predlog" });
                    continue;
                }
                trake.Add(new GanttTraka { RedKljuc = null, Od = r.Posao.Prijem, Do = r.Predaja, PosaoID = r.Posao.ID, Vrsta = VrstaTrake.BezLokomotive, Konflikt = konflikti.Contains(r.Posao.ID), Tekst = r.Posao.BrojNajave + " · " + r.Posao.Relacija });
            }
            gantt.Prikazi(PeriodOd, PeriodDo, Vrsta == VrstaPlana.Operativni, redovi, trake);

            lstNalazi.BeginUpdate();
            lstNalazi.Items.Clear();
            foreach (Nalaz n in prikaz.Nalazi.OrderBy(x => x.Nivo)) lstNalazi.Items.Add(n);
            lstNalazi.EndUpdate();

            var t = new DataTable();
            t.Columns.Add("ID", typeof(int));
            t.Columns.Add("Najava");
            t.Columns.Add("Telegram");
            t.Columns.Add("Relacija");
            t.Columns.Add("Prijem");
            t.Columns.Add("Km");
            t.Columns.Add("Bruto (t)");
            t.Columns.Add("BTTO");
            t.Columns.Add("Broj kola");
            t.Columns.Add("Tip kola");
            foreach (RezultatPosla r in bezLok)
                t.Rows.Add(r.Posao.ID, r.Posao.NajavaID + (string.IsNullOrEmpty(r.Posao.StatusNajave) ? "" : " / " + r.Posao.StatusNajave), OznakaTelegrama(r), r.Posao.Relacija, r.Posao.Prijem.ToString("dd.MM. HH:mm"),
                    r.Ruta.Count == 0 ? "nema rute" : r.Km.ToString("#,##0.0"), r.Bruto.ToString("#,##0.#"), r.Ruta.Count == 0 ? "–" : r.Btto.ToString("#,##0"),
                    r.Posao.BrojKola?.ToString() ?? "–", r.Posao.TipKola ?? "–");
            tabBezLok.DataSource = t;
            Podesi();
        }

        // Broj / EL ili DZ / GPS; podatak koji lokomotiva nema se ne piše.
        private static string NaslovLokomotive(LokomotivaOP l)
        {
            string s = l.Broj;
            if (l.Vuca == "E") s += " / EL";
            else if (l.Vuca == "D") s += " / DZ";
            return l.ImaGPS ? s + " / GPS" : s;
        }

        private const string Zastavica = "\u2691";

        private static string OznakaTelegrama(RezultatPosla r)
        {
            return r.Telegrami.Count == 0 ? "" : Zastavica + " " + r.Telegrami.Count;
        }

        private static string OpisTelegrama(TelegramOP t, PlanKontekst k)
        {
            return "Telegram " + t.Broj + " (" + (t.Vrsta == "ZATVOR" ? "zatvor" : "upozorenje") + "): " + k.Naziv(t.OdID) + " – " + k.Naziv(t.DoID)
                + (t.VaziDo.HasValue ? ", do " + t.VaziDo.Value.ToString("dd.MM. HH:mm") : "");
        }

        // Desni klik: telegrami zbog kojih je ruta zatvorena, obiđena ili pod upozorenjem.
        private ContextMenuStrip MeniTelegrama(int posaoId)
        {
            var meni = new ContextMenuStrip();
            RezultatPosla r = prikaz?.Rezultati.FirstOrDefault(x => x.Posao.ID == posaoId);
            if (r == null || r.Telegrami.Count == 0)
            {
                meni.Items.Add("Na ruti nema telegrama").Enabled = false;
                return meni;
            }
            foreach (TelegramOP t in r.Telegrami)
            {
                TelegramOP izabran = t;
                meni.Items.Add(OpisTelegrama(t, prikaz.Kontekst), null, (s, e) => PrikaziTelegram(izabran));
            }
            return meni;
        }

        private static string TekstTelegrama(TelegramOP t, PlanKontekst k)
        {
            string n = Environment.NewLine;
            return "Telegram: " + t.Broj + "   (ID " + t.ID + ")" + n
                + "Vrsta: " + (t.Vrsta == "ZATVOR" ? "zatvor pruge" : "upozorenje") + (string.IsNullOrEmpty(t.Kolosek) ? "" : ", kolosek: " + t.Kolosek) + n
                + "Deonica: " + k.Naziv(t.OdID) + " – " + k.Naziv(t.DoID) + n
                + "Važi: " + t.VaziOd.ToString("dd.MM.yyyy HH:mm") + " – " + (t.VaziDo.HasValue ? t.VaziDo.Value.ToString("dd.MM.yyyy HH:mm") : "bez kraja") + n
                + (t.TrajeOd.HasValue && t.TrajeDo.HasValue ? "Dnevno: " + t.TrajeOd.Value.ToString(@"hh\:mm") + " – " + t.TrajeDo.Value.ToString(@"hh\:mm") + n : "")
                + n + t.Napomena;
        }

        private void PrikaziTelegram(TelegramOP t)
        {
            using (var d = new Form { Text = "Telegram " + t.Broj, StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(Saobracaj.Tema.Theme.Dp(560), Saobracaj.Tema.Theme.Dp(360)), MinimizeBox = false })
            {
                var tekst = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Text = TekstTelegrama(t, prikaz.Kontekst) };
                var dole = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = Saobracaj.Tema.Theme.Dp(40), FlowDirection = FlowDirection.RightToLeft };
                var zatvori = new Button { Text = "Zatvori", AutoSize = true, DialogResult = DialogResult.Cancel };
                var ekran = new Button { Text = "Otvori u ekranu Telegrami", AutoSize = true, DialogResult = DialogResult.OK };
                dole.Controls.Add(zatvori);
                dole.Controls.Add(ekran);
                d.Controls.Add(tekst);
                d.Controls.Add(dole);
                d.CancelButton = zatvori;
                if (d.ShowDialog(this) == DialogResult.OK) OtvoriEkranTelegrama(t.ID);
            }
        }

        private void OtvoriEkranTelegrama(int telegramId)
        {
            var f = new Saobracaj.Sifarnici.frmTelegrami();
            if (!f.Pravo) { f.Dispose(); return; }
            f.Shown += (s, e) => IzaberiRedSaId(f, telegramId);
            f.Show(this);
        }

        // Postojeći ekran se ne menja, pa se red traži u njegovoj tabeli.
        private static void IzaberiRedSaId(Control c, int id)
        {
            foreach (Control x in c.Controls)
            {
                var tabela = x as DataGridView;
                if (tabela != null && tabela.Columns.Contains("ID"))
                {
                    foreach (DataGridViewRow r in tabela.Rows)
                        if (r.Visible && Convert.ToString(r.Cells["ID"].Value) == id.ToString())
                        {
                            DataGridViewCell celija = r.Cells.Cast<DataGridViewCell>().FirstOrDefault(y => y.Visible);
                            if (celija != null) tabela.CurrentCell = celija;
                            return;
                        }
                    return;
                }
                IzaberiRedSaId(x, id);
            }
        }

        private void Podesi()
        {
            // Tabela je uska, a zastavica i status moraju da se vide bez pomeranja.
            if (tabBezLok.Columns.Contains("Najava")) tabBezLok.Columns["Najava"].Width = Saobracaj.Tema.Theme.Dp(150);
            if (tabBezLok.Columns.Contains("Telegram")) tabBezLok.Columns["Telegram"].Width = Saobracaj.Tema.Theme.Dp(62);
            if (tabBezLok.Columns.Contains("Ponudi")) return;
            var dugme = new DataGridViewButtonColumn { Name = "Ponudi", HeaderText = "", Text = "Ponudi", UseColumnTextForButtonValue = true, Width = Saobracaj.Tema.Theme.Dp(70) };
            tabBezLok.Columns.Add(dugme);
        }

        private void OtvoriPosao(int posaoId, int? lokomotiva = null)
        {
            var f = new frmOPPosao(posaoId, lokomotiva);
            if (!f.Pravo) { f.Dispose(); return; }
            f.FormClosed += (s, e) => { if (!IsDisposed) { var _ = Osvezi(); } };
            f.Show(this);
        }

        private async Task UzmiNajave()
        {
            DateTime od = PeriodOd, doDatuma = PeriodDo;
            await EkranOP.Ucitaj(this, () =>
            {
                int novih = PosaoRepo.UzmiNajave(od, doDatuma);
                if (novih == 0) return 0;
                // Termini i ruta se odmah računaju, da bi ih videli i kalendar i tok posla.
                PlanKontekst k = PlanRepo.Ucitaj(od, doDatuma);
                foreach (PosaoPlan p in PlanRepo.Poslovi(od, doDatuma).Where(x => !x.PlaniranaPredaja.HasValue))
                    PosaoRepo.SacuvajRacun(PlanRacun.Izracunaj(k, p));
                return novih;
            }, n =>
            {
                EkranOP.Poruka(this, n == 0 ? "Nema novih najava u periodu." : "U plan je ušlo novih najava: " + n + ".");
                var _ = Osvezi();
            });
        }

        private async Task SacuvajVerziju()
        {
            DateTime od = PeriodOd, doDatuma = PeriodDo;
            string vrsta = VrstaBaza;
            await EkranOP.Ucitaj(this, () => PosaoRepo.SacuvajVerziju(vrsta, od, doDatuma), b =>
            {
                EkranOP.Poruka(this, "Sačuvana je verzija " + b + ".");
                var _ = Osvezi();
            });
        }

        private async Task Predlozi()
        {
            if (prikaz == null) return;
            PlanKontekst k = prikaz.Kontekst;
            List<PosaoPlan> poslovi = prikaz.Rezultati.Select(r => r.Posao).ToList();
            bool operativni = Vrsta == VrstaPlana.Operativni;
            DateTime od = PeriodOd;
            string izvorPredloga = null;
            await EkranOP.Ucitaj(this, () =>
            {
                if (OptimizatorKlijent.Podesen)
                {
                    try { return IzOptimizatora(k, OptimizatorKlijent.Predlozi(operativni, od)); }
                    catch (Exception ex) when (ex is System.Net.Http.HttpRequestException || ex is OperationCanceledException || ex is Newtonsoft.Json.JsonException)
                    {
                        izvorPredloga = "Optimizator nije dostupan, predlog je iz jednostavnog algoritma.\n\n";
                    }
                }
                return PredlogLokomotiva.Nacrt(k, poslovi);
            }, n =>
            {
                nacrt = n;
                btnPrihvati.Enabled = n.Any(x => x.Izbor != null);
                Prikazi();
                int bez = n.Count(x => x.Izbor == null);
                EkranOP.Poruka(this, izvorPredloga + "Predlog: " + n.Count(x => x.Izbor != null) + " poslova dobija lokomotivu" + (bez > 0 ? ", " + bez + " ostaje bez nje." : ".") + " Ništa nije upisano u poslove dok ne izaberete „Prihvati predlog“.");
            });
        }

        private List<StavkaNacrta> IzOptimizatora(PlanKontekst k, OdgovorOptimizatora o)
        {
            var lista = new List<StavkaNacrta>();
            foreach (RezultatPosla r in prikaz.Rezultati)
            {
                DodelaOptimizatora d = o.Dodele.FirstOrDefault(x => x.PosaoID == r.Posao.ID);
                if (d == null)
                {
                    if (o.Nedodeljeni.Contains(r.Posao.ID)) lista.Add(new StavkaNacrta { Posao = r.Posao, Rezultat = r, Razlog = "Nema slobodne lokomotive." });
                    continue;
                }
                List<LokomotivaOP> izabrane = d.Lokomotive.Select(id => k.Lokomotive.FirstOrDefault(l => l.ID == id)).Where(l => l != null).ToList();
                if (izabrane.Count != d.Lokomotive.Count) continue;
                lista.Add(new StavkaNacrta
                {
                    Posao = r.Posao,
                    Rezultat = r,
                    Masinovodja = d.Masinovodja,
                    Izbor = new KandidatLokomotiva { Lokomotive = izabrane, PraznaVoznjaMin = d.PraznaVoznjaMin, Razlog = "Optimizator, prazna vožnja " + d.PraznaVoznjaMin + " min" }
                });
            }
            return lista;
        }

        private async Task Prihvati()
        {
            if (nacrt == null) return;
            List<StavkaNacrta> stavke = nacrt.Where(x => x.Izbor != null).ToList();
            if (!EkranOP.Potvrdi(this, "Upisati predlog za " + stavke.Count + " poslova?")) return;
            await EkranOP.Ucitaj(this, () =>
            {
                var greske = new List<string>();
                foreach (StavkaNacrta s in stavke)
                {
                    try
                    {
                        PosaoRepo.PotvrdiLokomotive(s.Rezultat, s.Izbor, s.Izbor.Lokomotive.Count);
                        if (s.Masinovodja.HasValue) ZadatakRepo.Predlozi(s.Posao.ID, s.Masinovodja.Value, s.Posao.PrijemID, s.Rezultat.Polazak, s.Rezultat.Predaja);
                    }
                    catch (Exception ex) when (ex is DBConcurrencyException || ex is InvalidOperationException)
                    {
                        greske.Add("Najava " + s.Posao.BrojNajave + ": " + ex.Message);
                    }
                }
                return greske;
            }, greske =>
            {
                EkranOP.Poruka(this, greske.Count == 0 ? "Predlog je upisan." : "Upisano je " + (stavke.Count - greske.Count) + " od " + stavke.Count + " poslova.\n\n" + string.Join("\n", greske.Take(10)));
                var _ = Osvezi();
            });
        }
    }
}
