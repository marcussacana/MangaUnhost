using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Caching;

namespace MangaUnhost.Hosts
{
    internal class Mediocretoons : IHost
    {
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var page in GetChapterPages(ID))
            {
                yield return page.TryDownload(CFData, $"https://{CurrentHost}/obra/{currentBook}/capitulo/{ID}", Headers: Headers);
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            return currentBookInfo.capitulos
                .Select(x => new KeyValuePair<int, string>(x.id, x.numero));
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID).Length;
        }

        private string[] GetChapterPages(int ID)
        {
            var apiData = DownloadString($"https://api.{CurrentHost}/capitulos/{ID}");
            var chapterData = JsonConvert.DeserializeObject<ChapterData>(apiData);
            return chapterData.paginas.Select(x => $"https://{CDN}/obras/{currentBook}/capitulos/{chapterData.numero}/{x.src}").ToArray();
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Author = "Marcussacana",
                GenericPlugin = false,
                Name = "Mediocretoons",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 1)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            throw new NotImplementedException();
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.Contains("mediocretoons") && Uri.PathAndQuery.Contains("obra/");
        }

        private string currentBook = string.Empty;
        private BookInfo currentBookInfo;
        private CloudflareData? CFData = null;
        private string CurrentHost;
        private string CDN;
        public ComicInfo LoadUri(Uri Uri)
        {
            currentBook = Uri.LocalPath.Split('/')[2];

            var doc = new HtmlDocument();

            CFData = doc.LoadUrl($"https://{Uri.Host}/obra/" + currentBook);

            CDN = $"storage.{Uri.Host}";

            var indexNode = doc.SelectSingleNode("//script[contains(@src, '/index-')]");

            if (indexNode != null) { 
                var scriptUrl = new Uri(Uri, indexNode.GetAttributeValue("src", null));
                var scriptData = DownloadString(scriptUrl.AbsoluteUri);

                if (scriptData?.Contains("CDN_URL") ?? false){ 
                    CDN = scriptData.Substring("CDN_URL:\"", "\",").Substring("://");
                }
            }


            var apiData = DownloadString($"https://api.{Uri.Host}/obras/{currentBook}");

            currentBookInfo = JsonConvert.DeserializeObject<BookInfo>(apiData);

            CurrentHost = Uri.Host;

            return new ComicInfo()
            {
                ContentType = ContentType.Comic,
                Cover = $"https://{CDN}/obras/{currentBook}/{currentBookInfo.imagem}".TryDownload(CFData, Uri.AbsoluteUri, Headers: Headers),
                Title = currentBookInfo.nome,
                Url = Uri,
            };
        }

        private (string Key, string Value)[] Headers => new (string Key, string Value)[]
        {
            ("Origin", $"https://{CurrentHost}")
        };

        private string DownloadString(string url)
        {
            return new Uri(url).TryDownloadString(CFData, $"https://{CurrentHost}/", Headers: Headers);
        }

        private struct ChapterData {
            public int id;
            public string numero;
            public List<ChapterPage> paginas;
        }

        private struct  ChapterPage
        {
            public string src;
        }
        private struct BookInfo
        {
            public int id;
            public string nome;
            public string imagem;

            public List<ChapterInfo> capitulos;
        }

        private struct ChapterInfo
        {
            public int id;
            public string numero;
        }
    }
}
