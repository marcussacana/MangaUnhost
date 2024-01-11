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
using static MangaUnhost.Hosts.SlimeRead;

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
            var BookChaps = Info.props.pageProps.book_info.book_temp;
            return BookChaps
                .OrderByDescending(x => x.bt_id)
                .SelectMany(x => x.book_temp_caps)
                .Select(x => {
                    var ID = ChapterMap.Count;
                    int RealChapNum = (int)Math.Truncate(x.btc_cap);
                    int ChapNum = RealChapNum + 1;
                    float ChapPart = (float)Math.Round((x.btc_cap - ChapNum + 1) * 100f);
                    ChapterMap[ID] = ChapPart == 0 ? $"{RealChapNum}" : $"{RealChapNum}.{ChapPart}".TrimEnd('0', '.');
                    var Name = ChapPart == 0 ? $"{ChapNum}" : $"{ChapNum}.{ChapPart}".TrimEnd('0', '.');

                    return new KeyValuePair<int, string>(ID, $"{Name}");
                 });
        }

        public RootInfo GetComicInfo()
        {
            //possible get json with this url as well
            //https://slimeread.com/_next/data/aoKuUaWGR_--cgEqsV76H/index.json
            var Info = Document.SelectSingleNode("//script[@type='application/json']").InnerText;

            return Newtonsoft.Json.JsonConvert.DeserializeObject<RootInfo>(Info);
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID).Length;
        }

        public string[] GetChapterPages(int ID)
        {
            var Uri = $"https://ai3.slimeread.com:8443/book_cap_units?manga_id={MangaID}&cap={ChapterMap[ID]}";// &token={Token}";

            var JSON = Uri.TryDownloadString(Referer: "https://slimeread.com", UserAgent: ProxyTools.UserAgent).Trim(' ', '\t', '[', ']');

            
            var ChInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<ChapterInfo>(JSON);

            if (ChInfo.book_temp != null)
            {
                var Images = ChInfo.book_temp.SelectMany(x => x.book_temp_caps)
                    .SelectMany(x => x.book_temp_cap_unit)
                    .Select(x => $"https://objects.slimeread.com/{x.btcu_image}");

                return Images.ToArray();
            }

            var Info = Newtonsoft.Json.JsonConvert.DeserializeObject<BookTempCap>(JSON);

            var Pages = Info.book_temp_cap_unit
                .Select(x => $"https://objects.slimeread.com/{x.btcu_image}");

            return Pages.ToArray();

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
                Version = new Version(1, 0, 4)
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

            var CEF = new ChromiumWebBrowser();

            CEF.WaitInitialize();
            CEF.LoadUrl("https://slimeread.com/login");
            CEF.WaitForLoad();

            var Browser = new BrowserPopup(CEF, () => { return !CEF.GetCurrentUrl().Contains("/login") && !CEF.GetCurrentUrl().Contains("/registrar"); });
            Browser.ShowDialog();

            Token = CEF.GetCookie("nextauth.token");

            Ini.SetConfig("SlimeRead", "Token", Token, Main.SettingsPath);

            EnsureLogin();
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

            var CoverData = Cover.TryDownload(Cover);

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
            public int scan_id { get; set; }
        }

        public struct BtcCap
        {
            public float btc_cap { get; set; }
            public DateTime btc_date_updated { get; set; }
            public string btc_name { get; set; }
            public Scan scan { get; set; }
            public List<object> scans_parterners { get; set; }

            public int btc_id { get; set; }
            public int btc_scan_id { get; set; }
            public List<BookTempCapUnit> book_temp_cap_unit { get; set; }
        }

        public struct BookTemp
        {
            public int bt_id { get; set; }
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
            public List<object> book_infos { get; set; }
            public List<Related> related { get; set; }
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
            public int btc_id { get; set; }
            public int btc_scan_id { get; set; }
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