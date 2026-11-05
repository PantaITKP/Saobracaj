using System.Drawing;
using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPTrase
    {
        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Šifarnici planiranja", "Trase", null);

            // Uvoz
            var strUvoz = new TabPage("Uvoz iz Excela");
            var unutra = new TabControl { Dock = DockStyle.Fill };
            var sRaz = new TabPage("Razlike");
            var sGre = new TabPage("Greške za ispravku");
            var sIst = new TabPage("Verzija i istorija");
            tabRazlike.Dock = DockStyle.Fill; tabGreske.Dock = DockStyle.Fill; tabIstorija.Dock = DockStyle.Fill;
            sRaz.Controls.Add(tabRazlike); sGre.Controls.Add(tabGreske); sIst.Controls.Add(tabIstorija);
            unutra.TabPages.AddRange(new[] { sRaz, sGre, sIst });
            ThemeApplier.TabeviNovogIzgleda(unutra);
            var uvozKartica = new KarticaPolja("Datoteka → Pregled razlika → Potvrda → Objava");
            uvozKartica.Tekst(lblBrojaci)
                .Dugmad(
                    EkranOP.Dugme("Izaberi datoteku", (s, e) => { var _ = IzaberiDatoteku(); }),
                    EkranOP.Dugme("Odbaci", (s, e) => Odbaci()),
                    EkranOP.Dugme("Potvrdi uvoz", (s, e) => { var _ = PotvrdiUvoz(); }),
                    EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }));
            uvozKartica.Dock = DockStyle.Top;
            strUvoz.Controls.Add(unutra);
            strUvoz.Controls.Add(uvozKartica);
            strUvoz.Resize += (s, e) => { if (strUvoz.ClientSize.Width > 0) uvozKartica.Height = uvozKartica.Rasporedi(strUvoz.ClientSize.Width); };

            // Sve trase
            var strTrase = new TabPage("Sve trase");
            poljaTrasa.Izbor("PocetnaStanicaID", "Početna stanica")
                .Izbor("KrajnjaStanicaID", "Krajnja stanica")
                .Dugmad(
                    EkranOP.Dugme("Sačuvaj stanice", (s, e) => SacuvajStanice()),
                    EkranOP.Dugme("Izračunaj rutu", (s, e) => { var _ = IzracunajRutu(); }),
                    EkranOP.Dugme("Osveži", (s, e) => { var _ = PrikaziVerziju(); }));
            poljaTrasa.Kartica.Polje(new Label { Text = "Verzija" }, cboVerzija, siroko: true);
            cboVerzija.SelectedIndexChanged += (s, e) => { var _ = PrikaziVerziju(); };
            tabTrase.SelectionChanged += (s, e) => { if (tabTrase.SelectedRows.Count > 0) poljaTrasa.Popuni(EkranOP.Izabran(tabTrase)); };
            EkranOP.Strana(strTrase, poljaTrasa.Kartica, tabTrase);

            // Ad hoc
            var strAdHoc = new TabPage("Ad hoc");
            poljaAdHoc.Tekst("BrojVoza", "Broj voza")
                .BrojPrazno("MaxMasaT", "Max masa (t)")
                .Izbor("PocetnaStanicaID", "Početna stanica")
                .Izbor("KrajnjaStanicaID", "Krajnja stanica")
                .Tekst("VremePolaska", "Polazak (ČČ:MM)")
                .Tekst("VremeDolaska", "Dolazak (ČČ:MM)")
                .Datum("AdHocVaziOd", "Važi od")
                .Datum("AdHocVaziDo", "Važi do")
                .Tekst("Napomena", "Napomena", siroko: true)
                .Dugmad(
                    EkranOP.Dugme("Odobri", (s, e) => OdobriAdHoc()),
                    EkranOP.Dugme("Odbij", (s, e) => OdbijAdHoc()),
                    EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }));
            tabAdHoc.SelectionChanged += (s, e) => { if (tabAdHoc.SelectedRows.Count > 0) poljaAdHoc.Popuni(EkranOP.Izabran(tabAdHoc)); };
            EkranOP.Strana(strAdHoc, poljaAdHoc.Kartica, tabAdHoc);

            tabovi.TabPages.AddRange(new[] { strUvoz, strTrase, strAdHoc });
            tabovi.SelectedIndex = (int)PocetniTab;
            Load += (s, e) => { var _ = Osvezi(); };
            izgled.Zavrsi(tabovi, 1300, 820);
        }
    }
}
