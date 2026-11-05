using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Saobracaj.Planiranje.Forme;
using Saobracaj.Planiranje.Pravila;
using Saobracaj.Tema;

namespace Saobracaj.Planiranje.Kontrole
{
    internal sealed class RadnikMatrice
    {
        internal int DeSifra;
        internal string Naziv;
        internal bool Masinovodja;
        internal bool Manevrista;
    }

    internal sealed class RedMatrice
    {
        internal int ID;
        internal string Naziv;
    }

    internal sealed class StavkaMatrice
    {
        internal int DeSifra;
        internal int RedID;
        internal DateTime? VaziDo;
        internal string Napomena;
    }

    internal sealed class IzmeneMatrice
    {
        internal readonly List<StavkaMatrice> Upisi = new List<StavkaMatrice>();
        internal readonly List<StavkaMatrice> Brisi = new List<StavkaMatrice>();
        internal bool Ima => Upisi.Count > 0 || Brisi.Count > 0;
    }

    // Radnici u kolonama, pruge ili lokomotive u redovima. Red u tabeli postoji kad je ćelija čekirana.
    internal sealed class MatricaPanel : Panel
    {
        private const int DanaDoIsteka = 30;
        private static readonly Color BojaIstekao = ColorTranslator.FromHtml("#F3B8C0");
        private static readonly Color BojaIstice = ColorTranslator.FromHtml("#F7E8C4");

        private readonly DataGridView grid = new DataGridView();
        private readonly TextBox txtRadnik = new TextBox();
        private readonly TextBox txtRed = new TextBox();
        private readonly ComboBox cboUloga = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ContextMenuStrip meni = new ContextMenuStrip();
        private readonly Timer klikTajmer = new Timer();
        private readonly ToolStripMenuItem stavkaRok, stavkaTrajno, stavkaRedDa, stavkaRedNe, stavkaKolonaDa, stavkaKolonaNe;
        private readonly bool saUlogama;

        private Dictionary<string, StavkaMatrice> pocetno = new Dictionary<string, StavkaMatrice>();
        private Dictionary<string, StavkaMatrice> stanje = new Dictionary<string, StavkaMatrice>();
        private int klikKolona = -1, klikRed = -1;

        internal MatricaPanel(string naslovReda, bool filterUloga)
        {
            saUlogama = filterUloga;
            klikTajmer.Interval = SystemInformation.DoubleClickTime;
            klikTajmer.Tick += (s, e) =>
            {
                klikTajmer.Stop();
                PromeniCeliju(klikKolona, klikRed);
            };

            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            ThemeApplier.StilTabele(grid);
            PrimeniIzgledTabele();
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Naziv", HeaderText = naslovReda, Width = Theme.Dp(300), Frozen = true, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.CellFormatting += Formatiraj;
            grid.CellToolTipTextNeeded += Napomena;
            grid.CellMouseClick += Klik;
            grid.CellMouseDoubleClick += Dvoklik;
            grid.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Space || grid.CurrentCell == null) return;
                PromeniCeliju(grid.CurrentCell.ColumnIndex, grid.CurrentCell.RowIndex);
                e.Handled = true;
            };

            stavkaRok = (ToolStripMenuItem)meni.Items.Add("Važi do...", null, (s, e) => IzmeniRok(klikKolona, klikRed));
            stavkaTrajno = (ToolStripMenuItem)meni.Items.Add("Trajno", null, (s, e) => PostaviRok(klikKolona, klikRed, null));
            meni.Items.Add(new ToolStripSeparator());
            stavkaRedDa = (ToolStripMenuItem)meni.Items.Add("Čekiraj ceo red", null, (s, e) => CekirajRed(klikRed, true));
            stavkaRedNe = (ToolStripMenuItem)meni.Items.Add("Odčekiraj ceo red", null, (s, e) => CekirajRed(klikRed, false));
            stavkaKolonaDa = (ToolStripMenuItem)meni.Items.Add("Čekiraj celu kolonu", null, (s, e) => CekirajKolonu(klikKolona, true));
            stavkaKolonaNe = (ToolStripMenuItem)meni.Items.Add("Odčekiraj celu kolonu", null, (s, e) => CekirajKolonu(klikKolona, false));

            var gore = new FlowLayoutPanel { Dock = DockStyle.Top, Height = Theme.Dp(36), Padding = new Padding(Theme.Dp(4)), WrapContents = false };
            gore.Controls.Add(Natpis("Radnik"));
            txtRadnik.Width = Theme.Dp(150);
            gore.Controls.Add(txtRadnik);
            gore.Controls.Add(Natpis(naslovReda));
            txtRed.Width = Theme.Dp(170);
            gore.Controls.Add(txtRed);
            if (saUlogama)
            {
                cboUloga.Items.AddRange(new object[] { "Svi", "Mašinovođe", "Manevristi" });
                cboUloga.SelectedIndex = 0;
                cboUloga.Width = Theme.Dp(110);
                cboUloga.SelectedIndexChanged += (s, e) => PrimeniFilter();
                gore.Controls.Add(Natpis("Uloga"));
                gore.Controls.Add(cboUloga);
            }
            gore.Controls.Add(Natpis("Klik čekira · dvoklik ili desni klik: rok i ceo red/kolona · žuto: ističe za 30 dana · crveno: istekao"));
            txtRadnik.TextChanged += (s, e) => PrimeniFilter();
            txtRed.TextChanged += (s, e) => PrimeniFilter();

