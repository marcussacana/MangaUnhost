using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;
using System.Drawing.Imaging;
using Ionic.Zip;
using System.Data;
using MangaUnhost.Others;
using System.Text;
using MangaUnhost.Browser;
using Encoder = System.Drawing.Imaging.Encoder;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using MangaUnhost.Parallelism;
using System.Threading;
using System.Globalization;
using CefSharp.DevTools.Page;
using System.Diagnostics.Eventing.Reader;
using MangaUnhost.Decoders;

namespace MangaUnhost
{
    public partial class ComicPreview : UserControl
    {
        static WCRWindow Reader;
        ~ComicPreview()
        {
            var Img = CoverBox.Image;
            try
            {
                CoverBox?.Dispose();
            }
            catch { }
            try
            {
                Img?.Dispose();
            }
            catch { }
        }

        ILanguage Language => Main.Language;

        bool CoverFound = false;
        bool ChapsFound = false;
        bool IndexFound = false;

        string IndexPath;
        public string ComicPath { get; private set; }
        public string ChapPath { get; private set; }

        string HTML = null;

        bool Error = false;

        ComicInfo ComicInfo;
        IHost ComicHost = null;
        Uri ComicUrl = null;

        public bool Initialized { get; private set; }

        Action<string> OnExportAllAs;

        static Dictionary<string, ComicInfo> InfoCache = new Dictionary<string, ComicInfo>();
        static Dictionary<string, int> CountCache = new Dictionary<string, int>();

        public ComicPreview(string ComicDir, Action<string> OnExportAllAs)
        {
            InitializeComponent();
            SetupLanguagePairs();

            ComicPath += ComicDir;

            lblOpenSite.Text = Language.OpenSite;
            lblDownload.Text = Language.Download;
            lblNewChapters.Text = Language.Loading;
            ConvertTo.Text = Language.ConvertTo;
            ExportAs.Text = Language.ExportAs;
            ExportAllAs.Text = Language.ExportAllAs; 
            OpenDirectory.Text = Language.OpenDirectory;
            OpenChapter.Text = Language.OpenChapter;
            Refresh.Text = Language.Refresh;
            UpdateCheck.Text = Language.CheckUpdates;
            Translate.Text = Language.Translate;

            Refresh.Visible = Main.Config.AutoLibUpCheck;
            UpdateCheck.Visible = !Main.Config.AutoLibUpCheck;

            this.OnExportAllAs = OnExportAllAs;

            Visible = true;

            ParentChanged += OnParentChanged;
        }

        private void OnParentChanged(object sender, EventArgs e)
        {
            if (Parent == null)
                return;
            ParentChanged -= OnParentChanged;
            InitializePreview();
        }

        readonly string[] Langs = new string[] { "EN", "JA", "zh-CN", "RU", "ID", "FR", "IT", "ES", "PT" };

        void SetupLanguagePairs() {
            foreach (var SL in new string[] { "AUTO" }.Concat(Langs)) {
                var SourceLang = new ToolStripMenuItem(SL);
                foreach (var TL in Langs) {
                    if (SL == TL)
                        continue;

                    var TargetLang = new ToolStripMenuItem(TL);
                    TargetLang.Name = SL;
                    TargetLang.Click += (sender, e) =>
                    {
                        var Skip = MessageBox.Show(Language.ForceRetranslation, "MangaUnhost", MessageBoxButtons.YesNo) == DialogResult.Yes;
                        var TLItem = (ToolStripMenuItem)sender;
                        TranslateChapters(TLItem.Name, TLItem.Text, Skip);
                    };

                    SourceLang.DropDownItems.Add(TargetLang);
                }
                Translate.DropDownItems.Add(SourceLang);
            }
        }

