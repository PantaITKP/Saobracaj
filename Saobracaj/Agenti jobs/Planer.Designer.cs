namespace Saobracaj.Agenti_jobs
{
    partial class Planer
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.sacuvajPlanToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ucitajExcelTraseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPlan = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.txtPlaner = new System.Windows.Forms.RichTextBox();
            this.txtDisp = new System.Windows.Forms.RichTextBox();
            this.tabMreza = new System.Windows.Forms.TabPage();
            this.dgvMreza = new System.Windows.Forms.DataGridView();
            this.tabNajave = new System.Windows.Forms.TabPage();
            this.dgvNajave = new System.Windows.Forms.DataGridView();
            this.tabLokomotive = new System.Windows.Forms.TabPage();
            this.dgvLokomotive = new System.Windows.Forms.DataGridView();
            this.tabTelegrami = new System.Windows.Forms.TabPage();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.menuStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPlan.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabMreza.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvMreza)).BeginInit();
            this.tabNajave.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvNajave)).BeginInit();
            this.tabLokomotive.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLokomotive)).BeginInit();
            this.tabTelegrami.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.Color.SteelBlue;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sacuvajPlanToolStripMenuItem,
            this.ucitajExcelTraseToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1207, 28);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // sacuvajPlanToolStripMenuItem
            // 
            this.sacuvajPlanToolStripMenuItem.BackColor = System.Drawing.Color.MintCream;
            this.sacuvajPlanToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.sacuvajPlanToolStripMenuItem.Name = "sacuvajPlanToolStripMenuItem";
            this.sacuvajPlanToolStripMenuItem.Size = new System.Drawing.Size(104, 24);
            this.sacuvajPlanToolStripMenuItem.Text = "Sačuvaj plan";
            this.sacuvajPlanToolStripMenuItem.Click += new System.EventHandler(this.sacuvajPlanToolStripMenuItem_Click);
            // 
            // ucitajExcelTraseToolStripMenuItem
            // 
            this.ucitajExcelTraseToolStripMenuItem.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.ucitajExcelTraseToolStripMenuItem.BackColor = System.Drawing.Color.Crimson;
            this.ucitajExcelTraseToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ucitajExcelTraseToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.ucitajExcelTraseToolStripMenuItem.Name = "ucitajExcelTraseToolStripMenuItem";
            this.ucitajExcelTraseToolStripMenuItem.Size = new System.Drawing.Size(133, 24);
            this.ucitajExcelTraseToolStripMenuItem.Text = "Učitaj excel trase";
            this.ucitajExcelTraseToolStripMenuItem.Click += new System.EventHandler(this.ucitajExcelTraseToolStripMenuItem_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPlan);
            this.tabControl1.Controls.Add(this.tabMreza);
            this.tabControl1.Controls.Add(this.tabNajave);
            this.tabControl1.Controls.Add(this.tabLokomotive);
            this.tabControl1.Controls.Add(this.tabTelegrami);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabControl1.Location = new System.Drawing.Point(0, 28);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1207, 422);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPlan
            // 
            this.tabPlan.Controls.Add(this.splitContainer1);
            this.tabPlan.Location = new System.Drawing.Point(4, 27);
            this.tabPlan.Name = "tabPlan";
            this.tabPlan.Padding = new System.Windows.Forms.Padding(3);
            this.tabPlan.Size = new System.Drawing.Size(1199, 391);
            this.tabPlan.TabIndex = 0;
            this.tabPlan.Text = "Plan";
            this.tabPlan.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.txtPlaner);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.txtDisp);
            this.splitContainer1.Size = new System.Drawing.Size(1193, 385);
            this.splitContainer1.SplitterDistance = 748;
            this.splitContainer1.TabIndex = 0;
            // 
            // txtPlaner
            // 
            this.txtPlaner.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPlaner.Location = new System.Drawing.Point(0, 0);
            this.txtPlaner.Name = "txtPlaner";
            this.txtPlaner.Size = new System.Drawing.Size(748, 385);
            this.txtPlaner.TabIndex = 0;
            this.txtPlaner.Text = "";
            this.txtPlaner.TextChanged += new System.EventHandler(this.txtPlaner_TextChanged);
            // 
            // txtDisp
            // 
            this.txtDisp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDisp.Location = new System.Drawing.Point(0, 0);
            this.txtDisp.Name = "txtDisp";
            this.txtDisp.Size = new System.Drawing.Size(441, 385);
            this.txtDisp.TabIndex = 0;
            this.txtDisp.Text = "";
            // 
            // tabMreza
            // 
            this.tabMreza.Controls.Add(this.dgvMreza);
            this.tabMreza.Location = new System.Drawing.Point(4, 27);
            this.tabMreza.Name = "tabMreza";
            this.tabMreza.Padding = new System.Windows.Forms.Padding(3);
            this.tabMreza.Size = new System.Drawing.Size(1199, 391);
            this.tabMreza.TabIndex = 1;
            this.tabMreza.Text = "Stanje na mreži";
            this.tabMreza.UseVisualStyleBackColor = true;
            // 
            // dgvMreza
            // 
            this.dgvMreza.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvMreza.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvMreza.Location = new System.Drawing.Point(3, 3);
            this.dgvMreza.Name = "dgvMreza";
            this.dgvMreza.Size = new System.Drawing.Size(1193, 385);
            this.dgvMreza.TabIndex = 0;
            this.dgvMreza.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvMreza_CellEndEdit);
            this.dgvMreza.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvMreza_CellValueChanged);
            this.dgvMreza.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgvMreza_ColumnHeaderMouseClick);
            this.dgvMreza.CurrentCellDirtyStateChanged += new System.EventHandler(this.dgvMreza_CurrentCellDirtyStateChanged);
            this.dgvMreza.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.dgvMreza_DataBindingComplete);
            this.dgvMreza.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.dgvMreza_DataError);
            this.dgvMreza.RowValidated += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvMreza_RowValidated);
            this.dgvMreza.UserDeletedRow += new System.Windows.Forms.DataGridViewRowEventHandler(this.dgvMreza_UserDeletedRow);
            this.dgvMreza.UserDeletingRow += new System.Windows.Forms.DataGridViewRowCancelEventHandler(this.dgvMreza_UserDeletingRow);
            // 
            // tabNajave
            // 
            this.tabNajave.Controls.Add(this.dgvNajave);
            this.tabNajave.Location = new System.Drawing.Point(4, 27);
            this.tabNajave.Name = "tabNajave";
            this.tabNajave.Size = new System.Drawing.Size(1199, 391);
            this.tabNajave.TabIndex = 2;
            this.tabNajave.Text = "Najave vozova";
            this.tabNajave.UseVisualStyleBackColor = true;
            // 
            // dgvNajave
            // 
            this.dgvNajave.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvNajave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvNajave.Location = new System.Drawing.Point(0, 0);
            this.dgvNajave.Name = "dgvNajave";
            this.dgvNajave.Size = new System.Drawing.Size(1199, 391);
            this.dgvNajave.TabIndex = 0;
            this.dgvNajave.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvNajave_CellEndEdit);
            this.dgvNajave.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvNajave_CellValueChanged);
            this.dgvNajave.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgvNajave_ColumnHeaderMouseClick);
            this.dgvNajave.CurrentCellDirtyStateChanged += new System.EventHandler(this.dgvNajave_CurrentCellDirtyStateChanged);
            this.dgvNajave.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.dgvNajave_DataBindingComplete);
            this.dgvNajave.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.dgvNajave_DataError);
            this.dgvNajave.RowValidated += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvNajave_RowValidated);
            this.dgvNajave.UserDeletedRow += new System.Windows.Forms.DataGridViewRowEventHandler(this.dgvMreza_UserDeletedRow);
            this.dgvNajave.UserDeletingRow += new System.Windows.Forms.DataGridViewRowCancelEventHandler(this.dgvNajave_UserDeletingRow);
            // 
            // tabLokomotive
            // 
            this.tabLokomotive.Controls.Add(this.dgvLokomotive);
            this.tabLokomotive.Location = new System.Drawing.Point(4, 27);
            this.tabLokomotive.Name = "tabLokomotive";
            this.tabLokomotive.Size = new System.Drawing.Size(1199, 391);
            this.tabLokomotive.TabIndex = 3;
            this.tabLokomotive.Text = "Lokomotive / bruto";
            this.tabLokomotive.UseVisualStyleBackColor = true;
            // 
            // dgvLokomotive
            // 
            this.dgvLokomotive.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvLokomotive.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvLokomotive.Location = new System.Drawing.Point(0, 0);
            this.dgvLokomotive.Name = "dgvLokomotive";
            this.dgvLokomotive.Size = new System.Drawing.Size(1199, 391);
            this.dgvLokomotive.TabIndex = 0;
            this.dgvLokomotive.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvLokomotive_CellEndEdit);
            this.dgvLokomotive.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgvLokomotive_ColumnHeaderMouseClick);
            this.dgvLokomotive.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvLokomotive_CellValueChanged);
            this.dgvLokomotive.CurrentCellDirtyStateChanged += new System.EventHandler(this.dgvLokomotive_CurrentCellDirtyStateChanged);
            this.dgvLokomotive.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.dgvLokomotive_DataBindingComplete);
            this.dgvLokomotive.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.dgvLokomotive_DataError);
            this.dgvLokomotive.RowValidated += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvLokomotive_RowValidated);
            this.dgvLokomotive.UserDeletedRow += new System.Windows.Forms.DataGridViewRowEventHandler(this.dgvLokomotive_UserDeletedRow);
            this.dgvLokomotive.UserDeletingRow += new System.Windows.Forms.DataGridViewRowCancelEventHandler(this.dgvLokomotive_UserDeletingRow);
            // 
            // tabTelegrami
            // 
            this.tabTelegrami.Controls.Add(this.dataGridView1);
            this.tabTelegrami.Location = new System.Drawing.Point(4, 27);
            this.tabTelegrami.Name = "tabTelegrami";
            this.tabTelegrami.Size = new System.Drawing.Size(1199, 391);
            this.tabTelegrami.TabIndex = 4;
            this.tabTelegrami.Text = "Telegrami";
            this.tabTelegrami.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(1199, 391);
            this.dataGridView1.TabIndex = 0;
            // 
            // Planer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1207, 450);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Planer";
            this.Text = "Planer";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Planer_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Planer_FormClosed);
            this.Shown += new System.EventHandler(this.Planer_Shown);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPlan.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tabMreza.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvMreza)).EndInit();
            this.tabNajave.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvNajave)).EndInit();
            this.tabLokomotive.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvLokomotive)).EndInit();
            this.tabTelegrami.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPlan;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.RichTextBox txtPlaner;
        private System.Windows.Forms.RichTextBox txtDisp;
        private System.Windows.Forms.TabPage tabMreza;
        private System.Windows.Forms.TabPage tabNajave;
        private System.Windows.Forms.TabPage tabLokomotive;
        private System.Windows.Forms.ToolStripMenuItem sacuvajPlanToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ucitajExcelTraseToolStripMenuItem;
        private System.Windows.Forms.DataGridView dgvMreza;
        private System.Windows.Forms.DataGridView dgvNajave;
        private System.Windows.Forms.DataGridView dgvLokomotive;
        private System.Windows.Forms.TabPage tabTelegrami;
        private System.Windows.Forms.DataGridView dataGridView1;
    }
}