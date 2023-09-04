using CefSharp.OffScreen;
using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using Microsoft.WindowsAPICodePack.Dialogs;
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
                yield return PageUrl.TryDownload(CFData);
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            int ID = ChapterLinks.Count;

            var Nodes = Document.SelectNodes("//div[@class=\"chapters\"]//div[@class=\"tags\"]/a");
            
            string Link = null;
            if (Nodes?.Count > 0)
                Link = Nodes.First().GetAttributeValue("href", string.Empty);

            if (string.IsNullOrEmpty(Link)) {
                Link = OriUrl + "1";
            }

            bool Found = true;

            List<int> IDs = new List<int>();
            Dictionary<int, string> Founds = new Dictionary<int, string>();

            Dictionary<int, string> SortedFounds = new Dictionary<int, string>();

            while (Found)
            {
                var CurrentLink = Link;
                var Doc = LoadDocument(Link);

                Link = Link.Substring(0, Link.LastIndexOf('/') + 1);
                var Options = Doc.SelectNodes("//header[@class=\"navigation\"]//select[@name=\"Chapters\"]/option");

                if (Options == null)
                    break;

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
                {
                    Link = ChapterLinks[BID];
                    if (Link == CurrentLink)
                    {
                        var LastFound = Link.Split('/').Last();
                        
                        int ChapterIndex = 0;
                        int ChapterSubIndex = 0;

                        if (LastFound.Contains('.'))
                        {
                            ChapterIndex = int.Parse(LastFound.Split('.').First().Trim());
                            ChapterSubIndex = int.Parse(LastFound.Split('.').Last().Trim());
                        }
                        else
                            ChapterIndex = int.Parse(LastFound.Trim());
                        /* May flood request and got an ban ip, but will ignore the delay for recent release chapters
                        for (int x = 0; x < 2; x++)
                        {
                            for (int z = 0; z <= 3; z++)
                            {
                                int y = 0;
                                switch (z) {
                                    case 1:
                                        y = 1;
                                        break;
                                    case 2:
                                        y = 2;
                                        break;
                                    case 3:
                                        if (ChapterSubIndex == 0)
                                            y = 5;
                                        break;
                                }
                                
                                var NextPart = ChapterSubIndex + y == 0 ? $"{(ChapterIndex + x)}" : $"{(ChapterIndex + x)}.{(ChapterSubIndex + y)}";
                                Link = OriUrl + NextPart;
                                
                                if (Link == CurrentLink)
                                    continue;

                                var Result = Link.TryDownload(CFData, UserAgent: ProxyTools.UserAgent, Retries: 1);
                                if (Result != null)
                                    goto Next;
                                
                                Thread.Sleep(100);
                            }
                        }
                        */
                        Next:;
                        continue;
                    }
                }
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

            var Links = (from x in Pages select x.Replace(".webp", "").Replace("/images", "/mangas_files")).ToArray();
            if (Links.Where(x => string.IsNullOrEmpty(System.IO.Path.GetExtension(x))).Any())
                return Pages.ToArray();

            return Links;
        }

        private HtmlDocument GetChapterHtml(int ID)
        {
            return LoadDocument(ChapterLinks[ID]);
        }

        Dictionary<string, CloudflareData> ProxyCFData = new Dictionary<string, CloudflareData>();

        private HtmlDocument LoadDocument(string URL) {

            HtmlDocument Document = new HtmlDocument();
            Document.LoadUrl(URL, CFData, Referer: URL, AcceptableErrors: Errors, UserAgent: ProxyTools.UserAgent);
            if (Document.IsCloudflareTriggered())
            {
                using (ChromiumWebBrowser Browser = new ChromiumWebBrowser())
                {
                    Browser.WaitForLoad(URL);
                    do
                    {
                        CFData = Browser.BypassCloudflare();
                    } while (Browser.IsCloudflareTriggered());
                    Document.LoadHtml(CFData?.HTML);
                }
            }

            Thread.Sleep(200);

            if (Document.SelectSingleNode("//title")?.InnerText == "403 Forbidden" || Document.ParsedText.StartsWith("error code"))
            {
                var Proxy = ProxyTools.Proxy;
                Document.LoadUrl(URL, UserAgent: ProxyTools.UserAgent, Proxy: Proxy, AcceptableErrors: Errors);

                if (Document.IsCloudflareTriggered())
                {
                    using (ChromiumWebBrowser Browser = new ChromiumWebBrowser())
                    {
                        Browser.UseProxy(new WebProxy(Proxy));
                        Browser.WaitForLoad(URL);
                        do
                        {
                            ProxyCFData[Proxy] = Browser.BypassCloudflare();
                        } while (Browser.IsCloudflareTriggered());
                        Document.LoadHtml(ProxyCFData[Proxy].HTML);
                    }
                }

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
                Version = new Version(3, 4, 6)
            };
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.ToLower().Contains("mangahost") && Uri.AbsolutePath.ToLower().Contains("/manga/");
        }

        string OriUrl = null;
        static CloudflareData? CFData = null;
        WebExceptionStatus[] Errors => new WebExceptionStatus[] { WebExceptionStatus.ConnectionClosed, WebExceptionStatus.ProtocolError };
        public ComicInfo LoadUri(Uri Uri)
        {
            Document = LoadDocument(Uri.AbsoluteUri);

            OriUrl = Uri.AbsoluteUri;

            if (!OriUrl.EndsWith("/"))
                OriUrl += "/";

            ComicInfo Info = new ComicInfo();

            Info.Title = Document.Descendants("title").First().InnerText.Split('|')[0];
            Info.Title = HttpUtility.HtmlDecode(Info.Title);

            Info.Cover = new Uri(Document
                .SelectSingleNode("//div[@class=\"widget\"]//img")
                .GetAttributeValue("src", string.Empty)).TryDownload(CFData);

            Info.ContentType = ContentType.Comic;

            return Info;
        }
        public bool IsValidPage(string HTML, Uri URL) => false;
    }
}
