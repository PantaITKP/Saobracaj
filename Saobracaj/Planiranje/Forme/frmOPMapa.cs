using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Kontrole;
using Saobracaj.Planiranje.Podaci;
using Saobracaj.Tema;

namespace Saobracaj.Planiranje.Forme
{
    // Mapa lokomotiva: samo mapa sa trakom slojeva na vrhu i legendom u dnu.
    public partial class frmOPMapa : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmOPMapa";
        public bool Pravo { get; }

        private readonly KontrolaMape mapa = new KontrolaMape { Dock = DockStyle.Fill };

        public frmOPMapa()
        {
            InitializeComponent();
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        private void PrimeniNoviIzgled()
        {
            var izgled = new IzgledForme(this, "Transport / Tabla", "Mapa lokomotiva", null);
            Load += (s, e) => { var _ = Ucitaj(); };
            izgled.Zavrsi(mapa, 1400, 860);
            // Mapa zauzima ceo ekran ispod naslova.
            mapa.Parent.Dock = DockStyle.Fill;
            izgled.Mreza.Visible = false;
            foreach (Splitter podela in Controls.OfType<Splitter>()) podela.Visible = false;
        }

        private async Task Ucitaj()
        {
            await EkranOP.Ucitaj(this, () => new { Stanice = StaniceRepo.Sve(false), Veze = StaniceRepo.Veze() },
                d => mapa.Prikazi(d.Stanice, d.Veze));
            // Mreža nije učitana (greška je već prikazana): pokrivač ne sme da ostane.
            if (!IsDisposed && !mapa.Ucitana) mapa.PrikaziUcitavanje(false);
        }
    }
}
