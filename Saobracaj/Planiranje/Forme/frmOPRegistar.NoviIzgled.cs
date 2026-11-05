using System.Windows.Forms;
using Saobracaj.Tema;
using Saobracaj.Tema.Controls;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPRegistar
    {
        private static TabPage Strana(string naslov, Control sadrzaj)
        {
            var s = new TabPage(naslov);
            sadrzaj.Dock = DockStyle.Fill;
            s.Controls.Add(sadrzaj);
            return s;
        }

        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Šifarnici planiranja", "Registar pruga", null);

            var strUvoz = new TabPage("Uvoz");
            var unutra = new TabControl { Dock = DockStyle.Fill };
            unutra.TabPages.AddRange(new[]
            {
                Strana("Mesta izabrane pruge", tabMesta),
                Strana("Povezivanje brojeva pruga", tabPovezivanje),
                Strana("Problemi pri čišćenju", tabProblemi),
                Strana("Razlike prema trenutnom Registru", tabRazlike)
            });
            ThemeApplier.TabeviNovogIzgleda(unutra);
            cboPruga.SelectedIndexChanged += (s, e) => PrikaziMestaPruge();

            var kartica = new KarticaPolja("Datoteka → Čišćenje i mapiranje → Pregled razlika → Potvrda");
            kartica.Tekst(lblBrojaci);
            kartica.Polje(new Label { Text = "Pruga" }, cboPruga, siroko: true);
            kartica.Dugmad(
                EkranOP.Dugme("Izaberi datoteku", (s, e) => { var _ = IzaberiDatoteku(); }),
                EkranOP.Dugme("Odbaci", (s, e) => Odbaci()),
                EkranOP.Dugme("Potvrdi uvoz", (s, e) => { var _ = PotvrdiUvoz(); }),
                EkranOP.Dugme("Osveži", (s, e) => { var _ = Osvezi(); }));
            kartica.Dock = DockStyle.Top;
            strUvoz.Controls.Add(unutra);
            strUvoz.Controls.Add(kartica);
            strUvoz.Resize += (s, e) => { if (strUvoz.ClientSize.Width > 0) kartica.Height = kartica.Rasporedi(strUvoz.ClientSize.Width); };

            var strPregled = new TabPage("Pregled pruga");
            var podela = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1, SplitterDistance = 100 };
            stablo.Dock = DockStyle.Fill;
            tabPregled.Dock = DockStyle.Fill;
            podela.Panel1.Controls.Add(stablo);
            podela.Panel2.Controls.Add(tabPregled);
            strPregled.Controls.Add(podela);
            strPregled.Resize += (s, e) => { if (strPregled.ClientSize.Width > 700 && podela.SplitterDistance < 300) podela.SplitterDistance = 340; };
            stablo.AfterSelect += (s, e) => { if (e.Node.Tag is int) { var _ = PrikaziPrugu((int)e.Node.Tag); } };

            var tabovi = new TabControl();
            tabovi.TabPages.AddRange(new[] { strUvoz, strPregled, Strana("Istorija verzija", tabIstorija) });
            Load += (s, e) => { var _ = Osvezi(); };
            izgled.Zavrsi(tabovi, 1300, 820);
        }
    }
}
