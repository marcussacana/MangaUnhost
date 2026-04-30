using CefSharp.OffScreen;
using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace MangaUnhost.Hosts
{
    internal class DragonScanNext : IHost
    {
        private const string SiteBase = "https://rfdragonscan.net";
        private const string TestCredential = "dummy@gmail.com";

        private readonly Dictionary<int, ChapterEntry> chapterMap = new Dictionary<int, ChapterEntry>();
        private readonly Dictionary<int, string[]> pageMap = new Dictionary<int, string[]>();

        private ChromiumWebBrowser browser;
        private CookieContainer cookies = new CookieContainer();
        private string browserUserAgent = ProxyTools.UserAgent;
        private string apiBaseUrl = "https://api.rfdragonscan.net";
        private bool isAuthenticated;
        private bool networkHooksInstalled;
        private bool networkCaptureActive;
        private readonly object networkCaptureSync = new object();
        private readonly List<CapturedNetworkEntry> capturedNetworkEntries = new List<CapturedNetworkEntry>();
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
                // The prefix keeps the new-site host ahead of the legacy DragonScan
                // when both accept rfdragonscan URLs.
                Name = "DragonScan Next",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                GenericPlugin = false,
                Version = new Version(1, 1, 0)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            if (!IsValidUri(URL) || string.IsNullOrWhiteSpace(HTML))
                return false;

            return HTML.Contains("RF DragoN") &&
                   (HTML.Contains("/capitulo/") || HTML.Contains("Capítulos") || HTML.Contains("Capitulos"));
        }

        public bool IsValidUri(Uri Uri)
        {
            if (Uri == null || !IsSupportedHost(Uri))
                return false;

            if (Uri.AbsolutePath.StartsWith("/login", StringComparison.InvariantCultureIgnoreCase))
                return false;

            var segments = GetPathSegments(Uri);
            if (segments.Length < 2 || !Guid.TryParse(segments[0], out _))
                return false;

            if (segments.Length == 2)
                return true;

            return segments.Length >= 4 &&
                   segments[2].Equals("capitulo", StringComparison.InvariantCultureIgnoreCase);
        }

        public ComicInfo LoadUri(Uri Uri)
        {
            chapterMap.Clear();
            pageMap.Clear();

            currentSeriesUrl = ResolveSeriesUri(Uri);
            EnsureAuthenticated(currentSeriesUrl);

            var doc = DownloadHtmlDocument(currentSeriesUrl, SiteBase);
            if (!HasSeriesMetadata(doc))
                throw new Exception("Failed to load the series metadata over HTTP.");

            apiBaseUrl = ExtractApiBaseUrl(doc) ?? apiBaseUrl;

            var title = ExtractSeriesTitle(doc);
            var coverUrl = ExtractCoverUrl(doc);

            return new ComicInfo()
            {
                Title = title,
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
            var chapterUri = new Uri(chapter.Url);
            chapter.Referer = chapter.Url;

            var doc = DownloadHtmlDocument(chapterUri, currentSeriesUrl?.AbsoluteUri ?? SiteBase);
            var pages = ExtractPageUrlsFromDocument(doc);
            var expectedPageCount = ExtractExpectedPageCount(doc);

            foreach (var page in TryExtractPagesFromBrowserRequests(chapterUri, expectedPageCount))
            {
                if (!pages.Contains(page, StringComparer.InvariantCultureIgnoreCase))
                    pages.Add(page);
            }

            if (!pages.Any() || (expectedPageCount > 0 && pages.Count < expectedPageCount))
            {
                foreach (var page in TryExtractPagesFromApi(chapterUri))
                {
                    if (!pages.Contains(page, StringComparer.InvariantCultureIgnoreCase))
                        pages.Add(page);
                }
            }

            if (!pages.Any() || (expectedPageCount > 0 && pages.Count < expectedPageCount))
            {
                foreach (var page in TryExtractPagesFromRsc(chapterUri, expectedPageCount))
                {
                    if (!pages.Contains(page, StringComparer.InvariantCultureIgnoreCase))
                        pages.Add(page);
                }
            }

            if (!pages.Any())
                throw new Exception("Failed to load the chapter pages from RF DragonScan.");

            if (expectedPageCount > 0 && pages.Count < expectedPageCount)
                throw new Exception($"Failed to load all chapter pages from RF DragonScan. Expected {expectedPageCount}, got {pages.Count}.");

            pageMap[ID] = pages.ToArray();
            return pageMap[ID];
        }

        private void LoadChapterMap()
        {
            EnsureAuthenticated(currentSeriesUrl);

            var doc = DownloadHtmlDocument(currentSeriesUrl, currentSeriesUrl.AbsoluteUri);
            var reportedChapterCount = ExtractReportedChapterCount(doc);

            var chapters = TryExtractChaptersFromBrowserRequests(reportedChapterCount);

            if (!chapters.Any() || (reportedChapterCount > 0 && chapters.Count < reportedChapterCount))
                chapters = MergeChapterEntries(chapters, TryExtractChaptersFromRsc(currentSeriesUrl));

            if (!chapters.Any() || (reportedChapterCount > 0 && chapters.Count < reportedChapterCount))
                chapters = MergeChapterEntries(chapters, ProbeChaptersByNumber(doc));

            if (!chapters.Any())
                throw new Exception("Failed to load the chapter list from RF DragonScan.");

            int id = 0;
            foreach (var chapter in chapters)
            {
                chapter.Name = NormalizeChapterName(chapter.Name, chapter.Url);
                chapterMap[id++] = chapter;
            }
        }

        private HtmlDocument LoadDocument(Uri url, Func<HtmlDocument, bool> validator, int maxAttempts = 30, int waitMs = 350)
        {
            browser.WaitForLoad(url.AbsoluteUri);
            SyncBrowserState();

            if (browser.IsCloudflareTriggered())
            {
                browser.BypassCloudflare();
                SyncBrowserState();
            }

            HtmlDocument lastDoc = null;
            int adblockRetries = 0;
            for (int i = 0; i < maxAttempts; i++)
            {
                ThreadTools.Wait(waitMs, true);
                SyncBrowserState();

                if (IsLoginUrl(browser.GetCurrentUrl()))
                {
                    EnsureLogin(url);
                    browser.WaitForLoad(url.AbsoluteUri);
                    ThreadTools.Wait(400, true);
                    SyncBrowserState();
                }

                lastDoc = browser.GetDocument();
                if (IsAdblockDetectedPage(lastDoc) && adblockRetries++ < 2)
                {
                    browser.WaitForLoad(url.AbsoluteUri, 20);
                    ThreadTools.Wait(900, true);
                    SyncBrowserState();
                    continue;
                }

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
            browser.WaitInitialize();
            browserUserAgent = ProxyTools.UserAgent;
        }

        private void PrepareProtectedBrowser()
        {
            EnsureBrowser();

            if (networkHooksInstalled)
                return;

            browser.RegisterWebRequestHandlerEvents((sender, args) =>
            {
                if (!networkCaptureActive)
                    return;

                try
                {
                    args.Headers["Accept-Encoding"] = "identity";
                }
                catch { }
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

        private void EnsureAuthenticated(Uri returnUrl)
        {
            if (isAuthenticated)
                return;

            EnsureBrowser();
            EnsureLogin(returnUrl ?? new Uri(SiteBase));
            SyncBrowserState();
            isAuthenticated = true;
        }

        private void EnsureLogin(Uri returnUrl)
        {
            var loginUrl = $"{SiteBase}/login?redirect={HttpUtility.UrlEncode(returnUrl.PathAndQuery)}";
            if (!IsLoginUrl(browser.GetCurrentUrl()))
                browser.WaitForLoad(loginUrl);

            browser.WaitForLoad(15);
            ThreadTools.Wait(1500, true);

            for (int attempt = 0; attempt < 8; attempt++)
            {
                ThreadTools.Wait(600, true);

                if (!IsLoginUrl(browser.GetCurrentUrl()))
                {
                    browser.WaitForLoad(15);
                    SyncBrowserState();
                    return;
                }

                var submitted = TrySubmitLoginInteractively();
                if (!submitted)
                    submitted = TrySubmitLoginWithScript();

                if (!submitted)
                    continue;

                for (int wait = 0; wait < 20; wait++)
                {
                    ThreadTools.Wait(500, true);
                    SyncBrowserState();

                    if (!IsLoginUrl(browser.GetCurrentUrl()))
                    {
                        browser.WaitForLoad(15);
                        SyncBrowserState();
                        return;
                    }

                    var doc = browser.GetDocument();
                    if (doc?.DocumentNode?.SelectSingleNode("//input[@type='password']") == null)
                    {
                        browser.WaitForLoad(15);
                        SyncBrowserState();
                        return;
                    }
                }
            }

            if (IsLoginUrl(browser.GetCurrentUrl()))
                throw new Exception("Failed to login on RF DragonScan.");

            SyncBrowserState();
        }

        private bool TrySubmitLoginInteractively()
        {
            try
            {
                browser.GetBounds("//input[@type='email']");
                browser.TypeInInput("//input[@type='email']".CreateTargetSelectorFromXPATH(), TestCredential, true);
                ThreadTools.Wait(200, true);
                browser.TypeInInput("//input[@type='password']".CreateTargetSelectorFromXPATH(), TestCredential, true);
                ThreadTools.Wait(250, true);

                var submitBounds = browser.GetBounds("//button[@type='submit' or normalize-space()='Acessar' or contains(., 'Acessar')]");
                browser.ExecuteClick(submitBounds);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TrySubmitLoginWithScript()
        {
            return browser.EvaluateScript<bool>(@"(function () {
    function normalize(text) {
        return (text || '').replace(/\s+/g, ' ').trim().toLowerCase();
    }

    function setValue(input, value) {
        if (!input)
            return false;

        var proto = input.tagName === 'TEXTAREA'
            ? window.HTMLTextAreaElement.prototype
            : window.HTMLInputElement.prototype;
        var descriptor = Object.getOwnPropertyDescriptor(proto, 'value');

        if (descriptor && descriptor.set)
            descriptor.set.call(input, value);
        else
            input.value = value;

        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new Event('change', { bubbles: true }));
        return true;
    }

    var email = document.querySelector('input[type=""email""], input[autocomplete=""email""], input[autocomplete=""username""], input[name=""email""]');
    var password = document.querySelector('input[type=""password""], input[autocomplete=""current-password""], input[name=""password""]');
    var submit = Array.from(document.querySelectorAll('button, input[type=""submit""]')).find(function (element) {
        var text = normalize(element.innerText || element.value || element.getAttribute('aria-label'));
        return text.indexOf('entrar') >= 0 || text.indexOf('login') >= 0 || text.indexOf('acessar') >= 0;
    });

    if (!email || !password || !submit)
        return false;

    var loginTab = Array.from(document.querySelectorAll('button')).find(function (element) {
        return normalize(element.textContent) === 'login';
    });
    if (loginTab)
        loginTab.click();

    setValue(email, '" + TestCredential + @"');
    setValue(password, '" + TestCredential + @"');

    email.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Tab' }));
    password.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Enter' }));
    password.dispatchEvent(new KeyboardEvent('keyup', { bubbles: true, key: 'Enter' }));

    var form = submit.closest('form');
    if (form && typeof form.requestSubmit === 'function')
        form.requestSubmit(submit);
    else if (form)
        form.submit();
    else
        submit.click();

    return true;
})();");
        }

        private void SyncBrowserState()
        {
            var browserCookies = browser.GetCookies();
            if (browserCookies != null)
                cookies = browserCookies.ToContainer();

            browserUserAgent = browser.GetUserAgent() ?? ProxyTools.UserAgent;
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

                capturedNetworkEntries.Add(new CapturedNetworkEntry()
                {
                    Url = url,
                    ContentType = args?.WebResponse?.ContentType,
                    Body = body
                });
            }

            if (Program.Debug)
            {
                Program.Writer?.WriteLine("[DragonScanNext] {0} | {1} | {2}", args?.WebResponse?.StatusCode, args?.WebResponse?.ContentType, url);
                Program.Writer?.Flush();
            }
        }

        private bool ShouldCaptureNetworkEntry(string url, string contentType)
        {
            var lowered = (url ?? string.Empty).ToLowerInvariant();
            return lowered.Contains("rfdragonscan.net") ||
                   lowered.Contains("api.rfdragonscan.net") ||
                   lowered.Contains("cdn.rfdragonscan.net");
        }

        private bool ShouldCaptureNetworkBody(string url, string contentType)
        {
            var loweredUrl = (url ?? string.Empty).ToLowerInvariant();
            var loweredType = (contentType ?? string.Empty).ToLowerInvariant();

            if (loweredUrl.Contains("cdn.rfdragonscan.net/storage/v1/object/public/rf-dragon/mangas/"))
                return false;

            return loweredType.Contains("json") ||
                   loweredType.Contains("javascript") ||
                   loweredType.Contains("text") ||
                   loweredType.Contains("html") ||
                   loweredType.Contains("x-component") ||
                   loweredUrl.Contains("_rsc");
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
            catch { }

            return Encoding.UTF8.GetString(bytes);
        }

        private HtmlDocument DownloadHtmlDocument(Uri url, string referer)
        {
            EnsureAuthenticated(url);

            var html = TryDownloadText(url, referer, "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            var doc = CreateDocument(html);
            if (!LooksLikeLoginPage(doc, html))
                return doc;

            isAuthenticated = false;
            EnsureAuthenticated(url);

            html = TryDownloadText(url, referer, "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            return CreateDocument(html);
        }

        private string TryDownloadText(Uri url, string referer, string accept, (string Key, string Value)[] extraHeaders = null)
        {
            return url.TryDownloadString(
                Referer: string.IsNullOrWhiteSpace(referer) ? SiteBase + "/" : referer,
                UserAgent: browserUserAgent ?? ProxyTools.UserAgent,
                Accept: accept,
                Headers: MergeHeaders(GetDefaultRequestHeaders(), extraHeaders),
                Cookie: cookies,
                TimeoutSecs: 45);
        }

        private static HtmlDocument CreateDocument(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(string.IsNullOrWhiteSpace(html) ? "<html></html>" : html);
            return doc;
        }

        private static bool LooksLikeLoginPage(HtmlDocument doc, string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return false;

            var title = doc?.DocumentNode.SelectSingleNode("//title")?.InnerText ?? string.Empty;
            if (title.IndexOf("Entrar", StringComparison.InvariantCultureIgnoreCase) >= 0 &&
                doc?.DocumentNode.SelectSingleNode("//input[@type='password']") != null)
            {
                return true;
            }

            var lowered = html.ToLowerInvariant();
            return lowered.Contains("acesse sua conta") &&
                   lowered.Contains("entre para continuar") &&
                   lowered.Contains("type=\"password\"");
        }
        private List<ChapterEntry> ExtractChaptersFromApiPayload(string payload)
        {
            var entries = new List<ChapterEntry>();
            var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            var normalized = HttpUtility.HtmlDecode(payload).Replace("\\/", "/");

            var urlMatches = Regex.Matches(
                normalized,
                @"/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/[^""'\s<>\\]+/capitulo/\d+(?:[.,]\d+)?",
                RegexOptions.IgnoreCase);

            foreach (Match match in urlMatches)
            {
                var absolute = new Uri(new Uri(SiteBase), match.Value).AbsoluteUri;
                if (!seen.Add(absolute))
                    continue;

                entries.Add(new ChapterEntry()
                {
                    Url = absolute,
                    Name = Regex.Match(absolute, @"(?<=/capitulo/)\d+(?:[.,]\d+)?", RegexOptions.IgnoreCase).Value
                });
            }

            if (entries.Any())
            {
                return entries
                    .OrderBy(x => GetChapterSortKey(x.Name, x.Url))
                    .ThenBy(x => x.Url, StringComparer.InvariantCultureIgnoreCase)
                    .ToList();
            }

            var numberMatches = Regex.Matches(
                normalized,
                @"""(?:chapterNumber|chapter|capitulo|capituloNumero|capituloNumber)""\s*:\s*""?(\d+(?:[.,]\d+)?)",
                RegexOptions.IgnoreCase);

            foreach (Match match in numberMatches)
            {
                var number = match.Groups[1].Value.Replace(',', '.');
                var absolute = new Uri($"{currentSeriesUrl.AbsoluteUri.TrimEnd('/')}/capitulo/{number}").AbsoluteUri;
                if (!seen.Add(absolute))
                    continue;

                entries.Add(new ChapterEntry()
                {
                    Url = absolute,
                    Name = number
                });
            }

            return entries
                .OrderBy(x => GetChapterSortKey(x.Name, x.Url))
                .ThenBy(x => x.Url, StringComparer.InvariantCultureIgnoreCase)
                .ToList();
        }

        private List<ChapterEntry> TryExtractChaptersFromBrowserRequests(int expectedCount)
        {
            PrepareProtectedBrowser();
            BeginNetworkCapture();

            try
            {
                LoadDocument(currentSeriesUrl, _ => true, 12, 500);
                OpenChapterTab();
                var visibleEntries = WaitAndCollectChapters();
                var requestEntries = ParseCapturedChapterEntries(GetCapturedNetworkEntriesSnapshot());
                var merged = MergeChapterEntries(visibleEntries, requestEntries);

                if (merged.Any() && (expectedCount <= 0 || merged.Count >= expectedCount))
                    return merged;

                ThreadTools.Wait(1200, true);
                OpenChapterTab();
                visibleEntries = MergeChapterEntries(visibleEntries, WaitAndCollectChapters());
                requestEntries = MergeChapterEntries(requestEntries, ParseCapturedChapterEntries(GetCapturedNetworkEntriesSnapshot()));
                return MergeChapterEntries(visibleEntries, requestEntries);
            }
            finally
            {
                EndNetworkCapture();
            }
        }

        private List<ChapterEntry> ParseCapturedChapterEntries(IEnumerable<CapturedNetworkEntry> captured)
        {
            var entries = new List<ChapterEntry>();
            foreach (var item in captured)
            {
                if (string.IsNullOrWhiteSpace(item?.Body))
                    continue;

                entries.AddRange(ExtractChaptersFromApiPayload(item.Body));
            }

            return MergeChapterEntries(entries);
        }

        private List<ChapterEntry> ProbeChaptersByNumber(HtmlDocument seriesDoc)
        {
            int reportedCount = ExtractReportedChapterCount(seriesDoc);
            if (reportedCount <= 0)
                return new List<ChapterEntry>();

            int probeMax = Math.Min(Math.Max(reportedCount + 12, reportedCount * 2), 400);
            var entries = new List<ChapterEntry>();

            for (int number = probeMax; number >= 1; number--)
            {
                var chapterUri = new Uri($"{currentSeriesUrl.AbsoluteUri.TrimEnd('/')}/capitulo/{number}");
                var chapterDoc = DownloadHtmlDocument(chapterUri, currentSeriesUrl.AbsoluteUri);
                if (!LooksLikeChapterDocument(chapterDoc))
                    continue;

                entries.Add(new ChapterEntry()
                {
                    Url = chapterUri.AbsoluteUri,
                    Name = number.ToString(CultureInfo.InvariantCulture)
                });
            }

            return entries
                .OrderBy(x => GetChapterSortKey(x.Name, x.Url))
                .ThenBy(x => x.Url, StringComparer.InvariantCultureIgnoreCase)
                .ToList();
        }

        private List<string> ExtractPageUrlsFromDocument(HtmlDocument doc)
        {
            var html = doc?.DocumentNode?.OuterHtml ?? string.Empty;
            var pages = new List<string>(ExtractPageUrlsFromText(html));
            var nodes = doc?.DocumentNode?.SelectNodes("//img[@src]");
            if (nodes == null)
                return pages;

            foreach (var node in nodes)
            {
                var src = HttpUtility.HtmlDecode(node.GetAttributeValue("src", null) ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(src))
                    continue;

                if (!src.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var lowered = src.ToLowerInvariant();
                if (!lowered.Contains("cdn.rfdragonscan.net/storage/v1/object/public/rf-dragon/mangas/"))
                    continue;

                if (pages.Contains(src, StringComparer.InvariantCultureIgnoreCase))
                    continue;

                pages.Add(src);
            }

            return pages;
        }

        private List<string> TryExtractPagesFromApi(Uri chapterUri)
        {
            foreach (var endpoint in BuildPageApiCandidates(chapterUri))
            {
                var payload = TryDownloadText(endpoint, chapterUri.AbsoluteUri, "application/json,text/plain;q=0.9,*/*;q=0.8");
                if (string.IsNullOrWhiteSpace(payload))
                    continue;

                var pages = ExtractPageUrlsFromText(payload);
                if (pages.Any())
                    return pages;
            }

            return new List<string>();
        }

        private List<string> TryExtractPagesFromBrowserRequests(Uri chapterUri, int expectedCount)
        {
            PrepareProtectedBrowser();
            BeginNetworkCapture();

            try
            {
                LoadDocument(chapterUri, _ => true, 12, 500);
                var visiblePages = WaitAndCollectPages();
                var requestPages = ParseCapturedPageUrls(GetCapturedNetworkEntriesSnapshot());
                var merged = MergePageUrls(visiblePages, requestPages);

                if (merged.Any() && (expectedCount <= 0 || merged.Count >= expectedCount))
                    return merged;

                ThreadTools.Wait(900, true);
                visiblePages = MergePageUrls(visiblePages, WaitAndCollectPages());
                requestPages = MergePageUrls(requestPages, ParseCapturedPageUrls(GetCapturedNetworkEntriesSnapshot()));
                return MergePageUrls(visiblePages, requestPages);
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
                if (!string.IsNullOrWhiteSpace(item?.Url))
                {
                    var normalized = item.Url.ToLowerInvariant();
                    if (normalized.Contains("cdn.rfdragonscan.net/storage/v1/object/public/rf-dragon/mangas/") &&
                        seen.Add(item.Url))
                    {
                        pages.Add(item.Url);
                    }
                }

                foreach (var page in ExtractPageUrlsFromText(item?.Body))
                {
                    if (seen.Add(page))
                        pages.Add(page);
                }
            }

            return pages;
        }

        private static List<ChapterEntry> MergeChapterEntries(params IEnumerable<ChapterEntry>[] sets)
        {
            var merged = new List<ChapterEntry>();
            var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var set in sets)
            {
                if (set == null)
                    continue;

                foreach (var entry in set)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Url))
                        continue;

                    if (!seen.Add(entry.Url))
                        continue;

                    merged.Add(new ChapterEntry()
                    {
                        Url = entry.Url,
                        Name = NormalizeChapterName(entry.Name, entry.Url),
                        Referer = entry.Referer
                    });
                }
            }

            return merged
                .OrderBy(x => GetChapterSortKey(x.Name, x.Url))
                .ThenBy(x => x.Url, StringComparer.InvariantCultureIgnoreCase)
                .ToList();
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

        private IEnumerable<Uri> BuildPageApiCandidates(Uri chapterUri)
        {
            var apiRoot = new Uri((apiBaseUrl ?? "https://api.rfdragonscan.net").Trim().TrimEnd('/', '\\') + "/");
            var seriesSegments = GetPathSegments(currentSeriesUrl);
            var chapterSegments = GetPathSegments(chapterUri);
            if (seriesSegments.Length < 2 || chapterSegments.Length < 4)
                yield break;

            var projectId = seriesSegments[0];
            var linkId = seriesSegments[1];
            var chapterNumber = chapterSegments[3];
            var candidates = new[]
            {
                $"chapters/{chapterNumber}",
                $"chapter/{chapterNumber}",
                $"reader/{chapterNumber}",
                $"projects/{projectId}/chapters/{chapterNumber}",
                $"projects/{projectId}/{linkId}/chapters/{chapterNumber}",
                $"project/{projectId}/chapters/{chapterNumber}",
                $"mangas/{projectId}/chapters/{chapterNumber}",
                $"manga/{projectId}/chapters/{chapterNumber}",
                $"obra/{projectId}/capitulos/{chapterNumber}",
                $"obras/{projectId}/capitulos/{chapterNumber}",
                $"content/{projectId}/chapters/{chapterNumber}",
                $"contents/{projectId}/chapters/{chapterNumber}"
            };

            var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var candidate in candidates)
            {
                var absolute = new Uri(apiRoot, candidate).AbsoluteUri;
                if (seen.Add(absolute))
                    yield return new Uri(absolute);
            }
        }

        private static List<string> ExtractPageUrlsFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            var normalized = HttpUtility.HtmlDecode(text).Replace("\\/", "/");
            var matches = Regex.Matches(
                normalized,
                @"https://cdn\.rfdragonscan\.net/storage/v1/object/public/rf-dragon/mangas/[^\s""'<>\\]+",
                RegexOptions.IgnoreCase);

            var pages = new List<string>();
            var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (Match match in matches)
            {
                var url = match.Value.TrimEnd('\\', '"', '\'', ')', ']', ',', ';');
                if (!seen.Add(url))
                    continue;

                pages.Add(url);
            }

            return pages;
        }

        private static int ExtractExpectedPageCount(HtmlDocument doc)
        {
            var pageNodes = doc?.DocumentNode?.SelectNodes("//*[@data-page-index]");
            if (pageNodes != null && pageNodes.Any())
                return pageNodes.Count;

            var imgs = doc?.DocumentNode?.SelectNodes("//img[contains(@src, 'rf-dragon/mangas/')]");
            return imgs?.Count ?? 0;
        }

        private static string ExtractApiBaseUrl(HtmlDocument doc)
        {
            var html = doc?.DocumentNode?.OuterHtml ?? string.Empty;
            if (string.IsNullOrWhiteSpace(html))
                return null;

            foreach (var marker in new[] { "NEXT_API_URL\\\":\\\"", "NEXT_API_URL\":\"", "NEXT_API_URL=" })
            {
                var index = html.IndexOf(marker, StringComparison.InvariantCultureIgnoreCase);
                if (index < 0)
                    continue;

                var start = index + marker.Length;
                var rest = html.Substring(start);
                var match = Regex.Match(rest, "https?://[^\\\\\\\"'&<>\\s]+", RegexOptions.IgnoreCase);
                if (match.Success)
                    return match.Value.Trim().TrimEnd('/', '\\');
            }

            return null;
        }

        private static int ExtractReportedChapterCount(HtmlDocument doc)
        {
            var html = doc?.DocumentNode?.OuterHtml ?? string.Empty;
            if (string.IsNullOrWhiteSpace(html))
                return 0;

            var match = Regex.Match(
                HttpUtility.HtmlDecode(html),
                @"Cap[íi]?tulos\s*<[^>]+>\s*(\d+)\s*<",
                RegexOptions.IgnoreCase);

            if (!match.Success)
                match = Regex.Match(HttpUtility.HtmlDecode(html), @"Cap[íi]?tulos\s*(\d+)", RegexOptions.IgnoreCase);

            return match.Success && int.TryParse(match.Groups[1].Value, out int value)
                ? value
                : 0;
        }

        private static bool LooksLikeChapterDocument(HtmlDocument doc)
        {
            if (doc == null)
                return false;

            if (ExtractExpectedPageCount(doc) > 0)
                return true;

            var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText ?? string.Empty;
            if (title.IndexOf("RF DragoN", StringComparison.InvariantCultureIgnoreCase) < 0)
                return false;

            var text = NormalizeText(doc.DocumentNode.InnerText);
            return text.IndexOf("Capítulo", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                   text.IndexOf("Pagina", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                   text.IndexOf("Página", StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        private List<ChapterEntry> TryExtractChaptersFromRsc(Uri seriesUri)
        {
            var entries = new List<ChapterEntry>();
            var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var headers in BuildSeriesRscHeaderCandidates(seriesUri))
            {
                var payload = DownloadRscPayload(seriesUri, headers);
                if (string.IsNullOrWhiteSpace(payload))
                    continue;

                var normalized = HttpUtility.HtmlDecode(payload).Replace("\\/", "/");
                var matches = Regex.Matches(
                    normalized,
                    @"/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/[^""'\s<>\\]+/capitulo/\d+(?:[.,]\d+)?",
                    RegexOptions.IgnoreCase);

                foreach (Match match in matches)
                {
                    var absolute = new Uri(new Uri(SiteBase), match.Value).AbsoluteUri;
                    if (!seen.Add(absolute))
                        continue;

                    var chapterNumber = Regex.Match(absolute, @"(?<=/capitulo/)\d+(?:[.,]\d+)?", RegexOptions.IgnoreCase).Value;
                    entries.Add(new ChapterEntry()
                    {
                        Url = absolute,
                        Name = chapterNumber
                    });
                }

                if (entries.Any())
                    break;
            }

            return entries
                .OrderBy(x => GetChapterSortKey(x.Name, x.Url))
                .ThenBy(x => x.Url, StringComparer.InvariantCultureIgnoreCase)
                .ToList();
        }

        private List<string> TryExtractPagesFromRsc(Uri chapterUri, int minimumCount)
        {
            foreach (var headers in BuildChapterRscHeaderCandidates(chapterUri))
            {
                var payload = DownloadRscPayload(chapterUri, headers);
                if (string.IsNullOrWhiteSpace(payload))
                    continue;

                var pages = ExtractPageUrlsFromText(payload);
                if (pages.Count >= minimumCount || pages.Any())
                    return pages;
            }

            return new List<string>();
        }

        private string DownloadRscPayload(Uri url, (string Key, string Value)[] headers)
        {
            var rscUrl = AppendQueryParameter(url, "_rsc", Guid.NewGuid().ToString("N"));
            return TryDownloadText(rscUrl, url.AbsoluteUri, "text/x-component,text/plain;q=0.9,*/*;q=0.8", headers);
        }

        private IEnumerable<(string Key, string Value)[]> BuildSeriesRscHeaderCandidates(Uri seriesUri)
        {
            var baseHeaders = new[]
            {
                ("RSC", "1"),
                ("Next-Url", seriesUri.PathAndQuery),
                ("X-Requested-With", "XMLHttpRequest")
            };

            yield return baseHeaders;

            var segments = GetPathSegments(seriesUri);
            if (segments.Length < 2)
                yield break;

            yield return MergeHeaders(
                baseHeaders,
                new[]
                {
                    ("Next-Router-State-Tree", BuildSeriesRouterState(segments[0], segments[1]))
                });
        }

        private IEnumerable<(string Key, string Value)[]> BuildChapterRscHeaderCandidates(Uri chapterUri)
        {
            var baseHeaders = new[]
            {
                ("RSC", "1"),
                ("Next-Url", chapterUri.PathAndQuery),
                ("X-Requested-With", "XMLHttpRequest")
            };

            yield return baseHeaders;

            var segments = GetPathSegments(chapterUri);
            if (segments.Length < 4)
                yield break;

            var chapterNumber = segments[3];
            foreach (var chapterParamName in new[] { "chapterId", "chapter", "id", "number" })
            {
                yield return MergeHeaders(
                    baseHeaders,
                    new[]
                    {
                        ("Next-Router-State-Tree", BuildChapterRouterState(segments[0], segments[1], chapterNumber, chapterParamName))
                    });
            }
        }

        private static string BuildSeriesRouterState(string projectId, string linkId)
        {
            return $"[\"\",{{\"children\":[[\"projectId\",{JsonConvert.ToString(projectId)},\"d\"],{{\"children\":[[\"linkId\",{JsonConvert.ToString(linkId)},\"d\"],{{\"children\":[\"__PAGE__\",{{}}]}}]}}]}},null,null,true]";
        }

        private static string BuildChapterRouterState(string projectId, string linkId, string chapterNumber, string chapterParamName)
        {
            return $"[\"\",{{\"children\":[[\"projectId\",{JsonConvert.ToString(projectId)},\"d\"],{{\"children\":[[\"linkId\",{JsonConvert.ToString(linkId)},\"d\"],{{\"children\":[\"capitulo\",{{\"children\":[[{JsonConvert.ToString(chapterParamName)},{JsonConvert.ToString(chapterNumber)},\"d\"],{{\"children\":[\"__PAGE__\",{{}}]}}]}}]}}]}}]}},null,null,true]";
        }

        private static Uri AppendQueryParameter(Uri uri, string key, string value)
        {
            var builder = new UriBuilder(uri);
            var query = HttpUtility.ParseQueryString(builder.Query ?? string.Empty);
            query[key] = value;
            builder.Query = query.ToString();
            return builder.Uri;
        }

        private static (string Key, string Value)[] GetDefaultRequestHeaders()
        {
            return new[]
            {
                ("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7"),
                ("Accept-Encoding", "identity"),
                ("Cache-Control", "no-cache"),
                ("Pragma", "no-cache")
            };
        }

        private static (string Key, string Value)[] MergeHeaders(params (string Key, string Value)[][] sets)
        {
            var merged = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var set in sets)
            {
                if (set == null)
                    continue;

                foreach (var header in set)
                    merged[header.Key] = header.Value;
            }

            return merged
                .Select(x => (x.Key, x.Value))
                .ToArray();
        }

        private static string NormalizeText(string text)
        {
            return Regex.Replace(HttpUtility.HtmlDecode(text ?? string.Empty), @"\s+", " ").Trim();
        }

        private Uri ResolveSeriesUri(Uri uri)
        {
            var segments = GetPathSegments(uri);
            if (segments.Length < 2)
                throw new Exception("Unsupported RF DragonScan URL.");

            return new Uri($"{uri.GetLeftPart(UriPartial.Authority)}/{segments[0]}/{segments[1]}");
        }

        private bool HasSeriesMetadata(HtmlDocument doc)
        {
            var title = ExtractSeriesTitle(doc);
            var cover = ExtractCoverUrl(doc);

            return !string.IsNullOrWhiteSpace(title) &&
                   !string.IsNullOrWhiteSpace(cover) &&
                   !title.StartsWith("RF DragoN", StringComparison.InvariantCultureIgnoreCase) &&
                   cover.IndexOf("/og-image", StringComparison.InvariantCultureIgnoreCase) < 0;
        }

        private bool IsAdblockDetectedPage(HtmlDocument doc)
        {
            var html = doc?.ToHTML();
            if (string.IsNullOrWhiteSpace(html))
                return false;

            var lowered = html.ToLowerInvariant();
            var mentionsAdblock = lowered.Contains("adblock") ||
                                  lowered.Contains("ad block") ||
                                  lowered.Contains("bloqueador");
            var looksLikeWarning = lowered.Contains("detectado") ||
                                   lowered.Contains("detected") ||
                                   lowered.Contains("desative") ||
                                   lowered.Contains("disable") ||
                                   lowered.Contains("desabilite");

            return mentionsAdblock && looksLikeWarning;
        }

        private string ExtractSeriesTitle(HtmlDocument doc)
        {
            var node = doc.SelectSingleNode("//meta[@property='og:title']") ??
                       doc.SelectSingleNode("//meta[@name='twitter:title']") ??
                       doc.SelectSingleNode("//main//h1") ??
                       doc.SelectSingleNode("//h1");

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

        private string ExtractCoverUrl(HtmlDocument doc)
        {
            var node = doc.SelectSingleNode("//meta[@property='og:image']") ??
                       doc.SelectSingleNode("//meta[@name='twitter:image']") ??
                       doc.SelectSingleNode("//main//img[@src]");

            if (node == null)
                return null;

            var url = node.Name == "meta"
                ? node.GetAttributeValue("content", null)
                : node.GetAttributeValue("src", null);

            if (string.IsNullOrWhiteSpace(url))
                return null;

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri absolute))
                return absolute.AbsoluteUri;

            return new Uri(currentSeriesUrl, url).AbsoluteUri;
        }

        private void OpenChapterTab()
        {
            var clicked = browser.EvaluateScript<bool>(@"(function () {
    if (document.querySelectorAll('a[href*=""/capitulo/""]').length > 0)
        return true;

    function normalize(text) {
        return (text || '').replace(/\s+/g, ' ').trim().toLowerCase();
    }

    var button = Array.from(document.querySelectorAll('button')).find(function (element) {
        var text = normalize(element.textContent);
        return text.indexOf('capítulos') === 0 || text.indexOf('capitulos') === 0;
    });

    if (!button)
        return false;

    button.click();
    return true;
})();");

            if (clicked)
                ThreadTools.Wait(2500, true);
        }

        private List<ChapterEntry> WaitAndCollectChapters()
        {
            var lastCount = -1;
            var stableRounds = 0;
            var entries = new List<ChapterEntry>();

            for (int i = 0; i < 20 && stableRounds < 4; i++)
            {
                ThreadTools.Wait(i == 0 ? 2000 : 600, true);

                var current = ExtractVisibleChapters();
                if (!current.Any())
                {
                    OpenChapterTab();
                    continue;
                }

                if (current.Count == lastCount)
                    stableRounds++;
                else
                    stableRounds = 0;

                lastCount = current.Count;
                entries = current;
            }

            return entries
                .GroupBy(x => x.Url, StringComparer.InvariantCultureIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => GetChapterSortKey(x.Name, x.Url))
                .ThenBy(x => x.Url, StringComparer.InvariantCultureIgnoreCase)
                .ToList();
        }

        private List<ChapterEntry> ExtractVisibleChapters()
        {
            const string script = @"(function () {
    function normalize(text) {
        return (text || '').replace(/\s+/g, ' ').trim();
    }

    var items = [];
    var seen = {};

    document.querySelectorAll('a[href*=""/capitulo/""]').forEach(function (anchor) {
        var href = anchor.href || anchor.getAttribute('href') || '';
        if (!href)
            return;

        var text = normalize(anchor.textContent);
        if (!text)
            return;

        var match = text.match(/cap[íi]?tulo\s*([0-9]+(?:[.,][0-9]+)?)/i) ||
                    text.match(/([0-9]+(?:[.,][0-9]+)?)/);
        var name = match ? match[1].replace(',', '.') : text;
        var absolute = new URL(href, location.href).href;

        if (seen[absolute])
            return;

        seen[absolute] = true;
        items.push({ Url: absolute, Name: name });
    });

    return JSON.stringify(items);
})();";

            var json = browser.EvaluateScript<string>(script);
            if (string.IsNullOrWhiteSpace(json))
                return new List<ChapterEntry>();

            return JsonConvert.DeserializeObject<List<ChapterEntry>>(json) ?? new List<ChapterEntry>();
        }

        private List<string> WaitAndCollectPages()
        {
            var pages = new List<string>();
            var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            var stableRounds = 0;

            ScrollWindowToTop();
            ThreadTools.Wait(1800, true);

            for (int i = 0; i < 90 && stableRounds < 6; i++)
            {
                ThreadTools.Wait(i == 0 ? 1800 : 350, true);

                var beforeCount = pages.Count;
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
    function getSource(img) {
        return img.currentSrc || img.getAttribute('src') || img.getAttribute('data-src') || '';
    }

    var items = [];
    var seen = {};

    document.querySelectorAll('img').forEach(function (img) {
        var src = getSource(img);
        if (!src || src.indexOf('http') !== 0)
            return;

        var lowered = src.toLowerCase();
        if (lowered.indexOf('cdn.rfdragonscan.net') === -1)
            return;

        if (lowered.indexOf('/logo') !== -1 || lowered.indexOf('/avatar') !== -1 || lowered.indexOf('icon') !== -1)
            return;

        if (img.closest('nav, header, footer, aside'))
            return;

        var rect = img.getBoundingClientRect();
        var width = Math.max(rect.width || 0, img.naturalWidth || 0, img.width || 0);
        var height = Math.max(rect.height || 0, img.naturalHeight || 0, img.height || 0);

        if (width < 200 && height < 200)
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
    var step = Math.max(500, Math.floor(view * 0.85));

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
            browser.EvaluateScript($@"(function () {{
    window.scrollBy(0, {Math.Max(250, step)});
}})();");
        }

        private void ScrollWindowToTop()
        {
            browser.EvaluateScript(@"(function () {
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

        private static double GetChapterSortKey(string rawName, string url)
        {
            var chapterNumber = ExtractCanonicalChapterNumber(rawName, url);
            if (!string.IsNullOrWhiteSpace(chapterNumber) &&
                double.TryParse(chapterNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                return value;
            }

            return double.MaxValue;
        }

        private static string NormalizeChapterName(string rawName, string url)
        {
            var chapterNumber = ExtractCanonicalChapterNumber(rawName, url);
            if (!string.IsNullOrWhiteSpace(chapterNumber))
                return chapterNumber;

            return DataTools.GetRawName(HttpUtility.HtmlDecode(rawName ?? string.Empty)).Trim();
        }

        private static string ExtractCanonicalChapterNumber(string rawName, string url)
        {
            var urlMatch = Regex.Match(url ?? string.Empty, @"(?<=/capitulo/)\d+(?:[.,]\d+)?", RegexOptions.IgnoreCase);
            if (urlMatch.Success)
                return urlMatch.Value.Replace(',', '.');

            var rawText = HttpUtility.HtmlDecode(rawName ?? string.Empty);
            var titledMatch = Regex.Match(rawText, @"cap[íi]?tulo\s*(\d+(?:[.,]\d+)?)", RegexOptions.IgnoreCase);
            if (titledMatch.Success)
                return titledMatch.Groups[1].Value.Replace(',', '.');

            var fallbackMatch = Regex.Match(rawText, @"\d+(?:[.,]\d+)?");
            return fallbackMatch.Success
                ? fallbackMatch.Value.Replace(',', '.')
                : null;
        }

        private static bool IsSupportedHost(Uri uri)
        {
            return uri.Host.Equals("rfdragonscan.net", StringComparison.InvariantCultureIgnoreCase) ||
                   uri.Host.EndsWith(".rfdragonscan.net", StringComparison.InvariantCultureIgnoreCase);
        }

        private static string[] GetPathSegments(Uri uri)
        {
            return uri.AbsolutePath
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                .ToArray();
        }

        private static bool IsLoginUrl(string url)
        {
            return !string.IsNullOrWhiteSpace(url) &&
                   url.IndexOf("/login", StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        private sealed class ChapterEntry
        {
            public string Url { get; set; }
            public string Name { get; set; }
            public string Referer { get; set; }
        }

        private sealed class CapturedNetworkEntry
        {
            public string Url { get; set; }
            public string ContentType { get; set; }
            public string Body { get; set; }
        }

        private sealed class ScrollState
        {
            public bool AtBottom { get; set; }
            public int Step { get; set; }
        }
    }
}
