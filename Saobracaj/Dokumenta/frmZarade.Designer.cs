namespace Saobracaj.Dokumenta
{
    partial class frmZarade
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
            this.components = new System.ComponentModel.Container();
            Microsoft.Reporting.WinForms.ReportDataSource reportDataSource3 = new Microsoft.Reporting.WinForms.ReportDataSource();
            this.perftechBeogradDataSet3BindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.perftech_BeogradDataSet3 = new Saobracaj.Perftech_BeogradDataSet3();
            this.btnStampa = new System.Windows.Forms.Button();
            this.reportViewer1 = new Microsoft.Reporting.WinForms.ReportViewer();
            this.label6 = new System.Windows.Forms.Label();
            this.cboZaposleni = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.dtpVremeDo = new System.Windows.Forms.DateTimePicker();
            this.dtpVremeOd = new System.Windows.Forms.DateTimePicker();
            this.chkMilsped = new System.Windows.Forms.CheckBox();
            this.chkLokomotiva = new System.Windows.Forms.CheckBox();
            this.chkVanLokomotive = new System.Windows.Forms.CheckBox();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.perftechBeogradDataSet3BindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.perftech_BeogradDataSet3)).BeginInit();
            this.SuspendLayout();
            // 
            // perftechBeogradDataSet3BindingSource
            // 
            this.perftechBeogradDataSet3BindingSource.DataSource = this.perftech_BeogradDataSet3;
            this.perftechBeogradDataSet3BindingSource.Position = 0;
            // 
            // perftech_BeogradDataSet3
            // 
            this.perftech_BeogradDataSet3.DataSetName = "Perftech_BeogradDataSet3";
            this.perftech_BeogradDataSet3.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
            // 
            // btnStampa
            // 
            this.btnStampa.Location = new System.Drawing.Point(743, 8);
            this.btnStampa.Name = "btnStampa";
            this.btnStampa.Size = new System.Drawing.Size(89, 23);
            this.btnStampa.TabIndex = 90;
            this.btnStampa.Text = "Štampaj";
            this.btnStampa.UseVisualStyleBackColor = true;
            this.btnStampa.Click += new System.EventHandler(this.btnStampa_Click);
            // 
            // reportViewer1
            // 
            this.reportViewer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            reportDataSource3.Name = "DataSetZarada";
            reportDataSource3.Value = this.perftechBeogradDataSet3BindingSource;
            this.reportViewer1.LocalReport.DataSources.Add(reportDataSource3);
            this.reportViewer1.LocalReport.ReportEmbeddedResource = "Saobracaj.Dokumenta.Zarada.rdlc";
            this.reportViewer1.Location = new System.Drawing.Point(13, 72);
            this.reportViewer1.Name = "reportViewer1";
            this.reportViewer1.ServerReport.BearerToken = null;
            this.reportViewer1.Size = new System.Drawing.Size(819, 316);
            this.reportViewer1.TabIndex = 91;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(11, 12);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(56, 13);
            this.label6.TabIndex = 93;
            this.label6.Text = "Zaposleni:";
            // 
            // cboZaposleni
            // 
            this.cboZaposleni.BackColor = System.Drawing.Color.White;
            this.cboZaposleni.FormattingEnabled = true;
            this.cboZaposleni.Location = new System.Drawing.Point(73, 12);
            this.cboZaposleni.Name = "cboZaposleni";
            this.cboZaposleni.Size = new System.Drawing.Size(144, 21);
            this.cboZaposleni.TabIndex = 92;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(397, 13);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(51, 13);
            this.label2.TabIndex = 112;
            this.label2.Text = "VremeDo";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(224, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 13);
            this.label1.TabIndex = 111;
            this.label1.Text = "VremeOd";
            // 
            // dtpVremeDo
            // 
            this.dtpVremeDo.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpVremeDo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeDo.Location = new System.Drawing.Point(454, 11);
            this.dtpVremeDo.Name = "dtpVremeDo";
            this.dtpVremeDo.ShowUpDown = true;
            this.dtpVremeDo.Size = new System.Drawing.Size(110, 20);
            this.dtpVremeDo.TabIndex = 110;
            this.dtpVremeDo.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // dtpVremeOd
            // 
            this.dtpVremeOd.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpVremeOd.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeOd.Location = new System.Drawing.Point(281, 11);
            this.dtpVremeOd.Name = "dtpVremeOd";
            this.dtpVremeOd.ShowUpDown = true;
            this.dtpVremeOd.Size = new System.Drawing.Size(110, 20);
            this.dtpVremeOd.TabIndex = 109;
            this.dtpVremeOd.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // chkMilsped
            // 
            this.chkMilsped.AutoSize = true;
            this.chkMilsped.Location = new System.Drawing.Point(579, 14);
            this.chkMilsped.Name = "chkMilsped";
            this.chkMilsped.Size = new System.Drawing.Size(62, 17);
            this.chkMilsped.TabIndex = 113;
            this.chkMilsped.Text = "Milšped";
            this.chkMilsped.UseVisualStyleBackColor = true;
            // 
            // chkLokomotiva
            // 
            this.chkLokomotiva.AutoSize = true;
            this.chkLokomotiva.Location = new System.Drawing.Point(647, 14);
            this.chkLokomotiva.Name = "chkLokomotiva";
            this.chkLokomotiva.Size = new System.Drawing.Size(81, 17);
            this.chkLokomotiva.TabIndex = 114;
            this.chkLokomotiva.Text = "Lokomotiva";
            this.chkLokomotiva.UseVisualStyleBackColor = true;
            // 
            // chkVanLokomotive
            // 
            this.chkVanLokomotive.AutoSize = true;
            this.chkVanLokomotive.Location = new System.Drawing.Point(647, 37);
            this.chkVanLokomotive.Name = "chkVanLokomotive";
            this.chkVanLokomotive.Size = new System.Drawing.Size(103, 17);
            this.chkVanLokomotive.TabIndex = 115;
            this.chkVanLokomotive.Text = "Van Lokomotive";
            this.chkVanLokomotive.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(756, 37);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(76, 23);
            this.button1.TabIndex = 90;
            this.button1.Text = "Potvrdi";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // frmZarade
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(844, 400);
            this.Controls.Add(this.chkVanLokomotive);
            this.Controls.Add(this.chkLokomotiva);
            this.Controls.Add(this.chkMilsped);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.dtpVremeDo);
            this.Controls.Add(this.dtpVremeOd);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.cboZaposleni);
            this.Controls.Add(this.reportViewer1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnStampa);
            this.Name = "frmZarade";
            this.Text = "Obračun po satu";
            this.Load += new System.EventHandler(this.frmZarade_Load);
            this.Click += new System.EventHandler(this.frmZarade_Click);
            ((System.ComponentModel.ISupportInitialize)(this.perftechBeogradDataSet3BindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.perftech_BeogradDataSet3)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnStampa;
        private Microsoft.Reporting.WinForms.ReportViewer reportViewer1;
        private System.Windows.Forms.BindingSource perftechBeogradDataSet3BindingSource;
        private Perftech_BeogradDataSet3 perftech_BeogradDataSet3;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cboZaposleni;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker dtpVremeDo;
        private System.Windows.Forms.DateTimePicker dtpVremeOd;
        private System.Windows.Forms.CheckBox chkMilsped;
        private System.Windows.Forms.CheckBox chkLokomotiva;
        private System.Windows.Forms.CheckBox chkVanLokomotive;
        private System.Windows.Forms.Button button1;
    }
}