﻿namespace Saobracaj.Dokumenta
{
    partial class frmPronadjiVagon
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmPronadjiVagon));
            this.txtSifra = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.button4 = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.dataGridView2 = new System.Windows.Forms.DataGridView();
            this.label26 = new System.Windows.Forms.Label();
            this.cboPlatilac = new System.Windows.Forms.ComboBox();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).BeginInit();
            this.SuspendLayout();
            // 
            // txtSifra
            // 
            this.txtSifra.Location = new System.Drawing.Point(49, 12);
            this.txtSifra.Name = "txtSifra";
            this.txtSifra.Size = new System.Drawing.Size(147, 20);
            this.txtSifra.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(31, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Šifra:";
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(202, 10);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(187, 23);
            this.button4.TabIndex = 100;
            this.button4.Text = "Pronađi bilo gde";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(12, 66);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(876, 184);
            this.dataGridView1.TabIndex = 102;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 50);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(128, 13);
            this.label2.TabIndex = 103;
            this.label2.Text = "Na prijemnim teretnicama:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 262);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(75, 13);
            this.label3.TabIndex = 105;
            this.label3.Text = "Na prevozima:";
            // 
            // dataGridView2
            // 
            this.dataGridView2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView2.Location = new System.Drawing.Point(12, 278);
            this.dataGridView2.Name = "dataGridView2";
            this.dataGridView2.Size = new System.Drawing.Size(876, 273);
            this.dataGridView2.TabIndex = 104;
            this.dataGridView2.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView2_CellContentClick);
            // 
            // label26
            // 
            this.label26.AutoSize = true;
            this.label26.Location = new System.Drawing.Point(702, 14);
            this.label26.Name = "label26";
            this.label26.Size = new System.Drawing.Size(44, 13);
            this.label26.TabIndex = 107;
            this.label26.Text = "Platilac:";
            // 
            // cboPlatilac
            // 
            this.cboPlatilac.FormattingEnabled = true;
            this.cboPlatilac.Location = new System.Drawing.Point(774, 12);
            this.cboPlatilac.Name = "cboPlatilac";
            this.cboPlatilac.Size = new System.Drawing.Size(114, 21);
            this.cboPlatilac.TabIndex = 106;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(409, 10);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(179, 23);
            this.button1.TabIndex = 108;
            this.button1.Text = "Pronađi samo u 9";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // frmPronadjiVagon
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 563);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label26);
            this.Controls.Add(this.cboPlatilac);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.dataGridView2);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.txtSifra);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmPronadjiVagon";
            this.Text = "Pronađi vagon";
            this.Load += new System.EventHandler(this.frmPronadjiVagon_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtSifra;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DataGridView dataGridView2;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.ComboBox cboPlatilac;
        private System.Windows.Forms.Button button1;
    }
}