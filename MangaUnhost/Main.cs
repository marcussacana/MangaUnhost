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
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using EPubFactory;
using Nito.AsyncEx;

namespace MangaUnhost {
    public partial class Main : Form {

        Thread ClipThread = null;

        Settings Settings = new Settings();

        IHost[] Hosts = GetHostsInstances();

        ILanguage[] Languages = GetLanguagesInstance();

        Queue<string> ClipQueue = new Queue<string>();

        public static string SettingsPath => Program.SettingsPath;

        string DefaultLibPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{CurrentLanguage.Library}\\");

        ILanguage CurrentLanguage = new Languages.English();

        ComicInfo CurrentInfo;

        public static string Status {
            get => Instance.StatusBar.FirstLabelText;
            set {
                Instance.StatusBar.FirstLabelText = value;

                if (Instance.InvokeRequired)
                    Instance.Invoke(new MethodInvoker(() => Application.DoEvents()));
                else
                    Application.DoEvents();
            }
        }

        public static ILanguage Language {
            get {
                return Instance.CurrentLanguage;
            }
        }

        public static CaptchaSolverType Solver => Instance.Settings.AutoCaptcha ? CaptchaSolverType.SemiAuto : CaptchaSolverType.Manual;

        public static Main Instance = null;
        private string LastClipboard = null;
        private string LastIndex = null;

        public Main() {
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

            if (File.Exists(SettingsPath)) {
                AdvancedIni.FastOpen(out Settings, SettingsPath);
            } else {
                Settings = new Settings() {
                    Language = "English",
                    AutoCaptcha = false,
                    ImageClipping = true,
                    ReaderGenerator = true,
                    SkipDownloaded = true,
                    LibraryPath = DefaultLibPath
                };
            }

            ReloadSettings();

            Cef.Initialize(new CefSettings() {
                BrowserSubprocessPath = Program.BrowserSubprocessPath                
            }, false, browserProcessHandler: null);

            this.Shown += (sender, e) =>
            {
                //Prevent Hidden incon in taskbar
                Form frm = new Form()
                {
                    Size = new Size(1, 1)
                };
                frm.Shown += (a, b) => { frm.Close(); };
                frm.ShowDialog();

                TopMost = true;
                Focus();
                TopMost = false;
            };
        }

