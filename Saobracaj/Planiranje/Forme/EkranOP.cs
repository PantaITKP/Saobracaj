using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Agenti_jobs;
using Saobracaj.Planiranje.Pravila;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Planiranje.Forme
{
    // Zajednički pribor ekrana planiranja: tabela sa filterima, čitanje van UI niti i poruke o greškama.
    internal static class EkranOP
    {
        private static readonly HashSet<string> SkriveneKolone = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "RowVer" };

        internal static DataGridView NovaTabela(params string[] skrivene)
        {
            var g = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false
            };
            ThemeApplier.StilTabele(g);
            GridTitles.Configure(g, false);
            var filter = new GridColumnFilters(g);
            var kolone = new HashSet<string>(skrivene, StringComparer.OrdinalIgnoreCase);
            g.DataBindingComplete += filter.Bound;
            g.DataBindingComplete += (s, e) =>
            {
                GridTitles.Apply(g);
                foreach (DataGridViewColumn c in g.Columns)
                    if (SkriveneKolone.Contains(c.Name) || kolone.Contains(c.Name) || c.ValueType == typeof(byte[]))
                        c.Visible = false;
            };
            g.ColumnHeaderMouseClick += filter.Header;
            return g;
        }

        internal static Button Dugme(string tekst, EventHandler klik)
        {
            var b = new Button { Text = tekst };
            b.Click += klik;
            return b;
        }

        // Čitanje ide van UI niti, a forma ostaje živa dok traje (pravilo 10).
        internal static async Task Ucitaj<T>(Form forma, Func<T> citaj, Action<T> prikazi)
        {
            Cursor kursor = forma.Cursor;
            try
            {
                forma.UseWaitCursor = true;
                T rezultat = await Task.Run(citaj);
                if (!forma.IsDisposed) prikazi(rezultat);
            }
            catch (Exception ex)
            {
                if (!forma.IsDisposed) Greska(forma, ex);
            }
            finally
            {
                if (!forma.IsDisposed) forma.UseWaitCursor = false;
            }
        }

        internal static bool Izvrsi(Form forma, Action radnja)
        {
            try
            {
                radnja();
                return true;
            }
            catch (Exception ex)
            {
                Greska(forma, ex);
                return false;
            }
        }

        internal static void Greska(Form forma, Exception ex)
        {
            MessageBox.Show(forma, ex.Message, forma.Text, MessageBoxButtons.OK, ex is DBConcurrencyException ? MessageBoxIcon.Warning : MessageBoxIcon.Error);
        }

        internal static void Poruka(Form forma, string tekst)
        {
            MessageBox.Show(forma, tekst, forma.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        internal static bool Potvrdi(Form forma, string tekst)
        {
            return MessageBox.Show(forma, tekst, forma.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        internal static DataRow Izabran(DataGridView g)
        {
            // U SelectionChanged posle izbora iz koda CurrentRow još pokazuje prethodni red.
            DataGridViewRow izabran = g.SelectedRows.Count == 1 ? g.SelectedRows[0] : g.CurrentRow;
            var red = izabran?.DataBoundItem as DataRowView;
            return red?.Row;
        }

        private const int NajviseCvorova = 4;

        // Deo bez trase je predlog „od stanice do stanice“: kroz čvorove u kojima ruta prelazi na drugu prugu.
        private static string TekstDela(DeoKombinacije d, RezultatRute ruta, IDictionary<int, string> nazivi)
        {
            Func<int, string> naziv = id => { string n; return nazivi.TryGetValue(id, out n) ? n : "?"; };
            string od = naziv(d.OdID), doS = naziv(d.DoID);
            if (d.Trasa != null)
                return d.Trasa.Broj + " " + od + " – " + doS + (d.Trasa.MaxMasaT.HasValue ? " (" + d.Trasa.MaxMasaT.Value.ToString("0.#") + " t)" : "") + (d.DrugihTrasa > 0 ? " +" + d.DrugihTrasa : "");
            var cvorovi = new List<string>();
            int pocetak = ruta.Deonice.FindIndex(x => x.OdID == d.OdID), kraj = ruta.Deonice.FindIndex(x => x.DoID == d.DoID);
            for (int i = pocetak; pocetak >= 0 && i < kraj; i++)
                if (ruta.Deonice[i].PrugaID != ruta.Deonice[i + 1].PrugaID) cvorovi.Add(naziv(ruta.Deonice[i].DoID));
            string preko = cvorovi.Count == 0 ? "" : " preko " + string.Join(", ", cvorovi.Take(NajviseCvorova)) + (cvorovi.Count > NajviseCvorova ? " i još " + (cvorovi.Count - NajviseCvorova) : "");
            return "BEZ TRASE " + od + " – " + doS + preko + " (" + d.Km.ToString("N1") + " km)";
        }

        internal static DataTable TabelaKombinacija(IList<KombinacijaTrasa> lista, IDictionary<int, string> nazivi)
        {
            var t = new DataTable();
            t.Columns.Add("RB", typeof(int));
            foreach (string k in new[] { "Ruta", "Kombinacija trasa", "Trasa", "Ruta km", "Bez trase km", "Zastoji", "Max masa t", "Max dužina m" }) t.Columns.Add(k);
            for (int i = 0; i < lista.Count; i++)
            {
                KombinacijaTrasa k = lista[i];
                t.Rows.Add(i, k.RutaRB.HasValue ? "Ruta " + k.RutaRB.Value : "Lanac trasa", string.Join("  →  ", k.Delovi.Select(d => TekstDela(d, k.Ruta, nazivi))), k.BrojTrasa, k.Ruta.Km.ToString("N1"), k.KmBezTrase.ToString("N1"), k.Ruta.Zastoji.Count == 0 ? "nema" : string.Join("; ", k.Ruta.Zastoji.Select(z => z.Tekst)),
                    k.NajmanjaMasa.HasValue ? k.NajmanjaMasa.Value.ToString("0.#") : "–", k.NajmanjaDuzina.HasValue ? k.NajmanjaDuzina.Value.ToString("0.#") : "–");
            }
            return t;
        }

        // Tekst trasa traži širinu i prelazi u novi red.
        internal static void StilKombinacija(DataGridView g)
        {
            g.DataBindingComplete += (s, e) =>
            {
                g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
                if (g.Columns.Contains("Kombinacija trasa"))
                {
                    DataGridViewColumn tekst = g.Columns["Kombinacija trasa"];
                    tekst.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    tekst.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                }
                g.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
            };
        }

        // Strana taba: kartica gore, tabela ispod; visina kartice prati širinu strane.
        internal static void Strana(TabPage strana, KarticaPolja kartica, Control tabela)
        {
            tabela.Dock = DockStyle.Fill;
            strana.Controls.Add(tabela);
            kartica.Dock = DockStyle.Top;
            strana.Controls.Add(kartica);
            EventHandler rasporedi = (s, e) =>
            {
                if (strana.ClientSize.Width > 0)
                    kartica.Height = kartica.Rasporedi(strana.ClientSize.Width);
            };
            strana.Resize += rasporedi;
            strana.HandleCreated += rasporedi;
        }
    }

    // Polja jednog reda: pune se iz izabranog reda tabele, a čitaju pri čuvanju.
    internal sealed class UredjivacReda
    {
        private readonly Dictionary<string, Control> polja = new Dictionary<string, Control>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, object> podrazumevano = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        internal KarticaPolja Kartica { get; }
        internal DataRow Red { get; private set; }

        internal UredjivacReda(string naslov)
        {
            Kartica = new KarticaPolja(naslov);
        }

        private static Label Oznaka(string natpis)
        {
            return new Label { Text = natpis };
        }

        internal UredjivacReda Tekst(string kolona, string natpis, bool siroko = false, int visina = 0)
        {
            var t = new TextBox { Multiline = visina > 0 };
            polja[kolona] = t;
            Kartica.Polje(Oznaka(natpis), t, siroko, visina: visina);
            return this;
        }

        internal UredjivacReda Broj(string kolona, string natpis, int decimala = 2, decimal maks = 99999999m)
        {
            var n = new NumericUpDown { DecimalPlaces = decimala, Minimum = -maks, Maximum = maks, ThousandsSeparator = false };
            polja[kolona] = n;
            Kartica.Polje(Oznaka(natpis), n);
            return this;
        }

        // Prazno polje znači NULL, zato se decimalni broj unosi kao tekst.
        internal UredjivacReda BrojPrazno(string kolona, string natpis)
        {
            var t = new TextBox();
            t.Tag = "broj";
            polja[kolona] = t;
            Kartica.Polje(Oznaka(natpis), t);
            return this;
        }

        internal UredjivacReda Datum(string kolona, string natpis, bool saVremenom = false, bool moguPrazno = false)
        {
            var d = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = saVremenom ? "dd.MM.yyyy HH:mm" : "dd.MM.yyyy",
                ShowCheckBox = moguPrazno
            };
            polja[kolona] = d;
            Kartica.Polje(Oznaka(natpis), d);
            return this;
        }

        internal UredjivacReda Izbor(string kolona, string natpis, bool siroko = false)
        {
            var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            polja[kolona] = c;
            Kartica.Polje(Oznaka(natpis), c, siroko);
            return this;
        }

        internal UredjivacReda IzborVrednosti(string kolona, string natpis, params string[] vrednosti)
        {
            Izbor(kolona, natpis);
            ((ComboBox)polja[kolona]).Items.AddRange(vrednosti.Cast<object>().ToArray());
            return this;
        }

        internal void IzvorIzbora(string kolona, DataTable izvor, string prikaz, string vrednost, bool moguPrazno)
        {
            var kopija = izvor.Copy();
            if (moguPrazno)
            {
                DataRow prazan = kopija.NewRow();
                prazan[prikaz] = "";
                kopija.Rows.InsertAt(prazan, 0);
            }
            var c = (ComboBox)polja[kolona];
            c.DisplayMember = prikaz;
            c.ValueMember = vrednost;
            c.DataSource = kopija;
        }

        internal UredjivacReda Cekiranja(params string[] koloneINatpisi)
        {
            var lista = new List<CheckBox>();
            for (int i = 0; i + 1 < koloneINatpisi.Length; i += 2)
            {
                var c = new CheckBox { Text = koloneINatpisi[i + 1] };
                polja[koloneINatpisi[i]] = c;
                lista.Add(c);
            }
            Kartica.Cekiranja(lista.ToArray());
            return this;
        }

        internal UredjivacReda Dugmad(params Control[] dugmad)
        {
            Kartica.Dugmad(dugmad);
            return this;
        }

        // Vrednost koju polje dobija kod novog reda.
        internal void Omoguci(string kolona, bool moze)
        {
            polja[kolona].Enabled = moze;
        }

        // Polje koje se samo prikazuje.
        internal UredjivacReda Prikaz(string kolona, string natpis)
        {
            Tekst(kolona, natpis);
            ((TextBox)polja[kolona]).ReadOnly = true;
            return this;
        }

        internal UredjivacReda Podrazumevano(string kolona, object vrednost)
        {
            podrazumevano[kolona] = vrednost;
            return this;
        }

        internal void Isprazni()
        {
            Red = null;
            Ocisti();
            foreach (KeyValuePair<string, object> p in podrazumevano)
            {
                var ch = polja[p.Key] as CheckBox;
                if (ch != null) ch.Checked = (bool)p.Value;
                var n = polja[p.Key] as NumericUpDown;
                if (n != null) n.Value = Convert.ToDecimal(p.Value);
                var k = polja[p.Key] as ComboBox;
                if (k != null) k.SelectedItem = p.Value;
            }
        }

        private void Ocisti()
        {
            foreach (Control c in polja.Values)
            {
                var t = c as TextBox;
                if (t != null) t.Text = "";
                var n = c as NumericUpDown;
                if (n != null) n.Value = Math.Max(n.Minimum, Math.Min(n.Maximum, 0));
                var d = c as DateTimePicker;
                if (d != null) { d.Value = DateTime.Today; if (d.ShowCheckBox) d.Checked = false; }
                var k = c as ComboBox;
                if (k != null) k.SelectedIndex = k.Items.Count > 0 ? 0 : -1;
                var ch = c as CheckBox;
                if (ch != null) ch.Checked = false;
            }
        }

        internal void Popuni(DataRow red)
        {
            Isprazni();
            Red = red;
            if (red == null) return;
            Ocisti();
            foreach (KeyValuePair<string, Control> p in polja)
            {
                if (!red.Table.Columns.Contains(p.Key)) continue;
                object v = red[p.Key];
                bool prazno = v == DBNull.Value;
                var t = p.Value as TextBox;
                if (t != null) t.Text = prazno ? "" : (v is decimal || v is double ? Convert.ToString(v, CultureInfo.CurrentCulture) : Convert.ToString(v).TrimEnd());
                var n = p.Value as NumericUpDown;
                if (n != null) n.Value = prazno ? 0 : Math.Max(n.Minimum, Math.Min(n.Maximum, Convert.ToDecimal(v)));
                var d = p.Value as DateTimePicker;
                if (d != null)
                {
                    if (!prazno) d.Value = Convert.ToDateTime(v);
                    if (d.ShowCheckBox) d.Checked = !prazno;
                }
                var k = p.Value as ComboBox;
                if (k != null)
                {
                    if (k.DataSource != null) { if (prazno) k.SelectedIndex = k.Items.Count > 0 ? 0 : -1; else k.SelectedValue = v; }
                    else k.SelectedItem = prazno ? null : Convert.ToString(v).Trim();
                }
                var ch = p.Value as CheckBox;
                if (ch != null) ch.Checked = !prazno && Convert.ToBoolean(v);
            }
        }

        internal string Tekst(string kolona)
        {
            Control c = polja[kolona];
            var k = c as ComboBox;
            if (k != null) return k.SelectedItem as string ?? "";
            string s = c.Text.Trim();
            return s;
        }

        // Prazan tekst u bazi postaje NULL.
        internal string TekstIliNull(string kolona)
        {
            string s = Tekst(kolona);
            return s.Length == 0 ? null : s;
        }

        internal decimal Decimal(string kolona)
        {
            return ((NumericUpDown)polja[kolona]).Value;
        }

        internal int CeoBroj(string kolona)
        {
            return (int)((NumericUpDown)polja[kolona]).Value;
        }

        internal decimal? DecimalIliNull(string kolona, string natpis)
        {
            string s = polja[kolona].Text.Trim();
            if (s.Length == 0) return null;
            decimal v;
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out v)
                || decimal.TryParse(s.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out v))
                return v;
            throw new FormatException("Polje „" + natpis + "“ mora biti broj.");
        }

        internal double? DoubleIliNull(string kolona, string natpis)
        {
            decimal? v = DecimalIliNull(kolona, natpis);
            return v.HasValue ? (double?)v.Value : null;
        }

        // Kontrola polja, kad forma treba sama da reaguje na izbor.
        internal Control Polje(string kolona)
        {
            return polja[kolona];
        }

        internal void IzaberiID(string kolona, int? id)
        {
            ((ComboBox)polja[kolona]).SelectedValue = id.HasValue ? (object)id.Value : DBNull.Value;
        }

        internal int? IzabranID(string kolona)
        {
            object v = ((ComboBox)polja[kolona]).SelectedValue;
            return v == null || v == DBNull.Value ? (int?)null : Convert.ToInt32(v);
        }

        internal DateTime DatumVreme(string kolona)
        {
            return ((DateTimePicker)polja[kolona]).Value;
        }

        internal DateTime? DatumIliNull(string kolona)
        {
            var d = (DateTimePicker)polja[kolona];
            return d.ShowCheckBox && !d.Checked ? (DateTime?)null : d.Value;
        }

        internal bool Cek(string kolona)
        {
            return ((CheckBox)polja[kolona]).Checked;
        }

        internal byte[] RowVer
        {
            get { return Red == null || !Red.Table.Columns.Contains("RowVer") || Red.IsNull("RowVer") ? null : (byte[])Red["RowVer"]; }
        }

        internal int? ID
        {
            get { return Red == null || !Red.Table.Columns.Contains("ID") ? (int?)null : Convert.ToInt32(Red["ID"]); }
        }
    }
}
