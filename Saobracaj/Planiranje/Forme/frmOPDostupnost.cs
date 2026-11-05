using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Podaci;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPDostupnost : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmOPDostupnost";
        public bool Pravo { get; }

        private readonly DateTimePicker dtNedelja = new DateTimePicker { Format = DateTimePickerFormat.Short };
        private readonly ComboBox cboFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly DataGridView tabKalendar = EkranOP.NovaTabela("DeSifra");
        private readonly Label lblOsoba = new Label { Text = "Izaberite osobu u kalendaru." };
        private readonly Label lblZauzetost = new Label();

        private OsobljeKontekst kontekst;
        private DataTable kvalifikacije;
        private int? izabranaOsoba;

        public frmOPDostupnost()
        {
            InitializeComponent();
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        private DateTime Ponedeljak
        {
            get
            {
                DateTime d = dtNedelja.Value.Date;
                return d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
            }
        }

        private async Task Osvezi()
        {
            DateTime od = Ponedeljak, doD = od.AddDays(7);
            await EkranOP.Ucitaj(this, () =>
            {
                PlanKontekst p = PlanRepo.Ucitaj(od, doD);
                return ZadatakRepo.Kontekst(p, od, doD);
            }, k =>
            {
                kontekst = k;
                PrikaziKalendar();
                if (izabranaOsoba.HasValue) PrikaziOsobu(izabranaOsoba.Value);
            });
        }

        // Odsustvo je jače od smene, a zadatak se prikazuje uz nju.
        private string Celija(int osoba, DateTime dan)
        {
            OdsustvoOsobe o = kontekst.Odsustva.FirstOrDefault(x => x.DeSifra == osoba && x.Od.Date <= dan && x.Do.Date >= dan);
            if (o != null) return o.Vrsta == "BOLOVANJE" ? "Bolovanje" : o.Vrsta == "GODISNJI" ? "Godišnji" : o.Vrsta == "SLOBODAN" ? "Slobodan" : "Odsustvo";
            List<ZadatakOsobe> z = kontekst.Zadaci.Where(x => x.DeSifra == osoba && x.Aktivan && x.Od.Date <= dan && x.Do.Date >= dan).ToList();
            if (z.Count > 0) return "Posao " + string.Join(", ", z.Select(x => x.BrojNajave).Distinct());
            SmenaOsobe s = kontekst.Smene.FirstOrDefault(x => x.DeSifra == osoba && x.Datum.Date == dan);
            if (s == null) return "";
            return s.Vrsta == "DNEVNA" ? "Dnevna" : s.Vrsta == "NOCNA" ? "Noćna" : "Slobodan";
        }

        private void PrikaziKalendar()
        {
            if (kontekst == null) return;
            var t = new DataTable();
            t.Columns.Add("DeSifra", typeof(int));
            t.Columns.Add("Osoba");
            string[] dani = { "ned", "pon", "uto", "sre", "čet", "pet", "sub" };
            DateTime pon = Ponedeljak;
            for (int i = 0; i < 7; i++)
            {
                DateTime d = pon.AddDays(i);
                t.Columns.Add(dani[(int)d.DayOfWeek] + " " + d.ToString("dd.MM."));
            }
            int filter = cboFilter.SelectedIndex;
            foreach (OsobaPlan o in kontekst.Osobe.Where(x => filter <= 0 || (filter == 1 ? x.Masinovodja : x.Pregledac)))
            {
                DataRow r = t.NewRow();
                r[0] = o.DeSifra;
                r[1] = o.Naziv;
                for (int i = 0; i < 7; i++) r[i + 2] = Celija(o.DeSifra, pon.AddDays(i));
                t.Rows.Add(r);
            }
            tabKalendar.DataSource = t;
        }

        private static readonly Color BojaDnevna = ColorTranslator.FromHtml("#DDEDE4");
        private static readonly Color BojaNocna = ColorTranslator.FromHtml("#DEE9F1");
        private static readonly Color BojaOdsustvo = ColorTranslator.FromHtml("#F7E8C4");
        private static readonly Color BojaPosao = ColorTranslator.FromHtml("#FBDDE1");
        private static readonly Color BojaSlobodan = ColorTranslator.FromHtml("#E6E2DD");

        private void ObojiCeliju(object sender, DataGridViewCellFormattingEventArgs e)
        {
            string v = Convert.ToString(e.Value);
            if (e.ColumnIndex < 2 || v.Length == 0) return;
            Color? b = v == "Dnevna" ? BojaDnevna : v == "Noćna" ? BojaNocna : v == "Slobodan" ? BojaSlobodan : v.StartsWith("Posao") ? BojaPosao : (Color?)BojaOdsustvo;
            if (b.HasValue) { e.CellStyle.BackColor = b.Value; e.CellStyle.SelectionBackColor = b.Value; e.CellStyle.SelectionForeColor = Saobracaj.Tema.Theme.Tekst; }
        }

        private void PrikaziOsobu(int sifra)
        {
            izabranaOsoba = sifra;
            OsobaPlan o = kontekst?.Osobe.FirstOrDefault(x => x.DeSifra == sifra);
            if (o == null) return;
            var _ = PrikaziDetalje(o);
        }

        private async Task PrikaziDetalje(OsobaPlan o)
        {
            await EkranOP.Ucitaj(this, () => OsobljeRepo.Kvalifikacije(o.DeSifra), t =>
            {
                kvalifikacije = t;
                DateTime pon = Ponedeljak;
                string uloga = o.Masinovodja && o.Pregledac ? "Mašinovođa, pregledač" : o.Masinovodja ? "Mašinovođa" : o.Pregledac ? "Pregledač" : "–";
                IEnumerable<string> redovi = t.Rows.Cast<DataRow>().Select(r => string.Join(" · ", r.ItemArray.Where(x => x != null && x != DBNull.Value && !(x is byte[]) && !(x is int)).Select(Convert.ToString)));
                lblOsoba.Text = o.Naziv + "\nUloga: " + uloga + "\nMatična lokacija: " + kontekst.Plan.Naziv(o.MaticnaID) + "\nTurnus: " + (o.Turnus ?? "–") + "\nKvalifikacije:\n" + (t.Rows.Count == 0 ? "nema" : string.Join("\n", redovi));
                List<ZadatakOsobe> z = kontekst.Zadaci.Where(x => x.DeSifra == o.DeSifra && x.Aktivan && x.Od >= pon && x.Od < pon.AddDays(7)).ToList();
                ZadatakOsobe poslednji = z.OrderByDescending(x => x.Do).FirstOrDefault();
                lblZauzetost.Text = z.Count == 0 ? "Nema zadataka ove nedelje." : string.Join("\n", z.Select(x => "Najava " + x.BrojNajave + ": " + x.Od.ToString("dd.MM. HH:mm") + " – " + x.Do.ToString("HH:mm")))
                    + "\nLokacija posle smene: " + kontekst.Plan.Naziv(poslednji.PredajaStanicaID);
            });
        }

        private void IzmeniSmenu(int red, int kolona)
        {
            if (red < 0 || kolona < 2) return;
            int sifra = Convert.ToInt32(tabKalendar.Rows[red].Cells["DeSifra"].Value);
            DateTime dan = Ponedeljak.AddDays(tabKalendar.Columns[kolona].DisplayIndex - tabKalendar.Columns["Osoba"].DisplayIndex - 1);
            using (var d = new MaliDijalog("Smena · " + dan.ToString("dd.MM.yyyy")))
            {
                ComboBox vrsta = d.Izbor("Smena", "Dnevna", "Noćna", "Slobodan");
                DateTimePicker od = d.Datum("Od", dan.AddHours(7), true);
                DateTimePicker doV = d.Datum("Do", dan.AddHours(19), true);
                vrsta.SelectedIndexChanged += (s, e) =>
                {
                    od.Enabled = doV.Enabled = vrsta.SelectedIndex != 2;
                    if (vrsta.SelectedIndex == 1) { od.Value = dan.AddHours(19); doV.Value = dan.AddDays(1).AddHours(7); }
                    if (vrsta.SelectedIndex == 0) { od.Value = dan.AddHours(7); doV.Value = dan.AddHours(19); }
                };
                if (!d.Prikazi(this)) return;
                string baza = vrsta.SelectedIndex == 0 ? "DNEVNA" : vrsta.SelectedIndex == 1 ? "NOCNA" : "SLOBODAN";
                bool bezVremena = vrsta.SelectedIndex == 2;
                DateTime pocetak = od.Value, kraj = doV.Value;
                if (!bezVremena && kraj <= pocetak) { EkranOP.Poruka(this, "Kraj smene mora biti posle početka."); return; }
                if (EkranOP.Izvrsi(this, () => ZadatakRepo.SacuvajSmenuRucno(sifra, dan, baza, bezVremena ? (DateTime?)null : pocetak, bezVremena ? (DateTime?)null : kraj)))
                {
                    var _ = Osvezi();
                }
            }
        }

        private void DodajOdsustvo()
        {
            if (!izabranaOsoba.HasValue) { EkranOP.Poruka(this, "Izaberite osobu u kalendaru."); return; }
            var _ = UnesiOdsustvo(izabranaOsoba.Value);
        }

        private async Task UnesiOdsustvo(int sifra)
        {
            await EkranOP.Ucitaj(this, () => new { Razlozi = OdsustvaRepo.Razlozi(), Zaposleni = OdsustvaRepo.Zaposleni() }, p =>
            {
                using (var d = new MaliDijalog("Dodaj odsustvo"))
                {
                    ComboBox razlog = d.Izbor("Razlog", p.Razlozi.Rows.Cast<DataRow>().Select(r => Convert.ToString(r["Naziv"])).ToArray());
                    ComboBox odobrio = d.Izbor("Odobrio", new[] { "" }.Concat(p.Zaposleni.Rows.Cast<DataRow>().Select(r => Convert.ToString(r["Naziv"]))).ToArray());
                    DateTimePicker od = d.Datum("Od", dtNedelja.Value.Date);
                    DateTimePicker doD = d.Datum("Do", dtNedelja.Value.Date);
                    TextBox nap = d.Tekst("Napomena");
                    if (!d.Prikazi(this)) return;
                    if (odobrio.SelectedIndex <= 0) { EkranOP.Poruka(this, "Izaberite ko je odobrio."); return; }
                    DateTime a = od.Value.Date, b = doD.Value.Date;
                    if (b < a) { EkranOP.Poruka(this, "Kraj odsustva mora biti posle početka."); return; }
                    int razlogId = Convert.ToInt32(p.Razlozi.Rows[razlog.SelectedIndex]["ID"]);
                    int odobrioId = Convert.ToInt32(p.Zaposleni.Rows[odobrio.SelectedIndex - 1]["DeSifra"]);
                    string n = string.IsNullOrWhiteSpace(nap.Text) ? null : nap.Text.Trim();
                    if (EkranOP.Izvrsi(this, () => OdsustvaRepo.DodajRucno(sifra, razlogId, odobrioId, a, b, n)))
                    {
                        var _ = Osvezi();
                    }
                }
            });
        }

        private async Task Generisi()
        {
            if (kontekst == null) return;
            DateTime od = Ponedeljak, doD = od.AddDays(7);
            List<int> osobe = kontekst.Osobe.Select(o => o.DeSifra).ToList();
            await EkranOP.Ucitaj(this, () => TurnusiRepo.GenerisiSmene(od, doD, osobe), n =>
            {
                EkranOP.Poruka(this, "Upisano je smena: " + n + ".");
                var _ = Osvezi();
            });
        }

        private void PrikaziOdsustva()
        {
            if (!izabranaOsoba.HasValue) { EkranOP.Poruka(this, "Izaberite osobu u kalendaru."); return; }
            int sifra = izabranaOsoba.Value;
            var _ = PrikaziOdsustvaAsync(sifra);
        }

        private async Task PrikaziOdsustvaAsync(int sifra)
        {
            await EkranOP.Ucitaj(this, () => OdsustvaRepo.ZaOsobu(sifra), t =>
            {
                string tekst = t.Rows.Count == 0 ? "Nema odsustava." : string.Join("\n", t.Rows.Cast<DataRow>().Take(30).Select(r => r["Vrsta"] + ": " + Convert.ToDateTime(r["Od"]).ToString("dd.MM.yyyy") + " – " + Convert.ToDateTime(r["Do"]).ToString("dd.MM.yyyy")));
                EkranOP.Poruka(this, tekst);
            });
        }
    }
}
