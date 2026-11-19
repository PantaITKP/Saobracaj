using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Saobracaj.Agent.CimProvera;
using Saobracaj.Agent.CimProvera.Baza;
using Saobracaj.Agent.CimProvera.Radionica;

namespace Saobracaj.Agent.CimRadionica
{
    /// <summary>
    /// CimRadionica – glavna forma. Kartice: Poruke (uvoz, niti, obrada), Rezultat (odgovor, PRIMER, rezultat.json, log), Zbirno i poređenje,
    /// Pravila, Poslovi, Teretnice, Događaji, Testiranje (praćenje foldera), Podešavanja. Alat nikad ne šalje mail.
    /// </summary>
    internal sealed class GlavnaForma : Form
    {
        private readonly Podesavanja p;
        private RadniProstor rp;

        private readonly TabControl kartice = new TabControl { Dock = DockStyle.Fill };
        private readonly StatusStrip status = new StatusStrip();
        private readonly ToolStripStatusLabel lRezim = new ToolStripStatusLabel(), lSifarnici = new ToolStripStatusLabel(), lProstor = new ToolStripStatusLabel(), lPoruka = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        private readonly ToolStripProgressBar napredak = new ToolStripProgressBar { Width = 200 };

        // Poruke
        private readonly ComboBox cbProstor = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
        private readonly ListView lvPoruke = new ListView { View = View.Details, FullRowSelect = true, Dock = DockStyle.Fill, HideSelection = false };
        private readonly CheckBox chBrisiTmp = new CheckBox { Text = "obriši tmp podatke pre obrade (DELETE)", AutoSize = true };
        private readonly CheckBox chSamoNit = new CheckBox { Text = "samo izabrana nit", AutoSize = true };
        private readonly CheckBox chAnaliza = new CheckBox { Text = "napravi _Analiza", AutoSize = true };
        private readonly TextBox tbVerzija = new TextBox { Width = 90 };

