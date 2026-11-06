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
        private readonly WebBrowser mapa = new WebBrowser { ScriptErrorsSuppressed = true };
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
        private bool mapaUcitana;
        private readonly CheckBox chkLokomotive = new CheckBox { Text = "Lokomotive na mapi (GPS)", AutoSize = true, Checked = true };
        private readonly Label lblGps = new Label { AutoSize = true, MaximumSize = new System.Drawing.Size(SirinaPoljaRute - 30, 0) };
        private readonly Timer tajmerGps = new Timer { Interval = 60000 };
        private List<PozicijaLokomotive> pozicije;
        private bool gpsUToku;
        private bool gpsPrviPut = true;

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
                mapaUcitana = false;
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

        private async Task PrikaziMapu()
        {
            if (mapaUcitana || stanice == null) return;
            mapaUcitana = true;
            DataTable sveStanice = stanice;
            await EkranOP.Ucitaj(this, StaniceRepo.Veze, veze =>
            {
                mapa.ObjectForScripting = new MostMape(this);
                mapa.DocumentText = HtmlMape(sveStanice, veze);
            });
        }

        // Pozivi iz JavaScript-a mape stižu na UI niti.
        [ComVisible(true)]
        public sealed class MostMape
        {
            private readonly frmOPStanice forma;

            internal MostMape(frmOPStanice forma)
            {
                this.forma = forma;
            }

            public void PomeriStanicu(int id, double lat, double lon)
            {
                forma.PomeriStanicu(id, lat, lon);
            }

            public void IzaberiStanicu(int id)
            {
                forma.IzaberiNaMapi(id);
            }

            public void ZatvoriVezu(int id)
            {
                forma.ZameniZatvor(id);
            }
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
            if (mapa.Document != null) mapa.Document.InvokeScript("oznaciZatvor", new object[] { vezaId, zatvorena });
            lblRuta.Text = "Ručno zatvorenih deonica: " + rucnoZatvorene.Count + ". Zatvor važi dok je ekran otvoren.";
        }

        // ---------- Lokomotive (GPS) ----------

        private void PrebaciLokomotive()
        {
            if (chkLokomotive.Checked)
            {
                gpsPrviPut = true;
                tajmerGps.Start();
                var _ = OsveziLokomotive();
                return;
            }
            tajmerGps.Stop();
            pozicije = null;
            lblGps.Text = "";
            PozoviMapu("crtajLokomotive", "[]", false);
        }

        // Greška GPS servisa ide u natpis, a ne u poruku, jer se osvežava svakog minuta.
        private async Task OsveziLokomotive()
        {
            if (gpsUToku || !chkLokomotive.Checked) return;
            gpsUToku = true;
            if (pozicije == null) lblGps.Text = "Učitavam pozicije lokomotiva...";
            try
            {
                List<PozicijaLokomotive> lista = await Task.Run(() => GpsLokomotive.Ucitaj());
                if (IsDisposed || !chkLokomotive.Checked) return;
                pozicije = lista;
                CrtajLokomotive();
                var bez = lista.Where(p => !p.ImaPoziciju).Select(p => p.Oznaka).ToList();
                lblGps.Text = "Na mapi: " + lista.Count(p => p.ImaPoziciju) + " od " + lista.Count + " lokomotiva."
                    + (bez.Count > 0 ? " Bez pozicije u poslednja 24 h: " + string.Join(", ", bez) + "." : "")
                    + " Osveženo u " + DateTime.Now.ToString("HH:mm") + ", ponovo za minut.";
            }
            catch (Exception ex)
            {
                if (!IsDisposed) lblGps.Text = "GPS servis nije dostupan: " + (ex is TaskCanceledException ? "isteklo je vreme čekanja." : ex.Message);
            }
            finally
            {
                gpsUToku = false;
            }
        }

        private void CrtajLokomotive()
        {
            if (pozicije == null || !mapaUcitana) return;
            DateTime granica = DateTime.Now - GpsLokomotive.Svez;
            var n = CultureInfo.InvariantCulture;
            var tacke = pozicije.Where(p => p.ImaPoziciju).Select(p => new
            {
                n = p.Oznaka,
                a = p.Lat.Value,
                o = p.Lon.Value,
                s = p.Vreme.HasValue && p.Vreme.Value >= granica ? 1 : 0,
                b = BojaBrzine(p.Brzina),
                d = "<b>" + System.Net.WebUtility.HtmlEncode(p.Oznaka) + "</b>"
                    + (p.Model != null ? "<br>" + System.Net.WebUtility.HtmlEncode(p.Model) : "")
                    + (p.Vreme.HasValue ? "<br>Signal: " + p.Vreme.Value.ToString("dd.MM.yyyy HH:mm:ss") : "")
                    + (p.Brzina.HasValue ? "<br>Brzina: " + p.Brzina.Value.ToString("0", n) + " km/h" : "")
            });
            if (PozoviMapu("crtajLokomotive", Newtonsoft.Json.JsonConvert.SerializeObject(tacke), gpsPrviPut)) gpsPrviPut = false;
        }

        // Preko 5 km/h vozi (zelena), 0 stoji (crvena), između manevriše (plava). Brzina ispod 1 km/h računa se kao 0.
        private static string BojaBrzine(double? brzina)
        {
            if (!brzina.HasValue) return "#8A7C7F";
            if (brzina.Value > 5) return "#2E8B3A";
            if (brzina.Value < 1) return "#D7261E";
            return "#0B6E99";
        }

        private bool PozoviMapu(string funkcija, params object[] argumenti)
        {
            if (mapa.Document == null) return false;
            try
            {
                // Funkcija vraća 1; null znači da se stranica još učitava, pa DocumentCompleted crta ponovo.
                return mapa.Document.InvokeScript(funkcija, argumenti) != null;
            }
            catch (Exception)
            {
                return false;
            }
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
                    BrojAlternativa = 3
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
                    if (mapa.Document != null) mapa.Document.InvokeScript("crtajRutu", new object[] { "" });
                    return;
                }
                lblRuta.Text = RutaTekst.OpisSaZastojima(rez.Rute) + Environment.NewLine + Environment.NewLine + PregledPruga.TekstRute(rez.Rute.Rute[0], rez.Deonice);
                if (mapa.Document != null)
                    mapa.Document.InvokeScript("crtajRutu", new object[] { string.Join(",", rez.Rute.Rute[0].Deonice.Select(d => d.VezaID)) });
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
            if (tabKombinacije.SelectedRows.Count != 1 || mapa.Document == null) return;
            var red = tabKombinacije.SelectedRows[0].DataBoundItem as DataRowView;
            if (red == null) return;
            int i = Convert.ToInt32(red["RB"]);
            if (i < 0 || i >= kombinacije.Count) return;
            mapa.Document.InvokeScript("crtajRutu", new object[] { string.Join(",", kombinacije[i].Ruta.Deonice.Select(d => d.VezaID)) });
        }

        // LINESTRING(lon lat, lon lat) iz baze u parove [lat,lon] za Leaflet.
        private static string TackeWkt(string wkt)
        {
            var n = CultureInfo.InvariantCulture;
            var sb = new StringBuilder("[");
            foreach (Match m in Regex.Matches(wkt, @"(-?\d+(?:\.\d+)?)\s+(-?\d+(?:\.\d+)?)"))
                sb.Append("[").Append(m.Groups[2].Value).Append(",").Append(m.Groups[1].Value).Append("],");
            return sb.Append("]").ToString().Replace(",]", "]");
        }

        private static string JsTekst(object o)
        {
            return Convert.ToString(o).Trim().Replace("\\", "\\\\").Replace("'", "\\'").Replace("<", "&lt;");
        }

        private static string HtmlMape(DataTable stanice, DataTable veze)
        {
            var n = CultureInfo.InvariantCulture;
            var tacke = new StringBuilder();
            foreach (DataRow r in stanice.Rows)
            {
                if (r.IsNull("Latitude") || r.IsNull("Longitude") || !Convert.ToBoolean(r["Aktivna"])) continue;
                tacke.Append("[").Append(Convert.ToDouble(r["Latitude"]).ToString(n)).Append(",").Append(Convert.ToDouble(r["Longitude"]).ToString(n)).Append(",'").Append(JsTekst(r["Naziv"])).Append("',")
                    .Append(Convert.ToInt32(r["ID"])).Append(",").Append(ZaProveru(r) ? 1 : 0).Append("],");
            }
            var linije = new StringBuilder();
            foreach (DataRow r in veze.Rows)
            {
                string tackeVeze = !r.IsNull("GeomWKT")
                    ? TackeWkt(Convert.ToString(r["GeomWKT"]))
                    : "[[" + Convert.ToDouble(r["LatA"]).ToString(n) + "," + Convert.ToDouble(r["LonA"]).ToString(n) + "],[" + Convert.ToDouble(r["LatB"]).ToString(n) + "," + Convert.ToDouble(r["LonB"]).ToString(n) + "]]";
                int vrsta = Convert.ToBoolean(r["KiM"]) ? 2 : !r.IsNull("Elektrificirana") && Convert.ToBoolean(r["Elektrificirana"]) ? 1 : 0;
                string opis = JsTekst(r["Pruga"]) + ": " + JsTekst(r["NazivA"]) + " - " + JsTekst(r["NazivB"]) + ", " + Convert.ToDecimal(r["DuzinaKm"]).ToString("0.0", n) + " km";
                linije.Append("[").Append(Convert.ToInt32(r["ID"])).Append(",").Append(vrsta).Append(",'").Append(opis).Append("',").Append(tackeVeze).Append("],");
            }
            // WebBrowser radi kao IE11, zato je ovde ES5, bez strelica i const.
            return "<!doctype html><html><head><meta charset='utf-8'><meta http-equiv='X-UA-Compatible' content='IE=edge'>" +
                "<link rel='stylesheet' href='https://unpkg.com/leaflet@1.7.1/dist/leaflet.css'><script src='https://unpkg.com/leaflet@1.7.1/dist/leaflet.js'></script>" +
                "<style>html,body,#m{height:100%;margin:0}.lokt{font:bold 11px sans-serif;padding:1px 4px}.leg{background:#F8F6F3;padding:6px 8px;font:12px sans-serif;border:1px solid #D2C9C6}.leg i{display:inline-block;width:20px;border-top:3px solid;vertical-align:middle;margin-right:6px}</style></head><body><div id='m'></div><script>" +
                "var m=L.map('m',{preferCanvas:true}).setView([44.2,20.9],7);" +
                "L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png',{attribution:'&copy; OpenStreetMap contributors'}).addTo(m);" +
                "var boje=['#8A7C7F','#51111C','#C9BFBC'],v=[" + linije + "],l={};" +
                "function klik(id){return function(){window.external.ZatvoriVezu(id);};}" +
                "for(var i=0;i<v.length;i++){l[v[i][0]]=L.polyline(v[i][3],{color:boje[v[i][1]],weight:2}).bindTooltip(v[i][2]).on('click',klik(v[i][0])).addTo(m);l[v[i][0]].boja=boje[v[i][1]];l[v[i][0]].zatvor=false;}" +
                "var s=[" + tacke + "],ruta=[],vuceni=null;" +
                "function izbor(t){return function(){window.external.IzaberiStanicu(t[3]);if(vuceni)m.removeLayer(vuceni);" +
                "vuceni=L.marker([t[0],t[1]],{draggable:true}).bindTooltip(t[2]+' (prevuci)').addTo(m);" +
                "vuceni.on('dragend',function(e){var p=e.target.getLatLng();window.external.PomeriStanicu(t[3],p.lat,p.lng);});};}" +
                "for(var j=0;j<s.length;j++)L.circleMarker([s[j][0],s[j][1]],{radius:s[j][4]?2.5:2,weight:1,color:s[j][4]?'#C8962B':'#51111C',fillOpacity:0.9}).bindTooltip(s[j][2]).on('click',izbor(s[j])).addTo(m);" +
                "function stil(a){a.setStyle({color:a.zatvor?'#E0301E':(a.uRuti?'#0B6E99':a.boja),weight:a.uRuti||a.zatvor?5:2,dashArray:a.zatvor?'6,6':null});}" +
                "function oznaciZatvor(id,z){l[id].zatvor=z;stil(l[id]);}" +
                "function crtajRutu(ids){for(var i=0;i<ruta.length;i++){l[ruta[i]].uRuti=false;stil(l[ruta[i]]);}ruta=ids?ids.split(','):[];var g=null;" +
                "for(var k=0;k<ruta.length;k++){var a=l[ruta[k]];if(!a)continue;a.uRuti=true;stil(a);g=g?g.extend(a.getBounds()):L.latLngBounds(a.getBounds().getSouthWest(),a.getBounds().getNorthEast());}if(g)m.fitBounds(g);}" +
                "var lok=L.layerGroup().addTo(m);" +
                "function crtajLokomotive(js,fit){lok.clearLayers();var a=JSON.parse(js),g=[];for(var i=0;i<a.length;i++){var t=a[i];" +
                "L.circleMarker([t.a,t.o],{radius:7,weight:2,color:'#FFFFFF',fillColor:t.b,fillOpacity:t.s?1:0.45}).bindTooltip(t.n,{permanent:true,direction:'right',offset:[8,0],className:'lokt'}).bindPopup(t.d).addTo(lok);g.push([t.a,t.o]);}" +
                "if(fit&&g.length)m.fitBounds(L.latLngBounds(g).pad(0.2),{maxZoom:10});return 1;}" +
                "var leg=L.control({position:'bottomleft'});leg.onAdd=function(){var d=L.DomUtil.create('div','leg');" +
                "d.innerHTML='<i style=\"border-color:#51111C\"></i>Elektrificirana<br><i style=\"border-color:#8A7C7F\"></i>Bez kontaktne mreže ili nepoznato<br><i style=\"border-color:#C9BFBC\"></i>KiM<br><i style=\"border-color:#0B6E99\"></i>Ruta<br>Žuta tačka: koordinata za proveru. Klik na prugu: zatvor. Klik na tačku pa prevlačenje: nove koordinate.<br>Lokomotive (GPS): <span style=\"color:#2E8B3A\">&#9679;</span> preko 5 km/h <span style=\"color:#0B6E99\">&#9679;</span> 1-5 km/h <span style=\"color:#D7261E\">&#9679;</span> stoji. Bled krug: signal stariji od 15 min.';return d;};leg.addTo(m);" +
                "</script></body></html>";
        }
    }
}
