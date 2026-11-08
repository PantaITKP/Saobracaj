using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Modeli;
using Saobracaj.Planiranje.Podaci;
using Saobracaj.Planiranje.Pravila;
using Saobracaj.Tema;

namespace Saobracaj.Planiranje.Forme
{
    // Ponuda lokomotiva za task, poređana po zauzetosti, vuči, vremenu dolaska i udaljenosti.
    // Odmah se računa po planu (taskovi, poslovi, matična stanica), a GPS se dodaje kad stigne.
    internal sealed class DijalogLokomotiveTaska : Form
    {
        private readonly DataGridView tabela = EkranOP.NovaTabela("Indeks");
        private readonly Label lblOpis = new Label { Dock = DockStyle.Top, AutoSize = false };
        private readonly Label lblGps = new Label { Dock = DockStyle.Top, AutoSize = false, Text = "Učitavam GPS pozicije lokomotiva... Lista se posle preuređuje, pa je izbor moguć kad se učitavanje završi." };
        private readonly PlanKontekst kontekst;
        private readonly TaskOP task;
        private readonly IList<TaskOP> svi;
        private List<KandidatTaska> ponuda = new List<KandidatTaska>();
        // GPS preuređuje listu; dok se učitava, izbor je zaključan da klik ne bi pogodio lokomotivu koja je u međuvremenu došla na to mesto.
        private bool gpsUToku = true;
        private readonly Button vucna, potisak;
        private static readonly CultureInfo Kultura = CultureInfo.GetCultureInfo("sr-Latn-RS");

        internal LokomotivaOP Izabrana { get; private set; }
        internal string Uloga { get; private set; }

        internal DijalogLokomotiveTaska(PlanKontekst k, TaskOP t, IList<TaskOP> sviTaskovi, string uloga, Func<int, string> naziv)
        {
            kontekst = k;
            task = t;
            svi = sviTaskovi;
            Text = "Lokomotive za task " + t.ID;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ClientSize = new Size(Theme.Dp(1250), Theme.Dp(560));
            Font = Theme.Tekstualni(9.5f);

            ElektroRute el = PonudaLokomotivaTaska.Elektrifikacija(k, t);
            lblOpis.Height = Theme.Dp(58);
            lblOpis.Padding = new Padding(Theme.Dp(8), Theme.Dp(6), Theme.Dp(8), 0);
            lblOpis.Text = "Task " + t.ID + " · " + naziv(t.OdID) + " – " + naziv(t.DoID) + " · " + t.Km.ToString("#,##0.0", Kultura) + " km · polazak " + Vreme(t.Od)
                + " · trajanje " + TaskPravila.TekstTrajanja(t.TrajanjeMin) + " (zauzetost do " + Vreme(t.Do) + ")\n" + el.Tekst
                + " Redosled: slobodne, vuča prema ruti, stiže na vreme, vreme dolaska, km.";
            lblGps.Height = Theme.Dp(22);
            lblGps.Padding = new Padding(Theme.Dp(8), 0, Theme.Dp(8), 0);
            lblGps.ForeColor = Theme.TekstSporedni;

            tabela.Dock = DockStyle.Fill;
            tabela.DataBindingComplete += (s, e) =>
            {
                tabela.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
                if (tabela.Columns.Contains("Zauzetost")) tabela.Columns["Zauzetost"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            };
            tabela.CellFormatting += Boja;
            tabela.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && !gpsUToku) Izaberi(uloga); };

            var dugmad = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = Theme.Dp(46), FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            var ne = new Button { Text = "Odustani", DialogResult = DialogResult.Cancel, AutoSize = true };
            vucna = new Button { Text = "Dodaj kao vučnu", AutoSize = true, Enabled = false };
            potisak = new Button { Text = "Dodaj kao potisak", AutoSize = true, Enabled = false };
            vucna.Click += (s, e) => Izaberi(TaskLokomotiva.Vucna);
            potisak.Click += (s, e) => Izaberi(TaskLokomotiva.Potisak);
            dugmad.Controls.Add(ne);
            dugmad.Controls.Add(uloga == TaskLokomotiva.Potisak ? potisak : vucna);
            dugmad.Controls.Add(uloga == TaskLokomotiva.Potisak ? vucna : potisak);
            AcceptButton = uloga == TaskLokomotiva.Potisak ? potisak : vucna;
            CancelButton = ne;

            var okvir = new Panel { Dock = DockStyle.Fill, Padding = new Padding(Theme.Dp(8), 0, Theme.Dp(8), 0) };
            okvir.Controls.Add(tabela);
            Controls.Add(okvir);
            Controls.Add(lblGps);
            Controls.Add(lblOpis);
            Controls.Add(dugmad);

            Prikazi(null);
            Shown += (s, e) => { var _ = UcitajGps(); };
        }

        private static string Vreme(DateTime? v)
        {
            return v.HasValue ? v.Value.ToString("dd.MM. HH:mm") : "–";
        }

        private static string Minuti(int? m)
        {
            if (!m.HasValue) return "–";
            return m.Value >= 60 ? m.Value / 60 + " h " + (m.Value % 60).ToString("00") + " min" : m.Value + " min";
        }

