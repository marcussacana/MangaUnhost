using HtmlAgilityPack;
using MangaUnhost.Browser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MangaUnhost.Hosts {
    class HentaiCafe : IHost {

        Dictionary<int, string> LinkMap = new Dictionary<int, string>();
        Dictionary<int, string> NameMap = new Dictionary<int, string>();

        public string DownloadChapter(int ID) {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID) {
            foreach (var Page in GetChapterPages(ID))
                yield return Page.TryDownload();
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters() {
            int ID = NameMap.Count;
            NameMap[ID] = "One Shot";
            LinkMap[ID] = HttpUtility.HtmlDecode(Document
                .SelectSingleNode("//a[@class=\"x-btn x-btn-flat x-btn-rounded x-btn-large\"]")
                .GetAttributeValue("href", ""));
            yield return new KeyValuePair<int, string>(ID, NameMap[ID++]);
        }

        public int GetChapterPageCount(int ID) {
            return GetChapterPages(ID).Length;
        }

        private string[] GetChapterPages(int ID) {
            var Doc = new HtmlDocument();
            Doc.LoadUrl(new Uri(LinkMap[ID]));

            var Script = Doc.SelectSingleNode("//script[contains(., \"var pages\")]").InnerHtml;
            Script = Script.Substring(0, Script.IndexOf("var next_chapter"));
            Script += "\r\nvar rst = []; for (var i = 0; i < pages.length; i++) rst.push(pages[i].url); rst;";

            var Rst = (List<object>)JSTools.EvaulateScript(Script);

            return (from x in Rst select (string)x).ToArray();
        }

        public IDecoder GetDecoder() {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo() {
            return new PluginInfo() {
                Author = "Marcussacana",
                Name = "HentaiCafe",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidUri(Uri Uri) {
            string URL = Uri.AbsoluteUri.ToLower();
            return URL.Contains("hentai.cafe") && URL.Substring(URL.IndexOf(".cafe/")).Split('/')[1].Length > 2;
        }

        HtmlDocument Document;
        public ComicInfo LoadUri(Uri Uri) {
            Document = new HtmlDocument();
            Document.LoadUrl(Uri);

            ComicInfo Info = new ComicInfo();

            Info.Title = Document.SelectSingleNode("//h3").InnerText;
            Info.Title = HttpUtility.HtmlDecode(Info.Title);

            var Node = Document.SelectSingleNode("//img[contains(@class,\"alignnone size-large\")]");
                
            string CLink = Node.GetAttributeValue("data-cfsrc", null);
            if (CLink == null)
                CLink = Node.GetAttributeValue("src", null);

            Info.Cover = new Uri(HttpUtility.HtmlDecode(CLink)).TryDownload();

            Info.ContentType = ContentType.Comic;

            return Info;
        }
    }
}
