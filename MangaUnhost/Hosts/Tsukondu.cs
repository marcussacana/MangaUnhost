using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MangaUnhost.Hosts
{
    internal class Tsukondu : IHost
    {
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var Page in GetChapterPages(ID))
            {
                yield return Page.TryDownload(MangaUrl, ProxyTools.UserAgent);
            }
        }

        private string[] GetChapterPages(int ID)
        {
            var ChapterPage = LinkMap[ID].TryDownload(MangaUrl, ProxyTools.UserAgent);
            
            var ChapterDoc = new HtmlDocument();
            ChapterDoc.LoadHtml(Encoding.UTF8.GetString(ChapterPage));

            var js = ChapterDoc.SelectSingleNode("//script[contains(.,'ts_reader.run')]").InnerText;

            js = "var ts_reader = [];\r\nts_reader.run = function(a){return a;}\nvar rst = " + js.TrimStart();
            js += "rst.sources[0].images;";

            var Result = (List<object>)JSTools.DefaultBrowser.EvaluateScript(js);
            var Images = Result.Cast<string>();
            return Images.ToArray();
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            GetChapters();

            int ID = 0;
            foreach (var Pair in LinkNames)
            {
                LinkMap[ID] = Pair.Key;
                yield return new KeyValuePair<int, string>(ID++, Pair.Value);
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

        Dictionary<string, string> LinkNames = new Dictionary<string, string>();
        Dictionary<int, string> LinkMap = new Dictionary<int, string>();

        public string[] GetChapters()
        {
            var Chapters = Document.SelectNodes("//div[contains(@class, 'chbox') and not(contains(.,'{number}'))]/div/a");
            var Urls = Chapters.Select(x => x.GetAttributeValue("href", null)).ToArray();
            var Names = Chapters.Select(x => x.SelectSingleNode("span[@class='chapternum' and not(contains(.,'{number}'))]")
                                              .InnerText.ToLowerInvariant()
                                              .Replace("cap.", "")
                                              .Replace("cap", "")
                                              .Replace("capítulo.", "")
                                              .Replace("capítulo", ""))
                                .ToArray();

            LinkNames.Clear();
            for (int i = 0; i < Urls.Length; i++)
                LinkNames[Urls[i]] = Names[i].Trim();

            return Urls;
        }
 
        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Author = "Marcussacana",
                GenericPlugin = false,
                Name = "Tsukondu",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            throw new NotImplementedException();
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.Contains("tsundoku.com") && Uri.AbsoluteUri.Contains("/manga/");
        }

        HtmlDocument Document;
        public string MangaUrl;
        public ComicInfo LoadUri(Uri Uri)
        {
            MangaUrl = Uri.AbsoluteUri;
            var Data = Uri.TryDownload(MangaUrl, UserAgent: ProxyTools.UserAgent);

            var Page = Encoding.UTF8.GetString(Data);

            Document = new HtmlDocument();
            Document.LoadHtml(Page);

            return new ComicInfo() {
                Title = Document.SelectSingleNode("//h1[@class='entry-title']").InnerText,
                Cover = Document.SelectSingleNode("//div[@class='thumb']/img").GetAttributeValue("src", null).TryDownload(MangaUrl, ProxyTools.UserAgent),
                ContentType = ContentType.Comic
            };
        }
    }
}
