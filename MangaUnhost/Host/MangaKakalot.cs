using System;
using System.Linq;
using System.Net;
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
        public CookieContainer Cookies {
            get {
                return null;
            }
        }

        public string UserAgent { get { return null; } }
        public string GetChapterName(string ChapterURL) {
            const string Prefix1 = "chapter_";
            const string Prefix2 = "chapter-";
            string Name = string.Empty;
            if (ChapterURL.ToLower().Contains(Prefix1))
                Name = ChapterURL.Substring(ChapterURL.ToLower().IndexOf(Prefix1) + Prefix1.Length);
            else if (ChapterURL.ToLower().Contains(Prefix2))
                Name = ChapterURL.Substring(ChapterURL.ToLower().IndexOf(Prefix2) + Prefix2.Length);
            else
                throw new Exception("Unsupported Manga, Report It");

            return Name;
        }

        public string[] GetChapterPages(string HTML) {
            int Index = HTML.IndexOf("<div class=\"vung-doc\" id=\"vungdoc\"");
            if (Index < 0) {
                const string VarPrefix = "_book_link = '";
                string Url = HTML.Substring(HTML.IndexOf(VarPrefix) + VarPrefix.Length).Split('\'')[0].TrimStart('\\');
                Url = "http://" + Domain + "/" + Url + "/0";
                return GetChapterPages(Main.Download(Url, Encoding.UTF8));
            }
            int EndIndex = HTML.IndexOf("<div style=\"text-align:center;margin-top: 15px;\">", Index);
            if (EndIndex < 0)
                EndIndex = HTML.IndexOf("<div style=\"text-align:center;\">", Index);
            if (EndIndex < 0)
                EndIndex = HTML.IndexOf("<div style=\"max-width:", Index);

            string[] Links = Main.ExtractHtmlLinks(HTML.Substring(Index, EndIndex - Index), Domain);

            Links = (from x in Links where !x.Split('?')[0].ToLower().EndsWith(".js") && !x.Split('?')[0].ToLower().EndsWith(".php") &&
                     !x.Split('?')[0].ToLower().EndsWith(".css") && !x.Contains("/ads/") select x).ToArray();

            //Links = (from x in Links where x.Contains("blogspot.com") select x).Distinct().ToArray();

            return Links;
        }

        public string[] GetChapters() {
            int Index = HTML.IndexOf("<div class=\"chapter-list\">");
            string[] Links = Main.ExtractHtmlLinks(HTML.Substring(Index), Domain);

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
            return Main.ExtractHtmlLinks(Element, Domain).First();
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
                && URL.Contains("mangakakalot.") 
                && URL.Contains("/manga/");
        }

        string Domain;
        public void LoadPage(string URL) {
            Domain = URL.ToLower().Replace("https://", "").Replace("http://", "").Split('/')[0];
            HTML = Main.Download(URL, Encoding.UTF8);
        }

        public bool ValidateProxy(string Proxy) {
            throw new NotImplementedException();
        }
    }
}
