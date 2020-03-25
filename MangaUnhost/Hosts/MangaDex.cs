﻿using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
                yield return TryDownload(new Uri(PageUrl));
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

                    Empty = false;

                    var ChapterInfo = Node.SelectSingleNode(Node.XPath + "//a[@class=\"text-truncate\"]");
                    var ChapterLang = Node.SelectSingleNode(Node.XPath + "//span[contains(@class, \"flag\")]");

                    var Name = HttpUtility.HtmlDecode(ChapterInfo.InnerText).ToLower();
                    var Link = HttpUtility.HtmlDecode(ChapterInfo.GetAttributeValue("href", ""));
                    var Lang = HttpUtility.HtmlDecode(ChapterLang.GetAttributeValue("title", "")).Trim();

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
                    ChapterLinks[ID] = new Uri(new Uri("https://mangadex.org"), Link).AbsoluteUri;
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
            string API = $"https://mangadex.org/api/?id={CID}&type=chapter&baseURL=%2Fapi";

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
                Version = new Version(1, 4)
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

            Document = new HtmlAgilityPack.HtmlDocument();
            Document.LoadUrl(Uri, Referer: "https://mangadex.org", UserAgent: CFData?.UserAgent ?? null, Cookies: CFData?.Cookies ?? null);

            ComicInfo Info = new ComicInfo();

            Info.Title = Document.SelectSingleNode("//*[@id=\"content\"]//h6/span[@class=\"mx-1\"]").InnerText;
            Info.Title = HttpUtility.HtmlDecode(Info.Title);

            Info.Cover = TryDownload(new Uri(new Uri("https://mangadex.org"), Document
                .SelectSingleNode("//*[@id=\"content\"]//img[@class=\"rounded\"]")
                .GetAttributeValue("src", string.Empty)));

            Info.ContentType = ContentType.Comic;

            CurrentUrl = Uri.AbsoluteUri;

            return Info;
        }

        CloudflareData? CFData = null;

        private string TryDownload(string Url) {
            var Uri = new Uri(Url);
            var Data = TryDownload(Uri);

            return Encoding.UTF8.GetString(Data);
        }
        private byte[] TryDownload(Uri Url) {
            if (CFData != null) {
                return Url.TryDownload("https://mangadex.org", CFData?.UserAgent, Cookie: CFData?.Cookies);
            }
            try
            {
                return Url.TryDownload();
            }
            catch {
                CFData = JSTools.BypassCloudFlare(Url.AbsoluteUri);
                return TryDownload(Url);
            }
        }

        struct MangaDexApi {
            public int? id;
            public long? timestamp;
            public string hash;
            public string volume;
            public string chapter;
            public string title;
            public string lang_name;
            public string lang_code;
            public int? manga_id;
            public int? group_id;
            public int? group_id_1;
            public int? group_id_2;
            public int? group_id_3;
            public int? comments;
            public string server;
            public string[] page_array;
            public int? long_strip;
            public string status;
        }
    }
}
