using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Saobracaj.Agenti_jobs
{
    public partial class DopunaRadnogNaloga : Form
    {
        private readonly List<RNRaspodela> raspodela = new List<RNRaspodela>();
        private readonly SifarnikPrevoz prevoz;
        private readonly bool rezi;
        private readonly int osnovnaNajava;
        private DataRow najava, nalog, stavka;
        private bool ucitavanje;
        private GridColumnFilters filterRaspodela;
        public DopunaRadnogNaloga()
        {
            InitializeComponent();
            filterRaspodela = new GridColumnFilters(dgvRaspodela);
        }
        internal DopunaRadnogNaloga(SifarnikPrevoz p, DataRow voz) : this()
        {
            prevoz = p;
            rezi = Convert.ToBoolean(voz["Rezi"]);
            osnovnaNajava = int.Parse(Db.Text(voz, "AnnouncementFullId"));
            txtNajava.Text = osnovnaNajava.ToString();
            lblVoz.Text = "Voz " + p.BrojVoznje + " | " + Db.Text(voz, "From") + " - " + Db.Text(voz, "To") +
                (rezi ? " | REZI (0 kola)" : "") + " | bruto: " + voz["Weight"] + " t";
            numKola.Enabled = !rezi;
            chkTovareno.Enabled = !rezi;
            FillDGV();
        }
        private void FillDGV()
        {
            var data = new DataTable();
            data.Columns.Add("__Red", typeof(int)); data.Columns.Add("IDNajave", typeof(int));
            data.Columns.Add("RadniNalog", typeof(int)); data.Columns.Add("Stavka", typeof(string));
            data.Columns.Add("BrojKola", typeof(int)); data.Columns.Add("Tovareno", typeof(bool));
            data.Columns.Add("PlaniranaMasa", typeof(decimal));
            for (int i = 0; i < raspodela.Count; i++)
            {
                var x = raspodela[i];
                data.Rows.Add(i, x.IDNajave, x.RadniNalog, x.RB == 0 ? "Nova" : x.RB.ToString(), x.BrojKola, x.Tovareno, x.PlaniranaMasa);
            }
            dgvRaspodela.DataSource = data;
            dgvRaspodela.Columns["__Red"].Visible = false;
            dgvRaspodela.Columns["IDNajave"].HeaderText = "Najava";
            dgvRaspodela.Columns["RadniNalog"].HeaderText = "Radni nalog";
            dgvRaspodela.Columns["BrojKola"].HeaderText = "Broj kola";
            dgvRaspodela.Columns["PlaniranaMasa"].HeaderText = "Planirana masa (t)";
        }
        private void dgvRaspodela_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        { if (filterRaspodela != null) filterRaspodela.Header(sender, e); }
        private void dgvRaspodela_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        { if (filterRaspodela != null) filterRaspodela.Bound(sender, e); }

        private async void btnUcitajNajavu_Click(object sender, EventArgs e)
        {
            if (ucitavanje) return;
            int id;
            if (!int.TryParse(txtNajava.Text, out id) || id <= 0) { MessageBox.Show(this, "Unesite puni ID najave, npr. 261554."); return; }
            ucitavanje = true; btnUcitajNajavu.Enabled = false;
            try
            {
                nalog = null; stavka = null; najava = null;
                txtNalog.Text = ""; txtStavka.Text = "Nova stavka";
                var n = await Task.Run(() => RadniNalogDopuna.Najava(id));
                var izbor = await Task.Run(() => RadniNalogDopuna.Nalozi(id));
                if (IsDisposed || Disposing) return;
                if (izbor.Rows.Count == 0) throw new InvalidOperationException("Za ovu najavu ne postoji radni nalog u RadniNalogVezaNajave. Najpre ga povezite u postojecoj aplikaciji.");
                najava = n;
                numKola.Value = rezi ? 0 : (n.IsNull("BrojKola") ? 0 : Convert.ToDecimal(n["BrojKola"]));
                numMasa.Value = n.IsNull("Tezina") ? 0 : Convert.ToDecimal(n["Tezina"]);
                chkTovareno.Checked = !rezi && !n.IsNull("Tovareno") && Convert.ToInt32(n["Tovareno"]) == 1;
                if (izbor.Rows.Count == 1) nalog = izbor.Rows[0];
                else using (var f = new IzborStaniceCenovnika("Postojeci radni nalog", "Najava " + id + ": izaberite nalog kojem pripada ova voznja. Proverite prikazani status.", izbor))
                    if (f.ShowDialog(this) == DialogResult.OK) nalog = f.IzabraniRed;
                if (nalog != null) txtNalog.Text = nalog["ID"] + " | status: " + Db.Text(nalog, "StatusRN");
            }
            catch (Exception ex) { if (!IsDisposed) MessageBox.Show(this, ex.Message, "Izbor naloga"); }
            finally { ucitavanje = false; if (!IsDisposed) btnUcitajNajavu.Enabled = true; }
        }
        private async void btnIzaberiStavku_Click(object sender, EventArgs e)
        {
            if (nalog == null || ucitavanje) { MessageBox.Show(this, "Najpre ucitajte najavu i izaberite nalog."); return; }
            ucitavanje = true; btnIzaberiStavku.Enabled = false;
            try
            {
                int rn = Convert.ToInt32(nalog["ID"]);
                var data = await Task.Run(() => RadniNalogDopuna.Stavke(rn, prevoz.Broj));
                if (IsDisposed || Disposing) return;
                foreach (var r in data.Rows.Cast<DataRow>().Where(r => !RadniNalogDopuna.Nezavrsena(r)).ToList()) data.Rows.Remove(r);
                if (data.Rows.Count == 0) { MessageBox.Show(this, "Nema nezavrsenih stavki izvorne trase. Koristite novu stavku."); return; }
                using (var f = new IzborStaniceCenovnika("Dopuna postojece stavke", "Izaberite RB koji odgovara ovoj deonici. Realizovana vremena i mase bice popunjeni, planirani datumi ostaju sacuvani.", data))
                    if (f.ShowDialog(this) == DialogResult.OK)
                    { stavka = f.IzabraniRed; txtStavka.Text = "RB " + stavka["RB"] + " | " + stavka["Otpravna"] + " - " + stavka["Uputna"]; }
            }
            catch (Exception ex) { if (!IsDisposed) MessageBox.Show(this, ex.Message); }
            finally { ucitavanje = false; if (!IsDisposed) btnIzaberiStavku.Enabled = true; }
        }
        private void btnNovaStavka_Click(object sender, EventArgs e) { stavka = null; txtStavka.Text = "Nova stavka"; }
        private void txtNajava_TextChanged(object sender, EventArgs e)
        {
            najava = null; nalog = null; stavka = null;
            txtNalog.Text = ""; txtStavka.Text = "Nova stavka";
        }
        private void btnDodaj_Click(object sender, EventArgs e)
        {
            if (ucitavanje || najava == null || nalog == null) { MessageBox.Show(this, "Ucitajte najavu i izaberite nalog."); return; }
            var a = new RNRaspodela
            {
                IDNajave = Convert.ToInt32(najava["ID"]),
                RadniNalog = Convert.ToInt32(nalog["ID"]),
                RB = stavka == null ? 0 : Convert.ToInt32(stavka["RB"]),
                BrojKola = rezi ? 0 : Convert.ToInt32(numKola.Value),
                Tovareno = !rezi && chkTovareno.Checked,
                PlaniranaMasa = numMasa.Value,
                NajavaSnapshot = najava,
                StavkaSnapshot = stavka,
                StatusSnapshot = Db.Text(nalog, "StatusRN")
            };
            if ((!rezi && a.BrojKola <= 0) || raspodela.Any(x => x.IDNajave == a.IDNajave && x.RadniNalog == a.RadniNalog))
            { MessageBox.Show(this, "Unesite broj kola veci od nule. Ista najava na istom nalogu unosi se jednom; za izmenu uklonite prethodni red."); return; }
            int total = najava.IsNull("BrojKola") ? 0 : Convert.ToInt32(najava["BrojKola"]);
            if (raspodela.Where(x => x.IDNajave == a.IDNajave).Sum(x => x.BrojKola) + a.BrojKola > total)
            { MessageBox.Show(this, "Broj kola u ovoj voznji prelazi ukupni broj kola najave."); return; }
            if ((rezi || a.BrojKola < total) && MessageBox.Show(this,
                "Potvrdite planiranu masu za ovaj deo najave: " + a.PlaniranaMasa + " t. Masa se ne rasporedjuje automatski po broju kola.",
                "Planirana masa", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            raspodela.Add(a); FillDGV();
        }
        private void btnUkloni_Click(object sender, EventArgs e)
        {
            if (dgvRaspodela.CurrentRow == null) return;
            var view = dgvRaspodela.CurrentRow.DataBoundItem as DataRowView;
            if (view == null) return;
            int i = Convert.ToInt32(view["__Red"]);
            if (i >= 0 && i < raspodela.Count) { raspodela.RemoveAt(i); FillDGV(); }
        }
        private void btnPotvrdi_Click(object sender, EventArgs e)
        {
            if (ucitavanje) return;
            try
            {
                RadniNalogPravila.ProveriRaspodelu(raspodela, rezi, osnovnaNajava);
                if (MessageBox.Show(this, "Potvrdjujete izabrane postojece naloge i njihove statuse za ovu realizaciju? Statusi naloga i najava se ne menjaju.",
                    "Dopuna naloga", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                prevoz.RNStavke = raspodela.ToList(); DialogResult = DialogResult.OK; Close();
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message); }
        }
        private void btnOdustani_Click(object sender, EventArgs e) { DialogResult = DialogResult.Cancel; Close(); }
    }
}
