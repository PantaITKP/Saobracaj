
namespace Saobracaj.Dokumenta
{
    partial class frmArhivirajZapise
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
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtPrvi = new System.Windows.Forms.TextBox();
            this.txt_Poslednji = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 156);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(177, 43);
            this.button1.TabIndex = 0;
            this.button1.Text = "Arhiviraj";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(40, 21);
            this.label1.TabIndex = 1;
            this.label1.Text = "Prvi";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(284, 25);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(81, 21);
            this.label2.TabIndex = 1;
            this.label2.Text = "Poslednji";
            // 
            // txtPrvi
            // 
            this.txtPrvi.Location = new System.Drawing.Point(12, 49);
            this.txtPrvi.Name = "txtPrvi";
            this.txtPrvi.Size = new System.Drawing.Size(100, 22);
            this.txtPrvi.TabIndex = 2;
            // 
            // txt_Poslednji
            // 
            this.txt_Poslednji.Location = new System.Drawing.Point(265, 49);
            this.txt_Poslednji.Name = "txt_Poslednji";
            this.txt_Poslednji.Size = new System.Drawing.Size(100, 22);
            this.txt_Poslednji.TabIndex = 2;
            // 
            // frmArhivirajZapise
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(507, 232);
            this.Controls.Add(this.txt_Poslednji);
            this.Controls.Add(this.txtPrvi);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Name = "frmArhivirajZapise";
            this.Text = "frmArhivirajZapise";
            this.Load += new System.EventHandler(this.frmArhivirajZapise_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtPrvi;
        private System.Windows.Forms.TextBox txt_Poslednji;
    }
}