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

namespace Saobracaj.Planiranje.Kontrole
{
    // Objekat koji JavaScript mape zove kao window.external. WebBrowser traži da klasa bude public i vidljiva COM-u
    // celom putanjom, pa ne sme da bude ugnježdena u internal klasi.
    [ComVisible(true)]
    public sealed class MostMape
    {
        private readonly KontrolaMape mapa;

        internal MostMape(KontrolaMape mapa)
        {
            this.mapa = mapa;
        }

        public void PomeriStanicu(int id, double lat, double lon)
        {
            mapa.JsPomeriStanicu(id, lat, lon);
        }

        public void IzaberiStanicu(int id)
        {
            mapa.JsIzaberiStanicu(id);
        }

        public void ZatvoriVezu(int id)
        {
            mapa.JsZatvoriVezu(id);
        }
    }

    // Mapa mreže (Leaflet u WebBrowser-u, IE11): pruge, stanice, lokomotive sa GPS-a, zatvori iz telegrama i predlog puta.
    // Koriste je Mreža i stanice (tab Mapa) i Mapa lokomotiva.
    internal sealed class KontrolaMape : Panel
    {
        internal const int SirinaKolone = 340;
        internal const string BojaPrilaza = "#8E44AD";
        internal const string BojaPuta = "#00A651";

        private readonly WebBrowser web = new WebBrowser { Dock = DockStyle.Fill, ScriptErrorsSuppressed = true };
        private readonly CheckBox chkLokomotive = new CheckBox { Text = "Lokomotive na mapi (GPS)", AutoSize = true, Checked = true };
        private readonly Label lblGps = new Label { AutoSize = true, MaximumSize = new System.Drawing.Size(SirinaKolone - 30, 0) };
        private readonly CheckBox chkZatvori = new CheckBox { Text = "Zatvori iz telegrama", AutoSize = true, Checked = true, Margin = new Padding(3, 8, 3, 3) };
        private readonly Label lblZatvori = new Label { AutoSize = true, MaximumSize = new System.Drawing.Size(SirinaKolone - 30, 0) };
        private readonly CheckBox chkVagoni = new CheckBox { Text = "Prikaži vagone na stanicama", AutoSize = true, Margin = new Padding(3, 8, 3, 3) };
        private readonly Label lblVagoni = new Label { AutoSize = true, MaximumSize = new System.Drawing.Size(SirinaKolone - 30, 0) };
        private readonly FlowLayoutPanel panelVagona = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Visible = false, Margin = new Padding(0) };
        private readonly TextBox txtFilterVagona = new TextBox { Width = SirinaKolone - 40 };
        private readonly CheckedListBox listaVagona = new CheckedListBox { Width = SirinaKolone - 40, Height = 220, CheckOnClick = true, IntegralHeight = false, HorizontalScrollbar = true };
        private readonly Timer tajmerGps = new Timer { Interval = 60000 };
        // Agent upisuje telegrame na 2 minuta, pa je osvežavanje na 5 minuta dovoljno.
        private readonly Timer tajmerZatvora = new Timer { Interval = 5 * 60000 };

        private DataTable stanice;
        private List<ZatvorNaMapi> zatvori;
        private bool gpsUToku, zatvoriUToku, gpsPrviPut = true;
        private string prilaz = "", put = "", tacke = "[]";
        // Vagoni iz „Stanje na mreži“ (Planer): izvor je tabela iz Planera, a bez nje dbo.OperativniPlanMreza (samo čitanje).
        private DataTable izvorVagona;
        private List<VagonNaMapi> vagoni;
        private bool vagoniUToku, popunjavamListu;

