namespace MangaUnhost {
    partial class Main {
        /// <summary>
        /// Variável de designer necessária.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpar os recursos que estão sendo usados.
        /// </summary>
        /// <param name="disposing">true se for necessário descartar os recursos gerenciados; caso contrário, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código gerado pelo Windows Form Designer

        /// <summary>
        /// Método necessário para suporte ao Designer - não modifique 
        /// o conteúdo deste método com o editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            MainTimer = new System.Windows.Forms.Timer(components);
            ThemeContainer = new VSContainer();
            StatusBar = new VSStatusBar();
            MainTabMenu = new VSTabControl();
            DownloaderTab = new System.Windows.Forms.TabPage();
            ContainerScrollBar = new VSVerticalScrollBar();
            TitleLabel = new System.Windows.Forms.Label();
            ButtonsContainer = new System.Windows.Forms.FlowLayoutPanel();
            CoverBox = new System.Windows.Forms.PictureBox();
            LibraryTab = new System.Windows.Forms.TabPage();
            LibraryContainer = new ScrollFlowLayoutPanel();
            CrawlerTab = new System.Windows.Forms.TabPage();
            LinksListBox = new VSListBoxWBuiltInScrollBar();
            CrawlerCopyBtn = new VSButton();
            lblRegex = new System.Windows.Forms.Label();
            tbCrawlerRegex = new VSNormalTextBox();
            CrawlerStartBtn = new VSButton();
            lblUrl = new System.Windows.Forms.Label();
            tbCrawlerUrl = new VSNormalTextBox();
            SettingsTab = new System.Windows.Forms.TabPage();
            FeaturesGroupBox = new VSGroupBox();
            btnSetupComparsion = new VSButton();
            panel10 = new System.Windows.Forms.Panel();
            APNGBypassDisRadio = new System.Windows.Forms.RadioButton();
            APNGBypassEnaRadio = new System.Windows.Forms.RadioButton();
            label1 = new System.Windows.Forms.Label();
            panel9 = new System.Windows.Forms.Panel();
            lblLibUpdates = new System.Windows.Forms.Label();
            ManualUpCheckRadio = new System.Windows.Forms.RadioButton();
            AutoUpCheckRadio = new System.Windows.Forms.RadioButton();
            panel8 = new System.Windows.Forms.Panel();
            OtherReaderRadio = new System.Windows.Forms.RadioButton();
            ComicReaderRadio = new System.Windows.Forms.RadioButton();
            MangaReaderRadio = new System.Windows.Forms.RadioButton();
            LegacyReaderRadio = new System.Windows.Forms.RadioButton();
            lblReader = new System.Windows.Forms.Label();
            panel7 = new System.Windows.Forms.Panel();
            NewFolderRadio = new System.Windows.Forms.RadioButton();
            AskRadio = new System.Windows.Forms.RadioButton();
            UpdateUrlRadio = new System.Windows.Forms.RadioButton();
            lblReplaceMode = new System.Windows.Forms.Label();
            panel6 = new System.Windows.Forms.Panel();
            SkipDownEnbRadio = new System.Windows.Forms.RadioButton();
            SkipDownDisRadio = new System.Windows.Forms.RadioButton();
            lblSkipDownloaded = new System.Windows.Forms.Label();
            panel5 = new System.Windows.Forms.Panel();
            SaveAsAutoRadio = new System.Windows.Forms.RadioButton();
            SaveAsRawRadio = new System.Windows.Forms.RadioButton();
            SaveAsBmpRadio = new System.Windows.Forms.RadioButton();
            lblSaveAs = new System.Windows.Forms.Label();
            SaveAsJpgRadio = new System.Windows.Forms.RadioButton();
            SaveAsPngRadio = new System.Windows.Forms.RadioButton();
            panel4 = new System.Windows.Forms.Panel();
            lblClipWatcher = new System.Windows.Forms.Label();
            ClipWatcherEnbRadio = new System.Windows.Forms.RadioButton();
            ClipWatcherDisRadio = new System.Windows.Forms.RadioButton();
            panel3 = new System.Windows.Forms.Panel();
            lblReadeGenerator = new System.Windows.Forms.Label();
            ReaderGenEnbRadio = new System.Windows.Forms.RadioButton();
            ReaderGenDisRadio = new System.Windows.Forms.RadioButton();
            panel2 = new System.Windows.Forms.Panel();
            lblImageClipping = new System.Windows.Forms.Label();
            ImgClipEnbRadio = new System.Windows.Forms.RadioButton();
            ImgClipDisRadio = new System.Windows.Forms.RadioButton();
            panel1 = new System.Windows.Forms.Panel();
            lblCaptchaSolving = new System.Windows.Forms.Label();
            SemiAutoCaptchaRadio = new System.Windows.Forms.RadioButton();
            ManualCaptchaRadio = new System.Windows.Forms.RadioButton();
            EnvironmentGroupBox = new VSGroupBox();
            lblLanguage = new System.Windows.Forms.Label();
            LanguageBox = new VSComboBox();
            bntLibSelect = new VSButton();
            LibraryPathTBox = new VSNormalTextBox();
            lblLibrary = new System.Windows.Forms.Label();
            AboutTab = new System.Windows.Forms.TabPage();
            SupportedHostsBox = new VSGroupBox();
            SupportedHostListBox = new VSListBoxWBuiltInScrollBar();
            lblCredits = new System.Windows.Forms.Label();
            lblTitle = new System.Windows.Forms.Label();
            DebugTab = new System.Windows.Forms.TabPage();
            dbgTranslate = new VSButton();
            dbgBrowser = new VSButton();
            dbgButtonC = new VSButton();
            dbgButtonB = new VSButton();
            DbgButtonA = new VSButton();
            DbgPreview = new System.Windows.Forms.PictureBox();
            ThemeContainer.SuspendLayout();
            MainTabMenu.SuspendLayout();
            DownloaderTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)CoverBox).BeginInit();
            LibraryTab.SuspendLayout();
            CrawlerTab.SuspendLayout();
            SettingsTab.SuspendLayout();
            FeaturesGroupBox.SuspendLayout();
            panel10.SuspendLayout();
            panel9.SuspendLayout();
            panel8.SuspendLayout();
            panel7.SuspendLayout();
            panel6.SuspendLayout();
            panel5.SuspendLayout();
            panel4.SuspendLayout();
            panel3.SuspendLayout();
            panel2.SuspendLayout();
            panel1.SuspendLayout();
            EnvironmentGroupBox.SuspendLayout();
            AboutTab.SuspendLayout();
            SupportedHostsBox.SuspendLayout();
            DebugTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)DbgPreview).BeginInit();
            SuspendLayout();
            // 
            // MainTimer
            // 
            MainTimer.Interval = 300;
            MainTimer.Tick += MainTimerTick;
            // 
            // ThemeContainer
            // 
            ThemeContainer.AllowClose = true;
            ThemeContainer.AllowMaximize = true;
            ThemeContainer.AllowMinimize = true;
            ThemeContainer.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            ThemeContainer.BaseColour = System.Drawing.Color.FromArgb(45, 45, 48);
            ThemeContainer.BorderColour = System.Drawing.Color.FromArgb(15, 15, 18);
            ThemeContainer.Controls.Add(StatusBar);
            ThemeContainer.Controls.Add(MainTabMenu);
            ThemeContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            ThemeContainer.FontColour = System.Drawing.Color.FromArgb(153, 153, 153);
            ThemeContainer.FontSize = 12;
            ThemeContainer.Form = this;
            ThemeContainer.FormOrWhole = VSContainer.__FormOrWhole.Form;
            ThemeContainer.HoverColour = System.Drawing.Color.FromArgb(63, 63, 65);
            ThemeContainer.IconStyle = VSContainer.__IconStyle.FormIcon;
            ThemeContainer.Location = new System.Drawing.Point(0, 0);
            ThemeContainer.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            ThemeContainer.Name = "ThemeContainer";
            ThemeContainer.NoTitleWrap = false;
            ThemeContainer.ShowDots = false;
            ThemeContainer.ShowIcon = true;
            ThemeContainer.Size = new System.Drawing.Size(1397, 1019);
            ThemeContainer.TabIndex = 0;
            ThemeContainer.Text = "MangaUnhost";
            // 
            // StatusBar
            // 
            StatusBar.AmountOfString = VSStatusBar.AmountOfStrings.Two;
            StatusBar.BaseColour = System.Drawing.Color.FromArgb(0, 122, 204);
            StatusBar.BorderColour = System.Drawing.Color.FromArgb(27, 27, 29);
            StatusBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            StatusBar.FirstLabelAlignment = VSStatusBar.Alignments.Left;
            StatusBar.FirstLabelText = "IDLE";
            StatusBar.Font = new System.Drawing.Font("Segoe UI", 9F);
            StatusBar.imagetoShow = null;
            StatusBar.LinesToShow = VSStatusBar.LinesCount.One;
            StatusBar.Location = new System.Drawing.Point(0, 975);
            StatusBar.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            StatusBar.Name = "StatusBar";
            StatusBar.RectangleColor = System.Drawing.Color.Red;
            StatusBar.SecondLabelAlignment = VSStatusBar.Alignments.Right;
            StatusBar.SecondLabelText = "";
            StatusBar.SeperatorColour = System.Drawing.Color.Transparent;
            StatusBar.ShowBorder = true;
            StatusBar.showImage = true;
            StatusBar.ShowLine = true;
            StatusBar.Size = new System.Drawing.Size(1397, 44);
            StatusBar.TabIndex = 1;
            StatusBar.Text = "Status Bar";
            StatusBar.TextColour = System.Drawing.Color.FromArgb(255, 255, 255);
            StatusBar.ThirdLabelAlignment = VSStatusBar.Alignments.Right;
            StatusBar.ThirdLabelText = "Label3";
            StatusBar.WaitingBaseColour = System.Drawing.Color.FromArgb(104, 33, 122);
            // 
            // MainTabMenu
            // 
            MainTabMenu.ActiveColour = System.Drawing.Color.FromArgb(0, 122, 204);
            MainTabMenu.AllowDrop = true;
            MainTabMenu.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            MainTabMenu.BackTabColour = System.Drawing.Color.FromArgb(28, 28, 28);
            MainTabMenu.BaseColour = System.Drawing.Color.FromArgb(45, 45, 48);
            MainTabMenu.BorderColour = System.Drawing.Color.FromArgb(30, 30, 30);
            MainTabMenu.Controls.Add(DownloaderTab);
            MainTabMenu.Controls.Add(LibraryTab);
            MainTabMenu.Controls.Add(CrawlerTab);
            MainTabMenu.Controls.Add(SettingsTab);
            MainTabMenu.Controls.Add(AboutTab);
            MainTabMenu.Controls.Add(DebugTab);
            MainTabMenu.HorizontalLineColour = System.Drawing.Color.FromArgb(0, 122, 204);
            MainTabMenu.ItemSize = new System.Drawing.Size(240, 16);
            MainTabMenu.Location = new System.Drawing.Point(2, 75);
            MainTabMenu.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            MainTabMenu.Name = "MainTabMenu";
            MainTabMenu.SelectedIndex = 0;
            MainTabMenu.Size = new System.Drawing.Size(1397, 898);
            MainTabMenu.TabIndex = 0;
            MainTabMenu.TextColour = System.Drawing.Color.FromArgb(255, 255, 255);
            MainTabMenu.SelectedIndexChanged += OnTabChanged;
            // 
            // DownloaderTab
            // 
            DownloaderTab.BackColor = System.Drawing.Color.FromArgb(28, 28, 28);
            DownloaderTab.Controls.Add(ContainerScrollBar);
            DownloaderTab.Controls.Add(TitleLabel);
            DownloaderTab.Controls.Add(ButtonsContainer);
            DownloaderTab.Controls.Add(CoverBox);
            DownloaderTab.Location = new System.Drawing.Point(4, 20);
            DownloaderTab.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            DownloaderTab.Name = "DownloaderTab";
            DownloaderTab.Padding = new System.Windows.Forms.Padding(5, 6, 5, 6);
            DownloaderTab.Size = new System.Drawing.Size(1389, 874);
            DownloaderTab.TabIndex = 0;
            DownloaderTab.Text = "Downloader";
            // 
            // ContainerScrollBar
            // 
            ContainerScrollBar.AmountOfInnerLines = VSVerticalScrollBar.__InnerLineCount.None;
            ContainerScrollBar.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            ContainerScrollBar.ArrowHoveerColour = System.Drawing.Color.FromArgb(39, 123, 181);
            ContainerScrollBar.ArrowNormalColour = System.Drawing.Color.FromArgb(153, 153, 153);
            ContainerScrollBar.ArrowPressedColour = System.Drawing.Color.FromArgb(0, 113, 171);
            ContainerScrollBar.BaseColour = System.Drawing.Color.FromArgb(62, 62, 66);
            ContainerScrollBar.ButtonSize = 16;
            ContainerScrollBar.LargeChange = 10;
            ContainerScrollBar.Location = new System.Drawing.Point(1342, 115);
            ContainerScrollBar.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            ContainerScrollBar.Maximum = 100;
            ContainerScrollBar.Minimum = 0;
            ContainerScrollBar.Name = "ContainerScrollBar";
            ContainerScrollBar.OuterBorderColour = System.Drawing.Color.Empty;
            ContainerScrollBar.ShowOuterBorder = false;
            ContainerScrollBar.ShowThumbBorder = false;
            ContainerScrollBar.Size = new System.Drawing.Size(32, 738);
            ContainerScrollBar.SmallChange = 1;
            ContainerScrollBar.TabIndex = 3;
            ContainerScrollBar.Text = "vsVerticalScrollBar1";
            ContainerScrollBar.ThumbBorderColour = System.Drawing.Color.Empty;
            ContainerScrollBar.ThumbHoverColour = System.Drawing.Color.FromArgb(158, 158, 158);
            ContainerScrollBar.ThumbNormalColour = System.Drawing.Color.FromArgb(104, 104, 104);
            ContainerScrollBar.ThumbPressedColour = System.Drawing.Color.FromArgb(239, 235, 239);
            ContainerScrollBar.Value = 0;
            ContainerScrollBar.Visible = false;
            // 
            // TitleLabel
            // 
            TitleLabel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            TitleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 128);
            TitleLabel.ForeColor = System.Drawing.Color.White;
            TitleLabel.Location = new System.Drawing.Point(453, 2);
            TitleLabel.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            TitleLabel.Name = "TitleLabel";
            TitleLabel.Size = new System.Drawing.Size(922, 106);
            TitleLabel.TabIndex = 2;
            TitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ButtonsContainer
            // 
            ButtonsContainer.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ButtonsContainer.AutoScroll = true;
            ButtonsContainer.Location = new System.Drawing.Point(453, 115);
            ButtonsContainer.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            ButtonsContainer.Name = "ButtonsContainer";
            ButtonsContainer.Size = new System.Drawing.Size(920, 737);
            ButtonsContainer.TabIndex = 1;
            // 
            // CoverBox
            // 
            CoverBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            CoverBox.Location = new System.Drawing.Point(0, 0);
            CoverBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            CoverBox.Name = "CoverBox";
            CoverBox.Size = new System.Drawing.Size(443, 852);
            CoverBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            CoverBox.TabIndex = 0;
            CoverBox.TabStop = false;
            CoverBox.Click += MainCoverClicked;
            // 
            // LibraryTab
            // 
            LibraryTab.BackColor = System.Drawing.Color.FromArgb(28, 28, 28);
            LibraryTab.Controls.Add(LibraryContainer);
            LibraryTab.Location = new System.Drawing.Point(4, 20);
            LibraryTab.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            LibraryTab.Name = "LibraryTab";
            LibraryTab.Padding = new System.Windows.Forms.Padding(5, 6, 5, 6);
            LibraryTab.Size = new System.Drawing.Size(1389, 874);
            LibraryTab.TabIndex = 4;
            LibraryTab.Text = "Library";
            // 
            // LibraryContainer
            // 
            LibraryContainer.AutoScroll = true;
            LibraryContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            LibraryContainer.Location = new System.Drawing.Point(5, 6);
            LibraryContainer.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            LibraryContainer.Name = "LibraryContainer";
            LibraryContainer.Size = new System.Drawing.Size(1379, 862);
            LibraryContainer.TabIndex = 0;
            // 
            // CrawlerTab
            // 
            CrawlerTab.BackColor = System.Drawing.Color.FromArgb(28, 28, 28);
            CrawlerTab.Controls.Add(LinksListBox);
            CrawlerTab.Controls.Add(CrawlerCopyBtn);
            CrawlerTab.Controls.Add(lblRegex);
            CrawlerTab.Controls.Add(tbCrawlerRegex);
            CrawlerTab.Controls.Add(CrawlerStartBtn);
            CrawlerTab.Controls.Add(lblUrl);
            CrawlerTab.Controls.Add(tbCrawlerUrl);
            CrawlerTab.Location = new System.Drawing.Point(4, 20);
            CrawlerTab.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            CrawlerTab.Name = "CrawlerTab";
            CrawlerTab.Size = new System.Drawing.Size(1389, 874);
            CrawlerTab.TabIndex = 5;
            CrawlerTab.Text = "Crawler";
            // 
            // LinksListBox
            // 
            LinksListBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            LinksListBox.BaseColour = System.Drawing.Color.FromArgb(37, 37, 38);
            LinksListBox.BorderColour = System.Drawing.Color.FromArgb(35, 35, 35);
            LinksListBox.DontShowInnerScrollbarBorder = false;
            LinksListBox.FontColour = System.Drawing.Color.FromArgb(199, 199, 199);
            LinksListBox.Location = new System.Drawing.Point(17, 135);
            LinksListBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            LinksListBox.MultiSelect = true;
            LinksListBox.Name = "LinksListBox";
            LinksListBox.NonSelectedItemColour = System.Drawing.Color.FromArgb(62, 62, 64);
            LinksListBox.SelectedItemColour = System.Drawing.Color.FromArgb(47, 47, 47);
            LinksListBox.ShowWholeInnerBorder = true;
            LinksListBox.Size = new System.Drawing.Size(1362, 712);
            LinksListBox.TabIndex = 6;
            // 
            // CrawlerCopyBtn
            // 
            CrawlerCopyBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            CrawlerCopyBtn.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            CrawlerCopyBtn.BaseColour = System.Drawing.Color.FromArgb(45, 45, 48);
            CrawlerCopyBtn.BorderColour = System.Drawing.Color.FromArgb(15, 15, 18);
            CrawlerCopyBtn.FontColour = System.Drawing.Color.FromArgb(153, 153, 153);
            CrawlerCopyBtn.HoverColour = System.Drawing.Color.FromArgb(60, 60, 62);
            CrawlerCopyBtn.ImageAlignment = VSButton.__ImageAlignment.Left;
            CrawlerCopyBtn.ImageChoice = null;
            CrawlerCopyBtn.Location = new System.Drawing.Point(1243, 75);
            CrawlerCopyBtn.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            CrawlerCopyBtn.Name = "CrawlerCopyBtn";
            CrawlerCopyBtn.ShowBorder = true;
            CrawlerCopyBtn.ShowImage = false;
            CrawlerCopyBtn.ShowText = true;
            CrawlerCopyBtn.Size = new System.Drawing.Size(125, 44);
            CrawlerCopyBtn.TabIndex = 5;
            CrawlerCopyBtn.Text = "Copy";
            CrawlerCopyBtn.TextAlignment = System.Drawing.StringAlignment.Center;
            CrawlerCopyBtn.Click += CrawlerCopyBtn_Click;
            // 
            // lblRegex
            // 
            lblRegex.ForeColor = System.Drawing.Color.White;
            lblRegex.Location = new System.Drawing.Point(12, 85);
            lblRegex.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lblRegex.Name = "lblRegex";
            lblRegex.Size = new System.Drawing.Size(68, 25);
            lblRegex.TabIndex = 4;
            lblRegex.Text = "Regex:";
            lblRegex.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tbCrawlerRegex
            // 
            tbCrawlerRegex.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tbCrawlerRegex.BackColor = System.Drawing.Color.Transparent;
            tbCrawlerRegex.BackgroundColour = System.Drawing.Color.FromArgb(51, 51, 55);
            tbCrawlerRegex.BorderColour = System.Drawing.Color.FromArgb(35, 35, 35);
            tbCrawlerRegex.Location = new System.Drawing.Point(90, 75);
            tbCrawlerRegex.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            tbCrawlerRegex.MaxLength = 32767;
            tbCrawlerRegex.Multiline = false;
            tbCrawlerRegex.Name = "tbCrawlerRegex";
            tbCrawlerRegex.ReadOnly = false;
            tbCrawlerRegex.Size = new System.Drawing.Size(1143, 34);
            tbCrawlerRegex.Style = VSNormalTextBox.Styles.NotRounded;
            tbCrawlerRegex.TabIndex = 3;
            tbCrawlerRegex.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            tbCrawlerRegex.TextColour = System.Drawing.Color.FromArgb(153, 153, 153);
            tbCrawlerRegex.UseSystemPasswordChar = false;
            // 
            // CrawlerStartBtn
            // 
            CrawlerStartBtn.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            CrawlerStartBtn.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            CrawlerStartBtn.BaseColour = System.Drawing.Color.FromArgb(45, 45, 48);
            CrawlerStartBtn.BorderColour = System.Drawing.Color.FromArgb(15, 15, 18);
            CrawlerStartBtn.FontColour = System.Drawing.Color.FromArgb(153, 153, 153);
            CrawlerStartBtn.HoverColour = System.Drawing.Color.FromArgb(60, 60, 62);
            CrawlerStartBtn.ImageAlignment = VSButton.__ImageAlignment.Left;
            CrawlerStartBtn.ImageChoice = null;
            CrawlerStartBtn.Location = new System.Drawing.Point(1243, 15);
            CrawlerStartBtn.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            CrawlerStartBtn.Name = "CrawlerStartBtn";
            CrawlerStartBtn.ShowBorder = true;
            CrawlerStartBtn.ShowImage = false;
            CrawlerStartBtn.ShowText = true;
            CrawlerStartBtn.Size = new System.Drawing.Size(125, 44);
            CrawlerStartBtn.TabIndex = 2;
            CrawlerStartBtn.Text = "Start";
            CrawlerStartBtn.TextAlignment = System.Drawing.StringAlignment.Center;
            CrawlerStartBtn.Click += CrawlerStartBtn_Click;
            // 
            // lblUrl
            // 
            lblUrl.ForeColor = System.Drawing.Color.White;
            lblUrl.Location = new System.Drawing.Point(12, 15);
            lblUrl.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lblUrl.Name = "lblUrl";
            lblUrl.Size = new System.Drawing.Size(68, 44);
            lblUrl.TabIndex = 1;
            lblUrl.Text = "Url:";
            lblUrl.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tbCrawlerUrl
            // 
            tbCrawlerUrl.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tbCrawlerUrl.BackColor = System.Drawing.Color.Transparent;
            tbCrawlerUrl.BackgroundColour = System.Drawing.Color.FromArgb(51, 51, 55);
            tbCrawlerUrl.BorderColour = System.Drawing.Color.FromArgb(35, 35, 35);
            tbCrawlerUrl.Location = new System.Drawing.Point(90, 15);
            tbCrawlerUrl.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            tbCrawlerUrl.MaxLength = 32767;
            tbCrawlerUrl.Multiline = false;
            tbCrawlerUrl.Name = "tbCrawlerUrl";
            tbCrawlerUrl.ReadOnly = false;
            tbCrawlerUrl.Size = new System.Drawing.Size(1143, 34);
            tbCrawlerUrl.Style = VSNormalTextBox.Styles.NotRounded;
            tbCrawlerUrl.TabIndex = 0;
            tbCrawlerUrl.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            tbCrawlerUrl.TextColour = System.Drawing.Color.FromArgb(153, 153, 153);
            tbCrawlerUrl.UseSystemPasswordChar = false;
            // 
            // SettingsTab
            // 
            SettingsTab.BackColor = System.Drawing.Color.FromArgb(28, 28, 28);
            SettingsTab.Controls.Add(FeaturesGroupBox);
            SettingsTab.Controls.Add(EnvironmentGroupBox);
            SettingsTab.Location = new System.Drawing.Point(4, 20);
            SettingsTab.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            SettingsTab.Name = "SettingsTab";
            SettingsTab.Padding = new System.Windows.Forms.Padding(5, 6, 5, 6);
            SettingsTab.Size = new System.Drawing.Size(1389, 874);
            SettingsTab.TabIndex = 1;
            SettingsTab.Text = "Settings";
            // 
            // FeaturesGroupBox
            // 
            FeaturesGroupBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            FeaturesGroupBox.BorderColour = System.Drawing.Color.FromArgb(2, 118, 196);
            FeaturesGroupBox.Controls.Add(btnSetupComparsion);
            FeaturesGroupBox.Controls.Add(panel10);
            FeaturesGroupBox.Controls.Add(panel9);
            FeaturesGroupBox.Controls.Add(panel8);
            FeaturesGroupBox.Controls.Add(panel7);
            FeaturesGroupBox.Controls.Add(panel6);
            FeaturesGroupBox.Controls.Add(panel5);
            FeaturesGroupBox.Controls.Add(panel4);
            FeaturesGroupBox.Controls.Add(panel3);
            FeaturesGroupBox.Controls.Add(panel2);
            FeaturesGroupBox.Controls.Add(panel1);
            FeaturesGroupBox.Font = new System.Drawing.Font("Segoe UI", 10F);
            FeaturesGroupBox.HeaderColour = System.Drawing.Color.FromArgb(45, 45, 48);
            FeaturesGroupBox.Location = new System.Drawing.Point(10, 244);
            FeaturesGroupBox.MainColour = System.Drawing.Color.FromArgb(37, 37, 38);
            FeaturesGroupBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            FeaturesGroupBox.Name = "FeaturesGroupBox";
            FeaturesGroupBox.Size = new System.Drawing.Size(1373, 596);
            FeaturesGroupBox.TabIndex = 1;
            FeaturesGroupBox.Text = "Features";
            FeaturesGroupBox.TextColour = System.Drawing.Color.FromArgb(129, 129, 131);
            // 
            // btnSetupComparsion
            // 
            btnSetupComparsion.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnSetupComparsion.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            btnSetupComparsion.BaseColour = System.Drawing.Color.FromArgb(45, 45, 48);
            btnSetupComparsion.BorderColour = System.Drawing.Color.FromArgb(15, 15, 18);
            btnSetupComparsion.FontColour = System.Drawing.Color.FromArgb(153, 153, 153);
            btnSetupComparsion.HoverColour = System.Drawing.Color.FromArgb(60, 60, 62);
            btnSetupComparsion.ImageAlignment = VSButton.__ImageAlignment.Left;
            btnSetupComparsion.ImageChoice = null;
            btnSetupComparsion.Location = new System.Drawing.Point(977, 521);
            btnSetupComparsion.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btnSetupComparsion.Name = "btnSetupComparsion";
            btnSetupComparsion.ShowBorder = true;
            btnSetupComparsion.ShowImage = false;
            btnSetupComparsion.ShowText = true;
            btnSetupComparsion.Size = new System.Drawing.Size(378, 44);
            btnSetupComparsion.TabIndex = 15;
            btnSetupComparsion.Text = "Text Detection Tuning";
            btnSetupComparsion.TextAlignment = System.Drawing.StringAlignment.Center;
            btnSetupComparsion.Click += btnSetupComparsion_Click;
            // 
            // panel10
            // 
            panel10.BackColor = System.Drawing.Color.Transparent;
            panel10.Controls.Add(APNGBypassDisRadio);
            panel10.Controls.Add(APNGBypassEnaRadio);
            panel10.Controls.Add(label1);
            panel10.Location = new System.Drawing.Point(978, 194);
            panel10.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            panel10.Name = "panel10";
            panel10.Size = new System.Drawing.Size(377, 115);
            panel10.TabIndex = 14;
            // 
            // APNGBypassDisRadio
            // 
            APNGBypassDisRadio.AutoSize = true;
            APNGBypassDisRadio.BackColor = System.Drawing.Color.Transparent;
            APNGBypassDisRadio.Checked = true;
            APNGBypassDisRadio.ForeColor = System.Drawing.Color.White;
            APNGBypassDisRadio.Location = new System.Drawing.Point(193, 6);
            APNGBypassDisRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            APNGBypassDisRadio.Name = "APNGBypassDisRadio";
            APNGBypassDisRadio.Size = new System.Drawing.Size(113, 32);
            APNGBypassDisRadio.TabIndex = 8;
            APNGBypassDisRadio.TabStop = true;
            APNGBypassDisRadio.Text = "Disabled";
            APNGBypassDisRadio.UseVisualStyleBackColor = false;
            // 
            // APNGBypassEnaRadio
            // 
            APNGBypassEnaRadio.AutoSize = true;
            APNGBypassEnaRadio.BackColor = System.Drawing.Color.Transparent;
            APNGBypassEnaRadio.ForeColor = System.Drawing.Color.White;
            APNGBypassEnaRadio.Location = new System.Drawing.Point(193, 54);
            APNGBypassEnaRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            APNGBypassEnaRadio.Name = "APNGBypassEnaRadio";
            APNGBypassEnaRadio.Size = new System.Drawing.Size(107, 32);
            APNGBypassEnaRadio.TabIndex = 7;
            APNGBypassEnaRadio.Text = "Enabled";
            APNGBypassEnaRadio.UseVisualStyleBackColor = false;
            APNGBypassEnaRadio.CheckedChanged += APNGBypassCheckChanged;
            // 
            // label1
            // 
            label1.BackColor = System.Drawing.Color.Transparent;
            label1.ForeColor = System.Drawing.Color.White;
            label1.Location = new System.Drawing.Point(5, 2);
            label1.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(178, 40);
            label1.TabIndex = 6;
            label1.Text = "APNG Bypass:";
            label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // panel9
            // 
            panel9.BackColor = System.Drawing.Color.Transparent;
            panel9.Controls.Add(lblLibUpdates);
            panel9.Controls.Add(ManualUpCheckRadio);
            panel9.Controls.Add(AutoUpCheckRadio);
            panel9.Location = new System.Drawing.Point(977, 67);
            panel9.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            panel9.Name = "panel9";
            panel9.Size = new System.Drawing.Size(387, 115);
            panel9.TabIndex = 10;
            // 
            // lblLibUpdates
            // 
            lblLibUpdates.BackColor = System.Drawing.Color.Transparent;
            lblLibUpdates.ForeColor = System.Drawing.Color.White;
            lblLibUpdates.Location = new System.Drawing.Point(5, 0);
            lblLibUpdates.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lblLibUpdates.Name = "lblLibUpdates";
            lblLibUpdates.Size = new System.Drawing.Size(232, 40);
            lblLibUpdates.TabIndex = 5;
            lblLibUpdates.Text = "Library Updates:";
            lblLibUpdates.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // ManualUpCheckRadio
            // 
            ManualUpCheckRadio.AutoSize = true;
            ManualUpCheckRadio.BackColor = System.Drawing.Color.Transparent;
            ManualUpCheckRadio.ForeColor = System.Drawing.Color.White;
            ManualUpCheckRadio.Location = new System.Drawing.Point(247, 54);
            ManualUpCheckRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            ManualUpCheckRadio.Name = "ManualUpCheckRadio";
            ManualUpCheckRadio.Size = new System.Drawing.Size(102, 32);
            ManualUpCheckRadio.TabIndex = 1;
            ManualUpCheckRadio.Text = "Manual";
            ManualUpCheckRadio.UseVisualStyleBackColor = false;
            // 
            // AutoUpCheckRadio
            // 
            AutoUpCheckRadio.AutoSize = true;
            AutoUpCheckRadio.BackColor = System.Drawing.Color.Transparent;
            AutoUpCheckRadio.Checked = true;
            AutoUpCheckRadio.ForeColor = System.Drawing.Color.White;
            AutoUpCheckRadio.Location = new System.Drawing.Point(247, -2);
            AutoUpCheckRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            AutoUpCheckRadio.Name = "AutoUpCheckRadio";
            AutoUpCheckRadio.Size = new System.Drawing.Size(80, 32);
            AutoUpCheckRadio.TabIndex = 0;
            AutoUpCheckRadio.TabStop = true;
            AutoUpCheckRadio.Text = "Auto";
            AutoUpCheckRadio.UseVisualStyleBackColor = false;
            AutoUpCheckRadio.CheckedChanged += LibUpCheckChanged;
            // 
            // panel8
            // 
            panel8.BackColor = System.Drawing.Color.Transparent;
            panel8.Controls.Add(OtherReaderRadio);
            panel8.Controls.Add(ComicReaderRadio);
            panel8.Controls.Add(MangaReaderRadio);
            panel8.Controls.Add(LegacyReaderRadio);
            panel8.Controls.Add(lblReader);
            panel8.Location = new System.Drawing.Point(497, 450);
            panel8.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            panel8.Name = "panel8";
            panel8.Size = new System.Drawing.Size(470, 115);
            panel8.TabIndex = 13;
            // 
            // OtherReaderRadio
            // 
            OtherReaderRadio.AutoSize = true;
            OtherReaderRadio.BackColor = System.Drawing.Color.Transparent;
            OtherReaderRadio.ForeColor = System.Drawing.Color.White;
            OtherReaderRadio.Location = new System.Drawing.Point(275, 46);
            OtherReaderRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            OtherReaderRadio.Name = "OtherReaderRadio";
            OtherReaderRadio.Size = new System.Drawing.Size(87, 32);
            OtherReaderRadio.TabIndex = 12;
            OtherReaderRadio.Text = "Other";
            OtherReaderRadio.UseVisualStyleBackColor = false;
            OtherReaderRadio.CheckedChanged += OtherReaderChanged;
            // 
            // ComicReaderRadio
            // 
            ComicReaderRadio.AutoSize = true;
            ComicReaderRadio.BackColor = System.Drawing.Color.Transparent;
            ComicReaderRadio.ForeColor = System.Drawing.Color.White;
            ComicReaderRadio.Location = new System.Drawing.Point(157, 46);
            ComicReaderRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            ComicReaderRadio.Name = "ComicReaderRadio";
            ComicReaderRadio.Size = new System.Drawing.Size(92, 32);
            ComicReaderRadio.TabIndex = 11;
            ComicReaderRadio.Text = "Comic";
            ComicReaderRadio.UseVisualStyleBackColor = false;
            ComicReaderRadio.CheckedChanged += ComicReaderChanged;
            // 
            // MangaReaderRadio
            // 
            MangaReaderRadio.AutoSize = true;
            MangaReaderRadio.BackColor = System.Drawing.Color.Transparent;
            MangaReaderRadio.ForeColor = System.Drawing.Color.White;
            MangaReaderRadio.Location = new System.Drawing.Point(30, 46);
            MangaReaderRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            MangaReaderRadio.Name = "MangaReaderRadio";
            MangaReaderRadio.Size = new System.Drawing.Size(98, 32);
            MangaReaderRadio.TabIndex = 10;
            MangaReaderRadio.Text = "Manga";
            MangaReaderRadio.UseVisualStyleBackColor = false;
            MangaReaderRadio.CheckedChanged += MangaReaderChanged;
            // 
            // LegacyReaderRadio
            // 
            LegacyReaderRadio.AutoSize = true;
            LegacyReaderRadio.BackColor = System.Drawing.Color.Transparent;
            LegacyReaderRadio.Checked = true;
            LegacyReaderRadio.ForeColor = System.Drawing.Color.White;
            LegacyReaderRadio.Location = new System.Drawing.Point(275, 6);
            LegacyReaderRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            LegacyReaderRadio.Name = "LegacyReaderRadio";
            LegacyReaderRadio.Size = new System.Drawing.Size(97, 32);
            LegacyReaderRadio.TabIndex = 9;
            LegacyReaderRadio.TabStop = true;
            LegacyReaderRadio.Text = "Legacy";
            LegacyReaderRadio.UseVisualStyleBackColor = false;
            LegacyReaderRadio.CheckedChanged += LegacyReaderChanged;
            // 
            // lblReader
            // 
            lblReader.BackColor = System.Drawing.Color.Transparent;
            lblReader.ForeColor = System.Drawing.Color.White;
            lblReader.Location = new System.Drawing.Point(5, 0);
            lblReader.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lblReader.Name = "lblReader";
            lblReader.Size = new System.Drawing.Size(265, 40);
            lblReader.TabIndex = 8;
            lblReader.Text = "Reader:";
            lblReader.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // panel7
            // 
            panel7.BackColor = System.Drawing.Color.Transparent;
            panel7.Controls.Add(NewFolderRadio);
            panel7.Controls.Add(AskRadio);
            panel7.Controls.Add(UpdateUrlRadio);
            panel7.Controls.Add(lblReplaceMode);
            panel7.Location = new System.Drawing.Point(12, 450);
            panel7.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            panel7.Name = "panel7";
            panel7.Size = new System.Drawing.Size(475, 115);
            panel7.TabIndex = 12;
            // 
            // NewFolderRadio
            // 
            NewFolderRadio.AutoSize = true;
            NewFolderRadio.BackColor = System.Drawing.Color.Transparent;
            NewFolderRadio.ForeColor = System.Drawing.Color.White;
            NewFolderRadio.Location = new System.Drawing.Point(277, 46);
            NewFolderRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            NewFolderRadio.Name = "NewFolderRadio";
            NewFolderRadio.Size = new System.Drawing.Size(137, 32);
            NewFolderRadio.TabIndex = 8;
            NewFolderRadio.Text = "New Folder";
            NewFolderRadio.UseVisualStyleBackColor = false;
            NewFolderRadio.CheckedChanged += ReplaceNewFolderModeChanged;
            // 
            // AskRadio
            // 
            AskRadio.AutoSize = true;
            AskRadio.BackColor = System.Drawing.Color.Transparent;
            AskRadio.ForeColor = System.Drawing.Color.White;
            AskRadio.Location = new System.Drawing.Point(277, 6);
            AskRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            AskRadio.Name = "AskRadio";
            AskRadio.Size = new System.Drawing.Size(68, 32);
            AskRadio.TabIndex = 7;
            AskRadio.Text = "Ask";
            AskRadio.UseVisualStyleBackColor = false;
            AskRadio.CheckedChanged += ReplaceAskModeChanged;
            // 
            // UpdateUrlRadio
            // 
            UpdateUrlRadio.BackColor = System.Drawing.Color.Transparent;
            UpdateUrlRadio.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            UpdateUrlRadio.Checked = true;
            UpdateUrlRadio.ForeColor = System.Drawing.Color.White;
            UpdateUrlRadio.Location = new System.Drawing.Point(12, 46);
            UpdateUrlRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            UpdateUrlRadio.Name = "UpdateUrlRadio";
            UpdateUrlRadio.Size = new System.Drawing.Size(255, 44);
            UpdateUrlRadio.TabIndex = 6;
            UpdateUrlRadio.TabStop = true;
            UpdateUrlRadio.Text = "Update URL";
            UpdateUrlRadio.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            UpdateUrlRadio.UseVisualStyleBackColor = false;
            UpdateUrlRadio.CheckedChanged += ReplaceUpdateUrlModeChanged;
            // 
            // lblReplaceMode
            // 
            lblReplaceMode.BackColor = System.Drawing.Color.Transparent;
            lblReplaceMode.ForeColor = System.Drawing.Color.White;
            lblReplaceMode.Location = new System.Drawing.Point(5, 0);
            lblReplaceMode.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lblReplaceMode.Name = "lblReplaceMode";
            lblReplaceMode.Size = new System.Drawing.Size(262, 40);
            lblReplaceMode.TabIndex = 5;
            lblReplaceMode.Text = "Replace Mode:";
            lblReplaceMode.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // panel6
            // 
            panel6.BackColor = System.Drawing.Color.Transparent;
            panel6.Controls.Add(SkipDownEnbRadio);
            panel6.Controls.Add(SkipDownDisRadio);
            panel6.Controls.Add(lblSkipDownloaded);
            panel6.Location = new System.Drawing.Point(492, 321);
            panel6.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            panel6.Name = "panel6";
            panel6.Size = new System.Drawing.Size(475, 115);
            panel6.TabIndex = 11;
            // 
            // SkipDownEnbRadio
            // 
            SkipDownEnbRadio.AutoSize = true;
            SkipDownEnbRadio.BackColor = System.Drawing.Color.Transparent;
            SkipDownEnbRadio.ForeColor = System.Drawing.Color.White;
            SkipDownEnbRadio.Location = new System.Drawing.Point(280, 62);
            SkipDownEnbRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            SkipDownEnbRadio.Name = "SkipDownEnbRadio";
            SkipDownEnbRadio.Size = new System.Drawing.Size(107, 32);
            SkipDownEnbRadio.TabIndex = 7;
            SkipDownEnbRadio.Text = "Enabled";
            SkipDownEnbRadio.UseVisualStyleBackColor = false;
            SkipDownEnbRadio.CheckedChanged += SkipDownloadedSwitched;
            // 
            // SkipDownDisRadio
            // 
            SkipDownDisRadio.AutoSize = true;
            SkipDownDisRadio.BackColor = System.Drawing.Color.Transparent;
            SkipDownDisRadio.Checked = true;
            SkipDownDisRadio.ForeColor = System.Drawing.Color.White;
            SkipDownDisRadio.Location = new System.Drawing.Point(280, 6);
            SkipDownDisRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            SkipDownDisRadio.Name = "SkipDownDisRadio";
            SkipDownDisRadio.Size = new System.Drawing.Size(113, 32);
            SkipDownDisRadio.TabIndex = 6;
            SkipDownDisRadio.TabStop = true;
            SkipDownDisRadio.Text = "Disabled";
            SkipDownDisRadio.UseVisualStyleBackColor = false;
            // 
            // lblSkipDownloaded
            // 
            lblSkipDownloaded.BackColor = System.Drawing.Color.Transparent;
            lblSkipDownloaded.ForeColor = System.Drawing.Color.White;
            lblSkipDownloaded.Location = new System.Drawing.Point(5, 0);
            lblSkipDownloaded.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lblSkipDownloaded.Name = "lblSkipDownloaded";
            lblSkipDownloaded.Size = new System.Drawing.Size(265, 40);
            lblSkipDownloaded.TabIndex = 5;
            lblSkipDownloaded.Text = "Skip Downloaded:";
            lblSkipDownloaded.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // panel5
            // 
            panel5.BackColor = System.Drawing.Color.Transparent;
            panel5.Controls.Add(SaveAsAutoRadio);
            panel5.Controls.Add(SaveAsRawRadio);
            panel5.Controls.Add(SaveAsBmpRadio);
            panel5.Controls.Add(lblSaveAs);
            panel5.Controls.Add(SaveAsJpgRadio);
            panel5.Controls.Add(SaveAsPngRadio);
            panel5.Location = new System.Drawing.Point(492, 196);
            panel5.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            panel5.Name = "panel5";
            panel5.Size = new System.Drawing.Size(475, 115);
            panel5.TabIndex = 10;
            // 
            // SaveAsAutoRadio
            // 
            SaveAsAutoRadio.AutoSize = true;
            SaveAsAutoRadio.BackColor = System.Drawing.Color.Transparent;
            SaveAsAutoRadio.ForeColor = System.Drawing.Color.White;
            SaveAsAutoRadio.Location = new System.Drawing.Point(173, 52);
            SaveAsAutoRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            SaveAsAutoRadio.Name = "SaveAsAutoRadio";
            SaveAsAutoRadio.Size = new System.Drawing.Size(80, 32);
            SaveAsAutoRadio.TabIndex = 8;
            SaveAsAutoRadio.Text = "Auto";
            SaveAsAutoRadio.UseVisualStyleBackColor = false;
            SaveAsAutoRadio.CheckedChanged += AutoSaveAs;
            // 
            // SaveAsRawRadio
            // 
            SaveAsRawRadio.AutoSize = true;
            SaveAsRawRadio.BackColor = System.Drawing.Color.Transparent;
            SaveAsRawRadio.ForeColor = System.Drawing.Color.White;
            SaveAsRawRadio.Location = new System.Drawing.Point(378, 52);
            SaveAsRawRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            SaveAsRawRadio.Name = "SaveAsRawRadio";
            SaveAsRawRadio.Size = new System.Drawing.Size(80, 32);
            SaveAsRawRadio.TabIndex = 7;
            SaveAsRawRadio.Text = "RAW";
            SaveAsRawRadio.UseVisualStyleBackColor = false;
            SaveAsRawRadio.CheckedChanged += RAWSaveAs;
            // 
            // SaveAsBmpRadio
            // 
            SaveAsBmpRadio.AutoSize = true;
            SaveAsBmpRadio.BackColor = System.Drawing.Color.Transparent;
            SaveAsBmpRadio.ForeColor = System.Drawing.Color.White;
            SaveAsBmpRadio.Location = new System.Drawing.Point(277, 52);
            SaveAsBmpRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            SaveAsBmpRadio.Name = "SaveAsBmpRadio";
            SaveAsBmpRadio.Size = new System.Drawing.Size(77, 32);
            SaveAsBmpRadio.TabIndex = 6;
            SaveAsBmpRadio.Text = "BMP";
            SaveAsBmpRadio.UseVisualStyleBackColor = false;
            SaveAsBmpRadio.CheckedChanged += BMPSaveAs;
            // 
            // lblSaveAs
            // 
            lblSaveAs.BackColor = System.Drawing.Color.Transparent;
            lblSaveAs.ForeColor = System.Drawing.Color.White;
            lblSaveAs.Location = new System.Drawing.Point(12, 0);
            lblSaveAs.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lblSaveAs.Name = "lblSaveAs";
            lblSaveAs.Size = new System.Drawing.Size(255, 40);
            lblSaveAs.TabIndex = 5;
            lblSaveAs.Text = "Save As:";
            lblSaveAs.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // SaveAsJpgRadio
            // 
            SaveAsJpgRadio.AutoSize = true;
            SaveAsJpgRadio.BackColor = System.Drawing.Color.Transparent;
            SaveAsJpgRadio.ForeColor = System.Drawing.Color.White;
            SaveAsJpgRadio.Location = new System.Drawing.Point(378, -4);
            SaveAsJpgRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            SaveAsJpgRadio.Name = "SaveAsJpgRadio";
            SaveAsJpgRadio.Size = new System.Drawing.Size(69, 32);
            SaveAsJpgRadio.TabIndex = 1;
            SaveAsJpgRadio.Text = "JPG";
            SaveAsJpgRadio.UseVisualStyleBackColor = false;
            SaveAsJpgRadio.CheckedChanged += JPGSaveAs;
            // 
            // SaveAsPngRadio
            // 
            SaveAsPngRadio.AutoSize = true;
            SaveAsPngRadio.BackColor = System.Drawing.Color.Transparent;
            SaveAsPngRadio.Checked = true;
            SaveAsPngRadio.ForeColor = System.Drawing.Color.White;
            SaveAsPngRadio.Location = new System.Drawing.Point(277, -4);
            SaveAsPngRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            SaveAsPngRadio.Name = "SaveAsPngRadio";
            SaveAsPngRadio.Size = new System.Drawing.Size(77, 32);
            SaveAsPngRadio.TabIndex = 0;
            SaveAsPngRadio.TabStop = true;
            SaveAsPngRadio.Text = "PNG";
            SaveAsPngRadio.UseVisualStyleBackColor = false;
            SaveAsPngRadio.CheckedChanged += PNGSaveAs;
            // 
            // panel4
            // 
            panel4.BackColor = System.Drawing.Color.Transparent;
            panel4.Controls.Add(lblClipWatcher);
            panel4.Controls.Add(ClipWatcherEnbRadio);
            panel4.Controls.Add(ClipWatcherDisRadio);
            panel4.Location = new System.Drawing.Point(492, 69);
            panel4.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            panel4.Name = "panel4";
            panel4.Size = new System.Drawing.Size(475, 115);
            panel4.TabIndex = 9;
            // 
            // lblClipWatcher
            // 
            lblClipWatcher.BackColor = System.Drawing.Color.Transparent;
            lblClipWatcher.ForeColor = System.Drawing.Color.White;
            lblClipWatcher.Location = new System.Drawing.Point(5, 0);
            lblClipWatcher.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lblClipWatcher.Name = "lblClipWatcher";
            lblClipWatcher.Size = new System.Drawing.Size(265, 40);
            lblClipWatcher.TabIndex = 5;
            lblClipWatcher.Text = "Clipboard Watcher:";
            lblClipWatcher.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // ClipWatcherEnbRadio
            // 
            ClipWatcherEnbRadio.AutoSize = true;
            ClipWatcherEnbRadio.BackColor = System.Drawing.Color.Transparent;
            ClipWatcherEnbRadio.ForeColor = System.Drawing.Color.White;
            ClipWatcherEnbRadio.Location = new System.Drawing.Point(280, 56);
            ClipWatcherEnbRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            ClipWatcherEnbRadio.Name = "ClipWatcherEnbRadio";
            ClipWatcherEnbRadio.Size = new System.Drawing.Size(107, 32);
            ClipWatcherEnbRadio.TabIndex = 1;
            ClipWatcherEnbRadio.Text = "Enabled";
            ClipWatcherEnbRadio.UseVisualStyleBackColor = false;
            ClipWatcherEnbRadio.CheckedChanged += ClipWatcherSwitched;
            // 
            // ClipWatcherDisRadio
            // 
            ClipWatcherDisRadio.AutoSize = true;
            ClipWatcherDisRadio.BackColor = System.Drawing.Color.Transparent;
            ClipWatcherDisRadio.Checked = true;
            ClipWatcherDisRadio.ForeColor = System.Drawing.Color.White;
            ClipWatcherDisRadio.Location = new System.Drawing.Point(280, 0);
            ClipWatcherDisRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            ClipWatcherDisRadio.Name = "ClipWatcherDisRadio";
            ClipWatcherDisRadio.Size = new System.Drawing.Size(113, 32);
            ClipWatcherDisRadio.TabIndex = 0;
            ClipWatcherDisRadio.TabStop = true;
            ClipWatcherDisRadio.Text = "Disabled";
            ClipWatcherDisRadio.UseVisualStyleBackColor = false;
            // 
            // panel3
            // 
            panel3.BackColor = System.Drawing.Color.Transparent;
            panel3.Controls.Add(lblReadeGenerator);
            panel3.Controls.Add(ReaderGenEnbRadio);
            panel3.Controls.Add(ReaderGenDisRadio);
            panel3.Location = new System.Drawing.Point(12, 323);
            panel3.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            panel3.Name = "panel3";
            panel3.Size = new System.Drawing.Size(470, 115);
            panel3.TabIndex = 8;
            // 
            // lblReadeGenerator
            // 
            lblReadeGenerator.BackColor = System.Drawing.Color.Transparent;
            lblReadeGenerator.ForeColor = System.Drawing.Color.White;
            lblReadeGenerator.Location = new System.Drawing.Point(5, 0);
            lblReadeGenerator.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lblReadeGenerator.Name = "lblReadeGenerator";
            lblReadeGenerator.Size = new System.Drawing.Size(300, 40);
            lblReadeGenerator.TabIndex = 5;
            lblReadeGenerator.Text = "Reader Generator:";
            lblReadeGenerator.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // ReaderGenEnbRadio
            // 
            ReaderGenEnbRadio.AutoSize = true;
            ReaderGenEnbRadio.BackColor = System.Drawing.Color.Transparent;
            ReaderGenEnbRadio.ForeColor = System.Drawing.Color.White;
            ReaderGenEnbRadio.Location = new System.Drawing.Point(315, 56);
            ReaderGenEnbRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            ReaderGenEnbRadio.Name = "ReaderGenEnbRadio";
            ReaderGenEnbRadio.Size = new System.Drawing.Size(107, 32);
            ReaderGenEnbRadio.TabIndex = 1;
            ReaderGenEnbRadio.Text = "Enabled";
            ReaderGenEnbRadio.UseVisualStyleBackColor = false;
            ReaderGenEnbRadio.CheckedChanged += ReaderGeneratorSwitched;
            // 
            // ReaderGenDisRadio
            // 
            ReaderGenDisRadio.AutoSize = true;
            ReaderGenDisRadio.BackColor = System.Drawing.Color.Transparent;
            ReaderGenDisRadio.Checked = true;
            ReaderGenDisRadio.ForeColor = System.Drawing.Color.White;
            ReaderGenDisRadio.Location = new System.Drawing.Point(315, 0);
            ReaderGenDisRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            ReaderGenDisRadio.Name = "ReaderGenDisRadio";
            ReaderGenDisRadio.Size = new System.Drawing.Size(113, 32);
            ReaderGenDisRadio.TabIndex = 0;
            ReaderGenDisRadio.TabStop = true;
            ReaderGenDisRadio.Text = "Disabled";
            ReaderGenDisRadio.UseVisualStyleBackColor = false;
            // 
            // panel2
            // 
            panel2.BackColor = System.Drawing.Color.Transparent;
            panel2.Controls.Add(lblImageClipping);
            panel2.Controls.Add(ImgClipEnbRadio);
            panel2.Controls.Add(ImgClipDisRadio);
            panel2.Location = new System.Drawing.Point(12, 196);
            panel2.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            panel2.Name = "panel2";
            panel2.Size = new System.Drawing.Size(470, 115);
            panel2.TabIndex = 7;
            // 
            // lblImageClipping
            // 
            lblImageClipping.BackColor = System.Drawing.Color.Transparent;
            lblImageClipping.ForeColor = System.Drawing.Color.White;
            lblImageClipping.Location = new System.Drawing.Point(5, 0);
            lblImageClipping.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lblImageClipping.Name = "lblImageClipping";
            lblImageClipping.Size = new System.Drawing.Size(300, 40);
            lblImageClipping.TabIndex = 5;
            lblImageClipping.Text = "Image Clipping:";
            lblImageClipping.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // ImgClipEnbRadio
            // 
            ImgClipEnbRadio.AutoSize = true;
            ImgClipEnbRadio.BackColor = System.Drawing.Color.Transparent;
            ImgClipEnbRadio.ForeColor = System.Drawing.Color.White;
            ImgClipEnbRadio.Location = new System.Drawing.Point(315, 52);
            ImgClipEnbRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            ImgClipEnbRadio.Name = "ImgClipEnbRadio";
            ImgClipEnbRadio.Size = new System.Drawing.Size(107, 32);
            ImgClipEnbRadio.TabIndex = 1;
            ImgClipEnbRadio.Text = "Enabled";
            ImgClipEnbRadio.UseVisualStyleBackColor = false;
            ImgClipEnbRadio.CheckedChanged += ImgClippingSwitched;
            // 
            // ImgClipDisRadio
            // 
            ImgClipDisRadio.AutoSize = true;
            ImgClipDisRadio.BackColor = System.Drawing.Color.Transparent;
            ImgClipDisRadio.Checked = true;
            ImgClipDisRadio.ForeColor = System.Drawing.Color.White;
            ImgClipDisRadio.Location = new System.Drawing.Point(315, -4);
            ImgClipDisRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            ImgClipDisRadio.Name = "ImgClipDisRadio";
            ImgClipDisRadio.Size = new System.Drawing.Size(113, 32);
            ImgClipDisRadio.TabIndex = 0;
            ImgClipDisRadio.TabStop = true;
            ImgClipDisRadio.Text = "Disabled";
            ImgClipDisRadio.UseVisualStyleBackColor = false;
            // 
            // panel1
            // 
            panel1.BackColor = System.Drawing.Color.Transparent;
            panel1.Controls.Add(lblCaptchaSolving);
            panel1.Controls.Add(SemiAutoCaptchaRadio);
            panel1.Controls.Add(ManualCaptchaRadio);
            panel1.Location = new System.Drawing.Point(12, 69);
            panel1.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(470, 115);
            panel1.TabIndex = 6;
            // 
            // lblCaptchaSolving
            // 
            lblCaptchaSolving.BackColor = System.Drawing.Color.Transparent;
            lblCaptchaSolving.ForeColor = System.Drawing.Color.White;
            lblCaptchaSolving.Location = new System.Drawing.Point(5, 0);
            lblCaptchaSolving.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lblCaptchaSolving.Name = "lblCaptchaSolving";
            lblCaptchaSolving.Size = new System.Drawing.Size(300, 40);
            lblCaptchaSolving.TabIndex = 5;
            lblCaptchaSolving.Text = "Captcha Solving:";
            lblCaptchaSolving.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // SemiAutoCaptchaRadio
            // 
            SemiAutoCaptchaRadio.AutoSize = true;
            SemiAutoCaptchaRadio.BackColor = System.Drawing.Color.Transparent;
            SemiAutoCaptchaRadio.ForeColor = System.Drawing.Color.White;
            SemiAutoCaptchaRadio.Location = new System.Drawing.Point(315, 54);
            SemiAutoCaptchaRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            SemiAutoCaptchaRadio.Name = "SemiAutoCaptchaRadio";
            SemiAutoCaptchaRadio.Size = new System.Drawing.Size(128, 32);
            SemiAutoCaptchaRadio.TabIndex = 1;
            SemiAutoCaptchaRadio.Text = "Semi Auto";
            SemiAutoCaptchaRadio.UseVisualStyleBackColor = false;
            SemiAutoCaptchaRadio.CheckedChanged += CaptchaSolveSwitched;
            // 
            // ManualCaptchaRadio
            // 
            ManualCaptchaRadio.AutoSize = true;
            ManualCaptchaRadio.BackColor = System.Drawing.Color.Transparent;
            ManualCaptchaRadio.Checked = true;
            ManualCaptchaRadio.ForeColor = System.Drawing.Color.White;
            ManualCaptchaRadio.Location = new System.Drawing.Point(315, -2);
            ManualCaptchaRadio.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            ManualCaptchaRadio.Name = "ManualCaptchaRadio";
            ManualCaptchaRadio.Size = new System.Drawing.Size(102, 32);
            ManualCaptchaRadio.TabIndex = 0;
            ManualCaptchaRadio.TabStop = true;
            ManualCaptchaRadio.Text = "Manual";
            ManualCaptchaRadio.UseVisualStyleBackColor = false;
            // 
            // EnvironmentGroupBox
            // 
            EnvironmentGroupBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            EnvironmentGroupBox.BorderColour = System.Drawing.Color.FromArgb(2, 118, 196);
            EnvironmentGroupBox.Controls.Add(lblLanguage);
            EnvironmentGroupBox.Controls.Add(LanguageBox);
            EnvironmentGroupBox.Controls.Add(bntLibSelect);
            EnvironmentGroupBox.Controls.Add(LibraryPathTBox);
            EnvironmentGroupBox.Controls.Add(lblLibrary);
            EnvironmentGroupBox.Font = new System.Drawing.Font("Segoe UI", 10F);
            EnvironmentGroupBox.HeaderColour = System.Drawing.Color.FromArgb(45, 45, 48);
            EnvironmentGroupBox.Location = new System.Drawing.Point(10, 12);
            EnvironmentGroupBox.MainColour = System.Drawing.Color.FromArgb(37, 37, 38);
            EnvironmentGroupBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            EnvironmentGroupBox.Name = "EnvironmentGroupBox";
            EnvironmentGroupBox.Size = new System.Drawing.Size(1373, 221);
            EnvironmentGroupBox.TabIndex = 0;
            EnvironmentGroupBox.Text = "Environment";
            EnvironmentGroupBox.TextColour = System.Drawing.Color.FromArgb(129, 129, 131);
            // 
            // lblLanguage
            // 
            lblLanguage.BackColor = System.Drawing.Color.Transparent;
            lblLanguage.ForeColor = System.Drawing.Color.White;
            lblLanguage.Location = new System.Drawing.Point(5, 138);
            lblLanguage.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lblLanguage.Name = "lblLanguage";
            lblLanguage.Size = new System.Drawing.Size(135, 40);
            lblLanguage.TabIndex = 4;
            lblLanguage.Text = "Language:";
            lblLanguage.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // LanguageBox
            // 
            LanguageBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            LanguageBox.ArrowColour = System.Drawing.Color.FromArgb(153, 153, 153);
            LanguageBox.BackColor = System.Drawing.Color.Transparent;
            LanguageBox.BaseColour = System.Drawing.Color.FromArgb(51, 51, 55);
            LanguageBox.BorderColour = System.Drawing.Color.FromArgb(35, 35, 35);
            LanguageBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            LanguageBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            LanguageBox.Font = new System.Drawing.Font("Segoe UI", 10F);
            LanguageBox.FontColour = System.Drawing.Color.FromArgb(255, 255, 255);
            LanguageBox.FormattingEnabled = true;
            LanguageBox.LineColour = System.Drawing.Color.FromArgb(0, 122, 204);
            LanguageBox.Location = new System.Drawing.Point(150, 137);
            LanguageBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            LanguageBox.Name = "LanguageBox";
            LanguageBox.Size = new System.Drawing.Size(1202, 35);
            LanguageBox.SqaureColour = System.Drawing.Color.FromArgb(51, 51, 55);
            LanguageBox.SqaureHoverColour = System.Drawing.Color.FromArgb(52, 52, 52);
            LanguageBox.StartIndex = 0;
            LanguageBox.TabIndex = 3;
            LanguageBox.SelectedIndexChanged += LanguageChanged;
            // 
            // bntLibSelect
            // 
            bntLibSelect.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            bntLibSelect.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            bntLibSelect.BaseColour = System.Drawing.Color.FromArgb(45, 45, 48);
            bntLibSelect.BorderColour = System.Drawing.Color.FromArgb(15, 15, 18);
            bntLibSelect.FontColour = System.Drawing.Color.FromArgb(153, 153, 153);
            bntLibSelect.HoverColour = System.Drawing.Color.FromArgb(60, 60, 62);
            bntLibSelect.ImageAlignment = VSButton.__ImageAlignment.Left;
            bntLibSelect.ImageChoice = null;
            bntLibSelect.Location = new System.Drawing.Point(1295, 69);
            bntLibSelect.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            bntLibSelect.Name = "bntLibSelect";
            bntLibSelect.ShowBorder = true;
            bntLibSelect.ShowImage = false;
            bntLibSelect.ShowText = true;
            bntLibSelect.Size = new System.Drawing.Size(60, 44);
            bntLibSelect.TabIndex = 2;
            bntLibSelect.Text = "...";
            bntLibSelect.TextAlignment = System.Drawing.StringAlignment.Center;
            bntLibSelect.Click += BntLibSelectClicked;
            // 
            // LibraryPathTBox
            // 
            LibraryPathTBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            LibraryPathTBox.BackColor = System.Drawing.Color.Transparent;
            LibraryPathTBox.BackgroundColour = System.Drawing.Color.FromArgb(51, 51, 55);
            LibraryPathTBox.BorderColour = System.Drawing.Color.FromArgb(35, 35, 35);
            LibraryPathTBox.Location = new System.Drawing.Point(150, 69);
            LibraryPathTBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            LibraryPathTBox.MaxLength = 32767;
            LibraryPathTBox.Multiline = false;
            LibraryPathTBox.Name = "LibraryPathTBox";
            LibraryPathTBox.ReadOnly = true;
            LibraryPathTBox.Size = new System.Drawing.Size(1135, 34);
            LibraryPathTBox.Style = VSNormalTextBox.Styles.NotRounded;
            LibraryPathTBox.TabIndex = 1;
            LibraryPathTBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            LibraryPathTBox.TextColour = System.Drawing.Color.FromArgb(153, 153, 153);
            LibraryPathTBox.UseSystemPasswordChar = false;
            // 
            // lblLibrary
            // 
            lblLibrary.BackColor = System.Drawing.Color.Transparent;
            lblLibrary.ForeColor = System.Drawing.Color.White;
            lblLibrary.Location = new System.Drawing.Point(5, 75);
            lblLibrary.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lblLibrary.Name = "lblLibrary";
            lblLibrary.Size = new System.Drawing.Size(135, 40);
            lblLibrary.TabIndex = 0;
            lblLibrary.Text = "Library:";
            lblLibrary.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // AboutTab
            // 
            AboutTab.BackColor = System.Drawing.Color.FromArgb(28, 28, 28);
            AboutTab.Controls.Add(SupportedHostsBox);
            AboutTab.Controls.Add(lblCredits);
            AboutTab.Controls.Add(lblTitle);
            AboutTab.Location = new System.Drawing.Point(4, 20);
            AboutTab.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            AboutTab.Name = "AboutTab";
            AboutTab.Padding = new System.Windows.Forms.Padding(5, 6, 5, 6);
            AboutTab.Size = new System.Drawing.Size(1389, 874);
            AboutTab.TabIndex = 2;
            AboutTab.Text = "About";
            // 
            // SupportedHostsBox
            // 
            SupportedHostsBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            SupportedHostsBox.BorderColour = System.Drawing.Color.FromArgb(2, 118, 196);
            SupportedHostsBox.Controls.Add(SupportedHostListBox);
            SupportedHostsBox.Font = new System.Drawing.Font("Segoe UI", 10F);
            SupportedHostsBox.HeaderColour = System.Drawing.Color.FromArgb(45, 45, 48);
            SupportedHostsBox.Location = new System.Drawing.Point(15, 115);
            SupportedHostsBox.MainColour = System.Drawing.Color.FromArgb(37, 37, 38);
            SupportedHostsBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            SupportedHostsBox.Name = "SupportedHostsBox";
            SupportedHostsBox.Size = new System.Drawing.Size(1353, 700);
            SupportedHostsBox.TabIndex = 2;
            SupportedHostsBox.Text = "Supported Hosts";
            SupportedHostsBox.TextColour = System.Drawing.Color.FromArgb(129, 129, 131);
            // 
            // SupportedHostListBox
            // 
            SupportedHostListBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            SupportedHostListBox.BaseColour = System.Drawing.Color.FromArgb(37, 37, 38);
            SupportedHostListBox.BorderColour = System.Drawing.Color.FromArgb(35, 35, 35);
            SupportedHostListBox.DontShowInnerScrollbarBorder = false;
            SupportedHostListBox.FontColour = System.Drawing.Color.FromArgb(199, 199, 199);
            SupportedHostListBox.Location = new System.Drawing.Point(5, 63);
            SupportedHostListBox.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            SupportedHostListBox.MultiSelect = false;
            SupportedHostListBox.Name = "SupportedHostListBox";
            SupportedHostListBox.NonSelectedItemColour = System.Drawing.Color.FromArgb(62, 62, 64);
            SupportedHostListBox.SelectedItemColour = System.Drawing.Color.FromArgb(47, 47, 47);
            SupportedHostListBox.ShowWholeInnerBorder = true;
            SupportedHostListBox.Size = new System.Drawing.Size(1343, 613);
            SupportedHostListBox.TabIndex = 0;
            SupportedHostListBox.DoubleClick += SupportedHostClicked;
            // 
            // lblCredits
            // 
            lblCredits.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            lblCredits.AutoSize = true;
            lblCredits.ForeColor = System.Drawing.Color.White;
            lblCredits.Location = new System.Drawing.Point(1143, 821);
            lblCredits.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lblCredits.Name = "lblCredits";
            lblCredits.Size = new System.Drawing.Size(212, 25);
            lblCredits.TabIndex = 1;
            lblCredits.Text = "Created By Marcussacana";
            // 
            // lblTitle
            // 
            lblTitle.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 128);
            lblTitle.ForeColor = System.Drawing.Color.White;
            lblTitle.Location = new System.Drawing.Point(5, 6);
            lblTitle.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new System.Drawing.Size(1368, 138);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "MangaUnhost";
            lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // DebugTab
            // 
            DebugTab.BackColor = System.Drawing.Color.FromArgb(28, 28, 28);
            DebugTab.Controls.Add(dbgTranslate);
            DebugTab.Controls.Add(dbgBrowser);
            DebugTab.Controls.Add(dbgButtonC);
            DebugTab.Controls.Add(dbgButtonB);
            DebugTab.Controls.Add(DbgButtonA);
            DebugTab.Controls.Add(DbgPreview);
            DebugTab.Location = new System.Drawing.Point(4, 20);
            DebugTab.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            DebugTab.Name = "DebugTab";
            DebugTab.Padding = new System.Windows.Forms.Padding(5, 6, 5, 6);
            DebugTab.Size = new System.Drawing.Size(1389, 874);
            DebugTab.TabIndex = 3;
            DebugTab.Text = "Debug";
            // 
            // dbgTranslate
            // 
            dbgTranslate.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            dbgTranslate.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            dbgTranslate.BaseColour = System.Drawing.Color.FromArgb(45, 45, 48);
            dbgTranslate.BorderColour = System.Drawing.Color.FromArgb(15, 15, 18);
            dbgTranslate.FontColour = System.Drawing.Color.FromArgb(153, 153, 153);
            dbgTranslate.HoverColour = System.Drawing.Color.FromArgb(60, 60, 62);
            dbgTranslate.ImageAlignment = VSButton.__ImageAlignment.Left;
            dbgTranslate.ImageChoice = null;
            dbgTranslate.Location = new System.Drawing.Point(825, 67);
            dbgTranslate.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            dbgTranslate.Name = "dbgTranslate";
            dbgTranslate.ShowBorder = true;
            dbgTranslate.ShowImage = false;
            dbgTranslate.ShowText = true;
            dbgTranslate.Size = new System.Drawing.Size(163, 44);
            dbgTranslate.TabIndex = 5;
            dbgTranslate.Text = "Dbg Translate";
            dbgTranslate.TextAlignment = System.Drawing.StringAlignment.Center;
            dbgTranslate.Click += dbgTranslate_Click;
            // 
            // dbgBrowser
            // 
            dbgBrowser.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            dbgBrowser.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            dbgBrowser.BaseColour = System.Drawing.Color.FromArgb(45, 45, 48);
            dbgBrowser.BorderColour = System.Drawing.Color.FromArgb(15, 15, 18);
            dbgBrowser.FontColour = System.Drawing.Color.FromArgb(153, 153, 153);
            dbgBrowser.HoverColour = System.Drawing.Color.FromArgb(60, 60, 62);
            dbgBrowser.ImageAlignment = VSButton.__ImageAlignment.Left;
            dbgBrowser.ImageChoice = null;
            dbgBrowser.Location = new System.Drawing.Point(662, 67);
            dbgBrowser.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            dbgBrowser.Name = "dbgBrowser";
            dbgBrowser.ShowBorder = true;
            dbgBrowser.ShowImage = false;
            dbgBrowser.ShowText = true;
            dbgBrowser.Size = new System.Drawing.Size(153, 44);
            dbgBrowser.TabIndex = 4;
            dbgBrowser.Text = "Dbg Browser";
            dbgBrowser.TextAlignment = System.Drawing.StringAlignment.Center;
            dbgBrowser.Click += dbgBrowser_Click;
            // 
            // dbgButtonC
            // 
            dbgButtonC.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            dbgButtonC.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            dbgButtonC.BaseColour = System.Drawing.Color.FromArgb(45, 45, 48);
            dbgButtonC.BorderColour = System.Drawing.Color.FromArgb(15, 15, 18);
            dbgButtonC.FontColour = System.Drawing.Color.FromArgb(153, 153, 153);
            dbgButtonC.HoverColour = System.Drawing.Color.FromArgb(60, 60, 62);
            dbgButtonC.ImageAlignment = VSButton.__ImageAlignment.Left;
            dbgButtonC.ImageChoice = null;
            dbgButtonC.Location = new System.Drawing.Point(998, 12);
            dbgButtonC.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            dbgButtonC.Name = "dbgButtonC";
            dbgButtonC.ShowBorder = true;
            dbgButtonC.ShowImage = false;
            dbgButtonC.ShowText = true;
            dbgButtonC.Size = new System.Drawing.Size(172, 44);
            dbgButtonC.TabIndex = 3;
            dbgButtonC.Text = "Dbg Cloudflare";
            dbgButtonC.TextAlignment = System.Drawing.StringAlignment.Center;
            dbgButtonC.Click += DbgButtonCClicked;
            // 
            // dbgButtonB
            // 
            dbgButtonB.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            dbgButtonB.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            dbgButtonB.BaseColour = System.Drawing.Color.FromArgb(45, 45, 48);
            dbgButtonB.BorderColour = System.Drawing.Color.FromArgb(15, 15, 18);
            dbgButtonB.FontColour = System.Drawing.Color.FromArgb(153, 153, 153);
            dbgButtonB.HoverColour = System.Drawing.Color.FromArgb(60, 60, 62);
            dbgButtonB.ImageAlignment = VSButton.__ImageAlignment.Left;
            dbgButtonB.ImageChoice = null;
            dbgButtonB.Location = new System.Drawing.Point(825, 12);
            dbgButtonB.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            dbgButtonB.Name = "dbgButtonB";
            dbgButtonB.ShowBorder = true;
            dbgButtonB.ShowImage = false;
            dbgButtonB.ShowText = true;
            dbgButtonB.Size = new System.Drawing.Size(163, 44);
            dbgButtonB.TabIndex = 2;
            dbgButtonB.Text = "Dbg hCaptcha";
            dbgButtonB.TextAlignment = System.Drawing.StringAlignment.Center;
            dbgButtonB.Click += DbgButtonBClicked;
            // 
            // DbgButtonA
            // 
            DbgButtonA.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            DbgButtonA.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            DbgButtonA.BaseColour = System.Drawing.Color.FromArgb(45, 45, 48);
            DbgButtonA.BorderColour = System.Drawing.Color.FromArgb(15, 15, 18);
            DbgButtonA.FontColour = System.Drawing.Color.FromArgb(153, 153, 153);
            DbgButtonA.HoverColour = System.Drawing.Color.FromArgb(60, 60, 62);
            DbgButtonA.ImageAlignment = VSButton.__ImageAlignment.Left;
            DbgButtonA.ImageChoice = null;
            DbgButtonA.Location = new System.Drawing.Point(662, 12);
            DbgButtonA.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            DbgButtonA.Name = "DbgButtonA";
            DbgButtonA.ShowBorder = true;
            DbgButtonA.ShowImage = false;
            DbgButtonA.ShowText = true;
            DbgButtonA.Size = new System.Drawing.Size(153, 44);
            DbgButtonA.TabIndex = 1;
            DbgButtonA.Text = "Dbg Captcha";
            DbgButtonA.TextAlignment = System.Drawing.StringAlignment.Center;
            DbgButtonA.Click += DbgButtonClicked;
            // 
            // DbgPreview
            // 
            DbgPreview.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            DbgPreview.BackColor = System.Drawing.Color.White;
            DbgPreview.Location = new System.Drawing.Point(0, 0);
            DbgPreview.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            DbgPreview.Name = "DbgPreview";
            DbgPreview.Size = new System.Drawing.Size(652, 852);
            DbgPreview.TabIndex = 0;
            DbgPreview.TabStop = false;
            // 
            // Main
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1397, 1019);
            Controls.Add(ThemeContainer);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            MaximumSize = new System.Drawing.Size(3840, 2088);
            Name = "Main";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "MangaUnhost";
            TransparencyKey = System.Drawing.Color.Fuchsia;
            FormClosing += MainClosing;
            Shown += MainShown;
            ThemeContainer.ResumeLayout(false);
            MainTabMenu.ResumeLayout(false);
            DownloaderTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)CoverBox).EndInit();
            LibraryTab.ResumeLayout(false);
            CrawlerTab.ResumeLayout(false);
            SettingsTab.ResumeLayout(false);
            FeaturesGroupBox.ResumeLayout(false);
            panel10.ResumeLayout(false);
            panel10.PerformLayout();
            panel9.ResumeLayout(false);
            panel9.PerformLayout();
            panel8.ResumeLayout(false);
            panel8.PerformLayout();
            panel7.ResumeLayout(false);
            panel7.PerformLayout();
            panel6.ResumeLayout(false);
            panel6.PerformLayout();
            panel5.ResumeLayout(false);
            panel5.PerformLayout();
            panel4.ResumeLayout(false);
            panel4.PerformLayout();
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            EnvironmentGroupBox.ResumeLayout(false);
            AboutTab.ResumeLayout(false);
            AboutTab.PerformLayout();
            SupportedHostsBox.ResumeLayout(false);
            DebugTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)DbgPreview).EndInit();
            ResumeLayout(false);

        }

        #endregion

        private VSContainer ThemeContainer;
        private VSTabControl MainTabMenu;
        private System.Windows.Forms.TabPage DownloaderTab;
        private System.Windows.Forms.TabPage SettingsTab;
        private System.Windows.Forms.TabPage AboutTab;
        private System.Windows.Forms.TabPage DebugTab;
        private System.Windows.Forms.Label TitleLabel;
        private System.Windows.Forms.PictureBox CoverBox;
        private System.Windows.Forms.FlowLayoutPanel ButtonsContainer;
        private System.Windows.Forms.Timer MainTimer;
        private VSStatusBar StatusBar;
        private System.Windows.Forms.PictureBox DbgPreview;
        private VSButton DbgButtonA;
        private VSGroupBox EnvironmentGroupBox;
        private VSButton bntLibSelect;
        private VSNormalTextBox LibraryPathTBox;
        private System.Windows.Forms.Label lblLibrary;
        private System.Windows.Forms.Label lblLanguage;
        private VSComboBox LanguageBox;
        private VSGroupBox FeaturesGroupBox;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblCaptchaSolving;
        private System.Windows.Forms.RadioButton SemiAutoCaptchaRadio;
        private System.Windows.Forms.RadioButton ManualCaptchaRadio;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label lblImageClipping;
        private System.Windows.Forms.RadioButton ImgClipEnbRadio;
        private System.Windows.Forms.RadioButton ImgClipDisRadio;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Label lblClipWatcher;
        private System.Windows.Forms.RadioButton ClipWatcherEnbRadio;
        private System.Windows.Forms.RadioButton ClipWatcherDisRadio;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Label lblReadeGenerator;
        private System.Windows.Forms.RadioButton ReaderGenEnbRadio;
        private System.Windows.Forms.RadioButton ReaderGenDisRadio;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.RadioButton SaveAsRawRadio;
        private System.Windows.Forms.RadioButton SaveAsBmpRadio;
        private System.Windows.Forms.Label lblSaveAs;
        private System.Windows.Forms.RadioButton SaveAsJpgRadio;
        private System.Windows.Forms.RadioButton SaveAsPngRadio;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.RadioButton SkipDownEnbRadio;
        private System.Windows.Forms.RadioButton SkipDownDisRadio;
        private System.Windows.Forms.Label lblSkipDownloaded;
        private VSGroupBox SupportedHostsBox;
        private VSListBoxWBuiltInScrollBar SupportedHostListBox;
        private System.Windows.Forms.Label lblCredits;
        private System.Windows.Forms.Label lblTitle;
        private VSVerticalScrollBar ContainerScrollBar;
        private System.Windows.Forms.TabPage LibraryTab;
        private ScrollFlowLayoutPanel LibraryContainer;
        private System.Windows.Forms.RadioButton SaveAsAutoRadio;
        private System.Windows.Forms.TabPage CrawlerTab;
        private VSListBoxWBuiltInScrollBar LinksListBox;
        private VSButton CrawlerCopyBtn;
        private System.Windows.Forms.Label lblRegex;
        private VSNormalTextBox tbCrawlerRegex;
        private VSButton CrawlerStartBtn;
        private System.Windows.Forms.Label lblUrl;
        private VSNormalTextBox tbCrawlerUrl;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.RadioButton NewFolderRadio;
        private System.Windows.Forms.RadioButton AskRadio;
        private System.Windows.Forms.RadioButton UpdateUrlRadio;
        private System.Windows.Forms.Label lblReplaceMode;
        private VSButton dbgButtonB;
        private VSButton dbgButtonC;
        private System.Windows.Forms.Panel panel8;
        private System.Windows.Forms.RadioButton OtherReaderRadio;
        private System.Windows.Forms.RadioButton ComicReaderRadio;
        private System.Windows.Forms.RadioButton MangaReaderRadio;
        private System.Windows.Forms.RadioButton LegacyReaderRadio;
        private System.Windows.Forms.Label lblReader;
        private System.Windows.Forms.Panel panel9;
        private System.Windows.Forms.Label lblLibUpdates;
        private System.Windows.Forms.RadioButton ManualUpCheckRadio;
        private System.Windows.Forms.RadioButton AutoUpCheckRadio;
        private System.Windows.Forms.Panel panel10;
        private System.Windows.Forms.RadioButton APNGBypassDisRadio;
        private System.Windows.Forms.RadioButton APNGBypassEnaRadio;
        private System.Windows.Forms.Label label1;
        private VSButton dbgBrowser;
        private VSButton dbgTranslate;
		private VSButton btnSetupComparsion;
	}
}

