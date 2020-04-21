using CefSharp.OffScreen;
using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MangaUnhost.Hosts
{
    class MangaLivre : IHost
    {
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            var Link = LinkDB[ID];
            var HTML = TryDownString(Link);
            string Key = HTML.Substring("&token=", "&");

            foreach (var Page in GetPages(ID, Key, Link.AbsoluteUri)) {
                yield return TryDownload(new Uri(Page));
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            int CurrentPage = 1;
            while (true) {
                var Chapters = GetChapters(CurrentPage++, ID);
                if (Chapters.Count == 0)
                    break;
                foreach (var Pair in Chapters)
                    yield return Pair;
            }
        }

        public int GetChapterPageCount(int ID)
        {
            var Link = LinkDB[ID];
            var HTML = TryDownString(Link);
            string Key = HTML.Substring("&token=", "&");

            return GetPages(ID, Key, Link.AbsoluteUri).Length;
        }

        private Dictionary<int, string> GetChapters(int Page, int Serie) {
            var ApiUrl = new Uri($"https://mangalivre.net/series/chapters_list.json?page={Page}&id_serie={Serie}");
            var Request = (HttpWebRequest)WebRequest.Create(ApiUrl);
            Request.Method = "GET";
            Request.Accept = "application/json, text/javascript, */*; q=0.01";
            Request.Referer = MangaUrl;
            Request.UserAgent = UserAgent;
            Request.CookieContainer = Cookies;
            Request.Headers["X-Requested-With"] = "XMLHttpRequest";

            var JSON = Encoding.UTF8.GetString(Request.GetResponseData());
            Dictionary<int, string> Chapters = new Dictionary<int, string>();
            while (true) {
                var IDStr = DataTools.ReadJson(JSON, "id_release");
                if (IDStr == null)
                    break;
                var ID = int.Parse(IDStr);
                var Name = DataTools.ReadJson(JSON, "number");
                var Link = DataTools.ReadJson(JSON, "link");
                if (Link.Contains("/scanlator/")) {
                    JSON = JSON.Substring("\"link\":", IgnoreMissmatch: true);
                    Link = DataTools.ReadJson(JSON, "link");
                }
                Chapters[ID] = Name;
                LinkDB[ID] = new Uri(new Uri("https://mangalivre.net/"), Link);
                if (!JSON.Contains("\"link\":"))
                    break;
                JSON = JSON.Substring("\"link\":", IgnoreMissmatch: true);
            }
            return Chapters;
        }

        Dictionary<int, string[]> PagesCache = new Dictionary<int, string[]>();
        private string[] GetPages(int RelID, string Key, string ChapterLink)
        {
            if (PagesCache.ContainsKey(RelID))
                return PagesCache[RelID];

            var ApiUrl = new Uri($"https://mangalivre.net/leitor/pages/{RelID}.json?key={Key}");
            var Request = (HttpWebRequest)WebRequest.Create(ApiUrl);
            Request.Method = "GET";
            Request.Accept = "application/json, text/javascript, */*; q=0.01";
            Request.Referer = ChapterLink;
            Request.UserAgent = UserAgent;
            Request.CookieContainer = Cookies;
            Request.Headers["X-Requested-With"] = "XMLHttpRequest";

            var JSON = Encoding.UTF8.GetString(Request.GetResponseData());
            ChapterPages Pages = Extensions.JsonDecode<ChapterPages>(JSON);
            PagesCache[RelID] = Pages.images;
            return Pages.images;
        }

        struct ChapterPages {
            public string[] images;
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo() {
                Name = "MangaLivre",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0),
                Icon = Resources.Icons.MangaLivre
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            throw new NotImplementedException();
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.ToLower().Contains("mangalivre") && Uri.AbsolutePath.Contains("manga/");
        }

        public ComicInfo LoadUri(Uri Uri)
        {
            if (Uri.AbsolutePath.Contains("/cap"))
                Uri = new Uri(Uri.AbsoluteUri.Substring(null, "/cap"));

            InitializeCookies(Uri);

            string HTML = TryDownString(Uri);

            HtmlDocument Document = new HtmlDocument();
            Document.LoadHtml(HTML);

            MangaUrl = Uri.AbsoluteUri;

            return new ComicInfo() {
                Title = Document.SelectSingleNode("//span[@class=\"series-title\"]/h1").InnerText,
                Cover = TryDownload(new Uri(Document.SelectSingleNode("//div[@class=\"cover\"]/img").GetAttributeValue("src", null))),
                ContentType = ContentType.Comic,
                Url = Uri
            };
        }

        private void InitializeCookies(Uri Manga) {
            if (Cookies != null)
                return;

            ChromiumWebBrowser Browser = new ChromiumWebBrowser();
            Browser.WaitInitialize();
            Browser.Load(Manga.AbsoluteUri);
            Browser.WaitForLoad();
            /*
            if (!Browser.IsCaptchaSolved()) {
                Browser.TrySolveCaptcha();
            }*/

            UserAgent = Browser.GetUserAgent();
            Cookies = Browser.GetCookies().ToContainer();
            Browser.Dispose();
        }

        static Dictionary<int, Uri> LinkDB = new Dictionary<int, Uri>();
        static int ID => int.Parse(MangaUrl.Split('/').Last());
        static string MangaUrl = null;
        static string ChapterUrl = null;
        static CookieContainer Cookies = null;
        static string UserAgent;
        static byte[] TryDownload(Uri Url) {
            return Url.TryDownload(ChapterUrl, UserAgent, Cookie: Cookies);
        }
        static string TryDownString(Uri Url) => Encoding.UTF8.GetString(TryDownload(Url) ?? new byte[0]);
    }
}
