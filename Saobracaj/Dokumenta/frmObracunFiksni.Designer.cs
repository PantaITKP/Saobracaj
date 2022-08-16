
namespace Saobracaj.Dokumenta
{
    partial class frmObracunFiksni
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
            this.txtCenaSata = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.txtBrojSati = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.btnPostaviPrviDeo = new MetroFramework.Controls.MetroButton();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.label15 = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.metroButton2 = new MetroFramework.Controls.MetroButton();
            this.dtpVremeOd = new System.Windows.Forms.DateTimePicker();
            this.dtpVremeDo = new System.Windows.Forms.DateTimePicker();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.txtKurs = new System.Windows.Forms.NumericUpDown();
            this.txtBrRadnihDana = new System.Windows.Forms.TextBox();
            this.txtFondCasova = new System.Windows.Forms.TextBox();
            this.txtBrPraznicnihDana = new System.Windows.Forms.TextBox();
            this.txtPraznicniCasovi = new System.Windows.Forms.TextBox();
            this.txtMinimalnaCenaNeto = new System.Windows.Forms.NumericUpDown();
            this.txtMinimalnaZaradaNeto = new System.Windows.Forms.NumericUpDown();
            this.txtPoreskoOslobodjenje = new System.Windows.Forms.NumericUpDown();
            this.txtMinimalnaBrutoZarada = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.txtCenaSata)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtBrojSati)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtKurs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMinimalnaCenaNeto)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMinimalnaZaradaNeto)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPoreskoOslobodjenje)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMinimalnaBrutoZarada)).BeginInit();
            this.SuspendLayout();
            // 
            // txtCenaSata
            // 
            this.txtCenaSata.DecimalPlaces = 2;
            this.txtCenaSata.Enabled = false;
            this.txtCenaSata.Location = new System.Drawing.Point(398, 153);
            this.txtCenaSata.Margin = new System.Windows.Forms.Padding(4);
            this.txtCenaSata.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtCenaSata.Name = "txtCenaSata";
            this.txtCenaSata.Size = new System.Drawing.Size(96, 22);
            this.txtCenaSata.TabIndex = 151;
            this.txtCenaSata.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Enabled = false;
            this.label1.Location = new System.Drawing.Point(313, 153);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 17);
            this.label1.TabIndex = 150;
            this.label1.Text = "Cena sata:";
            // 
            // txtBrojSati
            // 
            this.txtBrojSati.DecimalPlaces = 2;
            this.txtBrojSati.Enabled = false;
            this.txtBrojSati.Location = new System.Drawing.Point(152, 153);
            this.txtBrojSati.Margin = new System.Windows.Forms.Padding(4);
            this.txtBrojSati.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtBrojSati.Name = "txtBrojSati";
            this.txtBrojSati.Size = new System.Drawing.Size(131, 22);
            this.txtBrojSati.TabIndex = 149;
            this.txtBrojSati.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Enabled = false;
            this.label2.Location = new System.Drawing.Point(17, 153);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(127, 17);
            this.label2.TabIndex = 148;
            this.label2.Text = "Broj mesečnih sati:";
            // 
            // btnPostaviPrviDeo
            // 
            this.btnPostaviPrviDeo.Location = new System.Drawing.Point(9, 201);
            this.btnPostaviPrviDeo.Margin = new System.Windows.Forms.Padding(4);
            this.btnPostaviPrviDeo.Name = "btnPostaviPrviDeo";
            this.btnPostaviPrviDeo.Size = new System.Drawing.Size(212, 28);
            this.btnPostaviPrviDeo.TabIndex = 152;
            this.btnPostaviPrviDeo.Text = "Povuci podatke";
            this.btnPostaviPrviDeo.UseSelectable = true;
            this.btnPostaviPrviDeo.Click += new System.EventHandler(this.btnPostaviPrviDeo_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(7, 248);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(4);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.Size = new System.Drawing.Size(1508, 397);
            this.dataGridView1.TabIndex = 153;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(758, 155);
            this.label15.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(73, 17);
            this.label15.TabIndex = 209;
            this.label15.Text = "Period do:";
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(525, 155);
            this.label21.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(73, 17);
            this.label21.TabIndex = 208;
            this.label21.Text = "Period od:";
            // 
            // metroButton2
            // 
            this.metroButton2.Enabled = false;
            this.metroButton2.Location = new System.Drawing.Point(317, 201);
            this.metroButton2.Margin = new System.Windows.Forms.Padding(4);
            this.metroButton2.Name = "metroButton2";
            this.metroButton2.Size = new System.Drawing.Size(212, 28);
            this.metroButton2.TabIndex = 210;
            this.metroButton2.Text = "Povuci sate";
            this.metroButton2.UseSelectable = true;
            this.metroButton2.Click += new System.EventHandler(this.metroButton2_Click);
            // 
            // dtpVremeOd
            // 
            this.dtpVremeOd.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dtpVremeOd.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeOd.Location = new System.Drawing.Point(606, 153);
            this.dtpVremeOd.Margin = new System.Windows.Forms.Padding(4);
            this.dtpVremeOd.Name = "dtpVremeOd";
            this.dtpVremeOd.ShowUpDown = true;
            this.dtpVremeOd.Size = new System.Drawing.Size(145, 22);
            this.dtpVremeOd.TabIndex = 215;
            this.dtpVremeOd.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // dtpVremeDo
            // 
            this.dtpVremeDo.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dtpVremeDo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeDo.Location = new System.Drawing.Point(840, 153);
            this.dtpVremeDo.Margin = new System.Windows.Forms.Padding(4);
            this.dtpVremeDo.Name = "dtpVremeDo";
            this.dtpVremeDo.ShowUpDown = true;
            this.dtpVremeDo.Size = new System.Drawing.Size(145, 22);
            this.dtpVremeDo.TabIndex = 214;
            this.dtpVremeDo.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(17, 23);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(143, 17);
            this.label3.TabIndex = 216;
            this.label3.Text = "Nbs srednji kurs EUR";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(191, 23);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(178, 17);
            this.label4.TabIndex = 216;
            this.label4.Text = "Broj radnih dana u mesecu";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(408, 23);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(141, 17);
            this.label5.TabIndex = 216;
            this.label5.Text = "Mesečni fond časova";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(586, 23);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(203, 17);
            this.label6.TabIndex = 216;
            this.label6.Text = "Broj prazničnih dana u mesecu";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(820, 23);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(207, 17);
            this.label7.TabIndex = 216;
            this.label7.Text = "Mesečni fond praznčnih časova";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(1063, 23);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(171, 17);
            this.label8.TabIndex = 216;
            this.label8.Text = "Minimalna cena rada neto";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(1266, 23);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(151, 17);
            this.label9.TabIndex = 216;
            this.label9.Text = "Minimalna neto zarada";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(17, 99);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(141, 17);
            this.label10.TabIndex = 216;
            this.label10.Text = "Poresko oslobođenje";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(314, 101);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(156, 17);
            this.label11.TabIndex = 216;
            this.label11.Text = "Minimalna bruto zarada";
            // 
            // txtKurs
            // 
            this.txtKurs.DecimalPlaces = 2;
            this.txtKurs.Location = new System.Drawing.Point(20, 44);
            this.txtKurs.Margin = new System.Windows.Forms.Padding(4);
            this.txtKurs.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtKurs.Name = "txtKurs";
            this.txtKurs.Size = new System.Drawing.Size(97, 22);
            this.txtKurs.TabIndex = 217;
            this.txtKurs.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // txtBrRadnihDana
            // 
            this.txtBrRadnihDana.Location = new System.Drawing.Point(227, 44);
            this.txtBrRadnihDana.Name = "txtBrRadnihDana";
            this.txtBrRadnihDana.Size = new System.Drawing.Size(100, 22);
            this.txtBrRadnihDana.TabIndex = 218;
            // 
            // txtFondCasova
            // 
            this.txtFondCasova.Location = new System.Drawing.Point(420, 43);
            this.txtFondCasova.Name = "txtFondCasova";
            this.txtFondCasova.Size = new System.Drawing.Size(100, 22);
            this.txtFondCasova.TabIndex = 218;
            // 
            // txtBrPraznicnihDana
            // 
            this.txtBrPraznicnihDana.Location = new System.Drawing.Point(651, 44);
            this.txtBrPraznicnihDana.Name = "txtBrPraznicnihDana";
            this.txtBrPraznicnihDana.Size = new System.Drawing.Size(100, 22);
            this.txtBrPraznicnihDana.TabIndex = 218;
            // 
            // txtPraznicniCasovi
            // 
            this.txtPraznicniCasovi.Location = new System.Drawing.Point(864, 43);
            this.txtPraznicniCasovi.Name = "txtPraznicniCasovi";
            this.txtPraznicniCasovi.Size = new System.Drawing.Size(100, 22);
            this.txtPraznicniCasovi.TabIndex = 218;
            // 
            // txtMinimalnaCenaNeto
            // 
            this.txtMinimalnaCenaNeto.DecimalPlaces = 2;
            this.txtMinimalnaCenaNeto.Location = new System.Drawing.Point(1082, 44);
            this.txtMinimalnaCenaNeto.Margin = new System.Windows.Forms.Padding(4);
            this.txtMinimalnaCenaNeto.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtMinimalnaCenaNeto.Name = "txtMinimalnaCenaNeto";
            this.txtMinimalnaCenaNeto.Size = new System.Drawing.Size(126, 22);
            this.txtMinimalnaCenaNeto.TabIndex = 217;
            this.txtMinimalnaCenaNeto.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // txtMinimalnaZaradaNeto
            // 
            this.txtMinimalnaZaradaNeto.DecimalPlaces = 2;
            this.txtMinimalnaZaradaNeto.Location = new System.Drawing.Point(1283, 45);
            this.txtMinimalnaZaradaNeto.Margin = new System.Windows.Forms.Padding(4);
            this.txtMinimalnaZaradaNeto.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtMinimalnaZaradaNeto.Name = "txtMinimalnaZaradaNeto";
            this.txtMinimalnaZaradaNeto.Size = new System.Drawing.Size(126, 22);
            this.txtMinimalnaZaradaNeto.TabIndex = 217;
            this.txtMinimalnaZaradaNeto.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // txtPoreskoOslobodjenje
            // 
            this.txtPoreskoOslobodjenje.DecimalPlaces = 2;
            this.txtPoreskoOslobodjenje.Location = new System.Drawing.Point(158, 97);
            this.txtPoreskoOslobodjenje.Margin = new System.Windows.Forms.Padding(4);
            this.txtPoreskoOslobodjenje.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtPoreskoOslobodjenje.Name = "txtPoreskoOslobodjenje";
            this.txtPoreskoOslobodjenje.Size = new System.Drawing.Size(125, 22);
            this.txtPoreskoOslobodjenje.TabIndex = 217;
            this.txtPoreskoOslobodjenje.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // txtMinimalnaBrutoZarada
            // 
            this.txtMinimalnaBrutoZarada.DecimalPlaces = 2;
            this.txtMinimalnaBrutoZarada.Location = new System.Drawing.Point(477, 99);
            this.txtMinimalnaBrutoZarada.Margin = new System.Windows.Forms.Padding(4);
            this.txtMinimalnaBrutoZarada.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtMinimalnaBrutoZarada.Name = "txtMinimalnaBrutoZarada";
            this.txtMinimalnaBrutoZarada.Size = new System.Drawing.Size(138, 22);
            this.txtMinimalnaBrutoZarada.TabIndex = 217;
            this.txtMinimalnaBrutoZarada.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // frmObracunFiksni
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1531, 649);
            this.Controls.Add(this.txtPraznicniCasovi);
            this.Controls.Add(this.txtBrPraznicnihDana);
            this.Controls.Add(this.txtFondCasova);
            this.Controls.Add(this.txtBrRadnihDana);
            this.Controls.Add(this.txtMinimalnaBrutoZarada);
            this.Controls.Add(this.txtPoreskoOslobodjenje);
            this.Controls.Add(this.txtMinimalnaZaradaNeto);
            this.Controls.Add(this.txtMinimalnaCenaNeto);
            this.Controls.Add(this.txtKurs);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.dtpVremeOd);
            this.Controls.Add(this.dtpVremeDo);
            this.Controls.Add(this.metroButton2);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.label21);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.btnPostaviPrviDeo);
            this.Controls.Add(this.txtCenaSata);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtBrojSati);
            this.Controls.Add(this.label2);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "frmObracunFiksni";
            this.Text = "Obračun zarade";
            ((System.ComponentModel.ISupportInitialize)(this.txtCenaSata)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtBrojSati)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtKurs)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMinimalnaCenaNeto)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMinimalnaZaradaNeto)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPoreskoOslobodjenje)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMinimalnaBrutoZarada)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NumericUpDown txtCenaSata;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown txtBrojSati;
        private System.Windows.Forms.Label label2;
        private MetroFramework.Controls.MetroButton btnPostaviPrviDeo;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label21;
        private MetroFramework.Controls.MetroButton metroButton2;
        private System.Windows.Forms.DateTimePicker dtpVremeOd;
        private System.Windows.Forms.DateTimePicker dtpVremeDo;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.NumericUpDown txtKurs;
        private System.Windows.Forms.TextBox txtBrRadnihDana;
        private System.Windows.Forms.TextBox txtFondCasova;
        private System.Windows.Forms.TextBox txtBrPraznicnihDana;
        private System.Windows.Forms.TextBox txtPraznicniCasovi;
        private System.Windows.Forms.NumericUpDown txtMinimalnaCenaNeto;
        private System.Windows.Forms.NumericUpDown txtMinimalnaZaradaNeto;
        private System.Windows.Forms.NumericUpDown txtPoreskoOslobodjenje;
        private System.Windows.Forms.NumericUpDown txtMinimalnaBrutoZarada;
    }
}