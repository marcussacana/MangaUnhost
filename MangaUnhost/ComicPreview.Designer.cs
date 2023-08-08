namespace MangaUnhost
{
    partial class ComicPreview
    {
        /// <summary> 
        /// Variável de designer necessária.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Limpar os recursos que estão sendo usados.
        /// </summary>
        /// <param name="disposing">true se for necessário descartar os recursos gerenciados; caso contrário, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código gerado pelo Designer de Componentes

        /// <summary> 
        /// Método necessário para suporte ao Designer - não modifique 
        /// o conteúdo deste método com o editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.CoverBox = new System.Windows.Forms.PictureBox();
            this.ComicMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ExportAs = new System.Windows.Forms.ToolStripMenuItem();
            this.ExportToCBZ = new System.Windows.Forms.ToolStripMenuItem();
            this.CBZExportToJPG = new System.Windows.Forms.ToolStripMenuItem();
            this.CBZExportToPNG = new System.Windows.Forms.ToolStripMenuItem();
            this.CBZExportToBMP = new System.Windows.Forms.ToolStripMenuItem();
            this.ExportToJPG = new System.Windows.Forms.ToolStripMenuItem();
            this.ExportToPNG = new System.Windows.Forms.ToolStripMenuItem();
            this.ExportToBMP = new System.Windows.Forms.ToolStripMenuItem();
            this.ExportAllAs = new System.Windows.Forms.ToolStripMenuItem();
            this.ConvertTo = new System.Windows.Forms.ToolStripMenuItem();
            this.ConvertToJPG = new System.Windows.Forms.ToolStripMenuItem();
            this.ConvertToPNG = new System.Windows.Forms.ToolStripMenuItem();
            this.ConvertToBMP = new System.Windows.Forms.ToolStripMenuItem();
            this.OpenChapter = new System.Windows.Forms.ToolStripMenuItem();
            this.OpenDirectory = new System.Windows.Forms.ToolStripMenuItem();
            this.Refresh = new System.Windows.Forms.ToolStripMenuItem();
            this.UpdateCheck = new System.Windows.Forms.ToolStripMenuItem();
            this.Translate = new System.Windows.Forms.ToolStripMenuItem();
            this.lblNewChapters = new System.Windows.Forms.Label();
            this.lblOpenSite = new System.Windows.Forms.LinkLabel();
            this.lblDownload = new System.Windows.Forms.LinkLabel();
            this.Delete = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.CoverBox)).BeginInit();
            this.ComicMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // CoverBox
            // 
            this.CoverBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CoverBox.ContextMenuStrip = this.ComicMenuStrip;
            this.CoverBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.CoverBox.Location = new System.Drawing.Point(0, 0);
            this.CoverBox.Name = "CoverBox";
            this.CoverBox.Size = new System.Drawing.Size(180, 260);
            this.CoverBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.CoverBox.TabIndex = 0;
            this.CoverBox.TabStop = false;
            this.CoverBox.Click += new System.EventHandler(this.CoverClicked);
            // 
            // ComicMenuStrip
            // 
            this.ComicMenuStrip.ImageScalingSize = new System.Drawing.Size(18, 18);
            this.ComicMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ExportAs,
            this.ExportAllAs,
            this.ConvertTo,
            this.OpenChapter,
            this.OpenDirectory,
            this.Delete,
            this.Refresh,
            this.UpdateCheck,
            this.Translate});
            this.ComicMenuStrip.Name = "ComicMenuStrip";
            this.ComicMenuStrip.Size = new System.Drawing.Size(199, 245);
            // 
            // ExportAs
            // 
            this.ExportAs.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ExportToCBZ,
            this.ExportToJPG,
            this.ExportToPNG,
            this.ExportToBMP});
            this.ExportAs.Name = "ExportAs";
            this.ExportAs.Size = new System.Drawing.Size(198, 24);
            this.ExportAs.Text = "Export As";
            // 
            // ExportToCBZ
            // 
            this.ExportToCBZ.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CBZExportToJPG,
            this.CBZExportToPNG,
            this.CBZExportToBMP});
            this.ExportToCBZ.Name = "ExportToCBZ";
            this.ExportToCBZ.Size = new System.Drawing.Size(115, 24);
            this.ExportToCBZ.Text = "CBZ";
            this.ExportToCBZ.Click += new System.EventHandler(this.ExportToCBZ_Click);
            // 
            // CBZExportToJPG
            // 
            this.CBZExportToJPG.Name = "CBZExportToJPG";
            this.CBZExportToJPG.Size = new System.Drawing.Size(115, 24);
            this.CBZExportToJPG.Text = "JPG";
            this.CBZExportToJPG.Click += new System.EventHandler(this.CBZExportToJPG_Click);
            // 
            // CBZExportToPNG
            // 
            this.CBZExportToPNG.Name = "CBZExportToPNG";
            this.CBZExportToPNG.Size = new System.Drawing.Size(115, 24);
            this.CBZExportToPNG.Text = "PNG";
            this.CBZExportToPNG.Click += new System.EventHandler(this.CBZExportToPNG_Click);
            // 
            // CBZExportToBMP
            // 
            this.CBZExportToBMP.Name = "CBZExportToBMP";
            this.CBZExportToBMP.Size = new System.Drawing.Size(115, 24);
            this.CBZExportToBMP.Text = "BMP";
            this.CBZExportToBMP.Click += new System.EventHandler(this.CBZExportToBMP_Click);
            // 
            // ExportToJPG
            // 
            this.ExportToJPG.Name = "ExportToJPG";
            this.ExportToJPG.Size = new System.Drawing.Size(115, 24);
            this.ExportToJPG.Text = "JPG";
            this.ExportToJPG.Click += new System.EventHandler(this.ExportToJPG_Click);
            // 
            // ExportToPNG
            // 
            this.ExportToPNG.Name = "ExportToPNG";
            this.ExportToPNG.Size = new System.Drawing.Size(115, 24);
            this.ExportToPNG.Text = "PNG";
            this.ExportToPNG.Click += new System.EventHandler(this.ExportToPNG_Click);
            // 
            // ExportToBMP
            // 
            this.ExportToBMP.Name = "ExportToBMP";
            this.ExportToBMP.Size = new System.Drawing.Size(115, 24);
            this.ExportToBMP.Text = "BMP";
            this.ExportToBMP.Click += new System.EventHandler(this.ExportToBMP_Click);
            // 
            // ExportAllAs
            // 
            this.ExportAllAs.Name = "ExportAllAs";
            this.ExportAllAs.Size = new System.Drawing.Size(198, 24);
            this.ExportAllAs.Text = "Export All As";
            this.ExportAllAs.Click += new System.EventHandler(this.ExportEverythingAs_Clicked);
            // 
            // ConvertTo
            // 
            this.ConvertTo.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ConvertToJPG,
            this.ConvertToPNG,
            this.ConvertToBMP});
            this.ConvertTo.Name = "ConvertTo";
            this.ConvertTo.Size = new System.Drawing.Size(198, 24);
            this.ConvertTo.Text = "Convert To";
            // 
            // ConvertToJPG
            // 
            this.ConvertToJPG.Name = "ConvertToJPG";
            this.ConvertToJPG.Size = new System.Drawing.Size(115, 24);
            this.ConvertToJPG.Text = "JPG";
            this.ConvertToJPG.Click += new System.EventHandler(this.ConvertToJPG_Click);
            // 
            // ConvertToPNG
            // 
            this.ConvertToPNG.Name = "ConvertToPNG";
            this.ConvertToPNG.Size = new System.Drawing.Size(115, 24);
            this.ConvertToPNG.Text = "PNG";
            this.ConvertToPNG.Click += new System.EventHandler(this.ConvertToPNG_Click);
            // 
            // ConvertToBMP
            // 
            this.ConvertToBMP.Name = "ConvertToBMP";
            this.ConvertToBMP.Size = new System.Drawing.Size(115, 24);
            this.ConvertToBMP.Text = "BMP";
            this.ConvertToBMP.Click += new System.EventHandler(this.ConvertToBMP_Click);
            // 
            // OpenChapter
            // 
            this.OpenChapter.Name = "OpenChapter";
            this.OpenChapter.Size = new System.Drawing.Size(198, 24);
            this.OpenChapter.Text = "Open Chapter";
            this.OpenChapter.Visible = false;
            // 
            // OpenDirectory
            // 
            this.OpenDirectory.Name = "OpenDirectory";
            this.OpenDirectory.Size = new System.Drawing.Size(198, 24);
            this.OpenDirectory.Text = "Open Directory";
            this.OpenDirectory.Click += new System.EventHandler(this.OpenDirectory_Click);
            // 
            // Refresh
            // 
            this.Refresh.Name = "Refresh";
            this.Refresh.Size = new System.Drawing.Size(198, 24);
            this.Refresh.Text = "Refresh";
            this.Refresh.Click += new System.EventHandler(this.Refresh_Click);
            // 
            // UpdateCheck
            // 
            this.UpdateCheck.Name = "UpdateCheck";
            this.UpdateCheck.Size = new System.Drawing.Size(198, 24);
            this.UpdateCheck.Text = "Check Updates";
            this.UpdateCheck.Click += new System.EventHandler(this.UpdateCheck_Click);
            // 
            // Translate
            // 
            this.Translate.Name = "Translate";
            this.Translate.Size = new System.Drawing.Size(198, 24);
            this.Translate.Text = "Translate";
            // 
            // lblNewChapters
            // 
            this.lblNewChapters.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblNewChapters.ForeColor = System.Drawing.Color.White;
            this.lblNewChapters.Location = new System.Drawing.Point(3, 265);
            this.lblNewChapters.Name = "lblNewChapters";
            this.lblNewChapters.Size = new System.Drawing.Size(174, 13);
            this.lblNewChapters.TabIndex = 1;
            this.lblNewChapters.Text = "Loading...";
            this.lblNewChapters.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblOpenSite
            // 
            this.lblOpenSite.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblOpenSite.AutoSize = true;
            this.lblOpenSite.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblOpenSite.Location = new System.Drawing.Point(3, 278);
            this.lblOpenSite.Name = "lblOpenSite";
            this.lblOpenSite.Size = new System.Drawing.Size(54, 13);
            this.lblOpenSite.TabIndex = 3;
            this.lblOpenSite.TabStop = true;
            this.lblOpenSite.Text = "Open Site";
            this.lblOpenSite.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OpenSiteClicked);
            // 
            // lblDownload
            // 
            this.lblDownload.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblDownload.AutoSize = true;
            this.lblDownload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblDownload.Location = new System.Drawing.Point(122, 278);
            this.lblDownload.Name = "lblDownload";
            this.lblDownload.Size = new System.Drawing.Size(55, 13);
            this.lblDownload.TabIndex = 4;
            this.lblDownload.TabStop = true;
            this.lblDownload.Text = "Download";
            this.lblDownload.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.DownloadClicked);
            // 
            // Delete
            // 
            this.Delete.Name = "Delete";
            this.Delete.Size = new System.Drawing.Size(198, 24);
            this.Delete.Text = "Delete";
            this.Delete.Click += new System.EventHandler(this.Delete_Click);
            // 
            // ComicPreview
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblDownload);
            this.Controls.Add(this.lblOpenSite);
            this.Controls.Add(this.lblNewChapters);
            this.Controls.Add(this.CoverBox);
            this.Name = "ComicPreview";
            this.Size = new System.Drawing.Size(180, 301);
            ((System.ComponentModel.ISupportInitialize)(this.CoverBox)).EndInit();
            this.ComicMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox CoverBox;
        private System.Windows.Forms.Label lblNewChapters;
        private System.Windows.Forms.LinkLabel lblOpenSite;
        private System.Windows.Forms.LinkLabel lblDownload;
        private System.Windows.Forms.ContextMenuStrip ComicMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem ExportAs;
        private System.Windows.Forms.ToolStripMenuItem ExportToCBZ;
        private System.Windows.Forms.ToolStripMenuItem CBZExportToJPG;
        private System.Windows.Forms.ToolStripMenuItem CBZExportToPNG;
        private System.Windows.Forms.ToolStripMenuItem CBZExportToBMP;
        private System.Windows.Forms.ToolStripMenuItem ExportToJPG;
        private System.Windows.Forms.ToolStripMenuItem ExportToPNG;
        private System.Windows.Forms.ToolStripMenuItem ExportToBMP;
        private System.Windows.Forms.ToolStripMenuItem ConvertTo;
        private System.Windows.Forms.ToolStripMenuItem ConvertToJPG;
        private System.Windows.Forms.ToolStripMenuItem ConvertToPNG;
        private System.Windows.Forms.ToolStripMenuItem ConvertToBMP;
        private System.Windows.Forms.ToolStripMenuItem OpenDirectory;
        private System.Windows.Forms.ToolStripMenuItem Refresh;
        private System.Windows.Forms.ToolStripMenuItem OpenChapter;
        private System.Windows.Forms.ToolStripMenuItem UpdateCheck;
        private System.Windows.Forms.ToolStripMenuItem ExportAllAs;
        private System.Windows.Forms.ToolStripMenuItem Translate;
        private System.Windows.Forms.ToolStripMenuItem Delete;
    }
}
