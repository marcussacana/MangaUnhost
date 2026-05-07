using CefSharp.OffScreen;
using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace MangaUnhost.Hosts
{
    internal class MangaFire : IHost
    {
        private const string SiteBase = "https://mangafire.to";

        private readonly Dictionary<int, ChapterEntry> chapterMap = new Dictionary<int, ChapterEntry>();
        private readonly Dictionary<int, string[]> pageMap = new Dictionary<int, string[]>();

        private ChromiumWebBrowser browser;
        private CookieContainer cookies = new CookieContainer();
        private string browserUserAgent = ProxyTools.UserAgent;
        private Uri currentSeriesUrl;
        private string currentSeriesId;
        private bool networkHooksInstalled;
        private bool networkCaptureActive;
        private readonly object networkCaptureSync = new object();
        private readonly List<CapturedNetworkEntry> capturedNetworkEntries = new List<CapturedNetworkEntry>();
        private readonly Dictionary<string, NetworkRequestInfo> pendingNetworkRequests = new Dictionary<string, NetworkRequestInfo>(StringComparer.InvariantCultureIgnoreCase);

        private static string LastLanguage;

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

            foreach (var chapter in chapterMap.OrderByDescending(x => x.Key))
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
                Name = "MangaFire",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                GenericPlugin = false,
                Version = new Version(1, 0, 0)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            if (!IsValidUri(URL) || string.IsNullOrWhiteSpace(HTML))
                return false;

            return HTML.Contains("itemprop=\"name\"") ||
                   HTML.Contains("/ajax/manga/") ||
                   HTML.IndexOf("mangafire", StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        public bool IsValidUri(Uri Uri)
        {
            if (Uri == null || !IsSupportedHost(Uri))
                return false;

            var path = Uri.AbsolutePath.ToLowerInvariant();
            return path.StartsWith("/manga/") || path.StartsWith("/read/");
        }

        public ComicInfo LoadUri(Uri Uri)
        {
            EnsureBrowser();

            chapterMap.Clear();
            pageMap.Clear();

            var entryDoc = LoadDocument(Uri, doc => IsSeriesPage(doc) || IsReaderPage(doc));
            currentSeriesUrl = ResolveSeriesUri(Uri, entryDoc);
            currentSeriesId = ExtractSeriesId(currentSeriesUrl);

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
            List<string> pages;
            HtmlDocument chapterDoc;
            TryExtractPagesFromBrowserRequests(new Uri(chapter.Url), out chapterDoc, out pages);

            if (!pages.Any())
                pages = ExtractPagesFromHtml(chapterDoc);

            pageMap[ID] = pages
                .Where(IsImageLikeUrl)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .OrderByFilenameNumber()
                .ToArray();

            if (!pageMap[ID].Any())
                throw new Exception("Failed to load the chapter pages from MangaFire.");

            return pageMap[ID];
        }

        private void LoadChapterMap()
        {
            LoadDocument(currentSeriesUrl, IsSeriesPage);
            EnsureChapterTabSelected();

            var languages = ExtractLanguageOptions();
            if (languages.Any())
            {
                var selectedLanguage = SelectLanguage(languages);
                if (selectedLanguage != null && TrySelectLanguage(selectedLanguage))
                {
                    LastLanguage = selectedLanguage.Text;
                    ThreadTools.Wait(700, true);
                    EnsureChapterTabSelected();
                }
            }

            var chapters = ScrollAndCollectChapters();
            if (!chapters.Any())
                chapters = ExtractVisibleChapters();

            if (!chapters.Any())
                chapters = ExtractChaptersFromHtml(browser.GetDocument());

            chapters = chapters
                .Where(x => !string.IsNullOrWhiteSpace(x.Url))
                .GroupBy(x => x.Url, StringComparer.InvariantCultureIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => GetChapterSortKey(x.DataNumber, x.Name, x.Url))
                .ToList();

            int id = 0;
            foreach (var chapter in chapters)
            {
                chapter.Name = NormalizeChapterName(chapter.DataNumber, chapter.Name, chapter.Url);
                chapterMap[id++] = chapter;
            }

            if (!chapterMap.Any())
                throw new Exception("Failed to load the chapter list from MangaFire.");
        }

        private HtmlDocument LoadDocument(Uri url, Func<HtmlDocument, bool> validator, int maxAttempts = 30, int waitMs = 300)
        {
            browser.WaitForLoad(url.AbsoluteUri);
            SyncBrowserState();

            if (browser.IsCloudflareTriggered())
            {
                browser.BypassCloudflare();
                browser.WaitForLoad(url.AbsoluteUri);
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
            PrepareProtectedBrowser();
        }

        private void SyncBrowserState()
        {
            var browserCookies = browser.GetCookies();
            if (browserCookies != null)
                cookies = browserCookies.ToContainer();

            browserUserAgent = browser.GetUserAgent() ?? ProxyTools.UserAgent;
        }

        private void PrepareProtectedBrowser()
        {
            if (browser == null || networkHooksInstalled)
                return;

            browser.RegisterWebRequestHandlerEvents((sender, args) =>
            {
                if (!networkCaptureActive)
                    return;

                try
                {
                    args.Headers["Accept-Encoding"] = "identity";
                }
                catch
                {
                }

                CaptureNetworkRequest(args);
            }, (sender, args) =>
            {
                if (!networkCaptureActive)
                    return;

                CaptureNetworkResponse(args);
            });

            networkHooksInstalled = true;
        }

        private void BeginNetworkCapture()
        {
            lock (networkCaptureSync)
            {
                capturedNetworkEntries.Clear();
                pendingNetworkRequests.Clear();
                networkCaptureActive = true;
            }
        }

        private void EndNetworkCapture()
        {
            lock (networkCaptureSync)
            {
                networkCaptureActive = false;
            }
        }

        private List<CapturedNetworkEntry> GetCapturedNetworkEntriesSnapshot()
        {
            lock (networkCaptureSync)
            {
                return capturedNetworkEntries.ToList();
            }
        }

        private void CaptureNetworkRequest(OnWebRequestEventArgs args)
        {
            var url = args?.WebRequest?.RequestUri?.AbsoluteUri;
            if (string.IsNullOrWhiteSpace(url) || !ShouldCaptureNetworkEntry(url, null))
                return;

            var info = new NetworkRequestInfo()
            {
                Url = url,
                Method = args?.WebRequest?.Method ?? "GET",
                Referer = args?.WebRequest?.Referer,
                Accept = args?.WebRequest?.Accept,
                ContentType = args?.WebRequest?.ContentType,
                Headers = CloneHeaders(args?.Headers),
                PostData = args?.PostData?.ToArray()
            };

            lock (networkCaptureSync)
            {
                pendingNetworkRequests[url] = info;
            }
        }

        private void CaptureNetworkResponse(OnWebResponseEventArgs args)
        {
            var url = args?.WebRequest?.RequestUri?.AbsoluteUri;
            if (string.IsNullOrWhiteSpace(url) || !ShouldCaptureNetworkEntry(url, args?.WebResponse?.ContentType))
                return;

            string body = null;
            if (ShouldCaptureNetworkBody(url, args?.WebResponse?.ContentType))
            {
                using (var buffer = new MemoryStream())
                {
                    args.ResponseData.CopyTo(buffer);
                    var bytes = buffer.ToArray();
                    args.OverrideData = new MemoryStream(bytes);
                    body = DecodeNetworkBody(bytes, args?.WebResponse?.CharacterSet);
                }
            }

            lock (networkCaptureSync)
            {
                if (!networkCaptureActive)
                    return;

                pendingNetworkRequests.TryGetValue(url, out NetworkRequestInfo requestInfo);
                capturedNetworkEntries.Add(new CapturedNetworkEntry()
                {
                    Url = url,
                    Method = requestInfo?.Method ?? args?.WebRequest?.Method ?? "GET",
                    Referer = requestInfo?.Referer ?? args?.WebRequest?.Referer,
                    Accept = requestInfo?.Accept ?? args?.WebRequest?.Accept,
                    RequestContentType = requestInfo?.ContentType ?? args?.WebRequest?.ContentType,
                    Headers = requestInfo?.Headers ?? CloneHeaders(args?.Headers),
                    PostData = requestInfo?.PostData,
                    ContentType = args?.WebResponse?.ContentType,
                    Body = body
                });
            }
        }

        private bool ShouldCaptureNetworkEntry(string url, string contentType)
        {
            var lowered = (url ?? string.Empty).ToLowerInvariant();
            return lowered.Contains("mangafire.") ||
                   lowered.Contains("/ajax/read/") ||
                   lowered.Contains("/ajax/image/") ||
                   lowered.Contains("/ajax/manga/") ||
                   lowered.Contains("/images/manga/");
        }

        private bool ShouldCaptureNetworkBody(string url, string contentType)
        {
            var loweredUrl = (url ?? string.Empty).ToLowerInvariant();
            var loweredType = (contentType ?? string.Empty).ToLowerInvariant();

            if (IsImageLikeUrl(url))
                return false;

            return loweredType.Contains("json") ||
                   loweredType.Contains("javascript") ||
                   loweredType.Contains("text") ||
                   loweredType.Contains("html") ||
                   loweredUrl.Contains("/ajax/read/") ||
                   loweredUrl.Contains("/ajax/image/");
        }

        private static string DecodeNetworkBody(byte[] bytes, string charset)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            try
            {
                if (!string.IsNullOrWhiteSpace(charset))
                    return Encoding.GetEncoding(charset).GetString(bytes);
            }
            catch
            {
            }

            return Encoding.UTF8.GetString(bytes);
        }

        private static (string Key, string Value)[] CloneHeaders(NameValueCollection headers)
        {
            if (headers == null || headers.Count == 0)
                return Array.Empty<(string Key, string Value)>();

            return headers.AllKeys
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => (Key: x, Value: headers[x]))
                .ToArray();
        }

        private Uri ResolveSeriesUri(Uri originalUri, HtmlDocument loadedDoc)
        {
            if (originalUri.AbsolutePath.StartsWith("/manga/", StringComparison.InvariantCultureIgnoreCase))
                return NormalizeUri(originalUri);

            var seriesLink = loadedDoc.SelectSingleNode("//link[@rel='canonical']")
                ?.GetAttributeValue("href", null);

            if (string.IsNullOrWhiteSpace(seriesLink) || !seriesLink.Contains("/manga/"))
            {
                seriesLink = loadedDoc
                    .SelectSingleNode("//a[contains(@href, '/manga/')]")
                    ?.GetAttributeValue("href", null);
            }

            if (!string.IsNullOrWhiteSpace(seriesLink))
                return NormalizeUri(new Uri(new Uri(SiteBase), seriesLink));

            throw new Exception("Failed to resolve the manga URL.");
        }

        private static Uri NormalizeUri(Uri uri)
        {
            return new Uri(uri.GetLeftPart(UriPartial.Path).TrimEnd('/'));
        }

        private bool IsSeriesPage(HtmlDocument doc)
        {
            return !string.IsNullOrWhiteSpace(GetSeriesTitle(doc)) &&
                   !string.IsNullOrWhiteSpace(GetCoverUrl(doc));
        }

        private bool IsReaderPage(HtmlDocument doc)
        {
            if (doc == null)
                return false;

            if (doc.SelectSingleNode("//img[@data-url or contains(@class, 'page-img') or contains(@class, 'img-fluid')]") != null)
                return true;

            var html = doc.ToHTML();
            return html.Contains("page-img") || html.Contains("data-url");
        }

        private string GetSeriesTitle(HtmlDocument doc)
        {
            var node = doc.SelectSingleNode("//h1[@itemprop='name']") ??
                       doc.SelectSingleNode("//main//h1") ??
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
            var node = doc.SelectSingleNode("//div[contains(@class, 'poster')]//img[@itemprop='image']") ??
                       doc.SelectSingleNode("//div[contains(@class, 'poster')]//img") ??
                       doc.SelectSingleNode("//meta[@property='og:image']");

            if (node == null)
                return null;

            var url = node.Name == "meta"
                ? node.GetAttributeValue("content", null)
                : node.GetAttributeValue("src", null);

            return EnsureAbsoluteUrl(url);
        }

        private LanguageOption SelectLanguage(List<LanguageOption> languages)
        {
            if (!languages.Any())
                return null;

            if (!string.IsNullOrWhiteSpace(LastLanguage))
            {
                var last = languages.FirstOrDefault(x => x.Text.Equals(LastLanguage, StringComparison.InvariantCultureIgnoreCase));
                if (last != null)
                    return last;
            }

            var selected = AccountTools.PromptOption("Select a Language", languages.Select(x => x.Text).ToArray());
            return languages.FirstOrDefault(x => x.Text == selected);
        }

        private List<LanguageOption> ExtractLanguageOptions()
        {
            const string script = @"(function () {
    function normalize(text) {
        return (text || '').replace(/\s+/g, ' ').trim();
    }

    function looksLikeLanguage(text) {
        var lowered = normalize(text).toLowerCase();
        if (!lowered || lowered.length > 60)
            return false;

        return /(english|portuguese|spanish|español|french|italian|japanese|korean|german|thai|indonesian|vietnamese|turkish|russian|arabic|chinese|raw)/.test(lowered);
    }

    var items = [];
    var seen = {};

    Array.from(document.querySelectorAll('button, a, [role=""tab""], li, label, div')).forEach(function (element) {
        var text = normalize(element.textContent);
        if (!looksLikeLanguage(text))
            return;

        var key = text.toLowerCase()
            .replace(/\(\s*\d+\s*chapters?\s*\)/ig, '')
            .replace(/\(\s*\d+\s*volumes?\s*\)/ig, '')
            .trim();

        if (!key || seen[key])
            return;

        seen[key] = true;
        items.push({ Text: text, Key: key });
    });

    return JSON.stringify(items);
})();";

            var json = browser.EvaluateScript<string>(script);
            if (string.IsNullOrWhiteSpace(json))
                return new List<LanguageOption>();

            return JsonConvert.DeserializeObject<List<LanguageOption>>(json) ?? new List<LanguageOption>();
        }

        private bool TrySelectLanguage(LanguageOption option)
        {
            if (option == null)
                return false;

            string text = JsonConvert.ToString(option.Text);
            string key = JsonConvert.ToString(option.Key);
            string script = $@"(function (text, key) {{
    function normalize(value) {{
        return (value || '').replace(/\s+/g, ' ').trim().toLowerCase();
    }}

    var targetText = normalize(text);
    var targetKey = normalize(key);
    var candidates = Array.from(document.querySelectorAll('button, a, [role=""tab""], li, label, div'));

    for (var i = 0; i < candidates.length; i++) {{
        var current = candidates[i];
        var label = normalize(current.textContent);
        if (!label)
            continue;

        if (label === targetText || label === targetKey || label.indexOf(targetKey + ' (') === 0) {{
            try {{
                current.click();
                return true;
            }} catch (error) {{
            }}
        }}
    }}

    return false;
}})({text}, {key});";

            return browser.EvaluateScript<bool>(script);
        }

        private void EnsureChapterTabSelected()
        {
            const string script = @"(function () {
    function normalize(text) {
        return (text || '').replace(/\s+/g, ' ').trim().toLowerCase();
    }

    var tab = Array.from(document.querySelectorAll('button, a, [role=""tab""]')).find(function (element) {
        var text = normalize(element.textContent);
        if (!text || text.length > 20)
            return false;

        return text === 'chapter' || text === 'chapters';
    });

    if (!tab)
        return false;

    try {
        tab.click();
        return true;
    } catch (error) {
        return false;
    }
})();";

            browser.EvaluateScript<bool>(script);
        }

        private void TryExtractPagesFromBrowserRequests(Uri chapterUri, out HtmlDocument chapterDoc, out List<string> pages)
        {
            PrepareProtectedBrowser();
            BeginNetworkCapture();

            try
            {
                chapterDoc = LoadDocument(chapterUri, IsReaderPage, 40, 400);
                chapterMap.FirstOrDefault(x => x.Value.Url == chapterUri.AbsoluteUri).Value.Referer = browser.GetCurrentUrl();
                SyncBrowserState();

                var visiblePages = WaitAndCollectPages();
                var requestPages = ParseCapturedPageUrls(GetCapturedNetworkEntriesSnapshot());
                pages = MergePageUrls(visiblePages, requestPages);

                if (pages.Any())
                    return;

                ThreadTools.Wait(1200, true);
                visiblePages = MergePageUrls(visiblePages, WaitAndCollectPages());
                requestPages = MergePageUrls(requestPages, ParseCapturedPageUrls(GetCapturedNetworkEntriesSnapshot()));
                pages = MergePageUrls(visiblePages, requestPages);
            }
            finally
            {
                EndNetworkCapture();
            }
        }

        private List<string> ParseCapturedPageUrls(IEnumerable<CapturedNetworkEntry> captured)
        {
            var pages = new List<string>();
            var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var item in captured)
            {
                if (!string.IsNullOrWhiteSpace(item?.Url) && IsImageLikeUrl(item.Url) && seen.Add(item.Url))
                    pages.Add(item.Url);

                foreach (var page in ExtractInlineImageUrls(item?.Body))
                {
                    if (seen.Add(EnsureAbsoluteUrl(page)))
                        pages.Add(EnsureAbsoluteUrl(page));
                }

                foreach (var page in ExtractNetworkHtmlImageUrls(item?.Body))
                {
                    if (seen.Add(EnsureAbsoluteUrl(page)))
                        pages.Add(EnsureAbsoluteUrl(page));
                }
            }

            return pages;
        }

        private static List<string> MergePageUrls(params IEnumerable<string>[] sets)
        {
            var merged = new List<string>();
            var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var set in sets)
            {
                if (set == null)
                    continue;

                foreach (var url in set)
                {
                    if (string.IsNullOrWhiteSpace(url) || !seen.Add(url))
                        continue;

                    merged.Add(url);
                }
            }

            return merged;
        }

        private List<ChapterEntry> ScrollAndCollectChapters()
        {
            var entries = new List<ChapterEntry>();
            var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            int stableRounds = 0;

            ScrollChapterListsToTop();
            ThreadTools.Wait(250, true);

            for (int i = 0; i < 120 && stableRounds < 6; i++)
            {
                ClickChapterLoadMoreButtons();
                ThreadTools.Wait(i == 0 ? 700 : 300, true);

                int beforeCount = entries.Count;
                foreach (var chapter in ExtractVisibleChapters())
                {
                    if (seen.Add(chapter.Url))
                        entries.Add(chapter);
                }

                stableRounds = beforeCount == entries.Count ? stableRounds + 1 : 0;

                if (!ScrollChapterListsBy(520) && stableRounds >= 3)
                    break;
            }

            return entries;
        }

        private List<ChapterEntry> ExtractVisibleChapters()
        {
            string seriesId = JsonConvert.ToString(currentSeriesId ?? string.Empty);
            string script = $@"(function (seriesId) {{
    function normalize(text) {{
        return (text || '').replace(/\s+/g, ' ').trim();
    }}

    function isChapterLike(url, dataId, dataNumber, name) {{
        if (dataId || dataNumber)
            return true;

        if (/(?:chapter|volume)-[^/?#]+/i.test(url || ''))
            return true;

        var lowered = normalize(name).toLowerCase();
        if (!lowered)
            return false;

        if (lowered.indexOf('start reading') >= 0)
            return false;

        return /(chapter|chap|ch\.|volume|vol\.)\s*\d/i.test(lowered) ||
               /\b\d+(?:[.,]\d+)?\b/.test(lowered);
    }}

    var items = [];
    var seen = {{}};

    Array.from(document.querySelectorAll('a[href*=""/read/""]')).forEach(function (anchor) {{
        var href = anchor.href || anchor.getAttribute('href') || '';
        if (!href)
            return;

        var abs = new URL(href, location.href).href;
        if (seriesId && abs.indexOf('.' + seriesId) < 0)
            return;

        var name = normalize(anchor.getAttribute('title')) || normalize(anchor.textContent);
        var dataId = anchor.getAttribute('data-id') || anchor.getAttribute('data-chapter') || '';
        var dataNumber = anchor.getAttribute('data-number') || anchor.getAttribute('data-num') || '';

        if (!dataNumber) {{
            var match = abs.match(/(?:chapter|volume)-([^/?#]+)/i);
            if (match)
                dataNumber = match[1];
        }}

        if (!name)
            name = dataNumber;

        if (!name || seen[abs] || !isChapterLike(abs, dataId, dataNumber, name))
            return;

        seen[abs] = true;
        items.push({{
            Url: abs,
            Name: name,
            DataId: dataId,
            DataNumber: dataNumber
        }});
    }});

    return JSON.stringify(items);
}})({seriesId});";

            var json = browser.EvaluateScript<string>(script);
            if (string.IsNullOrWhiteSpace(json))
                return new List<ChapterEntry>();

            return JsonConvert.DeserializeObject<List<ChapterEntry>>(json) ?? new List<ChapterEntry>();
        }

        private List<ChapterEntry> ExtractChaptersFromHtml(HtmlDocument doc)
        {
            var entries = new List<ChapterEntry>();
            var nodes = doc.SelectNodes("//a[contains(@href, '/read/')]");
            if (nodes == null)
                return entries;

            foreach (var anchor in nodes)
            {
                var href = anchor.GetAttributeValue("href", null);
                if (string.IsNullOrWhiteSpace(href))
                    continue;

                var absolute = EnsureAbsoluteUrl(href);
                if (string.IsNullOrWhiteSpace(absolute))
                    continue;

                if (!string.IsNullOrWhiteSpace(currentSeriesId) && !absolute.Contains("." + currentSeriesId))
                    continue;

                var name = anchor.GetAttributeValue("title", null) ?? anchor.InnerText;
                var dataId = anchor.GetAttributeValue("data-id", null);
                var dataNumber = anchor.GetAttributeValue("data-number", null) ?? anchor.GetAttributeValue("data-num", null);

                if (!LooksLikeChapterLink(absolute, dataId, dataNumber, name))
                    continue;

                entries.Add(new ChapterEntry()
                {
                    Url = absolute,
                    Name = HttpUtility.HtmlDecode(name ?? string.Empty).Trim(),
                    DataId = dataId,
                    DataNumber = dataNumber
                });
            }

            return entries;
        }

        private int ClickChapterLoadMoreButtons()
        {
            const string script = @"(function () {
    function normalize(text) {
        return (text || '').replace(/\s+/g, ' ').trim().toLowerCase();
    }

    var clicked = 0;
    Array.from(document.querySelectorAll('button, a, [role=""button""]')).forEach(function (element) {
        var text = normalize(element.textContent);
        if (!text)
            return;

        var matches =
            text.indexOf('show more') >= 0 ||
            text.indexOf('load more') >= 0 ||
            text.indexOf('more chapters') >= 0 ||
            text.indexOf('more volumes') >= 0;

        if (!matches)
            return;

        try {
            element.click();
            clicked++;
        } catch (error) {
        }
    });

    return clicked;
})();";

            return browser.EvaluateScript<int>(script);
        }

        private bool ScrollChapterListsBy(int step)
        {
            string script = $@"(function (step) {{
    function isScrollable(element) {{
        if (!element || element === document.body || element === document.documentElement)
            return false;

        var style = window.getComputedStyle(element);
        var overflowY = (style.overflowY || style.overflow || '').toLowerCase();
        if (overflowY.indexOf('auto') === -1 && overflowY.indexOf('scroll') === -1)
            return false;

        return element.scrollHeight > element.clientHeight + 20;
    }}

    var moved = false;
    Array.from(document.querySelectorAll('*')).forEach(function (element) {{
        if (!isScrollable(element))
            return;

        if (!element.querySelector('a[href*=""/read/""]'))
            return;

        var before = element.scrollTop || 0;
        var max = Math.max(0, element.scrollHeight - element.clientHeight);
        if (before >= max - 4)
            return;

        element.scrollTop = Math.min(max, before + step);
        if ((element.scrollTop || 0) > before)
            moved = true;
    }});

    var beforeY = window.pageYOffset || document.documentElement.scrollTop || document.body.scrollTop || 0;
    window.scrollBy(0, step);
    var afterY = window.pageYOffset || document.documentElement.scrollTop || document.body.scrollTop || 0;
    return moved || afterY > beforeY;
}})({Math.Max(220, step)});";

            return browser.EvaluateScript<bool>(script);
        }

        private void ScrollChapterListsToTop()
        {
            const string script = @"(function () {
    function isScrollable(element) {
        if (!element || element === document.body || element === document.documentElement)
            return false;

        var style = window.getComputedStyle(element);
        var overflowY = (style.overflowY || style.overflow || '').toLowerCase();
        if (overflowY.indexOf('auto') === -1 && overflowY.indexOf('scroll') === -1)
            return false;

        return element.scrollHeight > element.clientHeight + 20;
    }

    Array.from(document.querySelectorAll('*')).forEach(function (element) {
        if (!isScrollable(element))
            return;

        if (!element.querySelector('a[href*=""/read/""]'))
            return;

        element.scrollTop = 0;
    });

    window.scrollTo(0, 0);
})();";

            browser.EvaluateScript(script);
        }

        private List<string> WaitAndCollectPages()
        {
            var pages = new List<string>();
            var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            int stableRounds = 0;

            ScrollWindowToTop();
            ThreadTools.Wait(900, true);

            for (int i = 0; i < 90 && stableRounds < 6; i++)
            {
                ThreadTools.Wait(i == 0 ? 900 : 300, true);

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
            }

            return pages;
        }

        private List<string> ExtractVisiblePages()
        {
            const string script = @"(function () {
    function pickUrl(img) {
        return img.getAttribute('data-url') ||
               img.getAttribute('data-src') ||
               img.currentSrc ||
               img.getAttribute('src') ||
               '';
    }

    var items = [];
    var seen = {};
    var candidates = document.querySelectorAll('img[data-url], img.page-img, img.img-fluid, main img, .reader img');

    Array.from(candidates).forEach(function (img) {
        var src = pickUrl(img);
        if (!src || src.indexOf('http') !== 0)
            return;

        var lowered = src.toLowerCase();
        if (lowered.indexOf('avatar') >= 0 || lowered.indexOf('logo') >= 0 || lowered.indexOf('icon') >= 0 || lowered.indexOf('cover') >= 0)
            return;

        var rect = img.getBoundingClientRect();
        var width = Math.max(rect.width || 0, img.naturalWidth || 0, img.width || 0);
        var height = Math.max(rect.height || 0, img.naturalHeight || 0, img.height || 0);
        if (width < 120 && height < 120)
            return;

        if (seen[src])
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

        private List<string> ExtractPagesFromHtml(HtmlDocument doc)
        {
            var pages = new List<string>();
            var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            if (doc == null)
                return pages;

            var nodes = doc.SelectNodes("//img[@data-url or @data-src or @src]");
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    foreach (var url in new[]
                    {
                        node.GetAttributeValue("data-url", null),
                        node.GetAttributeValue("data-src", null),
                        node.GetAttributeValue("src", null)
                    })
                    {
                        MaybeAddImageUrl(pages, seen, url);
                    }
                }
            }

            if (pages.Any())
                return pages;

            var html = doc.ToHTML();
            foreach (var url in ExtractInlineImageUrls(html))
            {
                MaybeAddImageUrl(pages, seen, url);
            }

            if (pages.Any())
                return pages;

            foreach (var url in DataTools.ExtractHtmlLinks(html, currentSeriesUrl?.GetLeftPart(UriPartial.Authority) ?? SiteBase, true))
            {
                MaybeAddImageUrl(pages, seen, url);
            }

            return pages;
        }

        private IEnumerable<string> ExtractInlineImageUrls(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                yield break;

            var arrayMatches = Regex.Matches(
                html,
                @"(?:images|pages)\s*[:=]\s*(\[(?:\\.|[^\]])+\])",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in arrayMatches)
            {
                string raw = HttpUtility.HtmlDecode(match.Groups[1].Value);
                raw = raw.Replace("\\/", "/");

                object decoded = null;
                try
                {
                    decoded = JsonConvert.DeserializeObject(raw);
                }
                catch
                {
                }

                if (decoded is Newtonsoft.Json.Linq.JArray root)
                {
                    foreach (var token in root)
                    {
                        if (token?.Type == Newtonsoft.Json.Linq.JTokenType.String)
                        {
                            var value = token.ToString();
                            if (!string.IsNullOrWhiteSpace(value))
                                yield return value;
                        }
                        else if (token is Newtonsoft.Json.Linq.JArray nested && nested.Count > 0 && nested[0]?.Type == Newtonsoft.Json.Linq.JTokenType.String)
                        {
                            var value = nested[0].ToString();
                            if (!string.IsNullOrWhiteSpace(value))
                                yield return value;
                        }
                    }
                }
            }
        }

        private IEnumerable<string> ExtractNetworkHtmlImageUrls(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                yield break;

            foreach (var url in DataTools.ExtractHtmlLinks(html, currentSeriesUrl?.GetLeftPart(UriPartial.Authority) ?? SiteBase, true))
            {
                if (IsImageLikeUrl(EnsureAbsoluteUrl(url)))
                    yield return url;
            }
        }

        private void MaybeAddImageUrl(List<string> pages, HashSet<string> seen, string url)
        {
            url = EnsureAbsoluteUrl(url);
            if (!IsImageLikeUrl(url) || !seen.Add(url))
                return;

            pages.Add(url);
        }

        private string EnsureAbsoluteUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            if (url.StartsWith("//"))
                return "https:" + url;

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri absolute))
                return absolute.AbsoluteUri;

            var baseUri = currentSeriesUrl;
            if (baseUri == null && Uri.TryCreate(browser?.GetCurrentUrl(), UriKind.Absolute, out Uri current))
                baseUri = current;

            return baseUri == null ? url : new Uri(baseUri, url).AbsoluteUri;
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

        private byte[] DownloadBinary(string url, string referer)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            return url.TryDownload(
                Referer: referer,
                UserAgent: browserUserAgent,
                Cookie: cookies);
        }

        private static string ExtractSeriesId(Uri uri)
        {
            var match = Regex.Match(uri?.AbsolutePath ?? string.Empty, @"\.([a-z0-9]+)$", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string NormalizeChapterName(string dataNumber, string rawName, string url)
        {
            var number = ExtractChapterNumber(dataNumber, rawName, url);
            if (!string.IsNullOrWhiteSpace(number))
                return number;

            return DataTools.GetRawName(HttpUtility.HtmlDecode(rawName ?? string.Empty)).Trim();
        }

        private static double GetChapterSortKey(string dataNumber, string rawName, string url)
        {
            var number = ExtractChapterNumber(dataNumber, rawName, url);
            if (!string.IsNullOrWhiteSpace(number) &&
                double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                return value;
            }

            return DataTools.ForceNumber(rawName ?? url ?? string.Empty);
        }

        private static string ExtractChapterNumber(string dataNumber, string rawName, string url)
        {
            if (!string.IsNullOrWhiteSpace(dataNumber))
                return CleanChapterToken(dataNumber);

            var urlMatch = Regex.Match(url ?? string.Empty, @"(?:chapter|volume)-([^/?#]+)", RegexOptions.IgnoreCase);
            if (urlMatch.Success)
                return CleanChapterToken(urlMatch.Groups[1].Value);

            var rawText = HttpUtility.HtmlDecode(rawName ?? string.Empty);
            var rawMatch = Regex.Match(rawText, @"\d+(?:[.,]\d+)?");
            return rawMatch.Success
                ? rawMatch.Value.Replace(',', '.')
                : null;
        }

        private static string CleanChapterToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            token = HttpUtility.HtmlDecode(token)
                .Trim()
                .Trim('/', '#', '?')
                .Replace('_', '.')
                .Replace(',', '.');

            var numeric = Regex.Match(token, @"\d+(?:[.]\d+)?");
            if (numeric.Success)
                return numeric.Value;

            return DataTools.GetRawName(token, DeupperlizerOnly: false).Trim();
        }

        private static bool LooksLikeChapterLink(string url, string dataId, string dataNumber, string rawName)
        {
            if (!string.IsNullOrWhiteSpace(dataId) || !string.IsNullOrWhiteSpace(dataNumber))
                return true;

            if (Regex.IsMatch(url ?? string.Empty, @"(?:chapter|volume)-[^/?#]+", RegexOptions.IgnoreCase))
                return true;

            var lowered = HttpUtility.HtmlDecode(rawName ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(lowered))
                return false;

            if (lowered.Contains("start reading"))
                return false;

            return Regex.IsMatch(lowered, @"(chapter|chap|ch\.|volume|vol\.)\s*\d", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(lowered, @"\b\d+(?:[.,]\d+)?\b");
        }

        private static bool IsImageLikeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
                return false;

            var lowered = url.ToLowerInvariant();
            if (lowered.Contains("avatar") || lowered.Contains("logo") || lowered.Contains("icon") || lowered.Contains("cover"))
                return false;

            return lowered.Contains(".jpg") ||
                   lowered.Contains(".jpeg") ||
                   lowered.Contains(".png") ||
                   lowered.Contains(".webp") ||
                   lowered.Contains(".gif") ||
                   lowered.Contains(".bmp") ||
                   lowered.Contains(".avif");
        }

        private static bool IsSupportedHost(Uri uri)
        {
            return uri.Host.Equals("mangafire.to", StringComparison.InvariantCultureIgnoreCase) ||
                   uri.Host.EndsWith(".mangafire.to", StringComparison.InvariantCultureIgnoreCase);
        }

        private sealed class ChapterEntry
        {
            public string Url { get; set; }
            public string Name { get; set; }
            public string DataId { get; set; }
            public string DataNumber { get; set; }
            public string Referer { get; set; }
        }

        private sealed class LanguageOption
        {
            public string Text { get; set; }
            public string Key { get; set; }
        }

        private sealed class CapturedNetworkEntry
        {
            public string Url { get; set; }
            public string Method { get; set; }
            public string Referer { get; set; }
            public string Accept { get; set; }
            public string ContentType { get; set; }
            public string RequestContentType { get; set; }
            public (string Key, string Value)[] Headers { get; set; }
            public byte[] PostData { get; set; }
            public string Body { get; set; }
        }

        private sealed class NetworkRequestInfo
        {
            public string Url { get; set; }
            public string Method { get; set; }
            public string Referer { get; set; }
            public string Accept { get; set; }
            public string ContentType { get; set; }
            public (string Key, string Value)[] Headers { get; set; }
            public byte[] PostData { get; set; }
        }

        private sealed class ScrollState
        {
            public bool AtBottom { get; set; }
            public int Step { get; set; }
        }
    }
}