        private void InitializePreview()
        {
            foreach (var Language in Main.GetLanguagesInstance())
            {
                string PossibleCoverPath = Path.Combine(ComicPath, Language.Cover) + ".png";
                string PossibleIndexPath = Path.Combine(ComicPath, Language.Index) + ".html";
                string PossibleChapterPath = Path.Combine(ComicPath, Language.Chapters);
                if (File.Exists(PossibleCoverPath))
                {
                    using (var Cover = Image.FromFile(PossibleCoverPath))
                        CoverBox.Image = ResizeKeepingRatio((Bitmap)Cover, CoverBox.Width, CoverBox.Height);
                    CoverFound = true;
                }
                if (File.Exists(PossibleIndexPath))
                {
                    IndexPath = PossibleIndexPath;
                    IndexFound = true;
                }
                if (Directory.Exists(PossibleChapterPath))
                {
                    ChapPath = PossibleChapterPath;
                    ChapsFound = true;
                }
            }


            string UrlPath = Path.Combine(ComicPath, "Online.url");
            if (!File.Exists(UrlPath))
            {
                if (!IndexFound)
                    Invoke(new MethodInvoker(() => Visible = false));
                Initialized = true;
                return;
            }

            ComicMenuStrip.Opening += (sender, args) => JITContextMenu();


            string HostName = null;

            ComicUrl = new Uri(Ini.GetConfig("InternetShortcut", "URL", UrlPath));

            var Hosts = Main.GetHostsInstances();
            var HostQuery = (from x in Hosts where x.IsValidUri(ComicUrl) select x);
            if (!HostQuery.Any())
            {
                HostName = Ini.GetConfig("MangaUnhost", "Host", UrlPath, false);
                if (!string.IsNullOrWhiteSpace(HostName))
                {
                    HostQuery = (from x in Hosts where x.GetPluginInfo().Name == HostName select x);
                }

                if (!HostQuery.Any())
                {
                    HostName = string.Empty;
                    try
                    {
                        HTML = Encoding.UTF8.GetString(ComicUrl.TryDownload(ComicUrl.AbsoluteUri, ProxyTools.UserAgent, AcceptableErrors: new System.Net.WebExceptionStatus[] { System.Net.WebExceptionStatus.ProtocolError }));
                        if (HTML.IsCloudflareTriggered())
                        {
                            HTML = ComicUrl.AbsoluteUri.BypassCloudflare().HTML;
                        }

                        HostQuery = (from x in Hosts 
                                     where x.GetPluginInfo().GenericPlugin && x.IsValidPage(HTML, ComicUrl) 
                                     && Try(() => { 
                                            x.LoadUri(ComicUrl); 
                                            return x.EnumChapters().ToList(); 
                                        })?.Count > 0
                                     select x);
                    }
                    catch
                    {
                    }
                }
            }

            ComicHost = HostQuery.FirstOrDefault();

            if (HostName != null && ComicHost != null)
                Ini.SetConfig("MangaUnhost", "Host", ComicHost.GetPluginInfo().Name, UrlPath);

            Invoke(new MethodInvoker(() => {
                if (!Main.Config.AutoLibUpCheck)
                    lblNewChapters.Text = string.Empty;
            }));
            Initialized = true;
        }

        private T Try<T>(Func<T> Action) where T : class
        {
            try
            {
                return Action.Invoke();
            }
            catch
            {
                return null;
            }
        }

        public void JITContextMenu()
        {
            OpenChapter.Visible = false;
            OpenChapter.DropDownItems.Clear();
            if (ChapsFound)
            {
                List<ToolStripMenuItem> Items = new List<ToolStripMenuItem>();

                var Chapters = Directory.GetDirectories(ChapPath).OrderBy(x => DataTools.ForceNumber(x)).ToArray();
                for (int i = 0; i < Chapters.Length; i++)
                {
                    var ID = i;
                    var Chapter = Chapters[i];
                    var LastChapter = i != 0 ? Chapters[i - 1] : null;

                    string NextChapter = null;
                    if (i + 1 < Chapters.Length)
                        NextChapter = Chapters[i + 1];

                    var ChapName = Path.GetFileName(Chapter.TrimEnd('\\', '/'));
                    
                    var ChapItem = new ToolStripMenuItem(ChapName);

                    ChapItem.Click += (sender, args) =>
                    {

                        if (Main.Reader != ReaderMode.Legacy)
                            Program.EnsureWCR();

                        switch (Main.Reader)
                        {
                            case ReaderMode.Legacy:
                                var HtmlReader = Chapter + ".html";
                                if (File.Exists(HtmlReader))
                                    Process.Start(HtmlReader);
                                break;
                            default:
                                Reader = new WCRWindow(ID, Chapters);
                                Reader.Show();
                                break;
                        }
                    };

                    var Translate = new ToolStripMenuItem(Language.Translate);
                    foreach (var SL in new string[] { "AUTO" }.Concat(Langs))
                    {
                        var SourceLang = new ToolStripMenuItem(SL);
                        foreach (var TL in Langs)
                        {
                            if (SL == TL)
                                continue;

                            var TargetLang = new ToolStripMenuItem(TL);
                            TargetLang.Name = SL;
                            TargetLang.Click += (sender, e) =>
                            {
                                var TLItem = (ToolStripMenuItem)sender;
                                TranslateChapter(TLItem.Name, TLItem.Text, false, Chapter, LastChapter, NextChapter, null);
                            };

                            var TranslateAhead = new ToolStripMenuItem(Language.IncludeNextChapters);
                            TranslateAhead.Click += (sender, e) =>
                            {
                                var TLItem = ((ToolStripMenuItem)sender).GetCurrentParent();
                                TranslateChapters(TLItem.Name, TLItem.Text, true, Chapter);
                            };

                            TargetLang.DropDownItems.Add(TranslateAhead);

                            SourceLang.DropDownItems.Add(TargetLang);
                        }
                        Translate.DropDownItems.Add(SourceLang);
                    }
                    ChapItem.DropDownItems.Add(Translate);

                    if (Chapters.Length > 40)
                    {
                        Items.Add(ChapItem);

                        if (Items.Count >= 30)
                        {
                            ToolStripMenuItem Group = BuildItemGroup(Items);

                            OpenChapter.DropDownItems.Add(Group);
                        }
                    }
                    else
                    {
                        OpenChapter.DropDownItems.Add(ChapItem);
                    }

                    Extensions.SafeDoEvents();
                }

                if (Items.Count > 0)
                {
                    ToolStripMenuItem Group = BuildItemGroup(Items);

                    OpenChapter.DropDownItems.Add(Group);
                }

                OpenChapter.Visible = true;
            }
        }

