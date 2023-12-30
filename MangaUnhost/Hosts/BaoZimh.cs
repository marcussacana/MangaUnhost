using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;

namespace MangaUnhost.Hosts
{
    internal class BaoZimh : IHost
    {
        Dictionary<int, string> ChapterMap = new Dictionary<int, string>();
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var Page in GetChapterPages(ID))
            {
                yield return Page.TryDownload(Referer: CurrentUrl.AbsolutePath, UserAgent: ProxyTools.UserAgent);
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            var Chapters = Document.SelectNodes("//div[@id='chapter-items' or @id='chapters_other_list']//a");
            foreach (var Chapter in Chapters.Reverse())
            {
                var ChapterUrl = HttpUtility.HtmlDecode(Chapter.GetAttributeValue("href", null));
                ChapterUrl = new Uri(CurrentUrl, ChapterUrl).AbsoluteUri;

                var ChapterName = Chapter.InnerText;
                var ChapterValue = "";
                foreach (char Char in ChapterName)
                {
                    if (char.IsDigit(Char))
                        ChapterValue += Char;
                    else
                        break;
                }

                if (!string.IsNullOrWhiteSpace(ChapterValue))
                    ChapterName = ChapterValue;

                yield return new KeyValuePair<int, string>(ChapterMap.Count, ChapterName);
                ChapterMap[ChapterMap.Count] = ChapterUrl;
            }
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID).Length;
        }

        public string[] GetChapterPages(int ID)
        {
            var ChapterURL = ChapterMap[ID];

            var Doc = new HtmlDocument();
            Doc.LoadUrl(ChapterURL);

            var Scripts = Doc.SelectNodes("//script[@type='application/json']");

            List<string> Pages = new List<string>();
            foreach (var JSON in Scripts)
            {
                var Uri = DataTools.ReadJson(JSON.InnerHtml, "url");
                Pages.Add(Uri);
            }

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
                SupportComic = true,
                Author = "Marcussacana",
                Name = "BaoZimh",
                Version = new Version(1, 0)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            throw new NotImplementedException();
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.ToLower().Contains("baozimh")
                && Uri.PathAndQuery.Contains("/comic/");
        }

        Uri CurrentUrl;
        HtmlDocument Document = new HtmlDocument();

        public ComicInfo LoadUri(Uri Uri)
        {
            CurrentUrl = Uri;
            Document.LoadUrl(Uri);

            var CoverUrl = Document.SelectSingleNode("//img[contains(@src, '/cover/')]")?.GetAttributeValue("src", null)
                        ?? Document.SelectSingleNode("//amp-social-share[@type='pinterest']").GetAttributeValue("data-param-media", "").Split('?').First();

            var Title = Document.SelectSingleNode("//h1[contains(@class, 'comics-detail__title')]");

            return new ComicInfo()
            {
                ContentType = ContentType.Comic,
                Cover = CoverUrl.TryDownload(),
                Title = Title.InnerText.Trim()
            };

        }
    }
}
