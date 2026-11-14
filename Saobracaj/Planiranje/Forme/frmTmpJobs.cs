using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Saobracaj.Planiranje.Podaci;

namespace Saobracaj.Planiranje.Forme
{
    // Pregled poslova koje je agent CimProvera upisao iz tovarnih listova (planiranje.tmpJobs, test faze).
    // Samo prikaz: forma ništa ne menja u tmp tabelama.
    public partial class frmTmpJobs : Saobracaj.Tema.BaseForm
    {
        public static string code = "frmTmpJobs";
        public bool Pravo { get; }

        private static readonly Color BojaNepronadjenog = Color.FromArgb(0xC0, 0x50, 0x00);

        private readonly DataGridView tabela = EkranOP.NovaTabela("ID");
        private readonly DataGridView tabDokumenti = EkranOP.NovaTabela("ID");
        private readonly DataGridView tabPoreklo = EkranOP.NovaTabela();
        private readonly DataGridView tabIstorija = EkranOP.NovaTabela();
        private readonly TextBox txtNapomena = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None, BackColor = Color.White };
        private readonly UredjivacReda filter = new UredjivacReda("Filter");
        private readonly Label lblBroj = new Label { Text = "" };
        private readonly Dictionary<string, string> idZaNaziv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private int? prikazanPosao;
        private Font kurziv;

        public frmTmpJobs()
        {
            InitializeComponent();
            for (int i = 0; i < TmpCimRepo.NaziviIDevi.GetLength(0); i++) idZaNaziv[TmpCimRepo.NaziviIDevi[i, 0]] = TmpCimRepo.NaziviIDevi[i, 1];
            PrimeniNoviIzgled();
            Pravo = PraviloPristupa.Proveri(code);
        }

        private int? IzabranPosao
        {
            get
            {
                DataRow r = EkranOP.Izabran(tabela);
                return r == null ? (int?)null : Convert.ToInt32(r["ID"]);
            }
        }

        private async Task Osvezi()
        {
            DateTime od = filter.DatumVreme("Od"), doDana = filter.DatumVreme("Do");
            string tekst = filter.Tekst("Trazi");
            bool prazni = filter.Cek("SamoPrazni");
            int? bio = IzabranPosao;
            await EkranOP.Ucitaj(this, () => TmpCimRepo.Poslovi(od, doDana, tekst, prazni), t =>
            {
                prikazanPosao = null;
                tabela.DataSource = t;
                lblBroj.Text = "Poslova: " + t.Rows.Count + (t.Rows.Count == 0 ? " – agent u izabranom periodu nije upisao nijedan posao." : "");
                if (bio.HasValue) Izaberi(bio.Value);
                if (t.Rows.Count == 0) PrikaziDetalje(null);
            });
        }

        private void Izaberi(int id)
        {
            foreach (DataGridViewRow r in tabela.Rows)
            {
                var red = r.DataBoundItem as DataRowView;
                if (red == null || Convert.ToInt32(red["ID"]) != id) continue;
                tabela.ClearSelection();
                r.Selected = true;
                tabela.CurrentCell = r.Cells[FirstVisible(r)];
                return;
            }
        }

        private static int FirstVisible(DataGridViewRow r)
        {
            foreach (DataGridViewCell c in r.Cells) if (c.Visible) return c.ColumnIndex;
            return 0;
        }

        private void PrikaziDetalje(int? id)
        {
            if (id == prikazanPosao && id.HasValue) return;
            prikazanPosao = id;
            if (!id.HasValue)
            {
                txtNapomena.Text = "";
                tabDokumenti.DataSource = null;
                tabPoreklo.DataSource = null;
                tabIstorija.DataSource = null;
                return;
            }
            int posao = id.Value;
            var _ = EkranOP.Ucitaj(this, () => new
            {
                Nap = TmpCimRepo.Napomena(posao),
                Dok = TmpCimRepo.Dokumenti(posao),
                Por = TmpCimRepo.PorekloPolja(posao),
                Ist = TmpCimRepo.Istorija(posao)
            }, d =>
            {
                // Korisnik je u međuvremenu izabrao drugi posao.
                if (prikazanPosao != posao) return;
                txtNapomena.Text = (d.Nap ?? "").Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
                tabDokumenti.DataSource = d.Dok;
                tabPoreklo.DataSource = d.Por;
                tabIstorija.DataSource = d.Ist;
            });
        }

        private void OtvoriTeretnicu()
        {
            int? id = IzabranPosao;
            if (!id.HasValue) { EkranOP.Poruka(this, "Izaberite posao."); return; }
            int broj = 0;
            if (!EkranOP.Izvrsi(this, () => broj = TmpCimRepo.BrojTeretnica(id.Value))) return;
            if (broj == 0) { EkranOP.Poruka(this, "Za ovaj posao nema teretnice"); return; }
            var forma = new frmTmpTeretnica(id.Value);
            if (forma.Pravo) forma.Show(this); else forma.Dispose();
        }

        // Naziv čiji ID je prazan (agent nije pronašao šifru) prikazuje tekst iz dokumenta, u boji.
        private void Oboji(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string kolona = tabela.Columns[e.ColumnIndex].Name;
            string idKolona;
            if (!idZaNaziv.TryGetValue(kolona, out idKolona)) return;
            var red = tabela.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (red == null || !red.Row.Table.Columns.Contains(idKolona) || !red.Row.IsNull(idKolona)) return;
            if (e.Value == null || e.Value == DBNull.Value || Convert.ToString(e.Value).Trim().Length == 0)
            {
                e.Value = "—";
                e.CellStyle.ForeColor = Color.Gray;
            }
            else
            {
                e.Value = "? " + Convert.ToString(e.Value).Trim();
                e.CellStyle.ForeColor = BojaNepronadjenog;
                e.CellStyle.SelectionForeColor = Color.FromArgb(0xFF, 0xE0, 0xB0);
                e.CellStyle.Font = kurziv ?? (kurziv = new Font(tabela.Font, FontStyle.Italic));
            }
            e.FormattingApplied = true;
        }
    }
}
