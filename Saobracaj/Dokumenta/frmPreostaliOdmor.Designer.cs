
namespace Saobracaj.Dokumenta
{
    partial class frmPreostaliOdmor
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
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.cbo_Godina = new System.Windows.Forms.ComboBox();
            this.cbo_Zaposleni = new System.Windows.Forms.ComboBox();
            this.cbo_RM = new System.Windows.Forms.ComboBox();
            this.btn_godina = new System.Windows.Forms.Button();
            this.btn_zaposleni = new System.Windows.Forms.Button();
            this.btn_rm = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(12, 12);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.RowTemplate.Height = 24;
            this.dataGridView1.Size = new System.Drawing.Size(687, 731);
            this.dataGridView1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(712, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(119, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Pretrazi po godini";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(712, 77);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(157, 17);
            this.label2.TabIndex = 1;
            this.label2.Text = "Pretrazi po zaposlenom";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(712, 147);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(171, 17);
            this.label3.TabIndex = 1;
            this.label3.Text = "Pretrazi po radnom mestu";
            // 
            // cbo_Godina
            // 
            this.cbo_Godina.FormattingEnabled = true;
            this.cbo_Godina.Location = new System.Drawing.Point(715, 32);
            this.cbo_Godina.Name = "cbo_Godina";
            this.cbo_Godina.Size = new System.Drawing.Size(121, 24);
            this.cbo_Godina.TabIndex = 2;
            // 
            // cbo_Zaposleni
            // 
            this.cbo_Zaposleni.FormattingEnabled = true;
            this.cbo_Zaposleni.Location = new System.Drawing.Point(715, 101);
            this.cbo_Zaposleni.Name = "cbo_Zaposleni";
            this.cbo_Zaposleni.Size = new System.Drawing.Size(310, 24);
            this.cbo_Zaposleni.TabIndex = 2;
            // 
            // cbo_RM
            // 
            this.cbo_RM.FormattingEnabled = true;
            this.cbo_RM.Location = new System.Drawing.Point(715, 171);
            this.cbo_RM.Name = "cbo_RM";
            this.cbo_RM.Size = new System.Drawing.Size(310, 24);
            this.cbo_RM.TabIndex = 2;
            // 
            // btn_godina
            // 
            this.btn_godina.Location = new System.Drawing.Point(869, 32);
            this.btn_godina.Name = "btn_godina";
            this.btn_godina.Size = new System.Drawing.Size(44, 24);
            this.btn_godina.TabIndex = 3;
            this.btn_godina.Text = "?";
            this.btn_godina.UseVisualStyleBackColor = true;
            this.btn_godina.Click += new System.EventHandler(this.btn_godina_Click);
            // 
            // btn_zaposleni
            // 
            this.btn_zaposleni.Location = new System.Drawing.Point(1040, 101);
            this.btn_zaposleni.Name = "btn_zaposleni";
            this.btn_zaposleni.Size = new System.Drawing.Size(41, 24);
            this.btn_zaposleni.TabIndex = 3;
            this.btn_zaposleni.Text = "?";
            this.btn_zaposleni.UseVisualStyleBackColor = true;
            this.btn_zaposleni.Click += new System.EventHandler(this.btn_zaposleni_Click);
            // 
            // btn_rm
            // 
            this.btn_rm.Location = new System.Drawing.Point(1040, 171);
            this.btn_rm.Name = "btn_rm";
            this.btn_rm.Size = new System.Drawing.Size(41, 24);
            this.btn_rm.TabIndex = 3;
            this.btn_rm.Text = "?";
            this.btn_rm.UseVisualStyleBackColor = true;
            this.btn_rm.Click += new System.EventHandler(this.btn_rm_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(715, 221);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(140, 35);
            this.button1.TabIndex = 4;
            this.button1.Text = "Godina + RM";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // frmPreostaliOdmor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1103, 755);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btn_rm);
            this.Controls.Add(this.btn_zaposleni);
            this.Controls.Add(this.btn_godina);
            this.Controls.Add(this.cbo_RM);
            this.Controls.Add(this.cbo_Zaposleni);
            this.Controls.Add(this.cbo_Godina);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.dataGridView1);
            this.Name = "frmPreostaliOdmor";
            this.Text = "frmPreostaliOdmor";
            this.Load += new System.EventHandler(this.frmPreostaliOdmor_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cbo_Godina;
        private System.Windows.Forms.ComboBox cbo_Zaposleni;
        private System.Windows.Forms.ComboBox cbo_RM;
        private System.Windows.Forms.Button btn_godina;
        private System.Windows.Forms.Button btn_zaposleni;
        private System.Windows.Forms.Button btn_rm;
        private System.Windows.Forms.Button button1;
    }
}