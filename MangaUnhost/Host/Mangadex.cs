﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace MangaUnhost.Host
{
    class Mangadex : IHost
    {
        string IniPath = AppDomain.CurrentDomain.BaseDirectory + "MangaDex.ini";
        public Mangadex()
        {
            if (!System.IO.File.Exists(IniPath))
                Ini.SetConfig("MangaDex", "Language", "1", IniPath);
        }

        public string HostName => "MangaDex";

        public string DemoUrl => "https://mangadex.org/title/32944/i-am-a-child-of-this-house";

        public bool NeedsProxy => false;

        public CookieContainer Cookies => BrowserCookies.ToContainer();

        public string UserAgent => UA;

        public string Referrer => null;

        public bool SelfChapterDownload => true;

        public string GetChapterName(string ChapterURL)
        {
            return NameMap[ChapterURL];
        }

        public string[] GetChapterPages(string URL)
        {
            string ID = URL.Substring("/chapter/");

            string API = $"https://mangadex.org/api/?id={ID}&type=chapter&baseURL=%2Fapi";

            string Response = Main.Download(API, Encoding.UTF8, Cookies: Cookies, Referrer: "https://mangadex.org", UserAgent: UA);

            var Result = Extensions.JsonDecode<MangaDexApi>(Response);

            if (Result.status != "OK")
                throw new Exception();

            if (!Result.server.ToLower().Contains(".mangadex.org"))
                Result.server = "https://mangadex.org" + Result.server;

            List<string> Pages = new List<string>();
            foreach (string Page in Result.page_array)
            {
                Pages.Add($"{Result.server}{Result.hash}/{Page}");
            }

            return Pages.ToArray();
        }

        public string[] GetChapters()
        {
            List<string> Links = new List<string>();
            foreach (string HTML in HTMLs)
            {
                string[] Chapters = GetChapters(HTML, LID);
                string[] Names = GetChaptersName(HTML, LID);

                if (Names.Length != Chapters.Length)
                    throw new Exception();

                Links.AddRange(Chapters);
                NameMap.AddRange(Chapters, Names);
            }

            return Links.ToArray();
        }

        Dictionary<string, string> NameMap = new Dictionary<string, string>();

        public string GetFullName()
        {
            string HTML = HTMLs.First();

            string Name = HTML.Substring("<span class=\"mx-1\">", "</span>");

            return HttpUtility.HtmlDecode(Name);
        }

        public string GetName(string CodedName)
        {
            return Main.GetRawNameFromUrlFolder(CodedName);
        }

        public string GetPosterUrl()
        {
            return $"https://mangadex.org/images/manga/{ID}.jpg";
        }

        public void Initialize(string URL, out string Name, out string Page)
        {
            if (!IsValidLink(URL))
                throw new Exception();

            string Path = URL.Substring("/title/");

            Title = Path.Split('/')[1];
             ID = Path.Split('/')[0];

            Page = $"https://mangadex.org/title/{ID}/{Title}";
            Name = Main.GetRawNameFromUrlFolder(Title);
        }

        public bool IsValidLink(string URL)
        {
            //https://mangadex.org/title/32944/i-am-a-child-of-this-house
            return URL.ToLower().Contains("mangadex.org/title/") && !URL.ToLower().EndsWith("/title/");
        }

        public void LoadPage(string URL)
        {
            string PageMask = "https://mangadex.org/title/{0}/{1}/chapters/{2}";

            var CFData = Main.BypassCloudflare(string.Format(PageMask, ID, Title, "1"));
            BrowserCookies = CFData.AllCookies;
            UA = CFData.UserAgent;


            if (System.IO.File.Exists(IniPath))
                LID = Ini.GetConfig("MangaDex", "Language", IniPath);
            else
                LID = null;

            List<string> Pages = new List<string>();
            int PageNum = 1;
            while (true)
            {
                string Page = Main.Download(string.Format(PageMask, ID, Title, PageNum++), Encoding.UTF8, Cookies: Cookies, UserAgent: UA);
                if (GetChapters(Page, LID).Length == 0)
                {
                    if (Pages.Count == 0 && LID != null)
                    {
                        LID = null;
                        PageNum--;
                        continue;
                    }
                    break;
                }

                Pages.Add(Page);
            }

            HTMLs = Pages;
        }

        public bool ValidateProxy(string Proxy)
        {
            throw new NotImplementedException();
        }

        private string[] GetChapters(string HTML, string Lang = null)
        {
            string[] Elms = Main.GetElementsByClasses(HTML, "chapter-row", "d-flex", "row", "no-gutters", "p-2", "align-items-center", "border-bottom", "odd-row");

            List<string> Links = new List<string>();
            foreach (string Elm in Elms)
            {
                if (!Elm.Contains("data-id"))
                    continue;

                if (Lang != null && Main.GetElementAttribute(Elm, "data-lang") != Lang)
                    continue;

                Links.Add($"https://mangadex.org/chapter/{Main.GetElementAttribute(Elm, "data-id")}");
            }

            return Links.ToArray();
        }
        private string[] GetChaptersName(string HTML, string Lang = null)
        {
            string[] Elms = Main.GetElementsByClasses(HTML, "chapter-row", "d-flex", "row", "no-gutters", "p-2", "align-items-center", "border-bottom", "odd-row");

            List<string> Names = new List<string>();
            foreach (string Elm in Elms)
            {
                if (!Elm.Contains("data-chapter"))
                    continue;

                if (Lang != null && Main.GetElementAttribute(Elm, "data-lang") != Lang)
                    continue;

                Names.Add(Main.GetElementAttribute(Elm, "data-chapter"));
            }

            return Names.ToArray();
        }

        struct MangaDexApi
        {
            public int? id;
            public long? timestamp;
            public string hash;
            public string volume;
            public string chapter;
            public string title;
            public string lang_name;
            public string lang_code;
            public int? manga_id;
            public int? group_id;
            public int? group_id_1;
            public int? group_id_2;
            public int? group_id_3;
            public int? comments;
            public string server;
            public string[] page_array;
            public int? long_strip;
            public string status;
        }

        string LID = null;

        string Title;
        string ID;

        string UA;

        List<string> HTMLs = new List<string>();

        Cookie[] BrowserCookies;
    }
}
