
namespace Saobracaj.Dokumenta
{
    partial class frmEvidencijaGOSvi
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
            this.btnZaposleni = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.cboZaposleni = new System.Windows.Forms.ComboBox();
            this.dt_VremeOd = new System.Windows.Forms.DateTimePicker();
            this.dt_VremeDo = new System.Windows.Forms.DateTimePicker();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(12, 127);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.RowTemplate.Height = 24;
            this.dataGridView1.Size = new System.Drawing.Size(1572, 631);
            this.dataGridView1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 44);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Zaposleni";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(342, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 17);
            this.label2.TabIndex = 1;
            this.label2.Text = "Vreme OD";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(554, 40);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(74, 17);
            this.label3.TabIndex = 1;
            this.label3.Text = "Vreme DO";
            // 
            // btnZaposleni
            // 
            this.btnZaposleni.Location = new System.Drawing.Point(722, 49);
            this.btnZaposleni.Name = "btnZaposleni";
            this.btnZaposleni.Size = new System.Drawing.Size(233, 38);
            this.btnZaposleni.TabIndex = 2;
            this.btnZaposleni.Text = "Pretrazi po zaposlenom";
            this.btnZaposleni.UseVisualStyleBackColor = true;
            this.btnZaposleni.Click += new System.EventHandler(this.btnZaposleni_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(972, 49);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(233, 36);
            this.button2.TabIndex = 2;
            this.button2.Text = "Pretrazi po datumu";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(1238, 49);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(233, 36);
            this.button4.TabIndex = 2;
            this.button4.Text = "Obrisi filter";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // cboZaposleni
            // 
            this.cboZaposleni.FormattingEnabled = true;
            this.cboZaposleni.Location = new System.Drawing.Point(12, 65);
            this.cboZaposleni.Name = "cboZaposleni";
            this.cboZaposleni.Size = new System.Drawing.Size(213, 24);
            this.cboZaposleni.TabIndex = 3;
            // 
            // dt_VremeOd
            // 
            this.dt_VremeOd.CustomFormat = "dd.MM.yyyy";
            this.dt_VremeOd.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dt_VremeOd.Location = new System.Drawing.Point(310, 65);
            this.dt_VremeOd.Name = "dt_VremeOd";
            this.dt_VremeOd.Size = new System.Drawing.Size(158, 22);
            this.dt_VremeOd.TabIndex = 4;
            // 
            // dt_VremeDo
            // 
            this.dt_VremeDo.CustomFormat = "dd.MM.yyyy";
            this.dt_VremeDo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dt_VremeDo.Location = new System.Drawing.Point(518, 63);
            this.dt_VremeDo.Name = "dt_VremeDo";
            this.dt_VremeDo.Size = new System.Drawing.Size(158, 22);
            this.dt_VremeDo.TabIndex = 4;
            // 
            // frmEvidencijaGOSvi
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1596, 770);
            this.Controls.Add(this.dt_VremeDo);
            this.Controls.Add(this.dt_VremeOd);
            this.Controls.Add(this.cboZaposleni);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.btnZaposleni);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.dataGridView1);
            this.Name = "frmEvidencijaGOSvi";
            this.Text = "frmEvidencijaGOSvi";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnZaposleni;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.ComboBox cboZaposleni;
        private System.Windows.Forms.DateTimePicker dt_VremeOd;
        private System.Windows.Forms.DateTimePicker dt_VremeDo;
    }
}