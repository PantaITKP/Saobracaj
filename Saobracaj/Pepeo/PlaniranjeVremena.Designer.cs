namespace Saobracaj.Pepeo
{
    partial class PlaniranjeVremena
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
            this.label1 = new System.Windows.Forms.Label();
            this.cboTrasa = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.dtpPocetakPrevoza = new System.Windows.Forms.DateTimePicker();
            this.label3 = new System.Windows.Forms.Label();
            this.txtBrojCiklusa = new System.Windows.Forms.TextBox();
            this.btnGenerisiPlan = new System.Windows.Forms.Button();
            this.btnUcitajPlan = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.dtpKasnjenje = new System.Windows.Forms.DateTimePicker();
            this.txtNapomena = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.btnPotvrdiKasnjenje = new System.Windows.Forms.Button();
            this.btnZavrsiCiklus = new System.Windows.Forms.Button();
            this.dgvVremena = new System.Windows.Forms.DataGridView();
            this.cboCiklus = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.cboNajava = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.btnPoveziNajavu = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.cboKasnjenje = new System.Windows.Forms.ComboBox();
            this.tabCiklusi = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.btnAktivniPrevozi = new System.Windows.Forms.Button();
            this.btnZavrseniPrevozi = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.btnPomeraj = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvVremena)).BeginInit();
            this.tabCiklusi.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Trasa";
            // 
            // cboTrasa
            // 
            this.cboTrasa.FormattingEnabled = true;
            this.cboTrasa.Location = new System.Drawing.Point(12, 25);
            this.cboTrasa.Name = "cboTrasa";
            this.cboTrasa.Size = new System.Drawing.Size(383, 21);
            this.cboTrasa.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 62);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(88, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Početak prevoza";
            // 
            // dtpPocetakPrevoza
            // 
            this.dtpPocetakPrevoza.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpPocetakPrevoza.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpPocetakPrevoza.Location = new System.Drawing.Point(12, 79);
            this.dtpPocetakPrevoza.Name = "dtpPocetakPrevoza";
            this.dtpPocetakPrevoza.Size = new System.Drawing.Size(136, 20);
            this.dtpPocetakPrevoza.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(157, 62);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(44, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Ciklusa:";
            // 
            // txtBrojCiklusa
            // 
            this.txtBrojCiklusa.Location = new System.Drawing.Point(154, 79);
            this.txtBrojCiklusa.Name = "txtBrojCiklusa";
            this.txtBrojCiklusa.Size = new System.Drawing.Size(47, 20);
            this.txtBrojCiklusa.TabIndex = 3;
            // 
            // btnGenerisiPlan
            // 
            this.btnGenerisiPlan.Location = new System.Drawing.Point(207, 62);
            this.btnGenerisiPlan.Name = "btnGenerisiPlan";
            this.btnGenerisiPlan.Size = new System.Drawing.Size(96, 38);
            this.btnGenerisiPlan.TabIndex = 4;
            this.btnGenerisiPlan.Text = "Generiši plan";
            this.btnGenerisiPlan.UseVisualStyleBackColor = true;
            // 
            // btnUcitajPlan
            // 
            this.btnUcitajPlan.Location = new System.Drawing.Point(324, 61);
            this.btnUcitajPlan.Name = "btnUcitajPlan";
            this.btnUcitajPlan.Size = new System.Drawing.Size(89, 38);
            this.btnUcitajPlan.TabIndex = 4;
            this.btnUcitajPlan.Text = "Učitaj plan";
            this.btnUcitajPlan.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(435, 9);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(53, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "Kašnjenje";
            // 
            // dtpKasnjenje
            // 
            this.dtpKasnjenje.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpKasnjenje.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpKasnjenje.Location = new System.Drawing.Point(438, 22);
            this.dtpKasnjenje.Name = "dtpKasnjenje";
            this.dtpKasnjenje.Size = new System.Drawing.Size(136, 20);
            this.dtpKasnjenje.TabIndex = 2;
            // 
            // txtNapomena
            // 
            this.txtNapomena.Location = new System.Drawing.Point(438, 62);
            this.txtNapomena.Multiline = true;
            this.txtNapomena.Name = "txtNapomena";
            this.txtNapomena.Size = new System.Drawing.Size(356, 38);
            this.txtNapomena.TabIndex = 3;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(435, 45);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(62, 13);
            this.label5.TabIndex = 0;
            this.label5.Text = "Napomena:";
            // 
            // btnPotvrdiKasnjenje
            // 
            this.btnPotvrdiKasnjenje.Location = new System.Drawing.Point(810, 8);
            this.btnPotvrdiKasnjenje.Name = "btnPotvrdiKasnjenje";
            this.btnPotvrdiKasnjenje.Size = new System.Drawing.Size(96, 38);
            this.btnPotvrdiKasnjenje.TabIndex = 4;
            this.btnPotvrdiKasnjenje.Text = "Potvrdi kasnjenje";
            this.btnPotvrdiKasnjenje.UseVisualStyleBackColor = true;
            // 
            // btnZavrsiCiklus
            // 
            this.btnZavrsiCiklus.Location = new System.Drawing.Point(1133, 55);
            this.btnZavrsiCiklus.Name = "btnZavrsiCiklus";
            this.btnZavrsiCiklus.Size = new System.Drawing.Size(96, 35);
            this.btnZavrsiCiklus.TabIndex = 4;
            this.btnZavrsiCiklus.Text = "Završi prevoz";
            this.btnZavrsiCiklus.UseVisualStyleBackColor = true;
            // 
            // dgvVremena
            // 
            this.dgvVremena.AllowUserToAddRows = false;
            this.dgvVremena.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvVremena.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvVremena.Location = new System.Drawing.Point(1235, 9);
            this.dgvVremena.Name = "dgvVremena";
            this.dgvVremena.Size = new System.Drawing.Size(342, 151);
            this.dgvVremena.TabIndex = 5;
            // 
            // cboCiklus
            // 
            this.cboCiklus.FormattingEnabled = true;
            this.cboCiklus.Location = new System.Drawing.Point(949, 25);
            this.cboCiklus.Name = "cboCiklus";
            this.cboCiklus.Size = new System.Drawing.Size(76, 21);
            this.cboCiklus.TabIndex = 1;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(946, 9);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(38, 13);
            this.label6.TabIndex = 0;
            this.label6.Text = "Ciklus:";
            // 
            // cboNajava
            // 
            this.cboNajava.FormattingEnabled = true;
            this.cboNajava.Location = new System.Drawing.Point(1031, 25);
            this.cboNajava.Name = "cboNajava";
            this.cboNajava.Size = new System.Drawing.Size(198, 21);
            this.cboNajava.TabIndex = 1;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(1031, 9);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(44, 13);
            this.label7.TabIndex = 0;
            this.label7.Text = "Najava:";
            // 
            // btnPoveziNajavu
            // 
            this.btnPoveziNajavu.Location = new System.Drawing.Point(949, 52);
            this.btnPoveziNajavu.Name = "btnPoveziNajavu";
            this.btnPoveziNajavu.Size = new System.Drawing.Size(96, 38);
            this.btnPoveziNajavu.TabIndex = 4;
            this.btnPoveziNajavu.Text = "Povezi najavu";
            this.btnPoveziNajavu.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(595, 9);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(88, 13);
            this.label8.TabIndex = 0;
            this.label8.Text = "Razlog kašnjenja";
            // 
            // cboKasnjenje
            // 
            this.cboKasnjenje.FormattingEnabled = true;
            this.cboKasnjenje.Location = new System.Drawing.Point(598, 31);
            this.cboKasnjenje.Name = "cboKasnjenje";
            this.cboKasnjenje.Size = new System.Drawing.Size(197, 21);
            this.cboKasnjenje.TabIndex = 1;
            // 
            // tabCiklusi
            // 
            this.tabCiklusi.Alignment = System.Windows.Forms.TabAlignment.Bottom;
            this.tabCiklusi.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabCiklusi.Controls.Add(this.tabPage1);
            this.tabCiklusi.Controls.Add(this.tabPage2);
            this.tabCiklusi.Location = new System.Drawing.Point(12, 166);
            this.tabCiklusi.Multiline = true;
            this.tabCiklusi.Name = "tabCiklusi";
            this.tabCiklusi.SelectedIndex = 0;
            this.tabCiklusi.Size = new System.Drawing.Size(1569, 421);
            this.tabCiklusi.TabIndex = 6;
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(4, 4);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1561, 395);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 4);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1561, 395);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // btnAktivniPrevozi
            // 
            this.btnAktivniPrevozi.Location = new System.Drawing.Point(12, 122);
            this.btnAktivniPrevozi.Name = "btnAktivniPrevozi";
            this.btnAktivniPrevozi.Size = new System.Drawing.Size(96, 38);
            this.btnAktivniPrevozi.TabIndex = 4;
            this.btnAktivniPrevozi.Text = "Aktivni";
            this.btnAktivniPrevozi.UseVisualStyleBackColor = true;
            // 
            // btnZavrseniPrevozi
            // 
            this.btnZavrseniPrevozi.Location = new System.Drawing.Point(114, 122);
            this.btnZavrseniPrevozi.Name = "btnZavrseniPrevozi";
            this.btnZavrseniPrevozi.Size = new System.Drawing.Size(96, 38);
            this.btnZavrseniPrevozi.TabIndex = 4;
            this.btnZavrseniPrevozi.Text = "Završeni";
            this.btnZavrseniPrevozi.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(435, 113);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(122, 13);
            this.label9.TabIndex = 0;
            this.label9.Text = "Pomeri početak prevoza";
            // 
            // dateTimePicker1
            // 
            this.dateTimePicker1.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePicker1.Location = new System.Drawing.Point(438, 129);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.Size = new System.Drawing.Size(136, 20);
            this.dateTimePicker1.TabIndex = 2;
            // 
            // btnPomeraj
            // 
            this.btnPomeraj.Location = new System.Drawing.Point(587, 113);
            this.btnPomeraj.Name = "btnPomeraj";
            this.btnPomeraj.Size = new System.Drawing.Size(96, 38);
            this.btnPomeraj.TabIndex = 4;
            this.btnPomeraj.Text = "Potvrdi pomeraj";
            this.btnPomeraj.UseVisualStyleBackColor = true;
            // 
            // PlaniranjeVremena
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1593, 599);
            this.Controls.Add(this.tabCiklusi);
            this.Controls.Add(this.dgvVremena);
            this.Controls.Add(this.btnUcitajPlan);
            this.Controls.Add(this.btnZavrsiCiklus);
            this.Controls.Add(this.btnPoveziNajavu);
            this.Controls.Add(this.btnPomeraj);
            this.Controls.Add(this.btnPotvrdiKasnjenje);
            this.Controls.Add(this.btnZavrseniPrevozi);
            this.Controls.Add(this.btnAktivniPrevozi);
            this.Controls.Add(this.btnGenerisiPlan);
            this.Controls.Add(this.txtNapomena);
            this.Controls.Add(this.txtBrojCiklusa);
            this.Controls.Add(this.dateTimePicker1);
            this.Controls.Add(this.dtpKasnjenje);
            this.Controls.Add(this.dtpPocetakPrevoza);
            this.Controls.Add(this.cboNajava);
            this.Controls.Add(this.cboKasnjenje);
            this.Controls.Add(this.cboCiklus);
            this.Controls.Add(this.cboTrasa);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "PlaniranjeVremena";
            this.Text = "PlaniranjeVremena";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.PlaniranjeVremena_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvVremena)).EndInit();
            this.tabCiklusi.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cboTrasa;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DateTimePicker dtpPocetakPrevoza;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtBrojCiklusa;
        private System.Windows.Forms.Button btnGenerisiPlan;
        private System.Windows.Forms.Button btnUcitajPlan;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.DateTimePicker dtpKasnjenje;
        private System.Windows.Forms.TextBox txtNapomena;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnPotvrdiKasnjenje;
        private System.Windows.Forms.Button btnZavrsiCiklus;
        private System.Windows.Forms.DataGridView dgvVremena;
        private System.Windows.Forms.ComboBox cboCiklus;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cboNajava;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button btnPoveziNajavu;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox cboKasnjenje;
        private System.Windows.Forms.TabControl tabCiklusi;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button btnAktivniPrevozi;
        private System.Windows.Forms.Button btnZavrseniPrevozi;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.DateTimePicker dateTimePicker1;
        private System.Windows.Forms.Button btnPomeraj;
    }
}