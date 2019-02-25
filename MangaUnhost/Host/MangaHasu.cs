﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace MangaUnhost.Host {
    class MangaHasu : IHost {
        public string HostName => "MangaHasu";

        public string DemoUrl => "http://mangahasu.se/i-belong-to-house-castiello-p35077.html";

        public bool NeedsProxy => false;

        public CookieContainer Cookies => null;

        public string UserAgent => null;

        public string GetChapterName(string ChapterURL) {
            string[] Parts = ChapterURL.Split('-');
            string Name = string.Join("-", Parts.Take(Parts.Length - 1));
            Name = Name.Split('-').Last();
            return Name;
        }

        public string[] GetChapterPages(string HTML) {
            string[] Elements = Main.GetElementsByAttribute(HTML, "class", "page", true);

            string PageList = string.Join("", Elements);

            return Main.ExtractHtmlLinks(PageList, "mangahasu.se").Distinct().ToArray();
        }

        public string[] GetChapters() {
            string HTML = this.HTML.Substring(this.HTML.IndexOf("list-chapter"));

            HTML = HTML.Substring(0, HTML.IndexOf("</div>"));

            return Main.ExtractHtmlLinks(HTML, "mangahasu.se");
        }

        public string GetFullName() {
            string Title = Main.GetElementsByAttribute(HTML, "itemprop", "title").First();

            Title = HttpUtility.HtmlDecode(Title.Between('>', '<'));

            return Title;
        }

        public string GetName(string CodedName) {
            return Main.GetRawNameFromUrlFolder(CodedName);
        }

        public string GetPosterUrl() {
            string Link = Main.GetElementsByAttribute(HTML, "property", "og:image").First();
            Link = Link.Between('"', '"', 2);
            return Link;
        }

        //http://mangahasu.se/i-belong-to-house-castiello-p35077.html
        public void Initialize(string URL, out string Name, out string Page) {
            if (!IsValidLink(URL))
                throw new Exception();

            Name = URL.Substring(URL.ToLower().IndexOf(".se/"));
            Name = Name.Split('/')[1];
            Name = string.Join(" ", Name.Split('-').Take(Name.Split('-').Length - 1));

            Name = GetName(Name);
            Page = URL;
        }

        public bool IsValidLink(string URL) {
            string Last = URL.ToLower().Split('-').Last();
            return URL.ToLower().Contains("mangahasu.se/") && Last.Contains("p") && Last.EndsWith(".html");
        }

        string HTML;
        public void LoadPage(string URL) {
            HTML = Main.Download(URL, Encoding.UTF8);
        }

        public bool ValidateProxy(string Proxy) {
            throw new NotImplementedException();
        }
    }
}
