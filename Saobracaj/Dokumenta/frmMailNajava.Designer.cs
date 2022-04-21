
namespace Saobracaj.Dokumenta
{
    partial class frmMailNajava
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
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cb_Eta = new System.Windows.Forms.CheckBox();
            this.cb_pPrimanje = new System.Windows.Forms.CheckBox();
            this.cbList_Partneri = new System.Windows.Forms.CheckedListBox();
            this.btn_Filter = new System.Windows.Forms.Button();
            this.btn_PosaljiSvi = new System.Windows.Forms.Button();
            this.btn_Posalji = new System.Windows.Forms.Button();
            this.btn_Svi = new System.Windows.Forms.Button();
            this.btn_PosaljiFilter = new System.Windows.Forms.Button();
            this.cb_sPrimanje = new System.Windows.Forms.CheckBox();
            this.cb_sPredaja = new System.Windows.Forms.CheckBox();
            this.btn_Arhivirano = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(6, 191);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.RowTemplate.Height = 24;
            this.dataGridView1.Size = new System.Drawing.Size(1531, 732);
            this.dataGridView1.TabIndex = 0;
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(12, 68);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(153, 24);
            this.comboBox1.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 40);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "Status";
            // 
            // cb_Eta
            // 
            this.cb_Eta.AutoSize = true;
            this.cb_Eta.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cb_Eta.Location = new System.Drawing.Point(334, 22);
            this.cb_Eta.Name = "cb_Eta";
            this.cb_Eta.Size = new System.Drawing.Size(61, 22);
            this.cb_Eta.TabIndex = 3;
            this.cb_Eta.Text = "ETA";
            this.cb_Eta.UseVisualStyleBackColor = true;
            // 
            // cb_pPrimanje
            // 
            this.cb_pPrimanje.AutoSize = true;
            this.cb_pPrimanje.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cb_pPrimanje.Location = new System.Drawing.Point(334, 53);
            this.cb_pPrimanje.Name = "cb_pPrimanje";
            this.cb_pPrimanje.Size = new System.Drawing.Size(183, 22);
            this.cb_pPrimanje.TabIndex = 4;
            this.cb_pPrimanje.Text = "Predviđeno primanje";
            this.cb_pPrimanje.UseVisualStyleBackColor = true;
            // 
            // cbList_Partneri
            // 
            this.cbList_Partneri.FormattingEnabled = true;
            this.cbList_Partneri.Location = new System.Drawing.Point(542, 11);
            this.cbList_Partneri.Name = "cbList_Partneri";
            this.cbList_Partneri.Size = new System.Drawing.Size(386, 174);
            this.cbList_Partneri.TabIndex = 5;
            // 
            // btn_Filter
            // 
            this.btn_Filter.Location = new System.Drawing.Point(12, 115);
            this.btn_Filter.Name = "btn_Filter";
            this.btn_Filter.Size = new System.Drawing.Size(115, 33);
            this.btn_Filter.TabIndex = 6;
            this.btn_Filter.Text = "Filtriraj";
            this.btn_Filter.UseVisualStyleBackColor = true;
            this.btn_Filter.Click += new System.EventHandler(this.btn_Filter_Click);
            // 
            // btn_PosaljiSvi
            // 
            this.btn_PosaljiSvi.Location = new System.Drawing.Point(981, 53);
            this.btn_PosaljiSvi.Name = "btn_PosaljiSvi";
            this.btn_PosaljiSvi.Size = new System.Drawing.Size(146, 53);
            this.btn_PosaljiSvi.TabIndex = 7;
            this.btn_PosaljiSvi.Text = "Pošalji svima";
            this.btn_PosaljiSvi.UseVisualStyleBackColor = true;
            this.btn_PosaljiSvi.Click += new System.EventHandler(this.btn_PosaljiSvi_Click);
            // 
            // btn_Posalji
            // 
            this.btn_Posalji.Location = new System.Drawing.Point(1153, 53);
            this.btn_Posalji.Name = "btn_Posalji";
            this.btn_Posalji.Size = new System.Drawing.Size(146, 53);
            this.btn_Posalji.TabIndex = 7;
            this.btn_Posalji.Text = "Pošalji izabranima";
            this.btn_Posalji.UseVisualStyleBackColor = true;
            this.btn_Posalji.Click += new System.EventHandler(this.btn_Posalji_Click);
            // 
            // btn_Svi
            // 
            this.btn_Svi.Location = new System.Drawing.Point(160, 115);
            this.btn_Svi.Name = "btn_Svi";
            this.btn_Svi.Size = new System.Drawing.Size(115, 33);
            this.btn_Svi.TabIndex = 7;
            this.btn_Svi.Text = "Sve najave";
            this.btn_Svi.UseVisualStyleBackColor = true;
            this.btn_Svi.Click += new System.EventHandler(this.btn_Svi_Click);
            // 
            // btn_PosaljiFilter
            // 
            this.btn_PosaljiFilter.Location = new System.Drawing.Point(1329, 53);
            this.btn_PosaljiFilter.Name = "btn_PosaljiFilter";
            this.btn_PosaljiFilter.Size = new System.Drawing.Size(146, 53);
            this.btn_PosaljiFilter.TabIndex = 7;
            this.btn_PosaljiFilter.Text = "Pošalji filtrirano";
            this.btn_PosaljiFilter.UseVisualStyleBackColor = true;
            this.btn_PosaljiFilter.Click += new System.EventHandler(this.btn_PosaljiFilter_Click);
            // 
            // cb_sPrimanje
            // 
            this.cb_sPrimanje.AutoSize = true;
            this.cb_sPrimanje.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cb_sPrimanje.Location = new System.Drawing.Point(334, 84);
            this.cb_sPrimanje.Name = "cb_sPrimanje";
            this.cb_sPrimanje.Size = new System.Drawing.Size(157, 22);
            this.cb_sPrimanje.TabIndex = 4;
            this.cb_sPrimanje.Text = "Stvarno primanje";
            this.cb_sPrimanje.UseVisualStyleBackColor = true;
            // 
            // cb_sPredaja
            // 
            this.cb_sPredaja.AutoSize = true;
            this.cb_sPredaja.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cb_sPredaja.Location = new System.Drawing.Point(334, 115);
            this.cb_sPredaja.Name = "cb_sPredaja";
            this.cb_sPredaja.Size = new System.Drawing.Size(147, 22);
            this.cb_sPredaja.TabIndex = 4;
            this.cb_sPredaja.Text = "Stvarna predaja";
            this.cb_sPredaja.UseVisualStyleBackColor = true;
            // 
            // btn_Arhivirano
            // 
            this.btn_Arhivirano.BackColor = System.Drawing.Color.Orange;
            this.btn_Arhivirano.Location = new System.Drawing.Point(1367, 153);
            this.btn_Arhivirano.Name = "btn_Arhivirano";
            this.btn_Arhivirano.Size = new System.Drawing.Size(170, 32);
            this.btn_Arhivirano.TabIndex = 8;
            this.btn_Arhivirano.Text = "Arhivirano";
            this.btn_Arhivirano.UseVisualStyleBackColor = false;
            this.btn_Arhivirano.Click += new System.EventHandler(this.btn_Arhivirano_Click);
            // 
            // frmMailNajava
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1557, 935);
            this.Controls.Add(this.btn_Arhivirano);
            this.Controls.Add(this.btn_Svi);
            this.Controls.Add(this.btn_PosaljiFilter);
            this.Controls.Add(this.btn_Posalji);
            this.Controls.Add(this.btn_PosaljiSvi);
            this.Controls.Add(this.btn_Filter);
            this.Controls.Add(this.cbList_Partneri);
            this.Controls.Add(this.cb_sPredaja);
            this.Controls.Add(this.cb_sPrimanje);
            this.Controls.Add(this.cb_pPrimanje);
            this.Controls.Add(this.cb_Eta);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.dataGridView1);
            this.Name = "frmMailNajava";
            this.Text = "frmMailNajava";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.frmMailNajava_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox cb_Eta;
        private System.Windows.Forms.CheckBox cb_pPrimanje;
        private System.Windows.Forms.CheckedListBox cbList_Partneri;
        private System.Windows.Forms.Button btn_Filter;
        private System.Windows.Forms.Button btn_PosaljiSvi;
        private System.Windows.Forms.Button btn_Posalji;
        private System.Windows.Forms.Button btn_Svi;
        private System.Windows.Forms.Button btn_PosaljiFilter;
        private System.Windows.Forms.CheckBox cb_sPrimanje;
        private System.Windows.Forms.CheckBox cb_sPredaja;
        private System.Windows.Forms.Button btn_Arhivirano;
    }
}