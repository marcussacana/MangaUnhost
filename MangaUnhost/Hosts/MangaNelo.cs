using CefSharp.DevTools.DOM;
using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MangaUnhost.Hosts {
    class MangaNelo : IHost {
        string CurrentUrl;
        HtmlDocument Document;
        Dictionary<int, string> ChapterNames = new Dictionary<int, string>();
        Dictionary<int, string> ChapterLinks = new Dictionary<int, string>();

        CloudflareData? CFData = null;

        public NovelChapter DownloadChapter(int ID) {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID) {
            foreach (var PageUrl in GetChapterPages(ID)) {
                var Page = PageUrl.TryDownload(Referer: CurrentUrl);
                if (Page == null && CFData != null)
                    Page = PageUrl.TryDownload(CFData, Referer: CurrentUrl);
                if (Page == null) {
                    CFData = JSTools.BypassCloudflare(PageUrl);
                    Page = PageUrl.TryDownload(CFData, Referer: CurrentUrl);
                }
                yield return Page;
            }
        }

        string LastLang = null;
        private string SelectLanguage(string[] Avaliable)
        {
            if (LastLang != null && Avaliable.Contains(LastLang))
                return LastLang;
            return LastLang = AccountTools.PromptOption("Select a Language", Avaliable.Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).ToArray());
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters() {
            int ID = ChapterLinks.Count;

            var Nodes = Document.SelectNodes("//div[@class=\"chapter-list\"]/div/span/a");

            if (Nodes == null || Nodes.Count <= 0) {
                var SafeDoc = new HtmlDocument();
                if (Document.IsCloudflareTriggered()) {
                    CFData = JSTools.BypassCloudflare(CurrentUrl);
                    SafeDoc.LoadHtml(CFData?.HTML);
                } else
                    SafeDoc = Document;

                Nodes = SafeDoc.SelectNodes("//div[@class=\"chapter-list\"]/div/span/a");

                if (Nodes == null || Nodes.Count <= 0)
                    Nodes = SafeDoc.SelectNodes("//a[@class=\"chapter-name text-nowrap\"]");

                if (Nodes == null || Nodes.Count <= 0)
                    Nodes = SafeDoc.SelectNodes("//*[@class=\"chapter-list\"]//a");

                var allUrl = new Uri(new Uri(CurrentUrl), "./all-chapters");

                try { 
                    var tmpDoc = new HtmlDocument();
                    CFData = tmpDoc.LoadUrl(allUrl, CFData);

                    var allChaps = tmpDoc.SelectNodes("//*[@class=\"chapter-list\"]//a");
                    if (allChaps != null && allChaps.Count > 0)
                        Nodes = allChaps;
                }
                catch {

                }



                if (Nodes == null || Nodes.Count <= 0)
                {
                    var MangaId = SafeDoc.SelectSingleNode("//div[@id=\"main\"]").GetAttributeValue("data-id", null);

                    var RequestApi = new Uri($"https://{new Uri(CurrentUrl).Host}/ajax/manga/list-chapter-volume?id={MangaId}");

                    var Data = RequestApi.TryDownloadString(CFData);

                    if (Data != null)
                    {
                        SafeDoc.LoadHtml(Data);

                        var Languages = SafeDoc.SelectNodes("//a[@class=\"dropdown-item lang-item\"]").Select(x => (x.GetAttributeValue("data-code", null), x.GetDirectInnerText().Split('(').First().Trim()));

                        string LangCode = null;

                        if (Languages != null)
                        {
                            var Lang = SelectLanguage(Languages.Select(x=>x.Item2).ToArray());

                            LangCode = Languages.FirstOrDefault(x => x.Item2 == Lang).Item1;
                        }


                        if (LangCode != null)
                        {
                            Nodes = SafeDoc.SelectNodes($"//div[@id=\"list-chapter-{LangCode}\"]//a");
                        }
                        else
                        {

                            Nodes = SafeDoc.SelectNodes($"//a");
                        }
                    }
                }
            }

            foreach (var Node in Nodes)
            {
                if (Node.SelectSingleParent("/*[@class='chapter-update']") is HtmlNode child && child != null)
                    child.Remove();

                string Name = HttpUtility.HtmlDecode(Node.InnerText).ToLower();
                string Link = Node.GetAttributeValue("href", string.Empty);

                if (!Name.ToLower().Contains("chapter")) {
                    if (Link.ToLower().Contains("chapter"))
                        Name = Link.Substring("chapter");
                    Name = string.Join(".", from x in Name.Split(' ', '-', '_') where double.TryParse(x, out _) select x);
                } else
                    Name = Name.Substring("chapter").Trim();

                if (Name.Contains(":"))
                    Name = Name.Substring(0, Name.IndexOf(":"));

                Link = Link.EnsureAbsoluteUrl(CurrentUrl);

                ChapterNames[ID] = DataTools.GetRawName(Name);
                ChapterLinks[ID] = Link;
                yield return new KeyValuePair<int, string>(ID, ChapterNames[ID++]);
            }
        }

        public int GetChapterPageCount(int ID) {
            return GetChapterPages(ID).Length;
        }

        private string[] GetChapterPages(int ID) {
            var Page = GetChapterHtml(ID);
            List<string> Pages = new List<string>();

            var Nodes = Page.DocumentNode.SelectNodes("//*[@id=\"vungdoc\"]/img");

            if (Nodes == null || Nodes.Count <= 0)
                Nodes = Page.DocumentNode.SelectNodes("//*[@id=\"vungdoc\"]/div/img");

            if (Nodes == null || Nodes.Count <= 0)
                Nodes = Page.DocumentNode.SelectNodes("//*[@class=\"container-chapter-reader\"]/img");

            if (Nodes == null || Nodes.Count <= 0)
                Nodes = Page.DocumentNode.SelectNodes("//*[@id=\"chapter-reader\"]/img");

            if (Nodes == null || Nodes.Count <= 0) {
                Nodes = Page.DocumentNode.SelectNodes("//*[contains(@class, \"chapter-content-inner\")]/p");

                if (Nodes != null)
                {
                    return Nodes.First().InnerText.Split(',');
                }
            }
            
            if (Nodes == null || Nodes.Count <= 0)
            {
                var ChapId = Page.SelectSingleNode("//div[@id=\"reading\"]").GetAttributeValue("data-reading-id", null);

                var RequestApi = new Uri($"https://{new Uri(CurrentUrl).Host}/ajax/manga/images?id={ChapId}&type=chap");

                var Data = RequestApi.TryDownloadString(CFData);

                if (Data != null)
                {
                    Page.LoadHtml(Data);

                    Nodes = Page.DocumentNode.SelectNodes("//*[@data-url]");
                }
            }

            var PageList = Nodes.Where(x => x.GetAttributeValue("height", "") != "1");

            foreach (var Node in PageList)
                Pages.Add(Node.GetAttributeValue("src", null) ??
                    Node.GetAttributeValue("data-src", null) ??
                    Node.GetAttributeValue("data-url", ""));

            return Pages.ToArray();
        }

        private HtmlDocument GetChapterHtml(int ID) {
            HtmlDocument Document = new HtmlDocument();
            Document.LoadUrl(ChapterLinks[ID]);
            return Document;
        }

        public IDecoder GetDecoder() {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo() {
            return new PluginInfo() {
                Name = "MangaNelo Based Sites",
                Author = "Marcussacana",
                SupportComic = true,
                GenericPlugin = true,
                SupportNovel = false,
                Version = new Version(2, 1)
            };
        }

        public bool IsValidUri(Uri Uri) {
            string[] AllowedDomains = new string[] { "mangakakalot", "manganelo", "truyenmoi" };
            return (from x in AllowedDomains where Uri.Host.ToLower().Contains(x) select x).Count() > 0;
        }

        public ComicInfo LoadUri(Uri Uri) {
            CurrentUrl = Uri.AbsoluteUri;
            Document = new HtmlDocument();
            Document.LoadUrl(Uri);

            ComicInfo Info = new ComicInfo();

            Info.Title = (Document.SelectSingleNode("//ul[@class=\"manga-info-text\"]/li/h1") ??
                          Document.SelectSingleNode("//ul[@class=\"manga-info-text\"]/li/h2") ??
                          Document.SelectSingleNode("//div[@class=\"story-info-right\"]/h1")  ??
                          Document.SelectSingleNode("//h1[@class=\"title-manga\"]") ??
                          Document.SelectSingleNode("//h2[@class=\"title-manga\"]") ??
                          Document.SelectSingleNode("//h1[contains(@class, \"novel-title\")]") ??
                          Document.SelectSingleNode("//h2contains(@class, \"novel-title\")]") ??
                          Document.SelectSingleNode("//div[@class=\"detail-box\"]//h3[@class=\"manga-name\"]")).InnerText.Trim();

            Info.Title = HttpUtility.HtmlDecode(Info.Title);

            var coverNode = (Document.SelectSingleNode("//div[@class=\"manga-info-pic\"]/img") ??
                               Document.SelectSingleNode("//span[@class=\"info-image\"]/img") ??
                               Document.SelectSingleNode("//figure[@class=\"cover\"]/img") ??
                               Document.SelectSingleNode("//div[@class=\"media-left cover-detail\"]/img") ??
                               Document.SelectSingleNode("//div[@class=\"detail-box\"]//img[@class=\"manga-poster-img\"]"));

            string CoverUrl = coverNode.GetAttributeValue("data-src", null) ??
                                coverNode.GetAttributeValue("src", string.Empty);

            Info.Cover = CoverUrl.EnsureAbsoluteUrl(Uri).TryDownload();

            Info.ContentType = ContentType.Comic;

            return Info;
        }
        public bool IsValidPage(string HTML, Uri URL) {
            return (HTML.Contains("chapter-list") || HTML.Contains("chapter-name text-nowrap"));
        }
    }
}
