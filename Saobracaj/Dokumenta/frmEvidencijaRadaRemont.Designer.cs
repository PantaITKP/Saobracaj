﻿namespace Saobracaj.Dokumenta
{
    partial class frmEvidencijaRadaRemont
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmEvidencijaRadaRemont));
            this.txtRad = new System.Windows.Forms.NumericUpDown();
            this.txtMesto = new System.Windows.Forms.TextBox();
            this.label22 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.txtKartica = new System.Windows.Forms.TextBox();
            this.label19 = new System.Windows.Forms.Label();
            this.txtRacun = new System.Windows.Forms.TextBox();
            this.chkPlaceno = new System.Windows.Forms.CheckBox();
            this.chkPoslatMail = new System.Windows.Forms.CheckBox();
            this.label18 = new System.Windows.Forms.Label();
            this.txtEmail = new System.Windows.Forms.TextBox();
            this.btnPosaljiMail = new System.Windows.Forms.Button();
            this.txtOznaka = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.chkUnosMasinovođa = new System.Windows.Forms.CheckBox();
            this.txtUkupnoMašinovođa = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.cboVozilo = new System.Windows.Forms.ComboBox();
            this.label14 = new System.Windows.Forms.Label();
            this.cboNalogodavac = new System.Windows.Forms.ComboBox();
            this.label13 = new System.Windows.Forms.Label();
            this.txtRazlog = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.txtBrojVagona = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnUnesi = new System.Windows.Forms.Button();
            this.label11 = new System.Windows.Forms.Label();
            this.dataGridView2 = new System.Windows.Forms.DataGridView();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.txtDodatnaNapomena = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.txtKoeficijent = new System.Windows.Forms.TextBox();
            this.txtVreme = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtTrosak = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.dtpVremeDo = new System.Windows.Forms.DateTimePicker();
            this.label15 = new System.Windows.Forms.Label();
            this.dtpVremeOd = new System.Windows.Forms.DateTimePicker();
            this.label21 = new System.Windows.Forms.Label();
            this.cboAktivnost = new System.Windows.Forms.ComboBox();
            this.btnUbaciAktivnost = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.txtKomentar = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.cboRadniNalog = new System.Windows.Forms.ComboBox();
            this.cboZaposleni = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtSifra = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tsNapred = new System.Windows.Forms.ToolStripButton();
            this.tsNazad = new System.Windows.Forms.ToolStripButton();
            this.tsPrvi = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsDelete = new System.Windows.Forms.ToolStripButton();
            this.tsSave = new System.Windows.Forms.ToolStripButton();
            this.tsNew = new System.Windows.Forms.ToolStripButton();
            this.tsPoslednja = new System.Windows.Forms.ToolStripButton();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.dtpStavke = new System.Windows.Forms.DateTimePicker();
            this.label29 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.txtRad)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtRad
            // 
            this.txtRad.DecimalPlaces = 2;
            this.txtRad.Location = new System.Drawing.Point(498, 127);
            this.txtRad.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtRad.Name = "txtRad";
            this.txtRad.Size = new System.Drawing.Size(69, 20);
            this.txtRad.TabIndex = 359;
            this.txtRad.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // txtMesto
            // 
            this.txtMesto.Location = new System.Drawing.Point(359, 75);
            this.txtMesto.Name = "txtMesto";
            this.txtMesto.Size = new System.Drawing.Size(120, 20);
            this.txtMesto.TabIndex = 358;
            this.txtMesto.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(314, 77);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(39, 13);
            this.label22.TabIndex = 357;
            this.label22.Text = "Mesto:";
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Enabled = false;
            this.label20.Location = new System.Drawing.Point(306, 108);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(43, 13);
            this.label20.TabIndex = 355;
            this.label20.Text = "Kartica:";
            // 
            // txtKartica
            // 
            this.txtKartica.Enabled = false;
            this.txtKartica.Location = new System.Drawing.Point(355, 105);
            this.txtKartica.Name = "txtKartica";
            this.txtKartica.Size = new System.Drawing.Size(48, 20);
            this.txtKartica.TabIndex = 356;
            this.txtKartica.Text = "0";
            this.txtKartica.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(156, 108);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(75, 13);
            this.label19.TabIndex = 353;
            this.label19.Text = "Trošak računi:";
            // 
            // txtRacun
            // 
            this.txtRacun.Enabled = false;
            this.txtRacun.Location = new System.Drawing.Point(237, 103);
            this.txtRacun.Name = "txtRacun";
            this.txtRacun.Size = new System.Drawing.Size(48, 20);
            this.txtRacun.TabIndex = 354;
            this.txtRacun.Text = "0";
            this.txtRacun.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // chkPlaceno
            // 
            this.chkPlaceno.AutoSize = true;
            this.chkPlaceno.Location = new System.Drawing.Point(535, 103);
            this.chkPlaceno.Name = "chkPlaceno";
            this.chkPlaceno.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkPlaceno.Size = new System.Drawing.Size(65, 17);
            this.chkPlaceno.TabIndex = 352;
            this.chkPlaceno.Text = "Plaćeno";
            this.chkPlaceno.UseVisualStyleBackColor = true;
            // 
            // chkPoslatMail
            // 
            this.chkPoslatMail.AutoSize = true;
            this.chkPoslatMail.Location = new System.Drawing.Point(423, 104);
            this.chkPoslatMail.Name = "chkPoslatMail";
            this.chkPoslatMail.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkPoslatMail.Size = new System.Drawing.Size(83, 17);
            this.chkPoslatMail.TabIndex = 351;
            this.chkPoslatMail.Text = "Poslat Email";
            this.chkPoslatMail.UseVisualStyleBackColor = true;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(946, 158);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(35, 13);
            this.label18.TabIndex = 350;
            this.label18.Text = "Email:";
            // 
            // txtEmail
            // 
            this.txtEmail.Location = new System.Drawing.Point(981, 157);
            this.txtEmail.Name = "txtEmail";
            this.txtEmail.Size = new System.Drawing.Size(204, 20);
            this.txtEmail.TabIndex = 349;
            // 
            // btnPosaljiMail
            // 
            this.btnPosaljiMail.Location = new System.Drawing.Point(981, 180);
            this.btnPosaljiMail.Name = "btnPosaljiMail";
            this.btnPosaljiMail.Size = new System.Drawing.Size(204, 24);
            this.btnPosaljiMail.TabIndex = 348;
            this.btnPosaljiMail.Text = "Pošalji mail";
            this.btnPosaljiMail.UseVisualStyleBackColor = true;
            this.btnPosaljiMail.Click += new System.EventHandler(this.btnPosaljiMail_Click);
            // 
            // txtOznaka
            // 
            this.txtOznaka.Enabled = false;
            this.txtOznaka.Location = new System.Drawing.Point(77, 77);
            this.txtOznaka.Name = "txtOznaka";
            this.txtOznaka.Size = new System.Drawing.Size(52, 20);
            this.txtOznaka.TabIndex = 346;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Enabled = false;
            this.label17.Location = new System.Drawing.Point(7, 77);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(54, 13);
            this.label17.TabIndex = 347;
            this.label17.Text = "Broj ot/pr:";
            // 
            // chkUnosMasinovođa
            // 
            this.chkUnosMasinovođa.AutoSize = true;
            this.chkUnosMasinovođa.Checked = true;
            this.chkUnosMasinovođa.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkUnosMasinovođa.Location = new System.Drawing.Point(530, 75);
            this.chkUnosMasinovođa.Name = "chkUnosMasinovođa";
            this.chkUnosMasinovođa.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkUnosMasinovođa.Size = new System.Drawing.Size(126, 17);
            this.chkUnosMasinovođa.TabIndex = 345;
            this.chkUnosMasinovođa.Text = "Unos za mašinovođu";
            this.chkUnosMasinovođa.UseVisualStyleBackColor = true;
            // 
            // txtUkupnoMašinovođa
            // 
            this.txtUkupnoMašinovođa.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.txtUkupnoMašinovođa.Location = new System.Drawing.Point(781, 74);
            this.txtUkupnoMašinovođa.Name = "txtUkupnoMašinovođa";
            this.txtUkupnoMašinovođa.Size = new System.Drawing.Size(43, 20);
            this.txtUkupnoMašinovođa.TabIndex = 344;
            this.txtUkupnoMašinovođa.Text = "0";
            this.txtUkupnoMašinovođa.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(673, 76);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(109, 13);
            this.label16.TabIndex = 343;
            this.label16.Text = "Ukupno mašinovođa:";
            // 
            // cboVozilo
            // 
            this.cboVozilo.Enabled = false;
            this.cboVozilo.FormattingEnabled = true;
            this.cboVozilo.Items.AddRange(new object[] {
            "privatno vozilo",
            "501 L  GE ",
            "501 L  ZW",
            "501 L  VA",
            "501 L  SK",
            "501 L  MF",
            "501 L  PN",
            "501 L  PM",
            "501 L  PO",
            "501 L  RV",
            "501 L  RU",
            "501 L  OU",
            "501 L  OZ",
            "501 L  GD"});
            this.cboVozilo.Location = new System.Drawing.Point(781, 180);
            this.cboVozilo.Name = "cboVozilo";
            this.cboVozilo.Size = new System.Drawing.Size(159, 21);
            this.cboVozilo.TabIndex = 342;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Enabled = false;
            this.label14.Location = new System.Drawing.Point(694, 183);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(38, 13);
            this.label14.TabIndex = 341;
            this.label14.Text = "Vozilo:";
            // 
            // cboNalogodavac
            // 
            this.cboNalogodavac.FormattingEnabled = true;
            this.cboNalogodavac.Items.AddRange(new object[] {
            "Dejan Nedeljković"});
            this.cboNalogodavac.Location = new System.Drawing.Point(781, 153);
            this.cboNalogodavac.Name = "cboNalogodavac";
            this.cboNalogodavac.Size = new System.Drawing.Size(159, 21);
            this.cboNalogodavac.TabIndex = 340;
            this.cboNalogodavac.Text = "Dejan Nedeljković";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(694, 156);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(74, 13);
            this.label13.TabIndex = 339;
            this.label13.Text = "Nalogodavac:";
            // 
            // txtRazlog
            // 
            this.txtRazlog.Location = new System.Drawing.Point(509, 180);
            this.txtRazlog.Name = "txtRazlog";
            this.txtRazlog.Size = new System.Drawing.Size(176, 20);
            this.txtRazlog.TabIndex = 337;
            this.txtRazlog.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(439, 180);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(40, 13);
            this.label12.TabIndex = 338;
            this.label12.Text = "Razlog";
            // 
            // txtBrojVagona
            // 
            this.txtBrojVagona.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.txtBrojVagona.Enabled = false;
            this.txtBrojVagona.Location = new System.Drawing.Point(509, 155);
            this.txtBrojVagona.Name = "txtBrojVagona";
            this.txtBrojVagona.Size = new System.Drawing.Size(58, 20);
            this.txtBrojVagona.TabIndex = 335;
            this.txtBrojVagona.Text = "0";
            this.txtBrojVagona.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Enabled = false;
            this.label2.Location = new System.Drawing.Point(439, 155);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(67, 13);
            this.label2.TabIndex = 336;
            this.label2.Text = "Broj vagona:";
            // 
            // btnUnesi
            // 
            this.btnUnesi.Enabled = false;
            this.btnUnesi.Location = new System.Drawing.Point(403, 156);
            this.btnUnesi.Name = "btnUnesi";
            this.btnUnesi.Size = new System.Drawing.Size(32, 280);
            this.btnUnesi.TabIndex = 334;
            this.btnUnesi.Text = ">>";
            this.btnUnesi.UseVisualStyleBackColor = true;
            this.btnUnesi.Click += new System.EventHandler(this.btnUnesi_Click);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(10, 129);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(54, 13);
            this.label11.TabIndex = 333;
            this.label11.Text = "Aktivnost:";
            // 
            // dataGridView2
            // 
            this.dataGridView2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.dataGridView2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView2.Enabled = false;
            this.dataGridView2.Location = new System.Drawing.Point(13, 157);
            this.dataGridView2.Name = "dataGridView2";
            this.dataGridView2.Size = new System.Drawing.Size(387, 279);
            this.dataGridView2.TabIndex = 332;
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(442, 245);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(740, 189);
            this.dataGridView1.TabIndex = 331;
            // 
            // txtDodatnaNapomena
            // 
            this.txtDodatnaNapomena.Location = new System.Drawing.Point(781, 129);
            this.txtDodatnaNapomena.Name = "txtDodatnaNapomena";
            this.txtDodatnaNapomena.Size = new System.Drawing.Size(209, 20);
            this.txtDodatnaNapomena.TabIndex = 329;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(676, 128);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(101, 13);
            this.label10.TabIndex = 330;
            this.label10.Text = "Dodatno napomena";
            // 
            // txtKoeficijent
            // 
            this.txtKoeficijent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.txtKoeficijent.Location = new System.Drawing.Point(638, 129);
            this.txtKoeficijent.Name = "txtKoeficijent";
            this.txtKoeficijent.Size = new System.Drawing.Size(32, 20);
            this.txtKoeficijent.TabIndex = 327;
            this.txtKoeficijent.Text = "100";
            this.txtKoeficijent.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // txtVreme
            // 
            this.txtVreme.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.txtVreme.Location = new System.Drawing.Point(781, 46);
            this.txtVreme.Name = "txtVreme";
            this.txtVreme.Size = new System.Drawing.Size(43, 20);
            this.txtVreme.TabIndex = 326;
            this.txtVreme.Text = "0";
            this.txtVreme.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(7, 108);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(79, 13);
            this.label6.TabIndex = 315;
            this.label6.Text = "Dodatni trošak:";
            // 
            // txtTrosak
            // 
            this.txtTrosak.Location = new System.Drawing.Point(92, 103);
            this.txtTrosak.Name = "txtTrosak";
            this.txtTrosak.Size = new System.Drawing.Size(48, 20);
            this.txtTrosak.TabIndex = 316;
            this.txtTrosak.Text = "0";
            this.txtTrosak.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(573, 129);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(59, 13);
            this.label5.TabIndex = 328;
            this.label5.Text = "Koeficijent:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(439, 128);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(30, 13);
            this.label3.TabIndex = 312;
            this.label3.Text = "Rad:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(727, 46);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(48, 13);
            this.label9.TabIndex = 325;
            this.label9.Text = "Ukupno:";
            // 
            // dtpVremeDo
            // 
            this.dtpVremeDo.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpVremeDo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeDo.Location = new System.Drawing.Point(606, 47);
            this.dtpVremeDo.Name = "dtpVremeDo";
            this.dtpVremeDo.ShowUpDown = true;
            this.dtpVremeDo.Size = new System.Drawing.Size(115, 20);
            this.dtpVremeDo.TabIndex = 322;
            this.dtpVremeDo.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            this.dtpVremeDo.Leave += new System.EventHandler(this.dtpVremeDo_Leave);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(545, 48);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(55, 13);
            this.label15.TabIndex = 324;
            this.label15.Text = "Period do:";
            // 
            // dtpVremeOd
            // 
            this.dtpVremeOd.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpVremeOd.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeOd.Location = new System.Drawing.Point(431, 48);
            this.dtpVremeOd.Name = "dtpVremeOd";
            this.dtpVremeOd.ShowUpDown = true;
            this.dtpVremeOd.Size = new System.Drawing.Size(108, 20);
            this.dtpVremeOd.TabIndex = 321;
            this.dtpVremeOd.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(370, 48);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(55, 13);
            this.label21.TabIndex = 323;
            this.label21.Text = "Period od:";
            // 
            // cboAktivnost
            // 
            this.cboAktivnost.FormattingEnabled = true;
            this.cboAktivnost.Location = new System.Drawing.Point(70, 129);
            this.cboAktivnost.Name = "cboAktivnost";
            this.cboAktivnost.Size = new System.Drawing.Size(333, 21);
            this.cboAktivnost.TabIndex = 320;
            this.cboAktivnost.SelectedValueChanged += new System.EventHandler(this.cboAktivnost_SelectedValueChanged);
            this.cboAktivnost.Leave += new System.EventHandler(this.cboAktivnost_Leave);
            // 
            // btnUbaciAktivnost
            // 
            this.btnUbaciAktivnost.Location = new System.Drawing.Point(1004, 127);
            this.btnUbaciAktivnost.Name = "btnUbaciAktivnost";
            this.btnUbaciAktivnost.Size = new System.Drawing.Size(143, 24);
            this.btnUbaciAktivnost.TabIndex = 319;
            this.btnUbaciAktivnost.Text = "Unesi po satu";
            this.btnUbaciAktivnost.UseVisualStyleBackColor = true;
            this.btnUbaciAktivnost.Click += new System.EventHandler(this.btnUbaciAktivnost_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(849, 34);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(91, 13);
            this.label7.TabIndex = 318;
            this.label7.Text = "Dodatne beleške:";
            // 
            // txtKomentar
            // 
            this.txtKomentar.Location = new System.Drawing.Point(848, 50);
            this.txtKomentar.Multiline = true;
            this.txtKomentar.Name = "txtKomentar";
            this.txtKomentar.Size = new System.Drawing.Size(334, 68);
            this.txtKomentar.TabIndex = 317;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Enabled = false;
            this.label8.Location = new System.Drawing.Point(133, 77);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(67, 13);
            this.label8.TabIndex = 314;
            this.label8.Text = "Radni nalog:";
            // 
            // cboRadniNalog
            // 
            this.cboRadniNalog.Enabled = false;
            this.cboRadniNalog.FormattingEnabled = true;
            this.cboRadniNalog.Location = new System.Drawing.Point(200, 76);
            this.cboRadniNalog.Name = "cboRadniNalog";
            this.cboRadniNalog.Size = new System.Drawing.Size(108, 21);
            this.cboRadniNalog.TabIndex = 313;
            // 
            // cboZaposleni
            // 
            this.cboZaposleni.FormattingEnabled = true;
            this.cboZaposleni.Location = new System.Drawing.Point(200, 49);
            this.cboZaposleni.Name = "cboZaposleni";
            this.cboZaposleni.Size = new System.Drawing.Size(159, 21);
            this.cboZaposleni.TabIndex = 311;
            this.cboZaposleni.Leave += new System.EventHandler(this.cboZaposleni_Leave);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(134, 50);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(56, 13);
            this.label4.TabIndex = 310;
            this.label4.Text = "Zaposleni:";
            // 
            // txtSifra
            // 
            this.txtSifra.Location = new System.Drawing.Point(77, 50);
            this.txtSifra.Name = "txtSifra";
            this.txtSifra.Size = new System.Drawing.Size(52, 20);
            this.txtSifra.TabIndex = 308;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 50);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(64, 13);
            this.label1.TabIndex = 309;
            this.label1.Text = "Šifra zapisa:";
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
            // tsNazad
            // 
            this.tsNazad.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsNazad.Image = ((System.Drawing.Image)(resources.GetObject("tsNazad.Image")));
            this.tsNazad.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsNazad.Name = "tsNazad";
            this.tsNazad.Size = new System.Drawing.Size(23, 22);
            this.tsNazad.Text = "toolStripButton1";
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
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
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
            // tsPoslednja
            // 
            this.tsPoslednja.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsPoslednja.Image = ((System.Drawing.Image)(resources.GetObject("tsPoslednja.Image")));
            this.tsPoslednja.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsPoslednja.Name = "tsPoslednja";
            this.tsPoslednja.Size = new System.Drawing.Size(23, 22);
            this.tsPoslednja.Text = "toolStripButton1";
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
            this.toolStrip1.Size = new System.Drawing.Size(1194, 25);
            this.toolStrip1.TabIndex = 307;
            this.toolStrip1.Text = "Osveži";
            // 
            // dtpStavke
            // 
            this.dtpStavke.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpStavke.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpStavke.Location = new System.Drawing.Point(535, 206);
            this.dtpStavke.Name = "dtpStavke";
            this.dtpStavke.ShowUpDown = true;
            this.dtpStavke.Size = new System.Drawing.Size(125, 20);
            this.dtpStavke.TabIndex = 360;
            this.dtpStavke.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // label29
            // 
            this.label29.AutoSize = true;
            this.label29.Location = new System.Drawing.Point(445, 206);
            this.label29.Name = "label29";
            this.label29.Size = new System.Drawing.Size(84, 13);
            this.label29.TabIndex = 361;
            this.label29.Text = "Vreme izvršenja:";
            // 
            // frmEvidencijaRadaRemont
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1194, 527);
            this.Controls.Add(this.dtpStavke);
            this.Controls.Add(this.label29);
            this.Controls.Add(this.txtRad);
            this.Controls.Add(this.txtMesto);
            this.Controls.Add(this.label22);
            this.Controls.Add(this.label20);
            this.Controls.Add(this.txtKartica);
            this.Controls.Add(this.label19);
            this.Controls.Add(this.txtRacun);
            this.Controls.Add(this.chkPlaceno);
            this.Controls.Add(this.chkPoslatMail);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.txtEmail);
            this.Controls.Add(this.btnPosaljiMail);
            this.Controls.Add(this.txtOznaka);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.chkUnosMasinovođa);
            this.Controls.Add(this.txtUkupnoMašinovođa);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.cboVozilo);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.cboNalogodavac);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.txtRazlog);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.txtBrojVagona);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnUnesi);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.dataGridView2);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.txtDodatnaNapomena);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.txtKoeficijent);
            this.Controls.Add(this.txtVreme);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txtTrosak);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.dtpVremeDo);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.dtpVremeOd);
            this.Controls.Add(this.label21);
            this.Controls.Add(this.cboAktivnost);
            this.Controls.Add(this.btnUbaciAktivnost);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.txtKomentar);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.cboRadniNalog);
            this.Controls.Add(this.cboZaposleni);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtSifra);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "frmEvidencijaRadaRemont";
            this.Text = "Evidencija rada remont";
            this.Load += new System.EventHandler(this.frmEvidencijaRada_Load);
            ((System.ComponentModel.ISupportInitialize)(this.txtRad)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NumericUpDown txtRad;
        private System.Windows.Forms.TextBox txtMesto;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.TextBox txtKartica;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.TextBox txtRacun;
        private System.Windows.Forms.CheckBox chkPlaceno;
        private System.Windows.Forms.CheckBox chkPoslatMail;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox txtEmail;
        private System.Windows.Forms.Button btnPosaljiMail;
        private System.Windows.Forms.TextBox txtOznaka;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.CheckBox chkUnosMasinovođa;
        private System.Windows.Forms.TextBox txtUkupnoMašinovođa;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.ComboBox cboVozilo;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.ComboBox cboNalogodavac;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtRazlog;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txtBrojVagona;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnUnesi;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.DataGridView dataGridView2;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.TextBox txtDodatnaNapomena;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtKoeficijent;
        private System.Windows.Forms.TextBox txtVreme;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtTrosak;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.DateTimePicker dtpVremeDo;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.DateTimePicker dtpVremeOd;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.ComboBox cboAktivnost;
        private System.Windows.Forms.Button btnUbaciAktivnost;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtKomentar;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox cboRadniNalog;
        private System.Windows.Forms.ComboBox cboZaposleni;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtSifra;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolStripButton tsNapred;
        private System.Windows.Forms.ToolStripButton tsNazad;
        private System.Windows.Forms.ToolStripButton tsPrvi;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton tsDelete;
        private System.Windows.Forms.ToolStripButton tsSave;
        private System.Windows.Forms.ToolStripButton tsNew;
        private System.Windows.Forms.ToolStripButton tsPoslednja;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.DateTimePicker dtpStavke;
        private System.Windows.Forms.Label label29;
    }
}