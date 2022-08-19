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
            this.toolStripButton3});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1172, 27);
            this.toolStrip1.TabIndex = 9;
            this.toolStrip1.Text = "Pošalji mail infrastrukturi";
            this.toolStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.toolStrip1_ItemClicked);
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(154, 24);
            this.toolStripButton1.Text = "Otvori radni nalog";
            this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // toolStripButton2
            // 
            this.toolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton2.Image")));
            this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton2.Name = "toolStripButton2";
            this.toolStripButton2.Size = new System.Drawing.Size(76, 24);
            this.toolStripButton2.Text = "Osveži";
            this.toolStripButton2.Click += new System.EventHandler(this.toolStripButton2_Click);
            // 
            // toolStripButton3
            // 
            this.toolStripButton3.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton3.Image")));
            this.toolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton3.Name = "toolStripButton3";
            this.toolStripButton3.Size = new System.Drawing.Size(144, 24);
            this.toolStripButton3.Text = "Novi radni nalog";
            this.toolStripButton3.Click += new System.EventHandler(this.toolStripButton3_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(16, 66);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(4);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.Size = new System.Drawing.Size(1140, 388);
            this.dataGridView1.TabIndex = 10;
            this.dataGridView1.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellContentClick);
            this.dataGridView1.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            // 
            // txtSifra
            // 
            this.txtSifra.Location = new System.Drawing.Point(61, 34);
            this.txtSifra.Margin = new System.Windows.Forms.Padding(4);
            this.txtSifra.Name = "txtSifra";
            this.txtSifra.Size = new System.Drawing.Size(77, 22);
            this.txtSifra.TabIndex = 120;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 38);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 17);
            this.label1.TabIndex = 121;
            this.label1.Text = "Šifra:";
            // 
            // chkPR
            // 
            this.chkPR.AutoSize = true;
            this.chkPR.Location = new System.Drawing.Point(229, 37);
            this.chkPR.Margin = new System.Windows.Forms.Padding(4);
            this.chkPR.Name = "chkPR";
            this.chkPR.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkPR.Size = new System.Drawing.Size(49, 21);
            this.chkPR.TabIndex = 122;
            this.chkPR.Text = "PR";
            this.chkPR.UseVisualStyleBackColor = true;
            this.chkPR.CheckedChanged += new System.EventHandler(this.chkPR_CheckedChanged);
            // 
            // chkLA
            // 
            this.chkLA.AutoSize = true;
            this.chkLA.Location = new System.Drawing.Point(464, 37);
            this.chkLA.Margin = new System.Windows.Forms.Padding(4);
            this.chkLA.Name = "chkLA";
            this.chkLA.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkLA.Size = new System.Drawing.Size(49, 21);
            this.chkLA.TabIndex = 123;
            this.chkLA.Text = "RA";
            this.chkLA.UseVisualStyleBackColor = true;
            this.chkLA.CheckedChanged += new System.EventHandler(this.chkLA_CheckedChanged);
            // 
            // chkOD
            // 
            this.chkOD.AutoSize = true;
            this.chkOD.Location = new System.Drawing.Point(383, 37);
            this.chkOD.Margin = new System.Windows.Forms.Padding(4);
            this.chkOD.Name = "chkOD";
            this.chkOD.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkOD.Size = new System.Drawing.Size(51, 21);
            this.chkOD.TabIndex = 124;
            this.chkOD.Text = "OD";
            this.chkOD.UseVisualStyleBackColor = true;
            this.chkOD.CheckedChanged += new System.EventHandler(this.chkOD_CheckedChanged);
            // 
            // chkPL
            // 
            this.chkPL.AutoSize = true;
            this.chkPL.Location = new System.Drawing.Point(305, 36);
            this.chkPL.Margin = new System.Windows.Forms.Padding(4);
            this.chkPL.Name = "chkPL";
            this.chkPL.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkPL.Size = new System.Drawing.Size(47, 21);
            this.chkPL.TabIndex = 125;
            this.chkPL.Text = "PL";
            this.chkPL.UseVisualStyleBackColor = true;
            this.chkPL.CheckedChanged += new System.EventHandler(this.chkPL_CheckedChanged);
            // 
            // chkST
            // 
            this.chkST.AutoSize = true;
            this.chkST.Location = new System.Drawing.Point(541, 37);
            this.chkST.Margin = new System.Windows.Forms.Padding(4);
            this.chkST.Name = "chkST";
            this.chkST.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkST.Size = new System.Drawing.Size(48, 21);
            this.chkST.TabIndex = 126;
            this.chkST.Text = "ST";
            this.chkST.UseVisualStyleBackColor = true;
            this.chkST.CheckedChanged += new System.EventHandler(this.chkST_CheckedChanged);
            // 
            // chkZA
            // 
            this.chkZA.AutoSize = true;
            this.chkZA.Location = new System.Drawing.Point(617, 37);
            this.chkZA.Margin = new System.Windows.Forms.Padding(4);
            this.chkZA.Name = "chkZA";
            this.chkZA.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkZA.Size = new System.Drawing.Size(48, 21);
            this.chkZA.TabIndex = 127;
            this.chkZA.Text = "ZA";
            this.chkZA.UseVisualStyleBackColor = true;
            this.chkZA.CheckedChanged += new System.EventHandler(this.chkZA_CheckedChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(692, 30);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(177, 27);
            this.button1.TabIndex = 128;
            this.button1.Text = "Infrastruktura PL";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // txtNajava
            // 
            this.txtNajava.Location = new System.Drawing.Point(918, 31);
            this.txtNajava.Name = "txtNajava";
            this.txtNajava.Size = new System.Drawing.Size(91, 22);
            this.txtNajava.TabIndex = 129;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(1015, 26);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(141, 33);
            this.button2.TabIndex = 130;
            this.button2.Text = "Pretrazi po najavi";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // frmRadniNalogPregled
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1172, 458);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.txtNajava);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.chkZA);
            this.Controls.Add(this.chkST);
            this.Controls.Add(this.chkPL);
            this.Controls.Add(this.chkOD);
            this.Controls.Add(this.chkLA);
            this.Controls.Add(this.chkPR);
            this.Controls.Add(this.txtSifra);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.toolStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
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
    }
}