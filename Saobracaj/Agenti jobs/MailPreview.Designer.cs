namespace Saobracaj.Agenti_jobs
{
    partial class MailPreview
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.txtMail = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // txtMail
            // 
            this.txtMail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtMail.Font = new System.Drawing.Font("Calibri", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtMail.Location = new System.Drawing.Point(0, 0);
            this.txtMail.Name = "txtMail";
            this.txtMail.Size = new System.Drawing.Size(1080, 693);
            this.txtMail.TabIndex = 0;
            this.txtMail.Text = "";
            // 
            // MailPreview
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1080, 693);
            this.Controls.Add(this.txtMail);
            this.Name = "MailPreview";
            this.Text = "MailPreview";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox txtMail;
    }
}