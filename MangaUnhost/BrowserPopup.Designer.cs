namespace MangaUnhost {
    partial class BrowserPopup {
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
            this.Refresh = new System.Windows.Forms.Timer(this.components);
            this.StatusCheck = new System.Windows.Forms.Timer(this.components);
            this.ThemeContainer = new VSContainer();
            this.RadialProgBar = new VSRadialProgressBar();
            this.ScreenBox = new System.Windows.Forms.PictureBox();
            this.ThemeContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ScreenBox)).BeginInit();
            this.SuspendLayout();
            // 
            // Refresh
            // 
            this.Refresh.Interval = 40;
            this.Refresh.Tick += new System.EventHandler(this.RefreshTick);
            // 
            // StatusCheck
            // 
            this.StatusCheck.Interval = 2000;
            this.StatusCheck.Tick += new System.EventHandler(this.StatusCheckTick);
            // 
            // ThemeContainer
            // 
            this.ThemeContainer.AllowClose = true;
            this.ThemeContainer.AllowMaximize = false;
            this.ThemeContainer.AllowMinimize = true;
            this.ThemeContainer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.ThemeContainer.BaseColour = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.ThemeContainer.BorderColour = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(18)))));
            this.ThemeContainer.Controls.Add(this.RadialProgBar);
            this.ThemeContainer.Controls.Add(this.ScreenBox);
            this.ThemeContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ThemeContainer.FontColour = System.Drawing.Color.FromArgb(((int)(((byte)(153)))), ((int)(((byte)(153)))), ((int)(((byte)(153)))));
            this.ThemeContainer.FontSize = 12;
            this.ThemeContainer.Form = this;
            this.ThemeContainer.FormOrWhole = VSContainer.@__FormOrWhole.Form;
            this.ThemeContainer.HoverColour = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(63)))), ((int)(((byte)(65)))));
            this.ThemeContainer.IconStyle = VSContainer.@__IconStyle.FormIcon;
            this.ThemeContainer.Location = new System.Drawing.Point(0, 0);
            this.ThemeContainer.Name = "ThemeContainer";
            this.ThemeContainer.NoTitleWrap = false;
            this.ThemeContainer.ShowDots = false;
            this.ThemeContainer.ShowIcon = true;
            this.ThemeContainer.Size = new System.Drawing.Size(400, 620);
            this.ThemeContainer.TabIndex = 0;
            // 
            // RadialProgBar
            // 
            this.RadialProgBar.BackColor = System.Drawing.Color.Transparent;
            this.RadialProgBar.BaseColour = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.RadialProgBar.BorderColour = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.RadialProgBar.Location = new System.Drawing.Point(156, 262);
            this.RadialProgBar.Maximum = 100;
            this.RadialProgBar.MaximumSize = new System.Drawing.Size(78, 78);
            this.RadialProgBar.Name = "RadialProgBar";
            this.RadialProgBar.ProgressColour = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(62)))), ((int)(((byte)(66)))));
            this.RadialProgBar.RotationAngle = 255;
            this.RadialProgBar.ShowText = false;
            this.RadialProgBar.Size = new System.Drawing.Size(78, 78);
            this.RadialProgBar.StartingAngle = 360;
            this.RadialProgBar.TabIndex = 1;
            this.RadialProgBar.TextColour = System.Drawing.Color.FromArgb(((int)(((byte)(153)))), ((int)(((byte)(153)))), ((int)(((byte)(153)))));
            this.RadialProgBar.Value = 100;
            this.RadialProgBar.Visible = false;
            // 
            // ScreenBox
            // 
            this.ScreenBox.BackColor = System.Drawing.Color.White;
            this.ScreenBox.Location = new System.Drawing.Point(0, 39);
            this.ScreenBox.Name = "ScreenBox";
            this.ScreenBox.Size = new System.Drawing.Size(400, 580);
            this.ScreenBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.ScreenBox.TabIndex = 0;
            this.ScreenBox.TabStop = false;
            this.ScreenBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ScreenMouseDown);
            this.ScreenBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ScreenMouseMove);
            this.ScreenBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ScreenMouseUp);
            // 
            // BrowserPopup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 620);
            this.Controls.Add(this.ThemeContainer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.KeyPreview = true;
            this.MaximumSize = new System.Drawing.Size(1440, 860);
            this.Name = "BrowserPopup";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "SolveCaptcha";
            this.TransparencyKey = System.Drawing.Color.Fuchsia;
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.OnKeyPress);
            this.ThemeContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ScreenBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private VSContainer ThemeContainer;
        private System.Windows.Forms.PictureBox ScreenBox;
        private System.Windows.Forms.Timer Refresh;
        private VSRadialProgressBar RadialProgBar;
        private System.Windows.Forms.Timer StatusCheck;
    }
}