        private void MainShown(object sender, EventArgs e) {
            TopMost = true;
            Hide();
            Application.DoEvents();
            Show();
            Application.DoEvents();
            BringToFront();
            Application.DoEvents();
            Focus();
            Application.DoEvents();
            TopMost = false;

            ClipThread = new Thread(ClipWorker);
            ClipThread.Start();

            if (!Program.Updater.HaveUpdate())
                return;

            if (MessageBox.Show(CurrentLanguage.UpdateFound, "MangaUnhost", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                Hide();
                Program.Updater.Update();
            }

        }

        private T CreateInstance<T>(T Interface) {
            return (T)Activator.CreateInstance(Interface.GetType());
        }

        private void MainTimerTick(object sender, EventArgs e) {
            CheckClipboard();
        }

        private void CheckClipboard() {
            string Clip = null;
            try {
                Clip = Clipboard.GetText();
            } catch { }

            if (LastClipboard == Clip || Clip == null)
                return;
            LastClipboard = Clip;

            bool Result = Uri.TryCreate(Clip, UriKind.Absolute, out Uri URL);
            if (!Result)
                return;

            foreach (var Host in Hosts) {
                if (!Host.IsValidUri(URL))
                    continue;

                try {
                    LoadUri(URL, Host);
                    break;
                } catch (Exception ex) {
                    if (Program.Debug)
                        throw ex;
                }

                StatusBar.SecondLabelText = string.Empty;
                Status = CurrentLanguage.IDLE;
            }

        }

        public void LoadUri(Uri Uri, IHost Host) {
            Status = CurrentLanguage.LoadingComic;            

            var CurrentHost = CreateInstance(Host);

            CurrentInfo = CurrentHost.LoadUri(Uri);
            TitleLabel.Text = CurrentInfo.Title;
            CoverBox.Image = CurrentHost.GetDecoder().Decode(CurrentInfo.Cover);

            CurrentInfo.Url = Uri;

            CoverBox.Cursor = Cursors.Default;

            string Path = DataTools.GetRawName(CurrentInfo.Title.Trim(), FileNameMode: true);
            ChapterTools.MatchLibraryPath(ref Path, Settings.LibraryPath);
            RefreshCoverLink(Path);

            ButtonsContainer.Controls.Clear();

            var Chapters = new Dictionary<int, string>();
            foreach (var Chapter in CurrentHost.EnumChapters()) {
                VSButton Button = new VSButton() {
                    Size = new Size(110, 30),
                    Text = string.Format(CurrentLanguage.ChapterName, Chapter.Value),
                    Indentifier = CurrentHost
                };

                Button.Click += (sender, args) => {
                    try {
                        IHost HostIsnt = (IHost)((VSButton)sender).Indentifier;
                        DownloadChapter(Chapters, Chapter.Key, HostIsnt, CurrentInfo);
                    } catch (Exception ex){
                        if (Program.Debug)
                            throw ex;
                    }
                    StatusBar.SecondLabelText = string.Empty;
                    Status = CurrentLanguage.IDLE;
                };

                ButtonsContainer.Controls.Add(Button);
                Chapters.Add(Chapter.Key, Chapter.Value);
            }

            if (Chapters.Count > 1) {
                VSButton Bnt = new VSButton() {
                    Size = new Size(110, 30),
                    Text = CurrentLanguage.DownloadAll,
                    Indentifier = CurrentHost
                };

                Bnt.Click += (sender, args) => {
                    foreach (var Chapter in Chapters.Reverse()) {
                        try {
                            IHost HostIsnt = (IHost)((VSButton)sender).Indentifier;
                            DownloadChapter(Chapters, Chapter.Key, HostIsnt, CurrentInfo);
                        } catch (Exception ex){
                            if (Program.Debug)
                                throw;
                        }

                        StatusBar.SecondLabelText = string.Empty;
                        Status = CurrentLanguage.IDLE;
                    }
                };
                ButtonsContainer.Controls.Add(Bnt);
            }

            Status = CurrentLanguage.IDLE;
        }

        private void DownloadChapter(Dictionary<int, string> Chapters, int ID, IHost Host, ComicInfo Info) {
            string Name = DataTools.GetRawName(Chapters[ID], FileNameMode: true);

            StatusBar.SecondLabelText = $"{Info.Title} - {string.Format(CurrentLanguage.ChapterName, Name)}" ;
            Status = CurrentLanguage.Loading;

            int KIndex = Chapters.IndexOfKey(ID);
            string NName = null;
            string NextChapterPath = null;
            if (KIndex > 0)
                NName = DataTools.GetRawName(Chapters.Values.ElementAt(KIndex - 1), FileNameMode: true);

            string Title = DataTools.GetRawName(Info.Title.Trim(), FileNameMode: true);

            ChapterTools.MatchLibraryPath(ref Title, Settings.LibraryPath);
            RefreshCoverLink(Title);

            string TitleDir = Path.Combine(Settings.LibraryPath, Title);
            if (!Directory.Exists(TitleDir))
                Directory.CreateDirectory(TitleDir);

            ChapterTools.GetChapterPath(Languages, CurrentLanguage, TitleDir, Name, out string ChapterPath, false);

            string AbsolueChapterPath = Path.Combine(TitleDir, ChapterPath);

            if (Settings.SkipDownloaded && File.Exists(AbsolueChapterPath.TrimEnd('\\', '/') + ".html"))
                return;

            int PageCount = 1;

            if (Info.ContentType == ContentType.Comic) {
                PageCount = Host.GetChapterPageCount(ID);

                if (Directory.Exists(AbsolueChapterPath) && Directory.GetFiles(AbsolueChapterPath, "*").Length < PageCount)
                    Directory.Delete(AbsolueChapterPath, true);

                if (Settings.SkipDownloaded && Directory.Exists(AbsolueChapterPath))
                {
                    var Pages = (from x in Directory.GetFiles(AbsolueChapterPath) select Path.GetFileName(x)).ToArray();
                    ChapterTools.GenerateComicReader(CurrentLanguage, Pages, NextChapterPath, TitleDir, ChapterPath, Name);
                    return;
                }
            }

            if (Info.ContentType == ContentType.Novel)
            {
                ChapterPath = Path.Combine(AbsolueChapterPath, string.Format(CurrentLanguage.ChapterName, Name) + ".epub");
                if (Settings.SkipDownloaded && File.Exists(ChapterPath))
                    return;
                
            }


            if (!Directory.Exists(AbsolueChapterPath))
                Directory.CreateDirectory(AbsolueChapterPath);

            if (NName != null)
                ChapterTools.GetChapterPath(Languages, CurrentLanguage, TitleDir, NName, out NextChapterPath, false);

            string UrlData = string.Format(Properties.Resources.UrlFile, Info.Url.AbsoluteUri);
            File.WriteAllText(Path.Combine(TitleDir, "Online.url"), UrlData);
            

            switch (Info.ContentType) {
                case ContentType.Comic:
                    var Decoder = Host.GetDecoder();
                    List<string> Pages = new List<string>();
                    foreach (var Data in Host.DownloadPages(ID)) {
                        Status = string.Format(CurrentLanguage.Downloading, Pages.Count + 1, PageCount);
                        Application.DoEvents();

                        try
                        {
                            string PageName = $"{Pages.Count:D3}.png";
                            string PagePath = Path.Combine(TitleDir, ChapterPath, PageName);

                            if ((SaveAs)Settings.SaveAs == SaveAs.RAW) {
                                File.WriteAllBytes(PagePath, Data);
                            } else {
                                using (Bitmap Result = Decoder.Decode(Data)) {
                                    PageName = $"{Pages.Count:D3}.{GetExtension(Result, out ImageFormat Format)}";
                                    PagePath = Path.Combine(TitleDir, ChapterPath, PageName);
                                    Result.Save(PagePath, Format);
                                }
                            }

                            if (Settings.ImageClipping)
                                ClipQueue.Enqueue(PagePath);

                            Pages.Add(PageName);
                        } catch (Exception ex) {
                            if (Program.Debug)
                                throw;
                        }
                    }

                    if (Settings.ReaderGenerator) {
                        ChapterTools.GenerateComicReader(CurrentLanguage, Pages.ToArray(), NextChapterPath, TitleDir, ChapterPath, Name);
                        ChapterTools.GenerateReaderIndex(Languages, CurrentLanguage, Info, TitleDir, Name);
                    }
                    
                    break;
                case ContentType.Novel:                    
                    var Chapter = Host.DownloadChapter(ID);
                    AsyncContext.Run(async () => {
                        using (var Epub = await EPubWriter.CreateWriterAsync(File.Create(ChapterPath), Info.Title ?? "Untitled", Chapter.Author ?? "Anon", Chapter.URL ?? "None"))
                        {
                            Chapter.HTML.RemoveNodes("//script");
                            Chapter.HTML.RemoveNodes("//iframe");

                            foreach (var Node in Chapter.HTML.DocumentNode.DescendantsAndSelf())
                            {
                                if (Node.Name == "img" && Node.GetAttributeValue("src", string.Empty) != string.Empty)
                                {
                                    string Src = Node.GetAttributeValue("src", string.Empty);
                                    string RName = Path.GetFileName(Src.Substring(null, "?", IgnoreMissmatch: true));
                                    string Mime = ResourceHandler.GetMimeType(Path.GetExtension(RName));

                                    if (Node.GetAttributeValue("type", string.Empty) != string.Empty)
                                        Mime = Node.GetAttributeValue("type", string.Empty);

                                    Uri Link = new Uri(Info.Url, Src);

                                    if (string.IsNullOrWhiteSpace(RName))
                                        continue;

                                    byte[] Buffer = await Link.TryDownloadAsync();

                                    if (Buffer == null)
                                        continue;

                                    await Epub.AddResourceAsync(RName, Mime, Buffer);
                                    Node.SetAttributeValue("src", RName);
                                }

                                if (Node.Name == "link" && Node.GetAttributeValue("rel", string.Empty) == "stylesheet" && Node.GetAttributeValue("href", string.Empty) != string.Empty)
                                {
                                    string Src = Node.GetAttributeValue("href", string.Empty);
                                    string RName = Path.GetFileName(Src.Substring(null, "?", IgnoreMissmatch: true));
                                    string Mime = ResourceHandler.GetMimeType(Path.GetExtension(RName));

                                    if (Node.GetAttributeValue("type", string.Empty) != string.Empty)
                                        Mime = Node.GetAttributeValue("type", string.Empty);

                                    Uri Link = new Uri(Info.Url, Src);

                                    if (string.IsNullOrWhiteSpace(RName))
                                        continue;

                                    byte[] Buffer = await Link.TryDownloadAsync();

                                    if (Buffer == null)
                                        continue;

                                    await Epub.AddResourceAsync(RName, Mime, Buffer);
                                    Node.SetAttributeValue("href", RName);
                                }
                            }

                            Epub.Publisher = lblTitle.Text;

                            await Epub.AddChapterAsync("chapter.xhtml", Chapter.Title, Chapter.HTML.ToHTML());


                            await Epub.WriteEndOfPackageAsync();
                        }
                    });
                    break;
                default:
                    throw new Exception("Invalid Content Type");
            }
        }

        private string GetExtension(Bitmap Bitmap, out ImageFormat Format) {
            switch ((SaveAs)Settings.SaveAs) {
                case SaveAs.PNG:
                    Format = ImageFormat.Png;
                    return "png";
                case SaveAs.JPG:
                    Format = ImageFormat.Jpeg;
                    return "jpg";
                case SaveAs.BMP:
                    Format = ImageFormat.Bmp;
                    return "bmp";
                case SaveAs.AUTO:
                    Format = Bitmap.RawFormat;
                    return Bitmap.GetImageExtension();
                default:
                    throw new Exception("Invalid Save As Image Format");
            }
        }

        private void DbgButtonClicked(object sender, EventArgs e) {
            var OBrowser = new ChromiumWebBrowser("https://patrickhlauke.github.io/recaptcha/", new BrowserSettings {
                WebSecurity = CefState.Disabled
            });

            OBrowser.Size = new Size(1280, 720);
            OBrowser.InstallAdBlock();
            OBrowser.HookReCaptcha();

            while (!OBrowser.IsBrowserInitialized) {
                ThreadTools.Wait(50, true);
            }

            ThreadTools.Wait(3000, true);
            DbgPreview.Image = OBrowser.ScreenshotOrNull();
            Application.DoEvents();


            OBrowser.TrySolveCaptcha(CaptchaSolverType.Manual);

            DbgPreview.Image = OBrowser.ScreenshotOrNull();
            MessageBox.Show("Finished");
        }

        private void BntLibSelectClicked(object sender, EventArgs e) {
            var FileDialog = new CommonOpenFileDialog() {
                Multiselect = false,
                IsFolderPicker = true,
                EnsurePathExists = true,                
                InitialDirectory = LibraryPathTBox.Text
            };

            if (FileDialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            LibraryPathTBox.Text = Settings.LibraryPath = FileDialog.FileNames.First();
        }

        private void CaptchaSolveSwitched(object sender, EventArgs e) {
            Settings.AutoCaptcha = SemiAutoCaptchaRadio.Checked;
        }

        private void ClipWatcherSwitched(object sender, EventArgs e) {
            MainTimer.Enabled = ClipWatcherEnbRadio.Checked;
        }

        private void ImgClippingSwitched(object sender, EventArgs e) {
            Settings.ImageClipping = ImgClipEnbRadio.Checked;
        }

        private void ReaderGeneratorSwitched(object sender, EventArgs e) {
            Settings.ReaderGenerator = ReaderGenEnbRadio.Checked;
        }

        private void SkipDownloadedSwitched(object sender, EventArgs e) {
            Settings.SkipDownloaded = SkipDownEnbRadio.Checked;
        }


        private void PNGSaveAs(object sender, EventArgs e) {
            if (SaveAsPngRadio.Checked)
                Settings.SaveAs = (int)SaveAs.PNG;
        }

        private void JPGSaveAs(object sender, EventArgs e) {
            if (SaveAsJpgRadio.Checked)
                Settings.SaveAs = (int)SaveAs.JPG;
        }

        private void BMPSaveAs(object sender, EventArgs e) {
            if (SaveAsBmpRadio.Checked)
                Settings.SaveAs = (int)SaveAs.BMP;
        }

        private void RAWSaveAs(object sender, EventArgs e) {
            if (SaveAsRawRadio.Checked)
                Settings.SaveAs = (int)SaveAs.RAW;
        }

        private void AutoSaveAs(object sender, EventArgs e){
            if (SaveAsRawRadio.Checked)
                Settings.SaveAs = (int)SaveAs.AUTO;
        }

        private void LanguageChanged(object sender, EventArgs e) {
            Settings.Language = LanguageBox.SelectedItem.ToString();
            ReloadSettings();
        }

        private void ReloadSettings() {
            string OriPath = null;
            if (CurrentLanguage != null)
                OriPath = DefaultLibPath;

            CurrentLanguage = (from x in Languages where x.LanguageName == Settings.Language select x).Single();

            for (int i = 0; i < LanguageBox.Items.Count; i++) {
                if (LanguageBox.Items[i].ToString() == CurrentLanguage.LanguageName) {
                    LanguageBox.SelectedIndex = i;
                    break;
                }
            }

            if (!Directory.Exists(OriPath) || Directory.Exists(DefaultLibPath))
                LibraryPathTBox.Text = Settings.LibraryPath = DefaultLibPath;

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

            SaveAs SaveAs = (SaveAs)Settings.SaveAs;
            SaveAsPngRadio.Checked  = SaveAs == SaveAs.PNG;
            SaveAsJpgRadio.Checked  = SaveAs == SaveAs.JPG;
            SaveAsBmpRadio.Checked  = SaveAs == SaveAs.BMP;
            SaveAsRawRadio.Checked  = SaveAs == SaveAs.RAW;
            SaveAsAutoRadio.Checked = SaveAs == SaveAs.AUTO;



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

            ClipWatcherDisRadio.Text = CurrentLanguage.Disabled;
            ImgClipDisRadio.Text = CurrentLanguage.Disabled;
            ReaderGenDisRadio.Text = CurrentLanguage.Disabled;
            SkipDownDisRadio.Text = CurrentLanguage.Disabled;
            ClipWatcherEnbRadio.Text = CurrentLanguage.Enabled;
            ImgClipEnbRadio.Text = CurrentLanguage.Enabled;
            ReaderGenEnbRadio.Text = CurrentLanguage.Enabled;
            SkipDownEnbRadio.Text = CurrentLanguage.Enabled;

            SupportedHostsBox.Text = CurrentLanguage.SupportedHostsBox;

            lblTitle.Text = $"MangaUnhost v{GitHub.CurrentVersion}";
        }

        private void MainClosing(object sender, FormClosingEventArgs e) {
            try {
                AdvancedIni.FastSave(Settings, SettingsPath);
            } catch { }

            Environment.Exit(0);
        }

        private void SupportedHostClicked(object sender, EventArgs e) {
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

            if (SupportedHostListBox.ContextMenuStrip == null) {
            }

            var Actions = Host.GetPluginInfo().Actions;

            if (Actions == null)
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

        private void ClipWorker() {
            while (true) {

                while (ClipQueue.Count > 0) {
                    if (Status == CurrentLanguage.IDLE)
                        Status = CurrentLanguage.ClippingImages;

                    string Image = ClipQueue.Dequeue();

                    int Tries = 1;
                    while (Tries < 5) {
                        try {
                            BitmapTrim.DoTrim(Image, Tries);
                            break;
                        } catch {
                            Tries++;
                        }
                    }
                }

                Thread.Sleep(100);
                if (Status == CurrentLanguage.ClippingImages)
                    Status = CurrentLanguage.IDLE;
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

            LibraryContainer.Controls.Clear();
            foreach (var Comic in Directory.GetDirectories(Settings.LibraryPath))
            {
                LibraryContainer.Controls.Add(new ComicPreview(Comic));
                ThreadTools.Wait(10, true);
            }

            foreach (var Control in LibraryContainer.Controls)
                ((ComicPreview)Control).GetComicInfo();
            
            
        }
    }
}
