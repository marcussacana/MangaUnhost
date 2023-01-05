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
using System.Text.RegularExpressions;

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
            var Keys = GetChapterKeys(Link);

            ChapterUrl = Link.AbsoluteUri;

            //https://cdn.statically.io/img/images2.optimages.net/f=auto/firefox/HpSSrwPuYY_LvnfTXhKGFg/m6867758/11735/283339/296802/00.jpg
            var Legacy = GetPages(ID, Keys.Identifer, Keys.Token, Link.AbsoluteUri);
            var Avif = GetPages(ID, Keys.Identifer, Keys.Token, Link.AbsoluteUri, false);
            for (int i = 0; i < Legacy.Length; i++) {
                string Page = Legacy[i];
                if (Page.Contains("/f=auto/"))
                {
                    byte[] CurrPage = null;
                    try
                    {
                        CurrPage = new Uri(Page.Replace("cdn.statically.io/img/", "").Replace("/f=auto", "")).Download();
                    }
                    catch {
                        CurrPage = new Uri(Page).TryDownload();

                        if (CurrPage == null)
                            CurrPage = new Uri(Avif[i]).TryDownload();
                    }

                    if (CurrPage != null)
                    {
                        yield return CurrPage;
                        continue;
                    }
                }
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
            var Keys = GetChapterKeys(Link);
            return GetPages(ID, Keys.Identifer, Keys.Token, Link.AbsoluteUri).Length;
        }

        public (string Token, string Identifer) GetChapterKeys(Uri Chapter)
        {
            var HTML = TryDownString(Chapter);
            if (HTML.Contains("&token"))
            {
                string Identifer = HTML.Substring("&token=", "&");
                string Token = HTML.Substring("isVertical, \"", "\"");
                return (Token, Identifer);
            }
            else if (HTML.Contains("isVertical, \""))
            {
                var Identifer = HTML.Substring("READER_TOKEN", ";").Trim(' ', '\n', '\r', '\t', '=', '\'', '"');
                string Token = HTML.Substring("isVertical, \"", "\"");
                return (Token, Identifer);
            }
            else
            {
                var Identifer = HTML.Substring("READER_TOKEN", ";").Trim(' ', '\n', '\r', '\t', '=', '\'', '"');
                return (null, Identifer);
            }
        }

        private Dictionary<int, string> GetChapters(int Page, int Serie) {
            var ApiUrl = new Uri($"https://mangalivre.net/series/chapters_list.json?page={Page}&id_serie={Serie}");

            var Data = ApiUrl.Download(MangaUrl, UserAgent, Accept: "application/json, text/javascript, */*; q=0.01", Headers: new[] { 
                    ("Host", ApiUrl.Host),
                    ("X-Requested-With", "XMLHttpRequest" )
                }, Cookie: Cookies);

            var JSON = Encoding.UTF8.GetString(Data);
            Dictionary<int, string> Chapters = new Dictionary<int, string>();
            while (true) {
                var IDStr = DataTools.ReadJson(JSON, "id_release");
                if (IDStr == null)
                    break;
                var ID = int.Parse(IDStr);
                var Name = DataTools.ReadJson(JSON, "number");
                var Link = DataTools.ReadJson(JSON, "link");
                while (Link.Contains("/scanlator/")) {
                    JSON = JSON.Substring("\"link\":", IgnoreMissmatch: true);
                    Link = DataTools.ReadJson(JSON, "link");
                }
                Chapters[ID] = Name;
                LinkDB[ID] = Link.EnsureAbsoluteUri("https://mangalivre.net/");
                if (!JSON.Contains("\"link\":"))
                    break;
                JSON = JSON.Substring("\"officialSerieLink\":", IgnoreMissmatch: true);
            }
            return Chapters;
        }

        Dictionary<int, string[]> PagesCache = new Dictionary<int, string[]>();
        private string[] GetPages(int RelID, string Identifier, string Token, string ChapterLink, bool Legacy = true)
        {
            if (PagesCache.ContainsKey(RelID) && Legacy)
                return PagesCache[RelID];

            byte[] Data;
            if (Token == null)
            {
                var ApiUrl = new Uri($"https://mangalivre.net/leitor/pages/{RelID}.json?key={Identifier}");
                Data = TryDownload(ApiUrl, ChapterLink) ?? throw new Exception();
            }
            else
            {
                try
                {
                    var ApiUrl = new Uri($"https://mangalivre.net/leitor/pages/{RelID}.json?key={GenTokenA(Identifier, Token, RelID)}");
                    Data = TryDownload(ApiUrl, ChapterLink) ?? throw new Exception();
                }
                catch
                {
                    var ApiUrl = new Uri($"https://mangalivre.net/leitor/pages/{RelID}.json?key={GenTokenB(Identifier, Token, RelID)}");
                    Data = TryDownload(ApiUrl, ChapterLink) ?? throw new Exception();
                }
            }

            var JSON = Encoding.UTF8.GetString(Data);
            ChapterPages Pages = Extensions.JsonDecode<ChapterPages>(JSON);
            return PagesCache[RelID] = Pages.images.Select(x => Legacy ? x.legacy : x.avif).ToArray();
        }

        struct ChapterPages {
            public ImageEntry[] images;
        }

        struct ImageEntry
        {
            public string legacy;
            public string avif;
        }

        public IDecoder GetDecoder()
        {
            return new AvifDecoder();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo() {
                Name = "MangaLivre",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(2, 8),
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
                Title = Document.SelectSingleNode("//div[@id='series-data']/div/span[@class='series-title']/h1").InnerText,
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

        //look for match(/.{1,5}/gi)
        static string GenTokenA(string Identifier, string Token, int ID)
        {
            var RollIndex = Math.Max(ID % 7, 1);
            var TokenParts = SplitByLen(Token, 5);
            var TokenChars = string.Join("", TokenParts.Skip(RollIndex).Concat(TokenParts.Take(RollIndex))).ToArray();

            var ReverseIdentifier = Identifier.Reverse().ToArray();

            //t = TokenChars, s = ReverseIdentifier, n = Identifier

            const string CharList = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random rnd = new Random();

            var RealToken = string.Empty;
            for (int i = 0; i < Identifier.Length; i++)
            {
                RealToken += CharList[rnd.Next(0, CharList.Length)];
                RealToken += Identifier[i];
                RealToken += ReverseIdentifier[i];
                RealToken += TokenChars[i];
                RealToken += CharList[rnd.Next(0, CharList.Length)];
            }

            var RTokenParts = SplitByLen(RealToken, ID % 11);

            string FinalToken = string.Empty;
            for (int i = 0; i < RTokenParts.Length; i++)
            {
                FinalToken += string.Join("", (new char[RollIndex]).Select(x => CharList[rnd.Next(0, CharList.Length)])); // Just Random shit
                FinalToken += RTokenParts[i];
            }

            return FinalToken;
        }

        static string GenTokenB(string Identifier, string Token, int ID)
        {
            var RollIndex = Math.Max(ID % 7, 1);
            var TokenParts = SplitByLen(Token, 5);
            var TokenChars = string.Join("", TokenParts.Skip(RollIndex).Concat(TokenParts.Take(RollIndex))).ToArray();

            var ReverseIdentifier = Identifier.Reverse().ToArray();

            //t = TokenChars, s = ReverseIdentifier, n = Identifier

            //t = TokenChars, e = ReverseIdentifier, n = identifier 

            var RealToken = string.Empty;
            for (int i = 0; i < Identifier.Length; i++)
            {
                RealToken += Identifier[i];
                RealToken += ReverseIdentifier[i];
                RealToken += TokenChars[i];
            }

            return RealToken;
        }

        public static string[] SplitByLen(string Str, int Len)
        {
            var Regex = new Regex(".{1," + Math.Max(Len, 1) + "}");
            var Matches = Regex.Matches(Str);
            List<string> Parts = new List<string>();
            foreach (var Match in Matches.Cast<Match>())
                Parts.Add(Match.Value);
            return Parts.ToArray();
        }

        static Dictionary<int, Uri> LinkDB = new Dictionary<int, Uri>();
        static int ID => int.Parse(MangaUrl.Split('/').Last());
        static string MangaUrl = null;
        static string ChapterUrl = null;
        static CookieContainer Cookies = null;
        static string UserAgent;
        static byte[] TryDownload(Uri Url, string Referrer = null) {
            return Url.TryDownload(Referrer ?? ChapterUrl ?? MangaUrl, UserAgent, Cookie: Cookies, Accept: Accept);
        }

        const string Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
        static string TryDownString(Uri Url) => Encoding.UTF8.GetString(TryDownload(Url) ?? new byte[0]);
    }
}
