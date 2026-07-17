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
            EnsureBrowser();
            foreach (var page in GetPages(ID))
            {
                var dump = TryDump(page);
                if (dump == null && !page.StartsWith("data:"))
                {
                    try
                    {
                        var refUrl = ChapterMap.ContainsKey(ID) ? ChapterMap[ID] : currentUri?.AbsoluteUri;
                        dump = page.TryDownload(Referer: refUrl, UserAgent: Browser?.GetUserAgent());
                    }
                    catch { }
                }
                yield return dump;
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            EnsureBrowser();
            var chapList = new List<KeyValuePair<string, string>>();

            try
            {
                var html = Browser.GetHTML();
                var match = System.Text.RegularExpressions.Regex.Match(html, @"\\*""capitulos\\*""\s*:\s*\[(.*?)\]");
                if (match.Success)
                {
                    var jsonStr = "[" + match.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\") + "]";
                    var chaptersJson = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonStr);
                    if (chaptersJson != null && chaptersJson.Count > 0)
                    {
                        var baseUrl = currentUri.AbsoluteUri.TrimEnd('/');
                        foreach (var chap in chaptersJson)
                        {
                            string num = null;
                            if (chap.ContainsKey("numero") && chap["numero"] != null)
                                num = chap["numero"].ToString();
                            else if (chap.ContainsKey("id") && chap["id"] != null)
                                num = chap["id"].ToString();

                            if (num != null)
                            {
                                chapList.Add(new KeyValuePair<string, string>(num, $"{baseUrl}/{num}"));
                            }
                        }
                    }
                }
            }
            catch { }

            if (chapList.Count == 0)
            {
                var OrderMode = doc.SelectSingleNode("//div[p[contains(., 'Ordenar por:')]]//span | //button[contains(., 'ordem:')]");
                bool Ascending = OrderMode?.InnerText?.Contains("Crescente") == true || OrderMode?.InnerText?.Contains("antigos") == true;

                var chapNodes = doc.SelectNodes("//div[contains(@id, 'content-capitulos')]//span[contains(@class, 'chakra-badge') and not(.//*[local-name() = 'svg'])] | //button[p[contains(., 'CAP.') or contains(., 'Cap.')]]//p[contains(., 'CAP.') or contains(., 'Cap.')] | //p[(contains(., 'CAP.') or contains(., 'Cap.')) and not(ancestor::header)]");

                if (chapNodes != null)
                {
                    var nodes = chapNodes.AsEnumerable();
                    if (Ascending)
                        nodes = nodes.Reverse();

                    foreach (var node in nodes)
                    {
                        var chapName = node.InnerText.Replace("CAP.", "").Replace("Cap.", "").Trim();
                        if (string.IsNullOrWhiteSpace(chapName)) continue;

                        var baseUrl = currentUri.AbsoluteUri.TrimEnd('/');
                        chapList.Add(new KeyValuePair<string, string>(chapName, $"{baseUrl}/{chapName}"));
                    }
                }
            }

            foreach (var item in chapList)
            {
                int id = ChapterMap.Count;
                ChapterMap[id] = item.Value;
                yield return new KeyValuePair<int, string>(id, item.Key);
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

            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(html, @"\\*""imageUrls\\*""\s*:\s*\[(.*?)\]");
                if (match.Success)
                {
                    var jsonStr = "[" + match.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\") + "]";
                    var rst = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(jsonStr);
                    if (rst != null && rst.Length > 0)
                        return PageMap[ID] = rst;
                }
            }
            catch { }

            try
            {
                var nodes = doc.SelectNodes("//script[contains(., 'imageUrls')]");
                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(node.InnerHtml, @"\\*""imageUrls\\*""\s*:\s*\[(.*?)\]");
                        if (match.Success)
                        {
                            var jsonStr = "[" + match.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\") + "]";
                            var rst = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(jsonStr);
                            if (rst != null && rst.Length > 0)
                                return PageMap[ID] = rst;
                        }
                    }
                }
            }
            catch { }


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

        private void EnsureBrowser()
        {
            if (Browser != null) return;
            Browser = new ChromiumWebBrowser("about:blank");
            Browser.WaitInitialize();
            Browser.EarlyInjection("window.__originalToDataURL = HTMLCanvasElement.prototype.toDataURL; window.__originalToBlob = HTMLCanvasElement.prototype.toBlob; window.__originalCreateElement = Document.prototype.createElement;");
        }

        public ComicInfo LoadUri(Uri Uri)
        {
            EnsureBrowser();

            if (int.TryParse(Uri.PathAndQuery.Split('/').Last(), out _))
            {
                Uri = new Uri(Uri.AbsoluteUri.Substring(0, Uri.AbsoluteUri.LastIndexOf("/")));
            }

            if (Browser.Address != Uri.AbsoluteUri)
            {
                Browser.WaitForLoad(Uri);
                ThreadTools.Wait(1000);
            }

            currentUri = Uri;

            if (Browser.IsCloudflareTriggered())
                CFData = Browser.BypassCloudflare();

            doc = new HtmlDocument();
            doc.LoadHtml(Browser.GetHTML());

            var titleNode = doc.SelectNodes("//h1[@itemprop=\"name\"]")?.FirstOrDefault();
            if (titleNode == null)
                titleNode = doc.SelectNodes("//h1")?.FirstOrDefault();

            var coverNode = doc.SelectNodes("//meta[@property=\"og:image\"]")?.FirstOrDefault();
            string coverUrl = coverNode?.GetAttributeValue("content", null);
            if (string.IsNullOrWhiteSpace(coverUrl))
            {
                var imgNode = doc.SelectNodes("//img[contains(@alt, 'Capa') or contains(@alt, 'cover')]")?.FirstOrDefault();
                coverUrl = imgNode?.GetAttributeValue("src", null);
            }

            return new ComicInfo()
            {
                Title = titleNode?.InnerText?.Trim() ?? "Unknown",
                Cover = TryDump(coverUrl),
                ContentType = ContentType.Comic,
                Url = Uri
            };
        }

        public byte[] TryDump(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            if (url.StartsWith("data:"))
            {
                var commaIdx = url.IndexOf(',');
                if (commaIdx != -1)
                {
                    try
                    {
                        var base64 = url.Substring(commaIdx + 1);
                        return Convert.FromBase64String(base64);
                    }
                    catch { }
                }
            }

            EnsureBrowser();

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
                        
                        let cleanToDataURL = window.__originalToDataURL;
                        let cleanToBlob = window.__originalToBlob;
                        let cleanCreateElement = window.__originalCreateElement || document.createElement;

                        if (!cleanToDataURL || !cleanToBlob) {{
                            try {{
                                let ifr = cleanCreateElement.call(document, 'iframe');
                                ifr.style.display = 'none';
                                document.body.appendChild(ifr);
                                cleanToDataURL = cleanToDataURL || ifr.contentWindow.HTMLCanvasElement.prototype.toDataURL;
                                cleanToBlob = cleanToBlob || ifr.contentWindow.HTMLCanvasElement.prototype.toBlob;
                                document.body.removeChild(ifr);
                            }} catch (e) {{
                                cleanToDataURL = cleanToDataURL || HTMLCanvasElement.prototype.toDataURL;
                                cleanToBlob = cleanToBlob || HTMLCanvasElement.prototype.toBlob;
                            }}
                        }}

                        const sendDataUrl = (canvas) => {{
                            try {{
                                const dUrl = cleanToDataURL ? cleanToDataURL.call(canvas, 'image/png') : canvas.toDataURL('image/png');
                                if (dUrl && dUrl.startsWith('data:image')) {{
                                    CefSharp.PostMessage(prefix + dUrl.split(',')[1]);
                                    return true;
                                }}
                            }} catch (e) {{}}
                            return false;
                        }};

                        const sendBlob = async (canvas) => {{
                            return new Promise(resolve => {{
                                try {{
                                    const fn = cleanToBlob || canvas.toBlob;
                                    fn.call(canvas, blob => {{
                                        if (!blob) {{
                                            resolve(false);
                                            return;
                                        }}
                                        const reader = new FileReader();
                                        reader.onloadend = () => {{
                                            if (reader.result && reader.result.includes(',')) {{
                                                CefSharp.PostMessage(prefix + reader.result.split(',')[1]);
                                                resolve(true);
                                            }} else {{
                                                resolve(false);
                                            }}
                                        }};
                                        reader.readAsDataURL(blob);
                                    }}, 'image/png');
                                }} catch (e) {{
                                    resolve(false);
                                }}
                            }});
                        }};

                        const matchesUrl = (src) => {{
                            if (!src || typeof src !== 'string') return false;
                            if (src === url) return true;
                            try {{
                                const u1 = new URL(src, location.href).href;
                                const u2 = new URL(url, location.href).href;
                                if (u1 === u2 || u1.split('?')[0] === u2.split('?')[0]) return true;
                            }} catch {{}}
                            return src.includes(url) || url.includes(src);
                        }};

                        let foundDomImg = null;
                        let foundDomCanvas = null;

                        const allContainers = Array.from(document.querySelectorAll('div[data-page-idx], div[data-page], .rpage-page, div[id*=""page""]'));
                        for (const container of allContainers) {{
                            const img = container.querySelector('img');
                            if (img && matchesUrl((img.currentSrc || img.getAttribute('src') || img.src || '').trim())) {{
                                foundDomImg = img;
                                const c = container.querySelector('canvas');
                                if (c && c.width > 0 && c.height > 0) foundDomCanvas = c;
                                break;
                            }}
                        }}

                        if (!foundDomImg && !foundDomCanvas) {{
                            for (const img of Array.from(document.querySelectorAll('img'))) {{
                                if (matchesUrl((img.currentSrc || img.getAttribute('src') || img.src || '').trim())) {{
                                    foundDomImg = img;
                                    break;
                                }}
                            }}
                        }}

                        if (foundDomCanvas) {{
                            if (sendDataUrl(foundDomCanvas)) return;
                            if (await sendBlob(foundDomCanvas)) return;
                        }}

                        if (foundDomImg && foundDomImg.complete && foundDomImg.naturalWidth > 0) {{
                            try {{
                                const canvas = cleanCreateElement.call(document, 'canvas');
                                const ctx = canvas.getContext('2d');
                                canvas.width = foundDomImg.naturalWidth;
                                canvas.height = foundDomImg.naturalHeight;
                                ctx.drawImage(foundDomImg, 0, 0);
                                if (sendDataUrl(canvas)) return;
                                if (await sendBlob(canvas)) return;
                            }} catch (e) {{
                            }}
                        }}

                        try {{
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

                            const canvas = cleanCreateElement.call(document, 'canvas');
                            const ctx = canvas.getContext('2d');
                            canvas.width = img.naturalWidth;
                            canvas.height = img.naturalHeight;
                            ctx.drawImage(img, 0, 0);

                            if (sendDataUrl(canvas)) return;
                            if (await sendBlob(canvas)) return;
                        }} catch (e) {{
                        }}

                        try {{
                            const response = await fetch(url, {{ credentials: 'omit' }});
                            if (response.ok) {{
                                const blob = await response.blob();
                                if (blob && blob.size > 0) {{
                                    await new Promise(res => {{
                                        const reader = new FileReader();
                                        reader.onloadend = () => {{
                                            if (reader.result && reader.result.includes(',')) {{
                                                CefSharp.PostMessage(prefix + reader.result.split(',')[1]);
                                            }}
                                            res();
                                        }};
                                        reader.readAsDataURL(blob);
                                    }});
                                    return;
                                }}
                            }}
                        }} catch (e) {{}}

                        try {{
                            const img = new Image();
                            const loaded = new Promise((resolve, reject) => {{
                                img.onload = () => resolve();
                                img.onerror = () => reject(new Error('load failed'));
                            }});
                            img.src = url;
                            if (!(img.complete && img.naturalWidth > 0)) {{
                                await loaded;
                            }}
                            const canvas = cleanCreateElement.call(document, 'canvas');
                            const ctx = canvas.getContext('2d');
                            canvas.width = img.naturalWidth;
                            canvas.height = img.naturalHeight;
                            ctx.drawImage(img, 0, 0);

                            if (sendDataUrl(canvas)) return;
                            if (await sendBlob(canvas)) return;
                        }} catch (e) {{}}

                        CefSharp.PostMessage(prefix);
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
                        const collectedMap = new Map();

                        const tryExtractFromScriptOrData = (text) => {{
                            if (!text || typeof text !== 'string') return;
                            try {{
                                const matches = text.matchAll(/\\*""imageUrls\\*""\s*:\s*\[(.*?)\]/g);
                                for (const match of matches) {{
                                    let inner = match[1].replace(/\\""/g, '""').replace(/\\\\/g, '\\');
                                    const arr = JSON.parse('[' + inner + ']');
                                    if (Array.isArray(arr) && arr.length > 0) {{
                                        arr.forEach((u, idx) => {{
                                            if (u && typeof u === 'string' && u.startsWith('http')) {{
                                                if (!Array.from(collectedMap.values()).includes(u)) {{
                                                    collectedMap.set(idx, u);
                                                }}
                                            }}
                                        }});
                                    }}
                                }}
                            }} catch (e) {{}}
                        }};

                        if (typeof self.__next_f !== 'undefined' && Array.isArray(self.__next_f)) {{
                            for (const item of self.__next_f) tryExtractFromScriptOrData(JSON.stringify(item));
                        }}
                        if (typeof window.__next_f !== 'undefined' && Array.isArray(window.__next_f)) {{
                            for (const item of window.__next_f) tryExtractFromScriptOrData(JSON.stringify(item));
                        }}
                        if (typeof window.__NEXT_DATA__ !== 'undefined') {{
                            tryExtractFromScriptOrData(JSON.stringify(window.__NEXT_DATA__));
                        }}
                        Array.from(document.querySelectorAll('script')).forEach(s => {{
                            if (s && (s.textContent || s.innerHTML)) {{
                                tryExtractFromScriptOrData(s.textContent || s.innerHTML);
                            }}
                        }});

                        const collectCurrent = () => {{
                            const nodes = Array.from(document.querySelectorAll('div[data-page-idx], div[data-page], .rpage-page, div[id*=""page""]'));
                            for (const node of nodes) {{
                                let idx = parseInt(node.getAttribute('data-page-idx') ?? node.getAttribute('data-page') ?? -1);
                                const img = node.querySelector('img');
                                if (img) {{
                                    const src = (img.currentSrc || img.getAttribute('src') || img.src || '').trim();
                                    if (src.length > 0 && !src.startsWith('data:')) {{
                                        if (idx !== -1 && !isNaN(idx)) {{
                                            collectedMap.set(idx, src);
                                        }} else {{
                                            if (!Array.from(collectedMap.values()).includes(src)) {{
                                                collectedMap.set(collectedMap.size, src);
                                            }}
                                        }}
                                    }}
                                }}
                            }}
                        }};

                        window.scrollTo(0, 0);
                        await sleep(500);
                        collectCurrent();

                        let lastCount = -1;
                        let retriesWithoutNew = 0;

                        while (retriesWithoutNew < 3) {{
                            const step = window.innerHeight * 0.6;
                            let curr = 0;
                            const maxScroll = Math.max(document.body.scrollHeight, window.innerHeight * 2);

                            while (curr <= maxScroll) {{
                                window.scrollTo(0, curr);
                                await sleep(300);
                                collectCurrent();

                                const nodes = Array.from(document.querySelectorAll('div[data-page-idx], div[data-page], .rpage-page, div[id*=""page""]'));
                                for (const node of nodes) {{
                                    if (!node.dataset.scrolled) {{
                                        node.dataset.scrolled = '1';
                                        node.scrollIntoView({{ block: 'center', inline: 'nearest' }});
                                        await sleep(300);
                                        collectCurrent();
                                    }}
                                }}

                                curr += step;
                            }}

                            window.scrollTo(0, document.body.scrollHeight);
                            await sleep(500);
                            collectCurrent();

                            if (collectedMap.size > lastCount) {{
                                lastCount = collectedMap.size;
                                retriesWithoutNew = 0;
                            }} else {{
                                retriesWithoutNew++;
                            }}
                        }}

                        Array.from(document.querySelectorAll('script')).forEach(s => tryExtractFromScriptOrData(s.textContent || s.innerHTML));

                        const sortedIndices = Array.from(collectedMap.keys()).sort((a, b) => a - b);
                        const urls = sortedIndices.map(k => collectedMap.get(k));

                        CefSharp.PostMessage(prefix + JSON.stringify(urls));
                    }} catch {{
                        CefSharp.PostMessage({PrefixJs} + '[]');
                    }}
                }})();
            ";
        }
    }
}
