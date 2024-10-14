namespace Saobracaj.Dokumenta
{
    partial class frmRadniNalogPregled
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmRadniNalogPregled));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton3 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton5 = new System.Windows.Forms.ToolStripButton();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.txtSifra = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.chkPR = new System.Windows.Forms.CheckBox();
            this.chkLA = new System.Windows.Forms.CheckBox();
            this.chkOD = new System.Windows.Forms.CheckBox();
            this.chkPL = new System.Windows.Forms.CheckBox();
            this.chkST = new System.Windows.Forms.CheckBox();
            this.chkZA = new System.Windows.Forms.CheckBox();
            this.button1 = new System.Windows.Forms.Button();
            this.txtNajava = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.dtpVremeOd2 = new System.Windows.Forms.DateTimePicker();
            this.dtpVremeDo2 = new System.Windows.Forms.DateTimePicker();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.txtSaobraca = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.button4 = new System.Windows.Forms.Button();
            this.button5 = new System.Windows.Forms.Button();
            this.txtTrasa = new System.Windows.Forms.TextBox();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton1,
            this.toolStripButton2,
            this.toolStripButton3,
            this.toolStripButton5});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1036, 27);
            this.toolStrip1.TabIndex = 9;
            this.toolStrip1.Text = "Pošalji mail infrastrukturi";
            this.toolStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.toolStrip1_ItemClicked);
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(127, 24);
            this.toolStripButton1.Text = "Otvori radni nalog";
            this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // toolStripButton2
            // 
            this.toolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton2.Image")));
            this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton2.Name = "toolStripButton2";
            this.toolStripButton2.Size = new System.Drawing.Size(65, 24);
            this.toolStripButton2.Text = "Osveži";
            this.toolStripButton2.Click += new System.EventHandler(this.toolStripButton2_Click);
            // 
            // toolStripButton3
            // 
            this.toolStripButton3.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton3.Image")));
            this.toolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton3.Name = "toolStripButton3";
            this.toolStripButton3.Size = new System.Drawing.Size(119, 24);
            this.toolStripButton3.Text = "Novi radni nalog";
            this.toolStripButton3.Click += new System.EventHandler(this.toolStripButton3_Click);
            // 
            // toolStripButton5
            // 
            this.toolStripButton5.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton5.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton5.Image")));
            this.toolStripButton5.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton5.Name = "toolStripButton5";
            this.toolStripButton5.Size = new System.Drawing.Size(104, 24);
            this.toolStripButton5.Text = "Izvoz z ExceL - ZA";
            this.toolStripButton5.Click += new System.EventHandler(this.toolStripButton5_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(12, 148);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.Size = new System.Drawing.Size(1000, 221);
            this.dataGridView1.TabIndex = 10;
            this.dataGridView1.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellContentClick);
            this.dataGridView1.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            // 
            // txtSifra
            // 
            this.txtSifra.Location = new System.Drawing.Point(46, 28);
            this.txtSifra.Name = "txtSifra";
            this.txtSifra.Size = new System.Drawing.Size(59, 20);
            this.txtSifra.TabIndex = 120;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 31);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(31, 13);
            this.label1.TabIndex = 121;
            this.label1.Text = "Šifra:";
            // 
            // chkPR
            // 
            this.chkPR.AutoSize = true;
            this.chkPR.Location = new System.Drawing.Point(172, 30);
            this.chkPR.Name = "chkPR";
            this.chkPR.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkPR.Size = new System.Drawing.Size(41, 17);
            this.chkPR.TabIndex = 122;
            this.chkPR.Text = "PR";
            this.chkPR.UseVisualStyleBackColor = true;
            this.chkPR.CheckedChanged += new System.EventHandler(this.chkPR_CheckedChanged);
            // 
            // chkLA
            // 
            this.chkLA.AutoSize = true;
            this.chkLA.Location = new System.Drawing.Point(348, 30);
            this.chkLA.Name = "chkLA";
            this.chkLA.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkLA.Size = new System.Drawing.Size(41, 17);
            this.chkLA.TabIndex = 123;
            this.chkLA.Text = "RA";
            this.chkLA.UseVisualStyleBackColor = true;
            this.chkLA.CheckedChanged += new System.EventHandler(this.chkLA_CheckedChanged);
            // 
            // chkOD
            // 
            this.chkOD.AutoSize = true;
            this.chkOD.Location = new System.Drawing.Point(287, 30);
            this.chkOD.Name = "chkOD";
            this.chkOD.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkOD.Size = new System.Drawing.Size(42, 17);
            this.chkOD.TabIndex = 124;
            this.chkOD.Text = "OD";
            this.chkOD.UseVisualStyleBackColor = true;
            this.chkOD.CheckedChanged += new System.EventHandler(this.chkOD_CheckedChanged);
            // 
            // chkPL
            // 
            this.chkPL.AutoSize = true;
            this.chkPL.Location = new System.Drawing.Point(229, 29);
            this.chkPL.Name = "chkPL";
            this.chkPL.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkPL.Size = new System.Drawing.Size(39, 17);
            this.chkPL.TabIndex = 125;
            this.chkPL.Text = "PL";
            this.chkPL.UseVisualStyleBackColor = true;
            this.chkPL.CheckedChanged += new System.EventHandler(this.chkPL_CheckedChanged);
            // 
            // chkST
            // 
            this.chkST.AutoSize = true;
            this.chkST.Location = new System.Drawing.Point(406, 30);
            this.chkST.Name = "chkST";
            this.chkST.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkST.Size = new System.Drawing.Size(40, 17);
            this.chkST.TabIndex = 126;
            this.chkST.Text = "ST";
            this.chkST.UseVisualStyleBackColor = true;
            this.chkST.CheckedChanged += new System.EventHandler(this.chkST_CheckedChanged);
            // 
            // chkZA
            // 
            this.chkZA.AutoSize = true;
            this.chkZA.Location = new System.Drawing.Point(463, 30);
            this.chkZA.Name = "chkZA";
            this.chkZA.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkZA.Size = new System.Drawing.Size(40, 17);
            this.chkZA.TabIndex = 127;
            this.chkZA.Text = "ZA";
            this.chkZA.UseVisualStyleBackColor = true;
            this.chkZA.CheckedChanged += new System.EventHandler(this.chkZA_CheckedChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(22, 83);
            this.button1.Margin = new System.Windows.Forms.Padding(2);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(133, 22);
            this.button1.TabIndex = 128;
            this.button1.Text = "Infrastruktura PL";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // txtNajava
            // 
            this.txtNajava.Location = new System.Drawing.Point(167, 83);
            this.txtNajava.Margin = new System.Windows.Forms.Padding(2);
            this.txtNajava.Name = "txtNajava";
            this.txtNajava.Size = new System.Drawing.Size(69, 20);
            this.txtNajava.TabIndex = 129;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(240, 79);
            this.button2.Margin = new System.Windows.Forms.Padding(2);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(106, 27);
            this.button2.TabIndex = 130;
            this.button2.Text = "Pretrazi po najavi";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(394, 111);
            this.button3.Margin = new System.Windows.Forms.Padding(2);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(109, 26);
            this.button3.TabIndex = 131;
            this.button3.Text = "Pretraži";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // dtpVremeOd2
            // 
            this.dtpVremeOd2.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dtpVremeOd2.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeOd2.Location = new System.Drawing.Point(82, 112);
            this.dtpVremeOd2.Name = "dtpVremeOd2";
            this.dtpVremeOd2.ShowUpDown = true;
            this.dtpVremeOd2.Size = new System.Drawing.Size(110, 20);
            this.dtpVremeOd2.TabIndex = 318;
            this.dtpVremeOd2.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // dtpVremeDo2
            // 
            this.dtpVremeDo2.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dtpVremeDo2.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeDo2.Location = new System.Drawing.Point(274, 112);
            this.dtpVremeDo2.Name = "dtpVremeDo2";
            this.dtpVremeDo2.ShowUpDown = true;
            this.dtpVremeDo2.Size = new System.Drawing.Size(115, 20);
            this.dtpVremeDo2.TabIndex = 317;
            this.dtpVremeDo2.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.ForeColor = System.Drawing.Color.Red;
            this.label7.Location = new System.Drawing.Point(213, 114);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(55, 13);
            this.label7.TabIndex = 316;
            this.label7.Text = "Period do:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.ForeColor = System.Drawing.Color.Red;
            this.label8.Location = new System.Drawing.Point(21, 113);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(55, 13);
            this.label8.TabIndex = 315;
            this.label8.Text = "Period od:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 55);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(61, 13);
            this.label2.TabIndex = 121;
            this.label2.Text = "Broj zapisa:";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(82, 52);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(59, 20);
            this.textBox1.TabIndex = 120;
            this.textBox1.Text = "1000";
            // 
            // txtSaobraca
            // 
            this.txtSaobraca.Location = new System.Drawing.Point(578, 31);
            this.txtSaobraca.Margin = new System.Windows.Forms.Padding(2);
            this.txtSaobraca.Multiline = true;
            this.txtSaobraca.Name = "txtSaobraca";
            this.txtSaobraca.Size = new System.Drawing.Size(434, 112);
            this.txtSaobraca.TabIndex = 319;
            this.txtSaobraca.TextChanged += new System.EventHandler(this.textBox2_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(575, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(65, 13);
            this.label3.TabIndex = 320;
            this.label3.Text = "Kopija viber:";
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(892, 3);
            this.button4.Margin = new System.Windows.Forms.Padding(2);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(109, 26);
            this.button4.TabIndex = 321;
            this.button4.Text = "Generisi";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(452, 79);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(121, 26);
            this.button5.TabIndex = 322;
            this.button5.Text = "Pretraži po broju trase";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // txtTrasa
            // 
            this.txtTrasa.Location = new System.Drawing.Point(366, 83);
            this.txtTrasa.Name = "txtTrasa";
            this.txtTrasa.Size = new System.Drawing.Size(80, 20);
            this.txtTrasa.TabIndex = 323;
            // 
            // frmRadniNalogPregled
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1036, 372);
            this.Controls.Add(this.txtTrasa);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtSaobraca);
            this.Controls.Add(this.dtpVremeOd2);
            this.Controls.Add(this.dtpVremeDo2);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.txtNajava);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.chkZA);
            this.Controls.Add(this.chkST);
            this.Controls.Add(this.chkPL);
            this.Controls.Add(this.chkOD);
            this.Controls.Add(this.chkLA);
            this.Controls.Add(this.chkPR);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtSifra);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.toolStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmRadniNalogPregled";
            this.Text = "Radni nalozi";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.frmRadniNalogPregled_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.TextBox txtSifra;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolStripButton toolStripButton2;
        private System.Windows.Forms.CheckBox chkPR;
        private System.Windows.Forms.CheckBox chkLA;
        private System.Windows.Forms.CheckBox chkOD;
        private System.Windows.Forms.CheckBox chkPL;
        private System.Windows.Forms.CheckBox chkST;
        private System.Windows.Forms.CheckBox chkZA;
        private System.Windows.Forms.ToolStripButton toolStripButton3;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox txtNajava;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.ToolStripButton toolStripButton5;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.DateTimePicker dtpVremeOd2;
        private System.Windows.Forms.DateTimePicker dtpVremeDo2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox txtSaobraca;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.TextBox txtTrasa;
    }
}