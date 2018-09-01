using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MangaUnhost.Host {
    class MangaNelo : IHost {
        string HTML;
        public bool NeedsProxy {
            get {
                return false;
            }
        }
        public string HostName {
            get {
                return "MangaNelo";
            }
        }

        public string DemoUrl {
            get {
                return "http://manganelo.com/manga/isekai_cheat_magician";
            }
        }

        public string GetChapterName(string ChapterURL) {
            const string Prefix = "chapter_";
            string Name = ChapterURL.Substring(ChapterURL.IndexOf(Prefix) + Prefix.Length);

            return Name;
        }

        public string[] GetChapterPages(string HTML) {
            int Index = HTML.IndexOf("<div class=\"vung-doc\" id=\"vungdoc\">");
            int EndIndex = HTML.IndexOf("<div style=\"max-height:", Index);

            string[] Links = Main.ExtractHtmlLinks(HTML.Substring(Index, EndIndex - Index), "manganelo.com");

            Links = (from x in Links where !x.Split('?')[0].ToLower().EndsWith(".js") && !x.Split('?')[0].ToLower().EndsWith(".css") select x).ToArray();
            //Links = (from x in Links where x.Contains("blogspot.com") select x).Distinct().ToArray();

            return Links;
        }

        public string[] GetChapters() {
            int Index = HTML.IndexOf("<div class=\"chapter-list\">");
            string[] Links = Main.ExtractHtmlLinks(HTML.Substring(Index), "manganelo.com");

            return (from x in Links where x.Contains("/chapter/") select x).Distinct().ToArray();
        }

        public string GetFullName() {
            int Index = HTML.IndexOf("manga-info-pic");
            string Element = Main.GetElementsByContent(HTML, "img src=", Index).First();
            return Main.GetElementAttribute(Element, "alt");
        }

        public string GetName(string CodedName) {
            return Main.GetRawNameFromUrlFolder(CodedName); ;
        }

        public string GetPosterUrl() {
            int Index = HTML.IndexOf("manga-info-pic");
            string Element = Main.GetElementsByContent(HTML, "img src=", Index).First();
            return Main.ExtractHtmlLinks(Element, "manganelo.com").First();
        }

        public void Initialize(string URL, out string Name, out string Page) {
            //http://manganelo.com/manga/isekai_cheat_magician
            if (!IsValidLink(URL))
                throw new Exception();

            const string Prefix = "/manga/";
            int Index = URL.IndexOf(Prefix);
            if (Index < 0)
                throw new Exception();
            Index += Prefix.Length;

            Name = GetName(URL.Substring(Index, URL.Length - Index).Split('/')[0]);
            Page = URL;
        }

        public bool IsValidLink(string URL) {
            URL = URL.ToLower();
            return Uri.IsWellFormedUriString(URL, UriKind.Absolute) 
                && URL.Contains("manganelo.com")
                && URL.Contains("/manga/");
        }

        public void LoadPage(string URL) {
            HTML = Main.Download(URL, Encoding.UTF8);
        }
    }
}
