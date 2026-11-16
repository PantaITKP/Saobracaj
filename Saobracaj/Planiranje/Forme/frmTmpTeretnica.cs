using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Podaci;

namespace Saobracaj.Planiranje.Forme
{
    // Teretnica (spisak kola CIT 23) koju je agent CimProvera upisao za posao (planiranje.tmpTeretnica i tmpTeretnicaStavke).
    // Bez posla (otvorena iz menija) prikazuje teretnice poslednjih 30 dana, i one koje još nisu povezane sa poslom.
    // Samo prikaz.
    public partial class frmTmpTeretnica : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmTmpTeretnica";
        public bool Pravo { get; }

        private readonly int? jobId;
        private readonly DataGridView tabela = EkranOP.NovaTabela("ID", "OtpravnaID", "UputnaID", "IzvoznaID", "UvoznaID");
        private readonly UredjivacReda zaglavlje = new UredjivacReda("Zaglavlje teretnice");
        private readonly ComboBox cboTeretnica = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Label lblZbir = new Label { Text = "" };
        private bool puni;

        public frmTmpTeretnica() : this(null) { }

        public frmTmpTeretnica(int? jobId)
        {
            this.jobId = jobId;
            InitializeComponent();
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        private int? IzabranaTeretnica
        {
            get
            {
                object v = cboTeretnica.SelectedValue;
                return v == null || v == DBNull.Value || v is DataRowView ? (int?)null : Convert.ToInt32(v);
            }
        }

        private async Task Osvezi()
        {
            int? bila = IzabranaTeretnica;
            await EkranOP.Ucitaj(this, () => TmpCimRepo.Teretnice(jobId), t =>
            {
                puni = true;
                cboTeretnica.DisplayMember = "Naziv";
                cboTeretnica.ValueMember = "ID";
                cboTeretnica.DataSource = t;
                puni = false;
                if (t.Rows.Count == 0)
                {
                    zaglavlje.Popuni(null);
                    tabela.DataSource = null;
                    lblZbir.Text = jobId.HasValue ? "Za ovaj posao nema teretnice." : "Agent u poslednjih 30 dana nije upisao nijednu teretnicu.";
                    return;
                }
                if (bila.HasValue && t.Rows.Cast<DataRow>().Any(r => Convert.ToInt32(r["ID"]) == bila.Value)) cboTeretnica.SelectedValue = bila.Value;
                else cboTeretnica.SelectedIndex = 0;
                var _ = Prikazi();
            });
        }

        private async Task Prikazi()
        {
            int? id = IzabranaTeretnica;
            if (!id.HasValue) return;
            int ter = id.Value;
            await EkranOP.Ucitaj(this, () => new { Zag = TmpCimRepo.ZaglavljeTeretnice(ter), Sta = TmpCimRepo.Stavke(ter) }, d =>
            {
                if (IzabranaTeretnica != ter) return;
                zaglavlje.Popuni(d.Zag.Rows.Count > 0 ? d.Zag.Rows[0] : null);
                tabela.DataSource = d.Sta;
                lblZbir.Text = Zbir(d.Sta);
            });
        }

        // Zbirovi ispod stavki: broj kola, dužina (m), tara, neto i bruto (t) – jedinice kao dbo.TeretnicaStavke.
        internal static string Zbir(DataTable t)
        {
            Func<string, decimal> suma = k => t.Rows.Cast<DataRow>().Where(r => !r.IsNull(k)).Sum(r => Convert.ToDecimal(r[k]));
            decimal duzina = suma("Dužina (m)"), tara = suma("Tara (t)"), neto = suma("Neto (t)");
            var c = CultureInfo.GetCultureInfo("sr-Latn-RS");
            return "Ukupno: kola " + t.Rows.Count
                + " · dužina " + duzina.ToString("N2", c) + " m"
                + " · tara " + tara.ToString("N3", c) + " t"
                + " · neto " + neto.ToString("N3", c) + " t"
                + " · bruto " + (tara + neto).ToString("N3", c) + " t";
        }
    }
}
