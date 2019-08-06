using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MangaUnhost.Hosts {
    class Mangakakalot : IHost {
        HtmlDocument Document;
        Dictionary<int, string> ChapterNames = new Dictionary<int, string>();
        Dictionary<int, string> ChapterLinks = new Dictionary<int, string>();

        public string DownloadChapter(int ID) {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID) {
            foreach (var PageUrl in GetChapterPages(ID)) {
                yield return PageUrl.TryDownload();
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters() {
            int ID = ChapterLinks.Count;

            foreach (var Node in Document.SelectNodes("//div[@class=\"chapter-list\"]/div/span/a")) {
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
            
            foreach (var Node in Page.DocumentNode.SelectNodes("//*[@id=\"vungdoc\"]/img"))
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
                Name = "Mangakakalot",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 1)
            };
        }

        public bool IsValidUri(Uri Uri) {
            return (Uri.Host.ToLower().Contains("mangakakalot") || Uri.Host.ToLower().Contains("manganelo"))
                && Uri.AbsolutePath.ToLower().Contains("/manga/");
        }

        public ComicInfo LoadUri(Uri Uri) {
            Document = new HtmlDocument();
            Document.LoadUrl(Uri);

            ComicInfo Info = new ComicInfo();

            Info.Title = Document.SelectSingleNode("//h1").InnerText;
            Info.Title = HttpUtility.HtmlDecode(Info.Title);

            Info.Cover = new Uri(Document
                .SelectSingleNode("//div[@class=\"manga-info-pic\"]/img")
                .GetAttributeValue("src", string.Empty)).TryDownload();

            Info.ContentType = ContentType.Comic;

            return Info;
        }
    }
}
