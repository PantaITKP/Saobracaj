﻿namespace Saobracaj.Dokumenta
{
    partial class frmPregledRID
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmPregledRID));
            this.dtpStvarnoPrimanje = new System.Windows.Forms.DateTimePicker();
            this.dtpPredvidjenoPrimanje = new System.Windows.Forms.DateTimePicker();
            this.label8 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.cboRID = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.button4 = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.button1 = new System.Windows.Forms.Button();
            this.dataGridView2 = new System.Windows.Forms.DataGridView();
            this.label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).BeginInit();
            this.SuspendLayout();
            // 
            // dtpStvarnoPrimanje
            // 
            this.dtpStvarnoPrimanje.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpStvarnoPrimanje.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpStvarnoPrimanje.Location = new System.Drawing.Point(286, 12);
            this.dtpStvarnoPrimanje.Name = "dtpStvarnoPrimanje";
            this.dtpStvarnoPrimanje.ShowUpDown = true;
            this.dtpStvarnoPrimanje.Size = new System.Drawing.Size(110, 20);
            this.dtpStvarnoPrimanje.TabIndex = 21;
            // 
            // dtpPredvidjenoPrimanje
            // 
            this.dtpPredvidjenoPrimanje.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpPredvidjenoPrimanje.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpPredvidjenoPrimanje.Location = new System.Drawing.Point(90, 12);
            this.dtpPredvidjenoPrimanje.Name = "dtpPredvidjenoPrimanje";
            this.dtpPredvidjenoPrimanje.ShowUpDown = true;
            this.dtpPredvidjenoPrimanje.Size = new System.Drawing.Size(110, 20);
            this.dtpPredvidjenoPrimanje.TabIndex = 20;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(12, 12);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(59, 13);
            this.label8.TabIndex = 39;
            this.label8.Text = "Datum od :";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(221, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 13);
            this.label1.TabIndex = 40;
            this.label1.Text = "Datum do :";
            // 
            // cboRID
            // 
            this.cboRID.FormattingEnabled = true;
            this.cboRID.Location = new System.Drawing.Point(491, 14);
            this.cboRID.Name = "cboRID";
            this.cboRID.Size = new System.Drawing.Size(141, 21);
            this.cboRID.TabIndex = 41;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(453, 14);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 13);
            this.label2.TabIndex = 42;
            this.label2.Text = "RID :";
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(402, 9);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(16, 23);
            this.button4.TabIndex = 100;
            this.button4.Text = "P";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(12, 41);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(876, 249);
            this.dataGridView1.TabIndex = 101;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(638, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(19, 23);
            this.button1.TabIndex = 102;
            this.button1.Text = "P";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // dataGridView2
            // 
            this.dataGridView2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView2.Location = new System.Drawing.Point(12, 320);
            this.dataGridView2.Name = "dataGridView2";
            this.dataGridView2.Size = new System.Drawing.Size(876, 206);
            this.dataGridView2.TabIndex = 103;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 304);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(145, 13);
            this.label3.TabIndex = 104;
            this.label3.Text = "Isključeni vagoni sa RID-om :";
            // 
            // frmPregledRID
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(893, 530);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.dataGridView2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cboRID);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.dtpStvarnoPrimanje);
            this.Controls.Add(this.dtpPredvidjenoPrimanje);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmPregledRID";
            this.Text = "Pregled RID";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DateTimePicker dtpStvarnoPrimanje;
        private System.Windows.Forms.DateTimePicker dtpPredvidjenoPrimanje;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cboRID;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.DataGridView dataGridView2;
        private System.Windows.Forms.Label label3;
    }
}