        private static ToolStripMenuItem BuildItemGroup(List<ToolStripMenuItem> Items)
        {
            var Begin = Items.First().Text;
            var Last = Items.Last().Text;
            var Group = new ToolStripMenuItem(Begin + " - " + Last);

            Group.DropDownItems.AddRange(Items.ToArray());

            Items.Clear();
            return Group;
        }


        public async void GetComicInfo(bool UseCache)
        {
            while (!Initialized)
               await Task.Delay(50);

            try
            {
                if (ComicHost == null && UseCache && !InfoCache.ContainsKey(ComicPath))
                    throw new NullReferenceException();

                if (UseCache && InfoCache.ContainsKey(ComicPath))
                    ComicInfo = InfoCache[ComicPath];
                else
                {
                    try
                    {
                        InfoCache[ComicPath] = ComicInfo = ComicHost.LoadUri(ComicUrl);
                    }
                    catch
                    {
                        if (!IndexFound)
                            Invoke(new MethodInvoker(() => Visible = false));
                    }
                }

                if (!CoverFound)
                {
                    using (var Cover = ComicHost.GetDecoder().Decode(ComicInfo.Cover))
                    {
                        CoverBox.Image = ResizeKeepingRatio(Cover, CoverBox.Width, CoverBox.Height);
                        Cover.Save(Path.Combine(ComicPath, Language.Cover + ".png"));
                        CoverFound = true;
                    }
                }
            }
            catch (Exception ex)
            {
                //Invoke(new MethodInvoker(() => MessageBox.Show(ex.ToString(), "ERROR")));
                Error = true;
            }

            CheckUpdates(UseCache);

            if (IndexFound && Error)
            {
                Visible = true;
                ComicMenuStrip.Enabled = false;
                lblDownload.Visible = false;
                lblNewChapters.Visible = false;
            }
            else if (Error)
                Visible = false;

            if (!IndexFound)
                CoverBox.Cursor = Cursors.Default;
        }

        private void CheckUpdates(bool UseCache)
        {
            if (Error)
                return;

            //Prevent for update check freezes
            ThreadTools.ForceTimeoutAt = DateTime.Now.AddMinutes(1);

            Nito.AsyncEx.AsyncContext.Run(() =>
            {
                try
                {
                    int DownloadedChapters = 0;
                    if (ChapsFound)
                        DownloadedChapters = Directory.GetDirectories(ChapPath).Length;

                    int ChapCount = 0;
                    if (UseCache && CountCache.ContainsKey(ComicPath))
                        ChapCount = CountCache[ComicPath];
                    else {
                        var Chapters = ComicHost.EnumChapters().GroupBy(c => c.Value).Select(x => x.First()).ToArray();
                        CountCache[ComicPath] = ChapCount = Chapters.Length;
                    }

                    int NewChapters = ChapCount - DownloadedChapters;

                    Invoke(new MethodInvoker(() =>
                    {
                        if (NewChapters <= 0)
                            lblNewChapters.Visible = false;

                        lblNewChapters.Text = string.Format(Language.NewChapters, NewChapters);
                    }));
                }
                catch
                {
                    Error = true;
                }
            });

            ThreadTools.ForceTimeoutAt = null;
        }

        private void OpenSiteClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(ComicUrl.AbsoluteUri);
        }

