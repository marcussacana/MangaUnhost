using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Web;

namespace MangaUnhost.Hosts {
    class Webtoons : IHost {
        string Referer => "https://www.webtoons.com";
        string CurrentUrl;
        HtmlAgilityPack.HtmlDocument Document;
        Dictionary<int, string> ChapterNames = new Dictionary<int, string>();
        Dictionary<int, string> ChapterLinks = new Dictionary<int, string>();

        public string DownloadChapter(int ID) {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID) {
            foreach (var PageUrl in GetChapterPages(ID)) {
                yield return PageUrl.TryDownload(Referer);
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters() {
            int ID = ChapterLinks.Count;
            var Page = Document;
            string CurrentPage = CurrentUrl;

            bool Empty;
            do {
                Empty = true;
                foreach (var Node in Page.SelectNodes("//ul[@id=\"_listUl\"]/li")) {

                    var LinkNode = Node.SelectSingleNode(Node.XPath + "/a");

                    var Name = HttpUtility.HtmlDecode(Node.GetAttributeValue("data-episode-no", "")).ToLower();
                    Name = DataTools.GetRawName(Name.Trim());

                    var Link = HttpUtility.HtmlDecode(LinkNode.GetAttributeValue("href", ""));

                    if (ChapterLinks.ContainsValue(Link))
                        continue;

                    Empty = false;

                    ChapterNames[ID] = Name;
                    ChapterLinks[ID] = Link;

                    yield return new KeyValuePair<int, string>(ID, ChapterNames[ID++]);
                }

                if (!Empty) {
                    CurrentPage = GetNextPage(CurrentPage);
                    Page = new HtmlDocument();
                    Page.LoadUrl(CurrentPage, Referer: Referer);
                }

            } while (!Empty);
        }
        private string GetNextPage(string Page) {
            if (Page == null)
                Page = CurrentUrl.SetUrlParameter("page", "1");

            return Page.SetUrlParameter("page", (int.Parse(Page.GetUrlParamter("page") ?? "0") + 1).ToString());
        }

        public int GetChapterPageCount(int ID) {
            return GetChapterPages(ID).Length;
        }

        private string[] GetChapterPages(int ID) {
            var Page = GetChapterHtml(ID);
            List<string> Pages = new List<string>();

            foreach (var Node in Page.SelectNodes("//div[@id=\"_imageList\"]/img"))
                Pages.Add(new Uri(Node.GetAttributeValue("data-url", "")).AbsoluteUri);

            return Pages.ToArray();
        }

        private HtmlDocument GetChapterHtml(int ID) {
            var Document = new HtmlDocument();
            Document.LoadUrl(ChapterLinks[ID], Referer: Referer);
            return Document;
        }

        public IDecoder GetDecoder() {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo() {
            return new PluginInfo() {
                Name = "MangaDex",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidUri(Uri Uri) {
            return Uri.Host.ToLower().Contains("webtoons.com") && Uri.AbsoluteUri.ToLower().Contains("/list?title_no=");
        }

        public ComicInfo LoadUri(Uri Uri) {
            Document = new HtmlAgilityPack.HtmlDocument();
            Document.LoadUrl(Uri);

            ComicInfo Info = new ComicInfo();

            Info.Title = Document.SelectSingleNode("//div[@class=\"info\"]/h1").InnerText;
            Info.Title = HttpUtility.HtmlDecode(Info.Title);

            Info.Cover = Document
                .SelectSingleNode("//div[@class=\"detail_body\"]")
                .GetAttributeValue("style", string.Empty)
                .Substring("url(", ")").TryDownload(Referer);

            Info.ContentType = ContentType.Comic;

            CurrentUrl = Uri.AbsoluteUri;

            return Info;
        }
    }
}
