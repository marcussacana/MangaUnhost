using Emgu.CV;
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
    internal class BlackoutComics : IHost
    {
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var Page in GetChapterPages(ID))
            {
                yield return Page.TryDownload(CFData);
            }
        }

        private IEnumerable<string> GetChapterPages(int ID)
        {
            var ChapDoc = new HtmlDocument();
            ChapDoc.LoadUrl(ChapterMap[ID], CFData, Referer: CurrentUrl.AbsoluteUri, UserAgent: ProxyTools.UserAgent, Headers: RequiredHeaders);

            foreach (var Node in ChapDoc.SelectNodes("//div[contains(@class, 'img-capitulos')]//canvas[@class='blade']"))
            {
                var URL = Node.GetAttributeValue("data-src", null);
                yield return new Uri(CurrentUrl, URL).AbsoluteUri;
            }
        }

        Dictionary<int, string> ChapterMap = new Dictionary<int, string>();
        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            foreach (var ChapterNode in Document.SelectNodes("//div[@id='capitulosList']/h5"))
            {
                var ChapterElement = ChapterNode.SelectNodes("./a[contains(@href, '/comics/')]").Last();
                var ChapterUrl = ChapterElement.GetAttributeValue("href", null);
                var ChapterName = ChapterElement.InnerText.Trim().Replace("Capítulo", "").Trim();

                int ID = ChapterMap.Count;

                ChapterMap[ID] = ChapterUrl;

                yield return new KeyValuePair<int, string>(ID, ChapterName);
            }
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID).Count();
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "BlackoutComics",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(2, 0, 0)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            throw new NotImplementedException();
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.Contains("blackoutcomics.com") && Uri.AbsolutePath.StartsWith("/comics/");
        }

        Uri CurrentUrl;
        HtmlDocument Document;
        CloudflareData? CFData;

        (string, string)[] RequiredHeaders = new[] { 
            ("Accept-Language", "en-US,en;q=0.9")
        };
        public ComicInfo LoadUri(Uri Uri)
        {
            CurrentUrl = Uri;

            Login();
         
            Document = new HtmlDocument();
            CFData = Document.LoadUrl(Uri, UserAgent: ProxyTools.UserAgent, Headers: RequiredHeaders, Cookies: AuthCookies);


            if (CFData != null)
            {
                foreach (var Cookie in AuthCookies.GetCookies())
                {
                    CFData.Value.Cookies.Add(Uri, Cookie);
                }
            } 
            else
            {
                CFData = new CloudflareData()
                {
                    UserAgent = ProxyTools.UserAgent,
                    Cookies = AuthCookies,
                    HTML = Document.ToHTML(),
                };
            }

            var CoverURL = Document.SelectSingleNode("//img[@class='img-recen-add2']").GetAttributeValue("src", null);

            if (CoverURL.StartsWith("/"))
            {
                CoverURL = new Uri(Uri, CoverURL).AbsoluteUri;
            }

            return new ComicInfo()
            {
                Title = Document.SelectSingleNode("//div[@class='trailer-content']/h2[count(@*)=0]").InnerText,
                Cover = CoverURL.TryDownload(CFData),
                Url = Uri,
                ContentType = ContentType.Comic
            };
        }

        CookieContainer AuthCookies = null;
        private void Login()
        {
            if (AuthCookies != null)
                return;

            AuthCookies = new CookieContainer();

            var Homepage = new HtmlDocument();
            Homepage.LoadUrl("https://blackoutcomics.com/", UserAgent: ProxyTools.UserAgent, Headers: RequiredHeaders, Cookies: AuthCookies);

            var TokenNode = Homepage.SelectSingleNode("//form[contains(@action, 'login')]/input[@name='_token']");

            var TokenValue = TokenNode.GetAttributeValue("value", "");

            //Yeah, public just like that, you can fuck it if you feel like
            //but this account has been made just for this then you will
            //just lost your time as well.
            const string FakeLogin = "junmaedashit@gmail.com";
            const string FakePass = "5Tivk!QiuL_5hEb";

            var FormData = Encoding.UTF8.GetBytes($"_token={HttpUtility.UrlEncode(TokenValue)}&email={HttpUtility.UrlEncode(FakeLogin)}&password={HttpUtility.UrlEncode(FakePass)}");

            var Headers = RequiredHeaders.Concat(new[] { ("Content-Type", "application/x-www-form-urlencoded") }).ToArray();

            UrlTools.Upload("https://blackoutcomics.com/blackout/login", FormData, "https://blackoutcomics.com/", ProxyTools.UserAgent, Headers: Headers, Cookie: AuthCookies);
        }
    }
}
