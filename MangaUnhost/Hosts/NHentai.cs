using CefSharp.OffScreen;
using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MangaUnhost.Hosts {
    class NHentai : IHost {
        static string CurrentUrl;
        static ChromiumWebBrowser Browser = null;

        Dictionary<int, string> ChapterLinks = new Dictionary<int, string>();

        public string DownloadChapter(int ID) {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID) {
            foreach (var Page in GetChapterPages(ID)) {
                yield return TryDownload(new Uri(Page));
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters() {
            int ID = ChapterLinks.Count;
            ChapterLinks[ID] = CurrentUrl;
            yield return new KeyValuePair<int, string>(ID, "One Shot");
        }

        public int GetChapterPageCount(int ID) {
            return GetChapterPages(ID).Length;
        }

        private string[] GetChapterPages(int ID) {
            var Document = DownloadDocument(new Uri(ChapterLinks[ID]));
            var Nodes = Document.DocumentNode.SelectNodes("//div[@id=\"thumbnail-container\"]/div/a/img");

            while (Nodes == null) {
                SolveCaptcha();
                SkipSlowDown();

                Document = DownloadDocument(new Uri(ChapterLinks[ID]));
                Nodes = Document.DocumentNode.SelectNodes("//div[@id=\"thumbnail-container\"]/div/a/img");
            }

            List<string> Pages = new List<string>();
            foreach (var Node in Nodes) {
                string PageUrl = Node.GetAttributeValue("data-src", "");
                PageUrl = HttpUtility.HtmlDecode(PageUrl);
                PageUrl = PageUrl.Replace("t.nhentai.net", "i.nhentai.net");
                PageUrl = PageUrl.Replace("t.jpg", ".jpg").Replace("t.png", ".png").Replace("t.bmp", ".bmp");
                Pages.Add(PageUrl);
            }

            return Pages.ToArray();
        }

        public IDecoder GetDecoder() {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo() {
            return new PluginInfo() {
                Author = "Marcussacana",
                Name = "NHentai",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidUri(Uri Uri) {
            return Uri.Host.ToLower().Contains("nhentai") && Uri.AbsolutePath.Contains("/g/");
        }

        public ComicInfo LoadUri(Uri Uri) {
            if (Browser == null) {
                Browser = new ChromiumWebBrowser();
                Browser.Size = new System.Drawing.Size(500, 600);
                Browser.InstallAdBlock();
                Browser.HookReCaptcha();

                while (!Browser.IsBrowserInitialized)
                    ThreadTools.Wait(100, true);

                Login();
                SkipSlowDown();
                SolveCaptcha();
            }

            CurrentUrl = Uri.AbsoluteUri;

            var Document = DownloadDocument(Uri);
            ComicInfo Info = new ComicInfo();

            Info.Title = HttpUtility.HtmlDecode(Document.Descendants("title").First().InnerText);
            Info.Title = DataTools.GetRawName(Info.Title).Split('»').First();

            Info.Cover = TryDownload(new Uri(HttpUtility.HtmlDecode(Document
                .SelectSingleNode("//div[@id=\"cover\"]/a/img")
                .GetAttributeValue("data-src", ""))));

            Info.ContentType = ContentType.Comic;

            return Info;
        }

        public void Login() {
            Browser.Load("https://nhentai.net/login/");
            Browser.GetBrowser().WaitForLoad();

            var Window = new BrowserPopup(Browser, () => !Browser.GetBrowser().MainFrame.Url.Contains("/login"));
            Window.ShowDialog(Main.Instance);
        }

        private void SkipSlowDown() {
            Browser.Load(CurrentUrl);
            Browser.GetBrowser().WaitForLoad();

            bool SlowDown = Browser.GetBrowser().GetHTML().Contains("You're loading pages way too quickly");
            if (SlowDown) {
                Browser.GetBrowser().EvaluateScript("document.getElementsByClassName(\"button button-wide\")[0].click();");
                ThreadTools.Wait(1000, true);
                Browser.GetBrowser().WaitForLoad();
            }
        }

        private bool SolveCaptcha() {
            Browser.Load(CurrentUrl);
            Browser.GetBrowser().WaitForLoad();

            if (!Browser.GetBrowser().GetHTML().Contains("<h1>Really, slow down</h1>"))
                return false;

            if (Browser.GetBrowser().IsCaptchaSolved())
                return false;

            Browser.TrySolveCaptcha(Main.Solver);

            Browser.GetBrowser().EvaluateScript("document.forms[0].submit();");
            ThreadTools.Wait(1000, true);
            Browser.GetBrowser().WaitForLoad();

            return true;
        }
        public HtmlDocument DownloadDocument(Uri Url) {
            HtmlDocument Document = new HtmlDocument();
            Document.LoadHtml(Encoding.UTF8.GetString(TryDownload(Url)));
            return Document;
        } 
        public byte[] TryDownload(Uri Url) {
            return Url.TryDownload(Referer: "http://nhentai.net",
                                   UserAgent: Browser.GetBrowser().GetUserAgent(),
                                   Cookie: Browser.GetBrowser().GetCookies().ToContainer());
        }
    }
}
