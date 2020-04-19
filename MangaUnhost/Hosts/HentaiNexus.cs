using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using System;
using System.Collections.Generic;
using System.Web;

namespace MangaUnhost.Hosts
{
    class HentaiNexus : IHost
    {
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var Node in Document.SelectNodes("(//div[@class=\"card-image\"])/../.."))
            {
                Uri Link = new Uri(new Uri("https://hentainexus.com"), Node.GetAttributeValue("href", ""));
                HtmlDocument Document = new HtmlDocument();
                Document.LoadUrl(Link);
                Uri Img = new Uri(HttpUtility.HtmlDecode(Document.SelectSingleNode("//figure[@class=\"image\"]/img").GetAttributeValue("src", "")));
                yield return Img.TryDownload(Link.ToString());
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            yield return new KeyValuePair<int, string>(0, "Oneshot");
        }

        public int GetChapterPageCount(int ID)
        {
            return Document.SelectNodes("//div[@class=\"card-image\"]/figure/img").Count;
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "Hentai Nexus",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidUri(Uri Uri)
        {
            //https://hentainexus.com/view/6580
            return Uri.Host.ToLower().Contains("hentainexus") && Uri.PathAndQuery.ToLower().Contains("/view/");
        }

        HtmlDocument Document;
        Uri CurrentUrl;

        public ComicInfo LoadUri(Uri Uri)
        {
            CurrentUrl = Uri;
            
            Document = new HtmlDocument();
            Document.LoadUrl(Uri);

            ComicInfo Info = new ComicInfo();
            Info.ContentType = ContentType.Comic;
            Info.Title = HttpUtility.HtmlDecode(Document.SelectSingleNode("//h1[@class=\"title\"]").InnerText);
            Info.Cover = HttpUtility.HtmlDecode(Document.SelectSingleNode("//figure[@class=\"image\"]/img").GetAttributeValue("src", "")).TryDownload();
            Info.Url = Uri;

            return Info;

        }
        public bool IsValidPage(string HTML, Uri URL) => false;
    }
}
