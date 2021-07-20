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
using System.Threading;
using System.Web.WebSockets;

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

            Translate.Visible = Program.MTLAvailable;
            if (Program.MTLAvailable)
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

            var Initializer = new Task(InitializePreview);

            Initializer.Start();
        }

        readonly string[] Langs = new string[] { "EN", "JA", "ZH", "RU", "FR", "IT", "ES", "PT" };

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
                        var TLItem = (ToolStripMenuItem)sender;
                        TranslateChapters(TLItem.Name, TLItem.Text);
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


            ComicUrl = new Uri(Ini.GetConfig("InternetShortcut", "URL", UrlPath));

            var Hosts = Main.GetHostsInstances();
            var HostQuery = (from x in Hosts where x.IsValidUri(ComicUrl) select x);
            if (!HostQuery.Any())
            {
                try
                {
                    HTML = Encoding.UTF8.GetString(ComicUrl.TryDownload(ComicUrl.Host, ProxyTools.UserAgent));
                    HostQuery = (from x in Hosts where x.GetPluginInfo().GenericPlugin && x.IsValidPage(HTML, ComicUrl) select x);

                }
                catch { }
            }

            ComicHost = HostQuery.FirstOrDefault();

            Invoke(new MethodInvoker(() => {
                if (!Main.Config.AutoLibUpCheck)
                    lblNewChapters.Text = string.Empty;
            }));
            Initialized = true;
        }

        public void JITContextMenu()
        {
            OpenChapter.Visible = false;
            OpenChapter.DropDownItems.Clear();
            if (ChapsFound)
            {
                var Chapters = Directory.GetDirectories(ChapPath).OrderBy(x => ForceNumber(x)).ToArray();
                for (int i = 0; i < Chapters.Length; i++)
                {
                    var ID = i;
                    var Chapter = Chapters[i];

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

                    if (Program.MTLAvailable)
                    {
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
                                    TranslateChapter(Chapter, TLItem.Name, TLItem.Text);
                                };
                                SourceLang.DropDownItems.Add(TargetLang);
                            }
                            Translate.DropDownItems.Add(SourceLang);
                        }
                        ChapItem.DropDownItems.Add(Translate);
                    }

                    OpenChapter.DropDownItems.Add(ChapItem);
                }
                OpenChapter.Visible = true;
            }
        }

        private double ForceNumber(string Str)
        {
            Str = Path.GetFileName(Str);
            string Numbers = string.Empty;
            foreach (var Char in Str)
            {
                if (Char == '.' || Char == ',' || Char == 'v' || Char == 'V')
                    Numbers += '.';

                if (!char.IsNumber(Char))
                    continue;

                Numbers += Char;
            }
            try
            {
                return double.Parse(Numbers, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            catch
            {
                Str = Str.Replace(Language.ChapterName.Replace("{0}", "").Trim(), "").Trim();

                //basically alphabetical order in an unusual way
                var Factor = 0.0;

                foreach (var c in Str.Reverse())
                {
                    Factor += c;
                    Factor /= 100;
                }

                return Factor;
            }
        }

        public void GetComicInfo(bool UseCache)
        {
            while (!Initialized)
                Nito.AsyncEx.AsyncContext.Run(async () => await Task.Delay(50));

            try
            {
                if (ComicHost == null && UseCache && !InfoCache.ContainsKey(ComicPath))
                    throw new NullReferenceException();

                if (UseCache && InfoCache.ContainsKey(ComicPath))
                    ComicInfo = InfoCache[ComicPath];
                else
                {

                    Nito.AsyncEx.AsyncContext.Run(() =>
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
                    });
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
            ThreadTools.ForceTimeoutAt = DateTime.Now.AddMinutes(2);

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
                    else
                        CountCache[ComicPath] = ChapCount = ComicHost.EnumChapters().Count();

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

            for (int i = 0; i < Pages.Length; i++)
            {
                Main.Status = string.Format(ProgressMessage ?? Main.Language.Exporting, i, Pages.Length);
                Main.SubStatus = ChapName;

                var Page = Pages[i];
                var OutPage = Path.Combine(ChapOutDir, Path.GetFileNameWithoutExtension(Page) + "." + Format);
                Retry(() =>
                {
                    using (Image Original = Image.FromFile(Page))
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
                Zip.AddDirectory(InputDir, "/");
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
            var Chapters = Directory.GetDirectories(ChapPath).OrderBy(x => Path.GetFileName(x).Substring(" ", IgnoreMissmatch: true)).ToArray();
            for (int i = 0; i < Chapters.Length; i++)
            {
                string Chapter = Chapters[i];
                string NextChapter = null;
                if (i + 1 < Chapters.Length)
                    NextChapter = Chapters[i + 1];

                ConvertChapter(Chapter, NextChapter, Format);
            }

            Main.Status = Language.IDLE;
            Main.SubStatus = "";
        }

        private void ConvertChapter(string Chapter, string NextChapter, string Format)
        {
            var ChapName = Path.GetFileName(Chapter.TrimEnd('\\', '/'));
            var TmpChapter = Path.Combine(ChapPath, "tmp", ChapName);
            var ChapReader = Path.Combine(ChapPath, ChapName + ".html");

            if (!Directory.Exists(Path.GetDirectoryName(TmpChapter)))
                Directory.CreateDirectory(Path.GetDirectoryName(TmpChapter));

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

            var Pages = (from x in Directory.GetFiles(Chapter) select Path.GetFileName(x)).ToArray();
            ChapterTools.GenerateComicReader(Language, Pages, NextChapter, ChapPath, Chapter, ChapName);
        }

        void TranslateChapters(string SourceLang, string TargetLang)
        {
            var Chapters = Directory.GetDirectories(ChapPath);
            foreach (var Chapter in Chapters) {
                TranslateChapter(Chapter, SourceLang, TargetLang);
            }
        }

        private void TranslateChapter(string Chapter, string SourceLang, string TargetLang)
        {
            var Pages = ListFiles(Chapter, "*.png", "*.jpg", "*.gif", "*.jpeg", "*.bmp").OrderBy(x=> int.TryParse(Path.GetFileNameWithoutExtension(x), out int val) ? val : 0);
            
            foreach (var Page in Pages) {
                var NewPage = Path.Combine(Path.GetDirectoryName(Program.MTLPath), Path.GetFileName(Page));
                File.Copy(Page, NewPage, true);

                if (File.Exists(Page + ".bak"))
                    continue;

                for (int i = 3; i >= 0 && !File.Exists(NewPage + ".out.png"); i--)
                {
                    var StartInfo = new ProcessStartInfo(Program.PythonPath);
                    StartInfo.Arguments = $"{Path.GetFileName(Program.MTLPath)} --image \"{Path.GetFileName(Page)}\" --output \"{Path.GetFileName(Page)}.out.png\" --sourcelang {SourceLang} --targetlang {TargetLang} --use-inpainting"; // --use-cuda";
                    StartInfo.WorkingDirectory = Path.GetDirectoryName(Program.MTLPath);

                    var Proc = Process.Start(StartInfo);

                    while (!Proc.WaitForExit(100))
                        Application.DoEvents();
                }

                if (!File.Exists(NewPage + ".out.png"))
                    continue;

                if (!File.Exists(Page + ".bak"))
                    File.Move(Page, Page + ".bak");
                else
                    File.Delete(Page);

                File.Move(NewPage + ".out.png", Page);
                File.Delete(NewPage);
            }
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
                            g.CompositingQuality = CompositingQuality.HighQuality;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = SmoothingMode.HighQuality;

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
    }
}
