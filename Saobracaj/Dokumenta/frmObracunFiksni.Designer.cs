
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
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.label10 = new System.Windows.Forms.Label();
            this.dtpVremeOd2 = new System.Windows.Forms.DateTimePicker();
            this.dtpVremeDo2 = new System.Windows.Forms.DateTimePicker();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.txtSatiMesec = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            this.button9 = new System.Windows.Forms.Button();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtMinimalac = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.txtKurs = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.btnIzracunaj = new System.Windows.Forms.Button();
            this.dtpVremeDo = new System.Windows.Forms.DateTimePicker();
            this.label15 = new System.Windows.Forms.Label();
            this.dtpVremeOd = new System.Windows.Forms.DateTimePicker();
            this.label21 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSatiMesec)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMinimalac)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtKurs)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(27, 174);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(4);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.Size = new System.Drawing.Size(1488, 471);
            this.dataGridView1.TabIndex = 153;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.ForeColor = System.Drawing.Color.Red;
            this.label10.Location = new System.Drawing.Point(24, 58);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(325, 17);
            this.label10.TabIndex = 370;
            this.label10.Text = "Prekucati isti datum dok se ne nadje bolje resenje:";
            // 
            // dtpVremeOd2
            // 
            this.dtpVremeOd2.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dtpVremeOd2.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeOd2.Location = new System.Drawing.Point(105, 95);
            this.dtpVremeOd2.Margin = new System.Windows.Forms.Padding(4);
            this.dtpVremeOd2.Name = "dtpVremeOd2";
            this.dtpVremeOd2.ShowUpDown = true;
            this.dtpVremeOd2.Size = new System.Drawing.Size(145, 22);
            this.dtpVremeOd2.TabIndex = 369;
            this.dtpVremeOd2.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // dtpVremeDo2
            // 
            this.dtpVremeDo2.CustomFormat = "yyyy-MM-dd HH:mm";
            this.dtpVremeDo2.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeDo2.Location = new System.Drawing.Point(344, 95);
            this.dtpVremeDo2.Margin = new System.Windows.Forms.Padding(4);
            this.dtpVremeDo2.Name = "dtpVremeDo2";
            this.dtpVremeDo2.ShowUpDown = true;
            this.dtpVremeDo2.Size = new System.Drawing.Size(152, 22);
            this.dtpVremeDo2.TabIndex = 368;
            this.dtpVremeDo2.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.ForeColor = System.Drawing.Color.Red;
            this.label7.Location = new System.Drawing.Point(258, 95);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(73, 17);
            this.label7.TabIndex = 367;
            this.label7.Text = "Period do:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.ForeColor = System.Drawing.Color.Red;
            this.label8.Location = new System.Drawing.Point(24, 95);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(73, 17);
            this.label8.TabIndex = 366;
            this.label8.Text = "Period od:";
            // 
            // txtSatiMesec
            // 
            this.txtSatiMesec.DecimalPlaces = 2;
            this.txtSatiMesec.Location = new System.Drawing.Point(291, 128);
            this.txtSatiMesec.Margin = new System.Windows.Forms.Padding(4);
            this.txtSatiMesec.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtSatiMesec.Name = "txtSatiMesec";
            this.txtSatiMesec.Size = new System.Drawing.Size(92, 22);
            this.txtSatiMesec.TabIndex = 365;
            this.txtSatiMesec.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.ForeColor = System.Drawing.Color.Red;
            this.label9.Location = new System.Drawing.Point(205, 130);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(81, 17);
            this.label9.TabIndex = 364;
            this.label9.Text = "Sati mesec:";
            // 
            // button9
            // 
            this.button9.Location = new System.Drawing.Point(1074, 18);
            this.button9.Margin = new System.Windows.Forms.Padding(4);
            this.button9.Name = "button9";
            this.button9.Size = new System.Drawing.Size(175, 28);
            this.button9.TabIndex = 361;
            this.button9.Text = "Refresh";
            this.button9.UseVisualStyleBackColor = true;
            this.button9.Click += new System.EventHandler(this.button9_Click);
            // 
            // txtPassword
            // 
            this.txtPassword.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.txtPassword.Location = new System.Drawing.Point(926, 21);
            this.txtPassword.Margin = new System.Windows.Forms.Padding(4);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(132, 22);
            this.txtPassword.TabIndex = 360;
            this.txtPassword.UseSystemPasswordChar = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(881, 21);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(37, 17);
            this.label6.TabIndex = 359;
            this.label6.Text = "Šifra";
            // 
            // txtMinimalac
            // 
            this.txtMinimalac.DecimalPlaces = 2;
            this.txtMinimalac.Location = new System.Drawing.Point(485, 130);
            this.txtMinimalac.Margin = new System.Windows.Forms.Padding(4);
            this.txtMinimalac.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtMinimalac.Name = "txtMinimalac";
            this.txtMinimalac.Size = new System.Drawing.Size(92, 22);
            this.txtMinimalac.TabIndex = 357;
            this.txtMinimalac.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(403, 130);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(74, 17);
            this.label4.TabIndex = 356;
            this.label4.Text = "Minimalac:";
            // 
            // txtKurs
            // 
            this.txtKurs.DecimalPlaces = 2;
            this.txtKurs.Location = new System.Drawing.Point(105, 128);
            this.txtKurs.Margin = new System.Windows.Forms.Padding(4);
            this.txtKurs.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtKurs.Name = "txtKurs";
            this.txtKurs.Size = new System.Drawing.Size(92, 22);
            this.txtKurs.TabIndex = 349;
            this.txtKurs.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(24, 128);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 17);
            this.label1.TabIndex = 348;
            this.label1.Text = "Kurs:";
            // 
            // btnIzracunaj
            // 
            this.btnIzracunaj.Location = new System.Drawing.Point(613, 13);
            this.btnIzracunaj.Margin = new System.Windows.Forms.Padding(4);
            this.btnIzracunaj.Name = "btnIzracunaj";
            this.btnIzracunaj.Size = new System.Drawing.Size(191, 30);
            this.btnIzracunaj.TabIndex = 347;
            this.btnIzracunaj.Text = "Izračunaj 1";
            this.btnIzracunaj.UseVisualStyleBackColor = true;
            this.btnIzracunaj.Click += new System.EventHandler(this.btnIzracunaj_Click);
            // 
            // dtpVremeDo
            // 
            this.dtpVremeDo.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpVremeDo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeDo.Location = new System.Drawing.Point(344, 17);
            this.dtpVremeDo.Margin = new System.Windows.Forms.Padding(4);
            this.dtpVremeDo.Name = "dtpVremeDo";
            this.dtpVremeDo.ShowUpDown = true;
            this.dtpVremeDo.Size = new System.Drawing.Size(152, 22);
            this.dtpVremeDo.TabIndex = 344;
            this.dtpVremeDo.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(262, 18);
            this.label15.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(73, 17);
            this.label15.TabIndex = 346;
            this.label15.Text = "Period do:";
            // 
            // dtpVremeOd
            // 
            this.dtpVremeOd.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dtpVremeOd.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpVremeOd.Location = new System.Drawing.Point(110, 18);
            this.dtpVremeOd.Margin = new System.Windows.Forms.Padding(4);
            this.dtpVremeOd.Name = "dtpVremeOd";
            this.dtpVremeOd.ShowUpDown = true;
            this.dtpVremeOd.Size = new System.Drawing.Size(143, 22);
            this.dtpVremeOd.TabIndex = 343;
            this.dtpVremeOd.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(24, 16);
            this.label21.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(73, 17);
            this.label21.TabIndex = 345;
            this.label21.Text = "Period od:";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(756, 119);
            this.button1.Margin = new System.Windows.Forms.Padding(4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(175, 28);
            this.button1.TabIndex = 371;
            this.button1.Text = "Samo fiksni";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(955, 119);
            this.button2.Margin = new System.Windows.Forms.Padding(4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(175, 28);
            this.button2.TabIndex = 372;
            this.button2.Text = "Nisu fiksni";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // frmObracunFiksni
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1531, 649);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.dtpVremeOd2);
            this.Controls.Add(this.dtpVremeDo2);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.txtSatiMesec);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.button9);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txtMinimalac);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtKurs);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnIzracunaj);
            this.Controls.Add(this.dtpVremeDo);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.dtpVremeOd);
            this.Controls.Add(this.label21);
            this.Controls.Add(this.dataGridView1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "frmObracunFiksni";
            this.Text = "Obračun zarade fiksni";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSatiMesec)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMinimalac)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtKurs)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.DateTimePicker dtpVremeOd2;
        private System.Windows.Forms.DateTimePicker dtpVremeDo2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown txtSatiMesec;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button button9;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown txtMinimalac;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown txtKurs;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnIzracunaj;
        private System.Windows.Forms.DateTimePicker dtpVremeDo;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.DateTimePicker dtpVremeOd;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}