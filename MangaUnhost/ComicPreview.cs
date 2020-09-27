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
        string ComicPath;
        string ChapPath;
        string IndexPath;

        string HTML = null;

        bool Error = false;

        ComicInfo ComicInfo;
        IHost ComicHost = null;
        Uri ComicUrl = null;

        public bool Initialized { get; private set; }


        static Dictionary<string, ComicInfo> InfoCache = new Dictionary<string, ComicInfo>();
        static Dictionary<string, int> CountCache = new Dictionary<string, int>();

        public ComicPreview(string ComicDir)
        {
            InitializeComponent();
            ComicPath += ComicDir;

            lblOpenSite.Text = Language.OpenSite;
            lblDownload.Text = Language.Download;
            lblNewChapters.Text = Language.Loading;
            ConvertTo.Text = Language.ConvertTo;
            ExportAs.Text = Language.ExportAs;
            OpenDirectory.Text = Language.OpenDirectory;
            OpenChapter.Text = Language.OpenChapter;
            Refresh.Text = Language.Refresh;

            Visible = true;

            var Initializer = new Task(InitializePreview);

            Initializer.Start();
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
                    OpenChapter.DropDownItems.Add(ChapName, null, (sender, args) =>
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
                    });
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

        public void GetComicInfo()
        {
            while (!Initialized)
                Nito.AsyncEx.AsyncContext.Run(async () => await Task.Delay(50));

            try
            {
                if (ComicHost == null && !InfoCache.ContainsKey(ComicPath))
                    throw new NullReferenceException();

                if (InfoCache.ContainsKey(ComicPath))
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

            CheckUpdates();

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

        private void CheckUpdates()
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
                    if (CountCache.ContainsKey(ComicPath))
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
            System.Diagnostics.Process.Start(ComicUrl.AbsoluteUri);
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
                ExportChapter(Chapter, OutputDir, Format);
            }
        }

        private string ExportChapter(string Chapter, string OutputDir, string Format, string ProgressMessage = null)
        {
            var Pages = Directory.GetFiles(Chapter);

            if (Pages.Length == 0)
                return null;

            Format = Format.TrimStart('.').ToLower();

            var ChapName = Path.GetFileName(Chapter.TrimEnd('\\', '/'));
            var ChapOutDir = Path.Combine(OutputDir, ChapName);
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

        private void CBZExportChapters(string Format)
        {
            var OutDir = SelectDirectory();
            if (OutDir == null)
                return;

            var Chapters = Directory.GetDirectories(ChapPath);
            foreach (var Chapter in Chapters)
            {
                var ChapName = Path.GetFileName(Chapter.TrimEnd('\\', '/'));
                var ChapDir = Format == null ? Chapter : ExportChapter(Chapter, OutDir, Format);
                if (ChapDir == null)
                    continue;

                if (Format != null && Directory.GetFiles(ChapDir).Length == 0)
                    continue;

                Main.Status = Language.Compressing;
                Main.SubStatus = ChapName;
                CreateCBZ(ChapDir, Path.Combine(OutDir, ChapName + ".cbz"));

                if (Format != null)
                    Directory.Delete(ChapDir, true);
            }

            Main.Status = Language.IDLE;
            Main.SubStatus = "";
        }
        private void CreateCBZ(string InputDir, string Output)
        {
            using (ZipFile Zip = new ZipFile())
            {
                Zip.AddDirectory(InputDir, "/");
                Zip.CompressionMethod = CompressionMethod.BZip2;
                Zip.Save(Output);
            }
        }

        private string SelectDirectory()
        {
            using (var Container = ChaptersDir)
            using (CommonOpenFileDialog SaveAs = new CommonOpenFileDialog())
            {
                SaveAs.AddPlace(Container, FileDialogAddPlaceLocation.Top);
                SaveAs.AddPlace(KnownFolders.Desktop as ShellContainer, FileDialogAddPlaceLocation.Top);
                SaveAs.Title = Language.SelectASaveDir;
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
            bool OK = Retry(() => ExportChapter(TmpChapter, ChapPath, Format, Language.Converting));

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

        private bool Retry(Action Operation, int Times = 3)
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
            System.Diagnostics.Process.Start(ComicPath);
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
    }
}
