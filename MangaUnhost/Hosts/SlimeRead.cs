using CefSharp.OffScreen;
using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MangaUnhost.Hosts
{
    internal class SlimeRead : IHost
    {
        Dictionary<int, string> ChapterMap = new Dictionary<int, string>();
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        bool AltDomain = false;

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var Page in GetChapterPages(ID))
            {
                var ModDomain = Page.Replace("/objects.", "/black.");
                var OriDomain = Page;
                var ImgUrl = AltDomain ? ModDomain : Page;
                var Data = ImgUrl.TryDownload(Referer: "https://slimeread.com", UserAgent: ProxyTools.UserAgent);

                if (Data == null)
                {
                    AltDomain = !AltDomain;
                    ImgUrl = AltDomain ? ModDomain : Page;
                    Data = ImgUrl.TryDownload(Referer: "https://slimeread.com", UserAgent: ProxyTools.UserAgent);
                    if (Data == null)
                        throw new Exception();
                }

                yield return Data;
            }
        }


        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            var Info = GetComicInfo();
            var BookChaps = ((BookInfoInnerContent)Info.props.pageProps.book_info.book_infos
                .First(x => ((BookInfoInnerContent)x.book_info_content).type == "chapters")
                .book_info_content).chapters;
            return BookChaps
                .OrderByFilenameNumber(x => x.volume == null ? x.chapter : x.volume + "." + x.chapter)
                .Reverse()
                .Select(x => {
                    var ID = ChapterMap.Count;

                    ChapterMap[ID] = (int.Parse(x.chapter) - 1).ToString();

                    string Name = x.chapter;

                    return new KeyValuePair<int, string>(ID, $"{Name}");
                });
        }

        public RootInfo GetComicInfo()
        {

            var BuildInfo = Document.SelectSingleNode("//script[@type='application/json']").InnerText;
            var buildId = DataTools.ReadJson(BuildInfo, "buildId");

            var URL = $"https://slimeread.com/_next/data/{buildId}{CurrentUrl.AbsolutePath}.json";
            var ComicData = URL.TryDownloadString(Referer: "https://slimeread.com", UserAgent: ProxyTools.UserAgent).Trim(' ', '\t', '[', ']');


            var BookId = CurrentUrl.PathAndQuery.Split('/')[2];

            RootInfo Info;

            if (ComicData == null)
                Info = new RootInfo()
                {
                    props = JsonConvert.DeserializeObject<Props>(ComicData)
                };
            else
                Info = JsonConvert.DeserializeObject<RootInfo>(BuildInfo);

            string InfoData = null;

            List<BookInfoInner> BookInfos = Info.props.pageProps.book_info.book_infos;

            for (var i = 0; i < BookInfos.Count; i++)
            {
                var Entry = BookInfos[i];
                if (Entry.book_info_content is string json)
                {
                    Entry.book_info_content = JsonConvert.DeserializeObject<BookInfoInnerContent>(json);
                }
                BookInfos[i] = Entry;
            }

            try
            {
                URL = $"{GetCurrentDomain(false)}book_cap_units_all?manga_id={BookId}";
                InfoData = URL.TryDownloadString(Referer: "https://slimeread.com", UserAgent: ProxyTools.UserAgent).Trim(' ', '\t', '[', ']');

                if (!InfoData.TrimStart().StartsWith("["))
                    InfoData = $"[{InfoData.Trim()}]";

                Info = GetInfo(InfoData, Info);

                BookInfos = Info.props.pageProps.book_info.book_infos;
            }
            catch { }

            if (BookInfos == null || !BookInfos.Any(x => ((BookInfoInnerContent)x.book_info_content).type == "chapters"))
            {
                URL = $"{GetCurrentDomain(true)}book_cap_units_all?manga_id={BookId}";
                InfoData = URL.TryDownloadString(Referer: "https://slimeread.com", UserAgent: ProxyTools.UserAgent).Trim(' ', '\t', '[', ']');

                if (!InfoData.TrimStart().StartsWith("["))
                    InfoData = $"[{InfoData.Trim()}]";

                Info = GetInfo(InfoData, Info);
            }

            return Info;
        }

        private static RootInfo GetInfo(string InfoData, RootInfo Info)
        {
            var ChapInfo = JsonConvert.DeserializeObject<BtcCap[]>(InfoData);

            var infos = new[] {
                        new BookInfoInner()
                        {
                            book_info_book_id = 0,
                            book_info_content = new BookInfoInnerContent()
                            {
                                type = "chapters",
                                chapters = ChapInfo.Select(x =>
                                {
                                    return new BookInfoInnerChapter()
                                    {
                                        chapter = x.btc_cap.ToString(),
                                        id = x.btc_id.ToString()
                                    };
                                }).ToList()
                            }
                        }
                    }.ToList();

            var binfo = Info.props.pageProps.book_info;
            binfo.book_infos = infos;

            var pProps = Info.props.pageProps;
            pProps.book_info = binfo;

            var Props = Info.props;
            Props.pageProps = pProps;

            Info.props = Props;
            return Info;
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID).Length;
        }

         

        public string[] GetChapterPages(int ID)
        {
            string JSON = GetChapterInfoA(ID) ?? GetChapterInfoB(ID);

            if (JSON == null)
                throw new Exception();

            var ChInfo = JsonConvert.DeserializeObject<ChapterInfo>(JSON);

            if (ChInfo.book_temp != null)
            {
                var Images = ChInfo.book_temp.SelectMany(x => x.book_temp_caps)
                    .SelectMany(x => x.book_temp_cap_unit)
                    .Select(x => $"https://objects.slimeread.com/{x.btcu_image}");

                return Images.ToArray();
            }

            var Info = Newtonsoft.Json.JsonConvert.DeserializeObject<BookTempCap>(JSON);

            var Pages = Info.book_temp_cap_unit
                .Where(x => !x.btcu_image.StartsWith("folders/"))//remove ad
                .Select(x => $"https://objects.slimeread.com/{x.btcu_image}");

            return Pages.ToArray();

        }

        private string GetChapterInfoA(int ID)
        {
            var Uri = $"{GetCurrentDomain(false)}book_cap_units?manga_id={MangaID}&cap={ChapterMap[ID]}";// &token={Token}";

            var Rst = Uri.TryDownloadString(Referer: "https://slimeread.com", UserAgent: ProxyTools.UserAgent).Trim(' ', '\t', '[', ']');

            if (string.IsNullOrWhiteSpace(Rst.Trim(' ', '\t', '\r', '\n', '{', '}')))
                return null;

            return Rst;
        }
        private string GetChapterInfoB(int ID)
        {
            var Uri = $"{GetCurrentDomain(false)}book_cap_units?manga_id={MangaID}&cap={(int.Parse(ChapterMap[ID]) + 1)}";// &token={Token}";

            var InfoData = Uri.TryDownloadString(Referer: "https://slimeread.com", UserAgent: ProxyTools.UserAgent).Trim(' ', '\t', '[', ']');

            if (string.IsNullOrWhiteSpace(InfoData.Trim(' ', '\t', '\r', '\n', '{', '}')))
                return null;


            if (!InfoData.TrimStart().StartsWith("["))
                InfoData = $"[{InfoData.Trim()}]";

            var Info = JsonConvert.DeserializeObject<BtcCap[]>(InfoData);

            var CastData = new ChapterInfo()
            {
                book_temp = new List<BookTemp>()
                {
                    new BookTemp()
                    {
                        book_temp_caps = Info.Take(1).ToList()
                    }
                }
            };

            return JsonConvert.SerializeObject(CastData);
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "SmileRead",
                Author = "Marcussacana",
                SupportComic = true,
                Version = new Version(3, 1)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            throw new NotImplementedException();
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.ToLower().Contains("slimeread.com") && Uri.PathAndQuery.Contains("manga/");
        }

        string Token = null;
        public void EnsureLogin()
        {
            Token = Ini.GetConfig("SlimeRead", "Token", Main.SettingsPath, false);

            if (Token != null)
            {
                var LogURI = "https://slimeread.com/api/auth/login?token=" + Token;

                var Response = LogURI.TryDownloadString();

                if (Response.Contains("user_nickname"))
                    return;
            }

            var CEF = new ChromiumWebBrowser("about:blank");

            CEF.WaitInitialize();
            CEF.LoadUrl("https://slimeread.com/login");
            CEF.WaitForLoad();

            var Browser = new BrowserPopup(CEF, () => { return !CEF.GetCurrentUrl().Contains("/login") && !CEF.GetCurrentUrl().Contains("/registrar"); });
            Browser.ShowDialog();

            Token = CEF.GetCookie("nextauth.token");

            Ini.SetConfig("SlimeRead", "Token", Token, Main.SettingsPath);

            EnsureLogin();
        }

        private string GetCurrentDomain(bool IgnoreCache)
        {
            if (!IgnoreCache && Ini.GetConfigStatus("SlimeRead", "ApiChapter", Main.SettingsPath) == Ini.ConfigStatus.Ok)
                return Ini.GetConfig("SlimeRead", "ApiChapter", Main.SettingsPath);

            using (var Browser = new ChromiumWebBrowser(CurrentUrl.AbsoluteUri))
            {
                string Domain = null;
                Browser.WaitInitialize();

                Browser.RegisterWebRequestHandlerEvents((sender, args) => {
                    if (!args.WebRequest.RequestUri.AbsoluteUri.Contains("book_cap_units_all"))
                        return;
                    Domain = args.WebRequest.RequestUri.AbsoluteUri.Substring(null, "book_cap_units_all");
                }, null);

                Browser.WaitForLoad();

                DateTime BeginWait = DateTime.Now;
                while (Domain == null && (DateTime.Now - BeginWait).TotalSeconds < 10)
                {
                    ThreadTools.Wait(1000, true);
                }

                Ini.SetConfig("SlimeRead", "ApiChapter", Domain, Main.SettingsPath);

                return Domain;
            }
        }

        string MangaID;
        HtmlDocument Document = new HtmlDocument();

        string UserAgent = null;
        Uri CurrentUrl = null;
        public ComicInfo LoadUri(Uri Uri)
        {
            Document.LoadUrl(CurrentUrl = Uri);

            //EnsureLogin();
            //
            var Info = GetComicInfo();

            MangaID = Uri.PathAndQuery.TrimStart('/').Split('/')[1];
            var Title = Info.props.pageProps.book_info.book_name_original ?? Document.SelectSingleNode("//div[contains(@class, 'mt-4 sm:ml-4 sm:mt-0')]//p[contains(@class, 'font-bold')]").InnerText;
            var Cover = Info.props.pageProps.book_info.book_image ?? Document.SelectSingleNode("//div[contains(@class, 'mt-4 sm:ml-4 sm:mt-0')]/../img").GetAttributeValueByAlias("src", "data-cfsrc") as string;

            var CoverData = Cover.TryDownload(Uri.AbsoluteUri, ProxyTools.UserAgent);

            return new ComicInfo()
            {
                Title = Title,
                Cover = CoverData,
                ContentType = ContentType.Comic,
                Url = Uri
            };
        }

        public struct Genre
        {
            public string genre_name { get; set; }
        }

        public struct Author
        {
            public string author_name { get; set; }
        }

        public struct Categories
        {
            public string cat_name { get; set; }
            public int cat_id { get; set; }
            public string cat_name_ptBR { get; set; }
        }

        public struct Scan
        {
            public string scan_name { get; set; }
            public long scan_id { get; set; }
        }

        public struct BtcCap
        {
            public float btc_cap { get; set; }
            public DateTime btc_date_updated { get; set; }
            public string btc_name { get; set; }
            public Scan scan { get; set; }
            public List<object> scans_parterners { get; set; }

            public long btc_id { get; set; }
            public long btc_scan_id { get; set; }
            public List<BookTempCapUnit> book_temp_cap_unit { get; set; }
        }

        public struct BookTemp
        {
            public long bt_id { get; set; }
            public int bt_season { get; set; }
            public List<BtcCap> book_temp_caps { get; set; }
        }

        public struct Related
        {
            public int book_id { get; set; }
            public string book_name_original { get; set; }
            public string book_name { get; set; }
            public string book_image { get; set; }
            public int score { get; set; }
        }

        public struct BookInfo
        {
            public string book_name_original { get; set; }
            public string book_name { get; set; }
            public string book_image { get; set; }
            public string book_id { get; set; }
            public string book_name_alternatives { get; set; }
            public int book_status { get; set; }
            public DateTime book_date_updated { get; set; }
            public int book_views { get; set; }
            public string book_synopsis { get; set; }
            public int book_scan_id { get; set; }
            public string book_uuid { get; set; }
            public Genre genre { get; set; }
            public Author author { get; set; }
            public List<Categories> book_categories { get; set; }
            public List<BookTemp> book_temp { get; set; }
            public List<BookInfoInner> book_infos { get; set; }
            public List<Related> related { get; set; }
        }

        public struct BookInfoInner
        {
            public int book_info_id { get; set; }
            public int book_info_book_id { get; set; }
            public DateTime book_info_date_created { get; set; }
            public DateTime book_info_date_updated { get; set; }

            public object book_info_content { get; set; } //BookInfoInnerContent but may be serialized
        }

        public struct BookInfoInnerContent
        {
            public string type { get; set; }

            public List<BookInfoInnerChapter> chapters { get; set; }
        }

        public struct BookInfoInnerChapter
        {
            public string id { get; set; }
            public string name { get; set; }
            public string volume { get; set; }
            public string chapter { get; set; }

            public string scanlator { get; set; }
            public string externalUrl { get; set; }
        }

        public struct PageProps
        {
            public BookInfo book_info { get; set; }
            public int revalidate { get; set; }
        }

        public struct RootInfo
        {
            public Props props { get; set; }
            public string buildId { get; set; }
            public bool isFallback { get; set; }
            public bool gsp { get; set; }
            public List<object> scriptLoader { get; set; }
        }
        public struct Props
        {
            public PageProps pageProps { get; set; }
            public bool __N_SSG { get; set; }
        }

        ///Chapter info
        public struct ChapterInfo
        {
            public string book_name_original { get; set; }
            public string book_name { get; set; }
            public string book_image { get; set; }
            public string book_synopsis { get; set; }
            public int book_status { get; set; }
            public List<BookCategory> book_categories { get; set; }
            public List<BookTemp> book_temp { get; set; }
        }

        public struct BookCategory
        {
            public Category categories { get; set; }
        }

        public struct Category
        {
            public bool cat_nsfw { get; set; }
        }

        public struct BookTempCap
        {
            public long btc_id { get; set; }
            public long btc_scan_id { get; set; }
            public Scan scan { get; set; }
            public List<BookTempCapUnit> book_temp_cap_unit { get; set; }
        }

        public struct BookTempCapUnit
        {
            public object btcu_content { get; set; }
            public string btcu_image { get; set; }
            public object btcu_provider_host { get; set; }
            public string btcu_original_link { get; set; }
            public int btcu_downloaded_status { get; set; }
        }
    }
}