using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MangaUnhost.Hosts {
    class MangaHost : IHost {
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

            bool ManyMode = false;
            var Nodes = Document.SelectNodes("//*[@id=\"page\"]/section[3]/table/tbody/tr/td[1]/a");
            if (Nodes == null) {
                ManyMode = true;
                Nodes = Document.SelectNodes("//ul[@class=\"list_chapters\"]/li/a");
            }

            foreach (var Node in Nodes) {
                string Name;
                if (ManyMode) {
                    Name = Node.GetAttributeValue("id", "");

                    string HTML = HttpUtility.HtmlDecode(Node.GetAttributeValue("data-content", string.Empty));

                    ChapterLinks[ID] = HTML.Substring("<a href='", "'");
                } else {
                    Name = HttpUtility.HtmlDecode(Node.InnerText);
                    Name = Name.Substring(0, Name.LastIndexOf('-')).Substring("#").Trim();
                    ChapterLinks[ID] = Node.GetAttributeValue("href", string.Empty);
                }
                ChapterNames[ID] = DataTools.GetRawName(Name);
                yield return new KeyValuePair<int, string>(ID, ChapterNames[ID++]);
            }
        }

        public int GetChapterPageCount(int ID) {
            return GetChapterPages(ID).Length;
        }

        private string[] GetChapterPages(int ID) {
            var Page = GetChapterHtml(ID);
            List<string> Pages = new List<string>();
            
            foreach (var Node in Page.DocumentNode.SelectNodes("//body//script[@type=\"text/javascript\"]")) {
                if (!Node.InnerHtml.Contains("var images"))
                    continue;

                string JS = Node.InnerHtml.Substring("var images = ", "\"];") + "\"]";
                var Result = (from x in (List<object>)JSTools.EvaulateScript(JS) select (string)x).ToArray();
                foreach (string PageHtml in Result) {
                    var PageUrl = PageHtml.Substring("src=", " ").Trim(' ', '\'', '"');
                    Pages.Add(PageUrl.Replace(".webp", "").Replace("/images", "/mangas_files"));
                }
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
                Name = "MangaHost",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidUri(Uri Uri) {
            return Uri.Host.ToLower().Contains("mangahost") && Uri.AbsolutePath.ToLower().Contains("/manga/");
        }

        public ComicInfo LoadUri(Uri Uri) {
            Document = new HtmlDocument();
            Document.LoadUrl(Uri);

            ComicInfo Info = new ComicInfo();

            Info.Title = Document.Descendants("title").First().InnerText.Split('|')[0];
            Info.Title = HttpUtility.HtmlDecode(Info.Title);

            Info.Cover = new Uri(Document
                .SelectSingleNode("//img[@class=\"pull-left thumbnail\"]")
                .GetAttributeValue("src", string.Empty)).TryDownload();

            Info.ContentType = ContentType.Comic;

            return Info;
        }
    }
}
