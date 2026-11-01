namespace Saobracaj.Agenti_jobs
{
    partial class IzborStaniceCenovnika
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblUputstvo = new System.Windows.Forms.Label();
            this.txtPretraga = new System.Windows.Forms.TextBox();
            this.dgvIzbor = new System.Windows.Forms.DataGridView();
            this.panelDugmad = new System.Windows.Forms.FlowLayoutPanel();
            this.btnIzaberi = new System.Windows.Forms.Button();
            this.btnOdustani = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvIzbor)).BeginInit();
            this.panelDugmad.SuspendLayout();
            this.SuspendLayout();
            this.lblUputstvo.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblUputstvo.Height = 65;
            this.lblUputstvo.Padding = new System.Windows.Forms.Padding(8);
            this.lblUputstvo.Name = "lblUputstvo";
            this.txtPretraga.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtPretraga.Name = "txtPretraga";
            this.txtPretraga.TextChanged += this.txtPretraga_TextChanged;
            this.dgvIzbor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvIzbor.Name = "dgvIzbor";
            this.dgvIzbor.ReadOnly = true;
            this.dgvIzbor.AllowUserToAddRows = false;
            this.dgvIzbor.AllowUserToDeleteRows = false;
            this.dgvIzbor.MultiSelect = false;
            this.dgvIzbor.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvIzbor.CellDoubleClick += this.dgvIzbor_CellDoubleClick;
            this.panelDugmad.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelDugmad.Height = 45;
            this.panelDugmad.Padding = new System.Windows.Forms.Padding(6);
            this.panelDugmad.Name = "panelDugmad";
            this.panelDugmad.Controls.Add(this.btnIzaberi);
            this.panelDugmad.Controls.Add(this.btnOdustani);
            this.btnIzaberi.Name = "btnIzaberi";
            this.btnIzaberi.Text = "Izaberi";
            this.btnIzaberi.AutoSize = true;
            this.btnIzaberi.Click += this.btnIzaberi_Click;
            this.btnOdustani.Name = "btnOdustani";
            this.btnOdustani.Text = "Odustani";
            this.btnOdustani.AutoSize = true;
            this.btnOdustani.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnOdustani.Click += this.btnOdustani_Click;
            this.Controls.Add(this.dgvIzbor);
            this.Controls.Add(this.txtPretraga);
            this.Controls.Add(this.lblUputstvo);
            this.Controls.Add(this.panelDugmad);
            this.AcceptButton = this.btnIzaberi;
            this.CancelButton = this.btnOdustani;
            this.ClientSize = new System.Drawing.Size(850, 520);
            this.MinimumSize = new System.Drawing.Size(650, 400);
            this.Name = "IzborStaniceCenovnika";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            ((System.ComponentModel.ISupportInitialize)(this.dgvIzbor)).EndInit();
            this.panelDugmad.ResumeLayout(false);
            this.panelDugmad.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblUputstvo;
        private System.Windows.Forms.TextBox txtPretraga;
        private System.Windows.Forms.DataGridView dgvIzbor;
        private System.Windows.Forms.FlowLayoutPanel panelDugmad;
        private System.Windows.Forms.Button btnIzaberi;
        private System.Windows.Forms.Button btnOdustani;
    }
}
