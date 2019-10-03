using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MangaUnhost.Hosts {
    class MangaHasu : IHost {
        HtmlDocument Document;
        Dictionary<int, string> ChapterNames = new Dictionary<int, string>();
        Dictionary<int, string> ChapterLinks = new Dictionary<int, string>();

        CloudflareData? Cloudflare;

        public NovelChapter DownloadChapter(int ID) {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID) {
            foreach (var PageUrl in GetChapterPages(ID)) {
                yield return TryDownload(new Uri(PageUrl));
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters() {
            int ID = ChapterLinks.Count;

            foreach (var Node in Document.SelectNodes("//div[@class=\"list-chapter\"]/table/tbody/tr/td[1]/a")) {
                string Name = HttpUtility.HtmlDecode(Node.InnerText);
                Name = Name.Substring("chapter").Trim();

                if (Name.Contains(":"))
                    Name = Name.Split(':').First();

                ChapterNames[ID] = DataTools.GetRawName(Name);
                ChapterLinks[ID] = Node.GetAttributeValue("href", string.Empty);
                yield return new KeyValuePair<int, string>(ID, ChapterNames[ID++]);
            }
        }

        public int GetChapterPageCount(int ID) {
            return GetChapterPages(ID).Length;
        }
        private string[] GetChapterPages(int ID) {
            var Page = GetChapterHtml(ID);
            List<string> Pages = new List<string>();

            foreach (var Node in Page.DocumentNode.SelectNodes("//div[@class=\"img\"]/img"))
                Pages.Add(new Uri(Node.GetAttributeValue("src", "")).AbsoluteUri);
            
            return Pages.ToArray();
        }

        private HtmlDocument GetChapterHtml(int ID) {
            HtmlDocument Document = new HtmlDocument();
            string HTML = Encoding.UTF8.GetString(TryDownload(new Uri(ChapterLinks[ID])));
            while (HTML.IsCloudflareTriggered()) {
                Cloudflare = JSTools.BypassCloudFlare(ChapterLinks[ID]);
                HTML = Encoding.UTF8.GetString(TryDownload(new Uri(ChapterLinks[ID])));
            }
            Document.LoadHtml(HTML);
            return Document;
        }

        public IDecoder GetDecoder() {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo() {
            return new PluginInfo() {
                Name = "MangaHasu",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidUri(Uri Uri) {
            string URL = Uri.AbsoluteUri.ToLower();
            string Last = URL.Split('-').Last();
            return URL.Contains("mangahasu.se/") && Last.Contains("p") && Last.EndsWith(".html");
        }

        public ComicInfo LoadUri(Uri Uri) {
            string CurrentHtml = Encoding.UTF8.GetString(TryDownload(Uri));
             if (CurrentHtml.IsCloudflareTriggered()) {
                Cloudflare = JSTools.BypassCloudFlare(Uri.AbsoluteUri);
                CurrentHtml = Encoding.UTF8.GetString(TryDownload(Uri));
            }

            Document = new HtmlDocument();
            Document.LoadHtml(CurrentHtml);

            ComicInfo Info = new ComicInfo();
            Info.Title = Document.SelectSingleNode("//div[@class=\"info-title\"]/h1").InnerText;
            Info.Title = HttpUtility.HtmlDecode(Info.Title);

            Info.Cover = TryDownload(new Uri(Document
                .SelectSingleNode("//div[contains(@class, \"info-img\")]/img")
                .GetAttributeValue("src", "")));

            Info.ContentType = ContentType.Comic;

            return Info;
        }

        public byte[] TryDownload(Uri URL) {
            if (Cloudflare == null)
                return URL.TryDownload(AcceptableErrors: new System.Net.WebExceptionStatus[] { System.Net.WebExceptionStatus.ProtocolError } );
            else
                return URL.TryDownload(UserAgent: Cloudflare?.UserAgent, Cookie: Cloudflare?.Cookies, Referer: "http://mangahasu.se/");
        }
    }
}
