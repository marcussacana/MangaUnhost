﻿using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Xml;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace MangaUnhost.Hosts
{
    class WPMangaReader : IHost
    {
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var Page in GetChapterPages(ID))
            {
                yield return Page.TryDownload(CFData, Referer: CurrentUrl.AbsoluteUri);
            }
        }

        Dictionary<int, string> LinkMap = new Dictionary<int, string>();

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            int ID = LinkMap.Count;
            foreach (var Chap in Document.SelectNodes("//div[@id='chapterlist']//a") ?? ChapterDocument?.SelectNodes("//li[contains(@class, 'wp-manga-chapter')]/a"))
            {
                var ChapNumInfo = Chap.ChildNodes.Where(x => x.HasClass("chapternum"));
                string Name = HttpUtility.HtmlDecode((ChapNumInfo.Any() ? ChapNumInfo.First().InnerHtml : Chap.InnerText).ToLowerInvariant().Trim());
                string URL = Chap.GetAttributeValue("href", "");

                if (Name.StartsWith("chapter"))
                    Name = Name.Substring("chapter").Trim();

                if (Name.StartsWith("chap"))
                    Name = Name.Substring("chap").Trim(' ', '\t', '.');

                if (Name.StartsWith("ch."))
                    Name = Name.Substring("ch.", " ", IgnoreMissmatch: true);

                if (Name.StartsWith("capítulo"))
                    Name = Name.Substring("capítulo").Trim(' ', '\t', '.');

                if (Name.StartsWith("cap"))
                    Name = Name.Substring("cap").Trim(' ', '\t', '.');

                if (Name.Contains("-"))
                    Name = Name.Split('-').First().Trim();

                Name = DataTools.GetRawName(Name);

                LinkMap[ID] = URL;

                yield return new KeyValuePair<int, string>(ID++, Name);
            }
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID).Length;
        }

        string[] GetChapterPages(int ID)
        {
            var Doc = new HtmlDocument();
            Doc.LoadUrl(LinkMap[ID], CFData);

            try
            {
                var JS = Doc.SelectSingleNode("//script[contains(., 'ts_reader')]").InnerHtml;

                var ImgList = JS.Substring("\"source\"", "}");
                return DataTools.ExtractHtmlLinks(ImgList, CurrentUrl.Host, true).ToArray();
            }
            catch
            {
                List<string> Pages = new List<string>();
                foreach (var Page in Doc.SelectNodes("//img[contains(@id, 'image-')]"))
                {
                    var ImgUrl = Page.GetAttributeValue("data-lazy-srcset", "");

                    if (string.IsNullOrWhiteSpace(ImgUrl))
                        ImgUrl = Page.GetAttributeValue("data-src", "");
                    else
                        ImgUrl = ImgUrl.Trim().Split(',', ' ').First();

                    if (string.IsNullOrWhiteSpace(ImgUrl))
                        ImgUrl = Page.GetAttributeValue("src", "");

                    if (string.IsNullOrWhiteSpace(ImgUrl))
                        ImgUrl = Page.GetAttributeValue("data-cfsrc", "");

                    if (ImgUrl.StartsWith("//"))
                        ImgUrl = "http:" + ImgUrl;

                    Pages.Add(ImgUrl.Trim());
                }
                return Pages.ToArray();
            }
        }

        public IDecoder GetDecoder()
        {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "WPMangaReader",
                Author = "Marcussacana",
                GenericPlugin = true,
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(2, 1)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            return (HTML.Contains("/mangareader/") && HTML.Contains("\"main-info\"") ) || 
                   (HTML.Contains("wp-manga-chapter") && HTML.Contains("tab-chapter-listing"));
        }

        public bool IsValidUri(Uri Uri)
        {
            return false;
        }

        Uri CurrentUrl;

        CookieContainer _Cookies;
        CloudflareData? CFData = null;

        CookieContainer Cookies
        {
            get
            {
                if (CFData.HasValue)
                    return CFData?.Cookies;
                return _Cookies;
            }
            set
            {
                _Cookies = value;
            }
        }

        HtmlDocument ChapterDocument = null;

        HtmlDocument Document = new HtmlDocument();
        public ComicInfo LoadUri(Uri Uri)
        {
            Cookies = new CookieContainer();
            CurrentUrl = Uri;

            Document.LoadUrl(Uri, Cookies: Cookies);
            if (string.IsNullOrWhiteSpace(Document.ToHTML()) || Document.IsCloudflareTriggered())
            {
                CFData = JSTools.BypassCloudflare(Uri.AbsoluteUri);
                Document.LoadHtml(CFData?.HTML);
            }

            if (!CurrentUrl.AbsoluteUri.EndsWith("/"))
                CurrentUrl = new Uri(CurrentUrl.AbsoluteUri + "/");

            var ChapsUrl = new Uri(CurrentUrl.AbsoluteUri + "ajax/chapters/");

            try
            {
                var Data = ChapsUrl.Upload();
                ChapterDocument = new HtmlDocument();
                using (var Stream = new MemoryStream(Data))
                    ChapterDocument.Load(Stream);
            }
            catch { }

            ComicInfo Info = new ComicInfo();
            Info.Title = HttpUtility.HtmlDecode((Document.SelectSingleNode("//*[(self::h1 or self::h2 or self::h3) and @class='entry-title']") ??
                                                 Document.SelectSingleNode("//div[@id='manga-title']/*[(self::h1 or self::h2 or self::h3)]")).InnerText.Trim());

            var ImgNode = Document.SelectSingleNode("//div[@class='thumb']/img") ??
                          Document.SelectSingleNode("//div[@class='summary_image']//img");

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