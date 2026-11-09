using System;
using System.Linq;
using System.Windows.Forms;
using Saobracaj.Tema;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPStanice
    {
        private const int SirinaPoljaRute = Kontrole.KontrolaMape.SirinaKolone;
        private const int VisinaKombinacija = 190;
        private const int VisinaDeonicaPruge = 240;

        // Naziv dobija slobodnu širinu, a kilometri isti broj decimala.
        private static void StilPruga(DataGridView g, string siroka)
        {
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            foreach (DataGridViewColumn k in g.Columns)
                if (k.ValueType == typeof(decimal)) k.DefaultCellStyle.Format = "N1";
            if (g.Columns.Contains(siroka)) g.Columns[siroka].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Šifarnici planiranja", "Mreža i stanice", null);
            tabela.MultiSelect = true;
            tabela.SelectionChanged += (s, e) => { if (tabela.SelectedRows.Count == 1) PopuniIzabrani(); };

            polja.Tekst("Naziv", "Naziv")
                .Tekst("SifraIZS", "Šifra IŽS")
                .Prikaz("Kod", "Kod (računa se iz šifre)")
                .Tekst("UIC", "UIC")
                .Tekst("Drzava", "Država")
                .Tekst("VrstaMesta", "Vrsta mesta")
                .BrojPrazno("Latitude", "Geografska širina (Latitude, ≈ 44)")
                .BrojPrazno("Longitude", "Geografska dužina (Longitude, ≈ 20)")
                .Prikaz("KoordIzvor", "Izvor koordinata")
                .Cekiranja("Granicna", "Granična", "Aktivna", "Aktivna")
                .Podrazumevano("Aktivna", true)
                .Dugmad(
                    EkranOP.Dugme("Nova", (s, e) => Novi()),
                    EkranOP.Dugme("Sačuvaj", (s, e) => Sacuvaj()),
                    EkranOP.Dugme("Spoji duplikate", (s, e) => SpojiDuplikate()),
                    EkranOP.Dugme("Uvezi Registar", (s, e) => UveziRegistar()),
                    EkranOP.Dugme("Dopuni iz starog šifarnika", (s, e) => Dopuni()),
                    EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }));
            polja.Kartica.Cekiranja(chkSamoSrb, chkSamoStanice, chkZaProveru);
            EventHandler filter = (s, e) => PrimeniFiltere();
            chkSamoSrb.CheckedChanged += filter;
            chkSamoStanice.CheckedChanged += filter;
            chkZaProveru.CheckedChanged += filter;
            izgled.Mreza.MaxKolona = 1;
            izgled.Mreza.Dodaj(polja.Kartica);

            poljaRegistar.Prikaz("NazivRegistar", "Naziv u Registru")
                .Prikaz("NazivStari", "Stari naziv")
                .Prikaz("VrstaKod", "Vrsta (kod)")
                .Prikaz("Pruge", "Pruge")
                .Prikaz("MaxDuzinaVozaAB", "Max dužina voza A→B (m)")
                .Prikaz("MaxDuzinaVozaBA", "Max dužina voza B→A (m)")
                .Prikaz("Posednutost", "Posednutost")
                .Prikaz("OtvorenaZa", "Otvorena za P/T")
                .Prikaz("Vaga", "Vaga")
                .Prikaz("Rampa", "Rampa");

            poljaRuta.Izbor("OdID", "Od")
                .Izbor("DoID", "Do")
                .IzborVrednosti("Vuca", "Vuča", "Dizel", "Električna")
                .IzborVrednosti("Osovinsko", "Osovinsko opterećenje", "Bez provere", "16 t (A)", "18 t (B)", "20 t (C)", "22.5 t (D)")
                .BrojPrazno("BrutoT", "Bruto voza (t)")
                .BrojPrazno("DuzinaM", "Dužina voza (m)")
                .Datum("Datum", "Datum (zatvori iz telegrama)")
                .Cekiranja("SaKiM", "Pruge na KiM")
                .Podrazumevano("Vuca", "Dizel")
                .Podrazumevano("Osovinsko", "20 t (C)")
                .Dugmad(EkranOP.Dugme("Izračunaj rutu", (s, e) => { var _ = IzracunajRutu(); }));
            poljaRuta.Isprazni();

            var tabovi = new TabControl();
            var strTabela = new TabPage("Stanice");
            var strRegistar = new TabPage("Registar");
            var strMapa = new TabPage("Mapa");
            var strPruge = new TabPage("Pruge");
            tabela.Dock = DockStyle.Fill;
            mapa.Dock = DockStyle.Fill;
            strTabela.Controls.Add(tabela);
            strRegistar.AutoScroll = true;
            poljaRegistar.Kartica.Dock = DockStyle.Top;
            strRegistar.Controls.Add(poljaRegistar.Kartica);
            strRegistar.Resize += (s, e) =>
            {
                int sirina = strRegistar.ClientSize.Width;
                if (sirina > 0) poljaRegistar.Kartica.Height = poljaRegistar.Kartica.Rasporedi(sirina);
            };

            // Mapa dobija ceo ekran: polja rute su u koloni levo, a rezultat ispod njih.
            var levo = new Panel { Dock = DockStyle.Left, Width = SirinaPoljaRute, AutoScroll = true };
            lblRuta.Dock = DockStyle.Fill;
            levo.Controls.Add(lblRuta);
            poljaRuta.Kartica.Dock = DockStyle.Top;
            levo.Controls.Add(poljaRuta.Kartica);
            poljaRuta.Kartica.Height = poljaRuta.Kartica.Rasporedi(SirinaPoljaRute - SystemInformation.VerticalScrollBarWidth);
            // Slojevi mape su u traci na vrhu same mape.
            mapa.StanicaPomerena += PomeriStanicu;
            mapa.StanicaIzabrana += IzaberiNaMapi;
            mapa.VezaKliknuta += ZameniZatvor;
            // Kombinacije trasa su ispod mape, jer tekst trasa traži širinu.
            tabKombinacije.Dock = DockStyle.Bottom;
            tabKombinacije.Height = VisinaKombinacija;
            tabKombinacije.SelectionChanged += (s, e) => CrtajKombinaciju();
            EkranOP.StilKombinacija(tabKombinacije);
            strMapa.Controls.Add(mapa);
            strMapa.Controls.Add(tabKombinacije);
            strMapa.Controls.Add(levo);
            tabovi.TabPages.Add(strTabela);
            tabovi.TabPages.Add(strRegistar);
            tabovi.TabPages.Add(strMapa);
            tabovi.TabPages.Add(strPruge);

            var vrhPruga = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
            vrhPruga.Controls.Add(chkSamoKoriscene);
            vrhPruga.Controls.Add(lblPruge);
            chkSamoKoriscene.CheckedChanged += (s, e) => PopuniPruge();
            tabPruge.Dock = DockStyle.Fill;
            tabPruge.SelectionChanged += (s, e) => PrikaziDeonicePruge();
            tabDeonicePruge.Dock = DockStyle.Bottom;
            tabDeonicePruge.Height = VisinaDeonicaPruge;
            lblPruge.Margin = new Padding(8, 6, 0, 0);
            tabPruge.DataBindingComplete += (s, e) => StilPruga(tabPruge, "Naziv pruge");
            tabDeonicePruge.DataBindingComplete += (s, e) => StilPruga(tabDeonicePruge, "Od");
            strPruge.Controls.Add(tabPruge);
            strPruge.Controls.Add(tabDeonicePruge);
            strPruge.Controls.Add(vrhPruga);
            int visinaDonjeg = 0;
            tabovi.SelectedIndexChanged += (s, e) =>
            {
                // Okvir tabova pravi IzgledForme; na mapi zauzima ceo ekran, a na ostalim tabovima se vraća ispod kartice.
                bool naMapi = tabovi.SelectedTab == strMapa || tabovi.SelectedTab == strPruge;
                Control okvir = tabovi.Parent;
                if (naMapi && okvir.Dock == DockStyle.Bottom) visinaDonjeg = okvir.Height;
                izgled.Mreza.Visible = !naMapi;
                okvir.Dock = naMapi ? DockStyle.Fill : DockStyle.Bottom;
                if (!naMapi && visinaDonjeg > 0) okvir.Height = visinaDonjeg;
                foreach (Splitter podela in Controls.OfType<Splitter>()) podela.Visible = !naMapi;
                // Slojevi mape se učitavaju tek kad se mapa otvori.
                if (tabovi.SelectedTab == strMapa) { var _ = PrikaziMapu(); }
                if (tabovi.SelectedTab == strPruge) { var _ = PrikaziPruge(); }
            };

            Load += (s, e) => { var _ = Osvezi(); };
            izgled.Zavrsi(tabovi, 1200, 780);
        }
    }
}
