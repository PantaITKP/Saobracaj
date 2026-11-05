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
    public partial class frmOPPosao : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmOPPosao";
        private const int BrojPonuda = 3;

        public bool Pravo { get; }
        public int PosaoId { get; }

        private readonly int? predlozenaLokomotiva;
        private readonly Label lblNaslov = new Label();
        private readonly Label lblPodaci = new Label();
        private readonly Label lblLinija = new Label();
        private readonly Label lblMasa = new Label();
        private readonly Label lblIzabrano = new Label { Text = "Izabrano: ništa" };
        private readonly Label lblProvere = new Label();
        private readonly Panel trakaMase = new Panel();
        private readonly DataGridView tabRuta = EkranOP.NovaTabela("RB");
        private readonly DataGridView tabPonuda = EkranOP.NovaTabela("Indeks");
        private readonly DataGridView tabProvere = EkranOP.NovaTabela();
        private readonly DataGridView tabTrase = EkranOP.NovaTabela("RB");
        private readonly Label lblTrase = new Label();

        private PlanKontekst kontekst;
        private RezultatPosla rezultat;
        private MasaZaSeriju masa;
        private List<KandidatLokomotiva> ponuda = new List<KandidatLokomotiva>();
        private KandidatLokomotiva izabran;

        public frmOPPosao(int posaoId, int? lokomotiva = null)
        {
            PosaoId = posaoId;
            predlozenaLokomotiva = lokomotiva;
            InitializeComponent();
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        private sealed class PodaciEkrana
        {
            internal PlanKontekst Kontekst;
            internal RezultatPosla Rezultat;
            internal List<KandidatLokomotiva> Ponuda;
            internal List<string> Iskljuceni;
            internal List<KombinacijaTrasa> Kombinacije;
        }

        // Kombinacije trasa za najavu: isti račun kao na mapi, a bruto i dužina su iz najave.
        private static List<KombinacijaTrasa> KombinacijeZa(PlanKontekst k, RezultatPosla r)
        {
            PosaoPlan p = r.Posao;
            if (!p.PrijemID.HasValue || !p.PredajaID.HasValue || p.PrijemID == p.PredajaID) return new List<KombinacijaTrasa>();
            var z = new ZahtevRute { OdID = p.PrijemID.Value, DoID = p.PredajaID.Value, DuzinaM = p.DuzinaM, Datum = p.Prijem.Date, BrojAlternativa = 3 };
            return KombinacijeTrasa.SaZastojima(k, Rutiranje.IzracunajSaZastojima(k, z), z, TraseRepo.ZaKombinacije(p.Prijem.Date), r.Bruto > 0 ? r.Bruto : (decimal?)null);
        }

        private static PodaciEkrana Pripremi(int id)
        {
            PosaoPlan p = PlanRepo.Posao(id);
            if (p == null) throw new InvalidOperationException("Posao više ne postoji ili nema termin prijema.");
            PlanKontekst k = PlanRepo.Ucitaj(p.Prijem.Date, p.Prijem.Date.AddDays(7));
            RezultatPosla r = PlanRacun.Izracunaj(k, p);
            List<string> isk;
            List<KandidatLokomotiva> ponuda = PredlogLokomotiva.Kandidati(k, r, k.Kalendar, out isk);
            return new PodaciEkrana { Kontekst = k, Rezultat = r, Ponuda = ponuda, Iskljuceni = isk, Kombinacije = KombinacijeZa(k, r) };
        }

        private async Task Osvezi(int? lokomotiva = null)
        {
            int id = PosaoId;
            await EkranOP.Ucitaj(this, () => Pripremi(id), d =>
            {
                kontekst = d.Kontekst;
                rezultat = d.Rezultat;
                ponuda = d.Ponuda;
                Prikazi();
                PrikaziTrase(d.Kombinacije);
                if (lokomotiva.HasValue) IzaberiLokomotivu(lokomotiva.Value, d.Iskljuceni);
            });
        }

        // Prevlačenje trake u planu vodi ovde: isti izbor, iste provere i ista potvrda.
        private void IzaberiLokomotivu(int lokomotiva, List<string> iskljuceni)
        {
            KandidatLokomotiva k = ponuda.FirstOrDefault(c => c.Lokomotive.Any(l => l.ID == lokomotiva));
            if (k != null) { Izaberi(k); return; }
            LokomotivaOP lok = kontekst.Lokomotive.FirstOrDefault(l => l.ID == lokomotiva);
            string broj = lok?.Broj ?? "";
            string razlog = iskljuceni.FirstOrDefault(x => x.StartsWith(broj + ":"));
            EkranOP.Poruka(this, "Lokomotiva " + broj + " ne može da preuzme ovaj posao." + (razlog != null ? "\n\n" + razlog : ""));
        }

        private void PrikaziTrase(List<KombinacijaTrasa> lista)
        {
            PosaoPlan p = rezultat.Posao;
            tabTrase.DataSource = EkranOP.TabelaKombinacija(lista, kontekst.NaziviStanica);
            lblTrase.Text = lista.Count == 0
                ? "Za ovu najavu nema rute."
                : "Trase koje dozvoljavaju bruto " + Bro(rezultat.Bruto, " t") + (p.DuzinaM.HasValue ? " i dužinu " + Bro(p.DuzinaM, " m") : "") + ". Trase nemaju vremena, pa presedanje nije provereno.";
        }

        private static string Bro(decimal? v, string jedinica = "")
        {
            return v.HasValue ? v.Value.ToString("#,##0.##", CultureInfo.GetCultureInfo("sr-Latn-RS")) + jedinica : "–";
        }

        private void Prikazi()
        {
            PosaoPlan p = rezultat.Posao;
            lblNaslov.Text = "Najava " + p.BrojNajave + " · " + p.Relacija + "      Faza " + p.Faza + " – " + NazivFaze(p.Faza);
            lblPodaci.Text = "Otpravna: " + p.Otpravna + "\nUputna: " + p.Uputna
                + "\nBruto: " + Bro(rezultat.Bruto, " t") + (rezultat.MasaIzNajave ? "  (Čeka Teretnicu)" : "  (iz Teretnice)")
                + "\nKm po daljinaru: " + (rezultat.Ruta.Count == 0 ? "nema rute" : Bro(rezultat.Km, " km")) + "   ·   BTTO: " + (rezultat.Ruta.Count == 0 ? "–" : Bro(Math.Round(rezultat.Btto)))
                + "\nDužina: " + Bro(p.DuzinaM, " m") + "   ·   Broj kola: " + (p.BrojKola?.ToString() ?? "–") + "   ·   Tip kola: " + (p.TipKola ?? "–");
            lblLinija.Text = "Prijem " + p.Prijem.ToString("dd.MM. HH:mm") + "  →  obrada  →  polazak " + rezultat.Polazak.ToString("dd.MM. HH:mm")
                + "  →  dolazak " + rezultat.Dolazak.ToString("dd.MM. HH:mm") + "  →  predaja " + rezultat.Predaja.ToString("dd.MM. HH:mm");

            var ruta = new DataTable();
            ruta.Columns.Add("RB", typeof(int));
            foreach (string k in new[] { "Od", "Do", "Km", "Stanje", "Napomena" }) ruta.Columns.Add(k);
            foreach (DeonicaRute d in rezultat.Ruta)
                ruta.Rows.Add(d.RB, kontekst.Naziv(d.OdID), kontekst.Naziv(d.DoID), Bro(d.Km), OpisStanja(d.Stanje), d.Napomena);
            tabRuta.DataSource = ruta;
            if (!tabRuta.Columns.Contains("AdHoc"))
                tabRuta.Columns.Add(new DataGridViewButtonColumn { Name = "AdHoc", HeaderText = "", Text = "Zatraži ad hoc", UseColumnTextForButtonValue = true, MinimumWidth = Saobracaj.Tema.Theme.Dp(110) });

            List<KandidatLokomotiva> prikazane = ponuda.Take(BrojPonuda).ToList();
            KandidatLokomotiva vodeci = prikazane.FirstOrDefault();
            LokomotivaOP uzorak = vodeci?.Lokomotive[0] ?? kontekst.Lokomotive.FirstOrDefault(l => l.SerijaID.HasValue);
            masa = uzorak == null ? null : BrojLokomotiva.Odredi(kontekst, rezultat.Bruto, uzorak, rezultat.Ruta, p.Prijem);
            lblMasa.Text = masa?.Tekst ?? "Nema lokomotive sa serijom.";
            trakaMase.Invalidate();

            var t = new DataTable();
            t.Columns.Add("Indeks", typeof(int));
            foreach (string k in new[] { "#", "Lokomotive", "Lokacija", "Prazna vožnja", "Zauzetost", "Provere" }) t.Columns.Add(k);
            for (int i = 0; i < prikazane.Count; i++)
            {
                KandidatLokomotiva c = prikazane[i];
                List<ProveraStavka> pr = ProverePosla.Proveri(kontekst, rezultat, c.Lokomotive);
                t.Rows.Add(i, i + 1, c.Oznaka, c.Lokacija + " (" + c.IzvorLokacije + ")", c.LokacijaNepoznata ? "nepoznato" : c.PraznaVoznjaMin + " min", "Slobodna", pr.Count(x => x.Ok) + " od " + pr.Count + " · " + c.Razlog);
            }
            tabPonuda.DataSource = t;
            if (tabPonuda.Columns.Contains("#")) tabPonuda.Columns["#"].FillWeight = 25;
            if (!tabPonuda.Columns.Contains("Izaberi"))
                tabPonuda.Columns.Add(new DataGridViewButtonColumn { Name = "Izaberi", HeaderText = "", Text = "Izaberi", UseColumnTextForButtonValue = true, MinimumWidth = Saobracaj.Tema.Theme.Dp(70) });

            izabran = null;
            lblIzabrano.Text = "Izabrano: ništa";
            PrikaziProvere(new List<LokomotivaOP>());
        }

        private void PrikaziProvere(IList<LokomotivaOP> lokomotive)
        {
            List<ProveraStavka> provere = ProverePosla.Proveri(kontekst, rezultat, lokomotive);
            var t = new DataTable();
            foreach (string k in new[] { "Provera", "Stanje", "Objašnjenje" }) t.Columns.Add(k);
            foreach (ProveraStavka s in provere) t.Rows.Add(s.RB + ". " + s.Naziv, s.Ok ? "U redu" : "Nije", s.Tekst);
            tabProvere.DataSource = t;
            lblProvere.Text = provere.Count(x => x.Ok) + " od " + provere.Count + " u redu";
        }

        private void Izaberi(KandidatLokomotiva k)
        {
            izabran = k;
            lblIzabrano.Text = "Izabrano: " + k.Oznaka + " · " + k.Razlog;
            PrikaziProvere(k.Lokomotive);
        }

        private static string NazivFaze(int f)
        {
            string[] nazivi = { "", "Najava", "Plan lokomotive", "Osoblje i zadatak", "Prijem i pregled", "U putu", "Predaja u toku", "Predato" };
            return f >= 1 && f < nazivi.Length ? nazivi[f] : "";
        }

        private static string OpisStanja(string s)
        {
            switch (s)
            {
                case "IMA_TRASU": return "Ima trasu";
                case "NEMAMO_TRASU": return "Nemamo trasu";
                case "ZAHTEV_ADHOC": return "Zahtev za ad hoc";
                case "ZATVOR": return "Zatvor";
                default: return s;
            }
        }

        private async Task ZatraziAdHoc(int rb)
        {
            DeonicaRute d = rezultat.Ruta.FirstOrDefault(x => x.RB == rb);
            if (d == null) return;
            if (d.Stanje != "NEMAMO_TRASU") { EkranOP.Poruka(this, "Za ovu deonicu nije potreban zahtev."); return; }
            PosaoPlan p = rezultat.Posao;
            string od = kontekst.Naziv(d.OdID), doN = kontekst.Naziv(d.DoID);
            await EkranOP.Ucitaj(this, () => { PosaoRepo.ZatraziAdHoc(p, d, od, doN); return true; }, _ => { var __ = Osvezi(); });
        }

        private async Task Potvrdi()
        {
            if (izabran == null) { EkranOP.Poruka(this, "Prvo izaberite lokomotive iz ponude."); return; }
            List<ProveraStavka> pr = ProverePosla.Proveri(kontekst, rezultat, izabran.Lokomotive);
            List<ProveraStavka> neprosle = pr.Where(x => !x.Ok).ToList();
            ProveraStavka zauzetost = neprosle.FirstOrDefault(x => x.RB == 5), brojMase = neprosle.FirstOrDefault(x => x.RB == 2);
            if (zauzetost != null) { EkranOP.Poruka(this, zauzetost.Tekst + " Preklapanje se ne može potvrditi."); return; }
            if (brojMase != null) { EkranOP.Poruka(this, brojMase.Tekst); return; }
            if (neprosle.Count > 0 && !EkranOP.Potvrdi(this, "Nisu prošle sve provere:\n\n" + string.Join("\n", neprosle.Select(x => x.Naziv + ": " + x.Tekst)) + "\n\nPotvrditi ipak?")) return;
            KandidatLokomotiva k = izabran;
            RezultatPosla r = rezultat;
            await EkranOP.Ucitaj(this, () => { PosaoRepo.PotvrdiLokomotive(r, k, k.Lokomotive.Count); return true; }, _ =>
            {
                EkranOP.Poruka(this, "Lokomotive su potvrđene i posao je poslat dispečeru.");
                var __ = Osvezi();
            });
        }
    }
}
