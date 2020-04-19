using CefSharp.OffScreen;
using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace MangaUnhost.Hosts {
    class Tsumino : IHost {

        Dictionary<int, string> LinkMap = new Dictionary<int, string>();
        Dictionary<int, string> NameMap = new Dictionary<int, string>();

        public NovelChapter DownloadChapter(int ID) {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID) {
            CurrentUrl = LinkMap[ID];
            string Link = GetNextPage();
            int Reaming = GetChapterPageCount(ID);

            Browser.Load(Link);
            Browser.WaitForLoad();

            string Last = null;
            do {


                Uri PLink = null;
                while (PLink == null) {
                    ThreadTools.Wait(100, true);

                    if (!Browser.IsCaptchaSolved()) {
                        Browser.TrySolveCaptcha(Main.Solver);
                        Browser.EvaluateScript("document.getElementsByClassName(\"auth-page\")[0].getElementsByTagName(\"form\")[0].submit();");
                        Browser.WaitForLoad();
                        Cookies = Browser.GetCookies().ToContainer();
                    }

                    if (Browser.Address.Split('#').First() != Link && !Browser.Address.Contains("Read/Auth")) {
                        Browser.Load(Link);
                        Browser.WaitForLoad();
                    }

                    var Document = Browser.GetDocument();
                    var Node = Document.SelectSingleNode("//img[@class=\"img-responsive reader-img\"]");
                    if (Node == null)
                        continue;
                    var HREF = Node.GetAttributeValue("src", null);
                    if (HREF == null || HREF == Last)
                        continue;
                    

                    Last = HREF;
                    PLink = new Uri(new Uri(Domain), HREF);
                }

                Link = GetNextPage(Link);
                Browser.EvaluateScript("gotoNextPage();");
                yield return TryDownload(PLink);


            } while (--Reaming > 0);
        }

        public string GetNextPage(string Current = null) {
            if (Current == null) {
                Current = $"https://www.tsumino.com/Read/View/{CurrentId}/0";
            }

            int ID = int.Parse(Current.TrimEnd('/').Split('/').Last()) + 1;

            return $"https://www.tsumino.com/Read/View/{CurrentId}/{ID}";
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters() {
            int ID = NameMap.Count;

            var Nodes = Document.SelectNodes("//div[@class=\"book-collection-table\"]/a");
            if (Nodes == null || Nodes.Count == 0) {
                yield return new KeyValuePair<int, string>(ID++, "One Shot");
                yield break;
            }

            foreach (var Node in Nodes.Reverse()) {
                var Link = new Uri(new Uri(Domain), Node.GetAttributeValue("href", "")).AbsoluteUri;
                var Name = Node.SelectSingleNode(Node.XPath + "/span[@width=\"100%\"]").InnerText;
                Name = HttpUtility.HtmlDecode(Name);

                LinkMap[ID] = Link;
                NameMap[ID] = Name;

                yield return new KeyValuePair<int, string>(ID++, Name);
            }
        }

        public int GetChapterPageCount(int ID) {
            var Doc = DownloadDocument(new Uri(LinkMap[ID]));
            return int.Parse(Doc.SelectSingleNode("//div[@id=\"thumbnails-container\"]").GetAttributeValue("data-pages", ""));
        }

        public IDecoder GetDecoder() {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo() {
            return new PluginInfo() {
                Author = "Marcussacana",
                Name = "Tsumino",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidUri(Uri Uri) {
            return Uri.Host.ToLower().Contains("www.tsumino.com") && Uri.AbsolutePath.ToLower().Contains("/book/info/");
        }

        public ComicInfo LoadUri(Uri Uri) {
            if (Browser == null) {
                Browser = new ChromiumWebBrowser(Uri.AbsoluteUri);
                Browser.InstallAdBlock();
                Browser.HookReCaptcha();

                Browser.WaitForLoad();

                Cookies = Browser.GetCookies().ToContainer();
                UserAgent = Browser.GetUserAgent();
            }

            CurrentUrl = Uri.AbsoluteUri;

            Document = DownloadDocument(Uri);

            ComicInfo Info = new ComicInfo();
            Info.Title = HttpUtility.HtmlDecode(Document.SelectSingleNode("//div[@class=\"book-title\"]").InnerText);

            Info.Cover = TryDownload(new Uri(new Uri(Domain), HttpUtility.HtmlDecode(Document
                .SelectSingleNode("//img[@class=\"book-page-image img-responsive\"]")
                .GetAttributeValue("src", ""))));

            Info.ContentType = ContentType.Comic;

            return Info;
        }

        public static ChromiumWebBrowser Browser;
        public static HtmlDocument Document;
        public static CookieContainer Cookies;
        public static string UserAgent;
        public static string CurrentUrl;
        public static string Domain = "https://www.tsumino.com/";

        public static string CurrentId => CurrentUrl.Substring("/info/", "/");

        public HtmlDocument DownloadDocument(Uri Link) {
            var Doc = new HtmlDocument();
            Doc.LoadHtml(Encoding.UTF8.GetString(TryDownload(Link)));
            return Doc;
        }
        public byte[] TryDownload(Uri Link) {
            return Link.TryDownload(CurrentUrl, UserAgent, Cookie: Cookies);
        }
        public bool IsValidPage(string HTML, Uri URL) => false;
    }
}
