using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace MangaUnhost.Host {
    class Batoto : IHost {
        public string HostName => "Batoto";

        public string DemoUrl => "https://bato.to/series/69445";

        public bool NeedsProxy => false;

        public CookieContainer Cookies => null;

        public string UserAgent => null;

        public string Referrer => null;


        public string GetChapterName(string ChapterURL) {
            return NameMap[ChapterURL.ToLower()];
        }

        public string[] GetChapterPages(string HTML) {
            string Script = Main.GetElementsByContent(HTML, "images", 0, false).First();

            List<string> Pages = new List<string>();
            while (true) {
                int Index = Script.IndexOf("\":\"");
                if (Index < 0)
                    break;
                Script = Script.Substring(Index + 2);
                if (!Script.ToLower().StartsWith("\"http"))
                    break;

                string Link = Script.Between('"', '"');
                Pages.Add(Link);
            }

            return Pages.ToArray();
        }

        Dictionary<string, string> NameMap = new Dictionary<string, string>();
        public string[] GetChapters() {
            string[] Elements = Main.GetElementsByClasses(HTML, "chapt");

            List<string> Links = new List<string>();
            foreach (string Element in Elements) {
                string Name = Element.Between('>', '<', 1);
                if (Name.ToLower().Contains("[delete]"))
                    continue;

                Name = Name.ToLower().Replace("ch.", "").Replace("vol.", "").Trim().Replace("  ", ".").Replace(" ", ".");

                string Link = Main.ExtractHtmlLinks(Element, "bato.to").First();

                NameMap[Link.ToLower()] = Name;

                Links.Add(Link);
            }

            return Links.ToArray();
        }

        public string GetFullName() {
            string HTML = Main.GetElementsByClasses(this.HTML, "item-title").First();
            string Name = HTML.Between('>', '<', 1);

            return Name;
        }

        public string GetName(string CodedName) {
            return Main.GetRawNameFromUrlFolder(CodedName);
        }

        public string GetPosterUrl() {
            string Tag = Main.GetElementsByClasses(HTML, "row", "detail-set").First();

            return Main.ExtractHtmlLinks(Tag, "bato.to").First();
        }

        public void Initialize(string URL, out string Name, out string Page) {
            if (!IsValidLink(URL))
                throw new Exception();

            Name = "Manga";
            Page = URL;
        }

        public bool IsValidLink(string URL) {
            //https://bato.to/series/69445

            return URL.ToLower().Contains("bato.to/series/") && URL.StartsWith("http");
        }

        string HTML = null;
        public void LoadPage(string URL) {
            HTML = Main.Download(URL, Encoding.UTF8);
        }

        public bool ValidateProxy(string Proxy) {
            throw new NotImplementedException();
        }
    }
}
