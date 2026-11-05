using System;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Podaci;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPTurnusi : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmOPTurnusi";
        public bool Pravo { get; }

        private readonly DataGridView tabela = EkranOP.NovaTabela("ID");
        private readonly UredjivacReda polja = new UredjivacReda("Turnus");
        private readonly UredjivacReda poljaGen = new UredjivacReda("Generisanje smena");

        public frmOPTurnusi()
        {
            InitializeComponent();
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        private async Task Osvezi()
        {
            await EkranOP.Ucitaj(this, TurnusiRepo.Svi, t => tabela.DataSource = t);
        }

        private void Sacuvaj()
        {
            string naziv = polja.Tekst("Naziv");
            TimeSpan pocetak;
            if (naziv.Length == 0) { EkranOP.Poruka(this, "Unesite naziv turnusa."); return; }
            if (!TimeSpan.TryParseExact(polja.Tekst("Pocetak"), @"h\:mm", CultureInfo.InvariantCulture, out pocetak))
            {
                EkranOP.Poruka(this, "Početak unesite u obliku ČČ:MM, na primer 07:00.");
                return;
            }
            bool uspeh = EkranOP.Izvrsi(this, () => TurnusiRepo.Sacuvaj(polja.ID, naziv, pocetak, polja.CeoBroj("TrajanjeMin"), polja.CeoBroj("DanaRada"), polja.CeoBroj("DanaOdmora"), polja.Cek("Nocna"), polja.Cek("Aktivan")));
            if (uspeh) { var _ = Osvezi(); }
        }

        private async Task Generisi()
        {
            DateTime od = poljaGen.DatumVreme("Od").Date, doDan = poljaGen.DatumVreme("Do").Date;
            if (doDan < od) { EkranOP.Poruka(this, "Kraj perioda ne može biti pre početka."); return; }
            await EkranOP.Ucitaj(this, () => TurnusiRepo.GenerisiSmene(od, doDan, null),
                n => EkranOP.Poruka(this, "Upisano smena: " + n + ". Ručno unete smene nisu menjane."));
        }
    }
}
