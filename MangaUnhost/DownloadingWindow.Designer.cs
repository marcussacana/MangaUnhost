namespace MangaUnhost {
    partial class DownloadingWindow {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.ProgressUpdate = new System.Windows.Forms.Timer(this.components);
            this.ThemeContainer = new VSContainer();
            this.lblMessage = new System.Windows.Forms.Label();
            this.ProgressBar = new VSRadialProgressBar();
            this.ThemeContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // ProgressUpdate
            // 
            this.ProgressUpdate.Enabled = true;
            this.ProgressUpdate.Interval = 35;
            this.ProgressUpdate.Tick += new System.EventHandler(this.ProgressUpdateTick);
            // 
            // ThemeContainer
            // 
            this.ThemeContainer.AllowClose = false;
            this.ThemeContainer.AllowMaximize = false;
            this.ThemeContainer.AllowMinimize = false;
            this.ThemeContainer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.ThemeContainer.BaseColour = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.ThemeContainer.BorderColour = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(18)))));
            this.ThemeContainer.Controls.Add(this.lblMessage);
            this.ThemeContainer.Controls.Add(this.ProgressBar);
            this.ThemeContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ThemeContainer.FontColour = System.Drawing.Color.FromArgb(((int)(((byte)(153)))), ((int)(((byte)(153)))), ((int)(((byte)(153)))));
            this.ThemeContainer.FontSize = 12;
            this.ThemeContainer.Form = this;
            this.ThemeContainer.FormOrWhole = VSContainer.@__FormOrWhole.Form;
            this.ThemeContainer.HoverColour = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(63)))), ((int)(((byte)(65)))));
            this.ThemeContainer.IconStyle = VSContainer.@__IconStyle.FormIcon;
            this.ThemeContainer.Location = new System.Drawing.Point(0, 0);
            this.ThemeContainer.Name = "ThemeContainer";
            this.ThemeContainer.ShowIcon = true;
            this.ThemeContainer.Size = new System.Drawing.Size(258, 238);
            this.ThemeContainer.TabIndex = 0;
            this.ThemeContainer.Text = "Please Wait...";
            // 
            // lblMessage
            // 
            this.lblMessage.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.lblMessage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(153)))), ((int)(((byte)(153)))), ((int)(((byte)(153)))));
            this.lblMessage.Location = new System.Drawing.Point(12, 143);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new System.Drawing.Size(234, 86);
            this.lblMessage.TabIndex = 1;
            this.lblMessage.Text = "Downloading...";
            this.lblMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ProgressBar
            // 
            this.ProgressBar.BackColor = System.Drawing.Color.Transparent;
            this.ProgressBar.BaseColour = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.ProgressBar.BorderColour = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.ProgressBar.Location = new System.Drawing.Point(89, 62);
            this.ProgressBar.Maximum = 100;
            this.ProgressBar.Name = "ProgressBar";
            this.ProgressBar.ProgressColour = System.Drawing.Color.White;
            this.ProgressBar.RotationAngle = 255;
            this.ProgressBar.ShowText = true;
            this.ProgressBar.Size = new System.Drawing.Size(78, 78);
            this.ProgressBar.StartingAngle = 145;
            this.ProgressBar.TabIndex = 0;
            this.ProgressBar.Text = "LoadingBar";
            this.ProgressBar.TextColour = System.Drawing.Color.FromArgb(((int)(((byte)(153)))), ((int)(((byte)(153)))), ((int)(((byte)(153)))));
            this.ProgressBar.Value = 0;
            // 
            // DownloadingWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(258, 238);
            this.Controls.Add(this.ThemeContainer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximumSize = new System.Drawing.Size(1440, 860);
            this.Name = "DownloadingWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "LoadingWindow";
            this.TransparencyKey = System.Drawing.Color.Fuchsia;
            this.ThemeContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private VSContainer ThemeContainer;
        private VSRadialProgressBar ProgressBar;
        private System.Windows.Forms.Label lblMessage;
        private System.Windows.Forms.Timer ProgressUpdate;
    }
}