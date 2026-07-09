
namespace Saobracaj.Administracija
{
    partial class frmNotifikacije
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmNotifikacije));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsNew = new System.Windows.Forms.ToolStripButton();
            this.tsSave = new System.Windows.Forms.ToolStripButton();
            this.tsDelete = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.label1 = new System.Windows.Forms.Label();
            this.txt_Obavestenje = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cbList_Korisnici = new System.Windows.Forms.CheckedListBox();
            this.cb_Procitan = new System.Windows.Forms.CheckBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.dt_Slanje = new System.Windows.Forms.DateTimePicker();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.dt_Citanje = new System.Windows.Forms.DateTimePicker();
            this.label5 = new System.Windows.Forms.Label();
            this.txt_ID = new System.Windows.Forms.TextBox();
            this.combo_RadnoMesto = new System.Windows.Forms.ComboBox();
            this.button1 = new System.Windows.Forms.Button();
            this.btnOsvezi = new System.Windows.Forms.Button();
            this.cboTipNotifikacije = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.btn_Otvori = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.txt_putanja = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.ofd1 = new System.Windows.Forms.OpenFileDialog();
            this.fbd1 = new System.Windows.Forms.FolderBrowserDialog();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.label8 = new System.Windows.Forms.Label();
            this.dtpDatumVazenja = new System.Windows.Forms.DateTimePicker();
            this.label9 = new System.Windows.Forms.Label();
            this.txtKreirao = new System.Windows.Forms.TextBox();
            this.button3 = new System.Windows.Forms.Button();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsNew,
            this.tsSave,
            this.tsDelete,
            this.toolStripSeparator1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1300, 27);
            this.toolStrip1.TabIndex = 146;
            this.toolStrip1.Text = "Osveži";
            // 
            // tsNew
            // 
            this.tsNew.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsNew.Image = ((System.Drawing.Image)(resources.GetObject("tsNew.Image")));
            this.tsNew.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsNew.Name = "tsNew";
            this.tsNew.Size = new System.Drawing.Size(24, 24);
            this.tsNew.Text = "Novi";
            this.tsNew.Click += new System.EventHandler(this.tsNew_Click);
            // 
            // tsSave
            // 
            this.tsSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsSave.Image = ((System.Drawing.Image)(resources.GetObject("tsSave.Image")));
            this.tsSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsSave.Name = "tsSave";
            this.tsSave.Size = new System.Drawing.Size(24, 24);
            this.tsSave.Text = "tsSave";
            this.tsSave.Click += new System.EventHandler(this.tsSave_Click);
            // 
            // tsDelete
            // 
            this.tsDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsDelete.Image = ((System.Drawing.Image)(resources.GetObject("tsDelete.Image")));
            this.tsDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDelete.Name = "tsDelete";
            this.tsDelete.Size = new System.Drawing.Size(24, 24);
            this.tsDelete.Text = "toolStripButton1";
            this.tsDelete.Click += new System.EventHandler(this.tsDelete_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 27);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(115, 15);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 13);
            this.label1.TabIndex = 147;
            this.label1.Text = "Obaveštenje";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // txt_Obavestenje
            // 
            this.txt_Obavestenje.Location = new System.Drawing.Point(118, 40);
            this.txt_Obavestenje.Margin = new System.Windows.Forms.Padding(2);
            this.txt_Obavestenje.Multiline = true;
            this.txt_Obavestenje.Name = "txt_Obavestenje";
            this.txt_Obavestenje.Size = new System.Drawing.Size(430, 92);
            this.txt_Obavestenje.TabIndex = 148;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 194);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 13);
            this.label2.TabIndex = 147;
            this.label2.Text = "Za korisnika";
            // 
            // cbList_Korisnici
            // 
            this.cbList_Korisnici.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.cbList_Korisnici.FormattingEnabled = true;
            this.cbList_Korisnici.Location = new System.Drawing.Point(16, 209);
            this.cbList_Korisnici.Margin = new System.Windows.Forms.Padding(2);
            this.cbList_Korisnici.Name = "cbList_Korisnici";
            this.cbList_Korisnici.Size = new System.Drawing.Size(191, 319);
            this.cbList_Korisnici.TabIndex = 150;
            // 
            // cb_Procitan
            // 
            this.cb_Procitan.AutoSize = true;
            this.cb_Procitan.Location = new System.Drawing.Point(235, 319);
            this.cb_Procitan.Margin = new System.Windows.Forms.Padding(2);
            this.cb_Procitan.Name = "cb_Procitan";
            this.cb_Procitan.Size = new System.Drawing.Size(65, 17);
            this.cb_Procitan.TabIndex = 151;
            this.cb_Procitan.Text = "Procitan";
            this.cb_Procitan.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(17, 61);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(2);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.RowTemplate.Height = 24;
            this.dataGridView1.Size = new System.Drawing.Size(630, 472);
            this.dataGridView1.TabIndex = 152;
            this.dataGridView1.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            // 
            // dt_Slanje
            // 
            this.dt_Slanje.CustomFormat = "dd.MM.yyyy HH:mm:ss";
            this.dt_Slanje.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dt_Slanje.Location = new System.Drawing.Point(235, 225);
            this.dt_Slanje.Margin = new System.Windows.Forms.Padding(2);
            this.dt_Slanje.Name = "dt_Slanje";
            this.dt_Slanje.Size = new System.Drawing.Size(149, 20);
            this.dt_Slanje.TabIndex = 153;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(232, 209);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(68, 13);
            this.label3.TabIndex = 147;
            this.label3.Text = "Datum slanja";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(232, 266);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(72, 13);
            this.label4.TabIndex = 147;
            this.label4.Text = "Datum citanja";
            // 
            // dt_Citanje
            // 
            this.dt_Citanje.CustomFormat = "dd.MM.yyyy HH:mm:ss";
            this.dt_Citanje.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dt_Citanje.Location = new System.Drawing.Point(235, 282);
            this.dt_Citanje.Margin = new System.Windows.Forms.Padding(2);
            this.dt_Citanje.Name = "dt_Citanje";
            this.dt_Citanje.Size = new System.Drawing.Size(149, 20);
            this.dt_Citanje.TabIndex = 153;
            this.dt_Citanje.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 15);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(18, 13);
            this.label5.TabIndex = 147;
            this.label5.Text = "ID";
            // 
            // txt_ID
            // 
            this.txt_ID.Location = new System.Drawing.Point(16, 30);
            this.txt_ID.Margin = new System.Windows.Forms.Padding(2);
            this.txt_ID.Name = "txt_ID";
            this.txt_ID.Size = new System.Drawing.Size(66, 20);
            this.txt_ID.TabIndex = 148;
            // 
            // combo_RadnoMesto
            // 
            this.combo_RadnoMesto.FormattingEnabled = true;
            this.combo_RadnoMesto.Location = new System.Drawing.Point(10, 159);
            this.combo_RadnoMesto.Margin = new System.Windows.Forms.Padding(2);
            this.combo_RadnoMesto.Name = "combo_RadnoMesto";
            this.combo_RadnoMesto.Size = new System.Drawing.Size(270, 21);
            this.combo_RadnoMesto.TabIndex = 154;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(300, 159);
            this.button1.Margin = new System.Windows.Forms.Padding(2);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(65, 22);
            this.button1.TabIndex = 155;
            this.button1.Text = "Nadji";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnOsvezi
            // 
            this.btnOsvezi.Location = new System.Drawing.Point(386, 159);
            this.btnOsvezi.Margin = new System.Windows.Forms.Padding(2);
            this.btnOsvezi.Name = "btnOsvezi";
            this.btnOsvezi.Size = new System.Drawing.Size(65, 22);
            this.btnOsvezi.TabIndex = 155;
            this.btnOsvezi.Text = "Refresh";
            this.btnOsvezi.UseVisualStyleBackColor = true;
            this.btnOsvezi.Click += new System.EventHandler(this.btnOsvezi_Click);
            // 
            // cboTipNotifikacije
            // 
            this.cboTipNotifikacije.FormattingEnabled = true;
            this.cboTipNotifikacije.Items.AddRange(new object[] {
            "Saopštenje",
            "Naredba"});
            this.cboTipNotifikacije.Location = new System.Drawing.Point(231, 358);
            this.cboTipNotifikacije.Name = "cboTipNotifikacije";
            this.cboTipNotifikacije.Size = new System.Drawing.Size(180, 21);
            this.cboTipNotifikacije.TabIndex = 156;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(232, 342);
            this.label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(92, 13);
            this.label6.TabIndex = 157;
            this.label6.Text = "Vrsta obaveštenja";
            // 
            // btn_Otvori
            // 
            this.btn_Otvori.Location = new System.Drawing.Point(309, 498);
            this.btn_Otvori.Margin = new System.Windows.Forms.Padding(2);
            this.btn_Otvori.Name = "btn_Otvori";
            this.btn_Otvori.Size = new System.Drawing.Size(56, 23);
            this.btn_Otvori.TabIndex = 160;
            this.btn_Otvori.Text = "Otvori";
            this.btn_Otvori.UseVisualStyleBackColor = true;
            this.btn_Otvori.Click += new System.EventHandler(this.btn_Otvori_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(235, 498);
            this.button2.Margin = new System.Windows.Forms.Padding(2);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(56, 23);
            this.button2.TabIndex = 161;
            this.button2.Text = "Pronadji";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // txt_putanja
            // 
            this.txt_putanja.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txt_putanja.Location = new System.Drawing.Point(235, 463);
            this.txt_putanja.Margin = new System.Windows.Forms.Padding(2);
            this.txt_putanja.Name = "txt_putanja";
            this.txt_putanja.Size = new System.Drawing.Size(302, 20);
            this.txt_putanja.TabIndex = 159;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(236, 448);
            this.label7.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(43, 13);
            this.label7.TabIndex = 158;
            this.label7.Text = "Putanja";
            // 
            // ofd1
            // 
            this.ofd1.FileName = "openFileDialog1";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(12, 30);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.label8);
            this.splitContainer1.Panel1.Controls.Add(this.dtpDatumVazenja);
            this.splitContainer1.Panel1.Controls.Add(this.label5);
            this.splitContainer1.Panel1.Controls.Add(this.btnOsvezi);
            this.splitContainer1.Panel1.Controls.Add(this.button1);
            this.splitContainer1.Panel1.Controls.Add(this.btn_Otvori);
            this.splitContainer1.Panel1.Controls.Add(this.combo_RadnoMesto);
            this.splitContainer1.Panel1.Controls.Add(this.txt_ID);
            this.splitContainer1.Panel1.Controls.Add(this.button2);
            this.splitContainer1.Panel1.Controls.Add(this.cbList_Korisnici);
            this.splitContainer1.Panel1.Controls.Add(this.label2);
            this.splitContainer1.Panel1.Controls.Add(this.txt_Obavestenje);
            this.splitContainer1.Panel1.Controls.Add(this.txt_putanja);
            this.splitContainer1.Panel1.Controls.Add(this.label1);
            this.splitContainer1.Panel1.Controls.Add(this.label7);
            this.splitContainer1.Panel1.Controls.Add(this.label3);
            this.splitContainer1.Panel1.Controls.Add(this.label6);
            this.splitContainer1.Panel1.Controls.Add(this.label4);
            this.splitContainer1.Panel1.Controls.Add(this.cboTipNotifikacije);
            this.splitContainer1.Panel1.Controls.Add(this.cb_Procitan);
            this.splitContainer1.Panel1.Controls.Add(this.dt_Slanje);
            this.splitContainer1.Panel1.Controls.Add(this.dt_Citanje);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.label9);
            this.splitContainer1.Panel2.Controls.Add(this.txtKreirao);
            this.splitContainer1.Panel2.Controls.Add(this.button3);
            this.splitContainer1.Panel2.Controls.Add(this.dataGridView1);
            this.splitContainer1.Size = new System.Drawing.Size(1230, 542);
            this.splitContainer1.SplitterDistance = 566;
            this.splitContainer1.TabIndex = 162;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(232, 401);
            this.label8.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(78, 13);
            this.label8.TabIndex = 162;
            this.label8.Text = "Datum važenja";
            // 
            // dtpDatumVazenja
            // 
            this.dtpDatumVazenja.CustomFormat = "dd.MM.yyyy HH:mm:ss";
            this.dtpDatumVazenja.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpDatumVazenja.Location = new System.Drawing.Point(235, 417);
            this.dtpDatumVazenja.Margin = new System.Windows.Forms.Padding(2);
            this.dtpDatumVazenja.Name = "dtpDatumVazenja";
            this.dtpDatumVazenja.Size = new System.Drawing.Size(149, 20);
            this.dtpDatumVazenja.TabIndex = 163;
            this.dtpDatumVazenja.Value = new System.DateTime(2900, 1, 1, 0, 0, 0, 0);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(14, 19);
            this.label9.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(44, 13);
            this.label9.TabIndex = 158;
            this.label9.Text = "Korisnik";
            // 
            // txtKreirao
            // 
            this.txtKreirao.Location = new System.Drawing.Point(62, 16);
            this.txtKreirao.Margin = new System.Windows.Forms.Padding(2);
            this.txtKreirao.Name = "txtKreirao";
            this.txtKreirao.Size = new System.Drawing.Size(128, 20);
            this.txtKreirao.TabIndex = 157;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(204, 15);
            this.button3.Margin = new System.Windows.Forms.Padding(2);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(65, 22);
            this.button3.TabIndex = 156;
            this.button3.Text = "Filter";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // frmNotifikacije
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1300, 626);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "frmNotifikacije";
            this.Text = "Notifikacije";
            this.Load += new System.EventHandler(this.frmNotifikacije_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsNew;
        private System.Windows.Forms.ToolStripButton tsSave;
        private System.Windows.Forms.ToolStripButton tsDelete;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txt_Obavestenje;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckedListBox cbList_Korisnici;
        private System.Windows.Forms.CheckBox cb_Procitan;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DateTimePicker dt_Slanje;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.DateTimePicker dt_Citanje;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txt_ID;
        private System.Windows.Forms.ComboBox combo_RadnoMesto;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btnOsvezi;
        private System.Windows.Forms.ComboBox cboTipNotifikacije;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btn_Otvori;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox txt_putanja;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.OpenFileDialog ofd1;
        private System.Windows.Forms.FolderBrowserDialog fbd1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.DateTimePicker dtpDatumVazenja;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtKreirao;
        private System.Windows.Forms.Button button3;
    }
}