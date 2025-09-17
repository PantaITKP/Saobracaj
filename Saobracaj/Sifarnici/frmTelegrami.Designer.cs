namespace Saobracaj.Sifarnici
{
    partial class frmTelegrami
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmTelegrami));
            this.label8 = new System.Windows.Forms.Label();
            this.txt_Napomena = new System.Windows.Forms.TextBox();
            this.txt_BrTelegrama = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
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
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.label5 = new System.Windows.Forms.Label();
            this.cboPruga = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txt_kolosek = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.dt_TrajeDo = new System.Windows.Forms.DateTimePicker();
            this.dt_TrajeOd = new System.Windows.Forms.DateTimePicker();
            this.btn_dani = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txt_ID = new System.Windows.Forms.TextBox();
            this.btn_Aktivni = new System.Windows.Forms.Button();
            this.btn_svi = new System.Windows.Forms.Button();
            this.cb_Aktivni = new System.Windows.Forms.CheckBox();
            this.dt_VaziDo = new System.Windows.Forms.DateTimePicker();
            this.dt_VaziOd = new System.Windows.Forms.DateTimePicker();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.timer2 = new System.Windows.Forms.Timer(this.components);
            this.timer3 = new System.Windows.Forms.Timer(this.components);
            this.combo_OdStanice = new System.Windows.Forms.ComboBox();
            this.combo_DoStanice = new System.Windows.Forms.ComboBox();
            this.txt_PDF = new System.Windows.Forms.TextBox();
            this.btn_pdf = new System.Windows.Forms.Button();
            this.btn_prikazi = new System.Windows.Forms.Button();
            this.btn_Sacuvaj = new System.Windows.Forms.Button();
            this.cb_Narocita = new System.Windows.Forms.CheckBox();
            this.btn_narocite = new System.Windows.Forms.Button();
            this.timer4 = new System.Windows.Forms.Timer(this.components);
            this.button1 = new System.Windows.Forms.Button();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(14, 120);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(65, 13);
            this.label8.TabIndex = 79;
            this.label8.Text = "Napomena :";
            // 
            // txt_Napomena
            // 
            this.txt_Napomena.Location = new System.Drawing.Point(16, 137);
            this.txt_Napomena.Multiline = true;
            this.txt_Napomena.Name = "txt_Napomena";
            this.txt_Napomena.Size = new System.Drawing.Size(431, 45);
            this.txt_Napomena.TabIndex = 76;
            // 
            // txt_BrTelegrama
            // 
            this.txt_BrTelegrama.Location = new System.Drawing.Point(203, 49);
            this.txt_BrTelegrama.Name = "txt_BrTelegrama";
            this.txt_BrTelegrama.Size = new System.Drawing.Size(106, 20);
            this.txt_BrTelegrama.TabIndex = 72;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(122, 51);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 13);
            this.label1.TabIndex = 73;
            this.label1.Text = "Broj telegrama";
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
            this.toolStripSeparator2,
            this.toolStripButton1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1256, 27);
            this.toolStrip1.TabIndex = 82;
            this.toolStrip1.Text = "Štampaj izveštaj";
            // 
            // tsNew
            // 
            this.tsNew.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsNew.Image = ((System.Drawing.Image)(resources.GetObject("tsNew.Image")));
            this.tsNew.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsNew.Name = "tsNew";
            this.tsNew.Size = new System.Drawing.Size(24, 24);
            this.tsNew.Text = "Novi";
            this.tsNew.Click += new System.EventHandler(this.tsNew_Click);
            // 
            // tsSave
            // 
            this.tsSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsSave.Image = ((System.Drawing.Image)(resources.GetObject("tsSave.Image")));
            this.tsSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsSave.Name = "tsSave";
            this.tsSave.Size = new System.Drawing.Size(24, 24);
            this.tsSave.Text = "tsSave";
            this.tsSave.Click += new System.EventHandler(this.tsSave_Click);
            // 
            // tsDelete
            // 
            this.tsDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsDelete.Image = ((System.Drawing.Image)(resources.GetObject("tsDelete.Image")));
            this.tsDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDelete.Name = "tsDelete";
            this.tsDelete.Size = new System.Drawing.Size(24, 24);
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
            this.tsPrvi.Size = new System.Drawing.Size(24, 24);
            this.tsPrvi.Text = "toolStripButton1";
            // 
            // tsNazad
            // 
            this.tsNazad.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsNazad.Image = ((System.Drawing.Image)(resources.GetObject("tsNazad.Image")));
            this.tsNazad.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsNazad.Name = "tsNazad";
            this.tsNazad.Size = new System.Drawing.Size(24, 24);
            this.tsNazad.Text = "toolStripButton1";
            // 
            // tsNapred
            // 
            this.tsNapred.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsNapred.Image = ((System.Drawing.Image)(resources.GetObject("tsNapred.Image")));
            this.tsNapred.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsNapred.Name = "tsNapred";
            this.tsNapred.Size = new System.Drawing.Size(24, 24);
            this.tsNapred.Text = "toolStripButton1";
            // 
            // tsPoslednja
            // 
            this.tsPoslednja.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsPoslednja.Image = ((System.Drawing.Image)(resources.GetObject("tsPoslednja.Image")));
            this.tsPoslednja.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsPoslednja.Name = "tsPoslednja";
            this.tsPoslednja.Size = new System.Drawing.Size(24, 24);
            this.tsPoslednja.Text = "toolStripButton1";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 27);
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(101, 24);
            this.toolStripButton1.Text = "Prikaži telegrame";
            this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(17, 188);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.Size = new System.Drawing.Size(1226, 451);
            this.dataGridView1.TabIndex = 109;
            this.dataGridView1.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(14, 85);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(38, 13);
            this.label5.TabIndex = 114;
            this.label5.Text = "Pruga:";
            // 
            // cboPruga
            // 
            this.cboPruga.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(128)))));
            this.cboPruga.FormattingEnabled = true;
            this.cboPruga.Location = new System.Drawing.Point(58, 85);
            this.cboPruga.Name = "cboPruga";
            this.cboPruga.Size = new System.Drawing.Size(292, 21);
            this.cboPruga.TabIndex = 113;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(314, 51);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(45, 13);
            this.label6.TabIndex = 73;
            this.label6.Text = "Kolosek";
            // 
            // txt_kolosek
            // 
            this.txt_kolosek.Location = new System.Drawing.Point(364, 49);
            this.txt_kolosek.Name = "txt_kolosek";
            this.txt_kolosek.Size = new System.Drawing.Size(106, 20);
            this.txt_kolosek.TabIndex = 72;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(481, 39);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(63, 13);
            this.label7.TabIndex = 73;
            this.label7.Text = "Od Stanice:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(482, 71);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(63, 13);
            this.label9.TabIndex = 73;
            this.label9.Text = "Do Stanice:";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(778, 41);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(45, 13);
            this.label10.TabIndex = 73;
            this.label10.Text = "Važi od:";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(778, 71);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(45, 13);
            this.label11.TabIndex = 73;
            this.label11.Text = "Važi do:";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(1056, 71);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(49, 13);
            this.label12.TabIndex = 73;
            this.label12.Text = "Traje do:";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(1056, 43);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(49, 13);
            this.label13.TabIndex = 73;
            this.label13.Text = "Traje od:";
            // 
            // dt_TrajeDo
            // 
            this.dt_TrajeDo.CustomFormat = "HH:mm:ss";
            this.dt_TrajeDo.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dt_TrajeDo.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dt_TrajeDo.Location = new System.Drawing.Point(1111, 65);
            this.dt_TrajeDo.Name = "dt_TrajeDo";
            this.dt_TrajeDo.ShowUpDown = true;
            this.dt_TrajeDo.Size = new System.Drawing.Size(132, 21);
            this.dt_TrajeDo.TabIndex = 77;
            this.dt_TrajeDo.Value = new System.DateTime(2021, 12, 21, 23, 59, 59, 0);
            // 
            // dt_TrajeOd
            // 
            this.dt_TrajeOd.CustomFormat = "HH:mm:ss";
            this.dt_TrajeOd.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dt_TrajeOd.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dt_TrajeOd.Location = new System.Drawing.Point(1111, 37);
            this.dt_TrajeOd.Name = "dt_TrajeOd";
            this.dt_TrajeOd.ShowUpDown = true;
            this.dt_TrajeOd.Size = new System.Drawing.Size(132, 21);
            this.dt_TrajeOd.TabIndex = 77;
            this.dt_TrajeOd.Value = new System.DateTime(2021, 12, 21, 0, 0, 0, 0);
            // 
            // btn_dani
            // 
            this.btn_dani.Location = new System.Drawing.Point(606, 138);
            this.btn_dani.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btn_dani.Name = "btn_dani";
            this.btn_dani.Size = new System.Drawing.Size(99, 30);
            this.btn_dani.TabIndex = 115;
            this.btn_dani.Text = "Narednih 7 dana";
            this.btn_dani.UseVisualStyleBackColor = true;
            this.btn_dani.Click += new System.EventHandler(this.btn_dani_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 51);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(21, 13);
            this.label2.TabIndex = 73;
            this.label2.Text = "ID:";
            // 
            // txt_ID
            // 
            this.txt_ID.Location = new System.Drawing.Point(39, 49);
            this.txt_ID.Name = "txt_ID";
            this.txt_ID.Size = new System.Drawing.Size(78, 20);
            this.txt_ID.TabIndex = 72;
            // 
            // btn_Aktivni
            // 
            this.btn_Aktivni.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_Aktivni.Location = new System.Drawing.Point(709, 138);
            this.btn_Aktivni.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btn_Aktivni.Name = "btn_Aktivni";
            this.btn_Aktivni.Size = new System.Drawing.Size(102, 30);
            this.btn_Aktivni.TabIndex = 116;
            this.btn_Aktivni.Text = "Aktivni";
            this.btn_Aktivni.UseVisualStyleBackColor = true;
            this.btn_Aktivni.Click += new System.EventHandler(this.btn_Aktivni_Click);
            // 
            // btn_svi
            // 
            this.btn_svi.Location = new System.Drawing.Point(925, 137);
            this.btn_svi.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btn_svi.Name = "btn_svi";
            this.btn_svi.Size = new System.Drawing.Size(103, 30);
            this.btn_svi.TabIndex = 116;
            this.btn_svi.Text = "Svi telegrami";
            this.btn_svi.UseVisualStyleBackColor = true;
            this.btn_svi.Click += new System.EventHandler(this.btn_svi_Click);
            // 
            // cb_Aktivni
            // 
            this.cb_Aktivni.AutoSize = true;
            this.cb_Aktivni.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cb_Aktivni.Location = new System.Drawing.Point(484, 102);
            this.cb_Aktivni.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cb_Aktivni.Name = "cb_Aktivni";
            this.cb_Aktivni.Size = new System.Drawing.Size(73, 21);
            this.cb_Aktivni.TabIndex = 117;
            this.cb_Aktivni.Text = "Aktivan";
            this.cb_Aktivni.UseVisualStyleBackColor = true;
            // 
            // dt_VaziDo
            // 
            this.dt_VaziDo.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dt_VaziDo.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dt_VaziDo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dt_VaziDo.Location = new System.Drawing.Point(828, 65);
            this.dt_VaziDo.Name = "dt_VaziDo";
            this.dt_VaziDo.ShowUpDown = true;
            this.dt_VaziDo.Size = new System.Drawing.Size(150, 21);
            this.dt_VaziDo.TabIndex = 77;
            this.dt_VaziDo.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // dt_VaziOd
            // 
            this.dt_VaziOd.CustomFormat = "dd.MM.yyyy HH:mm";
            this.dt_VaziOd.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dt_VaziOd.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dt_VaziOd.Location = new System.Drawing.Point(828, 35);
            this.dt_VaziOd.Name = "dt_VaziOd";
            this.dt_VaziOd.ShowUpDown = true;
            this.dt_VaziOd.Size = new System.Drawing.Size(150, 21);
            this.dt_VaziOd.TabIndex = 77;
            this.dt_VaziOd.Value = new System.DateTime(1900, 1, 1, 0, 0, 0, 0);
            // 
            // timer1
            // 
            this.timer1.Interval = 120000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // timer2
            // 
            this.timer2.Interval = 120000;
            this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
            // 
            // timer3
            // 
            this.timer3.Interval = 120000;
            this.timer3.Tick += new System.EventHandler(this.timer3_Tick);
            // 
            // combo_OdStanice
            // 
            this.combo_OdStanice.FormattingEnabled = true;
            this.combo_OdStanice.Location = new System.Drawing.Point(544, 36);
            this.combo_OdStanice.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.combo_OdStanice.Name = "combo_OdStanice";
            this.combo_OdStanice.Size = new System.Drawing.Size(180, 21);
            this.combo_OdStanice.TabIndex = 118;
            // 
            // combo_DoStanice
            // 
            this.combo_DoStanice.FormattingEnabled = true;
            this.combo_DoStanice.Location = new System.Drawing.Point(544, 68);
            this.combo_DoStanice.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.combo_DoStanice.Name = "combo_DoStanice";
            this.combo_DoStanice.Size = new System.Drawing.Size(180, 21);
            this.combo_DoStanice.TabIndex = 118;
            this.combo_DoStanice.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // txt_PDF
            // 
            this.txt_PDF.Location = new System.Drawing.Point(1051, 138);
            this.txt_PDF.Name = "txt_PDF";
            this.txt_PDF.Size = new System.Drawing.Size(192, 20);
            this.txt_PDF.TabIndex = 72;
            // 
            // btn_pdf
            // 
            this.btn_pdf.Location = new System.Drawing.Point(1051, 111);
            this.btn_pdf.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btn_pdf.Name = "btn_pdf";
            this.btn_pdf.Size = new System.Drawing.Size(83, 21);
            this.btn_pdf.TabIndex = 119;
            this.btn_pdf.Text = "Pronadji PDF";
            this.btn_pdf.UseVisualStyleBackColor = true;
            this.btn_pdf.Click += new System.EventHandler(this.btn_pdf_Click);
            // 
            // btn_prikazi
            // 
            this.btn_prikazi.Location = new System.Drawing.Point(1159, 111);
            this.btn_prikazi.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btn_prikazi.Name = "btn_prikazi";
            this.btn_prikazi.Size = new System.Drawing.Size(83, 21);
            this.btn_prikazi.TabIndex = 119;
            this.btn_prikazi.Text = "Prikaži PDF";
            this.btn_prikazi.UseVisualStyleBackColor = true;
            this.btn_prikazi.Click += new System.EventHandler(this.btn_prikazi_Click);
            // 
            // btn_Sacuvaj
            // 
            this.btn_Sacuvaj.Location = new System.Drawing.Point(1159, 160);
            this.btn_Sacuvaj.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btn_Sacuvaj.Name = "btn_Sacuvaj";
            this.btn_Sacuvaj.Size = new System.Drawing.Size(83, 21);
            this.btn_Sacuvaj.TabIndex = 119;
            this.btn_Sacuvaj.Text = "Sačuvaj PDF";
            this.btn_Sacuvaj.UseVisualStyleBackColor = true;
            this.btn_Sacuvaj.Click += new System.EventHandler(this.btn_Sacuvaj_Click);
            // 
            // cb_Narocita
            // 
            this.cb_Narocita.AutoSize = true;
            this.cb_Narocita.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cb_Narocita.Location = new System.Drawing.Point(606, 102);
            this.cb_Narocita.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cb_Narocita.Name = "cb_Narocita";
            this.cb_Narocita.Size = new System.Drawing.Size(131, 21);
            this.cb_Narocita.TabIndex = 117;
            this.cb_Narocita.Text = "Naročita pošiljka";
            this.cb_Narocita.UseVisualStyleBackColor = true;
            // 
            // btn_narocite
            // 
            this.btn_narocite.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_narocite.Location = new System.Drawing.Point(815, 138);
            this.btn_narocite.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btn_narocite.Name = "btn_narocite";
            this.btn_narocite.Size = new System.Drawing.Size(106, 30);
            this.btn_narocite.TabIndex = 116;
            this.btn_narocite.Text = "Narocite posiljke";
            this.btn_narocite.UseVisualStyleBackColor = true;
            this.btn_narocite.Click += new System.EventHandler(this.btn_narocite_Click);
            // 
            // timer4
            // 
            this.timer4.Interval = 120000;
            this.timer4.Tick += new System.EventHandler(this.timer4_Tick);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(503, 138);
            this.button1.Margin = new System.Windows.Forms.Padding(2);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(99, 30);
            this.button1.TabIndex = 120;
            this.button1.Text = "Narednih 1 dan";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // frmTelegrami
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1256, 614);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btn_Sacuvaj);
            this.Controls.Add(this.btn_prikazi);
            this.Controls.Add(this.btn_pdf);
            this.Controls.Add(this.combo_DoStanice);
            this.Controls.Add(this.combo_OdStanice);
            this.Controls.Add(this.cb_Narocita);
            this.Controls.Add(this.cb_Aktivni);
            this.Controls.Add(this.btn_svi);
            this.Controls.Add(this.btn_narocite);
            this.Controls.Add(this.btn_Aktivni);
            this.Controls.Add(this.btn_dani);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.cboPruga);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.dt_VaziDo);
            this.Controls.Add(this.dt_TrajeOd);
            this.Controls.Add(this.dt_TrajeDo);
            this.Controls.Add(this.dt_VaziOd);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.txt_Napomena);
            this.Controls.Add(this.txt_kolosek);
            this.Controls.Add(this.txt_PDF);
            this.Controls.Add(this.txt_ID);
            this.Controls.Add(this.txt_BrTelegrama);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "frmTelegrami";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Telegrami";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.frmTelegrami_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txt_Napomena;
        private System.Windows.Forms.TextBox txt_BrTelegrama;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsNew;
        private System.Windows.Forms.ToolStripButton tsSave;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton tsPrvi;
        private System.Windows.Forms.ToolStripButton tsNazad;
        private System.Windows.Forms.ToolStripButton tsNapred;
        private System.Windows.Forms.ToolStripButton tsPoslednja;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cboPruga;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txt_kolosek;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.DateTimePicker dt_TrajeDo;
        private System.Windows.Forms.Button btn_dani;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txt_ID;
        private System.Windows.Forms.Button btn_Aktivni;
        private System.Windows.Forms.Button btn_svi;
        private System.Windows.Forms.CheckBox cb_Aktivni;
        private System.Windows.Forms.DateTimePicker dt_TrajeOd;
        private System.Windows.Forms.DateTimePicker dt_VaziDo;
        private System.Windows.Forms.DateTimePicker dt_VaziOd;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Timer timer2;
        private System.Windows.Forms.Timer timer3;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ComboBox combo_OdStanice;
        private System.Windows.Forms.ComboBox combo_DoStanice;
        private System.Windows.Forms.ToolStripButton tsDelete;
        private System.Windows.Forms.TextBox txt_PDF;
        private System.Windows.Forms.Button btn_pdf;
        private System.Windows.Forms.Button btn_prikazi;
        private System.Windows.Forms.Button btn_Sacuvaj;
        private System.Windows.Forms.CheckBox cb_Narocita;
        private System.Windows.Forms.Button btn_narocite;
        private System.Windows.Forms.Timer timer4;
        private System.Windows.Forms.Button button1;
    }
}