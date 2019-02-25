using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace MangaUnhost.Host {
    class HentaiCafe : IHost {
        string HTML;
        public bool NeedsProxy => false;

        public string HostName => "Hentai Cafe";

        public string DemoUrl => "https://hentai.cafe/aiue-oka-bitch-fuck/";

        public CookieContainer Cookies => null;

        public string UserAgent => null;

        public string Referrer => null;

        public string GetChapterName(string ChapterURL) => "One Shot";

        public string[] GetChapterPages(string HTML) {
            string[] PageElements = Main.GetElementsByAttribute(HTML, "onClick", "changePage(", true);

            string[] Links = new string[PageElements.Length];
            for (int i = 0; i < Links.Length; i++) {
                string Link = Main.ExtractHtmlLinks(PageElements[i], "hentai.cafe").First();
                Links[i] = Link;
            }

            for (int i = 0; i < Links.Length; i++) {
                string pHTML = Main.Download(Links[i], Encoding.UTF8);
                Links[i] = Main.ExtractHtmlLinks(Main.GetElementsByClasses(pHTML, "open").First(), "hentai.cafe").First();
            }

            return Links;
        }

        public string[] GetChapters() {
            string Element = Main.GetElementsByClasses(HTML, "x-btn", "x-btn-flat", "x-btn-rounded", "x-btn-large").First();
            return new string[] { Main.ExtractHtmlLinks(Element, "hentai.cafe").First() };
        }

        public string GetFullName() {
            string Element = Main.GetElementsByClasses(HTML, "entry-title").First();
        
            string Name = Element.Between('>', '<');
            string Result = string.Empty;

            bool InTag = false;
            foreach (char c in Name)
                switch (c) {
                    case '[':
                        InTag = true;
                        break;
                    case ']':
                        InTag = false;
                        break;
                    default:
                        if (InTag)
                            break;
                        Result += c;
                        break;
                }

            return Main.GetRawNameFromUrlFolder(Result.Trim(), true);
        }

        public string GetName(string CodedName) {
            return Main.GetRawNameFromUrlFolder(CodedName);
        }

        public string GetPosterUrl() {
            string sHTML = HTML.Substring(HTML.IndexOf("<div class=\"entry-content content\">"));
            string URL = Main.ExtractHtmlLinks(sHTML, "hentai.cafe").First();

            return URL;
        }

        public void Initialize(string URL, out string Name, out string Page) {
            if (!IsValidLink(URL))
                throw new Exception();

            Name = GetName(URL.Substring(URL.IndexOf(".cafe/")).Split('/')[1]);
            Page = URL;
        }

        public bool IsValidLink(string URL) {
            //https://hentai.cafe/utu-ultra-after/
            URL = URL.ToLower();
            return Uri.IsWellFormedUriString(URL, UriKind.Absolute) && URL.Contains("hentai.cafe") && URL.Substring(URL.IndexOf(".cafe/")).Split('/')[1].Length > 2;
        }

        public void LoadPage(string URL) {
            HTML = Main.Download(URL, Encoding.UTF8);
        }

        public bool ValidateProxy(string Proxy) {
            throw new NotImplementedException();
        }
    }
}
