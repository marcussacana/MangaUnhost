using CefSharp;
using CefSharp.EventHandler;
using CefSharp.Internals;
using CefSharp.OffScreen;
using MangaUnhost.Browser;
using Nito.AsyncEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using ThreadState = System.Threading.ThreadState;

namespace MangaUnhost {
    public static class Extensions {
        public static CookieContainer ToContainer(this System.Net.Cookie Cookie) => new System.Net.Cookie[] { Cookie }.ToContainer();
        public static CookieContainer ToContainer(this System.Net.Cookie[] Cookies) {
            if (Cookies == null)
                return new CookieContainer();

            CookieContainer Container = new CookieContainer();
            foreach (var Cookie in Cookies)
                if (Cookie != null)
                    try {
                        Container.Add(Cookie);
                    } catch { }

            return Container;
        }
        public static CookieContainer ToContainer(this CefSharp.Cookie Cookie) => new CefSharp.Cookie[] { Cookie }.ToContainer();
        public static CookieContainer ToContainer(this CefSharp.Cookie[] Cookies) {
            if (Cookies == null)
                return new CookieContainer();

            CookieContainer Container = new CookieContainer();
            foreach (var Cookie in Cookies)
                if (Cookie != null)
                    try {
                        Container.Add(new System.Net.Cookie(Cookie.Name, Cookie.Value, Cookie.Path, Cookie.Domain));
                    } catch { }

            return Container;
        }
        public static System.Net.Cookie[] GetCookies(this CookieContainer Cookies)
        {
            List<System.Net.Cookie> CookieList = new List<System.Net.Cookie>();
            Hashtable CookiesTable = (Hashtable)Cookies.GetType().InvokeMember("m_domainTable", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, Cookies, new object[] { } );

            foreach (var Path in CookiesTable.Keys)
            {
                var Domain = Path.ToString().TrimStart('.');
                Uri URL = new Uri(string.Format("http://{0}/", Domain));
                foreach (var Cookie in Cookies.GetCookies(URL).Cast<System.Net.Cookie>())
                    CookieList.Add(Cookie);

                URL = new Uri(string.Format("https://{0}/", Domain));
                foreach (var Cookie in Cookies.GetCookies(URL).Cast<System.Net.Cookie>())
                    CookieList.Add(Cookie);
            }

            return CookieList.ToArray();
        }

        public static string GetCookie(this ChromiumWebBrowser Browser, string Name) => Browser.GetBrowser().GetCookie(Name);
        public static CefSharp.Cookie[] GetCookies(this ChromiumWebBrowser Browser) => Browser.GetBrowser().GetCookies();
        public static void DeleteCookies(this ChromiumWebBrowser Browser) => Browser.GetBrowser().DeleteCookies();

        public static void DeleteCookie(this ChromiumWebBrowser Browser, string Name) => Browser.GetBrowser().DeleteCookie(Name);

        public static void UpdateCookie(this ChromiumWebBrowser Browser, System.Net.Cookie Cookie) => Browser.GetBrowser().UpdateCookie(Cookie);

        public static void UpdateCookie(this ChromiumWebBrowser Browser, CefSharp.Cookie Cookie) => Browser.GetBrowser().UpdateCookie(Cookie);

        public static string GetCookie(this IBrowser Browser, string Name) {
            return (from x in Browser.GetCookies() where x.Name == Name select x.Value).FirstOrDefault();
        }

        public static CefSharp.Cookie[] GetCookies(this IBrowser Browser) {
            try
            {
                return AsyncContext.Run(async () => await Cef.GetGlobalCookieManager().VisitUrlCookiesAsync(Browser.MainFrame.Url, true)).ToArray();
            }
            catch { return null;  }
        }

        public static void DeleteCookies(this IBrowser Browser) {
            try
            {
                AsyncContext.Run(async () => await Cef.GetGlobalCookieManager().DeleteCookiesAsync(new Uri(Browser.MainFrame.Url).Host));
            }
            catch { }
        }

        public static void DeleteCookie(this IBrowser Browser, string Name) {
            try
            {
                AsyncContext.Run(async () => await Cef.GetGlobalCookieManager().DeleteCookiesAsync(Browser.MainFrame.Url, Name));
            }
            catch { }
        }

