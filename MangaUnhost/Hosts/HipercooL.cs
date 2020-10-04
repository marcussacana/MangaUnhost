using HtmlAgilityPack;
using MangaUnhost.Browser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MangaUnhost.Hosts {
    class HipercooL : IHost {
        Dictionary<int, string> LinkMap = new Dictionary<int, string>();
        Dictionary<int, string> NameMap = new Dictionary<int, string>();

        public NovelChapter DownloadChapter(int ID) {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID) {
            foreach (var Page in GetChapterPages(ID))
                yield return Page.TryDownload();
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters() {
            int ID = NameMap.Count;

            foreach (var Node in Document.SelectNodes("//div[@class='chapter']/a")){
                LinkMap[ID] = Document.GetAttributeValue("href", "").EnsureAbsoluteUrl("https://hiper.cool");
                NameMap[ID++] = Link.Split('/').Last();
                yield return new KeyValuePair<int, string>(ID, NameMap[ID++]);
            }
        }

        public int GetChapterPageCount(int ID) {
            return GetChapterPages(ID).Length;
        }

        private string[] GetChapterPages(int ID) {
            var Doc = new HtmlDocument();
            Doc.LoadUrl(new Uri(LinkMap[ID]));

            var Nodes = Doc.SelectNodes("//div[@class='pages']/img");

            return (from x in Nodes select (string)x.GetAttributeValue("src")).ToArray();
        }

        public IDecoder GetDecoder() {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo() {
            return new PluginInfo() {
                Author = "Marcussacana",
                Name = "HipercooL",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidUri(Uri Uri) {
            string URL = Uri.AbsoluteUri.ToLower();
            return URL.Contains("hiper.cool") && URL.Contains("/books/");
        }

        HtmlDocument Document;
        public ComicInfo LoadUri(Uri Uri) {
            Document = new HtmlDocument();
            Document.LoadUrl(Uri);

            ComicInfo Info = new ComicInfo();

            Info.Title = Document.SelectSingleNode("//span[@class='title']").InnerText;

            var Node = Document.SelectSingleNode("//div[@class='cover']/img");
                
            string CLink = Node.GetAttributeValue("data-cfsrc", null);
            if (CLink == null)
                CLink = Node.GetAttributeValue("src", null);

            Info.Cover = new Uri(HttpUtility.HtmlDecode(CLink)).TryDownload();

            Info.ContentType = ContentType.Comic;

            return Info;
        }

        public bool IsValidPage(string HTML, Uri URL) => false;
    }
}
