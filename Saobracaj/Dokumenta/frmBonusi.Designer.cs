namespace Saobracaj.Dokumenta
{
    partial class frmBonusi
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
            this.sqLiteCommand1 = new System.Data.SQLite.SQLiteCommand();
            this.txtSifra = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.cboZaposleni = new System.Windows.Forms.ComboBox();
            this.valKoeficijent = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.valIznos = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label8 = new System.Windows.Forms.Label();
            this.valKOefUkupni = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.valOsnovica = new System.Windows.Forms.NumericUpDown();
            this.metroButton5 = new MetroFramework.Controls.MetroButton();
            this.label3 = new System.Windows.Forms.Label();
            this.valIznosZaRAspodelu = new System.Windows.Forms.NumericUpDown();
            this.valBrojDanaTeren = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.metroButton1 = new MetroFramework.Controls.MetroButton();
            this.metroButton2 = new MetroFramework.Controls.MetroButton();
            this.label9 = new System.Windows.Forms.Label();
            this.valBruto = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.valKoeficijent)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valIznos)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.valKOefUkupni)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valOsnovica)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valIznosZaRAspodelu)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valBrojDanaTeren)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valBruto)).BeginInit();
            this.SuspendLayout();
            // 
            // sqLiteCommand1
            // 
            this.sqLiteCommand1.CommandText = null;
            // 
            // txtSifra
            // 
            this.txtSifra.Location = new System.Drawing.Point(72, 29);
            this.txtSifra.Name = "txtSifra";
            this.txtSifra.Size = new System.Drawing.Size(100, 20);
            this.txtSifra.TabIndex = 16;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(31, 13);
            this.label1.TabIndex = 17;
            this.label1.Text = "Šifra:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(10, 70);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(56, 13);
            this.label6.TabIndex = 92;
            this.label6.Text = "Zaposleni:";
            // 
            // cboZaposleni
            // 
            this.cboZaposleni.BackColor = System.Drawing.Color.White;
            this.cboZaposleni.FormattingEnabled = true;
            this.cboZaposleni.Location = new System.Drawing.Point(72, 70);
            this.cboZaposleni.Name = "cboZaposleni";
            this.cboZaposleni.Size = new System.Drawing.Size(171, 21);
            this.cboZaposleni.TabIndex = 91;
            // 
            // valKoeficijent
            // 
            this.valKoeficijent.DecimalPlaces = 2;
            this.valKoeficijent.Location = new System.Drawing.Point(78, 109);
            this.valKoeficijent.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.valKoeficijent.Name = "valKoeficijent";
            this.valKoeficijent.Size = new System.Drawing.Size(82, 20);
            this.valKoeficijent.TabIndex = 160;
            this.valKoeficijent.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(10, 109);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(62, 30);
            this.label5.TabIndex = 159;
            this.label5.Text = "Koeficijent:";
            // 
            // valIznos
            // 
            this.valIznos.DecimalPlaces = 2;
            this.valIznos.Location = new System.Drawing.Point(78, 149);
            this.valIznos.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.valIznos.Name = "valIznos";
            this.valIznos.Size = new System.Drawing.Size(82, 20);
            this.valIznos.TabIndex = 162;
            this.valIznos.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(10, 149);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 30);
            this.label2.TabIndex = 161;
            this.label2.Text = "Iznos radnik:";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label9);
            this.panel1.Controls.Add(this.valBruto);
            this.panel1.Controls.Add(this.label8);
            this.panel1.Controls.Add(this.valKOefUkupni);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.valOsnovica);
            this.panel1.Controls.Add(this.metroButton5);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.valIznosZaRAspodelu);
            this.panel1.Location = new System.Drawing.Point(628, 29);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(591, 127);
            this.panel1.TabIndex = 163;
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(289, 14);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(100, 30);
            this.label8.TabIndex = 176;
            this.label8.Text = "UkupanKoef";
            // 
            // valKOefUkupni
            // 
            this.valKOefUkupni.DecimalPlaces = 2;
            this.valKOefUkupni.Location = new System.Drawing.Point(395, 14);
            this.valKOefUkupni.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.valKOefUkupni.Name = "valKOefUkupni";
            this.valKOefUkupni.Size = new System.Drawing.Size(110, 20);
            this.valKOefUkupni.TabIndex = 175;
            this.valKOefUkupni.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(289, 82);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(100, 30);
            this.label4.TabIndex = 174;
            this.label4.Text = "valOsnovica";
            // 
            // valOsnovica
            // 
            this.valOsnovica.DecimalPlaces = 2;
            this.valOsnovica.Location = new System.Drawing.Point(395, 82);
            this.valOsnovica.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.valOsnovica.Name = "valOsnovica";
            this.valOsnovica.Size = new System.Drawing.Size(110, 20);
            this.valOsnovica.TabIndex = 173;
            this.valOsnovica.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // metroButton5
            // 
            this.metroButton5.Location = new System.Drawing.Point(15, 52);
            this.metroButton5.Name = "metroButton5";
            this.metroButton5.Size = new System.Drawing.Size(123, 23);
            this.metroButton5.TabIndex = 172;
            this.metroButton5.Text = "Izra;unaj osnovicu";
            this.metroButton5.UseSelectable = true;
            this.metroButton5.Click += new System.EventHandler(this.metroButton5_Click);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(3, 14);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 30);
            this.label3.TabIndex = 162;
            this.label3.Text = "Ukupan iznos za raspodelu:";
            // 
            // valIznosZaRAspodelu
            // 
            this.valIznosZaRAspodelu.DecimalPlaces = 2;
            this.valIznosZaRAspodelu.Location = new System.Drawing.Point(109, 14);
            this.valIznosZaRAspodelu.Maximum = new decimal(new int[] {
            100000000,
            0,
            0,
            0});
            this.valIznosZaRAspodelu.Name = "valIznosZaRAspodelu";
            this.valIznosZaRAspodelu.Size = new System.Drawing.Size(110, 20);
            this.valIznosZaRAspodelu.TabIndex = 161;
            this.valIznosZaRAspodelu.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // valBrojDanaTeren
            // 
            this.valBrojDanaTeren.DecimalPlaces = 2;
            this.valBrojDanaTeren.Location = new System.Drawing.Point(78, 194);
            this.valBrojDanaTeren.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.valBrojDanaTeren.Name = "valBrojDanaTeren";
            this.valBrojDanaTeren.Size = new System.Drawing.Size(82, 20);
            this.valBrojDanaTeren.TabIndex = 165;
            this.valBrojDanaTeren.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(10, 194);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(62, 30);
            this.label7.TabIndex = 164;
            this.label7.Text = "Broj dana teren:";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(13, 251);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.Size = new System.Drawing.Size(1229, 369);
            this.dataGridView1.TabIndex = 166;
            // 
            // metroButton1
            // 
            this.metroButton1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.metroButton1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.metroButton1.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.metroButton1.Location = new System.Drawing.Point(324, 40);
            this.metroButton1.Name = "metroButton1";
            this.metroButton1.Size = new System.Drawing.Size(123, 23);
            this.metroButton1.TabIndex = 173;
            this.metroButton1.Text = "Postavi 0";
            this.metroButton1.UseSelectable = true;
            this.metroButton1.Click += new System.EventHandler(this.metroButton1_Click);
            // 
            // metroButton2
            // 
            this.metroButton2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.metroButton2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.metroButton2.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.metroButton2.Location = new System.Drawing.Point(337, 81);
            this.metroButton2.Name = "metroButton2";
            this.metroButton2.Size = new System.Drawing.Size(123, 23);
            this.metroButton2.TabIndex = 174;
            this.metroButton2.Text = "Updejtuj Vrednosti";
            this.metroButton2.UseSelectable = true;
            this.metroButton2.Click += new System.EventHandler(this.metroButton2_Click);
            // 
            // label9
            // 
            this.label9.Location = new System.Drawing.Point(289, 42);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(100, 30);
            this.label9.TabIndex = 178;
            this.label9.Text = "valOsnovica";
            // 
            // valBruto
            // 
            this.valBruto.DecimalPlaces = 2;
            this.valBruto.Location = new System.Drawing.Point(395, 42);
            this.valBruto.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.valBruto.Name = "valBruto";
            this.valBruto.Size = new System.Drawing.Size(110, 20);
            this.valBruto.TabIndex = 177;
            this.valBruto.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // frmBonusi
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1254, 632);
            this.Controls.Add(this.metroButton2);
            this.Controls.Add(this.metroButton1);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.valBrojDanaTeren);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.valIznos);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.valKoeficijent);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.cboZaposleni);
            this.Controls.Add(this.txtSifra);
            this.Controls.Add(this.label1);
            this.Name = "frmBonusi";
            this.Text = "frmBonusi";
            this.Load += new System.EventHandler(this.frmBonusi_Load);
            ((System.ComponentModel.ISupportInitialize)(this.valKoeficijent)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valIznos)).EndInit();
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.valKOefUkupni)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valOsnovica)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valIznosZaRAspodelu)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valBrojDanaTeren)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valBruto)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Data.SQLite.SQLiteCommand sqLiteCommand1;
        private System.Windows.Forms.TextBox txtSifra;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cboZaposleni;
        private System.Windows.Forms.NumericUpDown valKoeficijent;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown valIznos;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown valIznosZaRAspodelu;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown valOsnovica;
        private MetroFramework.Controls.MetroButton metroButton5;
        private System.Windows.Forms.NumericUpDown valBrojDanaTeren;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.DataGridView dataGridView1;
        private MetroFramework.Controls.MetroButton metroButton1;
        private MetroFramework.Controls.MetroButton metroButton2;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown valKOefUkupni;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown valBruto;
    }
}