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

        private readonly ComboBox cboPosao = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Label lblZaglavlje = new Label();
        private readonly Label lblIzabrani = new Label { Text = "Izabrani: niko" };
        private readonly Label lblNaslovPrijem = new Label();
        private readonly Label lblNaslovPredaja = new Label();
        private readonly TextBox txtNapomena = new TextBox { Multiline = true };
        private readonly DataGridView tabMasinovodje = EkranOP.NovaTabela("DeSifra");
        private readonly DataGridView tabPrijem = EkranOP.NovaTabela("DeSifra");
        private readonly DataGridView tabPredaja = EkranOP.NovaTabela("DeSifra");
        private readonly DataGridView tabOdgovori = EkranOP.NovaTabela("ID");

        private readonly Dictionary<string, KandidatOsoba> izabrani = new Dictionary<string, KandidatOsoba>();
        private TerminPosla termin;
        private OsobljeKontekst kontekst;
        private readonly List<string> greskePrevoza = new List<string>();
        private List<KandidatOsoba> masinovodje = new List<KandidatOsoba>(), zaPrijem = new List<KandidatOsoba>(), zaPredaju = new List<KandidatOsoba>();
        private List<ZadatakOsobe> zadaci = new List<ZadatakOsobe>();
        private bool punjenje;

        public frmOPPonudaOsoblja(int? posaoId = null)
        {
            PosaoId = posaoId;
            InitializeComponent();
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        private async Task UcitajPoslove()
        {
            await EkranOP.Ucitaj(this, () =>
            {
                TokPoslaRepo.UskladiSve();
                return TokPoslaRepo.Aktivni();
            }, t =>
            {
                punjenje = true;
                cboPosao.DataSource = t;
                cboPosao.DisplayMember = "Naziv";
                cboPosao.ValueMember = "ID";
                if (PosaoId.HasValue) cboPosao.SelectedValue = PosaoId.Value;
                else if (t.Rows.Count > 0) PosaoId = Convert.ToInt32(cboPosao.SelectedValue);
                punjenje = false;
                var _ = Osvezi();
            });
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
            if (!PosaoId.HasValue) { lblZaglavlje.Text = "Nema aktivnih poslova."; return; }
            int id = PosaoId.Value;
            await EkranOP.Ucitaj(this, () => Pripremi(id), d =>
            {
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
                lblZaglavlje.Text = "Ponuda osoblja · Najava " + d.Posao.BrojNajave + "   ·   Lokomotive: " + (d.Lokomotive.Length > 0 ? d.Lokomotive : "nisu potvrđene")
                    + "   ·   Polazak: " + termin.Polazak.ToString("dd.MM. HH:mm") + ", " + d.StanicaPrijema + "   ·   Predaja: " + termin.Predaja.ToString("dd.MM. HH:mm") + ", " + d.StanicaPredaje;
                lblNaslovPrijem.Text = "Pregledač: prijem, " + d.StanicaPrijema;
                lblNaslovPredaja.Text = "Pregledač: predaja, " + d.StanicaPredaje;
                tabMasinovodje.DataSource = TabelaMasinovodja(masinovodje);
                tabPrijem.DataSource = TabelaPregledaca(zaPrijem);
                tabPredaja.DataSource = TabelaPregledaca(zaPredaju);
                PrikaziOdgovore();
                PrikaziIzabrane();
            });
        }

        private static DataTable TabelaMasinovodja(IEnumerable<KandidatOsoba> lista)
        {
            var t = new DataTable();
            t.Columns.Add("DeSifra", typeof(int));
            foreach (string k in new[] { "Ime", "Turnus", "Lokacija", "Lokomotiva", "Pruga", "Zauzetost", "Ocena" }) t.Columns.Add(k);
            foreach (KandidatOsoba c in lista)
                t.Rows.Add(c.Osoba.DeSifra, c.Osoba.Naziv, c.Osoba.Turnus, c.Lokacija, c.ImaLokomotivu == true ? "Da" : "Ne", c.ImaPrugu == true ? "Da" : "Ne", c.Zauzetost, c.Ocena);
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
            return u == PonudaOsoblja.UlogaMasinovodja ? "Mašinovođa" : u == PonudaOsoblja.UlogaPrijem ? "Pregledač, prijem" : "Pregledač, predaja";
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
            lblIzabrani.Text = izabrani.Count == 0 ? "Izabrani: niko" : "Izabrani:\n" + string.Join("\n", izabrani.Select(p => OpisUloge(p.Key) + ": " + p.Value.Osoba.Naziv));
        }

        private async Task Posalji()
        {
            if (termin == null) return;
            if (!izabrani.ContainsKey(PonudaOsoblja.UlogaMasinovodja) && !zadaci.Any(z => z.Uloga == PonudaOsoblja.UlogaMasinovodja && z.Aktivan))
            {
                EkranOP.Poruka(this, "Izaberite mašinovođu.");
                return;
            }
            if (izabrani.Count == 0) { EkranOP.Poruka(this, "Izaberite osobu iz ponude."); return; }
            if (greskePrevoza.Count > 0) { EkranOP.Poruka(this, string.Join("\n", greskePrevoza)); return; }
            TerminPosla t = termin;
            string napomena = string.IsNullOrWhiteSpace(txtNapomena.Text) ? null : txtNapomena.Text.Trim();
            List<KandidatOsoba> osobe = izabrani.Values.ToList();
            List<ZadatakRepo.NoviZadatak> novi = osobe.Select(c => NoviZadatak(c, t, napomena)).ToList();
            await EkranOP.Ucitaj(this, () =>
            {
                ZadatakRepo.Posalji(t.PosaoID, novi, napomena);
                string naslov = "Zadatak: najava " + t.BrojNajave;
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
            string uloga = z.Uloga;
            await EkranOP.Ucitaj(this, () => { ZadatakRepo.Odgovori(z, prihvatio, razlog); return Pripremi(posao); }, d =>
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
