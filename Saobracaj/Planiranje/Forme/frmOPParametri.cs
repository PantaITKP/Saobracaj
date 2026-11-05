using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Podaci;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPParametri : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmOPParametri";
        public bool Pravo { get; }

        private readonly DataGridView tabela = EkranOP.NovaTabela("ID", "StanicaID", "StanicaDoID", "SerijaID");
        private readonly UredjivacReda polja = new UredjivacReda("Parametar");

        public frmOPParametri()
        {
            InitializeComponent();
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        private async Task Osvezi()
        {
            await EkranOP.Ucitaj(this, () => new
            {
                Par = ParametriRepo.SviSaNazivima(),
                Sta = StaniceRepo.ZaIzbor(),
                Ser = LokomotiveRepo.Serije()
            }, p =>
            {
                polja.IzvorIzbora("StanicaID", p.Sta, "Naziv", "ID", true);
                polja.IzvorIzbora("StanicaDoID", p.Sta, "Naziv", "ID", true);
                polja.IzvorIzbora("SerijaID", p.Ser, "Oznaka", "ID", true);
                tabela.DataSource = p.Par;
            });
        }

        private void Sacuvaj()
        {
            string kljuc = polja.Tekst("Kljuc");
            if (kljuc.Length == 0) { EkranOP.Poruka(this, "Unesite ključ parametra."); return; }
            decimal? vrednost = null;
            bool uspeh = EkranOP.Izvrsi(this, () =>
            {
                vrednost = polja.DecimalIliNull("Vrednost", "Vrednost");
                if (!vrednost.HasValue) throw new FormatException("Unesite vrednost parametra.");
                DateTime od = polja.DatumVreme("VaziOd").Date;
                DateTime? doDan = polja.DatumIliNull("VaziDo");
                if (polja.ID.HasValue)
                    ParametriRepo.Sacuvaj(polja.ID.Value, polja.RowVer, kljuc, polja.IzabranID("StanicaID"), polja.IzabranID("StanicaDoID"), polja.IzabranID("SerijaID"), vrednost.Value, polja.TekstIliNull("Jedinica"), polja.TekstIliNull("Opis"), polja.TekstIliNull("Izvor"), od, doDan);
                else
                    ParametriRepo.Dodaj(kljuc, polja.IzabranID("StanicaID"), polja.IzabranID("StanicaDoID"), polja.IzabranID("SerijaID"), vrednost.Value, polja.TekstIliNull("Jedinica"), polja.TekstIliNull("Opis"), polja.TekstIliNull("Izvor"), od, doDan);
            });
            if (uspeh) { var _ = Osvezi(); }
        }
    }
}
