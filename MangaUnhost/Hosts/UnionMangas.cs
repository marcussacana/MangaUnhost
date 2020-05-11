using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MangaUnhost.Hosts {
    class UnionMangas : IHost {
        string CurrentHost;
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

            foreach (var Node in Document.SelectNodes("//div[@class=\"row lancamento-linha\"]/div[1]/a")) {
                string Name = HttpUtility.HtmlDecode(Node.InnerText);
                Name = Name.Substring("Cap.").Trim();
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

            foreach (var Node in Page.DocumentNode.SelectNodes("//*[@id=\"leitor\"]/div//img[position()>2]"))
                Pages.Add(new Uri(Node.GetAttributeValue("src", "")).AbsoluteUri);
            
            return Pages.ToArray();
        }

        private HtmlDocument GetChapterHtml(int ID) {
            HtmlDocument Document = new HtmlDocument();
            string HTML = Encoding.UTF8.GetString(TryDownload(new Uri(ChapterLinks[ID])));
            while (HTML.IsCloudflareTriggered()) {
                Cloudflare = JSTools.BypassCloudflare(ChapterLinks[ID]);
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
                Name = "UnionMangas",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 1)
            };
        }

        public bool IsValidUri(Uri Uri) {
            return Uri.Host.ToLower().Contains("union") && Uri.Host.ToLower().Contains(".top") && Uri.AbsolutePath.Contains("manga/");
        }

        public ComicInfo LoadUri(Uri Uri) {
            CurrentHost = Uri.Host;

            string CurrentHtml = Encoding.UTF8.GetString(TryDownload(Uri));
             if (CurrentHtml.IsCloudflareTriggered()) {
                Cloudflare = JSTools.BypassCloudflare(Uri.AbsoluteUri);
                CurrentHtml = Encoding.UTF8.GetString(TryDownload(Uri));
            }

            Document = new HtmlDocument();
            Document.LoadHtml(CurrentHtml);

            ComicInfo Info = new ComicInfo();
            Info.Title = Document.Descendants("title").First().InnerText;
            Info.Title = HttpUtility.HtmlDecode(Info.Title);
            Info.Title = Info.Title.Substring(0, Info.Title.LastIndexOf("-")).Trim();

            Info.Cover = TryDownload(new Uri(Document
                .SelectSingleNode("//img[@class=\"img-thumbnail\"]")
                .GetAttributeValue("src", "")));

            Info.ContentType = ContentType.Comic;

            return Info;
        }

        public byte[] TryDownload(Uri URL) {
            byte[] Rst;

            if (URL.Host != CurrentHost)
            {
                Rst = new Uri(new Uri("https://" + CurrentHost), URL.PathAndQuery).TryDownload();
                if (Rst != null)
                    return Rst;
            }

            if (Cloudflare == null)
                Rst = URL.TryDownload(AcceptableErrors: new System.Net.WebExceptionStatus[] { System.Net.WebExceptionStatus.ProtocolError } );
            else
                Rst = URL.TryDownload(UserAgent: Cloudflare?.UserAgent, Cookie: Cloudflare?.Cookies);
            return Rst;
        }
        public bool IsValidPage(string HTML, Uri URL) => false;
    }
}
