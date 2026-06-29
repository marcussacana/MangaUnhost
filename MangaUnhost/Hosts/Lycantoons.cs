using CefSharp;
using CefSharp.OffScreen;
using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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
                yield return TryDump(page);
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            var OrderMode = doc.SelectSingleNode("//div[p[contains(., 'Ordenar por:')]]//span");

            bool Ascending = OrderMode?.InnerText?.Contains("Crescente") ?? false;

            var chapNodes = doc.SelectNodes("//div[contains(@id, 'content-capitulos')]//span[contains(@class, 'chakra-badge') and not(.//*[local-name() = 'svg'])]").AsEnumerable();

            if (Ascending)
                chapNodes = chapNodes.Reverse();

            foreach (var node in chapNodes)
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

        public string[] GetPages(int ID)
        {
            if (PageMap.ContainsKey(ID) && PageMap[ID].Length > 0)
                return PageMap[ID];

            var chapUrl = ChapterMap[ID];

            Browser.WaitForLoad(chapUrl);
            ThreadTools.Wait(1000);

            var html = Browser.GetHTML();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var node = doc.SelectNodes("//script[contains(., 'imageUrls')]");

            if (node != null)
            {
                var js = node.Single().InnerHtml;
                js = js.Substring("imageUrls", "]");
                js = js.Substring("[").Replace("\\\"", "\"");

                var rst = Browser.EvaluateScript<List<object>>($"[{js}]").Cast<string>().ToArray();
                if (rst.Length > 0)
                    return PageMap[ID] = rst;
            }


            var pages = CollectPageUrlsFromBrowser();
            if (pages.Length > 0)
                return PageMap[ID] = pages;

            var fallbackPages = new List<string>();

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
                fallbackPages.Add(url);
            }

            return PageMap[ID] = fallbackPages.ToArray();
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
                Version = new Version(2, 1),
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
            if (Browser == null)
            {
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
                Cover = TryDump(coverNode?.GetAttributeValue("content", null)),
                ContentType = ContentType.Comic,
                Url = Uri
            };
        }

        public byte[] TryDump(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            byte[] result = null;
            bool done = false;
            const string Prefix = "__LYCANTOONS_DUMP__";

            EventHandler<JavascriptMessageReceivedEventArgs> handler = null;

            handler = (s, e) =>
            {
                var message = e.Message?.ToString();

                if (string.IsNullOrEmpty(message) || !message.StartsWith(Prefix))
                    return;

                var base64 = message.Substring(Prefix.Length);

                if (!string.IsNullOrWhiteSpace(base64))
                {
                    try
                    {
                        result = Convert.FromBase64String(base64);
                    }
                    catch
                    {
                        result = null;
                    }
                }

                done = true;
            };

            Browser.JavascriptMessageReceived += handler;

            var safeUrl = JsonConvert.ToString(url);
            var safePrefix = JsonConvert.ToString(Prefix);

            var script = $@"
                (async () => {{
                    const prefix = {safePrefix};
                    try {{
                        const url = {safeUrl};
                        const img = new Image();
                        img.crossOrigin = 'anonymous';
                        img.decoding = 'async';

                        const loaded = new Promise((resolve, reject) => {{
                            img.onload = () => resolve();
                            img.onerror = () => reject(new Error('image load failed'));
                        }});

                        img.src = url;

                        if (!(img.complete && img.naturalWidth > 0)) {{
                            await loaded;
                        }}

                        const canvas = document.createElement('canvas');
                        const ctx = canvas.getContext('2d');

                        canvas.width = img.naturalWidth;
                        canvas.height = img.naturalHeight;
                        ctx.drawImage(img, 0, 0);

                        await new Promise(resolve => {{
                            canvas.toBlob(blob => {{
                                if (!blob) {{
                                    CefSharp.PostMessage(prefix);
                                    resolve();
                                    return;
                                }}

                                const reader = new FileReader();
                                reader.onloadend = () => {{
                                    CefSharp.PostMessage(prefix + reader.result.split(',')[1]);
                                    resolve();
                                }};
                                reader.readAsDataURL(blob);
                            }}, 'image/png');
                        }});
                    }} catch {{
                        CefSharp.PostMessage(prefix);
                    }}
                }})();
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

        private string[] CollectPageUrlsFromBrowser()
        {
            const string Prefix = "__LYCANTOONS_PAGES__";
            var raw = ExecuteScriptMessage(BuildPageCollectScript(Prefix), Prefix, 80000);

            if (string.IsNullOrWhiteSpace(raw))
                return new string[0];

            try
            {
                return JsonConvert.DeserializeObject<string[]>(raw) ?? new string[0];
            }
            catch
            {
                return new string[0];
            }
        }

        private string ExecuteScriptMessage(string script, string prefix, int timeoutMs)
        {
            string result = null;
            bool done = false;

            EventHandler<JavascriptMessageReceivedEventArgs> handler = null;
            handler = (s, e) =>
            {
                var message = e.Message?.ToString();
                if (string.IsNullOrEmpty(message) || !message.StartsWith(prefix))
                    return;

                result = message.Substring(prefix.Length);
                done = true;
            };

            Browser.JavascriptMessageReceived += handler;

            try
            {
                Browser.ExecuteScriptAsync(script);

                int elapsed = 0;
                while (!done && elapsed < timeoutMs)
                {
                    ThreadTools.Wait(100, true);
                    elapsed += 100;
                }
            }
            finally
            {
                Browser.JavascriptMessageReceived -= handler;
            }

            return done ? result : null;
        }

        private string BuildPageCollectScript(string prefix)
        {
            var PrefixJs = JsonConvert.ToString(prefix);

            return $@"
                (async () => {{
                    try {{
                        const prefix = {PrefixJs};
                        const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));
                        const pages = Array.from(document.querySelectorAll('div[data-page-idx]'));

                        if (!pages.length) {{
                            CefSharp.PostMessage(prefix + '[]');
                            return;
                        }}

                        window.scrollTo(0, 0);
                        await sleep(500);

                        for (const page of pages) {{
                            page.scrollIntoView({{ block: 'center', inline: 'nearest' }});
                            await sleep(350);
                        }}

                        window.scrollTo(0, document.body.scrollHeight);
                        await sleep(500);
                        window.scrollTo(0, 0);

                        for (let i = 0; i < 40; i++) {{
                            const ready = pages.every(page => {{
                                const img = page.querySelector('img');
                                if (!img) {{
                                    return false;
                                }}

                                const src = (img.currentSrc || img.getAttribute('src') || img.src || '').trim();
                                return src.length > 0;
                            }});

                            if (ready) {{
                                break;
                            }}

                            window.scrollTo(0, document.body.scrollHeight);
                            await sleep(500);
                        }}

                        await sleep(3000);

                        const urls = pages.map(page => {{
                            const img = page.querySelector('img');
                            if (!img) {{
                                return '';
                            }}

                            return (img.currentSrc || img.getAttribute('src') || img.src || '').trim();
                        }}).filter(url => url.length > 0);

                        CefSharp.PostMessage(prefix + JSON.stringify(urls));
                    }} catch {{
                        CefSharp.PostMessage({PrefixJs} + '[]');
                    }}
                }})();
            ";
        }
    }
}
