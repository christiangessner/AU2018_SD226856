namespace SD226856.RdlcAnalyzer
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.viewParams = new System.Windows.Forms.DataGridView();
            this.menu = new System.Windows.Forms.MenuStrip();
            this.fileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewFields = new System.Windows.Forms.DataGridView();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.panelParams = new System.Windows.Forms.Panel();
            this.labelParams = new System.Windows.Forms.Label();
            this.panelFields = new System.Windows.Forms.Panel();
            this.labelFields = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.viewParams)).BeginInit();
            this.menu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.viewFields)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.panelParams.SuspendLayout();
            this.panelFields.SuspendLayout();
            this.SuspendLayout();
            // 
            // viewParams
            // 
            this.viewParams.AllowUserToAddRows = false;
            this.viewParams.AllowUserToDeleteRows = false;
            this.viewParams.AllowUserToOrderColumns = true;
            this.viewParams.AllowUserToResizeRows = false;
            this.viewParams.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.viewParams.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
            this.viewParams.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.viewParams.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.viewParams.Dock = System.Windows.Forms.DockStyle.Fill;
            this.viewParams.Location = new System.Drawing.Point(0, 24);
            this.viewParams.MultiSelect = false;
            this.viewParams.Name = "viewParams";
            this.viewParams.RowHeadersVisible = false;
            this.viewParams.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.viewParams.Size = new System.Drawing.Size(806, 157);
            this.viewParams.TabIndex = 2;
            // 
            // menu
            // 
            this.menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMenuItem});
            this.menu.Location = new System.Drawing.Point(0, 0);
            this.menu.Name = "menu";
            this.menu.Size = new System.Drawing.Size(806, 24);
            this.menu.TabIndex = 4;
            this.menu.Text = "Menu";
            // 
            // fileMenuItem
            // 
            this.fileMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openMenuItem,
            this.exitMenuItem});
            this.fileMenuItem.Name = "fileMenuItem";
            this.fileMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileMenuItem.Text = "File";
            // 
            // openMenuItem
            // 
            this.openMenuItem.Name = "openMenuItem";
            this.openMenuItem.Size = new System.Drawing.Size(112, 22);
            this.openMenuItem.Text = "Open...";
            this.openMenuItem.Click += new System.EventHandler(this.OpenMenuItemClick);
            // 
            // exitMenuItem
            // 
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Size = new System.Drawing.Size(112, 22);
            this.exitMenuItem.Text = "Exit";
            this.exitMenuItem.Click += new System.EventHandler(this.ExitMenuItemClick);
            // 
            // viewFields
            // 
            this.viewFields.AllowUserToAddRows = false;
            this.viewFields.AllowUserToDeleteRows = false;
            this.viewFields.AllowUserToOrderColumns = true;
            this.viewFields.AllowUserToResizeRows = false;
            this.viewFields.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.viewFields.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
            this.viewFields.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.viewFields.Dock = System.Windows.Forms.DockStyle.Fill;
            this.viewFields.Location = new System.Drawing.Point(0, 24);
            this.viewFields.MultiSelect = false;
            this.viewFields.Name = "viewFields";
            this.viewFields.ReadOnly = true;
            this.viewFields.RowHeadersVisible = false;
            this.viewFields.Size = new System.Drawing.Size(806, 299);
            this.viewFields.TabIndex = 5;
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 24);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.viewParams);
            this.splitContainer.Panel1.Controls.Add(this.panelParams);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.viewFields);
            this.splitContainer.Panel2.Controls.Add(this.panelFields);
            this.splitContainer.Size = new System.Drawing.Size(806, 508);
            this.splitContainer.SplitterDistance = 181;
            this.splitContainer.TabIndex = 6;
            // 
            // panelParams
            // 
            this.panelParams.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.panelParams.Controls.Add(this.labelParams);
            this.panelParams.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelParams.Location = new System.Drawing.Point(0, 0);
            this.panelParams.Name = "panelParams";
            this.panelParams.Size = new System.Drawing.Size(806, 24);
            this.panelParams.TabIndex = 3;
            // 
            // labelParams
            // 
            this.labelParams.AutoSize = true;
            this.labelParams.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.labelParams.Location = new System.Drawing.Point(12, 6);
            this.labelParams.Name = "labelParams";
            this.labelParams.Size = new System.Drawing.Size(95, 13);
            this.labelParams.TabIndex = 0;
            this.labelParams.Text = "Report Parameters";
            // 
            // panelFields
            // 
            this.panelFields.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.panelFields.Controls.Add(this.labelFields);
            this.panelFields.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelFields.Location = new System.Drawing.Point(0, 0);
            this.panelFields.Name = "panelFields";
            this.panelFields.Size = new System.Drawing.Size(806, 24);
            this.panelFields.TabIndex = 6;
            // 
            // labelFields
            // 
            this.labelFields.AutoSize = true;
            this.labelFields.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.labelFields.Location = new System.Drawing.Point(12, 6);
            this.labelFields.Name = "labelFields";
            this.labelFields.Size = new System.Drawing.Size(69, 13);
            this.labelFields.TabIndex = 0;
            this.labelFields.Text = "Report Fields";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(806, 532);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.menu);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menu;
            this.Name = "MainForm";
            this.Text = "SD226856 - Report Analyzer";
            ((System.ComponentModel.ISupportInitialize)(this.viewParams)).EndInit();
            this.menu.ResumeLayout(false);
            this.menu.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.viewFields)).EndInit();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.panelParams.ResumeLayout(false);
            this.panelParams.PerformLayout();
            this.panelFields.ResumeLayout(false);
            this.panelFields.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView viewParams;
        private System.Windows.Forms.MenuStrip menu;
        private System.Windows.Forms.ToolStripMenuItem fileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.DataGridView viewFields;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.Panel panelParams;
        private System.Windows.Forms.Label labelParams;
        private System.Windows.Forms.Panel panelFields;
        private System.Windows.Forms.Label labelFields;
    }
}

