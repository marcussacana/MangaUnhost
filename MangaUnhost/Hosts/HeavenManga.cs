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
    class HeavenManga : IHost {
        string CurrentUrl;
        HtmlDocument Document;
        Dictionary<int, string> ChapterNames = new Dictionary<int, string>();
        Dictionary<int, string> ChapterLinks = new Dictionary<int, string>();

        public NovelChapter DownloadChapter(int ID) {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID) {
            foreach (var PageUrl in GetChapterPages(ID)) {
                yield return PageUrl.TryDownload();
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters() {
            int ID = ChapterLinks.Count;
            HtmlDocument Page = Document;
            string CurrentPage = CurrentUrl;

            bool Empty;

            do {
                Empty = true;
                var Nodes = Page.DocumentNode.SelectNodes("//h2[@class=\"chap\"]/a");
                if (Nodes == null)
                    break;
                foreach (var Node in Nodes) {
                    Empty = false;

                    var Name = HttpUtility.HtmlDecode(Node.InnerText).ToLower();
                    var Link = HttpUtility.HtmlDecode(Node.GetAttributeValue("href", ""));

                    if (Name.Contains("ch. "))
                        Name = Name.Substring("ch. ");
                    else if (Name.Contains("chap "))
                        Name = Name.Substring("chap ");

                    if (Name.Contains("v"))
                        Name = Name.Substring(0, Name.IndexOf("v"));

                    Name = DataTools.GetRawName(Name.Trim());

                    ChapterNames[ID] = Name;
                    ChapterLinks[ID] = Link;

                    yield return new KeyValuePair<int, string>(ID++, Name);
                }

                if (!Empty) {
                    CurrentPage = GetNextPage(CurrentPage);
                    Page = new HtmlDocument();
                    Page.LoadUrl(CurrentPage);
                }

            } while (!Empty);
        }
        private string GetNextPage(string Page) {
            if (Page == null)
                Page = CurrentUrl;

            string PageInd = Page.TrimEnd('-', '/').Split('/').Last().Split('-').Last();
            return Page.Substring(0, Page.IndexOf("/page-")) + "/page-" + (int.Parse(PageInd) + 1);
        }

        public int GetChapterPageCount(int ID) {
            return GetChapterPages(ID).Length;
        }

        private string[] GetChapterPages(int ID) {
            var Document = GetChapterHtml(ID);

            List<string> Pages = new List<string>();
            foreach (var Node in Document.SelectNodes("//div[@class=\"chapter-content-inner text-center\"]//img")) {
                Pages.Add(Node.GetAttributeValue("src", ""));
            }

            return Pages.ToArray();
        }
        private HtmlDocument GetChapterHtml(int ID) {
            HtmlDocument Document = new HtmlDocument();
            Document.LoadUrl(ChapterLinks[ID]);
            return Document;
        }
        public IDecoder GetDecoder() {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo() {
            return new PluginInfo() {
                Author = "Marcussacana",
                Name = "HeavenManga",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidUri(Uri Uri) {
            string URL = Uri.AbsoluteUri.ToLower();
            return URL.Contains("heavenmanga.vip") && URL.Substring(URL.IndexOf(".vip")).Split('/').Length >= 2;
        }

        public ComicInfo LoadUri(Uri Uri) {
            CurrentUrl = Uri.AbsoluteUri.TrimEnd('/');
            if (CurrentUrl.Contains("/page-"))
                CurrentUrl = CurrentUrl.Substring(0, CurrentUrl.ToLower().IndexOf("/page-"));
            CurrentUrl += "/page-1";

            Document = new HtmlDocument();
            Document.LoadUrl(Uri);

            ComicInfo Info = new ComicInfo();

            Info.Title = HttpUtility.HtmlDecode(Document.SelectSingleNode("//h1[@class=\"name bigger\"]").InnerText);
            Info.Cover = Document
                .SelectSingleNode("//div[@class=\"comic-info\"]//img")
                .GetAttributeValue("src", "").TryDownload();
            Info.ContentType = ContentType.Comic;

            return Info;
        }
    }
}
