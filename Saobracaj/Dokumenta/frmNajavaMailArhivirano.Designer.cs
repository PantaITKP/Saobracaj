
namespace Saobracaj.Dokumenta
{
    partial class frmNajavaMailArhivirano
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
            this.cb_sPrimanje = new System.Windows.Forms.CheckBox();
            this.cb_sPredaja = new System.Windows.Forms.CheckBox();
            this.cb_pPrimanje = new System.Windows.Forms.CheckBox();
            this.cb_Eta = new System.Windows.Forms.CheckBox();
            this.cbList_Partneri = new System.Windows.Forms.CheckedListBox();
            this.btn_Posalji = new System.Windows.Forms.Button();
            this.btn_PosaljiSvi = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // cb_sPrimanje
            // 
            this.cb_sPrimanje.AutoSize = true;
            this.cb_sPrimanje.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cb_sPrimanje.Location = new System.Drawing.Point(12, 163);
            this.cb_sPrimanje.Name = "cb_sPrimanje";
            this.cb_sPrimanje.Size = new System.Drawing.Size(157, 22);
            this.cb_sPrimanje.TabIndex = 6;
            this.cb_sPrimanje.Text = "Stvarno primanje";
            this.cb_sPrimanje.UseVisualStyleBackColor = true;
            // 
            // cb_sPredaja
            // 
            this.cb_sPredaja.AutoSize = true;
            this.cb_sPredaja.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cb_sPredaja.Location = new System.Drawing.Point(12, 121);
            this.cb_sPredaja.Name = "cb_sPredaja";
            this.cb_sPredaja.Size = new System.Drawing.Size(147, 22);
            this.cb_sPredaja.TabIndex = 7;
            this.cb_sPredaja.Text = "Stvarna predaja";
            this.cb_sPredaja.UseVisualStyleBackColor = true;
            // 
            // cb_pPrimanje
            // 
            this.cb_pPrimanje.AutoSize = true;
            this.cb_pPrimanje.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cb_pPrimanje.Location = new System.Drawing.Point(12, 82);
            this.cb_pPrimanje.Name = "cb_pPrimanje";
            this.cb_pPrimanje.Size = new System.Drawing.Size(183, 22);
            this.cb_pPrimanje.TabIndex = 8;
            this.cb_pPrimanje.Text = "Predviđeno primanje";
            this.cb_pPrimanje.UseVisualStyleBackColor = true;
            // 
            // cb_Eta
            // 
            this.cb_Eta.AutoSize = true;
            this.cb_Eta.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cb_Eta.Location = new System.Drawing.Point(12, 45);
            this.cb_Eta.Name = "cb_Eta";
            this.cb_Eta.Size = new System.Drawing.Size(61, 22);
            this.cb_Eta.TabIndex = 5;
            this.cb_Eta.Text = "ETA";
            this.cb_Eta.UseVisualStyleBackColor = true;
            // 
            // cbList_Partneri
            // 
            this.cbList_Partneri.FormattingEnabled = true;
            this.cbList_Partneri.Location = new System.Drawing.Point(232, 45);
            this.cbList_Partneri.Name = "cbList_Partneri";
            this.cbList_Partneri.Size = new System.Drawing.Size(386, 140);
            this.cbList_Partneri.TabIndex = 9;
            // 
            // btn_Posalji
            // 
            this.btn_Posalji.Location = new System.Drawing.Point(846, 45);
            this.btn_Posalji.Name = "btn_Posalji";
            this.btn_Posalji.Size = new System.Drawing.Size(146, 53);
            this.btn_Posalji.TabIndex = 10;
            this.btn_Posalji.Text = "Posalji izabranima";
            this.btn_Posalji.UseVisualStyleBackColor = true;
            this.btn_Posalji.Click += new System.EventHandler(this.btn_Posalji_Click);
            // 
            // btn_PosaljiSvi
            // 
            this.btn_PosaljiSvi.Location = new System.Drawing.Point(655, 45);
            this.btn_PosaljiSvi.Name = "btn_PosaljiSvi";
            this.btn_PosaljiSvi.Size = new System.Drawing.Size(146, 53);
            this.btn_PosaljiSvi.TabIndex = 11;
            this.btn_PosaljiSvi.Text = "Posalji svima";
            this.btn_PosaljiSvi.UseVisualStyleBackColor = true;
            this.btn_PosaljiSvi.Click += new System.EventHandler(this.btn_PosaljiSvi_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(12, 203);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.RowTemplate.Height = 24;
            this.dataGridView1.Size = new System.Drawing.Size(1573, 543);
            this.dataGridView1.TabIndex = 12;
            // 
            // frmNajavaMailArhivirano
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1597, 758);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.btn_Posalji);
            this.Controls.Add(this.btn_PosaljiSvi);
            this.Controls.Add(this.cbList_Partneri);
            this.Controls.Add(this.cb_sPrimanje);
            this.Controls.Add(this.cb_sPredaja);
            this.Controls.Add(this.cb_pPrimanje);
            this.Controls.Add(this.cb_Eta);
            this.Name = "frmNajavaMailArhivirano";
            this.Text = "frmNajavaMailArhivirano";
            this.Load += new System.EventHandler(this.frmNajavaMailArhivirano_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox cb_sPrimanje;
        private System.Windows.Forms.CheckBox cb_sPredaja;
        private System.Windows.Forms.CheckBox cb_pPrimanje;
        private System.Windows.Forms.CheckBox cb_Eta;
        private System.Windows.Forms.CheckedListBox cbList_Partneri;
        private System.Windows.Forms.Button btn_Posalji;
        private System.Windows.Forms.Button btn_PosaljiSvi;
        private System.Windows.Forms.DataGridView dataGridView1;
    }
}