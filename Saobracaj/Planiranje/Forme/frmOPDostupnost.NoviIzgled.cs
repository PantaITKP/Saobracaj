using System;
using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPDostupnost
    {
        private static Control Odeljak(string naslov, Control sadrzaj)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(Theme.Dp(6)) };
            sadrzaj.Dock = DockStyle.Fill;
            p.Controls.Add(sadrzaj);
            p.Controls.Add(new Label { Text = naslov, Dock = DockStyle.Top, Height = Theme.Dp(24), Font = Theme.Tekstualni(10f, true), ForeColor = Theme.Tekst });
            return p;
        }

        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Planiranje", "Dostupnost osoblja", null);
            izgled.Mreza.MaxKolona = 1;

            cboFilter.Items.AddRange(new object[] { "Svi", "Mašinovođe", "Pregledači" });
            cboFilter.SelectedIndex = 0;
            cboFilter.SelectedIndexChanged += (s, e) => PrikaziKalendar();
            dtNedelja.ValueChanged += (s, e) => { var _ = Osvezi(); };

            var glava = new KarticaPolja("Nedelja");
            glava.Polje(new Label { Text = "Nedelja" }, dtNedelja);
            glava.Polje(new Label { Text = "Prikaz" }, cboFilter);
            glava.Tekst(new Label { Text = "Legenda: zeleno dnevna · plavo noćna · sivo slobodan · žuto odsustvo · roze posao. Dvoklik na ćeliju menja smenu ručno." });
            glava.Dugmad(
                EkranOP.Dugme("Generiši smene iz turnusa", (s, e) => { var _ = Generisi(); }),
                EkranOP.Dugme("Dodaj odsustvo", (s, e) => DodajOdsustvo()),
                EkranOP.Dugme("Izmeni turnus", (s, e) =>
                {
                    var f = new frmOPTurnusi();
                    if (f.Pravo) f.Show(this); else f.Dispose();
                }),
                EkranOP.Dugme("Odsustva", (s, e) => PrikaziOdsustva()),
                EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }));
            izgled.Mreza.Dodaj(glava);

            tabKalendar.CellFormatting += ObojiCeliju;
            tabKalendar.DataBindingComplete += (s, e) => tabKalendar.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tabKalendar.CellDoubleClick += (s, e) => IzmeniSmenu(e.RowIndex, e.ColumnIndex);
            tabKalendar.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0) PrikaziOsobu(Convert.ToInt32(tabKalendar.Rows[e.RowIndex].Cells["DeSifra"].Value));
            };

            lblOsoba.Font = Theme.Tekstualni(9f);
            lblZauzetost.Font = Theme.Tekstualni(9f);
            lblOsoba.ForeColor = Theme.Tekst;
            lblZauzetost.ForeColor = Theme.Tekst;
            var desno = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            desno.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            desno.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            desno.Controls.Add(Odeljak("Osoba", lblOsoba), 0, 0);
            desno.Controls.Add(Odeljak("Zauzetost ove nedelje", lblZauzetost), 0, 1);

            var kolone = new TableLayoutPanel { ColumnCount = 2 };
            kolone.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
            kolone.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            kolone.Controls.Add(Odeljak("Kalendar", tabKalendar), 0, 0);
            kolone.Controls.Add(desno, 1, 0);

            Load += (s, e) => { var _ = Osvezi(); };
            izgled.Zavrsi(kolone, 1500, 900);
        }
    }
}
