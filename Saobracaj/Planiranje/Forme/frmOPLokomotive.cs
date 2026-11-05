using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Podaci;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPLokomotive : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmOPLokomotive";
        public bool Pravo { get; }

        private readonly DataGridView tabLokomotive = EkranOP.NovaTabela("ID", "SerijaID", "MaticnaStanicaID");
        private readonly DataGridView tabSerije = EkranOP.NovaTabela("ID", "IzvorniID");
        private readonly DataGridView tabZauzetost = EkranOP.NovaTabela("ID", "LokomotivaID");
        private readonly UredjivacReda poljaLok = new UredjivacReda("Lokomotiva");
        private readonly UredjivacReda poljaSer = new UredjivacReda("Serija");
        private readonly UredjivacReda poljaZau = new UredjivacReda("Zauzetost");

        public frmOPLokomotive()
        {
            InitializeComponent();
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        private async Task Osvezi()
        {
            await EkranOP.Ucitaj(this, () => new
            {
                Lok = LokomotiveRepo.Lokomotive(false),
                Ser = LokomotiveRepo.Serije(),
                Zau = LokomotiveRepo.Zauzetosti(),
                Sta = StaniceRepo.ZaIzbor()
            }, p =>
            {
                poljaLok.IzvorIzbora("SerijaID", p.Ser, "Oznaka", "ID", true);
                poljaLok.IzvorIzbora("MaticnaStanicaID", p.Sta, "Naziv", "ID", true);
                poljaZau.IzvorIzbora("LokomotivaID", p.Lok, "Broj", "ID", false);
                tabLokomotive.DataSource = p.Lok;
                tabSerije.DataSource = p.Ser;
                tabZauzetost.DataSource = p.Zau;
            });
        }

        private void SacuvajLokomotivu()
        {
            string broj = poljaLok.Tekst("Broj");
            if (broj.Length == 0) { EkranOP.Poruka(this, "Unesite broj lokomotive."); return; }
            string vlasnistvo = poljaLok.Tekst("Vlasnistvo");
            if (vlasnistvo.Length == 0) vlasnistvo = "SOPSTVENA";
            bool uspeh = EkranOP.Izvrsi(this, () =>
            {
                decimal? masa = poljaLok.DecimalIliNull("MasaT", "Masa");
                if (poljaLok.ID.HasValue)
                    LokomotiveRepo.SacuvajLokomotivu(poljaLok.ID.Value, poljaLok.RowVer, broj, poljaLok.TekstIliNull("Naziv"), poljaLok.IzabranID("SerijaID"), vlasnistvo, poljaLok.TekstIliNull("Zakupodavac"), poljaLok.Cek("ImaGPS"), poljaLok.TekstIliNull("TechnodisID"), poljaLok.IzabranID("MaticnaStanicaID"), masa, poljaLok.Cek("Aktivna"));
                else
                    LokomotiveRepo.DodajLokomotivu(broj, poljaLok.TekstIliNull("Naziv"), poljaLok.IzabranID("SerijaID"), vlasnistvo, poljaLok.TekstIliNull("Zakupodavac"), poljaLok.Cek("ImaGPS"), poljaLok.TekstIliNull("TechnodisID"), poljaLok.IzabranID("MaticnaStanicaID"), masa, poljaLok.Cek("Aktivna"));
            });
            if (uspeh) { var _ = Osvezi(); }
        }

        private void SacuvajSeriju()
        {
            string oznaka = poljaSer.Tekst("Oznaka");
            if (oznaka.Length == 0) { EkranOP.Poruka(this, "Unesite oznaku serije."); return; }
            bool uspeh = EkranOP.Izvrsi(this, () =>
            {
                decimal? masa = poljaSer.DecimalIliNull("MasaT", "Masa"), snaga = poljaSer.DecimalIliNull("SnagaKW", "Snaga"), duzina = poljaSer.DecimalIliNull("DuzinaM", "Dužina"), vmax = poljaSer.DecimalIliNull("VmaxKmh", "Vmax");
                int? v = vmax.HasValue ? (int?)vmax.Value : null;
                if (poljaSer.ID.HasValue)
                    LokomotiveRepo.SacuvajSeriju(poljaSer.ID.Value, poljaSer.RowVer, oznaka, poljaSer.TekstIliNull("Opis"), poljaSer.TekstIliNull("VrstaVuce"), masa, snaga, v, duzina, poljaSer.Cek("Aktivna"));
                else
                    LokomotiveRepo.DodajSeriju(oznaka, poljaSer.TekstIliNull("Opis"), poljaSer.TekstIliNull("VrstaVuce"), masa, snaga, v, duzina, poljaSer.Cek("Aktivna"));
            });
            if (uspeh) { var _ = Osvezi(); }
        }

        private void SacuvajZauzetost()
        {
            int? lok = poljaZau.IzabranID("LokomotivaID");
            string vrsta = poljaZau.Tekst("Vrsta");
            DateTime od = poljaZau.DatumVreme("Od"), doVreme = poljaZau.DatumVreme("Do");
            if (!lok.HasValue || vrsta.Length == 0) { EkranOP.Poruka(this, "Izaberite lokomotivu i vrstu zauzetosti."); return; }
            if (doVreme <= od) { EkranOP.Poruka(this, "Kraj mora biti posle početka."); return; }
            bool uspeh = EkranOP.Izvrsi(this, () => LokomotiveRepo.SacuvajZauzetost(poljaZau.ID, poljaZau.RowVer, lok.Value, vrsta, od, doVreme, poljaZau.TekstIliNull("Projekat"), poljaZau.TekstIliNull("Napomena")));
            if (uspeh) { var _ = Osvezi(); }
        }

        private void ObrisiZauzetost()
        {
            if (!poljaZau.ID.HasValue || !EkranOP.Potvrdi(this, "Obrisati izabranu zauzetost?")) return;
            if (EkranOP.Izvrsi(this, () => LokomotiveRepo.ObrisiZauzetost(poljaZau.ID.Value, poljaZau.RowVer)))
            {
                poljaZau.Popuni(null);
                var _ = Osvezi();
            }
        }

        private void Dopuni()
        {
            int dodato = 0;
            if (EkranOP.Izvrsi(this, () => dodato = LokomotiveRepo.DopuniIzStarog()))
            {
                EkranOP.Poruka(this, "Dodato novih serija i lokomotiva: " + dodato + ".");
                var _ = Osvezi();
            }
        }
    }
}