        public static void UpdateCookie(this IBrowser Browser, System.Net.Cookie Cookie) {
            Browser.UpdateCookie(new CefSharp.Cookie() {
                Domain = Cookie.Domain,
                Expires = Cookie.Expires,
                HttpOnly = Cookie.HttpOnly,
                Name = Cookie.Name,
                Value = Cookie.Value,
                Path = Cookie.Path
            });
        }
        public static void UpdateCookie(this IBrowser Browser, CefSharp.Cookie Cookie) {
            try
            {
                AsyncContext.Run(async () => await Cef.GetGlobalCookieManager().SetCookieAsync(Browser.MainFrame.Url, Cookie));
            }
            catch { }
        }

        public static bool IsRunning(this Thread Thread)
        {
            return Thread.ThreadState == ThreadState.Running || Thread.ThreadState == ThreadState.Background
                || Thread.ThreadState == ThreadState.WaitSleepJoin;
        }

        public static IEnumerable<KeyValuePair<string, string>> ToPair(this NameValueCollection Collection)
        {
            foreach (var Key in Collection.AllKeys)
            {
                foreach (var Value in Collection.GetValues(Key))
                    yield return new KeyValuePair<string, string>(Key, Value);
            }
        }

        //XPATH JS Script for testing
        //function $(X, A) { if (A === true) { var Results = []; var Query = document.evaluate(X, document, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null); for (var i = 0, length = Query.snapshotLength; i < length; ++i) Results.push(Query.snapshotItem(i)); return Results; } return document.evaluate(X, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue; }
        
        public static HtmlAgilityPack.HtmlNodeCollection SelectNodes(this HtmlAgilityPack.HtmlDocument Document, string XPath) => Document.DocumentNode.SelectNodes(XPath);
        public static HtmlAgilityPack.HtmlNode SelectSingleNode(this HtmlAgilityPack.HtmlDocument Document, string XPath) => Document.DocumentNode.SelectSingleNode(XPath);
        public static IEnumerable<HtmlAgilityPack.HtmlNode> Descendants(this HtmlAgilityPack.HtmlDocument Document, string name) => Document.DocumentNode.Descendants(name);
        public static string Between(this string String, char Begin, char End, int IndexA = 0, int IndexB = 0) => String.Split(Begin)[IndexA + 1].Split(End)[IndexB];

        public static string ToHTML(this HtmlAgilityPack.HtmlDocument Document)
        {
            using (var Stream = new MemoryStream())
            {
                Document.Save(Stream);
                return Document.Encoding.GetString(Stream.ToArray());
            }
        }

        public static void RemoveSingleNode(this HtmlAgilityPack.HtmlDocument Document, string XPath)
        {
            try
            {
                Document.DocumentNode.SelectSingleNode(XPath)?.Remove();
            }
            catch { }
        }

        public static void RemoveNodes(this HtmlAgilityPack.HtmlDocument Document, string XPath)
        {
            while (true)
            {
                try
                {
                    Document.DocumentNode.SelectSingleNode(XPath).Remove();
                }
                catch
                {
                    break;
                }
            }
        }

        public static object GetAttributeValueByAlias(this HtmlAgilityPack.HtmlNode Node, params string[] Names)
        {
            foreach (var Name in Names)
            {
                var Val = Node.GetAttributeValue(Name, null);

                if (Val != null)
                    return Val;
            }

            return null;
        }
        public static string ToLiteral(this string String, bool Quote = true, bool Apostrophe = false) {
            string Result = string.Empty;
            foreach (char c in String) {
                switch (c) {
                    case '\n':
                        Result += "\\n";
                        break;
                    case '\\':
                        Result += "\\\\";
                        break;
                    case '\t':
                        Result += "\\t";
                        break;
                    case '\r':
                        Result += "\\r";
                        break;
                    case '"':
                        if (!Quote)
                            goto default;
                        Result += "\\\"";
                        break;
                    case '\'':
                        if (!Apostrophe)
                            goto default;
                        Result += "\\'";
                        break;
                    default:
                        Result += c;
                        break;
                }
            }

            return Result;
        }
        public static int EndIndexOf(this string Origin, string Content, int BeginIndex = 0)
        {
            int Val = Origin.IndexOf(Content, BeginIndex);
            if (Val == -1)
                return -1;

            return Val + Content.Length;
        }

