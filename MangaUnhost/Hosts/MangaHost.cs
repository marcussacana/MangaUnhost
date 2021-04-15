using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;

namespace MangaUnhost.Hosts
{
    class MangaHost : IHost
    {
        HtmlDocument Document;
        Dictionary<int, string> ChapterNames = new Dictionary<int, string>();
        Dictionary<int, string> ChapterLinks = new Dictionary<int, string>();

        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var PageUrl in GetChapterPages(ID))
            {
                yield return PageUrl.TryDownload(UserAgent: ProxyTools.UserAgent);
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            int ID = ChapterLinks.Count;

            var Nodes = Document.SelectNodes("//div[@class=\"chapters\"]//div[@class=\"tags\"]/a");
            var Link = Nodes.First().GetAttributeValue("href", string.Empty);

            bool Found = true;

            List<int> IDs = new List<int>();
            Dictionary<int, string> Founds = new Dictionary<int, string>();

            Dictionary<int, string> SortedFounds = new Dictionary<int, string>();

            while (Found)
            {
                var Doc = LoadDocument(Link);

                Link = Link.Substring(0, Link.LastIndexOf('/') + 1);
                var Options = Doc.SelectNodes("//header[@class=\"navigation\"]//select[@name=\"Chapters\"]/option");

                int BID = ID;

                Found = false;

                foreach (var Option in Options)
                {
                    var Value = Option.GetAttributeValue("value", string.Empty);
                    var URL = Link + Value;

                    if (ChapterLinks.ContainsValue(URL))
                        continue;

                    Found = true;

                    ChapterLinks[ID] = URL;
                    ChapterNames[ID] = DataTools.GetRawName(Value);

                    Founds.Add(ID, ChapterNames[ID++].Trim());
                }

                for (int i = 0; i < ID - BID; i++)
                    IDs.Insert(i, Founds.Keys.ElementAt(BID + i));

                if (Found)
                    Link = ChapterLinks[BID];
            }

            foreach (var CID in IDs) {
                yield return new KeyValuePair<int, string>(CID, ChapterNames[CID]);
            }
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID).Length;
        }

        private string[] GetChapterPages(int ID)
        {
            var Page = GetChapterHtml(ID);
            List<string> Pages = new List<string>();

            var Scripts = Page.SelectNodes("//body//script[@type=\"text/javascript\"]");

            bool Found = false;

            foreach (var Node in Scripts)
            {
                if (!Node.InnerHtml.Contains("var images"))
                    continue;

                Found = true;

                string JS = Node.InnerHtml.Substring("var images = ", "\"];") + "\"]";
                var Result = (from x in JSTools.EvaluateScript<List<object>>(JS) select (string)x).ToArray();
                foreach (string PageHtml in Result)
                {
                    var PageUrl = PageHtml.Substring("src=", " ").Trim(' ', '\'', '"');
                    Pages.Add(PageUrl);
                }
            }

            if (!Found)
                foreach (var Img in Page.SelectNodes("//section[@id='imageWrapper']//img"))
                    Pages.Add(Img.GetAttributeValue("src", string.Empty));

            return (from x in Pages select x.Replace(".webp", "").Replace("/images", "/mangas_files")).ToArray();
        }

        private HtmlDocument GetChapterHtml(int ID)
        {
            return LoadDocument(ChapterLinks[ID]);
        }

        private HtmlDocument LoadDocument(string URL) {

            HtmlDocument Document = new HtmlDocument();
            Document.LoadUrl(URL, UserAgent: ProxyTools.UserAgent, AcceptableErrors: Errors);
            Thread.Sleep(200);
            if (Document.SelectSingleNode("//title")?.InnerText == "403 Forbidden" || Document.ParsedText.StartsWith("error code"))
            {
                Document.LoadUrl(URL, UserAgent: ProxyTools.UserAgent, Proxy: ProxyTools.Proxy, AcceptableErrors: Errors);
                if (Document.SelectSingleNode("//title")?.InnerText == "403 Forbidden" || Document.ParsedText.StartsWith("error code"))
                {
                    Thread.Sleep(500);
                    return LoadDocument(URL);
                }
            }
            return Document;
        }

        public IDecoder GetDecoder()
        {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "MangaHost",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(3, 3)
            };
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.ToLower().Contains("mangahost") && Uri.AbsolutePath.ToLower().Contains("/manga/");
        }


        WebExceptionStatus[] Errors => new WebExceptionStatus[] { WebExceptionStatus.ConnectionClosed, WebExceptionStatus.ProtocolError };
        public ComicInfo LoadUri(Uri Uri)
        {
            Document = LoadDocument(Uri.AbsoluteUri);

            ComicInfo Info = new ComicInfo();

            Info.Title = Document.Descendants("title").First().InnerText.Split('|')[0];
            Info.Title = HttpUtility.HtmlDecode(Info.Title);

            Info.Cover = new Uri(Document
                .SelectSingleNode("//div[@class=\"widget\"]//img")
                .GetAttributeValue("src", string.Empty)).TryDownload(UserAgent: ProxyTools.UserAgent);

            Info.ContentType = ContentType.Comic;

            return Info;
        }
        public bool IsValidPage(string HTML, Uri URL) => false;
    }
}
