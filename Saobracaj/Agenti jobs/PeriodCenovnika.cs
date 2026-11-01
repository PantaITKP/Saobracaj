using System;
using System.Windows.Forms;

namespace Saobracaj.Agenti_jobs
{
    public partial class PeriodCenovnika : Form
    {
        public DateTime DatumOd { get { return dtpOd.Value.Date; } }
        public DateTime DatumDo { get { return dtpDo.Value.Date; } }
        public int Godina { get { return DatumDo.Year; } }

        public PeriodCenovnika() : this(null, null, "")
        {
        }

        public PeriodCenovnika(DateTime? od, DateTime? doDana, string opis)
        {
            InitializeComponent();
            lblOpis.Text = opis;
            dtpOd.Value = od ?? new DateTime(DateTime.Today.Year, 1, 1);
            dtpDo.Value = doDana ?? new DateTime(DateTime.Today.Year, 12, 31);
            PrikaziGodinu();
        }

        private void PrikaziGodinu() { lblGodina.Text = "Godina važenja: " + Godina; }
        private void dtpOd_ValueChanged(object sender, EventArgs e) { PrikaziGodinu(); }
        private void dtpDo_ValueChanged(object sender, EventArgs e) { PrikaziGodinu(); }
        private void btnPotvrdi_Click(object sender, EventArgs e)
        {
            if (DatumOd > DatumDo)
            {
                MessageBox.Show(this, "Datum od ne može biti posle datuma do.");
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
        private void btnOdustani_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
