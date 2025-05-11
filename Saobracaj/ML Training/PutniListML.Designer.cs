namespace Saobracaj.ML_Training
{
    partial class PutniListML
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
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.label2 = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.cboRN = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.dgvCompare = new System.Windows.Forms.DataGridView();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.trenirajModelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.predvidiVremePutaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.simulirajTrasuToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.label4 = new System.Windows.Forms.Label();
            this.txtRN = new System.Windows.Forms.TextBox();
            this.btnUsvoji = new System.Windows.Forms.Button();
            this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCompare)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(12, 55);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(175, 21);
            this.comboBox1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Trasa";
            // 
            // dateTimePicker1
            // 
            this.dateTimePicker1.CustomFormat = "dd.MM.yyyy HH:mm:ss";
            this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePicker1.Location = new System.Drawing.Point(209, 56);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.Size = new System.Drawing.Size(158, 20);
            this.dateTimePicker1.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(206, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(88, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Pocetak prevoza";
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(12, 82);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(584, 587);
            this.dataGridView1.TabIndex = 4;
            // 
            // cboRN
            // 
            this.cboRN.FormattingEnabled = true;
            this.cboRN.Location = new System.Drawing.Point(714, 56);
            this.cboRN.Name = "cboRN";
            this.cboRN.Size = new System.Drawing.Size(121, 21);
            this.cboRN.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(682, 59);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(26, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "RN:";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(851, 40);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(104, 38);
            this.button2.TabIndex = 6;
            this.button2.Text = "Uporedi vremena";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // dgvCompare
            // 
            this.dgvCompare.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvCompare.Location = new System.Drawing.Point(684, 82);
            this.dgvCompare.Name = "dgvCompare";
            this.dgvCompare.Size = new System.Drawing.Size(450, 587);
            this.dgvCompare.TabIndex = 7;
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.Color.RoyalBlue;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.trenirajModelToolStripMenuItem,
            this.predvidiVremePutaToolStripMenuItem,
            this.simulirajTrasuToolStripMenuItem,
            this.refreshToolStripMenuItem});
            this.menuStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1146, 28);
            this.menuStrip1.TabIndex = 9;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // trenirajModelToolStripMenuItem
            // 
            this.trenirajModelToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.trenirajModelToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.trenirajModelToolStripMenuItem.Name = "trenirajModelToolStripMenuItem";
            this.trenirajModelToolStripMenuItem.Size = new System.Drawing.Size(117, 24);
            this.trenirajModelToolStripMenuItem.Text = "Treniraj model";
            this.trenirajModelToolStripMenuItem.Click += new System.EventHandler(this.trenirajModelToolStripMenuItem_Click);
            // 
            // predvidiVremePutaToolStripMenuItem
            // 
            this.predvidiVremePutaToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.predvidiVremePutaToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.predvidiVremePutaToolStripMenuItem.Name = "predvidiVremePutaToolStripMenuItem";
            this.predvidiVremePutaToolStripMenuItem.Size = new System.Drawing.Size(154, 24);
            this.predvidiVremePutaToolStripMenuItem.Text = "Predvidi vreme puta";
            this.predvidiVremePutaToolStripMenuItem.Click += new System.EventHandler(this.predvidiVremePutaToolStripMenuItem_Click);
            // 
            // simulirajTrasuToolStripMenuItem
            // 
            this.simulirajTrasuToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.simulirajTrasuToolStripMenuItem.ForeColor = System.Drawing.Color.DarkOrange;
            this.simulirajTrasuToolStripMenuItem.Name = "simulirajTrasuToolStripMenuItem";
            this.simulirajTrasuToolStripMenuItem.Size = new System.Drawing.Size(117, 24);
            this.simulirajTrasuToolStripMenuItem.Text = "Simuliraj Trasu";
            this.simulirajTrasuToolStripMenuItem.Click += new System.EventHandler(this.simulirajTrasuToolStripMenuItem_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(413, 40);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(26, 13);
            this.label4.TabIndex = 1;
            this.label4.Text = "RN:";
            // 
            // txtRN
            // 
            this.txtRN.Location = new System.Drawing.Point(383, 57);
            this.txtRN.Name = "txtRN";
            this.txtRN.Size = new System.Drawing.Size(100, 20);
            this.txtRN.TabIndex = 10;
            // 
            // btnUsvoji
            // 
            this.btnUsvoji.Location = new System.Drawing.Point(489, 40);
            this.btnUsvoji.Name = "btnUsvoji";
            this.btnUsvoji.Size = new System.Drawing.Size(107, 37);
            this.btnUsvoji.TabIndex = 11;
            this.btnUsvoji.Text = "Usvoji predvidjena vremena";
            this.btnUsvoji.UseVisualStyleBackColor = true;
            this.btnUsvoji.Click += new System.EventHandler(this.btnUsvoji_Click);
            // 
            // refreshToolStripMenuItem
            // 
            this.refreshToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.refreshToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            this.refreshToolStripMenuItem.Size = new System.Drawing.Size(70, 24);
            this.refreshToolStripMenuItem.Text = "Refresh";
            this.refreshToolStripMenuItem.Click += new System.EventHandler(this.refreshToolStripMenuItem_Click);
            // 
            // PutniListML
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1146, 681);
            this.Controls.Add(this.btnUsvoji);
            this.Controls.Add(this.txtRN);
            this.Controls.Add(this.dgvCompare);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.cboRN);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.dateTimePicker1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "PutniListML";
            this.Text = "PutniListML";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCompare)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker dateTimePicker1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.ComboBox cboRN;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.DataGridView dgvCompare;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem trenirajModelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem predvidiVremePutaToolStripMenuItem;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtRN;
        private System.Windows.Forms.Button btnUsvoji;
        private System.Windows.Forms.ToolStripMenuItem simulirajTrasuToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
    }
}