﻿namespace Saobracaj.Dokumenta
{
    partial class frmMUPDozvola
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMUPDozvola));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsNew = new System.Windows.Forms.ToolStripButton();
            this.tsSave = new System.Windows.Forms.ToolStripButton();
            this.tsDelete = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsPrvi = new System.Windows.Forms.ToolStripButton();
            this.tsNazad = new System.Windows.Forms.ToolStripButton();
            this.tsNapred = new System.Windows.Forms.ToolStripButton();
            this.tsPoslednja = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.dtpVremeDo = new System.Windows.Forms.DateTimePicker();
            this.label15 = new System.Windows.Forms.Label();
            this.dtpVremeOd = new System.Windows.Forms.DateTimePicker();
            this.label21 = new System.Windows.Forms.Label();
            this.cboZaposleni = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtSifra = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.txtEmail = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtJMBG = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtRadnoMesto = new System.Windows.Forms.TextBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.reportViewer1 = new Microsoft.Reporting.WinForms.ReportViewer();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.btnPosaljiMail = new System.Windows.Forms.Button();
            this.btnStampa = new System.Windows.Forms.Button();
            this.cboLokacija = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.txtAdresa = new System.Windows.Forms.TextBox();
            this.chkSluzbeni = new System.Windows.Forms.CheckBox();
            this.cboAutomobili = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.chkStan = new System.Windows.Forms.CheckBox();
            this.txtRelacija = new System.Windows.Forms.ComboBox();
            this.toolStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsNew,
            this.tsSave,
            this.tsDelete,
            this.toolStripSeparator1,
            this.tsPrvi,
            this.tsNazad,
            this.tsNapred,
            this.tsPoslednja,
            this.toolStripButton2,
            this.toolStripButton1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(921, 25);
            this.toolStrip1.TabIndex = 142;
            this.toolStrip1.Text = "Osveži";
            // 
            // tsNew
            // 
            this.tsNew.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsNew.Image = ((System.Drawing.Image)(resources.GetObject("tsNew.Image")));
            this.tsNew.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsNew.Name = "tsNew";
            this.tsNew.Size = new System.Drawing.Size(23, 22);
            this.tsNew.Text = "Novi";
            this.tsNew.Click += new System.EventHandler(this.tsNew_Click);
            // 
            // tsSave
            // 
            this.tsSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsSave.Image = ((System.Drawing.Image)(resources.GetObject("tsSave.Image")));
            this.tsSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsSave.Name = "tsSave";
            this.tsSave.Size = new System.Drawing.Size(23, 22);
            this.tsSave.Text = "tsSave";
            this.tsSave.Click += new System.EventHandler(this.tsSave_Click);
            // 
            // tsDelete
            // 
            this.tsDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsDelete.Image = ((System.Drawing.Image)(resources.GetObject("tsDelete.Image")));
            this.tsDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDelete.Name = "tsDelete";
            this.tsDelete.Size = new System.Drawing.Size(23, 22);
            this.tsDelete.Text = "toolStripButton1";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // tsPrvi
            // 
            this.tsPrvi.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsPrvi.Image = ((System.Drawing.Image)(resources.GetObject("tsPrvi.Image")));
            this.tsPrvi.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsPrvi.Name = "tsPrvi";
            this.tsPrvi.Size = new System.Drawing.Size(23, 22);
            this.tsPrvi.Text = "toolStripButton1";
            // 
            // tsNazad
            // 
            this.tsNazad.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsNazad.Image = ((System.Drawing.Image)(resources.GetObject("tsNazad.Image")));
            this.tsNazad.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsNazad.Name = "tsNazad";
            this.tsNazad.Size = new System.Drawing.Size(23, 22);
            this.tsNazad.Text = "toolStripButton1";
            // 
            // tsNapred
            // 
            this.tsNapred.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsNapred.Image = ((System.Drawing.Image)(resources.GetObject("tsNapred.Image")));
            this.tsNapred.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsNapred.Name = "tsNapred";
            this.tsNapred.Size = new System.Drawing.Size(23, 22);
            this.tsNapred.Text = "toolStripButton1";
            // 
            // tsPoslednja
            // 
            this.tsPoslednja.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsPoslednja.Image = ((System.Drawing.Image)(resources.GetObject("tsPoslednja.Image")));
            this.tsPoslednja.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsPoslednja.Name = "tsPoslednja";
            this.tsPoslednja.Size = new System.Drawing.Size(23, 22);
            this.tsPoslednja.Text = "toolStripButton1";
            // 
            // toolStripButton2
            // 
            this.toolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton2.Image")));
            this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton2.Name = "toolStripButton2";
            this.toolStripButton2.Size = new System.Drawing.Size(96, 22);
            this.toolStripButton2.Text = "Pošalji e mail";
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(105, 22);
            this.toolStripButton1.Text = "Štampa aktivnosti";
            // 
            // dtpVremeDo
            // 
            this.dtpVremeDo.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpVremeDo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeDo.Location = new System.Drawing.Point(625, 39);
            this.dtpVremeDo.Name = "dtpVremeDo";
            this.dtpVremeDo.ShowUpDown = true;
            this.dtpVremeDo.Size = new System.Drawing.Size(115, 20);
            this.dtpVremeDo.TabIndex = 171;
            this.dtpVremeDo.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(564, 40);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(55, 13);
            this.label15.TabIndex = 173;
            this.label15.Text = "Period do:";
            // 
            // dtpVremeOd
            // 
            this.dtpVremeOd.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpVremeOd.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeOd.Location = new System.Drawing.Point(450, 40);
            this.dtpVremeOd.Name = "dtpVremeOd";
            this.dtpVremeOd.ShowUpDown = true;
            this.dtpVremeOd.Size = new System.Drawing.Size(108, 20);
            this.dtpVremeOd.TabIndex = 170;
            this.dtpVremeOd.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(389, 40);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(55, 13);
            this.label21.TabIndex = 172;
            this.label21.Text = "Period od:";
            // 
            // cboZaposleni
            // 
            this.cboZaposleni.FormattingEnabled = true;
            this.cboZaposleni.Location = new System.Drawing.Point(219, 41);
            this.cboZaposleni.Name = "cboZaposleni";
            this.cboZaposleni.Size = new System.Drawing.Size(159, 21);
            this.cboZaposleni.TabIndex = 169;
            this.cboZaposleni.Leave += new System.EventHandler(this.cboZaposleni_Leave);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(153, 42);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(56, 13);
            this.label4.TabIndex = 168;
            this.label4.Text = "Zaposleni:";
            // 
            // txtSifra
            // 
            this.txtSifra.Location = new System.Drawing.Point(96, 42);
            this.txtSifra.Name = "txtSifra";
            this.txtSifra.Size = new System.Drawing.Size(52, 20);
            this.txtSifra.TabIndex = 166;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(26, 42);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(64, 13);
            this.label1.TabIndex = 167;
            this.label1.Text = "Šifra zapisa:";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(315, 118);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(35, 13);
            this.label18.TabIndex = 195;
            this.label18.Text = "Email:";
            // 
            // txtEmail
            // 
            this.txtEmail.Location = new System.Drawing.Point(318, 138);
            this.txtEmail.Name = "txtEmail";
            this.txtEmail.Size = new System.Drawing.Size(204, 20);
            this.txtEmail.TabIndex = 194;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(26, 175);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(153, 13);
            this.label7.TabIndex = 193;
            this.label7.Text = "Prebivalište / Trenutna adresa:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(26, 70);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(39, 13);
            this.label2.TabIndex = 197;
            this.label2.Text = "JMBG:";
            // 
            // txtJMBG
            // 
            this.txtJMBG.Location = new System.Drawing.Point(29, 86);
            this.txtJMBG.Name = "txtJMBG";
            this.txtJMBG.Size = new System.Drawing.Size(248, 20);
            this.txtJMBG.TabIndex = 196;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(315, 67);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(73, 13);
            this.label3.TabIndex = 199;
            this.label3.Text = "Radno mesto:";
            // 
            // txtRadnoMesto
            // 
            this.txtRadnoMesto.Location = new System.Drawing.Point(318, 86);
            this.txtRadnoMesto.Name = "txtRadnoMesto";
            this.txtRadnoMesto.Size = new System.Drawing.Size(204, 20);
            this.txtRadnoMesto.TabIndex = 198;
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(12, 297);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(897, 360);
            this.tabControl1.TabIndex = 200;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.reportViewer1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(889, 334);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Pregled";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // reportViewer1
            // 
            this.reportViewer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.reportViewer1.LocalReport.ReportEmbeddedResource = "Saobracaj.Dokumenta.Troskovi.rdlc";
            this.reportViewer1.Location = new System.Drawing.Point(6, 6);
            this.reportViewer1.Name = "reportViewer1";
            this.reportViewer1.Size = new System.Drawing.Size(877, 322);
            this.reportViewer1.TabIndex = 115;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.dataGridView1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(825, 350);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Sve dozvole";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(6, 6);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(813, 338);
            this.dataGridView1.TabIndex = 104;
            // 
            // btnPosaljiMail
            // 
            this.btnPosaljiMail.Location = new System.Drawing.Point(318, 164);
            this.btnPosaljiMail.Name = "btnPosaljiMail";
            this.btnPosaljiMail.Size = new System.Drawing.Size(153, 24);
            this.btnPosaljiMail.TabIndex = 201;
            this.btnPosaljiMail.Text = "Pošalji mail";
            this.btnPosaljiMail.UseVisualStyleBackColor = true;
            this.btnPosaljiMail.Click += new System.EventHandler(this.btnPosaljiMail_Click);
            // 
            // btnStampa
            // 
            this.btnStampa.Location = new System.Drawing.Point(29, 251);
            this.btnStampa.Name = "btnStampa";
            this.btnStampa.Size = new System.Drawing.Size(194, 23);
            this.btnStampa.TabIndex = 202;
            this.btnStampa.Text = "Štampaj";
            this.btnStampa.UseVisualStyleBackColor = true;
            this.btnStampa.Click += new System.EventHandler(this.btnStampa_Click);
            // 
            // cboLokacija
            // 
            this.cboLokacija.FormattingEnabled = true;
            this.cboLokacija.Items.AddRange(new object[] {
            "Firma",
            "Zrenjanin Šinvoz",
            "Pruga",
            "Skladište",
            "HBIS"});
            this.cboLokacija.Location = new System.Drawing.Point(625, 85);
            this.cboLokacija.Name = "cboLokacija";
            this.cboLokacija.Size = new System.Drawing.Size(159, 21);
            this.cboLokacija.TabIndex = 204;
            this.cboLokacija.SelectedIndexChanged += new System.EventHandler(this.cboLokacija_SelectedIndexChanged);
            this.cboLokacija.SelectedValueChanged += new System.EventHandler(this.comboBox1_SelectedValueChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(559, 86);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(50, 13);
            this.label5.TabIndex = 203;
            this.label5.Text = "Lokacija:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(559, 121);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(43, 13);
            this.label6.TabIndex = 205;
            this.label6.Text = "Adresa:";
            // 
            // txtAdresa
            // 
            this.txtAdresa.Location = new System.Drawing.Point(625, 118);
            this.txtAdresa.Multiline = true;
            this.txtAdresa.Name = "txtAdresa";
            this.txtAdresa.Size = new System.Drawing.Size(253, 55);
            this.txtAdresa.TabIndex = 206;
            // 
            // chkSluzbeni
            // 
            this.chkSluzbeni.AutoSize = true;
            this.chkSluzbeni.Location = new System.Drawing.Point(553, 189);
            this.chkSluzbeni.Name = "chkSluzbeni";
            this.chkSluzbeni.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkSluzbeni.Size = new System.Drawing.Size(66, 17);
            this.chkSluzbeni.TabIndex = 207;
            this.chkSluzbeni.Text = "Službeni";
            this.chkSluzbeni.UseVisualStyleBackColor = true;
            this.chkSluzbeni.CheckedChanged += new System.EventHandler(this.chkSluzbeni_CheckedChanged);
            // 
            // cboAutomobili
            // 
            this.cboAutomobili.FormattingEnabled = true;
            this.cboAutomobili.Location = new System.Drawing.Point(625, 228);
            this.cboAutomobili.Name = "cboAutomobili";
            this.cboAutomobili.Size = new System.Drawing.Size(239, 21);
            this.cboAutomobili.TabIndex = 209;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(559, 228);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(56, 13);
            this.label8.TabIndex = 208;
            this.label8.Text = "Automobil:";
            // 
            // chkStan
            // 
            this.chkStan.AutoSize = true;
            this.chkStan.Location = new System.Drawing.Point(29, 141);
            this.chkStan.Name = "chkStan";
            this.chkStan.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkStan.Size = new System.Drawing.Size(48, 17);
            this.chkStan.TabIndex = 210;
            this.chkStan.Text = "Stan";
            this.chkStan.UseVisualStyleBackColor = true;
            this.chkStan.CheckedChanged += new System.EventHandler(this.chkStan_CheckedChanged);
            // 
            // txtRelacija
            // 
            this.txtRelacija.FormattingEnabled = true;
            this.txtRelacija.Location = new System.Drawing.Point(29, 200);
            this.txtRelacija.Name = "txtRelacija";
            this.txtRelacija.Size = new System.Drawing.Size(229, 21);
            this.txtRelacija.TabIndex = 211;
            // 
            // frmMUPDozvola
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(921, 682);
            this.Controls.Add(this.txtRelacija);
            this.Controls.Add(this.chkStan);
            this.Controls.Add(this.cboAutomobili);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.chkSluzbeni);
            this.Controls.Add(this.txtAdresa);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.cboLokacija);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.btnStampa);
            this.Controls.Add(this.btnPosaljiMail);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtRadnoMesto);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtJMBG);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.txtEmail);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.dtpVremeDo);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.dtpVremeOd);
            this.Controls.Add(this.label21);
            this.Controls.Add(this.cboZaposleni);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtSifra);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "frmMUPDozvola";
            this.Text = "MUP Dozvola";
            this.Load += new System.EventHandler(this.frmMUPDozvola_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsNew;
        private System.Windows.Forms.ToolStripButton tsSave;
        private System.Windows.Forms.ToolStripButton tsDelete;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton tsPrvi;
        private System.Windows.Forms.ToolStripButton tsNazad;
        private System.Windows.Forms.ToolStripButton tsNapred;
        private System.Windows.Forms.ToolStripButton tsPoslednja;
        private System.Windows.Forms.ToolStripButton toolStripButton2;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.DateTimePicker dtpVremeDo;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.DateTimePicker dtpVremeOd;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.ComboBox cboZaposleni;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtSifra;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox txtEmail;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtJMBG;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtRadnoMesto;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private Microsoft.Reporting.WinForms.ReportViewer reportViewer1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btnPosaljiMail;
        private System.Windows.Forms.Button btnStampa;
        private System.Windows.Forms.ComboBox cboLokacija;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtAdresa;
        private System.Windows.Forms.CheckBox chkSluzbeni;
        private System.Windows.Forms.ComboBox cboAutomobili;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox chkStan;
        private System.Windows.Forms.ComboBox txtRelacija;
    }
}