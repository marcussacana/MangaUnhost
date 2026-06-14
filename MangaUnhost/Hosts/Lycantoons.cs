using CefSharp;
using CefSharp.OffScreen;
using HtmlAgilityPack;
using Http2;
using Http2.Hpack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using Nito.AsyncEx.Synchronous;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
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
            foreach (var node in doc.SelectNodes("//div[contains(@id, 'content-capitulos')]//span[contains(@class, 'chakra-badge') and not(.//*[local-name() = 'svg'])]"))
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

            if (PageMap.ContainsKey(ID) && PageMap[ID].Length == 0)
            {
                return PageMap[ID];
            }

            Browser.WaitForLoad(chapUrl);
            ThreadTools.Wait(3000);

            var html = Browser.GetHTML();

            var pages = new List<string>();

            while (true)
            {
                if (!html.Contains("alt=\"Page"))
                    break;

                html = html.Substring("alt=\"Page");
                html = html.Substring("src=");

                char Close = ' ';
                if (html.StartsWith("\""))
                    Close = '"';
                if (html.StartsWith("'"))
                    Close = '\'';

                var url = html.Substring(0, html.IndexOf(Close, 1)).Trim(Close);
                pages.Add(url);

            }

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
                Version = new Version(2, 0),
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



        //This site is blocking http1 requests, since there are no http2 support on .net 4,
        //the best method is to let the browser download and dump the result.
        public byte[] TryDownload(string url) {
            byte[] result = null;
            bool done = false;

            EventHandler<JavascriptMessageReceivedEventArgs> handler = null;

            handler = (s, e) =>
            {
                var base64 = e.Message?.ToString();

                if (!string.IsNullOrEmpty(base64))
                {
                    try
                    {
                        result = Convert.FromBase64String(base64);
                    }
                    catch
                    {
                        result = null;
                    }

                    done = true;
                }
            };

            Browser.JavascriptMessageReceived += handler;

            var script = $@"
                fetch('{url}', {{
                    method: 'GET',
                    credentials: 'omit'
                }})
                .then(r => r.arrayBuffer())
                .then(buffer => {{
                    const bytes = new Uint8Array(buffer);
                    let binary = '';

                    for (let i = 0; i < bytes.length; i++) {{
                        binary += String.fromCharCode(bytes[i]);
                    }}

                    const b64 = btoa(binary);

                    CefSharp.PostMessage(b64);
                }});
            ";

            Browser.ExecuteScriptAsync(script);

            int timeout = 100000;
            int elapsed = 0;

            while (!done && elapsed < timeout)
            {
                ThreadTools.Wait(100, true);
                elapsed += 100;
            }

            Browser.JavascriptMessageReceived -= handler;

            return done ? result : null;
        }

    }
}
