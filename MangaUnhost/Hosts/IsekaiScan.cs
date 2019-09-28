﻿using HtmlAgilityPack;
using MangaUnhost.Browser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MangaUnhost.Hosts
{
    class IsekaiScan : IHost
    {
        Dictionary<int, string> LinkMap = new Dictionary<int, string>();
        public string DownloadChapter(int ID)
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

            foreach (var Node in Document.SelectNodes("//li[starts-with(@class, \"wp-manga-chapter\")]/a"))
            {
                string URL = Node.GetAttributeValue("href", "");
                string Name = Node.InnerText.Trim();

                if (Name.StartsWith("chapter"))
                    Name = Name.Substring("chapter").Trim();
                if (Name.StartsWith("chap"))
                    Name = Name.Substring("chap").Trim(' ', '\t', '.');

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
                              select x.GetAttributeValue("data-src", "").Trim()).ToArray();

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
                Name = "Isekai Scan",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.ToLower().Contains("isekaiscan.com") && Uri.AbsolutePath.ToLower().Contains("manga/");
        }

        HtmlDocument Document = new HtmlDocument();
        public ComicInfo LoadUri(Uri Uri)
        {
            Document.LoadUrl(Uri);

            ComicInfo Info = new ComicInfo();
            Info.Title = Document.SelectSingleNode("//div[@class=\"post-title\"]/h3").InnerText;
            Info.Title = HttpUtility.HtmlDecode(Info.Title).Trim();

            Info.Cover = Document
                .SelectSingleNode("//div[@class=\"summary_image\"]/a/img")
                .GetAttributeValue("data-src", "").TryDownload();

            Info.ContentType = ContentType.Comic;

            return Info;
        }
    }
}