﻿namespace Saobracaj.Dokumenta
{
    partial class frmEvidencijaRadaZaglavlje
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
            this.button1 = new System.Windows.Forms.Button();
            this.btnPretrazi = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.cboZaposleni = new System.Windows.Forms.ComboBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.dtpPredvidjenoPrimanje = new System.Windows.Forms.DateTimePicker();
            this.dtpVremeDo = new System.Windows.Forms.DateTimePicker();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.btn = new System.Windows.Forms.Button();
            this.txtUkupniTrosak = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtPlaceno = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtNeplaceno = new System.Windows.Forms.TextBox();
            this.button4 = new System.Windows.Forms.Button();
            this.chkUnosMasinovođa = new System.Windows.Forms.CheckBox();
            this.button5 = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.txtNeplacenoRacuni = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtPlacenoRAcuni = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.txtUkupniTrosakRacuni = new System.Windows.Forms.TextBox();
            this.button7 = new System.Windows.Forms.Button();
            this.button8 = new System.Windows.Forms.Button();
            this.chkSmederevo1 = new System.Windows.Forms.CheckBox();
            this.chkCG = new System.Windows.Forms.CheckBox();
            this.chkRemont = new System.Windows.Forms.CheckBox();
            this.chkKragujevac = new System.Windows.Forms.CheckBox();
            this.button9 = new System.Windows.Forms.Button();
            this.button10 = new System.Windows.Forms.Button();
            this.chkSpolja = new System.Windows.Forms.CheckBox();
            this.button11 = new System.Windows.Forms.Button();
            this.button12 = new System.Windows.Forms.Button();
            this.dataGridView2 = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(811, 120);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(193, 23);
            this.button1.TabIndex = 99;
            this.button1.Text = "Izbriši selektovane stavke";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnPretrazi
            // 
            this.btnPretrazi.Location = new System.Drawing.Point(318, 5);
            this.btnPretrazi.Name = "btnPretrazi";
            this.btnPretrazi.Size = new System.Drawing.Size(74, 23);
            this.btnPretrazi.TabIndex = 98;
            this.btnPretrazi.Text = "Pretraži";
            this.btnPretrazi.UseVisualStyleBackColor = true;
            this.btnPretrazi.Click += new System.EventHandler(this.btnPretrazi_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(18, 7);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(56, 13);
            this.label6.TabIndex = 97;
            this.label6.Text = "Zaposleni:";
            // 
            // cboZaposleni
            // 
            this.cboZaposleni.BackColor = System.Drawing.Color.White;
            this.cboZaposleni.FormattingEnabled = true;
            this.cboZaposleni.Location = new System.Drawing.Point(80, 7);
            this.cboZaposleni.Name = "cboZaposleni";
            this.cboZaposleni.Size = new System.Drawing.Size(171, 21);
            this.cboZaposleni.TabIndex = 96;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(12, 149);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(1264, 169);
            this.dataGridView1.TabIndex = 95;
            this.dataGridView1.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            // 
            // dtpPredvidjenoPrimanje
            // 
            this.dtpPredvidjenoPrimanje.CustomFormat = "yyyy-MM-dd";
            this.dtpPredvidjenoPrimanje.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpPredvidjenoPrimanje.Location = new System.Drawing.Point(210, 36);
            this.dtpPredvidjenoPrimanje.Name = "dtpPredvidjenoPrimanje";
            this.dtpPredvidjenoPrimanje.ShowUpDown = true;
            this.dtpPredvidjenoPrimanje.Size = new System.Drawing.Size(95, 20);
            this.dtpPredvidjenoPrimanje.TabIndex = 100;
            this.dtpPredvidjenoPrimanje.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // dtpVremeDo
            // 
            this.dtpVremeDo.CustomFormat = "yyyy-MM-dd";
            this.dtpVremeDo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeDo.Location = new System.Drawing.Point(210, 62);
            this.dtpVremeDo.Name = "dtpVremeDo";
            this.dtpVremeDo.ShowUpDown = true;
            this.dtpVremeDo.Size = new System.Drawing.Size(95, 20);
            this.dtpVremeDo.TabIndex = 101;
            this.dtpVremeDo.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(318, 35);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(74, 23);
            this.button2.TabIndex = 102;
            this.button2.Text = "Pretraži";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(414, 7);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(108, 23);
            this.button3.TabIndex = 103;
            this.button3.Text = "Otvori zapis";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(149, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 13);
            this.label1.TabIndex = 104;
            this.label1.Text = "Vreme od:";
            // 
            // btn
            // 
            this.btn.Location = new System.Drawing.Point(537, 7);
            this.btn.Name = "btn";
            this.btn.Size = new System.Drawing.Size(108, 23);
            this.btn.TabIndex = 105;
            this.btn.Text = "Refresh";
            this.btn.UseVisualStyleBackColor = true;
            this.btn.Click += new System.EventHandler(this.btn_Click);
            // 
            // txtUkupniTrosak
            // 
            this.txtUkupniTrosak.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.txtUkupniTrosak.Location = new System.Drawing.Point(502, 39);
            this.txtUkupniTrosak.Name = "txtUkupniTrosak";
            this.txtUkupniTrosak.Size = new System.Drawing.Size(49, 20);
            this.txtUkupniTrosak.TabIndex = 157;
            this.txtUkupniTrosak.Text = "0";
            this.txtUkupniTrosak.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(391, 42);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(105, 13);
            this.label2.TabIndex = 158;
            this.label2.Text = "Ukupni radnik black:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(552, 42);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(49, 13);
            this.label3.TabIndex = 160;
            this.label3.Text = "Plaćeno:";
            // 
            // txtPlaceno
            // 
            this.txtPlaceno.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.txtPlaceno.Location = new System.Drawing.Point(607, 37);
            this.txtPlaceno.Name = "txtPlaceno";
            this.txtPlaceno.Size = new System.Drawing.Size(57, 20);
            this.txtPlaceno.TabIndex = 159;
            this.txtPlaceno.Text = "0";
            this.txtPlaceno.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(667, 40);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(62, 13);
            this.label4.TabIndex = 162;
            this.label4.Text = "Neplaćeno:";
            // 
            // txtNeplaceno
            // 
            this.txtNeplaceno.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.txtNeplaceno.Location = new System.Drawing.Point(735, 37);
            this.txtNeplaceno.Name = "txtNeplaceno";
            this.txtNeplaceno.Size = new System.Drawing.Size(57, 20);
            this.txtNeplaceno.TabIndex = 161;
            this.txtNeplaceno.Text = "0";
            this.txtNeplaceno.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // button4
            // 
            this.button4.Enabled = false;
            this.button4.Location = new System.Drawing.Point(811, 36);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(193, 23);
            this.button4.TabIndex = 163;
            this.button4.Text = "Plati selektovane stavke troškovi";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Visible = false;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // chkUnosMasinovođa
            // 
            this.chkUnosMasinovođa.AutoSize = true;
            this.chkUnosMasinovođa.Location = new System.Drawing.Point(12, 34);
            this.chkUnosMasinovođa.Name = "chkUnosMasinovođa";
            this.chkUnosMasinovođa.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkUnosMasinovođa.Size = new System.Drawing.Size(115, 17);
            this.chkUnosMasinovođa.TabIndex = 187;
            this.chkUnosMasinovođa.Text = "Samo mašinovodje";
            this.chkUnosMasinovođa.UseVisualStyleBackColor = true;
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(651, 7);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(140, 23);
            this.button5.TabIndex = 188;
            this.button5.Text = "Update masinovodja";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(811, 62);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(193, 23);
            this.button6.TabIndex = 189;
            this.button6.Text = "Pregledano računa izvršen";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(667, 66);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(62, 13);
            this.label5.TabIndex = 195;
            this.label5.Text = "Neplaćeno:";
            // 
            // txtNeplacenoRacuni
            // 
            this.txtNeplacenoRacuni.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.txtNeplacenoRacuni.Location = new System.Drawing.Point(735, 63);
            this.txtNeplacenoRacuni.Name = "txtNeplacenoRacuni";
            this.txtNeplacenoRacuni.Size = new System.Drawing.Size(57, 20);
            this.txtNeplacenoRacuni.TabIndex = 194;
            this.txtNeplacenoRacuni.Text = "0";
            this.txtNeplacenoRacuni.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(552, 68);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(49, 13);
            this.label7.TabIndex = 193;
            this.label7.Text = "Plaćeno:";
            // 
            // txtPlacenoRAcuni
            // 
            this.txtPlacenoRAcuni.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.txtPlacenoRAcuni.Location = new System.Drawing.Point(607, 63);
            this.txtPlacenoRAcuni.Name = "txtPlacenoRAcuni";
            this.txtPlacenoRAcuni.Size = new System.Drawing.Size(57, 20);
            this.txtPlacenoRAcuni.TabIndex = 192;
            this.txtPlacenoRAcuni.Text = "0";
            this.txtPlacenoRAcuni.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(412, 68);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(76, 13);
            this.label8.TabIndex = 191;
            this.label8.Text = "Ukupni računi:";
            // 
            // txtUkupniTrosakRacuni
            // 
            this.txtUkupniTrosakRacuni.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.txtUkupniTrosakRacuni.Location = new System.Drawing.Point(502, 65);
            this.txtUkupniTrosakRacuni.Name = "txtUkupniTrosakRacuni";
            this.txtUkupniTrosakRacuni.Size = new System.Drawing.Size(49, 20);
            this.txtUkupniTrosakRacuni.TabIndex = 190;
            this.txtUkupniTrosakRacuni.Text = "0";
            this.txtUkupniTrosakRacuni.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // button7
            // 
            this.button7.Location = new System.Drawing.Point(811, 7);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(135, 23);
            this.button7.TabIndex = 196;
            this.button7.Text = "Izdračunaj";
            this.button7.UseVisualStyleBackColor = true;
            this.button7.Click += new System.EventHandler(this.button7_Click);
            // 
            // button8
            // 
            this.button8.Location = new System.Drawing.Point(811, 91);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(193, 23);
            this.button8.TabIndex = 197;
            this.button8.Text = "Pregledano kartice";
            this.button8.UseVisualStyleBackColor = true;
            this.button8.Click += new System.EventHandler(this.button8_Click);
            // 
            // chkSmederevo1
            // 
            this.chkSmederevo1.AutoSize = true;
            this.chkSmederevo1.Enabled = false;
            this.chkSmederevo1.Location = new System.Drawing.Point(12, 60);
            this.chkSmederevo1.Name = "chkSmederevo1";
            this.chkSmederevo1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkSmederevo1.Size = new System.Drawing.Size(80, 17);
            this.chkSmederevo1.TabIndex = 198;
            this.chkSmederevo1.Text = "Smederevo";
            this.chkSmederevo1.UseVisualStyleBackColor = true;
            this.chkSmederevo1.Visible = false;
            // 
            // chkCG
            // 
            this.chkCG.AutoSize = true;
            this.chkCG.Enabled = false;
            this.chkCG.Location = new System.Drawing.Point(136, 62);
            this.chkCG.Name = "chkCG";
            this.chkCG.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkCG.Size = new System.Drawing.Size(41, 17);
            this.chkCG.TabIndex = 199;
            this.chkCG.Text = "CG";
            this.chkCG.UseVisualStyleBackColor = true;
            this.chkCG.Visible = false;
            // 
            // chkRemont
            // 
            this.chkRemont.AutoSize = true;
            this.chkRemont.Enabled = false;
            this.chkRemont.Location = new System.Drawing.Point(141, 62);
            this.chkRemont.Name = "chkRemont";
            this.chkRemont.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkRemont.Size = new System.Drawing.Size(63, 17);
            this.chkRemont.TabIndex = 200;
            this.chkRemont.Text = "Remont";
            this.chkRemont.UseVisualStyleBackColor = true;
            this.chkRemont.Visible = false;
            // 
            // chkKragujevac
            // 
            this.chkKragujevac.AutoSize = true;
            this.chkKragujevac.Enabled = false;
            this.chkKragujevac.Location = new System.Drawing.Point(47, 60);
            this.chkKragujevac.Name = "chkKragujevac";
            this.chkKragujevac.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkKragujevac.Size = new System.Drawing.Size(80, 17);
            this.chkKragujevac.TabIndex = 201;
            this.chkKragujevac.Text = "Kragujevac";
            this.chkKragujevac.UseVisualStyleBackColor = true;
            this.chkKragujevac.Visible = false;
            // 
            // button9
            // 
            this.button9.Location = new System.Drawing.Point(12, 83);
            this.button9.Name = "button9";
            this.button9.Size = new System.Drawing.Size(165, 23);
            this.button9.TabIndex = 202;
            this.button9.Text = "Pretraži";
            this.button9.UseVisualStyleBackColor = true;
            this.button9.Click += new System.EventHandler(this.button9_Click);
            // 
            // button10
            // 
            this.button10.Location = new System.Drawing.Point(183, 83);
            this.button10.Name = "button10";
            this.button10.Size = new System.Drawing.Size(165, 23);
            this.button10.TabIndex = 203;
            this.button10.Text = "Spoljni";
            this.button10.UseVisualStyleBackColor = true;
            this.button10.Click += new System.EventHandler(this.button10_Click);
            // 
            // chkSpolja
            // 
            this.chkSpolja.AutoSize = true;
            this.chkSpolja.Location = new System.Drawing.Point(257, 9);
            this.chkSpolja.Name = "chkSpolja";
            this.chkSpolja.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkSpolja.Size = new System.Drawing.Size(55, 17);
            this.chkSpolja.TabIndex = 204;
            this.chkSpolja.Text = "Spolja";
            this.chkSpolja.UseVisualStyleBackColor = true;
            // 
            // button11
            // 
            this.button11.Location = new System.Drawing.Point(469, 106);
            this.button11.Name = "button11";
            this.button11.Size = new System.Drawing.Size(120, 23);
            this.button11.TabIndex = 205;
            this.button11.Text = "Saberi smenu";
            this.button11.UseVisualStyleBackColor = true;
            this.button11.Click += new System.EventHandler(this.button11_Click);
            // 
            // button12
            // 
            this.button12.Location = new System.Drawing.Point(1020, 5);
            this.button12.Name = "button12";
            this.button12.Size = new System.Drawing.Size(120, 23);
            this.button12.TabIndex = 206;
            this.button12.Text = "Nadležni mašinovodja";
            this.button12.UseVisualStyleBackColor = true;
            // 
            // dataGridView2
            // 
            this.dataGridView2.AllowUserToAddRows = false;
            this.dataGridView2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView2.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dataGridView2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView2.Location = new System.Drawing.Point(1020, 32);
            this.dataGridView2.Name = "dataGridView2";
            this.dataGridView2.Size = new System.Drawing.Size(256, 111);
            this.dataGridView2.TabIndex = 207;
            // 
            // frmEvidencijaRadaZaglavlje
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1288, 330);
            this.Controls.Add(this.dataGridView2);
            this.Controls.Add(this.button12);
            this.Controls.Add(this.button11);
            this.Controls.Add(this.chkSpolja);
            this.Controls.Add(this.button10);
            this.Controls.Add(this.button9);
            this.Controls.Add(this.chkKragujevac);
            this.Controls.Add(this.chkRemont);
            this.Controls.Add(this.chkCG);
            this.Controls.Add(this.chkSmederevo1);
            this.Controls.Add(this.button8);
            this.Controls.Add(this.button7);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtNeplacenoRacuni);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.txtPlacenoRAcuni);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.txtUkupniTrosakRacuni);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.chkUnosMasinovođa);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtNeplaceno);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtPlaceno);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtUkupniTrosak);
            this.Controls.Add(this.btn);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.dtpVremeDo);
            this.Controls.Add(this.dtpPredvidjenoPrimanje);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnPretrazi);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.cboZaposleni);
            this.Controls.Add(this.dataGridView1);
            this.Name = "frmEvidencijaRadaZaglavlje";
            this.Text = "Evidencija rada po zaglavljima";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.frmEvidencijaRadaZaglavlje_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btnPretrazi;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cboZaposleni;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DateTimePicker dtpPredvidjenoPrimanje;
        private System.Windows.Forms.DateTimePicker dtpVremeDo;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btn;
        private System.Windows.Forms.TextBox txtUkupniTrosak;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtPlaceno;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtNeplaceno;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.CheckBox chkUnosMasinovođa;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtNeplacenoRacuni;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtPlacenoRAcuni;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtUkupniTrosakRacuni;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.Button button8;
        private System.Windows.Forms.CheckBox chkSmederevo1;
        private System.Windows.Forms.CheckBox chkCG;
        private System.Windows.Forms.CheckBox chkRemont;
        private System.Windows.Forms.CheckBox chkKragujevac;
        private System.Windows.Forms.Button button9;
        private System.Windows.Forms.Button button10;
        private System.Windows.Forms.CheckBox chkSpolja;
        private System.Windows.Forms.Button button11;
        private System.Windows.Forms.Button button12;
        private System.Windows.Forms.DataGridView dataGridView2;
    }
}