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
            this.CoverBox = new System.Windows.Forms.PictureBox();
            this.lblNewChapters = new System.Windows.Forms.Label();
            this.lblOpenSite = new System.Windows.Forms.LinkLabel();
            this.lblDownload = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)(this.CoverBox)).BeginInit();
            this.SuspendLayout();
            // 
            // CoverBox
            // 
            this.CoverBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CoverBox.Cursor = System.Windows.Forms.Cursors.Hand;
            this.CoverBox.Location = new System.Drawing.Point(0, 0);
            this.CoverBox.Name = "CoverBox";
            this.CoverBox.Size = new System.Drawing.Size(180, 230);
            this.CoverBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.CoverBox.TabIndex = 0;
            this.CoverBox.TabStop = false;
            this.CoverBox.Click += new System.EventHandler(this.CoverClicked);
            // 
            // lblNewChapters
            // 
            this.lblNewChapters.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblNewChapters.ForeColor = System.Drawing.Color.White;
            this.lblNewChapters.Location = new System.Drawing.Point(3, 235);
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
            this.lblOpenSite.Location = new System.Drawing.Point(3, 248);
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
            this.lblDownload.Location = new System.Drawing.Point(122, 248);
            this.lblDownload.Name = "lblDownload";
            this.lblDownload.Size = new System.Drawing.Size(55, 13);
            this.lblDownload.TabIndex = 4;
            this.lblDownload.TabStop = true;
            this.lblDownload.Text = "Download";
            this.lblDownload.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.DownloadClicked);
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
            this.Size = new System.Drawing.Size(180, 271);
            ((System.ComponentModel.ISupportInitialize)(this.CoverBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox CoverBox;
        private System.Windows.Forms.Label lblNewChapters;
        private System.Windows.Forms.LinkLabel lblOpenSite;
        private System.Windows.Forms.LinkLabel lblDownload;
    }
}
