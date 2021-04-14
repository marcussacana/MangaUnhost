﻿using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;

namespace MangaUnhost.Hosts
{
    class MangaHost : IHost
    {
        HtmlDocument Document;
        Dictionary<int, string> ChapterNames = new Dictionary<int, string>();
        Dictionary<int, string> ChapterLinks = new Dictionary<int, string>();

        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var PageUrl in GetChapterPages(ID))
            {
                yield return PageUrl.TryDownload(UserAgent: ProxyTools.UserAgent);
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            int ID = ChapterLinks.Count;

            var Nodes = Document.SelectNodes("//div[@class=\"chapters\"]//div[@class=\"tags\"]/a");


            var Link = Nodes.First().GetAttributeValue("href", string.Empty);
            var Doc = new HtmlDocument();
            Doc.LoadUrl(Link, UserAgent: ProxyTools.UserAgent, AcceptableErrors: Errors);

            Link = Link.Substring(0, Link.LastIndexOf('/') + 1);
            var Options = Doc.SelectNodes("//header[@class=\"navigation\"]//select[@name=\"Chapters\"]/option");

            foreach (var Option in Options)
            {
                var Value = Option.GetAttributeValue("value", string.Empty);

                ChapterLinks[ID] = Link + Value;
                ChapterNames[ID] = DataTools.GetRawName(Value);

                yield return new KeyValuePair<int, string>(ID, ChapterNames[ID++].Trim());
            }
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID).Length;
        }

        private string[] GetChapterPages(int ID)
        {
            var Page = GetChapterHtml(ID);
            List<string> Pages = new List<string>();

            var Scripts = Page.SelectNodes("//body//script[@type=\"text/javascript\"]");

            bool Found = false;

            foreach (var Node in Scripts)
            {
                if (!Node.InnerHtml.Contains("var images"))
                    continue;

                Found = true;

                string JS = Node.InnerHtml.Substring("var images = ", "\"];") + "\"]";
                var Result = (from x in JSTools.EvaluateScript<List<object>>(JS) select (string)x).ToArray();
                foreach (string PageHtml in Result)
                {
                    var PageUrl = PageHtml.Substring("src=", " ").Trim(' ', '\'', '"');
                    Pages.Add(PageUrl);
                }
            }

            if (!Found)
                foreach (var Img in Page.SelectNodes("//section[@id='imageWrapper']//img"))
                    Pages.Add(Img.GetAttributeValue("src", string.Empty));

            return (from x in Pages select x.Replace(".webp", "").Replace("/images", "/mangas_files")).ToArray();
        }

        private HtmlDocument GetChapterHtml(int ID)
        {
            HtmlDocument Document = new HtmlDocument();
            Document.LoadUrl(ChapterLinks[ID], UserAgent: ProxyTools.UserAgent, AcceptableErrors: Errors);
            if (Document.SelectSingleNode("//title")?.InnerText == "403 Forbidden" || Document.ParsedText.StartsWith("error code")) {
                Document.LoadUrl(ChapterLinks[ID], UserAgent: ProxyTools.UserAgent, Proxy: ProxyTools.Proxy, AcceptableErrors: Errors);
                if (Document.SelectSingleNode("//title")?.InnerText == "403 Forbidden" || Document.ParsedText.StartsWith("error code")) {
                    Thread.Sleep(1000);
                    return GetChapterHtml(ID);
                }
            }
            return Document;
        }

        public IDecoder GetDecoder()
        {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "MangaHost",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(3, 2)
            };
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.ToLower().Contains("mangahost") && Uri.AbsolutePath.ToLower().Contains("/manga/");
        }


        WebExceptionStatus[] Errors => new WebExceptionStatus[] { WebExceptionStatus.ConnectionClosed, WebExceptionStatus.ProtocolError };
        public ComicInfo LoadUri(Uri Uri)
        {
            Document = new HtmlDocument();
            Document.LoadUrl(Uri, UserAgent: ProxyTools.UserAgent, AcceptableErrors: Errors);

            ComicInfo Info = new ComicInfo();

            Info.Title = Document.Descendants("title").First().InnerText.Split('|')[0];
            Info.Title = HttpUtility.HtmlDecode(Info.Title);

            Info.Cover = new Uri(Document
                .SelectSingleNode("//div[@class=\"widget\"]//img")
                .GetAttributeValue("src", string.Empty)).TryDownload(UserAgent: ProxyTools.UserAgent);

            Info.ContentType = ContentType.Comic;

            return Info;
        }
        public bool IsValidPage(string HTML, Uri URL) => false;
    }
}