        // Slojevi (GPS i zatvori) sa natpisima; forma ih stavlja u svoju kolonu.
        internal FlowLayoutPanel PanelSlojeva { get; } = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(8, 6, 8, 6) };
        internal bool Ucitana { get; private set; }
        internal List<PozicijaLokomotive> Pozicije { get; private set; }
        // Stanice se prevlače samo na ekranu Mreža i stanice.
        internal bool DozvoliPomeranje { get; set; }
        internal Func<DataRow, bool> ZaProveru { get; set; }
        internal string DodatakLegende { get; set; } = "";

        internal event Action<int> StanicaIzabrana;
        internal event Action<int, double, double> StanicaPomerena;
        internal event Action<int> VezaKliknuta;
        internal event Action PozicijeOsvezene;

        internal KontrolaMape()
        {
            Controls.Add(web);
            PanelSlojeva.Controls.Add(chkLokomotive);
            PanelSlojeva.Controls.Add(lblGps);
            PanelSlojeva.Controls.Add(chkZatvori);
            PanelSlojeva.Controls.Add(lblZatvori);
            PanelSlojeva.Controls.Add(chkVagoni);
            PanelSlojeva.Controls.Add(lblVagoni);
            PanelSlojeva.Controls.Add(panelVagona);
            var dugmad = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            var sve = new Button { Text = "Uključi sve", AutoSize = true };
            var nijedan = new Button { Text = "Isključi sve", AutoSize = true };
            sve.Click += (s, e) => UkljuciVagone(true);
            nijedan.Click += (s, e) => UkljuciVagone(false);
            dugmad.Controls.Add(sve);
            dugmad.Controls.Add(nijedan);
            panelVagona.Controls.Add(new Label { Text = "Filter (klijent ili stanica):", AutoSize = true, Margin = new Padding(3, 4, 3, 0) });
            panelVagona.Controls.Add(txtFilterVagona);
            panelVagona.Controls.Add(dugmad);
            panelVagona.Controls.Add(listaVagona);
            chkLokomotive.CheckedChanged += (s, e) => PrebaciLokomotive();
            chkZatvori.CheckedChanged += (s, e) => PrebaciZatvore();
            chkVagoni.CheckedChanged += (s, e) => PrebaciVagone();
            txtFilterVagona.TextChanged += (s, e) => PopuniListuVagona();
            listaVagona.ItemCheck += (s, e) =>
            {
                if (popunjavamListu) return;
                ((VagonNaMapi)listaVagona.Items[e.Index]).Ukljucen = e.NewValue == CheckState.Checked;
                BeginInvoke((Action)CrtajVagone);
            };
            tajmerGps.Tick += (s, e) => { if (Visible) { var _ = OsveziLokomotive(); } };
            tajmerZatvora.Tick += (s, e) => { if (Visible) { var _ = OsveziZatvore(); } };
            web.DocumentCompleted += (s, e) => PonoviCrtanje();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                tajmerGps.Dispose();
                tajmerZatvora.Dispose();
            }
            base.Dispose(disposing);
        }

        // Prvi prikaz mape; slojevi se učitavaju tek tada, jer je GPS spor (prvi put ~30 s).
        internal void Prikazi(DataTable sveStanice, DataTable veze)
        {
            stanice = sveStanice;
            Ucitana = true;
            web.ObjectForScripting = new MostMape(this);
            web.DocumentText = Html(sveStanice, veze);
            Aktiviraj();
        }

        // Kad mapa ponovo dođe na ekran: odmah osveži slojeve, pa nastavi po tajmeru.
        internal void Aktiviraj()
        {
            if (!Ucitana) return;
            if (chkLokomotive.Checked) { tajmerGps.Start(); var _ = OsveziLokomotive(); }
            if (chkZatvori.Checked) { tajmerZatvora.Start(); var __ = OsveziZatvore(); }
            if (chkVagoni.Checked && vagoni == null) { var ___ = OsveziVagone(); }
        }

        // Prilaz lokomotive (ljubičasto, isprekidano) i predlog puta (zeleno); prazno briše.
        internal void CrtajRute(IEnumerable<int> prilazVeze, IEnumerable<int> putVeze)
        {
            prilaz = prilazVeze == null ? "" : string.Join(",", prilazVeze);
            put = putVeze == null ? "" : string.Join(",", putVeze);
            Pozovi("crtajRute", prilaz, put);
        }

        // Oznake na stanicama: [lat, lon, natpis, boja].
        internal void OznaciTacke(IEnumerable<Tuple<int, string, string>> stanicaNatpisBoja)
        {
            var lista = new List<object>();
            foreach (Tuple<int, string, string> t in stanicaNatpisBoja)
            {
                DataRow s = Stanica(t.Item1);
                if (s != null) lista.Add(new { a = Convert.ToDouble(s["Latitude"]), o = Convert.ToDouble(s["Longitude"]), n = t.Item2, b = t.Item3 });
            }
            tacke = Newtonsoft.Json.JsonConvert.SerializeObject(lista);
            Pozovi("oznaciTacke", tacke);
        }

        internal void OznaciZatvor(int vezaId, bool zatvorena)
        {
            Pozovi("oznaciZatvor", vezaId, zatvorena);
        }

        private DataRow Stanica(int id)
        {
            return stanice?.Rows.Cast<DataRow>().FirstOrDefault(r => Convert.ToInt32(r["ID"]) == id && !r.IsNull("Latitude") && !r.IsNull("Longitude"));
        }

        private void PonoviCrtanje()
        {
            CrtajZatvore();
            CrtajLokomotive();
            CrtajVagone();
            Pozovi("oznaciTacke", tacke);
            if (prilaz.Length > 0 || put.Length > 0) Pozovi("crtajRute", prilaz, put);
        }

        // Pozivi iz MostMape (JavaScript mape), na UI niti.
        internal void JsPomeriStanicu(int id, double lat, double lon) { StanicaPomerena?.Invoke(id, lat, lon); }
        internal void JsIzaberiStanicu(int id) { StanicaIzabrana?.Invoke(id); }
        internal void JsZatvoriVezu(int id) { VezaKliknuta?.Invoke(id); }

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
            Pozicije = null;
            lblGps.Text = "";
            Pozovi("crtajLokomotive", "[]", false);
        }

        // Greška GPS servisa ide u natpis, a ne u poruku, jer se osvežava svakog minuta.
        private async Task OsveziLokomotive()
        {
            if (gpsUToku || !chkLokomotive.Checked || !Ucitana) return;
            gpsUToku = true;
            if (Pozicije == null) lblGps.Text = "Učitavam pozicije lokomotiva...";
            try
            {
                List<PozicijaLokomotive> lista = await Task.Run(() => GpsLokomotive.Ucitaj());
                if (IsDisposed || !chkLokomotive.Checked) return;
                Pozicije = lista;
                CrtajLokomotive();
                var bez = lista.Where(p => !p.ImaPoziciju).Select(p => p.Oznaka).ToList();
                lblGps.Text = "Na mapi: " + lista.Count(p => p.ImaPoziciju) + " od " + lista.Count + " lokomotiva."
                    + (bez.Count > 0 ? " Bez pozicije u poslednja 24 h: " + string.Join(", ", bez) + "." : "")
                    + " Osveženo u " + DateTime.Now.ToString("HH:mm") + ", ponovo za minut.";
                PozicijeOsvezene?.Invoke();
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
            if (Pozicije == null || !Ucitana) return;
            DateTime granica = DateTime.Now - GpsLokomotive.Svez;
            var n = CultureInfo.InvariantCulture;
            var lista = Pozicije.Where(p => p.ImaPoziciju).Select(p => new
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
            if (Pozovi("crtajLokomotive", Newtonsoft.Json.JsonConvert.SerializeObject(lista), gpsPrviPut)) gpsPrviPut = false;
        }

        // Preko 5 km/h vozi (zelena), 0 stoji (crvena), između manevriše (plava). Brzina ispod 1 km/h računa se kao 0.
        private static string BojaBrzine(double? brzina)
        {
            if (!brzina.HasValue) return "#8A7C7F";
            if (brzina.Value > 5) return "#2E8B3A";
            if (brzina.Value < 1) return "#D7261E";
            return "#0B6E99";
        }

        // ---------- Vagoni na stanicama ----------

        // Planer (dugme GRAF) predaje kopiju tabele „Stanje na mreži“; opcija ostaje isključena dok je korisnik ne uključi.
        internal void PostaviVagone(DataTable stanjeNaMrezi)
        {
            izvorVagona = stanjeNaMrezi;
            vagoni = null;
            if (chkVagoni.Checked) { var _ = OsveziVagone(); }
        }

        private void PrebaciVagone()
        {
            panelVagona.Visible = chkVagoni.Checked;
            if (chkVagoni.Checked) { var _ = OsveziVagone(); return; }
            // Izbor se ne pamti: posle isključenja sve se ponovo učitava i sve je uključeno.
            vagoni = null;
            lblVagoni.Text = "";
            listaVagona.Items.Clear();
            txtFilterVagona.Text = "";
            Pozovi("crtajVagone", "[]");
        }

        private async Task OsveziVagone()
        {
            // Pre prve mape stanice nisu poznate; Aktiviraj ponovo poziva učitavanje.
            if (vagoniUToku || !chkVagoni.Checked || !Ucitana) return;
            vagoniUToku = true;
            lblVagoni.Text = "Učitavam stanje na mreži...";
            try
            {
                DataTable izvor = izvorVagona;
                DataTable st = stanice;
                List<VagonNaMapi> lista = await Task.Run(() => Rasporedi(izvor ?? StaniceRepo.StanjeNaMrezi(), st));
                if (IsDisposed || !chkVagoni.Checked) return;
                vagoni = lista;
                PopuniListuVagona();
                CrtajVagone();
                var bez = lista.Where(x => x.Lat == null).Select(x => x.Stanica).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
                int naMapi = lista.Count(x => x.Lat != null);
                lblVagoni.Text = (izvor != null ? "Iz Planera (stanje u trenutku otvaranja). " : "Iz tabele Stanje na mreži. ")
                    + "Na mapi: " + naMapi + " od " + lista.Count + " stavki, na " + lista.Where(x => x.Lat != null).Select(x => x.StanicaID).Distinct().Count() + " stanica."
                    + (bez.Count > 0 ? " Bez pozicije: " + string.Join(", ", bez) + "." : "");
            }
            catch (Exception ex)
            {
                if (!IsDisposed) lblVagoni.Text = "Stanje na mreži nije učitano: " + ex.Message;
            }
            finally
            {
                vagoniUToku = false;
            }
        }

        // Lista prikazuje samo stavke sa pozicijom; filter je po klijentu ili stanici, a čekiranje se čuva na stavci.
        private void PopuniListuVagona()
        {
            if (vagoni == null) return;
            string f = txtFilterVagona.Text.Trim();
            popunjavamListu = true;
            listaVagona.BeginUpdate();
            listaVagona.Items.Clear();
            foreach (VagonNaMapi v in vagoni.Where(x => x.Lat != null && x.Odgovara(f)))
                listaVagona.Items.Add(v, v.Ukljucen);
            listaVagona.EndUpdate();
            popunjavamListu = false;
        }

        // Uključi sve / Isključi sve važi za stavke koje filter prikazuje.
        private void UkljuciVagone(bool ukljuci)
        {
            if (vagoni == null) return;
            popunjavamListu = true;
            for (int i = 0; i < listaVagona.Items.Count; i++)
            {
                ((VagonNaMapi)listaVagona.Items[i]).Ukljucen = ukljuci;
                listaVagona.SetItemChecked(i, ukljuci);
            }
            popunjavamListu = false;
            CrtajVagone();
        }

        private void CrtajVagone()
        {
            if (vagoni == null || !Ucitana) return;
            var lista = vagoni.Where(x => x.Lat != null && x.Ukljucen).GroupBy(x => x.StanicaID).Select(g => new
            {
                a = g.First().Lat.Value,
                o = g.First().Lon.Value,
                n = g.Count(),
                t = "<b>" + System.Net.WebUtility.HtmlEncode(g.First().NazivNaMapi) + "</b> (" + g.Count() + ")<br>"
                    + string.Join("<br>", g.Select(x => System.Net.WebUtility.HtmlEncode(x.Opis)))
            });
            Pozovi("crtajVagone", Newtonsoft.Json.JsonConvert.SerializeObject(lista));
        }

        // „NAZIV:“ i „Naziv“ su ista stanica: dvotačka i razmaci se uklanjaju, pa se naziv normalizuje kao u ostatku Planiranja.
        internal static string KljucStanice(string naziv)
        {
            return NaziviStanica.Normalizuj((naziv ?? "").Replace(":", " ").Trim());
        }

        // Stavka bez trenutne stanice se preskače. Stanica koja nije u mreži, ili ima koordinate 0,0, ostaje bez pozicije.
        internal static List<VagonNaMapi> Rasporedi(DataTable stanje, DataTable sveStanice)
        {
            var rezultat = new List<VagonNaMapi>();
            if (stanje == null || !stanje.Columns.Contains("Stanica")) return rezultat;
            var poId = new Dictionary<int, DataRow>();
            if (sveStanice != null)
                foreach (DataRow r in sveStanice.Rows) poId[Convert.ToInt32(r["ID"])] = r;
            Func<DataRow, DataRow> glavna = r =>
            {
                for (int i = 0; i < 5 && r != null && r.Table.Columns.Contains("DuplikatOd") && !r.IsNull("DuplikatOd"); i++)
                {
                    DataRow d;
                    if (!poId.TryGetValue(Convert.ToInt32(r["DuplikatOd"]), out d)) break;
                    r = d;
                }
                return r;
            };
            Func<DataRow, bool> imaPoziciju = r => r != null && !r.IsNull("Latitude") && !r.IsNull("Longitude")
                && !(Convert.ToDouble(r["Latitude"]) == 0 && Convert.ToDouble(r["Longitude"]) == 0)
                && (!r.Table.Columns.Contains("Aktivna") || r.IsNull("Aktivna") || Convert.ToBoolean(r["Aktivna"]));
            // Ključ naziva → stanica; prednost ima stanica sa pozicijom, pa tekući naziv pre starog.
            var poNazivu = new Dictionary<string, DataRow>();
            if (sveStanice != null)
                foreach (string kolona in new[] { "Naziv", "NazivStari" })
                {
                    if (!sveStanice.Columns.Contains(kolona)) continue;
                    foreach (DataRow r in sveStanice.Rows)
                    {
                        string k = KljucStanice(Convert.ToString(r[kolona]));
                        if (k.Length == 0) continue;
                        DataRow g = glavna(r);
                        DataRow postojeca;
                        if (!poNazivu.TryGetValue(k, out postojeca) || (!imaPoziciju(postojeca) && imaPoziciju(g))) poNazivu[k] = g;
                    }
                }
            Func<DataRow, string, string> polje = (r, k) => r.Table.Columns.Contains(k) && !r.IsNull(k) ? Convert.ToString(r[k]).Trim() : "";
            foreach (DataRow r in stanje.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;
                string naziv = polje(r, "Stanica").Replace(":", " ").Trim();
                if (naziv.Length == 0) continue;
                var v = new VagonNaMapi { Stanica = naziv, IDVoza = polje(r, "IDVoza"), Vagoni = polje(r, "Vagoni"), Klijent = polje(r, "Klijent") };
                DataRow s;
                if (poNazivu.TryGetValue(KljucStanice(naziv), out s) && imaPoziciju(s))
                {
                    v.StanicaID = Convert.ToInt32(s["ID"]);
                    v.NazivNaMapi = Convert.ToString(s["Naziv"]).Trim();
                    v.Lat = Convert.ToDouble(s["Latitude"]);
                    v.Lon = Convert.ToDouble(s["Longitude"]);
                }
                rezultat.Add(v);
            }
            return rezultat.OrderBy(x => x.NazivNaMapi ?? x.Stanica).ThenBy(x => x.IDVoza).ToList();
        }

        // ---------- Zatvori iz telegrama ----------

        private void PrebaciZatvore()
        {
            if (chkZatvori.Checked)
            {
                tajmerZatvora.Start();
                var _ = OsveziZatvore();
                return;
            }
            tajmerZatvora.Stop();
            zatvori = null;
            lblZatvori.Text = "";
            Pozovi("crtajZatvore", "[]");
        }

        // Isti kontekst i ista pravila kao rutiranje (Ruta.JeAktivan, Ruta.Pokriva), za danas i naredna 3 dana.
        private async Task OsveziZatvore()
        {
            if (zatvoriUToku || !chkZatvori.Checked || !Ucitana) return;
            zatvoriUToku = true;
            if (zatvori == null) lblZatvori.Text = "Učitavam zatvore iz telegrama...";
            try
            {
                DateTime sada = DateTime.Now;
                List<ZatvorNaMapi> lista = await Task.Run(() => ZatvoriMape.Izracunaj(PlanRepo.Ucitaj(sada.Date, sada.Date.AddDays(2)), sada));
                if (IsDisposed || !chkZatvori.Checked) return;
                zatvori = lista;
                CrtajZatvore();
                Func<int, int> broj = x => lista.Count(z => z.Nivo == x);
                lblZatvori.Text = "Zatvori (deonica i stanica): u toku " + broj(ZatvoriMape.USada) + ", danas " + broj(ZatvoriMape.Danas)
                    + ", naredna 3 dana " + broj(ZatvoriMape.Uskoro) + ". Osveženo u " + sada.ToString("HH:mm") + ". Pređite mišem preko deonice za telegram.";
            }
            catch (Exception ex)
            {
                if (!IsDisposed) lblZatvori.Text = "Zatvori nisu učitani: " + ex.Message;
            }
            finally
            {
                zatvoriUToku = false;
            }
        }

        private void CrtajZatvore()
        {
            if (zatvori == null || !Ucitana || stanice == null) return;
            var lista = new List<object>();
            foreach (ZatvorNaMapi z in zatvori.OrderByDescending(x => x.Nivo))
            {
                string opis = string.Join("<br>", z.Opisi.OrderBy(o => o.Item1).Select(o => System.Net.WebUtility.HtmlEncode(o.Item2)));
                if (z.VezaID.HasValue)
                    lista.Add(new { v = z.VezaID.Value, n = z.Nivo, c = z.CelaPruga ? 1 : 0, t = opis });
                else if (z.StanicaID.HasValue)
                {
                    DataRow s = Stanica(z.StanicaID.Value);
                    if (s != null) lista.Add(new { v = 0, a = Convert.ToDouble(s["Latitude"]), o = Convert.ToDouble(s["Longitude"]), n = z.Nivo, c = z.CelaPruga ? 1 : 0, t = opis });
                }
            }
            Pozovi("crtajZatvore", Newtonsoft.Json.JsonConvert.SerializeObject(lista));
        }

        private bool Pozovi(string funkcija, params object[] argumenti)
        {
            if (!Ucitana || web.Document == null) return false;
            try
            {
                // Funkcija vraća 1; null znači da se stranica još učitava, pa DocumentCompleted crta ponovo.
                return web.Document.InvokeScript(funkcija, argumenti) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ---------- HTML ----------

        // LINESTRING(lon lat, lon lat) iz baze u parove [lat,lon] za Leaflet.
        private static string TackeWkt(string wkt)
        {
            var sb = new StringBuilder("[");
            foreach (Match m in Regex.Matches(wkt, @"(-?\d+(?:\.\d+)?)\s+(-?\d+(?:\.\d+)?)"))
                sb.Append("[").Append(m.Groups[2].Value).Append(",").Append(m.Groups[1].Value).Append("],");
            return sb.Append("]").ToString().Replace(",]", "]");
        }

        private static string JsTekst(object o)
        {
            return Convert.ToString(o).Trim().Replace("\\", "\\\\").Replace("'", "\\'").Replace("<", "&lt;");
        }

        private string Html(DataTable sveStanice, DataTable veze)
        {
            var n = CultureInfo.InvariantCulture;
            var s = new StringBuilder();
            foreach (DataRow r in sveStanice.Rows)
            {
                if (r.IsNull("Latitude") || r.IsNull("Longitude") || !Convert.ToBoolean(r["Aktivna"])) continue;
                bool proveriti = ZaProveru != null && ZaProveru(r);
                s.Append("[").Append(Convert.ToDouble(r["Latitude"]).ToString(n)).Append(",").Append(Convert.ToDouble(r["Longitude"]).ToString(n)).Append(",'").Append(JsTekst(r["Naziv"])).Append("',")
                    .Append(Convert.ToInt32(r["ID"])).Append(",").Append(proveriti ? 1 : 0).Append("],");
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
            string pomeranje = DozvoliPomeranje
                ? "if(vuceni)m.removeLayer(vuceni);vuceni=L.marker([t[0],t[1]],{draggable:true}).bindTooltip(t[2]+' (prevuci)').addTo(m);" +
                  "vuceni.on('dragend',function(e){var p=e.target.getLatLng();window.external.PomeriStanicu(t[3],p.lat,p.lng);});"
                : "";
            string legenda = "<i style=\"border-color:#51111C\"></i>Elektrificirana<br><i style=\"border-color:#8A7C7F\"></i>Bez kontaktne mreže ili nepoznato<br><i style=\"border-color:#C9BFBC\"></i>KiM<br>" +
                "<i style=\"border-color:" + BojaPrilaza + ";border-top-style:dashed\"></i>Prilaz lokomotive <i style=\"border-color:" + BojaPuta + "\"></i>Predlog puta<br>" +
                (DozvoliPomeranje ? "Žuta tačka: koordinata za proveru. Klik na tačku pa prevlačenje: nove koordinate.<br>" : "") +
                "Klik na prugu: ručni zatvor za račun rute.<br>Lokomotive (GPS): <span style=\"color:#2E8B3A\">&#9679;</span> preko 5 km/h <span style=\"color:#0B6E99\">&#9679;</span> 1-5 km/h <span style=\"color:#D7261E\">&#9679;</span> stoji. Bled krug: signal stariji od 15 min.<br>" +
                "Zatvori iz telegrama: <i style=\"border-color:#D7261E\"></i>u toku <i style=\"border-color:#F08C00\"></i>danas <i style=\"border-color:#2F80ED\"></i>naredna 3 dana. Puna linija: zatvor pruge, isprekidana: kolosek ili skretnica; krug: zatvor u stanici." +
                "<br>Vagoni na stanicama: <span style=\"background:#F2C94C;border:1px solid #7A5B00;padding:0 3px\">3</span> broj stavki; mišem preko ili klik: ID voza, vagoni, klijent." +
                (DodatakLegende.Length > 0 ? "<br>" + DodatakLegende : "");
            // WebBrowser radi kao IE11, zato je ovde ES5, bez strelica i const.
            return "<!doctype html><html><head><meta charset='utf-8'><meta http-equiv='X-UA-Compatible' content='IE=edge'>" +
                "<link rel='stylesheet' href='https://unpkg.com/leaflet@1.7.1/dist/leaflet.css'><script src='https://unpkg.com/leaflet@1.7.1/dist/leaflet.js'></script>" +
                "<style>html,body,#m{height:100%;margin:0}.lokt{font:bold 11px sans-serif;padding:1px 4px}.tackat{font:bold 12px sans-serif;padding:2px 6px}.zt{white-space:normal;width:380px}" +
                ".vagt{background:#F2C94C;border:1px solid #7A5B00;border-radius:3px;font:bold 11px sans-serif;padding:1px 4px;color:#3A2A00;white-space:nowrap}" +
                ".leg{background:#F8F6F3;padding:6px 8px;font:12px sans-serif;border:1px solid #D2C9C6;max-width:420px}.leg i{display:inline-block;width:20px;border-top:3px solid;vertical-align:middle;margin-right:6px}</style></head><body><div id='m'></div><script>" +
                "var m=L.map('m',{preferCanvas:true}).setView([44.2,20.9],7);" +
                "L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png',{attribution:'&copy; OpenStreetMap contributors'}).addTo(m);" +
                "var boje=['#8A7C7F','#51111C','#C9BFBC'],v=[" + linije + "],l={};" +
                "function klik(id){return function(){window.external.ZatvoriVezu(id);};}" +
                "for(var i=0;i<v.length;i++){l[v[i][0]]=L.polyline(v[i][3],{color:boje[v[i][1]],weight:2}).bindTooltip(v[i][2]).on('click',klik(v[i][0])).addTo(m);l[v[i][0]].boja=boje[v[i][1]];l[v[i][0]].zatvor=false;l[v[i][0]].uRuti=0;}" +
                "var s=[" + s + "],ruta=[],vuceni=null;" +
                "function izbor(t){return function(){window.external.IzaberiStanicu(t[3]);" + pomeranje + "};}" +
                "for(var j=0;j<s.length;j++)L.circleMarker([s[j][0],s[j][1]],{radius:s[j][4]?2.5:2,weight:1,color:s[j][4]?'#C8962B':'#51111C',fillOpacity:0.9}).bindTooltip(s[j][2]).on('click',izbor(s[j])).addTo(m);" +
                // uRuti: 0 nije na ruti, 1 prilaz lokomotive, 2 predlog puta.
                "function stil(a){a.setStyle({color:a.zatvor?'#E0301E':(a.uRuti==2?'" + BojaPuta + "':a.uRuti==1?'" + BojaPrilaza + "':a.boja),weight:a.uRuti||a.zatvor?5:2,dashArray:a.zatvor?'6,6':(a.uRuti==1?'10,6':null)});}" +
                "function oznaciZatvor(id,z){l[id].zatvor=z;stil(l[id]);return 1;}" +
                "function crtajRute(ids1,ids2){for(var i=0;i<ruta.length;i++){var b=l[ruta[i]];if(b){b.uRuti=0;stil(b);}}ruta=[];var g=null;" +
                "var delovi=[[ids1,1],[ids2,2]];for(var d=0;d<2;d++){var lista=delovi[d][0]?delovi[d][0].split(','):[];" +
                "for(var k=0;k<lista.length;k++){var a=l[lista[k]];if(!a)continue;ruta.push(lista[k]);a.uRuti=delovi[d][1];stil(a);a.bringToFront();g=g?g.extend(a.getBounds()):L.latLngBounds(a.getBounds().getSouthWest(),a.getBounds().getNorthEast());}}" +
                // Predlog puta je iznad zatvora, a lokomotive i oznake ostaju na vrhu.
                "lok.eachLayer(function(x){x.bringToFront();});tac.eachLayer(function(x){x.bringToFront();});if(g)m.fitBounds(g.pad(0.1));return 1;}" +
                // Zatvori su iznad pruga, a ispod lokomotiva; klik na zatvorenu deonicu i dalje menja ručni zatvor.
                "var zat=L.layerGroup().addTo(m),bojeZ=['#D7261E','#F08C00','#2F80ED'];" +
                "function crtajZatvore(js){zat.clearLayers();var a=JSON.parse(js);for(var i=0;i<a.length;i++){var z=a[i],b=bojeZ[z.n];" +
                "if(z.v){var p=l[z.v];if(!p)continue;L.polyline(p.getLatLngs(),{color:b,weight:z.c?6:4,opacity:0.9,dashArray:z.c?null:'8,6'}).bindTooltip(z.t,{sticky:true,className:'zt'}).on('click',klik(z.v)).addTo(zat);}" +
                "else L.circleMarker([z.a,z.o],{radius:7,weight:3,color:b,fill:false,dashArray:z.c?null:'3,3'}).bindTooltip(z.t,{className:'zt'}).addTo(zat);}return 1;}" +
                "var lok=L.layerGroup().addTo(m);" +
                "function crtajLokomotive(js,fit){lok.clearLayers();var a=JSON.parse(js),g=[];for(var i=0;i<a.length;i++){var t=a[i];" +
                "L.circleMarker([t.a,t.o],{radius:7,weight:2,color:'#FFFFFF',fillColor:t.b,fillOpacity:t.s?1:0.45}).bindTooltip(t.n,{permanent:true,direction:'right',offset:[8,0],className:'lokt'}).bindPopup(t.d).addTo(lok);g.push([t.a,t.o]);}" +
                "if(fit&&g.length)m.fitBounds(L.latLngBounds(g).pad(0.2),{maxZoom:10});return 1;}" +
                // Vagoni su u svom sloju: oznaka sa brojem stavki je gore-levo od stanice, da ne pokriva stanicu, lokomotive i zatvore.
                "m.createPane('vagoni');m.getPane('vagoni').style.zIndex=450;var vag=L.layerGroup().addTo(m);" +
                "function crtajVagone(js){vag.clearLayers();var a=JSON.parse(js);for(var i=0;i<a.length;i++){var t=a[i];" +
                "L.marker([t.a,t.o],{pane:'vagoni',icon:L.divIcon({className:'',html:'<div class=\"vagt\">'+t.n+'</div>',iconSize:null,iconAnchor:[22,22]})})" +
                ".bindTooltip(t.t,{direction:'top',className:'zt'}).bindPopup(t.t,{maxWidth:420}).addTo(vag);}return 1;}" +
                "var tac=L.layerGroup().addTo(m);" +
                "function oznaciTacke(js){tac.clearLayers();var a=JSON.parse(js);for(var i=0;i<a.length;i++){var t=a[i];" +
                "L.circleMarker([t.a,t.o],{radius:9,weight:3,color:t.b,fillColor:'#FFFFFF',fillOpacity:1}).bindTooltip(t.n,{permanent:true,direction:'top',offset:[0,-8],className:'tackat'}).addTo(tac);}return 1;}" +
                "var leg=L.control({position:'bottomleft'});leg.onAdd=function(){var d=L.DomUtil.create('div','leg');" +
                "d.innerHTML='" + legenda + "';return d;};leg.addTo(m);" +
                "</script></body></html>";
        }
    }

    // Stavka „Stanje na mreži“ na mapi; Lat je null kad stanica nije u mreži ili ima koordinate 0,0.
    internal sealed class VagonNaMapi
    {
        internal string Stanica, IDVoza, Vagoni, Klijent, NazivNaMapi;
        internal int StanicaID;
        internal double? Lat, Lon;
        internal bool Ukljucen = true;

        internal string Opis
        {
            get { return string.Join(" · ", new[] { IDVoza, Vagoni, Klijent }.Select(x => string.IsNullOrEmpty(x) ? "-" : x)); }
        }

        internal bool Odgovara(string filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;
            return new[] { Klijent, Stanica, NazivNaMapi }.Any(x => x != null && x.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0);
        }

        public override string ToString()
        {
            return Opis + " — " + (NazivNaMapi ?? Stanica);
        }
    }
}
