using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Kontrole;
using Saobracaj.Planiranje.Modeli;
using Saobracaj.Planiranje.Podaci;
using Saobracaj.Planiranje.Pravila;
using Saobracaj.Tema;

namespace Saobracaj.Planiranje.Forme
{
    // Mapa lokomotiva: gde je izabrana lokomotiva (GPS), kojim trasama stiže do početne stanice i kako dalje do krajnje.
    // Zatvori iz telegrama se obilaze, a električna lokomotiva ide samo elektrificiranim prugama dok god takav put postoji.
    public partial class frmOPMapa : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmOPMapa";
        public bool Pravo { get; }

        private const int VisinaPredloga = 220;
        private const string BojaLokomotive = "#2E8B3A", BojaPocetne = "#8E44AD", BojaKrajnje = "#00A651";

        private readonly KontrolaMape mapa = new KontrolaMape
        {
            DodatakLegende = "Krug sa natpisom: lokomotiva, početna i krajnja stanica."
        };
        private readonly UredjivacReda polja = new UredjivacReda("Put lokomotive");
        private readonly Label lblLokacija = new Label { AutoSize = true, MaximumSize = new System.Drawing.Size(KontrolaMape.SirinaKolone - 30, 0), Padding = new Padding(8, 0, 8, 6) };
        private readonly TextBox lblRuta = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None };
        private readonly DataGridView tabPredlozi = EkranOP.NovaTabela("RB");
        private readonly HashSet<int> rucnoZatvorene = new HashSet<int>();

        private DataTable stanice, lokomotive, staniceZaRutu;
        private List<KombinacijaTrasa> predlozi = new List<KombinacijaTrasa>();
        private int brojPrilaza;
        private IEnumerable<int> prilazVeze, putVeze;
        // Lokacija je ručno izabrana: osvežen GPS je više ne menja, dok se ne izabere druga lokomotiva.
        private bool lokacijaRucno, postavljam;

        public frmOPMapa()
        {
            InitializeComponent();
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Tabla", "Mapa lokomotiva", null);

            polja.Izbor("LokID", "Lokomotiva")
                .Izbor("TrenutnaID", "Lokomotiva je u stanici")
                .Izbor("OdID", "Početna stanica")
                .Izbor("DoID", "Krajnja stanica")
                .BrojPrazno("BrutoT", "Bruto voza (t)")
                .BrojPrazno("DuzinaM", "Dužina voza (m)")
                .Datum("Datum", "Datum (zatvori iz telegrama)")
                .Cekiranja("SaKiM", "Pruge na KiM")
                .Dugmad(EkranOP.Dugme("Izračunaj put", (s, e) => { var _ = Izracunaj(); }));
            polja.Isprazni();
            ((ComboBox)polja.Polje("LokID")).SelectedIndexChanged += (s, e) => { if (!postavljam) { lokacijaRucno = false; OdrediLokaciju(); } };
            ((ComboBox)polja.Polje("TrenutnaID")).SelectedIndexChanged += (s, e) => { if (!postavljam) { lokacijaRucno = true; OznaciTacke(); } };
            ((ComboBox)polja.Polje("OdID")).SelectedIndexChanged += (s, e) => { if (!postavljam) OznaciTacke(); };
            ((ComboBox)polja.Polje("DoID")).SelectedIndexChanged += (s, e) => { if (!postavljam) OznaciTacke(); };

            // Leva kolona: slojevi mape, polja, lokacija lokomotive i opis puta.
            var levo = new Panel { Dock = DockStyle.Left, Width = KontrolaMape.SirinaKolone, AutoScroll = true };
            lblRuta.Dock = DockStyle.Fill;
            levo.Controls.Add(lblRuta);
            lblLokacija.Dock = DockStyle.Top;
            levo.Controls.Add(lblLokacija);
            polja.Kartica.Dock = DockStyle.Top;
            levo.Controls.Add(polja.Kartica);
            polja.Kartica.Height = polja.Kartica.Rasporedi(KontrolaMape.SirinaKolone - SystemInformation.VerticalScrollBarWidth);
            levo.Controls.Add(mapa.PanelSlojeva);

            tabPredlozi.Dock = DockStyle.Bottom;
            tabPredlozi.Height = VisinaPredloga;
            tabPredlozi.SelectionChanged += (s, e) => CrtajIzabrani();
            EkranOP.StilKombinacija(tabPredlozi);
            mapa.Dock = DockStyle.Fill;
            mapa.VezaKliknuta += ZameniZatvor;
            mapa.PozicijeOsvezene += () => { if (!lokacijaRucno) OdrediLokaciju(); };

            var sadrzaj = new Panel();
            sadrzaj.Controls.Add(mapa);
            sadrzaj.Controls.Add(tabPredlozi);
            sadrzaj.Controls.Add(levo);

            Load += (s, e) => { var _ = Ucitaj(); };
            izgled.Zavrsi(sadrzaj, 1400, 860);
            // Mapa zauzima ceo ekran, kao tab Mapa na ekranu Mreža i stanice.
            Control okvir = sadrzaj.Parent;
            okvir.Dock = DockStyle.Fill;
            izgled.Mreza.Visible = false;
            foreach (Splitter podela in Controls.OfType<Splitter>()) podela.Visible = false;
        }

        private async Task Ucitaj()
        {
            await EkranOP.Ucitaj(this, () => new
            {
                Stanice = StaniceRepo.Sve(false),
                Izbor = StaniceRepo.ZaRutu(),
                Lokomotive = LokomotiveRepo.Lokomotive(false),
                Veze = StaniceRepo.Veze()
            }, d =>
            {
                stanice = d.Stanice;
                staniceZaRutu = d.Izbor;
                lokomotive = d.Lokomotive.Copy();
                lokomotive.Columns.Add("Prikaz", typeof(string));
                foreach (DataRow r in lokomotive.Rows)
                    r["Prikaz"] = Convert.ToString(r["Broj"]).Trim() + " · " + Convert.ToString(r["Serija"]).Trim() + " · " + NazivVuce(Convert.ToString(r["VrstaVuce"]));
                postavljam = true;
                polja.IzvorIzbora("LokID", lokomotive, "Prikaz", "ID", true);
                polja.IzvorIzbora("TrenutnaID", staniceZaRutu, "Naziv", "ID", true);
                polja.IzvorIzbora("OdID", staniceZaRutu, "Naziv", "ID", true);
                polja.IzvorIzbora("DoID", staniceZaRutu, "Naziv", "ID", true);
                postavljam = false;
                lblLokacija.Text = "Izaberite lokomotivu. Lokacija se uzima sa GPS-a, a bez njega iz matične stanice.";
                mapa.Prikazi(d.Stanice, d.Veze);
            });
        }

        private static string NazivVuce(string vuca)
        {
            switch ((vuca ?? "").Trim())
            {
                case "E": return "električna";
                case "D": return "dizel";
                default: return "vuča nepoznata";
            }
        }

        private DataRow IzabranaLokomotiva()
        {
            int? id = polja.IzabranID("LokID");
            return id.HasValue && lokomotive != null ? lokomotive.Rows.Cast<DataRow>().FirstOrDefault(r => Convert.ToInt32(r["ID"]) == id.Value) : null;
        }

        // GPS pozicija vezana za najbližu stanicu sa prugom; bez GPS-a matična stanica.
        private void OdrediLokaciju()
        {
            DataRow lok = IzabranaLokomotiva();
            if (lok == null) { OznaciTacke(); return; }
            string broj = Convert.ToString(lok["Broj"]).Trim();
            GpsLokacija g = null;
            if (mapa.Pozicije != null && stanice != null)
            {
                var koordinate = new Dictionary<int, double[]>();
                foreach (DataRow s in stanice.Rows)
                    if (!s.IsNull("Latitude") && !s.IsNull("Longitude") && Convert.ToBoolean(s["Aktivna"]))
                        koordinate[Convert.ToInt32(s["ID"])] = new[] { Convert.ToDouble(s["Latitude"]), Convert.ToDouble(s["Longitude"]) };
                var dostupne = new HashSet<int>(staniceZaRutu.Rows.Cast<DataRow>().Select(r => Convert.ToInt32(r["ID"])));
                var jedna = new List<LokomotivaOP> { new LokomotivaOP { ID = Convert.ToInt32(lok["ID"]), Broj = broj } };
                var tacke = mapa.Pozicije.Where(p => p.ImaPoziciju).Select(p => new GpsLokacija { Oznaka = p.Oznaka, Lat = p.Lat.Value, Lon = p.Lon.Value, Vreme = p.Vreme, Brzina = p.Brzina });
                GpsLokacije.Povezi(jedna, tacke.ToList(), koordinate, dostupne).TryGetValue(jedna[0].ID, out g);
            }
            int? stanica;
            if (g != null && g.StanicaID.HasValue)
            {
                stanica = g.StanicaID;
                lblLokacija.Text = "Lokacija " + broj + " sa GPS-a: kod " + NazivStanice(stanica.Value) + " (" + g.KmDoStanice.Value.ToString("0.0", CultureInfo.GetCultureInfo("sr-Latn-RS")) + " km)"
                    + (g.Vreme.HasValue ? ", signal " + g.Vreme.Value.ToString("dd.MM. HH:mm") : "") + (g.Brzina.HasValue ? ", " + g.Brzina.Value.ToString("0") + " km/h" : "") + ".";
            }
            else if (!lok.IsNull("MaticnaStanicaID"))
            {
                stanica = Convert.ToInt32(lok["MaticnaStanicaID"]);
                lblLokacija.Text = (mapa.Pozicije == null ? "GPS pozicije se još učitavaju; do tada " : "Nema GPS pozicije za " + broj + "; ") + "uzeta je matična stanica " + NazivStanice(stanica.Value) + ".";
            }
            else
            {
                stanica = null;
                lblLokacija.Text = (mapa.Pozicije == null ? "GPS pozicije se još učitavaju. " : "Nema GPS pozicije za " + broj + ", a ni matične stanice. ") + "Izaberite stanicu u kojoj je lokomotiva.";
            }
            postavljam = true;
            polja.IzaberiID("TrenutnaID", stanica);
            postavljam = false;
            OznaciTacke();
        }

        private string NazivStanice(int id)
        {
            DataRow r = stanice?.Rows.Cast<DataRow>().FirstOrDefault(x => Convert.ToInt32(x["ID"]) == id);
            return r == null ? "?" : Convert.ToString(r["Naziv"]).Trim();
        }

        private void OznaciTacke()
        {
            var oznake = new List<Tuple<int, string, string>>();
            int? lok = polja.IzabranID("TrenutnaID"), od = polja.IzabranID("OdID"), doS = polja.IzabranID("DoID");
            DataRow l = IzabranaLokomotiva();
            if (lok.HasValue) oznake.Add(Tuple.Create(lok.Value, "Lokomotiva " + (l == null ? "" : Convert.ToString(l["Broj"]).Trim()), BojaLokomotive));
            if (od.HasValue) oznake.Add(Tuple.Create(od.Value, "Početna", BojaPocetne));
            if (doS.HasValue) oznake.Add(Tuple.Create(doS.Value, "Krajnja", BojaKrajnje));
            mapa.OznaciTacke(oznake);
        }

        private void ZameniZatvor(int vezaId)
        {
            bool zatvorena = rucnoZatvorene.Add(vezaId);
            if (!zatvorena) rucnoZatvorene.Remove(vezaId);
            mapa.OznaciZatvor(vezaId, zatvorena);
            lblRuta.Text = "Ručno zatvorenih deonica: " + rucnoZatvorene.Count + ". Zatvor važi dok je ekran otvoren; izračunajte put ponovo.";
        }

        private sealed class Rezultat
        {
            internal RuteSaZastojima Prilaz;
            internal RuteSaZastojima Put;
            internal List<KombinacijaTrasa> PredloziPrilaza = new List<KombinacijaTrasa>();
            internal List<KombinacijaTrasa> PredloziPuta = new List<KombinacijaTrasa>();
            internal Dictionary<int, string> Nazivi;
        }

        private async Task Izracunaj()
        {
            DataRow lok = IzabranaLokomotiva();
            int? trenutna = polja.IzabranID("TrenutnaID"), od = polja.IzabranID("OdID"), doS = polja.IzabranID("DoID");
            if (lok == null) { EkranOP.Poruka(this, "Izaberite lokomotivu."); return; }
            if (!trenutna.HasValue) { EkranOP.Poruka(this, "Izaberite stanicu u kojoj je lokomotiva."); return; }
            if (!od.HasValue || !doS.HasValue) { EkranOP.Poruka(this, "Izaberite početnu i krajnju stanicu."); return; }
            decimal? bruto, duzina;
            try
            {
                bruto = polja.DecimalIliNull("BrutoT", "Bruto voza");
                duzina = polja.DecimalIliNull("DuzinaM", "Dužina voza");
            }
            catch (FormatException ex)
            {
                EkranOP.Poruka(this, ex.Message);
                return;
            }
            bool elektro = Convert.ToString(lok["VrstaVuce"]).Trim() == "E";
            DateTime datum = polja.DatumVreme("Datum").Date;
            bool saKiM = polja.Cek("SaKiM");
            var zatvorene = new HashSet<int>(rucnoZatvorene);
            int odLok = trenutna.Value, pocetna = od.Value, krajnja = doS.Value;

            await EkranOP.Ucitaj(this, () =>
            {
                PlanKontekst k = PlanRepo.Ucitaj(datum, datum.AddDays(1));
                List<TrasaZaRutu> trase = TraseRepo.ZaKombinacije(datum).ToList();
                DateTime polazak = DateTime.Now;
                var rez = new Rezultat { Nazivi = new Dictionary<int, string>(k.NaziviStanica) };
                if (odLok != pocetna)
                {
                    // Lokomotiva sama (prazna vožnja): bez bruto i dužine voza.
                    var z1 = new ZahtevRute { OdID = odLok, DoID = pocetna, Elektricna = elektro, ObaveznaElektro = true, Datum = datum, Polazak = polazak, SaKiM = saKiM, RucnoZatvorene = zatvorene, BrojAlternativa = 3 };
                    rez.Prilaz = Rutiranje.IzracunajSaZastojima(k, z1);
                    rez.PredloziPrilaza = KombinacijeTrasa.SaZastojima(k, rez.Prilaz, z1, trase, null);
                    // Put kreće kad lokomotiva stigne: zatvori se gledaju od tog trenutka.
                    decimal brzina = k.Parametar("BRZINA_PRAZNE_VOZNJE_KMH", datum, null, null, null, 50);
                    decimal km = rez.Prilaz.Rute.Count > 0 ? rez.Prilaz.Rute[0].Km : 0;
                    if (brzina > 0) polazak = polazak.AddHours((double)(km / brzina));
                }
                var z2 = new ZahtevRute { OdID = pocetna, DoID = krajnja, Elektricna = elektro, ObaveznaElektro = true, DuzinaM = duzina, Datum = datum, Polazak = polazak, SaKiM = saKiM, RucnoZatvorene = zatvorene, BrojAlternativa = 3 };
                rez.Put = Rutiranje.IzracunajSaZastojima(k, z2);
                rez.PredloziPuta = KombinacijeTrasa.SaZastojima(k, rez.Put, z2, trase, bruto);
                return rez;
            }, rez => Prikazi(rez, lok, odLok, pocetna, krajnja, elektro));
        }

        private void Prikazi(Rezultat rez, DataRow lok, int odLok, int pocetna, int krajnja, bool elektro)
        {
            string nl = Environment.NewLine;
            Func<int, string> naziv = id => { string n; return rez.Nazivi.TryGetValue(id, out n) ? n : "?"; };
            string broj = Convert.ToString(lok["Broj"]).Trim();
            string tekst = "Lokomotiva " + broj + " (" + NazivVuce(Convert.ToString(lok["VrstaVuce"])) + ")" + (elektro ? ": samo elektrificirane pruge, dok takav put postoji." : ".") + nl + nl;
            tekst += "1. PRILAZ: " + naziv(odLok) + " → " + naziv(pocetna) + nl;
            if (rez.Prilaz == null) tekst += "Lokomotiva je već u početnoj stanici.";
            else if (rez.Prilaz.Rute.Count == 0) tekst += "Između ovih stanica nema pruge" + (rucnoZatvorene.Count > 0 ? " uz ručno zatvorene deonice." : ".");
            else tekst += RutaTekst.OpisSaZastojima(rez.Prilaz);
            tekst += nl + nl + "2. PUT: " + naziv(pocetna) + " → " + naziv(krajnja) + nl;
            tekst += rez.Put.Rute.Count == 0 ? "Između ovih stanica nema pruge" + (rucnoZatvorene.Count > 0 ? " uz ručno zatvorene deonice." : ".") : RutaTekst.OpisSaZastojima(rez.Put);
            lblRuta.Text = tekst;

            // Jedna tabela: prvo predlozi za prilaz lokomotive, pa za put; RB je indeks u listi predloga.
            predlozi = rez.PredloziPrilaza.Concat(rez.PredloziPuta).ToList();
            brojPrilaza = rez.PredloziPrilaza.Count;
            DataTable t1 = EkranOP.TabelaKombinacija(rez.PredloziPrilaza, rez.Nazivi), t2 = EkranOP.TabelaKombinacija(rez.PredloziPuta, rez.Nazivi);
            DataTable t = t1.Clone();
            t.Columns.Add("Deo", typeof(string)).SetOrdinal(1);
            foreach (DataRow r in t1.Rows) t.Rows.Add(r.ItemArray.Take(1).Concat(new object[] { "1. Prilaz" }).Concat(r.ItemArray.Skip(1)).ToArray());
            foreach (DataRow r in t2.Rows)
            {
                DataRow n = t.Rows.Add(r.ItemArray.Take(1).Concat(new object[] { "2. Put" }).Concat(r.ItemArray.Skip(1)).ToArray());
                n["RB"] = brojPrilaza + Convert.ToInt32(r["RB"]);
            }
            prilazVeze = rez.Prilaz != null && rez.Prilaz.Rute.Count > 0 ? rez.Prilaz.Rute[0].Deonice.Select(d => d.VezaID).ToList() : null;
            putVeze = rez.Put.Rute.Count > 0 ? rez.Put.Rute[0].Deonice.Select(d => d.VezaID).ToList() : null;
            tabPredlozi.DataSource = t;
            tabPredlozi.ClearSelection();
            mapa.CrtajRute(prilazVeze, putVeze);
            OznaciTacke();
        }

        // Izabrani predlog menja samo svoj deo (prilaz ili put); drugi deo ostaje nacrtan.
        private void CrtajIzabrani()
        {
            if (tabPredlozi.SelectedRows.Count != 1) return;
            var red = tabPredlozi.SelectedRows[0].DataBoundItem as DataRowView;
            if (red == null) return;
            int i = Convert.ToInt32(red["RB"]);
            if (i < 0 || i >= predlozi.Count) return;
            List<int> veze = predlozi[i].Ruta.Deonice.Select(d => d.VezaID).ToList();
            if (i < brojPrilaza) prilazVeze = veze; else putVeze = veze;
            mapa.CrtajRute(prilazVeze, putVeze);
        }
    }
}
