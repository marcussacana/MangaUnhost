using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace MangaUnhost.Hosts
{
    internal class ArgosComic : IHost
    {
        private const string SiteBase = "https://aniargos.com";
        private const string DefaultEmail = "mangaunhost@gmail.com";
        private const string DefaultPassword = "MangaUnhost123!";
        private const string DefaultUsername = "MangaUnhost";

        private string loginActionId = "605173214da75861e74758282f14617d9d0dfa5607";
        private string registerActionId = "70003b1ccfac60b47fc4b287484bd30ba795394267";
        private string getAllChaptersActionId = "606c13e60309ce062fade63ac2f1cc68bbc5dc25f4";
        private string getPagesActionId = "6062e8559136ee33cc337e5520fb09950c3dced65e";

        private readonly Dictionary<int, ChapterInfo> chapterMap = new Dictionary<int, ChapterInfo>();
        private readonly Dictionary<int, string[]> pageMap = new Dictionary<int, string[]>();

        private CookieContainer cookies = new CookieContainer();
        private bool isAuthenticated = false;
        private Uri currentSeriesUrl;
        private string currentProjectId;
        private string currentLinkId;

        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            var chapter = chapterMap.ContainsKey(ID) ? chapterMap[ID] : null;
            var referer = chapter?.Url ?? currentSeriesUrl?.AbsoluteUri ?? SiteBase + "/";

            foreach (var page in GetChapterPages(ID))
            {
                var data = DownloadBinary(page, referer);
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
                Name = "Argos Comic",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                GenericPlugin = false,
                Version = new Version(1, 0, 0)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            if (!IsValidUri(URL))
                return false;

            return !string.IsNullOrWhiteSpace(HTML) &&
                   (HTML.Contains("Argos Comic") || HTML.Contains("aniargos.com") || HTML.Contains("argos-comic"));
        }

        public bool IsValidUri(Uri Uri)
        {
            if (Uri == null || !IsSupportedHost(Uri))
                return false;

            if (Uri.AbsolutePath.StartsWith("/login", StringComparison.OrdinalIgnoreCase) ||
                Uri.AbsolutePath.StartsWith("/projetos", StringComparison.OrdinalIgnoreCase) ||
                Uri.AbsolutePath.StartsWith("/parceiros", StringComparison.OrdinalIgnoreCase))
                return false;

            var segments = GetPathSegments(Uri);
            if (segments.Length < 2 || !Guid.TryParse(segments[0], out _))
                return false;

            if (segments.Length == 2)
                return true;

            return segments.Length >= 4 &&
                   segments[2].Equals("capitulo", StringComparison.OrdinalIgnoreCase);
        }

        public ComicInfo LoadUri(Uri Uri)
        {
            chapterMap.Clear();
            pageMap.Clear();

            currentSeriesUrl = ResolveSeriesUri(Uri);
            var segments = GetPathSegments(currentSeriesUrl);
            currentProjectId = segments[0];
            currentLinkId = segments[1];

            EnsureAuthenticated();

            var html = currentSeriesUrl.TryDownloadString(
                Referer: SiteBase + "/",
                UserAgent: ProxyTools.UserAgent,
                Cookie: cookies);

            var doc = new HtmlDocument();
            doc.LoadHtml(html ?? string.Empty);

            var title = ExtractTitle(doc);
            var coverUrl = ExtractCoverUrl(doc);

            return new ComicInfo()
            {
                Title = title,
                Cover = DownloadBinary(coverUrl, currentSeriesUrl.AbsoluteUri),
                ContentType = ContentType.Comic,
                Url = currentSeriesUrl
            };
        }

        private void LoadChapterMap()
        {
            EnsureAuthenticated();

            var payload = JsonConvert.SerializeObject(new object[] { currentProjectId, currentLinkId });
            var resp = PostServerAction(currentSeriesUrl, getAllChaptersActionId, payload, currentSeriesUrl.AbsoluteUri);

            var json = ExtractJsonFromRsc(resp);
            if (json?["groups"] == null)
            {
                TryDiscoverActions();
                resp = PostServerAction(currentSeriesUrl, getAllChaptersActionId, payload, currentSeriesUrl.AbsoluteUri);
                json = ExtractJsonFromRsc(resp);
            }

            if (json?["groups"] == null)
                throw new Exception("Falha ao obter lista de capítulos do Argos Comic.");

            var chapters = new List<ChapterInfo>();
            var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var groups = json["groups"] as JArray;
            if (groups != null)
            {
                foreach (var group in groups)
                {
                    var chapArray = group["chapters"] as JArray;
                    if (chapArray == null)
                        continue;

                    foreach (var ch in chapArray)
                    {
                        bool isUpcoming = ch["isUpcoming"]?.Value<bool>() ?? false;
                        if (isUpcoming)
                            continue;

                        string accessType = ch["accessType"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(accessType) && !accessType.Equals("FREE", StringComparison.OrdinalIgnoreCase))
                            continue;

                        bool hasRestriction = ch["hasRestriction"]?.Value<bool>() ?? false;
                        if (hasRestriction)
                            continue;

                        var rawTitle = ch["title"]?.ToString() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(rawTitle))
                            continue;

                        string chapterUrl = $"{currentSeriesUrl.AbsoluteUri.TrimEnd('/')}/capitulo/{FormatChapterForUrl(rawTitle)}";
                        if (!seenUrls.Add(chapterUrl))
                            continue;

                        var entry = new ChapterInfo
                        {
                            ChapterId = ch["id"]?.ToString(),
                            Title = rawTitle,
                            Name = rawTitle.Trim(),
                            Url = chapterUrl,
                            SortNumber = ParseChapterSortNumber(rawTitle)
                        };

                        chapters.Add(entry);
                    }
                }
            }

            var sorted = chapters
                .OrderBy(x => x.SortNumber)
                .ThenBy(x => x.Title)
                .ToList();

            int id = 0;
            chapterMap.Clear();
            foreach (var chapter in sorted)
            {
                chapter.InternalId = id;
                chapterMap[id++] = chapter;
            }
        }

        private string[] GetChapterPages(int ID)
        {
            if (pageMap.ContainsKey(ID))
                return pageMap[ID];

            if (!chapterMap.ContainsKey(ID))
                LoadChapterMap();

            if (!chapterMap.ContainsKey(ID))
                throw new Exception($"Capítulo com ID {ID} não encontrado.");

            var chapter = chapterMap[ID];
            EnsureAuthenticated();

            var chapterUri = new Uri(chapter.Url);
            var chapterNumberParam = chapter.Title.ToString().Replace('-', '.');
            var payload = JsonConvert.SerializeObject(new object[] { currentProjectId, chapterNumberParam });

            var resp = PostServerAction(chapterUri, getPagesActionId, payload, chapter.Url);
            var json = ExtractJsonFromRsc(resp);

            if (json?["pages"] == null)
            {
                isAuthenticated = false;
                EnsureAuthenticated();
                TryDiscoverActions();

                resp = PostServerAction(chapterUri, getPagesActionId, payload, chapter.Url);
                json = ExtractJsonFromRsc(resp);
            }

            var pagesArray = json?["pages"] as JArray;
            if (pagesArray == null || pagesArray.Count == 0)
                throw new Exception($"Falha ao carregar páginas do capítulo {chapter.Name} do Argos Comic.");

            var pageUrls = new List<string>();
            var sortedPages = pagesArray
                .OrderBy(p => p["pageNumber"]?.Value<int>() ?? 0);

            foreach (var page in sortedPages)
            {
                var photoUrl = page["photo"]?.ToString();
                if (!string.IsNullOrWhiteSpace(photoUrl))
                    pageUrls.Add(photoUrl);
            }

            if (!pageUrls.Any())
                throw new Exception($"Nenhuma página válida encontrada para o capítulo {chapter.Name}.");

            pageMap[ID] = pageUrls.ToArray();
            return pageMap[ID];
        }

        private void EnsureAuthenticated()
        {
            if (isAuthenticated)
                return;

            var loginUri = new Uri($"{SiteBase}/login");
            var loginPayload = JsonConvert.SerializeObject(new object[] { DefaultEmail, DefaultPassword });
            var resp = PostServerAction(loginUri, loginActionId, loginPayload);

            var json = ExtractJsonFromRsc(resp);
            if (json?["user"] != null)
            {
                isAuthenticated = true;
                return;
            }

            var regPayload = JsonConvert.SerializeObject(new object[] { DefaultUsername, DefaultEmail, DefaultPassword });
            resp = PostServerAction(loginUri, registerActionId, regPayload);
            json = ExtractJsonFromRsc(resp);
            if (json?["user"] != null)
            {
                isAuthenticated = true;
                return;
            }

            resp = PostServerAction(loginUri, loginActionId, loginPayload);
            json = ExtractJsonFromRsc(resp);
            if (json?["user"] != null)
            {
                isAuthenticated = true;
                return;
            }

            var cookieHeader = cookies.GetCookieHeader(new Uri(SiteBase));
            if (cookieHeader.Contains("access_token"))
            {
                isAuthenticated = true;
                return;
            }

            TryDiscoverActions();
            resp = PostServerAction(loginUri, loginActionId, loginPayload);
            cookieHeader = cookies.GetCookieHeader(new Uri(SiteBase));
            if (cookieHeader.Contains("access_token"))
            {
                isAuthenticated = true;
                return;
            }
        }

        private void TryDiscoverActions()
        {
            try
            {
                var targetUrl = currentSeriesUrl ?? new Uri(SiteBase);
                var html = targetUrl.TryDownloadString(
                    Referer: SiteBase + "/",
                    UserAgent: ProxyTools.UserAgent,
                    Cookie: cookies);

                if (string.IsNullOrWhiteSpace(html))
                    return;

                var chunkMatches = Regex.Matches(html, @"/_next/static/chunks/[a-zA-Z0-9.\-_]+\.js", RegexOptions.IgnoreCase);
                var chunkUrls = chunkMatches.Cast<Match>().Select(m => m.Value).Distinct().ToList();

                try
                {
                    var loginHtml = new Uri($"{SiteBase}/login").TryDownloadString(
                        Referer: SiteBase + "/",
                        UserAgent: ProxyTools.UserAgent,
                        Cookie: cookies);

                    if (!string.IsNullOrWhiteSpace(loginHtml))
                    {
                        var loginChunks = Regex.Matches(loginHtml, @"/_next/static/chunks/[a-zA-Z0-9.\-_]+\.js", RegexOptions.IgnoreCase);
                        foreach (Match m in loginChunks)
                        {
                            if (!chunkUrls.Contains(m.Value))
                                chunkUrls.Add(m.Value);
                        }
                    }
                }
                catch { }

                if (currentSeriesUrl != null)
                {
                    try
                    {
                        var chapRsc = new Uri($"{currentSeriesUrl.AbsoluteUri.TrimEnd('/')}/capitulo/1?_rsc=1").TryDownloadString(
                            Referer: currentSeriesUrl.AbsoluteUri,
                            UserAgent: ProxyTools.UserAgent,
                            Headers: new[] { ("RSC", "1") },
                            Cookie: cookies);

                        if (!string.IsNullOrWhiteSpace(chapRsc))
                        {
                            var chapChunks = Regex.Matches(chapRsc, @"/_next/static/chunks/[a-zA-Z0-9.\-_]+\.js", RegexOptions.IgnoreCase);
                            foreach (Match m in chapChunks)
                            {
                                if (!chunkUrls.Contains(m.Value))
                                    chunkUrls.Add(m.Value);
                            }
                        }
                    }
                    catch { }
                }

                foreach (var chunk in chunkUrls)
                {
                    try
                    {
                        var chunkUri = new Uri($"{SiteBase}{chunk}");
                        var chunkJs = chunkUri.TryDownloadString(
                            Referer: SiteBase + "/",
                            UserAgent: ProxyTools.UserAgent,
                            Cookie: cookies);

                        if (string.IsNullOrWhiteSpace(chunkJs))
                            continue;

                        var actionMatches = Regex.Matches(chunkJs, @"createServerReference\)?\(\s*""([a-f0-9]+)""[^)]*?,\s*""([^""]+)""\)");
                        foreach (Match am in actionMatches)
                        {
                            var actionHash = am.Groups[1].Value;
                            var actionName = am.Groups[2].Value;

                            switch (actionName)
                            {
                                case "getAllChapters":
                                    getAllChaptersActionId = actionHash;
                                    break;
                                case "getPages":
                                    getPagesActionId = actionHash;
                                    break;
                                case "login":
                                    loginActionId = actionHash;
                                    break;
                                case "registerWithAutoLogin":
                                    registerActionId = actionHash;
                                    break;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private string PostServerAction(Uri url, string actionId, string jsonBody, string referer = null)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.Timeout = 1000 * 30;
            request.CookieContainer = cookies;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ContentType = "application/json";
            request.UserAgent = ProxyTools.UserAgent;
            request.Referer = referer ?? SiteBase + "/";
            request.Headers["Next-Action"] = actionId;
            request.Headers["Origin"] = SiteBase;

            var postBytes = Encoding.UTF8.GetBytes(jsonBody);
            request.ContentLength = postBytes.Length;
            using (var stream = request.GetRequestStream())
                stream.Write(postBytes, 0, postBytes.Length);

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    var newCookies = response.Headers.GetSetCookies(response.ResponseUri);
                    if (newCookies != null)
                    {
                        foreach (var c in newCookies)
                            cookies.Add(c);
                    }

                    using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                        return reader.ReadToEnd();
                }
            }
            catch (WebException ex) when (ex.Response is HttpWebResponse errRes)
            {
                using (var reader = new StreamReader(errRes.GetResponseStream(), Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }

        private static JToken ExtractJsonFromRsc(string rsc)
        {
            if (string.IsNullOrWhiteSpace(rsc))
                return null;

            var lines = rsc.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("1:"))
                {
                    try
                    {
                        return JToken.Parse(trimmed.Substring(2));
                    }
                    catch { }
                }
            }

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                int colonIdx = trimmed.IndexOf(':');
                if (colonIdx > 0 && colonIdx <= 4)
                {
                    try
                    {
                        return JToken.Parse(trimmed.Substring(colonIdx + 1));
                    }
                    catch { }
                }

                try
                {
                    return JToken.Parse(trimmed);
                }
                catch { }
            }

            return null;
        }

        private byte[] DownloadBinary(string url, string referer)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            return url.TryDownload(
                Referer: referer ?? SiteBase + "/",
                UserAgent: ProxyTools.UserAgent,
                Cookie: cookies);
        }

        private static string ExtractTitle(HtmlDocument doc)
        {
            var node = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']") ??
                       doc.DocumentNode.SelectSingleNode("//meta[@name='twitter:title']") ??
                       doc.DocumentNode.SelectSingleNode("//h1") ??
                       doc.DocumentNode.SelectSingleNode("//title");

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

        private static string ExtractCoverUrl(HtmlDocument doc)
        {
            var node = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']") ??
                       doc.DocumentNode.SelectSingleNode("//meta[@name='twitter:image']") ??
                       doc.DocumentNode.SelectSingleNode("//main//img[@src]");

            if (node == null)
                return null;

            var url = node.Name == "meta"
                ? node.GetAttributeValue("content", null)
                : node.GetAttributeValue("src", null);

            return url?.Trim();
        }

        private static string FormatChapterForUrl(string title)
        {
            return (title ?? string.Empty).Replace('.', '-').Trim();
        }

        private static double ParseChapterSortNumber(string rawTitle)
        {
            var normalized = (rawTitle ?? string.Empty).Replace(',', '.').Trim();
            var match = Regex.Match(normalized, @"\d+(?:\.\d+)?");
            if (match.Success && double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                return val;

            return 0;
        }

        private static Uri ResolveSeriesUri(Uri uri)
        {
            var segments = GetPathSegments(uri);
            if (segments.Length < 2)
                throw new Exception("URL do Argos Comic inválida.");

            return new Uri($"{uri.GetLeftPart(UriPartial.Authority)}/{segments[0]}/{segments[1]}");
        }

        private static bool IsSupportedHost(Uri uri)
        {
            var host = uri.Host.ToLowerInvariant();
            return host == "aniargos.com" ||
                   host.EndsWith(".aniargos.com") ||
                   host == "argoscomic.com" ||
                   host.EndsWith(".argoscomic.com") ||
                   host == "argosscan.com" ||
                   host.EndsWith(".argosscan.com");
        }

        private static string[] GetPathSegments(Uri uri)
        {
            return uri.AbsolutePath
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                .ToArray();
        }

        private sealed class ChapterInfo
        {
            public int InternalId { get; set; }
            public string ChapterId { get; set; }
            public string Title { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
            public double SortNumber { get; set; }
        }
    }
}
