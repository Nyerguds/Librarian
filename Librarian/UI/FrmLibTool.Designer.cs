namespace LibrarianTool
{
    partial class FrmLibTool
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
            this.tsmiFile = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiSave = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiSaveAs = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiReload = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiArchive = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiInsert = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExtract = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiDelete = new System.Windows.Forms.ToolStripMenuItem();
            this.lbFilesList = new System.Windows.Forms.ListBox();
            this.lblFileNameVal = new System.Windows.Forms.Label();
            this.lblArchiveTypeVal = new System.Windows.Forms.Label();
            this.lblSelectedFile = new System.Windows.Forms.Label();
            this.lblFilename = new System.Windows.Forms.Label();
            this.LblArschiveType = new System.Windows.Forms.Label();
            this.lblSelectedFileVal = new System.Windows.Forms.Label();
            this.lblLocation = new System.Windows.Forms.Label();
            this.lblLocationVal = new System.Windows.Forms.Label();
            this.lblStartOffset = new System.Windows.Forms.Label();
            this.lblStartOffsetVal = new System.Windows.Forms.Label();
            this.lblFileSize = new System.Windows.Forms.Label();
            this.lblFileSizeVal = new System.Windows.Forms.Label();
            this.lblFilesVal = new System.Windows.Forms.Label();
            this.lblFiles = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiFile,
            this.tsmiArchive});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(484, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // tsmiFile
            // 
            this.tsmiFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiOpen,
            this.tsmiSave,
            this.tsmiSaveAs,
            this.tsmiReload,
            this.tsmiExit});
            this.tsmiFile.Name = "tsmiFile";
            this.tsmiFile.Size = new System.Drawing.Size(37, 20);
            this.tsmiFile.Text = "&File";
            // 
            // tsmiOpen
            // 
            this.tsmiOpen.Name = "tsmiOpen";
            this.tsmiOpen.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.tsmiOpen.Size = new System.Drawing.Size(236, 22);
            this.tsmiOpen.Text = "&Open archive...";
            this.tsmiOpen.Click += new System.EventHandler(this.tsmiOpen_Click);
            // 
            // tsmiSave
            // 
            this.tsmiSave.Name = "tsmiSave";
            this.tsmiSave.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.tsmiSave.Size = new System.Drawing.Size(236, 22);
            this.tsmiSave.Text = "&Save archive...";
            this.tsmiSave.Click += new System.EventHandler(this.tsmiSave_Click);
            // 
            // tsmiSaveAs
            // 
            this.tsmiSaveAs.Name = "tsmiSaveAs";
            this.tsmiSaveAs.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.S)));
            this.tsmiSaveAs.Size = new System.Drawing.Size(236, 22);
            this.tsmiSaveAs.Text = "Save archive &As...";
            this.tsmiSaveAs.Click += new System.EventHandler(this.tsmiSaveAs_Click);
            // 
            // tsmiReload
            // 
            this.tsmiReload.Name = "tsmiReload";
            this.tsmiReload.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.tsmiReload.Size = new System.Drawing.Size(236, 22);
            this.tsmiReload.Text = "Reload archive";
            this.tsmiReload.Click += new System.EventHandler(this.tsmiReload_Click);
            // 
            // tsmiExit
            // 
            this.tsmiExit.Name = "tsmiExit";
            this.tsmiExit.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.tsmiExit.Size = new System.Drawing.Size(236, 22);
            this.tsmiExit.Text = "Exit";
            this.tsmiExit.Click += new System.EventHandler(this.tsmiExit_Click);
            // 
            // tsmiArchive
            // 
            this.tsmiArchive.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiInsert,
            this.tsmiExtract,
            this.tsmiDelete});
            this.tsmiArchive.Name = "tsmiArchive";
            this.tsmiArchive.Size = new System.Drawing.Size(59, 20);
            this.tsmiArchive.Text = "&Archive";
            // 
            // tsmiInsert
            // 
            this.tsmiInsert.Name = "tsmiInsert";
            this.tsmiInsert.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
            this.tsmiInsert.Size = new System.Drawing.Size(177, 22);
            this.tsmiInsert.Text = "&Insert file...";
            this.tsmiInsert.Click += new System.EventHandler(this.tsmiInsert_Click);
            // 
            // tsmiExtract
            // 
            this.tsmiExtract.Name = "tsmiExtract";
            this.tsmiExtract.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this.tsmiExtract.Size = new System.Drawing.Size(177, 22);
            this.tsmiExtract.Text = "Extract file...";
            this.tsmiExtract.Click += new System.EventHandler(this.tsmiExtract_Click);
            // 
            // tsmiDelete
            // 
            this.tsmiDelete.Name = "tsmiDelete";
            this.tsmiDelete.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.tsmiDelete.Size = new System.Drawing.Size(177, 22);
            this.tsmiDelete.Text = "Delete file";
            this.tsmiDelete.Click += new System.EventHandler(this.tsmiDelete_Click);
            // 
            // lbFilesList
            // 
            this.lbFilesList.AllowDrop = true;
            this.lbFilesList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbFilesList.FormattingEnabled = true;
            this.lbFilesList.Location = new System.Drawing.Point(12, 27);
            this.lbFilesList.Name = "lbFilesList";
            this.lbFilesList.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lbFilesList.Size = new System.Drawing.Size(242, 420);
            this.lbFilesList.TabIndex = 2;
            this.lbFilesList.SelectedIndexChanged += new System.EventHandler(this.lbFilesList_SelectedIndexChanged);
            this.lbFilesList.DragDrop += new System.Windows.Forms.DragEventHandler(this.Lv_DragDrop);
            this.lbFilesList.DragEnter += new System.Windows.Forms.DragEventHandler(this.Lv_DragEnter);
            // 
            // lblFileNameVal
            // 
            this.lblFileNameVal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFileNameVal.AutoSize = true;
            this.lblFileNameVal.Location = new System.Drawing.Point(339, 37);
            this.lblFileNameVal.Name = "lblFileNameVal";
            this.lblFileNameVal.Size = new System.Drawing.Size(72, 13);
            this.lblFileNameVal.TabIndex = 3;
            this.lblFileNameVal.Text = "No file loaded";
            // 
            // lblArchiveTypeVal
            // 
            this.lblArchiveTypeVal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblArchiveTypeVal.AutoSize = true;
            this.lblArchiveTypeVal.Location = new System.Drawing.Point(339, 53);
            this.lblArchiveTypeVal.Name = "lblArchiveTypeVal";
            this.lblArchiveTypeVal.Size = new System.Drawing.Size(10, 13);
            this.lblArchiveTypeVal.TabIndex = 4;
            this.lblArchiveTypeVal.Text = "-";
            // 
            // lblSelectedFile
            // 
            this.lblSelectedFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSelectedFile.AutoSize = true;
            this.lblSelectedFile.Location = new System.Drawing.Point(260, 97);
            this.lblSelectedFile.Name = "lblSelectedFile";
            this.lblSelectedFile.Size = new System.Drawing.Size(26, 13);
            this.lblSelectedFile.TabIndex = 5;
            this.lblSelectedFile.Text = "File:";
            // 
            // lblFilename
            // 
            this.lblFilename.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFilename.AutoSize = true;
            this.lblFilename.Location = new System.Drawing.Point(260, 37);
            this.lblFilename.Name = "lblFilename";
            this.lblFilename.Size = new System.Drawing.Size(46, 13);
            this.lblFilename.TabIndex = 3;
            this.lblFilename.Text = "Archive:";
            // 
            // LblArschiveType
            // 
            this.LblArschiveType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.LblArschiveType.AutoSize = true;
            this.LblArschiveType.Location = new System.Drawing.Point(260, 53);
            this.LblArschiveType.Name = "LblArschiveType";
            this.LblArschiveType.Size = new System.Drawing.Size(31, 13);
            this.LblArschiveType.TabIndex = 4;
            this.LblArschiveType.Text = "Type";
            // 
            // lblSelectedFileVal
            // 
            this.lblSelectedFileVal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSelectedFileVal.AutoSize = true;
            this.lblSelectedFileVal.Location = new System.Drawing.Point(339, 97);
            this.lblSelectedFileVal.Name = "lblSelectedFileVal";
            this.lblSelectedFileVal.Size = new System.Drawing.Size(10, 13);
            this.lblSelectedFileVal.TabIndex = 5;
            this.lblSelectedFileVal.Text = "-";
            // 
            // lblLocation
            // 
            this.lblLocation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLocation.AutoSize = true;
            this.lblLocation.Location = new System.Drawing.Point(260, 113);
            this.lblLocation.Name = "lblLocation";
            this.lblLocation.Size = new System.Drawing.Size(51, 13);
            this.lblLocation.TabIndex = 5;
            this.lblLocation.Text = "Location:";
            // 
            // lblLocationVal
            // 
            this.lblLocationVal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLocationVal.AutoSize = true;
            this.lblLocationVal.Location = new System.Drawing.Point(339, 113);
            this.lblLocationVal.Name = "lblLocationVal";
            this.lblLocationVal.Size = new System.Drawing.Size(10, 13);
            this.lblLocationVal.TabIndex = 5;
            this.lblLocationVal.Text = "-";
            // 
            // lblStartOffset
            // 
            this.lblStartOffset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStartOffset.AutoSize = true;
            this.lblStartOffset.Location = new System.Drawing.Point(260, 129);
            this.lblStartOffset.Name = "lblStartOffset";
            this.lblStartOffset.Size = new System.Drawing.Size(61, 13);
            this.lblStartOffset.TabIndex = 5;
            this.lblStartOffset.Text = "Start offset:";
            // 
            // lblStartOffsetVal
            // 
            this.lblStartOffsetVal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStartOffsetVal.AutoSize = true;
            this.lblStartOffsetVal.Location = new System.Drawing.Point(339, 129);
            this.lblStartOffsetVal.Name = "lblStartOffsetVal";
            this.lblStartOffsetVal.Size = new System.Drawing.Size(10, 13);
            this.lblStartOffsetVal.TabIndex = 5;
            this.lblStartOffsetVal.Text = "-";
            // 
            // lblFileSize
            // 
            this.lblFileSize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFileSize.AutoSize = true;
            this.lblFileSize.Location = new System.Drawing.Point(260, 145);
            this.lblFileSize.Name = "lblFileSize";
            this.lblFileSize.Size = new System.Drawing.Size(47, 13);
            this.lblFileSize.TabIndex = 5;
            this.lblFileSize.Text = "File size:";
            // 
            // lblFileSizeVal
            // 
            this.lblFileSizeVal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFileSizeVal.AutoSize = true;
            this.lblFileSizeVal.Location = new System.Drawing.Point(339, 145);
            this.lblFileSizeVal.Name = "lblFileSizeVal";
            this.lblFileSizeVal.Size = new System.Drawing.Size(10, 13);
            this.lblFileSizeVal.TabIndex = 5;
            this.lblFileSizeVal.Text = "-";
            // 
            // lblFilesVal
            // 
            this.lblFilesVal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFilesVal.AutoSize = true;
            this.lblFilesVal.Location = new System.Drawing.Point(339, 69);
            this.lblFilesVal.Name = "lblFilesVal";
            this.lblFilesVal.Size = new System.Drawing.Size(10, 13);
            this.lblFilesVal.TabIndex = 4;
            this.lblFilesVal.Text = "-";
            // 
            // lblFiles
            // 
            this.lblFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFiles.AutoSize = true;
            this.lblFiles.Location = new System.Drawing.Point(260, 69);
            this.lblFiles.Name = "lblFiles";
            this.lblFiles.Size = new System.Drawing.Size(28, 13);
            this.lblFiles.TabIndex = 4;
            this.lblFiles.Text = "Files";
            // 
            // FrmLibTool
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 461);
            this.Controls.Add(this.lblFileSizeVal);
            this.Controls.Add(this.lblStartOffsetVal);
            this.Controls.Add(this.lblLocationVal);
            this.Controls.Add(this.lblSelectedFileVal);
            this.Controls.Add(this.lblFileSize);
            this.Controls.Add(this.lblStartOffset);
            this.Controls.Add(this.lblLocation);
            this.Controls.Add(this.lblSelectedFile);
            this.Controls.Add(this.lblFiles);
            this.Controls.Add(this.lblFilesVal);
            this.Controls.Add(this.LblArschiveType);
            this.Controls.Add(this.lblArchiveTypeVal);
            this.Controls.Add(this.lblFilename);
            this.Controls.Add(this.lblFileNameVal);
            this.Controls.Add(this.lbFilesList);
            this.Controls.Add(this.menuStrip1);
            this.Icon = global::LibrarianTool.Properties.Resources.LibrarianIcon;
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(350, 230);
            this.Name = "FrmLibTool";
            this.Text = "Librarian";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Frm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.Frm_DragEnter);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem tsmiFile;
        private System.Windows.Forms.ToolStripMenuItem tsmiOpen;
        private System.Windows.Forms.ToolStripMenuItem tsmiSave;
        private System.Windows.Forms.ToolStripMenuItem tsmiReload;
        private System.Windows.Forms.ToolStripMenuItem tsmiExit;
        private System.Windows.Forms.ToolStripMenuItem tsmiArchive;
        private System.Windows.Forms.ToolStripMenuItem tsmiInsert;
        private System.Windows.Forms.ToolStripMenuItem tsmiDelete;
        private System.Windows.Forms.ListBox lbFilesList;
        private System.Windows.Forms.Label lblFileNameVal;
        private System.Windows.Forms.Label lblArchiveTypeVal;
        private System.Windows.Forms.Label lblSelectedFile;
        private System.Windows.Forms.Label lblFilename;
        private System.Windows.Forms.Label LblArschiveType;
        private System.Windows.Forms.Label lblSelectedFileVal;
        private System.Windows.Forms.Label lblLocation;
        private System.Windows.Forms.Label lblLocationVal;
        private System.Windows.Forms.Label lblStartOffset;
        private System.Windows.Forms.Label lblStartOffsetVal;
        private System.Windows.Forms.Label lblFileSize;
        private System.Windows.Forms.Label lblFileSizeVal;
        private System.Windows.Forms.ToolStripMenuItem tsmiExtract;
        private System.Windows.Forms.ToolStripMenuItem tsmiSaveAs;
        private System.Windows.Forms.Label lblFilesVal;
        private System.Windows.Forms.Label lblFiles;
    }
}

