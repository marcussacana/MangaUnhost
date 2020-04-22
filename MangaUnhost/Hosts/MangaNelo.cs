using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MangaUnhost.Hosts {
    class MangaNelo : IHost {
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

            var Nodes = Document.SelectNodes("//div[@class=\"chapter-list\"]/div/span/a");

            if (Nodes == null || Nodes.Count <= 0) {
                var SafeDoc = new HtmlDocument();
                if (Document.IsCloudflareTriggered())
                    SafeDoc.LoadHtml(JSTools.BypassCloudFlare(CurrentUrl).HTML);
                else
                    SafeDoc = Document;

                Nodes = SafeDoc.SelectNodes("//div[@class=\"chapter-list\"]/div/span/a");

                if (Nodes == null || Nodes.Count <= 0)
                    Nodes = SafeDoc.SelectNodes("//a[@class=\"chapter-name text-nowrap\"]");

                if (Nodes == null || Nodes.Count <= 0)
                    Nodes = SafeDoc.SelectNodes("//div[@class=\"chapter-list\"]//a");
            }

            foreach (var Node in Nodes) {
                string Name = HttpUtility.HtmlDecode(Node.InnerText).ToLower();
                string Link = Node.GetAttributeValue("href", string.Empty);

                if (!Name.ToLower().Contains("chapter")) {
                    if (Link.ToLower().Contains("chapter"))
                        Name = Link.Substring("chapter");
                    Name = (from x in Name.Split(' ', '-', '_') where double.TryParse(x, out _) select x).First();
                } else
                    Name = Name.Substring("chapter").Trim();

                if (Name.Contains(":"))
                    Name = Name.Substring(0, Name.IndexOf(":"));

                ChapterNames[ID] = DataTools.GetRawName(Name);
                ChapterLinks[ID] = Link;
                yield return new KeyValuePair<int, string>(ID, ChapterNames[ID++]);
            }
        }

        public int GetChapterPageCount(int ID) {
            return GetChapterPages(ID).Length;
        }

        private string[] GetChapterPages(int ID) {
            var Page = GetChapterHtml(ID);
            List<string> Pages = new List<string>();

            var Nodes = Page.DocumentNode.SelectNodes("//*[@id=\"vungdoc\"]/img");

            if (Nodes == null || Nodes.Count <= 0)
                Nodes = Page.DocumentNode.SelectNodes("//*[@id=\"vungdoc\"]/div/img");

            if (Nodes == null || Nodes.Count <= 0)
                Nodes = Page.DocumentNode.SelectNodes("//*[@class=\"container-chapter-reader\"]/img");

            if (Nodes == null || Nodes.Count <= 0) {
                Nodes = Page.DocumentNode.SelectNodes("//*[contains(@class, \"chapter-content-inner\")]/p");

                return Nodes.First().InnerText.Split(',');
            }

            foreach (var Node in Nodes)
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
                Name = "MangaNelo Based Sites",
                Author = "Marcussacana",
                SupportComic = true,
                GenericPlugin = true,
                SupportNovel = false,
                Version = new Version(1, 5)
            };
        }

        public bool IsValidUri(Uri Uri) {
            string[] AllowedDomains = new string[] { "mangakakalot", "manganelo", "truyenmoi" };
            return (from x in AllowedDomains where Uri.Host.ToLower().Contains(x) select x).Count() > 0
                && (Uri.AbsolutePath.ToLower().Contains("/manga/") || Uri.AbsolutePath.ToLower().Contains("/doc"));
        }

        public ComicInfo LoadUri(Uri Uri) {
            CurrentUrl = Uri.AbsoluteUri;
            Document = new HtmlDocument();
            Document.LoadUrl(Uri);

            ComicInfo Info = new ComicInfo();

            Info.Title = (Document.SelectSingleNode("//ul[@class=\"manga-info-text\"]/li/h1") ??
                          Document.SelectSingleNode("//ul[@class=\"manga-info-text\"]/li/h2") ??
                          Document.SelectSingleNode("//div[@class=\"story-info-right\"]/h1")  ??
                          Document.SelectSingleNode("//h1[@class=\"title-manga\"]")           ??
                          Document.SelectSingleNode("//h2[@class=\"title-manga\"]")).InnerText;

            Info.Title = HttpUtility.HtmlDecode(Info.Title);

            string CoverUrl = (Document.SelectSingleNode("//div[@class=\"manga-info-pic\"]/img")          ??
                               Document.SelectSingleNode("//span[@class=\"info-image\"]/img")             ??
                               Document.SelectSingleNode("//div[@class=\"media-left cover-detail\"]/img")).GetAttributeValue("src", string.Empty);

            Info.Cover = (CoverUrl.StartsWith("/") ? new Uri(new Uri("http://" + Uri.Host), CoverUrl) : new Uri(CoverUrl)).TryDownload();

            Info.ContentType = ContentType.Comic;

            return Info;
        }
        public bool IsValidPage(string HTML, Uri URL) {
            return (HTML.Contains("chapter-list") || HTML.Contains("chapter-name text-nowrap"));
        }
    }
}
