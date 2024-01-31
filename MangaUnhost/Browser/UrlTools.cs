using MangaUnhost;
using MangaUnhost.Others;
using Nito.AsyncEx;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace MangaUnhost.Browser
{
    public static class UrlTools
    {
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

            return null;
        }
        public static string TryDownloadString(this Uri Url, CloudflareData? CFData = null, string Referer = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, WebExceptionStatus[] AcceptableErrors = null, int Retries = 3) =>
            Encoding.UTF8.GetString(Url.TryDownload(CFData, Referer, Proxy, Accept, null, Headers) ?? new byte[0]);

        public static string TryDownloadString(this Uri Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, WebExceptionStatus[] AcceptableErrors = null, int Retries = 3) =>
            Encoding.UTF8.GetString(Url.TryDownload(Referer, UserAgent, Proxy, Accept, Headers, Cookie, AcceptableErrors, Retries) ?? new byte[0]);

        public static string TryDownloadString(this string Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, WebExceptionStatus[] AcceptableErrors = null, int Retries = 3) =>
            Encoding.UTF8.GetString(new Uri(Url).TryDownload(Referer, UserAgent, Proxy, Accept, Headers, Cookie, AcceptableErrors, Retries) ?? new byte[0]);

        public static byte[] TryDownload(this Uri Url, CloudflareData? CFData, string Referer = null, string Proxy = null, string Accept = null, string UserAgent = null, (string Key, string Value)[] Headers = null, WebExceptionStatus[] AcceptableErros = null, int Retries = 3) =>
            Url.TryDownload(Referer, CFData?.UserAgent ?? UserAgent, Proxy, Accept, Headers, CFData?.Cookies, AcceptableErros, Retries);
        public static byte[] TryDownload(this string Url, CloudflareData? CFData, string Referer = null, string Proxy = null, string Accept = null, string UserAgent = null, (string Key, string Value)[] Headers = null, WebExceptionStatus[] AcceptableErros = null, int Retries = 3) =>
            Url.TryDownload(Referer, CFData?.UserAgent ?? UserAgent, Proxy, Accept, Headers, CFData?.Cookies, AcceptableErros, Retries);
        public static byte[] TryDownload(this string Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, WebExceptionStatus[] AcceptableErrors = null, int Retries = 3) =>
            new Uri(Url).TryDownload(Referer, UserAgent, Proxy, Accept, Headers, Cookie, AcceptableErrors, Retries);

        public static byte[] TryDownload(this Uri Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, WebExceptionStatus[] AcceptableErrors = null, int Retries = 3)
        {
            bool Finished = false;
            byte[] Result = null;

            var Thread = new Thread(async () => {
                try
                {
                    Result = await Url.TryDownloadAsync(Referer, UserAgent, Proxy, Accept, Headers, Cookie, AcceptableErrors: AcceptableErrors);
                }
                finally
                {
                    Finished = true;
                } 
            });

            Thread.Start();

            while (!Finished)
                ThreadTools.Wait(100, true);

            return Result;
        }

        public static async Task<byte[]> TryDownloadAsync(this Uri Url, CloudflareData CFData, string Referer = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, WebExceptionStatus[] AcceptableErros = null, int Retries = 3) =>
            await Url.TryDownloadAsync(Referer, CFData.UserAgent, Proxy, Accept, Headers, CFData.Cookies, AcceptableErros, Retries);
        public static async Task<byte[]> TryDownloadAsync(this string Url, CloudflareData CFData, string Referer = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, WebExceptionStatus[] AcceptableErros = null, int Retries = 3) =>
            await Url.TryDownloadAsync(Referer, CFData.UserAgent, Proxy, Accept, Headers, CFData.Cookies, AcceptableErros, Retries);
        public static async Task<byte[]> TryDownloadAsync(this string Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, WebExceptionStatus[] AcceptableErrors = null, int Retries = 3) =>
            await new Uri(Url).TryDownloadAsync(Referer, UserAgent, Proxy, Accept, Headers, Cookie, AcceptableErrors, Retries);

        public static async Task<byte[]> TryDownloadAsync(this Uri Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null, WebExceptionStatus[] AcceptableErrors = null, int Retries = 3)
        {
            try
            {
                return await Url.DownloadAsync(Referer, UserAgent, Proxy, Accept, Headers, Cookie);
            }
            catch (Exception ex)
            {
                if (ex is WebException)
                {
                    var Exception = (WebException)ex;
                    if (Exception.Status == WebExceptionStatus.ConnectFailure)
                    {
                        return null;
                    }

                    if (AcceptableErrors != null && AcceptableErrors.Contains(Exception.Status))
                    {
                        if (Exception.Status == WebExceptionStatus.ConnectionClosed)
                        {
                            return GetErrorContentOverHttps(Url, Referer, UserAgent, Cookie);
                        }
                        else
                        {
                            await HttpRequestLocker.WaitAsync();
                            try
                            {
                                using (WebResponse Response = Exception.Response)
                                using (Stream ResponseData = Response.GetResponseStream())
                                using (MemoryStream Stream = new MemoryStream())
                                {
                                    ResponseData.CopyTo(Stream);
                                    return Stream.ToArray();
                                }
                            }
                            finally
                            {
                                HttpRequestLocker.Release();
                            }
                        }
                    }
                    if (Retries > 0)
                        return await Url.TryDownloadAsync(Referer, UserAgent, Proxy, Accept, Headers, Cookie, AcceptableErrors, Retries - 1);
                }
                if (Program.Debug)
                {
                    Program.Writer?.WriteLine("TryDownload Error: {0}", ex.ToString());
                    Program.Writer?.Flush();
                }
                return null;
            }
        }

        public static SemaphoreSlim HttpRequestLocker = new SemaphoreSlim(10, 10);
        public static byte[] Download(this string Url, CloudflareData CFData, string Referer = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null) =>
            new Uri(Url).Download(Referer, CFData.UserAgent, Proxy, Accept, Headers, CFData.Cookies);
        public static byte[] Download(this Uri Url, CloudflareData CFData, string Referer = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null) =>
            Url.Download(Referer, CFData.UserAgent, Proxy, Accept, Headers, CFData.Cookies);
        public static byte[] Download(this string Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null) =>
            new Uri(Url).Download(Referer, UserAgent, Proxy, Accept, Headers, Cookie);
        public static byte[] Download(this Uri Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null)
        {
            byte[] Result = null;

            var Thread = new System.Threading.Thread(() =>
            Result = AsyncContext.Run(async () =>
            await Url.TryDownloadAsync(Referer, UserAgent, Proxy, Accept, Headers, Cookie)));

            Thread.Start();

            while (Thread.IsRunning())
                ThreadTools.Wait(100, true);

            if (Result == null)
                throw new WebException();

            return Result;
        }
        public static async Task<byte[]> DownloadAsync(this string Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null) =>
            await new Uri(Url).DownloadAsync(Referer, UserAgent, Proxy, Accept, Headers, Cookie);
        public static async Task<byte[]> DownloadAsync(this Uri Url, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null)
        {
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(Url);

            if (Headers == null)
                Request.Headers["Host"] = Url.Host;
            else
            {
                foreach (var Entry in Headers)
                    Request.Headers[Entry.Key] = Entry.Value;
            }

            Request.UseDefaultCredentials = true;
            Request.Method = "GET";
            Request.Timeout = Proxy == null ? 1000 * 5 : 1000 * 30;

            if (Referer != null)
                Request.Referer = Referer;

            if (Cookie != null)
                Request.CookieContainer = Cookie;

            if (UserAgent != null)
                Request.UserAgent = UserAgent;

            if (Accept != null)
                Request.Accept = Accept;
            else
                Request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8";

            if (Proxy != null)
                Request.Proxy = new WebProxy(Proxy);

            await HttpRequestLocker.WaitAsync();
            try
            {
                using (var Response = await Request.GetResponseAsync())
                using (var RespData = Response.GetResponseStream())
                using (var Output = new MemoryStream())
                {
                    await RespData.CopyToAsync(Output);
                    return Output.ToArray();
                }
            }
            finally
            {
                HttpRequestLocker.Release();
            }
        }

        public static byte[] Upload(this string Url, byte[] Data = null, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null) =>
            new Uri(Url).Upload(Data, Referer, UserAgent, Proxy, Accept, Headers, Cookie);
        public static byte[] Upload(this Uri Url, byte[] Data = null, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null)
        {
            byte[] Result = null;

            var Thread = new System.Threading.Thread(() =>
            Result = AsyncContext.Run(async () =>
            await Url.UploadAsync(Data, Referer, UserAgent, Proxy, Accept, Headers, Cookie)));

            Thread.Start();

            while (Thread.IsRunning())
                ThreadTools.Wait(100, true);

            if (Result == null)
                throw new WebException();

            return Result;
        }
        public static async Task<byte[]> UploadAsync(this string Url, byte[] Data = null, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null) =>
            await new Uri(Url).UploadAsync(Data, Referer, UserAgent, Proxy, Accept, Headers, Cookie);
        public static async Task<byte[]> UploadAsync(this Uri Url, byte[] Data = null, string Referer = null, string UserAgent = null, string Proxy = null, string Accept = null, (string Key, string Value)[] Headers = null, CookieContainer Cookie = null)
        {
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(Url);

            if (Headers == null)
                Request.Headers["Host"] = Url.Host;
            else
            {
                foreach (var Entry in Headers)
                    Request.Headers[Entry.Key] = Entry.Value;
            }

            Request.UseDefaultCredentials = true;
            Request.Method = "POST";
            Request.Timeout = 1000 * 30;

            if (Referer != null)
                Request.Referer = Referer;

            if (Cookie != null)
                Request.CookieContainer = Cookie;

            if (UserAgent != null)
                Request.UserAgent = UserAgent;

            if (Accept != null)
                Request.Accept = Accept;

            if (Proxy != null)
                Request.Proxy = new WebProxy(Proxy);

            await HttpRequestLocker.WaitAsync();

            try
            {

                if (Data != null)
                {
                    Request.ContentLength = Data.Length;
                    using (var UploadStream = Request.GetRequestStream())
                    using (var Input = new MemoryStream(Data))
                    {
                        Input.CopyTo(UploadStream);
                    }
                }

                try
                {
                    using (var Response = await Request.GetResponseAsync())
                    using (var RespData = Response.GetResponseStream())
                    using (var Output = new MemoryStream())
                    {
                        await RespData.CopyToAsync(Output);
                        return Output.ToArray();
                    }
                }
                catch
                {
                    return null;
                }
            }
            finally
            {
                HttpRequestLocker.Release();
            }
        }

        public static byte[] GetErrorContentOverHttps(this Uri Url, string Referer = null, string UserAgent = null, CookieContainer Cookie = null)
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
