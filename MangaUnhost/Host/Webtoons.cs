using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace MangaUnhost.Host
{
    class Webtoons : IHost
    {
        public string HostName => "Web Toons";

        public string DemoUrl => "https://www.webtoons.com/zh-hant/drama/gaizaojihua/list?title_no=1522&page=3";

        public bool NeedsProxy => false;

        public CookieContainer Cookies => null;

        public string UserAgent => null;

        public string Referrer => "https://www.webtoons.com";

        public bool SelfChapterDownload => false;

        public string GetChapterName(string ChapterURL)
        {
            string Name = ChapterURL.Substring(ChapterURL.IndexOf("episode_no="));
            Name = Name.Between('=', '&');

            return Name;
        }

        public string[] GetChapterPages(string HTML)
        {
            HTML = HTML.Substring(HTML.IndexOf("_imageList"));
            HTML = HTML.Substring(0, HTML.IndexOf("</div>"));

            string[] Elements = Main.GetElements(HTML, HTML.IndexOf(">") + 1, true);

            List<string> Pages = new List<string>();
            foreach (string Element in Elements)
            {
                string Link = Main.GetElementAttribute(Element, "data-url");

                Pages.Add(Link);
            }

            return Pages.ToArray();
        }

        public string[] GetChapters()
        {
            List<string> Chapters = new List<string>();
             //<div class="detail_lst">
            foreach (string FHTML in ListPages)
            {
                string HTML = FHTML.Substring(FHTML.IndexOf("<div class=\"detail_lst\">"));
                HTML = HTML.Substring(0, HTML.IndexOf("<div class=\"paginate\">"));

                string[] CChapters = Main.ExtractHtmlLinks(HTML, "www.webtoons.com");

                Chapters.AddRange((from x in CChapters where x.ToLower().Contains("/viewer?") select x));
            }

            return Chapters.Distinct().ToArray();
        }

        public string GetFullName()
        {
            string HTML = ListPages.First();

            HTML = HTML.Substring(HTML.IndexOf("<h1 class=\"subj\">"));
            HTML = HTML.Substring(0, HTML.IndexOf("/h1>"));

            return HttpUtility.HtmlDecode(HTML.Between('>', '<'));
        }

        public string GetName(string CodedName)
        {
            return CodedName;
        }

        public string GetPosterUrl()
        {
            string HTML = ListPages.First();

            HTML = HTML.Substring(HTML.IndexOf("detail_body"));
            HTML = HTML.Substring(0, HTML.IndexOf(">"));

            return HTML.Between('(', ')');
        }

        public void Initialize(string URL, out string Name, out string Page)
        {
            if (!IsValidLink(URL))
                throw new Exception();

            if (URL.ToLower().Contains("page="))
            {
                URL = URL.Replace("page=", "skip=");//nothing, just force show the default page
            }

            Name = "Comic";

            Page = URL;
        }

        public bool IsValidLink(string URL)
        {
            URL = URL.ToLower();
            return URL.Contains("http") && URL.Contains("www.webtoons.com") && URL.Contains("/list?title_no=");
        }

        string[] ListPages;
        public void LoadPage(string URL)
        {

            List<string> Urls = new List<string>();
            List<string> Pages = new List<string>();
            Urls.Add(URL);

            for (int i = 0; i < Urls.Count; i++)
            {
                string HTML = Main.Download(Urls[i], Encoding.UTF8);
                string[] Links = ExtractNextPages(HTML);
                foreach (string Link in Links)
                    if (!Urls.Contains(Link) && !Link.EndsWith("/#") && !Link.Contains("page=1"))
                        Urls.Add(Link);

                Pages.Add(HTML);
            }
            /*
            string tmp = Pages.First();
            Pages.RemoveAt(0);
            Pages.Add(tmp);
            */

            //Pages.RemoveAt(Pages.Count - 1);

            ListPages = Pages.ToArray();
        }

        private string[] ExtractNextPages(string HTML)
        {
            string Part = HTML.Substring(HTML.IndexOf("<div class=\"paginate\">"));
            Part = Part.Substring(0, Part.IndexOf("</div>"));

            return Main.ExtractHtmlLinks(Part, "https://www.webtoons.com");
        }

        public bool ValidateProxy(string Proxy)
        {
            throw new NotImplementedException();
        }
    }
}
