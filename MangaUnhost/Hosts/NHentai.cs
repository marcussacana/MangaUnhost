using CefSharp.OffScreen;
using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MangaUnhost.Hosts
{
    class NHentai : IHost {
        static string CurrentUrl;
        static ChromiumWebBrowser Browser = null;

        Dictionary<int, string> ChapterLinks = new Dictionary<int, string>();
        Dictionary<string, string[]> PageLinks = new Dictionary<string, string[]>();

        public NovelChapter DownloadChapter(int ID) {
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

        string LinkPrefix = null;

        private string[] GetChapterPages(int ID) {
            var URI = ChapterLinks[ID];

            if (PageLinks.ContainsKey(URI + ID))
                return PageLinks[URI + ID];

            const string ContainerQuery = "//div[@class=\"thumb-container\"]/a/img";

            var Document = DownloadDocument(new Uri(URI));
            var Nodes = Document.DocumentNode.SelectNodes(ContainerQuery);

            while (Nodes == null) {
                SolveCaptcha();
                SkipSlowDown();

                Document = DownloadDocument(new Uri(ChapterLinks[ID]));
                Nodes = Document.DocumentNode.SelectNodes(ContainerQuery);
            }

            List<string> Pages = new List<string>();
            foreach (var Node in Nodes) {
                string PageUrl = Node.GetAttributeValue("data-src", "");
                PageUrl = HttpUtility.HtmlDecode(PageUrl);
                PageUrl = PageUrl.Replace("t.nhentai.net", "i.nhentai.net");
                PageUrl = PageUrl.Replace("t.jpg", ".jpg").Replace("t.png", ".png").Replace("t.bmp", ".bmp");
                PageUrl = "https://i" + PageUrl.Substring(PageUrl.IndexOf(".nhentai") - 1);

                string OriPrefix = PageUrl.Substring("://", ".nhentai");
                string[] Prefixes = new string[] { "i7", "i6", "i5", "i4", "i3", "i2", "i1", "t7", "t6", "t5", "t4", "t3", "t2", "t1"};

                if (Pages.Count == 0)
                {
                    var rst = TryDownload(new Uri(PageUrl), 1);
                    foreach (var Prefix in Prefixes)
                    {
                        if (rst != null)
                            break;

                        LinkPrefix = Prefix;
                        rst = TryDownload(new Uri(PageUrl.Replace(OriPrefix, Prefix)));
                    }
                }

                if (LinkPrefix != null)
                    PageUrl = PageUrl.Replace(OriPrefix, LinkPrefix);
                
                Pages.Add(PageUrl);
            }
            

            return PageLinks[URI + ID] = Pages.ToArray();
        }

        public IDecoder GetDecoder() {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo() {
            return new PluginInfo() {
                Author = "Marcussacana",
                Name = "NH",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 2, 1)
            };
        }

        public bool IsValidUri(Uri Uri) {
            return Uri.Host.ToLower().Contains("nhentai") && Uri.AbsolutePath.Contains("/g/");
        }

        public ComicInfo LoadUri(Uri Uri) {
            if (Browser == null) {
                Browser = new ChromiumWebBrowser("about:blank");
                Browser.Size = new System.Drawing.Size(500, 600);
                Browser.ReCaptchaHook();

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

            if (Browser.GetBrowser().ReCaptchaIsSolved())
                return false;

            Browser.ReCaptchaTrySolve(Main.Solver);

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
        public byte[] TryDownload(Uri Url, int Tries = 3) {
            return Url.TryDownload(Referer: "https://nhentai.net",
                                   UserAgent: ProxyTools.UserAgent,
                                   Cookie: Browser.GetBrowser().GetCookies().ToContainer(), Retries: Tries);
        }
        public bool IsValidPage(string HTML, Uri URL) => false;
    }
}
