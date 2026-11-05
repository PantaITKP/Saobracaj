using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Agenti_jobs;
using Saobracaj.Planiranje.Podaci;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPTrase : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmOPTrase";
        public bool Pravo { get; }
        public TabTrase PocetniTab { get; }

        private readonly DataGridView tabRazlike = EkranOP.NovaTabela();
        private readonly DataGridView tabGreske = EkranOP.NovaTabela();
        private readonly DataGridView tabIstorija = EkranOP.NovaTabela("ID", "PrethodnaVerzijaID");
        private readonly DataGridView tabTrase = EkranOP.NovaTabela("ID", "VerzijaID", "PocetnaStanicaID", "KrajnjaStanicaID", "AdHocNajavaID", "AdHoc", "AdHocVaziOd", "AdHocVaziDo", "AdHocStatus");
        private readonly DataGridView tabAdHoc = EkranOP.NovaTabela("ID", "VerzijaID", "PocetnaStanicaID", "KrajnjaStanicaID", "AdHoc", "StanjeRazlike", "OpisPromene", "Greska");
        private readonly Label lblBrojaci = new Label { AutoSize = true, Text = "Izaberite datoteku." };
        private readonly ComboBox cboVerzija = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly UredjivacReda poljaTrasa = new UredjivacReda("Ispravka stanica");
        private readonly UredjivacReda poljaAdHoc = new UredjivacReda("Ad hoc trasa");
        private readonly TabControl tabovi = new TabControl();

        private List<RedTrase> pripremljeno;
        private byte[] sadrzajDatoteke;
        private string nazivDatoteke;
        private DateTime vaziOd, vaziDo;
        private int godina;

        public frmOPTrase(TabTrase tab = TabTrase.Uvoz)
        {
            PocetniTab = tab;
            InitializeComponent();
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        private async Task Osvezi()
        {
            await EkranOP.Ucitaj(this, () => new
            {
                Ver = TraseRepo.Verzije(),
                Ad = TraseRepo.ZahteviAdHoc(),
                Sta = StaniceRepo.ZaIzbor()
            }, p =>
            {
                poljaTrasa.IzvorIzbora("PocetnaStanicaID", p.Sta, "Naziv", "ID", true);
                poljaTrasa.IzvorIzbora("KrajnjaStanicaID", p.Sta, "Naziv", "ID", true);
                poljaAdHoc.IzvorIzbora("PocetnaStanicaID", p.Sta, "Naziv", "ID", true);
                poljaAdHoc.IzvorIzbora("KrajnjaStanicaID", p.Sta, "Naziv", "ID", true);
                tabIstorija.DataSource = p.Ver;
                tabAdHoc.DataSource = p.Ad;
                object izabrana = cboVerzija.SelectedValue;
                cboVerzija.DisplayMember = "Naziv";
                cboVerzija.ValueMember = "ID";
                cboVerzija.DataSource = p.Ver.Copy();
                if (izabrana != null) cboVerzija.SelectedValue = izabrana;
            });
        }

        private async Task PrikaziVerziju()
        {
            object v = cboVerzija.SelectedValue;
            if (v == null || v is DataRowView) return;
            int id = Convert.ToInt32(v);
            await EkranOP.Ucitaj(this, () => TraseRepo.Trase(id), t => tabTrase.DataSource = t);
        }

        private async Task IzaberiDatoteku()
        {
            string putanja;
            using (var d = new OpenFileDialog { Filter = "Excel (*.xlsx)|*.xlsx" })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                putanja = d.FileName;
            }
            DateTime od, doDan;
            int god;
            using (var p = new PeriodCenovnika(null, null, Path.GetFileName(putanja)))
            {
                if (p.ShowDialog(this) != DialogResult.OK) return;
                od = p.DatumOd; doDan = p.DatumDo; god = p.Godina;
            }
            await EkranOP.Ucitaj(this, () => Pripremi(putanja, god), r =>
            {
                pripremljeno = r.Redovi;
                sadrzajDatoteke = r.Sadrzaj;
                nazivDatoteke = Path.GetFileName(putanja);
                vaziOd = od; vaziDo = doDan; godina = god;
                PrikaziRazlike();
            });
        }

        private sealed class Priprema
        {
            internal List<RedTrase> Redovi;
            internal byte[] Sadrzaj;
        }

        private static Priprema Pripremi(string putanja, int godina)
        {
            byte[] sadrzaj = XlsxCitac.ProcitajBajtove(putanja);
            List<RouteInfo> rute;
            using (var m = new MemoryStream(sadrzaj)) rute = ExcelRoutes.Read(m);
            int? verzija;
            List<RedTrase> prethodni = TraseRepo.VazecaZaGodinu(godina, out verzija);
            return new Priprema
            {
                Redovi = RazlikeTrasa.Uporedi(rute.Select(RazlikeTrasa.IzRute).ToList(), prethodni, TraseRepo.StaniceZaPoredjenje()),
                Sadrzaj = sadrzaj
            };
        }

        private void PrikaziRazlike()
        {
            Func<string, int> broj = s => pripremljeno.Count(r => r.Stanje == s);
            lblBrojaci.Text = "Redova: " + pripremljeno.Count(r => r.Stanje != "UKINUTA")
                + "   ·   Novih: " + broj("NOVA") + "   ·   Izmenjenih: " + broj("IZMENJENA")
                + "   ·   Ukinutih: " + broj("UKINUTA") + "   ·   Grešaka: " + broj("GRESKA")
                + (RazlikeTrasa.ImaVremena(pripremljeno.Where(r => r.Stanje != "UKINUTA")) ? "" : "   ·   Datoteka nema vremena polaska i dolaska.");
            tabRazlike.DataSource = Tabela(pripremljeno.Where(r => r.Stanje != "ISTA" && r.Stanje != "GRESKA"));
            tabGreske.DataSource = Tabela(pripremljeno.Where(r => r.Stanje == "GRESKA"));
        }

        private static DataTable Tabela(IEnumerable<RedTrase> redovi)
        {
            var t = new DataTable();
            foreach (string k in new[] { "Stanje", "Broj voza", "Početna", "Krajnja", "Polazak", "Dolazak", "Max masa", "Dužina", "Promena ili greška" })
                t.Columns.Add(k);
            foreach (RedTrase r in redovi)
                t.Rows.Add(r.Stanje, r.BrojVoza, r.PocetnaNaziv, r.KrajnjaNaziv,
                    r.VremePolaska.HasValue ? r.VremePolaska.Value.ToString(@"hh\:mm") : "",
                    r.VremeDolaska.HasValue ? r.VremeDolaska.Value.ToString(@"hh\:mm") : "",
                    r.MaxMasaT, r.MaxDuzinaM, r.Greska ?? r.OpisPromene);
            return t;
        }

        private void Odbaci()
        {
            pripremljeno = null;
            sadrzajDatoteke = null;
            tabRazlike.DataSource = null;
            tabGreske.DataSource = null;
            lblBrojaci.Text = "Izaberite datoteku.";
        }

        private async Task PotvrdiUvoz()
        {
            if (pripremljeno == null) { EkranOP.Poruka(this, "Prvo izaberite datoteku."); return; }
            int greske = pripremljeno.Count(r => r.Stanje == "GRESKA");
            if (!EkranOP.Potvrdi(this, "Objaviti trase za " + godina + ". godinu" + (greske > 0 ? " sa " + greske + " redova sa greškom" : "") + "? Prethodna važeća verzija ide u arhivu.")) return;
            List<RedTrase> redovi = pripremljeno;
            byte[] sadrzaj = sadrzajDatoteke;
            string naziv = "Trase " + godina + " · " + nazivDatoteke;
            await EkranOP.Ucitaj(this, () => TraseRepo.Objavi(naziv, godina, vaziOd, vaziDo, "EXCEL", nazivDatoteke, sadrzaj, redovi), v =>
            {
                Odbaci();
                EkranOP.Poruka(this, "Trase su objavljene.");
                var _ = Osvezi();
            });
        }

        private void SacuvajStanice()
        {
            if (!poljaTrasa.ID.HasValue) return;
            if (EkranOP.Izvrsi(this, () => TraseRepo.IspraviStanice(poljaTrasa.ID.Value, poljaTrasa.RowVer, poljaTrasa.IzabranID("PocetnaStanicaID"), poljaTrasa.IzabranID("KrajnjaStanicaID"))))
            {
                var _ = PrikaziVerziju();
            }
        }

        // Popunjava rastojanje, raspodelu po kategorijama i vuči i spisak službenih mesta.
        private async Task IzracunajRutu()
        {
            int? id = poljaTrasa.ID, pocetna = poljaTrasa.IzabranID("PocetnaStanicaID"), krajnja = poljaTrasa.IzabranID("KrajnjaStanicaID");
            byte[] rowVer = poljaTrasa.RowVer;
            if (!id.HasValue) { EkranOP.Poruka(this, "Izaberite trasu."); return; }
            if (!pocetna.HasValue || !krajnja.HasValue) { EkranOP.Poruka(this, "Trasa mora da ima početnu i krajnju stanicu."); return; }
            await EkranOP.Ucitaj(this, () =>
            {
                PlanKontekst k = PlanRepo.Ucitaj(DateTime.Today, DateTime.Today.AddDays(1));
                RezultatRute r = Rutiranje.Izracunaj(k, new ZahtevRute { OdID = pocetna.Value, DoID = krajnja.Value }).FirstOrDefault();
                if (r != null) TraseRepo.SacuvajRutu(id.Value, rowVer, r);
                return r;
            }, r =>
            {
                if (r == null) { EkranOP.Poruka(this, "Između ovih stanica nema puta po mreži."); return; }
                EkranOP.Poruka(this, RutaTekst.Opis(r));
                var _ = PrikaziVerziju();
            });
        }

        private void OdobriAdHoc()
        {
            if (!poljaAdHoc.ID.HasValue) { EkranOP.Poruka(this, "Izaberite zahtev."); return; }
            string broj = poljaAdHoc.Tekst("BrojVoza");
            if (broj.Length == 0 || broj == "ADHOC") { EkranOP.Poruka(this, "Unesite broj voza."); return; }
            TimeSpan? polazak = RazlikeTrasa.ProcitajVreme(poljaAdHoc.Tekst("VremePolaska")), dolazak = RazlikeTrasa.ProcitajVreme(poljaAdHoc.Tekst("VremeDolaska"));
            if (!polazak.HasValue || !dolazak.HasValue) { EkranOP.Poruka(this, "Vreme unesite u obliku ČČ:MM."); return; }
            bool uspeh = EkranOP.Izvrsi(this, () => TraseRepo.OdobriAdHoc(poljaAdHoc.ID.Value, poljaAdHoc.RowVer, broj, poljaAdHoc.IzabranID("PocetnaStanicaID"), poljaAdHoc.IzabranID("KrajnjaStanicaID"), polazak, dolazak,
                poljaAdHoc.DecimalIliNull("MaxMasaT", "Max masa"), poljaAdHoc.DatumVreme("AdHocVaziOd").Date, poljaAdHoc.DatumVreme("AdHocVaziDo").Date, poljaAdHoc.TekstIliNull("Napomena")));
            if (uspeh) { poljaAdHoc.Popuni(null); var _ = Osvezi(); }
        }

        private void OdbijAdHoc()
        {
            if (!poljaAdHoc.ID.HasValue) { EkranOP.Poruka(this, "Izaberite zahtev."); return; }
            if (!EkranOP.Potvrdi(this, "Odbiti izabrani zahtev?")) return;
            if (EkranOP.Izvrsi(this, () => TraseRepo.OdbijAdHoc(poljaAdHoc.ID.Value, poljaAdHoc.RowVer, poljaAdHoc.TekstIliNull("Napomena"))))
            {
                poljaAdHoc.Popuni(null);
                var _ = Osvezi();
            }
        }
    }
}