        private void Prikazi(IDictionary<int, GpsLokacija> gps)
        {
            ponuda = PonudaLokomotivaTaska.Izracunaj(kontekst, task, svi, gps, DateTime.Now);
            var t = new DataTable();
            t.Columns.Add("Indeks", typeof(int));
            foreach (string k in new[] { "#", "Lokomotiva", "Vuča", "Lokacija", "Izvor lokacije", "Km do polaska", "Vreme dolaska", "Stiže", "Na vreme", "Zauzetost", "Sledeći task", "Napomena" })
                t.Columns.Add(k);
            for (int i = 0; i < ponuda.Count; i++)
            {
                KandidatTaska c = ponuda[i];
                LokomotivaOP l = c.Lokomotiva;
                t.Rows.Add(i, i + 1, l.Broj + (string.IsNullOrEmpty(l.Serija) ? "" : " · " + l.Serija), l.Vuca == "E" ? "EL" : l.Vuca == "D" ? "DZ" : "?",
                    c.Lokacija, c.IzvorLokacije, c.Km.HasValue ? c.Km.Value.ToString("#,##0.0", Kultura) : "nepoznato", Minuti(c.DolazakMin), Vreme(c.Stize),
                    !c.Km.HasValue ? "–" : c.KasniMin == 0 ? "Da" : "Kasni " + Minuti(c.KasniMin), c.Zauzetost, c.Sledeci, c.NapomenaVuce);
            }
            tabela.DataSource = t;
            if (tabela.Columns.Contains("#")) tabela.Columns["#"].Width = Theme.Dp(34);
        }

        // Isto kao na ekranu Posao: GPS (prvi put i do 30 s) vezuje lokomotivu za najbližu stanicu mreže.
        private async Task UcitajGps()
        {
            PlanKontekst k = kontekst;
            int od = task.OdID;
            try
            {
                var d = await Task.Run(() =>
                {
                    List<PozicijaLokomotive> pozicije = GpsLokomotive.Ucitaj();
                    Dictionary<int, double[]> koordinate = StaniceRepo.Koordinate();
                    ICollection<int> dostupne = k.Graf.Rastojanja(od).Keys;
                    List<GpsLokacija> tacke = pozicije.Where(x => x.ImaPoziciju).Select(x => new GpsLokacija
                    {
                        Oznaka = x.Oznaka, Lat = x.Lat.Value, Lon = x.Lon.Value, Vreme = x.Vreme, Brzina = x.Brzina
                    }).ToList();
                    return GpsLokacije.Povezi(k.Lokomotive, tacke, koordinate, dostupne);
                });
                if (IsDisposed) return;
                int? izabran = IzabranIndeks();
                LokomotivaOP pre = izabran.HasValue ? ponuda[izabran.Value].Lokomotiva : null;
                Prikazi(d);
                lblGps.Text = "GPS pozicija za " + d.Count + " lokomotiva. Lokomotiva bez kasnijeg plana računa se od GPS pozicije.";
                // Izbor se vraća po lokomotivi, ne po mestu u listi.
                if (pre != null)
                    foreach (DataGridViewRow r in tabela.Rows)
                        if (ponuda[Convert.ToInt32(r.Cells["Indeks"].Value)].Lokomotiva.ID == pre.ID) { tabela.ClearSelection(); tabela.CurrentCell = r.Cells["#"]; r.Selected = true; break; }
            }
            catch (Exception ex)
            {
                if (!IsDisposed) lblGps.Text = "GPS nije dostupan (" + (ex is TaskCanceledException ? "isteklo je vreme čekanja" : ex.Message) + "). Lokacija je iz plana i matične stanice.";
            }
            finally
            {
                if (!IsDisposed) { gpsUToku = false; vucna.Enabled = potisak.Enabled = true; }
            }
        }

        private int? IzabranIndeks()
        {
            DataRow r = EkranOP.Izabran(tabela);
            return r == null ? (int?)null : Convert.ToInt32(r["Indeks"]);
        }

        private void Izaberi(string uloga)
        {
            if (gpsUToku) return;
            int? i = IzabranIndeks();
            if (!i.HasValue) { MessageBox.Show(this, "Izaberite lokomotivu.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            KandidatTaska c = ponuda[i.Value];
            if (c.Zauzeta) { MessageBox.Show(this, c.Lokomotiva.Broj + ": " + c.Zauzetost + ".\n\nPreklapanje se ne može potvrditi.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var upozorenja = new List<string>();
            if (!c.VucaOdgovara) upozorenja.Add("Vuča: " + c.NapomenaVuce + ".");
            if (c.KasniMin > 0) upozorenja.Add("Lokomotiva stiže " + Vreme(c.Stize) + ", " + Minuti(c.KasniMin) + " posle polaska taska.");
            if (!c.Km.HasValue) upozorenja.Add("Lokacija lokomotive ili veza do polazne stanice nije poznata.");
            if (upozorenja.Count > 0 && MessageBox.Show(this, string.Join("\n", upozorenja) + "\n\nDodati ipak?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            Izabrana = c.Lokomotiva;
            Uloga = uloga;
            DialogResult = DialogResult.OK;
        }

        // Zauzete sivo, vuča koja ne odgovara ruti i kašnjenje crveno.
        private void Boja(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || !tabela.Columns.Contains("Indeks")) return;
            KandidatTaska c = ponuda[Convert.ToInt32(tabela.Rows[e.RowIndex].Cells["Indeks"].Value)];
            string kolona = tabela.Columns[e.ColumnIndex].Name;
            if (c.Zauzeta) e.CellStyle.ForeColor = ColorTranslator.FromHtml("#9AA0A6");
            else if ((kolona == "Vuča" || kolona == "Napomena") && !c.VucaOdgovara) e.CellStyle.ForeColor = ColorTranslator.FromHtml("#8E1B2A");
            else if (kolona == "Na vreme" && c.KasniMin > 0) e.CellStyle.ForeColor = ColorTranslator.FromHtml("#8E1B2A");
            else if (kolona == "Na vreme" && c.Km.HasValue) e.CellStyle.ForeColor = ColorTranslator.FromHtml("#2F6B4F");
        }
    }
}