        // Rezultat
        private readonly WebBrowser wbOdgovor = new WebBrowser { Dock = DockStyle.Fill, ScriptErrorsSuppressed = true };
        private readonly TextBox tbJson = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, Font = new Font("Consolas", 9), ReadOnly = true };
        private readonly TextBox tbLog = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, Font = new Font("Consolas", 9), ReadOnly = true };
        private readonly Label lRezultat = new Label { Dock = DockStyle.Top, Height = 40, Padding = new Padding(6) };

        // Zbirno
        private readonly ComboBox cbBila = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 }, cbSada = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
        private readonly CheckBox chSamoNe = new CheckBox { Text = "paket: samo „Tačno?“ = ne / delimično", AutoSize = true };
        private readonly WebBrowser wbPoredjenje = new WebBrowser { Dock = DockStyle.Fill, ScriptErrorsSuppressed = true };

        // Pravila, Poslovi, Teretnice, Događaji
        private readonly DataGridView gPravila = Mreza(), gPoslovi = Mreza(), gPoreklo = Mreza(), gIstorija = Mreza(), gTeretnice = Mreza(), gStavke = Mreza(), gDogadjaji = Mreza();

        // Testiranje
        private readonly TextBox tbTestiranje = new TextBox { Width = 420 };
        private readonly ListBox lbTestiranje = new ListBox { Dock = DockStyle.Fill, HorizontalScrollbar = true };
        private readonly Timer tajmer = new Timer { Interval = 5000 };
        private FileSystemWatcher posmatrac;
        private bool radi;

        // Podešavanja
        private readonly RadioButton rbOnline = new RadioButton { Text = "Online – upis u planiranje.tmp* (kao agent)", AutoSize = true };
        private readonly RadioButton rbOffline = new RadioButton { Text = "Offline – rezultat.json, predlog posla iz lokalne kopije šifarnika", AutoSize = true };
        private readonly Label lKonekcija = new Label { AutoSize = true };

        public GlavnaForma(Podesavanja p)
        {
            this.p = p;
            Text = "CimRadionica " + RezultatJson.VerzijaPrograma + " – testiranje CimProvera (ne šalje mail)";
            Width = 1400; Height = 860; StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9);
            status.Items.AddRange(new ToolStripItem[] { lProstor, new ToolStripSeparator(), lRezim, new ToolStripSeparator(), lSifarnici, new ToolStripSeparator(), lPoruka, napredak });
            Controls.Add(kartice);
            Controls.Add(status);
            kartice.TabPages.Add(KarticaPoruke());
            kartice.TabPages.Add(KarticaRezultat());
            kartice.TabPages.Add(KarticaZbirno());
            kartice.TabPages.Add(KarticaPravila());
            kartice.TabPages.Add(KarticaPoslovi());
            kartice.TabPages.Add(KarticaTeretnice());
            kartice.TabPages.Add(KarticaDogadjaji());
            kartice.TabPages.Add(KarticaTestiranje());
            kartice.TabPages.Add(KarticaPodesavanja());
            kartice.SelectedIndexChanged += (s, e) => OsveziKarticu();
            tajmer.Tick += (s, e) => ProveriTestiranje();
            Directory.CreateDirectory(p.Radni);
            if (!Directory.Exists(Path.Combine(p.Radni, p.RadniProstor))) Directory.CreateDirectory(Path.Combine(p.Radni, p.RadniProstor));
            OsveziProstore();
            OtvoriProstor(p.RadniProstor);
            OsveziStatus();
        }

        private static DataGridView Mreza() => new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells, RowHeadersVisible = false, BackgroundColor = SystemColors.Window,
        };

        private static Button Dugme(string tekst, EventHandler klik) { var b = new Button { Text = tekst, AutoSize = true, Margin = new Padding(3) }; b.Click += klik; return b; }

        private static FlowLayoutPanel Traka(params Control[] k)
        {
            var f = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(3) };
            f.Controls.AddRange(k);
            return f;
        }

        private void Poruka(string t) { lPoruka.Text = t; Application.DoEvents(); }

        // ------------------------------------------------------------------------------------------- radni prostor

        private void OsveziProstore()
        {
            cbProstor.Items.Clear();
            foreach (var d in Directory.GetDirectories(p.Radni).Select(Path.GetFileName).OrderBy(x => x)) cbProstor.Items.Add(d);
        }

        private void OtvoriProstor(string ime)
        {
            rp = RadniProstor.Otvori(Path.Combine(p.Radni, ime));
            p.RadniProstor = ime; p.Sacuvaj();
            cbProstor.SelectedItem = ime;
            OsveziPoruke();
            OsveziVerzije();
            OsveziStatus();
        }

        private void OsveziStatus()
        {
            lProstor.Text = "Radni prostor: " + rp?.Ime;
            var kon = p.Online && !string.IsNullOrEmpty(p.Konekcija());
            lRezim.Text = p.Online ? (kon ? "ONLINE (upis u planiranje.tmp*)" : "ONLINE – konekcija nije podešena!") : "OFFLINE";
            lRezim.ForeColor = p.Online ? (kon ? Color.DarkGreen : Color.Red) : Color.DarkOrange;
            var d = SifarniciKopija.Datum(Path.Combine(p.Koren, "sifarnici"));
            lSifarnici.Text = "Šifarnici (kopija): " + (d.HasValue ? d.Value.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) : "nema kopije");
            rbOnline.Checked = p.Online; rbOffline.Checked = !p.Online;
            lKonekcija.Text = "Konekcija: " + (string.IsNullOrEmpty(p.Konekcija()) ? "nije podešena" : "podešena (DPAPI, " + p.KonekcijaFajl + ")");
            tbVerzija.Text = p.Verzija;
        }

        // ------------------------------------------------------------------------------------------- kartica Poruke

        private TabPage KarticaPoruke()
        {
            var t = new TabPage("Poruke");
            cbProstor.SelectedIndexChanged += (s, e) => { if (cbProstor.SelectedItem is string ime && ime != rp?.Ime) OtvoriProstor(ime); };
            foreach (var (k, w) in new[] { ("NNN", 50), ("Datum", 120), ("Od", 200), ("Naslov", 420), ("Prilozi", 260), ("Izvor", 60), ("Status", 120), ("Odluka", 300) })
                lvPoruke.Columns.Add(k, w);
            lvPoruke.DoubleClick += (s, e) => { kartice.SelectedIndex = 1; };
            var traka = Traka(new Label { Text = "Radni prostor:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, cbProstor,
                Dugme("Novi…", (s, e) => NoviProstor()),
                Dugme("Uvezi fajlove (.msg, .eml, .zip)…", (s, e) => UveziFajlove()),
                Dugme("Uvezi folder…", (s, e) => UveziFolder()),
                new Label { Text = "  Verzija:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, tbVerzija,
                Dugme("Obradi izabrano", async (s, e) => await Obradi(Izabrane())),
                Dugme("Obradi sve", async (s, e) => await Obradi(rp.Hronoloski())),
                Dugme("Obradi ponovo neuspele", async (s, e) => { var nu = rp.Neuspeli(VerzijaIzPolja()); if (nu.Count == 0) MessageBox.Show("Nema neuspelih poruka u verziji " + VerzijaIzPolja() + "."); else await Obradi(nu); }),
                chSamoNit, chBrisiTmp, chAnaliza,
                Dugme("Kreni ispočetka", (s, e) => { if (MessageBox.Show("Obrisati stanje radnog prostora (dosije, ponavljanja)? Keš čitanja i rezultati ostaju.", "Kreni ispočetka", MessageBoxButtons.YesNo) == DialogResult.Yes) { rp.KreniIspocetka(); OsveziPoruke(); } }),
                Dugme("Otvori folder", (s, e) => Process.Start("explorer.exe", rp.Koren)));
            t.Controls.Add(lvPoruke);
            t.Controls.Add(traka);
            return t;
        }

        private void NoviProstor()
        {
            var ime = Unos("Ime novog radnog prostora:", "Novi");
            if (string.IsNullOrWhiteSpace(ime)) return;
            ime = RadniProstor.BezbednoIme(ime, 40);
            Directory.CreateDirectory(Path.Combine(p.Radni, ime));
            OsveziProstore();
            OtvoriProstor(ime);
        }

        private static string Unos(string pitanje, string podrazumevano)
        {
            using (var f = new Form { Text = "CimRadionica", Width = 420, Height = 150, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false })
            {
                var l = new Label { Text = pitanje, Left = 10, Top = 10, AutoSize = true };
                var tb = new TextBox { Text = podrazumevano, Left = 10, Top = 32, Width = 380 };
                var ok = new Button { Text = "U redu", DialogResult = DialogResult.OK, Left = 230, Top = 65 };
                var ne = new Button { Text = "Odustani", DialogResult = DialogResult.Cancel, Left = 315, Top = 65 };
                f.Controls.AddRange(new Control[] { l, tb, ok, ne }); f.AcceptButton = ok; f.CancelButton = ne;
                return f.ShowDialog() == DialogResult.OK ? tb.Text : null;
            }
        }

        private void UveziFajlove()
        {
            using (var d = new OpenFileDialog { Multiselect = true, Filter = "Poruke (*.msg;*.eml;*.zip)|*.msg;*.eml;*.zip|Sve (*.*)|*.*" })
                if (d.ShowDialog() == DialogResult.OK) Uvezi(d.FileNames);
        }

        private void UveziFolder()
        {
            using (var d = new FolderBrowserDialog { Description = "Folder sa porukama (.msg/.eml) ili folder jednog maila (prilozi + mail.txt/mail.docx)" })
                if (d.ShowDialog() == DialogResult.OK) Uvezi(new[] { d.SelectedPath });
        }

        private void Uvezi(IEnumerable<string> putanje)
        {
            Cursor = Cursors.WaitCursor;
            var greske = new List<string>();
            var nove = rp.Uvezi(putanje, greske);
            Cursor = Cursors.Default;
            OsveziPoruke();
            Poruka("Uvezeno: " + nove.Count + " novih poruka" + (greske.Count > 0 ? "; greške: " + greske.Count : ""));
            if (greske.Count > 0) MessageBox.Show(string.Join(Environment.NewLine, greske.Take(30)), "Uvoz – greške");
        }

        private void OsveziPoruke()
        {
            lvPoruke.BeginUpdate();
            lvPoruke.Items.Clear(); lvPoruke.Groups.Clear();
            var hron = rp.Hronoloski();
            int n = 0;
            foreach (var nit in rp.Niti())
            {
                n++;
                var g = new ListViewGroup("nit " + n + ": " + nit[0].Naslov + " (" + nit.Count + ")");
                lvPoruke.Groups.Add(g);
                foreach (var s in nit)
                {
                    var it = new ListViewItem(new[] { (hron.IndexOf(s) + 1).ToString("000"), s.Datum.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture), s.Od, s.Naslov, string.Join(", ", s.Prilozi), s.Izvor, s.Status, s.Odluka }) { Group = g, Tag = s };
                    if (s.Odluka.Contains("izveštaj")) it.ForeColor = Color.DarkRed;
                    lvPoruke.Items.Add(it);
                }
            }
            lvPoruke.EndUpdate();
            Poruka("Poruka: " + hron.Count + ", niti: " + n + ", obrađeno (" + p.Verzija + "): " + hron.Count(s => s.Rezultati.ContainsKey(p.Verzija)));
        }

        private List<StavkaProstora> Izabrane()
        {
            var izabrane = lvPoruke.SelectedItems.Cast<ListViewItem>().Select(i => (StavkaProstora)i.Tag).ToList();
            if (chSamoNit.Checked && izabrane.Count > 0)
            {
                var nit = rp.Niti().FirstOrDefault(x => x.Contains(izabrane[0]));
                if (nit != null) izabrane = nit;
            }
            return izabrane;
        }

        private string VerzijaIzPolja() => string.IsNullOrWhiteSpace(tbVerzija.Text) ? RezultatJson.VerzijaPrograma : tbVerzija.Text.Trim();

        /// <summary>Obrada u delovima od po VelicinaDela poruka; svaki deo u posebnom procesu (CimRadionica --radnik).</summary>
        private async Task Obradi(List<StavkaProstora> stavke)
        {
            if (radi || stavke.Count == 0) return;
            p.Verzija = VerzijaIzPolja(); p.Sacuvaj();
            var konekcija = p.Online ? p.Konekcija() : null;
            if (p.Online && string.IsNullOrEmpty(konekcija)) { MessageBox.Show("Online režim: konekcija nije podešena (kartica Podešavanja)."); return; }
            if (p.Online && chBrisiTmp.Checked)
            {
                if (MessageBox.Show("Obrisati PODATKE iz planiranje.tmp* (DELETE; tmpSinonimi i tmpNHM ostaju)?", "Brisanje tmp podataka", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                try { RadniProstor.ObrisiTmpPodatke(konekcija); } catch (Exception ex) { MessageBox.Show("Brisanje nije uspelo: " + ex.Message); return; }
            }
            // v3.0.1 (4.3): bez Syncfusion ključa Word prilozi se ne čitaju – jasno na početku obrade
            if (!Saobracaj.Agent.CimProvera.Prilozi.WordUPdf.UcitajKljuc()
                && MessageBox.Show(PorukaBezSyncfusion() + Environment.NewLine + Environment.NewLine + "Nastaviti obradu bez Word priloga?", "Syncfusion ključ", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            radi = true;
            var hron = rp.Hronoloski();
            var redom = stavke.Distinct().OrderBy(s => hron.IndexOf(s)).ToList();
            napredak.Maximum = redom.Count; napredak.Value = 0;
            var delovi = Enumerable.Range(0, (redom.Count + p.VelicinaDela - 1) / p.VelicinaDela).Select(i => redom.Skip(i * p.VelicinaDela).Take(p.VelicinaDela).ToList()).ToList();
            var sw = Stopwatch.StartNew();
            var greske = new List<string>();
            // v3.0 (odluka 18.11.): pred-čitanje (OCR u keš) sa najviše BrojRadnika radnika istovremeno, nižeg prioriteta
            PostaviRadnike(p);
            if (p.BrojRadnika > 1 && redom.Count > 1)
            {
                var citano = 0;
                Poruka("Pred-čitanje (" + p.BrojRadnika + " radnika)…");
                await Task.Run(() => PredCitaj(rp.Koren, redom.Select(x => x.Kljuc).ToList(), p, null, l =>
                {
                    if (!(l.StartsWith("CITANO ") || l.StartsWith("CITANJE-GRESKA "))) return;
                    var c = System.Threading.Interlocked.Increment(ref citano);
                    BeginInvoke((Action)(() => Poruka("Pred-čitanje " + c + "/" + redom.Count + " (" + p.BrojRadnika + " radnika, " + sw.Elapsed.ToString(@"hh\:mm\:ss") + ")")));
                }));
            }
            foreach (var deo in delovi)
            {
                var posao = new PosaoRadnika { Koren = rp.Koren, Kljucevi = deo.Select(x => x.Kljuc).ToList(), Online = p.Online, Verzija = p.Verzija, Kes = p.Kes, Analiza = chAnaliza.Checked ? rp.Koren : null };
                var javljeni = new HashSet<string>();
                var neuspeh = await Task.Run(() => PokreniRadnika(posao, konekcija, red =>
                {
                    var m = System.Text.RegularExpressions.Regex.Match(red, @"^(?:OK|GRESKA) (.+?) \| ");
                    if (m.Success) lock (javljeni) javljeni.Add(m.Groups[1].Value);
                    BeginInvoke((Action)(() =>
                    {
                        if (red.StartsWith("OK ") || red.StartsWith("GRESKA ")) { napredak.Value = Math.Min(napredak.Maximum, napredak.Value + 1); if (red.StartsWith("GRESKA ")) greske.Add(red); }
                        Poruka("Obrada " + napredak.Value + "/" + napredak.Maximum + " (" + sw.Elapsed.ToString(@"hh\:mm\:ss") + "): " + (red.Length > 140 ? red.Substring(0, 140) : red));
                    }));
                }));
                // v3.0: pad radnika se nikad ne prećutkuje – poruke dela bez ishoda dobijaju „nije obrađen – greška okruženja …“; obrada ide dalje
                List<StavkaProstora> neobradjene;
                lock (javljeni) neobradjene = RadniProstor.ZabeleziNeobradjene(rp.Koren, posao.Kljucevi, javljeni, p.Verzija, neuspeh, p.Online, chAnaliza.Checked);
                if (neobradjene.Count > 0) napredak.Value = Math.Min(napredak.Maximum, napredak.Value + neobradjene.Count);
                rp = RadniProstor.Otvori(rp.Koren);
                OsveziPoruke();
            }
            if (chAnaliza.Checked) Analiza.ZbirnoIzFoldera(rp.Koren, p.Verzija);
            try { Zbirno.Sacuvaj(rp, p.Verzija, PrethodnaVerzija()); } catch (Exception ex) { greske.Add("Zbirni Excel: " + ex.Message); }
            radi = false;
            OsveziVerzije();
            Poruka("Gotovo: " + redom.Count + " poruka za " + sw.Elapsed.ToString(@"hh\:mm\:ss") + (greske.Count > 0 ? ", grešaka " + greske.Count : "") + ". Zbirni Excel: " + Zbirno.Putanja(rp, p.Verzija));
            if (greske.Count > 0) MessageBox.Show(string.Join(Environment.NewLine, greske.Take(20)), "Obrada – greške");
            // v3.0: spisak poruka koje nisu obrađene (greška okruženja) + ponuda „Obradi ponovo neuspele“
            var neuspeli = rp.Neuspeli(p.Verzija).Where(x => redom.Any(y => y.Kljuc == x.Kljuc)).ToList();
            if (neuspeli.Count > 0
                && MessageBox.Show("NISU OBRAĐENI (" + neuspeli.Count + "):" + Environment.NewLine + string.Join(Environment.NewLine, neuspeli.Take(25).Select(x => rp.Rb(x).ToString("000") + "  " + x.Naslov + " – " + x.Odluka))
                                   + (neuspeli.Count > 25 ? Environment.NewLine + "… i još " + (neuspeli.Count - 25) : "") + Environment.NewLine + Environment.NewLine + "Obraditi ponovo neuspele sada?",
                                   "Obrada – neobrađene poruke", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                await Obradi(neuspeli);
        }

        /// <summary>
        /// Proces radnika; konekcija samo kroz promenljivu okruženja procesa (nikad u fajl posla). Vraća null kada je radnik završio
        /// uspešno, inače razlog (izlazni kod i prva linija izuzetka) – pozivalac tada zaustavlja obradu (redosled je hronološki).
        /// </summary>
        /// <summary>v3.0: prioritet procesa radnika (podešavanje PrioritetRadnika; podrazumevano BelowNormal).</summary>
        internal static ProcessPriorityClass PrioritetRadnika = ProcessPriorityClass.BelowNormal;

        internal static void PostaviRadnike(Podesavanja p)
        {
            PrioritetRadnika = Enum.TryParse<ProcessPriorityClass>(p.PrioritetRadnika ?? "", true, out var pr) && pr != ProcessPriorityClass.RealTime && pr != ProcessPriorityClass.High ? pr : ProcessPriorityClass.BelowNormal;
        }

        /// <summary>
        /// v3.0 (odluka 18.11.): pred-čitanje – poruke se dele na BrojRadnika radnika koji istovremeno čitaju priloge (OCR) u keš; ništa se ne
        /// obrađuje ni ne upisuje. Hronološka obrada posle ide iz keša jednim radnikom, pa najviše BrojRadnika procesa radi u isto vreme.
        /// </summary>
        /// <summary>v3.0.1 (4.3): poruka kada Syncfusion ključ nije podešen za ovaj nalog (bez vrednosti ključa).</summary>
        internal static string PorukaBezSyncfusion() =>
            "Syncfusion licencni ključ nije podešen za nalog " + Environment.UserName + " (" + Saobracaj.Agent.CimProvera.Prilozi.WordUPdf.StanjeKljuca + "). "
            + "Word prilozi (.doc/.docx/.rtf) neće biti pročitani i idu na ručnu proveru. Podešavanje: ponovo pokrenuti Instaliraj-CimRadionica.ps1 (unos ključa).";

        internal static void PredCitaj(string koren, List<string> kljucevi, Podesavanja p, string konekcija, Action<string> red)
        {
            var n = Math.Max(1, Math.Min(p.BrojRadnika, kljucevi.Count));
            var grupe = Enumerable.Range(0, n).Select(i => kljucevi.Where((k, j) => j % n == i).ToList()).Where(g => g.Count > 0).ToList();
            var zadaci = grupe.Select(g => System.Threading.Tasks.Task.Run(() =>
                PokreniRadnika(new PosaoRadnika { Koren = koren, Kljucevi = g, Online = false, Verzija = "pred-citanje", Kes = p.Kes, SamoCitanje = true }, null, red))).ToArray();
            System.Threading.Tasks.Task.WaitAll(zadaci);
        }

        internal static string PokreniRadnika(PosaoRadnika posao, string konekcija, Action<string> red)
        {
            // Windows Smart App Control povremeno odbije učitavanje nepotpisanog DLL-a (FileLoadException 0x800711C7) – radnik padne pre
            // prve poruke. Tada je ponovni pokušaj bezbedan (ništa nije obrađeno); posle bar jedne obrađene poruke se ne ponavlja.
            string neuspeh = null;
            for (int pokusaj = 1; pokusaj <= 3; pokusaj++)
            {
                var obradjeno = 0;
                neuspeh = PokreniRadnikaJednom(posao, konekcija, l => { obradjeno++; red(l); });
                if (neuspeh == null || obradjeno > 0) return neuspeh;
                if (pokusaj < 3) System.Threading.Thread.Sleep(5000);
            }
            return neuspeh + " (3 pokušaja)";
        }

        private static string PokreniRadnikaJednom(PosaoRadnika posao, string konekcija, Action<string> red)
        {
            var fajl = Path.Combine(Path.GetTempPath(), "CimRadionica-posao-" + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(fajl, JsonConvert.SerializeObject(posao), new UTF8Encoding(false));
            try
            {
                var psi = new ProcessStartInfo(Application.ExecutablePath, "--radnik \"" + fajl + "\"")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8 };
                if (!string.IsNullOrEmpty(konekcija)) psi.EnvironmentVariables["CIMPROVERA_KONEKCIJA"] = konekcija;
                else psi.EnvironmentVariables.Remove("CIMPROVERA_KONEKCIJA");
                string izuzetak = null;
                // v3.0: Tesseract (OpenMP) u radniku koristi jednu nit – opterećenje = broj radnika, ne sva jezgra
                psi.EnvironmentVariables["OMP_THREAD_LIMIT"] = "1";
                using (var pr = Process.Start(psi))
                {
                    try { pr.PriorityClass = PrioritetRadnika; } catch { }
                    pr.ErrorDataReceived += (s, e) => { if (izuzetak == null && e.Data != null && e.Data.Contains("Exception")) izuzetak = e.Data.Trim(); };
                    pr.BeginErrorReadLine();
                    string l;
                    while ((l = pr.StandardOutput.ReadLine()) != null) red(l);
                    pr.WaitForExit();
                    return pr.ExitCode == 0 ? null : "radnik je završio sa kodom " + pr.ExitCode + (izuzetak != null ? ": " + izuzetak : "");
                }
            }
            catch (Exception ex) { return "radnik nije pokrenut: " + ex.Message; }
            finally { try { File.Delete(fajl); } catch { } }
        }

        private string PrethodnaVerzija() => Verzije().Where(v => v != p.Verzija).OrderByDescending(v => File.GetLastWriteTime(Path.Combine(rp.Rezultati, v))).FirstOrDefault();

        private List<string> Verzije() => Directory.Exists(rp.Rezultati) ? Directory.GetDirectories(rp.Rezultati).Select(Path.GetFileName).OrderBy(x => x).ToList() : new List<string>();

        private void OsveziVerzije()
        {
            foreach (var cb in new[] { cbBila, cbSada }) { cb.Items.Clear(); foreach (var v in Verzije()) cb.Items.Add(v); }
            if (cbSada.Items.Count > 0) cbSada.SelectedIndex = cbSada.Items.Count - 1;
            if (cbBila.Items.Count > 1) cbBila.SelectedIndex = cbBila.Items.Count - 2; else if (cbBila.Items.Count > 0) cbBila.SelectedIndex = 0;
        }

        // ------------------------------------------------------------------------------------------- kartica Rezultat

        private TabPage KarticaRezultat()
        {
            var t = new TabPage("Rezultat");
            var unutra = new TabControl { Dock = DockStyle.Fill };
            var o = new TabPage("Odgovor (Live, nije poslat)"); o.Controls.Add(wbOdgovor);
            var j = new TabPage("rezultat.json"); j.Controls.Add(tbJson);
            var l = new TabPage("log.txt"); l.Controls.Add(tbLog);
            unutra.TabPages.AddRange(new[] { o, j, l });
            t.Controls.Add(unutra);
            t.Controls.Add(Traka(Dugme("Otvori PRIMER", (s, e) => OtvoriIzRezultata("PRIMER_*.pdf")), Dugme("Otvori folder rezultata", (s, e) => OtvoriIzRezultata(null)),
                Dugme("Otvori .eml", (s, e) => { var s0 = Izabrana(); if (s0 != null) Process.Start(Path.Combine(rp.Poruke, s0.Fajl)); })));
            t.Controls.Add(lRezultat);
            return t;
        }

        private StavkaProstora Izabrana() => lvPoruke.SelectedItems.Count > 0 ? (StavkaProstora)lvPoruke.SelectedItems[0].Tag : null;

        private string FolderRezultata(StavkaProstora s) => s != null && s.Rezultati.TryGetValue(p.Verzija, out var rel) ? Path.Combine(rp.Koren, rel) : null;

        private void OtvoriIzRezultata(string maska)
        {
            var f = FolderRezultata(Izabrana());
            if (f == null || !Directory.Exists(f)) return;
            if (maska == null) { Process.Start("explorer.exe", f); return; }
            var x = Directory.GetFiles(f, maska).FirstOrDefault();
            if (x != null) Process.Start(x); else MessageBox.Show("PRIMER nije napravljen za ovaj mail.");
        }

        private void PrikaziRezultat()
        {
            var s = Izabrana();
            var f = FolderRezultata(s);
            if (s == null) { lRezultat.Text = "Izaberite poruku na kartici Poruke."; return; }
            lRezultat.Text = s.Naslov + " – " + s.Od + ", " + s.Datum.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) + Environment.NewLine + "Odluka: " + s.Odluka + (f == null ? " (nije obrađeno u verziji " + p.Verzija + ")" : "");
            if (f == null) { wbOdgovor.DocumentText = "<p>Nije obrađeno.</p>"; tbJson.Text = ""; tbLog.Text = ""; return; }
            var o = Path.Combine(f, "odgovor.html");
            if (File.Exists(o)) wbOdgovor.Navigate(o);
            tbJson.Text = File.Exists(Path.Combine(f, "rezultat.json")) ? File.ReadAllText(Path.Combine(f, "rezultat.json"), Encoding.UTF8).Replace("\n", Environment.NewLine) : "";
            tbLog.Text = File.Exists(Path.Combine(f, "log.txt")) ? File.ReadAllText(Path.Combine(f, "log.txt"), Encoding.UTF8).Replace("\n", Environment.NewLine) : "";
        }

        // ------------------------------------------------------------------------------------------- kartica Zbirno i poređenje

        private TabPage KarticaZbirno()
        {
            var t = new TabPage("Zbirno i poređenje");
            t.Controls.Add(wbPoredjenje);
            t.Controls.Add(Traka(
                Dugme("Napravi Zbirni Excel", (s, e) => { var x = Zbirno.Sacuvaj(rp, p.Verzija, PrethodnaVerzija()); Poruka("Zbirni Excel: " + x); }),
                Dugme("Otvori Zbirni Excel", (s, e) => { var x = Zbirno.Putanja(rp, p.Verzija); if (!File.Exists(x)) Zbirno.Sacuvaj(rp, p.Verzija, PrethodnaVerzija()); Process.Start(x); }),
                new Label { Text = "  Uporedi:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, cbBila, new Label { Text = "→", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, cbSada,
                Dugme("Uporedi verzije", (s, e) => Uporedi()),
                Dugme("Napravi _Analiza (izabrane / sve)", (s, e) => NapraviAnalizu()),
                chSamoNe, Dugme("Napravi paket…", (s, e) => Paket()), Dugme("Paket za Claude…", (s, e) => PaketClaude())));
            return t;
        }

        private void Uporedi()
        {
            if (!(cbBila.SelectedItem is string a) || !(cbSada.SelectedItem is string b) || a == b) { MessageBox.Show("Izaberite dve različite verzije."); return; }
            foreach (var v in new[] { a, b }) if (!File.Exists(Zbirno.Putanja(rp, v))) Zbirno.Sacuvaj(rp, v);
            var x = Zbirno.SacuvajPoredjenje(rp, a, b);
            wbPoredjenje.Navigate(Path.ChangeExtension(x, ".html"));
            Poruka("Poređenje: " + x);
        }

        private void NapraviAnalizu()
        {
            var stavke = lvPoruke.SelectedItems.Count > 0 ? Izabrane() : rp.Hronoloski();
            var obradjene = stavke.Where(s => s.Rezultati.ContainsKey(p.Verzija)).ToList();
            if (obradjene.Count < stavke.Count && MessageBox.Show("Analiza traži ponovnu obradu poruka (strane se crtaju iz priloga). Obraditi " + stavke.Count + " poruka sa analizom?", "_Analiza", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            chAnaliza.Checked = true;
            _ = Obradi(stavke);
        }

        private void Paket()
        {
            using (var d = new SaveFileDialog { Filter = "Zip (*.zip)|*.zip", FileName = "CimRadionica_" + rp.Ime + "_" + p.Verzija + "_" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".zip" })
            {
                if (d.ShowDialog() != DialogResult.OK) return;
                if (!File.Exists(Zbirno.Putanja(rp, p.Verzija))) Zbirno.Sacuvaj(rp, p.Verzija);
                Zbirno.Paket(rp, p.Verzija, d.FileName, chSamoNe.Checked, out var preskoceno);
                Poruka("Paket: " + d.FileName + (preskoceno.Count > 0 ? " (izostavljeno " + preskoceno.Count + " fajlova sa tajnama)" : ""));
            }
        }

        /// <summary>v3.0: paket za Claude – samo _Analiza (slike sivo, 80 dpi), Zbirno, Poređenje, Rečnik (+ PROCITAJ.md iz izabranog foldera); delovi ≤ 100 MB.</summary>
        private void PaketClaude()
        {
            using (var d = new FolderBrowserDialog { Description = "Folder za paket za Claude (delovi do 100 MB)" })
            {
                if (d.ShowDialog() != DialogResult.OK) return;
                if (!File.Exists(Zbirno.Putanja(rp, p.Verzija))) Zbirno.Sacuvaj(rp, p.Verzija);
                var recnik = Path.Combine(d.SelectedPath, "Recnik_izraza_" + p.Verzija + ".xlsx");
                Recnik(rp, p.Verzija, recnik);
                var dodatni = new List<string> { recnik };
                var procitaj = Path.Combine(d.SelectedPath, "PROCITAJ.md");
                if (File.Exists(procitaj)) dodatni.Add(procitaj);
                var delovi = Zbirno.PaketZaClaude(rp, p.Verzija, d.SelectedPath, dodatni, out var preskoceno);
                Poruka("Paket za Claude: " + string.Join(", ", delovi.Select(x => Path.GetFileName(x) + " " + (new FileInfo(x).Length / 1048576.0).ToString("0") + " MB")) + (preskoceno.Count > 0 ? " (izostavljeno " + preskoceno.Count + ")" : ""));
            }
        }

        // ------------------------------------------------------------------------------------------- kartica Pravila

        private TabPage KarticaPravila()
        {
            var t = new TabPage("Pravila");
            t.Controls.Add(gPravila);
            t.Controls.Add(Traka(Dugme("Učitaj", (s, e) => UcitajPravila()), Dugme("Izvoz u Excel…", (s, e) => IzvozPravila())));
            return t;
        }

        private DataTable TabelaPravila()
        {
            var tb = new DataTable();
            foreach (var k in new[] { "Grupa", "ID", "Rubrika", "Naziv", "Težina", "Popunjava", "Uslov / provera", "Poruka", "Primer" }) tb.Columns.Add(k);
            var put = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pravila", "cim_validation_rules.json");
            if (!File.Exists(put)) return tb;
            var j = JObject.Parse(File.ReadAllText(put, Encoding.UTF8));
            foreach (var prop in j.Properties().Where(x => x.Value is JArray))
                foreach (var r in ((JArray)prop.Value).OfType<JObject>().Where(x => x["id"] != null))
                    tb.Rows.Add(prop.Name, (string)r["id"], (string)r["field"] ?? "", (string)r["name"] ?? "", (string)r["severity"] ?? "", (string)r["popunjava"] ?? "",
                        (string)(r["check"] ?? r["condition"] ?? r["if"]) ?? "", (string)r["message"] ?? "", (string)r["primer"] ?? "");
            return tb;
        }

        private void UcitajPravila() { gPravila.DataSource = TabelaPravila(); }

        private void IzvozPravila()
        {
            using (var d = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = "Pravila_" + DateTime.Now.ToString("yyyyMMdd") + ".xlsx" })
            {
                if (d.ShowDialog() != DialogResult.OK) return;
                var tb = TabelaPravila();
                var l = new XlsxList { Ime = "Pravila" };
                l.Zaglavlje.AddRange(tb.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
                foreach (DataRow r in tb.Rows) l.Redovi.Add(r.ItemArray);
                l.Prelom.Add(6); l.Prelom.Add(7); l.Sirine[6] = 50; l.Sirine[7] = 60; l.Sirine[8] = 40;
                Xlsx.Sacuvaj(d.FileName, l);
                Poruka("Pravila: " + d.FileName);
            }
        }

        // ------------------------------------------------------------------------------------------- Poslovi, Teretnice, Događaji (samo čitanje)

        private TabPage KarticaPoslovi()
        {
            var t = new TabPage("Poslovi");
            var deli = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 380 };
            deli.Panel1.Controls.Add(gPoslovi);
            var donji = new TabControl { Dock = DockStyle.Fill };
            var a = new TabPage("Poreklo polja"); a.Controls.Add(gPoreklo);
            var b = new TabPage("Istorija"); b.Controls.Add(gIstorija);
            donji.TabPages.AddRange(new[] { a, b });
            deli.Panel2.Controls.Add(donji);
            gPoslovi.SelectionChanged += (s, e) => DetaljiPosla();
            t.Controls.Add(deli);
            cbIzvorPoslova.SelectedIndexChanged += (s, e) => UcitajPoslove();
            t.Controls.Add(Traka(Dugme("Osveži", (s, e) => UcitajPoslove()), new Label { Text = "Izvor:", AutoSize = true, Padding = new Padding(6, 6, 0, 0) }, cbIzvorPoslova,
                new Label { Text = "online: planiranje.tmpJobs (samo čitanje); offline: predlog iz rezultat.json", AutoSize = true, Padding = new Padding(6, 6, 0, 0) }));
            return t;
        }

        private TabPage KarticaTeretnice()
        {
            var t = new TabPage("Teretnice");
            var deli = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 300 };
            deli.Panel1.Controls.Add(gTeretnice); deli.Panel2.Controls.Add(gStavke);
            gTeretnice.SelectionChanged += (s, e) => StavkeTeretnice();
            t.Controls.Add(deli);
            cbIzvorTeretnica.SelectedIndexChanged += (s, e) => UcitajTeretnice();
            t.Controls.Add(Traka(Dugme("Osveži", (s, e) => UcitajTeretnice()), new Label { Text = "Izvor:", AutoSize = true, Padding = new Padding(6, 6, 0, 0) }, cbIzvorTeretnica,
                new Label { Text = "zamenjene teretnice (Zamenjena = 1) nisu aktivne", AutoSize = true, Padding = new Padding(6, 6, 0, 0) }));
            return t;
        }

        private TabPage KarticaDogadjaji()
        {
            var t = new TabPage("Događaji");
            t.Controls.Add(gDogadjaji);
            t.Controls.Add(Traka(Dugme("Osveži", (s, e) => UcitajDogadjaje()), Dugme("Rečnik izraza u Excel…", (s, e) => RecnikIzraza())));
            return t;
        }

        private DataTable Upit(string sql, params object[] arg)
        {
            var tb = new DataTable();
            using (var c = UpisUBazu.Otvori(p.Konekcija()))
            using (var cmd = new SqlCommand(sql, c) { CommandTimeout = 120 })
            {
                for (int i = 0; i + 1 < arg.Length; i += 2) cmd.Parameters.AddWithValue((string)arg[i], arg[i + 1]);
                using (var da = new SqlDataAdapter(cmd)) da.Fill(tb);
            }
            return tb;
        }

        private bool Baza => p.Online && !string.IsNullOrEmpty(p.Konekcija());

        // v3.0.3 (3.5, OP_919): pregledi po izvoru – podrazumevano podaci alata; agent i svi na izbor
        private static ComboBox IzborIzvora() { var cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 }; cb.Items.AddRange(new object[] { "Alat", "Agent", "Svi" }); cb.SelectedIndex = 0; return cb; }
        private readonly ComboBox cbIzvorPoslova = IzborIzvora(), cbIzvorTeretnica = IzborIzvora();
        private bool? imaIzvorKolonu;

        /// <summary>„WHERE alias.Izvor = 'Alat'“ (ili prazno za „Svi“ / pre OP_919).</summary>
        private string UslovIzvora(ComboBox cb, string alias)
        {
            if (imaIzvorKolonu == null)
                try { imaIzvorKolonu = Upit("SELECT COL_LENGTH('planiranje.tmpJobs', 'Izvor') AS d").Rows[0][0] is object d && !(d is DBNull); } catch { imaIzvorKolonu = false; }
            var izv = cb.SelectedItem as string ?? "Alat";
            return imaIzvorKolonu == true && izv != "Svi" ? " WHERE " + alias + ".Izvor = '" + (izv == "Agent" ? "Agent" : "Alat") + "'" : "";
        }

        private void UcitajPoslove()
        {
            try
            {
                if (Baza)
                    gPoslovi.DataSource = Upit(@"SELECT TOP 500 j.ID, j.Verzija, so.Opis AS Otpravna, su.Opis AS Uputna, s1.Opis AS Stanica1, s3.Opis AS Stanica3, pl.PaNaziv AS Platilac,
 n1.Kod8 AS NHM1, j.BrojKola1, j.Neto1, j.Tara1, j.Bruto1, n2.Kod8 AS NHM2, j.BrojKola2, j.Neto2, j.Tara2, j.Bruto2, j.Referenca, j.BrojPonude, j.KljucPosiljke, LEFT(j.Napomena, 200) AS Napomena
FROM planiranje.tmpJobs j LEFT JOIN dbo.stanice so ON so.ID = j.OtpravnaID LEFT JOIN dbo.stanice su ON su.ID = j.UputnaID LEFT JOIN dbo.stanice s1 ON s1.ID = j.Stanica1ID
 LEFT JOIN dbo.stanice s3 ON s3.ID = j.Stanica3ID LEFT JOIN dbo.Partnerji pl ON pl.PaSifra = j.PlatilacID LEFT JOIN planiranje.tmpNHM n1 ON n1.ID = j.NHM1 LEFT JOIN planiranje.tmpNHM n2 ON n2.ID = j.NHM2
 " + UslovIzvora(cbIzvorPoslova, "j") + @"
ORDER BY j.ID DESC");
                else
                {
                    var tb = new DataTable();
                    foreach (var k in new[] { "NNN", "Naslov", "List" }.Concat(Zbirno.PoljaPosla.Where(x => x != "ID"))) tb.Columns.Add(k);
                    foreach (var s in rp.Hronoloski())
                    {
                        var j = Zbirno.Rezultat(rp, s, p.Verzija);
                        foreach (var pos in (j?["poslovi"] as JArray ?? new JArray()).OfType<JObject>())
                        {
                            var red = new List<object> { rp.Rb(s).ToString("000"), s.Naslov, (string)pos["list"] };
                            red.AddRange(Zbirno.PoljaPosla.Where(x => x != "ID").Select(x => (object)((string)pos["polja"]?[x] ?? "")));
                            tb.Rows.Add(red.ToArray());
                        }
                    }
                    gPoslovi.DataSource = tb;
                }
            }
            catch (Exception ex) { Poruka("Poslovi: " + ex.Message); }
        }

        private void DetaljiPosla()
        {
            if (!Baza || gPoslovi.CurrentRow == null || !gPoslovi.Columns.Contains("ID")) return;
            try
            {
                var id = Convert.ToInt32(gPoslovi.CurrentRow.Cells["ID"].Value);
                gPoreklo.DataSource = Upit("SELECT Kolona, Vrednost, Rubrika, TekstIzDokumenta, Pouzdanost, Kandidati FROM planiranje.tmpJobsPolje WHERE JobID = @id AND TeretnicaID IS NULL AND Kolona NOT LIKE 'TL:%' ORDER BY Kolona", "@id", id);
                gIstorija.DataSource = Upit("SELECT Vreme, Kolona, StaraVrednost, NovaVrednost, Razlog, Napomena FROM planiranje.tmpJobsIstorija WHERE JobID = @id ORDER BY ID DESC", "@id", id);
            }
            catch (Exception ex) { Poruka("Detalji posla: " + ex.Message); }
        }

        private void UcitajTeretnice()
        {
            if (!Baza) { Poruka("Teretnice: samo online (planiranje.tmpTeretnica)."); gTeretnice.DataSource = null; return; }
            try
            {
                var uslov = UslovIzvora(cbIzvorTeretnica, "t");   // postavlja i imaIzvorKolonu (OP_919: Izvor i Zamenjena)
                var zam = imaIzvorKolonu == true ? " t.Zamenjena, t.ZamenjenaTeretnicaID," : "";
                gTeretnice.DataSource = Upit(@"SELECT TOP 500 t.ID, t.JobID, t.Grupa," + zam + @" t.BrojTeretnice, t.Referenca, t.KljucSpiska, (SELECT COUNT(*) FROM planiranje.tmpTeretnicaStavke s WHERE s.BrojTeretnice = t.ID) AS Kola,
 (SELECT SUM(Tara) FROM planiranje.tmpTeretnicaStavke s WHERE s.BrojTeretnice = t.ID) AS Tara, (SELECT SUM(Neto) FROM planiranje.tmpTeretnicaStavke s WHERE s.BrojTeretnice = t.ID) AS Neto, t.Napomena
FROM planiranje.tmpTeretnica t" + uslov + " ORDER BY t.ID DESC");
            }
            catch (Exception ex) { Poruka("Teretnice: " + ex.Message); }
        }

        private void StavkeTeretnice()
        {
            if (!Baza || gTeretnice.CurrentRow == null) return;
            try { gStavke.DataSource = Upit("SELECT RB, BrojKola, Serija, BrojOsovina, Duzina, Tara, Neto, VRNP, RID, Dokument, Primedba FROM planiranje.tmpTeretnicaStavke WHERE BrojTeretnice = @t ORDER BY RB", "@t", Convert.ToInt32(gTeretnice.CurrentRow.Cells["ID"].Value)); }
            catch (Exception ex) { Poruka("Stavke: " + ex.Message); }
        }

        private List<(StavkaProstora s, JObject d)> SviDogadjaji() =>
            rp.Hronoloski().SelectMany(s => ((Zbirno.Rezultat(rp, s, p.Verzija)?["dogadjaji"] as JArray) ?? new JArray()).OfType<JObject>().Select(d => (s, d))).ToList();

        private void UcitajDogadjaje()
        {
            var tb = new DataTable();
            foreach (var k in new[] { "NNN", "Naslov", "Tip", "Vreme", "Vreme (tekst)", "Zona", "Mesto", "Vrednost", "Jezik", "Pouzdanost", "Rečenica" }) tb.Columns.Add(k);
            foreach (var (s, d) in SviDogadjaji())
                tb.Rows.Add(rp.Rb(s).ToString("000"), s.Naslov, (string)d["tip"], (string)d["vreme"], (string)d["vreme_tekst"], (string)d["zona"], (string)d["mesto"], (string)d["vrednost"], (string)d["jezik"], (string)d["pouzdanost"], (string)d["recenica"]);
            gDogadjaji.DataSource = tb;
        }

        private void RecnikIzraza()
        {
            using (var dlg = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = "Recnik_izraza_" + rp.Ime + ".xlsx" })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                Recnik(rp, p.Verzija, dlg.FileName);
                Poruka("Rečnik izraza: " + dlg.FileName);
            }
        }

        /// <summary>Rečnik izraza po jeziku i tipu (sa brojem) + svi događaji – Excel (2.10).</summary>
        internal static void Recnik(RadniProstor rp, string verzija, string izlaz)
        {
            var sve = rp.Hronoloski().SelectMany(s => ((Zbirno.Rezultat(rp, s, verzija)?["dogadjaji"] as JArray) ?? new JArray()).OfType<JObject>().Select(d => (s, d))).ToList();
            var r = new XlsxList { Ime = "Recnik" };
            r.Zaglavlje.AddRange(new[] { "Jezik", "Tip", "Izraz", "Broj" });
            foreach (var g in sve.GroupBy(x => ((string)x.d["jezik"], (string)x.d["tip"], ((string)x.d["izraz"] ?? "").ToLowerInvariant())).OrderBy(g => g.Key.Item1).ThenBy(g => g.Key.Item2).ThenByDescending(g => g.Count()))
                r.Redovi.Add(new object[] { g.Key.Item1, g.Key.Item2, g.Key.Item3, g.Count() });
            var d2 = new XlsxList { Ime = "Dogadjaji" };
            d2.Zaglavlje.AddRange(new[] { "NNN", "Datum maila", "Naslov", "Tip", "Vreme", "Vreme (tekst)", "Zona", "Mesto", "Vrednost", "Jezik", "Pouzdanost", "Rečenica" });
            foreach (var (s, d) in sve)
                d2.Redovi.Add(new object[] { rp.Rb(s).ToString("000"), s.Datum.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), s.Naslov, (string)d["tip"], (string)d["vreme"], (string)d["vreme_tekst"], (string)d["zona"], (string)d["mesto"], (string)d["vrednost"], (string)d["jezik"], (int?)d["pouzdanost"] ?? 0, (string)d["recenica"] });
            d2.Sirine[11] = 90; d2.Prelom.Add(11);
            Xlsx.Sacuvaj(izlaz, r, d2);
        }

        // ------------------------------------------------------------------------------------------- Testiranje (praćenje foldera, 2.11)

        private TabPage KarticaTestiranje()
        {
            var t = new TabPage("Testiranje");
            tbTestiranje.Text = p.TestiranjeFolder;
            t.Controls.Add(lbTestiranje);
            t.Controls.Add(Traka(new Label { Text = "Folder:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, tbTestiranje,
                Dugme("Pokreni praćenje", (s, e) => PokreniPracenje()), Dugme("Zaustavi", (s, e) => { tajmer.Stop(); posmatrac?.Dispose(); posmatrac = null; Zapis("praćenje zaustavljeno"); }),
                Dugme("Obradi sada (i neuspele)", (s, e) => ProveriTestiranje(true)),
                Dugme("Kreni ispočetka", (s, e) => KreniIspocetkaTestiranje()),
                new Label { Text = "Novi .msg/.eml (po datumu slanja) → radni prostor „Testiranje“, _Analiza u istom folderu. Beleške „X - napomena“ / „Zapazanja“ ulaze u ANALIZA.md.", AutoSize = true, Padding = new Padding(6, 6, 0, 0) }));
            return t;
        }

        private void Zapis(string t) { lbTestiranje.Items.Insert(0, DateTime.Now.ToString("HH:mm:ss") + "  " + t); }

        private void PokreniPracenje()
        {
            p.TestiranjeFolder = tbTestiranje.Text.Trim(); p.Sacuvaj();
            Directory.CreateDirectory(p.TestiranjeFolder);
            posmatrac?.Dispose();
            posmatrac = new FileSystemWatcher(p.TestiranjeFolder) { IncludeSubdirectories = false, EnableRaisingEvents = true };
            posmatrac.Created += (s, e) => BeginInvoke((Action)(() => Zapis("nov fajl: " + e.Name)));
            tajmer.Start();
            Zapis("praćenje: " + p.TestiranjeFolder);
            ProveriTestiranje();
        }

        private void KreniIspocetkaTestiranje()
        {
            if (MessageBox.Show("Radni prostor „Testiranje“ kreće ispočetka (stanje se briše, rezultati i _Analiza ostaju po verzijama). Nastaviti?", "Testiranje", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            var t = RadniProstor.Otvori(Path.Combine(p.Radni, "Testiranje"));
            t.KreniIspocetka();
            foreach (var s in t.Indeks.Stavke) s.Rezultati.Clear();
            t.Sacuvaj();
            Zapis("Testiranje: kreće ispočetka");
        }

        /// <summary>Nove poruke iz foldera Testiranje: uvoz u radni prostor „Testiranje“ i obrada po datumu slanja, sa _Analiza u folderu Testiranje.</summary>
        private async void ProveriTestiranje(bool iNeuspele = false)
        {
            if (radi || string.IsNullOrWhiteSpace(p.TestiranjeFolder) || !Directory.Exists(p.TestiranjeFolder)) return;
            var fajlovi = Directory.GetFiles(p.TestiranjeFolder).Where(f => System.Text.RegularExpressions.Regex.IsMatch(f, @"(?i)\.(msg|eml)$")).ToList();
            if (fajlovi.Count == 0) return;
            var test = RadniProstor.Otvori(Path.Combine(p.Radni, "Testiranje"));
            var nove = test.Uvezi(fajlovi);
            // v3.0: „Obradi sada“ obrađuje ponovo i poruke koje nisu obrađene zbog greške okruženja
            var neuspeleRanije = iNeuspele ? new HashSet<string>(test.Neuspeli(p.Verzija).Select(x => x.Kljuc)) : new HashSet<string>();
            var neobradjene = test.Hronoloski().Where(s => !s.Rezultati.ContainsKey(p.Verzija) || neuspeleRanije.Contains(s.Kljuc)).ToList();
            if (neobradjene.Count == 0) return;
            Zapis("novih: " + nove.Count + ", za obradu: " + neobradjene.Count);
            radi = true;
            try
            {
                var konekcija = p.Online ? p.Konekcija() : null;
                foreach (var deo in Enumerable.Range(0, (neobradjene.Count + p.VelicinaDela - 1) / p.VelicinaDela).Select(i => neobradjene.Skip(i * p.VelicinaDela).Take(p.VelicinaDela).ToList()))
                {
                    var posao = new PosaoRadnika { Koren = test.Koren, Kljucevi = deo.Select(x => x.Kljuc).ToList(), Online = p.Online, Verzija = p.Verzija, Kes = p.Kes, Analiza = p.TestiranjeFolder };
                    var javljeni = new HashSet<string>();
                    var neuspeh = await Task.Run(() => PokreniRadnika(posao, konekcija, red =>
                    {
                        var m = System.Text.RegularExpressions.Regex.Match(red, @"^(?:OK|GRESKA) (.+?) \| ");
                        if (m.Success) lock (javljeni) javljeni.Add(m.Groups[1].Value);
                        BeginInvoke((Action)(() => Zapis(red)));
                    }));
                    // v3.0: pad radnika – poruke bez ishoda dobijaju „nije obrađen – greška okruženja …“ (i ANALIZA.md u folderu Testiranje)
                    List<StavkaProstora> bez;
                    lock (javljeni) bez = RadniProstor.ZabeleziNeobradjene(test.Koren, posao.Kljucevi, javljeni, p.Verzija, neuspeh, p.Online, false);
                    foreach (var x in bez) { Analiza.NapisiNeuspeh(p.TestiranjeFolder, p.Verzija, RadniProstor.Otvori(test.Koren).Rb(x), x, Path.Combine(test.Koren, x.Rezultati[p.Verzija])); Zapis("NIJE OBRAĐEN: " + x.Naslov + " – " + x.Odluka); }
                }
                Analiza.ZbirnoIzFoldera(p.TestiranjeFolder, p.Verzija);
                test = RadniProstor.Otvori(test.Koren);
                Zbirno.Sacuvaj(test, p.Verzija);
                Zapis("gotovo – _Analiza\\" + p.Verzija + " u " + p.TestiranjeFolder);
                var nu = test.Neuspeli(p.Verzija);
                if (nu.Count > 0) Zapis("NISU OBRAĐENI (" + nu.Count + "): " + string.Join("; ", nu.Select(x => x.Naslov)) + " – dugme „Obradi sada (i neuspele)“ ih obrađuje ponovo.");
            }
            catch (Exception ex) { Zapis("greška: " + ex.Message); }
            finally { radi = false; }
        }

        // ------------------------------------------------------------------------------------------- Podešavanja

        private TabPage KarticaPodesavanja()
        {
            var t = new TabPage("Podešavanja");
            var f = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12), WrapContents = false, AutoScroll = true };
            rbOnline.CheckedChanged += (s, e) => { if (rbOnline.Checked != p.Online) { p.Online = rbOnline.Checked; p.Sacuvaj(); OsveziStatus(); } };
            f.Controls.Add(new Label { Text = "Režim rada", Font = new Font(Font, FontStyle.Bold), AutoSize = true });
            f.Controls.Add(rbOnline); f.Controls.Add(rbOffline);
            f.Controls.Add(lKonekcija);
            f.Controls.Add(Dugme("Podesi konekciju (DPAPI)…", (s, e) => PodesiKonekciju()));
            f.Controls.Add(new Label { Text = " ", AutoSize = true });
            f.Controls.Add(new Label { Text = "Šifarnici za offline (samo čitanje: stanice, Partnerji – korišćene kolone, tmpNHM, tmpSinonimi, Ponudba – samo PonStPon/PonPart)", AutoSize = true });
            f.Controls.Add(Dugme("Osveži lokalnu kopiju šifarnika", (s, e) => OsveziSifarnike()));
            f.Controls.Add(new Label { Text = " ", AutoSize = true });
            f.Controls.Add(new Label { Text = "Folderi: " + p.Koren + " (radni prostori: Radni\\, keš čitanja: kes\\, šifarnici: sifarnici\\)", AutoSize = true });
            f.Controls.Add(new Label { Text = "Alat NIKAD ne šalje mail: odgovor se priprema kao u režimu Live i čuva u odgovor.html. Stanje agenta (stanje\\posiljke.json) se ne dira.", AutoSize = true, ForeColor = Color.DarkRed });
            t.Controls.Add(f);
            return t;
        }

        private void PodesiKonekciju()
        {
            var k = Unos("Connection string (čuva se šifrovano – DPAPI, samo za ovog korisnika):", "");
            if (string.IsNullOrWhiteSpace(k)) return;
            try
            {
                using (var c = UpisUBazu.Otvori(k)) { }
                p.SacuvajKonekciju(k.Trim());
                OsveziStatus();
                Poruka("Konekcija je proverena i sačuvana (DPAPI).");
            }
            catch (Exception ex) { MessageBox.Show("Konekcija nije uspela: " + ex.Message); }
        }

        private void OsveziSifarnike()
        {
            var k = p.Konekcija();
            if (string.IsNullOrEmpty(k)) { MessageBox.Show("Za kopiju šifarnika potrebna je konekcija (Podesi konekciju)."); return; }
            try
            {
                Cursor = Cursors.WaitCursor;
                var d = SifarniciKopija.Napravi(k, Path.Combine(p.Koren, "sifarnici"));
                Poruka("Šifarnici kopirani: " + d.ToString("dd.MM.yyyy HH:mm"));
            }
            catch (Exception ex) { MessageBox.Show("Kopija šifarnika nije uspela: " + ex.Message); }
            finally { Cursor = Cursors.Default; OsveziStatus(); }
        }

        private void OsveziKarticu()
        {
            switch (kartice.SelectedTab?.Text)
            {
                case "Rezultat": PrikaziRezultat(); break;
                case "Pravila": if (gPravila.DataSource == null) UcitajPravila(); break;
                case "Poslovi": UcitajPoslove(); break;
                case "Teretnice": UcitajTeretnice(); break;
                case "Događaji": UcitajDogadjaje(); break;
            }
        }

        /// <summary>Snimci ekrana svih kartica (PNG) – za uputstvo. Vraća 0.</summary>
        public int Snimci(string folder)
        {
            Directory.CreateDirectory(folder);
            Show();
            if (lvPoruke.Items.Count > 0) lvPoruke.Items[Math.Min(1, lvPoruke.Items.Count - 1)].Selected = true;
            for (int i = 0; i < kartice.TabPages.Count; i++)
            {
                kartice.SelectedIndex = i;
                OsveziKarticu();
                var kraj = DateTime.Now.AddSeconds(i == 1 ? 3 : 1);
                while (DateTime.Now < kraj) { Application.DoEvents(); System.Threading.Thread.Sleep(50); }
                using (var b = new Bitmap(Width, Height))
                {
                    DrawToBitmap(b, new Rectangle(0, 0, Width, Height));
                    b.Save(Path.Combine(folder, (i + 1).ToString("00") + "_" + RadniProstor.BezbednoIme(kartice.TabPages[i].Text, 30) + ".png"), System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            Close();
            return 0;
        }
    }
}
