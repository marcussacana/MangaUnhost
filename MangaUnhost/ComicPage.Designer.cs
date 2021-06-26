namespace MangaUnhost
{
    partial class ComicPage
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
            this.lblMore = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblMore
            // 
            this.lblMore.BackColor = System.Drawing.Color.Transparent;
            this.lblMore.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblMore.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblMore.Font = new System.Drawing.Font("Microsoft Sans Serif", 40F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMore.ForeColor = System.Drawing.Color.Blue;
            this.lblMore.Location = new System.Drawing.Point(0, 0);
            this.lblMore.Name = "lblMore";
            this.lblMore.Size = new System.Drawing.Size(240, 370);
            this.lblMore.TabIndex = 0;
            this.lblMore.Text = "...\r\n";
            this.lblMore.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblMore.Click += new System.EventHandler(this.lblMore_Click);
            // 
            // ComicPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.lblMore);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "ComicPage";
            this.Size = new System.Drawing.Size(240, 370);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblMore;
    }
}
