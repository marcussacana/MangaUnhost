using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace MangaUnhost.Host {
    class MangaHasu : IHost {
        public string HostName => "MangaHasu";

        public string DemoUrl => (string)new Extensions.MethodInvoker(() => {
            string Home = Main.Download(Referrer, Encoding.UTF8, UserAgent: UA, Referrer: Referrer, Cookies: Cookies);
            Home = Home.Substring("<div class=\"wrapper_imgage\">", "</div>");
            string[] Links = Main.ExtractHtmlLinks(Home, "mangahasu.se");
            return Links[0];
        }).Invoke();

        public bool NeedsProxy => false;

        public CookieContainer Cookies => Cookie?.ToContainer();

        public string UserAgent => UA;

        public string Referrer => "http://mangahasu.se/";

        public bool SelfChapterDownload => true;

        public string GetChapterName(string ChapterURL) {
            if (ChapterMap.ContainsKey(ChapterURL.ToLower())) {
                try {
                    string Name = ChapterMap[ChapterURL.ToLower()];
                    Name = Name.Substring(Name.IndexOf("</span>"));

                    try {
                        Name = Name.Substring(Name.ToLower().IndexOf("chapter ")).Between(' ', ' ');
                        Name = Name.Split('<')[0].Split(':')[0];

                        return Name;
                    } catch {
                        Name = Name.Between('>', '<').Trim().Between(' ', ':');

                        return Name;
                    }
                } catch { }
            }

            return ChapterURL.Substring(ChapterURL.ToLower().IndexOf("/chapter-")).Split('-').Last();
        }

        public string[] GetChapterPages(string Link) {
            string HTML = null;
            while (true)
            {
                HTML = Main.Download(Link, Encoding.UTF8, UserAgent: UA, Cookies: Cookies, Referrer: Referrer);
                if (HTML.IsCloudflareTriggered())
                {
                    var Bypass = Main.BypassCloudflare(Link);
                    UA = Bypass.UserAgent;
                    Cookie = Bypass.AllCookies;
                    HTML = Bypass.HTML;
                }
                break;
            }

            string[] Elements = Main.GetElementsByAttribute(HTML, "class", "page", true);

            string PageList = string.Join("", Elements);

            return Main.ExtractHtmlLinks(PageList, "mangahasu.se").Distinct().ToArray();
        }

        static Cookie[] Cookie = null;
        static string UA = null;

        Dictionary<string, string> ChapterMap = new Dictionary<string, string>();
        public string[] GetChapters() {
            string HTML = this.HTML.Substring(this.HTML.IndexOf("list-chapter"));

            HTML = HTML.Substring(0, HTML.IndexOf("</div>"));

            string[] Links = Main.ExtractHtmlLinks(HTML, "mangahasu.se");
            string[] Names = Main.GetElementsByClasses(HTML, true, Class: "name");

            ChapterMap = new Dictionary<string, string>();
            for (int i = 0; i < Links.Length; i++)
                ChapterMap[Links[i].ToLower()] = Names[i];


            if (Cookie == null) {
                var Data = Main.BypassCloudflare(Links.First());
                UA = Data.UserAgent;
                Cookie = Data.AllCookies;
            }

            return Links;
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
