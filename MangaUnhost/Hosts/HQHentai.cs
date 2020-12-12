using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using System;
using System.Collections.Generic;
using System.Web;

namespace MangaUnhost.Hosts
{
    class HQHentai : IHost
    {
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var Node in Document.SelectNodes("//div[@class='fotos']/div[@class='foto']/img"))
            {
                var Link = Node.GetAttributeValue("src", "").EnsureAbsoluteUrl("https://www.hqhentai.com.br");
                yield return Link.TryDownload();
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            yield return new KeyValuePair<int, string>(0, "Oneshot");
        }

        public int GetChapterPageCount(int ID)
        {
            return Document.SelectNodes("//div[@class='fotos']/div[@class='foto']/img").Count;
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "HQHentai",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidUri(Uri Uri)
        {
            //https://www.hqhentai.com.br/revenge-hypnosis-2/
            return Uri.Host.ToLower().Contains("hqhentai");
        }

        HtmlDocument Document;
        Uri CurrentUrl;

        public ComicInfo LoadUri(Uri Uri)
        {
            CurrentUrl = Uri;
            
            Document = new HtmlDocument();
            Document.LoadUrl(Uri);

            var Signature = Document.SelectSingleNode("//h1[@class='title']/span");
            Signature.Remove();

            ComicInfo Info = new ComicInfo();
            Info.ContentType = ContentType.Comic;
            Info.Title = HttpUtility.HtmlDecode(Document.SelectSingleNode("//h1[@class='title']").InnerText);
            Info.Cover = HttpUtility.HtmlDecode(Document.SelectSingleNode("//figure/img").GetAttributeValue("src", "")).TryDownload();
            Info.Url = Uri;

            return Info;

        }
        public bool IsValidPage(string HTML, Uri URL) => false;
    }
}
