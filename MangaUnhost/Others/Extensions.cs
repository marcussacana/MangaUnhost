using CefSharp;
using CefSharp.OffScreen;
using MangaUnhost.Browser;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;

namespace MangaUnhost {
    static class Extensions {
        internal static CookieContainer ToContainer(this System.Net.Cookie Cookie) => new System.Net.Cookie[] { Cookie }.ToContainer();
        internal static CookieContainer ToContainer(this System.Net.Cookie[] Cookies) {
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
        internal static CookieContainer ToContainer(this CefSharp.Cookie Cookie) => new CefSharp.Cookie[] { Cookie }.ToContainer();
        internal static CookieContainer ToContainer(this CefSharp.Cookie[] Cookies) {
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

        internal static string GetCookie(this ChromiumWebBrowser Browser, string Name) => Browser.GetBrowser().GetCookie(Name);
        internal static CefSharp.Cookie[] GetCookies(this ChromiumWebBrowser Browser) => Browser.GetBrowser().GetCookies();
        internal static void DeleteCookies(this ChromiumWebBrowser Browser) => Browser.GetBrowser().DeleteCookies();

        internal static void DeleteCookie(this ChromiumWebBrowser Browser, string Name) => Browser.GetBrowser().DeleteCookie(Name);

        internal static void UpdateCookie(this ChromiumWebBrowser Browser, System.Net.Cookie Cookie) => Browser.GetBrowser().UpdateCookie(Cookie);

        internal static void UpdateCookie(this ChromiumWebBrowser Browser, CefSharp.Cookie Cookie) => Browser.GetBrowser().UpdateCookie(Cookie);

        internal static string GetCookie(this IBrowser Browser, string Name) {
            return (from x in Browser.GetCookies() where x.Name == Name select x.Value).FirstOrDefault();
        }

        internal static CefSharp.Cookie[] GetCookies(this IBrowser Browser) {
            return AsyncContext.Run(async () => await Cef.GetGlobalCookieManager().VisitUrlCookiesAsync(Browser.MainFrame.Url, true)).ToArray();
        }

        internal static void DeleteCookies(this IBrowser Browser) {
            AsyncContext.Run(async () => await Cef.GetGlobalCookieManager().DeleteCookiesAsync(new Uri(Browser.MainFrame.Url).Host));
        }

        internal static void DeleteCookie(this IBrowser Browser, string Name) {
            AsyncContext.Run(async () => await Cef.GetGlobalCookieManager().DeleteCookiesAsync(Browser.MainFrame.Url, Name));
        }

        internal static void UpdateCookie(this IBrowser Browser, System.Net.Cookie Cookie) {
            Browser.UpdateCookie(new CefSharp.Cookie() {
                Domain = Cookie.Domain,
                Expires = Cookie.Expires,
                HttpOnly = Cookie.HttpOnly,
                Name = Cookie.Name,
                Value = Cookie.Value,
                Path = Cookie.Path
            });
        }
        internal static void UpdateCookie(this IBrowser Browser, CefSharp.Cookie Cookie) {
            AsyncContext.Run(async () => await Cef.GetGlobalCookieManager().SetCookieAsync(Browser.MainFrame.Url, Cookie));
        }

        internal static bool IsRunning(this Thread Thread)
        {
            return Thread.ThreadState == ThreadState.Running || Thread.ThreadState == ThreadState.Background
                || Thread.ThreadState == ThreadState.WaitSleepJoin;
        }

        internal static HtmlAgilityPack.HtmlNodeCollection SelectNodes(this HtmlAgilityPack.HtmlDocument Document, string XPath) => Document.DocumentNode.SelectNodes(XPath);
        internal static HtmlAgilityPack.HtmlNode SelectSingleNode(this HtmlAgilityPack.HtmlDocument Document, string XPath) => Document.DocumentNode.SelectSingleNode(XPath);
        internal static IEnumerable<HtmlAgilityPack.HtmlNode> Descendants(this HtmlAgilityPack.HtmlDocument Document, string name) => Document.DocumentNode.Descendants(name);
        internal static string Between(this string String, char Begin, char End, int IndexA = 0, int IndexB = 0) => String.Split(Begin)[IndexA + 1].Split(End)[IndexB];
   
        internal static string ToLiteral(this string String, bool Quote = true, bool Apostrophe = false) {
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
        internal static int EndIndexOf(this string Origin, string Content, int BeginIndex = 0)
        {
            int Val = Origin.IndexOf(Content, BeginIndex);
            if (Val == -1)
                return -1;

            return Val + Content.Length;
        }   

        internal static string Substring(this string String, string AfterOf, bool CaseSensitive = false)
        {
            return String.Substring((CaseSensitive ? String.EndIndexOf(AfterOf) : String.ToLower().EndIndexOf(AfterOf.ToLower())));
        }
        internal static string Substring(this string String, string AfterOf, string BeforeOf, bool CaseSensitive = false)
        {
            string Result = String.Substring((CaseSensitive ? String.EndIndexOf(AfterOf) : String.ToLower().EndIndexOf(AfterOf.ToLower())));

            return Result.Substring(0, (CaseSensitive ? Result.IndexOf(BeforeOf) : Result.ToLower().IndexOf(BeforeOf.ToLower())));
        }

        internal static void AddRange<Key, Value>(this Dictionary<Key, Value> Dictionary, Key[] Keys, Value[] Values)
        {
            if (Keys.Length != Values.Length)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < Keys.Length; i++)
                Dictionary.Add(Keys[i], Values[i]);
        }

        internal static string JsonEncode<T>(T Data) {
            return new JavaScriptSerializer().Serialize(Data);
        }
        internal static T JsonDecode<T>(string Json) {
            return (T)new JavaScriptSerializer().Deserialize(Json, typeof(T));
        }

        static Point Rand(this Point Point) => new Point(Point.X + new Random().Next(0, 5), Point.Y + new Random().Next(0, 5));
       
    }
}
