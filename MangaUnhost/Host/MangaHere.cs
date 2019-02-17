using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        public CookieContainer Cookies {
            get {
                return null;
            }
        }
        public string UserAgent { get { return null; } }

        public string GetChapterName(string ChapterURL) {            
            const string Prefix = "/manga/";

            string Name = ChapterURL.Substring(ChapterURL.ToLower().IndexOf(Prefix));
            Name = Name.Split('/')[3].TrimStart('v', '0') + '.' + Name.Split('/')[4].TrimStart('c', '0');

            try {
                return double.Parse(Name.Trim('c', ' ').Replace(".", ",")).ToString().Replace(",", ".");
            } catch {
                return Name;
            }
        }
        
        public string[] GetChapterPages(string HTML) {
            string CID = HTML.Substring(HTML.IndexOf("chapterid")).Split(';')[0].Split('=')[1].Trim();

            int Begin = HTML.IndexOf("eval");
            int EndInd = HTML.IndexOf("</script>", Begin);
            string Script = HTML.Substring(0, EndInd).Substring(Begin);
            Script = Script.Beautifier();

            string Key = string.Empty;
            bool Swich = false;
            foreach (string Part in Script.Split(';')[0].Split('\'')) {
                Swich = !Swich;
                if (Swich)
                    continue;

                if (Part.Length == 1)
                    Key += Part;
            }

            string Page = Main.GetElementsByAttribute(HTML, "name", "og:url").First();
            Page = Main.GetElementAttribute(Page, "content");
            Page = Page.Substring(0, Page.LastIndexOf("/"));

            string URL = $"{Page}/chapterfun.ashx?cid={CID}&page=2&key=";

            string JS = Main.Download(URL, Encoding.UTF8);


            throw new NotImplementedException();
            //return Pages.ToArray();
        }

        public string[] GetChapters() {
            int TOCBegin = HTML.IndexOf("bookmarkbt");
            while (HTML[TOCBegin] != '<')
                TOCBegin--;

            int EndInd = HTML.IndexOf("detail-main-list-ad", TOCBegin);

            string[] Elements = Main.GetElementsByAttribute(HTML.Substring(0, EndInd), "target", "_blank", StartIndex: TOCBegin);

            List<string> URLs = new List<string>();
            foreach (string Element in Elements) {
                string[] Results = Main.ExtractHtmlLinks(Element, "www.mangahere.cc");
                if (Results[0].Contains("www.mangahere.cc/manga/"))
                    URLs.Add(Results[0]);
            }

            return URLs.ToArray();
        }

        public string GetFullName() {
            string Title = Main.GetElementsByClasses(HTML, 0, "detail-info-right-title-font").First();
            Title = Title.Split('>')[1].Split('<')[0];            
            return Main.GetRawNameFromUrlFolder(Title, true);
        }

        public string GetName(string CodedName) {
            return Main.GetRawNameFromUrlFolder(CodedName);
        }

        public string GetPosterUrl() {
            string Element = Main.GetElementsByClasses(HTML, 0, "detail-info-cover-img").First();

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
