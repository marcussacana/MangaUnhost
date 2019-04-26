﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace MangaUnhost.Host
{
    class ManhwaHentai : IHost
    {
        public string HostName => "ManhwaHentai";

        public string DemoUrl => "https://manhwahentai.com/manhwa/close-as-neighbors/";

        public bool NeedsProxy => false;

        public CookieContainer Cookies => null;

        public string UserAgent => null;

        public string Referrer => null;

        public bool SelfChapterDownload => false;


        Dictionary<string, string> NameMap = new Dictionary<string, string>();
        public string GetChapterName(string ChapterURL)
        {
            return NameMap[ChapterURL];

        }

        public string[] GetChapterPages(string HTML)
        {
            HTML = HTML.Substring("</div><div class=reading-content>", "<div class=\"ad");

            string[] Elements = Main.GetElementsByClasses(HTML, "wp-manga-chapter-img");

            string[] Links = (from x in Elements select Main.ExtractHtmlLinks(x, "manhwahentai.com").First()).ToArray();

            if (Links.First().Contains("\n"))
                Links = (from x in Links select x.Substring("\n")).ToArray();

            return Links;
        }

        public string[] GetChapters()
        {
            string HTML = this.HTML.Substring("<div class=\"listing-chapters_wrap", "<div class=c-chapter-readmore>");

            string[] Elms = Main.GetElementsByAttribute(HTML, "href", "http", true);

            List<string> Links = new List<string>();
            foreach (string Elm in Elms)
            {
                string Link = Main.ExtractHtmlLinks(Elm, "manhwahentai.com").First();
                string Name = Elm.Substring("Chapter ", "</a").Trim();

                Links.Add(Link);
                NameMap[Link] = Name;
            }

            return Links.ToArray();
        }

        public string GetFullName()
        {
            string Title = this.HTML.Substring("property=og:image:alt content=\"", "\">");
            return HttpUtility.HtmlDecode(Title);
        }

        public string GetName(string CodedName)
        {
            return Main.GetRawNameFromUrlFolder(CodedName);
        }

        public string GetPosterUrl()
        {
            string HTML = this.HTML.Substring("<div class=summary_image>");
            HTML = "<img " + HTML.Substring("<img ", "</a>");

            var Links = Main.ExtractHtmlLinks(HTML, "manhwahentai.com");

            return Links.First();
        }

        public void Initialize(string URL, out string Name, out string Page)
        {
            if (!IsValidLink(URL))
                throw new Exception();

            URL = URL.Split('?')[0];
            Name = URL.Substring("/manhwa/", "/");
            Name = Main.GetRawNameFromUrlFolder(Name);

            Page = URL;
        }

        public bool IsValidLink(string URL)
        {
            return URL.ToLower().Contains("manhwahentai.com/manhwa/") && !URL.ToLower().EndsWith("/manhwa/");
        }

        string HTML;
        public void LoadPage(string URL)
        {
            HTML = Main.Download(URL, Encoding.UTF8);
        }

        public bool ValidateProxy(string Proxy)
        {
            throw new NotImplementedException();
        }
    }
}
