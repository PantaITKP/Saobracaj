
namespace Saobracaj.Sifarnici
{
    partial class frmCenovniciVrsteAktivnosti
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmCenovniciVrsteAktivnosti));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsNew = new System.Windows.Forms.ToolStripButton();
            this.tsSave = new System.Windows.Forms.ToolStripButton();
            this.tsDelete = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsPrvi = new System.Windows.Forms.ToolStripButton();
            this.tsNazad = new System.Windows.Forms.ToolStripButton();
            this.tsNapred = new System.Windows.Forms.ToolStripButton();
            this.tsPoslednja = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.txtID = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtNaziv = new System.Windows.Forms.TextBox();
            this.lblNaziv = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.dt_VaziDo = new System.Windows.Forms.DateTimePicker();
            this.dt_VaziOd = new System.Windows.Forms.DateTimePicker();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.txtNapomena = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsNew,
            this.tsSave,
            this.tsDelete,
            this.toolStripSeparator1,
            this.tsPrvi,
            this.tsNazad,
            this.tsNapred,
            this.tsPoslednja,
            this.toolStripSeparator2});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(866, 27);
            this.toolStrip1.TabIndex = 83;
            this.toolStrip1.Text = "Štampaj izveštaj";
            // 
            // tsNew
            // 
            this.tsNew.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsNew.Image = ((System.Drawing.Image)(resources.GetObject("tsNew.Image")));
            this.tsNew.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsNew.Name = "tsNew";
            this.tsNew.Size = new System.Drawing.Size(29, 24);
            this.tsNew.Text = "Novi";
            this.tsNew.Click += new System.EventHandler(this.tsNew_Click);
            // 
            // tsSave
            // 
            this.tsSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsSave.Image = ((System.Drawing.Image)(resources.GetObject("tsSave.Image")));
            this.tsSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsSave.Name = "tsSave";
            this.tsSave.Size = new System.Drawing.Size(29, 24);
            this.tsSave.Text = "tsSave";
            this.tsSave.Click += new System.EventHandler(this.tsSave_Click);
            // 
            // tsDelete
            // 
            this.tsDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsDelete.Image = ((System.Drawing.Image)(resources.GetObject("tsDelete.Image")));
            this.tsDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDelete.Name = "tsDelete";
            this.tsDelete.Size = new System.Drawing.Size(29, 24);
            this.tsDelete.Text = "toolStripButton1";
            this.tsDelete.Click += new System.EventHandler(this.tsDelete_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 27);
            // 
            // tsPrvi
            // 
            this.tsPrvi.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsPrvi.Image = ((System.Drawing.Image)(resources.GetObject("tsPrvi.Image")));
            this.tsPrvi.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsPrvi.Name = "tsPrvi";
            this.tsPrvi.Size = new System.Drawing.Size(29, 24);
            this.tsPrvi.Text = "toolStripButton1";
            // 
            // tsNazad
            // 
            this.tsNazad.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsNazad.Image = ((System.Drawing.Image)(resources.GetObject("tsNazad.Image")));
            this.tsNazad.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsNazad.Name = "tsNazad";
            this.tsNazad.Size = new System.Drawing.Size(29, 24);
            this.tsNazad.Text = "toolStripButton1";
            // 
            // tsNapred
            // 
            this.tsNapred.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsNapred.Image = ((System.Drawing.Image)(resources.GetObject("tsNapred.Image")));
            this.tsNapred.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsNapred.Name = "tsNapred";
            this.tsNapred.Size = new System.Drawing.Size(29, 24);
            this.tsNapred.Text = "toolStripButton1";
            // 
            // tsPoslednja
            // 
            this.tsPoslednja.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsPoslednja.Image = ((System.Drawing.Image)(resources.GetObject("tsPoslednja.Image")));
            this.tsPoslednja.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsPoslednja.Name = "tsPoslednja";
            this.tsPoslednja.Size = new System.Drawing.Size(29, 24);
            this.tsPoslednja.Text = "toolStripButton1";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 27);
            // 
            // txtID
            // 
            this.txtID.Location = new System.Drawing.Point(110, 46);
            this.txtID.Margin = new System.Windows.Forms.Padding(4);
            this.txtID.Name = "txtID";
            this.txtID.Size = new System.Drawing.Size(112, 22);
            this.txtID.TabIndex = 240;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(27, 44);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(37, 17);
            this.label3.TabIndex = 239;
            this.label3.Text = "Šifra";
            // 
            // txtNaziv
            // 
            this.txtNaziv.Location = new System.Drawing.Point(110, 79);
            this.txtNaziv.Margin = new System.Windows.Forms.Padding(4);
            this.txtNaziv.Name = "txtNaziv";
            this.txtNaziv.Size = new System.Drawing.Size(332, 22);
            this.txtNaziv.TabIndex = 238;
            // 
            // lblNaziv
            // 
            this.lblNaziv.AutoSize = true;
            this.lblNaziv.Location = new System.Drawing.Point(27, 79);
            this.lblNaziv.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblNaziv.Name = "lblNaziv";
            this.lblNaziv.Size = new System.Drawing.Size(43, 17);
            this.lblNaziv.TabIndex = 237;
            this.lblNaziv.Text = "Naziv";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(13, 230);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(4);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.Size = new System.Drawing.Size(840, 357);
            this.dataGridView1.TabIndex = 241;
            this.dataGridView1.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            // 
            // dt_VaziDo
            // 
            this.dt_VaziDo.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dt_VaziDo.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.dt_VaziDo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dt_VaziDo.Location = new System.Drawing.Point(110, 146);
            this.dt_VaziDo.Margin = new System.Windows.Forms.Padding(4);
            this.dt_VaziDo.Name = "dt_VaziDo";
            this.dt_VaziDo.ShowUpDown = true;
            this.dt_VaziDo.Size = new System.Drawing.Size(199, 27);
            this.dt_VaziDo.TabIndex = 244;
            this.dt_VaziDo.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // dt_VaziOd
            // 
            this.dt_VaziOd.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dt_VaziOd.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.dt_VaziOd.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dt_VaziOd.Location = new System.Drawing.Point(110, 109);
            this.dt_VaziOd.Margin = new System.Windows.Forms.Padding(4);
            this.dt_VaziOd.Name = "dt_VaziOd";
            this.dt_VaziOd.ShowUpDown = true;
            this.dt_VaziOd.Size = new System.Drawing.Size(199, 27);
            this.dt_VaziOd.TabIndex = 245;
            this.dt_VaziOd.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(27, 146);
            this.label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(73, 17);
            this.label11.TabIndex = 242;
            this.label11.Text = "Datum do:";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(27, 109);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(73, 17);
            this.label10.TabIndex = 243;
            this.label10.Text = "Datum od:";
            // 
            // txtNapomena
            // 
            this.txtNapomena.Location = new System.Drawing.Point(110, 178);
            this.txtNapomena.Margin = new System.Windows.Forms.Padding(4);
            this.txtNapomena.Multiline = true;
            this.txtNapomena.Name = "txtNapomena";
            this.txtNapomena.Size = new System.Drawing.Size(602, 44);
            this.txtNapomena.TabIndex = 247;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 178);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 17);
            this.label1.TabIndex = 246;
            this.label1.Text = "Napomena";
            // 
            // frmCenovniciVrsteAktivnosti
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(866, 600);
            this.Controls.Add(this.txtNapomena);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.dt_VaziDo);
            this.Controls.Add(this.dt_VaziOd);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.txtID);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtNaziv);
            this.Controls.Add(this.lblNaziv);
            this.Controls.Add(this.toolStrip1);
            this.Name = "frmCenovniciVrsteAktivnosti";
            this.Text = "Cenovnici vrste aktivnosti";
            this.Load += new System.EventHandler(this.frmCenovniciVrsteAktivnosti_Load);
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
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.TextBox txtID;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtNaziv;
        private System.Windows.Forms.Label lblNaziv;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DateTimePicker dt_VaziDo;
        private System.Windows.Forms.DateTimePicker dt_VaziOd;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtNapomena;
        private System.Windows.Forms.Label label1;
    }
}