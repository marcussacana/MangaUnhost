using CefSharp.OffScreen;
using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaUnhost.Hosts
{
    internal class Lycantoons : IHost
    {
        Dictionary<int, string> ChapterMap = new Dictionary<int, string>();
        Dictionary<int, string[]> PageMap = new Dictionary<int, string[]>();
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var page in GetPages(ID))
            {
                yield return TryDownload(page);
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            foreach (var node in doc.SelectNodes("//div[contains(@id, 'content-capitulos')]//span[contains(@class, 'chakra-badge') and not(.//*[local-name() = 'svg'])]").Reverse())
            {
                var chapName = node.InnerText.Replace("Cap.", "").Trim();
                int id = ChapterMap.Count;

                var baseUrl = currentUri.AbsoluteUri;

                if (baseUrl.EndsWith("/"))
                    baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);

                ChapterMap[id] = $"{baseUrl}/{chapName}";

                yield return new KeyValuePair<int, string>(id, chapName);
            }
        }

        public int GetChapterPageCount(int ID)
        {
            return GetPages(ID).Length;
        }

        public string[] GetPages(int ID) {
            var chapUrl = ChapterMap[ID];

            if (PageMap.ContainsKey(ID))
            {
                return PageMap[ID];
            }

            Browser.WaitForLoad(chapUrl);
            ThreadTools.Wait(3000);

            var html = Browser.GetHTML();

            var pages = new List<string>();

            while (true)
            {
                if (!html.Contains("data-index"))
                    break;

                html = html.Substring("data-index");
                html = html.Substring("<img");
                html = html.Substring("src=");

                char Close = ' ';
                if (html.StartsWith("\""))
                    Close = '"';
                if (html.StartsWith("'"))
                    Close = '\'';

                var url = html.Substring(0, html.IndexOf(Close, 1));
                pages.Add(url);

            }

            /*
             HTML Parser Not Working
            foreach (var node in doc.SelectNodes("//div[@data-index]//img"))
            {
                pages.Add(node.GetAttributeValue("src", null));
            }
            */
            
            return PageMap[ID] = pages.ToArray();
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "Lycantoons",
                Author = "Marcussacana",
                Version = new Version(1, 0),
                SupportComic = true,
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            throw new NotImplementedException();
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.ToLower().Contains("lycantoons.com") && Uri.PathAndQuery.ToLower().Contains("/series/");
        }

        HtmlDocument doc = null;
        CloudflareData? CFData = null;
        Uri currentUri = null;
        ChromiumWebBrowser Browser { get; set; }
        public ComicInfo LoadUri(Uri Uri)
        {
            if (Browser == null) {
                Browser = new ChromiumWebBrowser(Uri.AbsoluteUri);
                Browser.WaitInitialize();
            }

            if (int.TryParse(Uri.PathAndQuery.Split('/').Last(), out _))
            {
                Uri = new Uri(Uri.AbsoluteUri.Substring(0, Uri.AbsoluteUri.LastIndexOf("/")));
            }

            Browser.WaitForLoad(Uri);
            ThreadTools.Wait(1000);

            currentUri = Uri;

            if (Browser.IsCloudflareTriggered())
                CFData = Browser.BypassCloudflare();

            doc = new HtmlDocument();
            doc.LoadHtml(Browser.GetHTML());

            var titleNode = doc.SelectNodes("//h1[@itemprop=\"name\"]").FirstOrDefault();
            var coverNode = doc.SelectNodes("//meta[@property=\"og:image\"]").FirstOrDefault();

            return new ComicInfo()
            {
                Title = titleNode?.InnerText.Trim(),
                Cover = TryDownload(coverNode?.GetAttributeValue("content", null)),
                ContentType = ContentType.Comic,
                Url = Uri
            };
        }

        public byte[] TryDownload(string url) {
            return url.TryDownload(CFData, currentUri.AbsoluteUri, UserAgent: ProxyTools.UserAgent);
        }
    }
}
