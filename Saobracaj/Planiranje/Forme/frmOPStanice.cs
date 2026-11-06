using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Podaci;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPStanice : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmOPStanice";
        public bool Pravo { get; }

        private readonly DataGridView tabela = EkranOP.NovaTabela("ID", "IzvorniID", "VrstaKod", "DuplikatOd", "NazivStari", "NazivRegistar", "KiM", "Posednutost", "OtvorenaZa", "Vaga", "Rampa");
        private readonly Kontrole.KontrolaMape mapa = new Kontrole.KontrolaMape { DozvoliPomeranje = true, ZaProveru = ZaProveru };
        private readonly UredjivacReda polja = new UredjivacReda("Stanica");
        private readonly UredjivacReda poljaRegistar = new UredjivacReda("Podaci iz Registra");
        private readonly UredjivacReda poljaRuta = new UredjivacReda("Ruta");
        private readonly CheckBox chkSamoSrb = new CheckBox { Text = "Samo SRB" };
        private readonly CheckBox chkSamoStanice = new CheckBox { Text = "Samo stanice" };
        private readonly CheckBox chkZaProveru = new CheckBox { Text = "Za proveru" };
        private readonly TextBox lblRuta = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None };
        private readonly DataGridView tabKombinacije = EkranOP.NovaTabela("RB");
        private List<KombinacijaTrasa> kombinacije = new List<KombinacijaTrasa>();
        private readonly HashSet<int> rucnoZatvorene = new HashSet<int>();
        private const int DanaNajava = 90;
        private readonly DataGridView tabPruge = EkranOP.NovaTabela("PrugaID");
        private readonly DataGridView tabDeonicePruge = EkranOP.NovaTabela();
        private readonly CheckBox chkSamoKoriscene = new CheckBox { Text = "Samo pruge koje se koriste", Checked = true, AutoSize = true };
        private readonly Label lblPruge = new Label { AutoSize = true };
        private PregledPrugaRezultat pregledPruga;
        private List<DeonicaPruge> deonicePruga;
        private bool prugeUcitane;
        private DataTable stanice;
        private bool mapaZastarela;

        public frmOPStanice()
        {
            InitializeComponent();
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        private async Task Osvezi()
        {
            await EkranOP.Ucitaj(this, () => new { Sve = StaniceRepo.Sve(false), Izbor = StaniceRepo.ZaRutu() }, t =>
            {
                stanice = t.Sve;
                PrimeniFiltere();
                poljaRuta.IzvorIzbora("OdID", t.Izbor, "Naziv", "ID", false);
                poljaRuta.IzvorIzbora("DoID", t.Izbor, "Naziv", "ID", false);
                mapaZastarela = true;
                prugeUcitane = false;
            });
        }

        // Filter ide preko kopije, jer DefaultView koriste filteri kolona.
        private void PrimeniFiltere()
        {
            if (stanice == null) return;
            IEnumerable<DataRow> redovi = stanice.Rows.Cast<DataRow>();
            if (chkSamoSrb.Checked) redovi = redovi.Where(r => string.Equals(Convert.ToString(r["Drzava"]).Trim(), "SRB", StringComparison.OrdinalIgnoreCase));
            if (chkSamoStanice.Checked) redovi = redovi.Where(r => !r.IsNull("VrstaKod") && (Convert.ToInt32(r["VrstaKod"]) == 1 || Convert.ToInt32(r["VrstaKod"]) == 2));
            if (chkZaProveru.Checked) redovi = redovi.Where(ZaProveru);
            DataTable prikaz = stanice.Clone();
            foreach (DataRow r in redovi) prikaz.ImportRow(r);
            tabela.DataSource = prikaz;
        }

        // Koordinata koja nije iz OSM tačke istog naziva, ni ručno potvrđena, traži proveru.
        private static bool ZaProveru(DataRow r)
        {
            string izvor = Convert.ToString(r["KoordIzvor"]).Trim();
            return izvor != "OSM" && izvor != "RUCNO" && string.Equals(Convert.ToString(r["Drzava"]).Trim(), "SRB", StringComparison.OrdinalIgnoreCase);
        }

        private void PopuniIzabrani()
        {
            DataRow r = EkranOP.Izabran(tabela);
            polja.Popuni(r);
            poljaRegistar.Popuni(r);
            bool izRegistra = r != null && Convert.ToString(r["Izvor"]).Trim() == "REGISTAR";
            polja.Omoguci("Naziv", !izRegistra);
            polja.Omoguci("SifraIZS", !izRegistra);
            polja.Omoguci("UIC", !izRegistra);
        }

        private static int? ProcitajSifru(string tekst)
        {
            if (string.IsNullOrWhiteSpace(tekst)) return null;
            int? sifra = NaziviRegistra.Sifra(tekst);
            if (!sifra.HasValue) throw new FormatException("Šifra IŽS ima pet cifara.");
            return sifra;
        }

        private void Sacuvaj()
        {
            string naziv = polja.Tekst("Naziv");
            if (naziv.Length == 0)
            {
                EkranOP.Poruka(this, "Unesite naziv stanice.");
                return;
            }
            bool uspeh = EkranOP.Izvrsi(this, () =>
            {
                double? lat = polja.DoubleIliNull("Latitude", "Geografska širina"), lon = polja.DoubleIliNull("Longitude", "Geografska dužina");
                string drzava = polja.TekstIliNull("Drzava");
                string greska = NaziviRegistra.GreskaKoordinata(drzava, lat, lon);
                if (greska != null) throw new FormatException(greska);
                int? sifra = ProcitajSifru(polja.Tekst("SifraIZS"));
                if (polja.ID.HasValue)
                    StaniceRepo.Sacuvaj(polja.ID.Value, polja.RowVer, naziv, sifra, polja.TekstIliNull("UIC"), drzava, polja.TekstIliNull("VrstaMesta"), lat, lon, polja.Cek("Granicna"), polja.Cek("Aktivna"));
                else
                    StaniceRepo.Dodaj(naziv, sifra, polja.TekstIliNull("UIC"), drzava, polja.TekstIliNull("VrstaMesta"), lat, lon, polja.Cek("Granicna"), polja.Cek("Aktivna"));
            });
            if (uspeh) _ = Osvezi();
        }

        private void Novi()
        {
            tabela.ClearSelection();
            polja.Isprazni();
            polja.Popuni(null);
            poljaRegistar.Popuni(null);
            polja.Omoguci("Naziv", true);
            polja.Omoguci("SifraIZS", true);
            polja.Omoguci("UIC", true);
        }

        private void SpojiDuplikate()
        {
            var izabrani = tabela.SelectedRows.Cast<DataGridViewRow>().OrderBy(r => r.Index).Select(r => ((DataRowView)r.DataBoundItem).Row).ToList();
            if (izabrani.Count != 2)
            {
                EkranOP.Poruka(this, "Izaberite tačno dve stanice (Ctrl + klik). Prva u tabeli ostaje, a druga postaje neaktivna.");
                return;
            }
            DataRow prva = izabrani[0], druga = izabrani[1];
            if (!EkranOP.Potvrdi(this, "Sve reference na „" + druga["Naziv"] + "“ prelaze na „" + prva["Naziv"] + "“, a „" + druga["Naziv"] + "“ postaje neaktivna. Nastaviti?"))
                return;
            if (EkranOP.Izvrsi(this, () => StaniceRepo.Spoji(Convert.ToInt32(prva["ID"]), Convert.ToInt32(druga["ID"]))))
                _ = Osvezi();
        }

        private void Dopuni()
        {
            int dodato = 0;
            if (EkranOP.Izvrsi(this, () => dodato = StaniceRepo.DopuniIzStarog()))
            {
                EkranOP.Poruka(this, "Dodato novih stanica: " + dodato + ".");
                _ = Osvezi();
            }
        }

        // Uvoz ima jedan put: ekran Registar pruga, sa pregledom razlika pre potvrde.
        private void UveziRegistar()
        {
            var forma = new frmOPRegistar();
            if (!forma.Pravo) { forma.Dispose(); return; }
            forma.FormClosed += (s, e) => { if (!IsDisposed) { var _ = Osvezi(); } };
            forma.Show(this);
        }

        // ---------- Pruge ----------

        private async Task PrikaziPruge()
        {
            if (prugeUcitane) return;
            prugeUcitane = true;
            await EkranOP.Ucitaj(this, () =>
            {
                List<DeonicaPruge> deonice = StaniceRepo.DeonicePruga();
                var trase = TraseRepo.ZaKombinacije(DateTime.Today).Select(t => new ParUpotrebe { OdID = t.OdID, DoID = t.DoID, Broj = 1 });
                return new { Deonice = deonice, Pregled = PregledPruga.Izracunaj(deonice, trase.ToList(), NajaveRepo.ParoviOd(DateTime.Today.AddDays(-DanaNajava))) };
            }, r =>
            {
                deonicePruga = r.Deonice;
                pregledPruga = r.Pregled;
                PopuniPruge();
            });
        }

        private void PopuniPruge()
        {
            if (pregledPruga == null) return;
            var t = new DataTable();
            t.Columns.Add("PrugaID", typeof(int));
            foreach (string k in new[] { "Pruga", "Naziv pruge", "Kategorija", "Elektrificirana" }) t.Columns.Add(k);
            foreach (string k in new[] { "Km", "Elektro km", "Bez mreže km", "Nepoznato km" }) t.Columns.Add(k, typeof(decimal));
            foreach (string k in new[] { "Trase", "Najave" }) t.Columns.Add(k, typeof(int));
            foreach (RedPruge r in pregledPruga.Redovi.Where(x => x.Koristi || !chkSamoKoriscene.Checked))
                t.Rows.Add(r.PrugaID, r.Sifra, r.Naziv, r.Kategorija, r.Elektrifikacija, Math.Round(r.Km, 1), Math.Round(r.KmElektro, 1), Math.Round(r.KmBezMreze, 1), Math.Round(r.KmNepoznato, 1), r.Trase, r.Najave);
            tabPruge.DataSource = t;
            int koristi = pregledPruga.Redovi.Count(r => r.Koristi);
            lblPruge.Text = "Koristi se " + koristi + " od " + pregledPruga.Redovi.Count + " pruga: važeće trase i najave iz poslednjih " + DanaNajava + " dana, po najkraćoj ruti. Bez rute: trasa " + pregledPruga.TraseBezRute + ", najava " + pregledPruga.NajaveBezRute + ".";
        }

        private void PrikaziDeonicePruge()
        {
            DataRow izabrana = EkranOP.Izabran(tabPruge);
            if (izabrana == null || deonicePruga == null) return;
            int id = Convert.ToInt32(izabrana["PrugaID"]);
            var t = new DataTable();
            foreach (string k in new[] { "Od", "Do" }) t.Columns.Add(k);
            t.Columns.Add("Km", typeof(decimal));
            foreach (string k in new[] { "Elektrificirana", "Klasa", "Vmax" }) t.Columns.Add(k);
            foreach (DeonicaPruge d in deonicePruga.Where(x => x.PrugaID == id))
                t.Rows.Add(d.OdNaziv, d.DoNaziv, Math.Round(d.Km, 1), d.Elektrificirana == true ? "Da" : d.Elektrificirana == false ? "Ne" : "Nepoznato", d.Klasa, d.Vmax.HasValue ? d.Vmax.Value + " km/h" : "");
            tabDeonicePruge.DataSource = t;
        }

        // ---------- Mapa ----------

        // Mapa se gradi kad se tab prvi put otvori, i ponovo posle osvežavanja stanica.
        private async Task PrikaziMapu()
        {
            if (stanice == null) return;
            if (mapa.Ucitana && !mapaZastarela) { mapa.Aktiviraj(); return; }
            mapaZastarela = false;
            DataTable sveStanice = stanice;
            await EkranOP.Ucitaj(this, StaniceRepo.Veze, veze => mapa.Prikazi(sveStanice, veze));
        }

        private void PomeriStanicu(int id, double lat, double lon)
        {
            DataRow r = stanice.Rows.Cast<DataRow>().FirstOrDefault(x => Convert.ToInt32(x["ID"]) == id);
            if (r == null) return;
            string greska = NaziviRegistra.GreskaKoordinata(Convert.ToString(r["Drzava"]), lat, lon);
            if (greska != null) { EkranOP.Poruka(this, greska); return; }
            EkranOP.Izvrsi(this, () =>
            {
                r["RowVer"] = StaniceRepo.PomeriNaMapi(id, (byte[])r["RowVer"], lat, lon);
                r["Latitude"] = lat;
                r["Longitude"] = lon;
                r["KoordIzvor"] = "RUCNO";
                PrimeniFiltere();
            });
        }

        private void IzaberiNaMapi(int id)
        {
            foreach (DataGridViewRow red in tabela.Rows)
            {
                var v = red.DataBoundItem as DataRowView;
                if (v == null || Convert.ToInt32(v["ID"]) != id) continue;
                tabela.ClearSelection();
                red.Selected = true;
                tabela.CurrentCell = red.Cells.Cast<DataGridViewCell>().FirstOrDefault(c => c.Visible);
                return;
            }
        }

        private void ZameniZatvor(int vezaId)
        {
            bool zatvorena = rucnoZatvorene.Add(vezaId);
            if (!zatvorena) rucnoZatvorene.Remove(vezaId);
            mapa.OznaciZatvor(vezaId, zatvorena);
            lblRuta.Text = "Ručno zatvorenih deonica: " + rucnoZatvorene.Count + ". Zatvor važi dok je ekran otvoren.";
        }

        private async Task IzracunajRutu()
        {
            int? od = poljaRuta.IzabranID("OdID"), doStanice = poljaRuta.IzabranID("DoID");
            if (!od.HasValue || !doStanice.HasValue) return;
            ZahtevRute z;
            decimal? brutoT;
            try
            {
                brutoT = poljaRuta.DecimalIliNull("BrutoT", "Bruto voza");
                string osovinsko = poljaRuta.Tekst("Osovinsko");
                z = new ZahtevRute
                {
                    OdID = od.Value,
                    DoID = doStanice.Value,
                    Elektricna = poljaRuta.Tekst("Vuca") == "Električna",
                    OsovinskoT = char.IsDigit(osovinsko.FirstOrDefault()) ? decimal.Parse(osovinsko.Split(' ')[0], CultureInfo.InvariantCulture) : (decimal?)null,
                    DuzinaM = poljaRuta.DecimalIliNull("DuzinaM", "Dužina voza"),
                    Datum = poljaRuta.DatumVreme("Datum").Date,
                    SaKiM = poljaRuta.Cek("SaKiM"),
                    RucnoZatvorene = new HashSet<int>(rucnoZatvorene),
                    BrojAlternativa = 3,
                    // Za današnji datum zatvori koji su već prošli ne smetaju.
                    Polazak = DateTime.Now
                };
            }
            catch (FormatException ex)
            {
                EkranOP.Poruka(this, ex.Message);
                return;
            }
            await EkranOP.Ucitaj(this, () =>
            {
                PlanKontekst k = PlanRepo.Ucitaj(z.Datum, z.Datum.AddDays(1));
                RuteSaZastojima rute = Rutiranje.IzracunajSaZastojima(k, z);
                List<KombinacijaTrasa> kombinacijeTrasa = KombinacijeTrasa.SaZastojima(k, rute, z, TraseRepo.ZaKombinacije(z.Datum), brutoT);
                return new { Rute = rute, Kombinacije = kombinacijeTrasa, Nazivi = new Dictionary<int, string>(k.NaziviStanica), Deonice = StaniceRepo.DeonicePruga() };
            }, rez =>
            {
                PrikaziKombinacije(rez.Kombinacije, rez.Nazivi);
                if (rez.Rute.Rute.Count == 0)
                {
                    lblRuta.Text = "Između ovih stanica nema pruge" + (rucnoZatvorene.Count > 0 ? " uz ručno zatvorene deonice." : ".");
                    mapa.CrtajRute(null, null);
                    return;
                }
                lblRuta.Text = RutaTekst.OpisSaZastojima(rez.Rute) + Environment.NewLine + Environment.NewLine + PregledPruga.TekstRute(rez.Rute.Rute[0], rez.Deonice);
                mapa.CrtajRute(null, rez.Rute.Rute[0].Deonice.Select(d => d.VezaID));
            });
        }

        private void PrikaziKombinacije(List<KombinacijaTrasa> lista, Dictionary<int, string> nazivi)
        {
            kombinacije = lista;
            tabKombinacije.DataSource = EkranOP.TabelaKombinacija(lista, nazivi);
        }

        // Izabrana kombinacija crta svoju rutu, jer alternativna ruta može imati bolje trase.
        private void CrtajKombinaciju()
        {
            if (tabKombinacije.SelectedRows.Count != 1) return;
            var red = tabKombinacije.SelectedRows[0].DataBoundItem as DataRowView;
            if (red == null) return;
            int i = Convert.ToInt32(red["RB"]);
            if (i < 0 || i >= kombinacije.Count) return;
            mapa.CrtajRute(null, kombinacije[i].Ruta.Deonice.Select(d => d.VezaID));
        }

    }
}
