using CefSharp.OffScreen;
using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace MangaUnhost.Hosts
{
    internal class NexusToons : IHost
    {
        private readonly Dictionary<int, ChapterEntry> chapterMap = new Dictionary<int, ChapterEntry>();
        private readonly Dictionary<int, string[]> pageMap = new Dictionary<int, string[]>();

        private ChromiumWebBrowser browser;
        private CookieContainer cookies = new CookieContainer();
        private string browserUserAgent = ProxyTools.UserAgent;
        private Uri currentSeriesUrl;

        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var page in GetChapterPages(ID))
            {
                var data = page.TryDownload(
                    Referer: chapterMap[ID].Referer ?? chapterMap[ID].Url,
                    UserAgent: browserUserAgent,
                    Cookie: cookies);

                if (data != null && data.Length > 0)
                    yield return data;
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            if (!chapterMap.Any())
                LoadChapterMap();

            foreach (var chapter in chapterMap.OrderBy(x => x.Key))
                yield return new KeyValuePair<int, string>(chapter.Key, chapter.Value.Name);
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID).Length;
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "Nexus Toons",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                GenericPlugin = false,
                Version = new Version(1, 0, 0)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            if (URL == null || !URL.Host.Contains("nexustoons.com"))
                return false;

            return HTML.Contains("NEXUS TOONS") &&
                   (HTML.Contains("/manga/") || HTML.Contains("/ler/") || HTML.Contains("reader-page-image"));
        }

        public bool IsValidUri(Uri Uri)
        {
            if (Uri == null || !Uri.Host.Contains("nexustoons.com"))
                return false;

            var path = Uri.AbsolutePath.ToLowerInvariant();
            return path.StartsWith("/manga/") || path.StartsWith("/ler/") || path.StartsWith("/r/");
        }

        public ComicInfo LoadUri(Uri Uri)
        {
            EnsureBrowser();

            chapterMap.Clear();
            pageMap.Clear();

            var entryDoc = LoadDocument(Uri, doc => IsSeriesPage(doc) || IsReaderPage(doc));
            currentSeriesUrl = ResolveSeriesUri(Uri, entryDoc);

            var seriesDoc = LoadDocument(currentSeriesUrl, IsSeriesPage);
            var coverUrl = GetCoverUrl(seriesDoc);

            return new ComicInfo()
            {
                Title = GetSeriesTitle(seriesDoc),
                Cover = DownloadBinary(coverUrl, currentSeriesUrl.AbsoluteUri),
                ContentType = ContentType.Comic,
                Url = currentSeriesUrl
            };
        }

        private string[] GetChapterPages(int ID)
        {
            if (pageMap.ContainsKey(ID))
                return pageMap[ID];

            if (!chapterMap.ContainsKey(ID))
                LoadChapterMap();

            var chapter = chapterMap[ID];
            var chapterDoc = LoadDocument(new Uri(chapter.Url), IsReaderPage, 30, 400);

            chapter.Referer = browser.GetCurrentUrl();
            SyncBrowserState();

            var pages = ScrollAndCollectPages();
            if (!pages.Any() && IsReaderPage(chapterDoc))
                pages = ExtractVisiblePages();

            pageMap[ID] = pages.ToArray();
            return pageMap[ID];
        }

        private void LoadChapterMap()
        {
            LoadDocument(currentSeriesUrl, IsSeriesPage);

            var chapters = ScrollAndCollectChapters();
            if (!chapters.Any())
                chapters = ExtractVisibleChapters();

            int id = 0;
            foreach (var chapter in chapters)
            {
                chapter.Name = NormalizeChapterName(chapter.Name);
                chapterMap[id++] = chapter;
            }
        }

        private HtmlDocument LoadDocument(Uri url, Func<HtmlDocument, bool> validator, int maxAttempts = 24, int waitMs = 300)
        {
            browser.WaitForLoad(url.AbsoluteUri);
            SyncBrowserState();

            if (browser.IsCloudflareTriggered())
            {
                browser.BypassCloudflare();
                SyncBrowserState();
            }

            HtmlDocument lastDoc = null;
            for (int i = 0; i < maxAttempts; i++)
            {
                ThreadTools.Wait(waitMs, true);
                SyncBrowserState();

                lastDoc = browser.GetDocument();
                if (validator == null || validator(lastDoc))
                    return lastDoc;
            }

            return lastDoc ?? new HtmlDocument();
        }

        private void EnsureBrowser()
        {
            if (browser != null)
                return;

            browser = new ChromiumWebBrowser("about:blank");
            browser.Size = new Size(1365, 768);
            browser.SetUserAgent(ProxyTools.UserAgent);
            browser.WaitInitialize();
        }

        private void SyncBrowserState()
        {
            var browserCookies = browser.GetCookies();
            if (browserCookies != null)
                cookies = browserCookies.ToContainer();

            browserUserAgent = browser.GetUserAgent() ?? ProxyTools.UserAgent;
        }

        private Uri ResolveSeriesUri(Uri originalUri, HtmlDocument loadedDoc)
        {
            if (originalUri.AbsolutePath.StartsWith("/manga/", StringComparison.InvariantCultureIgnoreCase))
                return NormalizeUri(originalUri);

            var seriesLink = (loadedDoc
                .SelectSingleNode("//header//a[contains(@href, '/manga/')]") ??
                loadedDoc.SelectSingleNode("//a[contains(@href, '/manga/')]"))
                ?.GetAttributeValue("href", null);

            if (!string.IsNullOrWhiteSpace(seriesLink))
                return NormalizeUri(new Uri(new Uri($"https://{originalUri.Host}"), seriesLink));

            var canonical = loadedDoc
                .SelectSingleNode("//link[@rel='canonical']")
                ?.GetAttributeValue("href", null);

            if (!string.IsNullOrWhiteSpace(canonical) && canonical.Contains("/manga/"))
                return NormalizeUri(new Uri(canonical));

            throw new Exception("Failed to resolve the manga URL.");
        }

        private static Uri NormalizeUri(Uri uri)
        {
            return new Uri(uri.GetLeftPart(UriPartial.Path).TrimEnd('/'));
        }

        private bool IsSeriesPage(HtmlDocument doc)
        {
            return doc?.SelectSingleNode("//h1") != null &&
                   !string.IsNullOrWhiteSpace(GetSeriesTitle(doc)) &&
                   !string.IsNullOrWhiteSpace(GetCoverUrl(doc));
        }

        private bool IsReaderPage(HtmlDocument doc)
        {
            return doc?.SelectNodes("//div[contains(@class, 'reader-page-image')]//img[@src]")?.Count > 0;
        }

        private string GetSeriesTitle(HtmlDocument doc)
        {
            var node = doc.SelectSingleNode("//main//h1") ??
                       doc.SelectSingleNode("//meta[@property='og:title']");

            if (node == null)
                return null;

            var title = node.Name == "meta"
                ? node.GetAttributeValue("content", null)
                : node.InnerText;

            title = HttpUtility.HtmlDecode(title ?? string.Empty).Trim();

            if (title.Contains("|"))
                title = title.Substring(0, title.IndexOf('|')).Trim();

            return title;
        }

        private string GetCoverUrl(HtmlDocument doc)
        {
            var imgNode = doc.SelectSingleNode("//img[contains(@alt, 'Capa de ')]") ??
                          doc.SelectSingleNode("//meta[@property='og:image']");

            if (imgNode == null)
                return null;

            var url = imgNode.Name == "meta"
                ? imgNode.GetAttributeValue("content", null)
                : imgNode.GetAttributeValue("src", null);

            if (string.IsNullOrWhiteSpace(url))
                return null;

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri absolute))
                return absolute.AbsoluteUri;

            var baseUri = currentSeriesUrl;
            if (baseUri == null && Uri.TryCreate(browser?.GetCurrentUrl(), UriKind.Absolute, out Uri current))
                baseUri = current;

            return baseUri == null ? url : new Uri(baseUri, url).AbsoluteUri;
        }

        private List<ChapterEntry> ScrollAndCollectChapters()
        {
            var entries = new List<ChapterEntry>();
            var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            int stableRounds = 0;

            ScrollChapterListToTop();
            ThreadTools.Wait(200, true);

            for (int i = 0; i < 120 && stableRounds < 6; i++)
            {
                int beforeCount = entries.Count;

                foreach (var chapter in ExtractVisibleChapters())
                {
                    if (seen.Add(chapter.Url))
                        entries.Add(chapter);
                }

                stableRounds = beforeCount == entries.Count ? stableRounds + 1 : 0;

                var scrollState = GetChapterListScrollState();
                if (scrollState.AtBottom && stableRounds >= 3)
                    break;

                ScrollChapterListBy(scrollState.Step);
                ThreadTools.Wait(250, true);
            }

            return entries;
        }

        private List<string> ScrollAndCollectPages()
        {
            var pages = new List<string>();
            var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            int stableRounds = 0;

            ScrollWindowToTop();
            ThreadTools.Wait(200, true);

            for (int i = 0; i < 120 && stableRounds < 6; i++)
            {
                int beforeCount = pages.Count;

                foreach (var page in ExtractVisiblePages())
                {
                    if (seen.Add(page))
                        pages.Add(page);
                }

                stableRounds = beforeCount == pages.Count ? stableRounds + 1 : 0;

                var scrollState = GetScrollState();
                if (scrollState.AtBottom && stableRounds >= 3)
                    break;

                ScrollWindowBy(scrollState.Step);
                ThreadTools.Wait(250, true);
            }

            return pages;
        }

        private List<ChapterEntry> ExtractVisibleChapters()
        {
            const string script = @"(function () {
    function normalize(text) {
        return (text || '').replace(/\s+/g, ' ').trim();
    }

    function getContainer() {
        var candidates = Array.from(document.querySelectorAll('div[role=""list""]'));
        return candidates.find(function (node) {
            return node.querySelector('a[href^=""/r/""], a[href^=""/ler/""]');
        }) || document;
    }

    var items = [];
    var seen = {};
    var container = getContainer();

    container.querySelectorAll('a[href^=""/r/""], a[href^=""/ler/""]').forEach(function (anchor) {
        var href = anchor.getAttribute('href') || '';
        var titleNode = anchor.querySelector('p') || anchor.querySelector('span') || anchor;
        var name = normalize(titleNode.textContent);

        if (!name)
            return;

        var lowered = name.toLowerCase();
        if (lowered.indexOf('cap') !== 0 && lowered.indexOf('capítulo') !== 0 && lowered.indexOf('capitulo') !== 0)
            return;

        var abs = new URL(href, location.href).href;
        if (seen[abs])
            return;

        seen[abs] = true;
        items.push({ Url: abs, Name: name });
    });

            return JSON.stringify(items);
})();";

            var json = browser.EvaluateScript<string>(script);
            if (string.IsNullOrWhiteSpace(json))
                return new List<ChapterEntry>();

            return JsonConvert.DeserializeObject<List<ChapterEntry>>(json) ?? new List<ChapterEntry>();
        }

        private List<string> ExtractVisiblePages()
        {
            const string script = @"(function () {
    var items = [];
    var seen = {};

    document.querySelectorAll('.reader-page-image img[src]').forEach(function (img) {
        var src = img.getAttribute('src') || '';
        if (!src || src.indexOf('http') !== 0 || seen[src])
            return;

        seen[src] = true;
        items.push(src);
    });

    return JSON.stringify(items);
})();";

            var json = browser.EvaluateScript<string>(script);
            if (string.IsNullOrWhiteSpace(json))
                return new List<string>();

            return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
        }

        private ScrollState GetScrollState()
        {
            const string script = @"(function () {
    var height = Math.max(
        document.body.scrollHeight || 0,
        document.documentElement.scrollHeight || 0,
        document.body.offsetHeight || 0,
        document.documentElement.offsetHeight || 0
    );

    var y = window.pageYOffset || document.documentElement.scrollTop || document.body.scrollTop || 0;
    var view = window.innerHeight || document.documentElement.clientHeight || 0;
    var step = Math.max(400, Math.floor(view * 0.8));

    return JSON.stringify({
        AtBottom: y + view >= height - 10,
        Step: step
    });
})();";

            var json = browser.EvaluateScript<string>(script);
            return JsonConvert.DeserializeObject<ScrollState>(json) ?? new ScrollState() { Step = 700 };
        }

        private ScrollState GetChapterListScrollState()
        {
            const string script = @"(function () {
    var candidates = Array.from(document.querySelectorAll('div[role=""list""]'));
    var list = candidates.find(function (node) {
        return node.querySelector('a[href^=""/r/""], a[href^=""/ler/""]');
    });

    if (!list) {
        return JSON.stringify({ AtBottom: true, Step: 500 });
    }

    return JSON.stringify({
        AtBottom: list.scrollTop + list.clientHeight >= list.scrollHeight - 10,
        Step: Math.max(220, Math.floor(list.clientHeight * 0.8))
    });
})();";

            var json = browser.EvaluateScript<string>(script);
            return JsonConvert.DeserializeObject<ScrollState>(json) ?? new ScrollState() { Step = 500 };
        }

        private void ScrollWindowBy(int step)
        {
            browser.EvaluateScript($@"(function() {{
    window.scrollBy(0, {Math.Max(200, step)});
}})();");
        }

        private void ScrollWindowToTop()
        {
            browser.EvaluateScript(@"(function() {
    window.scrollTo(0, 0);
})();");
        }

        private void ScrollChapterListToTop()
        {
            browser.EvaluateScript(@"(function () {
    var candidates = Array.from(document.querySelectorAll('div[role=""list""]'));
    var list = candidates.find(function (node) {
        return node.querySelector('a[href^=""/r/""], a[href^=""/ler/""]');
    });

    if (list)
        list.scrollTop = 0;
})();");
        }

        private void ScrollChapterListBy(int step)
        {
            browser.EvaluateScript($@"(function () {{
    var candidates = Array.from(document.querySelectorAll('div[role=""list""]'));
    var list = candidates.find(function (node) {{
        return node.querySelector('a[href^=""/r/""], a[href^=""/ler/""]');
    }});

    if (list)
        list.scrollTop += {Math.Max(180, step)};
}})();");
        }

        private byte[] DownloadBinary(string url, string referer)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            return url.TryDownload(
                Referer: referer,
                UserAgent: browserUserAgent,
                Cookie: cookies);
        }

        private string NormalizeChapterName(string rawName)
        {
            rawName = HttpUtility.HtmlDecode(rawName ?? string.Empty).Trim();

            var number = Regex.Match(rawName, @"\d+(?:[.,]\d+)?").Value;
            if (!string.IsNullOrWhiteSpace(number))
                return number.Replace(',', '.');

            return rawName;
        }

        private sealed class ChapterEntry
        {
            public string Url { get; set; }
            public string Name { get; set; }
            public string Referer { get; set; }
        }

        private sealed class ScrollState
        {
            public bool AtBottom { get; set; }
            public int Step { get; set; }
        }
    }
}
