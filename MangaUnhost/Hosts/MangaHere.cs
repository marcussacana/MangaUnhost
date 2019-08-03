using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace MangaUnhost.Hosts {
    class MangaHere : IHost {
        static string UserAgent = null;
        HtmlDocument Document;
        Dictionary<int, string> ChapterNames = new Dictionary<int, string>();
        Dictionary<int, string> ChapterLinks = new Dictionary<int, string>();

        public string DownloadChapter(int ID) {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID) {
            foreach (var PageUrl in GetChapterPages(ID)) {
                yield return TryDownload(new Uri(PageUrl));
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters() {
            int ID = ChapterLinks.Count;

            foreach (var Node in Document.SelectNodes("//ul[@class=\"detail-main-list\"]/li/a")) {

                var NameNode = Document.SelectSingleNode(Node.XPath + "/div/p[@class=\"title3\"]");

                string Name = NameNode.InnerText;
                Name = Name.Substring("ch.");

                if (Name.Contains("-"))
                    Name = Name.Split('-')[0];

                ChapterNames[ID] = DataTools.GetRawName(Name.Trim());
                ChapterLinks[ID] = new Uri(new Uri("https://www.mangahere.cc/"), Node.GetAttributeValue("href", string.Empty)).AbsoluteUri;

                yield return new KeyValuePair<int, string>(ID, ChapterNames[ID++]);
            }
        }

        public int GetChapterPageCount(int ID) {
            var Page = GetChapterHtml(ID);

            string ChpScript = Page.SelectSingleNode("//script[contains(., \"chapterid\")]").InnerHtml;
            string CntScript = $"{ChpScript}imagecount;";

            return (int)JSTools.EvaulateScript(CntScript);
        }

        private string[] GetChapterPages(int ID) {
            var Page = GetChapterHtml(ID);

            string KeyScript = Page.SelectSingleNode("//script[contains(., \"eval\")]").InnerHtml;
            string ChpScript = Page.SelectSingleNode("//script[contains(., \"chapterid\")]").InnerHtml;

            KeyScript = $"function $(a) {{var a = []; a.val = function(b){{return b}}; return a}}\r\n{KeyScript}";

            string CntScript = $"{ChpScript}imagecount;";
            string CidScript = $"{ChpScript}chapterid;";

            int Count = (int)JSTools.EvaulateScript(CntScript);
            int ChpId = (int)JSTools.EvaulateScript(CidScript);

            string Key = (string)JSTools.EvaulateScript(KeyScript);


            string Link = ChapterLinks[ID];
            Link = Link.Substring(0, Link.LastIndexOf("/"));
            List<string> Pages = new List<string>();
            for (int i = 1; i <= Count;) {
                string URL = $"{Link}/chapterfun.ashx?cid={ChpId}&page={i}&key={Key}";
                string JS = Encoding.UTF8.GetString(TryDownload(new Uri(URL), ChapterLinks[ID]));
                JS += "\r\nd.join('|');";
                string Result = (string)JSTools.EvaulateScript(JS);
                if (Result == null)
                    break;

                string[] cPages = Result.Split('|');
                for (int x = 0; x < cPages.Length; x++, i++) {
                    string cPage = cPages[x];
                    if (cPage.StartsWith("//"))
                        cPage = "https:" + cPage;

                    Pages.Add(cPage);
                }
            }

            return Pages.ToArray();
        }

        private HtmlDocument GetChapterHtml(int ID) {
            HtmlDocument Document = new HtmlDocument();
            string HTML = Encoding.UTF8.GetString(TryDownload(new Uri(ChapterLinks[ID])));
            Document.LoadHtml(HTML);
            return Document;
        }

        public IDecoder GetDecoder() {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo() {
            return new PluginInfo() {
                Name = "MangaHere",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidUri(Uri Uri) {
            return Uri.Host.ToLower().Contains("mangahere") && Uri.AbsolutePath.ToLower().Contains("/manga/");
        }

        public ComicInfo LoadUri(Uri Uri) {
            Document = new HtmlDocument();
            Document.LoadHtml(Encoding.UTF8.GetString(TryDownload(Uri)));

            ComicInfo Info = new ComicInfo();

            Info.Title = Document.SelectSingleNode("//span[@class=\"detail-info-right-title-font\"]").InnerText;
            Info.Title = HttpUtility.HtmlDecode(Info.Title.Trim());

            string URL = Document
                .SelectSingleNode("//img[@class=\"detail-info-cover-img\"]")
                .GetAttributeValue("src", string.Empty);

            Info.Cover = TryDownload(new Uri(URL));

            Info.ContentType = ContentType.Comic;

            return Info;
        }

        public static byte[] TryDownload(Uri URL, string Referer = null) {
            while (UserAgent == null)
                UserAgent = JSTools.DefaultBrowser.GetBrowser().GetUserAgent();

            return URL.TryDownload(Referer, UserAgent, Cookie: new Cookie("isAdult", "1", "/", "www.mangahere.cc").ToContainer());
        }
    }
}
