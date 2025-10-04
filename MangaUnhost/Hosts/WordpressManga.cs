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

            if (Nodes == null || Nodes.Count == 0)
                Nodes = Document.SelectNodes("//div[@class='chapter-details']/a");

            string lastName = null;

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

                if (Name.Contains(":"))
                    Name = Name.Substring(null, ":").Trim(GeneralTrim);


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

                if (lastName == Name)
                    LinkMap[--ID] = $"{URL}|{LinkMap[ID]}";
                else
                    LinkMap[ID] = URL;

                lastName = Name;

                yield return new KeyValuePair<int, string>(ID++, Name);
            }
        }

        public int GetChapterPageCount(int ID)
        {
            return GetPageLinks(ID).Length;
        }

        public int LastSuccess = -1;
        private string[] GetPageLinks(int ID)
        {

            List<string> AllLinks = new List<string>();
            foreach (var link in LinkMap[ID].Split('|'))
            {
                var Chapter = new HtmlDocument();

                int retry = 12;
                while (Chapter.DocumentNode.InnerText == string.Empty && retry-- > 0)
                {
                    switch (retry == 11 ? LastSuccess : retry) {
                        case 10:
                        case 9:
                            CFData = Chapter.LoadUrl(link, CFData, Referer: CurrentUrl.Host);
                            break;
                        case 8:
                        case 7:
                            CFData = Chapter.LoadUrl(link, Referer: CurrentUrl.Host, UserAgent: ProxyTools.UserAgent);
                            break;
                        case 6:
                        case 5:
                            Chapter.LoadUrl(link, UserAgent: ProxyTools.UserAgent);
                            break;
                        case 4:
                        case 3:
                            Chapter.LoadUrl(link);
                            break;
                        case 2:
                        case 1:
                            JSTools.DefaultBrowser.WaitForLoad(link);
                            Chapter.LoadHtml(JSTools.DefaultBrowser.GetDocument().ToHTML());
                            break;
                    }
                    ThreadTools.Wait(100);
                }

                LastSuccess = retry;

                var ScriptNode = Chapter.SelectSingleNode("//script[contains(., 'chapter_preloaded_images')]");
                if (ScriptNode != null)
                {
                    var Script = ScriptNode.InnerText + "\r\nchapter_preloaded_images;";
                    var Rst = (List<object>)JSTools.DefaultBrowser.EvaluateScript(Script);
                    AllLinks.AddRange(Rst.Cast<string>().Where(x => x.StartsWith("http")));
                    continue;
                }

                string[] Links = (from x in Chapter
                                  .SelectNodes("//img[starts-with(@id, 'image-')]|//*[starts-with(@id, 'image-')]//img|//img[@class='chapter-image']")
                                  select (x.GetAttributeValue("data-src", null) ??
                                          x.GetAttributeValue("src", null) ??
                                          x.GetAttributeValue("data-cfsrc", "")).Trim()).Distinct().ToArray();


                AllLinks.AddRange(Links.Where(x => x.StartsWith("http")));
            }

            return AllLinks.ToArray();
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
                Version = new Version(2, 5, 1)
            };
        }

        public bool IsValidUri(Uri Uri)
        {
            return (Uri.Host.ToLower().Contains("isekaiscan.com") && Uri.AbsolutePath.ToLower().Contains("manga/")) ||
                   (Uri.Host.ToLower().Contains("manga47.com") && Uri.AbsolutePath.ToLower().Contains("manga/")) ||
                   (Uri.Host.ToLower().Contains("manga68.com") && Uri.AbsolutePath.ToLower().Contains("manga/")) ||
                   (Uri.Host.ToLower().Contains("mangatx.com") && Uri.AbsolutePath.ToLower().Contains("manga/")) ||
                   (Uri.Host.ToLower().Contains("toonily.com") && Uri.AbsolutePath.ToLower().Contains("webtoon/")) ||
                   (Uri.Host.ToLower().Contains("mangalivre.blog") && Uri.AbsolutePath.ToLower().Contains("manga/"));
        }
        public bool IsValidPage(string HTML, Uri URL)
        {
            if (!HTML.Contains("wp-manga-chapter") && !HTML.Contains("wpManga"))
                return false;

            return true;
        }

        Uri CurrentUrl;

        static CookieContainer _Cookies;
        static CloudflareData? CFData = null;

        static CookieContainer Cookies { 
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
            var TitleNode = Document.SelectSingleNode("//div[@class='post-title' or @class='manga-info']/*[self::h3 or self::h2 or self::h1]");
            try { TitleNode.RemoveChild(TitleNode.ChildNodes.Where(x => x.Name == "span").Single()); } catch { }
            Info.Title = TitleNode.InnerText.Trim();

            if (Info.Title.ToUpper().StartsWith("HOT"))
                Info.Title = Info.Title.Substring(3);
            Info.Title = HttpUtility.HtmlDecode(Info.Title).Trim();

            var ImgNode = Document.SelectSingleNode("//div[@class='summary_image' or @class='manga-cover']//img");

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