        public static string Substring(this string String, string AfterOf, bool CaseSensitive = false, bool IgnoreMissmatch = false)
        {
            int AfterIndex = (CaseSensitive ? String.EndIndexOf(AfterOf) : String.ToLower().EndIndexOf(AfterOf.ToLower()));
            if (AfterIndex == -1 && IgnoreMissmatch)
                AfterIndex = 0;
            return String.Substring(AfterIndex);
        }
        public static string Substring(this string String, string AfterOf, string BeforeOf, bool CaseSensitive = false, bool IgnoreMissmatch = false)
        {
            string Result = String;

            
            if (AfterOf != null)
            {
                int AfterIndex = (CaseSensitive ? String.EndIndexOf(AfterOf) : String.ToLower().EndIndexOf(AfterOf.ToLower()));
                if (AfterIndex == -1 && IgnoreMissmatch)
                    AfterIndex = 0;

                Result = String.Substring(AfterIndex);
            }

            int BeforeIndex = (CaseSensitive ? Result.IndexOf(BeforeOf) : Result.ToLower().IndexOf(BeforeOf.ToLower()));
            if (BeforeIndex == -1 && IgnoreMissmatch)
                BeforeIndex = Result.Length;

            return Result.Substring(0, BeforeIndex);
        }

        public static void AddRange<Key, Value>(this Dictionary<Key, Value> Dictionary, Key[] Keys, Value[] Values)
        {
            if (Keys.Length != Values.Length)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < Keys.Length; i++)
                Dictionary.Add(Keys[i], Values[i]);
        }

        public static string JsonEncode<T>(T Data) {
            return new JavaScriptSerializer().Serialize(Data);
        }

        public static T JsonDecode<T>(string Json) {
            try
            {
                return (T)new JavaScriptSerializer().Deserialize(Json, typeof(T));
            }
            catch {
                return default(T);
            }
        }

        /// <summary>
        /// Returns -1 if read operation failed with an exception,
        /// Returns -2 if read operation failed with timeout
        /// </summary>
        public static async Task<int> TimeoutReadAsync(this Stream strm, byte[] buffer, int offset, int count, TimeSpan Timeout)
        {
            int readed = 0;
            TaskCompletionSource<bool> TCS = new TaskCompletionSource<bool>();
            new Thread(() => {
                try
                {
                    readed = strm.Read(buffer, offset, count);
                    TCS.SetResult(true);
                }
                catch
                {
                    TCS.SetResult(false);
                }
            }).Start();

            var rst = await Task.WhenAny(TCS.Task, Task.Delay(Timeout));
            if (rst is Task<bool> bTask)
            {
                if (!bTask.Result)
                    throw new IOException("Failed to read");

                return bTask.Result ? readed : -1;
            }

            return -2;
        }

        /// <summary>
        /// Returns -1 if write operation failed with an exception,
        /// Returns -2 if write operation failed with timeout
        /// </summary>
        public static async Task TimeoutWriteAsync(this Stream strm, byte[] buffer, int offset, int count, TimeSpan Timeout)
        {
            TaskCompletionSource<bool> TCS = new TaskCompletionSource<bool>();
            new Thread(() => {
                try
                {
                    strm.Write(buffer, offset, count);
                    TCS.SetResult(true);
                }
                catch
                {
                    TCS.SetResult(false);
                }
            }).Start();

            var rst = await Task.WhenAny(TCS.Task, Task.Delay(Timeout));

            if (rst is Task<bool> bTask)
            {
                if (!bTask.Result)
                    throw new IOException("Failed to write");
                return;
            }

            throw new IOException("Pipe not respoding.");
        }
        public static async Task WriteNullableString(this BinaryWriter Writer, string String)
        {
            if (String == null)
            {
                await Writer.BaseStream.TimeoutWriteAsync(new byte[1], 0, 1, TimeSpan.FromSeconds(60));
                return;
            }

            await Writer.BaseStream.TimeoutWriteAsync(new byte[] { 1 }, 0, 1, TimeSpan.FromSeconds(60));
            
            var Data = Encoding.UTF8.GetBytes(String);
            await Writer.BaseStream.TimeoutWriteAsync(BitConverter.GetBytes(Data.Length), 0, 4, TimeSpan.FromSeconds(60));
            await Writer.BaseStream.TimeoutWriteAsync(Data, 0, Data.Length, TimeSpan.FromSeconds(60));
        }
        public static string ReadNullableString(this BinaryReader Reader)
        {
            if (Reader.ReadBoolean())
            {
                var Size = Reader.ReadInt32();
                var Data = new byte[Size];
                Reader.Read(Data, 0, Size);

                return Encoding.UTF8.GetString(Data);
            }

            return null;
        }

