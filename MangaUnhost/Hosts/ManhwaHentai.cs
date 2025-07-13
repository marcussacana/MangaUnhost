using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Caching;

namespace MangaUnhost.Hosts
{
    internal class ManhwaHentai : IHost
    {
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var Page in GetChapPages(ID))
            {
                yield return new Uri(Page).TryDownload(CFData, Referer: currentUrl.AbsoluteUri);
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            foreach (var Chapter in GetChapters())
            {
                yield return new KeyValuePair<int, string>((int)(Chapter.Key * 1000), Chapter.Key.ToString());
            }
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapPages(ID).Count;
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        private List<string> GetChapPages(int ID)
        {
            var ChapUrl = Chapters[((double)ID) / 1000];

            var PageLinks = new List<string>();
            
            var Doc = new HtmlDocument();
            Doc.LoadUrl(ChapUrl, CFData);

            var Nodes = Doc
                .DocumentNode
                .SelectSingleNode("//script[contains(., 'read_image_list')]");

            var JS = Nodes.InnerHtml;

            var StoreArray = JS.Substring("var ", "= [").Trim();

            JS += $"\r\n{StoreArray}";

            var Pages = JSTools.EvaluateScript<List<object>>(JS).Cast<string>();

            return Pages.Select(p => new Uri(currentUrl, p).AbsoluteUri).ToList();
        }

        Dictionary<double, string> Chapters = new Dictionary<double, string>();
        private Dictionary<double, string> GetChapters()
        {
            Chapters = new Dictionary<double, string>();

            var Nodes = Doc
                .DocumentNode
                .SelectNodes("//div[@class='info']//li/a");

            if (Nodes != null)
            {
                foreach (var Node in Nodes)
                {
                    var name = Node.SelectSingleNode(Node.XPath + "//div[@class='chapter-name']").InnerText;

                    name = name.Replace("Capítulo", "");
                    name = name.Replace("Chapter", "").Trim();

                    var num = DataTools.ForceNumber(name);
                    Chapters[num] = new Uri(currentUrl, Node.GetAttributeValue("href", null)).AbsoluteUri;
                }
            }

            return Chapters;
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo
            {
                Name = "ManhwaHentai",
                Version = new Version(1, 0),
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                GenericPlugin = false,
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            return HTML.Contains("/update-view?");
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.ToLower().Contains("manhwahentai.io");
        }

        Uri currentUrl = null;
        HtmlDocument Doc = new HtmlDocument();
        CloudflareData? CFData = null;
        public ComicInfo LoadUri(Uri Uri)
        {
            currentUrl = Uri;
            CFData = Doc.LoadUrl(Uri);

            return new ComicInfo
            {
                Title = Doc
                    .DocumentNode
                    .SelectSingleNode("//h1[@class='title']")
                    .InnerText.Trim(),

                Url = Uri,
                ContentType = ContentType.Comic,
                Cover = Doc
                    .DocumentNode
                    .SelectSingleNode("//div[@class='single-detail']/div[@class='thumb']//img")
                    .GetAttributeValue("src", null)?.TryDownload()
            };
        }
    }
}
