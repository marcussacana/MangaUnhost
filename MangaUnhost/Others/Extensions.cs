﻿using CefSharp;
using CefSharp.OffScreen;
using MangaUnhost.Browser;
using Nito.AsyncEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Web.Script.Serialization;

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
            return AsyncContext.Run(async () => await Cef.GetGlobalCookieManager().VisitUrlCookiesAsync(Browser.MainFrame.Url, true)).ToArray();
        }

        public static void DeleteCookies(this IBrowser Browser) {
            AsyncContext.Run(async () => await Cef.GetGlobalCookieManager().DeleteCookiesAsync(new Uri(Browser.MainFrame.Url).Host));
        }

        public static void DeleteCookie(this IBrowser Browser, string Name) {
            AsyncContext.Run(async () => await Cef.GetGlobalCookieManager().DeleteCookiesAsync(Browser.MainFrame.Url, Name));
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
            AsyncContext.Run(async () => await Cef.GetGlobalCookieManager().SetCookieAsync(Browser.MainFrame.Url, Cookie));
        }

        public static bool IsRunning(this Thread Thread)
        {
            return Thread.ThreadState == ThreadState.Running || Thread.ThreadState == ThreadState.Background
                || Thread.ThreadState == ThreadState.WaitSleepJoin;
        }


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
            return (T)new JavaScriptSerializer().Deserialize(Json, typeof(T));
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
    }
}
