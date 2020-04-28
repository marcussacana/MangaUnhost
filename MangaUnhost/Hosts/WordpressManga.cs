using HtmlAgilityPack;
using MangaUnhost.Browser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MangaUnhost.Hosts
{
    class WordpressManga : IHost
    {

        bool ReverseChapters = false;

        Dictionary<int, string> LinkMap = new Dictionary<int, string>();
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var Page in GetPageLinks(ID))
                yield return Page.TryDownload();
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            int ID = LinkMap.Count;

            var Nodes = Document.SelectNodes("//li[starts-with(@class, \"wp-manga-chapter\")]/a");

            foreach (var Node in ReverseChapters ? Nodes.Reverse() : Nodes)
            {
                string URL = Node.GetAttributeValue("href", "");
                string Name = Node.InnerText.Trim().ToLower();

                if (Name.StartsWith("chapter"))
                    Name = Name.Substring("chapter").Trim();
                if (Name.StartsWith("chap"))
                    Name = Name.Substring("chap").Trim(' ', '\t', '.');
                    
                if (Name.Contains("-"))
                    Name = Name.Split('-').First().Trim();

                LinkMap[ID] = URL;

                yield return new KeyValuePair<int, string>(ID++, Name);
            }
        }

        public int GetChapterPageCount(int ID)
        {
            return GetPageLinks(ID).Length;
        }

        private string[] GetPageLinks(int ID)
        {
            var Chapter = new HtmlDocument();
            Chapter.LoadUrl(LinkMap[ID]);

            string[] Links = (from x in Chapter
                              .SelectNodes("//img[starts-with(@id, \"image-\")]")
                              select (x.GetAttributeValue("data-src", null) ??
                                      x.GetAttributeValue("src", "")).Trim()).ToArray();

            return Links;
        }

        public IDecoder GetDecoder()
        {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Author = "Marcussacana",
                Name = "Wordpress Manga Reader",
                SupportComic = true,
                SupportNovel = false,
                GenericPlugin = true,
                Version = new Version(1, 3)
            };
        }

        public bool IsValidUri(Uri Uri)
        {
           return (Uri.Host.ToLower().Contains("isekaiscan.com") && Uri.AbsolutePath.ToLower().Contains("manga/"))   ||
                  (Uri.Host.ToLower().Contains("manga47.com")    && Uri.AbsolutePath.ToLower().Contains("manga/"))   ||
                  (Uri.Host.ToLower().Contains("manga68.com")    && Uri.AbsolutePath.ToLower().Contains("manga/"))   ||
                  (Uri.Host.ToLower().Contains("toonily.com")    && Uri.AbsolutePath.ToLower().Contains("webtoon/"));
        }
        public bool IsValidPage(string HTML, Uri URL)
        {
            if (!HTML.Contains("wp-manga-chapter"))
                return false;

            if (URL.AbsolutePath.ToLower().Contains("manga/"))
                return true;
            if (URL.AbsolutePath.ToLower().Contains("webtoon/"))
                return true;

            return false;
        }

        HtmlDocument Document = new HtmlDocument();
        public ComicInfo LoadUri(Uri Uri)
        {
            if (Uri.Host.ToLower().Contains("manga47.com"))
                ReverseChapters = true;
            else 
                ReverseChapters = false;

            Document.LoadUrl(Uri);

            ComicInfo Info = new ComicInfo();
            Info.Title = Document.SelectSingleNode("//div[@class=\"post-title\"]/*[self::h3 or self::h2 or self::h1]").InnerText.Trim();
            if (Info.Title.ToUpper().StartsWith("HOT"))
                Info.Title = Info.Title.Substring(3);
            Info.Title = HttpUtility.HtmlDecode(Info.Title).Trim();
            
            var ImgNode = Document.SelectSingleNode("//div[@class=\"summary_image\"]/a/img");

            var ImgUrl = ImgNode.GetAttributeValue("data-lazy-srcset", "");
            
            if (string.IsNullOrEmpty(ImgUrl))
                ImgUrl = ImgNode.GetAttributeValue("data-src", "");
            else
                ImgUrl = ImgUrl.Trim().Split(',', ' ').First();
            
            if (string.IsNullOrWhiteSpace(ImgUrl))
                ImgUrl = ImgNode.GetAttributeValue("src", "");
            
            Info.Cover = ImgUrl.TryDownload();

            Info.ContentType = ContentType.Comic;

            return Info;
        }
    }
}
