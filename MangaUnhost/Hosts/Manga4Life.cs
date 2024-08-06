using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaUnhost.Hosts
{
    internal class Manga4Life : IHost
    {
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var Page in GetPages(ID))
            {
                yield return Page.TryDownload(CFData, Referer: ChapterList[ID]);
            }
        }

        Dictionary<int, int> ChapterNums = new Dictionary<int, int>();
        Dictionary<int, string> ChapterList = new Dictionary<int, string>();

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            var Chaps = GetChapters();
            for (var i = Chaps.Length - 1; i >= 0; i--)
            {
                var id = ChapterList.Count;
                ChapterList[id] = Chaps[i];
                ChapterNums[id] = i + 1;
                yield return new KeyValuePair<int, string>(id, ChapterNums[id].ToString());
            }
        }

        public int GetChapterPageCount(int ID)
        {
            return GetPages(ID).Length;
        }

        public string[] GetPages(int ID)
        {
            var Url = ChapterList[ID];
            var Doc = new HtmlDocument();
            Doc.LoadUrl(Url, CFData);

            var Script = Doc.SelectSingleNode("//script[contains(., 'vm.CurChapter = {')]").InnerHtml;
            var CurChap = Script.Substring("vm.CurChapter =", ";");
            var CurDomain = Script.Substring("vm.CurPathName = \"", "\";");

            var ChapDir = DataTools.ReadJson(CurChap, "Directory");
            var PageCount = int.Parse(DataTools.ReadJson(CurChap, "Page"));

            if (string.IsNullOrEmpty(ChapDir))
                ChapDir = "/";
            else
                ChapDir = $"/{ChapDir.Trim('/')}/";


            //https://manga4life.com/manga/Hope-Youre-Happy-Lemon
            var CurrentPath = CurrentUrl.AbsolutePath.Split('/').Last();
            var Pages = new string[PageCount];

            for (var i = 0; i < PageCount; i++)
            {
                Pages[i] = $"https://{CurDomain}/manga/{CurrentPath}{ChapDir}{ChapterNums[ID]:D4}-{i+1:D3}.png";
            }

            return Pages;
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        public string[] GetChapters()
        {
            var Script = CurrentDoc.SelectSingleNode("//script[contains(.,'vm.Chapters =')]").InnerHtml;
            var ChapList = Script.Substring("vm.Chapters =", ";");
            var ChapCount = ChapList.Split('{').Count() - 1;

            string[] Chapters = new string[ChapCount];

            //https://manga4life.com/manga/Hope-Youre-Happy-Lemon
            var CurrentPath = CurrentUrl.AbsolutePath.Split('/').Last();

            for (var i = 0; i < ChapCount; i++)
            {                
                Chapters[i] = $"https://manga4life.com/read-online/{CurrentPath}-chapter-{i + 1}.html";
            }

            return Chapters;
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo() { 
                Name = "Manga4Life",
                Author = "Marcussacana",
                SupportComic = true,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            throw new NotImplementedException();
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host == "manga4life.com" && Uri.AbsolutePath.Contains("/manga/");
        }

        Uri CurrentUrl;
        HtmlDocument CurrentDoc;
        CloudflareData? CFData;
        public ComicInfo LoadUri(Uri Uri)
        {
            CurrentUrl = Uri;
            CurrentDoc = new HtmlDocument();
            CFData = CurrentDoc.LoadUrl(Uri.AbsoluteUri);

            var Title = CurrentDoc.SelectSingleNode("//ul[@class='list-group list-group-flush']//h1").InnerText;
            var CoverUrl = CurrentDoc.SelectSingleNode("//img[@class='img-fluid bottom-5']").GetAttributeValue("src", null);

            return new ComicInfo() {
                Title = Title,
                Cover = CoverUrl.TryDownload(Referer: Uri.AbsoluteUri),
                ContentType = ContentType.Comic,
                Url = Uri
            };
        }
    }
}
