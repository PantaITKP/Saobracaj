namespace Saobracaj.Dokumenta
{
    partial class frmOsnovnaZarada
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmOsnovnaZarada));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsNew = new System.Windows.Forms.ToolStripButton();
            this.tsSave = new System.Windows.Forms.ToolStripButton();
            this.tsDelete = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsPrvi = new System.Windows.Forms.ToolStripButton();
            this.tsNazad = new System.Windows.Forms.ToolStripButton();
            this.tsNapred = new System.Windows.Forms.ToolStripButton();
            this.tsPoslednja = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton3 = new System.Windows.Forms.ToolStripButton();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.txtMinimalna = new System.Windows.Forms.NumericUpDown();
            this.label18 = new System.Windows.Forms.Label();
            this.txtCiljna = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.cboZaposleni = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.chkSmenski = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.btnStampa = new System.Windows.Forms.Button();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.chkParametar1 = new System.Windows.Forms.CheckBox();
            this.chkParametar2 = new System.Windows.Forms.CheckBox();
            this.txtPrviDeo = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtDrugiDeo = new System.Windows.Forms.NumericUpDown();
            this.btnPostaviPrviDeo = new MetroFramework.Controls.MetroButton();
            this.metroButton1 = new MetroFramework.Controls.MetroButton();
            this.metroButton2 = new MetroFramework.Controls.MetroButton();
            this.metroButton3 = new MetroFramework.Controls.MetroButton();
            this.chkFiksna = new System.Windows.Forms.CheckBox();
            this.metroButton4 = new MetroFramework.Controls.MetroButton();
            this.btnMinimalnaDrzavna = new MetroFramework.Controls.MetroButton();
            this.label5 = new System.Windows.Forms.Label();
            this.txtMinimalnaDrzavna = new System.Windows.Forms.NumericUpDown();
            this.chkBenificirani = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.cboTipRadnika = new System.Windows.Forms.ComboBox();
            this.chkPregleFiksni = new System.Windows.Forms.CheckBox();
            this.txtPrevoz = new System.Windows.Forms.NumericUpDown();
            this.label8 = new System.Windows.Forms.Label();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMinimalna)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtCiljna)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPrviDeo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDrugiDeo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMinimalnaDrzavna)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPrevoz)).BeginInit();
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
            this.tsPoslednja,
            this.toolStripButton1,
            this.toolStripButton2,
            this.toolStripButton3});
            this.toolStrip1.Location = new System.Drawing.Point(20, 60);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(932, 25);
            this.toolStrip1.TabIndex = 6;
            this.toolStrip1.Text = "Štampaj izveštaj";
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
            // toolStripButton1
            // 
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(103, 22);
            this.toolStripButton1.Text = "Povuci iz plata";
            this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
            // 
            // toolStripButton2
            // 
            this.toolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton2.Image")));
            this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton2.Name = "toolStripButton2";
            this.toolStripButton2.Size = new System.Drawing.Size(111, 22);
            this.toolStripButton2.Text = "Prikaži samo fiksne";
            this.toolStripButton2.Click += new System.EventHandler(this.toolStripButton2_Click);
            // 
            // toolStripButton3
            // 
            this.toolStripButton3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton3.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton3.Image")));
            this.toolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton3.Name = "toolStripButton3";
            this.toolStripButton3.Size = new System.Drawing.Size(146, 22);
            this.toolStripButton3.Text = "Prikaži one koji nisu fiksni";
            this.toolStripButton3.Click += new System.EventHandler(this.toolStripButton3_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(20, 220);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(948, 199);
            this.dataGridView1.TabIndex = 32;
            this.dataGridView1.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            // 
            // txtMinimalna
            // 
            this.txtMinimalna.DecimalPlaces = 2;
            this.txtMinimalna.Location = new System.Drawing.Point(243, 127);
            this.txtMinimalna.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtMinimalna.Name = "txtMinimalna";
            this.txtMinimalna.Size = new System.Drawing.Size(75, 20);
            this.txtMinimalna.TabIndex = 67;
            this.txtMinimalna.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(186, 129);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(57, 13);
            this.label18.TabIndex = 66;
            this.label18.Text = "Ugovorna:";
            // 
            // txtCiljna
            // 
            this.txtCiljna.DecimalPlaces = 2;
            this.txtCiljna.Location = new System.Drawing.Point(82, 127);
            this.txtCiljna.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtCiljna.Name = "txtCiljna";
            this.txtCiljna.Size = new System.Drawing.Size(98, 20);
            this.txtCiljna.TabIndex = 69;
            this.txtCiljna.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(41, 128);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 68;
            this.label2.Text = "Ciljna:";
            // 
            // cboZaposleni
            // 
            this.cboZaposleni.BackColor = System.Drawing.Color.White;
            this.cboZaposleni.FormattingEnabled = true;
            this.cboZaposleni.Location = new System.Drawing.Point(82, 100);
            this.cboZaposleni.Name = "cboZaposleni";
            this.cboZaposleni.Size = new System.Drawing.Size(212, 21);
            this.cboZaposleni.TabIndex = 94;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(20, 102);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(56, 13);
            this.label6.TabIndex = 95;
            this.label6.Text = "Zaposleni:";
            // 
            // chkSmenski
            // 
            this.chkSmenski.AutoSize = true;
            this.chkSmenski.Checked = true;
            this.chkSmenski.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSmenski.Location = new System.Drawing.Point(473, 104);
            this.chkSmenski.Name = "chkSmenski";
            this.chkSmenski.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkSmenski.Size = new System.Drawing.Size(101, 17);
            this.chkSmenski.TabIndex = 139;
            this.chkSmenski.Text = "Ima Smenski     ";
            this.chkSmenski.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(734, 20);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(28, 13);
            this.label4.TabIndex = 141;
            this.label4.Text = "Šifra";
            // 
            // btnStampa
            // 
            this.btnStampa.Location = new System.Drawing.Point(874, 18);
            this.btnStampa.Name = "btnStampa";
            this.btnStampa.Size = new System.Drawing.Size(55, 23);
            this.btnStampa.TabIndex = 142;
            this.btnStampa.Text = "Refresh";
            this.btnStampa.UseVisualStyleBackColor = true;
            this.btnStampa.Click += new System.EventHandler(this.btnStampa_Click);
            // 
            // txtPassword
            // 
            this.txtPassword.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.txtPassword.Location = new System.Drawing.Point(768, 20);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(100, 20);
            this.txtPassword.TabIndex = 143;
            this.txtPassword.UseSystemPasswordChar = true;
            // 
            // chkParametar1
            // 
            this.chkParametar1.AutoSize = true;
            this.chkParametar1.Checked = true;
            this.chkParametar1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkParametar1.Location = new System.Drawing.Point(500, 127);
            this.chkParametar1.Name = "chkParametar1";
            this.chkParametar1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkParametar1.Size = new System.Drawing.Size(74, 17);
            this.chkParametar1.TabIndex = 144;
            this.chkParametar1.Text = "Ima Noćni";
            this.chkParametar1.UseVisualStyleBackColor = true;
            // 
            // chkParametar2
            // 
            this.chkParametar2.AutoSize = true;
            this.chkParametar2.Checked = true;
            this.chkParametar2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkParametar2.Location = new System.Drawing.Point(487, 151);
            this.chkParametar2.Name = "chkParametar2";
            this.chkParametar2.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkParametar2.Size = new System.Drawing.Size(87, 17);
            this.chkParametar2.TabIndex = 145;
            this.chkParametar2.Text = "Ima Terenski";
            this.chkParametar2.UseVisualStyleBackColor = true;
            // 
            // txtPrviDeo
            // 
            this.txtPrviDeo.DecimalPlaces = 2;
            this.txtPrviDeo.Location = new System.Drawing.Point(82, 157);
            this.txtPrviDeo.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtPrviDeo.Name = "txtPrviDeo";
            this.txtPrviDeo.Size = new System.Drawing.Size(72, 20);
            this.txtPrviDeo.TabIndex = 147;
            this.txtPrviDeo.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(41, 160);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 146;
            this.label1.Text = "I deo:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(324, 155);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(37, 13);
            this.label3.TabIndex = 148;
            this.label3.Text = "II deo:";
            // 
            // txtDrugiDeo
            // 
            this.txtDrugiDeo.DecimalPlaces = 2;
            this.txtDrugiDeo.Location = new System.Drawing.Point(367, 153);
            this.txtDrugiDeo.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtDrugiDeo.Name = "txtDrugiDeo";
            this.txtDrugiDeo.Size = new System.Drawing.Size(100, 20);
            this.txtDrugiDeo.TabIndex = 149;
            this.txtDrugiDeo.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // btnPostaviPrviDeo
            // 
            this.btnPostaviPrviDeo.Location = new System.Drawing.Point(159, 154);
            this.btnPostaviPrviDeo.Name = "btnPostaviPrviDeo";
            this.btnPostaviPrviDeo.Size = new System.Drawing.Size(159, 23);
            this.btnPostaviPrviDeo.TabIndex = 150;
            this.btnPostaviPrviDeo.Text = "Postavi prvi deo svima isti";
            this.btnPostaviPrviDeo.UseSelectable = true;
            this.btnPostaviPrviDeo.Click += new System.EventHandler(this.btnPostaviPrviDeo_Click);
            // 
            // metroButton1
            // 
            this.metroButton1.Location = new System.Drawing.Point(606, 98);
            this.metroButton1.Name = "metroButton1";
            this.metroButton1.Size = new System.Drawing.Size(156, 23);
            this.metroButton1.TabIndex = 151;
            this.metroButton1.Text = "Prebaci I deo u DelavciPL";
            this.metroButton1.UseSelectable = true;
            this.metroButton1.Click += new System.EventHandler(this.metroButton1_Click);
            // 
            // metroButton2
            // 
            this.metroButton2.Location = new System.Drawing.Point(606, 130);
            this.metroButton2.Name = "metroButton2";
            this.metroButton2.Size = new System.Drawing.Size(156, 23);
            this.metroButton2.TabIndex = 152;
            this.metroButton2.Text = "Prebaci II deo u DelavciPL";
            this.metroButton2.UseSelectable = true;
            this.metroButton2.Click += new System.EventHandler(this.metroButton2_Click);
            // 
            // metroButton3
            // 
            this.metroButton3.Location = new System.Drawing.Point(159, 183);
            this.metroButton3.Name = "metroButton3";
            this.metroButton3.Size = new System.Drawing.Size(159, 23);
            this.metroButton3.TabIndex = 153;
            this.metroButton3.Text = "Postavi drugi deo ";
            this.metroButton3.UseSelectable = true;
            this.metroButton3.Click += new System.EventHandler(this.metroButton3_Click);
            // 
            // chkFiksna
            // 
            this.chkFiksna.AutoSize = true;
            this.chkFiksna.Checked = true;
            this.chkFiksna.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFiksna.Location = new System.Drawing.Point(484, 174);
            this.chkFiksna.Name = "chkFiksna";
            this.chkFiksna.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkFiksna.Size = new System.Drawing.Size(90, 17);
            this.chkFiksna.TabIndex = 154;
            this.chkFiksna.Text = "Fiksna isplata";
            this.chkFiksna.UseVisualStyleBackColor = true;
            // 
            // metroButton4
            // 
            this.metroButton4.Location = new System.Drawing.Point(782, 130);
            this.metroButton4.Name = "metroButton4";
            this.metroButton4.Size = new System.Drawing.Size(156, 23);
            this.metroButton4.TabIndex = 155;
            this.metroButton4.Text = "Sredi podatke pre arhiviranja Largo";
            this.metroButton4.UseSelectable = true;
            this.metroButton4.Click += new System.EventHandler(this.metroButton4_Click);
            // 
            // btnMinimalnaDrzavna
            // 
            this.btnMinimalnaDrzavna.Location = new System.Drawing.Point(782, 98);
            this.btnMinimalnaDrzavna.Name = "btnMinimalnaDrzavna";
            this.btnMinimalnaDrzavna.Size = new System.Drawing.Size(156, 23);
            this.btnMinimalnaDrzavna.TabIndex = 156;
            this.btnMinimalnaDrzavna.Text = "Prebaci I deo Ugovorna";
            this.btnMinimalnaDrzavna.UseSelectable = true;
            this.btnMinimalnaDrzavna.Click += new System.EventHandler(this.metroButton5_Click);
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(324, 120);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(62, 30);
            this.label5.TabIndex = 157;
            this.label5.Text = "Minimalna država:";
            // 
            // txtMinimalnaDrzavna
            // 
            this.txtMinimalnaDrzavna.DecimalPlaces = 2;
            this.txtMinimalnaDrzavna.Location = new System.Drawing.Point(385, 124);
            this.txtMinimalnaDrzavna.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtMinimalnaDrzavna.Name = "txtMinimalnaDrzavna";
            this.txtMinimalnaDrzavna.Size = new System.Drawing.Size(82, 20);
            this.txtMinimalnaDrzavna.TabIndex = 158;
            this.txtMinimalnaDrzavna.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // chkBenificirani
            // 
            this.chkBenificirani.AutoSize = true;
            this.chkBenificirani.Checked = true;
            this.chkBenificirani.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkBenificirani.Location = new System.Drawing.Point(478, 197);
            this.chkBenificirani.Name = "chkBenificirani";
            this.chkBenificirani.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkBenificirani.Size = new System.Drawing.Size(96, 17);
            this.chkBenificirani.TabIndex = 159;
            this.chkBenificirani.Text = "Ima benificirani";
            this.chkBenificirani.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(603, 174);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(63, 13);
            this.label7.TabIndex = 161;
            this.label7.Text = "Tip radnika:";
            // 
            // cboTipRadnika
            // 
            this.cboTipRadnika.BackColor = System.Drawing.Color.White;
            this.cboTipRadnika.FormattingEnabled = true;
            this.cboTipRadnika.Items.AddRange(new object[] {
            "Osnivač",
            "Radnik",
            "Penzioner"});
            this.cboTipRadnika.Location = new System.Drawing.Point(606, 193);
            this.cboTipRadnika.Name = "cboTipRadnika";
            this.cboTipRadnika.Size = new System.Drawing.Size(212, 21);
            this.cboTipRadnika.TabIndex = 160;
            // 
            // chkPregleFiksni
            // 
            this.chkPregleFiksni.AutoSize = true;
            this.chkPregleFiksni.Checked = true;
            this.chkPregleFiksni.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkPregleFiksni.Location = new System.Drawing.Point(589, 19);
            this.chkPregleFiksni.Name = "chkPregleFiksni";
            this.chkPregleFiksni.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.chkPregleFiksni.Size = new System.Drawing.Size(98, 17);
            this.chkPregleFiksni.TabIndex = 162;
            this.chkPregleFiksni.Text = "Rad sa Fiksnim";
            this.chkPregleFiksni.UseVisualStyleBackColor = true;
            // 
            // txtPrevoz
            // 
            this.txtPrevoz.DecimalPlaces = 2;
            this.txtPrevoz.Location = new System.Drawing.Point(367, 179);
            this.txtPrevoz.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.txtPrevoz.Name = "txtPrevoz";
            this.txtPrevoz.Size = new System.Drawing.Size(100, 20);
            this.txtPrevoz.TabIndex = 164;
            this.txtPrevoz.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(324, 181);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(43, 13);
            this.label8.TabIndex = 163;
            this.label8.Text = "Prevoz:";
            // 
            // frmOsnovnaZarada
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(972, 422);
            this.Controls.Add(this.txtPrevoz);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.chkPregleFiksni);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.cboTipRadnika);
            this.Controls.Add(this.chkBenificirani);
            this.Controls.Add(this.txtMinimalnaDrzavna);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.btnMinimalnaDrzavna);
            this.Controls.Add(this.metroButton4);
            this.Controls.Add(this.chkFiksna);
            this.Controls.Add(this.metroButton3);
            this.Controls.Add(this.metroButton2);
            this.Controls.Add(this.metroButton1);
            this.Controls.Add(this.btnPostaviPrviDeo);
            this.Controls.Add(this.txtDrugiDeo);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtPrviDeo);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chkParametar2);
            this.Controls.Add(this.chkParametar1);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.btnStampa);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.chkSmenski);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.cboZaposleni);
            this.Controls.Add(this.txtCiljna);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtMinimalna);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "frmOsnovnaZarada";
            this.Text = "Osnovna zarada";
            this.Load += new System.EventHandler(this.frmOsnovnaZarada_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMinimalna)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtCiljna)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPrviDeo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtDrugiDeo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtMinimalnaDrzavna)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPrevoz)).EndInit();
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
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.NumericUpDown txtMinimalna;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.NumericUpDown txtCiljna;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cboZaposleni;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox chkSmenski;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnStampa;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.CheckBox chkParametar1;
        private System.Windows.Forms.CheckBox chkParametar2;
        private System.Windows.Forms.NumericUpDown txtPrviDeo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown txtDrugiDeo;
        private MetroFramework.Controls.MetroButton btnPostaviPrviDeo;
        private MetroFramework.Controls.MetroButton metroButton1;
        private MetroFramework.Controls.MetroButton metroButton2;
        private MetroFramework.Controls.MetroButton metroButton3;
        private System.Windows.Forms.CheckBox chkFiksna;
        private MetroFramework.Controls.MetroButton metroButton4;
        private MetroFramework.Controls.MetroButton btnMinimalnaDrzavna;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown txtMinimalnaDrzavna;
        private System.Windows.Forms.ToolStripButton toolStripButton2;
        private System.Windows.Forms.ToolStripButton toolStripButton3;
        private System.Windows.Forms.CheckBox chkBenificirani;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cboTipRadnika;
        private System.Windows.Forms.CheckBox chkPregleFiksni;
        private System.Windows.Forms.NumericUpDown txtPrevoz;
        private System.Windows.Forms.Label label8;
    }
}