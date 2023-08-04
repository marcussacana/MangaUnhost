using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;

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

                var UserAgent = CFData?.UserAgent ?? ProxyTools.UserAgent;
                Data = Page.TryDownload(UserAgent: UserAgent, Referer: LinkMap[ID], Cookie: Cookies);

                if (Data == null || Data.Length == 0)
                    continue;

                yield return Data;
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            int ID = LinkMap.Count;

            var XPATH = "//li[starts-with(@class, 'wp-manga-chapter')]/a";
            var Nodes = Document.SelectNodes(XPATH);


            if (Nodes == null || Nodes.Count == 0)
            {
                var Browser = JSTools.DefaultBrowser;
                Browser.WaitForLoad(CurrentUrl.AbsoluteUri);

                var Begin = DateTime.Now;
                while (Nodes == null || Nodes.Count == 0)
                {
                    if ((DateTime.Now - Begin).TotalSeconds > 20)
                        break;
                    Document = Browser.GetDocument();
                    Nodes = Document.SelectNodes(XPATH);
                }
            }

            foreach (var Node in ReverseChapters ? Nodes.Reverse() : Nodes)
            {
                string URL = Node.GetAttributeValue("href", "");
                string Name = Node.InnerText.Trim().ToLower();
                string Prefix = string.Empty;

                char[] GeneralTrim = new char[] { ' ', '-', '\t', '.' };

                if (Name.StartsWith("vol")) {
                    Name = Name.Substring(" ").Trim();
                    Prefix = "Vol. " + Name.Substring(null, " ") + " Ch. ";
                    Name = Name.Substring(" ").Trim(GeneralTrim);
                }

                if (Name.StartsWith("chapter"))
                    Name = Name.Substring("chapter").Trim(GeneralTrim);

                if (Name.StartsWith("chap"))
                    Name = Name.Substring("chap").Trim(GeneralTrim);

                if (Name.StartsWith("capitulo"))
                    Name = Name.Substring("capitulo").Trim(GeneralTrim);

                if (Name.StartsWith("capítulo"))
                    Name = Name.Substring("capítulo").Trim(GeneralTrim);

                if (Name.StartsWith("cap."))
                    Name = Name.Substring("cap.").Trim(GeneralTrim);

                if (Name.StartsWith("cap"))
                    Name = Name.Substring("cap").Trim(GeneralTrim);

                if (Name.StartsWith("ch."))
                    Name = Name.Substring("ch.", " ", IgnoreMissmatch: true);

                if (Name.Contains("-"))
                    Name = Name.Split('-').First().Trim(GeneralTrim);

                Name = Prefix + DataTools.GetRawName(Name);

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
            
            Chapter.LoadUrl(LinkMap[ID], UserAgent: CFData?.UserAgent, Cookies: Cookies, Referer: CurrentUrl.AbsoluteUri);

            var ScriptNode = Chapter.SelectSingleNode("//script[contains(., 'chapter_preloaded_images')]");
            if (ScriptNode != null)
            {
                var Script = ScriptNode.InnerText + "\r\nchapter_preloaded_images;";
                var Rst = (List<object>)JSTools.DefaultBrowser.EvaluateScript(Script);
                return Rst.Cast<string>().ToArray();
            }

            string[] Links = (from x in Chapter
                              .SelectNodes("//img[starts-with(@id, 'image-')]|//*[starts-with(@id, 'image-')]//img")
                              select (x.GetAttributeValue("data-src", null) ??
                                      x.GetAttributeValue("src", null) ??
                                      x.GetAttributeValue("data-cfsrc", "")).Trim()).Distinct().ToArray();

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
                Version = new Version(2, 4, 2)
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
            if (!HTML.Contains("wp-manga-chapter") && !HTML.Contains("wpManga"))
                return false;

            return true;
        }

        Uri CurrentUrl;

        CookieContainer _Cookies;
        CloudflareData? CFData = null;

        CookieContainer Cookies { 
            get { 
                if (CFData.HasValue)
                    return CFData?.Cookies; 
                return _Cookies;
            }
            set {
                _Cookies = value;
            }
        }


        HtmlDocument Document = new HtmlDocument();
        public ComicInfo LoadUri(Uri Uri)
        {
            Cookies = new CookieContainer();
            CurrentUrl = Uri;
            ReverseChapters = Uri.Host.ToLower().Contains("manga47.com");

            Document.LoadUrl(Uri, Cookies: Cookies);
            if (string.IsNullOrWhiteSpace(Document.ToHTML()) || Document.IsCloudflareTriggered())
            {
                CFData = JSTools.BypassCloudflare(Uri.AbsoluteUri);
                Document.LoadHtml(CFData?.HTML);
            }

            ComicInfo Info = new ComicInfo();
            var TitleNode = Document.SelectSingleNode("//div[@class='post-title']/*[self::h3 or self::h2 or self::h1]");
            try { TitleNode.RemoveChild(TitleNode.ChildNodes.Where(x => x.Name == "span").Single()); } catch { }
            Info.Title = TitleNode.InnerText.Trim();

            if (Info.Title.ToUpper().StartsWith("HOT"))
                Info.Title = Info.Title.Substring(3);
            Info.Title = HttpUtility.HtmlDecode(Info.Title).Trim();

            var ImgNode = Document.SelectSingleNode("//div[@class='summary_image']//img");

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

            Info.Cover = TryDownload(ImgUrl);

            Info.ContentType = ContentType.Comic;

            return Info;
        }

        private byte[] TryDownload(string Url)
        {
            return Url.TryDownload(CFData, Referer: CurrentUrl.AbsoluteUri, UserAgent: ProxyTools.UserAgent);
        }
    }
}
