namespace Saobracaj.Sifarnici
{
    partial class frmTrasaStanice
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmTrasaStanice));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsNew = new System.Windows.Forms.ToolStripButton();
            this.tsSave = new System.Windows.Forms.ToolStripButton();
            this.tsDelete = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsPrvi = new System.Windows.Forms.ToolStripButton();
            this.tsNazad = new System.Windows.Forms.ToolStripButton();
            this.tsNapred = new System.Windows.Forms.ToolStripButton();
            this.tsPoslednja = new System.Windows.Forms.ToolStripButton();
            this.label6 = new System.Windows.Forms.Label();
            this.txtVoz = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cboKrajnja = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbPocetna = new System.Windows.Forms.ComboBox();
            this.txtSifra = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.cboPruga = new System.Windows.Forms.ComboBox();
            this.btnUbaci = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.button1 = new System.Windows.Forms.Button();
            this.toolStrip1.SuspendLayout();
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
            this.tsPoslednja});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(908, 25);
            this.toolStrip1.TabIndex = 6;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsNew
            // 
            this.tsNew.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsNew.Image = ((System.Drawing.Image)(resources.GetObject("tsNew.Image")));
            this.tsNew.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsNew.Name = "tsNew";
            this.tsNew.Size = new System.Drawing.Size(23, 22);
            this.tsNew.Text = "Novi";
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
            this.tsDelete.Click += new System.EventHandler(this.tsDelete_Click);
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
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(193, 39);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(98, 13);
            this.label6.TabIndex = 65;
            this.label6.Text = "Voz/Trasa oznaka:";
            // 
            // txtVoz
            // 
            this.txtVoz.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.txtVoz.Location = new System.Drawing.Point(306, 36);
            this.txtVoz.Name = "txtVoz";
            this.txtVoz.Size = new System.Drawing.Size(121, 20);
            this.txtVoz.TabIndex = 64;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(694, 60);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(42, 13);
            this.label3.TabIndex = 63;
            this.label3.Text = "Krajnja:";
            // 
            // cboKrajnja
            // 
            this.cboKrajnja.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(128)))));
            this.cboKrajnja.FormattingEnabled = true;
            this.cboKrajnja.Location = new System.Drawing.Point(742, 60);
            this.cboKrajnja.Name = "cboKrajnja";
            this.cboKrajnja.Size = new System.Drawing.Size(154, 21);
            this.cboKrajnja.TabIndex = 62;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 68);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 13);
            this.label2.TabIndex = 61;
            this.label2.Text = "Početna:";
            // 
            // cmbPocetna
            // 
            this.cmbPocetna.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(128)))));
            this.cmbPocetna.FormattingEnabled = true;
            this.cmbPocetna.Location = new System.Drawing.Point(92, 65);
            this.cmbPocetna.Name = "cmbPocetna";
            this.cmbPocetna.Size = new System.Drawing.Size(335, 21);
            this.cmbPocetna.TabIndex = 60;
            // 
            // txtSifra
            // 
            this.txtSifra.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.txtSifra.Location = new System.Drawing.Point(92, 39);
            this.txtSifra.Name = "txtSifra";
            this.txtSifra.Size = new System.Drawing.Size(85, 20);
            this.txtSifra.TabIndex = 59;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(31, 13);
            this.label1.TabIndex = 58;
            this.label1.Text = "Šifra:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(644, 28);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(38, 13);
            this.label4.TabIndex = 68;
            this.label4.Text = "Pruga:";
            // 
            // cboPruga
            // 
            this.cboPruga.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(128)))));
            this.cboPruga.FormattingEnabled = true;
            this.cboPruga.Location = new System.Drawing.Point(698, 28);
            this.cboPruga.Name = "cboPruga";
            this.cboPruga.Size = new System.Drawing.Size(198, 21);
            this.cboPruga.TabIndex = 67;
            this.cboPruga.SelectedIndexChanged += new System.EventHandler(this.cboPruga_SelectedIndexChanged);
            this.cboPruga.Leave += new System.EventHandler(this.cboPruga_Leave);
            // 
            // btnUbaci
            // 
            this.btnUbaci.Location = new System.Drawing.Point(447, 63);
            this.btnUbaci.Name = "btnUbaci";
            this.btnUbaci.Size = new System.Drawing.Size(116, 23);
            this.btnUbaci.TabIndex = 69;
            this.btnUbaci.Text = "Ubaci stanicu";
            this.btnUbaci.UseVisualStyleBackColor = true;
            this.btnUbaci.Click += new System.EventHandler(this.btnUbaci_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(12, 92);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(884, 258);
            this.dataGridView1.TabIndex = 70;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(566, 63);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(116, 23);
            this.button1.TabIndex = 71;
            this.button1.Text = "Izbaci";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // frmTrasaStanice
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(908, 372);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.btnUbaci);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cboPruga);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txtVoz);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cboKrajnja);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cmbPocetna);
            this.Controls.Add(this.txtSifra);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.toolStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmTrasaStanice";
            this.Text = "Trasa ruta";
            this.Load += new System.EventHandler(this.frmTrasaStanice_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
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
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtVoz;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cboKrajnja;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbPocetna;
        private System.Windows.Forms.TextBox txtSifra;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cboPruga;
        private System.Windows.Forms.Button btnUbaci;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button button1;
    }
}