using Microsoft.WindowsAPICodePack.Dialogs;
using CefSharp;
using CefSharp.OffScreen;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace MangaUnhost
{
    public partial class Main : Form
    {

        Thread CrawlerThread = null;
        Thread PostProcessThread = null;

        Settings Settings = new Settings();

        public IHost[] Hosts { get; private set; } = GetHostsInstances();

        ILanguage[] Languages = GetLanguagesInstance();

        Queue<Page> PostProcessQueue = new Queue<Page>();

        public static string SettingsPath => Program.SettingsPath;

        string DefaultLibPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{CurrentLanguage.Library}\\");

        ILanguage CurrentLanguage = new Languages.English();

        ComicInfo CurrentInfo;
        IHost CurrentHost;

        public static string Status
        {
            get => Instance.StatusBar.FirstLabelText;
            set
            {
                Instance.StatusBar.FirstLabelText = value;

                if (Instance.InvokeRequired)
                    Instance.Invoke(new MethodInvoker(() => Application.DoEvents()));
                else
                    Application.DoEvents();
            }
        }
        public static string SubStatus
        {
            get => Instance.StatusBar.SecondLabelText;
            set
            {
                Instance.StatusBar.SecondLabelText = value;

                if (Instance.InvokeRequired)
                    Instance.Invoke(new MethodInvoker(() => Application.DoEvents()));
                else
                    Application.DoEvents();
            }
        }
        public static string ExtraStatus
        {
            get => Instance.StatusBar.ThirdLabelText;
            set
            {
                Instance.StatusBar.ThirdLabelText = value;

                if (Instance.InvokeRequired)
                    Instance.Invoke(new MethodInvoker(() => Application.DoEvents()));
                else
                    Application.DoEvents();
            }
        }
        public static bool ThreeStatus
        {
            get => Instance.StatusBar.AmountOfString == VSStatusBar.AmountOfStrings.Three;
            set
            {
                Instance.StatusBar.AmountOfString = value ? VSStatusBar.AmountOfStrings.Three : VSStatusBar.AmountOfStrings.Two;

                if (Instance.InvokeRequired)
                    Instance.Invoke(new MethodInvoker(() => Application.DoEvents()));
                else
                    Application.DoEvents();
            }
        }

        public static ILanguage Language
        {
            get
            {
                return Instance.CurrentLanguage;
            }
        }

        public static ReaderMode Reader
        {
            get
            {
                return (ReaderMode)Instance.Settings.ReaderMode;
            }
        }

        internal static Settings Config
        {
            get {
                return Instance.Settings;
            }
        }

        public static CaptchaSolverType Solver => Instance.Settings.AutoCaptcha ? CaptchaSolverType.SemiAuto : CaptchaSolverType.Manual;

        public static Main Instance = null;
        private string LastClipboard = null;
        private string LastIndex = null;

        public Main()
        {
            InitializeComponent();
            Instance = this;

            if (!Program.Debug)
                MainTabMenu.TabPages.Remove(DebugTab);

            foreach (var Language in Languages)
                LanguageBox.Items.Add(Language.LanguageName);

            foreach (var Host in Hosts)
                SupportedHostListBox.AddItem(Host.GetPluginInfo().Name);

            SupportedHostListBox.ContextMenuStrip = new ContextMenuStrip();
            SupportedHostListBox.ContextMenuStrip.AutoSize = true;
            SupportedHostListBox.ContextMenuStrip.Opening += SupportedHostContextOpen;
            SupportedHostListBox.ContextMenuStrip.ShowCheckMargin = false;
            SupportedHostListBox.ContextMenuStrip.ShowImageMargin = false;

            if (File.Exists(SettingsPath))
            {
                AdvancedIni.FastOpen(out Settings, SettingsPath);
            }
            else
            {
                Settings = new Settings()
                {
                    Language = "English",
                    AutoCaptcha = false,
                    ImageClipping = true,
                    ReaderGenerator = true,
                    SkipDownloaded = true,
                    AutoLibUpCheck = Program.IsRealWindows,
                    LibraryPath = DefaultLibPath,
                    ReplaceMode = 1,
                    ReaderMode = 0
                };
            }

            ReloadSettings();
            var CefSettings = new CefSettings()
            {
                BrowserSubprocessPath = Program.BrowserSubprocessPath,
                LogSeverity = LogSeverity.Disable,
                WindowlessRenderingEnabled = true
            };

            CefSettings.RegisterScheme(new CefCustomScheme()
            {
                SchemeName = LocalSchemeFactory.SchemeName,
                SchemeHandlerFactory = new LocalSchemeFactory()
            });

            Cef.Initialize(CefSettings, false, browserProcessHandler: null);
        }

        private void MainShown(object sender, EventArgs e)
        {
            TopMost = true;
            BringToFront();
            Focus();
            TopMost = false;

            Invalidate();

            PostProcessThread = new Thread(PostProcessWorker);
            PostProcessThread.Start();

            CrawlerThread = new Thread(CrawlerWorker);
            CrawlerThread.Start();

            if (!Program.Updater.HaveUpdate())
                return;

            if (MessageBox.Show(CurrentLanguage.UpdateFound, "MangaUnhost", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Hide();
                Program.Updater.Update();
            }

            MainTimer.Enabled = true;
        }

        private T CreateInstance<T>(T Interface)
        {
            return (T)Activator.CreateInstance(Interface.GetType());
        }

        private void MainTimerTick(object sender, EventArgs e)
        {
            CheckClipboard();
        }

        private void CheckClipboard()
        {
            string Clip = null;

            try
            {
                Clip = Clipboard.GetText();
            }
            catch { }

            if (LastClipboard == Clip || Clip == null)
                return;

            LastClipboard = Clip;

            string[] Lines = Clip.Replace("\r\n", "\n").Split('\n');
            Lines = (from x in Lines where Uri.TryCreate(x, UriKind.Absolute, out _) select x).ToArray();
            if (Lines.Length > 1)
                Lines = (from x in Lines where (from y in Hosts where y.IsValidUri(new Uri(x)) select y).Any() select x).ToArray();

            if (Lines.Length > 1)
            {
                MainTimer.Enabled = false;

                var Rst = MessageBox.Show(this, string.Format(Language.ConfirmBulk, Lines.Length), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (Rst == DialogResult.No)
                {
                    MainTimer.Enabled = true;
                    return;
                }

                StatusBar.AmountOfString = VSStatusBar.AmountOfStrings.Three;
                for (int i = 0; i < Lines.Length; i++)
                {
                    string Link = Lines[i];
                    StatusBar.ThirdLabelText = string.Format(Language.QueueStatus, i + 1, Lines.Length);

                    AutoDownload(new Uri(Link));
                }

                StatusBar.AmountOfString = VSStatusBar.AmountOfStrings.Two;
                StatusBar.ThirdLabelText = string.Empty;
                StatusBar.Invalidate();

                MainTimer.Enabled = true;
            }
            else
            {
                bool Result = Uri.TryCreate(Clip, UriKind.Absolute, out Uri URL);
                if (!Result)
                    return;

                bool PluginFound = false;
                foreach (var Host in Hosts)
                {
                    if (!Host.IsValidUri(URL))
                        continue;

                    try
                    {
                        LoadUri(URL, Host);
                        PluginFound = true;
                        break;
                    }
                    catch
                    {
                        if (Program.Debug)
                            throw;
                    }

                    StatusBar.SecondLabelText = string.Empty;
                    Status = CurrentLanguage.IDLE;
                }

                if (!PluginFound)
                {
                    Status = CurrentLanguage.Loading;
                    string HTML;
                    var Data = URL.TryDownload(URL.Host, ProxyTools.UserAgent);
                    if (Data == null)
                    {
                        var BypassData = JSTools.BypassCloudflare(URL.Host);
                        HTML = BypassData.HTML;
                    }
                    else
                        HTML = Encoding.UTF8.GetString(Data);

                    if (string.IsNullOrWhiteSpace(HTML))
                        return;

                    foreach (var Host in Hosts)
                    {
                        if (!Host.GetPluginInfo().GenericPlugin)
                            continue;

                        try
                        {
                            if (!Host.IsValidPage(HTML, URL))
                                continue;

                            LoadUri(URL, Host);
                            break;
                        }
                        catch { }
                    }
                    StatusBar.SecondLabelText = string.Empty;
                    Status = CurrentLanguage.IDLE;
                }
            }
        }

        private void DbgButtonClicked(object sender, EventArgs e)
        {
            var OBrowser = new ChromiumWebBrowser("https://patrickhlauke.github.io/recaptcha/", new BrowserSettings
            {
                WebSecurity = CefState.Disabled
            });

            OBrowser.Size = new Size(1280, 720);
            OBrowser.ReCaptchaHook();

            while (!OBrowser.IsBrowserInitialized)
            {
                ThreadTools.Wait(50, true);
            }

            ThreadTools.Wait(3000, true);
            DbgPreview.Image = OBrowser.ScreenshotOrNull();
            Application.DoEvents();


            OBrowser.ReCaptchaTrySolve(CaptchaSolverType.Manual);

            DbgPreview.Image = OBrowser.ScreenshotOrNull();
            MessageBox.Show("Finished");
        }
        private void DbgButtonBClicked(object sender, EventArgs e)
        {
            var OBrowser = new ChromiumWebBrowser("https://www.tokyobitcoiner.com/hcaptcha", new BrowserSettings
            {
                WebSecurity = CefState.Disabled
            });

            OBrowser.Size = new Size(1280, 720);
            OBrowser.WaitInitialize();

            OBrowser.ShowDevTools();

            ThreadTools.Wait(3000, true);
            DbgPreview.Image = OBrowser.ScreenshotOrNull();
            Application.DoEvents();


            OBrowser.hCaptchaSolve();

            DbgPreview.Image = OBrowser.ScreenshotOrNull();
            MessageBox.Show("Finished");
        }
        private void DbgButtonCClicked(object sender, EventArgs e)
        {
            var OBrowser = new ChromiumWebBrowser("https://bato.to/", new BrowserSettings
            {
                WebSecurity = CefState.Disabled
            });

            OBrowser.Size = new Size(1280, 720);
            OBrowser.WaitInitialize();

            OBrowser.ShowDevTools();

            ThreadTools.Wait(3000, true);
            DbgPreview.Image = OBrowser.ScreenshotOrNull();
            Application.DoEvents();


            var cfdata = OBrowser.BypassCloudflare();

            MessageBox.Show("Finished");
            DbgPreview.Image = OBrowser.ScreenshotOrNull();
        }

        private void BntLibSelectClicked(object sender, EventArgs e)
        {
            var FileDialog = new CommonOpenFileDialog()
            {
                Multiselect = false,
                IsFolderPicker = true,
                EnsurePathExists = true,
                InitialDirectory = LibraryPathTBox.Text
            };

            if (FileDialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            LibraryPathTBox.Text = Settings.LibraryPath = FileDialog.FileNames.First();
        }

        private void CaptchaSolveSwitched(object sender, EventArgs e)
        {
            Settings.AutoCaptcha = SemiAutoCaptchaRadio.Checked;
        }

        private void ClipWatcherSwitched(object sender, EventArgs e)
        {
            MainTimer.Enabled = ClipWatcherEnbRadio.Checked;
        }

        private void ImgClippingSwitched(object sender, EventArgs e)
        {
            Settings.ImageClipping = ImgClipEnbRadio.Checked;
        }

        private void ReaderGeneratorSwitched(object sender, EventArgs e)
        {
            Settings.ReaderGenerator = ReaderGenEnbRadio.Checked;
        }

        private void SkipDownloadedSwitched(object sender, EventArgs e)
        {
            Settings.SkipDownloaded = SkipDownEnbRadio.Checked;
        }

        private void PNGSaveAs(object sender, EventArgs e)
        {
            if (SaveAsPngRadio.Checked)
                Settings.SaveAs = (int)SaveAs.PNG;
        }

        private void JPGSaveAs(object sender, EventArgs e)
        {
            if (SaveAsJpgRadio.Checked)
                Settings.SaveAs = (int)SaveAs.JPG;
        }

        private void BMPSaveAs(object sender, EventArgs e)
        {
            if (SaveAsBmpRadio.Checked)
                Settings.SaveAs = (int)SaveAs.BMP;
        }

        private void RAWSaveAs(object sender, EventArgs e)
        {
            if (SaveAsRawRadio.Checked)
                Settings.SaveAs = (int)SaveAs.RAW;
        }

        private void AutoSaveAs(object sender, EventArgs e)
        {
            if (SaveAsRawRadio.Checked)
                Settings.SaveAs = (int)SaveAs.AUTO;
        }

        private void LanguageChanged(object sender, EventArgs e)
        {
            Settings.Language = LanguageBox.SelectedItem.ToString();
            ReloadSettings();
        }

        private void ReloadSettings()
        {
            string OriPath = null;
            if (CurrentLanguage != null)
                OriPath = DefaultLibPath;

            foreach (var Language in Languages)
            {
                CurrentLanguage = Language;
                if (!Directory.Exists(Settings.LibraryPath) && Directory.Exists(DefaultLibPath))
                {
                    LibraryPathTBox.Text = Settings.LibraryPath = OriPath = DefaultLibPath;
                }
            }

            CurrentLanguage = (from x in Languages where x.LanguageName == Settings.Language select x).Single();

            for (int i = 0; i < LanguageBox.Items.Count; i++)
            {
                if (LanguageBox.Items[i].ToString() == CurrentLanguage.LanguageName)
                {
                    LanguageBox.SelectedIndex = i;
                    break;
                }
            }

            if (!Directory.Exists(Settings.LibraryPath) && Directory.Exists(DefaultLibPath))
                LibraryPathTBox.Text = Settings.LibraryPath = DefaultLibPath;

            LibraryPathTBox.Text = Settings.LibraryPath;

            //Update UI Settings Status
            ClipWatcherDisRadio.Checked = false;
            ClipWatcherEnbRadio.Checked = true;

            ManualCaptchaRadio.Checked = !Settings.AutoCaptcha;
            SemiAutoCaptchaRadio.Checked = Settings.AutoCaptcha;
            ImgClipDisRadio.Checked = !Settings.ImageClipping;
            ImgClipEnbRadio.Checked = Settings.ImageClipping;
            ReaderGenDisRadio.Checked = !Settings.ReaderGenerator;
            ReaderGenEnbRadio.Checked = Settings.ReaderGenerator;
            SkipDownDisRadio.Checked = !Settings.SkipDownloaded;
            SkipDownEnbRadio.Checked = Settings.SkipDownloaded;
            AutoUpCheckRadio.Checked = Settings.AutoLibUpCheck;
            ManualUpCheckRadio.Checked = !Settings.AutoLibUpCheck;

            SaveAs SaveAs = (SaveAs)Settings.SaveAs;
            SaveAsPngRadio.Checked = SaveAs == SaveAs.PNG;
            SaveAsJpgRadio.Checked = SaveAs == SaveAs.JPG;
            SaveAsBmpRadio.Checked = SaveAs == SaveAs.BMP;
            SaveAsRawRadio.Checked = SaveAs == SaveAs.RAW;
            SaveAsAutoRadio.Checked = SaveAs == SaveAs.AUTO;

            ReplaceMode ReplaceWhen = (ReplaceMode)Settings.ReplaceMode;
            UpdateUrlRadio.Checked = ReplaceWhen == ReplaceMode.UpdateURL;
            NewFolderRadio.Checked = ReplaceWhen == ReplaceMode.NewFolder;
            AskRadio.Checked = ReplaceWhen == ReplaceMode.Ask;

            ReaderMode Reader = (ReaderMode)Settings.ReaderMode;
            LegacyReaderRadio.Checked = Reader == ReaderMode.Legacy;
            MangaReaderRadio.Checked = Reader == ReaderMode.Manga;
            ComicReaderRadio.Checked = Reader == ReaderMode.Comic;
            OtherReaderRadio.Checked = Reader == ReaderMode.Other;

            //Load Translation
            DownloaderTab.Text = CurrentLanguage.DownloaderTab;
            SettingsTab.Text = CurrentLanguage.SettingsTab;
            AboutTab.Text = CurrentLanguage.AboutTab;
            LibraryTab.Text = CurrentLanguage.Library;

            EnvironmentGroupBox.Text = CurrentLanguage.EnvironmentBox;
            FeaturesGroupBox.Text = CurrentLanguage.FeaturesBox;

            lblCaptchaSolving.Text = CurrentLanguage.CaptchaSolvingLbl;
            lblClipWatcher.Text = CurrentLanguage.ClipboardWatcherLbl;
            lblImageClipping.Text = CurrentLanguage.ImageClippingLbl;
            lblLanguage.Text = CurrentLanguage.LanguageLbl;
            lblLibrary.Text = CurrentLanguage.LibraryLbl;
            lblReadeGenerator.Text = CurrentLanguage.ReaderGeneratorLbl;
            lblSaveAs.Text = CurrentLanguage.SaveAsLbl;
            lblSkipDownloaded.Text = CurrentLanguage.SkipDownloadedLbl;
            lblReplaceMode.Text = CurrentLanguage.ReplaceModeLbl;
            lblReader.Text = CurrentLanguage.ReaderModeLbl;
            lblLibUpdates.Text = CurrentLanguage.LibraryUpdates;

            ClipWatcherDisRadio.Text = CurrentLanguage.Disabled;
            ImgClipDisRadio.Text = CurrentLanguage.Disabled;
            ReaderGenDisRadio.Text = CurrentLanguage.Disabled;
            SkipDownDisRadio.Text = CurrentLanguage.Disabled;
            ClipWatcherEnbRadio.Text = CurrentLanguage.Enabled;
            ImgClipEnbRadio.Text = CurrentLanguage.Enabled;
            ReaderGenEnbRadio.Text = CurrentLanguage.Enabled;
            SkipDownEnbRadio.Text = CurrentLanguage.Enabled;
            UpdateUrlRadio.Text = CurrentLanguage.UpdateURL;
            NewFolderRadio.Text = CurrentLanguage.NewFolder;
            LegacyReaderRadio.Text = CurrentLanguage.Legacy;
            OtherReaderRadio.Text = CurrentLanguage.Other;
            AskRadio.Text = CurrentLanguage.Ask;

            SupportedHostsBox.Text = CurrentLanguage.SupportedHostsBox;

            CrawlerStartBtn.Text = CurrentLanguage.Start;
            CrawlerCopyBtn.Text = CurrentLanguage.Copy;

            lblTitle.Text = $"MangaUnhost v{GitHub.CurrentVersion}";
        }

        private void MainClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                AdvancedIni.FastSave(Settings, SettingsPath);
            }
            catch { }

            Cef.Shutdown();
            Environment.Exit(0);
        }

        private void SupportedHostClicked(object sender, EventArgs e)
        {
            if (SupportedHostListBox.SelectedItems.Length != 1)
                return;

            Point Click = SupportedHostListBox.PointToClient(Cursor.Position);

            int Index = SupportedHostListBox.GetElementIndex(Click);

            if (Index < 0)
                return;

            var PluginName = SupportedHostListBox.Items[Index].Text;
            var Host = (from x in Hosts where x.GetPluginInfo().Name == PluginName select x).Single();

            var Form = new PluginInfoPreview(Host, CurrentLanguage);
            Form.ShowDialog();
        }

        private void SupportedHostContextOpen(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Point Click = SupportedHostListBox.PointToClient(Cursor.Position);
            int Index = SupportedHostListBox.GetElementIndex(Click);

            if (Index < 0)
            {
                e.Cancel = true;
                return;
            }

            var PluginName = SupportedHostListBox.Items[Index].Text;
            var Host = (from x in Hosts where x.GetPluginInfo().Name == PluginName select x).Single();

            var AllActions = Host.GetPluginInfo().Actions;
            if (AllActions == null)
            {
                e.Cancel = true;
                return;
            }

            var Actions = (from x in AllActions where x.Availability.HasFlag(ActionTo.About) select x);

            if (!Actions.Any())
            {
                e.Cancel = true;
                return;
            }

            SupportedHostListBox.ContextMenuStrip.Items.Clear();

            foreach (var Action in Actions)
            {
                if (Action.Debug && !Program.Debug)
                    continue;
                var Item = new ToolStripButton(Action.Debug ? $"[DEBUG] {Action.Name}" : Action.Name);
                Item.AutoSize = true;
                Item.Click += (a, b) => Action.Action();
                SupportedHostListBox.ContextMenuStrip.Items.Add(Item);
            }

            if (SupportedHostListBox.ContextMenuStrip.Items.Count == 0)
            {
                e.Cancel = true;
                return;
            }

            SupportedHostListBox.ContextMenuStrip.PerformLayout();


            SupportedHostListBox.ContextMenuStrip.Show(SupportedHostListBox, Click);
        }

        private void PostProcessWorker()
        {
            while (true)
            {
                var OriStatus = Status;
                while (PostProcessQueue.Count > 0)
                {
                    if (Status != CurrentLanguage.SavingPages)                    
                        OriStatus = Status;
                    

                    if (Status == CurrentLanguage.IDLE)
                    {
                        Status = CurrentLanguage.ClippingImages;
                    }

                    ThreeStatus = PostProcessQueue.Count > 1;
                    ExtraStatus = string.Format(CurrentLanguage.Reaming, PostProcessQueue.Count);

                    var Image = PostProcessQueue.Dequeue();

                    if (Settings.ImageClipping)
                    {
                        int Tries = 1;
                        while (Tries < 5)
                        {
                            try
                            {
                                Image.Data = BitmapTrim.DoTrim(Image.Data, Tries);
                                break;
                            }
                            catch
                            {
                                Tries++;
                            }
                        }
                    }

                    if (Status != CurrentLanguage.ClippingImages && Status != CurrentLanguage.SavingPages)
                        OriStatus = Status;
                
                    Status = CurrentLanguage.SavingPages;
                    File.WriteAllBytes(Image.Path, Image.Data);
                }

                Thread.Sleep(100);
                if (Status == CurrentLanguage.ClippingImages || Status == CurrentLanguage.SavingPages)
                    Status = OriStatus;

                ThreeStatus = false;
            }
        }

        private void RefreshCoverLink(string ComicDir)
        {
            try
            {
                string Path = null;
                foreach (var Language in Languages)
                {
                    string PossiblePath = System.IO.Path.Combine(Settings.LibraryPath, ComicDir, Language.Index + ".html");
                    if (File.Exists(PossiblePath))
                    {
                        Path = PossiblePath;
                    }
                }

                if (Path != null)
                    CoverBox.Cursor = Cursors.Hand;

                LastIndex = Path;
            }
            catch { }
        }

        private void MainCoverClicked(object sender, EventArgs e)
        {
            if (LastIndex != null)
                System.Diagnostics.Process.Start(LastIndex);
        }

        public void FocusDownloader() => MainTabMenu.SelectTab(DownloaderTab);

        public static IHost[] GetHostsInstances() =>
            (from Asm in AppDomain.CurrentDomain.GetAssemblies()
             from Typ in Asm.GetTypes()
             where typeof(IHost).IsAssignableFrom(Typ) && !Typ.IsInterface
             select (IHost)Activator.CreateInstance(Typ)).OrderBy(x => x.GetPluginInfo().Name).ToArray();


        public static ILanguage[] GetLanguagesInstance() =>
            (from Asm in AppDomain.CurrentDomain.GetAssemblies()
             from Typ in Asm.GetTypes()
             where typeof(ILanguage).IsAssignableFrom(Typ) && !Typ.IsInterface
             select (ILanguage)Activator.CreateInstance(Typ)).OrderBy(x => x.LanguageName).ToArray();

        private void OnTabChanged(object sender, EventArgs e)
        {
            if (MainTabMenu.SelectedTab != LibraryTab)
                return;

            if (!Directory.Exists(Settings.LibraryPath))
                return;

            if (LibraryContainer.Controls.Count != 0)
            {
                LibraryContainer.Focus();
                return;
            }

            foreach (var Comic in Directory.GetDirectories(Settings.LibraryPath))
            {
                Try(() => LibraryContainer.Controls.Add(new ComicPreview(Comic)));
                Thread.Sleep(10);
                Application.DoEvents();
            }

            LibraryContainer.Focus();

            if (Settings.AutoLibUpCheck)
            {
                foreach (var Control in LibraryContainer.Controls)
                    Try(() => ((ComicPreview)Control).GetComicInfo(true));
            }
        }

        public void RefreshLibrary()
        {
            var Controls = new List<Control>(LibraryContainer.Controls.Cast<Control>());
            LibraryContainer.Controls.Clear();
            foreach (var Comic in Controls)
            {
                Comic.Dispose();
            }

            OnTabChanged(null, null);
        }

        Queue<string> LinksFound = new Queue<string>();
        string ParentLink;
        List<string> ProcessedLinks;
        StringBuilder ListString;

        private static bool Try(Action Action)
        {
            try
            {
                Action.Invoke();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void CrawlerStartBtn_Click(object sender, EventArgs e)
        {
            ListString = new StringBuilder();
            ProcessedLinks = new List<string>();
            ParentLink = tbCrawlerUrl.Text.Trim().ToLower().TrimEnd('/');
            LinksFound.Enqueue(tbCrawlerUrl.Text);
        }

        private void CrawlerCopyBtn_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(ListString.ToString());
        }

        public void CrawlerWorker()
        {
            while (true)
            {
                while (LinksFound.Count > 0)
                {
                    Status = Language.Crawling;
                    SubStatus = string.Format(Language.Reaming, LinksFound.Count);

                    try
                    {
                        Uri Link = new Uri(LinksFound.Dequeue());
                        ProcessedLinks.Add(Link.AbsoluteUri);

                        bool Https = Link.AbsoluteUri.Trim().ToLower().StartsWith("https://");
                        string Domain = (Https ? "https://" : "http://") + Link.Host;

                        string HTML = Encoding.UTF8.GetString(Link.TryDownload(UserAgent: ProxyTools.UserAgent));
                        List<string> Links = DataTools.ExtractHtmlLinks(HTML, Domain);

                        Links = (from x in Links where !ProcessedLinks.Contains(x) select x).Distinct().ToList();

                        Invoke(new MethodInvoker(() =>
                        {
                            var List = Links.ToList();
                            string Exp = tbCrawlerRegex.Text;
                            if (!string.IsNullOrEmpty(Exp))
                                List = (from x in List where Regex.IsMatch(x, Exp) select x).ToList();

                            LinksListBox.AddItems(List.ToArray());
                            foreach (var Item in List)
                                ListString.AppendLine(Item);
                        }));

                        foreach (string LinkFound in (from x in Links where !ProcessedLinks.Contains(x) && !LinksFound.Contains(x) select x))
                        {
                            if (LinkFound.Trim().ToLower().StartsWith(ParentLink))
                                LinksFound.Enqueue(LinkFound);
                        }
                    }
                    catch { }

                    Status = Language.IDLE;
                    SubStatus = string.Empty;
                }
                Thread.Sleep(10);
            }
        }

        private void ReplaceAskModeChanged(object sender, EventArgs e)
        {
            if (AskRadio.Checked)
                Settings.ReplaceMode = (int)ReplaceMode.Ask;
        }

        private void ReplaceNewFolderModeChanged(object sender, EventArgs e)
        {
            if (NewFolderRadio.Checked)
                Settings.ReplaceMode = (int)ReplaceMode.NewFolder;
        }

        private void ReplaceUpdateUrlModeChanged(object sender, EventArgs e)
        {
            if (UpdateUrlRadio.Checked)
                Settings.ReplaceMode = (int)ReplaceMode.UpdateURL;
        }

        private void LegacyReaderChanged(object sender, EventArgs e)
        {
            if (LegacyReaderRadio.Checked)
                Settings.ReaderMode = (int)ReaderMode.Legacy;
        }

        private void MangaReaderChanged(object sender, EventArgs e)
        {
            if (MangaReaderRadio.Checked)
                Settings.ReaderMode = (int)ReaderMode.Manga;
        }

        private void ComicReaderChanged(object sender, EventArgs e)
        {
            if (ComicReaderRadio.Checked)
                Settings.ReaderMode = (int)ReaderMode.Comic;
        }

        private void OtherReaderChanged(object sender, EventArgs e)
        {
            if (OtherReaderRadio.Checked)
                Settings.ReaderMode = (int)ReaderMode.Other;
        }

        private void LibUpCheckChanged(object sender, EventArgs e)
        {
            Settings.AutoLibUpCheck = AutoUpCheckRadio.Checked;
        }
    }
}
