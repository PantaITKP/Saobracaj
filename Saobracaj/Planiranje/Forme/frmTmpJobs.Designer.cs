namespace Saobracaj.Planiranje.Forme
{
    partial class frmTmpJobs
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null) components.Dispose();
                if (kurziv != null) kurziv.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.SuspendLayout();
            //
            // frmTmpJobs
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1500, 900);
            this.Name = "frmTmpJobs";
            this.Text = "Priprema · CIM poslovi (test)";
            this.ResumeLayout(false);

        }

        #endregion
    }
}
