using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MangaUnhost.Host {
    class RawLH : IHost {
        string HTML;
        public bool NeedsProxy {
            get {
                return false;
            }
        }
        public string HostName {
            get {
                return "RawLH";
            }
        }
        public string DemoUrl {
            get {
                return "http://rawlh.com/manga-29-to-jk-raw.html";
            }
        }
        public string GetChapterName(string ChapterURL) {
            //http://rawlh.com/read-29-to-jk-raw-chapter-1.html
            const string Prefix = "-chapter-";
            int Index = ChapterURL.ToLower().IndexOf(Prefix);
            if (Index < 0)
                throw new Exception();
            Index += Prefix.Length;

            return ChapterURL.Substring(Index).Replace(".html", "");
        }

        public string[] GetChapterPages(string HTML) {
            string[] Elements = Main.GetElementsByClasses(HTML, 0, "chapter-img");

            List<string> Links = new List<string>();
            foreach (string Element in Elements) {
                string Link = Main.GetElementAttribute(Element, "src").TrimEnd('\n');
                Link = HttpUtility.HtmlDecode(Link);
                if (Link.Contains("&url=")) {
                    Link = Link.Substring(Link.IndexOf("&url=") + 5).Trim();
                }
                if (!Uri.IsWellFormedUriString(Link, UriKind.Absolute))
                    continue;
                Links.Add(Link);
            }

            return Links.ToArray();
        }

        public string[] GetChapters() {
            string[] Elements = Main.GetElementsByClasses(HTML, 0, "chapter");

            string[] Links = new string[Elements.Length];
            for (int i = 0; i < Links.Length; i++)
                Links[i] = Main.ExtractHtmlLinks(Elements[i], "rawlh.com").First();

            return Links;
        }

        public string GetFullName() {
            int Index = HTML.IndexOf("<meta itemprop=\"position\" content=\"2\">");
            string Element = Main.GetElementsByAttribute(HTML, "itemprop", "name", true, false, Index).First();

            string Name = HttpUtility.HtmlDecode(Element.Split('>')[1].Split('<')[0]);

            return Name.Replace("- Raw", "").Trim();
        }

        public string GetName(string CodedName) {
            const string Prefix = "manga-";
            int Index = CodedName.ToLower().IndexOf(Prefix);
            if (Index < 0)
                throw new Exception();
            Index += Prefix.Length;

            string Name = CodedName.Substring(Index, CodedName.Length - Index);
            Name = Name.Replace("-raw.html", "").Replace(".html", "");
            return Main.GetRawNameFromUrlFolder(Name);
        }

        public string GetPosterUrl() {
            string Element = Main.GetElementsByClasses(HTML, 0, "hide").First();

            string Link = Main.ExtractHtmlLinks(Element, "rawlh.com").First();

            return Link;
        }

        public void Initialize(string URL, out string Name, out string Page) {
            if (!IsValidLink(URL))
                throw new Exception();


            Name = GetName(URL.Substring(URL.IndexOf("com/")).Split('/')[1]);
            Page = URL;
        }

        public bool IsValidLink(string URL) {
            URL = URL.ToLower();
            return Uri.IsWellFormedUriString(URL, UriKind.Absolute) && URL.Contains("rawlh.com") && URL.EndsWith(".html") && !URL.Contains("-chapter-");
        }

        public void LoadPage(string URL) {
            HTML = Main.Download(URL, Encoding.UTF8);
        }
    }
}
