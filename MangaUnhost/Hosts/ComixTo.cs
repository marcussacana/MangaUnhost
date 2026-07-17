using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using CefSharp;
using CefSharp.OffScreen;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using Newtonsoft.Json.Linq;

namespace MangaUnhost.Hosts
{
    internal class ComixTo : IHost
    {
        private ChromiumWebBrowser browser;
        private Uri CurrentUrl;
        private string MangaHid;
        private Dictionary<int, string> ChapterMap = new Dictionary<int, string>();
        private Dictionary<int, List<string>> PagesMap = new Dictionary<int, List<string>>();

        private void EnsureBrowser()
        {
            if (browser != null) return;
            browser = new ChromiumWebBrowser("about:blank");
            browser.WaitInitialize();
            browser.EarlyInjection("window.__originalToDataURL = HTMLCanvasElement.prototype.toDataURL; window.__originalCreateElement = Document.prototype.createElement;");
        }

        public ComicInfo LoadUri(Uri Uri)
        {
            CurrentUrl = Uri;
            MangaHid = Regex.Match(Uri.AbsolutePath, @"/title/([^/-]+)").Groups[1].Value;

            EnsureBrowser();
            browser.WaitForLoad(Uri.AbsoluteUri);
            ThreadTools.Wait(3000, true);

            var doc = browser.GetDocument();
            var titleNode = doc.SelectSingleNode("//h1");
            var title = titleNode != null ? titleNode.InnerText.Trim() : "Unknown";

            var coverNode = doc.SelectSingleNode("//img[contains(@src, 'static.comix.to')]");
            var coverUrl = coverNode != null ? coverNode.GetAttributeValue("src", "") : "";

            byte[] cover = null;
            if (!string.IsNullOrEmpty(coverUrl))
                cover = coverUrl.TryDownload();

            return new ComicInfo
            {
                Title = title,
                Cover = cover,
                ContentType = ContentType.Comic,
                Url = Uri
            };
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            EnsureBrowser();

            if (browser.Address != CurrentUrl.AbsoluteUri)
            {
                browser.WaitForLoad(CurrentUrl.AbsoluteUri);
                ThreadTools.Wait(3000, true);
            }

            int chapterId = 0;

            while (true)
            {
                ThreadTools.Wait(2000, true);

                var jsGetChapters = @"
                    JSON.stringify(
                        Array.from(document.querySelectorAll('a'))
                             .filter(a => /\/title\/[^/]+\/\d+-chapter/.test(a.href))
                             .map(a => {
                                 let title = a.innerText.trim();
                                 let m = a.href.match(/chapter-([\d.-]+)/i);
                                 let num = m ? m[1] : '';
                                 return {url: a.href, title: title, num: num};
                             })
                    )
                ";

                var chaptersJson = browser.EvaluateScript<string>(jsGetChapters);

                if (!string.IsNullOrEmpty(chaptersJson) && chaptersJson != "null")
                {
                    var chaptersData = JArray.Parse(chaptersJson);
                    foreach (var item in chaptersData)
                    {
                        string url = item["url"]?.ToString();
                        string name = item["title"]?.ToString();
                        string num = item["num"]?.ToString();

                        if (!string.IsNullOrEmpty(num))
                            name = num;

                        if (string.IsNullOrEmpty(url)) continue;
                        if (ChapterMap.Values.Contains(url)) continue;

                        ChapterMap[chapterId] = url;
                        yield return new KeyValuePair<int, string>(chapterId, name);
                        chapterId++;
                    }
                }

                var jsClickNext = @"
                    (function() {
                        var active = document.querySelector('.npager__num.is-active');
                        if (!active) return false;
                        var next = active.nextElementSibling;
                        if (next && next.classList.contains('npager__num')) {
                            next.click();
                            return true;
                        }
                        return false;
                    })()
                ";

                var clickedNext = browser.EvaluateScript<bool>(jsClickNext);
                if (!clickedNext) break;

                ThreadTools.Wait(3000, true);
            }
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID).Count;
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var url in GetChapterPages(ID))
            {
                if (url.StartsWith("data:"))
                {
                    var commaIdx = url.IndexOf(',');
                    if (commaIdx != -1)
                    {
                        var base64 = url.Substring(commaIdx + 1);
                        yield return Convert.FromBase64String(base64);
                        continue;
                    }
                }
                yield return url.TryDownload(Referer: ChapterMap[ID]);
            }
        }

        private List<string> GetChapterPages(int ID)
        {
            EnsureBrowser();
            var chapUrl = ChapterMap[ID];

            if (PagesMap.ContainsKey(ID))
                return PagesMap[ID];

            browser.WaitForLoad(chapUrl);
            ThreadTools.Wait(3000, true);

            var jsAccumulateImages = @"
                (async function() {
                    let pagesDict = {};
                    let unknownPages = [];
                    
                    let cleanToDataURL = window.__originalToDataURL;
                    if (!cleanToDataURL) {
                        try {
                            let createEl = window.__originalCreateElement || document.createElement;
                            let ifr = createEl.call(document, 'iframe');
                            ifr.style.display = 'none';
                            document.body.appendChild(ifr);
                            cleanToDataURL = ifr.contentWindow.HTMLCanvasElement.prototype.toDataURL;
                            document.body.removeChild(ifr);
                        } catch (e) {
                            cleanToDataURL = HTMLCanvasElement.prototype.toDataURL;
                        }
                    }

                    function collect() {
                        document.querySelectorAll('.rpage-page').forEach(pageContainer => {
                            let pageNum = -1;
                            let dataPage = pageContainer.getAttribute('data-page');
                            if (dataPage !== null) {
                                pageNum = parseInt(dataPage);
                            } else {
                                let label = pageContainer.getAttribute('aria-label');
                                if (label) {
                                    let m = label.match(/Page\s+(\d+)/i);
                                    if (m) pageNum = parseInt(m[1]);
                                }
                            }

                            let img = pageContainer.querySelector('img');
                            let foundValidImg = false;

                            if (img) {
                                let src = img.src || img.getAttribute('data-src') || img.getAttribute('data-lazy-src');
                                if (src && src.startsWith('http') &&
                                    !src.includes('avatar') &&
                                    !src.includes('logo') &&
                                    !src.includes('favicon') &&
                                    !src.includes('@280') &&
                                    !src.includes('comix.to/assets') &&
                                    !src.includes('google.com') &&
                                    !src.includes('cloudflare') &&
                                    !src.includes('static.comix.to')) {
                                    
                                    if (pageNum === -1) {
                                        let alt = img.getAttribute('alt') || '';
                                        let m = alt.match(/Page\s+(\d+)/i);
                                        if (m) pageNum = parseInt(m[1]);
                                    }
                                    
                                    if (pageNum !== -1) {
                                        pagesDict[pageNum] = src;
                                        foundValidImg = true;
                                    } else {
                                        if (!unknownPages.includes(src) && !Object.values(pagesDict).includes(src)) {
                                            unknownPages.push(src);
                                            foundValidImg = true;
                                        }
                                    }
                                }
                            }

                            if (!foundValidImg) {
                                let canvas = pageContainer.querySelector('canvas');
                                if (canvas && pageNum !== -1 && !pagesDict[pageNum]) {
                                    try {
                                        let dataUrl = cleanToDataURL ? cleanToDataURL.call(canvas, 'image/png') : canvas.toDataURL('image/png');
                                        if (dataUrl && dataUrl.startsWith('data:image')) {
                                            pagesDict[pageNum] = dataUrl;
                                        }
                                    } catch (e) {
                                        // Ignore tainted canvas errors
                                    }
                                }
                            }
                        });
                    }

                    // Force remove lazy loading attributes just in case
                    document.querySelectorAll('img[loading]').forEach(img => img.removeAttribute('loading'));

                    let sweeps = 0;
                    let lastImagesSize = 0;

                    while (sweeps < 3) {
                        // Un-mark all elements for the current sweep
                        document.querySelectorAll('.rpage-page').forEach(p => p.removeAttribute('data-scrolled'));

                        let retries = 0;

                        while (true) {
                            let unscrolled = document.querySelectorAll('.rpage-page:not([data-scrolled])');
                            
                            if (unscrolled.length > 0) {
                                // Process the next unscrolled element
                                let p = unscrolled[0];
                                p.setAttribute('data-scrolled', 'true');
                                p.scrollIntoView({ block: 'center' });
                                
                                await new Promise(r => setTimeout(r, 250)); // Wait for image to render
                                collect();
                                retries = 0;
                            } else {
                                // All visible elements have been processed.
                                // Scroll to the very last visible one to trigger loading of the next batch.
                                let all = document.querySelectorAll('.rpage-page');
                                if (all.length > 0) {
                                    all[all.length - 1].scrollIntoView({ block: 'end' });
                                }
                                
                                await new Promise(r => setTimeout(r, 500));
                                collect();
                                
                                let newUnscrolled = document.querySelectorAll('.rpage-page:not([data-scrolled])');
                                if (newUnscrolled.length > 0) {
                                    retries = 0;
                                    continue;
                                }
                                
                                retries++;
                                if (retries >= 6) {
                                    // Waited 3s total at bottom with no new elements
                                    break;
                                }
                            }
                        }

                        // Sweep check
                        let currentSize = Object.keys(pagesDict).length + unknownPages.length;
                        if (currentSize > lastImagesSize) {
                            lastImagesSize = currentSize;
                            sweeps++;
                            // Scroll to top to restart
                            let all = document.querySelectorAll('.rpage-page');
                            if (all.length > 0) all[0].scrollIntoView({ block: 'start' });
                            await new Promise(r => setTimeout(r, 500));
                        } else {
                            break;
                        }
                    }
                    
                    let sortedKeys = Object.keys(pagesDict).map(Number).sort((a,b) => a - b);
                    let finalUrls = sortedKeys.map(k => pagesDict[k]).concat(unknownPages);
                    return JSON.stringify(finalUrls);
                })();
            ";

            var task = browser.EvaluateScriptAsync<string>(jsAccumulateImages);

            while (!task.IsCanceled && !task.IsCompleted && !task.IsFaulted) 
                ThreadTools.Wait(1000, true);

            var imgsJson = task.Result;

            var result = new List<string>();
            if (!string.IsNullOrEmpty(imgsJson) && imgsJson != "null")
            {
                var imgs = JArray.Parse(imgsJson);
                foreach (var img in imgs)
                    result.Add(img.ToString());
            }

            return PagesMap[ID] = result;
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "ComixTo",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                GenericPlugin = false,
                Version = new Version(1, 2, 0)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            return IsValidUri(URL) && HTML.Contains("comix.to");
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.Contains("comix.to") && Uri.AbsolutePath.Contains("/title/");
        }

        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }
    }
}
