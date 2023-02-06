using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using System;
using System.Collections.Generic;
using System.Web;
using System.Text;

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
                yield return TryDownload(new Uri(Link));
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
                Name = "HQH",
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
            var HTML = Encoding.UTF8.GetString(TryDownload(Uri));
            Document.LoadHtml(HTML);

            if (Program.Debug){
                Program.Writer?.WriteLine("Load URL: {0}\r\nHTML: {1}", Uri.AbsoluteUri, HTML);
                Program.Writer?.Flush();
            }
            
            foreach (var Node in Document.SelectNodes("//span[@class='none']"))
                Node.Remove();

            ComicInfo Info = new ComicInfo();
            Info.ContentType = ContentType.Comic;
            Info.Title = HttpUtility.HtmlDecode(Document.SelectSingleNode("//h1[@class='Title']").InnerText);
            Info.Cover = HttpUtility.HtmlDecode(Document.SelectSingleNode("//figure/img").GetAttributeValue("src", "")).TryDownload();
            Info.Url = Uri;

            return Info;

        }

        CloudflareData? CFData = null;

        private byte[] TryDownload(Uri Url, string Referer = "https://www.hqhentai.com.br/") {
            if (CFData != null) {
                return Url.TryDownload(Referer, CFData?.UserAgent, Cookie: CFData?.Cookies);
            }
            try
            {
                return Url.TryDownload(Referer);
            }
            catch {
                CFData = JSTools.BypassCloudflare(Url.AbsoluteUri);
                return TryDownload(Url, Referer);
            }
        }

        public bool IsValidPage(string HTML, Uri URL) => false;
    }
}
