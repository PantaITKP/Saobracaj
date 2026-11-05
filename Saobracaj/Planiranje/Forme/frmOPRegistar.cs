using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Podaci;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Forme
{
    public partial class frmOPRegistar : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmOPRegistar";
        public bool Pravo { get; }

        private readonly Label lblBrojaci = new Label { Text = "Izaberite datoteku." };
        private readonly ComboBox cboPruga = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly DataGridView tabMesta = EkranOP.NovaTabela();
        private readonly DataGridView tabPovezivanje = EkranOP.NovaTabela();
        private readonly DataGridView tabProblemi = EkranOP.NovaTabela();
        private readonly DataGridView tabRazlike = EkranOP.NovaTabela();
        private readonly TreeView stablo = new TreeView { HideSelection = false };
        private readonly DataGridView tabPregled = EkranOP.NovaTabela("ID", "StanicaID");
        private readonly DataGridView tabIstorija = EkranOP.NovaTabela("ID");

        private SadrzajRegistra registar;
        private byte[] sadrzaj;
        private string nazivDatoteke;

        public frmOPRegistar()
        {
            InitializeComponent();
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        private async Task Osvezi()
        {
            await EkranOP.Ucitaj(this, () => new { Ver = RegistarRepo.Verzije(), Pru = RegistarRepo.VazecePruge() }, p =>
            {
                tabIstorija.DataSource = p.Ver;
                stablo.BeginUpdate();
                stablo.Nodes.Clear();
                foreach (var g in p.Pru.Rows.Cast<DataRow>().GroupBy(r => r.IsNull("Kategorija") ? "Bez kategorije" : Convert.ToString(r["Kategorija"])))
                {
                    TreeNode kat = stablo.Nodes.Add(g.Key);
                    foreach (DataRow r in g)
                        kat.Nodes.Add(new TreeNode((r.IsNull("RedniBroj") ? "" : r["RedniBroj"] + ". ") + r["Naziv"]) { Tag = Convert.ToInt32(r["ID"]) });
                }
                stablo.EndUpdate();
            });
        }

        private async Task PrikaziPrugu(int id)
        {
            await EkranOP.Ucitaj(this, () => RegistarRepo.Mesta(id), t => tabPregled.DataSource = t);
        }

        private async Task IzaberiDatoteku()
        {
            string putanja;
            using (var d = new OpenFileDialog { Filter = "Excel (*.xlsx)|*.xlsx" })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                putanja = d.FileName;
            }
            await EkranOP.Ucitaj(this, () => Pripremi(putanja), p =>
            {
                registar = p.Registar;
                sadrzaj = p.Sadrzaj;
                nazivDatoteke = Path.GetFileName(putanja);
                PrikaziPripremu(p.Razlike);
            });
        }

        private sealed class Priprema
        {
            internal SadrzajRegistra Registar;
            internal byte[] Sadrzaj;
            internal List<string> Razlike;
        }

        private static Priprema Pripremi(string putanja)
        {
            byte[] b = XlsxCitac.ProcitajBajtove(putanja);
            SadrzajRegistra reg;
            using (var m = new MemoryStream(b)) reg = RegistarCitac.Procitaj(m);
            PovezivanjeRegistra.PoveziMesta(reg, RegistarRepo.StaniceZaVezu());
            PovezivanjeRegistra.PoveziPruge(reg, RegistarRepo.StarePruge());
            return new Priprema { Registar = reg, Sadrzaj = b, Razlike = PovezivanjeRegistra.Razlike(reg, RegistarRepo.VazecaMesta()) };
        }

        private void PrikaziPripremu(List<string> razlike)
        {
            List<MestoRegistra> mesta = registar.Pruge.SelectMany(p => p.Mesta).ToList();
            int bezStanice = mesta.Count(m => !m.StanicaID.HasValue && !m.JeOznaka && !m.Inostranstvo);
            lblBrojaci.Text = "Redova: " + registar.BrojRedova + "   ·   Pruga: " + registar.Pruge.Count + "   ·   Pismo: ćirilica"
                + "   ·   Mesta: " + mesta.Count + "   ·   Po nazivu: " + mesta.Count(m => m.PovezanoPo == "NAZIV")
                + "   ·   Po UIC: " + mesta.Count(m => m.PovezanoPo == "UIC")
                + "   ·   Već usklađeno: " + mesta.Count(m => m.PovezanoPo == "REGISTAR")
                + "   ·   Iza granice: " + mesta.Count(m => m.Inostranstvo)
                + "   ·   Mesta bez stanice (biće nova): " + bezStanice
                + "   ·   Novih mesta: " + mesta.Count(m => m.Promena == "NOVO")
                + "   ·   Izmenjenih: " + mesta.Count(m => m.Promena == "IZMENJENO")
                + "   ·   Van saobraćaja: " + mesta.Count(m => !m.Aktivno);

            cboPruga.DataSource = registar.Pruge.Select((p, i) => new KeyValuePair<int, string>(i, p.Kljuc + ". " + p.Naziv)).ToList();
            cboPruga.DisplayMember = "Value";
            cboPruga.ValueMember = "Key";

            var pov = new DataTable();
            foreach (string k in new[] { "Kategorija", "Redni broj", "Naziv", "dbo.Pruga", "Stanje" }) pov.Columns.Add(k);
            foreach (PrugaRegistra p in registar.Pruge)
                pov.Rows.Add(p.Kategorija, p.RedniBroj, p.Naziv, p.DboNaziv, p.IzvorniID.HasValue ? "Povezana" : "Nije povezana");
            tabPovezivanje.DataSource = pov;
            tabProblemi.DataSource = Lista("Problem", registar.Problemi);
            tabRazlike.DataSource = Lista("Razlika", razlike);
        }

        private static DataTable Lista(string naslov, IEnumerable<string> redovi)
        {
            var t = new DataTable();
            t.Columns.Add(naslov);
            foreach (string r in redovi) t.Rows.Add(r);
            return t;
        }

        private void PrikaziMestaPruge()
        {
            if (registar == null || !(cboPruga.SelectedValue is int)) return;
            PrugaRegistra p = registar.Pruge[(int)cboPruga.SelectedValue];
            var t = new DataTable();
            foreach (string k in new[] { "RB", "U Registru", "U aplikaciji", "UIC", "Klasa", "Kategorija", "Stanje" }) t.Columns.Add(k);
            foreach (MestoRegistra m in p.Mesta)
                t.Rows.Add(m.RB, m.NazivOriginal, m.Naziv, m.UIC, m.Klasa, m.Kategorija,
                    m.JeOznaka ? "Oznaka" : m.Inostranstvo ? "Iza granice" : !m.Aktivno ? "Van saobraćaja" : !m.StanicaID.HasValue ? "Nova stanica" : m.Promena ?? "Povezano: " + m.PovezanoPo);
            tabMesta.DataSource = t;
        }

        private void Odbaci()
        {
            registar = null;
            sadrzaj = null;
            cboPruga.DataSource = null;
            tabMesta.DataSource = null;
            tabPovezivanje.DataSource = null;
            tabProblemi.DataSource = null;
            tabRazlike.DataSource = null;
            lblBrojaci.Text = "Izaberite datoteku.";
        }

        private async Task PotvrdiUvoz()
        {
            if (registar == null) { EkranOP.Poruka(this, "Prvo izaberite datoteku."); return; }
            if (!EkranOP.Potvrdi(this, "Potvrditi Registar? Trenutna važeća verzija ide u arhivu.")) return;
            SadrzajRegistra r = registar;
            byte[] b = sadrzaj;
            string naziv = nazivDatoteke;
            await EkranOP.Ucitaj(this, () => RegistarRepo.Potvrdi("Registar · " + naziv, naziv, b, r), v =>
            {
                Odbaci();
                EkranOP.Poruka(this, "Registar je potvrđen.");
                var _ = Osvezi();
            });
        }
    }
}
