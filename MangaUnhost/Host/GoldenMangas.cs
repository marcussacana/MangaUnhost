using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace MangaUnhost.Host
{
    class GoldenMangas : IHost
    {
        public string HostName => "Golden Mangas";

        public string DemoUrl => "https://goldenmangas.online/manga/kage-no-jitsuryokusha-ni-naritakute-br";

        public bool NeedsProxy => false;

        public CookieContainer Cookies => null;

        public string UserAgent => null;

        public string Referrer => null;

        public bool SelfChapterDownload => false;

        public string GetChapterName(string ChapterURL)
        {
            ChapterURL = ChapterURL.TrimEnd('/');
            return ChapterURL.Split('/').Last();
        }

        public string[] GetChapterPages(string HTML)
        {
            HTML = HTML.Substring("capitulos_images", "</center>");
            string[] Pages = Main.ExtractHtmlLinks(HTML, "goldenmangas.online");
            return Pages;
        }

        public string[] GetChapters()
        {
            string HTML = this.HTML.Substring("titulo-leitura cg_color", "<div class=\"clear");
            HTML = HTML.Substring("</h3>");

            string[] Elms = Main.GetElementsByContent(HTML, "<a href=\"/manga");
            List<string> Chapters = new List<string>();
            for (int i = 0; i < Elms.Length; i++)
                Chapters.Add(Main.ExtractHtmlLinks(Elms[i], "goldenmangas.online").First());

            return Chapters.ToArray();
        }

        public string GetFullName()
        {
            string HTML = this.HTML.Substring("<header class=\"breadcrumbs\">", "</h1>");
            return HTML.Substring("<h1>");
        }

        public string GetName(string CodedName)
        {
            return Main.GetRawNameFromUrlFolder(CodedName);
        }

        public string GetPosterUrl()
        {
            string Data = HTML.Substring("col-sm-4 text-right");
            Data = Main.ExtractHtmlLinks(Data, "goldenmangas.online", "src").First();
            Data = Data.Replace("/timthumb.php?src=", "");
            Data = Data.Split('?')[0].Split('&')[0];
            return Data;
        }

        public void Initialize(string URL, out string Name, out string Page)
        {
            if (!IsValidLink(URL))
                throw new Exception();

            //https://goldenmangas.online/manga/kage-no-jitsuryokusha-ni-naritakute-br

            Name = GetName(URL.Substring("/manga").TrimEnd('/').Split('/')[1]);
            Page = URL;
        }

        public bool IsValidLink(string URL)
        {
            URL = URL.ToLower();
            return Uri.IsWellFormedUriString(URL, UriKind.Absolute) && (URL.Contains("goldenmangas.online") || URL.Contains("montgomerymarkland.co")) && URL.Contains("/manga");
        }

        public void LoadPage(string URL)
        {
            HTML = Main.Download(URL, Encoding.UTF8);
        }

        string HTML;
        public bool ValidateProxy(string Proxy)
        {
            throw new NotImplementedException();
        }
    }
}
