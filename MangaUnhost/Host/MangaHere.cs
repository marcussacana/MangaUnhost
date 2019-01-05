﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MangaUnhost.Host {
    class MangaHere : IHost {
        string HTML;
        public bool NeedsProxy {
            get {
                return false;
            }
        }
        public string HostName { get {
                return "MangaHere";
            }
        }

        public string DemoUrl {
            get {
                return "http://www.mangahere.cc/manga/konjiki_no_moji_tsukai_yuusha_yonin_ni_makikomareta_unique_cheat/";
            }
        }

        public string GetChapterName(string ChapterURL) {            
            const string Prefix = "/manga/";

            string Name = ChapterURL.Substring(ChapterURL.TrimEnd('/').LastIndexOf('/')).Split('/')[1];


            try {
                return double.Parse(Name.Trim('c', ' ').Replace(".", ",")).ToString().Replace(",", ".");
            } catch {
                return Name;
            }
        }

        public string[] GetChapterPages(string HTML) {
            string[] Elements = Main.GetElementsByAttribute(HTML, "value", "www.mangahere.cc/manga", ContainsOnly: true);
            List<string> URLs = new List<string>();

            foreach (string Element in Elements) {
                string[] Results = Main.ExtractHtmlLinks(Element, "www.mangahere.cc");

                if (!URLs.Contains(Results[0]) && !Results[0].EndsWith("featured.html"))
                    URLs.Add(Results[0]);
            }

            List<string> Pages = new List<string>();
            foreach (string URL in URLs) {
                string cHTML = Main.Download(URL, Encoding.UTF8);
                string Element = Main.GetElementsByAttribute(cHTML, "onload", "loadImg", true).First();
                Pages.Add(Main.ExtractHtmlLinks(Element, "www.mangahere.cc").First());
            }

            return Pages.ToArray();
        }

        public string[] GetChapters() {
            int TOCBegin = HTML.IndexOf("<div class=\"detail_list\">");
            string[] Elements = Main.GetElementsByClasses(HTML, TOCBegin, "color_0077");

            List<string> URLs = new List<string>();
            foreach (string Element in Elements) {
                string[] Results = Main.ExtractHtmlLinks(Element, "www.mangahere.cc");
                if (Results[0].Contains("www.mangahere.cc/manga/"))
                    URLs.Add(Results[0]);
            }

            return URLs.ToArray();
        }

        public string GetFullName() {
            string Title = Main.GetElementsByClasses(HTML, 0, "title_icon").First();
            Title = Title.Split('>')[2].Split('<')[0];            
            return Main.GetRawNameFromUrlFolder(Title, true);
        }

        public string GetName(string CodedName) {
            return Main.GetRawNameFromUrlFolder(CodedName);
        }

        public string GetPosterUrl() {
            string Element = Main.GetElementsByContent(HTML, "img src=\"").First();

            return Main.ExtractHtmlLinks(Element, "www.mangahere.cc").First();
        }

        public void Initialize(string URL, out string Name, out string Page) {
            if (!IsValidLink(URL))
                throw new Exception();

            const string Prefix = "/manga/";
            int Index = URL.IndexOf(Prefix);
            if (Index < 0)
                throw new Exception();
            Index += Prefix.Length;

            string CodedName = URL.Substring(Index, URL.Length - Index).Split('/')[0];
            Name = GetName(CodedName);
            Page = URL;
        }

        public bool IsValidLink(string URL) {
            URL = URL.ToLower();
            return URL.Contains("mangahere") && URL.Contains("/manga/") && Uri.IsWellFormedUriString(URL, UriKind.Absolute);
        }

        public void LoadPage(string URL) {
            HTML = Main.Download(URL, Encoding.UTF8);
        }

        public bool ValidateProxy(string Proxy) {
            throw new NotImplementedException();
        }
    }
}