        public static async Task WriteStringArray(this BinaryWriter Writer, string[] Strings)
        {
            if (Strings == null)
            {
                await Writer.BaseStream.TimeoutWriteAsync(new byte[1], 0, 1, TimeSpan.FromSeconds(60));
                return;
            }

            await Writer.BaseStream.TimeoutWriteAsync(new byte[] { 1 }, 0, 1, TimeSpan.FromSeconds(60));

            await Writer.BaseStream.TimeoutWriteAsync(BitConverter.GetBytes(Strings.Length), 0, 4, TimeSpan.FromSeconds(60));

            foreach (var String in Strings)
                await Writer.WriteNullableString(String);
        }
        public static string[] ReadStringArray(this BinaryReader Reader)
        {
            if (!Reader.ReadBoolean())
            {
                return null;
            }

            var Data = new string[Reader.ReadInt32()];
            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] = Reader.ReadNullableString();
            }
            return Data;
        }
        public static byte[] GetResponseData(this WebRequest Request) {
            using (var Response = Request.GetResponse())
            using (var ResponseData = Response.GetResponseStream())
            using (var Buffer = new MemoryStream())
            {
                ResponseData.CopyTo(Buffer);
                return Buffer.ToArray();
            }
        }

        public static IEnumerable<T> CatchExceptions<T>(this IEnumerable<T> src, Action<Exception> action = null)
        {
            using (var enumerator = src.GetEnumerator())
            {
                bool next = true;
                while (next)
                {
                    try
                    {
                        next = enumerator.MoveNext();
                    }
                    catch (Exception ex)
                    {
                        if (action != null)
                            action(ex);

                        continue;
                    }

                    if (next)
                        yield return enumerator.Current;
                }
            }
        }

        public static void RegisterWebRequestHandlerEvents(this IWebBrowser Browser, EventHandler<OnWebRequestEventArgs> Request, EventHandler<OnWebResponseEventArgs> Response)
        {
            if (!(Browser.RequestHandler is RequestEventHandler))
                Browser.RequestHandler = new RequestEventHandler();

            ((RequestEventHandler)Browser.RequestHandler).OnResourceRequestEvent += (sender, args) =>
            {
                if (!(args.ResourceRequestHandler is ResourceRequestEventHandler))
                    args.ResourceRequestHandler = new ResourceRequestEventHandler();

                ((ResourceRequestEventHandler)args.ResourceRequestHandler).OnGetResourceHandlerEvent += (sender, args) =>
                {
                    bool IsHttpRequest = args.Request.Url.StartsWith("http", StringComparison.InvariantCultureIgnoreCase);
                    if (IsHttpRequest)
                    {
                        if (!(args.ResourceHandler is WebRequestResourceHandler))
                            args.ResourceHandler = new WebRequestResourceHandler();

                        if (Request != null)
                            ((WebRequestResourceHandler)args.ResourceHandler).OnRequest += Request;

                        if (Response != null)
                            ((WebRequestResourceHandler)args.ResourceHandler).OnResponse += Response;
                    }
                };
            };

            if (!(Browser.ResourceRequestHandlerFactory is ResourceRequestEventHandlerFactory))
                Browser.ResourceRequestHandlerFactory = new ResourceRequestEventHandlerFactory();

            ((ResourceRequestEventHandlerFactory)Browser.ResourceRequestHandlerFactory).OnGetResourceHandlerEvent += (sender, args) =>
            {
                bool IsHttpRequest = args.Request.Url.StartsWith("http", StringComparison.InvariantCultureIgnoreCase);
                if (IsHttpRequest)
                {
                    if (!(args.ResourceHandler is WebRequestResourceHandler))
                        args.ResourceHandler = new WebRequestResourceHandler();

                    if (Request != null)
                        ((WebRequestResourceHandler)args.ResourceHandler).OnRequest += Request;

                    if (Response != null)
                        ((WebRequestResourceHandler)args.ResourceHandler).OnResponse += Response;
                }
            };
        }

        public static IEnumerable<string> OrderByFilenameNumber(this IEnumerable<string> src)
        {
            var Regex = new Regex("(\\d+)\\.(png|jpg|jpeg|bmp|tiff|gif|webp)", RegexOptions.IgnoreCase);
            try
            {
                return src.OrderBy(x => int.Parse(Regex.Match(Path.GetFileName(x.Split('?').First())).Groups[1].Value)).ToArray();
            }
            catch
            {
                return src;
            }
        }
    }
}
