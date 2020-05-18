using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace MangaUnhost
{
    public partial class ComicPreview : UserControl
    {
        ~ComicPreview() {
            var Img = CoverBox.Image;
            CoverBox?.Dispose();
            Img?.Dispose();
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


        static Dictionary<string, ComicInfo> InfoCache = new Dictionary<string, ComicInfo>();
        static Dictionary<string, int> CountCache = new Dictionary<string, int>();

        public ComicPreview(string ComicDir)
        {
            InitializeComponent();
            ComicPath += ComicDir;

            lblOpenSite.Text = Language.OpenSite;
            lblDownload.Text = Language.Download;
            lblNewChapters.Text = Language.Loading;

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
        }

        public void GetComicInfo()
        {
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
            catch {
                Error = true;
            }

            CheckUpdates();

            if (CoverFound && IndexFound && Error)
            {
                Visible = true;
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

                    int NewChapters =  ChapCount - DownloadedChapters;

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
    }
}
