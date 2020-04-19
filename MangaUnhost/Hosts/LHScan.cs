using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MangaUnhost.Hosts {
    class LHScan : IHost {
        string CurrentDomain;
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

            foreach (var Node in Document.SelectNodes("//table//a")) {
                string Name = HttpUtility.HtmlDecode(Node.InnerText).ToLower();
                Name = Name.Substring("chapter").Trim();

                ChapterNames[ID] = DataTools.GetRawName(Name);
                ChapterLinks[ID] = new Uri(new Uri(CurrentDomain), Node.GetAttributeValue("href", string.Empty)).AbsoluteUri;
                yield return new KeyValuePair<int, string>(ID, ChapterNames[ID++]);
            }
        }

        public int GetChapterPageCount(int ID) {
            return GetChapterPages(ID).Length;
        }

        private string[] GetChapterPages(int ID) {
            var Page = GetChapterHtml(ID);
            List<string> Pages = new List<string>();

            foreach (var Node in Page.DocumentNode.SelectNodes("//img[@class=\"chapter-img\"]"))
                Pages.Add(Node.GetAttributeValue("src", ""));

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
                Name = "LHScan",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidUri(Uri Uri) {
            string URL = Uri.AbsoluteUri.ToLower();
            return (URL.Contains("rawlh.com") || URL.Contains("lhscan.net") || URL.Contains("18lhplus.com"))
                && URL.EndsWith(".html") && !URL.Contains("-chapter-");
        }

        public ComicInfo LoadUri(Uri Uri) {
            CurrentDomain = $"https://{Uri.Host}";
            Document = new HtmlDocument();
            Document.LoadUrl(Uri);

            ComicInfo Info = new ComicInfo();

            Info.Title = Document.Descendants("title").First().InnerText;
            Info.Title = HttpUtility.HtmlDecode(Info.Title);
            Info.Title = Info.Title.Substring(0, Info.Title.ToLower().IndexOf("- raw")).Trim();

            Info.Cover = new Uri(Document
                .SelectSingleNode("//div[@class=\"well info-cover\"]/img")
                .GetAttributeValue("src", string.Empty)).TryDownload();

            Info.ContentType = ContentType.Comic;

            return Info;
        }
        public bool IsValidPage(string HTML, Uri URL) => false;
    }
}
