using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MangaUnhost.Hosts {
    class Batoto : IHost {
        static string UserAgent = null;
        string CurrentDomain;
        HtmlDocument Document;
        Dictionary<int, string> ChapterNames = new Dictionary<int, string>();
        Dictionary<int, string> ChapterLinks = new Dictionary<int, string>();

        public string DownloadChapter(int ID) {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID) {
            foreach (var PageUrl in GetChapterPages(ID)) {
                yield return Download(new Uri(PageUrl));
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters() {
            int ID = ChapterLinks.Count;

            foreach (var Node in Document.SelectNodes("//div[@class=\"mt-4 chapter-list\"]//a[@class=\"chapt\"]")) {
                string Name = HttpUtility.HtmlDecode(Node.SelectSingleNode(Node.XPath + "/b").InnerText);
                if (Name.ToLower().Contains("[deleted]") || Name.ToLower().Contains("[delete]"))
                    continue;

                Name = Name.ToLower().Replace("ch.", "").Replace("vol.", "").Trim().Replace("  ", ".").Replace(" ", ".");

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

            foreach (var Node in Page.DocumentNode.SelectNodes("//script[contains(., \"images\")]")) {
                if (!Node.InnerHtml.Contains("var images"))
                    continue;

                string JS = Node.InnerHtml + "\r\nJSON.stringify(images)";
                var Result = (string)JSTools.EvaulateScript(JS);

                string Link = null;
                do {
                    Link = DataTools.ReadJson(Result, (Pages.Count+1).ToString());
                    if (Link != null)
                        Pages.Add(Link);
                    
                } while (Link != null);

            }

            return Pages.ToArray();
        }

        private HtmlDocument GetChapterHtml(int ID) {
            HtmlDocument Document = new HtmlDocument();
            Document.LoadUrl(ChapterLinks[ID], UserAgent: UserAgent);
            return Document;
        }

        public IDecoder GetDecoder() {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo() {
            return new PluginInfo() {
                Name = "Batoto",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidUri(Uri Uri) {
            return (Uri.Host.ToLower().Contains("bato.to") || Uri.Host.ToLower().Contains("mangawindow.net"))
                && Uri.AbsolutePath.ToLower().Contains("/series/");
        }

        public ComicInfo LoadUri(Uri Uri) {
            CurrentDomain = "https://" + Uri.Host;
            Document = new HtmlDocument();
            Document.LoadHtml(Encoding.UTF8.GetString(TryDownload(Uri)));

            ComicInfo Info = new ComicInfo();

            Info.Title = Document.Descendants("title").First().InnerText;
            Info.Title = HttpUtility.HtmlDecode(Info.Title.Substring(0, Info.Title.LastIndexOf("Manga")).Trim());

            string URL = Document
                .SelectSingleNode("//div[@class=\"row detail-set\"]//img")
                .GetAttributeValue("src", string.Empty);

            if (URL.StartsWith("//"))
                URL = "https:" + URL;

            Info.Cover = TryDownload(new Uri(URL));

            Info.ContentType = ContentType.Comic;

            return Info;
        }

        public static byte[] TryDownload(Uri URL) {
            while (UserAgent == null)
                UserAgent = JSTools.DefaultBrowser.GetUserAgent();

            return URL.TryDownload(UserAgent: UserAgent);
        }
        public static byte[] Download(Uri URL) {
            while (UserAgent == null)
                UserAgent = JSTools.DefaultBrowser.GetBrowser().GetUserAgent();

            return URL.TryDownload(UserAgent: UserAgent) ?? throw new Exception("Failed to Download");
        }
    }
}
