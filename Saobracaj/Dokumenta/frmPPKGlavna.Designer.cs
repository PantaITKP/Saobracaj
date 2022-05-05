
namespace Saobracaj.Dokumenta
{
    partial class frmPPKGlavna
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txt_Zaposleni = new System.Windows.Forms.TextBox();
            this.txt_ID = new System.Windows.Forms.TextBox();
            this.btn_PPK = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(12, 114);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.RowTemplate.Height = 24;
            this.dataGridView1.Size = new System.Drawing.Size(1277, 573);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 34);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Zaposleni";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(239, 34);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(112, 17);
            this.label2.TabIndex = 1;
            this.label2.Text = "Aktivnost Stavke";
            this.label2.Click += new System.EventHandler(this.label1_Click);
            // 
            // txt_Zaposleni
            // 
            this.txt_Zaposleni.Location = new System.Drawing.Point(12, 58);
            this.txt_Zaposleni.Name = "txt_Zaposleni";
            this.txt_Zaposleni.Size = new System.Drawing.Size(160, 22);
            this.txt_Zaposleni.TabIndex = 2;
            // 
            // txt_ID
            // 
            this.txt_ID.Location = new System.Drawing.Point(242, 58);
            this.txt_ID.Name = "txt_ID";
            this.txt_ID.Size = new System.Drawing.Size(129, 22);
            this.txt_ID.TabIndex = 3;
            // 
            // btn_PPK
            // 
            this.btn_PPK.Location = new System.Drawing.Point(446, 43);
            this.btn_PPK.Name = "btn_PPK";
            this.btn_PPK.Size = new System.Drawing.Size(153, 37);
            this.btn_PPK.TabIndex = 4;
            this.btn_PPK.Text = "Otvori PPK";
            this.btn_PPK.UseVisualStyleBackColor = true;
            this.btn_PPK.Click += new System.EventHandler(this.btn_PPK_Click);
            // 
            // frmPPKGlavna
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1301, 699);
            this.Controls.Add(this.btn_PPK);
            this.Controls.Add(this.txt_ID);
            this.Controls.Add(this.txt_Zaposleni);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.dataGridView1);
            this.Name = "frmPPKGlavna";
            this.Text = "frmPPKGlavna";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txt_Zaposleni;
        private System.Windows.Forms.TextBox txt_ID;
        private System.Windows.Forms.Button btn_PPK;
    }
}