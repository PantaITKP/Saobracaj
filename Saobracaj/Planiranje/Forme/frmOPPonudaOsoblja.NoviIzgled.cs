using System;
using System.Windows.Forms;
using Saobracaj.Planiranje.Pravila;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPPonudaOsoblja
    {
        private static Control Odeljak(Control naslov, Control sadrzaj)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(Theme.Dp(6)) };
            sadrzaj.Dock = DockStyle.Fill;
            p.Controls.Add(sadrzaj);
            naslov.Dock = DockStyle.Top;
            naslov.Height = Theme.Dp(24);
            naslov.Font = Theme.Tekstualni(10f, true);
            naslov.ForeColor = Theme.Tekst;
            p.Controls.Add(naslov);
            return p;
        }

        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Planiranje", "Ponuda osoblja", null);
            izgled.Mreza.MaxKolona = 1;

            cboPosao.SelectedIndexChanged += (s, e) =>
            {
                if (punjenje || !(cboPosao.SelectedValue is int)) return;
                PosaoId = (int)cboPosao.SelectedValue;
                var _ = Osvezi();
            };
            var glava = new KarticaPolja("Posao");
            glava.Polje(new Label { Text = "Najava" }, cboPosao, siroko: true);
            glava.Tekst(lblZaglavlje);
            izgled.Mreza.Dodaj(glava);

            tabMasinovodje.CellClick += (s, e) => IzaberiRed(tabMasinovodje, masinovodje, e.RowIndex, PonudaOsoblja.UlogaMasinovodja);
            tabPrijem.CellClick += (s, e) => IzaberiRed(tabPrijem, zaPrijem, e.RowIndex, PonudaOsoblja.UlogaPrijem);
            tabPredaja.CellClick += (s, e) => IzaberiRed(tabPredaja, zaPredaju, e.RowIndex, PonudaOsoblja.UlogaPredaja);
            tabOdgovori.CellContentClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                string kolona = tabOdgovori.Columns[e.ColumnIndex].Name;
                if (kolona != "Prihvatio" && kolona != "Odbio") return;
                var _ = Odgovori(Convert.ToInt32(tabOdgovori.Rows[e.RowIndex].Cells["ID"].Value), kolona == "Prihvatio");
            };
            foreach (DataGridView g in new[] { tabMasinovodje, tabPrijem, tabPredaja, tabOdgovori })
            {
                DataGridView tabela = g;
                tabela.DataBindingComplete += (s, e) => tabela.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }

            var levo = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
            levo.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            levo.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            levo.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            levo.Controls.Add(Odeljak(new Label { Text = "Mašinovođa" }, tabMasinovodje), 0, 0);
            levo.Controls.Add(Odeljak(lblNaslovPrijem, tabPrijem), 0, 1);
            levo.Controls.Add(Odeljak(lblNaslovPredaja, tabPredaja), 0, 2);

            var slanje = new KarticaPolja("Zadatak za slanje");
            slanje.Tekst(lblIzabrani);
            slanje.Polje(new Label { Text = "Napomena za mašinovođu" }, txtNapomena, siroko: true, visina: Theme.Dp(60));
            slanje.Dugmad(EkranOP.Dugme("Pošalji zadatak", (s, e) => { var _ = Posalji(); }), EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }));
            slanje.Dock = DockStyle.Top;

            var desno = new Panel { Dock = DockStyle.Fill };
            desno.Controls.Add(Odeljak(new Label { Text = "Odgovori osoblja" }, tabOdgovori));
            desno.Controls.Add(slanje);
            desno.Resize += (s, e) => { if (desno.ClientSize.Width > 0) slanje.Height = slanje.Rasporedi(desno.ClientSize.Width); };

            var kolone = new TableLayoutPanel { ColumnCount = 2 };
            kolone.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
            kolone.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            kolone.Controls.Add(levo, 0, 0);
            kolone.Controls.Add(desno, 1, 0);

            Load += (s, e) => { var _ = UcitajPoslove(); };
            izgled.Zavrsi(kolone, 1500, 900);
        }
    }
}
