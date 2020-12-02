using CefSharp.OffScreen;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MangaUnhost.Hosts {
    class MangaDex : IHost {
        string CurrentUrl;
        HtmlAgilityPack.HtmlDocument Document;
        Dictionary<int, string> ChapterNames = new Dictionary<int, string>();
        Dictionary<int, string> ChapterLinks = new Dictionary<int, string>();
        Dictionary<int, string> ChapterLangs = new Dictionary<int, string>();

        public NovelChapter DownloadChapter(int ID) {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID) {
            foreach (var PageUrl in GetChapterPages(ID)) {
                var Data = TryDownload(new Uri(PageUrl), ChapterLinks[ID]);
                if (Data == null)
                    continue;
                yield return Data;
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters() {
            int ID = ChapterLinks.Count;
            HtmlAgilityPack.HtmlDocument Page = Document;
            string CurrentPage = CurrentUrl;

            bool Empty;
            List<int> Ids = new List<int>();
            ChapterLangs = new Dictionary<int, string>();
            ChapterLinks = new Dictionary<int, string>();
            ChapterNames = new Dictionary<int, string>();

            do {
                bool First = true;
                Empty = true;
                foreach (var Node in Page.SelectNodes("//*[@id=\"content\"]//div[contains(@class, \"chapter-row\")]")) {
                    if (First) {
                        First = false;
                        continue;
                    }

                    var ChapterInfo = Node.SelectSingleNode(Node.XPath + "//a[@class=\"text-truncate\"]");
                    var ChapterLang = Node.SelectSingleNode(Node.XPath + "//span[contains(@class, \"flag\")]");

                    var Name = HttpUtility.HtmlDecode(ChapterInfo.InnerText).ToLower();
                    var Link = HttpUtility.HtmlDecode(ChapterInfo.GetAttributeValue("href", ""));
                    var Lang = HttpUtility.HtmlDecode(ChapterLang.GetAttributeValue("title", "")).Trim();

                    Link = Link.EnsureAbsoluteUrl("https://mangadex.org");

                    if (ChapterLinks.Values.Contains(Link))
                        continue;

                    Empty = false;

                    if (Name.Contains('-'))
                        Name = Name.Substring(0, Name.IndexOf("-"));

                    if (Name.Contains("vol.")) {
                        Name = Name.Substring("vol. ");

                        var Parts = Name.Split(' ');
                        if (Parts.Length > 2)
                            Name = Parts[2];
                        else
                            Name = char.ToUpper(Name[0]) + Name.Substring(1);

                    } else if (Name.Contains("ch. "))
                        Name = Name.Substring("ch. ");

                    ChapterNames[ID] = DataTools.GetRawName(Name.Trim());
                    ChapterLinks[ID] = Link;
                    ChapterLangs[ID] = Lang;

                    Ids.Add(ID++);

                }

                if (!Empty) {
                    CurrentPage = GetNextPage(CurrentPage);
                    Page = new HtmlAgilityPack.HtmlDocument();
                    Page.LoadUrl(CurrentPage, Referer: "https://mangadex.org", UserAgent: CFData?.UserAgent ?? null, Cookies: CFData?.Cookies ?? null);
                }

            } while (!Empty);

            string SelectedLang = SelectLanguage((from x in Ids select ChapterLangs[x]).Distinct().ToArray());

            return (from x in Ids
                    where ChapterLangs[x] == SelectedLang
                    select new KeyValuePair<int, string>(x, ChapterNames[x]));
        }

        private static string LastLang = null;
        private string SelectLanguage(string[] Avaliable) {
            if (LastLang != null && Avaliable.Contains(LastLang))
                return LastLang;
            return LastLang = AccountTools.PromptOption("Select a Language", Avaliable);
        }

        private string GetNextPage(string Page) {
            if (Page == null)
                Page = CurrentUrl;

            return Page.Substring(0, Page.IndexOf("/chapters/")) + "/chapters/" + (int.Parse(Page.Split('/').Last()) + 1);
        }

        public int GetChapterPageCount(int ID) {
            return GetChapterPages(ID).Length;
        }

        private string[] GetChapterPages(int ID) {
            string CID = ChapterLinks[ID].Substring("/chapter/");
            string API = $"https://mangadex.org/api/?id={CID}&server=null&saver=0&type=chapter";

            string JSON = TryDownload(API);

            MangaDexApi Result = Extensions.JsonDecode<MangaDexApi>(JSON);

            if (Result.status == "delayed")
                return null;
            if (Result.status != "OK")
                throw new Exception();

            if (!Result.server.ToLower().Contains("mangadex."))
                Result.server = "https://mangadex.org" + Result.server;

            List<string> Pages = new List<string>();
            foreach (string Page in Result.page_array) {
                Pages.Add($"{Result.server}{Result.hash}/{Page}");
            }

            return Pages.ToArray();
        }

        public IDecoder GetDecoder() {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo() {
            var Info = new PluginInfo() {
                Name = "MangaDex",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 6)
            };

            if (ChapterLangs.Count > 0) {
                Info.Actions = new CustomAction[] {
                    new CustomAction() {
                        Name = Main.Language.SwitchLanguage,
                        Availability = ActionTo.ChapterList,
                        Action = () => {
                            LastLang = null;
                            Main.Instance.Reload();
                        }
                    }
                };
            }
            return Info;
        }

        public bool IsValidUri(Uri Uri) {
            return Uri.Host.ToLower().Contains("mangadex") && Uri.AbsolutePath.ToLower().Contains("/title/");
        }

        public ComicInfo LoadUri(Uri Uri) {
            if (Uri.AbsoluteUri.Contains("/chapters/"))
                Uri = new Uri(Uri.AbsoluteUri.Substring(0, Uri.AbsoluteUri.IndexOf("/chapters/")).TrimEnd('/') + "/chapters/1");
            else
                Uri = new Uri(Uri.AbsoluteUri.TrimEnd('/') + "/chapters/1");

            if (CFData == null) {
                using (ChromiumWebBrowser Browser = new ChromiumWebBrowser()) {
                    Browser.WaitForLoad(Uri.AbsoluteUri);
                    do {
                        CFData = Browser.BypassCloudflare();
                    } while (Browser.IsCloudflareTriggered());
                }
            }

            Document = new HtmlAgilityPack.HtmlDocument();
            Document.LoadUrl(Uri, Referer: "https://mangadex.org", UserAgent: CFData?.UserAgent ?? null, Cookies: CFData?.Cookies ?? null);

            ComicInfo Info = new ComicInfo();

            Info.Title = Document.SelectSingleNode("//*[@id=\"content\"]//h6/span[@class=\"mx-1\"]").InnerText;
            Info.Title = HttpUtility.HtmlDecode(Info.Title);

            Info.Cover = TryDownload(Document
                .SelectSingleNode("//*[@id=\"content\"]//img[@class=\"rounded\"]")
                .GetAttributeValue("src", string.Empty).EnsureAbsoluteUri("https://mangadex.org"));

            Info.ContentType = ContentType.Comic;

            CurrentUrl = Uri.AbsoluteUri;

            if (Uri.AbsolutePath.Trim('/').Split('/').Length == 4) {
                CurrentUrl = Document.SelectSingleNode("//link[@rel='canonical']").GetAttributeValue("href", null);
                CurrentUrl = CurrentUrl.TrimEnd() + "/chapters/1";
            }

            return Info;
        }

        CloudflareData? CFData = null;

        private string TryDownload(string Url) {
            var Uri = new Uri(Url);
            var Data = TryDownload(Uri);

            return Encoding.UTF8.GetString(Data);
        }
        
        private byte[] TryDownload(Uri Url, string Referer = "https://mangadex.org") {
            if (CFData != null) {
                return Url.TryDownload(Referer, CFData?.UserAgent, Cookie: CFData?.Cookies);
            }
            try
            {
                return Url.TryDownload(Referer);
            }
            catch {
                CFData = JSTools.BypassCloudflare(Url.AbsoluteUri);
                return TryDownload(Url, Referer);
            }
        }
        public bool IsValidPage(string HTML, Uri URL) => false;

        struct MangaDexApi {
            /*
            public int? id;
            public long? timestamp;
            */
            public string hash;
            /*
            public string volume;
            public string chapter;
            public string title;
            public string lang_name;
            public string lang_code;
            public int? manga_id;
            public int? group_id;
            public string group_name;
            public int? group_id_2;
            public string group_name_2;
            public int? group_id_3;
            public string group_name_3;
            public int? comments;
            */
            public string server;
            public string[] page_array;
            //public int? long_strip;
            public string status;
        }
    }
}
