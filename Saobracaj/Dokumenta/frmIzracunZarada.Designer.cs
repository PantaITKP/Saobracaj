namespace Saobracaj.Dokumenta
{
    partial class frmIzracunZarada
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
            this.dtpVremeDo = new System.Windows.Forms.DateTimePicker();
            this.label15 = new System.Windows.Forms.Label();
            this.dtpVremeOd = new System.Windows.Forms.DateTimePicker();
            this.label21 = new System.Windows.Forms.Label();
            this.btnIzracunaj = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.label1 = new System.Windows.Forms.Label();
            this.txtKurs = new System.Windows.Forms.NumericUpDown();
            this.txtZarada = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.txtSmanjenje = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.txtMinimalac = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.button4 = new System.Windows.Forms.Button();
            this.button5 = new System.Windows.Forms.Button();
            this.button7 = new System.Windows.Forms.Button();
            this.button8 = new System.Windows.Forms.Button();
            this.dtpZakljucavanjeSmene = new System.Windows.Forms.DateTimePicker();
            this.label5 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.button9 = new System.Windows.Forms.Button();
            this.button10 = new System.Windows.Forms.Button();
            this.button11 = new System.Windows.Forms.Button();
            this.txtSatiMesec = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            this.dtpVremeOd2 = new System.Windows.Forms.DateTimePicker();
            this.dtpVremeDo2 = new System.Windows.Forms.DateTimePicker();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.button12 = new System.Windows.Forms.Button();
            this.txtPoreskoOslobodjenje = new System.Windows.Forms.NumericUpDown();
            this.label11 = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.dataGridView2 = new System.Windows.Forms.DataGridView();
            this.txtPrazniciFond = new System.Windows.Forms.NumericUpDown();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtKurs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtZarada)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSmanjenje)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMinimalac)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSatiMesec)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPoreskoOslobodjenje)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPrazniciFond)).BeginInit();
            this.SuspendLayout();
            // 
            // dtpVremeDo
            // 
            this.dtpVremeDo.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpVremeDo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeDo.Location = new System.Drawing.Point(254, 24);
            this.dtpVremeDo.Name = "dtpVremeDo";
            this.dtpVremeDo.ShowUpDown = true;
            this.dtpVremeDo.Size = new System.Drawing.Size(115, 20);
            this.dtpVremeDo.TabIndex = 273;
            this.dtpVremeDo.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            this.dtpVremeDo.ValueChanged += new System.EventHandler(this.dtpVremeDo_ValueChanged);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(193, 25);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(55, 13);
            this.label15.TabIndex = 275;
            this.label15.Text = "Period do:";
            // 
            // dtpVremeOd
            // 
            this.dtpVremeOd.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpVremeOd.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeOd.Location = new System.Drawing.Point(79, 25);
            this.dtpVremeOd.Name = "dtpVremeOd";
            this.dtpVremeOd.ShowUpDown = true;
            this.dtpVremeOd.Size = new System.Drawing.Size(108, 20);
            this.dtpVremeOd.TabIndex = 272;
            this.dtpVremeOd.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(14, 24);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(55, 13);
            this.label21.TabIndex = 274;
            this.label21.Text = "Period od:";
            // 
            // btnIzracunaj
            // 
            this.btnIzracunaj.Location = new System.Drawing.Point(385, 21);
            this.btnIzracunaj.Name = "btnIzracunaj";
            this.btnIzracunaj.Size = new System.Drawing.Size(143, 24);
            this.btnIzracunaj.TabIndex = 276;
            this.btnIzracunaj.Text = "Izračunaj 1";
            this.btnIzracunaj.UseVisualStyleBackColor = true;
            this.btnIzracunaj.Click += new System.EventHandler(this.btnIzracunaj_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(5, 6);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.Size = new System.Drawing.Size(1109, 318);
            this.dataGridView1.TabIndex = 277;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 84);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(31, 13);
            this.label1.TabIndex = 279;
            this.label1.Text = "Kurs:";
            // 
            // txtKurs
            // 
            this.txtKurs.DecimalPlaces = 2;
            this.txtKurs.Location = new System.Drawing.Point(80, 84);
            this.txtKurs.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtKurs.Name = "txtKurs";
            this.txtKurs.Size = new System.Drawing.Size(69, 20);
            this.txtKurs.TabIndex = 280;
            this.txtKurs.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // txtZarada
            // 
            this.txtZarada.DecimalPlaces = 2;
            this.txtZarada.Location = new System.Drawing.Point(278, 84);
            this.txtZarada.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtZarada.Name = "txtZarada";
            this.txtZarada.Size = new System.Drawing.Size(81, 20);
            this.txtZarada.TabIndex = 283;
            this.txtZarada.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(182, 84);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(91, 13);
            this.label2.TabIndex = 282;
            this.label2.Text = "Nova Zarada eur:";
            // 
            // txtSmanjenje
            // 
            this.txtSmanjenje.DecimalPlaces = 2;
            this.txtSmanjenje.Location = new System.Drawing.Point(76, 117);
            this.txtSmanjenje.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtSmanjenje.Name = "txtSmanjenje";
            this.txtSmanjenje.Size = new System.Drawing.Size(81, 20);
            this.txtSmanjenje.TabIndex = 285;
            this.txtSmanjenje.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 116);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(40, 13);
            this.label3.TabIndex = 284;
            this.label3.Text = "Kazne:";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(364, 82);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(25, 23);
            this.button2.TabIndex = 286;
            this.button2.Text = "?";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(163, 119);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(25, 19);
            this.button3.TabIndex = 287;
            this.button3.Text = "? - 3";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // txtMinimalac
            // 
            this.txtMinimalac.DecimalPlaces = 2;
            this.txtMinimalac.Location = new System.Drawing.Point(417, 123);
            this.txtMinimalac.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtMinimalac.Name = "txtMinimalac";
            this.txtMinimalac.Size = new System.Drawing.Size(69, 20);
            this.txtMinimalac.TabIndex = 289;
            this.txtMinimalac.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(356, 123);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(57, 13);
            this.label4.TabIndex = 288;
            this.label4.Text = "Minimalac:";
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(534, 91);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(134, 24);
            this.button4.TabIndex = 290;
            this.button4.Text = "Minimalac 2";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(677, 16);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(194, 28);
            this.button5.TabIndex = 291;
            this.button5.Text = "PN 1";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // button7
            // 
            this.button7.Location = new System.Drawing.Point(677, 50);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(194, 28);
            this.button7.TabIndex = 293;
            this.button7.Text = "PN - 2";
            this.button7.UseVisualStyleBackColor = true;
            this.button7.Click += new System.EventHandler(this.button7_Click);
            // 
            // button8
            // 
            this.button8.Location = new System.Drawing.Point(677, 84);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(194, 30);
            this.button8.TabIndex = 294;
            this.button8.Text = "Brisanje viška PN - 3";
            this.button8.UseVisualStyleBackColor = true;
            this.button8.Click += new System.EventHandler(this.button8_Click);
            // 
            // dtpZakljucavanjeSmene
            // 
            this.dtpZakljucavanjeSmene.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpZakljucavanjeSmene.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpZakljucavanjeSmene.Location = new System.Drawing.Point(909, 25);
            this.dtpZakljucavanjeSmene.Name = "dtpZakljucavanjeSmene";
            this.dtpZakljucavanjeSmene.ShowUpDown = true;
            this.dtpZakljucavanjeSmene.Size = new System.Drawing.Size(115, 20);
            this.dtpZakljucavanjeSmene.TabIndex = 295;
            this.dtpZakljucavanjeSmene.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(906, 9);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(116, 13);
            this.label5.TabIndex = 296;
            this.label5.Text = "Vreme početka smene:";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(909, 50);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(194, 30);
            this.button1.TabIndex = 297;
            this.button1.Text = "Zaključavanje smena";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click_2);
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(909, 84);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(194, 30);
            this.button6.TabIndex = 298;
            this.button6.Text = "Arhiviranje smena u izradi";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click_1);
            // 
            // txtPassword
            // 
            this.txtPassword.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.txtPassword.Location = new System.Drawing.Point(49, 184);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(100, 20);
            this.txtPassword.TabIndex = 301;
            this.txtPassword.UseSystemPasswordChar = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(15, 184);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(28, 13);
            this.label6.TabIndex = 299;
            this.label6.Text = "Šifra";
            // 
            // button9
            // 
            this.button9.Location = new System.Drawing.Point(154, 184);
            this.button9.Name = "button9";
            this.button9.Size = new System.Drawing.Size(131, 23);
            this.button9.TabIndex = 302;
            this.button9.Text = "Refresh";
            this.button9.UseVisualStyleBackColor = true;
            this.button9.Click += new System.EventHandler(this.button9_Click_1);
            // 
            // button10
            // 
            this.button10.Location = new System.Drawing.Point(534, 21);
            this.button10.Name = "button10";
            this.button10.Size = new System.Drawing.Size(127, 24);
            this.button10.TabIndex = 303;
            this.button10.Text = "Uključi prevoz";
            this.button10.UseVisualStyleBackColor = true;
            this.button10.Click += new System.EventHandler(this.button10_Click);
            // 
            // button11
            // 
            this.button11.ForeColor = System.Drawing.Color.Red;
            this.button11.Location = new System.Drawing.Point(291, 184);
            this.button11.Name = "button11";
            this.button11.Size = new System.Drawing.Size(177, 23);
            this.button11.TabIndex = 304;
            this.button11.Text = "Forma Obracuna za fiksne";
            this.button11.UseVisualStyleBackColor = true;
            this.button11.Click += new System.EventHandler(this.button11_Click);
            // 
            // txtSatiMesec
            // 
            this.txtSatiMesec.DecimalPlaces = 2;
            this.txtSatiMesec.Location = new System.Drawing.Point(272, 115);
            this.txtSatiMesec.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtSatiMesec.Name = "txtSatiMesec";
            this.txtSatiMesec.Size = new System.Drawing.Size(69, 20);
            this.txtSatiMesec.TabIndex = 310;
            this.txtSatiMesec.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.ForeColor = System.Drawing.Color.Red;
            this.label9.Location = new System.Drawing.Point(206, 116);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(62, 13);
            this.label9.TabIndex = 309;
            this.label9.Text = "Sati mesec:";
            // 
            // dtpVremeOd2
            // 
            this.dtpVremeOd2.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dtpVremeOd2.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeOd2.Location = new System.Drawing.Point(71, 63);
            this.dtpVremeOd2.Name = "dtpVremeOd2";
            this.dtpVremeOd2.ShowUpDown = true;
            this.dtpVremeOd2.Size = new System.Drawing.Size(110, 20);
            this.dtpVremeOd2.TabIndex = 314;
            this.dtpVremeOd2.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // dtpVremeDo2
            // 
            this.dtpVremeDo2.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dtpVremeDo2.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeDo2.Location = new System.Drawing.Point(247, 63);
            this.dtpVremeDo2.Name = "dtpVremeDo2";
            this.dtpVremeDo2.ShowUpDown = true;
            this.dtpVremeDo2.Size = new System.Drawing.Size(115, 20);
            this.dtpVremeDo2.TabIndex = 313;
            this.dtpVremeDo2.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.ForeColor = System.Drawing.Color.Red;
            this.label7.Location = new System.Drawing.Point(185, 64);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(55, 13);
            this.label7.TabIndex = 312;
            this.label7.Text = "Period do:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.ForeColor = System.Drawing.Color.Red;
            this.label8.Location = new System.Drawing.Point(10, 64);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(55, 13);
            this.label8.TabIndex = 311;
            this.label8.Text = "Period od:";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.ForeColor = System.Drawing.Color.Red;
            this.label10.Location = new System.Drawing.Point(10, 46);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(243, 13);
            this.label10.TabIndex = 339;
            this.label10.Text = "Prekucati isti datum dok se ne nadje bolje resenje:";
            // 
            // button12
            // 
            this.button12.Location = new System.Drawing.Point(484, 160);
            this.button12.Name = "button12";
            this.button12.Size = new System.Drawing.Size(177, 46);
            this.button12.TabIndex = 340;
            this.button12.Text = "Prikazi GAP - nema ih u obracunu/Nisu Pasivni";
            this.button12.UseVisualStyleBackColor = true;
            this.button12.Click += new System.EventHandler(this.button12_Click);
            // 
            // txtPoreskoOslobodjenje
            // 
            this.txtPoreskoOslobodjenje.DecimalPlaces = 2;
            this.txtPoreskoOslobodjenje.Location = new System.Drawing.Point(618, 123);
            this.txtPoreskoOslobodjenje.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtPoreskoOslobodjenje.Name = "txtPoreskoOslobodjenje";
            this.txtPoreskoOslobodjenje.Size = new System.Drawing.Size(69, 20);
            this.txtPoreskoOslobodjenje.TabIndex = 342;
            this.txtPoreskoOslobodjenje.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(501, 123);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(111, 13);
            this.label11.TabIndex = 341;
            this.label11.Text = "Poresko oslobodjenje:";
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(13, 219);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1126, 353);
            this.tabControl1.TabIndex = 343;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.dataGridView1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabPage1.Size = new System.Drawing.Size(1118, 327);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Stari obračun";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.dataGridView2);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabPage2.Size = new System.Drawing.Size(1118, 327);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Nataša";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // dataGridView2
            // 
            this.dataGridView2.AllowUserToAddRows = false;
            this.dataGridView2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView2.Location = new System.Drawing.Point(5, 6);
            this.dataGridView2.Name = "dataGridView2";
            this.dataGridView2.RowHeadersWidth = 51;
            this.dataGridView2.Size = new System.Drawing.Size(1109, 318);
            this.dataGridView2.TabIndex = 278;
            // 
            // txtPrazniciFond
            // 
            this.txtPrazniciFond.Location = new System.Drawing.Point(787, 167);
            this.txtPrazniciFond.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtPrazniciFond.Name = "txtPrazniciFond";
            this.txtPrazniciFond.Size = new System.Drawing.Size(69, 20);
            this.txtPrazniciFond.TabIndex = 376;
            this.txtPrazniciFond.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(710, 168);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(71, 13);
            this.label12.TabIndex = 375;
            this.label12.Text = "Praznici fond:";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.BackColor = System.Drawing.Color.Red;
            this.label13.ForeColor = System.Drawing.Color.White;
            this.label13.Location = new System.Drawing.Point(779, 204);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(324, 13);
            this.label13.TabIndex = 377;
            this.label13.Text = "Bonuse dodaje na UkupnoRSD na sp - SelectObracunMMVPrevoz";
            // 
            // frmIzracunZarada
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1143, 574);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.txtPrazniciFond);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.txtPoreskoOslobodjenje);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.button12);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.dtpVremeOd2);
            this.Controls.Add(this.dtpVremeDo2);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.txtSatiMesec);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.button11);
            this.Controls.Add(this.button10);
            this.Controls.Add(this.button9);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.dtpZakljucavanjeSmene);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.button8);
            this.Controls.Add(this.button7);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.txtMinimalac);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.txtSmanjenje);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtZarada);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtKurs);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnIzracunaj);
            this.Controls.Add(this.dtpVremeDo);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.dtpVremeOd);
            this.Controls.Add(this.label21);
            this.Name = "frmIzracunZarada";
            this.Text = "Izraćun Zarada";
            this.Load += new System.EventHandler(this.frmIzracunZarada_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtKurs)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtZarada)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSmanjenje)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMinimalac)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSatiMesec)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPoreskoOslobodjenje)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPrazniciFond)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DateTimePicker dtpVremeDo;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.DateTimePicker dtpVremeOd;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Button btnIzracunaj;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown txtKurs;
        private System.Windows.Forms.NumericUpDown txtZarada;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown txtSmanjenje;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.NumericUpDown txtMinimalac;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.Button button8;
        private System.Windows.Forms.DateTimePicker dtpZakljucavanjeSmene;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button button9;
        private System.Windows.Forms.Button button10;
        private System.Windows.Forms.Button button11;
        private System.Windows.Forms.NumericUpDown txtSatiMesec;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.DateTimePicker dtpVremeOd2;
        private System.Windows.Forms.DateTimePicker dtpVremeDo2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button button12;
        private System.Windows.Forms.NumericUpDown txtPoreskoOslobodjenje;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.DataGridView dataGridView2;
        private System.Windows.Forms.NumericUpDown txtPrazniciFond;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
    }
}