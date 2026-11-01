namespace Saobracaj.Agenti_jobs
{
    partial class PeriodCenovnika
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }
        private void InitializeComponent()
        {
            this.lblOpis = new System.Windows.Forms.Label();
            this.lblOd = new System.Windows.Forms.Label();
            this.lblDo = new System.Windows.Forms.Label();
            this.lblGodina = new System.Windows.Forms.Label();
            this.dtpOd = new System.Windows.Forms.DateTimePicker();
            this.dtpDo = new System.Windows.Forms.DateTimePicker();
            this.btnPotvrdi = new System.Windows.Forms.Button();
            this.btnOdustani = new System.Windows.Forms.Button();
            this.SuspendLayout();
            this.lblOpis.Name = "lblOpis";
            this.lblOpis.Location = new System.Drawing.Point(16, 12);
            this.lblOpis.Size = new System.Drawing.Size(470, 50);
            this.lblOd.Name = "lblOd"; this.lblOd.Text = "Važi od:";
            this.lblOd.Location = new System.Drawing.Point(16, 78);
            this.lblDo.Name = "lblDo"; this.lblDo.Text = "Važi do:";
            this.lblDo.Location = new System.Drawing.Point(16, 118);
            this.dtpOd.Name = "dtpOd";
            this.dtpOd.Location = new System.Drawing.Point(140, 74);
            this.dtpOd.Size = new System.Drawing.Size(220, 24);
            this.dtpOd.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpOd.CustomFormat = "dd.MM.yyyy";
            this.dtpOd.ValueChanged += this.dtpOd_ValueChanged;
            this.dtpDo.Name = "dtpDo";
            this.dtpDo.Location = new System.Drawing.Point(140, 114);
            this.dtpDo.Size = new System.Drawing.Size(220, 24);
            this.dtpDo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpDo.CustomFormat = "dd.MM.yyyy";
            this.dtpDo.ValueChanged += this.dtpDo_ValueChanged;
            this.lblGodina.Name = "lblGodina";
            this.lblGodina.Location = new System.Drawing.Point(16, 158);
            this.lblGodina.Size = new System.Drawing.Size(400, 28);
            this.btnPotvrdi.Name = "btnPotvrdi"; this.btnPotvrdi.Text = "Potvrdi";
            this.btnPotvrdi.Location = new System.Drawing.Point(260, 205);
            this.btnPotvrdi.Size = new System.Drawing.Size(100, 30);
            this.btnPotvrdi.Click += this.btnPotvrdi_Click;
            this.btnOdustani.Name = "btnOdustani"; this.btnOdustani.Text = "Odustani";
            this.btnOdustani.Location = new System.Drawing.Point(370, 205);
            this.btnOdustani.Size = new System.Drawing.Size(100, 30);
            this.btnOdustani.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnOdustani.Click += this.btnOdustani_Click;
            this.Controls.Add(this.lblOpis); this.Controls.Add(this.lblOd); this.Controls.Add(this.lblDo);
            this.Controls.Add(this.dtpOd); this.Controls.Add(this.dtpDo); this.Controls.Add(this.lblGodina);
            this.Controls.Add(this.btnPotvrdi); this.Controls.Add(this.btnOdustani);
            this.AcceptButton = this.btnPotvrdi; this.CancelButton = this.btnOdustani;
            this.ClientSize = new System.Drawing.Size(500, 255);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false; this.MinimizeBox = false;
            this.Name = "PeriodCenovnika";
            this.Text = "Period važenja cenovnika";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);
        }
        private System.Windows.Forms.Label lblOpis, lblOd, lblDo, lblGodina;
        private System.Windows.Forms.DateTimePicker dtpOd, dtpDo;
        private System.Windows.Forms.Button btnPotvrdi, btnOdustani;
    }
}
