using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web;

namespace MangaUnhost.Hosts
{
    public class Taiyo : IHost
    {

        public class Manga
        {
            public string Id { get; }
            public string Title { get; }

            public Manga(string id, string title)
            {
                Id = id;
                Title = title;
            }
        }

        public class Chapter
        {
            public string Id { get; }
            public string Number { get; }
            public Chapter(string id, string number)
            {
                Id = id;
                Number = number;
            }
        }

        public class Pages
        {
            public string ChapterId { get; }
            public string Number { get; }
            public string Name { get; }
            public List<string> PageUrls { get; }

            public Pages(string chapterId, string number, string name, List<string> pageUrls)
            {
                ChapterId = chapterId;
                Number = number;
                Name = name;
                PageUrls = pageUrls;
            }
        }

        private readonly string api = "https://taiyo.moe";
        private readonly string cdn = "https://cdn.taiyo.moe/medias/";

        public List<Chapter> GetChapters(string mangaId)
        {
            var chapters = new List<Chapter>();
            var page = 1;
            JObject root = null;

            do
            {
                var dic = new Dictionary<string, object>
                {
                    ["0"] = new Dictionary<string, object>() {
                        { "json",
                            new {
                                mediaId = mangaId,
                                page = page,
                                perPage = 30
                            }
                        }
                    }
                };
                var input = Uri.EscapeDataString(JsonConvert.SerializeObject(dic));

                var res = new Uri($"{api}/api/trpc/chapters.getByMediaId?batch=1&input={input}").TryDownloadString(CurrentUri.AbsoluteUri, UserAgent: ProxyTools.UserAgent);
                var arr = JArray.Parse(res);
                root = (JObject)arr[0]["result"]["data"]["json"];
                var chapterList = (JArray)root["chapters"];


                foreach (var ch in chapterList)
                {
                    chapters.Add(new Chapter(
                        ch["id"].ToString(),
                        ch["number"].ToString()
                    ));
                }

                page++;
            }
            while (page <= root["totalPages"].Value<int>());

            return chapters;
        }

        public string[] GetPages(string Id)
        {
            var dic = new Dictionary<string, object>
            {
                ["0"] = new Dictionary<string, object>() {
                    { "json", Id }
                }
            };
            var input = Uri.EscapeDataString(JsonConvert.SerializeObject(dic));
            var res = new Uri($"{api}/api/trpc/chapters.getById?batch=1&input={input}").TryDownloadString(CFData, CurrentUri.AbsoluteUri);
            var arr = JArray.Parse(res);
            var json = (JObject)arr[0]["result"]["data"]["json"];

            var mediaId = json["media"]["id"].ToString();
            var chapterId = json["id"].ToString();
            var pageList = new List<string>();

            foreach (var p in (JArray)json["pages"])
            {
                var pageId = p["id"].ToString();
                var ext = p["extension"].ToString();
                pageList.Add($"{cdn}{mediaId}/chapters/{chapterId}/{pageId}.{ext}");
            }

            return pageList.ToArray();
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host == "taiyo.moe" && Uri.PathAndQuery.Contains("media");
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            throw new NotImplementedException();
        }

        string ID = null;
        CloudflareData? CFData = null;
        HtmlDocument Doc = new HtmlDocument();
        Uri CurrentUri = null;
        public ComicInfo LoadUri(Uri Uri)
        {
            CurrentUri = Uri;
            CFData = Doc.LoadUrl(Uri);

            ID = Uri.Segments[2].Trim('/');

            var CoverUrl = new Uri(Uri, Doc
                    .DocumentNode
                    .SelectSingleNode("//img[contains(@class, 'cover-url')]").GetAttributeValue("src", null));

            if (CoverUrl.PathAndQuery.Contains("_next"))
            {
                CoverUrl = new Uri(HttpUtility.UrlDecode(CoverUrl.GetParameter("url")));
            }

            return new ComicInfo()
            {
                Title = Doc
                    .DocumentNode
                    .SelectSingleNode("//p[contains(@class, 'media-title')]").InnerText,
                Cover = CoverUrl.TryDownload(CFData),
                ContentType = ContentType.Comic,
                Url = Uri
            };
        }

        public int GetChapterPageCount(int ID)
        {
            var Chapter = Chapters[ID];
            return GetPages(Chapter).Length;
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        Dictionary<int, string> Chapters = new Dictionary<int, string>();
        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            foreach (var chapter in GetChapters(ID))
            {
                int vId = Chapters.Count;
                Chapters[vId] = chapter.Id;
                yield return new KeyValuePair<int, string>(vId, chapter.Number.ToString());
            }
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var pageUrl in GetPages(Chapters[ID]))
            {
                yield return new Uri(pageUrl).TryDownload(CFData, Referer: $"https://taiyo.moe/media/{ID}");
            }   
        }

        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo
            {
                Name = "Taiyo",
                Version = new Version(1, 0),
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false
            };
        }
    }
}
