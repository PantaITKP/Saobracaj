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

    // Uzorak u legendi: linija (puna ili isprekidana), krug ili oznaka sa brojem, isto kao na mapi.
    internal sealed class UzorakLegende : Control
    {
        private enum Oblik { Linija, Krug, Oznaka }

        private Oblik oblik;
        private System.Drawing.Color boja, ivica;
        private bool isprekidano, bledo;
        private int debljina = 3;

        private UzorakLegende()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            Size = new System.Drawing.Size(28, 16);
            Margin = new Padding(6, 4, 2, 0);
            BackColor = System.Drawing.Color.Transparent;
        }

        private static System.Drawing.Color Boja(string html) { return System.Drawing.ColorTranslator.FromHtml(html); }

        internal static UzorakLegende Linija(string boja, bool isprekidano = false, int debljina = 3)
        {
            return new UzorakLegende { oblik = Oblik.Linija, boja = Boja(boja), isprekidano = isprekidano, debljina = debljina };
        }

        internal static UzorakLegende Krug(string boja, string ivica = "#FFFFFF", bool isprekidano = false, bool bledo = false)
        {
            return new UzorakLegende { oblik = Oblik.Krug, boja = Boja(boja), ivica = Boja(ivica), isprekidano = isprekidano, bledo = bledo, Width = 18 };
        }

        internal static UzorakLegende Oznaka(string boja, string tekst)
        {
            return new UzorakLegende { oblik = Oblik.Oznaka, boja = Boja(boja), Text = tekst, Width = 20 };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int y = Height / 2;
            switch (oblik)
            {
                case Oblik.Linija:
                    using (var p = new System.Drawing.Pen(boja, debljina))
                    {
                        if (isprekidano) p.DashPattern = new[] { 2.5f, 1.5f };
                        g.DrawLine(p, 1, y, Width - 2, y);
                    }
                    break;
                case Oblik.Krug:
                    var r = new System.Drawing.Rectangle(2, y - 6, 12, 12);
                    using (var b = new System.Drawing.SolidBrush(bledo ? System.Drawing.Color.FromArgb(115, boja) : boja)) g.FillEllipse(b, r);
                    using (var p = new System.Drawing.Pen(ivica == System.Drawing.Color.White ? System.Drawing.Color.FromArgb(160, 160, 160) : ivica, 1.5f))
                    {
                        if (isprekidano) p.DashPattern = new[] { 2f, 2f };
                        g.DrawEllipse(p, r);
                    }
                    break;
                case Oblik.Oznaka:
                    var o = new System.Drawing.Rectangle(1, 1, Width - 3, Height - 3);
                    using (var b = new System.Drawing.SolidBrush(boja)) g.FillRectangle(b, o);
                    using (var f = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold))
                        TextRenderer.DrawText(g, Text, f, o, System.Drawing.Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    break;
            }
        }
    }

    // Mapa mreže (Leaflet u WebBrowser-u, IE11) sa trakom slojeva na vrhu: lokomotive (GPS), stanice i pruge,
    // telegrami (zatvori), stanje na mreži i najave iz operativnog plana. Predlog puta se crta preko pruga.
    // Koriste je Mreža i stanice (tab Mapa) i Mapa lokomotiva.
    internal sealed class KontrolaMape : Panel
    {
        internal const int SirinaKolone = 340;
        internal const string BojaPrilaza = "#8E44AD";
        internal const string BojaPuta = "#00A651";
        private const string BojaMreze = "#00796B", BojaNajava = "#C2185B";

        private readonly WebBrowser web = new WebBrowser { Dock = DockStyle.Fill, ScriptErrorsSuppressed = true };
        private readonly FlowLayoutPanel traka = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(6, 4, 6, 2) };
        private readonly CheckBox chkLokomotive = Sloj("Lokomotive");
        private readonly CheckBox chkVozila = Sloj("Vozila");
        private readonly CheckBox chkStanice = Sloj("Mreža");
        private readonly CheckBox chkTelegrami = Sloj("Telegrami");
        private readonly CheckBox chkMreza = Sloj("Stanje na mreži");
        private readonly CheckBox chkNajave = Sloj("Najave");
        // Stavke iz „Stanje na mreži“ i „Najave“ koje dugo stoje; dugme otvara spisak.
        private readonly Button btnAkcija = new Button { Text = "Potrebna akcija", AutoSize = true, Enabled = false, Margin = new Padding(10, 0, 4, 0) };
        private ProzorAkcije prozorAkcije;
        // Stanje sloja (koliko je na mapi, kada je osveženo, greška) je u opisu checkbox-a, a ne u tekstu ispod trake.
        private readonly ToolTip opisi = new ToolTip { AutoPopDelay = 20000, InitialDelay = 300 };
        // Dok se mapa i lokomotive ne učitaju, ceo ekran je pokriven i ništa se ne može kliknuti.
        private readonly Panel ucitavanje = new Panel { BackColor = System.Drawing.Color.FromArgb(248, 246, 243), Visible = false };
        private readonly Label lblUcitavanje = new Label { AutoSize = true, Text = "Učitavam mapu i pozicije lokomotiva..." };
        private readonly ProgressBar trakaUcitavanja = new ProgressBar { Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 30, Width = 320, Height = 18 };
        private readonly FlowLayoutPanel legenda = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, WrapContents = true, Padding = new Padding(4, 3, 4, 3) };
        private readonly Timer tajmerGps = new Timer { Interval = 60000 };
        // Agent upisuje telegrame na 2 minuta, a dispečeri menjaju plan stalno: zatvori na 5 minuta, plan na 2.
        private readonly Timer tajmerZatvora = new Timer { Interval = 5 * 60000 };
        private readonly Timer tajmerPlana = new Timer { Interval = 2 * 60000 };
        private readonly Dictionary<CheckBox, string> statusi = new Dictionary<CheckBox, string>();

        private DataTable stanice;
        private List<ZatvorNaMapi> zatvori;
        private PlanNaMapi plan;
        private bool gpsUToku, zatvoriUToku, planUToku, gpsPrviPut = true;
        private string prilaz = "", put = "", tacke = "[]";

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

        private static CheckBox Sloj(string naziv)
        {
            return new CheckBox { Text = naziv, AutoSize = true, Checked = true, Margin = new Padding(4, 3, 14, 3) };
        }

        internal KontrolaMape()
        {
            traka.Controls.AddRange(new Control[] { chkLokomotive, chkVozila, chkStanice, chkTelegrami, chkMreza, chkNajave, btnAkcija });
            btnAkcija.Click += (s, e) => OtvoriAkcije();
            // Redosled dodavanja određuje dokovanje: traka je na vrhu, status ispod nje, legenda u dnu, mapa popunjava ostatak.
            Controls.Add(web);
            Controls.Add(legenda);
            Controls.Add(traka);
            ucitavanje.Controls.Add(lblUcitavanje);
            ucitavanje.Controls.Add(trakaUcitavanja);
            Controls.Add(ucitavanje);
            Resize += (s, e) => RasporediUcitavanje();
            // Pokrivač je tu od prvog prikaza: prvo se učitava mreža, pa lokomotive.
            lblUcitavanje.Text = "Učitavam mrežu...";
            PrikaziUcitavanje(true);
            chkLokomotive.CheckedChanged += (s, e) => PrebaciLokomotive();
            chkVozila.CheckedChanged += (s, e) => PrebaciVozila();
            chkStanice.CheckedChanged += (s, e) => { Pozovi("prikaziMrezu", chkStanice.Checked); Status(chkStanice, null); };
            chkTelegrami.CheckedChanged += (s, e) => PrebaciZatvore();
            chkMreza.CheckedChanged += (s, e) => PrebaciPlan(chkMreza);
            chkNajave.CheckedChanged += (s, e) => PrebaciPlan(chkNajave);
            tajmerGps.Tick += (s, e) => { if (Visible) { var _ = OsveziLokomotive(); var __ = OsveziVozila(); } };
            tajmerZatvora.Tick += (s, e) => { if (Visible) { var _ = OsveziZatvore(); } };
            tajmerPlana.Tick += (s, e) => { if (Visible) { var _ = OsveziPlan(); } };
            web.DocumentCompleted += (s, e) => PonoviCrtanje();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                tajmerGps.Dispose();
                tajmerZatvora.Dispose();
                tajmerPlana.Dispose();
                opisi.Dispose();
            }
            base.Dispose(disposing);
        }

        // Pokrivač preko cele kontrole (i trake), sa trakom napretka u sredini.
        internal void PrikaziUcitavanje(bool prikazi)
        {
            ucitavanje.Visible = prikazi;
            if (!prikazi) return;
            RasporediUcitavanje();
            ucitavanje.BringToFront();
        }

        private void RasporediUcitavanje()
        {
            ucitavanje.Bounds = ClientRectangle;
            int x = (ucitavanje.Width - trakaUcitavanja.Width) / 2, y = ucitavanje.Height / 2;
            trakaUcitavanja.Location = new System.Drawing.Point(Math.Max(0, x), y);
            lblUcitavanje.Location = new System.Drawing.Point(Math.Max(0, (ucitavanje.Width - lblUcitavanje.Width) / 2), y - lblUcitavanje.Height - 8);
        }

        // Prvi prikaz mape; slojevi se učitavaju tek tada, jer je GPS spor (prvi put ~30 s).
        internal void Prikazi(DataTable sveStanice, DataTable veze)
        {
            stanice = sveStanice;
            Ucitana = true;
            if (legenda.Controls.Count == 0) NapraviLegendu();
            web.ObjectForScripting = new MostMape(this);
            web.DocumentText = Html(sveStanice, veze);
            if (ucitavanje.Visible)
            {
                lblUcitavanje.Text = "Učitavam pozicije lokomotiva (prvi put do pola minuta)...";
                RasporediUcitavanje();
            }
            Aktiviraj();
        }

        // Kad mapa ponovo dođe na ekran: odmah osveži slojeve, pa nastavi po tajmeru.
        internal void Aktiviraj()
        {
            if (!Ucitana) return;
            if (!chkLokomotive.Checked) PrikaziUcitavanje(false);
            if (chkLokomotive.Checked) { tajmerGps.Start(); var _ = OsveziLokomotive(); }
            if (chkVozila.Checked) { tajmerGps.Start(); var _v = OsveziVozila(); }
            if (chkTelegrami.Checked) { tajmerZatvora.Start(); var __ = OsveziZatvore(); }
            // Plan se učitava i kad su oba sloja isključena, jer ga koristi i dugme „Potrebna akcija“.
            tajmerPlana.Start();
            var ___ = OsveziPlan();
        }

        // Prilaz lokomotive (ljubičasto, isprekidano) i predlog puta (zeleno); prazno briše.
        internal void CrtajRute(IEnumerable<int> prilazVeze, IEnumerable<int> putVeze)
        {
            prilaz = prilazVeze == null ? "" : string.Join(",", prilazVeze);
            put = putVeze == null ? "" : string.Join(",", putVeze);
            Pozovi("crtajRute", prilaz, put);
        }

        // Oznake na stanicama: stanica, natpis, boja.
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
            if (!chkStanice.Checked) Pozovi("prikaziMrezu", false);
            CrtajZatvore();
            CrtajLokomotive();
            CrtajVozila();
            CrtajPlan();
            Pozovi("oznaciTacke", tacke);
            if (prilaz.Length > 0 || put.Length > 0) Pozovi("crtajRute", prilaz, put);
        }

        // Legenda u dnu, u istom redu kao traka slojeva; grupe redom kao slojevi.
        private void NapraviLegendu()
        {
            legenda.SuspendLayout();
            Action<string> grupa = naziv => legenda.Controls.Add(new Label { Text = naziv, AutoSize = true, Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold), Margin = new Padding(12, 4, 0, 0) });
            // Uzorak i opis su u jednom panelu, da se pri prelomu reda ne razdvoje.
            Action<Control, string> stavka = (uzorak, opis) =>
            {
                var par = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0), Padding = new Padding(0) };
                par.Controls.Add(uzorak);
                par.Controls.Add(new Label { Text = opis, AutoSize = true, Margin = new Padding(0, 4, 4, 0) });
                legenda.Controls.Add(par);
            };
            grupa("Pruge:");
            stavka(UzorakLegende.Linija("#51111C"), "elektrificirana");
            stavka(UzorakLegende.Linija("#8A7C7F"), "bez kontaktne mreže ili nepoznato");
            stavka(UzorakLegende.Linija("#C9BFBC"), "KiM");
            stavka(UzorakLegende.Linija("#E0301E", true), "ručni zatvor (klik na prugu)");
            grupa("Put:");
            stavka(UzorakLegende.Linija(BojaPrilaza, true, 4), "prilaz lokomotive");
            stavka(UzorakLegende.Linija(BojaPuta, false, 4), "predlog puta");
            grupa("Lokomotive:");
            stavka(UzorakLegende.Krug("#2E8B3A"), "preko 5 km/h");
            stavka(UzorakLegende.Krug("#0B6E99"), "1–5 km/h");
            stavka(UzorakLegende.Krug("#D7261E"), "stoji");
            stavka(UzorakLegende.Krug("#2E8B3A", bledo: true), "signal stariji od 15 min");
            stavka(UzorakLegende.Krug("#B8AFAF", "#5E5458", true), "bez signala duže od 24 h (uz datum)");
            grupa("Vozila:");
            stavka(UzorakLegende.Krug("#2E8B3A", "#1F1A1B"), "manji krug sa tamnom ivicom i oznakom vozila, iste boje po brzini");
            grupa("Telegrami:");
            stavka(UzorakLegende.Linija("#D7261E", false, 5), "u toku");
            stavka(UzorakLegende.Linija("#F08C00", false, 5), "danas");
            stavka(UzorakLegende.Linija("#2F80ED", false, 5), "naredna 3 dana");
            stavka(UzorakLegende.Linija("#5E5458", true, 3), "isprekidano: kolosek ili skretnica");
            stavka(UzorakLegende.Krug("#FFFFFF", "#D7261E"), "zatvor u stanici");
            grupa("Plan:");
            stavka(UzorakLegende.Oznaka(BojaMreze, "3"), "stanje na mreži (broj stavki)");
            stavka(UzorakLegende.Oznaka(BojaNajava, "2"), "najave na graničnom prelazu");
            stavka(UzorakLegende.Oznaka("#D7261E", "!"), "za proveru: stanje stoji " + PlanMrezeRepo.DanaUpozorenja + "+ dana od unosa, najava kasni " + PlanMrezeRepo.DanaUpozorenja + "+ dana od očekivanog vremena");
            if (DozvoliPomeranje)
            {
                grupa("Stanice:");
                stavka(UzorakLegende.Krug("#C8962B", "#C8962B"), "koordinata za proveru; klik na stanicu pa prevlačenje: nove koordinate");
            }
            if (DodatakLegende.Length > 0) legenda.Controls.Add(new Label { Text = DodatakLegende, AutoSize = true, Margin = new Padding(12, 4, 0, 0) });
            legenda.ResumeLayout(true);
        }

        // Stanje sloja se vidi kad se miš zadrži na njegovom checkbox-u.
        private void Status(CheckBox sloj, string tekst)
        {
            if (tekst == null) statusi.Remove(sloj); else statusi[sloj] = tekst;
            opisi.SetToolTip(sloj, tekst == null ? null : sloj.Text + ": " + tekst);
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
            if (!chkVozila.Checked) tajmerGps.Stop();
            Pozovi("crtajLokomotive", "[]", false);
            Status(chkLokomotive, null);
        }

        // Greška GPS servisa ide u natpis, a ne u poruku, jer se osvežava svakog minuta.
        private async Task OsveziLokomotive()
        {
            if (gpsUToku || !chkLokomotive.Checked || !Ucitana) return;
            gpsUToku = true;
            if (Pozicije == null) Status(chkLokomotive, "učitavam pozicije...");
            try
            {
                List<PozicijaLokomotive> lista = await Task.Run(() => GpsLokomotive.Ucitaj());
                if (IsDisposed) return;
                Pozicije = lista;
                PozicijeOsvezene?.Invoke();
                if (!chkLokomotive.Checked) return;
                CrtajLokomotive();
                DateTime dan = DateTime.Now.AddHours(-24);
                var stare = lista.Where(p => p.ImaPoziciju && p.Vreme.HasValue && p.Vreme.Value < dan).Select(p => p.Oznaka + " (" + p.Vreme.Value.ToString("dd.MM.") + ")").ToList();
                var bez = lista.Where(p => !p.ImaPoziciju).Select(p => p.Oznaka).ToList();
                Status(chkLokomotive, "na mapi " + lista.Count(p => p.ImaPoziciju) + " od " + lista.Count
                    + (stare.Count > 0 ? "; bez signala duže od 24 h, na poslednjoj poziciji: " + string.Join(", ", stare) : "")
                    + (bez.Count > 0 ? "; bez ijednog signala u " + GpsLokomotive.NajviseDanaUnazad + " dana: " + string.Join(", ", bez) : "")
                    + ". Osveženo u " + DateTime.Now.ToString("HH:mm") + ".");
            }
            catch (Exception ex)
            {
                if (!IsDisposed) Status(chkLokomotive, "GPS servis nije dostupan: " + (ex is TaskCanceledException ? "isteklo je vreme čekanja." : ex.Message));
            }
            finally
            {
                gpsUToku = false;
                // Posle prvog odgovora GPS-a (i kad je greška) ekran se otključava.
                if (!IsDisposed) PrikaziUcitavanje(false);
            }
        }

        private void CrtajLokomotive()
        {
            if (Pozicije == null || !Ucitana || !chkLokomotive.Checked) return;
            if (Pozovi("crtajLokomotive", TackeGps(Pozicije), gpsPrviPut)) gpsPrviPut = false;
        }

        // ---------- Vozila (GPS) ----------

        internal List<PozicijaLokomotive> PozicijeVozila { get; private set; }
        private bool vozilaUToku;

        private void PrebaciVozila()
        {
            if (chkVozila.Checked)
            {
                tajmerGps.Start();
                if (PozicijeVozila == null) { var _ = OsveziVozila(); } else CrtajVozila();
                return;
            }
            if (!chkLokomotive.Checked) tajmerGps.Stop();
            Pozovi("crtajVozila", "[]");
            Status(chkVozila, null);
        }

        // Isto kao lokomotive (isti API i ista pretraga unazad); greška ide u opis checkbox-a.
        private async Task OsveziVozila()
        {
            if (vozilaUToku || !chkVozila.Checked || !Ucitana) return;
            vozilaUToku = true;
            if (PozicijeVozila == null) Status(chkVozila, "učitavam pozicije...");
            try
            {
                List<PozicijaLokomotive> lista = await Task.Run(() => GpsLokomotive.UcitajVozila());
                if (IsDisposed) return;
                PozicijeVozila = lista;
                if (!chkVozila.Checked) return;
                CrtajVozila();
                DateTime dan = DateTime.Now.AddHours(-24);
                int stare = lista.Count(p => p.ImaPoziciju && p.Vreme.HasValue && p.Vreme.Value < dan);
                var bez = lista.Where(p => !p.ImaPoziciju).Select(p => p.Oznaka).ToList();
                Status(chkVozila, "na mapi " + lista.Count(p => p.ImaPoziciju) + " od " + lista.Count
                    + (stare > 0 ? "; bez signala duže od 24 h, na poslednjoj poziciji: " + stare : "")
                    + (bez.Count > 0 ? "; bez ijednog signala u " + GpsLokomotive.NajviseDanaUnazad + " dana: " + string.Join(", ", bez) : "")
                    + ". Osveženo u " + DateTime.Now.ToString("HH:mm") + ".");
            }
            catch (Exception ex)
            {
                if (!IsDisposed) Status(chkVozila, "GPS servis nije dostupan: " + (ex is TaskCanceledException ? "isteklo je vreme čekanja." : ex.Message));
            }
            finally
            {
                vozilaUToku = false;
            }
        }

        private void CrtajVozila()
        {
            if (PozicijeVozila == null || !Ucitana || !chkVozila.Checked) return;
            Pozovi("crtajVozila", TackeGps(PozicijeVozila));
        }

        // Tačke za mapu: boja po brzini, bledo posle 15 min, sivo isprekidano sa datumom posle 24 h bez signala.
        private static string TackeGps(List<PozicijaLokomotive> pozicije)
        {
            DateTime svez = DateTime.Now - GpsLokomotive.Svez, dan = DateTime.Now.AddHours(-24);
            var n = CultureInfo.InvariantCulture;
            var lista = pozicije.Where(p => p.ImaPoziciju).Select(p =>
            {
                bool staro = !p.Vreme.HasValue || p.Vreme.Value < dan;
                return new
                {
                    n = p.Oznaka + (staro && p.Vreme.HasValue ? " (" + p.Vreme.Value.ToString("dd.MM.") + ")" : ""),
                    a = p.Lat.Value,
                    o = p.Lon.Value,
                    s = p.Vreme.HasValue && p.Vreme.Value >= svez ? 1 : 0,
                    z = staro ? 1 : 0,
                    b = BojaBrzine(p.Brzina),
                    d = "<b>" + System.Net.WebUtility.HtmlEncode(p.Oznaka) + "</b>"
                        + (p.Model != null ? "<br>" + System.Net.WebUtility.HtmlEncode(p.Model) : "")
                        + (p.Vreme.HasValue ? "<br>Poslednji signal: " + p.Vreme.Value.ToString("dd.MM.yyyy HH:mm:ss") : "")
                        + (staro ? "<br>Bez signala duže od 24 h: prikazana je poslednja poznata pozicija." : "")
                        + (p.Brzina.HasValue && !staro ? "<br>Brzina: " + p.Brzina.Value.ToString("0", n) + " km/h" : "")
                };
            });
            return Newtonsoft.Json.JsonConvert.SerializeObject(lista);
        }

        // Preko 5 km/h vozi (zelena), 0 stoji (crvena), između manevriše (plava). Brzina ispod 1 km/h računa se kao 0.
        private static string BojaBrzine(double? brzina)
        {
            if (!brzina.HasValue) return "#8A7C7F";
            if (brzina.Value > 5) return "#2E8B3A";
            if (brzina.Value < 1) return "#D7261E";
            return "#0B6E99";
        }

        // ---------- Telegrami (zatvori) ----------

        private void PrebaciZatvore()
        {
            if (chkTelegrami.Checked)
            {
                tajmerZatvora.Start();
                var _ = OsveziZatvore();
                return;
            }
            tajmerZatvora.Stop();
            Pozovi("crtajZatvore", "[]");
            Status(chkTelegrami, null);
        }

        // Isti kontekst i ista pravila kao rutiranje (Ruta.JeAktivan, Ruta.Pokriva), za danas i naredna 3 dana.
        private async Task OsveziZatvore()
        {
            if (zatvoriUToku || !chkTelegrami.Checked || !Ucitana) return;
            zatvoriUToku = true;
            if (zatvori == null) Status(chkTelegrami, "učitavam zatvore...");
            try
            {
                DateTime sada = DateTime.Now;
                List<ZatvorNaMapi> lista = await Task.Run(() => ZatvoriMape.Izracunaj(PlanRepo.Ucitaj(sada.Date, sada.Date.AddDays(2)), sada));
                if (IsDisposed || !chkTelegrami.Checked) return;
                zatvori = lista;
                CrtajZatvore();
                Func<int, int> broj = x => lista.Count(z => z.Nivo == x);
                Status(chkTelegrami, "zatvori u toku " + broj(ZatvoriMape.USada) + ", danas " + broj(ZatvoriMape.Danas) + ", naredna 3 dana " + broj(ZatvoriMape.Uskoro)
                    + " (deonica i stanica). Osveženo u " + sada.ToString("HH:mm") + ".");
            }
            catch (Exception ex)
            {
                if (!IsDisposed) Status(chkTelegrami, "nisu učitani: " + ex.Message);
            }
            finally
            {
                zatvoriUToku = false;
            }
        }

        private void CrtajZatvore()
        {
            if (zatvori == null || !Ucitana || stanice == null || !chkTelegrami.Checked) return;
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

        // ---------- Stanje na mreži i najave (operativni plan) ----------

        private void PrebaciPlan(CheckBox sloj)
        {
            CrtajPlan();
            if (!sloj.Checked) Status(sloj, null);
            else if (plan != null) OpisiPlan();
        }

        // Iste tabele kao tabovi „Stanje na mreži“ i „Najave vozova“ u Planeru i dispMain-u.
        private async Task OsveziPlan()
        {
            if (planUToku || !Ucitana || stanice == null) return;
            planUToku = true;
            try
            {
                DataTable st = stanice;
                PlanNaMapi p = await Task.Run(() => PlanMrezeRepo.Ucitaj(st));
                if (IsDisposed) return;
                plan = p;
                planOsvezen = DateTime.Now;
                CrtajPlan();
                OpisiPlan();
                var zaAkciju = p.ZaAkciju();
                btnAkcija.Enabled = true;
                btnAkcija.Text = zaAkciju.Count == 0 ? "Potrebna akcija" : "Potrebna akcija (" + zaAkciju.Count + ")";
                btnAkcija.ForeColor = zaAkciju.Count == 0 ? System.Drawing.SystemColors.ControlText : System.Drawing.Color.FromArgb(0xD7, 0x26, 0x1E);
                string pravilo = "Stanje na mreži: stoji " + PlanMrezeRepo.DanaUpozorenja + "+ dana od unosa. Najave: očekivano vreme je prošlo pre " + PlanMrezeRepo.DanaUpozorenja + "+ dana.";
                opisi.SetToolTip(btnAkcija, (zaAkciju.Count == 0 ? "Nema stavki za proveru. " : zaAkciju.Count + " stavki za proveru: otvorite spisak. ") + pravilo);
                if (prozorAkcije != null && !prozorAkcije.IsDisposed) prozorAkcije.Prikazi(zaAkciju, planOsvezen);
            }
            catch (Exception ex)
            {
                if (!IsDisposed) { if (chkMreza.Checked) Status(chkMreza, "nije učitano: " + ex.Message); if (chkNajave.Checked) Status(chkNajave, "nije učitano: " + ex.Message); }
            }
            finally
            {
                planUToku = false;
            }
        }

        private DateTime planOsvezen;

        private void OpisiPlan()
        {
            string vreme = " Osveženo u " + planOsvezen.ToString("HH:mm") + ".";
            string nisu = plan.NisuNadjena.Count > 0 ? " Nije nađeno na mapi: " + string.Join(", ", plan.NisuNadjena) + "." : "";
            Func<List<MestoPlana>, string, string> zaProveru = (mesta, opis) =>
            {
                int n = mesta.Sum(m => m.Stavke.Count(s => s.ZaAkciju));
                return n == 0 ? "" : " " + opis + ": " + n + " (dugme Potrebna akcija).";
            };
            if (chkMreza.Checked) Status(chkMreza, plan.Mreza.Sum(m => m.Stavke.Count) + " stavki u " + plan.Mreza.Count + " stanica."
                + zaProveru(plan.Mreza, "Stoji " + PlanMrezeRepo.DanaUpozorenja + "+ dana od unosa") + vreme + nisu);
            if (chkNajave.Checked) Status(chkNajave, plan.Najave.Sum(m => m.Stavke.Count) + " najava na " + plan.Najave.Count + " graničnih prelaza."
                + zaProveru(plan.Najave, "Očekivano vreme prošlo pre " + PlanMrezeRepo.DanaUpozorenja + "+ dana") + vreme);
        }

        private static string OpisStavke(StavkaPlana s)
        {
            string kada = s.Uneto.HasValue ? " (uneto " + s.Uneto.Value.ToString("dd.MM.") + (s.Izmenjeno.HasValue && s.DanaBezIzmene < s.DanaStoji ? ", izmena " + s.Izmenjeno.Value.ToString("dd.MM.") : "") + ")" : "";
            string tekst = System.Net.WebUtility.HtmlEncode(s.Opis + kada);
            return s.ZaAkciju ? "<b style=\"color:#D7261E\">⚠ " + System.Net.WebUtility.HtmlEncode(s.KratkoUpozorenje) + "</b> " + tekst : "• " + tekst;
        }

        private void CrtajPlan()
        {
            if (!Ucitana) return;
            Func<List<MestoPlana>, bool, string> json = (mesta, ukljuceno) => !ukljuceno || mesta == null ? "[]" : Newtonsoft.Json.JsonConvert.SerializeObject(mesta.Select(m => new
            {
                a = m.Lat,
                o = m.Lon,
                k = m.Stavke.Count,
                w = m.ZaAkciju ? 1 : 0,
                t = "<b>" + System.Net.WebUtility.HtmlEncode(m.Naziv) + "</b>" + (m.Priblizno ? " (približno)" : "") + "<br>"
                    + string.Join("<br>", m.Stavke.OrderByDescending(s => s.ZaAkciju).ThenByDescending(s => s.DanaKasni).Select(OpisStavke))
            }));
            Pozovi("crtajPlan", json(plan?.Mreza, chkMreza.Checked), json(plan?.Najave, chkNajave.Checked));
        }

        private void OtvoriAkcije()
        {
            if (plan == null) return;
            if (prozorAkcije == null || prozorAkcije.IsDisposed)
            {
                prozorAkcije = new ProzorAkcije();
                prozorAkcije.MestoIzabrano += (lat, lon) => Pozovi("centriraj", lat, lon);
                prozorAkcije.Show(FindForm());
            }
            prozorAkcije.Prikazi(plan.ZaAkciju(), planOsvezen);
            prozorAkcije.Activate();
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
            // Legenda je u dnu kontrole (NapraviLegendu), ne na mapi. WebBrowser radi kao IE11, zato je ovde ES5, bez strelica i const.
            return "<!doctype html><html><head><meta charset='utf-8'><meta http-equiv='X-UA-Compatible' content='IE=edge'>" +
                "<link rel='stylesheet' href='https://unpkg.com/leaflet@1.7.1/dist/leaflet.css'><script src='https://unpkg.com/leaflet@1.7.1/dist/leaflet.js'></script>" +
                "<style>html,body,#m{height:100%;margin:0}.lokt{font:bold 11px sans-serif;padding:1px 4px}.tackat{font:bold 12px sans-serif;padding:2px 6px}.zt{white-space:normal;width:380px}" +
                ".pm,.pn{display:inline-block;color:#fff;font:bold 11px sans-serif;border:2px solid #fff;border-radius:3px;padding:0 5px;box-shadow:0 0 2px #333;white-space:nowrap}" +
                ".pm{background:" + BojaMreze + "}.pn{background:" + BojaNajava + "}.upoz{border:2px solid #FFD400;box-shadow:0 0 0 2px #D7261E}.leaflet-div-icon.ikona{background:none;border:none}" +
                "</style></head><body><div id='m'></div><script>" +
                "var m=L.map('m',{preferCanvas:true}).setView([44.2,20.9],7);" +
                "L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png',{attribution:'&copy; OpenStreetMap contributors'}).addTo(m);" +
                "var boje=['#8A7C7F','#51111C','#C9BFBC'],v=[" + linije + "],l={},mrezaVidljiva=true;" +
                "function klik(id){return function(){window.external.ZatvoriVezu(id);};}" +
                "for(var i=0;i<v.length;i++){l[v[i][0]]=L.polyline(v[i][3],{color:boje[v[i][1]],weight:2}).bindTooltip(v[i][2]).on('click',klik(v[i][0])).addTo(m);l[v[i][0]].boja=boje[v[i][1]];l[v[i][0]].zatvor=false;l[v[i][0]].uRuti=0;}" +
                "var s=[" + s + "],ruta=[],vuceni=null,tackeStanica=L.layerGroup().addTo(m);" +
                "function izbor(t){return function(){window.external.IzaberiStanicu(t[3]);" + pomeranje + "};}" +
                "for(var j=0;j<s.length;j++)L.circleMarker([s[j][0],s[j][1]],{radius:s[j][4]?2.5:2,weight:1,color:s[j][4]?'#C8962B':'#51111C',fillOpacity:0.9}).bindTooltip(s[j][2]).on('click',izbor(s[j])).addTo(tackeStanica);" +
                // Bez sloja Stanice ostaju samo deonice puta i ručni zatvori.
                "function vidljiva(a){var treba=mrezaVidljiva||a.uRuti||a.zatvor;if(treba&&!m.hasLayer(a))a.addTo(m);if(!treba&&m.hasLayer(a))m.removeLayer(a);}" +
                "function prikaziMrezu(b){mrezaVidljiva=b;for(var id in l)vidljiva(l[id]);if(b)tackeStanica.addTo(m);else m.removeLayer(tackeStanica);return 1;}" +
                // uRuti: 0 nije na ruti, 1 prilaz lokomotive, 2 predlog puta.
                "function stil(a){a.setStyle({color:a.zatvor?'#E0301E':(a.uRuti==2?'" + BojaPuta + "':a.uRuti==1?'" + BojaPrilaza + "':a.boja),weight:a.uRuti||a.zatvor?5:2,dashArray:a.zatvor?'6,6':(a.uRuti==1?'10,6':null)});vidljiva(a);}" +
                "function oznaciZatvor(id,z){l[id].zatvor=z;stil(l[id]);return 1;}" +
                "function crtajRute(ids1,ids2){for(var i=0;i<ruta.length;i++){var b=l[ruta[i]];if(b){b.uRuti=0;stil(b);}}ruta=[];var g=null;" +
                "var delovi=[[ids1,1],[ids2,2]];for(var d=0;d<2;d++){var lista=delovi[d][0]?delovi[d][0].split(','):[];" +
                "for(var k=0;k<lista.length;k++){var a=l[lista[k]];if(!a)continue;ruta.push(lista[k]);a.uRuti=delovi[d][1];stil(a);a.bringToFront();g=g?g.extend(a.getBounds()):L.latLngBounds(a.getBounds().getSouthWest(),a.getBounds().getNorthEast());}}" +
                // Predlog puta je iznad zatvora, a lokomotive i oznake ostaju na vrhu.
                "lok.eachLayer(function(x){x.bringToFront();});voz.eachLayer(function(x){x.bringToFront();});tac.eachLayer(function(x){x.bringToFront();});if(g)m.fitBounds(g.pad(0.1));return 1;}" +
                // Zatvori su iznad pruga, a ispod lokomotiva; klik na zatvorenu deonicu i dalje menja ručni zatvor.
                "var zat=L.layerGroup().addTo(m),bojeZ=['#D7261E','#F08C00','#2F80ED'];" +
                "function crtajZatvore(js){zat.clearLayers();var a=JSON.parse(js);for(var i=0;i<a.length;i++){var z=a[i],b=bojeZ[z.n];" +
                "if(z.v){var p=l[z.v];if(!p)continue;L.polyline(p.getLatLngs(),{color:b,weight:z.c?6:4,opacity:0.9,dashArray:z.c?null:'8,6'}).bindTooltip(z.t,{sticky:true,className:'zt'}).on('click',klik(z.v)).addTo(zat);}" +
                "else L.circleMarker([z.a,z.o],{radius:7,weight:3,color:b,fill:false,dashArray:z.c?null:'3,3'}).bindTooltip(z.t,{className:'zt'}).addTo(zat);}return 1;}" +
                // Stanje na mreži i najave: oznaka sa brojem stavki; klik otvara spisak.
                "var pla=L.layerGroup().addTo(m);" +
                // Mesto sa stavkom koja dugo stoji: oznaka sa „!“ i crvenom ivicom.
                "function oznakaPlana(t,klasa,pomak){return L.marker([t.a,t.o],{icon:L.divIcon({className:'ikona',html:'<span class=\"'+klasa+(t.w?' upoz':'')+'\">'+t.k+(t.w?' !':'')+'</span>',iconSize:null,iconAnchor:pomak}),zIndexOffset:t.w?700:500}).bindTooltip(t.t,{className:'zt',direction:'top'}).bindPopup(t.t,{maxWidth:420});}" +
                "function centriraj(a,o){m.setView([a,o],11);return 1;}" +
                "function crtajPlan(js1,js2){pla.clearLayers();var a=JSON.parse(js1),b=JSON.parse(js2);for(var i=0;i<a.length;i++)oznakaPlana(a[i],'pm',[-6,8]).addTo(pla);" +
                "for(var j=0;j<b.length;j++)oznakaPlana(b[j],'pn',[-6,-10]).addTo(pla);return 1;}" +
                "var lok=L.layerGroup().addTo(m);" +
                "function crtajLokomotive(js,fit){lok.clearLayers();var a=JSON.parse(js),g=[];for(var i=0;i<a.length;i++){var t=a[i];" +
                "L.circleMarker([t.a,t.o],{radius:7,weight:2,color:t.z?'#5E5458':'#FFFFFF',dashArray:t.z?'3,3':null,fillColor:t.z?'#B8AFAF':t.b,fillOpacity:t.s||t.z?1:0.45}).bindTooltip(t.n,{permanent:true,direction:'right',offset:[8,0],className:'lokt'}).bindPopup(t.d).addTo(lok);g.push([t.a,t.o]);}" +
                "if(fit&&g.length)m.fitBounds(L.latLngBounds(g).pad(0.2),{maxZoom:10});return 1;}" +
                // Vozila: manji krug sa tamnom ivicom, iste boje po brzini kao lokomotive.
                "var voz=L.layerGroup().addTo(m);" +
                "function crtajVozila(js){voz.clearLayers();var a=JSON.parse(js);for(var i=0;i<a.length;i++){var t=a[i];" +
                "L.circleMarker([t.a,t.o],{radius:5,weight:2,color:t.z?'#5E5458':'#1F1A1B',dashArray:t.z?'3,3':null,fillColor:t.z?'#B8AFAF':t.b,fillOpacity:t.s||t.z?1:0.45}).bindTooltip(t.n,{permanent:true,direction:'right',offset:[6,0],className:'lokt'}).bindPopup(t.d).addTo(voz);}return 1;}" +
                "var tac=L.layerGroup().addTo(m);" +
                "function oznaciTacke(js){tac.clearLayers();var a=JSON.parse(js);for(var i=0;i<a.length;i++){var t=a[i];" +
                "L.circleMarker([t.a,t.o],{radius:9,weight:3,color:t.b,fillColor:'#FFFFFF',fillOpacity:1}).bindTooltip(t.n,{permanent:true,direction:'top',offset:[0,-8],className:'tackat'}).addTo(tac);}return 1;}" +
                "</script></body></html>";
        }
    }
}
