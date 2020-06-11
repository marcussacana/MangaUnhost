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

namespace MangaUnhost
{
    public partial class ComicPreview : UserControl
    {
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
                    CoverBox.Image = Image.FromFile(PossibleCoverPath);
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
                Invoke(new MethodInvoker(() => Visible = false));
                Initialized = true;
                return;
            }


            ComicUrl = new Uri(Ini.GetConfig("InternetShortcut", "URL", UrlPath));

            var Hosts = Main.GetHostsInstances();
            foreach (var Host in Hosts)
            {
                if (!Host.IsValidUri(ComicUrl))
                    continue;
                ComicHost = Host;
                break;
            }

            Initialized = true;
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
                            Invoke(new MethodInvoker(() => Visible = false));
                        }
                    });
                }

                if (!CoverFound)
                {
                    CoverBox.Image = ComicHost.GetDecoder().Decode(ComicInfo.Cover);
                    CoverBox.Image.Save(Path.Combine(ComicPath, Language.Cover + ".png"));
                    CoverFound = true;
                }
            }
            catch (Exception ex)
            {
                //Invoke(new MethodInvoker(() => MessageBox.Show(ex.ToString(), "ERROR")));
                Error = true;
            }

            CheckUpdates();

            if (CoverFound && IndexFound && Error)
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
                System.Diagnostics.Process.Start(IndexPath);
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
                            using (EncoderParameter Parameter = new EncoderParameter(Encoder.Quality, 93L)) {
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
    }
}
