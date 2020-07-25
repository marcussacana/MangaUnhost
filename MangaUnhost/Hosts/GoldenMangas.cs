using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MangaUnhost.Hosts {
    class GoldenMangas : IHost {
        string CurrentDomain;
        HtmlDocument Document;
        Dictionary<int, string> ChapterNames = new Dictionary<int, string>();
        Dictionary<int, string> ChapterLinks = new Dictionary<int, string>();

        public NovelChapter DownloadChapter(int ID) {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID) {
            foreach (var Page in GetChapterPages(ID)) {
                yield return Page.TryDownload();
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters() {
            int ID = ChapterNames.Count;
            foreach (var Node in Document.SelectNodes("//ul[@class=\"capitulos\"]//a[starts-with(@href, \"/manga\")]")) {
                string URL = HttpUtility.HtmlDecode(Node.GetAttributeValue("href", "")).EnsureAbsoluteUrl(CurrentDomain);
                string Name = URL.Substring(URL.LastIndexOf("/") + 1);
                Name = DataTools.GetRawName(Name);

                ChapterNames[ID] = Name;
                ChapterLinks[ID] = URL;

                yield return new KeyValuePair<int, string>(ID++, Name);
            }
        }

        public int GetChapterPageCount(int ID) {
            return GetChapterPages(ID).Length;
        }
        
        public string[] GetChapterPages(int ID) {
            var Document = new HtmlDocument();
            Document.LoadUrl(ChapterLinks[ID]);

            List<string> Pages = new List<string>();
            foreach (var Node in Document.SelectNodes("//div[@id=\"capitulos_images\"]/center/img")) {
                Pages.Add(HttpUtility.HtmlDecode(Node.GetAttributeValue("src", "")).EnsureAbsoluteUrl(CurrentDomain));
            }

            return Pages.ToArray();
        }

        public IDecoder GetDecoder() {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo() {
            return new PluginInfo() {
                Author = "Marcussacana",
                Name = "GoldenMangas",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidUri(Uri Uri) {
            return Uri.Host.ToLower().Contains("goldenmangas") && Uri.AbsoluteUri.Contains("/manga");
        }

        public ComicInfo LoadUri(Uri Uri) {
            CurrentDomain = $"https://{Uri.Host}";

            Document = new HtmlDocument();
            Document.LoadUrl(Uri);

            ComicInfo Info = new ComicInfo();

            Info.Title = Document.SelectSingleNode("//h2[@class=\"cg_color\"]").InnerText;
            Info.Title = HttpUtility.HtmlDecode(Info.Title);

            Info.Cover = HttpUtility.HtmlDecode(Document
                .SelectSingleNode("//div[@class=\"col-sm-4 text-right\"]/img")
                .GetAttributeValue("src", "")).EnsureAbsoluteUrl(CurrentDomain).TryDownload();

            Info.ContentType = ContentType.Comic;

            return Info;
        }
        public bool IsValidPage(string HTML, Uri URL) => false;
    }
}
