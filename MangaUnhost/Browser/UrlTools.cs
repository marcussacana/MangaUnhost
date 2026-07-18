using MangaUnhost.Others;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Net.Http;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace MangaUnhost.Browser
{
    public static class UrlTools
    {
        public static (string Key, string Value)[] MergeHeaders((string Key, string Value)[] defaultHeaders, (string Key, string Value)[] overrideHeaders)
        {
            if (defaultHeaders == null && overrideHeaders == null) return null;
            if (defaultHeaders == null) return overrideHeaders;
            if (overrideHeaders == null) return defaultHeaders;

            var dict = new System.Collections.Generic.Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var h in defaultHeaders) dict[h.Key] = h.Value;
            foreach (var h in overrideHeaders) dict[h.Key] = h.Value;

            var list = new System.Collections.Generic.List<(string Key, string Value)>();
            foreach (var kvp in dict) list.Add((kvp.Key, kvp.Value));
            return list.ToArray();
        }

        public static string SetUrlParameter(this string url, string paramName, string value)
        {
            return new Uri(url).SetParameter(paramName, value).ToString();
        }

        public static string GetUrlParamter(this string url, string paramName)
        {
            return new Uri(url).GetParameter(paramName);
        }
        public static Uri EnsureAbsoluteUri(this Uri url, Uri domain) => new Uri(url.AbsoluteUri.EnsureAbsoluteUrl(domain.AbsoluteUri));
        public static Uri EnsureAbsoluteUri(this Uri url, string domain) => new Uri(url.AbsoluteUri.EnsureAbsoluteUrl(domain));

        public static Uri EnsureAbsoluteUri(this string url, Uri domain) => new Uri(url.EnsureAbsoluteUrl(domain.AbsoluteUri));
        public static Uri EnsureAbsoluteUri(this string url, string domain) => new Uri(url.EnsureAbsoluteUrl(domain));

        public static string EnsureAbsoluteUrl(this Uri url, Uri domain) => url.AbsoluteUri.EnsureAbsoluteUrl(domain.AbsoluteUri);
        public static string EnsureAbsoluteUrl(this string url, Uri domain) => url.EnsureAbsoluteUrl(domain.AbsoluteUri);
        public static string EnsureAbsoluteUrl(this Uri url, string domain) => url.AbsoluteUri.EnsureAbsoluteUrl(domain);
        public static string EnsureAbsoluteUrl(this string url, string domain)
        {
            if (!domain.ToLowerInvariant().StartsWith("http"))
                domain += "http://";

            var BaseUri = new Uri(domain);
            var Https = BaseUri.AbsoluteUri.ToLowerInvariant().StartsWith("https");
            domain = (Https ? "https://" : "http://") + BaseUri.Host;
            BaseUri = new Uri(domain);

            if (Program.Debug)
                Program.Writer?.WriteLine("Abs Uri Result: {0}", new Uri(BaseUri, url).AbsoluteUri);

            return new Uri(BaseUri, url).AbsoluteUri;
        }

        public static Uri SetParameter(this Uri url, string paramName, string value)
        {
            var queryParts = HttpUtility.ParseQueryString(url.Query);
            queryParts[paramName] = value;
            return new Uri(url.AbsoluteUriExcludingQuery() + '?' + queryParts.ToString());
        }
        public static string GetParameter(this Uri url, string paramName)
        {
            var queryParts = HttpUtility.ParseQueryString(url.Query);
            if (!queryParts.AllKeys.Contains(paramName))
                return null;
            return queryParts[paramName];
        }

        public static string SkipProtectors(this string URL) => SkipProtectors(new Uri(URL)).AbsoluteUri;
        public static Uri SkipProtectors(this Uri URL)
        {
            if (URL.AbsoluteUri.ToLower().Contains("googleusercontent.com/gadgets/proxy"))
            {
                return new Uri(HttpUtility.UrlDecode(URL.Query.Substring("&url=")));
            }

            return URL;
        }

        public static string AbsoluteUriExcludingQuery(this Uri url)
        {
            return url.AbsoluteUri.Split('?').FirstOrDefault() ?? string.Empty;
        }

        public static CloudflareData? LoadUrl(this HtmlAgilityPack.HtmlDocument Document, string Url, CloudflareData? CFData, System.Text.Encoding Encoding = null, string Referer = null, string Proxy = null, string Accept = null, string UserAgent = null, (string Key, string Value)[] Headers = null, WebExceptionStatus[] AcceptableErrors = null) =>
            Document.LoadUrl(Url, Encoding, Referer, CFData?.UserAgent ?? UserAgent, Proxy, Accept, Headers, CFData?.Cookies, AcceptableErrors);
        public static CloudflareData? LoadUrl(this HtmlAgilityPack.HtmlDocument Document, Uri Url, CloudflareData? CFData, System.Text.Encoding Encoding = null, string Referer = null, string Proxy = null, string Accept = null, string UserAgent = null, (string Key, string Value)[] Headers = null, WebExceptionStatus[] AcceptableErrors = null) =>
            Document.LoadUrl(Url, Encoding, Referer, CFData?.UserAgent ?? UserAgent, Proxy, Accept, Headers, CFData?.Cookies, AcceptableErrors);
        public static CloudflareData? LoadUrl(this HtmlAgilityPack.HtmlDocument Document, string Url, Encoding Encoding = null, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookies = null, WebExceptionStatus[] AcceptableErrors = null) =>
            Document.LoadUrl(new Uri(Url), Encoding, Referer, UserAgent, Proxy, Accept, Headers, Cookies, AcceptableErrors);

        public static CloudflareData? LoadUrl(this HtmlAgilityPack.HtmlDocument Document, Uri Url, Encoding Encoding = null, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookies = null, WebExceptionStatus[] AcceptableErrors = null)
        {
            if (Encoding == null)
                Encoding = Encoding.UTF8;

            string HTML = Encoding.GetString(Url.TryDownload(Referer, UserAgent, Proxy, Accept, Headers, Cookies, AcceptableErrors ?? new WebExceptionStatus[] { WebExceptionStatus.ProtocolError }) ?? new byte[0]);

            if (HTML.IsCloudflareTriggered())
            {
                var CFData = Url.AbsoluteUri.BypassCloudflare();
                Document.LoadHtml(CFData.HTML);
                return CFData;
            }

            Document.LoadHtml(HTML);

            if (Program.Debug)
            {
                Program.Writer?.WriteLine("Load URL: {0}\r\nHTML: {1}", Url.AbsoluteUri, HTML);
                Program.Writer?.Flush();
            }

            return new CloudflareData()
            {
                UserAgent = UserAgent,
                Cookies = Cookies,
                HTML = HTML
            };
        }

        public static string TryDownloadString(this Uri Url, CloudflareData? CFData = null, string Referer = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, WebExceptionStatus[] AcceptableErrors = null, int Retries = 3, int TimeoutSecs = 120, bool isImage = false) =>
            Encoding.UTF8.GetString(Url.TryDownload(Referer, CFData?.UserAgent ?? ProxyTools.UserAgent, Proxy, Accept, MergeHeaders(CFData?.Headers, Headers), CFData?.Cookies, AcceptableErrors, Retries, TimeoutSecs, isImage) ?? new byte[0]);

        public static string TryDownloadString(this Uri Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, WebExceptionStatus[] AcceptableErrors = null, int Retries = 3, int TimeoutSecs = 120, bool isImage = false) =>
            Encoding.UTF8.GetString(Url.TryDownload(Referer, UserAgent, Proxy, Accept, Headers, Cookie, AcceptableErrors, Retries, TimeoutSecs, isImage) ?? new byte[0]);

        public static string TryDownloadString(this string Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, WebExceptionStatus[] AcceptableErrors = null, int Retries = 3, int TimeoutSecs = 120, bool isImage = false) =>
            Encoding.UTF8.GetString(new Uri(Url).TryDownload(Referer, UserAgent, Proxy, Accept, Headers, Cookie, AcceptableErrors, Retries, TimeoutSecs, isImage) ?? new byte[0]);

        public static byte[] TryDownload(this Uri Url, CloudflareData? CFData, string Referer = null, string Proxy = null, string Accept = null, string UserAgent = null, (string Key, string Value)[] Headers = null, WebExceptionStatus[] AcceptableErros = null, int Retries = 3, int TimeoutSecs = 120, bool isImage = false) =>
            Url.TryDownload(Referer, CFData?.UserAgent ?? UserAgent ?? ProxyTools.UserAgent, Proxy, Accept, MergeHeaders(CFData?.Headers, Headers), CFData?.Cookies, AcceptableErros, Retries, TimeoutSecs, isImage);
        public static byte[] TryDownload(this string Url, CloudflareData? CFData, string Referer = null, string Proxy = null, string Accept = null, string UserAgent = null, (string Key, string Value)[] Headers = null, WebExceptionStatus[] AcceptableErros = null, int Retries = 3, int TimeoutSecs = 120, bool isImage = false) =>
            Url.TryDownload(Referer, CFData?.UserAgent ?? UserAgent ?? ProxyTools.UserAgent, Proxy, Accept, MergeHeaders(CFData?.Headers, Headers), CFData?.Cookies, AcceptableErros, Retries, TimeoutSecs, isImage);
        public static byte[] TryDownload(this string Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, WebExceptionStatus[] AcceptableErrors = null, int Retries = 3, int TimeoutSecs = 120, bool isImage = false) =>
            new Uri(Url).TryDownload(Referer, UserAgent, Proxy, Accept, Headers, Cookie, AcceptableErrors, Retries, TimeoutSecs, isImage);

        public static byte[] TryDownload(this Uri Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, WebExceptionStatus[] AcceptableErrors = null, int Retries = 3, int TimeoutSecs = 120, bool isImage = false)
        {
            bool Finished = false;
            byte[] Result = null;

            var Thread = new Thread(async () => {
                try
                {
                    Result = await Url.TryDownloadAsync(Referer, UserAgent, Proxy, Accept, Headers, Cookie, AcceptableErrors: AcceptableErrors, Retries: 3, isImage: isImage);
                }
                finally
                {
                    Finished = true;
                } 
            });

            Thread.Start();

            var waitBegin = DateTime.Now;
            while (!Finished && ((DateTime.Now - waitBegin).TotalSeconds < TimeoutSecs))
                ThreadTools.Wait(100, true);

            if (!Finished) {
                Thread.Abort();
                return null;
            }

            return Result;
        }

        public static async Task<byte[]> TryDownloadAsync(this Uri Url, CloudflareData CFData, string Referer = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, WebExceptionStatus[] AcceptableErros = null, int Retries = 3, bool isImage = false) =>
            await Url.TryDownloadAsync(Referer, CFData.UserAgent, Proxy, Accept, MergeHeaders(CFData.Headers, Headers), CFData.Cookies, AcceptableErros, Retries, isImage);
        public static async Task<byte[]> TryDownloadAsync(this string Url, CloudflareData CFData, string Referer = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, WebExceptionStatus[] AcceptableErros = null, int Retries = 3, bool isImage = false) =>
            await Url.TryDownloadAsync(Referer, CFData.UserAgent, Proxy, Accept, MergeHeaders(CFData.Headers, Headers), CFData.Cookies, AcceptableErros, Retries, isImage);
        public static async Task<byte[]> TryDownloadAsync(this string Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, WebExceptionStatus[] AcceptableErrors = null, int Retries = 3, bool isImage = false) =>
            await new Uri(Url).TryDownloadAsync(Referer, UserAgent, Proxy, Accept, Headers, Cookie, AcceptableErrors, Retries, isImage);

        public static async Task<byte[]> TryDownloadAsync(this Uri Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, WebExceptionStatus[] AcceptableErrors = null, int Retries = 3, bool isImage = false)
        {
            try
            {
                return await Url.DownloadAsync(Referer, UserAgent, Proxy, Accept, Headers, Cookie, isImage);
            }
            catch (Exception ex)
            {
                if (ex is HttpRequestException || ex is OperationCanceledException)
                {
                    // Trata ConnectionClosed com o fallback bruto TLS
                    if (ex.InnerException is System.IO.IOException || ex.InnerException is System.Net.Sockets.SocketException)
                    {
                        if (AcceptableErrors != null && AcceptableErrors.Contains(WebExceptionStatus.ConnectionClosed))
                            return GetErrorContentOverHttps(Url, Referer, UserAgent, Cookie);
                    }
                    
                    if (Retries > 0)
                        return await Url.TryDownloadAsync(Referer, UserAgent, Proxy, Accept, Headers, Cookie, AcceptableErrors, Retries - 1, isImage);
                }
                if (Program.Debug)
                {
                    Program.Writer?.WriteLine("TryDownload Error: {0}", ex.ToString());
                    Program.Writer?.Flush();
                }
                return null;
            }
        }

        public static SemaphoreSlim HttpRequestLocker = new SemaphoreSlim(20, 20);
        public static byte[] Download(this string Url, CloudflareData CFData, string Referer = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, bool isImage = false) =>
            new Uri(Url).Download(Referer, CFData.UserAgent, Proxy, Accept, MergeHeaders(CFData.Headers, Headers), CFData.Cookies, isImage);
        public static byte[] Download(this Uri Url, CloudflareData CFData, string Referer = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, bool isImage = false) =>
            Url.Download(Referer, CFData.UserAgent, Proxy, Accept, MergeHeaders(CFData.Headers, Headers), CFData.Cookies, isImage);
        public static byte[] Download(this string Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, bool isImage = false) =>
            new Uri(Url).Download(Referer, UserAgent, Proxy, Accept, Headers, Cookie, isImage);
        public static byte[] Download(this Uri Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, bool isImage = false)
        {
            return Url.TryDownloadAsync(Referer, UserAgent, Proxy, Accept, Headers, Cookie, AcceptableErrors: null, Retries: 3, isImage: isImage).RunInBackground();
        }
        public static async Task<byte[]> DownloadAsync(this string Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, bool isImage = false) =>
            await new Uri(Url).DownloadAsync(Referer, UserAgent, Proxy, Accept, Headers, Cookie, isImage);
        
        private static readonly ConditionalWeakTable<CookieContainer, ConcurrentDictionary<string, HttpClient>> _cookieClients = new();
        private static readonly ConcurrentDictionary<string, HttpClient> _statelessClients = new();

        private static HttpClient GetHttpClient(CookieContainer cookies, string proxy)
        {
            var proxyKey = proxy ?? string.Empty;
            if (cookies == null)
            {
                return _statelessClients.GetOrAdd(proxyKey, p => CreateClient(null, p));
            }

            var proxyDict = _cookieClients.GetValue(cookies, _ => new ConcurrentDictionary<string, HttpClient>());
            return proxyDict.GetOrAdd(proxyKey, p => CreateClient(cookies, p));
        }

        private static HttpClient CreateClient(CookieContainer cookies, string proxy)
        {
            var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            };
            
            if (cookies != null)
            {
                handler.UseCookies = true;
                handler.CookieContainer = cookies;
            }
            else
            {
                handler.UseCookies = false;
            }

            if (!string.IsNullOrEmpty(proxy))
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(proxy);
            }

            return new HttpClient(handler);
        }

        public static async Task<byte[]> DownloadAsync(this Uri Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, bool isImage = false)
        {
            var client = GetHttpClient(Cookie, Proxy);
            using var request = new HttpRequestMessage(HttpMethod.Get, Url);
            
            request.Version = HttpVersion.Version20;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

            if (Headers == null)
                request.Headers.TryAddWithoutValidation("Host", Url.Host);
            else
            {
                foreach (var Entry in Headers)
                    request.Headers.TryAddWithoutValidation(Entry.Key, Entry.Value);
            }

            if (Referer != null)
                request.Headers.TryAddWithoutValidation("Referer", Referer);

            if (UserAgent != null)
                request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);

            if (isImage)
            {
                var imgHeaders = JSTools.GetImageHeaders();
                foreach (var h in imgHeaders)
                {
                    string key = h.Key;
                    string val = h.Value;

                    if (key.ToLowerInvariant() == "accept" && Accept != null)
                        continue;

                    if (string.IsNullOrWhiteSpace(val))
                        continue;
                    
                    // Do not overwrite sec-fetch-site if CFData already provided it (e.g. same-origin)
                    if (key.ToLowerInvariant() == "sec-fetch-site")
                    {
                        if (request.Headers.Contains(key)) continue;
                        val = "same-site";
                    }

                    // Remove document-specific headers that were injected by CFData
                    if (request.Headers.Contains(key))
                        request.Headers.Remove(key);
                        
                    request.Headers.TryAddWithoutValidation(key, val);
                }
                
                // Specific cleanup of headers that shouldn't be in image requests
                if (request.Headers.Contains("upgrade-insecure-requests"))
                    request.Headers.Remove("upgrade-insecure-requests");

                if (request.Headers.Contains("sec-fetch-user"))
                    request.Headers.Remove("sec-fetch-user");

                if (Accept != null)
                {
                    if (request.Headers.Contains("Accept"))
                        request.Headers.Remove("Accept");
                    request.Headers.TryAddWithoutValidation("Accept", Accept);
                }
            }
            else
            {
                if (Accept != null)
                    request.Headers.TryAddWithoutValidation("Accept", Accept);
                else
                    request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
            }

            using var cts = new CancellationTokenSource(Proxy == null ? 5000 : 30000);

            await HttpRequestLocker.WaitAsync();
            try
            {
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cts.Token);
                return await response.Content.ReadAsByteArrayAsync();
            }
            finally
            {
                HttpRequestLocker.Release();
            }
        }

        public static byte[] Upload(this string Url, CloudflareData? cfdata, byte[] Data, string Referer = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, bool isImage = false) =>
            new Uri(Url).Upload(Data, Referer, cfdata?.UserAgent, Proxy, Accept, MergeHeaders(cfdata?.Headers, Headers), cfdata?.Cookies);
        public static byte[] Upload(this Uri Url, CloudflareData? cfdata, byte[] Data, string Referer = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, int timeout = 30) =>
            Url.Upload(Data, Referer, cfdata?.UserAgent, Proxy, Accept, MergeHeaders(cfdata?.Headers, Headers), cfdata?.Cookies, timeout);
        public static byte[] Upload(this string Url, byte[] Data = null, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, int timeout = 30) =>
            new Uri(Url).Upload(Data, Referer, UserAgent, Proxy, Accept, Headers, Cookie, timeout);
        public static byte[] Upload(this Uri Url, byte[] Data = null, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, int timeout = 30)
        {
            return Url.UploadAsync(Data, Referer, UserAgent, Proxy, Accept, Headers, Cookie, timeout).RunInBackground();
        }
        public static async Task<byte[]> UploadAsync(this string Url, byte[] Data = null, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, int timeout = 30) =>
            await new Uri(Url).UploadAsync(Data, Referer, UserAgent, Proxy, Accept, Headers, Cookie, timeout);
        public static async Task<byte[]> UploadAsync(this Uri Url, byte[] Data = null, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, int timeout = 30)
        {
            var client = GetHttpClient(Cookie, Proxy);
            using var request = new HttpRequestMessage(HttpMethod.Post, Url);
            
            request.Version = HttpVersion.Version20;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

            if (Headers == null)
                request.Headers.TryAddWithoutValidation("Host", Url.Host);
            else
            {
                foreach (var Entry in Headers)
                    request.Headers.TryAddWithoutValidation(Entry.Key, Entry.Value);
            }

            if (Referer != null)
                request.Headers.TryAddWithoutValidation("Referer", Referer);

            if (UserAgent != null)
                request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);

            if (Accept != null)
                request.Headers.TryAddWithoutValidation("Accept", Accept);

            if (Data != null)
                request.Content = new ByteArrayContent(Data);

            using var cts = new CancellationTokenSource(1000 * timeout);

            await HttpRequestLocker.WaitAsync();
            try
            {
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cts.Token);
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch
            {
                return null;
            }
            finally
            {
                HttpRequestLocker.Release();
            }
        }

        public static System.Net.Cookie[] GetSetCookies(this WebHeaderCollection Headers, Uri Url)
        {
            var CookieContainer = new CookieContainer();
            foreach (var HeaderKey in Headers.AllKeys)
            {
                if (HeaderKey.ToLowerInvariant() == "set-cookie")
                {
                    foreach (var HeaderValue in Headers.GetValues(HeaderKey))
                    {
                        CookieContainer.SetCookies(Url, HeaderValue);
                    }
                }
            }

            return CookieContainer.GetCookies();
        }
        public static byte[] GetErrorContentOverHttps(this Uri Url, string Referer = null, string UserAgent = null, CookieContainer Cookie = null, bool isImage = false)
        {
            TcpClient Tcp = new TcpClient(Url.Host, 443);
            Tcp.ReceiveTimeout = 1000 * 15;
            var TcpStream = Tcp.GetStream();
            var SslStream = new SslStream(TcpStream);
            SslStream.AuthenticateAsClient(Url.Host);

            StringBuilder Header = new StringBuilder();
            Header.AppendLine($"GET {Url.PathAndQuery} HTTP/1.1");
            Header.AppendLine("Host: " + Url.Host);

            if (Referer != null)
                Header.AppendLine("Referer: " + Referer.Replace("\n", "%0A").Replace("\r", "%0D"));

            if (UserAgent != null)
                Header.AppendLine("User-Agent: " + UserAgent.Replace("\n", "%0A").Replace("\r", "%0D"));

            if (Cookie != null)
                Header.AppendLine("Cookie: " + Cookie.GetCookieHeader(Url));

            Header.AppendLine("Cache-Control: no-cache");
            Header.AppendLine("");

            byte[] headerAsBytes = Encoding.UTF8.GetBytes(Header.ToString());
            SslStream.Write(headerAsBytes);


            using (var Buffer = new MemoryStream())
            using (StreamReader Reader = new StreamReader(SslStream, Encoding.ASCII, false, 1, false))
            {
                var Status = Reader.ReadLine();

                var CurLine = string.Empty;

                long Length = -1;

                try
                {
                    do
                    {
                        CurLine = Reader.ReadLine();
                        if (CurLine.StartsWith("Content-Length:"))
                            Length = long.Parse(CurLine.Substring(":").Trim());

                    } while (CurLine != "");
                }
                catch {
                    return null;
                }

                try
                {
                    SslStream.CopyTo(Buffer, 1);
                }
                catch { }
                

                return Buffer.ToArray();
            }
        }
    }
}
