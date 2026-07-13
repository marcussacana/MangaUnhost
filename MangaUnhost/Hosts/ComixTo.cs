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
                             .map(a => ({url: a.href, title: a.innerText.trim()}))
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
                    let images = new Set();
                    
                    function collect() {
                        Array.from(document.querySelectorAll('img')).forEach(img => {
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
                                images.add(src);
                            }
                        });
                    }

                    let processed = 0;
                    let retries = 0;

                    while (true) {
                        let pages = document.querySelectorAll('.rpage-page');
                        
                        if (processed >= pages.length) {
                            if (pages.length > 0) {
                                pages[pages.length - 1].scrollIntoView({ block: 'end' });
                            }
                            await new Promise(resolve => setTimeout(resolve, 500));
                            pages = document.querySelectorAll('.rpage-page');
                            
                            if (processed >= pages.length) {
                                retries++;
                                if (retries >= 6) break; // Waited 3s total at the end
                            } else {
                                retries = 0;
                            }
                            continue;
                        }
                        
                        // Scroll the current page container into view
                        let p = pages[processed];
                        p.scrollIntoView({ block: 'center' });
                        
                        // Wait for image to load/render
                        await new Promise(resolve => setTimeout(resolve, 250));
                        collect();
                        
                        processed++;
                    }
                    
                    return JSON.stringify(Array.from(images));
                })();
            ";

            var task = browser.EvaluateScriptAsync<string>(jsAccumulateImages);
            while (!task.IsCompleted && !task.IsCanceled)
                ThreadTools.Wait(500, true);

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
