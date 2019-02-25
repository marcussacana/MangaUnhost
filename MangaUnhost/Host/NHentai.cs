using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace MangaUnhost.Host {
    class NHentai : IHost {
        string HTML;
        public bool NeedsProxy {
            get {
                return true;
            }
        }
        public string HostName {
            get {
                return "nhentai";
            }
        }
        public string DemoUrl {
            get {
                return "http://nhentai.net/g/190997/";
            }
        }
        public CookieContainer Cookies {
            get {
                return null;
            }
        }
        public string UserAgent { get { return null; } }
        public string GetChapterName(string ChapterURL) {
            return "One Shot";
        }

        public string[] GetChapterPages(string HTML) {
            HTML = this.HTML;
            HTML = HTML.Substring(HTML.IndexOf("<div class=\"thumb-container\">"));
            string[] Elements = Main.GetElementsByClasses(HTML, "gallerythumb");

            List<string> Links = new List<string>();
            foreach (string Element in Elements) {
                string Page = (from x in Main.ExtractHtmlLinks(Element, "nhentai.net") where IsValidLink(x) select x).First();
                string pHTML = Main.Download(Page.Replace("http:", "https:"), Encoding.UTF8, AllowRedirect: false);
                
                Page = Main.GetElementsByClasses(pHTML, "fit-horizontal").First();
                Links.Add(Main.ExtractHtmlLinks(Page, "nhentai.net").First());
            }

            return Links.ToArray();
        }

        public string[] GetChapters() {
            return new string[] { "http://nhentai.net" };
        }

        public string GetFullName() {
            string Element = Main.GetElementsByContent(HTML, "&raquo;").First();
            int Index = Element.IndexOf("&raquo;");

            string Name = HttpUtility.HtmlDecode(Element.Substring(0, Index).Split('>')[1].TrimEnd());

            return Main.GetRawNameFromUrlFolder(Name, true);
        }

        public string GetName(string CodedName) {
            return "Unk";
        }

        public string GetPosterUrl() {
            int Index = HTML.IndexOf("<div id=\"cover\">");

            string Link = Main.ExtractHtmlLinks(HTML.Substring(Index), "nhentai.net").First();

            return Link;
        }

        public void Initialize(string URL, out string Name, out string Page) {
            if (!IsValidLink(URL))
                throw new Exception();


            string ID = URL.Substring(URL.IndexOf("/g/") + 3).Split('/')[0];

            Page = $"https://nhentai.net/g/{ID}/";
            Name = "Unk";
        }

        public bool IsValidLink(string URL) {
            //https://nhentai.net/g/190997/
            URL = URL.ToLower();
            return Uri.IsWellFormedUriString(URL, UriKind.Absolute) && URL.Contains("nhentai") && URL.Contains("/g/");
        }

        public void LoadPage(string URL) {
            HTML = Main.Download(URL, Encoding.UTF8);
        }

        Dictionary<string, bool> ProxyCache = new Dictionary<string, bool>();
        public bool ValidateProxy(string Proxy) {
            if (ProxyCache.ContainsKey(Proxy))
                return ProxyCache[Proxy];

            try {
                HttpWebRequest Request = WebRequest.Create(GetChapters()[0]) as HttpWebRequest;
                Request.Proxy = new WebProxy(Proxy);
                Request.Timeout = 10 * 1000;
                Request.Method = "GET";
                var Resp = Request.GetResponse() as HttpWebResponse;
                if (Resp.StatusCode != HttpStatusCode.OK || !Resp.ResponseUri.Host.ToLower().Contains("nhentai.net"))
                    return ProxyCache[Proxy] = false;
                return ProxyCache[Proxy] = true;
            } catch {
                return ProxyCache[Proxy] = false;
            }
        }
    }
}
