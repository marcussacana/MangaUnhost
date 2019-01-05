using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MangaUnhost.Host {
    class MangaKakalot : IHost {
        string HTML;
        public bool NeedsProxy {
            get {
                return false;
            }
        }
        public string HostName {
            get {
                return "MangaKakalot";
            }
        }

        public string DemoUrl {
            get {
                return "http://mangakakalot.com/manga/choujin_koukouseitachi_wa_isekai_demo_yoyuu_de_ikinuku_you_desu";
            }
        }

        public string GetChapterName(string ChapterURL) {
            const string Prefix = "chapter_";
            string Name = ChapterURL.Substring(ChapterURL.IndexOf(Prefix) + Prefix.Length);

            return Name;
        }

        public string[] GetChapterPages(string HTML) {
            int Index = HTML.IndexOf("<div class=\"vung-doc\" id=\"vungdoc\">");
            int EndIndex = HTML.IndexOf("<div style=\"text-align:center;margin-top: 15px;\">", Index);
            if (EndIndex < 0)
                EndIndex = HTML.IndexOf("<div style=\"text-align:center;\">", Index);

            string[] Links = Main.ExtractHtmlLinks(HTML.Substring(Index, EndIndex - Index), "mangakakalot.com");

            Links = (from x in Links where !x.Split('?')[0].ToLower().EndsWith(".js") && !x.Split('?')[0].ToLower().EndsWith(".php") &&
                     !x.Split('?')[0].ToLower().EndsWith(".css") && !x.Contains("/ads/") select x).ToArray();

            //Links = (from x in Links where x.Contains("blogspot.com") select x).Distinct().ToArray();

            return Links;
        }

        public string[] GetChapters() {
            int Index = HTML.IndexOf("<div class=\"chapter-list\">");
            string[] Links = Main.ExtractHtmlLinks(HTML.Substring(Index), "mangakakalot.com");

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
            return Main.ExtractHtmlLinks(Element, "mangakakalot.com").First();
        }

        public void Initialize(string URL, out string Name, out string Page) {
            //http://mangakakalot.com/manga/tales_of_demons_and_gods
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
                && URL.Contains("mangakakalot.com") 
                && URL.Contains("/manga/");
        }

        public void LoadPage(string URL) {
            HTML = Main.Download(URL, Encoding.UTF8);
        }

        public bool ValidateProxy(string Proxy) {
            throw new NotImplementedException();
        }
    }
}
