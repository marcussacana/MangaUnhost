using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MangaUnhost.Host {
    public class HeavenManga : IHost {
        string HTML;
        public string HostName {
            get {
                return "Heaven Manga";
            }
        }

        public string DemoUrl {
            get {
                return "http://heavenmanga.today/hentai-elf-to-majime-orc/";
            }
        }

        public string GetChapterName(string ChapterURL) {
            const string Prefix = "chap-";
            string CN = ChapterURL.Substring(ChapterURL.IndexOf(Prefix) + Prefix.Length).Trim('\\');
            if (CN.Split('-').Length > 2)
                return GetName(CN);
            else
                return CN.Replace("-", ".");
        }

        public string[] GetChapterPages(string HTML) {
            int BID = HTML.IndexOf("<center>");
            int Len = HTML.IndexOf("<span style=", BID) - BID;
            HTML = HTML.Substring(BID, Len);
            string[] Links = Main.ExtractHtmlLinks(HTML, "heavenmanga.today");

            return Links;
        }

        public string[] GetChapters() {
            string HTML = this.HTML;
            int io = 0;
            List<string> Chapters = new List<string>();
            while ((io = HTML.IndexOf("<h2 class=\"chap\">", io + 1)) > 0) {
                string Elm = HTML.Substring(io);
                int EndPos = Elm.IndexOf("<span ");
                string Link = Main.ExtractHtmlLinks(Elm.Substring(0, EndPos), "heavenmanga.today").First();
                Chapters.Add(Link);
            }

            return Chapters.ToArray();
        }

        public string GetFullName() {
            string[] Rsts = Main.GetElementsByClasses(HTML, 0, "name", "bigger");
            string Element = Rsts.First();

            return Element.Split('>')[1].Split('<')[0];
        }

        public string GetName(string CodedName) {
            return Main.GetRawNameFromUrlFolder(CodedName);
        }

        public string GetPosterUrl() {
            string HTML = this.HTML.Substring(this.HTML.IndexOf("thumb text-center"));
            int EndInd = HTML.IndexOf("</div>");
            string Link = Main.ExtractHtmlLinks(HTML.Substring(0, EndInd), "heavenmanga.today").First();
            return Link;
        }

        public void Initialize(string URL, out string Name, out string Page) {
            if (!IsValidLink(URL))
                throw new Exception();

            Name = GetName(URL.Substring(URL.IndexOf(".today")).Split('/')[1]);
            Page = URL;
        }

        public bool IsValidLink(string URL) {
            return Uri.IsWellFormedUriString(URL, UriKind.Absolute) && URL.Contains("heavenmanga.today") && URL.Substring(URL.IndexOf(".today")).Split('/').Length >= 2;
        }

        public void LoadPage(string URL) {
            HTML = Main.Download(URL, Encoding.UTF8);
        }
    }
}