        private void DownloadClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Main.Instance.LoadUri(ComicUrl, ComicHost);
            Main.Instance.FocusDownloader();
        }

        private void CoverClicked(object sender, EventArgs e)
        {
            if (IndexFound)
                Process.Start(IndexPath);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            ((IMouseable)Parent).DoMouseWhell(e);
        }

        private void ExportToJPG_Click(object sender, EventArgs e)
        {
            BeginInvoke(new MethodInvoker(() => ExportChapters("jpg")));
        }

        private void ExportToPNG_Click(object sender, EventArgs e)
        {
            BeginInvoke(new MethodInvoker(() => ExportChapters("png")));
        }

        private void ExportToBMP_Click(object sender, EventArgs e)
        {
            BeginInvoke(new MethodInvoker(() => ExportChapters("bmp")));
        }

        private void CBZExportToJPG_Click(object sender, EventArgs e)
        {
            BeginInvoke(new MethodInvoker(() => CBZExportChapters("jpg")));
        }

        private void CBZExportToPNG_Click(object sender, EventArgs e)
        {
            BeginInvoke(new MethodInvoker(() => CBZExportChapters("png")));
        }

        private void CBZExportToBMP_Click(object sender, EventArgs e)
        {
            BeginInvoke(new MethodInvoker(() => CBZExportChapters("bmp")));
        }

        private void ExportToCBZ_Click(object sender, EventArgs e)
        {
            BeginInvoke(new MethodInvoker(() => CBZExportChapters(null)));
        }


        private void ExportEverythingAs_Clicked(object sender, EventArgs e)
        {
            BeginInvoke(new MethodInvoker(() => OnExportAllAs(null)));
        }

        ShellContainer ChaptersDir => (ShellContainer)ShellObject.FromParsingName(ChapPath);
        private void ExportChapters(string Format, string OutputDir = null)
        {
            if (OutputDir == null)
            {
                OutputDir = SelectDirectory();
                if (OutputDir == null)
                    return;
            }

            var Chapters = Directory.GetDirectories(ChapPath);
            foreach (var Chapter in Chapters)
            {
                ExportChapter(Chapter, OutputDir, Format, Language);
            }
        }

        private static string ExportChapter(string Chapter, string OutputDir, string Format, ILanguage Language, bool SubDir = true, string ProgressMessage = null)
        {
            var Pages = Directory.GetFiles(Chapter);

            if (Pages.Length == 0)
                return null;

            Format = Format.TrimStart('.').ToLower();

            var ChapName = Path.GetFileName(Chapter.TrimEnd('\\', '/'));
            var ChapOutDir = SubDir ? Path.Combine(OutputDir, ChapName) : OutputDir;
            if (!Directory.Exists(ChapOutDir))
                Directory.CreateDirectory(ChapOutDir);

            var decoder = new CommonImage();

            for (int i = 0; i < Pages.Length; i++)
            {
                Main.Status = string.Format(ProgressMessage ?? Main.Language.Exporting, i, Pages.Length);
                Main.SubStatus = ChapName;

                var Page = Pages[i];
                var OutPage = Path.Combine(ChapOutDir, Path.GetFileNameWithoutExtension(Page) + "." + Format);
                Retry(() =>
                {
                    var data = File.ReadAllBytes(Page);
                    using (Image Original = decoder.Decode(data))
                    {
                        ImageFormat OutFormat = DataTools.GetImageFormat(Format);
                        if (OutFormat.Guid == ImageFormat.Jpeg.Guid)
                        {
                            using (EncoderParameters JpgEncoder = new EncoderParameters(1))
                            using (EncoderParameter Parameter = new EncoderParameter(Encoder.Quality, 93L))
                            {
                                ImageCodecInfo JpgCodec = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                                JpgEncoder.Param[0] = Parameter;
                                Original.Save(OutPage, JpgCodec, JpgEncoder);
                            }
                        }
                        else
                            Original.Save(OutPage);
                    }
                });
            }

            Main.Status = Language.IDLE;
            Main.SubStatus = "";
            return ChapOutDir;
        }

        public static bool CBZExport(string ComicPath, string Format, string OutDirectory = null, string NamePrefix = "") {
            var OutDir = OutDirectory ?? SelectDirectory((ShellContainer)ShellObject.FromParsingName(ComicPath), Main.Language);
            if (OutDir == null)
                return false;

            string IndexPath = null, ChapPath = null;
            bool ChapsFound = false, IndexFound = false;

            ILanguage ActiveLang = null;
            foreach (var Language in Main.GetLanguagesInstance())
            {
                string PossibleCoverPath = Path.Combine(ComicPath, Language.Cover) + ".png";
                string PossibleIndexPath = Path.Combine(ComicPath, Language.Index) + ".html";
                string PossibleChapterPath = Path.Combine(ComicPath, Language.Chapters);
                if (File.Exists(PossibleCoverPath))
                {
                    if (File.Exists(PossibleIndexPath))
                    {
                        IndexPath = PossibleIndexPath;
                        IndexFound = true;
                        ActiveLang = Language;
                    }
                    if (Directory.Exists(PossibleChapterPath))
                    {
                        ChapPath = PossibleChapterPath;
                        ChapsFound = true;
                        ActiveLang = Language;
                    }
                }
            }

            if (!ChapsFound || !IndexFound)
                return false;

            var Chapters = Directory.GetDirectories(ChapPath);
            bool OneShot = Chapters.Count() == 1;

            foreach (var Chapter in Chapters)
            {
                var ChapName = Path.GetFileName(Chapter.TrimEnd('\\', '/'));
                var ChapDir = Format == null ? Chapter : ExportChapter(Chapter, OutDir, Format, ActiveLang);
                if (ChapDir == null)
                    continue;

                if (Format != null && Directory.GetFiles(ChapDir).Length == 0)
                    continue;

                Main.Status = ActiveLang.Compressing;
                Main.SubStatus = ChapName;

                var FinalChapName = (OneShot ? Path.GetFileName(ComicPath.TrimEnd(' ', '\\', '/')) : ChapName);
                var Output = Path.Combine(OutDir, NamePrefix + FinalChapName + ".cbz");
                if (File.Exists(Output))
                    return true;

                CreateCBZ(ChapDir, Output);

                if (Format != null)
                    Directory.Delete(ChapDir, true);
            }

            Main.Status = ActiveLang.IDLE;
            Main.SubStatus = "";
            return true;
        }
        public void CBZExportChapters(string Format, string OutDirectory = null, string NamePrefix = "")
        {
            var OutDir = OutDirectory ?? SelectDirectory();
            if (OutDir == null)
                return;

            var Chapters = Directory.GetDirectories(ChapPath);
            foreach (var Chapter in Chapters)
            {
                var ChapName = Path.GetFileName(Chapter.TrimEnd('\\', '/'));
                var ChapDir = Format == null ? Chapter : ExportChapter(Chapter, OutDir, Format, Language);
                if (ChapDir == null)
                    continue;

                if (Format != null && Directory.GetFiles(ChapDir).Length == 0)
                    continue;

                Main.Status = Language.Compressing;
                Main.SubStatus = ChapName;
                
                var Output = Path.Combine(OutDir, NamePrefix + ChapName + ".cbz");
                if (File.Exists(Output))
                    return;

                CreateCBZ(ChapDir, Output);

                if (Format != null)
                    Directory.Delete(ChapDir, true);
            }

            Main.Status = Language.IDLE;
            Main.SubStatus = "";
        }

        private static void CreateCBZ(string InputDir, string Output)
        {
            using (ZipFile Zip = new ZipFile())
            {
                var Pages = Directory.GetFiles(InputDir)
                    .Where(x => !x.EndsWith(".tl.png") && !x.EndsWith(".tl" + Path.GetExtension(x)))
                    .ToArray();

                Zip.AddFiles(Pages, "");
                Zip.CompressionMethod = CompressionMethod.BZip2;
                Zip.Save(Output);
            }
        }

        private string SelectDirectory() => SelectDirectory((ShellContainer)ShellObject.FromParsingName(ChapPath), Language);

        public static string SelectDirectory(ShellContainer SContainer, ILanguage Language = null)
        {
            using (var Container = SContainer)
            using (CommonOpenFileDialog SaveAs = new CommonOpenFileDialog())
            {
                SaveAs.AddPlace(Container, FileDialogAddPlaceLocation.Top);
                SaveAs.AddPlace(KnownFolders.Desktop as ShellContainer, FileDialogAddPlaceLocation.Top);
                SaveAs.Title = Language.SelectASaveDir ?? "Select a Directory";
                SaveAs.ShowPlacesList = true;
                SaveAs.IsFolderPicker = true;
                SaveAs.EnsurePathExists = true;

                var DR = SaveAs.ShowDialog();
                if (DR != CommonFileDialogResult.Ok)
                    return null;

                return SaveAs.FileName;
            }
        }

        private void ConvertToJPG_Click(object sender, EventArgs e)
        {
            BeginInvoke(new MethodInvoker(() => ConvertChapters("jpg")));
        }

        private void ConvertToPNG_Click(object sender, EventArgs e)
        {
            BeginInvoke(new MethodInvoker(() => ConvertChapters("png")));
        }
        private void ConvertToBMP_Click(object sender, EventArgs e)
        {
            BeginInvoke(new MethodInvoker(() => ConvertChapters("bmp")));
        }

        private void ConvertChapters(string Format)
        {
            var Chapters = Directory.GetDirectories(ChapPath).OrderBy(x => DataTools.ForceNumber(x)).ToArray();
            for (int i = 0; i < Chapters.Length; i++)
            {
                string LastChapter = null;
                string Chapter = Chapters[i];
                string NextChapter = null;

                if (i > 0)
                    LastChapter = Chapters[i - 1];

                if (i + 1 < Chapters.Length)
                    NextChapter = Chapters[i + 1];

                ConvertChapter(Chapter, LastChapter, NextChapter, Format);
            }

            Main.Status = Language.IDLE;
            Main.SubStatus = "";
        }

        private void ConvertChapter(string Chapter, string LastChapter, string NextChapter, string Format)
        {
            var ChapName = Path.GetFileName(Chapter.TrimEnd('\\', '/'));
            var TmpChapter = Path.Combine(ChapPath, "tmp", ChapName);
            var ChapReader = Path.Combine(ChapPath, ChapName + ".html");

            if (!Directory.Exists(Path.GetDirectoryName(TmpChapter)))
                Directory.CreateDirectory(Path.GetDirectoryName(TmpChapter));

            if (Directory.Exists(TmpChapter))
                Directory.Delete(TmpChapter, true);

            Directory.Move(Chapter, TmpChapter);
            bool OK = Retry(() => ExportChapter(TmpChapter, ChapPath, Format, Language, true, Language.Converting));

            if (!OK)
            {
                Directory.Delete(Chapter, true);
                Directory.Move(TmpChapter, Chapter);
                Directory.Delete(Path.GetDirectoryName(TmpChapter), true);
                return;
            }

            Directory.Delete(TmpChapter, true);
            Directory.Delete(Path.GetDirectoryName(TmpChapter), true);

            if (!File.Exists(ChapReader))
                return;

            var Pages = (from x in Directory.GetFiles(Chapter) select Path.GetFileName(x))
                .Where(x => !x.EndsWith(".tl" + Path.GetExtension(x)))
                .OrderBy(x => DataTools.ForceNumber(x)).ToArray();

            var TlPages = (from x in Directory.GetFiles(Chapter) select Path.GetFileName(x))
                .Where(x => x.EndsWith(".tl" + Path.GetExtension(x)))
                .OrderBy(x => DataTools.ForceNumber(x)).ToArray();

            if (TlPages.Length == 0)
            {
                ChapterTools.GenerateComicReader(Language, Pages, LastChapter, NextChapter, ChapPath, Chapter, ChapName);
            }
            else
            {
                ChapterTools.GenerateComicReaderWithTranslation(Language, Pages, TlPages, LastChapter, NextChapter, Chapter);
            }

        }

        static IPacket[] Translators = null;
        async void TranslateChapters(string SourceLang, string TargetLang, bool AllowSkip, string SkipUntil = null)
        {
            var Chapters = Directory.GetDirectories(ChapPath)
                .OrderBy(x => DataTools.ForceNumber(Path.GetFileName(x.TrimEnd('/', '\\')))).ToArray();

            for (int i = 0; i < Chapters.Length; i++)
            {
                string Chapter = Chapters[i];

                if (SkipUntil != null && Chapter != SkipUntil)
                    continue;
                else
                    SkipUntil = null;

                string LastChapter = i == 0 ? null : Chapters[i - 1];
                string NextChapter = i + 1 < Chapters.Length ? Chapters[i + 1] : null;

                var Finished = false;

                AllowSkip = await TranslateChapter(SourceLang, TargetLang, AllowSkip, Chapter, LastChapter, NextChapter, () => Finished = true);

                while (!Finished)
                {
                    await Task.Delay(100);
                }
            }

            foreach (var Translator in Translators)
            {
                Translator?.Dispose();
            }

            Main.Status = Language.IDLE;
            Main.SubStatus = "";

            Main.Instance.Invoke(new MethodInvoker(() =>
            {
                MessageBox.Show(Language.TaskCompleted, "MangaUnhost", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }));
        }


        static new Dictionary<string, int> FailTlMap = new Dictionary<string, int>();

        private async Task<bool> TranslateChapter(string SourceLang, string TargetLang, bool AllowSkip, string Chapter, string LastChapter, string NextChapter, Action OnFinish, int Retries = 3)
        {
            var OriStatus = Main.Status;
            try { 
                if (Translators == null)
                {
                    var Count = Math.Max(Main.Config.TLConcurrency, 1);
                    
                    if (Program.MTLAvailable)
                        Count = Math.Max(Count/2, 1);

                    Translators = new IPacket[Count];
                    TlSemaphore = new SemaphoreSlim(Translators.Length);
                    TlCheckSemaphore = new SemaphoreSlim(1);
                }

                var Pages = ListFiles(Chapter, "*.png", "*.jpg", "*.gif", "*.jpeg", "*.bmp", ".webp")
                                    .Where(x => !x.EndsWith(".tl.png"))
                                    .OrderBy(x => int.TryParse(Path.GetFileNameWithoutExtension(x), out int val) ? val : 0).ToArray();

                var ReadyPages = ListFiles(Chapter, "*.png", "*.jpg", "*.gif", "*.jpeg", "*.bmp", ".webp")
                    .Where(x => x.EndsWith(".tl.png"))
                    .OrderBy(x => int.TryParse(Path.GetFileNameWithoutExtension(x), out int val) ? val : 0).ToArray();

                if (ReadyPages.Length == 0)
                    AllowSkip = false;

                Main.Status = Language.Loading;
                Main.SubStatus = Path.GetFileName(Chapter.TrimEnd('/', '\\'));

                int Translated = 0;
                int Failed = 0;

                Parallel.For(0, Pages.Length, new ParallelOptions()
                {
                    MaxDegreeOfParallelism = Translators.Length
                }, TranslatePage(SourceLang, TargetLang, AllowSkip, Pages, ReadyPages, (i) => Translated++, (i) => { 
                    if (!FailTlMap.ContainsKey(Pages[i]))
                        FailTlMap[Pages[i]] = 0;
                    FailTlMap[Pages[i]]++;
                    Failed++;
                }));

                int LastProgressCheck = 0;
                DateTime LastChanged = DateTime.Now;

                while (Translated + Failed < Pages.Length && (DateTime.Now - LastChanged).TotalMinutes < (Program.MTLAvailable ? 10 : 5))
                {
                    if (LastProgressCheck != Translated)
                    {
                        LastProgressCheck = Translated;
                        LastChanged = DateTime.Now;
                        Main.Status = string.Format(Language.Translating, $"({Translated}/{Pages.Length})");
                    }
                    await Task.Delay(100);
                }

                var NewReadyPages = Pages.Where(x => File.Exists(x + ".tl.png")).ToArray();

                if (NewReadyPages.Length != Pages.Length) {

                    //If this loop interation was able to translate any page
                    //but still missing pages then dont decrease the retries.
                    if (NewReadyPages.Length != ReadyPages.Length)
                        Retries++;

                    int pageRetries = Program.MTLAvailable ? 2 : 3;

                    var MissingPages = Pages.Except(NewReadyPages).ToArray();
                    var TranslatableMissingPages = MissingPages.Where(x => !FailTlMap.ContainsKey(x) || FailTlMap[x] < pageRetries).ToArray();

                    if (TranslatableMissingPages.Any())
                    {
                        PageTranslator.DisposeAll();

                        //Pages without text might never be successuflly translated
                        //causing a infinite loop without retries.
                        if (Retries > 0)
                            await TranslateChapter(SourceLang, TargetLang, true, Chapter, LastChapter, NextChapter, OnFinish, Retries - 1);
                        else
                            OnFinish?.Invoke();

                        return AllowSkip;
                    }   
                }

                ReadyPages = Pages.Select(x => x + ".tl.png").ToArray();

                ChapterTools.GenerateComicReaderWithTranslation(Language, Pages, ReadyPages, LastChapter, NextChapter, Chapter);

                PageTranslator.DisposeAll();

                OnFinish?.Invoke();
                return AllowSkip;
            }
            finally
            {
                Main.Status = OriStatus;
                Main.SubStatus = string.Empty;
            }
        }

        static SemaphoreSlim TlSemaphore = null;
        static SemaphoreSlim TlCheckSemaphore = null;

        private Action<int> TranslatePage(string SourceLang, string TargetLang, bool AllowSkip, string[] Pages, string[] ReadyPages, Action<int> OnPageReady, Action<int> OnPageFailed)
        {
            return async (i) =>
            {
                await TlSemaphore.WaitAsync();
                try
                {
                    for (int x = 0; x < (Program.MTLAvailable ? 1 : 4); x++)
                    {
                        var Page = Pages[i];

                        var ReadyExists = ReadyPages.Contains(Page + ".tl.png") || ReadyPages.Contains(Page + ".tl.jpg");
                        if (ReadyExists && AllowSkip)
                            break;

                        //If user does not allow skip, means that want retranslate it
                        //let's optimize by skipping images that seems to be translated
                        //it will retranslate some images as well but should be help
                        //to skip images that is really translated already.
                        if (ReadyExists)
                        {
                            await TlCheckSemaphore.WaitAsync();
                            try
                            {
                                using var ImgA = Bitmap.FromFile(Page);
                                using var ImgB = Bitmap.FromFile(Page + ".tl.png");

                                if (!ImgA.AreImagesSimilar(ImgB))
                                {
                                    if (!Main.Config.UseAForge && !ImgA.OpenCV_SSIM_AreImageSimilar(ImgB))
                                        break;
                                }
                            }
                            catch { }
                            finally
                            {
                                TlCheckSemaphore.Release();
                            }
                            
                        }

                        IPacket Translator = null;

                        bool OK = false;
                        try
                        {
                            var ImgData = File.ReadAllBytes(Page);

                            if (Page.ToLower().EndsWith(".webp"))
                            {
                                using var data = new CommonImage().Decode(ImgData);
                                using var ms = new MemoryStream();
                                data.Save(ms, ImageFormat.Png);
                                ImgData = ms.ToArray();
                                File.WriteAllBytes(Page, ImgData);
                            }
                            
                            bool IsBig = PageTranslator.IsImageTooBig(ImgData, out int Delay);

                            Translator = await GetTranslator();

                            await Translator.Request(new string[] { Page }, SourceLang, TargetLang);

                            DateTime Begin = DateTime.Now;

                            var awaiter = Translator.WaitForEnd(Delay, (i, total) => { 
                                //Translator?.Dispose();
                            });

                            if (AllowSkip)
                            {
                                while (!File.Exists(Page + ".tl.png"))
                                {
                                    if ((DateTime.Now - Begin).TotalSeconds > 60 * Math.Max(Delay, 1))
                                        break;

                                    if (awaiter.IsCompleted || awaiter.IsFaulted || awaiter.IsCanceled)
                                        break;

                                    await Task.Delay(1000);
                                }
                            }

                            OK = await awaiter;
                        }
                        catch { }
                        finally
                        {
                            if (!OK)
                            {
                                Translator?.Dispose();
                            }
                        }

                        if (!File.Exists(Page + ".tl.png"))
                            continue;

                        break;
                    }
                }
                finally
                {
                    TlSemaphore.Release();

                    lock (this)
                    {
                        if (File.Exists(Pages[i] + ".tl.png"))
                            OnPageReady?.Invoke(i);
                        else 
                            OnPageFailed?.Invoke(i);
                    }
                }

            };
        }
        private async Task<IPacket> GetTranslator()
        {
            IPacket Translator = null;

            if (GetNullTranslators().Any())
            {
                var Info = GetNullTranslators().First();

                Translator = await Server.Run(Server.HandlerType.PageTranslate);

                Translators[Info.Item2]?.Dispose();
                Translators[Info.Item2] = Translator;
            }
            else
            {
                while (!GetFreeTranslators().Any())
                {
                    await Task.Delay(100);
                }

                var Info = GetFreeTranslators().First();
                Translator = Info.Item1;
            }

            return Translator;
        }

        private IEnumerable<(IPacket,int)> GetNullTranslators()
        {
            return Translators.Select((x, i) => (x, i)).Where(x => x.x == null || !x.x.PipeStream.IsConnected || x.x.Disposed);
        }
        private IEnumerable<(IPacket, int)> GetFreeTranslators()
        {
            return Translators.Select((x, i) => (x, i)).Where(x => x.x != null && !x.x.Busy && x.x.PipeStream.IsConnected && !x.x.Disposed);
        }

        private string[] ListFiles(string Dir, params string[] Filters) {
            List<string> Files = new List<string>();
            foreach (var Filter in Filters)
                Files.AddRange(Directory.GetFiles(Dir, Filter));
            return Files.ToArray();
        }

        private static bool Retry(Action Operation, int Times = 3)
        {
            try
            {
                Operation.Invoke();
                return true;
            }
            catch
            {
                ThreadTools.Wait(1000, true);
                if (Times >= 0)
                    return Retry(Operation, Times - 1);

                return false;
            }
        }

        private void OpenDirectory_Click(object sender, EventArgs e)
        {
            Process.Start(ComicPath);
        }

        public Bitmap ResizeKeepingRatio(Bitmap source, int width, int height)
        {
            Bitmap result = null;

            try
            {
                if (source.Width != width || source.Height != height)
                {
                    // Resize image
                    float sourceRatio = (float)source.Width / source.Height;

                    using (var target = new Bitmap(width, height, PixelFormat.Format24bppRgb))
                    {
                        using (var g = Graphics.FromImage(target))
                        {
                            g.CompositingQuality = CompositingQuality.HighSpeed;
                            g.InterpolationMode = InterpolationMode.Default;
                            g.SmoothingMode = SmoothingMode.HighSpeed;

                            // Scaling
                            float scaling;
                            float scalingY = (float)source.Height / height;
                            float scalingX = (float)source.Width / width;
                            if (scalingX < scalingY) scaling = scalingX; else scaling = scalingY;

                            int newWidth = (int)(source.Width / scaling);
                            int newHeight = (int)(source.Height / scaling);

                            // Correct float to int rounding
                            if (newWidth < width) newWidth = width;
                            if (newHeight < height) newHeight = height;

                            // See if image needs to be cropped
                            int shiftX = 0;
                            int shiftY = 0;

                            if (newWidth > width)
                            {
                                shiftX = (newWidth - width) / 2;
                            }

                            if (newHeight > height)
                            {
                                shiftY = (newHeight - height) / 2;
                            }

                            // Draw image
                            g.DrawImage(source, -shiftX, -shiftY, newWidth, newHeight);
                        }

                        result = (Bitmap)target.Clone();
                    }
                }
                else
                {
                    // Image size matched the given size
                    result = (Bitmap)source.Clone();
                }
            }
            catch (Exception)
            {
                result = null;
            }

            return result;
        }

        private void Refresh_Click(object sender, EventArgs e)
        {
            CountCache.Clear();
            InfoCache.Clear();
            Main.Instance.RefreshLibrary();
        }

        private void UpdateCheck_Click(object sender, EventArgs e)
        {
            lblNewChapters.Text = Language.Loading;
            GetComicInfo(false);
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            var Title = Path.GetFileName(ComicPath.TrimEnd('/', '\\'));
            var Result = MessageBox.Show(string.Format(Language.ConfirmDelete, Title), Title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (Result == DialogResult.Yes)
            {
                Hide();

                try
                {
                    Directory.Delete(ComicPath, true);
                }
                catch { }
            }
        }
    }
}
