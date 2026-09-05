using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MangaUnhost.Hosts
{
    internal class Vegitoons : IHost
    {
        private readonly Dictionary<int, string[]> pageMap = new Dictionary<int, string[]>();
        private BookInfo currentBookInfo;
        private CloudflareData? CFData = null;
        private string currentHost;
        private string currentBook;
        private string API;
        private string CDN;

        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            var pages = GetChapterPages(ID);
            foreach (var page in pages)
            {
                var data = DownloadBinary(page);
                if (data != null && data.Length > 0)
                    yield return data;
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            if (currentBookInfo?.capitulos == null && !string.IsNullOrEmpty(currentBook))
                LoadBookInfo(currentBook);

            if (currentBookInfo?.capitulos == null)
                return Enumerable.Empty<KeyValuePair<int, string>>();

            var result = new List<KeyValuePair<int, string>>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var sorted = currentBookInfo.capitulos
                .Where(x => x.cap_liberado)
                .OrderByDescending(x => ParseChapterNumber(x))
                .ThenByDescending(x => x.cap_id);

            foreach (var chap in sorted)
            {
                var name = GetChapterString(chap);
                if (seen.Contains(name))
                {
                    if (!string.IsNullOrWhiteSpace(chap.cap_nome) && !seen.Contains(chap.cap_nome))
                        name = chap.cap_nome.Trim();
                    else
                        name = $"{name} ({chap.cap_id})";
                }
                seen.Add(name);
                result.Add(new KeyValuePair<int, string>(chap.cap_id, name));
            }

            return result;
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
                Author = "Marcussacana",
                GenericPlugin = false,
                Name = "Vegitoons",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0, 0)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            return IsValidUri(URL);
        }

        public bool IsValidUri(Uri Uri)
        {
            if (Uri == null)
                return false;

            var host = Uri.Host.ToLowerInvariant();
            var path = Uri.AbsolutePath.ToLowerInvariant();

            return host.Contains("vegitoons") && (path.Contains("/obra/") || path.Contains("/capitulo/"));
        }

        public ComicInfo LoadUri(Uri Uri)
        {
            pageMap.Clear();
            currentHost = Uri.Host;
            API = $"https://api.{currentHost}";
            CDN = $"https://cdn.{currentHost}";

            var doc = new HtmlDocument();
            CFData = doc.LoadUrl($"https://{currentHost}/");
            InspectDocForEndpoints(doc);

            string bookId = null;
            var path = Uri.AbsolutePath;
            var capMatch = Regex.Match(path, @"/capitulo/(\d+)", RegexOptions.IgnoreCase);
            if (capMatch.Success)
            {
                int capId = int.Parse(capMatch.Groups[1].Value);
                var capJson = DownloadString($"{API}/capitulos/{capId}");
                if (!string.IsNullOrWhiteSpace(capJson))
                {
                    var capToken = JToken.Parse(capJson);
                    if (capToken["resultado"] != null)
                        capToken = capToken["resultado"];

                    var capData = capToken.ToObject<ChapterData>();
                    if (capData != null && capData.obr_id > 0)
                    {
                        bookId = capData.obr_id.ToString();
                        Uri = new Uri($"https://{currentHost}/obra/{bookId}");
                    }
                }
            }

            if (string.IsNullOrEmpty(bookId))
            {
                var obraMatch = Regex.Match(path, @"/obra/(\d+)", RegexOptions.IgnoreCase);
                if (obraMatch.Success)
                {
                    bookId = obraMatch.Groups[1].Value;
                }
                else
                {
                    throw new Exception($"Could not extract obra ID from URL: {Uri}");
                }
            }

            currentBook = bookId;
            LoadBookInfo(currentBook);

            string coverUrl = EnsureAbsoluteUrl(currentBookInfo.obr_imagem);

            return new ComicInfo()
            {
                ContentType = ContentType.Comic,
                Cover = DownloadBinary(coverUrl),
                Title = currentBookInfo.obr_nome ?? "Unknown",
                Url = Uri
            };
        }

        private void LoadBookInfo(string bookId)
        {
            var apiData = DownloadString($"{API}/obras/{bookId}");
            if (string.IsNullOrWhiteSpace(apiData))
                throw new Exception($"Failed to retrieve obra {bookId} data from API.");

            var token = JToken.Parse(apiData);
            if (token["resultado"] != null)
                token = token["resultado"];

            currentBookInfo = token.ToObject<BookInfo>();
            if (currentBookInfo == null || currentBookInfo.obr_id == 0)
                throw new Exception($"Invalid obra data received for ID {bookId}.");
        }

        private string[] GetChapterPages(int ID)
        {
            if (pageMap.TryGetValue(ID, out var cached))
                return cached;

            var apiData = DownloadString($"{API}/capitulos/{ID}");
            if (string.IsNullOrWhiteSpace(apiData))
                throw new Exception($"Failed to retrieve chapter {ID} data from API.");

            var token = JToken.Parse(apiData);
            if (token["resultado"] != null)
                token = token["resultado"];

            var pagesToken = token["cap_paginas"];
            var pages = ExtractPages(pagesToken);

            return pageMap[ID] = pages;
        }

        private string[] ExtractPages(JToken token)
        {
            var list = new List<string>();
            if (token is JArray arr)
            {
                foreach (var item in arr)
                {
                    if (item.Type == JTokenType.String)
                    {
                        var str = item.ToString();
                        if (!string.IsNullOrWhiteSpace(str))
                            list.Add(EnsureAbsoluteUrl(str));
                    }
                    else if (item is JObject obj)
                    {
                        var src = obj["src"]?.ToString() ?? obj["url"]?.ToString() ?? obj["path"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(src))
                            list.Add(EnsureAbsoluteUrl(src));
                    }
                }
            }
            return list.ToArray();
        }

        private string EnsureAbsoluteUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return url;

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return url;

            if (url.StartsWith("//"))
                return "https:" + url;

            if (url.StartsWith("/"))
                return $"{CDN.TrimEnd('/')}{url}";

            return $"{CDN.TrimEnd('/')}/{url}";
        }

        private (string Key, string Value)[] Headers => new (string Key, string Value)[]
        {
            ("Origin", $"https://{currentHost}"),
            ("Referer", $"https://{currentHost}/")
        };

        private string DownloadString(string url, int timeoutSecs = 120)
        {
            CFData = new CloudflareData()
            {
                Cookies = CFData?.Cookies,
                UserAgent = CFData?.UserAgent ?? ProxyTools.UserAgent,
                HTML = CFData?.HTML
            };

            var data = new Uri(url).TryDownloadString(CFData, Referer: $"https://{currentHost}/", Headers: Headers, TimeoutSecs: timeoutSecs);
            if (string.IsNullOrWhiteSpace(data) || data.IsCloudflareTriggered())
            {
                CFData = JSTools.BypassCloudflare($"https://{currentHost}/");
                data = new Uri(url).TryDownloadString(CFData, Referer: $"https://{currentHost}/", Headers: Headers, TimeoutSecs: timeoutSecs);
            }

            return data;
        }

        private byte[] DownloadBinary(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var data = url.TryDownload(
                CFData,
                Referer: $"https://{currentHost}/",
                Headers: Headers,
                isImage: true);

            if (data == null || (data.Length < 2000 && Encoding.UTF8.GetString(data).IsCloudflareTriggered()))
            {
                CFData = JSTools.BypassCloudflare($"https://{currentHost}/");
                data = url.TryDownload(
                    CFData,
                    Referer: $"https://{currentHost}/",
                    Headers: Headers,
                    isImage: true);
            }

            return data;
        }

        private void InspectDocForEndpoints(HtmlDocument doc)
        {
            if (doc?.DocumentNode == null)
                return;

            try
            {
                var scriptNode = doc.DocumentNode.SelectSingleNode("//link[contains(@href, 'httpClient-')]") ??
                                 doc.DocumentNode.SelectSingleNode("//script[contains(@src, 'httpClient-')]");

                if (scriptNode != null)
                {
                    var src = scriptNode.GetAttributeValue("href", null) ?? scriptNode.GetAttributeValue("src", null);
                    if (!string.IsNullOrWhiteSpace(src))
                    {
                        var scriptUrl = new Uri(new Uri($"https://{currentHost}"), src);
                        var scriptData = DownloadString(scriptUrl.AbsoluteUri);
                        if (!string.IsNullOrWhiteSpace(scriptData))
                        {
                            var apiMatch = Regex.Match(scriptData, @"https://api\.[a-zA-Z0-9.\-]+");
                            if (apiMatch.Success)
                                API = apiMatch.Value;

                            var cdnMatch = Regex.Match(scriptData, @"https://cdn\.[a-zA-Z0-9.\-]+");
                            if (cdnMatch.Success)
                                CDN = cdnMatch.Value;
                        }
                    }
                }
            }
            catch { }
        }

        private double ParseChapterNumber(ChapterInfo chapter)
        {
            if (chapter.cap_numero != null)
            {
                var str = chapter.cap_numero.ToString().Replace(',', '.').Trim();
                if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                    return val;
            }

            if (!string.IsNullOrWhiteSpace(chapter.cap_nome))
            {
                var match = Regex.Match(chapter.cap_nome, @"\d+(?:[.,]\d+)?");
                if (match.Success && double.TryParse(match.Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                    return val;
            }

            return 0;
        }

        private string GetChapterString(ChapterInfo chapter)
        {
            if (chapter.cap_numero != null)
            {
                var str = chapter.cap_numero.ToString().Trim();
                if (!string.IsNullOrEmpty(str))
                    return str;
            }

            if (!string.IsNullOrWhiteSpace(chapter.cap_nome))
            {
                var match = Regex.Match(chapter.cap_nome, @"\d+(?:[.,]\d+)?");
                if (match.Success)
                    return match.Value;

                return chapter.cap_nome.Trim();
            }

            return chapter.cap_id.ToString();
        }

        private class BookInfo
        {
            public int obr_id;
            public string obr_nome;
            public string obr_imagem;
            public List<ChapterInfo> capitulos;
        }

        private class ChapterInfo
        {
            public int cap_id;
            public string cap_nome;
            public object cap_numero;
            public bool cap_liberado = true;
        }

        private class ChapterData
        {
            public int cap_id;
            public string cap_nome;
            public object cap_numero;
            public int obr_id;
            public List<string> cap_paginas;
        }
    }
}
