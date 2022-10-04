namespace Saobracaj.Sifarnici
{
    partial class frmLokomotive
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmLokomotive));
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.chkAktivna = new System.Windows.Forms.CheckBox();
            this.chkDizel = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.txtLokomotiva = new System.Windows.Forms.TextBox();
            this.btnRacun = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.txtMasa = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.cboSerija = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.btn_Dokumenta = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.dataGridView2 = new System.Windows.Forms.DataGridView();
            this.button1 = new System.Windows.Forms.Button();
            this.cboLokomotiva = new System.Windows.Forms.CheckBox();
            this.button2 = new System.Windows.Forms.Button();
            this.cboVozilo = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMasa)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 42);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 17);
            this.label1.TabIndex = 13;
            this.label1.Text = "Lokomotiva";
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(894, 31);
            this.comboBox1.Margin = new System.Windows.Forms.Padding(4);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(70, 24);
            this.comboBox1.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(759, 31);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(127, 17);
            this.label2.TabIndex = 15;
            this.label2.Text = "Tip voznog osoblja";
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(7, 7);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(4);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.Size = new System.Drawing.Size(943, 332);
            this.dataGridView1.TabIndex = 18;
            this.dataGridView1.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            // 
            // comboBox2
            // 
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Location = new System.Drawing.Point(867, 63);
            this.comboBox2.Margin = new System.Windows.Forms.Padding(4);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(97, 24);
            this.comboBox2.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(759, 63);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 17);
            this.label3.TabIndex = 19;
            this.label3.Text = "Osoblje";
            // 
            // chkAktivna
            // 
            this.chkAktivna.AutoSize = true;
            this.chkAktivna.Location = new System.Drawing.Point(16, 118);
            this.chkAktivna.Margin = new System.Windows.Forms.Padding(4);
            this.chkAktivna.Name = "chkAktivna";
            this.chkAktivna.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkAktivna.Size = new System.Drawing.Size(76, 21);
            this.chkAktivna.TabIndex = 188;
            this.chkAktivna.Text = "Aktivna";
            this.chkAktivna.UseVisualStyleBackColor = true;
            // 
            // chkDizel
            // 
            this.chkDizel.AutoSize = true;
            this.chkDizel.Location = new System.Drawing.Point(16, 146);
            this.chkDizel.Margin = new System.Windows.Forms.Padding(4);
            this.chkDizel.Name = "chkDizel";
            this.chkDizel.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkDizel.Size = new System.Drawing.Size(61, 21);
            this.chkDizel.TabIndex = 199;
            this.chkDizel.Text = "Dizel";
            this.chkDizel.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(16, 79);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(61, 17);
            this.label5.TabIndex = 201;
            this.label5.Text = "Lozinka:";
            // 
            // txtPassword
            // 
            this.txtPassword.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.txtPassword.Location = new System.Drawing.Point(107, 79);
            this.txtPassword.Margin = new System.Windows.Forms.Padding(4);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(204, 22);
            this.txtPassword.TabIndex = 200;
            this.txtPassword.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // txtLokomotiva
            // 
            this.txtLokomotiva.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.txtLokomotiva.Location = new System.Drawing.Point(107, 42);
            this.txtLokomotiva.Margin = new System.Windows.Forms.Padding(4);
            this.txtLokomotiva.Name = "txtLokomotiva";
            this.txtLokomotiva.Size = new System.Drawing.Size(204, 22);
            this.txtLokomotiva.TabIndex = 202;
            this.txtLokomotiva.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // btnRacun
            // 
            this.btnRacun.Location = new System.Drawing.Point(503, 112);
            this.btnRacun.Margin = new System.Windows.Forms.Padding(4);
            this.btnRacun.Name = "btnRacun";
            this.btnRacun.Size = new System.Drawing.Size(152, 33);
            this.btnRacun.TabIndex = 203;
            this.btnRacun.Text = "Promeni podatke";
            this.btnRacun.UseVisualStyleBackColor = true;
            this.btnRacun.Click += new System.EventHandler(this.btnRacun_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(168, 121);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(143, 17);
            this.label4.TabIndex = 205;
            this.label4.Text = "Masa teretnog vozila:";
            // 
            // txtMasa
            // 
            this.txtMasa.Location = new System.Drawing.Point(320, 118);
            this.txtMasa.Margin = new System.Windows.Forms.Padding(4);
            this.txtMasa.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtMasa.Name = "txtMasa";
            this.txtMasa.Size = new System.Drawing.Size(120, 22);
            this.txtMasa.TabIndex = 204;
            this.txtMasa.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(445, 122);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(28, 17);
            this.label6.TabIndex = 206;
            this.label6.Text = "KG";
            // 
            // cboSerija
            // 
            this.cboSerija.FormattingEnabled = true;
            this.cboSerija.Location = new System.Drawing.Point(320, 78);
            this.cboSerija.Margin = new System.Windows.Forms.Padding(4);
            this.cboSerija.Name = "cboSerija";
            this.cboSerija.Size = new System.Drawing.Size(189, 24);
            this.cboSerija.TabIndex = 207;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(320, 58);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(44, 17);
            this.label7.TabIndex = 208;
            this.label7.Text = "Serija";
            // 
            // btn_Dokumenta
            // 
            this.btn_Dokumenta.Location = new System.Drawing.Point(494, 12);
            this.btn_Dokumenta.Name = "btn_Dokumenta";
            this.btn_Dokumenta.Size = new System.Drawing.Size(118, 45);
            this.btn_Dokumenta.TabIndex = 209;
            this.btn_Dokumenta.Text = "Dokumenta";
            this.btn_Dokumenta.UseVisualStyleBackColor = true;
            this.btn_Dokumenta.Click += new System.EventHandler(this.btn_Dokumenta_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(19, 198);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(965, 375);
            this.tabControl1.TabIndex = 210;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.dataGridView1);
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(957, 346);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Izmena lokomotive";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.dataGridView2);
            this.tabPage2.Location = new System.Drawing.Point(4, 25);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(957, 346);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Mesta troška";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // dataGridView2
            // 
            this.dataGridView2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView2.Location = new System.Drawing.Point(7, 7);
            this.dataGridView2.Margin = new System.Windows.Forms.Padding(4);
            this.dataGridView2.Name = "dataGridView2";
            this.dataGridView2.RowHeadersWidth = 51;
            this.dataGridView2.Size = new System.Drawing.Size(943, 332);
            this.dataGridView2.TabIndex = 19;
            this.dataGridView2.SelectionChanged += new System.EventHandler(this.dataGridView2_SelectionChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(503, 158);
            this.button1.Margin = new System.Windows.Forms.Padding(4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(152, 33);
            this.button1.TabIndex = 211;
            this.button1.Text = "Prikaži sva mesta";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // cboLokomotiva
            // 
            this.cboLokomotiva.AutoSize = true;
            this.cboLokomotiva.Location = new System.Drawing.Point(752, 122);
            this.cboLokomotiva.Margin = new System.Windows.Forms.Padding(4);
            this.cboLokomotiva.Name = "cboLokomotiva";
            this.cboLokomotiva.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.cboLokomotiva.Size = new System.Drawing.Size(102, 21);
            this.cboLokomotiva.TabIndex = 212;
            this.cboLokomotiva.Text = "Lokomotiva";
            this.cboLokomotiva.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(663, 158);
            this.button2.Margin = new System.Windows.Forms.Padding(4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(263, 33);
            this.button2.TabIndex = 213;
            this.button2.Text = "Proglasi lokomotivu/Auto";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // cboVozilo
            // 
            this.cboVozilo.AutoSize = true;
            this.cboVozilo.Location = new System.Drawing.Point(867, 122);
            this.cboVozilo.Margin = new System.Windows.Forms.Padding(4);
            this.cboVozilo.Name = "cboVozilo";
            this.cboVozilo.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.cboVozilo.Size = new System.Drawing.Size(59, 21);
            this.cboVozilo.TabIndex = 215;
            this.cboVozilo.Text = "Auto";
            this.cboVozilo.UseVisualStyleBackColor = true;
            // 
            // frmLokomotive
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(998, 585);
            this.Controls.Add(this.cboVozilo);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.cboLokomotiva);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.btn_Dokumenta);
            this.Controls.Add(this.cboSerija);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtMasa);
            this.Controls.Add(this.btnRacun);
            this.Controls.Add(this.txtLokomotiva);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.chkDizel);
            this.Controls.Add(this.chkAktivna);
            this.Controls.Add(this.comboBox2);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "frmLokomotive";
            this.Text = "Lokomotive";
            this.Load += new System.EventHandler(this.frmLokomotive_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMasa)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox chkAktivna;
        private System.Windows.Forms.CheckBox chkDizel;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.TextBox txtLokomotiva;
        private System.Windows.Forms.Button btnRacun;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown txtMasa;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cboSerija;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button btn_Dokumenta;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.DataGridView dataGridView2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox cboLokomotiva;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.CheckBox cboVozilo;
    }
}