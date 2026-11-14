using System;
using System.Windows.Forms;
using Saobracaj.Tema;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmTmpJobs
    {
        private const int VisinaDetalja = 270;

        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Priprema / Porudžbine", "CIM poslovi (test)", null,
                () => prikazanPosao.HasValue ? "#" + prikazanPosao.Value : "");

            filter.Datum("Od", "Kreirano od")
                .Datum("Do", "Kreirano do")
                .Tekst("Trazi", "Tekst (referenca, platilac, stanica)", siroko: true)
                .Cekiranja("SamoPrazni", "Samo poslovi sa praznim partnerom ili stanicom")
                .Dugmad(
                    EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }),
                    EkranOP.Dugme("Otvori teretnicu", (s, e) => OtvoriTeretnicu()));
            filter.Kartica.Tekst(lblBroj);
            ((DateTimePicker)filter.Polje("Od")).Value = DateTime.Today.AddDays(-7);
            ((DateTimePicker)filter.Polje("Do")).Value = DateTime.Today;
            ((TextBox)filter.Polje("Trazi")).KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                var _ = Osvezi();
            };
            ((CheckBox)filter.Polje("SamoPrazni")).CheckedChanged += (s, e) => { var _ = Osvezi(); };
            izgled.Mreza.MaxKolona = 1;
            izgled.Mreza.Dodaj(filter.Kartica);

            tabela.CellFormatting += Oboji;
            tabela.SelectionChanged += (s, e) => { if (tabela.SelectedRows.Count == 1) { PrikaziDetalje(IzabranPosao); izgled.OsveziZaglavlje(); } };
            tabela.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OtvoriTeretnicu(); };
            tabela.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn k in tabela.Columns)
                {
                    if (k.ValueType == typeof(decimal)) k.DefaultCellStyle.Format = "N3";
                    if (k.ValueType == typeof(DateTime)) k.DefaultCellStyle.Format = "dd.MM.yyyy HH:mm";
                    // ID šifre stoji odmah posle naziva, uži i siv
                    if (k.Name.EndsWith("ID", StringComparison.Ordinal) || k.Name == "NHM1" || k.Name == "NHM2")
                        k.DefaultCellStyle.ForeColor = System.Drawing.Color.DimGray;
                }
                if (tabela.Columns.Contains("Dužina 1 (m)")) tabela.Columns["Dužina 1 (m)"].DefaultCellStyle.Format = "N2";
            };
            foreach (DataGridView g in new[] { tabDokumenti, tabPoreklo, tabIstorija })
            {
                g.Dock = DockStyle.Fill;
                var gg = g;
                gg.DataBindingComplete += (s, e) =>
                {
                    gg.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
                    foreach (DataGridViewColumn k in gg.Columns)
                        if (k.ValueType == typeof(DateTime)) k.DefaultCellStyle.Format = "dd.MM.yyyy HH:mm";
                };
            }
            tabPoreklo.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            tabIstorija.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            var detalji = new TabControl { Dock = DockStyle.Bottom, Height = Theme.Dp(VisinaDetalja) };
            var strNapomena = new TabPage("Napomena (tekst maila)");
            var strDokumenti = new TabPage("Dokumenti");
            var strPoreklo = new TabPage("Poreklo polja");
            var strIstorija = new TabPage("Istorija");
            txtNapomena.Dock = DockStyle.Fill;
            txtNapomena.Font = Theme.Tekstualni(9f);
            strNapomena.Controls.Add(txtNapomena);
            strDokumenti.Controls.Add(tabDokumenti);
            strPoreklo.Controls.Add(tabPoreklo);
            strIstorija.Controls.Add(tabIstorija);
            detalji.TabPages.Add(strNapomena);
            detalji.TabPages.Add(strDokumenti);
            detalji.TabPages.Add(strPoreklo);
            detalji.TabPages.Add(strIstorija);
            ThemeApplier.Apply(detalji);
            ThemeApplier.TabeviNovogIzgleda(detalji);

            tabela.Dock = DockStyle.Fill;
            var okvir = new Panel();
            okvir.Controls.Add(tabela);
            okvir.Controls.Add(new Splitter { Dock = DockStyle.Bottom, Height = Theme.Dp(6), BackColor = Theme.Pozadina2 });
            okvir.Controls.Add(detalji);

            Load += (s, e) => { var _ = Osvezi(); };
            izgled.Zavrsi(okvir, 1500, 900);
        }
    }
}
