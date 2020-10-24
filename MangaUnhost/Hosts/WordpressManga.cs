using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
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
            {
                byte[] Data;

                if (CFData == null)
                    Data = Page.TryDownload(UserAgent: ProxyTools.UserAgent, Referer: LinkMap[ID]);
                else
                    Data = Page.TryDownload(CFData, Referer: LinkMap[ID]);

                if (Data == null || Data.Length == 0)
                    continue;

                yield return Data;
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            int ID = LinkMap.Count;

            var XPATH = "//li[starts-with(@class, \"wp-manga-chapter\")]/a";
            var Nodes = Document.SelectNodes(XPATH);

            if (Nodes == null || Nodes.Count == 0)
            {
                var Browser = JSTools.DefaultBrowser;
                Browser.WaitForLoad(CurrentUrl.AbsoluteUri);
                Document = Browser.GetDocument();
                Nodes = Document.SelectNodes(XPATH);
            }

            foreach (var Node in ReverseChapters ? Nodes.Reverse() : Nodes)
            {
                string URL = Node.GetAttributeValue("href", "");
                string Name = Node.InnerText.Trim().ToLower();

                if (Name.StartsWith("chapter"))
                    Name = Name.Substring("chapter").Trim();
                if (Name.StartsWith("chap"))
                    Name = Name.Substring("chap").Trim(' ', '\t', '.');

                if (Name.StartsWith("ch."))
                    Name = Name.Substring("ch.", " ", IgnoreMissmatch: true);

                if (Name.StartsWith("capítulo"))
                    Name = Name.Substring("capítulo").Trim(' ', '\t', '.');

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
            Chapter.LoadUrl(LinkMap[ID], CFData);

            var ScriptNode = Chapter.SelectSingleNode("//script[contains(., 'chapter_preloaded_images')]");
            if (ScriptNode != null)
            {
                var Script = ScriptNode.InnerText + "\r\nchapter_preloaded_images;";
                var Rst = (List<object>)JSTools.DefaultBrowser.EvaluateScript(Script);
                return Rst.Cast<string>().ToArray();
            }

            string[] Links = (from x in Chapter
                              .SelectNodes("//img[starts-with(@id, \"image-\")]")
                              select (x.GetAttributeValue("data-src", null) ??
                                      x.GetAttributeValue("src", null) ??
                                      x.GetAttributeValue("data-cfsrc", "")).Trim()).ToArray();

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
                Version = new Version(1, 6)
            };
        }

        public bool IsValidUri(Uri Uri)
        {
            return (Uri.Host.ToLower().Contains("isekaiscan.com") && Uri.AbsolutePath.ToLower().Contains("manga/")) ||
                   (Uri.Host.ToLower().Contains("manga47.com") && Uri.AbsolutePath.ToLower().Contains("manga/")) ||
                   (Uri.Host.ToLower().Contains("manga68.com") && Uri.AbsolutePath.ToLower().Contains("manga/")) ||
                   (Uri.Host.ToLower().Contains("mangatx.com") && Uri.AbsolutePath.ToLower().Contains("manga/")) ||
                   (Uri.Host.ToLower().Contains("toonily.com") && Uri.AbsolutePath.ToLower().Contains("webtoon/"));
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

        Uri CurrentUrl;
        CloudflareData? CFData = null;
        HtmlDocument Document = new HtmlDocument();
        public ComicInfo LoadUri(Uri Uri)
        {
            CurrentUrl = Uri;
            ReverseChapters = Uri.Host.ToLower().Contains("manga47.com");

            Document.LoadUrl(Uri);
            if (string.IsNullOrWhiteSpace(Document.ToHTML()) || Document.IsCloudflareTriggered())
            {
                CFData = JSTools.BypassCloudflare(Uri.AbsoluteUri);
                Document.LoadHtml(CFData?.HTML);
            }

            ComicInfo Info = new ComicInfo();
            Info.Title = Document.SelectSingleNode("//div[@class=\"post-title\"]/*[self::h3 or self::h2 or self::h1]").InnerText.Trim();
            if (Info.Title.ToUpper().StartsWith("HOT"))
                Info.Title = Info.Title.Substring(3);
            Info.Title = HttpUtility.HtmlDecode(Info.Title).Trim();

            var ImgNode = Document.SelectSingleNode("//div[@class=\"summary_image\"]/a/img");

            var ImgUrl = ImgNode.GetAttributeValue("data-lazy-srcset", "");

            if (string.IsNullOrWhiteSpace(ImgUrl))
                ImgUrl = ImgNode.GetAttributeValue("data-src", "");
            else
                ImgUrl = ImgUrl.Trim().Split(',', ' ').First();

            if (string.IsNullOrWhiteSpace(ImgUrl))
                ImgUrl = ImgNode.GetAttributeValue("src", "");

            if (string.IsNullOrWhiteSpace(ImgUrl))
                ImgUrl = ImgNode.GetAttributeValue("data-cfsrc", "");

            if (ImgUrl.StartsWith("//"))
                ImgUrl = "http:" + ImgUrl;

            Info.Cover = ImgUrl.TryDownload(CFData);

            Info.ContentType = ContentType.Comic;

            return Info;
        }
    }
}