            Controls.Add(grid);
            Controls.Add(gore);
        }

        // Tema forme stilizuje sve tabele posle konstruktora, pa se ovo ponavlja pri svakom punjenju.
        private void PrimeniIzgledTabele()
        {
            grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            grid.GridColor = Theme.Linija;
            grid.ColumnHeadersHeight = Theme.Dp(64);
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        private static Label Natpis(string tekst)
        {
            return new Label { Text = tekst, AutoSize = true, Margin = new Padding(Theme.Dp(8), Theme.Dp(6), Theme.Dp(4), 0), ForeColor = Theme.TekstSporedni, Font = Theme.Tekstualni(8.5f) };
        }

        private static string Kljuc(int deSifra, int redId)
        {
            return deSifra + "|" + redId;
        }

        private static string Skraceno(RadnikMatrice r)
        {
            return r.Masinovodja && r.Manevrista ? "M, MAN" : r.Manevrista ? "MAN" : "M";
        }

        internal bool ImaIzmena => Izmene().Ima;

        internal void Postavi(IEnumerable<RadnikMatrice> radnici, IEnumerable<RedMatrice> redovi, IEnumerable<StavkaMatrice> stavke)
        {
            klikTajmer.Stop();
            PrimeniIzgledTabele();
            grid.SuspendLayout();
            grid.CurrentCell = null;
            grid.Rows.Clear();
            while (grid.Columns.Count > 1) grid.Columns.RemoveAt(1);
            foreach (RadnikMatrice r in radnici)
            {
                grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = r.Naziv + (saUlogama ? "\n" + Skraceno(r) : ""),
                    Width = Theme.Dp(92),
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    Tag = r,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
                });
            }
            foreach (RedMatrice r in redovi)
            {
                int i = grid.Rows.Add();
                grid.Rows[i].Tag = r;
                grid.Rows[i].Cells[0].Value = r.Naziv;
            }
            pocetno = stavke.ToDictionary(s => Kljuc(s.DeSifra, s.RedID));
            stanje = pocetno.ToDictionary(p => p.Key, p => new StavkaMatrice { DeSifra = p.Value.DeSifra, RedID = p.Value.RedID, VaziDo = p.Value.VaziDo, Napomena = p.Value.Napomena });
            grid.ResumeLayout();
            PrimeniFilter();
        }

        private void PrimeniFilter()
        {
            grid.CurrentCell = null;
            string radnik = NaziviStanica.Normalizuj(txtRadnik.Text), red = NaziviStanica.Normalizuj(txtRed.Text);
            for (int k = 1; k < grid.Columns.Count; k++)
            {
                var r = (RadnikMatrice)grid.Columns[k].Tag;
                bool uloga = !saUlogama || cboUloga.SelectedIndex <= 0 || (cboUloga.SelectedIndex == 1 ? r.Masinovodja : r.Manevrista);
                grid.Columns[k].Visible = uloga && (radnik.Length == 0 || NaziviStanica.Normalizuj(r.Naziv).Contains(radnik));
            }
            foreach (DataGridViewRow v in grid.Rows)
                v.Visible = red.Length == 0 || NaziviStanica.Normalizuj(((RedMatrice)v.Tag).Naziv).Contains(red);
        }

        private StavkaMatrice Stavka(int kolona, int red)
        {
            if (kolona < 1 || red < 0 || kolona >= grid.Columns.Count || red >= grid.Rows.Count) return null;
            StavkaMatrice s;
            return stanje.TryGetValue(Kljuc(((RadnikMatrice)grid.Columns[kolona].Tag).DeSifra, ((RedMatrice)grid.Rows[red].Tag).ID), out s) ? s : null;
        }

        private void Formatiraj(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex < 1 || e.RowIndex < 0) return;
            StavkaMatrice s = Stavka(e.ColumnIndex, e.RowIndex);
            e.Value = s == null ? "" : s.VaziDo.HasValue ? "✓ " + s.VaziDo.Value.ToString("dd.MM.yy") : "✓";
            e.FormattingApplied = true;
            if (s?.VaziDo == null) return;
            if (s.VaziDo.Value.Date < DateTime.Today) e.CellStyle.BackColor = e.CellStyle.SelectionBackColor = BojaIstekao;
            else if (s.VaziDo.Value.Date <= DateTime.Today.AddDays(DanaDoIsteka)) e.CellStyle.BackColor = e.CellStyle.SelectionBackColor = BojaIstice;
            else return;
            e.CellStyle.SelectionForeColor = Theme.Tekst;
        }

        private void Napomena(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            StavkaMatrice s = Stavka(e.ColumnIndex, e.RowIndex);
            if (s != null) e.ToolTipText = s.VaziDo.HasValue ? "Važi do " + s.VaziDo.Value.ToString("dd.MM.yyyy") : "Trajno";
        }

        private void Klik(object sender, DataGridViewCellMouseEventArgs e)
        {
            klikKolona = e.ColumnIndex;
            klikRed = e.RowIndex;
            if (e.Button == MouseButtons.Right)
            {
                klikTajmer.Stop();
                bool imaKolonu = e.ColumnIndex >= 1, imaRed = e.RowIndex >= 0;
                if (!imaKolonu && !imaRed) return;
                if (imaKolonu && imaRed) grid.CurrentCell = grid[e.ColumnIndex, e.RowIndex];
                bool cekirana = Stavka(e.ColumnIndex, e.RowIndex) != null;
                stavkaRok.Enabled = stavkaTrajno.Enabled = cekirana;
                stavkaRedDa.Enabled = stavkaRedNe.Enabled = imaRed;
                stavkaKolonaDa.Enabled = stavkaKolonaNe.Enabled = imaKolonu;
                meni.Show(Cursor.Position);
                return;
            }
            // Čeka se da se vidi da li je dvoklik, jer on na čekiranoj ćeliji menja rok, a ne odčekira je.
            if (e.Button == MouseButtons.Left && e.ColumnIndex >= 1 && e.RowIndex >= 0)
            {
                klikTajmer.Stop();
                klikTajmer.Start();
            }
        }

        private void Dvoklik(object sender, DataGridViewCellMouseEventArgs e)
        {
            klikTajmer.Stop();
            if (e.Button != MouseButtons.Left || e.ColumnIndex < 1 || e.RowIndex < 0) return;
            if (Stavka(e.ColumnIndex, e.RowIndex) == null) PromeniCeliju(e.ColumnIndex, e.RowIndex);
            else IzmeniRok(e.ColumnIndex, e.RowIndex);
        }

        internal void PromeniCeliju(int kolona, int red)
        {
            if (kolona < 1 || red < 0) return;
            PostaviCeliju(kolona, red, Stavka(kolona, red) == null);
            grid.InvalidateCell(kolona, red);
        }

        private void PostaviCeliju(int kolona, int red, bool cekirana)
        {
            int d = ((RadnikMatrice)grid.Columns[kolona].Tag).DeSifra, r = ((RedMatrice)grid.Rows[red].Tag).ID;
            string k = Kljuc(d, r);
            if (!cekirana) { stanje.Remove(k); return; }
            if (stanje.ContainsKey(k)) return;
            // Vraćanje čekiranja u istom prolazu zadržava raniji rok i napomenu.
            StavkaMatrice p;
            stanje[k] = pocetno.TryGetValue(k, out p)
                ? new StavkaMatrice { DeSifra = d, RedID = r, VaziDo = p.VaziDo, Napomena = p.Napomena }
                : new StavkaMatrice { DeSifra = d, RedID = r };
        }

        internal void CekirajRed(int red, bool cekirana)
        {
            if (red < 0) return;
            for (int k = 1; k < grid.Columns.Count; k++)
                if (grid.Columns[k].Visible) PostaviCeliju(k, red, cekirana);
            grid.Invalidate();
        }

        internal void CekirajKolonu(int kolona, bool cekirana)
        {
            if (kolona < 1) return;
            for (int r = 0; r < grid.Rows.Count; r++)
                if (grid.Rows[r].Visible) PostaviCeliju(kolona, r, cekirana);
            grid.Invalidate();
        }

        internal void PostaviRok(int kolona, int red, DateTime? vaziDo)
        {
            StavkaMatrice s = Stavka(kolona, red);
            if (s == null) return;
            s.VaziDo = vaziDo?.Date;
            grid.InvalidateCell(kolona, red);
        }

        private void IzmeniRok(int kolona, int red)
        {
            StavkaMatrice s = Stavka(kolona, red);
            if (s == null) return;
            using (var d = new MaliDijalog("Rok važenja"))
            {
                ComboBox vrsta = d.Izbor("Važi", "Do datuma", "Trajno");
                DateTimePicker datum = d.Datum("Datum", s.VaziDo ?? DateTime.Today);
                if (!s.VaziDo.HasValue) vrsta.SelectedIndex = 1;
                datum.Enabled = vrsta.SelectedIndex == 0;
                vrsta.SelectedIndexChanged += (o, e) => datum.Enabled = vrsta.SelectedIndex == 0;
                if (!d.Prikazi(FindForm())) return;
                PostaviRok(kolona, red, vrsta.SelectedIndex == 0 ? datum.Value.Date : (DateTime?)null);
            }
        }

        internal IzmeneMatrice Izmene()
        {
            var iz = new IzmeneMatrice();
            foreach (KeyValuePair<string, StavkaMatrice> p in stanje)
            {
                StavkaMatrice s;
                if (!pocetno.TryGetValue(p.Key, out s) || s.VaziDo != p.Value.VaziDo) iz.Upisi.Add(p.Value);
            }
            foreach (KeyValuePair<string, StavkaMatrice> p in pocetno)
                if (!stanje.ContainsKey(p.Key)) iz.Brisi.Add(p.Value);
            return iz;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                klikTajmer.Dispose();
                meni.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
