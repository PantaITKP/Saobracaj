using System;
using System.Linq;
using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPOsoblje
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Šifarnici planiranja", "Osoblje i kvalifikacije", null);

            // Uloge se menjaju u tabeli i upisuju u Delavci; ime i aktivnost se samo prikazuju.
            poljaOsoba.Izbor("MaticnaStanicaID", "Matična stanica")
                .Tekst("Napomena", "Napomena")
                .Cekiranja("UPlaniranju", "Uključi u plan", "Auto", "AUTO", "SamoDomicilna", "Samo domicilna")
                .Dugmad(
                    EkranOP.Dugme("Sačuvaj", (s, e) => SacuvajOsobe()),
                    EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }));
            tabOsoblje.SelectionChanged += (s, e) => { if (tabOsoblje.SelectedRows.Count > 0) { var _ = PrikaziOsobu(); } };
            tabOsoblje.DataBindingComplete += (s, e) =>
            {
                tabOsoblje.ReadOnly = false;
                foreach (DataGridViewColumn c in tabOsoblje.Columns)
                    c.ReadOnly = !Uloge.Contains(c.Name);
                if (tabOsoblje.Columns.Contains("UPlaniranju")) tabOsoblje.Columns["UPlaniranju"].HeaderText = "Uključi u plan";
            };
            // Čekiranje mora odmah da uđe u izvor podataka, da bi Sačuvaj video izmenu.
            tabOsoblje.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (tabOsoblje.IsCurrentCellDirty) tabOsoblje.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            poljaTurnus.Izbor("TurnusID", "Turnus")
                .Datum("Od", "Prvi radni dan")
                .Datum("Do", "Važi do", moguPrazno: true)
                .Dugmad(EkranOP.Dugme("Dodeli", (s, e) => DodeliTurnus()));

            poljaOdsustvo.Izbor("DeSifra", "Zaposleni")
                .Izbor("RazlogID", "Razlog")
                .Izbor("OdobrioDeSifra", "Odobrio")
                .Datum("Od", "Od")
                .Datum("Do", "Do")
                .Tekst("Napomena", "Napomena")
                .Dugmad(
                    EkranOP.Dugme("Dodaj odsustvo", (s, e) => DodajOdsustvo()),
                    EkranOP.Dugme("Osveži", (s, e) => { var _ = OsveziOdsustva(); }));
            poljaOdsustvo.Kartica.Polje(new Label { Text = "Pregled od" }, dtPregledOd);
            poljaOdsustvo.Kartica.Polje(new Label { Text = "Pregled do" }, dtPregledDo);
            poljaOdsustvo.Kartica.Cekiranja(cekMasinovodje, cekPregledaci, cekVozovodje, cekManevristi);
            EventHandler filter = (s, e) => { var _ = OsveziOdsustva(); };
            foreach (CheckBox c in new[] { cekMasinovodje, cekPregledaci, cekVozovodje, cekManevristi }) c.CheckedChanged += filter;
            dtPregledOd.ValueChanged += filter;
            dtPregledDo.ValueChanged += filter;

            // Kvalifikacije: dve matrice i zajedničko čuvanje.
            var kartKval = new KarticaPolja("Poznavanje pruga i lokomotiva");
            kartKval.Tekst(new Label { Text = "Prikazani su aktivni radnici. Kvalifikacija bez roka je trajna, a istekla se ne računa pri planiranju." });
            kartKval.Dugmad(
                EkranOP.Dugme("Sačuvaj", (s, e) => { var _ = SacuvajMatrice(); }),
                EkranOP.Dugme("Osveži", (s, e) => { var _ = OsveziMatrice(); }));
            kartKval.Dock = DockStyle.Top;
            var unutra = new TabControl { Dock = DockStyle.Fill };
            var sPruge = new TabPage("Poznavanje pruga");
            var sLok = new TabPage("Poznavanje lokomotiva");
            matricaPruga.Dock = DockStyle.Fill;
            matricaLokomotiva.Dock = DockStyle.Fill;
            sPruge.Controls.Add(matricaPruga);
            sLok.Controls.Add(matricaLokomotiva);
            unutra.TabPages.AddRange(new[] { sPruge, sLok });
            ThemeApplier.TabeviNovogIzgleda(unutra);

            var tabovi = new TabControl();
            var s0 = new TabPage("Osobe");
            var s1 = new TabPage("Kvalifikacije");
            var s2 = new TabPage("Turnus");
            var s3 = new TabPage("Odsustva");
            EkranOP.Strana(s0, poljaOsoba.Kartica, tabOsoblje);
            s1.Controls.Add(unutra);
            s1.Controls.Add(kartKval);
            s1.Resize += (s, e) => { if (s1.ClientSize.Width > 0) kartKval.Height = kartKval.Rasporedi(s1.ClientSize.Width); };
            EkranOP.Strana(s2, poljaTurnus.Kartica, tabTurnusi);
            EkranOP.Strana(s3, poljaOdsustvo.Kartica, tabOdsustva);
            tabovi.TabPages.AddRange(new[] { s0, s1, s2, s3 });

            FormClosing += (s, e) =>
            {
                if (ImaNesacuvano() && !EkranOP.Potvrdi(this, "Imate nesačuvane izmene. Zatvoriti formu bez čuvanja?")) e.Cancel = true;
            };
            Load += (s, e) => { var _ = Osvezi(); };
            izgled.Zavrsi(tabovi, 1300, 820);
        }
    }
}
