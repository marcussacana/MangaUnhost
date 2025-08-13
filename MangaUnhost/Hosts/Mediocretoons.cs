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
                yield return page.TryDownload(CFData, $"https://mediocretoons.com/obra/{currentBook}/capitulo/{ID}", Headers: Headers);
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            return currentBookInfo.capitulos
                .Select(x => new KeyValuePair<int, string>(x.id, x.numero)).Reverse();
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID).Length;
        }

        private string[] GetChapterPages(int ID)
        {
            var apiData = DownloadString($"https://api.mediocretoons.com/capitulos/{ID}");
            var chapterData = JsonConvert.DeserializeObject<ChapterData>(apiData);
            return chapterData.paginas.Select(x => $"https://storage.mediocretoons.com/obras/{currentBook}/capitulos/{chapterData.numero}/{x.src}").ToArray();
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
                Version = new Version(1, 0)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            throw new NotImplementedException();
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.Contains("mediocretoons.com") && Uri.PathAndQuery.Contains("obra/");
        }

        private string currentBook = string.Empty;
        private BookInfo currentBookInfo;
        private CloudflareData? CFData = null;
        public ComicInfo LoadUri(Uri Uri)
        {
            currentBook = Uri.LocalPath.Split('/')[2];

            CFData = new HtmlDocument().LoadUrl("https://mediocretoons.com/obra/" + currentBook);

            var apiData = DownloadString($"https://api.mediocretoons.com/obras/{currentBook}");

            currentBookInfo = JsonConvert.DeserializeObject<BookInfo>(apiData);

            return new ComicInfo()
            {
                ContentType = ContentType.Comic,
                Cover = $"https://storage.mediocretoons.com/obras/{currentBook}/{currentBookInfo.imagem}".TryDownload(CFData, Uri.AbsoluteUri, Headers: Headers),
                Title = currentBookInfo.nome,
                Url = Uri,
            };
        }

        private (string Key, string Value)[] Headers => new (string Key, string Value)[]
        {
            ("Origin", "https://mediocretoons.com")
        };

        private string DownloadString(string url)
        {
            return new Uri(url).TryDownloadString(CFData, "https://mediocretoons.com/", Headers: Headers);
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
