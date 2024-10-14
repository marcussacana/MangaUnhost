namespace MangaUnhost
{
    partial class TestDetection
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
			this.themeContainer = new MangaUnhost.VSContainer();
			this.lblExplain = new System.Windows.Forms.Label();
			this.GroupCV = new MangaUnhost.VSGroupBox();
			this.btnHelp = new MangaUnhost.VSButton();
			this.btnOCSetCurrentMode = new MangaUnhost.VSButton();
			this.btnOCTest = new MangaUnhost.VSButton();
			this.label4 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.tbOCSimilarity = new System.Windows.Forms.TextBox();
			this.GroupAF = new MangaUnhost.VSGroupBox();
			this.btnAFSetCurrentMode = new MangaUnhost.VSButton();
			this.btnAFTest = new MangaUnhost.VSButton();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.tbSimilarity = new System.Windows.Forms.TextBox();
			this.tbThreshold = new System.Windows.Forms.TextBox();
			this.imgRight = new System.Windows.Forms.PictureBox();
			this.imgLeft = new System.Windows.Forms.PictureBox();
			this.themeContainer.SuspendLayout();
			this.GroupCV.SuspendLayout();
			this.GroupAF.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.imgRight)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.imgLeft)).BeginInit();
			this.SuspendLayout();
			// 
			// themeContainer
			// 
			this.themeContainer.AllowClose = true;
			this.themeContainer.AllowMaximize = true;
			this.themeContainer.AllowMinimize = true;
			this.themeContainer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
			this.themeContainer.BaseColour = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
			this.themeContainer.BorderColour = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(18)))));
			this.themeContainer.Controls.Add(this.lblExplain);
			this.themeContainer.Controls.Add(this.GroupCV);
			this.themeContainer.Controls.Add(this.GroupAF);
			this.themeContainer.Controls.Add(this.imgRight);
			this.themeContainer.Controls.Add(this.imgLeft);
			this.themeContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.themeContainer.FontColour = System.Drawing.Color.FromArgb(((int)(((byte)(153)))), ((int)(((byte)(153)))), ((int)(((byte)(153)))));
			this.themeContainer.FontSize = 12;
			this.themeContainer.Form = this;
			this.themeContainer.FormOrWhole = MangaUnhost.VSContainer.@__FormOrWhole.WholeApplication;
			this.themeContainer.HoverColour = System.Drawing.Color.FromArgb(((int)(((byte)(63)))), ((int)(((byte)(63)))), ((int)(((byte)(65)))));
			this.themeContainer.IconStyle = MangaUnhost.VSContainer.@__IconStyle.FormIcon;
			this.themeContainer.Location = new System.Drawing.Point(0, 0);
			this.themeContainer.Name = "themeContainer";
			this.themeContainer.NoTitleWrap = false;
			this.themeContainer.ShowDots = false;
			this.themeContainer.ShowIcon = true;
			this.themeContainer.Size = new System.Drawing.Size(951, 648);
			this.themeContainer.TabIndex = 6;
			this.themeContainer.Text = "Text Detection Tuner";
			// 
			// lblExplain
			// 
			this.lblExplain.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.lblExplain.AutoSize = true;
			this.lblExplain.BackColor = System.Drawing.Color.Transparent;
			this.lblExplain.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(129)))), ((int)(((byte)(129)))), ((int)(((byte)(131)))));
			this.lblExplain.Location = new System.Drawing.Point(326, 6);
			this.lblExplain.Name = "lblExplain";
			this.lblExplain.Size = new System.Drawing.Size(287, 26);
			this.lblExplain.TabIndex = 8;
			this.lblExplain.Text = "Click at any image to change it.\r\nYou must select the same page but with differen" +
    "t translation\r\n";
			this.lblExplain.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// GroupCV
			// 
			this.GroupCV.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.GroupCV.BorderColour = System.Drawing.Color.FromArgb(((int)(((byte)(2)))), ((int)(((byte)(118)))), ((int)(((byte)(196)))));
			this.GroupCV.Controls.Add(this.btnHelp);
			this.GroupCV.Controls.Add(this.btnOCSetCurrentMode);
			this.GroupCV.Controls.Add(this.btnOCTest);
			this.GroupCV.Controls.Add(this.label4);
			this.GroupCV.Controls.Add(this.label6);
			this.GroupCV.Controls.Add(this.tbOCSimilarity);
			this.GroupCV.Font = new System.Drawing.Font("Segoe UI", 10F);
			this.GroupCV.HeaderColour = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
			this.GroupCV.Location = new System.Drawing.Point(483, 501);
			this.GroupCV.MainColour = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(38)))));
			this.GroupCV.Name = "GroupCV";
			this.GroupCV.Size = new System.Drawing.Size(465, 135);
			this.GroupCV.TabIndex = 8;
			this.GroupCV.Text = "OpenCV (Recomended)";
			this.GroupCV.TextColour = System.Drawing.Color.FromArgb(((int)(((byte)(129)))), ((int)(((byte)(129)))), ((int)(((byte)(131)))));
			// 
			// btnHelp
			// 
			this.btnHelp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
			this.btnHelp.BaseColour = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
			this.btnHelp.BorderColour = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(18)))));
			this.btnHelp.FontColour = System.Drawing.Color.FromArgb(((int)(((byte)(153)))), ((int)(((byte)(153)))), ((int)(((byte)(153)))));
			this.btnHelp.HoverColour = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(62)))));
			this.btnHelp.ImageAlignment = MangaUnhost.VSButton.@__ImageAlignment.Left;
			this.btnHelp.ImageChoice = null;
			this.btnHelp.Location = new System.Drawing.Point(7, 103);
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.ShowBorder = true;
			this.btnHelp.ShowImage = false;
			this.btnHelp.ShowText = true;
			this.btnHelp.Size = new System.Drawing.Size(453, 27);
			this.btnHelp.TabIndex = 8;
			this.btnHelp.Text = "Help";
			this.btnHelp.TextAlignment = System.Drawing.StringAlignment.Center;
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			// 
			// btnOCSetCurrentMode
			// 
			this.btnOCSetCurrentMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
			this.btnOCSetCurrentMode.BaseColour = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
			this.btnOCSetCurrentMode.BorderColour = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(18)))));
			this.btnOCSetCurrentMode.FontColour = System.Drawing.Color.FromArgb(((int)(((byte)(153)))), ((int)(((byte)(153)))), ((int)(((byte)(153)))));
			this.btnOCSetCurrentMode.HoverColour = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(62)))));
			this.btnOCSetCurrentMode.ImageAlignment = MangaUnhost.VSButton.@__ImageAlignment.Left;
			this.btnOCSetCurrentMode.ImageChoice = null;
			this.btnOCSetCurrentMode.Location = new System.Drawing.Point(254, 71);
			this.btnOCSetCurrentMode.Name = "btnOCSetCurrentMode";
			this.btnOCSetCurrentMode.ShowBorder = true;
			this.btnOCSetCurrentMode.ShowImage = false;
			this.btnOCSetCurrentMode.ShowText = true;
			this.btnOCSetCurrentMode.Size = new System.Drawing.Size(206, 27);
			this.btnOCSetCurrentMode.TabIndex = 7;
			this.btnOCSetCurrentMode.Text = "Use this Settings";
			this.btnOCSetCurrentMode.TextAlignment = System.Drawing.StringAlignment.Center;
			this.btnOCSetCurrentMode.Click += new System.EventHandler(this.btnOCSetCurrentMode_Click);
			// 
			// btnOCTest
			// 
			this.btnOCTest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
			this.btnOCTest.BaseColour = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
			this.btnOCTest.BorderColour = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(18)))));
			this.btnOCTest.FontColour = System.Drawing.Color.FromArgb(((int)(((byte)(153)))), ((int)(((byte)(153)))), ((int)(((byte)(153)))));
			this.btnOCTest.HoverColour = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(62)))));
			this.btnOCTest.ImageAlignment = MangaUnhost.VSButton.@__ImageAlignment.Left;
			this.btnOCTest.ImageChoice = null;
			this.btnOCTest.Location = new System.Drawing.Point(254, 37);
			this.btnOCTest.Name = "btnOCTest";
			this.btnOCTest.ShowBorder = true;
			this.btnOCTest.ShowImage = false;
			this.btnOCTest.ShowText = true;
			this.btnOCTest.Size = new System.Drawing.Size(207, 27);
			this.btnOCTest.TabIndex = 6;
			this.btnOCTest.Text = "Test Settings";
			this.btnOCTest.TextAlignment = System.Drawing.StringAlignment.Center;
			this.btnOCTest.Click += new System.EventHandler(this.btnOCTest_Click);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.BackColor = System.Drawing.Color.Transparent;
			this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(129)))), ((int)(((byte)(129)))), ((int)(((byte)(131)))));
			this.label4.Location = new System.Drawing.Point(28, 77);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(196, 21);
			this.label4.TabIndex = 6;
			this.label4.Text = "(any value from 0.0 up 1.0)";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.BackColor = System.Drawing.Color.Transparent;
			this.label6.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(129)))), ((int)(((byte)(129)))), ((int)(((byte)(131)))));
			this.label6.Location = new System.Drawing.Point(3, 37);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(84, 21);
			this.label6.TabIndex = 4;
			this.label6.Text = "Sensitivity:";
			// 
			// tbOCSimilarity
			// 
			this.tbOCSimilarity.Location = new System.Drawing.Point(93, 37);
			this.tbOCSimilarity.Name = "tbOCSimilarity";
			this.tbOCSimilarity.Size = new System.Drawing.Size(156, 28);
			this.tbOCSimilarity.TabIndex = 2;
			this.tbOCSimilarity.Text = "0.05";
			// 
			// GroupAF
			// 
			this.GroupAF.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.GroupAF.BorderColour = System.Drawing.Color.FromArgb(((int)(((byte)(2)))), ((int)(((byte)(118)))), ((int)(((byte)(196)))));
			this.GroupAF.Controls.Add(this.btnAFSetCurrentMode);
			this.GroupAF.Controls.Add(this.btnAFTest);
			this.GroupAF.Controls.Add(this.label3);
			this.GroupAF.Controls.Add(this.label2);
			this.GroupAF.Controls.Add(this.label1);
			this.GroupAF.Controls.Add(this.tbSimilarity);
			this.GroupAF.Controls.Add(this.tbThreshold);
			this.GroupAF.Font = new System.Drawing.Font("Segoe UI", 10F);
			this.GroupAF.HeaderColour = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
			this.GroupAF.Location = new System.Drawing.Point(12, 501);
			this.GroupAF.MainColour = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(38)))));
			this.GroupAF.Name = "GroupAF";
			this.GroupAF.Size = new System.Drawing.Size(465, 135);
			this.GroupAF.TabIndex = 2;
			this.GroupAF.Text = "AForge";
			this.GroupAF.TextColour = System.Drawing.Color.FromArgb(((int)(((byte)(129)))), ((int)(((byte)(129)))), ((int)(((byte)(131)))));
			// 
			// btnAFSetCurrentMode
			// 
			this.btnAFSetCurrentMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
			this.btnAFSetCurrentMode.BaseColour = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
			this.btnAFSetCurrentMode.BorderColour = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(18)))));
			this.btnAFSetCurrentMode.FontColour = System.Drawing.Color.FromArgb(((int)(((byte)(153)))), ((int)(((byte)(153)))), ((int)(((byte)(153)))));
			this.btnAFSetCurrentMode.HoverColour = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(62)))));
			this.btnAFSetCurrentMode.ImageAlignment = MangaUnhost.VSButton.@__ImageAlignment.Left;
			this.btnAFSetCurrentMode.ImageChoice = null;
			this.btnAFSetCurrentMode.Location = new System.Drawing.Point(253, 71);
			this.btnAFSetCurrentMode.Name = "btnAFSetCurrentMode";
			this.btnAFSetCurrentMode.ShowBorder = true;
			this.btnAFSetCurrentMode.ShowImage = false;
			this.btnAFSetCurrentMode.ShowText = true;
			this.btnAFSetCurrentMode.Size = new System.Drawing.Size(207, 27);
			this.btnAFSetCurrentMode.TabIndex = 7;
			this.btnAFSetCurrentMode.Text = "Use this Settings";
			this.btnAFSetCurrentMode.TextAlignment = System.Drawing.StringAlignment.Center;
			this.btnAFSetCurrentMode.Click += new System.EventHandler(this.btnAFSetCurrentMode_Click);
			// 
			// btnAFTest
			// 
			this.btnAFTest.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
			this.btnAFTest.BaseColour = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
			this.btnAFTest.BorderColour = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(15)))), ((int)(((byte)(18)))));
			this.btnAFTest.FontColour = System.Drawing.Color.FromArgb(((int)(((byte)(153)))), ((int)(((byte)(153)))), ((int)(((byte)(153)))));
			this.btnAFTest.HoverColour = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(62)))));
			this.btnAFTest.ImageAlignment = MangaUnhost.VSButton.@__ImageAlignment.Left;
			this.btnAFTest.ImageChoice = null;
			this.btnAFTest.Location = new System.Drawing.Point(254, 37);
			this.btnAFTest.Name = "btnAFTest";
			this.btnAFTest.ShowBorder = true;
			this.btnAFTest.ShowImage = false;
			this.btnAFTest.ShowText = true;
			this.btnAFTest.Size = new System.Drawing.Size(207, 27);
			this.btnAFTest.TabIndex = 6;
			this.btnAFTest.Text = "Test Settings";
			this.btnAFTest.TextAlignment = System.Drawing.StringAlignment.Center;
			this.btnAFTest.Click += new System.EventHandler(this.btnAFTest_Click);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.BackColor = System.Drawing.Color.Transparent;
			this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(129)))), ((int)(((byte)(129)))), ((int)(((byte)(131)))));
			this.label3.Location = new System.Drawing.Point(22, 103);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(196, 21);
			this.label3.TabIndex = 6;
			this.label3.Text = "(any value from 0.0 up 1.0)";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.BackColor = System.Drawing.Color.Transparent;
			this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(129)))), ((int)(((byte)(129)))), ((int)(((byte)(131)))));
			this.label2.Location = new System.Drawing.Point(3, 73);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(82, 21);
			this.label2.TabIndex = 5;
			this.label2.Text = "Threshold:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.BackColor = System.Drawing.Color.Transparent;
			this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(129)))), ((int)(((byte)(129)))), ((int)(((byte)(131)))));
			this.label1.Location = new System.Drawing.Point(3, 37);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(84, 21);
			this.label1.TabIndex = 4;
			this.label1.Text = "Sensitivity:";
			// 
			// tbSimilarity
			// 
			this.tbSimilarity.Location = new System.Drawing.Point(93, 37);
			this.tbSimilarity.Name = "tbSimilarity";
			this.tbSimilarity.Size = new System.Drawing.Size(156, 28);
			this.tbSimilarity.TabIndex = 2;
			this.tbSimilarity.Text = "0.01";
			// 
			// tbThreshold
			// 
			this.tbThreshold.Enabled = false;
			this.tbThreshold.Location = new System.Drawing.Point(93, 71);
			this.tbThreshold.Name = "tbThreshold";
			this.tbThreshold.Size = new System.Drawing.Size(156, 28);
			this.tbThreshold.TabIndex = 3;
			this.tbThreshold.Text = "0.8";
			// 
			// imgRight
			// 
			this.imgRight.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.imgRight.Image = global::MangaUnhost.Properties.Resources._014_jpg_tl;
			this.imgRight.InitialImage = null;
			this.imgRight.Location = new System.Drawing.Point(483, 36);
			this.imgRight.Name = "imgRight";
			this.imgRight.Size = new System.Drawing.Size(456, 459);
			this.imgRight.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.imgRight.TabIndex = 1;
			this.imgRight.TabStop = false;
			this.imgRight.Click += new System.EventHandler(this.pictureBox2_Click);
			// 
			// imgLeft
			// 
			this.imgLeft.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.imgLeft.Image = global::MangaUnhost.Properties.Resources._014;
			this.imgLeft.InitialImage = null;
			this.imgLeft.Location = new System.Drawing.Point(12, 36);
			this.imgLeft.Name = "imgLeft";
			this.imgLeft.Size = new System.Drawing.Size(465, 459);
			this.imgLeft.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.imgLeft.TabIndex = 0;
			this.imgLeft.TabStop = false;
			this.imgLeft.Click += new System.EventHandler(this.pictureBox1_Click);
			// 
			// TestDetection
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(951, 648);
			this.Controls.Add(this.themeContainer);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.MaximumSize = new System.Drawing.Size(1920, 1026);
			this.Name = "TestDetection";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Text Detection Tuner";
			this.TransparencyKey = System.Drawing.Color.Fuchsia;
			this.themeContainer.ResumeLayout(false);
			this.themeContainer.PerformLayout();
			this.GroupCV.ResumeLayout(false);
			this.GroupCV.PerformLayout();
			this.GroupAF.ResumeLayout(false);
			this.GroupAF.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.imgRight)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.imgLeft)).EndInit();
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox imgLeft;
        private System.Windows.Forms.PictureBox imgRight;
        private System.Windows.Forms.TextBox tbSimilarity;
        private System.Windows.Forms.TextBox tbThreshold;
		private VSContainer themeContainer;
		private VSGroupBox GroupAF;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private VSButton btnAFSetCurrentMode;
		private VSButton btnAFTest;
		private System.Windows.Forms.Label label3;
		private VSGroupBox GroupCV;
		private System.Windows.Forms.Label lblExplain;
		private VSButton btnOCSetCurrentMode;
		private VSButton btnOCTest;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TextBox tbOCSimilarity;
		private VSButton btnHelp;
	}
}