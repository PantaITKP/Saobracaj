
namespace Saobracaj.Sifarnici
{
    partial class frmVrstaAktivnostiArhiv
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
            this.label8 = new System.Windows.Forms.Label();
            this.cboCenovnik = new System.Windows.Forms.ComboBox();
            this.btn_dani = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.button2 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(34, 27);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(70, 17);
            this.label8.TabIndex = 186;
            this.label8.Text = "Cenovnik:";
            // 
            // cboCenovnik
            // 
            this.cboCenovnik.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(128)))));
            this.cboCenovnik.FormattingEnabled = true;
            this.cboCenovnik.Location = new System.Drawing.Point(112, 26);
            this.cboCenovnik.Margin = new System.Windows.Forms.Padding(4);
            this.cboCenovnik.Name = "cboCenovnik";
            this.cboCenovnik.Size = new System.Drawing.Size(388, 24);
            this.cboCenovnik.TabIndex = 185;
            // 
            // btn_dani
            // 
            this.btn_dani.Location = new System.Drawing.Point(537, 11);
            this.btn_dani.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btn_dani.Name = "btn_dani";
            this.btn_dani.Size = new System.Drawing.Size(231, 45);
            this.btn_dani.TabIndex = 187;
            this.btn_dani.Text = "Prebaci Aktuelni u novi cenovnik /Arhiviraj";
            this.btn_dani.UseVisualStyleBackColor = true;
            this.btn_dani.Click += new System.EventHandler(this.btn_dani_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(797, 11);
            this.button1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(231, 45);
            this.button1.TabIndex = 188;
            this.button1.Text = "Prikaži Cenovnik";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(24, 154);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(4);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.Size = new System.Drawing.Size(1257, 580);
            this.dataGridView1.TabIndex = 189;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(537, 78);
            this.button2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(231, 45);
            this.button2.TabIndex = 190;
            this.button2.Text = "Prebaci u aktuelni cenovnik";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // frmVrstaAktivnostiArhiv
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1294, 747);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btn_dani);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.cboCenovnik);
            this.Name = "frmVrstaAktivnostiArhiv";
            this.Text = "Vrste Aktivnosti Arhiv";
            this.Load += new System.EventHandler(this.frmVrstaAktivnostiArhiv_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox cboCenovnik;
        private System.Windows.Forms.Button btn_dani;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button button2;
    }
}